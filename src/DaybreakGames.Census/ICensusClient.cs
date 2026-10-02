using DaybreakGames.Census.Operators;

namespace DaybreakGames.Census;

public interface ICensusClient : IDisposable
{
    CensusQuery CreateQuery(string serviceName);
    Task<T> ExecuteQuery<T>(CensusQuery query);
    Task<IEnumerable<T>> ExecuteQueryList<T>(CensusQuery query);
    Task<IEnumerable<T>> ExecuteQueryBatch<T>(CensusQuery query);
    Uri CreateRequestUri(CensusQuery query);
}
