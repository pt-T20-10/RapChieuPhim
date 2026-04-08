using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Models.Entities;

namespace RapChieuPhim.Services
{
    public class DichVuervice
    {
        private readonly AppDbContext _context;

        public DichVuervice(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DanhMucDichVu>> LayDanhSachMenuAsync()
        {
            // Lấy danh mục, kèm theo danh sách dịch vụ của danh mục đó (bỏ qua những cái đã xóa)
            return await _context.DanhMucDichVu
                .Include(dm => dm.DichVu.Where(dv => dv.DaXoa == false))
                .Where(dm => dm.DaXoa == false && dm.DichVu.Any(dv => dv.DaXoa == false))
                .ToListAsync();
        }
    }
}