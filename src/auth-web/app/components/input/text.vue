<script setup lang="ts">
import type { HTMLAttributes, InputHTMLAttributes } from "vue"
import { useVModel } from "@vueuse/core"
import { cn } from '@/components/shadcn/lib/utils'

const props = defineProps<{
  rows?: number | string,
  multiline?: boolean,
  defaultValue?: string | number,
  modelValue?: string | number,
  class?: HTMLAttributes["class"],
  type?: InputHTMLAttributes["type"],
  label?: string,
  description?: string,
  disabled?: boolean,
  placeholder?: string,
  errors?: Array<string>
}>()

const emits = defineEmits<{
  (e: "update:modelValue", payload: string | number): void
}>()

const modelValue = useVModel(props, "modelValue", emits, {
  passive: true,
  defaultValue: props.defaultValue,
})
</script>

<template>
  <div :class="cn('mb-2',
    (errors || []).filter(p => p != null).length > 0 ? 'is-invalid' : ''
  )">
    <label for="" class="text-sm font-semibold" v-if="label">{{ label }}</label>
    <input v-if="!!!multiline" v-model="modelValue" data-slot="input"
      :aria-invalid="(errors || []).filter(p => p != null).length > 0 ? 'true' : 'false'" autocomplete="off" :class="cn(
        'file:text-foreground placeholder:text-muted-foreground selection:bg-primary selection:text-primary-foreground dark:bg-input/30 border-input flex h-9 w-full min-w-0 rounded-md border bg-transparent px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm',
        'focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]',
        'aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive',
        props.class,
      )" :placeholder="placeholder" :disabled="disabled" :type="props.type || 'text'">
    <textarea v-else v-model="modelValue" data-slot="input"
      :aria-invalid="(errors || []).filter(p => p != null).length > 0 ? 'true' : 'false'" autocomplete="off"
      :rows="rows || 3" :disabled="disabled" :class="cn(
        'file:text-foreground placeholder:text-muted-foreground selection:bg-primary selection:text-primary-foreground dark:bg-input/30 border-input flex w-full min-w-0 rounded-md border bg-transparent px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm',
        'focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]',
        'aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive',
        props.class,
      )"></textarea>
    <p class="text-xs" v-if="description">{{ description }}</p>
    <p class="text-red-500 text-xs" v-if="errors">{{ errors[0] }}</p>
  </div>
</template>

<style lang="scss">
.label-text {
  font-weight: 500;
}

.is-invalid {
  textarea {
    border-color: red !important;
  }
  input {
    border-color: red !important;
  }
}
</style>