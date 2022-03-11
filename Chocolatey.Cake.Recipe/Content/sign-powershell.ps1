[cmdletBinding()]
Param(
    [Parameter()]
    [String[]]
    $ScriptsToSign,

    [Parameter()]
    [String]
    $TimeStampServer,

    [Parameter(ParameterSetName="File")]
    [String]
    $CertificatePath,

    [Parameter(ParameterSetName="File")]
    [String]
    $CertificatePassword,

    [Parameter()]
    [String]
    $CertificateAlgorithm,

    [Parameter(ParameterSetName="Store")]
    [String]
    $CertificateSubjectName
)

$cert = if ($PSCmdlet.ParameterSetName -eq "File") {
    New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertificatePath, $CertificatePassword)
} else {
    Get-ChildItem Cert:\LocalMachine\My | Where-Object Subject -like "*$CertificateSubjectName*"
}

Set-AuthenticodeSignature -Filepath $ScriptsToSign -Cert $cert -TimeStampServer $TimeStampServer -IncludeChain NotRoot -HashAlgorithm $CertificateAlgorithm