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
| **รายงาน / ส่งออก CSV** | สรุปรายวันให้ฝ่ายบุคคลหรือความปลอดภัย |
| **Audit log** | ใครเป็นคน Check-in / Check-out |

ส่วนที่ออกแบบไว้ต่อได้ (ยังไม่ทำในเวอร์ชันนี้): เชื่อมเครื่องอ่านบัตรจริงผ่าน agent ที่ `127.0.0.1:5001`, QZ Tray/ESC-POS ยิงพิมพ์ตรง, แจ้งเตือน Host ทางอีเมล/ไลน์, Kiosk ลงทะเบียนเอง, หลายสาขา

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
- EF Core + **SQL Server** (โปรดักชัน) หรือ **SQLite** (Development)
- Bootstrap 5 + IBM Plex Sans Thai
- QRCoder, กล้องเบราว์เซอร์ (`getUserMedia`), html5-qrcode

## วิธีรัน

### 1) Development (SQLite ไม่ต้องมี SQL Server)

```bash
cd src/VisitorManagement.Web
dotnet run
```

เปิด `http://localhost:5088`

`appsettings.Development.json` ตั้ง `Database:Provider` เป็น `Sqlite`

### 2) SQL Server ด้วย Docker

```bash
docker compose up -d
```

แล้วตั้งใน `appsettings.json`:

```json
"Database": { "Provider": "SqlServer" },
"ConnectionStrings": {
  "SqlServer": "Server=localhost,1433;Database=VisitorManagement;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=True"
}
```

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/VisitorManagement.Web
```

รอบแรกระบบใช้ `EnsureCreated` สร้าง schema และ seed ข้อมูลตัวอย่าง

### บัญชีเริ่มต้น

| อีเมล | รหัสผ่าน | สิทธิ์ |
|---|---|---|
| `admin@company.local` | `Admin@12345` | Admin |
| `security@company.local` | `Security@12345` | รปภ. |
| `reception@company.local` | `Reception@12345` | Reception |
| `host@company.local` | `Host@12345` | Host |

**ควรเปลี่ยนรหัสผ่านทันทีในงานจริง**

## การพิมพ์บัตร Thermal

หน้า `/Visits/Badge/{id}` จัดหน้ากระดาษ **80mm** (`@page size: 80mm auto`)

1. หลัง Check-in ระบบเปิดหน้าบัตรและสั่ง `window.print()`
2. ในไดอะล็อกพิมพ์ เลือกเครื่อง Thermal แล้วปิด header/footer ของเบราว์เซอร์
3. พิมพ์ซ้ำได้จากหน้ารายละเอียดหรือรายการคนในพื้นที่

ถ้าต้องการยิง ESC/POS ตรงเครื่องโดยไม่ผ่านไดอะล็อกเบราว์เซอร์ ให้ต่อด้วย print agent (เช่น QZ Tray) ในเฟสถัดไป

## เครื่องอ่านบัตรประชาชน

ปุ่ม **อ่านบัตรประชาชน** จะเรียก `http://127.0.0.1:5001/api/thcard` ก่อน (agent บนเครื่อง รปภ. ที่ต่อ USB reader)

ถ้าไม่มี agent ระบบจะเติมข้อมูลตัวอย่างเพื่อสาธิต แล้วยังกรอกมือหรือค้นหาผู้เคยมาจากเลขบัตรได้

รูปแบบ JSON ที่ agent ควรคืน:

```json
{
  "nationalId": "3101700123452",
  "title": "นาย",
  "firstName": "ชื่อ",
  "lastName": "นามสกุล",
  "address": "ที่อยู่",
  "dateOfBirth": "1988-05-12"
}
```

## ทดสอบ

```bash
dotnet test
```

ครอบคลุม checksum บัตรประชาชน, เลข Visit รายวัน, Check-in/Check-out, บัญชีดำ, PDPA, ผู้มาซ้ำ และหน้า Login

## โครงสร้างโปรเจกต์

```
src/VisitorManagement.Web     เว็บ MVC
tests/VisitorManagement.Web.Tests
docker-compose.yml            SQL Server 2022
```
