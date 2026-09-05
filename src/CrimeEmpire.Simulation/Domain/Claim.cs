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

    /// <summary>
    /// Subject (a domain, never a business) has a revenue shortfall nobody has correctly attributed.
    ///
    /// Milestone 012. Deliberately never a business — see <c>Decision/Inference.cs</c>'s shortfall
    /// suspicion, which is the only place this is ever drawn. A boss who has been contradicted about
    /// why the takings are short may come to suspect that something else in his own domain is
    /// refusing, without being told what: the subject names where to look, never what to find.
    /// </summary>
    UnattributedShortfall,

    /// <summary>
    /// Subject (a person) clears the bar named in Object — see <see cref="CapabilityBar"/>.
    ///
    /// <b>Milestone 021, and the shape is the ruling.</b> How good somebody is at a job is a fact
    /// about the world that a character can be *wrong* about, unlike trust or fear, which have no
    /// truth value. It therefore belongs here, with a source and a confidence, rather than as a
    /// number on the relationship — which is where milestone 020 first put it, and why that had to
    /// be undone.
    ///
    /// <b>Graded rather than scalar, deliberately.</b> Magnitude is carried by *which* bars a
    /// character holds; <see cref="InformationRecord.Confidence"/> carries how sure he is of each.
    /// Collapsing the two — encoding "he is very good" as "I am very sure he is good" — is the
    /// distinction-losing move this project keeps having to undo, and the ruling forbids it. A man
    /// firmly believed to clear the low bar and firmly believed to fail the high one is a sharper
    /// statement than any single number, and it is one the ladder can make.
    /// </summary>
    PersonIsCapable,
}

/// <summary>
/// The bars a <see cref="ClaimKind.PersonIsCapable"/> claim can be about, lowest first.
///
/// Named constants rather than loose strings so no call site invents a bar, following the precedent
/// <see cref="ClaimKind.PolicyIssued"/> sets by naming a policy id in a claim's Object.
///
/// <b>Two, and about force only.</b> Milestone 021 proves the mechanism on one skill; assessments of
/// Persuasion, Discretion or Investigation are explicitly out of its scope.
///
/// <b>The ladder is ordered, and since milestone 021's correction that ordering is enforced on read
/// rather than on write.</b> Clearing <see cref="HardMan"/> implies clearing <see cref="RoughWork"/>.
/// Storage still admits an incoherent pair — a man may hold the high bar while rejecting the low one,
/// because he is allowed to be wrong and nothing gets to tidy his beliefs behind his back, which is
/// the same rule the correction enforces one file over by refusing to move a belief no information
/// reached him about. What changed is that no *reader* may act on the raw pair: every consumer goes
/// through <see cref="Read"/>, so the scorer and the roster cannot arrive at different tiers for the
/// same man, and the original records stay exactly as he formed them for developer traces and replay.
/// </summary>
public static class CapabilityBar
{
    /// <summary>He is up to leaning on somebody at all.</summary>
    public const string RoughWork = "rough-work";

    /// <summary>He is exceptional at it — the bar above <see cref="RoughWork"/>.</summary>
    public const string HardMan = "hard-man";

    /// <summary>The ladder, lowest bar first. Ordering is data, not a convention at each reader.</summary>
    public static readonly IReadOnlyList<string> Ladder = new[] { RoughWork, HardMan };

    /// <summary>The claim that <paramref name="personId"/> clears <paramref name="bar"/>.</summary>
    public static Claim About(string personId, string bar)
        => new(ClaimKind.PersonIsCapable, personId, bar);

