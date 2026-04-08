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
    public class LoaiGheController : Controller
    {
        private readonly AppDbContext _context;

        public LoaiGheController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/LoaiGhe
        public async Task<IActionResult> Index()
        {
            return View(await _context.LoaiGhe.ToListAsync());
        }

        // GET: RapPhim/LoaiGhe/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiGhe = await _context.LoaiGhe
                .FirstOrDefaultAsync(m => m.MaLoaiGhe == id);
            if (loaiGhe == null)
            {
                return NotFound();
            }

            return View(loaiGhe);
        }

        // GET: RapPhim/LoaiGhe/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RapPhim/LoaiGhe/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaLoaiGhe,TenLoaiGhe,HeSoGia,DaXoa")] LoaiGhe loaiGhe)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loaiGhe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loaiGhe);
        }

        // GET: RapPhim/LoaiGhe/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiGhe = await _context.LoaiGhe.FindAsync(id);
            if (loaiGhe == null)
            {
                return NotFound();
            }
            return View(loaiGhe);
        }

        // POST: RapPhim/LoaiGhe/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaLoaiGhe,TenLoaiGhe,HeSoGia,DaXoa")] LoaiGhe loaiGhe)
        {
            if (id != loaiGhe.MaLoaiGhe)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loaiGhe);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoaiGheExists(loaiGhe.MaLoaiGhe))
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
            return View(loaiGhe);
        }

        // GET: RapPhim/LoaiGhe/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiGhe = await _context.LoaiGhe
                .FirstOrDefaultAsync(m => m.MaLoaiGhe == id);
            if (loaiGhe == null)
            {
                return NotFound();
            }

            return View(loaiGhe);
        }

        // POST: RapPhim/LoaiGhe/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var loaiGhe = await _context.LoaiGhe.FindAsync(id);
            if (loaiGhe != null)
            {
                _context.LoaiGhe.Remove(loaiGhe);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LoaiGheExists(string id)
        {
            return _context.LoaiGhe.Any(e => e.MaLoaiGhe == id);
        }
    }
}
