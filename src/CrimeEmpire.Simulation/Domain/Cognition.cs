namespace CrimeSim.Domain;

/// <summary>
/// One thing a named source asserted to this character, preserved as said.
///
/// Testimony is append-only and is never collapsed. That is the whole point: the settled belief
/// keeps one stance per claim so decisions stay simple, while this log keeps every account that
/// was offered, so "Vincent said one thing and Tommy said another" survives long enough to be
/// shown side by side. INFORMATION_AND_LEGIBILITY.md requires conflicting accounts remain
/// traceable to different observations, beliefs or motives; a single overwritten record cannot do
/// that.
/// </summary>
public readonly record struct Testimony(
    Claim Claim,
    Stance AssertedStance,
    double AssertedConfidence,
    string SenderId,
    DateTime At,
    SourceKind ClaimedBasis = SourceKind.Report)
{
    /// <summary>
    /// The basis the speaker presented, kept exactly as offered — and only that.
    ///
    /// Deliberately not what he privately had. This log lives in the listener's head, so anything
    /// stored here is something the listener knows; putting the speaker's real basis here would
    /// hand him the truth about a lie. What the speaker actually had is developer truth and stays
    /// in the report log.
    ///
    /// The settled belief records only whether he now holds first-hand testimony or an ordinary
    /// report. Keeping the claimed basis here means nothing is lost by that coarsening: an account
    /// offered as discovery and one offered as inference stay distinguishable even though both
    /// land as reports.
    /// </summary>
    public SourceKind ClaimedBasis { get; init; } = ClaimedBasis;

    /// <summary>Whether the sender asserted the claim rather than denying it.</summary>
    public bool Affirms => AssertedStance is Stance.Knows or Stance.Believes or Stance.Suspects;

    public override string ToString() => $"{SenderId}: {AssertedStance} {Claim} ({At:yyyy-MM-dd})";
}

/// <summary>
/// Somebody has asserted, to this character, the opposite of a position he currently holds.
///
/// <b>A conflict, not a lie.</b> Nothing in this record says the speaker was dishonest, and nothing
/// in it could: it is assembled entirely from the listener's side of the exchange — what he held,
/// how he came to hold it, and what was claimed at him. The same shape is produced by deception, by
/// sincere disagreement, by a faulty memory, and by the listener having been wrong in the first
/// place, and the simulation deliberately cannot tell them apart from here. Whoever consumes this
/// therefore cannot accidentally react to the truth of the matter, because the truth of the matter
/// is not in it.
///
/// <b>Why it carries the prior's provenance when one rule reads it.</b> Milestone 006 charges the
/// same social cost whether the contradicted position was something he saw or something he was told
/// — <c>Cognition</c> already charges that difference epistemically, through the erosion rates and
/// stance protection below, and weighting it socially as well would bill it twice. The provenance is
/// preserved anyway so a later evidence-led pass can weight on it without having to reconstruct what
/// was discarded here.
/// </summary>
public readonly record struct AccountConflict(
    Claim Claim,
    string SpeakerId,
    Stance AssertedStance,
    double AssertedConfidence,
    SourceKind ClaimedBasis,
    Stance PriorStance,
    double PriorConfidence,
    SourceKind PriorSourceKind,
    string PriorSourceId)
{
    /// <summary>
    /// How hard the disagreement was, as the listener can perceive it: how firmly he held the
    /// position, times how firmly it was contradicted.
    ///
    /// Both factors are things this character has — his own confidence, and the confidence the
    /// speaker put behind the assertion. Neither is world truth, neither is the speaker's private
    /// basis, and neither is the candour of the report that carried it.
    /// </summary>
    public double Strength => PriorConfidence * AssertedConfidence;

    public override string ToString()
        => $"{SpeakerId} contradicted {Claim} (held {PriorConfidence:0.00} via {PriorSourceKind}, " +
           $"asserted {AssertedStance} {AssertedConfidence:0.00})";
}

