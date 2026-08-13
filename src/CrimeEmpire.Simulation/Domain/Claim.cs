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
    DateTime? LastReconsideredAt = null)
{
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

    public string ConfidenceLabel => Confidence switch
    {
        >= 0.9 => "personally witnessed",
        >= 0.7 => "strongly supported",
        >= 0.5 => "plausible",
        >= 0.3 => "uncertain",
        _ => "source reliability unknown",
    };

    public override string ToString()
        => $"{Stance} {Claim} ({ConfidenceLabel}, via {SourceKind}:{SourceId}, {AcquiredAt:yyyy-MM-dd})";
}
