using Application.Abstractions;

using Domain.Entities;

using Microsoft.EntityFrameworkCore;



namespace Infrastructure.Persistence.Repositories;



public sealed class UserRepository(AppDbContext context) : IUserRepository

{

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>

        context.Users

            .Include(u => u.Role)

            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);



    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>

        context.Users

            .Include(u => u.Role)

            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);



    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>

        context.Users.AnyAsync(u => u.Email == email, cancellationToken);



    public async Task AddAsync(User user, CancellationToken cancellationToken = default)

    {

        context.Users.Add(user);

        await context.SaveChangesAsync(cancellationToken);

    }

}

