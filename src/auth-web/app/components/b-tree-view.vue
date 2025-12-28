<template>
  <div class="w-full">
    <div class="flex justify-between">
      <div class="flex">
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
    <div class="overflow-x-auto overflow-y-auto mt-5 w-full border rounded" :style="`height: calc(100vh - 150px); max-width: calc(100vw - ${sidebarState ? 290 : 80}px);`">
      <div class="w-full h-60 justify-items-center justify-center justify-self-center p-10"
        v-if="ds.isLoading"
      >
        <Button variant="secondary" size="sm">
          <Spinner />
          Loading...
        </Button>
      </div>
      <div class="relative" v-if="!ds.isLoading">
        <div class="h-full" style="resize: vertical;">
          <div class="c-tree-chart" ref="c-tree-chart-org">
            <ul class="chart-list">
              <li class="chart-list-item">
                <div class="flex w-full align-middle justify-center items-center">
                  <ButtonGroup>
                    <Button variant="outline">
                      {{ds.data?.name}}
                    </Button>
                    <DropdownMenu>
                      <DropdownMenuTrigger as-child>
                        <Button variant="outline" size="icon" aria-label="More Options">
                          <Icon name="ph:dots-three-vertical-bold" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end" class="w-52">
                        <DropdownMenuLabel>Actions</DropdownMenuLabel>
                        <DropdownMenuSeparator />
                        <DropdownMenuGroup>
                          <template v-for="(item,i) in (actions || [])" :key="i">
                            <DropdownMenuSeparator v-if="item.type?.toLowerCase() == 'separator'" />
                            <DropdownMenuItem v-else
                              @click="() => item.onClick(ds.data || {})"
                            >
                              <!-- <User class="mr-2 h-4 w-4" /> -->
                              <Icon :name="item.icon" size="16"/>
                              <span>{{item.label}}</span>
                            </DropdownMenuItem>
                          </template>
                        </DropdownMenuGroup>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </ButtonGroup>
                </div>
                <TreeItem 
                  :data="ds.data"
                  :actions="actions"
                />
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import TreeItem from './b-tree-item.vue'
export default {
  components: { TreeItem },
  props: [
    'controller', 'action',
    'configurationPages', 'backable', 
    'buttons', 'actions', 'defaultFilter',
    'importTemplates', 'exportTemplates', 'exportFilters'
  ],
  data: () => ({
    isLoading: false,
    checkedList: [],
    isLoadingButtons: {},
    selectedKeyword: {},
    keyword: null,
    debounce: null,
    sort: {
      by: '',
      direction: 'desc'
    },
    columnList: [],
    data: {
      childrenList: [],
    }
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
      return useTreeSource();
    }
  },
  mounted: function() {
    this.ds.setActionController(this.controller, this.action)
    this.ds.load();
  },
  methods: {
    load: function() {
      this.isLoading = true
      this.$api.submit(this.controller, this.action, {})
        .then(({data}) => {
          this.data = data
        })
        .finally(_ => this.isLoading = false)
    },
    refresh: function() {
      this.ds.load();
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

<style lang="scss">

$border-width: 1px;
$reverse: false;

.org-chart {
  display: flex;
  justify-content: center;
  margin-top: 40px;
}


.c-tree-chart {
  @if $reverse {
    transform: rotate(180deg);
    transform-origin: 50%;
  }
}

.c-tree-chart ul.chart-list {
  position: relative;
  padding: 1em 0;
  white-space: nowrap;
  margin: 0 auto;
  text-align: center;
  &::after {
    content: "";
    display: table;
    clear: both;
  }

  .chart-dropdown-item {
    display: block;
    width: 100%;
    padding: 0.5rem 1.5rem;
    clear: both;
    font-weight: 400;
    color: #212529;
    white-space: nowrap;
    background-color: transparent;
    border-bottom: solid 1px rgba(0, 0, 0, .06);
    border: 0;cursor: pointer;
    display: flex;

    .chart-dropdown-item-icon {
      width: 30px;
      justify-self: start;
    }
  }
}


.c-tree-chart li.chart-list-item {
  display: inline-block; // need white-space fix
  vertical-align: top;
  text-align: center;
  list-style-type: none;
  position: relative;
  padding: 1em 0.5em 0 0.5em;
  &::before,
  &::after {
    content: "";
    position: absolute;
    top: 0;
    right: 50%;
    border-top: $border-width solid #ccc;
    width: 52%;
    height: 1em;
  }
  &::after {
    right: auto;
    left: 50%;
    border-left: $border-width solid #ccc;
  }
  &:only-child::after,
  &:only-child::before {
    display: none;
  }
  &:only-child {
    padding-top: 0;
  }
  &:first-child::before,
  &:last-child::after {
    border: 0 none;
  }
  &:last-child::before {
    border-right: $border-width solid #ccc;
    border-radius: 0 5px 0 0;
  }
  &:first-child::after {
    border-radius: 5px 0 0 0;
  }
}

.c-tree-chart {
  .organization-box {
    ul.dropdown-menu {
      &::before {
        border: none !important;
      }
    }
  }
}

.c-tree-chart ul.chart-list ul.chart-list::before {
  content: "";
  position: absolute;
  top: 0;
  left: 50%;
  border-left: $border-width solid #ccc;
  width: 0;
  height: 1em;
}

td {
  .dropdown-menu {
    border: none;
    box-shadow: 0 0 11px rgb(0 0 0 / 20%);
    .dropdown-item {
      font-size: 14px;
    }
  }
}

.tree-chart-container {
  border: solid 1px rgba($color: #000000, $alpha: 0.1);
  // display: flex;
  position: relative;
  flex: 1;
  //height: 56vh;
  border-radius: 10px;
  overflow: auto;
  justify-content: center;
}

/* width */
.tree-chart-container::-webkit-scrollbar {
  width: 7px;
  height: 7px;
}

/* Track */
.tree-chart-container::-webkit-scrollbar-track {
  background: #f1f1f1;
}

/* Handle */
.tree-chart-container::-webkit-scrollbar-thumb {
  background: rgba(0, 0, 0, 0.2);
  border-radius: 5px;
}

/* Handle on hover */
.tree-chart-container::-webkit-scrollbar-thumb:hover {
  background: rgba(0, 0, 0, 0.3);
}
</style>