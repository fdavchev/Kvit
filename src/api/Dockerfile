FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/api/Kvit.Api/Kvit.Api.csproj src/api/Kvit.Api/
COPY src/api/Kvit.Application/Kvit.Application.csproj src/api/Kvit.Application/
COPY src/api/Kvit.Domain/Kvit.Domain.csproj src/api/Kvit.Domain/
COPY src/api/Kvit.Infrastructure/Kvit.Infrastructure.csproj src/api/Kvit.Infrastructure/
RUN dotnet restore src/api/Kvit.Api/Kvit.Api.csproj

COPY src/api/ src/api/
RUN dotnet publish src/api/Kvit.Api/Kvit.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Kvit.Api.dll"]
