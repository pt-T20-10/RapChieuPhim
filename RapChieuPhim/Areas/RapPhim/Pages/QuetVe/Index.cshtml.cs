using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using RapChieuPhim.Models.Results;
using RapChieuPhim.Services;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;

namespace RapChieuPhim.Areas.RapPhim.Pages.QuetVe;

public class IndexModel : PageModel
{
    private readonly QuetVeService _quetVeService;
    private readonly QRCodeService _qrCodeService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<IndexModel> _logger;

    [BindProperty]
    public string MaQr { get; set; } = string.Empty;

    [BindProperty]
    public string MaPhongThietBi { get; set; } = string.Empty;

    public KetQuaQuetVe? KetQua { get; set; }
    
    public string? QrCodeImage { get; set; }

    public IndexModel(QuetVeService quetVeService, QRCodeService qrCodeService, AppDbContext dbContext, ILogger<IndexModel> logger)
    {
        _quetVeService = quetVeService;
        _qrCodeService = qrCodeService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public void OnGet()
    {
        MaPhongThietBi = HttpContext.Session.GetString("MaPhongThietBi") ?? "P001";
        _logger.LogInformation($"[QuetVe] OnGet - Phòng: {MaPhongThietBi}");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // ✅ LẤY MaPhongThietBi TỪ SESSION NGAY ĐẦU
        MaPhongThietBi = HttpContext.Session.GetString("MaPhongThietBi") ?? "P001";
        
        _logger.LogInformation($"[QuetVe] OnPost - MaQr: {MaQr}, Phòng: {MaPhongThietBi}");

        if (string.IsNullOrWhiteSpace(MaQr))
        {
            ModelState.AddModelError(nameof(MaQr), "Vui lòng quét hoặc nhập mã QR.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(MaPhongThietBi))
        {
            ModelState.AddModelError(nameof(MaPhongThietBi), "Thiết bị chưa được cấu hình.");
            return Page();
        }

        try
        {
            // Quét vé
            KetQua = await _quetVeService.QuetMaAsync(MaQr, MaPhongThietBi);

            // Nếu quét thành công, generate QR code
            if (KetQua.ThanhCong && KetQua.DuLieuVe != null)
            {
                QrCodeImage = _qrCodeService.GenerateQRCodeBase64(KetQua.DuLieuVe.MaVe);
                _logger.LogInformation($"[QuetVe] ✅ Quét vé thành công: {KetQua.DuLieuVe.MaVe}");
            }
            else
            {
                _logger.LogWarning($"[QuetVe] ❌ Quét vé thất bại: {KetQua.MaTinhTrang}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[QuetVe] ❌ Lỗi: {ex.Message}");
            ModelState.AddModelError("", "Có lỗi xảy ra!");
        }

        MaQr = string.Empty;
        return Page();
    }

    // API endpoint cho PDF export
    public async Task<IActionResult> OnPostExportPdfAsync(string maVe)
    {
        if (string.IsNullOrWhiteSpace(maVe))
        {
            return BadRequest("Mã vé không hợp lệ");
        }

        try
        {
            // Lấy thông tin vé từ DB
            var ve = await _dbContext.ChiTietVes
                .Include(v => v.MaSuatChieuNavigation)
                .ThenInclude(sc => sc.MaPhimNavigation)
                .Include(v => v.MaSuatChieuNavigation)
                .ThenInclude(sc => sc.MaPhongNavigation)
                .Include(v => v.MaDonHangNavigation)
                .ThenInclude(d => d.MaKhachHangNavigation)
                .Include(v => v.MaGheNavigation)
                .FirstOrDefaultAsync(v => v.MaVe == maVe);

            if (ve == null)
            {
                return NotFound();
            }

            // TODO: Tạo PDF từ thông tin vé
            // Tạm thời trả về JSON
            return new JsonResult(new
            {
                maVe = ve.MaVe,
                tenPhim = ve.MaSuatChieuNavigation.MaPhimNavigation.TenPhim,
                gioChieu = ve.MaSuatChieuNavigation.ThoiGianBatDau,
                phong = ve.MaSuatChieuNavigation.MaPhongNavigation.TenPhong,
                ghe = ve.MaGheNavigation.TenHang + ve.MaGheNavigation.SoThu,
                gia = ve.GiaVe
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi export PDF: {ex.Message}");
            return StatusCode(500, "Lỗi khi xuất PDF");
        }
    }
}