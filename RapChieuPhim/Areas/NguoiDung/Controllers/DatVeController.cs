using Microsoft.AspNetCore.Mvc;
using RapChieuPhim.Services;

namespace RapChieuPhim.Areas.NguoiDung.Controllers
{
    [Area("NguoiDung")]
    public class DatVeController : Controller
    {
        private readonly DatVeService _datVeService;

        public DatVeController(DatVeService datVeService)
        {
            _datVeService = datVeService;
        }

        public async Task<IActionResult> ChiTietPhim(string maPhim)
        {
            var phim = await _datVeService.LayChiTietPhimAsync(maPhim);
            if (phim == null)
            {
                return NotFound("Không tìm thấy phim này!");
            }
            return View(phim);
        }
    }
}