export enum DictType {
  collect = 'collect',
  simple = 'simple',
  wrong = 'wrong',
  known = 'known',
  word = 'word',
  article = 'article',
}
export enum Sort {
  normal = 0,
  random = 1,
  reverse = 2,
  reverseAll = 3,
  randomAll = 4,
}

export enum ShortcutKey {
  ShowWord = 'ShowWord',
  EditArticle = 'EditArticle',
  Next = 'Next',
  Previous = 'Previous',
  ToggleSimple = 'ToggleSimple',
  ToggleCollect = 'ToggleCollect',
  NextChapter = 'NextChapter',
  PreviousChapter = 'PreviousChapter',
  RepeatChapter = 'RepeatChapter',
  DictationChapter = 'DictationChapter',
  PlayWordPronunciation = 'PlayWordPronunciation',
  ToggleShowTranslate = 'ToggleShowTranslate',
  ToggleDictation = 'ToggleDictation',
  ToggleTheme = 'ToggleTheme',
  ToggleConciseMode = 'ToggleConciseMode',
  TogglePanel = 'TogglePanel',
  RandomWrite = 'RandomWrite',
  KnowWord = 'KnowWord',
  UnknownWord = 'UnknownWord',
}

export enum TranslateEngine {
  Baidu = 0,
}

export enum PracticeArticleWordType {
  Symbol,
  Number,
  Word,
}

//练习模式
export enum WordPracticeMode {
  System = 0,
  Free = 1,
  IdentifyOnly = 2, // 独立自测模式
  DictationOnly = 3, // 独立默写模式
  ListenOnly = 4, // 独立听写模式
  Shuffle = 5, // 随机复习模式
  Review = 6, // 复习模式
}

//练习类型
export enum WordPracticeType {
  FollowWrite, //跟写
  Spell,
  Identify,
  Listen,
  Dictation,
}

export enum CodeType {
  Login = 0,
  Register = 1,
  ResetPwd = 2,
  ChangeEmail = 3,
  ChangePhoneNew = 4,
  ChangePhoneOld = 5,
}

export enum ImportStatus {
  Idle = 0,
  Success = 1,
  Fail = 2,
}

//练习阶段
export enum WordPracticeStage {
  FollowWriteNewWord = 0,
  IdentifyNewWord = 1,
  ListenNewWord = 2,
  DictationNewWord = 3,

  FollowWriteReview = 4,
  IdentifyReview = 5,
  ListenReview = 6,
  DictationReview = 7,

  FollowWriteReviewAll = 8,
  IdentifyReviewAll = 9,
  ListenReviewAll = 10,
  DictationReviewAll = 11,

  Shuffle = 12,
  Complete = 13,
}
