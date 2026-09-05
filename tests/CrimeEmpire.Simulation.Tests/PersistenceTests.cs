using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using CrimeEmpire.Persistence;
using CrimeEmpire.Persistence.Session;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Trace;
using Microsoft.Data.Sqlite;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 015: the existing baseline seed-42 Vincent <c>SecureTribute</c> operation survives a
/// save and a fresh <see cref="SimulationSession"/> rebuilt from it — replay-backed, not a copy of
/// <c>World</c>. <see cref="PersistentSession"/>, the wrapper under test, is driven the same way the
/// Godot shell drives it: <c>StepEvent</c> to reach each pause, an option matched only by its public
/// <see cref="PendingOption.Description"/> text, never a candidate id or score — the same discipline
/// <c>PlayerOwnedOperationTests</c> established for milestone 014's golden path, reused here rather
/// than re-derived.
///
/// The genuine two-process restart proof — a real save written by one OS process and loaded by a
/// second, driven through actual Godot button presses — is a Godot headless check, not an xunit test:
/// <c>CrimeEmpire.Godot</c> is never loaded by <c>dotnet test</c> (the same reason
/// <c>--selftest</c>/<c>--selftest-goldenpath</c> are not here either). See the milestone archive for
/// that proof's exact commands and recorded output. What belongs here is everything ruling 7 asks for
/// that a single process, and this project's own conventions, can prove directly.
/// </summary>
public sealed class PersistenceTests
{
    private const int Seed = 42;
    private const string Variant = "baseline";
    private const string Controlled = "vincent";
    private const string Marco = "marco";

    // Independently pinned, matching PlayerOwnedOperationTests.cs and Game.cs's own copy — not
    // shared code, so the three cannot all be wrong about the same assumption together.
    private static readonly string[] SevenChoiceSequence =
    {
        "persuade Bellini's grocery to pay",
        "carry on getting Bellini's grocery to pay",
        "hand it to Tommy Nardo",
        "switch to threats with Bellini's grocery",
        "switch to force with Bellini's grocery — breaking the rule: no public violence in the harbour",
        "carry on getting Bellini's grocery to pay",
        "report to Salvatore Greco, leaving out your own part",
    };

    private const string LetItLie = "take no action";

    // ================================================================= exact internal identity (ruling 7)

    /// <summary>
    /// Save and load while <see cref="SessionStatus.Ready"/> (ruling 6, first half): every piece of
    /// internal state ruling 7 names — the clock, any outstanding fast-forward, the event queue's
    /// size, the developer trace (which carries every RNG-influenced score and every identifier), and
    /// Vincent and Tommy's ongoing <c>SecureTribute</c> execution state (owner, delegate, method,
    /// step) — matches exactly between the session that was saved and the fresh one replay rebuilt.
    /// </summary>
    [Fact]
    public void Exact_internal_state_is_identical_before_save_and_after_load_at_a_ready_point()
    {
        string path = NewSavePath();
        try
        {
            var original = PersistentSession.Start(Seed, Variant, Controlled);
            PlayChoices(original, SevenChoiceSequence.Take(3));
            Assert.Equal(SessionStatus.Ready, original.Status);

            original.Save(path);
            var loaded = PersistentSession.Load(path);

            AssertExactInternalIdentity(original, loaded);
        }
        finally
        {
            Cleanup(path);
        }
    }

    /// <summary>
    /// Save and load while <see cref="SessionStatus.AwaitingChoice"/> (ruling 6, second half): the
    /// same identity as above, plus the reproduced pause itself — date, actor, occasion, focus, and
    /// every offered option's exact public description and opaque token, field for field.
    /// </summary>
    [Fact]
    public void Exact_internal_state_is_identical_before_save_and_after_load_at_an_awaiting_choice_pause()
    {
        string path = NewSavePath();
        try
        {
            var original = PersistentSession.Start(Seed, Variant, Controlled);
            PlayChoices(original, SevenChoiceSequence.Take(3));
            AdvanceToNextPause(original);
            Assert.Equal(SessionStatus.AwaitingChoice, original.Status);

            original.Save(path);
            var loaded = PersistentSession.Load(path);

            AssertExactInternalIdentity(original, loaded);
        }
        finally
        {
            Cleanup(path);
        }
    }

    // ================================================================= golden-path equivalence

