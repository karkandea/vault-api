Get-ChildItem -Recurse -Filter *.cs | Select-String "ListAttachment" | Select-Object Path,LineNumber | Select-Object -First 10
