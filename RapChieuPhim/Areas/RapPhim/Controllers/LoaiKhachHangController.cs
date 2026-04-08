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
    public class LoaiKhachHangController : Controller
    {
        private readonly AppDbContext _context;

        public LoaiKhachHangController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RapPhim/LoaiKhachHang
        public async Task<IActionResult> Index()
        {
            return View(await _context.LoaiKhachHang.ToListAsync());
        }

        // GET: RapPhim/LoaiKhachHang/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiKhachHang = await _context.LoaiKhachHang
                .FirstOrDefaultAsync(m => m.MaLoaiKh == id);
            if (loaiKhachHang == null)
            {
                return NotFound();
            }

            return View(loaiKhachHang);
        }

        // GET: RapPhim/LoaiKhachHang/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RapPhim/LoaiKhachHang/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaLoaiKh,TenLoaiKh,NguongDiem,PhanTramGiamGia,DaXoa")] LoaiKhachHang loaiKhachHang)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loaiKhachHang);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loaiKhachHang);
        }

        // GET: RapPhim/LoaiKhachHang/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiKhachHang = await _context.LoaiKhachHang.FindAsync(id);
            if (loaiKhachHang == null)
            {
                return NotFound();
            }
            return View(loaiKhachHang);
        }

        // POST: RapPhim/LoaiKhachHang/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaLoaiKh,TenLoaiKh,NguongDiem,PhanTramGiamGia,DaXoa")] LoaiKhachHang loaiKhachHang)
        {
            if (id != loaiKhachHang.MaLoaiKh)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loaiKhachHang);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoaiKhachHangExists(loaiKhachHang.MaLoaiKh))
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
            return View(loaiKhachHang);
        }

        // GET: RapPhim/LoaiKhachHang/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiKhachHang = await _context.LoaiKhachHang
                .FirstOrDefaultAsync(m => m.MaLoaiKh == id);
            if (loaiKhachHang == null)
            {
                return NotFound();
            }

            return View(loaiKhachHang);
        }

        // POST: RapPhim/LoaiKhachHang/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var loaiKhachHang = await _context.LoaiKhachHang.FindAsync(id);
            if (loaiKhachHang != null)
            {
                _context.LoaiKhachHang.Remove(loaiKhachHang);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LoaiKhachHangExists(string id)
        {
            return _context.LoaiKhachHang.Any(e => e.MaLoaiKh == id);
        }
    }
}
