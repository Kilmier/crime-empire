namespace CrimeSim.Sim;

using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Strategy;

/// <summary>
/// The simulation loop. Time advances to the next scheduled event and no further — empty days cost
/// nothing. Most event kinds simply wake one character and hand them to the decision pipeline; the
/// exceptions are institutional (OrgReview) and perceptual (ObservationOpportunity), neither of
/// which is a personal deliberation.
/// </summary>
public static class Runner
{
    public static void Run(World world, DateTime until)
    {
        while (world.Queue.Next(until) is { } ev)
        {
            world.Now = ev.Time;
            Handle(world, ev);
        }
    }

    private static void Handle(World world, ScheduledEvent ev)
    {
        var actor = ev.OwnerId is null ? null : world.Find(ev.OwnerId);

        switch (ev.Kind)
        {
            case EventKind.OrgReview:
                LeadershipReview(world, ev);
                return;

            case EventKind.ObservationOpportunity when actor is not null:
                Observe(world, actor, ev);
                return;

            case EventKind.StrategyStep when actor is not null:
                Strategies.Advance(world, actor, ev);
                return;

            case EventKind.WorldTick:
                Tick(world, ev);
                return;

            case EventKind.AssignmentDelivered when actor is not null:
                DeliverAssignment(world, actor, ev);
                Pipeline.Deliberate(world, actor, ev);
                return;

            // Everything else is a reason for one person to think.
            case EventKind.RoleReview:
            case EventKind.StrategyComplete:
            case EventKind.StrategyBlocked:
            case EventKind.Incident:
            case EventKind.PressureThreshold:
                if (actor is not null) Pipeline.Deliberate(world, actor, ev);
                return;
        }
    }

    // ---------------------------------------------------------------- organisational intent
    /// <summary>
    /// Conditions -> priorities and policies -> office responsibilities -> assignments.
    ///
    /// This is not a deliberation and does not run the pipeline: an organisation is neither an
    /// omniscient agent nor N independent agents. Leadership converts institutional pressure into a
    /// bounded objective for whoever holds the relevant office, and that officeholder then decides
    /// for themselves what to do about it.
    /// </summary>
    private static void LeadershipReview(World world, ScheduledEvent ev)
    {
        var org = world.Org;
        if (org.BossId is null) return;
        var boss = world.Get(org.BossId);

        if (org.Condition(OrgCondition.RevenueLoss) < 0.35) return;

        var office = org.OfficeForDomain("harbour");
        if (office?.HolderId is null) return;
        if (org.Assignments.Any(a => a.RecipientId == office.HolderId && a.Deadline >= world.Now)) return;

        var priority = new Priority("p-harbour-revenue", "restore the harbour tribute", office.Domain, 1.0);
        if (!org.Priorities.Any(p => p.Id == priority.Id)) org.Priorities.Add(priority);

        var policy = org.PoliciesForDomain(office.Domain).FirstOrDefault();

        // What the issuer chooses to disclose, fixed now rather than looked up when the briefing
        // lands. The recipient learns the policy exists because the boss told him — not because
        // policies are globally visible — and he is told it in the terms the boss used at the time.
        var disclosed = new List<ReportedClaim>();
        foreach (var r in boss.Cognition.OfKind(ClaimKind.BusinessRefusesTribute))
            disclosed.Add(ReportedClaim.Honest(r.Claim, Stance.Believes, 0.75, r.SourceKind));

        if (policy is not null)
        {
            var awareness = policy.AwarenessClaim(org.Id);
            var held = boss.Cognition.Find(awareness);
            var basis = held?.SourceKind ?? SourceKind.Participant;
            disclosed.Add(ReportedClaim.Honest(awareness, Stance.Believes, 0.75, basis));
        }

        var assignment = new Assignment(
            world.NextAssignmentId(),
            priority.Description,
            boss.Id,
            office.HolderId,
            office.Domain,
            policy is null ? Array.Empty<string>() : new[] { policy.Description },
            disclosed,
            world.Now,
            world.Now.AddDays(30));

        org.Assignments.Add(assignment);
        world.Record("assignment", boss.Id, office.HolderId,
            $"{boss.Name} told {world.Get(office.HolderId).Name} to {priority.Description}");

        world.Queue.Schedule(world.Now.AddHours(6), EventKind.AssignmentDelivered, office.HolderId,
            $"{boss.Name} handed him the harbour and a deadline",
            new EventPayload { AssignmentId = assignment.Id });
    }

