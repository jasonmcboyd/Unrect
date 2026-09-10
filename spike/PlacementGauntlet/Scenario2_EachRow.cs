using PlacementGauntlet.Staged;

using Unrect.Projections;

using static PlacementGauntlet.Staged.Place;
using static Unrect.Projections.Projection;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario 2 — the eachRow idiom, the census hotspot: a bind row whose columns are found
  /// by caption, one <c>Right(col)</c> per field inside an <c>Overlay</c>. Fifteen of the corpus's
  /// ~30 placements are leaves shaped exactly like this, so the pass bar is <b>zero added
  /// ceremony</b>.
  /// <para>
  /// The result: <c>Decimal().Right(captions["Amount"])</c> becomes
  /// <c>Right(captions["Amount"]).Decimal()</c> — same tokens, same line length, one word moved. The
  /// stage-jumping argument of §3 (a leaf's extent is intrinsic, so its anchor closes immediately)
  /// turns out to cost nothing here BECAUSE the pipeline is inverted: the leaf IS the terminal, so
  /// there is no open size slot to close and <c>SizedToChildren</c> never appears.
  /// </para>
  /// </summary>
  public static class Scenario2
  {
    public static void Run()
    {
      Judge.Section("Scenario 2 — the eachRow idiom (the census hotspot)");

      var sheet = Sheets.BuyingPower();

      // OLD — subject first: read a decimal, and read it that many columns along.
      var oldPositions = Table(headerRows: 1, eachRow: captions => Overlay(o => new Position(
        Symbol: o.Next(Text().Right(captions["Symbol"])),
        Quantity: o.Next(Decimal().Right(captions["Quantity"])),
        MarketValue: o.Next(Decimal().Right(captions["Market Value"])),
        BuyingPower: o.Next(Decimal().Right(captions["Buying Power"])))));

      // NEW — placement first: that many columns along, a decimal.
      var newPositions = Table(headerRows: 1, eachRow: captions => Overlay(o => new Position(
        Symbol: o.Next(Place.Right(captions["Symbol"]).Text()),
        Quantity: o.Next(Place.Right(captions["Quantity"]).Decimal()),
        MarketValue: o.Next(Place.Right(captions["Market Value"]).Decimal()),
        BuyingPower: o.Next(Place.Right(captions["Buying Power"]).Decimal()))));

      Judge.Same("buying-power bind: the inverted leaf reads what the postfix leaf reads",
        oldPositions.Map(sheet), newPositions.Map(sheet));

      // And the whole table placed, to show the hotspot inside a placed region rather than at the root.
      var oldPlaced = oldPositions.Below(RowContaining("Account")).Sized(RowsWhileAnyValue());
      var newPlaced = Place.Below(RowContaining("Account")).Sized(RowsWhileAnyValue())
        .Table(headerRows: 0, eachRow: Overlay(o => new Position(
          Symbol: o.Next(Place.Right(1).Text()),
          Quantity: o.Next(Place.Right(2).Decimal()),
          MarketValue: o.Next(Place.Right(3).Decimal()),
          BuyingPower: o.Next(Place.Right(4).Decimal()))));

      var oldPlacedEquivalent = Table(headerRows: 0, eachRow: Overlay(o => new Position(
          Symbol: o.Next(Text().Right(1)),
          Quantity: o.Next(Decimal().Right(2)),
          MarketValue: o.Next(Decimal().Right(3)),
          BuyingPower: o.Next(Decimal().Right(4)))))
        .Below(RowContaining("Account"))
        .Sized(RowsWhileAnyValue());

      Judge.Same("the sparse-overlay table, placed and sized both ways",
        oldPlacedEquivalent.Map(sheet), newPlaced.Map(sheet));
      Judge.Note("Two stages and a terminal read left to right in execution order: below the caption, this tall, a table.");
      Judge.Note($"Ceremony: 4 leaves, 0 added words. Old '{"Decimal().Right(captions[\"Amount\"])"}' -> "
        + $"new '{"Right(captions[\"Amount\"]).Decimal()"}'.");

      // The one thing the hotspot loses: OrBlank, and every other clone modifier, still lands on the
      // TERMINAL's result rather than in the pipeline, so a tolerant leaf reads inside-out.
      var tolerantOld = Decimal().OrBlank().Right(2);
      var tolerantNew = Place.Right(2).Decimal().OrBlank();

      Judge.Same("a tolerant leaf: OrBlank stays postfix on the closed projection",
        Table(headerRows: 1, eachRow: Overlay(o => o.Next(tolerantOld))).Map(sheet),
        Table(headerRows: 1, eachRow: Overlay(o => o.Next(tolerantNew))).Map(sheet));
      Judge.Note("OrBlank/Named/Select/Optional are NOT stages and cannot be: they modify what was read, not where it sits."
        + " A declaration therefore reads placement-first, then subject, then tolerance — three directions in one line.");
    }
  }
}
