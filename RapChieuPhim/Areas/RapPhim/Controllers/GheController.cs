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
            var appDbContext = _context.Ghe.Include(g => g.MaLoaiGheNavigation).Include(g => g.MaPhongNavigation);
            return View(await appDbContext.ToListAsync());
        }

        // GET: RapPhim/Ghe/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ghe = await _context.Ghe
                .Include(g => g.MaLoaiGheNavigation)
                .Include(g => g.MaPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaGhe == id);
            if (ghe == null)
            {
                return NotFound();
            }

            return View(ghe);
        }

        // GET: RapPhim/Ghe/Create
        public IActionResult Create()
        {
            ViewData["MaLoaiGhe"] = new SelectList(_context.LoaiGhe, "MaLoaiGhe", "MaLoaiGhe");
            ViewData["MaPhong"] = new SelectList(_context.PhongChieu, "MaPhong", "MaPhong");
            return View();
        }

        // POST: RapPhim/Ghe/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaGhe,MaPhong,TenHang,SoThu,MaLoaiGhe,TrangThai,DaXoa")] Ghe ghe)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ghe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaLoaiGhe"] = new SelectList(_context.LoaiGhe, "MaLoaiGhe", "MaLoaiGhe", ghe.MaLoaiGhe);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieu, "MaPhong", "MaPhong", ghe.MaPhong);
            return View(ghe);
        }

        // GET: RapPhim/Ghe/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ghe = await _context.Ghe.FindAsync(id);
            if (ghe == null)
            {
                return NotFound();
            }
            ViewData["MaLoaiGhe"] = new SelectList(_context.LoaiGhe, "MaLoaiGhe", "MaLoaiGhe", ghe.MaLoaiGhe);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieu, "MaPhong", "MaPhong", ghe.MaPhong);
            return View(ghe);
        }

        // POST: RapPhim/Ghe/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaGhe,MaPhong,TenHang,SoThu,MaLoaiGhe,TrangThai,DaXoa")] Ghe ghe)
        {
            if (id != ghe.MaGhe)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ghe);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GheExists(ghe.MaGhe))
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
            ViewData["MaLoaiGhe"] = new SelectList(_context.LoaiGhe, "MaLoaiGhe", "MaLoaiGhe", ghe.MaLoaiGhe);
            ViewData["MaPhong"] = new SelectList(_context.PhongChieu, "MaPhong", "MaPhong", ghe.MaPhong);
            return View(ghe);
        }

        // GET: RapPhim/Ghe/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ghe = await _context.Ghe
                .Include(g => g.MaLoaiGheNavigation)
                .Include(g => g.MaPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaGhe == id);
            if (ghe == null)
            {
                return NotFound();
            }

            return View(ghe);
        }

        // POST: RapPhim/Ghe/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var ghe = await _context.Ghe.FindAsync(id);
            if (ghe != null)
            {
                _context.Ghe.Remove(ghe);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GheExists(string id)
        {
            return _context.Ghe.Any(e => e.MaGhe == id);
        }
    }
}
