







using StudentBazaar.Web.Models;
using StudentBazaar.Web.Models.ViewModels;

namespace StudentBazaar.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _env = env;
        }

        // ======================
        // Register (GET)
        // ======================
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var model = new RegisterViewModel
            {
                Universities = await _context.Universities
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = u.UniversityName
                    })
                    .ToListAsync(),
                Colleges = new List<SelectListItem>()
            };

            return View(model); // => Views/Account/Register.cshtml
        }

        // ======================
        // Register (POST)
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Universities = await _context.Universities
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = u.UniversityName
                    })
                    .ToListAsync();

                model.Colleges = await _context.Colleges
                    .Where(c => c.UniversityId == model.UniversityId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.CollegeName
                    })
                    .ToListAsync();

                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                Email = model.Email,
                UserName = model.Email,
                UniversityId = model.UniversityId,
                CollegeId = model.CollegeId,
                Address = model.Address
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // أي حد بيسجل من الـ UI العام يبقى Student بس
                await _userManager.AddToRoleAsync(user, "Student");

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Product");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            // إعادة تحميل الـ dropdowns
            model.Universities = await _context.Universities
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.UniversityName
                })
                .ToListAsync();

            model.Colleges = await _context.Colleges
                .Where(c => c.UniversityId == model.UniversityId)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CollegeName
                })
                .ToListAsync();

            return View(model);
        }

        // ======================
        // Login (GET)
        // ======================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // ======================
        // Login (POST)
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                // مؤقتًا كله يروح على المنتجات
                return RedirectToAction("Index", "Product");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View(model);
        }

        // ======================
        // Logout
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Product");
        }

        // ======================
        // Get Colleges (AJAX)
        // ======================
        [HttpGet]
        public async Task<JsonResult> GetColleges(int universityId)
        {
            var colleges = await _context.Colleges
                .Where(c => c.UniversityId == universityId)
                .Select(c => new { c.Id, c.CollegeName })
                .ToListAsync();

            return Json(colleges);
        }

        // ======================
        // Profile (GET)
        // ======================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var universities = await _context.Universities
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.UniversityName,
                    Selected = user.UniversityId == u.Id
                })
                .ToListAsync();

            var colleges = user.UniversityId.HasValue
                ? await _context.Colleges
                    .Where(c => c.UniversityId == user.UniversityId.Value)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.CollegeName,
                        Selected = user.CollegeId == c.Id
                    })
                    .ToListAsync()
                : new List<SelectListItem>();

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                UniversityId = user.UniversityId,
                CollegeId = user.CollegeId,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Universities = universities,
                Colleges = colleges
            };

            return View(model);
        }

        // ======================
        // Profile (POST - Update Info)
        // ======================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string email, string phoneNumber, string address, int? universityId, int? collegeId, IFormFile? profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            user.FullName = fullName;
            user.Email = email;
            user.UserName = email;
            user.PhoneNumber = phoneNumber;
            user.Address = address;
            user.UniversityId = universityId;
            user.CollegeId = collegeId;

            // Handle profile picture upload
            if (profilePicture != null && profilePicture.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "profiles");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, user.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                var fileName = $"{user.Id}_{Guid.NewGuid()}{Path.GetExtension(profilePicture.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                user.ProfilePictureUrl = $"/images/profiles/{fileName}";
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewBag.Universities = await _context.Universities
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.UniversityName,
                    Selected = user.UniversityId == u.Id
                })
                .ToListAsync();

            ViewBag.Colleges = user.UniversityId.HasValue
                ? await _context.Colleges
                    .Where(c => c.UniversityId == user.UniversityId.Value)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.CollegeName,
                        Selected = user.CollegeId == c.Id
                    })
                    .ToListAsync()
                : new List<SelectListItem>();

            return View("Profile", user);
        }

        // ======================
        // Change Password
        // ======================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New password and confirmation do not match.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
                TempData["Error"] = error.Description;

            return RedirectToAction(nameof(Profile));
        }

        // ======================
        // Settings (Placeholder)
        // ======================
        [Authorize]
        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }
    }
}
