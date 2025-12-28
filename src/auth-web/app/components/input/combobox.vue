<script setup lang="ts">
import { Check, ChevronsUpDown, Search } from "lucide-vue-next"
import { ref, type HTMLAttributes } from "vue"
import { useVModel } from "@vueuse/core"
import { cn } from "@/components/shadcn/lib/utils"
import { 
  Combobox, ComboboxAnchor, ComboboxEmpty, ComboboxGroup, 
  ComboboxInput, ComboboxItem, ComboboxItemIndicator, 
  ComboboxList, ComboboxTrigger 
} from "@/components/shadcn/components/ui/combobox"

var list = ref<Array<any>>([])

const value = ref<any>()

const props = defineProps<{
  defaultValue?: string | number
  modelValue?: string | number
  class?: HTMLAttributes["class"],
  label?: string,
  source?: string,
  placeholder?: string,
  filters?: Array<Array<any>>,
  valueField?: string,
  textField?: string,
  disabled?: boolean
}>()

const emits = defineEmits<{
  (e: "update:modelValue", payload: string | number): void
}>()

const modelValue = useVModel(props, "modelValue", emits, {
  passive: true,
  defaultValue: props.defaultValue,
})

const search = function(e: any) {
  load(e.target.value || '');
}

const app: any = useNuxtApp();
var load = function(e: any) {
  app.$api.getList(props.source || 'Personnels',(props.filters || []),['*'], 1, 20, ['Name asc']).then((resp: any) => {
    list = resp.data.data;
  });
}
const open = function(e: any) {
  load('')
}
</script>

<template>
  <div>
    <div class="mb-2 relative">
      <label for="" class="text-sm font-semibold">{{ label }}</label>
      <Combobox v-model="value" by="label" class="w-full relative">
        <ComboboxAnchor as-child>
          <ComboboxTrigger as-child
            @click="open"
          >
            <Button variant="outline" class="justify-between">
              {{ !!value ? value[textField || 'Label'] : `Select ${label}` }}
              <ChevronsUpDown class="ml-2 h-4 w-4 shrink-0 opacity-50" />
            </Button>
          </ComboboxTrigger>
        </ComboboxAnchor>

        <ComboboxList>
          <div class="relative w-full max-w-sm items-center">
            <ComboboxInput class="pl-9 focus-visible:ring-0 border-0 border-b rounded-none h-10" 
              :placeholder="props.placeholder || 'Search...'" 
              @input="search"
            />
            <span class="absolute start-0 inset-y-0 flex items-center justify-center px-3">
              <Search class="size-4 text-muted-foreground" />
            </span>
          </div>

          <ComboboxEmpty>
            No data found.
          </ComboboxEmpty>

          <ComboboxGroup>
            <ComboboxItem
              v-for="r in list"
              :key="r[valueField || 'Id']"
              :value="r"
            >
              {{ r[textField || 'Label'] }}
              <ComboboxItemIndicator>
                <Check :class="cn('ml-auto h-4 w-4')" />
              </ComboboxItemIndicator>
            </ComboboxItem>
          </ComboboxGroup>
        </ComboboxList>
      </Combobox>
    </div>
  </div>
</template>