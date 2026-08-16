using System.Globalization;
using System.Reflection;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 009's falsification checks: that putting a person inside the decision pipeline changed
/// nothing about the pipeline, and that what the person is shown is bounded by what his character
/// could know.
///
/// The two halves need different instruments. Behavioural identity is asserted against the rendered
/// developer trace, byte for byte, because that is the artefact the accepted baselines in
/// `REVIEW_LEDGER.md` are recorded as. Information safety is asserted against every string that can
/// reach a surface, because a leak that only shows up in one renderer is still a leak.
/// </summary>
public sealed class PlayerSessionTests
{
    private const int Seed = 42;
    private const int Days = 90;
    private const string Controlled = "vincent";

    private static DateTime End => Cast.Start.AddDays(Days);

    public static TheoryData<string> AllVariants()
    {
        var data = new TheoryData<string>();
        foreach (var v in Variants.All) data.Add(v);
        return data;
    }

    // ================================================================= behavioural identity

    /// <summary>
    /// A session with nobody controlled is the batch simulation with a clock bolted to it.
    ///
    /// Compared on the full rendered developer trace rather than on a structural snapshot, because
    /// the trace is what the accepted baselines are, and because a snapshot is its own comparator —
    /// a field this milestone forgot to add would make the comparison blinder rather than fail. The
    /// trace covers the truth log, every decision's trigger, agenda, beliefs used, every scored
    /// candidate with its total, every rejection with its reason, and the relationship diagnostic.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllVariants))]
    public void A_session_with_nobody_controlled_is_byte_identical_to_the_batch_simulation(string variant)
    {
        var batch = Cast.Build(Seed, variant);
        Runner.Run(batch, End);

        var session = SimulationSession.Start(Seed, variant, controlledCharacterId: null, viewpointCharacterId: Controlled);
        session.AdvanceTo(End);

        Assert.Equal(SessionStatus.Ready, session.Status);
        Assert.Equal(TraceWriter.Render(batch, variant, false), TraceWriter.Render(session.World, variant, false));
    }

    /// <summary>
    /// The prepare/resolve boundary is a seam and not a change. A controlled session that takes the
    /// option the character himself preferred, at every single pause, must produce the accepted
    /// history exactly — same decisions, same scores, same rejections, same wording.
    ///
    /// This is the test that would fail if <c>Prepare</c> and <c>Resolve</c> had drifted apart: a
    /// belief update moved to the wrong side of the split, a decision id allocated in a different
    /// order, an RNG stream drawn twice.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllVariants))]
    public void Auto_resolving_a_controlled_session_reproduces_the_batch_history(string variant)
    {
        var batch = Cast.Build(Seed, variant);
        Runner.Run(batch, End);

        var session = SimulationSession.Start(Seed, variant, Controlled);
        int pauses = 0;
        session.AdvanceTo(End);
        while (session.Status == SessionStatus.AwaitingChoice)
        {
            pauses++;
            session.ResolveAutomatically();
        }

        Assert.True(pauses > 0, $"{Controlled} never reached a deliberation in {variant}, so this proves nothing");
        Assert.Equal(TraceWriter.Render(batch, variant, false), TraceWriter.Render(session.World, variant, false));
    }

    /// <summary>
    /// The stepping pattern is a reading convenience, not an input. Four different ways of getting
    /// from the first day to the ninetieth, with the same player choosing the same way each time,
    /// must land on the same world.
    ///
    /// The policy is deliberately a real choice — the last option in the list, which is a
    /// non-default action almost every time — rather than automatic resolution, so this exercises
    /// the path the player actually uses. It is a pure function of the offered options, so "the
    /// player choices are identical" is true by construction across the four patterns.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllVariants))]
    public void Stepping_and_fast_forward_patterns_agree(string variant)
    {
        string OneCall()
        {
            var s = Play(variant);
            s.AdvanceTo(End);
            Settle(s);
            return TraceWriter.Render(s.World, variant, false);
        }

        string DayByDay()
        {
            var s = Play(variant);
            for (int i = 0; i < Days; i++)
            {
                s.AdvanceDays(1);
                Settle(s);
            }
            return TraceWriter.Render(s.World, variant, false);
        }

        string WeekByWeek()
        {
            var s = Play(variant);
            for (int i = 0; i < 12; i++)
            {
                s.AdvanceDays(7);
                Settle(s);
            }
            s.AdvanceTo(End);
            Settle(s);
            return TraceWriter.Render(s.World, variant, false);
        }

        string EventByEventThenFastForward()
        {
            var s = Play(variant);
            for (int i = 0; i < 25; i++)
            {
                s.StepEvent();
                Settle(s);
            }
            s.AdvanceTo(End);
            Settle(s);
            return TraceWriter.Render(s.World, variant, false);
        }

        string reference = OneCall();
        Assert.Equal(reference, DayByDay());
        Assert.Equal(reference, WeekByWeek());
        Assert.Equal(reference, EventByEventThenFastForward());
    }

