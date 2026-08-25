# ระบบ Visitor — ลงทะเบียนผู้มาติดต่อบริษัท

เว็บแอป **ASP.NET Core 8 MVC + SQL Server + Bootstrap 5** สำหรับ รปภ. / แผนกต้อนรับ ใช้ลงทะเบียนผู้มาติดต่อ ถ่ายรูป ออกบัตร QR แล้ว Check-out เมื่อคืนบัตร

## ภาพรวม Flow (ที่ระบุ + ส่วนที่เติม)

```
ผู้มาติดต่อ
    │
    ├── (ทางเลือก) พนักงาน Host ลงทะเบียนล่วงหน้า
    ▼
รปภ. ลงทะเบียน Visitor
    - อ่านบัตรประชาชน / พิมพ์มือ / ดึงประวัติผู้เคยมา
    - ตรวจบัญชีดำ + checksum เลขบัตร 13 หลัก
    - ถ่ายรูปกล้องเว็บ
    - ระบุ Host, วัตถุประสงค์, จุดเข้า, รถ, สิ่งของ, ผู้ติดตาม
    - รับความยินยอม PDPA
    - บันทึกเวลาเข้า
    - สร้าง QR แล้วพิมพ์บัตรบน Thermal Printer (80mm)
    │
    ▼
เข้าพื้นที่  (รายการ “ผู้ที่อยู่ในพื้นที่” + แจ้งเตือนเกินเวลา)
    │
    ▼
Check-out
    └── คืนบัตร → รปภ. สแกน QR / พิมพ์รหัส → บันทึกเวลาออก
```

### ส่วนที่ขาดจาก Flow เดิม แล้วเพิ่มในระบบนี้

| หัวข้อ | ทำไมต้องมี |
|---|---|
| **Host / แผนกที่มาหา** | รู้ว่ามาหาใคร ติดต่อกลับได้ และทำรายงานตามแผนก |
| **ประเภทผู้มาติดต่อ + วัตถุประสงค์** | แยก ลูกค้า / ผู้รับเหมา / ส่งของ / สัมภาษณ์ และกำหนด escort |
| **ลงทะเบียนล่วงหน้า** | พนักงานนัดหมายไว้ รปภ. ทำ Check-in เร็วขึ้น |
| **บัญชีดำ** | ปฏิเสธคนที่ไม่พึงประสงค์ตอนลงทะเบียน |
| **PDPA + หน้า Privacy** | เลขบัตรและรูปเป็นข้อมูลอ่อนไหว ต้องมี consent |
| **ผู้มาติดต่อซ้ำ** | กรอกเลขบัตรแล้วดึงชื่อ/บริษัท/โทรอัตโนมัติ |
| **ยานพาหนะ + ของนำเข้า + ผู้ติดตาม** | งาน รปภ. ใช้ตอนตรวจและสอบสวนเหตุ |
| **หลายจุดเข้า-ออก (Gate)** | รองรับประตูใหญ่ / พนักงาน / ขนส่ง |
| **ผู้ที่อยู่ในพื้นที่ + เกินเวลา** | เห็นคนที่ยังไม่คืนบัตร และรายการค้างคืน |
| **พิมพ์บัตรซ้ำ / ยกเลิก** | บัตรเสียหรือนัดหมายถูกยกเลิก |
| **สิทธิ์ผู้ใช้** | Admin, รปภ. (Security), Reception, Host |
| **รายงาน / ส่งออก Excel** | สรุปรายวันให้ฝ่ายบุคคลหรือความปลอดภัย |
| **Audit log** | ใครเป็นคน Check-in / Check-out |

ส่วนที่ออกแบบไว้ต่อได้: QZ Tray/ESC-POS ยิงพิมพ์ตรง, แจ้งเตือน Host ทางอีเมล/ไลน์, Kiosk ลงทะเบียนเอง, หลายสาขา

## สิทธิ์ผู้ใช้

