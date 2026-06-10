# -- Собираем --
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY src/services/notification/Notification.Worker/ /app/src/services/notification/Notification.Worker/

WORKDIR /app/src/services/notification/Notification.Worker

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# -- Запускаем --
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Notification.Worker.dll"]
