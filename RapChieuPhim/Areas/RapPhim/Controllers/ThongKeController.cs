using Microsoft.AspNetCore.Mvc;
using RapChieuPhim.Models.ViewModels;
using RapChieuPhim.Services;
using System;
using System.Threading.Tasks;

namespace RapChieuPhim.Areas.RapPhim.Controllers
{
    // BẮT BUỘC KHAI BÁO AREA ĐỂ ĐỊNH TUYẾN ĐÚNG
    [Area("RapPhim")]
    public class ThongKeController : Controller
    {
        private readonly ThongKeService _thongKeService;

        public ThongKeController(ThongKeService thongKeService)
        {
            _thongKeService = thongKeService;
        }

        // GET: /RapPhim/ThongKe
        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
        {
            try
            {
                DateTime endDate = denNgay ?? DateTime.Now;
                DateTime startDate = tuNgay ?? endDate.AddDays(-30);

                if (startDate > endDate)
                {
                    TempData["ErrorMessage"] = "Ngày bắt đầu không được lớn hơn ngày kết thúc.";
                    startDate = endDate.AddDays(-30);
                }

                var data = await _thongKeService.LayThongKeAsync(startDate, endDate);

                if (data.TongSoDonHang == 0)
                {
                    ViewBag.InfoMessage = "Không có dữ liệu giao dịch trong thời gian này.";
                }

                ViewBag.TuNgay = startDate.ToString("yyyy-MM-dd");
                ViewBag.DenNgay = endDate.ToString("yyyy-MM-dd");

                return View(data);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải dữ liệu thống kê: " + ex.Message;
                return View(new Models.ViewModels.ThongKeViewModel());
            }
        }

        // GET: /RapPhim/ThongKe/XuatExcel
        [HttpGet]
        public async Task<IActionResult> XuatExcel(DateTime? tuNgay, DateTime? denNgay)
        {
            try
            {
                DateTime endDate = denNgay ?? DateTime.Now;
                DateTime startDate = tuNgay ?? endDate.AddDays(-30);

                // Lấy dữ liệu
                var data = await _thongKeService.LayThongKeAsync(startDate, endDate);

                // GỌI XUỐNG SERVICE ĐỂ XUẤT FILE (chứ Controller không tự làm)
                var fileContents = _thongKeService.XuatExcel(data);

                string fileName = $"BaoCaoDoanhThu_{startDate:ddMMyyyy}_{endDate:ddMMyyyy}.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(fileContents, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi kết xuất chi tiết: {ex.Message}";
                return RedirectToAction(nameof(Index), new
                {
                    tuNgay = tuNgay?.ToString("yyyy-MM-dd"),
                    denNgay = denNgay?.ToString("yyyy-MM-dd")
                });
            }
        }

        // GET: /RapPhim/ThongKe/DuBao
        public async Task<IActionResult> DuBao(int? soNgay)
        {
            int inputDays = soNgay ?? 7;
            if (inputDays <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập số nguyên dương.";
                inputDays = 7;
            }

            var data = await _thongKeService.TinhDuBaoDoanhThuAsync(inputDays);

            if (!data.DuDieuKien)
            {
                ViewBag.WarningMessage = data.ThongBao;
            }

            return View(data);
        }
    }
}