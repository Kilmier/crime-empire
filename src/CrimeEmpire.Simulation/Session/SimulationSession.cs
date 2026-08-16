namespace CrimeSim.Session;

using CrimeSim.Decision;
using CrimeSim.Scenario;
using CrimeSim.Sim;

/// <summary>Whether the session can be advanced, or is waiting on the person controlling somebody.</summary>
public enum SessionStatus
{
    /// <summary>Time can be advanced.</summary>
    Ready,

    /// <summary>
    /// The controlled character has worked out what is open to him and has not committed to
    /// anything. Nothing else in the world may run until the choice is made — his decision is part
    /// of the event currently being handled, and letting later events resolve around a half-handled
    /// one would make the history depend on how long a person took to answer.
    /// </summary>
    AwaitingChoice,
}

/// <summary>
/// The boundary an interface talks to. Engine-neutral by construction: nothing in this file, or
/// anything it returns, names a Godot type, a console type, or a file.
///
/// <b>What it is for.</b> A player needs three things the batch runner never had to provide — a
/// clock he can move in the increments he chooses, a stopping point when the character he controls
/// has a decision to make, and a picture of the world limited to what that character could know.
/// This supplies exactly those three and nothing else.
///
/// <b>What it deliberately does not supply.</b> <see cref="World"/> is <c>internal</c>, so a UI
/// cannot reach the truth log, the decision records, the report log, the organisation's conditions,
/// or anybody else's cognition through this object at all — not by discipline but because the type
/// system will not name it. The only things that come out are <see cref="PlayerSnapshot"/> and
/// <see cref="PendingDecision"/>, both immutable and both built from the viewpoint or controlled
/// character's own state.
///
/// <b>Time.</b> There is no tick. Advancing raises a horizon and drains the existing scheduled-event
/// queue up to it, exactly as <see cref="Runner.Run"/> always did; empty days still cost nothing and
/// the clock still only lands on the times of real events. Which is why the stepping pattern cannot
/// change the outcome: the sequence of events handled is a property of the queue, and the horizon
/// only decides where a call stops reading it.
/// </summary>
public sealed class SimulationSession
{
    private readonly World _world;
    private readonly string? _controlledId;

    private PreparedDecision? _prepared;
    private PendingDecision? _pending;

    /// <summary>
    /// The player-facing calendar, which is not <see cref="World.Now"/>.
    ///
    /// <c>World.Now</c> is the time of the last event actually processed; this is how far the player
    /// has authorised time to run. They differ whenever a fast-forward crosses quiet days — the
    /// world's clock stops at the last thing that happened, and the player's calendar reaches the
    /// date he asked for. It never runs backwards: stepping a single event after a fast-forward
    /// processes something scheduled before the horizon, and the displayed date stays put.
    /// </summary>
    private DateTime _clock;

    /// <summary>
    /// An outstanding fast-forward target, or null when the last instruction was a single step.
    ///
    /// Kept so that a pause for a choice does not silently cancel the rest of the week the player
    /// asked for: he chooses, and the fast-forward carries on.
    /// </summary>
    private DateTime? _runUntil;

    private SimulationSession(World world, int seed, string variant, string? controlledId, string viewpointId)
    {
        _world = world;
        _controlledId = controlledId;
        Seed = seed;
        Variant = variant;
        ViewpointCharacterId = viewpointId;
        _clock = world.Now;
        StartedOn = world.Now;
    }

