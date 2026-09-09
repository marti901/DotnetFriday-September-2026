import { feedApiUrl, postsApiUrl } from './config'

export interface FeedPost {
  id: string
  monkeyId: string
  monkeyName: string
  message: string
  created: string
}

export interface MonkeyProfile {
  id: string
  name: string
}

export async function getFeed(): Promise<FeedPost[]> {
  const response = await fetch(feedApiUrl('/feed'))

  if (!response.ok) {
    throw new Error('The feed is stuck in a tree.')
  }

  return await response.json()
}

export async function getMonkey(monkeyId: string): Promise<MonkeyProfile> {
  const response = await fetch(feedApiUrl(`/monkeys/${monkeyId}`))

  if (!response.ok) {
    throw new Error('That monkey is not in the troop.')
  }

  return await response.json()
}

export async function createPost(monkeyId: string, message: string): Promise<void> {
  const response = await fetch(postsApiUrl('/post'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ monkeyId, message }),
  })

  if (!response.ok) {
    throw new Error('The post fell out of the tree.')
  }
}
