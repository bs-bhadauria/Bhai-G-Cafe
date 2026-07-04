param(
  [Parameter(Mandatory = $true)]
  [string]$WebhookSecret,
  [string]$OrderId = "BG-LOCAL001",
  [string]$PaymentId = "pay_test_123",
  [string]$ApiBaseUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

$payload = @{
  event = "payment.captured"
  payload = @{
    payment = @{
      entity = @{
        id = $PaymentId
        notes = @{
          publicOrderId = $OrderId
        }
      }
    }
    order = @{
      entity = @{
        id = "order_test_123"
        notes = @{
          publicOrderId = $OrderId
        }
      }
    }
  }
} | ConvertTo-Json -Depth 10 -Compress

$hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($WebhookSecret))
$signatureBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($payload))
$signature = ([System.BitConverter]::ToString($signatureBytes)).Replace("-", "").ToLowerInvariant()

Invoke-WebRequest `
  -UseBasicParsing `
  -Method Post `
  -Uri "$ApiBaseUrl/api/payments/webhooks/razorpay" `
  -Headers @{ "X-Razorpay-Signature" = $signature } `
  -ContentType "application/json" `
  -Body $payload
