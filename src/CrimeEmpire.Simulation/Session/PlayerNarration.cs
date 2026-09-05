namespace CrimeSim.Session;

using CrimeSim.Domain;

/// <summary>
/// Claims, provenance, confidence and standing rendered in fiction.
///
/// <b>Why this is in the simulation library and not in a renderer.</b> Every function here enforces
/// an information-safety rule rather than a layout preference. Discovery says "found out for
/// yourself" and never "saw", because finding a wrecked shopfront the next morning is not witnessing
/// a beating. Confidence is words and never a number, because an exact probability is
/// hidden state wearing a percentage sign. Standing says how far he would go on a man's word and
/// never how far the number moved. Those are properties of the information model. `AGENTS.md`'s
/// repository boundary keeps *console formatting* out of this library — padding, box drawing,
/// section headers — and all of that stays in `IntelligenceWriter`.
///
/// It lives here specifically so there is one implementation. Milestone 009 added a second surface
/// that shows a character what he knows; two copies of these rules is the shape this project's
/// recurring-failure list calls a distinction drawn in one place and dropped on the way to the next.
///
/// <b>Milestone 025 rewrote every phrase here into plain English</b>, on Matt's finding that the
/// screen read as jargon — "holding back what it owes", "the rule 'no-violence-harbour' stands",
/// "beyond doubt for him, he had a hand in it himself". Each phrase now says what the model holds in
/// the words a person would use for it, and the rules above are unchanged by the rewording: no
/// number, no sight where there was none, no accusation the character cannot make.
///
/// <b>Voice.</b> Every phrase takes the pronouns of the person it is about, and
/// <see cref="PlayerView.You"/> is a pronoun set like any other. When the viewpoint is the character
/// the player controls, the session speaks in the second person — "you would take his word" — and
/// when it is somebody being watched, or the console, in the third. The same functions produce both;
/// nothing here knows which it is producing except <see cref="Describe"/>, which needs to know
/// whether the man in a claim is the reader.
/// </summary>
public static class PlayerNarration
{
    /// <summary>
    /// The claim as a sentence. In fiction, never as a predicate.
    ///
    /// Public so a no-leak test can compute the exact wording a given claim would produce and assert
    /// its absence from a view. A test that hardcoded the prose would pass while this drifted.
    ///
    /// <paramref name="name"/> resolves every id that can appear in a claim: a person, a business,
    /// or — since milestone 025 — a policy, which resolves to the scenario's own description of the
    /// rule rather than to its id. "the rule 'no-violence-harbour'" was a developer id in the
    /// player's hands, and the description is prose a person wrote and the character was told, the
    /// same footing as an assignment's objective.
    /// </summary>
    public static string Describe(Claim c, Func<string, string> name)
        => Describe(c, name, selfId: null, self: null);

