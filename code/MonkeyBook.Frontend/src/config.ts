// In azure the apis live on their own origins, so the urls are loaded at runtime from config.json.
// Locally that file does not exist and the vite dev server proxies /api to both apis.
interface AppConfig {
  feedApiUrl: string
  postsApiUrl: string
}

let config: AppConfig = { feedApiUrl: '/api', postsApiUrl: '/api' }

export async function loadConfig(): Promise<void> {
  try {
    const response = await fetch('/config.json', { cache: 'no-store' })

    if (!response.ok) {
      return
    }

    const loaded = (await response.json()) as Partial<AppConfig>

    config = {
      feedApiUrl: trimSlash(loaded.feedApiUrl) ?? config.feedApiUrl,
      postsApiUrl: trimSlash(loaded.postsApiUrl) ?? config.postsApiUrl,
    }
  } catch {
    // No config.json, keep the dev defaults.
  }
}

export function feedApiUrl(path: string): string {
  return `${config.feedApiUrl}${path}`
}

export function postsApiUrl(path: string): string {
  return `${config.postsApiUrl}${path}`
}

function trimSlash(value: string | undefined): string | undefined {
  return value ? value.replace(/\/+$/, '') : undefined
}
