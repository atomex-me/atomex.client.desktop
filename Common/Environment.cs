using System;
using System.ComponentModel;

namespace Atomex.Client.Desktop.Common
{
    public static class Env
    {
        public static bool IsInDesignerMode()
        {
            // return LicenseManager.UsageMode == LicenseUsageMode.Designtime;
            return Environment.GetEnvironmentVariable("IS_DESIGNER") != null;
        }
    }
}