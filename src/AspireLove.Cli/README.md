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

## Quick start

```bash
# generate the aspire folder into your Lovable project
aspire-love init --path ./my-lovable-app

# then run everything together (from the project root)
aspire run
```

`init` writes an `aspire/` folder next to your app — a `.slnx` solution and a `*.AppHost` project
(AppHost.cs, appsettings.json, launchSettings.json) — and adds an `aspire` script to your
`package.json` so the AppHost can launch the Vite frontend. Nothing else in your project is touched.

## Modes

Pick a mode with `--mode` (default: `FullLocal`):

| Mode | What it does | When to use |
|------|--------------|-------------|
| **FullLocal** | Runs the full Supabase stack in Docker and applies your migrations and edge functions locally. | Day-to-day local development, fully offline. |
| **SupabaseSync** | Runs Supabase locally but pulls schema and data from an existing cloud project. | You want a local stack seeded from production/staging. |
| **RemoteConnect** | No local stack; the frontend talks directly to your existing Supabase cloud project. | Quick run against the real cloud backend. |

## Options (`init`)

| Option | Alias | Default | Description |
|--------|-------|---------|-------------|
| `--path` | `-p` | current directory | Path to the root of the Lovable project. |
| `--name` | `-n` | `package.json` name, then `MyAspireLove` | Project name. |
| `--organization` | `-o` | `My Company` | Organization name shown in Supabase Studio. |
| `--mode` | `-m` | `FullLocal` | `FullLocal`, `SupabaseSync` or `RemoteConnect`. |
| `--monitoring` | | off | Add the Grafana/Tempo/OpenTelemetry observability stack (local modes only). |
| `--lovable-api-key` | | — | Lovable AI gateway key, so the project's built-in AI keeps working locally. |
| `--db-password` | | `local-dev-password-123` | Local Postgres password. |
| `--user-name` | | `admin` | Default admin user name. |
| `--user-email` | | `admin@localhost` | Default admin user email. |
| `--user-password` | | `admin` | Default admin user password. |
| `--dry-run` | | off | Show what would be generated without writing any files. |
| `--yes` | `-y` | off | Proceed even if there are warnings. |

### SupabaseSync mode

| Option | Description |
|--------|-------------|
| `--sync-project-ref` | Supabase project ref to sync from. |
| `--sync-service-key` | Service key for sync. |
| `--sync-db-password` | Database password for sync. |
| `--sync-management-token` | Management token for sync. |

### RemoteConnect mode

| Option | Description |
|--------|-------------|
| `--remote-project-ref` | Supabase project ref to connect to. |
| `--remote-service-key` | Service key. |

## Examples

```bash
# Full local stack with a custom name and observability dashboards
aspire-love init -p ./my-lovable-app -n ShopFront --monitoring

# Preview what would be generated, without writing anything
aspire-love init -p ./my-lovable-app --dry-run

# Keep the project's Lovable AI working locally
aspire-love init -p ./my-lovable-app --lovable-api-key sk-lov-...

# Seed a local stack from an existing cloud project
aspire-love init -p ./my-lovable-app -m SupabaseSync \
  --sync-project-ref abcdefgh \
  --sync-service-key eyJhbGci... \
  --sync-db-password "$SUPABASE_DB_PASSWORD" \
  --sync-management-token sbp_...

# Run the frontend directly against the cloud backend (no local stack)
aspire-love init -p ./my-lovable-app -m RemoteConnect \
  --remote-project-ref abcdefgh \
  --remote-service-key eyJhbGci...

# Run it all together afterwards
aspire run
```

## Update

```bash
aspire-love update
```

## Under the hood

The generated AppHost is powered by
[**Nextended.Aspire.Hosting.Supabase**](https://www.nuget.org/packages/Nextended.Aspire.Hosting.Supabase),
the open-source .NET Aspire hosting integration that makes Supabase a first-class Aspire resource. A
single `builder.AddSupabase(...)` call brings up the entire stack — Postgres, Auth, Storage, Realtime,
the Kong gateway, Studio and the edge-function runtime — with a fluent API for migrations, edge
functions, a seeded user, cloud sync and observability. aspire.love is the tool that generates a
clean, readable AppHost around that package, so you get a real .NET solution instead of hand-rolled
Docker Compose.

## Prefer a UI?

There's also **aspire.love Studio**, a Fluent desktop app that walks you through every option with
live validation, a code preview and an auto-updating installer. Grab it from the website below.

## Links

- Website: <https://www.aspire.love>
- Source & issues: <https://github.com/fgilde/aspire.love>
- Author: [Florian Gilde](https://www.gilde.org) · <https://www.gilde.org>

---

MIT © Florian Gilde
