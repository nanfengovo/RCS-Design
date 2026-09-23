using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SIASUN.RCS.Swagger
{
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
            if (doc.RequestExample is { } reqEx
            && operation.RequestBody?.Content is not null
            && operation.RequestBody.Content.TryGetValue("application/json", out var reqMedia)
            && reqMedia is not null)
            {
                reqMedia.Example = JsonNode.Parse(reqEx.GetRawText());
            }
            if (doc.ResponseExample is { } resEx
                && operation.Responses is not null
                && operation.Responses.TryGetValue("200", out var ok)
                && ok?.Content is not null
                && ok.Content.TryGetValue("application/json", out var resMedia)
                && resMedia is not null)
            {
                resMedia.Example = JsonNode.Parse(resEx.GetRawText());
            }

        }
    }
}
