namespace StudentBazaar.Web.Repositories;

public interface IStudyYearRepository : IGenericRepository<StudyYear>
{
    void Update(StudyYear studyYear);
}
