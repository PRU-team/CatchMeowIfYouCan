
# 🐱 Catch Meow If You Can – Runner Game với Cheat System

Game runner 2D phát triển bằng Unity, tích hợp hệ thống cheat tiện lợi cho người chơi.

## ✨ Tính năng nổi bật

### 🎮 Gameplay

- Chạy vượt chướng ngại vật
- Hệ thống máu: Nhân vật có máu, va chạm sẽ mất máu và có thể chết
- Thu thập đồng xu để tăng điểm số
- Knockback: Bị đẩy lùi khi va vào chướng ngại vật


## 🚀 Hướng dẫn cài đặt & chạy game

### Yêu cầu hệ thống

- Unity 2022.3 LTS hoặc mới hơn
- Windows 10/11, macOS, hoặc Linux

### Cài đặt

1. Clone repository:
	```bash
	git clone https://github.com/PRU-team/CatchMeowIfYouCan.git
	```
2. Mở project bằng Unity Editor.
3. Tìm file `CatchMeowIfYouCan.exe` để chạy game (nếu có build sẵn).

## 🎮 Điều khiển

- A/D hoặc ←/→: Di chuyển trái/phải
- Space hoặc W: Nhảy
- ESC: Pause game
- TAB: Bật/tắt cheat mode


## 🛠️ Cấu trúc project

```
Assets/
├── Animations/
├── Art/
├── Audio/
├── Materials/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── BackGround/
│   ├── Collectibles/
│   ├── Core/
│   ├── Data/
│   ├── Debug/
│   ├── Debugging/
│   ├── Effects/
│   ├── Enemies/
│   ├── Environment/
│   ├── Level/
│   ├── Managers/
│   ├── Obstacles/
│   ├── Player/
│   ├── PowerUps/
│   ├── UI/
│   ├── Utils/
├── Settings/
├── Sprites/
├── TextMesh Pro/
├── Tile/
├── _Recovery/
```

### Một số file/scripts tiêu biểu

- `Scripts/Player/Player.cs`, `CatController.cs`, `CatAnimator.cs`
- `Scripts/Managers/GameManager.cs`, `LeaderboardManager.cs`, `Spawner.cs`
- `Scripts/Obstacles/BaseObstacle.cs`, `CarObstacle.cs`, `TrashBinObstacle.cs`
- `Scripts/Collectibles/BaseCollectible.cs`, `CoinCollectible.cs`, `GemCollectible.cs`
- `Scripts/UI/GameOverUI.cs`, `GameplayUI.cs`, `MainMenuUI.cs`, `SettingsUI.cs`
- `Scripts/PowerUps/BasePowerUp.cs`, `MagnetPowerUp.cs`, `ShieldPowerUp.cs`
- `Scripts/Enemies/CatcherController.cs`, `CatcherManager.cs`
- `Scripts/Effects/CameraShake.cs`, `AnimatedSprited.cs`
- `Scripts/Environment/BackgroundScroller.cs`, `StreetGenerator.cs`, `GroundMover.cs`
- `Scripts/Utils/StageManager.cs`


## �️ Cheat System (Thực tế)

Hiện tại project không có các script CheatSystem riêng biệt. Nếu muốn bật chế độ cheat/invincible, bạn có thể sử dụng các cách sau:

- **Debug/PowerUp**: Sử dụng các power-up như ShieldPowerUp để bảo vệ khỏi va chạm.
- **Chỉnh sửa script**: Có thể chỉnh sửa trực tiếp các script như `CatController.cs` để bỏ kiểm tra knockback, máu hoặc collision.
- **Debug Mode**: Thêm biến kiểm soát cheat (ví dụ: bool isCheatMode) vào các script Player hoặc Obstacles, sau đó kiểm tra phím TAB để bật/tắt trạng thái này.

Ví dụ ý tưởng cheat:

```csharp
// Trong CatController.cs
public bool isCheatMode = false;

void Update() {
	if (Input.GetKeyDown(KeyCode.Tab))
		isCheatMode = !isCheatMode;
}

// Khi xử lý va chạm
if (isCheatMode) {
	// Không mất máu, không knockback, đi xuyên vật cản
	return;
}
```

Bạn có thể tự thêm UI hiển thị trạng thái cheat bằng cách sử dụng các script UI có sẵn như `GameplayUI.cs`.

Nếu cần hướng dẫn chi tiết cách tích hợp cheat mode thực tế, hãy liên hệ hoặc tham khảo các script Player, Obstacles, PowerUps.

## � Troubleshooting

- Script class cannot be found: Kiểm tra lỗi compile, tên file và class phải khớp.
- Cheat không hoạt động: Đảm bảo Player có tag "Player", chướng ngại vật có `ObjDamage`.
- UI không hiển thị: Đảm bảo có Canvas, script `CheatUI` đã được attach.

## 📝 Changelog

### v1.0.0

- Hệ thống cheat cơ bản
- UI hiển thị trạng thái cheat
- Tự động reset collision khi tắt cheat
- Hướng dẫn sử dụng cheat

---

**Chúc bạn chơi game vui vẻ! 🎮**