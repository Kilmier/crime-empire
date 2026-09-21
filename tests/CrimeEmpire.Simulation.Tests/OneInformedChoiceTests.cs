using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 030: one existing, source-limited assessment now reaches the assignment recipient and
/// changes the choice he evaluates. These tests keep capture, receipt, social consequences,
/// supervisory review and player presentation as separate proof surfaces so one cannot stand in for
/// another.
/// </summary>
public sealed class OneInformedChoiceTests
{
    private const int Seed = 42;
    private static readonly Claim GroceryRefusal =
        new(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
    private static readonly Claim GroceryVulnerability =
        new(ClaimKind.TargetIsVulnerable, Cast.Grocery);

    [Fact]
    public void Issuance_pairs_only_the_held_same_target_assessment_and_delayed_delivery_uses_the_snapshot()
    {
        var world = Cast.Build(Seed, "baseline");
        var assignment = AdvanceToAssignment(world);

        int refusal = IndexOf(assignment.Disclosed, GroceryRefusal);
        Assert.True(refusal >= 0);
        var captured = assignment.Disclosed[refusal + 1];
        Assert.Equal(GroceryVulnerability, captured.Claim);
        Assert.Equal(Stance.Suspects, captured.AssertedStance);
        Assert.Equal(0.45, captured.AssertedConfidence, precision: 9);
        Assert.Equal(SourceKind.Inference, captured.ClaimedBasis);
        Assert.Equal(SourceKind.Inference, captured.ActualBasis);

        Assert.DoesNotContain(assignment.Disclosed, d =>
            d.Claim.Kind == ClaimKind.TargetIsVulnerable && d.Claim.Subject != Cast.Grocery);

        // The issuer changes his mind during the six-hour delay and acquires an unrelated view.
        // Neither may rewrite the words already captured in the assignment.
        var salvatore = world.Get("salvatore");
        salvatore.Cognition.Learn(GroceryVulnerability, Stance.Rejects, 1.0,
            SourceKind.Inference, salvatore.Id, world.Now.AddHours(1));
        var unrelated = new Claim(ClaimKind.TargetIsVulnerable, Cast.Tailor);
        salvatore.Cognition.Learn(unrelated, Stance.Believes, 0.9,
            SourceKind.Inference, salvatore.Id, world.Now.AddHours(1));

        var delivered = AdvanceToDelivery(world, assignment, "vincent");
        Assert.Equal(StepStatus.AwaitingChoice, delivered.Status);

        var vincent = world.Get("vincent");
        var held = vincent.Cognition.Find(GroceryVulnerability);
        Assert.NotNull(held);
        Assert.Equal(Stance.Suspects, held.Stance);
        Assert.Equal(0.45, held.Confidence, precision: 9);
        Assert.Equal(SourceKind.Report, held.SourceKind);
        Assert.Equal("salvatore", held.SourceId);
        Assert.Null(vincent.Cognition.Find(unrelated));

        var testimony = Assert.Single(vincent.Cognition.Testimony,
            t => t.Claim.Equals(GroceryVulnerability));
        Assert.Equal("salvatore", testimony.SenderId);
        Assert.Equal(Stance.Suspects, testimony.AssertedStance);
        Assert.Equal(0.45, testimony.AssertedConfidence, precision: 9);
        Assert.Equal(SourceKind.Inference, testimony.ClaimedBasis);

        Assert.Equal(0.41625,
            Salience.Perceive(vincent, world.Now).BelievesVulnerable(Cast.Grocery), precision: 9);
        Assert.DoesNotContain(world.AccountConflicts, c => c.ListenerId == vincent.Id);
        Assert.DoesNotContain(world.AccountAgreements, a => a.ListenerId == vincent.Id);
    }

    [Fact]
    public void Normal_delivery_changes_only_the_named_recipient_and_a_misaddressed_event_changes_nobody()
    {
        var normal = Cast.Build(Seed, "baseline");
        var assignment = AdvanceToAssignment(normal);
        var nonRecipientsBefore = normal.Characters.Values
            .Where(c => c.Id != assignment.RecipientId)
            .ToDictionary(c => c.Id, CharacterInformationFingerprint);

        AdvanceToDelivery(normal, assignment, "vincent");

        foreach (var character in normal.Characters.Values.Where(c => c.Id != assignment.RecipientId))
            Assert.Equal(nonRecipientsBefore[character.Id], CharacterInformationFingerprint(character));

        var wrong = Cast.Build(Seed, "baseline");
        Runner.Step(wrong, wrong.Now, controlledCharacterId: null); // consume the opening world tick
        var wrongAssignment = AddAssignment(
            wrong, recipientId: "vincent", ownerId: "tommy", GroceryAccount(), wrong.Now.AddMinutes(1));
        var before = wrong.Characters.Values.ToDictionary(c => c.Id, CharacterFingerprint);

        var step = Runner.Step(wrong, wrong.Now.AddMinutes(1), controlledCharacterId: "tommy");
        Assert.Equal(EventKind.AssignmentDelivered, step.Event!.Kind);
        Assert.Equal(wrongAssignment.Id, step.Event.Payload.AssignmentId);
        Assert.Equal(StepStatus.Advanced, step.Status);
        foreach (var character in wrong.Characters.Values)
            Assert.Equal(before[character.Id], CharacterFingerprint(character));
    }

    [Theory]
    [InlineData(Stance.Believes, "tommy", true, false)]
    [InlineData(Stance.Rejects, "tommy", false, true)]
    [InlineData(Stance.Suspects, "salvatore", false, false)]
    public void Assignment_receipt_uses_ordinary_reconciliation_and_social_effects(
        Stance priorStance, string priorSource, bool expectAgreement, bool expectConflict)
    {
        var world = Cast.Build(Seed, "baseline");
        Runner.Step(world, world.Now, controlledCharacterId: null);
        var vincent = world.Get("vincent");
        var account = GroceryAccount();
        var priorAccount = priorSource == "salvatore"
            ? account
            : ReportedClaim.Honest(
                GroceryVulnerability, priorStance, 0.6, SourceKind.Report);
        vincent.Cognition.Receive(priorAccount, priorSource, world.Now);

        // A control cognition receives the same two accounts directly through the ordinary API.
        // The assignment path must produce exactly that record and testimony history.
        var control = new Cognition();
        control.Receive(priorAccount, priorSource, world.Now);
        var expected = control.Receive(account, "salvatore", world.Now.AddMinutes(1));

        double trustBefore = vincent.Social.Toward("salvatore").Trust;
        var assignment = AddAssignment(
            world, "vincent", "vincent", account, world.Now.AddMinutes(1));
        var step = Runner.Step(world, world.Now.AddMinutes(1), controlledCharacterId: "vincent");
        Assert.Equal(assignment.Id, step.Event!.Payload.AssignmentId);

        Assert.Equal(expected.Record, vincent.Cognition.Find(GroceryVulnerability));
        Assert.Equal(control.Testimony, vincent.Cognition.Testimony.Where(
            t => t.Claim.Equals(GroceryVulnerability)).ToList());
        Assert.Equal(expectConflict,
            world.AccountConflicts.Any(c => c.ListenerId == vincent.Id && c.Conflict.Claim.Equals(GroceryVulnerability)));
        Assert.Equal(expectAgreement,
            world.AccountAgreements.Any(a => a.ListenerId == vincent.Id && a.Agreement.Claim.Equals(GroceryVulnerability)));

        double trustAfter = vincent.Social.Toward("salvatore").Trust;
        if (expectConflict) Assert.True(trustAfter < trustBefore);
        else if (expectAgreement) Assert.True(trustAfter > trustBefore);
        else Assert.Equal(trustBefore, trustAfter, precision: 9);
    }

    [Fact]
    public void A_relevant_assignment_receipt_schedules_a_real_supervisory_review()
    {
        var world = Cast.Build(Seed, "baseline");
        Runner.Step(world, world.Now, controlledCharacterId: null);
        var vincent = world.Get("vincent");
        var operation = StageDelegatedGroceryOperation(vincent);
        var assignment = AddAssignment(
            world, "vincent", "vincent", GroceryAccount(), world.Now.AddMinutes(1));

        var step = Runner.Step(world, world.Now.AddMinutes(1), controlledCharacterId: "vincent");

        Assert.Equal(assignment.Id, step.Event!.Payload.AssignmentId);
        Assert.NotNull(vincent.Cognition.Find(GroceryVulnerability));
        Assert.NotNull(operation.PendingReviewEventId);

        var unrelatedWorld = Cast.Build(Seed, "baseline");
        Runner.Step(unrelatedWorld, unrelatedWorld.Now, controlledCharacterId: null);
        var unrelatedVincent = unrelatedWorld.Get("vincent");
        var unrelatedOperation = StageDelegatedGroceryOperation(unrelatedVincent);
        AddAssignment(unrelatedWorld, "vincent", "vincent",
            ReportedClaim.Honest(
                new Claim(ClaimKind.TargetIsVulnerable, Cast.Tailor),
                Stance.Suspects, 0.45, SourceKind.Inference),
            unrelatedWorld.Now.AddMinutes(1));
        Runner.Step(unrelatedWorld, unrelatedWorld.Now.AddMinutes(1), controlledCharacterId: "vincent");
        Assert.Null(unrelatedOperation.PendingReviewEventId);

        var repeatWorld = Cast.Build(Seed, "baseline");
        Runner.Step(repeatWorld, repeatWorld.Now, controlledCharacterId: null);
        var repeatVincent = repeatWorld.Get("vincent");
        var repeatOperation = StageDelegatedGroceryOperation(repeatVincent);
        repeatVincent.Cognition.Receive(GroceryAccount(), "salvatore", repeatWorld.Now);
        AddAssignment(repeatWorld, "vincent", "vincent", GroceryAccount(), repeatWorld.Now.AddMinutes(1));
        Runner.Step(repeatWorld, repeatWorld.Now.AddMinutes(1), controlledCharacterId: "vincent");
        Assert.Null(repeatOperation.PendingReviewEventId);

        var nonOwnerWorld = Cast.Build(Seed, "baseline");
        Runner.Step(nonOwnerWorld, nonOwnerWorld.Now, controlledCharacterId: null);
        var ownedByVincent = StageDelegatedGroceryOperation(nonOwnerWorld.Get("vincent"));
        AddAssignment(nonOwnerWorld, "tommy", "tommy", GroceryAccount(), nonOwnerWorld.Now.AddMinutes(1));
        Runner.Step(nonOwnerWorld, nonOwnerWorld.Now.AddMinutes(1), controlledCharacterId: "tommy");
        Assert.NotNull(nonOwnerWorld.Get("tommy").Cognition.Find(GroceryVulnerability));
        Assert.Null(ownedByVincent.PendingReviewEventId);
    }

    [Fact]
    public void The_received_assessment_changes_expected_reward_and_the_existing_corroboration_subject()
    {
        var informed = PrepareOpening(includeVulnerability: true);
        var uninformed = PrepareOpening(includeVulnerability: false);

        var informedQuestion = Assert.Single(informed.Available,
            c => c.Kind == ActionKind.SeekCorroboration);
        var uninformedQuestion = Assert.Single(uninformed.Available,
            c => c.Kind == ActionKind.SeekCorroboration);
        Assert.Equal(GroceryVulnerability, informedQuestion.AboutClaim);
        Assert.Equal(GroceryRefusal, uninformedQuestion.AboutClaim);

        foreach (var method in Enum.GetValues<CoercionMethod>())
        {
            var withInformation = ScoreFor(informed, Cast.Grocery, method);
            var withoutInformation = ScoreFor(uninformed, Cast.Grocery, method);
            double rewardWith = Assert.Single(withInformation.Components,
                c => c.Name == "expected reward").Value;
            double rewardWithout = Assert.Single(withoutInformation.Components,
                c => c.Name == "expected reward").Value;
            Assert.True(rewardWith > rewardWithout,
                $"{method} expected reward did not read the received vulnerability assessment");
        }

        var leaves = informed.Available.Where(c =>
            c.Kind == ActionKind.StartStrategy && c.Strategy == StrategyKind.SecureTribute).ToList();
        Assert.Equal(6, leaves.Count);
        foreach (string target in new[] { Cast.Tailor, Cast.Grocery })
            Assert.Equal(Enum.GetValues<CoercionMethod>(),
                leaves.Where(c => c.TargetId == target).Select(c => c.Method!.Value).Order().ToArray());
    }

    [Fact]
    public void Hidden_resistance_does_not_change_the_opening_information_options_or_scores()
    {
        var low = Cast.Build(Seed, "baseline");
        low.Businesses[Cast.Grocery].Resistance = 0.0;
        var high = Cast.Build(Seed, "baseline");
        high.Businesses[Cast.Grocery].Resistance = 1.0;

        var preparedLow = PrepareOpening(low, includeVulnerability: true);
        var preparedHigh = PrepareOpening(high, includeVulnerability: true);

        Assert.Equal(preparedLow.Generated.Select(CandidateSignature),
            preparedHigh.Generated.Select(CandidateSignature));
        Assert.Equal(preparedLow.Available.Select(CandidateSignature),
            preparedHigh.Available.Select(CandidateSignature));
        Assert.Equal(preparedLow.Scored.Select(ScoreSignature),
            preparedHigh.Scored.Select(ScoreSignature));
    }

    [Fact]
    public void Controlled_and_autonomous_openings_prepare_the_same_concrete_choice()
    {
        var controlledWorld = Cast.Build(Seed, "baseline");
        var controlled = PrepareOpening(controlledWorld, includeVulnerability: true);

        var autonomousWorld = Cast.Build(Seed, "baseline");
        Runner.Run(autonomousWorld, Cast.Start.AddDays(1));
        var autonomous = autonomousWorld.Decisions.First(d => d.ActorId == "vincent");

        Assert.Equal(controlled.Generated.Select(CandidateSignature),
            autonomous.Generated.Select(CandidateSignature));
        Assert.Equal(controlled.Rejected.Select(r => $"{CandidateSignature(r.Candidate)}|{r.Stage}"),
            autonomous.Rejected.Select(r => $"{CandidateSignature(r.Candidate)}|{r.Stage}"));
        Assert.Equal(controlled.Scored.Select(ScoreSignature),
            autonomous.Scored.Select(ScoreSignature));

        Pipeline.Resolve(controlled, null);
        var controlledRecord = controlledWorld.Decisions.Last(d => d.ActorId == "vincent");
        Assert.Equal(autonomous.ChosenActionSignature(), controlledRecord.ChosenActionSignature());
    }

    [Fact]
    public void The_owner_keeps_the_method_he_ordered_when_a_delegate_privately_changes_his_approach()
    {
        var world = Cast.Build(Seed, "baseline");
        var opening = PrepareOpening(world, includeVulnerability: true);
        var threaten = opening.Available.Single(c =>
            c.Kind == ActionKind.StartStrategy
            && c.TargetId == Cast.Grocery
            && c.Method == CoercionMethod.Threaten);

        Pipeline.Resolve(opening, threaten.Id);
        var operation = Assert.Single(world.Get("vincent").Execution.Operations);
        Assert.Equal(CoercionMethod.Threaten, operation.OwnerOrderedMethod);

        // The handoff itself is not the behavior under test. Stage only the relationship, then
        // model a private delegate-side alteration: Vincent must continue to see what he ordered,
        // while Tommy sees the method he is actually using.
        operation.DelegatedToId = "tommy";
        operation.Method = CoercionMethod.Force;

        var ownerView = Assert.Single(PlayerView.Build(world, "vincent", world.Now).Operations);
        var executorView = Assert.Single(PlayerView.Build(world, "tommy", world.Now).Operations);
        Assert.Equal("threaten Bellini's grocery", ownerView.Approach);
        Assert.Equal("use force on Bellini's grocery", executorView.Approach);
    }

    [Fact]
    public void Existing_player_surfaces_explain_the_source_uncertainty_policy_and_six_methods_without_hidden_numbers()
    {
        var session = SimulationSession.Start(Seed, "baseline", "vincent");
        AdvanceToPause(session);

        var pending = session.Pending!;
        Assert.Contains(
            "Salvatore Greco told you: Bellini's grocery is not paying its tribute; he suspects Bellini's grocery would fold if leaned on.",
            pending.Focus);
        Assert.Contains(
            "Ferri's tailor shop is another part of that shortfall: you already know it is not paying its tribute.",
            pending.Focus);
        Assert.Contains("His standing rule: no public violence in the harbour.", pending.Focus);
        Assert.Equal(10, pending.Options.Count);
        Assert.Equal(6, pending.Options.Count(o =>
            o.Description.StartsWith("persuade ", StringComparison.Ordinal)
            || o.Description.StartsWith("threaten ", StringComparison.Ordinal)
            || o.Description.StartsWith("use force on ", StringComparison.Ordinal)));
        Assert.Contains(pending.Options,
            o => o.Description == "ask Tommy Nardo what he knows about whether Bellini's grocery would fold if leaned on");

        var belief = Assert.Single(session.Snapshot().Known,
            b => b.Claim.Kind == ClaimKind.TargetIsVulnerable && b.Claim.Subject == Cast.Grocery);
        Assert.Equal("Bellini's grocery would fold if leaned on", belief.Statement);
        Assert.Contains("Salvatore Greco", belief.Attribution);
        Assert.NotNull(belief.Certainty);

        string publicText = pending.Focus + "\n" + string.Join("\n", pending.Options.Select(o => o.Description))
                            + "\n" + belief.Statement + "\n" + belief.Certainty + "\n" + belief.Attribution;
        Assert.DoesNotContain("0.45", publicText, StringComparison.Ordinal);
        Assert.DoesNotContain("0.41625", publicText, StringComparison.Ordinal);
        Assert.DoesNotContain("resistance", publicText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("utility", publicText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recommend", publicText, StringComparison.OrdinalIgnoreCase);
    }

    private static Assignment AdvanceToAssignment(World world)
    {
        for (int guard = 0; guard < 20 && world.Org.Assignments.Count == 0; guard++)
            Runner.Step(world, Cast.Start.AddDays(1), controlledCharacterId: null);
        return Assert.Single(world.Org.Assignments);
    }

    private static StepResult AdvanceToDelivery(World world, Assignment assignment, string controlledId)
    {
        for (int guard = 0; guard < 50; guard++)
        {
            var step = Runner.Step(world, Cast.Start.AddDays(1), controlledId);
            if (step.Event?.Kind == EventKind.AssignmentDelivered
                && step.Event.Payload.AssignmentId == assignment.Id)
                return step;
        }
        throw new InvalidOperationException("the assignment delivery was never reached");
    }

    private static PreparedDecision PrepareOpening(bool includeVulnerability)
        => PrepareOpening(Cast.Build(Seed, "baseline"), includeVulnerability);

    private static PreparedDecision PrepareOpening(World world, bool includeVulnerability)
    {
        var assignment = AdvanceToAssignment(world);
        if (!includeVulnerability)
        {
            var disclosed = Assert.IsType<List<ReportedClaim>>(assignment.Disclosed);
            disclosed.RemoveAll(d => d.Claim.Equals(GroceryVulnerability));
        }

        var result = AdvanceToDelivery(world, assignment, "vincent");
        return Assert.IsType<PreparedDecision>(result.Awaiting);
    }

    private static Assignment AddAssignment(
        World world,
        string recipientId,
        string ownerId,
        ReportedClaim disclosed,
        DateTime deliveryAt)
    {
        var assignment = new Assignment(
            world.NextAssignmentId(),
            "staged receipt",
            "salvatore",
            recipientId,
            Cast.Harbour,
            Array.Empty<string>(),
            new[] { disclosed },
            world.Now,
            world.Now.AddDays(30));
        world.Org.Assignments.Add(assignment);
        world.Queue.Schedule(deliveryAt, EventKind.AssignmentDelivered, ownerId, "staged briefing",
            new EventPayload { AssignmentId = assignment.Id });
        return assignment;
    }

    private static ReportedClaim GroceryAccount()
        => ReportedClaim.Honest(
            GroceryVulnerability, Stance.Suspects, 0.45, SourceKind.Inference);

    private static StrategyInstance StageDelegatedGroceryOperation(Character vincent)
    {
        var operation = new StrategyInstance
        {
            OwnerId = vincent.Id,
            LocalSequence = vincent.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            Method = CoercionMethod.Persuade,
            StartedAt = Cast.Start,
            Deadline = Cast.Start.AddDays(30),
            DelegatedToId = "tommy",
        };
        vincent.Execution.Operations.Add(operation);
        return operation;
    }

    private static ScoreBreakdown ScoreFor(
        PreparedDecision prepared, string target, CoercionMethod method)
        => Assert.Single(prepared.Scored,
            s => s.Candidate.Kind == ActionKind.StartStrategy
                 && s.Candidate.TargetId == target
                 && s.Candidate.Method == method);

    private static int IndexOf(IReadOnlyList<ReportedClaim> disclosed, Claim claim)
    {
        for (int i = 0; i < disclosed.Count; i++)
            if (disclosed[i].Claim.Equals(claim)) return i;
        return -1;
    }

    private static string CharacterInformationFingerprint(Character c)
        => string.Join("|", c.Cognition.Records.Select(r => r.ToString())) + "||"
           + string.Join("|", c.Cognition.Testimony.Select(t => t.ToString()));

    private static string CharacterFingerprint(Character c)
        => CharacterInformationFingerprint(c) + $"||{c.Motivations.Responsibilities.Count}|"
           + $"{c.Execution.Commitments.Count}|{c.DecisionCount}";

    private static string CandidateSignature(Candidate c)
        => $"{c.Id}|{c.Kind}|{c.TargetId}|{c.Strategy}|{c.Method}|{c.AboutClaim}";

    private static string ScoreSignature(ScoreBreakdown score)
        => $"{CandidateSignature(score.Candidate)}|{score.Total:R}|{score.Noise:R}|"
           + string.Join(";", score.Components.Select(c => $"{c.Name}:{c.Value:R}:{c.Explanation}"));

    private static void AdvanceToPause(SimulationSession session)
    {
        for (int guard = 0; guard < 100 && session.Status != SessionStatus.AwaitingChoice; guard++)
            session.StepEvent();
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
    }
}
