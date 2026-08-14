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
                // He went and looked at the place. Discovery rather than witnessing: what he formed
                // a view about is a standing condition of the shop, not an event he attended.
                executor.Cognition.Learn(impression, Stance.Believes, read, SourceKind.Discovery, executor.Id, world.Now);

                // And the man who sent him has it from the man who went — an account from someone
                // who was there, which is nearer the thing than a filed report but is still his
                // word for it.
                if (owner.Id != executor.Id)
                    owner.Cognition.Learn(impression, Stance.Believes, read * 0.8,
                        SourceKind.FirstHandTestimony, executor.Id, world.Now);

                ScheduleNextStep(world, owner, s, $"{s.Label}: {step} done, next step due");
                return;
            }

            case 2: // the demand lands — the owner now has a decision to make
                world.Record("demand", executor.Id, business.Id,
                    $"{executor.Name} put a demand to {business.Name}");
                // His own shop and his own decision not to pay. Nobody had to tell him.
                world.Get(business.OwnerId).Cognition.Learn(
                    new Claim(ClaimKind.BusinessRefusesTribute, business.Id),
                    Stance.Knows, 1.0, SourceKind.Participant, business.OwnerId, world.Now);

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
                    // He struck the deal. His own act, not news that reached him.
                    executor.Cognition.Learn(new Claim(ClaimKind.TributeCollected, business.Id),
                        Stance.Knows, 1.0, SourceKind.Participant, executor.Id, world.Now);
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
                // Split, because the two men came to it differently. The one who collected was in
                // it; the one who sent him finds the takings arriving. Both are unmediated and
                // neither is the other's account, but they are not the same acquisition and the
                // record should not say they are.
                var collected = new Claim(ClaimKind.BusinessRefusesTribute, business.Id);
                executor.Cognition.Learn(collected, Stance.Rejects, 0.9,
                    SourceKind.Participant, executor.Id, world.Now);
                if (owner.Id != executor.Id)
                    owner.Cognition.Learn(collected, Stance.Rejects, 0.9,
                        SourceKind.Discovery, owner.Id, world.Now);

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

        // The man who did it. Nothing to discover and nobody to believe.
        executor.Cognition.Learn(violenceClaim, Stance.Knows, 1.0,
            SourceKind.Participant, executor.Id, world.Now);

        // That people saw him is a guess, not an observation: he did not turn round and check the
        // street, and he is reasoning from having done it somewhere public. Recording it as
        // something he established would make his fear of witnesses unshakeable, when it is
        // precisely the sort of thing he should be able to be wrong about in either direction.
        executor.Cognition.Learn(witnessClaim, Stance.Believes, 0.6,
            SourceKind.Inference, executor.Id, world.Now);

        // The grocer watched it happen to him. Witness rather than Participant: he knows what was
        // done and who did it, and specifically does not inherit the one thing Participant carries,
        // which is knowledge of who gave the order.
        world.Get(business.OwnerId).Cognition.Learn(violenceClaim, Stance.Knows, 1.0,
            SourceKind.Witness, business.OwnerId, world.Now);

        // The man who sent him hears that it was done. He was not there, and this is his executor's
        // word — testimony from a participant, which is nearer the event than a filed report but is
        // still an account he could come to doubt. Keeping this apart from his own authorship
        // below is the distinction this milestone exists for.
        if (owner.Id != executor.Id)
            owner.Cognition.Learn(violenceClaim, Stance.Knows, 0.9,
                SourceKind.FirstHandTestimony, executor.Id, world.Now);

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
            //
            // Participant, and deliberately not the same category as the line above that records
            // him hearing the beating was done. Those are two separate acquisitions and only one
            // of them can be argued out of him: he can be talked into doubting that Tommy went
            // through with it; he cannot be talked into doubting that he gave the order.
            owner.Cognition.Learn(
                new Claim(ClaimKind.PersonBreachedPolicy, owner.Id, s.BreachedPolicyId, ev.Id),
                Stance.Knows, 1.0, SourceKind.Participant, owner.Id, world.Now);
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

        // The boss has better access to what happened on his own territory than a passer-by does.
        //
        // He gets the violence and the witnesses — never the breach. Who authorised a beating is
        // not a property of the beating: a wrecked shopfront looks identical whether the capo
        // ordered it, tolerated it, or knew nothing about it. Handing the boss
        // PersonBreachedPolicy as an *observation* would let him see a conclusion rather than
        // reach one, and would make the concealment this milestone exists to model unfalsifiable
        // in the other direction — there would be nothing left for Vincent to hide. He can reach
        // it by inference (Decision/Inference.cs) or be told; both are defeasible, and observing
        // a wrecked shop is neither.
        if (s.BreachedPolicyId is not null)
            Offer(world.Org.BossId, 0.5, violenceClaim, witnessClaim);

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
            //
            // Inference, because that is what this is: nothing was observed and nobody said
            // anything. Coming up empty is a conclusion she drew from looking and not finding, and
            // filing it as something she established first-hand would let a failed canvass override
            // a real account of the same incident.
            foreach (var stale in owner.Cognition.OfKind(ClaimKind.WitnessSawIncident)
                         .Where(r => r.Claim.Subject == s.TargetId).ToList())
                owner.Cognition.Learn(stale.Claim, Stance.Doubts, stale.Confidence * 0.5,
                    SourceKind.Inference, owner.Id, world.Now);
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
