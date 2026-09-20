using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// A set of labels along an axis: the result of a table projecting its own header, handed to a row
  /// bind so a declaration written once can find this file's columns, and — since it is public and
  /// implements <see cref="ILabelSource"/> — what a scope-introducer such as
  /// <see cref="ProjectionBuilders{TSpace}.WithColumnLabels{T}(LabelMap, IProjectionDefinition{TSpace, T})"/>
  /// pushes so a decoupled record reads by name.
  /// <para>
  /// It is per-file data rather than geometry, which is why it arrives as an argument and not as
  /// something a space offers.
  /// </para>
  /// <para>
  /// <b>Two matching rules, kept apart deliberately.</b> The bind-rung members
  /// (<see cref="this[string]"/>, <see cref="Has(string)"/>) match by <see cref="CaptionComparer"/> — case
  /// and whitespace ignored — and cite the header cell behind an ambiguous or missing caption. The <see cref="ILabelSource"/> face the primitive
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
    /// parse, read by both <see cref="ProjectionBuilders{TSpace}.ColumnLabels(int)"/> and a built-in <c>Table</c>, so
    /// the two mint byte-identical labels, ordinals and citations. Both the label source and the
    /// header-citation face are the same <see cref="HeaderLabels{TSpace}"/>.
    /// </summary>
    internal static LabelMap FromHeader<TSpace>(CellStrip<TSpace> header)
      where TSpace : class, ISpace
    {
      var labels = new HeaderLabels<TSpace>(header);

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

    /// <summary>
    /// Each column's path through the header: its bands, outermost first, then its own label. One
    /// step long for a column under no band, and empty for a column with no label at all.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Paths => _source.Paths;

    IReadOnlyList<IReadOnlyList<int>> ILabelSource.Starts => _source.Starts;

    /// <summary>
    /// How many header rows these labels were read from: one for captions alone, more where rows of
    /// bands sit over them. A path may be no longer than this.
    /// </summary>
    public int Depth => _source.Depth;

    /// <summary>
    /// The column a PATH through the header names — <c>labels["From", "Id"]</c> — where each step
    /// is a name, the nth of a name <c>("Id", 1)</c>, or a position <c>1</c>, all counted from
    /// zero. See <see cref="LabelStep"/>.
    /// <para>
    /// A one-step path is a name: the column whose whole path it is, before a caption somewhere
    /// under a band. A longer path is exact. A name that several columns carry, a step nothing
    /// answers to, a position outside what the path has reached and a path that stops at a band are
    /// all loud failures that say what IS there.
    /// </para>
    /// </summary>
    public int this[params LabelStep[] path]
    {
      get
      {
        var answer = LabelPaths.Resolve(_source, path, CaptionComparer.Default);

        if (answer.Problem is string problem)
          throw Header.Failure(problem);

        return answer.IsBand
          ? throw Header.Failure($"{Written(path)} is a band over {answer.Columns.Count} columns, not a column; say which, by name or by position")
          : answer.Columns[0];
      }
    }

    /// <summary>
    /// Whether <paramref name="path"/> names a column — for the row that reads a column some
    /// exports have and others do not. A path that is ambiguous still throws, as the indexer does:
    /// answering yes or no would both be lies.
    /// </summary>
    public bool Has(params LabelStep[] path)
    {
      var answer = LabelPaths.Resolve(_source, path, CaptionComparer.Default);

      if (answer.Problem is string problem && problem.StartsWith("there is no ", StringComparison.Ordinal))
        return false;

      return answer.Problem is null ? !answer.IsBand : throw Header.Failure(answer.Problem);
    }

    /// <summary>
    /// A band as a map of its own: <c>labels.Under("From")</c> is the labels beneath From, by the
    /// names they have there, their columns where they have always been.
    /// <para>
    /// Internal on purpose. It is the label half of record blocks and nested types, which are
    /// speculative: tested and ready, promised to nobody until a shape for them has been chosen.
    /// </para>
    /// </summary>
    internal LabelMap Under(params LabelStep[] band)
    {
      var answer = LabelPaths.Resolve(_source, band, CaptionComparer.Default);

      if (answer.Problem is string problem)
        throw Header.Failure(problem);

      if (!answer.IsBand)
        throw Header.Failure($"{Written(band)} is a column, not a band, so there is nothing under it");

      return new LabelMap(new BandLabels(_source, answer.Columns, answer.Depth), _header);
    }

    /// <summary>
    /// The columns whose WHOLE path, its steps run together, is <paramref name="name"/> under
    /// <see cref="CaptionComparer"/> — how a flat member called <c>FromId</c> finds the column at
    /// <c>From, Id</c>. It is a way of MATCHING a member to a path, never a way of naming a column:
    /// nothing is parsed back out of the name, and two paths that run together alike come back
    /// together, for the binder to refuse. Only columns under a band are candidates; a column under
    /// none is found by its caption.
    /// </summary>
    internal IReadOnlyList<int> BoundByPath(string name)
    {
      var matches = new List<int>();
      var paths = _source.Paths;

      for (var column = 0; column < paths.Count; column++)
        if (paths[column].Count > 1 && CaptionComparer.Default.Equals(string.Concat(paths[column]), name))
          matches.Add(column);

      return matches;
    }

    /// <summary>
    /// The column <paramref name="path"/> names, for a binder resolving it on a member's behalf: the
    /// same answer as the indexer, with <paramref name="subject"/> said first in any failure, so a
    /// reader is told which member of their type the header disagreed with.
    /// </summary>
    internal int Column(IReadOnlyList<LabelStep> path, string subject)
    {
      var answer = LabelPaths.Resolve(_source, path, CaptionComparer.Default);

      if (answer.Problem is string problem)
        throw Header.Failure($"{subject} is bound to {Written(path)}: {problem}");

      return answer.IsBand
        ? throw Header.Failure($"{subject} is bound to {Written(path)}, which is a band over {answer.Columns.Count} columns, not a column; say which, by name or by position")
        : answer.Columns[0];
    }

    private static string Written(IReadOnlyList<LabelStep> path) => "[" + string.Join(", ", path.Select(step => step.ToString())) + "]";

    IReadOnlyList<string> ILabelSource.Labels => Labels;

    // The primitive face: the source's own content-rule lookup, the rule TableRow<TSpace> resolves
    // by. Kept off the CaptionComparer the bind rung uses, deliberately.
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

    /// <summary>
    /// The columns <paramref name="caption"/> binds to under <see cref="CaptionComparer"/> — the
    /// same lookup the indexer makes, handed over rather than answered, so a reflected binder can
    /// name the MEMBER in a failure where the indexer can only name the caption.
    /// </summary>
    internal IReadOnlyList<int> Bound(string caption) => Matches(caption);

    /// <summary>Where a column's caption cell is, for a failure of its own that cites the header.</summary>
    internal ProjectionLocation AddressOf(int column) => Header.AddressOf(column);

    // The bind rung cites header cells; a map of literals has none, so it cannot answer here. In
    // practice this is unreachable — the bind rung is only ever handed a table's own map — but a
    // clear error beats a NullReferenceException if a literal map is ever bound by mistake.
    /// <summary>
    /// A failure blaming the table this map was read from — its origin, its extent. How a reading
    /// built on these captions reports something the whole table is wrong about.
    /// </summary>
    internal ProjectionException Failure(string problem) => Header.Failure(problem);

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

      // A column under no band first, then a label anywhere: the rule a row read follows, under the
      // comparer a binding uses.
      var paths = _source.Paths;

      for (var column = 0; column < Labels.Count; column++)
        if (paths[column].Count == 1 && CaptionComparer.Default.Equals(Labels[column], caption))
          matches.Add(column);

      if (matches.Count > 0)
        return matches;

      for (var column = 0; column < Labels.Count; column++)
        if (paths[column].Count > 0 && CaptionComparer.Default.Equals(Labels[column], caption))
          matches.Add(column);

      return matches;
    }

    /// <summary>
    /// Says <paramref name="problem"/> about the table as a diagnostic rather than a failure. A map
    /// of literals has no header to say it about, and says nothing.
    /// </summary>
    internal void Note(DiagnosticSeverity severity, string problem) => _header?.Note(severity, problem);

    private ProjectionException Ambiguous(string caption, IReadOnlyList<int> matches)
      => Header.Failure(
        $"the caption '{caption}' matches the columns at "
        + $"{Header.AddressOf(matches[0]).A1} ('{Labels[matches[0]]}') and "
        + $"{Header.AddressOf(matches[1]).A1} ('{Labels[matches[1]]}'); "
        + "captions are matched ignoring case and whitespace");
  }

  /// <summary>
  /// The labels under one band, as labels in their own right: the same columns at the same
  /// ordinals, each path shorn of the steps that led to the band, and every column outside it a
  /// column with no label.
  /// </summary>
  internal sealed class BandLabels : ILabelSource
  {
    public BandLabels(ILabelSource whole, IReadOnlyList<int> columns, int depth)
    {
      var inside = new HashSet<int>(columns);

      Paths = whole.Paths.Select((path, column) => inside.Contains(column) ? (IReadOnlyList<string>)path.Skip(depth).ToList() : Array.Empty<string>()).ToList();
      Starts = whole.Starts.Select((starts, column) => inside.Contains(column) ? (IReadOnlyList<int>)starts.Skip(depth).ToList() : Array.Empty<int>()).ToList();
      Labels = Paths.Select(path => path.Count == 0 ? string.Empty : path[path.Count - 1]).ToList();
      Depth = Math.Max(1, whole.Depth - depth);
    }

    public IReadOnlyList<string> Labels { get; }

    public IReadOnlyList<IReadOnlyList<string>> Paths { get; }

    public IReadOnlyList<IReadOnlyList<int>> Starts { get; }

    public int Depth { get; }

    public IReadOnlyList<int> IndicesOf(string label)
    {
      var answer = LabelPaths.Resolve(this, new LabelStep[] { label }, Unrect.Strategies.CellMatching.TextComparer);

      return answer.Problem is null && !answer.IsBand ? answer.Columns : Array.Empty<int>();
    }
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

    public IReadOnlyList<IReadOnlyList<string>> Paths
      => _entries.Select(entry => (IReadOnlyList<string>)new[] { entry.Label }).ToList();

    public IReadOnlyList<IReadOnlyList<int>> Starts
      => _entries.Select((entry, position) => (IReadOnlyList<int>)new[] { position }).ToList();

    public int Depth => 1;

    public IReadOnlyList<int> IndicesOf(string label)
      => _entries.Where(entry => CellMatching.TextComparer.Equals(entry.Label, label)).Select(entry => entry.Index).ToList();
  }

  /// <summary>
  /// A header strip parsed into its labels: the trimmed names in column order, the content-rule lookup
  /// the primitive path resolves through, and the header cells the bind rung cites. It is the single
  /// home of the header parse — both a table and <see cref="ProjectionBuilders{TSpace}.ColumnLabels(int)"/> read a
  /// header through here.
  /// </summary>
  /// <typeparam name="TSpace">The space the header strip reads through.</typeparam>
  internal sealed class HeaderLabels<TSpace> : ILabelSource, IHeaderCitations
    where TSpace : class, ISpace
  {
    private readonly CellStrip<TSpace> _header;

    // Built once on first lookup: the content-rule map keyed by CellMatching.TextComparer, the rule
    // matchers and Caption use, so a lookup here and a RowContaining elsewhere find a caption on the
    // same terms.
    private Dictionary<string, List<int>>? _columnsByName;

    /// <param name="header">
    /// The header band, one row tall or several. Its LAST row holds the captions and every row
    /// above it is a row of bands; what comes out is each column's PATH — the distinct regions it
    /// passes through from the top of the header to the bottom.
    /// </param>
    internal HeaderLabels(CellStrip<TSpace> header)
    {
      var rows = header.Space.Area.Height;

      // The captions are what failures cite: a column is where its caption is.
      _header = rows > 1 ? header.Line(rows - 1) : header;

      var words = new List<string>[rows];

      for (var row = 0; row < rows; row++)
        words[row] = Words(rows > 1 ? header.Line(row) : header);

      Paths = HeaderRegions.Fold(words, out var starts);
      Starts = starts;
      Depth = rows;
      Labels = Paths.Select(path => path.Count == 0 ? string.Empty : path[path.Count - 1]).ToList();
    }

    /// <summary>Each column's own label — the last step of its path — and the empty string for a column with none.</summary>
    public IReadOnlyList<string> Labels { get; }

    /// <summary>Each column's path: its bands, outermost first, then its caption. Empty for a column with no label.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Paths { get; }

    public IReadOnlyList<IReadOnlyList<int>> Starts { get; }

    public int Depth { get; }

    // A label is whatever a header cell SAYS. It is a label by position, not by kind: a row of
    // years or of period-end dates is a row of captions.
    private static List<string> Words(CellStrip<TSpace> row)
      => row.Select(cell => cell.IsBlank ? string.Empty : (cell.AsText() ?? string.Empty).Trim()).ToList();

    public IReadOnlyList<int> IndicesOf(string label)
    {
      if (label is null)
        throw new ArgumentNullException(nameof(label));

      return (_columnsByName ??= BuildColumnsByName()).TryGetValue(label, out var indices)
        ? indices
        : Array.Empty<int>();
    }

    public ProjectionException Failure(string problem) => _header.Failure(problem);

    public void Note(DiagnosticSeverity severity, string problem) => _header.Note(severity, problem);

    public ProjectionLocation AddressOf(int column) => _header.AddressOf(column);

    private Dictionary<string, List<int>> BuildColumnsByName()
    {
      // A name is a one-step path, and it means the column whose WHOLE path it is — one under no
      // band — before it means a label that happens to be unique somewhere under one. Where it is
      // neither, it is every column that carries it, which is the ambiguity a by-name read refuses.
      var exact = new Dictionary<string, List<int>>(CellMatching.TextComparer);
      var anywhere = new Dictionary<string, List<int>>(CellMatching.TextComparer);

      void Add(Dictionary<string, List<int>> columns, string name, int index)
      {
        if (!columns.TryGetValue(name, out var indices))
          columns[name] = indices = new List<int>();

        indices.Add(index);
      }

      for (var index = 0; index < Paths.Count; index++)
      {
        if (Paths[index].Count == 0)
          continue;

        Add(anywhere, Labels[index], index);

        if (Paths[index].Count == 1)
          Add(exact, Labels[index], index);
      }

      foreach (var entry in exact)
        anywhere[entry.Key] = entry.Value;

      return anywhere;
    }
  }

  /// <summary>
  /// The header cells behind a table-minted <see cref="LabelMap"/>: how the bind rung cites an
  /// ambiguous or missing caption. <see cref="HeaderLabels{TSpace}"/> is the only implementation; a map of
  /// literals has none.
  /// </summary>
  internal interface IHeaderCitations
  {
    /// <summary>A failure blaming the table itself — its origin, its extent.</summary>
    ProjectionException Failure(string problem);

    /// <summary>The same sentence as a diagnostic: said about the table, and not thrown.</summary>
    void Note(DiagnosticSeverity severity, string problem);

    /// <summary>The address of the header cell at <paramref name="column"/>.</summary>
    ProjectionLocation AddressOf(int column);
  }
}
