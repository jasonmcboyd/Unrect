using System;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// Which leaf reads a CLR type: the one place that decides that a <c>decimal</c> member and a
  /// <c>Decimal()</c> leaf are the same reading, so a bound record and a hand-written row cannot
  /// describe the same cell differently.
  /// </summary>
  internal static class KindedLeaves
  {
    /// <summary>
    /// Whether a member of this type can be read from a cell at all: the six kinds a leaf asserts,
    /// or the point itself.
    /// </summary>
    /// <param name="type">The member's type, with nullability already stripped.</param>
    /// <param name="space">The space the record is declared over, which is the point a member may ask for.</param>
    internal static bool Reads(Type type, Type space)
      => type == typeof(string)
      || type == typeof(decimal)
      || type == typeof(double)
      || type == typeof(int)
      || type == typeof(DateTime)
      || type == typeof(bool)
      || type == PointOf(space);

    /// <summary>
    /// The kind-agnostic member's type: the address of the labelled cell, from which a reader asks
    /// whatever the six leaves do not cover. Constructed at the record's own space, because a point
    /// is invariant in it — a record written over one sheet type cannot be filled from another.
    /// </summary>
    /// <param name="space">The space the record is declared over.</param>
    internal static Type PointOf(Type space) => typeof(Point<>).MakeGenericType(space);

    /// <summary>
    /// The leaf that fills <paramref name="member"/>, boxed so one array of them can fill a record
    /// of mixed types. Blank tolerance rides on the leaf, so a nullable member reads a blank as
    /// null and still fails on the wrong kind.
    /// </summary>
    /// <typeparam name="TSpace">The sheet the record is declared over.</typeparam>
    /// <param name="member">The member to fill.</param>
    internal static IProjectionDefinition<TSpace, object?> For<TSpace>(MemberPlan member)
      where TSpace : class, ISheetCells
    {
      var leaf = Boxed<TSpace>(member.Type);

      return member.BlankTolerant ? Tolerant(leaf) : leaf;
    }

    private static IProjectionDefinition<TSpace, object?> Boxed<TSpace>(Type type)
      where TSpace : class, ISheetCells
    {
      // The one member that asserts nothing: it hands over the cell's address and lets the reader
      // ask. No kind, so no kind failure, and nothing for blank tolerance to do.
      if (type == typeof(Point<TSpace>))
        return ProjectionBuilders<TSpace>.Point().Select(point => (object?)point);

      if (type == typeof(string))
        return SpreadsheetProjections.Kinded<TSpace, object?>(
          "Text", (Point<TSpace> cell, out object? v, out CellProblem? p) => Box(cell.Space.TryGetTextAt(cell.Column, cell.Row, out var value, out p), value, out v));

      if (type == typeof(decimal))
        return SpreadsheetProjections.Kinded<TSpace, object?>(
          "Decimal", (Point<TSpace> cell, out object? v, out CellProblem? p) => Box(CellReading.Decimal(cell, out var value, out p), value, out v));

      if (type == typeof(double))
        return SpreadsheetProjections.Kinded<TSpace, object?>(
          "Double", (Point<TSpace> cell, out object? v, out CellProblem? p) => Box(cell.Space.TryGetDoubleAt(cell.Column, cell.Row, out var value, out p), value, out v));

      if (type == typeof(int))
        return SpreadsheetProjections.Kinded<TSpace, object?>(
          "Integer", (Point<TSpace> cell, out object? v, out CellProblem? p) => Box(CellReading.Integer(cell, out var value, out p), value, out v));

      if (type == typeof(DateTime))
        return SpreadsheetProjections.Kinded<TSpace, object?>(
          "Date", (Point<TSpace> cell, out object? v, out CellProblem? p) => Box(cell.Space.TryGetDateTimeAt(cell.Column, cell.Row, out var value, out p), value, out v));

      if (type == typeof(bool))
        return SpreadsheetProjections.Kinded<TSpace, object?>(
          "Boolean", (Point<TSpace> cell, out object? v, out CellProblem? p) => Box(cell.Space.TryGetBooleanAt(cell.Column, cell.Row, out var value, out p), value, out v));

      // Unreachable: the plan refuses a type Reads says no to, where it is written rather than per
      // file. Kept so this method is correct read on its own rather than only in context.
      throw new ArgumentException($"No cell reading yields {type.Name}.", nameof(type));
    }

    private static bool Box<T>(bool read, T value, out object? boxed)
    {
      boxed = read ? value : null;
      return read;
    }

    /// <summary>
    /// The same leaf, reading a blank cell as null — <c>OrBlank</c>'s own seam, reached here rather
    /// than through the modifier because the result type is already <c>object?</c>.
    /// <para>
    /// The cast is the invariant the point branch of <see cref="Boxed{TSpace}"/> pays for: every
    /// leaf built there is a <see cref="ReadDefinition{TSpace, TResult}"/> EXCEPT the point's,
    /// which is a <c>Select</c> over <c>Point()</c> — and a point member is never blank-tolerant,
    /// because <c>RowBinding</c> refuses <c>Point&lt;TSpace&gt;?</c> where it is declared. Take that
    /// refusal away and this cast is where it would be felt.
    /// </para>
    /// </summary>
    private static IProjectionDefinition<TSpace, object?> Tolerant<TSpace>(IProjectionDefinition<TSpace, object?> leaf)
      where TSpace : class, ISheetCells
      => ((ReadDefinition<TSpace, object?>)leaf).Tolerating<object?>(value => value);
  }
}
