<template>
  <div class="mb-2">
    <Checkbox 
      @update:model-value="checkAll"
      v-bind:model-value="!modelValue.some(p => p.Apply === false) && modelValue.length > 0"
    /><span class="text-xs ms-2 font-semibold">All {{label}}</span>
    <div :class="`grid grid-cols-${col || '1'}`">
      <div v-for="(item, i) in (modelValue || [])">
        <Checkbox v-model="item.Apply" :value="item.Apply" 
          @update:model-value="change"
        /> 
        <span class="ms-2 text-xs"> {{item.RequirementLabel}}</span>
      </div>
    </div>
  </div>
</template>
<script>
export default {
  props: ['modelValue', 'col', 'label', 'onChange'],
  emits: ['update:modelValue'],
  data: () => ({
    
  }),
  methods: {
    change: function() {
      if(this.onChange) {
        this.onChange(this.modelValue);
      }
    },
    checkAll: function(e) {
      if(e) {
        this.modelValue.forEach(e => {
          e.Apply = true;
        })
        if(this.onChange) {
          this.onChange(this.modelValue);
        }
        return;
      }
      this.modelValue.forEach(e => {
        e.Apply = false;
      })
      if(this.onChange) {
        this.onChange(this.modelValue);
      }
    }
  }
}
</script>
<!-- 
<template>
  <div class="mb-2">
    <Checkbox 
      @update:model-value="checkAll"
      v-bind:model-value="!list.some(p => p.Apply === false)"
    /><span class="text-xs ms-2 font-semibold">All {{label}}</span>
    <div :class="`grid grid-cols-${col || '1'}`">
      <div v-for="(item, i) in (list || [])">
        <Checkbox v-model="item.Apply" :value="item.Apply"
          @update:model-value="change"
        /> 
        <span class="ms-2 text-xs"> {{item.RequirementLabel}}</span>
      </div>
    </div>
  </div>
</template>
<script>
export default {
  props: ['list', 'modelValue', 'col', 'label', 'onChange'],
  emits: ['update:modelValue'],
  data: () => ({
    listI: [],
  }),
  mounted: function() {
    // this.list = (this.list || []).map(p => ({...p, Apply: false}))
  },
  methods: {
    change: function() {
      if(this.onChange) {
        this.onChange(this.list);
      }
    },
    checkAll: function(e) {
      if(e) {
        this.list.forEach(e => {
          e.Apply = true;
        })
        this.onChange(this.list);
        return;
      }
        this.list.forEach(e => {
          e.Apply = false;
        })

      this.onChange(this.list);
    }
  }
}
</script> -->