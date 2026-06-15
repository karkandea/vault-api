(Get-Content "APS-REST-API\Repository\ShoppingCart\InvoiceManagementRepository.cs") | Select-String "GenerateInvoiceAttachment" | Select-Object LineNumber,Line
