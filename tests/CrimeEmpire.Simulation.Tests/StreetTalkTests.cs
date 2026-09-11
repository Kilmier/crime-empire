using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Strategy;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 022 — The Street Talks. `SourceKind.Rumor` had been in the vocabulary since milestone
/// 003 with nothing producing it, while the entire receiving side sat built and unexercised:
/// `Salience` discounts it hardest, `Cognition.AttributionRank` ranks it lowest so a rumour that
/// becomes a report re-attributes upward, `Provenance.IsTestimony` includes it, and
/// `PlayerNarration` has an arm for it.
///
/// Meanwhile the violence path filed **everything** as `SourceKind.Discovery` sourced to the
/// observer — so a man who works the same street "came across" who beat up a grocer, a day later,
/// by proximity. `Runner.Observe`'s own comment listed "talk on the street" among the traces being
/// rolled against and then declared "there is no rumour network here." That is the category
/// bundling `Provenance.cs` exists to prevent, and it cost four distinct things at once: the light
/// suspicion discount instead of the heavy one, resistance to being argued out of, no attribution
/// upgrade — and, the load-bearing one, **ineligibility for corroboration**, since you can only
/// check what you were told.
///
/// <b>Originally: the natural run could not reach this, and that was recorded rather than engineered
/// away.</b> After the scope review rejected civilians (a civilian holding a violence rumour has no
/// reader — `Fear` moves only through coercion resolution), the eligible population is nearly empty:
/// the executor is excluded, the detective goes the discovery route because she went looking, and the
/// man who ordered it keeps discovery for the reason in
/// <see cref="The_man_who_ordered_it_is_not_learning_it_from_the_street"/>. That leaves one man in
/// the accepted fixture — Salvatore — whose roll is `0.5 × (0.4 + 0.6 × 0.15) ≈ 0.245`. At this
/// milestone's own original commit, he failed it at seed 42, and every variant's trace hash and
/// chosen-action digest was unmoved by the milestone — the honest measure of an inert mechanism, not
/// a claim that nothing would ever change. The proofs below were staged for that reason, and the
/// scope named this outcome in advance as acceptable, explicitly forbidding a raised discoverability
/// until it fired.
///
/// <b>No longer the state at seed 42, since the `Rng.ForOccasion` correction of 2026-09-09</b> (see
/// `docs/milestones/022-the-street-talks.md`'s correction sections). Salvatore's roll now lands at
/// seed 42 in every variant that reaches a violence incident (`baseline`, `watchful-boss`,
/// `disloyal-vincent`, `resentful-tommy`, `capable-angelo`) — the mechanism this milestone built is no
/// longer inert there. **Only Vincent's own owner's-carve-out discovery route and Kane's investigator
/// route remain non-results at seed 42**; neither the owner nor the investigator comes to hold the
/// claim in any variant at that seed. The staged proofs below are unaffected — they never depended on
/// seed 42's own natural-run outcome — and the bounded-seed and complete-production-path tests further
/// down this file are what now demonstrate the mechanism against a real run, at seeds found by search.
/// </summary>
public sealed class StreetTalkTests
{
    private const int Seed = 42;
    private static DateTime At => Cast.Start;

    // ================================================================= what the scheduler decides

    /// <summary>
    /// The provenance is decided where the opportunity is scheduled, not where it is resolved, so
    /// this reads the scheduled payloads directly — deterministic, and free of the discoverability
    /// roll that makes the natural run silent.
    ///
    /// Three routes, three answers. The detective went looking and establishes what she finds. A man
    /// who merely works the street hears it, from the neighbourhood, with nobody to name. The man who
    /// ordered it is neither — see the test below.
    /// </summary>
    [Fact]
    public void Proximity_is_scheduled_as_rumour_and_investigation_as_discovery()
    {
        var world = StagedBeating();
        var scheduled = Drain(world)
            .Where(e => e.Kind == EventKind.ObservationOpportunity)
            .ToDictionary(e => e.OwnerId!, e => e.Payload, StringComparer.Ordinal);

        // Kane clears the investigation threshold, so what she turns up is her own reading and names
        // her as its source.
        Assert.Equal(SourceKind.Discovery, scheduled["kane"].AcquiredAs);
        Assert.Null(scheduled["kane"].AttributedTo);

        // Salvatore works the harbour and did not order this one. He hears it.
        Assert.Equal(SourceKind.Rumor, scheduled["salvatore"].AcquiredAs);

        // And it is attributed to the place, never to a person — INFORMATION_AND_LEGIBILITY.md's own
        // form, "a rumor attributed to a neighborhood or source". A rumour with a man's name on it
        // would be an account, and would give the hearer somebody to go back to.
        Assert.Equal(Cast.Harbour, scheduled["salvatore"].AttributedTo);
        Assert.Null(world.Find(scheduled["salvatore"].AttributedTo!));
    }

