<script setup lang="ts">
defineProps({
  list: { type: Array<any> }
})
</script>

<template>
  <DropdownMenu>
    <DropdownMenuTrigger as-child>
      <Button variant="outline" size="icon-sm">
        <Icon name="ph:gear-bold" size="16" />
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent class="w-56 mr-10">
      <DropdownMenuLabel>Configurations</DropdownMenuLabel>
      <DropdownMenuSeparator />
      <DropdownMenuGroup>
        <template v-for="(item, i) in list">
          <DropdownMenuItem v-if="item.type == 'page'"
            @click="() => $router.push('/app/' + ($route.params.id || $route.query.organizationid) + item.link)">
            <!-- <User class="mr-2 h-4 w-4" /> -->
            <Icon :name="item.icon" size="16" />
            <span>{{ item.label }}</span>
          </DropdownMenuItem>
          <DropdownMenuItem v-if="item.type == 'button'" @click="() => item.onClick()">
            <Icon :name="item.icon" size="16" />
            <span>{{ item.label }}</span>
          </DropdownMenuItem>
          <DropdownMenuSeparator v-if="item.type == 'separator'" />
        </template>
      </DropdownMenuGroup>
    </DropdownMenuContent>
  </DropdownMenu>
</template>