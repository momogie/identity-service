<script setup lang="ts">
import { parseDate, type DateValue } from '@internationalized/date'
import { DateFormatter, getLocalTimeZone, today } from '@internationalized/date'

import { CalendarIcon } from 'lucide-vue-next'
import { cn } from '@/components/shadcn/lib/utils'
import { Button } from '@/components/shadcn/components/ui/button'
import { Calendar } from '@/components/shadcn/components/ui/calendar'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/shadcn/components/ui/popover'
import type { HTMLAttributes, InputHTMLAttributes } from 'vue'

const defaultPlaceholder = today(getLocalTimeZone())
var date = ref() as Ref<DateValue>

const df = new DateFormatter('en-US', {
  dateStyle: 'long',
})

const props = defineProps<{
  modelValue?: string | null ,
  defaultValue?: string | number,
  label?: string,
  description?: string,
  errors?: Array<string>,
  disabled?: boolean,
}>()

const emits = defineEmits<{
  (e: "update:modelValue", payload: string | number): void
}>()

watch(() => props.modelValue, (after) => {
  if(!after) return;

  date.value = parseDate(after.split("T")[0])
})

watch(date, (after) => {
  emits('update:modelValue', after.toString())
})
</script>

<template>
  <div
    :class="cn('mb-2 input-date', 
      (errors || []).filter(p => p != null).length > 0 ? 'is-invalid' : ''
    )"
  >
    <label for="" class="text-sm font-semibold">{{ label }}</label>
    <Popover v-slot="{ close }">
      <PopoverTrigger as-child :disabled="disabled">
        <Button
          variant="outline"
          :class="cn('w-full justify-start text-left font-normal', !date && 'text-muted-foreground')"
        >
          <CalendarIcon />
          {{ date ? date.toString() : "Pick a date" }}
        </Button>
      </PopoverTrigger>
      <PopoverContent class="w-auto p-0" align="start">
        <Calendar
          v-model="date"
          :default-placeholder="defaultPlaceholder"
          layout="month-and-year"
          initial-focus
          @update:model-value="close"
          :disabled="disabled"
        />
      </PopoverContent>
    </Popover>
    <p class="text-xs" v-if="description">{{ description }}</p>
    <p class="text-red-500 text-xs" v-if="errors">{{ errors[0] }}</p>
  </div>
</template>


<style lang="scss">
.label-text {
  font-weight: 500;
}
.is-invalid.input-date {
  button {
    border-color: red!important;
  }
}
</style>