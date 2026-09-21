namespace CrimeSim.Org;

using CrimeSim.Domain;
using CrimeSim.Sim;
using CrimeSim.Strategy;

/// <summary>Shared milestone-030 target pairing and receipt path, used by assignment and commissioning.</summary>
public static class AssignmentBriefing
{
    public static IReadOnlyList<ReportedClaim> CaptureTarget(Character issuer, string target)
    {
        var result = new List<ReportedClaim>();
        if (issuer.Cognition.Find(new Claim(ClaimKind.BusinessRefusesTribute, target)) is { IsHeld: true } refusal)
        {
            result.Add(ReportedClaim.Honest(refusal.Claim, refusal.Stance, refusal.Confidence, refusal.SourceKind));
            AddAssessment(issuer, target, result);
        }
        return result.AsReadOnly();
    }

    public static void AddAssessment(Character issuer, string target, List<ReportedClaim> disclosed)
    {
        if (issuer.Cognition.Find(new Claim(ClaimKind.TargetIsVulnerable, target)) is { IsHeld: true } r)
            disclosed.Add(ReportedClaim.Honest(r.Claim, r.Stance, r.Confidence, r.SourceKind));
    }

    public static bool Deliver(World world, Character recipient, string issuerId, string recipientId,
        IReadOnlyList<ReportedClaim> disclosed)
    {
        if (recipient.Id != recipientId) return false;
        foreach (var claim in disclosed)
        {
            var receipt = recipient.Cognition.Receive(claim, issuerId, world.Now);
            Strategies.ReviewAfterReceipt(world, recipient, receipt);
            if (receipt.Conflict is { } conflict)
            {
                world.AccountConflicts.Add(new PerceivedConflict(recipient.Id, conflict, world.Now));
                Relations.RecordAccountConflict(recipient, conflict, world.Now);
            }
            if (receipt.Agreement is { } agreement)
            {
                world.AccountAgreements.Add(new PerceivedAgreement(recipient.Id, agreement, world.Now));
                Relations.RecordAccountAgreement(recipient, agreement, world.Now);
            }
        }
        return true;
    }
}
