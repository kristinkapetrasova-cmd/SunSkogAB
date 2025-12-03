# === build stage ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# zkopíruj csproj a obnov závislosti (rychlejší díky cache)
COPY SunSkog.Api.csproj ./
RUN dotnet restore "SunSkog.Api.csproj"

# zbytek zdrojáků + publish
COPY . .
RUN dotnet publish "SunSkog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# === runtime stage ===
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "SunSkog.Api.dll"]