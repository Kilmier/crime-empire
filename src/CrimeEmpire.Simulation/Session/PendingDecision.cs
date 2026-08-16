namespace CrimeSim.Session;

/// <summary>
/// One thing the controlled character could do, as he would put it to himself.
///
/// <see cref="Id"/> is the candidate id, and it is the token a choice is made with. It is not a
/// ranking, a score, or a position: <see cref="PendingDecision.Options"/> is ordered by this string,
/// deliberately, so that nothing about the order says which one the model would have taken.
/// </summary>
public sealed record PendingOption(string Id, string Description);

/// <summary>
/// A deliberation stopped at the last question, waiting for a person to answer it.
///
/// <b>What is here, and why each thing is allowed.</b>
///
///  - <see cref="Options"/> — the candidates that survived his own redundancy, salience, knowledge,
///    capability and access filters. Authorized explicitly by milestone 009's scope: this is the
///    player standing where the character stands, which is what shared agency means.
///  - <see cref="Occasion"/> — the trigger's cause. Every cause that can wake a *deliberation* is
///    authored from the waking character's own side: he was handed an assignment, somebody reported
///    to him, somebody asked him a question, a shop held out against him, a pressure became hard to
///    ignore. For the character the player controls, that is what just happened to him rather than
///    hidden state. See milestone 009's ruling 7, which flags this as the judgement most worth
///    challenging.
///  - <see cref="Focus"/> — the agenda's <c>Description</c>: the objective he was given, the course
///    of action he is already running, or the pressure by name.
///
/// <b>What is deliberately absent.</b> Every score and score component; the noise draw; the
/// candidates that were rejected and the stage that rejected them; the salience notes; the agenda's
/// <c>Reason</c>, which embeds a numeric pressure value and the developer-facing trigger kind; and
/// any fact the character does not hold. The developer-facing form of all of it is
/// <see cref="CrimeSim.Decision.PreparedDecision"/> and it does not come through here.
/// </summary>
public sealed record PendingDecision(
    DateTime At,
    string ActorId,
    string ActorName,
    string ActorRole,
    string Occasion,
    string Focus,
    IReadOnlyList<PendingOption> Options);
