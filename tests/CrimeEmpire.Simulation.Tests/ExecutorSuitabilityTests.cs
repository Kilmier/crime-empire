using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Strategy;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 020 — The Right Person for the Job. `Generators.FromRelationship` used to delegate to
/// the single highest-trust subordinate available — deterministically Tommy, Vincent's only
/// organisational subordinate — with nothing about who would actually do the job better entering
/// the choice (`ROADMAP.md`, "Executor suitability/capability is not modelled"). The new
/// `capable-angelo` variant gives Vincent a second subordinate — Angelo Conti, harder-hitting than
/// Tommy (Coercion 0.80 vs. 0.55) but far less trusted (Vincent's trust in him is 0.35 against
/// Tommy's 0.70) — so the fork is a genuine comparison rather than a foregone one.
///
/// Three production changes, each minimal and each proven here:
///
///   1. <c>FromRelationship</c> now yields one <see cref="ActionKind.DelegateStrategy"/> candidate
///      per subordinate rather than pre-selecting one. With exactly one subordinate (every existing
///      accepted variant) this degenerates to exactly the one candidate it always produced.
///   2. <see cref="Utility"/> gained one new score component, "executor capability", reading
///      <see cref="Candidate.ExecutorCoercion"/> — set only when there are two or more subordinates
///      to compare, so it is <c>null</c> and never emitted for every existing accepted variant.
///   3. <see cref="Strategies.ResolveViolence"/> now scales its resistance reduction by the actual
///      executor's Coercion, calibrated so it reduces to exactly the old flat <c>0.3</c> at Tommy's
///      own Coercion (0.55) — the only value that call has ever been exercised against in an
///      accepted run — so no existing trace hash moves.
///
/// <b>Correction (Codex P1, `f468e19`'s review).</b> <c>ExecutorCoercion</c> originally came from
/// <c>GeneratorContext.SubordinateCoercion</c>, a dictionary <c>Pipeline.Prepare</c> built by
/// reading <c>world.Get(id).Capabilities[Skill.Coercion]</c> straight off <see cref="Sim.World"/> —
/// the objective figure, not anything Vincent holds. Codex found that violates the same rule every
/// other score component in <c>Decision/Utility.cs</c> obeys (its own file header: "Note the
/// signature of <c>Score</c>: it receives a <c>PerceivedSituation</c> and never a <c>World</c>"):
/// <c>Pipeline.SubordinatesOf</c> reading the roster to learn *who* reports to Vincent is a
/// legitimate authority scan (`DESIGN_DECISIONS.md`'s "office relationship" ruling), but *how good*
/// that man is at the job is a fact about him, not the org chart, and nothing licensed reading it
/// past the belief limit every other relationship dimension already respects. The fix: a new
/// <see cref="Domain.Relations.AssessedCoercion"/> dimension on <see cref="Domain.IRelationship"/>,
/// alongside <c>Trust</c>/<c>Obligation</c>/<c>Fear</c> — Vincent's own held belief about each
/// subordinate's Coercion, set at scenario construction to match the pre-correction figures exactly
/// (Tommy 0.55 in <c>Cast.Build</c>, Angelo 0.80 in <c>Variants.Apply</c>) so every accepted trace
/// hash and the natural run's own preference are unmoved. <c>Generators.FromRelationship</c> now
/// reads <c>ctx.Actor.Social.Toward(sub).AssessedCoercion</c> — the actor's own relationship record,
/// exactly the channel <see cref="Utility.Loyalty"/> already reads — and <c>Pipeline.Prepare</c> no
/// longer touches <c>Capabilities</c> for anyone but the actor himself. Four new tests below prove
/// the assessment, not the objective figure, is what scoring reads, in both directions, and that a
/// missing assessment neither falls back to <c>World</c> nor silently scores as zero.
///
/// <b>What is deliberately not re-proven here.</b> Controlling every character (including Angelo)
/// with every pause auto-resolved, reproducing the fully autonomous history, and viewpoint identity
/// never changing simulation history are already covered — for `capable-angelo` specifically, not
/// merely in principle — by <c>ControlledAutonomousParityTests</c>'s and
/// <c>PlayerSessionTests</c>'s existing sweeps, both of which iterate <see cref="Variants.All"/>
/// generically. Adding <c>capable-angelo</c> to that array is what extends them; no new test code
/// was needed, and the full suite was confirmed still green after the addition. The same is true of
/// the "no player-facing phrase carries a decimal" regression. What remains genuinely new — the
/// fork itself, eligibility, the capability score's presence and absence, the executor-scaled force
/// outcome, save/load through the new fork, and a targeted check that the new score vocabulary
/// specifically never reaches a player surface — is what follows.
/// </summary>
public sealed class ExecutorSuitabilityTests
{
    private const int Seed = 42;
    private const string Variant = "capable-angelo";
    private const string Baseline = "baseline";
    private const string Vincent = "vincent";
    private const string Tommy = "tommy";
    private const string Angelo = "angelo";

    private static DateTime End => Cast.Start.AddDays(90);

    // Independently pinned, matching DirectActionVsDelegationTests' own copy, per this project's
    // practice of not sharing the same constant across files that check the same assumption.
    private const string StartPersuade = "persuade Bellini's grocery to pay";
    private const string CarryOn = "carry on getting Bellini's grocery to pay";
    private const string DelegateToTommy = "hand it to Tommy Nardo";
    private const string DelegateToAngelo = "hand it to Angelo Conti";

    // ================================================================= natural + eligibility

    /// <summary>
    /// The executable feature claim: Vincent's fork now offers continuation and delegation to
    /// <em>either</em> subordinate together, in the same <see cref="PendingDecision"/>. Direct
    /// falsifier of mutation check 1 (highest-trust-only generation): reverting
    /// <c>FromRelationship</c> to its pre-020 <c>OrderByDescending(trust).First()</c> would offer
    /// only Tommy (0.70 trust beats Angelo's 0.35), and this fails.
    /// </summary>
    [Fact]
    public void Vincents_fork_offers_continuation_and_both_subordinates_together()
    {
        var session = SimulationSession.Start(Seed, Variant, Vincent);
        var pending = ReachFork(session, End);

        var descriptions = pending.Options.Select(o => o.Description).ToList();
        Assert.Contains(CarryOn, descriptions);
        Assert.Contains(DelegateToTommy, descriptions);
        Assert.Contains(DelegateToAngelo, descriptions);
    }

    /// <summary>
    /// Eligibility: both delegate candidates survive <see cref="Filters"/> — salience, knowledge,
    /// capability, access — independently, and both appear in
    /// <see cref="PreparedDecision.Available"/>, not merely among what was generated. Read through
    /// the real production pipeline (<see cref="Runner.Step"/>/<see cref="Pipeline.Resolve"/>), not
    /// a staged candidate set.
    /// </summary>
    [Fact]
    public void Both_subordinates_are_independently_eligible_at_the_fork()
    {
        var world = Cast.Build(Seed, Variant);
        var prepared = AdvanceToVincentsFork(world);

        var delegateCandidates = prepared.Available
            .Where(c => c.Kind == ActionKind.DelegateStrategy)
            .ToList();
        Assert.Equal(2, delegateCandidates.Count);
        Assert.Contains(delegateCandidates, c => c.TargetId == Tommy);
        Assert.Contains(delegateCandidates, c => c.TargetId == Angelo);

        // Neither is a stray rejection dressed up as availability — both are absent from Rejected.
        Assert.DoesNotContain(prepared.Rejected, r => r.Candidate.Kind == ActionKind.DelegateStrategy);
    }

