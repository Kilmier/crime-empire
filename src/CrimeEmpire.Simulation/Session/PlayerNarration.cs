namespace CrimeSim.Session;

using CrimeSim.Domain;

/// <summary>
/// Claims, provenance, confidence and standing rendered in fiction.
///
/// <b>Why this is in the simulation library and not in a renderer.</b> Every function here enforces
/// an information-safety rule rather than a layout preference. Discovery says "came across" and
/// never "saw", because finding a wrecked shopfront the next morning is not witnessing a beating.
/// Confidence is words and never a number, because an exact probability is hidden state wearing a
/// percentage sign. Standing says how far he would go on a man's word and never why, because a
/// relationship that cooled because an account did not match must read identically to one that was
/// never warm. Those are properties of the information model. `AGENTS.md`'s repository boundary
/// keeps *console formatting* out of this library — padding, box drawing, section headers — and all
/// of that stays in `IntelligenceWriter`.
///
/// It lives here specifically so there is one implementation. Milestone 009 added a second surface
/// that shows a character what he knows; two copies of these rules is the shape this project's
/// recurring-failure list calls a distinction drawn in one place and dropped on the way to the next.
///
/// The wording was moved here verbatim from `CrimeEmpire.Runner`'s `IntelligenceWriter`, which now
/// delegates to it, so the accepted no-leak tests keep pinning the same strings.
/// </summary>
public static class PlayerNarration
{
    /// <summary>
    /// The claim as a sentence. In fiction, never as a predicate.
    ///
    /// Public so a no-leak test can compute the exact wording a given claim would produce and assert
    /// its absence from a view. A test that hardcoded the prose would pass while this drifted.
    /// </summary>
    public static string Describe(Claim c, Func<string, string> name) => c.Kind switch
    {
        ClaimKind.BusinessRefusesTribute => $"{name(c.Subject)} is holding back what it owes",
        ClaimKind.PersonUsedViolence => $"{name(c.Subject)} put hands on {name(c.Object)}",
        ClaimKind.PoliceInvestigating => $"the police are looking at {name(c.Subject)}",
        ClaimKind.PersonHoldsGrievance => $"{name(c.Subject)} carries something against {name(c.Object)}",
        ClaimKind.TributeCollected => $"{name(c.Subject)} has paid",
        ClaimKind.WitnessSawIncident => $"somebody on the street saw {name(c.Object)} at {name(c.Subject)}",
        ClaimKind.PolicyIssued => $"the rule \"{c.Object}\" stands",
        ClaimKind.PersonBreachedPolicy => $"{name(c.Subject)} went outside \"{c.Object}\"",
        ClaimKind.TargetIsVulnerable => $"{name(c.Subject)} would not stand up to pressure",
        // Subject is a domain, never a person or a business — name() must not be called on it, or a
        // display-name lookup would either resolve nothing or, worse, resolve something by accident.
        ClaimKind.UnattributedShortfall => $"something in the {c.Subject} still is not paying what it owes",
        _ => c.ToString(),
    };

    /// <summary>
    /// How far he would go on this person's word, in words.
    ///
    /// Qualitative for the same reason confidence is: the number is hidden state, and a percentage
    /// would let the player read the model instead of the man. Note that nothing here explains
    /// *why* he stands where he does. A relationship that cooled because an account did not match
    /// reads identically to one that was never warm, which is correct — the difference is a matter
    /// of history the player has to reconstruct from the accounts, not a label the interface hands
    /// over.
    /// </summary>
    public static string Standing(double trust, Pronouns self, Pronouns other) => trust switch
    {
        >= 0.60 => $"{self.Subject} would take {other.Possessive} word",
        >= 0.35 => $"{self.Subject} {self.Verb("takes", "take")} {other.Object} as {self.Subject} " +
                   $"{self.Verb("finds", "find")} {other.Object}",
        >= 0.15 => $"{self.Subject} {self.Verb("has", "have")} {self.Possessive} reservations about {other.Object}",
        > 0 => $"{self.Subject} would not take {other.Possessive} word for much",
        // Not "anything {other.Subject} says": two people in one sentence and both of them "he" is
        // a sentence the reader cannot parse. The other stays out of subject position here, which
        // also keeps this parallel with the band above it.
        _ => $"{self.Subject} would not take {other.Possessive} word at all",
    };

    /// <summary>
    /// How afraid of somebody he is, or null when it is not worth saying.
    ///
    /// Never an accusation and never a number, exactly as <see cref="Standing"/>.
    /// </summary>
    public static string? Wariness(double fear, Pronouns self, Pronouns other)
        => fear <= 0.25
            ? null
            : $"{self.Subject} {self.Verb("is", "are")} {(fear > 0.6 ? "frightened" : "wary")} of {other.Object}";

    /// <summary>
    /// Qualitative confidence only. INFORMATION_AND_LEGIBILITY.md lists the vocabulary; the numeric
    /// confidence behind it is hidden state and stays hidden. A contradicted account says so
    /// instead, because how sure he was stopped being the interesting fact about it.
    /// </summary>
    public static string Qualify(InformationRecord r, bool contested)
        => contested ? "contradicted" : r.ConfidenceLabel;

    /// <summary>
    /// Where it came from, as the player is entitled to see it. "Vincent says an associate heard"
    /// is meaningfully different from having watched it happen, and the difference has to survive
    /// into the sentence.
    ///
    /// The rule this enforces: none of these may claim presence, sight, or participation that the
    /// acquisition category does not carry. Discovery in particular says "came across", never
    /// "saw" — finding a wrecked shopfront the next morning is not witnessing a beating.
    /// </summary>
    public static string Attribute(InformationRecord r, Func<string, string> name, Pronouns self)
        => r.SourceKind switch
        {
            // Deliberately not "he saw it". Vincent holds that he went outside his boss's rule
            // because he decided to, which is not a thing anybody watches happen.
            SourceKind.Participant => $"{self.Subject} had a hand in it {self.Reflexive}",
            SourceKind.Witness => $"{self.Subject} saw it {self.Reflexive}",
            SourceKind.Discovery => $"{self.Subject} came across it",
            SourceKind.FirstHandTestimony => $"{name(r.SourceId)} was in it and told {self.Object} so",
            SourceKind.Report => $"{name(r.SourceId)} told {self.Object}",
            SourceKind.Inference => $"{self.Subject} worked it out {self.Reflexive}",
            _ => $"talk, no better sourced than {name(r.SourceId)}",
        };

    /// <summary>
    /// What he has on his own account, for a claim his sources disagree about — or null when the
    /// only positions he holds came from other people, in which case they are already listed under
    /// the names of the men who gave them.
    ///
    /// Only what he came to on his own account counts as a separate voice here. Testing for "not a
    /// report" would file somebody else's first-hand account as his own.
    /// </summary>
    public static string? OwnBasis(InformationRecord r, Pronouns self)
        => r.SourceKind switch
        {
            SourceKind.Participant => $"{self.Possessive} own doing",
            SourceKind.Witness => $"{self.Possessive} own eyes",
            SourceKind.Discovery => $"what {self.Subject} came across",
            SourceKind.Inference => $"what {self.Subject} worked out",
            _ => null,
        };
}
