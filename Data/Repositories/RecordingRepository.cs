using Dapper;
using Xtreamium.Proxy.Data.Models;

namespace Xtreamium.Proxy.Data.Repositories;

public interface IRecordingRepository : IRepository<Recording> {
  Task<Recording?> GetByJobIdAsync(string jobId);
  Task<IEnumerable<Recording>> GetScheduledRecordingsAsync();
  Task<IEnumerable<Recording>> GetCompletedRecordingsAsync();
}

public class RecordingRepository : Repository<Recording>, IRecordingRepository {
  public RecordingRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) {
  }

  public async Task<Recording?> GetByJobIdAsync(string jobId) {
    using var connection = await _connectionFactory.CreateConnectionAsync();
    const string sql = "SELECT * FROM recordings WHERE JobId = @JobId";
    return await connection.QueryFirstOrDefaultAsync<Recording>(sql, new { JobId = jobId });
  }

  public async Task<IEnumerable<Recording>> GetScheduledRecordingsAsync() {
    using var connection = await _connectionFactory.CreateConnectionAsync();
    const string sql = "SELECT * FROM recordings WHERE IsRecorded = 0 ORDER BY StartTime DESC";
    return await connection.QueryAsync<Recording>(sql);
  }

  public async Task<IEnumerable<Recording>> GetCompletedRecordingsAsync() {
    using var connection = await _connectionFactory.CreateConnectionAsync();
    const string sql = "SELECT * FROM recordings WHERE IsRecorded = 1 ORDER BY StartTime DESC";
    return await connection.QueryAsync<Recording>(sql);
  }
}
