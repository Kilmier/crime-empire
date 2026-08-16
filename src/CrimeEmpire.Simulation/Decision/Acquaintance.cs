namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Sim;

/// <summary>
/// Who a character has any reason to know exists. <b>One derivation, and it is
/// <see cref="KnownTo"/>.</b>
///
/// The player-facing view has always needed this — it must never put a name in front of the player
/// that the viewpoint character has no way to know — and candidate generation needs the same answer
/// for the same reason: an option targeting somebody the actor has never heard of is an option his
/// beliefs do not support. <c>PlayerView.KnownPeople</c> and <c>GeneratorContext.AcquaintedIds</c>
/// both read this method and nothing else.
///
/// <b>Two corrections got here, and the second one's mistake is the instructive one.</b> Milestone
/// 009 shipped with the corroboration generator picking its target straight out of the authoritative
/// organisation roster. Correction 2 narrowed that by knowledge and then widened it again by
/// "office relationships" — derived from <c>Pipeline.SuperiorOf</c> and
/// <c>Pipeline.SubordinatesOf</c>, which are scans of <c>world.Characters</c> for members at a
/// neighbouring <see cref="Capabilities.Authority"/>. That is the same roster, one layer down and
/// under a different name, so a same-organisation stranger one rung below the actor was still
/// reachable and still rendered by name. The lesson is narrow and worth keeping: <b>naming a thing
/// after the justification does not make it the justification.</b> "Office relationship" is only an
/// office relationship if it comes from an office.
///
/// <b>What it may consult.</b> The character's own cognition and social state, plus the
/// organisation's explicit institutional positions — <see cref="Org.Organization.Offices"/> and
/// <see cref="Org.Organization.BossId"/>, which are named formal posts rather than a headcount. The
/// world is asked only whether an id names a person at all, which resolves references he already has
/// rather than introducing new ones.
/// </summary>
public static class Acquaintance
{
    /// <summary>
    /// Everyone this character could name: whoever he has heard of, plus the officeholders of his own
    /// organisation.
    ///
    /// This is the authoritative set. Both readers use it, so neither can be more generous than the
    /// other, and the two cannot drift — which they had, in opposite directions, twice.
    /// </summary>
    public static IReadOnlyList<string> KnownTo(World world, Character who)
    {
        var known = new SortedSet<string>(HeardOf(world, who), StringComparer.Ordinal);
        foreach (var id in Officeholders(world, who)) known.Add(id);
        known.Remove(who.Id);
        return known.ToList();
    }

    /// <summary>
    /// Everyone this character has heard of, in id order: whoever appears in a claim he holds,
    /// whoever has given him an account, whoever he has a relationship with, and whoever he holds a
    /// grievance against.
    ///
    /// Note that <see cref="Cognition.Records"/> is scanned rather than held beliefs alone. Having
    /// come to reject something somebody told you does not unmake your having heard of them.
    ///
    /// Internal, so that nothing outside this file can accidentally read the cognition-only half and
    /// believe it has asked the whole question. A test that compared <c>PlayerView.KnownPeople</c>
    /// against this — while the generators used the wider set — was how Correction 2's leak went
    /// unnoticed.
    /// </summary>
    internal static IReadOnlyList<string> HeardOf(World world, Character who)
    {
        var known = new SortedSet<string>(StringComparer.Ordinal);

        void Consider(string? id)
        {
            if (string.IsNullOrEmpty(id) || id == who.Id) return;
            if (world.Find(id) is null) return;
            known.Add(id);
        }

        foreach (var r in who.Cognition.Records)
        {
            Consider(r.Claim.Subject);
            Consider(r.Claim.Object);
            Consider(r.SourceId);
        }

        foreach (var t in who.Cognition.Testimony) Consider(t.SenderId);
        foreach (var id in who.Social.Others) Consider(id);
        foreach (var g in who.Social.Grievances) Consider(g.AgainstId);

        return known.ToList();
    }

    /// <summary>
    /// The people holding a named institutional position in this character's own organisation: the
    /// boss, and whoever holds each <see cref="Org.Office"/>.
    ///
    /// <b>Why an office is knowledge and a headcount is not.</b> An office is a formal, named post —
    /// "capo, harbour" — and <see cref="Inference"/> already reads who holds which office in one's own
    /// organisation as an institutional fact a member is party to, while refusing everything else
    /// about other characters. Membership is a different thing entirely:
    /// <c>IntelligenceWriter</c> has said since milestone 003 that "who else is in this outfit" is
    /// exactly the kind of thing a boss can be wrong about, and rank is a property of a person rather
    /// than a post, so "everyone one rung below me" is a fact about the roster and not about any
    /// institution.
    ///
    /// A soldier holding no office is therefore not knowable this way, however senior he is. He
    /// becomes knowable the moment anything actually puts him in somebody's head.
    ///
    /// Empty for anybody outside the organisation — a grocer is party to none of this.
    /// </summary>
    internal static IReadOnlyList<string> Officeholders(World world, Character who)
    {
        if (who.Social.OrganizationId is not { } orgId || orgId != world.Org.Id)
            return Array.Empty<string>();

        var holders = new SortedSet<string>(StringComparer.Ordinal);

        if (world.Org.BossId is { } boss && world.Find(boss) is not null) holders.Add(boss);

        foreach (var office in world.Org.Offices)
            if (office.HolderId is { } holder && world.Find(holder) is not null)
                holders.Add(holder);

        holders.Remove(who.Id);
        return holders.ToList();
    }
}
