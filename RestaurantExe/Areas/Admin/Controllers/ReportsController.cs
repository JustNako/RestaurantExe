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
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Policy = Permissions.Reports.View)]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Policy = Permissions.Reports.View)]
        public async Task<IActionResult> DailyReport(DateTime? date)
        {
            var targetDate = date ?? DateTime.Today;
            ViewBag.Date = targetDate;

            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Table)
                .Where(o => o.CreatedAt.Date == targetDate.Date && o.IsClosed)
                .ToListAsync();

            // Здесь мы позже создадим ViewModel и посчитаем: сумма, чаевые, категории и т.д.

            return View(orders);
        }

        [Authorize(Policy = Permissions.Reports.View)]
        public IActionResult ZReport()
        {
            // Берём текущую активную смену
            var shift = _context.Shifts
                .OrderByDescending(s => s.OpenedAt)
                .FirstOrDefault(s => s.ClosedAt != null);

            if (shift == null)
            {
                TempData["ErrorMessage"] = "Нет закрытой смены для формирования Z-отчёта.";
                return RedirectToAction("Index");
            }

            var orders = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Where(o => o.CreatedAt >= shift.OpenedAt && o.CreatedAt <= shift.ClosedAt && o.IsClosed)
                .ToList();

            decimal totalRevenue = orders.Sum(o => o.Items.Sum(i => i.Price * i.Quantity) - (o.Discount));
            decimal totalDiscounts = orders.Sum(o => o.Discount);

            decimal cashPayments = orders
                .Where(o => o.PaymentType == PaymentType.Cash)
                .Sum(o => o.Items.Sum(i => i.Price * i.Quantity) - (o.Discount));

            decimal cardPayments = orders
                .Where(o => o.PaymentType == PaymentType.Card)
                .Sum(o => o.Items.Sum(i => i.Price * i.Quantity) - (o.Discount));

            // Mixed: используем фактически оплаченные суммы
            decimal mixedCash = orders
                .Where(o => o.PaymentType == PaymentType.Mixed)
                .Sum(o => o.PaidCash);
            decimal mixedCard = orders
                .Where(o => o.PaymentType == PaymentType.Mixed)
                .Sum(o => o.PaidCard);
            decimal mixedTotal = mixedCash + mixedCard;

            var notes = _context.ShiftNotes
                .Where(n => n.ShiftId == shift.Id)
                .Select(n => n.Text)
                .ToList();

            var viewModel = new ZReportViewModel
            {
                ShiftStart = shift.OpenedAt,
                ShiftEnd = shift.ClosedAt.Value,
                TotalOrders = orders.Count,
                TotalRevenue = totalRevenue,
                CashPayments = cashPayments,
                CardPayments = cardPayments,
                MixedCashPayments = mixedCash,
                MixedCardPayments = mixedCard,
                MixedTotalPayments = mixedTotal,
                TotalDiscounts = totalDiscounts,
                Notes = notes
            };

            return View(viewModel);
        }


    }

}
