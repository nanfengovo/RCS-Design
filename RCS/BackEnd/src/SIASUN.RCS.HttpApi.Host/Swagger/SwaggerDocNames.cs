using System;

namespace SIASUN.RCS.Swagger
{
    /// <summary>
    /// Swagger 文档名约定：{group}.{culture}，例如 abp.zh-Hans。
    /// group 为 abp/rcs/dashboard/common/all；culture 可为 zh-Hans（含连字符）。
    /// </summary>
    public static class SwaggerDocNames
    {
        public static readonly string[] Groups = ["abp", "rcs", "dashboard", "common", "all"];

        public static string Format(string group, string culture) => $"{group}.{culture}";

        public static bool TryParse(string? documentName, out string group, out string culture)
        {
            group = "";
            culture = "";
            if (string.IsNullOrWhiteSpace(documentName))
                return false;

            foreach (var g in Groups)
            {
                if (documentName.Equals(g, StringComparison.OrdinalIgnoreCase))
                {
                    group = g;
                    return true;
                }

                var prefix = g + ".";
                if (documentName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    group = g;
                    culture = documentName[prefix.Length..];
                    return !string.IsNullOrWhiteSpace(culture);
                }
            }

            return false;
        }

        public static string GetGroup(string? documentName)
        {
            return TryParse(documentName, out var group, out _) ? group : documentName ?? "";
        }

        public static string? GetCulture(string? documentName)
        {
            return TryParse(documentName, out _, out var culture) ? culture : null;
        }
    }
}
