using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>How one member of <c>T</c> is filled from a row: which caption, which kind, how read.</summary>
  internal sealed class MemberPlan
  {
    public MemberPlan(string name, string caption, CellKind? kind, bool blankTolerant, CellReader<object?>? read)
    {
      Name = name;
      Caption = caption;
      Kind = kind;
      BlankTolerant = blankTolerant;
      Read = read;
    }

    /// <summary>The member's own name, for messages.</summary>
    public string Name { get; }

    /// <summary>The caption to look for — inferred from the name, or declared by an override.</summary>
    public string Caption { get; }

    /// <summary>The kind to assert, or null for a <c>CellValue</c> member, which asserts nothing.</summary>
    public CellKind? Kind { get; }

    /// <summary>A blank cell yields null rather than failing — <c>Nullable&lt;T&gt;</c> or <c>string?</c>.</summary>
    public bool BlankTolerant { get; }

    /// <summary>Null for a <c>CellValue</c> member, which is handed the cell itself.</summary>
    public CellReader<object?>? Read { get; }
  }
}
