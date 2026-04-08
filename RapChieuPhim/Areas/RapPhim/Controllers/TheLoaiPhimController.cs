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
    public class TheLoaiPhimController : Controller
    {
        private readonly AppDbContext _context;

        public TheLoaiPhimController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/TheLoaiPhim
        public async Task<IActionResult> Index()
        {
            return View(await _context.TheLoaiPhim.ToListAsync());
        }

        // GET: RapPhim/TheLoaiPhim/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theLoaiPhim = await _context.TheLoaiPhim
                .FirstOrDefaultAsync(m => m.MaTheLoai == id);
            if (theLoaiPhim == null)
            {
                return NotFound();
            }

            return View(theLoaiPhim);
        }

        // GET: RapPhim/TheLoaiPhim/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RapPhim/TheLoaiPhim/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaTheLoai,TenTheLoai,DaXoa")] TheLoaiPhim theLoaiPhim)
        {
            if (ModelState.IsValid)
            {
                _context.Add(theLoaiPhim);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(theLoaiPhim);
        }

        // GET: RapPhim/TheLoaiPhim/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theLoaiPhim = await _context.TheLoaiPhim.FindAsync(id);
            if (theLoaiPhim == null)
            {
                return NotFound();
            }
            return View(theLoaiPhim);
        }

        // POST: RapPhim/TheLoaiPhim/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaTheLoai,TenTheLoai,DaXoa")] TheLoaiPhim theLoaiPhim)
        {
            if (id != theLoaiPhim.MaTheLoai)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(theLoaiPhim);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TheLoaiPhimExists(theLoaiPhim.MaTheLoai))
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
            return View(theLoaiPhim);
        }

        // GET: RapPhim/TheLoaiPhim/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theLoaiPhim = await _context.TheLoaiPhim
                .FirstOrDefaultAsync(m => m.MaTheLoai == id);
            if (theLoaiPhim == null)
            {
                return NotFound();
            }

            return View(theLoaiPhim);
        }

        // POST: RapPhim/TheLoaiPhim/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var theLoaiPhim = await _context.TheLoaiPhim.FindAsync(id);
            if (theLoaiPhim != null)
            {
                _context.TheLoaiPhim.Remove(theLoaiPhim);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TheLoaiPhimExists(string id)
        {
            return _context.TheLoaiPhim.Any(e => e.MaTheLoai == id);
        }
    }
}
