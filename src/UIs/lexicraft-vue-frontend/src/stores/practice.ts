import { defineStore } from 'pinia'
import { useSettingStore } from './setting'
import {WordPracticeStage} from "@/types/enum.ts";
import { WordPracticeModeStageMap, WordPracticeStageNameMap } from '@/config/env.ts'

export interface PracticeState {
  stage: WordPracticeStage
  startDate: number
  spend: number
  total: number
  newWordNumber: number
  reviewWordNumber: number
  writeWordNumber: number
  inputWordNumber: number //当前总输入了多少个单词（不包含跳过）
  wrong: number
}

export const usePracticeStore = defineStore('practice', {
  state: (): PracticeState => {
    return {
      stage: WordPracticeStage.FollowWriteNewWord,
      spend: 0,
      startDate: Date.now(),
      total: 0,
      newWordNumber: 0,
      reviewWordNumber: 0,
      writeWordNumber: 0,
      inputWordNumber: 0,
      wrong: 0,
    }
  },
  getters: {
    getStageName: (state) => {
      return WordPracticeStageNameMap[state.stage]
    },
    nextStage: (state) => {
      const settingStore = useSettingStore()
      const stages = WordPracticeModeStageMap[settingStore.wordPracticeMode]
      const index = stages.findIndex(v => v === state.stage)
      return stages[index + 1]
    }
  }
})
