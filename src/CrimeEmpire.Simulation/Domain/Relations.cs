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

    // MILESTONE 021 REMOVED AssessedCoercion FROM HERE, and the reason is the rule this interface
    // is governed by rather than a preference. Milestone 020 added it as a fifth dimension: what the
    // character believed about another's Coercion. Every dimension above is an *attitude*, with no
    // truth value — there is no fact of the matter about how much Vincent trusts Tommy beyond
    // Vincent's own state. An assessment of somebody's skill has a referent, Tommy's actual
    // Capabilities[Skill.Coercion], so it can be *wrong*; and things a character can be wrong about
    // live in Cognition, with a source, a confidence and the ability to be contested and revised,
    // none of which a bare number here can carry. It is now ClaimKind.PersonIsCapable. See
    // docs/RELATIONSHIPS.md, "The vocabulary", which is back to four.

    /// <summary>What this character holds against that one, in the order it accumulated.</summary>
    IReadOnlyList<Grievance> Grievances { get; }

    /// <summary>
    /// Why his standing toward this person moved, in the order it moved — milestone 023.
    ///
    /// Read by the player-facing roster and by nothing that scores. See
    /// <see cref="StandingChange"/> for why this is history rather than a fifth dimension.
    /// </summary>
    IReadOnlyList<StandingChange> StandingHistory { get; }

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
        private readonly System.Collections.ObjectModel.ReadOnlyCollection<Grievance> _readOnly;
        private readonly List<StandingChange> _standingHistory = new();
        private readonly System.Collections.ObjectModel.ReadOnlyCollection<StandingChange> _historyReadOnly;

        public Relationship(string otherId, bool stored)
        {
            OtherId = otherId;
            Stored = stored;
            _readOnly = _grievances.AsReadOnly();
            _historyReadOnly = _standingHistory.AsReadOnly();
        }

        public string OtherId { get; }

        /// <summary>
        /// Whether this instance is the one held in a <see cref="SocialState"/>, as opposed to a
        /// throwaway reading handed back for somebody the character has no relationship with.
        ///
        /// The mutation guard keys off this rather than off reference equality with a shared
        /// sentinel, because absent readings are no longer a single shared object — each one carries
        /// the id that was asked about.
        /// </summary>
        public bool Stored { get; }

        // Plain setters are safe here precisely because the type is private to Relations. Nothing
        // outside this class can obtain a reference of this type to call them on.
        public double Trust { get; set; }
        public double Fear { get; set; }
        public double Obligation { get; set; }

        /// <summary>
        /// Wrapped rather than handed out directly. <c>IReadOnlyList&lt;T&gt;</c> is an interface,
        /// not a guarantee: returning the backing <c>List&lt;Grievance&gt;</c> as one let any caller
        /// cast it straight back and add to it, so the "read-only" surface was read-only by
        /// politeness. The wrapper cannot be cast to the list, and it is built once rather than per
        /// access so a read stays allocation-free.
        /// </summary>
        public IReadOnlyList<Grievance> Grievances => _readOnly;

        public double GrievanceWeight
        {
            get
            {
                double sum = 0;
                foreach (var g in _grievances) sum += g.Severity;
                return sum;
            }
        }

        /// <summary>Wrapped for the same reason <see cref="Grievances"/> is.</summary>
        public IReadOnlyList<StandingChange> StandingHistory => _historyReadOnly;

        public void Remember(StandingChange change) => _standingHistory.Add(change);

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
    ///
    /// A fresh instance per call rather than one shared sentinel, so the reading can carry the id
    /// that was actually asked about. A shared object had to report <c>OtherId = ""</c>, which made
    /// every absent read claim to be about nobody — wrong in itself, and actively misleading to any
    /// caller that logged or grouped by it. It is never stored, so no relationship state is created
    /// by asking.
    /// </summary>
    internal static IRelationship Absent(string otherId) => new Relationship(otherId, stored: false);

    /// <summary>Creates an empty relationship for storage. Called only by <see cref="SocialState.Ensure"/>.</summary>
    internal static IRelationship Create(string otherId) => new Relationship(otherId, stored: true);

    /// <summary>
    /// The stored relationship as something this class can move.
    ///
    /// The cast cannot fail for anything <see cref="Create"/> produced, and the stored check is a
    /// fail-closed guard rather than an expected path: every mutating route below goes through
    /// <see cref="SocialState.Ensure"/>, which always returns a stored instance. Mutating an absent
    /// reading would change an object nothing holds, silently discarding the write.
    /// </summary>
    private static Relationship Writable(IRelationship rel)
    {
        var concrete = rel as Relationship
            ?? throw new InvalidOperationException(
                $"Relationship toward '{rel.OtherId}' was not created by Relations. " +
                "Relationship state must not be supplied from outside this class.");

        if (!concrete.Stored)
            throw new InvalidOperationException(
                $"The absent-relationship reading for '{rel.OtherId}' is not mutable. It is returned " +
                "for a person the character has no relationship with and is held by nobody; a " +
                "mutation must establish a stored relationship first, which every route in " +
                "Relations does.");

        return concrete;
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

    /// <summary>
    /// PROVISIONAL TUNING, not a derived figure. What a single fresh, non-repeated corroborating
    /// account is worth in trust, before it is scaled by how firmly both sides held their positions.
    ///
    /// Milestone 016, ruling 1: deliberately a separate named constant from
    /// <see cref="ConflictTrustCost"/> rather than the same value reused in the positive direction.
    /// The two starting at the same number (0.35) is a provisional symmetric choice and nothing
    /// more — nothing yet distinguishes what a corroboration is worth from what a contradiction
    /// costs, and naming them separately means a later evidence-led pass can move one without
    /// moving the other. Neither value is tuned by the milestone that introduced this one.
    ///
    /// <b><c>static readonly</c>, not <c>const</c> — immutable at runtime, but with a genuine field
    /// identity a test can inspect.</b> A <c>const</c> field is inlined as a literal at every call
    /// site at compile time, so two <c>const</c> fields that happen to share a value — this one and
    /// <see cref="ConflictTrustCost"/>, both `0.35` today — compile to the identical IL and become
    /// indistinguishable afterwards: a defect that reads the wrong one of the two produces no
    /// observable difference at all until the two values diverge. That gap is exactly what let a
    /// review mutation (swapping this constant for <see cref="ConflictTrustCost"/> inside
    /// <see cref="RecordAccountAgreement"/>) pass every existing test. The fix is not to make the
    /// value mutable at runtime — a plain mutable <c>static</c> field was tried and rejected, because
    /// it is process-global state with no persistence or replay story of its own, reachable by any
    /// other test or code running in the same process. `static readonly` is immutable exactly like
    /// `const` from any caller's point of view, but unlike `const` it is a genuine static field with
    /// its own metadata token, so a test can prove which field <see cref="RecordAccountAgreement"/>'s
    /// compiled IL actually references — a structural fact fixed at compile time, not a runtime value
    /// varied by a test. See <c>AccountAgreementTests.cs</c>.
    /// </summary>
    public static readonly double AccountAgreementTrustGain = 0.35;

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
    public static void RecordAccountConflict(Character listener, AccountConflict conflict, DateTime at)
    {
        var rel = Writable(listener.Social.Ensure(conflict.SpeakerId));
        // Scaled by how hard the disagreement was, from the listener's side only: how firmly he held
        // the position, times how firmly it was contradicted. Both are actor-visible.
        rel.Trust = Clamp(rel.Trust - ConflictTrustCost * conflict.Strength);

        // And why, for the roster to say — milestone 023. Written here rather than reconstructed
        // afterwards, from the same listener-side evidence the movement itself came from: this method
        // cannot reach the truth log or the speaker's candour, so what it remembers cannot claim to
        // know he was lied to. It records that he was contradicted, which is all the listener has.
        rel.Remember(new StandingChange(StandingCause.AccountContradicted, at, conflict.Claim));
    }

    // ---------------------------------------------------------------- the agreement consequence
    /// <summary>
    /// Applies the social consequence of a perceived account agreement: the listener trusts the
    /// speaker more.
    ///
    /// Milestone 016. Mirrors <see cref="RecordAccountConflict"/>'s shape, sign reversed, but its
    /// "one rule, whatever the prior was" reasoning does not carry over unchanged — see below.
    /// <b>Perceived, not detected</b> — nothing here knows or can know whether the speaker was
    /// sincere, and nothing here can reach far enough to find out, because <see cref="AccountAgreement"/>
    /// is assembled entirely from the listener's own side of the exchange and carries no reference to
    /// the truth log, the report log, <c>ReportedClaim.ActualBasis</c>, or <c>Report.Candor</c> —
    /// enforced by the argument type, not by discipline. <b>Directional</b> — only the listener's own
    /// relationship toward the speaker moves; the speaker is not told his account landed as support.
    ///
    /// <b>One rule, whatever the prior was — but, unlike <see cref="RecordAccountConflict"/>, not
    /// because the distinction is charged somewhere else.</b> On the conflict side, `Cognition`
    /// genuinely does charge the epistemic difference between direct observation and testimony first
    /// — a testimonial prior erodes faster (0.45) than an unmediated one (0.15) and only the latter's
    /// stance is protected — so re-weighting it here really would bill the same distinction twice.
    /// The agreement branch that produces <see cref="AccountAgreement"/> has no equivalent: the
    /// confidence raise in <see cref="Cognition.Receive"/> is a flat `0.15 × asserted confidence`
    /// regardless of the prior's `SourceKind`. So this method reading only
    /// <see cref="AccountAgreement.Strength"/> is not "the distinction is charged elsewhere" — it is
    /// milestone 016 deliberately applying one flat social rule regardless of provenance, the same
    /// choice the conflict rule makes but for a different and more honest reason: nobody has decided
    /// whether a corroboration from a man who saw it himself should count for more trust than one from
    /// a man who only heard it. `AccountAgreement.PriorSourceKind`/`PriorSourceId` are preserved
    /// anyway, so that decision — if it is ever made — has something to weight against without
    /// reconstructing what this method dropped. See `docs/DESIGN_DECISIONS.md`, "Relationships — the
    /// agreement direction, settled by milestone 016".
    /// </summary>
    public static void RecordAccountAgreement(Character listener, AccountAgreement agreement, DateTime at)
    {
        var rel = Writable(listener.Social.Ensure(agreement.SpeakerId));
        rel.Trust = Clamp(rel.Trust + AccountAgreementTrustGain * agreement.Strength);
        rel.Remember(new StandingChange(StandingCause.AccountCorroborated, at, agreement.Claim));
    }

    // ---------------------------------------------------------------- ordinary movement
    /// <summary>
    /// These two have met. Establishes that the subject knows this person exists, and moves no
    /// dimension.
    ///
    /// <b>Why a relationship record and not a claim.</b> A stored all-zero relationship already means
    /// "we have met and nothing has passed between us" — it is what <see cref="Establish"/> produces
    /// before any dimension is set, and it is why <see cref="SocialState.Others"/> is one of the
    /// inputs to <c>Acquaintance.HeardOf</c>. Making the meeting explicit therefore adds no new
    /// concept; it records something the model was already relying on and had no way to state.
    ///
    /// <b>What it fixes.</b> A man could put a demand to a shopkeeper in his own shop, or a question
    /// to another character, and the person on the receiving end had nothing in his head naming the
    /// man in front of him. Candidate generation then offered him "pay what Vincent Russo is asking"
    /// about somebody the player-facing view would not name. Milestone 009's fourth correction; the
    /// review before it fixed the same defect in the corroboration generator alone.
    ///
    /// Scoring is untouched: every dimension reads zero, which is exactly what
    /// <see cref="Absent"/> reads, so a met-but-otherwise-unconnected person is worth the same to
    /// <c>Utility</c> as a stranger. What changes is that he can be named.
    ///
    /// Deliberately not reciprocal. Relationships are directional, and the two sides of an encounter
    /// do not always both register it — call it once per side that actually noticed.
    /// </summary>
    public static void Meet(Character subject, string otherId)
    {
        if (subject.Id == otherId) return;
        Writable(subject.Social.Ensure(otherId));
    }

    /// <summary>Somebody frightened him. Used by coercion resolution.</summary>
    public static void Frighten(Character subject, string ofId, double delta, DateTime at)
    {
        var rel = Writable(subject.Social.Ensure(ofId));
        double before = rel.Fear;
        rel.Fear = Clamp(rel.Fear + delta);

        // Only when it actually moved. A man already as frightened as the scale allows does not
        // acquire a fresh memory of being frightened again, and recording one would put an entry on
        // the roster that nothing in his state changed to match.
        if (rel.Fear > before) rel.Remember(new StandingChange(StandingCause.Frightened, at));
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
