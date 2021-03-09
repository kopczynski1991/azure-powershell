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

using StaticAnalysis.ProblemIds;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Tools.Common.Helpers;
using Tools.Common.Issues;
using Tools.Common.Loaders;
using Tools.Common.Loggers;
using Tools.Common.Models;

namespace StaticAnalysis.SignatureVerifier
{
    public class SignatureVerifier
    {
// TODO: Remove IfDef code
#if !NETSTANDARD
        private AppDomain _appDomain;
#endif
        private readonly string _signatureIssueReportLoggerName;
        private ReportLogger<SignatureIssue> issueLogger;
        public SignatureVerifier()
        {
            Name = "Signature Verifier";
            _signatureIssueReportLoggerName = "SignatureIssues.csv";
        }
        public AnalysisLogger Logger { get; set; }

        public string Name { get; private set; }

        public void initLogger()
        {
            issueLogger = Logger.CreateLogger<SignatureIssue>(_signatureIssueReportLoggerName);
        }

        public void AnalyzeModule(ModuleMetadata moduleMetadata)
        {
            var cmdlets = moduleMetadata.Cmdlets;

            foreach (var cmdlet in cmdlets)
            {
                Logger.WriteMessage("Processing cmdlet '{0}'", cmdlet.ClassName);
                const string defaultRemediation = "Determine if the cmdlet should implement ShouldProcess and " +
                                                    "if so determine if it should implement Force / ShouldContinue";
                if (!cmdlet.SupportsShouldProcess && cmdlet.HasForceSwitch)
                {
                    issueLogger.LogSignatureIssue(
                        cmdlet: cmdlet,
                        severity: 0,
                        problemId: SignatureProblemId.ForceWithoutShouldProcessAttribute,
                        description: string.Format("{0} Has  -Force parameter but does not set the SupportsShouldProcess " +
                                                    "property to true in the Cmdlet attribute.", cmdlet.Name),
                        remediation: defaultRemediation);
                }
                if (!cmdlet.SupportsShouldProcess && cmdlet.ConfirmImpact != ConfirmImpact.Medium)
                {
                    issueLogger.LogSignatureIssue(
                        cmdlet: cmdlet,
                        severity: 2,
                        problemId: SignatureProblemId.ConfirmLeveleWithNoShouldProcess,
                        description:
                        string.Format("{0} Changes the ConfirmImpact but does not set the " +
                                        "SupportsShouldProcess property to true in the cmdlet attribute.",
                            cmdlet.Name),
                        remediation: defaultRemediation);
                }
                if (!cmdlet.SupportsShouldProcess && cmdlet.IsShouldProcessVerb)
                {
                    issueLogger.LogSignatureIssue(
                        cmdlet: cmdlet,
                        severity: 1,
                        problemId: SignatureProblemId.ActionIndicatesShouldProcess,
                        description:
                        string.Format(
                            "{0} Does not support ShouldProcess but the cmdlet verb {1} indicates that it should.",
                            cmdlet.Name, cmdlet.VerbName),
                        remediation: defaultRemediation);
                }
                if (cmdlet.ConfirmImpact != ConfirmImpact.Medium)
                {
                    issueLogger.LogSignatureIssue(
                        cmdlet: cmdlet,
                        severity: 2,
                        problemId: SignatureProblemId.ConfirmLevelChange,
                        description:
                        string.Format("{0} changes the confirm impact.  Please ensure that the " +
                                        "change in ConfirmImpact is justified", cmdlet.Name),
                        remediation:
                        "Verify that ConfirmImpact is changed appropriately by the cmdlet. " +
                        "It is very rare for a cmdlet to change the ConfirmImpact.");
                }
                if (!cmdlet.IsApprovedVerb)
                {
                    issueLogger.LogSignatureIssue(
                        cmdlet: cmdlet,
                        severity: 1,
                        problemId: SignatureProblemId.CmdletWithUnapprovedVerb,
                        description:
                        string.Format(
                            "{0} uses the verb '{1}', which is not on the list of approved " +
                            "verbs for PowerShell commands. Use the cmdlet 'Get-Verb' to see " +
                            "the full list of approved verbs and consider renaming the cmdlet.",
                            cmdlet.Name, cmdlet.VerbName),
                        remediation: "Consider renaming the cmdlet to use an approved verb for PowerShell.");
                }

                if (!cmdlet.HasSingularNoun)
                {
                    issueLogger.LogSignatureIssue(
                        cmdlet: cmdlet,
                        severity: 1,
                        problemId: SignatureProblemId.CmdletWithPluralNoun,
                        description:
                        string.Format(
                            "{0} uses the noun '{1}', which does not follow the enforced " +
                            "naming convention of using a singular noun for a cmdlet name.",
                            cmdlet.Name, cmdlet.NounName),
                        remediation: "Consider using a singular noun for the cmdlet name.");
                }

                if (!cmdlet.OutputTypes.Any())
                {
                    issueLogger.LogSignatureIssue(
                        cmdlet: cmdlet,
                        severity: 1,
                        problemId: SignatureProblemId.CmdletWithNoOutputType,
                        description:
                        string.Format(
                            "Cmdlet '{0}' has no defined output type.", cmdlet.Name),
                        remediation: "Add an OutputType attribute that declares the type of the object(s) returned " +
                                        "by this cmdlet. If this cmdlet returns no output, please set the output " +
                                        "type to 'bool' and make sure to implement the 'PassThru' parameter.");
                }

                foreach (var parameter in cmdlet.GetParametersWithPluralNoun())
                {
                    issueLogger.LogSignatureIssue(
                        cmdlet: cmdlet,
                        severity: 1,
                        problemId: SignatureProblemId.ParameterWithPluralNoun,
                        description:
                        string.Format(
                            "Parameter {0} of cmdlet {1} does not follow the enforced " +
                            "naming convention of using a singular noun for a parameter name.",
                            parameter.Name, cmdlet.Name),
                        remediation: "Consider using a singular noun for the parameter name.");
                }

                foreach (var parameterSet in cmdlet.ParameterSets)
                {
                    if (parameterSet.Name.Contains(" "))
                    {
                        issueLogger.LogSignatureIssue(
                            cmdlet: cmdlet,
                            severity: 1,
                            problemId: SignatureProblemId.ParameterSetWithSpace,
                            description:
                            string.Format(
                                "Parameter set '{0}' of cmdlet '{1}' contains a space, which " +
                                "is discouraged for PowerShell parameter sets.",
                                parameterSet.Name, cmdlet.Name),
                            remediation: "Remove the space(s) in the parameter set name.");
                    }

                    if (parameterSet.Parameters.Any(p => p.Position >= 4))
                    {
                        issueLogger.LogSignatureIssue(
                            cmdlet: cmdlet,
                            severity: 1,
                            problemId: SignatureProblemId.ParameterWithOutOfRangePosition,
                            description:
                            string.Format(
                                "Parameter set '{0}' of cmdlet '{1}' contains at least one parameter " +
                                "with a position larger than four, which is discouraged.",
                                parameterSet.Name, cmdlet.Name),
                            remediation: "Limit the number of positional parameters in a single parameter set to " +
                                            "four or fewer.");
                    }
                }

                if (cmdlet.ParameterSets.Count > 2 && cmdlet.DefaultParameterSetName == "__AllParameterSets")
                {
                    issueLogger.LogSignatureIssue(
                        cmdlet: cmdlet,
                        severity: 1,
                        problemId: SignatureProblemId.MultipleParameterSetsWithNoDefault,
                        description:
                        string.Format(
                            "Cmdlet '{0}' has multiple parameter sets, but no defined default parameter set.",
                            cmdlet.Name),
                        remediation: "Define a default parameter set in the cmdlet attribute.");
                }
            }
        }

    }

    public static class LogExtensions
    {
        public static void LogSignatureIssue(this ReportLogger<SignatureIssue> issueLogger, CmdletMetadata cmdlet,
            string description, string remediation, int severity, int problemId)
        {
            issueLogger.LogRecord(new SignatureIssue
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