using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

namespace Unrect.Interactive
{
  /// <summary>
  /// Where a scaffold finds the labels a type's members are named after.
  /// </summary>
  public enum LabelsIn
  {
    /// <summary>
    /// A row of captions with the data beneath it — a table, or one record laid out across. What
    /// <c>Table&lt;T&gt;()</c> binds.
    /// </summary>
    Row,

    /// <summary>
    /// A column of labels with the values to its right — the card that heads so many reports: one
    /// record, read across instead of down.
    /// </summary>
    Column,
  }

  /// <summary>
  /// A first type declaration, read off a sheet and handed back as C# source to paste.
  /// <para>
  /// Exploratory, like everything in <c>Unrect.Interactive</c>: nothing in the library calls it, and
  /// what it returns is text, never a projection. C# has no type providers, so the way a script
  /// gets a type for an unfamiliar file is that something writes one and a human pastes it; this is
  /// that something. Dump what it returns, paste it, and edit it.
  /// </para>
  /// <para>
  /// <b>It is a starting point, not a truth.</b> Every type it writes is a guess from a handful of
  /// samples: a column of whole numbers in the sample reads as <c>int</c> however wide the file's
  /// numbers get further down, and a column that happens not to be blank in the first five rows
  /// reads as non-nullable. Where a label cannot become a member name that binds back to it, the
  /// source says so in a comment rather than leaving it to the first failed <c>Map</c>.
  /// </para>
  /// </summary>
  public static class Scaffolding
  {
    /// <summary>How many rows, or columns, are looked through for the labels when nobody says where they are.</summary>
    private const int LabelSearch = 50;

    /// <summary>The longest a positional record is written on one line before it is broken one member per line.</summary>
    private const int LineLimit = 120;

    /// <summary>
    /// The C# source of a positional record matching <paramref name="sheet"/>'s labels:
    /// <code>
    /// public sealed record Transaction(string Client, DateTime TransactionDate, decimal Amount);
    /// </code>
    /// <para>
    /// <b>The labels</b> are the text cells of one row (<see cref="LabelsIn.Row"/>, the default —
    /// a table, or one record laid out across) or of one column (<see cref="LabelsIn.Column"/> —
    /// a card, its values to the right). Which row or column is found rather than asked: the first
    /// whose cells are all text and which is at least half as full as the fullest, which steps over
    /// a title block. <paramref name="at"/> says it outright where that guess is wrong. A blank
    /// label cell, and one that is not text, is skipped rather than named.
    /// </para>
    /// <para>
    /// <b>The names.</b> A label becomes a member name by dropping everything that is not a letter,
    /// digit or underscore and capitalising each run that survives, which is what
    /// <see cref="CaptionComparer"/> binds back to — so <c>"Contribution ITD"</c> becomes
    /// <c>ContributionITD</c> and binds with nothing declared. A name that would start with a digit
    /// is prefixed with <c>_</c>, a label with no usable characters becomes <c>Column</c> and its
    /// 1-based position among the labels named, and a name already taken gains a number. Wherever
    /// the result does <em>not</em> bind back to its label — <c>"Net (USD)"</c> becomes
    /// <c>NetUSD</c>, and the comparer counts the parentheses — a comment above the type says so and
    /// writes the <c>.Column(…)</c> override that fixes it.
    /// </para>
    /// <para>
    /// <b>The types</b> are read from up to <paramref name="samples"/> cells beside each label —
    /// beneath it, or to its right: all numbers is <c>decimal</c>, or <c>int</c> where every one is
    /// whole and fits one; all dates is <c>DateTime</c>, all booleans <c>bool</c>, and anything else
    /// — text, or a mix — is <c>string</c>. A blank sample, or an error cell (a condition rather
    /// than a value), makes the member nullable; a label with no samples at all is <c>string?</c>.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The sheet being read.</typeparam>
    /// <param name="sheet">The sheet to read labels and samples from.</param>
    /// <param name="typeName">The type's name.</param>
    /// <param name="labels">Whether the labels run along a row or down a column.</param>
    /// <param name="at">The 0-based row, or column, the labels are on; found when omitted.</param>
    /// <param name="samples">How many cells beside each label to type the member from.</param>
    /// <param name="headerRows">
    /// How many rows the header is: the captions, under any rows of bands. With bands a member is
    /// named as <c>Table&lt;T&gt;(headerRows)</c> will match it — its band and caption run
    /// together, <c>FromId</c> for the column at From, Id — and typed from the cells under the
    /// whole header. Along a row only.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="sheet"/> or <paramref name="typeName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="typeName"/> is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="at"/> or <paramref name="samples"/> is negative.</exception>
    /// <exception cref="OutOfBoundsException"><paramref name="at"/> is past the end of the sheet.</exception>
    /// <exception cref="InvalidOperationException">No labels were found and <paramref name="at"/> was not given.</exception>
    public static string ScaffoldRecord<TSpace>(this TSpace sheet, string typeName, LabelsIn labels = LabelsIn.Row, int? at = null, int samples = 5, int headerRows = 1)
      where TSpace : class, ICellSpace
      => Record(Read(Region.Of(sheet), typeName, labels, at, samples, headerRows), typeName, labels, headerRows);

