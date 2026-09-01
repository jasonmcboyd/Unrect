namespace Unrect.Shapes
{
  /// <summary>
  /// How much a diagnostic ought to worry the reader.
  /// </summary>
  public enum DiagnosticSeverity
  {
    /// <summary>
    /// Something the decomposition noticed and handled as designed: an alternative that did not
    /// match, space the shape did not describe.
    /// </summary>
    Info,

    /// <summary>
    /// Declared tolerance was exercised — a shape failed and a boundary supplied a filler. The
    /// parse succeeded, but something in the file was not what the shape says it should be.
    /// </summary>
    Warning
  }
}
