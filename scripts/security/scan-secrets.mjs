#!/usr/bin/env node
import { execFileSync } from 'node:child_process'
import { readFileSync, statSync } from 'node:fs'
import path from 'node:path'

const root = path.resolve(import.meta.dirname, '../..')
const selfPath = 'scripts/security/scan-secrets.mjs'
const excluded = /(^|\/)(?:bin|obj|node_modules|dist|src-tauri\/target|public\/dicts)(?:\/|$)/i
const textExtensions = new Set(['.cs', '.csproj', '.fs', '.json', '.md', '.props', '.targets', '.toml', '.ts', '.tsx', '.vue', '.xml', '.yaml', '.yml', '.config', '.conf', '.env', '.ps1', '.sh', '.mjs', '.js'])

const files = execFileSync('git', ['ls-files', '-z'], { cwd: root })
  .toString('utf8')
  .split('\0')
  .filter(Boolean)
  .filter(file => file !== selfPath && !excluded.test(file.replaceAll('\\', '/')))

const safeValue = raw => {
  const value = raw.trim().replace(/^[\"']|[\"']$/g, '').trim()
  if (!value) return true
  if (/^(?:\$\{.+\}|\$\(.+\)|%[A-Z0-9_]+%|<[^>]+>)$/i.test(value)) return true
  return /(?:placeholder|example|changeme|change-me|your[_ -]|dummy|mock|not[-_ ]a[-_ ]real|redacted)/i.test(value)
}

const findings = []
const add = (file, line, rule, key = '') => findings.push({ file, line, rule, key })

for (const file of files) {
  const extension = path.extname(file).toLowerCase()
  if (!textExtensions.has(extension) && !/dockerfile$/i.test(file)) continue
  const fullPath = path.join(root, file)
  const isStructuredConfig = ['.json', '.yaml', '.yml', '.toml', '.config', '.conf', '.env'].includes(extension) || /dockerfile$/i.test(file)
  if (statSync(fullPath).size > 2 * 1024 * 1024) continue
  const buffer = readFileSync(fullPath)
  if (buffer.includes(0)) continue
  const lines = buffer.toString('utf8').split(/\r?\n/)

  lines.forEach((line, index) => {
    const lineNumber = index + 1
    if (/-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/.test(line)) add(file, lineNumber, 'private-key')
    if (/\b(?:AKIA|ASIA)[A-Z0-9]{16}\b/.test(line) || /\bgh[pousr]_[A-Za-z0-9]{30,}\b/.test(line)) add(file, lineNumber, 'token-pattern')

    const uri = line.match(/\b[a-z][a-z0-9+.-]*:\/\/([^\s/:@]+):([^\s/@]+)@/i)
    if (uri && !safeValue(uri[2])) add(file, lineNumber, 'credential-uri')

    const connectionPassword = line.match(/(?:^|[;\"'])\s*(Password|Pwd)\s*=\s*([^;\"']+)/i)
    if (connectionPassword && !safeValue(connectionPassword[2])) add(file, lineNumber, 'connection-string-password', connectionPassword[1])

    if (isStructuredConfig) {
      const jsonAssignment = line.match(/[\"']([^\"']*(?:password|secret|secretkey|accesskey|clientsecret|securitykey))[^\"']*[\"']\s*:\s*[\"']([^\"']*)[\"']/i)
      if (jsonAssignment && !safeValue(jsonAssignment[2])) add(file, lineNumber, 'sensitive-config-value', jsonAssignment[1])

      const yamlAssignment = line.match(/^\s*([A-Za-z0-9_.-]*(?:password|secret|secretkey|accesskey|clientsecret|securitykey))\s*:\s*(.+?)\s*$/i)
      if (yamlAssignment && !safeValue(yamlAssignment[2])) add(file, lineNumber, 'sensitive-config-value', yamlAssignment[1])
    }

    const environmentLiteral = line.match(/WithEnvironment\(\"([^\"]*(?:password|secret|securitykey)[^\"]*)\"\s*,\s*\"([^\"]+)\"/i)
    if (environmentLiteral && !safeValue(environmentLiteral[2])) add(file, lineNumber, 'hard-coded-environment-secret', environmentLiteral[1])
  })
}

if (findings.length > 0) {
  console.error('检测到疑似敏感信息（仅显示位置和规则，不回显内容）：')
  for (const finding of findings) {
    const key = finding.key ? ' key=' + finding.key : ''
    console.error('- ' + finding.file + ':' + finding.line + ' [' + finding.rule + ']' + key)
  }
  process.exit(1)
}

console.log('敏感信息扫描通过：已检查 ' + files.length + ' 个受版本控制文件。')
