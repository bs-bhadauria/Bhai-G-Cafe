param(
  [string]$ApiBaseUrl = "http://localhost:8080",
  [string]$AdminKey = "dev-admin-key",
  [string]$WebhookSecret = "",
  [string]$RazorpayKeySecret = ""
)

$ErrorActionPreference = "Stop"

Write-Host "Health:"
(Invoke-WebRequest -UseBasicParsing "$ApiBaseUrl/api/health").Content

Write-Host "`nMenu count:"
(((Invoke-WebRequest -UseBasicParsing "$ApiBaseUrl/api/menu").Content | ConvertFrom-Json).Count)

$codBody = @{
  customer = @{
    fullName = "Phase2 Verify"
    email = "verify-cod@example.com"
    phone = "+919876543210"
    deliveryType = "delivery"
    address = "Kasganj Uttar Pradesh 207123"
    tableNumber = ""
    specialInstructions = "No onion"
  }
  items = @(
    @{ menuItemId = "smoked-burrata"; quantity = 1 }
  )
  paymentMethod = "cod"
  currency = "INR"
} | ConvertTo-Json -Depth 6

Write-Host "`nCOD order:"
$codOrder = (Invoke-WebRequest -UseBasicParsing "$ApiBaseUrl/api/orders" -Method Post -ContentType "application/json" -Body $codBody).Content | ConvertFrom-Json
$codOrder | ConvertTo-Json -Depth 6

$onlineBody = @{
  customer = @{
    fullName = "Phase2 Verify"
    email = "verify-online@example.com"
    phone = "+919876543210"
    deliveryType = "takeaway"
    address = ""
    tableNumber = ""
    specialInstructions = ""
  }
  items = @(
    @{ menuItemId = "lobster-risotto"; quantity = 1 }
  )
  paymentMethod = "online"
  currency = "INR"
} | ConvertTo-Json -Depth 6

Write-Host "`nOnline order:"
$onlineOrder = (Invoke-WebRequest -UseBasicParsing "$ApiBaseUrl/api/orders" -Method Post -ContentType "application/json" -Body $onlineBody).Content | ConvertFrom-Json
$onlineOrder | ConvertTo-Json -Depth 6

if ($RazorpayKeySecret -and $onlineOrder.paymentGateway -and $onlineOrder.paymentGateway.providerOrderId) {
  $paymentId = "pay_verify_local"
  $signaturePayload = "$($onlineOrder.paymentGateway.providerOrderId)|$paymentId"
  $hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($RazorpayKeySecret))
  $signatureBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($signaturePayload))
  $signature = ([System.BitConverter]::ToString($signatureBytes)).Replace("-", "").ToLowerInvariant()

  $verifyBody = @{
    publicOrderId = $onlineOrder.orderId
    providerOrderId = $onlineOrder.paymentGateway.providerOrderId
    providerPaymentId = $paymentId
    signature = $signature
  } | ConvertTo-Json

  Write-Host "`nDirect payment verification:"
  (Invoke-WebRequest -UseBasicParsing "$ApiBaseUrl/api/payments/razorpay/verify" -Method Post -ContentType "application/json" -Body $verifyBody).Content
}
elseif ($WebhookSecret) {
  Write-Host "`nWebhook test:"
  powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "test-webhook.ps1") -WebhookSecret $WebhookSecret -OrderId $onlineOrder.orderId -PaymentId "pay_verify_local" -ApiBaseUrl $ApiBaseUrl | Out-Null
  Write-Host "Webhook accepted for $($onlineOrder.orderId)"
}
else {
  Write-Host "`nPayment confirmation step skipped. Pass -RazorpayKeySecret or -WebhookSecret to verify paid status."
}

Write-Host "`nAdmin stats:"
(Invoke-WebRequest -UseBasicParsing "$ApiBaseUrl/api/admin/stats" -Headers @{ "X-Admin-Key" = $AdminKey }).Content
