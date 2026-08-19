namespace CrimeSim.Decision;

using CrimeSim.Domain;

/// <summary>
/// Rejects options the character cannot conceive of, does not know enough to attempt, or cannot
/// perform. Every rejection carries its stage and a sentence of reason, because the rejections are
/// often more revealing than the choice — "he never considered it, because nobody had told him"
/// is the line that proves the simulation is belief-limited rather than merely claiming to be.
/// </summary>
public static class Filters
{
    public sealed record Result(
        List<Candidate> Passed,
        List<Rejection> Rejected);

    public static Result Apply(GeneratorContext ctx, IReadOnlyList<Candidate> candidates, SalienceProfile salience)
    {
        var rejected = new List<Rejection>();

        // Stage 0 — redundancy. Whether this is even worth doing at all, decided before anything
        // else is weighed. Placed ahead of salience so a duplicate that would otherwise rank highly
        // cannot take one of the bounded candidate slots and crowd out a genuinely different option.
        var redundant = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in candidates)
        {
            if (c.Kind != ActionKind.StartStrategy) continue;

            // ConcealIncident is identified by which incident it is about, never by (Kind,
            // TargetId). A location is not an incident: two separate beatings at the same shop are
            // two different things to cover up, and a (Kind, TargetId) match to the running
            // instance would wrongly treat the second as a restart of the first, blocking a
            // legitimate replacement. This branch owns all of ConcealIncident's redundancy
            // reasoning and never falls through to the generic check below.
            if (c.Strategy == StrategyKind.ConcealIncident)
            {
                // Fail closed. A ConcealIncident candidate with no incident attached cannot be
                // checked against AttemptedConcealments at all, which would let it start without
                // ever being recorded — the exact gap the MVP rule exists to close. Refuse it
                // outright rather than let it through unrecorded; Commit.StartStrategy repeats this
                // guard as a throw, so the two together make an unrecorded start structurally
                // impossible even if a future candidate reaches Commit some other way.
                if (c.AboutIncident is not { } incident)
                {
                    rejected.Add(new Rejection(c, RejectionStage.Redundancy,
                        $"{ctx.Actor.Name} has no specific incident in mind to cover up"));
                    redundant.Add(c.Id);
                    continue;
                }

                // MVP rule, not a permanent design commitment: one attempt at concealing a given
                // incident, whether the attempt is still running or has already finished. Recording
                // the incident rather than the running instance is what lets this cover the
                // completed state too — nothing is "running" any more by then. See
                // docs/CURRENT_MILESTONE.md.
                if (ctx.Actor.Execution.AttemptedConcealments.Contains(incident))
                {
                    rejected.Add(new Rejection(c, RejectionStage.Redundancy,
                        $"{ctx.Actor.Name} has already had a go at covering that up"));
                    redundant.Add(c.Id);
                }
                continue;
            }

            if (ctx.Actor.Execution.Strategy is { } running
                && running.Kind == c.Strategy && running.TargetId == c.TargetId)
            {
                rejected.Add(new Rejection(c, RejectionStage.Redundancy,
                    $"{ctx.Actor.Name} is already handling that"));
                redundant.Add(c.Id);
            }
        }

        // Stage 1 — salience. What occurs to them at all.
        var salient = new List<(Candidate Candidate, double Score)>();
        foreach (var c in candidates)
        {
            if (redundant.Contains(c.Id)) continue;

            double s = salience.For(c);
            if (s < SalienceProfile.Threshold)
                rejected.Add(new Rejection(c, RejectionStage.Salience, $"it did not occur to {ctx.Actor.Name} (salience {s:0.00})"));
            else
                salient.Add((c, s));
        }

        var considered = salient
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Candidate.Id, StringComparer.Ordinal)
            .ToList();

        foreach (var extra in considered.Skip(SalienceProfile.MaxCandidates))
            rejected.Add(new Rejection(extra.Candidate, RejectionStage.Salience,
                $"crowded out — only {SalienceProfile.MaxCandidates} options held his attention"));

        var passed = new List<Candidate>();

        // Stages 2-4 — knowledge, capability, access.
        foreach (var (c, _) in considered.Take(SalienceProfile.MaxCandidates))
        {
            var missing = c.RequiredKnowledge.FirstOrDefault(k => !ctx.Perceived.Holds(k));
            if (c.RequiredKnowledge.Count > 0 && !c.RequiredKnowledge.All(ctx.Perceived.Holds))
            {
                rejected.Add(new Rejection(c, RejectionStage.Knowledge,
                    $"{ctx.Actor.Name} does not know that {Describe(missing)}"));
                continue;
            }

            if (c.RequiredSkill is { } skill && ctx.Actor.Capabilities[skill] < c.RequiredSkillLevel)
            {
                rejected.Add(new Rejection(c, RejectionStage.Capability,
                    $"his {skill.ToString().ToLowerInvariant()} is not up to it"));
                continue;
            }

            if (c.RequiredCrew > ctx.Actor.Capabilities.Crew)
            {
                rejected.Add(new Rejection(c, RejectionStage.Capability,
                    $"he does not have {c.RequiredCrew} people free"));
                continue;
            }

            if (c.RequiredAuthority > ctx.Actor.Capabilities.Authority)
            {
                rejected.Add(new Rejection(c, RejectionStage.Access,
                    "he has no standing to do that"));
                continue;
            }

            if (!ctx.Actor.Capabilities.CanReach(c.Domain))
            {
                rejected.Add(new Rejection(c, RejectionStage.Access,
                    $"he has no reach in {c.Domain}"));
                continue;
            }

            passed.Add(c);
        }

        return new Result(passed, rejected);
    }

    private static string Describe(Claim c) => c.Kind switch
    {
        ClaimKind.BusinessRefusesTribute => $"{c.Subject} is holding back payments",
        ClaimKind.WitnessSawIncident => $"anything happened at {c.Subject}",
        ClaimKind.PersonUsedViolence => $"{c.Subject} attacked {c.Object}",
        ClaimKind.PoliceInvestigating => $"police are looking at {c.Subject}",
        ClaimKind.PolicyIssued => $"{c.Subject} has a standing rule about {c.Object}",
        ClaimKind.TargetIsVulnerable => $"{c.Subject} is in a weak position",
        ClaimKind.UnattributedShortfall => $"anything in the {c.Subject} is still not right",
        _ => c.ToString(),
    };
}
