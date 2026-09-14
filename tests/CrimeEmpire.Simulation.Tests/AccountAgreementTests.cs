using System.Reflection;
using System.Reflection.Emit;
using CrimeEmpire.Persistence.Session;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Strategy;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 016: the social consequence of a perceived account agreement — the mirror image of
/// milestone 006's <see cref="AccountConflict"/>/<see cref="Relations.RecordAccountConflict"/>, built
/// the same way and tested the same way. See <c>docs/CURRENT_MILESTONE.md</c> for the ruling each of
/// these pins, and <c>RelationalConsequenceTests.cs</c> for the conflict-direction original this
/// mirrors — deliberately not shared code between the two files, so the two cannot both be wrong
/// about the same fixture assumption.
///
/// The load-bearing pair is the natural seed-42 chain and the staged counterfactual, kept separate
/// per ruling 9: the natural-run test proves the real exchange, the real emitted
/// <see cref="AccountAgreement"/>, the real trust movement, and the real later score read, all off
/// the unmodified baseline scenario; the counterfactual proves the mechanism's behavioural relevance
/// off two deliberately staged, otherwise-identical fixtures, so neither test's evidence depends on
/// the other's.
/// </summary>
public sealed class AccountAgreementTests
{
    private static readonly DateTime At = new(1987, 3, 2, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Claim Beating = new(ClaimKind.PersonUsedViolence, "tommy", "bellini-grocery");
    private static readonly Claim Vulnerable = new(ClaimKind.TargetIsVulnerable, "bellini-grocery");

    // ---------------------------------------------------------------- the state machine (ruling 4)

    [Fact]
    public void News_is_neither_an_agreement_nor_a_conflict()
    {
        var listener = new Cognition();

        var receipt = listener.Receive(Affirm(0.8), "tommy", At);

        Assert.Null(receipt.Agreement);
        Assert.Null(receipt.Conflict);
    }

    [Fact]
    public void A_new_voice_agreeing_with_the_held_position_is_an_agreement()
    {
        var listener = new Cognition();
        listener.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, "someone", At);

        var receipt = listener.Receive(Affirm(0.8), "tommy", At.AddDays(1));

        var agreement = Assert.NotNull(receipt.Agreement);
        Assert.Null(receipt.Conflict);
        Assert.Equal("tommy", agreement.SpeakerId);
        Assert.Equal(Beating, agreement.Claim);
        Assert.Equal(Stance.Believes, agreement.AssertedStance);
        Assert.Equal(0.8, agreement.AssertedConfidence, 9);
        Assert.Equal(Stance.Believes, agreement.PriorStance);
        Assert.Equal(0.6, agreement.PriorConfidence, 9);
        Assert.Equal(SourceKind.Discovery, agreement.PriorSourceKind);
        Assert.Equal("someone", agreement.PriorSourceId);
    }

    /// <summary>
    /// Ruling 4's second agreement-producing case: a speaker who previously denied the claim and has
    /// since come round to agree with what the listener still holds. The prior denial is staged
    /// exactly as <c>RelationalConsequenceTests.Affirm_deny_affirm_emits_one_conflict_per_genuine_reversal</c>
    /// stages it (same starting confidence, same denial strength), because that test already proves
    /// this specific denial erodes the belief without displacing it — the listener still holds the
    /// claim when the speaker comes back round, which is what "while the listener still holds that
    /// direction" requires.
    /// </summary>
    [Fact]
    public void A_speaker_reversing_into_agreement_while_the_listener_still_holds_it_is_an_agreement()
    {
        var listener = Character("salvatore");
        Relations.Establish(listener, "tommy", trust: 0.90);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, listener.Id, At);

        var denial = listener.Cognition.Receive(Denial(0.9), "tommy", At.AddDays(1));
        Assert.NotNull(denial.Conflict);
        Assert.True(listener.Cognition.Holds(Beating), "the fixture must not displace the belief, or this is not the case under test");

        var comesRound = listener.Cognition.Receive(Affirm(0.8), "tommy", At.AddDays(2));

