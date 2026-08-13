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
            record = record with { AcquiredAt = prior.AcquiredAt, LastReconsideredAt = at };
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
        _testimony.Add(new Testimony(
            asserted.Claim, asserted.AssertedStance, asserted.AssertedConfidence, senderId, at));

        bool affirms = asserted.AssertedStance is Stance.Knows or Stance.Believes or Stance.Suspects;
        var prior = Find(asserted.Claim);

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
            bool independent = prior.SourceId != senderId;
            double raised = independent
                ? Math.Clamp(prior.Confidence + 0.15 * asserted.AssertedConfidence, 0, 1)
                : prior.Confidence;
            return Replace(prior, prior with { Confidence = raised, LastReconsideredAt = at });
        }

        // Disagreement.
        double erosion = prior.SourceKind == SourceKind.Direct ? 0.15 : 0.45;
        double shaken = Math.Clamp(prior.Confidence * (1 - erosion * asserted.AssertedConfidence), 0, 1);

        // Below the point where he would still act on it, the stance itself gives way — but only
        // for something he was told. What he saw himself decays in confidence and stays held.
        var stance = prior.Stance;
        if (prior.SourceKind != SourceKind.Direct && shaken < 0.3)
            stance = prior.IsHeld ? Stance.Doubts : Stance.Suspects;

        return Replace(prior, prior with { Stance = stance, Confidence = shaken, LastReconsideredAt = at });
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
        bool affirmed = false, denied = false;
        foreach (var t in AccountsOf(claim))
        {
            if (t.Affirms) affirmed = true;
            else denied = true;
            if (affirmed && denied) return true;
        }

        if (!affirmed && !denied) return false;

        // A single account that cuts against his own direct observation is a conflict too.
        return Find(claim) is { SourceKind: SourceKind.Direct } own && own.IsHeld != affirmed;
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
