namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Sim;

/// <summary>
/// Who a character has any reason to know exists.
///
/// <b>One derivation, two readers.</b> The player-facing view has always needed this — it must never
/// put a name in front of the player that the viewpoint character has no way to know — and candidate
/// generation needs the same answer for the same reason: an option targeting somebody the actor has
/// never heard of is an option his beliefs do not support. Milestone 009's second correction found
/// them disagreeing, with the player view deriving it carefully and
/// <see cref="Generators.FromRelationship"/> reading the authoritative organisation roster instead.
/// Two answers to one question is the failure `REVIEW_LEDGER.md` names most often; this file is the
/// one answer, and <c>PlayerView.KnownPeople</c> now reads it rather than repeating it.
///
/// <b>What it may consult.</b> The character's own cognition and social state, and nothing else. The
/// world is asked only whether an id names a person at all — "the books" and a grocery are not people
/// to go and ask — which resolves references he already has rather than introducing new ones.
/// </summary>
public static class Acquaintance
{
    /// <summary>
    /// Everyone this character has heard of, in id order: whoever appears in a claim he holds,
    /// whoever has given him an account, whoever he has a relationship with, and whoever he holds a
    /// grievance against.
    ///
    /// Note that <see cref="Cognition.Records"/> is scanned rather than held beliefs alone. Having
    /// come to reject something somebody told you does not unmake your having heard of them.
    /// </summary>
    public static IReadOnlyList<string> HeardOf(World world, Character who)
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
    /// Everyone he could actually go and put a question to: whoever he has heard of, plus the office
    /// relationships he is party to by holding an office himself.
    ///
    /// <b>Why the office additions are justified and the roster is not.</b> A man knows who he
    /// answers to and who answers to him — that is what holding an office means, and
    /// <see cref="Inference"/> already draws the same line, reading "who holds which office in his own
    /// organisation" as an institutional fact a member is party to while refusing anybody else's
    /// beliefs. What he does not necessarily know is the *membership*: <c>ctx.OrgMemberIds</c> is the
    /// authoritative roster, rank-blind and belief-blind, and "who else is in this outfit" is exactly
    /// the kind of thing a boss can be wrong about. The rank-blindness is worth keeping — a boss
    /// seeking a second account has to be able to reach past the man who reports to him — so this
    /// narrows the roster by knowledge without narrowing it by rank.
    ///
    /// Deliberately a superset of <see cref="HeardOf"/> rather than a replacement for it: a soldier
    /// who has never exchanged a word with anybody still knows who his capo is, and a rule that made
    /// him unable to name his own superior would be a correctness fix that narrows what can be
    /// expressed — the first pattern on the ledger's list.
    /// </summary>
    public static IReadOnlyList<string> CouldApproach(
        World world,
        Character who,
        string? superiorId,
        IReadOnlyList<string> subordinateIds)
    {
        var reachable = new SortedSet<string>(HeardOf(world, who), StringComparer.Ordinal);

        if (superiorId is not null) reachable.Add(superiorId);
        foreach (var id in subordinateIds) reachable.Add(id);

        reachable.Remove(who.Id);
        return reachable.ToList();
    }
}
