using System;

namespace Unrect.Core
{
  /// <summary>
  /// A strategy asked for more space than it was given, a cut did not fit the region it was cut
  /// from, or a coordinate lay outside a space. Carries no diagnostics at this level: the layer that
  /// knows a declaration's path and a cell's address adds them; a caller reading an
  /// <see cref="ISpace"/> directly sees it bare.
  /// </summary>
  public class OutOfBoundsException : Exception
  {
  }
}
