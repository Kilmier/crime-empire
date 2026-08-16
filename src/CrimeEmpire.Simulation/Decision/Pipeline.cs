namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;
using CrimeSim.Strategy;

/// <summary>
/// One deliberation, carried up to the point of choosing and no further.
///
/// <b>Developer-facing, exactly like <see cref="DecisionRecord"/>.</b> It carries the ranked
/// breakdowns, the rejections and the salience notes, which are precisely the hidden state a player
/// must not be handed. The player-facing projection of a paused decision is
/// <c>CrimeSim.Session.PendingDecision</c>, which is built from <see cref="Available"/> alone and
/// from nothing else here.
///
/// It exists because a player choosing an action and an NPC choosing one have to be the same act.
/// The pipeline's first six stages run identically for both — the same beliefs, the same bounded
/// generation, the same salience, knowledge, capability and access rejections — and only the last
/// question, "which available option do you prefer?", is answered by a person instead of by
/// <see cref="Utility"/>. Splitting there is the smallest cut that makes player choice possible
/// without a second decision path, which is what actor parity forbids.
/// </summary>
public sealed class PreparedDecision
{
    internal PreparedDecision(
        World world,
        Character actor,
        ScheduledEvent trigger,
        Agenda agenda,
        GeneratorContext ctx,
        PerceivedSituation perceived,
        SalienceProfile salience,
        IReadOnlyList<Candidate> generated,
        IReadOnlyList<Rejection> rejected,
        IReadOnlyList<ScoreBreakdown> scored)
    {
        World = world;
        Actor = actor;
        Trigger = trigger;
        Agenda = agenda;
        Context = ctx;
        Perceived = perceived;
        Salience = salience;
        Generated = generated;
        Rejected = rejected;
        Scored = scored;

        // Ordinal id order, deliberately not rank order and deliberately not the salience order
        // Filters hands back. Either would tell whoever reads this which option the model prefers,
        // which is a utility score with the number filed off. Milestone 009, ruling 5.
        Available = scored
            .Select(s => s.Candidate)
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .ToList();
    }

    internal World World { get; }

    public Character Actor { get; }
    public ScheduledEvent Trigger { get; }
    public Agenda Agenda { get; }
    public DateTime At => World.Now;

    internal GeneratorContext Context { get; }
    internal PerceivedSituation Perceived { get; }
    internal SalienceProfile Salience { get; }
    internal IReadOnlyList<Candidate> Generated { get; }
    internal IReadOnlyList<Rejection> Rejected { get; }

    /// <summary>
    /// Every scored breakdown, best first. DEVELOPER-FACING — this is the utility calculation.
    /// </summary>
    public IReadOnlyList<ScoreBreakdown> Scored { get; }

    /// <summary>
    /// What is actually open to him: the candidates that survived redundancy, salience, knowledge,
    /// capability and access, in candidate-id order. This is the only member a player-facing surface
    /// may read.
    /// </summary>
    public IReadOnlyList<Candidate> Available { get; }

    /// <summary>Whether <see cref="Pipeline.Resolve"/> has already been run on this.</summary>
    public bool IsResolved { get; internal set; }

    /// <summary>Whether this candidate id is one he could actually take.</summary>
    public bool Allows(string candidateId)
    {
        foreach (var c in Available)
            if (string.Equals(c.Id, candidateId, StringComparison.Ordinal)) return true;
        return false;
    }
}

/// <summary>
/// The decision pipeline, one stage per step of the architecture document:
///
///   trigger -> update beliefs -> select agenda -> generate bounded candidates ->
///   reject unavailable -> score on perceived situation -> commit -> schedule reconsideration
///
/// The stages are kept separate rather than collapsed into one scoring function because the whole
/// point is being able to answer five different questions afterwards: what woke him, what mattered,
/// what occurred to him, what was actually open to him, and what he preferred.
///
/// Milestone 009 cut the sequence in two at the last of those. <see cref="Prepare"/> runs stages
/// 0-6; <see cref="Resolve"/> runs 7-8 and writes the record. <see cref="Deliberate"/> is their
/// composition and is what <see cref="Runner"/> still calls for every autonomous character, so
/// "NPC deliberation is unchanged" holds by construction rather than only by test.
/// </summary>
public static class Pipeline
{
    /// <summary>
    /// A full deliberation, choosing the option the character himself prefers.
    ///
    /// Every NPC takes this path, unchanged. It is literally <see cref="Prepare"/> followed by
    /// <see cref="Resolve"/> with no preferred candidate, which is what the single-method version
    /// always did.
    /// </summary>
    public static DecisionRecord Deliberate(World world, Character actor, ScheduledEvent trigger)
        => Resolve(Prepare(world, actor, trigger), null);

