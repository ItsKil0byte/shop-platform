# -- Собираем --
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY contracts/ /app/contracts/

COPY src/services/order/Order.Domain/ /app/src/services/order/Order.Domain/
COPY src/services/order/Order.Application/ /app/src/services/order/Order.Application/
COPY src/services/order/Order.Infrastructure/ /app/src/services/order/Order.Infrastructure/
COPY src/services/order/Order.Api/ /app/src/services/order/Order.Api/

COPY src/building-blocks/ /app/src/building-blocks/

WORKDIR /app/src/services/order/Order.Api

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# -- Запускаем --
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Order.Api.dll"]