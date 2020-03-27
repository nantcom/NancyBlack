namespace NantCom.NancyBlack
{

    public sealed partial class ContextItems
    {
        public const string SubSite = "SubSite";
        public const string SiteDatabase = "SiteDatabase";
        public const string CurrentSite = "CurrentSite";
        public const string SiteSettings = "SiteSettings";
        public const string RootPath = "RootPath";
    }

    /// <summary>
    /// List of Built-in Cookies
    /// </summary>
    public sealed class BuiltInCookies
    {
        /// <summary>
        /// GUID of User, automatically generated and valid for 1 day after last user request
        /// </summary>
        public const string UserId = "userid";
    }
}