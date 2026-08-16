namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Sim;

/// <summary>
/// Which relationship state a score component was derived from.
///
/// <b>Why this exists rather than grouping by component name.</b> Before milestone 008 the only way
/// to ask "how much of this score came from a relationship" was to filter components whose Name was
/// "relationship effects". Measured across the five variants at seed 42, 61 of the 168 components
/// carrying that name — 36% — read no relationship state at all: they are
/// <see cref="ActionKind.SeekCorroboration"/>'s "going behind X", which is <c>-0.45 * proud</c>, a
/// pure trait term wearing a relationship label. Two production tests and the whole planned
/// diagnostic were about to aggregate on that name. A label is not a derivation, and this is the
/// ledger's "two different things treated as one" pattern found inside the instrument built to
/// measure it.
///
/// Set at the point the value is computed, never inferred afterwards.
///
/// <see cref="Belonging"/> is a drive, not a relationship dimension. It is enumerated here because
/// it is one of the three inputs to <see cref="LoyaltyReading"/> and milestone 008's ruling 3
/// requires trust, obligation, Belonging and grievance to stay separately inspectable through the
/// scoring path. It is deliberately excluded from <see cref="Relational"/>: a man with no
/// relationships still has a need to belong.
/// </summary>
[Flags]
public enum RelationshipFacet
{
    None = 0,
    Trust = 1,
    Obligation = 2,
    Belonging = 4,
    Grievance = 8,
    Fear = 16,

    /// <summary>The facets that are actually relationship state, for counterfactual purposes.</summary>
    Relational = Trust | Obligation | Grievance | Fear,
}

/// <summary>
/// One reason a candidate scored as it did.
///
/// <paramref name="WithoutRelationship"/> is what this component would have been worth had the actor
/// held no relationship with anybody — trust, obligation, fear and grievance all zero, psychology
/// untouched. It is recorded at emission because that is the only place the decomposition is still
/// available; reconstructing it afterwards from the total would require re-deriving the clamp.
/// Null means the component reads no relationship state, so the two values are the same.
/// </summary>
public sealed record ScoreComponent(
    string Name,
    double Value,
    string Explanation,
    RelationshipFacet Reads = RelationshipFacet.None,
    double? WithoutRelationship = null)
{
    /// <summary>What this component would be worth to a man with no relationships.</summary>
    public double RelationshipFreeValue => WithoutRelationship ?? Value;

    /// <summary>How much of this component's value is owed to relationship state.</summary>
    public double RelationshipShare => Value - RelationshipFreeValue;
}

public sealed record ScoreBreakdown(
    Candidate Candidate,
    double Total,
    double Noise,
    IReadOnlyList<ScoreComponent> Components)
{
    public IEnumerable<ScoreComponent> Significant()
        => Components.Where(c => Math.Abs(c.Value) >= 0.15)
                     .OrderByDescending(c => Math.Abs(c.Value));

    // ------------------------------------------------------------ relationship diagnostics
    //
    // Developer-facing. None of this reaches IntelligenceWriter or any player-facing surface — a
    // player who could read the exact contribution of his capo's trust would be reading the utility
    // calculation INFORMATION_AND_LEGIBILITY.md exists to keep out of his hands.
    //
    // Deliberately NOT filtered through Significant(). The 0.15 cutoff is right for a human-readable
    // reason list and wrong for a measurement: on the very decision milestone 007's finding was taken
    // from, both halves of the report pair (+0.0219 and -0.0156) fall under it, so the rendered trace
    // printed no relationship line at all for a candidate whose net relationship contribution was the
    // number being reported. A cutoff that hides a cancelling pair hides exactly the cancellation.

    /// <summary>
    /// Every component derived from a named facet, in emission order. No cutoff.
    ///
    /// <b>Includes <see cref="RelationshipFacet.Belonging"/>, which is not relationship state.</b>
    /// It is listed because it is the fourth contribution to loyalty and a reader decomposing a
    /// loyalty term needs to see all four — but its <see cref="ScoreComponent.RelationshipShare"/> is
    /// zero, so it contributes nothing to <see cref="RelationshipGross"/>,
    /// <see cref="RelationshipNet"/>, or the counterfactual. Shown as context, never counted as
    /// relational. Sweeping Belonging into a figure reported as relational is precisely the
    /// conflation the facets were introduced to end.
    ///
    /// Components with no facet are excluded, which is what keeps <c>SeekCorroboration</c>'s
    /// "going behind X" — a pure trait term named "relationship effects" — out of the channel.
    /// </summary>
    public IEnumerable<ScoreComponent> RelationshipComponents()
        => Components.Where(c => c.Reads != RelationshipFacet.None);

    /// <summary>
    /// The sum of the absolute gross values of the relationship components.
    ///
    /// Reported alongside the net because the two answer different questions and this milestone
    /// exists because only the net was ever visible. Gross 0.0375 against net 0.0063 says the
    /// considerations are large relative to what survives them; gross 0.0063 would have said the
    /// character barely weighed the relationship at all. Those are different worlds.
    /// </summary>
    public double RelationshipGross()
        => RelationshipComponents().Sum(c => Math.Abs(c.RelationshipShare));

    /// <summary>What relationship state is actually worth to this candidate, after cancellation.</summary>
    public double RelationshipNet()
        => RelationshipComponents().Sum(c => c.RelationshipShare);

    /// <summary>
    /// This candidate's score for a man holding no relationship with anybody.
    ///
    /// Computed by summing each component's relationship-free value rather than by re-scoring, so it
    /// reuses this breakdown's own noise draw and compares like with like. Re-scoring would draw
    /// fresh noise of up to ±0.05 — larger than the effect being measured on the report candidates —
    /// and would also require mutating relationship state, which milestone 006 deliberately made
    /// impossible from outside <see cref="CrimeSim.Domain.Relations"/>.
    /// </summary>
    public double TotalWithoutRelationships()
        => Total - RelationshipNet();
}

