namespace CrimeSim.Sim;

/// <summary>
/// Thrown when the event loop is about to do something that should be structurally impossible — a
/// stale or misrouted strategy step reaching Strategies.Advance, or an observation opportunity
/// missing the occasion key every scheduler is required to set. This is a development kernel: a
/// violation here means a scheduling invariant broke somewhere upstream, and failing loudly is what
/// SIMULATION_ARCHITECTURE.md's "stale references fail visibly in development rather than silently
/// corrupting history" asks for, rather than quietly advancing the wrong instance.
///
/// Properly cancelled events never reach this: EventQueue.Next skips them before delivery. If this
/// is ever thrown from a run that made no direct, out-of-band event, the defect is upstream — a
/// scheduling path that failed to cancel a superseded step, not this check.
/// </summary>
public sealed class SimulationInvariantException : InvalidOperationException
{
    public SimulationInvariantException(string message) : base(message) { }
}
