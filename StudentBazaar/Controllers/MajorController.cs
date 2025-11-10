
namespace StudentBazaar.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MajorController : ControllerBase
    {
        private readonly IGenericRepository<Major> _repo;

        public MajorController(IGenericRepository<Major> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync(includeWord: "College,StudyYears"));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(m => m.Id == id, includeWord: "College,StudyYears");
            return entity == null ? NotFound() : Ok(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Major entity)
        {
            await _repo.AddAsync(entity);
            await _repo.SaveAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Major entity)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(m => m.Id == id);
            if (existing == null) return NotFound();

            existing.MajorName = entity.MajorName;
            existing.CollegeId = entity.CollegeId;
            existing.UpdatedAt = DateTime.Now;

            await _repo.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(m => m.Id == id);
            if (existing == null) return NotFound();

            _repo.Remove(existing);
            await _repo.SaveAsync();
            return NoContent();
        }
    }
}
