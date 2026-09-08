using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 026: in person, things come back.
///
/// Two channels, both actor-neutral, neither a disclosure. A man asked about something he holds
/// nothing on says so, and the asker records that he said so. A man who has just been told
/// something, or threatened, shows something on his face, and the man who told or threatened him
/// reads it — correctly, wrongly, or not at all. What the listener actually thinks or feels is
/// consulted only to decide what a clean read would return; it never reaches the speaker directly.
/// </summary>
public sealed class InPersonTests
{
    private const int Seed = 42;
    private static readonly DateTime End = Cast.Start.AddDays(90);

    private static readonly Claim Grocery = new(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
    private static readonly Claim VincentsViolence = new(ClaimKind.PersonUsedViolence, "vincent", Cast.Grocery);

    // ================================================================= the no-position reply

    /// <summary>
    /// The finding itself: Vincent asks Tommy on 2 March about a shop Tommy has never heard of, and
    /// before this milestone the request sat as "no answer yet" for the whole run. Now Tommy says he
    /// knows nothing, the request is answered, and Tommy is no longer somebody Vincent has not heard
    /// from — all through the ordinary pipeline, nothing staged.
    /// </summary>
    [Fact]
    public void A_man_asked_about_something_he_knows_nothing_of_says_so()
    {
        var session = SimulationSession.Start(Seed, "baseline", "vincent");
        var pending = AdvanceToPause(session);
        const string ask = "ask Tommy Nardo what he knows about whether Bellini's grocery is not paying its tribute";
        session.Choose(pending.Options.Single(o => o.Description == ask).Id);

        var asked = session.Snapshot();
        Assert.Contains(asked.AwaitingAnswers, r => r.AskedId == "tommy");
        Assert.Contains(asked.Silent, p => p.Id == "tommy");

        // Tommy is not controlled; his wake resolves through the pipeline on its own.
        session.AdvanceDays(3);
        var answered = session.Snapshot();

        Assert.DoesNotContain(answered.AwaitingAnswers, r => r.AskedId == "tommy");
        Assert.DoesNotContain(answered.Silent, p => p.Id == "tommy");
        var disclaimer = Assert.Single(answered.Disclaimers);
        Assert.Equal("tommy", disclaimer.PersonId);
        Assert.Equal(
            "Tommy Nardo says he knows nothing about whether Bellini's grocery is not paying its tribute",
            disclaimer.Description);

        // And it is not a denial: the grocery claim is neither contested nor listed as disagreed.
        Assert.DoesNotContain(answered.Disagreements, d => d.Claim.Matches(Grocery));
        Assert.False(answered.Known.Single(b => b.Claim.Matches(Grocery)).Contested);
    }

    /// <summary>
    /// The distinction that must survive the trip. A disclaimer counts as having heard from the man
    /// and as an answer to the question; it never counts as a position, so it cannot make a claim
    /// contested, cannot appear among the accounts of it, and cannot make his later real account a
    /// repetition of it.
    /// </summary>
    [Fact]
    public void A_disclaimer_is_an_answer_and_never_a_denial()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");
        vincent.Cognition.Learn(Grocery, Stance.Believes, 0.75, SourceKind.Report, "salvatore", Cast.Start);

        vincent.Cognition.ReceiveDisclaimer(Grocery, "tommy", Cast.Start.AddDays(1));

        Assert.True(vincent.Cognition.HasAccountFrom("tommy"));
        Assert.True(vincent.Cognition.HasAccountFrom("tommy", Grocery));
        Assert.False(vincent.Cognition.IsContested(Grocery));
        Assert.Empty(vincent.Cognition.AccountsOf(Grocery));
        Assert.Single(vincent.Cognition.Disclaimers);

        // A later real account from the same man is his first account, not a repeat of nothing.
        var receipt = vincent.Cognition.Receive(
            ReportedClaim.Honest(Grocery, Stance.Rejects, 0.7, SourceKind.Witness), "tommy", Cast.Start.AddDays(5));
        Assert.NotNull(receipt.Conflict);
        Assert.Single(vincent.Cognition.AccountsOf(Grocery));
    }

