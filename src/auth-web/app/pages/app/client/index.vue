<template>
  <div>
    <b-grid-url
      url="/api/client/list"
      store-key="client-list"
      :buttons="[
        { label: 'ADD NEW', icon: 'ph:plus', showOnChecked: false, onClick: create},
        // { type: 'separator', showOnChecked: true},
        // { label: 'DELETE', icon: 'ph:trash-bold', showOnChecked: true, onClick: remove},
      ]"
      :columns="[
        { key: 'Id', label: 'Id', searchable: true,},
        { key: 'Name', label: 'Name', searchable: true,},
        { key: 'ApplicationName', label: 'Application Name', searchable: true,},
        { key: 'Secret', label: 'Secret', type: 'secret',},
        { key: 'IsActive', label: 'Active', type: 'check'},
      ]"
      :default-filter="[
      ]"
      :sort-list="[]"
      :actions="[
        { label: 'Edit', icon: 'ph:note-pencil-bold', onClick: (v) => edit(v)},
        { type: 'separator'},
        { label: 'Activate', icon: 'ph:check-circle-bold', visible: (v) => !v.IsActive, onClick: (v) => activate(v.Id, false)},
        { label: 'Deactivate', icon: 'ph:x-circle-bold', visible: (v) => v.IsActive, onClick: (v) => activate(v.Id, true)},
        { type: 'separator'},
        { label: 'Delete', icon: 'ph:trash-bold', onClick: (v) => remove(v.Id)},
      ]"
    />
    <Create />
    <Edit :data="selectedItem" />
  </div>
</template>

<script>
import Create from './create.x.vue'
import Edit from './edit.x.vue'
export default {
  components: { Create, Edit },
  data: () => ({
    selectedItem: {},
  }),
  computed: {
    app: function() {
      return useApp();
    }
  },
  mounted: function() {
    this.app.setMenus([{Title: 'Clients'}])
  },
  methods: {
    create: function() {
      return new Promise((resolve, reject) => {
        this.$modal.show('create')
        resolve();
      })
    },
    edit: function(v) {
        this.selectedItem = v;
      return new Promise((resolve, reject) => {
        this.$modal.show('edit')
        resolve();
      })
    },
    activate: function(v, y) {
      return new Promise((resolve, reject) => {
        this.$swal.confirm(y ? 'Deactivate Client' : 'Activate Client')
          .then(() => {
              this.$http.patch('/client/activation?id='+ v)
                .then(() => {
                  useGridUrl('client-list')().load();
                  resolve();
                })
                .catch(err => {
                  if(err.response) {
                    this.$swal.error('Failed!', err.response?.data)
                  }
                  reject()
                })
          })
          .catch(_ => resolve())
      })
    },
    remove: function(v) {
      return new Promise((resolve, reject) => {
        this.$swal.confirmDelete()
          .then(() => {
              this.$http.delete('/client/remove?id='+ v)
                .then(() => {
                  useGridUrl('client-list')().load();
                  resolve();
                })
                .catch(err => {
                  if(err.response) {
                    this.$swal.error('Failed!', err.response?.data)
                  }
                  reject()
                })
          })
          .catch(_ => resolve())
      })
    },
  }
}
</script>