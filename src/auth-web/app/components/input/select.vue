<template>
  <div
    class="mb-2"
    :class="(errors || []).filter(p => p != null).length > 0 ? 'is-invalid' : ''"
  >
    <label for="" class="text-sm font-semibold" v-if="label">{{ label }}</label>
    <Multiselect
      v-model="tempValue"
      :options="options"
      :label="textField || 'Name'"
      :trackBy="valueField || 'Id'"
      :valueProp="valueField || 'Id'"
      @search="search"
      @search-change="searchChange"
      @open="open"
      @input="change"
      @select="selectChange"
      @deselect="deselect"
      @clear="clear"
      :searchable="true"
      :hide-selected="true"
      :internal-search="false"
      :loading="loading"
      :close-on-select="true"
      :clear-on-select="false"
      :preserve-search="true"
      :resolve-on-load="false"
      :filter-results="false"
      :disabled="disabled"
      :placeholder="placeholder"
      class="text-sm shadow-xs"
    />
  </div>
</template>

<script>
import Multiselect from '@vueform/multiselect'

export default {
  model: {
    prop: 'modelValue',
    event: 'update',
  },
  emits: ['update:modelValue'],
  props: [
    'modelValue', 'options', 'loading',
    'label', 'trackBy', 'searchable', 'disabled', 'placeholder', 'select',
    'clear', 'valueField', 'textField', 'errors'
  ],
  components: { Multiselect, },
  data: () => ({
    tempValue: null,
  }),
  watch: {
    modelValue: function(after, before) {
      if(after === 0 )
        this.tempValue = 0;
      else
        this.tempValue = after == null ? null : JSON.parse(JSON.stringify(after))
    }
  },
  mounted: function() {
    if(this.modelValue === 0 )
      this.tempValue = 0;
    else
      this.tempValue = this.modelValue == null ? null : JSON.parse(JSON.stringify(this.modelValue))
  },
  methods: {
    change: function(v) {
      this.$emit("update:modelValue", v);
      
      if(this.$attrs['onInput'])
        this.$attrs['onInput'](v);
    },
    search: function(v) {
      if(this.$attrs['onSearch'])
        this.$attrs['onSearch'](v);
    },
    searchChange: function(v) {
      if(this.$attrs['onSearchChange'])
        this.$attrs['onSearchChange'](v);
    },
    open: function(v) {
      if(this.$attrs['onOpen'])
        this.$attrs['onOpen']();
    },
    selectChange: function(v) {
      if(v === undefined)
        return;
        
      if(this['select'])
        this['select'](v);
    },
    deselect: function() {
      this.$emit("update:modelValue", null);
    }
  },
}
</script>

<style>
.multiselect, 
.multiselect-wrapper, 
.multiselect-spacer {
  min-height: 36px!important;
  border-radius: 0.625rem;
  /* border-color: oklch(0.922 0 0); */
  border-color: var(--border);
}

.is-invalid > 
.multiselect {
  border-color: red!important;
}
.multiselect-search {
  height: 80%;
  top: 5%;
    border-radius: 0.625rem;
}
</style>