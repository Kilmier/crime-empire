namespace CrimeSim.Strategy;

using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;

/// <summary>
/// Parameterised multi-step procedures. Authored shape, variable target and method — deliberately
/// not free-form planning. Each strategy owns its steps, its resolution, its completion condition
/// and the traces it leaves behind.
///
/// Note the division of labour with the decision pipeline: the pipeline decides *whether* to run a
/// strategy; this file decides what happens when it runs, using objective world state. Resolution
/// is allowed to disagree with what the actor expected, and usually should.
/// </summary>
public static class Strategies
{
    public static readonly string[] TributeSteps = { "make the approach", "put the demand", "press or accept", "collect" };
    public static readonly string[] ConcealSteps = { "quiet the witnesses", "tidy the paperwork" };
    public static readonly string[] InvestigateSteps = { "check the records", "canvass the street", "put on surveillance" };

    public static string[] StepsFor(StrategyKind k) => k switch
    {
        StrategyKind.SecureTribute => TributeSteps,
        StrategyKind.ConcealIncident => ConcealSteps,
        _ => InvestigateSteps,
    };

    public static TimeSpan StepInterval(StrategyKind k) => k switch
    {
        StrategyKind.SecureTribute => TimeSpan.FromDays(3),
        StrategyKind.ConcealIncident => TimeSpan.FromDays(2),
        _ => TimeSpan.FromDays(4),
    };

    public static void ScheduleNextStep(World world, Character actor, StrategyInstance s, string cause)
    {
        var ev = world.Queue.Schedule(
            world.Now + StepInterval(s.Kind),
            EventKind.StrategyStep,
            s.DelegatedToId ?? actor.Id,
            cause,
            new EventPayload
            {
                Strategy = s.Kind,
                StepIndex = s.StepIndex,
                TargetId = s.TargetId,
            });
        s.PendingStepEventId = ev.Id;
    }

    /// <summary>Runs one step of the actor's current strategy and schedules what follows.</summary>
    public static void Advance(World world, Character actor, ScheduledEvent ev, Rng rng)
    {
        // The step may belong to the delegator even though the delegate is executing it.
        var owner = world.Characters.Values.FirstOrDefault(c =>
            c.Execution.Strategy is { } st &&
            (c.Id == actor.Id || st.DelegatedToId == actor.Id) &&
            st.Kind == ev.Payload.Strategy);

        if (owner?.Execution.Strategy is not { } s) return;

        var steps = StepsFor(s.Kind);
        if (s.StepIndex >= steps.Length)
        {
            Complete(world, owner, s, "ran out of steps", rng);
            return;
        }

        string stepName = steps[s.StepIndex];
        s.StepIndex++;

        switch (s.Kind)
        {
            case StrategyKind.SecureTribute:
                AdvanceTribute(world, owner, actor, s, stepName, rng);
                break;
            case StrategyKind.ConcealIncident:
                AdvanceConceal(world, owner, actor, s, stepName, rng);
                break;
            case StrategyKind.InvestigateIncident:
                AdvanceInvestigation(world, owner, actor, s, stepName, rng);
                break;
        }
    }

