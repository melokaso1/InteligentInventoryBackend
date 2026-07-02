using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(AppDbContext context) : ICustomerRepository
{
    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.Customers.AsNoTracking().FirstOrDefaultAsync(
            c => c.Email.ToLower() == email.Trim().ToLowerInvariant(),
            cancellationToken);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Customer> GetOrCreateAsync(
        string fullName,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await context.Customers.FirstOrDefaultAsync(
            c => c.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = normalizedEmail,
            CreatedAt = DateTime.UtcNow,
        };

        context.Customers.Add(customer);
        return customer;
    }

    public void Add(Customer entity) => context.Customers.Add(entity);
}
