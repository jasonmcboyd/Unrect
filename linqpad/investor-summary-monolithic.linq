<Query Kind="Program">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
  <Namespace>static Unrect.Spreadsheets.SheetProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
</Query>

void Main()
{
	// The MONOLITHIC counterpart to investor-summary.linq: the same report parsed as one inline
	// VerticalFlow rather than hoisted into named sub-projections. Kept side-by-side to compare the
	// two structures — identical output, different decomposition.
	//
	// The space is named once, in the query's namespace imports: the canonical vocabulary as
	// `using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>`, and the
	// sheet's own readings as `using static Unrect.Spreadsheets.SheetProjectionBuilders<...>`.
	var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\investor-summary.xlsx");
		
	// The tables keep the lambda form: each column is resolved from its caption once, the view hands
	// the cell back as a place, and the read written at it — .Text(), .Date(), .Decimal() — carries
	// that cell's A1 location into any failure. The decomposed investor-summary.linq reads exactly the
	// same way, so what the two files differ in is the decomposition and nothing else.
	//
	// Column(c => ...) discovers the header height; the gap before the summary is the table's own default
	// offset; the gap before the details section is the AfterBlankRows() entry; the gaps between detail
	// blocks are the repeat's separator.
	var report =
		VerticalFlow(v =>
		{
			var reportHeader = v.Next(
				Column(c => new
				{
					Title      = c[0].Text(),
					ReportDate = c[1].Date(),
					ReportId   = c[2].Text(),
				}));
			var summary = v.Next(Table<InvestorSummary>());
			var details = v.Next(
				AfterBlankRows()
				.VerticalRepeat(
					VerticalFlow(block =>
					{
						var investor = block.Next(Text());
						var transactions = block.Next(Table<Transaction>());

						return block.Build(read => new InvestorTransactions(read.Of(investor), read.Of(transactions)));
					}),
					separatedBy: BlankRows(),
					atLeast: 1));

			return v.Build(read => new
			{
				ReportHeader = read.Of(reportHeader),
				Summary = read.Of(summary),
				Details = read.Of(details),
			});
		});
	
	var result = report.Map(SpreadsheetSpace.Create(path, "Summary"));
	
	// Cross-region correlations are post-parse validation, not decomposition.
	(result.Summary.Count == result.Details.Count).Dump("summary rows == detail blocks");
	result.Dump();
	
}

// You can define other methods, fields, classes and namespaces here
public class InvestorSummary
{
	public string Investor { get; init; }
	public decimal Contributions { get; init; }
	public decimal Distributions { get; init; }
	public decimal Net { get; init; }
}

public class InvestorTransactions
{
	public InvestorTransactions(string investorName, IEnumerable<Transaction> transactions)
	{
		InvestorName = investorName;
		Transactions = transactions.ToArray();
	}
	
	public string InvestorName { get; }
	public Transaction[] Transactions { get; }
}

public class Transaction
{
	public DateTime Date { get; init; }
	public string TransactionType { get; init; }
	public decimal Amount { get; init; }
}
