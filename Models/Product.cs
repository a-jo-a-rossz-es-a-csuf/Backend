using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsApi.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("category_id")]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("sku")]
        public string Sku { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("stock_quantity")]
        public int StockQuantity { get; set; }

        [MaxLength(255)]
        [Column("image_url")]
        public string ImageUrl { get; set; }

        [MaxLength(100)]
        [Column("manufacturer")]
        public string Manufacturer { get; set; }

        [Column("warranty_months")]
        public int? WarrantyMonths { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<ProductCompatibility> Compatibilities { get; set; }
    }
}
