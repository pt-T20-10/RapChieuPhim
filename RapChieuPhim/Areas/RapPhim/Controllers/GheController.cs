using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Models.Entities;

namespace RapChieuPhim.Areas.RapPhim.Controllers
{
    [Area("RapPhim")]
    public class GheController : Controller
    {
        private readonly AppDbContext _context;

        public GheController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/Ghe
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Ghe
                .Include(g => g.MaLoaiGheNavigation)
                .Include(g => g.MaPhongNavigation)
                .Where(g => !g.DaXoa);
            return View(await appDbContext.ToListAsync());
        }

        // =============================================
        // GET: RapPhim/Ghe/SoDoGhe?maPhong=PC01
        // Xem sơ đồ ghế theo phòng
        // =============================================
        public async Task<IActionResult> SoDoGhe(string maPhong)
        {
            if (string.IsNullOrEmpty(maPhong))
                return RedirectToAction("Index", "PhongChieu");

            var phong = await _context.PhongChieu
                .Include(p => p.MaLoaiPhongNavigation)
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong && !p.DaXoa);

            if (phong == null) return NotFound();

            var ghes = await _context.Ghe
                .Include(g => g.MaLoaiGheNavigation)
                .Where(g => g.MaPhong == maPhong && !g.DaXoa)
                .OrderBy(g => g.TenHang)
                .ThenBy(g => g.SoThu.Length)
                .ThenBy(g => g.SoThu)
                .ToListAsync();

            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            if (TempData["Error"] != null) ViewBag.Error = TempData["Error"];

