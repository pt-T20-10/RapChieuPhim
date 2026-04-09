using System;
using System.IO;
using QRCoder;

namespace RapChieuPhim.Services;

public class QRCodeService
{
    /// <summary>
    /// Tạo QR code dưới dạng Base64 string
    /// Module size: 20 (lớn hơn để dễ quét)
    /// ECCLevel: M (tối ưu balance giữa kích thước và khả năng khôi phục)
    /// </summary>
    public string GenerateQRCodeBase64(string data)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("Data không được để trống", nameof(data));
            }

            Console.WriteLine($"[QRCodeService] Generating QR for: {data}");

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                // ECCLevel.M thay vì Q để QR code nhỏ hơn, dễ quét hơn
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
                
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    // Module size: 20 (pixel per module)
                    byte[] qrCodeImage = qrCode.GetGraphic(20);
                    string base64String = Convert.ToBase64String(qrCodeImage);
                    
                    Console.WriteLine($"[QRCodeService] ✅ QR code generated successfully");
                    
                    return $"data:image/png;base64,{base64String}";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QRCodeService] ❌ Error: {ex.Message}");
            throw new Exception($"Lỗi tạo QR code: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tạo QR code dưới dạng PNG bytes (cho PDF export)
    /// </summary>
    public byte[] GenerateQRCodeBytes(string data)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("Data không được để trống", nameof(data));
            }

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    return qrCode.GetGraphic(20);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi tạo QR code: {ex.Message}", ex);
        }
    }
}