# Changelog

All notable changes to the Tuseme .NET SDK will be documented in this file.

## [1.0.0] - 2026-04-25

### Added
- Initial release of the Tuseme .NET SDK.
- `TusemeClient` with async/await and thread-safe authentication.
- `Messages.SendAsync()` — send SMS to one or more recipients.
- `Messages.GetAsync()` — check delivery status of a message.
- `Messages.ListAsync()` — list sent messages with pagination.
- Built-in retry logic with exponential backoff.
- Exception hierarchy: `AuthenticationException`, `ValidationException`, `RateLimitException`.
- .NET 6, 7, 8 multi-targeting.
- System.Text.Json serialization.
