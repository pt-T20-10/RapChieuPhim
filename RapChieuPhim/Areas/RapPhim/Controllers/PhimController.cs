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

        // GET: RapPhim/Phim
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Phim
                .Include(p => p.MaTheLoaiNavigation)
                .Where(p => !p.DaXoa);
            return View(await appDbContext.ToListAsync());
        }

        // GET: RapPhim/Phim/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phim
                .Include(p => p.MaTheLoaiNavigation)
                .FirstOrDefaultAsync(m => m.MaPhim == id);
            if (phim == null)
            {
                return NotFound();
            }

            return View(phim);
        }

        public IActionResult Create()
        {
            // Set giá trị mặc định trước khi trả về View
            var phim = new Phim
            {
                TrangThai = "SapChieu",
                DaXoa = false
            };

                ViewData["MaTheLoai"] = new SelectList(
                _context.TheLoaiPhim.Where(t => !t.DaXoa),
                "MaTheLoai", "TenTheLoai");

            return View(phim);   
        }

        // POST: RapPhim/Phim/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaPhim,TenPhim,MaTheLoai,ThoiLuong,NgayPhatHanh,MoTa,PhanLoaiDoTuoi,DuongDanAnh,TrangThai,DaXoa,DuongDanTrailer")] Phim phim)
        {
            // Xóa navigation properties khỏi ModelState
            // vì chúng không có trong form — EF tự load sau
            ModelState.Remove("MaTheLoaiNavigation");
            ModelState.Remove("SuatChieu");

            if (string.IsNullOrEmpty(phim.MaTheLoai))
                ModelState.AddModelError("MaTheLoai", "Vui lòng chọn thể loại phim");

            if (ModelState.IsValid)
            {
                _context.Add(phim);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaTheLoai"] = new SelectList(
                _context.TheLoaiPhim.Where(t => !t.DaXoa),
                "MaTheLoai", "TenTheLoai", phim.MaTheLoai);
            return View(phim);
        }

        // GET: RapPhim/Phim/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phim.FindAsync(id);
            if (phim == null)
            {
                return NotFound();
            }
            ViewData["MaTheLoai"] = new SelectList(_context.TheLoaiPhim, "MaTheLoai", "TenTheLoai", phim.MaTheLoai);
            return View(phim);
        }

        // POST: RapPhim/Phim/Edit/5
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
            ViewData["MaTheLoai"] = new SelectList(_context.TheLoaiPhim, "MaTheLoai", "TenTheLoai", phim.MaTheLoai);
            return View(phim);
        }

        // GET: RapPhim/Phim/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phim
                .Include(p => p.MaTheLoaiNavigation)
                .FirstOrDefaultAsync(m => m.MaPhim == id);
            if (phim == null)
            {
                return NotFound();
            }

            return View(phim);
        }

        // POST: RapPhim/Phim/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var phim = await _context.Phim.FindAsync(id);
            if (phim != null)
            {
                phim.DaXoa = true;             
                _context.Update(phim);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PhimExists(string id)
        {
            return _context.Phim.Any(e => e.MaPhim == id);
        }
    }
}
