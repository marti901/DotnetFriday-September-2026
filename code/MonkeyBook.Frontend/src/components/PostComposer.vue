<script setup lang="ts">
import { computed, ref } from 'vue'
import { createPost } from '../api'
import { CurrentMonkeyId, MaxMessageLength } from '../currentMonkey'

const props = defineProps<{ monkeyName: string }>()
const emit = defineEmits<{ posted: [] }>()

const message = ref('')
const isSending = ref(false)
const error = ref('')

const remaining = computed(() => MaxMessageLength - message.value.length)
const canSend = computed(() => message.value.trim().length > 0 && remaining.value >= 0 && !isSending.value)

async function send() {
  if (!canSend.value) {
    return
  }

  isSending.value = true
  error.value = ''

  try {
    await createPost(CurrentMonkeyId, message.value.trim())
    message.value = ''
    emit('posted')
  } catch (sendError) {
    error.value = (sendError as Error).message
  } finally {
    isSending.value = false
  }
}
</script>

<template>
  <section class="composer">
    <div class="composer-head">
      <span class="avatar">🐒</span>
      <h2>What's swinging, {{ props.monkeyName }}?</h2>
    </div>

    <textarea
      v-model="message"
      rows="3"
      placeholder="Share a banana story with the troop..."
      @keydown.ctrl.enter="send"
    ></textarea>

    <div class="composer-foot">
      <span class="counter" :class="{ 'counter-over': remaining < 0 }">{{ remaining }} 🍌 left</span>
      <button :disabled="!canSend" @click="send">{{ isSending ? 'Swinging...' : 'Hoot it!' }}</button>
    </div>

    <p v-if="error" class="error">🙈 {{ error }}</p>
  </section>
</template>

<style scoped>
.composer {
  background: var(--cream);
  border: 4px solid var(--bark);
  border-radius: 18px;
  padding: 1rem 1.25rem;
  box-shadow: 0 8px 0 rgba(18, 48, 28, 0.35);
}

.composer-head {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.composer-head h2 {
  margin: 0;
  font-size: 1.1rem;
  color: var(--bark);
}

.avatar {
  font-size: 1.7rem;
  background: var(--banana-soft);
  border: 3px solid var(--bark-light);
  border-radius: 50%;
  width: 3rem;
  height: 3rem;
  display: grid;
  place-items: center;
}

textarea {
  width: 100%;
  margin-top: 0.75rem;
  padding: 0.6rem;
  font-size: 1rem;
  color: var(--jungle-dark);
  background: #fffdf5;
  border: 3px solid var(--jungle-light);
  border-radius: 12px;
  resize: vertical;
}

textarea:focus {
  outline: none;
  border-color: var(--banana);
}

.composer-foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 0.6rem;
}

.counter {
  font-size: 0.85rem;
  color: var(--bark-light);
}

.counter-over {
  color: #b4231c;
  font-weight: bold;
}

button {
  background: var(--banana);
  color: var(--bark);
  font-weight: bold;
  font-size: 1rem;
  padding: 0.5rem 1.4rem;
  border: 3px solid var(--bark);
  border-radius: 999px;
  box-shadow: 0 4px 0 var(--bark);
}

button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  box-shadow: none;
}

button:not(:disabled):active {
  transform: translateY(4px);
  box-shadow: none;
}

.error {
  margin: 0.6rem 0 0;
  color: #b4231c;
}
</style>
