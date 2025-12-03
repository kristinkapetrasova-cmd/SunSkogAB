# Přidání jedné položky do výkazu
$baseUrl = "http://localhost:5250"

$entryBody = @{
  workDate  = "2025-10-10"
  project   = "Projekt A"
  task      = "Tezba"
  hours     = 7.5
  km        = 12
  pieces    = 0
  hourRate  = 200
  kmRate    = 8
  pieceRate = 0
  comment   = "Test z API"
} | ConvertTo-Json

$entry = Invoke-RestMethod "$baseUrl/api/timesheets/$timesheetId/entries" -Method POST -Headers $headers -ContentType "application/json" -Body $entryBody
"Entry přidán: $($entry.id)"

# Volitelně zkontroluj detail výkazu
$ts = Invoke-RestMethod "$baseUrl/api/timesheets/$timesheetId" -Headers $headers
$ts | Format-List