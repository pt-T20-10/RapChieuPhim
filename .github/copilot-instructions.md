# Copilot Instructions

## Project Guidelines
- Tính năng quét vé chỉ đơn giản: quét QR (MaDonHang) từ email → hiển thị thông tin vé (phim, ghế, thời gian, giá) để in/biên lại. Không cần kiểm tra phòng chiếu hay cấu hình phòng thiết bị. Bỏ toàn bộ logic MaPhongThietBi và kiểm tra SAI_PHONG từ QuetVeService, Index.cshtml.cs, và Index.cshtml UI. Ưu tiên độ tin cậy của việc quét QR hơn là các thay đổi về giao diện, và không hài lòng khi các chỉnh sửa giao diện làm hỏng chức năng quét. Nếu có vấn đề về độ tin cậy của máy quét, ưu tiên sửa chữa chức năng trực tiếp thay vì thay đổi kiểu dáng theo từng giai đoạn.
- Sử dụng iTextSharp để tạo PDF trong hệ thống in vé. PDF nên bao gồm: thông tin phim (tên, thời gian, phòng), chi tiết ghế (số ghế, loại ghế), dịch vụ đã đặt (bỏng ngô, đồ uống - số lượng và giá), tổng số tiền, và quy định của rạp chiếu phim. Ưu tiên in PDF vé bằng tiếng Việt có dấu đầy đủ và bỏ phần QR để vé gọn như vé phim thực tế.

## UI Improvements
- Ưu tiên cải tiến giao diện người dùng thực tế, sạch sẽ và tránh các thay đổi hình thức quá cầu kỳ.

## Ngôn Ngữ
- Ưu tiên phản hồi bằng tiếng Việt.