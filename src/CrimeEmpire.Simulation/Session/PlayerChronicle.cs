namespace CrimeSim.Session;

using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Sim;

public enum ChronicleKind { Order, Account, Income, Ended }
/// <summary>Typed namespaces avoid collisions between decisions, reports and operations.</summary>
public sealed record ChronicleIdentity(ChronicleKind Kind, long SourceId, int Ordinal,
    OperationIdentity? Operation);

/// <summary>Copied meanings. No domain record, raw confidence, truth id, or private report fields.</summary>
public sealed record PlayerChronicleEntry(ChronicleIdentity Id, DateTime At,
    string Subject, string Description)
{
    public ActionKind? Action { get; init; }
    public CoercionMethod? Method { get; init; }
    public string? ExecutorId { get; init; }
    public string? ExecutorName { get; init; }
    public string? SourceId { get; init; }
    public string? SourceName { get; init; }
    public PlayerClaim? Claim { get; init; }
    public Stance? Stance { get; init; }
    public string? Uncertainty { get; init; }
    public SourceKind? ClaimedBasis { get; init; }
    // Reports supply receipt time, not event time. Never substitute the truth-log timestamp.
    public DateTime? KnownEventAt { get; init; }
    public double? Amount { get; init; }
    public bool? MoneyArrived { get; init; }
}

public static partial class PlayerView
{
    private static IReadOnlyList<PlayerChronicleEntry> Chronicle(World world, Character who,
        Func<string, string> name, Pronouns self)
    {
        var entries = new List<PlayerChronicleEntry>();
        foreach (var d in world.Decisions.Where(d => d.ActorId == who.Id))
        {
            if (d.Chosen?.Candidate is not { Strategy: StrategyKind.SecureTribute, TargetId: not null } c
                || c.Kind is not (ActionKind.StartStrategy or ActionKind.ContinueStrategy
                    or ActionKind.AlterStrategy or ActionKind.PostponeStrategy
                    or ActionKind.DelegateStrategy or ActionKind.AbandonStrategy)) continue;
            var operation = c.Kind == ActionKind.StartStrategy ? d.StartedOperation
                : c.OperationOwnerId is { } owner && c.OperationSequence is { } seq
                    ? new OperationIdentity(owner, seq) : null;
            // A delegation candidate's target is its executor. Do not invent the missing business.
            string? executor = c.Kind == ActionKind.StartStrategy
                ? d.ExecutorChoice?.Candidate.InitialExecutorId
                    ?? (d.ExecutorChoice?.Candidate.Kind == ActionKind.StartStrategy ? d.ActorId : c.InitialExecutorId)
                : c.Kind == ActionKind.DelegateStrategy ? c.TargetId : null;
            entries.Add(new PlayerChronicleEntry(new(ChronicleKind.Order, d.Id, 0, operation), d.At,
                name(c.TargetId), PlayerOption.Describe(c, name, self,
                    id => world.Find(id)?.Pronouns ?? Pronouns.He, who.Id))
            { Action = c.Kind, Method = c.Method, ExecutorId = executor,
              ExecutorName = executor is null ? null : name(executor) });
        }
        foreach (var a in who.Execution.OperationAccounts)
        {
            // Quantify only the assertion as received, never the recipient's evolving belief.
            string certainty = a.AssertedConfidence switch
            { >= .9 => "certain", >= .7 => "fairly sure", >= .5 => "probably true", >= .3 => "not sure", _ => "cannot vouch for it" };
            entries.Add(new PlayerChronicleEntry(new(ChronicleKind.Account, a.SourceReportId,
                a.ClaimOrdinal, new(a.OwnerId, a.Sequence)), a.At, name(a.Claim.Subject),
                PlayerNarration.Describe(a.Claim, name, who.Id, self))
            { SourceId = a.SenderId, SourceName = name(a.SenderId), Claim = PlayerClaim.Of(a.Claim),
              Stance = a.Stance, Uncertainty = certainty, ClaimedBasis = a.ClaimedBasis });
        }
        foreach (var r in who.Capabilities.CashReceipts.Where(r => r.Operation is not null))
            entries.Add(new PlayerChronicleEntry(new(ChronicleKind.Income, 0, 0, r.Operation), r.At,
                name(r.SourceId), "Money received")
            { Amount = r.Amount, ExecutorId = r.ExecutorId, ExecutorName = name(r.ExecutorId), MoneyArrived = true });
        foreach (var e in who.Execution.EndedTributeOrders)
            entries.Add(new PlayerChronicleEntry(new(ChronicleKind.Ended, 0, 0, e.Operation), e.At,
                e.TargetId is null ? "tribute order" : name(e.TargetId), "Order ended")
            { ExecutorId = e.ExecutorId, ExecutorName = name(e.ExecutorId), MoneyArrived = e.MoneyArrived,
              Uncertainty = e.MoneyArrived ? null : "outcome unknown" });
        return entries.OrderBy(e => e.At).ThenBy(e => e.Id.Kind).ThenBy(e => e.Id.SourceId)
            .ThenBy(e => e.Id.Ordinal).ThenBy(e => e.Id.Operation?.OwnerId, StringComparer.Ordinal)
            .ThenBy(e => e.Id.Operation?.LocalSequence).ToList();
    }
}

