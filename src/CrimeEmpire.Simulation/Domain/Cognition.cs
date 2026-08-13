namespace CrimeSim.Domain;

/// <summary>
/// What a character knows, believes and suspects. This is the only source of situational fact a
/// decision is allowed to consult — see Decision/PerceivedSituation.cs, which wraps it and is the
/// sole argument the scorer receives.
/// </summary>
public sealed class Cognition
{
    private readonly List<InformationRecord> _records = new();

    public IReadOnlyList<InformationRecord> Records => _records;

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
            _records[existing] = record;
            return record;
        }

        _records.Add(record);
        return record;
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
