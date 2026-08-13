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

    /// <summary>He asserts the opposite of something he holds.</summary>
    False,
}

/// <summary>
/// One proposition as asserted by a sender — not as the sender privately holds it.
///
/// The split matters: a character reporting falsely asserts a stance and confidence that differ
/// from his own belief, and both halves have to be representable for the lie to be a lie rather
/// than a second opinion.
/// </summary>
public readonly record struct ReportedClaim(Claim Claim, Stance AssertedStance, double AssertedConfidence)
{
    public override string ToString() => $"{AssertedStance} {Claim}";
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
