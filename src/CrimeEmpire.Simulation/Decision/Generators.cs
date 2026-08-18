namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;

/// <summary>
/// Everything a generator is allowed to see.
///
/// There is no Psychology here and no raw Cognition: options are proposed from role, commitment,
/// pressure, trigger and relationship, and any situational fact must come through Perceived. Note
/// that KnownPolicies is pre-filtered to policies this character has actually been told about — a
/// policy nobody told you cannot shape what occurs to you.
/// </summary>
public sealed record GeneratorContext(
    CharacterView Actor,
    PerceivedSituation Perceived,
    Agenda Agenda,
    DateTime Now,
    ScheduledEvent Trigger,
    Office? MyOffice,
    Assignment? MyAssignment,
    IReadOnlyList<Policy> KnownPolicies,
    string? SuperiorId,
    IReadOnlyList<string> SubordinateIds,
    // Everyone in the same organisation, regardless of rank. Needed because the chain of command
    // is not the only path information can take — a boss seeking a second account has to be able
    // to reach past the man who reports to him.
    //
    // AUTHORITATIVE, AND THEREFORE NOT A LIST OF PEOPLE HE KNOWS ABOUT. It is read off the world's
    // roster. Anything that turns a member into a target must intersect it with AcquaintedIds first;
    // see that field.
    IReadOnlyList<string> OrgMemberIds,
    // Everyone this character could name — the belief-limited half of the pair above, and the only
    // half a target may be drawn from. It is Acquaintance.KnownTo and nothing else: what he holds in
    // cognition and social state, widened ONLY by the holders of his own organisation's named posts
    // (Organization.Offices and BossId). See Decision/Acquaintance.cs.
    //
    // NOT the roster, and no authority scan standing in for an office. Milestone 009's second
    // correction widened this by Pipeline.SuperiorOf and SubordinatesOf, calling them office
    // relationships; those are scans of world.Characters at a neighbouring Authority, which is
    // OrgMemberIds under another name, and it was rejected for it. The third correction settled the
    // rule above — an office relationship is only an office relationship if it comes from an office.
    //
    // Why the field exists at all: the corroboration generator used to pick its target straight out
    // of OrgMemberIds, so a character could put a question to somebody whose existence nothing in his
    // head established, and the player-facing option then resolved that id into a visible name.
    // Belief-limited candidate generation is the rule the architecture states — "reject unknown,
    // impossible, or unavailable candidates" — and a target is exactly the kind of thing that can be
    // unknown.
    IReadOnlyList<string> AcquaintedIds,
    // Reports this character has already sent. Needed so that "report in" stops being a live
    // option once he has said everything he has — otherwise a standing responsibility wakes him
    // on a timer and he volunteers the same account indefinitely.
    IReadOnlyList<Report> ReportsSent,
    // Questions this character has already put to somebody. Distinct from the answers he got:
    // an unanswered question has still been asked, and asking again gets the same silence.
    IReadOnlyList<InformationRequest> RequestsMade,
    // VisibleTargets: entities whose *existence* is public — a storefront can be seen from the
    // street. What is happening inside it is not public, and still requires a held claim.
    IReadOnlyList<string> VisibleTargets);

/// <summary>
/// The bounded set of proposers. The shared action vocabulary never becomes a universal menu:
/// each generator offers at most three options, and the union is trimmed by salience before
/// anything is scored.
/// </summary>
public static class Generators
{
    public static List<Candidate> GenerateAll(GeneratorContext ctx)
    {
        var all = new List<Candidate>();
        all.AddRange(FromCommitment(ctx));
        all.AddRange(FromResponsibility(ctx));
        all.AddRange(FromPressure(ctx));
        all.AddRange(FromTrigger(ctx));
        all.AddRange(FromRelationship(ctx));
        all.AddRange(FromDelegation(ctx));
        // Last, so it fills gaps rather than taking questions off generators that already own them.
        // The (kind, target, claim) dedupe below keeps the first proposer, so a delegator who can
        // already audit his own executor keeps FromDelegation's version and its wording.
        all.AddRange(FromAllegation(ctx));

        // De-duplicate by id, keeping the first proposer.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var deduped = all.Where(c => seen.Add(c.Id)).ToList();

        // And again on what a question actually *is*, which its id does not capture.
        //
        // Two generators can propose the same question: FromRelationship offers to corroborate an
        // account he was given, FromDelegation offers to put it to the man he sent. They build
        // different ids and different wording, so the id pass above lets both through — but if they
        // land on the same target and the same claim they are one act, and offering it twice would
        // spend two of a bounded six slots saying the same thing, then record two rejections for it.
        //
        // Deduplicated on (kind, target, claim) rather than by removing either generator. A direct
        // question to the man who did the work and an unsolicited partial report from him are
        // different acts, and so are corroborating a rumour and auditing your own executor; the
        // overlap is narrow and only the overlap is collapsed. First in generator order wins, which
        // is fixed and therefore deterministic.
        var asked = new HashSet<(ActionKind, string, Claim)>();
        return deduped
            .Where(c => c.Kind != ActionKind.SeekCorroboration
                        || c.TargetId is null
                        || c.AboutClaim is not { } about
                        || asked.Add((c.Kind, c.TargetId, about)))
            .ToList();
    }

