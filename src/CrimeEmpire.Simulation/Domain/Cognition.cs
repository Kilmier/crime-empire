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

        // Has this man already told him *this*, as opposed to having spoken about it at all?
        //
        // The distinction carries real weight. Repetition is not evidence — it is the same
        // evidence, said again, and hearing it a second time must not compound confidence or count
        // as a development. But a man who affirmed something last month and denies it today has
        // not repeated himself: he has recanted, and that is new information of the most
        // interesting kind. Suppressing it because the sender was familiar would make a witness
        // permanently unable to take anything back.
        //
        // So the test is same sender AND same direction. Checking the record's SourceId alone was
        // wrong twice over: the record keeps its original source through revisions, so one man
        // could corroborate his own earlier report indefinitely and read as a fresh voice each
        // time; and a claim first acquired by observation names the observer, so a single sender
        // never matched at all.
        bool sameAccountAgain =
            (prior?.SourceId == senderId && prior.IsHeld == affirms) ||
            _testimony.Any(t => t.SenderId == senderId
                                && t.Claim.Equals(asserted.Claim)
                                && t.Affirms == affirms);

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

        // Agreement. A second, independent voice is worth more than the same voice repeating
        // itself, so corroboration only counts when it comes from someone new.
        if (prior.IsHeld == affirms)
        {
            // Nothing new was said and nothing changed, so the record is left exactly as it was —
            // including its reconsideration time. A belief that gets stamped as freshly revisited
            // every time somebody repeats himself would make "he has learned something since he
            // last spoke" true forever, and two characters would report to each other until the
            // calendar ran out.
            if (sameAccountAgain) return prior;

            double raised = Math.Clamp(prior.Confidence + 0.15 * asserted.AssertedConfidence, 0, 1);
            return Replace(prior, prior with { Confidence = raised, LastReconsideredAt = at });
        }

        // Disagreement. Repetition is not evidence here either: a man who has already denied it
        // once does not wear the belief down further by denying it again. A man who is denying it
        // for the first time does, even if he has affirmed it before — that is a recantation, and
        // it falls through to the erosion below.
        if (sameAccountAgain) return prior;

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