    /// <summary>
    /// One coherent reading of where somebody sits on the ladder, for every bar the reader has any
    /// position on. THE SINGLE DERIVATION — <see cref="Decision.Utility"/> and
    /// <see cref="Session.PlayerView"/> both come through here, so what the decision weighed and what
    /// the roster says he takes the man for cannot disagree.
    ///
    /// <b>The rule, in one sentence: the highest bar he holds sets his tier, and every bar below it
    /// is entailed.</b> That single rule covers both ways the raw records fall short of a coherent
    /// ladder — a gap (he holds the high bar and has never considered the low one) and a genuine
    /// contradiction (he holds the high bar and rejects the low one). Resolving the contradiction the
    /// other way, letting the rejection win, would need a second rule and would make the gap case
    /// inconsistent with it.
    ///
    /// <b>Entailment supplies a position; it never overwrites one that already agrees.</b> A bar he
    /// independently holds keeps its own confidence, so a firm "up to rough work" is not quietly
    /// reduced to the confidence of a shakier belief above it. Only a bar he has no view on, or one
    /// he rejects while holding something above it, takes the entailing bar's confidence — and is
    /// marked <see cref="CapabilityReading.Entailed"/> so a caller can tell a conclusion he reached
    /// from one the ladder reached for him.
    ///
    /// <b>Nothing here mutates cognition.</b> It reads through the supplied lookup and returns a
    /// projection; the records it read are untouched and stay available raw.
    /// </summary>
    /// <param name="personId">The man being assessed.</param>
    /// <param name="position">
    /// The reader's own lookup — <c>PerceivedSituation.Position</c> for scoring, a scan of
    /// <c>Cognition.Records</c> for presentation. Called once per bar, lowest first, so a lookup with
    /// its own bookkeeping (the perceived situation marks a record as consulted) sees the same order
    /// it always did.
    /// </param>
    public static IReadOnlyList<CapabilityReading> Read(
        string personId, Func<Claim, InformationRecord?> position)
    {
        var raw = new InformationRecord?[Ladder.Count];
        for (int i = 0; i < Ladder.Count; i++)
            raw[i] = position(About(personId, Ladder[i]));

        var readings = new List<CapabilityReading>(Ladder.Count);

        for (int i = 0; i < Ladder.Count; i++)
        {
            if (raw[i] is { IsHeld: true } own)
            {
                readings.Add(new CapabilityReading(Ladder[i], true, own.Confidence, Entailed: false));
                continue;
            }

            // The highest bar above this one that he actually holds. Searched downward from the top
            // so the answer does not depend on how many rungs the ladder has.
            InformationRecord? entailing = null;
            for (int j = Ladder.Count - 1; j > i; j--)
                if (raw[j] is { IsHeld: true } higher) { entailing = higher; break; }

            if (entailing is { } e)
                readings.Add(new CapabilityReading(Ladder[i], true, e.Confidence, Entailed: true));
            else if (raw[i] is { } rejected)
                readings.Add(new CapabilityReading(Ladder[i], false, rejected.Confidence, Entailed: false));
        }

        return readings;
    }

    /// <summary>
    /// Whether a resolved reading clears one named bar, or null where he has no view of it at all.
    /// Null is deliberately not folded into false — "he has never thought about it" and "he has
    /// concluded the man is not up to it" are different things and the roster renders them so.
    /// </summary>
    public static bool? Clears(IReadOnlyList<CapabilityReading> readings, string bar)
    {
        foreach (var r in readings)
            if (string.Equals(r.Bar, bar, StringComparison.Ordinal)) return r.Clears;
        return null;
    }
}

/// <summary>
/// One bar of <see cref="CapabilityBar"/>'s ladder as a reader should act on it, after the ladder's
/// implication has been applied. See <see cref="CapabilityBar.Read"/>.
/// </summary>
/// <param name="Bar">Which bar, from <see cref="CapabilityBar.Ladder"/>.</param>
/// <param name="Clears">Whether he takes the man to clear it.</param>
/// <param name="Confidence">How sure — his own for a position he holds, the entailing bar's otherwise.</param>
/// <param name="Entailed">
/// True when the ladder supplied this position rather than the man himself: he either had no view of
/// this bar, or rejected it while holding one above it. Kept so a developer trace can tell the two
/// apart; no scoring rule reads it, because an entailed position is a position.
/// </param>
public readonly record struct CapabilityReading(string Bar, bool Clears, double Confidence, bool Entailed);

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

/// <summary>
/// How a character came to hold a claim — the acquisition method, and nothing about how sure he is.
///
/// There is deliberately no umbrella value covering "unmediated". A single broad category is what
/// this vocabulary replaced: it could not tell a man who ordered a beating from a man who watched
/// one from a man who found the wreckage the next morning, and every rule that keyed off it had to
/// treat all three the same. Ask for the shared property through <see cref="Provenance"/> instead;
/// reintroducing an umbrella member is how the conflation comes back.
/// </summary>
public enum SourceKind
{
    /// <summary>
    /// He did it, ordered it, or it is his own act. The only category that justifies knowing
    /// hidden authorship, because the author is him.
    /// </summary>
    Participant,

