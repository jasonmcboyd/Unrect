using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Unrect.Projections
{
  /// <summary>
  /// What a path through a header comes to: one column, a band of several, or a sentence saying why
  /// it came to neither. The sentence names what IS there, so the fix can be read off it.
  /// </summary>
  internal readonly struct LabelAnswer
  {
    private LabelAnswer(IReadOnlyList<int> columns, int depth, bool band, string? problem)
    {
      Columns = columns;
      Depth = depth;
      IsBand = band;
      Problem = problem;
    }

    /// <summary>The column the path names, or the columns of the band it names.</summary>
    internal IReadOnlyList<int> Columns { get; }

    /// <summary>How many steps of a column's path the answer has consumed — where a band's own children begin.</summary>
    internal int Depth { get; }

    internal bool IsBand { get; }

    internal string? Problem { get; }

    internal static LabelAnswer Column(int column, int depth) => new LabelAnswer(new[] { column }, depth, false, null);

    internal static LabelAnswer Band(IReadOnlyList<int> columns, int depth) => new LabelAnswer(columns, depth, true, null);

    internal static LabelAnswer Failed(string problem) => new LabelAnswer(Array.Empty<int>(), 0, false, problem);
  }

  /// <summary>
  /// Walks a path of <see cref="LabelStep"/>s down a header's regions.
  /// <para>
  /// A region is a run of columns that share a label at one level of their paths and began at the
  /// same column — which is what keeps two bands that say the same thing two bands, and what
  /// <c>("Totals", 1)</c> addresses. A one-step path is a column under no band before it is a
  /// caption somewhere under one; a longer path is exact.
  /// </para>
  /// </summary>
  internal static class LabelPaths
  {
    internal static LabelAnswer Resolve(ILabelSource source, IReadOnlyList<LabelStep> steps, IEqualityComparer<string> comparer, IReadOnlyList<int>? within = null, int depth = 0)
    {
      if (steps is null)
        throw new ArgumentNullException(nameof(steps));

      if (steps.Count == 0)
        throw new ArgumentException("A path needs at least one step.", nameof(steps));

      var paths = source.Paths;
      var starts = source.Starts;
      var columns = within ?? Enumerable.Range(0, paths.Count).Where(column => paths[column].Count > 0).ToList();
      var reached = "the header";

      for (var at = 0; at < steps.Count; at++)
      {
        var step = steps[at];
        var last = at == steps.Count - 1;

        if (step.IsPosition)
        {
          if (!last)
            return LabelAnswer.Failed($"{step} is a position, which names a column, so nothing can follow it in a path");

          var span = Span(columns);
          var position = step.Index.GetValueOrDefault();

          return position >= 0 && position < span.Count
            ? LabelAnswer.Column(span[position], depth)
            : LabelAnswer.Failed($"{reached} spans {Count(span.Count, "column")} (0 to {(span.Count - 1).ToString(CultureInfo.InvariantCulture)}), so there is no position {position.ToString(CultureInfo.InvariantCulture)}");
        }

        var level = depth;
        var named = columns.Where(column => paths[column].Count > level && comparer.Equals(paths[column][level], step.Name!)).ToList();

        var regions = named.GroupBy(column => starts[column][level]).OrderBy(region => region.Key).Select(region => region.ToList()).ToList();

        // A one-step path is a NAME rather than a walk: the column whose whole path it is, before
        // a caption somewhere under a band, before a band. With an index it counts every column
        // that carries the caption, in column order — ("Id", 0) is the first Id wherever it sits.
        if (steps.Count == 1 && depth == 0)
        {
          var captioned = columns.Where(column => comparer.Equals(paths[column][paths[column].Count - 1], step.Name!)).ToList();
          var whole = captioned.Where(column => paths[column].Count == 1).ToList();
          var leaves = step.Index is null && whole.Count > 0 ? whole : captioned;

          if (leaves.Count > 0)
            return Pick(leaves, step, column => column, column => LabelAnswer.Column(column, paths[column].Count), paths, reached, columns, level);
        }

        var picked = Pick(regions, step, region => region[0], region => Answer(region, level + 1, paths), paths, reached, columns, level);

        if (picked.Problem is not null || last)
          return picked;

        if (!picked.IsBand)
          return LabelAnswer.Failed($"{step} is a column, not a band, so nothing is under it");

        columns = picked.Columns;
        depth = level + 1;
        reached = step.ToString();
      }

      throw new InvalidOperationException("a path's last step returns");
    }

    private static LabelAnswer Answer(List<int> region, int depth, IReadOnlyList<IReadOnlyList<string>> paths)
      => region.Count == 1 && paths[region[0]].Count == depth
        ? LabelAnswer.Column(region[0], depth)
        : LabelAnswer.Band(region, depth);

    private static LabelAnswer Pick<T>(
      List<T> found,
      LabelStep step,
      Func<T, int> first,
      Func<T, LabelAnswer> answer,
      IReadOnlyList<IReadOnlyList<string>> paths,
      string reached,
      IReadOnlyList<int> columns,
      int level)
    {
      if (step.Index is int index)
        return index >= 0 && index < found.Count
          ? answer(found[index])
          : LabelAnswer.Failed(found.Count == 0
            ? Missing(step, paths, reached, columns, level)
            : $"there {(found.Count == 1 ? "is" : "are")} {found.Count.ToString(CultureInfo.InvariantCulture)} \"{step.Name}\" under {reached} (0 to {(found.Count - 1).ToString(CultureInfo.InvariantCulture)}), so there is no {step}");

      if (found.Count == 1)
        return answer(found[0]);

      if (found.Count == 0)
        return LabelAnswer.Failed(Missing(step, paths, reached, columns, level));

      return LabelAnswer.Failed(
        $"\"{step.Name}\" is {found.Count.ToString(CultureInfo.InvariantCulture)} things under {reached}: "
        + string.Join(" and ", found.Select(one => Written(paths[first(one)])))
        + $"; say which by its path, or (\"{step.Name}\", 0) for the first");
    }

    private static string Missing(LabelStep step, IReadOnlyList<IReadOnlyList<string>> paths, string reached, IReadOnlyList<int> columns, int level)
    {
      var here = columns.Where(column => paths[column].Count > level).Select(column => paths[column][level]).Distinct().Select(name => $"\"{name}\"").ToList();

      return $"there is no \"{step.Name}\" under {reached}; " + (here.Count == 0 ? "nothing is" : "under it " + (here.Count == 1 ? "is " : "are ") + string.Join(", ", here));
    }

    /// <summary>A band's reach: every column from its first to its last, labelled or not.</summary>
    private static List<int> Span(IReadOnlyList<int> columns)
      => columns.Count == 0 ? new List<int>() : Enumerable.Range(columns[0], columns[columns.Count - 1] - columns[0] + 1).ToList();

    internal static string Written(IReadOnlyList<string> path) => "[" + string.Join(", ", path.Select(step => $"\"{step}\"")) + "]";

    private static string Count(int count, string noun) => $"{count.ToString(CultureInfo.InvariantCulture)} {noun}{(count == 1 ? string.Empty : "s")}";
  }
}
