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
using System.IO;

namespace RapChieuPhim.Areas.RapPhim.Controllers
{
    [Area("RapPhim")]
    public class SuatChieuController : Controller
    {
        private readonly AppDbContext _context;

        public SuatChieuController(AppDbContext context)
        {
            _context = context;
        }

        // ================= AUTO UPDATE COMMON =================
        private void UpdateTrangThai(SuatChieu s)
        {
            var now = DateTime.Now;

            if (s.TrangThai == "DaLenLich" && s.ThoiGianBatDau <= now)
            {
                s.TrangThai = "DangChieu";
            }
            else if (s.TrangThai == "DangChieu" && s.ThoiGianKetThuc <= now)
            {
                s.TrangThai = "DaKetThuc";
            }
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index()
        {
            var data = await _context.SuatChieu
                .Include(s => s.MaPhimNavigation)
                .Include(s => s.MaPhongNavigation)
                .Where(s => !s.DaXoa)
                .ToListAsync();

            foreach (var s in data)
            {
                UpdateTrangThai(s);
            }

            return View(data);
        }

        // ================= DETAILS =================
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var suatChieu = await _context.SuatChieu
                .Include(s => s.MaPhimNavigation)
                .Include(s => s.MaPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaSuatChieu == id);

            if (suatChieu == null) return NotFound();

            UpdateTrangThai(suatChieu);

            return View(suatChieu);
        }

        // ================= CREATE =================
        public IActionResult Create()
        {
            ViewData["MaPhim"] = new SelectList(
                _context.Phim.Where(p => !p.DaXoa),
                "MaPhim", "TenPhim");

            ViewData["MaPhong"] = new SelectList(
                _context.PhongChieu.Where(p => !p.DaXoa),
                "MaPhong", "TenPhong");

            return View(new SuatChieu
            {
                TrangThai = "DaLenLich",
                DaXoa = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SuatChieu suatChieu)
        {
            ModelState.Remove("MaPhimNavigation");
            ModelState.Remove("MaPhongNavigation");
            ModelState.Remove("ChiTietVe");

            if (suatChieu.ThoiGianKetThuc <= suatChieu.ThoiGianBatDau)
                ModelState.AddModelError("ThoiGianKetThuc", "Thời gian kết thúc phải lớn hơn thời gian bắt đầu");

            if (!string.IsNullOrEmpty(suatChieu.MaPhong))
            {
                var trung = await _context.SuatChieu.AnyAsync(s =>
                    s.MaPhong == suatChieu.MaPhong &&
                    !s.DaXoa &&
                    s.ThoiGianBatDau < suatChieu.ThoiGianKetThuc &&
                    s.ThoiGianKetThuc > suatChieu.ThoiGianBatDau);

                if (trung)
                    ModelState.AddModelError("", "Phòng đã có suất chiếu trùng thời gian");
            }

            if (ModelState.IsValid)
            {
                suatChieu.DaXoa = false;
                suatChieu.TrangThai = "DaLenLich";

                _context.Add(suatChieu);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thêm suất chiếu thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaPhim"] = new SelectList(_context.Phim.Where(p => !p.DaXoa), "MaPhim", "TenPhim", suatChieu.MaPhim);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieu.Where(p => !p.DaXoa), "MaPhong", "TenPhong", suatChieu.MaPhong);

            return View(suatChieu);
        }

        // ================= EDIT =================
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var suatChieu = await _context.SuatChieu.FindAsync(id);
            if (suatChieu == null) return NotFound();

            UpdateTrangThai(suatChieu);

            ViewData["MaPhim"] = new SelectList(_context.Phim.Where(p => !p.DaXoa), "MaPhim", "TenPhim", suatChieu.MaPhim);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieu.Where(p => !p.DaXoa), "MaPhong", "TenPhong", suatChieu.MaPhong);

            return View(suatChieu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, SuatChieu suatChieu)
        {
            if (id != suatChieu.MaSuatChieu) return NotFound();

            ModelState.Remove("MaPhimNavigation");
            ModelState.Remove("MaPhongNavigation");
            ModelState.Remove("ChiTietVe");

            if (suatChieu.ThoiGianKetThuc <= suatChieu.ThoiGianBatDau)
                ModelState.AddModelError("ThoiGianKetThuc", "Thời gian kết thúc phải lớn hơn thời gian bắt đầu");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(suatChieu);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật suất chiếu thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.SuatChieu.Any(e => e.MaSuatChieu == id))
                        return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["MaPhim"] = new SelectList(_context.Phim.Where(p => !p.DaXoa), "MaPhim", "TenPhim", suatChieu.MaPhim);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieu.Where(p => !p.DaXoa), "MaPhong", "TenPhong", suatChieu.MaPhong);

            return View(suatChieu);
        }

