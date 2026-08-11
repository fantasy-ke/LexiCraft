# BuildingBlocks.OSS

`BuildingBlocks.OSS` 为 MinIO、阿里云 OSS、腾讯云 COS 和自定义对象存储实现提供统一接口、命名实例选择、配置校验及预签名 URL 缓存。

## 设计目标

- 业务层只依赖 `IOSSService` 或 `IOSSServiceFactory`，不直接创建厂商 SDK Client。
- 工厂只向业务层暴露实例选择和默认存储桶，不返回 AccessKey、SecretKey 等连接凭据。
- 同一服务可以同时配置多个对象存储实例，例如主存储、归档存储和迁移目标。
- 所有连接配置统一放入 `Providers`，避免模块配置与单个提供商配置耦合。
- 新增提供商时通过注册扩展，不修改 `OssServiceFactory` 的 `switch`。
- `OSSOptions.Enable=false` 时仍可解析 `IOSSService`，避免可选 OSS 导致宿主 DI 启动失败。

## 核心抽象

| 类型 | 作用 |
| --- | --- |
| `IOSSService` | 跨提供商的存储桶、对象、ACL 和预签名 URL 操作 |
| `IOSSServiceFactory` | 获取默认或指定名称的对象存储实例及默认存储桶 |
| `IOSSProviderActivator` | 根据单个提供商配置创建实现，主要用于扩展新提供商 |
| `OSSOptions` | 模块开关、默认实例名称和命名提供商集合 |
| `OSSProviderOptions` | 单个命名对象存储实例的连接配置 |
| `IAliyunOssService` | 阿里云特有能力 |
| `IMinioOssService` | MinIO 特有能力 |
| `IQCloudOSSService` | 腾讯云特有能力 |

内置提供商类型名称：

- `OSSProviderNames.Minio`
- `OSSProviderNames.Aliyun`
- `OSSProviderNames.QCloud`

## 服务注册

```csharp
using BuildingBlocks.OSS;

builder.AddOssService();
```

OpenAPI、数据库等其他模块不需要感知具体 OSS SDK。

## 推荐：命名多提供商配置

```json
{
  "OSSOptions": {
    "Enable": true,
    "DefaultProvider": "primary",
    "Providers": {
      "primary": {
        "Type": "Minio",
        "Endpoint": "localhost:9000",
        "AccessKey": "${MINIO_ACCESS_KEY}",
        "SecretKey": "${MINIO_SECRET_KEY}",
        "Region": "us-east-1",
        "DefaultBucket": "lexicraft-files",
        "IsEnableHttps": false,
        "IsEnableCache": true
      },
      "archive": {
        "Type": "Aliyun",
        "Endpoint": "https://oss-cn-hangzhou.aliyuncs.com",
        "AccessKey": "${ALIYUN_ACCESS_KEY}",
        "SecretKey": "${ALIYUN_SECRET_KEY}",
        "DefaultBucket": "lexicraft-archive",
        "IsEnableHttps": true
      }
    }
  }
}
```

配置文件中不要提交真实密钥。示例中的 `${...}` 仅表示应由环境变量、用户机密或部署平台注入。

### 使用默认实例

```csharp
using BuildingBlocks.OSS.Interface;

public sealed class FileStorage(IOSSService ossService)
{
    public Task<bool> UploadAsync(string bucket, string objectName, Stream stream,
        CancellationToken cancellationToken)
    {
        return ossService.PutObjectAsync(bucket, objectName, stream, cancellationToken);
    }
}
```

`IOSSService` 对应 `DefaultProvider` 指定的实例。

### 使用指定实例

```csharp
using BuildingBlocks.OSS.Interface;

public sealed class ArchiveStorage(IOSSServiceFactory ossServiceFactory)
{
    public Task<bool> ArchiveAsync(string objectName, Stream stream,
        CancellationToken cancellationToken)
    {
        var archiveBucket = ossServiceFactory.GetDefaultBucket("archive");
        var archiveService = ossServiceFactory.Create("archive");

        return archiveService.PutObjectAsync(
            archiveBucket,
            objectName,
            stream,
            cancellationToken);
    }
}
```

同一名称的实例由工厂缓存并复用。只有一个命名实例且未设置 `DefaultProvider` 时，会自动选择该实例。配置以应用启动时的 Options 快照为准，修改配置后应重启服务。

## 禁用对象存储

```json
{
  "OSSOptions": {
    "Enable": false
  }
}
```

禁用后：

- `IOSSService` 和 `IOSSServiceFactory` 仍可被依赖注入解析。
- `IOSSServiceFactory.IsEnabled` 为 `false`。
- 真正调用 OSS 操作会抛出明确的 `InvalidOperationException`。
- 业务服务可以根据 `IsEnabled` 或 `DefaultBucket` 决定使用本地文件存储等回退方案。

## 提供商专用接口

如果某个内置类型只配置了一个实例，可以注入对应专用接口：

```csharp
public sealed class MinioAdministration(IMinioOssService minioService)
{
    // 使用 MinIO 特有的 Policy、未完成上传等能力。
}
```

如果同一类型配置了多个实例，优先使用 `IOSSServiceFactory.Create("实例名")`。只有默认实例能够消除歧义时，才会注册提供商专用接口别名。

## 扩展新的提供商

新的实现只需实现 `IOSSService`，并提供包含 `OSSProviderOptions` 参数的公开构造函数。其他依赖会由 DI 提供：

```csharp
builder.Services.AddOssProvider<MyS3Service>("S3");
builder.AddOssService();
```

配置中通过 `Type` 使用自定义类型：

```json
{
  "OSSOptions": {
    "Enable": true,
    "DefaultProvider": "s3-primary",
    "Providers": {
      "s3-primary": {
        "Type": "S3",
        "Endpoint": "https://s3.example.com",
        "DefaultBucket": "files"
      }
    }
  }
}
```

`Type` 用于匹配通过 `AddOssProvider<TService>` 注册的实现，因此新增厂商不需要修改工厂代码。自定义实现负责校验自己的专有配置。

## 启动校验

启用模块时会检查：

- 默认实例名称是否存在。
- 每个实例是否配置 `Type`。
- MinIO、阿里云和腾讯云的 Endpoint、AccessKey、SecretKey 等必要字段。
- 每个实例的提供商类型是否已经注册。

禁用模块时不要求提供连接配置。

## 从旧配置迁移

旧的根级 `Provider`、`Endpoint`、`AccessKey`、`SecretKey` 等单提供商字段已移除。升级时需要：

1. 在 `Providers` 下创建一个命名实例，例如 `primary`。
2. 将原连接字段移入该实例，并使用字符串 `Type` 指定 `Minio`、`Aliyun`、`QCloud` 或自定义类型。
3. 将 `DefaultProvider` 设置为该实例名称；只有一个实例时可以省略。
4. 普通业务继续注入 `IOSSService`，只有选择非默认实例时才注入 `IOSSServiceFactory`。
5. 不要在业务层根据厂商写 `switch`；厂商差异应保留在 Provider 实现内。
