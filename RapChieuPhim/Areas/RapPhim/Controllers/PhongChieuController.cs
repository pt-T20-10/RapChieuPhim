using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Models.Entities;
using ClosedXML.Excel;

namespace RapChieuPhim.Areas.RapPhim.Controllers
{
    [Area("RapPhim")]
    public class PhongChieuController : Controller
    {
        private readonly AppDbContext _context;

        public PhongChieuController(AppDbContext context)
        {
            _context = context;
        }

        // =============================================
        // GET: RapPhim/PhongChieu
        // =============================================
        public async Task<IActionResult> Index()
        {
            if (TempData["Success"] != null) ViewBag.Success = TempData["Success"];
            if (TempData["Error"] != null) ViewBag.Error = TempData["Error"];

            var list = await _context.PhongChieu
                .Include(p => p.MaLoaiPhongNavigation)
                .Where(p => !p.DaXoa)
                .ToListAsync();
            return View(list);
        }

        // =============================================
        // GET: RapPhim/PhongChieu/Details/5
        // =============================================
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var phongChieu = await _context.PhongChieu
                .Include(p => p.MaLoaiPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaPhong == id && !m.DaXoa);

            if (phongChieu == null) return NotFound();
            return View(phongChieu);
        }

        // =============================================
        // GET: RapPhim/PhongChieu/Create
        // =============================================
        public IActionResult Create()
        {
            var phongChieu = new PhongChieu
            {
                TrangThai = "HoatDong",
                DaXoa = false
            };
            LoadLoaiPhongDropdown();
            LoadLoaiGheDropdown(); // dùng cho chọn loại ghế mặc định
            return View(phongChieu);
        }

