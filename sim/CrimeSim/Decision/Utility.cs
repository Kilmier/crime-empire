namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Sim;

public sealed record ScoreComponent(string Name, double Value, string Explanation);

public sealed record ScoreBreakdown(
    Candidate Candidate,
    double Total,
    double Noise,
    IReadOnlyList<ScoreComponent> Components)
{
    public IEnumerable<ScoreComponent> Significant()
        => Components.Where(c => Math.Abs(c.Value) >= 0.15)
                     .OrderByDescending(c => Math.Abs(c.Value));
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
    /// Loyalty is derived, not stored. Attachment, obligation, a general need to belong and
    /// accumulated grievance pull in different directions; collapsing them into one saved number
    /// would erase exactly the distinctions that make a betrayal legible.
    /// </summary>
    public static double Loyalty(CharacterView actor, Psychology psy, string otherId)
    {
        var rel = actor.Social.Toward(otherId);
        double v = 0.45 * rel.Trust
                 + 0.30 * rel.Obligation
                 + 0.25 * psy[Drive.Belonging]
                 - 0.50 * actor.Social.GrievanceAgainst(otherId);
        return Math.Clamp(v, 0, 1);
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

        void Add(string name, double value, string why)
        {
            if (Math.Abs(value) <= 1e-9) return;
            // Fold repeats together — an option that serves status twice should read as one larger
            // reason, not as the same sentence printed twice.
            int at = parts.FindIndex(p => p.Name == name && p.Explanation == why);
            if (at >= 0) parts[at] = parts[at] with { Value = parts[at].Value + value };
            else parts.Add(new ScoreComponent(name, value, why));
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
            {
                double loyalty = Loyalty(actor, psy, cand.TargetId);
                Add("relationship effects", 0.9 * loyalty, $"he expects {cand.TargetId} to carry it out for him");
                break;
            }
            case ActionKind.ReportToSuperior when cand.TargetId is not null:
                Add("relationship effects", 0.7 * Loyalty(actor, psy, cand.TargetId), "reporting maintains standing with his superior");
                break;
            case ActionKind.Retaliate when cand.TargetId is not null:
                Add("relationship effects", 0.8 * actor.Social.GrievanceAgainst(cand.TargetId), "he holds a grievance against the target");
                break;
            case ActionKind.SeekApproval when cand.TargetId is not null:
                Add("relationship effects", 0.4 * actor.Social.Toward(cand.TargetId).Obligation, "it defers to his superior");
                break;
            case ActionKind.Concede when cand.TargetId is not null:
                Add("relationship effects", 1.6 * actor.Social.Toward(cand.TargetId).Fear, "he is afraid of them");
                break;
            case ActionKind.Refuse when cand.TargetId is not null:
                Add("relationship effects", -1.6 * actor.Social.Toward(cand.TargetId).Fear, "holding out against them frightens him");
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
            double loyalty = Loyalty(actor, psy, cand.TargetId);
            Add("perceived personal risk", -(1.3 + 2.2 * loyalty) * (1 - 0.4 * aggressive),
                "going after him would be a serious step");
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
            double loyalty = Loyalty(actor, psy, cand.PolicyIssuerId);
            // Pride reduces deference: a proud man discounts a rule he did not set.
            double reluctance = cand.BreachesPolicyStrength * (0.6 + 1.2 * loyalty) * (1 - 0.55 * proud) * 2.4;
            Add("reluctance to breach policy", -reluctance,
                proud > 0.5
                    ? $"the ban weighs on him less than it should, because it is not his rule"
                    : $"it would defy {cand.PolicyIssuerId}'s standing instruction");
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
                yield return (Drive.Belonging, 0.7);
                yield return (Drive.Status, -0.25);
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
