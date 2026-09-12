namespace CrimeSim.Session;

/// <summary>
/// The one out-of-fiction objective for the bounded harbour scenario.
///
/// This is session metadata, not something any character knows and not a claim about the world.
/// Keeping it beside <see cref="SimulationSession"/> rather than in <see cref="PlayerSnapshot"/>
/// prevents a scenario brief from becoming in-fiction knowledge merely because an interface shows it.
/// </summary>
public sealed record SessionObjective(string Name, DateTime Deadline);

/// <summary>The two possible results of the bounded scenario objective.</summary>
public enum ObjectiveOutcome
{
    ObjectiveMet,
    ObjectiveUnmet,
}

/// <summary>
/// The immutable, out-of-fiction result exposed once the fixed session deadline has been drained.
/// It deliberately carries no score, condition value, character judgment, or path back to world state.
/// </summary>
public sealed record SessionResult(ObjectiveOutcome Outcome, DateTime ResolvedAt);