    /// <summary>
    /// The same sentence, with the reader referred to as themselves when the claim is about them.
    ///
    /// "Vincent Russo broke the rule" is a strange thing to read about yourself. When
    /// <paramref name="selfId"/> is the claim's subject or object and the voice is the second person,
    /// that position reads "you", with the verb agreeing. In the third person the name is kept, so
    /// a watched character's view never has two men both called "he" in one sentence.
    /// </summary>
    public static string Describe(Claim c, Func<string, string> name, string? selfId, Pronouns? self)
    {
        bool second = self is not null && selfId is not null && PlayerView.IsSecondPerson(self);
        bool subjectIsSelf = second && c.Subject == selfId;
        bool objectIsSelf = second && c.Object == selfId;

        string Subject() => subjectIsSelf ? self!.Subject : name(c.Subject);
        string Object() => objectIsSelf ? self!.Object : name(c.Object);
        // Agreement for the two claim kinds whose subject is a person and whose verb is present
        // tense. Everything else is past tense or has a business for a subject.
        string V(string singular, string plural) => subjectIsSelf ? self!.Verb(singular, plural) : singular;

        return c.Kind switch
        {
            ClaimKind.BusinessRefusesTribute => $"{Subject()} is not paying its tribute",
            ClaimKind.PersonUsedViolence => $"{Subject()} got violent at {Object()}",
            ClaimKind.PoliceInvestigating => $"the police are investigating {Subject()}",
            ClaimKind.PersonHoldsGrievance => $"{Subject()} {V("has", "have")} a grudge against {Object()}",
            ClaimKind.TributeCollected => $"{Subject()} has paid its tribute",
            ClaimKind.WitnessSawIncident => $"somebody on the street saw {Object()} at {Subject()}",
            // Subject is the organisation and Object the policy id; neither is a person. The rule's
            // text comes from name(), which resolves a policy id to its description.
            ClaimKind.PolicyIssued => $"the outfit's rule: {name(c.Object)}",
            ClaimKind.PersonBreachedPolicy => $"{Subject()} broke the rule: {name(c.Object)}",
            ClaimKind.TargetIsVulnerable => $"{Subject()} would fold if leaned on",
            // Subject is a domain, never a person or a business — name() must not be called on it, or a
            // display-name lookup would either resolve nothing or, worse, resolve something by accident.
            ClaimKind.UnattributedShortfall => $"somebody in the {c.Subject} still is not paying",
            // The Object is a CapabilityBar token, not a name, so it is matched rather than passed to
            // name(). Milestone 021 added this kind without adding it here, and it reached players as
            // a raw `PersonIsCapable(angelo -> hard-man)` dump; found while scoping milestone 024.
            ClaimKind.PersonIsCapable => c.Object == CapabilityBar.HardMan
                ? $"{Subject()} {V("is", "are")} a hard man"
                : $"{Subject()} can handle leaning on somebody",
            _ => c.ToString(),
        };
    }

    /// <summary>
    /// The claim denied, as a sentence — what a man says when he lies about it. Only the two kinds a
    /// man can be asked about himself have a shape of their own; anything else falls back to "it is
    /// not true that…", which is always grammatical and never wrong.
    /// </summary>
    public static string Deny(Claim c, Func<string, string> name, string? selfId, Pronouns? self)
    {
        bool second = self is not null && selfId is not null && PlayerView.IsSecondPerson(self);
        string subject = second && c.Subject == selfId ? self!.Subject : name(c.Subject);
        string obj = second && c.Object == selfId ? self!.Object : name(c.Object);

        return c.Kind switch
        {
            ClaimKind.PersonUsedViolence => $"{subject} did not get violent at {obj}",
            ClaimKind.PersonBreachedPolicy => $"{subject} did not break the rule: {name(c.Object)}",
            _ => $"it is not true that {Describe(c, name, selfId, self)}",
        };
    }

