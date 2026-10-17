# PRD – SPACE EXPLORER

**Tên game:** Space Explorer  
**Thể loại:** 2D Space Shooter / Survival  
**Engine:** Unity 6  
**Nền tảng:** PC  
**Góc nhìn:** 2D Top-down

---

## 1. Tổng quan sản phẩm

**Space Explorer** là game 2D lấy bối cảnh ngoài không gian.

Người chơi điều khiển một **tàu vũ trụ** di chuyển trong khu vực không gian, né tránh các **asteroid**, thu thập **star** để tăng điểm để vượt màn và sử dụng hệ thống **tự động bắn** để tiêu diệt mục tiêu.

### Mục tiêu chính

> Di chuyển → Né tránh → Tự động bắn → Tiêu diệt mục tiêu → Thu thập Star → Tăng điểm → Sinh tồn

---

## 2. Gameplay

### 2.1 Player

Người chơi điều khiển một chiếc spaceship.

#### Di chuyển

Sử dụng:

- `↑`, `W` Di chuyển lên
- `↓`, `S` Di chuyển xuống
- `←`, `A` Di chuyển trái
- `→`, `D` Di chuyển phải

Tàu có thể di chuyển theo mọi hướng.

### Thông số

- Move Speed
- HP

---

## 3. Hệ thống bắn

Game sử dụng **auto-fire**.

Người chơi **không cần liên tục nhấn nút bắn**.

Tàu sẽ tự động tạo đạn theo khoảng thời gian:

```text
Fire Rate
    ↓
Spawn Bullet
    ↓
Bullet bay
    ↓
Va chạm Enemy/Asteroid
    ↓
Damage
    ↓
Destroy Bullet
```

### Thiết kế vũ khí

Không sử dụng asset súng riêng.

Hệ thống bắn có thể được điều chỉnh thông qua:

- Damage
- Fire Rate
- Bullet Count
- Speed

---

## 4. Bullet

Mỗi viên đạn có nhiệm vụ:

1. Được tạo ra từ `FirePoint`(được gán trên máy bay tùy loại).
2. Bay theo hướng bắn.
3. Kiểm tra va chạm.
4. Gây Damage cho mục tiêu.
5. Bị Destroy sau khi va chạm.

```text
Ship
 │
 │ Auto Fire
 ↓
Bullet
 │
 │ Move
 ↓
Asteroid
 │
 │ Damage
 ↓
Asteroid HP giảm
 │
 └── HP <= 0
        ↓
      Destroy Asteroid
```

---

## 5. Asteroid

Asteroid là các vật thể xuất hiện trong không gian động vào máy bay sẽ trừ hp tùy loại.

### Chức năng

- Di chuyển trong Scene.
- Có HP.
- Có thể va chạm với Player.
- Nhận damage từ Bullet.
- Bị tiêu diệt khi HP về 0.

### Thông số

- HP
- Move Speed
- Damage

---

## 6. Star

Star là vật phẩm để người chơi thu thập.

Khi Player chạm Star:

```text
Player
   ↓
chạm Star
   ↓
Star biến mất
   ↓
Score + điểm
```

Ví dụ:

```text
Star = +10 Score
```

Giá trị điểm có thể điều chỉnh trong quá trình phát triển.

---

## 7. HP / Damage

Player có HP.

Khi Player va chạm với Asteroid:

```text
Player
   ↓
Collision
   ↓
Take Damage
   ↓
HP giảm
```

Nếu:

```text
HP <= 0
```

thì:

```text
Game Over
   ↓
End Game Scene
```

---

## 8. Score System

Điểm số được tăng khi người chơi thu thập Star.

Ví dụ:

```text
Collect Star
     ↓
Score += 10
```

UI Gameplay hiển thị:

```text
SCORE: 120
```

Khi Game Over, điểm cuối cùng được truyền sang End Game Scene.
Khi đủ điểm thì sẽ qua màn tiếp theo

---

---

## 9. Scene và Game Flow

Game được chia thành các Scene chính:

```text
Main Menu
   ↓
Select Ship / Select Level
   ↓
Gameplay
   ↓
┌───────────────────────┐
│ Hoàn thành màn chơi   │
│ hoặc Game Over        │
└───────────────────────┘
   ↓
End Game
```

### 9.1 Main Menu

Main Menu là màn hình chính để người chơi lựa chọn các chức năng trước khi bắt đầu chơi.

Phần chữ hiển thị trực tiếp trong game sử dụng **English**.

```text
              SPACE EXPLORER

                  [ START ]

              [ SELECT SHIP ]

             [ SELECT LEVEL ]

             [ INSTRUCTIONS ]

                  [ QUIT ]
```

