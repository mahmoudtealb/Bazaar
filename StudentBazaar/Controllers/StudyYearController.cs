
namespace StudentBazaar.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudyYearController : ControllerBase
    {
        private readonly IGenericRepository<StudyYear> _repo;

        public StudyYearController(IGenericRepository<StudyYear> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync(includeWord: "Major,Products"));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(s => s.Id == id, includeWord: "Major,Products");
            return entity == null ? NotFound() : Ok(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StudyYear entity)
        {
            await _repo.AddAsync(entity);
            await _repo.SaveAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] StudyYear entity)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return NotFound();

            existing.YearName = entity.YearName;
            existing.MajorId = entity.MajorId;
            existing.UpdatedAt = DateTime.Now;

            await _repo.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return NotFound();

            _repo.Remove(existing);
            await _repo.SaveAsync();
            return NoContent();
        }
    }
}
