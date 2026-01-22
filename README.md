# Redis + EF Core + SQL Server (Console POC) — .NET 6

Console POC that stores data in **SQL Server (EF Core)** and uses **Redis** as a cache (JSON strings).

Flow:
1. User enters `Name` and `Age`
2. App inserts the user into SQL Server
3. App reads the user using **cache-aside**:
   - Try Redis first
   - If cache miss → read from SQL → write to Redis with TTL
4. Second read should hit Redis

---

## Tech
- .NET 6 Console App
- EF Core 6 + SQL Server provider
- StackExchange.Redis
- Configuration via `appsettings.json`

---

## Project Structure

- `Program.cs`  
  Orchestrates the flow (insert, read with cache, read again).
- `Models/UserProfile.cs`  
  Entity saved to SQL and cached as JSON in Redis.
- `Data/AppDbContext.cs`  
  EF Core DbContext + table mapping.
- `Data/UserRepository.cs`  
  Repo wrapper for insert and read.
- `appsettings.json`  
  SQL Server + Redis connection strings + TTL.

---

## Configuration

### `appsettings.json`

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=(localdb)\\MSSQLLocalDB;Database=RedisEfSqlPocDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "TtlSeconds": 600
  }
}