    // ================================================================= acquaintance boundary (Correction 2)

    /// <summary>
    /// Codex P1 on `436f6c7`. <c>GeneratorContext.SubordinateIds</c> is <c>Pipeline.SubordinatesOf</c>'s
    /// own authority scan — a legitimate organisational fact for identity ("who is my subordinate"),
    /// per <c>DESIGN_DECISIONS.md</c>'s "an office relationship is only an office relationship if it
    /// comes from an office" ruling — but not itself grounds to treat that man as somebody Vincent
    /// could name. <c>Acquaintance.KnownTo</c>'s own header says so directly: "a soldier holding no
    /// office is therefore not knowable this way, however senior he is." A subordinate present in
    /// <c>SubordinateIds</c> but absent from <c>ctx.AcquaintedIds</c> — nobody has ever put him in
    /// Vincent's head, and he holds no named office — must not be offered as a delegate.
    ///
    /// Staged directly against <see cref="Generators.GenerateAll"/> rather than through the full
    /// pipeline: every scenario fixture this project ships already establishes a relationship
    /// between Vincent and each of his real subordinates at construction (<c>Cast.Build</c> for
    /// Tommy, <c>Variants.Apply</c> for Angelo), so there is no natural route to an
    /// organisationally-subordinate, wholly-unacquainted stranger without hand-building the
    /// <see cref="GeneratorContext"/> the way this test does.
    ///
    /// Three things are proven together, deliberately in one staged set rather than three:
    ///
    ///   - <b>Negative.</b> "aldo-stranger" is never offered, despite being in <c>SubordinateIds</c>.
    ///   - <b>Positive acquaintance control.</b> Tommy, who genuinely is acquainted, is still offered
    ///     — proving the filter is selective rather than a blanket suppression that would make the
    ///     negative assertion vacuous (a filter that excluded everyone would also "pass" it).
    ///   - <b>Comparative computed from the filtered set, not the raw count.</b> Raw
    ///     <c>SubordinateIds.Count</c> here is 2 (Tommy plus the stranger), which the pre-correction
    ///     gate would have read as a genuine two-way choice; with the stranger filtered out, only one
    ///     nameable subordinate remains, so Tommy's own candidate is not
    ///     <see cref="Candidate.ComparingExecutors"/> at all — there is nothing to compare him
    ///     against that Vincent could actually name.
    ///
    /// <b>Mutation check.</b> Reverting <c>FromRelationship</c>'s subordinate loop to iterate
    /// <c>ctx.SubordinateIds</c> directly (this test's own pre-correction shape) was confirmed to make
    /// this test fail on all three counts — the stranger appears, and Tommy's candidate is wrongly
    /// marked as a two-way comparison — then reverted.
    /// </summary>
    [Fact]
    public void An_organisationally_subordinate_but_unacquainted_stranger_is_not_offered_as_a_delegate()
    {
        var world = Cast.Build(Seed, Baseline);
        var vincent = world.Get(Vincent);
        vincent.Execution.Strategy = new StrategyInstance
        {
            OwnerId = vincent.Id,
            LocalSequence = vincent.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            Method = CoercionMethod.Persuade,
            StartedAt = world.Now,
            Deadline = world.Now.AddDays(30),
        };

        var ctx = Context(
            world, vincent,
            subordinateIds: new[] { Tommy, "aldo-stranger" },
            acquainted: new[] { Tommy });

        var delegateCandidates = Generators.GenerateAll(ctx)
            .Where(c => c.Kind == ActionKind.DelegateStrategy)
            .ToList();

        Assert.DoesNotContain(delegateCandidates, c => c.TargetId == "aldo-stranger");

        var tommyCandidate = Assert.Single(delegateCandidates);
        Assert.Equal(Tommy, tommyCandidate.TargetId);
        Assert.False(tommyCandidate.ComparingExecutors);
    }

    // ================================================================= scoring: capability present/absent

    /// <summary>
    /// The ladder does what a scalar could not: **magnitude is which bars Vincent holds, and it is
    /// separately visible from how sure he is of each.**
    ///
    /// In the natural `capable-angelo` fixture Vincent believes Angelo clears both bars and believes
    /// Tommy clears only the low one — he has formed no view on whether Tommy is exceptional. So
    /// Angelo's breakdown carries *two* capability components and Tommy's carries one, which is the
    /// graded structure milestone 021's ruling 5 chose over a single number, asserted rather than
    /// argued. Each is tagged <see cref="RelationshipFacet.None"/> and truthfully so this time: the
    /// term now reads beliefs out of `perceived` and no relationship state at all.
    /// </summary>
    [Fact]
    public void Capability_is_scored_as_the_bars_he_holds_not_as_one_number()
    {
        var world = Cast.Build(Seed, Variant);
        var prepared = AdvanceToVincentsFork(world);

        var tommyScored = prepared.Scored.Single(
            s => s.Candidate.Kind == ActionKind.DelegateStrategy && s.Candidate.TargetId == Tommy);
        var angeloScored = prepared.Scored.Single(
            s => s.Candidate.Kind == ActionKind.DelegateStrategy && s.Candidate.TargetId == Angelo);

        Assert.True(tommyScored.Candidate.ComparingExecutors);
        Assert.True(angeloScored.Candidate.ComparingExecutors);

        var tommyBars = tommyScored.Components.Where(c => c.Name == "executor capability").ToList();
        var angeloBars = angeloScored.Components.Where(c => c.Name == "executor capability").ToList();

        // One bar for Tommy, two for Angelo — the ladder, not a scalar.
        Assert.Single(tommyBars);
        Assert.Equal(2, angeloBars.Count);

        // The high bar is the one that distinguishes them, and it is worth more than the low one.
        var hardMan = angeloBars.Single(c => c.Explanation.Contains("hard man", StringComparison.Ordinal));
        Assert.True(hardMan.Value > tommyBars[0].Value);

        // Summed, Angelo is the better bet on capability alone.
        Assert.True(angeloBars.Sum(c => c.Value) > tommyBars.Sum(c => c.Value));

        // Reads no relationship state — the tag milestone 020 used, now for the stated reason.
        foreach (var bar in tommyBars.Concat(angeloBars))
            Assert.Equal(RelationshipFacet.None, bar.Reads);
    }

    /// <summary>
    /// Certainty and magnitude move independently, which is the whole substance of ruling 5.
    ///
    /// Two worlds where Vincent holds *exactly the same bars* about Angelo — both of them — and
    /// differs only in how sure he is of the high one. The high bar's component moves; the low bar's
    /// does not; and no bar appears or disappears. A design that had encoded "how good" as "how sure"
    /// could not produce this result, because there would be nothing left to vary independently.
    /// </summary>
    [Fact]
    public void How_sure_he_is_moves_separately_from_which_bars_he_holds()
    {
        var certain = BuildAngeloWorld(actualCoercion: 0.80, believedHardMan: 0.90);
        var unsure = BuildAngeloWorld(actualCoercion: 0.80, believedHardMan: 0.20);

        var certainBars = CapabilityBarsFor(AdvanceToVincentsFork(certain), Angelo);
        var unsureBars = CapabilityBarsFor(AdvanceToVincentsFork(unsure), Angelo);

        // Same bars held in both — the ladder is identical, only the certainty differs.
        Assert.Equal(2, certainBars.Count);
        Assert.Equal(2, unsureBars.Count);

        var certainHigh = certainBars.Single(c => c.Explanation.Contains("hard man", StringComparison.Ordinal));
        var unsureHigh = unsureBars.Single(c => c.Explanation.Contains("hard man", StringComparison.Ordinal));
        var certainLow = certainBars.Single(c => c.Explanation.Contains("up to leaning", StringComparison.Ordinal));
        var unsureLow = unsureBars.Single(c => c.Explanation.Contains("up to leaning", StringComparison.Ordinal));

        Assert.True(certainHigh.Value > unsureHigh.Value);
        Assert.Equal(certainLow.Value, unsureLow.Value, precision: 9);
    }

