<template>
  <b-modal 
    id="create" 
    title="Add New Application" 
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
      <div>
        <div class="flex gap-2 mb-2">
          <Checkbox v-model="model.IsCallbackOnOrganizationCreated" /> 
          <div>
            <span class="text-sm"> Callback on organization added</span>
            <div class="text-xs text-gray-400">
              Application will receive callback when organization added to this application.
            </div>
          </div>
        </div>
        <div v-if="model.IsCallbackOnOrganizationCreated">
          <input-text 
            label="Callback Url *" 
            v-model="model.OrganizationCreatedCallbackUrl" 
            :errors="errors?.OrganizationCreatedCallbackUrl" 
            multiline
            rows="2"
          />
        </div>
      </div>
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
      IsCallbackOnOrganizationCreated: false,
    },
    errors: {},
  }),
  mounted: function () {
  },
  methods: {
    submit: function () {
      this.isLoading = true;
      this.$http.post('/application/create', this.model)
        .then(()=> {
          this.$swal.success('Success!', 'Create application successful!')
          this.$modal.hide('create')
          useGridUrl('application-list')().load();
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