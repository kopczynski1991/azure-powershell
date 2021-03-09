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

//#define SERIALIZE

using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using Tools.Common.Loggers;
using Tools.Common.Models;

namespace StaticAnalysis.BreakingChangeAnalyzer
{
    public class BreakingChangeAnalyzer
    {
        public AnalysisLogger Logger { get; set; }
        public string Name { get; set; }
        public string BreakingChangeIssueReportLoggerName { get; set; }
        public ReportLogger<BreakingChangeIssue> issueLogger { get; private set; }

        // TODO: Remove IfDef code
#if !NETSTANDARD
        private AppDomain _appDomain;
#endif

        public BreakingChangeAnalyzer()
        {
            Name = "Breaking Change Analyzer";
            BreakingChangeIssueReportLoggerName = "BreakingChangeIssues.csv";
        }

        public void initLogger()
        {
            issueLogger = Logger.CreateLogger<BreakingChangeIssue>("BreakingChangeIssues.csv");
        }

        public void AnalyzeModule(ModuleMetadata newModuleMetadata, String assemblyFileName, String oldModuleMetadataFilePath)
        {
            if (!File.Exists(oldModuleMetadataFilePath))
            {
                return;
            }

            var oldModuleMetadata = DeserializeCmdlets(oldModuleMetadataFilePath);

            var output = string.Format("Before filter\nOld module cmdlet count: {0}\nNew module cmdlet count: {1}",
                oldModuleMetadata.Cmdlets.Count, newModuleMetadata.Cmdlets.Count);

            output += string.Format("\nCmdlet file: {0}", assemblyFileName);

            output += string.Format("After filter\nOld module cmdlet count: {0}\nNew module cmdlet count: {1}",
                oldModuleMetadata.Cmdlets.Count, newModuleMetadata.Cmdlets.Count);

            foreach (var cmdlet in oldModuleMetadata.Cmdlets)
            {
                output += string.Format("\n\tOld cmdlet - {0}", cmdlet.Name);
            }

            foreach (var cmdlet in newModuleMetadata.Cmdlets)
            {
                output += string.Format("\n\tNew cmdlet - {0}", cmdlet.Name);
            }

            issueLogger.WriteMessage(output + Environment.NewLine);

            RunBreakingChangeChecks(oldModuleMetadata, newModuleMetadata, issueLogger);

        }

        /// <summary>
        /// Deserialize the cmdlets to compare them to the changed modules
        /// </summary>
        /// <param name="fileName">Name of the file we are to deserialize the cmdlets from.</param>
        /// <returns></returns>
        private static ModuleMetadata DeserializeCmdlets(string fileName)
        {
           return JsonConvert.DeserializeObject<ModuleMetadata>(File.ReadAllText(fileName));
        }

        /// <summary>
        /// Run all of the different breaking change checks that we have for the tool
        /// </summary>
        /// <param name="oldModuleMetadata">Information about the module from the old (serialized) assembly.</param>
        /// <param name="newModuleMetadata">Information about the module from the new assembly.</param>
        /// <param name="issueLogger">ReportLogger that will keep track of issues found.</param>
        private void RunBreakingChangeChecks(
            ModuleMetadata oldModuleMetadata,
            ModuleMetadata newModuleMetadata,
            ReportLogger<BreakingChangeIssue> issueLogger)
        {
            // Get the list of cmdlet metadata from each module
            var oldCmdlets = oldModuleMetadata.Cmdlets;
            var newCmdlets = newModuleMetadata.Cmdlets;

            // Get the type dictionary from each module
            var oldTypeDictionary = oldModuleMetadata.TypeDictionary;
            var newTypeDictionary = newModuleMetadata.TypeDictionary;

            // Initialize a TypeMetadataHelper object that knows how to compare types
            var typeMetadataHelper = new TypeMetadataHelper(oldTypeDictionary, newTypeDictionary);

            // Initialize a CmdletMetadataHelper object that knows how to compare cmdlets
            var cmdletMetadataHelper = new CmdletMetadataHelper(typeMetadataHelper);

            // Compare the cmdlet metadata
            cmdletMetadataHelper.CompareCmdletMetadata(oldCmdlets, newCmdlets, issueLogger);
        }
    }

    public static class LogExtensions
    {
        public static void LogBreakingChangeIssue(
            this ReportLogger<BreakingChangeIssue> issueLogger, CmdletMetadata cmdlet,
            string description, string remediation, int severity, int problemId)
        {
            issueLogger.LogRecord(new BreakingChangeIssue
            {
                ClassName = cmdlet.ClassName,
                Target = cmdlet.Name,
                Description = description,
                Remediation = remediation,
                Severity = severity,
                ProblemId = problemId
            });
        }
    }
}