    /// <summary>
    /// Holding a bar, doubting it, and having no view on it are three different states, and the
    /// middle one is not the same as the third.
    ///
    /// A man Vincent has concluded is *not* a hard man scores worse than one he has never thought
    /// about, because a held negative position is information and an absent one is not. This is the
    /// branch the natural fixture deliberately does not seed — Vincent having decided Tommy is no
    /// hard man would be an opinion invented for him — so it is proven here instead.
    /// </summary>
    [Fact]
    public void A_rejected_bar_scores_worse_than_a_bar_he_has_no_view_on()
    {
        var rejected = BuildAngeloWorld(
            actualCoercion: 0.80, believedHardMan: 0.80, hardManRejected: true);
        var noView = BuildAngeloWorld(actualCoercion: 0.80, believedHardMan: null);

        var rejectedBars = CapabilityBarsFor(AdvanceToVincentsFork(rejected), Angelo);
        var noViewBars = CapabilityBarsFor(AdvanceToVincentsFork(noView), Angelo);

        // No view on the high bar means no component for it at all — not a zero-valued one.
        Assert.Single(noViewBars);
        Assert.DoesNotContain(noViewBars, c => c.Explanation.Contains("hard man", StringComparison.Ordinal));

        // A rejected high bar is present and negative.
        var rejectedHigh = rejectedBars.Single(
            c => c.Explanation.Contains("no more than adequate", StringComparison.Ordinal));
        Assert.True(rejectedHigh.Value < 0);

        // And therefore the man he has written off is a worse bet than the man he has not weighed.
        Assert.True(rejectedBars.Sum(c => c.Value) < noViewBars.Sum(c => c.Value));
    }

    /// <summary>
    /// The P1 correction to milestone 020's accepted state, proven against the thing that was
    /// actually wrong rather than against the tag alone.
    ///
    /// Milestone 020's correction 3 had to add a `Capability` facet, because the component then read
    /// a figure stored on the relationship record and was lying about it. Moving the belief into
    /// `Cognition` makes the *original* tag correct: capability is no longer relationship state, so
    /// the component belongs outside the relationship channel and a man with no relationships still
    /// holds his beliefs about who is any good.
    ///
    /// The last assertion is the one that matters and is deliberately not this test's own arithmetic:
    /// it compares the reported counterfactual against a score genuinely produced by a Vincent
    /// stripped of every relationship, and requires the capability term to have survived that.
    /// </summary>
    [Fact]
    public void Capability_is_outside_the_relationship_channel_and_survives_the_counterfactual()
    {
        var world = Cast.Build(Seed, Variant);
        var prepared = AdvanceToVincentsFork(world);

        var angeloScored = prepared.Scored.Single(
            s => s.Candidate.Kind == ActionKind.DelegateStrategy && s.Candidate.TargetId == Angelo);
        var bars = angeloScored.Components.Where(c => c.Name == "executor capability").ToList();
        Assert.NotEmpty(bars);

        // Not in the relationship channel, and contributing nothing to its gross or net.
        Assert.DoesNotContain(angeloScored.RelationshipComponents(), c => c.Name == "executor capability");
        foreach (var bar in bars)
        {
            Assert.Equal(bar.Value, bar.RelationshipFreeValue, precision: 9);
            Assert.Equal(0.0, bar.RelationshipShare, precision: 9);
        }

        // And therefore the whole capability contribution survives the relationship counterfactual:
        // what Vincent believes Angelo is good for does not depend on his standing toward him.
        Assert.Equal(
            angeloScored.Total - angeloScored.RelationshipNet(),
            angeloScored.TotalWithoutRelationships(),
            precision: 9);
        Assert.Contains(bars.Sum(c => c.Value), new[] { bars.Sum(c => c.RelationshipFreeValue) });
    }

    /// <summary>
    /// The regression that guarantees every existing accepted variant's hash cannot move: with
    /// exactly one subordinate the candidate is not <see cref="Candidate.ComparingExecutors"/> and
    /// the "executor capability" component is never emitted at all — not emitted-at-zero, genuinely
    /// absent, so <c>Utility.Add</c>'s near-zero guard never even has to fire.
    /// </summary>
    [Fact]
    public void A_single_subordinate_gets_no_executor_capability_comparison()
    {
        var world = Cast.Build(Seed, Baseline);
        var prepared = AdvanceToVincentsFork(world, Baseline);

        var tommyScored = prepared.Scored.Single(s => s.Candidate.Kind == ActionKind.DelegateStrategy);
        Assert.False(tommyScored.Candidate.ComparingExecutors);
        Assert.DoesNotContain(tommyScored.Components, c => c.Name == "executor capability");
    }

    // ================================================================= belief vs. objective capability
    //
    // The negative controls that keep the omniscient read out. These independently vary Angelo's
    // *actual* Capabilities[Skill.Coercion] and what Vincent *believes* about him — two things
    // milestone 020 forced equal by scoring the first directly. `BuildAngeloWorld` below constructs
    // each combination, mirroring `Variants.Apply`'s "capable-angelo" case but parameterised, which
    // is the test-local duplication this project prefers to a scenario knob that exists for tests.

    /// <summary>
    /// Moving Angelo's hidden, objective Coercion — the figure only <see cref="Strategies.ResolveViolence"/>
    /// may read — changes nothing about what Vincent's fork offers, scores or prefers, because
    /// scoring never reaches it. Vincent believes exactly the same things in both worlds; only the
    /// actual <see cref="Capabilities"/> differs (0.80 vs. 0.15).
    /// </summary>
    [Fact]
    public void Changing_only_angelos_hidden_actual_capability_leaves_scoring_unchanged()
    {
        var reference = BuildAngeloWorld(actualCoercion: 0.80);
        var hiddenChange = BuildAngeloWorld(actualCoercion: 0.15);

        var refPrepared = AdvanceToVincentsFork(reference);
        var changedPrepared = AdvanceToVincentsFork(hiddenChange);

        // Same candidates offered.
        Assert.Equal(
            refPrepared.Available.Select(c => c.Id).OrderBy(id => id, StringComparer.Ordinal),
            changedPrepared.Available.Select(c => c.Id).OrderBy(id => id, StringComparer.Ordinal));

        var refAngelo = refPrepared.Scored.Single(
            s => s.Candidate.Kind == ActionKind.DelegateStrategy && s.Candidate.TargetId == Angelo);
        var changedAngelo = changedPrepared.Scored.Single(
            s => s.Candidate.Kind == ActionKind.DelegateStrategy && s.Candidate.TargetId == Angelo);

        // Same capability components, same total score, same rank — none of it moved with the
        // hidden actual capability, because nothing in the scoring path ever read it.
        Assert.Equal(
            CapabilityBarsFor(refPrepared, Angelo).Select(c => c.Value),
            CapabilityBarsFor(changedPrepared, Angelo).Select(c => c.Value));
        Assert.Equal(refAngelo.Total, changedAngelo.Total, precision: 9);
        Assert.Equal(
            refPrepared.Scored[0].Candidate.Id,
            changedPrepared.Scored[0].Candidate.Id);

        // The hidden change is real, not a no-op fixture bug: the two worlds' Angelos genuinely
        // differ where ResolveViolence looks.
        Assert.Equal(0.80, reference.Get(Angelo).Capabilities[Skill.Coercion]);
        Assert.Equal(0.15, hiddenChange.Get(Angelo).Capabilities[Skill.Coercion]);
    }

