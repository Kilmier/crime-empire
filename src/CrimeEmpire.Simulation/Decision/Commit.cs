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

                // Replacing whatever instance is currently running, if any — legitimate (a
                // genuinely different incident, a new target) but not something that may orphan the
                // old instance's pending step or leave its commitment behind for AbandonStrategy or
                // Complete to never find. Filters' redundancy stage already refused a candidate that
                // would merely restart the same (Kind, TargetId) or re-attempt an already-handled
                // incident, so anything that reaches here is a real replacement.
                if (actor.Execution.Strategy is { } previous)
                {
                    if (previous.PendingStepEventId is { } pendingPrevious)
                        world.Queue.Cancel(pendingPrevious,
                            $"{actor.Name} started {c.Strategy} instead of {previous.Label}");
                    Strategies.RemoveCommitments(world, previous);
                }

                var s = new StrategyInstance
                {
                    OwnerId = actor.Id,
                    LocalSequence = actor.StrategyCount++,
                    Kind = c.Strategy!.Value,
                    Domain = c.Domain ?? "",
                    TargetId = c.TargetId,
                    Method = c.Method ?? CoercionMethod.Persuade,
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
                };
                actor.Execution.Strategy = s;
                actor.Execution.Intention = c.Description;
                actor.Execution.Commitments.Add(new Commitment(
                    $"strategy:{s.OwnerId}:{s.LocalSequence}", c.Description, ctx.SuperiorId, world.Now, 0.6));

                if (s.Kind == StrategyKind.ConcealIncident && c.AboutIncident is { } incident)
                    actor.Execution.AttemptedConcealments.Add(incident);

                Strategies.ScheduleNextStep(world, s, $"{s.Label}: first step");
                reconsideration.Add("the target refuses outright");
                reconsideration.Add("he comes to believe police are watching");
                return $"began {s.Label}"
                       + (c.BreachesPolicyId is not null ? $" — knowingly against \"{c.BreachesPolicyId}\"" : "");
            }

            case ActionKind.ContinueStrategy:
            {
                var s = actor.Execution.Strategy!;

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
                var s = actor.Execution.Strategy!;
                var was = s.Method;
                s.Method = c.Method ?? s.Method;
                s.BreachedPolicyId = c.BreachesPolicyId ?? s.BreachedPolicyId;
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
                var s = actor.Execution.Strategy!;
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

                    // A briefing can contradict what the man already holds, and when it does it is
                    // a conflict like any other. Applying the consequence only in the report channel
                    // would be this project's most reliable defect — a rule written where it was
                    // noticed and missing everywhere else the value travels — and it would mean a
                    // capo could contradict his own soldier for free by calling it an instruction.
                    if (receipt.Conflict is { } conflict)
                    {
                        world.AccountConflicts.Add(new PerceivedConflict(sub.Id, conflict, world.Now));
                        Relations.RecordAccountConflict(sub, conflict);
                    }
                }

                sub.Execution.Commitments.Add(new Commitment(
                    $"strategy:{s.OwnerId}:{s.LocalSequence}", $"handle {s.Label} for {actor.Name}", actor.Id, world.Now, 0.7));

                Strategies.ScheduleNextStep(world, s, $"{s.Label}: {sub.Name} takes it on");
                reconsideration.Add($"{sub.Name} reports back or fails to");
                return $"handed {s.Label} to {sub.Name}";
            }

            case ActionKind.PostponeStrategy:
            {
                var s = actor.Execution.Strategy;
                if (s is null) return "let matters sit";
                Strategies.ScheduleNextStep(world, s, $"{s.Label}: picked back up after a pause", TimeSpan.FromDays(7));
                reconsideration.Add("the delay makes things worse");
                return $"put {s.Label} off for a week";
            }

            case ActionKind.AbandonStrategy:
            {
                var s = actor.Execution.Strategy!;
                if (s.PendingStepEventId is { } pending)
                    world.Queue.Cancel(pending, $"{actor.Name} abandoned {s.Label}");
                actor.Execution.Strategy = null;
                actor.Execution.Intention = null;
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
                if (actor.Execution.Strategy is { } s)
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
                // Only come back to someone who has something standing to come back to. Waking a
                // character on a timer to decide nothing is exactly the fast-forward cost the
                // event model is supposed to avoid.
                if (actor.Motivations.Responsibilities.Count > 0 || ctx.MyAssignment is not null)
                    world.Queue.Schedule(world.Now.AddDays(30), EventKind.RoleReview, actor.Id,
                        "his patch came up for review again");
                return "did nothing";
        }
    }
}
