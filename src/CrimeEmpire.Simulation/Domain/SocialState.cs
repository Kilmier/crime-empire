namespace CrimeSim.Domain;

public sealed record Grievance(string AgainstId, string Description, double Severity, DateTime At);

/// <summary>
/// One character's directed social state.
///
/// This type stores relationships and answers questions about them. It does not change them —
/// every mutation lives in <see cref="Relations"/>, and <see cref="IRelationship"/>'s
/// dimensions have private setters, so that separation is enforced by the compiler rather than by
/// convention. See the header of Relations.cs for why.
/// </summary>
public sealed class SocialState
{
    private readonly Dictionary<string, IRelationship> _relationships = new();

    public string? OrganizationId { get; set; }

    /// <summary>
    /// The relationships this character actually has, in id order.
    ///
    /// Ordered rather than handed out as a dictionary, because this feeds the replay comparison and
    /// dictionary enumeration order is an implementation detail the determinism rules explicitly
    /// forbid depending on. Callers that want a stable sequence get one here rather than each
    /// remembering to sort.
    /// </summary>
    public IEnumerable<IRelationship> All
        => _relationships.Values.OrderBy(r => r.OtherId, StringComparer.Ordinal);

    /// <summary>Ids of everyone this character has a relationship with, in id order.</summary>
    public IEnumerable<string> Others
        => _relationships.Keys.OrderBy(id => id, StringComparer.Ordinal);

    /// <summary>
    /// How this character stands toward another. <b>Reading never creates.</b>
    ///
    /// This used to be a get-or-create, which was invisible while relationships were outside the
    /// replay comparison and became a determinism hazard the moment they entered it: scoring reads
    /// a great many relationships that do not exist — every delegation candidate, every concession,
    /// every policy breach consults one — and a creating read would have made the act of scoring a
    /// candidate change the snapshot. A character with no relationship to somebody reads as zero
    /// across the board, which is what "no relationship" means, and scores identically to the empty
    /// record the old behaviour would have inserted.
    /// </summary>
    public IRelationship Toward(string otherId)
        => _relationships.TryGetValue(otherId, out var r) ? r : Relations.Absent(otherId);

    /// <summary>The stored relationship, or null. Null means none has ever been established.</summary>
    internal IRelationship? Existing(string otherId)
        => _relationships.TryGetValue(otherId, out var r) ? r : null;

    /// <summary>
    /// The stored relationship, creating an empty one if there is none.
    ///
    /// Internal, and called only by <see cref="Relations"/> on a route that is about to change
    /// something. Note that a bypass of this method would be inert rather than dangerous: what it
    /// hands back is a relationship nobody outside <see cref="Relations"/> can move. The enforcement
    /// lives on the dimensions, not on the dictionary.
    /// </summary>
    internal IRelationship Ensure(string otherId)
    {
        if (!_relationships.TryGetValue(otherId, out var r))
        {
            r = Relations.Create(otherId);
            _relationships[otherId] = r;
        }
        return r;
    }

    /// <summary>
    /// Everything this character holds against anybody, in a deterministic order: by the person it
    /// is against, then in the order the grievances accumulated against that person.
    /// </summary>
    public IEnumerable<Grievance> Grievances
    {
        get
        {
            foreach (var rel in All)
                foreach (var g in rel.Grievances)
                    yield return g;
        }
    }

    /// <summary>What this character holds against one person, summed.</summary>
    public double GrievanceAgainst(string otherId) => Toward(otherId).GrievanceWeight;
}