    // ---------------------------------------------------------------- current intention
    private static IEnumerable<Candidate> FromCommitment(GeneratorContext ctx)
    {
        if (ctx.Actor.Execution.Strategy is not { } s) yield break;

        yield return new Candidate(
            $"continue:{s.Kind}:{s.TargetId}",
            ActionKind.ContinueStrategy,
            nameof(FromCommitment),
            $"carry on with {s.Label}")
        {
            TargetId = s.TargetId,
            Strategy = s.Kind,
            Method = s.Kind == StrategyKind.SecureTribute ? s.Method : null,
            Domain = s.Domain,
        };

        if (s.Kind == StrategyKind.SecureTribute && Escalate(s.Method) is { } harder)
        {
            yield return Coercive(
                ctx,
                $"escalate:{s.TargetId}:{harder}",
                ActionKind.AlterStrategy,
                nameof(FromCommitment),
                $"push harder on {s.TargetId} — {harder.ToString().ToLowerInvariant()} instead",
                s.TargetId!,
                StrategyKind.SecureTribute,
                harder,
                s.Domain);
        }

        yield return new Candidate(
            $"abandon:{s.Kind}:{s.TargetId}",
            ActionKind.AbandonStrategy,
            nameof(FromCommitment),
            $"drop {s.Label}")
        {
            TargetId = s.TargetId,
            Strategy = s.Kind,
            Domain = s.Domain,
        };
    }

    // ---------------------------------------------------------------- role and responsibility
    private static IEnumerable<Candidate> FromResponsibility(GeneratorContext ctx)
    {
        string? domain = ctx.Agenda.Domain ?? ctx.MyOffice?.Domain;
        if (domain is null) yield break;

        // Already running something in this domain: FromCommitment owns that.
        if (ctx.Actor.Execution.Strategy?.Domain == domain) yield break;

        bool investigator = ctx.Actor.Capabilities[Skill.Investigation] >= 0.4;

        if (investigator)
        {
            // Work from an actual lead if she has one. Claims are matched by identity, so the
            // requirement has to name the record she really holds rather than a reconstructed
            // one — otherwise a detective is refused permission to investigate her own lead.
            var leads = ctx.Perceived.OfKind(ClaimKind.WitnessSawIncident).ToList();
            var named = ctx.Perceived.OfKind(ClaimKind.PersonUsedViolence).ToList();

            // A lead she has already put a name against is not a reason to open the case again —
            // and "already named" is a question about the incident, never about the address. Matched
            // on Claim.EventId, so a second beating at a shop she has already closed a case on is
            // still a live lead. Comparing the violence claim's Object to the lead's Subject asked
            // whether she had named anybody for anything at that location, which is milestone 005's
            // ruling read off the map instead of the case.
            var lead = leads.FirstOrDefault(r => !named.Any(v => SameIncident(v.Claim, r.Claim)));
            if (leads.Count > 0 && lead is null) yield break;

            string? target = lead?.Claim.Subject ?? ctx.VisibleTargets.FirstOrDefault();
            if (target is null) yield break;

            yield return new Candidate(
                // Keyed on the incident where there is one, so two cases at one address are two
                // candidates rather than one that looks like a repeat of itself.
                lead is not null ? $"investigate:{target}:{lead.Claim.EventId}" : $"investigate:{target}",
                ActionKind.StartStrategy,
                nameof(FromResponsibility),
                $"open an investigation into events at {target}")
            {
                TargetId = target,
                Strategy = StrategyKind.InvestigateIncident,
                Domain = domain,
                // With no lead, the requirement is one she cannot meet — and the trace says so.
                RequiredKnowledge = new[] { lead?.Claim ?? new Claim(ClaimKind.WitnessSawIncident, target) },
                // Which incident the case is about, so its steps can act on it. Null when she has no
                // lead, which is also the case the knowledge filter refuses.
                AboutIncident = lead?.Claim,
                RequiredSkill = Skill.Investigation,
                RequiredSkillLevel = 0.3,
            };
            yield break;
        }

        // Collection duty belongs to whoever holds the office or was given the job. A boss with a
        // general interest in revenue does not personally lean on a grocer — that is what the
        // office-to-assignment chain exists to prevent.
        if (ctx.MyOffice is null && ctx.MyAssignment is null) yield break;

        var refusing = ctx.Perceived.OfKind(ClaimKind.BusinessRefusesTribute)
                                    .Select(r => r.Claim.Subject)
                                    .FirstOrDefault();
        string? mark = refusing ?? ctx.VisibleTargets.FirstOrDefault();
        if (mark is null) yield break;

        foreach (var method in new[] { CoercionMethod.Persuade, CoercionMethod.Threaten, CoercionMethod.Force })
        {
            yield return Coercive(
                ctx,
                $"start:tribute:{mark}:{method}",
                ActionKind.StartStrategy,
                nameof(FromResponsibility),
                Verb(method, mark),
                mark,
                StrategyKind.SecureTribute,
                method,
                domain,
                requires: new[] { new Claim(ClaimKind.BusinessRefusesTribute, mark) });
        }
    }

