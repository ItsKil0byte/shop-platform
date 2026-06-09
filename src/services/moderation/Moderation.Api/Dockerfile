FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY contracts/ /app/contracts/

COPY src/services/moderation/Moderation.Api/ /app/src/services/moderation/Moderation.Api/

WORKDIR /app/src/serrvices/moderation/Moderation.Api

WORKDIR /app/src/services/moderation/Moderation.Api

RUN dotnet restore

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Moderation.Api.dll"]