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
    public class NhanVienController : Controller
    {
        private readonly AppDbContext _context;

        public NhanVienController(AppDbContext context)
        {
            _context = context;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index()
        {
            var data = await _context.NhanVien
                .Where(x => !x.DaXoa)
                .ToListAsync();

            if (!data.Any())
                TempData["Info"] = "Chưa có nhân viên nào trong hệ thống.";

            return View(data);
        }

        // ================= DETAILS =================
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Thiếu mã nhân viên!";
                return RedirectToAction(nameof(Index));
            }

            var nhanVien = await _context.NhanVien
                .FirstOrDefaultAsync(m => m.MaNhanVien == id && !m.DaXoa);

            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên!";
                return RedirectToAction(nameof(Index));
            }

            return View(nhanVien);
        }

        // ================= CREATE =================
        public IActionResult Create()
        {
            return View(new NhanVien
            {
                NgayVaoLam = DateOnly.FromDateTime(DateTime.Now),
                DaXoa = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien nhanVien)
        {
            // ===== VALIDATE =====
            if (string.IsNullOrWhiteSpace(nhanVien.MaNhanVien))
                ModelState.AddModelError("MaNhanVien", "Mã nhân viên không được để trống");

            if (string.IsNullOrWhiteSpace(nhanVien.HoTen))
                ModelState.AddModelError("HoTen", "Họ tên không được để trống");

            if (string.IsNullOrWhiteSpace(nhanVien.GioiTinh))
                ModelState.AddModelError("GioiTinh", "Vui lòng chọn giới tính");

            if (nhanVien.NgaySinh != null && nhanVien.NgaySinh > DateOnly.FromDateTime(DateTime.Now))
                ModelState.AddModelError("NgaySinh", "Ngày sinh không hợp lệ");

            if (nhanVien.NgaySinh != null &&
                DateOnly.FromDateTime(DateTime.Now).Year - nhanVien.NgaySinh.Value.Year < 18)
                ModelState.AddModelError("NgaySinh", "Nhân viên phải từ 18 tuổi");

            if (nhanVien.NgayVaoLam > DateOnly.FromDateTime(DateTime.Now))
                ModelState.AddModelError("NgayVaoLam", "Ngày vào làm không hợp lệ");

            if (nhanVien.NgaySinh != null && nhanVien.NgayVaoLam <= nhanVien.NgaySinh)
                ModelState.AddModelError("NgayVaoLam", "Ngày vào làm phải sau ngày sinh");

            if (string.IsNullOrWhiteSpace(nhanVien.CaLamViec))
                ModelState.AddModelError("CaLamViec", "Ca làm việc không được để trống");

            // Check trùng mã
            var exists = await _context.NhanVien
                .AnyAsync(x => x.MaNhanVien == nhanVien.MaNhanVien);

            if (exists)
                ModelState.AddModelError("MaNhanVien", "Mã nhân viên đã tồn tại");

            // ===== SAVE =====
            if (ModelState.IsValid)
            {
                try
                {
                    nhanVien.DaXoa = false;

                    _context.Add(nhanVien);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Thêm nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["Error"] = "Lỗi khi thêm nhân viên!";
                }
            }

            TempData["Error"] = "Vui lòng kiểm tra lại dữ liệu!";
            return View(nhanVien);
        }

        // ================= EDIT =================
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Thiếu mã nhân viên!";
                return RedirectToAction(nameof(Index));
            }

            var nhanVien = await _context.NhanVien.FindAsync(id);
            if (nhanVien == null || nhanVien.DaXoa)
            {
                TempData["Error"] = "Nhân viên không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            return View(nhanVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, NhanVien nhanVien)
        {
            if (id != nhanVien.MaNhanVien)
            {
                TempData["Error"] = "Sai mã nhân viên!";
                return RedirectToAction(nameof(Index));
            }

            // ===== VALIDATE =====
            if (string.IsNullOrWhiteSpace(nhanVien.HoTen))
                ModelState.AddModelError("HoTen", "Họ tên không được để trống");

            if (string.IsNullOrWhiteSpace(nhanVien.GioiTinh))
                ModelState.AddModelError("GioiTinh", "Vui lòng chọn giới tính");

            if (nhanVien.NgaySinh != null && nhanVien.NgaySinh > DateOnly.FromDateTime(DateTime.Now))
                ModelState.AddModelError("NgaySinh", "Ngày sinh không hợp lệ");

            if (nhanVien.NgaySinh != null &&
                DateOnly.FromDateTime(DateTime.Now).Year - nhanVien.NgaySinh.Value.Year < 18)
                ModelState.AddModelError("NgaySinh", "Nhân viên phải từ 18 tuổi");

            if (nhanVien.NgayVaoLam > DateOnly.FromDateTime(DateTime.Now))
                ModelState.AddModelError("NgayVaoLam", "Ngày vào làm không hợp lệ");

            if (nhanVien.NgaySinh != null && nhanVien.NgayVaoLam <= nhanVien.NgaySinh)
                ModelState.AddModelError("NgayVaoLam", "Ngày vào làm phải sau ngày sinh");

            if (string.IsNullOrWhiteSpace(nhanVien.CaLamViec))
                ModelState.AddModelError("CaLamViec", "Ca làm việc không được để trống");

            // ===== UPDATE =====
            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.NhanVien.FindAsync(id);
                    if (existing == null)
                    {
                        TempData["Error"] = "Nhân viên không tồn tại!";
                        return RedirectToAction(nameof(Index));
                    }

                    existing.HoTen = nhanVien.HoTen;
                    existing.NgaySinh = nhanVien.NgaySinh;
                    existing.GioiTinh = nhanVien.GioiTinh;
                    existing.DiaChi = nhanVien.DiaChi;
                    existing.NgayVaoLam = nhanVien.NgayVaoLam;
                    existing.CaLamViec = nhanVien.CaLamViec;

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["Error"] = "Lỗi khi cập nhật!";
                }
            }

            TempData["Error"] = "Vui lòng kiểm tra lại dữ liệu!";
            return View(nhanVien);
        }

        // ================= DELETE (SOFT DELETE) =================
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Thiếu mã nhân viên!";
                return RedirectToAction(nameof(Index));
            }

            var nhanVien = await _context.NhanVien
                .FirstOrDefaultAsync(m => m.MaNhanVien == id);

            if (nhanVien == null)
            {
                TempData["Error"] = "Nhân viên không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            var coDonHang = await _context.DonHang
                .AnyAsync(x => x.MaNhanVien == id);

            ViewBag.CoRangBuoc = coDonHang;

            return View(nhanVien);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var coDonHang = await _context.DonHang
                .AnyAsync(x => x.MaNhanVien == id);

            if (coDonHang)
            {
                TempData["Error"] = "Không thể xóa vì nhân viên đã có dữ liệu liên quan!";
                return RedirectToAction(nameof(Index));
            }

            var nhanVien = await _context.NhanVien.FindAsync(id);
            if (nhanVien != null)
            {
                nhanVien.DaXoa = true;

                _context.Update(nhanVien);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Xóa nhân viên thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy nhân viên!";
            }

            return RedirectToAction(nameof(Index));
        }

        // ================= EXPORT EXCEL =================
        public async Task<IActionResult> ExportExcel()
        {
            var data = await _context.NhanVien
                .Where(x => !x.DaXoa)
                .ToListAsync();

            if (!data.Any())
            {
                TempData["Error"] = "Không có dữ liệu để xuất!";
                return RedirectToAction(nameof(Index));
            }

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("NhanVien");

                ws.Cell(1, 1).Value = "Mã NV";
                ws.Cell(1, 2).Value = "Họ tên";
                ws.Cell(1, 3).Value = "Ngày sinh";
                ws.Cell(1, 4).Value = "Giới tính";
                ws.Cell(1, 5).Value = "Địa chỉ";
                ws.Cell(1, 6).Value = "Ngày vào làm";
                ws.Cell(1, 7).Value = "Ca làm";

                int row = 2;

                foreach (var item in data)
                {
                    ws.Cell(row, 1).Value = item.MaNhanVien;
                    ws.Cell(row, 2).Value = item.HoTen;
                    ws.Cell(row, 3).Value = item.NgaySinh?.ToString();
                    ws.Cell(row, 4).Value = item.GioiTinh;
                    ws.Cell(row, 5).Value = item.DiaChi;
                    ws.Cell(row, 6).Value = item.NgayVaoLam.ToString();
                    ws.Cell(row, 7).Value = item.CaLamViec;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    TempData["Success"] = "Xuất Excel thành công!";

                    return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "NhanVien.xlsx");
                }
            }
        }

        // ================= CHECK =================
        private bool NhanVienExists(string id)
        {
            return _context.NhanVien.Any(e => e.MaNhanVien == id);
        }
    }
}