namespace CrimeSim.Session;

using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Sim;

/// <summary>Somebody the viewpoint character has heard of. Id and display name, nothing else.</summary>
public sealed record PlayerPerson(string Id, string Name);

/// <summary>
/// One thing the viewpoint character holds, as he could relate it.
///
/// <see cref="Confidence"/> and <see cref="Attribution"/> are already-resolved qualitative phrases
/// rather than raw values, so no surface downstream can re-derive them differently — or print the
/// number.
/// </summary>
public sealed record PlayerBelief(
    PlayerClaim Claim,
    string Statement,
    DateTime AcquiredAt,
    DateTime ReconsideredAt,
    string Confidence,
    string Attribution,
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
    IReadOnlyList<string> Grievances,
    IReadOnlyList<PlayerStandingMoment> History)
{
    /// <summary>Frozen at construction — see <see cref="Frozen"/>.</summary>
    public IReadOnlyList<string> Grievances { get; init; } = Frozen.List(Grievances);

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
/// <see cref="PlayerNarration.Standing"/> and <see cref="PlayerNarration.Movement"/> already refuse.
/// </summary>
public sealed record PlayerStandingMoment(string Description, bool Warmed, DateTime At);

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
/// in <see cref="PlayerSnapshot.Known"/>, <see cref="PlayerSnapshot.Recent"/> or
/// <see cref="PlayerSnapshot.Disagreements"/> through the existing derivation, attributed the existing
/// way.
/// </summary>
public sealed record PlayerRequest(
    string AskedId,
    string AskedName,
    Pronouns AskedPronouns,
    PlayerClaim About,
    string Statement,
    DateTime AskedAt);

/// <summary>
/// Qualitative movement in the viewpoint character's own outward trust toward somebody, from a fresh
/// account conflict or agreement he personally received.
///
/// Added by milestone 018, and the one place <see cref="PlayerView.Build"/> reads
/// <see cref="World.AccountConflicts"/> and <see cref="World.AccountAgreements"/> — filtered to
/// <c>ListenerId == </c> this character, never anyone else's, so another character's own trust
/// movement can never appear here. Projects only who moved and which direction; never a strength,
/// a prior confidence, a prior source kind, or a claimed basis. Scoped to trust alone, because these
/// two collections are the only relationship-mutating events with an existing audit trail of "this
/// moved, this way, toward this person" — fear and grievance move through
/// <see cref="Domain.Relations.Frighten"/> and <see cref="Domain.Relations.RaiseGrievance"/>, neither
/// of which has an equivalent record, and this milestone adds no new persistent state to manufacture
/// one.
/// </summary>
public sealed record PlayerRelationshipMovement(
    string PersonId, string PersonName, Pronouns PersonPronouns, bool Warmed, DateTime At);

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
/// <see cref="World.Requests"/>, an organisational condition, or any utility score.</b> Four fields
/// now read four of those collections, each filtered to this viewpoint character alone and reduced to
/// audited typed fields — never a raw record, a score, a candidate id, a report's candour or withheld
/// list, or a relationship's strength/confidence: <see cref="LastAction"/> reads
/// <see cref="World.Decisions"/> for `ActorId == this character` — his own decisions only — and keeps
/// only `.Chosen.Candidate` and `.At`; <see cref="AwaitingAnswers"/> reads <see cref="World.Requests"/>
/// for `AskerId == this character` and is resolved entirely from his own
/// <see cref="Domain.Cognition.Testimony"/>, never from <see cref="World.Decisions"/> for anybody —
/// the review's second correction rejected an interim version that read the <em>asked</em> person's
/// own `DecisionRecord` existence to distinguish pending from declined, because that told the asker
/// about a private mental event nothing in the fiction had communicated to him;
/// <see cref="RecentTrustMovements"/> reads <see cref="World.AccountConflicts"/>/
/// <see cref="World.AccountAgreements"/> for `ListenerId == this character`. <see cref="World.TruthLog"/>
/// and <see cref="World.Reports"/> themselves remain untouched by this type, and
/// <see cref="World.Decisions"/> is never read here for any character other than the viewpoint. See
/// <see cref="PlayerCommittedAction"/>, <see cref="PlayerRequest"/> and
/// <see cref="PlayerRelationshipMovement"/>'s own doc comments for the exact boundary each keeps.
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
    IReadOnlyList<PlayerBelief> Known,
    IReadOnlyList<PlayerBelief> Recent,
    IReadOnlyList<PlayerDisagreement> Disagreements,
    IReadOnlyList<PlayerAttitude> Attitudes,
    IReadOnlyList<PlayerBelief> Unsettled,
    IReadOnlyList<PlayerPerson> Silent,
    /// <summary>What he just did, if his last committed action is still his most recent. Null only
    /// when he has never yet committed to anything at all.</summary>
    PlayerCommittedAction? LastAction,
    /// <summary>His own business's paying status, or null when he owns none.</summary>
    PlayerBusinessStatus? MyBusiness,
    IReadOnlyList<PlayerRequest> AwaitingAnswers,
    IReadOnlyList<PlayerRelationshipMovement> RecentTrustMovements)
{
    public IReadOnlyList<PlayerBelief> Known { get; init; } = Frozen.List(Known);
    public IReadOnlyList<PlayerBelief> Recent { get; init; } = Frozen.List(Recent);
    public IReadOnlyList<PlayerDisagreement> Disagreements { get; init; } = Frozen.List(Disagreements);
    public IReadOnlyList<PlayerAttitude> Attitudes { get; init; } = Frozen.List(Attitudes);
    public IReadOnlyList<PlayerBelief> Unsettled { get; init; } = Frozen.List(Unsettled);
    public IReadOnlyList<PlayerPerson> Silent { get; init; } = Frozen.List(Silent);
    public IReadOnlyList<PlayerRequest> AwaitingAnswers { get; init; } = Frozen.List(AwaitingAnswers);
    public IReadOnlyList<PlayerRelationshipMovement> RecentTrustMovements { get; init; } = Frozen.List(RecentTrustMovements);
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
    /// How far back "recent" reaches, for the observable-consequences feed.
    ///
    /// A window over what he holds, not over what happened: every entry is already something he
    /// knows, and the window only decides how much of it is worth putting in front of him first. A
    /// short window can therefore hide nothing he was entitled to — the full list is
    /// <see cref="PlayerSnapshot.Known"/>. Two weeks against a ninety-day scenario, chosen to be the
    /// right order of magnitude and nothing more.
    /// </summary>
    public static readonly TimeSpan RecentWindow = TimeSpan.FromDays(14);

    public static PlayerSnapshot Build(World world, string viewpointId, DateTime asOf)
    {
        var who = world.Get(viewpointId);
        var self = who.Pronouns;

        string Name(string id) =>
            world.Find(id)?.Name
            ?? world.Businesses.GetValueOrDefault(id)?.Name
            ?? id;

        // How to refer to somebody else. A business is not a person and takes the neuter form
        // nothing currently asks for, so an unknown id falls back to the same default Character
        // carries — which is the position everything was in before pronouns existed, not a new
        // assumption introduced here.
        Pronouns Theirs(string id) => world.Find(id)?.Pronouns ?? Domain.Pronouns.He;

        PlayerBelief Belief(InformationRecord r)
        {
            bool contested = who.Cognition.IsContested(r.Claim);
            return new PlayerBelief(
                // The predicate crosses, the truth-log counter does not. See PlayerClaim.
                PlayerClaim.Of(r.Claim),
                PlayerNarration.Describe(r.Claim, Name),
                r.AcquiredAt,
                r.ReconsideredAt,
                PlayerNarration.Qualify(r, contested),
                PlayerNarration.Attribute(r, Name, self),
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

        // ---------------------------------------------------------------- what has just moved
        //
        // Ordered by when he last had cause to think about it rather than by when he acquired it: a
        // three-week-old belief somebody contradicted yesterday is news, and a timeline keyed to
        // acquisition would bury it. ReconsideredAt falls back to AcquiredAt, so a claim nobody has
        // revisited still sorts by when he got it.
        var recent = held
            .Where(b => b.ReconsideredAt >= asOf - RecentWindow)
            .OrderByDescending(b => b.ReconsideredAt)
            .ThenBy(b => b.Claim.ToString(), StringComparer.Ordinal)
            .ToList();

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
                .Select(t => new PlayerAccount(t.SenderId, Name(t.SenderId), t.Affirms, t.At))
                .ToList();

            disagreements.Add(new PlayerDisagreement(
                PlayerClaim.Of(claim),
                PlayerNarration.Describe(claim, Name),
                ownBasis,
                own?.IsHeld ?? false,
                accounts));
        }

        // ---------------------------------------------------------------- how he takes people
        var attitudes = KnownPeople(world, who)
            .Select(id => (Id: id, Rel: who.Social.Toward(id)))
            .Where(x => x.Rel.Trust > 0 || x.Rel.Fear > 0 || x.Rel.Grievances.Count > 0)
            .Select(x => new PlayerAttitude(
                x.Id,
                Name(x.Id),
                Theirs(x.Rel.OtherId),
                PlayerNarration.Standing(x.Rel.Trust, self, Theirs(x.Rel.OtherId)),
                PlayerNarration.Wariness(x.Rel.Fear, self, Theirs(x.Rel.OtherId)),
                // Quoted verbatim by the surfaces that show them. Grievance descriptions are
                // written from the holder's own side and mostly in the first person, so they read
                // correctly as his words about it and stay his.
                x.Rel.Grievances.Select(g => g.Description).ToList(),
                // And why his standing moved, oldest first — the order it happened in, which is the
                // order a history has to be read in. Rendered here, from the typed cause, rather
                // than carried as prose out of the domain.
                x.Rel.StandingHistory.Select(h => new PlayerStandingMoment(
                    PlayerNarration.WhyStandingMoved(h.Cause, self, Name(x.Rel.OtherId)),
                    PlayerNarration.Warmed(h.Cause),
                    h.At)).ToList()))
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
            .Select(id => new PlayerPerson(id, Name(id)))
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
            ? new PlayerCommittedAction(lastDecision.At, PlayerOption.Describe(chosenCandidate, Name, self, Theirs))
            : null;

        // ---------------------------------------------------------------- his own business
        var ownedBusiness = world.Businesses.Values.FirstOrDefault(b => b.OwnerId == who.Id);
        PlayerBusinessStatus? myBusiness = ownedBusiness is null
            ? null
            : new PlayerBusinessStatus(ownedBusiness.Id, ownedBusiness.Name, ownedBusiness.PayingTribute);

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
                r.AskedId, Name(r.AskedId), Theirs(r.AskedId),
                PlayerClaim.Of(r.About), PlayerNarration.Describe(r.About, Name), r.At))
            .ToList();

        // ---------------------------------------------------------------- recent trust movement
        //
        // Trust only, own-listener-side only — see PlayerRelationshipMovement's own doc comment.
        var trustMovements = world.AccountConflicts
            .Where(c => c.ListenerId == who.Id && c.At >= asOf - RecentWindow)
            .Select(c => new PlayerRelationshipMovement(
                c.Conflict.SpeakerId, Name(c.Conflict.SpeakerId), Theirs(c.Conflict.SpeakerId), false, c.At))
            .Concat(world.AccountAgreements
                .Where(a => a.ListenerId == who.Id && a.At >= asOf - RecentWindow)
                .Select(a => new PlayerRelationshipMovement(
                    a.Agreement.SpeakerId, Name(a.Agreement.SpeakerId), Theirs(a.Agreement.SpeakerId), true, a.At)))
            .OrderByDescending(m => m.At)
            .ThenBy(m => m.PersonId, StringComparer.Ordinal)
            .ToList();

        return new PlayerSnapshot(
            asOf,
            who.Id,
            who.Name,
            who.RoleTitle,
            self,
            who.Capabilities.Cash,
            held,
            recent,
            disagreements,
            attitudes,
            unsettled,
            silent,
            lastAction,
            myBusiness,
            awaitingAnswers,
            trustMovements);
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
