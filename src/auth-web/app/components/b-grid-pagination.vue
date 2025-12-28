<template>
  <div class="flex justify-between">
    <div>
      <div class="flex-1 text-sm text-muted-foreground">
        Showing 
        {{(ds.filter.pageNumber * ds.filter.maximumResult) - ds.filter.maximumResult + 1 }}
        to 
        <span v-if="ds.data.totalRows > ds.filter.pageNumber * ds.filter.maximumResult">
          {{ds.filter.pageNumber * ds.filter.maximumResult}} 
        </span>
        <span v-else>
          {{ds.data.totalRows}}
        </span>
        of {{ds.data.totalRows}} row(s). 
      </div>
    </div>
    <div class="flex">
      <div>
        <Select :placeholder="ds.filter?.maximumResult || 25" >
          <SelectTrigger>
            <SelectValue :placeholder="`${ds.filter?.maximumResult || 25}`" />
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
        :items-per-page="ds.filter.maximumResult" 
        :total="ds.data.totalRows" 
        :default-page="ds.filter.pageNumber"
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
  </div>
</template>

<script>
import { useGridSource } from '~/stores/grid-source';

export default {
  computed: {
    ds: function() {
      return useGridSource();
    }
  }
}
</script>