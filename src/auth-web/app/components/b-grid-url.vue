<template>
  <div class="w-full">
    <div class="flex justify-between">
      <div class="flex">
        <Popover>
          <PopoverTrigger as-child>
            <Button variant="outline" size="icon-sm" class="mr-1" >
              <Icon name="ph:faders-fill" size="16"/>
            </Button>
          </PopoverTrigger>
          <PopoverContent class="w-60"
            :align="`start`"
          >
            <div class="grid">
              <div class="space-y-2 border-b -mx-4 px-3 pb-3 flex">
                <!-- <Checkbox /> -->
                <h4 class="font-medium leading-none text-sm mt-1">
                  Field to Show 
                </h4>
              </div>
              <!-- <DropdownMenuSeparator /> -->
              <div class="max-h-64 overflow-y-auto -mx-4 ps-3">
                <div v-for="(col, i) in (columnList || [])">
                  <Checkbox :id="`col_show_${i}`" 
                    v-model="col.visible"
                    @update:model-value="(v) => {
                      col.visible = v;
                    }"
                  >
                  </Checkbox>
                  <span class="text-xs ms-2">{{ col.label }}</span>
                </div>
              </div>
            </div>
          </PopoverContent>
        </Popover>
        <div class="grid w-80">
          <InputGroup class="text-sm h-8">
            <InputGroupInput placeholder="Enter search query" 
              @keyup="search"
            />
            <InputGroupAddon align="inline-start">
              <DropdownMenu>
                <DropdownMenuTrigger as-child>
                  <InputGroupButton variant="ghost" class="text-xs">
                    <span v-if="selectedKeyword?.key">
                      {{ selectedKeyword.label }}
                    </span>
                    <span v-else>
                      Search In... 
                    </span>
                    <!-- <Icon name="ph:chevron-down-bold" size="12"/> -->
                    <!-- <ChevronDownIcon class="size-3" /> -->
                  </InputGroupButton>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" class="[--radius:0.95rem]">
                  <DropdownMenuItem v-for="(item, i) in (columns || []).filter(p => p.searchable === true)"
                    @click="() => selectedKeyword = item"
                  >
                    {{ item.label }}
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </InputGroupAddon>
          </InputGroup>
        </div>
      </div>
      <div class="flex h-5">
        <template v-for="(item, i) in (buttons || []).filter(p => (p.showOnChecked == null || !p.showOnChecked || (p?.showOnChecked === true  && checkedList.length > 0 )))">
          <Separator orientation="vertical" class="ms-2 mt-1" 
            v-if="item.type?.toLowerCase() == 'separator'"
          />
          <Button variant="outline" size="sm" 
            @click="(e) => onTopButtonClick(e, checkedList, item.onClick, i)"
            :disabled="isLoadingButtons[`btn_top_${i}`] === true"
            class="ms-2"
            v-else
          >
            <Icon :name="item.icon" size="16"/>
            {{item.label}}
          </Button>
        </template>
        <Separator orientation="vertical" class="mx-2 mt-1" v-if="(buttons || []).length > 0"/>
        <TooltipProvider 
          v-if="(exportTemplates || []).length > 0"
        >
          <Tooltip>
            <TooltipTrigger as-child>
              <Button variant="outline" size="icon-sm" class="mr-1" 
                @click="$modal.show('export-data')"
              >
                <Icon name="ph:download-bold" size="16"/>
              </Button>
            </TooltipTrigger>
            <TooltipContent>
              <p>Export</p>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider> 
        <TooltipProvider 
          v-if="(importTemplates || []).length > 0"
        >
          <Tooltip>
            <TooltipTrigger as-child>
              <Button variant="outline" size="icon-sm" class="mr-1" 
                @click="$modal.show('import-data')"
              >
                <Icon name="ph:upload-bold" size="16"/>
              </Button>
            </TooltipTrigger>
            <TooltipContent>
              <p>Import</p>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger as-child>
              <Button variant="outline" size="icon-sm" @click="refresh">
                <Icon name="ph:arrows-clockwise-bold" size="16"/>
              </Button>
            </TooltipTrigger>
            <TooltipContent>
              <p>Refresh</p>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
        <Separator orientation="vertical" class="mx-2 mt-1" v-if="configurationPages"/>
        <b-grid-setup :list="configurationPages" v-if="configurationPages" />
        <Separator orientation="vertical" class="mx-2 mt-1" v-if="backable !== undefined && backable !== false"/>
        <TooltipProvider v-if="backable !== undefined && backable !== false">
          <Tooltip>
            <TooltipTrigger as-child>
              <Button variant="outline" size="sm" @click="() => $router.back()">
                <Icon name="ph:arrow-bend-double-up-left-bold" size="16"/>
                Go Back
              </Button>
            </TooltipTrigger>
            <TooltipContent>
              <p>Go Back</p>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </div>
    </div>
    <div class="overflow-x-auto overflow-y-auto mt-5 w-full" 
      :style="`height: ${height ?? 'calc(100vh - 192px)'}; max-width: calc(100vw - ${sidebarState ? 300 : 80}px);`"
    >
      <table class="w-full text-xs  border-gray-200 rounded-lg">
        <thead class=" text-gray-700 uppercase sticky top-0 z-1 bg-background">
          <tr class=" border-b-2">
            <th class="ps-0 pe-1 sticky left-0 z-1 w-2 bg-background">
              <Checkbox id="grid_check_all" class="m-0" 
                @update:model-value="(v) => checkAll(v)" 
                :model-value="checkedList.length > 0 && checkedList.length == (ds.data.data || []).length"
              />
            </th>
            <th class="px-2 py-1 text-left truncate max-w-[320px]" 
              v-for="col in (columnList || []).filter(p => p.visible !== false)"
            >
              <div class="flex cursor-default">
                <div 
                  @click="() => toggleSort(col.key)"
                >
                  {{col.label}}
                </div>
                <div class="flex-1">
                  <Icon v-if="sort.by == col.key && sort.direction == 'asc'" 
                    name="ph:caret-up-fill" size="18"
                  />
                  <Icon v-else-if="sort.by == col.key && sort.direction == 'desc'" 
                    name="ph:caret-down-fill" size="18"
                  />
                </div>
                <div>
                  <Popover v-if="col.filterable === true">
                    <PopoverTrigger as-child>
                      <Icon name="ph:sliders-horizontal-bold" size="16" class="ms-1 cursor-pointer"/>
                    </PopoverTrigger>
                    <PopoverContent class="w-80">
                      <div class="grid gap-4">
                        <div class="space-y-2">
                          <h4 class="font-medium leading-none">
                            Filter {{ col.label }}
                          </h4>
                        </div>
                        <div class="grid gap-2">
                          <input-date-range v-if="col.type === 'date'" />
                          <input-combobox v-if="col.type === 'datasource'" />
                        </div>
                      </div>
                    </PopoverContent>
                  </Popover>
                </div>
              </div>
            </th>
            <th class="px-2 py-1 text-left sticky right-0 w-2 bg-background"></th>
          </tr>
        </thead>
        <tbody class="divide-y [&>tr:nth-child(even)]:bg-gray-100 dark:[&>tr:nth-child(even)]:bg-gray-800 dark:text-gray-100">
          <tr>
            <td class="text-center" :colspan="`${columns?.length + 2}`"
              v-if="ds.isLoading"
            >
              <div class="w-full h-60 justify-items-center justify-center justify-self-center p-10">
                <Button variant="secondary" size="sm">
                  <Spinner />
                  Loading...
                </Button>
              </div>
            </td>
          </tr>
          <tr v-for="(item, i) in (ds.data.Items || [])"
            v-if="!ds.isLoading"
          >
            <td class="px-1 sticky left-0  bg-background">
              <Checkbox :id="`terms_1_${i}`"
                @update:model-value="(v) => check(v, (keys || []).length > 0 ? ((keys || []).map(p => item[p]).join(';')) : item.Id)" 
                :model-value="checkedList.some(p => p == ((keys || []).length > 0 ? ((keys || []).map(p => item[p]).join(';')) : item.Id))"
              />
            </td>
            <td class="px-2 py-1 truncate max-w-[120px]" v-for="col in (columnList || []).filter(p => p.visible !== false)"
              :class="col.class"
            >
              <span v-if="col.type == 'date'">
                {{ $func.formatDate(item[col.key]) }}
              </span>
              <span v-else-if="col.type == 'datetime'">
                {{ $func.formatDateTime(item[col.key]) }}
              </span>
              <span v-else-if="col.type == 'time'">
                {{ $func.formatTime(item[col.key]) }}
              </span>
              <span v-else-if="col.type == 'day'">
                {{ $func.formatDay(item[col.key]) }}
              </span>
              <span v-else-if="col.type == 'money'">
                {{ $func.formatMoney(item[col.key]) }}
              </span>
              <span v-else-if="col.type == 'check'">
                <Checkbox
                  :value="() => item[col.key]"
                  :default-value="item[col.key]"
                  class="pointer-events-none"
                />
                <!-- <Icon name="ph:check-square-bold" size="16" v-if="item[col.key] === true"
                  class="text-green-600"
                />
                <Icon name="ph:x-square-bold" size="16" v-else
                  class="text-red-600"
                /> -->
              </span>
              <span v-else>
                {{ item[col.key] }}
              </span>
            </td>
            <td class="px-2 py-1 sticky right-0  bg-background">
              <b-grid-item-action 
                :data="item"
                :actions="actions"
              />
            </td>
          </tr>
        </tbody>
      </table>
      <Empty class="from-muted/50 to-background from-30%"
        v-if="!ds.isLoading && ds.data.Items.length == 0"
      >
        <EmptyHeader>
          <EmptyMedia variant="icon">
            <Icon name="ph:note-blank" size="43" />
          </EmptyMedia>
          <EmptyTitle>No Data Available</EmptyTitle>
          <EmptyDescription>
            You're all caught up. New data will appear here.
          </EmptyDescription>
        </EmptyHeader>
        <EmptyContent>
          <Button variant="outline" size="sm" @click="refresh">
            <Icon name="ph:arrows-clockwise-bold" size="16"/>
            Refresh
          </Button>
        </EmptyContent>
      </Empty>
    </div>
    <b-grid-url-pagination :store-key="storeKey ??  url" class="mt-2" />
    <!-- <shared-import-data />
    <shared-export-data :templates="exportTemplates" :filters="exportFilters"/> -->
  </div>
