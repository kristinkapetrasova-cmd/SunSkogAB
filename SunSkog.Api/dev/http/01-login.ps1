# Přihlášení jako Admin a uložení tokenu/hlaviček do session
$baseUrl = "http://localhost:5250"

# Volitelně obnov admina (Dev)
try {
  Invoke-RestMethod "$baseUrl/dev/reset-admin" -Method POST | Out-Null
} catch {}

$body = @{ email = "admin@sunskog.local"; password = "Admin123!" } | ConvertTo-Json
$login = Invoke-RestMethod "$baseUrl/auth/login" -Method POST -ContentType "application/json" -Body $body

$global:token = $login.token
$global:headers = @{ Authorization = "Bearer $token" }

"Token získán. Zkuste: Invoke-RestMethod $baseUrl/api/secret -Headers `$headers"