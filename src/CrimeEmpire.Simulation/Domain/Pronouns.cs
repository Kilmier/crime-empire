namespace CrimeSim.Domain;

/// <summary>
/// How a character is referred to in player-facing prose.
///
/// <b>Why this exists.</b> Every player-facing surface said "he". Det. Iris Kane is the only woman
/// in the cast, and her own intelligence view opened <c>WHAT HE HAS</c> and told her that
/// "everything here is something <i>he</i> saw or was told" — a shipping surface stating something
/// false about a person, for the whole of milestones 003 to 010. It was found by looking at the
/// detective for milestone 011 and not by any test, because nothing had ever asserted that a
/// character is described as themselves.
///
/// <b>Scope.</b> Player-facing surfaces only — <see cref="Session.PlayerNarration"/>,
/// <see cref="Session.PlayerOccasion"/>, <see cref="Session.PlayerOption"/>, the console
/// intelligence writer and the Godot panels. The developer trace still says "he" throughout. That is
/// deliberate rather than an oversight: it is a debugging tool that the architecture doc explicitly
/// separates from player-facing accounts, and changing 59 more strings there would move the trace
/// hashes for no player-visible gain. It stays on the carried-forward list.
///
/// <b>Verb agreement is real and is not faked.</b> A pronoun set that produced "they has nothing to
/// decide" would be worse than the defect it replaced, so <see cref="Verb"/> is the mechanism and
/// every present-tense call site uses it. <see cref="They"/> is therefore usable rather than
/// decorative, even though the current cast contains nobody who takes it.
/// </summary>
public sealed record Pronouns(
    string Subject,
    string Object,
    string Possessive,
    string Reflexive,
    bool PluralVerb = false)
{
    public static readonly Pronouns He = new("he", "him", "his", "himself");
    public static readonly Pronouns She = new("she", "her", "her", "herself");
    public static readonly Pronouns They = new("they", "them", "their", "themselves", PluralVerb: true);

    /// <summary>The subject form at the start of a sentence.</summary>
    public string Subject_ => char.ToUpperInvariant(Subject[0]) + Subject[1..];

    /// <summary>
    /// The form of a present-tense verb that agrees with this pronoun.
    ///
    /// Both forms are supplied by the caller rather than derived, because English third-person
    /// agreement is not a suffix rule — "has/have" and "does/do" are not "-s", and guessing would
    /// produce "he haves". A call site that needs agreement has to name both words, which also makes
    /// every such site greppable.
    /// </summary>
    public string Verb(string thirdPersonSingular, string plural) => PluralVerb ? plural : thirdPersonSingular;

    public override string ToString() => $"{Subject}/{Object}";
}
