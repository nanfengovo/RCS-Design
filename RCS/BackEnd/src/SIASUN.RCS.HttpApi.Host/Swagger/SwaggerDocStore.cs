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
        /// <summary>
        /// 整个进程只有一个Store,单例写法
        /// </summary>
        public static SwaggerDocStore Instance { get; } = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            // 忽略大小写
            PropertyNameCaseInsensitive = true,
        };

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



    }
}
