<template>
  <AlertDialog :open="$alertState.show">
    <AlertDialogContent class="w-96">
      <AlertDialogHeader>
        <AlertDialogTitle>{{ $alertState.title }}</AlertDialogTitle>
        <AlertDialogDescription>
          {{ $alertState.message }}
        </AlertDialogDescription>
      </AlertDialogHeader>

      <AlertDialogFooter>
        <!-- <AlertDialogCancel @click="close">Ok</AlertDialogCancel> -->
        <AlertDialogAction @click="close">Ok</AlertDialogAction>
      </AlertDialogFooter>
    </AlertDialogContent>
  </AlertDialog>
</template>

<script>
export default {
  data: () => ({

  }),
  methods: {
    close: function() {
      this.$alertState.show = false;
    },
  }
}
</script>
