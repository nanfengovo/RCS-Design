using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace SIASUN.RCS.Swagger
{
    public class SwaggerDocStoreOptions
    {
        /// <summary>相对 ContentRoot/BaseDirectory 的 i18n 目录。</summary>
        public string RelativeDirectory { get; set; } = Path.Combine("Swagger", "i18n");

        public string DefaultCulture { get; set; } = "zh-Hans";
    }

    /// <summary>
    /// 启动时扫描 Swagger/i18n/*.json，按文件名（文化）缓存到内存。
    /// 新增语言：丢一个 {culture}.json 进去即可。
    /// </summary>
    public sealed class SwaggerDocStore : ISwaggerDocStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Dictionary<string, SwaggerModel> _byCulture;
        private readonly string _defaultCulture;
        private readonly IReadOnlyList<string> _cultures;

        public SwaggerDocStore(
            IOptions<SwaggerDocStoreOptions>? options = null,
            ILogger<SwaggerDocStore>? logger = null)
        {
            var opt = options?.Value ?? new SwaggerDocStoreOptions();
            var log = logger ?? NullLogger<SwaggerDocStore>.Instance;
            _defaultCulture = string.IsNullOrWhiteSpace(opt.DefaultCulture)
                ? "zh-Hans"
                : opt.DefaultCulture.Trim();

            _byCulture = LoadAll(opt.RelativeDirectory, log);
            if (_byCulture.Count == 0)
            {
                _byCulture[_defaultCulture] = EmptyModel();
                log.LogWarning("Swagger i18n: no JSON found under {Dir}, using empty default {Culture}",
                    opt.RelativeDirectory, _defaultCulture);
            }
            else if (!_byCulture.ContainsKey(_defaultCulture))
            {
                // 默认文化文件缺失时，用排序后的第一个，保证始终能解析
                _defaultCulture = _byCulture.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).First();
            }

            _cultures = _byCulture.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public string DefaultCulture => _defaultCulture;

        public IReadOnlyList<string> AvailableCultures => _cultures;

        public string ResolveCulture(string? culture)
        {
            if (string.IsNullOrWhiteSpace(culture))
                return _defaultCulture;

            var c = culture.Trim();
            if (_byCulture.ContainsKey(c))
                return c;

            // en-US -> en
            var dash = c.IndexOf('-');
            if (dash > 0)
            {
                var prefix = c[..dash];
                if (_byCulture.ContainsKey(prefix))
                    return prefix;
            }

            // 大小写不敏感匹配
            foreach (var key in _byCulture.Keys)
            {
                if (key.Equals(c, StringComparison.OrdinalIgnoreCase))
                    return key;
            }

            return _defaultCulture;
        }

        public bool TryGetOperation(string culture, string? method, string? path, out ApiDoc? doc)
        {
            doc = null;
            if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(path))
                return false;

            var model = GetModel(culture);
            if (model.Operations is null || model.Operations.Count == 0)
                return false;

            return model.Operations.TryGetValue(NormalizeKey(method, path), out doc);
        }

        public bool TryGetTag(string culture, string tagName, out TagDoc? doc)
        {
            doc = null;
            if (string.IsNullOrWhiteSpace(tagName))
                return false;

            var tags = GetModel(culture).Tags;
            if (tags is null)
                return false;

            return tags.TryGetValue(tagName, out doc);
        }

        public IReadOnlyDictionary<string, TagDoc> GetTags(string culture)
        {
            return GetModel(culture).Tags ?? new Dictionary<string, TagDoc>();
        }

        private SwaggerModel GetModel(string culture)
        {
            var resolved = ResolveCulture(culture);
            return _byCulture[resolved];
        }

        private static Dictionary<string, SwaggerModel> LoadAll(string relativeDirectory, ILogger log)
        {
            var result = new Dictionary<string, SwaggerModel>(StringComparer.OrdinalIgnoreCase);
            var dir = ResolveDirectory(relativeDirectory);
            if (!Directory.Exists(dir))
            {
                log.LogWarning("Swagger i18n directory not found: {Dir}", dir);
                return result;
            }

            foreach (var file in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
            {
                var culture = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(culture) ||
                    culture.StartsWith('_') ||
                    culture.Equals("meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var json = File.ReadAllText(file, Encoding.UTF8);
                    var model = JsonSerializer.Deserialize<SwaggerModel>(json, JsonOptions) ?? EmptyModel();
                    model.Tags ??= new Dictionary<string, TagDoc>();
                    model.Operations ??= new Dictionary<string, ApiDoc>();
                    result[culture] = model;
                    log.LogInformation("Swagger i18n loaded {Culture} ({Ops} ops) from {File}",
                        culture, model.Operations.Count, file);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Swagger i18n failed to load {File}", file);
                }
            }

            return result;
        }

        private static string ResolveDirectory(string relativeDirectory)
        {
            if (Path.IsPathRooted(relativeDirectory))
                return relativeDirectory;

            var baseDir = AppContext.BaseDirectory;
            var candidate = Path.Combine(baseDir, relativeDirectory);
            if (Directory.Exists(candidate))
                return candidate;

            // 开发时偶发从项目目录跑
            return Path.Combine(Directory.GetCurrentDirectory(), relativeDirectory);
        }

        private static SwaggerModel EmptyModel() => new()
        {
            Tags = new Dictionary<string, TagDoc>(),
            Operations = new Dictionary<string, ApiDoc>()
        };

        public static string NormalizeKey(string httpMethod, string relativePath)
        {
            var path = relativePath.Split('?', 2)[0].Trim().TrimEnd('/');
            if (!path.StartsWith('/'))
                path = "/" + path;
            return $"{httpMethod.Trim().ToUpperInvariant()} {path}";
        }
    }
}
