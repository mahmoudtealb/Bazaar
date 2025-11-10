
namespace StudentBazaar.Web.Implementation
{
    public class UniversityRepository : GenericRepository<University>, IUniversityRepository
    {
        private readonly ApplicationDbContext _context;

        public UniversityRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void Update(University university)
        {
            // ممكن تستخدم نفس الأسلوب بتاع الكليات
            _context.Universities.Update(university);
        }
    }
}
