namespace CrimeSim.Domain;

/// <summary>
/// How honest the sender was *trying* to be.
///
/// This records intent, not accuracy, and the distinction is load-bearing:
/// INFORMATION_AND_LEGIBILITY.md's open questions ask how lies are distinguished from sincere false
/// beliefs in development traces. A Candid report can still assert something false — the sender was
/// simply wrong — and a False report can accidentally land on the truth. Judging honesty by
/// comparing the report against world truth would collapse exactly the distinction this exists to
/// preserve, so nothing may derive Candor from the truth log.
/// </summary>
public enum ReportCandor
{
    /// <summary>Everything relevant he holds, at the confidence he holds it.</summary>
    Candid,

    /// <summary>True as far as it goes. The incriminating part is simply not mentioned.</summary>
    Partial,

    /// <summary>
    /// He asserts the opposite of something he holds.
    ///
    /// "Something he holds" is the whole of it. A man who has come to reject a claim and says so is
    /// being candid about a rejection, not lying — there is nothing for the assertion to
    /// contradict. Producing this candour for a position the sender does not hold would file a
    /// sincere denial as deception and collapse the lie/mistaken-belief distinction this enum
    /// exists to keep.
    /// </summary>
    False,
}

/// <summary>
/// One proposition as asserted by a sender — not as the sender privately holds it.
///
/// The split matters: a character reporting falsely asserts a stance and confidence that differ
/// from his own belief, and both halves have to be representable for the lie to be a lie rather
/// than a second opinion.
///
/// Basis is split in two, and the split is the whole point.
///
/// <see cref="ClaimedBasis"/> is what the account presents itself as — the only thing the listener
/// may act on, because it is the only thing he was actually offered. <see cref="ActualBasis"/> is
/// how the speaker really came by it, and is developer truth: it lives in the report log and must
/// never reach the recipient's cognition.
///
/// Collapsing them leaked. A man denying his own beating transmitted `Participant`, the listener
/// recorded first-hand testimony, and the denial thereby announced the very participation it was
/// denying — the concealment defeating itself through a field nobody was looking at. A liar
/// discloses a bare assertion unless he chooses to claim presence; what he privately has stays
/// private.
/// </summary>
/// Neither basis is a positional parameter, and that is deliberate. When both defaulted, supplying
/// only the claimed one — the natural thing to write — left the actual one silently at `Report`, so
/// an honest participant briefing his own man came out flagged as a misrepresentation. A field
/// whose wrong value is what you get by not thinking about it is a trap. Set them together through
/// <see cref="Honest"/> or <see cref="Misrepresenting"/>, or name them both.
public readonly record struct ReportedClaim(
    Claim Claim,
    Stance AssertedStance,
    double AssertedConfidence)
{
    /// <summary>What the account presents itself as. The only basis the listener may act on.</summary>
    public SourceKind ClaimedBasis { get; init; } = SourceKind.Report;

    /// <summary>How the speaker really came by it. Developer truth; never reaches the recipient.</summary>
    public SourceKind ActualBasis { get; init; } = SourceKind.Report;

    /// <summary>True when the speaker is presenting a basis other than the one he has.</summary>
    public bool BasisIsMisrepresented => ClaimedBasis != ActualBasis;

    /// <summary>A man saying what he has, the way he has it. Claimed and actual are the same.</summary>
    public static ReportedClaim Honest(Claim claim, Stance stance, double confidence, SourceKind basis)
        => new(claim, stance, confidence) { ClaimedBasis = basis, ActualBasis = basis };

    /// <summary>
    /// A man presenting a basis other than his own — typically a denial, which discloses nothing
    /// about how he would know.
    ///
    /// Degenerates to <see cref="Honest"/> when the two coincide, which is correct: a man whose
    /// real basis was already a report gives nothing away by claiming one.
    /// </summary>
    public static ReportedClaim Misrepresenting(
        Claim claim, Stance stance, double confidence, SourceKind claimed, SourceKind actual)
        => new(claim, stance, confidence) { ClaimedBasis = claimed, ActualBasis = actual };

    public override string ToString() => $"{AssertedStance} {Claim}";
}

/// <summary>
/// One character having asked another to account for one particular thing.
///
/// Recorded because asking is an act with a cost and a consequence, not a poll. Without it,
/// "have I asked this man yet" could only be answered by looking for his reply — so a request
/// that went unanswered stayed invisible, and the asker put the same question again every time
/// he was woken. A question is spent when it is asked, not when it is answered; whether the
/// other man chooses to say anything is his decision, and silence is itself an answer.
///
/// <see cref="About"/> is what keeps that from becoming a permanent silence. A request scoped only
/// to a pair of people meant that having once asked a man about a shakedown, you could never ask
/// him about anything again for the rest of the simulation — one question closing the channel
/// between two characters for good. What is spent is the question, not the relationship.
/// </summary>
public sealed record InformationRequest(long Id, string AskerId, string AskedId, Claim About, DateTime At)
{
    public override string ToString() => $"{AskerId} asked {AskedId} about {About} ({At:yyyy-MM-dd})";
}

/// <summary>
/// One message through the organisational report channel, modelling
/// INFORMATION_AND_LEGIBILITY.md's "Report or Message" contract.
///
/// DEVELOPER TRUTH. <see cref="Withheld"/> in particular is the set of claims the sender held and
/// chose not to pass on; the doc requires development tools be able to see omitted facts, and
/// equally requires that the player-facing layer never see them. Nothing under Runner/ may render
/// this type — the player sees its effects on the recipient's cognition, not the record itself.
/// </summary>
public sealed record Report(
    long Id,
    string SenderId,
    string RecipientId,
    DateTime At,
    ReportCandor Candor,
    IReadOnlyList<ReportedClaim> Asserted,
    IReadOnlyList<Claim> Withheld,
    string Framing)
{
    public override string ToString()
        => $"{SenderId} -> {RecipientId} [{Candor}] {Asserted.Count} asserted, {Withheld.Count} withheld";
}
