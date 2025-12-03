# Submit + Approve + Logs
$baseUrl = "http://localhost:5250"

Invoke-RestMethod "$baseUrl/api/timesheets/$timesheetId/submit" -Method POST -Headers $headers | Out-Null
"Timesheet submitted."

Invoke-RestMethod "$baseUrl/api/timesheets/$timesheetId/approve" -Method POST -Headers $headers | Out-Null
"Timesheet approved."

$logs = Invoke-RestMethod "$baseUrl/api/timesheets/$timesheetId/logs" -Headers $headers
"Workflow log:"
$logs | Format-Table