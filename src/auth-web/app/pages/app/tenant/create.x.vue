<template>
  <b-modal 
    id="create" 
    title="Add New Tenant" 
    @hidden="hidden" 
    @submit="submit" 
    :is-loading="isLoading"
  >
    <form>
      <input-text 
        label="Code *" 
        v-model="model.Code" 
        :errors="errors?.Code" 
      />
      <input-text 
        label="Name *" 
        v-model="model.Name" 
        :errors="errors?.Name" 
      />
      <input-text 
        label="Description *" 
        v-model="model.Description" 
        :errors="errors?.Description" 
        multiline
        rows="3"
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
      this.$http.post('/tenant/create', this.model)
        .then(()=> {
          this.$swal.success('Success!', 'Create tenant successful!')
          this.$modal.hide('create')
          useGridUrl('tenant-list')().load();
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