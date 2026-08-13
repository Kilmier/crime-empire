namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;

public enum AgendaKind
{
    RespondToTrigger,
    FulfilAssignment,
    DischargeResponsibility,
    RelievePressure,
    ContinueCommitment,
    Idle,
}

/// <summary>What currently matters to the character — the second question the pipeline separates.</summary>
public sealed record Agenda(
    AgendaKind Kind,
    string Description,
    string Reason,
    string? Domain = null,
    string? TargetId = null,
    long? AssignmentId = null,
    double Weight = 1.0);

public static class AgendaSelection
{
    /// <summary>
    /// Picks the one thing demanding attention. Ordered rather than scored: an urgent interrupt
    /// beats a standing assignment, which beats a background pressure. Keeping this a short
    /// explicit ladder is what makes the resulting trace readable.
    /// </summary>
    public static Agenda Select(
        World world,
        Character c,
        ScheduledEvent trigger,
        PerceivedSituation perceived,
        Assignment? assignment)
    {
        // 1. Urgent events demand a direct response.
        if (trigger.Kind is EventKind.Incident or EventKind.StrategyBlocked or EventKind.PressureThreshold)
        {
            return new Agenda(
                AgendaKind.RespondToTrigger,
                trigger.Cause,
                $"the event demanded a response ({trigger.Kind})",
                // Falling back to their standing beat matters: without it, someone with no active
                // strategy (a detective hearing a rumour) responds to an event with no domain and
                // so generates no role-based options at all.
                Domain: c.Execution.Strategy?.Domain
                        ?? assignment?.Domain
                        ?? c.Motivations.Responsibilities.FirstOrDefault()?.Domain,
                TargetId: trigger.Payload.TargetId,
                AssignmentId: assignment?.Id,
                Weight: 1.3);
        }

        // 2. A live assignment with a deadline outranks self-directed concerns.
        if (assignment is not null && assignment.Deadline >= world.Now)
        {
            double urgency = Urgency(world.Now, assignment.IssuedAt, assignment.Deadline);
            return new Agenda(
                AgendaKind.FulfilAssignment,
                assignment.Objective,
                $"he owes {world.Get(assignment.IssuerId).Name} a result by {assignment.Deadline:yyyy-MM-dd}",
                assignment.Domain,
                AssignmentId: assignment.Id,
                Weight: 1.0 + urgency * 0.5);
        }

        // 3. An active strategy continues to matter until it resolves.
        if (c.Execution.Strategy is { } strat)
        {
            return new Agenda(
                AgendaKind.ContinueCommitment,
                $"ongoing: {strat.Label}",
                "he has an unfinished course of action",
                strat.Domain,
                strat.TargetId,
                strat.AssignmentId,
                Weight: 0.9);
        }

        // 4. Otherwise the loudest pressure.
        if (c.Motivations.Dominant() is { } pressure && pressure.Value > 0.35)
        {
            return new Agenda(
                AgendaKind.RelievePressure,
                $"pressure: {pressure.Kind}",
                $"{pressure.Kind} had grown to {pressure.Value:0.00}",
                c.Motivations.Responsibilities.FirstOrDefault()?.Domain,
                Weight: 0.7 + pressure.Value * 0.6);
        }

        // 5. Standing duty.
        if (c.Motivations.Responsibilities.Count > 0)
        {
            var r = c.Motivations.Responsibilities[0];
            return new Agenda(
                AgendaKind.DischargeResponsibility,
                r.Description,
                "a standing responsibility came up for review",
                r.Domain,
                Weight: 0.6);
        }

        return new Agenda(AgendaKind.Idle, "nothing pressing", "no agenda cleared the threshold", Weight: 0.2);
    }

    public static double Urgency(DateTime now, DateTime issued, DateTime deadline)
    {
        double total = (deadline - issued).TotalDays;
        if (total <= 0) return 1.0;
        double elapsed = (now - issued).TotalDays;
        return Math.Clamp(elapsed / total, 0, 1);
    }
}
