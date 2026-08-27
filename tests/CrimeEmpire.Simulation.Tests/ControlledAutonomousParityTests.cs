using System.Text;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using Xunit;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 019. `docs/milestones/018-the-player-can-see-what-their-choice-did.md`'s third
/// correction and `ROADMAP.md` both recorded an anomaly: at Tommy's natural first asked-to-account
/// decision in the baseline seed-42 scenario, controlling him and immediately calling
/// <see cref="SimulationSession.ResolveAutomatically"/> was said to pick a self-protective partial
/// report while the fully autonomous run answered candidly at the identical decision.
///
/// Milestone 019 investigated this end to end — the exact cited repro, compared stage by stage
/// through the real pipeline — and it did not reproduce, at HEAD or at either of milestone 018's own
/// two prior commits. The "candid" half of the claim traces to
/// <see cref="ScenarioReachTests.And_the_executor_gives_his_delegator_an_account_of_it"/>, which
/// only asserts that a report exists whose <c>AnsweringClaim</c> matches the question — true of a
/// partial report that withholds precisely that claim as much as a candid one, because
/// <see cref="Reporting.Compose"/> stamps <c>AnsweringClaim</c> from the candidate regardless of
/// <see cref="ReportCandor"/>. No committed test ever actually compared the two paths' candour. See
/// `docs/milestones/019-controlled-autonomous-actor-parity-is-pinned.md` for the full investigation
/// account and `018-...md`'s appended correction for the retraction.
///
/// No production code changed for this milestone. What follows is the permanent regression that
/// pins the parity this investigation found, so a real future divergence is caught by a test rather
/// than by accident during an unrelated milestone the way this one nearly wasn't.
/// </summary>
public sealed class ControlledAutonomousParityTests
{
    private const int Seed = 42;
    private const string Baseline = "baseline";

    // ================================================================= the exact cited decision

    /// <summary>
    /// Stage 1 of the cited repro. <c>PreparedDecision.Scored</c> — the developer-facing candidate
    /// set and totals, deliberately never exposed through <see cref="SimulationSession"/>'s
    /// player-facing <see cref="PendingDecision"/> — is read directly off the same
    /// <see cref="Runner.Step"/> boundary <see cref="SimulationSession"/> itself pauses on, and
    /// compared against the identical decision's recorded <c>Scored</c> from a fully autonomous run.
    /// Comparing before either path resolves anything is what makes this "prepared candidates and
    /// totals," not "the choice happened to match."
    /// </summary>
    [Fact]
    public void Tommys_asked_to_account_decision_has_identical_prepared_candidates_and_totals()
    {
        var autoWorld = Cast.Build(Seed, Baseline);
        Runner.Run(autoWorld, Cast.Start.AddDays(90));
        var autoDecision = autoWorld.Decisions.First(d => d.ActorId == "tommy");

        var controlledWorld = Cast.Build(Seed, Baseline);
        var prepared = AdvanceToTommysPause(controlledWorld);

        Assert.Equal(autoDecision.TriggerEventId, prepared.Trigger.Id);
        Assert.Equal(autoDecision.Scored.Count, prepared.Scored.Count);
        for (int i = 0; i < autoDecision.Scored.Count; i++)
        {
            var expected = autoDecision.Scored[i];
            var actual = prepared.Scored[i];
            Assert.Equal(expected.Candidate.Id, actual.Candidate.Id);
            Assert.Equal(expected.Candidate.Candor, actual.Candidate.Candor);
            Assert.Equal(expected.Candidate.TargetId, actual.Candidate.TargetId);
            Assert.Equal(expected.Candidate.AnsweringClaim, actual.Candidate.AnsweringClaim);
            Assert.Equal(expected.Total, actual.Total, precision: 9);
        }
    }

