using System.Text.Json;
using System.Diagnostics;
using CrimeEmpire.Persistence.Session;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

public sealed class SceneTests
{
    internal static string Json(object? value) => JsonSerializer.Serialize(value);
    internal static void Pick(PersistentSession s, string words) => s.Choose(s.Pending!.Options.Single(o => o.Description == words).Id);
    internal static PersistentSession Open(bool delegated, string variant = "baseline")
    {
        var s = PersistentSession.Start(42, variant, "vincent");
        for (int i = 0; i < 100 && s.Pending is null; i++) s.StepEvent();
        Pick(s, "threaten Bellini's grocery");
        Pick(s, delegated ? "Assign Tommy Nardo" : "Do it yourself");
        Pick(s, "Confirm operation");
        return s;
    }
    internal static void Advance(PersistentSession s, int speed = 0)
    {
        if (s.Pending is { } p)
        {
            var option = p.Options.FirstOrDefault(o => o.Description.StartsWith("carry on")
                || o.Description == "leave these orders unchanged" || o.Description == "take no action")
                ?? p.Options.First(o => o.Description != "Go back");
            s.ChooseAndConfirm(option.Id);
        }
        else if (speed > 0) s.AdvanceDays(speed);
        else s.StepEvent();
    }
    internal static string Retained(World world) => Json(new
    {
        Decisions = world.Decisions.Select(d => new { d.Id, d.ActorId, Executor = d.ExecutorChoice?.Candidate, d.StartedOperation }),
        Sources = world.Characters.Values.OrderBy(c => c.Id).Select(c => new
        { c.Id, c.Execution.OperationAccounts, c.Execution.EndedTributeOrders, c.Capabilities.CashReceipts }),
    });

    private static string ReplayEvidence(PersistentSession s) => Json(new
    {
        Snapshot = s.Snapshot(), s.Pending, Sources = Retained(s.InnerSession.World),
        World = ControlledAutonomousParityTests.ComprehensiveFingerprint(s.InnerSession.World),
    });

    private static void FreshProcess(PersistentSession session, string path)
    {
        session.Save(path);
        string expected = path + ".expected";
        File.WriteAllText(expected, ReplayEvidence(session));
        try
        {
            var start = new ProcessStartInfo("dotnet")
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            start.ArgumentList.Add("vstest");
            start.ArgumentList.Add(typeof(SceneTests).Assembly.Location);
            start.ArgumentList.Add("--TestCaseFilter:FullyQualifiedName=CrimeEmpire.Simulation.Tests.SceneTests.Snapshot_contract_and_fresh_process_probe");
            start.Environment["CE_M032_REPLAY_PROBE"] = path;
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(60000)) { process.Kill(true); throw new TimeoutException("Scene replay child did not finish."); }
            Assert.True(process.ExitCode == 0, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
        }
        finally { File.Delete(expected); }
    }

