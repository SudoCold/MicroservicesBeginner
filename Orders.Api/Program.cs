using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// The factory manages HTTP connections for calls to the other service.
builder.Services.AddHttpClient("Products", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Products"]
        ?? "http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

// Thread-safe storage because multiple requests can arrive at once.
var orders = new ConcurrentDictionary<Guid, Order>();

app.MapGet("/", () => "Orders API is running. Try /orders");
app.MapGet("/orders", () => Results.Ok(orders.Values.OrderBy(o => o.CreatedAt)));
app.MapGet("/orders/{id:guid}", (Guid id) =>
    orders.TryGetValue(id, out var order)
        ? Results.Ok(order)
        : Results.NotFound());

app.MapPost("/orders", async Task<IResult> (
    CreateOrderRequest request, IHttpClientFactory factory) =>
{
    if (request.ProductId <= 0 || request.Quantity is < 1 or > 100)
        return Results.BadRequest(new { error = "Use a positive productId and quantity from 1 to 100." });

    ProductDto? product;
    try
    {
        var client = factory.CreateClient("Products");
        using var response = await client.GetAsync($"/products/{request.ProductId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return Results.NotFound(new { error = "Product does not exist." });

        response.EnsureSuccessStatusCode();
        product = await response.Content.ReadFromJsonAsync<ProductDto>();
    }
    catch (HttpRequestException)
    {
        return Results.Problem("Products API is unavailable. Start it and try again.", statusCode: 503);
    }
    catch (OperationCanceledException)
    {
        return Results.Problem("Products API took too long to respond.", statusCode: 503);
    }
    catch (JsonException)
    {
        return Results.Problem("Products API returned invalid data.", statusCode: 502);
    }

    if (product is null || string.IsNullOrWhiteSpace(product.Name) || product.Price < 0)
        return Results.Problem("Products API returned invalid data.", statusCode: 502);

    // Save the name and price at purchase time, even if the catalog later changes.
    var order = new Order(Guid.NewGuid(), request.ProductId, product.Name,
        request.Quantity, product.Price, DateTimeOffset.UtcNow);
    orders[order.Id] = order;

    return Results.Created($"/orders/{order.Id}", order);
});

app.Run();

public record CreateOrderRequest(int ProductId, int Quantity);
// DTO = Data Transfer Object: the JSON shape expected from Products API.
public record ProductDto(int Id, string Name, decimal Price);
public record Order(Guid Id, int ProductId, string ProductName,
    int Quantity, decimal UnitPrice, DateTimeOffset CreatedAt)
{
    public decimal Total => Quantity * UnitPrice;
}
