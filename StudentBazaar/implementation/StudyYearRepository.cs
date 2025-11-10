
namespace StudentBazaar.Web.implementation;

public class StudyYearRepository : GenericRepository<StudyYear>, IStudyYearRepository
{
    private readonly ApplicationDbContext _context;
    public StudyYearRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public void Update(StudyYear studyYear)
    {
        _context.StudyYears.Update(studyYear);
    }
}