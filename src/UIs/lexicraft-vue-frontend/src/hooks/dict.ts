import type { Article, Dict, TaskWords, Word } from '@/types/types.ts'
import { useBaseStore } from '@/stores/base.ts'
import { useSettingStore } from '@/stores/setting.ts'
import { getDefaultDict, getDefaultWord } from '@/types/func.ts'
import { _getDictDataByUrl, cloneDeep, getRandomN, resourceWrap, shuffle, sleep, splitIntoN } from '@/utils'
import { onMounted, ref, watch } from 'vue'
import { AppEnv, DICT_LIST, DictId } from '@/config/env.ts'
import { detail } from '@/apis'
import { useRuntimeStore } from '@/stores/runtime.ts'
import { useRoute, useRouter } from 'vue-router'
import { DictType } from '@/types/enum.ts'

export function useWordOptions() {
  const store = useBaseStore()

  function isWordCollect(val: Word) {
    return !!store.collectWord.words.find(v => v.word.toLowerCase() === val.word.toLowerCase())
  }

  function toggleWordCollect(val: Word) {
    let rIndex = store.collectWord.words.findIndex(v => v.word.toLowerCase() === val.word.toLowerCase())
    if (rIndex > -1) {
      store.collectWord.words.splice(rIndex, 1)
    } else {
      store.collectWord.words.push(val)
    }
    store.collectWord.length = store.collectWord.words.length
  }

  function isWordSimple(val: Word) {
    return !!store.knownWords.includes(val.word.toLowerCase())
  }

  function toggleWordSimple(val: Word) {
    let rIndex = store.knownWords.findIndex(v => v === val.word.toLowerCase())
    if (rIndex > -1) {
      store.known.words.splice(rIndex, 1)
    } else {
      store.known.words.push(val)
    }
    store.known.length = store.known.words.length
  }

  function delWrongWord(val: Word) {
    let rIndex = store.wrong.words.findIndex(v => v.word.toLowerCase() === val.word.toLowerCase())
    if (rIndex > -1) {
      store.wrong.words.splice(rIndex, 1)
    }
    store.wrong.length = store.wrong.words.length
  }

  function delSimpleWord(val: Word) {
    let rIndex = store.known.words.findIndex(v => v.word.toLowerCase() === val.word.toLowerCase())
    if (rIndex > -1) {
      store.known.words.splice(rIndex, 1)
    }
    store.known.length = store.known.words.length
  }

  return {
    isWordCollect,
    toggleWordCollect,
    isWordSimple,
    toggleWordSimple,
    delWrongWord,
    delSimpleWord,
  }
}

export function useArticleOptions() {
  const store = useBaseStore()

  function isArticleCollect(val: Article) {
    return !!store.collectArticle?.articles?.find(v => v.id === val.id)
  }

  //todo 这里先收藏，再修改。收藏里面的未同步。单词也是一样的
  function toggleArticleCollect(val: Article) {
    let rIndex = store.collectArticle.articles.findIndex(v => v.id === val.id)
    if (rIndex > -1) {
      store.collectArticle.articles.splice(rIndex, 1)
    } else {
      store.collectArticle.articles.push(val)
    }
    store.collectArticle.length = store.collectArticle.articles.length
  }

  return {
    isArticleCollect,
    toggleArticleCollect,
  }
}

