Param(
    [string]
    $PackageDirectory,
    [string[]]
    $ModulesToAnalyze,
    [Tools.Common.Loggers.AnalysisLogger]
    $AnalysisLogger
)

$BreakingChangeAnalyzer = [StaticAnalysis.BreakingChangeAnalyzer.BreakingChangeAnalyzer]::New()
$BreakingChangeAnalyzer.Logger = $AnalysisLogger
$BreakingChangeAnalyzer.initLogger()
$ModulesInV4 = Get-ModulesInV4 $PackageDirectory $ModulesToAnalyze
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
        
        if ($Null -ne $ModulesInV4 -and $ModulesInV4.Contains($moduleToAnalyze))
        {
            $ModuleMetadata = Get-ModuleMetadata $ModuleInfo
            $BreakingChangeAnalyzer.AnalyzeModule($ModuleMetadata, $moduleToAnalyze)
        }
        else
        {
            foreach ($NestedModule in $ModuleInfo.NestedModules)
            {
                $ModuleMetadata = Get-ModuleMetadata $NestedModule
                $OldModuleMetadataFilePath = "C:\Users\yunwang\source\repos\azure-powershell-generation\artifacts\StaticAnalysis\SerializedCmdlets\$($NestedModule.Name).dll.json"
                Write-Host $OldModuleMetadataFilePath
                $BreakingChangeAnalyzer.AnalyzeModule($ModuleMetadata, $NestedModule.Name, $OldModuleMetadataFilePath)
            }
        }
    }
}