    // ------------------------------------------------------------------ tribute
    private static void AdvanceTribute(
        World world, Character owner, Character executor, StrategyInstance s, string step, Rng rng)
    {
        var business = s.TargetId is null ? null : world.Businesses.GetValueOrDefault(s.TargetId);
        if (business is null)
        {
            Complete(world, owner, s, "the target no longer exists", rng);
            return;
        }

        switch (s.StepIndex)
        {
            case 1: // approached — and formed a first impression of how hard this will be
            {
                world.Record("approach", executor.Id, business.Id,
                    $"{executor.Name} came by {business.Name}");

                // A read on the target, not a reading of the variable: he sizes the place up, gets
                // it roughly right, and may be wrong at the margin.
                double read = Math.Clamp(1.0 - business.Resistance + rng.Range(-0.2, 0.2), 0, 1);
                var impression = new Claim(ClaimKind.TargetIsVulnerable, business.Id);
                executor.Cognition.Learn(impression, Stance.Believes, read, SourceKind.Direct, executor.Id, world.Now);
                if (owner.Id != executor.Id)
                    owner.Cognition.Learn(impression, Stance.Believes, read * 0.8, SourceKind.Report, executor.Id, world.Now);

                ScheduleNextStep(world, owner, s, $"{s.Label}: {step} done, next step due");
                return;
            }

            case 2: // the demand lands — the owner now has a decision to make
                world.Record("demand", executor.Id, business.Id,
                    $"{executor.Name} put a demand to {business.Name}");
                world.Get(business.OwnerId).Cognition.Learn(
                    new Claim(ClaimKind.BusinessRefusesTribute, business.Id),
                    Stance.Knows, 1.0, SourceKind.Direct, business.OwnerId, world.Now);

                world.Queue.Schedule(world.Now, EventKind.Incident, business.OwnerId,
                    $"{executor.Name} demanded payment",
                    new EventPayload { TargetId = executor.Id, Note = "tribute-demanded" });

                ScheduleNextStep(world, owner, s, $"{s.Label}: awaiting an answer");
                return;

            case 3: // press or accept
            {
                // The owner already answered the demand. Whether he pays is his decision, made by
                // the same pipeline everyone else uses — not a roll made on his behalf.
                if (business.PayingTribute)
                {
                    world.Record("tribute-agreed", executor.Id, business.Id,
                        $"{business.Name} came to terms with {executor.Name}");
                    executor.Cognition.Learn(new Claim(ClaimKind.TributeCollected, business.Id),
                        Stance.Knows, 1.0, SourceKind.Direct, executor.Id, world.Now);
                    ScheduleNextStep(world, owner, s, $"{s.Label}: collection due");
                    return;
                }

                // He said no. Applying the method changes the grocer's situation, and then he
                // decides again — coercion works through his decision, not around it.
                if (!s.PressureApplied && s.Method != CoercionMethod.Persuade)
                {
                    s.PressureApplied = true;
                    var marco = world.Get(business.OwnerId);

                    if (s.Method == CoercionMethod.Threaten)
                    {
                        marco.Social.Toward(executor.Id).Fear += 0.35;
                        world.Record("threat", executor.Id, business.Id,
                            $"{executor.Name} made {business.Name} a promise about what came next");
                    }
                    else
                    {
                        marco.Social.Toward(executor.Id).Fear += 0.55;
                        ResolveViolence(world, owner, executor, s, business, rng);
                    }

                    world.Queue.Schedule(world.Now.AddHours(6), EventKind.Incident, marco.Id,
                        $"after {executor.Name}, the demand was still on the table",
                        new EventPayload { TargetId = executor.Id, Note = "tribute-demanded" });

                    // Step back one, so the next step re-enters this check and finds out whether
                    // the pressure told.
                    s.StepIndex = 2;
                    ScheduleNextStep(world, owner, s, $"{s.Label}: seeing whether the pressure told");
                    return;
                }

                Blocked(world, owner, s, business, executor);
                return;
            }

            default: // collected
                owner.Capabilities.Cash += business.MonthlyRevenue * 0.2;
                owner.Motivations.AddPressure(PressureKind.RevenueShortfall, -0.6);
                world.Org.AdjustCondition(OrgCondition.RevenueLoss, -0.5);
                world.Record("tribute-collected", executor.Id, business.Id,
                    $"{executor.Name} collected from {business.Name}");

                // He has seen the money arrive, so he no longer believes the place is holding out.
                // Without this the objective changes but the belief driving it does not, and he
                // starts the whole thing over again on a target that is already paying.
                foreach (var who in new[] { owner, executor })
                    who.Cognition.Learn(new Claim(ClaimKind.BusinessRefusesTribute, business.Id),
                        Stance.Rejects, 0.9, SourceKind.Direct, who.Id, world.Now);

                CloseAssignment(world, owner, s);
                Complete(world, owner, s, "the money started arriving", rng);
                return;
        }
    }

    /// <summary>The target held out. Records the failure so continuation can lose value to evidence.</summary>
    private static void Blocked(World world, Character owner, StrategyInstance s, Business business, Character executor)
    {
        s.FailedAttempts++;

        // Rewind to the confrontation step. Carrying on means trying again, not skipping ahead to
        // collect money nobody agreed to pay.
        s.StepIndex = 2;
        s.PressureApplied = false;

        world.Record("tribute-refused", business.OwnerId, executor.Id,
            $"{business.Name} still would not pay ({s.Method.ToString().ToLowerInvariant()}, attempt {s.FailedAttempts})");
        owner.Motivations.AddPressure(PressureKind.RevenueShortfall, 0.2);
        world.Queue.Schedule(world.Now, EventKind.StrategyBlocked, owner.Id,
            $"{business.Name} held out against {s.Method.ToString().ToLowerInvariant()}",
            new EventPayload { TargetId = business.Id, Strategy = s.Kind });
    }

    /// <summary>An assignment that has been satisfied stops generating obligations.</summary>
    private static void CloseAssignment(World world, Character owner, StrategyInstance s)
    {
        if (s.AssignmentId is not { } id) return;
        world.Org.Assignments.RemoveAll(a => a.Id == id);
        owner.Motivations.Responsibilities.RemoveAll(r => r.Id == $"assignment:{id}");
        owner.Execution.Commitments.RemoveAll(c => c.Id == $"assignment:{id}");
    }

