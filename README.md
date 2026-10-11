# DaybreakGames.Census

[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/voidwell/daybreakgames.census/build-test.yml?branch=main&style=for-the-badge)](https://github.com/voidwell/daybreakgames.census/actions/workflows/build-test.yml)
[![Latest Release](https://img.shields.io/github/v/release/voidwell/daybreakgames.census?style=for-the-badge)](https://github.com/voidwell/daybreakgames/releases/latest)
[![NuGet](https://img.shields.io/nuget/v/DaybreakGames.Census.svg?style=for-the-badge)](https://www.nuget.org/packages/DaybreakGames.Census/)
[![MIT License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

**This package is unofficial and is NOT in any way supported by Daybreak Games.**

All use of game data and other content must comply with [Daybreak Games Intellectual Property policy](http://census.daybreakgames.com/#intellectual-property)
and Daybreak Game Company's [Terms of Service](https://www.daybreakgames.com/termsofservice.vm?locale=en_US).

---

DaybreakGames.Census is a library that makes it much easier to use the Census API provided by Daybreak Games Company.

For a full demonstration check out the [DemoApp](https://github.com/voidwell/DaybreakGames.Census/tree/main/src/DemoApp)!

## Table of Contents

- [Installing](#installing)
- [Creating a new query](#creating-a-new-query)
- [Returning data from the query](#returning-data-from-the-query)
- [Setting global parameters](#setting-global-parameters)
- [Cancelling a query](#cancelling-a-query)
- [Configuring the HTTP client and timeout](#configuring-the-http-client-and-timeout)
- [Logging](#logging)
- [Defining a condition](#defining-a-condition)
- [Setting a language](#setting-a-language)
- [Show certain fields](#show-certain-fields)
- [Hide certain fields](#hide-certain-fields)
- [Set a limit for number of rows to return](#set-a-limit-for-number-of-rows-to-return)
- [Set the starting row](#set-the-starting-row)
- [Add a resolve](#add-a-resolve)
- [Join to another service](#join-to-another-service)
- [Tree results on a field](#tree-results-on-a-field)
- [Getting the url of the query](#getting-the-url-of-the-query)
- [Streaming data](#streaming-data)

### Installing

DaybreakGames.Census is available via [NuGet](https://www.nuget.org/packages/DaybreakGames.Census/).

To inject DaybreakGames.Census services into your project add `services.AddCensusServices();`
to your ConfigureServices block.

### Creating a new query

To start creating queries, use `ICensusQueryFactory` via DI and call
`ICensusQueryFactory.Create(<serviceName>)` to get a `CensusQuery` object.

```C#
public MyClass(ICensusQueryFactory censusFactory)
{
    var query = censusFactory.Create("character");
    query.SetServiceId("my-service-id")
        .SetServiceNamespace("ps2");
}
```

`SetServiceId` and `SetServiceNamespace` are optional. If not set than the values
provided in `AddCensusServices()` are used. Otherwise it defaults to `ServiceId="example"`
and `ServiceNamespace="ps2:v2"`.

### Returning data from the query

There are three methods of query resolution: `GetAsync<T>()`, `GetListAsync<T>()`, and
`GetBatchAsync<T>()`. A model can be passed in that deserializes to the expected census model
that matches Pascal Case versions of the census properties (i.e `character_id` becomes
`CharacterId`). For other cases, the `[JsonProperty]` decorators should be respected.

If a model is not given than the default return type is as a JToken.

```C#
var resultJToken = await query.GetAsync();
```

### Setting global parameters

If your application is always using the same service id and namespace you might as well set it
globally so you don't have to configure it with every query.

```C#
public void ConfigureServices(IServiceCollection services)
{
    services.AddCensusServices(options =>
            {
                options.CensusServiceId = "my-service-id";
                options.CensusServiceNamespace = "ps2";
            });
}
```

### Cancelling a query

`GetAsync`, `GetListAsync` and `GetBatchAsync` (and their generic overloads) accept an optional
`CancellationToken`. Cancelling it aborts the in-flight request and throws an
`OperationCanceledException`. For `GetBatchAsync` the token is checked between each page request.

```C#
var characters = await query.GetListAsync<CensusCharacterModel>(cancellationToken);
```

A request that exceeds `CensusTimeout` (see below) is cancelled the same way.

### Configuring the HTTP client and timeout

Census requests are sent through a typed `HttpClient` registered with `AddHttpClient`, so the
client's lifetime and handlers are managed by `IHttpClientFactory`. `CensusTimeout` (default
10 seconds) and `UserAgent` are applied to that client.

```C#
services.AddCensusServices(options =>
        {
            options.CensusTimeout = TimeSpan.FromSeconds(30);
            options.UserAgent = "my-app/1.0";
        });
```

Because the client is registered through `AddHttpClient`, `ICensusClient` and `ICensusQueryFactory`
are transient and no longer implement `IDisposable`. To customize the underlying client further
(retry policies, handlers, etc.), call `AddHttpClient` for it again after `AddCensusServices()`:

```C#
services.AddCensusServices();
services.AddHttpClient<ICensusClient, CensusClient>()
        .AddStandardResilienceHandler();
```

The stream client reconnects automatically if no message is received within 35 seconds.

### Logging

The library logs through `Microsoft.Extensions.Logging`. All messages use structured templates, so
properties such as `RequestUri`, `StatusCode` and `ElapsedMilliseconds` are available as fields in
structured logging sinks. Logging is controlled entirely through the standard log level filters on
these categories:

| Category | Level | Event Id | Message |
| --- | --- | --- | --- |
| `DaybreakGames.Census.CensusClient` | Trace | 85400 | Request sent (`RequestUri`) |
| `DaybreakGames.Census.CensusClient` | Trace | 85401 | Request completed (`StatusCode`, `ElapsedMilliseconds`, `RequestUri`) |
| `DaybreakGames.Census.CensusClient` | Trace | 85402 | Request timed out (`ElapsedMilliseconds`, `RequestUri`). The exception is still thrown to the caller |
| `DaybreakGames.Census.CensusClient` | Error | 84530 | Unknown exception while processing a query |
| `DaybreakGames.Census.CensusClient` | Error | 84531 | Census service unavailable |
| `DaybreakGames.Census.CensusClient` | Error | 84532 | Census returned a server error |
| `DaybreakGames.Census.CensusClient` | Error | 84533 | Connection failed or a non-success status code was returned |
| `DaybreakGames.Census.CensusClient` | Error | 84534 | Request timed out |
| `DaybreakGames.Census.Stream.CensusStreamClient` | Information | | Stream connected, reconnected and subscription sent |
| `DaybreakGames.Census.Stream.CensusStreamClient` | Warning | 75421 | Stream disconnected |
| `System.Net.Http.HttpClient.ICensusClient.LogicalHandler` | Information | | Request start and end, including duration (from `IHttpClientFactory`) |
| `System.Net.Http.HttpClient.ICensusClient.ClientHandler` | Information | | Each attempt sent over the wire, including retries (from `IHttpClientFactory`) |
| `System.Net.Http.HttpClient.ICensusClient.*` | Trace | | Request and response headers (from `IHttpClientFactory`) |

Errors are always logged and the exception is still thrown to the caller. A query cancelled through
its `CancellationToken` is not logged as an error.

To see a trace of every Census request with timings, lower the level of the `DaybreakGames.Census`
namespace (or just the `CensusClient` category) in `appsettings.json`:

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "DaybreakGames.Census": "Trace",
            "System.Net.Http.HttpClient.ICensusClient": "Warning"
        }
    }
}
```

To silence the library entirely, set `"DaybreakGames.Census": "None"`.

**Note:** the logged `RequestUri` includes your service id (`/s:<service-id>/`).

### Defining a condition

```C#
query.Where("name.lower").Equals("lampjaw");
```

The following operations and their equivalent syntax is below:

* `Equals`: =
* `NotEquals`: =!
* `IsLessThan`: =<
* `IsLessThanOrEquals`: =[
* `IsGreaterThan`: =>
* `IsGreaterThanOrEquals`: =]
* `StartsWith`: =^
* `Contains`: =*

### Setting a language

```C#
query.SetLanguage("en");

OR

query.SetLanguage(CensusLanguage.English);
```

No language is set by default so you will receive all localized strings if available.

### Show certain fields

```C#
query.ShowFields("character_id"[, "field2", "field3", ...]);
```

### Hide certain fields

```C#
query.HideFields("currency"[, "field2", "field3", ...]);
```

### Set a limit for number of rows to return

```C#
query.SetLimit(10);
```

### Set the starting row

```C#
query.SetStart(100);
```

### Add a resolve

```C#
query.AddResolve("world"[, "resolve2", "resolve3", ...]);
```

### Join to another service

```C#
var worldJoin = query.JoinService("characters_world");
```

Join objects have the following methods:

* `IsList(bool)`
* `IsOuterJoin(bool)`
* `ShowFields(array)`: See the 'Show certain fields' section above
* `HideFields(array)`: See the 'Hide certain fields' section above
* `OnField(string)`
* `ToField(string)`
* `InjectAt(string)`
* `Where(string)`: See the 'Defining a condition' section above
* `JoinService(string)`: Returns another join object for sub joining

### Tree results on a field

```C#
var query = queryFactory.Create("vehicle");
var vehicleTree = query.TreeField("type_id");
```

Tree objects have the following methods:

* `IsList(bool)`
* `GroupPrefix(string)`
* `StartField(string)`
* `TreeField(string)`: Returns another tree object for sub grouping

### Getting the url of the query

```C#
Uri uri = query.GetUri();
```

### Streaming data

This package also allows you to leverage the Census' websocket service to get data
in real time. Inject `ICensusStreamClient` and subcribe to events.
See the [WebsocketMonitor class in the DemoApp](https://github.com/voidwell/DaybreakGames.Census/blob/main/src/DemoApp/WebsocketMonitor.cs)
for a full example.
