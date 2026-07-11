# OpenVK to ASP.NET Migration Guidelines

These rules apply when migrating the OpenVK PHP/Chandler codebase to ASP.NET Core.

## Architecture

1. **Solution Structure**:
   - `Ovk.Net.Core`: Contains domain models, interfaces, and shared business logic. No database or web dependencies.
   - `Ovk.Net.Infrastructure`: Contains Entity Framework Core DbContext, migrations, repository implementations, and external service integrations (Redis, email, etc.).
   - `Ovk.Net.Web`: The ASP.NET Core MVC project containing Controllers, Razor Views, view models, and UI static assets.

2. **Technology Stack**:
   - **Framework**: ASP.NET Core 8.0+ (MVC).
   - **ORM**: Entity Framework Core.
   - **Views**: Razor (`.cshtml`).
   - **Caching/Events**: Redis (via `StackExchange.Redis` or similar).

## Code Translation Rules

1. **PHP to C# Mapping**:
   - Translate PHP classes directly to C# classes, ensuring proper access modifiers (`public`, `internal`, `private`).
   - Replace PHP's weak typing and arrays (`array()`, `[]`) with strongly-typed C# equivalents (`List<T>`, `Dictionary<K,V>`, `IEnumerable<T>`).
   - Use `async`/`await` for all I/O operations (database calls, Redis, HTTP requests) to improve performance, replacing synchronous PHP calls.
   - Use built-in ASP.NET Core Dependency Injection (DI) to manage services instead of global/singleton service locators.

2. **Database and ORM**:
   - Use EF Core Code-First approach. Recreate the existing OpenVK schema using EF Core Models and `DbContext`.
   - Prefer LINQ queries over raw SQL where possible.
   - If raw SQL is needed for complex queries or performance, use EF Core's `FromSqlRaw` or Dapper for that specific query.

3. **Templates (Latte to Razor)**:
   - Convert Latte tags to Razor C# syntax.
   - `{$variable}` -> `@Model.Variable`
   - `{foreach $items as $item}` -> `@foreach (var item in Model.Items)`
   - `{if $condition}` -> `@if (Model.Condition)`
   - `{include 'template'}` -> `<partial name="_Template" />`
   - Use Layouts (`_Layout.cshtml`) and Sections (`@RenderSection`) to replace Latte template inheritance.

4. **Controllers**:
   - Controllers should be lightweight. They should take services via constructor injection.
   - Map PHP route handlers (from Chandler/OpenVK routes) to standard ASP.NET Core route attributes (e.g., `[Route("...")]`, `[HttpGet]`).
