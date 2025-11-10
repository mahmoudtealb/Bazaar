

namespace StudentBazaar.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingCartItemController : ControllerBase
    {
        private readonly IGenericRepository<ShoppingCartItem> _repo;

        public ShoppingCartItemController(IGenericRepository<ShoppingCartItem> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync(includeWord: "User,Listing"));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(s => s.Id == id, includeWord: "User,Listing");
            return entity == null ? NotFound() : Ok(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShoppingCartItem entity)
        {
            await _repo.AddAsync(entity);
            await _repo.SaveAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ShoppingCartItem entity)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return NotFound();

            existing.UserId = entity.UserId;
            existing.ListingId = entity.ListingId;
            existing.Quantity = entity.Quantity;
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
