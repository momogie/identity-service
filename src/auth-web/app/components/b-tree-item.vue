<template>
  <ul class="chart-list">
    <li v-for="(item, i) in (data.childrenList || [])" class="chart-list-item">
      <div class="flex w-full align-middle justify-center items-center">
        <ButtonGroup>
          <Button variant="outline">
            {{ item.name }}
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
                <template v-for="(x,i) in (actions || [])" :key="i">
                  <DropdownMenuSeparator v-if="x.type?.toLowerCase() == 'separator'" />
                  <DropdownMenuItem v-else
                    @click="() => x.onClick(item)"
                  >
                    <!-- <User class="mr-2 h-4 w-4" /> -->
                    <Icon :name="x.icon" size="16"/>
                    <span>{{x.label}}</span>
                  </DropdownMenuItem>
                </template>
              </DropdownMenuGroup>
            </DropdownMenuContent>
          </DropdownMenu>
        </ButtonGroup>
      </div>
      <b-tree-item 
        :data="item"
        :actions="actions"
        v-if="item.childrenList.length > 0"
      />
    </li>
  </ul>
</template>

<script>
export default {
  name: 'b-tree-item',
  props: ['data', 'actions'],
  data: () => ({
    selectedItem: {},
  }),
}
</script>