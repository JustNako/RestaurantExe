using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Data;
using Restaurant.Entities;
using Restaurant.Authorization;
using System.Security.Claims;

namespace Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = Permissions.Orders.View)]
    public class ReservationsController : Controller
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? date = null)
        {
            if (date.HasValue)
            {
                // Если дата выбрана — показываем бронирования только на этот день
                var targetDate = date.Value.Date;
                var startOfDay = targetDate;
                var endOfDay = startOfDay.AddDays(1);

                var reservations = await _context.Reservations
                    .Include(r => r.Table)
                    .Where(r => r.ReservationTime > startOfDay && r.ReservationTime < endOfDay)
                    .OrderBy(r => r.ReservationTime)
                    .ToListAsync();

                ViewBag.SelectedDate = targetDate;
                ViewBag.ShowAll = false;

                return View(reservations);
            }
            else
            {
                // Если дата не выбрана — показываем все бронирования, сгруппированные по дате
                var reservations = await _context.Reservations
                    .Include(r => r.Table)
                    .OrderBy(r => r.ReservationTime)
                    .ToListAsync();

                ViewBag.ShowAll = true;

                // Передаем все бронирования в модель, но в представлении сделаем группировку по датам
                return View(reservations);
            }
        }


        [HttpGet]
        [Authorize(Policy = Permissions.Orders.Create)]
        public async Task<IActionResult> Create(int? tableId = null)
        {
            var tables = await _context.Tables.ToListAsync();
            ViewBag.Tables = tables;
            ViewBag.TableId = tableId;
            
            var model = new Reservation();
            if (tableId.HasValue)
            {
                model.TableId = tableId.Value;
            }
            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation reservation)
        {
            // Игнорируем валидацию для полей, которые не приходят из формы
            ModelState.Remove(nameof(Reservation.Table));
            ModelState.Remove(nameof(Reservation.CreatedBy));
            ModelState.Remove(nameof(Reservation.CreatedAt));
            ModelState.Remove(nameof(Reservation.Status));
            
            if (!ModelState.IsValid)
            {
                ViewBag.Tables = await _context.Tables.ToListAsync();
                ViewBag.TableId = reservation.TableId;
                return View(reservation);
            }

            // Проверяем, не пересекается ли бронирование с другими активными бронированиями
            var conflictingReservation = await _context.Reservations
                .Where(r => r.TableId == reservation.TableId 
                    && r.Status == ReservationStatus.Active
                    && r.ReservationTime <= reservation.ReservationTime.AddHours(2)
                    && (r.EndTime ?? r.ReservationTime.AddHours(2)) >= reservation.ReservationTime)
                .FirstOrDefaultAsync();

            if (conflictingReservation != null)
            {
                ModelState.AddModelError("", $"Стол уже забронирован на это время. Существующее бронирование: {conflictingReservation.ReservationTime:HH:mm}");
                ViewBag.Tables = await _context.Tables.ToListAsync();
                ViewBag.TableId = reservation.TableId;
                return View(reservation);
            }

            // Устанавливаем время окончания (по умолчанию +2 часа)
            if (!reservation.EndTime.HasValue)
            {
                reservation.EndTime = reservation.ReservationTime.AddHours(2);
            }

            reservation.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            reservation.Status = ReservationStatus.Active;

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Бронирование создано на {reservation.ReservationTime:dd.MM.yyyy HH:mm}";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Orders.Edit)]
        public async Task<IActionResult> Edit(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            ViewBag.Tables = await _context.Tables.ToListAsync();
            return View(reservation);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Reservation reservation)
        {
            // Игнорируем валидацию для полей, которые не приходят из формы
            ModelState.Remove(nameof(Reservation.Table));
            ModelState.Remove(nameof(Reservation.CreatedBy));
            ModelState.Remove(nameof(Reservation.CreatedAt));
            
            if (!ModelState.IsValid)
            {
                ViewBag.Tables = await _context.Tables.ToListAsync();
                return View(reservation);
            }

            var existingReservation = await _context.Reservations.FindAsync(reservation.Id);
            if (existingReservation == null)
                return NotFound();

            // Проверяем конфликты (исключая текущее бронирование)
            var conflictingReservation = await _context.Reservations
                .Where(r => r.Id != reservation.Id
                    && r.TableId == reservation.TableId
                    && r.Status == ReservationStatus.Active
                    && r.ReservationTime <= reservation.ReservationTime.AddHours(2)
                    && (r.EndTime ?? r.ReservationTime.AddHours(2)) >= reservation.ReservationTime)
                .FirstOrDefaultAsync();

            if (conflictingReservation != null)
            {
                ModelState.AddModelError("", $"Стол уже забронирован на это время. Существующее бронирование: {conflictingReservation.ReservationTime:HH:mm}");
                ViewBag.Tables = await _context.Tables.ToListAsync();
                return View(reservation);
            }

            existingReservation.TableId = reservation.TableId;
            existingReservation.ReservationTime = reservation.ReservationTime;
            existingReservation.EndTime = reservation.EndTime ?? reservation.ReservationTime.AddHours(2);
            existingReservation.CustomerName = reservation.CustomerName;
            existingReservation.CustomerPhone = reservation.CustomerPhone;
            existingReservation.Notes = reservation.Notes;
            existingReservation.Status = reservation.Status;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Бронирование обновлено";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return NotFound();

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Бронирование отменено";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return NotFound();

            reservation.Status = ReservationStatus.Completed;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Бронирование завершено";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return NotFound();

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Бронирование удалено";
            return RedirectToAction(nameof(Index));
        }
    }
}

