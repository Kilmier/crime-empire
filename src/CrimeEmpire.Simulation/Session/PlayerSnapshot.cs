namespace CrimeSim.Session;

using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Sim;

/// <summary>Somebody the viewpoint character has heard of. Id and display name, nothing else.</summary>
public sealed record PlayerPerson(string Id, string Name);

/// <summary>
/// One thing the viewpoint character holds, as he could relate it.
///
/// <see cref="Certainty"/> and <see cref="Attribution"/> are already-resolved qualitative phrases
/// rather than raw values, so no surface downstream can re-derive them differently — or print the
/// number. Either may be null, and a null is a decision rather than a gap: the certainty is omitted
/// for what he saw or did himself, and the attribution for an act of his own that the sentence
/// already names him as the author of. See <see cref="PlayerNarration.Certainty"/> and
/// <see cref="PlayerNarration.Attribute(InformationRecord, Func{string, string}, Pronouns, Func{string, bool}, string?)"/>.
///
/// <see cref="ReconsideredAt"/> is when he last had cause to think about it — a contradiction or a
/// corroboration moves it, acquisition sets it — and is what the interface orders by, so a
/// three-week-old belief somebody disputed yesterday reads as news, which is what it is. Milestone
/// 025 removed the separate <c>Recent</c> list that used to carry the same beliefs re-sorted by this
/// field: it was a literal subset of <see cref="PlayerSnapshot.Known"/>, and the screen drew every
/// entry in it twice.
/// </summary>
public sealed record PlayerBelief(
    PlayerClaim Claim,
    string Statement,
    DateTime AcquiredAt,
    DateTime ReconsideredAt,
    string? Certainty,
    string? Attribution,
    bool Contested,
    bool IsHeld);

/// <summary>One account he was given, attributed to the man who gave it.</summary>
public sealed record PlayerAccount(string SourceId, string SourceName, bool Affirms, DateTime At);

/// <summary>
/// A matter his sources do not agree about, with every account side by side and his own — if he has
/// one of his own — listed among them rather than above them as the answer.
/// </summary>
public sealed record PlayerDisagreement(
    PlayerClaim Claim,
    string Statement,
    string? OwnBasis,
    bool OwnPositionHeld,
    IReadOnlyList<PlayerAccount> Accounts)
{
    /// <summary>Frozen at construction — see <see cref="Frozen"/>.</summary>
    public IReadOnlyList<PlayerAccount> Accounts { get; init; } = Frozen.List(Accounts);
}

/// <summary>
/// How he takes one person. His own attitude outward, never anything about what they make of him,
/// which is their private state and not his to know.
/// </summary>
public sealed record PlayerAttitude(
    string PersonId,
    string PersonName,
    Pronouns PersonPronouns,
    string Standing,
    string? Wariness,
    string? TakenFor,
    IReadOnlyList<string> Grievances,
    IReadOnlyList<PlayerStandingMoment> History,
    IReadOnlyList<PlayerImpression> Impressions)
{
    /// <summary>Frozen at construction — see <see cref="Frozen"/>.</summary>
    public IReadOnlyList<string> Grievances { get; init; } = Frozen.List(Grievances);

    /// <summary>
    /// What this man has seemed to make of what he was told or shown, oldest first — milestone 026.
    /// The viewpoint character's own readings of a face, which can be wrong, and never the man's
    /// actual state.
    /// </summary>
    public IReadOnlyList<PlayerImpression> Impressions { get; init; } = Frozen.List(Impressions);

    /// <summary>
    /// Why his standing toward this man moved, oldest first — milestone 023.
    ///
    /// The half the interface never had. A standing phrase says where he stands and deliberately not
    /// how he got there, so a relationship that cooled because he was contradicted to his face read
    /// exactly like one that had never been warm.
    /// </summary>
    public IReadOnlyList<PlayerStandingMoment> History { get; init; } = Frozen.List(History);
}

/// <summary>
/// One remembered reason a standing moved, as the player is entitled to see it: what happened, when,
/// and which way it went. Never how far — that is the same hidden magnitude
/// <see cref="PlayerNarration.Standing"/> refuses.
/// </summary>
public sealed record PlayerStandingMoment(string Description, bool Warmed, DateTime At);

/// <summary>One reading of a man's face, in words, dated — milestone 026. See <see cref="PlayerAttitude.Impressions"/>.</summary>
public sealed record PlayerImpression(string PersonId, string PersonName, string Description, DateTime At);

/// <summary>
/// Somebody who, asked, said he knew nothing of the matter — milestone 026. An answer, and shown as
/// one: it resolves the request and it is not a position, so it appears beside who has said nothing
/// rather than among the beliefs or the disagreements.
/// </summary>
public sealed record PlayerDisclaimer(string PersonId, string PersonName, string Description, DateTime At);

/// <summary>
/// The viewpoint character's own most recently committed action, as he could relate it — the same
/// wording <see cref="PlayerOption"/> gave the option when it was offered to whoever chose it, never
/// a fresh phrasing of the same thing.
///
/// Actor-neutral by construction, not by a second write path: <see cref="Decision.Pipeline.Resolve"/>
/// appends a <see cref="Decision.DecisionRecord"/> to <see cref="World.Decisions"/> for every commit,
/// a player's choice and an autonomous one alike, so this projects whichever one this character's own
/// last entry happens to be. Everything else on that record — trigger, agenda, beliefs used,
/// generated and rejected candidates, scores, the raw outcome string, reconsideration triggers,
/// salience notes — is discarded by the projection and never reaches this type.
/// </summary>
public sealed record PlayerCommittedAction(DateTime At, string Description);

