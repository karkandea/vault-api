cd "C:\Users\I Nyoman Krisna\GLI-APS-RestApi"
(Get-Content "APS-REST-API\Queries\Finance.cs") | Select-String "RefId|InvoicePO|Attachment|Category" | Select-Object LineNumber,Line
