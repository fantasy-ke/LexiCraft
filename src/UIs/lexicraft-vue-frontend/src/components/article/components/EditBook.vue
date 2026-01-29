<script lang="ts" setup>
import type {Dict} from '@/types/types.ts'
import {cloneDeep} from '@/utils'
import Toast from '@/components/base/toast/Toast.ts'
import {onMounted, reactive} from 'vue'
import {useRuntimeStore} from '@/stores/runtime.ts'
import {useBaseStore} from '@/stores/base.ts'
import BaseButton from '@/components/BaseButton.vue'
import {getDefaultDict} from '@/types/func.ts'
import {Option, Select} from '@/components/base/select'
import BaseInput from '@/components/base/BaseInput.vue'
import Form from '@/components/base/form/Form.vue'
import FormItem from '@/components/base/form/FormItem.vue'
import {addDict} from '@/apis'
import {AppEnv, DictId} from '@/config/env.ts'
import {nanoid} from 'nanoid'
import {DictType} from '@/types/enum.ts'

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
const dictFormRef = $ref<any>()
let loading = $ref(false)
const dictRules: any = reactive({
  name: [
    {required: true, message: '请输入名称', trigger: 'blur'},
    {max: 20, message: '名称不能超过20个字符', trigger: 'blur'},
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
  <div class="edit-book-container w-140 mt-8 p-10 bg-white dark:bg-slate-900 rounded-[2.5rem] shadow-2xl border border-slate-100 dark:border-slate-800 relative overflow-hidden group">
    <!-- Background Decorators -->
    <div class="absolute -right-20 -top-20 w-80 h-80 bg-blue-500/5 rounded-full blur-3xl pointer-events-none group-hover:bg-blue-500/10 transition-colors duration-700"></div>
    <div class="absolute -left-20 -bottom-20 w-64 h-64 bg-indigo-500/5 rounded-full blur-3xl pointer-events-none"></div>

    <div class="relative z-10">
      <div class="mb-10 text-center lg:text-left">
        <h2 class="text-3xl font-black grad-text m-0 mb-2">{{ isAdd ? 'Create Dictionary' : 'Edit Dictionary' }}</h2>
        <p class="text-xs font-bold text-slate-400 uppercase tracking-[0.1em]">Customize your learning collection details</p>
      </div>

      <Form ref="dictFormRef" :model="dictForm" :rules="dictRules" class="premium-form space-y-6">
        <FormItem label="词典名称" prop="name">
          <BaseInput v-model="dictForm.name" class="!h-12 !rounded-xl !bg-slate-50 dark:!bg-slate-800 border-none px-4 font-bold" placeholder="例如：考研英语词汇"/>
        </FormItem>
        
        <FormItem label="详细描述" prop="description">
          <Textarea 
             v-model="dictForm.description" 
             class="!rounded-2xl !bg-slate-50 dark:!bg-slate-800 border-none p-4 font-medium" 
             placeholder="添加一些备注或说明..."
             :autosize="{ minRows: 4, maxRows: 6 }"
          />
        </FormItem>

        <div class="flex items-center gap-4 pt-8">
          <BaseButton 
             class="flex-[2] h-14 rounded-2xl bg-gradient-to-r from-blue-600 to-indigo-600 border-none text-white font-black text-lg shadow-xl shadow-blue-600/20 transition-all hover:scale-[1.02] active:scale-[0.98]" 
             :loading="loading"
             @click="onSubmit"
          >
            Confirm & Save
          </BaseButton>
          <BaseButton 
             class="flex-1 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 border-slate-100 dark:border-slate-700 text-slate-500 font-bold transition-all hover:bg-slate-100 active:scale-[0.98]" 
             @click="emit('close')"
          >
            Cancel
          </BaseButton>
        </div>
      </Form>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
