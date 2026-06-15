SELECT ATH.Id, ATH.RefId, ATH.Category, ATH.Description
FROM PurchaseOrder PO
JOIN Attachment ATH ON PO.Id = ATH.RefId
WHERE PO.Id = 12273
AND ATH.Category = 'INV'
AND ATH.Description = 'ShoppingCart'
