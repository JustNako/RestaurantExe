// Файл: Areas/Admin/Controllers/CategoryController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Data;
using Restaurant.Entities;
using Restaurant.ViewModels;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Authorization;

namespace Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = Permissions.Categories.View)]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }



        [Authorize(Policy = Permissions.Categories.View)]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.Include(c => c.Products).ToListAsync();
            return View(categories);
        }


        [HttpGet]
        [Authorize(Policy = Permissions.Categories.Create)]
        public IActionResult Create()
            {
                
                return View();
            }

            // POST: /Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Categories.Create)]
        public async Task<IActionResult> Create(CreateCategoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var category = new Category
            {
                Name = model.Name
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Categories.Edit)]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            var model = new CreateCategoryViewModel { Name = category.Name };
            return View(model);
        }

        // POST: /Admin/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Categories.Edit)]
        public async Task<IActionResult> Edit(int id, CreateCategoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            category.Name = model.Name;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: /Admin/Category/Details/5
        [Authorize(Policy = Permissions.Categories.View)]
        public async Task<IActionResult> Details(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: /Admin/Category/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Categories.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            // Remove products belonging to this category first.
            // RecipeItems and other related entities are configured with cascade delete
            // where appropriate in the DbContext, so removing products will cascade.
            if (category.Products != null && category.Products.Any())
            {
                _context.Products.RemoveRange(category.Products);
            }

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }


    }
}