/// <summary>
/// PSYCHOLOGY-READING FILE 2 OF 2 (the other is Salience.cs).
///
/// Local utility over an already-bounded candidate set — not a search. Note the signature of
/// <see cref="Score"/>: it receives a PerceivedSituation and never a World. A character therefore
/// cannot score an option using a fact they do not hold, because the objective world is not
/// reachable from here.
/// </summary>
public static class Utility
{
    // Method tables. Perceived effectiveness is what the actor expects; actual resolution lives in
    // the strategies and may disagree, which is the point.
    private static double BaseRisk(CoercionMethod m) => m switch
    {
        CoercionMethod.Persuade => 0.05,
        CoercionMethod.Threaten => 0.25,
        CoercionMethod.Force => 0.55,
        _ => 0.0,
    };

    private static double BaseEffect(CoercionMethod m) => m switch
    {
        CoercionMethod.Persuade => 0.35,
        CoercionMethod.Threaten => 0.60,
        CoercionMethod.Force => 0.85,
        _ => 0.0,
    };

    private static double Exposure(CoercionMethod m) => m switch
    {
        CoercionMethod.Persuade => 0.05,
        CoercionMethod.Threaten => 0.35,
        CoercionMethod.Force => 0.80,
        _ => 0.0,
    };

    /// <summary>
    /// Loyalty is derived, not stored, and is read here as its parts rather than as one number.
    ///
    /// Attachment, obligation, a general need to belong and accumulated grievance pull in different
    /// directions; collapsing them into one saved number would erase exactly the distinctions that
    /// make a betrayal legible. <c>DESIGN_DECISIONS.md</c> says so, and until milestone 008 the
    /// scorer said it and then immediately undid it — the four were summed, clamped, and handed to
    /// every reader as a single scalar with no way back.
    ///
    /// <b>Grievance is outside the clamp, and that is the substantive change.</b> It used to be
    /// subtracted inside <c>Math.Clamp(…, 0, 1)</c>, which meant that once the sum floored at zero
    /// further grievance was free and further trust was worthless — the two most interesting
    /// dimensions in the model saturating silently against each other. It is now carried separately
    /// and applied by each reader at that reader's own coefficient, so the arithmetic is unchanged
    /// wherever the old sum did not clamp, and honest where it did.
    ///
    /// The <c>0.50</c> coefficient is preserved exactly and remains PROVISIONAL TUNING. Milestone
    /// 008 did not tune it and was forbidden to.
    /// </summary>
    public readonly record struct LoyaltyReading(
        double Trust, double Obligation, double Belonging, double Grievance)
    {
        public const double TrustWeight = 0.45;
        public const double ObligationWeight = 0.30;
        public const double BelongingWeight = 0.25;

        /// <summary>
        /// PROVISIONAL TUNING, unchanged in magnitude by milestone 008. What a unit of accumulated
        /// grievance is worth against loyalty, at a reader's own coefficient.
        /// </summary>
        public const double GrievanceWeight = 0.50;

        // The four contributions, kept apart all the way to the score.
        //
        // Milestone 008 originally computed these, summed them, and handed every reader one number
        // tagged `Trust | Obligation | Belonging`. That satisfied the letter of "grievance is its own
        // contribution" and missed the rest of ruling 3: three of the four were separately *computed*
        // and not separately *inspectable*, which is this project's signature defect — a distinction
        // drawn in one place and dropped on the way to the next. Codex found it. Readers now emit one
        // component per contribution.

        /// <summary>What trust in this person is worth toward the bond.</summary>
        public double TrustPart => TrustWeight * Trust;

        /// <summary>What he owes this person, toward the bond.</summary>
        public double ObligationPart => ObligationWeight * Obligation;

        /// <summary>
        /// What his need to belong contributes. A drive, not a relationship dimension — it survives
        /// the relationship counterfactual, because a man with nobody still wants to belong.
        /// </summary>
        public double BelongingPart => BelongingWeight * Belonging;

        /// <summary>What he holds against the man, as a negative offset. Never clamped away.</summary>
        public double GrievancePart => -GrievanceWeight * Grievance;

        /// <summary>
        /// The bond, as the sum of its three parts.
        ///
        /// <b>No clamp, because every input is clamped before it gets here.</b> It used to read
        /// <c>Math.Clamp(…, 0, 1)</c>. The three weights total exactly <c>1.0</c>; <c>Trust</c> and
        /// <c>Obligation</c> are clamped to <c>[0,1]</c> by <see cref="CrimeSim.Domain.Relations"/> on
        /// every write, and <c>Belonging</c> is clamped to the same range by
        /// <see cref="Psychology"/>'s constructor, through which both <c>With</c> overloads pass. The
        /// sum is therefore always in <c>[0,1]</c>.
        ///
        /// <b>That last clause was documentation and not enforcement when this clamp was first
        /// removed.</b> <c>Psychology</c> stated the range on its indexers and checked nothing, so a
        /// caller passing <c>Belonging = 5.0</c> would have been clamped before the removal and not
        /// after it — a real behaviour change hidden behind a prose guarantee. Codex found it; the
        /// constructor now enforces the range that argument depends on.
        ///
        /// It mattered here because a clamp that binds cannot be split: there would be no honest way
        /// to say how much of a clamped total each part contributed. Removing it is what makes
        /// emitting the parts separately exactly equal to emitting the sum, which is why totals,
        /// choices and every downstream figure are preserved. Pinned by
        /// <c>The_parts_sum_to_the_bond_across_the_whole_range</c> and by
        /// <c>Loyalty_parts_stay_in_range_through_the_public_api</c>.
        /// </summary>
        public double Value => TrustPart + ObligationPart + BelongingPart;

        public bool HasGrievance => Grievance > 1e-9;
    }

