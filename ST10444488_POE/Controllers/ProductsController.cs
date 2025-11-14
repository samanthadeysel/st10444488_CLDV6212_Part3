using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Storage_Services;
using System.Text.Json;
using ST10444488_POE.Models;
using ST10444488_POE.Storage_Services;

public class ProductsController : Controller
{
    private readonly FunctionService _functionService;

    public ProductsController(FunctionService functionService)
    {
        _functionService = functionService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var result = await _functionService.CallFunctionAsync("GetProductList", null);
        if (string.IsNullOrWhiteSpace(result) || result.StartsWith("<"))
        {
            ViewBag.Error = "Unable to load products.";
            return View(new List<Product>());
        }

        var products = JsonSerializer.Deserialize<List<Product>>(result);
        return View(products);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Product product, IFormFile ImageFile)
    {
        product.RowKey = Guid.NewGuid().ToString();

        if (ImageFile != null && ImageFile.Length > 0)
        {
            using var ms = new MemoryStream();
            await ImageFile.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());

            var uploadRequest = new
            {
                ProductId = product.RowKey,
                FileName = ImageFile.FileName,
                FileData = base64
            };

            var resultJson = await _functionService.CallFunctionAsync("UploadProductImage", uploadRequest);
            if (string.IsNullOrWhiteSpace(resultJson) || resultJson.StartsWith("<"))
            {
                ViewBag.Error = "Image upload failed.";
                return View(product);
            }

            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(resultJson);
            product.ImageUrl = result["imageUrl"];
        }

        await _functionService.CallFunctionAsync("InsertProduct", product);
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(string partitionKey, string rowKey)
    {
        var result = await _functionService.CallFunctionAsync("GetProductDetails", new { PartitionKey = partitionKey, RowKey = rowKey });
        if (string.IsNullOrWhiteSpace(result) || result.StartsWith("<"))
        {
            ViewBag.Error = "Product not found.";
            return View(new Product());
        }

        var product = JsonSerializer.Deserialize<Product>(result);
        return View(product);
    }

    [Authorize]
    public async Task<IActionResult> AddToCart(string partitionKey, string rowKey)
    {
        var result = await _functionService.CallFunctionAsync("GetProductDetails", new { PartitionKey = partitionKey, RowKey = rowKey });
        var product = JsonSerializer.Deserialize<Product>(result);

        var cartJson = HttpContext.Session.GetString("Cart");
        var cart = string.IsNullOrEmpty(cartJson)
            ? new List<Product>()
            : JsonSerializer.Deserialize<List<Product>>(cartJson);

        cart.Add(product);

        HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));

        return RedirectToAction("Index", "Cart");
    }
}