</template>

<script>
export default {
  props: [
    'url', 'columns', 'sortList', 'keys', 'height', 'storeKey',
    'configurationPages', 'backable', 
    'buttons', 'actions', 'defaultFilter',
    'importTemplates', 'exportTemplates', 'exportFilters'
  ],
  data: () => ({
    checkedList: [],
    isLoadingButtons: {},
    selectedKeyword: {},
    keyword: null,
    debounce: null,
    sort: {
      
    },
    columnList: [],
  }),
  setup() {
    const sidebarState = useCookie('sidebar_state')
    return { sidebarState }
  },
  computed: {
    app: function() {
      return useApp();
    },
    ds: function() {
      return useGridUrl(this.storeKey ?? this.url)();
    }
  },
  mounted: function() {
    this.ds.setUrl(this.url)
    this.ds.setFilter(this.defaultFilter || [])
    this.ds.load();
    this.columnList = this.columns.map(p => ({...p, visible: true}))
    if((this.columnList || []).filter(p => p.searchable === true).length > 0) {
      this.selectedKeyword = this.columnList?.filter(p => p.searchable === true)[0];
    }
  },
  methods: {
    search: function(e) {
      if(this.debounce) {
        clearInterval(this.debounce)
      }
      this.debounce = setTimeout(() => {
        var filter = [
          ...(this.defaultFilter || []),
          [this.selectedKeyword.key, 'like', `%${e.target.value ?? ''}%`]
        ]
        this.ds.setUrl(this.url)
        this.ds.setFilter(filter)
        // this.ds.setSort(this.sortList);
        this.ds.load();
      }, 300);
    },
    toggleSort: function(v) {
      // this.sort.by = v;
      // this.sort.direction = this.sort.direction == 'asc' ? 'desc' : 'asc';
      // this.ds.setSort([`${v} ${this.sort.direction}`]);
      this.ds.load();
    },
    refresh: function(e) {
      e.preventDefault();
      this.ds.setUrl(this.url)
      // this.ds.setSort(this.sortList);
      this.ds.load();
    },
    checkAll: function(check = false) {
      if(check)
        this.checkedList =  (this.ds.data?.data || []).map(p => (this.keys || []).length > 0 ? ((this.keys || []).map(c => p[c]).join(';')) : p.Id)
      else
        this.checkedList = [];
    },
    check: function(check = false, id) {
      if(check)
        this.checkedList.push(id)
      else 
        this.checkedList = (this.ds.data?.data || []).filter(p => this.checkedList.some(c => c == (this.keys || []).length > 0 ? ((this.keys || []).map(c => p[c]).join(';')) : p.Id)).filter(p => ((this.keys || []).length > 0 ? ((this.keys || []).map(c => p[c]).join(';')) : p.Id) != id ).map(p => (this.keys || []).length > 0 ? ((this.keys || []).map(c => p[c]).join(';')) : p.Id)
    },
    onTopButtonClick: function(e, checkedList, onClick, i) {
      this.isLoadingButtons[`btn_top_${i}`] = true;
      onClick(e, checkedList)?.finally(_ => {
        this.checkedList = [];
        this.isLoadingButtons[`btn_top_${i}`] = false;
      })
    },
  }
}
</script>