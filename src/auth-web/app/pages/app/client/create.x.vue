<template>
  <b-modal 
    id="create" 
    title="Add New Client" 
    @hidden="hidden" 
    @submit="submit" 
    :is-loading="isLoading"
  >
    <form>
      <input-text 
        label="Name *" 
        v-model="model.Name" 
        :errors="errors?.Name" 
      />
    </form>
  </b-modal>
</template>

<script>

export default {
  data: () => ({
    isLoading: false,
    model: {
      Name: null,
      Description: null,
      Code: null,
    },
    errors: {},
  }),
  mounted: function () {
  },
  methods: {
    submit: function () {
      this.isLoading = true;
      this.$http.post('/client/create', this.model)
        .then(()=> {
          this.$swal.success('Success!', 'Create client successful!')
          this.$modal.hide('create')
          useGridUrl('client-list')().load();
        })
        .catch(err => {
          this.errors = err?.response?.data?.Errors;
        })
        .finally(_ => this.isLoading = false)
    },
    hidden: function () {
      this.model = {};
      this.errors = {};
    }
  }
}
</script>