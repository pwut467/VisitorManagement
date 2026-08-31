# โปรแกรมอ่านบัตรประชาชน

รันบนเครื่องที่เสียบเครื่องอ่าน USB (เครื่อง รปภ.) ไม่ใช่บนเซิร์ฟเวอร์เว็บ

## Windows (System Tray)

ดับเบิลคลิก `start-card-reader.bat` หรือ:

```bash
dotnet run -f net8.0-windows --project src/VisitorManagement.CardReader
```

โปรแกรมจะ**ย่อลง System Tray** (ไม่มีหน้าต่าง console)

- คลิกขวาไอคอน → สถานะเครื่องอ่าน / เปิดหน้า Health / ออกจากโปรแกรม
- ดับเบิลคลิกไอคอน → ดูสถานะ
- รันซ้ำถ้าเปิดอยู่แล้ว จะแจ้งว่ากำลังทำงานอยู่

## Linux (console)

```bash
chmod +x src/VisitorManagement.CardReader/start-card-reader.sh
./src/VisitorManagement.CardReader/start-card-reader.sh
```

ต้องการ: `pcscd`, `libpcsclite1`, `libccid`

```bash
dotnet run -f net8.0 --project src/VisitorManagement.CardReader
```

ฟังที่ `http://127.0.0.1:5001`

- `GET /health`
- `GET /api/status`
- `GET /api/thcard?photo=true`

หมายเหตุ: เบราว์เซอร์ต้องเปิดเว็บและ CardReader บนเครื่องเดียวกัน (หรือ CardReader บนเครื่องที่มี USB เครื่องอ่าน) เพราะ agent ฟังที่ localhost เท่านั้น
