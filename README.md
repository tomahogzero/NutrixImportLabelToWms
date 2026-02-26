# Nutrix Sync Label to WMS

แอปเดสก์ท็อป WPF (.NET Framework 4.7.2) สำหรับสแกน QR จากฉลาก, parse ข้อมูล, validate กับ Epicor BAQ OData (Basic Auth + headers), และบันทึกผลลง SQL Server พร้อมหน้าประวัติการสแกน

## โครงสร้างโปรเจกต์

- `NutrixSyncLabelToWms.sln`
- `src/NutrixSyncLabelToWms.App` (WPF + MVVM + MahApps.Metro)
- `src/NutrixSyncLabelToWms.Core` (Models/Interfaces)
- `src/NutrixSyncLabelToWms.Infrastructure` (Epicor client, SQL repository, parser, settings store)
- `sql/create_tables.sql`

## ความสามารถหลัก

1. **Scan Page**
   - ช่อง `QR Input` สำหรับ scanner keyboard wedge หรือ paste ข้อความ
   - ปุ่ม `Parse` / `Validate` / `Save`
   - แสดงฟิลด์ parse: `PartNum`, `LotNum`, `Qty`, `UOM`, `ExpireDate`, `ReceiveDate`
   - แสดงผล validate: `Matched rows`, `Label Qty vs OnHand Qty`, สถานะ `PASS/FAIL`

2. **History Page**
   - DataGrid แสดงรายการบันทึกย้อนหลัง (ใหม่สุดก่อน)
   - Filter: วันที่, PartNum, LotNum, Status (PASS/FAIL)

3. **Settings Page**
   - Epicor settings: BaseUrl, Company, Plant, WarehouseCode, XApiKey, Username, Password
   - SQL ConnectionString
   - ปุ่ม Save/Reload/Test Epicor/Test SQL
   - เก็บ user settings ที่ `%AppData%/NutrixSyncLabelToWms/user-settings.json`

## Epicor BAQ ที่รองรับ

เรียก endpoint:

`GET {BaseUrl}/api/v2/odata/{Company}/BaqSvc/NTXzPartLotOnHand/Data?$top=200&$count=true&$filter=...`

ส่ง headers:

- `Accept: application/json`
- `X-API-Key: {XApiKey}`
- `CallSettings: {"Company":"...","Plant":"..."}`
- `Authorization: Basic base64(username:password)`

filter ตัวอย่าง:

- `PartBin_PartNum eq 'RM11-O-PTM-001' and PartBin_WarehouseCode eq 'PDO11'`

## Validation Rules

- ใช้ `PartNum + LotNum` จาก QR เทียบกับ BAQ response
- ไม่พบ => `FAIL (NOT_FOUND)`
- พบ => sum `PartBin_OnhandQty` เป็น `QtyOnHand`
  - ถ้า `QtyOnHand >= QtyLabel` => `PASS`
  - ไม่พอ => `FAIL (INSUFFICIENT_QTY)`

## QR Parsing Strategy

เรียงลำดับ parser:

1. JSON
2. key=value หรือ key:value แยกด้วย `; | newline`
3. regex fallback (Part/Lot/Qty/UOM)

## การใช้งานใน Visual Studio 2022 (.NET Framework 4.7.2)

1. เปิดไฟล์ `NutrixSyncLabelToWms.sln`
2. Restore NuGet packages
3. ตั้ง `NutrixSyncLabelToWms.App` เป็น Startup Project
4. Build และ Run
5. ไปหน้า **Settings** แล้วตั้งค่า Epicor + SQL
6. กด **Test Epicor** และ **Test SQL**
7. ไปหน้า **Scan** แล้วสแกน/วาง QR, Parse, Validate, Save
8. ไปหน้า **History** เพื่อตรวจสอบรายการย้อนหลัง

## หมายเหตุ

- ห้าม hardcode secrets ใน source code
- หาก SQL ไม่มีตาราง ระบบจะสร้าง `ScanTransactions` อัตโนมัติเมื่อกด Save
- สามารถใช้ไฟล์ `sql/create_tables.sql` สำหรับสร้างตารางล่วงหน้าได้
