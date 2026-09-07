using System;
using System.Collections.Generic;
using System.Linq;

namespace Unrect.Projections
{
  /// <summary>
  /// Where a table's captions are: the result of the table projecting its own header, handed to a
  /// row bind so a declaration written once can find this file's columns.
  /// <para>
  /// It is per-file data rather than geometry, which is why it arrives as an argument and not as
  /// something a space offers. Only a table mints one, so a caption-dependent row cannot be written
  /// anywhere a header has not been read — the safety here is arity, not a check.
  /// </para>
  /// <para>
  /// Captions are matched by <see cref="CaptionComparer"/> — case and whitespace ignored — the same
  /// rule that binds <c>Table&lt;T&gt;()</c>, so a caption written the same way in both places
  /// cannot resolve to two different columns.
  /// </para>
  /// </summary>
  public sealed class CaptionMap
  {
    private readonly TableView _table;

    /// <summary>
    /// Minted by <see cref="Projection.Table{T}(int, Func{CaptionMap, IProjection{T}}, string)"/>
    /// and by nothing else. A test wanting a real one can take the table's own view through the
    /// bottom rung — <c>Table(1, table =&gt; table)</c> — and mint it from that.
    /// </summary>
    internal CaptionMap(TableView table) => _table = table;

    /// <summary>
    /// Each column's caption, in column order and trimmed, with the empty string where a column
    /// carries none — so <c>Captions[i]</c> is what column <c>i</c> is called.
    /// </summary>
    public IReadOnlyList<string> Captions => _table.ColumnNames;

    /// <summary>
    /// The column captioned <paramref name="caption"/>, as an index into the row a record is handed
    /// — <c>Decimal().Right(captions["Amount"])</c>.
    /// <para>
    /// Strict in both directions: a caption no column carries is a loud failure listing the
    /// captions the file does carry, and one that two columns carry is a loud failure naming both.
    /// Neither can be answered with a column, and answering with the first would be a guess.
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
          ? _table.Failure(
            $"no column is captioned '{caption}'; the table's captions are "
            + string.Join(", ", Captions.Select(c => $"'{c}'")))
          : Ambiguous(caption, matches);
      }
    }

    /// <summary>
    /// Whether the table carries a column captioned <paramref name="caption"/> — for the row that
    /// reads a column some exports have and others do not.
    /// <para>
    /// An ambiguous caption still throws, exactly as the indexer does: two columns of that name is
    /// a table nobody can read by name, and answering "yes" or "no" would both be lies. A missing
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

    private List<int> Matches(string caption)
    {
      if (caption is null)
        throw new ArgumentNullException(nameof(caption));

      if (caption.Trim().Length == 0)
        throw new ArgumentException("A caption cannot be empty or whitespace.", nameof(caption));

      var matches = new List<int>();

      for (var column = 0; column < Captions.Count; column++)
        if (CaptionComparer.Default.Equals(Captions[column], caption))
          matches.Add(column);

      return matches;
    }

    private ProjectionException Ambiguous(string caption, IReadOnlyList<int> matches)
      => _table.Failure(
        $"the caption '{caption}' matches the columns at "
        + $"{_table.Header.AddressOf(matches[0]).A1} ('{Captions[matches[0]]}') and "
        + $"{_table.Header.AddressOf(matches[1]).A1} ('{Captions[matches[1]]}'); "
        + "captions are matched ignoring case and whitespace");
  }
}