    /// <summary>
    /// The same seven choices, made across a save and a fresh process-equivalent reload after the
    /// third, reach the identical accepted 1 April consequence a wholly uninterrupted run reaches —
    /// same rendered trace, same cash.
    /// </summary>
    [Fact]
    public void Loaded_and_uninterrupted_golden_path_reach_the_same_outcome()
    {
        var uninterrupted = PersistentSession.Start(Seed, Variant, Controlled);
        PlayChoices(uninterrupted, SevenChoiceSequence);

        string path = NewSavePath();
        try
        {
            var interrupted = PersistentSession.Start(Seed, Variant, Controlled);
            PlayChoices(interrupted, SevenChoiceSequence.Take(3));
            interrupted.Save(path);

            var resumed = PersistentSession.Load(path);
            PlayChoices(resumed, SevenChoiceSequence.Skip(3));

            Assert.Equal(6840, resumed.Snapshot().Cash);
            Assert.Equal(uninterrupted.Snapshot().Cash, resumed.Snapshot().Cash);
            Assert.Equal(
                TraceWriter.Render(uninterrupted.InnerSession.World, Variant, false),
                TraceWriter.Render(resumed.InnerSession.World, Variant, false));
        }
        finally
        {
            Cleanup(path);
        }
    }

    // ================================================================= determinism

    /// <summary>Loading the same save twice, then playing the same continuation on each, produces
    /// byte-identical history both immediately after loading and after the continuation.</summary>
    [Fact]
    public void Repeated_loads_of_the_same_save_are_deterministic()
    {
        string path = NewSavePath();
        try
        {
            var setup = PersistentSession.Start(Seed, Variant, Controlled);
            PlayChoices(setup, SevenChoiceSequence.Take(3));
            setup.Save(path);

            var loadedOnce = PersistentSession.Load(path);
            var loadedTwice = PersistentSession.Load(path);

            Assert.Equal(
                TraceWriter.Render(loadedOnce.InnerSession.World, Variant, false),
                TraceWriter.Render(loadedTwice.InnerSession.World, Variant, false));

            PlayChoices(loadedOnce, SevenChoiceSequence.Skip(3));
            PlayChoices(loadedTwice, SevenChoiceSequence.Skip(3));

            Assert.Equal(
                TraceWriter.Render(loadedOnce.InnerSession.World, Variant, false),
                TraceWriter.Render(loadedTwice.InnerSession.World, Variant, false));
        }
        finally
        {
            Cleanup(path);
        }
    }

    // ================================================================= counterfactual choice

    /// <summary>
    /// Two independent loads of the identical save, sent down different valid options at the very
    /// pause it was saved at, diverge on their own — no coefficient tuned, nothing hardcoded to make
    /// either branch win. Starting the operation and letting it lie are the same fork milestone 014's
    /// own counterfactual test uses; this is that fork, taken from a save instead of a fresh session.
    /// </summary>
    [Fact]
    public void Counterfactual_valid_choices_from_the_same_save_diverge_naturally()
    {
        string path = NewSavePath();
        try
        {
            var setup = PersistentSession.Start(Seed, Variant, Controlled);
            AdvanceToNextPause(setup);
            setup.Save(path);

            var golden = PersistentSession.Load(path);
            PlayChoices(golden, SevenChoiceSequence);

            var declined = PersistentSession.Load(path);
            ChooseByDescription(declined, LetItLie);

            Assert.Equal(6840, golden.Snapshot().Cash);
            Assert.NotEqual(golden.Snapshot().Cash, declined.Snapshot().Cash);
            Assert.True(golden.InnerSession.World.Businesses[Cast.Grocery].PayingTribute);
            Assert.False(declined.InnerSession.World.Businesses[Cast.Grocery].PayingTribute);
        }
        finally
        {
            Cleanup(path);
        }
    }

    // ================================================================= information boundary

