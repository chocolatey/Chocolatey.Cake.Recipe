# Copyright © 2022 Chocolatey Software, Inc.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
#
# You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

[cmdletBinding()]
Param(
    [Parameter()]
    [String[]]
    $ScriptsToSign,

    [Parameter()]
    [String]
    $OutputFolder,

    [Parameter()]
    [String]
    $TimeStampServer,

    [Parameter(ParameterSetName = "File")]
    [String]
    $CertificatePath,

    [Parameter(ParameterSetName = "File")]
    [String]
    $CertificatePassword,

    [Parameter()]
    [String]
    $CertificateAlgorithm,

    [Parameter(ParameterSetName = "Store")]
    [String]
    $CertificateSubjectName
)

$Cert = if ($PSCmdlet.ParameterSetName -eq "File") {
    New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertificatePath, $CertificatePassword)
}
else {
    Get-ChildItem Cert:\LocalMachine\My | Where-Object {$_.Subject -Like "*$CertificateSubjectName*" -and
                                                                $_.Issuer -match 'DigiCert' -and
                                                                $_.NotAfter -ge [datetime]::Now}
}

if ($Cert) {
    $CommonSignParams = @{
        'TimestampServer' = $TimeStampServer
        'IncludeChain'    = 'NotRoot'
        'HashAlgorithm'   = $CertificateAlgorithm
        'Cert'            = $Cert
    }

    foreach ($Script in $ScriptsToSign) {
        $ExistingSig = Get-AuthenticodeSignature -FilePath $Script

        if ($ExistingSig.Status -ne 'Valid' -or $ExistingSig.SignerCertificate.Issuer -notmatch 'DigiCert' -or $ExistingSig.SignerCertificate.NotAfter -lt [datetime]::Now) {
            $NewSig = Set-AuthenticodeSignature -FilePath $Script @CommonSignParams
            Write-Host "Script file '$Script' signed with status: $($NewSig.Status)"

            if (!(Test-Path -Path $OutputFolder)) {
                $null = New-Item -Path $OutputFolder -Type Directory
            }
            Copy-Item -Path $Script -Destination $OutputFolder
        } else {
            Write-Host "Script file '$Script' does not need signing, current signature is valid."
        }
    }
} else {
    Write-Warning "Skipping script signing, no currently valid DigiCert issued Authenticode signing certificate matching '$($CertificateSubjectName)' was found."
}