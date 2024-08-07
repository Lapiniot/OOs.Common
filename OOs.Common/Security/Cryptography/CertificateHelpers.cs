using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#nullable enable

namespace OOs.Security.Cryptography;

public static class CertificateHelpers
{
    public static X509Certificate2 CreateSelfSignedCertificate(X500DistinguishedName subjectName, X509Extension subjectAlternateNames,
        DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var rsa = RSA.Create(4096);
        var request = CreateSelfSignedCertificateRequest(rsa, subjectName, subjectAlternateNames);
        return request.CreateSelfSigned(notBefore, notAfter);
    }

    public static CertificateRequest CreateSelfSignedCertificateRequest(RSA key, X500DistinguishedName subjectName, X509Extension subjectAlternateNames)
    {
        var request = new CertificateRequest(subjectName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.NonRepudiation | X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication")], true)); // extendedKeyUsage = serverAuth
        request.CertificateExtensions.Add(subjectAlternateNames);
        var subjectKeyIdentifier = new X509SubjectKeyIdentifierExtension(request.PublicKey, false);
        request.CertificateExtensions.Add(subjectKeyIdentifier);
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(subjectKeyIdentifier));
        return request;
    }

    public static X500DistinguishedName BuildSubjectNameExtension(string commonName,
        string? organization = null, string? organizationalUnit = null,
        string? countryOrRegion = null, string? stateOrProvince = null,
        string? emailAddress = null)
    {
        var subjBuilder = new X500DistinguishedNameBuilder();
        subjBuilder.AddCommonName(commonName);

        if (!string.IsNullOrWhiteSpace(countryOrRegion))
            subjBuilder.AddCountryOrRegion(countryOrRegion);
        if (!string.IsNullOrWhiteSpace(stateOrProvince))
            subjBuilder.AddStateOrProvinceName(stateOrProvince);
        if (!string.IsNullOrEmpty(organization))
            subjBuilder.AddOrganizationName(organization);
        if (!string.IsNullOrWhiteSpace(organizationalUnit))
            subjBuilder.AddOrganizationalUnitName(organizationalUnit);
        if (!string.IsNullOrWhiteSpace(emailAddress))
            subjBuilder.AddEmailAddress(emailAddress);

        return subjBuilder.Build();
    }

    public static X509Extension BuildSubjectAlternateNamesExtension(IEnumerable<string> dnsNames, IEnumerable<IPAddress> iPAddresses, bool critical = false)
    {
        ArgumentNullException.ThrowIfNull(dnsNames);
        ArgumentNullException.ThrowIfNull(iPAddresses);

        var sanBuilder = new SubjectAlternativeNameBuilder();

        foreach (var dnsName in dnsNames)
        {
            sanBuilder.AddDnsName(dnsName);
        }

        foreach (var ipAddress in iPAddresses)
        {
            sanBuilder.AddIpAddress(ipAddress);
        }

        return sanBuilder.Build(critical);
    }

    public static byte[] GenerateSelfSignedCertificate(string commonName, string organization,
        string organizationalUnit, DateTimeOffset notBefore, DateTimeOffset notAfter,
        IEnumerable<string> dnsNames, IEnumerable<IPAddress> ipAddresses,
        X509ContentType contentType = X509ContentType.Pfx)
    {
        using var certificate = CreateSelfSignedCertificate(
            BuildSubjectNameExtension(commonName, organization, organizationalUnit: organizationalUnit),
            BuildSubjectAlternateNamesExtension(dnsNames, ipAddresses), notBefore, notAfter);
        return certificate.Export(contentType, "");
    }
}