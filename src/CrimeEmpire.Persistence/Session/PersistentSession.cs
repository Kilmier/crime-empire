using CrimeSim.Session;

namespace CrimeEmpire.Persistence.Session;

/// <summary>
/// A <see cref="SimulationSession"/> that remembers every successful input made against it, and can
/// write that memory to — or rebuild itself from — one SQLite save via <see cref="SaveStore"/>.
///
/// <b>What this adds over the session it wraps: recording and replay, nothing else.</b> Every public
/// member below either passes straight through to an inner <see cref="SimulationSession"/> or logs
/// one <see cref="SessionCommand"/> after a passthrough call returns normally. No new decision or
/// information channel is added: in-fiction state still comes only through <see cref="Snapshot"/>
/// and <see cref="Pending"/>, while <see cref="Objective"/> and <see cref="Result"/> pass through the
/// session's separate out-of-fiction metadata. Nothing about <c>World</c> is reachable from here:
/// the inner session is exposed only <c>internal</c>, to the test assembly, the same treatment
/// <see cref="SimulationSession.World"/> already gets.
///
/// <b>Loading replays rather than restores.</b> <see cref="Load"/> starts a genuinely fresh
/// <see cref="SimulationSession"/> from the save's own seed and variant and drives every recorded
/// command back through the session's real public mutators, in order — the same calls a live session
/// received the first time, not a deserialized copy of anything internal. Milestone 015 ruling 2:
/// this is what makes the reconstruction exact rather than approximate, and what keeps
/// <c>CrimeEmpire.Simulation</c> free of any notion that it is ever being saved at all.
/// </summary>
public sealed class PersistentSession
{
    private readonly SimulationSession _session;
    private readonly List<SessionCommand> _log;

    private PersistentSession(SimulationSession session, List<SessionCommand> log)
    {
        _session = session;
        _log = log;
    }

    public static PersistentSession Start(
        int seed,
        string variant,
        string? controlledCharacterId,
        string? viewpointCharacterId = null)
    {
        var session = SimulationSession.Start(seed, variant, controlledCharacterId, viewpointCharacterId);
        return new PersistentSession(session, new List<SessionCommand>());
    }

    public int Seed => _session.Seed;
    public string Variant => _session.Variant;
    public string? ControlledCharacterId => _session.ControlledCharacterId;
    public string ViewpointCharacterId => _session.ViewpointCharacterId;
    public DateTime StartedOn => _session.StartedOn;
    public DateTime Date => _session.Date;
    public SessionStatus Status => _session.Status;
    public PendingDecision? Pending => _session.Pending;
    public SessionObjective Objective => _session.Objective;
    public SessionResult? Result => _session.Result;

    public PlayerSnapshot Snapshot() => _session.Snapshot();

    public void StepEvent()
    {
        _session.StepEvent();
        _log.Add(SessionCommand.StepEvent());
    }

    public void AdvanceDays(int days)
    {
        _session.AdvanceDays(days);
        _log.Add(SessionCommand.AdvanceDays(days));
    }

    public void Choose(string optionId)
    {
        _session.Choose(optionId);
        _log.Add(SessionCommand.Choose(optionId));
    }

    public void ReviewOperation(string token)
    {
        _session.ReviewOperation(token);
        _log.Add(SessionCommand.ReviewOperation(token));
    }

    /// <summary>
    /// Writes the complete history of this session — meta plus every successful input so far — to
    /// <paramref name="path"/>. Works while ready, awaiting a choice, or resolved (milestone 027
    /// ruling 6): saving records inputs already made, not a copy of transient world state, and
    /// <see cref="Load"/> reaching the identical pause or terminal result is a consequence of
    /// replaying those same inputs.
    /// </summary>
    public void Save(string path)
    {
        var data = new SaveData(
            SaveStore.CurrentSchemaVersion,
            SimulationBuild.CurrentId,
            Seed,
            Variant,
            ControlledCharacterId,
            ViewpointCharacterId,
            _log);

        SaveStore.Write(path, data);
    }

    /// <summary>
    /// Rebuilds a session from <paramref name="path"/> by starting fresh and replaying its command
    /// log. A save that fails <see cref="SaveStore"/>'s own schema/build checks, or whose replay
    /// rejects a command — an option token nothing at that pause offers, most obviously — throws
    /// rather than falling back to any partial or default state. See <see cref="SaveFormatException"/>.
    /// </summary>
    public static PersistentSession Load(string path)
    {
        var data = SaveStore.Read(path);
        var session = SimulationSession.Start(data.Seed, data.Variant, data.ControlledCharacterId, data.ViewpointCharacterId);
        var log = new List<SessionCommand>(data.Commands.Count);

        for (int i = 0; i < data.Commands.Count; i++)
        {
            var command = data.Commands[i];
            try
            {
                Apply(session, command);
            }
            catch (Exception ex)
            {
                throw new SaveFormatException(
                    $"'{path}' failed to replay at ordinal {i} ({command.Kind}) — {ex.Message}", ex);
            }

            log.Add(command);
        }

        return new PersistentSession(session, log);
    }

    private static void Apply(SimulationSession session, SessionCommand command)
    {
        switch (command.Kind)
        {
            case SessionCommandKind.StepEvent:
                session.StepEvent();
                break;
            case SessionCommandKind.AdvanceDays:
                session.AdvanceDays(command.Days!.Value);
                break;
            case SessionCommandKind.Choose:
                session.Choose(command.OptionToken!);
                break;
            case SessionCommandKind.ReviewOperation:
                session.ReviewOperation(command.OptionToken!);
                break;
            default:
                throw new SaveFormatException($"unrecognised command kind '{command.Kind}' during replay.");
        }
    }

    /// <summary>
    /// The wrapped session. <b>Internal — see the type header.</b> Visible to the test assembly alone,
    /// so the exact-internal-state comparisons ruling 7 requires can reach it; nothing in
    /// <c>CrimeEmpire.Godot</c> or any other consumer of this type ever can.
    /// </summary>
    internal SimulationSession InnerSession => _session;
}
