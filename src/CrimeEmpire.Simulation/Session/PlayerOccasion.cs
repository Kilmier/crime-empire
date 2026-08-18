namespace CrimeSim.Session;

using CrimeSim.Decision;
using CrimeSim.Domain;
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
    /// What woke him, in terms that assert nothing beyond what the event itself establishes for the
    /// character it is addressed to.
    ///
    /// Null means the interface says nothing.
    ///
    /// <b>Keyed on the event's structured note as well as its kind, since milestone 009's fourth
    /// correction.</b> <see cref="EventKind.RoleReview"/> has five schedulers — a periodic look at his
    /// own patch, and four occasions on which somebody has just spoken to him — and one phrase for
    /// all of them told a man he was doing his rounds when his soldier had reported in. That is not a
    /// leak; it is the opposite, and worse for it. It withheld something he certainly knew and put a
    /// specific false reason in its place, and the withheld part is the most decision-relevant
    /// context there is: "somebody has just put a question to you" is precisely why you would answer
    /// it.
    ///
    /// The phrases below name nobody. Whoever spoke is already named in the options — "give Tommy
    /// Nardo his account of…" — so naming him twice buys nothing and widens the surface.
    /// </summary>
    internal static string? For(ScheduledEvent trigger) => trigger.Kind switch
    {
        // He has just been briefed, through Cognition.Receive, by the man who issued it.
        EventKind.AssignmentDelivered => "he has just been handed something to do",

        // Somebody spoke to him, and the note says which act it was. Each is established for him by
        // the act itself: he was the one asked, reported to, or petitioned.
        EventKind.RoleReview => trigger.Payload.Note switch
        {
            "asked-to-account" => "somebody has put a question to him",
            "reported-to" => "somebody has reported to him",
            "permission-sought" => "somebody has asked him for room to move",
            _ => "he came back round to his own patch",
        },

        // Runner.Observe schedules this only after the observer actually acquired something, so the
        // event's own precondition establishes the phrase.
        EventKind.Incident => "something reached him",

        // His own pressure, crossed in his own head.
        EventKind.PressureThreshold => "something had got hard to ignore",

        // StrategyComplete and StrategyBlocked, and anything added later. Silence is the default and
        // must stay the default: adding a kind here is a claim that the character necessarily knows
        // why he is thinking, and that claim has already been wrong once.
        _ => null,
    };

    /// <summary>
    /// What is on his mind — derived from his own state, never passed through from the agenda's
    /// developer-facing description.
    ///
    /// <see cref="AgendaKind.RespondToTrigger"/> is excluded because
    /// <see cref="AgendaSelection.Select"/> sets its <c>Description</c> to <c>trigger.Cause</c>
    /// verbatim, which is the authored string this file exists to keep out.
    ///
    /// <b>Two of the four remaining kinds were passing developer text too, until milestone 009's
    /// fourth correction.</b> <see cref="AgendaKind.ContinueCommitment"/>'s description is
    /// <c>"ongoing: " + StrategyInstance.Label</c>, which put
    /// <c>ConcealIncident(, target=bellini-grocery, method=Persuade)</c> in front of a player — raw
    /// ids, a raw enum, and the empty-domain defect already on the carried-forward list.
    /// <see cref="AgendaKind.RelievePressure"/>'s was <c>"pressure: " + PressureKind</c>. Both are now
    /// phrased here from the typed values, and the strategy phrasing is
    /// <see cref="PlayerOption.Work"/> — the same one the options use, rather than a second wording
    /// of the same thing.
    ///
    /// The two that do pass their description through are prose a person wrote about him and that he
    /// holds: the objective he was briefed on, and his own standing responsibility.
    /// </summary>
    internal static string? Focus(Character actor, Agenda agenda, ScheduledEvent trigger, Func<string, string> name)
    {
        // A wake we cannot describe is a wake we say nothing about. Otherwise the focus would narrate
        // the same delegated outcome the occasion was suppressed for.
        if (For(trigger) is null) return null;

        return agenda.Kind switch
        {
            // The objective he was handed, in the issuer's words, which he was told.
            AgendaKind.FulfilAssignment => agenda.Description,

            // His own standing duty, in the words the scenario gave it.
            AgendaKind.DischargeResponsibility => agenda.Description,

            // The course of action he started, described as his options describe it.
            AgendaKind.ContinueCommitment when actor.Execution.Strategy is { } s =>
                PlayerOption.Work(s.Kind, s.TargetId, name),

            // What is pressing on him, by what it is rather than by its enum name.
            AgendaKind.RelievePressure => actor.Motivations.Dominant() is { } p ? Pressure(p.Kind) : null,

            _ => null,
        };
    }

    /// <summary>
    /// A pressure in the character's own terms. Closed, and silent on anything unnamed — the same
    /// fail-closed default as the occasion vocabulary.
    /// </summary>
    private static string? Pressure(PressureKind kind) => kind switch
    {
        PressureKind.RevenueShortfall => "the money that is not arriving",
        PressureKind.LegalExposure => "how exposed he is",
        PressureKind.Resentment => "what he is carrying against somebody",
        PressureKind.Fear => "what he is afraid of",
        PressureKind.OrganizationalInstability => "how unsteady the outfit has become",
        _ => null,
    };
}
