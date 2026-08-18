namespace CrimeSim.Trace;

using System.Text;
using CrimeSim.Domain;
using CrimeSim.Session;
using CrimeSim.Sim;

/// <summary>
/// What one character could actually tell you, rendered as the player sees it on a console.
///
/// THE RULE THIS FILE EXISTS TO ENFORCE: everything below is derived from the viewpoint
/// character's own <see cref="Cognition"/> — his settled beliefs and the accounts he has been
/// given — and from nothing else. It must never read <see cref="World.TruthLog"/>,
/// <see cref="World.Decisions"/>, <see cref="World.Reports"/>, or any utility score.
///
/// <b>Since milestone 009 it does not enforce that rule itself; it consumes something that does.</b>
/// <see cref="PlayerView.Build"/> in the simulation library is the single place that decides what a
/// viewpoint character may be shown, and this file turns its <see cref="PlayerSnapshot"/> into
/// sections, padding and box drawing. The Godot interface renders the same snapshot a different way.
/// Two surfaces deriving the source limit independently is the failure shape `REVIEW_LEDGER.md`
/// records as a distinction drawn in one place and dropped on the way to the next — so there is one
/// derivation and two layouts, and this file owns only the layout.
///
/// This remains the deliberate opposite of TraceWriter.cs, which shows the true trigger, the real
/// score components and the chosen strategy. INFORMATION_AND_LEGIBILITY.md requires development
/// traces stay separate from player-facing information, and separate files is the cheapest way to
/// make that separation visible rather than merely intended.
///
/// Two smaller rules follow from the same document, and both now live with the snapshot rather than
/// here. Confidence appears only as qualitative language — never a number, because an exact
/// probability is hidden state wearing a percentage sign. And an account is attributed to the source
/// the player can actually see, so that "Vincent says" and "he saw it himself" never collapse into
/// the same sentence.
/// </summary>
public static class IntelligenceWriter
{
    public static string Render(World world, string viewpointId)
        => Render(PlayerView.Build(world, viewpointId, world.Now));

    /// <summary>
    /// The layout, over an already source-limited snapshot.
    ///
    /// Nothing in here consults a world, so there is no route by which this method could widen what
    /// the snapshot decided to include.
    /// </summary>
    public static string Render(PlayerSnapshot view)
    {
        var sb = new StringBuilder();

        sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
        sb.AppendLine($" WHAT {view.ViewpointName.ToUpperInvariant()} KNOWS — {view.ViewpointRole}");
        sb.AppendLine($" as of {view.Date:d MMMM yyyy}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
        sb.AppendLine();
        var self = view.ViewpointPronouns;
        sb.AppendLine($" Everything here is something {self.Subject} saw or was told. Where it is wrong, it is");
        sb.AppendLine(" wrong because somebody was wrong or somebody lied.");
        sb.AppendLine();

        // ---------------------------------------------------------------- what he has
        sb.AppendLine($"WHAT {self.Subject.ToUpperInvariant()} {self.Verb("HAS", "HAVE")}");
        sb.AppendLine();
        if (view.Known.Count == 0)
        {
            sb.AppendLine($"  Nothing. Nobody has told {self.Object} anything and {self.Subject} " +
                          $"{self.Verb("has", "have")} seen nothing {self.Reflexive}.");
        }
        else
        {
            foreach (var b in view.Known)
            {
                string mark = b.Contested ? " ⚠" : "";
                sb.AppendLine($"  {b.AcquiredAt:d MMM}  {b.Statement}{mark}");
                sb.AppendLine($"           {b.Confidence}, {b.Attribution}");
            }
        }
        sb.AppendLine();

        // ---------------------------------------------------------------- disagreement
        if (view.Disagreements.Count > 0)
        {
            sb.AppendLine("ACCOUNTS THAT DO NOT AGREE");
            sb.AppendLine();
            foreach (var d in view.Disagreements)
            {
                sb.AppendLine($"  On whether {d.Statement}:");

                if (d.OwnBasis is { } basis)
                    sb.AppendLine($"     {basis,-20} — {(d.OwnPositionHeld ? "it happened" : "it did not")}");

                foreach (var a in d.Accounts)
                    sb.AppendLine($"     {a.SourceName,-20} — {(a.Affirms ? "it happened" : "it did not")} ({a.At:d MMM})");

                sb.AppendLine();
            }
        }

        // ---------------------------------------------------------------- how he takes them
        //
        // His own attitude, and only ever his own — what *he* makes of the people he deals with, and
        // nothing about what they make of him, which is their private state and not his to know. The
        // three rules that keep this section honest (never a number, never an accusation, never
        // anybody he does not know) are enforced where the snapshot is built.
        if (view.Attitudes.Count > 0)
        {
            sb.AppendLine("HOW HE TAKES THEM");
            sb.AppendLine();
            foreach (var a in view.Attitudes)
            {
                sb.AppendLine($"  {a.PersonName}");
                sb.AppendLine($"     {a.Standing}");
                if (a.Wariness is { } wariness)
                    sb.AppendLine($"     {wariness}");
                // Quoted rather than folded into the sentence. Grievance descriptions are written
                // from the holder's own side and mostly in the first person — "moved against me",
                // "the harbour was handed to me only after it stopped earning" — so embedding one
                // after "he holds it against him that" produces broken English. As his own words
                // about it, they read correctly and stay his.
                foreach (var g in a.Grievances)
                    sb.AppendLine($"     what {self.Subject} {self.Verb("holds", "hold")} against " +
                                  $"{a.PersonPronouns.Object}: \"{g}\"");
            }
            sb.AppendLine();
        }

        // ---------------------------------------------------------------- open questions
        if (view.Unsettled.Count > 0 || view.Silent.Count > 0)
        {
            sb.AppendLine($"WHAT {self.Subject.ToUpperInvariant()} CANNOT SETTLE");
            sb.AppendLine();
            foreach (var b in view.Unsettled)
                sb.AppendLine($"  · whether {b.Statement} — {b.Confidence}");
            foreach (var p in view.Silent)
                sb.AppendLine($"  · {p.Name} has not given {self.Object} an account");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// The people this character has any reason to know exist, in id order.
    ///
    /// Kept as this type's public surface because the accepted no-leak tests pin it here; the rule
    /// itself lives in <see cref="PlayerView.KnownPeople"/> alongside everything else that decides
    /// what a viewpoint character may see.
    /// </summary>
    public static IReadOnlyList<string> KnownPeople(World world, Character who)
        => PlayerView.KnownPeople(world, who);

    /// <summary>
    /// The claim as a sentence. In fiction, never as a predicate. See
    /// <see cref="PlayerNarration.Describe"/>, which owns the wording.
    /// </summary>
    public static string Describe(Claim c, Func<string, string> name)
        => PlayerNarration.Describe(c, name);

    /// <summary>
    /// How far he would go on this person's word, in words. See
    /// <see cref="PlayerNarration.Standing"/>, which owns the wording.
    /// </summary>
    public static string Standing(double trust, Pronouns self, Pronouns other)
        => PlayerNarration.Standing(trust, self, other);
}
