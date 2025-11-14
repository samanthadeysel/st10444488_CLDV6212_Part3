using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Models;
using System.Text.Json;

public class CartController : Controller
{
    private List<Cart> GetCart()
    {
        var data = HttpContext.Session.GetString("CART");
        return data == null ? new List<Cart>() : JsonSerializer.Deserialize<List<Cart>>(data) ?? new List<Cart>();
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
            var updated = new Cart
            {
                RowKey = item.RowKey,
                Name = item.Name,
                Price = item.Price,
                Quantity = qty,
                ImageUrl = item.ImageUrl
            };

            cart.Remove(item);
            cart.Add(updated);
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
}