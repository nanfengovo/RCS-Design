# Swagger i18n

每个 `{culture}.json` 是一份完整外挂文档（`tags` + `operations`）。

## 增加语言

1. 复制 `zh-Hans.json` 为例如 `ja.json`
2. 翻译文案
3. 重启 Host

`SwaggerDocStore` 会扫描本目录下所有 `*.json`（忽略 `_` 前缀与 `meta.json`），Swagger UI 右上角语言按钮会自动出现新语言。

当前内置显示名：`zh-Hans` 简体中文、`zh-Hant` 繁體中文、`en` English；其它文化码直接显示文件名。

## 文档名约定

OpenAPI document = `{group}.{culture}`，例如 `abp.zh-Hans`、`rcs.en`。
