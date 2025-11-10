namespace StudentBazaar.Web.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    void Update(User user);
    Task<User?> GetByEmailAsync(string email);
}