        // ================= DELETE =================
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var suatChieu = await _context.SuatChieu
                .Include(s => s.MaPhimNavigation)
                .Include(s => s.MaPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaSuatChieu == id);

            if (suatChieu == null) return NotFound();

            UpdateTrangThai(suatChieu);

            var coVe = await _context.ChiTietVe
                .AnyAsync(v => v.MaSuatChieu == id && v.TrangThai == "ChuaSuDung" && !v.DaXoa);

            ViewBag.CoVe = coVe;

            return View(suatChieu);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var coVe = await _context.ChiTietVe
                .AnyAsync(v => v.MaSuatChieu == id && v.TrangThai == "ChuaSuDung" && !v.DaXoa);

            if (coVe)
            {
                TempData["Error"] = "Không thể xóa vì còn vé chưa sử dụng!";
                return RedirectToAction(nameof(Index));
            }

            var suatChieu = await _context.SuatChieu.FindAsync(id);
            if (suatChieu != null)
            {
                suatChieu.DaXoa = true;
                suatChieu.TrangThai = "DaHuy";

                _context.Update(suatChieu);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Xóa suất chiếu thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // ================= EXPORT EXCEL =================
        public async Task<IActionResult> ExportExcel()
        {
            var data = await _context.SuatChieu
                .Include(s => s.MaPhimNavigation)
                .Include(s => s.MaPhongNavigation)
                .Where(s => !s.DaXoa)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("SuatChieu");

                worksheet.Cell(1, 1).Value = "Mã suất";
                worksheet.Cell(1, 2).Value = "Phim";
                worksheet.Cell(1, 3).Value = "Phòng";
                worksheet.Cell(1, 4).Value = "Bắt đầu";
                worksheet.Cell(1, 5).Value = "Kết thúc";
                worksheet.Cell(1, 6).Value = "Giá";
                worksheet.Cell(1, 7).Value = "Trạng thái";

                int row = 2;

                foreach (var item in data)
                {
                    worksheet.Cell(row, 1).Value = item.MaSuatChieu;
                    worksheet.Cell(row, 2).Value = item.MaPhimNavigation?.TenPhim;
                    worksheet.Cell(row, 3).Value = item.MaPhongNavigation?.TenPhong;
                    worksheet.Cell(row, 4).Value = item.ThoiGianBatDau;
                    worksheet.Cell(row, 5).Value = item.ThoiGianKetThuc;
                    worksheet.Cell(row, 6).Value = item.GiaGoc;
                    worksheet.Cell(row, 7).Value = item.TrangThai;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    TempData["Success"] = "Xuất Excel thành công!";

                    return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "SuatChieu.xlsx");
                }
            }
        }

        private bool SuatChieuExists(string id)
        {
            return _context.SuatChieu.Any(e => e.MaSuatChieu == id);
        }

        public async Task<IActionResult> Cancel(string id)
        {
            var item = await _context.SuatChieu.FindAsync(id);
            if (item == null) return NotFound();

            item.TrangThai = "DaHuy";

            _context.Update(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Hủy suất chiếu thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}