        var agreement = Assert.NotNull(comesRound.Agreement);
        Assert.Null(comesRound.Conflict);
        Assert.Equal("tommy", agreement.SpeakerId);
    }

    /// <summary>
    /// Ruling 4's explicit exclusion: the trigger must not broaden to every same-direction account.
    /// The same voice affirming again, without having reversed in between, is still one man's single
    /// voice — <c>Cognition.Receive</c>'s own comment calls this "firming up or softening" — and must
    /// not fire agreement a second time even though the direction still agrees.
    /// </summary>
    [Fact]
    public void The_same_speaker_reaffirming_without_reversal_is_not_an_agreement()
    {
        var listener = new Cognition();
        listener.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, "someone", At);

        var first = listener.Receive(Affirm(0.7), "tommy", At.AddDays(1));
        Assert.NotNull(first.Agreement);

        var second = listener.Receive(Affirm(0.9), "tommy", At.AddDays(2));

        Assert.Null(second.Agreement);
        Assert.Null(second.Conflict);
    }

    [Fact]
    public void Verbatim_repetition_is_not_an_agreement()
    {
        var listener = new Cognition();
        listener.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, "someone", At);

        var first = listener.Receive(Affirm(0.8), "tommy", At.AddDays(1));
        Assert.NotNull(first.Agreement);

        var second = listener.Receive(Affirm(0.8), "tommy", At.AddDays(2));

        Assert.Null(second.Agreement);
        Assert.Null(second.Conflict);
    }

    [Fact]
    public void Disagreement_is_a_conflict_never_an_agreement()
    {
        var listener = new Cognition();
        listener.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, "someone", At);

        var receipt = listener.Receive(Denial(0.9), "tommy", At.AddDays(1));

        Assert.NotNull(receipt.Conflict);
        Assert.Null(receipt.Agreement);
    }

    // ---------------------------------------------------------------- mutual exclusivity (ruling 3)

    /// <summary>
    /// The nullable fields on <see cref="Receipt"/> do not enforce this by themselves — ruling 3 is
    /// explicit that the type system claim would be false. This walks every branch of the state
    /// machine above and checks the invariant directly against what <see cref="Cognition.Receive"/>
    /// actually returned, rather than trusting the shape of the type.
    /// </summary>
    [Fact]
    public void Agreement_and_conflict_are_never_both_present_across_the_whole_state_machine()
    {
        Receipt News()
        {
            var l = new Cognition();
            return l.Receive(Affirm(0.8), "tommy", At);
        }

        Receipt FreshAgreement()
        {
            var l = new Cognition();
            l.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, "x", At);
            return l.Receive(Affirm(0.8), "tommy", At.AddDays(1));
        }

        Receipt Reaffirmation()
        {
            var l = new Cognition();
            l.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, "x", At);
            l.Receive(Affirm(0.7), "tommy", At.AddDays(1));
            return l.Receive(Affirm(0.9), "tommy", At.AddDays(2));
        }

        Receipt VerbatimRepeat()
        {
            var l = new Cognition();
            l.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, "x", At);
            l.Receive(Affirm(0.8), "tommy", At.AddDays(1));
            return l.Receive(Affirm(0.8), "tommy", At.AddDays(2));
        }

        Receipt Conflict()
        {
            var l = new Cognition();
            l.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, "x", At);
            return l.Receive(Denial(0.8), "tommy", At.AddDays(1));
        }

        Receipt Reversal()
        {
            var l = Character("salvatore");
            Relations.Establish(l, "tommy", trust: 0.90);
            l.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, l.Id, At);
            l.Cognition.Receive(Denial(0.9), "tommy", At.AddDays(1));
            return l.Cognition.Receive(Affirm(0.8), "tommy", At.AddDays(2));
        }

        var cases = new (string Name, Receipt Receipt)[]
        {
            ("news", News()),
            ("fresh agreement", FreshAgreement()),
            ("reaffirmation", Reaffirmation()),
            ("verbatim repeat", VerbatimRepeat()),
            ("conflict", Conflict()),
            ("reversal into agreement", Reversal()),
        };

        foreach (var (name, receipt) in cases)
            Assert.False(receipt.Conflict is not null && receipt.Agreement is not null,
                $"[{name}] a single receipt carried both a conflict and an agreement");
    }

    // ---------------------------------------------------------------- ruling 5: directionality and boundary

    [Fact]
    public void Only_the_listener_relationship_moves()
    {
        var listener = Character("salvatore");
        var speaker = Character("tommy");
        Relations.Establish(listener, "tommy", trust: 0.30);
        Relations.Establish(speaker, "salvatore", trust: 0.30);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, listener.Id, At);

        Apply(listener, listener.Cognition.Receive(Affirm(0.8), "tommy", At.AddDays(1)));

        Assert.True(listener.Social.Toward("tommy").Trust > 0.30);
        Assert.Equal(0.30, speaker.Social.Toward("salvatore").Trust, 9);
    }

    [Fact]
    public void Trust_cannot_be_driven_above_one()
    {
        var listener = Character("salvatore");
        Relations.Establish(listener, "tommy", trust: 0.98);
        listener.Cognition.Learn(Beating, Stance.Knows, 1.0, SourceKind.Participant, listener.Id, At);

        Apply(listener, listener.Cognition.Receive(Affirm(1.0), "tommy", At.AddDays(1)));

        Assert.Equal(1.0, listener.Social.Toward("tommy").Trust, 9);
    }

    [Fact]
    public void Strength_is_how_firmly_he_held_it_times_how_firmly_it_was_agreed()
    {
        var listener = new Cognition();
        listener.Learn(Beating, Stance.Believes, 0.60, SourceKind.Discovery, "someone", At);

        var agreement = Assert.NotNull(listener.Receive(Affirm(0.50), "tommy", At.AddDays(1)).Agreement);

        Assert.Equal(0.60, agreement.PriorConfidence, 9);
        Assert.Equal(0.50, agreement.AssertedConfidence, 9);
        Assert.Equal(0.30, agreement.Strength, 9);
    }

    /// <summary>
    /// Ruling 1's proof, corrected twice. First after Codex's review of `66917c7`: the original
    /// version of this test computed its expected value from
    /// <c>Relations.AccountAgreementTrustGain</c> and compared it against production's own result —
    /// which cannot discriminate a defect that reads <c>ConflictTrustCost</c> instead, because both
    /// constants equal `0.35` today, and the test's own expected-value formula silently tracked
    /// whichever one production actually used. Every existing test, including that one, passed under
    /// Codex's exact mutation. The first fix made <c>AccountAgreementTrustGain</c> a plain mutable
    /// <c>static</c> field and varied its runtime value — which discriminated correctly, but Codex's
    /// second review found the fix itself was the defect: a publicly mutable field is process-global
    /// state with no persistence or replay story, reachable by any other test or code in the same
    /// process, which is exactly the kind of state this project's determinism guarantees exist to
    /// rule out.
    ///
    /// This version proves the same fact — which field <see cref="Relations.RecordAccountAgreement"/>
    /// actually reads — structurally, from the method's own compiled IL, with
    /// <see cref="Relations.AccountAgreementTrustGain"/> immutable again (<c>static readonly</c>).
    /// <see cref="StaticFieldsReadBy"/> walks the method body's bytecode instruction by instruction —
    /// built from <see cref="OpCodes"/>' own canonical operand-size metadata via reflection, rather
    /// than a hand-transcribed opcode table that could itself be silently wrong — and resolves every
    /// <c>ldsfld</c> it finds to the real <see cref="FieldInfo"/> being read. No runtime value is
    /// varied, and nothing outside this one read-only reflective walk is touched.
    /// </summary>
    [Fact]
    public void RecordAccountAgreement_reads_its_own_dedicated_field_not_conflicttrustcost()
    {
        var method = typeof(Relations).GetMethod(nameof(Relations.RecordAccountAgreement), BindingFlags.Public | BindingFlags.Static)!;

        var staticFieldsRead = StaticFieldsReadBy(method).ToList();

        Assert.Contains(staticFieldsRead, f => f.Name == nameof(Relations.AccountAgreementTrustGain));
        Assert.DoesNotContain(staticFieldsRead, f => f.Name == nameof(Relations.ConflictTrustCost));
    }

    [Fact]
    public void The_agreement_gain_and_the_conflict_cost_are_separate_fields_that_happen_to_agree_today()
    {
        // Ruling 1: a separately named field, not ConflictTrustCost reused. Equal at 0.35 today — a
        // provisional symmetric starting point — is checked here as a plain value fact; it is the
        // test above, not this one, that proves production actually reads the right field.
        Assert.Equal(0.35, Relations.AccountAgreementTrustGain, 9);
        Assert.Equal(0.35, Relations.ConflictTrustCost, 9);
    }

    /// <summary>
    /// Ruling 5: this must consume only <see cref="AccountAgreement"/> and reach nothing else. Two
    /// <see cref="ReportedClaim"/>s differing only in <see cref="ReportedClaim.ActualBasis"/> — the
    /// speaker's private truth, never the recipient's — must produce byte-identical agreements and
    /// identical trust movement, because <c>ActualBasis</c> never crosses into what
    /// <see cref="Cognition.Receive"/> reads to build one.
    /// </summary>
    [Fact]
    public void The_agreement_does_not_depend_on_the_speakers_private_actual_basis()
    {
        double TrustAfter(SourceKind actualBasis)
        {
            var listener = Character("salvatore");
            Relations.Establish(listener, "tommy", trust: 0.30);
            listener.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, listener.Id, At);

            var claimed = ReportedClaim.Misrepresenting(
                Beating, Stance.Believes, 0.8, claimed: SourceKind.Report, actual: actualBasis);
            Apply(listener, listener.Cognition.Receive(claimed, "tommy", At.AddDays(1)));
            return listener.Social.Toward("tommy").Trust;
        }

        double viaHonestParticipant = TrustAfter(SourceKind.Participant);
        double viaRumor = TrustAfter(SourceKind.Rumor);
        double viaReport = TrustAfter(SourceKind.Report);

        Assert.Equal(viaHonestParticipant, viaRumor, 9);
        Assert.Equal(viaHonestParticipant, viaReport, 9);
    }

    /// <summary>
    /// The same boundary, exercised through <c>Report.Candor</c> — which does not even reach
    /// <see cref="Cognition.Receive"/>'s parameter list, since <see cref="Reporting.Deliver"/> passes
    /// only the individual <see cref="ReportedClaim"/>s from <c>Report.Asserted</c>. Two reports
    /// differing only in <c>Candor</c>, with identical asserted claims, must land identically.
    /// </summary>
    [Fact]
    public void The_agreement_does_not_depend_on_the_reports_candor()
    {
        double TrustAfter(ReportCandor candor)
        {
            var world = Cast.Build(42, "baseline");
            var salvatore = world.Get("salvatore");
            Relations.Establish(salvatore, "tommy", trust: 0.30);
            salvatore.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, salvatore.Id, world.Now);

            var report = new Report(
                world.NextReportId(), "tommy", salvatore.Id, world.Now, candor,
                new[] { ReportedClaim.Honest(Beating, Stance.Believes, 0.8, SourceKind.Participant) },
                Array.Empty<Claim>(), "framing");

            Reporting.Deliver(world, report, salvatore);
            return salvatore.Social.Toward("tommy").Trust;
        }

        Assert.Equal(TrustAfter(ReportCandor.Candid), TrustAfter(ReportCandor.Partial), 9);
    }

    // ---------------------------------------------------------------- all three receipt paths (ruling 7)

    [Fact]
    public void A_report_that_agrees_gains_the_recipient_trust()
    {
        var world = Cast.Build(42, "baseline");
        var salvatore = world.Get("salvatore");
        var tommy = world.Get("tommy");
        Relations.Establish(salvatore, "tommy", trust: 0.30);
        salvatore.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, salvatore.Id, world.Now);

        var report = new Report(
            world.NextReportId(), tommy.Id, salvatore.Id, world.Now, ReportCandor.Candid,
            new[] { ReportedClaim.Honest(Beating, Stance.Believes, 0.9, SourceKind.Participant) },
            Array.Empty<Claim>(), "framing");

        Reporting.Deliver(world, report, salvatore);

        Assert.True(salvatore.Social.Toward("tommy").Trust > 0.30);
        Assert.Single(world.AccountAgreements);
        Assert.Equal("salvatore", world.AccountAgreements[0].ListenerId);
        Assert.Empty(world.AccountConflicts);
    }

    [Fact]
    public void An_assignment_briefing_that_agrees_gains_the_recipient_trust()
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");
        Relations.Establish(vincent, "salvatore", trust: 0.30);

        vincent.Cognition.Learn(Vulnerable, Stance.Believes, 0.7, SourceKind.Discovery, vincent.Id, world.Now);

        var assignment = new Assignment(
            world.NextAssignmentId(), "restore the harbour tribute", "salvatore", vincent.Id, Cast.Harbour,
            Array.Empty<string>(),
            new[] { ReportedClaim.Honest(Vulnerable, Stance.Believes, 0.9, SourceKind.Report) },
            world.Now, world.Now.AddDays(30));
        world.Org.Assignments.Add(assignment);

        world.Queue.Schedule(world.Now, EventKind.AssignmentDelivered, vincent.Id, "briefed",
            new EventPayload { AssignmentId = assignment.Id });
        Runner.Run(world, world.Now.AddMinutes(1));

        Assert.True(vincent.Social.Toward("salvatore").Trust > 0.30);
        Assert.Contains(world.AccountAgreements, a => a.ListenerId == "vincent" && a.Agreement.SpeakerId == "salvatore");
    }

    [Fact]
    public void A_delegation_briefing_that_agrees_gains_the_delegate_trust()
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");
        var tommy = world.Get("tommy");

        vincent.Cognition.Learn(Vulnerable, Stance.Believes, 0.9, SourceKind.Discovery, vincent.Id, world.Now);
        tommy.Cognition.Learn(Vulnerable, Stance.Believes, 0.6, SourceKind.Witness, tommy.Id, world.Now);

        double before = tommy.Social.Toward("vincent").Trust;
        Assert.True(before > 0, "the fixture needs a real starting relationship for this to be visible");

        Delegate(world, vincent, tommy);

        Assert.True(tommy.Social.Toward("vincent").Trust > before);
        Assert.Contains(world.AccountAgreements, a => a.ListenerId == "tommy" && a.Agreement.SpeakerId == "vincent");
    }

    // ---------------------------------------------------------------- the seed-42 legal-choice chain (ruling 8)

    /// <summary>
    /// A legal-choice path preserves the corroboration exchange: Vincent finishes the tailor
    /// personally, sends Tommy to the grocery, Tommy asks, and Salvatore's generated candid answer
    /// raises trust through the actual receipt. No belief, request or report is injected.
    /// This is a controlled-choice witness, not the fully autonomous baseline.
    /// </summary>
    [Fact]
    public void Salvatores_generated_answer_to_tommy_raises_tommys_trust_when_chosen()
    {
        var world = Cast.Build(42, "baseline");
        var tommy = world.Get("tommy");

        double before = tommy.Social.Toward("salvatore").Trust;
        Assert.Equal(0.30, before, 9);

        var prepared = AdvanceToRequestedCorroboration(world);
        Assert.Equal(EventKind.RoleReview, prepared.Trigger.Kind);

        var answer = prepared.Scored
            .Select(s => s.Candidate)
            .Single(c => c.Kind == ActionKind.ReportToSuperior && c.TargetId == "tommy"
                         && c.Candor == ReportCandor.Candid && c.AnsweringClaim is { } a && a.Equals(Vulnerable));

        // At this corrected legal-choice fork, the candid answer is also his preferred option.
        var topRanked = prepared.Scored.OrderByDescending(s => s.Total).ThenBy(s => s.Candidate.Id, StringComparer.Ordinal).First();
        Assert.Equal(answer.Id, topRanked.Candidate.Id);

        Pipeline.Resolve(prepared, answer.Id);

        var agreement = Assert.Single(world.AccountAgreements,
            a => a.ListenerId == "tommy" && a.Agreement.SpeakerId == "salvatore" && a.Agreement.Claim.Equals(Vulnerable));

        Assert.DoesNotContain(world.AccountConflicts, c => c.ListenerId == "tommy" && c.Conflict.SpeakerId == "salvatore");

        double expected = Math.Clamp(before + Relations.AccountAgreementTrustGain * agreement.Agreement.Strength, 0, 1);
        Assert.Equal(expected, tommy.Social.Toward("salvatore").Trust, 9);
        Assert.True(tommy.Social.Toward("salvatore").Trust > before);
    }

    /// <summary>
    /// The read: a later Tommy decision about an entirely different claim,
    /// <c>PersonUsedViolence</c>, scores its report-related components differently once the
    /// corroboration above has happened, through the existing, unmodified <c>AddLoyaltyParts</c>
    /// component of <see cref="ActionKind.ReportToSuperior"/> and <see cref="ReportCandor.Partial"/> in
    /// <c>Utility.cs</c>. This is the controlled-choice half of ruling 9's pair — see
    /// <see cref="The_agreement_measurably_changes_a_later_staged_score"/> for the staged counterfactual
    /// half.
    ///
    /// <b>Retargeted alongside the test above, for the identical reason.</b> The trust movement is real
    /// (proved above); what changed is that it no longer arrives on its own, so this reads the same
    /// candidate's score before and after the identical controlled choice rather than before and after
    /// an autonomous date.
    /// </summary>
    [Fact]
    public void A_later_report_score_reads_the_trust_the_chosen_agreement_raised()
    {
        var undisturbed = Cast.Build(42, "baseline");
        double trustBefore = undisturbed.Get("tommy").Social.Toward("salvatore").Trust;

        var afterAgreement = Cast.Build(42, "baseline");
        var prepared = AdvanceToRequestedCorroboration(afterAgreement);
        var answer = prepared.Scored
            .Select(s => s.Candidate)
            .Single(c => c.Kind == ActionKind.ReportToSuperior && c.TargetId == "tommy"
                         && c.Candor == ReportCandor.Candid && c.AnsweringClaim is { } a && a.Equals(Vulnerable));
        Pipeline.Resolve(prepared, answer.Id);
        double trustAfter = afterAgreement.Get("tommy").Social.Toward("salvatore").Trust;

        Assert.True(trustAfter > trustBefore);

        var reportCandid = new Candidate("answer:salvatore", ActionKind.ReportToSuperior, "test", "give his account")
        { TargetId = "salvatore", Domain = Cast.Harbour, Candor = ReportCandor.Candid };

        double ScoreFor(World world)
        {
            var tommy = world.Get("tommy");
            var ctx = Context(world, tommy);
            var rng = Rng.ForOccasion(world.Seed, "test|fixed");
            return Utility.Score(reportCandid, tommy.View, tommy.Psychology, ctx.Perceived, ctx.Agenda, rng, ctx.CurrentExecution).RelationshipNet();
        }

        Assert.NotEqual(ScoreFor(undisturbed), ScoreFor(afterAgreement));
    }

    /// <summary>
    /// Reaches the corroboration fork through deliberately selected available actions.
    /// All intermediate consequence, cognition and request updates use the production pipeline.
    /// </summary>
    // Deliberately finish the first shop personally, leaving the grocery as Tommy's first job.
    // This preserves the existing corroboration witness through legal choices, not staged beliefs.
    private static PreparedDecision AdvanceToRequestedCorroboration(World world)
    {
        var opening = AdvanceToPause(world, "vincent");
        Pipeline.Resolve(opening, opening.Available.Single(c => c.Kind == ActionKind.StartStrategy
            && c.TargetId == Cast.Tailor && c.Method == CoercionMethod.Force).Id);
        bool delegated = false;
        bool asked = false;
        var seen = new List<string>();
        for (int guard = 0; guard < 1000; guard++)
        {
            var step = Runner.Step(world, Cast.Start.AddDays(90), !delegated ? "vincent" : asked ? "salvatore" : "tommy");
            if (step.Status == StepStatus.Exhausted) break;
            if (step.Awaiting is not { } prepared) continue;
            seen.Add($"{prepared.Actor.Id} {prepared.At:d} " + string.Join(" | ", prepared.Available.Select(c => c.Id)));
            if (asked && prepared.Available.Any(c => c.AnsweringClaim == Vulnerable
                && c.TargetId == "tommy" && c.Candor == ReportCandor.Candid)) return prepared;
            if (!delegated)
            {
                var handover = prepared.Available.FirstOrDefault(c => c.Kind == ActionKind.DelegateStrategy && c.TargetId == "tommy");
                var start = prepared.Available.FirstOrDefault(c => c.Kind == ActionKind.StartStrategy && c.TargetId == Cast.Grocery && c.Method == CoercionMethod.Threaten);
                Pipeline.Resolve(prepared, handover?.Id ?? start?.Id);
                delegated = handover is not null;
                continue;
            }
            var ask = prepared.Available.FirstOrDefault(c => c.Kind == ActionKind.SeekCorroboration && c.TargetId == "salvatore" && c.AboutClaim == Vulnerable);
            Pipeline.Resolve(prepared, ask?.Id);
            if (ask is not null) asked = true;
        }
        throw new InvalidOperationException("The real corroboration choice/answer was not reached. " + string.Join("\n", seen));
    }

    private static PreparedDecision AdvanceToPause(World world, string controlled)
    {
        for (int guard = 0; guard < 5000; guard++)
        {
            var step = Runner.Step(world, DateTime.MaxValue, controlled);
            if (step.Status == StepStatus.AwaitingChoice) return step.Awaiting!;
            if (step.Status == StepStatus.Exhausted)
                throw new InvalidOperationException($"queue exhausted before {controlled} ever paused");
        }
        throw new InvalidOperationException("guard exceeded");
    }

    // ---------------------------------------------------------------- ruling 9: the staged counterfactual

    /// <summary>
    /// The counterfactual, kept deliberately separate from the natural-run pair above: two
    /// otherwise-identical staged fixtures, one with the agreement consequence applied through the
    /// real <see cref="Cognition.Receive"/>/<see cref="Relations.RecordAccountAgreement"/> path, one
    /// without, scoring the same candidate with the same deterministic noise stream. No production
    /// switch, no stubbed receipt call, no manipulation of the natural fixture — exactly the shape
    /// <c>RelationalConsequenceTests.A_conflict_changes_a_later_score</c> already uses for the
    /// conflict direction.
    /// </summary>
    [Fact]
    public void The_agreement_measurably_changes_a_later_staged_score()
    {
        double undisturbed = ReportScoreAfter(agreement: false);
        double agreed = ReportScoreAfter(agreement: true);

        Assert.True(agreed > undisturbed,
            $"a corroborated man should weigh reporting to that person differently: " +
            $"{agreed} was not above {undisturbed}");
    }

    // ---------------------------------------------------------------- actor-neutral and deterministic

    /// <summary>
    /// Ruling 10: controlled-versus-autonomous equivalence. Vincent player-controlled, resolving
    /// every pause with the pipeline's own preference through <c>SimulationSession.ResolveAutomatically</c>,
    /// must reach the identical <c>World.AccountAgreements</c> and trust state as nobody being
    /// controlled at all — the agreement mechanism sits inside <c>Commit</c>/<c>Reporting</c>/<c>Runner</c>,
    /// not inside any player-only branch, so it must not care which path drove it there.
    /// </summary>
    [Fact]
    public void The_agreement_mechanism_is_identical_whether_vincent_is_controlled_or_autonomous()
    {
        var end = Cast.Start.AddDays(90); // M028: the agreeing account arrives later in the parallel history.

        var autonomous = SimulationSession.Start(42, "baseline", controlledCharacterId: null, viewpointCharacterId: "tommy");
        autonomous.AdvanceTo(end);

        var controlled = SimulationSession.Start(42, "baseline", "vincent");
        controlled.AdvanceTo(end);
        while (controlled.Status == SessionStatus.AwaitingChoice)
        {
            controlled.ResolveAutomatically();
            if (controlled.Status == SessionStatus.Ready) controlled.AdvanceTo(end);
        }

        Assert.Equal(
            TraceWriter.Render(autonomous.World, "baseline", false),
            TraceWriter.Render(controlled.World, "baseline", false));

        Assert.Equal(autonomous.World.AccountAgreements.Count, controlled.World.AccountAgreements.Count);
        Assert.True(autonomous.World.AccountAgreements.Count > 0, "the run needs to actually exercise the mechanism to prove anything");
        Assert.Equal(
            autonomous.World.Get("tommy").Social.Toward("salvatore").Trust,
            controlled.World.Get("tommy").Social.Toward("salvatore").Trust, 9);
    }

    [Fact]
    public void The_natural_chain_is_deterministic_across_independent_runs()
    {
        double TrustAfterIndependentRun()
        {
            var world = Cast.Build(42, "baseline");
            Runner.Run(world, new DateTime(1987, 4, 10, 0, 0, 0));
            return world.Get("tommy").Social.Toward("salvatore").Trust;
        }

        Assert.Equal(TrustAfterIndependentRun(), TrustAfterIndependentRun(), 9);
    }

    /// <summary>
    /// Milestone 015's replay must reconstruct the agreement consequence exactly, not merely the
    /// trace text: a save taken partway through the chain, loaded and continued, must reach the same
    /// <c>AccountAgreements</c> and trust as an uninterrupted run — through
    /// <see cref="PersistentSession"/>'s real save/load, not a hand-rolled comparison.
    /// </summary>
    [Fact]
    public void Save_and_load_replay_reproduces_the_agreement_and_trust_state()
    {
        var end = Cast.Start.AddDays(90); // M028: the agreeing account arrives later in the parallel history.
        var split = new DateTime(1987, 4, 2, 0, 0, 0); // before the 6 April question, well short of the answer

        var uninterrupted = PersistentSession.Start(42, "baseline", controlledCharacterId: null, viewpointCharacterId: "tommy");
        uninterrupted.AdvanceDays((int)Math.Ceiling((end - uninterrupted.StartedOn).TotalDays));

        string path = Path.Combine(Path.GetTempPath(), $"ce-agreement-test-{Guid.NewGuid():N}.db");
        try
        {
            var toSave = PersistentSession.Start(42, "baseline", controlledCharacterId: null, viewpointCharacterId: "tommy");
            toSave.AdvanceDays((int)Math.Ceiling((split - toSave.StartedOn).TotalDays));
            toSave.Save(path);

            var loaded = PersistentSession.Load(path);
            loaded.AdvanceDays((int)Math.Ceiling((end - split).TotalDays));

            Assert.Equal(
                TraceWriter.Render(uninterrupted.InnerSession.World, "baseline", false),
                TraceWriter.Render(loaded.InnerSession.World, "baseline", false));

            Assert.Equal(uninterrupted.InnerSession.World.AccountAgreements.Count, loaded.InnerSession.World.AccountAgreements.Count);
            Assert.True(uninterrupted.InnerSession.World.AccountAgreements.Count > 0);
            Assert.Equal(
                uninterrupted.InnerSession.World.Get("tommy").Social.Toward("salvatore").Trust,
                loaded.InnerSession.World.Get("tommy").Social.Toward("salvatore").Trust, 9);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ---------------------------------------------------------------- fixtures

    private static ReportedClaim Denial(double confidence)
        => ReportedClaim.Misrepresenting(
            Beating, Stance.Rejects, confidence, claimed: SourceKind.Report, actual: SourceKind.Participant);

    private static ReportedClaim Affirm(double confidence)
        => ReportedClaim.Honest(Beating, Stance.Believes, confidence, SourceKind.Participant);

    private static void Apply(Character listener, Receipt receipt)
    {
        if (receipt.Conflict is { } conflict) Relations.RecordAccountConflict(listener, conflict, Cast.Start);
        if (receipt.Agreement is { } agreement) Relations.RecordAccountAgreement(listener, agreement, Cast.Start);
    }

    private static Character Character(string id) => new()
    {
        Id = id,
        Name = id,
        RoleTitle = "test",
        Capabilities = new Capabilities(new Dictionary<Skill, double>(), 1, 1000, 1, new[] { Cast.Harbour }),
        Psychology = new Psychology(new Dictionary<Trait, double>(), new Dictionary<Drive, double>()),
    };

    private static void Delegate(World world, Character from, Character to)
    {
        from.Execution.Strategy = new StrategyInstance
        {
            OwnerId = from.Id,
            LocalSequence = from.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            Method = CoercionMethod.Threaten,
            StartedAt = world.Now,
            Deadline = world.Now.AddDays(30),
        };

        var candidate = new Candidate($"delegate:{to.Id}", ActionKind.DelegateStrategy, "test", "hand it over")
        { TargetId = to.Id, Domain = Cast.Harbour };

        var ctx = Context(world, from);
        Commit.Apply(world, from, candidate, ctx.Agenda, ctx, new List<string>());
    }

    private static GeneratorContext Context(World world, Character actor)
    {
        var perceived = new PerceivedSituation(
            actor.Id, world.Now, actor.Cognition.Records, actor.Cognition.Testimony);

        return new GeneratorContext(
            actor.View, perceived,
            new Agenda(AgendaKind.DischargeResponsibility, "get the harbour earning", "assigned", Cast.Harbour),
            world.Now,
            new ScheduledEvent
            {
                Id = 1,
                Time = world.Now,
                Kind = EventKind.RoleReview,
                OwnerId = actor.Id,
                Cause = "test",
            },
            world.Org.OfficeForDomain(Cast.Harbour), null, Array.Empty<Policy>(),
            Pipeline.SuperiorOf(world, actor), Pipeline.SubordinatesOf(world, actor),
            Pipeline.OrgMembersOf(world, actor),
            Acquaintance.KnownTo(world, actor),
            Array.Empty<Report>(), Array.Empty<InformationRequest>(), new[] { Cast.Grocery },
            Pipeline.SubordinatesOf(world, actor).Where(id => Pipeline.AvailableToExecute(world, id)).ToList(),
            Strategies.CurrentExecution(world, actor));
    }

    /// <summary>
    /// One staged Tommy weighing an answer to Salvatore, optionally after Salvatore has corroborated
    /// something Tommy already held. Everything but the agreement is held identical, including the
    /// noise stream — mirrors <c>RelationalConsequenceTests.ReportScoreAfter</c> exactly, sign
    /// reversed.
    /// </summary>
    private static double ReportScoreAfter(bool agreement)
    {
        var world = Cast.Build(42, "baseline");
        var tommy = world.Get("tommy");
        Relations.Establish(tommy, "salvatore", trust: 0.30, obligation: 0.40);

        tommy.Cognition.Learn(Vulnerable, Stance.Believes, 0.7, SourceKind.Discovery, tommy.Id, world.Now);

        if (agreement)
            Apply(tommy, tommy.Cognition.Receive(
                ReportedClaim.Honest(Vulnerable, Stance.Believes, 0.8, SourceKind.Report),
                "salvatore", world.Now));

        var candidate = new Candidate("answer:salvatore", ActionKind.ReportToSuperior, "test", "give his account")
        { TargetId = "salvatore", Domain = Cast.Harbour, Candor = ReportCandor.Candid };

        var ctx = Context(world, tommy);
        var rng = Rng.ForOccasion(world.Seed, "test|fixed");
        return Utility.Score(candidate, tommy.View, tommy.Psychology, ctx.Perceived, ctx.Agenda, rng, ctx.CurrentExecution).RelationshipNet();
    }

    // ---------------------------------------------------------------- minimal IL walk (ruling 1's proof)

    /// <summary>
    /// Every static field <paramref name="method"/>'s compiled IL reads via <c>ldsfld</c>, resolved
    /// to the real <see cref="FieldInfo"/>. A minimal, linear bytecode walk — not a full decompiler —
    /// built specifically to answer "which static fields does this one method read", which is all
    /// <see cref="RecordAccountAgreement_reads_its_own_dedicated_field_not_conflicttrustcost"/> needs.
    ///
    /// Operand sizes come from <see cref="OpCodes"/>' own <c>OperandType</c> metadata, read via
    /// reflection over every public static <see cref="OpCode"/> field the BCL declares — not a
    /// hand-transcribed table, which could be wrong in exactly the way this test exists to rule out
    /// for production code. <c>InlineSwitch</c> is the one variable-length operand in CIL and is
    /// handled explicitly; every other operand shape has a fixed size.
    /// </summary>
    private static IEnumerable<FieldInfo> StaticFieldsReadBy(MethodInfo method)
    {
        var body = method.GetMethodBody() ?? throw new InvalidOperationException($"{method} has no method body");
        byte[] il = body.GetILAsByteArray() ?? throw new InvalidOperationException($"{method} has no IL bytes");
        var module = method.Module;
        var opcodesByValue = OpCodesByValue();

        int i = 0;
        while (i < il.Length)
        {
            short value;
            if (il[i] == 0xFE)
            {
                value = (short)(0xFE00 | il[i + 1]);
                i += 2;
            }
            else
            {
                value = il[i];
                i += 1;
            }

            if (!opcodesByValue.TryGetValue(value, out var opcode))
                throw new InvalidOperationException(
                    $"unrecognised IL opcode 0x{value:X} at offset {i} while scanning {method} — " +
                    "this walker's opcode table (built from OpCodes' own metadata) does not cover it.");

            if (opcode.OperandType == OperandType.InlineSwitch)
            {
                int caseCount = BitConverter.ToInt32(il, i);
                i += 4 + 4 * caseCount;
                continue;
            }

            int operandSize = opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI
                    or OperandType.InlineMethod or OperandType.InlineSig or OperandType.InlineString
                    or OperandType.InlineTok or OperandType.InlineType or OperandType.ShortInlineR => 4,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                _ => throw new InvalidOperationException(
                    $"unhandled operand type {opcode.OperandType} for {opcode.Name} while scanning {method}"),
            };

            if (opcode.Value == OpCodes.Ldsfld.Value || opcode.Value == OpCodes.Ldsflda.Value)
            {
                int token = BitConverter.ToInt32(il, i);
                yield return module.ResolveField(token)
                    ?? throw new InvalidOperationException($"token 0x{token:X} did not resolve to a field in {method}");
            }

            i += operandSize;
        }
    }

    private static Dictionary<short, OpCode> OpCodesByValue()
    {
        var table = new Dictionary<short, OpCode>();
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(OpCode)) continue;
            var opcode = (OpCode)field.GetValue(null)!;
            table[opcode.Value] = opcode;
        }
        return table;
    }
}
