# ----------------------------------------------------------------------------------
# Copyright Microsoft Corporation
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
# ----------------------------------------------------------------------------------

$ExceptionFileNames = @("AssemblyVersionConflict.csv", "BreakingChangeIssues.csv", "ExtraAssemblies.csv", "HelpIssues.csv", "MissingAssemblies.csv", "SignatureIssues.csv")

function main {
    param(
        [string]
        $PackageDirectory,
        [string]
        $ReportsDirectory,
        [string[]]
        $ModulesToAnalyze,
        [System.Boolean]
        $HelpOnly = $False,
        [System.Boolean]
        $SkipHelp = $False,
        [System.Boolean]
        $UseExceptions = $False,
        [string[]]
        [Validateset('HelpAnalyzer', 'BreakingChangeAnalyzer', 'DependencyAnalyzer', 'SignatureVerifier')]
        $Analyzor
    )

    Try {
        if (-not $PSBoundParameters.ContainsKey('PackageDirectory')) {
            $PackageDirectory = Resolve-Path -Path $(Join-Path -Path $PSScriptRoot -ChildPath '..\..\artifacts\Debug')
        }
        else {
            $PackageDirectory = Resolve-Path -Path $PackageDirectory
        }
        if (-not $PSBoundParameters.ContainsKey('ModulesToAnalyze')) {
            [System.Collections.Generic.List[string]]$ModulesToAnalyze = Get-ChildItem -Path $PackageDirectory | ForEach-Object {$_.BaseName}
        }
        if (-not $PSBoundParameters.ContainsKey('ReportsDirectory')) {
            $ReportsDirectory = Resolve-Path -Path $(Join-Path -Path $PackageDirectory -ChildPath '..\StaticAnalysisResults')
        }
        if (-not $PSBoundParameters.ContainsKey('Analyzor')) {
            $Analyzor = @('BreakingChangeAnalyzer', 'DependencyAnalyzer', 'SignatureVerifier')
        }
        Import-Module ./GetModuleMetadata.psm1 -Force
        Import-Module ./ModuleFilter.psm1 -Force

        [void][Reflection.Assembly]::LoadFrom($(Join-Path -Path $PSScriptRoot -ChildPath '..\Tools.Common\bin\Debug\netstandard2.0\Tools.Common.dll'))
        [void][Reflection.Assembly]::LoadFrom($(Join-Path -Path $PSScriptRoot -ChildPath '\obj\Debug\StaticAnalysis.Netcore.dll'))
        $ExceptionsDirectory = Resolve-Path -Path $(Join-Path -Path $PSScriptRoot -ChildPath "..\..\artifacts\StaticAnalysis\Exceptions")

        $AnalysisLogger = [Tools.Common.Loggers.AnalysisLogger]::New($ReportsDirectory, $ExceptionsDirectory)
        if ($helpOnly)
        {
            $Analyzers.Clear()
        }
        if (-not $skipHelp)
        {
            $Analyzers += 'HelpAnalyzer'
        }
        if ($Analyzers.Contains('HelpAnalyzer'))
        {
            ./HelpAnalyzer/HelpAnalyzer.ps1 $PackageDirectory $ModulesToAnalyze $AnalysisLogger
        }
        if ($Analyzers.Contains('BreakingChangeAnalyzer'))
        {
            ./BreakingChangeAnalyzer/BreakingChangeAnalyzer.ps1 $PackageDirectory $ModulesToAnalyze $AnalysisLogger
        }
        if ($Analyzers.Contains('DependencyAnalyzer'))
        {
            ./DependencyAnalyzer/DependencyAnalyzer.ps1 $PackageDirectory $ModulesToAnalyze $AnalysisLogger
        }
        if ($Analyzers.Contains('SignatureVerifier'))
        {
            ./SignatureVerifier/SignatureVerifier.ps1 $PackageDirectory $ModulesToAnalyze $AnalysisLogger
        }
        $AnalysisLogger.WriteReports()
        $AnalysisLogger.CheckForIssues(2)
    }
    Catch {
        throw
    }
    Finally {
        # foreach ($exceptionFileName in $ExceptionFileNames)
        # {
        #     $exceptionFilePath = Join-Path -Path $exceptionsDirectory -ChildPath $exceptionFileName
        #     if (Test-Path $exceptionFilePath)
        #     {
        #         rm $exceptionFilePath
        #     }
        # }
    }
}

function ConsolidateExceptionFiles {
    Param(
        [string]
        $exceptionsDirectory
    )

    foreach ($exceptionFileName in $ExceptionFileNames) {
        Write-Host "-----------------------------"
        $moduleExceptionFilePaths = [IO.Directory]::EnumerateFiles($exceptionsDirectory, $exceptionFileName, [System.IO.SearchOption]::AllDirectories) | Where-Object {[IO.Directory]::GetParent($_).Name.StartsWith("Az.")}
        
        $exceptionFilePath = Join-Path -Path $exceptionsDirectory -ChildPath $exceptionFileName
        if (Test-Path $exceptionFilePath)
        {
            rm $exceptionFilePath
        }

        [IO.File]::Create($exceptionFilePath).Close()
        $fileEmpty = $True
        foreach ($moduleExceptionFilePath in $moduleExceptionFilePaths)
        {
            [string[]]$content = [IO.File]::ReadAllLines($moduleExceptionFilePath)
            # [string[]]$content = Get-Content $moduleExceptionFilePath
            Write-Host $moduleExceptionFilePath
            Write-Host '======================================='
            Write-Host $content[0]
            if ($content.Length -gt 1)
            {
                if ($fileEmpty)
                {
                    # Write the header
                    [IO.File]::WriteAllLines($exceptionFilePath, $content[0])
                    $fileEmpty = $False;
                }

                # Write everything but the header
                $content = $content.Skip(1).ToArray()
                [IO.File]::AppendAllLines($exceptionFilePath, $content)
            }
        }
    }
}