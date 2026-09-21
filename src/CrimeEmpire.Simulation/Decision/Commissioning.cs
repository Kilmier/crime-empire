namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Sim;
using CrimeSim.Strategy;

/// <summary>One cached executor evaluation for one retained tribute leaf; never a running operation.</summary>
public sealed record PreparedCommission(Candidate Operation, IReadOnlyList<ScoreBreakdown> Scored)
{
    public IReadOnlyList<Candidate> Available { get; } = Array.AsReadOnly(Scored.Select(s => s.Candidate)
        .OrderBy(c => c.Id, StringComparer.Ordinal).ToArray());
}

public static class Commissioning
{
    public static bool IsOperation(Candidate c) => c.Kind == ActionKind.StartStrategy
        && c.Strategy == StrategyKind.SecureTribute && c.TargetId is not null;

    public static bool PersonallyEligible(GeneratorContext ctx, Candidate operation)
        => ctx.CurrentExecution is null && RequirementsMet(ctx.Actor, operation);

    private static bool RequirementsMet(CharacterView actor, Candidate c)
        => (c.RequiredSkill is not { } skill || actor.Capabilities[skill] >= c.RequiredSkillLevel)
           && actor.Capabilities.Crew >= c.RequiredCrew
           && actor.Capabilities.Authority >= c.RequiredAuthority
           && actor.Capabilities.CanReach(c.Domain);

    // The same eligibility used by existing handover. Actual subordinate skills are not forecasts.
    internal static IReadOnlyList<string> Delegates(GeneratorContext ctx) => ctx.SubordinateIds
        .Intersect(ctx.AcquaintedIds).Intersect(ctx.AvailableSubordinateIds)
        .OrderBy(id => id, StringComparer.Ordinal).ToArray();

    internal static Candidate DelegateCandidate(string id, string executor, StrategyKind kind,
        string? domain, CoercionMethod? method, bool comparative)
        => new(id, ActionKind.DelegateStrategy, nameof(Generators), $"have {executor} execute {kind}")
        { TargetId = executor, Strategy = kind, Domain = domain, Method = method,
          RequiredCrew = 1, ComparingExecutors = comparative };

    public static bool CanCommission(GeneratorContext ctx, Candidate operation, SalienceProfile salience)
        => PersonallyEligible(ctx, operation) || (ctx.Actor.Capabilities.Crew >= 1
            && ctx.Actor.Capabilities.CanReach(operation.Domain)
            && Delegates(ctx).Count > 0
            && salience.For(DelegateCandidate("", "", operation.Strategy!.Value,
                operation.Domain, operation.Method, false)) >= SalienceProfile.Threshold);

    public static PreparedCommission Prepare(PreparedDecision decision, Candidate operation)
    {
        if (decision.IsResolved || !IsOperation(operation) || !decision.Allows(operation.Id))
            throw new SimulationInvariantException("Commissioning requires an unresolved retained operation leaf.");
        // A caller may provide a value with a retained id; only the actual retained leaf owns
        // the target, method and requirements. Never let a forged value poison the draft cache.
        operation = decision.Available.Single(c => c.Id == operation.Id);
        if (decision.Commissions.TryGetValue(operation.Id, out var cached)) return cached;

        var ctx = decision.Context;
        var delegates = Delegates(ctx);
        var retained = delegates.Select(id => DelegateCandidate($"executor:{operation.Id}:{id}", id,
                operation.Strategy!.Value, operation.Domain, operation.Method, delegates.Count > 1))
            .Where(c => RequirementsMet(ctx.Actor, c) && decision.Salience.For(c) >= SalienceProfile.Threshold)
            .OrderByDescending(c => decision.Salience.For(c))
            .ThenByDescending(c => Utility.DelegationConsiderations(ctx.Actor, decision.Actor.Psychology,
                decision.Perceived, c.TargetId!, c.ComparingExecutors).Sum(p => p.Value))
            .ThenBy(c => c.Id, StringComparer.Ordinal).Take(5).ToList();
        if (PersonallyEligible(ctx, operation))
            retained.Add(operation with { Id = $"executor:{operation.Id}:{decision.Actor.Id}" });

        // An existing keyed stream, local to this decision/leaf. Navigation order cannot reroll it,
        // and preparing executor options never consumes the world's or first stage's random state.
        var rng = Rng.ForOccasion(decision.World.Seed,
            $"commission|{decision.Actor.Id}|{decision.DecisionIndex}|{operation.Id}");
        var scored = retained.OrderBy(c => c.Id, StringComparer.Ordinal)
            .Select(c => Utility.Score(c, ctx.Actor, decision.Actor.Psychology, decision.Perceived,
                decision.Agenda, rng, null))
            .OrderByDescending(s => s.Total).ThenBy(s => s.Candidate.Id, StringComparer.Ordinal).ToArray();
        cached = new PreparedCommission(operation, Array.AsReadOnly(scored));
        decision.Commissions.Add(operation.Id, cached);
        return cached;
    }

    internal static string Executor(PreparedDecision decision, Candidate choice)
        => choice.Kind == ActionKind.DelegateStrategy ? choice.TargetId! : decision.Actor.Id;

    internal static void Validate(World world, Character owner, Candidate operation, string executor)
    {
        if (!IsOperation(operation) || !world.Characters.ContainsKey(executor)
            || !Pipeline.AvailableToExecute(world, executor)
            || !owner.Capabilities.CanReach(operation.Domain)
            || owner.Capabilities.Authority < operation.RequiredAuthority
            || !operation.RequiredKnowledge.All(owner.Cognition.Holds)
            || owner.Execution.Operations.Any(s => s.Kind == operation.Strategy && s.TargetId == operation.TargetId))
            throw new SimulationInvariantException("The commissioning choice is no longer eligible.");
        if (executor == owner.Id)
        {
            if (!RequirementsMet(owner.View, operation))
                throw new SimulationInvariantException("Personal execution is no longer eligible.");
        }
        else if (owner.Capabilities.Crew < 1 || !Pipeline.SubordinatesOf(world, owner).Contains(executor)
            || !Acquaintance.KnownTo(world, owner).Contains(executor))
            throw new SimulationInvariantException("Commissioning requires a known eligible direct subordinate.");
    }
}
