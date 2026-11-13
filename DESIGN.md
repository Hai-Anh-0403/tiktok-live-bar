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

HUD dùng khung tham chiếu tối thiểu 540 × 960, co theo cả hai chiều. Bỏ thanh tiêu đề thương hiệu phía trên theo yêu cầu; hai thẻ quà/Top giữ vị trí y=72. Thông báo quà neo dưới khung, phía trên vùng feed, để tránh tên/avatar khi camera focus. Sàn và người chơi là trọng tâm, tránh đặt chữ giữa mặt nhân vật. Giữ F1/F2/F3/F11 và các điều khiển hiện có.

## Elevation & Depth

Ba khung LED lùi dần về hậu cảnh, bục DJ và các bậc thềm tạo chiều sâu. Giữ lối nhìn tới GIF trung tâm và vị trí podium của Top 1/2/3. Tái sử dụng material, shader, đồng hồ BPM và cụm đèn sẵn có; không thêm post-processing hay đèn thời gian thực.

## Shapes

Khung chữ nhật, nan LED mảnh, bục tròn thấp. Không thêm vật cản che người chơi.

## Components

Equalizer bàn DJ và LED dùng BPM cấu hình của ClubBeatClock; đây không phải phân tích âm thanh bằng FFT. Chuyển động ánh sáng liên tục, không thêm strobe. Camera và hiệu ứng quà giữ nguyên. Preview trình duyệt minh họa thiết kế, không thay thế kiểm chứng Unity/Windows.

Quả cầu disco trong preview cần có các ô gương sáng/tối rõ để thấy hướng xoay, lõi tự phát sáng và quầng cyan mềm. Dùng xoay đều 24 độ/giây như rig Unity; nhịp sáng nhẹ và nút Tạm dừng dùng cùng đồng hồ mô phỏng. Không thêm đèn thời gian thực để bù cho vật liệu kim loại thiếu môi trường phản chiếu.

Nhân vật dùng ảnh gốc không nén phá hủy. Ảnh hiện tại khoảng 240–256 px cho người chơi và 416 × 380 cho GIF chính; không coi phóng to là tạo thêm chi tiết HD. Sáu kiểu chuyển động toàn thân thay phiên theo BPM, nối mềm giữa các kiểu; mỗi nhân vật lệch pha để không đồng loạt cùng một dáng. Không bóp méo mạnh mặt, không thay art và không giả định sprite có rig tay chân. Giữ đường đi/jump, thứ hạng và scale hiệu ứng quà. GIF chính chỉ nhận chuyển động phụ nhẹ.

## Character diversity and Top preview

Giữ toàn bộ clip cũ, bổ sung bốn nhân vật chibi có silhouette và màu riêng: mèo cam, robot cyan, vịt vàng, phi hành gia trắng/tím. Một atlas PNG trong suốt 4×4 chứa bốn động tác cho mỗi nhân vật; không thay GIF chính. Hai nhóm cũ chiếm tổng 50% lượt chọn (đầu nấm 25%, gấu trúc 25%); bốn nhóm mới chia đều 50% còn lại. Không xem 12 clip cùng đầu nấm là 12 loại ngoại hình. Lệnh đổi nhân vật loại nhóm hiện tại rồi chọn theo trọng số còn lại; giữ nguyên quà và câu lệnh.

Preview mặc định có 8 nhân vật: 4 thuộc hai nhóm cũ và 4 thuộc bốn nhóm mới. Ba Top đứng trên bục sát mép sân khấu, cùng hàng z=-4.15; bục Top 3 nâng lên y=0.55 và đưa về x=-4.6 để tách khỏi đám đông, giữ khoảng trống cho GIF chính. Tên, hạng và điểm minh họa đồng bộ bảng. Dùng nhãn gọn gắn với vị trí chiếu của nhân vật, tránh che mặt và chồng chữ. Nút Xem Top đưa camera tới góc sân khấu và quay lại toàn sàn; quà vẫn có thể focus như trước. Tất cả danh tính/điểm của preview phải ghi rõ mô phỏng, không giả là dữ liệu LIVE.

## New-player entrance

Người mới rơi từ phía trên về vị trí được phân: khoảng 10 đơn vị, 1.8 giây. Rơi tăng tốc rồi hãm mềm trong 72% thời gian đầu, chạm sàn và nảy nhẹ ở phần còn lại. Một quầng cyan lan ra trong khoảng 0.55 giây và squash/stretch nhẹ tạo cảm giác tiếp đất. Tái sử dụng glow/material/mesh và trạng thái spawn sẵn có, không thêm đèn thời gian thực, âm thanh hoặc thư viện. Hiệu ứng chỉ chạy một lần cho một lượt tạo người mới; nhận quà tiếp, focus và thay mẫu không làm rơi lại. Camera nhắm vị trí sàn trong lúc vào để thấy quá trình rơi. Giữ khả năng jump/walk hiện có để ngắt quá trình vào sàn.

