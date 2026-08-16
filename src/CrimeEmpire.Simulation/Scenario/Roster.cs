namespace CrimeSim.Scenario;

using CrimeSim.Sim;

/// <summary>One person a session can be opened on, as a new-game screen needs to list them.</summary>
public sealed record ScenarioCharacter(string Id, string Name, string RoleTitle);

/// <summary>One scenario configuration, as a new-game screen needs to list them.</summary>
public sealed record ScenarioVariant(string Id, string Description);

/// <summary>
/// What a new-game screen may ask about the scenario before a session exists.
///
/// <b>This is setup, not play, and the distinction is the whole justification for the type.</b>
/// Which people are in the scenario and which configurations exist are facts about the game being
/// started; they are not things a character knows, and nothing here is ever shown as knowledge.
/// Once a session is running, everything an interface may see comes through
/// <see cref="CrimeSim.Session.PlayerSnapshot"/>, which is bounded by one character's own cognition
/// and never enumerates the roster.
///
/// It exists so an interface never has to touch <see cref="World"/> to populate a dropdown. Reaching
/// into <c>World.Characters</c> for a start screen would be harmless in itself and would put the
/// one thing milestone 009 keeps out of the UI's hands directly into them.
/// </summary>
public static class Roster
{
    /// <summary>
    /// Who the start screen offers first. A presentation default rather than a simulation fact —
    /// nothing in the engine privileges him, and a session opened on anybody else, or on nobody,
    /// runs exactly the same way.
    /// </summary>
    public const string DefaultControlledId = "vincent";

    /// <summary>
    /// The scenario's people, in id order.
    ///
    /// Built by constructing a throwaway world, because the cast is defined by
    /// <see cref="Cast.Build"/> and a second hand-maintained list would drift from it. The seed is
    /// irrelevant — nothing about identity depends on it — and the world is discarded.
    /// </summary>
    public static IReadOnlyList<ScenarioCharacter> Characters(string variant)
        => Cast.Build(seed: 0, variant).Characters.Values
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .Select(c => new ScenarioCharacter(c.Id, c.Name, c.RoleTitle))
            .ToList();

    /// <summary>The falsification fixture's configurations, in the order <c>Variants.All</c> lists them.</summary>
    public static IReadOnlyList<ScenarioVariant> Variants()
        => Scenario.Variants.All
            .Select(v => new ScenarioVariant(v, Scenario.Variants.Describe(v)))
            .ToList();
}
