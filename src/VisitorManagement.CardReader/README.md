# โปรแกรมอ่านบัตรประชาชน

รันบนเครื่องที่เสียบเครื่องอ่าน USB (เครื่อง รปภ.) ไม่ใช่บนเซิร์ฟเวอร์เว็บ

## Windows

ดับเบิลคลิก `start-card-reader.bat` หรือ:

```bash
dotnet run --project src/VisitorManagement.CardReader
```

## Linux

```bash
chmod +x src/VisitorManagement.CardReader/start-card-reader.sh
./src/VisitorManagement.CardReader/start-card-reader.sh
```

ต้องการ: `pcscd`, `libpcsclite1`, `libccid`

ฟังที่ `http://127.0.0.1:5001`

- `GET /health`
- `GET /api/status`
- `GET /api/thcard?photo=true`

หมายเหตุ: เบราว์เซอร์ต้องเปิดเว็บและ CardReader บนเครื่องเดียวกัน (หรือ CardReader บนเครื่องที่มี USB เครื่องอ่าน) เพราะ agent ฟังที่ localhost เท่านั้น
