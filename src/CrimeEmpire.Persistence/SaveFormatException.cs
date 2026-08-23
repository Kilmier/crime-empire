namespace CrimeEmpire.Persistence;

/// <summary>
/// A save could not be loaded, and nothing was substituted for it.
///
/// Ruling 7: malformed schema, an invalid command kind, an option token that names nothing offered
/// at its point in the replay, or a reordered/duplicated/gapped command ordinal must all fail
/// visibly rather than autoplay, reset, or partially load. Every one of those cases throws this
/// (or, for an option token replay rejects, the same <c>SimulationInvariantException</c> a live
/// session already throws for the identical reason) instead of returning a best-effort session.
/// </summary>
public sealed class SaveFormatException : Exception
{
    public SaveFormatException(string message) : base(message) { }

    public SaveFormatException(string message, Exception innerException) : base(message, innerException) { }
}
