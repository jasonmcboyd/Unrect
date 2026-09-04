using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Unrect.Core;
using Unrect.Shapes;

using static Unrect.Shapes.Shape;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// The retention family: how many bytes stay LIVE when a declaration's result is kept, which is the
  /// one question BenchmarkDotNet's <c>Allocated</c> column cannot answer.
  ///
  /// <para><b>Why this family is not a BenchmarkDotNet class.</b> Allocation and retention are
  /// different quantities and a duplicate string is the case that separates them: the duplicate is
  /// allocated by the reader before the adapter ever sees it, so a change that dedups at adaptation
  /// time removes nothing from <c>Allocated</c> and a great deal from the live set. Measuring the live
  /// set means holding exactly one result and collecting everything else — the opposite of what a
  /// benchmark engine does, which runs an operation thousands of times and keeps none of them. And the
  /// number is DETERMINISTIC: the same input retains the same bytes, so there is no distribution to
  /// estimate and nothing for warmup, unrolling or outlier detection to earn. One build, one forced
  /// full collection, one reading.</para>
  ///
  /// <para><b>How it still rides the same rails.</b> It is a matrix leg like every other family, it
  /// records its runner's CPU like every other leg, and it emits the same
  /// <c>{name, unit: "bytes", value}</c> shape the memory rows of every other family are already
  /// stored and charted as. Nothing about the ingest pipeline changes to carry it.</para>
  ///
  /// <para><b>What it exists to judge, and the three rows that must NOT move.</b> The subject is
  /// adapter-level value interning — repeated strings sharing one instance — and both doors reach their
  /// adapter for real: the eager rows read a genuine <c>.xlsx</c> through
  /// <c>SpreadsheetSpace.Create</c>, and the streaming rows go through <c>SheetStore</c>'s chunk fill,
  /// which every row source passes. A floor built on a locally-made <c>GridSpace</c> would bypass the
  /// eager adapter and read flat under the very change it exists to judge.</para>
  /// <list type="bullet">
  ///   <item><b>The floor</b> — <c>Eager_SpaceHeld</c>, <c>Eager_ResultHeld</c>,
  ///     <c>Streaming_ResultHeld</c>. These should fall.</item>
  ///   <item><b>The <c>_Unique</c> controls</b> — the same strings with nothing repeated, at identical
  ///     lengths and counts (<see cref="RetentionSpaces"/>), so today each reads the same as its twin
  ///     and afterwards it must not move. A duplicated row that fell while its control fell with it is
  ///     drift, not dedup.</item>
  ///   <item><b><c>Eager_SpaceHeld_Shared</c></b> — the same values from a shared-string-encoded file,
  ///     where ExcelDataReader hands back one instance per distinct value and has therefore already done
  ///     the interning. It is a control in the same sense, and it is also the TARGET: it prices the
  ///     floor's best case on the same cells and the same chart.</item>
  /// </list>
  ///
  /// <para><b>The protocol, which is the whole instrument.</b> Each scenario is built once and thrown
  /// away to warm the code paths and to let the fixture's transients settle; a baseline is read after a
  /// forced, blocking, compacting full collection with nothing held; the scenario is built again and
  /// the reading taken after the same collection with the result — and only the result — reachable.
  /// The difference is the live set of what was held. Everything a scenario builds and does not return
  /// (the grid under a projection, the reader and its string table, the window and its reader pool) is
  /// out of scope by the time the reading is taken, deliberately: "result held, source released" is the
  /// shape of the question a caller asks.</para>
  /// </summary>
  internal static class Retention
  {
    /// <summary>
    /// BenchmarkDotNet names a row <c>Namespace.Class.Method</c>, and the dashboard splits a row's
    /// class from its method on the first dot after that prefix. Emitting the same shape is what makes
    /// these rows read as one more family rather than as a handful of orphans.
    /// </summary>
    private const string RowPrefix = "Unrect.Benchmarks.Retention.";

    private const string Unit = "bytes";

    private static readonly IShape<IReadOnlyList<LedgerRow>> Ledger = TableRows<LedgerRow>();

    /// <summary>
    /// The scenarios, in the order a reader should meet them: what the grid costs, what the same grid
    /// costs when nothing repeats, what the projection off that grid costs, what the projection costs
    /// with no grid under it at all, and that last one's control.
    /// </summary>
    private static readonly (string Name, bool Unique, string What, Func<int, object> Build)[] Scenarios =
    {
      ("Eager_SpaceHeld", false, "SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held",
        rows => RetentionSpaces.EagerSpace(unique: false, sharedStrings: false, rows)),

      ("Eager_SpaceHeld_Unique", true, "CONTROL — the same file and reader, every text distinct",
        rows => RetentionSpaces.EagerSpace(unique: true, sharedStrings: false, rows)),

      ("Eager_SpaceHeld_Shared", false, "CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups",
        rows => RetentionSpaces.EagerSpace(unique: false, sharedStrings: true, rows)),

      ("Eager_ResultHeld", false, "TableRows over the eager grid; result held, grid released",
        rows => EagerResult(unique: false, rows)),

      ("Streaming_ResultHeld", false, "TableRows through a window; result held, workbook closed",
        rows => StreamingResult(unique: false, rows)),

      ("Streaming_ResultHeld_Unique", true, "CONTROL — the same projection, every text distinct",
        rows => StreamingResult(unique: true, rows)),
    };

    /// <summary>
    /// Runs every scenario and writes the results where the workflow stores them from. Throws — and so
    /// fails the leg — if a scenario measured something other than what it claims to; see
    /// <see cref="Inspect"/>.
    /// </summary>
    public static void Run(string[] args)
    {
      var artifacts = Argument(args, "--artifacts") ?? Path.Combine(".", "artifacts", "Retention");
      var repeats = Number(args, "--repeats") ?? 3;

      // A local probing knob only. A reading at any other size is not comparable to the trend line, so
      // one is printed and deliberately NOT written: there is no way to publish a number this job
      // itself considers incomparable.
      var rows = Number(args, "--rows") ?? RetentionSpaces.Rows;
      var publishable = rows == RetentionSpaces.Rows;

      Console.WriteLine("Retention — live bytes with the result held (deterministic, one-shot).");
      Console.WriteLine(FormattableString.Invariant(
        $"Fixture: {rows:N0} rows x {RetentionSpaces.Columns} columns, five text columns; {repeats} reading(s) per scenario, median reported."));

      // Both workbooks up front, so the generation cost is reported where it is paid rather than
      // landing inside whichever eager scenario happened to run first.
      var generating = Stopwatch.StartNew();

      foreach (var (unique, shared) in new[] { (false, false), (true, false), (false, true) })
      {
        var path = RetentionWorkbooks.Path(unique, shared, rows, RetentionSpaces.Columns);

        Console.WriteLine(FormattableString.Invariant(
          $"Eager fixture: {path} ({new FileInfo(path).Length / 1024d / 1024d:N1} MB)"));
      }

      Console.WriteLine(FormattableString.Invariant($"Fixtures ready in {generating.Elapsed.TotalSeconds:N1}s."));
      Console.WriteLine();

      var results = new List<Row>();

      foreach (var scenario in Scenarios)
      {
        Warm(scenario.Name, scenario.Unique, rows, scenario.Build);

        var readings = new long[repeats];

        for (var i = 0; i < repeats; i++)
          readings[i] = Reading(scenario.Build, rows);

        Array.Sort(readings);

        var median = readings[readings.Length / 2];
        var spread = readings[readings.Length - 1] - readings[0];

        results.Add(new Row(
          RowPrefix + scenario.Name,
          Unit,
          median,
          FormattableString.Invariant($"± {spread:N0} {Unit}"),
          FormattableString.Invariant($"median of {repeats} · {scenario.What}")));

        Console.WriteLine(FormattableString.Invariant(
          $"  {scenario.Name,-30} {Megabytes(median),10} MB   ({median:N0} bytes, spread {spread:N0})"));
        Console.WriteLine(FormattableString.Invariant($"  {string.Empty,-30} {scenario.What}"));
        Console.WriteLine();
      }

      if (!publishable)
      {
        Console.WriteLine(FormattableString.Invariant(
          $"NOT WRITTEN: --rows {rows} is not the family's fixture size ({RetentionSpaces.Rows:N0}), so these readings are not comparable to the trend line."));

        return;
      }

      Console.WriteLine("Wrote " + Write(artifacts, results));
    }

    /// <summary>
    /// One reading: baseline with nothing held, then the same measurement with the scenario's result
    /// and nothing else reachable. Not inlined, so the reference the scenario returns cannot outlive
    /// this frame in a caller's stack slot and inflate the NEXT scenario's baseline.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Reading(Func<int, object> build, int rows)
    {
      Collect();

      var before = GC.GetTotalMemory(forceFullCollection: true);
      var held = build(rows);

      Collect();

      var after = GC.GetTotalMemory(forceFullCollection: true);

      // The reading is taken while `held` is still reachable; this is what says so to the JIT, which is
      // otherwise free to consider the local dead the moment `build` returned.
      GC.KeepAlive(held);

      return after - before;
    }

    /// <summary>
    /// A discarded build before the first reading. Two jobs, both load-bearing: it JITs everything a
    /// measured build will run (a first-call compilation retains its own bytes, and they would land in
    /// whichever scenario ran first), and it is where the scenario is checked for having produced what
    /// it claims — the retention analogue of the rig's rule that a new benchmark's OUTPUT is verified
    /// and not just its number.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Warm(string name, bool unique, int rows, Func<int, object> build)
    {
      var warm = build(rows);

      Inspect(name, unique, rows, warm);

      GC.KeepAlive(warm);
    }

    /// <summary>
    /// What the scenario actually holds, checked and reported. The check is that the fixture has the
    /// duplication it is supposed to have — a fixture that quietly stopped repeating would make the
    /// interning change look like it did nothing, and the number would be perfectly plausible.
    /// <para>
    /// The instance count is REPORTED and never asserted: it is 250k today because every equal string
    /// is a separate object, and driving it to the distinct-value count is precisely what the change
    /// this family exists to judge will do. An assertion on it would fail the moment the change landed.
    /// </para>
    /// </summary>
    private static void Inspect(string name, bool unique, int rows, object held)
    {
      var clients = new List<string>();

      switch (held)
      {
        case ISpace space:
          if (space.Area.Height != rows || space.Area.Width != RetentionSpaces.Columns)
            throw new InvalidOperationException(
              FormattableString.Invariant($"{name}: expected a {rows}x{RetentionSpaces.Columns} grid, got {space.Area.Height}x{space.Area.Width}."));

          for (var row = 1; row < rows; row++)
            clients.Add(space[0, row].GetString());

          break;

        case IReadOnlyList<LedgerRow> projected:
          if (projected.Count != rows - 1)
            throw new InvalidOperationException(
              FormattableString.Invariant($"{name}: expected {rows - 1} projected rows, got {projected.Count}."));

          for (var row = 0; row < projected.Count; row++)
            clients.Add(projected[row].Client);

          break;

        default:
          throw new InvalidOperationException($"{name}: nothing knows how to inspect a {held.GetType()}.");
      }

      var values = new HashSet<string>(StringComparer.Ordinal);
      var instances = new HashSet<object>(ReferenceEqualityComparer.Instance);

      foreach (var client in clients)
      {
        values.Add(client);
        instances.Add(client);
      }

      var expected = unique ? rows - 1 : RetentionSpaces.DistinctClients;

      if (values.Count != expected)
        throw new InvalidOperationException(
          FormattableString.Invariant($"{name}: expected {expected} distinct Client values, got {values.Count} — the fixture is not the fixture this family measures."));

      Console.WriteLine(FormattableString.Invariant(
        $"  {name,-30} fidelity: {clients.Count:N0} Client cells, {values.Count:N0} distinct values held as {instances.Count:N0} instances"));
    }

    /// <summary>
    /// The eager door's projection, with the grid released. The grid is a local and its last use is the
    /// map, so it is unreachable before this returns and certainly before the reading's collection —
    /// which is the point of the row: what a caller keeps after parsing, not what parsing needed.
    /// </summary>
    private static object EagerResult(bool unique, int rows) =>
      Ledger.Map(RetentionSpaces.EagerSpace(unique, sharedStrings: false, rows));

    /// <summary>
    /// The streaming door's projection, with the pool disposed and the window gone: the same result,
    /// arrived at without ever materialising the sheet.
    /// </summary>
    private static object StreamingResult(bool unique, int rows)
    {
      using var pool = RetentionSpaces.Pool(unique, rows: rows);

      return Ledger.Map(RetentionSpaces.Windowed(pool, rows: rows));
    }

    /// <summary>
    /// A forced, blocking, COMPACTING collection of every generation, twice with finalizers drained in
    /// between — a first pass can queue finalizers that a second pass is needed to actually free.
    /// Compacting matters because the reading is a heap size and a fragmented heap reports bytes no
    /// object is using.
    /// </summary>
    private static void Collect()
    {
      for (var pass = 0; pass < 2; pass++)
      {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
      }

      GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static string Write(string artifacts, List<Row> results)
    {
      var directory = Path.Combine(artifacts, "results");

      Directory.CreateDirectory(directory);

      var path = Path.Combine(directory, "retention-benchmarks.json");

      File.WriteAllText(
        path,
        JsonSerializer.Serialize(
          results,
          new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

      return path;
    }

    private static int? Number(string[] args, string name) =>
      int.TryParse(Argument(args, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
        ? value
        : (int?)null;

    private static string? Argument(string[] args, string name)
    {
      for (var i = 0; i < args.Length - 1; i++)
        if (string.Equals(args[i], name, StringComparison.Ordinal))
          return args[i + 1];

      return null;
    }

    private static string Megabytes(long bytes) =>
      (bytes / 1024d / 1024d).ToString("N1", CultureInfo.InvariantCulture);

    /// <summary>
    /// One row of the <c>customSmallerIsBetter</c> document the workflow stores — the same shape every
    /// family's memory rows are already published in, whose schema is lowercase
    /// (<c>name</c>/<c>unit</c>/<c>value</c>, with <c>range</c> and <c>extra</c> optional). Every
    /// member here is one word, so the camel-case policy at the serializer spells all five exactly.
    /// </summary>
    private sealed class Row
    {
      internal Row(string name, string unit, long value, string range, string extra)
      {
        Name = name;
        Unit = unit;
        Value = value;
        Range = range;
        Extra = extra;
      }

      public string Name { get; }

      public string Unit { get; }

      public long Value { get; }

      public string Range { get; }

      public string Extra { get; }
    }
  }
}
