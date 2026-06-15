Get-ChildItem -Recurse -Filter *.cshtml | Select-String "GenerateInvoice|Click here to view" | Select-Object Path,LineNumber
