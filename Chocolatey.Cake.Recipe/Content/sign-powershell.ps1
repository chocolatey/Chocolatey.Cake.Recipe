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

$cert = if ($PSCmdlet.ParameterSetName -eq "File") {
    New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertificatePath, $CertificatePassword)
}
else {
    Get-ChildItem Cert:\LocalMachine\My | Where-Object Subject -Like "*$CertificateSubjectName*"
}

Set-AuthenticodeSignature -FilePath $ScriptsToSign -Cert $cert -TimestampServer $TimeStampServer -IncludeChain NotRoot -HashAlgorithm $CertificateAlgorithm