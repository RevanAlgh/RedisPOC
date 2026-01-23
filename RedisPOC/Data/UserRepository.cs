using Microsoft.EntityFrameworkCore;
using RedisPOC.Models;

namespace RedisPOC.Data
{
    public class UserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db) => _db = db;

        public async Task<UserProfile> InsertAsync(UserProfile user, CancellationToken ct)
        {
            _db.UserProfiles.Add(user);
            await _db.SaveChangesAsync(ct);
            return user;
        }

        public Task<UserProfile?> GetAsync(Guid id, CancellationToken ct)
        {
            return _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        }
    }
}
