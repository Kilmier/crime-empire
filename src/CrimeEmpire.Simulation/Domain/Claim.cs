namespace CrimeSim.Domain;

/// <summary>
/// The claim vocabulary for the spike. Structured predicates rather than free text, so that
/// "does this character know X" is an exact lookup and cannot accidentally succeed on a substring.
/// Small on purpose — INFORMATION_AND_LEGIBILITY.md asks what the smallest useful vocabulary is,
/// and this is a first answer to be revised from evidence.
/// </summary>
public enum ClaimKind
{
    /// <summary>Subject (a business) is refusing or short-paying tribute.</summary>
    BusinessRefusesTribute,

    /// <summary>Subject used violence against Object.</summary>
    PersonUsedViolence,

    /// <summary>Police are investigating Subject or Subject's district.</summary>
    PoliceInvestigating,

    /// <summary>Subject holds a grievance against Object.</summary>
    PersonHoldsGrievance,

    /// <summary>Tribute was collected from Subject.</summary>
    TributeCollected,

    /// <summary>Someone witnessed the incident identified by EventId.</summary>
    WitnessSawIncident,

    /// <summary>Subject (an organisation) issued the policy named in Object.</summary>
    PolicyIssued,

    /// <summary>Subject acted outside the policy named in Object.</summary>
    PersonBreachedPolicy,

    /// <summary>Subject is vulnerable to pressure (weak resistance, no protection).</summary>
    TargetIsVulnerable,
}

/// <summary>A proposition a character can hold, communicate, or be wrong about.</summary>
public readonly record struct Claim(ClaimKind Kind, string Subject, string Object = "", long EventId = 0)
{
    public override string ToString()
        => Object.Length == 0
            ? $"{Kind}({Subject}{(EventId != 0 ? $"#{EventId}" : "")})"
            : $"{Kind}({Subject} -> {Object}{(EventId != 0 ? $"#{EventId}" : "")})";
}

public enum Stance
{
    Knows,
    Believes,
    Suspects,
    Doubts,
    Rejects,
}

public enum SourceKind
{
    /// <summary>Personally observed. Highest reliability.</summary>
    Direct,

    /// <summary>Told by a named character through an organisational channel.</summary>
    Report,

    /// <summary>Circulating without a reliable chain of support.</summary>
    Rumor,

    /// <summary>Derived by the character from other things they hold.</summary>
    Inference,
}

/// <summary>
/// One character's stance on one claim. Confidence is character-relative and carries no guarantee
/// of truth — a high-confidence belief may be flatly wrong, and the simulation must never use this
/// record as a shortcut to authoritative world state.
/// </summary>
public sealed record InformationRecord(
    Claim Claim,
    Stance Stance,
    double Confidence,
    SourceKind SourceKind,
    string SourceId,
    DateTime AcquiredAt,
    DateTime? LastReconsideredAt = null,
    bool Contested = false)
{
    /// <summary>
    /// Set when somebody has told him the opposite of this, whatever he concluded in the end.
    ///
    /// Recorded at the moment of the disagreement rather than re-derived later from the current
    /// stance, because the stance is exactly what a contradiction changes: a belief eroded until
    /// he doubts it now *agrees* with the man who talked him out of it, and re-deriving would
    /// report no conflict precisely in the case where the deception worked. The disagreement is a
    /// fact about his sources, and it does not stop having happened.
    /// </summary>
    public bool Contested { get; init; } = Contested;

    /// <summary>
    /// When this was last argued about, defaulting to when it was first acquired.
    ///
    /// Kept distinct from <see cref="AcquiredAt"/> on purpose, as
    /// INFORMATION_AND_LEGIBILITY.md's Character Information Record has both. Letting a later
    /// corroboration overwrite the acquisition time would silently rewrite the player's timeline —
    /// something he was told in March would appear to have been learned in May, purely because
    /// somebody mentioned it again.
    /// </summary>
    public DateTime ReconsideredAt => LastReconsideredAt ?? AcquiredAt;

    public bool IsHeld => Stance is Stance.Knows or Stance.Believes or Stance.Suspects;

    /// <summary>
    /// How sure he is, in words, saying nothing about how he came to be sure.
    ///
    /// INFORMATION_AND_LEGIBILITY.md's confidence vocabulary includes "personally witnessed", and
    /// this used to emit it at the top of the range — but confidence and provenance are different
    /// axes, and a label chosen purely by a number cannot claim a method. Vincent holds that he
    /// went outside his boss's rule at full confidence because he *decided* it; rendering that as
    /// "personally witnessed" tells the player he watched something happen, which is a fact the
    /// simulation never recorded and which is flatly untrue.
    ///
    /// "Personally witnessed" is a legitimate thing to say once provenance can establish
    /// witnessing — see the open provenance item in
    /// docs/milestones/003-information-transmission.md. Until then the labels describe certainty
    /// only, and <see cref="SourceKind"/> is the one thing allowed to speak about method.
    /// </summary>
    public string ConfidenceLabel => Confidence switch
    {
        >= 0.9 => "beyond doubt for him",
        >= 0.7 => "strongly supported",
        >= 0.5 => "plausible",
        >= 0.3 => "uncertain",
        _ => "source reliability unknown",
    };

    public override string ToString()
        => $"{Stance} {Claim} ({ConfidenceLabel}, via {SourceKind}:{SourceId}, {AcquiredAt:yyyy-MM-dd})";
}
