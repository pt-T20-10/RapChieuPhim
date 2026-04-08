using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Extensions; // Gọi thư viện Session vừa tạo
using RapChieuPhim.Models.ViewModels;
using RapChieuPhim.Services;

namespace RapChieuPhim.Areas.NguoiDung.Controllers
{
    [Area("NguoiDung")]
    public class DichVuController : Controller
    {
        private readonly DichVuService _dichVuService;
        private readonly AppDbContext _context;

        public DichVuController(DichVuService dichVuService, AppDbContext context)
        {
            _dichVuService = dichVuService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var menu = await _dichVuService.LayDanhSachMenuAsync();
            return View(menu);
        }

        // --- CÁC HÀM XỬ LÝ GIỎ HÀNG ---
        const string CART_KEY = "GioHangBapNuoc";

        public List<CartItem> LayGioHang()
        {
            return HttpContext.Session.Get<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
        }

        [HttpPost]
        public IActionResult ThemVaoGio(string maDichVu, int soLuong = 1)
        {
            var gioHang = LayGioHang();
            var item = gioHang.SingleOrDefault(p => p.MaDichVu == maDichVu);

            if (item != null) // Nếu có rồi thì tăng số lượng
            {
                item.SoLuong += soLuong;
            }
            else // Chưa có thì truy vấn DB để lấy thông tin thêm vào
            {
                var dichVu = _context.DichVus.SingleOrDefault(p => p.MaDichVu == maDichVu);
                if (dichVu == null) return NotFound("Không tìm thấy dịch vụ");

                item = new CartItem
                {
                    MaDichVu = dichVu.MaDichVu,
                    TenDichVu = dichVu.TenDichVu,
                    GiaBan = dichVu.GiaBan,
                    SoLuong = soLuong,
                    DuongDanHinh = string.IsNullOrEmpty(dichVu.DuongDanHinh) ? "/images/default/no-image.jpg" : dichVu.DuongDanHinh
                };
                gioHang.Add(item);
            }

            // Lưu lại vào Session
            HttpContext.Session.Set(CART_KEY, gioHang);

            // Trả về số lượng item để update cái icon Giỏ hàng trên Header
            return Json(new { success = true, totalItems = gioHang.Sum(x => x.SoLuong) });
        }

        // Hàm để sang trang Xem Giỏ Hàng
        [HttpGet]
        public IActionResult GioHang()
        {
            return View(LayGioHang());
        }

        [HttpPost]
        public IActionResult CapNhatSoLuong(string maDichVu, int kieuThayDoi)
        {
            var gioHang = LayGioHang();
            var item = gioHang.SingleOrDefault(p => p.MaDichVu == maDichVu);

            if (item != null)
            {
                // kieuThayDoi là 1 (Tăng) hoặc -1 (Giảm)
                item.SoLuong += kieuThayDoi;

                if (item.SoLuong <= 0)
                {
                    gioHang.Remove(item); // Xóa nếu số lượng về 0
                }
            }

            HttpContext.Session.Set(CART_KEY, gioHang);

            return Json(new
            {
                success = true,
                soLuongMoi = item?.SoLuong ?? 0,
                itemThanhTien = item?.ThanhTien ?? 0,
                tongTien = gioHang.Sum(x => x.ThanhTien),
                tongSoLuong = gioHang.Sum(x => x.SoLuong)
            });
        }

        [HttpPost]
        public IActionResult XoaKhoiGio(string maDichVu)
        {
            var gioHang = LayGioHang();
            var item = gioHang.SingleOrDefault(p => p.MaDichVu == maDichVu);

            if (item != null)
            {
                gioHang.Remove(item);
                HttpContext.Session.Set(CART_KEY, gioHang);
            }

            return Json(new
            {
                success = true,
                tongTien = gioHang.Sum(x => x.ThanhTien),
                tongSoLuong = gioHang.Sum(x => x.SoLuong)
            });
        }

        [HttpPost]
        public async Task<IActionResult> ApDungKhuyenMai(string maCode)
        {
            var gioHang = LayGioHang();
            double tongTien = gioHang.Sum(x => x.ThanhTien);

            if (tongTien == 0)
                return Json(new { success = false, message = "Giỏ hàng đang trống." });

            // Tìm mã Code trong DB
            var km = await _context.KhuyenMais.FirstOrDefaultAsync(x => x.MaCode == maCode && x.DaXoa == false);

            // Kiểm tra các lớp bảo vệ của KhuyenMai
            if (km == null) return Json(new { success = false, message = "Mã giảm giá không tồn tại." });
            if (km.TrangThai != "DangApDung") return Json(new { success = false, message = "Mã này chưa được kích hoạt hoặc đã đóng." });
            if (DateTime.Now < km.TuNgay || DateTime.Now > km.DenNgay) return Json(new { success = false, message = "Mã không trong thời gian sử dụng." });
            if (km.SoLuongConLai <= 0) return Json(new { success = false, message = "Mã đã hết lượt sử dụng." });
            if (tongTien < km.DonToiThieu) return Json(new { success = false, message = $"Đơn hàng tối thiểu {string.Format("{0:#,##0}", km.DonToiThieu)}đ để áp dụng mã này." });

            // Tính tiền giảm (Theo % và không vượt quá Giảm Tối Đa)
            double tienGiam = tongTien * (km.PhanTramGiam / 100.0);
            if (km.GiamToiDa.HasValue && tienGiam > km.GiamToiDa.Value)
            {
                tienGiam = km.GiamToiDa.Value;
            }

            double tongTienMoi = tongTien - tienGiam;

            // Lưu thông tin vào Session để lát chuyển sang trang Thanh Toán
            HttpContext.Session.SetString("MaKhuyenMai", km.MaKhuyenMai);
            HttpContext.Session.SetString("SoTienGiam", tienGiam.ToString());
            HttpContext.Session.SetString("TongTienSauGiam", tongTienMoi.ToString());

            return Json(new
            {
                success = true,
                message = "Áp dụng mã thành công!",
                tienGiam = tienGiam,
                tongTienMoi = tongTienMoi
            });
        }
    }
}