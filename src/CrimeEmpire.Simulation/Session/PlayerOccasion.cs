namespace CrimeSim.Session;

using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;

/// <summary>
/// Why the controlled character is thinking about this now — said in a closed vocabulary this file
/// owns, and never in the words the scheduler used.
///
/// <b>What was wrong before, because it is the whole reason this file exists.</b> Milestone 009
/// shipped with <c>PendingDecision.Occasion</c> set to <see cref="ScheduledEvent.Cause"/> directly,
/// on a ruling that argued every deliberation-waking cause is authored from the waking character's
/// own side. That claim was false for two event kinds, and falsely stated about all of them — which
/// is this project's signature defect, made this time by the person writing the ruling that named it.
///
/// <see cref="Strategies.Blocked"/> schedules <see cref="EventKind.StrategyBlocked"/> addressed to
/// the strategy's <em>owner</em> with the cause <c>"Bellini's grocery held out against force"</c>,
/// and <see cref="Strategies.Complete"/> schedules <see cref="EventKind.StrategyComplete"/> with
/// <c>"… finished: the cleanup made things worse"</c>. When the work was delegated, the owner was not
/// there, nobody has told him, and no discovery roll has been made. Those sentences are the
/// executor's operational outcome, and handing them to the owner is precisely the "authority
/// delivers knowledge" leak that <c>Strategies.ResolveViolence</c> has a long comment refusing to
/// commit.
///
/// <b>The rule now.</b> Nothing authored by a scheduler crosses the boundary at all. The occasion is
/// chosen from a fixed list keyed on the event kind, and <b>the default is silence</b> — a kind that
/// is not named below produces no occasion, so a future event kind is mute until somebody decides
/// what a character could honestly be said to know about it. Fail-closed rather than fail-open is
/// the difference between this and the ruling it replaces.
///
/// <b>The two strategy-outcome kinds, revisited by milestone 026's second correction.</b> Milestone
/// 009 made both silent: a test for "did he execute it himself" is available for
/// <see cref="EventKind.StrategyBlocked"/>, where the instance is still live, and not for
/// <see cref="EventKind.StrategyComplete"/>, where <see cref="Strategies.Complete"/> has already
/// cleared it. That reasoning was about the <em>outcome</em>, and it still holds: no outcome crosses.
/// The <em>fact</em> is different. That his job has ended is his own state either way, so a
/// completion now says so and nothing more; a block says the shop turned him down only when the
/// instance shows he was the man in the room, and stays silent for a delegate's.
/// </summary>
internal static class PlayerOccasion
{
    /// <summary>
    /// What woke him, in terms that assert nothing beyond what the event itself establishes for the
    /// character it is addressed to.
    ///
    /// Null means the interface says nothing.
    ///
    /// <b>Keyed on the event's structured note as well as its kind, since milestone 009's fourth
    /// correction.</b> <see cref="EventKind.RoleReview"/> has five schedulers — a periodic look at his
    /// own patch, and four occasions on which somebody has just spoken to him — and one phrase for
    /// all of them told a man he was doing his rounds when his soldier had reported in. That is not a
    /// leak; it is the opposite, and worse for it. It withheld something he certainly knew and put a
    /// specific false reason in its place, and the withheld part is the most decision-relevant
    /// context there is: "somebody has just put a question to you" is precisely why you would answer
    /// it.
    ///
    /// The phrases below name nobody, with one deliberate exception since milestone 018: a tribute
    /// demand names the demander, because "who is asking me for money" is precisely the fact the
    /// decision it wakes is about — unlike the RoleReview cases above, no option elsewhere on the
    /// same panel already names him.
    /// </summary>
    /// <param name="voice">The pronoun set to speak in; the actor's own when null. The session
    /// passes <see cref="PlayerView.You"/> for the controlled character (milestone 025).</param>
    internal static string? For(
        ScheduledEvent trigger, Character actor, Func<string, string> name, Pronouns? voice = null)
    {
        var self = voice ?? actor.Pronouns;

        return trigger.Kind switch
        {
            // He has just been briefed, through Cognition.Receive, by the man who issued it.
            EventKind.AssignmentDelivered =>
                $"{self.Subject} {self.Verb("has", "have")} just been given a job",

            // Somebody spoke to him, and the note says which act it was. Each is established for him
            // by the act itself: he was the one asked, reported to, or petitioned.
            EventKind.RoleReview => trigger.Payload.Note switch
            {
                "asked-to-account" => $"somebody has asked {self.Object} a question",
                "reported-to" => $"somebody has reported to {self.Object}",
                "permission-sought" => $"somebody has asked {self.Object} for permission",
                _ => $"{self.Subject} {self.Verb("is", "are")} checking on {self.Possessive} own patch",
            },

            // Runner.Observe schedules this only after the observer actually acquired something, so
            // the event's own precondition establishes the phrase — except the tribute-demand note,
            // which is established the same way: the owner learned BusinessRefusesTribute /
            // met the demander in the very same commit that scheduled this (Strategies.cs), so naming
            // the demander here asserts nothing he does not already hold.
            EventKind.Incident => trigger.Payload.Note switch
            {
                "tribute-demanded" when trigger.Payload.TargetId is { } demanderId =>
                    Demand(actor, demanderId, trigger.Payload.AboutClaim?.Subject, name, self),
                _ => $"something has come to {self.Possessive} attention",
            },

            // His own pressure, crossed in his own head.
            EventKind.PressureThreshold => "something has become hard to ignore",

            // Milestone 026's second correction. Milestone 009 made these two silent because the
            // event's authored cause carries the outcome of work that may have been delegated, and
            // that was right about the outcome and wrong about the fact. That his job has ended is
            // his own state — Strategies.Complete cleared his Execution.Strategy, and the screen
            // beside this already says "you have nothing running" — so the fact is said and the
            // outcome is not: "one way or another" is the whole of what he can be told.
            // Money arriving is not the delegate's private operational outcome: the owner receives
            // it, and the collection branch has already written his own Discovery reading before
            // scheduling this event. He also knows whom he assigned. Name those two owner-known
            // facts while keeping method, progress and everything else about delegated execution
            // private. Other completions retain the deliberately outcome-agnostic wording below.
            EventKind.StrategyComplete when
                trigger.Payload.Strategy == StrategyKind.SecureTribute
                && trigger.Payload.Note == "the money started arriving"
                && trigger.Payload.TargetId is { } paidBusiness
                && trigger.Payload.ExecutorId is { } executorId =>
                    executorId == actor.Id
                        ? $"money from {name(paidBusiness)} has started arriving after {self.Subject} handled the job"
                        : $"money from {name(paidBusiness)} has started arriving after {name(executorId)} handled the job",

            EventKind.StrategyComplete =>
                $"the job {self.Subject} had running has come to an end, one way or another",

            // Blocked is different: the instance is still live, so whether he was the man in the
            // room is knowable here. His own work turned down is his own experience; a delegate's
            // is that man's, and stays silent exactly as milestone 024 keeps his progress silent.
            EventKind.StrategyBlocked when actor.Execution.Strategy is { DelegatedToId: null } own =>
                own.TargetId is { } target
                    ? $"{name(target)} has turned {self.Object} down"
                    : $"what {self.Subject} had running has stalled",

            // A delegate's block, and anything added later. Silence is the default and must stay
            // the default: adding a kind here is a claim that the character necessarily knows why
            // he is thinking, and that claim has already been wrong once.
            _ => null,
        };
    }

