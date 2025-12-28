<template>
  <div>
    <Tabs default-value="personal-profile" class="w-full h-full" orientation="vertical">
      <div class="flex h-full">
        <div>
          <div>
            <TabsList class="grid w-full grid-cols-1 h-[250px] overflow-y-auto overflow-x-hidden">
              <TabsTrigger v-for="(mod, i) in criteriaTypeList" :value="mod.code"
                class="text-start justify-items-start justify-start"
              >
                {{ mod.name }}
              </TabsTrigger>
            </TabsList>
          </div>
        </div>
        <div class="flex-1 ps-4">
          <TabsContent value="personal-profile">
            <div class="grid grid-cols-2">
              <input-criteria-checkbox 
                label="Genders"
                :model-value="gender"
                @input="$emit('update:modelValue', $event.target.value)"
              />
              <input-criteria-checkbox 
                label="Marital Statuses"
                :model-value="maritalStatus"
                @input="$emit('update:modelValue', $event.target.value)"
              />
              <input-criteria-checkbox 
                label="Religions"
                :model-value="religion"
                @input="$emit('update:modelValue', $event.target.value)"
              />
              <input-criteria-checkbox 
                label="Nationalities"
                :model-value="nationality"
                @input="$emit('update:modelValue', $event.target.value)"
              />
              <input-criteria-checkbox 
                label="Ethnics"
                :model-value="ethnic"
                @input="$emit('update:modelValue', $event.target.value)"
              />
            </div>
          </TabsContent>
          <TabsContent value="organization">
            <input-criteria-checkbox 
              label="Work Locations"
              :model-value="workLocation"
              @input="$emit('update:modelValue', $event.target.value)"
            />
            <input-criteria-checkbox 
              label="Departments"
              :model-value="department"
              @input="$emit('update:modelValue', $event.target.value)"
            />
            <input-criteria-checkbox 
              label="Divisions"
              :model-value="division"
              @input="$emit('update:modelValue', $event.target.value)"
            />
          </TabsContent>
          <TabsContent value="job-structure">
            <input-criteria-checkbox 
              label="Job Titles"
              :model-value="jobTitle"
              @input="$emit('update:modelValue', $event.target.value)"
            />
            <input-criteria-checkbox 
              label="Job Grades"
              :model-value="jobGrade"
              @input="$emit('update:modelValue', $event.target.value)"
            />
            <input-criteria-checkbox 
              label="Job Positions"
              :model-value="jobPosition"
              @input="$emit('update:modelValue', $event.target.value)"
            />
          </TabsContent>
          <TabsContent value="employment-status">
            <input-criteria-checkbox 
              label="Employment Statuses"
              :model-value="employmentStatus"
              @input="$emit('update:modelValue', $event.target.value)"
            />
          </TabsContent>
          <TabsContent value="tenure">
            <input-criteria-checkbox 
              label="Tenures"
              :model-value="tenure"
              @input="$emit('update:modelValue', $event.target.value)"
            />
          </TabsContent>
          <TabsContent value="payroll-group">
            <input-criteria-checkbox 
              label="Payroll Groups"
              :model-value="payrollGroup"
              @input="$emit('update:modelValue', $event.target.value)"
            />
          </TabsContent>
          <TabsContent value="classification">
            <input-criteria-checkbox 
              label="Classifications"
              :model-value="classification"
              @input="$emit('update:modelValue', $event.target.value)"
            />
          </TabsContent>
        </div>
      </div>
    </Tabs>
  </div>
</template>

<script>
export default {
  props: [
    'gender', 'maritalStatus', 'religion', 'nationality', 'ethnic',
    'workLocation', 'department', 'division',
    'jobTitle', 'jobGrade', 'jobPosition', 
    'employmentStatus', 'tenure', 'payrollGroup', 'classification'
  ],
  emits: [
    'update:gender', 'update:maritalStatus', 'update:religion', 'update:nationality', 'update:ethnic',
    'update:workLocation', 'update:department', 'update:division',
    'update:jobTitle', 'update:jobGrade', 'update:jobPosition', 
    'update:employmentStatus', 'update:tenure', 'update:payrollGroup', 'update:classification'
  ],
  data: () => ({
    isLoading: false,
    criteriaTypeList: [
      { code: 'personal-profile', name: 'Personal Profile' },
      { code: 'organization', name: 'Location & Organization' },
      { code: 'job-structure', name: 'Job Structures' },
      { code: 'employment-status', name: 'Employment Status' },
      { code: 'tenure', name: 'Tenure' },
      { code: 'payroll-group', name: 'Payroll Groups' },
      { code: 'classification', name: 'Classifications' },
    ],
    category: [

    ],
    list: [],
  }),
  watch: {

  },
  mounted: function() {
    this.load();
  },
  methods: {
    load: function() {
      this.isLoading = true;
      this.$api.getListnoSort('PolicyRequirements', [])
        .then(({data}) => {
          this.$emit('update:gender', this.mapList(data.data, 'Gender', this.gender))
          this.$emit('update:maritalStatus', this.mapList(data.data, 'MaritalStatus', this.maritalStatus))
          this.$emit('update:religion', this.mapList(data.data, 'Religion', this.religion))
          this.$emit('update:nationality', this.mapList(data.data, 'Nationality', this.nationality))
          this.$emit('update:ethnic', this.mapList(data.data, 'Ethnicity', this.ethnic))
          this.$emit('update:workLocation', this.mapList(data.data, 'WorkLocation', this.workLocation))
          this.$emit('update:department', this.mapList(data.data, 'Department', this.department))
          this.$emit('update:division', this.mapList(data.data, 'Division', this.division))
          this.$emit('update:jobTitle', this.mapList(data.data, 'JobTitle', this.jobTitle))
          this.$emit('update:jobGrade', this.mapList(data.data, 'JobGrade', this.jobGrade))
          this.$emit('update:jobPosition', this.mapList(data.data, 'JobPosition', this.jobPosition))
          this.$emit('update:employmentStatus', this.mapList(data.data, 'EmploymentStatus', this.employmentStatus))
          this.$emit('update:tenure', this.mapList(data.data, 'EmploymentAgeRange', this.tenure))
          this.$emit('update:payrollGroup', this.mapList(data.data, 'PayrollGroup', this.payrollGroup))
          this.$emit('update:classification', this.mapList(data.data, 'PersonnelClassificationType', this.classification))
        })
        .finally(_ => this.isLoading =false)
    },
    mapList: function(list, category, defaultList) {
      return list.filter(p => p.RequirementCategory == category)
        .map(p => ({...p, Apply: (defaultList || []).find(c => c.RequirementId == p.RequirementId)?.Apply || false}));
    },
  }
}
</script>