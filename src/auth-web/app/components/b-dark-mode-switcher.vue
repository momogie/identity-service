<template>
  <div>
    <Switch
      @update:model-value="toggle"
    >
    </Switch>
  </div>
</template>

<script>
export default {
  computed: {
    isDarkMode: function() {
      return document.documentElement.classList.contains('dark');
    }
  },
  methods: {
    toggle: function(e) {
      if(e) {
        document.documentElement.classList.add('dark')
      }else {
        document.documentElement.classList.remove('dark')
      }
    }
  }
}
</script>