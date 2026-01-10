<script setup lang="ts">
import BasePage from '@/components/BasePage.vue'
import Collapse from '@/components/base/Collapse.vue'
import WeChat from '@/components/ChannelIcons/WeChat.vue'
import { APP_NAME, GITHUB } from '@/config/env.ts'
import ConflictNoticeText from '@/components/ConflictNoticeText.vue'
</script>

<template>
  <BasePage>
    <div class="center">
      <div class="card-white qa w-2/3">
        <div class="font-bold text-2xl mb-6">常见问题解答</div>
        <div class="list">
          <Collapse
            q="网站是免费的吗？"
            :a="[
              '不完全免费，因为想要长久发展后续收费是必然的，但不会必须付费才能使用，我们尽量在免费与收费之间找一个平衡点',
              // '不登录依然可以使用大部分功能，但数据需要自己管理，如需多设备使用则需要自行导入导出',
              // '登录后提供官方词典/书籍的数据同步功能，如需要同步自定义词典/书籍，则需要开通会员，同时会提供更多的学习内容和功能',
              '项目是开源的，可自行部署',
            ]"
          />

          <div class="line"></div>

          <Collapse q="无法输入，按下键盘没有反应？">
            <ConflictNoticeText />
          </Collapse>

          <div class="line"></div>

          <Collapse q="数据在哪？怎么在多台电脑/设备之间使用？">
            <div>
              1. 所有用户数据
              <b class="text-red">保存在本地浏览器中</b>。如果您需要在不同的设备、浏览器上使用
              {{ APP_NAME }}， 您需要手动进行数据导出和导入
            </div>
            <p>
              2. 设置 -> 数据设置 -> 在原电脑上导出数据 -> 通过社交软件发送给新电脑 ->
              在新电脑上导入
            </p>
            <p>3. 正在开发账户体系</p>
          </Collapse>

          <div class="line"></div>

          <Collapse q="网站自动规划的单词数量太多了，怎么修改？">
            <p>
              1.
              默认复习词数量与新词数量是1：4，如果新词40个，那么会复习40个上次学习的，复习120个之前学习的（由近到远）
            </p>
            <p>2. 您可在通过 设置 -> 单词设置 -> 复习比 修改</p>
          </Collapse>

          <div class="line"></div>

          <Collapse
            q="完成一次学习要很久，流程是不是太冗长了？"
            :a="[
              '这的确是个问题，冗长的流程容易让人失去背单词的积极性，我正在思考如何优化学习流程，如果您有好的建议欢迎反馈',
              '错误单词会重新再来，如果只是手误按错了，后续重新练习时，可以按Tab键跳过。无法判断用户是手误还是真的不会，所以只能错词统统重来，直到正确为止',
              '复习时，只有选择了不认识的单词才会要求听写与默写，这是合理的，不过目前会出现同一个单词复习了N遍的问题',
              '上线了账户体系之后，会添加艾宾浩斯的记忆曲线功能，到时候规划的复习单词会比现在更智能',
            ]"
          />

          <div class="line"></div>

          <Collapse
            q="会添加艾宾浩斯的记忆曲线功能吗？"
            :a="['上线了账户体系之后，会添加艾宾浩斯的记忆曲线功能']"
          />

          <div class="line"></div>

          <Collapse
            q="能否  自行添加单词/自定义词典/导入自己的单词/修改单词内容？"
            :a="[
              '可以',
              '在单词界面，点击“创建个人词典”',
              '创建完成之后，在词典详情页面，点击 “添加单词” 图标，即可添加自己的单词',
              '也可以点击 “导入” 图标，批量导入（需要严格按照模板xlsx格式来）',
            ]"
          />

          <div class="line"></div>

          <Collapse
            q="能否  自行添加文章/自定义书籍/导入自己的文章/修改文章内容？"
            :a="[
              '可以，操作步骤基本和添加单词的一样',
              '在文章界面，点击“创建个人书籍”',
              '创建完成之后，在书籍详情页面，点击顶部的 “文章管理” 按钮，即可添加自己的文章',
            ]"
          />

          <div class="line"></div>

          <Collapse q="怎么关闭按键音？" :a="['设置 -> 通用设置 -> 音效 -> 按键音，关闭即可']" />

          <div class="line"></div>

          <Collapse
            q="平板能用吗？"
            :a="[
              '平板可以使用，但使用蓝牙键盘体验会更好，毕竟系统自带的虚拟键盘占了1/3的屏幕空间，比较影响观感',
              '连接蓝牙键盘',
              '安卓平板，需要开启 “电脑模式”；iPad无需此操作',
            ]"
          />

          <div class="line"></div>

          <Collapse
            q="手机能用吗？"
            :a="[
              '手机可以使用，但暂时未进行其针对优化，使用起来可能会有不方便的地方，还是建议在电脑或平板上用',
            ]"
          />

          <div class="line"></div>

          <Collapse q="有APP、小程序吗？" a="无，只有网站" />

          <div class="line"></div>

          <Collapse
            q="如何向开发团队反馈问题和功能需求？"
            :a="[
              '可以加入我们官方 微信 群, 详细的描述您想要的功能以及告知这个功能想要解决的问题是什么',
              '如果您在应用中发现了错误或漏洞，请提供详细的描述和重现问题的步骤，当然最好提供一个小视频',
              '也可以给我们提工单',
              '也可以去 github/issues 提交',
            ]"
          >
            <div class="flex items-center">
              微信群：
              <WeChat />
            </div>
            <p>
              GitHub地址：<a :href="GITHUB" target="_blank">{{ GITHUB }}</a>
            </p>
            <div class="">
              工单反馈：<a :href="`https://v.wjx.cn/vm/ev0W7fv.aspx#`" target="_blank"
                >https://v.wjx.cn/vm/ev0W7fv.aspx#</a
              >
            </div>
          </Collapse>
        </div>
      </div>
    </div>
  </BasePage>
</template>

<style scoped lang="scss"></style>
