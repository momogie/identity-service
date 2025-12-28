<template>
  <div>
    <label for="" class="text-sm font-semibold">{{ label }}</label>
    <p class="text-red-500 text-xs" v-if="errors">{{ errors[0] }}</p>
    <div :class="`grid grid-cols-${col || '1'}`">
      <div v-for="(item, i) in list">
        <Checkbox 
          :value="selectedItems.some(p => p[valueField || 'Id'] == item[valueField || 'Id'])" 
          :default-value="selectedItems.some(p => p[valueField || 'Id'] == item[valueField || 'Id'])" 
          :model-value="selectedItems.some(p => p[valueField || 'Id'] == item[valueField || 'Id'])" 
          @update:model-value="(v) => onItemChecked(v, item)"
        /> <span class="text-xs"> {{item[textField || 'Label']}}</span>
      </div>
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
    'modelValue', 'label', 'options', 'col', 'source',
    'filters', 'sorts', 'valueField', 'textField', 'errors'
  ],
  data: () => ({
    isLoading: false,
    list: [],
    selectedItems: [],
    tempValue: null,
    debounce: null,
  }),
  watch: {
    modelValue: function(after) {
      this.selectedItems = after;
    }
  },
  mounted: function() {
    this.selectedItems = [...(this.modelValue || [])];
    this.load();
  },
  methods: {
    load: function(q = '', d = null) {
      var ids = Array.isArray(d) ? d.map(p => '&ids=' + p) : [];

      if(this.debounce != null)
        clearTimeout(this.debounce);

      var filters = [...(this.filters || [])];
      filters.push([(this.textField || 'Label'), 'like', `%${q}%`]);
      
      this.isLoading = true;
      this.debounce = setTimeout(() => {
        this.$api.getList(this.source || 'Personnels', filters,['*'] , 1, 20, [`${(this.textField || 'Label')} asc`]).then((resp) => {
          this.list = resp.data.data;
        }).finally(() => this.isLoading = false);
          
        clearTimeout(this.debounce);
      }, 200)
    },
    onItemChecked: function(a, b) {
      if(a) {
        this.selectedItems.push(b);
        this.$emit('update:modelValue', this.selectedItems)
        return;
      }
      this.selectedItems = this.selectedItems.filter(p => p[this.valueField || 'Id'] != b[this.valueField || 'Id'])
      this.$emit('update:modelValue', this.selectedItems)
    },
  }
}
</script>