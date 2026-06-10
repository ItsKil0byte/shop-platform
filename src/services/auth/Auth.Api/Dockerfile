FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY src/services/auth/Auth.Domain/ /app/src/services/auth/Auth.Domain/
COPY src/services/auth/Auth.Application/ /app/src/services/auth/Auth.Application/
COPY src/services/auth/Auth.Infrastructure/ /app/src/services/auth/Auth.Infrastructure/
COPY src/services/auth/Auth.Api/ /app/src/services/auth/Auth.Api/

WORKDIR /app/src/services/auth/Auth.Api

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Auth.Api.dll"]

