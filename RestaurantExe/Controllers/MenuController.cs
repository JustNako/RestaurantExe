using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Data;
using Restaurant.Entities;
using RestaurantExe.Helpers;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantExe.Controllers
{
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;
        private const string CartSessionKey = "CartItems";

        public MenuController(AppDbContext context)
        {
            _context = context;
        }

        // --- Cart Helper Methods ---
        private List<CartItemDto> GetCartFromSession()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemDto>>(CartSessionKey);
            return cart ?? new List<CartItemDto>();
        }
        
        private void SaveCart(List<CartItemDto> cart)
        {
            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
        }

        // --- Cart API Endpoints ---
        [HttpGet]
        public IActionResult GetCart()
        {
            var cartData = GetCartFromSession();
            return Json(cartData);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            var product = _context.Products.Find(productId);
            if (product == null || !product.IsAvailable)
                return Json(new { success = false, message = "Product not found or unavailable" });

            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                item.Quantity++;
            }
            else
            {
                cart.Add(new CartItemDto
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    ImagePath = product.ImagePath
                });
            }

            SaveCart(cart);
            return Json(cart);
        }

        [HttpPost]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item == null)
                return Json(new { success = false, message = "Item not found in cart" });

            if (quantity <= 0)
                cart.Remove(item);
            else
                item.Quantity = quantity;

            SaveCart(cart);
            return Json(cart);
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return Json(cart);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOrder(string qrCode)
        {
            try
            {
                var cart = GetCartFromSession();
                if (cart == null || cart.Count == 0)
                    return Json(new { success = false, message = "Корзина пуста" });

                // Find table by qrCode
                var qrLink = await _context.QrLinks.FirstOrDefaultAsync(q => q.Code == qrCode);
                if (qrLink == null || qrLink.TableId == null)
                    return Json(new { success = false, message = "Стол не найден" });

                // Create Order
                var order = new Order
                {
                    TableId = qrLink.TableId.Value,
                    CreatedAt = DateTime.Now,
                    Status = OrderStatus.New,
                    TotalPrice = cart.Sum(x => x.Price * x.Quantity)
                };

                // Create OrderItems
                foreach (var cartItem in cart)
                {
                    order.Items.Add(new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Price
                    });
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Mark table as occupied
                var table = await _context.Tables.FindAsync(qrLink.TableId.Value);
                if (table != null)
                {
                    table.IsOccupied = true;
                    await _context.SaveChangesAsync();
                }

                // Clear cart
                SaveCart(new List<CartItemDto>());

                return Json(new { success = true, message = "Заказ успешно отправлен!", orderId = order.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ошибка: " + ex.Message });
            }
        }

        // --- Public Menu Display ---
        [HttpGet]
        public async Task<IActionResult> Index(string qrCode)
        {
            if (string.IsNullOrEmpty(qrCode))
            {
                return NotFound("QR code is missing.");
            }

            var qrLink = await _context.QrLinks.FirstOrDefaultAsync(q => q.Code == qrCode);
            if (qrLink == null || qrLink.TableId == null)
            {
                return NotFound("Invalid QR code or table not associated.");
            }

            // Pass table information to the view
            var table = await _context.Tables.FindAsync(qrLink.TableId);
            ViewData["TableNumber"] = table?.Number ?? "N/A";
            ViewData["QrCode"] = qrCode;

            var categories = await _context.Categories
                .Include(c => c.Products.Where(p => p.IsAvailable))
                .Where(c => c.Products.Any(p => p.IsAvailable))
                .ToListAsync();

            return View(categories);
        }
    }
}
