namespace eFirmaApi.Common
{
    public enum CerticateType : byte
    {
        Unknow = 0,
        Fiel = 1,
        Firel = 2,
        Multiple = 3
    }
    public enum CertificateStatus : byte
    {
        Valid,
        Revocated,
        Unknowed
    }
}
