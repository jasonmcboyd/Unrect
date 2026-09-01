namespace Unrect.Shapes
{
  /// <summary>
  /// <c>Vertical</c> and <c>Horizontal</c>: a cursor-lambda form, then the fixed arities 2 through 8
  /// written out rather than abstracted. Arity lives here and nowhere else — one <c>StackShape</c>
  /// backs the tuple forms, and eight is where <c>ValueTuple</c> stops before <c>TRest</c>; nest a
  /// stack, or take the lambda form, to go further.
  /// <para>
  /// Every overload lays its children out in declaration order along its axis and consumes along
  /// that axis only. The fixed arities return their results as a tuple — which <c>Select</c> unpacks
  /// — while the lambda form builds the result where the parts are read.
  /// </para>
  /// </summary>
  public static partial class Shape
  {
    /// <summary>
    /// A flow downwards whose children are declared by calling <c>Next</c> on the cursor, in the
    /// order they appear on the sheet, and whose result the lambda builds from what they read:
    /// <c>Vertical(v =&gt; new Report(Header: v.Next(header), Rows: v.Next(rows)))</c>. The parts
    /// are named where they are read, and there is no arity to run out of.
    /// <para>
    /// The lambda declares a <em>sequence of shapes</em>, nothing more. Alternation belongs to
    /// <c>Choice</c>, <c>Else</c>, and <c>Optional</c>; repetition to <c>Repeat</c>; gaps to the
    /// following shape's offset. Conditionals, loops, and arithmetic over positions inside the
    /// lambda are the row-walking this library exists to replace, and a lambda that picks a later
    /// shape from an earlier value can never be rendered or checked without a file.
    /// </para>
    /// <para>
    /// Capture nothing you write to. A shape is safe to apply to many spaces at once only because
    /// everything it holds is immutable, and a lambda that increments a counter or appends to a
    /// list gives that up. It also runs partially inside a losing <c>Choice</c> branch, where
    /// diagnostics roll back but side effects do not.
    /// </para>
    /// <para>
    /// Do the reading inside the leaf, not around it: <c>decimal.Parse(v.Next(raw))</c> that throws
    /// blames this flow at its own origin, while a projection inside the leaf blames the cell.
    /// </para>
    /// <para>
    /// The lambda must call <c>Next</c> at least once — a flow that declares nothing would match
    /// anything and describe nothing — and what it declares can be enumerated only by running it, so
    /// this flow cannot be inspected without a space the way the fixed-arity overloads can.
    /// </para>
    /// </summary>
    public static IShape<T> Vertical<T>(Layout<T> build)
      => new CursorStackShape<T>(Orientation.Vertical, NotNull(build, nameof(build)), Placement.Default);

    /// <summary>
    /// A flow rightwards whose children are declared by calling <c>Next</c> on the cursor; see
    /// <see cref="Vertical{T}(Layout{T})"/> for what belongs in the lambda and what does not.
    /// </summary>
    public static IShape<T> Horizontal<T>(Layout<T> build)
      => new CursorStackShape<T>(Orientation.Horizontal, NotNull(build, nameof(build)), Placement.Default);

    /// <summary>2 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2)> Vertical<T1, T2>(
      IShape<T1> first,
      IShape<T2> second)
      => Stack<(T1, T2)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)) },
        values => ((T1)values[0]!, (T2)values[1]!));

    /// <summary>3 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3)> Vertical<T1, T2, T3>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third)
      => Stack<(T1, T2, T3)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!));

    /// <summary>4 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4)> Vertical<T1, T2, T3, T4>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth)
      => Stack<(T1, T2, T3, T4)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!));

    /// <summary>5 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5)> Vertical<T1, T2, T3, T4, T5>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth)
      => Stack<(T1, T2, T3, T4, T5)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!));

    /// <summary>6 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6)> Vertical<T1, T2, T3, T4, T5, T6>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth)
      => Stack<(T1, T2, T3, T4, T5, T6)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!));

    /// <summary>7 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6, T7)> Vertical<T1, T2, T3, T4, T5, T6, T7>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth,
      IShape<T7> seventh)
      => Stack<(T1, T2, T3, T4, T5, T6, T7)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)), NotNull(seventh, nameof(seventh)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!, (T7)values[6]!));

    /// <summary>8 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6, T7, T8)> Vertical<T1, T2, T3, T4, T5, T6, T7, T8>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth,
      IShape<T7> seventh,
      IShape<T8> eighth)
      => Stack<(T1, T2, T3, T4, T5, T6, T7, T8)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)), NotNull(seventh, nameof(seventh)), NotNull(eighth, nameof(eighth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!, (T7)values[6]!, (T8)values[7]!));

    /// <summary>2 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2)> Horizontal<T1, T2>(
      IShape<T1> first,
      IShape<T2> second)
      => Stack<(T1, T2)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)) },
        values => ((T1)values[0]!, (T2)values[1]!));

    /// <summary>3 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3)> Horizontal<T1, T2, T3>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third)
      => Stack<(T1, T2, T3)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!));

    /// <summary>4 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4)> Horizontal<T1, T2, T3, T4>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth)
      => Stack<(T1, T2, T3, T4)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!));

    /// <summary>5 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5)> Horizontal<T1, T2, T3, T4, T5>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth)
      => Stack<(T1, T2, T3, T4, T5)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!));

    /// <summary>6 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6)> Horizontal<T1, T2, T3, T4, T5, T6>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth)
      => Stack<(T1, T2, T3, T4, T5, T6)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!));

    /// <summary>7 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6, T7)> Horizontal<T1, T2, T3, T4, T5, T6, T7>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth,
      IShape<T7> seventh)
      => Stack<(T1, T2, T3, T4, T5, T6, T7)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)), NotNull(seventh, nameof(seventh)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!, (T7)values[6]!));

    /// <summary>8 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6, T7, T8)> Horizontal<T1, T2, T3, T4, T5, T6, T7, T8>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth,
      IShape<T7> seventh,
      IShape<T8> eighth)
      => Stack<(T1, T2, T3, T4, T5, T6, T7, T8)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)), NotNull(seventh, nameof(seventh)), NotNull(eighth, nameof(eighth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!, (T7)values[6]!, (T8)values[7]!));
  }
}
