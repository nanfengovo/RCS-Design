using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SIASUN.RCS.Swagger
{
    /// <summary>
    /// 按文档文化把 Tag 说明贴到 OpenAPI Document.Tags。
    /// </summary>
    public class SwaggerDocDocumentFilter : IDocumentFilter
    {
        private readonly ISwaggerDocStore _store;

        public SwaggerDocDocumentFilter(ISwaggerDocStore store)
        {
            _store = store;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var culture = SwaggerDocNames.GetCulture(context.DocumentName) ?? _store.DefaultCulture;
            culture = _store.ResolveCulture(culture);

            var tagDocs = _store.GetTags(culture);
            if (tagDocs.Count == 0)
                return;

            swaggerDoc.Tags ??= new HashSet<OpenApiTag>();

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