| Role | ความสามารถ |
|---|---|
| **Security / Reception** | Check-in, พิมพ์บัตร, Check-out, ดูคนในพื้นที่, รายงาน, ดูบัญชีดำ |
| **Host** | ลงทะเบียนล่วงหน้าให้แขกของตัวเอง |
| **Admin** | ทั้งหมด + พนักงาน, ข้อมูลหลัก, ผู้ใช้, ตั้งค่าบริษัท, จัดการบัญชีดำ |

## โครงสร้างข้อมูลหลัก

- **Visitor** — โปรไฟล์คน (เลขบัตรไม่ซ้ำ) ใช้ซ้ำได้ทุกครั้งที่มา
- **Visit** — แต่ละรอบเข้า-ออก (`VyyyyMMdd-0001`) สถานะ PreRegistered / CheckedIn / CheckedOut / Cancelled / Denied
- **Employee, Department, Gate, VisitorType, VisitPurpose** — ข้อมูลหลัก
- **BlacklistEntry, AuditLog, CompanyProfile** — ความปลอดภัยและการตั้งค่าบัตร

QR บนบัตรมี payload `VISIT|{VisitCode}` เพื่อให้ทั้งกล้องและเครื่องสแกน USB (keyboard wedge) ใช้ Check-out ได้

## เทคโนโลยี

- ASP.NET Core 8 MVC, Identity (cookie)
- EF Core + **SQL Server Express** (ฐานข้อมูล `VisitorManagment`)
- Bootstrap 5 + IBM Plex Sans Thai
- QRCoder, กล้องเบราว์เซอร์ (`getUserMedia`), html5-qrcode

## ฐานข้อมูล SQL Server Express

ค่าเริ่มต้นชี้ไปที่ instance **SQLEXPRESS** ชื่อฐาน **VisitorManagment**

```
Server=.\SQLEXPRESS;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

รอบแรกที่เปิดแอป ระบบจะ:

1. `Database.Migrate()` — สร้างฐาน `VisitorManagment` และตารางทั้งหมดบน SQL Server Express
2. seed ข้อมูลหลัก (แผนก, พนักงาน, ประตู, ประเภทผู้มาติดต่อ, ผู้ใช้ทดลอง, รายการเข้าพบตัวอย่าง)

หรือรัน migrate เองก่อนเปิดเว็บ:

```bash
dotnet tool restore
dotnet ef database update --project src/VisitorManagement.Web
```

ถ้าใช้ SQL Authentication แทน Windows Auth ให้เปลี่ยน connection string เป็น:

```
Server=.\SQLEXPRESS;Database=VisitorManagment;User Id=sa;Password=รหัสผ่านของคุณ;TrustServerCertificate=True;MultipleActiveResultSets=True
```

สคริปต์ SQL สำรองอยู่ที่ `src/VisitorManagement.Web/Data/Migrations/VisitorManagment.sql` (เปิดใน SSMS แล้วรันบน SQLEXPRESS ได้)

### Docker (edition Express)

```bash
docker compose up -d
```

แล้วตั้ง connection string เป็น:

```
Server=localhost,1433;Database=VisitorManagment;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=True
```

## วิธีรัน

```bash
cd src/VisitorManagement.Web
dotnet run
```

เปิด `http://localhost:5088`

ต้องมี SQL Server Express รันอยู่ และบัญชี Windows (หรือ sa) มีสิทธิ์สร้างฐานข้อมูล

### บัญชีเริ่มต้น

| ชื่อผู้ใช้ | รหัสผ่าน | สิทธิ์ |
|---|---|---|
| `SKAdmin` | `123456` | Admin |
| `9641` | `123456` | Security |

เปิดแอปครั้งแรกจะล้างผู้ใช้เดิมทั้งหมด แล้วสร้างสองบัญชีนี้ (รหัสผ่านทุกบัญชีเป็น `123456`)

## การพิมพ์บัตร Thermal

หน้า `/Visits/Badge/{id}` จัดหน้ากระดาษ **80mm** (`@page size: 80mm auto`)