    [Fact]
    public void Snapshot_contract_and_fresh_process_probe()
    {
        Assert.Equal(new[] { "Action", "Amount", "Claim", "ClaimedBasis", "Description", "ExecutorId", "ExecutorName", "Id", "KnownEventAt", "Method", "MoneyArrived", "SourceId", "SourceName", "Stance", "Subject", "Uncertainty", "At" }.OrderBy(x => x),
            typeof(PlayerChronicleEntry).GetProperties().Select(p => p.Name).OrderBy(x => x));
        if (Environment.GetEnvironmentVariable("CE_M032_REPLAY_PROBE") is { } path)
            Assert.Equal(File.ReadAllText(path + ".expected"), ReplayEvidence(PersistentSession.Load(path)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Public_route_retains_real_order_receipt_and_completion_and_survives_replay(bool delegated)
    {
        var s = Open(delegated);
        var world = s.InnerSession.World;
        var op = Assert.Single(world.Get("vincent").Execution.Operations);
        var identity = new OperationIdentity(op.OwnerId, op.LocalSequence);
        var order = Assert.Single(s.Snapshot().Chronicle);
        Assert.Equal(identity, order.Id.Operation);
        Assert.Equal(delegated ? "tommy" : "vincent", order.ExecutorId);
        Assert.Equal(CoercionMethod.Threaten, order.Method);
        Assert.Equal(ActionKind.StartStrategy, order.Action);
        bool refusal = false;
        var path = Path.Combine(Path.GetTempPath(), $"ce-scene-{Guid.NewGuid():N}.db");
        try
        {
            void Replay()
            {
                s.Save(path); var loaded = PersistentSession.Load(path);
                Assert.Equal(Json(s.Snapshot()), Json(loaded.Snapshot()));
                Assert.Equal(Json(s.Pending), Json(loaded.Pending));
                Assert.Equal(Retained(world), Retained(loaded.InnerSession.World));
                Assert.Equal(SimulationReplayTests.Snapshot(world), SimulationReplayTests.Snapshot(loaded.InnerSession.World));
            }
            Replay();
            FreshProcess(s, path);
            for (int i = 0; i < 1000 && !world.Get("vincent").Execution.EndedTributeOrders.Any(e => e.Operation == identity); i++)
            {
                Advance(s);
                var card = string.Join(" ", SceneNarration.Situation(s.Snapshot(), s.Pending));
                refusal |= card.Contains("has turned you down");
                if (i == 8) Replay();
            }
            var receipt = Assert.Single(world.Get("vincent").Capabilities.CashReceipts, r => r.Operation == identity);
            var income = Assert.Single(s.Snapshot().Chronicle, e => e.Id.Kind == ChronicleKind.Income && e.Id.Operation == identity);
            Assert.Equal(receipt.Amount, income.Amount);
            Assert.Equal(receipt.At, income.At);
            Assert.Equal(receipt.ExecutorId, income.ExecutorId);
            Assert.True(Assert.Single(s.Snapshot().Chronicle, e => e.Id.Kind == ChronicleKind.Ended && e.Id.Operation == identity).MoneyArrived);
            Assert.Equal(order, s.Snapshot().Chronicle.Single(e => e.Id == order.Id));
            if (!delegated) Assert.True(refusal, "Direct neutral route must include personal refusal before payment.");
            Replay();
            FreshProcess(s, path);
            Assert.Empty(PlayerView.Build(world, "kane", world.Now).Chronicle);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Autonomous_writers_retain_the_same_sources_and_identity_rules()
    {
        var world = Cast.Build(42, "baseline"); Runner.Run(world, Cast.Start.AddDays(90));
        var starts = world.Decisions.Where(d => d.Chosen?.Candidate is { Kind: ActionKind.StartStrategy, Strategy: StrategyKind.SecureTribute }).ToArray();
        Assert.NotEmpty(starts);
        Assert.All(starts, d => { Assert.NotNull(d.StartedOperation); Assert.NotNull(d.ExecutorChoice); });
        var owner = world.Get("vincent");
        Assert.NotEmpty(owner.Execution.EndedTributeOrders);
        Assert.NotEmpty(owner.Execution.OperationAccounts);
        Assert.All(owner.Capabilities.CashReceipts, r => Assert.Contains(starts, d => d.StartedOperation == r.Operation));
        Assert.All(owner.Execution.EndedTributeOrders, e =>
        {
            var start = Assert.Single(starts, d => d.StartedOperation == e.Operation);
            Assert.Equal(start.Chosen!.Candidate.TargetId, e.TargetId);
            if (e.MoneyArrived) Assert.Contains(owner.Capabilities.CashReceipts,
                r => r.Operation == e.Operation && r.SourceId == e.TargetId && r.At == e.At && r.ExecutorId == e.ExecutorId);
        });
        Assert.All(owner.Execution.OperationAccounts, a =>
        {
            var report = world.Reports.Single(r => r.Id == a.SourceReportId);
            Assert.Equal(report.Asserted[a.ClaimOrdinal].AssertedConfidence, a.AssertedConfidence);
            Assert.Equal(report.Asserted[a.ClaimOrdinal].ClaimedBasis, a.ClaimedBasis);
        });
    }

    private static (PersistentSession Session, Report Report) Undelivered()
    {
        var s = Open(true); var world = s.InnerSession.World; var sub = world.Get("tommy");
        for (int i = 0; i < 200 && sub.Execution.OperationLearning.Count == 0; i++) Advance(s);
        var learned = Assert.Single(sub.Execution.OperationLearning.Values);
        var report = Reporting.Compose(world, sub, world.Get("vincent"),
            new Candidate("account", ActionKind.ReportToSuperior, "test", "account")
            { TargetId = "vincent", Candor = ReportCandor.Candid, AnsweringClaim = learned.Position.Claim },
            Salience.Perceive(sub, world.Now));
        Assert.NotEmpty(report.Operations);
        return (s, report);
    }

    [Fact]
    public void All_channels_absent_hides_account_and_private_progress_but_legitimate_receipt_restores_it()
    {
        var (s, report) = Undelivered(); var world = s.InnerSession.World; var owner = world.Get("vincent");
        var truth = Json(world.TruthLog);
        Assert.Empty(owner.Execution.OperationAccounts);
        Assert.DoesNotContain(s.Snapshot().Chronicle, e => e.Id.Kind == ChronicleKind.Account);
        Assert.Contains("No report about this operation yet", string.Join(" ", SceneNarration.Situation(s.Snapshot(), s.Pending)));
        // Keep the actual events and executor knowledge fixed; deliver through the real writer.
        Reporting.Deliver(world, report, owner);
        var account = Assert.Single(s.Snapshot().Chronicle, e => e.Id.Kind == ChronicleKind.Account);
        Assert.Equal(report.Id, account.Id.SourceId);
        Assert.Contains("reported:", string.Join(" ", SceneNarration.Situation(s.Snapshot(), s.Pending)));
        Assert.Equal(truth, Json(world.TruthLog.Take(world.TruthLog.Count - 1)));
        Assert.Empty(PlayerView.Build(world, "kane", world.Now).Chronicle);
    }

    [Fact]
    public void Same_instant_reports_append_preserved_stance_confidence_basis_and_identity()
    {
        var (s, report) = Undelivered(); var world = s.InnerSession.World; var owner = world.Get("vincent");
        Reporting.Deliver(world, report, owner);
        var original = Assert.Single(s.Snapshot().Chronicle, e => e.Id.Kind == ChronicleKind.Account);
        var later = report with { Id = world.NextReportId(), Asserted = report.Asserted.Select(a =>
            ReportedClaim.Misrepresenting(a.Claim, Stance.Rejects, .35, SourceKind.Inference, SourceKind.Participant)).ToArray() };
        Reporting.Deliver(world, later, owner);
        var entries = s.Snapshot().Chronicle.Where(e => e.Id.Kind == ChronicleKind.Account).ToArray();
        Assert.Equal(2, entries.Length); Assert.Equal(original, entries[0]); Assert.NotEqual(entries[0].Id, entries[1].Id);
        Assert.Equal(report.At, entries[1].At); Assert.Equal(Stance.Rejects, entries[1].Stance);
        Assert.Equal(SourceKind.Inference, entries[1].ClaimedBasis); Assert.Equal("not sure", entries[1].Uncertainty);
        Assert.Null(entries[1].KnownEventAt);
        Assert.DoesNotContain("participation", SceneNarration.Entry(entries[1]));
        Assert.Equal(Json(entries), Json(s.Snapshot().Chronicle.Where(e => e.Id.Kind == ChronicleKind.Account).ToArray()));
    }

    [Fact]
    public void Two_independent_revisions_without_refresh_change_knowledge_not_received_history()
    {
        // Focused cognition/projection control. Arbitrary test writes are deliberately not save commands.
        var (s, report) = Undelivered(); var world = s.InnerSession.World; var owner = world.Get("vincent");
        Reporting.Deliver(world, report, owner);
        var claim = report.Asserted[0].Claim;
        owner.Cognition.Learn(claim, Stance.Believes, 1, SourceKind.Inference, owner.Id, world.Now);
        var history = Json(s.Snapshot().Chronicle);
        var because = new Reconsideration(ReconsiderCause.AcquiredAgain, SourceKind.Inference, owner.Id);
        Assert.NotNull(owner.Cognition.Revise(claim, .8, owner.Id, world.Now.AddMinutes(1), because));
        Assert.NotNull(owner.Cognition.Revise(claim, .35, owner.Id, world.Now.AddMinutes(2), because));
        Assert.Equal(history, Json(s.Snapshot().Chronicle));
        Assert.Contains(s.Snapshot().Known, b => b.Claim == PlayerClaim.Of(claim) && b.Certainty == "you are not sure of it");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancellation_and_same_target_recommission_keep_distinct_production_ids(bool delegated)
    {
        var s = Open(delegated);
        if (delegated) { s.StepEvent(); Pick(s, "take no action"); }
        var first = s.Snapshot().Chronicle[0];
        s.ReviewOperation(s.Snapshot().Operations.Single().ReviewToken!);
        Pick(s, "drop getting Bellini's grocery to pay");
        var cancelled = s.Snapshot().Chronicle.Last();
        Assert.Equal(ActionKind.AbandonStrategy, cancelled.Action); Assert.Equal(first.Id.Operation, cancelled.Id.Operation);
        var path = Path.Combine(Path.GetTempPath(), $"ce-cancel-{Guid.NewGuid():N}.db");
        try
        {
            s.Save(path); var load = PersistentSession.Load(path);
            Assert.Equal(Json(s.Snapshot()), Json(load.Snapshot()));
            Assert.Equal(Retained(s.InnerSession.World), Retained(load.InnerSession.World));
            FreshProcess(s, path);
            for (int i = 0; i < 200 && s.Pending?.Options.Any(o => o.Description == "persuade Bellini's grocery to pay") != true; i++) Advance(s);
            Pick(s, "threaten Bellini's grocery"); Pick(s, "Do it yourself"); Pick(s, "Confirm operation");
            var second = s.Snapshot().Chronicle.Last(e => e.Action == ActionKind.StartStrategy);
            Assert.Equal(first.Subject, second.Subject); Assert.NotEqual(first.Id.Operation, second.Id.Operation);
            Assert.Equal(first, s.Snapshot().Chronicle.Single(e => e.Id == first.Id));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Repeated_production_outcome_revisions_replay_without_rewriting_accounts()
    {
        var s = Open(true, "capable-angelo"); var world = s.InnerSession.World; var owner = world.Get("vincent");
        var claim = CapabilityBar.About("tommy", CapabilityBar.RoughWork);
        var revisions = new List<InformationRecord>();
        string originalTestimony = Json(owner.Cognition.Testimony);
        var previous = owner.Cognition.Records.Single(r => r.Claim == claim);
        bool second = false;
        for (int i = 0; i < 2000 && s.Status != SessionStatus.Resolved; i++)
        {
            if (!second && owner.Capabilities.CashReceipts.Count > 0 && s.Pending?.Options.Any(o => o.Description == "threaten Ferri's tailor shop") == true)
            {
                Pick(s, "threaten Ferri's tailor shop"); Pick(s, "Assign Tommy Nardo"); Pick(s, "Confirm operation"); second = true;
            }
            else Advance(s);
            var next = owner.Cognition.Records.Single(r => r.Claim == claim);
            if (next != previous && next.Reconsidered?.Cause == ReconsiderCause.DelegatedOutcome)
            {
                revisions.Add(next);
                Assert.Equal(originalTestimony, Json(owner.Cognition.Testimony));
                Assert.Empty(owner.Execution.OperationAccounts);
            }
            previous = next;
        }
        Assert.True(revisions.Count >= 2, $"second={second}; revisions={Json(revisions)}; receipts={Json(owner.Capabilities.CashReceipts)}");
        Assert.Equal(.85, revisions[0].Confidence, 8);
        Assert.Equal(.95, revisions[1].Confidence, 8);
        var incomes = s.Snapshot().Chronicle.Where(e => e.Id.Kind == ChronicleKind.Income).ToArray();
        Assert.Equal(2, incomes.Length);
        Assert.Equal(2, incomes.Select(e => e.Id.Operation).Distinct().Count());
        Assert.All(incomes, e => Assert.Contains(owner.Capabilities.CashReceipts,
            r => r.Operation == e.Id.Operation && r.At == e.At && r.Amount == e.Amount));
        Assert.DoesNotContain(s.Snapshot().Chronicle, e => e.Claim == PlayerClaim.Of(claim));
        Assert.Contains(s.Snapshot().Known, b => b.Claim == PlayerClaim.Of(claim) && b.Certainty == "you are certain of it");
        var path = Path.Combine(Path.GetTempPath(), $"ce-revisions-{Guid.NewGuid():N}.db");
        try
        {
            s.Save(path); var loaded = PersistentSession.Load(path);
            Assert.Equal(Json(s.Snapshot()), Json(loaded.Snapshot()));
            Assert.Equal(Retained(world), Retained(loaded.InnerSession.World));
            FreshProcess(s, path);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Completion_writer_keeps_unknown_reason_private_and_excludes_other_strategy_families()
    {
        var s = Open(true); var world = s.InnerSession.World; var owner = world.Get("vincent");
        var op = Assert.Single(owner.Execution.Operations);
        CrimeSim.Strategy.Strategies.Complete(world, owner, op, "SECRET executor outcome");
        var ended = Assert.Single(s.Snapshot().Chronicle, e => e.Id.Kind == ChronicleKind.Ended);
        Assert.Equal(new OperationIdentity(op.OwnerId, op.LocalSequence), ended.Id.Operation);
        Assert.False(ended.MoneyArrived); Assert.Equal("outcome unknown", ended.Uncertainty);
        Assert.Contains("reason is unknown", SceneNarration.Entry(ended));
        Assert.DoesNotContain("SECRET", Json(ended));
        var other = new StrategyInstance { OwnerId = owner.Id, LocalSequence = 99,
            Kind = StrategyKind.ConcealIncident, Domain = Cast.Harbour, StartedAt = world.Now, Deadline = world.Now.AddDays(10) };
        CrimeSim.Strategy.Strategies.Complete(world, owner, other, "SECRET second outcome");
        Assert.Single(owner.Execution.EndedTributeOrders);
    }

    [Fact]
    public void Same_target_two_production_operations_receive_same_instant_accounts_on_their_real_identities()
    {
        var (s, firstReport) = Undelivered(); var world = s.InnerSession.World; var owner = world.Get("vincent");
        // Drain a pending owner choice if present, without moving to another event.
        if (s.Pending is not null) Advance(s);
        s.ReviewOperation(s.Snapshot().Operations.Single().ReviewToken!);
        Pick(s, "drop getting Bellini's grocery to pay");
        for (int i = 0; i < 200 && s.Pending?.Options.Any(o => o.Description == "threaten Bellini's grocery") != true; i++) Advance(s);
        Pick(s, "threaten Bellini's grocery"); Pick(s, "Assign Tommy Nardo"); Pick(s, "Confirm operation");
        int sequence = owner.Execution.Operations.Single().LocalSequence;
        var sub = world.Get("tommy");
        for (int i = 0; i < 200 && !sub.Execution.OperationLearning.Values.Any(l => l.Sequence == sequence); i++) Advance(s);
        var learned = sub.Execution.OperationLearning.Values.First(l => l.Sequence == sequence);
        var second = Reporting.Compose(world, sub, owner, new Candidate("second", ActionKind.ReportToSuperior, "test", "account")
            { TargetId = owner.Id, Candor = ReportCandor.Candid, AnsweringClaim = learned.Position.Claim }, Salience.Perceive(sub, world.Now));
        Assert.NotEqual(firstReport.Operations[0].Sequence, second.Operations[0].Sequence);
        // Delivery-time collision control, retaining both production-composed attributions.
        firstReport = firstReport with { At = second.At };
        Reporting.Deliver(world, firstReport, owner); Reporting.Deliver(world, second, owner);
        var entries = s.Snapshot().Chronicle.Where(e => e.Id.Kind == ChronicleKind.Account).ToArray();
        Assert.Equal(2, entries.Length); Assert.Equal(entries[0].At, entries[1].At);
        Assert.Equal(firstReport.Operations[0].Sequence, entries[0].Id.Operation!.LocalSequence);
        Assert.Equal(second.Operations[0].Sequence, entries[1].Id.Operation!.LocalSequence);
        Assert.NotEqual(entries[0].Id, entries[1].Id);
    }

    [Fact]
    public void Missing_start_link_stays_absent_even_with_matching_live_operation()
    {
        var s = Open(false); var world = s.InnerSession.World;
        int index = world.Decisions.FindIndex(d => d.StartedOperation is not null);
        world.Decisions[index] = world.Decisions[index] with { StartedOperation = null };
        Assert.Null(Assert.Single(s.Snapshot().Chronicle).Id.Operation);
        Assert.NotNull(Assert.Single(s.Snapshot().Operations).TributeOperation);
    }

    [Fact]
    public void Autonomous_received_accounts_reconstruct_field_completely_in_a_fresh_process()
    {
        var session = PersistentSession.Start(42, "baseline", null, "vincent");
        session.AdvanceDays(90);
        var accounts = session.InnerSession.World.Get("vincent").Execution.OperationAccounts;
        Assert.NotEmpty(accounts);
        Assert.Contains(accounts, a => a.ClaimOrdinal > 0);
        var path = Path.Combine(Path.GetTempPath(), $"ce-accounts-{Guid.NewGuid():N}.db");
        try { FreshProcess(session, path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Projection_is_frozen_and_ignores_private_report_fields_and_developer_words()
    {
        var (s, report) = Undelivered(); var world = s.InnerSession.World;
        Reporting.Deliver(world, report, world.Get("vincent"));
        var snapshot = s.Snapshot(); string expected = Json(snapshot.Chronicle);
        world.Reports.Clear(); world.TruthLog.Clear();
        for (int i = 0; i < world.Decisions.Count; i++)
            world.Decisions[i] = world.Decisions[i] with { Outcome = "SECRET PRIVATE OUTCOME", Trigger = "SECRET TRIGGER" };
        Assert.Equal(expected, Json(s.Snapshot().Chronicle));
        world.Get("vincent").Execution.OperationAccounts.Clear();
        Assert.Equal(expected, Json(snapshot.Chronicle));
        Assert.Throws<NotSupportedException>(() => ((IList<PlayerChronicleEntry>)snapshot.Chronicle).Clear());
        Assert.DoesNotContain("SECRET", Json(s.Snapshot().Chronicle));
    }

    [Fact]
    public void Replay_comparison_covers_every_field_of_each_new_retained_record()
    {
        var (s, report) = Undelivered(); var world = s.InnerSession.World; var owner = world.Get("vincent");
        Reporting.Deliver(world, report, owner);
        for (int i = 0; i < 1000 && owner.Capabilities.CashReceipts.Count == 0; i++) Advance(s);
        var account = owner.Execution.OperationAccounts[0];
        var receipt = owner.Capabilities.CashReceipts[0];
        var ended = owner.Execution.EndedTributeOrders[0];
        var decision = world.Decisions.First(d => d.StartedOperation is not null);
        // Enumerating record properties makes a newly added field a required test case, not a silent omission.
        foreach (object record in new object[] { account, receipt, ended, decision.StartedOperation! })
        foreach (var property in record.GetType().GetProperties().Where(p => p.SetMethod is not null))
        {
            object? original = property.GetValue(record);
            object changed = original switch
            {
                string str => str + "-changed", int n => n + 1, long n => n + 1,
                double n => n + .01, bool b => !b, DateTime at => at.AddMinutes(1),
                Claim c => c with { Subject = c.Subject + "-changed" },
                OperationIdentity id => id with { LocalSequence = id.LocalSequence + 1 },
                Stance st => st == Stance.Knows ? Stance.Doubts : Stance.Knows,
                SourceKind source => source == SourceKind.Report ? SourceKind.Inference : SourceKind.Report,
                _ => throw new InvalidOperationException($"Missing field probe: {property.Name}"),
            };
            string before = SimulationReplayTests.Snapshot(world);
            property.SetValue(record, changed);
            Assert.NotEqual(before, SimulationReplayTests.Snapshot(world));
            property.SetValue(record, original);
            Assert.Equal(before, SimulationReplayTests.Snapshot(world));
        }
    }

    [Fact]
    public void Rendering_phrasing_refresh_frequency_and_speed_cannot_change_the_world()
    {
        var slow = Open(false); var fast = Open(false);
        for (int i = 0; i < 3000 && slow.Status != SessionStatus.Resolved; i++)
        {
            var before = ControlledAutonomousParityTests.ComprehensiveFingerprint(slow.InnerSession.World);
            var snap = slow.Snapshot();
            _ = string.Join("\n", snap.Chronicle.Select(SceneNarration.Entry));
            _ = string.Join(" | ", snap.Chronicle.Select(e => SceneNarration.Entry(e).ToUpperInvariant()));
            _ = SceneNarration.Situation(snap, slow.Pending);
            Assert.Equal(before, ControlledAutonomousParityTests.ComprehensiveFingerprint(slow.InnerSession.World));
            Advance(slow);
        }
        for (int i = 0; i < 3000 && fast.Status != SessionStatus.Resolved; i++) Advance(fast, 7);
        Assert.Equal(SessionStatus.Resolved, slow.Status); Assert.Equal(SessionStatus.Resolved, fast.Status);
        Assert.Equal(SimulationReplayTests.Snapshot(slow.InnerSession.World), SimulationReplayTests.Snapshot(fast.InnerSession.World));
        Assert.Equal(Json(slow.Snapshot()), Json(fast.Snapshot()));
        Assert.Equal(slow.Result, fast.Result);
    }
}
