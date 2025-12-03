# Stáhne CSV (souhrn i detail) do aktuální složky
$baseUrl = "http://localhost:5250"
$from = "2025-10-01"
$to   = "2025-10-31"

Invoke-WebRequest "$baseUrl/api/admin/export/timesheets.csv?from=$from&to=$to" -Headers $headers -OutFile ".\export_timesheets_oct.csv"
"Uloženo: export_timesheets_oct.csv"

Invoke-WebRequest "$baseUrl/api/admin/export/timesheets-details.csv?from=$from&to=$to" -Headers $headers -OutFile ".\export_timesheets_oct_details.csv"
"Uloženo: export_timesheets_oct_details.csv"