using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using RapChieuPhim.Data;
using RapChieuPhim.Models.Entities;

namespace RapChieuPhim.Areas.RapPhim.Controllers
{
    [Area("RapPhim")]
    public class BanHangController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PayOSClient _payOS;

        public BanHangController(AppDbContext context, PayOSClient payOS)
        {
            _context = context;
            _payOS = payOS;
        }

        // ══════════════════════════════════════════════
        // TRANG CHỌN LOẠI BÁN
        // ══════════════════════════════════════════════

        public IActionResult Index() => View();

        // ══════════════════════════════════════════════
        // QUẦY BÁN VÉ
        // ══════════════════════════════════════════════

        // 1. DANH SÁCH PHIM
        [HttpGet]
        public async Task<IActionResult> BanVe()
        {
            var dsPhim = await _context.Phim
                .Include(p => p.MaTheLoaiNavigation)
                .Where(p => !p.DaXoa && p.TrangThai == "DangChieu")
                .OrderBy(p => p.TenPhim)
                .ToListAsync();

            return View(dsPhim);
        }

        // 2. CHỌN SUẤT CHIẾU + GHẾ
        [HttpGet]
        public async Task<IActionResult> ChonGhe(string maPhim)
        {
            var phim = await _context.Phim
                .Include(p => p.MaTheLoaiNavigation)
                .FirstOrDefaultAsync(p => p.MaPhim == maPhim && !p.DaXoa);

            if (phim == null) return NotFound();

            var dsSuatChieu = await _context.SuatChieu
                .Include(s => s.MaPhongNavigation)
                .Where(s => s.MaPhim == maPhim
                         && s.ThoiGianBatDau >= DateTime.Now
                         && s.TrangThai == "DaLenLich"
                         && !s.DaXoa)
                .OrderBy(s => s.ThoiGianBatDau)
                .ToListAsync();

            ViewBag.Phim = phim;
            ViewBag.SuatChieu = dsSuatChieu
                .GroupBy(s => s.ThoiGianBatDau.Date)
                .ToList();

            return View();
        }

        // 3. API LẤY SƠ ĐỒ GHẾ (tái sử dụng logic từ DatVeController)
        [HttpGet]
        public async Task<IActionResult> LaySoDoGhe(string maSuatChieu)
        {
            var suatChieu = await _context.SuatChieu
                .Include(s => s.MaPhongNavigation)
                .FirstOrDefaultAsync(s => s.MaSuatChieu == maSuatChieu && !s.DaXoa);

            if (suatChieu == null) return Json(new { success = false });

            var dsGhe = await _context.Ghe
                .Include(g => g.MaLoaiGheNavigation)
                .Where(g => g.MaPhong == suatChieu.MaPhong && !g.DaXoa)
                .OrderBy(g => g.TenHang).ThenBy(g => g.SoThu)
                .ToListAsync();

            var veCuaSuatChieu = await _context.ChiTietVe
                .Include(v => v.MaDonHangNavigation)
                .Where(v => v.MaSuatChieu == maSuatChieu
                         && !v.DaXoa
                         && v.MaDonHangNavigation.TrangThai != "DaHuy")
                .ToListAsync();

            var result = dsGhe.Select(g =>
            {
                int trangThai = 1; // Trống

                if (g.TrangThai == "DangBaoTri" || g.TrangThai == "DaKhoa")
                {
                    trangThai = 4; // Bảo trì
                }
                else
                {
                    var ve = veCuaSuatChieu.FirstOrDefault(v => v.MaGhe == g.MaGhe);
                    if (ve != null)
                    {
                        var trangThaiDon = ve.MaDonHangNavigation.TrangThai;
                        if (trangThaiDon == "DaThanhToan" || trangThaiDon == "DaXuatHoaDon")
                            trangThai = 2; // Đã đặt
                        else if (trangThaiDon == "ChoThanhToan")
                            trangThai = 3; // Đang giữ (online)
                    }
                }

                return new
                {
                    MaGhe = g.MaGhe,
                    TenGhe = g.TenHang + g.SoThu,
                    GiaVe = suatChieu.GiaGoc * g.MaLoaiGheNavigation.HeSoGia,
                    LoaiGhe = g.MaLoaiGheNavigation.TenLoaiGhe,
                    TrangThai = trangThai
                };
            }).ToList();

            return Json(new
            {
                success = true,
                dsGhe = result,
                tenPhong = suatChieu.MaPhongNavigation.TenPhong,
                tenPhim = suatChieu.MaPhongNavigation.TenPhong
            });
        }

