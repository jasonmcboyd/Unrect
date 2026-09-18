using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// One reading of one declaration over one space, split into the facets an equivalence law is
  /// allowed to name. It exists so a law can be stated at the level it actually holds at rather
  /// than at the level one hopes for: L1 (value and failure classification), L2 (L1 plus what was
  /// consumed and where), L3 (L2 plus the diagnostics and the failure's path and subject).
  /// <para>
  /// Generalized by copy from the comparator at the bottom of
  /// <c>Projections/LazyDenotationTests</c> — the tree's strongest existing differential machinery,
  /// which compares one declaration read two ways. That suite is a finished record and is left
  /// alone; the mild duplication between it and this file is accepted, because a law suite wants to
  /// name a <em>level</em> and the lazy sweep wants to compare everything at once.
  /// </para>
  /// <para>
  /// <strong>The one rule for negative pins.</strong> A law that stops holding at some level is
  /// pinned by asserting the SPECIFIC difference — <c>Assert.Contains("Under", failure.Path)</c>,
  /// <c>Assert.NotEqual(expected.Consumed, actual.Consumed)</c> with both spelled out — never by a
  /// blanket "these differ somewhere". There is deliberately no <c>AssertNotL3</c>: a test that
  /// only says "not identical" would keep passing after the difference it was written for had been
  /// replaced by a different one.
  /// </para>
  /// </summary>
  internal sealed class Observation
  {
    internal Observation(
      string value,
      string consumed,
      string offset,
      string advance,
      IReadOnlyList<string> diagnostics,
      string? failure,
      string? failureSubject,
      string? failurePath)
    {
      Value = value;
      Consumed = consumed;
      Offset = offset;
      Advance = advance;
      Diagnostics = diagnostics;
      Failure = failure;
      FailureSubject = failureSubject;
      FailurePath = failurePath;
    }

    /// <summary>
    /// The projected value, rendered deeply — a list by its elements and a record by its properties,
    /// all the way down, so a difference anywhere inside a result is a difference in this facet. See
    /// <see cref="Observations.RenderValue(object?)"/> for why the second half is not optional.
    /// </summary>
    public string Value { get; }

    /// <summary>How much of its own extent the projection used, as "WxH".</summary>
    public string Consumed { get; }

    /// <summary>Where the placement resolved to, as "WxH" — L2's other half.</summary>
    public string Offset { get; }

    /// <summary>
    /// What a following sibling must step past: offset plus consumed. Distinct from <see
    /// cref="Consumed"/> on purpose — the offset-on-a-flow law is the case where two spellings agree
    /// on the advance and disagree on how it was split.
    /// </summary>
    public string Advance { get; }

    /// <summary>What the parse noticed, in order — order is part of the claim, so this is L3.</summary>
    public IReadOnlyList<string> Diagnostics { get; }

    /// <summary>
    /// The failure's L1 identity, or null where the declaration succeeded: the problem itself, the
    /// cell it was looking at, whether it was a fault, and what it wrapped.
    /// <para>
    /// Deliberately WITHOUT the path and the subject. A modifier that may change only diagnostics —
    /// <c>.Named</c> is the whole family — changes both of those and nothing else, so leaving them
    /// in would make L1 unstatable for exactly the laws that need it most. They are compared at L3,
    /// where they belong: a path is what a user reads, not what a declaration means.
    /// </para>
    /// </summary>
    public string? Failure { get; }

    /// <summary>The failing projection's label, or null where the declaration succeeded — L3.</summary>
    public string? FailureSubject { get; }

    /// <summary>The declaration path that reached the failure, or null — L3.</summary>
    public string? FailurePath { get; }
  }

  /// <summary>
  /// How a differential law suite reads a declaration and how it compares two readings. See <see
  /// cref="Observation"/> for what the levels mean and for the negative-pin rule.
  /// </summary>
  internal static class Observations
  {
    /// <summary>
    /// Reads <paramref name="projection"/> over <paramref name="space"/> and records everything a
    /// caller can observe about the reading.
    /// <para>
    /// Two entry points because they answer different halves of the question: the value and what the
    /// parse noticed come from <c>MapWithDiagnostics</c>, the offset and the extent consumed from
    /// <c>Apply</c>. A projection is a value safe to apply twice, so this is one reading asked about
    /// twice rather than two readings.
    /// </para>
    /// </summary>
    public static Observation Observe<TSpace, T>(IProjectionDefinition<TSpace, T> projection, TSpace space)
      where TSpace : class, ISpace
    {
      try
      {
        var mapped = projection.MapWithDiagnostics(space);
        var applied = projection.Apply(space);

        return new Observation(
          RenderValue(mapped.Value),
          Render(applied.Consumed),
          Render(applied.Offset.Size),
          Render(applied.Advance),
          mapped.Diagnostics.Select(Describe).ToList(),
          null,
          null,
          null);
      }
      catch (ProjectionException failure)
      {
        return new Observation(
          "<threw>",
          "<threw>",
          "<threw>",
          "<threw>",
          Array.Empty<string>(),
          Describe(failure),
          failure.Subject,
          failure.Path);
      }
    }

    /// <summary>
    /// The same reading through the push interpreter: the machine the definition builds, driven
    /// over the space's rows. What phase 4 runs the whole suite against; here, what the
    /// equivalence theories compare to <see cref="Observe{TSpace, T}"/>.
    /// </summary>
    public static Observation ObservePush<TSpace, T>(IProjectionDefinition<TSpace, T> projection, TSpace space)
      where TSpace : class, ISpace
    {
      try
      {
        var context = ProjectionContext.Root(space);
        var mark = context.Diagnostics.Mark();
        var extent = Plane<TSpace>.Of(space);
        var applied = PushSession<TSpace>.Apply(projection, space, context);

        if (!(applied.Advance.Width == 0 && applied.Advance.Height == 0 && context.Diagnostics.AbsorbedAt(mark)))
          ProjectionExtensions.ReportUnconsumed(projection, extent, applied.Offset.Size, applied.Consumed, context);

        return new Observation(
          RenderValue(applied.Value),
          Render(applied.Consumed),
          Render(applied.Offset.Size),
          Render(applied.Advance),
          context.Diagnostics.Snapshot().Select(Describe).ToList(),
          null,
          null,
          null);
      }
      catch (ProjectionException failure)
      {
        return new Observation(
          "<threw>",
          "<threw>",
          "<threw>",
          "<threw>",
          Array.Empty<string>(),
          Describe(failure),
          failure.Subject,
          failure.Path);
      }
    }

    /// <summary>L1: the same value, or the same failure said in the same words about the same cell.</summary>
    public static void AssertL1(Observation expected, Observation actual)
    {
      Facet("value", expected.Value, actual.Value);
      Facet("failure", expected.Failure, actual.Failure);
    }

    /// <summary>L2: L1, plus the same extent consumed from the same place, and the same advance.</summary>
    public static void AssertL2(Observation expected, Observation actual)
    {
      AssertL1(expected, actual);

      Facet("consumed", expected.Consumed, actual.Consumed);
      Facet("offset", expected.Offset, actual.Offset);
      Facet("advance", expected.Advance, actual.Advance);
    }

    /// <summary>L3: L2, plus what a user reads — the diagnostics in order, and the failure's label and path.</summary>
    public static void AssertL3(Observation expected, Observation actual)
    {
      AssertL2(expected, actual);

      Facet("failure subject", expected.FailureSubject, actual.FailureSubject);
      Facet("failure path", expected.FailurePath, actual.FailurePath);

      Assert.Equal(expected.Diagnostics, actual.Diagnostics);
    }

    /// <summary>
    /// Compares one facet by name, so a failure reads "consumed: 3x2 != consumed: 3x3" rather than
    /// leaving the reader to work out which of eight strings moved.
    /// </summary>
    private static void Facet(string name, string? expected, string? actual)
      => Assert.Equal($"{name}: {expected ?? "<none>"}", $"{name}: {actual ?? "<none>"}");

    private static string Render(Size size) => $"{size.Width}x{size.Height}";

    /// <summary>
    /// A value spelled out far enough that the value facet can see a difference anywhere inside it.
    /// <para>
    /// <strong>Why this is not just <c>ToString</c>.</strong> A record's compiler-written
    /// <c>ToString</c> prints each member through that member's own <c>ToString</c>, so a record whose
    /// field is a list prints the list as its type name — <c>Rows = System.Collections.Generic.List`1[…]</c>
    /// — and two readings whose rows differ compare EQUAL. That was found by perturbation, not by
    /// reading: a twin declaration built with <c>Right(5)</c> where the original had <c>Right(6)</c>
    /// passed an L3 comparison of the acceptance corpus's records. So a value that is not a sequence
    /// and does not speak for itself is rendered from its own public readable properties, recursively.
    /// </para>
    /// <para>
    /// <strong>What still speaks for itself.</strong> A type carrying a HAND-WRITTEN
    /// <c>ToString</c> override keeps it, because that override is nearly always more informative than
    /// its properties are: <c>Cell</c> prints the number in the cell, while its three public
    /// properties (<c>Kind</c>, <c>IsBlank</c>, <c>HasValue</c>) would print everything about it except
    /// the value. The line between the two is drawn at <see cref="CompilerGeneratedAttribute"/>, which
    /// is exactly what a record's synthesized <c>ToString</c> and an anonymous type both carry, so the
    /// rule is: nobody wrote a sentence for this type, therefore spell out its parts.
    /// </para>
    /// </summary>
    private static string RenderValue(object? value) => RenderValue(value, new Ancestry(), 0);

    /// <summary>The maximum object nesting rendered before the renderer says so and stops.</summary>
    private const int MaxDepth = 12;

    private static string RenderValue(object? value, Ancestry ancestors, int depth) => value switch
    {
      null => "<null>",
      string text => text,
      IEnumerable items => RenderSequence(items, ancestors, depth),
      IConvertible => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
      _ when SpeaksForItself(value.GetType()) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
      _ => RenderObject(value, ancestors, depth),
    };

    private static string RenderSequence(IEnumerable items, Ancestry ancestors, int depth)
    {
      if (!ancestors.Enter(items, depth, out var refusal))
      {
        return refusal;
      }

      try
      {
        return "[" + string.Join(", ", items.Cast<object?>().Select(item => RenderValue(item, ancestors, depth + 1))) + "]";
      }
      finally
      {
        ancestors.Leave(items);
      }
    }

    /// <summary>
    /// <c>TypeName { Prop = …, … }</c> over the public readable instance properties, in declaration
    /// order (metadata order, which is stable for a given build — the two sides of a comparison are
    /// always the same type read twice, so ordering is a readability choice, not a correctness one).
    /// An anonymous type drops the compiler's name and renders as a bare <c>{ … }</c>.
    /// </summary>
    private static string RenderObject(object value, Ancestry ancestors, int depth)
    {
      if (!ancestors.Enter(value, depth, out var refusal))
      {
        return refusal;
      }

      try
      {
        var type = value.GetType();

        var properties = type
          .GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
          .OrderBy(property => property.MetadataToken)
          .Select(property => $"{property.Name} = {RenderProperty(property, value, ancestors, depth)}")
          .ToList();

        var name = IsAnonymous(type) ? string.Empty : FriendlyName(type) + " ";

        return properties.Count == 0
          ? name + "{ }"
          : name + "{ " + string.Join(", ", properties) + " }";
      }
      finally
      {
        ancestors.Leave(value);
      }
    }

    /// <summary>
    /// A getter that throws is reported as having thrown rather than being allowed to fail the
    /// comparison from inside the renderer — the harness's job is to describe a reading, not to add a
    /// second way for one to fail.
    /// </summary>
    private static string RenderProperty(PropertyInfo property, object value, Ancestry ancestors, int depth)
    {
      try
      {
        return RenderValue(property.GetValue(value), ancestors, depth + 1);
      }
      catch (Exception thrown)
      {
        return $"<threw {(thrown as TargetInvocationException)?.InnerException?.GetType().Name ?? thrown.GetType().Name}>";
      }
    }

    /// <summary>
    /// Whether the type carries a <c>ToString</c> a human wrote. <see cref="object"/>'s and <see
    /// cref="ValueType"/>'s do not count (they print the type name), and neither does a synthesized
    /// one — a record's, or an anonymous type's.
    /// </summary>
    private static bool SpeaksForItself(Type type)
    {
      var toString = type.GetMethod(nameof(ToString), BindingFlags.Public | BindingFlags.Instance, binder: null, types: Type.EmptyTypes, modifiers: null);

      return toString is not null
        && toString.DeclaringType != typeof(object)
        && toString.DeclaringType != typeof(ValueType)
        && !toString.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
        && !IsAnonymous(type);
    }

    private static bool IsAnonymous(Type type)
      => type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
      && type.IsGenericType
      && type.Name.Contains("AnonymousType");

    private static string FriendlyName(Type type)
    {
      if (!type.IsGenericType)
      {
        return type.Name;
      }

      var name = type.Name.Substring(0, type.Name.IndexOf('`'));

      return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FriendlyName))}>";
    }

    /// <summary>
    /// The chain of values currently being rendered, so a value that contains itself renders as
    /// <c>&lt;cycle&gt;</c> instead of recursing forever. Ancestry, not history: a value is removed
    /// when its rendering finishes, so the SAME object appearing twice as siblings renders twice.
    /// </summary>
    private sealed class Ancestry
    {
      private readonly HashSet<object> _open = new HashSet<object>(ReferenceComparer.Instance);

      public bool Enter(object value, int depth, out string refusal)
      {
        if (depth >= MaxDepth)
        {
          refusal = "<depth>";
          return false;
        }

        if (!_open.Add(value))
        {
          refusal = "<cycle>";
          return false;
        }

        refusal = string.Empty;
        return true;
      }

      public void Leave(object value) => _open.Remove(value);

      private sealed class ReferenceComparer : IEqualityComparer<object>
      {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();

        public new bool Equals(object? first, object? second) => ReferenceEquals(first, second);

        public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
      }
    }

    private static string Describe(ProjectionDiagnostic diagnostic)
      => $"{diagnostic.Severity} {diagnostic.Subject} at {diagnostic.Location.A1} in {diagnostic.Path}: {diagnostic.Message}";

    /// <summary>
    /// The failure's L1 identity. <c>Problem</c> rather than <c>Message</c> because the message
    /// template embeds the path and the subject, which are L3 — see <see cref="Observation.Failure"/>.
    /// </summary>
    private static string Describe(ProjectionException failure)
      => $"{failure.Problem} | at={failure.Location} "
       + $"| fault={failure.IsFault} | inner={failure.InnerException?.GetType().Name ?? "<none>"}";
  }
}
