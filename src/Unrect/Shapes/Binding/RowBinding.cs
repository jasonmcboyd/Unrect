using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// The plan for turning a table row into a <c>T</c>: which members exist, what caption and kind
  /// each carries, and a compiled materializer. Everything reflective happens once, at shape
  /// construction — no reflection runs per <c>Map</c> and none per row, so the shape stays immutable
  /// and safe to apply to many workbooks at once.
  /// </summary>
  internal sealed class RowBinding<T>
  {
    private static readonly IReadOnlyDictionary<Type, (CellKind Kind, CellReader<object?> Read)> Readers =
      new Dictionary<Type, (CellKind, CellReader<object?>)>
      {
        [typeof(string)] = (CellKind.Text, Box<string>(CellReading.ReadString)),
        [typeof(decimal)] = (CellKind.Number, Box<decimal>(CellReading.ReadDecimal)),
        [typeof(double)] = (CellKind.Number, Box<double>(CellReading.ReadDouble)),
        [typeof(int)] = (CellKind.Number, Box<int>(CellReading.ReadInteger)),
        [typeof(DateTime)] = (CellKind.Temporal, Box<DateTime>(CellReading.ReadDateTime)),
        [typeof(bool)] = (CellKind.Boolean, Box<bool>(CellReading.ReadBoolean)),
      };

    private readonly Func<object?[], T> _materialize;

    private RowBinding(IReadOnlyList<MemberPlan> members, Func<object?[], T> materialize)
    {
      Members = members;
      _materialize = materialize;
    }

    /// <summary>The members that must find a caption, in materialization order.</summary>
    public IReadOnlyList<MemberPlan> Members { get; }

    public T Materialize(object?[] values) => _materialize(values);

    public static RowBinding<T> Create(TableBinding<T>? binding)
    {
      var captions = binding?.Captions ?? new Dictionary<string, string>(StringComparer.Ordinal);
      var ignored = new HashSet<string>(binding?.Ignored ?? Array.Empty<string>(), StringComparer.Ordinal);

      foreach (var name in captions.Keys)
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
        ? FromConstructor(constructors[0], captions, ignored)
        : FromProperties(parameterless, captions, ignored);
    }

    private static RowBinding<T> FromConstructor(
      ConstructorInfo constructor,
      IReadOnlyDictionary<string, string> captions,
      ISet<string> ignored)
    {
      var parameters = constructor.GetParameters();

      if (parameters.Length == 0)
        throw new ArgumentException($"{typeof(T).Name} has no properties to bind.");

      // Selectors name properties; positional records name the parameter identically, and a
      // hand-written constructor differs only in case.
      string Declared(ParameterInfo parameter)
        => captions.FirstOrDefault(c => CaptionComparer.Default.Equals(c.Key, parameter.Name!)).Value
           ?? parameter.Name!;

      bool Ignored(ParameterInfo parameter)
        => ignored.Any(name => CaptionComparer.Default.Equals(name, parameter.Name!));

      Verify(captions.Keys, ignored, parameters.Select(p => p.Name!), viaConstructor: true);

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
        members.Add(Plan(PropertyName(parameter.Name!), Declared(parameter), parameter.ParameterType,
          () => NullableAnnotations.IsAnnotatedNullable(parameter)));

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
      ISet<string> ignored)
    {
      // Read-only properties are invisible to the binder and are never required.
      var settable = typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetSetMethod() is not null && p.GetIndexParameters().Length == 0)
        .ToArray();

      if (settable.Length == 0)
        throw new ArgumentException($"{typeof(T).Name} has no properties to bind.");

      Verify(captions.Keys, ignored, settable.Select(p => p.Name), viaConstructor: false);

      var members = new List<MemberPlan>();
      var values = Expression.Parameter(typeof(object?[]), "values");
      var bindings = new List<MemberBinding>();

      foreach (var property in settable)
      {
        if (ignored.Contains(property.Name))
          continue;

        var index = members.Count;

        members.Add(Plan(property.Name, captions.TryGetValue(property.Name, out var caption) ? caption : property.Name,
          property.PropertyType, () => NullableAnnotations.IsAnnotatedNullable(property)));

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

    private static MemberPlan Plan(string name, string caption, Type type, Func<bool> annotatedNullable)
    {
      // A CellValue member has no kind to assert and no conversion to fail, so blank tolerance is
      // not a thing it can have — the cell is handed over as it is.
      if (type == typeof(CellValue))
        return new MemberPlan(name, caption, null, false, null);

      var underlying = Nullable.GetUnderlyingType(type);
      var effective = underlying ?? type;

      if (!Readers.TryGetValue(effective, out var reader))
      {
        var described = Describe(effective) + (underlying is null ? string.Empty : "?");

        throw new ArgumentException(
          $"{typeof(T).Name}.{name} is {Article(described)} {described}, and no cell accessor yields {described}. "
          + "Supported: string, decimal, double, int, DateTime, bool, CellValue, and the nullable forms. "
          + "Read it as int or decimal and convert in Select.");
      }

      // Nullable<T> is blank tolerance the CLR can see; string? is blank tolerance the annotation
      // carries. Neither is kind tolerance: a text cell in a decimal? column still fails.
      var tolerant = underlying is not null || (effective == typeof(string) && annotatedNullable());

      return new MemberPlan(name, caption, reader.Kind, tolerant, reader.Read);
    }

    /// <summary>
    /// The name a C# reader would have written. Keyword aliases for the primitives that get asked
    /// for, and the plain type name for everything else — a <c>Guid</c> or a <c>TimeSpan</c> reads
    /// correctly as itself.
    /// </summary>
    private static string Describe(Type type)
      => type == typeof(long) ? "long"
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
    /// "u" is excluded deliberately: every u-word this can produce — <c>uint</c>, <c>ulong</c>,
    /// <c>Uri</c> — is pronounced "yoo" and takes "a".
    /// </summary>
    private static string Article(string described)
      => "aeio".IndexOf(char.ToLowerInvariant(described[0])) >= 0 ? "an" : "a";

    private static CellReader<object?> Box<TValue>(CellReader<TValue> read)
      => (CellValue cell, Func<string> at, out object? value, out string? conversion) =>
      {
        var ok = read(cell, at, out var typed, out conversion);
        value = ok ? typed : null;
        return ok;
      };
  }
}
