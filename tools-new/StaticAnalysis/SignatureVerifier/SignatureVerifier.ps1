Param(
    [string]
    $PackageDirectory,
    [string[]]
    $ModulesToAnalyze,
    [Tools.Common.Loggers.AnalysisLogger]
    $AnalysisLogger
)

$SignatureVerifier = [StaticAnalysis.SignatureVerifier.SignatureVerifier]::New()
$SignatureVerifier.Logger = $AnalysisLogger
$SignatureVerifier.initLogger()
foreach ($moduleToAnalyze in $modulesToAnalyze)
{
    foreach ($directory in $PackageDirectory)
    {
        $Psd1Path = Get-ChildItem -Name "$ModuleToAnalyze.psd1" -Path $directory -Recurse -Exclude obj,bin,export
        if ($Null -eq $Psd1Path)
        {
            continue
        }
        $Psd1Path = Join-Path -Path $directory -ChildPath $Psd1Path
        Import-Module $Psd1Path -Force

        $ModuleInfo = Get-Module $ModuleToAnalyze
        $ModuleMetadata = Get-ModuleMetadata $ModuleInfo
        $SignatureVerifier.AnalyzeModule($ModuleMetadata)
    }
}