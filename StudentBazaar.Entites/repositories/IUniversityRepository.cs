namespace StudentBazaar.Entities.Repositories;

public interface IUniversityRepository: IGenericRepository<University>
{
    void Update(University university);
}
