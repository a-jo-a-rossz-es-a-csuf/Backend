using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoPartsApi.Data;
using AutoPartsApi.Models;

namespace AutoPartsApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
            [FromQuery] int? categoryId,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(products);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            return Ok(product);
        }


        [HttpGet("search/by-car")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchByCarModel(
            [FromQuery] int brandId,
            [FromQuery] int? modelId)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Compatibilities)
                    .ThenInclude(c => c.CarModel)
                        .ThenInclude(m => m.Brand)
                .Where(p => p.IsActive);

            if (modelId.HasValue)
            {
                query = query.Where(p => p.Compatibilities.Any(c => c.ModelId == modelId.Value));
            }
            else
            {
                query = query.Where(p => p.Compatibilities.Any(c => c.CarModel.BrandId == brandId));
            }

            var products = await query.ToListAsync();
            return Ok(products);
        }
    }
}
