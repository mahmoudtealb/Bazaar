

namespace StudentBazaar.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductCategoryController : ControllerBase
    {
        private readonly IGenericRepository<ProductCategory> _repo;

        public ProductCategoryController(IGenericRepository<ProductCategory> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync(includeWord: "Products"));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(c => c.Id == id, includeWord: "Products");
            return entity == null ? NotFound() : Ok(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCategory entity)
        {
            await _repo.AddAsync(entity);
            await _repo.SaveAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductCategory entity)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(c => c.Id == id);
            if (existing == null) return NotFound();

            existing.CategoryName = entity.CategoryName;
            existing.UpdatedAt = DateTime.Now;

            await _repo.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(c => c.Id == id);
            if (existing == null) return NotFound();

            _repo.Remove(existing);
            await _repo.SaveAsync();
            return NoContent();
        }
    }
}