    /// <summary>
    /// A tribute demand, named to the extent the demanded actually experienced it.
    ///
    /// Force leaves the owner a held <c>PersonUsedViolence(demander -&gt; business)</c> claim
    /// (<c>Strategies.ResolveViolence</c>, <c>SourceKind.Witness</c>) and is named as such —
    /// <b>only when both the demander and the business match this demand</b>, per milestone 018's
    /// correction: the same man's violence at a different shop is a different fact and must not read
    /// as "over this". <paramref name="businessId"/> is <see cref="EventPayload.AboutClaim"/>'s own
    /// subject, carried by the same claim shape <c>Strategies.cs</c> already learns the owner's
    /// resistance belief from, rather than a new payload field. Threaten leaves nothing equivalent —
    /// only <c>Relations.Frighten</c>'s fear rise, which has no reader here — so a
    /// threatened-but-not-yet-forced demand reads as a plain demand rather than asserting a method the
    /// character has no structural record of experiencing.
    /// </summary>
    private static string Demand(
        Character actor, string demanderId, string? businessId, Func<string, string> name, Pronouns self)
        => businessId is not null
           && actor.Cognition.OfKind(ClaimKind.PersonUsedViolence)
               .Any(r => r.Claim.Subject == demanderId && r.Claim.Object == businessId)
            ? $"{name(demanderId)} has already used force over this"
            : $"{name(demanderId)} is demanding tribute from {self.Object}";

