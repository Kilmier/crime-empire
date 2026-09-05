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
/// <b>THE NATURAL RUN CANNOT REACH THIS, AND THAT IS RECORDED RATHER THAN ENGINEERED AWAY.</b>
/// After the scope review rejected civilians (a civilian holding a violence rumour has no reader —
/// `Fear` moves only through coercion resolution), the eligible population is nearly empty: the
/// executor is excluded, the detective goes the discovery route because she went looking, and the
/// man who ordered it keeps discovery for the reason in
/// <see cref="The_man_who_ordered_it_is_not_learning_it_from_the_street"/>. That leaves one man in
/// the accepted fixture — Salvatore — whose roll is `0.5 × (0.4 + 0.6 × 0.15) ≈ 0.245`, and he
/// fails it at seed 42. **Every variant's trace hash and chosen-action digest is therefore unmoved
/// by this milestone**, which is the honest measure of an inert mechanism rather than a claim that
/// nothing changed. The proofs below are staged for that reason, and the scope named this outcome
/// in advance as acceptable — explicitly forbidding a raised discoverability until it fired.
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

    // ================================================================= helpers

    /// <summary>
    /// Drives a real delegated force operation through the production path until the beating
    /// resolves, leaving its observation opportunities on the queue. Mirrors
    /// <c>ExecutorSuitabilityTests</c>' staging idiom rather than sharing it, per this project's
    /// practice of not sharing helpers across milestone-specific files.
    /// </summary>
    private static World StagedBeating()
    {
        var world = Cast.Build(Seed, "baseline");
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
            VisibleTargets: Array.Empty<string>());
}
