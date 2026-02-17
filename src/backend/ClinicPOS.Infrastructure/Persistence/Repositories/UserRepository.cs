using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
            .FirstOrDefaultAsync(u => u.Username == username, ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }
}
