# โปรแกรมอ่านบัตรประชาชน

รันบนเครื่อง Windows ที่เสียบเครื่องอ่าน USB (เครื่อง รปภ.) — ย่อใน **System Tray**

## รัน

ดับเบิลคลิก `start-card-reader.bat` หรือ:

```bash
dotnet run --project src/VisitorManagement.CardReader
```

## Publish (สำหรับ copy ไปเครื่องอื่น)

ดับเบิลคลิก `publish-card-reader.bat` หรือ:

```bash
dotnet publish src/VisitorManagement.CardReader -c Release -o ./publish
```

ใน Visual Studio: คลิกขวาโปรเจกต์ → **Publish** → ใช้โปรไฟล์ `FolderProfile` (target `net8.0-windows`)

แล้วคัดลอกโฟลเดอร์ publish ไปเครื่อง รปภ. แล้วรัน `VisitorManagement.CardReader.exe`

## System Tray

- คลิกขวาไอคอน → สถานะเครื่องอ่าน / เปิดหน้า Health / ออกจากโปรแกรม
- ดับเบิลคลิกไอคอน → ดูสถานะ
- รันซ้ำถ้าเปิดอยู่แล้ว จะแจ้งว่ากำลังทำงานอยู่

ฟังที่ `http://127.0.0.1:5001`

- `GET /health`
- `GET /api/status`
- `GET /api/thcard?photo=true`

หมายเหตุ: เบราว์เซอร์ต้องเปิดเว็บและ CardReader บนเครื่องเดียวกัน (หรือใช้ proxy ของเว็บไปยัง agent บนเครื่องที่มี USB)
