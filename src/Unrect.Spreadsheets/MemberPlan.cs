using System;

namespace Unrect.Spreadsheets
{
  /// <summary>How one member of <c>T</c> is filled from a row: which caption, which reading.</summary>
  internal sealed class MemberPlan
  {
    public MemberPlan(string name, string caption, Type type, bool blankTolerant, int? position = null, Func<object, object?>? reading = null, Unrect.Projections.LabelStep[]? path = null)
    {
      Path = path;
      Reading = reading;
      Name = name;
      Caption = caption;
      Type = type;
      BlankTolerant = blankTolerant;
      Position = position;
    }

    /// <summary>The same member, read from the column at <paramref name="position"/> whatever its caption says.</summary>
    public MemberPlan At(int? position) => new MemberPlan(Name, Caption, Type, BlankTolerant, position, Reading, Path);

    /// <summary>The same member, read from the column <paramref name="path"/> names in a banded header.</summary>
    public MemberPlan Via(Unrect.Projections.LabelStep[]? path) => new MemberPlan(Name, Caption, Type, BlankTolerant, Position, Reading, path);

    /// <summary>The path through the header this member was bound by; null where it binds by caption, position or reading.</summary>
    public Unrect.Projections.LabelStep[]? Path { get; }

    /// <summary>The member's own name, for messages.</summary>
    public string Name { get; }

    /// <summary>The caption to look for — inferred from the name, or declared by an override.</summary>
    public string Caption { get; }

    /// <summary>
    /// The column this member was bound to by position, counted from the table's left edge; null
    /// where it binds by <see cref="Caption"/>.
    /// </summary>
    public int? Position { get; }

    /// <summary>
    /// The member's own reading of the row — handed the row as the space-typed view the binding
    /// declared it over — or null where it reads one column.
    /// </summary>
    public Func<object, object?>? Reading { get; }

    /// <summary>The member's own CLR type, stripped of nullability — which leaf reads this column.</summary>
    public Type Type { get; }

    /// <summary>A blank cell yields null rather than failing — <c>Nullable&lt;T&gt;</c> or <c>string?</c>.</summary>
    public bool BlankTolerant { get; }
  }
}