    /// <summary>
    /// The case my own scope review missed, found by chasing a regression rather than by reviewing.
    ///
    /// Vincent ordered the beating. When the street says his man used force, he is not learning news
    /// from strangers — he is coming upon the consequence of his own order, which is exactly what
    /// `SourceKind.Discovery` is defined as: "came upon a trace or a consequence afterwards.
    /// Explicitly implies he was *not* present." Milestone 017's accepted ruling already said he
    /// acquires whether-it-was-carried-out "through a report or a discovery roll like anyone else",
    /// and filing him with the passers-by rewrote that in passing.
    ///
    /// **Recorded because the consequence was large and the discovery order was unflattering:** with
    /// the owner on rumour, his whole concealment calculus shrank — every term scaled by how sure he
    /// is of what he would be hiding — and `baseline` and `watchful-boss` collapsed to identical
    /// chosen actions, tripping the distinctness guard that exists so an honest non-result cannot
    /// quietly become "nothing distinguishes anything". The categorisation argument above stands on
    /// its own, but it was a failing test that sent me looking for it.
    /// </summary>
    [Fact]
    public void The_man_who_ordered_it_is_not_learning_it_from_the_street()
    {
        var world = StagedBeating();
        var scheduled = Drain(world)
            .Where(e => e.Kind == EventKind.ObservationOpportunity)
            .ToDictionary(e => e.OwnerId!, e => e.Payload, StringComparer.Ordinal);

        Assert.Equal(SourceKind.Discovery, scheduled["vincent"].AcquiredAs);
        Assert.Null(scheduled["vincent"].AttributedTo);

        // He is in the district and below the investigation threshold, so nothing but the ownership
        // carve-out distinguishes him from Salvatore, who is scheduled as rumour on the same event.
        Assert.True(world.Get("vincent").Capabilities[Skill.Investigation] < 0.4);
        Assert.Equal(SourceKind.Rumor, scheduled["salvatore"].AcquiredAs);
    }

    // ================================================================= what it unlocks

    /// <summary>
    /// The milestone's point, and the reason the provenance is worth getting right rather than a
    /// labelling nicety: <b>you can only corroborate what you were told.</b>
    ///
    /// `FromRelationship`'s corroboration branch takes the thinnest belief whose source kind
    /// `IsTestimony()`, on the stated reasoning that there is nothing to corroborate about your own
    /// eyes. A violence belief acquired as discovery is therefore structurally ineligible — the man
    /// most worth asking about is the one thing he cannot ask about. As rumour it becomes an
    /// ordinary question he can put to somebody.
    ///
    /// Two worlds identical but for how Salvatore came by the same claim about the same man.
    /// </summary>
    [Fact]
    public void A_violence_belief_can_be_corroborated_as_rumour_and_cannot_as_discovery()
    {
        Assert.False(Corroborates(SourceKind.Discovery, "salvatore"),
            "a belief he established himself should offer nothing to corroborate");
        Assert.True(Corroborates(SourceKind.Rumor, Cast.Harbour),
            "talk going round the neighbourhood is exactly what a man checks with somebody");
    }

    /// <summary>
    /// A rumour introduces no acquaintance. Holding one names a place, and a place is not somebody
    /// he can put a question to — so the belief cannot smuggle a new person into
    /// <c>Acquaintance.KnownTo</c>, which is the single derivation for who a candidate may target
    /// (`DESIGN_DECISIONS.md`, settled by milestone 009's third correction after two failures).
    /// </summary>
    [Fact]
    public void Hearing_talk_makes_nobody_nameable()
    {
        var world = Cast.Build(Seed, "baseline");
        var salvatore = world.Get("salvatore");
        var before = Acquaintance.KnownTo(world, salvatore).ToList();

        salvatore.Cognition.Learn(
            new Claim(ClaimKind.PersonUsedViolence, "nobody-at-all", Cast.Grocery, 7),
            Stance.Suspects, 0.35, SourceKind.Rumor, Cast.Harbour, At);

        // Neither the place it is attributed to nor a name the world does not know is added.
        var after = Acquaintance.KnownTo(world, salvatore).ToList();
        Assert.Equal(before, after);
        Assert.DoesNotContain(Cast.Harbour, after);
    }

