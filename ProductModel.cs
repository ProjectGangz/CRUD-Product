using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerceClothing.Models
{
	public class ProductModel
	{
		[Key]
		public int ProductID { get; set; }

		[ForeignKey("Category")]
		public int CategoryID { get; set; }
        public CategoryModel? Category { get; set; }
        [Required]
		[MaxLength(100)]
		public string? ProductName { get; set; }

		public string? ProductDescription { get; set; }

		[Required]
		[Column(TypeName = "decimal(10, 2)")]
		public decimal Price { get; set; }

		[MaxLength(50)]
		public string? Size { get; set; }

		[MaxLength(50)]
		public string? Color { get; set; }

		public string? ProductImage { get; set; }
        public IFormFile? ImageFile { get; set; }


        [Required]
		public int Quantity { get; set; }

		// Navigation property
		
	}
}