    /// <summary>
    /// Moving only what Vincent believes about Angelo — leaving Angelo's actual Coercion fixed at
    /// 0.80 in both worlds — changes the capability contribution and flips which of the two delegate
    /// candidates Vincent prefers <em>between the two of them</em>. This is the positive half of the
    /// same claim: the belief is a real scoring input, not state nobody reads.
    ///
    /// Scoped to the delegate-versus-delegate comparison rather than <c>Scored[0]</c> overall:
    /// at this fork — Vincent's very first pause after starting the strategy himself — "carry on
    /// what he just started" outscores either delegate candidate regardless of Angelo's assessment
    /// (continuing costs nothing socially yet, per <c>Utility</c>'s "commitment value"; the archive's
    /// own 6.52-vs-6.38 comparison was drawn from a later decision in the full run, after the direct
    /// attempt had already failed once). That does not change what this test needs to show: whether
    /// the assessment is what decides Angelo-versus-Tommy specifically.
    /// </summary>
    [Fact]
    public void Changing_only_vincents_assessment_of_angelo_changes_score_and_can_flip_preference()
    {
        // Same objective man in both. In one Vincent rates him a hard man; in the other he has
        // concluded the opposite, which is a held position rather than an absence of one.
        var trusting = BuildAngeloWorld(actualCoercion: 0.80, believedHardMan: 0.70);
        var doubting = BuildAngeloWorld(
            actualCoercion: 0.80, believedHardMan: 0.70, hardManRejected: true);

        var trustingPrepared = AdvanceToVincentsFork(trusting);
        var doubtingPrepared = AdvanceToVincentsFork(doubting);

        var trustingAngelo = trustingPrepared.Scored.Single(
            s => s.Candidate.Kind == ActionKind.DelegateStrategy && s.Candidate.TargetId == Angelo);
        var trustingTommy = trustingPrepared.Scored.Single(
            s => s.Candidate.Kind == ActionKind.DelegateStrategy && s.Candidate.TargetId == Tommy);
        var doubtingAngelo = doubtingPrepared.Scored.Single(
            s => s.Candidate.Kind == ActionKind.DelegateStrategy && s.Candidate.TargetId == Angelo);
        var doubtingTommy = doubtingPrepared.Scored.Single(
            s => s.Candidate.Kind == ActionKind.DelegateStrategy && s.Candidate.TargetId == Tommy);

        double trustingCapability = CapabilityBarsFor(trustingPrepared, Angelo).Sum(c => c.Value);
        double doubtingCapability = CapabilityBarsFor(doubtingPrepared, Angelo).Sum(c => c.Value);
        Assert.True(trustingCapability > doubtingCapability);

        // Tommy's own score is untouched by Vincent's view of Angelo moving (each candidate scores
        // independently), so the whole swing lands on Angelo's total — enough, at seed 42, to flip
        // which of the two men Vincent would rather send once Angelo is written off as no hard man.
        Assert.Equal(trustingTommy.Total, doubtingTommy.Total, precision: 9);
        Assert.True(trustingAngelo.Total > trustingTommy.Total,
            "expected Angelo, believed a hard man, to outscore Tommy between the two delegate options");
        Assert.True(doubtingAngelo.Total < doubtingTommy.Total,
            "expected Angelo, written off as no hard man, to score below Tommy between the two options");
    }

    /// <summary>
    /// A subordinate Vincent has formed no view of at all gets no "executor capability" term —
    /// never the executor's real 0.80 (a fallback to <c>World</c>) and never a zero-valued component
    /// standing in for an opinion he does not have. Angelo's actual Coercion is deliberately far from
    /// both, so either regression is caught here.
    ///
    /// The milestone-020 version of this test guarded a nullable scalar. The belief model makes the
    /// same guarantee structurally: <c>perceived.Position</c> returns null for a claim he holds no
    /// position on, and the loop simply emits nothing.
    /// </summary>
    [Fact]
    public void A_man_he_has_no_view_of_gets_no_capability_term_at_all()
    {
        var world = BuildAngeloWorld(
            actualCoercion: 0.80, believedRoughWork: null, believedHardMan: null);
        var prepared = AdvanceToVincentsFork(world);

        Assert.Empty(CapabilityBarsFor(prepared, Angelo));

        // Tommy's own belief is untouched by Angelo's absence — his term is still there, so this is
        // a targeted absence, not the comparative branch failing to fire at all.
        Assert.NotEmpty(CapabilityBarsFor(prepared, Tommy));
    }

    // ================================================================= revision: the belief moves at runtime
    //
    // Milestone 021's own deliverable. Everything above proves capability is *read* as a belief;
    // these prove it is a belief in the sense that matters — one the world can move.

    /// <summary>
    /// The natural proof, and the milestone's headline claim: in an unmodified `capable-angelo` run,
    /// what Vincent believes about the man he sent is not what he believed at the start.
    ///
    /// Asserts on the reconsideration stamp as well as the confidence, because the two say different
    /// things: the stamp is what `Cognition.Revise` sets and is therefore evidence the production
    /// revision path ran, while a changed confidence alone could in principle come from anywhere.
    /// </summary>
    [Fact]
    public void The_delegators_read_of_his_man_moves_during_a_natural_run()
    {
        var world = Cast.Build(Seed, Variant);
        Runner.Run(world, End);

        var vincent = world.Get(Vincent);
        var hardMan = vincent.Cognition.Records.Single(
            r => r.Claim.Equals(CapabilityBar.About(Angelo, CapabilityBar.HardMan)));

        Assert.NotEqual(0.70, hardMan.Confidence);
        Assert.True(hardMan.ReconsideredAt > hardMan.AcquiredAt,
            "the belief was never reconsidered, so nothing in the run revised it");
    }

    /// <summary>
    /// Both directions of the rule, staged directly against it so the arithmetic is visible.
    /// Success makes him surer of what he holds; failure makes him less sure. The rule is exercised
    /// through its own public entry point rather than through a full run, following this project's
    /// practice of keeping a rule testable without staging a whole world around it.
    /// </summary>
    [Theory]
    [InlineData(true, 0.90)]
    [InlineData(false, 0.70)]
    public void A_delegated_outcome_moves_confidence_in_the_direction_of_the_outcome(
        bool succeeded, double expected)
    {
        var world = Cast.Build(Seed, Variant);
        var vincent = world.Get(Vincent);
        var claim = CapabilityBar.About(Angelo, CapabilityBar.RoughWork);

        Assert.Equal(0.80, vincent.Cognition.Records.Single(r => r.Claim.Equals(claim)).Confidence);

        Suitability.RecordDelegatedOutcome(vincent, Angelo, succeeded, world.Now.AddDays(1));

        Assert.Equal(
            expected,
            vincent.Cognition.Records.Single(r => r.Claim.Equals(claim)).Confidence,
            precision: 9);
    }

