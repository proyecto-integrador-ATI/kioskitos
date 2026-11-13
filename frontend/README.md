# Kioskitos Frontend

Kioskitos web client, built with Next.js, TypeScript, and the App Router. The
application will centralize the management of clients, projects, tasks,
estimates, tracked hours, and contractual agreements for independent
professionals.

## Requirements

- Node.js 22.
- npm.

The expected Node.js version is defined in [.nvmrc](./.nvmrc).

## Installation

From this directory:

```bash
npm ci
```

## Environment variables

Copy [.env.example](./.env.example) to `.env.local` and configure the REST API
URL:

```env
NEXT_PUBLIC_API_URL=http://localhost:5076
```

The port and protocol must match the .NET backend configuration. The backend
must allow requests from `http://localhost:3000` through CORS.

## Development

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000).

## Available scripts

```bash
npm run lint
npm test
npm run test:coverage
npm run build
npm start
```

`npm run test:coverage` generates the `coverage/lcov.info` report, which can be
used by analysis tools such as SonarQube.

## Estructura

```text
frontend/
├── app/           # Routes and layouts
├── components/    # Reusable UI components
├── hooks/         # Custom React hooks
├── services/      # API clients and external services
├── types/         # Shared TypeScript types
└── public/        # Static assets
```

The HTTP client is implemented in [services/api.ts](./services/api.ts) and uses
`NEXT_PUBLIC_API_URL` as the backend base URL.
