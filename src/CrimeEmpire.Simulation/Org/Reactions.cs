namespace CrimeSim.Org;

using CrimeSim.Domain;
using CrimeSim.Sim;

/// <summary>
/// What a man reads off the face of the man he has just spoken to — milestone 026.
///
/// <b>A channel, not a disclosure.</b> `DESIGN_DECISIONS.md` settles that a listener's reaction to
/// an account is his own state: the conflict is perceived on his side, never detected, and the
/// speaker's relationship "does not move, unless he separately observes a response". This is that
/// response. The speaker acquires an <see cref="Impression"/> on his relationship toward the
/// listener — what the listener <em>seemed</em> to make of it — and the impression can be wrong.
/// Nothing here reads <see cref="Report.Candor"/> or a claim's <c>ActualBasis</c>: the truth a read
/// is drawn against is the listener's own position after the exchange, or his own fear, assembled
/// from his side exactly as <see cref="AccountConflict"/> is.
///
/// <b>The read is a roll, keyed like every other.</b> The speaker's Investigation — the skill
/// <c>Runner.Observe</c> already uses for noticing — against the listener's Discretion, the skill
/// concealment already uses for hiding. A clean read returns the truth; a failed one is wrong some of
/// the time and blank the rest. Wrong is reachable in the fixture, not merely possible: Vincent reads
/// people at 0.10 and Salvatore hides at 0.65.
///
/// <b>Every account is given in person</b> (ruling a): the model has no messengers, so every
/// <see cref="Report"/> and every tribute demand is one man in front of another, and the read runs
/// on all of them. A delegated demand is read by the executor — the man in the room — and never by
/// the owner who sent him, which milestone 024 settled for progress and holds here for faces.
///
/// <b>Nothing reads an impression yet</b> (ruling d). It lands in an NPC's relationship exactly as it
/// lands in the player's, and no generator or score consults it. Recorded rather than argued away.
/// </summary>
public static class Reactions
{
    /// <summary>
    /// Above this the listener's fear shows on his face — the same threshold the roster uses before
    /// it says "wary", so a man who reads as frightened is a man the roster would call wary of you.
    /// </summary>
    public const double FearVisibleAbove = 0.25;

    /// <summary>The share of failed reads that come back wrong rather than blank.</summary>
    public const double WrongReadChance = 0.15;

    /// <summary>
    /// How often the reader gets it right. Even skills read cleanly a little more often than not;
    /// a sharp reader against a poor hider nearly always; a poor reader against a good hider rarely,
    /// but never never.
    /// </summary>
    public static double CleanReadChance(double reader, double hider)
        => Math.Clamp(0.55 + 0.6 * (reader - hider), 0.15, 0.95);

    /// <summary>
    /// After an account is delivered: what the recipient seemed to make of what was put to him.
    ///
    /// The exchange is about the question answered, or failing that the claim that actually landed
    /// — one he pushed back on, one he took as news, in that order — and only then whatever the
    /// report led with. The first version took the first assertion, and a capo who opened his report
    /// by repeating the boss's own rule back to him came away with a reading of how the boss took
    /// his own rule. A report that asserts nothing about it — a man saying he knows nothing — puts
    /// nothing to the listener to take or not, and leaves no impression. "Taken" means the
    /// listener's own position, after <see cref="Cognition.Receive"/> has had its say, points the
    /// way the assertion pointed.
    /// </summary>
    public static void AfterReport(
        World world, Character sender, Character recipient, Report report,
        IReadOnlyList<(ReportedClaim Claim, Receipt Receipt)> receipts)
    {
        Claim? aboutOrNull = report.AnsweringClaim ?? Landed(receipts);
        if (aboutOrNull is not { } about) return;

        ReportedClaim? assertionOrNull = null;
        foreach (var a in report.Asserted)
            if (a.Claim.Equals(about)) { assertionOrNull = a; break; }
        if (assertionOrNull is not { } assertion) return;

        bool affirmed = assertion.AssertedStance is Stance.Knows or Stance.Believes or Stance.Suspects;
        var position = recipient.Cognition.Find(about);
        bool taken = position is not null && position.IsHeld == affirmed;

        // Keyed on the exchange itself, never on the report's id: an unrelated report inserted
        // elsewhere in the run must not reroll this one (milestone 005's rule).
        var rng = Rng.ForOccasion(world.Seed,
            $"reaction|report|{sender.Id}|{recipient.Id}|{report.At:O}|{about.Kind}|{about.Subject}|{about.Object}");

        var read = Read(
            rng,
            sender.Capabilities[Skill.Investigation],
            recipient.Capabilities[Skill.Discretion],
            taken ? ImpressionKind.SeemedConvinced : ImpressionKind.SeemedUnconvinced,
            taken ? ImpressionKind.SeemedUnconvinced : ImpressionKind.SeemedConvinced);

        Relations.RecordImpression(sender, recipient.Id, new Impression(read, about, report.At, report.Id));
    }