    private static void DeliverAssignment(World world, Character actor, ScheduledEvent ev)
    {
        var assignment = world.Org.Assignments.FirstOrDefault(a => a.Id == ev.Payload.AssignmentId);
        if (assignment is null) return;

        // Briefing a man is telling him something, so it goes through Receive like any other
        // account: it leaves testimony he can weigh, contest and attribute. Using Learn here put
        // claims into his head with a source attached but nothing on record of anyone having
        // spoken — which reads, from the outside, exactly like the organisation handing him
        // knowledge for being in it.
        //
        // Delivering the snapshot taken at issuance, not the issuer's current beliefs. Reading them
        // back here asked what the boss thinks *now* about something he said six hours ago, so a
        // change of mind in between rewrote what he had already told his capo.
        foreach (var disclosed in assignment.Disclosed)
            actor.Cognition.Receive(disclosed, assignment.IssuerId, world.Now);

        actor.Motivations.Responsibilities.Add(
            new Responsibility($"assignment:{assignment.Id}", assignment.Objective, assignment.Domain));
        actor.Execution.Commitments.Add(new Commitment(
            $"assignment:{assignment.Id}", assignment.Objective, assignment.IssuerId, world.Now, 0.8));
    }

    // ---------------------------------------------------------------- perception
    /// <summary>
    /// A chance to notice, not a notification. Whether the claim is acquired depends on the
    /// observer's attentiveness and the trace's discoverability; a missed roll means the thing
    /// still happened and simply was not seen.
    /// </summary>
    private static void Observe(World world, Character observer, ScheduledEvent ev)
    {
        // Keyed to the occasion the scheduler built it from, not to this event's own Id — the whole
        // point being that inserting or removing an unrelated event elsewhere cannot reroll it.
        var occasionKey = ev.Payload.OccasionKey
            ?? throw new SimulationInvariantException(
                $"ObservationOpportunity event {ev.Id} carries no occasion key; every scheduler of " +
                "this event kind is required to set one.");
        var rng = Rng.ForOccasion(world.Seed, occasionKey);
        double attentiveness = 0.4 + 0.6 * observer.Capabilities[Skill.Investigation];
        if (!rng.Chance(ev.Payload.Discoverability * attentiveness)) return;

        bool learnedSomething = false;
        foreach (var claim in ev.Payload.Claims)
        {
            // Direct, and sourced to the observer himself: he noticed this, nobody told him. The
            // confidence is well short of certainty because noticing a trace is not the same as
            // understanding it — but it is his own, which is what makes it hard to talk him out
            // of later. This is the "direct observation" half of the information loop; there is
            // no rumour network here, and a claim acquired this way is never re-transmitted
            // except through the explicit report channel.
            // Discovery, and this is the load-bearing one. He is rolling against how discoverable a
            // trace was, a day after the fact — a wrecked shopfront, a man visibly worked over,
            // talk on the street. None of that puts him at the scene, and recording it as
            // witnessing would have the simulation assert his whereabouts on the strength of a
            // discovery roll.
            observer.Cognition.Learn(claim, Stance.Believes, 0.6, SourceKind.Discovery, observer.Id, world.Now);
            learnedSomething = true;

            if (claim.Kind == ClaimKind.PersonBreachedPolicy && claim.Subject != observer.Id)
            {
                observer.Social.Grievances.Add(
                    new Grievance(claim.Subject, $"went outside my instruction on {claim.Object}", 0.45, world.Now));
                observer.Motivations.AddPressure(PressureKind.Resentment, 0.4);
            }

            if (claim.Kind == ClaimKind.PoliceInvestigating && claim.Subject == observer.Id)
                observer.Motivations.AddPressure(PressureKind.LegalExposure, 0.35);
        }

        if (learnedSomething)
            world.Queue.Schedule(world.Now.AddHours(2), EventKind.Incident, observer.Id,
                "what he heard changed the picture",
                new EventPayload { RelatedEventId = ev.Payload.RelatedEventId, TargetId = ev.Payload.TargetId });
    }

    // ---------------------------------------------------------------- slow world
    private static void Tick(World world, ScheduledEvent ev)
    {
        foreach (var b in world.Businesses.Values.OrderBy(b => b.Id, StringComparer.Ordinal))
            if (!b.PayingTribute)
                world.Org.AdjustCondition(OrgCondition.RevenueLoss, 0.05);

        foreach (var c in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            if (!c.IsOrgMember) continue;
            c.Motivations.AddPressure(PressureKind.LegalExposure, -0.02);

            // A pressure crossing a threshold is itself a reason to think.
            foreach (var kind in new[] { PressureKind.LegalExposure, PressureKind.Resentment })
            {
                if (c.Motivations.Pressure(kind) >= 0.6 && c.Execution.Strategy is null)
                {
                    world.Queue.Schedule(world.Now.AddHours(1), EventKind.PressureThreshold, c.Id,
                        $"{kind} had become hard to ignore");
                    break;
                }
            }
        }

        world.Queue.Schedule(world.Now.AddDays(7), EventKind.OrgReview, world.Org.BossId, "weekly look at the books");
        world.Queue.Schedule(world.Now.AddDays(7), EventKind.WorldTick, null, "a week passed");
    }
}
