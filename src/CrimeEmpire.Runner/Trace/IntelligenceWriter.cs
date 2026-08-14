namespace CrimeSim.Trace;

using System.Text;
using CrimeSim.Domain;
using CrimeSim.Sim;

/// <summary>
/// What one character could actually tell you, rendered as the player sees it.
///
/// THE RULE THIS FILE EXISTS TO ENFORCE: everything below is derived from the viewpoint
/// character's own <see cref="Cognition"/> — his settled beliefs and the accounts he has been
/// given — and from nothing else. It must never read <see cref="World.TruthLog"/>,
/// <see cref="World.Decisions"/>, <see cref="World.Reports"/>, or any utility score.
/// <c>World</c> is passed only to turn ids into display names, which are public knowledge.
///
/// This is the deliberate opposite of TraceWriter.cs, which shows the true trigger, the real score
/// components and the chosen strategy. INFORMATION_AND_LEGIBILITY.md requires development traces
/// stay separate from player-facing information, and separate files is the cheapest way to make
/// that separation visible rather than merely intended.
///
/// Two smaller rules follow from the same document. Confidence appears only as qualitative
/// language — never a number, because an exact probability is hidden state wearing a percentage
/// sign. And an account is attributed to the source the player can actually see, so that
/// "Vincent says" and "he saw it himself" never collapse into the same sentence.
/// </summary>
public static class IntelligenceWriter
{
    public static string Render(World world, string viewpointId)
    {
        var who = world.Get(viewpointId);
        var sb = new StringBuilder();

        string Name(string id) =>
            world.Find(id)?.Name
            ?? world.Businesses.GetValueOrDefault(id)?.Name
            ?? id;

        sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
        sb.AppendLine($" WHAT {who.Name.ToUpperInvariant()} KNOWS — {who.RoleTitle}");
        sb.AppendLine($" as of {world.Now:d MMMM yyyy}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine(" Everything here is something he saw or was told. Where it is wrong, it is");
        sb.AppendLine(" wrong because somebody was wrong or somebody lied.");
        sb.AppendLine();

        var held = who.Cognition.Records
            .Where(r => r.IsHeld)
            .OrderBy(r => r.AcquiredAt)
            .ThenBy(r => r.Claim.ToString(), StringComparer.Ordinal)
            .ToList();

        // ---------------------------------------------------------------- what he has
        sb.AppendLine("WHAT HE HAS");
        sb.AppendLine();
        if (held.Count == 0)
        {
            sb.AppendLine("  Nothing. Nobody has told him anything and he has seen nothing himself.");
        }
        else
        {
            foreach (var r in held)
            {
                string mark = who.Cognition.IsContested(r.Claim) ? " ⚠" : "";
                sb.AppendLine($"  {r.AcquiredAt:d MMM}  {Describe(r.Claim, Name)}{mark}");
                sb.AppendLine($"           {Qualify(r, who.Cognition.IsContested(r.Claim))}, {Attribute(r, Name)}");
            }
        }
        sb.AppendLine();

        // ---------------------------------------------------------------- disagreement
        var contested = who.Cognition.Testimony
            .Select(t => t.Claim)
            .Distinct()
            .Where(who.Cognition.IsContested)
            .OrderBy(c => c.ToString(), StringComparer.Ordinal)
            .ToList();

        if (contested.Count > 0)
        {
            sb.AppendLine("ACCOUNTS THAT DO NOT AGREE");
            sb.AppendLine();
            foreach (var claim in contested)
            {
                sb.AppendLine($"  On whether {Describe(claim, Name)}:");

                // What he had on his own account — seen or worked out — belongs in the list
                // alongside the others rather than above them as the answer. A conclusion he
                // reached himself is still just one account, and it is the one most likely to be
                // wrong about who was responsible.
                // Only what he came to on his own account belongs here as a separate voice. Testing
                // for "not a report" used to serve, because everything else was either unmediated
                // or inference; with first-hand testimony in the vocabulary that test would file
                // somebody else's account as his own, and it is already listed below under the
                // name of the man who gave it.
                if (who.Cognition.Find(claim) is { } own
                    && (own.SourceKind.IsSelfAcquired() || own.SourceKind == SourceKind.Inference))
                {
                    string basis = own.SourceKind switch
                    {
                        SourceKind.Participant => "his own doing",
                        SourceKind.Witness => "his own eyes",
                        SourceKind.Discovery => "what he came across",
                        _ => "what he worked out",
                    };
                    sb.AppendLine($"     {basis,-20} — {(own.IsHeld ? "it happened" : "it did not")}");
                }

                foreach (var t in who.Cognition.AccountsOf(claim).OrderBy(t => t.At))
                    sb.AppendLine($"     {Name(t.SenderId),-20} — {(t.Affirms ? "it happened" : "it did not")} ({t.At:d MMM})");

                sb.AppendLine();
            }
        }

        // ---------------------------------------------------------------- how he takes people
        //
        // His own attitude, and only ever his own. This section renders the viewpoint character's
        // relationships outward — what *he* makes of the people he deals with — and nothing about
        // what they make of him, which is their private state and not his to know.
        //
        // Three rules it must keep, and each one is a way this section could leak:
        //
        //  - Never a number. "He would not take his word for much" is a reading; "trust 0.12" is
        //    hidden state with a decimal point.
        //  - Never an accusation. A relationship that cooled because somebody contradicted him says
        //    exactly that — he no longer takes the man's word — and never that he was lied to. The
        //    simulation itself cannot tell a lie from an honest disagreement here, and the player
        //    must not be handed a certainty the model does not have.
        //  - Never anybody he does not know. Bounded by KnownPeople, like every other section.
        var attitudes = KnownPeople(world, who)
            .Select(id => (Id: id, Rel: who.Social.Toward(id)))
            .Where(x => x.Rel.Trust > 0 || x.Rel.Fear > 0 || x.Rel.Grievances.Count > 0)
            .ToList();

        if (attitudes.Count > 0)
        {
            sb.AppendLine("HOW HE TAKES THEM");
            sb.AppendLine();
            foreach (var (id, rel) in attitudes)
            {
                sb.AppendLine($"  {Name(id)}");
                sb.AppendLine($"     {Standing(rel.Trust)}");
                if (rel.Fear > 0.25)
                    sb.AppendLine($"     {(rel.Fear > 0.6 ? "he is frightened of him" : "he is wary of him")}");
                // Quoted rather than folded into the sentence. Grievance descriptions are written
                // from the holder's own side and mostly in the first person — "moved against me",
                // "the harbour was handed to me only after it stopped earning" — so embedding one
                // after "he holds it against him that" produces broken English. As his own words
                // about it, they read correctly and stay his.
                foreach (var g in rel.Grievances)
                    sb.AppendLine($"     what he holds against him: \"{g.Description}\"");
            }
            sb.AppendLine();
        }

        // ---------------------------------------------------------------- open questions
        var thin = held
            .Where(r => r.Confidence < 0.5 || who.Cognition.IsContested(r.Claim))
            .OrderBy(r => r.Claim.ToString(), StringComparer.Ordinal)
            .ToList();

        // Who he could go to — drawn from the people he has actually heard of, never from the
        // world's roster. Enumerating the organisation here would put names in front of the player
        // that the viewpoint character has no way to know, and "who else is in this outfit" is
        // exactly the kind of thing a boss might be wrong about.
        var neverAsked = KnownPeople(world, who)
            .Where(id => !who.Cognition.HasAccountFrom(id))
            .ToList();

        if (thin.Count > 0 || neverAsked.Count > 0)
        {
            sb.AppendLine("WHAT HE CANNOT SETTLE");
            sb.AppendLine();
            foreach (var r in thin)
                sb.AppendLine($"  · whether {Describe(r.Claim, Name)} — {Qualify(r, who.Cognition.IsContested(r.Claim))}");
            foreach (var id in neverAsked)
                sb.AppendLine($"  · {Name(id)} has not given him an account");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// The people this character has any reason to know exist, in id order.
    ///
    /// Assembled strictly from his own head: whoever appears in a claim he holds, whoever has
    /// given him an account, whoever he has a relationship with, and whoever he holds a grievance
    /// against. The world is consulted only to ask whether an id names a person at all — "the
    /// books" and a grocery are not people to go and ask — which resolves references he already
    /// has rather than introducing new ones.
    ///
    /// Public so tests can assert that no name outside this set ever reaches the page.
    /// </summary>
    public static IReadOnlyList<string> KnownPeople(World world, Character who)
    {
        var known = new SortedSet<string>(StringComparer.Ordinal);

        void Consider(string? id)
        {
            if (string.IsNullOrEmpty(id) || id == who.Id) return;
            if (world.Find(id) is null) return;
            known.Add(id);
        }

        foreach (var r in who.Cognition.Records)
        {
            Consider(r.Claim.Subject);
            Consider(r.Claim.Object);
            Consider(r.SourceId);
        }

        foreach (var t in who.Cognition.Testimony) Consider(t.SenderId);
        foreach (var id in who.Social.Others) Consider(id);
        foreach (var g in who.Social.Grievances) Consider(g.AgainstId);

        return known.ToList();
    }

    /// <summary>
    /// The claim as a sentence. In fiction, never as a predicate.
    ///
    /// Public so the no-leak test can compute the exact wording a given claim would produce and
    /// assert its absence from the view. A test that hardcoded the prose would pass while the
    /// renderer drifted.
    /// </summary>
    public static string Describe(Claim c, Func<string, string> name) => c.Kind switch
    {
        ClaimKind.BusinessRefusesTribute => $"{name(c.Subject)} is holding back what it owes",
        ClaimKind.PersonUsedViolence => $"{name(c.Subject)} put hands on {name(c.Object)}",
        ClaimKind.PoliceInvestigating => $"the police are looking at {name(c.Subject)}",
        ClaimKind.PersonHoldsGrievance => $"{name(c.Subject)} carries something against {name(c.Object)}",
        ClaimKind.TributeCollected => $"{name(c.Subject)} has paid",
        ClaimKind.WitnessSawIncident => $"somebody on the street saw {name(c.Object)} at {name(c.Subject)}",
        ClaimKind.PolicyIssued => $"the rule \"{c.Object}\" stands",
        ClaimKind.PersonBreachedPolicy => $"{name(c.Subject)} went outside \"{c.Object}\"",
        ClaimKind.TargetIsVulnerable => $"{name(c.Subject)} would not stand up to pressure",
        _ => c.ToString(),
    };

    /// <summary>
    /// How far he would go on this person's word, in words.
    ///
    /// Qualitative for the same reason confidence is: the number is hidden state, and a percentage
    /// would let the player read the model instead of the man. Note that nothing here explains
    /// *why* he stands where he does. A relationship that cooled because an account did not match
    /// reads identically to one that was never warm, which is correct — the difference is a matter
    /// of history the player has to reconstruct from the accounts above, not a label the interface
    /// hands over.
    ///
    /// Public so the leak test can assert the exact wording never asserts deception.
    /// </summary>
    public static string Standing(double trust) => trust switch
    {
        >= 0.60 => "he would take his word",
        >= 0.35 => "he takes him as he finds him",
        >= 0.15 => "he has his reservations about him",
        > 0 => "he would not take his word for much",
        _ => "he puts no weight on anything the man says",
    };

    /// <summary>
    /// Qualitative confidence only. INFORMATION_AND_LEGIBILITY.md lists the vocabulary; the
    /// numeric confidence behind it is hidden state and stays hidden.
    /// </summary>
    private static string Qualify(InformationRecord r, bool contested)
        => contested ? "contradicted" : r.ConfidenceLabel;

    /// <summary>
    /// Where it came from, as the player is entitled to see it. "Vincent says an associate heard"
    /// is meaningfully different from having watched it happen, and the difference has to survive
    /// into the sentence.
    ///
    /// This used to be deliberately vague. A single unmediated category covered a man who watched
    /// something, a man who did it, and a man who had it from the person who did, so the wording
    /// could not say "he saw it" or "he was there" without asserting a fact the record did not
    /// contain. Provenance now distinguishes them, and each sentence below says only what its
    /// category actually establishes.
    ///
    /// The rule this enforces: none of these may claim presence, sight, or participation that the
    /// acquisition category does not carry. Discovery in particular says "came across", never
    /// "saw" — finding a wrecked shopfront the next morning is not witnessing a beating.
    /// </summary>
    private static string Attribute(InformationRecord r, Func<string, string> name)
        => r.SourceKind switch
        {
            // Deliberately not "he saw it". Vincent holds that he went outside his boss's rule
            // because he decided to, which is not a thing anybody watches happen.
            SourceKind.Participant => "he had a hand in it himself",
            SourceKind.Witness => "he saw it himself",
            SourceKind.Discovery => "he came across it",
            SourceKind.FirstHandTestimony => $"{name(r.SourceId)} was in it and told him so",
            SourceKind.Report => $"{name(r.SourceId)} told him",
            SourceKind.Inference => "he worked it out himself",
            _ => $"talk, no better sourced than {name(r.SourceId)}",
        };
}
