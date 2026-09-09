<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import PostCard from './components/PostCard.vue'
import PostComposer from './components/PostComposer.vue'
import { getFeed, getMonkey, type FeedPost } from './api'
import { CurrentMonkeyId } from './currentMonkey'

const posts = ref<FeedPost[]>([])
const monkeyName = ref('Monkey')
const error = ref('')
const isLoading = ref(true)

let refreshTimer: number | undefined

async function loadFeed() {
  try {
    posts.value = await getFeed()
    error.value = ''
  } catch (loadError) {
    error.value = (loadError as Error).message
  } finally {
    isLoading.value = false
  }
}

onMounted(async () => {
  try {
    monkeyName.value = (await getMonkey(CurrentMonkeyId)).name
  } catch {
    // The feed is more important than the name, so we keep the placeholder.
  }

  await loadFeed()

  // Posts are stored by a background service, so the feed catches up a moment later.
  refreshTimer = window.setInterval(loadFeed, 3000)
})

onUnmounted(() => window.clearInterval(refreshTimer))
</script>

<template>
  <div class="page">
    <header>
      <h1>🍌 MonkeyBook</h1>
      <p>The social network of the canopy</p>
    </header>

    <main>
      <PostComposer :monkey-name="monkeyName" @posted="loadFeed" />

      <h2 class="feed-title">🌴 Latest hoots</h2>

      <p v-if="error" class="notice error">🙊 {{ error }}</p>
      <p v-else-if="isLoading" class="notice">Swinging over to the feed...</p>
      <p v-else-if="posts.length === 0" class="notice">No hoots yet. Be the first monkey to speak up!</p>

      <div class="feed">
        <PostCard v-for="post in posts" :key="post.id" :post="post" />
      </div>
    </main>
  </div>
</template>

<style scoped>
.page {
  max-width: 42rem;
  margin: 0 auto;
  padding: 1.5rem 1rem 3rem;
}

header {
  text-align: center;
  color: var(--banana-soft);
  margin-bottom: 1.5rem;
}

header h1 {
  margin: 0;
  font-size: 2.6rem;
  color: var(--banana);
  text-shadow: 3px 3px 0 var(--bark);
  letter-spacing: 1px;
}

header p {
  margin: 0.25rem 0 0;
  font-style: italic;
}

.feed-title {
  color: var(--banana-soft);
  font-size: 1.2rem;
  margin: 1.75rem 0 0.75rem;
}

.feed {
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
}

.notice {
  background: var(--banana-soft);
  border: 3px dashed var(--bark);
  border-radius: 14px;
  padding: 0.8rem 1rem;
  margin: 0 0 0.9rem;
  color: var(--bark);
}

.notice.error {
  background: #ffd9d5;
}
</style>