    /// <summary>
    /// Direction is relative to what he already believes, not to the outcome alone.
    ///
    /// A boss who has written a man off as no hard man becomes *less* sure of that when the job
    /// comes off. Read the other way — "success always raises confidence" — he would grow steadily
    /// more certain of his poor opinion every time the man succeeded, which is a counter rather than
    /// a belief. This is the case that distinguishes the two readings.
    /// </summary>
    [Fact]
    public void Success_erodes_a_position_he_holds_against_the_man()
    {
        var world = BuildAngeloWorld(
            actualCoercion: 0.80, believedHardMan: 0.80, hardManRejected: true);
        var vincent = world.Get(Vincent);
        var claim = CapabilityBar.About(Angelo, CapabilityBar.HardMan);

        var before = vincent.Cognition.Records.Single(r => r.Claim.Equals(claim));
        Assert.False(before.IsHeld);
        Assert.Equal(0.80, before.Confidence);

        Suitability.RecordDelegatedOutcome(vincent, Angelo, succeeded: true, world.Now.AddDays(1));

        var after = vincent.Cognition.Records.Single(r => r.Claim.Equals(claim));
        Assert.Equal(0.70, after.Confidence, precision: 9);
        Assert.False(after.IsHeld);
    }

    /// <summary>
    /// Ruling 6's own proof obligation: the assessment must be able to end up <em>further</em> from
    /// the truth than it started, or the attribution error is not being modelled at all.
    ///
    /// Angelo is objectively poor at rough work (0.15) and Vincent already over-rates him. The job
    /// succeeds — for reasons that have nothing to do with Angelo's Coercion, which is the whole
    /// point of calling this confounded — and Vincent comes away *more* confident in a belief that
    /// was already wrong. A design in which evidence could only ever improve an assessment would
    /// fail this test, and would not be modelling belief.
    /// </summary>
    [Fact]
    public void A_confounded_success_can_make_a_wrong_belief_more_wrong()
    {
        var world = BuildAngeloWorld(actualCoercion: 0.15, believedHardMan: 0.60);
        var vincent = world.Get(Vincent);
        var claim = CapabilityBar.About(Angelo, CapabilityBar.HardMan);

        // He is wrong about the man: believed a hard man, objectively feeble.
        Assert.True(vincent.Cognition.Records.Single(r => r.Claim.Equals(claim)).IsHeld);
        Assert.Equal(0.15, world.Get(Angelo).Capabilities[Skill.Coercion]);

        Suitability.RecordDelegatedOutcome(vincent, Angelo, succeeded: true, world.Now.AddDays(1));

        Assert.Equal(
            0.70,
            vincent.Cognition.Records.Single(r => r.Claim.Equals(claim)).Confidence,
            precision: 9);
    }

    /// <summary>
    /// The negative controls, together because they are one rule seen from three sides: the outcome
    /// moves the belief of the man who sent him, about the man he sent, and nothing else.
    /// </summary>
    [Fact]
    public void An_outcome_moves_only_the_delegators_own_view_of_that_one_man()
    {
        var world = BuildAngeloWorld(actualCoercion: 0.80);
        var vincent = world.Get(Vincent);
        var salvatore = world.Get("salvatore");

        // A bystander who happens to hold the same belief, acquired the same way.
        salvatore.Cognition.Learn(
            CapabilityBar.About(Angelo, CapabilityBar.RoughWork),
            Stance.Believes, 0.80, SourceKind.Inference, "salvatore", world.Now);

        Suitability.RecordDelegatedOutcome(vincent, Angelo, succeeded: true, world.Now.AddDays(1));

        // The other man in the comparison is untouched — this was not a job about Tommy.
        Assert.Equal(
            0.75,
            vincent.Cognition.Records
                .Single(r => r.Claim.Equals(CapabilityBar.About(Tommy, CapabilityBar.RoughWork)))
                .Confidence,
            precision: 9);

        // And the bystander's identical belief is untouched — nobody told him how it went.
        Assert.Equal(
            0.80,
            salvatore.Cognition.Records
                .Single(r => r.Claim.Equals(CapabilityBar.About(Angelo, CapabilityBar.RoughWork)))
                .Confidence,
            precision: 9);

        // A man who did the work himself forms no opinion of himself this way.
        int before = vincent.Cognition.Records.Count;
        Suitability.RecordDelegatedOutcome(vincent, Vincent, succeeded: true, world.Now.AddDays(2));
        Assert.Equal(before, vincent.Cognition.Records.Count);
    }

    /// <summary>
    /// An outcome revises an opinion; it never invents one, and it cannot revise one he was told.
    ///
    /// The second half records a real consequence of putting capability in the ordinary claim
    /// vocabulary: capability beliefs travel through the report and corroboration channels like any
    /// other claim, and <see cref="Cognition.Revise"/> admits only a holder's own reading — so a view
    /// acquired second-hand is, for now, permanently frozen. Recorded in `ROADMAP.md` as debt rather
    /// than papered over here.
    /// </summary>
    [Fact]
    public void An_outcome_neither_invents_a_view_nor_revises_one_he_was_told()
    {
        var world = BuildAngeloWorld(actualCoercion: 0.80, believedHardMan: null);
        var vincent = world.Get(Vincent);
        var told = world.Get("salvatore");

        // Salvatore holds the same belief, but on Vincent's word rather than his own reading.
        var claim = CapabilityBar.About(Angelo, CapabilityBar.RoughWork);
        told.Cognition.Learn(claim, Stance.Believes, 0.80, SourceKind.Report, Vincent, world.Now);

        Suitability.RecordDelegatedOutcome(vincent, Angelo, succeeded: true, world.Now.AddDays(1));
        Suitability.RecordDelegatedOutcome(told, Angelo, succeeded: true, world.Now.AddDays(1));

        // No view on the high bar before, and none invented by a job going well.
        Assert.DoesNotContain(
            vincent.Cognition.Records,
            r => r.Claim.Equals(CapabilityBar.About(Angelo, CapabilityBar.HardMan)));

        // And the second-hand belief is refused by Revise's own provenance guard.
        Assert.Equal(
            0.80,
            told.Cognition.Records.Single(r => r.Claim.Equals(claim)).Confidence,
            precision: 9);
    }

    // ================================================================= staged: attribution + capability-scaled resolution