    /// <summary>
    /// The structural version of milestone 009's boundary test, walked from
    /// <see cref="PersistentSession"/> instead of <see cref="SimulationSession"/>: nothing the
    /// persistence wrapper adds gives an interface a path to <c>World</c>, a decision record, a
    /// score, a report, or the mutable <c>Capabilities</c> object another character's cash lives on.
    /// </summary>
    [Fact]
    public void The_persistence_surface_exposes_no_developer_state()
    {
        Type[] forbidden =
        {
            typeof(World), typeof(SimulationSession), typeof(WorldEvent), typeof(DecisionRecord),
            typeof(ScoreBreakdown), typeof(ScoreComponent), typeof(PreparedDecision), typeof(Candidate),
            typeof(Rejection), typeof(Report), typeof(ReportedClaim), typeof(InformationRequest),
            typeof(Character), typeof(CharacterView), typeof(Cognition), typeof(SocialState),
            typeof(IRelationship), typeof(StepResult), typeof(ScheduledEvent), typeof(EventPayload),
            typeof(Agenda), typeof(Psychology), typeof(InformationRecord), typeof(Testimony),
            typeof(Capabilities), typeof(Claim),
        };

        var seen = new HashSet<Type>();
        var queue = new Queue<Type>(new[] { typeof(PersistentSession), typeof(PendingDecision), typeof(PlayerSnapshot) });

        while (queue.Count > 0)
        {
            var surface = queue.Dequeue();
            if (!seen.Add(surface)) continue;

            foreach (var member in surface.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (var type in TypesReferencedBy(member))
                {
                    Assert.False(forbidden.Contains(type),
                        $"{surface.Name}.{member.Name} exposes {type.Name} to whatever holds it");

                    if (type.Assembly == typeof(SimulationSession).Assembly || type.Assembly == typeof(PersistentSession).Assembly)
                        queue.Enqueue(type);
                }
            }
        }

        Assert.Contains(typeof(PlayerBelief), seen);
        Assert.Contains(typeof(PendingOption), seen);
    }

    /// <summary>
    /// Milestone 014's negative test, re-run against a session that went through a save and a load:
    /// another character's cash still cannot appear anywhere in the reachable snapshot, in any
    /// representation — a number reachable as itself, or the same value's text reachable inside some
    /// other string. Checking only the numeric case (as this test originally did — corrected per
    /// Codex's review of `9537b38`) would have missed a leak that arrived as text, which is exactly
    /// the shape milestone 014's own correction (`ff4213a`) found and fixed once already, by leaking a
    /// sentinel into <see cref="PlayerAttitude.Standing"/> — a nested string field the numeric-only
    /// check could never have reached at all. Mutation-checked the same way: see this milestone's
    /// appended correction for the confirmation that the string check below fails specifically, with
    /// <c>Cash</c> itself left correct.
    /// </summary>
    [Fact]
    public void Another_characters_cash_never_appears_in_a_loaded_sessions_snapshot()
    {
        string path = NewSavePath();
        try
        {
            var setup = PersistentSession.Start(Seed, Variant, Controlled);
            PlayChoices(setup, SevenChoiceSequence.Take(3));
            setup.Save(path);

            var loaded = PersistentSession.Load(path);

            const double marcoSentinel = 555_444;
            loaded.InnerSession.World.Get(Marco).Capabilities.Cash = marcoSentinel;

            var values = ValueGraph(loaded.Snapshot()).ToList();
            Assert.True(values.Count > 8, "the walk reached very little of the snapshot, so this proves nothing");
            Assert.DoesNotContain(marcoSentinel, values.OfType<double>());

            string marcoText = marcoSentinel.ToString(CultureInfo.InvariantCulture);
            foreach (string text in values.OfType<string>())
                Assert.DoesNotContain(marcoText, text, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup(path);
        }
    }

    // ================================================================= malformed input (ruling 7)

    [Fact]
    public void Loading_a_file_that_is_not_a_database_fails_visibly()
    {
        string path = NewSavePath();
        File.WriteAllBytes(path, new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 });
        try
        {
            Assert.Throws<SaveFormatException>(() => SaveStore.Read(path));
        }
        finally
        {
            Cleanup(path);
        }
    }

