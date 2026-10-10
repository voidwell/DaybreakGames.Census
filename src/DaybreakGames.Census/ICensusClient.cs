using DaybreakGames.Census.Operators;

namespace DaybreakGames.Census;

public interface ICensusClient
{
    CensusQuery CreateQuery(string serviceName);
    Task<T> ExecuteQueryAsync<T>(CensusQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<T>> ExecuteQueryListAsync<T>(CensusQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<T>> ExecuteQueryBatchAsync<T>(CensusQuery query, CancellationToken cancellationToken);
    Uri CreateRequestUri(CensusQuery query);
}
