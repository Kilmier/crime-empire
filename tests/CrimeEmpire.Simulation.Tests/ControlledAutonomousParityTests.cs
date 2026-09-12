using System.Reflection;
using System.Text;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Strategy;
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

    /// <summary>
    /// <see cref="Tommys_asked_to_account_decision_resolves_automatically_to_the_identical_partial_report"/>
    /// below reads Tommy's report specifically to Vincent, answering Vincent's own question about his
    /// own violence — milestone 019's cited repro. At seed 42, under the <see cref="Rng.ForOccasion"/>
    /// correction of 2026-09-09, Vincent's own discovery roll on that violence no longer lands, so the
    /// question that reaches Tommy first comes from Salvatore instead, and the test's hardcoded
    /// <c>RecipientId == "vincent"</c> check finds nothing. The other tests in this file read whatever
    /// Tommy's first decision actually is, whoever it answers, so parity holds for them regardless and
    /// they are unaffected. Same search, same seed, as <c>ScenarioReachTests</c>' identical constant.
    /// </summary>
    private const int AltSeedWhereVincentAsksTommy = 199;

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
    /// Vincent — and, since that report withholds precisely the claim Vincent asked about, it
    /// asserts nothing his own cognition can register, so the request stays identically outstanding
    /// in <c>AwaitingAnswers</c> on both paths rather than resolving.
    ///
    /// <b>Retargeted 2026-09-11 by milestone 024's sixth correction.</b> Seed 199's natural reach
    /// depended on a delegate autonomously reaching Force, now structurally impossible for any
    /// delegate in this cast. This test's own claim is controlled/autonomous parity at the
    /// asked-to-account decision, not natural Force emergence, so Vincent's choice of Force and his
    /// subsequent question are staged identically on both the autonomous and the controlled world —
    /// through <see cref="Commit.Apply"/> — and the decision compared is whichever one actually
    /// answers Vincent's question (matched on <c>AnsweringClaim</c>), not literally Tommy's first ever,
    /// since a staged origin does not guarantee no other decision of his intervenes first.
    ///
    /// <b>Also found while retargeting:</b> Tommy's own top choice here is now Candid, not Partial —
    /// the correction changed his knowledge and pressure situation enough that self-protection no
    /// longer wins this particular decision. The parity claim never depended on which candor wins,
    /// only that both paths land on the identical one, so the test is read off whichever candor
    /// actually wins rather than repinned to the old assumption.
    /// </summary>
    [Fact]
    public void Tommys_asked_to_account_decision_resolves_automatically_to_the_identical_report()
    {
        var autoWorld = Cast.Build(Seed, Baseline);
        StageViolenceAndQuestion(autoWorld);
        Runner.Run(autoWorld, autoWorld.Now.AddDays(20));
        var autoDecision = autoWorld.Decisions.First(d =>
            d.ActorId == "tommy" && d.Chosen?.Candidate.AnsweringClaim is { Kind: ClaimKind.PersonUsedViolence });

        var session = SimulationSession.Start(Seed, Baseline, "tommy", "vincent");
        StageViolenceAndQuestion(session.World);
        PendingDecision pending;
        for (int guard = 0; ; guard++)
        {
            if (guard > 20) throw new InvalidOperationException("Tommy never reached the asked-to-account decision");
            for (int step = 0; step < 5000 && session.Status != SessionStatus.AwaitingChoice; step++)
                session.StepEvent();
            Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
            pending = session.Pending!;
            if (pending.Options.Any(o => o.Description.Contains("got violent at", StringComparison.Ordinal)))
                break;
            session.ResolveAutomatically();
        }

        session.ResolveAutomatically();
        var controlledWorld = session.World;
        var controlledDecision = controlledWorld.Decisions.First(d =>
            d.ActorId == "tommy" && d.Chosen?.Candidate.AnsweringClaim is { Kind: ClaimKind.PersonUsedViolence });

        // The complete behavioral identity of the choice — kind, id, target, claims and, explicitly,
        // Candor — not only "an answer exists," which is all
        // ScenarioReachTests.And_the_executor_gives_his_delegator_an_account_of_it ever checked.
        Assert.Equal(autoDecision.ChosenActionSignature(), controlledDecision.ChosenActionSignature());
        Assert.Equal(autoDecision.Chosen?.Candidate.Candor, controlledDecision.Chosen?.Candidate.Candor);

        var autoReport = autoWorld.Reports.Single(r =>
            r.SenderId == "tommy" && r.RecipientId == "vincent"
            && Equals(r.AnsweringClaim, autoDecision.Chosen!.Candidate.AnsweringClaim));
        var controlledReport = controlledWorld.Reports.Single(r =>
            r.SenderId == "tommy" && r.RecipientId == "vincent"
            && Equals(r.AnsweringClaim, controlledDecision.Chosen!.Candidate.AnsweringClaim));

        Assert.Equal(autoReport.Candor, controlledReport.Candor);
        Assert.Equal(autoReport.Asserted.Select(a => a.Claim), controlledReport.Asserted.Select(a => a.Claim));
        Assert.Equal(autoReport.Withheld, controlledReport.Withheld);

        // Candid here, not Partial — a full account of precisely the asked claim, so it resolves the
        // request rather than leaving it outstanding, on both paths identically. The parity claim is
        // that both paths land on the identical outstanding-or-not state, whichever one that is.
        var autoSnapshot = PlayerView.Build(autoWorld, "vincent", autoWorld.Now);
        Assert.Equal(
            autoSnapshot.AwaitingAnswers.Any(r => r.AskedId == "tommy"),
            session.Snapshot().AwaitingAnswers.Any(r => r.AskedId == "tommy"));
        Assert.DoesNotContain(session.Snapshot().AwaitingAnswers, r => r.AskedId == "tommy");
    }

    /// <summary>
    /// The staged origin this test needs: Vincent delegates a permitted method to Tommy, Tommy — the
    /// current executor — is staged straight to Force at the <see cref="Commit"/> boundary (no
    /// generator can offer a crew-1 delegate that escalation), the operation runs to a real,
    /// resolved violence through the ordinary pipeline, and only then is Vincent's own question
    /// staged — the discovery roll that would ordinarily produce his suspicion is probabilistic and
    /// unreliable at this seed. Applied identically to both the autonomous and the controlled world
    /// so the comparison is fair.
    /// </summary>
    private static void StageViolenceAndQuestion(World world)
    {
        var vincent = world.Get("vincent");
        var tommy = world.Get("tommy");

        var startCtx = Context(world, vincent);
        Commit.Apply(world, vincent,
            new Candidate("start:tribute:grocery", ActionKind.StartStrategy, "test", "strong-arm bellini-grocery")
            { TargetId = Cast.Grocery, Strategy = StrategyKind.SecureTribute, Domain = Cast.Harbour, Method = CoercionMethod.Persuade },
            startCtx.Agenda, startCtx, new List<string>());
        var s = vincent.Execution.Strategy!;

        var delegateCtx = Context(world, vincent);
        Commit.Apply(world, vincent,
            new Candidate($"delegate:{s.Kind}:tommy", ActionKind.DelegateStrategy, "test", "hand it to tommy")
            { TargetId = "tommy", Strategy = s.Kind, Method = s.Method, Domain = s.Domain, RequiredCrew = 1 },
            delegateCtx.Agenda, delegateCtx, new List<string>());

        var alterCtx = Context(world, tommy);
        Commit.Apply(world, tommy,
            new Candidate($"alter:{s.Kind}:force", ActionKind.AlterStrategy, "test", "switch to force")
            { TargetId = s.TargetId, Strategy = s.Kind, Domain = s.Domain, Method = CoercionMethod.Force, BreachesPolicyId = "no-violence-harbour" },
            alterCtx.Agenda, alterCtx, new List<string>());

        Runner.Run(world, world.Now.AddDays(20));

        var violenceEventId = world.TruthLog.First(e => e.Kind == "violence").Id;
        var violenceClaim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, violenceEventId);
        vincent.Cognition.Learn(violenceClaim, Stance.Suspects, 0.5, SourceKind.Discovery, vincent.Id, world.Now);

        var askCtx = Context(world, vincent);
        Commit.Apply(world, vincent,
            new Candidate("ask:tommy:violence", ActionKind.SeekCorroboration, "test", "ask tommy directly")
            { TargetId = "tommy", AboutClaim = violenceClaim },
            askCtx.Agenda, askCtx, new List<string>());
    }

    private static GeneratorContext Context(World world, Character actor)
        => new(
            actor.View, Salience.Perceive(actor, world.Now),
            new Agenda(AgendaKind.DischargeResponsibility, "keep the harbour earning", "test", Cast.Harbour),
            world.Now,
            new ScheduledEvent { Id = 0, Time = world.Now, Kind = EventKind.RoleReview, OwnerId = actor.Id, Cause = "test" },
            MyOffice: null, MyAssignment: null, KnownPolicies: Array.Empty<Policy>(),
            SuperiorId: null, SubordinateIds: Array.Empty<string>(), OrgMemberIds: Array.Empty<string>(),
            AcquaintedIds: Array.Empty<string>(), ReportsSent: Array.Empty<Report>(),
            RequestsMade: Array.Empty<InformationRequest>(), VisibleTargets: Array.Empty<string>(),
            AvailableSubordinateIds: Array.Empty<string>(), CurrentExecution: Strategies.CurrentExecution(world, actor));

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

    /// <summary>
    /// Astra's accepted historical-audit finding L1. Observation provenance is future-relevant
    /// queued state: when the event resolves, these two fields decide whether the same claim lands
    /// as a rumour or an own discovery, and who or what the observer attributes it to. Each field
    /// therefore has to distinguish an otherwise-identical world before the event is consumed.
    /// </summary>
    [Fact]
    public void Pending_observation_provenance_fields_each_change_the_comprehensive_fingerprint()
    {
        string WithProvenance(SourceKind acquiredAs, string? attributedTo)
        {
            var world = Cast.Build(Seed, Baseline);
            world.Queue.Schedule(
                Cast.Start.AddYears(1),
                EventKind.ObservationOpportunity,
                "salvatore",
                "staged pending observation",
                new EventPayload
                {
                    Claims = new[] { new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 17) },
                    Discoverability = 0.5,
                    AcquiredAs = acquiredAs,
                    AttributedTo = attributedTo,
                });
            return ComprehensiveFingerprint(world);
        }

        string discovery = WithProvenance(SourceKind.Discovery, attributedTo: null);

        Assert.NotEqual(discovery, WithProvenance(SourceKind.Rumor, attributedTo: null));
        Assert.NotEqual(discovery, WithProvenance(SourceKind.Discovery, Cast.Harbour));
    }

    // ================================================================= permanent parity sweep

    /// <summary>
    /// The bounded sweep this milestone's investigation ran ad hoc, promoted to a permanent
    /// regression. Every variant, every character individually controlled with every pause
    /// immediately auto-resolved, across the complete 90-day seed-42 horizon, compared against a
    /// fully autonomous run of the identical variant. The comparison is not limited to the
    /// controlled character's own decisions or to option wording: <see cref="ComprehensiveFingerprint"/>
    /// covers every replay-relevant collection <see cref="World"/> holds — meaningful final world
    /// state, not only decision identity. The viewpoint is deliberately kept different from the
    /// controlled character throughout, so every iteration also exercises the separate invariant
    /// that viewpoint identity cannot influence simulation behaviour — though
    /// <see cref="Only_the_viewpoint_differing_does_not_change_simulation_history"/> below is the
    /// test that isolates that variable on its own, holding the controlled character fixed.
    /// </summary>
    [Fact]
    public void Auto_resolved_control_reproduces_fully_autonomous_history_for_every_variant_and_character()
    {
        DateTime end = Cast.Start.AddDays(90);

        foreach (var variant in Variants.All)
        {
            var autoWorld = Cast.Build(Seed, variant);
            Runner.Run(autoWorld, end);
            string autoFingerprint = ComprehensiveFingerprint(autoWorld);

            foreach (var charId in autoWorld.Characters.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                string viewpoint = charId == "salvatore" ? "vincent" : "salvatore";
                var session = SimulationSession.Start(Seed, variant, charId, viewpoint);
                DriveFullyAutoResolved(session, end);

                string controlledFingerprint = ComprehensiveFingerprint(session.World);
                Assert.True(
                    autoFingerprint == controlledFingerprint,
                    $"variant '{variant}', controlling '{charId}' (viewpoint '{viewpoint}'): " +
                    "controlled+auto-resolve history diverged from the fully autonomous history.\n\n" +
                    $"AUTONOMOUS:\n{autoFingerprint}\n\nCONTROLLED:\n{controlledFingerprint}");
            }
        }
    }

    // ================================================================= viewpoint-only parity

    /// <summary>
    /// A true viewpoint-only parity check, isolated from the autonomous-vs-controlled question the
    /// sweep above answers. Both sessions here are controlled, and both auto-resolve every pause —
    /// the only thing that differs between them is <see cref="SimulationSession.ViewpointCharacterId"/>.
    /// <see cref="PlayerSnapshot"/> is allowed, and expected, to differ between them (that is the
    /// entire point of a viewpoint); the underlying simulation <see cref="World"/> either session
    /// drives must not — viewpoint is presentation-only per `DESIGN_DECISIONS.md`'s player-boundary
    /// section, and this is what pins that as a behaviour rather than only an architectural
    /// intention.
    /// </summary>
    [Fact]
    public void Only_the_viewpoint_differing_does_not_change_simulation_history()
    {
        DateTime end = Cast.Start.AddDays(90);

        foreach (var variant in Variants.All)
        {
            var roster = Cast.Build(Seed, variant).Characters.Keys.OrderBy(k => k, StringComparer.Ordinal);

            foreach (var controlledId in roster)
            {
                string viewpointA = controlledId;
                string viewpointB = controlledId == "salvatore" ? "vincent" : "salvatore";

                var sessionA = SimulationSession.Start(Seed, variant, controlledId, viewpointA);
                DriveFullyAutoResolved(sessionA, end);

                var sessionB = SimulationSession.Start(Seed, variant, controlledId, viewpointB);
                DriveFullyAutoResolved(sessionB, end);

                string fingerprintA = ComprehensiveFingerprint(sessionA.World);
                string fingerprintB = ComprehensiveFingerprint(sessionB.World);
                Assert.True(
                    fingerprintA == fingerprintB,
                    $"variant '{variant}', controlling '{controlledId}': viewpoint '{viewpointA}' " +
                    $"vs. '{viewpointB}' produced different simulation histories, though only the " +
                    "viewpoint differed between the two sessions.\n\n" +
                    $"VIEWPOINT {viewpointA}:\n{fingerprintA}\n\nVIEWPOINT {viewpointB}:\n{fingerprintB}");
            }
        }
    }

    /// <summary>
    /// Drives a controlled session to exactly <paramref name="end"/>, auto-resolving every pause it
    /// reaches — the shared driving loop both the autonomous-vs-controlled sweep and the
    /// viewpoint-only check use, so the two tests differ only in what they compare, not in how they
    /// drive the session.
    ///
    /// Uses <see cref="SimulationSession.AdvanceTo"/>, not repeated <see cref="SimulationSession
    /// .StepEvent"/>: <c>StepEvent</c> pumps with an unbounded horizon
    /// (<see cref="DateTime.MaxValue"/>) by design — it is "handle whatever is next," not "handle
    /// whatever is next before this date" — so a guard loop built on it can process an event
    /// scheduled after <paramref name="end"/> before the loop's own <c>session.Date &lt; end</c>
    /// check ever notices. <c>Runner.Run(world, end)</c>, which the fully autonomous reference run
    /// uses, never crosses <paramref name="end"/> at all, because <c>Queue.Next(until)</c> bounds it.
    /// The first version of this method used <c>StepEvent</c> and produced exactly that: an extra,
    /// causally inert event processed past the 90-day horizon on the controlled side only, differing
    /// from the autonomous reference in <c>World.Now</c> and queue depth alone — a test-harness
    /// artefact <see cref="SimulationReplayTests.Snapshot"/>'s <c>now</c>/<c>queue</c> lines caught
    /// immediately, with every other line already identical. <c>AdvanceTo</c> resumes correctly
    /// through <see cref="SimulationSession.ResolveAutomatically"/> because resolving a pause calls
    /// the session's own <c>Resume()</c> against the fast-forward horizon it already recorded — the
    /// same mechanism a real caller fast-forwarding past a choice relies on.
    /// </summary>
    private static void DriveFullyAutoResolved(SimulationSession session, DateTime end)
    {
        session.AdvanceTo(end);
        for (int guard = 0; guard < 20000 && session.Status == SessionStatus.AwaitingChoice; guard++)
            session.ResolveAutomatically();
    }

    /// <summary>
    /// A deterministic text fingerprint of meaningful, replay-relevant world state. Built on top of
    /// <see cref="SimulationReplayTests.Snapshot"/> — the project's existing comprehensive replay
    /// comparator, covering the truth log, decisions, reports, requests, businesses, and every
    /// character's tier/strategy/relationship/cognition/testimony state — rather than re-deriving a
    /// second, narrower copy of the same comparison. Appended below is every remaining mutable
    /// collection an explicit audit of <c>World</c> and <c>Character</c> found <c>Snapshot</c> does
    /// not cover, because it was written for replay determinism rather than this milestone's parity
    /// question: each character's <c>Capabilities.Cash</c>/<c>Crew</c> (mutated by
    /// <c>Strategies.cs</c>'s tribute collection — the rest of <c>Capabilities</c> is fixed at
    /// scenario construction and never written during a run), <c>Execution.Intention</c> and
    /// <c>Commitments</c> and the <c>StrategyInstance</c> fields <c>Snapshot</c> omits
    /// (<c>DelegatedToId</c>, <c>Deadline</c>, <c>AssignmentId</c>, <c>BreachedPolicyId</c>,
    /// <c>FailedAttempts</c>, <c>PressureApplied</c>) and reconsideration state, every
    /// <c>Motivations</c> field (<c>Ambition</c>, <c>Responsibilities</c>, <c>Pressures</c>,
    /// <c>ImmediateNeeds</c> — none of which <c>Snapshot</c> reads at all),
    /// <c>SocialState.OrganizationId</c>, milestone 018's causal-feedback state
    /// (<c>AccountConflicts</c>/<c>AccountAgreements</c>), <c>Encounters</c>, every trace a truth-log
    /// entry carries, the organisation's own state (conditions, priorities, policies, offices,
    /// assignments), <c>World.ObservationOccasionKeys</c>, and — not only <c>Queue.Count</c>, which a
    /// prior correction found insufficient to catch a driving-loop bug that overshot the horizon —
    /// the full deterministic contents of every event still pending (read via <see cref="PendingEvents"/>,
    /// test-only reflection rather than a production accessor — see that method's own comment) plus
    /// which ones were cancelled and why. Not a hash — kept as readable text so a mismatch's
    /// assertion message is diagnostic rather than two opaque digests.
    ///
    /// What the audit confirmed is deliberately absent because nothing in the simulation loop writes
    /// it after scenario construction: <c>Psychology</c> (only ever reassigned by
    /// <c>Variants.cs</c> before a run starts), <c>Capabilities.Authority</c>/<c>Districts</c>, and
    /// <c>Motivations.Ambition</c> in practice (assignable, but the accepted cast only ever sets it
    /// once, in <c>Cast.cs</c>) — included anyway below since they cost nothing to carry and the
    /// point of an audit is not to trust that judgement silently.
    /// </summary>
    private static string ComprehensiveFingerprint(World world)
    {
        var sb = new StringBuilder();
        sb.Append(SimulationReplayTests.Snapshot(world)).Append('\n');

        foreach (var character in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            sb.Append(
                $"capabilities|{character.Id}|{character.Capabilities.Cash:0.0000}|" +
                $"{character.Capabilities.Crew}|{character.Capabilities.Authority}|" +
                $"{string.Join(',', character.Capabilities.Districts.OrderBy(d => d, StringComparer.Ordinal))}\n");

            sb.Append($"organization|{character.Id}|{character.Social.OrganizationId}\n");

            var m = character.Motivations;
            sb.Append($"ambition|{character.Id}|{m.Ambition}\n");
            sb.Append(
                "responsibilities|" + character.Id + "|" +
                string.Join(',', m.Responsibilities.Select(r => $"{r.Id}:{r.Description}:{r.Domain}")) + "\n");
            sb.Append(
                "pressures|" + character.Id + "|" +
                string.Join(',', m.Pressures.OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}={kv.Value:0.0000}")) + "\n");
            sb.Append("immediate-needs|" + character.Id + "|" + string.Join(',', m.ImmediateNeeds) + "\n");

            sb.Append($"intention|{character.Id}|{character.Execution.Intention}\n");

            foreach (var c in character.Execution.Commitments)
                sb.Append(
                    $"commitment|{character.Id}|{c.Id}|{c.Description}|{c.ToWhomId}|{c.Since:O}|" +
                    $"{c.Weight:0.0000}\n");

            var s = character.Execution.Strategy;
            // PolicyBreachDecisionMakerId is carried by SimulationReplayTests.Snapshot above: it is
            // behavioral actor identity, not scheduling noise, so both replay comparators name it.
            sb.Append(
                $"strategy-extra|{character.Id}|{s?.DelegatedToId}|{s?.Deadline:O}|{s?.AssignmentId}|" +
                $"{s?.BreachedPolicyId}|{s?.FailedAttempts}|{s?.PressureApplied}\n");

            sb.Append(
                $"reconsideration|{character.Id}|" +
                $"{string.Join(',', character.Execution.ReconsiderationTriggers)}|" +
                $"{character.Execution.NextReview:O}\n");
        }

        sb.Append("observation-occasions|").AppendJoin(',', world.ObservationOccasionKeys).Append('\n');

        sb.Append("queue-pending|")
          .AppendJoin('|', PendingEvents(world.Queue).Select(e =>
              $"{e.Id}:{e.Time:O}:{e.Kind}:{e.OwnerId}:{e.Cause}:{e.Payload.TargetId}:" +
              $"{e.Payload.AssignmentId}:{e.Payload.RelatedEventId}:{e.Payload.Strategy}:" +
              $"{e.Payload.StepIndex}:{e.Payload.Note}:{string.Join(',', e.Payload.Claims)}:" +
              $"{e.Payload.Discoverability:0.0000}:{e.Payload.AcquiredAs}:{e.Payload.AttributedTo}:" +
              $"{e.Payload.AboutClaim}:{e.Payload.StrategyOwnerId}:" +
              $"{e.Payload.StrategySequence}:{e.Payload.AdvanceOrdinal}:{e.Payload.OccasionKey}"))
          .Append('\n');
        sb.Append("queue-cancelled|")
          .AppendJoin('|', world.Queue.Cancelled.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}:{kv.Value}"))
          .Append('\n');

        foreach (var pc in world.AccountConflicts)
            sb.Append(
                $"conflict|{pc.ListenerId}|{pc.At:O}|{pc.Conflict.Claim}|{pc.Conflict.SpeakerId}|" +
                $"{pc.Conflict.AssertedStance}|{pc.Conflict.AssertedConfidence:0.0000}|" +
                $"{pc.Conflict.ClaimedBasis}|{pc.Conflict.PriorStance}|" +
                $"{pc.Conflict.PriorConfidence:0.0000}|{pc.Conflict.PriorSourceKind}|" +
                $"{pc.Conflict.PriorSourceId}\n");

        foreach (var pa in world.AccountAgreements)
            sb.Append(
                $"agreement|{pa.ListenerId}|{pa.At:O}|{pa.Agreement.Claim}|{pa.Agreement.SpeakerId}|" +
                $"{pa.Agreement.AssertedStance}|{pa.Agreement.AssertedConfidence:0.0000}|" +
                $"{pa.Agreement.ClaimedBasis}|{pa.Agreement.PriorStance}|" +
                $"{pa.Agreement.PriorConfidence:0.0000}|{pa.Agreement.PriorSourceKind}|" +
                $"{pa.Agreement.PriorSourceId}\n");

        foreach (var e in world.Encounters)
            sb.Append($"encounter|{e.WhoId}|{e.MetId}|{e.At:O}\n");

        foreach (var ev in world.TruthLog)
            foreach (var t in ev.Traces)
                sb.Append($"trace|{ev.Id}|{t.Kind}|{t.Description}|{t.DistrictId}|{t.Discoverability:0.0000}\n");

        sb.Append($"org-boss|{world.Org.BossId}\n");
        sb.Append("org-condition|")
          .AppendJoin('|', Enum.GetValues<OrgCondition>().Select(k => $"{k}={world.Org.Condition(k):0.0000}"))
          .Append('\n');
        foreach (var p in world.Org.Priorities)
            sb.Append($"org-priority|{p.Id}|{p.Description}|{p.Domain}|{p.Weight:0.0000}\n");
        foreach (var p in world.Org.Policies)
            sb.Append($"org-policy|{p.Id}|{p.Description}|{p.Kind}|{p.Domain}|{p.Strength:0.0000}\n");
        foreach (var o in world.Org.Offices.OrderBy(o => o.Title, StringComparer.Ordinal))
            sb.Append($"org-office|{o.Title}|{o.Domain}|{o.Authority}|{o.HolderId}\n");
        foreach (var a in world.Org.Assignments.OrderBy(a => a.Id))
            sb.Append(
                $"org-assignment|{a.Id}|{a.Objective}|{a.IssuerId}|{a.RecipientId}|{a.Domain}|" +
                $"{string.Join(',', a.Constraints)}|{string.Join(',', a.Disclosed)}|{a.IssuedAt:O}|" +
                $"{a.Deadline:O}\n");

        return sb.ToString();
    }

    /// <summary>
    /// Every event still pending in <paramref name="queue"/>, in (time, id) order — read without
    /// dequeuing, so inspecting it changes nothing about how <c>EventQueue.Next</c> will later drain
    /// it.
    ///
    /// <b>Test-only reflection, deliberately, and not a production accessor.</b> A prior version of
    /// this correction added an <c>internal EventQueue.PendingEvents</c> property to
    /// <c>src/CrimeEmpire.Simulation/Sim/EventQueue.cs</c> to reach the same state — read-only,
    /// additive, and behaviourally inert, but still a change to production source under a milestone
    /// whose explicit requirement was that production simulation code remain unchanged. Review
    /// rejected that: a milestone's own archive is not the authority that gets to grant itself an
    /// exception to its own guardrail, however narrow or well-reasoned. This reaches the same private
    /// <c>PriorityQueue&lt;ScheduledEvent, (DateTime, long)&gt;</c> field
    /// (<c>EventQueue</c>'s only backing store for pending events) via reflection instead, so
    /// `src/CrimeEmpire.Simulation/` stays byte-identical to the pre-milestone baseline. Fragile in
    /// the ordinary sense that any reflection-based test is — a rename of the private field breaks
    /// this method, not silently — which is an acceptable trade for not touching the file it reaches
    /// into.
    /// </summary>
    private static IReadOnlyList<ScheduledEvent> PendingEvents(EventQueue queue)
    {
        var field = typeof(EventQueue).GetField("_queue", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                "EventQueue no longer has a private '_queue' field; this reflection-based test " +
                "helper needs updating to match its current backing store.");

        var priorityQueue = (PriorityQueue<ScheduledEvent, (DateTime Time, long Seq)>)field.GetValue(queue)!;

        return priorityQueue.UnorderedItems
            .Select(item => item.Element)
            .OrderBy(e => e.Time)
            .ThenBy(e => e.Id)
            .ToList();
    }
}
