namespace Unrect.Projections
{
  /// <summary>
  /// How much a diagnostic ought to worry the reader.
  /// </summary>
  public enum DiagnosticSeverity
  {
    /// <summary>
    /// Something the decomposition noticed and handled as designed: an alternative that did not
    /// match, space the projection did not describe.
    /// </summary>
    Info,

    /// <summary>
    /// Declared tolerance was exercised — a projection failed and a boundary supplied a filler. The
    /// parse succeeded, but something in the file was not what the projection says it should be.
    /// </summary>
    Warning
  }
}
