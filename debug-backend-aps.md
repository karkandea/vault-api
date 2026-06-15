Get-ChildItem -Recurse -Filter "FinanceRepository.cs" | Select-String "InvoiceAttachment|GetAttachmentShop|GetAttachmentNonShop" | Select-Object Path,LineNumber
