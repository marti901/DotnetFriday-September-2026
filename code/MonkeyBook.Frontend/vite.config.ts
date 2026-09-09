import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// Aspire injects the addresses of the referenced services as environment variables.
function serviceUrl(resourceName: string, fallback: string): string {
  return (
    process.env[`services__${resourceName}__http__0`] ??
    process.env[`services__${resourceName}__https__0`] ??
    fallback
  )
}

const feedApi = serviceUrl('monkeybook-feedapi', 'http://localhost:5156')
const postsApi = serviceUrl('monkeybook-postsapi', 'http://localhost:5157')

// The dev server proxies the api calls, so the browser only talks to one origin and we need no cors.
export default defineConfig({
  plugins: [vue()],
  server: {
    port: Number(process.env.PORT ?? 5173),
    strictPort: true,
    proxy: {
      '/api/feed': { target: feedApi, changeOrigin: true, rewrite: stripApiPrefix },
      '/api/monkeys': { target: feedApi, changeOrigin: true, rewrite: stripApiPrefix },
      '/api/post': { target: postsApi, changeOrigin: true, rewrite: stripApiPrefix },
    },
  },
})

function stripApiPrefix(path: string): string {
  return path.replace(/^\/api/, '')
}
