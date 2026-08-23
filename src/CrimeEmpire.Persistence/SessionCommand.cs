namespace CrimeEmpire.Persistence;

/// <summary>The three ways a save's ordered input log can move a replayed session.</summary>
public enum SessionCommandKind
{
    /// <summary>Maps to <c>SimulationSession.StepEvent()</c>.</summary>
    StepEvent,

    /// <summary>Maps to <c>SimulationSession.AdvanceDays(int)</c>. <see cref="SessionCommand.Days"/> carries the count.</summary>
    AdvanceDays,

    /// <summary>Maps to <c>SimulationSession.Choose(string)</c>. <see cref="SessionCommand.OptionToken"/> carries the opaque token.</summary>
    Choose,
}

/// <summary>
/// One successful call against the session's own public mutators, in the order it was made.
///
/// "Successful" is load-bearing: <see cref="Session.PersistentSession"/> appends one of these only
/// after the underlying <c>SimulationSession</c> call has already returned normally. Every one of
/// those calls throws before mutating anything on a bad input — a decision awaiting an answer that
/// gets an out-of-turn <c>AdvanceDays</c>, a <c>Choose</c> naming an option that was never offered —
/// so a log built this way never contains a step that would fail on replay for a reason replay
/// itself introduced. It can still fail replay if the log was corrupted after the fact, which is
/// exactly the case ruling 7's malformed-input tests are for.
///
/// Nothing here names a candidate id, a score, or anything else <c>PendingDecision</c> would not
/// hand a Godot button. <see cref="OptionToken"/> is the same opaque token
/// <c>PendingOption.Id</c> already is.
/// </summary>
public sealed record SessionCommand(SessionCommandKind Kind, int? Days = null, string? OptionToken = null)
{
    public static SessionCommand StepEvent() => new(SessionCommandKind.StepEvent);

    public static SessionCommand AdvanceDays(int days) => new(SessionCommandKind.AdvanceDays, Days: days);

    public static SessionCommand Choose(string optionToken) => new(SessionCommandKind.Choose, OptionToken: optionToken);
}
