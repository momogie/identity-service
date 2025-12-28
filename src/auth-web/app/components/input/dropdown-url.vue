<template>
  <div :class="col ? ('form-group col-' + col) : 'form-group'">
    <label class="d-block">{{(label || 'Payroll Group')}}</label>
    <div>
      <input-multiselect
        v-model="tempValue"
        :options="list"
        :close-on-select="true"
        :clear-on-select="false"
        :preserve-search="true"
        open-direction="bottom"
        :placeholder="placeholder || 'Search Payroll Group'"
        :searchable="true"
        label="Name"
        track-by="Id"
        trackBy="Id"
        :hide-selected="true"
        :internal-search="false"
        :loading="isLoading"
        @search-change="search"
        @open="open"
        @clear="clear"
        :select="change"
        :class="cClass"
        :multiple="multiple !== undefined || false"
        select-label=""
        deselect-label=""
      />
    </div>
    <div class="invalid-feedback d-block" v-if="errors">
      {{ errors[0] }}
    </div>
  </div>
</template>

<script>
export default {
  model: {
    prop: 'modelValue',
    event: 'update',
  },
  emits: ['update:modelValue'],
  props: [
    'modelValue', 'label', 'col', 'description', 'placeholder', 'onSelect', 'errors', 'multiple',
    'url'
  ],
  data: () => ({
    isLoading: false,
    list: [],
    tempValue: null,
    debounce: null,
  }),
  computed: {
    cClass: function() {
      return (this.errors ? 'is-invalid' : '');
    }
  },
  watch: {
    modelValue: function(after, before) {
      if(after == null)
        this.tempValue = null;
        
      this.load(null, after || '');
    },
    tempValue: function(after) {
    },
  },
  mounted: function() {
    if(this.modelValue)
      this.tempValue = objCopy(this.modelValue);

    this.load(null, this.modelValue);
  },
  methods: {
    change: function(v) {
      
      if(this.onSelect)
        this.onSelect(v);
      
      this.$emit("update:modelValue", v);
    },
    search: function(q) {
      this.load(q, null);
    },
    open: function() {
      this.load('', null);
    },
    load: function(q = '', d = null) {
      
      var ids = Array.isArray(d) ? d.map(p => '&ids=' + p) : [];

      if(this.debounce != null)
        clearTimeout(this.debounce);

      this.isLoading = true;
      this.debounce = setTimeout(() => {
        this.$http.get(`${this.url}?q=${q || ''}${ids.length == 0 ? (d ? '&ids=' + d : '') : ids.join('')}`)
          .then(p => {
            this.list = p.data.Data;
            if(d != '' && d != null && p.data.Data.length > 0) {
              if(this.multiple !== undefined) {
                this.tempValue = p.data.Data.map(p => p.Id);
              }
              else {
                this.tempValue = p.data.Data[0].Id;
                if(this.modelValue != this.tempValue)
                  this.$emit("update:modelValue", this.tempValue.Id);
              }
            }
          })
          .finally(() => this.isLoading = false);
          
        clearTimeout(this.debounce);
      }, 200)
    },
    clear: function() {
      this.$emit("update:modelValue", null);
    }
  }
}
</script>