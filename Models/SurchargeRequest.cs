namespace FeeNominalService.Models;

public class SurchargeRequest
{
    public string? nicn { get; set; }
    public string? processor { get; set; }
    public decimal? amount { get; set; }
    public decimal? totalAmount { get; set; }
    public string? country { get; set; }
    public string? region { get; set; }
    public List<string>? campaign { get; set; }
    public List<string>? data { get; set; }
    public string? sTxId { get; set; }
    public string? mTxId { get; set; }
    public string? cardToken { get; set; }
    public EntryMethod? entryMethod { get; set; }
    public decimal? nonSurchargableAmount { get; set; }
} 