    /// <summary>Reads the four inputs to loyalty without collapsing them.</summary>
    public static LoyaltyReading Loyalty(CharacterView actor, Psychology psy, string otherId)
    {
        var rel = actor.Social.Toward(otherId);
        return new LoyaltyReading(
            rel.Trust, rel.Obligation, psy[Drive.Belonging], actor.Social.GrievanceAgainst(otherId));
    }

    public static ScoreBreakdown Score(
        Candidate cand,
        CharacterView actor,
        Psychology psy,
        PerceivedSituation perceived,
        Agenda agenda,
        Rng rng)
    {
        var parts = new List<ScoreComponent>();
        double aggressive = psy[Trait.Aggressive];
        double cautious = psy[Trait.Cautious];
        double proud = psy[Trait.Proud];

        void Add(string name, double value, string why,
                 RelationshipFacet reads = RelationshipFacet.None, double? withoutRelationship = null)
        {
            if (Math.Abs(value) <= 1e-9 && (withoutRelationship ?? 0) == 0) return;
            // Fold repeats together — an option that serves status twice should read as one larger
            // reason, not as the same sentence printed twice. Facets union and the relationship-free
            // values sum, so folding cannot silently drop half a derivation.
            int at = parts.FindIndex(p => p.Name == name && p.Explanation == why);
            if (at >= 0)
                parts[at] = parts[at] with
                {
                    Value = parts[at].Value + value,
                    Reads = parts[at].Reads | reads,
                    WithoutRelationship = parts[at].RelationshipFreeValue + (withoutRelationship ?? value),
                };
            else parts.Add(new ScoreComponent(name, value, why, reads, withoutRelationship));
        }

        // Loyalty is read as parts and emitted as parts — one component per contribution, each
        // carrying exactly one facet. Ruling 3 in full: trust, obligation, Belonging and grievance
        // are each separately inspectable through the scoring path, not merely separately computed
        // before being summed.
        //
        // Every explanation must be distinct per consideration as well as per facet. A report
        // candidate passes through two readers — the standing reporting buys, and the cost of the
        // candour selected — and `Add` folds on (name, explanation), so reusing a phrase between them
        // would silently merge two contributions ruling 2 requires to stay apart.
        //
        // Belonging alone survives the counterfactual: it is a drive, and a man with no relationships
        // still has one.
        void AddLoyaltyParts(
            string name, double coefficient, LoyaltyReading loyalty,
            string trustWhy, string obligationWhy, string belongingWhy, string grievanceWhy)
        {
            Add(name, coefficient * loyalty.TrustPart, trustWhy, RelationshipFacet.Trust, 0);
            Add(name, coefficient * loyalty.ObligationPart, obligationWhy,
                RelationshipFacet.Obligation, 0);
            Add(name, coefficient * loyalty.BelongingPart, belongingWhy,
                RelationshipFacet.Belonging, coefficient * loyalty.BelongingPart);
            if (loyalty.HasGrievance)
                Add(name, coefficient * loyalty.GrievancePart, grievanceWhy,
                    RelationshipFacet.Grievance, 0);
        }

        // --- perceived goal progress -------------------------------------------------------
        bool servesAgenda = cand.Domain is not null && cand.Domain == agenda.Domain
                            && cand.Kind is ActionKind.StartStrategy or ActionKind.ContinueStrategy
                                          or ActionKind.AlterStrategy or ActionKind.DelegateStrategy;
        if (servesAgenda)
            Add("perceived goal progress", 1.6 * agenda.Weight, $"it advances {agenda.Description}");
        if (cand.Kind is ActionKind.DoNothing or ActionKind.PostponeStrategy)
            Add("perceived goal progress", -0.5 * agenda.Weight, "it defers the thing that matters");

        // Asking for latitude is a route to the same objective, not a retreat from it. Without
        // this it can never compete with acting, and the only ways past a rule are to obey it or
        // to break it — which is not a choice, it is a funnel.
        if (cand.Kind == ActionKind.SeekApproval)
            Add("perceived goal progress", 0.55 * agenda.Weight,
                "it is a way to get what he was told to get without going outside the rule");

        // --- responsibility / order compliance ---------------------------------------------
        if (agenda.AssignmentId is not null)
        {
            double obligation = actor.Execution.Commitments
                .Where(c => c.Id == $"assignment:{agenda.AssignmentId}")
                .Sum(c => c.Weight);

            if (servesAgenda)
                Add("responsibility compliance", 1.0 + obligation, "it discharges an assignment he accepted");
            else if (cand.Kind is ActionKind.AbandonStrategy)
                Add("responsibility compliance", -1.2 - obligation, "abandoning would leave the assignment unmet");
        }

        // --- relationship effects ------------------------------------------------------------
        switch (cand.Kind)
        {
            case ActionKind.DelegateStrategy when cand.TargetId is not null:
                AddLoyaltyParts("relationship effects", 0.9, Loyalty(actor, psy, cand.TargetId),
                    $"he trusts {cand.TargetId} to carry it out for him",
                    $"what he owes {cand.TargetId} makes handing it over natural",
                    "putting work through his own people is what belonging looks like",
                    $"what he holds against {cand.TargetId} makes him a worse bet");
                break;
            case ActionKind.ReportToSuperior when cand.TargetId is not null:
                AddLoyaltyParts("relationship effects", 0.7, Loyalty(actor, psy, cand.TargetId),
                    "reporting keeps him right with a man he trusts",
                    "reporting keeps him right with a man he owes",
                    "reporting in is part of belonging to the outfit",
                    "what he holds against the man takes the good out of reporting to him");
                break;
            case ActionKind.Retaliate when cand.TargetId is not null:
                Add("relationship effects", 0.8 * actor.Social.GrievanceAgainst(cand.TargetId),
                    "he holds a grievance against the target", RelationshipFacet.Grievance, 0);
                break;
            case ActionKind.SeekApproval when cand.TargetId is not null:
                Add("relationship effects", 0.4 * actor.Social.Toward(cand.TargetId).Obligation,
                    "it defers to his superior", RelationshipFacet.Obligation, 0);
                break;
            case ActionKind.Concede when cand.TargetId is not null:
                Add("relationship effects", 1.6 * actor.Social.Toward(cand.TargetId).Fear,
                    "he is afraid of them", RelationshipFacet.Fear, 0);
                break;
            case ActionKind.Refuse when cand.TargetId is not null:
                Add("relationship effects", -1.6 * actor.Social.Toward(cand.TargetId).Fear,
                    "holding out against them frightens him", RelationshipFacet.Fear, 0);
                break;
        }

        // --- personality and value alignment -------------------------------------------------
        foreach (var (drive, weight) in DriveProfile(cand))
        {
            double v = psy[drive] * weight;
            if (Math.Abs(v) > 1e-9)
                Add("value alignment", v, $"{drive.ToString().ToLowerInvariant()} {(weight > 0 ? "served" : "sacrificed")}");
        }

        // --- expected reward (perceived, not actual) -----------------------------------------
        if (cand.Method is { } method && cand.TargetId is not null)
        {
            double vulnerability = perceived.BelievesVulnerable(cand.TargetId);
            double skill = method == CoercionMethod.Persuade
                ? actor.Capabilities[Skill.Persuasion]
                : actor.Capabilities[Skill.Coercion];

            // Aggression inflates the expected success of force. It does not choose force.
            double believedEffect = BaseEffect(method) * (1 + 0.35 * aggressive * (method == CoercionMethod.Force ? 1 : 0));
            double reward = 2.2 * believedEffect * (0.4 + 0.6 * vulnerability) * (0.5 + 0.5 * skill);
            Add("expected reward", reward,
                $"he rates {method.ToString().ToLowerInvariant()} likely to work on a target he reads as {(vulnerability > 0.55 ? "weak" : "uncertain")}");
        }

        // --- urgency --------------------------------------------------------------------------
        if (agenda.Kind is AgendaKind.RespondToTrigger or AgendaKind.RelievePressure && servesAgenda)
            Add("urgency", 0.8 * agenda.Weight, "the situation would not keep");

        // --- continuation / commitment value ---------------------------------------------------
        int failed = actor.Execution.Strategy?.FailedAttempts ?? 0;

        // Capped deliberately. An uncapped frustration term eventually swamps every trait, so a
        // cautious man and a violent one escalate to the same place given enough failures — which
        // makes personality decorative. Frustration should argue for change, not dictate which
        // change, and past three failures it has made its point.
        const int FrustrationCap = 3;
        int frustration = Math.Min(failed, FrustrationCap);

        if (cand.Kind == ActionKind.ContinueStrategy)
        {
            Add("commitment value", 0.6 + 0.5 * actor.Execution.CommitmentWeight + 0.3 * proud,
                "continuing what he already started costs nothing socially");

            // Commitment gives continuity; it must not survive contrary evidence indefinitely.
            if (frustration > 0)
                Add("demonstrated failure", -0.95 * frustration,
                    $"the same approach has already come back empty {failed} time{(failed == 1 ? "" : "s")}");
        }

        if (cand.Kind == ActionKind.AlterStrategy && frustration > 0)
            Add("demonstrated failure", 0.80 * frustration, "what he has been doing is not working");

        // --- perceived personal risk -----------------------------------------------------------
        if (cand.Kind == ActionKind.Retaliate && cand.TargetId is not null)
        {
            // Moving on someone you are bound to is dangerous in proportion to how bound you are.
            var loyalty = Loyalty(actor, psy, cand.TargetId);
            double nerve = 1 - 0.4 * aggressive;

            // The flat part of the danger is not relational: moving on anybody is a serious step.
            Add("perceived personal risk", -1.3 * nerve,
                "going after him would be a serious step");

            // And the part that depends on how bound to him he is, one component per binding.
            // Grievance reduces the danger rather than adding to it, which is the point: a man with
            // something against you is closer to moving on you.
            AddLoyaltyParts("perceived personal risk", -2.2 * nerve, loyalty,
                "he would be moving on a man he trusts",
                "he would be moving on a man he owes",
                "moving on his own would cost him his place",
                "what he holds against him makes the step feel less serious");
        }

        if (cand.Kind is ActionKind.Concede or ActionKind.Refuse)
        {
            // Pride is what makes backing down expensive for a man with nothing else to defend.
            double shame = cand.Kind == ActionKind.Concede ? -0.9 * proud : 0.7 * proud;
            Add("pride", shame, cand.Kind == ActionKind.Concede
                ? "giving in in front of his own street would shame him"
                : "he does not want to be seen to fold");
        }

        if (cand.Method is { } m2)
        {
            double risk = BaseRisk(m2) * (1 + 1.0 * cautious) * (1 - 0.6 * aggressive) * 3.0;
            Add("perceived personal risk", -risk,
                aggressive > 0.5 && m2 == CoercionMethod.Force
                    ? "his aggression makes escalation feel cheaper than it is"
                    : "he weighs the danger to himself");
        }

        // --- resource and time cost -------------------------------------------------------------
        if (cand.RequiredCrew > 0)
            Add("resource cost", -0.18 * cand.RequiredCrew, $"it ties up {cand.RequiredCrew} of his people");

        // --- legal and information exposure ------------------------------------------------------
        if (cand.Method is { } m3)
        {
            double believedInterest = perceived.PerceivedPoliceInterest(actor.Id);
            double exposure = Exposure(m3) * (0.35 + 1.6 * believedInterest) * 1.8;
            Add("legal exposure", -exposure,
                believedInterest > 0.2
                    ? "he believes police attention is already on him"
                    : "he sees no particular police interest");
        }

        // --- moral / social reluctance ------------------------------------------------------------
        if (cand.BreachesPolicyId is not null && cand.PolicyIssuerId is not null)
        {
            var loyalty = Loyalty(actor, psy, cand.PolicyIssuerId);
            // Pride reduces deference: a proud man discounts a rule he did not set.
            double scale = cand.BreachesPolicyStrength * (1 - 0.55 * proud) * 2.4;

            // The rule weighs something regardless of who set it.
            Add("reluctance to breach policy", -0.6 * scale,
                proud > 0.5
                    ? $"the ban weighs on him less than it should, because it is not his rule"
                    : $"it would defy {cand.PolicyIssuerId}'s standing instruction");

            // And more, or less, according to what binds him to the man who set it.
            AddLoyaltyParts("reluctance to breach policy", -1.2 * scale, loyalty,
                $"the rule comes from a man he trusts",
                $"the rule comes from a man he owes",
                "stepping over his own outfit's rule sits badly with him",
                $"what he holds against {cand.PolicyIssuerId} makes the rule easier to step over");
        }

        // --- what to say when reporting -------------------------------------------------------------
        //
        // Concealment is priced against three things he can actually perceive: how badly the fact
        // would hurt him if his superior had it, how likely he thinks it is that somebody else
        // already saw it, and what he owes the man he would be lying to. He is not consulting
        // whether the claim is objectively true — only what he holds and what he expects.
        //
        // NOTE ON THE PAIRING, milestone 008 ruling 2. Every report candidate carries two
        // relationship considerations, and they are two things rather than one:
        //
        //   * from the ReportToSuperior case far above, `+0.7 * loyalty` — the general standing a man
        //     gets from reporting to his superior at all;
        //   * from the branches below, the relationship cost of the candour he selected —
        //     `+0.8` for handing it over straight, `-0.5` for an omission, `-1.4` for a lie.
        //
        // Both are legitimate and both keep their coefficients. What was wrong was that they were
        // indistinguishable once summed and invisible once rendered: on a partial report they net to
        // `0.2 * loyalty`, and at Vincent's seeds that is 0.044 falling to 0.006 after two account
        // conflicts — both halves under the trace's 0.15 cutoff, so the reason list printed nothing
        // at all for the candidate milestone 007's central finding was measured on. They are now
        // separately named, separately tagged, and reported gross as well as net.
        if (cand.Candor is { } candor && cand.TargetId is not null)
        {
            var loyalty = Loyalty(actor, psy, cand.TargetId);

            // How exposed he believes he already is. A man who thinks the street saw him has
            // less to gain by denying it, because the denial is what gets caught.
            double believedWitnesses = 0;
            foreach (var w in perceived.OfKind(ClaimKind.WitnessSawIncident))
                believedWitnesses = Math.Max(believedWitnesses, w.Confidence);

            // The weight of what he would be hiding, as he holds it. Used only by the Candid branch
            // below, which prices the cost of handing it over — a cost history cannot discount,
            // because disclosing it is still the first time he would have disclosed it.
            double stakes = cand.Suppressed.Count == 0
                ? 0
                : cand.Suppressed.Max(s => perceived.Confidence(s.Claim));

            double protection = SelfProtection(candor, cand.Suppressed, perceived);

            switch (candor)
            {
                case ReportCandor.Candid when stakes > 0:
                    Add("self-protection", -2.0 * stakes,
                        "telling him straight means handing over the part that damns him");
                    AddLoyaltyParts("relationship effects", 0.8, loyalty,
                        "he would rather a man he trusts heard it from him than from someone else",
                        "a man he owes has a straight account coming to him",
                        "coming clean is what belonging asks of him",
                        "what he holds against him takes the value out of coming clean");
                    break;

                case ReportCandor.Partial:
                    Add("self-protection", protection, "what he leaves out cannot be held against him");
                    Add("perceived personal risk", -1.1 * believedWitnesses,
                        "an account with a hole in it invites the question he cannot answer");
                    AddLoyaltyParts("relationship effects", -0.5, loyalty,
                        "he is holding out on a man he trusts",
                        "he is holding out on a man he owes",
                        "an account with a hole in it is not what belonging asks",
                        "what he holds against him makes the omission sit easier");
                    break;

                case ReportCandor.False:
                    // A denial that holds is worth more than an omission: an omission leaves the
                    // fact sitting there to be found, a successful denial buries it. What makes
                    // it dangerous is almost entirely whether he thinks somebody can contradict
                    // him — so the risk is carried by believed witnesses rather than by a flat
                    // penalty on lying. A large constant here would mean no configuration of any
                    // character could ever choose this, which is not a deterrent, it is a
                    // candidate that exists only to be rejected.
                    Add("self-protection", protection, "a flat denial buries it, if it holds");
                    Add("perceived personal risk", -(0.25 + 3.0 * believedWitnesses) * (1 + cautious),
                        believedWitnesses > 0.4
                            ? "he thinks there were people on that street who saw him"
                            : "he does not think anyone can put him there");
                    AddLoyaltyParts("relationship effects", -1.4, loyalty,
                        "he would be lying to a man he trusts",
                        "he would be lying to a man he owes",
                        "lying to his own is the opposite of belonging",
                        "what he holds against him makes lying to him sit easier");
                    break;
            }
        }

        // Putting a question to somebody buys certainty about *the thing he is asking about*, and
        // costs standing only if asking means going over the head of whoever told him.
        //
        // Both halves used to be wrong in the same way: the uncertainty term scanned every
        // testimonial belief he held and priced the question off the weakest one, whatever the
        // question was actually about. A man perfectly happy with his information on the matter in
        // hand scored a large "he is not satisfied with the account he has" because something
        // unrelated and thin was sitting in his head — and once a delegator could ask about a claim
        // he had established himself, the term stopped having any connection to the candidate at
        // all. The standing cost had the matching defect: it was charged unconditionally, so a man
        // asking the very person named in a claim he found himself was recorded as going behind a
        // source who did not exist.
        if (cand.Kind == ActionKind.SeekCorroboration && cand.TargetId is not null)
        {
            // Only what this question is about. A candidate of this kind without a subject is
            // malformed — both generators set one — and scoring silently invents nothing for it.
            if (cand.AboutClaim is { } asked && perceived.Position(asked) is { } position)
            {
                Add("uncertainty", 1.5 * (1 - position.Confidence),
                    position.Confidence > 0.7
                        ? "he is fairly sure of this already"
                        : "he is not satisfied with the account he has");

                // Going behind somebody only means something when there is somebody to go behind.
                // A claim he was told has a source whose word he is quietly declining to take; one
                // he saw, did, found or worked out has none, and asking the man named in it for his
                // version is an ordinary question rather than a slight.
                //
                // TAGGED `None`, DELIBERATELY, AND PINNED BY A TEST. This is `-0.45 * proud`: it
                // reads a trait and no relationship state whatsoever. It carries the name
                // "relationship effects" because socially that is what it is about, and that name is
                // exactly what made it dangerous — across the five variants at seed 42 it accounts
                // for 61 of the 168 components carrying that name, 36% of them, and two production
                // tests plus this milestone's whole diagnostic were built to aggregate on the name.
                // A label is not a derivation. The facet is the derivation.
                if (position.SourceKind.IsTestimony() && position.SourceId != cand.TargetId)
                    Add("relationship effects", -0.45 * proud,
                        $"going behind {position.SourceId} is an admission he does not trust him",
                        RelationshipFacet.None);
            }
        }

        // --- uncertainty ----------------------------------------------------------------------------
        if (cand.RequiredKnowledge.Count > 0)
        {
            double avg = cand.RequiredKnowledge.Average(perceived.Confidence);
            double penalty = (1 - avg) * 1.2 * (1 + cautious);
            Add("uncertainty", -penalty, $"his information here is {(avg > 0.7 ? "solid" : "thin")}");
        }

        // --- switching and opportunity cost -----------------------------------------------------------
        if (cand.Kind is ActionKind.AlterStrategy or ActionKind.AbandonStrategy or ActionKind.PostponeStrategy
            && actor.Execution.Strategy is not null)
        {
            double baseCost = cand.Kind == ActionKind.AbandonStrategy ? 1.1 : 0.5;
            double statusCost = cand.Kind == ActionKind.AbandonStrategy ? 0.9 * proud : 0.3 * proud;
            Add("switching cost", -(baseCost + actor.Execution.CommitmentWeight * 0.5 + statusCost),
                proud > 0.5 && cand.Kind == ActionKind.AbandonStrategy
                    ? "backing down now would cost him standing"
                    : "changing course wastes what he has already spent");
        }

        double subtotal = parts.Sum(p => p.Value);

        // Controlled noise breaks near-ties only. It is recorded separately so it can never be
        // mistaken for motive when reading a trace.
        double noise = rng.Range(-0.05, 0.05);

        return new ScoreBreakdown(cand, subtotal + noise, noise, parts);
    }

