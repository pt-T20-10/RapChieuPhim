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
    public class PhimController : Controller
    {
        private readonly AppDbContext _context;

        public PhimController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/Phims
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Phims.Include(p => p.MaTheLoaiNavigation);
            return View(await appDbContext.ToListAsync());
        }

        // GET: RapPhim/Phims/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims
                .Include(p => p.MaTheLoaiNavigation)
                .FirstOrDefaultAsync(m => m.MaPhim == id);
            if (phim == null)
            {
                return NotFound();
            }

            return View(phim);
        }

        // GET: RapPhim/Phims/Create
        public IActionResult Create()
        {
            ViewData["MaTheLoai"] = new SelectList(_context.TheLoaiPhims, "MaTheLoai", "MaTheLoai");
            return View();
        }

        // POST: RapPhim/Phims/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaPhim,TenPhim,MaTheLoai,ThoiLuong,NgayPhatHanh,MoTa,PhanLoaiDoTuoi,DuongDanAnh,TrangThai,DaXoa,DuongDanTrailer")] Phim phim)
        {
            if (ModelState.IsValid)
            {
                _context.Add(phim);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaTheLoai"] = new SelectList(_context.TheLoaiPhims, "MaTheLoai", "MaTheLoai", phim.MaTheLoai);
            return View(phim);
        }

        // GET: RapPhim/Phims/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims.FindAsync(id);
            if (phim == null)
            {
                return NotFound();
            }
            ViewData["MaTheLoai"] = new SelectList(_context.TheLoaiPhims, "MaTheLoai", "MaTheLoai", phim.MaTheLoai);
            return View(phim);
        }

        // POST: RapPhim/Phims/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaPhim,TenPhim,MaTheLoai,ThoiLuong,NgayPhatHanh,MoTa,PhanLoaiDoTuoi,DuongDanAnh,TrangThai,DaXoa,DuongDanTrailer")] Phim phim)
        {
            if (id != phim.MaPhim)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phim);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhimExists(phim.MaPhim))
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
            ViewData["MaTheLoai"] = new SelectList(_context.TheLoaiPhims, "MaTheLoai", "MaTheLoai", phim.MaTheLoai);
            return View(phim);
        }

        // GET: RapPhim/Phims/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims
                .Include(p => p.MaTheLoaiNavigation)
                .FirstOrDefaultAsync(m => m.MaPhim == id);
            if (phim == null)
            {
                return NotFound();
            }

            return View(phim);
        }

        // POST: RapPhim/Phims/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var phim = await _context.Phims.FindAsync(id);
            if (phim != null)
            {
                _context.Phims.Remove(phim);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PhimExists(string id)
        {
            return _context.Phims.Any(e => e.MaPhim == id);
        }
    }
}