    internal static string Record(List<Member> members, string typeName, LabelsIn labels, int headerRows = 1)
    {
      var source = new StringBuilder();

      if (headerRows > 1)
        source.Append($"// binds with Table<{typeName}>({headerRows}): a member answers to a caption, or to its band and caption run together").Append(Environment.NewLine);

      foreach (var note in Notes(members, typeName, labels))
        source.Append("// ").Append(note).Append(Environment.NewLine);

      var parameters = members.Select(member => $"{member.Type} {member.Name}").ToList();
      var oneLine = $"public sealed record {typeName}({string.Join(", ", parameters)});";

      if (oneLine.Length <= LineLimit)
        return source.Append(oneLine).ToString();

      source.Append($"public sealed record {typeName}(");

      for (var index = 0; index < parameters.Count; index++)
        source.Append(Environment.NewLine).Append("    ").Append(parameters[index]).Append(index < parameters.Count - 1 ? "," : ");");

      return source.ToString();
    }

    /// <summary>
    /// The same type as <see cref="ScaffoldRecord{TSpace}"/> writes, as a class with a parameterless
    /// constructor and one init-only property per label — the other shape <c>Table&lt;T&gt;()</c>
    /// binds:
    /// <code>
    /// public sealed class Transaction
    /// {
    ///     public string Client { get; init; } = "";
    ///     public decimal Amount { get; init; }
    /// }
    /// </code>
    /// A label that does not bind back to its member is noted in a comment above that property.
    /// </summary>
    /// <typeparam name="TSpace">The sheet being read.</typeparam>
    /// <param name="sheet">The sheet to read labels and samples from.</param>
    /// <param name="typeName">The type's name.</param>
    /// <param name="labels">Whether the labels run along a row or down a column.</param>
    /// <param name="at">The 0-based row, or column, the labels are on; found when omitted.</param>
    /// <param name="samples">How many cells beside each label to type the member from.</param>
    /// <param name="headerRows">
    /// How many rows the header is: the captions, under any rows of bands. With bands a member is
    /// named as <c>Table&lt;T&gt;(headerRows)</c> will match it — its band and caption run
    /// together, <c>FromId</c> for the column at From, Id — and typed from the cells under the
    /// whole header. Along a row only.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="sheet"/> or <paramref name="typeName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="typeName"/> is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="at"/> or <paramref name="samples"/> is negative.</exception>
    /// <exception cref="OutOfBoundsException"><paramref name="at"/> is past the end of the sheet.</exception>
    /// <exception cref="InvalidOperationException">No labels were found and <paramref name="at"/> was not given.</exception>
    public static string ScaffoldClass<TSpace>(this TSpace sheet, string typeName, LabelsIn labels = LabelsIn.Row, int? at = null, int samples = 5, int headerRows = 1)
      where TSpace : class, ICellSpace
      => Class(Read(Region.Of(sheet), typeName, labels, at, samples, headerRows), typeName, labels, headerRows);

    internal static string Class(List<Member> members, string typeName, LabelsIn labels, int headerRows = 1)
    {
      var notes = members.ToDictionary(member => member.Name, _ => new List<string>());

      foreach (var (member, note) in MemberNotes(members, typeName, labels))
        notes[member].Add(note);

      var source = new StringBuilder();

      if (headerRows > 1)
        source.Append($"// binds with Table<{typeName}>({headerRows}): a member answers to a caption, or to its band and caption run together").Append(Environment.NewLine);

      source.Append($"public sealed class {typeName}").Append(Environment.NewLine).Append('{');

      foreach (var member in members)
      {
        foreach (var note in notes[member.Name])
          source.Append(Environment.NewLine).Append("    // ").Append(note);

        // A non-nullable string with nothing assigned is a warning wherever nullable is on, and a
        // pasted type should be clean in either setting.
        source.Append(Environment.NewLine).Append($"    public {member.Type} {member.Name} {{ get; init; }}")
          .Append(member.Type == "string" ? " = \"\";" : string.Empty);
      }

      return source.Append(Environment.NewLine).Append('}').ToString();
    }

