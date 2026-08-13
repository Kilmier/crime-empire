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

        // Stage 1 — salience. What occurs to them at all.
        var salient = new List<(Candidate Candidate, double Score)>();
        foreach (var c in candidates)
        {
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
        _ => c.ToString(),
    };
}
