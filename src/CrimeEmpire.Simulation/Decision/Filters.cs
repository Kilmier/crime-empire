namespace CrimeSim.Decision;

using CrimeSim.Domain;

public readonly record struct AttentionGroupKey(
    ActionKind Kind,
    StrategyKind Strategy,
    string TargetId);

public sealed record AttentionLeaf(Candidate Candidate, double Salience);

public sealed record AttentionAlternative(
    AttentionGroupKey? GroupKey,
    Candidate Anchor,
    double Salience,
    bool FocusedCancellation,
    IReadOnlyList<AttentionLeaf> Leaves)
{
    public bool IsGroup => GroupKey is not null;
    public string OrderingId => Anchor.Id;
}

public sealed record AttentionAllocation(
    bool GroupingActivated,
    IReadOnlyList<string> EligibleTargetIds,
    IReadOnlyList<AttentionAlternative> OrderedAlternatives,
    IReadOnlyList<AttentionAlternative> RetainedAlternatives,
    IReadOnlyList<AttentionLeaf> RetainedLeaves);

/// <summary>
/// Rejects options the character cannot conceive of, does not know enough to attempt, or cannot
/// perform. Every rejection carries its stage and a sentence of reason, because the rejections are
/// often more revealing than the choice — "he never considered it, because nobody had told him"
/// is the line that proves the simulation is belief-limited rather than merely claiming to be.
/// </summary>
public static class Filters
{
    private static bool FocusedCancellation(GeneratorContext ctx, Candidate c)
        => ctx.ReviewOperation is { } reviewed && c.IsOperationReview
            && c.Kind == ActionKind.AbandonStrategy && c.OperationOwnerId == reviewed.OwnerId
            && c.OperationSequence == reviewed.LocalSequence;

    private static bool EligibleKnownRefusal(Candidate c)
        => c.Generator == "FromResponsibility"
            && c.Kind == ActionKind.StartStrategy
            && c.Strategy == StrategyKind.SecureTribute
            && c.TargetId is not null
            && c.Method is CoercionMethod.Persuade or CoercionMethod.Threaten or CoercionMethod.Force
            && c.RequiredKnowledge.Contains(
                new Claim(ClaimKind.BusinessRefusesTribute, c.TargetId));

    public sealed record Result(
        List<Candidate> Passed,
        List<Rejection> Rejected,
        AttentionAllocation Attention);

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

            // Milestone 024's sixth correction, the one-operation-per-involved-character rule applied to
            // self-starts: a man currently carrying somebody else's delegated operation (he owns
            // nothing of his own, but CurrentExecution finds one naming him as DelegatedToId) has no
            // hands free, whatever the new candidate's own kind or target. Refused outright, never
            // generated-then-scored, the same shape Pipeline.AvailableToExecute's own delegation-
            // eligibility check already established for being offered as a delegate in the first
            // place — this is the mirror case, starting one of his own instead.
            if (ctx.Actor.Execution.Strategy is null && ctx.CurrentExecution is { } busyWith)
            {
                rejected.Add(new Rejection(c, RejectionStage.Redundancy,
                    $"{ctx.Actor.Name} already has his hands full with {busyWith.Label}"));
                redundant.Add(c.Id);
                continue;
            }

            // Exclude this owner's existing target, not other actors' unknown operations.
            // Supervision itself does not occupy the owner's personal execution slot.
            if (ctx.Actor.Execution.Operations.Any(s => s.Kind == c.Strategy && s.TargetId == c.TargetId)
                && c.Strategy != StrategyKind.ConcealIncident)
            {
                rejected.Add(new Rejection(c, RejectionStage.Redundancy,
                    $"{ctx.Actor.Name} already has an operation on that target"));
                redundant.Add(c.Id);
                continue;
            }

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

