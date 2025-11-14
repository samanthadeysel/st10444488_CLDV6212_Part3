using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Models;
using ST10444488_POE.Storage_Services;
using System.Text.Json;

namespace ST10444488_POE.Controllers
{
    public class CustomersController : Controller
    {
        private readonly FunctionService _functionService;

        public CustomersController(FunctionService functionService)
        {
            _functionService = functionService;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _functionService.CallFunctionAsync("GetCustomers", null);

            if (string.IsNullOrWhiteSpace(result) || result.TrimStart().StartsWith("<"))
            {
                ViewBag.Error = "Failed to load customers.";
                return View(new List<Customer>());
            }

            var allCustomers = JsonSerializer.Deserialize<List<Customer>>(result);
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userCustomer = allCustomers.FirstOrDefault(c => c.IdentityUserId == userId);

            return View(userCustomer != null ? new List<Customer> { userCustomer } : new List<Customer>());
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var result = await _functionService.CallFunctionAsync("GetCustomer", new { PartitionKey = partitionKey, RowKey = rowKey });
            var customer = JsonSerializer.Deserialize<Customer>(result);
            return View(customer);
        }

        public async Task<IActionResult> Create()
        {
            var result = await _functionService.CallFunctionAsync("GetProducts", null);
            var products = JsonSerializer.Deserialize<List<Product>>(result);
            ViewBag.Products = products;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            customer.IdentityUserId = userId;

            customer.RowKey = Guid.NewGuid().ToString();
            customer.PartitionKey = "Customer";

            await _functionService.CallFunctionAsync("InsertCustomer", customer);
            return RedirectToAction("Details", new { partitionKey = customer.PartitionKey, rowKey = customer.RowKey });
        }

        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var result = await _functionService.CallFunctionAsync("GetCustomer", new { PartitionKey = partitionKey, RowKey = rowKey });
            var customer = JsonSerializer.Deserialize<Customer>(result);
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Customer updated)
        {
            try
            {
                await _functionService.CallFunctionAsync("UpdateCustomer", updated);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                return View(updated);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var result = await _functionService.CallFunctionAsync("GetCustomer", new { PartitionKey = partitionKey, RowKey = rowKey });
            var customer = JsonSerializer.Deserialize<Customer>(result);
            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _functionService.CallFunctionAsync("DeleteCustomer", new { PartitionKey = partitionKey, RowKey = rowKey });
            return RedirectToAction(nameof(Index));
        }
    }
}