/// <summary>
/// The order he currently has out — milestone 024. Null when he has nothing running.
///
/// <b><see cref="Progress"/> is null for delegated work, and that is the milestone rather than a
/// gap.</b> What he ordered, who he gave it to and when are his own acts and his to know. How far
/// along somebody else has got is not: milestones 017 and 022 both settled that the man who ordered a
/// job learns whether it was carried out "through a report or a discovery roll like anyone else", and
/// `StrategyInstance.StepIndex` on a delegated instance is the executor's state. Putting it on this
/// screen would be the omniscience the information model exists to prevent, arriving through a panel
/// instead of through a belief.
///
/// So a delegated operation reads as what he ordered, of whom, and silence — and the silence is
/// honest. What breaks it is a report, a rumour, or the takings arriving, all of which reach him
/// through channels that already exist and already surface in his beliefs.
///
/// <b><see cref="Since"/> is null for the executor, for the identical reason — milestone 024's second
/// correction.</b> `StrategyInstance.StartedAt` records when the *owner* started the operation, not
/// when it was handed over: nothing anywhere records a handover time, and rendering the owner's own
/// start date to the man it was delegated to would tell him "you have had this since 2 Mar" when he
/// may have been handed it on the 14th — the owner's own fact, not his. The owner still sees it,
/// because it is when he began something he is still watching, exactly as he still sees what he
/// ordered and who is carrying it.
/// </summary>
public sealed record PlayerOperation(
    string Description,
    string Approach,
    string? ExecutorName,
    DateTime? Since,
    string? Progress,
    string? ReviewToken = null);

/// <summary>One itemized receipt from this character's own cash history.</summary>
public sealed record PlayerIncome(
    DateTime At,
    double Amount,
    string SourceName,
    string ExecutorName,
    bool HandledPersonally);

/// <summary>
/// The viewpoint character's own business, when he owns one. <see cref="PayingTribute"/> only — on
/// the same footing as <see cref="PlayerSnapshot.Cash"/> (milestone 014 ruling 1): an owner always
/// knows whether his own shop is currently paying, without needing a belief record to stand in for
/// it. <see cref="Sim.Business.Resistance"/> is deliberately excluded: its own doc comment states it
/// is "objective; characters only estimate it", so it must never cross as a fact he simply has.
/// </summary>
public sealed record PlayerBusinessStatus(string Id, string Name, bool PayingTribute);

/// <summary>
/// A question the viewpoint character himself put to somebody, still without a communicated
/// response. Every instance that reaches this list is, definitionally, one nothing has answered yet —
/// see <see cref="PlayerView.Build"/>'s own comment on <c>AwaitingAnswers</c> for exactly what
/// "answered" means and where it is decided.
///
/// <b>Corrected three times by milestone 018's review.</b> The first correction resolved a request
/// from testimony alone, which could not distinguish "not yet" from "he decided against it" — both
/// read as permanently pending. The fix it shipped read <see cref="World.Decisions"/> for the
/// <em>asked</em> character to tell whether his own triggered deliberation had resolved, and Codex's
/// second review correctly rejected that: the asker never received any message establishing that
/// Tommy had decided anything at all, so showing "he chose not to say" exposed a private mental event
/// nothing in the fiction communicated to him — the canonical information boundary this project holds
/// everywhere else. A same-review attempt to split a third "Declined" value out of a communicated
/// account that denies the claim was also wrong: the natural proof scenario has Vincent give
/// Salvatore a full, sincere account that happens to contradict what Salvatore believed, and that is
/// an answer, not a refusal. Once the corrected derivation read only the asker's own
/// <see cref="Cognition.Testimony"/>, the disposition type it had produced two values for had no
/// remaining reason to exist — every instance actually reaching a caller was the same value, since an
/// answered request drops out before construction — so the third correction removed both the type and
/// this record's field for it, rather than carry a type whose only inhabited state is implicit in
/// list membership.
///
/// Added by milestone 018, and the one place <see cref="PlayerView.Build"/> reads
/// <see cref="World.Requests"/> — filtered to requests this character himself asked, never anyone
/// else's. Nothing here reads <see cref="World.Reports"/>, <c>Report.AnsweringClaim</c>, or
/// <see cref="World.Decisions"/> for anybody but the viewpoint's own <see cref="LastAction"/> (a
/// different field entirely) — whether a request belongs here is read entirely from this character's
/// own <see cref="Cognition.Testimony"/>, the ordinary report channel's own effect on his cognition,
/// which is identical whether the asked character answered under player control or autonomously. The
/// answer itself, whichever way it points, is not rendered here: once delivered, it already appears
/// in <see cref="PlayerSnapshot.Known"/> or <see cref="PlayerSnapshot.Disagreements"/> through the
/// existing derivation, attributed the existing way.
/// </summary>
public sealed record PlayerRequest(
    string AskedId,
    string AskedName,
    Pronouns AskedPronouns,
    PlayerClaim About,
    string Statement,
    DateTime AskedAt);

