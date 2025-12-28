<script lang="ts">
export const description
  = "A sidebar that collapses to icons."
export const iframeHeight = "800px"
export const containerClass = "w-full h-full"
</script>

<script setup lang="ts">
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
    <SidebarInset>
      <header class="flex h-16 shrink-0 items-center gap-2 transition-[width,height] ease-linear group-has-data-[collapsible=icon]/sidebar-wrapper:h-12 border-b">
        <div class="flex items-center gap-2 px-4 w-full">
          <!-- <SidebarTrigger class="-ml-1" />
          <Separator
            orientation="vertical"
            class="mr-2 data-[orientation=vertical]:h-4"
          /> -->
          <div class="flex-1">
            <Breadcrumb class="flex-1">
              <BreadcrumbList>
                <BreadcrumbItem class="hidden md:block">
                  <BreadcrumbLink href="#">
                    Identity Service
                  </BreadcrumbLink>
                </BreadcrumbItem>
                <!-- <BreadcrumbSeparator class="hidden md:block" />
                <BreadcrumbItem>
                  <BreadcrumbPage>Data Fetching</BreadcrumbPage>
                </BreadcrumbItem> -->
              </BreadcrumbList>
            </Breadcrumb>
          </div>
          <b-theme-switcher />
          <b-header-user-info></b-header-user-info>
        </div>
      </header>
      <div class="mt-10">
        <div class="flex items-center justify-between ms-5">
          <div class="space-y-1">
            <h2 class="text-2xl font-semibold tracking-tight"> Listen Now </h2>
            <p class="text-sm text-muted-foreground"> Top picks for you. Updated daily. </p>
          </div>
        </div>
        <div class="grid grid-cols-6 gap-4 p-4 pt-0 mt-5">
          <div v-for="(item, i) in 10">
            <Card 
            >
              <CardHeader>
                <CardTitle>Card Title</CardTitle>
                <CardDescription>Card Description</CardDescription>
              </CardHeader>
              <CardContent>
                <p>Card Content</p>
              </CardContent>
              <CardFooter>
                <p>Card Footer</p>
              </CardFooter>
            </Card>
          </div>
          <!-- <div class="grid auto-rows-min gap-4 md:grid-cols-3">
            <div class="bg-muted/50 aspect-video rounded-xl" />
            <div class="bg-muted/50 aspect-video rounded-xl" />
            <div class="bg-muted/50 aspect-video rounded-xl" />
          </div>
          <div class="bg-muted/50 min-h-[100vh] flex-1 rounded-xl md:min-h-min" /> -->
        </div>
      </div>
    </SidebarInset>
  </SidebarProvider>
</template>