    /// <summary>
    /// Force resolves through the real production path once with each subordinate as executor,
    /// from otherwise-identical staged state — mirroring milestone 017's Section B idiom exactly,
    /// applied to the second subordinate. Proves three things at once:
    ///
    ///   - attribution names the actual executor (milestone 017's rule, reprised for Angelo — the
    ///     direct falsifier of mutation check 4, attributing the witness claim to the owner);
    ///   - the owner, when he differs, holds no first-hand copy;
    ///   - the resistance drop is the executor's own Coercion run through the calibrated formula,
    ///     not a flat constant and not the owner's Coercion (the direct falsifier of mutation
    ///     check 3, reading <c>owner.Capabilities[Skill.Coercion]</c> instead of the executor's —
    ///     Vincent's 0.75 would give 0.38 for both cases below, matching neither expected value).
    /// </summary>
    [Theory]
    [InlineData(Tommy, 0.55, 0.30)]
    [InlineData(Angelo, 0.80, 0.40)]
    public void Force_resolution_is_attributed_to_and_scaled_by_the_actual_executor(
        string executorId, double expectedCoercion, double expectedReduction)
    {
        var world = Cast.Build(Seed, Variant);
        var owner = world.Get(Vincent);
        var executor = world.Get(executorId);
        var business = world.Businesses[Cast.Grocery];
        double startingResistance = business.Resistance;

        Assert.Equal(expectedCoercion, executor.Capabilities[Skill.Coercion]);

        var s = OpenTributeCase(world, owner, executor, CoercionMethod.Force);
        AdvanceTributeSteps(world, executor, s, steps: 3); // approach, demand, press (force resolves here)

        var violenceEvent = world.TruthLog.Single(e => e.Kind == "violence");
        Assert.Equal(executorId, violenceEvent.ActorId);

        // Scoped to a single observer (Kane — Investigation 0.70 clears the bystander-witness
        // threshold every time) because ResolveViolence can offer the identical claim to several
        // eligible observers at once; matching DirectActionVsDelegationTests' own precedent rather
        // than asserting on whichever happens to be first.
        var opportunity = Drain(world).SingleOrDefault(e =>
            e.Kind == EventKind.ObservationOpportunity && e.OwnerId == "kane"
            && e.Payload.Claims.Any(c => c.Kind == ClaimKind.WitnessSawIncident && c.EventId == violenceEvent.Id));
        Assert.True(opportunity is not null,
            "ResolveViolence did not schedule any observation opportunity for this incident, so " +
            "attribution cannot be checked against production output");
        var witnessClaim = opportunity!.Payload.Claims.Single(c => c.Kind == ClaimKind.WitnessSawIncident);
        Assert.Equal(executorId, witnessClaim.Object);

        var violenceClaims = executor.Cognition.OfKind(ClaimKind.PersonUsedViolence).ToList();
        Assert.Contains(violenceClaims, r => r.Claim.Subject == executorId);
        if (owner.Id != executor.Id)
            Assert.DoesNotContain(owner.Cognition.Records, r => r.Claim.Kind == ClaimKind.PersonUsedViolence);

        Assert.Equal(startingResistance - expectedReduction, business.Resistance, precision: 9);
    }

    /// <summary>
    /// The natural run's own consequence, not only a staged one: whichever subordinate Vincent
    /// actually delegates to at seed 42, that same man — never the owner, never the other
    /// subordinate — is the one whose name is on the violence when force is eventually applied.
    /// Combined with the staged proof above (which pins the exact numeric outcome each executor's
    /// own Coercion produces from identical state), this ties the natural run's choice to a real,
    /// production-computed consequence rather than asserting the arithmetic a second time across a
    /// full 90-day run, where other events (Marco's own concession) also move
    /// <see cref="Business.Resistance"/> and would make a hand-derived expected total fragile.
    /// </summary>
    [Fact]
    public void The_natural_runs_chosen_executor_is_who_throws_the_punch()
    {
        var world = Cast.Build(Seed, Variant);
        Runner.Run(world, End);

        var delegation = world.Decisions.Single(d =>
            d.Chosen?.Candidate.Kind == ActionKind.DelegateStrategy
            && d.Chosen.Candidate.TargetId is Tommy or Angelo);
        string executorId = delegation.Chosen!.Candidate.TargetId!;

        var violence = world.TruthLog.SingleOrDefault(e => e.Kind == "violence");
        Assert.True(violence is not null, "no force was applied in the natural run, so this proves nothing");
        Assert.Equal(executorId, violence!.ActorId);
    }

    // ================================================================= save/load through the fork

