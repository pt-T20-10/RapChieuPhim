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
    public class SuatChieuController : Controller
    {
        private readonly AppDbContext _context;

        public SuatChieuController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/SuatChieu
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.SuatChieus.Include(s => s.MaPhimNavigation).Include(s => s.MaPhongNavigation);
            return View(await appDbContext.ToListAsync());
        }

        // GET: RapPhim/SuatChieu/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var suatChieu = await _context.SuatChieus
                .Include(s => s.MaPhimNavigation)
                .Include(s => s.MaPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaSuatChieu == id);
            if (suatChieu == null)
            {
                return NotFound();
            }

            return View(suatChieu);
        }

        // GET: RapPhim/SuatChieu/Create
        public IActionResult Create()
        {
            ViewData["MaPhim"] = new SelectList(_context.Phims, "MaPhim", "MaPhim");
            ViewData["MaPhong"] = new SelectList(_context.PhongChieus, "MaPhong", "MaPhong");
            return View();
        }

        // POST: RapPhim/SuatChieu/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaSuatChieu,MaPhim,MaPhong,ThoiGianBatDau,ThoiGianKetThuc,GiaGoc,TrangThai,DaXoa")] SuatChieu suatChieu)
        {
            if (ModelState.IsValid)
            {
                _context.Add(suatChieu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaPhim"] = new SelectList(_context.Phims, "MaPhim", "MaPhim", suatChieu.MaPhim);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieus, "MaPhong", "MaPhong", suatChieu.MaPhong);
            return View(suatChieu);
        }

        // GET: RapPhim/SuatChieu/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var suatChieu = await _context.SuatChieus.FindAsync(id);
            if (suatChieu == null)
            {
                return NotFound();
            }
            ViewData["MaPhim"] = new SelectList(_context.Phims, "MaPhim", "MaPhim", suatChieu.MaPhim);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieus, "MaPhong", "MaPhong", suatChieu.MaPhong);
            return View(suatChieu);
        }

        // POST: RapPhim/SuatChieu/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaSuatChieu,MaPhim,MaPhong,ThoiGianBatDau,ThoiGianKetThuc,GiaGoc,TrangThai,DaXoa")] SuatChieu suatChieu)
        {
            if (id != suatChieu.MaSuatChieu)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(suatChieu);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SuatChieuExists(suatChieu.MaSuatChieu))
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
            ViewData["MaPhim"] = new SelectList(_context.Phims, "MaPhim", "MaPhim", suatChieu.MaPhim);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieus, "MaPhong", "MaPhong", suatChieu.MaPhong);
            return View(suatChieu);
        }

        // GET: RapPhim/SuatChieu/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var suatChieu = await _context.SuatChieus
                .Include(s => s.MaPhimNavigation)
                .Include(s => s.MaPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaSuatChieu == id);
            if (suatChieu == null)
            {
                return NotFound();
            }

            return View(suatChieu);
        }

        // POST: RapPhim/SuatChieu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var suatChieu = await _context.SuatChieus.FindAsync(id);
            if (suatChieu != null)
            {
                _context.SuatChieus.Remove(suatChieu);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SuatChieuExists(string id)
        {
            return _context.SuatChieus.Any(e => e.MaSuatChieu == id);
        }
    }
}
