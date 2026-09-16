using System;

namespace Unrect.Spreadsheets
{
  /// <summary>How one member of <c>T</c> is filled from a row: which caption, which reading.</summary>
  internal sealed class MemberPlan
  {
    public MemberPlan(string name, string caption, Type type, bool blankTolerant)
    {
      Name = name;
      Caption = caption;
      Type = type;
      BlankTolerant = blankTolerant;
    }

    /// <summary>The member's own name, for messages.</summary>
    public string Name { get; }

    /// <summary>The caption to look for — inferred from the name, or declared by an override.</summary>
    public string Caption { get; }

    /// <summary>The member's own CLR type, stripped of nullability — which leaf reads this column.</summary>
    public Type Type { get; }

    /// <summary>A blank cell yields null rather than failing — <c>Nullable&lt;T&gt;</c> or <c>string?</c>.</summary>
    public bool BlankTolerant { get; }
  }
}
