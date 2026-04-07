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
    public class DichVuController : Controller
    {
        private readonly AppDbContext _context;

        public DichVuController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/DichVus
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.DichVus.Include(d => d.MaDanhMucNavigation);
            return View(await appDbContext.ToListAsync());
        }

        // GET: RapPhim/DichVus/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dichVu = await _context.DichVus
                .Include(d => d.MaDanhMucNavigation)
                .FirstOrDefaultAsync(m => m.MaDichVu == id);
            if (dichVu == null)
            {
                return NotFound();
            }

            return View(dichVu);
        }

        // GET: RapPhim/DichVus/Create
        public IActionResult Create()
        {
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucDichVus, "MaDanhMuc", "MaDanhMuc");
            return View();
        }

        // POST: RapPhim/DichVus/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaDichVu,TenDichVu,MaDanhMuc,GiaBan,SoLuongTon,DaXoa,DuongDanHinh")] DichVu dichVu)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dichVu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucDichVus, "MaDanhMuc", "MaDanhMuc", dichVu.MaDanhMuc);
            return View(dichVu);
        }

        // GET: RapPhim/DichVus/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null)
            {
                return NotFound();
            }
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucDichVus, "MaDanhMuc", "MaDanhMuc", dichVu.MaDanhMuc);
            return View(dichVu);
        }

        // POST: RapPhim/DichVus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaDichVu,TenDichVu,MaDanhMuc,GiaBan,SoLuongTon,DaXoa,DuongDanHinh")] DichVu dichVu)
        {
            if (id != dichVu.MaDichVu)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dichVu);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DichVuExists(dichVu.MaDichVu))
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
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucDichVus, "MaDanhMuc", "MaDanhMuc", dichVu.MaDanhMuc);
            return View(dichVu);
        }

        // GET: RapPhim/DichVus/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dichVu = await _context.DichVus
                .Include(d => d.MaDanhMucNavigation)
                .FirstOrDefaultAsync(m => m.MaDichVu == id);
            if (dichVu == null)
            {
                return NotFound();
            }

            return View(dichVu);
        }

        // POST: RapPhim/DichVus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu != null)
            {
                _context.DichVus.Remove(dichVu);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DichVuExists(string id)
        {
            return _context.DichVus.Any(e => e.MaDichVu == id);
        }
    }
}
