using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CrimeEmpire.Persistence;
using CrimeEmpire.Persistence.Session;
using CrimeSim.Scenario;
using CrimeSim.Session;
using Godot;

namespace CrimeEmpire.GodotShell;

/// <summary>
/// The whole interface.
///
/// <b>What it is allowed to know.</b> One <see cref="SimulationSession"/>, and through it exactly
/// two things: a <see cref="PlayerSnapshot"/> and a <see cref="PendingDecision"/>, which is the
/// controlled character's own options. Ruling 1 (milestone 014) states the snapshot's contract
/// precisely: it may expose the viewpoint character's own private state, cognition, and legitimately
/// known information — his own cash among it — but no other character's private state, no world
/// truth, no utility score, and no reference or path back to mutable simulation state. It cannot
/// reach the world, the truth log, a decision record, a utility score or anybody else's beliefs,
/// because the session does not expose them — that is enforced by the type system, not by this
/// file's restraint.
///
/// <b>It consumes structured data and never parses text.</b> Nothing here reads
/// <c>IntelligenceWriter</c>'s console rendering; every label below is built from a snapshot field.
/// The console renderer and this file are two layouts over one source-limited derivation, which is
/// the point of <see cref="PlayerView"/> existing.
///
/// <b>It is deliberately plain.</b> No theme, no art, no animation, no map. Labels in columns and
/// buttons that move the clock, plus milestone 015's one fixed save slot. Milestone 009's scope
/// forbids polish beyond a clear functional layout, and there is a reason beyond time: a shell that
/// looked finished would invite judgements about the game that only the simulation can earn.
///
/// <b>Save and load (milestone 015).</b> One fixed slot, <see cref="ProductionSavePath"/> — no file
/// picker, no slot management, no autosave. <see cref="PersistentSession"/> is the only new thing
/// this file knows about beyond milestone 014's boundary: it still hands out nothing but
/// <see cref="PlayerSnapshot"/> and <see cref="PendingDecision"/>, and never <c>World</c>.
///
/// <b>The restart self-tests never touch the production slot.</b> Corrected per Codex's review of
/// `9537b38`, which found `--selftest-restart-save` deleting and overwriting
/// <c>user://crime-empire-save.db</c> directly — the same file a real player's save lives in.
/// <see cref="_activeSavePath"/> is resolved once, in <see cref="_Ready"/>, before any button exists:
/// a real launch resolves it to <see cref="ProductionSavePath"/>, and a restart self-test launch
/// resolves it to <see cref="SelfTestRestartSavePath"/> instead. Every Save/Load button's handler
/// reads <see cref="_activeSavePath"/>, never either constant directly, so the two self-test flags
/// exercise the exact same button-press code path a player uses while never being able to reach the
/// player's own file.
/// </summary>
public partial class Game : Control
{
    /// <summary>Command-line switch that drives the real interface headlessly and dumps what it built.</summary>
    private const string SelfTestFlag = "--selftest";

    /// <summary>
    /// Command-line switch for milestone 014's golden path: Vincent's existing seed-42
    /// <c>SecureTribute</c> operation against Bellini's grocery, played through real button presses
    /// rather than the general self-test's "always take the first option" policy, reaching the
    /// accepted 1 April consequence and reading the rendered cash off the live screen.
    /// </summary>
    private const string GoldenPathFlag = "--selftest-goldenpath";

    /// <summary>
    /// Command-line switch for milestone 017's direct-action proof: the same seed-42
    /// <c>SecureTribute</c> operation, played through real button presses that never delegate to
    /// Tommy — Vincent presses "carry on" at the exact pause that also offers delegation, then
    /// escalates personally, reaching a naturally divergent, personally-executed consequence.
    /// </summary>
    private const string DirectActionFlag = "--selftest-directaction";

    /// <summary>
    /// Command-line switch for milestone 018's first natural proof: Salvatore's own seed-42
    /// <c>cautious-vincent</c> ask — "ask Vincent Russo for his own account of whether Bellini's
    /// grocery is holding back what it owes" — played through real button presses. Proves the request
    /// renders as an immediate, unresolved acknowledgement, and that Vincent's own naturally-occurring
    /// answer (observed directly before this flag was written, never staged or tuned) resolves it and
    /// attributes the account to him on the live screen.
    /// </summary>
    private const string CorroborationFlag = "--selftest-corroboration";

    /// <summary>
    /// Command-line switch for milestone 018's second natural proof: Marco's own seed-42
    /// <c>baseline</c> first tribute demand, played through real button presses. Proves the decision
    /// panel names the demander before Marco chooses, and that his response renders an immediate
    /// acknowledgement plus his business's own known paying status.
    /// </summary>
    private const string TributeFlag = "--selftest-tribute";

    /// <summary>
    /// Command-line switch for milestone 015's restart proof, process A: plays the golden path's
    /// first three choices (start, carry on, delegate to Tommy) through real buttons, saves to
    /// <see cref="SelfTestRestartSavePath"/> (never the production slot — see the type header), and
    /// exits. Meant to be run as a genuinely separate OS process from <see cref="RestartLoadFlag"/> —
    /// see the milestone archive for the exact two-invocation proof.
    /// </summary>
    private const string RestartSaveFlag = "--selftest-restart-save";

    /// <summary>
    /// Command-line switch for milestone 015's restart proof, process B: loads
    /// <see cref="SelfTestRestartSavePath"/> — written by a prior, separate
    /// <see cref="RestartSaveFlag"/> process — and plays the golden path's remaining four choices
    /// through real buttons, reaching the accepted 1 April consequence.
    /// </summary>
    private const string RestartLoadFlag = "--selftest-restart-load";

    /// <summary>How far the self-test runs the scenario, matching the runner's default span.</summary>
    private const int SelfTestDays = 90;

    /// <summary>
    /// The one fixed save slot a real player's Save/Load buttons write to (ruling 4). Overridable via
    /// <c>CE_SAVE_PATH_OVERRIDE</c> purely so a verification can prove the restart self-tests never
    /// touch it without writing to Matt's own real file to do so — unset, which is every real launch,
    /// this resolves to the actual <c>user://</c> slot exactly as before. Globalized rather than a
    /// compile-time constant: <c>user://</c> only resolves to a real path once Godot's engine is up.
    /// </summary>
    private static string ProductionSavePath
    {
        get
        {
            string? overridePath = System.Environment.GetEnvironmentVariable("CE_SAVE_PATH_OVERRIDE");
            return string.IsNullOrEmpty(overridePath)
                ? ProjectSettings.GlobalizePath("user://crime-empire-save.db")
                : overridePath;
        }
    }

    /// <summary>
    /// The restart self-tests' own dedicated slot — a different file from
    /// <see cref="ProductionSavePath"/> under every circumstance, including the override above, so a
    /// misconfigured environment cannot accidentally point both at the same file.
    /// </summary>
    private static string SelfTestRestartSavePath
        => ProjectSettings.GlobalizePath("user://crime-empire-selftest-restart-save.db");

