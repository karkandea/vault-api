Get-ChildItem -Recurse -Filter "*.cs" | Select-String "AttachmentInvoiceShoppingCart" | Select-Object Path,LineNumber
