cd ..\GLI-APS-WebApp
Get-ChildItem -Recurse -Filter *.cshtml | Select-String "view Attachment|GenerateInvoice|ListAttachment" | Select-Object Path,LineNumber | Select-Object -First 10
