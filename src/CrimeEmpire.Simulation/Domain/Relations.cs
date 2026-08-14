namespace CrimeSim.Domain;

/// <summary>
/// How one character stands toward another — the read surface, and the only one there is.
///
/// Every property here is get-only, and the class that implements it is private to
/// <see cref="Relations"/>, so no code outside that class can name the concrete type, construct one,
/// or move a dimension. Read paths take this interface and cannot do anything else with it.
///
/// Loyalty is deliberately absent. It is derived in Decision/Utility.cs from trust, obligation and
/// the Belonging drive, because "loyal" collapses distinctions — attachment versus obligation versus
/// fear of consequence — that should behave differently.
///
/// `Affection` was removed in milestone 006. It had been declared since the first commit and was
/// never read or written by anything in the simulation, the runner, or the tests. Nothing was
/// invented to preserve it: a dimension that cannot name a behavioural purpose does not belong,
/// which is the rule DESIGN_DECISIONS.md already applied when it closed the trait vocabulary.
/// </summary>
public interface IRelationship
{
    string OtherId { get; }
    double Trust { get; }
    double Fear { get; }

    /// <summary>Sense of owing this person — favours, patronage, formal subordination.</summary>
    double Obligation { get; }

    /// <summary>What this character holds against that one, in the order it accumulated.</summary>
    IReadOnlyList<Grievance> Grievances { get; }

    double GrievanceWeight { get; }
}

/// <summary>
/// The one place relationship state is created or changed.
///
/// Before this existed, relationship dimensions were public settable fields and four scattered sites
/// moved them — two `Fear +=` inside a strategy, two grievance `Add` calls in unrelated files —
/// while `Trust` and `Obligation` were written only by the scenario builder and never moved again
/// for the rest of a run. That is not a schema; it is four conventions that happened to agree.
///
/// <b>How the exclusivity is enforced.</b> The implementing type is a <c>private sealed class</c>
/// nested here. C#'s accessibility rules do the work: outside this class the type cannot be named,
/// so it cannot be constructed, subclassed, or cast to. Everyone else holds an
/// <see cref="IRelationship"/>, which exposes no way to change anything. This is deliberately not an
/// `internal` setter guarded by a reflection test — milestone 004 ended on the observation that a
/// guarantee which is a property of the type survives a refactor while a convention about how to
/// call something does not, and an `internal` mutator is a convention with a compiler-shaped hat on.
///
/// Two consequences worth stating because they are easy to undo by accident:
///
///  - <b>Reading never creates.</b> <see cref="SocialState.Toward"/> hands back a shared zero-valued
///    reading when there is no relationship, and only the routes below create one. Scoring reads a
///    great many relationships that do not exist; once relationship state entered the replay
///    comparison, a get-or-create read would have made the act of scoring change the snapshot.
///  - <b>Grievances live on the relationship.</b> A grievance is directed at somebody by definition,
///    so `AgainstId` was always a relationship key wearing a different name. Holding them here means
///    they cannot be added behind this API, and it makes <see cref="SocialState.GrievanceAgainst"/>
///    a local sum rather than a scan.
/// </summary>
public static class Relations
{
    private sealed class Relationship : IRelationship
    {
        private readonly List<Grievance> _grievances = new();

        public Relationship(string otherId) => OtherId = otherId;

        public string OtherId { get; }

        // Plain setters are safe here precisely because the type is private to Relations. Nothing
        // outside this class can obtain a reference of this type to call them on.
        public double Trust { get; set; }
        public double Fear { get; set; }
        public double Obligation { get; set; }

        public IReadOnlyList<Grievance> Grievances => _grievances;

        public double GrievanceWeight
        {
            get
            {
                double sum = 0;
                foreach (var g in _grievances) sum += g.Severity;
                return sum;
            }
        }

        public void Add(Grievance g) => _grievances.Add(g);
        public void ClearGrievances() => _grievances.Clear();

        public override string ToString()
            => $"→{OtherId} trust {Trust:0.00} fear {Fear:0.00} obligation {Obligation:0.00}"
               + (_grievances.Count == 0 ? "" : $" grievance {GrievanceWeight:0.00}");
    }

    /// <summary>
    /// The reading returned for somebody this character has no relationship with: zero on every
    /// dimension, which is what "no relationship" means, and identical in scoring to the empty
    /// record a get-or-create read would have inserted.
    /// </summary>
    internal static readonly IRelationship None = new Relationship("");

    /// <summary>Creates an empty relationship for storage. Called only by <see cref="SocialState.Ensure"/>.</summary>
    internal static IRelationship Create(string otherId) => new Relationship(otherId);

