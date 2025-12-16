using Microsoft.EntityFrameworkCore;
using Restaurant.Data;
using Restaurant.Entities;

public interface ISettingsService
{
    Task<Settings> GetSettingsAsync();
}

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _context;

    public SettingsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Settings> GetSettingsAsync()
    {
        return await _context.Settings.FirstOrDefaultAsync();
    }
}
