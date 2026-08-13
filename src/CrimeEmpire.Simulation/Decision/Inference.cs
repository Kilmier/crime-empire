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
    }
}