    /// <summary>
    /// Stage 2 of the cited repro. Resolving that exact pause through the actual production
    /// <see cref="SimulationSession.ResolveAutomatically"/> — the method the anomaly named —
    /// produces the identical chosen candidate, explicitly including <see cref="ReportCandor"/>, and
    /// the identical consequence: the same Partial report, withholding the same claim, reaching
    /// Vincent, and the same request dropping out of <c>AwaitingAnswers</c>.
    /// </summary>
    [Fact]
    public void Tommys_asked_to_account_decision_resolves_automatically_to_the_identical_partial_report()
    {
        var autoWorld = Cast.Build(Seed, Baseline);
        Runner.Run(autoWorld, Cast.Start.AddDays(90));
        var autoDecision = autoWorld.Decisions.First(d => d.ActorId == "tommy");
        Assert.Equal(ReportCandor.Partial, autoDecision.Chosen?.Candidate.Candor);

        var session = SimulationSession.Start(Seed, Baseline, "tommy", "vincent");
        for (int guard = 0; guard < 5000 && session.Status != SessionStatus.AwaitingChoice; guard++)
            session.StepEvent();
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
        Assert.Equal("tommy", session.Pending!.ActorId);

        session.ResolveAutomatically();
        var controlledWorld = session.World;
        var controlledDecision = controlledWorld.Decisions.First(d => d.ActorId == "tommy");

        // The complete behavioral identity of the choice — kind, id, target, claims and, explicitly,
        // Candor — not only "an answer exists," which is all
        // ScenarioReachTests.And_the_executor_gives_his_delegator_an_account_of_it ever checked.
        Assert.Equal(autoDecision.ChosenActionSignature(), controlledDecision.ChosenActionSignature());
        Assert.Equal(ReportCandor.Partial, controlledDecision.Chosen?.Candidate.Candor);

        var autoReport = autoWorld.Reports.Single(r =>
            r.SenderId == "tommy" && r.RecipientId == "vincent"
            && Equals(r.AnsweringClaim, autoDecision.Chosen!.Candidate.AnsweringClaim));
        var controlledReport = controlledWorld.Reports.Single(r =>
            r.SenderId == "tommy" && r.RecipientId == "vincent"
            && Equals(r.AnsweringClaim, controlledDecision.Chosen!.Candidate.AnsweringClaim));

        Assert.Equal(ReportCandor.Partial, autoReport.Candor);
        Assert.Equal(autoReport.Candor, controlledReport.Candor);
        Assert.Equal(autoReport.Asserted.Select(a => a.Claim), controlledReport.Asserted.Select(a => a.Claim));
        Assert.Equal(autoReport.Withheld, controlledReport.Withheld);

        // A Partial report that withholds precisely the asked claim asserts nothing Vincent's own
        // cognition can register, so the request stays outstanding — structurally indistinguishable
        // from silence, per `DESIGN_DECISIONS.md`'s "Causal feedback" section. The parity claim is
        // that both paths land on that same outstanding state identically, not that the request
        // resolves.
        Assert.Contains(session.Snapshot().AwaitingAnswers, r => r.AskedId == "tommy");
    }

    /// <summary>Drives a fresh world to Tommy's first pause via the same <see cref="Runner.Step"/>
    /// boundary <see cref="SimulationSession"/> uses internally, returning the prepared decision
    /// before anything resolves.</summary>
    private static PreparedDecision AdvanceToTommysPause(World world)
    {
        for (int guard = 0; guard < 5000; guard++)
        {
            var step = Runner.Step(world, DateTime.MaxValue, "tommy");
            if (step.Status == StepStatus.AwaitingChoice) return step.Awaiting!;
            if (step.Status == StepStatus.Exhausted)
                throw new InvalidOperationException("queue exhausted before Tommy ever paused");
        }
        throw new InvalidOperationException("guard exceeded before Tommy ever paused");
    }

    // ================================================================= comparator guard

    /// <summary>
    /// The false-assurance pattern that produced the retracted "Tommy answers candidly" claim,
    /// guarded directly. Matching on <c>AnsweringClaim</c> alone — exactly what
    /// <see cref="ScenarioReachTests.And_the_executor_gives_his_delegator_an_account_of_it"/> checks
    /// — cannot tell a candid answer from a partial one that withholds the very claim asked about,
    /// because <see cref="Reporting.Compose"/> stamps <c>Report.AnsweringClaim</c> from
    /// <c>Candidate.AnsweringClaim</c> unconditionally, regardless of <see cref="ReportCandor"/>. The
    /// comparator this milestone's parity tests actually use —
    /// <see cref="DecisionRecord.ChosenActionSignature"/> — must not share that blind spot.
    /// </summary>
    [Fact]
    public void Answering_claim_alone_cannot_distinguish_candid_from_partial_but_the_chosen_action_signature_does()
    {
        var about = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 11);

        var candid = new Candidate(
            "answer:vincent:x", ActionKind.ReportToSuperior, "FromRelationship", "gave an account")
        {
            TargetId = "vincent",
            Candor = ReportCandor.Candid,
            AnsweringClaim = about,
        };
        var partial = new Candidate(
            "answer:vincent:x:partial", ActionKind.ReportToSuperior, "FromRelationship", "gave a partial account")
        {
            TargetId = "vincent",
            Candor = ReportCandor.Partial,
            AnsweringClaim = about,
        };

        // The exact shape of the false assurance: the one field the retracted check compared is
        // equal, even though the two candidates are not the same choice.
        Assert.Equal(candid.AnsweringClaim, partial.AnsweringClaim);
        Assert.NotEqual(candid.Candor, partial.Candor);

        var candidRecord = Staged("tommy", candid);
        var partialRecord = Staged("tommy", partial);

