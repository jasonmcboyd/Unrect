using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unrect.Size
{
  internal static class SizeExtensions
  {
    internal static Core.Offset ToOffset(this Core.Size size) => new Core.Offset(size.Width, size.Height);
    internal static Core.Area ToArea(this Core.Size size) => new Core.Area(size.Width, size.Height);
  }
}
