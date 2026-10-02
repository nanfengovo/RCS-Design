using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SIASUN.RCS.Swagger
{
    /// <summary>
    /// swagger 文档建模
    /// </summary>
    public class SwaggerModel
    {
        /// <summary>
        /// Swagger Tag（分组标题）说明，键为 Tag 名，如 AbpApiDefinition。
        /// </summary>
        public Dictionary<string, TagDoc>? Tags { get; set; }

        /// <summary>
        /// 存到内存里
        /// </summary>
        public Dictionary<string, ApiDoc>? Operations { get; set; }
    }

    /// <summary>
    /// Swagger 分组（Tag）文档
    /// </summary>
    public class TagDoc
    {
        /// <summary>
        /// 显示名 / 短标题（可选，不填则沿用 Tag 名）
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 分组说明，显示在 Swagger UI 标题旁
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// API 文档
    /// </summary>
    public class ApiDoc
    {
        /// <summary>
        /// 接口摘要
        /// </summary>
        public  string? Summary { get; set; }

        /// <summary>
        /// 接口描述
        /// </summary>

        public  string? Description { get; set; }

        /// <summary>
        /// 请求示例
        /// </summary>

        public  JsonElement? RequestExample { get; set; }

        /// <summary>
        /// 响应示例
        /// </summary>

        public  JsonElement? ResponseExample { get; set; }

        /// <summary>
        /// 调用示例
        /// </summary>

        public  List<CallExamples>? CallExamples { get; set; }
    }

    public class CallExamples
    {
        /// <summary>
        /// 示例类型
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// 示例语言
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// 示例代码
        /// </summary>
        public string? Code { get; set; }
    }


}
