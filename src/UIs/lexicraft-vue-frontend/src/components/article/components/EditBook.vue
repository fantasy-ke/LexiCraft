<script setup lang="ts">
import type { Dict } from '@/types/types.ts'
import { cloneDeep } from '@/utils'
import Toast from '@/components/base/toast/Toast.ts'
import { onMounted, reactive } from 'vue'
import { useRuntimeStore } from '@/stores/runtime.ts'
import { useBaseStore } from '@/stores/base.ts'
import BaseButton from '@/components/BaseButton.vue'
import { getDefaultDict } from '@/types/func.ts'
import { Option, Select } from '@/components/base/select'
import BaseInput from '@/components/base/BaseInput.vue'
import Form from '@/components/base/form/Form.vue'
import FormItem from '@/components/base/form/FormItem.vue'
import { addDict } from '@/apis'
import { AppEnv, DictId } from '@/config/env.ts'
import { nanoid } from 'nanoid'
import { DictType } from '@/types/enum.ts'

const props = defineProps<{
  isAdd: boolean
  isBook: boolean
}>()
const emit = defineEmits<{
  submit: []
  close: []
}>()
const runtimeStore = useRuntimeStore()
const store = useBaseStore()
const DefaultDictForm = {
  id: '',
  name: '',
  description: '',
  category: '',
  tags: [],
  translateLanguage: 'zh-CN',
  language: 'en',
  type: DictType.article,
}
let dictForm: any = $ref(cloneDeep(DefaultDictForm))
const dictFormRef = $ref()
let loading = $ref(false)
const dictRules = reactive({
  name: [
    { required: true, message: '请输入名称', trigger: 'blur' },
    { max: 20, message: '名称不能超过20个字符', trigger: 'blur' },
  ],
})

async function onSubmit() {
  await dictFormRef.validate(async valid => {
    if (valid) {
      let data: Dict = getDefaultDict(dictForm)
      data.type = props.isBook ? DictType.article : DictType.word
      let source = [store.article, store.word][props.isBook ? 0 : 1]
      //todo 可以检查的更准确些，比如json对比
      if (props.isAdd) {
        data.id = 'custom-dict-' + Date.now()
        data.custom = true
        if (source.bookList.find(v => v.name === data.name)) {
          Toast.warning('已有相同名称！')
          return
        } else {
          if (AppEnv.CAN_REQUEST) {
            loading = true
            let res = await addDict(null, data)
            loading = false
            if (res.success) {
              data = getDefaultDict(res.data)
            } else {
              return Toast.error(res.msg)
            }
          }
          source.bookList.push(cloneDeep(data))
          runtimeStore.editDict = data
          emit('submit')
          Toast.success('添加成功')
        }
      } else {
        let rIndex = source.bookList.findIndex(v => v.id === data.id)
        //任意修改，都将其变为自定义词典
        if (
          !data.custom &&
          ![DictId.wordKnown, DictId.wordWrong, DictId.wordCollect, DictId.articleCollect].includes(
            data.en_name || data.id
          )
        ) {
          data.custom = true
          if (!data.id.includes('_custom')) {
            data.id += '_custom_' + nanoid(6)
          }
        }
        runtimeStore.editDict = data
        if (rIndex > -1) {
          source.bookList[rIndex] = getDefaultDict(data)
          emit('submit')
          Toast.success('修改成功')
        } else {
          source.bookList.push(getDefaultDict(data))
          Toast.success('修改成功并加入我的词典')
        }
      }
      console.log('submit!', data)
    } else {
      Toast.warning('请填写完整')
    }
  })
}

onMounted(() => {
  if (!props.isAdd) {
    dictForm = cloneDeep(runtimeStore.editDict)
  }
})
</script>

<template>
  <div class="w-120 mt-4">
    <Form ref="dictFormRef" :rules="dictRules" :model="dictForm" label-width="8rem">
      <FormItem label="名称" prop="name">
        <BaseInput v-model="dictForm.name" />
      </FormItem>
      <FormItem label="描述">
        <BaseInput v-model="dictForm.description" textarea />
      </FormItem>
      <FormItem label="原文语言" v-if="false">
        <Select v-model="dictForm.language" placeholder="请选择选项">
          <Option label="英语" value="en" />
          <Option label="德语" value="de" />
          <Option label="日语" value="ja" />
          <Option label="代码" value="code" />
        </Select>
      </FormItem>
      <FormItem label="译文语言" v-if="false">
        <Select v-model="dictForm.translateLanguage" placeholder="请选择选项">
          <Option label="中文" value="zh-CN" />
          <Option label="英语" value="en" />
          <Option label="德语" value="de" />
          <Option label="日语" value="ja" />
        </Select>
      </FormItem>
      <div class="center">
        <base-button type="info" @click="emit('close')">关闭</base-button>
        <base-button type="primary" :loading="loading" @click="onSubmit">确定</base-button>
      </div>
    </Form>
  </div>
</template>

<style scoped lang="scss"></style>
