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
    public class TaiKhoanController : Controller
    {
        private readonly AppDbContext _context;

        public TaiKhoanController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/TaiKhoan
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.TaiKhoan.Include(t => t.MaKhachHangNavigation).Include(t => t.MaNhanVienNavigation);
            return View(await appDbContext.ToListAsync());
        }

        // GET: RapPhim/TaiKhoan/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoan
                .Include(t => t.MaKhachHangNavigation)
                .Include(t => t.MaNhanVienNavigation)
                .FirstOrDefaultAsync(m => m.TenDangNhap == id);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            return View(taiKhoan);
        }

        // GET: RapPhim/TaiKhoan/Create
        public IActionResult Create()
        {
            ViewData["MaKhachHang"] = new SelectList(_context.KhachHang, "MaKhachHang", "MaKhachHang");
            ViewData["MaNhanVien"] = new SelectList(_context.NhanVien, "MaNhanVien", "MaNhanVien");
            return View();
        }

        // POST: RapPhim/TaiKhoan/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenDangNhap,MatKhau,VaiTro,TrangThai,MaKhachHang,MaNhanVien,DaXoa")] TaiKhoan taiKhoan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(taiKhoan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaKhachHang"] = new SelectList(_context.KhachHang, "MaKhachHang", "MaKhachHang", taiKhoan.MaKhachHang);
            ViewData["MaNhanVien"] = new SelectList(_context.NhanVien, "MaNhanVien", "MaNhanVien", taiKhoan.MaNhanVien);
            return View(taiKhoan);
        }

        // GET: RapPhim/TaiKhoan/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoan.FindAsync(id);
            if (taiKhoan == null)
            {
                return NotFound();
            }
            ViewData["MaKhachHang"] = new SelectList(_context.KhachHang, "MaKhachHang", "MaKhachHang", taiKhoan.MaKhachHang);
            ViewData["MaNhanVien"] = new SelectList(_context.NhanVien, "MaNhanVien", "MaNhanVien", taiKhoan.MaNhanVien);
            return View(taiKhoan);
        }

        // POST: RapPhim/TaiKhoan/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("TenDangNhap,MatKhau,VaiTro,TrangThai,MaKhachHang,MaNhanVien,DaXoa")] TaiKhoan taiKhoan)
        {
            if (id != taiKhoan.TenDangNhap)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taiKhoan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaiKhoanExists(taiKhoan.TenDangNhap))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaKhachHang"] = new SelectList(_context.KhachHang, "MaKhachHang", "MaKhachHang", taiKhoan.MaKhachHang);
            ViewData["MaNhanVien"] = new SelectList(_context.NhanVien, "MaNhanVien", "MaNhanVien", taiKhoan.MaNhanVien);
            return View(taiKhoan);
        }

        // GET: RapPhim/TaiKhoan/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoan
                .Include(t => t.MaKhachHangNavigation)
                .Include(t => t.MaNhanVienNavigation)
                .FirstOrDefaultAsync(m => m.TenDangNhap == id);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            return View(taiKhoan);
        }

        // POST: RapPhim/TaiKhoan/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var taiKhoan = await _context.TaiKhoan.FindAsync(id);
            if (taiKhoan != null)
            {
                _context.TaiKhoan.Remove(taiKhoan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TaiKhoanExists(string id)
        {
            return _context.TaiKhoan.Any(e => e.TenDangNhap == id);
        }
    }
}
