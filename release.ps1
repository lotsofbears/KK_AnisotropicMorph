$dir = $PSScriptRoot + "\"
$out = $dir + "\out"
Remove-Item -Force -Path ($out) -Recurse -ErrorAction SilentlyContinue

# KK -------------------------------------
Write-Output ("Creating KK release")

New-Item -ItemType Directory -Force -Path ($out + "\BepInEx\plugins") | Out-Null

Copy-Item -Path ($dir + "\bin\KK\*") -Destination ($out + "\BepInEx\plugins") -ErrorAction Stop -Force | Out-Null
# Copy-Item copies empty directories and I don't see any way to tell it to only copy files
Remove-Item -Path ($out + "\BepInEx\plugins\KK_AnisotropicMorph.pdb") -Force
Remove-Item -Path ($out + "\BepInEx\plugins\README.md") -Force

$ver = "v" + (Get-ChildItem -Path ($dir + "\bin\KK\KK_AnisotropicMorph.dll") -Force -ErrorAction Stop)[0].VersionInfo.FileVersion.ToString() -replace "([\d+\.]+?\d+)[\.0]*$", '${1}'
Write-Output ("Version " + $ver)
Compress-Archive -Path ($out + "\*") -Force -CompressionLevel "Optimal" -DestinationPath ($dir +"bin\KK_AnisotropicMorph_" + $ver + ".zip")

Remove-Item -Force -Path ($out) -Recurse -ErrorAction SilentlyContinue


# KKS ------------------------------------
Write-Output ("Creating KKS release")

New-Item -ItemType Directory -Force -Path ($out + "\BepInEx\plugins") | Out-Null

Copy-Item -Path ($dir + "\bin\KKS\*") -Destination ($out + "\BepInEx\plugins") -ErrorAction Stop -Force | Out-Null
# Copy-Item copies empty directories and I don't see any way to tell it to only copy files
Remove-Item -Path ($out + "\BepInEx\plugins\KKS_AnisotropicMorph.pdb") -Force
Remove-Item -Path ($out + "\BepInEx\plugins\README.md") -Force

$ver = "v" + (Get-ChildItem -Path ($dir + "\bin\KKS\KKS_AnisotropicMorph.dll") -Force -ErrorAction Stop)[0].VersionInfo.FileVersion.ToString() -replace "([\d+\.]+?\d+)[\.0]*$", '${1}'
Write-Output ("Version " + $ver)
Compress-Archive -Path ($out + "\*") -Force -CompressionLevel "Optimal" -DestinationPath ($dir +"bin\KKS_AnisotropicMorph_" + $ver + ".zip")

Remove-Item -Force -Path ($out) -Recurse -ErrorAction SilentlyContinue