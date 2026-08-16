using System.Reflection;
using System.Text;
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

        // Ordinal candidate-id order, never rank order. Asserted against the candidates rather than
        // against the tokens the player sees: the tokens are hashes and carry no order of their own,
        // which is the whole point of them, so the neutrality of the ordering is a property of what
        // they stand for. That the order is independent of rank is asserted separately, by
        // The_option_the_character_would_have_taken_is_not_always_offered_first.
        var available = PreparedOf(session).Available.Select(c => c.Id).ToList();
        Assert.Equal(available.OrderBy(id => id, StringComparer.Ordinal).ToList(), available);
        Assert.Equal(available.Count, pending.Options.Count);

        // Resolve it and read the record the pipeline wrote: everything offered was scored, and
        // nothing that was rejected was offered.
        session.Choose(pending.Options[0].Id);

        var record = session.World.Decisions.Last(d => d.ActorId == Controlled);
        var scoredIds = record.Scored.Select(s => s.Candidate.Id).ToHashSet(StringComparer.Ordinal);
        var offeredIds = available.ToHashSet(StringComparer.Ordinal);

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

        // Options carry opaque tokens, so the raw candidate id is not a key the session accepts —
        // and neither is anything else that was not offered.
        string rejectedCandidateId = RejectedOptionOf(session);
        Assert.Throws<SimulationInvariantException>(() => session.Choose(rejectedCandidateId));
        Assert.Throws<SimulationInvariantException>(() => session.Choose("no-such-option"));

        // And the guarantee that matters underneath it: the pipeline itself refuses a candidate its
        // own filters ruled out at this very deliberation. Asserted against `Pipeline.Resolve`
        // directly, because that is where actor parity is enforced — the session's token map is an
        // indirection in front of it, not a second answer to the same question.
        Assert.Throws<SimulationInvariantException>(
            () => Pipeline.Resolve(PreparedOf(session), rejectedCandidateId));

        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
        Assert.Equal(decisionsBefore, session.World.Decisions.Count);

        // The same choice, made again, goes through. Note that resolving it also resumes the
        // fast-forward it interrupted, so other characters decide things too — the claim is that
        // *his* decision was recorded and records what he chose, not that exactly one thing
        // happened.
        var available = PreparedOf(session).Available.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        session.Choose(pending.Options[0].Id);

        var his = session.World.Decisions[decisionsBefore];
        Assert.Equal(Controlled, his.ActorId);
        Assert.Contains(his.Chosen!.Candidate.Id, available);
    }

    /// <summary>
    /// The option token is a handle, not a name. It must reveal nothing about the candidate — in
    /// particular not the truth-log <c>EventId</c> that <c>Claim.ToString()</c> prints into candidate
    /// ids — and it must be stable, because a token the player pressed has to mean the same thing on
    /// an identical replay.
    /// </summary>
    [Fact]
    public void Option_tokens_are_opaque_and_stable()
    {
        var first = SimulationSession.Start(Seed, "baseline", Controlled);
        var second = SimulationSession.Start(Seed, "baseline", Controlled);

        var a = RunToFirstPause(first);
        var b = RunToFirstPause(second);

        Assert.Equal(a.Options.Select(o => o.Id), b.Options.Select(o => o.Id));

        var candidateIds = PreparedOf(first).Available.Select(c => c.Id).ToList();
        Assert.NotEmpty(candidateIds);

        foreach (var option in a.Options)
        {
            Assert.DoesNotContain(option.Id, candidateIds);
            Assert.Matches("^[0-9a-f]{12}$", option.Id);
        }
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
    /// <b>Every player-facing surface is checked, not only the snapshot at the end.</b> Milestone
    /// 009's review found the leak in the pending decision rather than in the snapshot, so the
    /// transcript below accumulates every pending decision the run produces — occasion, focus and
    /// every option's wording — alongside a snapshot taken at each pause and at the end.
    [Theory]
    [InlineData("vincent")]
    [InlineData("salvatore")]
    [InlineData("tommy")]
    public void A_hidden_fact_never_reaches_any_player_surface(string viewpoint)
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled, viewpoint);

        // Known only to Kane, who talks to nobody in this scenario, and never communicated.
        var planted = new Claim(ClaimKind.PersonHoldsGrievance, "salvatore", "vincent");
        session.World.Get("kane").Cognition.Learn(
            planted, Stance.Knows, 1.0, SourceKind.Participant, "kane", Cast.Start);

        // True of the world and held by nobody: the bakery is refusing and it has never been said.
        var unspoken = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Bakery);

        var surface = new StringBuilder();
        var claimsShown = new List<PlayerClaim>();

        void Capture()
        {
            var snap = session.Snapshot();
            surface.AppendLine(Flatten(snap));
            surface.AppendLine(IntelligenceWriter.Render(snap));
            claimsShown.AddRange(snap.Known.Concat(snap.Recent).Concat(snap.Unsettled).Select(b => b.Claim));
            claimsShown.AddRange(snap.Disagreements.Select(d => d.Claim));

            if (session.Pending is { } pending) surface.AppendLine(Flatten(pending));
        }

        Capture();
        session.AdvanceTo(End);
        while (session.Status == SessionStatus.AwaitingChoice)
        {
            Capture();
            session.Choose(session.Pending!.Options[^1].Id);
        }
        Capture();

        var who = session.World.Get(viewpoint);
        Assert.False(who.Cognition.Holds(planted), "the planted fact leaked into the character's own head");
        Assert.False(who.Cognition.Holds(unspoken), "the bakery stopped being an unknown, so it proves nothing here");

        string Name(string id) =>
            session.World.Find(id)?.Name ?? session.World.Businesses.GetValueOrDefault(id)?.Name ?? id;

        string text = surface.ToString();
        foreach (var hidden in new[] { planted, unspoken })
        {
            Assert.DoesNotContain(PlayerNarration.Describe(hidden, Name), text, StringComparison.Ordinal);
            Assert.DoesNotContain(claimsShown, c => c.Matches(hidden));
        }
    }

    /// <summary>
    /// The P1 finding, as a staged proof: an owner whose delegated operation fails learns nothing
    /// from being woken about it.
    ///
    /// <c>Strategies.Blocked</c> schedules <see cref="EventKind.StrategyBlocked"/> addressed to the
    /// strategy's owner, with the cause "Bellini's grocery held out against force" — the executor's
    /// operational outcome, written by the scheduler, for a man who was not there and has been told
    /// nothing. Milestone 009 shipped that string straight into the pending decision as its occasion,
    /// and into its focus as well, because <c>AgendaSelection</c> sets a RespondToTrigger agenda's
    /// description to the trigger cause verbatim.
    ///
    /// Staged rather than taken from a natural run, because the case has to be exercised on demand
    /// and with the owner and executor definitely distinct — and it drives the real
    /// <see cref="Runner.Step"/> and the real projection, not a reconstruction of them.
    /// </summary>
    [Fact]
    public void A_delegated_failure_tells_its_owner_nothing_until_somebody_does()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");

        // He set it going and handed the execution to Tommy. He is the owner; Tommy is the one there.
        vincent.Execution.Strategy = new StrategyInstance
        {
            OwnerId = vincent.Id,
            LocalSequence = vincent.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            Method = CoercionMethod.Force,
            StartedAt = world.Now,
            Deadline = world.Now.AddDays(30),
            DelegatedToId = "tommy",
        };
        vincent.Execution.RecordDelegation("tommy");

        const string leak = "Bellini's grocery held out against force";
        var blocked = world.Queue.Schedule(
            world.Now, EventKind.StrategyBlocked, vincent.Id, leak,
            new EventPayload { TargetId = Cast.Grocery, Strategy = StrategyKind.SecureTribute });

        // Drive the real loop until that event is the one being handled.
        StepResult step;
        do
        {
            step = Runner.Step(world, world.Now.AddDays(1), Controlled);
            Assert.NotEqual(StepStatus.Exhausted, step.Status);
        }
        while (step.Event?.Id != blocked.Id);

        Assert.Equal(StepStatus.AwaitingChoice, step.Status);
        var pending = SimulationSession.Project(
            step.Awaiting!,
            new Dictionary<string, string>(StringComparer.Ordinal),
            id => world.Find(id)?.Name ?? world.Businesses.GetValueOrDefault(id)?.Name ?? id);

        // He was not there, nobody has told him, and no discovery roll was made — so the interface
        // says nothing about why he is thinking, rather than reading him the scheduler's sentence.
        Assert.Null(pending.Occasion);
        Assert.Null(pending.Focus);
        Assert.DoesNotContain(leak, Flatten(pending), StringComparison.Ordinal);
        Assert.DoesNotContain("held out", Flatten(pending), StringComparison.OrdinalIgnoreCase);

        // And the same for what he can be shown of the world.
        var snapshot = PlayerView.Build(world, vincent.Id, world.Now);
        Assert.DoesNotContain(leak, Flatten(snapshot) + IntelligenceWriter.Render(snapshot), StringComparison.Ordinal);
    }

    /// <summary>
    /// The same claim for a completion, which is the harder half: <c>Strategies.Complete</c> clears
    /// the owner's strategy before the event is handled, so "did he execute it himself" is not a
    /// question the projection can still answer. That is why the rule is silence for both kinds
    /// rather than a condition evaluated per kind — two rules for one question is how the
    /// distinction gets dropped between them.
    /// </summary>
    [Fact]
    public void A_delegated_completion_tells_its_owner_nothing_either()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");

        const string leak = "SecureTribute finished: the cleanup made things worse";
        var complete = world.Queue.Schedule(
            world.Now, EventKind.StrategyComplete, vincent.Id, leak,
            new EventPayload { Strategy = StrategyKind.ConcealIncident, Note = "the cleanup made things worse" });

        StepResult step;
        do
        {
            step = Runner.Step(world, world.Now.AddDays(1), Controlled);
            Assert.NotEqual(StepStatus.Exhausted, step.Status);
        }
        while (step.Event?.Id != complete.Id);

        Assert.Equal(StepStatus.AwaitingChoice, step.Status);
        var pending = SimulationSession.Project(
            step.Awaiting!,
            new Dictionary<string, string>(StringComparer.Ordinal),
            id => world.Find(id)?.Name ?? id);

        Assert.Null(pending.Occasion);
        Assert.Null(pending.Focus);
        Assert.DoesNotContain("cleanup", Flatten(pending), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The structural version of the finding, over natural runs: <b>no string a scheduler or a
    /// generator authored reaches a player-facing surface</b>.
    ///
    /// Every decision record carries the exact cause text of the event that woke it, so a full run
    /// yields the whole vocabulary of authored trigger causes actually used — and the assertion is
    /// against that, not against a hand-written list of the ones somebody thought of. The same for
    /// candidate descriptions, which are the generators' developer wording.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllVariants))]
    public void No_authored_developer_string_reaches_a_player_surface(string variant)
    {
        var session = SimulationSession.Start(Seed, variant, Controlled);
        var surface = new StringBuilder();

        session.AdvanceTo(End);
        while (session.Status == SessionStatus.AwaitingChoice)
        {
            surface.AppendLine(Flatten(session.Pending!));
            session.ResolveAutomatically();
        }
        surface.AppendLine(Flatten(session.Snapshot()));

        string text = surface.ToString();
        Assert.NotEqual(0, text.Length);

        var authored = session.World.Decisions.Select(d => d.Trigger)
            .Concat(session.World.Decisions.SelectMany(d => d.Generated).Select(c => c.Description))
            .Concat(session.World.Decisions.SelectMany(d => d.Rejected).Select(r => r.Reason))
            .Concat(session.World.Decisions.Select(d => d.Agenda.Reason))
            .Where(s => s.Length > 12)
            .Distinct(StringComparer.Ordinal);

        foreach (var developerText in authored)
            Assert.DoesNotContain(developerText, text, StringComparison.Ordinal);
    }

    /// <summary>
    /// The occasion vocabulary is closed and fails closed. Anything not explicitly admitted — which
    /// includes both strategy-outcome kinds today, and any event kind added later — produces no
    /// occasion at all, so a new kind is mute until somebody decides what a character necessarily
    /// knows about it.
    /// </summary>
    [Fact]
    public void The_occasion_vocabulary_is_closed_and_silent_by_default()
    {
        var admitted = new[]
        {
            EventKind.AssignmentDelivered, EventKind.RoleReview,
            EventKind.Incident, EventKind.PressureThreshold,
        };

        foreach (var kind in Enum.GetValues<EventKind>())
        {
            string? occasion = PlayerOccasion.For(kind);
            if (admitted.Contains(kind)) Assert.False(string.IsNullOrEmpty(occasion), $"{kind} lost its occasion");
            else Assert.Null(occasion);
        }

        Assert.Null(PlayerOccasion.For(EventKind.StrategyBlocked));
        Assert.Null(PlayerOccasion.For(EventKind.StrategyComplete));

        // A RespondToTrigger agenda's description *is* the trigger cause, so it never passes even on
        // an admitted kind — the same value arriving through a second field is the shape this whole
        // correction is about.
        var respond = new Agenda(AgendaKind.RespondToTrigger, "some authored cause", "why");
        Assert.Null(PlayerOccasion.Focus(respond, EventKind.Incident));

        var assignment = new Agenda(AgendaKind.FulfilAssignment, "restore the harbour tribute", "why");
        Assert.Equal("restore the harbour tribute", PlayerOccasion.Focus(assignment, EventKind.AssignmentDelivered));
        Assert.Null(PlayerOccasion.Focus(assignment, EventKind.StrategyBlocked));
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

        // Matched on the predicate, since the boundary drops the truth-log counter. A held record
        // whose predicate matches is the strongest statement available here, and it is the one that
        // matters: the claim is his.
        bool HeHolds(PlayerClaim shown)
            => who.Cognition.Records.Any(r => r.IsHeld && shown.Matches(r.Claim));

        foreach (var b in snapshot.Known) Assert.True(HeHolds(b.Claim));
        foreach (var b in snapshot.Recent) Assert.True(HeHolds(b.Claim));
        foreach (var b in snapshot.Unsettled) Assert.True(HeHolds(b.Claim));

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
            typeof(Report), typeof(ReportedClaim), typeof(InformationRequest), typeof(Character),
            typeof(CharacterView), typeof(Cognition), typeof(SocialState), typeof(IRelationship),
            typeof(StepResult), typeof(ScheduledEvent), typeof(EventPayload), typeof(Agenda),
            typeof(Psychology), typeof(InformationRecord), typeof(Testimony),

            // Milestone 009's second correction. Claim carries EventId — a truth-log counter with no
            // player-facing meaning — so the boundary hands out PlayerClaim instead.
            typeof(Claim),
        };

        // Recursive, because a forbidden type one hop further in is just as reachable. The first
        // version of this test walked one level and would have passed with `Claim` sitting on
        // PlayerBelief, which is exactly where it was.
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>(new[] { typeof(SimulationSession), typeof(PendingDecision), typeof(PlayerSnapshot) });

        while (queue.Count > 0)
        {
            var surface = queue.Dequeue();
            if (!seen.Add(surface)) continue;

            foreach (var member in surface.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (var type in TypesReferencedBy(member))
                {
                    Assert.False(forbidden.Contains(type),
                        $"{surface.Name}.{member.Name} exposes {type.Name} to whatever holds it");

                    // Follow anything of ours; the framework's own types are not the risk.
                    if (type.Assembly == typeof(SimulationSession).Assembly) queue.Enqueue(type);
                }
            }
        }

        // The walk has to have actually gone somewhere, or an empty crawl would pass silently.
        Assert.Contains(typeof(PlayerBelief), seen);
        Assert.Contains(typeof(PlayerAttitude), seen);
        Assert.Contains(typeof(PendingOption), seen);
    }

    /// <summary>
    /// The DTOs are immutable in fact, not by politeness.
    ///
    /// An <c>IReadOnlyList&lt;T&gt;</c> backed by a <c>List&lt;T&gt;</c> can be cast straight back and
    /// mutated — the same defect milestone 006 fixed on relationship grievances, found again on the
    /// player boundary. Checked against real instances rather than declared types, because the
    /// declared type is exactly what is honest and the runtime type is what is not.
    /// </summary>
    [Fact]
    public void Every_collection_on_the_player_boundary_is_genuinely_read_only()
    {
        var session = SimulationSession.Start(Seed, "baseline", Controlled);
        var pending = RunToFirstPause(session);
        var snapshot = session.Snapshot();

        int checkedCollections = 0;

        void Walk(object? value, string path)
        {
            if (value is null || value is string) return;
            var type = value.GetType();

            if (value is System.Collections.IEnumerable sequence)
            {
                checkedCollections++;
                Assert.False(value is System.Collections.IList { IsReadOnly: false },
                    $"{path} is a {type.Name}, which a consumer can cast back and mutate");
                Assert.False(type.IsArray, $"{path} is an array, whose elements can be replaced");

                foreach (var item in sequence) Walk(item, $"{path}[]");
                return;
            }

            if (type.Assembly != typeof(SimulationSession).Assembly) return;

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (property.GetIndexParameters().Length == 0)
                    Walk(property.GetValue(value), $"{path}.{property.Name}");
        }

        Walk(snapshot, nameof(PlayerSnapshot));
        Walk(pending, nameof(PendingDecision));

        Assert.True(checkedCollections >= 8,
            $"only {checkedCollections} collections were reached, so this proves very little");
    }

    // ================================================================= belief-limited targets

    /// <summary>
    /// A corroboration target has to be somebody the actor has heard of.
    ///
    /// The staged regression milestone 009's second review asked for. An organisation member nobody
    /// has heard of is added to the cast, with an id that sorts <b>before</b> the legitimate
    /// alternative — so the generator's own <c>OrderBy(id)</c> would pick him if the belief limit
    /// were absent, which is what makes this a proof rather than a coincidence. The world is built
    /// here rather than by changing the scenario: <c>Cast</c> is untouched and the six-character
    /// fixture is unchanged.
    /// </summary>
    [Fact]
    public void A_corroboration_target_he_has_never_heard_of_is_not_generated_or_rendered()
    {
        var world = Cast.Build(Seed, "baseline");
        var salvatore = world.Get("salvatore");
        var stranger = AddStranger(world);

        // Without the belief limit this man would win outright: he is on the roster, and he sorts
        // first. If either stops being true the test has stopped proving anything.
        var roster = Pipeline.OrgMembersOf(world, salvatore);
        Assert.Contains(stranger.Id, roster);
        Assert.Equal(stranger.Id, roster.OrderBy(id => id, StringComparer.Ordinal).First());

        // And nothing in Salvatore's head names him.
        Assert.DoesNotContain(stranger.Id, Acquaintance.HeardOf(world, salvatore));

        var prepared = Pipeline.Prepare(world, salvatore, Wake(world, salvatore));
        var corroborations = prepared.Generated
            .Where(c => c.Kind == ActionKind.SeekCorroboration)
            .ToList();

        Assert.NotEmpty(corroborations);
        Assert.DoesNotContain(corroborations, c => c.TargetId == stranger.Id);

        // The known alternative is still eligible. A filter that removed the option entirely would
        // be a correctness fix that narrows what can be expressed, which is its own defect.
        Assert.Contains(corroborations, c => c.TargetId == "vincent");

        // And nothing renders his name, which is the half that reaches a person.
        var pending = SimulationSession.Project(
            prepared,
            new Dictionary<string, string>(StringComparer.Ordinal),
            id => world.Find(id)?.Name ?? world.Businesses.GetValueOrDefault(id)?.Name ?? id);

        Assert.DoesNotContain(stranger.Name, Flatten(pending), StringComparison.Ordinal);
        Assert.DoesNotContain(stranger.Id, Flatten(pending), StringComparison.Ordinal);

        var snapshot = PlayerView.Build(world, salvatore.Id, world.Now);
        Assert.DoesNotContain(stranger.Name, Flatten(snapshot), StringComparison.Ordinal);
        Assert.DoesNotContain(stranger.Id, snapshot.Silent.Select(p => p.Id));
    }

    /// <summary>
    /// The other direction, and it matters as much: hearing of somebody makes him eligible.
    ///
    /// A filter that simply removed strangers forever would be the ledger's first recurring pattern —
    /// a correctness fix that narrows what can be expressed. The same man, once Salvatore holds a
    /// claim naming him, becomes somebody he can go and ask.
    /// </summary>
    [Fact]
    public void Hearing_of_somebody_makes_him_a_target_he_can_approach()
    {
        var world = Cast.Build(Seed, "baseline");
        var salvatore = world.Get("salvatore");
        var stranger = AddStranger(world);

        // Somebody mentions him. That is all it takes to have heard of a man.
        salvatore.Cognition.Learn(
            new Claim(ClaimKind.PersonUsedViolence, stranger.Id, Cast.Grocery),
            Stance.Suspects, 0.4, SourceKind.Report, "vincent", Cast.Start);

        Assert.Contains(stranger.Id, Acquaintance.HeardOf(world, salvatore));

        var prepared = Pipeline.Prepare(world, salvatore, Wake(world, salvatore));
        Assert.Contains(
            prepared.Generated.Where(c => c.Kind == ActionKind.SeekCorroboration),
            c => c.TargetId == stranger.Id);
    }

    /// <summary>
    /// An office relationship is knowledge too. A soldier who has exchanged no words with anybody
    /// still knows who his capo is, and the belief limit must not make him unable to name his own
    /// superior — <c>Inference</c> already treats who holds which office in one's own organisation as
    /// an institutional fact a member is party to.
    /// </summary>
    [Fact]
    public void Office_relationships_count_as_knowing_somebody()
    {
        var world = Cast.Build(Seed, "baseline");
        var tommy = world.Get("tommy");

        var reachable = Acquaintance.CouldApproach(
            world, tommy, Pipeline.SuperiorOf(world, tommy), Pipeline.SubordinatesOf(world, tommy));

        Assert.Contains("vincent", reachable);
        Assert.DoesNotContain(tommy.Id, reachable);

        // And it is a widening of what he has heard of, never a replacement for it.
        foreach (var id in Acquaintance.HeardOf(world, tommy)) Assert.Contains(id, reachable);
    }

    /// <summary>
    /// The structural version, over natural runs: every question anybody puts in the accepted
    /// scenario goes to somebody that character could actually name.
    ///
    /// Checked against the recorded requests rather than against candidates, because a request is
    /// the act itself — and <b>at the moment each one is made</b>, stepping the run event by event,
    /// rather than against the asker's acquaintance set at the end.
    ///
    /// The first version of this test did check at the end and was very nearly vacuous: answering a
    /// question puts the answerer's testimony in the asker's head, so a man he had never heard of
    /// when he asked is a man he has heard of by the time the run finishes. It passed with the belief
    /// limit removed. A test that cannot fail is the ledger's false-assurance pattern, and this one
    /// was an instance of it until the loop below was stepped.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllVariants))]
    public void Nobody_ever_puts_a_question_to_a_stranger(string variant)
    {
        var world = Cast.Build(Seed, variant);
        int checkedSoFar = 0;

        void CheckNewRequests()
        {
            for (; checkedSoFar < world.Requests.Count; checkedSoFar++)
            {
                var request = world.Requests[checkedSoFar];
                var asker = world.Get(request.AskerId);
                var reachable = Acquaintance.CouldApproach(
                    world, asker,
                    Pipeline.SuperiorOf(world, asker),
                    Pipeline.SubordinatesOf(world, asker));

                Assert.True(reachable.Contains(request.AskedId),
                    $"{request.AskerId} put a question to {request.AskedId} on {request.At:yyyy-MM-dd} " +
                    "with nothing in his head establishing that man exists");
            }
        }

        while (Runner.Step(world, End, controlledCharacterId: null).Status == StepStatus.Advanced)
            CheckNewRequests();

        CheckNewRequests();
        Assert.NotEmpty(world.Requests);
    }

    /// <summary>
    /// One derivation, two readers. The player view and candidate generation must agree about who a
    /// character has heard of, because they are asking the same question — and before milestone
    /// 009's second correction they did not.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllVariants))]
    public void The_player_view_and_the_generators_agree_about_who_he_has_heard_of(string variant)
    {
        var world = Cast.Build(Seed, variant);
        Runner.Run(world, End);

        foreach (var who in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
            Assert.Equal(
                PlayerView.KnownPeople(world, who),
                Acquaintance.HeardOf(world, who));
    }

    /// <summary>
    /// An organisation member nobody has heard of, whose id sorts before every other member's.
    ///
    /// Added to the world the test built, never to <c>Cast</c>. The six-character scenario fixture is
    /// unchanged and the ceiling on it is untouched.
    /// </summary>
    private static Character AddStranger(World world)
    {
        var stranger = new Character
        {
            Id = "aldo-stranger",
            Name = "Aldo Stranger",
            RoleTitle = "soldier",
            Capabilities = new Capabilities(
                new Dictionary<Skill, double> { [Skill.Coercion] = 0.4 },
                crew: 1, cash: 100, authority: 1, districts: new[] { Cast.Harbour }),
            Psychology = new Psychology(
                new Dictionary<Trait, double> { [Trait.Cautious] = 0.5 },
                new Dictionary<Drive, double> { [Drive.Belonging] = 0.5 }),
        };

        stranger.Social.OrganizationId = Cast.OrgId;
        world.Characters[stranger.Id] = stranger;
        return stranger;
    }

    /// <summary>A plain wake, for staging a deliberation without running a whole scenario.</summary>
    private static ScheduledEvent Wake(World world, Character actor) => new()
    {
        Id = 9_000,
        Time = world.Now,
        Kind = EventKind.RoleReview,
        OwnerId = actor.Id,
        Cause = "staged: his patch came up for review",
    };

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

    /// <summary>Every string a pending decision puts in front of the player.</summary>
    private static string Flatten(PendingDecision d)
        => string.Join('\n', new[] { d.ActorName, d.ActorRole, d.Occasion, d.Focus }
            .Concat(d.Options.Select(o => o.Description))
            .Where(s => !string.IsNullOrEmpty(s)));

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