/// <summary>
/// The result of being told something: the resulting record, and whether the telling was a conflict.
///
/// <see cref="Cognition.Receive"/> used to return the record alone, which meant the one place that
/// knows a contradiction occurred had no way to say so, and the fact was recoverable afterwards only
/// by inspecting <see cref="InformationRecord.Contested"/> — a flag that stays true forever and so
/// cannot distinguish "he was contradicted just now" from "he was contradicted in March".
/// </summary>
public readonly record struct Receipt(InformationRecord Record, AccountConflict? Conflict);

/// <summary>
/// What a character knows, believes and suspects. This is the only source of situational fact a
/// decision is allowed to consult — see Decision/PerceivedSituation.cs, which wraps it and is the
/// sole argument the scorer receives.
///
/// This type knows nothing about relationships. It reports a conflict through <see cref="Receipt"/>
/// and the caller decides what it costs socially; adding a SocialState reference here would put a
/// social consequence inside the belief store, where the layering says it does not belong and where
/// the "decisions read cognition, never world" rule would be much harder to keep honest.
/// </summary>
public sealed class Cognition
{
    private readonly List<InformationRecord> _records = new();
    private readonly List<Testimony> _testimony = new();

    public IReadOnlyList<InformationRecord> Records => _records;

    /// <summary>Every account this character was given, in the order they arrived. Never pruned.</summary>
    public IReadOnlyList<Testimony> Testimony => _testimony;

    /// <summary>
    /// Records a claim. A later acquisition replaces an earlier one when it is at least as
    /// confident, or when it was personally observed — seeing something yourself always overrides
    /// what you were told, so a stale report cannot outlive the evidence that contradicts it.
    /// A vague rumour still cannot erase direct observation.
    /// </summary>
    public InformationRecord Learn(
        Claim claim,
        Stance stance,
        double confidence,
        SourceKind sourceKind,
        string sourceId,
        DateTime at)
    {
        var existing = _records.FindIndex(r => r.Claim.Equals(claim));
        var record = new InformationRecord(claim, stance, confidence, sourceKind, sourceId, at);

        if (existing >= 0)
        {
            var prior = _records[existing];
            bool overrides = sourceKind.OverridesPriorRecord() || confidence >= prior.Confidence;
            if (!overrides) return prior;
            // He is revising something he already had, so the acquisition time stands. Only the
            // moment he last had cause to think about it moves.
            // A disagreement that has already happened is not undone by later learning more.
            record = record with
            {
                AcquiredAt = prior.AcquiredAt,
                LastReconsideredAt = at,
                Contested = prior.Contested,
            };
            _records[existing] = record;
            return record;
        }

        _records.Add(record);
        return record;
    }

