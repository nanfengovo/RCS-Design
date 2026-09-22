using System;
using System.Collections.Generic;
using System.Text;

namespace SIASUN.RCS.Common
{
    public class AppVersionOptions
    {
        public const string SectionName = "Version"; // 或根对象直接绑定
        public string BackEndVersion { get; set; } = "0.0.0";

        public string FrontEndVersion { get; set; } = "0.0.0";
        public string BackEndBuild { get; set; } = "";

        public string FrontEndBuild { get; set; } = "";

        public string Channel { get; set; } = "dev";
    }
}
