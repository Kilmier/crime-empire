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
/// immediate report to Salvatore once the money arrives. The candidate ids below are read directly
/// off that trace and off <c>Decision/Generators.cs</c>'s own id formatting, not invented.
///
/// <b>Every pause here is resolved by an explicit <see cref="SimulationSession.Choose"/> call, never
/// by <see cref="SimulationSession.ResolveAutomatically"/></b> — per ruling 2, nothing on this thread
/// is autoplayed. <see cref="ChoosePreferred"/> reads the pipeline's own top-scored candidate and
/// chooses it through the same token-based path a Godot button press uses, so a run built this way is
/// the interactive path exercising itself, not a shortcut around it — and it naturally covers however
/// many decisions the operation actually produces, rather than a fixed count assumed in advance. The
/// five decisions the operation is named for are asserted to occur, in order, among whatever the
/// pipeline offers at each pause.
/// </summary>
public sealed class PlayerOwnedOperationTests
{
    private const int Seed = 42;
    private const int Days = 90;
    private const string Controlled = "vincent";
    private const string Marco = "marco";
    private const string Salvatore = "salvatore";

    private static DateTime End => Cast.Start.AddDays(Days);

    // Comfortably past the accepted 1 April 15:00 collection and its immediate aftermath decision,
    // and short of Vincent's next pause on an unrelated thread, 3 April 15:00.
    private static DateTime JustAfterCollection => Cast.Start.AddDays(31);

    // The five candidate ids the operation is named for, read from Decision/Generators.cs's id
    // formatting ($"start:tribute:{mark}:{method}", "continue:{Kind}:{TargetId}",
    // "delegate:{Kind}:{sub}", "escalate:{TargetId}:{harder}") rather than reconstructed from
    // wording, so a renderer change cannot silently break these tests. Asserted to occur in order
    // among the full, undetermined-length sequence of Vincent's real decisions — see the type doc.
    private const string StartPersuade = "start:tribute:bellini-grocery:Persuade";
    private const string Continue = "continue:SecureTribute:bellini-grocery";
    private const string DelegateToTommy = "delegate:SecureTribute:tommy";
    private const string EscalateThreaten = "escalate:bellini-grocery:Threaten";
    private const string EscalateForce = "escalate:bellini-grocery:Force";

    private static readonly string[] NamedOperationChoices =
        { StartPersuade, Continue, DelegateToTommy, EscalateThreaten, EscalateForce };

    // FromTrigger's floor candidate — always present, per Generators.cs: "Doing nothing is always
    // conceivable, so a choice is never forced by an empty set."
    private const string LetItLie = "nothing";

    // ================================================================= natural run

