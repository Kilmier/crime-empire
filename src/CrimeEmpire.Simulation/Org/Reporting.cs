namespace CrimeSim.Org;

using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Sim;

/// <summary>
/// The organisational report channel: the one explicit route by which what a character holds
/// becomes something another character has been told.
///
/// Two rules hold everything else up.
///
/// First, a sender can only assert claims he actually holds, or the negation of one — he cannot
/// pass on a fact he does not have. INFORMATION_AND_LEGIBILITY.md states this as a success
/// criterion ("Sources never communicate facts unavailable to them unless they are inventing or
/// inferring those facts"), and composing the report from the sender's own perceived situation
/// rather than from world state is what enforces it structurally rather than by discipline.
///
/// Second, delivery goes through <see cref="Cognition.Receive"/>, never <c>Learn</c>. Being told
/// something is not the same event as finding it out, and the recipient is entitled to disbelieve.
/// </summary>
public static class Reporting
{
    /// <summary>How much of his own certainty survives being restated to somebody else.</summary>
    private const double SecondHand = 0.75;

    /// <summary>How confidently a man denies something he knows perfectly well happened.</summary>
    private const double DenialConfidence = 0.80;

    /// <summary>Most claims one report will carry. A report is not a memory dump.</summary>
    private const int MaxAsserted = 3;

    /// <summary>
    /// When this recipient was last actually told this character's position on this claim.
    ///
    /// Per claim, deliberately, rather than per report. Keying "has he already covered this" off
    /// the timestamp of his last report treated everything as handled the moment he said anything
    /// at all — so a retraction that lost its place to the three-item cap was dropped and then
    /// marked as delivered, which is the worst of both.
    /// </summary>
    public static DateTime? LastConveyed(
        IEnumerable<Report> reportsSent, string recipientId, Claim claim)
        => Latest(reportsSent, recipientId, r => r.Asserted.Any(a => a.Claim.Equals(claim)));

    /// <summary>
    /// When he last deliberately kept this from that recipient.
    ///
    /// Withholding is a decision *about* the claim, not an absence of one. Keeping it separate
    /// from having said it is what lets the three cases stay apart: told, deliberately not told,
    /// and never reached.
    /// </summary>
    public static DateTime? LastWithheld(
        IEnumerable<Report> reportsSent, string recipientId, Claim claim)
        => Latest(reportsSent, recipientId, r => r.Withheld.Any(w => w.Equals(claim)));

    /// <summary>
    /// When he last did anything with this claim toward that recipient — said it or chose not to.
    ///
    /// This is the one report eligibility keys off, and the distinction it draws is the fix for a
    /// report that repeated weekly forever. Three cases, and only two of them are outstanding:
    ///
    ///  - <b>asserted</b> — he told him. Settled until his own position changes.
    ///  - <b>withheld</b> — he considered it and decided to keep it back. Also settled: that
    ///    decision stands until something changes his mind, and treating a suppressed claim as
    ///    permanently unsaid meant it stayed "news" forever, re-arming reporting every time and
    ///    producing thirteen identical partial accounts.
    ///  - <b>neither</b> — it never reached the page, because the length cap cut it. Genuinely
    ///    outstanding, and must stay so.
    ///
    /// Note this makes concealment self-limiting in the right way: a man who has decided to keep
    /// something back stops bringing the subject up, and starts again only if his position on it
    /// moves.
    /// </summary>
    public static DateTime? LastAddressed(
        IEnumerable<Report> reportsSent, string recipientId, Claim claim)
    {
        var said = LastConveyed(reportsSent, recipientId, claim);
        var kept = LastWithheld(reportsSent, recipientId, claim);
        if (said is null) return kept;
        if (kept is null) return said;
        return said > kept ? said : kept;
    }

    /// <summary>Whether this position is news — never addressed, or addressed and since changed.</summary>
    public static bool NeedsConveying(
        IEnumerable<Report> reportsSent, string recipientId, InformationRecord belief)
    {
        var addressed = LastAddressed(reportsSent, recipientId, belief.Claim);
        return addressed is null || belief.ReconsideredAt > addressed;
    }