        // Milestone 029's branch is decided after redundancy has established which target proposals
        // are genuinely eligible, but before salience can erase every method for one of them. It
        // consults only belief-derived generated candidates. The generator admits at most two known
        // refusal targets, so exactly two means this one bounded occasion and nothing broader.
        var eligibleKnownRefusals = candidates
            .Where(c => !redundant.Contains(c.Id) && EligibleKnownRefusal(c))
            .ToList();
        var eligibleTargetIds = eligibleKnownRefusals
            .Select(c => c.TargetId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        bool groupingActivated = eligibleTargetIds.Count == 2;

        // Stage 1 — salience. What occurs to them at all.
        var salient = new List<(Candidate Candidate, double Score)>();
        foreach (var c in candidates)
        {
            if (redundant.Contains(c.Id)) continue;

            double s = salience.For(c);
            if (FocusedCancellation(ctx, c)) s = Math.Max(s, SalienceProfile.Threshold);
            if (s < SalienceProfile.Threshold)
                rejected.Add(new Rejection(c, RejectionStage.Salience, $"it did not occur to {ctx.Actor.Name} (salience {s:0.00})"));
            else
                salient.Add((c, s));
        }

        var flatOrdered = salient
            .OrderByDescending(x => FocusedCancellation(ctx, x.Candidate))
            .ThenByDescending(x => x.Score)
            .ThenBy(x => x.Candidate.Id, StringComparer.Ordinal)
            .ToList();

        List<(Candidate Candidate, double Score)> considered;
        AttentionAllocation attention;

        if (!groupingActivated)
        {
            // Preserve the original pipeline literally when the exact activation predicate is
            // false: same ordering, truncation, rejection text/order and late feasibility pass.
            foreach (var extra in flatOrdered.Skip(SalienceProfile.MaxCandidates))
                rejected.Add(new Rejection(extra.Candidate, RejectionStage.Salience,
                    $"crowded out — only {SalienceProfile.MaxCandidates} options held his attention"));

            considered = flatOrdered.Take(SalienceProfile.MaxCandidates).ToList();
            var ordered = flatOrdered
                .Select(x => Singleton(ctx, x.Candidate, x.Score))
                .ToList();
            attention = new AttentionAllocation(
                false,
                eligibleTargetIds,
                ordered,
                ordered.Take(SalienceProfile.MaxCandidates).ToList(),
                considered.Select(x => new AttentionLeaf(x.Candidate, x.Score)).ToList());
        }
        else
        {
            var eligibleIds = eligibleKnownRefusals
                .Select(c => c.Id)
                .ToHashSet(StringComparer.Ordinal);
            var alternatives = new List<AttentionAlternative>();

            // Groups are made only from methods that independently survived salience. A target
            // whose methods all fell below threshold contributes no empty top-level alternative.
            foreach (var leaves in flatOrdered
                         .Where(x => eligibleIds.Contains(x.Candidate.Id))
                         .GroupBy(x => x.Candidate.TargetId!, StringComparer.Ordinal))
            {
                var orderedLeaves = leaves
                    .Select(x => new AttentionLeaf(x.Candidate, x.Score))
                    .OrderByDescending(x => x.Salience)
                    .ThenBy(x => x.Candidate.Id, StringComparer.Ordinal)
                    .ToList();
                if (orderedLeaves.Count == 0) continue;

                var anchor = orderedLeaves[0];
                alternatives.Add(new AttentionAlternative(
                    new AttentionGroupKey(
                        ActionKind.StartStrategy,
                        StrategyKind.SecureTribute,
                        leaves.Key),
                    anchor.Candidate,
                    anchor.Salience,
                    false,
                    orderedLeaves));
            }

            alternatives.AddRange(flatOrdered
                .Where(x => !eligibleIds.Contains(x.Candidate.Id))
                .Select(x => Singleton(ctx, x.Candidate, x.Score)));

            var ordered = alternatives
                .OrderByDescending(x => x.FocusedCancellation)
                .ThenByDescending(x => x.Salience)
                .ThenBy(x => x.OrderingId, StringComparer.Ordinal)
                .ToList();
            var retained = ordered.Take(SalienceProfile.MaxCandidates).ToList();

            foreach (var extra in ordered.Skip(SalienceProfile.MaxCandidates))
            foreach (var leaf in extra.Leaves)
                rejected.Add(new Rejection(leaf.Candidate, RejectionStage.Salience,
                    $"crowded out — only {SalienceProfile.MaxCandidates} top-level options held his attention"));

            var retainedIds = retained
                .SelectMany(x => x.Leaves)
                .Select(x => x.Candidate.Id)
                .ToHashSet(StringComparer.Ordinal);

            // Grouping changes membership only. Restore the original concrete flat order before
            // late feasibility and Utility.Score consume it (and therefore before RNG draws).
            considered = flatOrdered
                .Where(x => retainedIds.Contains(x.Candidate.Id))
                .ToList();
            attention = new AttentionAllocation(
                true,
                eligibleTargetIds,
                ordered,
                retained,
                considered.Select(x => new AttentionLeaf(x.Candidate, x.Score)).ToList());
        }

        var passed = new List<Candidate>();

        // Stages 2-4 — knowledge, capability, access.
        foreach (var (c, _) in considered)
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

        return new Result(passed, rejected, attention);
    }

    private static AttentionAlternative Singleton(GeneratorContext ctx, Candidate c, double salience)
        => new(
            null,
            c,
            salience,
            FocusedCancellation(ctx, c),
            new[] { new AttentionLeaf(c, salience) });

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