            ViewBag.Phong = phong;
            return View(ghes);
        }

        // =============================================
        // POST: RapPhim/Ghe/DoiTrangThai
        // Chuyển trạng thái ghế theo sơ đồ trạng thái
        // Trống → DangBaoTri → Trống
        // Trống → DaKhoa → Trống (mở khóa)
        // DaKhoa: khóa/mở khóa hàng loạt
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiTrangThai(string maGhe, string trangThaiMoi, string maPhong)
        {
            var ghe = await _context.Ghe.FindAsync(maGhe);
            if (ghe == null)
            {
                TempData["Error"] = "Không tìm thấy ghế.";
                return RedirectToAction(nameof(SoDoGhe), new { maPhong });
            }

            string trangThaiCu = ghe.TrangThai;
            bool hopLe = false;
            string thongBao = "";

            // Kiểm tra chuyển trạng thái hợp lệ theo sơ đồ
            switch (trangThaiMoi)
            {
                case "DangBaoTri":
                    // Trống → Đang bảo trì (lỗi cơ sở vật chất)
                    if (trangThaiCu == "Trong") { hopLe = true; thongBao = $"Ghế {maGhe} chuyển sang Đang bảo trì."; }
                    break;

                case "Trong":
                    // DangBaoTri → Trống (hoàn tất sửa chữa)
                    // DaKhoa → Trống (quản lý mở khóa)
                    // DaDat → Trống (khách hoàn vé / rạp hủy vé)
                    if (trangThaiCu == "DangBaoTri" || trangThaiCu == "DaKhoa" || trangThaiCu == "DaDat")
                    { hopLe = true; thongBao = $"Ghế {maGhe} đã chuyển về Trống."; }
                    break;

                case "DaKhoa":
                    // Trống → Đã khóa (quản lý khóa ghế sự kiện)
                    if (trangThaiCu == "Trong") { hopLe = true; thongBao = $"Ghế {maGhe} đã được khóa."; }
                    break;

                default:
                    thongBao = "Trạng thái không hợp lệ.";
                    break;
            }

            if (!hopLe)
            {
                TempData["Error"] = $"Không thể chuyển ghế {maGhe} từ '{TenTrangThai(trangThaiCu)}' sang '{TenTrangThai(trangThaiMoi)}'.";
                return RedirectToAction(nameof(SoDoGhe), new { maPhong });
            }

            ghe.TrangThai = trangThaiMoi;
            _context.Update(ghe);
            await _context.SaveChangesAsync();

            TempData["Success"] = thongBao;
            return RedirectToAction(nameof(SoDoGhe), new { maPhong });
        }

        // =============================================
        // POST: RapPhim/Ghe/DoiTrangThaiHangLoat
        // Khóa/mở khóa nhiều ghế cùng lúc (theo hàng hoặc tất cả)
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiTrangThaiHangLoat(
            string maPhong,
            string? tenHang,        // null = tất cả phòng
            string trangThaiMoi)
        {
            var query = _context.Ghe.Where(g => g.MaPhong == maPhong && !g.DaXoa);

            if (!string.IsNullOrEmpty(tenHang))
                query = query.Where(g => g.TenHang == tenHang);

            // Chỉ cho phép: Trống → DaKhoa hoặc DaKhoa → Trống
            if (trangThaiMoi == "DaKhoa")
                query = query.Where(g => g.TrangThai == "Trong");
            else if (trangThaiMoi == "Trong")
                query = query.Where(g => g.TrangThai == "DaKhoa");
            else
            {
                TempData["Error"] = "Thao tác hàng loạt chỉ hỗ trợ khóa/mở khóa ghế.";
                return RedirectToAction(nameof(SoDoGhe), new { maPhong });
            }

            var dsGhe = await query.ToListAsync();
            if (!dsGhe.Any())
            {
                TempData["Error"] = "Không có ghế nào phù hợp để thực hiện thao tác.";
                return RedirectToAction(nameof(SoDoGhe), new { maPhong });
            }

            foreach (var g in dsGhe)
            {
                g.TrangThai = trangThaiMoi;
                _context.Update(g);
            }
            await _context.SaveChangesAsync();

            string phamVi = string.IsNullOrEmpty(tenHang) ? "toàn phòng" : $"hàng {tenHang}";
            string hanh = trangThaiMoi == "DaKhoa" ? "khóa" : "mở khóa";
            TempData["Success"] = $"Đã {hanh} {dsGhe.Count} ghế ({phamVi}).";
            return RedirectToAction(nameof(SoDoGhe), new { maPhong });
        }

        // =============================================
        // CRUD cơ bản (giữ nguyên từ bản cũ)
        // =============================================
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();
            var ghe = await _context.Ghe
                .Include(g => g.MaLoaiGheNavigation)
                .Include(g => g.MaPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaGhe == id);
            if (ghe == null) return NotFound();
            return View(ghe);
        }

        public IActionResult Create()
        {
            var ghe = new Ghe { TrangThai = "Trong", DaXoa = false };
            ViewData["MaLoaiGhe"] = new SelectList(_context.LoaiGhe.Where(l => !l.DaXoa), "MaLoaiGhe", "TenLoaiGhe");
            ViewData["MaPhong"] = new SelectList(_context.PhongChieu.Where(p => !p.DaXoa), "MaPhong", "TenPhong");
            return View(ghe);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaGhe,MaPhong,TenHang,SoThu,MaLoaiGhe,TrangThai,DaXoa")] Ghe ghe)
        {
            ModelState.Remove("MaLoaiGheNavigation");
            ModelState.Remove("MaPhongNavigation");
            ModelState.Remove("ChiTietVe");
            if (ModelState.IsValid)
            {
                ghe.DaXoa = false;
                _context.Add(ghe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaLoaiGhe"] = new SelectList(_context.LoaiGhe.Where(l => !l.DaXoa), "MaLoaiGhe", "TenLoaiGhe", ghe.MaLoaiGhe);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieu.Where(p => !p.DaXoa), "MaPhong", "TenPhong", ghe.MaPhong);
            return View(ghe);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var ghe = await _context.Ghe.FindAsync(id);
            if (ghe == null) return NotFound();
            ViewData["MaLoaiGhe"] = new SelectList(_context.LoaiGhe.Where(l => !l.DaXoa), "MaLoaiGhe", "TenLoaiGhe", ghe.MaLoaiGhe);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieu.Where(p => !p.DaXoa), "MaPhong", "TenPhong", ghe.MaPhong);
            return View(ghe);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaGhe,MaPhong,TenHang,SoThu,MaLoaiGhe,TrangThai,DaXoa")] Ghe ghe)
        {
            if (id != ghe.MaGhe) return NotFound();
            ModelState.Remove("MaLoaiGheNavigation");
            ModelState.Remove("MaPhongNavigation");
            ModelState.Remove("ChiTietVe");
            if (ModelState.IsValid)
            {
                try { _context.Update(ghe); await _context.SaveChangesAsync(); }
                catch (DbUpdateConcurrencyException)
                { if (!_context.Ghe.Any(e => e.MaGhe == id)) return NotFound(); throw; }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaLoaiGhe"] = new SelectList(_context.LoaiGhe.Where(l => !l.DaXoa), "MaLoaiGhe", "TenLoaiGhe", ghe.MaLoaiGhe);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieu.Where(p => !p.DaXoa), "MaPhong", "TenPhong", ghe.MaPhong);
            return View(ghe);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();
            var ghe = await _context.Ghe
                .Include(g => g.MaLoaiGheNavigation)
                .Include(g => g.MaPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaGhe == id);
            if (ghe == null) return NotFound();
            var coVe = await _context.ChiTietVe.AnyAsync(v => v.MaGhe == id && v.TrangThai == "ChuaSuDung" && !v.DaXoa);
            ViewBag.CoVe = coVe;
            return View(ghe);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var ghe = await _context.Ghe.FindAsync(id);
            if (ghe != null) { ghe.DaXoa = true; _context.Update(ghe); }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =============================================
        // Helper
        // =============================================
        private static string TenTrangThai(string tt) => tt switch
        {
            "Trong" => "Trống",
            "DangGiu" => "Đang giữ",
            "DaDat" => "Đã đặt",
            "DangBaoTri" => "Đang bảo trì",
            "DaKhoa" => "Đã khóa",
            _ => tt
        };

        private bool GheExists(string id) => _context.Ghe.Any(e => e.MaGhe == id);
    }
}