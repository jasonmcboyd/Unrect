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
      Exception? inner)
      : base(BuildMessage(subject, problem, path, location), inner)
    {
      Subject = subject;
      Path = path;
      Location = location;
      Requested = requested;
      Shape = shape;
    }

    public string Subject { get; }
    public string Path { get; }
    public ShapeLocation Location { get; }
    public Size? Requested { get; }
    public IShape Shape { get; }

    private static string BuildMessage(string subject, string problem, string path, ShapeLocation location)
      => $"{subject}: {problem}{Environment.NewLine}"
       + $"  in {path}{Environment.NewLine}"
       + $"  at {location}; {location.Available.Width}x{location.Available.Height} available";
  }
}
