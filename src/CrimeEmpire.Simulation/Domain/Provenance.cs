namespace CrimeSim.Domain;

/// <summary>
/// The properties simulation rules actually care about when they ask where a claim came from.
///
/// These exist so the question is asked once and answered in one place. Before the split there was
/// a single <c>Direct</c> value and four rules compared against it directly; splitting it into four
/// categories would have meant four separate lists of enum members, free to drift apart. A rule
/// that wants "did he establish this himself" should say so, not enumerate.
///
/// Note that <see cref="SourceKind.Inference"/> is deliberately in neither group. A conclusion is
/// not something he was told, and it is not something he established by observation either — it is
/// his own reasoning, defeasible in its own way, and rules that lump it with either are usually
/// wrong about it.
/// </summary>
public static class Provenance
{
    /// <summary>
    /// He established this himself, without going through anyone's account of it.
    ///
    /// This is the property behind resisting contradiction and overriding what he was told
    /// earlier: being told you did not see what you saw is weak evidence, whereas one report
    /// contradicting another is an ordinary conflict of sources.
    /// </summary>
    public static bool IsUnmediated(this SourceKind kind)
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
