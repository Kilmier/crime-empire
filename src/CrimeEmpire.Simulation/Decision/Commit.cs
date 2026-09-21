namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;
using CrimeSim.Strategy;

/// <summary>
/// Turns the chosen candidate into world state, commitments and scheduled events.
///
/// Commitment is created here rather than re-derived each tick: a character normally carries on
/// with what they started until it finishes, fails, becomes impossible, or something interrupts —
/// and abandoning costs them. That is what stops small score fluctuations producing a character
/// who changes their mind every time they are woken.
/// </summary>
public static class Commit
{
    public static string Apply(
        World world,
        Character actor,
        Candidate c,
        Agenda agenda,
        GeneratorContext ctx,
        List<string> reconsideration)
    {
        switch (c.Kind)
        {
            case ActionKind.StartStrategy:
            {
                // Fail closed, mirroring Filters' own refusal: a ConcealIncident that reaches Commit
                // without an incident attached must never start, because it could never be recorded
                // in AttemptedConcealments and the MVP rule would have nothing to check next time.
                // Filters is expected to have already refused this candidate; this is the second
                // layer for whatever might reach Commit some other way, e.g. a hand-built candidate
                // that skipped filtering.
                if (c.Strategy == StrategyKind.ConcealIncident && c.AboutIncident is null)
                    throw new SimulationInvariantException(
                        $"ConcealIncident candidate '{c.Id}' has no AboutIncident; it must be " +
                        "refused before commitment, not started unrecorded.");

                // Fail closed, mirroring Filters' own refusal: a man currently carrying somebody
                // else's delegated operation may not start one of his own — the one-operation-per-
                // involved-character rule, mirrored from Pipeline.AvailableToExecute's identical
                // enforcement on being offered as a delegate. Re-read World at the write boundary:
                // GeneratorContext is a preparation-time snapshot and is not authoritative if a
                // caller reaches Commit directly or the world changes between prepare and commit.
                bool commissioning = Commissioning.IsOperation(c);
                string executorId = c.InitialExecutorId ?? actor.Id;
                if (commissioning) Commissioning.Validate(world, actor, c, executorId);
                var authoritativeExecution = Strategies.CurrentExecution(world, actor);
                if (!commissioning && actor.Execution.Strategy is null && authoritativeExecution is { } busyWith)
                    throw new SimulationInvariantException(
                        $"'{actor.Id}' cannot start {c.Strategy}; he is currently carrying " +
                        $"{busyWith.Label} for somebody else. One operation at a time.");

                // Supervision leaves personal execution free, but must not duplicate the owner's
                // existing operation on this target. Independent actors are not globally reserved.
                if (c.Strategy != StrategyKind.ConcealIncident && actor.Execution.Operations.Any(
                        s => s.Kind == c.Strategy && s.TargetId == c.TargetId))
                    throw new SimulationInvariantException(
                        $"'{actor.Id}' already owns {c.Strategy} on {c.TargetId}.");

                // Replacing whatever instance is currently running, if any — legitimate (a
                // genuinely different incident, a new target) but not something that may orphan the
                // old instance's pending step or leave its commitment behind for AbandonStrategy or
                // Complete to never find. Filters' redundancy stage already refused a candidate that
                // would merely restart the same (Kind, TargetId) or re-attempt an already-handled
                // incident, so anything that reaches here is a real replacement.
                if (!commissioning && actor.Execution.Strategy is { } previous)
                {
                    if (previous.PendingStepEventId is { } pendingPrevious)
                        world.Queue.Cancel(pendingPrevious,
                            $"{actor.Name} started {c.Strategy} instead of {previous.Label}");
                    Strategies.RemoveCommitments(world, previous);
                }

                var s = new StrategyInstance
                {
                    OwnerId = actor.Id,
                    DelegatedToId = commissioning && executorId != actor.Id ? executorId : null,
                    CommissionedExecutorId = commissioning ? executorId : null,
                    InitialBriefing = commissioning && executorId != actor.Id
                        ? AssignmentBriefing.CaptureTarget(actor, c.TargetId!) : Array.Empty<ReportedClaim>(),
                    LocalSequence = actor.StrategyCount++,
                    Kind = c.Strategy!.Value,
                    Domain = c.Domain ?? "",
                    TargetId = c.TargetId,
                    Method = c.Method ?? CoercionMethod.Persuade,
                    OwnerOrderedMethod = c.Method ?? CoercionMethod.Persuade,
                    StartedAt = world.Now,
                    Deadline = ctx.MyAssignment?.Deadline ?? world.Now.AddDays(30),
                    AssignmentId = agenda.AssignmentId,
                    // Which incident this is about, carried on the instance so its steps can act on
                    // it. Commit already recorded the incident in AttemptedConcealments and then
                    // dropped it, so a concealment knew which incident it must not repeat and not
                    // which incident it was concealing. Milestone 005's ruling applies here as well:
                    // the incident is the identity, never "whatever happened at this address", which
                    // is why this is the event id rather than TargetId. A claim carrying no event id
                    // names no incident and yields null rather than 0.
                    SourceEventId = c.AboutIncident is { EventId: not 0 } incidentEvent
                        ? incidentEvent.EventId
                        : null,
                    BreachedPolicyId = c.BreachesPolicyId,
                    // Set together with BreachedPolicyId, never inferred later: whoever committed
                    // this StartStrategy is the one who chose the method, full stop. See
                    // StrategyInstance.PolicyBreachDecisionMakerId's own doc comment.
                    PolicyBreachDecisionMakerId = c.BreachesPolicyId is not null ? actor.Id : null,
                };
                if (commissioning) actor.Execution.Operations.Add(s);
                else actor.Execution.Strategy = s;
                actor.Execution.Intention = c.Description;
                actor.Execution.Commitments.Add(new Commitment(
                    $"strategy:{s.OwnerId}:{s.LocalSequence}", c.Description, ctx.SuperiorId, world.Now, 0.6));

                if (s.Kind == StrategyKind.ConcealIncident && c.AboutIncident is { } incident)
                    actor.Execution.AttemptedConcealments.Add(incident);

                if (s.DelegatedToId is { } initialExecutor)
                {
                    var sub = world.Get(initialExecutor);
                    actor.Execution.RecordDelegation(initialExecutor);
                    AssignmentBriefing.Deliver(world, sub, actor.Id, initialExecutor, s.InitialBriefing);
                    sub.Execution.Commitments.Add(new Commitment(
                        $"strategy:{s.OwnerId}:{s.LocalSequence}", $"handle {s.Label} for {actor.Name}", actor.Id, world.Now, 0.7));
                    Strategies.ScheduleReview(world, s);
                    if (authoritativeExecution is null)
                        world.Queue.Schedule(world.Now, EventKind.RoleReview, actor.Id,
                            "commissioning left his hands free", new EventPayload { Note = "hands-free" });
                }
                Strategies.ScheduleNextStep(world, s, $"{s.Label}: first step");
                reconsideration.Add("the target refuses outright");
                reconsideration.Add("he comes to believe police are watching");
                return $"began {s.Label}"
                       + (c.BreachesPolicyId is not null ? $" — knowingly against \"{c.BreachesPolicyId}\"" : "");
            }

            case ActionKind.ContinueStrategy:
            {
                if (ctx.ReviewOperation is not null)
                {
                    Strategies.ScheduleReview(world, OwnedOperation(actor, c));
                    return "left the operation's orders unchanged";
                }
                // Belongs to whoever is currently executing — the delegate, once there is one.
                // Milestone 024's sixth correction; see GeneratorContext.CurrentExecution.
                var s = ExecutedOperation(world, actor, c, ctx);

                // Carrying on means leaving whatever is already scheduled alone, not replacing it
                // with a fresh one at a fresh interval. ScheduleNextStep always cancels-and-
                // reschedules, which is right for Alter/Delegate/Postpone/SeekApproval — each of
                // those deliberately changes the timing — but wrong here: a character woken early by
                // some unrelated trigger (a pressure threshold, an incident) who simply chooses to
                // continue was silently pushing the strategy's next step a full interval further out
                // every time, which could delay it indefinitely under repeated early wakes. Only
                // schedule when nothing is actually still pending.
                if (s.PendingStepEventId is null || world.Queue.Cancelled.ContainsKey(s.PendingStepEventId.Value))
                    Strategies.ScheduleNextStep(world, s, $"{s.Label}: next step");

                reconsideration.Add("the approach stops working");
                return $"carried on with {s.Label}";
            }

            case ActionKind.AlterStrategy:
            {
                // Belongs to whoever is currently executing — the delegate, once there is one.
                // Milestone 024's sixth correction; see GeneratorContext.CurrentExecution.
                var s = ExecutedOperation(world, actor, c, ctx);
                var was = s.Method;
                s.Method = c.Method ?? s.Method;
                if (actor.Id == s.OwnerId) s.OwnerOrderedMethod = s.Method;
                // Only when this alter genuinely changes which prohibited method is operative —
                // the first breach ever recorded, or the method actually moving under a breaching
                // candidate — does the decision-maker change. A repeated or no-op alter that leaves
                // the method where it was must not rewrite who made the original choice, even if it
                // still carries the same BreachesPolicyId.
                if (c.BreachesPolicyId is not null && (s.PolicyBreachDecisionMakerId is null || was != s.Method))
                {
                    s.BreachedPolicyId = c.BreachesPolicyId;
                    s.PolicyBreachDecisionMakerId = actor.Id;
                }
                // Drop back to the confrontation step so the new method actually gets used, and
                // let the new method have its own turn at pressing. StepIndex may legitimately
                // repeat this way; NextAdvanceOrdinal never does, which is why occasion keys are
                // built from the ordinal and not from StepIndex.
                s.StepIndex = 2;
                s.PressureApplied = false;
                Strategies.ScheduleNextStep(world, s, $"{s.Label}: escalated approach");
                reconsideration.Add("the escalation draws attention");
                return $"escalated from {was.ToString().ToLowerInvariant()} to {s.Method.ToString().ToLowerInvariant()} against {s.TargetId}"
                       + (c.BreachesPolicyId is not null ? $" — knowingly against \"{c.BreachesPolicyId}\"" : "");
            }

            case ActionKind.DelegateStrategy:
            {
                var s = OwnedOperation(actor, c);

                // Fail closed, mirroring the ConcealIncident guard above: Filters/Generators is
                // expected to have already refused a candidate targeting a busy subordinate — see
                // Pipeline.AvailableToExecute, the one definition both enforce — but this is the
                // second layer for a hand-built candidate, or one a future caller generates outside
                // this pipeline, that reached Commit some other way.
                if (!Pipeline.AvailableToExecute(world, c.TargetId!))
                    throw new SimulationInvariantException(
                        $"'{c.TargetId}' cannot be delegated {s.Label}; he already owns a running " +
                        "strategy or is already carrying delegated work for somebody else. The " +
                        "one-operation-per-executor rule must be enforced before Commit, not here.");

                if (c.IsOperationReview && (!Pipeline.SubordinatesOf(world, actor).Contains(c.TargetId!)
                    || !Acquaintance.KnownTo(world, actor).Contains(c.TargetId!)))
                    throw new SimulationInvariantException("Delegation requires an acquainted direct subordinate.");
                bool freedPersonalExecution = s.DelegatedToId is null;
                if (s.DelegatedToId is { } former)
                    world.Get(former).Execution.Commitments.RemoveAll(
                        entry => entry.Id == $"strategy:{s.OwnerId}:{s.LocalSequence}");
                s.DelegatedToId = c.TargetId;
                var sub = world.Get(c.TargetId!);

                // Recorded on the character, not only on the instance. He is owed an account of
                // this work after it finishes as much as during it — see DelegatedExecutorIds.
                actor.Execution.RecordDelegation(sub.Id);
                // The delegate learns enough to act, and no more. Information topology changes here.
                //
                // Through Receive, because handing a man a job is briefing him: it is an act of
                // telling, it leaves testimony he can attribute and later contest, and it carries
                // the delegator's own basis so the delegate can tell "I saw it myself" from "I was
                // told". Using Learn put claims in his head sourced to his boss with nothing on
                // record of his boss having said anything — indistinguishable, from the outside,
                // from the organisation simply handing knowledge downward.
                foreach (var belief in ctx.Perceived.Beliefs
                             .Where(b => b.Claim.Subject == s.TargetId && b.IsHeld)
                             .Take(2))
                {
                    var receipt = sub.Cognition.Receive(
                        ReportedClaim.Honest(
                            belief.Claim, Stance.Believes, belief.Confidence * 0.8, belief.SourceKind),
                        actor.Id, world.Now);

                    Strategies.ReviewAfterReceipt(world, sub, receipt);

                    // A briefing can contradict what the man already holds, and when it does it is
                    // a conflict like any other. Applying the consequence only in the report channel
                    // would be this project's most reliable defect — a rule written where it was
                    // noticed and missing everywhere else the value travels — and it would mean a
                    // capo could contradict his own soldier for free by calling it an instruction.
                    if (receipt.Conflict is { } conflict)
                    {
                        world.AccountConflicts.Add(new PerceivedConflict(sub.Id, conflict, world.Now));
                        Relations.RecordAccountConflict(sub, conflict, world.Now);
                    }

                    // Milestone 016: the mirror-image consequence, at the same site, for the same
                    // reason ruling 7 requires all three receipt paths to carry it — a briefing can
                    // corroborate what the man already holds just as readily as it can contradict it.
                    if (receipt.Agreement is { } agreement)
                    {
                        world.AccountAgreements.Add(new PerceivedAgreement(sub.Id, agreement, world.Now));
                        Relations.RecordAccountAgreement(sub, agreement, world.Now);
                    }
                }

                sub.Execution.Commitments.Add(new Commitment(
                    $"strategy:{s.OwnerId}:{s.LocalSequence}", $"handle {s.Label} for {actor.Name}", actor.Id, world.Now, 0.7));

                Strategies.ScheduleNextStep(world, s, $"{s.Label}: {sub.Name} takes it on");
                Strategies.ScheduleReview(world, s);
                if (freedPersonalExecution)
                    world.Queue.Schedule(world.Now, EventKind.RoleReview, actor.Id,
                        "delegation freed his hands", new EventPayload { Note = "hands-free" });
                reconsideration.Add($"{sub.Name} reports back or fails to");
                return $"handed {s.Label} to {sub.Name}";
            }

            case ActionKind.PostponeStrategy:
            {
                // Belongs to whoever is currently executing — the delegate, once there is one.
                // Milestone 024's sixth correction; see GeneratorContext.CurrentExecution. Explicitly
                // reschedules exactly one step, which is the whole point: the operation is
                // preserved, not silently dropped the way an unhandled DoNothing would leave it.
                var s = c.OperationSequence is null ? ctx.CurrentExecution : ExecutedOperation(world, actor, c, ctx);
                if (s is null) return "let matters sit";
                Strategies.ScheduleNextStep(world, s, $"{s.Label}: picked back up after a pause", TimeSpan.FromDays(7));
                reconsideration.Add("the delay makes things worse");
                return $"put {s.Label} off for a week";
            }

            case ActionKind.AbandonStrategy:
            {
                var s = OwnedOperation(actor, c);
                if (s.PendingStepEventId is { } pending)
                    world.Queue.Cancel(pending, $"{actor.Name} abandoned {s.Label}");
                actor.Execution.Operations.Remove(s);
                if (actor.Execution.Strategy is null) actor.Execution.Intention = null;
                Strategies.RemoveCommitments(world, s);
                world.Queue.Schedule(world.Now.AddDays(10), EventKind.RoleReview, actor.Id,
                    "periodic review of his own patch");
                return $"dropped {s.Label}";
            }

            case ActionKind.ReportToSuperior:
            {
                var boss = world.Get(c.TargetId!);
                var report = Reporting.Compose(world, actor, boss, c, ctx.Perceived);
                Reporting.Deliver(world, report, boss);

                // The note is what lets the player-facing occasion say why he is thinking without
                // reading the authored cause. RoleReview covers five different schedulers, and a
                // phrase keyed on the event kind alone told a man he was doing his rounds when
                // somebody had just reported to him. Nothing in the decision path reads this value —
                // Generators only tests for "asked-to-account" and "tribute-demanded".
                world.Queue.Schedule(world.Now.AddDays(1), EventKind.RoleReview, boss.Id,
                    $"{actor.Name} reported in",
                    new EventPayload { TargetId = actor.Id, Note = "reported-to" });

                return report.Candor switch
                {
                    ReportCandor.Partial =>
                        $"reported to {boss.Name}, keeping {report.Withheld.Count} thing(s) back",
                    ReportCandor.False =>
                        $"told {boss.Name} it did not happen",
                    ReportCandor.Uninformed =>
                        $"told {boss.Name} he knew nothing of it",
                    _ => $"reported to {boss.Name} ({report.Asserted.Count} claims passed on)",
                };
            }

            case ActionKind.SeekCorroboration:
            {
                var other = world.Get(c.TargetId!);

                // Filed before anything else, because the question is spent the moment it leaves
                // his mouth. Waiting for a reply to record it would make an unanswered request
                // indistinguishable from one never made. Scoped to what he asked about, so it
                // spends that question rather than the channel.
                var about = c.AboutClaim ?? default;
                world.Requests.Add(new InformationRequest(
                    world.NextRequestId(), actor.Id, other.Id, about, world.Now));

                // Asking is a thing that happened. It belongs in the developer log alongside every
                // other occurrence, and putting it there also brings it under the runner's
                // determinism hash rather than leaving it as untracked state.
                // Naming the subject here, not just the pair. This line is the independent check on
                // request state: the replay snapshot is its own comparator and cannot catch a
                // dropped field, whereas the runner's --verify hash covers the truth log by a
                // different route and will move if the subject is lost.
                world.Record("request", actor.Id, other.Id,
                    $"{actor.Name} asked {other.Name} for his own account of {about}");

                // Being asked is meeting the asker. Without this a man could be woken to answer
                // somebody he had no way of naming, and the answer candidate would name him anyway.
                Relations.Meet(other, actor.Id);
                world.Encounters.Add(new Encounter(other.Id, actor.Id, world.Now));

                // He asks; the other man decides for himself what to say. Waking the other
                // character is the whole point — a request that produced an answer directly would
                // be truth synchronisation wearing a question mark.
                world.Queue.Schedule(world.Now.AddDays(1), EventKind.RoleReview, other.Id,
                    $"{actor.Name} asked him directly what happened",
                    new EventPayload
                    {
                        TargetId = actor.Id,
                        Note = "asked-to-account",
                        AboutClaim = about,
                    });
                reconsideration.Add($"{other.Name} gives his account, or avoids giving one");
                return $"went to {other.Name} for his own account";
            }

            case ActionKind.SeekApproval:
            {
                var boss = world.Get(c.TargetId!);
                world.Queue.Schedule(world.Now.AddDays(1), EventKind.RoleReview, boss.Id,
                    $"{actor.Name} asked for latitude in {agenda.Domain}",
                    new EventPayload { TargetId = actor.Id, Note = "permission-sought" });
                if (ctx.CurrentExecution is { } s)
                    Strategies.ScheduleNextStep(world, s, $"{s.Label}: waiting on {boss.Name}", TimeSpan.FromDays(5));
                reconsideration.Add($"{boss.Name} answers, or does not");
                return $"asked {boss.Name} for room to move";
            }

            case ActionKind.Retaliate:
            {
                var target = world.Get(c.TargetId!);
                world.Record("retaliation", actor.Id, target.Id,
                    $"{actor.Name} moved against {target.Name}");
                Relations.RaiseGrievance(target, new Grievance(actor.Id, "moved against me", 0.5, world.Now));
                world.Org.AdjustCondition(Org.OrgCondition.LeadershipInstability, 0.25);
                // Acting on a grudge spends it. Otherwise resentment is a tap that never closes and
                // the character strikes at the same person every time he is woken.
                actor.Motivations.AddPressure(PressureKind.Resentment, -0.5);
                return $"moved against {target.Name}";
            }

            case ActionKind.Concede:
            {
                var biz = world.Businesses.Values.FirstOrDefault(b => b.OwnerId == actor.Id);
                if (biz is not null)
                {
                    if (!biz.PayingTribute)
                        biz.TributeCollectedForCurrentAgreement = false;
                    biz.PayingTribute = true;
                    biz.Resistance = Math.Max(0, biz.Resistance - 0.4);
                    world.Record("concede", actor.Id, biz.Id, $"{actor.Name} agreed to pay");
                }
                return "agreed to pay";
            }

            case ActionKind.Refuse:
            {
                var biz = world.Businesses.Values.FirstOrDefault(b => b.OwnerId == actor.Id);
                if (biz is not null)
                {
                    biz.Resistance = Math.Min(1, biz.Resistance + 0.15);
                    world.Record("refuse", actor.Id, biz.Id, $"{actor.Name} refused to pay");
                }
                reconsideration.Add("they come back harder");
                return "refused";
            }

            default:
                if (ctx.ReviewOperation is { } reviewed)
                {
                    Strategies.ScheduleReview(world, reviewed);
                    return "left the operation's orders unchanged";
                }
                // Only come back to someone who has something standing to come back to. Waking a
                // character on a timer to decide nothing is exactly the fast-forward cost the
                // event model is supposed to avoid.
                if (actor.Motivations.Responsibilities.Count > 0 || ctx.MyAssignment is not null)
                    world.Queue.Schedule(world.Now.AddDays(30), EventKind.RoleReview, actor.Id,
                        "his patch came up for review again");
                return "did nothing";
        }
    }

    private static StrategyInstance ExecutedOperation(World world, Character actor, Candidate candidate, GeneratorContext ctx)
    {
        if (candidate.OperationSequence is null)
            return ctx.CurrentExecution ?? throw new SimulationInvariantException("No current execution.");
        var s = Strategies.CurrentExecution(world, actor);
        if (s is null || s.OwnerId != candidate.OperationOwnerId || s.LocalSequence != candidate.OperationSequence)
            throw new SimulationInvariantException("The selected operation is no longer executed by this actor.");
        return s;
    }

    private static StrategyInstance OwnedOperation(Character actor, Candidate candidate)
    {
        var s = candidate.OperationSequence is { } sequence
            ? actor.Execution.Operations.SingleOrDefault(s => s.LocalSequence == sequence
                && s.OwnerId == candidate.OperationOwnerId)
            : actor.Execution.Strategy;
        return s ?? throw new SimulationInvariantException("The selected owned operation is no longer active.");
    }
}
