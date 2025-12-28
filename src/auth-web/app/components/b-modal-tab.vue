<template>
  <div
    :id="id"
    class="modal hidden fixed inset-0 z-10 "
  >
    <div class="modal-backdrop fixed inset-0 bg-black/50 "></div>
     <!-- Modal panel -->
    <div
      class="modal-panel fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2
             rounded-xl shadow-lg scale-95 opacity-0 bg-card
             transition-all duration-200 ease-out flex flex-col overflow-hidden"
      :class="computedSize"
    >
      <!-- Header -->
      <div class="flex justify-between items-center  border-gray-200 px-5 py-3">
        <h3 class="text-lg font-semibold text-gray-800 dark:text-primary">
          {{ title }}
        </h3>
        <button
          type="button"
          class="text-gray-400 hover:text-gray-600 transition"
          @click="$modal.hide(id)"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
          </svg>
        </button>
      </div>

      <div class="px-5 pt-1">
        <Tabs default-value="personal-profile" class="w-full" orientation="horizontal">
          <TabsList :class="`grid w-full grid-cols-${(tabs || []).length} h-full overflow-y-auto overflow-x-hidden`">
            <TabsTrigger 
              v-for="(item, i) in (tabs || [])"
              :value="item.key"
            >
              {{item.label}}
            </TabsTrigger>
          </TabsList>
          <!-- Body -->
          <div class="overflow-y-auto -mx-5 px-5 pb-5" style="max-height: 700px;">
            <TabsContent 
              v-for="(item, i) in (tabs || [])"
              :value="item.key"
            >
              <slot :name="item.key" />
            </TabsContent>
          </div>
        </Tabs>
      </div>

      <!-- Footer (optional) -->
      <div v-if="noFooter === false">
        <div
          v-if="$slots.footer"
          class="border-t border-gray-200 bg-card px-5 py-3 flex justify-end gap-2"
        >
          <slot name="footer" />
        </div>
        <div v-else
          class="border-t border-gray-200 bg-card px-5 py-3 flex justify-end gap-2"
        >
          <DialogFooter>
            <Button type="submit" @click="onSubmit"
              class="me-2">
              {{submitLabel}}
            </Button>
            <Button type="submit"
              variant="secondary"
              v-if="cancelable !== false"
              @click="$modal.hide(id)"
            >
              Cancel
            </Button>
          </DialogFooter>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  id: { type: String, required: true, },
  title: { type: String },
  onSubmit: { },
  submitLabel: { type: String, default: 'Submit' },
  cancelable: { type: Boolean, default: true },
  noFooter: { type: Boolean, default: false },
  size: { type: String, default: 'w-11/12 max-w-lg max-h-9/10' }, // default responsif
  tabs: []
})
/*
  Jika size mengandung h-full, modal otomatis full screen dan
  sudutnya diubah jadi tidak rounded (biar rapi seperti page)
*/
const computedSize = computed(() => {
  return props.size.includes('h-full')
    ? `${props.size} rounded-none`
    : props.size
})
</script>