- **START**: Bắt đầu chơi với spaceship và level đang được lựa chọn.
- **SELECT SHIP**: Mở giao diện lựa chọn spaceship.
- **SELECT LEVEL**: Mở giao diện lựa chọn level.
- **INSTRUCTIONS**: Hiển thị hướng dẫn chơi.
- **QUIT**: Thoát game.

### 9.2 Select Ship

Người chơi có thể lựa chọn spaceship trước khi bắt đầu level.

```text
                SELECT SHIP

          [ SHIP 01 ]  [ SHIP 02 ]

          [ SHIP 03 ]  [ SHIP 04 ]

                 [ SELECT ]

                   [ BACK ]
```

Mỗi spaceship có thể có thông số khác nhau:

- HP
- Move Speed
- Damage
- Fire Rate
- Bullet Count

Khi người chơi chọn một spaceship, các thông số tương ứng được hiển thị để người chơi biết khả năng của tàu.

Nếu phiên bản đầu tiên chưa có nhiều spaceship, có thể chỉ triển khai một spaceship mặc định nhưng vẫn giữ hệ thống `SELECT SHIP` để mở rộng sau.

### 9.3 Select Level

Người chơi lựa chọn level tại giao diện Select Level.

```text
               SELECT LEVEL

                [ LEVEL 1 ]

              [ LEVEL 2 🔒 ]

              [ LEVEL 3 🔒 ]

              [ LEVEL 4 🔒 ]

                   [ BACK ]
```

Mỗi level có một trong các trạng thái:

- **UNLOCKED**: Có thể chơi.
- **LOCKED**: Chưa được mở khóa.
- **COMPLETED**: Đã hoàn thành.

Level được mở khóa theo thứ tự:

```text
Complete LEVEL 1
       ↓
Unlock LEVEL 2
       ↓
Complete LEVEL 2
       ↓
Unlock LEVEL 3
       ↓
Complete LEVEL 3
       ↓
Unlock LEVEL 4
```

Người chơi **không thể chơi level tiếp theo khi chưa hoàn thành level trước đó**.

Sau khi hoàn thành một level, trạng thái mở khóa cần được lưu lại để khi người chơi thoát game và mở lại, các level đã mở khóa vẫn được giữ nguyên.

### 9.4 Gameplay Scene

Gameplay Scene là nơi người chơi trực tiếp điều khiển spaceship và thực hiện các nhiệm vụ của level.

Gameplay cần hiển thị tối thiểu:

```text
SCORE: 120
HP: 80
LEVEL 1
```

Trong Gameplay:

- Player di chuyển bằng các phím mũi tên.
- Player tự động bắn.
- Asteroid xuất hiện và di chuyển trong Scene.
- Bullet gây Damage cho mục tiêu.
- Player nhận Damage khi va chạm với Asteroid.
- Player thu thập Star để tăng Score.

Mỗi level có thể thay đổi:

- Background / môi trường không gian.
- Số lượng Asteroid.
- Tốc độ Asteroid.
- HP hoặc Damage của Asteroid.
- Số lượng Star.
- Mục tiêu cần đạt để hoàn thành level.

### 9.5 Điều kiện hoàn thành Level

Một level được xem là hoàn thành khi người chơi đạt đủ điều kiện hoàn thành được thiết lập cho level đó.

Điều kiện chính được sử dụng trong phiên bản hiện tại:

```text
Score >= Target Score
       ↓
Complete Level
       ↓
Unlock Next Level
```

Ví dụ:

```text
Target Score = 100

Player Score = 100
       ↓
LEVEL COMPLETED
       ↓
Unlock LEVEL 2
```

Khi hoàn thành level:

1. Lưu trạng thái level là `COMPLETED`.
2. Mở khóa level tiếp theo.
3. Chuyển sang End Game hoặc màn hình thông báo hoàn thành.
4. Cho phép người chơi quay về Main Menu hoặc tiếp tục level tiếp theo.

### 9.6 Game Over

Nếu HP của Player giảm xuống 0:

```text
HP <= 0
   ↓
GAME OVER
   ↓
End Game
```

Người chơi không được xem là hoàn thành level nếu bị Game Over.

### 9.7 End Game Scene

End Game Scene hiển thị kết quả của lần chơi hiện tại.

```text
                GAME OVER

                 SCORE
                   120

              [ PLAY AGAIN ]

              [ MAIN MENU ]

                  [ QUIT ]
```

Khi level được hoàn thành, nội dung có thể thay đổi thành:

```text
             LEVEL COMPLETED

                 SCORE
                   120

             [ NEXT LEVEL ]

              [ MAIN MENU ]

                  [ QUIT ]
```

- **PLAY AGAIN**: Chơi lại level hiện tại.
- **NEXT LEVEL**: Chuyển sang level tiếp theo khi level đã hoàn thành.
- **MAIN MENU**: Quay về Main Menu.
- **QUIT**: Thoát game.

