using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// I believe this was done for the sake of testing LINQ knowledge, but in case it's not, I'd suggest adding the filtering
// in all of these methods into the data layer itself instead of always getting every single result every single time.
// This helps keep requests smaller and lower memory usage.

namespace ProductCatalog.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductControllerCodeReview : ControllerBase
    {
        private readonly IProductServiceCodeReview _productService;

        public ProductControllerCodeReview(IProductServiceCodeReview productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(string category = null, int page = 1, int pageSize = 10, decimal? minPrice = null, decimal? maxPrice = null)
        {
            var allProducts = await _productService.GetAllProductsAsync();

            // Instead of iterating through each product, we can use LINQ queries to keep our code cleaner
            // and easier to modify in the future

            var filteredProducts = allProducts.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                filteredProducts = filteredProducts.Where(p => p.Category == category);
            
            // Filter on minimum price, inclusively
            if (minPrice.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Price >= minPrice.Value);
            
            // Filter on maximum price, inclusively
            if (maxPrice.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Price <= maxPrice.Value);

            // Get total before pagination
            var totalFilteredProducts = filteredProducts.Count();

            // Apply pagination
            var pagedProducts = filteredProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                Products = pagedProducts,
                TotalCount = allProducts.Count(),
                TotalFilteredCount = totalFilteredProducts,
                Page = page,
                PageSize = pageSize
            });
        }

        // Because this comment explicitly says they are doing manual validation, I wouldn't make any changes. Instead,
        // I would ask the dev why they opted for manual validation insteade of data annotations, as .NET supports things
        // like [Required], [MinLength], [Range], etc. OOTB. If there is a valid reason, then I would leave the changes.
        // Otherwise, I'd reject the PR so the model uses those and you could simply use:
        //
        // if (!ModelState.IsValid) {
        //     return BadRequest(ModelState);
        // }
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateRequestCodeReview request)
        {
            // Check for null body just in case a malformed requested was made
            if (request == null)
            {
                return BadRequest("Missing body");
            }

            // Manual validation instead of using data annotations
            if (string.IsNullOrEmpty(request.Name))
            {
                return BadRequest("Product name is required");
            }

            if (request.Name.Length < 3)
            {
                return BadRequest("Product name must be at least 3 characters");
            }

            if (request.Price <= 0)
            {
                return BadRequest("Product price must be greater than 0");
            }

            if (string.IsNullOrEmpty(request.Category))
            {
                return BadRequest("Product category is required");
            }

            var product = new ProductCodeReview
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Category = request.Category,
                InStock = request.InStock
            };

            // Forgot the `await` operator! I've done it plenty of times myself :)
            var createdProduct = await _productService.CreateProductAsync(product);

            // While it's not required, returning a 201 is the standard HTTP status code to signify something
            // was created successfully. 
            return StatusCode(StatusCodes.Status201Created, createdProduct);

            // Possible other fixes:
            // IF GetProductById() exists in another file:
            // return Created($"/api/path/to/products/{createdProduct.Id}", createdProduct);
            // IF you decide to add a GetProductById() method:
            // return CreatedAtAction(nameof(GetProductById), new { id = createdProduct.Id }, createdProduct)
        }

        [HttpGet("category/{categoryName}")]
        public async Task<IActionResult> GetProductsByCategory(string categoryName)
        {
            // Simplifying code to use LINQ instead of loop.
            var allProducts = await _productService.GetAllProductsAsync();
            var filteredProducts = allProducts
                .Where(p => p.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Optionally, you may also simplify it further with the following:
            //
            // var filteredProducts =
            //     await _productService
            //         .GetAllProductsAsync()
            //         .Where(p => p.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
            //         .ToList();
            //
            // return Ok(filteredProducts);

            return Ok(filteredProducts);
        }

        // Add in pagination to limit data and remain consistent with `GetProducts()` method
        // Add in missing `async` operator since it's needed for `GetAllProductsAsync()`, and update method name
        [HttpGet("search")]
        public async Task<IActionResult> SearchProductsAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            // Make sure a search is being provided. Otherwise, GetProducts() should be used
            if (string.IsNullOrEmpty(searchTerm))
            {
                return BadRequest("Search term must not be empty");
            }

            var allProducts = await _productService.GetAllProductsAsync();
            
            var filteredProducts = allProducts
                .Where(p =>
                    p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            var totalFilteredProducts = filteredProducts.Count();
            
            var paginatedResults = filteredProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new {
                Results = paginatedResults,
                TotalCount = allProducts.Count(),
                TotalFilteredCount = totalFilteredProducts,
                Page = page,
                PageSize = pageSize
            });
        }
    }

    // (NOTE: Just renaming this so VS doesn't throw errors - I wouldn't normally do this in a PR)
    public class ProductCreateRequestCodeReview
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public bool InStock { get; set; }
    }

    public class ProductCodeReview
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public bool InStock { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public interface IProductServiceCodeReview
    {
        Task<List<ProductCodeReview>> GetAllProductsAsync();
        Task<ProductCodeReview> CreateProductAsync(ProductCodeReview product);
    }
}