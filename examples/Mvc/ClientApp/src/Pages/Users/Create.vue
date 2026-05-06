<script setup>
import { computed } from 'vue';
import { Head, Link, useForm } from '@inertiajs/vue3';
import AppShell from '../../components/AppShell.vue';

const props = defineProps({
  errors: {
    type: Object,
    default: () => ({}),
  },
});

const form = useForm({
  Name: '',
  Email: '',
});

const nameError = computed(() => form.errors.name ?? props.errors.name ?? '');

function submit() {
  form.post('/users');
}
</script>

<template>
  <Head title="Create User" />

  <AppShell
    eyebrow="PRG validation"
    title="Create a user"
    description="The MVC sample redirects after POST and flashes errors into the next GET so Vue can render them naturally on the returned page."
  >
    <section class="panel form-panel">
      <form class="form-grid" @submit.prevent="submit" data-testid="vue-create-form">
        <label class="field">
          <span>Name</span>
          <input
            v-model="form.Name"
            data-testid="vue-name-input"
            name="Name"
            placeholder="Charlie"
          />
          <span v-if="nameError" class="field-error" data-testid="vue-create-name-error">{{ nameError }}</span>
        </label>

        <label class="field">
          <span>Email</span>
          <input
            v-model="form.Email"
            data-testid="vue-email-input"
            name="Email"
            type="email"
            placeholder="charlie@example.com"
          />
        </label>

        <div class="button-row">
          <button class="button button--primary" type="submit" :disabled="form.processing" data-testid="vue-submit-button">
            {{ form.processing ? 'Saving…' : 'Create user' }}
          </button>
          <Link href="/users" class="button button--secondary">Back to users</Link>
        </div>
      </form>
    </section>
  </AppShell>
</template>