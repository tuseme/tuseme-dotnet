# Tuseme .NET SDK

Official .NET client for the [Tuseme SMS API](https://docs.tuseme.co.ke).

[![NuGet](https://img.shields.io/nuget/v/Tuseme)](https://www.nuget.org/packages/Tuseme)
[![.NET 6+](https://img.shields.io/badge/.NET-6%2B-blue.svg)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Installation

```bash
dotnet add package Tuseme
```

Or via Package Manager:

```powershell
Install-Package Tuseme
```

## Quick Start

```csharp
using Tuseme;

var client = new TusemeClient(
    apiKey: "tk_test_your_api_key",
    apiSecret: "sk_test_your_api_secret"
);

var response = await client.Messages.SendAsync(new SendRequest
{
    Content = "Hello from Tuseme! Your OTP is 482910.",
    SenderId = "TUSEME-LTD",
    Recipients = new List<Recipient>
    {
        new() { Msisdn = "+254712345678", Name = "John Doe" }
    },
    Type = "transactional",
    Priority = "HIGH",
});

Console.WriteLine($"Message ID: {response.MessageId}");
Console.WriteLine($"Status: {response.Status}");
```

## Features

- **Async/await** throughout — fully non-blocking
- **Automatic authentication** — tokens obtained and refreshed transparently
- **Thread-safe** — `SemaphoreSlim`-based token management
- **Built-in retries** — exponential backoff for transient failures
- **.NET 6, 7, 8** supported
- **System.Text.Json** — fast, allocation-efficient serialization

## Authentication

```csharp
// Sandbox credentials (for testing)
var client = new TusemeClient("tk_test_...", "sk_test_...");

// Production credentials
var client = new TusemeClient("tk_live_...", "sk_live_...");
```

The SDK will:
1. Automatically obtain an access token on the first request
2. Cache the token until it expires
3. Transparently refresh expired tokens

## Usage

### Send SMS

```csharp
// Single recipient
var response = await client.Messages.SendAsync(new SendRequest
{
    Content = "Your verification code is 123456",
    SenderId = "TUSEME-LTD",
    Recipients = new() { new() { Msisdn = "+254712345678" } },
    Type = "transactional",
});

// Multiple recipients with metadata
var response = await client.Messages.SendAsync(new SendRequest
{
    Content = "Flash sale! 50% off today only.",
    SenderId = "TUSEME-LTD",
    Recipients = new()
    {
        new() { Msisdn = "+254712345678", Name = "Alice" },
        new() { Msisdn = "+254798765432", Name = "Bob" },
    },
    Type = "promotional",
    Metadata = new() { ["campaign"] = "flash_sale_q2" },
});
```

### Check Delivery Status

```csharp
var status = await client.Messages.GetAsync("msg_a1b2c3d4...");
Console.WriteLine($"Status: {status.Status}");
Console.WriteLine($"Delivered at: {status.DeliveredAt}");
```

### List Messages

```csharp
var result = await client.Messages.ListAsync(page: 1, pageSize: 20, status: "delivered");
Console.WriteLine(result);
```

## Error Handling

```csharp
using Tuseme;

try
{
    var response = await client.Messages.SendAsync(new SendRequest
    {
        Content = "Hello!",
        Recipients = new() { new() { Msisdn = "+254712345678" } },
    });
}
catch (AuthenticationException)
{
    Console.Error.WriteLine("Invalid credentials — check your API key and secret");
}
catch (ValidationException ex)
{
    Console.Error.WriteLine($"Bad request: {ex.Message}");
}
catch (RateLimitException ex)
{
    Console.Error.WriteLine($"Rate limited — retry after {ex.RetryAfter}s");
}
```

## Configuration

```csharp
var client = new TusemeClient(
    apiKey: "...",
    apiSecret: "...",
    baseUrl: "https://api.tuseme.co.ke/api/v1",  // default
    timeoutSeconds: 30,  // request timeout
    maxRetries: 3        // automatic retries on failure
);
```

## License

MIT — see [LICENSE](LICENSE).
