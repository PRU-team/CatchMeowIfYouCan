## 🚨 **SETUP DEBUG GUIDE - SỬA LỖI BUILDING & CATCHER**

### **Bước 1: Thêm GameDebugTool**
1. Tạo Empty GameObject tên "GameDebugTool"
2. Add Component → GameDebugTool 
3. Bật "Enable Debug Logs" và "Show Visual Debug"

### **Bước 2: Test Building Movement**
1. Right-click GameDebugTool → **Debug Building Movement**
2. Kiểm tra Console logs:
   - ✓ "Fixed Background Manager found - buildings should move with ground"
   - ✓ "Ground [name] has GroundMover component" 
   - ✓ "Building [name] attached to [ground]"

**Nếu thấy lỗi:**
- "✗ No Fixed Background Manager" → Tạo FixedBackgroundManager
- "✗ Ground missing GroundMover component" → Right-click GameDebugTool → **Fix All Ground Movers**
- "✗ Building has NO PARENT" → Right-click BuildingSpawner → **Force Fix All Building Attachments**

### **Bước 3: Test Catcher Boundary Detection**
1. Right-click GameDebugTool → **Debug Catcher Boundary Detection**
2. Di chuyển mèo đến gần LEFT boundary (trái màn hình)
3. Kiểm tra Console logs:
   - Distance to LEFT boundary: [số nhỏ]
   - Should trigger: true

**Nếu Catcher không trigger:**
- Check "Cat target is NULL" → Đảm bảo mèo có tag "Player"
- Check "Game camera is NULL" → Assign Main Camera
- Right-click GameDebugTool → **Force Trigger Nearest Catcher** để test manual

### **Bước 4: Setup đúng Catcher**
1. **Tạo Catcher GameObject:**
   - Position: (0, 0, 0) 
   - Add CatcherController component
   - Add Sprite Renderer với sprite

2. **Configure CatcherController:**
   ```
   Trigger Boundary: Left
   Boundary Trigger Distance: 2
   Active Position: (-3, 0, 0)  // Gần biên trái
   Touch Detection Radius: 1.5
   Enable Debug Logs: ✓
   ```

3. **Tạo CatcherManager:**
   - Empty GameObject tên "CatcherManager"
   - Add CatcherManager component
   - Auto Find Catchers: ✓

### **Bước 5: Quick Fix Commands**
```
Right-click GameDebugTool:
→ Debug Building Movement       (kiểm tra building)
→ Debug Catcher Boundary        (kiểm tra catcher)
→ Force Test Building Movement  (test chi tiết)
→ Force Trigger Nearest Catcher (test catcher manual)
→ Fix All Ground Movers         (sửa ground movement)
→ Reset All Systems             (reset mọi thứ)
```

### **Bước 6: Visual Debug**
Trong Scene view sẽ thấy:
- **White lines**: Camera boundaries
- **Red lines**: Trigger zones (2 units from boundary)
- **Yellow sphere**: Cat position

### **Common Issues & Fixes:**

**Building không di chuyển:**
1. Check có FixedBackgroundManager không
2. Check Ground có GroundMover component không  
3. Check Building có parent là Ground không
4. Use: Fix All Ground Movers

**Catcher không xuất hiện:**
1. Check Cat có tag "Player" không
2. Check Catcher có CatcherController không
3. Check Trigger Boundary Direction đúng không
4. Check Boundary Trigger Distance đủ lớn không
5. Di chuyển mèo sát biên trái hơn

**Cả hai đều không hoạt động:**
1. Check GameManager có bị pause game không (Time.timeScale = 0)
2. Check các GameObject có bị disabled không
3. Use: Reset All Systems

### **Expected Behavior:**
1. **Buildings**: Di chuyển từ phải qua trái cùng với ground
2. **Catcher**: Xuất hiện khi mèo cách biên trái ≤ 2 units
3. **Debug Logs**: Continuous logging về trạng thái boundary detection

Use GameDebugTool để debug và kiểm tra mọi thứ! 🛠️