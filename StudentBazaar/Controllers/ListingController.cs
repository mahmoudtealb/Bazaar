

namespace StudentBazaar.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListingController : ControllerBase
    {
        private readonly IGenericRepository<Listing> _repo;

        public ListingController(IGenericRepository<Listing> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync(includeWord: "Product,Seller,Orders,ShoppingCartItems"));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(l => l.Id == id, includeWord: "Product,Seller,Orders,ShoppingCartItems");
            return entity == null ? NotFound() : Ok(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Listing entity)
        {
            await _repo.AddAsync(entity);
            await _repo.SaveAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Listing entity)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(l => l.Id == id);
            if (existing == null) return NotFound();

            existing.Price = entity.Price;
            existing.Condition = entity.Condition;
            existing.Description = entity.Description;
            existing.Discount = entity.Discount;
            existing.Status = entity.Status;
            existing.PostingDate = entity.PostingDate;
            existing.ProductId = entity.ProductId;
            existing.SellerId = entity.SellerId;
            existing.UpdatedAt = DateTime.Now;

            await _repo.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(l => l.Id == id);
            if (existing == null) return NotFound();

            _repo.Remove(existing);
            await _repo.SaveAsync();
            return NoContent();
        }
    }
}