    /// <summary>
    /// Stages 0-6: take stock, perceive, situate, select the agenda, generate, filter, score.
    ///
    /// Everything here is either a read or a belief update the character makes on waking; nothing
    /// here commits him to anything, schedules anything, or writes a decision record. That is what
    /// makes it safe to stop between this and <see cref="Resolve"/> and wait for a person.
    /// </summary>
    public static PreparedDecision Prepare(World world, Character actor, ScheduledEvent trigger)
    {
        var rng = Rng.ForDecision(world.Seed, actor.Id, actor.DecisionCount++);

        // 0. Take stock. Conclusions he can draw from what he already holds, before he consults
        //    any of it — so that a thing worked out is available to the same stages as a thing
        //    seen, while staying distinguishable from it by source.
        Inference.Reconsider(world, actor, world.Now);

        // 1. Perceive. Beliefs only; traits shape confidence in second-hand claims.
        var perceived = Salience.Perceive(actor, world.Now);
        var salience = Salience.Build(actor, perceived);

        // 2. Situate the actor in the organisation.
        var org = world.Org;
        var office = org.OfficeFor(actor.Id);
        var assignment = org.Assignments
            .Where(a => a.RecipientId == actor.Id && a.Deadline >= world.Now)
            .OrderByDescending(a => a.IssuedAt)
            .FirstOrDefault();

        var knownPolicies = org.Policies
            .Where(p => perceived.KnowsPolicy(org.Id, p.Id))
            .ToList();

        // 3. What currently matters.
        var agenda = AgendaSelection.Select(world, actor, trigger, perceived, assignment);

        string? domain = agenda.Domain ?? office?.Domain;
        var ctx = new GeneratorContext(
            actor.View,
            perceived,
            agenda,
            world.Now,
            trigger,
            office,
            assignment,
            knownPolicies,
            SuperiorOf(world, actor),
            SubordinatesOf(world, actor),
            OrgMembersOf(world, actor),
            world.Reports.Where(r => r.SenderId == actor.Id).ToList(),
            world.Requests.Where(r => r.AskerId == actor.Id).ToList(),
            VisibleTargets(world, domain));

        // 4-5. Bounded generation, then salience/knowledge/capability/access rejection.
        var generated = Generators.GenerateAll(ctx);
        var filtered = Filters.Apply(ctx, generated, salience);

        // 6. Local utility over what remains. Note: perceived situation only, never World.
        //
        // The ranking is total — descending score, then candidate id — so it does not depend on the
        // order Filters happened to hand the survivors over in. That is what lets a player-facing
        // surface reorder Available without any risk of moving a score.
        var scored = filtered.Passed
            .Select(c => Utility.Score(c, actor.View, actor.Psychology, perceived, agenda, rng))
            .OrderByDescending(b => b.Total)
            .ThenBy(b => b.Candidate.Id, StringComparer.Ordinal)
            .ToList();

        return new PreparedDecision(
            world, actor, trigger, agenda, ctx, perceived, salience,
            generated, filtered.Rejected, scored);
    }