    /// <summary>
    /// The stored relationship as something this class can move.
    ///
    /// The cast cannot fail for anything <see cref="Create"/> produced, and the sentinel check is a
    /// fail-closed guard rather than an expected path: every mutating route below goes through
    /// <see cref="SocialState.Ensure"/>, which always returns a stored instance.
    /// </summary>
    private static Relationship Writable(IRelationship rel)
    {
        if (ReferenceEquals(rel, None))
            throw new InvalidOperationException(
                "The shared zero-valued relationship reading is not mutable. It is returned for a " +
                "person the character has no relationship with; a mutation must establish a real " +
                "relationship first, which every route in Relations does.");

        return rel as Relationship
               ?? throw new InvalidOperationException(
                   $"Relationship toward '{rel.OtherId}' was not created by Relations.Create. " +
                   "Relationship state must not be supplied from outside this class.");
    }

    private static double Clamp(double v) => Math.Clamp(v, 0.0, 1.0);

    // ---------------------------------------------------------------- provisional tuning
    /// <summary>
    /// PROVISIONAL TUNING, not a derived figure. What a single fresh account conflict costs in
    /// trust, before it is scaled by how hard the disagreement was.
    ///
    /// It sits alongside the `FirstHandTestimony` suspicion discount of 0.15 and the `Discovery`
    /// discount of 0.10 as a number chosen to be the right order of magnitude and nothing more.
    /// Nothing yet distinguishes it behaviourally from 0.25 or 0.45, and the milestone that
    /// introduced it deliberately did not tune it to make any particular scenario outcome occur —
    /// see CURRENT_MILESTONE.md rulings 7 and 14.
    /// </summary>
    public const double ConflictTrustCost = 0.35;

    // ---------------------------------------------------------------- the conflict consequence
    /// <summary>
    /// Applies the social consequence of a perceived account conflict: the listener trusts the
    /// speaker less.
    ///
    /// <b>Perceived, not detected.</b> Nothing here knows or can know whether the speaker lied. A
    /// conflict is somebody asserting, to this character, the opposite of a position this character
    /// holds — which may be deception, sincere disagreement, faulty memory, or this character having
    /// been wrong in the first place. The distinction is not drawn here and must not be: this method
    /// takes an <see cref="AccountConflict"/>, which is assembled entirely from the listener's side
    /// of the exchange, and so has no access to the truth log, the report log,
    /// <c>ReportedClaim.ActualBasis</c>, or <c>Report.Candor</c>. That is enforced by the argument
    /// type rather than by discipline.
    ///
    /// <b>Directional.</b> Only the listener's relationship toward the speaker moves. The speaker is
    /// not told he was disbelieved and his own relationship is untouched; if he ever learns of it, he
    /// learns through the same channels as anything else.
    ///
    /// <b>One rule, whatever the prior was.</b> A conflict against something he saw himself and one
    /// against something he was told cost the same socially. The epistemic difference is already
    /// charged in <see cref="Cognition.Receive"/>, which erodes an unmediated record at 0.15 and a
    /// testimonial one at 0.45 and protects the stance of the former; charging it again here would
    /// bill the same distinction twice. The conflict carries the prior's provenance regardless, so a
    /// later evidence-led pass can weight on it without having to reconstruct what was dropped.
    ///
    /// <b>No grievance.</b> Being contradicted costs trust and raises nothing else. A conflict is not
    /// evidence of a wrong — it is two accounts that do not fit, and the man on the other end may
    /// simply be mistaken.
    /// </summary>
    public static void RecordAccountConflict(Character listener, AccountConflict conflict)
    {
        var rel = Writable(listener.Social.Ensure(conflict.SpeakerId));
        // Scaled by how hard the disagreement was, from the listener's side only: how firmly he held
        // the position, times how firmly it was contradicted. Both are actor-visible.
        rel.Trust = Clamp(rel.Trust - ConflictTrustCost * conflict.Strength);
    }

    // ---------------------------------------------------------------- ordinary movement
    /// <summary>Somebody frightened him. Used by coercion resolution.</summary>
    public static void Frighten(Character subject, string ofId, double delta)
    {
        var rel = Writable(subject.Social.Ensure(ofId));
        rel.Fear = Clamp(rel.Fear + delta);
    }

    /// <summary>He now holds something against that person.</summary>
    public static void RaiseGrievance(Character subject, Grievance grievance)
        => Writable(subject.Social.Ensure(grievance.AgainstId)).Add(grievance);

    // ---------------------------------------------------------------- scenario construction
    /// <summary>
    /// Establishes a starting relationship. Scenario construction only.
    ///
    /// Routed through this API rather than reaching past it, so there is one door rather than two. A
    /// second seeding path able to set dimensions directly would be the natural place for ad-hoc
    /// mutation to reappear without anybody noticing.
    /// </summary>
    public static void Establish(
        Character subject, string towardId, double trust = 0, double obligation = 0, double fear = 0)
    {
        var rel = Writable(subject.Social.Ensure(towardId));
        rel.Trust = Clamp(trust);
        rel.Obligation = Clamp(obligation);
        rel.Fear = Clamp(fear);
    }

    /// <summary>Drops what this character holds against that one. Scenario construction only.</summary>
    public static void ClearGrievancesAgainst(Character subject, string otherId)
    {
        if (subject.Social.Existing(otherId) is { } rel) Writable(rel).ClearGrievances();
    }
}
