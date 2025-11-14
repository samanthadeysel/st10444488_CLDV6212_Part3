using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Models;
using System.Text.Json;

namespace ST10444488_POE.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IConfiguration _config;
        private readonly string _tableName = "Products";

        public ProductsController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            string connectionString = _config["AzureStorage:ConnectionString"];
            var tableClient = new TableClient(connectionString, _tableName);
            await tableClient.CreateIfNotExistsAsync();

            var products = tableClient.Query<Product>().ToList();
            return View(products);
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            string connectionString = _config["AzureStorage:ConnectionString"];
            var tableClient = new TableClient(connectionString, _tableName);
            var product = await tableClient.GetEntityAsync<Product>(partitionKey, rowKey);
            return View(product.Value);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile ImageFile)
        {
            string connectionString = _config["AzureStorage:ConnectionString"];
            var tableClient = new TableClient(connectionString, _tableName);
            await tableClient.CreateIfNotExistsAsync();

            product.PartitionKey = "Bakery";
            product.RowKey = Guid.NewGuid().ToString();

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient("productimages");
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient($"{product.RowKey}_{ImageFile.FileName}");
                using var stream = ImageFile.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);

                product.ImageUrl = blobClient.Uri.ToString();
            }

            await tableClient.AddEntityAsync(product);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            string connectionString = _config["AzureStorage:ConnectionString"];
            var tableClient = new TableClient(connectionString, _tableName);
            var product = await tableClient.GetEntityAsync<Product>(partitionKey, rowKey);
            return View(product.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product)
        {
            string connectionString = _config["AzureStorage:ConnectionString"];
            var tableClient = new TableClient(connectionString, _tableName);
            await tableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            string connectionString = _config["AzureStorage:ConnectionString"];
            var tableClient = new TableClient(connectionString, _tableName);
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult AddToCart(string rowId, string name, decimal price, string imageUrl, string category, string sizes, string selectedSize)
        {
            var cartJson = HttpContext.Session.GetString("CART");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<Cart>()
                : JsonSerializer.Deserialize<List<Cart>>(cartJson) ?? new List<Cart>();

            var existingItem = cart.FirstOrDefault(c => c.RowKey == rowId && c.SelectedSize == selectedSize);
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
            }
            else
            {
                var cartItem = new Cart
                {
                    RowKey = rowId,
                    Name = name,
                    Price = price,
                    Quantity = 1,
                    ImageUrl = imageUrl,
                    Category = category,
                    SelectedSize = selectedSize,
                    Sizes = sizes
                };

                cart.Add(cartItem);
            }

            HttpContext.Session.SetString("CART", JsonSerializer.Serialize(cart));
            return RedirectToAction("Index", "Cart");
        }
    }
}