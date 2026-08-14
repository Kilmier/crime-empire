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
            if (c.Social.Grievances.Count > 0)
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
