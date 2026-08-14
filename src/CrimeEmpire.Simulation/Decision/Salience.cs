namespace CrimeSim.Decision;

using CrimeSim.Domain;

/// <summary>
/// PSYCHOLOGY-READING FILE 1 OF 2 (the other is Utility.cs).
///
/// Traits act here, before scoring: they decide which options occur to a character at all, and how
/// second-hand information is weighted. This is what stops every personality from considering the
/// same menu and merely assigning it slightly different numbers.
/// </summary>
public sealed class SalienceProfile
{
    private readonly Dictionary<ActionKind, double> _byAction = new();
    private readonly Dictionary<CoercionMethod, double> _byMethod = new();
    private readonly Dictionary<StrategyKind, double> _byStrategy = new();
    private readonly Dictionary<ReportCandor, double> _byCandor = new();
    private readonly List<string> _notes = new();

    /// <summary>Below this, an option simply does not occur to the character.</summary>
    public const double Threshold = 0.45;

    /// <summary>Maximum options carried into filtering and scoring.</summary>
    public const int MaxCandidates = 6;

    public IReadOnlyList<string> Notes => _notes;

    internal void Scale(ActionKind k, double factor, string note)
    {
        _byAction[k] = Get(_byAction, k) * factor;
        Note(note);
    }

    internal void Scale(CoercionMethod m, double factor, string note)
    {
        _byMethod[m] = Get(_byMethod, m) * factor;
        Note(note);
    }

    internal void Scale(StrategyKind s, double factor, string note)
    {
        _byStrategy[s] = Get(_byStrategy, s) * factor;
        Note(note);
    }

    internal void Scale(ReportCandor c, double factor, string note)
    {
        _byCandor[c] = Get(_byCandor, c) * factor;
        Note(note);
    }

    private void Note(string note)
    {
        if (note.Length > 0 && !_notes.Contains(note)) _notes.Add(note);
    }

    private static double Get<T>(Dictionary<T, double> d, T k) where T : notnull
        => d.TryGetValue(k, out var v) ? v : 1.0;

    public double For(Candidate c)
    {
        double s = Get(_byAction, c.Kind);
        if (c.Method is { } m) s *= Get(_byMethod, m);
        if (c.Strategy is { } st) s *= Get(_byStrategy, st);
        if (c.Candor is { } cd) s *= Get(_byCandor, cd);
        return s;
    }
}

public static class Salience
{
    /// <summary>
    /// Builds the character's belief-limited view, applying the Suspicious discount to anything not
    /// personally observed. Confidence is a perception, so the discount belongs at read time rather
    /// than being baked in when the information was acquired.
    /// </summary>
    public static PerceivedSituation Perceive(Character c, DateTime now)
    {
        double suspicion = c.Psychology[Trait.Suspicious];
        var records = c.Cognition.Records.Select(r =>
        {
            if (r.SourceKind.ExemptFromSuspicion() || suspicion <= 0) return r;
            double discount = r.SourceKind switch
            {
                // He found it himself, so there is no source to distrust — but a wrecked shopfront
                // is a reading, and a suspicious man second-guesses his own readings. Small, and
                // below every discount that applies to somebody else's word. A tuning guess.
                SourceKind.Discovery => 0.10 * suspicion,
                // Closer to the event than a filed report, and correspondingly harder to wave away:
                // the man telling him was in it. Still discounted, because it is still an account.
                // The 0.15 is a tuning guess sitting between unmediated and Report, not a derived
                // figure.
                SourceKind.FirstHandTestimony => 0.15 * suspicion,
                SourceKind.Report => 0.25 * suspicion,
                SourceKind.Rumor => 0.45 * suspicion,
                SourceKind.Inference => 0.30 * suspicion,
                _ => 0.0,
            };
            return r with { Confidence = Math.Clamp(r.Confidence * (1 - discount), 0, 1) };
        });
        return new PerceivedSituation(c.Id, now, records, c.Cognition.Testimony);
    }

