<template>
  <div class="mb-2">
    <label for="" class="text-sm font-semibold">{{ label }}</label>
    <RadioGroup :default-value="tempValue" orientation="horizontal" :class="`grid grid-cols-${col ?? '3'} mt-1`"
      @update:model-value="update"
      v-model="tempValue"
    >
      <div class="flex items-center space-x-2 text-sm" 
        v-for="(item,i) in (options || [])"
      >
        <RadioGroupItem id="r1" :value="item[valueField || 'value']"/>
        <Label for="r1">
          {{ item[textField || 'label'] }}
        </Label>
      </div>
    </RadioGroup>
  </div>
</template>
<script>
export default {
  model: {
    prop: 'modelValue',
    event: 'update',
  },
  emits: ['update:modelValue'],
  props: ['modelValue','label', 'options', 'valueField', 'textField', 'col'],
  data: () => ({
    tempValue: null,
  }),
  watch: {
    modelValue: function(after) {
      this.tempValue = after;
    }
  },
  mounted: function() {
    this.tempValue = this.modelValue;
  },
  methods: {
    update: function(e) {
      this.$emit('update:modelValue', e)
    }
  }
}
</script>