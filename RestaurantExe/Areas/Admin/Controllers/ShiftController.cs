using Microsoft.AspNetCore.Mvc;
using Restaurant.Data;
using Restaurant.Entities;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Authorization;

namespace Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ShiftController : Controller
    {
        private readonly AppDbContext _context;

        public ShiftController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var shifts = _context.Shifts.OrderByDescending(s => s.OpenedAt).ToList();
            return View(shifts);
        }

        [Authorize(Policy = Permissions.Shifts.Open)]
        public IActionResult Open()
        {
            var openShift = _context.Shifts.FirstOrDefault(s => s.ClosedAt == null);
            if (openShift != null)
            {
                TempData["ErrorMessage"] = "Смена уже открыта.";
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Shifts.Open)]
        public IActionResult Open(decimal openingCash)
        {
            var shift = new Shift
            {
                OpenedAt = DateTime.Now,
                OpenedBy = User.Identity.Name ?? "Admin",
                OpeningCash = openingCash
            };

            _context.Shifts.Add(shift);
            _context.SaveChanges();
            TempData["SuccessMessage"] = $"Смена открыта: {shift.OpenedAt:g}.";
            return RedirectToAction("Index");
        }

        [Authorize(Policy = Permissions.Shifts.Close)]
        public IActionResult Close()
        {
            var openShift = _context.Shifts.FirstOrDefault(s => s.ClosedAt == null);
            if (openShift == null)
            {
                TempData["ErrorMessage"] = "Нет открытой смены.";
                return RedirectToAction("Index");
            }

            return View(openShift);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Shifts.Close)]
        public IActionResult Close(decimal closingCash, string? noteText)
        {
            var openShift = _context.Shifts.FirstOrDefault(s => s.ClosedAt == null);
            if (openShift == null)
                return NotFound();

            openShift.ClosedAt = DateTime.Now;
            openShift.ClosedBy = User.Identity.Name ?? "Admin";
            openShift.ClosingCash = closingCash;

            if (!string.IsNullOrWhiteSpace(noteText))
            {
                _context.ShiftNotes.Add(new ShiftNote
                {
                    ShiftId = openShift.Id,
                    Text = noteText
                });
            }

            _context.SaveChanges();
            TempData["SuccessMessage"] = $"Смена закрыта: {openShift.ClosedAt:g}.";
            return RedirectToAction("Index");
        }
    }
}
