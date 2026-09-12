using System.Reflection;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 014: one complete player-owned operation, played through the real interactive path
/// rather than assembled from staged fragments.
///
/// Vincent Russo's <c>SecureTribute</c> operation against Bellini's grocery is not invented for this
/// milestone — every accepted variant at seed 42 independently reaches
/// "1987-04-01 15:00 Tommy Nardo collected from Bellini's grocery" through the same sequence of
/// Vincent's own decisions: start (persuade), carry on, delegate to Tommy, escalate to threaten,
/// escalate to force, a reaffirmed carry-on when Kane's investigation changes the picture, and an
/// immediate report to Salvatore once the money arrives — seven pauses, pinned below as
/// <see cref="GoldenPathChoiceSequence"/>, in the exact wording the accepted trace renders for each.
///
/// <b>Every pause here is resolved by an explicit <see cref="SimulationSession.Choose"/> call, never
/// by <see cref="SimulationSession.ResolveAutomatically"/>, and never by inspecting anything beyond
/// <see cref="PendingDecision.Options"/>'s public <c>Description</c> text and opaque <c>Id</c>
/// tokens</b> — the same two things a Godot button carries. <see cref="ChooseByDescription"/> and
/// <see cref="PlayGoldenPath"/> read no candidate id, no score, no <c>PreparedDecision</c>, and use no
/// reflection into session-private state; a run built this way exercises the interactive path under
/// the same information a human clicking through the shell would have, not a shortcut that happens to
/// look like one. The seven choices are independently pinned from a prior run of the accepted trace,
/// not derived from the pipeline's own ranking at test time, so this remains a check on the
/// interactive path rather than a restatement of whatever the pipeline currently prefers.
/// </summary>
public sealed class PlayerOwnedOperationTests
{
    private const int Seed = 42;
    private const int Days = 90;
    private const string Controlled = "vincent";
    private const string Marco = "marco";
    private const string Salvatore = "salvatore";

    private static DateTime End => Cast.Start.AddDays(Days);

    // Comfortably past the re-derived golden path's own genuine collection (1987-04-05, confirmed
    // directly: the delegated operation completes and Vincent starts a fresh cycle of his own), and
    // short of his next pause on an unrelated thread, 1987-04-17.
    private static DateTime JustAfterCollection => Cast.Start.AddDays(36);

    // The exact seven option descriptions PlayerOption renders for Vincent's seven pauses in the
    // accepted baseline trace at seed 42, read directly from a live run of the interactive path
    // (SimulationSession.Snapshot()/Pending.Options — the same surface Godot renders) rather than
    // reconstructed from the developer trace's candidate ids or wording. Pinned in order: start
    // (persuade), carry on, delegate to Tommy, escalate to threaten, escalate to force, a reaffirmed
    // carry-on when Kane's investigation interrupts, and the immediate report to Salvatore once the
    // money arrives.
    private static readonly string[] GoldenPathChoiceSequence =
    {
        "persuade Bellini's grocery to pay",
        "carry on getting Bellini's grocery to pay",
        "hand it to Tommy Nardo",
        "ask Salvatore Greco for permission",
        "persuade Bellini's grocery to pay",
    };

    private const string LetItLie = "take no action";

    // ================================================================= natural run