    /// <summary>
    /// The claim an exchange was really about, from the listener's receipts: something he pushed
    /// back on, else something that was news to him, else something he already held and heard
    /// again from a new voice, else what the report led with. News outranks corroboration because
    /// a capo repeating the boss's own rule back to him corroborates it — the boss holds it — and
    /// the first version of this read how the boss took his own rule.
    ///
    /// <b>"News" is <see cref="Receipt.IsNews"/>, never <c>AcquiredAt == at</c>.</b> A correction
    /// found alongside milestone 026's playtest: a claim he already held can have been acquired on
    /// this exact date through some other channel entirely, coincidentally equal to this report's own
    /// timestamp — the equality test could not tell "created just now, by this call" from "already
    /// on the books, dated today by chance", and a pre-existing claim with that coincidence could
    /// out-rank the genuinely fresh one in this same delivery. <c>Receipt.IsNews</c> is set only by
    /// the one branch of <see cref="Cognition.Receive"/> that actually finds no prior record, so it
    /// says what happened in this call rather than what the calendar happens to show.
    /// </summary>
    private static Claim? Landed(IReadOnlyList<(ReportedClaim Claim, Receipt Receipt)> receipts)
    {
        if (receipts.Count == 0) return null;
        foreach (var (claim, receipt) in receipts)
            if (receipt.Conflict is not null) return claim.Claim;
        foreach (var (claim, receipt) in receipts)
            if (receipt.IsNews) return claim.Claim;
        foreach (var (claim, receipt) in receipts)
            if (receipt.Agreement is not null) return claim.Claim;
        return receipts[0].Claim.Claim;
    }

    /// <summary>
    /// After a demand backed by a threat or by force: whether the shopkeeper seemed frightened.
    /// Read by the man who made the demand, against the shopkeeper's fear of <em>him</em>.
    /// </summary>
    public static void AfterDemand(World world, Character executor, Character owner, string occasionKey, DateTime at)
    {
        bool frightened = owner.Social.Toward(executor.Id).Fear > FearVisibleAbove;
        var rng = Rng.ForOccasion(world.Seed, occasionKey);

        var read = Read(
            rng,
            executor.Capabilities[Skill.Investigation],
            owner.Capabilities[Skill.Discretion],
            frightened ? ImpressionKind.SeemedFrightened : ImpressionKind.SeemedUnmoved,
            frightened ? ImpressionKind.SeemedUnmoved : ImpressionKind.SeemedFrightened);

        Relations.RecordImpression(executor, owner.Id, new Impression(read, null, at));
    }

    /// <summary>One draw: the truth, the opposite, or nothing at all.</summary>
    internal static ImpressionKind Read(
        Rng rng, double reader, double hider, ImpressionKind truth, ImpressionKind opposite)
    {
        double clean = CleanReadChance(reader, hider);
        double u = rng.NextDouble();
        if (u < clean) return truth;
        if (u < clean + WrongReadChance) return opposite;
        return ImpressionKind.GaveNothingAway;
    }
}
