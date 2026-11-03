# Postman – FPTU Cup API

Bộ test tự động cho toàn bộ API (118 request, hơn 270 assertion).

## Cách dùng

1. Chạy API ở môi trường Development (tự migrate và nạp dữ liệu mẫu): `dotnet run --project fptu_cup --urls http://localhost:5100`.
2. Trong Postman: **Import** hai file `FptuCup.postman_collection.json` và `FptuCup.local.postman_environment.json`, chọn environment **FPTU Cup - Local** (biến `baseUrl`, đổi nếu API chạy cổng khác).
3. Chạy collection bằng **Runner**, theo thứ tự các folder 01 → 08. Token và id được lưu vào biến collection sau mỗi bước.

## Chạy bằng dòng lệnh (Newman)

```bash
npm install -g newman
newman run FptuCup.postman_collection.json -e FptuCup.local.postman_environment.json
```

## Lưu ý

- Folder **01–06** chạy lại được nhiều lần (các lần tạo dữ liệu tạm đều tự xóa hoặc dùng tên ngẫu nhiên).
- Folder **07 – Trận trực tiếp** nộp đội hình, bắt đầu rồi **kết thúc** trận vòng 4 nên chỉ chạy được một lần. Kiểm tra BR09, BR10, BR14, BR19–BR22 và việc sinh án treo giò khi kết thúc trận.
- Folder **08 – Quá hạn nộp đội hình (BR23)** chạy sau folder 07, cũng chỉ chạy một lần: giả lập thời điểm 13/11/2026 09:30 (giờ Việt Nam) bằng `asOf` để xử thua đội thiếu đội hình, hủy trận khi cả hai đội thiếu.
- Muốn chạy lại folder 07–08: xóa database `FptuCupDb` (LocalDB), chạy lại app để seed lại.
- Dữ liệu mẫu gắn với lịch tháng 10–11/2026. Nếu giờ thật của máy đã qua hạn nộp đội hình vòng 4 (13/11/2026 08:00) thì `ForfeitWorker` đã tự xử các trận thiếu đội hình, nên folder 07–08 có thể không còn đúng kỳ vọng; khi đó hãy đổi ngày trong `DbSeeder`.
- Tài khoản mẫu: `admin / Admin@123`, `referee1`, `referee2` / `Referee@123`, `manager_cntt`, `manager_dt` / `Manager@123`.
