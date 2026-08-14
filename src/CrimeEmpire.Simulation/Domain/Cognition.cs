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
    DateTime At)
{
    /// <summary>Whether the sender asserted the claim rather than denying it.</summary>
    public bool Affirms => AssertedStance is Stance.Knows or Stance.Believes or Stance.Suspects;

    public override string ToString() => $"{SenderId}: {AssertedStance} {Claim} ({At:yyyy-MM-dd})";
}

/// <summary>
/// What a character knows, believes and suspects. This is the only source of situational fact a
/// decision is allowed to consult — see Decision/PerceivedSituation.cs, which wraps it and is the
/// sole argument the scorer receives.
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
            bool overrides = sourceKind == SourceKind.Direct || confidence >= prior.Confidence;
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
    public InformationRecord Receive(ReportedClaim asserted, string senderId, DateTime at)
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
        bool verbatimRepeat = latestFromSender is { } last
            ? last.AssertedStance == asserted.AssertedStance
              && Math.Abs(last.AssertedConfidence - asserted.AssertedConfidence) < 1e-9
            : prior?.SourceId == senderId && prior.IsHeld == affirms && !prior.Contested;

        // Whether he has moved. A reversal is worth a change of confidence; firming up or
        // softening the same position is worth noting but is still one man's single voice, so it
        // must not compound — the two questions are separate and were previously conflated.
        bool reversal = latestFromSender is { } prev ? prev.Affirms != affirms : true;

        _testimony.Add(new Testimony(
            asserted.Claim, asserted.AssertedStance, asserted.AssertedConfidence, senderId, at));

        if (prior is null)
        {
            var fresh = new InformationRecord(
                asserted.Claim, asserted.AssertedStance, asserted.AssertedConfidence,
                SourceKind.Report, senderId, at);
            _records.Add(fresh);
            return fresh;
        }

        // Word for word what he said last time: the record is left exactly as it was, including
        // its reconsideration stamp. A belief marked freshly revisited every time somebody repeats
        // himself would make "I have learned something since I last spoke" true forever, and two
        // characters would file accounts at each other until the calendar ran out.
        if (verbatimRepeat) return prior;

        // He has said something at least slightly different. That is worth registering as a
        // development even when it does not shift the belief — which is the case for a man firming
        // up or softening a position he already gave: still one voice, so it must not compound.
        if (!reversal && prior.IsHeld == affirms)
            return Replace(prior, prior with { LastReconsideredAt = at });

        // Agreement, from a voice that is new to this claim or has just come round to it. Either
        // way it is support the belief did not have before.
        if (prior.IsHeld == affirms)
        {
            double raised = Math.Clamp(prior.Confidence + 0.15 * asserted.AssertedConfidence, 0, 1);
            return Replace(prior, prior with { Confidence = raised, LastReconsideredAt = at });
        }

        // Disagreement: a first denial from this man, or a reversal of what he told him before.
        // Either is a reason to be less sure; hearing the identical denial twice is not, and has
        // already returned above.
        double erosion = prior.SourceKind == SourceKind.Direct ? 0.15 : 0.45;
        double shaken = Math.Clamp(prior.Confidence * (1 - erosion * asserted.AssertedConfidence), 0, 1);

        // Below the point where he would still act on it, the stance itself gives way — but only
        // for something he was told. What he saw himself decays in confidence and stays held.
        var stance = prior.Stance;
        if (prior.SourceKind != SourceKind.Direct && shaken < 0.3)
            stance = prior.IsHeld ? Stance.Doubts : Stance.Suspects;

        return Replace(prior, prior with
        {
            Stance = stance,
            Confidence = shaken,
            LastReconsideredAt = at,
            Contested = true,
        });
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
