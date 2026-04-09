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
    public class PhimController : Controller
    {
        private readonly AppDbContext _context;

        public PhimController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== AUTO UPDATE STATE =====================
        private void UpdateTrangThaiTuDong(Phim phim)
        {
            if (phim.DaXoa) return;

            var today = DateOnly.FromDateTime(DateTime.Now);

            if (phim.TrangThai == "SapChieu" &&
                phim.NgayPhatHanh <= today)
            {
                phim.TrangThai = "DangChieu";
            }
        }

        // ===================== INDEX =====================
        public async Task<IActionResult> Index()
        {
            var data = await _context.Phim
                .Include(p => p.MaTheLoaiNavigation)
                .Where(p => !p.DaXoa)
                .ToListAsync();

            // Auto update trạng thái theo ngày
            foreach (var phim in data)
            {
                UpdateTrangThaiTuDong(phim);
            }

            await _context.SaveChangesAsync();

            return View(data);
        }

        // ===================== CREATE =====================
        public IActionResult Create()
        {
            var phim = new Phim
            {
                TrangThai = "SapChieu",
                DaXoa = false,
                NgayPhatHanh = DateOnly.FromDateTime(DateTime.Now)
            };

            ViewData["MaTheLoai"] = new SelectList(
                _context.TheLoaiPhim.Where(t => !t.DaXoa),
                "MaTheLoai", "TenTheLoai");

            return View(phim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Phim phim)
        {
            ModelState.Remove("MaTheLoaiNavigation");
            ModelState.Remove("SuatChieu");

            if (ModelState.IsValid)
            {
                _context.Add(phim);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thêm phim thành công";
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaTheLoai"] = new SelectList(
                _context.TheLoaiPhim.Where(t => !t.DaXoa),
                "MaTheLoai", "TenTheLoai", phim.MaTheLoai);

            return View(phim);
        }

        // ===================== EDIT =====================
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var phim = await _context.Phim.FindAsync(id);
            if (phim == null) return NotFound();

            ViewData["MaTheLoai"] = new SelectList(
                _context.TheLoaiPhim.Where(t => !t.DaXoa),
                "MaTheLoai", "TenTheLoai", phim.MaTheLoai);

            return View(phim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Phim phim)
        {
            if (id != phim.MaPhim) return NotFound();

            ModelState.Remove("MaTheLoaiNavigation");
            ModelState.Remove("SuatChieu");

            if (ModelState.IsValid)
            {
                var existing = await _context.Phim.FindAsync(id);
                if (existing == null) return NotFound();

                // ================= VALIDATE STATE =================
                bool isValid = false;

                if (existing.TrangThai == "SapChieu" &&
                    (phim.TrangThai == "DangChieu" || phim.TrangThai == "NgungChieu"))
                {
                    isValid = true;
                }
                else if (existing.TrangThai == "DangChieu" &&
                    phim.TrangThai == "NgungChieu")
                {
                    isValid = true;
                }
                else if (existing.TrangThai == "NgungChieu" &&
                    phim.TrangThai == "SapChieu")
                {
                    isValid = true;
                }

                if (!isValid)
                {
                    TempData["Error"] = "Chuyển trạng thái không hợp lệ!";
                    return RedirectToAction(nameof(Index));
                }

                // ================= UPDATE DATA =================
                existing.TenPhim = phim.TenPhim;
                existing.MaTheLoai = phim.MaTheLoai;
                existing.ThoiLuong = phim.ThoiLuong;
                existing.NgayPhatHanh = phim.NgayPhatHanh;
                existing.MoTa = phim.MoTa;
                existing.PhanLoaiDoTuoi = phim.PhanLoaiDoTuoi;
                existing.DuongDanAnh = phim.DuongDanAnh;
                existing.DuongDanTrailer = phim.DuongDanTrailer;
                existing.TrangThai = phim.TrangThai;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật thành công";
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaTheLoai"] = new SelectList(
                _context.TheLoaiPhim.Where(t => !t.DaXoa),
                "MaTheLoai", "TenTheLoai", phim.MaTheLoai);

            return View(phim);
        }

        // ===================== DELETE (SOFT) =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var phim = await _context.Phim
                .Include(p => p.SuatChieu)
                .FirstOrDefaultAsync(p => p.MaPhim == id);

            if (phim == null) return NotFound();

            if (phim.SuatChieu != null && phim.SuatChieu.Any())
            {
                TempData["Error"] = "Không thể xóa — phim đang có suất chiếu";
                return RedirectToAction(nameof(Index));
            }

            phim.DaXoa = true;

            // hủy phim -> ngừng chiếu
            phim.TrangThai = "NgungChieu";

            _context.Update(phim);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa thành công";
            return RedirectToAction(nameof(Index));
        }

        // ===================== DETAILS =====================
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var phim = await _context.Phim
                .Include(p => p.MaTheLoaiNavigation)
                .FirstOrDefaultAsync(p => p.MaPhim == id);

            if (phim == null) return NotFound();

            return View(phim);
        }

        // ===================== EXPORT EXCEL =====================
        public async Task<IActionResult> ExportExcel()
        {
            var data = await _context.Phim
                .Where(p => !p.DaXoa)
                .Include(p => p.MaTheLoaiNavigation)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Phim");

            worksheet.Cell(1, 1).Value = "Mã phim";
            worksheet.Cell(1, 2).Value = "Tên phim";
            worksheet.Cell(1, 3).Value = "Thể loại";
            worksheet.Cell(1, 4).Value = "Thời lượng";
            worksheet.Cell(1, 5).Value = "Trạng thái";

            int row = 2;
            foreach (var p in data)
            {
                worksheet.Cell(row, 1).Value = p.MaPhim;
                worksheet.Cell(row, 2).Value = p.TenPhim;
                worksheet.Cell(row, 3).Value = p.MaTheLoaiNavigation?.TenTheLoai;
                worksheet.Cell(row, 4).Value = p.ThoiLuong;
                worksheet.Cell(row, 5).Value = p.TrangThai;
                row++;
            }

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            TempData["Success"] = "Xuất Excel thành công";

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "DanhSachPhim.xlsx");
        }
    }
}