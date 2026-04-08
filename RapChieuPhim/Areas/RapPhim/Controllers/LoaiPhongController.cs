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
    public class LoaiPhongController : Controller
    {
        private readonly AppDbContext _context;

        public LoaiPhongController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/LoaiPhong
        public async Task<IActionResult> Index()
        {
            return View(await _context.LoaiPhong.ToListAsync());
        }

        // GET: RapPhim/LoaiPhong/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _context.LoaiPhong
                .FirstOrDefaultAsync(m => m.MaLoaiPhong == id);
            if (loaiPhong == null)
            {
                return NotFound();
            }

            return View(loaiPhong);
        }

        // GET: RapPhim/LoaiPhong/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RapPhim/LoaiPhong/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaLoaiPhong,TenLoaiPhong,DaXoa")] LoaiPhong loaiPhong)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loaiPhong);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loaiPhong);
        }

        // GET: RapPhim/LoaiPhong/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _context.LoaiPhong.FindAsync(id);
            if (loaiPhong == null)
            {
                return NotFound();
            }
            return View(loaiPhong);
        }

        // POST: RapPhim/LoaiPhong/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaLoaiPhong,TenLoaiPhong,DaXoa")] LoaiPhong loaiPhong)
        {
            if (id != loaiPhong.MaLoaiPhong)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loaiPhong);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoaiPhongExists(loaiPhong.MaLoaiPhong))
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
            return View(loaiPhong);
        }

        // GET: RapPhim/LoaiPhong/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _context.LoaiPhong
                .FirstOrDefaultAsync(m => m.MaLoaiPhong == id);
            if (loaiPhong == null)
            {
                return NotFound();
            }

            return View(loaiPhong);
        }

        // POST: RapPhim/LoaiPhong/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var loaiPhong = await _context.LoaiPhong.FindAsync(id);
            if (loaiPhong != null)
            {
                _context.LoaiPhong.Remove(loaiPhong);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LoaiPhongExists(string id)
        {
            return _context.LoaiPhong.Any(e => e.MaLoaiPhong == id);
        }
    }
}