    /// <summary>
    /// Trait- and circumstance-driven salience. Each modifier below corresponds to a documented
    /// behavioural purpose on the Trait enum; adding one here without a purpose there is the start
    /// of an unbalanceable trait list.
    /// </summary>
    public static SalienceProfile Build(Character c, PerceivedSituation perceived)
    {
        var p = new SalienceProfile();

        double aggressive = c.Psychology[Trait.Aggressive];
        double cautious = c.Psychology[Trait.Cautious];
        double proud = c.Psychology[Trait.Proud];
        double suspicious = c.Psychology[Trait.Suspicious];

        if (aggressive > 0)
        {
            p.Scale(CoercionMethod.Force, 1 + 0.90 * aggressive, $"{c.Name} readily thinks of force");
            p.Scale(CoercionMethod.Threaten, 1 + 0.50 * aggressive, "");
            p.Scale(CoercionMethod.Persuade, 1 - 0.30 * aggressive, "");
            p.Scale(ActionKind.PostponeStrategy, 1 - 0.60 * aggressive, "waiting does not readily occur to him");
            p.Scale(ActionKind.AbandonStrategy, 1 - 0.50 * aggressive, "");
        }

        if (cautious > 0)
        {
            p.Scale(StrategyKind.ConcealIncident, 1 + 0.80 * cautious, $"{c.Name} thinks in terms of covering tracks");
            p.Scale(ActionKind.DelegateStrategy, 1 + 0.60 * cautious, "delegation comes to mind");
            p.Scale(ActionKind.PostponeStrategy, 1 + 0.50 * cautious, "");
            p.Scale(ActionKind.SeekApproval, 1 + 0.45 * cautious, "");
            p.Scale(CoercionMethod.Force, 1 - 0.70 * cautious, "");
            p.Scale(CoercionMethod.Persuade, 1 + 0.30 * cautious, "");

            // Leaving something out is a way of covering tracks and reads as the same trait.
            // Flatly denying it is not — it is the version that falls apart if anyone else talks,
            // and a careful man is precisely the one who notices that.
            p.Scale(ReportCandor.Partial, 1 + 0.55 * cautious, $"{c.Name} thinks in terms of what need not be said");
            p.Scale(ReportCandor.False, 1 - 0.40 * cautious, "");
        }

        if (proud > 0)
        {
            p.Scale(ActionKind.SeekApproval, 1 - 0.60 * proud, $"asking permission does not come naturally to {c.Name}");
            p.Scale(ActionKind.AbandonStrategy, 1 - 0.55 * proud, "");
            p.Scale(ActionKind.Concede, 1 - 0.65 * proud, "");
            p.Scale(ActionKind.ContinueStrategy, 1 + 0.20 * proud, "");

            // Deference to authority falls — and volunteering your own misconduct to your superior
            // is about as deferential as it gets.
            p.Scale(ReportCandor.Candid, 1 - 0.45 * proud, "");
        }

        if (suspicious > 0)
        {
            p.Scale(ActionKind.Retaliate, 1 + 0.45 * suspicious, "");
            p.Scale(ActionKind.ReportToSuperior, 1 - 0.30 * suspicious, "");

            // Someone who discounts second-hand claims is the same someone to whom asking a second
            // person naturally occurs.
            p.Scale(ActionKind.SeekCorroboration, 1 + 0.60 * suspicious,
                $"{c.Name} does not take one man's word for it");
        }

        // Circumstance, not personality: believed police interest makes concealment and caution
        // occur to anyone, regardless of traits.
        double heat = perceived.PerceivedPoliceInterest(c.Id);
        if (heat > 0.2)
        {
            p.Scale(StrategyKind.ConcealIncident, 1 + 0.9 * heat, $"{c.Name} believes police are interested");
            p.Scale(CoercionMethod.Force, 1 - 0.5 * heat, "");
        }

        return p;
    }
}
