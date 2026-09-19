using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// Declares how one type's members bind to a table's captions, where the caption comparer would
  /// not have found them by itself. Immutable: every method returns a new instance, so a binding
  /// handed to two factories cannot be changed by either.
  /// </summary>
  /// <typeparam name="TSpace">The sheet the table is declared over — what a row handed to a member's own reading is a row of.</typeparam>
  /// <typeparam name="T">What one record reads.</typeparam>
  public sealed class TableBinding<TSpace, T>
    where TSpace : class, ISheetCells
  {
    internal TableBinding()
      : this(
        new Dictionary<string, string>(StringComparer.Ordinal),
        new Dictionary<string, int>(StringComparer.Ordinal),
        new Dictionary<string, Func<TableRow<TSpace>, object?>>(StringComparer.Ordinal),
        new List<string>())
    {
    }

    private TableBinding(
      IReadOnlyDictionary<string, string> captions,
      IReadOnlyDictionary<string, int> positions,
      IReadOnlyDictionary<string, Func<TableRow<TSpace>, object?>> readings,
      IReadOnlyCollection<string> ignored)
    {
      Captions = captions;
      Positions = positions;
      Readings = readings;
      Ignored = ignored;
    }

    internal IReadOnlyDictionary<string, string> Captions { get; }
    internal IReadOnlyDictionary<string, int> Positions { get; }
    internal IReadOnlyDictionary<string, Func<TableRow<TSpace>, object?>> Readings { get; }
    internal IReadOnlyCollection<string> Ignored { get; }

    /// <summary>
    /// Binds one member to a caption the comparer would not have found — a plural caption, a
    /// shorter member name, a heading with punctuation in it. The caption is still resolved through
    /// <see cref="CaptionComparer"/>, so this declares a <em>different caption</em>, not a different
    /// rule: <c>Column(t =&gt; t.Date, "Transaction Date")</c> still matches a header reading
    /// <c>"Transaction  Date"</c>.
    /// </summary>
    public TableBinding<TSpace, T> Column<TMember>(Expression<Func<T, TMember>> member, string caption)
    {
      var name = MemberName(member, nameof(member), nameof(Column));

      if (string.IsNullOrWhiteSpace(caption))
        throw new ArgumentException("A column caption cannot be empty or whitespace.", nameof(caption));

      if (IsBound(name))
        throw new ArgumentException($"{typeof(T).Name}.{name} is bound twice.", nameof(member));

      var captions = Captions.ToDictionary(bound => bound.Key, bound => bound.Value, StringComparer.Ordinal);

      captions.Add(name, caption);

      return new TableBinding<TSpace, T>(captions, Positions, Readings, Ignored);
    }

    /// <summary>
    /// Binds one member to a column by position — <c>Column(t =&gt; t.Amount, 3)</c> — for the column
    /// a caption cannot reach: one whose header cell is blank, or one of two that carry the same
    /// caption. The position is counted from the table's left edge, as <c>row[3]</c> counts it, and
    /// whatever caption that column has is not consulted. A position the table turns out not to
    /// have is a failure when the table is read, citing its header.
    /// </summary>
    public TableBinding<TSpace, T> Column<TMember>(Expression<Func<T, TMember>> member, int index)
    {
      var name = MemberName(member, nameof(member), nameof(Column));

      if (index < 0)
        throw new ArgumentOutOfRangeException(nameof(index), index, "A column position cannot be negative.");

      if (IsBound(name))
        throw new ArgumentException($"{typeof(T).Name}.{name} is bound twice.", nameof(member));

      var positions = Positions.ToDictionary(bound => bound.Key, bound => bound.Value, StringComparer.Ordinal);

      positions.Add(name, index);

      return new TableBinding<TSpace, T>(Captions, positions, Readings, Ignored);
    }

    /// <summary>
    /// Fills one member from the ROW rather than from a column —
    /// <c>Column(f =&gt; f.IsDeprecated, row =&gt; row["Fund"].Font().Color == CellColor.Red)</c> — for
    /// the thing a record should say that no caption holds: what a cell looks like, a sign, two
    /// columns compared. Every other member still binds by its caption with nothing declared. The
    /// row is the one a lambda table hands out, so cells are read by caption or by position and the
    /// reading may be anything the file's space can answer; a read that fails is located like any
    /// other, under the member's name.
    /// </summary>
    public TableBinding<TSpace, T> Column<TMember>(Expression<Func<T, TMember>> member, Func<TableRow<TSpace>, TMember> read)
    {
      var name = MemberName(member, nameof(member), nameof(Column));

      if (read is null)
        throw new ArgumentNullException(nameof(read));

      if (IsBound(name))
        throw new ArgumentException($"{typeof(T).Name}.{name} is bound twice.", nameof(member));

      var readings = Readings.ToDictionary(bound => bound.Key, bound => bound.Value, StringComparer.Ordinal);

      readings.Add(name, row => read(row));

      return new TableBinding<TSpace, T>(Captions, Positions, readings, Ignored);
    }

    private bool IsBound(string name)
      => Captions.ContainsKey(name) || Positions.ContainsKey(name) || Readings.ContainsKey(name);

    /// <summary>
    /// Declares that one member is not read from the table. The opt-out is per member and by name,
    /// deliberately: a blanket "non-strict" flag would tolerate the <em>next</em> member somebody
    /// adds too, silently, which is the failure mode strictness exists to prevent.
    /// </summary>
    public TableBinding<TSpace, T> Ignore<TMember>(Expression<Func<T, TMember>> member)
    {
      var name = MemberName(member, nameof(member), nameof(Ignore));
      var ignored = new List<string>(Ignored);

      // Ignoring twice is idempotent and harmless, so it is absorbed; binding twice is a
      // contradiction — two captions for one member — and stays an error.

      if (!ignored.Contains(name, StringComparer.Ordinal))
        ignored.Add(name);

      return new TableBinding<TSpace, T>(Captions, Positions, Readings, ignored);
    }

    /// <summary>
    /// A selector must be a direct property access on the lambda parameter — <c>t =&gt; t.Date</c>,
    /// optionally wrapped in the compiler's boxing conversion. Anything deeper names something the
    /// binder cannot fill.
    /// </summary>
    private static string MemberName<TMember>(Expression<Func<T, TMember>> member, string parameter, string caller)
    {
      if (member is null)
        throw new ArgumentNullException(parameter);

      var body = member.Body is UnaryExpression conversion && conversion.NodeType == ExpressionType.Convert
        ? conversion.Operand
        : member.Body;

      if (body is MemberExpression access
        && access.Expression is ParameterExpression
        && access.Member is PropertyInfo property)
        return property.Name;

      throw new ArgumentException(
        $"{caller}({member}) does not select a property of {typeof(T).Name}; select a property directly.",
        parameter);
    }
  }
}