    /// <summary>
    /// Opens a session on the harbour scenario.
    ///
    /// <paramref name="controlledCharacterId"/> null is the batch simulation with a viewpoint on it:
    /// every character acts autonomously and the session never pauses. Naming somebody makes only
    /// that character's deliberations stop for a choice; everybody else continues to decide for
    /// themselves.
    ///
    /// <paramref name="viewpointCharacterId"/> is whose knowledge the snapshot is limited to. It
    /// defaults to the controlled character, and the two are separable on purpose — watching the
    /// scenario through the boss's eyes while nobody is controlled is how the accepted runner
    /// already reads it.
    /// </summary>
    public static SimulationSession Start(
        int seed,
        string variant,
        string? controlledCharacterId,
        string? viewpointCharacterId = null)
    {
        if (!Variants.All.Contains(variant, StringComparer.Ordinal))
            throw new ArgumentException(
                $"unknown scenario variant '{variant}'; expected one of {string.Join(", ", Variants.All)}",
                nameof(variant));

        var world = Cast.Build(seed, variant);

        if (controlledCharacterId is not null && world.Find(controlledCharacterId) is null)
            throw new ArgumentException(
                $"no such character '{controlledCharacterId}' in this scenario", nameof(controlledCharacterId));

        string viewpoint = viewpointCharacterId
            ?? controlledCharacterId
            ?? throw new ArgumentException(
                "a session with no controlled character still needs a viewpoint character — there is " +
                "no omniscient view to fall back on.",
                nameof(viewpointCharacterId));

        if (world.Find(viewpoint) is null)
            throw new ArgumentException(
                $"no such character '{viewpoint}' in this scenario", nameof(viewpointCharacterId));

        return new SimulationSession(world, seed, variant, controlledCharacterId, viewpoint);
    }

    public int Seed { get; }
    public string Variant { get; }

    /// <summary>Whose deliberations stop for a choice, or null when nobody is controlled.</summary>
    public string? ControlledCharacterId => _controlledId;

    /// <summary>Whose knowledge <see cref="Snapshot"/> is limited to.</summary>
    public string ViewpointCharacterId { get; }

    /// <summary>The date the scenario opens on, so an interface can express a span without knowing
    /// anything about the fixture.</summary>
    public DateTime StartedOn { get; }

    /// <summary>The player-facing date.</summary>
    public DateTime Date => _clock;

    public SessionStatus Status => _pending is null ? SessionStatus.Ready : SessionStatus.AwaitingChoice;

    /// <summary>The decision waiting on the player, or null.</summary>
    public PendingDecision? Pending => _pending;

    /// <summary>
    /// The world as the viewpoint character could relate it, at the current date.
    ///
    /// Rebuilt on each call rather than cached, because it is a picture of a moving thing — and
    /// returned as an immutable record, so holding on to an old one is a stale view rather than a
    /// window that quietly widens.
    /// </summary>
    public PlayerSnapshot Snapshot() => PlayerView.Build(_world, ViewpointCharacterId, _clock);

    // ---------------------------------------------------------------- advancing time
    /// <summary>
    /// Handles the next scheduled event, whenever it is.
    ///
    /// Deliberately unbounded: the point of a discrete-event calendar is that the next thing to
    /// happen is the next thing to happen, and a "next event" control that refused to cross midnight
    /// would be a tick with extra steps. It clears any outstanding fast-forward, so a choice made
    /// after a single step does not resume a week the player is no longer asking for.
    /// </summary>
    public void StepEvent()
    {
        RequireReady();
        _runUntil = null;
        Pump(DateTime.MaxValue, oneEventOnly: true);
    }

    /// <summary>Runs the calendar forward by whole days from the current date.</summary>
    public void AdvanceDays(int days)
    {
        if (days < 1) throw new ArgumentOutOfRangeException(nameof(days), days, "advance at least one day");
        AdvanceTo(_clock.AddDays(days));
    }

    /// <summary>
    /// Runs every scheduled event up to <paramref name="horizon"/>, stopping early only for a
    /// choice.
    ///
    /// A horizon at or before the current date is a no-op rather than an error: it is what "advance
    /// to a date already reached" means, and the queue is not consulted, so it cannot move anything.
    /// </summary>
    public void AdvanceTo(DateTime horizon)
    {
        RequireReady();
        if (horizon <= _clock) return;
        _runUntil = horizon;
        Pump(horizon, oneEventOnly: false);
    }

