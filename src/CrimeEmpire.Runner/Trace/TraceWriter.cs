namespace CrimeSim.Trace;

using System.Text;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Sim;

/// <summary>
/// Renders developer decision traces. Development builds only — this shows the true trigger, the
/// real score components and the actual chosen strategy, which is precisely the hidden state a
/// player must not be handed.
/// </summary>
public static class TraceWriter
{
    public static string Render(World world, string variant, bool full)
    {
        var sb = new StringBuilder();

        sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
        sb.AppendLine($" CRIMINAL EMPIRE — behavioural spike");
        sb.AppendLine($" seed {world.Seed} · variant \"{variant}\" · {world.Decisions.Count} decisions · {world.TruthLog.Count} events");
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
        sb.AppendLine();

        sb.AppendLine("WHAT HAPPENED (authoritative world log — the developer's view, not the player's)");
        sb.AppendLine();
        foreach (var e in world.TruthLog)
        {
            sb.AppendLine($"  {e.At:yyyy-MM-dd HH:mm}  {e.Summary}");
            foreach (var t in e.Traces)
                sb.AppendLine($"                     ↳ trace: {t.Description} (discoverability {t.Discoverability:0.00})");
        }
        sb.AppendLine();

        sb.AppendLine("DECISION TRACES");
        sb.AppendLine();
        foreach (var d in world.Decisions)
            sb.Append(RenderDecision(d, full));

        sb.AppendLine(Summary(world));
        return sb.ToString();
    }

