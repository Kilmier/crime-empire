namespace CrimeSim.Session;

using CrimeSim.Domain;

/// <summary>
/// Claims, provenance, confidence and standing rendered in fiction.
///
/// <b>Why this is in the simulation library and not in a renderer.</b> Every function here enforces
/// an information-safety rule rather than a layout preference. Discovery says "came across" and
/// never "saw", because finding a wrecked shopfront the next morning is not witnessing a beating.
/// Confidence is words and never a number, because an exact probability is hidden state wearing a
/// percentage sign. Standing says how far he would go on a man's word and never why, because a
/// relationship that cooled because an account did not match must read identically to one that was
/// never warm. Those are properties of the information model. `AGENTS.md`'s repository boundary
/// keeps *console formatting* out of this library — padding, box drawing, section headers — and all
/// of that stays in `IntelligenceWriter`.
///
/// It lives here specifically so there is one implementation. Milestone 009 added a second surface
/// that shows a character what he knows; two copies of these rules is the shape this project's
/// recurring-failure list calls a distinction drawn in one place and dropped on the way to the next.
///
/// The wording was moved here verbatim from `CrimeEmpire.Runner`'s `IntelligenceWriter`, which now
/// delegates to it, so the accepted no-leak tests keep pinning the same strings.
/// </summary>
public static class PlayerNarration
{
    /// <summary>
    /// The claim as a sentence. In fiction, never as a predicate.
    ///
    /// Public so a no-leak test can compute the exact wording a given claim would produce and assert
    /// its absence from a view. A test that hardcoded the prose would pass while this drifted.
    /// </summary>
    public static string Describe(Claim c, Func<string, string> name) => c.Kind switch
    {
        ClaimKind.BusinessRefusesTribute => $"{name(c.Subject)} is holding back what it owes",
        ClaimKind.PersonUsedViolence => $"{name(c.Subject)} put hands on {name(c.Object)}",
        ClaimKind.PoliceInvestigating => $"the police are looking at {name(c.Subject)}",
        ClaimKind.PersonHoldsGrievance => $"{name(c.Subject)} carries something against {name(c.Object)}",
        ClaimKind.TributeCollected => $"{name(c.Subject)} has paid",
        ClaimKind.WitnessSawIncident => $"somebody on the street saw {name(c.Object)} at {name(c.Subject)}",
        ClaimKind.PolicyIssued => $"the rule \"{c.Object}\" stands",
        ClaimKind.PersonBreachedPolicy => $"{name(c.Subject)} went outside \"{c.Object}\"",
        ClaimKind.TargetIsVulnerable => $"{name(c.Subject)} would not stand up to pressure",
        // Subject is a domain, never a person or a business — name() must not be called on it, or a
        // display-name lookup would either resolve nothing or, worse, resolve something by accident.
        ClaimKind.UnattributedShortfall => $"something in the {c.Subject} still is not paying what it owes",
        // Milestone 021 added this claim kind and did not add it here, so it fell through to the
        // fallback below and reached players as a raw `PersonIsCapable(angelo -> hard-man)` dump —
        // ids and all, on a surface whose entire design exists to keep them out. Found while
        // scoping milestone 024. The Object is a CapabilityBar token, not a name, so it is matched
        // rather than passed to name().
        ClaimKind.PersonIsCapable => c.Object == CapabilityBar.HardMan
            ? $"{name(c.Subject)} is a hard man"
            : $"{name(c.Subject)} is up to leaning on somebody",
        _ => c.ToString(),
    };

    /// <summary>
    /// How far he would go on this person's word, in words.
    ///
    /// Qualitative for the same reason confidence is: the number is hidden state, and a percentage
    /// would let the player read the model instead of the man.
    ///
    /// <b>Nothing in this phrase explains why he stands where he does, and that part still holds.</b>
    /// What no longer holds is the conclusion this comment used to draw from it — that a relationship
    /// which cooled because an account did not match *should* read identically to one that was never
    /// warm, leaving the difference for the player to reconstruct from the claim log. Matt reversed
    /// that on 2026-09-04: defensible for a developer reading a trace, wrong for somebody playing a
    /// game. Milestone 023 put the cause on the roster beside this phrase, as
    /// <see cref="WhyStandingMoved"/>, dated and attached to the man. The phrase itself is unchanged
    /// and still leaks neither the cause nor a number, which is what the original reasoning was
    /// actually protecting.
    /// </summary>
    public static string Standing(double trust, Pronouns self, Pronouns other) => trust switch
    {
        >= 0.60 => $"{self.Subject} would take {other.Possessive} word",
        >= 0.35 => $"{self.Subject} {self.Verb("takes", "take")} {other.Object} as {self.Subject} " +
                   $"{self.Verb("finds", "find")} {other.Object}",
        >= 0.15 => $"{self.Subject} {self.Verb("has", "have")} {self.Possessive} reservations about {other.Object}",
        > 0 => $"{self.Subject} would not take {other.Possessive} word for much",
        // Not "anything {other.Subject} says": two people in one sentence and both of them "he" is
        // a sentence the reader cannot parse. The other stays out of subject position here, which
        // also keeps this parallel with the band above it.
        _ => $"{self.Subject} would not take {other.Possessive} word at all",
    };

