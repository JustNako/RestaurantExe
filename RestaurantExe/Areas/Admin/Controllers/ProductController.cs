using Microsoft.AspNetCore.Mvc;
using Restaurant.Data;
using Restaurant.Entities;
using Restaurant.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Authorization;

[Area("Admin")]
public class ProductController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProductController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        this._env = env;
    }

    [Authorize(Policy = Permissions.Products.Create)]
    public IActionResult Create(int categoryId)
    {
        var model = new CreateProductViewModel { CategoryId = categoryId };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Products.Create)]
    public async Task<IActionResult> Create(CreateProductViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        string? imageUrl = null;

        if (model.ImageFile != null)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ImageFile.CopyToAsync(stream);
            }

            imageUrl = "/uploads/" + uniqueFileName;
        }

        var product = new Product
        {
            Name = model.Name,
            Price = model.Price,
            //Description = model.Description, // Добавлено
            ImagePath = imageUrl,
            CategoryId = model.CategoryId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Category", new { id = model.CategoryId });
    }

    [Authorize(Policy = Permissions.Products.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        var categoryId = product.CategoryId;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Category", new { id = categoryId });
    }
}
