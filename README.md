# MonkeyBook

A tiny Facebook clone for monkeys, built as a .NET Aspire solution.

## Projects

| Project | What it does |
| --- | --- |
| `MonkeyBook.AppHost` | Aspire app host, starts everything (Postgres, Redis, Dapr, the apis and the frontend). |
| `MonkeyBook.PostsApi` | `POST /post` publishes a `post-created` event through Dapr pub/sub. |
| `MoneyBook.BackgroundProcessor` | Subscribes to `post-created`, applies migrations and is the only writer of the database. |
| `MonkeyBook.FeedApi` | `GET /feed` and `GET /monkeys/{id}`, read only. |
| `MonkeyBook.Shared` | Entities, `DbContext` and migrations. |
| `MonkeyBook.Frontend` | Vue 3 + Vite + TypeScript frontend. |

## Running

Prerequisites: .NET 10 SDK, Node.js, Docker (for Postgres and Redis) and the Dapr CLI.

```bash
cd code/MonkeyBook.Frontend
npm install

cd ..
dotnet run --project MonkeyBook.AppHost
```

Open the Aspire dashboard and follow the endpoint of the `monkeybook-frontend` resource.

## Frontend

The frontend is a small Vue single page app.

- The Vite dev server proxies `/api/feed` and `/api/monkeys` to the feed api and `/api/post` to the posts api,
  using the service addresses that Aspire injects as environment variables. That way the browser only talks to
  one origin, so the apis need no cors configuration.
- The logged in monkey is hard coded in `src/currentMonkey.ts` (`22222222-2222-2222-2222-222222222222`, Kong).
  Its display name is fetched from `GET /monkeys/{id}`.
- Because posts are stored by the background processor, a new post shows up a moment later. The feed therefore
  refreshes every 3 seconds and directly after posting.
- Posts are limited to 280 characters.

To work on the frontend alone, run `npm run dev` in `code/MonkeyBook.Frontend`. It then falls back to the
default localhost ports of the two apis, so they need to be running as well.