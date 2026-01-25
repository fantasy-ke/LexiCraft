import {defineStore} from 'pinia'
import {Dict, Word} from '../types/types'
import {_getStudyProgress, checkAndUpgradeSaveDict} from '@/utils'
import {shallowReactive} from 'vue'
import {getDefaultDict} from '@/types/func'
import {get} from 'idb-keyval'
import {AppEnv, DictId, SAVE_DICT_KEY} from '@/config/env'
import {add2MyDict, dictListVersion, myDictList} from '@/apis'
import Toast from '@/components/base/toast/Toast'

export interface BaseState {
    simpleWords: string[]
    load: boolean
    word: {
        studyIndex: number
        bookList: Dict[]
    }
    article: {
        bookList: Dict[]
        studyIndex: number
    }
    dictListVersion: number
}

export const getDefaultBaseState = (): BaseState => ({
    simpleWords: [
        'a',
        'an',
        'i',
        'my',
        'me',
        'you',
        'your',
        'he',
        'his',
        'she',
        'her',
        'it',
        'what',
        'who',
        'where',
        'how',
        'when',
        'which',
        'be',
        'am',
        'is',
        'was',
        'are',
        'were',
        'do',
        'did',
        'can',
        'could',
        'will',
        'would',
        'the',
        'that',
        'this',
        'and',
        'not',
        'no',
        'yes',
        'to',
        'of',
        'for',
        'at',
        'in',
    ],
    load: false,
    word: {
        bookList: [
            getDefaultDict({id: DictId.wordCollect, en_name: DictId.wordCollect, name: '收藏'}),
            getDefaultDict({id: DictId.wordWrong, en_name: DictId.wordCollect, name: '错词'}),
            getDefaultDict({
                id: DictId.wordKnown,
                en_name: DictId.wordCollect,
                name: '已掌握',
                description: '已掌握后的单词不会出现在练习中',
            }),
        ],
        studyIndex: -1,
    },
    article: {
        bookList: [
            getDefaultDict({id: DictId.articleCollect, en_name: DictId.articleCollect, name: '收藏'}),
        ],
        studyIndex: -1,
    },
    dictListVersion: 1,
})

