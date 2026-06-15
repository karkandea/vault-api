SELECT PO.Id, ATH.Id, ATH.RefId
FROM PurchaseOrder PO
JOIN Attachment ATH ON PO.Id = ATH.RefId
WHERE PO.Id = 12273 AND ATH.Category = 'INV'
