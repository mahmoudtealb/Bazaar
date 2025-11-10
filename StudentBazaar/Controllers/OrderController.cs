
namespace StudentBazaar.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IGenericRepository<Order> _repo;

        public OrderController(IGenericRepository<Order> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync(includeWord: "Listing,Buyer,Shipment"));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(o => o.Id == id, includeWord: "Listing,Buyer,Shipment");
            return entity == null ? NotFound() : Ok(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Order entity)
        {
            await _repo.AddAsync(entity);
            await _repo.SaveAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Order entity)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(o => o.Id == id);
            if (existing == null) return NotFound();

            existing.ListingId = entity.ListingId;
            existing.BuyerId = entity.BuyerId;
            existing.OrderDate = entity.OrderDate;
            existing.Status = entity.Status;
            existing.PaymentMethod = entity.PaymentMethod;
            existing.TotalAmount = entity.TotalAmount;
            existing.SiteCommission = entity.SiteCommission;
            existing.UpdatedAt = DateTime.Now;

            await _repo.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(o => o.Id == id);
            if (existing == null) return NotFound();

            _repo.Remove(existing);
            await _repo.SaveAsync();
            return NoContent();
        }
    }
}
