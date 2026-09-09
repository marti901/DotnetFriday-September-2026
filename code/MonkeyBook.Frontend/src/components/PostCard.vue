<script setup lang="ts">
import { computed } from 'vue'
import type { FeedPost } from '../api'

const props = defineProps<{ post: FeedPost }>()

const faces = ['🐒', '🦍', '🙈', '🐵', '🦧']

// Every monkey keeps the same face by picking it with the id.
const face = computed(() => {
  const sum = [...props.post.monkeyId].reduce((total, character) => total + character.charCodeAt(0), 0)
  return faces[sum % faces.length]
})

// The api stores everything in utc, older serializers leave the marker out.
const created = computed(() => {
  const value = props.post.created
  const utcValue = /(Z|[+-]\d{2}:\d{2})$/.test(value) ? value : `${value}Z`
  return new Date(utcValue).toLocaleString()
})
</script>

<template>
  <article class="post">
    <span class="avatar">{{ face }}</span>
    <div>
      <p class="meta"><strong>{{ post.monkeyName }}</strong> <span>· {{ created }}</span></p>
      <p class="message">{{ post.message }}</p>
    </div>
  </article>
</template>

<style scoped>
.post {
  display: flex;
  gap: 0.85rem;
  background: var(--cream);
  border: 4px solid var(--bark);
  border-left-width: 12px;
  border-radius: 16px;
  padding: 0.9rem 1.1rem;
  box-shadow: 0 6px 0 rgba(18, 48, 28, 0.3);
}

.avatar {
  font-size: 1.6rem;
  background: var(--banana-soft);
  border: 3px solid var(--bark-light);
  border-radius: 50%;
  width: 2.9rem;
  height: 2.9rem;
  flex: 0 0 auto;
  display: grid;
  place-items: center;
}

.meta {
  margin: 0 0 0.3rem;
  font-size: 0.85rem;
  color: var(--bark-light);
}

.meta strong {
  color: var(--jungle);
  font-size: 1rem;
}

.message {
  margin: 0;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}
</style>
