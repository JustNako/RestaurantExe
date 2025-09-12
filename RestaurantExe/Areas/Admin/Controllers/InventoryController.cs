using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Authorization;
using Restaurant.Data;
using Restaurant.Entities;

namespace Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = Permissions.Inventory.View)]
    public class InventoryController : Controller
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var ingredients = await _context.Ingredients
                .OrderBy(i => i.Name)
                .ToListAsync();
            return View(ingredients);
        }

        [Authorize(Policy = Permissions.Inventory.Manage)]
        [HttpGet]
        public async Task<IActionResult> ManageRecipe(int? productId)
        {
            var products = await _context.Products
                .OrderBy(p => p.Name)
                .ToListAsync();
            ViewBag.Products = products;

            if (productId == null && products.Any())
            {
                productId = products.First().Id;
            }

            if (productId == null)
            {
                return View("ManageRecipe", new List<RecipeItem>());
            }

            ViewBag.SelectedProductId = productId;

            var recipe = await _context.RecipeItems
                .Where(r => r.ProductId == productId)
                .Include(r => r.Ingredient)
                .ToListAsync();

            var allIngredients = await _context.Ingredients.OrderBy(i => i.Name).ToListAsync();
            ViewBag.AllIngredients = allIngredients;

            return View(recipe);
        }

        [Authorize(Policy = Permissions.Inventory.Manage)]
        [HttpPost]
        public async Task<IActionResult> ManageRecipe(int productId, int[] ingredientIds, decimal[] quantities)
        {
            if (ingredientIds.Length != quantities.Length)
            {
                return BadRequest("Invalid payload");
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var existing = await _context.RecipeItems.Where(r => r.ProductId == productId).ToListAsync();
            _context.RecipeItems.RemoveRange(existing);

            for (int i = 0; i < ingredientIds.Length; i++)
            {
                var q = quantities[i];
                if (q <= 0) continue;
                _context.RecipeItems.Add(new RecipeItem
                {
                    ProductId = productId,
                    IngredientId = ingredientIds[i],
                    QuantityPerUnit = q
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Рецепт обновлен";
            return RedirectToAction(nameof(ManageRecipe), new { productId });
        }

        [Authorize(Policy = Permissions.Inventory.Manage)]
        [HttpGet]
        public IActionResult CreateIngredient()
        {
            return View(new Ingredient());
        }

        [Authorize(Policy = Permissions.Inventory.Manage)]
        [HttpPost]
        public async Task<IActionResult> CreateIngredient(Ingredient model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Ingredients.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = Permissions.Inventory.Manage)]
        [HttpGet]
        public async Task<IActionResult> AddStock(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null) return NotFound();
            ViewBag.Ingredient = ingredient;
            return View();
        }

        [Authorize(Policy = Permissions.Inventory.Manage)]
        [HttpPost]
        public async Task<IActionResult> AddStock(int id, decimal quantity, string? reason)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null) return NotFound();
            if (quantity <= 0)
            {
                ModelState.AddModelError("quantity", "Количество должно быть больше 0");
                ViewBag.Ingredient = ingredient;
                return View();
            }
            ingredient.CurrentQuantity += quantity;
            _context.StockMovements.Add(new StockMovement
            {
                IngredientId = ingredient.Id,
                Quantity = quantity,
                Type = StockMovementType.Inbound,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Поступление на склад" : reason
            });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = Permissions.Inventory.Manage)]
        [HttpPost]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.Id == id);
            if (ingredient == null) return NotFound();

            var hasMovements = await _context.StockMovements.AnyAsync(m => m.IngredientId == id);
            if (hasMovements)
            {
                TempData["ErrorMessage"] = $"Нельзя удалить ингредиент '{ingredient.Name}', так как есть движения по складу.";
                return RedirectToAction(nameof(Index));
            }

            // Удалим связанные записи рецептов (настроен каскад, но удалим явно на всякий случай)
            var recipeLinks = await _context.RecipeItems.Where(r => r.IngredientId == id).ToListAsync();
            if (recipeLinks.Count > 0)
            {
                _context.RecipeItems.RemoveRange(recipeLinks);
            }

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Ингредиент удален";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = Permissions.Inventory.Manage)]
        [HttpPost]
        public async Task<IActionResult> ForceDeleteIngredient(int id)
        {
            var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.Id == id);
            if (ingredient == null) return NotFound();

            var recipeLinks = await _context.RecipeItems.Where(r => r.IngredientId == id).ToListAsync();
            if (recipeLinks.Count > 0)
            {
                _context.RecipeItems.RemoveRange(recipeLinks);
            }

            var movements = await _context.StockMovements.Where(m => m.IngredientId == id).ToListAsync();
            if (movements.Count > 0)
            {
                _context.StockMovements.RemoveRange(movements);
            }

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Ингредиент и все связанные движения удалены";
            return RedirectToAction(nameof(Index));
        }
    }
}