/// <summary>
/// Everything one character could tell you, at one moment, as immutable data.
///
/// THE RULE THIS TYPE EXISTS TO ENFORCE, amended by milestone 014 and again by milestone 018: every
/// field below is either derived from the viewpoint character's own <see cref="Cognition"/> and
/// <see cref="SocialState"/>, or copied out of some other private state that is legitimately his own
/// to know without a belief record standing in for it — <see cref="Cash"/>, from his own
/// <see cref="Domain.Capabilities"/>, was the first example; <see cref="MyBusiness"/> is the second.
/// Nothing here may be derived from <em>another</em> character's private state — his cash, his
/// scores, his cognition — and nothing here carries a reference back to the object it was copied
/// from: every field is a value, copied once, not a window onto something that can still change.
/// <see cref="World"/> is consulted only to turn ids into display names, which are public knowledge,
/// and to ask whether an id names a person at all.
///
/// <b>Milestone 018 narrows, rather than repeals, the rule that nothing here reads
/// <see cref="World.TruthLog"/>, <see cref="World.Decisions"/>, <see cref="World.Reports"/>,
/// <see cref="World.Requests"/>, an organisational condition, or any utility score.</b> Two fields
/// read two of those collections, each filtered to this viewpoint character alone and reduced to
/// audited typed fields — never a raw record, a score, a candidate id, a report's candour or withheld
/// list, or a relationship's strength/confidence: <see cref="LastAction"/> reads
/// <see cref="World.Decisions"/> for `ActorId == this character` — his own decisions only — and keeps
/// only `.Chosen.Candidate` and `.At`; <see cref="AwaitingAnswers"/> reads <see cref="World.Requests"/>
/// for `AskerId == this character` and is resolved entirely from his own
/// <see cref="Domain.Cognition.Testimony"/>, never from <see cref="World.Decisions"/> for anybody —
/// the review's second correction rejected an interim version that read the <em>asked</em> person's
/// own `DecisionRecord` existence to distinguish pending from declined, because that told the asker
/// about a private mental event nothing in the fiction had communicated to him.
/// <see cref="World.TruthLog"/> and <see cref="World.Reports"/> themselves remain untouched by this
/// type, and <see cref="World.Decisions"/> is never read here for any character other than the
/// viewpoint. See <see cref="PlayerCommittedAction"/> and <see cref="PlayerRequest"/>'s own doc
/// comments for the exact boundary each keeps.
///
/// <b>Milestone 025 removed two projections rather than adding any.</b> <c>RecentTrustMovements</c>
/// (milestone 018) read <see cref="World.AccountConflicts"/> and <see cref="World.AccountAgreements"/>
/// to say that trust toward somebody moved, without saying why; milestone 023's
/// <see cref="PlayerAttitude.History"/> reads the same events and does say why, so the older
/// projection was a second, less informative derivation of one fact and went. <c>Recent</c> was the
/// same <see cref="PlayerBelief"/> objects as <see cref="Known"/>, re-sorted; recency is now the
/// order of <see cref="Known"/> as the interface draws it, not a second list.
///
/// It is a snapshot rather than a live view on purpose. A UI holding a reference into the running
/// world would be one property access away from the truth log; a record built once and handed over
/// cannot become more revealing later. <b>Every collection below is frozen at construction</b> — an
/// <c>IReadOnlyList&lt;T&gt;</c> backed by a <c>List&lt;T&gt;</c> is read-only by politeness and can
/// be cast straight back, which is the same defect milestone 006 fixed on relationship grievances and
/// milestone 009's review found again here.
/// </summary>
public sealed record PlayerSnapshot(
    DateTime Date,
    string ViewpointId,
    string ViewpointName,
    string ViewpointRole,
    Pronouns ViewpointPronouns,
    /// <summary>
    /// His own cash, copied out of <see cref="Domain.Capabilities.Cash"/> as a plain value at
    /// construction. A character always knows his own balance — this is not mediated truth the way a
    /// belief is, and it is not scored state the way a relationship reading is, so it is exempt from
    /// "no number reaches the player" by the same reasoning that exempts a calendar date: it is a fact
    /// about him, not a measurement the model took of anybody.
    /// </summary>
    double Cash,
    IReadOnlyList<PlayerIncome> Income,
    /// <summary>
    /// What he knows about himself — his own skills, in words. Milestone 026's first correction, on
    /// the same footing as <see cref="Cash"/>: his own state, copied out of his own
    /// <see cref="Domain.Capabilities"/>, never a number and never anybody else's.
    /// </summary>
    IReadOnlyList<string> SelfKnowledge,
    IReadOnlyList<PlayerBelief> Known,
    IReadOnlyList<PlayerDisagreement> Disagreements,
    IReadOnlyList<PlayerAttitude> Attitudes,
    IReadOnlyList<PlayerBelief> Unsettled,
    IReadOnlyList<PlayerPerson> Silent,
    /// <summary>Who told him they knew nothing of what he asked, oldest first — milestone 026.</summary>
    IReadOnlyList<PlayerDisclaimer> Disclaimers,
    /// <summary>
    /// What hangs over him, as sentences — milestone 026's second correction. Empty when nothing
    /// does. Built entirely from his own state: the acts he holds that name him, whether he holds
    /// that somebody saw, what he himself has told whom about it (his own sent reports), what he
    /// read off their faces, and whether anybody has put it to him. Never whether anybody else
    /// knows: that is their state, and it reaches him only if they say so.
    /// </summary>
    IReadOnlyList<string> Exposure,
    /// <summary>What he just did, if his last committed action is still his most recent. Null only
    /// when he has never yet committed to anything at all.</summary>
    PlayerCommittedAction? LastAction,
    /// <summary>His own business's paying status, or null when he owns none.</summary>
    PlayerBusinessStatus? MyBusiness,
    /// <summary>His active orders and work he executes for someone else.</summary>
    IReadOnlyList<PlayerOperation> Operations,
    IReadOnlyList<PlayerRequest> AwaitingAnswers)
{
    public IReadOnlyList<PlayerIncome> Income { get; init; } = Frozen.List(Income);
    public IReadOnlyList<PlayerOperation> Operations { get; init; } = Frozen.List(Operations);
    public IReadOnlyList<string> SelfKnowledge { get; init; } = Frozen.List(SelfKnowledge);
    public IReadOnlyList<PlayerBelief> Known { get; init; } = Frozen.List(Known);
    public IReadOnlyList<PlayerDisagreement> Disagreements { get; init; } = Frozen.List(Disagreements);
    public IReadOnlyList<PlayerAttitude> Attitudes { get; init; } = Frozen.List(Attitudes);
    public IReadOnlyList<PlayerBelief> Unsettled { get; init; } = Frozen.List(Unsettled);
    public IReadOnlyList<PlayerPerson> Silent { get; init; } = Frozen.List(Silent);
    public IReadOnlyList<PlayerDisclaimer> Disclaimers { get; init; } = Frozen.List(Disclaimers);
    public IReadOnlyList<string> Exposure { get; init; } = Frozen.List(Exposure);
    public IReadOnlyList<PlayerRequest> AwaitingAnswers { get; init; } = Frozen.List(AwaitingAnswers);
}

