# Mobile Remote Toolkit - Vue Client

Minimal Vue 3 + TypeScript frontend to consume the Mobile.Remote.Toolkit API.

Quick start

```bash
cd Mobile.Remote.Toolkit.Vue
npm install
npm run dev
```

- Edit `.env` or `.env.local` to set `VITE_API_BASE_URL` if needed.
- The app expects the API to expose endpoints like `/api/Android/devices` (see API controllers).

Best practices included:

- TypeScript types for API responses
- Central `apiClient` axios instance
- Composition API + typed calls
- Simple routing with `vue-router`
