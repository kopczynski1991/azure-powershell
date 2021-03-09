// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using Tools.Common.Helpers;
using Tools.Common.Issues;
using Tools.Common.Loaders;
using Tools.Common.Loggers;
using Tools.Common.Models;

namespace StaticAnalysis.HelpAnalyzer
{
    /// <summary>
    /// Static analyzer for PowerShell Help
    /// </summary>
    public class HelpAnalyzer
    {
        public const int MissingHelp = 6050;
        public const int MissingHelpFile = 6000;
        public ReportLogger<HelpIssue> helpLogger { get; private set; }
        public HelpAnalyzer()
        {
            Name = "Help Analyzer";
        }
        public AnalysisLogger Logger { get; set; }
        public string Name { get; private set; }

        public void initLogger()
        {
            helpLogger = Logger.CreateLogger<HelpIssue>("HelpIssues.csv");
        }

        public void ValidateHelpRecords(IList<CmdletMetadata> cmdlets, IList<string> helpRecords)
        {
            foreach (var cmdlet in cmdlets)
            {
                if (!helpRecords.Contains(cmdlet.Name, StringComparer.OrdinalIgnoreCase))
                {
                    helpLogger.LogRecord(new HelpIssue
                    {
                        Target = cmdlet.ClassName,
                        Severity = 1,
                        ProblemId = MissingHelp,
                        Description = string.Format("Help missing for cmdlet {0} implemented by class {1}",
                        cmdlet.Name, cmdlet.ClassName),
                        Remediation = string.Format("Add Help record for cmdlet {0} to help file.", cmdlet.Name)
                    });
                }
            }
        }
    }
}
