# Copyright © 2024 Chocolatey Software, Inc.
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
    $ScriptsToVerify
)

$AllScriptsVerified = $true

Write-Output ""
Write-Output "========== Verifying PowerShell Scripts =========="
Write-Output ""

foreach ($ScriptToVerify in $ScriptsToVerify) {
    $ExistingSig = Get-AuthenticodeSignature -FilePath $ScriptToVerify

    if ($ExistingSig.Status -ne 'Valid' -or $ExistingSig.SignerCertificate.Issuer -notmatch 'DigiCert' -or $ExistingSig.SignerCertificate.NotAfter -lt [datetime]::Now) {
        $AllScriptsVerified = $false
        Write-Output "Script file '$ScriptToVerify' contains an invalid signature, which must be corrected before build can succeed."
    } else {
        Write-Output "Script file '$ScriptToVerify' does not need signing, current signature is valid."
    }
}

Write-Output ""
Write-Output "========== Verification Complete =========="
Write-Output ""

if ($AllScriptsVerified -eq $false) {
    throw "At least one PowerShell script had an invalid signature. Check output for details."
}