namespace CrimeSim.Decision;

using CrimeSim.Domain;

/// <summary>
/// The character's own picture of their situation, built from their belief set alone.
///
/// This type holds no reference to World, and neither does Utility.Score, which takes it as its
/// only source of situational fact. "Characters do not act on information they lack" is therefore
/// a type signature rather than a test that can rot. Do not add a World field here.
///
/// Every read is recorded in <see cref="Used"/> so the decision trace can report which beliefs and
/// sources actually fed the choice.
/// </summary>
public sealed class PerceivedSituation
{
    private readonly List<InformationRecord> _beliefs;
    private readonly List<InformationRecord> _used = new();

    private readonly List<Testimony> _testimony;

    public PerceivedSituation(
        string actorId,
        DateTime now,
        IEnumerable<InformationRecord> beliefs,
        IEnumerable<Testimony>? testimony = null)
    {
        ActorId = actorId;
        Now = now;
        _beliefs = beliefs.ToList();
        _testimony = testimony?.ToList() ?? new List<Testimony>();
    }

    public string ActorId { get; }
    public DateTime Now { get; }
    public IReadOnlyList<InformationRecord> Beliefs => _beliefs;

    /// <summary>
    /// The accounts he has been given. Part of his own view of his situation — remembering who
    /// told you what is not privileged world state.
    /// </summary>
    public IReadOnlyList<Testimony> Testimony => _testimony;

    /// <summary>
    /// Whether this person has already given him their version. Asking a man a second time for the
    /// account he has already given is not a live option — it is the same question, and it is what
    /// turns "seek corroboration" into an exchange that repeats until the calendar runs out.
    /// </summary>
    public bool HasAccountFrom(string senderId)
    {
        foreach (var t in _testimony)
            if (t.SenderId == senderId) return true;
        return false;
    }

    /// <summary>Beliefs consulted so far during this deliberation, in order of first use.</summary>
    public IReadOnlyList<InformationRecord> Used => _used;

    private void MarkUsed(InformationRecord r)
    {
        if (!_used.Contains(r)) _used.Add(r);
    }

    public InformationRecord? Find(Claim claim)
    {
        foreach (var r in _beliefs)
        {
            if (!r.Claim.Equals(claim)) continue;
            MarkUsed(r);
            return r.IsHeld ? r : null;
        }
        return null;
    }

    public bool Holds(Claim claim) => Find(claim) is not null;

    public double Confidence(Claim claim) => Find(claim)?.Confidence ?? 0.0;

    public IEnumerable<InformationRecord> OfKind(ClaimKind kind)
    {
        foreach (var r in _beliefs)
        {
            if (r.Claim.Kind != kind || !r.IsHeld) continue;
            MarkUsed(r);
            yield return r;
        }
    }

    /// <summary>
    /// How much police interest this character believes exists around a subject. Derived from held
    /// claims only — a character under active surveillance they have not noticed perceives zero.
    /// </summary>
    public double PerceivedPoliceInterest(string subject)
    {
        double max = 0;
        foreach (var r in OfKind(ClaimKind.PoliceInvestigating))
            if (r.Claim.Subject == subject || r.Claim.Object == subject)
                max = Math.Max(max, r.Confidence);
        return max;
    }

    /// <summary>True only if this character has been told the policy exists.</summary>
    public bool KnowsPolicy(string orgId, string policyId)
        => Holds(new Claim(ClaimKind.PolicyIssued, orgId, policyId));

    public double BelievesVulnerable(string targetId)
        => Confidence(new Claim(ClaimKind.TargetIsVulnerable, targetId));

    public double BelievesRefusing(string targetId)
        => Confidence(new Claim(ClaimKind.BusinessRefusesTribute, targetId));
}
