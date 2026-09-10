using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 023 — The Roster Reads. Twenty milestones of relationship machinery, and the interface
/// showed the player five adjectives and nothing about how any of it got that way.
///
/// Grudges were the only durable per-relationship history, and they run one direction only. Trust has
/// moved at runtime since milestone 006 and both ways since 016; fear has moved since the first
/// coercion resolution; neither left any trace of *why*. Milestone 018 added "his trust in X cooled"
/// as a transient recent-events item, which scrolls past, says nothing about the cause, and is not
/// attached to the man it concerns. So a relationship that cooled because somebody contradicted him
/// to his face read exactly like one that had never been warm.
///
/// `PlayerNarration.Standing`'s own doc comment argued that was correct — the player "has to
/// reconstruct" the cause from the accounts. **Matt reversed that on 2026-09-04**: defensible for a
/// developer reading a claim log, wrong for somebody playing a game.
/// </summary>
public sealed class RosterHistoryTests
{
    private static readonly DateTime At = Cast.Start;
    private static readonly Claim Beating = new(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery);

    // ================================================================= written where it happens

    /// <summary>
    /// The movement and the memory of it are written together, in the one place that owns
    /// relationship mutation. Anything else would be reconstructing a cause after the fact from state
    /// that no longer says what produced it.
    /// </summary>
    [Fact]
    public void A_contradiction_costs_trust_and_records_why()
    {
        var listener = Salvatore(out var world);
        Relations.Establish(listener, "tommy", trust: 0.80);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, listener.Id, At);

        var receipt = listener.Cognition.Receive(
            ReportedClaim.Honest(Beating, Stance.Rejects, 0.9, SourceKind.Participant),
            "tommy", At.AddDays(1));
        Relations.RecordAccountConflict(listener, receipt.Conflict!.Value, At.AddDays(1));

        var rel = listener.Social.Toward("tommy");
        Assert.True(rel.Trust < 0.80);

