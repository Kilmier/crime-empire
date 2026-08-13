using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

public sealed class EventQueueTests
{
    [Fact]
    public void Next_orders_events_by_time_then_insertion_order()
    {
        var queue = new EventQueue();
        var start = new DateTime(1987, 3, 2, 8, 0, 0, DateTimeKind.Utc);

        var later = queue.Schedule(start.AddHours(2), EventKind.Incident, "vincent", "later");
        var firstAtSameTime = queue.Schedule(start.AddHours(1), EventKind.Incident, "vincent", "first");
        var secondAtSameTime = queue.Schedule(start.AddHours(1), EventKind.Incident, "vincent", "second");

        Assert.Equal(firstAtSameTime.Id, queue.Next(start.AddDays(1))?.Id);
        Assert.Equal(secondAtSameTime.Id, queue.Next(start.AddDays(1))?.Id);
        Assert.Equal(later.Id, queue.Next(start.AddDays(1))?.Id);
        Assert.Null(queue.Next(start.AddDays(1)));
    }

    [Fact]
    public void Next_skips_cancelled_events_and_retains_the_reason()
    {
        var queue = new EventQueue();
        var at = new DateTime(1987, 3, 2, 8, 0, 0, DateTimeKind.Utc);
        var cancelled = queue.Schedule(at, EventKind.StrategyStep, "vincent", "obsolete plan");
        var live = queue.Schedule(at.AddMinutes(1), EventKind.Incident, "vincent", "new information");

        queue.Cancel(cancelled.Id, "the target left town");

        Assert.Equal(live.Id, queue.Next(at.AddHours(1))?.Id);
        Assert.Equal("the target left town", queue.Cancelled[cancelled.Id]);
    }

    [Fact]
    public void Next_does_not_advance_past_the_requested_calendar_time()
    {
        var queue = new EventQueue();
        var at = new DateTime(1987, 3, 2, 8, 0, 0, DateTimeKind.Utc);
        var future = queue.Schedule(at.AddDays(7), EventKind.WorldTick, null, "a week passed");

        Assert.Null(queue.Next(at.AddDays(6)));
        Assert.Equal(future.Id, queue.Next(at.AddDays(7))?.Id);
    }
}
