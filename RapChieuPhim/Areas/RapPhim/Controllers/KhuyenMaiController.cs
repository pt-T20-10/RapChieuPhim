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
    public class KhuyenMaiController : Controller
    {
        private readonly AppDbContext _context;

        public KhuyenMaiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/KhuyenMai
        public async Task<IActionResult> Index()
        {
            return View(await _context.KhuyenMai.ToListAsync());
        }

        // GET: RapPhim/KhuyenMai/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khuyenMai = await _context.KhuyenMai
                .FirstOrDefaultAsync(m => m.MaKhuyenMai == id);
            if (khuyenMai == null)
            {
                return NotFound();
            }

            return View(khuyenMai);
        }

        // GET: RapPhim/KhuyenMai/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RapPhim/KhuyenMai/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaKhuyenMai,MaCode,PhanTramGiam,GiamToiDa,TuNgay,DenNgay,SoLuongConLai,DonToiThieu,TrangThai,DaXoa")] KhuyenMai khuyenMai)
        {
            if (ModelState.IsValid)
            {
                _context.Add(khuyenMai);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(khuyenMai);
        }

        // GET: RapPhim/KhuyenMai/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khuyenMai = await _context.KhuyenMai.FindAsync(id);
            if (khuyenMai == null)
            {
                return NotFound();
            }
            return View(khuyenMai);
        }

        // POST: RapPhim/KhuyenMai/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaKhuyenMai,MaCode,PhanTramGiam,GiamToiDa,TuNgay,DenNgay,SoLuongConLai,DonToiThieu,TrangThai,DaXoa")] KhuyenMai khuyenMai)
        {
            if (id != khuyenMai.MaKhuyenMai)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(khuyenMai);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KhuyenMaiExists(khuyenMai.MaKhuyenMai))
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
            return View(khuyenMai);
        }

        // GET: RapPhim/KhuyenMai/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khuyenMai = await _context.KhuyenMai
                .FirstOrDefaultAsync(m => m.MaKhuyenMai == id);
            if (khuyenMai == null)
            {
                return NotFound();
            }

            return View(khuyenMai);
        }

        // POST: RapPhim/KhuyenMai/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var khuyenMai = await _context.KhuyenMai.FindAsync(id);
            if (khuyenMai != null)
            {
                _context.KhuyenMai.Remove(khuyenMai);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KhuyenMaiExists(string id)
        {
            return _context.KhuyenMai.Any(e => e.MaKhuyenMai == id);
        }
    }
}
