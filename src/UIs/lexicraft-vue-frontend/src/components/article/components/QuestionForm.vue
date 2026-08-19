<template>
  <div class="question-form en-article-family">
    <div class="flex items-center justify-between">
      <div class="font-bold">Multiple choice questions 选择题</div>
      <div v-if="false">
        <button
            v-if="!started"
            class="bg-blue-600 text-white px-4 py-1 rounded"
            @click="startExam"
        >开始
        </button>
        <span v-if="started" class="text-red-600 font-semibold font-family">
          倒计时：{{ timeLeft }} 秒
        </span>
      </div>
    </div>

    <form @submit.prevent>
      <QuestionItem
          v-for="(q, i) in questions"
          :key="i"
          ref="questionRefs1"
          :correct-answer="q.correctAnswer"
          :explanation="q.explanation"
          :immediate-feedback="props.immediateFeedback"
          :options="q.options"
          :question-index="i + 1"
          :randomize="props.randomize"
          :stem="q.stem"
          @answered="onAnswered"
      />
    </form>

    <div class="center items-center gap-2  mt-10">
      <button
          class="bg-green-600 text-white px-6 py-2 rounded"
          @click="submitAll"
      >提交试卷
      </button>
      <span class="text-xl">浅红：错误 深红：未选 绿：正确</span>
    </div>
  </div>
</template>

<script lang="ts" setup>
import {ref, useTemplateRef} from 'vue'
import QuestionItem from './QuestionItem.vue'
import Toast from '@/components/base/toast/Toast.ts'

interface Question {
  stem: string
  options: string[]
  correctAnswer: string[]
  explanation: string
}

interface QuestionResult {
  index: number
  selected: string[]
  isCorrect: boolean
}

interface QuestionItemExpose {
  submit: () => void
  getResult: () => QuestionResult
}

interface IProps {
  questions: Question[]
  duration: number
  immediateFeedback: boolean
  randomize: boolean
}

const props = withDefaults(defineProps<IProps>(), {
  questions: () => [],
  duration: 300,
  immediateFeedback: false,
  randomize: false
})

const questionRefs = useTemplateRef<QuestionItemExpose[]>('questionRefs1')
const started = ref(false)
const timeLeft = ref(props.duration || 300)
let timer: ReturnType<typeof setInterval> | undefined

const startExam = () => {
  started.value = true
  timeLeft.value = props.duration || 300
  timer = setInterval(() => {
    timeLeft.value--
    if (timeLeft.value <= 0) {
      clearInterval(timer)
      timer = undefined
      submitAll()
    }
  }, 1000)
}

const onAnswered = (res: QuestionResult) => {
  console.log('Answered:', res)
  // 可收集中间过程（非必须）
}

const submitAll = () => {
  console.log(questionRefs)
  const items = questionRefs.value ?? []
  items.forEach(q => q.submit())
  const results = items.map(q => q.getResult())
  const correctCount = results.filter(r => r.isCorrect).length
  const wrongCount = results.length - correctCount

  console.log('最终结果：', results)
  Toast.success(`共 ${results.length} 题，答对 ${correctCount}，答错 ${wrongCount}`)
}
</script>

<style scoped>

</style>
