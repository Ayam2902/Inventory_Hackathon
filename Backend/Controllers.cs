using Microsoft.AspNetCore.Mvc;
using InventoryApi.Models;
using InventoryApi.Repositories;

namespace InventoryApi.Controllers;

// ---------------------------------------------------------------
// PRODUCTS CONTROLLER  GET/POST/PUT/DELETE /api/products
// ---------------------------------------------------------------
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repo;
    public ProductsController(IProductRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Product>>>> GetAll()
    {
        var products = await _repo.GetAllAsync();
        return Ok(new ApiResponse<IEnumerable<Product>>(true, "OK", products));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<Product>>> GetById(int id)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product is null)
            return NotFound(new ApiResponse<Product>(false, "Product not found", null));
        return Ok(new ApiResponse<Product>(true, "OK", product));
    }

    [HttpGet("sku/{sku}")]
    public async Task<ActionResult<ApiResponse<Product>>> GetBySku(string sku)
    {
        var product = await _repo.GetBySkuAsync(sku);
        if (product is null)
            return NotFound(new ApiResponse<Product>(false, "Product not found", null));
        return Ok(new ApiResponse<Product>(true, "OK", product));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] CreateProductDto dto)
    {
        var id = await _repo.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id },
            new ApiResponse<int>(true, "Product created", id));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] CreateProductDto dto)
    {
        var result = await _repo.UpdateAsync(id, dto);
        if (!result) return NotFound(new ApiResponse<bool>(false, "Product not found", false));
        return Ok(new ApiResponse<bool>(true, "Product updated", true));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _repo.DeleteAsync(id);
        if (!result) return NotFound(new ApiResponse<bool>(false, "Product not found", false));
        return Ok(new ApiResponse<bool>(true, "Product deleted", true));
    }
}

// ---------------------------------------------------------------
// INVENTORY CONTROLLER  GET /api/inventory, PATCH /api/inventory/{id}/stock
// ---------------------------------------------------------------
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryRepository _repo;
    public InventoryController(IInventoryRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryItem>>>> GetAll()
    {
        var items = await _repo.GetAllAsync();
        return Ok(new ApiResponse<IEnumerable<InventoryItem>>(true, "OK", items));
    }

    [HttpGet("{productId:int}")]
    public async Task<ActionResult<ApiResponse<InventoryItem>>> GetByProduct(int productId)
    {
        var item = await _repo.GetByProductIdAsync(productId);
        if (item is null)
            return NotFound(new ApiResponse<InventoryItem>(false, "Not found", null));
        return Ok(new ApiResponse<InventoryItem>(true, "OK", item));
    }

    [HttpGet("alerts/low-stock")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryItem>>>> GetLowStock()
    {
        var items = await _repo.GetLowStockAsync();
        return Ok(new ApiResponse<IEnumerable<InventoryItem>>(true, "OK", items));
    }

    [HttpPatch("{productId:int}/stock")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStock(
        int productId, [FromBody] UpdateStockDto dto)
    {
        await _repo.UpdateStockAsync(productId, dto);
        return Ok(new ApiResponse<bool>(true, "Stock updated", true));
    }

    [HttpGet("{productId:int}/history")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StockTransaction>>>> GetHistory(int productId)
    {
        var txns = await _repo.GetTransactionHistoryAsync(productId);
        return Ok(new ApiResponse<IEnumerable<StockTransaction>>(true, "OK", txns));
    }
}

// ---------------------------------------------------------------
// CATEGORIES CONTROLLER
// ---------------------------------------------------------------
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repo;
    public CategoriesController(ICategoryRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Category>>>> GetAll()
    {
        var cats = await _repo.GetAllAsync();
        return Ok(new ApiResponse<IEnumerable<Category>>(true, "OK", cats));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] Category category)
    {
        var id = await _repo.CreateAsync(category);
        return Ok(new ApiResponse<int>(true, "Category created", id));
    }
}