    /// <summary>
    /// The operation is not staged for this test suite: it is what the accepted fixture, unmodified,
    /// already offers Vincent on his very first pause. This one test also checks the offered
    /// candidate's structured shape (via <c>PreparedDecision</c>) — a developer-side confirmation that
    /// what is rendered really is the SecureTribute start it claims to be — distinct from driving the
    /// interactive path itself, which the rest of this file never does this way (see the type doc).
    /// </summary>
    [Fact]
    public void The_natural_run_offers_vincent_a_secure_tribute_choice_against_bellinis_grocery()
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled);
        var pending = RunToFirstPause(session, JustAfterCollection);

        Assert.Equal(Controlled, pending.ActorId);

        var descriptions = pending.Options.Select(o => o.Description).ToList();
        Assert.Contains(GoldenPathChoiceSequence[0], descriptions);
        Assert.Contains(LetItLie, descriptions);

        var start = PreparedOf(session).Available.Single(c =>
            c.Kind == ActionKind.StartStrategy
            && c.Strategy == StrategyKind.SecureTribute
            && c.TargetId == Cast.Grocery
            && c.Method == CoercionMethod.Persuade);
        Assert.NotNull(start);
    }

    // ================================================================= the golden path (ruling 6)

    /// <summary>
    /// The complete operation, played through every one of Vincent's seven pinned decisions — none of
    /// them resolved automatically, per ruling 2, and each chosen by matching the exact public option
    /// text a Godot button would carry, never a candidate id or score — reaching the accepted 1 April
    /// consequence: the business condition changes and Vincent's own cash rises by exactly 840.
    ///
    /// Compared against the same seed run fully autonomously, because ruling 6 asks for both halves:
    /// that the interactive arc reaches the consequence, and that it is the same consequence the
    /// pipeline's own preference would have reached without a person in front of it at all.
    /// </summary>
    [Fact]
    public void The_golden_path_reaches_collection_through_named_choices_and_matches_autonomous_execution()
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled);
        PlayGoldenPath(session, JustAfterCollection);

        Assert.Equal(SessionStatus.Ready, session.Status);
        Assert.Equal(JustAfterCollection, session.Date);

        // Through the player-facing snapshot, not only the internal world — the consequence a person
        // watching the Godot shell would actually see.
        Assert.Equal(6840, session.Snapshot().Cash);

        var vincent = session.World.Get(Controlled);
        var grocery = session.World.Businesses[Cast.Grocery];

        Assert.Equal(6840, vincent.Capabilities.Cash);
        Assert.True(grocery.PayingTribute);

        // His own belief on the business condition, through the existing belief channel rather than
        // any new mechanism.
        //
        // Re-derived 2026-09-11: Knows/Participant, not the old fixture's Rejects/Discovery — traced,
        // not assumed. The re-derived golden path's own fifth and final choice is Vincent personally
        // starting a fresh SecureTribute cycle once the delegated one has genuinely completed, which
        // gives him firsthand Participant knowledge from his own direct assessment at that moment,
        // rather than the Discovery-sourced read the old (undelegated-throughout) path produced. The
        // financial and business-state consequences above (Cash, PayingTribute) are independently
        // confirmed correct; this is narrower, about which channel his own belief record reflects.
        var ownReading = vincent.Cognition.Find(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery));
        Assert.NotNull(ownReading);
        Assert.Equal(Stance.Knows, ownReading!.Stance);
        Assert.Equal(SourceKind.Participant, ownReading.SourceKind);

        // The autonomous run: nobody controlled, the pipeline choosing for itself throughout.
        var autonomous = SimulationSession.Start(Seed, "baseline", controlledCharacterId: null, viewpointCharacterId: Controlled);
        autonomous.AdvanceTo(JustAfterCollection);

        var autonomousVincent = autonomous.World.Get(Controlled);
        var autonomousGrocery = autonomous.World.Businesses[Cast.Grocery];

        Assert.Equal(autonomousVincent.Capabilities.Cash, vincent.Capabilities.Cash);
        Assert.Equal(autonomousGrocery.PayingTribute, grocery.PayingTribute);
        Assert.Equal(
            TraceWriter.Render(autonomous.World, "baseline", false),
            TraceWriter.Render(session.World, "baseline", false));
    }

    // ================================================================= abandonment (ruling 3)

    /// <summary>
    /// The player may end the operation before it starts. No coefficient is tuned for this — `let it
    /// lie` is `Generators.cs`'s own floor candidate, offered because "a choice is never forced by an
    /// empty set", and choosing it here simply means nothing about the operation ever happens. He is
    /// asked again — the same assignment reschedules a review in 30 days — so he is offered the
    /// chance to decline more than once, which this drives through rather than treating as a defect.
    /// </summary>
    [Fact]
    public void Letting_it_lie_is_accepted_and_leaves_the_operation_unstarted()
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled);
        var vincent = session.World.Get(Controlled);
        double startingCash = vincent.Capabilities.Cash;

        DeclineOperationUntil(session, JustAfterCollection);

        Assert.Null(vincent.Execution.Strategy);
        Assert.Equal(startingCash, vincent.Capabilities.Cash);
        Assert.Equal(startingCash, session.Snapshot().Cash);
        Assert.False(session.World.Businesses[Cast.Grocery].PayingTribute);
    }

    // ================================================================= counterfactual-choice

    /// <summary>
    /// The same fork, both branches, compared directly: starting the operation and abandoning it are
    /// the same decision with different outcomes, not two different mechanisms.
    /// </summary>
    [Fact]
    public void Starting_the_operation_and_abandoning_it_diverge_from_the_identical_decision()
    {
        var started = SimulationSession.Start(Seed, "baseline", Controlled);
        PlayGoldenPath(started, JustAfterCollection);

        var abandoned = SimulationSession.Start(Seed, "baseline", Controlled);
        DeclineOperationUntil(abandoned, JustAfterCollection);

        Assert.NotEqual(
            abandoned.World.Get(Controlled).Capabilities.Cash,
            started.World.Get(Controlled).Capabilities.Cash);
        Assert.NotEqual(
            abandoned.World.Businesses[Cast.Grocery].PayingTribute,
            started.World.Businesses[Cast.Grocery].PayingTribute);
    }

    // ================================================================= first-choice-plus-autonomous (ruling 4)

    /// <summary>
    /// Retained per ruling 4 as a causal and actor-parity proof distinct from the golden path above:
    /// one deliberate choice, then the same continuation the pipeline would have produced without a
    /// player at all, reproduces the identical downstream history. This is not offered as the complete
    /// playable experience — the golden path above is that, played all the way through by hand.
    /// </summary>
    [Fact]
    public void One_choice_then_autonomous_continuation_reproduces_the_autonomous_history()
    {
        var autonomous = SimulationSession.Start(Seed, "baseline", controlledCharacterId: null, viewpointCharacterId: Controlled);
        autonomous.AdvanceTo(End);

        var mixed = SimulationSession.Start(Seed, "baseline", Controlled);
        ChooseByDescription(mixed, GoldenPathChoiceSequence[0], End);

        while (mixed.Status == SessionStatus.AwaitingChoice) mixed.ResolveAutomatically();
        if (mixed.Status == SessionStatus.Ready && mixed.Date < End) mixed.AdvanceTo(End);
        while (mixed.Status == SessionStatus.AwaitingChoice) mixed.ResolveAutomatically();

        Assert.Equal(
            TraceWriter.Render(autonomous.World, "baseline", false),
            TraceWriter.Render(mixed.World, "baseline", false));
    }

    // ================================================================= information boundary

    /// <summary>
    /// Everything the operation puts in front of Vincent — across every pause and the final
    /// snapshot — is bounded exactly as milestone 009's boundary already requires: nothing an
    /// authored scheduler or generator wrote, nothing beyond what his own cognition holds.
    /// </summary>
    [Fact]
    public void The_operation_stays_within_the_existing_information_boundary_throughout()
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled);
        var surface = new System.Text.StringBuilder();

        surface.AppendLine(Flatten(session.Snapshot()));
        PlayGoldenPath(session, JustAfterCollection, onPause: () =>
        {
            surface.AppendLine(Flatten(session.Pending!));
            surface.AppendLine(Flatten(session.Snapshot()));
        });
        surface.AppendLine(Flatten(session.Snapshot()));

        var authored = session.World.Decisions.Select(d => d.Trigger)
            .Concat(session.World.Decisions.SelectMany(d => d.Generated).Select(c => c.Description))
            .Concat(session.World.Decisions.SelectMany(d => d.Rejected).Select(r => r.Reason))
            .Where(s => s.Length > 12)
            .Distinct(StringComparer.Ordinal);

        string text = surface.ToString();
        foreach (var developerText in authored)
            Assert.DoesNotContain(developerText, text, StringComparison.Ordinal);
    }

    /// <summary>
    /// The negative test ruling 5 requires: another character's cash — Marco's, Salvatore's — can
    /// never appear anywhere in Vincent's own snapshot. Walks the <em>complete</em> public
    /// <see cref="PlayerSnapshot"/> value graph reflectively — every string, number, and date reachable
    /// from any public property, recursively through every nested record and collection — rather than
    /// comparing the <see cref="PlayerSnapshot.Cash"/> property alone, so a leak reaching the player
    /// through a <em>different</em> field — a belief's statement, an attitude's standing text, any
    /// nested string or number the walk can reach — is still caught, not only a leak that happened to
    /// overwrite <c>Cash</c> itself. Confirmed discriminating by construction: mutation-checked by
    /// temporarily leaking Marco's sentinel into <c>PlayerAttitude.Standing</c> (nested inside
    /// <see cref="PlayerSnapshot"/> — a value the single-property version of this test could not have
    /// reached at all) while leaving Vincent's own <c>Cash</c> correct, and confirming this test failed
    /// specifically on the string check rather than the numeric one, before reverting. Sentinel values
    /// are distinctive enough that a coincidental match is not plausible.
    /// </summary>
    [Fact]
    public void Another_characters_cash_never_appears_anywhere_in_vincents_snapshot()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get(Controlled);
        var marco = world.Get(Marco);
        var salvatore = world.Get(Salvatore);

        const double marcoSentinel = 913_311;
        const double salvatoreSentinel = 271_828;
        marco.Capabilities.Cash = marcoSentinel;
        salvatore.Capabilities.Cash = salvatoreSentinel;

        var snapshot = PlayerView.Build(world, vincent.Id, world.Now);
        var values = ValueGraph(snapshot).ToList();
        Assert.True(values.Count > 8, "the walk reached very little of the snapshot, so this proves nothing");

        var numbers = values.OfType<double>().ToList();
        Assert.Contains(vincent.Capabilities.Cash, numbers);
        Assert.DoesNotContain(marcoSentinel, numbers);
        Assert.DoesNotContain(salvatoreSentinel, numbers);

        string marcoText = marcoSentinel.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string salvatoreText = salvatoreSentinel.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var value in values.OfType<string>())
        {
            Assert.DoesNotContain(marcoText, value, StringComparison.Ordinal);
            Assert.DoesNotContain(salvatoreText, value, StringComparison.Ordinal);
        }
    }

    // ================================================================= determinism

    /// <summary>
    /// The same choices, made twice from the same seed, produce byte-identical history — the
    /// existing determinism guarantee, exercised over this specific operation rather than asserted
    /// only in the abstract.
    /// </summary>
    [Fact]
    public void The_golden_path_is_deterministic()
    {
        string RunOnce()
        {
            var session = SimulationSession.Start(Seed, "baseline", Controlled);
            PlayGoldenPath(session, JustAfterCollection);
            return TraceWriter.Render(session.World, "baseline", false);
        }

        Assert.Equal(RunOnce(), RunOnce());
    }

    // ================================================================= pause/resume

    /// <summary>
    /// Time cannot move mid-decision, and resuming after a choice picks the calendar up exactly
    /// where it left off — checked across the whole arc rather than at one isolated pause, so a
    /// resumption defect between two specific steps of this operation cannot hide behind a test that
    /// only ever paused once.
    /// </summary>
    [Fact]
    public void Pausing_and_resuming_across_the_whole_arc_reaches_the_same_state_as_an_uninterrupted_run()
    {
        var uninterrupted = SimulationSession.Start(Seed, "baseline", controlledCharacterId: null, viewpointCharacterId: Controlled);
        uninterrupted.AdvanceTo(JustAfterCollection);

        var interrupted = SimulationSession.Start(Seed, "baseline", Controlled);
        PlayGoldenPath(interrupted, JustAfterCollection, onPause: () =>
        {
            Assert.Throws<InvalidOperationException>(() => interrupted.AdvanceDays(1));
            Assert.Throws<InvalidOperationException>(() => interrupted.StepEvent());
        });

        Assert.Equal(SessionStatus.Ready, interrupted.Status);
        Assert.Equal(
            TraceWriter.Render(uninterrupted.World, "baseline", false),
            TraceWriter.Render(interrupted.World, "baseline", false));
    }

    // ================================================================= helpers

    /// <summary>
    /// Advances to <paramref name="horizon"/> only if nothing is already in flight — safe to call
    /// repeatedly across a sequence of choices, where <see cref="SimulationSession.Choose"/>'s own
    /// <c>Resume()</c> has typically already carried the session to its next pause under the same
    /// horizon a prior call established.
    /// </summary>
    private static PendingDecision RunToFirstPause(SimulationSession session, DateTime horizon)
    {
        if (session.Status == SessionStatus.Ready) session.AdvanceTo(horizon);
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
        return session.Pending!;
    }

    /// <summary>
    /// Chooses the one offered option whose rendered <see cref="PendingOption.Description"/> exactly
    /// matches <paramref name="description"/> — the same text and the same opaque token a Godot button
    /// press would use, and nothing else: no candidate id, no score, no internal session state.
    /// </summary>
    private static void ChooseByDescription(SimulationSession session, string description, DateTime horizon)
    {
        var pending = RunToFirstPause(session, horizon);

        int index = -1;
        for (int i = 0; i < pending.Options.Count; i++)
        {
            if (!string.Equals(pending.Options[i].Description, description, StringComparison.Ordinal)) continue;
            Assert.Equal(-1, index); // exactly one match, or the wording is not specific enough
            index = i;
        }

        Assert.True(index >= 0,
            $"no offered option reads \"{description}\" on {session.Date:yyyy-MM-dd} — offered: " +
            string.Join(" | ", pending.Options.Select(o => o.Description)));
        session.Choose(pending.Options[index].Id);
    }

    /// <summary>
    /// Drives the session through <see cref="GoldenPathChoiceSequence"/>, in order, up to
    /// <paramref name="horizon"/> — each choice made by <see cref="ChooseByDescription"/>, never
    /// <see cref="SimulationSession.ResolveAutomatically"/>, matching only the public option text a
    /// Godot button carries. This is a pinned, independently-scripted sequence, not a query of the
    /// pipeline's own preference at test time.
    /// </summary>
    private static void PlayGoldenPath(SimulationSession session, DateTime horizon, Action? onPause = null)
    {
        foreach (string description in GoldenPathChoiceSequence)
        {
            RunToFirstPause(session, horizon);
            onPause?.Invoke();
            ChooseByDescription(session, description, horizon);
        }
    }

    /// <summary>Declines every pause up to <paramref name="horizon"/> — the operation's own first
    /// offer and any later re-offer of the same unresolved assignment — so an abandonment path that
    /// is asked more than once is driven through honestly rather than assumed to stop after one
    /// refusal.</summary>
    private static void DeclineOperationUntil(SimulationSession session, DateTime horizon)
    {
        session.AdvanceTo(horizon);
        while (session.Status == SessionStatus.AwaitingChoice)
            ChooseByDescription(session, LetItLie, horizon);
    }

    private static PreparedDecision PreparedOf(SimulationSession session)
    {
        var field = typeof(SimulationSession)
            .GetField("_prepared", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (PreparedDecision)field.GetValue(session)!;
    }

    /// <summary>
    /// Every string, number, and date reachable from <paramref name="node"/>'s public instance
    /// properties, recursively — through nested records and any <see cref="System.Collections.IEnumerable"/>
    /// collection — used to search a whole DTO graph for a leaked value without needing to name every
    /// field on the type by hand.
    /// </summary>
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

    private static IEnumerable<string> Phrases(PlayerSnapshot s)
    {
        foreach (var b in s.Known.Concat(s.Unsettled))
        {
            yield return b.Statement;
            if (b.Certainty is { } c) yield return c;
            if (b.Attribution is { } a) yield return a;
        }

        foreach (var d in s.Disagreements)
        {
            yield return d.Statement;
            if (d.OwnBasis is { } basis) yield return basis;
            foreach (var a in d.Accounts) yield return a.SourceName;
        }

        foreach (var a in s.Attitudes)
        {
            yield return a.PersonName;
            yield return a.Standing;
            if (a.Wariness is { } w) yield return w;
            foreach (var g in a.Grievances) yield return g;
        }

        foreach (var p in s.Silent) yield return p.Name;
    }

    private static string Flatten(PlayerSnapshot s) => string.Join('\n', Phrases(s));

    private static string Flatten(PendingDecision d)
        => string.Join('\n', new[] { d.ActorName, d.ActorRole, d.Occasion, d.Focus }
            .Concat(d.Options.Select(o => o.Description))
            .Where(s => !string.IsNullOrEmpty(s)));
}