    private VBoxContainer _root = null!;
    private PersistentSession? _session;
    private string? _statusMessage;

    /// <summary>
    /// Which file this process's Save/Load buttons actually read and write. Resolved once, in
    /// <see cref="_Ready"/>, before any button exists — see the type header's "restart self-tests
    /// never touch the production slot".
    /// </summary>
    private string _activeSavePath = null!;

    // Start-screen state, read once when the game begins and not consulted afterwards.
    private LineEdit _seedField = null!;
    private OptionButton _variantField = null!;
    private OptionButton _controlledField = null!;
    private OptionButton _viewpointField = null!;

    private IReadOnlyList<ScenarioCharacter> _cast = Array.Empty<ScenarioCharacter>();
    private IReadOnlyList<ScenarioVariant> _variants = Array.Empty<ScenarioVariant>();

    public override void _Ready()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 14);
        AddChild(margin);

        _root = new VBoxContainer();
        margin.AddChild(_root);

        _cast = Roster.Characters("baseline");
        _variants = Roster.Variants();

        _activeSavePath = FlagRequested(RestartSaveFlag) || FlagRequested(RestartLoadFlag)
            ? SelfTestRestartSavePath
            : ProductionSavePath;

        if (SelfTestRequested())
        {
            RunSelfTest();
            return;
        }

        if (GoldenPathRequested())
        {
            RunGoldenPathSelfTest();
            return;
        }

        if (DirectActionRequested())
        {
            RunDirectActionSelfTest();
            return;
        }

        if (FlagRequested(CorroborationFlag))
        {
            RunCorroborationSelfTest();
            return;
        }

        if (FlagRequested(TributeFlag))
        {
            RunTributeSelfTest();
            return;
        }

        if (FlagRequested(RestartSaveFlag))
        {
            RunRestartSaveSelfTest();
            return;
        }

        if (FlagRequested(RestartLoadFlag))
        {
            RunRestartLoadSelfTest();
            return;
        }

        BuildStartScreen();
    }

    // ================================================================= start screen

    /// <summary>
    /// Seed, scenario variant, who is controlled, and whose eyes the world is seen through.
    ///
    /// The cast and variant lists come from <see cref="Roster"/> rather than from a world, so
    /// populating a dropdown never touches simulation state. Listing the cast here is a fact about
    /// the game being started and not about anybody's knowledge; nothing on the main screen
    /// enumerates people the viewpoint character has not heard of.
    /// </summary>
    private void BuildStartScreen()
    {
        Clear(_root);

        _root.AddChild(Heading("CRIMINAL EMPIRE"));
        _root.AddChild(Plain("A headless simulation with a window cut into it. Choose a starting point."));
        _root.AddChild(new HSeparator());

        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 8);
        _root.AddChild(grid);

        grid.AddChild(Plain("Seed"));
        _seedField = new LineEdit { Text = "42", CustomMinimumSize = new Vector2(220, 0) };
        grid.AddChild(_seedField);

        grid.AddChild(Plain("Scenario"));
        _variantField = new OptionButton();
        foreach (var v in _variants) _variantField.AddItem($"{v.Id} — {v.Description}");
        _variantField.Selected = 0;
        grid.AddChild(_variantField);

        grid.AddChild(Plain("You control"));
        _controlledField = new OptionButton();
        _controlledField.AddItem("nobody — watch the simulation run");
        foreach (var c in _cast) _controlledField.AddItem($"{c.Name} — {c.RoleTitle}");
        _controlledField.Selected = 1 + IndexOfCharacter(Roster.DefaultControlledId);
        grid.AddChild(_controlledField);

        grid.AddChild(Plain("You see through"));
        _viewpointField = new OptionButton();
        foreach (var c in _cast) _viewpointField.AddItem($"{c.Name} — {c.RoleTitle}");
        _viewpointField.Selected = IndexOfCharacter(Roster.DefaultControlledId);
        grid.AddChild(_viewpointField);

        _root.AddChild(new HSeparator());

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 12);
        _root.AddChild(buttons);

        var begin = new Button { Text = "Begin" };
        begin.Pressed += BeginFromStartScreen;
        buttons.AddChild(begin);

        var load = new Button { Text = "Load saved game", Disabled = !SaveStore.Exists(_activeSavePath) };
        load.Pressed += LoadFixedSlot;
        buttons.AddChild(load);

        _root.AddChild(Plain(
            "Controlling somebody stops the clock whenever they have a decision to make, and offers " +
            "what actually occurred to them. Everyone else goes on deciding for themselves either " +
            "way."));

        if (_statusMessage is { } status) _root.AddChild(Faint($"· {status}"));
    }

    private int IndexOfCharacter(string id)
    {
        for (int i = 0; i < _cast.Count; i++)
            if (string.Equals(_cast[i].Id, id, StringComparison.Ordinal)) return i;
        return 0;
    }

    private void BeginFromStartScreen()
    {
        int seed = int.TryParse(_seedField.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : 42;

        string variant = _variants[Math.Max(0, _variantField.Selected)].Id;

        // Index 0 is "nobody"; every later entry is a character in cast order.
        int controlledIndex = _controlledField.Selected - 1;
        string? controlled = controlledIndex >= 0 && controlledIndex < _cast.Count
            ? _cast[controlledIndex].Id
            : null;

        string viewpoint = _cast[Math.Max(0, _viewpointField.Selected)].Id;

        StartSession(seed, variant, controlled, viewpoint);
    }

    private void StartSession(int seed, string variant, string? controlled, string viewpoint)
    {
        _session = PersistentSession.Start(seed, variant, controlled, viewpoint);
        _statusMessage = null;
        Refresh();
    }

    /// <summary>
    /// Loads the one fixed save slot. A save that fails <see cref="SaveStore"/>'s own checks — wrong
    /// schema, a different simulation build, a corrupted or reordered command log — throws rather
    /// than falling back to anything, and this catches only to turn that throw into the same plain
    /// status line a failed save reports, not to paper over it: the session is left exactly as it was
    /// (unset, if this is the start screen) rather than half-loaded.
    /// </summary>
    private void LoadFixedSlot()
    {
        try
        {
            _session = PersistentSession.Load(_activeSavePath);
            _statusMessage = "loaded";
        }
        catch (Exception ex) when (ex is SaveFormatException or InvalidOperationException)
        {
            _statusMessage = $"load failed — {ex.Message}";
        }

        RefreshCurrentScreen();
    }

    private void SaveFixedSlot(PersistentSession session)
    {
        try
        {
            session.Save(_activeSavePath);
            _statusMessage = "saved";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _statusMessage = $"save failed — {ex.Message}";
        }

        RefreshCurrentScreen();
    }

    /// <summary>Rebuilds whichever screen is live — the start screen when no session exists yet
    /// (a failed load from there leaves it that way), the main screen otherwise.</summary>
    private void RefreshCurrentScreen()
    {
        if (_session is null) BuildStartScreen();
        else Refresh();
    }

    // ================================================================= main screen

    private void Refresh()
    {
        if (_session is not { } session) return;

        Clear(_root);

        var snapshot = session.Snapshot();

        _root.AddChild(BuildToolbar(session, snapshot));
        if (_statusMessage is { } status) _root.AddChild(Faint($"· {status}"));
        _root.AddChild(new HSeparator());

        var columns = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 18);
        _root.AddChild(columns);

        columns.AddChild(Column("WHAT HE KNOWS", BuildKnowledge(snapshot)));
        columns.AddChild(Column("LATELY", BuildRecent(snapshot)));
        columns.AddChild(Column("WHAT JUST HAPPENED", BuildCausalThread(snapshot)));
        columns.AddChild(Column("HOW HE TAKES THEM", BuildAttitudes(snapshot)));
        columns.AddChild(Column(
            session.ControlledCharacterId is null ? "NOBODY IS BEING CONTROLLED" : "A DECISION",
            BuildDecision(session, snapshot)));
    }

    private Control BuildToolbar(PersistentSession session, PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;
        var bar = new HBoxContainer();
        bar.AddThemeConstantOverride("separation", 12);

        bar.AddChild(Heading(session.Date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)));
        bar.AddChild(Plain($"· {snapshot.ViewpointName}, {snapshot.ViewpointRole}"));
        bar.AddChild(Plain($"· cash on hand {snapshot.Cash.ToString("N0", CultureInfo.InvariantCulture)}"));
        bar.AddChild(Plain($"· seed {session.Seed.ToString(CultureInfo.InvariantCulture)} · {session.Variant}"));

        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        bar.AddChild(spacer);

        bool paused = session.Status == SessionStatus.AwaitingChoice;

        bar.AddChild(Advance("Next event", paused, () => session.StepEvent()));
        bar.AddChild(Advance("Advance a day", paused, () => session.AdvanceDays(1)));
        bar.AddChild(Advance("Advance a week", paused, () => session.AdvanceDays(7)));

        // Save and load work in either state (ruling 6) — unlike the three clock controls above,
        // neither is disabled while paused.
        var save = new Button { Text = "Save" };
        save.Pressed += () => SaveFixedSlot(session);
        bar.AddChild(save);

        var load = new Button { Text = "Load" };
        load.Pressed += LoadFixedSlot;
        bar.AddChild(load);

        bar.AddChild(Plain(paused
            ? $"· paused — {p.Subject} {p.Verb("has", "have")} something to decide"
            : "· running"));

        return bar;
    }

    /// <summary>
    /// A control that moves the clock, disabled while a decision is outstanding.
    ///
    /// Disabled rather than hidden, and the session refuses the call as well: a half-handled event
    /// is not a place time can move through, and the history would otherwise depend on how long
    /// somebody took to answer.
    /// </summary>
    private Button Advance(string label, bool paused, Action move)
    {
        var button = new Button { Text = label, Disabled = paused };
        button.Pressed += () =>
        {
            move();
            Refresh();
        };
        return button;
    }

    private IEnumerable<Control> BuildKnowledge(PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;
        if (snapshot.Known.Count == 0)
        {
            yield return Plain($"Nothing. Nobody has told {p.Object} anything and {p.Subject} {p.Verb("has", "have")} seen nothing {p.Reflexive}.");
            yield break;
        }

        foreach (var belief in snapshot.Known)
        {
            yield return Plain(
                $"{belief.AcquiredAt.ToString("d MMM", CultureInfo.InvariantCulture)}  " +
                $"{belief.Statement}{(belief.Contested ? "  (contradicted)" : "")}");
            yield return Faint($"        {belief.Confidence}, {belief.Attribution}");
        }

        if (snapshot.Unsettled.Count > 0 || snapshot.Silent.Count > 0)
        {
            yield return new HSeparator();
            yield return Plain("WHAT HE CANNOT SETTLE");
            foreach (var belief in snapshot.Unsettled)
                yield return Faint($"· whether {belief.Statement} — {belief.Confidence}");
            foreach (var person in snapshot.Silent)
                yield return Faint($"· {person.Name} has not given {snapshot.ViewpointPronouns.Object} an account");
        }
    }

    /// <summary>
    /// Recent observable consequences — and only where the viewpoint character could know them.
    ///
    /// Every entry is something he holds; the window only decides how much of what he already knows
    /// is worth putting in front of him first. Ordered by when he last had cause to think about it,
    /// so a three-week-old belief somebody contradicted yesterday reads as news, which is what it is.
    /// </summary>
    private IEnumerable<Control> BuildRecent(PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;
        if (snapshot.Recent.Count == 0)
        {
            yield return Plain($"Nothing has reached {snapshot.ViewpointPronouns.Object} lately.");
            yield break;
        }

        foreach (var belief in snapshot.Recent)
        {
            yield return Plain(
                $"{belief.ReconsideredAt.ToString("d MMM", CultureInfo.InvariantCulture)}  {belief.Statement}");
            yield return Faint($"        {belief.Confidence}, {belief.Attribution}");
        }

        foreach (var disagreement in snapshot.Disagreements)
        {
            yield return new HSeparator();
            yield return Plain($"Accounts differ on whether {disagreement.Statement}");
            if (disagreement.OwnBasis is { } basis)
                yield return Faint($"    {basis} — {(disagreement.OwnPositionHeld ? "it happened" : "it did not")}");
            foreach (var account in disagreement.Accounts)
                yield return Faint(
                    $"    {account.SourceName} — {(account.Affirms ? "it happened" : "it did not")} " +
                    $"({account.At.ToString("d MMM", CultureInfo.InvariantCulture)})");
        }
    }

    private IEnumerable<Control> BuildAttitudes(PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;
        if (snapshot.Attitudes.Count == 0)
        {
            yield return Plain($"{p.Subject_} {p.Verb("has", "have")} nothing much to say about anybody.");
        }
        else
        {
            foreach (var attitude in snapshot.Attitudes)
            {
                yield return Plain(attitude.PersonName);
                yield return Faint($"    {attitude.Standing}");
                if (attitude.Wariness is { } wariness)
                    yield return Faint($"    {wariness}");
                foreach (var grievance in attitude.Grievances)
                    yield return Faint(
                        $"    what {p.Subject} {p.Verb("holds", "hold")} against " +
                        $"{attitude.PersonPronouns.Object}: \"{grievance}\"");
            }
        }

        // Qualitative trust movement only — see PlayerRelationshipMovement's own doc comment for why
        // fear, obligation and grievance are not shown here. Shown regardless of whether the person
        // otherwise made the Attitudes list above, so a fresh movement is never silently absorbed.
        if (snapshot.RecentTrustMovements.Count > 0)
        {
            yield return new HSeparator();
            yield return Plain("RECENTLY");
            foreach (var movement in snapshot.RecentTrustMovements)
                yield return Faint(
                    $"{movement.At.ToString("d MMM", CultureInfo.InvariantCulture)}  " +
                    $"{PlayerNarration.Movement(movement.Warmed, p, movement.PersonName)}");
        }
    }

    /// <summary>
    /// The viewpoint character's own causal thread: what he last committed to, his own business's
    /// status if he owns one, and what he is still waiting to hear back on. Milestone 018 — every
    /// value here is a projection already computed onto <see cref="PlayerSnapshot"/>, never a second
    /// record this file keeps of its own.
    /// </summary>
    private IEnumerable<Control> BuildCausalThread(PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;

        yield return snapshot.LastAction is { } action
            ? Plain($"{action.At.ToString("d MMM", CultureInfo.InvariantCulture)}  {p.Subject_} chose to {action.Description}")
            : Plain($"{p.Subject_} {p.Verb("has", "have")} not committed to anything yet.");

        if (snapshot.MyBusiness is { } business)
            yield return Faint($"    {business.Name}: {(business.PayingTribute ? "currently paying" : "not currently paying")}");

        yield return new HSeparator();
        yield return Plain("AWAITING ANSWERS");

        if (snapshot.AwaitingAnswers.Count == 0)
        {
            yield return Faint("Nothing outstanding.");
            yield break;
        }

        foreach (var request in snapshot.AwaitingAnswers)
        {
            yield return Plain(
                $"{request.AskedAt.ToString("d MMM", CultureInfo.InvariantCulture)}  asked {request.AskedName} " +
                $"for {request.AskedPronouns.Possessive} own account of whether {request.Statement}");
            yield return Faint("    no answer yet");
        }
    }

    /// <summary>
    /// The controlled character's decision. Its pronouns are the *viewpoint's* until there is a
    /// pending decision to take them from — the two are the same character in this shell, and where
    /// they are not, the panel is describing the man being watched rather than the man deciding.
    /// </summary>
    private IEnumerable<Control> BuildDecision(PersistentSession session, PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;

        if (session.ControlledCharacterId is null)
        {
            yield return Plain(
                $"Everybody is deciding for themselves. Advance the clock and read what {p.Subject} " +
                $"{p.Verb("comes", "come")} to hear about it.");
            yield break;
        }

        if (session.Pending is not { } pending)
        {
            yield return Plain(
                $"{p.Subject_} {p.Verb("has", "have")} nothing in front of {p.Object} at the moment.");
            yield return Faint($"Advance the clock until something reaches {p.Object}.");
            yield break;
        }

        var actor = pending.ActorPronouns;

        yield return Plain($"{pending.ActorName}, {pending.ActorRole}");

        // Both are nullable, and a null is not a gap to fill with something plausible. It means the
        // simulation cannot say he knows why he is thinking about this — a delegated operation that
        // failed while he was elsewhere, and nobody has told him yet. The panel says nothing, and the
        // options are still his own.
        yield return Faint(pending.Occasion is { } occasion
            ? $"{pending.At.ToString("d MMM yyyy", CultureInfo.InvariantCulture)} — {occasion}"
            : pending.At.ToString("d MMM yyyy", CultureInfo.InvariantCulture));

        if (pending.Focus is { } focus) yield return Faint($"on {actor.Possessive} mind: {focus}");

        yield return new HSeparator();

        foreach (var option in pending.Options)
        {
            var button = new Button
            {
                Text = option.Description,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Alignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };

            string id = option.Id;
            button.Pressed += () =>
            {
                session.Choose(id);
                Refresh();
            };

            yield return button;
        }

        yield return Faint(
            $"These are the options that occurred to {actor.Object} and that {actor.Subject} could " +
            $"actually take. What {actor.Subject} never thought of, and what {actor.Subject} " +
            $"{actor.Verb("does", "do")} not know, are not listed.");
    }

    // ================================================================= self-test

    /// <summary>
    /// Drives the real interface headlessly: opens a session, plays it to the end taking the first
    /// option every time, and prints every string the interface built.
    ///
    /// It exists so that "the Godot project starts headlessly" and "a hidden fact cannot appear in
    /// the Godot UI" are both things a command can answer, rather than claims about a window nobody
    /// looked at. It builds the same panels through the same methods a person would see — there is
    /// no separate headless rendering path, because a check against one would prove nothing about
    /// the other.
    /// </summary>
    private void RunSelfTest()
    {
        try
        {
            SelfTest();
        }
        catch (Exception ex)
        {
            // A throw is a failure, not a crash to be read out of a log by eye. Milestone 009's
            // review found the same gap in the success path: a check whose result nothing can act on
            // is not a check.
            GD.PrintErr($"CE-SELFTEST FAILED — {ex}");
            GetTree().Quit(1);
        }
    }

    private void SelfTest()
    {
        GD.Print("CE-SELFTEST begin");

        StartSession(seed: 42, variant: "baseline", controlled: Roster.DefaultControlledId, viewpoint: Roster.DefaultControlledId);

        var session = _session!;
        DateTime end = session.StartedOn.AddDays(SelfTestDays);
        int choices = 0;
        int decisionScreens = 0;

        // Every screen the interface builds during the run, not merely the last one. A check run
        // against the final state alone would never see a pending-decision panel, which is the
        // panel most worth checking — it is the one that renders text derived from the deliberation.
        // Collected from the live node tree rather than from the snapshot behind it, because the
        // claim under test is about the interface.
        var transcript = new StringBuilder();
        Collect(this, transcript);

        // Bounded rather than "until done": a loop that could not terminate would hang a headless
        // verification run instead of failing it.
        //
        // EVERY STEP GOES THROUGH A BUTTON. Milestone 009's fourth correction: this loop used to call
        // the session directly and then call Refresh itself, so the one genuinely fiddly path in the
        // interface — a button's Pressed handler resolving a choice, and the rebuild then detaching
        // and freeing that very button — had never run. A headless check that bypasses the widgets
        // proves the session works, which was never the thing in doubt.
        for (int guard = 0; guard < 2000 && session.Date < end; guard++)
        {
            string label;

            if (session.Status == SessionStatus.AwaitingChoice)
            {
                var pending = session.Pending!;
                GD.Print($"CE-SELFTEST decision {pending.At:yyyy-MM-dd} {pending.ActorId} " +
                         $"options={pending.Options.Count} taking=\"{pending.Options[0].Description}\"");
                label = pending.Options[0].Description;
                choices++;
            }
            else
            {
                label = "Advance a week";
            }

            if (!Press(label))
                throw new InvalidOperationException($"no button reading \"{label}\" is on screen");

            if (session.Status == SessionStatus.AwaitingChoice) decisionScreens++;
            Collect(this, transcript);
        }

        GD.Print($"CE-SELFTEST date={session.Date:yyyy-MM-dd} " +
                 $"choices={choices.ToString(CultureInfo.InvariantCulture)} " +
                 $"decision-screens={decisionScreens.ToString(CultureInfo.InvariantCulture)}");

        GD.Print("== CE-UI-TEXT-BEGIN ==");
        GD.Print(transcript.ToString());
        GD.Print("== CE-UI-TEXT-END ==");

        // The exit code is the result. Printing a failure and exiting 0 makes the check unusable
        // from a script, which is what a verification step is for — found by milestone 009's review.
        bool proved = choices > 0 && decisionScreens > 0 && session.Date >= end;
        if (proved)
        {
            GD.Print("CE-SELFTEST ok");
            GetTree().Quit();
            return;
        }

        GD.PrintErr(
            "CE-SELFTEST FAILED — the run did not reach the end of the scenario having rendered and " +
            "answered a decision, so it proves nothing");
        GetTree().Quit(1);
    }

    // ================================================================= golden path (milestone 014)

    /// <summary>
    /// Drives Vincent's own seed-42 <c>SecureTribute</c> operation against Bellini's grocery through
    /// real button presses — the interactive playthrough itself, not a claim about it. Presses the
    /// seven pinned option texts in order (independently derived from the same accepted trace as
    /// <c>PlayerOwnedOperationTests</c> in the test project, not shared with it, so the two checks
    /// cannot both be wrong about the same assumption), and reads the rendered cash label off the
    /// live screen — never <see cref="SimulationSession"/>'s internal state — to confirm the accepted
    /// 1 April consequence: 6,000 rising to 6,840. No further time advance is needed after the
    /// seventh choice: the collection that pays Vincent happens in the same event sweep that produces
    /// his seventh decision (reporting the outcome to Salvatore), so the rendered cash is already
    /// current by the time that screen is on-screen.
    ///
    /// <b>Asserts the opening screen too, before any button is pressed.</b> A check that only reads
    /// the final screen cannot tell a real 6,000-to-6,840 change from a toolbar that always rendered
    /// 6,840 regardless of what happened — confirmed by mutation: temporarily hardcoding the toolbar
    /// to a fixed "6,840" made the opening assertion fail, before the mutation was reverted.
    /// </summary>
    /// <summary>
    /// The exact seven option descriptions Vincent's accepted seed-42 <c>SecureTribute</c> operation
    /// offers, in order — shared by <see cref="GoldenPathSelfTest"/> and milestone 015's two-process
    /// restart proof, which is this same sequence split after the third choice rather than a second,
    /// independently-typed copy of it.
    /// </summary>
    private static readonly string[] SevenChoiceSequence =
    {
        "talk Bellini's grocery round",
        "carry on getting Bellini's grocery to pay",
        "have Tommy Nardo take it on",
        "change tack with Bellini's grocery — threats instead",
        "change tack with Bellini's grocery — force instead — against the standing rule \"no-violence-harbour\"",
        "carry on getting Bellini's grocery to pay",
        "report to Salvatore Greco, leaving out his own part",
    };

    /// <summary>
    /// Presses each of <paramref name="choices"/> in order, using "Next event" to reach each pause —
    /// never "Advance a week", which carries an outstanding fast-forward horizon across a
    /// <c>Choose</c> call and can silently sail past the decision this is looking for. Throws rather
    /// than returning a partial result if the run gives up before every choice is made, so a caller
    /// never has to remember to check how far it got.
    /// </summary>
    private void PressChoicesInOrder(PersistentSession session, IReadOnlyList<string> choices, string logTag)
    {
        int choiceIndex = 0;
        for (int guard = 0; guard < 2000 && choiceIndex < choices.Count; guard++)
        {
            if (session.Status == SessionStatus.AwaitingChoice)
            {
                string expected = choices[choiceIndex];
                GD.Print($"{logTag} decision {choiceIndex + 1} on {session.Date:yyyy-MM-dd} — pressing \"{expected}\"");
                if (!Press(expected))
                    throw new InvalidOperationException(
                        $"the decision on {session.Date:yyyy-MM-dd} does not offer \"{expected}\"");
                choiceIndex++;
            }
            else
            {
                if (!Press("Next event"))
                    throw new InvalidOperationException("no \"Next event\" control is available");
            }
        }

        if (choiceIndex < choices.Count)
            throw new InvalidOperationException(
                $"only reached choice {choiceIndex} of {choices.Count} before giving up");
    }

    private void RunGoldenPathSelfTest()
    {
        try
        {
            GoldenPathSelfTest();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"CE-GOLDENPATH FAILED — {ex}");
            GetTree().Quit(1);
        }
    }

    private void GoldenPathSelfTest()
    {
        GD.Print("CE-GOLDENPATH begin");

        StartSession(seed: 42, variant: "baseline", controlled: Roster.DefaultControlledId, viewpoint: Roster.DefaultControlledId);
        var session = _session!;

        // Proves a displayed *change*, not merely that the final screen happens to read 6,840 — a
        // check that only asserted the end value would pass just as well against a toolbar that
        // always rendered 6,840 regardless of the snapshot behind it.
        var startScreen = new StringBuilder();
        Collect(this, startScreen);
        if (!startScreen.ToString().Contains("cash on hand 6,000", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "the opening screen does not read \"cash on hand 6,000\" — the golden path's own " +
                "starting point is wrong, so the later 6,840 would prove nothing");

        PressChoicesInOrder(session, SevenChoiceSequence, "CE-GOLDENPATH");

        // No eighth pause should follow the seventh choice unaddressed — if one does, something
        // (an unaddressed decision, a fast-forward that outran the choice just made) has silently
        // moved past the point this check claims to have reached.
        if (session.Status == SessionStatus.AwaitingChoice)
            throw new InvalidOperationException(
                $"an unaddressed decision followed the seventh choice, on {session.Date:yyyy-MM-dd} — " +
                "the run has moved past the point it should have stopped at");

        // The rendered screen, exactly as a person watching would read it — collected the same way
        // the general self-test proves its own transcript, never by reading World or Capabilities.
        var screenText = new StringBuilder();
        Collect(this, screenText);
        string screen = screenText.ToString();

        GD.Print("== CE-GOLDENPATH-SCREEN-BEGIN ==");
        GD.Print(screen);
        GD.Print("== CE-GOLDENPATH-SCREEN-END ==");

        bool proved = screen.Contains("cash on hand 6,840", StringComparison.Ordinal);

        if (proved)
        {
            GD.Print("CE-GOLDENPATH ok");
            GetTree().Quit();
            return;
        }

        GD.PrintErr(
            "CE-GOLDENPATH FAILED — did not reach the accepted 1 April consequence with cash on hand " +
            "reading 6,840 on screen, so it proves nothing");
        GetTree().Quit(1);
    }

    // ================================================================= direct action (milestone 017)

    /// <summary>
    /// Milestone 017: the same seed-42 Vincent <c>SecureTribute</c> operation
    /// <see cref="GoldenPathSelfTest"/> plays, but never delegated — Vincent presses "carry on" at the
    /// exact pause that also offers "have Tommy Nardo take it on" (the fork this milestone is about),
    /// then continues personally through escalation. Independently pinned from a live run of the
    /// interactive path, the same way <see cref="SevenChoiceSequence"/> itself was derived, not shared
    /// with it or with the test project's copy of the same fork.
    ///
    /// Diverges from the accepted delegated trace naturally, through real button presses alone: Vincent
    /// himself — not Tommy — puts hands on Bellini's grocery, conceals it himself, and answers for it
    /// himself. Proceeds still land on Vincent regardless (cash still rises to 6,840) — ownership
    /// determines proceeds, execution is what diverged, exactly the milestone's own distinction.
    /// </summary>
    private static readonly string[] DirectActionChoiceSequence =
    {
        "talk Bellini's grocery round",
        "carry on getting Bellini's grocery to pay",
        "change tack with Bellini's grocery — threats instead",
        "change tack with Bellini's grocery — force instead — against the standing rule \"no-violence-harbour\"",
        "clean up after it before anyone else does",
        "carry on covering it up",
        "ask Salvatore Greco for room to move",
        "give Salvatore Greco his account of whether Bellini's grocery is holding back what it owes",
    };

    private static bool DirectActionRequested()
        => OS.GetCmdlineArgs().Contains(DirectActionFlag) || OS.GetCmdlineUserArgs().Contains(DirectActionFlag);

    /// <summary>Presses "Next event" until a decision is waiting — the same discipline
    /// <see cref="PressChoicesInOrder"/> uses internally, exposed here so a caller can inspect a
    /// pause's real offered options before deciding what to press next.</summary>
    private void AdvanceToPause(PersistentSession session)
    {
        for (int guard = 0; guard < 2000 && session.Status != SessionStatus.AwaitingChoice; guard++)
        {
            if (!Press("Next event")) throw new InvalidOperationException("no \"Next event\" control is available");
        }
        if (session.Status != SessionStatus.AwaitingChoice)
            throw new InvalidOperationException("gave up waiting for a decision to appear");
    }

    private void RunDirectActionSelfTest()
    {
        try
        {
            DirectActionSelfTest();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"CE-DIRECTACTION FAILED — {ex}");
            GetTree().Quit(1);
        }
    }

    private void DirectActionSelfTest()
    {
        GD.Print("CE-DIRECTACTION begin");

        StartSession(seed: 42, variant: "baseline", controlled: Roster.DefaultControlledId, viewpoint: Roster.DefaultControlledId);
        var session = _session!;

        var startScreen = new StringBuilder();
        Collect(this, startScreen);
        if (!startScreen.ToString().Contains("cash on hand 6,000", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "the opening screen does not read \"cash on hand 6,000\" — this proof's own starting " +
                "point is wrong, so a later change would prove nothing");

        // Reach the first pause (the start decision) the same way PressChoicesInOrder does — "Next
        // event" until something is waiting — then press the start, then reach the pause that follows
        // it, which is the fork this milestone is about.
        AdvanceToPause(session);
        if (!Press(DirectActionChoiceSequence[0]))
            throw new InvalidOperationException($"could not press \"{DirectActionChoiceSequence[0]}\"");
        AdvanceToPause(session);

        // Confirm the executable feature claim itself before doing anything else: this pause offers
        // direct continuation and delegation together, as real, simultaneously pressable buttons on
        // the live screen — not two decisions in sequence.
        //
        // Corrected per Codex's review of `9de2c75`: the original version read
        // session.Pending.Options — the session's own internal state, not the rendered interface — so
        // a UI defect that silently failed to render one of the two option buttons (while the session
        // still legitimately offered both underneath) would never have been caught here. This now
        // walks the actual live scene tree via FindButton, the same helper Press itself uses to find
        // and click a button by its rendered text, so the check inspects exactly what a person looking
        // at the screen would see.
        bool carryOnRendered = FindButton(this, "carry on getting Bellini's grocery to pay") is not null;
        bool delegateRendered = FindButton(this, "have Tommy Nardo take it on") is not null;
        if (!carryOnRendered || !delegateRendered)
            throw new InvalidOperationException(
                "the fork pause does not render both continuation and delegation as real buttons — " +
                $"carry-on button present: {carryOnRendered}, delegate button present: {delegateRendered}");

        PressChoicesInOrder(session, DirectActionChoiceSequence.Skip(1).ToList(), "CE-DIRECTACTION");

        var screenText = new StringBuilder();
        Collect(this, screenText);
        string screen = screenText.ToString();

        GD.Print("== CE-DIRECTACTION-SCREEN-BEGIN ==");
        GD.Print(screen);
        GD.Print("== CE-DIRECTACTION-SCREEN-END ==");

        // Ownership determines proceeds regardless of who executed — unchanged from the accepted
        // delegated trace's own consequence, checked as a required negative: this is not where the
        // two branches diverge.
        bool proceeds = screen.Contains("cash on hand 6,840", StringComparison.Ordinal);

        // Execution responsibility is exactly what diverged: Vincent himself put hands on the target
        // and knows it as his own act ("he had a hand in it himself"), never Tommy — the opposite of
        // the accepted delegated trace, where Tommy is the one named and Vincent only came across it.
        bool executedPersonally = screen.Contains("Vincent Russo put hands on Bellini's grocery", StringComparison.Ordinal);
        bool noTommyExecution = !screen.Contains("Tommy Nardo put hands on Bellini's grocery", StringComparison.Ordinal);

        if (proceeds && executedPersonally && noTommyExecution)
        {
            GD.Print("CE-DIRECTACTION ok");
            GetTree().Quit();
            return;
        }

        GD.PrintErr(
            "CE-DIRECTACTION FAILED — proceeds=" + proceeds + " executedPersonally=" + executedPersonally +
            " noTommyExecution=" + noTommyExecution + " — the direct branch did not reach the expected " +
            "personally-executed consequence, so it proves nothing");
        GetTree().Quit(1);
    }

    // ================================================================= corroboration (milestone 018)

    private void RunCorroborationSelfTest()
    {
        try
        {
            CorroborationSelfTest();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"CE-CORROBORATION FAILED — {ex}");
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// Salvatore's own seed-42 <c>cautious-vincent</c> ask, played through real buttons.
    ///
    /// Reached via "Next event" alone, never a fast-forward control — pressing "Advance a day/week"
    /// would carry an outstanding horizon across the ask itself and run the session straight past the
    /// unresolved moment this proof exists to catch, exactly the reason <see cref="PressChoicesInOrder"/>
    /// and <see cref="AdvanceToPause(PersistentSession)"/> already use "Next event" only.
    ///
    /// Vincent's answer is not staged: observed directly, before this method was written, to arrive
    /// naturally within a few days through the ordinary report channel, and to contradict what
    /// Salvatore already held from "the books" — so this proof also exercises a live disagreement
    /// resolving with attribution, not merely a belief appearing.
    /// </summary>
    private void CorroborationSelfTest()
    {
        GD.Print("CE-CORROBORATION begin");

        StartSession(seed: 42, variant: "cautious-vincent", controlled: "salvatore", viewpoint: "salvatore");
        var session = _session!;

        const string ask =
            "ask Vincent Russo for his own account of whether Bellini's grocery is holding back what it owes";

        AdvanceToPause(session);
        if (!Press(ask))
            throw new InvalidOperationException($"the natural run never offers \"{ask}\" to Salvatore");

        var afterAsk = new StringBuilder();
        Collect(this, afterAsk);
        string afterAskText = afterAsk.ToString();

        GD.Print("== CE-CORROBORATION-AFTER-ASK-BEGIN ==");
        GD.Print(afterAskText);
        GD.Print("== CE-CORROBORATION-AFTER-ASK-END ==");

        bool acknowledged = afterAskText.Contains("chose to ask Vincent Russo", StringComparison.Ordinal);
        bool unresolved =
            afterAskText.Contains(
                "asked Vincent Russo for his own account of whether Bellini's grocery is holding back what it owes",
                StringComparison.Ordinal)
            && afterAskText.Contains("no answer yet", StringComparison.Ordinal);

        if (!acknowledged || !unresolved)
        {
            GD.PrintErr(
                "CE-CORROBORATION FAILED — acknowledged=" + acknowledged + " unresolved=" + unresolved +
                " — the request was not shown as an immediate, unresolved acknowledgement");
            GetTree().Quit(1);
            return;
        }

        // Advance one event at a time, exactly as a person clicking "Next event" would, until either
        // the request resolves or nothing further can be pressed without answering a new decision —
        // the natural run's own pace decides which, never a fixed number of days.
        string finalText = afterAskText;
        for (int guard = 0; guard < 100; guard++)
        {
            var current = new StringBuilder();
            Collect(this, current);
            finalText = current.ToString();
            if (!finalText.Contains("no answer yet", StringComparison.Ordinal)) break;
            if (!Press("Next event")) break;
        }

        GD.Print("== CE-CORROBORATION-FINAL-BEGIN ==");
        GD.Print(finalText);
        GD.Print("== CE-CORROBORATION-FINAL-END ==");

        bool resolved = !finalText.Contains("no answer yet", StringComparison.Ordinal);
        bool attributedToVincent =
            finalText.Contains("Accounts differ on whether Bellini's grocery is holding back", StringComparison.Ordinal)
            && finalText.Contains("Vincent Russo", StringComparison.Ordinal);

        if (resolved && attributedToVincent)
        {
            GD.Print("CE-CORROBORATION ok");
            GetTree().Quit();
            return;
        }

        GD.PrintErr(
            "CE-CORROBORATION FAILED — resolved=" + resolved + " attributedToVincent=" + attributedToVincent +
            " — the natural run did not resolve and attribute Vincent's answer on the live screen");
        GetTree().Quit(1);
    }

    // ================================================================= tribute demand (milestone 018)

    private void RunTributeSelfTest()
    {
        try
        {
            TributeSelfTest();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"CE-TRIBUTE FAILED — {ex}");
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// Marco's own seed-42 <c>baseline</c> first tribute demand — Vincent's, before any escalation or
    /// delegation, and before Marco holds any violence claim against anybody — played through real
    /// buttons. Proves the decision panel names the demander before Marco chooses, and that refusing
    /// renders an immediate acknowledgement plus the business's own known unpaid status, never a
    /// number and never an invented future consequence.
    /// </summary>
    private void TributeSelfTest()
    {
        GD.Print("CE-TRIBUTE begin");

        StartSession(seed: 42, variant: "baseline", controlled: "marco", viewpoint: "marco");
        var session = _session!;

        AdvanceToPause(session);

        var beforeChoice = new StringBuilder();
        Collect(this, beforeChoice);
        string beforeChoiceText = beforeChoice.ToString();

        GD.Print("== CE-TRIBUTE-BEFORE-BEGIN ==");
        GD.Print(beforeChoiceText);
        GD.Print("== CE-TRIBUTE-BEFORE-END ==");

        bool namesTheDemander =
            beforeChoiceText.Contains("Vincent Russo is demanding tribute from him", StringComparison.Ordinal);
        if (!namesTheDemander)
        {
            GD.PrintErr("CE-TRIBUTE FAILED — the panel does not name the demander before Marco chooses");
            GetTree().Quit(1);
            return;
        }

        if (!Press("refuse Vincent Russo"))
            throw new InvalidOperationException("could not press \"refuse Vincent Russo\"");

        var afterChoice = new StringBuilder();
        Collect(this, afterChoice);
        string afterChoiceText = afterChoice.ToString();

        GD.Print("== CE-TRIBUTE-AFTER-BEGIN ==");
        GD.Print(afterChoiceText);
        GD.Print("== CE-TRIBUTE-AFTER-END ==");

        bool acknowledged = afterChoiceText.Contains("chose to refuse Vincent Russo", StringComparison.Ordinal);
        bool businessStatusShown =
            afterChoiceText.Contains("Bellini's grocery: not currently paying", StringComparison.Ordinal);
        bool noInventedRetaliation = !afterChoiceText.Contains("harder", StringComparison.Ordinal);

        if (acknowledged && businessStatusShown && noInventedRetaliation)
        {
            GD.Print("CE-TRIBUTE ok");
            GetTree().Quit();
            return;
        }

        GD.PrintErr(
            "CE-TRIBUTE FAILED — acknowledged=" + acknowledged + " businessStatusShown=" + businessStatusShown +
            " noInventedRetaliation=" + noInventedRetaliation);
        GetTree().Quit(1);
    }

    // ================================================================= restart proof (milestone 015)

    /// <summary>
    /// Process A of the two-process restart proof: plays <see cref="SevenChoiceSequence"/>'s first
    /// three choices (start, carry on, delegate to Tommy) through real buttons, presses the real
    /// "Save" button, and exits. Run as a genuinely separate OS process from
    /// <see cref="RunRestartLoadSelfTest"/> — two independent headless Godot invocations against the
    /// same real save slot, not two calls within one process. See the milestone archive for the exact
    /// commands and recorded output.
    /// </summary>
    private void RunRestartSaveSelfTest()
    {
        try
        {
            RestartSaveSelfTest();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"CE-RESTART-SAVE FAILED — {ex}");
            GetTree().Quit(1);
        }
    }

    private void RestartSaveSelfTest()
    {
        GD.Print("CE-RESTART-SAVE begin");

        // A fresh slot for this proof, not whatever a prior manual run left behind — the point is to
        // prove process B can only have reached its result through this process's own save. This is
        // always the isolated self-test slot (_activeSavePath, resolved in _Ready before this method
        // runs), never the production one.
        if (SaveStore.Exists(_activeSavePath)) System.IO.File.Delete(_activeSavePath);

        StartSession(seed: 42, variant: "baseline", controlled: Roster.DefaultControlledId, viewpoint: Roster.DefaultControlledId);
        var session = _session!;

        var startScreen = new StringBuilder();
        Collect(this, startScreen);
        if (!startScreen.ToString().Contains("cash on hand 6,000", StringComparison.Ordinal))
            throw new InvalidOperationException("the opening screen does not read \"cash on hand 6,000\"");

        PressChoicesInOrder(session, SevenChoiceSequence.Take(3).ToArray(), "CE-RESTART-SAVE");

        if (!Press("Save"))
            throw new InvalidOperationException("no \"Save\" control is available");

        if (_statusMessage != "saved")
            throw new InvalidOperationException($"pressing Save did not report success — status: {_statusMessage}");

        if (!SaveStore.Exists(_activeSavePath))
            throw new InvalidOperationException($"pressing Save did not create a save at '{_activeSavePath}'");

        GD.Print($"CE-RESTART-SAVE saved to {_activeSavePath} on {session.Date:yyyy-MM-dd}");
        GD.Print("CE-RESTART-SAVE ok");
        GetTree().Quit();
    }

    /// <summary>
    /// Process B of the two-process restart proof: loads the save <see cref="RunRestartSaveSelfTest"/>
    /// wrote — in a prior, separate OS process — through the real "Load saved game" button, then plays
    /// <see cref="SevenChoiceSequence"/>'s remaining four choices through real buttons, reaching the
    /// same accepted 1 April consequence <see cref="GoldenPathSelfTest"/> reaches in one continuous
    /// process: 6,840 on the rendered screen.
    /// </summary>
    private void RunRestartLoadSelfTest()
    {
        try
        {
            RestartLoadSelfTest();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"CE-RESTART-LOAD FAILED — {ex}");
            GetTree().Quit(1);
        }
    }

    private void RestartLoadSelfTest()
    {
        GD.Print("CE-RESTART-LOAD begin");

        // The self-test fixture is cleaned up here regardless of outcome — success, a thrown
        // exception, or the explicit failure path below — so a failed run never leaves a stale file
        // that could make the next run's "no pre-existing save" assumptions silently wrong. This
        // process is the last of the two by construction (it consumes what process A wrote), so it
        // is the natural place for cleanup to live; process A's own file is always the isolated
        // self-test slot, never the production one, per the type header.
        try
        {
            if (!SaveStore.Exists(_activeSavePath))
                throw new InvalidOperationException(
                    $"no save at '{_activeSavePath}' — run with {RestartSaveFlag} first, as a separate process");

            BuildStartScreen();

            if (!Press("Load saved game"))
                throw new InvalidOperationException("no \"Load saved game\" control is available");

            if (_session is not { } session)
                throw new InvalidOperationException($"loading did not produce a session — status: {_statusMessage}");

            GD.Print($"CE-RESTART-LOAD loaded at {session.Date:yyyy-MM-dd}, status={session.Status}");

            PressChoicesInOrder(session, SevenChoiceSequence.Skip(3).ToArray(), "CE-RESTART-LOAD");

            // Same check GoldenPathSelfTest makes: nothing unaddressed should follow the seventh choice.
            if (session.Status == SessionStatus.AwaitingChoice)
                throw new InvalidOperationException(
                    $"an unaddressed decision followed the seventh choice, on {session.Date:yyyy-MM-dd}");

            var screenText = new StringBuilder();
            Collect(this, screenText);
            string screen = screenText.ToString();

            GD.Print("== CE-RESTART-LOAD-SCREEN-BEGIN ==");
            GD.Print(screen);
            GD.Print("== CE-RESTART-LOAD-SCREEN-END ==");

            bool proved = screen.Contains("cash on hand 6,840", StringComparison.Ordinal);
            if (proved)
            {
                GD.Print("CE-RESTART-LOAD ok");
                GetTree().Quit();
                return;
            }

            GD.PrintErr(
                "CE-RESTART-LOAD FAILED — did not reach the accepted 1 April consequence with cash on hand " +
                "reading 6,840 on screen, so it proves nothing");
            GetTree().Quit(1);
        }
        finally
        {
            CleanupSelfTestRestartSlot();
        }
    }

    /// <summary>
    /// Deletes the restart self-tests' own dedicated save (and its <c>.tmp</c> sibling, in case a
    /// prior run's write was itself interrupted) — never the production slot, which this never
    /// references. Best-effort: a cleanup failure is not the claim this self-test exists to prove, so
    /// it is swallowed rather than turned into a false failure of the restart proof itself.
    /// </summary>
    private static void CleanupSelfTestRestartSlot()
    {
        try
        {
            if (SaveStore.Exists(SelfTestRestartSavePath)) System.IO.File.Delete(SelfTestRestartSavePath);
            string tmp = SelfTestRestartSavePath + ".tmp";
            if (System.IO.File.Exists(tmp)) System.IO.File.Delete(tmp);
        }
        catch
        {
            // Best effort, per the doc comment above.
        }
    }

    private static bool GoldenPathRequested()
        => OS.GetCmdlineArgs().Contains(GoldenPathFlag) || OS.GetCmdlineUserArgs().Contains(GoldenPathFlag);

    private static bool FlagRequested(string flag)
        => OS.GetCmdlineArgs().Contains(flag) || OS.GetCmdlineUserArgs().Contains(flag);

    /// <summary>
    /// Presses the button reading this text, as a person would, and lets its own handler do the rest
    /// — including the rebuild that frees the button the signal came from.
    /// </summary>
    private bool Press(string text)
    {
        var button = FindButton(this, text);
        if (button is null) return false;

        button.EmitSignal(BaseButton.SignalName.Pressed);
        return true;
    }

    private static Button? FindButton(Node node, string text)
    {
        if (node is Button b && !b.Disabled && b.Text == text) return b;

        foreach (var child in node.GetChildren())
            if (FindButton(child, text) is { } found)
                return found;

        return null;
    }

    private static bool SelfTestRequested()
        => OS.GetCmdlineArgs().Contains(SelfTestFlag) || OS.GetCmdlineUserArgs().Contains(SelfTestFlag);

    private static void Collect(Node node, StringBuilder into)
    {
        switch (node)
        {
            case Label label:
                into.AppendLine(label.Text);
                break;
            case Button button:
                into.AppendLine(button.Text);
                break;
            case LineEdit edit:
                into.AppendLine(edit.Text);
                break;
        }

        foreach (var child in node.GetChildren())
            Collect(child, into);
    }

    // ================================================================= plain widgets

    private static Label Heading(string text) => new()
    {
        Text = text,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    private static Label Plain(string text) => new()
    {
        Text = text,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
    };

    private static Label Faint(string text)
    {
        var label = Plain(text);
        label.Modulate = new Color(1, 1, 1, 0.72f);
        return label;
    }

    private static Control Column(string title, IEnumerable<Control> rows)
    {
        var box = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };

        box.AddChild(Heading(title));

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        box.AddChild(scroll);

        var inner = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(inner);

        foreach (var row in rows) inner.AddChild(row);
        return box;
    }

    /// <summary>
    /// Empties a container.
    ///
    /// Detached first and freed afterwards, deliberately. <see cref="Node.QueueFree"/> alone would
    /// leave the old nodes in the tree until the end of the frame, so the self-test's walk would
    /// report a screen that no longer exists; <see cref="GodotObject.Free"/> alone would be an
    /// immediate deletion that can happen while one of those very nodes is emitting the signal that
    /// caused the rebuild. Removing then queueing does neither.
    /// </summary>
    private static void Clear(Node container)
    {
        foreach (var child in container.GetChildren().ToArray())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }
}
