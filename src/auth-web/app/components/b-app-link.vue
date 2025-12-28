<template>
  <nuxt-link :to="link"
    :exact-active-class="exactActiveClass || ''"
    :active-class="activeClass || ''"
  >
    <slot />
  </nuxt-link>
</template>

<script>
export default {
  props: ['to', 'exact-active-class', 'active-class'],
  computed:{
    wsId: function() {
      return this.$route.params.id || this.$route.query?.organizationid;
    },
    link: function() {
      return `/app/${this.wsId}${this.to}`;
    },
  }
}
</script>