using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Models.Entities;

namespace RapChieuPhim.Services
{
    public class DatVeService
    {
        private readonly AppDbContext _context;

        public DatVeService(AppDbContext context)
        {
            _context = context;
        }

      
        public async Task<List<Phim>> LayDanhSachPhimAsync()
        {
            return await _context.Phim
                .Include(p => p.MaTheLoaiNavigation) // Lấy thông tin Thể loại
                .Where(p => p.DaXoa == false && (p.TrangThai == "DangChieu" || p.TrangThai == "SapChieu"))
                .ToListAsync();
        }

        //truy xuất phim theo ID
        public async Task<Phim?> LayChiTietPhimAsync(string maPhim)
        {
            return await _context.Phim
                .Include(p => p.MaTheLoaiNavigation)
                .FirstOrDefaultAsync(p => p.MaPhim == maPhim && p.DaXoa == false);
        }
    }
}