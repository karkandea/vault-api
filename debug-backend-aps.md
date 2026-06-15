Get-ChildItem -Recurse -Filter *.cs | Select-String "InvoiceAttachment" | Select-Object Path,LineNumber