/// <summary>
/// The one place that decides what a viewpoint character may be shown.
///
/// Before milestone 009 this logic lived inside the console renderer, which was fine while there was
/// one surface. There are now two, and two independent answers to "what may this character be shown"
/// is precisely the failure shape `REVIEW_LEDGER.md` records as *a distinction drawn in one place and
/// dropped on the way to the next*. `IntelligenceWriter` renders this snapshot rather than deriving
/// its own; the Godot interface displays its fields. Neither can be more generous than the other,
/// because neither decides.
/// </summary>
public static class PlayerView
{
    /// <summary>
    /// How far back "recent" reaches — the part of what he holds that the interface puts in front of
    /// him first, and marks as fresh.
    ///
    /// A window over what he holds, not over what happened: every entry is already something he
    /// knows, and the window only decides how much of it is worth emphasising. A short window can
    /// therefore hide nothing he was entitled to — the full list is
    /// <see cref="PlayerSnapshot.Known"/>. Two weeks against a ninety-day scenario, chosen to be the
    /// right order of magnitude and nothing more. Milestone 025 made it emphasis inside one list
    /// rather than a second list; the constant is public so the renderer applies this window and
    /// not one of its own.
    /// </summary>
    public static readonly TimeSpan RecentWindow = TimeSpan.FromDays(14);

    /// <summary>
    /// The second person, as a pronoun set. Milestone 025: when the viewpoint is the character the
    /// player controls, the interface speaks to him as "you" — "you would take his word" — and every
    /// narrated phrase produces that voice through the same <see cref="Pronouns"/> parameter it
    /// already took. A watched character and the console keep the third person.
    ///
    /// Constructed here rather than declared beside <see cref="Pronouns.He"/>: the domain's own set
    /// describes characters as they are, and "you" is a fact about who is reading, which is the
    /// session's business.
    /// </summary>
    public static readonly Pronouns You = new("you", "you", "your", "yourself", PluralVerb: true);

    /// <summary>Whether a pronoun set addresses the reader. Only <see cref="Describe"/> needs to know.</summary>
    public static bool IsSecondPerson(Pronouns p) => p.Subject == You.Subject;

    /// <summary>
    /// Display names, which are public knowledge — the only thing the world is asked for on behalf
    /// of any surface. A person's name, a business's name, or — milestone 025 — a policy's own
    /// description, so that a rule reaches the player as the words it was given in rather than as
    /// its id. One function, shared with <see cref="SimulationSession"/>, so an option and a belief
    /// resolve the same id to the same words.
    /// </summary>
    public static Func<string, string> NameIn(World world)
        => id => world.Find(id)?.Name
                 ?? world.Businesses.GetValueOrDefault(id)?.Name
                 ?? world.Org.PolicyById(id)?.Description
                 ?? id;

