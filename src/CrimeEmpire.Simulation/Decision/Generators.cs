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
    IReadOnlyList<string> OrgMemberIds,
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

        // De-duplicate by id, keeping the first proposer.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return all.Where(c => seen.Add(c.Id)).ToList();
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

            // A lead she has already put a name against is not a reason to open the case again.
            var lead = leads.FirstOrDefault(r => !named.Any(v => v.Claim.Object == r.Claim.Subject));
            if (leads.Count > 0 && lead is null) yield break;

            string? target = lead?.Claim.Subject ?? ctx.VisibleTargets.FirstOrDefault();
            if (target is null) yield break;

            yield return new Candidate(
                $"investigate:{target}",
                ActionKind.StartStrategy,
                nameof(FromResponsibility),
                $"open an investigation into events at {target}")
            {
                TargetId = target,
                Strategy = StrategyKind.InvestigateIncident,
                Domain = domain,
                // With no lead, the requirement is one she cannot meet — and the trace says so.
                RequiredKnowledge = new[] { lead?.Claim ?? new Claim(ClaimKind.WitnessSawIncident, target) },
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
                        var ownPart = new[] { question };

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
                    .Select(r => r.Claim)
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
            string? other = ctx.OrgMemberIds
                .Where(id => id != ctx.Actor.Id
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
