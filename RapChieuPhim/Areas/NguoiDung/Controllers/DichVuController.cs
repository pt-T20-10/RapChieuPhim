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
        private readonly DichVuervice _DichVuervice;
        private readonly AppDbContext _context;

        public DichVuController(DichVuervice DichVuervice, AppDbContext context)
        {
            _DichVuervice = DichVuervice;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var menu = await _DichVuervice.LayDanhSachMenuAsync();
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
                var dichVu = _context.DichVu.SingleOrDefault(p => p.MaDichVu == maDichVu);
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
            double tongTienBapNuoc = gioHang.Sum(x => x.ThanhTien);

            // ── THÊM: Lấy thêm tiền vé nếu đang trong luồng đặt vé ─
            double tongTienVe = 0;
            string maDonHangTam = HttpContext.Session.GetString("DatVe_MaDonHang_Tam");
            if (!string.IsNullOrEmpty(maDonHangTam))
            {
                var dhTam = await _context.DonHang.FindAsync(maDonHangTam);
                if (dhTam != null && dhTam.TrangThai == "ChoThanhToan")
                    tongTienVe = dhTam.TongTienBanDau;
            }

            double tongTien = tongTienBapNuoc + tongTienVe; // ← tổng đúng
                                                            // ────────────────────────────────────────────────────────

            if (tongTien == 0)
                return Json(new { success = false, message = "Giỏ hàng đang trống." });

            var km = await _context.KhuyenMai
                .FirstOrDefaultAsync(x => x.MaCode == maCode && x.DaXoa == false);

            if (km == null)
                return Json(new { success = false, message = "Mã giảm giá không tồn tại." });
            if (km.TrangThai != "DangApDung")
                return Json(new { success = false, message = "Mã này chưa được kích hoạt hoặc đã đóng." });
            if (DateTime.Now < km.TuNgay || DateTime.Now > km.DenNgay)
                return Json(new { success = false, message = "Mã không trong thời gian sử dụng." });
            if (km.SoLuongConLai <= 0)
                return Json(new { success = false, message = "Mã đã hết lượt sử dụng." });
            if (tongTien < km.DonToiThieu)
                return Json(new { success = false, message = $"Đơn hàng tối thiểu {string.Format("{0:#,##0}", km.DonToiThieu)}đ để áp dụng mã này." });

            double tienGiam = tongTien * (km.PhanTramGiam / 100.0);
            if (km.GiamToiDa.HasValue && tienGiam > km.GiamToiDa.Value)
                tienGiam = km.GiamToiDa.Value;

            double tongTienMoi = tongTien - tienGiam;

            HttpContext.Session.SetString("MaKhuyenMai", km.MaKhuyenMai);
            HttpContext.Session.SetString("SoTienGiam", tienGiam.ToString());
            HttpContext.Session.SetString("TongTienSauGiam", tongTienMoi.ToString());

            return Json(new
            {
                success = true,
                message = "Áp dụng mã thành công!",
                tienGiam,
                tongTienMoi
            });
        }
    }
}