    /// <summary>
    /// <paramref name="voice"/> is the pronoun set the snapshot speaks in, defaulting to the
    /// viewpoint character's own. The session passes <see cref="You"/> when the viewpoint is the
    /// controlled character; nothing about what is shown changes with it, only how it is put.
    /// </summary>
    public static PlayerSnapshot Build(World world, string viewpointId, DateTime asOf, Pronouns? voice = null)
    {
        var who = world.Get(viewpointId);
        var self = voice ?? who.Pronouns;
        var name = NameIn(world);
        bool IsPerson(string id) => world.Find(id) is not null;

        // How to refer to somebody else. A business is not a person and takes the neuter form
        // nothing currently asks for, so an unknown id falls back to the same default Character
        // carries — which is the position everything was in before pronouns existed, not a new
        // assumption introduced here.
        Pronouns Theirs(string id) => world.Find(id)?.Pronouns ?? Domain.Pronouns.He;

        string Statement(Claim c) => PlayerNarration.Describe(c, name, who.Id, self);

        PlayerBelief Belief(InformationRecord r)
        {
            bool contested = who.Cognition.IsContested(r.Claim);
            return new PlayerBelief(
                // The predicate crosses, the truth-log counter does not. See PlayerClaim.
                PlayerClaim.Of(r.Claim),
                Statement(r.Claim),
                r.AcquiredAt,
                r.ReconsideredAt,
                PlayerNarration.Certainty(r, contested, self),
                PlayerNarration.Attribute(r, name, self, IsPerson, who.Id),
                contested,
                r.IsHeld);
        }

        // ---------------------------------------------------------------- what he has
        var heldRecords = who.Cognition.Records
            .Where(r => r.IsHeld)
            .OrderBy(r => r.AcquiredAt)
            .ThenBy(r => r.Claim.ToString(), StringComparer.Ordinal)
            .ToList();

        var held = heldRecords.Select(Belief).ToList();

        // ---------------------------------------------------------------- disagreement
        var disagreements = new List<PlayerDisagreement>();
        foreach (var claim in who.Cognition.Testimony
                     .Select(t => t.Claim)
                     .Distinct()
                     .Where(who.Cognition.IsContested)
                     .OrderBy(c => c.ToString(), StringComparer.Ordinal))
        {
            // What he had on his own account — seen, done, found or worked out — belongs in the
            // list alongside the others rather than above them as the answer. A conclusion he
            // reached himself is still just one account, and it is the one most likely to be wrong
            // about who was responsible. Anything he was told is already listed below under the
            // name of the man who gave it.
            var own = who.Cognition.Find(claim);
            string? ownBasis = own is null ? null : PlayerNarration.OwnBasis(own, self);

            var accounts = who.Cognition.AccountsOf(claim)
                .OrderBy(t => t.At)
                .Select(t => new PlayerAccount(t.SenderId, name(t.SenderId), t.Affirms, t.At))
                .ToList();

            disagreements.Add(new PlayerDisagreement(
                PlayerClaim.Of(claim),
                Statement(claim),
                ownBasis,
                own?.IsHeld ?? false,
                accounts));
        }

        // ---------------------------------------------------------------- how he takes people
        // What he takes a man to be good for, off his own beliefs — never the man's actual
        // Capabilities, which he has no way to read. Null on a bar means he has formed no view of
        // it, which the renderer keeps distinct from a poor view.
        //
        // THROUGH CapabilityBar.Read, THE SAME DERIVATION THE SCORER USES. Milestone 021's
        // correction. This used to scan the raw records itself, so a character holding the high bar
        // while rejecting the low one would be described here as somebody the player "would not send
        // to lean on anybody" while the decision that sent him had weighed him as a hard man. Two
        // readers resolving a ladder independently is the failure REVIEW_LEDGER.md records as a
        // distinction drawn in one place and dropped on the way to the next; there is now one
        // resolution and two renderings of it.
        InformationRecord? Position(Claim claim)
        {
            foreach (var r in who.Cognition.Records)
                if (r.Claim.Equals(claim)) return r;
            return null;
        }

        string? TakenFor(string personId)
        {
            var reading = CapabilityBar.Read(personId, Position);
            return PlayerNarration.TakenFor(
                CapabilityBar.Clears(reading, CapabilityBar.RoughWork),
                CapabilityBar.Clears(reading, CapabilityBar.HardMan),
                self,
                Theirs(personId));
        }

        var attitudes = KnownPeople(world, who)
            .Select(id => (Id: id, Rel: who.Social.Toward(id), TakenFor: TakenFor(id)))
            // A man he has an opinion of the usefulness of belongs on the roster even with no
            // relationship dimension moved toward him — otherwise the one thing this column was
            // extended to show could be filtered out before it was ever rendered.
            .Where(x => x.Rel.Trust > 0 || x.Rel.Fear > 0 || x.Rel.Grievances.Count > 0
                        || x.TakenFor is not null || x.Rel.Impressions.Count > 0)
            .Select(x => new PlayerAttitude(
                x.Id,
                name(x.Id),
                Theirs(x.Rel.OtherId),
                PlayerNarration.Standing(
                    x.Rel.Trust, self, Theirs(x.Rel.OtherId), everMoved: x.Rel.StandingHistory.Count > 0),
                PlayerNarration.Wariness(x.Rel.Fear, self, Theirs(x.Rel.OtherId)),
                x.TakenFor,
                // Quoted verbatim by the surfaces that show them. Grievance descriptions are
                // written from the holder's own side and mostly in the first person, so they read
                // correctly as his words about it and stay his.
                x.Rel.Grievances.Select(g => g.Description).ToList(),
                // And why his standing moved, oldest first — the order it happened in, which is the
                // order a history has to be read in. Rendered here, from the typed cause, rather
                // than carried as prose out of the domain.
                x.Rel.StandingHistory.Select(h => new PlayerStandingMoment(
                    PlayerNarration.WhyStandingMoved(
                        h.Cause, self, name(x.Rel.OtherId),
                        h.About is { } about ? Statement(about) : null),
                    PlayerNarration.Warmed(h.Cause),
                    h.At)).ToList(),
                // And what he has read off the man's face — milestone 026. His readings, which can
                // be wrong; the man's own state is never consulted here.
                x.Rel.Impressions.Select(i => new PlayerImpression(
                    x.Id,
                    name(x.Id),
                    PlayerNarration.Impression(
                        i.Kind, self, name(x.Id), i.About is { } about ? Statement(about) : null),
                    i.At)).ToList()))
            .ToList();

        // ---------------------------------------------------------------- open questions
        // Thin or disputed. Read off the record's own confidence rather than the rendered phrase,
        // because the phrase is deliberately coarse and cannot answer "under a half" — which is the
        // threshold, and is hidden state precisely so that it can be.
        var unsettled = heldRecords
            .Where(r => r.Confidence < 0.5 || who.Cognition.IsContested(r.Claim))
            .OrderBy(r => r.Claim.ToString(), StringComparer.Ordinal)
            .Select(Belief)
            .ToList();

        // Who he could go to — drawn from the people he has actually heard of, never from the
        // world's roster. Enumerating the organisation here would put names in front of the player
        // that the viewpoint character has no way to know, and "who else is in this outfit" is
        // exactly the kind of thing a boss might be wrong about.
        var silent = KnownPeople(world, who)
            .Where(id => !who.Cognition.HasAccountFrom(id))
            .Select(id => new PlayerPerson(id, name(id)))
            .ToList();

        // Who told him they knew nothing — milestone 026. Read from his own testimony, the same
        // record a request's resolution is read from, so the two cannot disagree about whether a
        // question was answered.
        var disclaimers = who.Cognition.Disclaimers
            .OrderBy(t => t.At)
            .ThenBy(t => t.SenderId, StringComparer.Ordinal)
            .Select(t => new PlayerDisclaimer(
                t.SenderId, name(t.SenderId),
                PlayerNarration.Disclaimer(name(t.SenderId), Theirs(t.SenderId), Statement(t.Claim)),
                t.At))
            .ToList();

        // ---------------------------------------------------------------- what he just did
        //
        // The most recent entry world.Decisions holds for this character, reduced to exactly two
        // audited fields — see PlayerCommittedAction's own doc comment. world.Decisions is written by
        // Pipeline.Resolve for every commit regardless of who or what chose it, so this is
        // actor-neutral by construction: nothing here branches on whether a person or the pipeline
        // itself made the choice.
        var lastDecision = world.Decisions
            .Where(d => d.ActorId == who.Id)
            .OrderByDescending(d => d.At)
            .ThenByDescending(d => d.Id)
            .FirstOrDefault();

        // Chosen is null exactly when nothing was open to him; DecisionRecord.Outcome in that case is
        // the developer-only literal "nothing was open to him" and must never be read here.
        PlayerCommittedAction? lastAction = lastDecision?.Chosen is { Candidate: var chosenCandidate }
            ? new PlayerCommittedAction(
                lastDecision.At, PlayerOption.Describe(chosenCandidate, name, self, Theirs, who.Id))
            : null;

        // ---------------------------------------------------------------- his own business
        var ownedBusiness = world.Businesses.Values.FirstOrDefault(b => b.OwnerId == who.Id);
        PlayerBusinessStatus? myBusiness = ownedBusiness is null
            ? null
            : new PlayerBusinessStatus(ownedBusiness.Id, ownedBusiness.Name, ownedBusiness.PayingTribute);

        // ---------------------------------------------------------------- cash this character received
        // The balance and its receipts are the same character-owned state. This names no payer's
        // cash and reads no truth log: each receipt was written atomically with this character's own
        // balance change at collection.
        var income = who.Capabilities.CashReceipts
            .OrderBy(r => r.At)
            .Select(r => new PlayerIncome(
                r.At,
                r.Amount,
                name(r.SourceId),
                name(r.ExecutorId),
                r.ExecutorId == who.Id))
            .ToList();

        // ---------------------------------------------------------------- awaiting answers
        //
        // Corrected three times by milestone 018's review. The first correction read World.Decisions
        // for the ASKED character to tell "not yet decided" from "decided and declined" — Codex's
        // second review correctly rejected that: the asker never received any message establishing
        // the asked person had decided anything at all, so it exposed a private mental event nothing
        // in the fiction communicated to him. A same-pass attempt to split a communicated denial out
        // as its own "Declined" value was also wrong: the natural proof has Vincent give Salvatore a
        // full, sincere account that happens to contradict him, which is an answer, not a refusal. The
        // third correction removed the now-pointless two-value type entirely: every request that
        // reaches AwaitingAnswers is, by construction, one nothing has answered.
        //
        // "Answered" is read entirely from this character's own Cognition.Testimony — never
        // World.Decisions for anybody but the viewpoint himself (see LastAction above), never
        // World.Reports/Report.AnsweringClaim, and never elapsed calendar time. Two different private,
        // uncommunicated choices by the asked person — silence, or a report that withholds precisely
        // this claim — both produce no testimony at all and are therefore structurally
        // indistinguishable here: both leave the request in this list, because that is genuinely all
        // the asker can tell. Neither may ever be described as a communicated refusal; only an actual
        // account, in either direction, removes a request from this list.
        bool Answered(InformationRequest r)
            => who.Cognition.Testimony.Any(t => t.SenderId == r.AskedId && t.Claim.Equals(r.About) && t.At >= r.At);

        var awaitingAnswers = world.Requests
            .Where(r => r.AskerId == who.Id && !Answered(r))
            .OrderBy(r => r.At)
            .ThenBy(r => r.Id)
            .Select(r => new PlayerRequest(
                r.AskedId, name(r.AskedId), Theirs(r.AskedId),
                PlayerClaim.Of(r.About), Statement(r.About), r.At))
            .ToList();

        return new PlayerSnapshot(
            asOf,
            who.Id,
            who.Name,
            who.RoleTitle,
            self,
            who.Capabilities.Cash,
            income,
            PlayerNarration.SelfKnowledge(
                who.Capabilities[Skill.Persuasion], who.Capabilities[Skill.Coercion],
                who.Capabilities[Skill.Discretion], who.Capabilities[Skill.Investigation], self),
            held,
            disagreements,
            attitudes,
            unsettled,
            silent,
            disclaimers,
            Exposure(world, who, heldRecords, name, self, Statement),
            lastAction,
            myBusiness,
            Operating(world, who, name, self),
            awaitingAnswers);
    }

