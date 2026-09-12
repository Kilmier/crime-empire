using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CrimeEmpire.Persistence;
using CrimeEmpire.Persistence.Session;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using Godot;

namespace CrimeEmpire.GodotShell;

/// <summary>
/// The whole interface.
///
/// <b>What it is allowed to know.</b> One <see cref="SimulationSession"/>. Its in-fiction state comes
/// through a <see cref="PlayerSnapshot"/> and a <see cref="PendingDecision"/>, which is the controlled
/// character's own options; milestone 027's objective and one-bit result are explicitly separate,
/// out-of-fiction session metadata. Ruling 1 (milestone 014) states the snapshot's contract
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
/// <b>It is deliberately plain.</b> No theme, no art, no animation, no map. Panels of labels and
/// buttons that move the clock, plus milestone 015's one fixed save slot. Milestone 009's scope
/// forbids polish beyond a clear functional layout, and there is a reason beyond time: a shell that
/// looked finished would invite judgements about the game that only the simulation can earn.
///
/// <b>The layout (milestone 025).</b> A strip along the top — the date, who you are, your cash, the
/// clock controls — and four weighted panels beneath it: what you know, what you are doing, what you
/// think of people, and the decision in front of you. The two panels the screen used to carry
/// besides these, "LATELY" and "RECENTLY", were copies: the same beliefs re-sorted, and the same trust
/// movements the roster already explains. Recency is now the order of the first panel and a rule
/// across it, and nothing is drawn twice — <see cref="AssertNoClaimDrawnTwice"/> is the guard, run
/// on every screen every self-test builds.
///
/// <b>Save and load (milestone 015).</b> One fixed slot, <see cref="ProductionSavePath"/> — no file
/// picker, no slot management, no autosave. <see cref="PersistentSession"/> is the only new thing
/// this file knows about beyond milestone 014's boundary: it passes through the same player
/// projection plus milestone 027's bounded scenario metadata, and never <c>World</c>.
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
    /// Command-line switch for milestone 027's complete rendered proof: opening objective, both
    /// natural terminal outcomes, disabled terminal controls, and load of a resolved session.
    /// </summary>
    private const string EndingFlag = "--selftest-ending";

    /// <summary>
    /// Command-line switch for milestone 014's golden path: Vincent's existing seed-42
    /// <c>SecureTribute</c> operation against Bellini's grocery, played through real button presses
    /// rather than the general self-test's "always take the first option" policy, reaching the
    /// accepted 5 April consequence and reading the rendered cash off the live screen.
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
    /// <c>cautious-vincent</c> ask — "ask Vincent Russo what he knows about whether Bellini's
    /// grocery is not paying its tribute" — played through real button presses. Proves the request
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
    /// Command-line switch for the correction to `53694a2`'s `TakenFor` coverage: proves the
    /// "what he takes a man for" line — milestones 023/024 — actually reaches the live rendered
    /// roster panel, which the equivalent xunit-level check cannot: the belief-list panel
    /// (`WHAT ... KNOWS`) already contains the same words ("hard man") independently of whether the
    /// attitude panel (`WHAT ... THINKS OF PEOPLE`) renders anything at all, so a check against the
    /// whole screen would pass even with the attitude line removed. This isolates the attitude
    /// panel's own section of the screen before asserting.
    ///
    /// <c>capable-angelo</c> specifically: the one variant whose own <c>Cast.Build</c> seeds
    /// Vincent's belief that Angelo clears both capability bars, so this reaches the line naturally
    /// rather than by staging one.
    /// </summary>
    private const string CapabilityFlag = "--selftest-capability";

    /// <summary>
    /// Command-line switch for the correction to `f993386`'s operation panel: proves the delegated
    /// operation line — milestone 024 — actually reaches the live "WHAT ... DOING" panel, for both
    /// the owner's silent-progress view and the executor's own-progress view, against the real
    /// screen rather than the snapshot behind it. `Bellini's grocery` already appears in the
    /// `WHAT ... KNOWS` panel independently of whether this one renders anything at all, so a check
    /// against the whole screen would pass even with the operation block removed — this isolates the
    /// `DOING` panel's own section, between its header and `WHAT JUST HAPPENED`, before asserting.
    ///
    /// The natural day-20 baseline: nobody controlled, run autonomously to the same point
    /// `OperationReadsTests.The_panel_is_populated_during_a_natural_run` reads, so this is the
    /// identical operation seen from the live interface rather than a staged one.
    /// </summary>
    private const string OperationFlag = "--selftest-operation";

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
    /// <see cref="RestartSaveFlag"/> process — and plays the golden path's remaining three choices
    /// through real buttons, reaching the accepted 5 April consequence.
    /// </summary>
    private const string RestartLoadFlag = "--selftest-restart-load";

    /// <summary>How far the self-test runs the scenario, matching the runner's default span.</summary>
    private const int SelfTestDays = 90;

    /// <summary>
    /// Node metadata key stamped on every label that presents a claim as an entry of its own — a
    /// belief headline, or the head of a standalone disagreement. <see cref="AssertNoClaimDrawnTwice"/>
    /// reads it back off the live tree. Stamped inside the one helper that draws such an entry, so a
    /// second rendering of the same claim through the same helper is caught by construction; a
    /// hand-rolled duplicate that bypassed the helper would escape, and that is the stated limit.
    /// </summary>
    private const string ClaimMeta = "claim";

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
    private OptionButton _playAsField = null!;
    private CheckBox _watchOnlyField = null!;
    private CheckBox _developerViewpointToggle = null!;
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
        _root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(_root);

        _cast = Roster.Characters("baseline");
        _variants = Roster.Variants();

        _activeSavePath = FlagRequested(RestartSaveFlag) || FlagRequested(RestartLoadFlag) || FlagRequested(EndingFlag)
            ? SelfTestRestartSavePath
            : ProductionSavePath;

        if (SelfTestRequested())
        {
            RunSelfTest();
            return;
        }

        if (FlagRequested(EndingFlag))
        {
            RunEndingSelfTest();
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

        if (FlagRequested(CapabilityFlag))
        {
            RunCapabilitySelfTest();
            return;
        }

        if (FlagRequested(OperationFlag))
        {
            RunOperationSelfTest();
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
    /// Seed, scenario variant, and who you play.
    ///
    /// "Play as" is one field (milestone 025): the person you control is the person whose eyes you
    /// see through. "Watch only" keeps the old "nobody" mode — everybody decides for themselves and
    /// you read what your chosen character comes to hear. A viewpoint that differs from the
    /// controlled character is a debugging capability — the no-leak proofs depend on it — so it stays
    /// reachable, behind a developer toggle at the foot of the screen rather than on the front door.
    ///
    /// The cast and variant lists come from <see cref="Roster"/> rather than from a world, so
    /// populating a dropdown never touches simulation state. Listing the cast here is a fact about
    /// the game being started and not about anybody's knowledge; nothing on the main screen
    /// enumerates people the viewpoint character has not heard of.
    /// </summary>
    private void BuildStartScreen()
    {
        Clear(_root);

        _root.AddChild(Title("CRIMINAL EMPIRE"));
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

        grid.AddChild(Plain("Play as"));
        _playAsField = new OptionButton();
        foreach (var c in _cast) _playAsField.AddItem($"{c.Name} — {c.RoleTitle}");
        _playAsField.Selected = IndexOfCharacter(Roster.DefaultControlledId);
        grid.AddChild(_playAsField);

        grid.AddChild(Plain(""));
        _watchOnlyField = new CheckBox { Text = "Watch only — everybody decides for themselves" };
        grid.AddChild(_watchOnlyField);

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
            "Playing somebody stops the clock whenever they have a decision to make, and offers what " +
            "actually occurred to them. Everyone else goes on deciding for themselves either way."));

        if (_statusMessage is { } status) _root.AddChild(Faint($"· {status}"));

        // Developer: see through somebody other than the person you play. Hidden until asked for.
        var spacer = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        _root.AddChild(spacer);

        var developer = new HBoxContainer();
        developer.AddThemeConstantOverride("separation", 12);
        _root.AddChild(developer);

        _developerViewpointToggle = new CheckBox { Text = "developer: see through somebody else" };
        _developerViewpointToggle.Modulate = new Color(1, 1, 1, 0.6f);
        developer.AddChild(_developerViewpointToggle);

        _viewpointField = new OptionButton { Visible = false };
        foreach (var c in _cast) _viewpointField.AddItem($"{c.Name} — {c.RoleTitle}");
        _viewpointField.Selected = IndexOfCharacter(Roster.DefaultControlledId);
        developer.AddChild(_viewpointField);

        _developerViewpointToggle.Toggled += on => _viewpointField.Visible = on;
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

        string playAs = _cast[Math.Max(0, _playAsField.Selected)].Id;
        string? controlled = _watchOnlyField.ButtonPressed ? null : playAs;

        // The viewpoint is the person you play unless the developer toggle says otherwise.
        string viewpoint = _developerViewpointToggle.ButtonPressed
            ? _cast[Math.Max(0, _viewpointField.Selected)].Id
            : playAs;

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
        var p = snapshot.ViewpointPronouns;
        string who = p.Subject.ToUpperInvariant();

        _root.AddChild(BuildToolbar(session, snapshot));
        _root.AddChild(BuildObjective(session));

        var columns = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 10);
        _root.AddChild(columns);

        // Weighted, not equal: the knowledge panel and the decision panel carry the longest lines.
        columns.AddChild(Panel($"WHAT {who} {p.Verb("KNOWS", "KNOW")}", BuildKnowledge(snapshot), 1.25f));
        columns.AddChild(Panel($"WHAT {who} {p.Verb("IS", "ARE")} DOING", BuildDoing(snapshot), 1.0f));
        columns.AddChild(Panel($"WHAT {who} {p.Verb("THINKS", "THINK")} OF PEOPLE", BuildAttitudes(snapshot), 1.0f));
        columns.AddChild(Panel(
            session.Status == SessionStatus.Resolved
                ? "SESSION ENDED"
                : session.ControlledCharacterId is null ? "WATCHING ONLY" : "A DECISION",
            BuildDecision(session, snapshot),
            1.25f));
    }

    /// <summary>
    /// Scenario metadata, deliberately outside the character-facing columns. The objective is
    /// visible from the opening instant; while the session is live this exposes no condition value
    /// or progress. Once resolved it adds only the authorized one-bit result.
    /// </summary>
    private static Control BuildObjective(PersistentSession session)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var margin = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 8);
        panel.AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 18);
        margin.AddChild(row);

        string heading = session.Result?.Outcome switch
        {
            ObjectiveOutcome.ObjectiveMet => "OBJECTIVE MET",
            ObjectiveOutcome.ObjectiveUnmet => "OBJECTIVE UNMET",
            _ => "SESSION OBJECTIVE",
        };
        row.AddChild(Heading(heading));
        row.AddChild(Plain($"{session.Objective.Name} before this 90-day session ends."));
        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        row.AddChild(FaintChip(session.Result is null
            ? $"Ends {session.Objective.Deadline.ToString("d MMMM yyyy 'at' HH:mm 'UTC'", CultureInfo.InvariantCulture)}"
            : $"Session ended {session.Result.ResolvedAt.ToString("d MMMM yyyy 'at' HH:mm 'UTC'", CultureInfo.InvariantCulture)}"));
        return panel;
    }

    /// <summary>
    /// The strip along the top: two rows.
    ///
    /// The first is what a player glances at — the date, who you are, your cash — and the controls
    /// that move the clock. The second is the session's state and, far right and faint, the seed and
    /// variant, which are developer metadata and were the fourth thing on the old single row.
    ///
    /// <b>Why the date used to wrap.</b> <see cref="Plain"/> sets <c>ExpandFill</c> and the old
    /// toolbar used it for every label, so four expanding labels took all the slack of the row and
    /// the non-expanding date heading was squeezed to its minimum width, where word-wrap broke it
    /// vertically. Nothing on this strip expands now except the spacer.
    /// </summary>
    private Control BuildToolbar(PersistentSession session, PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;
        var strip = new PanelContainer();
        var inner = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            inner.AddThemeConstantOverride(side, 8);
        strip.AddChild(inner);

        var rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 4);
        inner.AddChild(rows);

        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 18);
        rows.AddChild(top);

        top.AddChild(Title(session.Date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)));
        top.AddChild(Chip($"{snapshot.ViewpointName}, {snapshot.ViewpointRole}"));
        top.AddChild(Chip($"cash on hand {snapshot.Cash.ToString("N0", CultureInfo.InvariantCulture)}"));

        top.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        bool paused = session.Status == SessionStatus.AwaitingChoice;
        bool clockDisabled = session.Status != SessionStatus.Ready;

        top.AddChild(Advance("Next event", clockDisabled, () => session.StepEvent()));
        top.AddChild(Advance("Advance a day", clockDisabled, () => session.AdvanceDays(1)));
        top.AddChild(Advance("Advance a week", clockDisabled, () => session.AdvanceDays(7)));

        top.AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });

        // Save and load work while ready, paused, or resolved (ruling 6) — unlike the three clock
        // controls above, neither is disabled by session status.
        var save = new Button { Text = "Save" };
        save.Pressed += () => SaveFixedSlot(session);
        top.AddChild(save);

        var load = new Button { Text = "Load" };
        load.Pressed += LoadFixedSlot;
        top.AddChild(load);

        var bottom = new HBoxContainer();
        bottom.AddThemeConstantOverride("separation", 18);
        rows.AddChild(bottom);

        bottom.AddChild(FaintChip(session.Status switch
        {
            SessionStatus.AwaitingChoice => $"paused — {p.Subject} {p.Verb("has", "have")} something to decide",
            SessionStatus.Resolved => "session ended",
            _ => "running",
        }));
        if (_statusMessage is { } status) bottom.AddChild(FaintChip($"· {status}"));
        bottom.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        bottom.AddChild(FaintChip($"seed {session.Seed.ToString(CultureInfo.InvariantCulture)} · {session.Variant}"));

        return strip;
    }

    /// <summary>
    /// A control that moves the clock, disabled while a decision is outstanding or after resolution.
    ///
    /// Disabled rather than hidden, and the session refuses the call as well: a half-handled event
    /// is not a place time can move through, and the history would otherwise depend on how long
    /// somebody took to answer.
    /// </summary>
    private Button Advance(string label, bool disabled, Action move)
    {
        var button = new Button { Text = label, Disabled = disabled };
        button.Pressed += () =>
        {
            move();
            Refresh();
        };
        return button;
    }

    /// <summary>
    /// Everything he holds, in one list, newest first.
    ///
    /// <b>Recency is ordering and emphasis, not a second list</b> — Matt's ruling 2 for milestone
    /// 025. Ordered by when he last had cause to think about it, so a three-week-old belief somebody
    /// disputed yesterday reads as news, which is what it is. The beliefs inside
    /// <see cref="PlayerView.RecentWindow"/> come first; a faint rule separates them from the rest.
    ///
    /// A disagreement about a belief he holds is drawn under that belief — who said what, with dates
    /// — rather than as a second entry restating the claim. A disagreement about a claim he does
    /// <em>not</em> hold, which the projection allows, has no belief to nest under and is drawn on
    /// its own under its own heading.
    ///
    /// Thin or disputed beliefs are not repeated in a "cannot settle" list of their own: the
    /// certainty phrase on each entry already says "not sure" or "disputed", and drawing the belief a
    /// second time was the same duplication as the two panels this milestone removed. Who has told
    /// him nothing keeps its place, because that is not a belief.
    /// </summary>
    private IEnumerable<Control> BuildKnowledge(PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;
        if (snapshot.Known.Count == 0)
        {
            yield return Plain(
                $"Nothing yet. Nobody has told {p.Object} anything and {p.Subject} " +
                $"{p.Verb("has", "have")} seen nothing {p.Reflexive}.");
        }

        // PlayerClaim deliberately drops the incident id. Two distinct incidents can therefore
        // arrive here under one visible predicate; group at that lossy boundary, render the claim
        // once, and retain every incident's own position and basis as well as every source account.
        var byClaim = snapshot.Disagreements
            .GroupBy(d => d.Claim)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PlayerDisagreement>)g.ToList());
        var ordered = snapshot.Known
            .GroupBy(b => b.Claim)
            // The player-facing vocabulary cannot distinguish same-predicate incidents. Use the
            // freshest position for the one visible headline; all disagreement accounts survive
            // through byClaim above instead of one incident overwriting another.
            .Select(g => g.OrderByDescending(b => b.ReconsideredAt)
                          .ThenByDescending(b => b.AcquiredAt)
                          .First())
            .OrderByDescending(b => b.ReconsideredAt)
            .ThenBy(b => b.Claim.ToString(), StringComparer.Ordinal)
            .ToList();
        DateTime freshSince = snapshot.Date - PlayerView.RecentWindow;

        bool anyFresh = ordered.Any(b => b.ReconsideredAt >= freshSince);
        bool anyEarlier = ordered.Any(b => b.ReconsideredAt < freshSince);
        bool ruleDrawn = false;

        foreach (var belief in ordered)
        {
            if (anyFresh && anyEarlier && !ruleDrawn && belief.ReconsideredAt < freshSince)
            {
                yield return Faint("— earlier —");
                ruleDrawn = true;
            }

            foreach (var row in BeliefEntry(belief, byClaim.GetValueOrDefault(belief.Claim), p))
                yield return row;
        }

        // Disagreements with no held belief to sit under.
        var heldClaims = snapshot.Known.Select(b => b.Claim).ToHashSet();
        var standalone = snapshot.Disagreements
            .Where(d => !heldClaims.Contains(d.Claim))
            .GroupBy(d => d.Claim)
            .OrderBy(g => g.Key.ToString(), StringComparer.Ordinal)
            .ToList();
        if (standalone.Count > 0)
        {
            yield return new HSeparator();
            yield return Plain("ACCOUNTS DIFFER");
            foreach (var group in standalone)
            {
                var d = group.First();
                yield return ClaimEntry(d.Claim, $"on whether {d.Statement}");
                foreach (var incident in group)
                    foreach (var row in DisagreementRows(incident, p)) yield return row;
            }
        }

        if (snapshot.Silent.Count > 0)
        {
            yield return new HSeparator();
            yield return Plain("NOT HEARD FROM");
            foreach (var person in snapshot.Silent)
                yield return Faint($"· {person.Name} has not told {p.Object} anything yet");
        }

        // Milestone 026: a man who, asked, said he knew nothing. An answer, and not a belief — so it
        // sits here beside who has said nothing, not among the beliefs.
        if (snapshot.Disclaimers.Count > 0)
        {
            yield return new HSeparator();
            yield return Plain($"TOLD {p.Object.ToUpperInvariant()} THEY KNOW NOTHING");
            foreach (var d in snapshot.Disclaimers)
                yield return Faint($"{d.At.ToString("d MMM", CultureInfo.InvariantCulture)}  {d.Description}");
        }
    }

    /// <summary>
    /// One belief, drawn once: the dated statement, then how he has it, then — if his sources
    /// disagree about it — who said what. The only place a <see cref="PlayerBelief"/> is turned into
    /// widgets, which is what makes <see cref="AssertNoClaimDrawnTwice"/> a check by construction.
    /// </summary>
    private static IEnumerable<Control> BeliefEntry(
        PlayerBelief belief,
        IReadOnlyList<PlayerDisagreement>? disagreements,
        Pronouns p)
    {
        yield return ClaimEntry(
            belief.Claim,
            $"{belief.ReconsideredAt.ToString("d MMM", CultureInfo.InvariantCulture)}  {belief.Statement}");

        // Source, then how sure, then since when — each only when it says something. No certainty
        // on what he saw or did himself; no source on an act of his own the sentence already names
        // him as the author of; no "since" when he has had it since the day he got it.
        var parts = new List<string>();
        if (belief.Attribution is { } attribution) parts.Add(attribution);
        if (belief.Certainty is { } certainty) parts.Add(certainty);
        if (belief.ReconsideredAt != belief.AcquiredAt)
            parts.Add($"first learned {belief.AcquiredAt.ToString("d MMM", CultureInfo.InvariantCulture)}");
        if (parts.Count > 0) yield return Faint($"        {string.Join("; ", parts)}");

        if (disagreements is not null)
            foreach (var disagreement in disagreements)
                foreach (var row in DisagreementRows(disagreement, p)) yield return row;
    }

    /// <summary>
    /// One incident-specific disagreement beneath a possibly coalesced player claim. Its own
    /// position and basis belong to this incident just as its named accounts do; neither may be
    /// borrowed from the freshest same-predicate belief chosen for the shared heading.
    /// </summary>
    private static IEnumerable<Control> DisagreementRows(PlayerDisagreement d, Pronouns p)
    {
        if (d.OwnBasis is { } basis)
            yield return Faint(
                $"        {p.Subject} {p.Verb("thinks", "think")} " +
                $"{(d.OwnPositionHeld ? "so" : "otherwise")} ({basis})");
        foreach (var row in AccountRows(d)) yield return row;
    }

    private static IEnumerable<Control> AccountRows(PlayerDisagreement d)
    {
        foreach (var account in d.Accounts)
            yield return Faint(
                $"        {account.SourceName} {(account.Affirms ? "says so" : "says otherwise")} " +
                $"({account.At.ToString("d MMM", CultureInfo.InvariantCulture)})");
    }

    /// <summary>A claim presented as an entry of its own, stamped so the duplicate guard can find it.</summary>
    private static Label ClaimEntry(PlayerClaim claim, string text)
    {
        var label = Plain(text);
        label.SetMeta(ClaimMeta, claim.ToString());
        return label;
    }

    /// <summary>
    /// What he is doing: the order he has out, what he just did, and what he is waiting to hear
    /// back on. Milestone 024 put the standing order into the causal-thread column to avoid making
    /// the old layout worse; milestone 025 gives it the top of a panel of its own.
    ///
    /// For delegated work this says who has it and stops — how far along somebody else has got is
    /// that man's state, not his. Every value here is a projection already computed onto
    /// <see cref="PlayerSnapshot"/>, never a second record this file keeps of its own.
    /// </summary>
    private IEnumerable<Control> BuildDoing(PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;

        if (snapshot.Operation is { } op)
        {
            yield return Plain(op.Description);
            // Second correction: Since is the owner's own age for the operation, not a handover time
            // — nothing records when it was delegated — and is null for the executor for the same
            // reason "Tommy has had it since 2 Mar" would be a false statement whenever the job was
            // delegated later than it began. Omitted for him rather than rendered wrong.
            if (op.Since is { } since)
                yield return Faint($"    running since {since.ToString("d MMM", CultureInfo.InvariantCulture)}");
            if (op.ExecutorName is { } executor)
                yield return Faint($"    {executor} is handling it");
            yield return Faint($"    {op.Progress ?? "no word back yet"}");
        }
        else
        {
            yield return Plain($"{p.Subject_} {p.Verb("has", "have")} nothing running.");
        }

        yield return new HSeparator();
        yield return Plain("WHAT JUST HAPPENED");

        yield return snapshot.LastAction is { } action
            ? Faint($"{action.At.ToString("d MMM", CultureInfo.InvariantCulture)}  {p.Subject} chose to {action.Description}")
            : Faint($"{p.Subject_} {p.Verb("has", "have")} not committed to anything yet.");

        // Milestone 026: what he read off the other man's face, if anything, since he last acted.
        // The same reading is on the roster under the man; this is the one that answers "and how
        // did that go" at the moment he asks it.
        var latestRead = snapshot.Attitudes
            .SelectMany(a => a.Impressions)
            .Where(i => snapshot.LastAction is not { } last || i.At >= last.At)
            .OrderByDescending(i => i.At)
            .FirstOrDefault();
        if (latestRead is not null)
            yield return Faint($"{latestRead.At.ToString("d MMM", CultureInfo.InvariantCulture)}  {latestRead.Description}");

        if (snapshot.MyBusiness is { } business)
            yield return Faint(
                $"{Capital(p.Possessive)} own shop, {business.Name}, is " +
                $"{(business.PayingTribute ? "paying" : "not paying")} at the moment");

        yield return new HSeparator();
        yield return Plain("WAITING TO HEAR BACK");

        if (snapshot.AwaitingAnswers.Count == 0)
        {
            yield return Faint("Nothing outstanding.");
            yield break;
        }

        foreach (var request in snapshot.AwaitingAnswers)
        {
            var asked = request.AskedPronouns;
            yield return Plain(
                $"{request.AskedAt.ToString("d MMM", CultureInfo.InvariantCulture)}  asked {request.AskedName} " +
                $"what {asked.Subject} {asked.Verb("knows", "know")} about whether {request.Statement}");
            // Every entry here is Pending by construction (an Answered request already dropped out —
            // see PlayerSnapshot.AwaitingAnswers). Nothing has reached him, whether the asked person
            // has not yet decided or decided privately and said nothing — genuinely indistinguishable
            // to him, per milestone 018's second correction, and must not be rendered as though they
            // were different facts. A communicated answer, in either direction, is not shown here at
            // all — it already appears under the belief it concerns.
            yield return Faint("    no answer yet");
        }
    }

    private IEnumerable<Control> BuildAttitudes(PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;

        // Himself first — milestone 026's first correction. What he is good and bad at, in words,
        // his to know the way his cash is. Placed above the men he deals with because the last
        // clause explains the readings beneath: a man who reads people badly gets "gave nothing
        // away" a lot, and the screen should say why.
        yield return Plain(PlayerView.IsSecondPerson(p) ? "You" : snapshot.ViewpointName);
        foreach (string clause in snapshot.SelfKnowledge)
            yield return Faint($"    {clause}");

        if (snapshot.Attitudes.Count == 0)
        {
            yield return Faint($"{p.Subject_} {p.Verb("has", "have")} nothing much to say about anybody else.");
            yield break;
        }
        yield return new HSeparator();

        foreach (var attitude in snapshot.Attitudes)
        {
            yield return Plain(attitude.PersonName);
            yield return Faint($"    {attitude.Standing}");
            if (attitude.Wariness is { } wariness)
                yield return Faint($"    {wariness}");
            // What he takes the man to be good for — a belief about him, not an attitude toward
            // him, and the two are deliberately separate lines because the model keeps them
            // separate: a man whose word he would not take can still be the one he sends.
            if (attitude.TakenFor is { } takenFor)
                yield return Faint($"    {takenFor}");
            foreach (var grievance in attitude.Grievances)
                yield return Faint(
                    $"    what {p.Subject} {p.Verb("holds", "hold")} against " +
                    $"{attitude.PersonPronouns.Object}: \"{grievance}\"");

            // Why it got that way (milestone 023) and what his face seemed to say (milestone 026),
            // one timeline, oldest first — the order a history reads in, and dated because "when"
            // is most of what makes it a history rather than a list of grumbles. The arrow carries
            // the direction of a movement; the dot marks a reading, which moved nothing and can be
            // wrong.
            var timeline = attitude.History
                .Select(m => (m.At, Line: $"{(m.Warmed ? "↑" : "↓")} {m.Description}"))
                .Concat(attitude.Impressions.Select(i => (i.At, Line: $"· {i.Description}")))
                .OrderBy(x => x.At);
            foreach (var (at, line) in timeline)
                yield return Faint($"    {at.ToString("d MMM", CultureInfo.InvariantCulture)}  {line}");
        }
    }

    /// <summary>
    /// The controlled character's decision, put to the player as "you". The panel's pronouns are
    /// the *viewpoint's* until there is a pending decision to take them from — the two are the same
    /// character unless the developer viewpoint is in use, and then the panel is describing the man
    /// being watched rather than the man deciding.
    /// </summary>
    private IEnumerable<Control> BuildDecision(PersistentSession session, PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;

        if (session.Status == SessionStatus.Resolved)
        {
            yield return Plain("This bounded session has ended.");
            yield return Faint("No further decisions or clock advances are available. Save and load remain available.");
            yield break;
        }

        if (session.ControlledCharacterId is null)
        {
            yield return Plain(
                $"Nobody is under your control. Move the clock on and read what {p.Subject} " +
                $"{p.Verb("comes", "come")} to hear.");
            yield break;
        }

        if (session.Pending is not { } pending)
        {
            yield return Plain("Nothing to decide right now.");
            yield return Faint("Move the clock on until something comes up.");
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

        // Milestone 026's second correction: what hangs over him, so that "cover it up" and "deny
        // it" read as what they are — his own conscience listing exits — rather than as somebody
        // having found out. Shown only where a decision is being made, because that is where Matt
        // asked "why do I have these options".
        if (snapshot.Exposure.Count > 0)
        {
            yield return Faint($"what hangs over {p.Object}: {string.Join(" ", snapshot.Exposure)}");
        }

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
            $"These are the things that occurred to {actor.Object} and that {actor.Subject} could " +
            $"actually do. Anything {actor.Subject} did not think of is not here.");
    }

    // ================================================================= self-test

    private void RunEndingSelfTest()
    {
        try
        {
            EndingSelfTest();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"CE-ENDING FAILED — {ex}");
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// Milestone 027's rendered proof. Every advance, save, and load uses the actual button handler;
    /// every assertion reads the compiled live scene tree. The isolated self-test slot is selected in
    /// <see cref="_Ready"/> before any button exists, so this cannot touch a player's save.
    /// </summary>
    private void EndingSelfTest()
    {
        CleanupSelfTestRestartSlot();
        try
        {
            StartSession(seed: 42, variant: "baseline", controlled: null, viewpoint: "salvatore");
            var baseline = _session!;
            string opening = Screen();
            if (baseline.Date != Cast.Start
                || !opening.Contains("SESSION OBJECTIVE", StringComparison.Ordinal)
                || !opening.Contains(
                    "Bring the harbour shortfall under control before this 90-day session ends.",
                    StringComparison.Ordinal)
                || !opening.Contains("Ends 31 May 1987 at 08:00 UTC", StringComparison.Ordinal))
                throw new InvalidOperationException("the opening screen does not render the authorized objective and deadline");

            AdvanceToEndingThroughButtons(baseline);
            string unmet = Screen();
            if (baseline.Result?.Outcome != ObjectiveOutcome.ObjectiveUnmet
                || !unmet.Contains("OBJECTIVE UNMET", StringComparison.Ordinal))
                throw new InvalidOperationException("the natural baseline ending is not rendered as ObjectiveUnmet");
            AssertTerminalControls();

            if (!Press("Save") || !SaveStore.Exists(_activeSavePath))
                throw new InvalidOperationException("the resolved baseline session did not save through the live Save button");

            StartSession(seed: 42, variant: "cautious-vincent", controlled: null, viewpoint: "salvatore");
            var cautious = _session!;
            AdvanceToEndingThroughButtons(cautious);
            string met = Screen();
            if (cautious.Result?.Outcome != ObjectiveOutcome.ObjectiveMet
                || !met.Contains("OBJECTIVE MET", StringComparison.Ordinal))
                throw new InvalidOperationException("the natural cautious-vincent ending is not rendered as ObjectiveMet");
            AssertTerminalControls();

            if (!Press("Load"))
                throw new InvalidOperationException("the resolved save could not be loaded through the live Load button");
            string loaded = Screen();
            if (_session?.Result?.Outcome != ObjectiveOutcome.ObjectiveUnmet
                || _session.Status != SessionStatus.Resolved
                || !loaded.Contains("OBJECTIVE UNMET", StringComparison.Ordinal))
                throw new InvalidOperationException("loading after resolution did not reproduce the saved terminal result");
            AssertTerminalControls();

            if (loaded.Contains("RevenueLoss", StringComparison.Ordinal)
                || loaded.Contains("0.90", StringComparison.Ordinal)
                || loaded.Contains("0.15", StringComparison.Ordinal))
                throw new InvalidOperationException("the rendered terminal block exposed raw objective progress");

            GD.Print("CE-ENDING opening=ok baseline=ObjectiveUnmet cautious-vincent=ObjectiveMet controls=disabled postload=ok");
            GD.Print("CE-ENDING ok");
            GetTree().Quit();
        }
        finally
        {
            CleanupSelfTestRestartSlot();
        }
    }

    private void AdvanceToEndingThroughButtons(PersistentSession session)
    {
        for (int guard = 0; guard < 100 && session.Status != SessionStatus.Resolved; guard++)
            if (!Press("Advance a week"))
                throw new InvalidOperationException("the live week control vanished before the session resolved");

        if (session.Status != SessionStatus.Resolved)
            throw new InvalidOperationException("the live clock controls did not reach the terminal boundary");
    }

    private void AssertTerminalControls()
    {
        foreach (string label in new[] { "Next event", "Advance a day", "Advance a week" })
        {
            var button = FindAnyButton(this, label);
            if (button is null || !button.Disabled)
                throw new InvalidOperationException($"terminal clock control \"{label}\" is absent or enabled");
        }

        foreach (string label in new[] { "Save", "Load" })
        {
            var button = FindAnyButton(this, label);
            if (button is null || button.Disabled)
                throw new InvalidOperationException($"terminal persistence control \"{label}\" is absent or disabled");
        }
    }

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

        ProjectedClaimCollisionSelfTest();

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
        transcript.Append(Screen());

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
            transcript.Append(Screen());
        }

        GD.Print($"CE-SELFTEST date={session.Date:yyyy-MM-dd} " +
                 $"choices={choices.ToString(CultureInfo.InvariantCulture)} " +
                 $"decision-screens={decisionScreens.ToString(CultureInfo.InvariantCulture)}");

        GD.Print("== CE-UI-TEXT-BEGIN ==");
        GD.Print(transcript.ToString());
        GD.Print("== CE-UI-TEXT-END ==");

        // The exit code is the result. Printing a failure and exiting 0 makes the check unusable
        // from a script, which is what a verification step is for — found by milestone 009's review.
        bool proved = choices > 0 && decisionScreens > 0
            && session.Date == end && session.Status == SessionStatus.Resolved;
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

    /// <summary>
    /// Astra's accepted historical-audit finding L3, exercised at the rendering boundary. The
    /// simulation projection may contain two incident-specific disagreements whose truth-log ids
    /// are deliberately absent from their identical <see cref="PlayerClaim"/> values. The live
    /// knowledge renderer must draw that visible predicate once and retain both incidents' own
    /// positions and bases as well as both source accounts.
    /// </summary>
    private void ProjectedClaimCollisionSelfTest()
    {
        var claim = new PlayerClaim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery);
        const string statement = "Tommy Nardo got violent at Bellini's grocery";
        DateTime first = Cast.Start.AddDays(1);
        DateTime second = Cast.Start.AddDays(4);

        var snapshot = new PlayerSnapshot(
            Date: second,
            ViewpointId: "salvatore",
            ViewpointName: "Salvatore Greco",
            ViewpointRole: "boss",
            ViewpointPronouns: PlayerView.You,
            Cash: 0,
            SelfKnowledge: Array.Empty<string>(),
            Known: new[]
            {
                new PlayerBelief(
                    claim, statement, first, second, "disputed", "you found out for yourself",
                    Contested: true, IsHeld: true),
            },
            Disagreements: new[]
            {
                new PlayerDisagreement(
                    claim, statement, "what you saw first", OwnPositionHeld: true,
                    new[] { new PlayerAccount("vincent", "Vincent Russo", Affirms: false, first) }),
                new PlayerDisagreement(
                    claim, statement, "what you worked out later", OwnPositionHeld: false,
                    new[] { new PlayerAccount("kane", "Detective Eileen Kane", Affirms: true, second) }),
            },
            Attitudes: Array.Empty<PlayerAttitude>(),
            Unsettled: Array.Empty<PlayerBelief>(),
            Silent: Array.Empty<PlayerPerson>(),
            Disclaimers: Array.Empty<PlayerDisclaimer>(),
            Exposure: Array.Empty<string>(),
            LastAction: null,
            MyBusiness: null,
            Operation: null,
            AwaitingAnswers: Array.Empty<PlayerRequest>());

        Clear(_root);
        _root.AddChild(Panel("WHAT YOU KNOW", BuildKnowledge(snapshot), 1.0f));
        string screen = Screen();

        int statementCount = screen.Split(statement, StringSplitOptions.None).Length - 1;
        if (statementCount != 1)
            throw new InvalidOperationException(
                $"the projected claim collision rendered its visible predicate {statementCount} times");
        if (!screen.Contains("Vincent Russo says otherwise", StringComparison.Ordinal)
            || !screen.Contains("Detective Eileen Kane says so", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "the projected claim collision did not render every incident's source account");
        if (!screen.Contains("you think so (what you saw first)", StringComparison.Ordinal)
            || !screen.Contains("you think otherwise (what you worked out later)", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "the projected claim collision did not render every incident's own position and basis");
    }

    // ================================================================= golden path (milestone 014)

    /// <summary>
    /// The exact option descriptions Vincent's accepted seed-42 <c>SecureTribute</c> operation offers,
    /// in order — shared by <see cref="GoldenPathSelfTest"/> and milestone 015's two-process restart
    /// proof, which is this same sequence split after the third choice rather than a second,
    /// independently-typed copy of it. Independently derived from the same accepted trace as
    /// <c>PlayerOwnedOperationTests</c> in the test project, not shared with it, so the two checks
    /// cannot both be wrong about the same assumption.
    /// </summary>
    // Re-derived 2026-09-11 by milestone 024's sixth correction — see the identical copy's own
    // comment in PersistenceTests.cs/PlayerOwnedOperationTests.cs for the full reasoning. Vincent can
    // no longer alter a delegated operation, so his escalation is delegated to Tommy at step 3 and
    // Tommy's own escalation and the operation's genuine completion happen autonomously in the
    // background; Vincent's remaining pauses report the completed result and answer the next day's
    // organizational review. The former second collection cycle was a real double-payment defect
    // exposed by Matt's 2026-09-12 playtest.
    private static readonly string[] GoldenPathChoiceSequence =
    {
        "persuade Bellini's grocery to pay",
        "carry on getting Bellini's grocery to pay",
        "hand it to Tommy Nardo",
        "ask Salvatore Greco for permission",
        "report the situation to Salvatore Greco",
        "ask Salvatore Greco for permission",
    };

    /// <summary>
    /// Presses each of <paramref name="choices"/> in order, using "Next event" to reach each pause —
    /// never "Advance a week", which carries an outstanding fast-forward horizon across a
    /// <c>Choose</c> call and can silently sail past the decision this is looking for. Throws rather
    /// than returning a partial result if the run gives up before every choice is made, so a caller
    /// never has to remember to check how far it got. Every screen built on the way is checked for a
    /// claim drawn twice.
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
                        $"the decision on {session.Date:yyyy-MM-dd} does not offer \"{expected}\" — offered: " +
                        string.Join(" | ", session.Pending!.Options.Select(o => o.Description)));
                choiceIndex++;
            }
            else
            {
                if (!Press("Next event"))
                    throw new InvalidOperationException("no \"Next event\" control is available");
            }

            AssertNoClaimDrawnTwice();
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

    /// <summary>
    /// Drives Vincent's own seed-42 <c>SecureTribute</c> operation against Bellini's grocery through
    /// real button presses — the interactive playthrough itself, not a claim about it — and reads the
    /// rendered cash label off the live screen, never <see cref="SimulationSession"/>'s internal
    /// state, to confirm the accepted consequence: 6,000 rising to 6,840.
    ///
    /// <b>Asserts the opening screen too, before any button is pressed.</b> A check that only reads
    /// the final screen cannot tell a real 6,000-to-6,840 change from a toolbar that always rendered
    /// 6,840 regardless of what happened — confirmed by mutation: temporarily hardcoding the toolbar
    /// to a fixed "6,840" made the opening assertion fail, before the mutation was reverted.
    /// </summary>
    private void GoldenPathSelfTest()
    {
        GD.Print("CE-GOLDENPATH begin");

        StartSession(seed: 42, variant: "baseline", controlled: Roster.DefaultControlledId, viewpoint: Roster.DefaultControlledId);
        var session = _session!;

        if (!Screen().Contains("cash on hand 6,000", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "the opening screen does not read \"cash on hand 6,000\" — the golden path's own " +
                "starting point is wrong, so the later 6,840 would prove nothing");

        PressChoicesInOrder(session, GoldenPathChoiceSequence, "CE-GOLDENPATH");

        // No further pause should follow the final choice unaddressed — if one does, something
        // (an unaddressed decision, a fast-forward that outran the choice just made) has silently
        // moved past the point this check claims to have reached.
        if (session.Status == SessionStatus.AwaitingChoice)
            throw new InvalidOperationException(
                $"an unaddressed decision followed the final choice, on {session.Date:yyyy-MM-dd} — " +
                "the run has moved past the point it should have stopped at");

        // The rendered screen, exactly as a person watching would read it — collected the same way
        // the general self-test proves its own transcript, never by reading World or Capabilities.
        string screen = Screen();

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
            "CE-GOLDENPATH FAILED — did not reach the accepted 5 April consequence with cash on hand " +
            "reading 6,840 on screen, so it proves nothing");
        GetTree().Quit(1);
    }

    // ================================================================= direct action (milestone 017)

    /// <summary>
    /// Milestone 017: the same seed-42 Vincent <c>SecureTribute</c> operation
    /// <see cref="GoldenPathSelfTest"/> plays, but never delegated — Vincent presses "carry on" at the
    /// exact pause that also offers "hand it to Tommy Nardo" (the fork this milestone is about),
    /// then continues personally through escalation. Independently pinned from a live run of the
    /// interactive path, the same way <see cref="GoldenPathChoiceSequence"/> itself was derived, not shared
    /// with it or with the test project's copy of the same fork.
    ///
    /// Diverges from the accepted delegated trace naturally, through real button presses alone: Vincent
    /// himself — not Tommy — puts hands on Bellini's grocery, conceals it himself, and answers for it
    /// himself. Proceeds still land on Vincent regardless (cash still rises to 6,840) — ownership
    /// determines proceeds, execution is what diverged, exactly the milestone's own distinction.
    ///
    /// <b>Re-derived 2026-09-09, after the choices past the fourth stopped matching</b> (the
    /// `Rng.ForOccasion` correction redistributing which observation opportunities land at seed 42),
    /// <b>and again 2026-09-11 by milestone 024's sixth correction.</b> This fork was never delegated,
    /// so it was never affected by that correction's own delegated-execution changes directly — but the
    /// concealment step's own duration changed: "cover it up" now resolves completely in one step,
    /// where it previously needed a "carry on covering it up" continuation two pauses later, and
    /// nothing after it (the old sequence's trailing "ask Salvatore Greco for permission" beat) is
    /// offered at the point this now stops. Re-derived from a fresh live run of the interactive path,
    /// not patched around the mismatch. Confirmed against the live screen, not assumed: cash still
    /// reads 6,840, "you got violent at Bellini's grocery" is on screen in the second person throughout,
    /// and "Tommy Nardo got violent" never appears.
    /// </summary>
    private static readonly string[] DirectActionChoiceSequence =
    {
        "persuade Bellini's grocery to pay",
        "carry on getting Bellini's grocery to pay",
        "switch to threats with Bellini's grocery",
        "switch to force with Bellini's grocery — breaking the rule: no public violence in the harbour",
        "deny it to Salvatore Greco: tell him you did not get violent at Bellini's grocery",
        "tell Salvatore Greco what you know about whether somebody on the street saw you at Bellini's grocery",
        "cover it up before anyone finds out",
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
            AssertNoClaimDrawnTwice();
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

        if (!Screen().Contains("cash on hand 6,000", StringComparison.Ordinal))
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
        bool delegateRendered = FindButton(this, "hand it to Tommy Nardo") is not null;
        if (!carryOnRendered || !delegateRendered)
            throw new InvalidOperationException(
                "the fork pause does not render both continuation and delegation as real buttons — " +
                $"carry-on button present: {carryOnRendered}, delegate button present: {delegateRendered}");

        PressChoicesInOrder(session, DirectActionChoiceSequence.Skip(1).ToList(), "CE-DIRECTACTION");

        string screen = Screen();

        GD.Print("== CE-DIRECTACTION-SCREEN-BEGIN ==");
        GD.Print(screen);
        GD.Print("== CE-DIRECTACTION-SCREEN-END ==");

        // Ownership determines proceeds regardless of who executed — unchanged from the accepted
        // delegated trace's own consequence, checked as a required negative: this is not where the
        // two branches diverge.
        bool proceeds = screen.Contains("cash on hand 6,840", StringComparison.Ordinal);

        // Execution responsibility is exactly what diverged: Vincent himself put hands on the target
        // and knows it as his own act — in the second person, since he is the man playing — never
        // Tommy, the opposite of the accepted delegated trace, where Tommy is the one named and
        // Vincent only came across it.
        bool executedPersonally = screen.Contains("you got violent at Bellini's grocery", StringComparison.Ordinal);
        bool noTommyExecution = !screen.Contains("Tommy Nardo got violent at Bellini's grocery", StringComparison.Ordinal);

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
    /// Vincent's own seed-42 <c>cautious-vincent</c> answer to Salvatore's question, played through
    /// real buttons.
    ///
    /// Reached via "Next event" alone, never a fast-forward control — pressing "Advance a day/week"
    /// would carry an outstanding horizon across a choice and run the session straight past the
    /// unresolved moment this proof exists to catch, exactly the reason <see cref="PressChoicesInOrder"/>
    /// and <see cref="AdvanceToPause(PersistentSession)"/> already use "Next event" only.
    ///
    /// <b>Retargeted 2026-09-11 by milestone 024's sixth correction.</b> Salvatore's own question still
    /// arrives entirely on its own, unstaged — confirmed directly — but Vincent's own autonomous choice
    /// at the resulting wake no longer answers it with the exact asked claim (see
    /// <c>docs/OPEN_CONCERNS.md</c> #7), so Vincent is controlled here instead of Salvatore, driven
    /// through his own known earlier choices (start, continue, delegate — pressed explicitly by their
    /// known text, since this project has no access to the test assembly's automatic-resolution helper)
    /// up to the one decision that matters: answering Salvatore's question with the exact claim asked
    /// about. And found while retargeting: the answer confirms rather than contradicts what Salvatore's
    /// own assignment already told Vincent in the first place, so this now reads a resolved, attributed
    /// corroboration rather than the old "Vincent Russo says otherwise" disagreement framing.
    /// </summary>
    private void CorroborationSelfTest()
    {
        GD.Print("CE-CORROBORATION begin");

        StartSession(seed: 42, variant: "cautious-vincent", controlled: "vincent", viewpoint: "salvatore");
        var session = _session!;

        const string claim = "Bellini's grocery is not paying its tribute";
        const string answer = "tell Salvatore Greco what you know about whether Bellini's grocery is not paying its tribute";

        PressChoicesInOrder(session, new[]
        {
            "persuade Bellini's grocery to pay",
            "carry on getting Bellini's grocery to pay",
            "hand it to Tommy Nardo",
        }, "CE-CORROBORATION");

        // Salvatore's own question arrives on its own by now; find Vincent's resulting wake and
        // answer it with the exact claim, rather than pressing "Next event" past it.
        bool offered = false;
        for (int guard = 0; guard < 400 && !offered; guard++)
        {
            AdvanceToPause(session);
            if (session.Pending!.Options.Any(o => o.Description == answer)) { offered = true; break; }
            if (!Press("Next event"))
                throw new InvalidOperationException("Vincent never reached a decision offering the exact-claim answer");
        }

        if (!Press(answer))
            throw new InvalidOperationException($"could not press \"{answer}\"");

        string finalText = Screen();

        GD.Print("== CE-CORROBORATION-FINAL-BEGIN ==");
        GD.Print(finalText);
        GD.Print("== CE-CORROBORATION-FINAL-END ==");

        // Resolved: the request no longer reads as outstanding. Attributed: Vincent's own account
        // appears against the claim, as a corroboration rather than a dispute.
        bool resolved = !finalText.Contains("no answer yet", StringComparison.Ordinal);
        int claimAt = finalText.IndexOf(claim, StringComparison.Ordinal);
        int attributionAt = finalText.IndexOf("Vincent Russo", StringComparison.Ordinal);
        bool attributedToVincent = claimAt >= 0 && attributionAt > claimAt;

        if (resolved && attributedToVincent)
        {
            GD.Print("CE-CORROBORATION ok");
            GetTree().Quit();
            return;
        }

        GD.PrintErr(
            "CE-CORROBORATION FAILED — resolved=" + resolved + " attributedToVincent=" + attributedToVincent +
            " — Vincent's answer did not resolve and attribute on the live screen");
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

        string beforeChoiceText = Screen();

        GD.Print("== CE-TRIBUTE-BEFORE-BEGIN ==");
        GD.Print(beforeChoiceText);
        GD.Print("== CE-TRIBUTE-BEFORE-END ==");

        bool namesTheDemander =
            beforeChoiceText.Contains("Vincent Russo is demanding tribute from you", StringComparison.Ordinal);
        if (!namesTheDemander)
        {
            GD.PrintErr("CE-TRIBUTE FAILED — the panel does not name the demander before Marco chooses");
            GetTree().Quit(1);
            return;
        }

        if (!Press("refuse Vincent Russo"))
            throw new InvalidOperationException("could not press \"refuse Vincent Russo\"");

        string afterChoiceText = Screen();

        GD.Print("== CE-TRIBUTE-AFTER-BEGIN ==");
        GD.Print(afterChoiceText);
        GD.Print("== CE-TRIBUTE-AFTER-END ==");

        bool acknowledged = afterChoiceText.Contains("chose to refuse Vincent Russo", StringComparison.Ordinal);
        bool businessStatusShown =
            afterChoiceText.Contains("Bellini's grocery, is not paying at the moment", StringComparison.Ordinal);
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

    // ================================================================= what he takes a man for (milestones 023/024)

    private void RunCapabilitySelfTest()
    {
        try
        {
            CapabilitySelfTest();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"CE-CAPABILITY FAILED — {ex}");
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// Proves the "what he takes a man for" line — <see cref="PlayerAttitude.TakenFor"/> — reaches
    /// the live rendered roster panel, against the real screen rather than the snapshot behind it.
    ///
    /// <b>Why this needs the live screen and not only a `PlayerView`-level check.</b> Codex's review
    /// of `53694a2` found the equivalent xunit test asserting `rendered.Contains("hard man")` against
    /// the whole console render — false assurance, because <c>WHAT VINCENT HAS</c> (the belief list)
    /// already contains "Angelo Conti is a hard man" independently of whether the attitude panel
    /// renders anything at all; that assertion would pass even with `TakenFor` removed entirely. The
    /// same risk exists here: the Godot roster's own belief panel, <c>WHAT VINCENT KNOWS</c>, carries
    /// the identical words. This isolates the attitude panel specifically — <c>WHAT ... THINKS OF
    /// PEOPLE</c> — by locating that section header in the flattened screen text and asserting only
    /// against what follows it, the same technique the corrected xunit test now uses against
    /// <c>IntelligenceWriter</c>'s own section.
    ///
    /// <c>capable-angelo</c>, not <c>baseline</c>: the one variant whose own <c>Cast.Build</c> seeds
    /// Vincent's belief that Angelo clears both capability bars, so the line is reached naturally
    /// rather than staged. No fixture, capability rule, or scoring changed to make this reachable —
    /// the belief was already seeded there for milestone 020's own purposes.
    /// </summary>
    private void CapabilitySelfTest()
    {
        GD.Print("CE-CAPABILITY begin");

        StartSession(seed: 42, variant: "capable-angelo", controlled: Roster.DefaultControlledId, viewpoint: Roster.DefaultControlledId);

        string screen = Screen();

        GD.Print("== CE-CAPABILITY-SCREEN-BEGIN ==");
        GD.Print(screen);
        GD.Print("== CE-CAPABILITY-SCREEN-END ==");

        // "OF PEOPLE" rather than "THINKS OF PEOPLE": the panel header is second person for the
        // character being played ("WHAT YOU THINK OF PEOPLE") and third person otherwise ("WHAT
        // VINCENT RUSSO THINKS OF PEOPLE") — Vincent is both here, since he is controlled and the
        // viewpoint, so the second-person form is what is actually on screen.
        int attitudeSection = screen.IndexOf("OF PEOPLE", StringComparison.Ordinal);
        bool sectionFound = attitudeSection >= 0;
        string afterHeader = sectionFound ? screen[attitudeSection..] : "";
        bool takenForShown = afterHeader.Contains("hard man", StringComparison.Ordinal);

        // The false-assurance risk this test exists to close, confirmed rather than assumed: the
        // words appear earlier on the same screen, in the belief panel, before the attitude section
        // even starts — so a check against the whole screen would never have caught the mutation
        // below.
        bool wordsAlsoAppearEarlier = sectionFound && screen[..attitudeSection].Contains("hard man", StringComparison.Ordinal);

        if (sectionFound && takenForShown && wordsAlsoAppearEarlier)
        {
            GD.Print("CE-CAPABILITY ok");
            GetTree().Quit();
            return;
        }

        GD.PrintErr(
            "CE-CAPABILITY FAILED — sectionFound=" + sectionFound + " takenForShown=" + takenForShown +
            " wordsAlsoAppearEarlier=" + wordsAlsoAppearEarlier +
            " — the attitude panel does not show what Vincent takes Angelo for, so it proves nothing");
        GetTree().Quit(1);
    }

    // ================================================================= the operation panel (milestone 024)

    private void RunOperationSelfTest()
    {
        try
        {
            OperationSelfTest();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"CE-OPERATION FAILED — {ex}");
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// Proves the delegated operation line — <see cref="PlayerSnapshot.Operation"/> — reaches the
    /// live rendered <c>DOING</c> panel, from both sides of the same delegation, against the real
    /// screen rather than the snapshot behind it.
    ///
    /// <b>Why this needs the live screen and not only a `PlayerView`-level check.</b> Codex's review
    /// of `f993386` found no coverage on either final presentation surface at all. The same
    /// false-assurance risk `--selftest-capability` closed on the attitude panel exists here too:
    /// `Bellini's grocery` already appears in `WHAT ... KNOWS`, the belief panel, independently of
    /// whether the `DOING` panel renders the operation at all — confirmed below, not assumed. This
    /// isolates the `DOING` panel's own section — between its header (`DOING`, matching either the
    /// second- or third-person form) and the next block it always prints, `WHAT JUST HAPPENED` —
    /// before asserting against it.
    ///
    /// Nobody is controlled here — the natural day-20 baseline runs autonomously, the identical
    /// fixture <c>OperationReadsTests.The_panel_is_populated_during_a_natural_run</c> reads via
    /// <c>Runner.Run</c> directly — so this proves the live interface, not a staged screen.
    /// </summary>
    private void OperationSelfTest()
    {
        GD.Print("CE-OPERATION begin");

        StartSession(seed: 42, variant: "baseline", controlled: null, viewpoint: "vincent");
        _session!.AdvanceDays(20);
        Refresh();
        string vincentScreen = Screen();

        GD.Print("== CE-OPERATION-VINCENT-BEGIN ==");
        GD.Print(vincentScreen);
        GD.Print("== CE-OPERATION-VINCENT-END ==");

        (bool ok, string why) vincentResult = CheckOperationSection(
            vincentScreen, mustContain: "Tommy Nardo is handling it", mustNotContain: "made his demand");

        StartSession(seed: 42, variant: "baseline", controlled: null, viewpoint: "tommy");
        _session!.AdvanceDays(20);
        Refresh();
        string tommyScreen = Screen();

        GD.Print("== CE-OPERATION-TOMMY-BEGIN ==");
        GD.Print(tommyScreen);
        GD.Print("== CE-OPERATION-TOMMY-END ==");

        (bool ok, string why) tommyResult = CheckOperationSection(
            tommyScreen, mustContain: "made his demand", mustNotContain: "is handling it");

        if (vincentResult.ok && tommyResult.ok)
        {
            GD.Print("CE-OPERATION ok");
            GetTree().Quit();
            return;
        }

        GD.PrintErr(
            "CE-OPERATION FAILED — vincent: " + vincentResult.why + " — tommy: " + tommyResult.why);
        GetTree().Quit(1);
    }

    /// <summary>
    /// Isolates the <c>DOING</c> panel's own section of a flattened screen (between its header and
    /// the <c>WHAT JUST HAPPENED</c> block it always prints next) and checks it against exactly one
    /// expected phrase and one phrase that must not be there — plus, as a demonstrated precondition
    /// rather than an assumed one, that the expected phrase's key noun phrase ("Bellini's grocery")
    /// already appears earlier on the same screen, in the unrelated belief panel, proving the
    /// false-assurance risk a whole-screen check would have missed is real.
    /// </summary>
    private static (bool Ok, string Why) CheckOperationSection(string screen, string mustContain, string mustNotContain)
    {
        int doingStart = screen.IndexOf("DOING", StringComparison.Ordinal);
        if (doingStart < 0) return (false, "no DOING panel header found");

        int doingEnd = screen.IndexOf("WHAT JUST HAPPENED", doingStart, StringComparison.Ordinal);
        if (doingEnd < 0) return (false, "no WHAT JUST HAPPENED marker found after DOING");

        string section = screen[doingStart..doingEnd];

        bool wordsAppearEarlier = screen[..doingStart].Contains("Bellini's grocery", StringComparison.Ordinal);
        if (!wordsAppearEarlier)
            return (false, "the false-assurance precondition did not hold — nothing to prove by isolating the section");

        if (!section.Contains(mustContain, StringComparison.Ordinal))
            return (false, $"DOING section does not contain \"{mustContain}\"");
        if (section.Contains(mustNotContain, StringComparison.Ordinal))
            return (false, $"DOING section wrongly contains \"{mustNotContain}\"");

        return (true, "");
    }

    // ================================================================= restart proof (milestone 015)

    /// <summary>
    /// Process A of the two-process restart proof: plays <see cref="GoldenPathChoiceSequence"/>'s first
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

        if (!Screen().Contains("cash on hand 6,000", StringComparison.Ordinal))
            throw new InvalidOperationException("the opening screen does not read \"cash on hand 6,000\"");

        PressChoicesInOrder(session, GoldenPathChoiceSequence.Take(3).ToArray(), "CE-RESTART-SAVE");

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
    /// <see cref="GoldenPathChoiceSequence"/>'s remaining three choices through real buttons, reaching the
    /// same accepted 5 April consequence <see cref="GoldenPathSelfTest"/> reaches in one continuous
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

            PressChoicesInOrder(session, GoldenPathChoiceSequence.Skip(3).ToArray(), "CE-RESTART-LOAD");

            // Same check GoldenPathSelfTest makes: nothing unaddressed should follow the final choice.
            if (session.Status == SessionStatus.AwaitingChoice)
                throw new InvalidOperationException(
                    $"an unaddressed decision followed the final choice, on {session.Date:yyyy-MM-dd}");

            string screen = Screen();

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
                "CE-RESTART-LOAD FAILED — did not reach the accepted 5 April consequence with cash on hand " +
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

    private static Button? FindAnyButton(Node node, string text)
    {
        if (node is Button b && b.Text == text) return b;

        foreach (var child in node.GetChildren())
            if (FindAnyButton(child, text) is { } found)
                return found;

        return null;
    }

    private static bool SelfTestRequested()
        => OS.GetCmdlineArgs().Contains(SelfTestFlag) || OS.GetCmdlineUserArgs().Contains(SelfTestFlag);

    /// <summary>
    /// The live screen as text, checked for a claim drawn twice on the way. Every self-test reads the
    /// interface through this, so the guard runs on every screen any of them looks at.
    /// </summary>
    private string Screen()
    {
        AssertNoClaimDrawnTwice();
        var sb = new StringBuilder();
        Collect(this, sb);
        return sb.ToString();
    }

    /// <summary>
    /// Milestone 025's one real regression guard: no claim is presented as an entry of its own twice
    /// on one screen. Walks the live node tree for labels stamped by <see cref="ClaimEntry"/> and
    /// throws on a repeat. Mutation-checked by re-adding a second rendering of the belief list, which
    /// fails every self-test at the first screen with a belief on it.
    ///
    /// The limit, stated: a second rendering that bypassed <see cref="ClaimEntry"/> would not be
    /// stamped and would escape. <see cref="BeliefEntry"/> is the only thing that accepts a
    /// <see cref="PlayerBelief"/>, so writing such a bypass means writing a second belief-drawing
    /// path, which is the thing prohibited.
    /// </summary>
    private void AssertNoClaimDrawnTwice()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string claim in ClaimsDrawn(this))
            if (!seen.Add(claim))
                throw new InvalidOperationException($"the claim {claim} is drawn twice on one screen");
    }

    private static IEnumerable<string> ClaimsDrawn(Node node)
    {
        if (node is Label label && label.HasMeta(ClaimMeta))
            yield return label.GetMeta(ClaimMeta).AsString();

        foreach (var child in node.GetChildren())
            foreach (string claim in ClaimsDrawn(child))
                yield return claim;
    }

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

    private static string Capital(string s) => char.ToUpperInvariant(s[0]) + s[1..];

    // ================================================================= plain widgets

    /// <summary>The screen's title, and the date on the strip: larger, never wrapped, never expanding.</summary>
    private static Label Title(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 22);
        return label;
    }

    /// <summary>A panel heading: a little larger than body text, so the eye finds the panel first.</summary>
    private static Label Heading(string text)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", 17);
        return label;
    }

    /// <summary>A short fact on the strip. Takes its own width and no more — see <see cref="BuildToolbar"/>.</summary>
    private static Label Chip(string text) => new()
    {
        Text = text,
        SizeFlagsVertical = SizeFlags.ShrinkCenter,
    };

    private static Label FaintChip(string text)
    {
        var label = Chip(text);
        label.Modulate = new Color(1, 1, 1, 0.72f);
        return label;
    }

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

    /// <summary>
    /// A boxed panel with a heading and a scrolling body. <paramref name="weight"/> is its share of
    /// the row's width relative to its neighbours.
    /// </summary>
    private static Control Panel(string title, IEnumerable<Control> rows, float weight)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = weight,
        };

        var margin = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 10);
        panel.AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        margin.AddChild(box);

        box.AddChild(Heading(title));
        box.AddChild(new HSeparator());

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        box.AddChild(scroll);

        var inner = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        inner.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(inner);

        foreach (var row in rows) inner.AddChild(row);
        return panel;
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
