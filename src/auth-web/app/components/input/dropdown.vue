<template>
  <div class="mb-2" :class="col ? ('form-group col-' + col) : 'form-group'">
    <span v-if="label" class="font-semibold text-sm">{{ label }}</span>
    <!-- {{ (errors || []).filter(p => p != null && p != 'null').length }} -->
    <div :class="(errors || []).filter(p => p != null && p != 'null').length > 0 ? 'is-invalid' : null">
      <input-multiselect v-model="tempValue" :options="list" :close-on-select="true" :clear-on-select="false"
        :preserve-search="true" open-direction="bottom" :placeholder="placeholder || 'Search '" :searchable="true"
        :label="textField || 'Label'" :track-by="valueField || 'Id'" :trackBy="valueField || 'Id'" :hide-selected="true"
        :internal-search="false" :loading="isLoading" @search-change="search" @open="open" @clear="clear"
        :select="change" :multiple="multiple !== undefined || false" select-label="" deselect-label=""
        :disabled="disabled" />
    </div>
    <p class="text-xs" v-if="description">{{ description }}</p>
    <p class="text-red-500 text-xs" v-if="errors">{{ errors[0] }}</p>
  </div>
</template>

<script>
export default {
  model: {
    prop: 'modelValue',
    event: 'update',
  },
  emits: ['update:modelValue'],
  props: ['modelValue', 'label', 'col', 'description', 'placeholder',
    'onSelect', 'errors', 'multiple', 'disabled',
    'source', 'onClear', 'filters', 'sorts', 'valueField', 'textField', 'fieldnames', 'type'
  ],
  data: () => ({
    isLoading: false,
    list: [],
    tempValue: null,
    debounce: null,
  }),
  computed: {
    cClass: function () {
      return (this.errors ? 'is-invalid' : '');
    },
    ds: function () {
      return useDataSource();
    }
  },
  watch: {
    modelValue: function (after, before) {
      if (after == null)
        this.tempValue = null;

      this.tempValue = after;
      this.load(null, after || '');
    },
    tempValue: function (after) {
    },
  },
  mounted: function () {
    if (this.modelValue)
      this.tempValue = objCopy(this.modelValue);

    this.load(null, this.modelValue);
  },
  methods: {
    formatDate(dateStr) {
      if (!dateStr) return null;

      const date = new Date(dateStr);
      if (isNaN(date)) return null;

      const day = String(date.getDate()).padStart(2, '0');
      const month = date.toLocaleString('en-US', { month: 'short' });
      const year = date.getFullYear();

      return `${day} ${month} ${year}`;
    },
    change: function (v) {
      if (this.onSelect) {
        this.onSelect(this.list.find(p => p[this.valueField || 'Id'] == v));
      }

      this.$emit("update:modelValue", v);
    },
    search: function (q) {
      this.load(q, null);
    },
    open: function () {
      this.load('', null);
    },
    load: function (q = '', d = null) {
      var ids = Array.isArray(d) ? d.map(p => '&ids=' + p) : [];

      if (this.debounce != null)
        clearTimeout(this.debounce);

      var fieldnames = [...(this.fieldnames || ['*'])];
      var filters = [...(this.filters || [])];
      filters.push([(this.textField || 'Label'), 'like', `%${q || ''}%`]);
      if (Array.isArray(d)) {
        filters.push([this.valueField || 'Id', 'in', `(${d.map(p => ',' + p)})`])
      }
      else {
        if (d) {
          filters.push([this.valueField || 'Id', d])
        }
      }
      this.isLoading = true;
      this.debounce = setTimeout(() => {
        this.$api.getList(this.source || 'Personnels', filters, fieldnames, (this.sorts || [`${(this.textField || 'Label')} asc`])).then((resp) => {
          this.list = resp.data.data;
          if (this.type == 'terminatedEmployee') {
            this.list = this.list.map(p => ({
              ...p,
              Name: `${p.EmployeeNumber} - ${p.Name} || Temination Date : ${this.formatDate(p.ResignedDate)}`,
            }));
          }
          else if (this.type == 'employee') {
            this.list = this.list.map(p => ({
              ...p,
              Name: `${p.EmployeeNumber} - ${p.Name}`,
            }));
          }
        }).finally(() => this.isLoading = false);

        clearTimeout(this.debounce);
      }, 200)
    },
    clear: function () {
      this.$emit("update:modelValue", null);
      if (this.onClear) {
        this.onClear()
      }
    }
  }
}
</script>

<style lang="scss"></style>