# Pause và độ khó

## Cấu hình trong Inspector

Mỗi scene `Level1`–`Level4` dùng `GameManager.Required Score` và các trường
`Level Difficulty` trên `AsteroidSpawner` hiện có. Không có hệ thống wave thứ hai.

| Scene | Required Score | Spawn Interval | Speed Multiplier | Max Active Asteroids |
| --- | ---: | ---: | ---: | ---: |
| Level1 | 100 | 2.5 s | 1.00 | 5 |
| Level2 | 200 | 2.2 s | 1.10 | 7 |
| Level3 | 300 | 2.0 s | 1.20 | 9 |
| Level4 | 400 | 1.8 s | 1.30 | 11 |

Độ khó tăng qua nhịp spawn, tốc độ, mật độ và thời gian cần thu thập sao.
Tỷ trọng Small/Medium/Large mặc định là 9/1/0.25 ở cả bốn màn: ưu tiên mục tiêu
ít HP để các tàu bắn một viên mỗi giây vẫn có cơ hội xử lý. Đây là trọng số
lựa chọn, không bảo đảm tỷ lệ chính xác trong một lượt chơi; bước kiểm tra
vị trí an toàn có thể bỏ qua một lượt spawn.

Các lượt Play Mode ban đầu cho thấy cấu hình 7/2/1 cùng Level4 1.3 s, 1.45x
tạo quá nhiều mục tiêu còn sống với các tàu bắn một viên mỗi giây. Lượt kiểm tra
tiếp theo với 9/1/0.25 vẫn gây áp lực lớn cho tàu một nòng ở Level3–4. Bản cân
bằng ưu tiên Small (khoảng 88% trước bước kiểm tra vị trí), tăng hệ số tốc độ từng
bước 0.1 và dùng khoảng spawn 2.5/2.2/2.0/1.8 s để dành thời gian thu thập sao.
Camera Level3 có orthographic size 10, còn Level4 là 8; do đó Level4 vốn có
quãng đường rơi ngắn hơn. Không cần tăng tốc độ quá mạnh để tạo áp lực cao hơn.

`Spawn Area / Safety` cho phép chỉnh khoảng cách mép màn hình (0.5 world unit),
khoảng cách ngang của các thiên thạch vừa xuất hiện (1.2) và khoảng cách dự đoán
giữa các lần chạm đáy (0.75 s). Vị trí tính cả kích thước sprite và camera thực tế.
Chỉ sinh một thiên thạch mỗi lượt, không dồn lượt spawn đã bỏ lỡ sau Pause.
Giới hạn số lượng là trần an toàn, không phải số lượng bắt buộc trên màn hình.

Giữ nguyên thông số prefab:

| Thiên thạch | HP | Tốc độ gốc | Damage va chạm / chạm đáy |
| --- | ---: | ---: | ---: |
| Small | 10 | 1.5 | 10 / 10 |
| Medium | 30 | 1.2 | 20 / 20 |
| Large | 50 | 0.8 | 30 / 30 |

Sao vẫn xuất hiện mỗi 2 s, rơi với tốc độ 2 và cộng 10 điểm khi thu thập.
HP, damage, bullet count, fire rate của mỗi Player Prefab không đổi.

| Player Prefab | HP | Move Speed | Damage / viên | Bullet Count | Fire Rate |
| --- | ---: | ---: | ---: | ---: | ---: |
| Player | 100 | 5 | 10 | 2 | 0.5 s |
| Player 1 | 100 | 5 | 20 | 1 | 1.0 s |
| Player 2 | 100 | 5 | 20 | 1 | 1.0 s |

`ShipManager`, `PlayerSpawner`, luồng mở khóa và chuyển màn giữ cấu trúc hiện có.
Level1–3 mở khóa và tự chuyển sang màn kế tiếp. Level4 giữ trạng thái
`LevelComplete` và thông báo hoàn thành trong log như trước; không thêm luồng
win scene mới. Game Over vẫn tải `EndGame`.

## Pause

`Assets/Level/Prefabs/UI/PauseMenu.prefab` được dùng dưới `GameplayUI` của cả
bốn scene. Nút PAUSE nằm dưới HP; overlay chỉ hiển thị khi đang Pause.
`LevelStartUI` vẫn chỉ nằm trên `LevelStartText`.

- PAUSE, Escape hoặc P: tạm dừng; Escape/P lần nữa hoặc RESUME: tiếp tục.
- RESTART: khôi phục thời gian, tải lại chính scene đang chơi.
- MAIN MENU: khôi phục thời gian, trở về MainMenu.
- GameManager lưu time scale trước Pause, chặn Pause khi đã kết thúc màn.
- Gameplay dùng thời gian scaled và kiểm tra trạng thái trước khi thay đổi
  movement, spawn, damage hoặc score. Không có vòng spawn chạy bằng thời gian thực.
- AudioManager Pause/UnPause hai AudioSource hiện có, chỉ tiếp tục nguồn đang phát
  trước Pause; không sinh thêm nguồn hoặc manager.
- Font dùng asset `Audiowide-Regular SDF.asset` hiện có. Title #F4F8FF,
  chữ nút #FFFFFF, hover/focus #66E6FF qua helper `ButtonTextState` hiện có.

## Kiểm tra

Trong Edit Mode, lưu scene rồi chọn **Tools > Space Explorer > Verify Pause and Balance**.
Harness chạy các scene thật trong Play Mode, kiểm tra Pause, timer, movement,
score/HP, audio, Escape/P, restart, menu, Game Over và mở khóa màn kế tiếp.
Sau đó bot dùng Input System/WASD và vũ khí thật để thử từng prefab tàu với cả
bốn màn. Bot không thay HP/damage/fire rate; simulation chạy time scale 1–2 để
rút ngắn thời gian nhưng vẫn căn bắn chính xác cho tàu một nòng. Đây không
thay thế đánh giá cảm giác chơi bởi người thật.

Kết quả nằm ở `Temp/PauseBalanceWork/playmode-results.txt` (không đưa vào Git).
Nếu bot thua, **Tools > Space Explorer > Retry Failed Ship Trials** chạy thêm
tối đa ba seed cho các trường hợp đó và giữ kết quả ban đầu trong
`initial-matrix-results.txt`. Một lượt bot thua được ghi rõ; việc thắng ở seed
khác chỉ chứng minh khả năng hoàn thành, không bảo đảm thắng mọi lượt ngẫu nhiên.
Harness sao lưu và khôi phục SelectedPlayer, LastLevel, UnlockedLevel và scene
đang mở. Nếu Unity bị đóng đột ngột giữa bài kiểm tra, bản sao nằm ở
`Temp/PauseBalanceWork/test-backup.json`.

Kiểm tra thủ công thêm ở Game View 9:16: nhấp chuột và điều hướng bàn phím các
nút, nghe âm thanh khi Pause/Resume, quan sát SCORE/HP và LEVEL banner, chơi
nhiều lượt với từng tàu để đánh giá nhịp độ và khả năng né theo nhiều seed.
