#!/usr/bin/env node
import {readdirSync, readFileSync} from 'node:fs'
import path from 'node:path'

const root = path.resolve(import.meta.dirname, '..')
const sourceRoot = path.join(root, 'src')
const allowedFiles = new Set([
  'src/apis/dict.ts',
  'src/apis/index.ts',
  'src/apis/member.ts',
  'src/apis/user.ts',
  'src/apis/words.ts',
])
const sourceExtensions = new Set(['.js', '.ts', '.tsx', '.vue'])
const legacyImport = /(?:from\s+|import\s*)['"]@\/utils\/http(?:\.ts)?['"]/
const violations = []

function walk(directory) {
  for (const entry of readdirSync(directory, {withFileTypes: true})) {
    const fullPath = path.join(directory, entry.name)
    if (entry.isDirectory()) {
      walk(fullPath)
      continue
    }
    if (!sourceExtensions.has(path.extname(entry.name))) continue

    const relativePath = path.relative(root, fullPath).replaceAll('\\', '/')
    if (legacyImport.test(readFileSync(fullPath, 'utf8')) && !allowedFiles.has(relativePath)) {
      violations.push(relativePath)
    }
  }
}

walk(sourceRoot)

if (violations.length > 0) {
  console.error('禁止新增旧版 @/utils/http 客户端引用，请使用按服务域划分的新客户端：')
  violations.forEach(file => console.error('- ' + file))
  process.exit(1)
}

console.log('旧 HTTP 客户端门禁通过：现有兼容调用未扩散。')
