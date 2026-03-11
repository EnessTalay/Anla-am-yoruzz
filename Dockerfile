FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Anlasalamiyoruz.sln", "./"]
COPY ["src/Anlasalamiyoruz.API/Anlasalamiyoruz.API.csproj", "src/Anlasalamiyoruz.API/"]
COPY ["src/Anlasalamiyoruz.Application/Anlasalamiyoruz.Application.csproj", "src/Anlasalamiyoruz.Application/"]
COPY ["src/Anlasalamiyoruz.Domain/Anlasalamiyoruz.Domain.csproj", "src/Anlasalamiyoruz.Domain/"]
COPY ["src/Anlasalamiyoruz.Infrastructure/Anlasalamiyoruz.Infrastructure.csproj", "src/Anlasalamiyoruz.Infrastructure/"]

RUN dotnet restore

COPY . .
WORKDIR "/src/src/Anlasalamiyoruz.API"
RUN dotnet publish "Anlasalamiyoruz.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Anlasalamiyoruz.API.dll"]