# PRN_Jira

Ứng dụng ASP.NET Core (Razor + Web API) kết nối Jira, quản lý project và tạo SRS (Software Requirements Specification) từ dữ liệu Jira.

---

## Yêu cầu

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (để chạy MySQL)

---

## 1. Tạo và chạy database MySQL (Docker)

Chạy container MySQL với database và user sẵn cho app:

```bash
docker run --name prn-mysql -e MYSQL_ROOT_PASSWORD=root1234 -e MYSQL_DATABASE=prn_jira_db -e MYSQL_USER=prn_user -e MYSQL_PASSWORD=prn1234 -p 3307:3306 -d mysql:8.4
```

- **Database:** `prn_jira_db`
- **User:** `prn_user` / **Password:** `prn1234`
- **Port:** `3307` (máy host) → `3306` (trong container)

Connection string mặc định trong `appsettings.json` đã dùng đúng thông tin trên. Nếu bạn đổi port/user/password khi chạy Docker, hãy sửa lại trong `PRN_Jira/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3307;Database=prn_jira_db;User=prn_user;Password=prn1234;"
}
```

Áp dụng migration (tạo bảng):

```bash
cd PRN_Jira
dotnet ef database update
```

(Nếu chưa cài EF tools: `dotnet tool install -g dotnet-ef`)

---

## 2. Lấy Jira API Token (Atlassian)

App cần **Jira Access Token** để gọi API Jira thay bạn.

1. Mở: **[https://id.atlassian.com/manage-profile/security/api-tokens](https://id.atlassian.com/manage-profile/security/api-tokens)**
2. Đăng nhập tài khoản Atlassian (email dùng cho Jira).
3. Bấm **Create API token**, đặt tên (vd: `PRN_Jira`) rồi **Create**.
4. **Sao chép token** và lưu lại (chỉ hiển thị một lần). Token này bạn sẽ nhập vào app ở bước **Jira Setup**.

---

## 3. Chạy ứng dụng

```bash
cd PRN_Jira
dotnet run
```

Truy cập:

- **Web (Razor):** https://localhost:7xxx hoặc http://localhost:5xxx (xem port in ra trong terminal)
- **Swagger API:** https://localhost:7xxx/swagger (hoặc http://localhost:5xxx/swagger)

---

## 4. Hướng dẫn sử dụng app

### 4.1. Đăng ký và đăng nhập

- Vào **Register** → tạo tài khoản (Username + Password).
- **Login** bằng Username và Password.

### 4.2. Jira Setup (bắt buộc trước khi dùng SRS)

Sau khi đăng nhập, nếu chưa cấu hình Jira, bạn sẽ được chuyển đến **Jira Setup**. Nhập:

| Trường | Mô tả |
|--------|--------|
| **Jira Base URL** | URL site Jira, ví dụ: `https://your-domain.atlassian.net` (không gõ dấu `/` ở cuối). |
| **Jira Email** | Email tài khoản Atlassian/Jira. |
| **Jira Access Token** | Token vừa tạo tại [Atlassian API tokens](https://id.atlassian.com/manage-profile/security/api-tokens). |

Bấm **Save**. Cấu hình này dùng chung cho tài khoản; mỗi **project** sau đó sẽ gắn với một Jira project cụ thể.

### 4.3. Projects – Tạo project và nhập Jira Project Id/Key

- Vào **Projects** (trên menu).
- **Create** project mới, nhập:
  - **Jira Base URL** (có thể giống Jira Setup).
  - **Jira Email** (có thể giống Jira Setup).
  - **Jira Project Id/Key** — **bắt buộc** để dùng SRS (vd: `PRN`, `DEMO` — là key project trong Jira).
- Có thể **Edit** hoặc **Delete** project từ danh sách.

Nếu chưa nhập **Jira Project Id/Key** cho bất kỳ project nào, khi vào trang **SRS** app sẽ thông báo và yêu cầu vào Projects → chỉnh sửa để nhập.

### 4.4. SRS – Xem và tạo snapshot SRS

- Vào **SRS** (trên menu).
- Chọn **Project** (project đã có Jira Project Id/Key).
- **Create snapshot**: nhập mô tả (tuỳ chọn) rồi bấm **Create snapshot** để tạo phiên bản SRS từ Jira.
- Bấm **View** ở từng version để xem nội dung SRS (Releases, Epics, User stories) và **Download SRS** (PDF) nếu cần.

---

## Tóm tắt luồng

1. **Chạy MySQL** (Docker) → **Cập nhật DB** (`dotnet ef database update`).
2. **Lấy API token** tại [id.atlassian.com/manage-profile/security/api-tokens](https://id.atlassian.com/manage-profile/security/api-tokens).
3. **Chạy app** (`dotnet run`) → **Register** → **Login**.
4. **Jira Setup**: Base URL, Email, Access Token.
5. **Projects**: tạo project, nhập **Jira Project Id/Key**.
6. **SRS**: chọn project → tạo snapshot → xem/tải PDF.
