PS C:\Users\I Nyoman Krisna\GLI-APS-RestApi> (Get-Content "APS-REST-API\Queries\Finance.cs")[4451..4548]
        public string GetAttachmentShop()
        {
            string query = $@"DECLARE @SubCategory INT = (
                               SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01261'
                           )
                                                        DECLARE @PurchaseOrderId INT = (
                                                                                                                          SELECT Id FROM PurchaseOrder WHERE PONumber = @RequestNumber
                                                                                                                   )
                                                        DECLARE @PurchaseRequestId INT = (

 SELECT PRTOPO.PurchaseRequestlId

 FROM PurchaseOrder PO

         JOIN PurchaseOrderToPurchaseRequest PRTOPO

                 ON PO.Id = PRTOPO.PurchaseOrderId

 WHERE PO.PONumber = @RequestNumber
                                                                                                                        )

                                                        CREATE TABLE #Header (PurchaseOrderDetailId INT)
                                                        INSERT INTO #Header
                                                        (
                                                                PurchaseOrderDetailId
                                                        )
                                                        SELECT POD.Id
                                                        FROM PurchaseOrder PO
                                                                JOIN PurchaseOrderDetail POD
                                                                        ON PO.Id = POD.PurchaseOrderId
                                                        WHERE PO.PONumber = @RequestNumber

                                                        SELECT Id
                                                        FROM
                                                        (
                                                                SELECT ATH.Id
                                                                FROM PurchaseRequestItemDetail PRD
                                                                        JOIN Attachment ATH
                                                                                ON PRD.AttachmentId = ATH.Id
                                                                WHERE PRD.PurchaseRequestId IN(@PurchaseRequestId)
                                                                          AND ATH.Category = 'PR'
                                                        ) ATCH
                                                        UNION ALL
                                                        SELECT Id
                                                        FROM
                                                        (
                                                                SELECT ATH.Id
                                                                FROM Attachment ATH
                                                                WHERE ATH.RefId = @PurchaseRequestId
                                                                          AND ATH.Category = 'PurchaseRequest'
                                                        ) ATCH
                                                        UNION ALL
                                                        SELECT Id
                                                        FROM
                                                        (
                                                                SELECT ATH.Id
                                                                FROM PurchaseOrder PO
                                                                        JOIN Attachment ATH
                                                                                ON PO.AttachmentId = ATH.Id
                                                                WHERE PO.Id IN ( @PurchaseOrderId )
                                                                          AND ATH.Category = 'PO'
                                                        ) ATCH
                                                        UNION ALL
                                                        SELECT Id
                                                        FROM
                                                        (
                                                                SELECT ATH.Id
                                                                FROM DeliveryNotesDetail DND
                                                                        JOIN Attachment ATH
                                                                                ON DND.Id = ATH.RefId
                                                                WHERE DND.PurchaseOrderDetailId IN (

           SELECT PurchaseOrderDetailId FROM #Header

   )
                                                                          AND ATH.Category = 'DN'
                                                        ) ATCH
                                                        UNION ALL
                                                        SELECT Id
                                                        FROM
                                                        (
                                                                SELECT ATH.Id
                                                                FROM PurchaseOrder PO
                                                                         JOIN Attachment ATH
                                                                                ON PO.Id = ATH.RefId
                                                                WHERE PO.Id = @PurchaseOrderId
                                                                          AND ATH.Category = 'INV'
                                                                          AND ATH.Description = 'ShoppingCart'
                                                        ) ATCH
                                                        UNION ALL
                                                        SELECT Id
                                                        FROM
                                                        (
                                                                SELECT ATH.Id
                                                                FROM InvoicePO INV
                                                                        JOIN Attachment ATH
                                                                                ON ATH.RefId = INV.Id
                                                                WHERE INV.PurchaeseOrderId = @PurchaseOrderId
                                                                          AND ATH.Category = 'INV'
                                                                          AND INV.CategoryProcess_SubCategoryId = @SubCategory
                                                        ) ATCH
                                                        DROP TABLE #Header";
            return query;
        }
        public string GetAttachmentNonShop()
PS C:\Users\I Nyoman Krisna\GLI-APS-RestApi>