        var moment = Assert.Single(rel.StandingHistory);
        Assert.Equal(StandingCause.AccountContradicted, moment.Cause);
        Assert.Equal(At.AddDays(1), moment.At);
        Assert.Empty(world.TruthLog); // nothing was invented in the world to carry it
    }

    /// <summary>
    /// The boundary this shares with <see cref="Being_frightened_is_remembered_only_when_it_actually_moved"/>:
    /// a man already floored at zero trust does not acquire a fresh memory of being contradicted
    /// again, because nothing about his state changed to match it. Through the real production path
    /// — <c>Cognition.Receive</c> produces the conflict, not a hand-built <c>AccountConflict</c> — so
    /// this proves the guard against a genuine contradiction, not a synthetic one.
    ///
    /// Corrected 2026-09-09, after Codex's review of milestone 023's `6738200` found
    /// <see cref="Relations.RecordAccountConflict"/> remembering unconditionally, even at the clamp —
    /// a history entry with no movement behind it, exactly the defect
    /// <see cref="Being_frightened_is_remembered_only_when_it_actually_moved"/> already guards against
    /// for fear. Mutation-checked: reverting the guard makes this test fail with a single
    /// <c>AccountContradicted</c> entry in <c>StandingHistory</c> despite trust staying at 0.
    /// </summary>
    [Fact]
    public void A_contradiction_at_the_trust_floor_is_not_remembered()
    {
        var listener = Salvatore(out _);
        Relations.Establish(listener, "tommy", trust: 0.0);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, listener.Id, At);

        var receipt = listener.Cognition.Receive(
            ReportedClaim.Honest(Beating, Stance.Rejects, 0.9, SourceKind.Participant),
            "tommy", At.AddDays(1));
        Relations.RecordAccountConflict(listener, receipt.Conflict!.Value, At.AddDays(1));

        var rel = listener.Social.Toward("tommy");
        Assert.Equal(0.0, rel.Trust, precision: 9);
        Assert.Empty(rel.StandingHistory);
    }

    /// <summary>
    /// The upward direction, which grudges could never express: something a man did that improved
    /// how he is regarded, kept as durably as something he did that damaged it.
    /// </summary>
    [Fact]
    public void A_corroboration_raises_trust_and_records_why()
    {
        var listener = Salvatore(out _);
        Relations.Establish(listener, "tommy", trust: 0.40);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, listener.Id, At);

        var receipt = listener.Cognition.Receive(
            ReportedClaim.Honest(Beating, Stance.Believes, 0.8, SourceKind.Participant),
            "tommy", At.AddDays(1));
        Relations.RecordAccountAgreement(listener, receipt.Agreement!.Value, At.AddDays(1));

        var rel = listener.Social.Toward("tommy");
        Assert.True(rel.Trust > 0.40);
        Assert.Equal(StandingCause.AccountCorroborated, Assert.Single(rel.StandingHistory).Cause);
    }

    /// <summary>
    /// The mirror of <see cref="A_contradiction_at_the_trust_floor_is_not_remembered"/>: a man already
    /// as trusted as the scale allows does not acquire a fresh memory of being corroborated again.
    /// Through the real production path, like its sibling above.
    ///
    /// Corrected 2026-09-09, after Codex's review of milestone 023's `6738200` found
    /// <see cref="Relations.RecordAccountAgreement"/> remembering unconditionally, even at the clamp.
    /// Mutation-checked: reverting the guard makes this test fail with a single
    /// <c>AccountCorroborated</c> entry in <c>StandingHistory</c> despite trust staying at 1.
    /// </summary>
    [Fact]
    public void A_corroboration_at_the_trust_ceiling_is_not_remembered()
    {
        var listener = Salvatore(out _);
        Relations.Establish(listener, "tommy", trust: 1.0);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, listener.Id, At);

        var receipt = listener.Cognition.Receive(
            ReportedClaim.Honest(Beating, Stance.Believes, 0.8, SourceKind.Participant),
            "tommy", At.AddDays(1));
        Relations.RecordAccountAgreement(listener, receipt.Agreement!.Value, At.AddDays(1));

        var rel = listener.Social.Toward("tommy");
        Assert.Equal(1.0, rel.Trust, precision: 9);
        Assert.Empty(rel.StandingHistory);
    }

    /// <summary>
    /// Fear, which is the third thing that moves and the one nothing has ever explained to a player.
    ///
    /// And the boundary: a man already as frightened as the scale allows acquires no fresh memory of
    /// being frightened again, because nothing about his state changed to match it. A history entry
    /// with no movement behind it is a line the roster cannot justify.
    /// </summary>
    [Fact]
    public void Being_frightened_is_remembered_only_when_it_actually_moved()
    {
        var marco = Marco(out _);

        Relations.Frighten(marco, "tommy", 0.35, At);
        Assert.Equal(StandingCause.Frightened, Assert.Single(marco.Social.Toward("tommy").StandingHistory).Cause);

        // Saturate, then push again against the ceiling.
        Relations.Frighten(marco, "tommy", 1.0, At.AddDays(1));
        int afterSaturating = marco.Social.Toward("tommy").StandingHistory.Count;
        Relations.Frighten(marco, "tommy", 0.5, At.AddDays(2));

        Assert.Equal(1.0, marco.Social.Toward("tommy").Fear, precision: 9);
        Assert.Equal(afterSaturating, marco.Social.Toward("tommy").StandingHistory.Count);
    }

    /// <summary>
    /// Directional, like every other relationship fact. A movement in what Salvatore makes of Tommy
    /// is Salvatore's memory and appears nowhere in Tommy's.
    /// </summary>
    [Fact]
    public void A_movement_is_remembered_by_the_man_it_moved_and_nobody_else()
    {
        var world = Cast.Build(42, "baseline");
        var salvatore = world.Get("salvatore");
        var tommy = world.Get("tommy");

        Relations.Establish(salvatore, "tommy", trust: 0.80);
        salvatore.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, salvatore.Id, At);
        var receipt = salvatore.Cognition.Receive(
            ReportedClaim.Honest(Beating, Stance.Rejects, 0.9, SourceKind.Participant),
            "tommy", At.AddDays(1));
        Relations.RecordAccountConflict(salvatore, receipt.Conflict!.Value, At.AddDays(1));

        Assert.Single(salvatore.Social.Toward("tommy").StandingHistory);
        Assert.Empty(tommy.Social.Toward("salvatore").StandingHistory);
    }

    // ================================================================= what the player is told

    /// <summary>
    /// The player gets the reason in words, and the words are built from the typed cause rather than
    /// from a string written where the movement happened — `DESIGN_DECISIONS.md`'s rule that no
    /// simulation-authored string crosses the boundary, which is why `Relations` never composes one.
    ///
    /// And it says what happened without asserting what was true: being contradicted is a fact about
    /// the exchange, whereas "he lied to you" is a fact about the speaker that the listener has no
    /// access to and the model deliberately refuses to hand over.
    /// </summary>
    [Fact]
    public void The_reason_reads_as_prose_and_never_accuses()
    {
        string contradicted =
            PlayerNarration.WhyStandingMoved(StandingCause.AccountContradicted, Pronouns.He, "Vincent Russo");

        Assert.Contains("Vincent Russo", contradicted, StringComparison.Ordinal);
        Assert.DoesNotContain("lie", contradicted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("false", contradicted, StringComparison.OrdinalIgnoreCase);

        // No number reaches any of them — the existing standing-phrase rule, applied to the new one.
        foreach (var cause in Enum.GetValues<StandingCause>())
        {
            string rendered = PlayerNarration.WhyStandingMoved(cause, Pronouns.He, "Vincent Russo");
            Assert.DoesNotContain(rendered, ".0", StringComparison.Ordinal);
            Assert.All("0123456789", d => Assert.DoesNotContain(d.ToString(), rendered, StringComparison.Ordinal));
        }

        // Direction is derived from the cause, never stored beside it where the two could disagree.
        Assert.True(PlayerNarration.Warmed(StandingCause.AccountCorroborated));
        Assert.False(PlayerNarration.Warmed(StandingCause.AccountContradicted));
        Assert.False(PlayerNarration.Warmed(StandingCause.Frightened));
    }

    /// <summary>
    /// The end-to-end claim, through the real player projection rather than the domain: after a run
    /// in which somebody was contradicted, the roster carries the reason, attached to the man it is
    /// about.
    ///
    /// Read off `PlayerView.Build` rather than off `Social` directly, because the milestone's claim is
    /// about what a player sees and a test that asserted on the domain would pass with the projection
    /// entirely unwired — the false-assurance shape this project's ledger names as recurring.
    /// </summary>
    [Fact]
    public void The_roster_carries_the_reason_after_a_natural_run()
    {
        var world = Cast.Build(42, "baseline");
        Runner.Run(world, Cast.Start.AddDays(90));

        var withHistory = Cast.Build(42, "baseline");
        Runner.Run(withHistory, Cast.Start.AddDays(90));

        var snapshot = PlayerView.Build(withHistory, "salvatore", withHistory.Now);
        var remembered = snapshot.Attitudes.SelectMany(a => a.History).ToList();

        Assert.NotEmpty(remembered);
        Assert.All(remembered, m => Assert.False(string.IsNullOrWhiteSpace(m.Description)));

        // Attached to somebody, dated, and inside the run.
        Assert.Contains(snapshot.Attitudes, a => a.History.Count > 0);
        Assert.All(remembered, m => Assert.InRange(m.At, Cast.Start, Cast.Start.AddDays(90)));
    }

    // ================================================================= what a capability claim says

    /// <summary>
    /// Corrects a gap Codex found in `4da1e66`'s own regression coverage: milestone 021 added
    /// <c>ClaimKind.PersonIsCapable</c> without adding it to <see cref="PlayerNarration.Describe"/>,
    /// and nothing caught it — it reached players as a raw
    /// <c>PersonIsCapable(angelo -&gt; hard-man)</c> dump until it was found while scoping milestone
    /// 024, per <see cref="PlayerNarration.Describe"/>'s own doc comment. `4da1e66` fixed the arm but
    /// added no test proving it, so a future edit could remove it again and nothing would fail.
    ///
    /// Both bars, both read as prose rather than as the developer predicate.
    /// </summary>
    [Fact]
    public void PersonIsCapable_claims_render_as_prose_not_as_the_developer_predicate()
    {
        // An unmistakably internal id, resolved to a different display name — "tommy" would have let
        // this test pass whether or not the production arm actually called the resolver, since an
        // identity resolver leaves an already name-shaped id unchanged either way. Codex's review of
        // `2dec7ff` found exactly that gap.
        const string internalId = "char-000e7f";
        const string displayName = "Tommy Nardo";
        string Resolve(string id) => id == internalId ? displayName : id;

        var hardMan = CapabilityBar.About(internalId, CapabilityBar.HardMan);
        var roughWork = CapabilityBar.About(internalId, CapabilityBar.RoughWork);

        string hardManProse = PlayerNarration.Describe(hardMan, Resolve);
        string roughWorkProse = PlayerNarration.Describe(roughWork, Resolve);

        Assert.Equal("Tommy Nardo is a hard man", hardManProse);
        Assert.Equal("Tommy Nardo can handle leaning on somebody", roughWorkProse);

        // The resolved display name is what a player sees, never the internal id it came from.
        Assert.Contains(displayName, hardManProse, StringComparison.Ordinal);
        Assert.Contains(displayName, roughWorkProse, StringComparison.Ordinal);
        Assert.DoesNotContain(internalId, hardManProse, StringComparison.Ordinal);
        Assert.DoesNotContain(internalId, roughWorkProse, StringComparison.Ordinal);

        // Neither reads as the raw predicate the defect actually produced.
        Assert.NotEqual(hardMan.ToString(), hardManProse);
        Assert.NotEqual(roughWork.ToString(), roughWorkProse);
        Assert.DoesNotContain("PersonIsCapable", hardManProse, StringComparison.Ordinal);
        Assert.DoesNotContain("PersonIsCapable", roughWorkProse, StringComparison.Ordinal);
        Assert.DoesNotContain(CapabilityBar.HardMan, hardManProse, StringComparison.Ordinal);
        Assert.DoesNotContain(CapabilityBar.RoughWork, roughWorkProse, StringComparison.Ordinal);
    }

    /// <summary>
    /// The falsifier for the whole class of defect `PersonIsCapable` was, not only that one instance:
    /// a <c>ClaimKind</c> added to the vocabulary and never added to <see cref="PlayerNarration.Describe"/>
    /// silently falls through to <c>_ =&gt; c.ToString()</c>, the exact raw developer predicate a
    /// player must never see. This drives every defined <c>ClaimKind</c> through <c>Describe</c> with
    /// a generic claim and asserts the result is never that fallback — so a future kind added to the
    /// enum without a narration arm fails this test immediately, rather than reaching a player first
    /// and being found by scoping the next milestone the way this one was.
    ///
    /// The representative claim is deliberately generic (an arbitrary subject and object id, not a
    /// per-kind fixture) so the test needs no maintenance when a new kind is added — only a narration
    /// arm for it. <c>PersonIsCapable</c>'s object happens not to match either named
    /// <see cref="CapabilityBar"/> constant here, which is fine: the arm's ternary still produces
    /// prose either way, and the dedicated test above pins the exact wording for both real bars.
    /// </summary>
    [Fact]
    public void Every_defined_claim_kind_has_its_own_narration()
    {
        foreach (var kind in Enum.GetValues<ClaimKind>())
        {
            var claim = new Claim(kind, "subject-id", "object-id");
            string rendered = PlayerNarration.Describe(claim, id => id);

            Assert.NotEqual(claim.ToString(), rendered);
        }
    }

    // ================================================================= helpers

    private static Character Salvatore(out World world)
    {
        world = Cast.Build(42, "baseline");
        return world.Get("salvatore");
    }

    private static Character Marco(out World world)
    {
        world = Cast.Build(42, "baseline");
        return world.Get("marco");
    }
}
