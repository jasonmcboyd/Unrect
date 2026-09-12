namespace Unrect.Projections
{
  /// <summary>
  /// How the body of a <c>Table</c> treats a fully-blank row (one whose every cell
  /// <see cref="Unrect.Core.CellValue.IsBlank"/> is true).
  /// </summary>
  public readonly struct BlankRowStrategy
  {
    private BlankRowStrategy(bool continues, DiagnosticSeverity? diagnostic, bool isFault)
    {
      Continues = continues;
      Diagnostic = diagnostic;
      IsFault = isFault;
    }

    // false == stop.
    internal bool Continues { get; }

    // null == none; Info == Tolerate.
    internal DiagnosticSeverity? Diagnostic { get; }

    // Terminal error: no tolerance boundary may absorb it.
    internal bool IsFault { get; }

    /// <summary>Stop at the first blank row (the default).</summary>
    public static BlankRowStrategy Stop => default;

    /// <summary>Skip a blank row and keep reading.</summary>
    public static BlankRowStrategy Skip => new BlankRowStrategy(true, null, false);

    /// <summary>Fail at a blank row.</summary>
    public static BlankRowStrategy Fault => new BlankRowStrategy(false, null, true);

    /// <summary>Skip a blank row, keep reading, and record an Info.</summary>
    public static BlankRowStrategy Tolerate => new BlankRowStrategy(true, DiagnosticSeverity.Info, false);

    /// <summary>True for <c>default(BlankRowStrategy)</c>: stop, no record, no diagnostic.</summary>
    internal bool IsStop => !Continues && Diagnostic is null && !IsFault;
  }
}