    /// <summary>
    /// The single-event control and the fast-forward pull from the same queue. Stepping past a
    /// horizon already reached must not re-run anything, and a fast-forward to a date the calendar
    /// has already passed must do nothing at all.
    /// </summary>
    [Fact]
    public void Advancing_to_a_date_already_reached_does_nothing()
    {
        var session = SimulationSession.Start(Seed, "baseline", controlledCharacterId: null, viewpointCharacterId: Controlled);
        session.AdvanceTo(Cast.Start.AddDays(30));

        string before = TraceWriter.Render(session.World, "baseline", false);
        session.AdvanceTo(Cast.Start.AddDays(30));
        session.AdvanceTo(Cast.Start.AddDays(10));

        Assert.Equal(Cast.Start.AddDays(30), session.Date);
        Assert.Equal(before, TraceWriter.Render(session.World, "baseline", false));
    }

    /// <summary>
    /// The player-facing calendar is not <see cref="World.Now"/>. A fast-forward across quiet days
    /// reaches the date the player asked for even though the world's own clock stopped at the last
    /// thing that actually happened.
    /// </summary>
    [Fact]
    public void The_calendar_reaches_the_date_the_player_asked_for()
    {
        var session = SimulationSession.Start(Seed, "baseline", controlledCharacterId: null, viewpointCharacterId: Controlled);
        session.AdvanceTo(End);

        Assert.Equal(End, session.Date);
        Assert.True(session.World.Now <= End);
        Assert.Equal(End, session.Snapshot().Date);
    }

    // ================================================================= the choice itself

