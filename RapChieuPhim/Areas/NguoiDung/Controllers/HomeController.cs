using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RapChieuPhim.Models;
using RapChieuPhim.Services;

namespace RapChieuPhim.Areas.NguoiDung.Controllers
{
    [Area("NguoiDung")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DatVeService _datVeService;

        // Tiêm DatVeService vào Controller
        public HomeController(ILogger<HomeController> logger, DatVeService datVeService)
        {
            _logger = logger;
            _datVeService = datVeService;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy danh sách phim từ Service
            var danhSachPhim = await _datVeService.LayDanhSachPhimAsync();
            return View(danhSachPhim);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}