    /// <summary>
    /// The reply moves nothing but the record: no belief, no standing. A man saying he knows nothing
    /// has neither contradicted nor corroborated anybody.
    /// </summary>
    [Fact]
    public void A_disclaimer_moves_no_belief_and_no_standing()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");
        var tommy = world.Get("tommy");
        double trustBefore = vincent.Social.Toward("tommy").Trust;
        int recordsBefore = vincent.Cognition.Records.Count;

        var report = new Report(1, "tommy", "vincent", Cast.Start.AddDays(1), ReportCandor.Uninformed,
            Array.Empty<ReportedClaim>(), Array.Empty<Claim>(), "staged", Grocery);
        Reporting.Deliver(world, report, vincent);

        Assert.Equal(trustBefore, vincent.Social.Toward("tommy").Trust);
        Assert.Equal(recordsBefore, vincent.Cognition.Records.Count);
        Assert.Empty(vincent.Social.Toward("tommy").StandingHistory);
        Assert.Single(vincent.Cognition.Disclaimers);
        // Nothing was put to Vincent to take or not, so Tommy reads nothing off his face either.
        Assert.Empty(tommy.Social.Toward("vincent").Impressions);
    }

    // ================================================================= the reaction: listener-side only

    /// <summary>
    /// Two reports identical in everything the listener can see and differing only in the candour
    /// the developer log records leave the same impression on the speaker. Making the reaction read
    /// <c>Report.Candor</c> fails this — that is the mutation this test exists for.
    ///
    /// Compared on a seed where the read came back with something: the two deliveries share one
    /// keyed draw, so a blank read would be blank for both and hide a reaction that had read the
    /// candour. That is how the first version of this test passed against exactly that mutation.
    /// </summary>
    [Fact]
    public void The_reaction_is_read_from_the_listener_side_only()
    {
        int seed = Enumerable.Range(1, 80).First(s => Deliver(s, ReportCandor.False) != ImpressionKind.GaveNothingAway);
        Assert.Equal(Deliver(seed, ReportCandor.False), Deliver(seed, ReportCandor.Candid));

        static ImpressionKind Deliver(int seed, ReportCandor candor)
        {
            var world = Cast.Build(seed, "baseline");
            var marco = world.Get("marco");
            var vincent = world.Get("vincent");
            marco.Cognition.Learn(VincentsViolence, Stance.Knows, 1.0, SourceKind.Witness, "marco", Cast.Start);

            var denial = ReportedClaim.Misrepresenting(
                VincentsViolence, Stance.Rejects, 0.8, claimed: SourceKind.Report, actual: SourceKind.Participant);
            var report = new Report(1, "vincent", "marco", Cast.Start.AddDays(2), candor,
                new[] { denial }, new[] { VincentsViolence }, "staged", VincentsViolence);
            Reporting.Deliver(world, report, marco);

            return Assert.Single(vincent.Social.Toward("marco").Impressions).Kind;
        }
    }

    /// <summary>The impression lands on the speaker's side toward the listener, and nowhere else.</summary>
    [Fact]
    public void The_impression_is_the_speakers_and_the_listener_gets_none()
    {
        var world = Cast.Build(Seed, "baseline");
        var marco = world.Get("marco");
        var vincent = world.Get("vincent");
        marco.Cognition.Learn(VincentsViolence, Stance.Knows, 1.0, SourceKind.Witness, "marco", Cast.Start);

        var report = new Report(1, "vincent", "marco", Cast.Start.AddDays(2), ReportCandor.False,
            new[] { ReportedClaim.Misrepresenting(VincentsViolence, Stance.Rejects, 0.8, SourceKind.Report, SourceKind.Participant) },
            new[] { VincentsViolence }, "staged", VincentsViolence);
        Reporting.Deliver(world, report, marco);

        var impression = Assert.Single(vincent.Social.Toward("marco").Impressions);
        Assert.Equal(VincentsViolence, impression.About!.Value);
        Assert.Empty(marco.Social.Toward("vincent").Impressions);
        foreach (var other in world.Characters.Values.Where(c => c.Id is not "vincent"))
            Assert.All(other.Social.All, rel => Assert.Empty(rel.Impressions));
    }

    // ================================================================= the reaction: can be wrong

    /// <summary>
    /// A poor reader against a good hider is right rarely, wrong sometimes, and blank most of the
    /// time; a sharp reader against a poor hider is right nearly always. Removing the draw — always
    /// returning the truth — fails the first half, and that is the belief-reader this milestone
    /// exists not to build.
    /// </summary>
    [Fact]
    public void A_read_can_be_wrong_and_can_be_blank()
    {
        var truth = ImpressionKind.SeemedUnconvinced;
        var opposite = ImpressionKind.SeemedConvinced;

        var poor = new Dictionary<ImpressionKind, int>();
        var sharp = new Dictionary<ImpressionKind, int>();
        for (int i = 0; i < 400; i++)
        {
            var rng = Rng.ForOccasion(Seed, $"read-test|{i}");
            var rngAgain = Rng.ForOccasion(Seed, $"read-test|{i}");
            Count(poor, Reactions.Read(rng, reader: 0.0, hider: 1.0, truth, opposite));
            Count(sharp, Reactions.Read(rngAgain, reader: 1.0, hider: 0.0, truth, opposite));
        }

        Assert.True(poor[opposite] > 0, "a poor reader is never wrong — the face is a belief-reader");
        Assert.True(poor[ImpressionKind.GaveNothingAway] > poor[truth], "a good hider gives too much away");
        Assert.True(sharp[truth] > 350, "a sharp reader against a poor hider misreads too often");
        Assert.True(sharp[truth] < 400, "a sharp reader is never wrong — the face is a belief-reader");

        static void Count(Dictionary<ImpressionKind, int> into, ImpressionKind k)
            => into[k] = into.GetValueOrDefault(k) + 1;
    }

    /// <summary>The same exchange in the same run reads the same way twice. Keyed, not global.</summary>
    [Fact]
    public void The_read_is_deterministic_for_the_exchange()
    {
        ImpressionKind Once()
        {
            var world = Cast.Build(Seed, "baseline");
            var marco = world.Get("marco");
            marco.Cognition.Learn(VincentsViolence, Stance.Knows, 1.0, SourceKind.Witness, "marco", Cast.Start);
            var report = new Report(1, "vincent", "marco", Cast.Start.AddDays(2), ReportCandor.False,
                new[] { ReportedClaim.Misrepresenting(VincentsViolence, Stance.Rejects, 0.8, SourceKind.Report, SourceKind.Participant) },
                new[] { VincentsViolence }, "staged", VincentsViolence);
            Reporting.Deliver(world, report, marco);
            return world.Get("vincent").Social.Toward("marco").Impressions.Single().Kind;
        }

        Assert.Equal(Once(), Once());
    }

    // ================================================================= the boundary is a difference

    /// <summary>
    /// Two worlds identical but for what the listener holds must render differently when the read
    /// is clean, and identically when it is not. Showing the reaction unconditionally — the tempting
    /// simplification — passes the first half and fails the second.
    ///
    /// With the cast as written (Vincent reads people at 0.10, Marco hides at 0.30) a clean read
    /// happens under half the time and a blank one about as often, so both seeds are searched for
    /// rather than assumed, and the test states what it relies on.
    /// </summary>
    [Fact]
    public void A_clean_read_tells_the_two_worlds_apart_and_a_failed_one_does_not()
    {
        int cleanSeed = Enumerable.Range(1, 80).First(seed =>
            Read(seed, listenerHolds: true) == ImpressionKind.SeemedUnconvinced
            && Read(seed, listenerHolds: false) == ImpressionKind.SeemedConvinced);

        Assert.NotEqual(Describe(cleanSeed, listenerHolds: true), Describe(cleanSeed, listenerHolds: false));

        int blankSeed = Enumerable.Range(1, 80).First(seed =>
            Read(seed, listenerHolds: true) == ImpressionKind.GaveNothingAway
            && Read(seed, listenerHolds: false) == ImpressionKind.GaveNothingAway);

        Assert.Equal(Describe(blankSeed, listenerHolds: true), Describe(blankSeed, listenerHolds: false));
    }

    private static ImpressionKind Read(int seed, bool listenerHolds)
        => Stage(seed, listenerHolds).Get("vincent").Social.Toward("marco").Impressions.Single().Kind;

    private static string Describe(int seed, bool listenerHolds)
    {
        var world = Stage(seed, listenerHolds);
        var roster = PlayerView.Build(world, "vincent", world.Now, PlayerView.You).Attitudes
            .Single(a => a.PersonId == "marco");
        return string.Join("|", roster.Impressions.Select(i => i.Description));
    }

    /// <summary>
    /// Vincent denies the violence to Marco. Marco either saw it (and will not be talked out of his
    /// own eyes) or holds nothing on it (and takes the denial as his first account).
    /// </summary>
    private static World Stage(int seed, bool listenerHolds)
    {
        var world = Cast.Build(seed, "baseline");
        var marco = world.Get("marco");
        if (listenerHolds)
            marco.Cognition.Learn(VincentsViolence, Stance.Knows, 1.0, SourceKind.Witness, "marco", Cast.Start);

        var report = new Report(1, "vincent", "marco", Cast.Start.AddDays(2), ReportCandor.False,
            new[] { ReportedClaim.Misrepresenting(VincentsViolence, Stance.Rejects, 0.8, SourceKind.Report, SourceKind.Participant) },
            new[] { VincentsViolence }, "staged", VincentsViolence);
        Reporting.Deliver(world, report, marco);
        return world;
    }

    // ================================================================= the demand, and delegation

    /// <summary>
    /// A threat is read by the man who made it. When Tommy makes it for Vincent, Tommy reads Marco's
    /// face and Vincent reads nothing — a delegated demand stays silent to the owner, as milestone
    /// 024 settled for progress.
    /// </summary>
    [Fact]
    public void A_delegated_threat_is_read_by_the_executor_and_not_the_owner()
    {
        var session = SimulationSession.Start(Seed, "baseline", "vincent");
        Choose(session, "persuade Bellini's grocery to pay");
        Choose(session, "carry on getting Bellini's grocery to pay");
        Choose(session, "hand it to Tommy Nardo");
        Choose(session, "switch to threats with Bellini's grocery");
        session.AdvanceDays(10);

        var tommy = session.World.Get("tommy");
        var vincent = session.World.Get("vincent");
        Assert.Contains(tommy.Social.Toward("marco").Impressions,
            i => i.About is null && i.Kind is ImpressionKind.SeemedFrightened
                 or ImpressionKind.SeemedUnmoved or ImpressionKind.GaveNothingAway);
        Assert.DoesNotContain(vincent.Social.Toward("marco").Impressions, i => i.About is null);
    }

    // ================================================================= the natural lie

    /// <summary>
    /// The playtest scene: Vincent uses force, Marco asks him about it, Vincent denies it to his
    /// face. Vincent comes away with a reading of Marco, shown on his roster under Marco and under
    /// what just happened — in the second person, as his own reading, never as Marco's state.
    /// </summary>
    [Fact]
    public void Lying_to_a_mans_face_leaves_a_reading_of_it()
    {
        // Matt's own path, 2026-09-05: ask Tommy first, use force when the job comes round again,
        // then Marco puts his question and the denial is on the table.
        var session = SimulationSession.Start(Seed, "baseline", "vincent");
        Choose(session, "ask Tommy Nardo what he knows about whether Bellini's grocery is not paying its tribute");
        Choose(session, "use force on Bellini's grocery — breaking the rule: no public violence in the harbour");

        const string deny = "deny it to Marco Bellini: tell him you did not get violent at Bellini's grocery";
        PendingDecision? offering = null;
        var seen = new List<string>();
        for (int guard = 0; guard < 400 && offering is null && session.Date < End; guard++)
        {
            var pending = AdvanceToPause(session);
            seen.Add($"{session.Date:d MMM}: {string.Join(" | ", pending.Options.Select(o => o.Description))}");
            if (pending.Options.Any(o => o.Description == deny)) offering = pending;
            else session.Choose(pending.Options.First(o => o.Description.StartsWith("carry on", StringComparison.Ordinal)
                                                          || o.Description == "take no action").Id);
        }
        Assert.True(offering is not null, "the denial was never offered; pauses seen:" + Environment.NewLine + string.Join(Environment.NewLine, seen));
        session.Choose(offering!.Options.Single(o => o.Description == deny).Id);

        var vincent = session.World.Get("vincent");
        var impression = Assert.Single(vincent.Social.Toward("marco").Impressions, i => i.About is not null);
        Assert.Equal(VincentsViolence.Kind, impression.About!.Value.Kind);

        var snapshot = session.Snapshot();
        var marco = snapshot.Attitudes.Single(a => a.PersonId == "marco");
        var read = Assert.Single(marco.Impressions, i => i.Description.Contains("you got violent", StringComparison.Ordinal));
        // A reading, never a fact about Marco: "seemed to", "did not seem to", or "you could not tell".
        Assert.Matches("^(Marco Bellini seemed to believe you|Marco Bellini did not seem to believe you|you could not tell whether Marco Bellini believed you)", read.Description);
        Assert.DoesNotContain("believes", read.Description, StringComparison.Ordinal);

        // And what hangs over him says so in his own terms: the act, the witness, the denial, and
        // that Marco put it to him — never that Marco knows.
        string hanging = string.Join(" ", snapshot.Exposure);
        Assert.StartsWith("You got violent at Bellini's grocery, against the outfit's rule.", hanging);
        Assert.Contains("You denied it to Marco Bellini on ", hanging);
        Assert.Contains("Marco Bellini asked you about it on ", hanging);
        Assert.DoesNotContain("knows", hanging, StringComparison.Ordinal);
    }

    /// <summary>
    /// Before anybody has spoken to him about it, what hangs over him ends with exactly that — a
    /// statement about his own testimony log, not about anybody's knowledge. And a man with nothing
    /// to hide has nothing hanging over him.
    /// </summary>
    [Fact]
    public void What_hangs_over_him_says_nobody_has_raised_it_until_somebody_does()
    {
        var clean = SimulationSession.Start(Seed, "baseline", "vincent");
        Assert.Empty(clean.Snapshot().Exposure);

        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");
        vincent.Cognition.Learn(VincentsViolence, Stance.Knows, 1.0, SourceKind.Participant, "vincent", Cast.Start);
        vincent.Cognition.Learn(new Claim(ClaimKind.PersonBreachedPolicy, "vincent", "no-violence-harbour"),
            Stance.Knows, 1.0, SourceKind.Participant, "vincent", Cast.Start);

        var hanging = PlayerView.Build(world, "vincent", world.Now, PlayerView.You).Exposure;
        Assert.Equal(new[]
        {
            "You got violent at Bellini's grocery, against the outfit's rule.",
            "Nobody has raised it with you.",
        }, hanging);
    }

    /// <summary>
    /// Correction found alongside milestone 026's playtest (a Codex finding): the reaction clause
    /// used to be looked up independently of which report it was about — only "an impression about
    /// any act claim, toward this recipient, most recent" — so a later report that only withheld a
    /// second incident could still surface the reaction read off the recipient's face from an
    /// earlier report about a different one. Nothing is ever read off a face for a claim that was
    /// never put to it: <c>Reactions.AfterReport</c> only reacts to what a report actually asserts,
    /// so a withheld-only report must show no reaction at all, however recently a different incident
    /// earned one from the same man.
    ///
    /// Two incidents, two reports, one recipient. The earlier report tells Salvatore about the
    /// grocery incident and earns a real reaction. The later report withholds the bakery incident
    /// entirely — the one <c>Exposure</c> must describe, being the most recent — and must read as
    /// silence about it, not as Salvatore's reaction to the grocery incident borrowed a second time.
    /// </summary>
    [Fact]
    public void A_withheld_only_report_does_not_inherit_an_older_reaction_to_a_different_incident()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");

        var groceryIncident = new Claim(ClaimKind.PersonUsedViolence, "vincent", Cast.Grocery, 1);
        var bakeryIncident = new Claim(ClaimKind.PersonUsedViolence, "vincent", Cast.Bakery, 2);
        vincent.Cognition.Learn(groceryIncident, Stance.Knows, 1.0, SourceKind.Participant, "vincent", Cast.Start);
        vincent.Cognition.Learn(bakeryIncident, Stance.Knows, 1.0, SourceKind.Participant, "vincent",
            Cast.Start.AddDays(10));

        var toldAboutGrocery = new Report(1, "vincent", "salvatore", Cast.Start.AddDays(1), ReportCandor.Candid,
            new[] { ReportedClaim.Honest(groceryIncident, Stance.Knows, 1.0, SourceKind.Participant) },
            Array.Empty<Claim>(), "staged");
        world.Reports.Add(toldAboutGrocery);
        Relations.RecordImpression(vincent, "salvatore",
            new Impression(ImpressionKind.SeemedConvinced, groceryIncident, toldAboutGrocery.At));

        var keptBakeryQuiet = new Report(2, "vincent", "salvatore", Cast.Start.AddDays(15), ReportCandor.Candid,
            Array.Empty<ReportedClaim>(), new[] { bakeryIncident }, "staged");
        world.Reports.Add(keptBakeryQuiet);

        var hanging = PlayerView.Build(world, "vincent", world.Now, PlayerView.You).Exposure;
        var toSalvatore = Assert.Single(hanging, l => l.Contains("Salvatore Greco", StringComparison.Ordinal));

        Assert.StartsWith("You kept it from Salvatore Greco on", toSalvatore);
        Assert.DoesNotContain("seemed", toSalvatore, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("believe", toSalvatore, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tell whether", toSalvatore, StringComparison.OrdinalIgnoreCase);
    }

    // ================================================================= first correction

    /// <summary>
    /// He knows what he is good at, in words and never in numbers — Matt's ruling from the second
    /// playtest, in the register he gave. Vincent as written: a fair talker, physically
    /// threatening, keeps a straight face well enough, reads people badly. Kane is the mirror on
    /// the last two, and she is described as herself.
    /// </summary>
    [Fact]
    public void He_knows_what_he_is_good_at_in_words()
    {
        var session = SimulationSession.Start(Seed, "baseline", "vincent");
        var you = session.Snapshot().SelfKnowledge;
        Assert.Equal(new[]
        {
            "you can talk and communicate with people fairly well",
            "you are physically threatening",
            "you keep a straight face well enough",
            "you have a hard time reading people",
        }, you);

        var world = Cast.Build(Seed, "baseline");
        var kane = PlayerView.Build(world, "kane", world.Now).SelfKnowledge;
        Assert.Contains("she reads people well", kane);
        Assert.Contains("she is not physically threatening", kane);

        foreach (var c in world.Characters.Values)
            Assert.All(PlayerView.Build(world, c.Id, world.Now).SelfKnowledge,
                line => Assert.DoesNotMatch(@"\d", line));
    }

    /// <summary>
    /// The reaction attaches to the claim that landed, not the one the report led with. A report
    /// that opens by repeating the boss's own rule back to him and then tells him something new
    /// leaves an impression about the news.
    /// </summary>
    [Fact]
    public void The_reaction_is_about_the_claim_that_landed_not_the_one_the_report_led_with()
    {
        var world = Cast.Build(Seed, "baseline");
        var salvatore = world.Get("salvatore");
        var vincent = world.Get("vincent");
        // The rule he set himself, and something he holds nothing on: the scenario seeds him with
        // the grocery's weakness too, so that would have been corroboration, not news.
        var rule = world.Org.Policies[0].AwarenessClaim(Cast.OrgId);
        var news = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery);
        Assert.Null(salvatore.Cognition.Find(news));
        vincent.Cognition.Learn(rule, Stance.Believes, 0.75, SourceKind.Report, "salvatore", Cast.Start);
        vincent.Cognition.Learn(news, Stance.Believes, 0.6, SourceKind.Discovery, "vincent", Cast.Start);

        var report = new Report(1, "vincent", "salvatore", Cast.Start.AddDays(3), ReportCandor.Candid,
            new[]
            {
                ReportedClaim.Honest(rule, Stance.Believes, 0.7, SourceKind.Report),
                ReportedClaim.Honest(news, Stance.Believes, 0.5, SourceKind.Discovery),
            },
            Array.Empty<Claim>(), "staged");
        Reporting.Deliver(world, report, salvatore);

        var impression = Assert.Single(vincent.Social.Toward("salvatore").Impressions);
        Assert.Equal(news, impression.About!.Value);
    }

    /// <summary>
    /// Correction found alongside milestone 026's playtest (a Codex finding). <c>Reactions.Landed</c>
    /// used to infer "this is news to him" by comparing <c>InformationRecord.AcquiredAt</c> against
    /// the report's own timestamp — but a claim Salvatore already held can have been acquired, through
    /// some other channel entirely, on the exact date this unrelated report happens to land. The
    /// timestamp equality cannot tell that coincidence apart from a record this very call just
    /// created, and a pre-existing claim caught in that coincidence could out-rank the claim that was
    /// actually news in the same delivery — especially when it is asserted first, since the old check
    /// returned on the first match it found.
    ///
    /// Salvatore already holds the old claim, acquired the same day this report lands — staged
    /// directly, not produced by chance, so the coincidence is guaranteed rather than hunted for.
    /// The genuinely new claim is asserted second. The reaction must be about the new claim.
    /// </summary>
    [Fact]
    public void The_reaction_is_about_the_claim_that_is_actually_new_not_one_sharing_its_timestamp()
    {
        var world = Cast.Build(Seed, "baseline");
        var salvatore = world.Get("salvatore");
        var vincent = world.Get("vincent");

        var reportDate = Cast.Start.AddDays(3);
        var oldClaim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery);
        var actualNews = new Claim(ClaimKind.PoliceInvestigating, "tommy");

        // Acquired, by some other channel, on the exact date the report below will land — the
        // coincidence the old heuristic could not tell apart from genuine freshness.
        salvatore.Cognition.Learn(oldClaim, Stance.Believes, 0.6, SourceKind.Inference, salvatore.Id, reportDate);
        Assert.Null(salvatore.Cognition.Find(actualNews));

        var report = new Report(1, "vincent", "salvatore", reportDate, ReportCandor.Candid,
            new[]
            {
                // The old, coincidentally-dated claim first — under the old heuristic this alone
                // decided the match, before the genuinely fresh claim was ever considered.
                ReportedClaim.Honest(oldClaim, Stance.Believes, 0.7, SourceKind.Report),
                ReportedClaim.Honest(actualNews, Stance.Believes, 0.5, SourceKind.Report),
            },
            Array.Empty<Claim>(), "staged");
        Reporting.Deliver(world, report, salvatore);

        var impression = Assert.Single(vincent.Social.Toward("salvatore").Impressions);
        Assert.Equal(actualNews, impression.About!.Value);
    }

    // ================================================================= fixtures

    private static PendingDecision AdvanceToPause(SimulationSession session)
    {
        for (int guard = 0; guard < 5000 && session.Status != SessionStatus.AwaitingChoice; guard++)
            session.StepEvent();
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
        return session.Pending!;
    }

    private static void Choose(SimulationSession session, string description)
    {
        var pending = AdvanceToPause(session);
        var option = pending.Options.SingleOrDefault(o => o.Description == description);
        Assert.True(option is not null,
            $"{session.Date:yyyy-MM-dd} does not offer \"{description}\" — offered: " +
            string.Join(" | ", pending.Options.Select(o => o.Description)));
        session.Choose(option!.Id);
    }
}
