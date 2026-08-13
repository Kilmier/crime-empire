namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;
using CrimeSim.Strategy;

/// <summary>
/// The decision pipeline, one stage per step of the architecture document:
///
///   trigger -> update beliefs -> select agenda -> generate bounded candidates ->
///   reject unavailable -> score on perceived situation -> commit -> schedule reconsideration
///
/// The stages are kept separate rather than collapsed into one scoring function because the whole
/// point is being able to answer five different questions afterwards: what woke him, what mattered,
/// what occurred to him, what was actually open to him, and what he preferred.
/// </summary>
public static class Pipeline
{
    public static DecisionRecord Deliberate(World world, Character actor, ScheduledEvent trigger)
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
            VisibleTargets(world, domain));

        // 4-5. Bounded generation, then salience/knowledge/capability/access rejection.
        var generated = Generators.GenerateAll(ctx);
        var filtered = Filters.Apply(ctx, generated, salience);

        // 6. Local utility over what remains. Note: perceived situation only, never World.
        var scored = filtered.Passed
            .Select(c => Utility.Score(c, actor.View, actor.Psychology, perceived, agenda, rng))
            .OrderByDescending(b => b.Total)
            .ThenBy(b => b.Candidate.Id, StringComparer.Ordinal)
            .ToList();

        var chosen = scored.FirstOrDefault();

        // 7-8. Commit and schedule what follows.
        var reconsideration = new List<string>();
        string outcome = chosen is null
            ? "nothing was open to him"
            : Commit.Apply(world, actor, chosen.Candidate, agenda, ctx, reconsideration);

        actor.Execution.ReconsiderationTriggers.Clear();
        actor.Execution.ReconsiderationTriggers.AddRange(reconsideration);

        var record = new DecisionRecord(
            world.NextDecisionId(),
            world.Now,
            actor.Id,
            actor.Name,
            trigger.Id,
            trigger.Kind,
            trigger.Cause,
            agenda,
            perceived.Used.ToList(),
            generated,
            filtered.Rejected,
            scored,
            chosen,
            outcome,
            reconsideration,
            salience.Notes.Where(n => n.Length > 0).ToList());

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
