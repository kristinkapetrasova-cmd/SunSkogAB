# Vytvoření týmu + přidání člena + vypsání členů
$baseUrl = "http://localhost:5250"

$users = Invoke-RestMethod "$baseUrl/api/admin/teams/users" -Headers $headers
$users | Format-Table

# Vytvoř tým (lead je admin)
$leadId = ($users | Where-Object { $_.email -eq "admin@sunskog.local" }).id
$teamBody = @{ name = "Parta A"; leadUserId = $leadId } | ConvertTo-Json
$team = Invoke-RestMethod "$baseUrl/api/admin/teams/" -Method POST -Headers $headers -ContentType "application/json" -Body $teamBody
$teamId = $team.id
"Team created: $($team.name) ($teamId)"

# Přidej člena (test user)
$memberId = ($users | Where-Object { $_.email -eq "user@sunskog.local" }).id
$memberBody = @{ userId = $memberId; role = "Member"; from = "2025-10-01" } | ConvertTo-Json
$added = Invoke-RestMethod "$baseUrl/api/admin/teams/$teamId/members" -Method POST -Headers $headers -ContentType "application/json" -Body $memberBody
"Member added: $($added.userId)"

# Vypiš členy
$members = Invoke-RestMethod "$baseUrl/api/admin/teams/$teamId/members" -Headers $headers
$members | Format-Table