    /// <summary>
    /// What the player is told about it: that it is going round, and where — never who, and never a
    /// number. `PlayerNarration`'s rumour arm was a catch-all fallback from milestone 003 until this
    /// milestone made it reachable and gave it its own wording.
    /// </summary>
    [Fact]
    public void A_rumour_reads_as_talk_and_names_no_man()
    {
        var record = new InformationRecord(
            new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 7),
            Stance.Suspects, 0.35, SourceKind.Rumor, Cast.Harbour, At);

        string rendered = PlayerNarration.Attribute(record, id => id, Pronouns.He);

        Assert.Contains("going round", rendered, StringComparison.Ordinal);
        Assert.Contains(Cast.Harbour, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("told", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("saw", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(".", rendered.Replace("...", ""), StringComparison.Ordinal);
    }

    // ================================================================= complete production path

    /// <summary>
    /// <b>Correction found reviewing milestone 022, 2026-09-09 — <see cref="Rng.ForOccasion"/>'s
    /// finalizer.</b> Before this fix, three occasion keys differing only in their trailing observer
    /// id — Salvatore's, Vincent's and Kane's, on this exact event — could never all succeed together
    /// at any seed: the old linear finalizer let the world seed's own contribution cancel out of any
    /// two keys' XOR difference, locking every pair of streams into one fixed, seed-independent
    /// relationship no seed could escape. The milestone archive recorded the search that found this
    /// (thousands of seeds, never once a joint success) as an open, unexplained property rather than a
    /// defect, because fixing the shared RNG was out of that correction's authorization. It is not out
    /// of this one's: <see cref="Rng.ForOccasion"/>'s doc comment has the full account.
    ///
    /// This is the falsifying half of that fix, kept intentionally thin: it proves the joint case that
    /// used to be structurally impossible now occurs, at one deterministic seed, through the real
    /// production loop — not a claim about how often, which the class doc above already forbids
    /// asserting. <see cref="The_street_talk_survives_its_complete_production_path"/> below is the
    /// full positive proof, with every boundary the milestone drew checked against this same seed;
    /// this test exists so the falsifying claim has its own narrow, easily mutation-checked witness
    /// rather than being read out of the larger test's assertions.
    ///
    /// <b>Mutation-checked.</b> Reverting <see cref="Rng.ForOccasion"/>'s finalizer to the old single
    /// linear step (<c>h ^= h &gt;&gt; 15</c>) and re-running this test at this identical seed fails
    /// it — restored and confirmed, not merely asserted.
    /// </summary>
    [Fact]
    public void Three_observers_of_the_same_event_can_succeed_together_at_a_deterministic_seed()
    {
        const int SeedWhereAllThreeLand = 222;
        var world = StagedBeating(SeedWhereAllThreeLand);

        var executor = world.Get("tommy");
        var salvatore = world.Get("salvatore");
        var vincent = world.Get("vincent");
        var kane = world.Get("kane");

        var violenceEvent = world.TruthLog.Single(e => e.Kind == "violence");
        var violenceClaim = new Claim(ClaimKind.PersonUsedViolence, executor.Id, Cast.Grocery, violenceEvent.Id);
        var witnessClaim = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, executor.Id, violenceEvent.Id);

        Runner.Run(world, world.Now.AddDays(2));

        // The falsifying claim itself, and nothing more: all three genuinely came to hold something
        // about this one event, at one seed, through the real loop — the exact joint outcome the old
        // finalizer made permanently unreachable.
        Assert.NotNull(salvatore.Cognition.Find(violenceClaim));
        Assert.NotNull(vincent.Cognition.Find(violenceClaim));
        Assert.NotNull(kane.Cognition.Find(witnessClaim));
    }

    /// <summary>
    /// Milestone 022's own archive named this the thing most worth checking and left it undone: every
    /// proof above reads the <em>scheduled payload</em> directly, which pins what the scheduler
    /// decided and never proves the real loop carries it through — that <see cref="Runner.Observe"/>
    /// actually resolves the queued <see cref="EventKind.ObservationOpportunity"/>, actually rolls
    /// against it, and actually lands the claim in the observer's own <see cref="Cognition"/> as
    /// <see cref="SourceKind.Rumor"/>, attributed to the district rather than to him. Authorized
    /// 2026-09-08, narrowly: this proves the existing distinction survives its complete production
    /// path, and changes nothing about the mechanism itself.
    ///
    /// <b>Seed moved a second time, 2026-09-09, and the boundary assertions strengthened from guards
    /// to positives.</b> Seed 25 was a pre-fix search result: under the old, linear finalizer, at most
    /// one of Salvatore's, Vincent's and Kane's rolls on this event could ever land, so the two
    /// boundary checks below could only ever be guards ("null or Discovery") rather than positive
    /// proof — there was no seed that could make them anything else. The corrected finalizer (see
    /// <see cref="Rng.ForOccasion"/>) removes that structural lock, and
    /// <see cref="Three_observers_of_the_same_event_can_succeed_together_at_a_deterministic_seed"/>
    /// above is the narrow proof that a joint-success seed exists. This test now uses that same seed
    /// so its own boundary assertions can be positive too: not just "the owner and the investigator
    /// never come to hold this as talk," but "they hold it as Discovery, specifically, in the same run
    /// where Salvatore genuinely holds it as talk" — the stronger claim a guard can never make.
    ///
    /// <b>The roll is still a genuine, irreducible Bernoulli draw, and the seed is still found by
    /// search, not by casting.</b> No character stat is pushed toward certainty and no discoverability
    /// coefficient is touched — the occasion key is built entirely from strategy bookkeeping the RNG
    /// never reads, so it is identical at every seed; only the seed moves the three rolls. This test
    /// neither touches, re-derives, nor depends on what seed 42 itself now does — see the class doc
    /// comment above for what seed 42's own outcome is since the `Rng.ForOccasion` correction
    /// (Salvatore's route now lands there; the owner's and the investigator's still do not).
    /// </summary>
    [Fact]
    public void The_street_talk_survives_its_complete_production_path()
    {
        // Found by search, not by casting — see the summary above. All three rolls land at this seed.
        const int SeedWhereAllThreeLand = 222;
        var world = StagedBeating(SeedWhereAllThreeLand);

        var executor = world.Get("tommy");
        var salvatore = world.Get("salvatore");
        var vincent = world.Get("vincent");
        var kane = world.Get("kane");

        var violenceEvent = world.TruthLog.Single(e => e.Kind == "violence");
        var violenceClaim = new Claim(ClaimKind.PersonUsedViolence, executor.Id, Cast.Grocery, violenceEvent.Id);
        var witnessClaim = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, executor.Id, violenceEvent.Id);

        // The real loop, not a hand inspection of the queue: drains and resolves every scheduled
        // event up to and past the observation opportunities, exactly as a natural run would.
        Runner.Run(world, world.Now.AddDays(2));

        // The point of the milestone, proven end to end: the street worker actually comes to hold
        // the executor's name, as talk, attributed to where he heard it rather than to himself.
        var heard = salvatore.Cognition.Find(violenceClaim);
        Assert.NotNull(heard);
        Assert.Equal(SourceKind.Rumor, heard!.SourceKind);
        Assert.Equal(Cast.Harbour, heard.SourceId);
        Assert.NotEqual(salvatore.Id, heard.SourceId);

        // The two boundaries the milestone drew, now checked positively: at this seed both the owner
        // and the investigator also come to hold the claim — genuinely, through the same real loop —
        // and they hold it as Discovery, never as talk. The joint occurrence is exactly what the old
        // finalizer made impossible to witness.
        var ownerRead = vincent.Cognition.Find(violenceClaim);
        Assert.NotNull(ownerRead);
        Assert.Equal(SourceKind.Discovery, ownerRead!.SourceKind);
        Assert.Equal(vincent.Id, ownerRead.SourceId);

        var investigatorRead = kane.Cognition.Find(witnessClaim);
        Assert.NotNull(investigatorRead);
        Assert.Equal(SourceKind.Discovery, investigatorRead!.SourceKind);
        Assert.Equal(kane.Id, investigatorRead.SourceId);

        // A rumour still names no man he could go and find — Hearing_talk_makes_nobody_nameable's
        // claim, re-checked against a belief the real loop produced rather than one staged directly.
        Assert.DoesNotContain(Cast.Harbour, Acquaintance.KnownTo(world, salvatore));
    }

