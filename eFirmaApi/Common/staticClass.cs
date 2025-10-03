namespace eFirmaApi.Common
{
    public class staticClass
    {
        /*public static string getApiName()
        {
            return AppSettings.Instance.Get<string>("AppSettings:ApiName");
        }*/
        public static string getURIGenerarPKCS7()
        {
            return AppSettings.Instance.Get<string>("PJEO:urlFirmaPKCS7");
        }
        public static string getURIGenerarEvidencia()
        {
            return AppSettings.Instance.Get<string>("PJEO:urlFirmaEvidencia");
        }
    }
}