    // ---------------------------------------------------------------- choosing
    /// <summary>
    /// Commits the controlled character to one of the options that were open to him, then resumes
    /// whatever fast-forward was interrupted.
    ///
    /// The commitment runs through <see cref="Pipeline.Resolve"/> and therefore through
    /// <see cref="Commit"/> — the same code, in the same order, with the same consequences, as when
    /// an NPC chooses. There is no player action implementation and no player-only branch anywhere
    /// beneath this call.
    ///
    /// An id that names something not open to him throws, and throws before anything is mutated, so
    /// a rejected choice leaves the session exactly where it was and the player can choose again.
    /// </summary>
    public void Choose(string optionId)
    {
        if (_prepared is not { } prepared)
            throw new InvalidOperationException(
                "nothing is waiting on a choice; the controlled character is not mid-deliberation.");

        // Resolve validates the id against the candidates that survived his own filters and throws
        // before touching any state. Nulling the pending decision only after it returns is what
        // makes a rejected choice recoverable rather than wedging the session.
        Pipeline.Resolve(prepared, optionId);

        _prepared = null;
        _pending = null;
        Resume();
    }

    /// <summary>
    /// Commits the controlled character to whichever option he himself would have preferred.
    ///
    /// <b>Internal on purpose.</b> This is the autonomous path, and it exists so a test can drive a
    /// controlled session through the prepare/resolve boundary and compare the result against the
    /// accepted batch history. Handing it to an interface would let a player read the model's
    /// preference by pressing a button, which is a utility score delivered one bit at a time.
    /// </summary>
    internal void ResolveAutomatically()
    {
        if (_prepared is not { } prepared)
            throw new InvalidOperationException(
                "nothing is waiting on a choice; the controlled character is not mid-deliberation.");

        Pipeline.Resolve(prepared, null);

        _prepared = null;
        _pending = null;
        Resume();
    }

    /// <summary>
    /// The running world. <b>Internal — see the type header.</b> Visible to the test assembly alone,
    /// so that the byte-identity and attribution checks can compare against the batch simulation.
    /// </summary>
    internal World World => _world;

    // ---------------------------------------------------------------- the loop
    private void Pump(DateTime until, bool oneEventOnly)
    {
        while (true)
        {
            var step = Runner.Step(_world, until, _controlledId);

            if (step.Status == StepStatus.AwaitingChoice)
            {
                _prepared = step.Awaiting;
                _pending = Project(step.Awaiting!);
                Reached(_world.Now);
                return;
            }

            if (step.Status == StepStatus.Exhausted) break;

            Reached(_world.Now);
            if (oneEventOnly) return;
        }

        // Nothing left before the horizon. The remaining days are genuinely empty, so the calendar
        // reaches the date the player asked for and the fast-forward is discharged.
        if (!oneEventOnly && _runUntil is { } horizon)
        {
            Reached(horizon);
            _runUntil = null;
        }
    }

    private void Resume()
    {
        if (_runUntil is { } horizon) Pump(horizon, oneEventOnly: false);
    }

    private void Reached(DateTime at)
    {
        if (at > _clock) _clock = at;
    }

    private void RequireReady()
    {
        if (_pending is not null)
            throw new InvalidOperationException(
                $"{_pending.ActorName} is mid-decision. Time cannot move until the choice is made — " +
                "resolving later events around a half-handled one would make the history depend on " +
                "how long somebody took to answer.");
    }

    /// <summary>
    /// The player-facing projection of a stopped deliberation. Reads
    /// <see cref="PreparedDecision.Available"/> and three descriptive strings, and nothing else —
    /// see <see cref="PendingDecision"/> for what each is and why it is admissible.
    /// </summary>
    private static PendingDecision Project(PreparedDecision prepared) => new(
        prepared.At,
        prepared.Actor.Id,
        prepared.Actor.Name,
        prepared.Actor.RoleTitle,
        prepared.Trigger.Cause,
        prepared.Agenda.Description,
        prepared.Available
            .Select(c => new PendingOption(c.Id, c.Description))
            .ToList());
}
