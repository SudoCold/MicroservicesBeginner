var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Only this service owns the product catalog.
// A real application would store these records in its own database.
Product[] products =
[
    new(1, "Keyboard", 1500m),
    new(2, "Mouse", 500m),
    new(3, "Monitor", 8000m)
];

app.MapGet("/", () => "Products API is running. Try /products");
app.MapGet("/products", () => Results.Ok(products));
app.MapGet("/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.Run();

// A record is a compact C# type for carrying data.
public record Product(int Id, string Name, decimal Price);
