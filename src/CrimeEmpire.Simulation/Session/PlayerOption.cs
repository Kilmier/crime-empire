namespace CrimeSim.Session;

using CrimeSim.Decision;
using CrimeSim.Domain;

/// <summary>
/// One available candidate, put into words from its structured fields.
///
/// <b>Why not <see cref="Candidate.Description"/>.</b> That string is written by the generators for
/// the developer trace, and it interpolates <see cref="Claim"/> directly — so
/// <c>"give salvatore his account of PersonUsedViolence(tommy -&gt; bellini-grocery#7)"</c> carried a
/// truth-log correlation number into the player's hands, and named people by id. Milestone 009
/// deferred that as cosmetic; its review disagreed, and the review is right: a `#7` is
/// <see cref="Sim.WorldEvent.Id"/> and has no player-facing meaning at all.
///
/// So the option text is derived here from the candidate's typed fields — kind, strategy, method,
/// target, candour, and the claim a question or an answer is about — and any claim in it goes
/// through <see cref="PlayerNarration.Describe"/>, which is the audited path and prints no counter.
/// Nothing a generator wrote reaches the player, which is the same guarantee
/// <see cref="PlayerOccasion"/> makes about the scheduler.
///
/// <b>Every input is already belief-limited.</b> Candidates are generated from the actor's
/// <see cref="PerceivedSituation"/>, so a field read here cannot name a fact he lacks, and a
/// corroboration target comes from <see cref="Acquaintance.KnownTo"/> — his own cognition and
/// social state, widened only by the holders of his organisation's named posts. So resolving an id
/// to a display name here cannot put a stranger's name in front of anybody.
///
/// That was not true when this file was written: <c>Generators.FromRelationship</c> then drew its
/// target from the whole organisation roster, and this comment recorded it as a soft edge deferred
/// to `ROADMAP.md`. Milestone 009's review called it a P1 — correctly, since rendering the name is
/// the half that reaches a person — and its third correction closed it upstream.
/// </summary>
internal static class PlayerOption
{
    internal static string Describe(Candidate c, Func<string, string> name)
    {
        string text = Body(c, name);

        // A rule he knows about, being stepped over. Admissible because BreachesPolicyId is only ever
        // populated from policies the character actually holds — Generators.Coercive reads
        // ctx.KnownPolicies, and an unknown rule deters nobody.
        return c.BreachesPolicyId is { } policy
            ? $"{text} — against the standing rule \"{policy}\""
            : text;
    }

    private static string Body(Candidate c, Func<string, string> name) => c.Kind switch
    {
        ActionKind.ContinueStrategy => $"carry on {Work(c, name)}",
        ActionKind.AlterStrategy when c.Method is { } m && c.TargetId is { } t =>
            $"change tack with {name(t)} — {Verb(m)} instead",
        ActionKind.AlterStrategy => "change tack",
        ActionKind.DelegateStrategy when c.TargetId is { } sub => $"have {name(sub)} take it on",
        ActionKind.DelegateStrategy => "hand it to somebody",
        ActionKind.PostponeStrategy => "leave it for now",
        ActionKind.AbandonStrategy => $"drop {Work(c, name)}",
        ActionKind.StartStrategy => Start(c, name),
        ActionKind.ReportToSuperior => Speak(c, name),
        ActionKind.SeekApproval when c.TargetId is { } boss => $"ask {name(boss)} for room to move",
        ActionKind.SeekApproval => "ask for room to move",
        ActionKind.SeekCorroboration when c.TargetId is { } other =>
            c.AboutClaim is { } about
                ? $"ask {name(other)} for his own account of whether {PlayerNarration.Describe(about, name)}"
                : $"ask {name(other)} for his own account",
        ActionKind.SeekCorroboration => "get somebody else's account",
        ActionKind.RequestHelp when c.TargetId is { } helper => $"ask {name(helper)} for help",
        ActionKind.RequestHelp => "ask for help",
        ActionKind.Retaliate when c.TargetId is { } enemy => $"move against {name(enemy)}",
        ActionKind.Retaliate => "move against him",
        ActionKind.Concede when c.TargetId is { } asker => $"pay what {name(asker)} is asking",
        ActionKind.Concede => "pay what is being asked",
        ActionKind.Refuse when c.TargetId is { } asker => $"refuse {name(asker)}",
        ActionKind.Refuse => "refuse",
        _ => "let it lie",
    };

    /// <summary>The course of action itself, without repeating how it is being pursued.</summary>
    private static string Work(Candidate c, Func<string, string> name) => c.Strategy switch
    {
        StrategyKind.SecureTribute when c.TargetId is { } t => $"getting {name(t)} to pay",
        StrategyKind.SecureTribute => "the collection",
        StrategyKind.ConcealIncident => "covering it up",
        StrategyKind.InvestigateIncident when c.TargetId is { } t => $"looking into {name(t)}",
        StrategyKind.InvestigateIncident => "the investigation",
        _ => "what he started",
    };

    private static string Start(Candidate c, Func<string, string> name) => c.Strategy switch
    {
        StrategyKind.SecureTribute when c.TargetId is { } t && c.Method is { } m => m switch
        {
            CoercionMethod.Persuade => $"talk {name(t)} round",
            CoercionMethod.Threaten => $"lean on {name(t)}",
            _ => $"strong-arm {name(t)}",
        },
        StrategyKind.ConcealIncident => "clean up after it before anyone else does",
        StrategyKind.InvestigateIncident when c.TargetId is { } t => $"open an investigation at {name(t)}",
        StrategyKind.InvestigateIncident => "open an investigation",
        _ => "set something in motion",
    };

    /// <summary>
    /// What he would say, and how straight he would say it.
    ///
    /// The candour is his own choice and belongs in front of him — it is the option, not a hidden
    /// property of it. What the claim is about goes through the narrator, so a claim's counter never
    /// appears; a report with no particular subject is a general account and says so.
    /// </summary>
    private static string Speak(Candidate c, Func<string, string> name)
    {
        string who = c.TargetId is { } t ? name(t) : "his superior";

        if (c.AnsweringClaim is { } question)
        {
            string subject = PlayerNarration.Describe(question, name);
            return c.Candor switch
            {
                ReportCandor.Partial => $"tell {who} about whether {subject}, leaving out his own part",
                ReportCandor.False => $"tell {who} that it is not so, about whether {subject}",
                _ => $"give {who} his account of whether {subject}",
            };
        }

        return c.Candor switch
        {
            ReportCandor.Partial => $"report to {who}, leaving out his own part",
            ReportCandor.False => $"tell {who} it did not happen",
            _ => $"report the situation to {who}",
        };
    }

    private static string Verb(CoercionMethod m) => m switch
    {
        CoercionMethod.Persuade => "talking",
        CoercionMethod.Threaten => "threats",
        _ => "force",
    };
}
