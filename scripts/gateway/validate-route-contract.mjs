#!/usr/bin/env node
import {readFileSync} from 'node:fs'
import path from 'node:path'
import {fileURLToPath} from 'node:url'

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url))
const root = path.resolve(scriptDirectory, '../..')
const contractPath = path.join(root, 'src/ApiGateway/Fantasy.ApiGateway/gateway-routes.contract.json')
const contract = readJson(contractPath)
const externalConfig = process.argv[2]

function readJson(filePath) {
  return JSON.parse(readFileSync(filePath, 'utf8'))
}

function assert(condition, message) {
  if (!condition) throw new Error(message)
}

function assertSameKeys(actual, expected, label) {
  const actualKeys = Object.keys(actual ?? {}).sort()
  const expectedKeys = Object.keys(expected ?? {}).sort()
  assert(JSON.stringify(actualKeys) === JSON.stringify(expectedKeys), `${label} 不一致：实际 ${actualKeys.join(', ')}；期望 ${expectedKeys.join(', ')}`)
}

function validateRoute(routeId, actual, expected) {
  assert(actual, `缺少路由 ${routeId}`)
  assert(actual.ClusterId === expected.clusterId, `${routeId}.ClusterId 应为 ${expected.clusterId}`)
  assert(actual.Match?.Path === expected.publicPath, `${routeId}.Match.Path 应为 ${expected.publicPath}`)
  assert(!actual.Match?.Hosts, `${routeId} 不应绑定 Hosts；公开域名由部署环境决定`)

  const expectedTransforms = [
    {PathPattern: expected.upstreamPath},
    {'X-Forwarded': 'Set'},
    {RequestHeader: 'X-Forwarded-Prefix', Set: expected.forwardedPrefix},
  ]
  assert(
    JSON.stringify(actual.Transforms) === JSON.stringify(expectedTransforms),
    `${routeId}.Transforms 与路由契约不一致`,
  )
}

function validateProxyConfig(config, label, destinations) {
  const proxy = config.ReverseProxy
  assert(proxy?.Routes && proxy?.Clusters, `${label} 缺少 ReverseProxy.Routes 或 ReverseProxy.Clusters`)
  assertSameKeys(proxy.Routes, contract.routes, `${label} 路由集合`)

  for (const [routeId, expected] of Object.entries(contract.routes)) {
    validateRoute(routeId, proxy.Routes[routeId], expected)
  }

  const expectedClusterIds = [...new Set(Object.values(contract.routes).map(route => route.clusterId))]
  assertSameKeys(proxy.Clusters, Object.fromEntries(expectedClusterIds.map(id => [id, true])), `${label} 集群集合`)

  if (destinations) {
    for (const [clusterId, expectedAddress] of Object.entries(destinations)) {
      const destinationEntries = Object.values(proxy.Clusters[clusterId]?.Destinations ?? {})
      assert(destinationEntries.length === 1, `${label}.${clusterId} 必须且只能配置一个目标地址`)
      assert(destinationEntries[0].Address === expectedAddress, `${label}.${clusterId} 地址应为 ${expectedAddress}`)
    }
  }
}

function validateGatewayPipeline() {
  const program = readFileSync(path.join(root, 'src/ApiGateway/Fantasy.ApiGateway/Program.cs'), 'utf8')
  for (const requiredCall of [
    'app.UseDefaultCors();',
    'app.UseSecurityHeaders();',
    'app.UseRateLimiter();',
    'app.MapDefaultEndpoints();',
    'app.MapReverseProxy();',
  ]) {
    assert(program.includes(requiredCall), `网关启动管道缺少 ${requiredCall}`)
  }
}

try {
  if (externalConfig) {
    const resolved = path.resolve(process.cwd(), externalConfig)
    validateProxyConfig(readJson(resolved), `外部配置 ${resolved}`)
    console.log(`网关路由契约通过：${resolved}`)
  } else {
    for (const [profileName, profile] of Object.entries(contract.profiles)) {
      const configPath = path.join(root, profile.config)
      validateProxyConfig(readJson(configPath), profileName, profile.destinations)
    }
    validateGatewayPipeline()
    console.log(`网关路由契约通过：${Object.keys(contract.routes).length} 条路由，${Object.keys(contract.profiles).length} 个配置环境。`)
  }
} catch (error) {
  console.error(`网关路由契约失败：${error.message}`)
  process.exit(1)
}