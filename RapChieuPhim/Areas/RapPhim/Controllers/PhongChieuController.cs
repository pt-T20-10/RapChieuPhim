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
    public class PhongChieuController : Controller
    {
        private readonly AppDbContext _context;

        public PhongChieuController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/PhongChieu
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.PhongChieus.Include(p => p.MaLoaiPhongNavigation);
            return View(await appDbContext.ToListAsync());
        }

        // GET: RapPhim/PhongChieu/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phongChieu = await _context.PhongChieus
                .Include(p => p.MaLoaiPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaPhong == id);
            if (phongChieu == null)
            {
                return NotFound();
            }

            return View(phongChieu);
        }

        // GET: RapPhim/PhongChieu/Create
        public IActionResult Create()
        {
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "MaLoaiPhong");
            return View();
        }

        // POST: RapPhim/PhongChieu/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaPhong,TenPhong,MaLoaiPhong,SoGhe,TrangThai,DaXoa")] PhongChieu phongChieu)
        {
            if (ModelState.IsValid)
            {
                _context.Add(phongChieu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "MaLoaiPhong", phongChieu.MaLoaiPhong);
            return View(phongChieu);
        }

        // GET: RapPhim/PhongChieu/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phongChieu = await _context.PhongChieus.FindAsync(id);
            if (phongChieu == null)
            {
                return NotFound();
            }
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "MaLoaiPhong", phongChieu.MaLoaiPhong);
            return View(phongChieu);
        }

        // POST: RapPhim/PhongChieu/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaPhong,TenPhong,MaLoaiPhong,SoGhe,TrangThai,DaXoa")] PhongChieu phongChieu)
        {
            if (id != phongChieu.MaPhong)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phongChieu);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhongChieuExists(phongChieu.MaPhong))
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
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "MaLoaiPhong", phongChieu.MaLoaiPhong);
            return View(phongChieu);
        }

        // GET: RapPhim/PhongChieu/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phongChieu = await _context.PhongChieus
                .Include(p => p.MaLoaiPhongNavigation)
                .FirstOrDefaultAsync(m => m.MaPhong == id);
            if (phongChieu == null)
            {
                return NotFound();
            }

            return View(phongChieu);
        }

        // POST: RapPhim/PhongChieu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var phongChieu = await _context.PhongChieus.FindAsync(id);
            if (phongChieu != null)
            {
                _context.PhongChieus.Remove(phongChieu);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PhongChieuExists(string id)
        {
            return _context.PhongChieus.Any(e => e.MaPhong == id);
        }
    }
}
