using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Authorization;
using Restaurant.Data;
using Restaurant.Entities;
using System.Diagnostics;

namespace Restaurant.Areas.Admin.Controllers

{
    [Area("Admin")]
    
    public class CashierController : Controller
    {
        private readonly AppDbContext _context;

        public CashierController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Policy = Permissions.Cash.View)]
        public async Task<IActionResult> Index()
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            var servicePercent = settings?.ServiceChargePercentage ?? 0m;

            var orders = await _context.Orders
                .Where(o => !o.IsClosed && o.Status != OrderStatus.Cancelled)
                .Include(o => o.Table)
                .Include(o => o.Items)

                .ThenInclude(i => i.Product)

                .ToListAsync();

            foreach (var order in orders)
            {
                order.Items ??= new List<OrderItem>();

                var itemsTotal = order.Items.Sum(i => i.Price * i.Quantity);
                if (order.Place == Place.DineIn)
                {
                    order.ServicePercent = servicePercent;
                    order.ServiceAmount = Math.Round(order.ItemsTotal * servicePercent / 100m, 2);
                }
                else
                {
                    order.ServicePercent = 0m;
                    order.ServiceAmount = 0m;
                }

                order.TotalAmount = Math.Max(0,
                    order.ItemsTotal + order.ServiceAmount - order.Discount
                );
            }


            return View(orders);
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Cash.Close)]
        public async Task<IActionResult> CloseOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (!order.IsClosed)
            {
                var settings = await _context.Settings.FirstOrDefaultAsync();
                var servicePercent = settings?.ServiceChargePercentage ?? 0m;

                order.ItemsTotal = order.Items.Sum(i => i.Price * i.Quantity);

                if (order.Place == Place.DineIn)
                {
                    order.ServicePercent = servicePercent;
                    order.ServiceAmount = Math.Round(order.ItemsTotal * servicePercent / 100m, 2);
                }
                else
                {
                    order.ServicePercent = 0m;
                    order.ServiceAmount = 0m;
                }

                order.TotalAmount = Math.Max(0,
                    order.ItemsTotal + order.ServiceAmount - order.Discount
                );


                await _context.SaveChangesAsync();
            }


            return View(order);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Cash.Close)]
        public async Task<IActionResult> CloseOrder(int id, decimal discount, PaymentType paymentType, decimal? paidCash, decimal? paidCard)
        {
            Debug.WriteLine("CloseOrder POST action started.");
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            

            if (order == null)
            {
                Debug.WriteLine($"Order with id {id} not found.");
                return NotFound();
            }
            Debug.WriteLine($"Order with id {id} found.");

            /*
            // Проверим наличие ингридиентов по рецептам и подготовим списание
            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var recipes = await _context.RecipeItems
                .Where(ri => productIds.Contains(ri.ProductId))
                .Include(ri => ri.Ingredient)
                .ToListAsync();

            
            // агрегируем списание по ингредиентам
            var requiredByIngredient = new Dictionary<int, decimal>();
            foreach (var item in order.Items)
            {
                var productRecipe = recipes.Where(r => r.ProductId == item.ProductId);
                foreach (var r in productRecipe)
                {
                    var qty = r.QuantityPerUnit * item.Quantity;
                    if (!requiredByIngredient.ContainsKey(r.IngredientId)) requiredByIngredient[r.IngredientId] = 0;
                    requiredByIngredient[r.IngredientId] += qty;
                }
            }

            // проверка остатков
            foreach (var kv in requiredByIngredient)
            {
                var ing = recipes.First(r => r.IngredientId == kv.Key).Ingredient;
                if (ing.CurrentQuantity < kv.Value)
                {
                    TempData["ErrorMessage"] = $"Недостаточно ингредиента '{ing.Name}'. Требуется {kv.Value} {ing.Unit}, есть {ing.CurrentQuantity} {ing.Unit}.";
                    Debug.WriteLine($"Not enough ingredient '{ing.Name}'. Required {kv.Value} {ing.Unit}, have {ing.CurrentQuantity} {ing.Unit}.");
                    return RedirectToAction("Index");
                }
            }
            */



            // обновляем только скидку и итог
            order.Discount = discount;
            order.TotalAmount = Math.Max(0,
                order.ItemsTotal + order.ServiceAmount - discount
            );

            var finalAmount = order.TotalAmount;
            order.PaymentType = paymentType;


            // Обработка частичных платежей
            if (paymentType == PaymentType.Mixed)
            {
                var cash = Math.Max(0m, paidCash ?? 0m);
                var card = Math.Max(0m, paidCard ?? 0m);

                if (Math.Abs(cash + card - finalAmount) > 0.01m)
                {
                    TempData["ErrorMessage"] =
                        $"Сумма наличными и картой должна равняться {finalAmount:N2} ₸";
                    return RedirectToAction(nameof(CloseOrder), new { id });
                }

                order.PaidCash = cash;
                order.PaidCard = card;
            }

            else if (paymentType == PaymentType.Cash)
            {
                order.PaidCash = finalAmount;
                order.PaidCard = 0m;
            }
            else if (paymentType == PaymentType.Card)
            {
                order.PaidCash = 0m;
                order.PaidCard = finalAmount;
            }
            else if (paymentType == PaymentType.Online)
            {
                order.PaidCash = 0m;
                order.PaidCard = 0m;
            }
            
            order.IsClosed = true;
            order.Status = OrderStatus.Completed; // Обновляем статус на Completed

            // Освобождаем стол при закрытии заказа
            if (order.Table != null)
            {
                order.Table.IsOccupied = false;
            }

            /*
            // списываем ингредиенты и создаем движения
            var lowAfterDeduct = new List<string>();
            foreach (var kv in requiredByIngredient)
            {
                var ing = recipes.First(r => r.IngredientId == kv.Key).Ingredient;
                ing.CurrentQuantity -= kv.Value;
                _context.StockMovements.Add(new StockMovement
                {
                    IngredientId = ing.Id,
                    Quantity = kv.Value,
                    Type = StockMovementType.Outbound,
                    OrderId = order.Id,
                    Reason = $"Списание при закрытии заказа #{order.Id}"
                });

                if (ing.CurrentQuantity <= ing.MinimumQuantity)
                {
                    lowAfterDeduct.Add($"{ing.Name} ({ing.CurrentQuantity} {ing.Unit})");
                }
            }
            */

            Debug.WriteLine("Before SaveChangesAsync.");
            await _context.SaveChangesAsync();
            Debug.WriteLine("After SaveChangesAsync.");

            TempData["SuccessMessage"] = $"✅ Заказ #{order.Id} успешно закрыт. Стол №{order.Table?.Number} освобожден.";
            /*if (lowAfterDeduct.Any())
            {
                TempData["WarningMessage"] = "Низкий остаток: " + string.Join(", ", lowAfterDeduct);
            }*/

            

            return RedirectToAction("PrintReceipt", new { id = order.Id });
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Cash.View)]
        public async Task<IActionResult> PrintReceipt(int id)
        {
            

            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

    }
}
