<script setup lang="ts">
import { CheckIcon, ChevronsUpDownIcon } from 'lucide-vue-next'
import { ref } from 'vue'
import { Button } from '@/components/shadcn/components/ui/button'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/shadcn/components/ui/command'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/shadcn/components/ui/popover'

import { cn } from '@/components/shadcn/lib/utils'

const theme = useTheme();

// const frameworks = [
//   { value: 'next.js', label: 'Next.js' },
//   { value: 'sveltekit', label: 'SvelteKit' },
//   { value: 'nuxt.js', label: 'Nuxt.js' },
//   { value: 'remix', label: 'Remix' },
//   { value: 'astro', label: 'Astro' },
// ]

const open = ref(false)
const value = ref('')
</script>

<template>
  <Popover v-model:open="open">
    <PopoverTrigger as-child>
      <Button
        variant="outline" size="icon-sm"
        :aria-expanded="open"
      >
        <Icon name="ph:palette-bold"/>
        <!-- Theme: {{ theme.selectedTheme?.name }}
        <ChevronsUpDownIcon class="ml-2 h-4 w-4 shrink-0 opacity-50" /> -->
      </Button>
    </PopoverTrigger>
    <PopoverContent class="w-[200px] p-0">
      <Command>
        <CommandInput placeholder="Search theme..." />
        <CommandList>
          <CommandEmpty>No theme found.</CommandEmpty>
          <CommandGroup>
            <CommandItem
              v-for="v in theme.list"
              :key="v.key"
              :value="v.key"
              @select="() => {
                // open = false
                theme.setTheme(v.key)
              }"
            >
              <CheckIcon
                :class="cn(
                  'mr-2 h-4 w-4',
                  theme.selectedTheme?.key === v.key ? 'opacity-100' : 'opacity-0',
                )"
              />
              {{ v.name }}
            </CommandItem>
          </CommandGroup>
        </CommandList>
      </Command>
    </PopoverContent>
  </Popover>
</template>
