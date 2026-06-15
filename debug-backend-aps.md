(Get-Content "APS-REST-API\Queries\Finance.cs") | Select-String "public string GetAttachment|public string Get.*Attachment" | Select-Object LineNumber,Line
