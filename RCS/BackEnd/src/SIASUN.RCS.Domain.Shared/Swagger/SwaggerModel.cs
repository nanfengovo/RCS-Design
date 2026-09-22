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
        /// 存到内存里
        /// </summary>
        public  Dictionary<string, ApiDoc>? Operations { get; set; }
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
