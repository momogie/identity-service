<template>
  <b-modal 
    id="edit" 
    title="Edit Tenant" 
    @hidden="hidden" 
    @shown="shown" 
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
  props:['data'],
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
      this.$http.patch('/organization/edit?id='+ this.data.Id, this.model)
        .then(()=> {
          this.$swal.success('Success!', 'Edit organization successful!')
          this.$modal.hide('edit')
          useGridUrl('organization-list')().load();
        })
        .catch(err => {
          this.errors = err?.response?.data?.Errors;
        })
        .finally(_ => this.isLoading = false)
    },
    shown: function() {
      this.model = {...(this.data || {})}
      this.errors = {};
    },
    hidden: function () {
      this.model = {};
      this.errors = {};
    }
  }
}
</script>