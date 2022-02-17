[cmdletBinding()]
Param(
    [Parameter()]
    [String[]]
    $ScriptsToSign,

    [Parameter()]
    [String]
    $TimeStampServer,

    [Parameter()]
    [String]
    $CertificatePath,

    [Parameter()]
    [String]
    $CertificatePassword,

    [Parameter()]
    [String]
    $CertificateAlgorithm
)

$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertificatePath, $CertificatePassword)

Set-AuthenticodeSignature -Filepath $ScriptsToSign -Cert $cert -TimeStampServer $TimeStampServer -IncludeChain NotRoot -HashAlgorithm $CertificateAlgorithm