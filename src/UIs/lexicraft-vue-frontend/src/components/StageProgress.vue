<template>
  <div class="flex gap-5 w-full h-3">
    <template v-for="i of props.stages">
      <template v-if="i?.children?.length && i.active">
        <div :style="{ width: i.ratio + '%' }" class="flex gap-1">
          <template v-for="j of i.children">
            <Tooltip :title="j.name">
              <Progress
                  :active="j.active"
                  :color="j.active ? '#72c240' : '#69b1ff'"
                  :percentage="j.percentage"
                  :show-text="false"
                  :stroke-width="8"
                  :style="{ width: j.ratio + '%' }"
              />
            </Tooltip>
          </template>
        </div>
      </template>
      <template v-else>
        <Tooltip :title="i.name">
          <Progress
              :active="i.active"
              :color="i.active && props.stages.length > 1 ? '#72c240' : '#69b1ff'"
              :percentage="i.percentage"
              :show-text="false"
              :stroke-width="8"
              :style="{ width: i.ratio + '%' }"
          />
        </Tooltip>
      </template>
    </template>
  </div>
</template>
<script lang="ts" setup>
import Tooltip from '@/components/base/Tooltip.vue'
import Progress from '@/components/base/Progress.vue'

const props = defineProps<{
  stages: {
    name: string
    active?: boolean
    percentage: number
    ratio: number
    children: {
      active: boolean
      name: string
      percentage: number
      ratio: number
    }[]
  }[]
}>()
</script>
<style lang="scss" scoped></style>
