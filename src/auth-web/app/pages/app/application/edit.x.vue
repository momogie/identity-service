<template>
  <b-modal 
    id="edit" 
    title="Edit Application" 
    @hidden="hidden" 
    @shown="shown" 
    @submit="submit" 
    :is-loading="isLoading"
  >
    <form>
      <input-text 
        label="Name *" 
        v-model="model.Name" 
        :errors="errors?.Name" 
      />
      <input-text 
        label="Url *" 
        v-model="model.Url" 
        :errors="errors?.Url" 
      />
      <input-text 
        label="Redirect Url *" 
        v-model="model.RedirectUrl" 
        :errors="errors?.RedirectUrl" 
        multiline
        rows="2"
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
      this.$http.patch('/application/edit?id='+ this.data.Id, this.model)
        .then(()=> {
          this.$swal.success('Success!', 'Edit application successful!')
          this.$modal.hide('edit')
          useGridUrl('application-list')().load();
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