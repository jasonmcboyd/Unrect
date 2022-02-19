<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Array\bin\Debug\net5.0\Unrect.Array.dll">C:\Users\jason\Dev\Unrect\src\Unrect.Array\bin\Debug\net5.0\Unrect.Array.dll</Reference>
  <Reference Relative="..\src\Unrect.Array\bin\Debug\net5.0\Unrect.Core.dll">C:\Users\jason\Dev\Unrect\src\Unrect.Array\bin\Debug\net5.0\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\net5.0\Unrect.dll">C:\Users\jason\Dev\Unrect\src\Unrect\bin\Debug\net5.0\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Array\bin\Debug\net5.0\Unrect.Spaces.dll">C:\Users\jason\Dev\Unrect\src\Unrect.Array\bin\Debug\net5.0\Unrect.Spaces.dll</Reference>
  <Namespace>static Unrect.OffsetStrategy</Namespace>
  <Namespace>static Unrect.RegionBuilderFactory&lt;int&gt;</Namespace>
  <Namespace>static Unrect.RegionMapperFactory</Namespace>
  <Namespace>static Unrect.SizeStrategy</Namespace>
  <Namespace>Unrect</Namespace>
  <Namespace>Unrect.Array</Namespace>
</Query>


var nums = new[,]
{
	{0, 0, 1,  2,  3,  4,  0},
	{0, 0, 5,  6,  7,  8,  0},
	{0, 0, 0,  0,  0,  0,  0},
	{0, 0, 9,  10, 11, 12, 0},
	{0, 0, 13, 14, 15, 16, 0},
	{0, 0, 17, 18, 19, 20, 0},
	{0, 0, 0,  0,  0,  0,  0},
	{0, 0, 0,  0,  0,  0,  0},
	{0, 0, 21, 22, 23, 24, 0},
	{0, 0, 25, 26, 27, 28, 0},
	{0, 0, 0,  0,  0,  0,  0}
};




var space = new ArraySpace<int>(nums);

var builder =
	Vertical(
		Horizontal(
			0,0,3,2,
			Builder(0,0,1,2),
			Builder()),
		Builder(1,0,3,2),
		Builder(2,0, Max()));

var region = builder.Build(space);

var temp =
	Map(
	region,
	(r, x, y, z) => new { First = x, Second = y, Third = z},
	x => Map(
		x,
		(r, y, z) => new {Inner1 = y, Inner2 = z},
		y => y.ToArray(),
		z => z.ToArray()),
	y => y.ToArray(),
	z => z.ToArray());
	
var map =
	builder.Map(
		x => new { Inner1 = x.Subregion1.ToArray(), Inner2 = x.Subregion2.ToArray() },
		y => y.ToArray(),
		z => z.ToArray(),
		(x, y, z) => new { First = x, Second = y, Third = z});

//map.Map(region).Dump();

var map2 =
	builder.Map(
		x => x.Map(
			y => y.ToArray(),
			z => z.ToArray(),
			(y, z) => new { Inner1 = y, Inner2 = z}),
		y => y.ToArray(),
		z => z.ToArray(),
		(x, y, z) => new { First = x, Second = y, Third = z });
		
map2.Map(region).Dump();
		
var test = new
{
	First = new
	{
		Inner1 = region.Subregion1.Subregion1.ToArray(),
		Inner2 = region.Subregion1.Subregion2.ToArray()
	},
	Second = region.Subregion2.ToArray(),
	Third = region.Subregion3.ToArray()
};
//test.Dump();


/*
var builder =
	Vertical(
		Horizontal(
			0,0,3,2,
			Builder(0,0,1,2),
			Builder()),
		Builder(1,0,3,2),
		Builder(2,0, Max()));
		
builder.Map(sheet =>
	new 
	{
		First = sheet.Subregion1.Map(header =>
			new 
			{
				Inner1 = header.Subregion1.ToArray(),
				Inner2 = header.Subregion2.ToArray()
			}
		Second = sheet.Subregion2.ToArray(),
		Third = sheet.Subregion3.ToArray()
	})
	
builder.Map((r1, r2, r3) =>
	new 
	{
		First = r1.Map((rr1, rr2) =>
			new 
			{
				Inner1 = rr1.ToArray(),
				Inner2 = rr2.ToArray()
			}),
		Second = r2.ToArray(),
		Third = r3.ToArray()
	})
	
var map =
	Vertical(
		Horizontal(0,0,3,2,
			Builder(0,0,1,2).ArrayMap(),
			Builder().ArrayMap()
		).Map((x, y) => new { Inner1 = x, Inner2 = y }),
		Builder(1,0,3,2).ArrayMap(),
		Builder(2,0, Max()).ArrayMap()
	).Map((x, y, z) => new { First = x, Second = y, Third = z });
*/