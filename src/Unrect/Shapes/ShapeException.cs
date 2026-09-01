using System;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// The one exception the fused shape layer throws. It names the shape that failed, the path that
  /// reached it, and where on the sheet it was looking; a bare <see cref="OutOfBoundsException"/>
  /// never escapes a <c>Map</c> call.
  /// </summary>
  public sealed class ShapeException : Exception
  {
    internal ShapeException(
      string subject,
      string problem,
      string path,
      ShapeLocation location,
      Size? requested,
      IShape shape,
      Exception? inner,
      bool isProjectionFault = false)
      : base(BuildMessage(subject, problem, path, location), inner)
    {
      Subject = subject;
      Problem = problem;
      Path = path;
      Location = location;
      Requested = requested;
      Shape = shape;
      IsProjectionFault = isProjectionFault;
    }

    private ShapeException(string problem, ShapeException original)
      : this(
        original.Subject,
        problem,
        original.Path,
        original.Location,
        original.Requested,
        original.Shape,
        original,
        original.IsProjectionFault)
    {
    }

    public string Subject { get; }
    public string Path { get; }

    public ShapeLocation Location { get; }
    public Size? Requested { get; }
    public IShape Shape { get; }

    /// <summary>The problem on its own, without the subject, path, and location around it.</summary>
    internal string Problem { get; }

    /// <summary>
    /// True when the projection did not merely disagree with the data but broke: a null reference,
    /// an index past the end of the caller's own array. Tolerance boundaries absorb failures about
    /// the shape of the data, never bugs in the code reading it, so this travels with the failure
    /// to keep it from being swallowed.
    /// </summary>
    internal bool IsProjectionFault { get; }

    /// <summary>
    /// The same failure with something added to its problem — for context only the shape above it
    /// could know. The subject, path, and location are untouched: whatever failed still owns the
    /// failure, and the note merely points at what probably caused it. The original is kept as the
    /// inner exception, so <see cref="Exception.GetBaseException"/> still reaches the root cause.
    /// </summary>
    internal ShapeException WithNote(string note)
      // Problems do not end in a full stop, but one quoting a foreign exception message inherits
      // its punctuation; dropping it keeps the note from reading as ".; note:".
      => new ShapeException($"{Problem.TrimEnd('.')}; note: {note}", this);

    private static string BuildMessage(string subject, string problem, string path, ShapeLocation location)
      => $"{subject}: {problem}{Environment.NewLine}"
       + $"  in {path}{Environment.NewLine}"
       + $"  at {location}; {location.Available.Width}x{location.Available.Height} available";
  }
}