    // ---------------------------------------------------------------- concealment value
    /// <summary>What silence is worth about a matter this recipient has heard nothing on.</summary>
    private const double OmissionProtection = 1.5;

    /// <summary>
    /// The extra a denial buys over silence. An omission leaves the fact sitting there to be found;
    /// a denial that holds buries it.
    ///
    /// Not a new coefficient. <c>OmissionProtection + DenialPremium</c> is 1.9, exactly the figure a
    /// first-time denial has always scored — the two are what that number was always the sum of, now
    /// separated so the second can be charged when only the first has already been bought.
    /// </summary>
    private const double DenialPremium = 0.4;

    /// <summary>
    /// What this report's concealment is worth <em>beyond what the sender already holds</em>.
    ///
    /// The defect this replaces: self-protection was paid in full on every report, so a man who had
    /// already kept something from his boss was paid again for keeping it from him a second, third
    /// and fourth time, and "report while hiding the same thing" won indefinitely. It is the same
    /// shape as the repeated-partial-report bug milestone 003 fixed in
    /// <see cref="Reporting.LastAddressed"/>, with the scoring half left undone: eligibility called
    /// the matter settled while scoring called it freshly at stake.
    ///
    /// Four states and two acts, per <see cref="PriorDisclosureState"/>:
    ///
    ///  - <b>nothing said</b> — omission buys <c>1.5</c>, denial buys <c>1.9</c>;
    ///  - <b>already withheld</b> — omitting again buys nothing, since the silence is already his;
    ///    escalating to a denial still buys the premium;
    ///  - <b>already disclosed</b> — the recipient has the claim, so omitting it later buys nothing;
    ///    denying it is a fresh attempt to bury something he holds, and buys the premium;
    ///  - <b>already denied</b> — the recipient has the denial. Neither act buys anything.
    ///
    /// <b>Per claim to completion, then the maximum.</b> Each suppressed claim's value is computed
    /// whole and the largest wins. Taking separate maxima of the omission and premium halves and
    /// adding them would let the two come from different claims and report a figure that no single
    /// concealment actually buys.
    ///
    /// Reads the actor's own perceived confidence for the stakes, so this remains a judgement about
    /// what he thinks he is hiding, not about what is true.
    /// </summary>
    private static double SelfProtection(
        ReportCandor candor, IReadOnlyList<SuppressedClaim> suppressed, PerceivedSituation perceived)
    {
        double best = 0;

        foreach (var s in suppressed)
        {
            double stakes = perceived.Confidence(s.Claim);
            double value = candor switch
            {
                ReportCandor.Partial => s.Prior == PriorDisclosureState.NeverAddressed
                    ? OmissionProtection * stakes
                    : 0,
                ReportCandor.False => s.Prior switch
                {
                    PriorDisclosureState.NeverAddressed => (OmissionProtection + DenialPremium) * stakes,
                    PriorDisclosureState.Denied => 0,
                    _ => DenialPremium * stakes,
                },
                _ => 0,
            };

            if (value > best) best = value;
        }

        return best;
    }

