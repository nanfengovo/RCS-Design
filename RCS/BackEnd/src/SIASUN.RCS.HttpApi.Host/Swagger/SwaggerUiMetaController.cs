using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace SIASUN.RCS.Swagger
{
    /// <summary>
    /// 给 Swagger UI 工具条提供可用文化 / 默认文化（加 JSON 文件后自动出现）。
    /// </summary>
    [AllowAnonymous]
    [Route("swagger")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SwaggerUiMetaController : AbpController
    {
        private static readonly Dictionary<string, string> DisplayNames = new()
        {
            ["zh-Hans"] = "简体中文",
            ["zh-Hant"] = "繁體中文",
            ["en"] = "English",
            ["ja"] = "日本語",
            ["ko"] = "한국어"
        };

        private readonly ISwaggerDocStore _store;

        public SwaggerUiMetaController(ISwaggerDocStore store)
        {
            _store = store;
        }

        [HttpGet("rcs-meta.json")]
        public SwaggerUiMetaDto Get()
        {
            return new SwaggerUiMetaDto
            {
                DefaultCulture = _store.DefaultCulture,
                Cultures = _store.AvailableCultures.Select(id => new SwaggerUiCultureDto
                {
                    Id = id,
                    Label = DisplayNames.TryGetValue(id, out var label) ? label : id
                }).ToList(),
                Groups =
                [
                    new SwaggerUiGroupDto { Id = "abp", Name = "ABP API" },
                    new SwaggerUiGroupDto { Id = "rcs", Name = "RCS API" },
                    new SwaggerUiGroupDto { Id = "dashboard", Name = "Dashboard API" },
                    new SwaggerUiGroupDto { Id = "common", Name = "Common API" },
                    new SwaggerUiGroupDto { Id = "all", Name = "All API" }
                ]
            };
        }
    }

    public class SwaggerUiMetaDto
    {
        [JsonPropertyName("defaultCulture")]
        public string DefaultCulture { get; set; } = "zh-Hans";

        [JsonPropertyName("cultures")]
        public List<SwaggerUiCultureDto> Cultures { get; set; } = [];

        [JsonPropertyName("groups")]
        public List<SwaggerUiGroupDto> Groups { get; set; } = [];
    }

    public class SwaggerUiCultureDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("label")]
        public string Label { get; set; } = "";
    }

    public class SwaggerUiGroupDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }
}