    /// <summary>
    /// Force resolves the same way regardless of who ordered it, and leaves traces regardless of
    /// whether anyone is currently looking. Discovery is a separate question from occurrence.
    /// </summary>
    private static void ResolveViolence(
        World world, Character owner, Character executor, StrategyInstance s, Business business, Rng rng)
    {
        business.Damaged = true;
        business.Resistance = Math.Max(0, business.Resistance - 0.3);

        var ev = world.Record("violence", executor.Id, business.Id,
            $"{executor.Name} put hands on {business.Name}",
            new Trace("injury", $"{business.Name} was visibly worked over", business.DistrictId, 0.75),
            new Trace("witness", "people on the street saw it", business.DistrictId, 0.55),
            new Trace("damage", "the front of the shop was wrecked", business.DistrictId, 0.65));

        var violenceClaim = new Claim(ClaimKind.PersonUsedViolence, executor.Id, business.Id, ev.Id);
        var witnessClaim = new Claim(ClaimKind.WitnessSawIncident, business.Id, executor.Id, ev.Id);

        // Participants know what they themselves did. This is direct observation, not reporting —
        // the report/rumour/distortion layer is step 1b.
        executor.Cognition.Learn(violenceClaim, Stance.Knows, 1.0, SourceKind.Direct, executor.Id, world.Now);
        executor.Cognition.Learn(witnessClaim, Stance.Believes, 0.6, SourceKind.Direct, executor.Id, world.Now);
        world.Get(business.OwnerId).Cognition.Learn(violenceClaim, Stance.Knows, 1.0, SourceKind.Direct, business.OwnerId, world.Now);

        if (owner.Id != executor.Id)
            owner.Cognition.Learn(violenceClaim, Stance.Knows, 0.9, SourceKind.Direct, executor.Id, world.Now);

        owner.Motivations.AddPressure(PressureKind.LegalExposure, 0.3);
        executor.Motivations.AddPressure(PressureKind.LegalExposure, 0.35);

        if (s.BreachedPolicyId is not null)
        {
            world.Org.AdjustCondition(OrgCondition.LeadershipInstability, 0.2);

            // He ordered it. A man does not have to discover that he went outside his own boss's
            // rule — he is the one who decided to. This is what gives him something to conceal
            // later; without it the person who ordered the breach holds no claim naming himself
            // and can only ever report candidly, while the subordinate who carried it out takes
            // all of the exposure.
            owner.Cognition.Learn(
                new Claim(ClaimKind.PersonBreachedPolicy, owner.Id, s.BreachedPolicyId, ev.Id),
                Stance.Knows, 1.0, SourceKind.Direct, owner.Id, world.Now);
        }

        // Who gets a chance to notice, and on what terms. Collected before scheduling so that one
        // event yields at most one opportunity per person: a character who qualifies twice over
        // (the boss, who is both owed a report about the breach and works the same district) would
        // otherwise get two independent rolls, notice twice, and deliberate twice at the same
        // instant on the same news.
        var opportunities = new Dictionary<string, (double Discoverability, Claim[] Claims)>(StringComparer.Ordinal);

        void Offer(string? id, double discoverability, params Claim[] claims)
        {
            if (id is null || id == executor.Id || id == owner.Id) return;
            // Better access wins. Two routes to the same news is not twice the chance of hearing
            // it; it is one chance, on the better of the two terms.
            if (opportunities.TryGetValue(id, out var existing) && existing.Discoverability >= discoverability) return;
            opportunities[id] = (discoverability, claims);
        }

        // Two different reasons to notice, and they are not the same reason. Someone whose job is
        // looking finds out because they went looking. Someone who works the same street finds out
        // because it happened where they are — proximity, not skill. Without the second, the only
        // people who can ever contradict an account are investigators, and an organisation becomes
        // a place where nobody sees anything.
        foreach (var c in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            if (c.Capabilities[Skill.Investigation] >= 0.4)
                Offer(c.Id, 0.6, witnessClaim);
            else if (c.IsOrgMember && c.Capabilities.Districts.Contains(business.DistrictId))
                Offer(c.Id, 0.35, witnessClaim, violenceClaim);
        }

        // The boss is owed an account of a breach by virtue of the office, which is better access
        // than merely working the district.
        if (s.BreachedPolicyId is not null)
            Offer(world.Org.BossId, 0.5,
                violenceClaim,
                new Claim(ClaimKind.PersonBreachedPolicy, owner.Id, s.BreachedPolicyId, ev.Id));

        foreach (var (id, o) in opportunities.OrderBy(k => k.Key, StringComparer.Ordinal))
            ScheduleObservation(world, id, ev, o.Discoverability, o.Claims);
    }

