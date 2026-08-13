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
    /// When this recipient was last told this character's position on this claim, if ever.
    ///
    /// Per claim, deliberately, rather than per report. Keying "has he already covered this" off
    /// the timestamp of his last report treated everything as handled the moment he said anything
    /// at all — so a retraction that lost its place to the three-item cap was dropped and then
    /// marked as delivered, which is the worst of both. Asking about the individual claim makes an
    /// omission stay outstanding until it is actually said.
    /// </summary>
    public static DateTime? LastConveyed(
        IEnumerable<Report> reportsSent, string recipientId, Claim claim)
    {
        DateTime? latest = null;
        foreach (var r in reportsSent)
        {
            if (r.RecipientId != recipientId) continue;
            foreach (var a in r.Asserted)
            {
                if (!a.Claim.Equals(claim)) continue;
                if (latest is null || r.At > latest) latest = r.At;
            }
        }
        return latest;
    }

    /// <summary>Whether this position is news to the recipient — never said, or said and since changed.</summary>
    public static bool NeedsConveying(
        IEnumerable<Report> reportsSent, string recipientId, InformationRecord belief)
    {
        var conveyed = LastConveyed(reportsSent, recipientId, belief.Claim);
        return conveyed is null || belief.ReconsideredAt > conveyed;
    }

    public static Report Compose(
        World world,
        Character sender,
        Character recipient,
        Candidate candidate,
        PerceivedSituation perceived)
    {
        var candor = candidate.Candor ?? ReportCandor.Candid;
        var suppressed = candidate.Suppressed;

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

        var positions = perceived.Beliefs
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

            if (awkward && candor == ReportCandor.False)
            {
                // He asserts the opposite of what he holds. The claim is the same claim — this is
                // a denial of a specific proposition, not a change of subject.
                withheld.Add(b.Claim);
                asserted.Add(new ReportedClaim(b.Claim, Stance.Rejects, DenialConfidence));
                continue;
            }

            if (asserted.Count >= MaxAsserted) continue;

            // His actual position, in the direction he actually holds it. Held things are softened
            // to "believes" because restating something to somebody else is second-hand by
            // definition; a position he has come to reject is passed on as a rejection, which is
            // the whole point of letting non-held records into a report. Flattening everything to
            // Believes would turn "I was wrong about that" into "that is true".
            var stance = b.IsHeld ? Stance.Believes : b.Stance;
            asserted.Add(new ReportedClaim(b.Claim, stance, b.Confidence * SecondHand));
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
