

namespace StudentBazaar.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly IGenericRepository<Rating> _repo;

        public RatingController(IGenericRepository<Rating> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync(includeWord: "User,Product"));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(r => r.Id == id, includeWord: "User,Product");
            return entity == null ? NotFound() : Ok(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Rating entity)
        {
            await _repo.AddAsync(entity);
            await _repo.SaveAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Rating entity)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(r => r.Id == id);
            if (existing == null) return NotFound();

            existing.UserId = entity.UserId;
            existing.ProductId = entity.ProductId;
            existing.Stars = entity.Stars;
            existing.Comment = entity.Comment;
            existing.UpdatedAt = DateTime.Now;

            await _repo.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(r => r.Id == id);
            if (existing == null) return NotFound();

            _repo.Remove(existing);
            await _repo.SaveAsync();
            return NoContent();
        }
    }
}
