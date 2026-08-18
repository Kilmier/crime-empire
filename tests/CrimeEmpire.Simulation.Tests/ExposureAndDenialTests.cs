using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Strategy;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 010: a man can act on his own exposure, and a denial is priced from the incident it
/// is about rather than from the worst thing in his head.
///
/// Two structural defects, pinned separately because they are independent and each was live on its
/// own. The first is that <c>Strategies.AdvanceConceal</c>'s "quiet the witnesses" step silenced
/// nobody — not even in the mind of the man running it — because the instance could not name the
/// incident it was concealing and nothing could lower a character's confidence in his own
/// inference. The second is that <c>Utility</c> priced a denial on a maximum over every
/// <see cref="ClaimKind.WitnessSawIncident"/> the actor held, whatever incident it belonged to.
///
/// The discretion roll is controlled by casting rather than by seed-hunting. It is
/// <c>discretion + rng.Range(-0.15, 0.15) &gt; 0.45</c> over a half-open range, so Salvatore
/// (0.65) is clean at every seed and Tommy (0.30) is clumsy at every seed — the latter by
/// arithmetic, since even the largest draw leaves him exactly at the threshold and the comparison
/// is strict. That is itself a finding, and it is recorded in the milestone archive.
/// </summary>
public sealed class ExposureAndDenialTests
{
    // ================================================================ the instance names its incident