    /// <summary>
    /// What hangs over him — milestone 026's second correction, on Matt's finding that a pause
    /// offered "cover it up" and "tell Salvatore it did not happen" with nothing on screen saying
    /// why. The options come from his own guilty knowledge, not from anybody having found out, and
    /// this says so in his own terms.
    ///
    /// <b>Every clause has a source on his side.</b> The acts: held claims that name him as the man
    /// who used force or broke a rule. The witness: a held claim that somebody saw him. What he told
    /// whom: his own sent reports, read for the claim — asserted, withheld, or denied — which are his
    /// own acts and his to remember. The reading: his impression of the man he told. What came back:
    /// questions put to him and accounts given to him about it, or the statement that nobody has
    /// raised it, which is a statement about his own testimony log. Nothing here consults anybody
    /// else's cognition, and "Salvatore knows" is a sentence this can never produce.
    /// </summary>
    private static IReadOnlyList<string> Exposure(
        World world, Character who, IReadOnlyList<InformationRecord> heldRecords,
        Func<string, string> name, Pronouns self, Func<Claim, string> statement)
    {
        var acts = heldRecords
            .Where(r => r.Claim.Subject == who.Id
                        && r.Claim.Kind is ClaimKind.PersonUsedViolence or ClaimKind.PersonBreachedPolicy)
            .ToList();
        if (acts.Count == 0) return Array.Empty<string>();

        var lines = new List<string>();
        string Cap(string s) => char.ToUpperInvariant(s[0]) + s[1..];

        // The acts. Violence first, and the rule it broke folded in rather than said twice.
        var violence = acts.Where(r => r.Claim.Kind == ClaimKind.PersonUsedViolence).ToList();
        bool breach = acts.Any(r => r.Claim.Kind == ClaimKind.PersonBreachedPolicy);
        if (violence.Count > 0)
            lines.Add(Cap(string.Join("; ", violence.Select(r => statement(r.Claim))))
                      + (breach ? ", against the outfit's rule." : "."));
        else
            lines.Add(Cap(string.Join("; ", acts.Select(r => statement(r.Claim)))) + ".");

        // Whether he holds that somebody saw him.
        foreach (var seen in heldRecords.Where(r => r.Claim.Kind == ClaimKind.WitnessSawIncident && r.Claim.Object == who.Id))
            lines.Add(Cap(statement(seen.Claim)) + ".");

        // What he has told whom about it — his own reports, his own acts.
        var actClaims = acts.Select(r => r.Claim).ToList();
        foreach (var recipient in world.Reports.Where(r => r.SenderId == who.Id).Select(r => r.RecipientId).Distinct())
        {
            var latest = world.Reports
                .Where(r => r.SenderId == who.Id && r.RecipientId == recipient)
                .Where(r => actClaims.Any(c => r.Asserted.Any(a => a.Claim.Equals(c)) || r.Withheld.Contains(c)))
                .OrderByDescending(r => r.At).ThenByDescending(r => r.Id)
                .FirstOrDefault();
            if (latest is null) continue;

            bool denied = latest.Asserted.Any(a => actClaims.Contains(a.Claim)
                                                   && a.AssertedStance is Stance.Rejects or Stance.Doubts);
            bool told = latest.Asserted.Any(a => actClaims.Contains(a.Claim)
                                                 && a.AssertedStance is Stance.Knows or Stance.Believes or Stance.Suspects);
            string when = latest.At.ToString("d MMMM", System.Globalization.CultureInfo.InvariantCulture);
            string what = denied ? $"denied it to {name(recipient)}"
                : told ? $"told {name(recipient)} about it"
                : $"kept it from {name(recipient)}";
            string line = $"{self.Subject_} {what} on {when}";

            // And what he read off the man's face when he did — tied to this exact exchange, never
            // to the most recent reaction about any act claim. A correction to milestone 026's second
            // correction (a Codex finding): scoped only by recipient and "about an act claim", a
            // report that merely withheld this incident could still surface an older reaction to a
            // different one, because the lookup below never checked that the reaction came from
            // `latest` at all. A read is never recorded for a withheld claim in the first place
            // (`Reactions.AfterReport` reads only what was actually asserted), so requiring the
            // impression's own timestamp to match `latest.At` and its claim to be one `latest`
            // actually asserted is what a withheld-only report needs to correctly surface nothing.
            //
            // A second correction to that correction (a further Codex finding): timestamp and claim
            // together still are not unique — two distinct reports to the same man, about the same
            // claim, at the same instant, are not forbidden, and neither was distinguishable this way.
            // `Impression.ReportId` names the report that actually produced the reading, so matching
            // it against `latest.Id` is the one check that cannot be fooled by that coincidence; the
            // timestamp and claim checks are kept rather than dropped; they are just no longer load-
            // bearing on their own.
            var assertedActClaims = latest.Asserted
                .Where(a => actClaims.Contains(a.Claim))
                .Select(a => a.Claim)
                .ToList();
            var read = who.Social.Toward(recipient).Impressions
                .Where(i => i.ReportId == latest.Id
                            && i.At == latest.At && i.About is { } about && assertedActClaims.Contains(about))
                .OrderByDescending(i => i.At)
                .FirstOrDefault();
            if (read is not null)
            {
                string reading = PlayerNarration.Impression(read.Kind, self, name(recipient), null);
                // The blank reading for a report reads "could not tell whether he believed you"; the
                // null-about form above is the fear one, so build the belief wording by hand here.
                if (read.Kind == ImpressionKind.GaveNothingAway)
                    reading = $"{self.Subject} could not tell whether {name(recipient)} believed {self.Object}";
                line += $", and {reading}";
            }
            lines.Add(line + ".");
        }

        // What has come back to him about it.
        var raised = new List<string>();
        foreach (var q in world.Requests.Where(q => q.AskedId == who.Id && actClaims.Contains(q.About)).OrderBy(q => q.At))
            raised.Add($"{name(q.AskerId)} asked {self.Object} about it on " +
                       q.At.ToString("d MMMM", System.Globalization.CultureInfo.InvariantCulture));
        foreach (var t in actClaims.SelectMany(who.Cognition.AccountsOf).OrderBy(t => t.At))
            raised.Add($"{name(t.SenderId)} spoke to {self.Object} about it on " +
                       t.At.ToString("d MMMM", System.Globalization.CultureInfo.InvariantCulture));
        lines.Add(raised.Count == 0
            ? $"Nobody has raised it with {self.Object}."
            : Cap(string.Join("; ", raised)) + ".");

        return lines;
    }

