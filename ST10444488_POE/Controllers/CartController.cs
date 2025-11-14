using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using ST10444488_POE.Models;
using ST10444488_POE.Storage_Services;
using System.Text.Json;

namespace ST10444488_POE.Controllers
{
    public class CartController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly FunctionService _functionService;

        public CartController(IConfiguration configuration, FunctionService functionService)
        {
            _configuration = configuration;
            _functionService = functionService;
        }

        private List<Cart> GetCart()
        {
            var data = HttpContext.Session.GetString("CART");
            return string.IsNullOrEmpty(data)
                ? new List<Cart>()
                : JsonSerializer.Deserialize<List<Cart>>(data) ?? new List<Cart>();
        }

        private void SaveCart(List<Cart> cart)
        {
            var json = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("CART", json);
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            ViewBag.Total = cart.Sum(c => c.LineTotal);
            return View(cart.OrderBy(c => c.RowKey).ToList());
        }

        [HttpPost]
        public IActionResult Update(string rowKey, int qty)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.RowKey == rowKey);
            if (item == null) return RedirectToAction(nameof(Index));

            if (qty <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                item.Quantity = qty;
            }

            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove("CART");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            var customerResult = await _functionService.CallFunctionAsync("TableStorage", new
            {
                Operation = "Query",
                PartitionKey = "Customer",
                Filter = $"RowKey eq '{User.Identity.Name}'"
            });

            if (string.IsNullOrWhiteSpace(customerResult) || customerResult.TrimStart().StartsWith("<"))
            {
                TempData["OrderSuccess"] = "Customer lookup failed.";
                return RedirectToAction("Index");
            }

            var customers = JsonSerializer.Deserialize<List<Customer>>(customerResult);
            var customer = customers?.FirstOrDefault();

            if (customer == null)
            {
                TempData["OrderSuccess"] = "Customer not found.";
                return RedirectToAction("Index");
            }

            var order = new Order
            {
                PartitionKey = "Order",
                RowKey = Guid.NewGuid().ToString(),
                CustomerRowKey = customer.RowKey,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Address = customer.Address,
                ProductRowKeys = string.Join(",", cart.Select(c => c.RowKey)),
                ProductNames = string.Join(", ", cart.Select(c => c.Name)),
                Quantity = cart.Sum(c => c.Quantity),
                TotalCost = cart.Sum(c => c.LineTotal),
                OrderDate = DateTime.Now,
                Status = "Pending"
            };

            await _functionService.CallFunctionAsync("TableStorage", new
            {
                Operation = "Insert",
                Entity = order
            });

            await _functionService.CallFunctionAsync("WriteToQueue", order);

            HttpContext.Session.Remove("CART");
            TempData["OrderSuccess"] = "Your order has been placed successfully!";
            return RedirectToAction("Details", "Orders", new { partitionKey = order.PartitionKey, rowKey = order.RowKey });
        }
    }
}