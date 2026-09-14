using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// A set of labels along an axis: the result of a table projecting its own header, handed to a row
  /// bind so a declaration written once can find this file's columns, and — since it is public and
  /// implements <see cref="ILabelSource"/> — what a scope-introducer such as
  /// <see cref="Projection.WithColumnLabels{T}(LabelMap, IProjection{T})"/> pushes so a decoupled
  /// record reads by name.
  /// <para>
  /// It is per-file data rather than geometry, which is why it arrives as an argument and not as
  /// something a space offers.
  /// </para>
  /// <para>
  /// <b>Two matching rules, kept apart deliberately.</b> The bind-rung members
  /// (<see cref="this[string]"/>, <see cref="Has"/>) match by <see cref="CaptionComparer"/> — case
  /// and whitespace ignored — the same rule that binds <c>Table&lt;T&gt;()</c>, and cite the header
  /// cell behind an ambiguous or missing caption. The <see cref="ILabelSource"/> face the primitive
  /// path resolves through matches by the source's own content rule (<c>CellMatching.TextComparer</c>),
  /// which is the rule <c>TableRow</c> and the matchers use. A table's own header answers both; a
  /// <see cref="Of"/> map of literals answers only the primitive face.
  /// </para>
  /// </summary>
  public sealed class LabelMap : ILabelSource
  {
    // Always present: the labels and their ordinals, in the frame they were read. Every LabelMap has
    // one; only a table-minted map also has header cells to cite, which is the other field.
    private readonly ILabelSource _source;

    // The bind rung's header addresses and Failure, for the ambiguous/missing citations — null for a
    // map of literals, which has no header cells to point at.
    private readonly IHeaderCitations? _header;

    private LabelMap(ILabelSource source, IHeaderCitations? header)
    {
      _source = source;
      _header = header;
    }

    /// <summary>
    /// Parses <paramref name="header"/> into the labels a table binds by — the one home of the header
    /// parse, read by both <see cref="Projection.ColumnLabels(int)"/> and a built-in <c>Table</c>, so
    /// the two mint byte-identical labels, ordinals and citations. Both the label source and the
    /// header-citation face are the same <see cref="HeaderLabels"/>.
    /// </summary>
    internal static LabelMap FromHeader(CellStrip header, ProjectionContext context)
    {
      var labels = new HeaderLabels(header, context);

      return new LabelMap(labels, labels);
    }

    /// <summary>
    /// A label map of known columns, for the headerless-known-layout case: <c>LabelMap.Of(("EIN", 0),
    /// ("Name", 1))</c>. It answers the primitive <see cref="ILabelSource"/> face only — there are no
    /// header cells behind it, so the bind-rung members that cite one are not meaningful on it.
    /// </summary>
    /// <param name="labels">Each label and the ordinal it names, in the frame the labels are read.</param>
    public static LabelMap Of(params (string Label, int Index)[] labels)
      => new LabelMap(new LiteralLabels(labels ?? throw new ArgumentNullException(nameof(labels))), null);

    /// <summary>
    /// Each column's label, in column order and trimmed, with the empty string where a column carries
    /// none — so <c>Labels[i]</c> is what column <c>i</c> is called.
    /// </summary>
    public IReadOnlyList<string> Labels => _source.Labels;

    IReadOnlyList<string> ILabelSource.Labels => Labels;

    // The primitive face: the source's own content-rule lookup, byte-identical with the pre-step-1
    // TableRow resolution. Kept off the CaptionComparer the bind rung uses, deliberately.
    IReadOnlyList<int> ILabelSource.IndicesOf(string label) => _source.IndicesOf(label);

    /// <summary>
    /// The column captioned <paramref name="caption"/>, as an index into the row a record is handed
    /// — <c>Decimal().Right(captions["Amount"])</c>.
    /// <para>
    /// Strict in both directions: a caption no column carries is a loud failure listing the captions
    /// the file does carry, and one that two columns carry is a loud failure naming both. Neither can
    /// be answered with a column, and answering with the first would be a guess.
    /// </para>
    /// </summary>
    public int this[string caption]
    {
      get
      {
        var matches = Matches(caption);

        if (matches.Count == 1)
          return matches[0];

        throw matches.Count == 0
          ? Header.Failure(
            $"no column is captioned '{caption}'; the table's captions are "
            + string.Join(", ", Labels.Select(c => $"'{c}'")))
          : Ambiguous(caption, matches);
      }
    }

    /// <summary>
    /// Whether the table carries a column captioned <paramref name="caption"/> — for the row that
    /// reads a column some exports have and others do not.
    /// <para>
    /// An ambiguous caption still throws, exactly as the indexer does: two columns of that name is a
    /// table nobody can read by name, and answering "yes" or "no" would both be lies. A missing
    /// column is the only absence this reports.
    /// </para>
    /// </summary>
    public bool Has(string caption)
    {
      var matches = Matches(caption);

      return matches.Count switch
      {
        0 => false,
        1 => true,
        _ => throw Ambiguous(caption, matches),
      };
    }

    // The bind rung cites header cells; a map of literals has none, so it cannot answer here. In
    // practice this is unreachable — the bind rung is only ever handed a table's own map — but a
    // clear error beats a NullReferenceException if a literal map is ever bound by mistake.
    private IHeaderCitations Header => _header ?? throw new InvalidOperationException(
      "This LabelMap was created from literal labels and has no header cells to cite; the "
      + "caption-comparer members (the indexer, Has) are only meaningful for a table's own header.");

    private List<int> Matches(string caption)
    {
      if (caption is null)
        throw new ArgumentNullException(nameof(caption));

      if (caption.Trim().Length == 0)
        throw new ArgumentException("A caption cannot be empty or whitespace.", nameof(caption));

      var matches = new List<int>();

      for (var column = 0; column < Labels.Count; column++)
        if (CaptionComparer.Default.Equals(Labels[column], caption))
          matches.Add(column);

      return matches;
    }

    private ProjectionException Ambiguous(string caption, IReadOnlyList<int> matches)
      => Header.Failure(
        $"the caption '{caption}' matches the columns at "
        + $"{Header.AddressOf(matches[0]).A1} ('{Labels[matches[0]]}') and "
        + $"{Header.AddressOf(matches[1]).A1} ('{Labels[matches[1]]}'); "
        + "captions are matched ignoring case and whitespace");
  }

  /// <summary>
  /// A <see cref="LabelMap"/> of known columns, matched by the content rule the primitive path uses.
  /// It has no header cells, so it answers only <see cref="ILabelSource"/>.
  /// </summary>
  internal sealed class LiteralLabels : ILabelSource
  {
    private readonly (string Label, int Index)[] _entries;

    internal LiteralLabels((string Label, int Index)[] entries) => _entries = entries;

    public IReadOnlyList<string> Labels => _entries.Select(entry => entry.Label).ToList();

    public IReadOnlyList<int> IndicesOf(string label)
      => _entries.Where(entry => CellMatching.TextComparer.Equals(entry.Label, label)).Select(entry => entry.Index).ToList();
  }

  /// <summary>
  /// A header strip parsed into its labels: the trimmed names in column order, the content-rule lookup
  /// the primitive path resolves through, and the header cells the bind rung cites. It is the single
  /// home of the header parse — both a table and <see cref="Projection.ColumnLabels(int)"/> read a
  /// header through here.
  /// </summary>
  internal sealed class HeaderLabels : ILabelSource, IHeaderCitations
  {
    private readonly CellStrip _header;
    private readonly ProjectionContext _context;

    // Built once on first lookup: the content-rule map keyed by CellMatching.TextComparer, the rule
    // matchers and Caption use, so a lookup here and a RowContaining elsewhere find a caption on the
    // same terms.
    private Dictionary<string, List<int>>? _columnsByName;

    internal HeaderLabels(CellStrip header, ProjectionContext context)
    {
      _header = header;
      _context = context;
      Labels = header.Select(cell => cell.TryGetString()?.Trim() ?? string.Empty).ToList();
    }

    public IReadOnlyList<string> Labels { get; }

    public IReadOnlyList<int> IndicesOf(string label)
    {
      if (label is null)
        throw new ArgumentNullException(nameof(label));

      return (_columnsByName ??= BuildColumnsByName()).TryGetValue(label, out var indices)
        ? indices
        : Array.Empty<int>();
    }

    public ProjectionException Failure(string problem) => _context.Failure(problem, _header.Space);

    public ProjectionLocation AddressOf(int column) => _header.AddressOf(column);

    private Dictionary<string, List<int>> BuildColumnsByName()
    {
      var columns = new Dictionary<string, List<int>>(CellMatching.TextComparer);

      for (var index = 0; index < Labels.Count; index++)
      {
        var name = Labels[index];

        if (name.Length == 0)
          continue;

        if (!columns.TryGetValue(name, out var indices))
          columns[name] = indices = new List<int>();

        indices.Add(index);
      }

      return columns;
    }
  }

  /// <summary>
  /// The header cells behind a table-minted <see cref="LabelMap"/>: how the bind rung cites an
  /// ambiguous or missing caption. <see cref="HeaderLabels"/> is the only implementation; a map of
  /// literals has none.
  /// </summary>
  internal interface IHeaderCitations
  {
    /// <summary>A failure blaming the table itself — its origin, its extent.</summary>
    ProjectionException Failure(string problem);

    /// <summary>The address of the header cell at <paramref name="column"/>.</summary>
    ProjectionLocation AddressOf(int column);
  }
}
