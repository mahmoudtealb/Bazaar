namespace StudentBazaar.Entities.Repositories;

public interface IMajorRepository : IGenericRepository<Major>
{
    void Update(Major major);
}