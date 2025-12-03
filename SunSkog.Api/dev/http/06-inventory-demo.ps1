# Mini-flow skladu: založit -> přijmout -> vydat -> low-stock
$baseUrl = "http://localhost:5250"

# Založení položky
$itemBody = @{
  name = "Helma práce"
  stockKeepingUnit = "HL-001"
  serialNumber = "SN-XYZ-001"
  minStock = 1
} | ConvertTo-Json

$item = Invoke-RestMethod "$baseUrl/api/inventory/items" -Method POST -Headers $headers -ContentType "application/json" -Body $itemBody
$itemId = $item.id
"Item created: $itemId"

# Příjem 2 ks
Invoke-RestMethod "$baseUrl/api/inventory/items/$itemId/receive" -Method POST -Headers $headers -ContentType "application/json" -Body (@{ quantity = 2; note = "počáteční příjem" } | ConvertTo-Json) | Out-Null
"Item received: 2 ks"

# Výdej 1 ks (bez přiřazení konkr. usera – pro demo)
Invoke-RestMethod "$baseUrl/api/inventory/items/$itemId/issue" -Method POST -Headers $headers -ContentType "application/json" -Body (@{ quantity = 1; note = "výdej na směnu" } | ConvertTo-Json) | Out-Null
"Item issued: 1 ks"

# Low stock kontrola
$low = Invoke-RestMethod "$baseUrl/api/inventory/low-stock" -Headers $headers
"Low-stock seznam:"
$low | Format-Table