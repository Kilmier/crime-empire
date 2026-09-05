namespace CrimeSim.Domain;

/// <summary>
/// What the outcome of delegated work does to the delegator's read of the man he sent.
///
/// The cognition counterpart of <see cref="Relations.RecordAccountConflict"/>, and deliberately the
/// same shape: one named consequence, in one place, taking only what the delegator can actually
/// perceive. It receives the executor's <em>id</em> and never his <see cref="Character"/>, so the
/// rule structurally cannot consult <c>Capabilities[Skill.Coercion]</c> — the omniscient read
/// milestone 020 shipped twice. That is enforced by the signature rather than by discipline.
///
/// <b>This is an attribution error, and it is modelled on purpose (milestone 021, ruling 6).</b>
/// Whether a shakedown works turns on the mark's resistance, the method chosen, the owner's own
/// Persuasion and a roll — the executor's Coercion is one input among several and on the persuade
/// path it is not an input at all. A delegator who concludes anything about his man from the
/// takings arriving is reasoning from confounded evidence. He does it anyway, because people do,
/// and because a belief that can only ever become more accurate is not a belief worth modelling.
/// The milestone's own proof obligation follows from that: the assessment must be able to end up
/// <em>further</em> from the truth than it started.
///
/// <b>What it deliberately does not do.</b> It revises positions the delegator already holds; it
/// never invents one. A boss who has never formed a view about whether a man is any good does not
/// acquire one because a job went well — that would be a different and much stronger claim, and
/// <see cref="Cognition.Revise"/>'s own contract (it returns null when there is no such record)
/// makes the boundary structural rather than remembered.
/// </summary>
public static class Suitability
{
    /// <summary>
    /// PROVISIONAL TUNING, not a derived figure. How far one delegated outcome moves the
    /// delegator's confidence in what he already believes about the man.
    ///
    /// Chosen once, at the same order of magnitude as the other provisional social constants
    /// (<see cref="Relations.ConflictTrustCost"/>, <see cref="Relations.AccountAgreementTrustGain"/>)
    /// and smaller, because one job is weaker evidence about a man than being contradicted to your
    /// face is about a speaker. Not tuned toward any run's outcome.
    /// </summary>
    public const double OutcomeConfidenceShift = 0.10;

    /// <summary>
    /// Applies the outcome of a delegated operation to what <paramref name="owner"/> believes about
    /// the man he sent.
    ///
    /// <b>Direction is relative to what he already thinks, not to the outcome alone.</b> A success
    /// makes him surer of a bar he holds and less sure of one he has rejected; a failure does the
    /// reverse. Reading it as "success raises confidence" would have a boss who thinks his man is no
    /// hard man grow *more* certain of that every time the man succeeds, which is not doubt, it is a
    /// counter.
    ///
    /// Silent when the owner executed the work himself: this is a judgement about somebody else.
    /// </summary>
    public static void RecordDelegatedOutcome(
        Character owner, string executorId, bool succeeded, DateTime at)
    {
        if (owner.Id == executorId) return;

        foreach (string bar in CapabilityBar.Ladder)
        {
            var claim = CapabilityBar.About(executorId, bar);

            InformationRecord? prior = null;
            foreach (var r in owner.Cognition.Records)
                if (r.Claim.Equals(claim)) { prior = r; break; }

            if (prior is null) continue;

            double direction = succeeded == prior.IsHeld ? 1.0 : -1.0;

            // Revise clamps to [0,1] and refuses anything that is not this holder's own reading,
            // which is why the seeded belief is SourceKind.Inference sourced to the holder. A
            // capability belief somebody was *told* is not revisable by watching a job go well, and
            // that restriction is inherited rather than restated here.
            owner.Cognition.Revise(
                claim, prior.Confidence + direction * OutcomeConfidenceShift, owner.Id, at);
        }
    }
}
