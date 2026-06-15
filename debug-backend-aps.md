DECLARE @SubCategory INT = (SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01261')
SELECT ATH.Id FROM InvoicePO INV
JOIN Attachment ATH ON ATH.RefId = INV.Id
WHERE INV.PurchaeseOrderId = 12273
AND ATH.Category = 'INV'
AND INV.CategoryProcess_SubCategoryId = @SubCategory
