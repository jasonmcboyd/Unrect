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
      bool isFault = false)
      : base(BuildMessage(subject, problem, path, location), inner)
    {
      Subject = subject;
      Problem = problem;
      Path = path;
      Location = location;
      Requested = requested;
      Shape = shape;
      IsFault = isFault;
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
        original.IsFault)
    {
    }

    /// <summary>The quoted name or description of the shape that failed.</summary>
    public string Subject { get; }

    /// <summary>The chain of enclosing shapes that reached <see cref="Subject"/>, rendered for a failure message.</summary>
    public string Path { get; }

    /// <summary>Where on the sheet the failure occurred.</summary>
    public ShapeLocation Location { get; }

    /// <summary>The extent the failing placement or extent strategy asked for, when the failure names one.</summary>
    public Size? Requested { get; }

    /// <summary>The shape that failed.</summary>
    public IShape Shape { get; }

    /// <summary>The problem on its own, without the subject, path, and location around it.</summary>
    internal string Problem { get; }

    /// <summary>
    /// True when something broke rather than disagreed: a bug in the reading code, or the
    /// environment failing underneath it — a null reference, an index past the end of the caller's
    /// own array, a disk that stopped answering, a workbook read after its owner was disposed.
    /// <para>
    /// Tolerance boundaries absorb failures about the shape of the data, never these, so the flag
    /// travels with the failure to keep it from being swallowed. It covers placement as well as
    /// projection: under streaming a strategy reads cells too, and a disk failure inside
    /// <c>SkipBlankRows</c> must never be reported as "the section is absent".
    /// </para>
    /// </summary>
    internal bool IsFault { get; }

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