    public static string RenderDecision(DecisionRecord d, bool full)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"── {d.At:yyyy-MM-dd HH:mm} · {d.ActorName} " + new string('─', Math.Max(0, 46 - d.ActorName.Length)));
        sb.AppendLine($"   woke because   {d.Trigger}  [{d.TriggerKind}]");
        sb.AppendLine($"   what mattered  {d.Agenda.Description} — {d.Agenda.Reason}");

        if (d.BeliefsUsed.Count > 0)
        {
            sb.AppendLine("   what he knew");
            foreach (var b in d.BeliefsUsed)
                sb.AppendLine($"                  {b.Stance} {b.Claim} — {b.ConfidenceLabel}, via {b.SourceKind.Label()}:{b.SourceId}");
        }

        if (d.Scored.Count > 0)
        {
            sb.AppendLine("   weighed up");
            foreach (var s in d.Scored)
            {
                bool chosen = ReferenceEquals(s, d.Chosen);
                sb.AppendLine($"                  {s.Candidate.Description,-52} {s.Total,6:0.00}{(chosen ? "   ← chosen" : "")}");
            }
        }

        var neverOccurred = d.Rejected.Where(r => r.Stage == RejectionStage.Salience).ToList();
        var ruledOut = d.Rejected.Where(r => r.Stage != RejectionStage.Salience).ToList();

        if (ruledOut.Count > 0)
        {
            sb.AppendLine("   ruled out");
            foreach (var r in ruledOut)
                sb.AppendLine($"                  {r.Candidate.Description,-52} {r.Reason}");
        }

        if (full && neverOccurred.Count > 0)
        {
            sb.AppendLine("   never occurred to him");
            foreach (var r in neverOccurred)
                sb.AppendLine($"                  {r.Candidate.Description,-52} {r.Reason}");
        }

        // The prose form from the architecture document. This is the line that decides whether the
        // whole approach is working: it should read as a motive, not as an audit trail.
        if (d.Chosen is { } c)
        {
            sb.AppendLine();
            sb.AppendLine($"   {d.ActorName} {Past(c.Candidate)} because:");
            foreach (var comp in c.Significant())
                sb.AppendLine($"     · {comp.Explanation} ({comp.Name} {comp.Value:+0.00;-0.00})");

            var missed = d.Rejected.FirstOrDefault(r => r.Stage == RejectionStage.Knowledge);
            if (missed is not null)
                sb.AppendLine($"     · and {missed.Reason.ToLowerInvariant()}");

            AppendRelationshipDiagnostic(sb, d);

            sb.AppendLine($"   → {d.Outcome}");
            if (d.Reconsideration.Count > 0)
                sb.AppendLine($"   → he will think again if: {string.Join("; ", d.Reconsideration)}");
        }
        else
        {
            sb.AppendLine($"   → {d.Outcome}");
        }

        sb.AppendLine();
        return sb.ToString();
    }

    /// <summary>
    /// The relationship channel for this decision. DEVELOPER-FACING — milestone 008, ruling 8.
    ///
    /// Three things the reason list above cannot say, all of which milestone 007's central finding
    /// turned out to need:
    ///
    ///  - <b>gross as well as net.</b> A partial report to a superior carries two loyalty
    ///    considerations pulling opposite ways, `+0.7` for the standing reporting buys and `-0.5` for
    ///    what an omission costs. Net they are `0.2 * loyalty`. Only the net was ever visible, so a
    ///    large pair that nearly cancels was indistinguishable from a character who barely weighed
    ///    the relationship at all.
    ///  - <b>no cutoff.</b> The reason list drops anything under 0.15. On the 11 May decision the
    ///    007 finding was measured on, both halves fall under it, so the trace printed no
    ///    relationship line at all for the candidate whose relationship contribution was the number
    ///    being reported. A cutoff that hides a cancelling pair hides exactly the cancellation.
    ///  - <b>the counterfactual.</b> What this candidate would score for a man with no relationships,
    ///    and whether the ranking would differ — which is the only form in which "does this matter"
    ///    has an answer.
    ///
    /// Printed only when something actually read relationship state, so decisions the channel does
    /// not touch stay readable.
    /// </summary>
    private static void AppendRelationshipDiagnostic(StringBuilder sb, DecisionRecord d)
    {
        if (d.Scored.Count == 0) return;
        if (!d.Scored.Any(s => s.RelationshipGross() > 1e-9)) return;

        var counterfactualWinner = d.Scored
            .OrderByDescending(s => s.TotalWithoutRelationships())
            .ThenBy(s => s.Candidate.Id, StringComparer.Ordinal)
            .First();
        bool channelDecided = !ReferenceEquals(counterfactualWinner, d.Scored[0]);

        sb.AppendLine("   relationship channel (developer diagnostic; no cutoff applied)");

        // The chosen candidate and its nearest rival. Enough to see what the channel did to the
        // decision without printing the whole set.
        foreach (var s in d.Scored.Take(2))
        {
            double margin = s.Total - d.Scored[0].Total;
            sb.AppendLine(
                $"     {s.Candidate.Id,-46} gross {s.RelationshipGross(),7:0.0000}" +
                $"  net {s.RelationshipNet(),8:+0.0000;-0.0000;0.0000}" +
                $"  margin {margin,8:+0.0000;-0.0000;0.0000}" +
                $"  without-relationships {s.TotalWithoutRelationships(),7:0.0000}");

            foreach (var c in s.RelationshipComponents())
                sb.AppendLine(
                    $"       [{c.Reads,-33}] {c.Value,8:+0.0000;-0.0000}" +
                    $" (relational {c.RelationshipShare,8:+0.0000;-0.0000;0.0000})  {c.Explanation}");
        }

        if (channelDecided)
            sb.AppendLine(
                $"     ⚠ the relationship channel decided this: without it, " +
                $"\"{counterfactualWinner.Candidate.Id}\" would have won.");
    }

    private static string Past(Candidate c) => c.Kind switch
    {
        ActionKind.StartStrategy => $"chose to {c.Description}",
        ActionKind.ContinueStrategy => "carried on",
        ActionKind.AlterStrategy => $"chose to {c.Description}",
        ActionKind.DelegateStrategy => $"chose to {c.Description}",
        ActionKind.AbandonStrategy => "dropped it",
        ActionKind.ReportToSuperior => "reported in",
        ActionKind.SeekApproval => "asked permission",
        ActionKind.Retaliate => $"chose to {c.Description}",
        ActionKind.Concede => "gave in",
        ActionKind.Refuse => "refused",
        _ => "did nothing",
    };

    public static string Summary(World world)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WHERE THINGS STOOD AT THE END");
        sb.AppendLine();

        foreach (var c in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            sb.AppendLine($"  {c.Name} ({c.RoleTitle})");
            sb.AppendLine($"     doing        {c.Execution.Intention ?? "nothing in particular"}");
            var pressures = c.Motivations.Pressures.Where(p => p.Value > 0.05)
                .OrderByDescending(p => p.Value).ThenBy(p => p.Key)
                .Select(p => $"{p.Key} {p.Value:0.00}");
            sb.AppendLine($"     carrying     {(pressures.Any() ? string.Join(", ", pressures) : "nothing pressing")}");
            sb.AppendLine($"     believes     {c.Cognition.Records.Count(r => r.IsHeld)} things");
            if (c.Social.Grievances.Any())
                sb.AppendLine($"     resents      {string.Join("; ", c.Social.Grievances.Select(g => $"{g.AgainstId} ({g.Description})"))}");
        }

        sb.AppendLine();
        foreach (var b in world.Businesses.Values.OrderBy(b => b.Id, StringComparer.Ordinal))
            sb.AppendLine($"  {b.Name}: {(b.PayingTribute ? "paying" : "not paying")}, resistance {b.Resistance:0.00}{(b.Damaged ? ", wrecked" : "")}");

        sb.AppendLine();
        foreach (var (cond, v) in world.Org.Conditions.OrderBy(k => k.Key))
            sb.AppendLine($"  {world.Org.Name} — {cond}: {v:0.00}");

        return sb.ToString();
    }
}
