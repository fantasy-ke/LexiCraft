# BuildingBlocks.OSS

用于对象存储服务 (OSS) 跨平台交互的核心组件，支持阿里云 (Aliyun)、腾讯云 (QCloud) 和 Minio 等多种提供商。

## 主要特性

- **多平台支持**：统一封装阿里云、腾讯云、Minio 的基础操作。
- **工厂模式**：根据配置自动注入对应的服务实现。
- **扩展性好**：易于增加新的存储提供商。
- **缓存支持**：内置对象元数据和预签名 URL 缓存。

## 快速开始

### 1. 注册服务

在 `Program.cs` 中添加服务：

```csharp
using BuildingBlocks.OSS;

// 方式 1：通过 Action 配置
builder.AddOssService(options => {
    options.Provider = OSSProvider.Minio;
    options.Endpoint = "localhost:9000";
    options.AccessKey = "your-access-key";
    options.SecretKey = "your-secret-key";
    options.DefaultBucket = "test-bucket";
    options.Enable = true;
    options.IsEnableHttps = false;
});

// 方式 2：使用配置文件中的默认节点 (OSSOptions)
builder.AddOssService();
```

### 2. appsettings.json 配置示例

```json
{
  "OSSOptions": {
    "Provider": 1, // 1: Minio, 2: Aliyun, 3: QCloud
    "Enable": true,
    "Endpoint": "localhost:9000",
    "AccessKey": "minio",
    "SecretKey": "minio123",
    "DefaultBucket": "test-bucket",
    "IsEnableHttps": false,
    "IsEnableCache": true
  }
}
```

### 3. 使用服务

通过依赖注入，获取对应提供商的服务接口：

```csharp
using BuildingBlocks.OSS.Interface;
using BuildingBlocks.OSS.EntityType;
using BuildingBlocks.OSS.Models.Dto;

public class MyFileService(IOSSService<OSSMinio> ossService)
{
    public async Task UploadAsync(Stream stream, string fileName)
    {
        var input = new UploadObjectInput
        {
            BucketName = "test-bucket",
            ObjectName = fileName,
            Stream = stream
        };
        await ossService.PutObjectAsync(input);
    }
}
```

## 核心接口

- `IOSSService<T>`: 基础操作接口（上传、下载、删除、存储桶操作等）。
- `IAliyunOssService`: 阿里云特有操作扩展。
- `IMinioOssService`: Minio 特有操作扩展。
- `IQCloudOSSService`: 腾讯云特有操作扩展。
