using System.Globalization;

namespace DaybreakGames.Census.Test;

public class CensusOperandTests
{
    private static readonly DateTime Date = new(2020, 1, 2, 3, 4, 5);

    public CensusOperandTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    [Fact]
    public void CensusEqualsMultipleStrings()
    {
        var query = Query();
        query.Where("field").Equals("a", "b", "c");

        Assert.Equal("character/?field=a,b,c", query.ToString());
    }

    [Fact]
    public void CensusEqualsMultipleInts()
    {
        var query = Query();
        query.Where("field").Equals(1, 2, 3);

        Assert.Equal("character/?field=1,2,3", query.ToString());
    }

    [Fact]
    public void CensusEqualsDateTime()
    {
        var query = Query();
        query.Where("field").Equals(Date, Date.AddDays(1));

        Assert.Equal("character/?field=2020-01-02 03:04:05,2020-01-03 03:04:05", query.ToString());
    }

    [Fact]
    public void CensusNotEqualsMultipleValues()
    {
        var query = Query();
        query.Where("field").NotEquals("a", "b");

        Assert.Equal("character/?field=!a,b", query.ToString());
    }

    [Fact]
    public void CensusNotEqualsNumbers()
    {
        var query = Query();
        query.Where("a").NotEquals(1, 2);
        query.Where("b").NotEquals(1.5, 2.5);
        query.Where("c").NotEquals(1.5f, 2.5f);

        Assert.Equal("character/?a=!1,2&b=!1.5,2.5&c=!1.5,2.5", query.ToString());
    }

    [Fact]
    public void CensusNotEqualsDateTime()
    {
        var query = Query();
        query.Where("field").NotEquals(Date);

        Assert.Equal("character/?field=!2020-01-02 03:04:05", query.ToString());
    }

    [Fact]
    public void CensusLessThanTypes()
    {
        var query = Query();
        query.Where("a").IsLessThan(1);
        query.Where("b").IsLessThan(1.5);
        query.Where("c").IsLessThan(1.5f);
        query.Where("d").IsLessThan(Date);

        Assert.Equal("character/?a=<1&b=<1.5&c=<1.5&d=<2020-01-02 03:04:05", query.ToString());
    }

    [Fact]
    public void CensusLessThanOrEqualsTypes()
    {
        var query = Query();
        query.Where("a").IsLessThanOrEquals(1);
        query.Where("b").IsLessThanOrEquals(1.5);
        query.Where("c").IsLessThanOrEquals(1.5f);
        query.Where("d").IsLessThanOrEquals(Date);

        Assert.Equal("character/?a=[1&b=[1.5&c=[1.5&d=[2020-01-02 03:04:05", query.ToString());
    }

    [Fact]
    public void CensusGreaterThanTypes()
    {
        var query = Query();
        query.Where("a").IsGreaterThan(1);
        query.Where("b").IsGreaterThan(1.5);
        query.Where("c").IsGreaterThan(1.5f);
        query.Where("d").IsGreaterThan(Date);

        Assert.Equal("character/?a=>1&b=>1.5&c=>1.5&d=>2020-01-02 03:04:05", query.ToString());
    }

    [Fact]
    public void CensusGreaterThanOrEqualsTypes()
    {
        var query = Query();
        query.Where("a").IsGreaterThanOrEquals(1);
        query.Where("b").IsGreaterThanOrEquals(1.5);
        query.Where("c").IsGreaterThanOrEquals(1.5f);
        query.Where("d").IsGreaterThanOrEquals(Date);

        Assert.Equal("character/?a=]1&b=]1.5&c=]1.5&d=]2020-01-02 03:04:05", query.ToString());
    }

    [Fact]
    public void CensusStartsWithAndContains()
    {
        var query = Query();
        query.Where("a").StartsWith("abc");
        query.Where("b").Contains("def");

        Assert.Equal("character/?a=^abc&b=*def", query.ToString());
    }

    [Fact]
    public void CensusWhereWithAction()
    {
        var query = Query();
        query.Where("field", o => o.Equals("x"));

        Assert.Equal("character/?field=x", query.ToString());
    }

    private static Operators.CensusQuery Query()
    {
        return new CensusQueryFactory(null!).Create("character");
    }
}
