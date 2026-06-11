using Cart.Application.Interfaces;
using Cart.Grpc;
using Grpc.Core;

namespace Cart.Api.Grpc;

public class CartGrpcService : CartService.CartServiceBase
{
    private readonly ICartService _cartService;

    public CartGrpcService(ICartService cartService)
    {
        _cartService = cartService;
    }

    public override async Task<GetCartResponse> GetCart(GetCartRequest request, ServerCallContext context)
    {
        var cart = await _cartService.GetCartAsync(request.UserId);

        var response = new GetCartResponse
        {
            TotalPrice = (double)cart.TotalPrice
        };

        foreach (var item in cart.Items)
        {
            response.Items.Add(new CartItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = (double)item.Price
            });
        }

        return response;
    }

    public override async Task<Empty> ClearCart(ClearCartRequest request, ServerCallContext context)
    {
        await _cartService.ClearCartAsync(request.UserId);
        return new Empty();
    }
}