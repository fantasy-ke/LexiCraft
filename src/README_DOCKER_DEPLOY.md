LexiCraft 部署方案说明（Docker / 蓝绿 / Swarm / Kubernetes）
===========================================================

目录
----

- 一、基础说明
- 二、单栈 docker compose 部署（当前默认方式）
- 三、基于 docker compose 的蓝绿部署 + Nginx/Traefik
- 四、基于 Docker Swarm 的滚动更新
- 五、基于 Kubernetes 的滚动更新

一、基础说明
------------

当前仓库根路径：`d:/A_StudyCode/APersonalProjects/LexiCraft`

在 `src/compose.yaml` 中定义了以下服务：

- `lexicraft.apigateway`：API 网关，对外暴露 HTTP 端口
- `files-grpc`：文件服务（gRPC）
- `identity-api`：身份认证服务
- `vocabulary-api`：词汇服务
- `practice-api`：练习服务

所有服务都加入 `lexicraft-net` 网络，镜像由同目录下的 Dockerfile 构建。

二、单栈 docker compose 部署
---------------------------

适合本地开发、测试或对短时间中断不敏感的环境。

在 `src` 目录下执行：

```bash
cd src
docker compose up -d --build
```

更新代码后重新部署（会有短暂中断）：

```bash
cd src
docker compose up -d --build --force-recreate
```

此时：

- 网关对外端口为 `5000`（映射到容器内 `8080`）
- 访问地址类似：`http://服务器IP:5000`

三、基于 docker compose 的蓝绿部署 + Nginx/Traefik
------------------------------------------------

目标：在单机环境中通过两套独立的 compose 栈（blue / green），配合 Nginx 或 Traefik，实现近乎无停机的版本切换。

### 3.1 关键设计点

- 使用同一个 `src/compose.yaml` 文件，依靠 `project name` 隔离 blue / green 栈
- 通过环境变量 `APIGATEWAY_PORT` 控制网关对外端口
- 外层使用 Nginx/Traefik，只暴露一个稳定入口（例如 80 端口），内部再指向 blue 或 green 网关

为支持多栈并行运行，已对 `src/compose.yaml` 做了两点调整：

- 移除所有服务的 `container_name`，避免多栈时容器名冲突
- 将网关端口改为：`"${APIGATEWAY_PORT:-5000}:8080"`

### 3.2 启动 blue 栈

在 `src` 目录下执行：

Linux / macOS（bash / zsh）：

```bash
cd src
APIGATEWAY_PORT=5000 docker compose -p lexicraft_blue up -d --build
```

Windows PowerShell：

```powershell
cd src
$env:APIGATEWAY_PORT="5000"
docker compose -p lexicraft_blue up -d --build
```

说明：

- `-p lexicraft_blue`：指定当前栈的工程名为 `lexicraft_blue`
- `APIGATEWAY_PORT=5000`：blue 栈对外暴露端口为 5000

此时 blue 栈网关访问地址为：`http://服务器IP:5000`。

### 3.3 启动 green 栈

保持 blue 栈继续运行，在 `src` 目录下再启动一套 green 栈：

Linux / macOS（bash / zsh）：

```bash
cd src
APIGATEWAY_PORT=5001 docker compose -p lexicraft_green up -d --build
```

Windows PowerShell：

```powershell
cd src
$env:APIGATEWAY_PORT="5001"
docker compose -p lexicraft_green up -d --build
```

说明：

- green 栈的工程名为 `lexicraft_green`
- 对外暴露端口为 5001

此时：

- blue：`http://服务器IP:5000`
- green：`http://服务器IP:5001`

两套后端完全独立，可以分别访问验证。

### 3.4 使用 Nginx 做蓝绿切换

假设 Nginx 运行在宿主机上，对外监听 80 端口。其反向代理目标可以配置为：

- blue：`http://127.0.0.1:5000`
- green：`http://127.0.0.1:5001`

一个最简思路是：

- 初始让 Nginx 只转发到 blue
- 部署 green，验证通过后将 Nginx upstream 改为 green
- 观察一段时间后，如果一切正常，停止 blue 栈

示例步骤：

1. 启动 blue 栈（5000 端口）
2. Nginx 代理到 blue
3. 部署 green 栈到 5001 端口
4. 验证 green 正常（直接访问 5001）
5. 修改 Nginx 配置，将流量切到 green
6. 重载 Nginx 配置
7. 确认 green 运行稳定后，关闭 blue 栈

相关 docker compose 命令：

Linux / macOS（bash / zsh）：

```bash
cd src

APIGATEWAY_PORT=5000 docker compose -p lexicraft_blue up -d --build

APIGATEWAY_PORT=5001 docker compose -p lexicraft_green up -d --build

docker compose -p lexicraft_blue ps
docker compose -p lexicraft_green ps

docker compose -p lexicraft_blue down
```

Windows PowerShell：

```powershell
cd src

$env:APIGATEWAY_PORT="5000"
docker compose -p lexicraft_blue up -d --build

$env:APIGATEWAY_PORT="5001"
docker compose -p lexicraft_green up -d --build

docker compose -p lexicraft_blue ps
docker compose -p lexicraft_green ps

docker compose -p lexicraft_blue down
```

### 3.5 使用 Traefik 的思路

如果使用 Traefik，可以：

- 将 blue 和 green 栈都接入同一个 Docker 网络
- 通过 Traefik 的路由规则（例如 Host、Header、Path）将流量导向不同栈
- 通过调整路由规则或权重，实现按百分比灰度/蓝绿切换

