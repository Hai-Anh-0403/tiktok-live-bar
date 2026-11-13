---
name: Midnight Live Club
description: A layered concert stage that keeps gift senders visible in a portrait LIVE.
colors:
  background: "#060B18"
  surface: "#101B30"
  primary: "#42E8E0"
  secondary: "#FF4EA4"
  tertiary: "#FFC46B"
  text: "#F0F5FF"
  muted: "#AFBDD2"
typography:
  title:
    fontFamily: Unity default sans-serif
    fontSize: 20px
    fontWeight: 700
  body:
    fontFamily: Unity default sans-serif
    fontSize: 14px
    lineHeight: 1.4
rounded:
  sm: 4px
spacing:
  sm: 8px
  md: 12px
  lg: 24px
components:
  overlay-panel:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    padding: 12px
---

## Overview

Thiết kế sân khấu club với khung LED lồng nhiều lớp, bàn DJ, bục sân khấu và sàn tối. Giữ GIF chính, nền ngoài, nhân vật người xem và toàn bộ luồng quà hiện có. Màu cyan/hồng tạo chuyển động; vàng dành cho người ủng hộ.

## Colors

Nền xanh đen giữ tên người chơi rõ. LED cyan và hồng là điểm nhấn, không phủ sáng toàn màn hình. Giữ phong cách sân khấu đêm nhất quán; không thêm bộ chuyển light/dark cho game vốn chỉ có cảnh đêm. Giao diện điều khiển web không đổi.

## Typography

Dùng font sẵn có trong Unity, không tải font mạng. Nhãn quà và tên dài phải có vùng riêng, không đè số điểm. Nội dung trung lập: mô tả hiệu ứng và cảm ơn, không thúc ép tặng quà.

## Layout

HUD dùng khung tham chiếu tối thiểu 540 × 960, co theo cả hai chiều. Header nằm trên hai thẻ quà/Top; thông báo quà neo dưới khung, phía trên vùng feed, để tránh tên/avatar khi camera focus. Sàn và người chơi là trọng tâm, tránh đặt chữ giữa mặt nhân vật. Giữ F1/F2/F3/F11 và các điều khiển hiện có.

## Elevation & Depth

Ba khung LED lùi dần về hậu cảnh, bục DJ và các bậc thềm tạo chiều sâu. Giữ lối nhìn tới GIF trung tâm và vị trí podium của Top 1/2/3. Tái sử dụng material, shader, đồng hồ BPM và cụm đèn sẵn có; không thêm post-processing hay đèn thời gian thực.

## Shapes

Khung chữ nhật, nan LED mảnh, bục tròn thấp. Không thêm vật cản che người chơi.

## Components

Equalizer bàn DJ và LED dùng BPM cấu hình của ClubBeatClock; đây không phải phân tích âm thanh bằng FFT. Chuyển động ánh sáng liên tục, không thêm strobe. Camera và hiệu ứng quà giữ nguyên. Preview trình duyệt minh họa thiết kế, không thay thế kiểm chứng Unity/Windows.

Nhân vật dùng ảnh gốc không nén phá hủy. Ảnh hiện tại khoảng 240–256 px cho người chơi và 416 × 380 cho GIF chính; không coi phóng to là tạo thêm chi tiết HD. Sáu kiểu chuyển động toàn thân thay phiên theo BPM, nối mềm giữa các kiểu; mỗi nhân vật lệch pha để không đồng loạt cùng một dáng. Không bóp méo mạnh mặt, không thay art và không giả định sprite có rig tay chân. Giữ đường đi/jump, thứ hạng và scale hiệu ứng quà. GIF chính chỉ nhận chuyển động phụ nhẹ.

## Do's and Don'ts

- Giữ dữ liệu quà thật, cách tạo nhân vật, bảng Top và cấu hình TikFinity.
- Không đưa khách demo hoặc quà mô phỏng của preview vào Bridge đang LIVE.
- Kiểm tra khung dọc nhỏ và desktop; không tuyên bố FPS hoặc build Windows khi chưa chạy Unity.
