using System.Collections.Generic;

namespace SIASUN.RCS.Swagger
{
    /// <summary>
    /// 多语言 Swagger 外挂文档存储：按文化加载 JSON，O(1) 查询。
    /// </summary>
    public interface ISwaggerDocStore
    {
        /// <summary>默认文化（无匹配时回退）。</summary>
        string DefaultCulture { get; }

        /// <summary>已加载的文化列表（文件名，如 zh-Hans）。</summary>
        IReadOnlyList<string> AvailableCultures { get; }

        /// <summary>规范化并解析到已加载文化；未知则回退默认。</summary>
        string ResolveCulture(string? culture);

        bool TryGetOperation(string culture, string? method, string? path, out ApiDoc? doc);

        bool TryGetTag(string culture, string tagName, out TagDoc? doc);

        IReadOnlyDictionary<string, TagDoc> GetTags(string culture);
    }
}
