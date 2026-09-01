<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Array.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Array.dll</Reference>
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Excel.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Excel.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Excel</Namespace>
  <Namespace>Unrect.Shapes</Namespace>
  <Namespace>static Unrect.Shapes.Shape</Namespace>
</Query>

var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\investor-summary.xlsx");

// Per-investor detail block: a name cell over a transaction table.
var investorDetail =
	Vertical(
		Cell(v => v.GetString()).Named("investor name"),
		TableRows(r => new
		{
			Date = r["Date"].GetDateTime(),
			Type = r["Transaction Type"].GetString(),
			Amount = r["Amount"].GetDecimal(),
		}).Named("transactions"))
	.Select((investor, txns) => new { Investor = investor, Transactions = txns })
	.Named("investor detail");

// The report. Column(c => ...) discovers the header height; the gap before the summary is the
// table's own default offset; the gap before the details section is the repeat's offset; the
// gaps between detail blocks are the repeat's separator.
var report =
	Vertical(
		Column(c => new
		{
			Title = c[0].GetString(),
			ReportDate = c[1].GetDateTime(),
			ReportId = c[2].GetString(),
		}).Named("report header"),
		TableRows(r => new
		{
			Investor = r["Investor"].GetString(),
			Contributions = r["Contributions"].GetDecimal(),
			Distributions = r["Distributions"].GetDecimal(),
			Net = r["Net"].GetDecimal(),
		}).Named("summary"),
		Repeat(investorDetail, separatedBy: BlankRows(), atLeast: 1)
			.AfterBlankRows()
			.Named("investor details"))
	.Select((header, summary, details) => new
	{
		ReportHeader = header,
		Summary = summary,
		Details = details,
	});

var result = report.Map(SpreadsheetSpace.Create(path, "Summary"));

// Cross-region correlations are post-parse validation, not decomposition.
(result.Summary.Count == result.Details.Count).Dump("summary rows == detail blocks");
result.Dump();
