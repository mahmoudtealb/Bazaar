namespace StudentBazaar.Entities.Repositories;

public interface IRatingRepository : IGenericRepository<Rating>
{
    void Update(Rating rating);

}
