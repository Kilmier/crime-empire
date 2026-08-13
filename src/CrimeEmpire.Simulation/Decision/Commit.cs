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
                var s = new StrategyInstance
                {
                    Kind = c.Strategy!.Value,
                    Domain = c.Domain ?? "",
                    TargetId = c.TargetId,
                    Method = c.Method ?? CoercionMethod.Persuade,
                    StartedAt = world.Now,
                    Deadline = ctx.MyAssignment?.Deadline ?? world.Now.AddDays(30),
                    AssignmentId = agenda.AssignmentId,
                    BreachedPolicyId = c.BreachesPolicyId,
                };
                actor.Execution.Strategy = s;
                actor.Execution.Intention = c.Description;
                actor.Execution.Commitments.Add(new Commitment(
                    $"strategy:{s.Kind}:{s.TargetId}", c.Description, ctx.SuperiorId, world.Now, 0.6));

                Strategies.ScheduleNextStep(world, actor, s, $"{s.Label}: first step");
                reconsideration.Add("the target refuses outright");
                reconsideration.Add("he comes to believe police are watching");
                return $"began {s.Label}"
                       + (c.BreachesPolicyId is not null ? $" — knowingly against \"{c.BreachesPolicyId}\"" : "");
            }

            case ActionKind.ContinueStrategy:
            {
                var s = actor.Execution.Strategy!;
                Strategies.ScheduleNextStep(world, actor, s, $"{s.Label}: next step");
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
                // let the new method have its own turn at pressing.
                s.StepIndex = 2;
                s.PressureApplied = false;
                Strategies.ScheduleNextStep(world, actor, s, $"{s.Label}: escalated approach");
                reconsideration.Add("the escalation draws attention");
                return $"escalated from {was.ToString().ToLowerInvariant()} to {s.Method.ToString().ToLowerInvariant()} against {s.TargetId}"
                       + (c.BreachesPolicyId is not null ? $" — knowingly against \"{c.BreachesPolicyId}\"" : "");
            }

            case ActionKind.DelegateStrategy:
            {
                var s = actor.Execution.Strategy!;
                s.DelegatedToId = c.TargetId;
                var sub = world.Get(c.TargetId!);
                // The delegate learns enough to act, and no more. Information topology changes here.
                foreach (var claim in ctx.Perceived.Beliefs
                             .Where(b => b.Claim.Subject == s.TargetId && b.IsHeld)
                             .Take(2))
                    sub.Cognition.Learn(claim.Claim, Stance.Believes, claim.Confidence * 0.8,
                        SourceKind.Report, actor.Id, world.Now);

                sub.Execution.Commitments.Add(new Commitment(
                    $"strategy:{s.Kind}:{s.TargetId}", $"handle {s.Label} for {actor.Name}", actor.Id, world.Now, 0.7));

                Strategies.ScheduleNextStep(world, actor, s, $"{s.Label}: {sub.Name} takes it on");
                reconsideration.Add($"{sub.Name} reports back or fails to");
                return $"handed {s.Label} to {sub.Name}";
            }

            case ActionKind.PostponeStrategy:
            {
                var s = actor.Execution.Strategy;
                if (s is null) return "let matters sit";
                world.Queue.Schedule(world.Now.AddDays(7), EventKind.StrategyStep, actor.Id,
                    $"{s.Label}: picked back up after a pause",
                    new EventPayload { Strategy = s.Kind, TargetId = s.TargetId });
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
                actor.Execution.Commitments.RemoveAll(x => x.Id.StartsWith("strategy:", StringComparison.Ordinal));
                world.Queue.Schedule(world.Now.AddDays(10), EventKind.RoleReview, actor.Id,
                    "periodic review of his own patch");
                return $"dropped {s.Label}";
            }

            case ActionKind.ReportToSuperior:
            {
                var boss = world.Get(c.TargetId!);
                var report = Reporting.Compose(world, actor, boss, c, ctx.Perceived);
                Reporting.Deliver(world, report, boss);

                world.Queue.Schedule(world.Now.AddDays(1), EventKind.RoleReview, boss.Id,
                    $"{actor.Name} reported in");

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
                world.Record("request", actor.Id, other.Id,
                    $"{actor.Name} asked {other.Name} for his own account");

                // He asks; the other man decides for himself what to say. Waking the other
                // character is the whole point — a request that produced an answer directly would
                // be truth synchronisation wearing a question mark.
                world.Queue.Schedule(world.Now.AddDays(1), EventKind.RoleReview, other.Id,
                    $"{actor.Name} asked him directly what happened",
                    new EventPayload { TargetId = actor.Id, Note = "asked-to-account" });
                reconsideration.Add($"{other.Name} gives his account, or avoids giving one");
                return $"went to {other.Name} for his own account";
            }

            case ActionKind.SeekApproval:
            {
                var boss = world.Get(c.TargetId!);
                world.Queue.Schedule(world.Now.AddDays(1), EventKind.RoleReview, boss.Id,
                    $"{actor.Name} asked for latitude in {agenda.Domain}");
                if (actor.Execution.Strategy is { } s)
                    world.Queue.Schedule(world.Now.AddDays(5), EventKind.StrategyStep, actor.Id,
                        $"{s.Label}: waiting on {boss.Name}",
                        new EventPayload { Strategy = s.Kind, TargetId = s.TargetId });
                reconsideration.Add($"{boss.Name} answers, or does not");
                return $"asked {boss.Name} for room to move";
            }

            case ActionKind.Retaliate:
            {
                var target = world.Get(c.TargetId!);
                world.Record("retaliation", actor.Id, target.Id,
                    $"{actor.Name} moved against {target.Name}");
                target.Social.Grievances.Add(new Grievance(actor.Id, "moved against me", 0.5, world.Now));
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
