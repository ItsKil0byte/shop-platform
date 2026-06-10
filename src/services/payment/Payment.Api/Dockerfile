# -- Собираем --
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY contracts/ /app/contracts/

COPY src/services/payment/Payment.Domain/ /app/src/services/payment/Payment.Domain/
COPY src/services/payment/Payment.Application/ /app/src/services/payment/Payment.Application/
COPY src/services/payment/Payment.Infrastructure/ /app/src/services/payment/Payment.Infrastructure/
COPY src/services/payment/Payment.Api/ /app/src/services/payment/Payment.Api/

WORKDIR /app/src/services/payment/Payment.Api

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# -- Запускаем --
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Payment.Api.dll"]
