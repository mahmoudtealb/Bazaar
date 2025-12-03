

namespace StudentBazaar.Web.ViewModels
{
    public class ProductCreateViewModel
    {
        public Product Product { get; set; } = new Product();

        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();

        public List<SelectListItem> Universities { get; set; } = new List<SelectListItem>();

        public List<SelectListItem> Colleges { get; set; } = new List<SelectListItem>();

        public List<IFormFile>? Files { get; set; }
    }
}