    // --- Reading the sheet ---------------------------------------------------------------------------

    /// <summary>One member of the scaffolded type: the label it came from, what it is called, and what its samples argued for.</summary>
    internal sealed class Member
    {
      public Member(string label, string name, string type, int position, IReadOnlyList<string>? path = null)
      {
        Path = path ?? new[] { label };
        Label = label;
        Name = name;
        Type = type;
        Position = position;
      }

      public string Label { get; }

      public string Name { get; }

      public string Type { get; }

      /// <summary>How far along the label line this member's label sits, counted from the region's edge.</summary>
      public int Position { get; }

      /// <summary>The label's path through a banded header: its bands, then the label. One step where there are none.</summary>
      public IReadOnlyList<string> Path { get; }

      /// <summary>The path run together, which is what a flat member's name is matched against under bands.</summary>
      public string RunTogether => string.Concat(Path);

      /// <summary>Whether the binder would find this member's column from its name alone.</summary>
      public bool Binds => CaptionComparer.Default.Equals(Name, Label) || (Path.Count > 1 && CaptionComparer.Default.Equals(Name, RunTogether));
    }

    internal static List<Member> Read(Region region, string typeName, LabelsIn labels, int? at, int samples, int headerRows = 1, IReadOnlyList<IReadOnlyList<string>>? paths = null)
    {
      Name(typeName);

      if (headerRows < 1)
        throw new ArgumentOutOfRangeException(nameof(headerRows), headerRows, "A scaffold reads its names from a header, which is at least one row.");

      if (headerRows > 1 && labels != LabelsIn.Row)
        throw new ArgumentException("Rows of bands over the captions are read along a row; a header down a column has one line of labels.", nameof(headerRows));

      if (at is int given && given < 0)
        throw new ArgumentOutOfRangeException(nameof(at));

      if (samples < 0)
        throw new ArgumentOutOfRangeException(nameof(samples));

      var grid = new Axes(region, labels);

      // Checked here rather than left to the first cell read: a sheet with no cells along the label
      // line reads none at all, and would otherwise scaffold an empty type from a line that does not
      // exist.
      if (at is int line && line >= grid.Lines)
        throw new OutOfBoundsException();

      var labelLine = at ?? FindLabels(grid);
      var taken = new HashSet<string>(CaptionComparer.Default);
      var members = new List<Member>();

      if (headerRows > 1)
      {
        if (labelLine + headerRows > grid.Lines)
          throw new OutOfBoundsException();

        // The header parse is the table's own, so what is scaffolded is what will bind: the paths a
        // Table<T>(headerRows) would see, read from the same rows.
        paths ??= ProjectionBuilders<ICellSpace>
          .Down(region.Row + labelLine)
          .Of(ProjectionBuilders<ICellSpace>.ColumnLabels(headerRows))
          .Map(region.Sheet)
          .Paths;

        for (var position = 0; position < grid.Length && region.Column + position < paths.Count; position++)
        {
          var path = paths[region.Column + position];

          if (path.Count == 0)
            continue;

          // Named as it will be matched: the band and the caption run together.
          var name = Distinct(string.Concat(path.Select((step, index) => Identifier(step, members.Count).TrimStart(index == 0 ? '\0' : '_'))), taken);

          members.Add(new Member(path[path.Count - 1], name, Type(grid, labelLine + headerRows - 1, position, samples), position, path));
        }

        return members;
      }

      for (var position = 0; position < grid.Length; position++)
      {
        if (!grid.Text(labelLine, position, out var label) || label.Trim().Length == 0)
          continue;

        var name = Distinct(Identifier(label, members.Count), taken);

        members.Add(new Member(label.Trim(), name, Type(grid, labelLine, position, samples), position));
      }

      return members;
    }

    /// <summary>Refuses a type name nothing could be declared under.</summary>
    internal static void Name(string typeName)
    {
      if (typeName is null)
        throw new ArgumentNullException(nameof(typeName));

      if (typeName.Trim().Length == 0)
        throw new ArgumentException("A scaffolded type needs a name.", nameof(typeName));
    }

