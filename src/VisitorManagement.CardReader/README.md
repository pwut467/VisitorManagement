# โปรแกรมอ่านบัตรประชาชน

รันบนเครื่องที่เสียบเครื่องอ่าน USB (เครื่อง รปภ.) ไม่ใช่บนเซิร์ฟเวอร์เว็บ

```bash
dotnet run --project src/VisitorManagement.CardReader
```

ฟังที่ `http://127.0.0.1:5001`

ต้องการ:

- เครื่องอ่านมาตรฐาน PC/SC
- Windows: บริการ Smart Card + ไดรเวอร์เครื่องอ่าน
- Linux: `pcscd` และ `libpcsclite1`
