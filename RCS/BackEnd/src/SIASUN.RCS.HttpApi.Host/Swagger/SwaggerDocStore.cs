using Grpc.Net.Client.Balancer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SIASUN.RCS.Swagger
{
    /// <summary>
    /// 把额外的配置扩展加载到内存
    /// </summary>
    public class SwaggerDocStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            // 忽略大小写
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// 整个进程只有一个Store,单例写法
        /// </summary>
        public static SwaggerDocStore Instance { get; } = new();

        /// <summary>
        /// 存 json
        /// </summary>

        private readonly SwaggerModel? _model;

        /// <summary>
        /// 赋值
        /// </summary>
        private SwaggerDocStore()
        {
            _model = Load();    
        }

        /// <summary>
        /// 加载配置文件的json
        /// </summary>
        /// <returns></returns>
        private static SwaggerModel Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Swagger", "SwaggerDoc.json");
            if (!File.Exists(path))
                return new SwaggerModel { Operations = new Dictionary<string, ApiDoc>() };

            try
            {
                var json = File.ReadAllText(path,Encoding.UTF8);
                return JsonSerializer.Deserialize<SwaggerModel>(json, JsonOptions)
                ?? new SwaggerModel { Operations = new Dictionary<string, ApiDoc>() };
            }
            catch
            {
                // MVP：别让启动崩；以后可加 ILogger
                return new SwaggerModel { Operations = new Dictionary<string, ApiDoc>() };
            }
        }

        /// <summary>
        /// 尝试获取接口文档
        /// </summary>
        /// <param name="method">方法类型</param>
        /// <param name="path">请求路径</param>
        /// <param name="doc">输出的接口文档</param>
        /// <returns></returns>
        public bool TryGet(string? method, string? path, out ApiDoc? doc)
        {
            doc = null;
            if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(path))
                return false;

            if(_model!.Operations is null || _model.Operations.Count == 0)
                return false;
            var key = NormalizeKey(method, path);

            return _model.Operations.TryGetValue(key, out doc);
        }

        /// <summary>
        /// 统一方法和接口的格式
        /// </summary>
        /// <param name="httpMethod">方法</param>
        /// <param name="relativePath">接口路径</param>
        /// <returns></returns>
        public static string NormalizeKey(string httpMethod, string relativePath)
        {
            var path = relativePath.Split('?', 2)[0].Trim().TrimEnd('/');
            if (!path.StartsWith('/'))
                path = "/" + path;
            return $"{httpMethod.Trim().ToUpperInvariant()} {path}";
        }

    }
}
