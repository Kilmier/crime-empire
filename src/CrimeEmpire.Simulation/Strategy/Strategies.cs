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

    /// <summary>
    /// The one path that may schedule a StrategyStep. Cancels any step already pending for this
    /// instance first, so "at most one pending step per instance" holds regardless of which command
    /// path — continue, alter, delegate, postpone, seek approval, or a fresh start — is doing the
    /// scheduling, rather than being an accident of PendingStepEventId being a single-slot field
    /// that silently orphans whatever it used to point to. Every caller, including the two that used
    /// to schedule directly (Commit's postpone and seek-approval paths), now goes through here.
    /// </summary>
    public static void ScheduleNextStep(World world, StrategyInstance s, string cause, TimeSpan? interval = null)
    {
        if (s.PendingStepEventId is { } pending)
            world.Queue.Cancel(pending, $"{s.Label}: superseded by a newly scheduled step");

        var ev = world.Queue.Schedule(
            world.Now + (interval ?? StepInterval(s.Kind)),
            EventKind.StrategyStep,
            s.DelegatedToId ?? s.OwnerId,
            cause,
            new EventPayload
            {
                StrategyOwnerId = s.OwnerId,
                StrategySequence = s.LocalSequence,
                AdvanceOrdinal = s.NextAdvanceOrdinal,
                Strategy = s.Kind,
                StepIndex = s.StepIndex,
                TargetId = s.TargetId,
            });
        s.PendingStepEventId = ev.Id;
    }

    /// <summary>
    /// Removes this instance's commitment from the owner and, if execution was delegated, from the
    /// delegate too. Both sides accumulate one — the owner's at start, the delegate's at
    /// delegation — but only the owner's was ever cleaned up on completion or abandonment. Since
    /// CommitmentWeight feeds utility, a finished delegate kept carrying weight for work that was
    /// already done.
    /// </summary>
    public static void RemoveCommitments(World world, StrategyInstance s)
    {
        string id = $"strategy:{s.OwnerId}:{s.LocalSequence}";
        if (world.Find(s.OwnerId) is { } owner)
            owner.Execution.Commitments.RemoveAll(c => c.Id == id);
        if (s.DelegatedToId is { } delegateId && world.Find(delegateId) is { } sub)
            sub.Execution.Commitments.RemoveAll(c => c.Id == id);
    }

    /// <summary>
    /// Runs one step of the actor's current strategy and schedules what follows.
    ///
    /// Validates the delivered event against the exact instance it was scheduled for: owner, local
    /// sequence, advance ordinal, that this is the instance's own pending event, and that the woken
    /// character is who the step was actually addressed to. All five must hold. A delivery that
    /// fails any of them throws — this is a development kernel, EventQueue.Next already filters out
    /// anything properly cancelled before it reaches here, so a failure means a scheduling invariant
    /// broke upstream, not that this is a normal case to route silently around.
    /// </summary>
    public static void Advance(World world, Character actor, ScheduledEvent ev)
    {
        var payload = ev.Payload;
        string? ownerId = payload.StrategyOwnerId;
        int? sequence = payload.StrategySequence;
        int? ordinal = payload.AdvanceOrdinal;

        var owner = ownerId is null ? null : world.Find(ownerId);
        var s = owner?.Execution.Strategy;

        if (ownerId is null || sequence is null || ordinal is null || owner is null || s is null
            || s.LocalSequence != sequence.Value
            || s.NextAdvanceOrdinal != ordinal.Value
            || s.PendingStepEventId != ev.Id
            || actor.Id != (s.DelegatedToId ?? s.OwnerId))
        {
            throw new SimulationInvariantException(
                $"StrategyStep event {ev.Id} does not match a live strategy instance " +
                $"(owner={ownerId ?? "?"}, sequence={sequence?.ToString() ?? "?"}, " +
                $"ordinal={ordinal?.ToString() ?? "?"}, awakened={actor.Id}). A stale or misrouted " +
                "step must never advance a replacement.");
        }

        s.PendingStepEventId = null;
        s.NextAdvanceOrdinal = ordinal.Value + 1;
        var rng = Rng.ForOccasion(world.Seed, $"step|{s.OwnerId}|{s.LocalSequence}|{ordinal.Value}");

        var steps = StepsFor(s.Kind);
        if (s.StepIndex >= steps.Length)
        {
            Complete(world, owner, s, "ran out of steps");
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
            Complete(world, owner, s, "the target no longer exists");
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

                // The man who sent him learns nothing here. He was not there, and nobody has yet
                // told him anything: writing the executor's read straight into his head would be
                // sending a report that never left anyone's mouth. If he wants it, his man has to
                // report it through the channel like anything else.

                ScheduleNextStep(world, s, $"{s.Label}: {step} done, next step due");
                return;
            }

            case 2: // the demand lands — the owner now has a decision to make
                world.Record("demand", executor.Id, business.Id,
                    $"{executor.Name} put a demand to {business.Name}");
                // His own shop and his own decision not to pay. Nobody had to tell him.
                world.Get(business.OwnerId).Cognition.Learn(
                    new Claim(ClaimKind.BusinessRefusesTribute, business.Id),
                    Stance.Knows, 1.0, SourceKind.Participant, business.OwnerId, world.Now);

                // And he has now met the man who put it to him. Somebody standing in your shop
                // asking for money is somebody you can name afterwards, and until this line the
                // model had no way to say so — the owner's own Concede and Refuse options named a
                // man nothing in his head established. Directional: the executor already knows who
                // he leaned on, because he chose the target.
                Relations.Meet(world.Get(business.OwnerId), executor.Id);
                world.Encounters.Add(new Encounter(business.OwnerId, executor.Id, world.Now));

                world.Queue.Schedule(world.Now, EventKind.Incident, business.OwnerId,
                    $"{executor.Name} demanded payment",
                    new EventPayload { TargetId = executor.Id, Note = "tribute-demanded" });

                ScheduleNextStep(world, s, $"{s.Label}: awaiting an answer");
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
                    ScheduleNextStep(world, s, $"{s.Label}: collection due");
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
                        Relations.Frighten(marco, executor.Id, 0.35);
                        world.Record("threat", executor.Id, business.Id,
                            $"{executor.Name} made {business.Name} a promise about what came next");
                    }
                    else
                    {
                        Relations.Frighten(marco, executor.Id, 0.55);
                        ResolveViolence(world, owner, executor, s, business, rng);
                    }

                    world.Queue.Schedule(world.Now.AddHours(6), EventKind.Incident, marco.Id,
                        $"after {executor.Name}, the demand was still on the table",
                        new EventPayload { TargetId = executor.Id, Note = "tribute-demanded" });

                    // Step back one, so the next step re-enters this check and finds out whether
                    // the pressure told.
                    s.StepIndex = 2;
                    ScheduleNextStep(world, s, $"{s.Label}: seeing whether the pressure told");
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
                Complete(world, owner, s, "the money started arriving");
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

        // The man who sent him is told nothing here, and learns nothing here.
        //
        // This used to write "Tommy told him" straight into the delegator's head at the moment of
        // the beating, with no report, no message, no meeting and no trace behind it. Ordering
        // something is not a channel: authority does not deliver knowledge, and a player-facing
        // account saying his man told him — when his man had not yet opened his mouth — is an
        // invented source. He knows he gave the order, which is recorded below as his own act. That
        // it was carried out is a separate fact, and it reaches him the way anything else does:
        // somebody reports it, or he comes across the traces himself.

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
            // Note the boundary this sits on. Knowing he gave the order is his own act. Knowing it
            // was carried out is not, and is deliberately absent here — he acquires that through a
            // report or a discovery roll like anyone else, or not at all. A man can be talked into
            // doubting that Tommy went through with it; he cannot be talked out of having given
            // the order.
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
            // Only the man who was there is excluded. The one who ordered it is not: he was not
            // present, he has been told nothing, and he works the same district as everybody else
            // who gets a roll. Excluding him made sense only while his knowledge was being written
            // in for free; without that, denying him the ordinary chance to notice would leave a
            // man who cannot find out about the thing he ordered by any route at all.
            if (id is null || id == executor.Id) return;
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
            ScheduleObservation(world, s, "violence", id, ev.Id, ev.TargetId, o.Discoverability, o.Claims);
    }

    /// <summary>
    /// Schedules a chance to notice, keyed to the strategy advance that produced the opportunity
    /// rather than to any global identifier — so inserting or removing an unrelated event elsewhere
    /// cannot reroll this roll. traceKind distinguishes the different reasons the same
    /// instance/advance can offer more than one kind of opportunity: a beating and a police-interest
    /// surveillance opportunity are not the same occasion even when they trace back to the same
    /// advance. relatedEventId/relatedTargetId are history for the payload and the message text
    /// only — never randomness.
    /// </summary>
    private static void ScheduleObservation(
        World world, StrategyInstance s, string traceKind, string? observerId,
        long relatedEventId, string? relatedTargetId, double discoverability, IReadOnlyList<Claim> claims)
    {
        if (observerId is null) return;

        // Advance already incremented NextAdvanceOrdinal on entry, so the ordinal that caused this
        // opportunity is one behind the ordinal the *next* advance will use.
        int ordinal = s.NextAdvanceOrdinal - 1;
        string occasionKey = $"obs|{s.OwnerId}|{s.LocalSequence}|{ordinal}|{traceKind}|{observerId}";
        world.ObservationOccasionKeys.Add(occasionKey);

        world.Queue.Schedule(
            world.Now + TimeSpan.FromDays(1),
            EventKind.ObservationOpportunity,
            observerId,
            $"he may notice what was left behind at {relatedTargetId}",
            new EventPayload
            {
                RelatedEventId = relatedEventId,
                TargetId = relatedTargetId,
                Claims = claims,
                Discoverability = discoverability,
                OccasionKey = occasionKey,
            });
    }

    // ------------------------------------------------------------------ concealment

    /// <summary>
    /// How far one attempt at quieting witnesses moves the concealer's own view of whether the
    /// street can place him there. The same two magnitudes the step already applies to
    /// <see cref="PressureKind.LegalExposure"/> beside it, in the same directions.
    ///
    /// Deliberately not new numbers. The step has always modelled a clumsy cleanup as actively
    /// worse rather than merely ineffective — exposure rises, and completion says "the cleanup made
    /// things worse" — so the belief follows the model already there. Choosing a gentler failure
    /// here, such as leaving the belief untouched, would be picking a coefficient by what it does to
    /// a downstream decision, which milestone 010's ruling 3 forbids.
    /// </summary>
    private const double QuietedWitnessesConfidence = 0.2;
    private const double StirredWitnessesConfidence = 0.1;

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

        // StepIndex has already been incremented, so 1 is the first step — the same convention
        // AdvanceTribute switches on. Only "quiet the witnesses" touches what he thinks about
        // witnesses; "tidy the paperwork" is about records and keeps the exposure effect alone.
        if (s.StepIndex == 1)
            QuietWitnesses(world, executor, s, clean);

        if (s.StepIndex >= ConcealSteps.Length)
            Complete(world, owner, s, clean ? "the loose ends were tied off" : "the cleanup made things worse");
        else
            ScheduleNextStep(world, s, $"{s.Label}: {step} done");
    }

    /// <summary>
    /// The step named for silencing witnesses, finally silencing somebody — in exactly one place.
    ///
    /// <b>What moves is a belief, not the world.</b> No trace is removed, no truth-log entry is
    /// altered, and nobody else's cognition is touched. The grocer still saw what he saw and the
    /// wrecked shopfront is still wrecked; what changes is the executor's own confidence that the
    /// street can put him at this incident. So a man who has cleaned up and has not is exactly as
    /// available as a man who has and does not believe it — being wrong stays possible in both
    /// directions, which is why <see cref="ResolveViolence"/> records that belief as an
    /// <see cref="SourceKind.Inference"/> in the first place.
    ///
    /// <b>Whose belief.</b> The executor's. He is the man who went out and did it and formed a view
    /// from what he found; a delegator who sent him learns nothing here, exactly as he learns
    /// nothing from the approach step. <see cref="Cognition.Revise"/> refuses anything that is not
    /// the holder's own inference, so this cannot overwrite something he saw or something he was
    /// told.
    ///
    /// <b>Which incident.</b> The one the instance names, by event id. Every witness belief carrying
    /// that id moves and nothing else does — a man cleaning up after one beating does not become
    /// calmer about a different one, which is the same scan defect this milestone fixes in
    /// <see cref="Decision.Utility"/>. An instance with no identified incident quiets nothing at all
    /// rather than falling back on the target, per milestone 005's ruling.
    /// </summary>
    private static void QuietWitnesses(World world, Character executor, StrategyInstance s, bool clean)
    {
        if (s.SourceEventId is not { } incidentId) return;

        double delta = clean ? -QuietedWitnessesConfidence : StirredWitnessesConfidence;

        // Materialised before revising: Revise writes into the same record list this enumerates.
        var about = executor.Cognition.OfKind(ClaimKind.WitnessSawIncident)
            .Where(r => r.Claim.EventId == incidentId)
            .ToList();

        foreach (var r in about)
            executor.Cognition.Revise(r.Claim, r.Confidence + delta, executor.Id, world.Now);
    }

    // ------------------------------------------------------------------ investigation
    private static void AdvanceInvestigation(
        World world, Character owner, Character executor, StrategyInstance s, string step, Rng rng)
    {
        double skill = executor.Capabilities[Skill.Investigation];
        bool found = skill + rng.Range(-0.2, 0.2) > 0.55;

        world.Record("investigate", executor.Id, s.TargetId,
            $"{executor.Name} went to {step} at {s.TargetId}");

        // Which incident this case is about, carried from the lead it was opened on. A case with no
        // identified incident investigates nothing rather than falling back on the address — the
        // knowledge filter already refuses a candidate with no lead, so this is the second layer.
        long? incidentId = s.SourceEventId;

        if (found && incidentId is { } incident)
        {
            // She learns who, only if a witness claim naming a person is already in her head — and
            // only from a witness to *this* incident. Matching on s.TargetId asked what anybody had
            // seen at that address, so a second beating at the same shop fed the first case's canvass
            // a name from an event it was not investigating.
            var lead = owner.Cognition.OfKind(ClaimKind.WitnessSawIncident)
                .FirstOrDefault(r => r.Claim.EventId == incident && r.Claim.Object.Length > 0);

            if (lead is not null)
            {
                // Where, taken from the lead rather than from the instance's TargetId. Both name
                // the same shop here, and the lead is the right source on this milestone's own
                // terms: the incident's facts come from the incident. It is also the non-nullable
                // one, so this stops depending on a null check that the incident-scoped guard above
                // no longer performs.
                owner.Cognition.Learn(
                    new Claim(ClaimKind.PersonUsedViolence, lead.Claim.Object, lead.Claim.Subject, incident),
                    Stance.Suspects, 0.55, SourceKind.Inference, owner.Id, world.Now);

                // The suspect's own risk rises only if he comes to believe he is being looked at —
                // about the incident he is being looked at over. A PoliceInvestigating claim naming
                // no incident is a heat bar with one holder: it cannot be answered, cannot be
                // corroborated against anything, and cannot go stale when the case does. The related
                // event id was a hardcoded 0 for the same reason nothing had an incident to hand.
                var suspect = world.Find(lead.Claim.Object);
                if (suspect is not null)
                    ScheduleObservation(
                        world, s, "surveillance", suspect.Id, relatedEventId: incident,
                        relatedTargetId: suspect.Id,
                        discoverability: 0.4,
                        claims: new[] { new Claim(ClaimKind.PoliceInvestigating, suspect.Id, "", incident) });
            }
        }

        if (s.StepIndex < InvestigateSteps.Length)
        {
            ScheduleNextStep(world, s, $"{s.Label}: {step} done");
            return;
        }

        // Whether she has put a name to *this incident*. Asking whether she had named anybody for
        // anything at this address meant one closed case at a shop declared every later case there
        // solved before it started.
        bool named = incidentId is { } closing && owner.Cognition
            .OfKind(ClaimKind.PersonUsedViolence).Any(v => v.Claim.EventId == closing);

        if (!named && incidentId is { } cold)
        {
            // The trail went cold. She stops treating the street talk as something to act on —
            // otherwise she reopens the same dead case every time the calendar nudges her.
            //
            // Through Revise, not Learn. This was written as a Learn at half confidence and did
            // nothing at all: Learn's override rule discards an Inference arriving less confident
            // than what is already held, so a man's own conclusions could only ever firm up. The
            // branch has been inert since it was written and no natural run reaches it, which is why
            // it went unnoticed — milestone 010 found the same shape in the concealment step and
            // added Revise for it. Stance is not passed: a revision is a change of confidence, and
            // demoting the stance as well would be a second rule with its own threshold.
            //
            // Her own conclusion is exactly what this is. Nothing was observed and nobody said
            // anything; coming up empty is something she worked out from looking and not finding —
            // which is also why Revise accepts it, since it refuses anything that is not the
            // holder's own inference. A lead she was *told* therefore survives a failed canvass,
            // and that is correct: not finding a witness is not evidence the account was false.
            foreach (var stale in owner.Cognition.OfKind(ClaimKind.WitnessSawIncident)
                         .Where(r => r.Claim.EventId == cold).ToList())
                owner.Cognition.Revise(stale.Claim, stale.Confidence * 0.5, owner.Id, world.Now);
        }

        Complete(world, owner, s, named ? "the canvass turned up a name" : "the trail went cold");
    }

    // ------------------------------------------------------------------ completion
    public static void Complete(World world, Character owner, StrategyInstance s, string why)
    {
        world.Queue.Schedule(world.Now, EventKind.StrategyComplete, owner.Id,
            $"{s.Label} finished: {why}",
            new EventPayload { Strategy = s.Kind, TargetId = s.TargetId, Note = why });

        owner.Execution.Strategy = null;
        owner.Execution.Intention = null;
        RemoveCommitments(world, s);
    }
}