## Stage flame jets

Bốn vòi phụt lửa đồ họa vàng–cam đặt trên mép sàn sân khấu phía sau người biểu diễn: x=-5,-1.6,1.6,5; y=0.8; z=-5.6. Phụt theo đợt ngắn, tắt mềm, không phủ bảng Top hoặc che mặt. Tái sử dụng hệ hạt/shader có sẵn, không thêm đèn/âm thanh. Giới hạn một đợt mỗi 3 giây, tối đa bốn vòi cùng lúc và dọn hiệu ứng sau khoảng 2 giây. Game kích hoạt cùng luồng pháo hoa/Party hiện có, giữ nguyên pháo hoa và pháo lạnh. Preview có nút Phụt lửa riêng, cùng cơ chế giới hạn và Tạm dừng.

## Viewer welcome and ranked wings

Khách vào xem LIVE nhận dòng “Tên khách đã vào bar” chạy từ phải sang trái trong dải cao 40 px, cách mép trên 12 px của HUD tham chiếu. Nền dùng surface tối, viền cyan, chữ đậm 18 px; vùng chữ được cắt gọn và không đè hai thẻ quà/Top ở y=72. Dải ẩn khi không có khách mới. Mỗi lượt 8 giây, tối đa 8 lượt chờ và bỏ lượt chờ quá 24 giây để tránh lời chào trễ. Tên là văn bản thuần, giữ nguyên nội dung sau chuẩn hóa khoảng trắng; tái dùng giới hạn tên sẵn có của Bridge thay vì cắt thêm có thể tách emoji trên runtime Unity. Sự kiện vào xem không tự thay đổi điều kiện vào sàn.

Ba hạng có cánh riêng từ bộ ảnh sẵn có: Top 1 lông vũ vàng/lửa (`wings-fire`), Top 2 pha lê bạc/cyan (`wings-ice`), Top 3 neon tím/hồng (`wings-neon`). Độ rộng giảm theo hạng, đập nhẹ theo nhịp sau lưng, không thêm đèn hoặc che tên/mặt. Hạng hiện tại quyết định cánh, kể cả lúc focus quà hoặc tổng quà lớn; đổi hạng chuyển theme ngay, rời Top trở về cánh quà thông thường. Preview giữ tối đa ba bộ cánh, gắn với người giữ hạng và có nút Khách vào để thử lời chào bằng dữ liệu mô phỏng.

## Board visibility

Hai bảng quà/Top mặc định hiện, có thể ẩn/hiện riêng trong menu F1 hoặc qua F4 (quà), F5 (Top). F3 tiếp tục bật/tắt chung HUD và giữ lựa chọn riêng khi bật lại. Dùng nút hiện có với nhãn động “ẨN/HIỆN QUÀ”, “ẨN/HIỆN TOP”; hai nút nằm dưới nội dung của cả hai tab, không chồng điều khiển cũ. Preview có hai nút tương ứng, trạng thái aria-pressed phản ánh bảng đang hiện. Ẩn riêng bảng không ẩn lời chào, nhân vật, tên/cánh/bục Top và không ngắt dữ liệu quà/điểm. Lựa chọn áp dụng trong phiên hiện tại, không thêm cấu hình lưu trên đĩa.

## Camera direction

Đạo diễn tự động mở rộng thành 12 góc: toàn sàn, quét ba lớp đám đông, Top, dolly hai bên, reveal hậu cảnh, cận sân khấu/DJ, vòng cung Top, crane và orbit chéo. Lia chậm theo nhịp, điểm đầu/cuối được ease và nối khoảng hai beat giữa góc; giữ đường chân trời thẳng, không rung tay hoặc lắc roll. Góc toàn sàn/Top/DJ tính khung ngang theo tỷ lệ màn hình để khung dọc không cắt chủ thể. Quà và lời chào vào sàn tiếp tục ưu tiên; hết cận cảnh trở về góc toàn ổn định rồi mới lia tiếp. F6 chuyển góc khi đang quay tự động. Preview có nút Góc nhìn tuần tự 12 góc và chế độ Camera tự động; nút Xem Top, focus quà và Tạm dừng vẫn hoạt động.

## Do's and Don'ts

- Giữ dữ liệu quà thật, cách tạo nhân vật, bảng Top và cấu hình TikFinity.
- Không đưa khách demo hoặc quà mô phỏng của preview vào Bridge đang LIVE.
- Kiểm tra khung dọc nhỏ và desktop; không tuyên bố FPS hoặc build Windows khi chưa chạy Unity.
