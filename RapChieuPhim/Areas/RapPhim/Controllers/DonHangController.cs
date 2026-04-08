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
    public class DonHangController : Controller
    {
        private readonly AppDbContext _context;

        public DonHangController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/DonHang
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.DonHang.Include(d => d.MaKhachHangNavigation).Include(d => d.MaKhuyenMaiNavigation).Include(d => d.MaNhanVienNavigation);
            return View(await appDbContext.ToListAsync());
        }

        // GET: RapPhim/DonHang/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donHang = await _context.DonHang
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaKhuyenMaiNavigation)
                .Include(d => d.MaNhanVienNavigation)
                .FirstOrDefaultAsync(m => m.MaDonHang == id);
            if (donHang == null)
            {
                return NotFound();
            }

            return View(donHang);
        }

        // GET: RapPhim/DonHang/Create
        public IActionResult Create()
        {
            ViewData["MaKhachHang"] = new SelectList(_context.KhachHang, "MaKhachHang", "MaKhachHang");
            ViewData["MaKhuyenMai"] = new SelectList(_context.KhuyenMai, "MaKhuyenMai", "MaKhuyenMai");
            ViewData["MaNhanVien"] = new SelectList(_context.NhanVien, "MaNhanVien", "MaNhanVien");
            return View();
        }

        // POST: RapPhim/DonHang/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaDonHang,MaKhachHang,MaNhanVien,MaKhuyenMai,NgayTao,TongTienBanDau,TongTienSauGiam,TrangThai,DaXoa")] DonHang donHang)
        {
            if (ModelState.IsValid)
            {
                _context.Add(donHang);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaKhachHang"] = new SelectList(_context.KhachHang, "MaKhachHang", "MaKhachHang", donHang.MaKhachHang);
            ViewData["MaKhuyenMai"] = new SelectList(_context.KhuyenMai, "MaKhuyenMai", "MaKhuyenMai", donHang.MaKhuyenMai);
            ViewData["MaNhanVien"] = new SelectList(_context.NhanVien, "MaNhanVien", "MaNhanVien", donHang.MaNhanVien);
            return View(donHang);
        }

        // GET: RapPhim/DonHang/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donHang = await _context.DonHang.FindAsync(id);
            if (donHang == null)
            {
                return NotFound();
            }
            ViewData["MaKhachHang"] = new SelectList(_context.KhachHang, "MaKhachHang", "MaKhachHang", donHang.MaKhachHang);
            ViewData["MaKhuyenMai"] = new SelectList(_context.KhuyenMai, "MaKhuyenMai", "MaKhuyenMai", donHang.MaKhuyenMai);
            ViewData["MaNhanVien"] = new SelectList(_context.NhanVien, "MaNhanVien", "MaNhanVien", donHang.MaNhanVien);
            return View(donHang);
        }

        // POST: RapPhim/DonHang/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaDonHang,MaKhachHang,MaNhanVien,MaKhuyenMai,NgayTao,TongTienBanDau,TongTienSauGiam,TrangThai,DaXoa")] DonHang donHang)
        {
            if (id != donHang.MaDonHang)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(donHang);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DonHangExists(donHang.MaDonHang))
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
            ViewData["MaKhachHang"] = new SelectList(_context.KhachHang, "MaKhachHang", "MaKhachHang", donHang.MaKhachHang);
            ViewData["MaKhuyenMai"] = new SelectList(_context.KhuyenMai, "MaKhuyenMai", "MaKhuyenMai", donHang.MaKhuyenMai);
            ViewData["MaNhanVien"] = new SelectList(_context.NhanVien, "MaNhanVien", "MaNhanVien", donHang.MaNhanVien);
            return View(donHang);
        }

        // GET: RapPhim/DonHang/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donHang = await _context.DonHang
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaKhuyenMaiNavigation)
                .Include(d => d.MaNhanVienNavigation)
                .FirstOrDefaultAsync(m => m.MaDonHang == id);
            if (donHang == null)
            {
                return NotFound();
            }

            return View(donHang);
        }

        // POST: RapPhim/DonHang/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var donHang = await _context.DonHang.FindAsync(id);
            if (donHang != null)
            {
                _context.DonHang.Remove(donHang);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DonHangExists(string id)
        {
            return _context.DonHang.Any(e => e.MaDonHang == id);
        }
    }
}
