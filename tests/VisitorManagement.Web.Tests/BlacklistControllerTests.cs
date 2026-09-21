using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Controllers;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Tests;

public class BlacklistControllerTests
{
    [Fact]
    public async Task ToggleAndDelete_Work()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        db.BlacklistEntries.Add(new BlacklistEntry
        {
            FullName = "นาย ทดสอบ",
            NationalId = "1234567890121",
            Reason = "ทดสอบ",
            IsActive = true,
            CreatedAt = TimeHelper.Now
        });
        await db.SaveChangesAsync();
        var entry = await db.BlacklistEntries.SingleAsync();

        var controller = new BlacklistController(db)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new InMemoryTempDataProvider())
        };

        await controller.Toggle(entry.Id);
        Assert.False((await db.BlacklistEntries.FindAsync(entry.Id))!.IsActive);

        await controller.Delete(entry.Id);
        Assert.Empty(db.BlacklistEntries);
    }

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object?> _data = new();
        public IDictionary<string, object?> LoadTempData(HttpContext context) => _data;
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) =>
            _data = new Dictionary<string, object?>(values);
    }
}