    [Fact]
    public void Loading_a_sqlite_file_with_no_save_tables_fails_visibly()
    {
        string path = NewSavePath();
        try
        {
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var create = connection.CreateCommand();
                create.CommandText = "CREATE TABLE unrelated (x INTEGER);";
                create.ExecuteNonQuery();
            }

            Assert.Throws<SaveFormatException>(() => SaveStore.Read(path));
        }
        finally
        {
            Cleanup(path);
        }
    }

    [Fact]
    public void Loading_a_save_with_an_unrecognised_command_kind_fails_visibly_without_autoplay()
    {
        string path = NewSavePath();
        try
        {
            SaveStore.Write(path, new SaveData(SaveStore.CurrentSchemaVersion, SimulationBuild.CurrentId, Seed, Variant, Controlled, Controlled, Array.Empty<SessionCommand>()));

            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var insert = connection.CreateCommand();
                insert.CommandText = "INSERT INTO save_commands (ordinal, kind, arg) VALUES (0, 'banana', NULL);";
                insert.ExecuteNonQuery();
            }

            var ex = Assert.Throws<SaveFormatException>(() => SaveStore.Read(path));
            Assert.Contains("banana", ex.Message);
        }
        finally
        {
            Cleanup(path);
        }
    }

    [Fact]
    public void Loading_a_save_with_a_gapped_command_ordinal_fails_visibly_without_autoplay()
    {
        string path = NewSavePath();
        try
        {
            SaveStore.Write(path, new SaveData(SaveStore.CurrentSchemaVersion, SimulationBuild.CurrentId, Seed, Variant, Controlled, Controlled, Array.Empty<SessionCommand>()));

            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var insert = connection.CreateCommand();
                insert.CommandText = "INSERT INTO save_commands (ordinal, kind, arg) VALUES (0, 'StepEvent', NULL), (2, 'StepEvent', NULL);";
                insert.ExecuteNonQuery();
            }

            var ex = Assert.Throws<SaveFormatException>(() => SaveStore.Read(path));
            Assert.Contains("ordinal", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(path);
        }
    }

    /// <summary>
    /// An option token nothing offers at its point in the replay — the shape a hand-edited or
    /// truncated save would take — throws during <see cref="PersistentSession.Load"/> rather than
    /// silently substituting the pipeline's own preference (autoplay) or any default option
    /// (fallback). The same <see cref="SimulationInvariantException"/> a live session already throws
    /// for an unrecognised token, wrapped with the replay ordinal it failed at.
    /// </summary>
    [Fact]
    public void Loading_a_save_with_an_option_token_nothing_offers_fails_visibly_without_autoplay()
    {
        string path = NewSavePath();
        try
        {
            var probe = PersistentSession.Start(Seed, Variant, Controlled);
            AdvanceToNextPause(probe);
            probe.Save(path);
            var stepsOnly = SaveStore.Read(path).Commands;

            var tampered = new SaveData(
                SaveStore.CurrentSchemaVersion, SimulationBuild.CurrentId, Seed, Variant, Controlled, Controlled,
                stepsOnly.Append(SessionCommand.Choose("not-a-real-option-token")).ToList());
            SaveStore.Write(path, tampered);

            Assert.Throws<SaveFormatException>(() => PersistentSession.Load(path));
        }
        finally
        {
            Cleanup(path);
        }
    }

    [Fact]
    public void Loading_a_save_with_a_different_schema_version_fails_visibly()
    {
        string path = NewSavePath();
        try
        {
            SaveStore.Write(path, new SaveData(SaveStore.CurrentSchemaVersion + 1, SimulationBuild.CurrentId, Seed, Variant, Controlled, Controlled, Array.Empty<SessionCommand>()));

            var ex = Assert.Throws<SaveFormatException>(() => SaveStore.Read(path));
            Assert.Contains("schema version", ex.Message);
        }
        finally
        {
            Cleanup(path);
        }
    }

    [Fact]
    public void Loading_a_save_from_a_different_simulation_build_fails_visibly()
    {
        string path = NewSavePath();
        try
        {
            SaveStore.Write(path, new SaveData(SaveStore.CurrentSchemaVersion, "not-the-real-build-id", Seed, Variant, Controlled, Controlled, Array.Empty<SessionCommand>()));

            var ex = Assert.Throws<SaveFormatException>(() => SaveStore.Read(path));
            Assert.Contains("build", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(path);
        }
    }

    // ================================================================= interrupted writes

    /// <summary>
    /// A genuinely killed writer process — a real, separate OS process (<c>CrimeEmpire.Persistence.
    /// InterruptedWriteHarness</c>), terminated after it has actually begun writing the <c>.tmp</c>
    /// database (schema created, its transaction open) but before that transaction could commit or
    /// the file could be moved onto the real path — leaves the real save path byte-for-byte unchanged
    /// and still loadable.
    ///
    /// Corrected per Codex's review of `9537b38`: the original version locked the <c>.tmp</c> path
    /// <em>before</em> calling <c>Save</c>, so the write it exercised failed at the very first attempt
    /// to open the file — a real failure, but "fails before writing begins" rather than "interrupted
    /// after writing begins", which is what ruling 7 actually asks for and a materially easier case
    /// for <see cref="SaveStore"/> to get right. The harness process and a named, manual-reset
    /// <see cref="EventWaitHandle"/> replace that with explicit, deterministic synchronization rather
    /// than a timing guess: the harness signals the event from inside
    /// <see cref="SaveStore.OnTransactionOpenedForTest"/> and then blocks forever, so this test's own
    /// <see cref="Process.Kill(bool)"/> call always lands at the same point in the harness's progress,
    /// not wherever a race happened to catch it.
    /// </summary>
    [Fact]
    public void A_genuinely_interrupted_writer_process_leaves_the_previous_valid_save_loadable()
    {
        string path = NewSavePath();
        string tmpPath = path + ".tmp";
        string eventName = $"ce-interrupt-{Guid.NewGuid():N}";
        Process? harness = null;
        try
        {
            var first = PersistentSession.Start(Seed, Variant, Controlled);
            AdvanceToNextPause(first);
            first.Save(path);

            byte[] beforeBytes = File.ReadAllBytes(path);
            string beforeTrace = TraceWriter.Render(PersistentSession.Load(path).InnerSession.World, Variant, false);

            using var started = new EventWaitHandle(false, EventResetMode.ManualReset, eventName);

            string harnessDll = Path.Combine(AppContext.BaseDirectory, "CrimeEmpire.Persistence.InterruptedWriteHarness.dll");
            Assert.True(File.Exists(harnessDll),
                $"harness assembly not found at '{harnessDll}' — was its ProjectReference removed from the test project?");

            var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false };
            startInfo.ArgumentList.Add(harnessDll);
            startInfo.ArgumentList.Add(path);
            startInfo.ArgumentList.Add(eventName);

            harness = Process.Start(startInfo);
            Assert.NotNull(harness);

            bool signaled = started.WaitOne(TimeSpan.FromSeconds(15));
            Assert.True(signaled,
                "the harness process never signalled that it had opened a write transaction — this test is not exercising the intended interruption point");

            harness!.Kill(entireProcessTree: true);
            Assert.True(harness.WaitForExit(TimeSpan.FromSeconds(15)), "the harness process did not exit after being killed");

            Assert.True(File.Exists(tmpPath),
                "the killed harness left no .tmp artifact behind — this test is not exercising the intended interruption point");

            byte[] afterBytes = File.ReadAllBytes(path);
            string afterTrace = TraceWriter.Render(PersistentSession.Load(path).InnerSession.World, Variant, false);

            Assert.Equal(beforeBytes, afterBytes);
            Assert.Equal(beforeTrace, afterTrace);
        }
        finally
        {
            harness?.Dispose();
            Cleanup(path);
            foreach (string leftover in new[] { tmpPath, tmpPath + "-journal", tmpPath + "-wal", tmpPath + "-shm" })
                if (File.Exists(leftover)) File.Delete(leftover);
        }
    }

    // ================================================================= helpers

    private static string NewSavePath() => Path.Combine(Path.GetTempPath(), $"ce-persistence-test-{Guid.NewGuid():N}.db");

    private static void Cleanup(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>Presses "Next event" (via <see cref="PersistentSession.StepEvent"/>) until the
    /// controlled character has a decision waiting — the same discipline the Godot shell's "Next
    /// event" button and its self-tests use, bounded so a defect fails the test rather than hangs
    /// it.</summary>
    private static void AdvanceToNextPause(PersistentSession session)
    {
        for (int guard = 0; guard < 5000 && session.Status != SessionStatus.AwaitingChoice; guard++)
            session.StepEvent();

        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
    }

    /// <summary>Chooses the one offered option whose rendered description exactly matches — the same
    /// text and opaque token a Godot button carries, nothing else.</summary>
    private static void ChooseByDescription(PersistentSession session, string description)
    {
        AdvanceToNextPause(session);
        var pending = session.Pending!;

        int index = -1;
        for (int i = 0; i < pending.Options.Count; i++)
        {
            if (!string.Equals(pending.Options[i].Description, description, StringComparison.Ordinal)) continue;
            Assert.Equal(-1, index);
            index = i;
        }

        Assert.True(index >= 0,
            $"no offered option reads \"{description}\" on {session.Date:yyyy-MM-dd} — offered: " +
            string.Join(" | ", pending.Options.Select(o => o.Description)));
        session.Choose(pending.Options[index].Id);
    }

    private static void PlayChoices(PersistentSession session, IEnumerable<string> descriptions)
    {
        foreach (string description in descriptions) ChooseByDescription(session, description);
    }

    /// <summary>
    /// Everything ruling 7 asks "exact internal replay-state identity" to cover, proven by fingerprinting
    /// the complete <paramref name="original"/>/<paramref name="loaded"/> <see cref="PersistentSession"/>
    /// wrapper itself — not <see cref="SimulationSession.World"/> alone — so the mechanism's own
    /// completeness does not depend on which fields somebody thought to name. <see cref="DeepFingerprint"/>
    /// walks every field, public and private, recursively, from that one root: <c>PersistentSession</c>'s
    /// own <c>_log</c> (what a later save would replay), <c>SimulationSession</c>'s <c>_controlledId</c>,
    /// <c>ViewpointCharacterId</c>, <c>Seed</c>, <c>StartedOn</c>, <c>_prepared</c>, <c>_pending</c> in
    /// full (actor name, role, pronouns included, not only the handful of properties a hand-picked check
    /// happened to compare), <c>_optionIds</c>, the clock and fast-forward state, and — through
    /// <c>_world</c> — every field <c>World</c> and everything it owns carries, exactly as before.
    ///
    /// <b>Corrected twice per Codex's review of `9537b38` and `af7d34f`.</b> The first correction fixed
    /// a version that checked <c>World.Queue.Count</c> and a hand-picked list of counters and
    /// per-character strategy fields by deep-fingerprinting <c>World</c> — but still checked
    /// <c>_controlledId</c>, <c>ViewpointCharacterId</c>, <c>Seed</c>, <c>StartedOn</c>, the complete
    /// <c>_pending</c> record, and <c>PersistentSession._log</c> through a second hand-picked list
    /// (or, for the first three and <c>_log</c>, not at all) — the exact failure mode ruling 7's own
    /// "not a hand-picked list" instruction exists to rule out, found again one level up. Fingerprinting
    /// the whole wrapper removes the second list rather than extending it.
    ///
    /// <c>TraceWriter.Render</c> is kept alongside the fingerprint as a second, independently-built
    /// instrument — the one this project has used as its "byte-identical" proof since milestone 009 —
    /// not because the fingerprint needs help, but because two differently-built checks agreeing is
    /// stronger evidence than one. <c>Status</c> and <c>Date</c> are asserted first only so a failure
    /// reads as "paused vs. running" or "wrong date" before the much larger fingerprint diff, not as an
    /// additional completeness mechanism of their own — both are already implied by the fingerprint,
    /// since <c>Status</c> is a pure function of <c>_pending</c>'s nullness and <c>Date</c> of the
    /// fingerprinted <c>_clock</c>.
    /// </summary>
    private static void AssertExactInternalIdentity(PersistentSession original, PersistentSession loaded)
    {
        Assert.Equal(original.Status, loaded.Status);
        Assert.Equal(original.Date, loaded.Date);

        Assert.Equal(
            TraceWriter.Render(original.InnerSession.World, original.Variant, false),
            TraceWriter.Render(loaded.InnerSession.World, loaded.Variant, false));

        var fingerprintA = DeepFingerprint(original, new HashSet<object>(ReferenceEqualityComparer.Instance)).ToList();
        var fingerprintB = DeepFingerprint(loaded, new HashSet<object>(ReferenceEqualityComparer.Instance)).ToList();
        Assert.True(fingerprintA.Count > 100,
            "the deep fingerprint reached very little of the session, so this proves nothing");
        Assert.Equal(fingerprintA, fingerprintB);
    }

    /// <summary>
    /// A structural fingerprint of every field reachable from <paramref name="node"/> — public and
    /// private, instance, recursively through objects, dictionaries, and any other collection — used
    /// only by this test file, never by production code, to prove two independently-built object
    /// graphs are identical in every respect capable of affecting future execution, not merely in the
    /// respects a curated list or a rendered trace happens to cover.
    ///
    /// Dictionaries and <see cref="PriorityQueue{TElement,TPriority}"/> are unwrapped through their
    /// own public logical-content surface (<see cref="IDictionary"/>, <c>UnorderedItems</c>) rather
    /// than by reflecting their backing arrays directly — a backing array can carry excess capacity
    /// and stale slots beyond the collection's logical length, which would make two logically-equal
    /// collections fingerprint as different for a reason that has nothing to do with the state under
    /// test. Every other reference type falls through to raw field reflection, which is what reaches
    /// <c>EventQueue</c>'s own private <c>_queue</c>, <c>_cancelled</c>, and <c>_nextId</c>, and every
    /// one of <c>World</c>'s private identifier counters, without needing to name any of them here.
    ///
    /// Cycle-safe via a caller-supplied, reference-identity-keyed visited set — reference identity
    /// rather than the default equality, because several reachable types are records with their own
    /// structural <c>Equals</c>, and treating two distinct-but-equal records as "already visited"
    /// would under-traverse rather than merely deduplicate.
    /// </summary>
    private static IEnumerable<object> DeepFingerprint(object? node, HashSet<object> visited)
    {
        switch (node)
        {
            case null:
                yield break;
            case string s:
                yield return s;
                yield break;
            case bool or byte or sbyte or short or ushort or int or uint or long or ulong
                or float or double or decimal or DateTime or DateTimeOffset or TimeSpan or Guid:
                yield return node;
                yield break;
        }

        var type = node.GetType();

        if (type.IsEnum)
        {
            yield return node;
            yield break;
        }

        if (!type.IsValueType && !visited.Add(node))
        {
            yield return "<cycle>";
            yield break;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PriorityQueue<,>))
        {
            var unorderedItems = (System.Collections.IEnumerable)type.GetProperty("UnorderedItems")!.GetValue(node)!;
            foreach (var item in unorderedItems)
            foreach (var v in DeepFingerprint(item, visited))
                yield return v;
            yield break;
        }

        if (node is System.Collections.IDictionary dictionary)
        {
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                foreach (var v in DeepFingerprint(entry.Key, visited)) yield return v;
                foreach (var v in DeepFingerprint(entry.Value, visited)) yield return v;
            }
            yield break;
        }

        if (node is System.Collections.IEnumerable sequence)
        {
            foreach (var item in sequence)
            foreach (var v in DeepFingerprint(item, visited))
                yield return v;
            yield break;
        }

        foreach (var field in AllInstanceFields(type))
        {
            object? value;
            try { value = field.GetValue(node); }
            catch { continue; }

            // The field's own name, not only its value — so a value moving from one field to a
            // different field of the same type cannot cancel out in a flat sequence comparison.
            yield return field.Name;
            foreach (var v in DeepFingerprint(value, visited))
                yield return v;
        }
    }

    private static IEnumerable<FieldInfo> AllInstanceFields(Type type)
    {
        for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                yield return f;
    }

    /// <summary>Every string, number, and date reachable from a DTO graph — copied from
    /// <c>PlayerOwnedOperationTests.cs</c> rather than shared, matching this test suite's existing
    /// per-file convention for this helper.</summary>
    private static IEnumerable<object> ValueGraph(object? node)
    {
        switch (node)
        {
            case null:
                yield break;
            case string s:
                yield return s;
                yield break;
            case double or int or long or bool or DateTime:
                yield return node;
                yield break;
            case System.Collections.IEnumerable seq:
                foreach (var item in seq)
                foreach (var v in ValueGraph(item))
                    yield return v;
                yield break;
            default:
                var type = node.GetType();
                if (type.IsEnum) { yield return node; yield break; }
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.GetIndexParameters().Length > 0) continue;
                    foreach (var v in ValueGraph(property.GetValue(node)))
                        yield return v;
                }
                yield break;
        }
    }

    private static IEnumerable<Type> TypesReferencedBy(MemberInfo member) => member switch
    {
        PropertyInfo p => Unwrap(p.PropertyType),
        FieldInfo f => Unwrap(f.FieldType),
        MethodInfo m => Unwrap(m.ReturnType).Concat(m.GetParameters().SelectMany(x => Unwrap(x.ParameterType))),
        ConstructorInfo c => c.GetParameters().SelectMany(x => Unwrap(x.ParameterType)),
        _ => Array.Empty<Type>(),
    };

    private static IEnumerable<Type> Unwrap(Type t)
    {
        yield return t;
        if (t.IsGenericType)
            foreach (var arg in t.GetGenericArguments())
                foreach (var inner in Unwrap(arg))
                    yield return inner;
        if (t.IsArray && t.GetElementType() is { } element)
            foreach (var inner in Unwrap(element))
                yield return inner;
    }
}