    /// <summary>
    /// The fork forced through a save and two independent loads, each continued to a real
    /// consequence and compared against an equivalent unsaved control — mirroring milestone 017's
    /// Section C exactly, applied to the Tommy-vs-Angelo choice instead of continue-vs-delegate.
    /// </summary>
    [Fact]
    public void Save_and_load_from_the_fork_reproduces_either_chosen_executor()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ce-m020-fork-{Guid.NewGuid():N}.db");
        try
        {
            var setup = CrimeEmpire.Persistence.Session.PersistentSession.Start(Seed, Variant, Vincent);
            AdvanceToNextPause(setup);
            ChooseByDescription(setup, StartPersuade);
            AdvanceToNextPause(setup); // the fork: carry on / delegate-Tommy / delegate-Angelo
            setup.Save(path);

            var tommyLoaded = CrimeEmpire.Persistence.Session.PersistentSession.Load(path);
            ChooseByDescription(tommyLoaded, DelegateToTommy);
            SettleLastOption(tommyLoaded);

            var angeloLoaded = CrimeEmpire.Persistence.Session.PersistentSession.Load(path);
            ChooseByDescription(angeloLoaded, DelegateToAngelo);
            SettleLastOption(angeloLoaded);

            var tommyControl = CrimeEmpire.Persistence.Session.PersistentSession.Start(Seed, Variant, Vincent);
            AdvanceToNextPause(tommyControl);
            ChooseByDescription(tommyControl, StartPersuade);
            AdvanceToNextPause(tommyControl);
            ChooseByDescription(tommyControl, DelegateToTommy);
            SettleLastOption(tommyControl);

            var angeloControl = CrimeEmpire.Persistence.Session.PersistentSession.Start(Seed, Variant, Vincent);
            AdvanceToNextPause(angeloControl);
            ChooseByDescription(angeloControl, StartPersuade);
            AdvanceToNextPause(angeloControl);
            ChooseByDescription(angeloControl, DelegateToAngelo);
            SettleLastOption(angeloControl);

            AssertPersistentEquivalence(tommyLoaded, tommyControl);
            AssertPersistentEquivalence(angeloLoaded, angeloControl);

            var tommyWorld = tommyLoaded.InnerSession.World;
            var angeloWorld = angeloLoaded.InnerSession.World;
            Assert.True(
                tommyWorld.TruthLog.Count > 1 && angeloWorld.TruthLog.Count > 1,
                "the settled continuation barely moved past the fork, so this proves nothing about a " +
                "real consequence");
            Assert.NotEqual(
                TraceWriter.Render(tommyWorld, Variant, false),
                TraceWriter.Render(angeloWorld, Variant, false));

            Assert.Contains(Tommy, tommyWorld.Get(Vincent).Execution.DelegatedExecutorIds);
            Assert.Contains(Angelo, angeloWorld.Get(Vincent).Execution.DelegatedExecutorIds);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ================================================================= preference leakage

    /// <summary>
    /// The developer-only vocabulary this milestone introduces — the "executor capability" reason
    /// text and both subordinates' raw Coercion figures — never reaches the fork's player-facing
    /// options. Direct falsifier of mutation check 5: having <c>PlayerOption</c> or
    /// <c>PendingDecision</c> include the score or the capability reason text makes this fail.
    ///
    /// Narrower than, and additional to, <c>PlayerSessionTests</c>' existing generic "no player
    /// phrase carries a decimal" regression (already extended to this variant for free by adding it
    /// to <see cref="Variants.All"/>): that check catches a leaked <em>number</em>, not a leaked
    /// <em>phrase</em> with no digits in it, such as "better suited" or "not the man for rough
    /// work".
    /// </summary>
    [Fact]
    public void Neither_delegates_capability_reasoning_or_score_reaches_the_player()
    {
        var session = SimulationSession.Start(Seed, Variant, Vincent);
        var pending = ReachFork(session, End);

        string text = string.Join('\n', pending.Options.Select(o => o.Description));
        Assert.DoesNotContain("hard man", text, StringComparison.Ordinal);
        Assert.DoesNotContain("up to leaning", text, StringComparison.Ordinal);
        Assert.DoesNotContain("no more than adequate", text, StringComparison.Ordinal);
        Assert.DoesNotContain("not the man for rough work", text, StringComparison.Ordinal);
        Assert.DoesNotContain("executor capability", text, StringComparison.Ordinal);
        // Nor the belief vocabulary itself — a player must not read the claim kind or its bars.
        Assert.DoesNotContain("PersonIsCapable", text, StringComparison.Ordinal);
        Assert.DoesNotContain(CapabilityBar.RoughWork, text, StringComparison.Ordinal);
        Assert.DoesNotContain(CapabilityBar.HardMan, text, StringComparison.Ordinal);
        foreach (var figure in new[] { "0.8", "0.55", "0.35", "0.65", "0.7", "0.75" })
            Assert.DoesNotContain(figure, text, StringComparison.Ordinal);

        // Each delegate option identifies its own candidate and nothing more — no ranking, no
        // recommendation. Both are present as distinctly-worded options; PreparedDecision.Available
        // is separately pinned (milestone 009 ruling 5) to be sorted by candidate id, never score,
        // which is what a player-facing surface reading Available verbatim inherits for free.
        Assert.Contains(DelegateToTommy, text);
        Assert.Contains(DelegateToAngelo, text);
    }

    /// <summary>
    /// Every "executor capability" component on one man's delegate candidate — the ladder as scored,
    /// which is a list now rather than a single value, because magnitude is which bars are held.
    /// </summary>
    private static List<ScoreComponent> CapabilityBarsFor(PreparedDecision prepared, string executorId)
        => prepared.Scored
            .Single(s => s.Candidate.Kind == ActionKind.DelegateStrategy
                         && s.Candidate.TargetId == executorId)
            .Components
            .Where(c => c.Name == "executor capability")
            .ToList();

    // ================================================================= helpers — natural/session level

    private static PendingDecision RunToNextPause(SimulationSession session, DateTime horizon)
    {
        if (session.Status == SessionStatus.Ready) session.AdvanceTo(horizon);
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
        return session.Pending!;
    }

    private static PendingDecision ReachFork(SimulationSession session, DateTime horizon)
    {
        RunToNextPause(session, horizon);
        ChooseByDescription(session, StartPersuade);
        return RunToNextPause(session, horizon);
    }

    private static void ChooseByDescription(SimulationSession session, string description)
    {
        var pending = session.Pending ?? RunToNextPause(session, End);

        int index = -1;
        for (int i = 0; i < pending.Options.Count; i++)
        {
            if (!string.Equals(pending.Options[i].Description, description, StringComparison.Ordinal)) continue;
            Assert.Equal(-1, index);
            index = i;
        }

        Assert.True(index >= 0,
            $"no offered option reads \"{description}\" on {session.Date:yyyy-MM-dd} — offered: " +
            string.Join(" | ", pending.Options.Select(o => o.Description)));
        session.Choose(pending.Options[index].Id);
    }

    // ================================================================= helpers — developer-facing pipeline level

    /// <summary>
    /// Drives a fresh world to Vincent's fork — his second pause, immediately after the start
    /// decision — through the real <see cref="Runner.Step"/>/<see cref="Pipeline.Resolve"/>
    /// boundary, returning the prepared decision (with <c>Scored</c>, <c>Available</c> and
    /// <c>Rejected</c> all populated) before anything at the fork itself resolves.
    /// </summary>
    private static PreparedDecision AdvanceToVincentsFork(World world, string variant = Variant)
    {
        var first = AdvanceToVincentsNextPause(world);
        // Matched on structured fields, not Candidate.Description — that string is the developer
        // trace's own wording ("talk bellini-grocery round"), not the player-facing StartPersuade
        // text ("talk Bellini's grocery round") ChooseByDescription matches against Options; the
        // two vocabularies are deliberately different (PlayerOption.cs's own header explains why).
        var startCandidate = first.Available.Single(c =>
            c.Kind == ActionKind.StartStrategy && c.Strategy == StrategyKind.SecureTribute
            && c.TargetId == Cast.Grocery && c.Method == CoercionMethod.Persuade);
        Pipeline.Resolve(first, startCandidate.Id);

        return AdvanceToVincentsNextPause(world);
    }

    /// <summary>
    /// Not the literal next event: <c>StrategyStep</c> auto-advances silently (never a
    /// <c>Think</c>/pause), so the events between one of Vincent's pauses and his next — his own
    /// approach step, Marco's own demand/concede/refuse decision, his own blocked-pressure step —
    /// are real intervening advances, not skipped. Loops the same way
    /// <c>ControlledAutonomousParityTests.AdvanceToTommysPause</c> does.
    /// </summary>
    private static PreparedDecision AdvanceToVincentsNextPause(World world)
    {
        for (int guard = 0; guard < 5000; guard++)
        {
            var step = Runner.Step(world, DateTime.MaxValue, Vincent);
            if (step.Status == StepStatus.AwaitingChoice) return step.Awaiting!;
            if (step.Status == StepStatus.Exhausted)
                throw new InvalidOperationException("queue exhausted before Vincent ever paused");
        }
        throw new InvalidOperationException("guard exceeded before Vincent ever paused");
    }

    /// <summary>
    /// A world with Tommy and Angelo both present, letting Angelo's actual Coercion and Vincent's
    /// assessment of it be varied independently — the two figures the pre-correction code forced
    /// equal by reading one off the other.
    ///
    /// Deliberately test-local rather than a knob added to <c>Variants.Apply</c>: everything else
    /// about Angelo is copied verbatim from the real <c>capable-angelo</c> case (psychology, crew,
    /// districts, his own outgoing relationships), and calling this with <c>(0.80, 0.80)</c> — the
    /// production variant's own figures — reproduces that variant's fork exactly, which is what
    /// grounds the "reference" side of each comparison below in the real accepted behaviour rather
    /// than an invented one.
    /// </summary>
    private static World BuildAngeloWorld(
        double actualCoercion,
        double? believedRoughWork = 0.80,
        double? believedHardMan = 0.70,
        bool hardManRejected = false)
    {
        var world = Cast.Build(Seed, Baseline);
        var vincent = world.Get(Vincent);

        var angelo = new Character
        {
            Id = Angelo,
            Name = "Angelo Conti",
            RoleTitle = "soldier",
            Pronouns = Pronouns.He,
            Capabilities = new Capabilities(
                new Dictionary<Skill, double>
                {
                    [Skill.Coercion] = actualCoercion, [Skill.Persuasion] = 0.20,
                    [Skill.Discretion] = 0.25, [Skill.Investigation] = 0.10,
                },
                crew: 1, cash: 700, authority: 1, districts: new[] { Cast.Harbour }),
            Psychology = new Psychology(
                new Dictionary<Trait, double>
                {
                    [Trait.Aggressive] = 0.45, [Trait.Cautious] = 0.55,
                    [Trait.Proud] = 0.30, [Trait.Suspicious] = 0.30,
                },
                new Dictionary<Drive, double>
                {
                    [Drive.Belonging] = 0.70, [Drive.Security] = 0.55,
                    [Drive.Wealth] = 0.45, [Drive.Status] = 0.35,
                }),
        };
        world.Characters[angelo.Id] = angelo;
        angelo.Social.OrganizationId = Cast.OrgId;

        Relations.Establish(vincent, "angelo", trust: 0.35, obligation: 0.10);
        Relations.Establish(angelo, "vincent", trust: 0.65, obligation: 0.55);
        Relations.Establish(angelo, "salvatore", trust: 0.25, obligation: 0.30);

        // Tommy's own seeded belief, matching Variants.Apply's, so the comparison between the two
        // men is the same one the production variant poses.
        vincent.Cognition.Learn(
            CapabilityBar.About(Tommy, CapabilityBar.RoughWork),
            Stance.Believes, 0.75, SourceKind.Inference, Vincent, world.Now);

        // Angelo's, parameterised. Null on either bar means Vincent has formed no view of him at
        // that bar at all — which must stay distinguishable from having concluded he fails it, and
        // is why `hardManRejected` is a separate argument rather than a negative confidence.
        if (believedRoughWork is { } roughWork)
            vincent.Cognition.Learn(
                CapabilityBar.About(Angelo, CapabilityBar.RoughWork),
                Stance.Believes, roughWork, SourceKind.Inference, Vincent, world.Now);

        if (believedHardMan is { } hardMan)
            vincent.Cognition.Learn(
                CapabilityBar.About(Angelo, CapabilityBar.HardMan),
                hardManRejected ? Stance.Rejects : Stance.Believes,
                hardMan, SourceKind.Inference, Vincent, world.Now);

        return world;
    }

    // ================================================================= helpers — staged (Section B idiom)

    /// <summary>Independently copied from <c>DirectActionVsDelegationTests</c>, per this project's
    /// practice of not sharing test helpers across milestone-specific files.</summary>
    private static StrategyInstance OpenTributeCase(
        World world, Character owner, Character executor, CoercionMethod method)
    {
        var ctx = Context(world, owner);
        var start = new Candidate($"start:tribute:{Cast.Grocery}:{method}", ActionKind.StartStrategy, "test",
            $"lean on {world.Businesses[Cast.Grocery].Name}")
        {
            TargetId = Cast.Grocery,
            Strategy = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            Method = method,
        };
        Commit.Apply(world, owner, start, ctx.Agenda, ctx, new List<string>());
        var s = owner.Execution.Strategy!;

        if (executor.Id != owner.Id)
        {
            var delegateCtx = Context(world, owner);
            var delegateCandidate = new Candidate($"delegate:{s.Kind}:{executor.Id}", ActionKind.DelegateStrategy,
                "test", $"have {executor.Name} take it on")
            {
                TargetId = executor.Id,
                Strategy = s.Kind,
                Method = s.Method,
                Domain = s.Domain,
                RequiredCrew = 1,
            };
            Commit.Apply(world, owner, delegateCandidate, delegateCtx.Agenda, delegateCtx, new List<string>());
        }

        return s;
    }

    private static void AdvanceTributeSteps(World world, Character executor, StrategyInstance s, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            if (s.PendingStepEventId is null) return;
            Strategies.Advance(world, executor, new ScheduledEvent
            {
                Id = s.PendingStepEventId!.Value,
                Time = world.Now,
                Kind = EventKind.StrategyStep,
                OwnerId = executor.Id,
                Cause = "test",
                Payload = new EventPayload
                {
                    StrategyOwnerId = s.OwnerId,
                    StrategySequence = s.LocalSequence,
                    AdvanceOrdinal = s.NextAdvanceOrdinal,
                    Strategy = s.Kind,
                    StepIndex = s.StepIndex,
                    TargetId = s.TargetId,
                },
            });
        }
    }

