using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using SIASUN.RCS.Common;
using SIASUN.RCS.EntityFrameworkCore;
using SIASUN.RCS.HealthChecks;
using SIASUN.RCS.MultiTenancy;
using SIASUN.RCS.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.Security.Claims;
using Volo.Abp.Studio;
using Volo.Abp.Studio.Client.AspNetCore;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;

namespace SIASUN.RCS;

[DependsOn(
    typeof(RCSHttpApiModule),
    typeof(AbpStudioClientAspNetCoreModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(RCSApplicationModule),
    typeof(RCSEntityFrameworkCoreModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreSerilogModule)
    )]
public class RCSHttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("RCS");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        var swaggerClientId = configuration["AuthServer:SwaggerClientId"];
        PreConfigure<OpenIddictServerBuilder>(builder =>
        {
            builder.AddEventHandler<OpenIddictServerEvents.ExtractTokenRequestContext>(b =>
            {
                b.UseInlineHandler(context =>
                {
                    var request = context.Request;
                    if (request is null || !string.IsNullOrEmpty(request.ClientId))
                    {
                        return default;
                    }

                    var redirectUri = request.RedirectUri;
                    if (string.IsNullOrEmpty(swaggerClientId) ||
                        redirectUri is null ||
                        !redirectUri.Contains("/swagger/oauth2-redirect.html", StringComparison.OrdinalIgnoreCase))
                    {
                        return default;
                    }

                    request.ClientId = swaggerClientId;
                    return default;
                });

                b.SetOrder(100_000);
            });
        });

        if (!hostingEnvironment.IsDevelopment())
        {
            PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
            {
                options.AddDevelopmentEncryptionAndSigningCertificate = false;
            });

            PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
            {
                serverBuilder.AddProductionEncryptionAndSigningCertificate("openiddict.pfx", configuration["AuthServer:CertificatePassPhrase"]!);
                serverBuilder.SetIssuer(new Uri(configuration["AuthServer:Authority"]!));
            });
        }
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<AppVersionOptions>(configuration.GetSection(AppVersionOptions.SectionName));
        Configure<AbpAntiForgeryOptions>(options =>
        {
            options.AutoValidate = false;
        });
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (!configuration.GetValue<bool>("App:DisablePII"))
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        }

        if (!configuration.GetValue<bool>("AuthServer:RequireHttpsMetadata"))
        {
            Configure<OpenIddictServerAspNetCoreOptions>(options =>
            {
                options.DisableTransportSecurityRequirement = true;
            });
            
            Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        if (hostingEnvironment.IsDevelopment())
        {
            context.Services.AddRazorPages()
                .AddRazorRuntimeCompilation();
        }

        ConfigureStudio(hostingEnvironment);
        ConfigureAuthentication(context);
        ConfigureUrls(configuration);
        ConfigureBundles(hostingEnvironment);
        ConfigureConventionalControllers();
        ConfigureHealthChecks(context);
        ConfigureSwagger(context, configuration);
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
    }

    private void ConfigureStudio(IHostEnvironment hostingEnvironment)
    {
        if (hostingEnvironment.IsProduction())
        {
            Configure<AbpStudioClientOptions>(options =>
            {
                options.IsLinkEnabled = false;
            });
        }
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());
        });
    }

    private void ConfigureBundles(IHostEnvironment hostingEnvironment)
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );

            options.ScriptBundles.Configure(
                LeptonXLiteThemeBundles.Scripts.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-scripts.js");
                    if (hostingEnvironment.IsDevelopment())
                    {
                        bundle.AddFiles("/dev-login-helper.js");
                    }
                }
            );
        });
    }


    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<RCSDomainSharedModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}SIASUN.RCS.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<RCSDomainModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}SIASUN.RCS.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<RCSApplicationContractsModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}SIASUN.RCS.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<RCSApplicationModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}SIASUN.RCS.Application"));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(RCSApplicationModule).Assembly);
        });
    }

    private static void ConfigureSwagger(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.Configure<SwaggerDocStoreOptions>(opt =>
        {
            opt.RelativeDirectory = Path.Combine("Swagger", "i18n");
            opt.DefaultCulture = "zh-Hans";
        });

        // 启动扫描 i18n；按 group.culture 注册文档，Swashbuckle 按文档名缓存（高性能）
        var docStore = new SwaggerDocStore(
            Microsoft.Extensions.Options.Options.Create(new SwaggerDocStoreOptions
            {
                RelativeDirectory = Path.Combine("Swagger", "i18n"),
                DefaultCulture = "zh-Hans"
            }));
        context.Services.AddSingleton<ISwaggerDocStore>(docStore);
        context.Services.AddTransient<SwaggerDocOperationFilter>();
        context.Services.AddTransient<SwaggerDocDocumentFilter>();

        var cultures = docStore.AvailableCultures.Count > 0
            ? docStore.AvailableCultures
            : (IReadOnlyList<string>)[docStore.DefaultCulture];

        context.Services.AddAbpSwaggerGenWithOidc(
            configuration["AuthServer:Authority"]!,
            ["RCS"],
            [AbpSwaggerOidcFlows.AuthorizationCode],
            null,
            options =>
            {
                options.OperationFilter<SwaggerDocOperationFilter>();
                options.DocumentFilter<SwaggerDocDocumentFilter>();

                foreach (var culture in cultures)
                {
                    options.SwaggerDoc(SwaggerDocNames.Format("abp", culture),
                        new OpenApiInfo { Title = "ABP API", Version = "v1", Description = $"ABP built-in APIs [{culture}]" });
                    options.SwaggerDoc(SwaggerDocNames.Format("rcs", culture),
                        new OpenApiInfo { Title = "RCS API", Version = "v1", Description = $"RCS business APIs [{culture}]" });
                    options.SwaggerDoc(SwaggerDocNames.Format("dashboard", culture),
                        new OpenApiInfo { Title = "Dashboard API", Version = "v1", Description = $"Dashboard APIs [{culture}]" });
                    options.SwaggerDoc(SwaggerDocNames.Format("common", culture),
                        new OpenApiInfo { Title = "Common API", Version = "v1", Description = $"Common APIs [{culture}]" });
                    options.SwaggerDoc(SwaggerDocNames.Format("all", culture),
                        new OpenApiInfo { Title = "All API", Version = "v1", Description = $"All APIs [{culture}]" });
                }

                options.DocInclusionPredicate((docName, description) =>
                {
                    if (!SwaggerDocNames.TryParse(docName, out var group, out _))
                        return false;

                    if (!description.TryGetMethodInfo(out var method))
                        return group == "all";

                    var asm = method.DeclaringType?.Assembly.GetName().Name ?? "";
                    var isAbp = asm.StartsWith("Volo.Abp", StringComparison.OrdinalIgnoreCase);
                    var apiGroup = description.GroupName;
                    return group switch
                    {
                        "abp" => isAbp,
                        "rcs" => apiGroup == "rcs",
                        "dashboard" => apiGroup == "dashboard",
                        "common" => apiGroup == "common",
                        "all" => true,
                        _ => false
                    };
                });
                options.CustomSchemaIds(type => type.FullName);

                var xmlPath = Path.Combine(AppContext.BaseDirectory, "SIASUN.RCS.HttpApi.xml");
                if (File.Exists(xmlPath))
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

                var appXml = Path.Combine(AppContext.BaseDirectory, "SIASUN.RCS.Application.xml");
                if (File.Exists(appXml))
                    options.IncludeXmlComments(appXml);
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]?
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.Trim().RemovePostFix("/"))
                            .ToArray() ?? Array.Empty<string>()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    private void ConfigureHealthChecks(ServiceConfigurationContext context)
    {
        context.Services.AddRCSHealthChecks();
    }


    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        app.UseForwardedHeaders();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseRouting();
        app.MapAbpStaticAssets();
        app.UseAbpStudioLink();
        app.UseAbpSecurityHeaders();
        app.UseCors();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }

        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        app.UseSwagger();

        app.UseAbpSwaggerUI(options =>
        {
            var swaggerDocStore = context.ServiceProvider.GetRequiredService<ISwaggerDocStore>();
            var uiCulture = swaggerDocStore.DefaultCulture;

            // UI 下拉只挂默认文化；语言切换由 rcs-swagger.js 动态改写为 group.culture
            options.SwaggerEndpoint($"/swagger/abp.{uiCulture}/swagger.json", "ABP API");
            options.SwaggerEndpoint($"/swagger/rcs.{uiCulture}/swagger.json", "RCS API");
            options.SwaggerEndpoint($"/swagger/dashboard.{uiCulture}/swagger.json", "Dashboard API");
            options.SwaggerEndpoint($"/swagger/all.{uiCulture}/swagger.json", "All API");
            options.SwaggerEndpoint($"/swagger/common.{uiCulture}/swagger.json", "Common API");

            options.InjectJavascript("/swagger-ui/rcs-swagger.js?v=2");
            options.InjectStylesheet("/swagger-ui/rcs-swagger.css?v=2");

            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            var swaggerClientId = configuration["AuthServer:SwaggerClientId"];
            options.OAuthClientId(swaggerClientId);
            options.OAuthScopes("RCS");
            options.OAuthUsePkce();
            // Swagger UI 的 OIDC 换 token 请求不会带上 client_id，OpenIddict 会因此返回 ID2029。
            options.UseRequestInterceptor(
                "(req) => { const url = req.url || ''; if (!url.includes('/connect/token')) return req; const id = '" + swaggerClientId + "'; if (req.body && typeof req.body.append === 'function') { req.body.append('client_id', id); return req; } if (typeof req.body === 'string' && req.body.indexOf('client_id=') < 0) { req.body += (req.body ? '&' : '') + 'client_id=' + encodeURIComponent(id); } return req; }");
        });
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
    }
}
