FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SistemaTsMiddleware.sln .
COPY src/SistemaTs.Core/SistemaTs.Core.csproj src/SistemaTs.Core/
COPY src/SistemaTs.Infrastructure/SistemaTs.Infrastructure.csproj src/SistemaTs.Infrastructure/
COPY src/SistemaTs.Api/SistemaTs.Api.csproj src/SistemaTs.Api/
RUN dotnet restore

COPY src/ src/
RUN dotnet publish src/SistemaTs.Api/SistemaTs.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:3001
EXPOSE 3001

ENTRYPOINT ["dotnet", "SistemaTs.Api.dll"]
