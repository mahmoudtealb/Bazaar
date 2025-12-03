namespace StudentBazaar.Entities.Repositories;

public interface ICollegeRepository :IGenericRepository<College>
{
  void Update (College college);    
}