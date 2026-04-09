using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using RapChieuPhim.Models.Results;
using RapChieuPhim.Services;

namespace RapChieuPhim.Areas.RapPhim.Pages.QuetVe;

public class IndexModel : PageModel
{
    private readonly QuetVeService _quetVeService;
    private readonly QRCodeService _qrCodeService;
    private readonly PdfTicketService _pdfTicketService;
    private readonly ILogger<IndexModel> _logger;

    [BindProperty]
    public string MaQr { get; set; } = string.Empty;

    public KetQuaQuetVe? KetQua { get; set; }

    public string? QrCodeImage { get; set; }

    public string? MaQrDaQuet { get; set; }

    public IndexModel(QuetVeService quetVeService, QRCodeService qrCodeService, PdfTicketService pdfTicketService, ILogger<IndexModel> logger)
    {
        _quetVeService = quetVeService;
        _qrCodeService = qrCodeService;
        _pdfTicketService = pdfTicketService;
        _logger = logger;
    }

    public void OnGet()
    {
        _logger.LogInformation("[QuetVe] OnGet");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation($"[QuetVe] OnPost - MaQr: {MaQr}");

        if (string.IsNullOrWhiteSpace(MaQr))
        {
            ModelState.AddModelError(nameof(MaQr), "Vui lòng quét hoặc nhập mã QR.");
            return Page();
        }

        try
        {
            KetQua = await _quetVeService.QuetMaAsync(MaQr);

            if (KetQua.ThanhCong && KetQua.DuLieuVe != null)
            {
                MaQrDaQuet = MaQr;
                QrCodeImage = _qrCodeService.GenerateQRCodeBase64(MaQrDaQuet);
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

    public async Task<IActionResult> OnPostExportPdfAsync(string maVe)
    {
        if (string.IsNullOrWhiteSpace(maVe))
        {
            _logger.LogWarning("[QuetVe] Export PDF: Mã vé trống");
            return BadRequest(new { success = false, message = "Mã vé không hợp lệ" });
        }

        try
        {
            _logger.LogInformation($"[QuetVe] Đang tạo PDF cho vé: {maVe}");
            var pdfBytes = await _pdfTicketService.GeneratePdfTicketAsync(maVe);
            var fileName = $"VeTruocXuatBan_{maVe}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            _logger.LogInformation($"[QuetVe] ✅ PDF tạo thành công: {fileName}");
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[QuetVe] ❌ Lỗi tạo PDF - MaVe: {maVe}, Error: {ex.Message}");
            return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
        }
    }

    public IActionResult OnGetDebugQr(string maVe = "TEST_QR_001")
    {
        try
        {
            string qrCodeBase64 = _qrCodeService.GenerateQRCodeBase64(maVe);
            _logger.LogInformation($"[QuetVe] Debug QR for: {maVe}");

            return new JsonResult(new
            {
                success = true,
                maVe = maVe,
                qrCodeBase64 = qrCodeBase64,
                message = "✅ QR debug thành công"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[QuetVe] Debug QR Error: {ex.Message}");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostConfirmEntryAsync(string maVe)
    {
        if (string.IsNullOrWhiteSpace(maVe))
        {
            return BadRequest(new { success = false, message = "Mã vé không hợp lệ." });
        }

        try
        {
            var success = await _quetVeService.XacNhanVaoRapTheoMaVeAsync(maVe);

            if (!success)
            {
                return BadRequest(new { success = false, message = "Không thể xác nhận vào rạp (vé đã dùng hoặc không tồn tại)." });
            }

            _logger.LogInformation($"[QuetVe] ✅ Xác nhận vào rạp thành công - MaVe: {maVe}");
            return new JsonResult(new { success = true, message = "Xác nhận vào rạp thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[QuetVe] ❌ Lỗi xác nhận vào rạp - MaVe: {maVe}, Error: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi xác nhận vào rạp." });
        }
    }
}