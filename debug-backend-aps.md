Get-ChildItem -Recurse -Filter *.cs | Select-String "invoice/attachment|InvoiceAttachment|RequestDetailId" | Select-Object Path,LineNumber | Select-Object -First 15
