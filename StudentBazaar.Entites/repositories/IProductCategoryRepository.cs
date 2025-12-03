namespace StudentBazaar.Entities.Repositories;

public interface IProductCategoryRepository : IGenericRepository<ProductCategory>
{
    void Update(ProductCategory productCategory);
}

