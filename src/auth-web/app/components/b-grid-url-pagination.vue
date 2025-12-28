<template>
  <div class="flex justify-between">
    <div>
      <div class="flex-1 text-sm text-muted-foreground">
        Showing 
        {{(ds.data.Page * ds.data.Length) - ds.data.Length + 1 }}
        to 
        <span v-if="ds.data.Filtered > ds.data.Page * ds.data.Length">
          {{ds.data.Page * ds.data.Length}} 
        </span>
        <span v-else>
          {{ds.data.Filtered}}
        </span>
        of {{ds.data.Filtered}} row(s). 
      </div>
    </div>
    <div class="flex">
      <div>
        <Select :placeholder="ds.data?.Length || 25" >
          <SelectTrigger>
            <SelectValue :placeholder="`${ds.data?.Length || 25}`" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem :value="item" v-for="(item, i) in [10, 25, 50, 100]"
              @click="() => ds.setLength(item)"
            >
              {{item}}
            </SelectItem>
          </SelectContent>
        </Select>
      </div>
      <Pagination v-slot="{ page }" 
        :items-per-page="ds.data.Length" 
        :total="ds.data.Filtered" 
        :default-page="ds.data.Page"
        @update:page="(v) => ds.setPage(v)"
      >
        <PaginationContent v-slot="{ items }">
          <PaginationPrevious />
          <template v-for="(item, index) in items" :key="index">
            <PaginationItem
              v-if="item.type === 'page'"
              :value="item.value"
              :is-active="item.value === page"
            >
              {{ item.value }}
            </PaginationItem>
          </template>

          <PaginationEllipsis :index="4" />

          <PaginationNext />
        </PaginationContent>
      </Pagination>
    </div>
    <!-- {{ this.storeKey }} -->
  </div>
</template>

<script>
// import { useGridSource } from '~/stores/grid-source';

export default {
  props: ['storeKey'],
  computed: {
    ds: function() {
      return useGridUrl(this.storeKey)();
    }
  }
}
</script>