    /// <summary>
    /// When the controlled character stops, what he is offered is exactly what survived his own
    /// redundancy, salience, knowledge, capability and access filters — no more, and in an order
    /// that says nothing.
    ///
    /// Both halves are checked against the production pipeline rather than against a copy of its
    /// rules: the offered ids are compared with <see cref="PreparedDecision.Available"/> derived
    /// from the same run, and the rejected candidates are drawn from the same decision record.
    /// </summary>
    [Fact]
    public void The_pending_decision_offers_only_what_survived_his_own_filters()
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled);
        var pending = RunToFirstPause(session);

        Assert.Equal(Controlled, pending.ActorId);
        Assert.NotEmpty(pending.Options);

        // Ordinal id order, never rank order.
        Assert.Equal(
            pending.Options.Select(o => o.Id).OrderBy(id => id, StringComparer.Ordinal).ToList(),
            pending.Options.Select(o => o.Id).ToList());

        // Resolve it and read the record the pipeline wrote: everything offered was scored, and
        // nothing that was rejected was offered.
        session.Choose(pending.Options[0].Id);

        var record = session.World.Decisions.Last(d => d.ActorId == Controlled);
        var scoredIds = record.Scored.Select(s => s.Candidate.Id).ToHashSet(StringComparer.Ordinal);
        var offeredIds = pending.Options.Select(o => o.Id).ToHashSet(StringComparer.Ordinal);

        Assert.True(scoredIds.SetEquals(offeredIds),
            $"offered [{string.Join(", ", offeredIds)}] against scored [{string.Join(", ", scoredIds)}]");
        Assert.NotEmpty(record.Rejected);
        foreach (var rejected in record.Rejected)
            Assert.DoesNotContain(rejected.Candidate.Id, offeredIds);
    }

    /// <summary>
    /// Ordering by id rather than by rank has to be observable, or the assertion above is satisfied
    /// by coincidence. Somewhere in a full run the option the character would have taken is not the
    /// first one offered — if it always were, the list would be leaking his preference whatever the
    /// sort key claimed.
    /// </summary>
    [Fact]
    public void The_option_the_character_would_have_taken_is_not_always_offered_first()
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled);
        int pauses = 0, topWasFirst = 0;

        session.AdvanceTo(End);
        while (session.Status == SessionStatus.AwaitingChoice)
        {
            var pending = session.Pending!;
            pauses++;

            session.ResolveAutomatically();
            string taken = session.World.Decisions.Last(d => d.ActorId == Controlled).Chosen!.Candidate.Id;
            if (string.Equals(pending.Options[0].Id, taken, StringComparison.Ordinal)) topWasFirst++;
        }

        Assert.True(pauses > 1, "not enough pauses in the run to make this claim");
        Assert.True(topWasFirst < pauses,
            "the preferred option was first in every single list, so the ordering cannot be shown to " +
            "be independent of rank");
    }

    /// <summary>
    /// Choosing something other than the option the character would have taken changes the history,
    /// and the change is attributable: every decision before it is identical, the decision at the
    /// fork records the id the player chose, and what follows diverges from there.
    ///
    /// The comparison walks the decision records rather than the rendered trace, because "identical
    /// up to a point, then different" is the claim, and a hash can only say "different".
    /// </summary>
    [Fact]
    public void Choosing_a_non_default_action_produces_an_attributable_history_change()
    {
        var automatic = SimulationSession.Start(Seed, "baseline", Controlled);
        automatic.AdvanceTo(End);
        while (automatic.Status == SessionStatus.AwaitingChoice) automatic.ResolveAutomatically();

        var chosen = SimulationSession.Start(Seed, "baseline", Controlled);

        // Find the first pause offering a genuine alternative, and take the one he would not have.
        // Every pause before it — if any — is resolved the way he would have resolved it, so the
        // only difference between the two runs is the one choice.
        string? preferred = null;
        int forkIndex = -1;

        chosen.AdvanceTo(End);
        while (chosen.Status == SessionStatus.AwaitingChoice)
        {
            var pending = chosen.Pending!;

            // What he would have preferred, read without resolving: the PreparedDecision the session
            // is holding is the pipeline's own, so this is the production ranking.
            string wanted = PreferredOptionOf(chosen);
            var alternative = forkIndex >= 0
                ? null
                : pending.Options.FirstOrDefault(o => !string.Equals(o.Id, wanted, StringComparison.Ordinal));

            if (alternative is not null)
            {
                preferred = wanted;
                forkIndex = chosen.World.Decisions.Count;
                chosen.Choose(alternative.Id);
            }
            else
            {
                chosen.ResolveAutomatically();
            }
        }

        Assert.True(forkIndex >= 0, "no pause in the whole run offered more than one option");

        var before = automatic.World.Decisions;
        var after = chosen.World.Decisions;

        // Everything up to the fork is the same simulation.
        for (int i = 0; i < forkIndex; i++)
            Assert.Equal(before[i].ChosenActionSignature(), after[i].ChosenActionSignature());

        // The fork itself records the player's choice, and it is not what the character preferred.
        var forked = after[forkIndex];
        Assert.Equal(Controlled, forked.ActorId);
        Assert.NotEqual(preferred, forked.Chosen!.Candidate.Id);
        Assert.NotEqual(before[forkIndex].ChosenActionSignature(), forked.ChosenActionSignature());

        // And the history is a different history from there on.
        Assert.NotEqual(
            string.Join('\n', before.Select(d => d.ChosenActionSignature())),
            string.Join('\n', after.Select(d => d.ChosenActionSignature())));
    }

    /// <summary>
    /// Actor parity, stated as a refusal. There is no route by which a player commits to something
    /// his character could not have taken himself, and the refusal leaves the session exactly where
    /// it was so he can choose again.
    /// </summary>
    [Fact]
    public void An_action_that_was_not_open_to_him_is_refused_and_the_session_stays_usable()
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled);
        var pending = RunToFirstPause(session);
        int decisionsBefore = session.World.Decisions.Count;

        // Something his own filters ruled out at this very deliberation — the strongest form of the
        // claim, because it is an option that genuinely occurred to somebody and was refused, not a
        // string that never named anything.
        Assert.Throws<SimulationInvariantException>(() => session.Choose(RejectedOptionOf(session)));
        Assert.Throws<SimulationInvariantException>(() => session.Choose("no-such-candidate"));

        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
        Assert.Equal(decisionsBefore, session.World.Decisions.Count);

        // The same choice, made again, goes through. Note that resolving it also resumes the
        // fast-forward it interrupted, so other characters decide things too — the claim is that
        // *his* decision was recorded and records what he chose, not that exactly one thing
        // happened.
        session.Choose(pending.Options[0].Id);

        var his = session.World.Decisions[decisionsBefore];
        Assert.Equal(Controlled, his.ActorId);
        Assert.Equal(pending.Options[0].Id, his.Chosen!.Candidate.Id);
    }

    /// <summary>
    /// A half-handled event is not a place time can move through. Otherwise the history would depend
    /// on how long somebody took to answer, which is the frame-rate dependence the determinism
    /// invariants forbid wearing a different hat.
    /// </summary>
    [Fact]
    public void Time_cannot_move_while_a_choice_is_outstanding()
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled);
        RunToFirstPause(session);

        Assert.Throws<InvalidOperationException>(() => session.StepEvent());
        Assert.Throws<InvalidOperationException>(() => session.AdvanceDays(1));
        Assert.Throws<InvalidOperationException>(() => session.AdvanceTo(End));
    }

    /// <summary>A prepared decision commits exactly once.</summary>
    [Fact]
    public void A_prepared_decision_cannot_be_resolved_twice()
    {
        var world = Cast.Build(Seed, "baseline");
        var step = Runner.Step(world, End, Controlled);
        while (step.Status == StepStatus.Advanced) step = Runner.Step(world, End, Controlled);

        Assert.Equal(StepStatus.AwaitingChoice, step.Status);
        var prepared = step.Awaiting!;
        Pipeline.Resolve(prepared, null);
        Assert.Throws<SimulationInvariantException>(() => Pipeline.Resolve(prepared, null));
    }

    /// <summary>
    /// The characters nobody is controlling go on deciding for themselves. Asserted by counting: in
    /// a controlled run, every decision by somebody other than the controlled character is still
    /// made, and the run is not merely Vincent's diary.
    /// </summary>
    [Fact]
    public void Npcs_keep_deciding_while_one_character_is_controlled()
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled);
        session.AdvanceTo(End);
        while (session.Status == SessionStatus.AwaitingChoice) session.ResolveAutomatically();

        var actors = session.World.Decisions.Select(d => d.ActorId).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("salvatore", actors);
        Assert.Contains("tommy", actors);
        Assert.True(actors.Count > 2, "only the controlled character and one other ever decided anything");
    }

    // ================================================================= information safety

    /// <summary>
    /// A fact the viewpoint character does not hold cannot reach him, whether it was planted for the
    /// purpose or is simply something nobody in the scenario knows.
    ///
    /// Two facts, deliberately of different kinds. The planted one is held with full confidence by
    /// another character throughout the run, so any path that reads somebody else's cognition would
    /// surface it. The bakery is the fixture's own designed asymmetry — it really is refusing, the
    /// organisation's takings really are short because of it, and no character holds the claim — so
    /// any path that reads objective world state would surface that one.
    ///
    /// The wording is computed from the production narrator rather than hardcoded, so a renderer
    /// that changed its phrasing cannot slip past a test that pinned the old prose.
    /// </summary>
    [Theory]
    [InlineData("vincent")]
    [InlineData("salvatore")]
    [InlineData("tommy")]
    public void A_hidden_fact_never_reaches_the_player_snapshot(string viewpoint)
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled, viewpoint);

        // Known only to Kane, who talks to nobody in this scenario, and never communicated.
        var planted = new Claim(ClaimKind.PersonHoldsGrievance, "salvatore", "vincent");
        session.World.Get("kane").Cognition.Learn(
            planted, Stance.Knows, 1.0, SourceKind.Participant, "kane", Cast.Start);

        // True of the world and held by nobody: the bakery is refusing and it has never been said.
        var unspoken = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Bakery);

        session.AdvanceTo(End);
        while (session.Status == SessionStatus.AwaitingChoice)
            session.Choose(session.Pending!.Options[^1].Id);

        var who = session.World.Get(viewpoint);
        Assert.False(who.Cognition.Holds(planted), "the planted fact leaked into the character's own head");
        Assert.False(who.Cognition.Holds(unspoken), "the bakery stopped being an unknown, so it proves nothing here");

        string Name(string id) =>
            session.World.Find(id)?.Name ?? session.World.Businesses.GetValueOrDefault(id)?.Name ?? id;

        var snapshot = session.Snapshot();
        string surface = Flatten(snapshot) + "\n" + IntelligenceWriter.Render(snapshot);

        foreach (var hidden in new[] { planted, unspoken })
        {
            Assert.DoesNotContain(PlayerNarration.Describe(hidden, Name), surface, StringComparison.Ordinal);
            Assert.DoesNotContain(snapshot.Known, b => b.Claim.Equals(hidden));
            Assert.DoesNotContain(snapshot.Recent, b => b.Claim.Equals(hidden));
            Assert.DoesNotContain(snapshot.Unsettled, b => b.Claim.Equals(hidden));
            Assert.DoesNotContain(snapshot.Disagreements, d => d.Claim.Equals(hidden));
        }
    }

    /// <summary>
    /// Everything in the snapshot is something the viewpoint character holds, and everyone named in
    /// it is somebody he has heard of. The second half is checked against
    /// <see cref="PlayerView.KnownPeople"/> — the production rule — rather than a copy of it.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllVariants))]
    public void The_snapshot_is_bounded_by_what_the_viewpoint_character_holds(string variant)
    {
        var session = SimulationSession.Start(Seed, variant, controlledCharacterId: null, viewpointCharacterId: "salvatore");
        session.AdvanceTo(End);

        var who = session.World.Get("salvatore");
        var snapshot = session.Snapshot();
        var known = PlayerView.KnownPeople(session.World, who).ToHashSet(StringComparer.Ordinal);

        foreach (var b in snapshot.Known) Assert.True(who.Cognition.Holds(b.Claim));
        foreach (var b in snapshot.Recent) Assert.True(who.Cognition.Holds(b.Claim));
        foreach (var b in snapshot.Unsettled) Assert.True(who.Cognition.Holds(b.Claim));

        foreach (var a in snapshot.Attitudes) Assert.Contains(a.PersonId, known);
        foreach (var p in snapshot.Silent) Assert.Contains(p.Id, known);
        foreach (var d in snapshot.Disagreements)
            foreach (var account in d.Accounts)
                Assert.Contains(account.SourceId, known);

        // Nobody outside the cast's own display names may appear either, and the run must not be
        // vacuous.
        Assert.NotEmpty(snapshot.Known);
    }

    /// <summary>
    /// No number reaches the player. Confidence, trust, fear and grievance severity are all hidden
    /// state, and every phrase the snapshot carries is qualitative.
    ///
    /// The check is that no decimal appears in any phrase the model produced. Dates are carried as
    /// <see cref="DateTime"/> fields and never as text, so a calendar cannot trip this.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllVariants))]
    public void The_snapshot_never_prints_a_model_value(string variant)
    {
        var session = SimulationSession.Start(Seed, variant, controlledCharacterId: null, viewpointCharacterId: Controlled);
        session.AdvanceTo(End);

        var snapshot = session.Snapshot();
        var decimalNumber = new System.Text.RegularExpressions.Regex(@"\d+[.,]\d");

        foreach (var phrase in Phrases(snapshot))
            Assert.False(decimalNumber.IsMatch(phrase),
                $"a player-facing phrase carries a model value: \"{phrase}\"");
    }

    /// <summary>
    /// The boundary itself, asserted structurally: nothing an interface can reach through the
    /// session hands it the world, the truth log, a decision record, a score, a report, or a raw
    /// candidate.
    ///
    /// Reflection rather than reading the Godot sources, because this is the property that matters —
    /// a future UI cannot reach what the public surface does not carry, whatever it tries.
    /// </summary>
    [Fact]
    public void The_session_surface_exposes_no_developer_state()
    {
        Type[] forbidden =
        {
            typeof(World), typeof(WorldEvent), typeof(DecisionRecord), typeof(ScoreBreakdown),
            typeof(ScoreComponent), typeof(PreparedDecision), typeof(Candidate), typeof(Rejection),
            typeof(Report), typeof(InformationRequest), typeof(Character), typeof(Cognition),
            typeof(SocialState), typeof(IRelationship), typeof(StepResult), typeof(Agenda),
        };

        foreach (var surface in new[] { typeof(SimulationSession), typeof(PendingDecision), typeof(PlayerSnapshot) })
        {
            foreach (var member in surface.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (var type in TypesReferencedBy(member))
                    Assert.False(forbidden.Contains(type),
                        $"{surface.Name}.{member.Name} exposes {type.Name} to whatever holds it");
            }
        }
    }

    // ================================================================= helpers

    /// <summary>
    /// A session driven by a fixed, non-default policy: always take the last option in the list.
    ///
    /// A pure function of what is offered, so it is the same "player" whichever order the calendar
    /// is read in — which is what makes the stepping-pattern comparison a comparison of patterns
    /// rather than of policies.
    /// </summary>
    private static SimulationSession Play(string variant)
        => SimulationSession.Start(Seed, variant, Controlled);

    private static void Settle(SimulationSession session)
    {
        while (session.Status == SessionStatus.AwaitingChoice)
            session.Choose(session.Pending!.Options[^1].Id);
    }

    private static PendingDecision RunToFirstPause(SimulationSession session)
    {
        session.AdvanceTo(End);
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
        return session.Pending!;
    }

    /// <summary>
    /// The option the controlled character would have taken, read out of the pipeline's own ranking
    /// without resolving anything.
    ///
    /// Reaches through the session's private state on purpose: this is a developer question, and
    /// answering it through a public API would mean the answer was available to an interface.
    /// </summary>
    private static string PreferredOptionOf(SimulationSession session)
        => PreparedOf(session).Scored[0].Candidate.Id;

    /// <summary>
    /// An option this deliberation actually generated and then refused, for whatever reason. Taken
    /// from the production filter's own rejection list rather than invented.
    /// </summary>
    private static string RejectedOptionOf(SimulationSession session)
    {
        var prepared = PreparedOf(session);
        var offered = prepared.Available.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        var generated = prepared.Generated.FirstOrDefault(c => !offered.Contains(c.Id));

        Assert.NotNull(generated);
        return generated!.Id;
    }

    private static PreparedDecision PreparedOf(SimulationSession session)
    {
        var field = typeof(SimulationSession)
            .GetField("_prepared", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (PreparedDecision)field.GetValue(session)!;
    }

    private static IEnumerable<string> Phrases(PlayerSnapshot s)
    {
        foreach (var b in s.Known.Concat(s.Recent).Concat(s.Unsettled))
        {
            yield return b.Statement;
            yield return b.Confidence;
            yield return b.Attribution;
        }

        foreach (var d in s.Disagreements)
        {
            yield return d.Statement;
            if (d.OwnBasis is { } basis) yield return basis;
            foreach (var a in d.Accounts) yield return a.SourceName;
        }

        foreach (var a in s.Attitudes)
        {
            yield return a.PersonName;
            yield return a.Standing;
            if (a.Wariness is { } w) yield return w;
            foreach (var g in a.Grievances) yield return g;
        }

        foreach (var p in s.Silent) yield return p.Name;
    }

    private static string Flatten(PlayerSnapshot s)
        => string.Join('\n', Phrases(s));

    private static IEnumerable<Type> TypesReferencedBy(MemberInfo member) => member switch
    {
        PropertyInfo p => Unwrap(p.PropertyType),
        FieldInfo f => Unwrap(f.FieldType),
        MethodInfo m => Unwrap(m.ReturnType).Concat(m.GetParameters().SelectMany(x => Unwrap(x.ParameterType))),
        ConstructorInfo c => c.GetParameters().SelectMany(x => Unwrap(x.ParameterType)),
        _ => Array.Empty<Type>(),
    };

    private static IEnumerable<Type> Unwrap(Type t)
    {
        yield return t;
        if (t.IsGenericType)
            foreach (var arg in t.GetGenericArguments())
                foreach (var inner in Unwrap(arg))
                    yield return inner;
        if (t.IsArray && t.GetElementType() is { } element)
            foreach (var inner in Unwrap(element))
                yield return inner;
    }
}
