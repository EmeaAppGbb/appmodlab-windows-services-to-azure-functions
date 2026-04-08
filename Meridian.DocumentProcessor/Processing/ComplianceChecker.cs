using System;
using System.Data;
using log4net;
using Meridian.Shared.Data;

namespace Meridian.DocumentProcessor.Processing
{
    public class ComplianceChecker
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ComplianceChecker));

        public string CheckCompliance(string extractedData, string documentType)
        {
            try
            {
                var rules = SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetActiveComplianceRules);
                var passedCount = 0;
                var failedCount = 0;

                foreach (DataRow rule in rules.Rows)
                {
                    var ruleName = rule["Name"].ToString();
                    var expression = rule["Expression"].ToString();
                    
                    var passed = EvaluateRule(extractedData, expression);
                    if (passed)
                        passedCount++;
                    else
                        failedCount++;

                    log.Debug($"Rule '{ruleName}' result: {(passed ? "PASS" : "FAIL")}");
                }

                var result = failedCount == 0 ? "PASS" : "FAIL";
                log.Info($"Compliance check completed: {passedCount} passed, {failedCount} failed. Overall: {result}");
                return result;
            }
            catch (Exception ex)
            {
                log.Error("Error checking compliance", ex);
                return "ERROR";
            }
        }

        private bool EvaluateRule(string data, string expression)
        {
            if (string.IsNullOrEmpty(data))
                return false;

            if (expression.Contains("CONTAINS"))
            {
                var keyword = expression.Replace("CONTAINS:", "").Trim();
                return data.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            }
            else if (expression.Contains("LENGTH"))
            {
                var minLength = int.Parse(expression.Replace("LENGTH>", "").Trim());
                return data.Length > minLength;
            }

            return true;
        }
    }
}
