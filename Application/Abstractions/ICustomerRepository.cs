using Domain.Entities;

namespace Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Customer> GetOrCreateAsync(string fullName, string email, CancellationToken cancellationToken = default);
    void Add(Customer entity);
}
