Param(
    [string]
    $PackageDirectory,
    [string[]]
    $ModulesToAnalyze,
    [Tools.Common.Loggers.AnalysisLogger]
    $AnalysisLogger
)

$HelpAnalyzer = [StaticAnalysis.HelpAnalyzer.HelpAnalyzer]::New()
$HelpAnalyzer.Logger = $AnalysisLogger
$HelpAnalyzer.initLogger()

$ModulesInV4 = Get-ModulesInV4 $PackageDirectory $ModulesToAnalyze

foreach ($moduleToAnalyze in $ModulesToAnalyze)
{
    foreach ($directory in $PackageDirectory)
    {
        $Psd1Path = Get-ChildItem -Name "$ModuleToAnalyze.psd1" -Path $directory -Recurse -Exclude obj,bin,export
        if ($Null -eq $Psd1Path)
        {
            continue
        }

        $Psd1Path = Join-Path -Path $directory -ChildPath $Psd1Path
        Write-Host $Psd1Path
        Import-Module $Psd1Path -Force

        if ($Null -ne $ModulesInV4 -and $ModulesInV4.Contains($moduleToAnalyze))
        {
            [System.Collections.Generic.List[string]]$HelpFiles = Get-ChildItem -Recurse -Name "*.md" -Path "$directory\$($moduleToAnalyze -replace 'Az.', '')\docs" | ForEach-Object {$_.split('\')[-1] -replace '.md', ''}
        }
        else
        {
            [System.Collections.Generic.List[string]]$HelpFiles = Get-ChildItem -Recurse -Name "*.md" -Path "$directory\$moduleToAnalyze\help" | ForEach-Object {$_.split('\')[-1] -replace '\.md', ''}
        }
        if ($Null -eq $HelpFiles -or $HelpFiles.Length -eq 0)
        {
            Write-Host $HelpFiles
            $Issue = [StaticAnalysis.HelpAnalyzer.HelpIssue]::New()
            $Issue.Assembly = $moduleToAnalyze
            $Issue.Description = "$moduleToAnalyze has no matching help folder"
            $Issue.Severity = 0
            $Issue.Remediation = "Make sure a help folder for $moduleToAnalyze exists and it is being copied to the output directory."
            $Issue.Target = $moduleToAnalyze
            $Issue.HelpFile = "$moduleToAnalyze/folder"
            $Issue.ProblemId = [StaticAnalysis.HelpAnalyzer.HelpAnalyzer]::MissingHelpFile
            $HelpAnalyzer.helpLogger.LogRecord($Issue)
        }
        $ModuleInfo = Get-Module $ModulesToAnalyze

        foreach ($NestedModule in $ModuleInfo.NestedModules)
        {
            $ModuleMetadata = Get-ModuleMetadata $NestedModule
            $HelpAnalyzer.ValidateHelpRecords($ModuleMetadata.Cmdlets, $HelpFiles)
            
        }
    }
}