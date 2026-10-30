# Beginner .NET 10 microservices

Two independent C# Web APIs for a small shop. No extra NuGet packages are needed.

## Architecture

| Service | Address | Responsibility |
| --- | --- | --- |
| Products.Api | http://localhost:5001 | Owns the product catalog |
| Orders.Api | http://localhost:5002 | Creates and stores orders; asks Products for product details over HTTP |

When you POST an order, Orders validates the quantity, sends a GET request to Products, calculates the total, and saves the order. The client supplies a product ID and quantity; the price comes from Products.

These are microservices because they run as separate processes and communicate over HTTP. Neither project references the other. Each owns its data. Two folders inside one running API would not provide this separation.

## 1. Install the SDK

Install the **.NET 10 SDK**, not only the runtime: https://dotnet.microsoft.com/download/dotnet/10.0

Check your installation in a terminal:

```sh
dotnet --version
```

Use a 10.x SDK. Extract this ZIP and open a terminal in the SimpleMicroservices folder (the folder containing this README).

## 2. Run Products

```sh
dotnet run --project Products.Api --no-launch-profile --urls http://localhost:5001
```

Leave this terminal running. Open http://localhost:5001/products in your browser.

## 3. Run Orders

Open a second terminal in the same SimpleMicroservices folder:

```sh
dotnet run --project Orders.Api --no-launch-profile --urls http://localhost:5002
```

Leave this terminal running too. Open http://localhost:5002/orders; initially you will see an empty JSON array: [].

## 4. Create an order

In a third terminal, using **Windows PowerShell**:

```powershell
Invoke-RestMethod -Uri http://localhost:5002/orders -Method Post -ContentType 'application/json' -Body '{"productId":1,"quantity":2}'
```

Or using **macOS/Linux bash**:

```sh
curl -i http://localhost:5002/orders -H 'Content-Type: application/json' -d '{"productId":1,"quantity":2}'
```

Expected: HTTP 201 Created with a generated order ID, productName Keyboard, quantity 2, unitPrice 1500, and total 3000. The Location response header points to /orders/{generated-id}.

Refresh http://localhost:5002/orders to see the saved order.

You can also open requests.http in an editor with HTTP request support (for example, VS Code with the REST Client extension). Execute each request separately. This file includes invalid-input and service-unavailable examples.

## Understand the code

- Program.cs starts the API and defines its routes.
- MapGet handles reading data with HTTP GET.
- MapPost handles creating data with HTTP POST.
- A record defines the fields in a data object.
- Results.Ok sends HTTP 200; Results.Created sends HTTP 201.
- IHttpClientFactory supplies an HTTP client for calling Products.
- await lets the server handle other work while waiting for the HTTP response.
- Orders.Api/appsettings.json stores the Products service URL.
- Each .csproj targets net10.0 and uses the ASP.NET Core Web SDK.

## Manual checks

| Action | Expected result |
| --- | --- |
| GET /products on port 5001 | 200 with three products |
| POST /orders on port 5002 with productId 1, quantity 2 | 201 with total 3000 |
| GET /orders/{created-id} on port 5002 | 200 with the created order |
| POST quantity 0 | 400 |
| POST productId 999, quantity 1 | 404 |
| Stop Products using Ctrl+C, then POST a valid order | 503 |
| With Products stopped, GET existing orders | 200; Orders still owns its data |

Build each project independently with `dotnet build Products.Api` and `dotnet build Orders.Api`.

## Scope and next steps

This is a local learning example. Products are a fixed catalog and orders are stored in memory. Restarting Orders clears its orders. Multiple instances would each have separate memory. There is no database, authentication, stock reservation, payment processing, gateway, or Swagger UI.

Once you understand the HTTP flow, replace each service's memory storage with its own database, then explore authentication and Docker. Production deployment also needs HTTPS and appropriate operational controls.

If a port is already in use, stop the conflicting app or change the run command's port. If you change Products' port, update Orders.Api/appsettings.json too.

Validation note: project XML, JSON, and archive contents were checked during creation. Compilation and live HTTP tests could not be run because the authoring environment did not have the .NET SDK.

Reference: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
