namespace Unrect.Projections
{
  /// <summary>
  /// How the body of a <c>Table</c> treats a fully-blank row (one whose every cell
  /// <see cref="Unrect.Core.CellValue.IsBlank"/> is true). The presets are named points in a
  /// three-axis space — Control (continue vs stop), Diagnostic (none, Info, or fault), and Value
  /// (skip a blank vs project one to a record). This value carries the Control and Diagnostic axes;
  /// the Value axis is the difference between the <c>onBlank:</c> overloads (which skip) and the
  /// <c>blankRecord:</c> overloads (which project).
  /// <para>
  /// <see cref="Stop"/> is <c>default</c> by construction, so a <c>Table</c> overload that takes
  /// <c>onBlank = default</c> behaves exactly as the overload without it: the table is self-bounding
  /// and ends at the first blank row.
  /// </para>
  /// </summary>
  public readonly struct BlankRowStrategy
  {
    private BlankRowStrategy(bool continues, DiagnosticSeverity? diagnostic, bool isFault)
    {
      Continues = continues;
      Diagnostic = diagnostic;
      IsFault = isFault;
    }

    // Control axis: false == stop.
    internal bool Continues { get; }

    // Diagnostic axis: null == none; Info == Tolerate.
    internal DiagnosticSeverity? Diagnostic { get; }

    // Terminal error — a blank row is malformed data no tolerance boundary may absorb.
    internal bool IsFault { get; }

    /// <summary>Stop at the first blank row — the default, self-bounding: the blank <em>is</em> the end.</summary>
    public static BlankRowStrategy Stop => default;

    /// <summary>Skip a blank row and keep reading; produce no record for it.</summary>
    public static BlankRowStrategy Skip => new BlankRowStrategy(true, null, false);

    /// <summary>Stop at a blank row and fail terminally — a blank row is malformed data.</summary>
    public static BlankRowStrategy Fault => new BlankRowStrategy(false, null, true);

    /// <summary>Skip a blank row, keep reading, and record a nonterminal Info for it.</summary>
    public static BlankRowStrategy Tolerate => new BlankRowStrategy(true, DiagnosticSeverity.Info, false);

    /// <summary>The default: stop, no record, no diagnostic. <c>default(BlankRowStrategy)</c> is this.</summary>
    internal bool IsStop => !Continues && Diagnostic is null && !IsFault;
  }
}