    /// <summary>
    /// Stages 7-8: commit, schedule what follows, and write the decision record.
    ///
    /// <paramref name="chosenCandidateId"/> null means "whichever he preferred", which is the
    /// autonomous path and the only one an NPC ever takes. A non-null id must name a candidate in
    /// <see cref="PreparedDecision.Available"/>; anything else throws rather than silently falling
    /// back to the top-ranked option, because a player-only action would be a second action
    /// implementation and actor parity forbids one.
    ///
    /// The chosen breakdown is taken by reference out of <see cref="PreparedDecision.Scored"/> —
    /// not rebuilt — so <c>DecisionRecord.Chosen</c> is still reference-equal to its entry in
    /// <c>Scored</c>, which is what the developer trace's "← chosen" marker keys on.
    /// </summary>
    public static DecisionRecord Resolve(PreparedDecision prepared, string? chosenCandidateId)
    {
        if (prepared.IsResolved)
            throw new SimulationInvariantException(
                $"the deliberation {prepared.Actor.Id} began at {prepared.At:O} has already been " +
                "resolved; a prepared decision commits exactly once.");

        var world = prepared.World;
        var actor = prepared.Actor;

        ScoreBreakdown? chosen;
        if (chosenCandidateId is null)
        {
            chosen = prepared.Scored.Count > 0 ? prepared.Scored[0] : null;
        }
        else
        {
            chosen = prepared.Scored
                .FirstOrDefault(s => string.Equals(s.Candidate.Id, chosenCandidateId, StringComparison.Ordinal));

            if (chosen is null)
                throw new SimulationInvariantException(
                    $"'{chosenCandidateId}' is not open to {actor.Id} at {prepared.At:O}. A choice " +
                    "must name one of the candidates that survived his own belief, salience, " +
                    "capability and access filters — there is no path that resolves an option the " +
                    "character could not have taken himself.");
        }

        prepared.IsResolved = true;

        // 7-8. Commit and schedule what follows.
        var reconsideration = new List<string>();
        string outcome = chosen is null
            ? "nothing was open to him"
            : Commit.Apply(world, actor, chosen.Candidate, prepared.Agenda, prepared.Context, reconsideration);

        actor.Execution.ReconsiderationTriggers.Clear();
        actor.Execution.ReconsiderationTriggers.AddRange(reconsideration);

        var record = new DecisionRecord(
            world.NextDecisionId(),
            world.Now,
            actor.Id,
            actor.Name,
            prepared.Trigger.Id,
            prepared.Trigger.Kind,
            prepared.Trigger.Cause,
            prepared.Agenda,
            prepared.Perceived.Used.ToList(),
            prepared.Generated,
            prepared.Rejected,
            prepared.Scored,
            chosen,
            outcome,
            reconsideration,
            prepared.Salience.Notes.Where(n => n.Length > 0).ToList());

        world.Decisions.Add(record);
        return record;
    }

    /// <summary>Lowest-ranked office holder strictly above this character in the same organisation.</summary>
    public static string? SuperiorOf(World world, Character actor)
    {
        if (!actor.IsOrgMember) return null;
        int mine = actor.Capabilities.Authority;
        return world.Characters.Values
            .Where(c => c.Social.OrganizationId == actor.Social.OrganizationId
                        && c.Capabilities.Authority > mine)
            .OrderBy(c => c.Capabilities.Authority)
            .ThenBy(c => c.Id, StringComparer.Ordinal)
            .FirstOrDefault()?.Id;
    }

    /// <summary>
    /// Everyone in the actor's organisation, himself excluded. Rank-blind on purpose: this is the
    /// social reach of the organisation, not its reporting hierarchy, and the two differ.
    /// </summary>
    public static IReadOnlyList<string> OrgMembersOf(World world, Character actor)
    {
        if (!actor.IsOrgMember) return Array.Empty<string>();
        return world.Characters.Values
            .Where(c => c.Social.OrganizationId == actor.Social.OrganizationId && c.Id != actor.Id)
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .Select(c => c.Id)
            .ToList();
    }

    public static IReadOnlyList<string> SubordinatesOf(World world, Character actor)
    {
        if (!actor.IsOrgMember) return Array.Empty<string>();
        int mine = actor.Capabilities.Authority;
        return world.Characters.Values
            .Where(c => c.Social.OrganizationId == actor.Social.OrganizationId
                        && c.Capabilities.Authority == mine - 1)
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .Select(c => c.Id)
            .ToList();
    }

    private static IReadOnlyList<string> VisibleTargets(World world, string? domain)
        => domain is null
            ? Array.Empty<string>()
            : world.BusinessesIn(domain).Select(b => b.Id).ToList();
}
