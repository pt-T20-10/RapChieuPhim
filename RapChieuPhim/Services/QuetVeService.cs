using System;
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

    public async Task<KetQuaQuetVe> QuetMaAsync(string maQr, string maPhongThietBi)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(maQr))
            {
                _logger.LogWarning("Quét vé thất bại: MaQr trống");
                return TaoLoi("KHONG_TON_TAI", "Mã QR không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(maPhongThietBi))
            {
                _logger.LogWarning("Quét vé thất bại: MaPhongThietBi trống");
                return TaoLoi("SAI_PHONG", "Thiết bị không được cấu hình đúng.");
            }

            // ✅ Bước 1: Tìm vé - thử cả MaQr và MaVe
            var ve = await _dbContext.ChiTietVes
                .Include(v => v.MaSuatChieuNavigation)
                    .ThenInclude(sc => sc.MaPhimNavigation)
                .Include(v => v.MaSuatChieuNavigation)
                    .ThenInclude(sc => sc.MaPhongNavigation)
                .Include(v => v.MaGheNavigation)
                .Include(v => v.MaDonHangNavigation)
                .FirstOrDefaultAsync(v => v.MaQr == maQr || v.MaVe == maQr);

            if (ve == null)
            {
                _logger.LogWarning($"Quét vé thất bại: Vé không tồn tại - Ma: {maQr}");
                return TaoLoi("KHONG_TON_TAI", "Vé không tồn tại trong hệ thống.");
            }

            // ✅ Kiểm tra đơn hàng có bị xóa không
            if (ve.MaDonHangNavigation?.DaXoa == true)
            {
                _logger.LogWarning($"Quét vé thất bại: Đơn hàng bị xóa - MaVe: {ve.MaVe}");
                return TaoLoi("KHONG_TON_TAI", "Vé không hợp lệ (đơn hàng đã xóa).");
            }

            // ✅ Kiểm tra trạng thái vé
            if (ve.TrangThai != "ChuaSuDung")
            {
                var maTinhTrang = ve.TrangThai == "DaSuDung" ? "DA_DUNG" : "DA_HUY";
                var tinNhan = ve.TrangThai == "DaSuDung" 
                    ? "Vé này đã được sử dụng rồi." 
                    : "Vé này đã bị hủy.";

                _logger.LogWarning($"Quét vé thất bại: Vé đã {ve.TrangThai} - MaVe: {ve.MaVe}");
                return TaoLoi(maTinhTrang, tinNhan);
            }

            // ✅ Kiểm tra SuatChieu có tồn tại không
            if (ve.MaSuatChieuNavigation == null)
            {
                _logger.LogWarning($"Quét vé thất bại: Suất chiếu không tồn tại - MaVe: {ve.MaVe}");
                return TaoLoi("LOI_HE_THONG", "Dữ liệu suất chiếu bị lỗi.");
            }

            var suatChieu = ve.MaSuatChieuNavigation;
            var gioHienTai = DateTime.Now;
            var gioSomNhat = suatChieu.ThoiGianBatDau.AddMinutes(-15);

            // ✅ Kiểm tra giờ chiếu
            if (gioHienTai < gioSomNhat || gioHienTai > suatChieu.ThoiGianKetThuc)
            {
                _logger.LogWarning($"Quét vé thất bại: Sai giờ chiếu - MaVe: {ve.MaVe}, GioHienTai: {gioHienTai:O}");
                return TaoLoi("SAI_GIO", 
                    $"Giờ quét vé không hợp lệ. Giờ chiếu từ {gioSomNhat:dd/MM/yyyy HH:mm} đến {suatChieu.ThoiGianKetThuc:dd/MM/yyyy HH:mm}.");
            }

            // ✅ Kiểm tra phòng chiếu
            if (suatChieu.MaPhongNavigation == null)
            {
                _logger.LogWarning($"Quét vé thất bại: Phòng chiếu không tồn tại - MaVe: {ve.MaVe}");
                return TaoLoi("LOI_HE_THONG", "Dữ liệu phòng chiếu bị lỗi.");
            }

            var phongChieu = suatChieu.MaPhongNavigation;
            if (phongChieu.MaPhong != maPhongThietBi)
            {
                _logger.LogWarning($"Quét vé thất bại: Sai phòng - MaVe: {ve.MaVe}, PhongVe: {phongChieu.MaPhong}, PhongThietBi: {maPhongThietBi}");
                return TaoLoi("SAI_PHONG", 
                    $"Phòng chiếu không khớp. Vé này cho phòng '{phongChieu.TenPhong}' nhưng thiết bị quét ở phòng khác.");
            }

            // ✅ Cập nhật trạng thái và lưu DB
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    ve.TrangThai = "DaSuDung";
                    _dbContext.ChiTietVes.Update(ve);
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var phim = suatChieu.MaPhimNavigation;
                    _logger.LogInformation($"Quét vé thành công - MaVe: {ve.MaVe}, MaPhim: {phim?.MaPhim}, MaPhong: {phongChieu.MaPhong}");

                    return new KetQuaQuetVe
                    {
                        ThanhCong = true,
                        MaTinhTrang = "THANH_CONG",
                        TinNhan = "Quét vé thành công!",
                        DuLieuVe = new ChiTietVeQuetResponse
                        {
                            MaVe = ve.MaVe,
                            TenPhim = phim?.TenPhim ?? "N/A",
                            TenPhong = phongChieu.TenPhong,
                            MaGhe = ve.MaGheNavigation?.MaGhe ?? "N/A",
                            ThoiGianBatDau = suatChieu.ThoiGianBatDau,
                            ThoiGianKetThuc = suatChieu.ThoiGianKetThuc,
                            GiaVe = ve.GiaVe,
                            TrangThaiMoi = "DaSuDung"
                        }
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Lỗi lưu DB khi quét vé - MaVe: {ve.MaVe}, Error: {ex.Message}");
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi không mong muốn khi quét vé - MaQr: {maQr}, Error: {ex.Message}");
            return TaoLoi("LOI_HE_THONG", "Có lỗi xảy ra, vui lòng thử lại.");
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