    [Fact]
    public void Committing_a_concealment_records_the_incident_on_the_instance()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var incident = new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery, 7);

        Start(world, tommy, incident);

        Assert.Equal(7, tommy.Execution.Strategy!.SourceEventId);
    }

    /// <summary>
    /// A claim with no event id names no incident, and must not become event 0 — every idless claim
    /// would then share one incident, which is the scan defect this milestone fixes wearing a
    /// different hat. The concealment is still allowed to run; it simply has no witnesses to quiet.
    /// </summary>
    [Fact]
    public void An_incident_claim_with_no_event_id_leaves_the_instance_naming_no_incident()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");

        Start(world, tommy, new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery));

        Assert.NotNull(tommy.Execution.Strategy);
        Assert.Null(tommy.Execution.Strategy!.SourceEventId);
    }

    // ================================================================ quieting witnesses

    [Fact]
    public void Quieting_the_witnesses_cleanly_lowers_what_the_concealer_thinks_the_street_saw()
    {
        // Salvatore, because his discretion clears the threshold at every draw.
        var world = Cast.Build(seed: 1, "baseline");
        var concealer = world.Get("salvatore");
        var witness = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, concealer.Id, 7);
        Believe(concealer, world, witness, 0.6);

        QuietWitnesses(world, concealer, incidentId: 7);

        Assert.Equal(0.4, concealer.Cognition.ConfidenceIn(witness), 6);
    }

    /// <summary>
    /// It may fail, and failing is not nothing. The step already models a clumsy cleanup as
    /// actively worse than no cleanup — exposure rises and completion says so — and the belief
    /// follows the same model. Being wrong stays possible in both directions: nothing about the
    /// world has changed either way.
    /// </summary>
    [Fact]
    public void A_clumsy_cleanup_moves_it_the_other_way()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var concealer = world.Get("tommy");
        var witness = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, concealer.Id, 7);
        Believe(concealer, world, witness, 0.6);

        QuietWitnesses(world, concealer, incidentId: 7);

        Assert.Equal(0.7, concealer.Cognition.ConfidenceIn(witness), 6);
    }

    /// <summary>
    /// Milestone 005's ruling, one level down: the incident is the identity. A man cleaning up after
    /// one beating does not become calmer about a different one, even at the same shop.
    /// </summary>
    [Fact]
    public void Quieting_the_witnesses_leaves_every_other_incident_alone()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var concealer = world.Get("salvatore");
        var thisOne = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, concealer.Id, 7);
        var anotherAtTheSameShop = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, concealer.Id, 9);
        var elsewhere = new Claim(ClaimKind.WitnessSawIncident, Cast.Bakery, concealer.Id, 11);
        Believe(concealer, world, thisOne, 0.6);
        Believe(concealer, world, anotherAtTheSameShop, 0.6);
        Believe(concealer, world, elsewhere, 0.6);

        QuietWitnesses(world, concealer, incidentId: 7);

        Assert.Equal(0.4, concealer.Cognition.ConfidenceIn(thisOne), 6);
        Assert.Equal(0.6, concealer.Cognition.ConfidenceIn(anotherAtTheSameShop), 6);
        Assert.Equal(0.6, concealer.Cognition.ConfidenceIn(elsewhere), 6);
    }

    /// <summary>
    /// Ruling 2. Quieting witnesses changes a belief, not the world: no trace is removed, the truth
    /// log gains only the ordinary record of the attempt, and nobody else's cognition moves. The
    /// grocer who watched it happen is still holding exactly what he held.
    /// </summary>
    [Fact]
    public void Quieting_the_witnesses_touches_no_trace_no_other_cognition_and_removes_nothing_from_the_truth_log()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var concealer = world.Get("salvatore");

        // A real incident in the truth log, carrying the very trace the step is named for — and the
        // incident the concealment will name, so a step that reached for its traces would find
        // them. Staging the incident with an id nothing had recorded is how the first version of
        // this test passed against a mutation that deleted the witness trace.
        var recorded = world.Record("violence", concealer.Id, Cast.Grocery, "it happened",
            new Trace("witness", "people on the street saw it", Cast.Harbour, 0.55),
            new Trace("damage", "the front of the shop was wrecked", Cast.Harbour, 0.65));

        var witness = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, concealer.Id, recorded.Id);
        Believe(concealer, world, witness, 0.6);
        var bystander = world.Get("marco");
        bystander.Cognition.Learn(witness, Stance.Knows, 1.0, SourceKind.Witness, bystander.Id, world.Now);

        var truthBefore = world.TruthLog.Select(Describe).ToList();
        var othersBefore = Cognitions(world, except: concealer.Id);

        QuietWitnesses(world, concealer, incidentId: recorded.Id);

        // His own view moved, so the check below is about what else did — not about nothing running.
        Assert.Equal(0.4, concealer.Cognition.ConfidenceIn(witness), 6);

        Assert.Equal(othersBefore, Cognitions(world, except: concealer.Id));
        Assert.Equal(1.0, bystander.Cognition.ConfidenceIn(witness), 6);

        // The truth log is appended to — the attempt itself happened — and nothing already in it,
        // traces included, is altered or removed.
        Assert.Equal(truthBefore, world.TruthLog.Take(truthBefore.Count).Select(Describe).ToList());
        Assert.Contains(recorded.Traces, t => t.Kind == "witness");
    }

    private static string Describe(WorldEvent e)
        => $"{e.Id}|{e.Kind}|{e.ActorId}|{e.TargetId}|{e.Summary}|" +
           string.Join(",", e.Traces.Select(t => $"{t.Kind}:{t.Description}:{t.Discoverability}"));

    /// <summary>
    /// The second step is about records, not people. Driving both steps of a real instance also
    /// checks that only the first one is wired.
    /// </summary>
    [Fact]
    public void Tidying_the_paperwork_does_not_touch_what_he_thinks_the_street_saw()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var concealer = world.Get("salvatore");
        var witness = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, concealer.Id, 7);
        Believe(concealer, world, witness, 0.6);
        var s = Start(world, concealer, new Claim(ClaimKind.PersonUsedViolence, concealer.Id, Cast.Grocery, 7));

        Advance(world, concealer, s);
        double afterFirstStep = concealer.Cognition.ConfidenceIn(witness);
        Advance(world, concealer, s);

        Assert.Equal(0.4, afterFirstStep, 6);
        Assert.Equal(afterFirstStep, concealer.Cognition.ConfidenceIn(witness), 6);
        Assert.Equal(2, s.StepIndex);
    }

    /// <summary>
    /// Whose belief moves. The executor's: he is the man who went out and did it and came away with
    /// a view of how it went, and a delegator who sent him learns nothing here — the same rule
    /// <see cref="Strategies"/> states at the approach step and at the beating itself. If he wants
    /// to know how the cleanup went, his man has to tell him through the channel.
    ///
    /// Staged rather than taken from a natural run, because concealment is never delegated in the
    /// fixture, so owner and executor coincide everywhere and a mutation swapping one for the other
    /// passed the whole suite. Found by the mutation check, not by reading the code.
    ///
    /// Note the deliberate asymmetry with the exposure the step applies beside it, which goes to the
    /// owner. Pressure is motivational state the simulation applies to whoever carries the
    /// consequence; a belief is cognition and reaches nobody who was not there.
    /// </summary>
    [Fact]
    public void A_delegated_cleanup_moves_the_executors_view_and_not_the_delegators()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var owner = world.Get("vincent");
        var executor = world.Get("salvatore");   // clean at every draw
        var incident = new Claim(ClaimKind.PersonUsedViolence, executor.Id, Cast.Grocery, 7);
        var witness = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, executor.Id, 7);
        Believe(owner, world, witness, 0.6);
        Believe(executor, world, witness, 0.6);

        var s = Start(world, owner, incident);
        s.DelegatedToId = executor.Id;
        Advance(world, executor, s);

        Assert.Equal(0.4, executor.Cognition.ConfidenceIn(witness), 6);
        Assert.Equal(0.6, owner.Cognition.ConfidenceIn(witness), 6);
    }

    [Fact]
    public void A_concealment_that_names_no_incident_quiets_nothing()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var concealer = world.Get("salvatore");
        var witness = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, concealer.Id, 7);
        Believe(concealer, world, witness, 0.6);
        var s = Start(world, concealer, new Claim(ClaimKind.PersonUsedViolence, concealer.Id, Cast.Grocery));

        Advance(world, concealer, s);

        Assert.Null(s.SourceEventId);
        Assert.Equal(0.6, concealer.Cognition.ConfidenceIn(witness), 6);
    }

    // ================================================================ revising one's own conclusion

    /// <summary>
    /// The gap that made defect 1 possible, pinned so a future simplification cannot route the step
    /// back through <see cref="Cognition.Learn"/>. Learn's override rule discards a less confident
    /// inference, so a man's own conclusions could only ever firm up.
    /// </summary>
    [Fact]
    public void Learn_cannot_lower_a_mans_confidence_in_his_own_inference()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var claim = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, tommy.Id, 7);
        Believe(tommy, world, claim, 0.6);

        tommy.Cognition.Learn(claim, Stance.Believes, 0.2, SourceKind.Inference, tommy.Id, world.Now);

        Assert.Equal(0.6, tommy.Cognition.ConfidenceIn(claim), 6);
    }

    /// <summary>
    /// What he did, what he saw, and what somebody told him are none of them his to simply revise.
    /// The first two are defended by <see cref="Provenance.OverridesPriorRecord"/> and
    /// <see cref="Provenance.ProtectsStance"/>; the last is an account he has to be argued out of
    /// through <see cref="Cognition.Receive"/>, and a quieter second route would let a wishful
    /// character discard testimony without the disagreement ever being recorded.
    /// </summary>
    [Theory]
    [InlineData(SourceKind.Participant)]
    [InlineData(SourceKind.Witness)]
    [InlineData(SourceKind.FirstHandTestimony)]
    [InlineData(SourceKind.Report)]
    [InlineData(SourceKind.Rumor)]
    public void What_he_did_saw_or_was_told_is_not_his_to_revise(SourceKind provenance)
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var claim = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, tommy.Id, 7);
        tommy.Cognition.Learn(claim, Stance.Believes, 0.6, provenance, tommy.Id, world.Now);

        var revised = tommy.Cognition.Revise(claim, 0.1, tommy.Id, world.Now);

        Assert.Null(revised);
        Assert.Equal(0.6, tommy.Cognition.ConfidenceIn(claim), 6);
    }

    /// <summary>
    /// His own reasoning and his own reading of a trace are both his to think better of.
    ///
    /// <b>Discovery was refused until milestone 011</b>, and this theory case is the correction.
    /// Admitting Inference alone put Discovery in with Participant and Witness, which is the exact
    /// bundle <see cref="Provenance"/> exists to prevent — its other four predicates all say a
    /// discovery is a reading that can be weak, wrong and reconsidered, and a fifth saying it was
    /// unrevisable contradicted them. It surfaced through implementation rather than inspection:
    /// <c>AdvanceInvestigation</c>'s cold-trail branch demotes a lead the investigator found herself,
    /// so the repair written for it in milestone 011 was still a no-op until this changed.
    /// </summary>
    [Theory]
    [InlineData(SourceKind.Inference)]
    [InlineData(SourceKind.Discovery)]
    public void His_own_reading_is_his_to_revise(SourceKind provenance)
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var claim = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, tommy.Id, 7);
        tommy.Cognition.Learn(claim, Stance.Believes, 0.6, provenance, tommy.Id, world.Now);

        var revised = tommy.Cognition.Revise(claim, 0.1, tommy.Id, world.Now);

        Assert.NotNull(revised);
        Assert.Equal(0.1, tommy.Cognition.ConfidenceIn(claim), 6);
        Assert.Equal(provenance, revised!.SourceKind);
    }

    /// <summary>Somebody else's conclusion is not his to revise either, however it is filed.</summary>
    [Fact]
    public void Another_mans_inference_is_not_his_to_revise()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var claim = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, tommy.Id, 7);
        tommy.Cognition.Learn(claim, Stance.Believes, 0.6, SourceKind.Inference, "vincent", world.Now);

        Assert.Null(tommy.Cognition.Revise(claim, 0.1, tommy.Id, world.Now));
        Assert.Equal(0.6, tommy.Cognition.ConfidenceIn(claim), 6);
    }

    /// <summary>
    /// A revision is a reconsideration and nothing else: the stance stands, the acquisition time
    /// stands, and only the reconsideration stamp moves. The stamp is load-bearing —
    /// <see cref="Reporting.NeedsConveying"/> reads it, so a man who has changed his mind has
    /// something to say again.
    /// </summary>
    [Fact]
    public void Revising_moves_the_reconsideration_stamp_and_leaves_stance_and_acquisition_alone()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var claim = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, tommy.Id, 7);
        var acquired = world.Now;
        Believe(tommy, world, claim, 0.6);

        var later = acquired.AddDays(3);
        var revised = tommy.Cognition.Revise(claim, 0.4, tommy.Id, later);

        Assert.NotNull(revised);
        Assert.Equal(Stance.Believes, revised!.Stance);
        Assert.Equal(acquired, revised.AcquiredAt);
        Assert.Equal(later, revised.ReconsideredAt);
        Assert.True(Reporting.NeedsConveying(Array.Empty<Report>(), "vincent", revised));
    }

    [Fact]
    public void A_revision_stays_inside_the_confidence_range()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var claim = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, tommy.Id, 7);
        Believe(tommy, world, claim, 0.6);

        Assert.Equal(0.0, tommy.Cognition.Revise(claim, -3.0, tommy.Id, world.Now)!.Confidence, 6);
        Assert.Equal(1.0, tommy.Cognition.Revise(claim, 4.0, tommy.Id, world.Now)!.Confidence, 6);
    }

    // ================================================================ pricing a denial

    /// <summary>
    /// Defect 2, through the production scorer. The actor holds two witness beliefs: a thin one
    /// about the incident he would be denying, and a near-certain one about an unrelated incident.
    /// The denial must be priced from the first. Before this milestone the scan took a maximum over
    /// every witness belief he held and the unrelated one decided it — the same shape as the
    /// <c>SeekCorroboration</c> scan `404b416` fixed, where a question was priced from the weakest
    /// unrelated belief in the asker's head.
    ///
    /// Asserted as a comparison between staged worlds rather than against a hardcoded number, so
    /// the test pins the scoping and not a coefficient.
    /// </summary>
    [Fact]
    public void A_denial_is_priced_from_the_witnesses_to_the_incident_it_is_about()
    {
        double thinHereAndDamningElsewhere = DenialRisk(aboutThisIncident: 0.1, aboutAnother: 0.95);
        double thinEverywhere = DenialRisk(aboutThisIncident: 0.1, aboutAnother: 0.1);
        double damningHere = DenialRisk(aboutThisIncident: 0.95, aboutAnother: 0.1);

        // What he believes about a different incident does not price this denial at all.
        Assert.Equal(thinEverywhere, thinHereAndDamningElsewhere, 9);

        // And what he believes about this one prices it heavily, so the scoping is not vacuous.
        Assert.True(damningHere < thinEverywhere - 1.0,
            $"the incident's own witnesses must dominate the price; {damningHere:0.000} vs {thinEverywhere:0.000}");
    }

    /// <summary>
    /// The default event id is not an incident. Matching on it would let every unattributed claim
    /// share one incident, which is the same scan back at a smaller scale.
    /// </summary>
    [Fact]
    public void A_suppressed_claim_naming_no_incident_is_not_priced_from_unrelated_idless_beliefs()
    {
        var suppressed = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery);

        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        tommy.Cognition.Learn(suppressed, Stance.Knows, 1.0, SourceKind.Participant, tommy.Id, world.Now);
        Believe(tommy, world, new Claim(ClaimKind.WitnessSawIncident, Cast.Bakery, tommy.Id), 0.95);
        double withIdlessWitness = Risk(Deny(suppressed), tommy, world);

        var clean = Cast.Build(seed: 1, "baseline");
        var cleanTommy = clean.Get("tommy");
        cleanTommy.Cognition.Learn(suppressed, Stance.Knows, 1.0, SourceKind.Participant, cleanTommy.Id, clean.Now);
        double withNoWitnessAtAll = Risk(Deny(suppressed), cleanTommy, clean);

        Assert.Equal(withNoWitnessAtAll, withIdlessWitness, 9);
    }

    // ================================================================ the player boundary, ruling 5

    /// <summary>
    /// Ruling 5. The concealment step this milestone wired moves a belief that is nobody's but the
    /// concealer's, so the boundary is re-checked after a natural run in which that step has
    /// actually run — every variant, every viewpoint.
    ///
    /// Asserted at the claim level rather than on rendered prose, and that distinction is a finding
    /// of this milestone's own self-review rather than a preference. The first version of this test
    /// forbade the narrator's phrasing of each witness claim from appearing in
    /// <c>IntelligenceWriter</c>'s output; the phrase never appears there for any viewpoint,
    /// including the ones who hold the claim, so the test could not fail and did not fail when the
    /// view was mutated to read every character's cognition. It was a false-assurance test of the
    /// exact shape <c>REVIEW_LEDGER.md</c> names, written during a milestone whose own ruling 8
    /// requires mutation-checking. It is only recorded because the mutation check ran.
    ///
    /// Extends <c>PlayerSessionTests.The_snapshot_is_bounded_by_what_the_viewpoint_character_holds</c>,
    /// which asserts the same property for one viewpoint. Six characters holding six different
    /// amounts of the same run is where a source limit derived per-surface would show.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void No_viewpoint_is_shown_a_belief_he_does_not_hold(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));

        int shown = 0;
        foreach (var viewpoint in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            var snapshot = PlayerView.Build(world, viewpoint.Id, world.Now);

            foreach (var belief in snapshot.Known.Concat(snapshot.Recent).Concat(snapshot.Unsettled))
            {
                shown++;
                Assert.True(
                    viewpoint.Cognition.Records.Any(r => r.IsHeld && belief.Claim.Matches(r.Claim)),
                    $"{variant}: {viewpoint.Id} was shown \"{belief.Statement}\", which he does not hold");
            }
        }

        // Nobody is shown anything is not a passing boundary, it is an empty one.
        Assert.True(shown > 0, "no viewpoint was shown anything, so nothing was bounded");
    }

    // ================================================================ helpers

    private static void Believe(Character who, World world, Claim claim, double confidence)
        => who.Cognition.Learn(claim, Stance.Believes, confidence, SourceKind.Inference, who.Id, world.Now);

    /// <summary>Starts a real concealment instance through the production Commit path.</summary>
    private static StrategyInstance Start(World world, Character actor, Claim incident)
    {
        var ctx = Context(world, actor);
        var candidate = new Candidate($"conceal:{incident}", ActionKind.StartStrategy, "test", "clean it up")
        {
            Strategy = StrategyKind.ConcealIncident,
            TargetId = incident.Object.Length > 0 ? incident.Object : Cast.Grocery,
            Domain = Cast.Harbour,
            AboutIncident = incident,
        };
        Commit.Apply(world, actor, candidate, ctx.Agenda, ctx, new List<string>());
        return actor.Execution.Strategy!;
    }

    /// <summary>Delivers the instance's own pending step through the production Advance path.</summary>
    private static void Advance(World world, Character actor, StrategyInstance s)
        => Strategies.Advance(world, actor, new ScheduledEvent
        {
            Id = s.PendingStepEventId!.Value,
            Time = world.Now,
            Kind = EventKind.StrategyStep,
            OwnerId = actor.Id,
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

    /// <summary>Runs exactly the first concealment step against a real instance naming this incident.</summary>
    private static void QuietWitnesses(World world, Character actor, long incidentId)
    {
        var s = Start(world, actor, new Claim(ClaimKind.PersonUsedViolence, actor.Id, Cast.Grocery, incidentId));
        Advance(world, actor, s);
    }

    private static List<string> Cognitions(World world, string except)
        => world.Characters.Values
            .Where(c => c.Id != except)
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .SelectMany(c => c.Cognition.Records.Select(r => $"{c.Id}|{r}"))
            .ToList();

    private static Candidate Deny(Claim suppressed)
        => new($"deny:{suppressed}", ActionKind.ReportToSuperior, "test", "tell him it did not happen")
        {
            TargetId = "vincent",
            Candor = ReportCandor.False,
            AnsweringClaim = suppressed,
            Suppressed = new[] { new SuppressedClaim(suppressed, PriorDisclosureState.NeverAddressed) },
        };

    /// <summary>
    /// The denial's own risk component, read out of the production breakdown by the name the scorer
    /// emits — never recomputed here, which is how two earlier regression tests in this repository
    /// passed against a copy of the rule rather than against the rule.
    /// </summary>
    private static double Risk(Candidate denial, Character actor, World world)
    {
        var perceived = Salience.Perceive(actor, world.Now);
        var agenda = new Agenda(AgendaKind.DischargeResponsibility, "keep the harbour earning", "test", Cast.Harbour);
        var breakdown = Utility.Score(
            denial, actor.View, actor.Psychology, perceived, agenda, Rng.ForOccasion(world.Seed, "test|fixed"));

        return breakdown.Components
            .Where(c => c.Name == "perceived personal risk")
            .Sum(c => c.Value);
    }

    private static double DenialRisk(double aboutThisIncident, double aboutAnother)
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var suppressed = new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery, 7);
        tommy.Cognition.Learn(suppressed, Stance.Knows, 1.0, SourceKind.Participant, tommy.Id, world.Now);
        Believe(tommy, world, new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, tommy.Id, 7), aboutThisIncident);
        Believe(tommy, world, new Claim(ClaimKind.WitnessSawIncident, Cast.Bakery, tommy.Id, 9), aboutAnother);

        return Risk(Deny(suppressed), tommy, world);
    }

    private static GeneratorContext Context(World world, Character actor)
        => new(
            actor.View,
            Salience.Perceive(actor, world.Now),
            new Agenda(AgendaKind.Idle, "test", "test", Cast.Harbour),
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
            OrgMemberIds: Array.Empty<string>(),
            AcquaintedIds: Array.Empty<string>(),
            ReportsSent: Array.Empty<Report>(),
            RequestsMade: Array.Empty<InformationRequest>(),
            VisibleTargets: Array.Empty<string>());
}