    // ---------------------------------------------------------------- pressure
    private static IEnumerable<Candidate> FromPressure(GeneratorContext ctx)
    {
        if (ctx.Actor.Motivations.Dominant() is not { } dominant) yield break;
        if (dominant.Value < 0.3) yield break;

        switch (dominant.Kind)
        {
            case PressureKind.LegalExposure:
            {
                var incident = ctx.Perceived.OfKind(ClaimKind.PersonUsedViolence)
                    .FirstOrDefault(r => r.Claim.Subject == ctx.Actor.Id
                                      || ctx.SubordinateIds.Contains(r.Claim.Subject));
                if (incident is null) yield break;
                yield return new Candidate(
                    $"conceal:{incident.Claim.EventId}",
                    ActionKind.StartStrategy,
                    nameof(FromPressure),
                    "clean up after the incident before anyone else does")
                {
                    TargetId = incident.Claim.Object,
                    Strategy = StrategyKind.ConcealIncident,
                    Domain = ctx.MyOffice?.Domain ?? ctx.Agenda.Domain,
                    RequiredKnowledge = new[] { incident.Claim },
                    RequiredSkill = Skill.Discretion,
                    RequiredSkillLevel = 0.2,
                    RequiredCrew = 1,
                    AboutIncident = incident.Claim,
                };
                break;
            }

            case PressureKind.Resentment:
            {
                var grievance = ctx.Actor.Social.Grievances
                    .OrderByDescending(g => g.Severity)
                    .FirstOrDefault();
                if (grievance is null) yield break;

                // If the grudge is about a specific breach, moving on it requires still believing
                // the breach happened — and naming the claim here is what puts it into the trace.
                var basis = ctx.Perceived.OfKind(ClaimKind.PersonBreachedPolicy)
                    .FirstOrDefault(r => r.Claim.Subject == grievance.AgainstId);

                yield return new Candidate(
                    $"retaliate:{grievance.AgainstId}",
                    ActionKind.Retaliate,
                    nameof(FromPressure),
                    $"move against {grievance.AgainstId} over {grievance.Description}")
                {
                    TargetId = grievance.AgainstId,
                    Domain = ctx.Agenda.Domain,
                    RequiredAuthority = 2,
                    RequiredKnowledge = basis is null
                        ? Array.Empty<Claim>()
                        : new[] { basis.Claim },
                };
                break;
            }

            case PressureKind.RevenueShortfall when ctx.Actor.Execution.Strategy is { } s
                                                    && s.Kind == StrategyKind.SecureTribute
                                                    && Escalate(s.Method) is { } harder:
            {
                yield return Coercive(
                    ctx,
                    $"escalate:{s.TargetId}:{harder}",
                    ActionKind.AlterStrategy,
                    nameof(FromPressure),
                    $"the money is not arriving — {harder.ToString().ToLowerInvariant()} {s.TargetId}",
                    s.TargetId!,
                    StrategyKind.SecureTribute,
                    harder,
                    s.Domain);
                break;
            }
        }
    }

    // ---------------------------------------------------------------- direct response to the trigger
    private static IEnumerable<Candidate> FromTrigger(GeneratorContext ctx)
    {
        // The floor. Doing nothing is always conceivable, so a choice is never forced by an empty set.
        yield return new Candidate("nothing", ActionKind.DoNothing, nameof(FromTrigger), "let it lie");

        if (ctx.Trigger.Kind == EventKind.Incident && ctx.Trigger.Payload.Note == "tribute-demanded")
        {
            string from = ctx.Trigger.Payload.TargetId ?? "";
            yield return new Candidate($"concede:{from}", ActionKind.Concede, nameof(FromTrigger), $"pay what {from} is asking")
            {
                TargetId = from,
            };
            yield return new Candidate($"refuse:{from}", ActionKind.Refuse, nameof(FromTrigger), $"refuse {from}")
            {
                TargetId = from,
            };
        }
    }

