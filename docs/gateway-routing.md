# 网关路由契约与前端访问约定

## 目标

前端只使用一个网关基址访问 Identity、Vocabulary、Practice 和 Files。仓库通过可审查的 JSON 契约与自动校验，避免 Development、Docker 和外部 AgileConfig 的路由转换静默漂移。

## 公开路径

| 服务域 | 前端公开路径 | 网关转发路径 | 当前目标集群 |
| --- | --- | --- | --- |
| Identity | `/identity/v{n}/{**catch-all}` | `/api/v{n}/identity/{**catch-all}` | `identity-cluster` |
| Vocabulary | `/vocabulary/v{n}/{**catch-all}` | `/api/v{n}/vocabulary/{**catch-all}` | `vocabulary-cluster` |
| Practice | `/practice/v{n}/{**catch-all}` | `/api/v{n}/practice/{**catch-all}` | `practice-cluster` |
| Files | `/files/{**catch-all}` | `/{**catch-all}` | `files-cluster` |

Files 当前只在前端使用已从服务端确认的 `/files/content` HTTP 内容读取端点。上传仍是 gRPC 契约，不把它猜测为 HTTP 路由。

契约源文件：`src/ApiGateway/Fantasy.ApiGateway/gateway-routes.contract.json`。

## 前端基址

- 浏览器默认使用同源 `/`，生产部署不再隐式回退到 `http://localhost:5000`。
- `VITE_API_BASE_URL` 仅在前端与网关不同源时设置，例如测试环境的独立网关域名。
- 本地 Vite 开发服务器将 `/identity`、`/vocabulary`、`/practice`、`/files` 代理到 `VITE_DEV_GATEWAY_TARGET`；未设置时只在开发工具中默认使用 `http://localhost:5000`。
- 服务域路径统一由 `src/UIs/lexicraft-vue-frontend/src/config/apiRoutes.ts` 生成，业务模块不得自行拼接另一套主机地址。

## 自动校验

在仓库根目录执行：

```powershell
node scripts/gateway/validate-route-contract.mjs
```

默认校验：

- Development 与 Docker 的路由 ID、匹配路径、YARP 转换和集群集合；
- 两个环境各自的目标地址；
- 路由不绑定固定 `Hosts`；
- 网关启动管道仍接入 CORS、安全响应头、限流、默认健康端点和反向代理。

外部 AgileConfig 导出为与 `appsettings` 相同的 JSON 结构后，可执行：

```powershell
node scripts/gateway/validate-route-contract.mjs path\to\sanitized-gateway-config.json
```

外部配置校验只比较公开路由、转换和集群集合，不校验环境专属目标地址。导出文件必须脱敏，不能包含 AgileConfig Secret、连接字符串或其他凭据。

## 验证边界

契约脚本属于静态配置检查，不能证明目标服务可达、OAuth 可用或数据库连接正常。发布前仍需在实际 Aspire、Compose 或部署环境中验证 `/health`、CORS 预检、401/403/429、四个路由的最小转发以及认证刷新链路。