    /// <summary>
    /// How afraid of somebody he is, or null when it is not worth saying.
    ///
    /// Never an accusation and never a number, exactly as <see cref="Standing"/>.
    /// </summary>
    public static string? Wariness(double fear, Pronouns self, Pronouns other)
        => fear <= 0.25
            ? null
            : $"{self.Subject} {self.Verb("is", "are")} {(fear > 0.6 ? "frightened" : "wary")} of {other.Object}";

    /// <summary>
    /// Trust moving toward somebody, in words. No number, exactly as <see cref="Standing"/> and
    /// <see cref="Wariness"/> — the strength of the movement is hidden state, only its direction is
    /// said. Added by milestone 018 for <c>PlayerRelationshipMovement</c>.
    /// </summary>
    public static string Movement(bool warmed, Pronouns self, string otherName)
        => $"{self.Possessive} trust in {otherName} {(warmed ? "grew" : "cooled")}";

    /// <summary>
    /// Why a standing moved, in words — milestone 023, and the half
    /// <see cref="Standing"/> deliberately withholds.
    ///
    /// Built from <see cref="StandingCause"/> rather than from a string written where the movement
    /// happened, for the settled reason that no simulation-authored string crosses this boundary:
    /// `Relations` has only ids and claims to hand, so a sentence composed there would either leak an
    /// id or force the domain to know about names. The same rule that makes `PlayerOption` build its
    /// wording from typed fields.
    ///
    /// Says what happened and never how much, exactly as the standing phrase and
    /// <see cref="Movement"/> do — and never asserts the speaker was lying, because the character
    /// cannot know that. Being contradicted is a fact about the exchange; who was right is not.
    /// </summary>
    public static string WhyStandingMoved(
        StandingCause cause, Pronouns self, string otherName, string? about = null)
    {
        // What it was about, when there is a proposition in it. Without this the roster rendered
        // three genuinely different corroborations on one day as the same sentence three times, and
        // a history that cannot tell its own entries apart reads as a bug even when the state behind
        // it is correct. Found by reading the output, not by a test.
        // Colon rather than "about", because the claim descriptions are whole clauses — "somebody on
        // the street saw Angelo Conti at Bellini's grocery" — and "…what he already believed about
        // somebody on the street saw…" is not a sentence.
        string on = about is null ? "" : $": {about}";

        return cause switch
        {
            // Deliberately not the word "contradicted": that is a *confidence label*, replacing the
            // usual one on a belief he still holds, and `InformationTransmissionTests` pins its
            // appearance to exactly that case. Reusing it here made the word show up on a roster
            // line and broke a biconditional that was correctly asserting something else.
            StandingCause.AccountContradicted =>
                $"{otherName} told {self.Object} otherwise{on}",
            StandingCause.AccountCorroborated =>
                $"{otherName} backed {self.Object} up{on}",
            _ => $"{otherName} put the frighteners on {self.Object}",
        };
    }

    /// <summary>Which way a remembered cause moves the standing. Derived, never stored twice.</summary>
    public static bool Warmed(StandingCause cause) => cause == StandingCause.AccountCorroborated;

    /// <summary>
    /// How far he has got with work he is doing himself, in words — milestone 024.
    ///
    /// <b>Only ever called for his own work.</b> The step somebody else has reached is that man's
    /// state, not his; see <see cref="PlayerOperation.Progress"/>. This function has no way to tell
    /// the difference, so the caller carries that rule and a test pins it.
    ///
    /// Named, never indexed. `StepIndex` is 0-3 and the steps are already written in plain language
    /// in `Strategies` — "make the approach", "put the demand" — so this reports the last one he
    /// actually completed rather than inventing a second vocabulary for the same four things. Empty
    /// attempts are said in words for the same reason a standing is: a count is a measurement, and
    /// the player is being told what happened, not shown the model's arithmetic.
    /// </summary>
    public static string OwnProgress(string? lastStepDone, int failedAttempts, Pronouns self)
    {
        string where = lastStepDone is null
            ? $"{self.Subject} {self.Verb("has", "have")} not started in earnest yet"
            : $"{self.Subject} {self.Verb("has", "have")} {Past(lastStepDone)}";

        return failedAttempts switch
        {
            0 => where,
            1 => $"{where}, and once it came back empty",
            2 => $"{where}, and twice it came back empty",
            _ => $"{where}, and it keeps coming back empty",
        };
    }

