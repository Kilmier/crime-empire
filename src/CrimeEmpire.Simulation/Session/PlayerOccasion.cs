namespace CrimeSim.Session;

using CrimeSim.Decision;
using CrimeSim.Sim;

/// <summary>
/// Why the controlled character is thinking about this now — said in a closed vocabulary this file
/// owns, and never in the words the scheduler used.
///
/// <b>What was wrong before, because it is the whole reason this file exists.</b> Milestone 009
/// shipped with <c>PendingDecision.Occasion</c> set to <see cref="ScheduledEvent.Cause"/> directly,
/// on a ruling that argued every deliberation-waking cause is authored from the waking character's
/// own side. That claim was false for two event kinds, and falsely stated about all of them — which
/// is this project's signature defect, made this time by the person writing the ruling that named it.
///
/// <see cref="Strategies.Blocked"/> schedules <see cref="EventKind.StrategyBlocked"/> addressed to
/// the strategy's <em>owner</em> with the cause <c>"Bellini's grocery held out against force"</c>,
/// and <see cref="Strategies.Complete"/> schedules <see cref="EventKind.StrategyComplete"/> with
/// <c>"… finished: the cleanup made things worse"</c>. When the work was delegated, the owner was not
/// there, nobody has told him, and no discovery roll has been made. Those sentences are the
/// executor's operational outcome, and handing them to the owner is precisely the "authority
/// delivers knowledge" leak that <c>Strategies.ResolveViolence</c> has a long comment refusing to
/// commit.
///
/// <b>The rule now.</b> Nothing authored by a scheduler crosses the boundary at all. The occasion is
/// chosen from a fixed list keyed on the event kind, and <b>the default is silence</b> — a kind that
/// is not named below produces no occasion, so a future event kind is mute until somebody decides
/// what a character could honestly be said to know about it. Fail-closed rather than fail-open is
/// the difference between this and the ruling it replaces.
///
/// <b>Why the two strategy-outcome kinds are silent rather than conditional.</b> A test for "did he
/// execute it himself" is available for <see cref="EventKind.StrategyBlocked"/>, where the instance
/// is still live, and not for <see cref="EventKind.StrategyComplete"/>, where
/// <see cref="Strategies.Complete"/> has already cleared it by the time the event is handled. Two
/// rules for one question is how the distinction gets dropped on the way from one to the other. One
/// rule, and it is silence.
/// </summary>
internal static class PlayerOccasion
{
    /// <summary>
    /// What woke him, in terms that assert nothing beyond the fact that he was woken.
    ///
    /// Null means the interface says nothing. Each phrase below is admissible because the event kind
    /// itself establishes it for the character it is addressed to:
    ///
    ///  - <see cref="EventKind.AssignmentDelivered"/> — he has just been briefed, through
    ///    <c>Cognition.Receive</c>, by the man who issued it.
    ///  - <see cref="EventKind.RoleReview"/> — a periodic look at his own patch, or somebody having
    ///    spoken to him. Every scheduler of this kind addresses it to the person the speaking was
    ///    done to; none of them carries a third party's outcome. The phrase says only that he came
    ///    back round to it, which is true of all of them.
    ///  - <see cref="EventKind.Incident"/> — scheduled by <c>Runner.Observe</c> only after the
    ///    observer actually acquired something, so "something reached him" is established by the
    ///    event's own precondition.
    ///  - <see cref="EventKind.PressureThreshold"/> — his own pressure, crossed in his own head.
    /// </summary>
    internal static string? For(EventKind kind) => kind switch
    {
        EventKind.AssignmentDelivered => "he has just been handed something to do",
        EventKind.RoleReview => "he came back round to his own patch",
        EventKind.Incident => "something reached him",
        EventKind.PressureThreshold => "something had got hard to ignore",

        // StrategyComplete and StrategyBlocked, and anything added later. Silence is the default and
        // must stay the default: adding a kind here is a claim that the character necessarily knows
        // why he is thinking, and that claim has already been wrong once.
        _ => null,
    };

    /// <summary>
    /// What is on his mind, from the agenda — and only where the agenda's own description is
    /// structurally his.
    ///
    /// <see cref="AgendaKind.RespondToTrigger"/> is excluded because
    /// <see cref="AgendaSelection.Select"/> sets its <c>Description</c> to <c>trigger.Cause</c>
    /// verbatim. That is the same string this file exists to keep out, arriving through a second
    /// field — which is exactly the "distinction drawn in one place and dropped on the way to the
    /// next" shape, and it is why this is a suppression list rather than a sanitising pass over one
    /// property.
    ///
    /// The four kinds that do pass are each structurally the character's own:
    /// <see cref="AgendaKind.FulfilAssignment"/> is the objective he was briefed on,
    /// <see cref="AgendaKind.ContinueCommitment"/> is the course of action he started,
    /// <see cref="AgendaKind.RelievePressure"/> is his own pressure by name, and
    /// <see cref="AgendaKind.DischargeResponsibility"/> is his own standing duty. None can carry
    /// anybody else's outcome, because none is written by anybody else.
    /// </summary>
    internal static string? Focus(Agenda agenda, EventKind kind)
    {
        // A wake we cannot describe is a wake we say nothing about. Otherwise the focus would
        // narrate the same delegated outcome the occasion was suppressed for.
        if (For(kind) is null) return null;

        return agenda.Kind switch
        {
            AgendaKind.FulfilAssignment => agenda.Description,
            AgendaKind.ContinueCommitment => agenda.Description,
            AgendaKind.RelievePressure => agenda.Description,
            AgendaKind.DischargeResponsibility => agenda.Description,
            _ => null,
        };
    }
}
