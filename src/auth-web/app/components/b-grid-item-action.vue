<script setup >

</script>

<template>
  <DropdownMenu>
    <DropdownMenuTrigger as-child>
      <Icon name="ph:dots-three-outline-fill" size="16" />
    </DropdownMenuTrigger>
    <DropdownMenuContent class="w-56 mr-10">
      <DropdownMenuLabel>Actions</DropdownMenuLabel>
      <DropdownMenuSeparator />
      <DropdownMenuGroup>
        <template v-for="(item,i) in list" :key="i">
          <DropdownMenuSeparator v-if="item.type?.toLowerCase() == 'separator'" />
          <DropdownMenuItem v-else
            @click="() => item.onClick(data)"
          >
            <!-- <User class="mr-2 h-4 w-4" /> -->
            <Icon :name="item.icon" size="16"/>
            <span>{{item.label}}</span>
          </DropdownMenuItem>
        </template>
      </DropdownMenuGroup>
    </DropdownMenuContent>
  </DropdownMenu>
</template>

<script>
export default {
  props: ['actions', 'data'],
  computed: {
    list: function() {
      return (this.actions || []).filter(a => {
        if (a.visible && typeof a.visible == 'function') {
          return a.visible(this.data);
        }
        if(a.visible && typeof a.visible == 'boolean') {
          return a.visible;
        }
        return true;
      })
    }
  },
}
</script>