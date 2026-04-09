using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Models.Entities;
using ClosedXML.Excel;
using System.IO;

namespace RapChieuPhim.Areas.RapPhim.Controllers
{
    [Area("RapPhim")]
    public class KhuyenMaiController : Controller
    {
        private readonly AppDbContext _context;

        public KhuyenMaiController(AppDbContext context)
        {
            _context = context;
        }

        // ================= AUTO UPDATE =================
        private async Task AutoUpdateTrangThai()
        {
            var now = DateTime.Now;

            var list = await _context.KhuyenMai
                .Where(x => !x.DaXoa)
                .ToListAsync();

            foreach (var km in list)
            {
                if (km.SoLuongConLai <= 0 || km.DenNgay <= now)
                    km.TrangThai = "DaKetThuc";
                else if (km.TuNgay <= now && km.DenNgay > now)
                    km.TrangThai = "DangApDung";
                else
                    km.TrangThai = "ChoKichHoat";
            }

            await _context.SaveChangesAsync();
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index()
        {
            await AutoUpdateTrangThai();

            var data = await _context.KhuyenMai
                .Where(x => !x.DaXoa)
                .ToListAsync();

            return View(data);
        }

        // ================= DETAILS =================
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Không tìm thấy mã khuyến mãi!";
                return RedirectToAction(nameof(Index));
            }

            var km = await _context.KhuyenMai
                .FirstOrDefaultAsync(m => m.MaKhuyenMai == id && !m.DaXoa);

            if (km == null)
            {
                TempData["Error"] = "Khuyến mãi không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            return View(km);
        }