    /// <summary>
    /// He was there and saw it. Carries what was done and by whom he could see — never who
    /// authorised it, which is not a visible property of an event.
    /// </summary>
    Witness,

    /// <summary>
    /// He came upon a trace or a consequence afterwards. Explicitly implies he was *not* present:
    /// a wrecked shopfront is found, not witnessed.
    /// </summary>
    Discovery,

    /// <summary>
    /// Someone who was in it told him directly. Still testimony — closer to the event than a
    /// filed report, but an account he could disbelieve, not something he established himself.
    /// </summary>
    FirstHandTestimony,

    /// <summary>Told by a named character through an organisational channel.</summary>
    Report,

    /// <summary>Circulating without a reliable chain of support.</summary>
    Rumor,

    /// <summary>Derived by the character from other things they hold.</summary>
    Inference,
}

/// <summary>
/// Why a settled belief was last reconsidered — the occasion, not the original acquisition.
///
/// Added by milestone 021's correction. Confidence was moving on evidence and the record said only
/// *when*, so a belief that shifted because a job came back looked identical to one that shifted
/// because a canvass found nothing. Named causes rather than free text, following
/// <see cref="StandingCause"/>, which milestone 023 added to the relationship record for the same
/// reason and in the same shape.
/// </summary>
public enum ReconsiderCause
{
    /// <summary>Work he handed to somebody came back, and he read something into it about the man.</summary>
    DelegatedOutcome,

    /// <summary>He went back over an incident of his own and revised who he thinks can place him there.</summary>
    ConcealmentAttempted,

    /// <summary>He looked for a witness and did not find one, which weakens the lead without refuting it.</summary>
    CanvassFoundNothing,
}

/// <summary>
/// What moved a belief the last time it moved, kept alongside — never instead of — how it was first
/// acquired.
///
/// <b>Both halves are the point.</b> <see cref="InformationRecord.SourceKind"/> and
/// <see cref="InformationRecord.SourceId"/> still say how he came to hold the thing at all; this says
/// what later gave him cause to think again, and through what channel that reached him. Overwriting
/// the acquisition source with the revision's would make a belief he inferred in March look like one
/// he discovered in May — the same silent-rewrite failure <see cref="InformationRecord.AcquiredAt"/>
/// and <see cref="InformationRecord.ReconsideredAt"/> are kept separate to prevent.
/// </summary>
/// <param name="Cause">The occasion.</param>
/// <param name="Via">
/// How the evidence for the revision reached him — <see cref="SourceKind.Discovery"/> for takings
/// arriving, <see cref="SourceKind.Participant"/> for something he did himself. Never a source he
/// was not actually on the receiving end of.
/// </param>
/// <param name="AboutId">
/// Who or what the occasion concerned — the man he sent, the incident he went back over. An id, not
/// a sentence: this is state, and the wording belongs to the player-facing layer.
/// </param>
public readonly record struct Reconsideration(ReconsiderCause Cause, SourceKind Via, string AboutId)
{
    public override string ToString() => $"{Cause}:{Via}:{AboutId}";
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
    bool Contested = false,
    Reconsideration? Reconsidered = null)
{
    /// <summary>
    /// What last gave him cause to think again, or null if nothing has since he acquired it.
    ///
    /// Paired with <see cref="ReconsideredAt"/>, which says when. Deliberately not a history: the
    /// settled belief keeps one stance and one occasion, and anything richer belongs in the
    /// append-only testimony log rather than here.
    /// </summary>
    public Reconsideration? Reconsidered { get; init; } = Reconsidered;

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
    /// Provenance can now establish witnessing — <see cref="SourceKind.Witness"/> exists — but that
    /// does not move "personally witnessed" back into this list. Certainty and method are separate
    /// axes and each gets its own sentence: the player is told how sure he is here, and how he came
    /// to be sure by the provenance rendering. A confidence label that named a method would let a
    /// number assert a fact about how something was learned, which is the defect this list was
    /// rewritten to remove.
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