---

## 10. Game Object Structure

Cấu trúc GameObject chính trong Gameplay Scene:

```text
Gameplay
│
├── GameManager
│
├── Player
│   ├── PlayerVisual
│   └── FirePoint
│
├── Enemies
│   ├── Asteroid
│   ├── Asteroid
│   └── ...
│
├── Collectibles
│   ├── Star
│   ├── Star
│   └── ...
│
├── BulletContainer
│
├── Main Camera
│
└── Canvas
    ├── Score
    ├── HP
    ├── Level
    └── Pause
```

Cấu trúc này giúp phân biệt rõ các nhóm đối tượng trong Gameplay Scene và thuận tiện cho việc quản lý khi số lượng object tăng lên.

---

## 11. Script Structure

Các script được chia theo chức năng:

```text
Assets/Code/Scripts
│
├── Player
│   └── PlayerController.cs
│
├── Enemy
│   └── EnemyController.cs
│
├── Weapon
│   ├── WeaponAim.cs
│   └── Bullet.cs
│
├── Collectible
│   └── Star.cs
│
├── Game
│   ├── GameManager.cs
│   ├── ScoreManager.cs
│   ├── LevelManager.cs
│   └── SceneLoader.cs
│
├── UI
│   ├── MainMenuUI.cs
│   ├── SelectShipUI.cs
│   ├── SelectLevelUI.cs
│   ├── GameplayUI.cs
│   └── EndGameUI.cs
│
└── Managers
    └── AudioManager.cs
```

### Chức năng chính

- `PlayerController.cs`: xử lý di chuyển và trạng thái Player.
- `EnemyController.cs`: xử lý di chuyển, HP và Damage của Asteroid.
- `WeaponAim.cs`: xử lý hệ thống tự động bắn và tạo Bullet.
- `Bullet.cs`: xử lý chuyển động và va chạm của Bullet.
- `Star.cs`: xử lý việc thu thập Star.
- `ScoreManager.cs`: quản lý điểm số.
- `GameManager.cs`: quản lý trạng thái Gameplay và điều kiện thắng/thua.
- `LevelManager.cs`: quản lý level hiện tại, level đã hoàn thành và level được mở khóa.
- `SceneLoader.cs`: chuyển đổi giữa các Scene.
- `MainMenuUI.cs`: xử lý các button trong Main Menu.
- `SelectShipUI.cs`: xử lý lựa chọn spaceship.
- `SelectLevelUI.cs`: xử lý lựa chọn level và trạng thái khóa/mở.
- `GameplayUI.cs`: cập nhật Score, HP và Level trên UI.
- `EndGameUI.cs`: hiển thị kết quả và xử lý các button sau khi kết thúc level.

---

## 12. Level System

Mỗi level có cấu hình riêng để tạo sự khác biệt trong Gameplay.

Ví dụ:

| Level | Môi trường | Asteroid | Target Score |
|---|---|---:|---:|
| Level 1 | Space Training | Ít | 100 |
| Level 2 | Asteroid Zone | Vừa | 200 |
| Level 3 | Deep Space | Nhiều | 300 |
| Level 4 | Dangerous Space | Nhiều + nhanh | 500 |

Các giá trị trên là ví dụ để định hướng thiết kế và có thể thay đổi trong quá trình phát triển.

Mỗi level cần lưu tối thiểu:

- Level ID
- Target Score
- Asteroid Count
- Asteroid Speed
- Asteroid Damage
- Star Count
- Background

---

## 13. Data và Progress

Game cần lưu các dữ liệu liên quan đến tiến trình người chơi:

```text
Selected Ship
       +
Current Level
       +
Unlocked Levels
       +
Completed Levels
```

Sau khi hoàn thành level:

```text
Current Level
      ↓
Completed
      ↓
Save Progress
      ↓
Unlock Next Level
```

Tiến trình cần được giữ lại sau khi đóng và mở lại game.

---

## 14. UI Requirements

Toàn bộ text hiển thị trực tiếp trong game sử dụng **English**.

Ví dụ:

```text
SPACE EXPLORER

START
SELECT SHIP
SELECT LEVEL
INSTRUCTIONS
QUIT

SCORE
HP
LEVEL

GAME OVER
LEVEL COMPLETED

PLAY AGAIN
NEXT LEVEL
MAIN MENU
BACK
```

Phần giải thích trong PRD và tài liệu phát triển vẫn sử dụng tiếng Việt.

UI cần dễ đọc, nút bấm rõ ràng và thống nhất phong cách với chủ đề không gian.

---

## 15. Audio

Game có thể sử dụng:

