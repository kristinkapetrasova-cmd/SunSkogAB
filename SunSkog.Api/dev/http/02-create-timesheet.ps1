# Vytvoření timesheetu (Admin jako zaměstnanec pro demo)
$baseUrl = "http://localhost:5250"

$createTsBody = @{
  periodStart = "2025-10-01"
  periodEnd   = "2025-10-15"
  notes       = "Dev test výkaz"
} | ConvertTo-Json

$created = Invoke-RestMethod "$baseUrl/api/timesheets" -Method POST -Headers $headers -ContentType "application/json" -Body $createTsBody
$global:timesheetId = $created.id

"Timesheet vytvořen: $timesheetId"