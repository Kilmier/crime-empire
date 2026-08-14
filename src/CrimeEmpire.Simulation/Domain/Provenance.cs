namespace CrimeSim.Domain;

/// <summary>
/// One named predicate per behaviour that provenance controls.
///
/// There used to be a single <c>IsUnmediated</c> covering Participant, Witness and Discovery, and
/// four different rules keyed off it: overriding an earlier record, resisting contradiction,
/// keeping a stance below the acting threshold, and escaping the Suspicious discount. Bundling them
/// meant a category could not be given one property without silently inheriting the other three,
/// and Discovery inherited all four when it should have had none of them. Finding a wrecked
/// shopfront the next morning is an *interpretation of a trace*: it can be weak, it can be wrong
/// about who or why, and it should be contestable and reconsiderable like any other reading.
///
/// The memberships below currently coincide, which is not the point. The point is that they are
/// separate questions with separate answers, so changing one cannot quietly change the others.
/// Adding a category means answering four questions rather than one.
///
/// <see cref="SourceKind.Inference"/> sits outside every group here. A conclusion is neither
/// something he was told nor something he established by observation; it is his own reasoning,
/// defeasible in its own way, and rules that lump it with either are usually wrong about it.
/// </summary>
public static class Provenance
{
    /// <summary>
    /// Whether a new record of this kind displaces an earlier one regardless of confidence.
    ///
    /// Doing it or seeing it beats what you were told earlier, however firmly you were told it.
    /// Discovery does not qualify: coming across a consequence is a reading of evidence, and a
    /// reading should not be able to evict a first-hand account simply by arriving later.
    /// </summary>
    public static bool OverridesPriorRecord(this SourceKind kind)
        => kind is SourceKind.Participant or SourceKind.Witness;

    /// <summary>
    /// Whether being contradicted costs this belief less confidence than usual.
    ///
    /// Being told you did not see what you saw is weak evidence. Being told you misread a wrecked
    /// shopfront is not — that is an ordinary disagreement about what the evidence meant, and
    /// Discovery erodes at the normal rate because of it.
    /// </summary>
    public static bool ResistsContradiction(this SourceKind kind)
        => kind is SourceKind.Participant or SourceKind.Witness;

    /// <summary>
    /// Whether the stance survives when confidence falls below the point he would act on it.
    ///
    /// What he did or saw decays in confidence but stays held. An interpreted trace can be argued
    /// down into doubt, which is the behaviour the finding asked for: Discovery must be
    /// contestable, not merely dented.
    /// </summary>
    public static bool ProtectsStance(this SourceKind kind)
        => kind is SourceKind.Participant or SourceKind.Witness;

    /// <summary>
    /// Whether the Suspicious trait leaves this alone.
    ///
    /// Suspicion is doubt about what other people tell you, so doing it and seeing it are exempt.
    /// Discovery is not: a suspicious man second-guesses his own reading of a scene, which is a
    /// different thing from doubting his own eyes.
    /// </summary>
    public static bool ExemptFromSuspicion(this SourceKind kind)
        => kind is SourceKind.Participant or SourceKind.Witness;

    /// <summary>
    /// Descriptive, not behavioural: he came by this himself rather than through anyone's account.
    ///
    /// Used for the structural invariant that such a record must name its holder as its source.
    /// Deliberately not wired to any rule — that is what the bundle used to be.
    /// </summary>
    public static bool IsSelfAcquired(this SourceKind kind)
        => kind is SourceKind.Participant or SourceKind.Witness or SourceKind.Discovery;

    /// <summary>
    /// Somebody told him. Covers a participant's own account as well as a filed report — closeness
    /// to the event changes how much it is discounted, not whether it is testimony.
    ///
    /// This is the property behind "worth seeking a second account of": what he was told can be
    /// checked against another source, and what he established himself cannot be checked that way.
    /// </summary>
    public static bool IsTestimony(this SourceKind kind)
        => kind is SourceKind.FirstHandTestimony or SourceKind.Report or SourceKind.Rumor;

    /// <summary>
    /// How a claim arrives when this speaker passes it on.
    ///
    /// A man who did it or saw it is giving first-hand testimony. Everyone else is passing on
    /// something they did not establish themselves, and that is a report however sincere they are —
    /// including a man relaying what he was told first-hand, because repeating testimony makes it
    /// hearsay. Discovery reports an interpretation, not the event, so it arrives as a report too.
    ///
    /// This is what the listener records. What the speaker actually had is kept verbatim on the
    /// testimony entry, so nothing is lost by the settled belief being coarser.
    /// </summary>
    public static SourceKind AsHeardFrom(this SourceKind speakerBasis)
        => speakerBasis is SourceKind.Participant or SourceKind.Witness
            ? SourceKind.FirstHandTestimony
            : SourceKind.Report;

    /// <summary>Short form for developer traces, where the enum name reads badly.</summary>
    public static string Label(this SourceKind kind) => kind switch
    {
        SourceKind.Participant => "did it",
        SourceKind.Witness => "saw it",
        SourceKind.Discovery => "found it",
        SourceKind.FirstHandTestimony => "first-hand",
        SourceKind.Report => "reported",
        SourceKind.Rumor => "rumour",
        _ => "inferred",
    };
}
