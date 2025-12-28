<template>
    <div class="px-5 pt-1 flex w-full flex-col gap-6">
        <Tabs default-value="default" class="w-full" orientation="horizontal">
            <TabsList :class="`grid w-full grid-cols-${(tabs || []).length} h-full overflow-y-auto overflow-x-hidden`">
                <TabsTrigger v-for="(item, i) in (tabs || [])" :value="item.key">
                    {{ item.label }}
                </TabsTrigger>
            </TabsList>
            <!-- Body -->
            <div class="overflow-y-auto -mx-5 px-5 pb-5" style="max-height: 400px;">
                <TabsContent v-for="(item, i) in (tabs || [])" :value="item.key">
                    <slot :name="item.key"></slot>
                </TabsContent>
            </div>
        </Tabs>
    </div>
</template>
<script setup>
const props = defineProps({
    tabs: []
})
</script>