    /// <summary>
    /// Takes delivery of one assertion through the report channel.
    ///
    /// Deliberately not <see cref="Learn"/>. Learn models acquiring information; this models being
    /// *told* something, which is a different event with a different failure mode — the sender may
    /// be lying. Two consequences follow. First, the account is always kept verbatim in testimony,
    /// even when it loses the argument, so the disagreement stays visible. Second, a contradiction
    /// costs the standing belief confidence instead of one side silently overwriting the other:
    /// hearing a flat denial of something you were told should leave you less sure, not switch you
    /// cleanly to the newer account. Direct observation resists hardest — being told you did not
    /// see what you saw is weak evidence — but it is not immune, because a character who never
    /// doubts his own eyes cannot be deceived at all.
    /// </summary>
    public Receipt Receive(ReportedClaim asserted, string senderId, DateTime at)
    {
        bool affirms = asserted.AssertedStance is Stance.Knows or Stance.Believes or Stance.Suspects;
        var prior = Find(asserted.Claim);

        // What did this man last say about this, and is he now saying something different?
        //
        // Against his *latest* account, not against anything he has ever said. Searching the whole
        // history for a matching direction meant a witness who affirmed, recanted, then affirmed
        // again had his final word matched to his first and thrown away as repetition — so a man
        // could be talked round and back, and only the first two moves would register. His current
        // position is the one that counts; what he said before it is history, and history is what
        // the testimony log is for.
        Testimony? latestFromSender = null;
        for (int i = _testimony.Count - 1; i >= 0; i--)
        {
            var t = _testimony[i];
            if (t.SenderId != senderId || !t.Claim.Equals(asserted.Claim)) continue;
            latestFromSender = t;
            break;
        }

        // Word for word what he said last time. Only this is repetition.
        //
        // The basis he claims is part of what he said. "I heard it happened" and "I was there, it
        // happened" are the same words about the same claim and are not the same account: the
        // second is a man putting his own presence behind it, and treating that as a repeat would
        // let a witness step forward and be filed as somebody clearing his throat.
        //
        // Compared as claimed, not as the listener files it. Projecting through AsHeardFrom first
        // collapsed Participant and Witness onto one value, so "I did it" and "I saw it" — which
        // are different accounts of the same events, and differ in whether he is confessing —
        // counted as the same man saying the same thing twice.
        bool sameWordsAsLastTime = latestFromSender is { } last
            && last.AssertedStance == asserted.AssertedStance
            && Math.Abs(last.AssertedConfidence - asserted.AssertedConfidence) < 1e-9
            && last.ClaimedBasis == asserted.ClaimedBasis;

        // Whether this character has moved on the matter since that earlier account.
        //
        // Repetition is a property of the exchange, not of the speaker alone. Keyed on the words by
        // themselves, a man could be re-told something he had since come to reject and it counted as
        // nothing — his boss re-issuing a briefing after he had found out for himself that the shop
        // was paying registered as somebody clearing his throat. That is the same defect shape as the
        // self-protection one: a settled/unsettled judgement made from one side of an exchange that
        // has two.
        //
        // "Independently" falls out of the structure rather than needing its own check: the
        // comparison is against this speaker's *latest* account, and that account set the
        // reconsideration stamp itself, so the only way the stamp can be later is movement from
        // somewhere else.
        //
        // One conflict, then inert again. The disagreement branch below stamps the record at `at` and
        // the testimony added above carries the same `at`, so the next identical account finds them
        // equal and returns as a repeat. `<=` rather than `<` means same-instant movement does not
        // re-arm, which is conservative and deterministic.
        //
        // ReconsideredAt, never the nullable LastReconsideredAt: a record nobody has revisited has
        // no reconsideration stamp, and comparing null against an account timestamp would treat a
        // never-revisited belief as one that had moved.
        bool movedSinceThatAccount = latestFromSender is { } previous
            && prior is not null
            && prior.ReconsideredAt > previous.At;

        bool verbatimRepeat = latestFromSender is not null
            ? sameWordsAsLastTime && !movedSinceThatAccount
            : prior?.SourceId == senderId && prior.IsHeld == affirms && !prior.Contested;

        // Whether he has moved. A reversal is worth a change of confidence; firming up or
        // softening the same position is worth noting but is still one man's single voice, so it
        // must not compound — the two questions are separate and were previously conflated.
        bool reversal = latestFromSender is { } prev ? prev.Affirms != affirms : true;

        _testimony.Add(new Testimony(
            asserted.Claim, asserted.AssertedStance, asserted.AssertedConfidence, senderId, at,
            asserted.ClaimedBasis));

        if (prior is null)
        {
            // How it arrives depends on what the account presents itself as. An account offered as
            // a participant's or a witness's own becomes first-hand testimony; anything else is a
            // report, however sincere. Note this reads the *claimed* basis: what the speaker
            // privately has is not something the listener was told.
            var fresh = new InformationRecord(
                asserted.Claim, asserted.AssertedStance, asserted.AssertedConfidence,
                asserted.ClaimedBasis.AsHeardFrom(), senderId, at);
            _records.Add(fresh);
            // Nothing to conflict with. Being told something he has no position on either way is
            // news, not a disagreement, however wrong it may be.
            return new Receipt(fresh, null);
        }

        // Word for word what he said last time: the record is left exactly as it was, including
        // its reconsideration stamp. A belief marked freshly revisited every time somebody repeats
        // himself would make "I have learned something since I last spoke" true forever, and two
        // characters would file accounts at each other until the calendar ran out.
        // Word for word what he said last time — including, crucially, when what he said last time
        // was the same denial. Repeating a contradiction is not a fresh conflict, and returning here
        // before the disagreement branch below is what makes "new, non-repeated" structural rather
        // than a second rule that could disagree with this one. The same early return is what stops
        // repeated denials compounding confidence loss, so the two guarantees cannot drift apart.
        if (verbatimRepeat) return new Receipt(prior, null);

        // Whether this account is better sourced than the one the belief currently rests on, and
        // therefore ought to become the thing he would cite.
        //
        // This used to be decided only when the claim was new, so a man who already had a rumour
        // and was then told by the participant himself kept the rumour's provenance and the
        // rumour-teller's name against it for the rest of the run. Attribution follows the best
        // account he has been given, not the first.
        var upgraded = Upgrade(prior, asserted, senderId);

        // He has said something at least slightly different. That is worth registering as a
        // development even when it does not shift the belief — which is the case for a man firming
        // up or softening a position he already gave: still one voice, so it must not compound.
        if (!reversal && prior.IsHeld == affirms)
            return new Receipt(Replace(prior, upgraded with { LastReconsideredAt = at }), null);

        // Agreement, from a voice that is new to this claim or has just come round to it. Either
        // way it is support the belief did not have before.
        if (prior.IsHeld == affirms)
        {
            double raised = Math.Clamp(prior.Confidence + 0.15 * asserted.AssertedConfidence, 0, 1);
            return new Receipt(
                Replace(prior, upgraded with { Confidence = raised, LastReconsideredAt = at }), null);
        }

        // Disagreement: a first denial from this man, or a reversal of what he told him before.
        // Either is a reason to be less sure; hearing the identical denial twice is not, and has
        // already returned above.
        // What he did or saw resists hardest. An account he was given — including one from the man
        // who was in it — is testimony, and testimony against testimony is an ordinary conflict of
        // sources rather than being told you did not see what you saw. A trace he interpreted is in
        // the ordinary bracket too: disagreeing about what a wrecked shopfront meant is a normal
        // disagreement, not a challenge to his eyes.
        double erosion = prior.SourceKind.ResistsContradiction() ? 0.15 : 0.45;
        double shaken = Math.Clamp(prior.Confidence * (1 - erosion * asserted.AssertedConfidence), 0, 1);

        // Below the point where he would still act on it, the stance itself gives way — but only
        // for something he was told. What he saw himself decays in confidence and stays held.
        var stance = prior.Stance;
        if (!prior.SourceKind.ProtectsStance() && shaken < 0.3)
            stance = prior.IsHeld ? Stance.Doubts : Stance.Suspects;

        var contradicted = Replace(prior, prior with
        {
            Stance = stance,
            Confidence = shaken,
            LastReconsideredAt = at,
            Contested = true,
        });

        // The conflict is reported from exactly the branch that sets Contested, and describes the
        // position as it stood *before* being shaken — the listener's firmness at the moment he was
        // contradicted is what made the disagreement a hard one, not what is left of it afterwards.
        //
        // The prior's provenance travels whether or not any rule currently reads it. Milestone 004's
        // lesson, four times over, was that a distinction drawn in one place and dropped on the way
        // to the next is the defect this codebase produces most reliably.
        return new Receipt(contradicted, new AccountConflict(
            asserted.Claim,
            senderId,
            asserted.AssertedStance,
            asserted.AssertedConfidence,
            asserted.ClaimedBasis,
            prior.Stance,
            prior.Confidence,
            prior.SourceKind,
            prior.SourceId));
    }

