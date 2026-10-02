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
    /// 按文档名中的文化，把 i18n JSON 贴到 Operation 上。
    /// </summary>
    public class SwaggerDocOperationFilter : IOperationFilter
    {
        private readonly ISwaggerDocStore _store;

        public SwaggerDocOperationFilter(ISwaggerDocStore store)
        {
            _store = store;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var culture = SwaggerDocNames.GetCulture(context.DocumentName) ?? _store.DefaultCulture;
            culture = _store.ResolveCulture(culture);

            if (!_store.TryGetOperation(
                    culture,
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
            ApplyCallExamples(operation, doc, culture);
        }

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

        private static void ApplyResponseExample(OpenApiOperation operation, ApiDoc doc)
        {
            if (doc.ResponseExample is not { } resEx
                || resEx.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                return;

            var node = JsonNode.Parse(resEx.GetRawText());
            if (node is null)
                return;

            operation.Responses ??= new OpenApiResponses();

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

        private static void ApplyCallExamples(OpenApiOperation operation, ApiDoc doc, string culture)
        {
            if (doc.CallExamples is not { Count: > 0 })
                return;

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(operation.Description))
                sb.AppendLine(operation.Description).AppendLine();

            var examplesHeading = culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? "### 调用示例"
                : "### Examples";
            sb.AppendLine(examplesHeading);
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
