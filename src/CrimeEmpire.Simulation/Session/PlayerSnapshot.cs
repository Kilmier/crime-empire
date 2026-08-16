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
    string Standing,
    string? Wariness,
    IReadOnlyList<string> Grievances)
{
    /// <summary>Frozen at construction — see <see cref="Frozen"/>.</summary>
    public IReadOnlyList<string> Grievances { get; init; } = Frozen.List(Grievances);
}

/// <summary>
/// Everything one character could tell you, at one moment, as immutable data.
///
/// THE RULE THIS TYPE EXISTS TO ENFORCE: every field below is derived from the viewpoint character's
/// own <see cref="Cognition"/> and <see cref="SocialState"/> and from nothing else.
/// <see cref="World"/> is consulted only to turn ids into display names, which are public knowledge,
/// and to ask whether an id names a person at all. Nothing here reads <see cref="World.TruthLog"/>,
/// <see cref="World.Decisions"/>, <see cref="World.Reports"/>, <see cref="World.Requests"/>, an
/// organisational condition, or any utility score.
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
    IReadOnlyList<PlayerBelief> Known,
    IReadOnlyList<PlayerBelief> Recent,
    IReadOnlyList<PlayerDisagreement> Disagreements,
    IReadOnlyList<PlayerAttitude> Attitudes,
    IReadOnlyList<PlayerBelief> Unsettled,
    IReadOnlyList<PlayerPerson> Silent)
{
    public IReadOnlyList<PlayerBelief> Known { get; init; } = Frozen.List(Known);
    public IReadOnlyList<PlayerBelief> Recent { get; init; } = Frozen.List(Recent);
    public IReadOnlyList<PlayerDisagreement> Disagreements { get; init; } = Frozen.List(Disagreements);
    public IReadOnlyList<PlayerAttitude> Attitudes { get; init; } = Frozen.List(Attitudes);
    public IReadOnlyList<PlayerBelief> Unsettled { get; init; } = Frozen.List(Unsettled);
    public IReadOnlyList<PlayerPerson> Silent { get; init; } = Frozen.List(Silent);
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

        string Name(string id) =>
            world.Find(id)?.Name
            ?? world.Businesses.GetValueOrDefault(id)?.Name
            ?? id;

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
                PlayerNarration.Attribute(r, Name),
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
            string? ownBasis = own is null ? null : PlayerNarration.OwnBasis(own);

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
                PlayerNarration.Standing(x.Rel.Trust),
                PlayerNarration.Wariness(x.Rel.Fear),
                // Quoted verbatim by the surfaces that show them. Grievance descriptions are
                // written from the holder's own side and mostly in the first person, so they read
                // correctly as his words about it and stay his.
                x.Rel.Grievances.Select(g => g.Description).ToList()))
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

        return new PlayerSnapshot(
            asOf,
            who.Id,
            who.Name,
            who.RoleTitle,
            held,
            recent,
            disagreements,
            attitudes,
            unsettled,
            silent);
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
