using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Data;
using Restaurant.Entities;
using Restaurant.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Authorization;

namespace Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Policy = Permissions.Orders.View)]
        public async Task<IActionResult> Index(OrderStatus? status, int page = 1, int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 12;

            var baseQuery = _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Items)
                .AsQueryable();

            if (status.HasValue)
            {
                baseQuery = baseQuery.Where(o => o.Status == status.Value);
            }

            var totalCount = await baseQuery.CountAsync();

            var pagedOrders = await baseQuery
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Подсчеты по всем статусам без учета текущего фильтра
            var allOrdersQuery = _context.Orders.AsQueryable();

            var vm = new OrdersIndexViewModel
            {
                Orders = pagedOrders,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                SelectedStatus = status,
                NewCount = await allOrdersQuery.CountAsync(o => o.Status == OrderStatus.New),
                CompletedCount = await allOrdersQuery.CountAsync(o => o.Status == OrderStatus.Completed),
                CancelledCount = await allOrdersQuery.CountAsync(o => o.Status == OrderStatus.Cancelled)
            };

            return View(vm);
        }




        [HttpGet]
        [Authorize(Policy = Permissions.Orders.Create)]
        public IActionResult Create(int? tableId = null)
        {
            var activeShift = _context.Shifts.FirstOrDefault(s => s.ClosedAt == null);
            if (activeShift == null)
            {
                TempData["ErrorMessage"] = "❌ Нельзя создать заказ, пока нет открытой смены.";
                return RedirectToAction("Index");
            }

            // Получаем только свободные столы
            var availableTables = _context.Tables.Where(t => !t.IsOccupied).ToList();

            var model = new CreateOrderViewModel
            {
                Tables = availableTables,
                Categories = _context.Categories
                    .Include(c => c.Products)
                    .ToList(),
                Products = _context.Products.ToList()
            };

            // Если передан ID стола, устанавливаем его как выбранный
            if (tableId.HasValue && availableTables.Any(t => t.Id == tableId.Value))
            {
                model.TableId = tableId.Value;
            }

            return View(model);
        }


        [HttpPost]
        [Authorize(Policy = Permissions.Orders.Create)]
        public async Task<IActionResult> Create(CreateOrderViewModel vm)
        {
            // Проверяем наличие открытой смены перед созданием заказа (серверная защита)
            var activeShift = await _context.Shifts.FirstOrDefaultAsync(s => s.ClosedAt == null);
            if (activeShift == null)
            {
                TempData["ErrorMessage"] = "❌ Нельзя создать заказ, пока нет открытой смены.";
                return RedirectToAction("Index");
            }

            // Игнорируем служебные поля
            ModelState.Remove(nameof(vm.Categories));
            ModelState.Remove(nameof(vm.Tables));
            ModelState.Remove(nameof(vm.Products));

            if (!ModelState.IsValid || vm.ProductsWithQuantity == null || !vm.ProductsWithQuantity.Any(p => p.Quantity > 0))
            {
                vm.Categories = await _context.Categories.Include(c => c.Products).ToListAsync();
                vm.Tables = await _context.Tables.ToListAsync();
                return View(vm);
            }

            // Получаем продукты
            var productIds = vm.ProductsWithQuantity.Select(p => p.ProductId).ToList();
            var productsFromDb = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            // Создаём список позиций заказа с ценами
            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            foreach (var item in vm.ProductsWithQuantity.Where(p => p.Quantity > 0))
            {
                var product = productsFromDb.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    var orderItem = new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        Price = product.Price // 🔹 сохраняем цену на момент заказа
                    };

                    orderItems.Add(orderItem);
                    totalAmount += product.Price * item.Quantity;
                }
            }

            
            // Создаём заказ
            var order = new Order
            {
                TableId = vm.TableId,
                CreatedAt = DateTime.Now,
                TotalPrice = totalAmount,
                Items = orderItems,
                ShiftId = activeShift.Id,
                Place = vm.Place

            };

            _context.Orders.Add(order);

            // Делаем стол занятым
            var table = await _context.Tables.FindAsync(vm.TableId);
            if (table != null)
            {
                table.IsOccupied = true;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }







        [HttpGet]
        [Authorize(Policy = Permissions.Orders.Edit)]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var products = await _context.Products.ToListAsync();

            var vm = new EditOrderViewModel
            {
                Id = order.Id,
                TableId = order.TableId,
                Tables = await _context.Tables.ToListAsync(),
                Categories = await _context.Categories
                    .Include(c => c.Products)
                    .ToListAsync(),
                ProductsWithQuantity = products
                    .Select(p => new OrderProductInput
                    {
                        ProductId = p.Id,
                        Quantity = order.Items.FirstOrDefault(i => i.ProductId == p.Id)?.Quantity ?? 0
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.Edit)]
        public async Task<IActionResult> Edit(EditOrderViewModel vm)
        {
            ModelState.Remove(nameof(vm.Categories));
            ModelState.Remove(nameof(vm.Tables));
            ModelState.Remove(nameof(vm.Products));

            if (!ModelState.IsValid || vm.ProductsWithQuantity == null)
            {
                vm.Tables = await _context.Tables.ToListAsync();
                vm.Categories = await _context.Categories.Include(c => c.Products).ToListAsync();
                return View(vm);
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == vm.Id);

            if (order == null)
                return NotFound();

            // Обрабатываем смену стола
            var oldTableId = order.TableId;
            var newTableId = vm.TableId;

            if (oldTableId != newTableId)
            {
                // Проверяем, что новый стол существует и не занят
                var newTable = await _context.Tables.FindAsync(newTableId);
                if (newTable == null)
                {
                    return NotFound();
                }

                if (newTable.IsOccupied)
                {
                    ModelState.AddModelError(nameof(vm.TableId), "Выбранный стол уже занят.");
                    vm.Tables = await _context.Tables.ToListAsync();
                    vm.Categories = await _context.Categories.Include(c => c.Products).ToListAsync();
                    return View(vm);
                }

                // Освобождаем старый стол
                var oldTable = await _context.Tables.FindAsync(oldTableId);
                if (oldTable != null)
                {
                    oldTable.IsOccupied = false;
                }

                // Занимаем новый стол
                newTable.IsOccupied = true;
                order.TableId = newTableId;
            }

            order.Items.Clear();

            decimal totalAmount = 0;
            var productIds = vm.ProductsWithQuantity.Select(p => p.ProductId).ToList();
            var productsFromDb = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var item in vm.ProductsWithQuantity.Where(p => p.Quantity > 0))
            {
                var product = productsFromDb.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    totalAmount += product.Price * item.Quantity;
                    order.Items.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        Price = product.Price
                    });
                }
            }

            order.TotalPrice = totalAmount;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }








        [Authorize(Policy = Permissions.Orders.View)]
        public async Task<IActionResult> Details(int id)
            
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: /Admin/Orders/Print/5
        [HttpGet]
        [Authorize(Policy = Permissions.Orders.View)]
        public async Task<IActionResult> Print(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null) return NotFound();

            order.Status = OrderStatus.Cancelled;
            
            // Освобождаем стол при отмене заказа
            if (order.Table != null)
            {
                order.Table.IsOccupied = false;
            }

            

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Complete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null) return NotFound();

            order.Status = OrderStatus.Completed;
            
            // Освобождаем стол при завершении заказа
            if (order.Table != null)
            {
                order.Table.IsOccupied = false;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