    /// <summary>
    /// How far he would go on this person's word, in words.
    ///
    /// Qualitative for the same reason confidence is: the number is hidden state, and a percentage
    /// would let the player read the model instead of the man.
    ///
    /// <b>Nothing in this phrase explains why he stands where he does, and that part still holds.</b>
    /// Milestone 023 put the cause on the roster beside this phrase, as
    /// <see cref="WhyStandingMoved"/>, dated and attached to the man; the phrase itself still leaks
    /// neither the cause nor a number.
    /// </summary>
    public static string Standing(double trust, Pronouns self, Pronouns other) => trust switch
    {
        >= 0.60 => $"{self.Subject} would take {other.Possessive} word",
        >= 0.35 => $"{self.Subject} would take {other.Possessive} word within reason",
        >= 0.15 => $"{self.Subject} {self.Verb("has", "have")} {self.Possessive} doubts about {other.Object}",
        > 0 => $"{self.Subject} would not take {other.Possessive} word for much",
        // Not "believe a word he says": a test guards against "lie" appearing in a standing phrase,
        // and "believe" contains it.
        _ => $"{self.Subject} would not take {other.Possessive} word on anything",
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
    /// Why a standing moved, in words — milestone 023, and the half
    /// <see cref="Standing"/> deliberately withholds.
    ///
    /// Built from <see cref="StandingCause"/> rather than from a string written where the movement
    /// happened, for the settled reason that no simulation-authored string crosses this boundary:
    /// `Relations` has only ids and claims to hand, so a sentence composed there would either leak an
    /// id or force the domain to know about names. The same rule that makes `PlayerOption` build its
    /// wording from typed fields.
    ///
    /// Says what happened and never how much, exactly as the standing phrase does — and never
    /// asserts the speaker was lying, because the character cannot know that. Being told a different
    /// story is a fact about the exchange; who was right is not.
    /// </summary>
    public static string WhyStandingMoved(
        StandingCause cause, Pronouns self, string otherName, string? about = null)
    {
        // What it was about, when there is a proposition in it. Without this the roster rendered
        // three genuinely different corroborations on one day as the same sentence three times, and
        // a history that cannot tell its own entries apart reads as a bug even when the state behind
        // it is correct. Found by reading the output, not by a test.
        // A dash rather than "about", because the claim descriptions are whole clauses — and rather
        // than a colon, because a rule's description already carries one.
        string on = about is null ? "" : $" — {about}";

        return cause switch
        {
            StandingCause.AccountContradicted =>
                $"{otherName} told {self.Object} a different story{on}",
            StandingCause.AccountCorroborated =>
                $"{otherName} confirmed what {self.Subject} had heard{on}",
            _ => $"{otherName} scared {self.Object}",
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
    /// in `Strategies`, so this reports the last one he actually completed rather than inventing a
    /// second vocabulary for the same four things. Empty attempts are said in words for the same
    /// reason a standing is: a count is a measurement, and the player is being told what happened,
    /// not shown the model's arithmetic.
    /// </summary>
    public static string OwnProgress(string? lastStepDone, int failedAttempts, Pronouns self)
    {
        string where = lastStepDone is null
            ? $"{self.Subject} {self.Verb("has", "have")} not properly started yet"
            : $"{self.Subject} {self.Verb("has", "have")} {Past(lastStepDone, self)}";

        // A failed attempt is the target holding out — `Strategies.Blocked` records it as
        // "tribute-refused" — so it is said as a refusal. "Went nowhere" read as if nothing had
        // happened at all; Matt's playtest finding.
        return failedAttempts switch
        {
            0 => where,
            1 => $"{where}, and been refused once",
            2 => $"{where}, and been refused twice",
            _ => $"{where}, and been refused again and again",
        };
    }

    /// <summary>
    /// The step names are written as instructions — "make the approach" — and a progress line needs
    /// them as things already done. Kept as a small table rather than a general conjugator, because
    /// there are nine of them in the whole game and a general one would be wrong more often.
    /// </summary>
    private static string Past(string step, Pronouns self) => step switch
    {
        "make the approach" => "made the first approach",
        "put the demand" => $"made {self.Possessive} demand",
        "press or accept" => "pushed harder",
        "collect" => "collected",
        "quiet the witnesses" => "seen to the witnesses",
        "tidy the paperwork" => "tidied the paperwork",
        "check the records" => "been through the records",
        "canvass the street" => "asked around on the street",
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
            (true, _) => $"{self.Subject} {self.Verb("reckons", "reckon")} {other.Subject} can lean on people",
            (false, _) => $"{self.Subject} would not send {other.Object} to lean on anybody",
            // No view on the low bar but a settled one that he is no hard man — an odd shape, and
            // reported as what it is rather than smoothed into either neighbour.
            (null, false) => $"{self.Subject} {self.Verb("does", "do")} not take {other.Object} " +
                             "for a hard man",
        };

    /// <summary>
    /// How sure he is, in words, or null when saying so would be redundant.
    ///
    /// Qualitative only — INFORMATION_AND_LEGIBILITY.md asks for language of this kind and the
    /// numeric confidence behind it stays hidden. The bands are the same five
    /// <see cref="InformationRecord.ConfidenceLabel"/> draws, and a test walks the whole range to
    /// pin that they change at exactly the same points; the label itself is developer vocabulary
    /// that reaches the trace, which is why it is not simply reused here.
    ///
    /// <b>Null for what he saw or did himself.</b> "You saw it yourself; you are certain of it" says
    /// the same thing twice, and the certainty on a witnessed fact is not a separate piece of
    /// information the player is owed. Matt's finding, milestone 025.
    ///
    /// <b>"disputed" replaces the band when somebody has told him otherwise</b>, whatever he
    /// concluded — how sure he was stopped being the interesting fact about it, and the accounts are
    /// listed beside the claim so the reader can see who said what.
    /// </summary>
    public static string? Certainty(InformationRecord r, bool contested, Pronouns self)
    {
        if (contested) return "disputed";
        if (r.SourceKind is SourceKind.Witness or SourceKind.Participant) return null;

        return r.Confidence switch
        {
            >= 0.9 => $"{self.Subject} {self.Verb("is", "are")} certain of it",
            >= 0.7 => $"{self.Subject} {self.Verb("is", "are")} fairly sure of it",
            >= 0.5 => "probably true",
            >= 0.3 => $"{self.Subject} {self.Verb("is", "are")} not sure of it",
            _ => $"{self.Subject} cannot vouch for it",
        };
    }

    /// <summary>
    /// Where it came from, as the player is entitled to see it. "Vincent says an associate heard"
    /// is meaningfully different from having watched it happen, and the difference has to survive
    /// into the sentence.
    ///
    /// The rule this enforces: none of these may claim presence, sight, or participation that the
    /// acquisition category does not carry. Discovery in particular says "found out for yourself",
    /// never "saw" — finding a wrecked shopfront the next morning is not witnessing a beating — and
    /// says nothing about *what* was found, because the same category covers the wreckage he came
    /// across and the weakness he sized up on an approach.
    /// </summary>
    public static string Attribute(InformationRecord r, Func<string, string> name, Pronouns self)
        => Attribute(r, name, self, isPerson: _ => true, selfId: null)!;

    /// <summary>
    /// <see cref="Attribute(InformationRecord, Func{string, string}, Pronouns)"/>, with two things
    /// only the projection can tell it.
    ///
    /// <paramref name="isPerson"/>: a report's source is usually a man who told him, but the
    /// scenario seeds one belief from "the books" — a ledger, not a person — and "the books told him"
    /// read as though a Mr. Books had. A source that is not a person "says so" instead. (The verb is
    /// plural because the only such source is plural; a singular one would need its own arm.)
    ///
    /// <paramref name="selfId"/>: null result when the claim is his own act and he is its author —
    /// "you broke the rule; you had a hand in it yourself" is redundant, and the sentence already says
    /// who did it. A participant in somebody else's act keeps the line: he ordered it, or was in on
    /// it, and that is information.
    /// </summary>
    public static string? Attribute(
        InformationRecord r, Func<string, string> name, Pronouns self, Func<string, bool> isPerson, string? selfId)
        => r.SourceKind switch
        {
            // The boss holds his own rule as its author. "Had a hand in it" is true and reads oddly
            // for a rule, so it gets its own words.
            SourceKind.Participant when r.Claim.Kind == ClaimKind.PolicyIssued =>
                $"{self.Subject} set it {self.Reflexive}",
            SourceKind.Participant when selfId is not null && r.Claim.Subject == selfId => null,
            // Deliberately not "he saw it". Vincent holds that he went outside his boss's rule
            // because he decided to, which is not a thing anybody watches happen.
            SourceKind.Participant => $"{self.Subject} had a hand in it {self.Reflexive}",
            SourceKind.Witness => $"{self.Subject} saw it {self.Reflexive}",
            SourceKind.Discovery => $"{self.Subject} found out for {self.Reflexive}",
            // A rule is not something a man is "in on": the boss who issued it told him in person.
            SourceKind.FirstHandTestimony when r.Claim.Kind == ClaimKind.PolicyIssued =>
                $"{name(r.SourceId)} told {self.Object} in person",
            SourceKind.FirstHandTestimony => $"{name(r.SourceId)} was in on it and told {self.Object}",
            SourceKind.Report => isPerson(r.SourceId)
                ? $"{name(r.SourceId)} told {self.Object}"
                : $"{name(r.SourceId)} say so",
            SourceKind.Inference => $"{self.Subject} worked it out {self.Reflexive}",
            // Names a place, never a man — that is what makes it a rumour rather than an account,
            // and it is why this arm cannot borrow the "X told him" shape above.
            SourceKind.Rumor => $"it is going round {name(r.SourceId)}",
            _ => $"just talk, from {name(r.SourceId)}",
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
            SourceKind.Discovery => $"what {self.Subject} found out",
            SourceKind.Inference => $"what {self.Subject} worked out",
            _ => null,
        };
}