        // 4. TẠO ĐƠN HÀNG + CHUYỂN SANG THANH TOÁN
        [HttpPost]
        public async Task<IActionResult> XacNhanVe(string maSuatChieu,
                                                    List<string> dsGhe)
        {
            if (string.IsNullOrEmpty(maSuatChieu) || dsGhe == null || !dsGhe.Any())
                return RedirectToAction("BanVe", "BanHang", new { area = "RapPhim"});

            // Kiểm tra bảo mật: ghế có bị lấy mất không
            var veDaBan = await _context.ChiTietVe
                .Include(v => v.MaDonHangNavigation)
                .Where(v => v.MaSuatChieu == maSuatChieu
                         && dsGhe.Contains(v.MaGhe)
                         && !v.DaXoa
                         && (v.MaDonHangNavigation.TrangThai == "DaThanhToan"
                          || v.MaDonHangNavigation.TrangThai == "DaXuatHoaDon"
                          || (v.MaDonHangNavigation.TrangThai == "ChoThanhToan"
                              && v.MaDonHangNavigation.NgayTao.AddMinutes(5) > DateTime.Now)))
                .AnyAsync();

            if (veDaBan)
                return Content("<script>alert('Ghế vừa được đặt bởi khách online. Vui lòng chọn ghế khác!'); window.history.back();</script>", "text/html");

            var suatChieu = await _context.SuatChieu
                .Include(s => s.MaPhimNavigation)
                .Include(s => s.MaPhongNavigation)
                .FirstOrDefaultAsync(s => s.MaSuatChieu == maSuatChieu);

            var gheList = await _context.Ghe
                .Include(g => g.MaLoaiGheNavigation)
                .Where(g => dsGhe.Contains(g.MaGhe))
                .ToListAsync();

            double tongTien = gheList.Sum(g =>
                suatChieu!.GiaGoc * g.MaLoaiGheNavigation.HeSoGia);

            // Lấy MaNhanVien từ Session
            string maNhanVien = HttpContext.Session.GetString("MaNhanVien") ?? "";
            string maDonHang = DateTime.Now.ToString("yyMMddHHmmss");

            var donHang = new DonHang
            {
                MaDonHang = maDonHang,
                MaNhanVien = string.IsNullOrEmpty(maNhanVien) ? null : maNhanVien,
                NgayTao = DateTime.Now,
                TongTienBanDau = tongTien,
                TongTienSauGiam = tongTien,
                TrangThai = "ChoThanhToan",
                DaXoa = false
            };
            _context.DonHang.Add(donHang);

            foreach (var g in gheList)
            {
                _context.ChiTietVe.Add(new ChiTietVe
                {
                    MaVe = "V" + DateTime.Now.Ticks.ToString()[^8..],
                    MaDonHang = maDonHang,
                    MaSuatChieu = maSuatChieu,
                    MaGhe = g.MaGhe,
                    GiaVe = suatChieu!.GiaGoc * g.MaLoaiGheNavigation.HeSoGia,
                    TrangThai = "ChoXuLy",   // chờ xử lý, chưa thanh toán
                    DaXoa = false
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("ThanhToanVe", "BanHang", new { area = "RapPhim", maDonHang });
        }

        // 5. TRANG THANH TOÁN VÉ
        [HttpGet]
        public async Task<IActionResult> ThanhToanVe(string maDonHang)
        {
            var donHang = await _context.DonHang
                .Include(d => d.MaNhanVienNavigation)
                .Include(d => d.ThanhToan)
                .Include(d => d.ChiTietVe)
                    .ThenInclude(v => v.MaSuatChieuNavigation)
                        .ThenInclude(s => s.MaPhimNavigation)
                .Include(d => d.ChiTietVe)                      
                    .ThenInclude(v => v.MaSuatChieuNavigation)
                        .ThenInclude(s => s.MaPhongNavigation)
                .Include(d => d.ChiTietVe)
                    .ThenInclude(v => v.MaGheNavigation)
                        .ThenInclude(g => g.MaLoaiGheNavigation)
                .Include(d => d.ChiTietDichVu)
                    .ThenInclude(dv => dv.MaDichVuNavigation)
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang
                                       && d.TrangThai == "ChoThanhToan");

            if (donHang == null) return NotFound();

            return View(donHang);
        }

        // 6A. THANH TOÁN TIỀN MẶT
        [HttpPost]
        public async Task<IActionResult> XuLyTienMat(string maDonHang, double tienNhan)
        {
            var donHang = await _context.DonHang
            .Include(d => d.ChiTietVe)
                .ThenInclude(v => v.MaSuatChieuNavigation)
                    .ThenInclude(s => s.MaPhimNavigation)
            .Include(d => d.ChiTietVe)
                .ThenInclude(v => v.MaSuatChieuNavigation)
                    .ThenInclude(s => s.MaPhongNavigation)
            .Include(d => d.ChiTietVe)
                .ThenInclude(v => v.MaGheNavigation)
                    .ThenInclude(g => g.MaLoaiGheNavigation)
            .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);

            if (donHang == null || donHang.TrangThai != "ChoThanhToan")
                return BadRequest();

            if (tienNhan < donHang.TongTienSauGiam)
                return Json(new { success = false, message = "Số tiền nhận không đủ" });

            // Cập nhật đơn hàng
            donHang.TrangThai = "DaThanhToan";
            foreach (var ve in donHang.ChiTietVe)
            {
                ve.TrangThai = "ChuaSuDung";
                ve.MaQr = Guid.NewGuid().ToString("N").ToUpper();
            }
            if (!string.IsNullOrEmpty(donHang.MaKhuyenMai))
            {
                var km = await _context.KhuyenMai
                    .FirstOrDefaultAsync(k => k.MaKhuyenMai == donHang.MaKhuyenMai && !k.DaXoa);
                if (km != null && km.SoLuongConLai > 0)
                {
                    km.SoLuongConLai -= 1;
                    if (km.SoLuongConLai == 0) km.TrangThai = "DaKetThuc";
                }
            }

            // Tạo bản ghi ThanhToan
            _context.ThanhToan.Add(new ThanhToan
            {
                MaThanhToan = "TT" + DateTime.Now.ToString("yyMMddHHmmss"),
                MaDonHang = maDonHang,
                PhuongThuc = "TienMat",
                SoTien = tienNhan,
                TrangThai = "ThanhCong",
                NgayThanhToan = DateTime.Now,
                DaXoa = false
            });

            await _context.SaveChangesAsync();

            double tienThua = tienNhan - donHang.TongTienSauGiam;
            return Json(new
            {
                success = true,
                tienThua,
                redirectUrl = Url.Action("InVe", "BanHang",
                    new { area = "RapPhim", maDonHang })
            });
        }

        // 6B. THANH TOÁN QR — TẠO LINK PAYOS
        [HttpPost]
        public async Task<IActionResult> TaoThanhToanQR(string maDonHang)
        {
            var donHang = await _context.DonHang
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);

            if (donHang == null) return BadRequest();

            HttpContext.Session.SetString("LoaiGiaoDich", "BanVeTrucTiep");

            var domain = $"{Request.Scheme}://{Request.Host}";
            long orderCode = long.Parse(maDonHang);

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)donHang.TongTienSauGiam,
                Description = $"Ban ve {DateTime.Now:ddMMyyHHmm}",
                CancelUrl = $"{domain}/NguoiDung/ThanhToan/KetQua?orderCode={orderCode}&cancel=true",
                ReturnUrl = $"{domain}/NguoiDung/ThanhToan/KetQua?orderCode={orderCode}&cancel=false"
            };

            var paymentLink = await _payOS.PaymentRequests.CreateAsync(paymentRequest);

            return Json(new { success = true, checkoutUrl = paymentLink.CheckoutUrl });
        }

        // 7. TRANG IN VÉ + HÓA ĐƠN
        [HttpGet]
        public async Task<IActionResult> InVe(string maDonHang)
        {
            var donHang = await _context.DonHang
                .Include(d => d.MaNhanVienNavigation)
                .Include(d => d.ThanhToan)
                .Include(d => d.ChiTietVe)
                    .ThenInclude(v => v.MaSuatChieuNavigation)
                        .ThenInclude(s => s.MaPhimNavigation)
                .Include(d => d.ChiTietVe)
                    .ThenInclude(v => v.MaSuatChieuNavigation)
                        .ThenInclude(s => s.MaPhongNavigation)
                .Include(d => d.ChiTietVe)
                    .ThenInclude(v => v.MaGheNavigation)
                        .ThenInclude(g => g.MaLoaiGheNavigation)
                .Include(d => d.ChiTietDichVu)
                    .ThenInclude(dv => dv.MaDichVuNavigation)
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang
                                       && d.TrangThai == "DaThanhToan");

            if (donHang == null) return NotFound();

            return View(donHang);
        }
        // ══════════════════════════════════════════════
        // QUẦY BÁN DỊCH VỤ
        // ══════════════════════════════════════════════

        // 1. CHỌN MÓN
        [HttpGet]
        public async Task<IActionResult> BanDichVu()
        {
            var menu = await _context.DanhMucDichVu
                .Include(dm => dm.DichVu.Where(dv => !dv.DaXoa))
                .Where(dm => !dm.DaXoa && dm.DichVu.Any(dv => !dv.DaXoa))
                .ToListAsync();

            return View(menu);
        }

        // 2. TẠO ĐƠN HÀNG DỊCH VỤ
        [HttpPost]
        public async Task<IActionResult> XacNhanDichVu(
            List<string> dsMaDichVu, List<int> dsSoLuong)
        {
            if (dsMaDichVu == null || !dsMaDichVu.Any())
                return RedirectToAction("BanDichVu", "BanHang", new { area = "RapPhim" });

            var dichVuList = await _context.DichVu
                .Where(dv => dsMaDichVu.Contains(dv.MaDichVu) && !dv.DaXoa)
                .ToListAsync();

            string maNhanVien = HttpContext.Session.GetString("MaNhanVien") ?? "";
            string maDonHang = "DV" + DateTime.Now.ToString("yyMMddHHmmss");

            double tongTien = 0;
            for (int i = 0; i < dsMaDichVu.Count; i++)
            {
                var dv = dichVuList.FirstOrDefault(d => d.MaDichVu == dsMaDichVu[i]);
                if (dv != null) tongTien += dv.GiaBan * dsSoLuong[i];
            }

            var donHang = new DonHang
            {
                MaDonHang = maDonHang,
                MaNhanVien = string.IsNullOrEmpty(maNhanVien) ? null : maNhanVien,
                NgayTao = DateTime.Now,
                TongTienBanDau = tongTien,
                TongTienSauGiam = tongTien,
                TrangThai = "ChoThanhToan",
                DaXoa = false
            };
            _context.DonHang.Add(donHang);

            for (int i = 0; i < dsMaDichVu.Count; i++)
            {
                var dv = dichVuList.FirstOrDefault(d => d.MaDichVu == dsMaDichVu[i]);
                if (dv == null || dsSoLuong[i] <= 0) continue;

                _context.ChiTietDichVu.Add(new ChiTietDichVu
                {
                    MaChiTiet = "CT" + DateTime.Now.Ticks.ToString()[^8..] + i,
                    MaDonHang = maDonHang,
                    MaDichVu = dsMaDichVu[i],
                    SoLuong = dsSoLuong[i],
                    DonGia = dv.GiaBan,
                    // ThanhTien là computed column — không set
                    DaXoa = false
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("ThanhToanDichVu", "BanHang",
                new { area = "RapPhim", maDonHang });
        }

        // 3. TRANG THANH TOÁN DỊCH VỤ
        [HttpGet]
        public async Task<IActionResult> ThanhToanDichVu(string maDonHang)
        {
            var donHang = await _context.DonHang
                .Include(d => d.MaNhanVienNavigation)
                .Include(d => d.ChiTietDichVu)
                    .ThenInclude(ct => ct.MaDichVuNavigation)
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang
                                       && d.TrangThai == "ChoThanhToan");

            if (donHang == null) return NotFound();

            return View(donHang);
        }

        // 4A. THANH TOÁN TIỀN MẶT DỊCH VỤ
        [HttpPost]
        public async Task<IActionResult> XuLyTienMatDichVu(string maDonHang, double tienNhan)
        {
            var donHang = await _context.DonHang
                .Include(d => d.ChiTietDichVu)
                    .ThenInclude(ct => ct.MaDichVuNavigation)
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);

            if (donHang == null || donHang.TrangThai != "ChoThanhToan")
                return BadRequest();

            if (tienNhan < donHang.TongTienSauGiam)
                return Json(new { success = false, message = "Số tiền nhận không đủ" });

            donHang.TrangThai = "DaThanhToan";
            if (!string.IsNullOrEmpty(donHang.MaKhuyenMai))
            {
                var km = await _context.KhuyenMai
                    .FirstOrDefaultAsync(k => k.MaKhuyenMai == donHang.MaKhuyenMai && !k.DaXoa);
                if (km != null && km.SoLuongConLai > 0)
                {
                    km.SoLuongConLai -= 1;
                    if (km.SoLuongConLai == 0) km.TrangThai = "DaKetThuc";
                }
            }
            _context.ThanhToan.Add(new ThanhToan
            {
                MaThanhToan = "TT" + DateTime.Now.ToString("yyMMddHHmmss"),
                MaDonHang = maDonHang,
                PhuongThuc = "TienMat",
                SoTien = tienNhan,
                TrangThai = "ThanhCong",
                NgayThanhToan = DateTime.Now,
                DaXoa = false
            });

            await _context.SaveChangesAsync();

            double tienThua = tienNhan - donHang.TongTienSauGiam;
            return Json(new
            {
                success = true,
                tienThua,
                redirectUrl = Url.Action("InHoaDon", "BanHang",
                    new { area = "RapPhim", maDonHang })
            });
        }

        // 4B. THANH TOÁN QR DỊCH VỤ
        [HttpPost]
        public async Task<IActionResult> TaoThanhToanQRDichVu(string maDonHang)
        {
            var donHang = await _context.DonHang
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);

            if (donHang == null) return BadRequest();

            HttpContext.Session.SetString("LoaiGiaoDich", "BanDichVuTrucTiep");

            var domain = $"{Request.Scheme}://{Request.Host}";
            long orderCode = long.Parse(maDonHang.Replace("DV", ""));

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)donHang.TongTienSauGiam,
                Description = $"Ban DV {DateTime.Now:ddMMyyHHmm}",
                CancelUrl = $"{domain}/NguoiDung/ThanhToan/KetQua?orderCode={orderCode}&cancel=true",
                ReturnUrl = $"{domain}/NguoiDung/ThanhToan/KetQua?orderCode={orderCode}&cancel=false"
            };

            var paymentLink = await _payOS.PaymentRequests.CreateAsync(paymentRequest);
            return Json(new { success = true, checkoutUrl = paymentLink.CheckoutUrl });
        }

        // 5. IN HÓA ĐƠN DỊCH VỤ
        [HttpGet]
        public async Task<IActionResult> InHoaDon(string maDonHang)
        {
            var donHang = await _context.DonHang
                .Include(d => d.MaNhanVienNavigation)
                .Include(d => d.ThanhToan)
                .Include(d => d.ChiTietDichVu)
                    .ThenInclude(ct => ct.MaDichVuNavigation)
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang
                                       && d.TrangThai == "DaThanhToan");

            if (donHang == null) return NotFound();

            return View(donHang);
        }
        [HttpPost]
        public async Task<IActionResult> ApDungKhuyenMai(string maCode, string maDonHang)
        {
            var donHang = await _context.DonHang.FindAsync(maDonHang);
            if (donHang == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            double tongTien = donHang.TongTienBanDau;

            var km = await _context.KhuyenMai
                .FirstOrDefaultAsync(x => x.MaCode == maCode && !x.DaXoa);

            if (km == null)
                return Json(new { success = false, message = "Mã giảm giá không tồn tại." });
            if (km.TrangThai != "DangApDung")
                return Json(new { success = false, message = "Mã chưa được kích hoạt hoặc đã đóng." });
            if (DateTime.Now < km.TuNgay || DateTime.Now > km.DenNgay)
                return Json(new { success = false, message = "Mã không trong thời gian sử dụng." });
            if (km.SoLuongConLai <= 0)
                return Json(new { success = false, message = "Mã đã hết lượt sử dụng." });
            if (tongTien < km.DonToiThieu)
                return Json(new { success = false, message = $"Đơn tối thiểu {km.DonToiThieu:N0}₫ để dùng mã này." });

            double tienGiam = tongTien * (km.PhanTramGiam / 100.0);
            if (km.GiamToiDa.HasValue && tienGiam > km.GiamToiDa.Value)
                tienGiam = km.GiamToiDa.Value;

            double tongTienMoi = tongTien - tienGiam;

            // Cập nhật thẳng vào DonHang luôn
            donHang.MaKhuyenMai = km.MaKhuyenMai;
            donHang.TongTienSauGiam = tongTienMoi;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Áp dụng thành công! Giảm {tienGiam:N0}₫",
                tienGiam,
                tongTienMoi
            });
        }

        [HttpPost]
        public async Task<IActionResult> HuyKhuyenMai(string maDonHang)
        {
            var donHang = await _context.DonHang.FindAsync(maDonHang);
            if (donHang != null)
            {
                donHang.MaKhuyenMai = null;
                donHang.TongTienSauGiam = donHang.TongTienBanDau;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true, tongTienGoc = donHang?.TongTienBanDau ?? 0 });
        }
    }

}