    private static void ScheduleObservation(
        World world, string? observerId, WorldEvent ev, double discoverability, IReadOnlyList<Claim> claims)
    {
        if (observerId is null) return;
        world.Queue.Schedule(
            world.Now + TimeSpan.FromDays(1),
            EventKind.ObservationOpportunity,
            observerId,
            $"he may notice what was left behind at {ev.TargetId}",
            new EventPayload
            {
                RelatedEventId = ev.Id,
                TargetId = ev.TargetId,
                Claims = claims,
                Discoverability = discoverability,
            });
    }

    // ------------------------------------------------------------------ concealment
    private static void AdvanceConceal(
        World world, Character owner, Character executor, StrategyInstance s, string step, Rng rng)
    {
        double discretion = executor.Capabilities[Skill.Discretion];
        bool clean = discretion + rng.Range(-0.15, 0.15) > 0.45;

        world.Record("conceal", executor.Id, s.TargetId,
            $"{executor.Name} tried to {step} — {(clean ? "tidily" : "clumsily")}");

        if (clean)
            owner.Motivations.AddPressure(PressureKind.LegalExposure, -0.2);
        else
            owner.Motivations.AddPressure(PressureKind.LegalExposure, 0.1);

        if (s.StepIndex >= ConcealSteps.Length)
            Complete(world, owner, s, clean ? "the loose ends were tied off" : "the cleanup made things worse", rng);
        else
            ScheduleNextStep(world, owner, s, $"{s.Label}: {step} done");
    }

    // ------------------------------------------------------------------ investigation
    private static void AdvanceInvestigation(
        World world, Character owner, Character executor, StrategyInstance s, string step, Rng rng)
    {
        double skill = executor.Capabilities[Skill.Investigation];
        bool found = skill + rng.Range(-0.2, 0.2) > 0.55;

        world.Record("investigate", executor.Id, s.TargetId,
            $"{executor.Name} went to {step} at {s.TargetId}");

        if (found && s.TargetId is not null)
        {
            // She learns who, only if a witness claim naming a person is already in her head.
            var lead = owner.Cognition.OfKind(ClaimKind.WitnessSawIncident)
                .FirstOrDefault(r => r.Claim.Subject == s.TargetId && r.Claim.Object.Length > 0);

            if (lead is not null)
            {
                owner.Cognition.Learn(
                    new Claim(ClaimKind.PersonUsedViolence, lead.Claim.Object, s.TargetId, lead.Claim.EventId),
                    Stance.Suspects, 0.55, SourceKind.Inference, owner.Id, world.Now);

                // The suspect's own risk rises only if he comes to believe he is being looked at.
                var suspect = world.Find(lead.Claim.Object);
                if (suspect is not null)
                    ScheduleObservation(
                        world, suspect.Id,
                        new WorldEvent(0, world.Now, "surveillance", owner.Id, suspect.Id, "police interest", Array.Empty<Trace>()),
                        0.4,
                        new[] { new Claim(ClaimKind.PoliceInvestigating, suspect.Id) });
            }
        }

        if (s.StepIndex < InvestigateSteps.Length)
        {
            ScheduleNextStep(world, owner, s, $"{s.Label}: {step} done");
            return;
        }

        bool named = s.TargetId is not null && owner.Cognition
            .OfKind(ClaimKind.PersonUsedViolence).Any(v => v.Claim.Object == s.TargetId);

        if (!named && s.TargetId is not null)
        {
            // The trail went cold. She stops treating the street talk as something to act on —
            // otherwise she reopens the same dead case every time the calendar nudges her.
            foreach (var stale in owner.Cognition.OfKind(ClaimKind.WitnessSawIncident)
                         .Where(r => r.Claim.Subject == s.TargetId).ToList())
                owner.Cognition.Learn(stale.Claim, Stance.Doubts, stale.Confidence * 0.5,
                    SourceKind.Direct, owner.Id, world.Now);
        }

        Complete(world, owner, s, named ? "the canvass turned up a name" : "the trail went cold", rng);
    }

    // ------------------------------------------------------------------ completion
    public static void Complete(World world, Character owner, StrategyInstance s, string why, Rng rng)
    {
        world.Queue.Schedule(world.Now, EventKind.StrategyComplete, owner.Id,
            $"{s.Label} finished: {why}",
            new EventPayload { Strategy = s.Kind, TargetId = s.TargetId, Note = why });

        owner.Execution.Strategy = null;
        owner.Execution.Intention = null;
        owner.Execution.Commitments.RemoveAll(c => c.Id.StartsWith("strategy:", StringComparison.Ordinal));
    }
}