        Assert.NotEqual(candidRecord.ChosenActionSignature(), partialRecord.ChosenActionSignature());
    }

    private static DecisionRecord Staged(string actorId, Candidate chosen)
    {
        var breakdown = new ScoreBreakdown(chosen, 1.0, 0.0, Array.Empty<ScoreComponent>());
        return new DecisionRecord(
            1, Cast.Start, actorId, actorId,
            0, EventKind.RoleReview, "staged: an authored cause that must never reach a player",
            new Agenda(AgendaKind.Idle, "nothing pressing", "no agenda cleared the threshold"),
            Array.Empty<InformationRecord>(), Array.Empty<Candidate>(), Array.Empty<Rejection>(),
            new[] { breakdown }, breakdown, "staged outcome",
            Array.Empty<string>(), Array.Empty<string>());
    }

    // ================================================================= permanent parity sweep

    /// <summary>
    /// The bounded sweep this milestone's investigation ran ad hoc, promoted to a permanent
    /// regression. Every variant, every character individually controlled with every pause
    /// immediately auto-resolved, across the complete 90-day seed-42 horizon, compared against a
    /// fully autonomous run of the identical variant. The comparison is not limited to the
    /// controlled character's own decisions: since nothing about being controlled should touch
    /// anyone else either, the fingerprint below covers every actor's decisions, every report and
    /// request, business tribute status, organisational conditions, and every pairwise relationship
    /// — meaningful final world state, not only decision identity. The viewpoint is deliberately kept
    /// different from the controlled character throughout, so every iteration also exercises the
    /// separate invariant that viewpoint identity cannot influence simulation behaviour.
    /// </summary>
    [Fact]
    public void Auto_resolved_control_reproduces_fully_autonomous_history_for_every_variant_and_character()
    {
        DateTime end = Cast.Start.AddDays(90);

        foreach (var variant in Variants.All)
        {
            var autoWorld = Cast.Build(Seed, variant);
            Runner.Run(autoWorld, end);
            string autoFingerprint = WorldFingerprint(autoWorld);

            foreach (var charId in autoWorld.Characters.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                string viewpoint = charId == "salvatore" ? "vincent" : "salvatore";
                var session = SimulationSession.Start(Seed, variant, charId, viewpoint);
                var controlledWorld = session.World;

                for (int guard = 0; guard < 20000 && session.Date < end; guard++)
                {
                    if (session.Status == SessionStatus.AwaitingChoice)
                    {
                        session.ResolveAutomatically();
                        continue;
                    }
                    session.StepEvent();
                }
                if (session.Status == SessionStatus.AwaitingChoice)
                    session.ResolveAutomatically();

                string controlledFingerprint = WorldFingerprint(controlledWorld);
                Assert.True(
                    autoFingerprint == controlledFingerprint,
                    $"variant '{variant}', controlling '{charId}' (viewpoint '{viewpoint}'): " +
                    "controlled+auto-resolve history diverged from the fully autonomous history.\n\n" +
                    $"AUTONOMOUS:\n{autoFingerprint}\n\nCONTROLLED:\n{controlledFingerprint}");
            }
        }
    }

    /// <summary>
    /// A deterministic text fingerprint of meaningful world state: every actor's decision history
    /// (via <see cref="DecisionRecord.ChosenActionSignature"/>), every report and request, business
    /// tribute status, organisational conditions, and every pairwise relationship. Not a hash — kept
    /// as readable text so a mismatch's assertion message is diagnostic rather than two opaque
    /// digests.
    /// </summary>
    private static string WorldFingerprint(World world)
    {
        var sb = new StringBuilder();

        sb.Append("DECISIONS|")
          .AppendJoin('|', world.Decisions.Select(d => d.ChosenActionSignature()))
          .Append('\n');

        sb.Append("REPORTS|")
          .AppendJoin('|', world.Reports.OrderBy(r => r.Id).Select(r =>
              $"{r.SenderId}->{r.RecipientId}:{r.Candor}:{r.AnsweringClaim}:" +
              $"asserted=[{string.Join(',', r.Asserted.Select(a => a.Claim))}]:" +
              $"withheld=[{string.Join(',', r.Withheld)}]"))
          .Append('\n');

        sb.Append("REQUESTS|")
          .AppendJoin('|', world.Requests.OrderBy(r => r.Id).Select(r => $"{r.AskerId}->{r.AskedId}:{r.About}"))
          .Append('\n');

        sb.Append("BUSINESSES|")
          .AppendJoin('|', world.Businesses.Values
              .OrderBy(b => b.Id, StringComparer.Ordinal)
              .Select(b => $"{b.Id}:paying={b.PayingTribute}"))
          .Append('\n');

        sb.Append("ORG|")
          .AppendJoin('|', Enum.GetValues<OrgCondition>().Select(k => $"{k}={world.Org.Condition(k):0.0000}"))
          .Append('\n');

        var ids = world.Characters.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        foreach (var a in ids)
        {
            foreach (var b in ids)
            {
                if (a == b) continue;
                var rel = world.Get(a).Social.Toward(b);
                sb.Append(
                    $"REL {a}->{b}: trust={rel.Trust:0.0000} fear={rel.Fear:0.0000} " +
                    $"obligation={rel.Obligation:0.0000} grievances={rel.Grievances.Count}\n");
            }
        }

        return sb.ToString();
    }
}
