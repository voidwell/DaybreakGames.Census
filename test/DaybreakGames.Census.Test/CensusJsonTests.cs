using System.Text.Json;
using DaybreakGames.Census.JsonConverters;

namespace DaybreakGames.Census.Test;

public class CensusJsonTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new BooleanJsonConverter(), new DateTimeJsonConverter() }
    };

    [Theory]
    [InlineData("CharacterId", "character_id")]
    [InlineData("Name", "name")]
    [InlineData("ServiceIdValue", "service_id_value")]
    [InlineData("alreadylower", "alreadylower")]
    public void CensusUnderscoreNamingPolicy(string name, string expected)
    {
        var policy = new UnderscorePropertyJsonNamingPolicy();

        Assert.Equal(expected, policy.ConvertName(name));
    }

    [Fact]
    public void CensusUnderscoreNamingPolicyRejectsNull()
    {
        var policy = new UnderscorePropertyJsonNamingPolicy();

        Assert.Throws<ArgumentNullException>(() => policy.ConvertName(null!));
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    [InlineData("", false)]
    public void CensusBooleanConverterReadsStrings(string value, bool expected)
    {
        var result = JsonSerializer.Deserialize<bool>($"\"{value}\"", Options);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CensusBooleanConverterWritesBoolean()
    {
        Assert.Equal("true", JsonSerializer.Serialize(true, Options));
        Assert.Equal("false", JsonSerializer.Serialize(false, Options));
    }

    [Fact]
    public void CensusDateTimeConverterReadsEpochSeconds()
    {
        var result = JsonSerializer.Deserialize<DateTime>("\"1577934245\"", Options);

        Assert.Equal(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void CensusDateTimeConverterReadsDateString()
    {
        var result = JsonSerializer.Deserialize<DateTime>("\"2020-01-02 03:04:05\"", Options);

        Assert.Equal(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void CensusDateTimeConverterWritesUtc()
    {
        var value = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        Assert.Equal("\"2020-01-02T03:04:05Z\"", JsonSerializer.Serialize(value, Options));
    }

    [Fact]
    public void CensusTryGetValueReturnsProperty()
    {
        using var doc = JsonDocument.Parse("{\"a\":1}");

        Assert.Equal(1, doc.RootElement.TryGetValue("a").GetInt32());
    }

    [Fact]
    public void CensusTryGetValueMissingPropertyIsUndefined()
    {
        using var doc = JsonDocument.Parse("{\"a\":1}");

        Assert.Equal(JsonValueKind.Undefined, doc.RootElement.TryGetValue("b").ValueKind);
    }

    [Fact]
    public void CensusTryGetValueUndefinedElementIsUndefined()
    {
        JsonElement element = default;

        Assert.Equal(JsonValueKind.Undefined, element.TryGetValue("a").ValueKind);
    }

    [Fact]
    public void CensusTryGetString()
    {
        using var doc = JsonDocument.Parse("{\"a\":\"text\",\"n\":5}");

        Assert.Equal("text", doc.RootElement.TryGetString("a"));
        Assert.Equal("5", doc.RootElement.TryGetString("n"));
        Assert.Null(doc.RootElement.TryGetString("missing"));
    }
}
