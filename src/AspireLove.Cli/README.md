# aspire.love

Make your [Lovable](https://lovable.dev) project independent of the Lovable/Supabase cloud by
generating a clean **.NET Aspire AppHost** that runs your whole stack locally — Supabase, edge
functions and the Vite frontend — with a single command.

## Install

```bash
dotnet tool install -g aspire.love
```

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

Project home: <https://github.com/fgilde/aspire.love>
