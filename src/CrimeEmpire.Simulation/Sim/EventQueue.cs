namespace CrimeSim.Sim;

/// <summary>
/// Discrete-event queue under a continuous game calendar. Empty time is skipped: the clock jumps
/// to the next scheduled event rather than ticking.
///
/// Ordering is (time, sequence). The sequence tiebreak is what makes replay reproducible — without
/// it, two events at the same instant would resolve in whatever order the heap happened to produce.
/// </summary>
public sealed class EventQueue
{
    private readonly PriorityQueue<ScheduledEvent, (DateTime Time, long Seq)> _queue = new();
    private readonly Dictionary<long, string> _cancelled = new();
    private long _nextId = 1;

    public int Count => _queue.Count;

    /// <summary>Cancelled events and the reason, kept for the developer trace.</summary>
    public IReadOnlyDictionary<long, string> Cancelled => _cancelled;

    /// <summary>
    /// Every event still pending, in (time, sequence) order — read without dequeuing, so inspecting
    /// it changes nothing about how <see cref="Next"/> will later drain it.
    ///
    /// Internal and read-only: it exists purely so a replay/parity comparator (test-only) can see
    /// queued-event <em>contents</em> rather than only <see cref="Count"/>, mirroring the same
    /// internal-accessor pattern <c>SimulationSession.World</c> already uses for the identical
    /// reason — the type system admits the test assembly and nothing else. No simulation behaviour
    /// reads this member or is affected by its existence.
    /// </summary>
    internal IReadOnlyList<ScheduledEvent> PendingEvents
        => _queue.UnorderedItems
            .Select(item => item.Element)
            .OrderBy(e => e.Time)
            .ThenBy(e => e.Id)
            .ToList();

    public ScheduledEvent Schedule(
        DateTime time,
        EventKind kind,
        string? ownerId,
        string cause,
        EventPayload? payload = null)
    {
        var ev = new ScheduledEvent
        {
            Id = _nextId++,
            Time = time,
            Kind = kind,
            OwnerId = ownerId,
            Cause = cause,
            Payload = payload ?? EventPayload.None,
        };
        _queue.Enqueue(ev, (time, ev.Id));
        return ev;
    }

    /// <summary>
    /// Invalidate a pending event. The reason is retained rather than the event silently vanishing —
    /// "invalidated events cancel or transform safely and leave a traceable reason".
    /// </summary>
    public void Cancel(long eventId, string reason) => _cancelled[eventId] = reason;

    /// <summary>Dequeues the next live event, skipping cancelled ones.</summary>
    public ScheduledEvent? Next(DateTime notAfter)
    {
        while (_queue.TryPeek(out var peeked, out var key))
        {
            if (key.Time > notAfter) return null;
            _queue.Dequeue();
            if (_cancelled.ContainsKey(peeked.Id)) continue;
            return peeked;
        }
        return null;
    }
}