    private static DateTime? Latest(
        IEnumerable<Report> reportsSent, string recipientId, Func<Report, bool> mentions)
    {
        DateTime? latest = null;
        foreach (var r in reportsSent)
        {
            if (r.RecipientId != recipientId || !mentions(r)) continue;
            if (latest is null || r.At > latest) latest = r.At;
        }
        return latest;
    }

    /// <summary>
    /// Whether this suppressed claim would actually produce a denial.
    ///
    /// Mirrors the two conditions the composition loop applies: the claim has to survive the
    /// answering scope, and the sender has to hold it — there is nothing to contradict otherwise.
    /// Reads <see cref="PerceivedSituation.Beliefs"/> rather than an accessor on purpose, because
    /// this is a structural check on the candidate and must not register as the character having
    /// consulted anything.
    /// </summary>
    private static bool WouldDeny(Claim claim, Candidate candidate, PerceivedSituation perceived)
        => (candidate.AnsweringClaim is not { } asked || claim.Equals(asked))
           && perceived.Beliefs.Any(b => b.Claim.Equals(claim) && b.IsHeld);

    public static Report Compose(
        World world,
        Character sender,
        Character recipient,
        Candidate candidate,
        PerceivedSituation perceived)
    {
        var candor = candidate.Candor ?? ReportCandor.Candid;
        var suppressed = candidate.Suppressed;

        // A false report has to contain a lie. Not "was asked for", not "was labelled" — has to
        // actually emit a denial of something the sender privately holds, once the answering scope
        // and the held-position rule below have had their say.
        //
        // Without this the label and the content could disagree. A candidate marked False whose
        // suppressed claim the sender does not hold fell through to the honest branch, emitted his
        // real rejection, and still came back stamped ReportCandor.False with lying framing — a
        // sincere man recorded as a liar by a field nothing had checked. The generator no longer
        // offers such a candidate, but a rule that lives only in the generator is one refactor away
        // from being lost, and Candor is read by the developer trace and the replay snapshot.
        //
        // Rejected rather than quietly relabelled to Candid. Silently correcting the caller would
        // hide a real inconsistency in whoever built the candidate, and SIMULATION_ARCHITECTURE.md
        // asks for exactly the opposite: fail visibly in development rather than silently
        // corrupting history.
        if (candor == ReportCandor.False && !suppressed.Any(c => WouldDeny(c, candidate, perceived)))
        {
            throw new ArgumentException(
                $"Report candour is False but nothing would be denied. {sender.Id} -> {recipient.Id}, " +
                $"candidate '{candidate.Id}', suppressed [{string.Join(", ", suppressed)}]" +
                (candidate.AnsweringClaim is { } scope ? $", answering {scope}" : "") +
                ". A false report must assert the opposite of a position the sender holds; if he " +
                "holds no such position, the honest report is the only one available to him.",
                nameof(candidate));
        }

        // What he has to offer: anything he has a position on, held or explicitly disbelieved,
        // with whatever is *news* to this recipient first.
        //
        // Ordering held positions first — the previous rule — quietly reinstated the bug it was
        // meant to fix. A man with three standing beliefs filled the report with them every time,
        // so a retraction never reached the page, while the report itself marked the matter
        // covered. News leads instead: something never said, or said and since changed, outranks
        // another restatement of what the recipient already has. Within that, held positions come
        // before disavowals, because "here is what I have" is the substance and "that other thing
        // was wrong" is the correction to it.
        //
        // Ordering is explicit rather than incidental because report content feeds the determinism
        // snapshot.
        var sent = world.Reports.Where(r => r.SenderId == sender.Id).ToList();

        // An answer is confined to the question. Without this a man asked about one specific thing
        // composes the same general account as a man volunteering one, so the request's subject
        // never reaches the reply and "did he answer what I asked" has no answer in the data.
        //
        // Note the filter runs before the cap, not after: a claim that is the entire point of the
        // report must not lose its place to two more newsworthy ones.
        var positions = perceived.Beliefs
            .Where(b => candidate.AnsweringClaim is not { } asked || b.Claim.Equals(asked))
            .OrderByDescending(b => NeedsConveying(sent, recipient.Id, b))
            .ThenByDescending(b => b.IsHeld)
            .ThenByDescending(b => b.Confidence)
            .ThenBy(b => b.Claim.ToString(), StringComparer.Ordinal)
            .ToList();

        var asserted = new List<ReportedClaim>();
        var withheld = new List<Claim>();

        foreach (var b in positions)
        {
            bool awkward = suppressed.Any(s => s.Equals(b.Claim));

            if (awkward && candor == ReportCandor.Partial)
            {
                // Simple omission. Everything he does say is true, which is exactly what makes a
                // partial account harder to catch than a lie.
                withheld.Add(b.Claim);
                continue;
            }

            // Note the IsHeld condition. A lie is the assertion of something contrary to what the
            // sender privately holds, so there has to be something to contradict: if he does not
            // hold the claim, "it did not happen" is simply his position, and dressing it as a
            // denial would record a sincere man as a liar. The generator no longer offers a False
            // candidate in that case, and this is the same rule stated where the report is actually
            // composed, so the two cannot drift apart. A position he does not hold falls through to
            // the honest assertion below, which passes on his rejection as a rejection.
            if (awkward && candor == ReportCandor.False && b.IsHeld)
            {
                // He asserts the opposite of what he holds. The claim is the same claim — this is
                // a denial of a specific proposition, not a change of subject.
                // The basis is still his own. A man denying something he did is lying as a
                // participant, and the listener is right to receive it as first-hand testimony —
                // false first-hand testimony, which is the most convincing kind and exactly what
                // makes concealment worth modelling.
                withheld.Add(b.Claim);
                asserted.Add(new ReportedClaim(b.Claim, Stance.Rejects, DenialConfidence, b.SourceKind));
                continue;
            }

            if (asserted.Count >= MaxAsserted) continue;

            // His actual position, in the direction he actually holds it. Held things are softened
            // to "believes" because restating something to somebody else is second-hand by
            // definition; a position he has come to reject is passed on as a rejection, which is
            // the whole point of letting non-held records into a report. Flattening everything to
            // Believes would turn "I was wrong about that" into "that is true".
            // The speaker's own basis travels with the claim. Without it every account landed as
            // a generic report, so the listener could not tell the man who did it from the man
            // repeating what he had heard — and first-hand testimony could only exist by being
            // fabricated at the point of delivery, which is what it was.
            var stance = b.IsHeld ? Stance.Believes : b.Stance;
            asserted.Add(new ReportedClaim(b.Claim, stance, b.Confidence * SecondHand, b.SourceKind));
        }

        string framing = candor switch
        {
            ReportCandor.Partial => $"{sender.Name} gave {recipient.Name} an account with the worst of it left out",
            ReportCandor.False => $"{sender.Name} told {recipient.Name} it had not happened",
            _ => $"{sender.Name} told {recipient.Name} what he had",
        };

        return new Report(
            world.NextReportId(),
            sender.Id,
            recipient.Id,
            world.Now,
            candor,
            asserted,
            withheld,
            framing);
    }

    /// <summary>
    /// Hands the report to its recipient and files it in the developer truth log.
    ///
    /// The world event summary deliberately describes only the act of reporting, not its honesty.
    /// Whether the account was straight is in <see cref="Report.Candor"/>, which the player-facing
    /// layer never reads.
    /// </summary>
    public static void Deliver(World world, Report report, Character recipient)
    {
        foreach (var claim in report.Asserted)
            recipient.Cognition.Receive(claim, report.SenderId, report.At);

        world.Reports.Add(report);
        world.Record("report", report.SenderId, report.RecipientId, report.Framing);
    }
}
