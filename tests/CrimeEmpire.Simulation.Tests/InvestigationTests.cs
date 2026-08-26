using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Strategy;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 011: a case is about an incident rather than an address, and the detective has a move
/// after she names a suspect.
///
/// Everything here is staged. The accepted scenario contains exactly one incident, so every
/// incident-scoping rule below is satisfied by coincidence in a natural run and a test taken from
/// one would pass whichever rule the code used — which is how the location-scoped versions survived
/// from milestone 001 to here. Two incidents at one shop is the case that separates them, and the
/// fixture has never contained it.
/// </summary>
public sealed class InvestigationTests
{
    private const long First = 11;
    private const long Second = 12;

    // ================================================================ a case is about an incident

    /// <summary>The instance carries the incident from the lead it was opened on.</summary>
    [Fact]
    public void An_investigation_names_the_incident_its_lead_belongs_to()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var kane = world.Get("kane");

        var s = OpenCase(world, kane, Lead(kane, First));

        Assert.Equal(First, s.SourceEventId);
    }

    /// <summary>
    /// A canvass into one beating does not pick up the witness to a different one at the same shop.
    /// Both leads name a person, so a location match would find either; only the case's own lead may
    /// produce a suspect.
    /// </summary>
    [Fact]
    public void A_canvass_names_a_suspect_only_from_a_witness_to_its_own_incident()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var kane = world.Get("kane");
        Believe(kane, world, new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "tommy", First), 0.6);
        Believe(kane, world, new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "vincent", Second), 0.9);

        RunToCompletion(world, kane, OpenCase(world, kane, Lead(kane, Second)));

        var suspicions = kane.Cognition.OfKind(ClaimKind.PersonUsedViolence).ToList();
        Assert.All(suspicions, r => Assert.Equal(Second, r.Claim.EventId));
        Assert.Contains(suspicions, r => r.Claim.Subject == "vincent");
        Assert.DoesNotContain(suspicions, r => r.Claim.Subject == "tommy");
    }

    /// <summary>
    /// Having closed one case at a shop does not close the next one there before it starts. The
    /// completion check asked whether she had named anybody for anything at this address.
    /// </summary>
    [Fact]
    public void A_name_on_one_incident_does_not_close_a_case_on_another_at_the_same_shop()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var kane = world.Get("kane");

        // She already suspects somebody over the first beating.
        kane.Cognition.Learn(new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, First),
            Stance.Suspects, 0.55, SourceKind.Inference, kane.Id, world.Now);

        // The second case is opened on a lead that names nobody, so it can never produce a name.
        var s = OpenCase(world, kane, new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "", Second));
        RunToCompletion(world, kane, s);

        // The outcome is on the completion event's note, which is what wakes the owner and what any
        // later reader sees — not in the truth log, which records only that she went and looked.
        var completion = Drain(world).Single(e => e.Kind == EventKind.StrategyComplete);
        Assert.Equal("the trail went cold", completion.Payload.Note);
    }

    /// <summary>
    /// The generator's half of the same rule. A lead she has put a name against is spent; a lead for
    /// a different incident at the same address is not, and the two candidates are distinguishable.
    /// </summary>
    [Fact]
    public void A_second_incident_at_the_same_shop_is_still_worth_opening()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var kane = world.Get("kane");
        Believe(kane, world, new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "tommy", First), 0.6);
        kane.Cognition.Learn(new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, First),
            Stance.Suspects, 0.55, SourceKind.Inference, kane.Id, world.Now);

        // With only the spent lead, nothing is offered.
        Assert.DoesNotContain(Generators.GenerateAll(Context(world, kane)),
            c => c.Strategy == StrategyKind.InvestigateIncident);

        // A second beating at the same shop is a second case.
        Believe(kane, world, new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "vincent", Second), 0.9);
        var opened = Generators.GenerateAll(Context(world, kane))
            .Where(c => c.Strategy == StrategyKind.InvestigateIncident)
            .ToList();

        Assert.Single(opened);
        Assert.Equal(Second, opened[0].AboutIncident!.Value.EventId);
    }

    /// <summary>
    /// Event 0 is not an incident. Matching on the default would make every unattributed claim share
    /// one, which is the scan defect milestone 010 removed from <c>Utility</c>, one layer up.
    /// </summary>
    [Fact]
    public void The_default_event_id_is_not_an_incident()
    {
        var a = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "tommy");
        var b = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery);

        Assert.False(Generators.SameIncident(a, b));
        Assert.False(Generators.SameIncident(a, a));
        Assert.True(Generators.SameIncident(a with { EventId = First }, b with { EventId = First }));
        Assert.False(Generators.SameIncident(a with { EventId = First }, b with { EventId = Second }));
    }

    // ================================================================ the trail going cold

    /// <summary>
    /// The branch that has been inert since it was written. It calls for the investigator to stop
    /// treating a dead lead as actionable, and did nothing at all: it was a <c>Learn</c> at half
    /// confidence, and Learn discards a record arriving less confident than the one already held.
    ///
    /// Staged, and it has to be. **No natural run reaches this branch** — Kane's canvass turns up a
    /// name in all five variants at seed 42, so the fixture cannot demonstrate the fix and this test
    /// is the only thing standing behind it.
    /// </summary>
    [Fact]
    public void A_canvass_that_finds_nothing_demotes_the_lead_it_was_opened_on()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var kane = world.Get("kane");
        var dead = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "", First);
        Believe(kane, world, dead, 0.8);

        RunToCompletion(world, kane, OpenCase(world, kane, dead));

        Assert.Equal(0.4, kane.Cognition.ConfidenceIn(dead), 6);
    }

    /// <summary>And only that one — a cold case is not a reason to doubt a different one.</summary>
    [Fact]
    public void A_cold_trail_leaves_every_other_incident_alone()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var kane = world.Get("kane");
        var dead = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "", First);
        var elsewhere = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "", Second);
        Believe(kane, world, dead, 0.8);
        Believe(kane, world, elsewhere, 0.8);

        RunToCompletion(world, kane, OpenCase(world, kane, dead));

        Assert.Equal(0.4, kane.Cognition.ConfidenceIn(dead), 6);
        Assert.Equal(0.8, kane.Cognition.ConfidenceIn(elsewhere), 6);
    }

    /// <summary>
    /// A lead she was *told* survives a canvass that finds nothing, because failing to find a witness
    /// is not evidence the account was false. That falls out of <see cref="Provenance.IsOwnReading"/>
    /// rather than being a rule of its own: testimony is something she has to be argued out of.
    /// </summary>
    [Fact]
    public void A_cold_trail_does_not_demote_a_lead_somebody_gave_her()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var kane = world.Get("kane");
        var told = new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "", First);
        kane.Cognition.Learn(told, Stance.Believes, 0.8, SourceKind.Report, "marco", world.Now);

        RunToCompletion(world, kane, OpenCase(world, kane, told));

        Assert.Equal(0.8, kane.Cognition.ConfidenceIn(told), 6);
    }

    // ================================================================ police interest names its incident

    /// <summary>
    /// A <see cref="ClaimKind.PoliceInvestigating"/> claim that names no incident is a heat bar with
    /// one holder: nothing can answer it, nothing can corroborate it, and it cannot go stale when the
    /// case does. `INFORMATION_AND_LEGIBILITY.md`'s anti-heat-bar tests are what this is for.
    /// </summary>
    [Fact]
    public void Police_interest_names_the_incident_the_suspect_is_being_looked_at_over()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var kane = world.Get("kane");
        Believe(kane, world, new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "tommy", First), 0.6);

        RunToCompletion(world, kane, OpenCase(world, kane, Lead(kane, First)));

        var offered = Drain(world)
            .Where(e => e.Kind == EventKind.ObservationOpportunity)
            .SelectMany(e => e.Payload.Claims ?? Array.Empty<Claim>())
            .Where(c => c.Kind == ClaimKind.PoliceInvestigating)
            .ToList();

        Assert.NotEmpty(offered);
        Assert.All(offered, c => Assert.Equal(First, c.EventId));
    }

    // ================================================================ putting it to the man it names

    /// <summary>
    /// The headline. Before milestone 011 an investigator who had named a suspect took a candidate
    /// set of exactly one option — <c>let it lie</c> — with nothing generated and nothing rejected,
    /// for the rest of the run. She now puts the allegation to him, in every variant where an
    /// incident occurs, and the exchange reaches his side of the channel.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void An_investigator_who_has_named_a_suspect_puts_it_to_him(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));

        var suspicion = world.Get("kane").Cognition.OfKind(ClaimKind.PersonUsedViolence).ToList();
        Assert.NotEmpty(suspicion);

        var asked = world.Requests.Where(r => r.AskerId == "kane").ToList();
        Assert.Single(asked);
        Assert.Equal("tommy", asked[0].AskedId);
        Assert.Contains(suspicion, s => s.Claim.Equals(asked[0].About));
    }

    /// <summary>
    /// And in most variants he answers, so it is an exchange rather than a message into the dark.
    ///
    /// <b>Not asserted for every variant, deliberately.</b> A question is spent when it is put and the
    /// reply is the other man's to give or withhold — <c>resentful-tommy</c> is the case where he
    /// never gets round to it, and asserting an answer there would be asserting a link the simulation
    /// does not make. The variants are listed rather than filtered so that one falling silent is a
    /// visible change to this list rather than a quiet reduction in what is checked.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void The_suspect_answers_the_detective(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));

        var answer = world.Reports.Single(r => r.SenderId == "tommy" && r.RecipientId == "kane");
        Assert.Equal(world.Requests.Single(r => r.AskerId == "kane").About, answer.AnsweringClaim);
    }

    /// <summary>
    /// She asks once. A question is spent when it is put, not when it is answered — the reply is the
    /// other man's to give or withhold, so waiting for one means asking forever.
    /// </summary>
    [Fact]
    public void An_allegation_is_spent_when_it_is_put()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var kane = world.Get("kane");
        var allegation = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, First);
        kane.Cognition.Learn(allegation, Stance.Suspects, 0.55, SourceKind.Inference, kane.Id, world.Now);

        Assert.Contains(Generators.GenerateAll(Context(world, kane, "tommy")), Alleges);

        // Through Generators.CanAsk, the production rule, rather than a copy of it here.
        var alreadyAsked = new[] { new InformationRequest(1, kane.Id, "tommy", allegation, world.Now) };
        Assert.False(Generators.CanAsk(alreadyAsked, "tommy", allegation));
        Assert.DoesNotContain(Generators.GenerateAll(Context(world, kane, alreadyAsked, "tommy")), Alleges);

        // And what is spent is the question, not the man: a different allegation is still live.
        var other = new Claim(ClaimKind.PersonBreachedPolicy, "tommy", "no-violence-harbour", Second);
        kane.Cognition.Learn(other, Stance.Suspects, 0.4, SourceKind.Inference, kane.Id, world.Now);
        Assert.Contains(Generators.GenerateAll(Context(world, kane, alreadyAsked, "tommy")), Alleges);
    }

    /// <summary>
    /// The complement of the corroboration route. What he was told he checks against somebody else;
    /// what he worked out himself he puts to the man it names. Neither generator may cover the
    /// other's provenance, or one act would be offered twice out of a bounded set of six.
    /// </summary>
    [Fact]
    public void What_he_was_told_is_not_something_he_alleges()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var kane = world.Get("kane");
        kane.Cognition.Learn(new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, First),
            Stance.Believes, 0.55, SourceKind.Report, "marco", world.Now);

        Assert.DoesNotContain(Generators.GenerateAll(Context(world, kane, "tommy", "marco")), Alleges);
    }

    /// <summary>A man does not put his own act to himself; the answering side would have nothing to do.</summary>
    [Fact]
    public void He_does_not_put_his_own_act_to_himself()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        tommy.Cognition.Learn(new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery, First),
            Stance.Knows, 1.0, SourceKind.Participant, tommy.Id, world.Now);

        Assert.DoesNotContain(Generators.GenerateAll(Context(world, tommy, tommy.Id)), Alleges);
    }

    /// <summary>
    /// Milestone 009's rule, over this candidate's target like every other. Somebody she holds a
    /// claim about but could not name is not somebody she can go and question — and the filter has to
    /// admit as well as exclude, or it would be narrowing what can honestly be expressed.
    /// </summary>
    [Fact]
    public void An_allegation_goes_only_to_somebody_she_could_name()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var kane = world.Get("kane");
        kane.Cognition.Learn(new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, First),
            Stance.Suspects, 0.55, SourceKind.Inference, kane.Id, world.Now);

        Assert.DoesNotContain(Generators.GenerateAll(Context(world, kane)), Alleges);
        Assert.Contains(Generators.GenerateAll(Context(world, kane, "tommy")), Alleges);
    }

    /// <summary>
    /// The milestone's hypothesis, as a structural check rather than a score. Being asked by somebody
    /// who is not his superior still puts the three-way choice in front of him — candid, partial and
    /// false — so the exchange a detective opens is one a suspect can lie in. Whether he *does* is a
    /// measurement and lives in the archive.
    /// </summary>
    [Fact]
    public void Being_asked_by_somebody_who_is_not_his_superior_still_offers_a_denial()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var allegation = new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery, First);
        tommy.Cognition.Learn(allegation, Stance.Knows, 1.0, SourceKind.Participant, tommy.Id, world.Now);

        var ctx = Asked(world, tommy, by: "kane", about: allegation);
        var answers = Generators.GenerateAll(ctx)
            .Where(c => c.Kind == ActionKind.ReportToSuperior && c.TargetId == "kane")
            .ToList();

        // Kane is nobody's superior, and the context says so — the redirect must not confer rank.
        Assert.Null(ctx.SuperiorId);
        Assert.Equal(
            new[] { ReportCandor.Candid, ReportCandor.False, ReportCandor.Partial },
            answers.Select(a => a.Candor!.Value).OrderBy(c => c.ToString(), StringComparer.Ordinal));
    }

    /// <summary>
    /// Milestone 011's ruling 5, checked rather than argued — and it was not checked at the time.
    /// The milestone asserted only that <c>PlayerOption</c> renders the <em>kind</em> of candidate
    /// from typed fields; nothing established that the allegation actually reaches a person
    /// controlling the investigator. Found by the self-review of `6a8a765`.
    ///
    /// No <c>AdvanceTo</c> inside the loop: <c>Choose</c> resumes and runs on to the next pause by
    /// itself, and advancing again while a choice is outstanding is refused — deliberately, since a
    /// half-handled decision is not a place the clock may pass through. Writing that loop the other
    /// way is what made the first attempt at this check report, wrongly, that she was offered nothing.
    /// </summary>
    [Fact]
    public void A_player_controlling_the_investigator_is_offered_the_allegation()
    {
        var session = SimulationSession.Start(42, "baseline", "kane", "kane");
        var offered = new List<string>();

        session.AdvanceTo(Cast.Start.AddDays(90));
        while (session.Status == SessionStatus.AwaitingChoice)
        {
            offered.AddRange(session.Pending!.Options.Select(o => o.Description));
            session.Choose(session.Pending!.Options[0].Id);
        }

        Assert.Contains(offered, o =>
            o.Contains("ask Tommy Nardo", StringComparison.Ordinal)
            && o.Contains("put hands on", StringComparison.Ordinal));
    }

    // ================================================================ delegated investigation
    //
    // Corrective scope, found by review: AdvanceInvestigation read and wrote `owner` throughout,
    // never `executor`. Harmless in the accepted scenario — Kane delegates to nobody, so
    // `owner == executor` always and every test above is satisfied by coincidence — and wrong in
    // general, the same shape milestone 010 had already fixed for concealment. Staged, because the
    // fixture cannot delegate an investigation on its own; nobody in the cast has both organisational
    // subordinates and Investigation skill.

    /// <summary>
    /// Opens a case on the owner and delegates it through the production `Commit` path, exactly as
    /// `OpenCase` opens one — but without pre-seeding the owner's belief in the lead, so a belief the
    /// executor ends up holding can only have come from the assertion made directly on her, not from
    /// `DelegateStrategy`'s own belief transfer (which would otherwise also carry a lead the owner
    /// held, since a `WitnessSawIncident`'s Subject is the business, the same field `TargetId` names).
    /// That is what makes the tests below a check on `AdvanceInvestigation` specifically, not a
    /// second test of delegation's existing transfer.
    /// </summary>
    private static StrategyInstance OpenDelegatedCase(World world, Character owner, Character executor, Claim lead)
    {
        var ctx = Context(world, owner);
        var candidate = new Candidate($"investigate:{lead}", ActionKind.StartStrategy, "test",
            $"open an investigation into events at {lead.Subject}")
        {
            TargetId = lead.Subject,
            Strategy = StrategyKind.InvestigateIncident,
            Domain = Cast.Harbour,
            AboutIncident = lead,
        };
        Commit.Apply(world, owner, candidate, ctx.Agenda, ctx, new List<string>());
        var s = owner.Execution.Strategy!;

        var delegateCtx = Context(world, owner);
        var delegateCandidate = new Candidate($"delegate:{s.Kind}:{executor.Id}", ActionKind.DelegateStrategy,
            "test", $"have {executor.Name} take it on")
        {
            TargetId = executor.Id,
            Strategy = s.Kind,
            Domain = Cast.Harbour,
            RequiredCrew = 1,
        };
        Commit.Apply(world, owner, delegateCandidate, delegateCtx.Agenda, delegateCtx, new List<string>());
        return s;
    }

    /// <summary>
    /// The successful canvass. Kane is the executor here rather than the owner — her Investigation
    /// skill of 0.70 is what the roll actually reads, and nothing about the roll consults who owns
    /// the strategy.
    /// </summary>
    [Fact]
    public void A_delegated_investigations_lead_is_drawn_from_the_executors_own_belief()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var vincent = world.Get("vincent");
        var kane = world.Get("kane");
        var lead = Lead(kane, First);

        var s = OpenDelegatedCase(world, owner: vincent, executor: kane, lead);
        Assert.Equal(kane.Id, s.DelegatedToId);
        // Confirms the isolation the helper's own doc comment claims: delegation's belief transfer
        // had nothing of this claim's subject to carry, because the owner was never given it.
        Assert.Null(vincent.Cognition.Find(lead));

        Believe(kane, world, lead, 0.6);
        RunToCompletion(world, kane, s);

        var suspicion = kane.Cognition.OfKind(ClaimKind.PersonUsedViolence).ToList();
        Assert.Contains(suspicion, r => r.Claim.Subject == "tommy" && r.Claim.EventId == First);
        Assert.All(suspicion, r => Assert.Equal(SourceKind.Inference, r.SourceKind));
        Assert.All(suspicion, r => Assert.Equal(kane.Id, r.SourceId));

        // The completion check ("has a name been put to this incident") has to read the same
        // cognition the name was written to, or a canvass that genuinely succeeded still reports
        // "the trail went cold" and demotes the very lead it just confirmed. Checked two ways: the
        // outcome the strategy actually recorded, and that the lead's own confidence was not halved
        // by a cold-trail branch that should never have run.
        var completion = Drain(world).Single(e => e.Kind == EventKind.StrategyComplete);
        Assert.Equal("the canvass turned up a name", completion.Payload.Note);
        Assert.Equal(0.6, kane.Cognition.ConfidenceIn(lead), 6);
    }

    /// <summary>
    /// The owner learns nothing from a case he delegated — not the suspicion the canvass turns up,
    /// and not even that a canvass happened, unless the executor reports it through the ordinary
    /// channel. That silence is the correction: before it, this suspicion landed directly in the
    /// owner's head with no report, no message, and no trace of anyone having said anything.
    /// </summary>
    [Fact]
    public void The_owner_of_a_delegated_investigation_does_not_learn_its_results_invisibly()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var vincent = world.Get("vincent");
        var kane = world.Get("kane");
        var lead = Lead(kane, First);

        var s = OpenDelegatedCase(world, owner: vincent, executor: kane, lead);
        Believe(kane, world, lead, 0.6);
        RunToCompletion(world, kane, s);

        // Sanity: the canvass really did produce a position — this test is checking who holds it,
        // not whether anything was found at all.
        Assert.Contains(kane.Cognition.Records, r => r.Claim.Kind == ClaimKind.PersonUsedViolence);
        Assert.DoesNotContain(vincent.Cognition.Records, r => r.Claim.Kind == ClaimKind.PersonUsedViolence);
    }

    /// <summary>
    /// The cold trail, on a delegated case, and deterministically so regardless of seed: Tommy's
    /// Investigation skill of 0.10 cannot clear the 0.55 threshold even at the top of the roll's
    /// range (0.10 + 0.20 = 0.30), so `found` is false on every run without depending on the RNG draw
    /// at all. This is also the mutation-relevant case: revising the wrong person's copy of the claim
    /// (owner's id where the executor's own reading is required) would leave this confidence
    /// unchanged rather than halved, since `Cognition.Revise` refuses a record whose source is not
    /// the caller.
    /// </summary>
    [Fact]
    public void A_delegated_investigations_cold_trail_demotes_the_executors_own_lead()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var vincent = world.Get("vincent");
        var tommy = world.Get("tommy");
        var lead = Lead(tommy, First);

        var s = OpenDelegatedCase(world, owner: vincent, executor: tommy, lead);
        Believe(tommy, world, lead, 0.6);
        RunToCompletion(world, tommy, s);

        Assert.Equal(0.3, tommy.Cognition.ConfidenceIn(lead), 6);
        Assert.DoesNotContain(vincent.Cognition.Records, r => r.Claim.Kind == ClaimKind.WitnessSawIncident);
    }

    /// <summary>
    /// Both outcomes — a name turned up, and a cold trail — are as deterministic delegated as they
    /// already are undelegated: identical seed, identical result, on a freshly built world each time
    /// rather than a shared one a first run could have left in a different state.
    /// </summary>
    [Fact]
    public void Delegated_investigation_outcomes_are_deterministic()
    {
        (double confidence, string? suspect) Run()
        {
            var world = Cast.Build(seed: 1, "baseline");
            var vincent = world.Get("vincent");
            var kane = world.Get("kane");
            var lead = Lead(kane, First);
            var s = OpenDelegatedCase(world, owner: vincent, executor: kane, lead);
            Believe(kane, world, lead, 0.6);
            RunToCompletion(world, kane, s);
            var suspicion = kane.Cognition.OfKind(ClaimKind.PersonUsedViolence).FirstOrDefault();
            return (kane.Cognition.ConfidenceIn(lead), suspicion?.Claim.Subject);
        }

        var first = Run();
        var second = Run();
        Assert.Equal(first, second);
        Assert.Equal("tommy", first.suspect);
    }

    // ================================================================ helpers

    private static bool Alleges(Candidate c)
        => c.Kind == ActionKind.SeekCorroboration && c.Generator == "FromAllegation";

    /// <summary>A context whose trigger is somebody putting a question directly.</summary>
    private static GeneratorContext Asked(World world, Character actor, string by, Claim about)
        => Context(world, actor, by) with
        {
            Trigger = new ScheduledEvent
            {
                Id = 0,
                Time = world.Now,
                Kind = EventKind.RoleReview,
                OwnerId = actor.Id,
                Cause = "test",
                Payload = new EventPayload { Note = "asked-to-account", TargetId = by, AboutClaim = about },
            },
        };


    private static Claim Lead(Character who, long incident)
        => new(ClaimKind.WitnessSawIncident, Cast.Grocery, "tommy", incident);

    private static void Believe(Character who, World world, Claim claim, double confidence)
        => who.Cognition.Learn(claim, Stance.Believes, confidence, SourceKind.Discovery, who.Id, world.Now);

    /// <summary>Opens a real case through the production Commit path, on a named lead.</summary>
    private static StrategyInstance OpenCase(World world, Character actor, Claim lead)
    {
        if (actor.Cognition.Find(lead) is null) Believe(actor, world, lead, 0.6);

        var ctx = Context(world, actor);
        var candidate = new Candidate($"investigate:{lead}", ActionKind.StartStrategy, "test",
            $"open an investigation into events at {lead.Subject}")
        {
            TargetId = lead.Subject,
            Strategy = StrategyKind.InvestigateIncident,
            Domain = Cast.Harbour,
            AboutIncident = lead,
        };
        Commit.Apply(world, actor, candidate, ctx.Agenda, ctx, new List<string>());
        return actor.Execution.Strategy!;
    }

    /// <summary>Drives every step of the instance through the production Advance path.</summary>
    private static void RunToCompletion(World world, Character actor, StrategyInstance s)
    {
        for (int i = 0; i < Strategies.InvestigateSteps.Length; i++)
        {
            Strategies.Advance(world, actor, new ScheduledEvent
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
            if (s.PendingStepEventId is null) return;
        }
    }

    /// <summary>Everything still queued, taken off through the queue's own ordering.</summary>
    private static List<ScheduledEvent> Drain(World world)
    {
        var drained = new List<ScheduledEvent>();
        while (world.Queue.Next(world.Now.AddYears(1)) is { } ev) drained.Add(ev);
        return drained;
    }

    private static GeneratorContext Context(World world, Character actor, params string[] acquainted)
        => Context(world, actor, Array.Empty<InformationRequest>(), acquainted);

    private static GeneratorContext Context(
        World world, Character actor, IReadOnlyList<InformationRequest> requestsMade, params string[] acquainted)
        => new(
            actor.View,
            Salience.Perceive(actor, world.Now),
            new Agenda(AgendaKind.DischargeResponsibility, "clear cases in the harbour district",
                "test", Cast.Harbour),
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
            AcquaintedIds: acquainted,
            ReportsSent: Array.Empty<Report>(),
            RequestsMade: requestsMade,
            VisibleTargets: Array.Empty<string>());
}
