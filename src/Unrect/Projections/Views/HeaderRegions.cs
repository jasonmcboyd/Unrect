using System;
using System.Collections.Generic;

namespace Unrect.Projections
{
  /// <summary>
  /// A header's words, folded into each column's path.
  /// <para>
  /// <b>The rule.</b> A merged cell's text belongs to every cell it covers, and a column's path is
  /// the distinct REGIONS it passes through from the top of the header to the bottom. "Date" merged
  /// down two rows is one region, so its column's path is <c>Date</c>; "From" merged across two
  /// columns is one region at the top of two columns, whose paths are <c>From, Id</c> and
  /// <c>From, Code</c>.
  /// </para>
  /// <para>
  /// <b>The convention.</b> A merged cell reads as a value in its first cell and blanks beside or
  /// beneath it, and nothing in the cells says whether a blank was merged over or merely left
  /// empty. So the regions are reconstructed the way every reader of such headers reconstructs
  /// them: a label claims the blank cells beneath it; if it has none beneath it, it claims the blank
  /// cells to its right — up to the next label in its row, never past the end of the region above
  /// it, and never over a column with nothing beneath it, which is a spacer. The last row holds the
  /// captions and never claims sideways: a blank caption is a column with no label. A wrong guess
  /// never reads a different cell; it names the same column under a band its author might not have
  /// given it.
  /// </para>
  /// <para>
  /// A pure function over words, asking nothing of a space but what its cells say: the blanks
  /// arrive as empty strings.
  /// </para>
  /// </summary>
  internal static class HeaderRegions
  {
    /// <summary>Each column's path, outermost band first; empty for a column with no label.</summary>
    /// <param name="rows">The header's rows of words, top to bottom, all the same length. The last is the captions.</param>
    internal static IReadOnlyList<IReadOnlyList<string>> Fold(IReadOnlyList<IReadOnlyList<string>> rows)
    {
      var width = rows.Count == 0 ? 0 : rows[0].Count;
      var captions = rows.Count - 1;
      var paths = new List<string>[width];

      // Where each column's region in the row above began — a band may not reach past the end of
      // the region over it. -1 where the column is under none.
      var above = new int[width];
      var closed = new bool[width];

      for (var column = 0; column < width; column++)
      {
        paths[column] = new List<string>();
        above[column] = -1;
      }

      bool Beneath(int row, int column)
      {
        for (var below = row + 1; below <= captions; below++)
          if (rows[below][column].Length > 0)
            return true;

        return false;
      }

      for (var row = 0; row < captions; row++)
      {
        var band = string.Empty;
        var start = -1;
        var parent = -1;
        var current = new int[width];

        for (var column = 0; column < width; column++)
        {
          current[column] = -1;

          if (closed[column])
          {
            band = string.Empty;
            continue;
          }

          var word = rows[row][column];

          if (word.Length > 0 && !Beneath(row, column))
          {
            // A tall region: the label over nothing but blanks. It is this column's own name, the
            // column is finished, and it claims nothing to its right.
            paths[column].Add(word);
            closed[column] = true;
            band = string.Empty;
            continue;
          }

          if (word.Length > 0)
          {
            band = word;
            start = column;
            parent = above[column];
          }
          else if (band.Length > 0 && (!Beneath(row, column) || above[column] != parent))
          {
            // A spacer, or the end of the region above: the band stops here.
            band = string.Empty;
          }

          if (band.Length > 0)
          {
            paths[column].Add(band);
            current[column] = start;
          }
        }

        above = current;
      }

      for (var column = 0; column < width; column++)
      {
        if (closed[column])
          continue;

        var caption = captions >= 0 ? rows[captions][column] : string.Empty;

        // A band names the columns that HAVE a caption. One with none is a column with no label,
        // whatever is written over it, and is reached by position.
        if (caption.Length == 0)
          paths[column].Clear();
        else
          paths[column].Add(caption);
      }

      return Array.ConvertAll(paths, path => (IReadOnlyList<string>)path);
    }
  }
}
