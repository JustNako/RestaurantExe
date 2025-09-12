using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Data;
using Restaurant.Entities;

namespace Restaurant.Controllers
{
    [Route("qr")]
    public class QrController : Controller
    {
        private readonly AppDbContext _context;

        public QrController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> Go(string code)
        {
            var link = await _context.QrLinks
                .Include(q => q.Table)
                .FirstOrDefaultAsync(x => x.Code == code && x.IsActive);

            if (link == null)
            {
                return NotFound("QR код не найден или неактивен");
            }

            if (link.ExpiresAt != null && link.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest("QR код истек");
            }

            // Увеличиваем счетчик сканирований
            link.ScanCount++;
            await _context.SaveChangesAsync();

            return link.Type switch
            {
                QrLinkType.Table when link.TableId != null
                    => RedirectToAction("Create", "Orders", new { area = "Admin", tableId = link.TableId }),
                QrLinkType.CustomUrl when !string.IsNullOrWhiteSpace(link.CustomUrl)
                    => Redirect(link.CustomUrl),
                _ => NotFound("Некорректный тип QR кода")
            };
        }
    }
}


