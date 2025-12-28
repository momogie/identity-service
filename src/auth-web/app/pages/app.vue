<script lang="ts">
export const description
  = "A sidebar that collapses to icons."
export const iframeHeight = "800px"
export const containerClass = "w-full h-full"
</script>

<script setup lang="ts">
import AppSidebar from '~/components/shadcn/AppSidebar.vue'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/shadcn/components/ui/breadcrumb'
import { Separator } from '@/components/shadcn/components/ui/separator'
import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from '@/components/shadcn/components/ui/sidebar'
const app = useApp();

definePageMeta({
  middleware: [ 'user' ],
  // pageTransition: {
  //   name: "zoom",
  //   mode: "out-in"
  // },
})
</script>

<template>
  <SidebarProvider>
    <AppSidebar />
    <SidebarInset>
      <header class="flex h-16 shrink-0 items-center gap-2 transition-[width,height] ease-linear group-has-data-[collapsible=icon]/sidebar-wrapper:h-12 border-b">
        <div class="flex items-center gap-2 px-4 w-full">
          <SidebarTrigger class="-ml-1" />
          <Separator
            orientation="vertical"
            class="mr-2 data-[orientation=vertical]:h-4"
          />
          <div class="flex-1">
            <Breadcrumb class="flex-1">
              <BreadcrumbList>
                <template v-for="(item, i) in app.list">
                  <BreadcrumbItem class="hidden md:block">
                    <BreadcrumbLink href="#">
                      {{item['Title']}}
                    </BreadcrumbLink>
                  </BreadcrumbItem>
                  <BreadcrumbSeparator class="hidden md:block" 
                    v-if="app.list.length > i+1"
                  />
                </template>
              </BreadcrumbList>
            </Breadcrumb>
          </div>
          <b-theme-switcher />
          <b-header-user-info></b-header-user-info>
        </div>
      </header>
      <div class="flex flex-1 flex-col gap-4 p-4">
        <nuxt-page></nuxt-page>
      </div>
    </SidebarInset>
  </SidebarProvider>
</template>
