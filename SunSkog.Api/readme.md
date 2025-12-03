# SunSkog API — funkční prototyp (Timesheety, Workflow, Exporty, Reporty, Sklad, Týmy)

## 1) Rychlý start

### SQL Server (Docker)
```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Strong!Passw0rd" -p 1433:1433 --name sunskog_sqldata -d mcr.microsoft.com/mssql/server:2022-latest

# SunSkog AB – Interní systém pro výkazy a sklad

Tento projekt je interní webová aplikace pro společnost **SunSkog AB**, sloužící k evidenci pracovních výkazů zaměstnanců a správě interního skladu vybavení.

---

## Architektura

- **Frontend:** React + TypeScript + Tailwind + React Router  
- **Backend API:** ASP.NET Core (.NET 8 Minimal API)  
- **Databáze:** SQL Server 2022 (Docker kontejner)  
- **ORM:** Entity Framework Core  
- **Autentizace:** Identity + JWT Bearer Tokens  
- **RBAC:** Role-based access control (uživatelské role a politiky)
- **Lokalizace:** CZ / EN – připravené pro vícejazyčné UI

---

## Spuštění projektu

### Backend (API)
```bash
cd SunSkog.Api
dotnet restore
dotnet build
dotnet run

API http://localhost:5250
SWAGGER UI http://localhost:5250/swagger
Zdravotní endpoint http://localhost:5250/health