    /// <summary>
    /// How good an account is, for deciding which one a belief should be attributed to.
    ///
    /// Only the kinds a listener can end up with are ranked. A record he established himself is
    /// never re-attributed to somebody who told him the same thing — being told what you already
    /// saw does not make it his account — so those return null and the caller leaves them alone.
    /// </summary>
    private static int? AttributionRank(SourceKind kind) => kind switch
    {
        SourceKind.FirstHandTestimony => 2,
        SourceKind.Report => 1,
        SourceKind.Rumor => 0,
        _ => null,
    };

    /// <summary>
    /// Re-attributes a belief to a better account, if this one is better.
    ///
    /// Equal-ranked accounts leave it alone: two reports are two reports, and the first man to say
    /// it keeps his name against it. Only a genuine step up — a rumour becoming a report, a report
    /// becoming a participant's own word — moves the attribution, and it moves the source with it,
    /// because a belief that says "first-hand" while naming the man who only passed it on is worse
    /// than either fact alone.
    /// </summary>
    private static InformationRecord Upgrade(InformationRecord prior, ReportedClaim asserted, string senderId)
    {
        if (AttributionRank(prior.SourceKind) is not { } currentRank) return prior;

        var heard = asserted.ClaimedBasis.AsHeardFrom();
        if (AttributionRank(heard) is not { } incomingRank || incomingRank <= currentRank) return prior;

        return prior with { SourceKind = heard, SourceId = senderId };
    }

