# aspire.love

[![NuGet](https://img.shields.io/nuget/v/love.aspire.svg)](https://www.nuget.org/packages/love.aspire)

**Make your [Lovable](https://lovable.dev) project independent of the Lovable/Supabase cloud.**

`aspire.love` generates a clean **.NET Aspire AppHost** for your existing Lovable project, so your
whole stack — Supabase, edge functions and the Vite frontend — runs locally with a single command.
No vendor lock-in: switch between a full local stack, a cloud-synced stack, or a direct remote
connection whenever you like.

## Install

```bash
dotnet tool install -g love.aspire
```

> The NuGet listing id is `love.aspire` — the brand stays **aspire.love**, but the `Aspire` prefix
> on NuGet is reserved by Microsoft, so the package id mirrors it.

## Use

```bash
# generate the aspire folder into your Lovable project
aspire-love init --path ./my-lovable-app

# then run everything together
aspire run
```

## Modes

- **FullLocal** (default) — runs the full Supabase stack in Docker and applies your migrations and edge functions.
- **SupabaseSync** — runs Supabase locally but pulls schema and data from an existing cloud project.
- **RemoteConnect** — no local stack; the frontend talks directly to your existing Supabase cloud project.

## Update

```bash
aspire-love update
```

## Prefer a UI?

There's also **aspire.love Studio**, a Fluent desktop app that walks you through every option with
live validation, a code preview and an auto-updating installer. Grab it from the website below.

## Links

- Website: <https://www.aspire.love>
- Source & issues: <https://github.com/fgilde/aspire.love>
- Author: [Florian Gilde](https://www.gilde.org) · <https://www.gilde.org>

---

MIT © Florian Gilde
