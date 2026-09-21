namespace CrimeSim.Session;

using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Sim;

/// <summary>Whether the session can advance, is waiting on a choice, or has resolved.</summary>
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

    /// <summary>The fixed scenario deadline has been drained and its objective evaluated.</summary>
    Resolved,
}

/// <summary>
/// The boundary an interface talks to. Engine-neutral by construction: nothing in this file, or
/// anything it returns, names a Godot type, a console type, or a file.
///
/// <b>What it is for.</b> A player needs four things the batch runner never had to provide — a
/// clock he can move in the increments he chooses, a stopping point when the character he controls
/// has a decision to make, a picture of the world limited to what that character could know, and an
/// out-of-fiction beginning and end for this bounded scenario.
///
/// <b>What it deliberately does not supply.</b> <see cref="World"/> is <c>internal</c>, so a UI
/// cannot reach the truth log, the decision records, the report log, the organisation's conditions,
/// or anybody else's cognition through this object at all — not by discipline but because the type
/// system will not name it. In-fiction state comes out only through <see cref="PlayerSnapshot"/> and
/// <see cref="PendingDecision"/>, both immutable and both built from the viewpoint or controlled
/// character's own state. <see cref="SessionObjective"/> and <see cref="SessionResult"/> are separate,
/// immutable scenario metadata: the former is always public and the latter is only a one-bit outcome.
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
    private SessionResult? _result;

    /// <summary>
    /// Opaque option token to candidate id, for the decision currently in front of the player.
    ///
    /// Rebuilt with each pause and never outlives one, so a token from an earlier decision cannot be
    /// applied to a later one.
    /// </summary>
    private readonly Dictionary<string, string> _optionIds = new(StringComparer.Ordinal);

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
        Objective = new SessionObjective(
            "Bring the harbour shortfall under control",
            Cast.Start.AddDays(90));
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

    /// <summary>
    /// The scenario's out-of-fiction objective, available before time advances. It is not part of
    /// <see cref="PlayerSnapshot"/> because seeing the demo's brief does not put it into a character's
    /// cognition.
    /// </summary>
    public SessionObjective Objective { get; }

    /// <summary>The player-facing date.</summary>
    public DateTime Date => _clock;

    public SessionStatus Status => _result is not null
        ? SessionStatus.Resolved
        : _pending is null ? SessionStatus.Ready : SessionStatus.AwaitingChoice;

    /// <summary>
    /// The one-bit scenario result after resolution, or null while the session is still running.
    /// No live revenue condition or progress measure crosses this boundary.
    /// </summary>
    public SessionResult? Result => _result;

    /// <summary>The decision waiting on the player, or null.</summary>
    public PendingDecision? Pending => _pending;

    /// <summary>
    /// The world as the viewpoint character could relate it, at the current date.
    ///
    /// Rebuilt on each call rather than cached, because it is a picture of a moving thing — and
    /// returned as an immutable record, so holding on to an old one is a stale view rather than a
    /// window that quietly widens.
    /// </summary>
    /// <summary>
    /// The snapshot speaks to the player as "you" when the viewpoint is the character he controls,
    /// and about "him" or "her" when it is somebody being watched — milestone 025. A voice, not a
    /// widening: the same fields, worded for whoever is reading.
    /// </summary>
    public PlayerSnapshot Snapshot() => PlayerView.Build(_world, ViewpointCharacterId, _clock, Voice);

    private Pronouns? Voice => _controlledId == ViewpointCharacterId ? PlayerView.You : null;

    // ---------------------------------------------------------------- advancing time
    /// <summary>
    /// Handles the next scheduled event within the bounded scenario.
    ///
    /// It remains event-driven rather than day-bounded — the next thing can be weeks away — but the
    /// fixed scenario deadline is its maximum horizon. It clears any outstanding fast-forward, so a
    /// choice made after a single step does not resume a week the player is no longer asking for.
    /// </summary>
    public void StepEvent()
    {
        RequireReady();
        _runUntil = null;
        Pump(Objective.Deadline, oneEventOnly: true);
    }

    /// <summary>Request an owner review, through the same occasion used by autonomous supervisors.</summary>
    public void ReviewOperation(string operationToken)
    {
        RequireReady();
        if (_controlledId is null || _controlledId != ViewpointCharacterId)
            throw new InvalidOperationException("Review requires control of the viewpoint character.");
        var actor = _world.Get(_controlledId);
        var operation = actor.Execution.Operations.SingleOrDefault(s => $"work-{s.LocalSequence}" == operationToken)
            ?? throw new ArgumentException("This operation is not an active order of yours.", nameof(operationToken));
        Strategy.Strategies.ScheduleReview(_world, operation, _clock);
        _runUntil = null;
        Pump(_clock, oneEventOnly: false);
    }

    /// <summary>Runs the calendar forward by whole days from the current date.</summary>
    public void AdvanceDays(int days)
    {
        RequireUnresolved();
        if (days < 1) throw new ArgumentOutOfRangeException(nameof(days), days, "advance at least one day");

        // Clamp before DateTime arithmetic so even an intentionally huge fast-forward reaches the
        // scenario boundary instead of overflowing on a date the session is not allowed to reach.
        double daysRemaining = (Objective.Deadline - _clock).TotalDays;
        AdvanceTo(days >= daysRemaining ? Objective.Deadline : _clock.AddDays(days));
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
        _runUntil = horizon > Objective.Deadline ? Objective.Deadline : horizon;
        Pump(_runUntil.Value, oneEventOnly: false);
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
    /// A token that names nothing open to him throws, and throws before anything is mutated, so a
    /// rejected choice leaves the session exactly where it was and the player can choose again.
    ///
    /// <paramref name="optionId"/> is the opaque token from <see cref="PendingOption.Id"/>, not a
    /// candidate id — see <see cref="PendingOption"/> for why the candidate id does not cross the
    /// boundary. The token is translated back here and the translated id is then put to
    /// <see cref="Pipeline.Resolve"/>, which remains the sole authority on whether an action was open
    /// to him: this method never decides that question and never falls back to a default.
    /// </summary>
    public void Choose(string optionId)
    {
        RequireUnresolved();
        if (_prepared is not { } prepared)
            throw new InvalidOperationException(
                "nothing is waiting on a choice; the controlled character is not mid-deliberation.");

        if (!_optionIds.TryGetValue(optionId, out string? candidateId))
            throw new SimulationInvariantException(
                $"'{optionId}' is not one of the options {prepared.Actor.Id} was offered at " +
                $"{prepared.At:O}. A choice names an option from the pending decision it answers.");

        // Resolve validates the candidate against the ones that survived his own filters and throws
        // before touching any state. Clearing the pending decision only after it returns is what
        // makes a rejected choice recoverable rather than wedging the session.
        Pipeline.Resolve(prepared, candidateId);

        ClearPending();
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
        RequireUnresolved();
        if (_prepared is not { } prepared)
            throw new InvalidOperationException(
                "nothing is waiting on a choice; the controlled character is not mid-deliberation.");

        Pipeline.Resolve(prepared, null);

        ClearPending();
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
                // A pause is always the controlled character's own, so the decision is put to the
                // player in the second person whoever the viewpoint is.
                _pending = Project(
                    step.Awaiting!, _optionIds, PlayerView.NameIn(_world), PronounsIn(_world), PlayerView.You,
                    id => _world.Org.Assignments.FirstOrDefault(a => a.Id == id));
                Reached(_world.Now);

                // A single-step call normally has no outstanding horizon. At the deadline the
                // choice is nevertheless part of draining that inclusive boundary, so remember it:
                // after the answer, Resume must process every same-instant consequence before the
                // result can be evaluated.
                if (_world.Now == Objective.Deadline && _runUntil is null)
                    _runUntil = Objective.Deadline;
                return;
            }

            if (step.Status == StepStatus.Exhausted) break;

            Reached(_world.Now);
            if (oneEventOnly && _world.Now < Objective.Deadline) return;
        }

        // Nothing left before the horizon. The remaining days are genuinely empty, so the calendar
        // reaches the date the player asked for and the fast-forward is discharged.
        if (!oneEventOnly && _runUntil is { } horizon)
        {
            Reached(horizon);
            _runUntil = null;
        }

        // StepEvent has no run-until value, but when no event remains at or before the deadline its
        // bounded "next" is the boundary itself. Advance the player calendar there and resolve.
        if (until == Objective.Deadline)
        {
            Reached(Objective.Deadline);
            ResolveAtDeadline();
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
        RequireUnresolved();
        if (_pending is not null)
            throw new InvalidOperationException(
                $"{_pending.ActorName} is mid-decision. Time cannot move until the choice is made — " +
                "resolving later events around a half-handled one would make the history depend on " +
                "how long somebody took to answer.");
    }

    private void RequireUnresolved()
    {
        if (_result is not null)
            throw new InvalidOperationException(
                "the session has resolved; no further simulation input is allowed.");
    }

    /// <summary>
    /// Evaluates the one scenario objective only after <see cref="Pump"/> has established that no
    /// event remains at or before the inclusive deadline. Reading authoritative organization state
    /// here determines the out-of-fiction result; it writes nothing back into the world or anybody's
    /// cognition.
    /// </summary>
    private void ResolveAtDeadline()
    {
        if (_result is not null || _pending is not null || _clock < Objective.Deadline) return;

        var outcome = _world.Org.Condition(Org.OrgCondition.RevenueLoss)
            < Org.Organization.SignificantRevenueLoss
            ? ObjectiveOutcome.ObjectiveMet
            : ObjectiveOutcome.ObjectiveUnmet;

        _runUntil = null;
        _result = new SessionResult(outcome, Objective.Deadline);
    }

    private void ClearPending()
    {
        _prepared = null;
        _pending = null;
        _optionIds.Clear();
    }

    /// <summary>
    /// How to refer to somebody, which is public knowledge in the same way a display name is.
    /// Falls back to the same default <see cref="Character.Pronouns"/> carries, so an id that names
    /// no person behaves exactly as everything did before pronouns existed.
    /// </summary>
    private static Func<string, Pronouns> PronounsIn(World world)
        => id => world.Find(id)?.Pronouns ?? Pronouns.He;

    /// <summary>
    /// The player-facing projection of a stopped deliberation.
    ///
    /// <b>Nothing authored by a generator or a scheduler crosses here.</b> The occasion and the focus
    /// come from <see cref="PlayerOccasion"/>'s closed vocabulary and are null when nothing can
    /// honestly be said; each option's wording comes from <see cref="PlayerOption"/>, built from the
    /// candidate's typed fields; and each option's id is an opaque token rather than the candidate id.
    /// The three things that used to pass through verbatim — <c>Trigger.Cause</c>,
    /// <c>Agenda.Description</c> and <c>Candidate.Description</c> — were all developer text, and the
    /// first two carried a delegated operation's outcome to an owner nobody had told.
    ///
    /// Internal rather than private so a staged test can drive it directly with a world it built,
    /// which is the only way to exercise the delegated-outcome case deterministically.
    /// </summary>
    internal static PendingDecision Project(
        PreparedDecision prepared,
        IDictionary<string, string> optionIds,
        Func<string, string> name,
        Func<string, Pronouns> pronouns,
        Pronouns? voice = null,
        Func<long, Org.Assignment?>? assignment = null)
    {
        var self = voice ?? prepared.Actor.Pronouns;

        optionIds.Clear();

        var options = new List<PendingOption>(prepared.Available.Count);
        foreach (var candidate in prepared.Available)
        {
            string token = Token(candidate.Id);

            // Fail closed. Two candidates sharing a token would make one of them unchoosable and the
            // other choosable under somebody else's name; 48 bits makes it vanishingly unlikely and
            // silence about it would make it undiagnosable.
            if (!optionIds.TryAdd(token, candidate.Id))
                throw new SimulationInvariantException(
                    $"option token collision at {prepared.Actor.Id}'s decision: '{candidate.Id}' and " +
                    $"'{optionIds[token]}' both hash to '{token}'.");

            options.Add(new PendingOption(
                token, PlayerOption.Describe(candidate, name, self, pronouns, prepared.Actor.Id)));
        }

        // These are already-visible concrete options, not a second read of world truth. Passing
        // their distinct targets lets the briefing connect the assignment to an undisclosed refusal
        // the actor already knows, without naming a business that did not actually occur to him.
        var relevantAssignmentTargets = prepared.Available
            .Where(c => c.Kind == ActionKind.StartStrategy
                        && c.Strategy == StrategyKind.SecureTribute
                        && c.Domain == prepared.Agenda.Domain
                        && c.TargetId is not null)
            .Select(c => c.TargetId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new PendingDecision(
            prepared.At,
            prepared.Actor.Id,
            prepared.Actor.Name,
            prepared.Actor.RoleTitle,
            self,
            PlayerOccasion.For(prepared.Trigger, prepared.Actor, name, self),
            PlayerOccasion.Focus(
                prepared.Actor, prepared.Agenda, prepared.Trigger, name, self, assignment, pronouns,
                relevantAssignmentTargets),
            options);
    }

    /// <summary>
    /// A stable, meaningless handle for a candidate.
    ///
    /// Deterministic, because identical inputs must produce identical runs and a token the player
    /// pressed has to mean the same thing on a replay. Opaque, because the candidate id is developer
    /// data — it embeds <c>Claim.ToString()</c>, which prints the truth-log <c>EventId</c>.
    /// </summary>
    private static string Token(string candidateId)
        => Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(candidateId)))[..12].ToLowerInvariant();
}
