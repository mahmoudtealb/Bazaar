
using System.ComponentModel;

namespace StudentBazaar.Entities.Repositories
{
    public interface ICategoryRepository : IGenericRepository<CategoryAttribute>
    {
        // Update existing category details
        void UpdateCategoryAttribute(CategoryAttribute categoryAttribute);
    }
}
