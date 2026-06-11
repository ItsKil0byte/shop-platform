FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY contracts/ /app/contracts/
COPY src/building-blocks/ /app/src/building-blocks/

COPY src/services/catalog/Catalog.Domain/ /app/src/services/catalog/Catalog.Domain/
COPY src/services/catalog/Catalog.Application/ /app/src/services/catalog/Catalog.Application/
COPY src/services/catalog/Catalog.Infrastructure/ /app/src/services/catalog/Catalog.Infrastructure/
COPY src/services/catalog/Catalog.Api/ /app/src/services/catalog/Catalog.Api/

WORKDIR /app/src/services/catalog/Catalog.Api

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Catalog.Api.dll"]