    private InformationRecord Replace(InformationRecord prior, InformationRecord updated)
    {
        int i = _records.FindIndex(r => r.Claim.Equals(prior.Claim));
        if (i >= 0) _records[i] = updated;
        return updated;
    }

    /// <summary>Every account this character was given about one claim, in arrival order.</summary>
    public IEnumerable<Testimony> AccountsOf(Claim claim)
        => _testimony.Where(t => t.Claim.Equals(claim));

    /// <summary>Whether this person has given him an account of anything at all.</summary>
    public bool HasAccountFrom(string senderId)
    {
        foreach (var t in _testimony)
            if (t.SenderId == senderId) return true;
        return false;
    }

    /// <summary>
    /// Whether this person has given him an account <em>of this claim</em>.
    ///
    /// The distinction matters wherever the question is "have I already heard him on this". Asking
    /// only whether he has ever said anything treats one man's single remark about a shakedown as
    /// his answer to every question that could ever be put to him.
    /// </summary>
    public bool HasAccountFrom(string senderId, Claim about)
    {
        foreach (var t in _testimony)
            if (t.SenderId == senderId && t.Claim.Equals(about)) return true;
        return false;
    }

    /// <summary>
    /// True when the accounts this character holds do not agree — either two sources said opposite
    /// things, or someone contradicted what he saw for himself. This is what the player-facing
    /// layer renders as "contradicted"; it is a property of the sources, not of the truth, and a
    /// contested claim may still be perfectly true.
    /// </summary>
    public bool IsContested(Claim claim)
    {
        // Somebody contradicted what he held at the time they said it. Recorded then, because the
        // stance may since have moved to agree with them — which is the deception succeeding, not
        // the disagreement disappearing.
        if (Find(claim) is { Contested: true }) return true;

        // Or two of his sources simply disagree with each other.
        bool affirmed = false, denied = false;
        foreach (var t in AccountsOf(claim))
        {
            if (t.Affirms) affirmed = true;
            else denied = true;
            if (affirmed && denied) return true;
        }

        return false;
    }

    public InformationRecord? Find(Claim claim)
    {
        foreach (var r in _records)
            if (r.Claim.Equals(claim)) return r;
        return null;
    }

    public IEnumerable<InformationRecord> OfKind(ClaimKind kind)
        => _records.Where(r => r.Claim.Kind == kind && r.IsHeld);

    /// <summary>True only if the character actually holds this claim. Never consults world truth.</summary>
    public bool Holds(Claim claim) => Find(claim) is { IsHeld: true };

    public double ConfidenceIn(Claim claim) => Find(claim) is { IsHeld: true } r ? r.Confidence : 0.0;
}