    /// <summary>
    /// Determinism, checked at the level this correction actually touched. The same seed against the
    /// same staged scenario must reach the identical three outcomes through the real loop — not merely
    /// "a seed exists", which the test above already proves, but "this seed's result is reproducible."
    /// Two independently built worlds, never one reused, so nothing but the seed and the occasion keys
    /// carries information between them.
    /// </summary>
    [Fact]
    public void Identical_seed_and_occasion_key_reproduce_the_same_observation_outcomes()
    {
        const int SeedWhereAllThreeLand = 222;

        static (Stance? Stance, SourceKind? Source, double? Confidence) Read(World world, string observerId, Claim claim)
        {
            var record = world.Get(observerId).Cognition.Find(claim);
            return (record?.Stance, record?.SourceKind, record?.Confidence);
        }

        static World RunOnce()
        {
            var world = StagedBeating(SeedWhereAllThreeLand);
            Runner.Run(world, world.Now.AddDays(2));
            return world;
        }

        var a = RunOnce();
        var b = RunOnce();

        var executorA = a.Get("tommy");
        var violenceEventA = a.TruthLog.Single(e => e.Kind == "violence");
        var violenceClaimA = new Claim(ClaimKind.PersonUsedViolence, executorA.Id, Cast.Grocery, violenceEventA.Id);
        var witnessClaimA = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, executorA.Id, violenceEventA.Id);