export function getCurrentStudyWord(): TaskWords {
  const store = useBaseStore()
  let data = { new: [], review: [], write: [], shuffle: [] }
  let dict = store.sdict
  let isTest = false
  let words = dict.words.slice()
  if (isTest) {
    words = Array.from({ length: 10 }).map((v, i) => {
      return getDefaultWord({ word: String(i) })
    })
  }
  if (words?.length) {
    const settingStore = useSettingStore()
    //忽略时是否加上自定义的简单词
    let ignoreList = [store.allIgnoreWords, store.knownWords][settingStore.ignoreSimpleWord ? 0 : 1]
    const perDay = dict.perDayStudyNumber
    let start = dict.lastLearnIndex
    let complete = dict.complete
    let isEnd = start >= dict.length - 1
    if (isTest) {
      start = 1
      complete = true
    }
    //如果已完成，并且记录在最后，那么直接随机取复习词
    if (complete && isEnd) {
      //复习比最小是1
      let ratio = settingStore.wordReviewRatio || 1
      let ignoreList = [store.allIgnoreWords, store.knownWords][settingStore.ignoreSimpleWord ? 0 : 1]
      // 先将可用词表全部随机，再按需过滤忽略列表，只取到目标数量为止
      let shuffled = shuffle(cloneDeep(dict.words))
      let count = 0
      data.write = []
      for (let item of shuffled) {
        if (!ignoreList.includes(item.word.toLowerCase())) {
          data.write.push(item)
          count++
          if (count >= perDay * ratio) {
            break
          }
        }
      }
      return data
    }

    let end = start
    let list = dict.words.slice(start)
    //从start往后取perDay个单词，作为当前练习单词
    for (let item of list) {
      if (!ignoreList.includes(item.word.toLowerCase())) {
        if (data.new.length < perDay) {
          data.new.push(item)
        } else break
      }
      end++
    }

    //如果复习比大于等于1，或者已完成，那么就取复习词
    if (settingStore.wordReviewRatio >= 1 || complete) {
      //从start往前取perDay个单词，作为当前复习单词，取到0为止
      list = dict.words.slice(0, start).reverse()
      //但如果已完成，则滚动取值
      if (complete) list = list.concat(dict.words.slice(end).reverse())
      for (let item of list) {
        if (!ignoreList.includes(item.word.toLowerCase())) {
          if (data.review.length < perDay) {
            data.review.push(item)
          } else break
        }
        start--
      }
    }

    // //如果是自由模式，那么统统设置到new字段里面去
    // if (settingStore.wordPracticeMode === WordPracticeMode.Free) {
    //   data.new = data.new.length ? data.new : data.review
    //   data.review = []
    //   return data
    // }

    // 上上次更早的单词
    //默认只取start之前的单词
    if (settingStore.wordReviewRatio >= 2) {
      let candidateWords = dict.words.slice(0, start).reverse()
      //但如果已完成，则滚动取值
      if (complete) candidateWords = candidateWords.concat(dict.words.slice(end).reverse())
      candidateWords = candidateWords.filter(w => !ignoreList.includes(w.word.toLowerCase()))
      // console.log(candidateWords.map(v => v.word))
      //最终要获取的单词数量
      const totalNeed = perDay * (settingStore.wordReviewRatio - 1)
      if (candidateWords.length <= totalNeed) {
        data.write = candidateWords
      } else {
        //write数组放的是上上次之前的单词，总的数量为perDayStudyNumber * 3，取单词的规则为：从后往前取6个perDayStudyNumber的，越靠前的取的单词越多。
        let days = 6
        // 分6组，每组最多 perDay 个
        const groups: Word[][] = splitIntoN(candidateWords.slice(0, days * perDay), 6)
        // console.log('groups', groups)

        // 分配数量，靠前组多，靠后组少，例如分配比例 [6,5,4,3,2,1]
        const ratio = Array.from({ length: days }, (_, i) => i + 1).reverse()
        const ratioSum = ratio.reduce((a, b) => a + b, 0)
        const realRatio = ratio.map(r => Math.round((r / ratioSum) * totalNeed))
        // console.log(ratio, ratioSum, realRatio, realRatio.reduce((a, b) => a + b, 0))

        // 按比例从每组随机取单词
        let writeWords: Word[] = []
        groups.map((v, i) => {
          writeWords = writeWords.concat(getRandomN(v, realRatio[i]))
        })
        // console.log('writeWords', writeWords)
        data.write = writeWords
      }
    }

    //如果已完成，那么合并写词和复习词
    if (complete) {
      // data.new = []
      // data.review = data.review.concat(data.write)
      // data.write = []
    }
  }
  // console.log('data-new', data.new.map(v => v.word))
  // console.log('data-review', data.review.map(v => v.word))
  // console.log('data-write', data.write.map(v => v.word))
  return data
}

export function useGetDict() {
  const store = useBaseStore()
  const runtimeStore = useRuntimeStore()
  let loading = ref(false)
  const route = useRoute()
  const router = useRouter()

  watch(
    [() => store.load, () => loading],
    ([a, b]) => {
      if (a && b.value) loadDict()
    },
    { immediate: true }
  )

  onMounted(() => {
    if (!runtimeStore.editDict?.id) {
      let dictId = route.query?.id
      if (!dictId) {
        return router.push('/articles')
      }
      loading.value = true
    } else {
      loadDict(runtimeStore.editDict)
    }
  })

  async function loadDict(dict?: Dict) {
    if (!dict) {
      dict = getDefaultDict()
      let dictId = route.query.id
      //先在自己的词典列表里面找，如果没有再在资源列表里面找
      dict = store.article.bookList.find(v => v.id === dictId)
      let r = await fetch(resourceWrap(DICT_LIST.WORD.ALL))
      let dict_list = await r.json()
      if (!dict) dict = dict_list.flat().find(v => v.id === dictId) as Dict
    }
    if (dict && dict.id) {
      if (
        !dict?.articles?.length &&
        !dict?.custom &&
        ![DictId.articleCollect].includes(dict.en_name || dict.id) &&
        !dict?.is_default
      ) {
        loading.value = true
        let r = await _getDictDataByUrl(dict, DictType.article)
        runtimeStore.editDict = r
      }
      if (store.article.bookList.find(book => book.id === runtimeStore.editDict.id)) {
        if (AppEnv.CAN_REQUEST) {
          let res = await detail({ id: runtimeStore.editDict.id })
          if (res.success) {
            runtimeStore.editDict.statistics = res.data.statistics
            if (res.data.articles.length) {
              runtimeStore.editDict.articles = res.data.articles
            }
          }
        }
      }
      loading.value = false
    } else {
      router.push('/articles')
    }
  }

  return {
    loading,
  }
}