    /// <summary>
    /// The step names are written as instructions — "make the approach" — and a progress line needs
    /// them as things already done. Kept as a small table rather than a general conjugator, because
    /// there are seven of them in the whole game and a general one would be wrong more often.
    /// </summary>
    private static string Past(string step) => step switch
    {
        "make the approach" => "made the approach",
        "put the demand" => "put the demand",
        "press or accept" => "pressed the point",
        "collect" => "collected",
        "quiet the witnesses" => "seen to the witnesses",
        "tidy the paperwork" => "tidied the paperwork",
        "check the records" => "been through the records",
        "canvass the street" => "canvassed the street",
        "put on surveillance" => "put somebody on watch",
        _ => "made a start",
    };

    /// <summary>
    /// What he takes this man to be good for, or null when he has never formed a view.
    ///
    /// <b>A belief, and it reads like one.</b> This is not an attitude toward the man — that is
    /// <see cref="Standing"/> — it is what he thinks is true about him, held on the
    /// <see cref="CapabilityBar"/> ladder in his own cognition, and it can be flatly wrong. The
    /// wording says "takes him for" rather than "is" for exactly that reason: the roster is reporting
    /// his opinion, not the man.
    ///
    /// Both bars null means no opinion, and the line is omitted rather than rendered as an absence —
    /// "he has no view on whether Vincent is any good" is a sentence about the model, not about the
    /// world. That is the same null-versus-zero distinction the ladder itself is built on: having no
    /// view and having a poor view are different states and must not collapse into one phrase.
    /// </summary>
    public static string? TakenFor(bool? roughWork, bool? hardMan, Pronouns self, Pronouns other)
        => (roughWork, hardMan) switch
        {
            (null, null) => null,
            (_, true) => $"{self.Subject} {self.Verb("takes", "take")} {other.Object} for a hard man",
            (true, _) => $"{self.Subject} {self.Verb("reckons", "reckon")} {other.Subject} is up to " +
                         "leaning on somebody",
            (false, _) => $"{self.Subject} would not send {other.Object} to lean on anybody",
            // No view on the low bar but a settled one that he is no hard man — an odd shape, and
            // reported as what it is rather than smoothed into either neighbour.
            (null, false) => $"{self.Subject} {self.Verb("does", "do")} not take {other.Object} " +
                             "for a hard man",
        };

    /// <summary>
    /// Qualitative confidence only. INFORMATION_AND_LEGIBILITY.md lists the vocabulary; the numeric
    /// confidence behind it is hidden state and stays hidden. A contradicted account says so
    /// instead, because how sure he was stopped being the interesting fact about it.
    /// </summary>
    public static string Qualify(InformationRecord r, bool contested)
        => contested ? "contradicted" : r.ConfidenceLabel;

    /// <summary>
    /// Where it came from, as the player is entitled to see it. "Vincent says an associate heard"
    /// is meaningfully different from having watched it happen, and the difference has to survive
    /// into the sentence.
    ///
    /// The rule this enforces: none of these may claim presence, sight, or participation that the
    /// acquisition category does not carry. Discovery in particular says "came across", never
    /// "saw" — finding a wrecked shopfront the next morning is not witnessing a beating.
    /// </summary>
    public static string Attribute(InformationRecord r, Func<string, string> name, Pronouns self)
        => r.SourceKind switch
        {
            // Deliberately not "he saw it". Vincent holds that he went outside his boss's rule
            // because he decided to, which is not a thing anybody watches happen.
            SourceKind.Participant => $"{self.Subject} had a hand in it {self.Reflexive}",
            SourceKind.Witness => $"{self.Subject} saw it {self.Reflexive}",
            SourceKind.Discovery => $"{self.Subject} came across it",
            SourceKind.FirstHandTestimony => $"{name(r.SourceId)} was in it and told {self.Object} so",
            SourceKind.Report => $"{name(r.SourceId)} told {self.Object}",
            SourceKind.Inference => $"{self.Subject} worked it out {self.Reflexive}",
            // Names a place, never a man — that is what makes it a rumour rather than an account,
            // and it is why this arm cannot borrow the "X told him" shape above. Milestone 022 made
            // this explicit rather than the fallback it had been since milestone 003, because it is
            // now reachable: something finally produces a rumour.
            SourceKind.Rumor => $"it is going round {name(r.SourceId)}",
            _ => $"talk, no better sourced than {name(r.SourceId)}",
        };

    /// <summary>
    /// What he has on his own account, for a claim his sources disagree about — or null when the
    /// only positions he holds came from other people, in which case they are already listed under
    /// the names of the men who gave them.
    ///
    /// Only what he came to on his own account counts as a separate voice here. Testing for "not a
    /// report" would file somebody else's first-hand account as his own.
    /// </summary>
    public static string? OwnBasis(InformationRecord r, Pronouns self)
        => r.SourceKind switch
        {
            SourceKind.Participant => $"{self.Possessive} own doing",
            SourceKind.Witness => $"{self.Possessive} own eyes",
            SourceKind.Discovery => $"what {self.Subject} came across",
            SourceKind.Inference => $"what {self.Subject} worked out",
            _ => null,
        };
}