    /// <summary>Which drives an option serves or sacrifices. Weights, not goals.</summary>
    private static IEnumerable<(Drive Drive, double Weight)> DriveProfile(Candidate c)
    {
        if (c.Strategy == StrategyKind.SecureTribute)
        {
            yield return (Drive.Wealth, 1.0);
            yield return (Drive.Status, 0.4);
        }
        if (c.Strategy == StrategyKind.ConcealIncident)
            yield return (Drive.Security, 1.1);
        if (c.Strategy == StrategyKind.InvestigateIncident)
            yield return (Drive.Status, 0.7);

        switch (c.Method)
        {
            case CoercionMethod.Force:
                yield return (Drive.Status, 0.4);
                yield return (Drive.Security, -0.7);
                break;
            case CoercionMethod.Threaten:
                yield return (Drive.Status, 0.2);
                yield return (Drive.Security, -0.3);
                break;
        }

        switch (c.Kind)
        {
            case ActionKind.ReportToSuperior:
            case ActionKind.SeekApproval:
                // Reporting at all serves belonging. Concealing within it serves security instead,
                // and costs the belonging it would otherwise have bought.
                yield return (Drive.Belonging, c.Candor is ReportCandor.Candid or null ? 0.7 : 0.2);
                yield return (Drive.Status, -0.25);
                if (c.Candor is ReportCandor.Partial or ReportCandor.False)
                    yield return (Drive.Security, 0.8);
                break;

            case ActionKind.SeekCorroboration:
                yield return (Drive.Security, 0.5);
                break;
            case ActionKind.AbandonStrategy:
                yield return (Drive.Status, -0.9);
                yield return (Drive.Security, 0.5);
                break;
            case ActionKind.Concede:
                yield return (Drive.Status, -0.5);
                yield return (Drive.Wealth, -0.7);
                yield return (Drive.Security, 0.9);
                break;
            case ActionKind.Refuse:
                yield return (Drive.Wealth, 0.7);
                yield return (Drive.Status, 0.3);
                yield return (Drive.Security, -0.8);
                break;
            case ActionKind.Retaliate:
                yield return (Drive.Status, 0.6);
                yield return (Drive.Security, -0.4);
                break;
            case ActionKind.DelegateStrategy:
                yield return (Drive.Security, 0.4);
                break;
        }
    }
}