export const useBaseStore = defineStore('base', {
    state: (): BaseState => {
        return getDefaultBaseState()
    },
    getters: {
        collectWord(): Dict {
            return this.word.bookList[0]
        },
        collectArticle(): Dict {
            return this.article.bookList[0]
        },
        wrong(): Dict {
            return this.word.bookList[1]
        },
        known(): Dict {
            return this.word.bookList[2]
        },
        knownWords(): string[] {
            return this.known.words.map((v: Word) => v.word.toLowerCase())
        },
        allIgnoreWords() {
            return this.known.words
                .map((v: Word) => v.word.toLowerCase())
                .concat(this.simpleWords.map((v: string) => v.toLowerCase()))
        },
        sdict(): Dict {
            if (this.word.studyIndex >= 0) {
                return this.word.bookList[this.word.studyIndex] ?? getDefaultDict()
            }
            return getDefaultDict()
        },
        groupLength(): number {
            return Math.ceil(this.sdict.length / this.sdict.perDayStudyNumber)
        },
        currentGroup(): number {
            //当能除尽时，应该加1
            let s = this.sdict.lastLearnIndex % this.sdict.perDayStudyNumber
            let d = this.sdict.lastLearnIndex / this.sdict.perDayStudyNumber
            return Math.floor(s === 0 ? (d + 1) : d)
        },
        currentStudyProgress(): number {
            if (!this.sdict.length) return 0
            return _getStudyProgress(this.sdict.lastLearnIndex, this.sdict.length)
        },
        getDictCompleteDate(): number {
            if (!this.sdict.length) return 0
            if (!this.sdict.perDayStudyNumber) return 0
            return Math.ceil(
                (this.sdict.length - this.sdict.lastLearnIndex) / this.sdict.perDayStudyNumber
            )
        },
        sbook(): Dict {
            return this.article.bookList[this.article.studyIndex] ?? {}
        },
        currentBookProgress(): number {
            if (!this.sbook.length) return 0
            if (this.sbook.complete) return 100
            return _getStudyProgress(this.sbook.lastLearnIndex, this.sbook.length)
        },
    },
    actions: {
        setState(obj: BaseState) {
            obj.word.bookList.map(book => {
                book.words = shallowReactive(book.words)
                book.articles = shallowReactive(book.articles)
                book.statistics = shallowReactive(book.statistics)
            })
            obj.article.bookList.map(book => {
                book.words = shallowReactive(book.words)
                book.articles = shallowReactive(book.articles)
                book.statistics = shallowReactive(book.statistics)
            })
            this.$patch(obj)
        },
        async init() {
            return new Promise(async resolve => {
                try {
                    let configStr: string = await get(SAVE_DICT_KEY.key)
                    let data = checkAndUpgradeSaveDict(configStr)
                    if (AppEnv.IS_OFFICIAL) {
                        let r = await dictListVersion()
                        if (r.success) {
                            data.dictListVersion = r.data
                        }
                    }
                    if (AppEnv.CAN_REQUEST) {
                        let res = await myDictList()
                        if (res.success) {
                            //只保留未同步的
                            data.word.bookList = data.word.bookList.filter(v => !v.sync)
                            data.article.bookList = data.article.bookList.filter(v => !v.sync)
                            //这里看看是否要 shallowReactive
                            Object.assign(data, res.data)
                        }
                    }
                    // console.log('data', data)
                    this.setState(data)
                } catch (e) {
                    console.error('读取本地dict数据失败', e)
                }
                resolve(true)
            })
        },
        //改变词典
        async changeDict(val: Dict) {
            if (AppEnv.CAN_REQUEST) {
                let r = await add2MyDict({
                    id: val.id,
                    perDayStudyNumber: val.perDayStudyNumber,
                    lastLearnIndex: val.lastLearnIndex,
                    complete: val.complete,
                })
                if (!r.success) return Toast.error(r.msg)
                else val.userDictId = r.data
            }
            //把其他的词典的单词数据都删掉，全保存在内存里太卡了
            this.word.bookList.slice(3).map(v => {
                if (!v.custom) {
                    v.words = shallowReactive([])
                }
            })
            let rIndex = this.word.bookList.findIndex((v: Dict) => v.id === val.id)
            let total = val.length || val.words.length
            if (total > 0 && total < val.perDayStudyNumber) {
                val.perDayStudyNumber = total
            }
            if (rIndex > -1) {
                this.word.studyIndex = rIndex
                this.word.bookList[this.word.studyIndex].words = shallowReactive(val.words)
                this.word.bookList[this.word.studyIndex].perDayStudyNumber = val.perDayStudyNumber
                this.word.bookList[this.word.studyIndex].lastLearnIndex = val.lastLearnIndex
                this.word.bookList[this.word.studyIndex].userDictId = val.userDictId
                this.word.bookList[this.word.studyIndex].complete = val.complete
            } else {
                this.word.bookList.push(getDefaultDict(val))
                this.word.studyIndex = this.word.bookList.length - 1
            }
        },
        //改变书籍
        async changeBook(val: Dict) {
            if (AppEnv.CAN_REQUEST) {
                let r = await add2MyDict({
                    id: val.id,
                    perDayStudyNumber: val.perDayStudyNumber,
                    lastLearnIndex: val.lastLearnIndex,
                    complete: val.complete,
                })
                if (!r.success) {
                    return Toast.error(r.msg)
                }
            }
            //把其他的书籍里面的文章数据都删掉，全保存在内存里太卡了
            this.article.bookList.slice(1).map(v => {
                if (!v.custom) {
                    v.articles = shallowReactive([])
                }
            })
            let rIndex = this.article.bookList.findIndex((v: Dict) => v.id === val.id)
            if (rIndex > -1) {
                this.article.studyIndex = rIndex
                //不要整个等于，不然统计没了
                // this.article.bookList[this.article.studyIndex] = getDefaultDict(val)
                this.article.bookList[this.article.studyIndex].articles = shallowReactive(val.articles)
                this.article.bookList[this.article.studyIndex].cover = val.cover
                this.article.bookList[this.article.studyIndex].name = val.name
                this.article.bookList[this.article.studyIndex].description = val.description
            } else {
                this.article.bookList.push(getDefaultDict(val))
                this.article.studyIndex = this.article.bookList.length - 1
            }
        },
    },
})
