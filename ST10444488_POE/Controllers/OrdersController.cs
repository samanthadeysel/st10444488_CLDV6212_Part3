using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Models;
using ST10444488_POE.Storage_Services;
using System.Text.Json;

namespace ST10444488_POE.Controllers
{
    public class OrdersController : Controller
    {
        private readonly FunctionService _functionService;

        public OrdersController(FunctionService functionService)
        {
            _functionService = functionService;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _functionService.CallFunctionAsync("GetOrders", null);

            if (string.IsNullOrWhiteSpace(result) || result.TrimStart().StartsWith("<"))
            {
                ViewBag.Error = "Failed to load orders.";
                return View(new List<Order>());
            }

            var orders = JsonSerializer.Deserialize<List<Order>>(result);
            return View(orders);
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var result = await _functionService.CallFunctionAsync("GetOrder", new { PartitionKey = partitionKey, RowKey = rowKey });

            if (string.IsNullOrWhiteSpace(result) || result.TrimStart().StartsWith("<"))
            {
                ViewBag.Error = "Order details could not be loaded.";
                return View(new Order());
            }

            var order = JsonSerializer.Deserialize<Order>(result);
            return View(order);
        }

        public async Task<IActionResult> Create()
        {
            var customersJson = await _functionService.CallFunctionAsync("GetCustomers", null);
            var productsJson = await _functionService.CallFunctionAsync("GetProducts", null);

            ViewBag.Customers = customersJson.TrimStart().StartsWith("<") ? new List<Customer>() : JsonSerializer.Deserialize<List<Customer>>(customersJson);
            ViewBag.Products = productsJson.TrimStart().StartsWith("<") ? new List<Product>() : JsonSerializer.Deserialize<List<Product>>(productsJson);

            return View(new Order());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Order order)
        {
            var customersJson = await _functionService.CallFunctionAsync("GetCustomers", null);
            var productsJson = await _functionService.CallFunctionAsync("GetProducts", null);

            var customers = customersJson.TrimStart().StartsWith("<") ? new List<Customer>() : JsonSerializer.Deserialize<List<Customer>>(customersJson);
            var products = productsJson.TrimStart().StartsWith("<") ? new List<Product>() : JsonSerializer.Deserialize<List<Product>>(productsJson);

            ViewBag.Customers = customers;
            ViewBag.Products = products;

            var selectedKeys = order.ProductRowKeys?.Split(',') ?? Array.Empty<string>();
            var selectedProducts = products.Where(p => selectedKeys.Contains(p.RowKey)).ToList();

            order.Quantity = selectedProducts.Count;
            order.TotalCost = selectedProducts.Sum(p => (decimal)p.Price);
            order.ProductNames = string.Join(", ", selectedProducts.Select(p => p.Name));

            if (string.IsNullOrEmpty(order.CustomerRowKey) || selectedProducts.Count == 0)
            {
                ModelState.AddModelError("", "Please select a customer and at least one product.");
                return View(order);
            }

            var customer = customers.FirstOrDefault(c => c.RowKey == order.CustomerRowKey);
            if (customer == null)
            {
                ModelState.AddModelError("", "Customer not found.");
                return View(order);
            }

            order.FirstName = customer.FirstName;
            order.LastName = customer.LastName;
            order.Address = customer.Address;

            order.RowKey = Guid.NewGuid().ToString();
            order.PartitionKey = "Order";
            order.OrderDate = DateTime.Now;

            await _functionService.CallFunctionAsync("InsertOrder", order);
            await _functionService.CallFunctionAsync("WriteToQueue", order);

            return RedirectToAction("Details", new { partitionKey = order.PartitionKey, rowKey = order.RowKey });
        }

        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var orderJson = await _functionService.CallFunctionAsync("GetOrder", new { PartitionKey = partitionKey, RowKey = rowKey });
            var customersJson = await _functionService.CallFunctionAsync("GetCustomers", null);
            var productsJson = await _functionService.CallFunctionAsync("GetProducts", null);

            ViewBag.Customers = customersJson.TrimStart().StartsWith("<") ? new List<Customer>() : JsonSerializer.Deserialize<List<Customer>>(customersJson);
            ViewBag.Products = productsJson.TrimStart().StartsWith("<") ? new List<Product>() : JsonSerializer.Deserialize<List<Product>>(productsJson);

            var order = orderJson.TrimStart().StartsWith("<") ? new Order() : JsonSerializer.Deserialize<Order>(orderJson);
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Order updated)
        {
            var customersJson = await _functionService.CallFunctionAsync("GetCustomers", null);
            var productsJson = await _functionService.CallFunctionAsync("GetProducts", null);

            var customers = customersJson.TrimStart().StartsWith("<") ? new List<Customer>() : JsonSerializer.Deserialize<List<Customer>>(customersJson);
            var products = productsJson.TrimStart().StartsWith("<") ? new List<Product>() : JsonSerializer.Deserialize<List<Product>>(productsJson);

            ViewBag.Customers = customers;
            ViewBag.Products = products;

            var selectedKeys = updated.ProductRowKeys?.Split(',') ?? Array.Empty<string>();
            var selectedProducts = products.Where(p => selectedKeys.Contains(p.RowKey)).ToList();

            updated.ProductNames = string.Join(", ", selectedProducts.Select(p => p.Name));
            updated.Quantity = selectedProducts.Count;
            updated.TotalCost = selectedProducts.Sum(p => (decimal)p.Price);

            var customer = customers.FirstOrDefault(c => c.RowKey == updated.CustomerRowKey);
            if (customer != null)
            {
                updated.FirstName = customer.FirstName;
                updated.LastName = customer.LastName;
                updated.Address = customer.Address;
            }

            await _functionService.CallFunctionAsync("UpdateOrder", updated);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var result = await _functionService.CallFunctionAsync("GetOrder", new { PartitionKey = partitionKey, RowKey = rowKey });

            if (string.IsNullOrWhiteSpace(result) || result.TrimStart().StartsWith("<"))
            {
                ViewBag.Error = "Order could not be loaded.";
                return View(new Order());
            }

            var order = JsonSerializer.Deserialize<Order>(result);
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _functionService.CallFunctionAsync("DeleteOrder", new { PartitionKey = partitionKey, RowKey = rowKey });
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Process(string partitionKey, string rowKey)
        {
            var result = await _functionService.CallFunctionAsync("GetOrder", new { PartitionKey = partitionKey, RowKey = rowKey });

            if (string.IsNullOrWhiteSpace(result) || result.TrimStart().StartsWith("<"))
            {
                ViewBag.Error = "Order could not be loaded.";
                return View(new Order());
            }

            var order = JsonSerializer.Deserialize<Order>(result);
            return View(order);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Process(Order updated)
        {
            // Only update status
            var result = await _functionService.CallFunctionAsync("GetOrder", new { PartitionKey = updated.PartitionKey, RowKey = updated.RowKey });
            var existing = JsonSerializer.Deserialize<Order>(result);

            existing.Status = updated.Status;

            await _functionService.CallFunctionAsync("UpdateOrder", existing);
            return RedirectToAction(nameof(Index));
        }
    }
}