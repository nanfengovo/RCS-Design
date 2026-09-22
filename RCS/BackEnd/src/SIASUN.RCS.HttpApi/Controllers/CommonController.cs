using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SIASUN.RCS.Common;
using SIASUN.RCS.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace SIASUN.RCS.Controllers;

/// <summary>
/// 通用控制器--用来存放一些公用的接口
/// </summary>
[ApiExplorerSettings(GroupName = "common")]
public  class CommonController : AbpControllerBase
{

    private readonly AppVersionOptions _version;
    public CommonController(IOptions<AppVersionOptions> version)
    {
        LocalizationResource = typeof(RCSResource);
        _version = version.Value;
    }

    /// <summary>
    /// 获取版本相关的信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("version")]
    public AppVersionOptions Get() => _version;
}
