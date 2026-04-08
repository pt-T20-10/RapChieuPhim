using Microsoft.AspNetCore.Mvc;
using RapChieuPhim.Models.ViewModels;
using RapChieuPhim.Services;
using System;
using System.Threading.Tasks;

namespace RapChieuPhim.Areas.RapPhim.Controllers
{
    [Area("RapPhim")]
    public class ThongKeController : Controller
    {
        private readonly ThongKeService _thongKeService;

        public ThongKeController(ThongKeService thongKeService)
        {
            _thongKeService = thongKeService;
        }

        // Cập nhật: Thêm tham số lọc maPhim, maPhong
        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay, string maPhim, string maPhong)
        {
            try
            {
                DateTime endDate = denNgay ?? DateTime.Now.Date;
                // CHUẨN ĐẶC TẢ BƯỚC 2: Mặc định truy vấn dữ liệu ngày hiện tại
                DateTime startDate = tuNgay ?? DateTime.Now.Date;

                if (startDate > endDate)
                {
                    TempData["ErrorMessage"] = "Ngày bắt đầu không được lớn hơn ngày kết thúc.";
                    startDate = endDate;
                }

                // Chuyền tham số lọc xuống Service
                var data = await _thongKeService.LayThongKeAsync(startDate, endDate, maPhim, maPhong);

                if (data.TongSoDonHang == 0)
                {
                    ViewBag.InfoMessage = "Không có dữ liệu giao dịch trong thời gian này.";
                }

                // Đổ dữ liệu ra View
                ViewBag.TuNgay = startDate.ToString("yyyy-MM-dd");
                ViewBag.DenNgay = endDate.ToString("yyyy-MM-dd");
                ViewBag.SelectedPhim = maPhim;
                ViewBag.SelectedPhong = maPhong;
                ViewBag.PhimList = await _thongKeService.LayDanhSachPhimAsync();
                ViewBag.PhongList = await _thongKeService.LayDanhSachPhongAsync();

                return View(data);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải dữ liệu thống kê: " + ex.Message;
                return View(new Models.ViewModels.ThongKeViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> XuatExcel(DateTime? tuNgay, DateTime? denNgay, string maPhim, string maPhong)
        {
            try
            {
                DateTime endDate = denNgay ?? DateTime.Now.Date;
                DateTime startDate = tuNgay ?? DateTime.Now.Date;

                // Lấy dữ liệu ĐÃ LỌC để xuất Excel chuẩn xác
                var data = await _thongKeService.LayThongKeAsync(startDate, endDate, maPhim, maPhong);
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
                    denNgay = denNgay?.ToString("yyyy-MM-dd"),
                    maPhim = maPhim,
                    maPhong = maPhong
                });
            }
        }

        // GET: /RapPhim/ThongKe/DuBao
        public async Task<IActionResult> DuBao(int? soNgay)
        {
            int inputDays = soNgay ?? 7;
            var data = await _thongKeService.TinhDuBaoDoanhThuAsync(inputDays);

            // Bắt ngoại lệ 2.a: Thông báo không đủ dữ liệu
            if (!data.DuDieuKien)
            {
                ViewBag.WarningMessage = data.ThongBao;
            }

            return View(data);
        }

        // --- THÊM MỚI: TẢI FILE EXCEL DỰ BÁO ---
        [HttpGet]
        public async Task<IActionResult> XuatExcelDuBao(int? soNgay)
        {
            try
            {
                int inputDays = soNgay ?? 7;
                var data = await _thongKeService.TinhDuBaoDoanhThuAsync(inputDays);

                if (!data.DuDieuKien)
                {
                    TempData["ErrorMessage"] = data.ThongBao;
                    return RedirectToAction(nameof(DuBao));
                }

                var fileContents = _thongKeService.XuatExcelDuBao(data);
                string fileName = $"DuBaoDoanhThu_{inputDays}NgayTiepTheo.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(fileContents, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi kết xuất chi tiết: {ex.Message}";
                return RedirectToAction(nameof(DuBao), new { soNgay = soNgay });
            }
        }
    }
}