using Cart.Grpc;
using Grpc.Core;
using Order.Application.DTOs;
using Order.Application.Interfaces;

namespace Order.Infrastructure.GrpcClients;

public class CartGrpcClient(CartService.CartServiceClient client) : ICartClient
{
    private readonly CartService.CartServiceClient _client = client;

    public async Task ClearCartAsync(string userId, CancellationToken cancellationToken = default)
    {
        ClearCartRequest request = new() { UserId = userId };
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);

        try
        {
            await _client.ClearCartAsync(
                request, deadline: deadline, cancellationToken: cancellationToken
            );
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            throw new TimeoutException("Запрос к сервису корзины превысил время ожидания.", ex);
        }
    }

    public async Task<(decimal TotalPrice, List<CartItemDto> Items)> GetCartAsync(string userId, CancellationToken cancellationToken = default)
    {
        GetCartRequest request = new() { UserId = userId };
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);

        try
        {
            GetCartResponse response = await _client.GetCartAsync(
                request, deadline: deadline, cancellationToken: cancellationToken
            );

            List<CartItemDto> items = [.. response.Items.Select(item => new CartItemDto 
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = (decimal) item.Price
            })];

            return ((decimal) response.TotalPrice, items);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            throw new TimeoutException("Запрос к сервису корзины превысил время ожидания.", ex);
        }
    }
}