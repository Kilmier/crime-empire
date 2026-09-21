using System.Reflection;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 027: the harbour scenario advertises one out-of-fiction objective, drains its fixed
/// inclusive boundary, exposes only a one-bit result, and becomes immutable to simulation input.
/// </summary>
public sealed class SessionEndingTests
{
    private const int Seed = 42;
    private static DateTime Deadline => Cast.Start.AddDays(90);

    [Fact]
    public void Objective_is_public_from_the_opening_instant_without_a_live_result()
    {
        var session = Watch("baseline", "salvatore");

        Assert.Equal(Cast.Start, session.Date);
        Assert.Equal("Bring the harbour shortfall under control", session.Objective.Name);
        Assert.Equal(Deadline, session.Objective.Deadline);
        Assert.Null(session.Result);
        Assert.Equal(SessionStatus.Ready, session.Status);
    }

    [Theory]
    [InlineData("baseline", ObjectiveOutcome.ObjectiveUnmet, 0.50)]
    [InlineData("cautious-vincent", ObjectiveOutcome.ObjectiveUnmet, 0.50)]
    public void Natural_watch_only_runs_record_the_untuned_parallel_scenario_results(
        string variant,
        ObjectiveOutcome expected,
        double expectedLoss)
    {
        var session = Watch(variant, "salvatore");

        session.AdvanceTo(Deadline);

        Assert.Equal(SessionStatus.Resolved, session.Status);
        Assert.Equal(Deadline, session.Date);
        Assert.Equal(new SessionResult(expected, Deadline), session.Result);
        Assert.Equal(expectedLoss, session.World.Org.Condition(OrgCondition.RevenueLoss), precision: 10);
    }

    [Theory]
    [InlineData(0.349999, ObjectiveOutcome.ObjectiveMet)]
    [InlineData(Organization.SignificantRevenueLoss, ObjectiveOutcome.ObjectiveUnmet)]
    [InlineData(0.350001, ObjectiveOutcome.ObjectiveUnmet)]
    public void Terminal_threshold_is_strictly_below_the_existing_significance_boundary(
        double loss,
        ObjectiveOutcome expected)
    {
        var session = Watch("baseline", "salvatore");
        session.AdvanceTo(Deadline.AddTicks(-1));
        session.World.Org.Conditions[OrgCondition.RevenueLoss] = loss;

        session.AdvanceTo(Deadline);

        Assert.Equal(expected, session.Result!.Outcome);
    }

    [Fact]
    public void Every_event_at_the_deadline_is_drained_and_a_later_event_stays_queued()
    {
        var session = Watch("baseline", "salvatore");
        session.AdvanceTo(Deadline.AddTicks(-1));

        session.World.Queue.Schedule(Deadline, EventKind.Incident, null, "first exact-deadline event");
        session.World.Queue.Schedule(Deadline, EventKind.Incident, null, "second exact-deadline event");
        var later = session.World.Queue.Schedule(
            Deadline.AddTicks(1), EventKind.Incident, null, "event after the terminal boundary");

        session.StepEvent();

        Assert.Equal(SessionStatus.Resolved, session.Status);
        Assert.Null(session.World.Queue.Next(Deadline));
        Assert.Equal(later.Id, session.World.Queue.Next(Deadline.AddTicks(1))!.Id);
    }

    [Fact]
    public void A_controlled_decision_at_the_deadline_must_be_answered_before_resolution()
    {
        var session = SimulationSession.Start(Seed, "baseline", "vincent");
        AdvanceWithAutomaticChoices(session, Deadline.AddTicks(-1));
        session.World.Queue.Schedule(Deadline, EventKind.RoleReview, "vincent", "exact-deadline review");
        session.World.Queue.Schedule(Deadline, EventKind.Incident, null, "same-instant tail");

        session.StepEvent();

        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
        Assert.Equal(Deadline, session.Date);
        Assert.Null(session.Result);

        session.ChooseAndConfirm(session.Pending!.Options[0].Id);

        Assert.Equal(SessionStatus.Resolved, session.Status);
        Assert.NotNull(session.Result);
        Assert.Null(session.World.Queue.Next(Deadline));
    }

    [Fact]
    public void Every_oversized_advance_form_clamps_to_the_terminal_boundary()
    {
        var byDays = Watch("baseline", "salvatore");
        byDays.AdvanceDays(int.MaxValue);

        var byDate = Watch("baseline", "salvatore");
        byDate.AdvanceTo(DateTime.MaxValue);

        var byEvent = Watch("baseline", "salvatore");
        byEvent.AdvanceTo(Deadline.AddTicks(-1));
        var later = byEvent.World.Queue.Schedule(
            Deadline.AddTicks(1), EventKind.Incident, null, "next event is outside the session");
        byEvent.StepEvent();

        foreach (var session in new[] { byDays, byDate, byEvent })
        {
            Assert.Equal(SessionStatus.Resolved, session.Status);
            Assert.Equal(Deadline, session.Date);
        }
        Assert.Equal(later.Id, byEvent.World.Queue.Next(Deadline.AddTicks(1))!.Id);
    }

    [Fact]
    public void Every_simulation_input_refuses_after_resolution_before_any_state_mutates()
    {
        var session = Watch("baseline", "salvatore");
        session.AdvanceTo(Deadline);
        var before = Fingerprint(session);

        Action[] refused =
        {
            session.StepEvent,
            () => session.AdvanceDays(1),
            () => session.AdvanceTo(DateTime.MaxValue),
            () => session.ChooseAndConfirm("not-an-option"),
            session.ResolveAutomatically,
        };

        foreach (var input in refused)
        {
            Assert.Throws<InvalidOperationException>(input);
            Assert.Equal(before, Fingerprint(session));
        }
    }

