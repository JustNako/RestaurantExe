using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Data;
using Restaurant.Entities;
using Restaurant.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Authorization;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = Permissions.Orders.View)]
    public class QrController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public QrController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var qrLinks = await _context.QrLinks
                .Include(q => q.Table)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            var viewModels = qrLinks.Select(q => new QrLinkViewModel
            {
                Id = q.Id,
                Code = q.Code,
                Type = q.Type,
                TableId = q.TableId,
                CustomUrl = q.CustomUrl,
                IsActive = q.IsActive,
                ExpiresAt = q.ExpiresAt,
                Description = q.Description,
                TableNumber = q.Table?.Number != null ? q.Table.Number.ToString() : " Стол не назначен",
                ScanCount = q.ScanCount,
                CreatedAt = q.CreatedAt
            }).ToList();



            return View(viewModels);
        }

        [Authorize(Policy = Permissions.Orders.Create)]
        public IActionResult Create()
        {
            var tables = _context.Tables.OrderBy(t => t.Number).ToList();
            ViewBag.Tables = tables;
            return View(new QrLinkViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Orders.Create)]
        public async Task<IActionResult> Create(QrLinkViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var tables = _context.Tables.OrderBy(t => t.Number).ToList();
                ViewBag.Tables = tables;
                return View(model);
            }

            // Проверяем уникальность кода
            var existingCode = await _context.QrLinks.FirstOrDefaultAsync(q => q.Code == model.Code);
            if (existingCode != null)
            {
                ModelState.AddModelError("Code", "QR код с таким именем уже существует");
                var tables = _context.Tables.OrderBy(t => t.Number).ToList();
                ViewBag.Tables = tables;
                return View(model);
            }

            var qrLink = new QrLink
            {
                Code = model.Code,
                Type = model.Type,
                TableId = model.Type == QrLinkType.Table ? model.TableId : null,
                CustomUrl = model.Type == QrLinkType.CustomUrl ? model.CustomUrl : null,
                IsActive = model.IsActive,
                ExpiresAt = model.ExpiresAt,
                Description = model.Description
            };

            _context.QrLinks.Add(qrLink);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ QR код '{model.Code}' успешно создан!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = Permissions.Orders.Edit)]
        public async Task<IActionResult> Edit(int id)
        {
            var qrLink = await _context.QrLinks.FindAsync(id);
            if (qrLink == null) return NotFound();

            var model = new QrLinkViewModel
            {
                Id = qrLink.Id,
                Code = qrLink.Code,
                Type = qrLink.Type,
                TableId = qrLink.TableId,
                CustomUrl = qrLink.CustomUrl,
                IsActive = qrLink.IsActive,
                ExpiresAt = qrLink.ExpiresAt,
                Description = qrLink.Description
            };

            var tables = _context.Tables.OrderBy(t => t.Number).ToList();
            ViewBag.Tables = tables;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Orders.Edit)]
        public async Task<IActionResult> Edit(int id, QrLinkViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var tables = _context.Tables.OrderBy(t => t.Number).ToList();
                ViewBag.Tables = tables;
                return View(model);
            }

            var qrLink = await _context.QrLinks.FindAsync(id);
            if (qrLink == null) return NotFound();

            // Проверяем уникальность кода (исключая текущий)
            var existingCode = await _context.QrLinks.FirstOrDefaultAsync(q => q.Code == model.Code && q.Id != id);
            if (existingCode != null)
            {
                ModelState.AddModelError("Code", "QR код с таким именем уже существует");
                var tables = _context.Tables.OrderBy(t => t.Number).ToList();
                ViewBag.Tables = tables;
                return View(model);
            }

            qrLink.Code = model.Code;
            qrLink.Type = model.Type;
            qrLink.TableId = model.Type == QrLinkType.Table ? model.TableId : null;
            qrLink.CustomUrl = model.Type == QrLinkType.CustomUrl ? model.CustomUrl : null;
            qrLink.IsActive = model.IsActive;
            qrLink.ExpiresAt = model.ExpiresAt;
            qrLink.Description = model.Description;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ QR код '{model.Code}' успешно обновлен!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Orders.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var qrLink = await _context.QrLinks.FindAsync(id);
            if (qrLink == null) return NotFound();

            _context.QrLinks.Remove(qrLink);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"🗑️ QR код '{qrLink.Code}' удален!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadQr(int id)
        {
            var qrLink = await _context.QrLinks.FindAsync(id);
            if (qrLink == null) return NotFound();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var qrUrl = $"{baseUrl}/qr/{qrLink.Code}";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
            using var pngQr = new PngByteQRCode(qrCodeData);
            var bytes = pngQr.GetGraphic(20);

            return File(bytes, "image/png", $"qr_{qrLink.Code}.png");
        }

        [HttpGet]
        public async Task<IActionResult> GenerateForTable(int tableId)
        {
            var table = await _context.Tables.FindAsync(tableId);
            if (table == null) return NotFound();

            // Проверяем, есть ли уже QR код для этого стола
            var existingQrLink = await _context.QrLinks
                .FirstOrDefaultAsync(q => q.TableId == tableId && q.Type == QrLinkType.Table);

            if (existingQrLink != null)
            {
                // Если QR код уже существует, показываем его
                TempData["InfoMessage"] = $"ℹ️ Для стола №{table.Number} уже есть QR код: {existingQrLink.Code}";
                return RedirectToAction("Index");
            }

            // Генерируем уникальный код только если его нет
            var code = GenerateUniqueCode();
            
            var qrLink = new QrLink
            {
                Code = code,
                Type = QrLinkType.Table,
                TableId = tableId,
                Description = $"Стол №{table.Number}",
                IsActive = true
            };

            _context.QrLinks.Add(qrLink);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ QR код '{code}' создан для стола №{table.Number}!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ShowTableQr(int tableId)
        {
            var table = await _context.Tables.FindAsync(tableId);
            if (table == null) return NotFound();

            // Ищем существующий QR код для стола
            var qrLink = await _context.QrLinks
                .FirstOrDefaultAsync(q => q.TableId == tableId && q.Type == QrLinkType.Table);

            if (qrLink == null)
            {
                // Если QR кода нет, создаем новый
                var code = GenerateUniqueCode();
                qrLink = new QrLink
                {
                    Code = code,
                    Type = QrLinkType.Table,
                    TableId = tableId,
                    Description = $"Стол №{table.Number}",
                    IsActive = true
                };

                _context.QrLinks.Add(qrLink);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ QR код '{code}' создан для стола №{table.Number}!";
            }

            // Показываем QR код
            ViewBag.Table = table;
            ViewBag.QrLink = qrLink;
            return View("ShowTableQr");
        }

        private string GenerateUniqueCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;
            
            do
            {
                code = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (_context.QrLinks.Any(q => q.Code == code));

            return code;
        }
    }
}