/// <summary>Deterministic expression over frozen meanings; cannot access a world or session.</summary>
public static class SceneNarration
{
    public const string HistoryLimit = "Your orders, received accounts and money received — not a complete encounter transcript. Accounts stay as received; current beliefs are in the knowledge view.";

    public static string Entry(PlayerChronicleEntry e) => e.Id.Kind switch
    {
        ChronicleKind.Order => $"Order: {e.Description}." + (e.ExecutorName is { } executor ? $" Assigned to {executor}." : ""),
        ChronicleKind.Account => $"{e.SourceName} reported: {StanceWords(e.Stance)} that {e.Description}; "
            + $"{e.Uncertainty}. Claimed basis: {BasisWords(e.ClaimedBasis)}. Event time unknown.",
        ChronicleKind.Income => $"Received {e.Amount?.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} from {e.Subject}; {e.ExecutorName} handled the job.",
        ChronicleKind.Ended => $"The order at {e.Subject} ended. " + (e.MoneyArrived == true ? "Money arrived." : "Outcome unknown; the reason is unknown."),
        _ => throw new ArgumentOutOfRangeException(nameof(e)),
    };

    public static IReadOnlyList<string> Situation(PlayerSnapshot snapshot, PendingDecision? pending)
    {
        var lines = new List<string>();
        if (pending?.ActorId == snapshot.ViewpointId)
        {
            if (pending.Occasion is { } occasion) lines.Add(occasion);
            if (pending.Focus is { } focus) lines.Add(focus);
        }
        // One current situation: last visible tribute operation in stable identity order, or latest history.
        var op = snapshot.Operations.LastOrDefault(o => o.TributeOperation is not null);
        if (op is not null)
        {
            lines.Add($"{op.Description} — {op.Approach}. " + (op.ExecutorName is { } executor
                ? $"{executor} is handling it." : $"{snapshot.ViewpointPronouns.Subject_} {snapshot.ViewpointPronouns.Verb("is", "are")} handling it."));
            if (op.Progress is { } progress) lines.Add(progress);
            var latest = snapshot.Chronicle.LastOrDefault(e => e.Id.Operation == op.TributeOperation);
            if (latest is not null) lines.Add(Entry(latest));
            lines.Add("The reason for any unexplained delay is unknown.");
        }
        else if (snapshot.Chronicle.LastOrDefault() is { } latest)
        {
            var history = latest.Id.Operation is null ? new[] { latest }
                : snapshot.Chronicle.Where(e => e.Id.Operation == latest.Id.Operation).ToArray();
            if (history.FirstOrDefault(e => e.Action == ActionKind.StartStrategy) is { } order && order != latest)
                lines.Add(Entry(order));
            if (history.LastOrDefault(e => e.Id.Kind == ChronicleKind.Account) is { } account && account != latest)
                lines.Add(Entry(account));
            if (history.LastOrDefault(e => e.Id.Kind == ChronicleKind.Income) is { } income && income != latest)
                lines.Add(Entry(income));
            lines.Add(Entry(latest));
        }
        else lines.Add("No tribute order to recount yet.");
        return Frozen.List(lines);
    }

    private static string StanceWords(Stance? stance) => stance switch
    { Stance.Knows => "knows", Stance.Believes => "believes", Stance.Suspects => "suspects", Stance.Doubts => "doubts", Stance.Rejects => "rejects the claim", _ => "gave an account" };
    private static string BasisWords(SourceKind? basis) => basis switch
    { SourceKind.Participant => "participation", SourceKind.Witness => "personal observation",
      SourceKind.Discovery => "discovery", SourceKind.Inference => "inference",
      SourceKind.FirstHandTestimony => "first-hand testimony", SourceKind.Rumor => "rumor",
      _ => "report" };
}