    /// <summary>
    /// Project owned orders and the viewpoint actor's current execution in stable identity order.
    /// Ownership does not imply access to a delegate's progress; only the executor sees that state.
    /// Work wording is shared with PlayerOption, without exposing internal ids or mutable instances.
    /// </summary>
    private static IReadOnlyList<PlayerOperation> Operating(World world, Character who, Func<string, string> name, Pronouns self)
        => world.Characters.Values.SelectMany(c => c.Execution.Operations)
            .Where(s => s.OwnerId == who.Id || s.DelegatedToId == who.Id)
            .OrderBy(s => s.OwnerId, StringComparer.Ordinal).ThenBy(s => s.LocalSequence)
            .Select(s => ProjectOperation(who, s, name, self)).ToList();

    private static PlayerOperation ProjectOperation(Character who, StrategyInstance s, Func<string, string> name, Pronouns self)
    {
        // StepIndex is the *next* step to run, so the last one completed is the one before it. Null
        // before anything has run, which reads as not having started in earnest rather than as a
        // step named "nothing".
        var steps = Strategy.Strategies.StepsFor(s.Kind);
        string? lastDone = s.StepIndex > 0 && s.StepIndex - 1 < steps.Length
            ? steps[s.StepIndex - 1]
            : null;

        // The one man actually doing the work — the delegate if there is one, the owner himself
        // otherwise. Progress is shown only to him; everyone else who can see this operation at all
        // (the owner, watching a delegate) learns the outcome the way milestones 017 and 022 settled.
        bool doingItHimself = who.Id == (s.DelegatedToId ?? s.OwnerId);
        CoercionMethod visibleMethod = who.Id == s.OwnerId
            // The owner's own last order, never the delegate's live method. Production operations
            // always set this at start; Persuade is the fail-closed default for hand-built fixtures.
            ? s.OwnerOrderedMethod ?? CoercionMethod.Persuade
            : s.Method;

        return new PlayerOperation(
            PlayerOption.Work(s.Kind, s.TargetId, name, self),
            s.Kind == StrategyKind.SecureTribute && s.TargetId is { } target
                ? PlayerOption.Approach(visibleMethod, target, name)
                : PlayerOption.Work(s.Kind, s.TargetId, name, self),
            s.DelegatedToId is { } executor && executor != who.Id ? name(executor) : null,
            // The owner's own act, known to him regardless of who is carrying it now. Not the
            // executor's: StartedAt is when the operation began, which for a man it was later handed
            // to is not when he came to hold it — see PlayerOperation.Since's own comment.
            who.Id == s.OwnerId ? s.StartedAt : null,
            doingItHimself ? PlayerNarration.OwnProgress(lastDone, s.FailedAttempts, self)
                : who.Execution.OperationAccounts.LastOrDefault(a => a.OwnerId == s.OwnerId && a.Sequence == s.LocalSequence) is { } account
                    ? $"{name(account.SenderId)} gave an account of this operation; see the attributed information."
                    : $"No report about this operation yet; its progress is unknown to {self.Object}.",
            who.Id == s.OwnerId ? $"work-{s.LocalSequence}" : null);
    }