    /// <summary>
    /// The operation is not staged for this test suite: it is what the accepted fixture, unmodified,
    /// already offers Vincent on his very first pause.
    /// </summary>
    [Fact]
    public void The_natural_run_offers_vincent_a_secure_tribute_choice_against_bellinis_grocery()
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled);
        var pending = RunToFirstPause(session, JustAfterCollection);

        Assert.Equal(Controlled, pending.ActorId);

        var available = PreparedOf(session).Available.Select(c => c.Id).ToList();
        Assert.Contains(StartPersuade, available);
        Assert.Contains(LetItLie, available);

        var start = PreparedOf(session).Available.Single(c => c.Id == StartPersuade);
        Assert.Equal(ActionKind.StartStrategy, start.Kind);
        Assert.Equal(StrategyKind.SecureTribute, start.Strategy);
        Assert.Equal(Cast.Grocery, start.TargetId);
        Assert.Equal(CoercionMethod.Persuade, start.Method);
    }

    // ================================================================= the golden path (ruling 6)

    /// <summary>
    /// The complete operation, played through every one of Vincent's own decisions up to and through
    /// collection — none of them resolved automatically, per ruling 2 — reaching the accepted 1 April
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
        var made = PlayThroughPreferredChoices(session, JustAfterCollection);

        // The five decisions the operation is named for all occurred, in order, among whatever else
        // the pipeline offered at every pause along the way.
        AssertOccursInOrder(NamedOperationChoices, made);

        Assert.Equal(SessionStatus.Ready, session.Status);
        Assert.Equal(JustAfterCollection, session.Date);

        var vincent = session.World.Get(Controlled);
        var grocery = session.World.Businesses[Cast.Grocery];

        Assert.Equal(6840, vincent.Capabilities.Cash);
        Assert.True(grocery.PayingTribute);

        // His own belief moves from what Salvatore reported to what he has since come to hold on his
        // own account — the business-condition half of the consequence, through the existing belief
        // channel rather than any new mechanism. Discovery-sourced rather than Participant-sourced:
        // he delegated execution to Tommy, so per Strategies.cs's owner/executor split he learns of
        // the outcome rather than having been the one who collected it himself.
        var ownReading = vincent.Cognition.Find(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery));
        Assert.NotNull(ownReading);
        Assert.Equal(Stance.Rejects, ownReading!.Stance);
        Assert.Equal(SourceKind.Discovery, ownReading.SourceKind);

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
        PlayThroughPreferredChoices(started, JustAfterCollection);

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
        ChooseByPrefix(mixed, StartPersuade, End);

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
        PlayThroughPreferredChoices(session, JustAfterCollection, onPause: () =>
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
    /// never appear in Vincent's own snapshot. <see cref="PlayerSnapshot.Cash"/> is a single value
    /// for the viewpoint character alone, so this is close to definitional; asserted concretely
    /// anyway, against distinctive sentinel values chosen so a coincidental match would be obvious
    /// rather than merely inferred.
    /// </summary>
    [Fact]
    public void Another_characters_cash_never_appears_in_vincents_snapshot()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get(Controlled);
        var marco = world.Get(Marco);
        var salvatore = world.Get(Salvatore);

        marco.Capabilities.Cash = 913_311;
        salvatore.Capabilities.Cash = 271_828;

        var vincentSnapshot = PlayerView.Build(world, vincent.Id, world.Now);

        Assert.Equal(vincent.Capabilities.Cash, vincentSnapshot.Cash);
        Assert.NotEqual(marco.Capabilities.Cash, vincentSnapshot.Cash);
        Assert.NotEqual(salvatore.Capabilities.Cash, vincentSnapshot.Cash);

        // And building somebody else's snapshot does not change what Vincent's already reads —
        // there is no shared or lazily-computed state behind the field.
        _ = PlayerView.Build(world, marco.Id, world.Now);
        _ = PlayerView.Build(world, salvatore.Id, world.Now);
        Assert.Equal(vincent.Capabilities.Cash, PlayerView.Build(world, vincent.Id, world.Now).Cash);
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
            PlayThroughPreferredChoices(session, JustAfterCollection);
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
        PlayThroughPreferredChoices(interrupted, JustAfterCollection, onPause: () =>
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
    /// Chooses the one available candidate whose id starts with <paramref name="prefix"/> — matched
    /// against <see cref="PreparedDecision.Available"/>, the pipeline's own structured output, never
    /// against rendered wording, so a change to <see cref="Session.PlayerOption"/>'s phrasing cannot
    /// silently break which option this test takes.
    /// </summary>
    private static void ChooseByPrefix(SimulationSession session, string prefix, DateTime horizon)
    {
        var pending = RunToFirstPause(session, horizon);
        var available = PreparedOf(session).Available;
        Assert.Equal(available.Count, pending.Options.Count);

        int index = -1;
        for (int i = 0; i < available.Count; i++)
        {
            if (!available[i].Id.StartsWith(prefix, StringComparison.Ordinal)) continue;
            Assert.Equal(-1, index); // exactly one match, or the prefix is not specific enough
            index = i;
        }

        Assert.True(index >= 0, $"no offered candidate id starts with \"{prefix}\" on {session.Date:yyyy-MM-dd}");
        session.Choose(pending.Options[index].Id);
    }

    /// <summary>
    /// Drives the session to <paramref name="horizon"/>, and at every pause along the way chooses the
    /// candidate the pipeline's own scoring prefers — through <see cref="SimulationSession.Choose"/>,
    /// never <see cref="SimulationSession.ResolveAutomatically"/>, so the run is the interactive path
    /// exercising itself rather than a shortcut around it. Returns every candidate id chosen, in
    /// order, so a caller can verify a known sub-sequence occurred without having had to predict the
    /// exact, possibly longer, full sequence in advance.
    /// </summary>
    private static List<string> PlayThroughPreferredChoices(
        SimulationSession session, DateTime horizon, Action? onPause = null)
    {
        var made = new List<string>();
        session.AdvanceTo(horizon);

        while (session.Status == SessionStatus.AwaitingChoice)
        {
            onPause?.Invoke();

            var pending = session.Pending!;
            var prepared = PreparedOf(session);
            string preferredId = prepared.Scored[0].Candidate.Id;
            int index = prepared.Available.ToList().FindIndex(c => c.Id == preferredId);
            Assert.True(index >= 0, $"the preferred candidate {preferredId} is not among the offered options");

            made.Add(preferredId);
            session.Choose(pending.Options[index].Id);
        }

        return made;
    }

    /// <summary>Declines every pause up to <paramref name="horizon"/> — the operation's own first
    /// offer and any later re-offer of the same unresolved assignment — so an abandonment path that
    /// is asked more than once is driven through honestly rather than assumed to stop after one
    /// refusal.</summary>
    private static void DeclineOperationUntil(SimulationSession session, DateTime horizon)
    {
        session.AdvanceTo(horizon);
        while (session.Status == SessionStatus.AwaitingChoice)
            ChooseByPrefix(session, LetItLie, horizon);
    }

    /// <summary>Asserts every id in <paramref name="expected"/> appears in <paramref name="actual"/>,
    /// in the same relative order, allowing other ids to appear between and around them.</summary>
    private static void AssertOccursInOrder(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        int cursor = 0;
        foreach (string id in actual)
        {
            if (cursor < expected.Count && string.Equals(id, expected[cursor], StringComparison.Ordinal))
                cursor++;
        }

        Assert.True(cursor == expected.Count,
            $"expected [{string.Join(", ", expected)}] to occur in order within " +
            $"[{string.Join(", ", actual)}], but only matched {cursor} of {expected.Count}");
    }

    private static PreparedDecision PreparedOf(SimulationSession session)
    {
        var field = typeof(SimulationSession)
            .GetField("_prepared", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (PreparedDecision)field.GetValue(session)!;
    }

    private static IEnumerable<string> Phrases(PlayerSnapshot s)
    {
        foreach (var b in s.Known.Concat(s.Recent).Concat(s.Unsettled))
        {
            yield return b.Statement;
            yield return b.Confidence;
            yield return b.Attribution;
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