四、基于 Docker Swarm 的滚动更新
------------------------------

Docker Swarm 适合在多节点或需要简单编排的场景下使用，通过 `deploy` 段落可以定义副本数量和滚动更新策略。

### 4.1 基本思路

- 将当前 `src/compose.yaml` 作为 Swarm stack 的基础
- 在每个服务下增加 `deploy` 配置，指定：
  - `replicas`：副本数
  - `update_config`：滚动更新策略，例如并行更新数、暂停时间
- 使用 `docker stack deploy` 部署

### 4.2 Swarm 初始化

在任意管理节点上初始化 Swarm（如果已经有 Swarm 可跳过）：

```bash
docker swarm init
```

### 4.3 在 compose 中增加 deploy 配置（示例）

以 `identity-api` 为例，在 `services.identity-api` 下可以增加：

```yaml
  identity-api:
    image: lexicraft.services.identity.api
    build:
      context: .
      dockerfile: microservices/Identity/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: "Docker"
      AgileConfig__Nodes: "http://172.144.21.1:8500"
      AgileConfig__AppId: "identity-api"
      AgileConfig__Secret: "bb123456"
      AgileConfig__ENV: "TEST"
    restart: unless-stopped
    networks:
      - lexicraft-net
    deploy:
      replicas: 2
      update_config:
        parallelism: 1
        delay: 10s
      restart_policy:
        condition: on-failure
```

其中：

- `replicas: 2`：保持 2 个副本
- `parallelism: 1`：滚动更新时一次只更新一个副本
- `delay: 10s`：每个副本更新完成后等待 10 秒再更新下一个

可以为其他服务根据需要添加相似的 `deploy` 段落。

### 4.4 使用 docker stack 部署

在 `src` 目录下执行：

```bash
cd src
docker stack deploy -c compose.yaml lexicraft
```

查看服务状态：

```bash
docker stack services lexicraft
docker service ls
docker service ps lexicraft_identity-api
```

更新代码和镜像后，只需再次执行部署命令：

```bash
cd src
docker stack deploy -c compose.yaml lexicraft
```

Swarm 会根据 `deploy.update_config` 配置，对各服务进行滚动更新，确保始终有部分副本在对外提供服务，从而避免整体停机。

五、基于 Kubernetes 的滚动更新
----------------------------

Kubernetes 提供原生的滚动更新能力，适合更复杂和大规模的生产环境。

### 5.1 基本思路

- 为每个服务创建 Deployment + Service
- Deployment 使用 `RollingUpdate` 策略
- 通过镜像标签（例如 `:v1`, `:v2`）控制版本
- 使用 `kubectl apply` 更新 Deployment，Kubernetes 负责按策略滚动更新 Pod

### 5.2 示例：API 网关 Deployment 和 Service

以下为一个简化示例，假设镜像已推送为 `your-registry/lexicraft.apigateway:v1`：

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: lexicraft-apigateway
  labels:
    app: lexicraft-apigateway
spec:
  replicas: 2
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxUnavailable: 0
      maxSurge: 1
  selector:
    matchLabels:
      app: lexicraft-apigateway
  template:
    metadata:
      labels:
        app: lexicraft-apigateway
    spec:
      containers:
        - name: apigateway
          image: your-registry/lexicraft.apigateway:v1
          ports:
            - containerPort: 8080
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "Docker"
            - name: AgileConfig__Nodes
              value: "http://172.144.21.1:8500"
            - name: AgileConfig__AppId
              value: "api-gateway"
            - name: AgileConfig__Secret
              value: "bb123456"
            - name: AgileConfig__ENV
              value: "TEST"
---
apiVersion: v1
kind: Service
metadata:
  name: lexicraft-apigateway
spec:
  type: LoadBalancer
  selector:
    app: lexicraft-apigateway
  ports:
    - name: http
      port: 80
      targetPort: 8080
```

### 5.3 滚动更新操作示例

1. 初始部署 v1 版本：

```bash
kubectl apply -f apigateway-deployment.yaml
```

2. 构建并推送 v2 镜像，例如 `your-registry/lexicraft.apigateway:v2`

3. 更新 Deployment 使用 v2 镜像：

- 可以修改 YAML 中的 `image` 字段后再次执行 `kubectl apply`
- 或使用命令行方式：

```bash
kubectl set image deployment/lexicraft-apigateway apigateway=your-registry/lexicraft.apigateway:v2
```

4. 查看滚动更新进度：

```bash
kubectl rollout status deployment/lexicraft-apigateway
```

如果更新出现问题，可以回滚到上一个版本：

```bash
kubectl rollout undo deployment/lexicraft-apigateway
```

### 5.4 其他服务的迁移思路

- 为 `files-grpc`、`identity-api`、`vocabulary-api`、`practice-api` 分别创建对应的 Deployment 和 Service
- 复用 compose.yaml 中的环境变量配置
- 根据服务对外暴露方式（ClusterIP / NodePort / LoadBalancer）选择合适的 Service 类型

六、方案选择建议
----------------

- 开发 / 测试环境：
  - 可以继续使用单栈 docker compose
  - 如果希望减小中断，可以结合蓝绿部署方案
- 小规模生产：
  - 建议使用 docker compose 蓝绿部署 + Nginx/Traefik
  - 部署简单、可控性强
- 多节点或更大规模生产：
  - 可以考虑使用 Docker Swarm 或 Kubernetes
  - 利用其内置的滚动更新、健康检查、扩缩容能力
