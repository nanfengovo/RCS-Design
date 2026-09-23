using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SIASUN.RCS.Swagger
{
    /// <summary>
    /// 把 SwaggerDoc.json 里的词条贴到对应 OpenAPI Operation 上。
    /// </summary>
    public class SwaggerDocOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!SwaggerDocStore.Instance.TryGet(
                    context.ApiDescription.HttpMethod,
                    context.ApiDescription.RelativePath,
                    out var doc)
                || doc is null)
                return;

            if (!string.IsNullOrWhiteSpace(doc.Summary))
                operation.Summary = doc.Summary;

            if (!string.IsNullOrWhiteSpace(doc.Description))
                operation.Description = doc.Description;

            ApplyRequestExample(operation, doc);
            ApplyResponseExample(operation, doc);
            ApplyCallExamples(operation, doc);
        }

        /// <summary>
        /// 请求体 Example：挂到所有 *json media type（避免 UI 选到 text/json 时看不到）。
        /// </summary>
        private static void ApplyRequestExample(OpenApiOperation operation, ApiDoc doc)
        {
            if (doc.RequestExample is not { } reqEx
                || reqEx.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                return;

            var node = JsonNode.Parse(reqEx.GetRawText());
            if (node is null || operation.RequestBody?.Content is null)
                return;

            SetExampleOnJsonContent(operation.RequestBody.Content, node);
        }

        /// <summary>
        /// 200 响应 Example：没有 Content / json 时补上，保证能写进去。
        /// </summary>
        private static void ApplyResponseExample(OpenApiOperation operation, ApiDoc doc)
        {
            if (doc.ResponseExample is not { } resEx
                || resEx.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                return;

            var node = JsonNode.Parse(resEx.GetRawText());
            if (node is null)
                return;

            operation.Responses ??= new OpenApiResponses();

            // Responses 存的是 IOpenApiResponse（Content 只读接口），需要可写的 OpenApiResponse
            OpenApiResponse writable;
            if (operation.Responses.TryGetValue("200", out var existing) && existing is OpenApiResponse existingWritable)
            {
                writable = existingWritable;
                writable.Content ??= new Dictionary<string, OpenApiMediaType>();
            }
            else if (existing is not null)
            {
                writable = new OpenApiResponse
                {
                    Description = existing.Description,
                    Content = existing.Content != null
                        ? new Dictionary<string, OpenApiMediaType>(existing.Content)
                        : new Dictionary<string, OpenApiMediaType>()
                };
                operation.Responses["200"] = writable;
            }
            else
            {
                writable = new OpenApiResponse
                {
                    Description = "OK",
                    Content = new Dictionary<string, OpenApiMediaType>()
                };
                operation.Responses["200"] = writable;
            }

            SetExampleOnJsonContent(writable.Content!, node);

            if (!writable.Content!.Keys.Any(k =>
                    k.Contains("json", StringComparison.OrdinalIgnoreCase)))
            {
                writable.Content["application/json"] = new OpenApiMediaType { Example = node };
            }
        }

        /// <summary>
        /// 调用示例没有标准 OpenAPI 字段 → 拼进 Description（Markdown）。
        /// </summary>
        private static void ApplyCallExamples(OpenApiOperation operation, ApiDoc doc)
        {
            if (doc.CallExamples is not { Count: > 0 })
                return;

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(operation.Description))
                sb.AppendLine(operation.Description).AppendLine();

            sb.AppendLine("### 调用示例");
            foreach (var ex in doc.CallExamples)
            {
                if (string.IsNullOrWhiteSpace(ex.Code))
                    continue;

                sb.AppendLine($"**{ex.Title ?? ex.Language ?? "example"}**");
                sb.AppendLine();
                sb.Append("```").Append(ex.Language ?? "").AppendLine();
                sb.AppendLine(ex.Code.Trim());
                sb.AppendLine("```");
                sb.AppendLine();
            }

            operation.Description = sb.ToString();
        }

        /// <summary>
        /// 对 Content 里每个含 json 的 media type：保留 Schema，整项换成带 Example 的 OpenApiMediaType。
        /// </summary>
        private static void SetExampleOnJsonContent(
            IDictionary<string, OpenApiMediaType> content,
            JsonNode node)
        {
            foreach (var key in content.Keys.ToList())
            {
                if (!key.Contains("json", StringComparison.OrdinalIgnoreCase))
                    continue;

                content.TryGetValue(key, out var existing);
                content[key] = new OpenApiMediaType
                {
                    Schema = existing?.Schema,
                    Example = node
                };
            }
        }
    }
}