    /// <summary>
    /// What is on his mind — derived from his own state, never passed through from the agenda's
    /// developer-facing description.
    ///
    /// <see cref="AgendaKind.RespondToTrigger"/> is excluded because
    /// <see cref="AgendaSelection.Select"/> sets its <c>Description</c> to <c>trigger.Cause</c>
    /// verbatim, which is the authored string this file exists to keep out.
    ///
    /// <b>Two of the four remaining kinds were passing developer text too, until milestone 009's
    /// fourth correction.</b> <see cref="AgendaKind.ContinueCommitment"/>'s description is
    /// <c>"ongoing: " + StrategyInstance.Label</c>, which put
    /// <c>ConcealIncident(, target=bellini-grocery, method=Persuade)</c> in front of a player — raw
    /// ids, a raw enum, and the empty-domain defect already on the carried-forward list.
    /// <see cref="AgendaKind.RelievePressure"/>'s was <c>"pressure: " + PressureKind</c>. Both are now
    /// phrased here from the typed values, and the strategy phrasing is
    /// <see cref="PlayerOption.Work"/> — the same one the options use, rather than a second wording
    /// of the same thing.
    ///
    /// The two that do pass their description through are prose a person wrote about him and that he
    /// holds: the objective he was briefed on, and his own standing responsibility.
    /// </summary>
    /// <param name="assignment">Resolves an assignment id to the record of what was said when it
    /// was issued — who gave it, what he disclosed, the rule he attached, the deadline. Null keeps
    /// the objective alone. Milestone 025's first correction, on Matt's playtest finding that a
    /// decision arrived with no context but "restore the harbour tribute".</param>
    internal static string? Focus(
        Character actor, Agenda agenda, ScheduledEvent trigger, Func<string, string> name, Pronouns? voice = null,
        Func<long, Assignment?>? assignment = null, Func<string, Pronouns>? pronouns = null)
    {
        var self = voice ?? actor.Pronouns;

        // A wake we cannot describe is a wake we say nothing about. Otherwise the focus would narrate
        // the same delegated outcome the occasion was suppressed for.
        if (For(trigger, actor, name, self) is null) return null;

        return agenda.Kind switch
        {
            // The objective he was handed, in the issuer's words, which he was told — and, when the
            // record of the briefing is to hand, the rest of what he was told with it.
            AgendaKind.FulfilAssignment =>
                agenda.AssignmentId is { } id && assignment?.Invoke(id) is { } given
                    ? Briefing(given, agenda.Description, actor, name, self, pronouns)
                    : agenda.Description,

            // His own standing duty, in the words the scenario gave it.
            AgendaKind.DischargeResponsibility => agenda.Description,

            // The course of action he started, described as his options describe it.
            AgendaKind.ContinueCommitment when actor.Execution.Strategy is { } s =>
                PlayerOption.Work(s.Kind, s.TargetId, name, self),

            // What is pressing on him, by what it is rather than by its enum name.
            AgendaKind.RelievePressure =>
                actor.Motivations.Dominant() is { } p ? Pressure(p.Kind, self) : null,

            _ => null,
        };
    }

    /// <summary>
    /// The job as it was given, from the record taken at issuance: the objective, who wants it and by
    /// when, what he was told, and the rule he was told to keep.
    ///
    /// <b>Everything here was said to him.</b> <see cref="Assignment.Disclosed"/> is the snapshot of
    /// what the issuer asserted at the time, delivered through <c>Cognition.Receive</c> like any
    /// account; <see cref="Assignment.Constraints"/> is the rule's own description; the deadline is
    /// the one he was given. Nothing reads the issuer's current mind — "he is waiting to see how you
    /// handle it" would be the boss's state, which the capo does not have — and the rule is stated
    /// from the constraint rather than repeated from the disclosed awareness claim, so it appears
    /// once.
    /// </summary>
    private static string Briefing(
        Assignment given, string objective, Character actor, Func<string, string> name, Pronouns self,
        Func<string, Pronouns>? pronouns)
    {
        var issuer = pronouns?.Invoke(given.IssuerId) ?? Pronouns.He;
        string issuerName = name(given.IssuerId);

        var told = given.Disclosed
            .Where(d => d.Claim.Kind != ClaimKind.PolicyIssued)
            .Select(d => d.AssertedStance is Stance.Suspects or Stance.Doubts
                ? $"{issuer.Subject} {issuer.Verb("suspects", "suspect")} {PlayerNarration.Describe(d.Claim, name, actor.Id, self)}"
                : PlayerNarration.Describe(d.Claim, name, actor.Id, self))
            .ToList();

        var parts = new List<string>
        {
            $"{objective}, for {issuerName}, by {given.Deadline.ToString("d MMMM", System.Globalization.CultureInfo.InvariantCulture)}.",
        };
        if (told.Count > 0)
            parts.Add($"{issuerName} told {self.Object}: {string.Join("; ", told)}.");
        if (given.Constraints.Count > 0)
            parts.Add($"{Capital(issuer.Possessive)} standing rule: {string.Join("; ", given.Constraints)}.");

        return string.Join(" ", parts);
    }

    private static string Capital(string s) => char.ToUpperInvariant(s[0]) + s[1..];

    /// <summary>
    /// A pressure in the character's own terms. Closed, and silent on anything unnamed — the same
    /// fail-closed default as the occasion vocabulary.
    /// </summary>
    private static string? Pressure(PressureKind kind, Pronouns self) => kind switch
    {
        PressureKind.RevenueShortfall => "the money that is not coming in",
        PressureKind.LegalExposure => $"how exposed {self.Subject} {self.Verb("is", "are")}",
        PressureKind.Resentment => $"a grudge {self.Subject} {self.Verb("is", "are")} nursing",
        PressureKind.Fear => $"what {self.Subject} {self.Verb("is", "are")} afraid of",
        PressureKind.OrganizationalInstability => "how unsteady the outfit has become",
        _ => null,
    };
}