    /// <summary>The cells a scaffold reads: a whole sheet, or the region a declaration handed it.</summary>
    internal readonly struct Region
    {
      public Region(ICellSpace sheet, int column, int row, int width, int height)
      {
        Sheet = sheet;
        Column = column;
        Row = row;
        Width = width;
        Height = height;
      }

      public ICellSpace Sheet { get; }

      public int Column { get; }

      public int Row { get; }

      public int Width { get; }

      public int Height { get; }

      public static Region Of(ICellSpace sheet)
        => sheet is null
          ? throw new ArgumentNullException(nameof(sheet))
          : new Region(sheet, 0, 0, sheet.Area.Width, sheet.Area.Height);

      public static Region Of<TSpace>(Plane<TSpace> plane)
        where TSpace : class, ICellSpace
        => new Region(plane.Space, plane.Origin.Width, plane.Origin.Height, plane.Width, plane.Area.Height);
    }

    /// <summary>
    /// The sheet read along the label axis: a <em>line</em> is a row when the labels run along one
    /// and a column when they run down one, and a <em>position</em> is how far along that line.
    /// The samples for a label sit on the lines after the label's own, at the same position.
    /// </summary>
    private readonly struct Axes
    {
      private readonly Region _region;
      private readonly ICellSpace _sheet;
      private readonly bool _rows;

      public Axes(Region region, LabelsIn labels)
      {
        _region = region;
        _sheet = region.Sheet;
        _rows = labels == LabelsIn.Row;
      }

      public int Lines => _rows ? _region.Height : _region.Width;

      public int Length => _rows ? _region.Width : _region.Height;

      public string LineNoun => _rows ? "row" : "column";

      private int Column(int line, int position) => _region.Column + (_rows ? position : line);

      private int Row(int line, int position) => _region.Row + (_rows ? line : position);

      public bool IsBlank(int line, int position) => _sheet.IsBlankAt(Column(line, position), Row(line, position));

      public bool IsText(int line, int position) => _sheet.ValueAt(Column(line, position), Row(line, position)).Kind == CellKind.Text;

      public bool IsError(int line, int position) => _sheet.ValueAt(Column(line, position), Row(line, position)).Kind == CellKind.Error;

      public bool Text(int line, int position, out string text) => _sheet.ValueAt(Column(line, position), Row(line, position)).TryGetText(out text);

      private static bool IsWhole(double number)
        => number >= int.MinValue && number <= int.MaxValue && Math.Floor(number) == number;

      /// <summary>The type one cell reads as, narrowest first — a whole number is an <c>int</c> until another sample says otherwise.</summary>
      public string Reads(int line, int position)
      {
        var value = _sheet.ValueAt(Column(line, position), Row(line, position));

        return value.Kind switch
        {
          CellKind.Number => value.TryGetNumber(out var number) && IsWhole(number) ? "int" : "decimal",
          CellKind.Temporal => "DateTime",
          CellKind.Boolean => "bool",
          _ => "string",
        };
      }
    }

    /// <summary>
    /// The line the labels are on: the first whose cells are all text and which is at least half as
    /// full as the fullest line looked at. All text, because a label is a word someone typed; half as
    /// full, because a title block is a line or two of one cell each sitting over a table several
    /// cells wide, and that is the thing this has to step over.
    /// </summary>
    private static int FindLabels(Axes grid)
    {
      var search = Math.Min(grid.Lines, LabelSearch);
      var filled = new int[search];
      var allText = new bool[search];
      var fullest = 0;

      for (var line = 0; line < search; line++)
      {
        allText[line] = true;

        for (var position = 0; position < grid.Length; position++)
        {
          if (grid.IsBlank(line, position))
            continue;

          filled[line]++;
          allText[line] &= grid.IsText(line, position);
        }

        fullest = Math.Max(fullest, filled[line]);
      }

      var needed = (fullest + 1) / 2;

      for (var line = 0; line < search; line++)
        if (filled[line] > 0 && allText[line] && filled[line] >= needed)
          return line;

      throw new InvalidOperationException(
        $"No {grid.LineNoun} of labels was found in the first {search} {grid.LineNoun}s: none is all text and at least "
        + $"half as full as the fullest. Pass at: to say which {grid.LineNoun} the labels are on.");
    }

