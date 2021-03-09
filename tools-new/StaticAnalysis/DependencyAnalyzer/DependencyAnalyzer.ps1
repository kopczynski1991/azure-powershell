Param(
    [string]
    $PackageDirectory,
    [string[]]
    $ModulesToAnalyze,
    [Tools.Common.Loggers.AnalysisLogger]
    $AnalysisLogger
)
[System.Collections.Generic.List[string]]$ModulesInV3 = Get-ModulesInV3 $PackageDirectory $ModulesToAnalyze
Write-Host $PackageDirectory
$Analyzor = [StaticAnalysis.DependencyAnalyzer.DependencyAnalyzer]::New()
$Analyzor.Logger = $AnalysisLogger
$Analyzor.Analyze([System.Collections.Generic.List[string]]@($PackageDirectory), $ModulesInV3)