using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppStartupState _startupState;

    public HomeController(AppStartupState startupState)
    {
        _startupState = startupState;
    }

    public IActionResult Index() => RedirectToAction("Index", "Dashboard");

    [HttpGet]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Database()
    {
        if (_startupState.IsDatabaseReady)
        {
            return RedirectToAction(nameof(Index));
        }

        var message = _startupState.FailureMessage
            ?? "ยังเชื่อมต่อฐานข้อมูลไม่ได้ — ตรวจ SQL Server และ ConnectionStrings:SqlServer";
        if (!string.IsNullOrWhiteSpace(_startupState.FailureLogPath))
        {
            message += "\n\nไฟล์ log: " + _startupState.FailureLogPath;
        }

        return View(model: message);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