        var executorB = b.Get("tommy");
        var violenceEventB = b.TruthLog.Single(e => e.Kind == "violence");
        var violenceClaimB = new Claim(ClaimKind.PersonUsedViolence, executorB.Id, Cast.Grocery, violenceEventB.Id);
        var witnessClaimB = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, executorB.Id, violenceEventB.Id);

        // Two identically-built fresh worlds diverge in nothing before this point, so the event this
        // milestone's claims are keyed on should not even shift identity, let alone outcome.
        Assert.Equal(violenceEventA.Id, violenceEventB.Id);

        Assert.Equal(Read(a, "salvatore", violenceClaimA), Read(b, "salvatore", violenceClaimB));
        Assert.Equal(Read(a, "vincent", violenceClaimA), Read(b, "vincent", violenceClaimB));
        Assert.Equal(Read(a, "kane", witnessClaimA), Read(b, "kane", witnessClaimB));
    }

    /// <summary>
    /// Insertion stability, the property <see cref="Rng.ForOccasion"/>'s own doc comment names as the
    /// reason an occasion key may never carry a global scheduling identifier: a causally unrelated
    /// event scheduled anywhere else must not reroll this one. Proven directly rather than assumed
    /// from the key format — a dummy <c>RoleReview</c> for a character with no bearing on this
    /// operation is scheduled ahead of everything else in the queue, so it resolves, and decides,
    /// first (confirmed below: Nunzio has a real decision in the disturbed run and none in the
    /// undisturbed one), while the three observers' occasion-key-driven outcomes — built from
    /// strategy-local bookkeeping the insertion never touches — do not move.
    /// </summary>
    [Fact]
    public void Unrelated_event_insertion_does_not_reroll_an_observation()
    {
        const int SeedWhereAllThreeLand = 222;

        static World Staged(bool withUnrelatedInsertion)
        {
            var world = StagedBeating(SeedWhereAllThreeLand);
            if (withUnrelatedInsertion)
                world.Queue.Schedule(world.Now, EventKind.RoleReview, "nunzio", "test: unrelated insertion");
            Runner.Run(world, world.Now.AddDays(2));
            return world;
        }

        static (bool NunzioDecided, SourceKind? Salvatore, SourceKind? Vincent, SourceKind? Kane) Outcomes(World world)
        {
            var executor = world.Get("tommy");
            var violenceEvent = world.TruthLog.Single(e => e.Kind == "violence");
            var violenceClaim = new Claim(ClaimKind.PersonUsedViolence, executor.Id, Cast.Grocery, violenceEvent.Id);
            var witnessClaim = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, executor.Id, violenceEvent.Id);
            return (
                world.Decisions.Any(d => d.ActorId == "nunzio"),
                world.Get("salvatore").Cognition.Find(violenceClaim)?.SourceKind,
                world.Get("vincent").Cognition.Find(violenceClaim)?.SourceKind,
                world.Get("kane").Cognition.Find(witnessClaim)?.SourceKind);
        }

        var undisturbed = Outcomes(Staged(withUnrelatedInsertion: false));
        var disturbed = Outcomes(Staged(withUnrelatedInsertion: true));

        // The insertion did perturb the run — this is not a no-op test.
        Assert.False(undisturbed.NunzioDecided);
        Assert.True(disturbed.NunzioDecided);

        // And none of that reached the occasion keys: the same three observers land the same outcomes.
        Assert.Equal(undisturbed.Salvatore, disturbed.Salvatore);
        Assert.Equal(undisturbed.Vincent, disturbed.Vincent);
        Assert.Equal(undisturbed.Kane, disturbed.Kane);
    }

    // ================================================================= helpers

    /// <summary>
    /// Drives a real delegated force operation through the production path until the beating
    /// resolves, leaving its observation opportunities on the queue. Mirrors
    /// <c>ExecutorSuitabilityTests</c>' staging idiom rather than sharing it, per this project's
    /// practice of not sharing helpers across milestone-specific files.
    /// </summary>
    private static World StagedBeating(int seed = Seed)
    {
        var world = Cast.Build(seed, "baseline");
        var owner = world.Get("vincent");
        var executor = world.Get("tommy");

        var ctx = Context(world, owner);
        var start = new Candidate($"start:tribute:{Cast.Grocery}:Force", ActionKind.StartStrategy, "test",
            "lean on the grocery")
        {
            TargetId = Cast.Grocery,
            Strategy = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            Method = CoercionMethod.Force,
        };
        Commit.Apply(world, owner, start, ctx.Agenda, ctx, new List<string>());
        var s = owner.Execution.Strategy!;

        var delegateCtx = Context(world, owner);
        Commit.Apply(world, owner,
            new Candidate($"delegate:{s.Kind}:tommy", ActionKind.DelegateStrategy, "test", "hand it to Tommy")
            {
                TargetId = "tommy",
                Strategy = s.Kind,
                Method = s.Method,
                Domain = s.Domain,
                RequiredCrew = 1,
            },
            delegateCtx.Agenda, delegateCtx, new List<string>());

        // approach, demand, press — force resolves on the third.
        for (int i = 0; i < 3 && s.PendingStepEventId is not null; i++)
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

        Assert.Contains(world.TruthLog, e => e.Kind == "violence");
        return world;
    }

    /// <summary>
    /// Whether the corroboration generator offers to check this violence claim with somebody, given
    /// how the holder came by it. Everything else about the two worlds is identical.
    /// </summary>
    private static bool Corroborates(SourceKind how, string sourceId)
    {
        var world = Cast.Build(Seed, "baseline");
        var salvatore = world.Get("salvatore");

        salvatore.Cognition.Learn(
            new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 7),
            Stance.Suspects, 0.35, how, sourceId, At);

        return Generators.GenerateAll(Context(world, salvatore, "tommy", "vincent"))
            .Any(c => c.Kind == ActionKind.SeekCorroboration
                      && c.Generator == "FromRelationship"
                      && c.AboutClaim is { } a && a.Kind == ClaimKind.PersonUsedViolence);
    }

    private static List<ScheduledEvent> Drain(World world)
    {
        var drained = new List<ScheduledEvent>();
        while (world.Queue.Next(world.Now.AddYears(1)) is { } ev) drained.Add(ev);
        return drained;
    }

    private static GeneratorContext Context(World world, Character actor, params string[] acquainted)
        => new(
            actor.View,
            Salience.Perceive(actor, world.Now),
            new Agenda(AgendaKind.DischargeResponsibility, "keep the family earning", "test", Cast.Harbour),
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
            SubordinateIds: Array.Empty<string>(),
            OrgMemberIds: acquainted,
            AcquaintedIds: acquainted,
            ReportsSent: Array.Empty<Report>(),
            RequestsMade: Array.Empty<InformationRequest>(),
            VisibleTargets: Array.Empty<string>(),
            AvailableSubordinateIds: Array.Empty<string>());
}