เนื้อหาบัตร: เลขที่, วันที่เวลาเข้า, ชื่อผู้มาติดต่อ, หน่วยงาน, วัตถุประสงค์, ผู้ต้องการพบ, ช่องลงชื่อผู้รับการติดต่อ / เจ้าหน้าที่ ร.ป.ภ. และบาร์โค้ดเลขที่บัตร

1. หลัง Check-in ระบบเปิดหน้าบัตรและสั่ง `window.print()`
2. ในไดอะล็อกพิมพ์ เลือกเครื่อง Thermal แล้วปิด header/footer ของเบราว์เซอร์
3. พิมพ์ซ้ำได้จากหน้าบัตร, รายละเอียด, ประวัติการเข้าพบ หรือรายการคนในพื้นที่

ถ้าต้องการยิง ESC/POS ตรงเครื่องโดยไม่ผ่านไดอะล็อกเบราว์เซอร์ ให้ต่อด้วย print agent (เช่น QZ Tray) ในเฟสถัดไป

## เครื่องอ่านบัตรประชาชน (ของจริง)

เบราว์เซอร์อ่านเครื่อง USB Smart Card ไม่ได้โดยตรง ต้องเปิด **โปรแกรมอ่านบัตรบนเครื่อง รปภ.** ที่เสียบเครื่องอ่าน

1. เสียบเครื่องอ่านบัตรประชาชน (มาตรฐาน PC/SC เช่น ACS, CREATOR, Gemalto)
2. Windows: เปิดบริการ **Smart Card** และติดตั้งไดรเวอร์เครื่องอ่าน  
   Linux: ติดตั้ง `pcscd` / `libpcsclite`
3. บนเครื่องนั้นรัน:

```bash
dotnet run --project src/VisitorManagement.CardReader
```

หรือดับเบิลคลิก `src/VisitorManagement.CardReader/start-card-reader.bat`

โปรแกรมจะฟังที่ `http://127.0.0.1:5001`

4. เปิดหน้า Check-in ในเบราว์เซอร์บนเครื่องเดียวกัน
5. เสียบบัตร แล้วกด **อ่านบัตรประชาชน**

ระบบจะอ่านผ่าน APDU ของบัตรประชาชนไทย: เลขบัตร 13 หลัก, คำนำหน้า/ชื่อ/นามสกุล (TIS-620), ที่อยู่, วันเกิด, เพศ, รูป JPEG บนบัตร

สถานะบนหน้า Check-in:

- **ยังไม่เปิดโปรแกรมอ่านบัตร** — ยังไม่ได้รัน CardReader
- **ไม่พบเครื่องอ่าน** — ยังไม่ได้เสียบ USB reader
- **พร้อมอ่าน — ยังไม่มีบัตร** — เสียบบัตรได้เลย
- **พบบัตรในเครื่องอ่าน** — กดอ่านได้

ถ้ายังไม่มีบัตร ปุ่มอ่านจะรอสูงสุด 30 วินาทีให้เสียบบัตร ไม่มีการเติมข้อมูลจำลอง

API ของโปรแกรมอ่านบัตร:

- `GET /api/status` — รายชื่อเครื่องอ่าน และว่ามีบัตรหรือไม่
- `GET /api/thcard?photo=true` — อ่านบัตรจริง คืน JSON + รูป base64

## ทดสอบ

```bash
dotnet test
```

ครอบคลุม checksum บัตรประชาชน, เลข Visit รายวัน, Check-in/Check-out, บัญชีดำ, PDPA, ผู้มาซ้ำ และหน้า Login

## โครงสร้างโปรเจกต์

```
src/VisitorManagement.Web              เว็บ MVC
src/VisitorManagement.CardReader       โปรแกรมอ่านบัตรบนเครื่อง รปภ. (PC/SC)
src/VisitorManagement.CardReader.Core  APDU + ถอดรหัส TIS-620
tests/VisitorManagement.Web.Tests
docker-compose.yml                     SQL Server 2022
```
