namespace CrimeEmpire.Persistence;

/// <summary>
/// Everything a save stores, and nothing else — ruling 2 and ruling 9. No <c>World</c>, no opaque
/// blob: <see cref="Commands"/> is a plain ordered list of the same three shapes a Godot button press
/// already produces.
/// </summary>
/// <param name="SchemaVersion">
/// The save file's own column/table shape. Bumped only when that shape changes — independent of
/// <see cref="BuildId"/>, which tracks the simulation's compiled behaviour instead.
/// </param>
/// <param name="BuildId">
/// <c>CrimeEmpire.Simulation</c>'s own assembly <c>ModuleVersionId</c>, as a GUID string. Ruling 8:
/// compatibility is same-build only, and this is what "same build" means — see
/// <see cref="SimulationBuild.CurrentId"/>.
/// </param>
/// <param name="Seed">The seed the scenario was built with.</param>
/// <param name="Variant">The scenario variant.</param>
/// <param name="ControlledCharacterId">Who the player controlled, or null.</param>
/// <param name="ViewpointCharacterId">Whose knowledge the snapshot is limited to.</param>
/// <param name="Commands">The ordered log of successful session inputs.</param>
public sealed record SaveData(
    int SchemaVersion,
    string BuildId,
    int Seed,
    string Variant,
    string? ControlledCharacterId,
    string ViewpointCharacterId,
    IReadOnlyList<SessionCommand> Commands);