    [Fact]
    public void Result_is_not_part_of_the_character_snapshot_or_a_raw_progress_channel()
    {
        var session = Watch("baseline", "salvatore");
        var beforeTypeGraph = PublicTypeGraph(typeof(PlayerSnapshot)).ToHashSet();

        Assert.DoesNotContain(typeof(SessionObjective), beforeTypeGraph);
        Assert.DoesNotContain(typeof(SessionResult), beforeTypeGraph);
        Assert.DoesNotContain(typeof(ObjectiveOutcome), beforeTypeGraph);
        Assert.DoesNotContain(
            typeof(PlayerSnapshot).GetProperties(),
            p => p.Name.Contains("Objective", StringComparison.OrdinalIgnoreCase)
                || p.Name.Contains("Result", StringComparison.OrdinalIgnoreCase)
                || p.Name.Contains("RevenueLoss", StringComparison.OrdinalIgnoreCase));

        session.AdvanceTo(Deadline);

        Assert.Equal(
            new[] { nameof(SessionResult.Outcome), nameof(SessionResult.ResolvedAt) },
            typeof(SessionResult).GetProperties().Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal));
        Assert.DoesNotContain("Bring the harbour shortfall under control", SnapshotStrings(session.Snapshot()));
    }

    [Fact]
    public void Control_policy_and_viewpoint_do_not_create_a_second_terminal_path()
    {
        var watchedByBoss = Watch("baseline", "salvatore");
        watchedByBoss.AdvanceTo(Deadline);

        var watchedByVincent = Watch("baseline", "vincent");
        watchedByVincent.AdvanceTo(Deadline);

        var controlled = SimulationSession.Start(Seed, "baseline", "vincent", "salvatore");
        AdvanceWithAutomaticChoices(controlled, Deadline);

        string expectedTrace = TraceWriter.Render(watchedByBoss.World, "baseline", false);
        Assert.Equal(expectedTrace, TraceWriter.Render(watchedByVincent.World, "baseline", false));
        Assert.Equal(expectedTrace, TraceWriter.Render(controlled.World, "baseline", false));
        Assert.Equal(watchedByBoss.Result, watchedByVincent.Result);
        Assert.Equal(watchedByBoss.Result, controlled.Result);
        Assert.All(new[] { watchedByBoss, watchedByVincent, controlled },
            s => Assert.Equal(SessionStatus.Resolved, s.Status));
    }

    private static SimulationSession Watch(string variant, string viewpoint)
        => SimulationSession.Start(Seed, variant, controlledCharacterId: null, viewpointCharacterId: viewpoint);

    private static void AdvanceWithAutomaticChoices(SimulationSession session, DateTime horizon)
    {
        session.AdvanceTo(horizon);
        for (int guard = 0; guard < 20000 && session.Status == SessionStatus.AwaitingChoice; guard++)
            session.ResolveAutomatically();
        Assert.NotEqual(SessionStatus.AwaitingChoice, session.Status);
    }

    private static IReadOnlyList<object> Fingerprint(SimulationSession session)
        => DeepFingerprint(session, new HashSet<object>(ReferenceEqualityComparer.Instance)).ToList();

    private static IEnumerable<object> DeepFingerprint(object? node, HashSet<object> visited)
    {
        switch (node)
        {
            case null: yield break;
            case string or bool or byte or sbyte or short or ushort or int or uint or long or ulong
                or float or double or decimal or DateTime or DateTimeOffset or TimeSpan or Guid:
                yield return node;
                yield break;
        }

        var type = node.GetType();
        if (type.IsEnum) { yield return node; yield break; }
        if (!type.IsValueType && !visited.Add(node)) { yield return "<cycle>"; yield break; }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PriorityQueue<,>))
        {
            var items = (System.Collections.IEnumerable)type.GetProperty("UnorderedItems")!.GetValue(node)!;
            foreach (var item in items)
            foreach (var value in DeepFingerprint(item, visited))
                yield return value;
            yield break;
        }

        if (node is System.Collections.IDictionary dictionary)
        {
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                foreach (var value in DeepFingerprint(entry.Key, visited)) yield return value;
                foreach (var value in DeepFingerprint(entry.Value, visited)) yield return value;
            }
            yield break;
        }

        if (node is System.Collections.IEnumerable sequence)
        {
            foreach (var item in sequence)
            foreach (var value in DeepFingerprint(item, visited))
                yield return value;
            yield break;
        }

        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        foreach (var field in current.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            yield return field.Name;
            foreach (var value in DeepFingerprint(field.GetValue(node), visited)) yield return value;
        }
    }

    private static IEnumerable<Type> PublicTypeGraph(Type root)
    {
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>(new[] { root });
        while (queue.TryDequeue(out var type))
        {
            if (!seen.Add(type)) continue;
            yield return type;
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyType = property.PropertyType;
                queue.Enqueue(propertyType);
                if (propertyType.IsGenericType)
                    foreach (var argument in propertyType.GetGenericArguments()) queue.Enqueue(argument);
            }
        }
    }

    private static IEnumerable<string> SnapshotStrings(object? node)
    {
        if (node is null) yield break;
        if (node is string stringValue) { yield return stringValue; yield break; }
        if (node is System.Collections.IEnumerable sequence)
        {
            foreach (var item in sequence)
            foreach (var nestedText in SnapshotStrings(item)) yield return nestedText;
            yield break;
        }
        var type = node.GetType();
        if (type.IsPrimitive || type.IsEnum || node is DateTime) yield break;
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            foreach (var nestedText in SnapshotStrings(property.GetValue(node))) yield return nestedText;
        }
    }
}
