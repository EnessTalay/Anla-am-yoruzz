# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║  Stage 1 — Build                                                            ║
# ║  SDK imajını kullanarak projeyi derliyoruz.                                 ║
# ╚══════════════════════════════════════════════════════════════════════════════╝
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Önce sadece .csproj dosyalarını kopyala → bağımlılık katmanı önbelleğe alınır.
# Kaynak kod değişse bile bu katman yeniden build edilmez.
COPY Anlasalamiyoruz.sln                                                          ./
COPY src/Anlasalamiyoruz.API/Anlasalamiyoruz.API.csproj                          src/Anlasalamiyoruz.API/
COPY src/Anlasalamiyoruz.Application/Anlasalamiyoruz.Application.csproj          src/Anlasalamiyoruz.Application/
COPY src/Anlasalamiyoruz.Domain/Anlasalamiyoruz.Domain.csproj                    src/Anlasalamiyoruz.Domain/
COPY src/Anlasalamiyoruz.Infrastructure/Anlasalamiyoruz.Infrastructure.csproj    src/Anlasalamiyoruz.Infrastructure/

RUN dotnet restore --locked-mode

# Kaynak kodun tamamını kopyala ve Release modunda yayınla
COPY src/ src/
RUN dotnet publish src/Anlasalamiyoruz.API/Anlasalamiyoruz.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║  Stage 2 — Runtime                                                          ║
# ║  SDK'sız, sadece runtime imajı → küçük ve güvenli final image.             ║
# ╚══════════════════════════════════════════════════════════════════════════════╝
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Yayınlanan dosyaları kopyala
COPY --from=build /app/publish .

# Render ve benzeri platformlar PORT env var'ını dinamik olarak atar.
# ASPNETCORE_HTTP_PORTS .NET 8+ için önerilen yöntemdir;
# ASPNETCORE_URLS da desteklenmektedir (bkz. Program.cs).
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

# Root olmayan kullanıcı ile çalıştır (güvenlik)
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Anlasalamiyoruz.API.dll"]
