using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CrimeSim.Scenario;
using CrimeSim.Session;
using Godot;

namespace CrimeEmpire.GodotShell;

/// <summary>
/// The whole interface.
///
/// <b>What it is allowed to know.</b> One <see cref="SimulationSession"/>, and through it exactly
/// two things: a <see cref="PlayerSnapshot"/>, which is bounded by the viewpoint character's own
/// cognition and relationships, and a <see cref="PendingDecision"/>, which is the controlled
/// character's own options. It cannot reach the world, the truth log, a decision record, a utility
/// score or anybody else's beliefs, because the session does not expose them — that is enforced by
/// the type system, not by this file's restraint.
///
/// <b>It consumes structured data and never parses text.</b> Nothing here reads
/// <c>IntelligenceWriter</c>'s console rendering; every label below is built from a snapshot field.
/// The console renderer and this file are two layouts over one source-limited derivation, which is
/// the point of <see cref="PlayerView"/> existing.
///
/// <b>It is deliberately plain.</b> No theme, no art, no animation, no map, no save. Labels in
/// columns and buttons that move the clock. Milestone 009's scope forbids polish beyond a clear
/// functional layout, and there is a reason beyond time: a shell that looked finished would invite
/// judgements about the game that only the simulation can earn.
/// </summary>
public partial class Game : Control
{
    /// <summary>Command-line switch that drives the real interface headlessly and dumps what it built.</summary>
    private const string SelfTestFlag = "--selftest";

    /// <summary>How far the self-test runs the scenario, matching the runner's default span.</summary>
    private const int SelfTestDays = 90;

    private VBoxContainer _root = null!;
    private SimulationSession? _session;

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

        if (SelfTestRequested())
        {
            RunSelfTest();
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

        var begin = new Button { Text = "Begin" };
        begin.Pressed += BeginFromStartScreen;
        _root.AddChild(begin);

        _root.AddChild(Plain(
            "Controlling somebody stops the clock whenever they have a decision to make, and offers " +
            "what actually occurred to them. Everyone else goes on deciding for themselves either " +
            "way."));
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
        _session = SimulationSession.Start(seed, variant, controlled, viewpoint);
        Refresh();
    }

    // ================================================================= main screen

    private void Refresh()
    {
        if (_session is not { } session) return;

        Clear(_root);

        var snapshot = session.Snapshot();

        _root.AddChild(BuildToolbar(session, snapshot));
        _root.AddChild(new HSeparator());

        var columns = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 18);
        _root.AddChild(columns);

        columns.AddChild(Column("WHAT HE KNOWS", BuildKnowledge(snapshot)));
        columns.AddChild(Column("LATELY", BuildRecent(snapshot)));
        columns.AddChild(Column("HOW HE TAKES THEM", BuildAttitudes(snapshot)));
        columns.AddChild(Column(
            session.ControlledCharacterId is null ? "NOBODY IS BEING CONTROLLED" : "A DECISION",
            BuildDecision(session, snapshot)));
    }

    private Control BuildToolbar(SimulationSession session, PlayerSnapshot snapshot)
    {
        var p = snapshot.ViewpointPronouns;
        var bar = new HBoxContainer();
        bar.AddThemeConstantOverride("separation", 12);

        bar.AddChild(Heading(session.Date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)));
        bar.AddChild(Plain($"· {snapshot.ViewpointName}, {snapshot.ViewpointRole}"));
        bar.AddChild(Plain($"· seed {session.Seed.ToString(CultureInfo.InvariantCulture)} · {session.Variant}"));

        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        bar.AddChild(spacer);

        bool paused = session.Status == SessionStatus.AwaitingChoice;

        bar.AddChild(Advance("Next event", paused, () => session.StepEvent()));
        bar.AddChild(Advance("Advance a day", paused, () => session.AdvanceDays(1)));
        bar.AddChild(Advance("Advance a week", paused, () => session.AdvanceDays(7)));

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
            yield break;
        }

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

    /// <summary>
    /// The controlled character's decision. Its pronouns are the *viewpoint's* until there is a
    /// pending decision to take them from — the two are the same character in this shell, and where
    /// they are not, the panel is describing the man being watched rather than the man deciding.
    /// </summary>
    private IEnumerable<Control> BuildDecision(SimulationSession session, PlayerSnapshot snapshot)
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
