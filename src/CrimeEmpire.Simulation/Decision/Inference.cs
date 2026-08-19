namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;

/// <summary>
/// Conclusions a character draws from what he already holds.
///
/// This exists because of a distinction that has to survive: some things are *seen* and some are
/// *worked out*, and the second kind can be wrong in ways the first cannot. Violence against a
/// shop is observable — a wrecked front, a beaten owner, people on the street. Who authorised it
/// is not observable at all. It is a conclusion about someone who may have been nowhere near the
/// place, and the only honest routes to it are being told or reasoning your way there.
///
/// So the boss reasons: there was violence, it happened on a patch that has a man responsible for
/// it, and I have a standing rule against exactly that. Therefore he probably sanctioned it. That
/// is a suspicion, it is recorded as <see cref="SourceKind.Inference"/> sourced to the man who
/// drew it, and it is wrong whenever a capo's crew acts without him.
///
/// What this may read is bounded. Institutional facts a member of an organisation is party to —
/// who holds which office in his own organisation — and the existence and location of a business,
/// which is visible from the street. Never another character's beliefs, never the truth log.
/// </summary>
public static class Inference
{
    /// <summary>How far a suspicion drawn this way falls short of the fact it rests on.</summary>
    private const double SuspicionOfFact = 0.45;

    /// <summary>
    /// Takes stock before deciding. Called at the top of the pipeline so it covers every route by
    /// which a character's information can change — noticing something, being told something, or
    /// simply being woken with what he already had.
    /// </summary>
    public static void Reconsider(World world, Character who, DateTime now)
    {
        var org = world.Org;
        if (who.Social.OrganizationId is null || who.Social.OrganizationId != org.Id) return;

        // ToList: Learn writes to the same collection OfKind reads.
        foreach (var violence in who.Cognition.OfKind(ClaimKind.PersonUsedViolence).ToList())
        {
            if (world.Businesses.GetValueOrDefault(violence.Claim.Object) is not { } business) continue;

            var office = org.OfficeForDomain(business.DistrictId);
            if (office?.HolderId is not { } holder) continue;

            // He does not need to deduce his own conduct, and the man who did it is not thereby
            // the man who sanctioned it.
            if (holder == who.Id || holder == violence.Claim.Subject) continue;

            foreach (var policy in org.PoliciesForDomain(business.DistrictId))
            {
                if (policy.Kind != PolicyKind.NoPublicViolence) continue;

                // A rule nobody told him about cannot be a rule he notices being broken.
                if (!who.Cognition.Holds(policy.AwarenessClaim(org.Id))) continue;

                var breach = new Claim(ClaimKind.PersonBreachedPolicy, holder, policy.Id, violence.Claim.EventId);
                double confidence = SuspicionOfFact * violence.Confidence;

                // Only when it would actually move him. Re-deriving the same suspicion every time
                // he is woken would mark the belief as freshly reconsidered on a timer, which is
                // exactly what makes an exchange look like it has new content when it has none.
                if (who.Cognition.Find(breach) is { } already && already.Confidence >= confidence) continue;

                who.Cognition.Learn(breach, Stance.Suspects, confidence, SourceKind.Inference, who.Id, now);
            }
        }

        ReconsiderUnattributedShortfall(who, org, now);
    }

    /// <summary>
    /// A shortfall he cannot attribute becomes a shortfall he suspects has another cause.
    ///
    /// Milestone 012. The organisation's <see cref="OrgCondition.RevenueLoss"/> is an objective
    /// condition, and the boss's belief about why is an attribution sourced to somebody's account —
    /// never his own eyes. When that account has been contradicted (<see
    /// cref="InformationRecord.Contested"/>, a fact <see cref="Cognition"/> already records rather
    /// than a fresh threshold invented for this) and the condition has not gone away, he has enough
    /// to suspect the attribution is wrong without anybody telling him what is actually true. That is
    /// the distinction this method exists to preserve: he ends up suspecting that <em>something</em>
    /// in his own domain is refusing, never which business. Reading <c>World.Businesses</c> to find
    /// out would answer the question he is not entitled to ask.
    ///
    /// <b>Only the boss.</b> The organisational condition is a fact about the family's books, not
    /// about any one man's patch, and only its leadership is answerable for it. Ruling 1's opening
    /// asymmetry is otherwise untouched: nobody's starting knowledge changes, and this is the route
    /// <em>out</em> of it that the milestone was scoped to add, not a second route in.
    ///
    /// <b>Which domain.</b> The office structure of his own organisation — <c>Organization.Offices</c>
    /// — is the same institutional fact this file already reads to find who holds which post; the
    /// domain an office covers is part of that same fact, not a business lookup. The fixture has
    /// exactly one office and one domain, so this does not have to choose between several; a second
    /// domain would need a rule this milestone was not asked to write.
    /// </summary>
    private static void ReconsiderUnattributedShortfall(Character who, Organization org, DateTime now)
    {
        if (who.Id != org.BossId) return;
        if (org.Condition(OrgCondition.RevenueLoss) < Organization.SignificantRevenueLoss) return;

        // The attribution he currently blames it on, if he still holds one and it has actually been
        // contradicted — an uncontested account gives him nothing to doubt. Ordered so the choice is
        // deterministic if he ever comes to hold more than one at once.
        var attributed = who.Cognition.Records
            .Where(r => r.Claim.Kind == ClaimKind.BusinessRefusesTribute && r.IsHeld
                        && who.Cognition.IsContested(r.Claim))
            .OrderBy(r => r.Claim.Subject, StringComparer.Ordinal)
            .FirstOrDefault();
        if (attributed is null) return;

        string? domain = org.Offices.Select(o => o.Domain).FirstOrDefault();
        if (domain is null) return;

        var gap = new Claim(ClaimKind.UnattributedShortfall, domain);
        double confidence = SuspicionOfFact * attributed.Confidence;

        // Same re-derivation guard as the policy-breach inference above, and for the same reason:
        // waking him and reaching an identical conclusion every time must not read as fresh content.
        if (who.Cognition.Find(gap) is { } alreadyGap && alreadyGap.Confidence >= confidence) return;

        who.Cognition.Learn(gap, Stance.Suspects, confidence, SourceKind.Inference, who.Id, now);
    }
}