    /// <summary>
    /// The C# type the sample cells argue for: what each of them reads as, folded together until
    /// they either agree or stop agreeing. A member is typed by all of its samples rather than by
    /// its first, because "all of them agree" is the only claim a handful of cells can support.
    /// </summary>
    private static string Type(Axes grid, int labelLine, int position, int samples)
    {
      string? agreed = null;
      var blanks = false;

      for (var offset = 1; offset <= samples && labelLine + offset < grid.Lines; offset++)
      {
        var line = labelLine + offset;

        // An error is a condition, not a value — the same cell a nullable member tolerates.
        if (grid.IsBlank(line, position) || grid.IsError(line, position))
        {
          blanks = true;
          continue;
        }

        var sample = grid.Reads(line, position);

        agreed = agreed is null ? sample : Agree(agreed, sample);
      }

      // No samples at all is the same answer as samples that agreed on nothing, except that it is
      // also a reason to be nullable: nothing was seen, so nothing rules a blank out.
      return agreed is null ? "string?" : blanks ? agreed + "?" : agreed;
    }

    /// <summary>
    /// The type that covers both samples. Only one pair genuinely meets: a whole number and a
    /// fractional one are both numbers, so they widen to <c>decimal</c> rather than disagreeing.
    /// Everything else that differs is a column this cannot type, which is <c>string</c> — what
    /// every cell can say.
    /// </summary>
    private static string Agree(string first, string second)
      => first == second ? first
        : (first == "int" || first == "decimal") && (second == "int" || second == "decimal") ? "decimal"
        : "string";

    // --- Naming -------------------------------------------------------------------------------------

    /// <summary>
    /// <paramref name="label"/> as a C# identifier: every run of identifier characters capitalised,
    /// everything else dropped. What survives is spelled as the file spelled it, so an acronym stays
    /// one.
    /// </summary>
    private static string Identifier(string label, int index)
    {
      var identifier = new StringBuilder(label.Length);
      var starting = true;

      foreach (var character in label)
      {
        if (!char.IsLetterOrDigit(character) && character != '_')
        {
          starting = true;
          continue;
        }

        identifier.Append(starting ? char.ToUpperInvariant(character) : character);
        starting = false;
      }

      if (identifier.Length == 0)
        return "Column" + (index + 1).ToString(CultureInfo.InvariantCulture);

      return char.IsDigit(identifier[0])
        ? "_" + identifier.ToString()
        : identifier.ToString();
    }

    /// <summary>
    /// <paramref name="name"/>, numbered until nothing else has it. Numbered under
    /// <see cref="CaptionComparer"/> rather than by string equality, because two members differing
    /// only in case or spacing would bind to the same column — which is the collision worth avoiding,
    /// not the one the compiler would have caught.
    /// </summary>
    private static string Distinct(string name, HashSet<string> taken)
    {
      var candidate = name;

      for (var suffix = 2; taken.Contains(candidate); suffix++)
        candidate = name + suffix.ToString(CultureInfo.InvariantCulture);

      taken.Add(candidate);

      return candidate;
    }

    // --- What the reader has to be told ---------------------------------------------------------------

    private static IEnumerable<string> Notes(List<Member> members, string typeName, LabelsIn labels)
      => MemberNotes(members, typeName, labels).Select(note => note.Note).Distinct();

    /// <summary>
    /// What a human has to know before this type will bind, one note per member it concerns. Two
    /// labels that are one caption to the comparer cannot be told apart by name, so each of their
    /// members carries the override that binds it by position; a label that merely lost characters
    /// on the way to a name gets the override that names it.
    /// </summary>
    private static IEnumerable<(string Member, string Note)> MemberNotes(List<Member> members, string typeName, LabelsIn labels)
    {
      var parameter = char.ToLowerInvariant(typeName.Trim()[0]).ToString();

      foreach (var member in members)
      {
        var twins = members.Where(other => CaptionComparer.Default.Equals(other.RunTogether, member.RunTogether)).ToList();

        if (twins.Count > 1)
        {
          var labelled = string.Join(" and ", twins.Select(twin => Literal(twin.Label)));

          yield return (
            member.Name,
            labels == LabelsIn.Row
              ? $"{labelled} are one caption to the binder: .Column({parameter} => {parameter}.{member.Name}, {member.Position})"
              : $"{labelled} are one label, which nothing can tell apart by name");

          continue;
        }

        if (member.Binds)
          continue;

        yield return (
          member.Name,
          labels == LabelsIn.Row
            ? $"{Literal(member.Label)} does not bind to {member.Name} by name: .Column({parameter} => {parameter}.{member.Name}, {string.Join(", ", member.Path.Select(Literal))})"
            : $"{Literal(member.Label)} does not match {member.Name} by name");
      }
    }

    /// <summary><paramref name="text"/> as a C# string literal.</summary>
    private static string Literal(string text)
      => "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
  }
}
