using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RapChieuPhim.Data;
using RapChieuPhim.Models.Entities;
using RapChieuPhim.Models.Results;

namespace RapChieuPhim.Services;

public class QuetVeService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<QuetVeService> _logger;

    public QuetVeService(AppDbContext dbContext, ILogger<QuetVeService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ✅ SIMPLIFIED: Chỉ cần quét MaQr -> Lấy MaDonHang -> Hiển thị thông tin
    public async Task<KetQuaQuetVe> QuetMaAsync(string maQr)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(maQr))
            {
                _logger.LogWarning("Quét vé thất bại: MaQr trống");
                return TaoLoi("KHONG_TON_TAI", "Mã QR không hợp lệ.");
            }

            // ✅ Tìm vé từ MaVe hoặc MaQR để lấy MaDonHang
            var veTimDuoc = await _dbContext.ChiTietVe
                .FirstOrDefaultAsync(v => v.MaVe == maQr || v.MaQr == maQr);

            string maDonHang = null;

            if (veTimDuoc != null)
            {
                maDonHang = veTimDuoc.MaDonHang;
                _logger.LogInformation($"Tìm được vé {maQr} -> MaDonHang: {maDonHang}");
            }
            else
            {
                // Nếu không tìm được vé, coi maQr là MaDonHang
                maDonHang = maQr;
                _logger.LogInformation($"Không tìm được vé, coi {maQr} là MaDonHang");
            }

            // ✅ Lấy tất cả vé của đơn hàng
            var danhSachVe = await _dbContext.ChiTietVe
                .Include(v => v.MaSuatChieuNavigation)
                    .ThenInclude(sc => sc.MaPhimNavigation)
                .Include(v => v.MaSuatChieuNavigation)
                    .ThenInclude(sc => sc.MaPhongNavigation)
                .Include(v => v.MaGheNavigation)
                .Include(v => v.MaDonHangNavigation)
                .Where(v => v.MaDonHang == maDonHang)
                .ToListAsync();

            if (!danhSachVe.Any())
            {
                _logger.LogWarning($"Quét vé thất bại: Đơn hàng không tồn tại - MaDonHang: {maDonHang}");
                return TaoLoi("KHONG_TON_TAI", "Đơn hàng không tồn tại trong hệ thống.");
            }

            // ✅ Kiểm tra đơn hàng có bị xóa không
            var donHang = danhSachVe.First().MaDonHangNavigation;
            if (donHang?.DaXoa == true)
            {
                _logger.LogWarning($"Quét vé thất bại: Đơn hàng bị xóa - MaDonHang: {maDonHang}");
                return TaoLoi("KHONG_TON_TAI", "Đơn hàng không hợp lệ (đã xóa).");
            }

            // ✅ Kiểm tra vé chưa sử dụng
            var veConLai = danhSachVe.Where(v => v.TrangThai == "ChuaSuDung").ToList();

            if (!veConLai.Any())
            {
                _logger.LogWarning($"Quét vé thất bại: Tất cả vé đã sử dụng - MaDonHang: {maDonHang}");
                return TaoLoi("DA_DUNG", "Tất cả vé trong đơn hàng này đã được sử dụng rồi.");
            }

            // ✅ Lấy thông tin suất chiếu
            var veDauTien = veConLai.First();
            if (veDauTien.MaSuatChieuNavigation == null)
            {
                _logger.LogWarning($"Quét vé thất bại: Suất chiếu không tồn tại - MaDonHang: {maDonHang}");
                return TaoLoi("LOI_HE_THONG", "Dữ liệu suất chiếu bị lỗi.");
            }

            var suatChieu = veDauTien.MaSuatChieuNavigation;

            // ✅ Kiểm tra phòng chiếu có dữ liệu không (chỉ check null, KHÔNG check phòng nào)
            if (suatChieu.MaPhongNavigation == null)
            {
                _logger.LogWarning($"Quét vé thất bại: Phòng chiếu không tồn tại - MaDonHang: {maDonHang}");
                return TaoLoi("LOI_HE_THONG", "Dữ liệu phòng chiếu bị lỗi.");
            }

            // ✅ Lấy danh sách ghế
            var danhSachGhe = string.Join(", ", veConLai
                .Select(v => v.MaGheNavigation?.MaGhe ?? "N/A")
                .OrderBy(x => x));

            // ✅ Lấy danh sách dịch vụ của đơn hàng
            var danhSachDichVu = await _dbContext.ChiTietDichVu
                .Include(d => d.MaDichVuNavigation)
                .Where(d => d.MaDonHang == maDonHang)
                .ToListAsync();

            // ✅ Tính tổng tiền (vé + dịch vụ)
            double giaTienVe = veConLai.Sum(v => v.GiaVe);
            double giaTienDichVu = danhSachDichVu.Sum(d => d.DonGia * d.SoLuong);
            double tongTien = giaTienVe + giaTienDichVu;

            // ✅ Trả về thông tin vé
            _logger.LogInformation($"Quét vé thành công - MaDonHang: {maDonHang}, SoVeConLai: {veConLai.Count}, Ghe: {danhSachGhe}");

            return new KetQuaQuetVe
            {
                ThanhCong = true,
                MaTinhTrang = "THANH_CONG",
                TinNhan = $"Hợp lệ! ({veConLai.Count} vé)",
                DuLieuVe = new ChiTietVeQuetResponse
                {
                    MaVe = veConLai.FirstOrDefault()?.MaVe ?? "N/A",
                    SoLuongVe = veConLai.Count,
                    TenPhim = suatChieu.MaPhimNavigation?.TenPhim ?? "N/A",
                    TenPhong = suatChieu.MaPhongNavigation.TenPhong,
                    MaGhe = danhSachGhe,
                    ThoiGianBatDau = suatChieu.ThoiGianBatDau,
                    ThoiGianKetThuc = suatChieu.ThoiGianKetThuc,
                    GiaVe = giaTienVe,
                    GiaDichVu = giaTienDichVu,
                    TongTien = tongTien,
                    TrangThaiMoi = "ChuaSuDung",
                    DanhSachDichVu = danhSachDichVu.Select(d => new DichVuQuetResponse
                    {
                        TenDichVu = d.MaDichVuNavigation?.TenDichVu ?? "N/A",
                        SoLuong = d.SoLuong,
                        DonGia = d.DonGia,
                        ThanhTien = d.DonGia * d.SoLuong
                    }).ToList()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi không mong muốn khi quét vé - MaQr: {maQr}, Error: {ex.Message}");
            return TaoLoi("LOI_HE_THONG", "Có lỗi xảy ra, vui lòng thử lại.");
        }
    }

    // ✅ Xác nhận vé đã sử dụng
    public async Task<bool> XacNhanVaoRapAsync(string maDonHang)
    {
        try
        {
            var danhSachVe = await _dbContext.ChiTietVe
                .Where(v => v.MaDonHang == maDonHang && v.TrangThai == "ChuaSuDung")
                .ToListAsync();

            if (!danhSachVe.Any())
            {
                return false;
            }

            foreach (var ve in danhSachVe)
            {
                ve.TrangThai = "DaSuDung";
            }

            _dbContext.ChiTietVe.UpdateRange(danhSachVe);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Xác nhận vào rạp thành công - MaDonHang: {maDonHang}, SoVe: {danhSachVe.Count}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi xác nhận vào rạp - MaDonHang: {maDonHang}, Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> XacNhanVaoRapTheoMaVeAsync(string maVe)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(maVe))
            {
                return false;
            }

            var ve = await _dbContext.ChiTietVe
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.MaVe == maVe);

            if (ve == null || string.IsNullOrWhiteSpace(ve.MaDonHang))
            {
                _logger.LogWarning($"Không tìm thấy vé hoặc mã đơn hàng - MaVe: {maVe}");
                return false;
            }

            return await XacNhanVaoRapAsync(ve.MaDonHang);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi xác nhận theo mã vé - MaVe: {maVe}, Error: {ex.Message}");
            return false;
        }
    }

    private static KetQuaQuetVe TaoLoi(string maTinhTrang, string tinNhan)
    {
        return new KetQuaQuetVe
        {
            ThanhCong = false,
            MaTinhTrang = maTinhTrang,
            TinNhan = tinNhan
        };
    }
}