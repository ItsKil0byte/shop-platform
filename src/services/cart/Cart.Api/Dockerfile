FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY contracts/ /app/contracts/
COPY src/building-blocks/ /app/src/building-blocks/

COPY src/services/cart/Cart.Domain/ /app/src/services/cart/Cart.Domain/
COPY src/services/cart/Cart.Application/ /app/src/services/cart/Cart.Application/
COPY src/services/cart/Cart.Infrastructure/ /app/src/services/cart/Cart.Infrastructure/
COPY src/services/cart/Cart.Api/ /app/src/services/cart/Cart.Api/

WORKDIR /app/src/services/cart/Cart.Api

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Cart.Api.dll"]