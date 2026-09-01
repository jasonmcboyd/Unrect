using System;
using System.Linq;

namespace Unrect.Shapes
{
  /// <summary>
  /// The record of something the decomposition noticed: an alternative that did not match, a
  /// tolerance boundary that absorbed a failure, space nothing described. It carries the same
  /// subject, path, and location a <see cref="ShapeException"/> would, so a warning is as easy to
  /// act on as an error — for an absorbed failure, they describe the shape that actually failed
  /// rather than the boundary that caught it.
  /// </summary>
  public sealed class ShapeDiagnostic
  {
    internal ShapeDiagnostic(
      DiagnosticSeverity severity,
      string subject,
      string message,
      string path,
      ShapeLocation location)
    {
      Severity = severity;
      Subject = subject;
      Message = OneLine(message);
      Path = path;
      Location = location;
    }

    public DiagnosticSeverity Severity { get; }

    /// <summary>The problem, phrased as in a <see cref="ShapeException"/>.</summary>
    public string Message { get; }

    /// <summary>The quoted name or description of the shape the diagnostic is about.</summary>
    public string Subject { get; }

    /// <summary>The declaration path of what caused the event, not of what handled it.</summary>
    public string Path { get; }

    public ShapeLocation Location { get; }

    public override string ToString() => $"{Severity}: {Subject}: {Message} — in {Path} at {Location}";

    /// <summary>
    /// A diagnostic is one line in a list of them. A problem laid out over several lines — a
    /// choice's tally of what each alternative made of the space — is folded back into clauses.
    /// </summary>
    private static string OneLine(string message)
    {
      if (message.IndexOf('\n') < 0)
        return message;

      var clauses = message
        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
        .Select(clause => clause.Trim())
        .Where(clause => clause.Length > 0);

      return string.Join("; ", clauses);
    }
  }
}
