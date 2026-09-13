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
    /// <summary>
    /// <paramref name="selfId"/> is the actor's own id, so a claim in the wording that is about him
    /// reads "you" in the second person — see
    /// <see cref="PlayerNarration.Describe(Claim, Func{string, string}, string?, Pronouns?)"/>.
    /// Null keeps every name.
    /// </summary>
    internal static string Describe(
        Candidate c, Func<string, string> name, Pronouns self, Func<string, Pronouns> pronouns,
        string? selfId = null)
    {
        string text = Body(c, name, self, pronouns, selfId);

        // A rule he knows about, being stepped over. Admissible because BreachesPolicyId is only ever
        // populated from policies the character actually holds — Generators.Coercive reads
        // ctx.KnownPolicies, and an unknown rule deters nobody. name() resolves the policy id to the
        // rule's own description (milestone 025); the id itself is developer text.
        return c.BreachesPolicyId is { } policy
            ? $"{text} — breaking the rule: {name(policy)}"
            : text;
    }

    private static string Body(
        Candidate c, Func<string, string> name, Pronouns self, Func<string, Pronouns> pronouns,
        string? selfId) => c.Kind switch
    {
        ActionKind.ContinueStrategy when c.IsOperationReview => "leave these orders unchanged",
        ActionKind.ContinueStrategy => $"carry on {Work(c, name, self)}",
        ActionKind.AlterStrategy when c.Method is { } m && c.TargetId is { } t =>
            $"switch to {Verb(m)} with {name(t)}",
        ActionKind.AlterStrategy => "change approach",
        ActionKind.DelegateStrategy when c.TargetId is { } sub => $"hand it to {name(sub)}",
        ActionKind.DelegateStrategy => "hand it to somebody",
        ActionKind.PostponeStrategy => "leave it for now",
        ActionKind.AbandonStrategy => $"drop {Work(c, name, self)}",
        ActionKind.StartStrategy => Start(c, name),
        ActionKind.ReportToSuperior => Speak(c, name, self, pronouns, selfId),
        ActionKind.SeekApproval when c.TargetId is { } boss => $"ask {name(boss)} for permission",
        ActionKind.SeekApproval => "ask for permission",
        ActionKind.SeekCorroboration when c.TargetId is { } other =>
            c.AboutClaim is { } about
                ? $"ask {name(other)} what {pronouns(other).Subject} " +
                  $"{pronouns(other).Verb("knows", "know")} about whether " +
                  $"{PlayerNarration.Describe(about, name, selfId, self)}"
                : $"ask {name(other)} what {pronouns(other).Subject} {pronouns(other).Verb("knows", "know")}",
        ActionKind.SeekCorroboration => "ask somebody what they know",
        ActionKind.RequestHelp when c.TargetId is { } helper => $"ask {name(helper)} for help",
        ActionKind.RequestHelp => "ask for help",
        ActionKind.Retaliate when c.TargetId is { } enemy => $"move against {name(enemy)}",
        ActionKind.Retaliate => "move against the man",
        ActionKind.Concede when c.TargetId is { } asker => $"pay what {name(asker)} is asking",
        ActionKind.Concede => "pay what is being asked",
        ActionKind.Refuse when c.TargetId is { } asker => $"refuse {name(asker)}",
        ActionKind.Refuse => "refuse",
        ActionKind.DoNothing when c.OperationSequence is not null => "leave these orders unchanged",
        // Not "do nothing": the floor candidate's id is the word "nothing", and a test guards that
        // no candidate id ever reaches the player's text — a guard worth keeping even when the id
        // happens to be a plain word.
        _ => "take no action",
    };

    private static string Work(Candidate c, Func<string, string> name, Pronouns self)
        => Work(c.Strategy, c.TargetId, name, self);

    /// <summary>
    /// The course of action itself, without repeating how it is being pursued.
    ///
    /// Internal and taking the fields rather than a candidate, so <see cref="PlayerOccasion"/> can
    /// describe a running strategy the same way an option describes one. The alternative was a second
    /// phrasing of the same thing, which is how <c>StrategyInstance.Label</c> — a developer string
    /// carrying raw ids and an empty domain — reached the player as the decision's focus.
    /// </summary>
    internal static string Work(
        StrategyKind? strategy, string? targetId, Func<string, string> name, Pronouns self)
        => strategy switch
        {
            StrategyKind.SecureTribute when targetId is { } t => $"getting {name(t)} to pay",
            StrategyKind.SecureTribute => "the collection",
            StrategyKind.ConcealIncident => "covering it up",
            StrategyKind.InvestigateIncident when targetId is { } t => $"looking into {name(t)}",
            StrategyKind.InvestigateIncident => "the investigation",
            _ => $"what {self.Subject} started",
        };

    private static string Start(Candidate c, Func<string, string> name) => c.Strategy switch
    {
        StrategyKind.SecureTribute when c.TargetId is { } t && c.Method is { } m => m switch
        {
            CoercionMethod.Persuade => $"persuade {name(t)} to pay",
            CoercionMethod.Threaten => $"threaten {name(t)}",
            _ => $"use force on {name(t)}",
        },
        StrategyKind.ConcealIncident => "cover it up before anyone finds out",
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
    /// <summary>
    /// What he would say, and how straight he would say it — milestone 025's first correction
    /// reworded the three answers after Matt could not tell them apart in play.
    ///
    /// The partial answer to a question withholds the one claim the question is about, so it is
    /// silence on the subject, and says so; "leaving out his own part" described the mechanism and
    /// not the effect. The false answer is a denial and reads as one. And when the question is about
    /// his own act — the only case the generator offers deception for — the honest answer is an
    /// admission, and reads as one.
    /// </summary>
    private static string Speak(
        Candidate c, Func<string, string> name, Pronouns self, Func<string, Pronouns> pronouns, string? selfId)
    {
        string who = c.TargetId is { } t ? name(t) : $"{self.Possessive} superior";
        var whom = c.TargetId is { } id ? pronouns(id) : Pronouns.He;

        if (c.AnsweringClaim is { } question)
        {
            string subject = PlayerNarration.Describe(question, name, selfId, self);
            bool ownAct = selfId is not null && question.Subject == selfId
                          && question.Kind is ClaimKind.PersonUsedViolence or ClaimKind.PersonBreachedPolicy;
            return c.Candor switch
            {
                ReportCandor.Uninformed =>
                    $"tell {who} {self.Subject} {self.Verb("knows", "know")} nothing about it",
                ReportCandor.Partial => $"say nothing to {who} about it either way",
                ReportCandor.False =>
                    $"deny it to {who}: tell {whom.Object} {PlayerNarration.Deny(question, name, selfId, self)}",
                _ => ownAct
                    ? $"admit it to {who}: {subject}"
                    : $"tell {who} what {self.Subject} {self.Verb("knows", "know")} about whether {subject}",
            };
        }

        return c.Candor switch
        {
            ReportCandor.Partial => $"report to {who}, leaving out {self.Possessive} own part",
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
