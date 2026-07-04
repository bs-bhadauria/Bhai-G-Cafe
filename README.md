# Bhai G & Cafe

Restaurant ordering project with:

- storefront frontend in [`index.html`](/C:/Users/Bhoopendra%20Singh/Projects/Bhai-G-Cafe-Restaurant/index.html)
- secure ASP.NET Core backend in [`server/`](/C:/Users/Bhoopendra%20Singh/Projects/Bhai-G-Cafe-Restaurant/server)
- admin dashboard in [`admin/index.html`](/C:/Users/Bhoopendra%20Singh/Projects/Bhai-G-Cafe-Restaurant/admin/index.html)

## Current Backend Status

Implemented:

- menu API and order API
- PostgreSQL order store integration
- SQL migration runner
- legacy JSON-to-PostgreSQL import path
- JSON-backed fallback persistence when Postgres is not configured
- COD and online order creation flow
- Razorpay order creation scaffold
- Razorpay webhook signature verification
- email/SMS notification provider hooks
- admin stats, order list, and status updates
- frontend public-config awareness so online payment is only enabled when backend keys exist

## Project Structure

```text
Bhai-G-Cafe-Restaurant/
|-- index.html
|-- assets/
|   |-- app.js
|   |-- menu.json
|   `-- styles.css
|-- admin/
|   `-- index.html
|-- scripts/
|   |-- start-backend.ps1
|   |-- test-webhook.ps1
|   `-- verify-phase2.ps1
`-- server/
    |-- BhaiGCafe.Api.csproj
    |-- Database/
    |-- Program.cs
    |-- appsettings.json
    |-- appsettings.Development.json
    |-- appsettings.Local.example.json
    |-- Models/
    |-- Options/
    |-- Properties/
    `-- Services/
```

## Run Locally

Frontend:

```powershell
python -m http.server 5500
```

Open:

```text
http://127.0.0.1:5500
```

Backend:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start-backend.ps1
```

Default backend URL:

```text
http://localhost:8080
```

If needed, override the frontend API base before [`assets/app.js`](/C:/Users/Bhoopendra%20Singh/Projects/Bhai-G-Cafe-Restaurant/assets/app.js):

```html
<script>
  window.BHAI_G_CAFE_API_BASE = "http://localhost:8080";
</script>
```

## Local Config

Use [`server/appsettings.Local.example.json`](/C:/Users/Bhoopendra%20Singh/Projects/Bhai-G-Cafe-Restaurant/server/appsettings.Local.example.json) as the template for your private local settings.

Actual local file:

- [`server/appsettings.Local.json`](/C:/Users/Bhoopendra%20Singh/Projects/Bhai-G-Cafe-Restaurant/server/appsettings.Local.json)
- this file is git-ignored
- keep real secrets only in this file or environment variables

Fields you will normally update:

- `ConnectionStrings:Postgres`
- `Database:ImportLegacyJsonOnStartup`
- `Payments:Razorpay:KeyId`
- `Payments:Razorpay:KeySecret`
- `Payments:Razorpay:WebhookSecret`
- `Notifications:Email:*`
- `Notifications:Sms:*`

PostgreSQL behavior:

- if `ConnectionStrings:Postgres` is blank, backend safely falls back to JSON file storage
- if `ConnectionStrings:Postgres` is set, backend switches to PostgreSQL automatically
- if `Database:AutoMigrateOnStartup = true`, SQL migrations run during startup
- if `Database:ImportLegacyJsonOnStartup = true`, legacy JSON orders are imported once when the database is empty

Notification auth is configurable:

- `AuthHeaderName = "Authorization"` with `AuthScheme = "Bearer"` for standard bearer APIs
- or set a provider-specific header name such as `x-api-key`

## Payment Flow

COD:

- frontend posts order to `POST /api/orders`
- backend saves order immediately

Online:

- frontend posts order to `POST /api/orders`
- backend creates Razorpay order if keys are configured
- frontend opens hosted Razorpay checkout
- frontend posts Razorpay success data to `POST /api/payments/razorpay/verify`
- webhook hits `POST /api/payments/webhooks/razorpay`
- backend verifies signature and marks order paid

Frontend safety behavior:

- if Razorpay keys are missing, Card / UPI / Wallet methods are disabled automatically
- local card fields are never sent to the backend
- payment secrets remain server-side only

## Admin Dashboard

Open:

```text
http://127.0.0.1:5500/admin/index.html
```

Admin APIs:

- `GET /api/admin/stats`
- `GET /api/admin/orders`
- `PATCH /api/admin/orders/{orderId}/status`

Required header:

```text
X-Admin-Key: <your-admin-key>
```

## Verification

Build verification:

```powershell
cd server
dotnet build
```

Current fallback verification:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify-phase2.ps1
```

If Razorpay test keys are configured and you want the script to simulate a successful server-side verification:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify-phase2.ps1 -RazorpayKeySecret "<your-test-key-secret>"
```

PostgreSQL activation check:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify-postgres.ps1
```

What this currently verifies:

- API starts
- health endpoint responds
- menu endpoint responds
- order creation works
- webhook verification works
- admin endpoints work

When PostgreSQL is configured, the same API flow will use DB-backed persistence instead of JSON fallback.

Local webhook verification:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-webhook.ps1 -WebhookSecret "dev-webhook-secret" -OrderId "BG-XXXXXXX"
```

## Verified On This Machine

Already verified:

- `.NET 8 SDK` available
- backend restore/build succeeds
- PostgreSQL package restore succeeds
- `GET /api/health`
- `GET /api/menu`
- `GET /api/config/public`
- `POST /api/orders` for COD
- `POST /api/orders` for online flow
- local webhook signature verification
- admin stats/orders endpoints

Current observed storage on this machine:

- `json-file`
- reason: local PostgreSQL connection string is still blank

## To Enable PostgreSQL

1. Install PostgreSQL locally or use a hosted PostgreSQL instance.
2. Open [`server/appsettings.Local.json`](/C:/Users/Bhoopendra%20Singh/Projects/Bhai-G-Cafe-Restaurant/server/appsettings.Local.json).
3. Set `ConnectionStrings.Postgres`.
4. Optionally set `Database.ImportLegacyJsonOnStartup` to `true` for the first startup if you want existing JSON orders imported.
5. Start the backend again.
6. Check `GET /api/health` and confirm `storage.provider` becomes `postgres`.

Example connection string:

```text
Host=localhost;Port=5432;Database=bhaigcafe;Username=postgres;Password=your-strong-password;SSL Mode=Prefer;Trust Server Certificate=true
```

## Important Security Notes

- do not put Razorpay secret keys in frontend code
- do not commit real `appsettings.Local.json`
- do not commit real PostgreSQL passwords
- keep webhook secret server-side only
- admin dashboard is still API-key based, so production should move to proper auth
- current notification hooks are provider-agnostic HTTP integrations and should be pointed only to trusted APIs