    // ---------------------------------------------------------------- relationships
    private static IEnumerable<Candidate> FromRelationship(GeneratorContext ctx)
    {
        var s = ctx.Actor.Execution.Strategy;

        if (s is not null && s.DelegatedToId is null && ctx.SubordinateIds.Count > 0)
        {
            string sub = ctx.SubordinateIds
                .OrderByDescending(id => ctx.Actor.Social.Toward(id).Trust)
                .ThenBy(id => id, StringComparer.Ordinal)
                .First();

            yield return new Candidate(
                $"delegate:{s.Kind}:{sub}",
                ActionKind.DelegateStrategy,
                nameof(FromRelationship),
                $"have {sub} handle {s.Label}")
            {
                TargetId = sub,
                Strategy = s.Kind,
                Method = s.Kind == StrategyKind.SecureTribute ? s.Method : null,
                Domain = s.Domain,
                RequiredCrew = 1,
            };
        }

        // Being asked directly changes who he answers to for this one exchange. Without it a
        // soldier can only ever report up one rung, and a boss who goes round his capo to ask a
        // question gets no reply — the request would reach a man whose only reporting route leads
        // back to the person he was asked about.
        string? asker = null;
        Claim? askedAbout = null;
        if (ctx.Trigger.Payload.Note == "asked-to-account")
        {
            asker = ctx.Trigger.Payload.TargetId;
            askedAbout = ctx.Trigger.Payload.AboutClaim;
        }

        // Who this account goes to. Being asked directly redirects the answer for this one
        // exchange — it does not make the asker his superior, and nothing below may treat it as
        // though it had. Authority questions key off ctx.SuperiorId, never off this.
        if ((asker ?? ctx.SuperiorId) is { } recipient
            && (askedAbout is not null || (asker is null && HasSomethingNewFor(ctx, recipient))))
        {
            // Answering a direct question and filing an account are different acts. Conflating them
            // is what let a man asked about one specific beating answer with whatever three things
            // happened to be most newsworthy — which is not an answer to anything.
            if (askedAbout is { } question)
            {
                // He answers out of what he has, and every record counts, not only held ones: a man
                // who has come to reject something can still say so, and that denial is a real
                // answer. Where he has no position at all, nothing is offered and the request goes
                // unanswered — already a modelled outcome, and the only honest one. He cannot be
                // made to produce information he does not possess.
                var position = ctx.Perceived.Position(question);

                if (position is not null)
                {
                    // Whether concealing this would be concealment at all.
                    //
                    // Naming him is not enough. If he does not hold the thing, saying it did not
                    // happen is his honest position, and offering that as a False report would file
                    // a sincere denial as a lie — destroying the one distinction ReportCandor
                    // exists to draw, and doing it in the direction that makes an innocent man look
                    // guilty in the developer trace. There is likewise nothing to leave out of an
                    // account when he does not believe there was anything to leave out.
                    bool namesHim =
                        question.Kind is ClaimKind.PersonUsedViolence or ClaimKind.PersonBreachedPolicy
                        && question.Subject == ctx.Actor.Id;
                    bool couldDeceive = namesHim && position.IsHeld;

                    yield return new Candidate(
                        $"answer:{recipient}:{question}",
                        ActionKind.ReportToSuperior,
                        nameof(FromRelationship),
                        $"give {recipient} his account of {question}")
                    {
                        TargetId = recipient,
                        Domain = ctx.Agenda.Domain,
                        Candor = ReportCandor.Candid,
                        AnsweringClaim = question,
                    };

                    if (couldDeceive)
                    {
                        // What this recipient already has from him about it. Read here, where his own
                        // sent reports are in scope, and carried on the candidate — Utility scores
                        // from PerceivedSituation and never sees a report log.
                        var ownPart = new[]
                        {
                            new SuppressedClaim(
                                question,
                                Reporting.PriorDisclosure(ctx.ReportsSent, recipient, question)),
                        };

                        yield return new Candidate(
                            $"answer:{recipient}:{question}:partial",
                            ActionKind.ReportToSuperior,
                            nameof(FromRelationship),
                            $"give {recipient} nothing on his own part in it")
                        {
                            TargetId = recipient,
                            Domain = ctx.Agenda.Domain,
                            Candor = ReportCandor.Partial,
                            Suppressed = ownPart,
                            AnsweringClaim = question,
                        };

                        yield return new Candidate(
                            $"answer:{recipient}:{question}:false",
                            ActionKind.ReportToSuperior,
                            nameof(FromRelationship),
                            $"tell {recipient} it did not happen")
                        {
                            TargetId = recipient,
                            Domain = ctx.Agenda.Domain,
                            Candor = ReportCandor.False,
                            Suppressed = ownPart,
                            AnsweringClaim = question,
                        };
                    }
                }
            }
            else
            {
                // What he would rather his superior did not hear: things he holds that name him as
                // the one who used force or went outside a rule. Nothing else is worth lying about,
                // so if this is empty the only report he can conceive of is the honest one.
                var awkward = ctx.Perceived.OfKind(ClaimKind.PersonUsedViolence)
                    .Concat(ctx.Perceived.OfKind(ClaimKind.PersonBreachedPolicy))
                    .Where(r => r.Claim.Subject == ctx.Actor.Id)
                    .Select(r => new SuppressedClaim(
                        r.Claim, Reporting.PriorDisclosure(ctx.ReportsSent, recipient, r.Claim)))
                    .ToList();

                yield return new Candidate(
                    $"report:{recipient}",
                    ActionKind.ReportToSuperior,
                    nameof(FromRelationship),
                    $"report the situation to {recipient}")
                {
                    TargetId = recipient,
                    Domain = ctx.Agenda.Domain,
                    Candor = ReportCandor.Candid,
                };

                if (awkward.Count > 0)
                {
                    yield return new Candidate(
                        $"report:{recipient}:partial",
                        ActionKind.ReportToSuperior,
                        nameof(FromRelationship),
                        $"report to {recipient}, leaving out his own part")
                    {
                        TargetId = recipient,
                        Domain = ctx.Agenda.Domain,
                        Candor = ReportCandor.Partial,
                        Suppressed = awkward,
                    };

                    yield return new Candidate(
                        $"report:{recipient}:false",
                        ActionKind.ReportToSuperior,
                        nameof(FromRelationship),
                        $"tell {recipient} it did not happen")
                    {
                        TargetId = recipient,
                        Domain = ctx.Agenda.Domain,
                        Candor = ReportCandor.False,
                        Suppressed = awkward,
                    };
                }
            }

            // Permission is a question for the man above him, and only for him.
            //
            // Keyed off SuperiorId rather than the recipient of this account. Being asked a
            // question redirects who he answers to; it does not put the asker over him. Reusing the
            // recipient here had a boss with no superior fall back to whoever had just questioned
            // him, so Salvatore asked his own soldier for permission to relax Salvatore's own
            // policy — authority running backwards down the chain because of who spoke last.
            //
            // A man with nobody above him has nobody to ask. That is not a gap to fill with the
            // nearest available person; it is what being the boss means, and the option simply does
            // not arise for him.
            if (ctx.SuperiorId is { } authority)
            {
                // Only conceivable if he knows there is a rule to ask about.
                var relevant = ctx.KnownPolicies.FirstOrDefault(p => p.Domain == (ctx.Agenda.Domain ?? ""));
                if (relevant is not null)
                {
                    yield return new Candidate(
                        $"approval:{authority}:{relevant.Id}",
                        ActionKind.SeekApproval,
                        nameof(FromRelationship),
                        $"ask {authority} to relax \"{relevant.Description}\"")
                    {
                        TargetId = authority,
                        Domain = ctx.Agenda.Domain,
                        RequiredKnowledge = new[] { relevant.AwarenessClaim(ctx.Actor.Social.OrganizationId ?? "") },
                    };
                }
            }
        }

        // Going to somebody else for their version of the same thing.
        //
        // Only conceivable about something he was told rather than saw — there is nothing to
        // corroborate about your own eyes — and only worth doing if there is somebody to ask who
        // is not the man who told him. This is the recipient-initiated direction of the same
        // report channel, which INFORMATION_AND_LEGIBILITY.md sanctions directly: leaders can
        // request audits and seek corroboration. Without it a boss can only ever hear from the
        // one subordinate who reports to him, and a lie from that subordinate is unfalsifiable.
        // Anything he was told, not only what came up the formal channel. A participant's own
        // account is testimony too and is exactly the kind of thing worth checking against a second
        // voice; keying this on Report alone would quietly exempt it.
        var secondhand = ctx.Perceived.Beliefs
            .Where(b => b.IsHeld && b.SourceKind.IsTestimony())
            .OrderBy(b => b.Confidence)
            .ThenBy(b => b.Claim.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();

        if (secondhand is not null)
        {
            // Whom he could actually go to: in the outfit, and somebody he could name. The second
            // half is the belief limit — Acquaintance.KnownTo, not the roster and not an authority
            // scan over it — without which the roster itself proposed the target, so a man could go
            // and put a question to somebody he had no way of knowing existed.
            var acquainted = new HashSet<string>(ctx.AcquaintedIds, StringComparer.Ordinal);

            string? other = ctx.OrgMemberIds
                .Where(id => id != ctx.Actor.Id
                             && acquainted.Contains(id)
                             && id != secondhand.SourceId
                             // Not the man who just asked him to account for himself. Answering a
                             // question with the same question back is not corroboration.
                             && id != asker
                             // Not anyone who has already given his version *of this*. Scoped to
                             // the claim, because that is the question being spent. Asking only
                             // whether the man had ever said anything meant one remark about a
                             // shakedown stood as his answer to every question that could later be
                             // put to him — the same conflation as spending the pair rather than
                             // the question, one layer up.
                             && !ctx.Perceived.HasAccountFrom(id, secondhand.Claim)
                             // And — the part that actually bounds this — not anyone he has
                             // already asked. Filtering on replies alone meant a question that
                             // went unanswered left no trace, so the asker put it again on every
                             // wake: 318 requests against 5 replies in the disloyal run, because
                             // the one thing that ends the loop is the answer, and the answer is
                             // the other man's decision to make, not his — but only about this
                             // same matter. Spending the pair rather than the question meant one
                             // shakedown enquiry barred him from ever asking that man about
                             // anything else for the rest of the run.
                             && CanAsk(ctx.RequestsMade, id, secondhand.Claim))
                .OrderBy(id => id, StringComparer.Ordinal)
                .FirstOrDefault();

            if (other is not null)
                yield return new Candidate(
                    $"corroborate:{other}:{secondhand.Claim}",
                    ActionKind.SeekCorroboration,
                    nameof(FromRelationship),
                    $"ask {other} for his own account")
                {
                    TargetId = other,
                    Domain = ctx.Agenda.Domain,
                    AboutClaim = secondhand.Claim,
                };
        }
    }

    // ---------------------------------------------------------------- allegation
    /// <summary>
    /// Putting an allegation to the person it names.
    ///
    /// <b>The complement of the corroboration route, and the two together cover every provenance
    /// exactly once.</b> What you were told, you check against somebody else — that is
    /// <see cref="FromRelationship"/>'s <c>secondhand</c> branch, and its reasoning is that there is
    /// nothing to corroborate about your own eyes. What you worked out or came across yourself, you
    /// put to the man it names. Neither is a special case of the other, and this is the same shape
    /// milestone 007 chose when it added <see cref="FromDelegation"/> rather than relaxing the
    /// corroboration restriction: *the restriction is right for corroboration, and the answer is a
    /// different generator rather than a wider one.* Widening a limit by renaming its justification
    /// is what got milestone 009's second correction rejected.
    ///
    /// <b>Why this is not a detective's action.</b> Nothing here reads a role, a title or a skill.
    /// It is the general act of asking somebody about something you hold against them, and every
    /// character with such a belief gets it. It happens to matter most for an investigator, because
    /// **an investigator's beliefs are self-acquired by construction** — she works things out and
    /// comes across things, never being told — so the corroboration route can never fire for her,
    /// she delegates to nobody, and belonging to no organisation she has no superior to report to.
    /// Before this she had no way to put a question to anybody at all: her candidate set after naming
    /// a suspect was one option, <c>let it lie</c>, with nothing generated and nothing rejected.
    ///
    /// The two claim kinds are the two that name a person as having done something, which is the
    /// same pair the answering branch tests for <c>namesHim</c>. That correspondence is the point:
    /// a question this generator can ask is a question the recipient can answer candidly, partially
    /// or falsely, so the exchange is a real one rather than a message with no reply.
    /// </summary>
    private static IEnumerable<Candidate> FromAllegation(GeneratorContext ctx)
    {
        var acquainted = new HashSet<string>(ctx.AcquaintedIds, StringComparer.Ordinal);

        foreach (var held in ctx.Perceived.Beliefs
                     .Where(b => b.IsHeld
                                 && b.Claim.Kind is ClaimKind.PersonUsedViolence
                                                 or ClaimKind.PersonBreachedPolicy
                                 // Not what he was told — that is the corroboration route's, and
                                 // offering both would be one act proposed twice.
                                 && !b.SourceKind.IsTestimony())
                     // Thinnest first, matching every other question generator: the account he is
                     // least sure of is the one worth putting.
                     .OrderBy(b => b.Confidence)
                     .ThenBy(b => b.Claim.ToString(), StringComparer.Ordinal))
        {
            string subject = held.Claim.Subject;

            // Not himself. A man does not put his own act to himself, and the answering branch would
            // have nothing to do with it.
            if (subject == ctx.Actor.Id) continue;

            // Somebody he could name. Milestone 009's rule, over every candidate's target and not
            // only the one that was reported — Acquaintance.KnownTo is the single derivation.
            if (!acquainted.Contains(subject)) continue;

            // Spent when asked, like every other question, and not worth putting to a man who has
            // already given his version of this.
            if (!CanAsk(ctx.RequestsMade, subject, held.Claim)) continue;
            if (ctx.Perceived.HasAccountFrom(subject, held.Claim)) continue;

            yield return new Candidate(
                $"allege:{subject}:{held.Claim}",
                ActionKind.SeekCorroboration,
                nameof(FromAllegation),
                $"put it to {subject} directly")
            {
                TargetId = subject,
                Domain = ctx.Agenda.Domain,
                AboutClaim = held.Claim,
            };

            // One question at a time, for the same reason FromDelegation stops at one: a bounded
            // candidate set is not a place to put a whole case file.
            yield break;
        }
    }

    // ---------------------------------------------------------------- delegation
    /// <summary>
    /// A delegator asking the man he sent to account for what he did.
    ///
    /// This is the one route by which an account travels *down* the chain of delegation rather than
    /// up it, and without it the executor's version of his own work never reaches the person who
    /// ordered it. Before this existed the only character who ever put a question was whoever
    /// happened to hold a shaky second-hand belief, and being asked redirects the answer to the
    /// asker — so a soldier's account went to the boss and never to the capo who sent him.
    ///
    /// Deliberately separate from the corroboration generator in <see cref="FromRelationship"/>,
    /// which refuses anything the asker established himself on the reasoning that there is nothing
    /// to corroborate about your own eyes. That reasoning is right for corroboration and wrong here:
    /// a delegator who finds traces that his own man wrecked a shopfront is not trying to confirm
    /// what he saw, he is asking for an account of work he ordered. Which is exactly the case where
    /// the executor has a reason to shade it — so the restriction was quietly removing the most
    /// interesting exchange in the model.
    ///
    /// INFORMATION_AND_LEGIBILITY.md sanctions this directly: delegation changes information
    /// topology, a subordinate assigned an operation may owe an account of it, and leaders can
    /// request audits and seek corroboration.
    ///
    /// Note what this does *not* consult: no relationship dimension gates it, orders it, or scores
    /// it. Whether the delegator trusts the man has no bearing on whether asking occurs to him —
    /// relationship influence stays confined to utility, per the milestone's scoring-only ruling.
    /// </summary>
    private static IEnumerable<Candidate> FromDelegation(GeneratorContext ctx)
    {
        // Everyone who has executed work for him, whether or not that work is still running. The
        // ordering is the order he first delegated to them, which is stable and not a dictionary
        // traversal.
        foreach (string executor in ctx.Actor.Execution.DelegatedExecutorIds)
        {
            // Something he holds about what that man has done. Ordered by how thin it is, so the
            // account he is least sure of is the one he goes after first.
            InformationRecord? about = null;
            foreach (var b in ctx.Perceived.Beliefs
                         .Where(b => b.IsHeld && b.Claim.Subject == executor)
                         .OrderBy(b => b.Confidence)
                         .ThenBy(b => b.Claim.ToString(), StringComparer.Ordinal))
            {
                // Spent when asked, like every other question, and not worth asking a man who has
                // already given his version of this.
                if (!CanAsk(ctx.RequestsMade, executor, b.Claim)) continue;
                if (ctx.Perceived.HasAccountFrom(executor, b.Claim)) continue;
                about = b;
                break;
            }

            if (about is null) continue;

            yield return new Candidate(
                $"account:{executor}:{about.Claim}",
                ActionKind.SeekCorroboration,
                nameof(FromDelegation),
                $"put it to {executor} directly and hear his account")
            {
                TargetId = executor,
                Domain = ctx.Agenda.Domain,
                AboutClaim = about.Claim,
            };

            // One question at a time. A generator that offered one per executor per claim would put
            // the whole audit in front of him at once, which is not how a bounded candidate set works.
            yield break;
        }
    }

    /// <summary>
    /// Whether anything has changed in what he holds since he last reported to this person.
    ///
    /// "Something to say" is not only a newly acquired fact. Being contradicted, being
    /// corroborated, or having a stance or confidence move are all developments worth carrying to
    /// a superior — a capo who learns his account is disputed has something to report even though
    /// he acquired no new claim. Testing acquisition alone missed every one of those, so a
    /// character could sit on a belief that had been overturned since he last spoke.
    ///
    /// Reconsideration time is the right test precisely because
    /// <see cref="Cognition.Receive"/> only advances it when something actually changed; hearing
    /// the same man repeat himself leaves it alone, so this cannot become permanently true.
    ///
    /// Being asked directly bypasses this — a question deserves an answer even if the answer is
    /// the one already given.
    /// </summary>
    /// <summary>
    /// Whether two claims are about the same incident.
    ///
    /// The predicate rather than the kinds: a `WitnessSawIncident` lead, the `PersonUsedViolence` it
    /// leads to, and the `PoliceInvestigating` that follows are three statements about one event, and
    /// what ties them together is <see cref="Claim.EventId"/>. **Event 0 is not an incident** — it is
    /// the default for a claim that names none, and matching on it would make every unattributed
    /// claim share a single incident, which is the scan defect milestone 010 removed from
    /// <c>Utility</c> reappearing one layer up.
    ///
    /// Public and parameterised for the same reason as <see cref="CanAsk"/> and
    /// <see cref="HasSomethingToReport"/>: a rule buried inside a LINQ predicate is a rule nothing
    /// can test directly, and this repository has already had two regression tests pass against a
    /// hand-written copy of a rule rather than against the rule.
    /// </summary>
    public static bool SameIncident(Claim a, Claim b) => a.EventId != 0 && a.EventId == b.EventId;

    private static bool HasSomethingNewFor(GeneratorContext ctx, string recipientId)
        => HasSomethingToReport(ctx.ReportsSent, ctx.Perceived.Beliefs, recipientId);

    /// <summary>
    /// Whether this character can still put this question to this person.
    ///
    /// A question is spent when asked — the reply is the other man's to give or withhold, so
    /// waiting for one means asking forever. But what is spent is the question, not the
    /// relationship: scoping the record to a pair meant a single enquiry barred him from ever
    /// asking that man about anything again.
    ///
    /// Public and parameterised for the same reason as
    /// <see cref="HasSomethingToReport"/>: a rule buried in a LINQ predicate inside a generator is
    /// a rule nothing can test, and the first attempt at a regression test for this asserted
    /// against a hand-written copy of the condition rather than against the condition itself.
    /// </summary>
    public static bool CanAsk(
        IEnumerable<InformationRequest> requestsMade, string askedId, Claim about)
    {
        foreach (var r in requestsMade)
            if (r.AskedId == askedId && r.About.Equals(about)) return false;
        return true;
    }

    /// <summary>
    /// The eligibility rule itself, taking only what it needs so it can be exercised directly.
    ///
    /// Kept public and parameterised rather than reading it off a GeneratorContext, because the
    /// interesting cases — reported, then contradicted, having acquired nothing new — are awkward
    /// to stage through a whole simulation run, and a rule that can only be tested end-to-end
    /// tends not to be tested at all. That is how the acquisition-only version survived.
    /// </summary>
    public static bool HasSomethingToReport(
        IEnumerable<Report> reportsSent,
        IEnumerable<InformationRecord> beliefs,
        string recipientId)
    {
        var sent = reportsSent as IReadOnlyCollection<Report> ?? reportsSent.ToList();

        // Per claim, not per report. Comparing against the timestamp of his last report said
        // "covered" about everything the moment he said anything, so a position squeezed out by
        // the length cap was silently retired — the report both dropped it and marked it handled.
        //
        // Note also the absence of an IsHeld filter. Learning that something he reported is not so
        // is one of the most worthwhile things he can carry back, and a stance fallen to Doubts or
        // Rejects is exactly that.
        foreach (var b in beliefs)
            if (Reporting.NeedsConveying(sent, recipientId, b)) return true;

        return false;
    }

    // ---------------------------------------------------------------- helpers
    /// <summary>
    /// Builds a coercive candidate and attaches any policy it would breach — but only from policies
    /// the character knows about, so an unknown rule exerts no pull.
    /// </summary>
    private static Candidate Coercive(
        GeneratorContext ctx,
        string id,
        ActionKind kind,
        string generator,
        string description,
        string targetId,
        StrategyKind strategy,
        CoercionMethod method,
        string? domain,
        IReadOnlyList<Claim>? requires = null)
    {
        Policy? breached = null;
        foreach (var p in ctx.KnownPolicies)
        {
            bool violates = p.Kind switch
            {
                PolicyKind.NoPublicViolence => method == CoercionMethod.Force && p.Domain == domain,
                PolicyKind.ProtectBusiness => method != CoercionMethod.Persuade && p.Domain == targetId,
                _ => false,
            };
            if (violates) { breached = p; break; }
        }

        return new Candidate(id, kind, generator, description)
        {
            TargetId = targetId,
            Strategy = strategy,
            Method = method,
            Domain = domain,
            RequiredKnowledge = requires ?? Array.Empty<Claim>(),
            RequiredSkill = method == CoercionMethod.Persuade ? Skill.Persuasion : Skill.Coercion,
            RequiredSkillLevel = method == CoercionMethod.Force ? 0.3 : 0.15,
            RequiredCrew = method == CoercionMethod.Force ? 2 : method == CoercionMethod.Threaten ? 1 : 0,
            BreachesPolicyId = breached?.Id,
            BreachesPolicyStrength = breached?.Strength ?? 0,
            PolicyIssuerId = breached is null ? null : ctx.SuperiorId,
        };
    }

    private static CoercionMethod? Escalate(CoercionMethod m) => m switch
    {
        CoercionMethod.Persuade => CoercionMethod.Threaten,
        CoercionMethod.Threaten => CoercionMethod.Force,
        _ => null,
    };

    private static string Verb(CoercionMethod m, string target) => m switch
    {
        CoercionMethod.Persuade => $"talk {target} round",
        CoercionMethod.Threaten => $"lean on {target}",
        _ => $"strong-arm {target}",
    };
}