        // ================= CREATE =================
        public IActionResult Create()
        {
            return View(new KhuyenMai
            {
                TrangThai = "ChoKichHoat",
                DaXoa = false,
                SoLuongConLai = 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhuyenMai km)
        {
            // 🔥 FIX QUAN TRỌNG (giống PhimController)
            ModelState.Remove("DonHang");

            // ===== VALIDATE =====
            if (string.IsNullOrWhiteSpace(km.MaKhuyenMai))
                ModelState.AddModelError("MaKhuyenMai", "Mã khuyến mãi không được để trống");

            if (string.IsNullOrWhiteSpace(km.MaCode))
                ModelState.AddModelError("MaCode", "Mã code không được để trống");

            if (km.PhanTramGiam < 0 || km.PhanTramGiam > 100)
                ModelState.AddModelError("PhanTramGiam", "Phần trăm phải từ 0 - 100");

            if (km.DenNgay <= km.TuNgay)
                ModelState.AddModelError("DenNgay", "Ngày kết thúc phải sau ngày bắt đầu");

            if (km.SoLuongConLai < 0)
                ModelState.AddModelError("SoLuongConLai", "Số lượng phải >= 0");

            if (km.DonToiThieu < 0)
                ModelState.AddModelError("DonToiThieu", "Đơn tối thiểu phải >= 0");

            bool trungCode = await _context.KhuyenMai
                .AnyAsync(x => x.MaCode == km.MaCode);

            if (trungCode)
                ModelState.AddModelError("MaCode", "Mã code đã tồn tại");

            bool trungMa = await _context.KhuyenMai
                .AnyAsync(x => x.MaKhuyenMai == km.MaKhuyenMai);

            if (trungMa)
                ModelState.AddModelError("MaKhuyenMai", "Mã khuyến mãi đã tồn tại");

            // ===== SAVE =====
            if (ModelState.IsValid)
            {
                km.TrangThai = "ChoKichHoat";
                km.DaXoa = false;

                _context.Add(km);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thêm khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Thêm thất bại! Vui lòng kiểm tra lại dữ liệu.";
            return View(km);
        }

        // ================= EDIT =================
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Không tìm thấy khuyến mãi!";
                return RedirectToAction(nameof(Index));
            }

            var km = await _context.KhuyenMai.FindAsync(id);

            if (km == null || km.DaXoa)
            {
                TempData["Error"] = "Khuyến mãi không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            return View(km);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, KhuyenMai km)
        {
            if (id != km.MaKhuyenMai)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
                return RedirectToAction(nameof(Index));
            }

            // 🔥 FIX QUAN TRỌNG
            ModelState.Remove("DonHang");

            // ===== VALIDATE =====
            if (string.IsNullOrWhiteSpace(km.MaCode))
                ModelState.AddModelError("MaCode", "Mã code không được để trống");

            if (km.PhanTramGiam < 0 || km.PhanTramGiam > 100)
                ModelState.AddModelError("PhanTramGiam", "Phần trăm phải từ 0 - 100");

            if (km.DenNgay <= km.TuNgay)
                ModelState.AddModelError("DenNgay", "Ngày kết thúc phải sau ngày bắt đầu");

            if (km.SoLuongConLai < 0)
                ModelState.AddModelError("SoLuongConLai", "Số lượng phải >= 0");

            if (km.DonToiThieu < 0)
                ModelState.AddModelError("DonToiThieu", "Đơn tối thiểu phải >= 0");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(km);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật khuyến mãi thành công!";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Lỗi khi cập nhật!";
                    return View(km);
                }

                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Cập nhật thất bại!";
            return View(km);
        }

        // ================= DELETE (SOFT) =================
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var km = await _context.KhuyenMai
                .FirstOrDefaultAsync(m => m.MaKhuyenMai == id);

            if (km == null) return NotFound();

            bool coDon = await _context.DonHang
                .AnyAsync(d => d.MaKhuyenMai == id);

            ViewBag.CoDon = coDon;

            return View(km);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var km = await _context.KhuyenMai.FindAsync(id);

            bool coDon = await _context.DonHang
                .AnyAsync(d => d.MaKhuyenMai == id);

            if (coDon)
            {
                TempData["Error"] = "Không thể xóa vì đã có đơn hàng sử dụng!";
                return RedirectToAction(nameof(Index));
            }

            if (km != null)
            {
                km.DaXoa = true;
                km.TrangThai = "DaKetThuc";

                _context.Update(km);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Xóa thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // ================= EXPORT =================
        public async Task<IActionResult> ExportExcel()
        {
            var data = await _context.KhuyenMai
                .Where(x => !x.DaXoa)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("KhuyenMai");

                ws.Cell(1, 1).Value = "Mã";
                ws.Cell(1, 2).Value = "Code";
                ws.Cell(1, 3).Value = "% Giảm";
                ws.Cell(1, 4).Value = "Giảm tối đa";
                ws.Cell(1, 5).Value = "Từ ngày";
                ws.Cell(1, 6).Value = "Đến ngày";
                ws.Cell(1, 7).Value = "Số lượng";
                ws.Cell(1, 8).Value = "Đơn tối thiểu";
                ws.Cell(1, 9).Value = "Trạng thái";

                int row = 2;

                foreach (var item in data)
                {
                    ws.Cell(row, 1).Value = item.MaKhuyenMai;
                    ws.Cell(row, 2).Value = item.MaCode;
                    ws.Cell(row, 3).Value = item.PhanTramGiam;
                    ws.Cell(row, 4).Value = item.GiamToiDa;
                    ws.Cell(row, 5).Value = item.TuNgay;
                    ws.Cell(row, 6).Value = item.DenNgay;
                    ws.Cell(row, 7).Value = item.SoLuongConLai;
                    ws.Cell(row, 8).Value = item.DonToiThieu;
                    ws.Cell(row, 9).Value = item.TrangThai;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    TempData["Success"] = "Xuất Excel thành công!";

                    return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "KhuyenMai.xlsx");
                }
            }
        }

        private bool KhuyenMaiExists(string id)
        {
            return _context.KhuyenMai.Any(e => e.MaKhuyenMai == id);
        }
    }
}