- Background Music cho Main Menu.
- Background Music cho Gameplay.
- Sound Effect khi bắn.
- Sound Effect khi Bullet va chạm.
- Sound Effect khi thu thập Star.
- Sound Effect khi Player nhận Damage.
- Sound Effect khi hoàn thành Level.
- Sound Effect khi Game Over.

Các asset âm thanh nên được tổ chức riêng theo Music và Sound Effect để dễ quản lý.

---

## 16. Asset Organization

Cấu trúc asset đề xuất:

```text
Assets
│
├── Art
│   └── Sprites
│       ├── Player
│       ├── Enemy
│       ├── Environment
│       ├── Collectibles
│       ├── Weapons
│       └── UI
│
├── Audio
│   ├── Music
│   └── Sound
│
├── Code
│   └── Scripts
│
└── Level
    ├── Prefabs
    │   ├── Player
    │   ├── Enemy
    │   ├── Star
    │   ├── Bullet
    │   └── UI
    │
    ├── Scenes
    ├── TilePalettes
    │   ├── Environment
    │   └── Gameplay
    │
    └── UI
        ├── MainMenu
        ├── Gameplay
        └── EndGame
```

Đối với ảnh tài nguyên môi trường hoặc Tilemap, nên giữ file gốc riêng với các tài nguyên đã xử lý để dễ chỉnh sửa và quản lý.

---

## 17. Gameplay Flow Chi tiết

Luồng chơi dự kiến:

```text
                 MAIN MENU
                     │
        ┌────────────┼────────────┐
        ↓            ↓            ↓
   SELECT SHIP  SELECT LEVEL  INSTRUCTIONS
        │            │
        └──────┬─────┘
               ↓
             START
               ↓
           GAMEPLAY
               │
        ┌──────┴──────┐
        ↓             ↓
     GAME OVER    TARGET SCORE
        │             │
        ↓             ↓
    END GAME    LEVEL COMPLETED
                      │
                      ↓
               UNLOCK NEXT LEVEL
                      │
              ┌───────┴───────┐
              ↓               ↓
          NEXT LEVEL       MAIN MENU
```

---

## 18. MVP

Phiên bản MVP cần hoàn thành các chức năng sau:

### Player
- Di chuyển bằng phím mũi tên.
- Có HP.
- Có hệ thống Auto Fire.

### Combat
- Bullet được tạo từ FirePoint.
- Bullet di chuyển và va chạm.
- Asteroid có HP.
- Asteroid nhận Damage và bị Destroy khi HP về 0.

### Collectible
- Star xuất hiện trong Gameplay.
- Player thu thập Star.
- Score tăng sau khi thu thập.

### Level
- Có nhiều level.
- Level được mở khóa tuần tự.
- Không thể chơi level tiếp theo khi level trước chưa hoàn thành.
- Tiến trình mở khóa được lưu lại.

### Scene
- Main Menu.
- Select Ship.
- Select Level.
- Gameplay.
- End Game.

### UI
- SCORE.
- HP.
- LEVEL.
- Các button điều hướng.
- Thông báo Game Over / Level Completed.

---

## 19. Thứ tự triển khai

Nên triển khai theo thứ tự sau để tránh phụ thuộc chéo giữa các hệ thống:

```text
1. Player Movement
        ↓
2. Player HP
        ↓
3. Auto Fire
        ↓
4. Bullet
        ↓
5. Asteroid
        ↓
6. Bullet → Asteroid Damage
        ↓
7. Player → Asteroid Damage
        ↓
8. Star → Score
        ↓
9. Level Completion
        ↓
10. Main Menu
        ↓
11. Select Ship
        ↓
12. Select Level + Lock/Unlock
        ↓
13. End Game
        ↓
14. Save Progress
        ↓
15. Audio / VFX / Polish
```

---

## 20. Acceptance Criteria

Game được xem là hoàn thành khi:

- Player di chuyển đúng bằng các phím mũi tên.
- Player tự động bắn theo Fire Rate.
- Bullet có thể gây Damage cho Asteroid.
- Asteroid có thể bị Destroy khi HP về 0.
- Player mất HP khi va chạm với Asteroid.
- Star được thu thập và cộng Score.
- Khi HP về 0, game chuyển sang trạng thái Game Over.
- Khi đạt Target Score, level được hoàn thành.
- Hoàn thành level hiện tại sẽ mở khóa level tiếp theo.
- Người chơi không thể vào level đang bị LOCKED.
- Trạng thái mở khóa level được lưu sau khi thoát game.
- Main Menu có các chức năng:
  - `START`
  - `SELECT SHIP`
  - `SELECT LEVEL`
  - `INSTRUCTIONS`
  - `QUIT`
- End Game hiển thị đúng kết quả của lần chơi.
- Text hiển thị trong game được thống nhất bằng English.