        // =============================================
        // POST: RapPhim/PhongChieu/Create
        // Tự động sinh ghế theo SoGhe, mỗi hàng 10 ghế, tên hàng A/B/C...
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("MaPhong,TenPhong,MaLoaiPhong,SoGhe,TrangThai,DaXoa")]
            PhongChieu phongChieu,
            string maLoaiGheMacDinh = "LG01")
        {
            ModelState.Remove("MaLoaiPhongNavigation");
            ModelState.Remove("Ghe");
            ModelState.Remove("SuatChieu");

            // Validate trùng mã
            if (await _context.PhongChieu.AnyAsync(p => p.MaPhong == phongChieu.MaPhong))
                ModelState.AddModelError("MaPhong", "Mã phòng đã tồn tại.");

            if (string.IsNullOrEmpty(phongChieu.MaLoaiPhong))
                ModelState.AddModelError("MaLoaiPhong", "Vui lòng chọn loại phòng.");

            if (phongChieu.SoGhe <= 0)
                ModelState.AddModelError("SoGhe", "Số ghế phải lớn hơn 0.");

            if (!ModelState.IsValid)
            {
                LoadLoaiPhongDropdown(phongChieu.MaLoaiPhong);
                LoadLoaiGheDropdown(maLoaiGheMacDinh);
                return View(phongChieu);
            }

            phongChieu.DaXoa = false;
            _context.Add(phongChieu);
            await _context.SaveChangesAsync();

            // ---- Tự động sinh ghế ----
            // Mỗi hàng tối đa 10 ghế, tên hàng theo bảng chữ cái A, B, C...
            var dsGhe = SinhDanhSachGhe(phongChieu.MaPhong, phongChieu.SoGhe, maLoaiGheMacDinh);
            _context.Ghe.AddRange(dsGhe);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Thêm phòng '{phongChieu.TenPhong}' thành công! Đã tạo {dsGhe.Count} ghế.";
            return RedirectToAction(nameof(Index));
        }

        // =============================================
        // GET: RapPhim/PhongChieu/Edit/5
        // =============================================
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var phongChieu = await _context.PhongChieu.FindAsync(id);
            if (phongChieu == null) return NotFound();

            LoadLoaiPhongDropdown(phongChieu.MaLoaiPhong);
            return View(phongChieu);
        }

        // =============================================
        // POST: RapPhim/PhongChieu/Edit/5
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id,
            [Bind("MaPhong,TenPhong,MaLoaiPhong,SoGhe,TrangThai,DaXoa")]
            PhongChieu phongChieu)
        {
            if (id != phongChieu.MaPhong) return NotFound();

            ModelState.Remove("MaLoaiPhongNavigation");
            ModelState.Remove("Ghe");
            ModelState.Remove("SuatChieu");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phongChieu);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Cập nhật phòng '{phongChieu.TenPhong}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.PhongChieu.Any(e => e.MaPhong == id)) return NotFound();
                    throw;
                }
            }

            LoadLoaiPhongDropdown(phongChieu.MaLoaiPhong);
            return View(phongChieu);
        }

        // =============================================
        // GET: RapPhim/PhongChieu/Delete/5
        // =============================================
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var phongChieu = await _context.PhongChieu
                .Include(p => p.MaLoaiPhongNavigation)
                .Include(p => p.SuatChieu)
                .Include(p => p.Ghe)
                .FirstOrDefaultAsync(m => m.MaPhong == id && !m.DaXoa);

            if (phongChieu == null) return NotFound();

            var soSuatChieuHoatDong = phongChieu.SuatChieu
                .Count(s => !s.DaXoa && s.TrangThai != "DaKetThuc" && s.TrangThai != "DaHuy");
            var soGheDaDat = phongChieu.Ghe
                .Count(g => !g.DaXoa && (g.TrangThai == "DaDat" || g.TrangThai == "DangGiu"));

            ViewBag.CoRangBuoc = soSuatChieuHoatDong > 0 || soGheDaDat > 0;
            ViewBag.SoSuatChieu = soSuatChieuHoatDong;
            ViewBag.SoGheDaDat = soGheDaDat;
            return View(phongChieu);
        }

        // =============================================
        // POST: RapPhim/PhongChieu/Delete/5  — Soft delete
        // =============================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var phongChieu = await _context.PhongChieu
                .Include(p => p.SuatChieu)
                .Include(p => p.Ghe)
                .FirstOrDefaultAsync(p => p.MaPhong == id && !p.DaXoa);

            if (phongChieu == null) return NotFound();

            var soSuatChieuHoatDong = phongChieu.SuatChieu
                .Count(s => !s.DaXoa && s.TrangThai != "DaKetThuc" && s.TrangThai != "DaHuy");
            var soGheDaDat = phongChieu.Ghe
                .Count(g => !g.DaXoa && (g.TrangThai == "DaDat" || g.TrangThai == "DangGiu"));

            if (soSuatChieuHoatDong > 0 || soGheDaDat > 0)
            {
                TempData["Error"] = "Không thể xóa — dữ liệu đang được sử dụng trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            // Soft delete phòng
            phongChieu.DaXoa = true;
            _context.Update(phongChieu);

            // Soft delete ghế của phòng
            foreach (var ghe in phongChieu.Ghe.Where(g => !g.DaXoa))
            {
                ghe.DaXoa = true;
                _context.Update(ghe);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Xóa phòng '{phongChieu.TenPhong}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // =============================================
        // GET: RapPhim/PhongChieu/ExportExcel
        // =============================================
        public async Task<IActionResult> ExportExcel()
        {
            var list = await _context.PhongChieu
                .Include(p => p.MaLoaiPhongNavigation)
                .Include(p => p.Ghe)
                .Where(p => !p.DaXoa)
                .OrderBy(p => p.MaPhong)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Phòng chiếu");

            // Header
            string[] headers = { "Mã phòng", "Tên phòng", "Loại phòng", "Số ghế (cấu hình)", "Ghế thực tế", "Trạng thái" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a73e8");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 2;
            foreach (var p in list)
            {
                ws.Cell(row, 1).Value = p.MaPhong;
                ws.Cell(row, 2).Value = p.TenPhong;
                ws.Cell(row, 3).Value = p.MaLoaiPhongNavigation?.TenLoaiPhong ?? p.MaLoaiPhong;
                ws.Cell(row, 4).Value = p.SoGhe;
                ws.Cell(row, 5).Value = p.Ghe.Count(g => !g.DaXoa);
                ws.Cell(row, 6).Value = p.TrangThai switch
                {
                    "HoatDong" => "Hoạt động",
                    "BaoDuong" => "Bảo dưỡng",
                    "Dong" => "Đóng",
                    _ => p.TrangThai
                };
                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DanhSachPhongChieu_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // =============================================
        // Helpers
        // =============================================
        private void LoadLoaiPhongDropdown(string? selected = null)
        {
            ViewData["MaLoaiPhong"] = new SelectList(
                _context.LoaiPhong.Where(l => !l.DaXoa).ToList(),
                "MaLoaiPhong", "TenLoaiPhong", selected);
        }

        private void LoadLoaiGheDropdown(string? selected = "LG01")
        {
            ViewData["MaLoaiGhe"] = new SelectList(
                _context.LoaiGhe.Where(l => !l.DaXoa).ToList(),
                "MaLoaiGhe", "TenLoaiGhe", selected);
        }

        /// <summary>
        /// Sinh danh sách ghế tự động: mỗi hàng 10 ghế, hàng A/B/C...
        /// MaGhe = "{MaPhong}_{Hang}{SoThu}" VD: PC01_A1
        /// </summary>
        private List<Ghe> SinhDanhSachGhe(string maPhong, int soGhe, string maLoaiGhe)
        {
            var result = new List<Ghe>();
            int gheConLai = soGhe;
            int hangIndex = 0; // 0=A, 1=B, ...

            while (gheConLai > 0)
            {
                string tenHang = ((char)('A' + hangIndex)).ToString();
                int soGheHang = Math.Min(10, gheConLai);

                for (int so = 1; so <= soGheHang; so++)
                {
                    result.Add(new Ghe
                    {
                        MaGhe = $"{maPhong}_{tenHang}{so}",
                        MaPhong = maPhong,
                        TenHang = tenHang,
                        SoThu = so.ToString(),
                        MaLoaiGhe = maLoaiGhe,
                        TrangThai = "Trong",
                        DaXoa = false
                    });
                }

                gheConLai -= soGheHang;
                hangIndex++;
            }

            return result;
        }
    }
}