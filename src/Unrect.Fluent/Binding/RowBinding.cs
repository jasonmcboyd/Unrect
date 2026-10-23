using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Fluent
{
  /// <summary>
  /// The plan for turning a table row into a <c>T</c>: which members exist, what caption and CLR
  /// type each carries, and a compiled materializer. Everything reflective happens once, at projection
  /// construction — no reflection runs per <c>Map</c> and none per row, so the projection stays
  /// immutable and safe to apply to many workbooks at once.
  /// <para>
  /// A plan is also the hand door to the reflective table: <see cref="Create{TSpace}"/> matches a
  /// type's members to captions once, and <see cref="Project{TSpace}(LabelMap)"/> is the record
  /// projection a table's bind rung applies to every row.
  /// </para>
  /// </summary>
  /// <typeparam name="T">The record type the plan fills.</typeparam>
  public sealed class RowBinding<T>
  {
    private readonly Func<object?[], T> _materialize;

    private RowBinding(IReadOnlyList<MemberPlan> members, Func<object?[], T> materialize)
    {
      Members = members;
      _materialize = materialize;
    }

    /// <summary>The members that must find a caption, in materialization order.</summary>
    internal IReadOnlyList<MemberPlan> Members { get; }

    internal T Materialize(object?[] values) => _materialize(values);

    /// <summary>
    /// The plan projected over one row's labels: the record projection the reflective table applies
    /// per row. The captions are resolved here, once per application of the table — each member is
    /// placed at the column its caption names and read as the member's own type declares — and every
    /// member that found no column is reported together, naming the table's captions.
    /// <para>Fluent: <c>Table&lt;T&gt;()</c> — the whole table; <c>Record&lt;T&gt;(labels)</c> — one row.</para>
    /// <para>
    /// By hand, the table the fluent rung composes is the bind node over a header and this projection:
    /// <code>
    /// var plan = RowBinding&lt;Line&gt;.Create&lt;ICellSpace&gt;(null, typeof(ICellSpace));
    /// new LabelledDefinition&lt;ICellSpace, IReadOnlyList&lt;Line&gt;&gt;(
    ///   LabelAxis.Column,
    ///   new ColumnLabelsDefinition&lt;ICellSpace&gt;(1, Placement&lt;ICellSpace&gt;.Default),
    ///   labels =&gt; new BandsDefinition&lt;ICellSpace, Line&gt;(plan.Project&lt;ICellSpace&gt;(labels), Orientation.Vertical, 1, default, null, Placement&lt;ICellSpace&gt;.Default),
    ///   opacity, Placement&lt;ICellSpace&gt;.Default)
    /// </code>
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space the row is read over; the one the plan was created for.</typeparam>
    /// <param name="labels">The header's captions, as the bind rung hands them to its row.</param>
    public IProjectionDefinition<TSpace, T> Project<TSpace>(LabelMap labels)
      where TSpace : class, ISpace
      => Project<TSpace>(labels ?? throw new ArgumentNullException(nameof(labels)), loose: false);

    /// <summary>
    /// <see cref="Project{TSpace}(LabelMap)"/> with the one strictness the exploratory table
    /// switches: a member no column binds is left at its default and said so in a Warning, and the
    /// columns no member reads are listed in an Info.
    /// </summary>
    internal IProjectionDefinition<TSpace, T> Project<TSpace>(LabelMap labels, bool loose)
      where TSpace : class, ISpace
      => RecordRows.Project<TSpace, T>(this, labels, loose);

    /// <summary>
    /// The plan for filling a <typeparamref name="T"/> from a labelled row, resolved once here:
    /// every member's caption, CLR type and reading, and a compiled materializer. A plan is immutable
    /// and reusable, so one serves every row of every application of a table.
    /// </summary>
    /// <typeparam name="TSpace">The space the record is declared over.</typeparam>
    /// <param name="binding">The per-member declarations, or null for what reflection writes.</param>
    /// <param name="space">
    /// The space the record is declared over, and which point type a member may therefore ask for:
    /// it must be <c>typeof(TSpace)</c> of the <c>RecordRow&lt;TSpace, T&gt;</c> this plan is used
    /// by. A point is invariant in its space, so a plan checked against one space and read through
    /// another would refuse the member it had just accepted.
    /// </param>
    /// <returns>The plan, or an <see cref="ArgumentException"/> where a member cannot be filled: no public constructor, a type no reading yields, a declaration naming a member the type has not got.</returns>
    public static RowBinding<T> Create<TSpace>(TableBinding<TSpace, T>? binding, Type space)
      where TSpace : class, ISpace
    {
      var readings = new Dictionary<string, Func<object, object?>>(StringComparer.Ordinal);

      foreach (var reading in binding?.Readings ?? new Dictionary<string, Func<TableRow<TSpace>, object?>>())
      {
        var read = reading.Value;

        readings.Add(reading.Key, row => read((TableRow<TSpace>)row));
      }

      var captions = binding?.Captions ?? new Dictionary<string, string>(StringComparer.Ordinal);
      var positions = binding?.Positions ?? new Dictionary<string, int>(StringComparer.Ordinal);
      var ignored = new HashSet<string>(binding?.Ignored ?? Array.Empty<string>(), StringComparer.Ordinal);

      var paths = binding?.Paths ?? new Dictionary<string, LabelStep[]>(StringComparer.Ordinal);

      foreach (var name in captions.Keys.Concat(positions.Keys).Concat(readings.Keys).Concat(paths.Keys))
        if (ignored.Contains(name))
          throw new ArgumentException($"{typeof(T).Name}.{name} is both bound and ignored.", nameof(binding));

      // A single parameterized constructor and no parameterless one is the record case, and it is
      // checked first so a positional record binds through its constructor even though its
      // properties look settable.
      var constructors = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
      var parameterless = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);

      if (constructors.Length == 0)
        throw new ArgumentException($"{typeof(T).Name} cannot be constructed: it has no public constructor.");

      if (parameterless is null && constructors.Length > 1)
        throw new ArgumentException(
          $"{typeof(T).Name} cannot be constructed: it has {constructors.Length} public constructors and no parameterless one. "
          + "Give it one constructor, or a parameterless constructor and settable properties.");

      return parameterless is null
        ? FromConstructor(constructors[0], captions, positions, readings, paths, ignored, space)
        : FromProperties(parameterless, captions, positions, readings, paths, ignored, space);
    }

    private static RowBinding<T> FromConstructor(
      ConstructorInfo constructor,
      IReadOnlyDictionary<string, string> captions,
      IReadOnlyDictionary<string, int> positions,
      IReadOnlyDictionary<string, Func<object, object?>> readings,
      IReadOnlyDictionary<string, LabelStep[]> paths,
      ISet<string> ignored,
      Type space)
    {
      var parameters = constructor.GetParameters();

      // Unreachable from Create, which routes a parameterless constructor to the property path;
      // kept so this method is correct read on its own rather than only in context.
      if (parameters.Length == 0)
        throw new ArgumentException($"{typeof(T).Name} has no properties to bind.");

      // Selectors name properties; positional records name the parameter identically, and a
      // hand-written constructor differs only in case.
      string Declared(ParameterInfo parameter)
        => captions.FirstOrDefault(c => CaptionComparer.Default.Equals(c.Key, parameter.Name!)).Value
           ?? parameter.Name!;

      int? Position(ParameterInfo parameter)
        => positions.Where(p => CaptionComparer.Default.Equals(p.Key, parameter.Name!)).Select(p => (int?)p.Value).FirstOrDefault();

      Func<object, object?>? Reading(ParameterInfo parameter)
        => readings.Where(r => CaptionComparer.Default.Equals(r.Key, parameter.Name!)).Select(r => r.Value).FirstOrDefault();

      LabelStep[]? Path(ParameterInfo parameter)
        => paths.Where(p => CaptionComparer.Default.Equals(p.Key, parameter.Name!)).Select(p => p.Value).FirstOrDefault();

      bool Ignored(ParameterInfo parameter)
        => ignored.Any(name => CaptionComparer.Default.Equals(name, parameter.Name!));

      Verify(captions.Keys.Concat(positions.Keys).Concat(readings.Keys).Concat(paths.Keys), ignored, parameters.Select(p => p.Name!), viaConstructor: true);

      var members = new List<MemberPlan>();
      var values = Expression.Parameter(typeof(object?[]), "values");
      var arguments = new List<Expression>();

      foreach (var parameter in parameters)
      {
        if (Ignored(parameter))
        {
          if (!parameter.HasDefaultValue)
            throw new ArgumentException(
              $"{typeof(T).Name}.{parameter.Name} cannot be ignored: the constructor parameter has no default value.");

          arguments.Add(Expression.Constant(parameter.DefaultValue, parameter.ParameterType));
          continue;
        }

        var index = members.Count;

        // Named for the PROPERTY, not the parameter: a hand-written constructor takes camelCase
        // parameters, and guidance reading Column(t => t.date, "…") would not compile.
        members.Add(Reading(parameter) is Func<object, object?> reading
          ? new MemberPlan(PropertyName(parameter.Name!), Declared(parameter), parameter.ParameterType, blankTolerant: false, reading: reading)
          : Plan(PropertyName(parameter.Name!), Declared(parameter), parameter.ParameterType,
            () => NullableAnnotations.IsAnnotatedNullable(parameter), space).At(Position(parameter)).Via(Path(parameter)));

        arguments.Add(Expression.Convert(
          Expression.ArrayIndex(values, Expression.Constant(index)), parameter.ParameterType));
      }

      var materialize = Expression
        .Lambda<Func<object?[], T>>(Expression.New(constructor, arguments), values)
        .Compile();

      return new RowBinding<T>(members, materialize);
    }

    private static RowBinding<T> FromProperties(
      ConstructorInfo parameterless,
      IReadOnlyDictionary<string, string> captions,
      IReadOnlyDictionary<string, int> positions,
      IReadOnlyDictionary<string, Func<object, object?>> readings,
      IReadOnlyDictionary<string, LabelStep[]> paths,
      ISet<string> ignored,
      Type space)
    {
      // A property with a setter of any accessibility is bindable — init, set, private set — since
      // the materialiser is a compiled expression and reaches a private setter as a mapper should.
      // A get-only property is invisible to the binder and is never required.
      var settable = typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetSetMethod(nonPublic: true) is not null && p.GetIndexParameters().Length == 0)
        .ToArray();

      if (settable.Length == 0)
        throw new ArgumentException($"{typeof(T).Name} has no properties to bind.");

      Verify(captions.Keys.Concat(positions.Keys).Concat(readings.Keys).Concat(paths.Keys), ignored, settable.Select(p => p.Name), viaConstructor: false);

      var members = new List<MemberPlan>();
      var values = Expression.Parameter(typeof(object?[]), "values");
      var bindings = new List<MemberBinding>();

      foreach (var property in settable)
      {
        if (ignored.Contains(property.Name))
          continue;

        var index = members.Count;

        members.Add(readings.TryGetValue(property.Name, out var reading)
          ? new MemberPlan(property.Name, property.Name, property.PropertyType, blankTolerant: false, reading: reading)
          : Plan(property.Name, captions.TryGetValue(property.Name, out var caption) ? caption : property.Name,
            property.PropertyType, () => NullableAnnotations.IsAnnotatedNullable(property), space)
            .At(positions.TryGetValue(property.Name, out var position) ? position : (int?)null)
            .Via(paths.TryGetValue(property.Name, out var path) ? path : null));

        // Expression.Bind accepts an init-only property: the modreq is a compile-time signal, and
        // the setter is an ordinary setter in metadata.
        bindings.Add(Expression.Bind(property, Expression.Convert(
          Expression.ArrayIndex(values, Expression.Constant(index)), property.PropertyType)));
      }

      var materialize = Expression
        .Lambda<Func<object?[], T>>(Expression.MemberInit(Expression.New(parameterless), bindings), values)
        .Compile();

      return new RowBinding<T>(members, materialize);
    }

    /// <summary>
    /// Every declared override and ignore must name something this path can fill. On the property
    /// path that is any settable property; on the constructor path it is a constructor parameter,
    /// and a real property that is not one cannot be bound there — an extra <c>init</c> property
    /// beside a positional record's parameters is the case that reaches this.
    /// </summary>
    private static void Verify(
      IEnumerable<string> bound,
      IEnumerable<string> ignored,
      IEnumerable<string> members,
      bool viaConstructor)
    {
      var known = members.ToArray();

      foreach (var name in bound.Concat(ignored))
      {
        if (known.Any(member => CaptionComparer.Default.Equals(member, name)))
          continue;

        var exists = typeof(T)
          .GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .Any(property => CaptionComparer.Default.Equals(property.Name, name));

        throw new ArgumentException(
          exists && viaConstructor
            ? $"{typeof(T).Name}.{name} is not a constructor parameter, so it cannot be bound or ignored; "
              + $"{typeof(T).Name} is built through its constructor, which fills only its parameters."
            : $"{typeof(T).Name} has no property named {name}.");
      }
    }

    /// <summary>The property a constructor parameter fills, so messages name what a user can type.</summary>
    private static string PropertyName(string parameter)
      => typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .FirstOrDefault(property => CaptionComparer.Default.Equals(property.Name, parameter))
        ?.Name ?? parameter;

    private static MemberPlan Plan(string name, string caption, Type type, Func<bool> annotatedNullable, Type space)
    {
      var underlying = Nullable.GetUnderlyingType(type);
      var effective = underlying ?? type;

      var point = effective == MemberReads.PointOf(space);

      // A point IS the cell, blank included, so a nullable point has no blank left to tolerate —
      // and the advice differs from every other refusal, which is why it is said separately.
      if (point && underlying is not null)
        throw new ArgumentException(
          $"{typeof(T).Name}.{name} is a {Describe(effective)}?, and a point is the cell itself, "
          + "blank included, so there is no blank for the nullable form to tolerate. Declare it "
          + "non-nullable.");

      if (!MemberReads.Reads(effective, space))
      {
        var described = Describe(effective) + (underlying is null ? string.Empty : "?");

        throw new ArgumentException(
          $"{typeof(T).Name}.{name} is {Article(described)} {described}, and no cell accessor yields {described}. "
          + $"Supported: string, decimal, double, int, DateTime, bool, {Describe(MemberReads.PointOf(space))}, and the nullable forms. "
          + "Read it as int or decimal and convert in Select.");
      }

      // Nullable<T> is blank tolerance the CLR can see; string? is blank tolerance the annotation
      // carries. Neither is kind tolerance: a text cell in a decimal? column still fails.
      var tolerant = underlying is not null || (effective == typeof(string) && annotatedNullable());

      return new MemberPlan(name, caption, effective, tolerant);
    }

    /// <summary>
    /// The name a C# reader would have written. Keyword aliases for the primitives that get asked
    /// for, angle brackets for a constructed generic, and the plain type name for everything else —
    /// a <c>Guid</c> or a <c>TimeSpan</c> reads correctly as itself.
    /// </summary>
    private static string Describe(Type type)
      => Arity(type) is int arity ? $"{type.Name.Substring(0, arity)}<{string.Join(", ", type.GetGenericArguments().Select(argument => argument.Name))}>"
       : type == typeof(long) ? "long"
       : type == typeof(float) ? "float"
       : type == typeof(short) ? "short"
       : type == typeof(byte) ? "byte"
       : type == typeof(sbyte) ? "sbyte"
       : type == typeof(uint) ? "uint"
       : type == typeof(ulong) ? "ulong"
       : type == typeof(ushort) ? "ushort"
       : type == typeof(char) ? "char"
       : type == typeof(object) ? "object"
       : type.Name;

    /// <summary>
    /// Where a constructed generic's name stops and its arity mark begins, or null where the type
    /// has no mark to strip. A non-generic type NESTED in a generic one is generic by
    /// <c>IsGenericType</c> and still carries no backtick of its own, which is the case that turns
    /// the obvious substring into an exception.
    /// </summary>
    private static int? Arity(Type type)
    {
      var mark = type.Name.IndexOf('`');

      return type.IsGenericType && mark > 0 ? mark : (int?)null;
    }

    /// <summary>
    /// "u" is excluded deliberately: every u-word this can produce — <c>uint</c>, <c>ulong</c>,
    /// <c>Uri</c> — is pronounced "yoo" and takes "a".
    /// </summary>
    private static string Article(string described)
      => "aeio".IndexOf(char.ToLowerInvariant(described[0])) >= 0 ? "an" : "a";
  }
}
