
namespace StudentBazaar.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IGenericRepository<User> _repo;

        public UserController(IGenericRepository<User> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync(includeWord: "University,College,ListingsPosted,OrdersPlaced,RatingsGiven,ShipmentsHandled,ShoppingCartItems"));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(u => u.Id == id, includeWord: "University,College,ListingsPosted,OrdersPlaced,RatingsGiven,ShipmentsHandled,ShoppingCartItems");
            return entity == null ? NotFound() : Ok(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] User entity)
        {
            await _repo.AddAsync(entity);
            await _repo.SaveAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] User entity)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(u => u.Id == id);
            if (existing == null) return NotFound();

            existing.FullName = entity.FullName;
            existing.Email = entity.Email;
            existing.Phone = entity.Phone;
            existing.PasswordHash = entity.PasswordHash;
            existing.Role = entity.Role;
            existing.Address = entity.Address;
            existing.UniversityId = entity.UniversityId;
            existing.CollegeId = entity.CollegeId;
            existing.UpdatedAt = DateTime.Now;

            await _repo.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(u => u.Id == id);
            if (existing == null) return NotFound();

            _repo.Remove(existing);
            await _repo.SaveAsync();
            return NoContent();
        }
    }
}
