using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SIASUN.RCS.Swagger
{
    /// <summary>
    /// 把 SwaggerDoc.json 里的 tags 说明贴到 OpenAPI Document.Tags 上（Swagger UI 分组标题注释）。
    /// </summary>
    public class SwaggerDocDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var tagDocs = SwaggerDocStore.Instance.Tags;
            if (tagDocs is null || tagDocs.Count == 0)
                return;

            swaggerDoc.Tags ??= new HashSet<OpenApiTag>();

            // 文档里已出现的 tag 名（来自各 Operation）
            var used = new HashSet<string>(StringComparer.Ordinal);
            if (swaggerDoc.Paths is not null)
            {
                foreach (var path in swaggerDoc.Paths.Values)
                {
                    if (path.Operations is null)
                        continue;
                    foreach (var op in path.Operations.Values)
                    {
                        if (op.Tags is null)
                            continue;
                        foreach (var t in op.Tags)
                        {
                            if (!string.IsNullOrWhiteSpace(t.Name))
                                used.Add(t.Name);
                        }
                    }
                }
            }

            foreach (var kv in tagDocs)
            {
                var tagName = kv.Key;
                var doc = kv.Value;
                if (string.IsNullOrWhiteSpace(tagName) || doc is null)
                    continue;

                // 本 OpenAPI 文档未用到的 Tag 不注入，避免污染其它 Swagger 分组（rcs/dashboard）
                if (used.Count > 0 && !used.Contains(tagName))
                    continue;

                var description = doc.Description;
                if (string.IsNullOrWhiteSpace(description))
                    continue;

                var existing = swaggerDoc.Tags.FirstOrDefault(t =>
                    string.Equals(t.Name, tagName, StringComparison.Ordinal));

                if (existing is not null)
                {
                    existing.Description = description;
                    if (!string.IsNullOrWhiteSpace(doc.Name))
                        existing.Name = tagName; // Name 仍用原始 Tag，便于与 Operation 关联
                }
                else
                {
                    swaggerDoc.Tags.Add(new OpenApiTag
                    {
                        Name = tagName,
                        Description = description
                    });
                }
            }
        }
    }
}