    private static List<ScheduledEvent> Drain(World world)
    {
        var drained = new List<ScheduledEvent>();
        while (world.Queue.Next(world.Now.AddYears(1)) is { } ev) drained.Add(ev);
        return drained;
    }

    private static GeneratorContext Context(
        World world, Character actor, IReadOnlyList<string>? subordinateIds = null, params string[] acquainted)
        => new(
            actor.View,
            Salience.Perceive(actor, world.Now),
            new Agenda(AgendaKind.DischargeResponsibility, "clear the family's business", "test", Cast.Harbour),
            world.Now,
            new ScheduledEvent
            {
                Id = 0,
                Time = world.Now,
                Kind = EventKind.RoleReview,
                OwnerId = actor.Id,
                Cause = "test",
            },
            MyOffice: null,
            MyAssignment: null,
            KnownPolicies: Array.Empty<Policy>(),
            SuperiorId: null,
            SubordinateIds: subordinateIds ?? Array.Empty<string>(),
            OrgMemberIds: Array.Empty<string>(),
            AcquaintedIds: acquainted,
            ReportsSent: Array.Empty<Report>(),
            RequestsMade: Array.Empty<InformationRequest>(),
            VisibleTargets: Array.Empty<string>());

    // ================================================================= helpers — save/load (Section C idiom)

    private static void AdvanceToNextPause(CrimeEmpire.Persistence.Session.PersistentSession session)
    {
        for (int guard = 0; guard < 5000 && session.Status != SessionStatus.AwaitingChoice; guard++)
            session.StepEvent();
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
    }

    private static void ChooseByDescription(CrimeEmpire.Persistence.Session.PersistentSession session, string description)
    {
        AdvanceToNextPause(session);
        var pending = session.Pending!;

        int index = -1;
        for (int i = 0; i < pending.Options.Count; i++)
        {
            if (!string.Equals(pending.Options[i].Description, description, StringComparison.Ordinal)) continue;
            Assert.Equal(-1, index);
            index = i;
        }

        Assert.True(index >= 0,
            $"no offered option reads \"{description}\" on {session.Date:yyyy-MM-dd} — offered: " +
            string.Join(" | ", pending.Options.Select(o => o.Description)));
        session.Choose(pending.Options[index].Id);
    }

    private static void SettleLastOption(CrimeEmpire.Persistence.Session.PersistentSession session)
    {
        session.AdvanceDays(90);
        while (session.Status == SessionStatus.AwaitingChoice)
            session.Choose(session.Pending!.Options[^1].Id);
    }

    private static string StrategyFingerprint(StrategyInstance? s)
        => s is null
            ? "none"
            : $"{s.OwnerId}|{s.DelegatedToId}|{s.Kind}|{s.Method}|{s.StepIndex}|{s.TargetId}";

    private static IEnumerable<string> Phrases(PlayerSnapshot s)
    {
        foreach (var b in s.Known.Concat(s.Unsettled))
        {
            yield return b.Statement;
            if (b.Certainty is { } c) yield return c;
            if (b.Attribution is { } a) yield return a;
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

    private static string Flatten(PlayerSnapshot s) => string.Join('\n', Phrases(s));

    private static void AssertPersistentEquivalence(
        CrimeEmpire.Persistence.Session.PersistentSession loaded,
        CrimeEmpire.Persistence.Session.PersistentSession control)
    {
        Assert.Equal(
            TraceWriter.Render(loaded.InnerSession.World, Variant, false),
            TraceWriter.Render(control.InnerSession.World, Variant, false));
        Assert.Equal(Flatten(loaded.Snapshot()), Flatten(control.Snapshot()));
        Assert.Equal(
            StrategyFingerprint(loaded.InnerSession.World.Get(Vincent).Execution.Strategy),
            StrategyFingerprint(control.InnerSession.World.Get(Vincent).Execution.Strategy));
        Assert.Equal(
            loaded.InnerSession.World.Get(Vincent).Capabilities.Cash,
            control.InnerSession.World.Get(Vincent).Capabilities.Cash);
    }
}
