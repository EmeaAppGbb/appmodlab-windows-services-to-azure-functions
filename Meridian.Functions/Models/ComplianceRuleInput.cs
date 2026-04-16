namespace Meridian.Functions.Models;

/// <summary>
/// Input for a single compliance rule check activity — used in fan-out/fan-in
/// to evaluate each rule independently in parallel.
/// </summary>
public class ComplianceRuleInput
{
    public string? DocumentId { get; set; }
    public string? FileName { get; set; }
    public string? DocumentType { get; set; }
    public string? ExtractedData { get; set; }
    public string? RuleName { get; set; }
    public string? RuleExpression { get; set; }
}