    /// <summary>
    /// The people this character could name, in id order.
    ///
    /// <b>The derivation lives in <see cref="Acquaintance.KnownTo"/></b>, one layer down, and this
    /// is a delegation rather than a copy: candidate generation asks the same question, through
    /// <c>GeneratorContext.AcquaintedIds</c>, and must get the same answer. It is what the character
    /// holds — whoever appears in a claim he holds, gave him an account, he has a relationship with,
    /// or he holds a grievance against — widened <b>only</b> by the holders of his own organisation's
    /// named posts, <c>Organization.Offices</c> and <c>BossId</c>.
    ///
    /// Note what it is not widened by: the organisation roster, and no authority scan standing in for
    /// an office. Milestone 009's second correction widened it by <c>Pipeline.SuperiorOf</c> and
    /// <c>SubordinatesOf</c> — which are that roster under another name — and was rejected for it;
    /// the third correction settled the rule above. Do not read the derivation off
    /// <see cref="Acquaintance.HeardOf"/>, which is the cognition-only half and is `internal`
    /// precisely because a test that compared this method against it, while the generators used the
    /// wider set, is how that leak survived the correction written to close it.
    ///
    /// Public so tests can assert that no name outside this set ever reaches any surface.
    /// </summary>
    public static IReadOnlyList<string> KnownPeople(World world, Character who)
        => Acquaintance.KnownTo(world, who);
}
