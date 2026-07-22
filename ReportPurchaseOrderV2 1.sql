DECLARE @Start INT = 0,
        @Length INT = 10,
        @PR_Category_Id INT = NULL,
        @PR_No NVARCHAR(50) = NULL,
        @PR_Status_ValueId INT = NULL,
        @PR_Date_Begin DATE = NULL,
        @PR_Date_End DATE = NULL,
        @Department_Id INT = NULL,
        @Account_Code_Id INT = NULL,
        @Cost_Center_Id INT = NULL,
        @Vendor_Id INT = NULL,
        @Order_Type_Id INT = NULL,
        @Order_No NVARCHAR(50) = NULL,
        @Order_Status_ValueId INT = NULL,
        @Order_Date_Begin DATE = '2025-01-01',
        @Order_Date_End DATE = '2026-07-16'

SET NOCOUNT ON;
                            -- ============================================================================
                            -- 2. CLEANUP (Hapus Temp Table jika sudah ada)
                            -- ============================================================================
                            IF OBJECT_ID('tempdb..#w_bt') IS NOT NULL
                                DROP TABLE #w_bt;
                            IF OBJECT_ID('tempdb..#w_p_last_psc') IS NOT NULL
                                DROP TABLE #w_p_last_psc;
                            IF OBJECT_ID('tempdb..#w_ps_TAT') IS NOT NULL
                                DROP TABLE #w_ps_TAT;
                            IF OBJECT_ID('tempdb..#w_p_FinalSpesificationDate_GenerateDate') IS NOT NULL
                                DROP TABLE #w_p_FinalSpesificationDate_GenerateDate;
                            IF OBJECT_ID('tempdb..#w_p_FinalSpesificationDate_GenerateDate_TATWD') IS NOT NULL
                                DROP TABLE #w_p_FinalSpesificationDate_GenerateDate_TATWD;
                            IF OBJECT_ID('tempdb..#w_prid_group_prioc') IS NOT NULL
                                DROP TABLE #w_prid_group_prioc;
                            IF OBJECT_ID('tempdb..#w_pr_group_prid') IS NOT NULL
                                DROP TABLE #w_pr_group_prid;
                            IF OBJECT_ID('tempdb..#w_psd_group_psoc') IS NOT NULL
                                DROP TABLE #w_psd_group_psoc;
                            IF OBJECT_ID('tempdb..#w_ps_group_psd') IS NOT NULL
                                DROP TABLE #w_ps_group_psd;
                            IF OBJECT_ID('tempdb..#w_pvqd_group_pvqoc') IS NOT NULL
                                DROP TABLE #w_pvqd_group_pvqoc;
                            IF OBJECT_ID('tempdb..#w_pvq_group_pvqd') IS NOT NULL
                                DROP TABLE #w_pvq_group_pvqd;
                            IF OBJECT_ID('tempdb..#w_pr') IS NOT NULL
                                DROP TABLE #w_pr;
                            IF OBJECT_ID('tempdb..#w_argm') IS NOT NULL
                                DROP TABLE #w_argm;
                            IF OBJECT_ID('tempdb..#w_type_process') IS NOT NULL
                                DROP TABLE #w_type_process;
                            IF OBJECT_ID('tempdb..#w_dn') IS NOT NULL
                                DROP TABLE #w_dn;
                            IF OBJECT_ID('tempdb..#w_ipo') IS NOT NULL
                                DROP TABLE #w_ipo;

                            -- ============================================================================
                            -- 3. PEMBUATAN TEMP TABLE & INDEXING
                            -- ============================================================================

                            /* 
                               --- #w_bt ---
                               Fungsi: Mengambil data transaksi budget (BudgetTransaction) yang terkait 
                               dengan SubCategory tertentu. Digunakan untuk join dengan PRF.
                            */
                            SELECT 1 as x,
                                   bt.RefNumber,
                                   bt.L_Currency_Code,
                                   bt.RateAmount
                            INTO #w_bt
                            FROM BudgetTransaction as bt
                                INNER JOIN SubCategory as Transaction_sc
                                    ON Transaction_sc.SubCategoryCode IN ( 'Transaction.SC', 'Transaction.PRF' )
                                       AND Transaction_sc.Id = bt.Transaction_SubCategoryId
                            GROUP BY bt.RefNumber,
                                     bt.L_Currency_Code,
                                     bt.RateAmount;

                            CREATE NONCLUSTERED INDEX IX_w_bt_RefNumber ON #w_bt (RefNumber);


                            /* 
                               --- #w_p_last_psc ---
                               Fungsi: Mengambil PRFSpendingCategory terakhir (Max Id) per PRFId 
                               yang statusnya aktif (Status = 1).
                            */
                            SELECT 1 as x,
                                   psc.PRFId,
                                   MAX(psc.Id) as PRFSpendingCategoryId
                            INTO #w_p_last_psc
                            FROM PRFSpendingCategory as psc
                            WHERE psc.Status = 1
                            GROUP BY psc.PRFId;

                            CREATE NONCLUSTERED INDEX IX_w_p_last_psc_PRFId ON #w_p_last_psc (PRFId);


                            /* 
                               --- #w_ps_TAT ---
                               Fungsi: Menghitung Turnaround Time (TAT) dalam hari kerja (Working Days) 
                               dari proses PRFSpendingCategory ke PRFSummary. 
                               Menghitung selisih hari, minggu, weekend, dan hari libur.
                            */
                            SELECT 1 as x,
                                   ps.Id as PRFSummaryId,
                                   psc.CreatedTime,
                                   ps.PRFSummaryDate,
                                   (DATEDIFF(dd, psc.CreatedTime, ps.PRFSummaryDate) + 1) as 'dd',
                                   (DATEDIFF(wk, psc.CreatedTime, ps.PRFSummaryDate) * 2) as 'wk',
                                   (CASE
                                        WHEN DATENAME(dw, psc.CreatedTime) = 'Sunday' THEN
                                            1
                                        ELSE
                                            0
                                    END
                                   ) as 'Sunday',
                                   (CASE
                                        WHEN DATENAME(dw, ps.PRFSummaryDate) = 'Saturday' THEN
                                            1
                                        ELSE
                                            0
                                    END
                                   ) as 'Saturday',
                                   (
                                       SELECT COUNT(*)
                                       FROM MasterHoliday
                                       WHERE CAST(DateHoliday as date)
                                       BETWEEN CAST(psc.CreatedTime as date) AND CAST(ps.PRFSummaryDate as date)
                                   ) as 'MasterHoliday',
                                   -- TAT (WD)
                                   (0 + (DATEDIFF(dd, psc.CreatedTime, ps.PRFSummaryDate) + 1)
                                    - (DATEDIFF(wk, psc.CreatedTime, ps.PRFSummaryDate) * 2)
                                    - (CASE
                                           WHEN DATENAME(dw, psc.CreatedTime) = 'Sunday' THEN
                                               1
                                           ELSE
                                               0
                                       END
                                      ) - (CASE
                                               WHEN DATENAME(dw, ps.PRFSummaryDate) = 'Saturday' THEN
                                                   1
                                               ELSE
                                                   0
                                           END
                                          ) -
                                    (
                                        SELECT COUNT(*)
                                        FROM MasterHoliday
                                        WHERE CAST(DateHoliday as date)
                                        BETWEEN CAST(psc.CreatedTime as date) AND CAST(ps.PRFSummaryDate as date)
                                    )
                                   ) as 'TAT_WD'
                            INTO #w_ps_TAT
                            FROM PRFSummary as ps
                                INNER JOIN #w_p_last_psc as p_last_psc
                                    ON p_last_psc.PRFId = ps.PRFId
                                INNER JOIN PRFSpendingCategory as psc
                                    ON psc.Id = p_last_psc.PRFSpendingCategoryId;

                            CREATE NONCLUSTERED INDEX IX_w_ps_TAT_PRFSummaryId
                            ON #w_ps_TAT (PRFSummaryId);


                            /* 
                               --- #w_p_FinalSpesificationDate_GenerateDate ---
                               Fungsi: Menentukan tanggal Final Specification dan Generate Date (ProcSum/PAP) 
                               untuk perhitungan TAT yang lebih akurat.
                            */
                            SELECT 1 as x,
                                   p.Id as PRFId,
                                   CAST(COALESCE(pvq.FinalSpesificationDate, psc.CreatedTime) as date) as 'FinalSpesificationDate',
                                   (CASE
                                        WHEN tp_sc.SubCategoryCode = 'SC-2023-08-11134' -- PAP
                                   THEN
                                            pap.GenerateDate
                                        ELSE
                                            ps.PRFSummaryDate
                                    END
                                   ) as 'GenerateDate'
                            INTO #w_p_FinalSpesificationDate_GenerateDate
                            FROM PRF as p
                                INNER JOIN SubCategory as tp_sc
                                    ON tp_sc.Id = p.TypeProcess_SubCategory
                                INNER JOIN #w_p_last_psc as p_last_psc
                                    ON p_last_psc.PRFId = p.Id
                                INNER JOIN PRFSpendingCategory as psc
                                    ON psc.Id = p_last_psc.PRFSpendingCategoryId
                                INNER JOIN PRFVendorQuotation as pvq
                                    ON pvq.PRFId = p.Id
                                LEFT JOIN
                                (
                                    SELECT ps.PRFId,
                                           MAX(ps.Id) as Id
                                    FROM PRFSummary as ps
                                    GROUP BY ps.PRFId
                                ) as last_ps
                                    ON last_ps.PRFId = p.Id
                                LEFT JOIN PRFSummary as ps
                                    ON ps.PRFVendorQuotationId = pvq.Id
                                       AND last_ps.Id = ps.Id
                                LEFT JOIN PAP as pap
                                    ON pap.PRFVendorQuotationId = pvq.Id;

                            CREATE NONCLUSTERED INDEX IX_w_p_FinalSpesificationDate_GenerateDate_PRFId
                            ON #w_p_FinalSpesificationDate_GenerateDate (PRFId);


                            /* 
                               --- #w_p_FinalSpesificationDate_GenerateDate_TATWD ---
                               Fungsi: Menghitung detail komponen hari (hari, minggu, weekend, libur) 
                               antara Final Spec dan Generate Date untuk mendapatkan TAT_WD final.
                            */
                            SELECT 1 as x,
                                   x.PRFId,
                                   x.FinalSpesificationDate,
                                   x.GenerateDate,
                                   x.datediff_day,
                                   x.datediff_week,
                                   x.datename_Sunday,
                                   x.datename_Saturday,
                                   x.MasterHoliday_count,
                                   (x.datediff_day - x.datediff_week - x.datename_Sunday - x.datename_Saturday - x.MasterHoliday_count) as 'TAT_WD'
                            INTO #w_p_FinalSpesificationDate_GenerateDate_TATWD
                            FROM
                            (
                                SELECT 1 as x,
                                       p_FinalSpesificationDate_GenerateDate.PRFId,
                                       p_FinalSpesificationDate_GenerateDate.FinalSpesificationDate,
                                       p_FinalSpesificationDate_GenerateDate.GenerateDate,
                                       DATEDIFF(
                                                   day,
                                                   p_FinalSpesificationDate_GenerateDate.FinalSpesificationDate,
                                                   p_FinalSpesificationDate_GenerateDate.GenerateDate
                                               ) as 'datediff_day',
                                       (DATEDIFF(
                                                    week,
                                                    p_FinalSpesificationDate_GenerateDate.FinalSpesificationDate,
                                                    p_FinalSpesificationDate_GenerateDate.GenerateDate
                                                ) * 2
                                       ) as 'datediff_week',
                                       (CASE
                                            WHEN DATENAME(dw, p_FinalSpesificationDate_GenerateDate.FinalSpesificationDate) = 'Sunday' THEN
                                                1
                                            ELSE
                                                0
                                        END
                                       ) as 'datename_Sunday',
                                       (CASE
                                            WHEN DATENAME(dw, p_FinalSpesificationDate_GenerateDate.GenerateDate) = 'Saturday' THEN
                                                1
                                            ELSE
                                                0
                                        END
                                       ) as 'datename_Saturday',
                                       (
                                           SELECT COUNT(*) as c
                                           FROM MasterHoliday as mh
                                           WHERE CAST(mh.DateHoliday as date)
                                           BETWEEN CAST(p_FinalSpesificationDate_GenerateDate.FinalSpesificationDate as date) AND CAST(p_FinalSpesificationDate_GenerateDate.GenerateDate as date)
                                       ) as 'MasterHoliday_count'
                                FROM #w_p_FinalSpesificationDate_GenerateDate as p_FinalSpesificationDate_GenerateDate
                            ) as x;

                            CREATE NONCLUSTERED INDEX IX_w_p_FinalSpesificationDate_GenerateDate_TATWD_PRFId
                            ON #w_p_FinalSpesificationDate_GenerateDate_TATWD (PRFId);


                            /* 
                               --- #w_prid_group_prioc ---
                               Fungsi: Agregasi Other Cost per PurchaseRequestItemDetail.
                            */
                            SELECT 1 as x,
                                   prid.PurchaseRequestId,
                                   prioc.PurchaseRequestItemDetailId,
                                   SUM(prioc.Amount) as Amount_sum
                            INTO #w_prid_group_prioc
                            FROM PurchaseRequestItemDetail as prid
                                INNER JOIN PurchaseRequestItemOtherCost as prioc
                                    ON prioc.PurchaseRequestItemDetailId = prid.Id
                            GROUP BY prid.PurchaseRequestId,
                                     prioc.PurchaseRequestItemDetailId;

                            CREATE NONCLUSTERED INDEX IX_w_prid_group_prioc_PurchaseRequestItemDetailId
                            ON #w_prid_group_prioc (PurchaseRequestItemDetailId);
                            CREATE NONCLUSTERED INDEX IX_w_prid_group_prioc_PurchaseRequestId
                            ON #w_prid_group_prioc (PurchaseRequestId);


                            /* 
                               --- #w_pr_group_prid ---
                               Fungsi: Agregasi Total Harga (Qty * Price) dan Other Cost per PurchaseRequestId.
                            */
                            SELECT 1 as x,
                                   prid.PurchaseRequestId,
                                   SUM(prid.QtyRequest * prid.ItemPrice) as QtyRequest_x_ItemPrice_x_RateAmount_sum,
                                   SUM(prid_group_prioc.Amount_sum) as group_prioc_Amount_sum
                            INTO #w_pr_group_prid
                            FROM PurchaseRequestItemDetail as prid
                                INNER JOIN #w_prid_group_prioc as prid_group_prioc
                                    ON prid_group_prioc.PurchaseRequestItemDetailId = prid.Id
                            GROUP BY prid.PurchaseRequestId;

                            CREATE NONCLUSTERED INDEX IX_w_pr_group_prid_PurchaseRequestId
                            ON #w_pr_group_prid (PurchaseRequestId);


                            /* 
                               --- #w_psd_group_psoc ---
                               Fungsi: Agregasi Other Cost per PRFSummaryDetail.
                            */
                            SELECT 1 as x,
                                   psd.PRFSummaryId,
                                   psoc.PRFSummaryDetailId,
                                   SUM(psoc.OtherCostAmount) as OtherCostAmount_sum
                            INTO #w_psd_group_psoc
                            FROM PRFSummaryDetail as psd
                                INNER JOIN PRFSummaryOtherCost as psoc
                                    ON psoc.PRFSummaryDetailId = psd.Id
                            GROUP BY psd.PRFSummaryId,
                                     psoc.PRFSummaryDetailId;

                            CREATE NONCLUSTERED INDEX IX_w_psd_group_psoc_PRFSummaryDetailId
                            ON #w_psd_group_psoc (PRFSummaryDetailId);
                            CREATE NONCLUSTERED INDEX IX_w_psd_group_psoc_PRFSummaryId
                            ON #w_psd_group_psoc (PRFSummaryId);


                            /* 
                               --- #w_ps_group_psd ---
                               Fungsi: Agregasi Total Harga dan Other Cost per PRFSummaryId (hanya item terpilih).
                            */
                            SELECT 1 as x,
                                   psd.PRFSummaryId,
                                   SUM(psd.Qty * psd.ItemPrice) as Qty_x_ItemPrice_x_RateAmmount_sum,
                                   SUM(psd_group_psoc.OtherCostAmount_sum) as group_psoc_OtherCostAmount_sum
                            INTO #w_ps_group_psd
                            FROM PRFSummaryDetail as psd
                                INNER JOIN #w_psd_group_psoc as psd_group_psoc
                                    ON psd_group_psoc.PRFSummaryDetailId = psd.Id
                            WHERE psd.IsSelected = 1
                            GROUP BY psd.PRFSummaryId;

                            CREATE NONCLUSTERED INDEX IX_w_ps_group_psd_PRFSummaryId
                            ON #w_ps_group_psd (PRFSummaryId);


                            /* 
                               --- #w_pvqd_group_pvqoc ---
                               Fungsi: Agregasi Other Cost per PRFVendorQuotationDetail.
                            */
                            SELECT 1 as x,
                                   pvqd.PRFVendorQuotationId,
                                   pvqoc.PRFVendorQuotationDetailId,
                                   SUM(pvqoc.OtherCostAmount) as OtherCostAmount_sum
                            INTO #w_pvqd_group_pvqoc
                            FROM PRFVendorQuotationDetail as pvqd
                                INNER JOIN PRFVendorQuotationOtherCost as pvqoc
                                    ON pvqoc.PRFVendorQuotationDetailId = pvqd.Id
                            GROUP BY pvqd.PRFVendorQuotationId,
                                     pvqoc.PRFVendorQuotationDetailId;

                            CREATE NONCLUSTERED INDEX IX_w_pvqd_group_pvqoc_PRFVendorQuotationDetailId
                            ON #w_pvqd_group_pvqoc (PRFVendorQuotationDetailId);
                            CREATE NONCLUSTERED INDEX IX_w_pvqd_group_pvqoc_PRFVendorQuotationId
                            ON #w_pvqd_group_pvqoc (PRFVendorQuotationId);


                            /* 
                               --- #w_pvq_group_pvqd ---
                               Fungsi: Agregasi Total Harga dan Other Cost per PRFVendorQuotationId.
                            */
                            SELECT 1 as x,
                                   pvqd.PRFVendorQuotationId,
                                   SUM(pvqd.Qty * pvqd.ItemPrice) as Qty_x_ItemPrice_x_RateAmmount_sum,
                                   SUM(pvqd_group_pvqoc.OtherCostAmount_sum) as group_pvqoc_OtherCostAmount_sum
                            INTO #w_pvq_group_pvqd
                            FROM PRFVendorQuotationDetail as pvqd
                                INNER JOIN #w_pvqd_group_pvqoc as pvqd_group_pvqoc
                                    ON pvqd_group_pvqoc.PRFVendorQuotationDetailId = pvqd.Id
                            GROUP BY pvqd.PRFVendorQuotationId;

                            CREATE NONCLUSTERED INDEX IX_w_pvq_group_pvqd_PRFVendorQuotationId
                            ON #w_pvq_group_pvqd (PRFVendorQuotationId);


                            /* 
                               --- #w_pr ---
                               Fungsi: MAIN TABLE 1. Menggabungkan data Purchase Request (PR) dan PRF (Procurement Request Form).
                               Ini adalah hasil Union All antara proses Shopping Cart dan Non-Shopping Cart.
                               Berisi informasi detail item, vendor, harga, TAT, dan SLA.
                            */
                            SELECT 1 as x,
                                   cp_sc.Id as _CategoryProcess_SubCategoryId,
                                   cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode,
                                   cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName,
                                   tp_sc.Id as _TypeProcess_SubCategoryId,
                                   tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode,
                                   tp_sc.SubCategoryName as _TypeProcess_SubCategoryName,
                                   -- PurchaseOrder
                                   prid.Id as _PurchaseRequestItemDetailId,
                                   NULL as _PurchaseOrderDetailId,
                                   NULL as _PurchaseOrderTOPId,
                                   -- PONonShopping
                                   NULL as _PONonShoppingDetailId,
                                   NULL as _PONonShoppingTOPId,
                                   -- PONonShopping, GuaranteeLetter
                                   NULL as _PRFSummaryDetailId,
                                   -- Contract
                                   NULL as _PRFSummaryId,
                                   -- PAP
                                   NULL as _PRFVendorQuotationDetailId,
                                   --
                                   pr.Id as _Id,
                                   prid.Id as _DetailId,
                                   pr.RequestCode as 'PR_No',
                                   mt.Name as 'PR_Status',
                                   CAST(pr.RequestDates as datetime) as 'PR_Date',
                                   Requestor_ua.Username as 'Requester',
                                   (Requestor_ua_cc.Code + ' - ' + Requestor_ua_cc.Name) as 'Department',
                                   NULL as 'Type_Of_Transaction',
                                   NULL as 'Buyer_User_Name',
                                   NULL as 'Total_Budget_Estimation',
                                   NULL as 'Critical',
                                   NULL as 'DPIA',
                                   NULL as 'VSDDT',
                                   NULL as 'Outsourcing_Status',
                                   NULL as 'Category',
                                   (i.ItemCode + ' - ' + i.Name) as 'Item_Name',
                                   (am.AccountCode + ' - ' + am.Description) as 'Account_Code',
                                   (cc.Code + ' - ' + cc.Name) as 'Cost_Center',
                                   v.Id as _VendorId,
                                   (v.Code + ' - ' + v.Name) as 'Vendor_Selection',
                                   i.L_Currency_Code as 'Currency',
                                   pr.CreatedTime as 'PR_Posted_Date',
                                   (
                                       --- Incident = INC29383617 // PMF = REQ-7635 
                                       --- SELECT TOP 1
                                       ---     pod.DeliveryRequestDate
                                       --- FROM PurchaseOrderDetail pod
                                       --- WHERE pod.PurchaseOrderId = prtopo.PurchaseOrderId
                                       SELECT TOP 1 pod.DeliveryRequestDate FROM PurchaseOrderDetail pod WHERE pod.ItemId = prid.ItemId
                                   ) as 'Delivery_Request_Date',
                                   NULL as 'Final_Spec_Req_Date',
                                   NULL as 'Generate_Proc_Sum_Date',
                                   NULL as 'TAT_WD',
                                   NULL as 'SLA_WD',
                                   NULL as 'SLA_Status',
                                   (v.Code + ' - ' + v.Name) as 'Vendor',
                                   NULL as 'Selected',
                                   pr_group_prid.QtyRequest_x_ItemPrice_x_RateAmount_sum as 'Total_Price',
                                   (prid.QtyRequest * prid.ItemPrice) as 'Price_Per_Item',
                                   pr_group_prid.QtyRequest_x_ItemPrice_x_RateAmount_sum + pr_group_prid.group_prioc_Amount_sum as 'Total_Price_Inc_Other_Cost',
                                   (prid.QtyRequest * prid.ItemPrice) + prid_group_prioc.Amount_sum as 'Price_Per_Item_Inc_Other_Cost',
                                   NULL as 'Realised_Saving',
                                   '' as 'ReasonCancel'
                            INTO #w_pr
                            FROM PurchaseRequest as pr
                                LEFT JOIN #w_pr_group_prid as pr_group_prid
                                    ON pr_group_prid.PurchaseRequestId = pr.Id
                                INNER JOIN Flips.UserAccount as Requestor_ua
                                    ON Requestor_ua.Id = pr.RequestorAccountId
                                INNER JOIN CostCenter as Requestor_ua_cc
                                    ON Requestor_ua_cc.Id = Requestor_ua.CostCenterId
                                       AND (
                                               @Department_Id IS NULL
                                               OR Requestor_ua_cc.Id = @Department_Id
                                           )
                                INNER JOIN MasterTable as mt
                                    ON mt.Category = 'PurchaseRequest.Status'
                                       AND mt.ValueId = pr.Status
                                       AND (
                                               @PR_Status_ValueId IS NULL
                                               OR mt.ValueId = @PR_Status_ValueId
                                           )
                                INNER JOIN PurchaseRequestItemDetail as prid
                                    ON prid.PurchaseRequestId = pr.Id
                                LEFT JOIN #w_prid_group_prioc as prid_group_prioc
                                    ON prid_group_prioc.PurchaseRequestItemDetailId = prid.Id
                                INNER JOIN Item as i
                                    ON i.Id = prid.ItemId
                                INNER JOIN Vendor as v
                                    ON v.Id = i.VendorId
                                       AND (
                                               @Vendor_Id IS NULL
                                               OR v.Id = @Vendor_Id
                                           )
                                INNER JOIN AccountMaster as am
                                    ON am.Id = prid.AccountMasterId
                                       AND (
                                               @Account_Code_Id IS NULL
                                               OR am.Id = @Account_Code_Id
                                           )
                                INNER JOIN PurchaseRequestItemCostCenter as pricc
                                    ON pricc.PurchaseRequestItemDetailId = prid.Id
                                INNER JOIN CostCenter as cc
                                    ON cc.Id = pricc.CostCenterId
                                       AND (
                                               @Cost_Center_Id IS NULL
                                               OR cc.Id = @Cost_Center_Id
                                           )
                                INNER JOIN SubCategory as cp_sc
                                    ON cp_sc.SubCategoryCode = 'SC-2024-02-01261'
                                       AND (
                                               @PR_Category_Id IS NULL
                                               OR cp_sc.Id = @PR_Category_Id
                                           )
                                INNER JOIN SubCategory as tp_sc
                                    ON tp_sc.SubCategoryCode = 'SC-2023-08-11132'
                                       AND (
                                               @Order_Type_Id IS NULL
                                               OR tp_sc.Id = @Order_Type_Id
                                           )
                                --- Incident = INC29383617 // PMF = REQ-7635 
                                --- LEFT JOIN PurchaseOrderToPurchaseRequest as prtopo
                                ---     ON pr.Id = prtopo.PurchaseRequestlId
                            WHERE 1 = 1
                                  AND (
                                          @PR_No IS NULL
                                          OR pr.RequestCode LIKE @PR_No
                                      )
                                  AND (
                                          @PR_Date_Begin IS NULL
                                          OR pr.RequestDates >= @PR_Date_Begin
                                      )
                                  AND (
                                          @PR_Date_End IS NULL
                                          OR pr.RequestDates <= @PR_Date_End
                                      )
                            UNION ALL
                            SELECT 1 as x,
                                   cp_sc.Id as _CategoryProcess_SubCategoryId,
                                   cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode,
                                   cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName,
                                   tp_sc.Id as _TypeProcess_SubCategoryId,
                                   tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode,
                                   tp_sc.SubCategoryName as _TypeProcess_SubCategoryName,
                                   -- PurchaseOrder
                                   NULL as _PurchaseRequestItemDetailId,
                                   NULL as _PurchaseOrderDetailId,
                                   NULL as _PurchaseOrderTOPId,
                                   -- PONonShopping
                                   NULL as _PONonShoppingDetailId,
                                   NULL as _PONonShoppingTOPId,
                                   -- PONonShopping, GuaranteeLetter
                                   psd.Id as _PRFSummaryDetailId,
                                   -- Contract
                                   ps.Id as _PRFSummaryId,
                                   -- PAP
                                   pvqd.Id as _PRFVendorQuotationDetailId,
                                   --
                                   p.Id as _Id,
                                   pd.Id as _DetailId,
                                   p.PRFNo as 'PR_No',
                                   mt.Name as 'PR_Status',
                                   p.RequestDate as 'PR_Date',
                                   ua.Username as 'Requester',
                                   (ua_cc.Code + ' - ' + ua_cc.Name) as 'Department',
                                   p.TypeOfTransaction as 'Type_Of_Transaction',
                                   b_ua.Username as 'Buyer_User_Name',
                                   (p.TotalBudgetEstimation) as 'Total_Budget_Estimation',
                                   (CASE COALESCE(p.IsRiskAssementForm, 0)
                                        WHEN 1 THEN
                                            'Yes'
                                        ELSE
                                            'No'
                                    END
                                   ) as 'Critical',
                                   (CASE
                                        WHEN p.DataActivity IS NULL THEN
                                            ''
                                        WHEN p.DataActivity = 1 THEN
                                            'Yes'
                                        ELSE
                                            'No'
                                    END
                                   ) as 'DPIA',
                                   (CASE
                                        WHEN p.ITSecurityActivity IS NULL THEN
                                            ''
                                        WHEN p.ITSecurityActivity = 1 THEN
                                            'Yes'
                                        ELSE
                                            'No'
                                    END
                                   ) as 'VSDDT',
                                   p.TypeOrder as 'Outsourcing_Status',
                                   s_c.Category as 'Category',
                                   pd.RequestItemName as 'Item_Name',
                                   (am.AccountCode + ' - ' + am.Description) as 'Account_Code',
                                   (cc.Code + ' - ' + cc.Name) as 'Cost_Center',
                                   v.Id as _VendorId,
                                   (v.Code + ' - ' + v.Name) as 'Vendor_Selection',
                                   p.L_Currency_Code as 'Currency',
                                   p.CreatedTime as 'PR_Posted_Date',
                                   pd.DeliveryRequestDate as 'Delivery_Request_Date',
                                   p_FinalSpesificationDate_GenerateDate_TATWD.FinalSpesificationDate as 'Final_Spec_Req_Date',
                                   p_FinalSpesificationDate_GenerateDate_TATWD.GenerateDate as 'Generate_Proc_Sum_Date',
                                   p_FinalSpesificationDate_GenerateDate_TATWD.TAT_WD as 'TAT_WD',
                                   (CASE
                                        WHEN p_FinalSpesificationDate_GenerateDate_TATWD.TAT_WD IS NULL THEN
                                            NULL
                                        ELSE
                                            5
                                    END
                                   ) as 'SLA_WD',
                                   (CASE
                                        WHEN p_FinalSpesificationDate_GenerateDate_TATWD.TAT_WD IS NULL THEN
                                            NULL
                                        WHEN p_FinalSpesificationDate_GenerateDate_TATWD.TAT_WD <= 5 THEN
                                            'Meet'
                                        ELSE
                                            'Not Meet'
                                    END
                                   ) as 'SLA_Status',
                                   (v.Code + ' - ' + v.Name) as 'Vendor',
                                   (CASE
                                        WHEN tp_sc.SubCategoryCode = 'SC-2023-08-11134'
                                             AND pvqd.IsSelected = 1 THEN
                                            'Yes'
                                        WHEN psd.IsSelected = 1 THEN
                                            'Yes'
                                        ELSE
                                            ''
                                    END
                                   ) as 'Selected',
                                   (CASE
                                        WHEN tp_sc.SubCategoryCode = 'SC-2023-08-11134' THEN
                                            pvq_group_pvqd.Qty_x_ItemPrice_x_RateAmmount_sum
                                        WHEN tp_sc.SubCategoryCode = 'SC-2023-08-11135' THEN
                                            p.GRAmount
                                        ELSE
                                            ps_group_psd.Qty_x_ItemPrice_x_RateAmmount_sum
                                    END
                                   ) as 'Total_Price',
                                   (CASE
                                        WHEN tp_sc.SubCategoryCode = 'SC-2023-08-11134' THEN
                                   (pvqd.Qty * pvqd.ItemPrice)
                                        ELSE
                                   (psd.Qty * psd.ItemPrice)
                                    END
                                   ) as 'Price_Per_Item',
                                   (CASE
                                        WHEN tp_sc.SubCategoryCode = 'SC-2023-08-11134' THEN
                                   (pvq_group_pvqd.Qty_x_ItemPrice_x_RateAmmount_sum + pvq_group_pvqd.group_pvqoc_OtherCostAmount_sum)
                                        ELSE
                                   (ps_group_psd.Qty_x_ItemPrice_x_RateAmmount_sum + ps_group_psd.group_psoc_OtherCostAmount_sum)
                                    END
                                   ) as 'Total_Price_Inc_Other_Cost',
                                   (CASE
                                        WHEN tp_sc.SubCategoryCode = 'SC-2023-08-11134' THEN
                                   ((pvqd.Qty * pvqd.ItemPrice) + pvqd_group_pvqoc.OtherCostAmount_sum)
                                        ELSE
                                   ((psd.Qty * psd.ItemPrice) + psd_group_psoc.OtherCostAmount_sum)
                                    END
                                   ) as 'Price_Per_Item_Inc_Other_Cost',
                                   (CASE
                                        WHEN tp_sc.SubCategoryCode = 'SC-2023-08-11134' THEN
                                   (CASE
                                        WHEN pvq_group_pvqd.PRFVendorQuotationId IS NULL THEN
                                            NULL
                                        WHEN p.TotalBudgetEstimation > (pvq_group_pvqd.Qty_x_ItemPrice_x_RateAmmount_sum
                                                                        + pvq_group_pvqd.group_pvqoc_OtherCostAmount_sum
                                                                       ) THEN
                                            p.TotalBudgetEstimation
                                            - (pvq_group_pvqd.Qty_x_ItemPrice_x_RateAmmount_sum + pvq_group_pvqd.group_pvqoc_OtherCostAmount_sum)
                                        ELSE
                                            0
                                    END
                                   )
                                        ELSE
                                   (CASE
                                        WHEN ps_group_psd.PRFSummaryId IS NULL THEN
                                            NULL
                                        WHEN p.TotalBudgetEstimation > (ps_group_psd.Qty_x_ItemPrice_x_RateAmmount_sum
                                                                        + ps_group_psd.group_psoc_OtherCostAmount_sum
                                                                       ) THEN
                                            p.TotalBudgetEstimation
                                            - (ps_group_psd.Qty_x_ItemPrice_x_RateAmmount_sum + ps_group_psd.group_psoc_OtherCostAmount_sum)
                                        ELSE
                                            0
                                    END
                                   )
                                    END
                                   ) as 'Realised_Saving',
                                   p.ReasonCancel as 'ReasonCancel'
                            FROM PRF as p
                                LEFT JOIN #w_p_FinalSpesificationDate_GenerateDate_TATWD as p_FinalSpesificationDate_GenerateDate_TATWD
                                    ON p_FinalSpesificationDate_GenerateDate_TATWD.PRFId = p.Id
                                LEFT JOIN #w_bt as bt
                                    ON bt.RefNumber = p.PRFNo
                                INNER JOIN SubCategory as cp_sc
                                    ON cp_sc.SubCategoryCode = 'SC-2024-02-01262'
                                       AND (
                                               @PR_Category_Id IS NULL
                                               OR cp_sc.Id = @PR_Category_Id
                                           )
                                LEFT JOIN SubCategory as tp_sc
                                    ON tp_sc.Id = p.TypeProcess_SubCategory
                                       AND (
                                               @Order_Type_Id IS NULL
                                               OR tp_sc.Id = @Order_Type_Id
                                           )
                                INNER JOIN Flips.UserAccount as ua
                                    ON ua.Id = p.RequestorAccountId
                                INNER JOIN CostCenter as ua_cc
                                    ON ua_cc.Id = ua.CostCenterId
                                       AND (
                                               @Department_Id IS NULL
                                               OR ua_cc.Id = @Department_Id
                                           )
                                LEFT JOIN Flips.UserAccount as b_ua
                                    ON b_ua.Id = p.BuyerAccountId
                                INNER JOIN MasterTable as mt
                                    ON mt.Category = 'PRF.Status'
                                       AND mt.ValueId = p.Status
                                       AND (
                                               @PR_Status_ValueId IS NULL
                                               OR mt.ValueId = @PR_Status_ValueId
                                           )
                                INNER JOIN AccountMaster as am
                                    ON am.AccountCode = p.BudgetCode
                                       AND (
                                               @Account_Code_Id IS NULL
                                               OR am.Id = @Account_Code_Id
                                           )
                                INNER JOIN CostCenter as cc
                                    ON cc.Id = p.CostCenterId
                                       AND (
                                               @Cost_Center_Id IS NULL
                                               OR cc.Id = @Cost_Center_Id
                                           )
                                INNER JOIN Spending_Category as s_c
                                    ON s_c.Id = p.Spending_Category
                                INNER JOIN #w_p_last_psc as last_psc
                                    ON last_psc.PRFId = p.id
                                INNER JOIN PRFSpendingCategory as psc
                                    ON psc.Id = last_psc.PRFSpendingCategoryId
                                INNER JOIN PRFDetail as pd
                                    ON pd.PRFId = p.Id
                                LEFT JOIN PRFVendorQuotation as pvq
                                    ON pvq.PRFId = p.Id
                                LEFT JOIN PRFVendorQuotationDetail as pvqd
                                    ON pvqd.PRFVendorQuotationId = pvq.Id
                                       AND pvqd.PRFDetailId = pd.Id
                                       AND pvqd.Status = 1
                                       AND pvqd.IsSelected = 1
                                LEFT JOIN Vendor as v
                                    ON v.Id = pvqd.VendorId
                                       AND (
                                               @Vendor_Id IS NULL
                                               OR v.Id = @Vendor_Id
                                           )
                                LEFT JOIN
                                (
                                    SELECT ps.PRFId,
                                           MAX(ps.Id) as LastPRFSummaryId
                                    FROM PRFSummary as ps
                                    GROUP BY ps.PRFId
                                ) as last_ps
                                    ON last_ps.PRFId = p.Id
                                LEFT JOIN PRFSummary as ps
                                    ON ps.Id = last_ps.LastPRFSummaryId
                                LEFT JOIN #w_ps_group_psd as ps_group_psd
                                    ON ps_group_psd.PRFSummaryId = ps.Id
                                LEFT JOIN #w_pvq_group_pvqd as pvq_group_pvqd
                                    ON pvq_group_pvqd.PRFVendorQuotationId = pvq.Id
                                LEFT JOIN PRFSummaryDetail as psd
                                    ON psd.PRFSummaryId = ps.Id
                                       AND psd.PRFVendorQuotationDetailId = pvqd.Id
                                       AND psd.IsSelected = 1
                                LEFT JOIN #w_psd_group_psoc as psd_group_psoc
                                    ON psd_group_psoc.PRFSummaryDetailId = psd.Id
                                LEFT JOIN #w_pvqd_group_pvqoc as pvqd_group_pvqoc
                                    ON pvqd_group_pvqoc.PRFVendorQuotationDetailId = pvqd.Id
                            WHERE 1 = 1
                                  AND (
                                          @PR_No IS NULL
                                          OR p.PRFNo LIKE @PR_No
                                      )
                                  AND (
                                          @PR_Date_Begin IS NULL
                                          OR p.RequestDate >= @PR_Date_Begin
                                      )
                                  AND (
                                          @PR_Date_End IS NULL
                                          OR p.RequestDate <= @PR_Date_End
                                      )


                            CREATE NONCLUSTERED INDEX IX_w_pr_Id ON #w_pr (_Id);
                            CREATE NONCLUSTERED INDEX IX_w_pr_PR_No ON #w_pr (PR_No);


                            /* 
                               --- #w_argm ---
                               Fungsi: Mengambil data Approver terakhir (berdasarkan Max Id) per ApprovalRequestId.
                            */
                            SELECT 1 as x,
                                   argm.ApprovalRequestId as _ApprovalRequestId,
                                   argm.LastUpdatedTime as 'Approver_Date',
                                   ua.Username as 'Approver_Name'
                            INTO #w_argm
                            FROM ApprovalRequestGroupMember as argm
                                INNER JOIN
                                (
                                    SELECT 1 as x,
                                           argm.ApprovalRequestId,
                                           MAX(argm.Id) as Id
                                    FROM ApprovalRequestGroupMember as argm
                                    WHERE argm.Status NOT IN ( 0, 1 )
                                    GROUP BY argm.ApprovalRequestId
                                ) as current_argm
                                    ON current_argm.Id = argm.Id
                                INNER JOIN Flips.UserAccount as ua
                                    ON ua.Id = argm.AccountId;

                            CREATE NONCLUSTERED INDEX IX_w_argm_ApprovalRequestId
                            ON #w_argm (_ApprovalRequestId);


                            /* 
                               --- #w_type_process ---
                               Fungsi: MAIN TABLE 2. Menggabungkan berbagai jenis Order/Proses (PO, PONonShopping, GL, Contract, PAP).
                               Berisi informasi status order, tanggal, nilai, dan approver.
                            */
                            SELECT 1 as x,
                                   cp_sc.Id as _CategoryProcess_SubCategoryId,
                                   cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode,
                                   cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName,
                                   tp_sc.Id as _TypeProcess_SubCategoryId,
                                   tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode,
                                   tp_sc.SubCategoryName as _TypeProcess_SubCategoryName,
                                   -- PurchaseOrder
                                   pod.PurchaseRequestItemDetailId as _PurchaseRequestItemDetailId,
                                   pod.Id as _PurchaseOrderDetailId,
                                   pot.Id as _PurchaseOrderTOPId,
                                   -- PONonShopping
                                   NULL as _PONonShoppingDetailId,
                                   NULL as _PONonShoppingTOPId,
                                   -- PONonShopping, GuaranteeLetter
                                   NULL as _PRFSummaryDetailId,
                                   -- Contract
                                   NULL as _PRFSummaryId,
                                   -- PAP
                                   NULL as _PRFVendorQuotationDetailId,
                                   --
                                   po.Id as _Id,
                                   pod.Id as _DetailId,
                                   tp_sc.SubCategoryName as 'Order_Type',
                                   po.PONumber as 'Order_No',
                                   mt.ShortDescription as 'Order_Status',
                                   po.PoDate as 'Order_Date',
                                   (pod.Qty * pod.ItemPrice) as 'Order_Grand_Total_Amount',
                                   po.ApproverDate as 'Approver_Date',
                                   ua.Username as 'Approver_Name'
                            INTO #w_type_process
                            FROM PurchaseOrder as po
                                INNER JOIN Flips.UserAccount as ua
                                    ON ua.Id = po.ApproverAccountId
                                INNER JOIN MasterTable as mt
                                    ON mt.Category = 'PurchaseOrder.Status'
                                       AND mt.ValueId = po.Status
                                       AND (
                                               @Order_Status_ValueId IS NULL
                                               OR mt.ValueId = @Order_Status_ValueId
                                           )
                                INNER JOIN PurchaseOrderTOP as pot
                                    ON pot.PurchaseOrderId = po.Id
                                INNER JOIN PurchaseOrderDetail as pod
                                    ON pod.PurchaseOrderId = po.Id
                                INNER JOIN SubCategory as cp_sc
                                    ON cp_sc.SubCategoryCode = 'SC-2024-02-01261'
                                INNER JOIN SubCategory as tp_sc
                                    ON tp_sc.SubCategoryCode = 'SC-2023-08-11132'
                                       AND (
                                               @Order_Type_Id IS NULL
                                               OR tp_sc.Id = @Order_Type_Id
                                           )
                            WHERE 1 = 1
                                  AND (
                                          @Order_No IS NULL
                                          OR po.PONumber like @Order_No
                                      )
                                  AND po.PoDate >= @Order_Date_Begin
                                  AND po.PoDate <= @Order_Date_End
                            UNION ALL
                            SELECT 1 as x,
                                   cp_sc.Id as _CategoryProcess_SubCategoryId,
                                   cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode,
                                   cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName,
                                   tp_sc.Id as _TypeProcess_SubCategoryId,
                                   tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode,
                                   tp_sc.SubCategoryName as _TypeProcess_SubCategoryName,
                                   -- PurchaseOrder
                                   NULL as _PurchaseRequestItemDetailId,
                                   NULL as _PurchaseOrderDetailId,
                                   NULL as _PurchaseOrderTOPId,
                                   -- PONonShopping
                                   ponsd.Id as _PONonShoppingDetailId,
                                   ponst.Id as _PONonShoppingTOPId,
                                   -- PONonShopping , GuaranteeLetter
                                   ponsd.PRFSummaryDetailId as _PRFSummaryDetailId,
                                   -- Contract
                                   NULL as _PRFSummaryId,
                                   -- PAP
                                   NULL as _PRFVendorQuotationDetailId,
                                   --
                                   pons.Id as _Id,
                                   ponsd.Id as _DetailId,
                                   tp_sc.SubCategoryName as 'Order_Type',
                                   pons.PONumber as 'Order_No',
                                   mt.ShortDescription as 'Order_Status',
                                   pons.PoDate as 'Order_Date',
                                   pons.TotalAmount as 'Order_Grand_Total_Amount',
                                   pons.ApproverDate as 'Approver_Date',
                                   ua.Username as 'Approver_Name'
                            FROM PONonShopping as pons
                                INNER JOIN Flips.UserAccount as ua
                                    ON ua.Id = pons.ApproverAccountId
                                INNER JOIN MasterTable as mt
                                    ON mt.Category = 'PurchaseOrder.Status'
                                       AND mt.ValueId = pons.Status
                                       AND (
                                               @Order_Status_ValueId IS NULL
                                               OR mt.ValueId = @Order_Status_ValueId
                                           )
                                INNER JOIN PONonShoppingTOP as ponst
                                    ON ponst.PONonShoppingId = pons.Id
                                INNER JOIN PONonShoppingDetail as ponsd
                                    ON ponsd.PONonShoppingId = pons.Id
                                INNER JOIN
                                (
                                    SELECT ponsioc.PONonShoppingDetailId,
                                           SUM(COALESCE(ponsioc.Amount, 0)) as Amount_sum
                                    FROM PONonShoppingItemOtherCost as ponsioc
                                    GROUP BY ponsioc.PONonShoppingDetailId
                                ) as ponsioc
                                    ON ponsioc.PONonShoppingDetailId = ponsd.Id
                                INNER JOIN SubCategory as cp_sc
                                    ON cp_sc.SubCategoryCode = 'SC-2024-02-01262'
                                INNER JOIN SubCategory as tp_sc
                                    ON tp_sc.SubCategoryCode = 'SC-2023-08-11132'
                                       AND (
                                               @Order_Type_Id IS NULL
                                               OR tp_sc.Id = @Order_Type_Id
                                           )
                            WHERE 1 = 1
                                  AND (
                                          @Order_No IS NULL
                                          OR pons.PONumber like @Order_No
                                      )
                                  AND pons.PoDate >= @Order_Date_Begin
                                  AND pons.PoDate <= @Order_Date_End
                            UNION ALL
                            SELECT 1 as x,
                                   cp_sc.Id as _CategoryProcess_SubCategoryId,
                                   cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode,
                                   cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName,
                                   tp_sc.Id as _TypeProcess_SubCategoryId,
                                   tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode,
                                   tp_sc.SubCategoryName as _TypeProcess_SubCategoryName,
                                   -- PurchaseOrder
                                   NULL as _PurchaseRequestItemDetailId,
                                   NULL as _PurchaseOrderDetailId,
                                   NULL as _PurchaseOrderTOPId,
                                   -- PONonShopping
                                   NULL as _PONonShoppingDetailId,
                                   NULL as _PONonShoppingTOPId,
                                   -- PONonShopping , GuaranteeLetter
                                   gld.PRFSummaryDetailId as _PRFSummaryDetailId,
                                   -- Contract
                                   NULL as _PRFSummaryId,
                                   -- PAP
                                   NULL as _PRFVendorQuotationDetailId,
                                   --
                                   gl.Id as _Id,
                                   gld.Id as _DetailId,
                                   tp_sc.SubCategoryName as 'Order_Type',
                                   gl.GLNumber as 'Order_No',
                                   mt.ShortDescription as 'Order_Status',
                                   gl.GLDate as 'Order_Date',
                                   (gld.Qty * gld.ItemPrice) as 'Order_Grand_Total_Amount',
                                   argm.Approver_Date as 'Approver_Date',
                                   argm.Approver_Name as 'Approver_Name'
                            FROM GuaranteeLetter as gl
                                INNER JOIN MasterTable as mt
                                    ON mt.Category = 'GuaranteeLetter.Status'
                                       AND mt.ValueId = gl.Status
                                       AND (
                                               @Order_Status_ValueId IS NULL
                                               OR mt.ValueId = @Order_Status_ValueId
                                           )
                                INNER JOIN GuaranteeLetterDetail as gld
                                    ON gld.GuaranteeLetterId = gl.Id
                                INNER JOIN SubCategory as cp_sc
                                    ON cp_sc.SubCategoryCode = 'SC-2024-02-01262'
                                INNER JOIN SubCategory as tp_sc
                                    ON tp_sc.SubCategoryCode = 'SC-2023-08-11133'
                                       AND (
                                               @Order_Type_Id IS NULL
                                               OR tp_sc.Id = @Order_Type_Id
                                           )
                                LEFT JOIN #w_argm as argm
                                    ON argm._ApprovalRequestId = gl.ApprovalRequestId
                            WHERE 1 = 1
                                  AND (
                                          @Order_No IS NULL
                                          OR gl.GLNumber like @Order_No
                                      )
                                  AND gl.GLDate >= @Order_Date_Begin
                                  AND gl.GLDate <= @Order_Date_End
                            UNION ALL
                            SELECT 1 as x,
                                   cp_sc.Id as _CategoryProcess_SubCategoryId,
                                   cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode,
                                   cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName,
                                   tp_sc.Id as _TypeProcess_SubCategoryId,
                                   tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode,
                                   tp_sc.SubCategoryName as _TypeProcess_SubCategoryName,
                                   -- PurchaseOrder
                                   NULL as _PurchaseRequestItemDetailId,
                                   NULL as _PurchaseOrderDetailId,
                                   NULL as _PurchaseOrderTOPId,
                                   -- PONonShopping
                                   NULL as _PONonShoppingDetailId,
                                   NULL as _PONonShoppingTOPId,
                                   -- PONonShopping , GuaranteeLetter
                                   NULL as _PRFSummaryDetailId,
                                   -- Contract
                                   c.PRFSummaryId as _PRFSummaryId,
                                   -- PAP
                                   NULL as _PRFVendorQuotationDetailId,
                                   --
                                   c.Id as _Id,
                                   NULL as _DetailId,
                                   tp_sc.SubCategoryName as 'Order_Type',
                                   c.ContractNo as 'Order_No',
                                   mt.ShortDescription as 'Order_Status',
                                   c.UploadFinalDate as 'Order_Date',
                                   c.AmountContract as 'Order_Grand_Total_Amount',
                                   NULL as 'Approver_Date',
                                   NULL as 'Approver_Name'
                            FROM Contract as c
                                INNER JOIN MasterTable as mt
                                    ON mt.Category = 'Contract.Status'
                                       AND mt.ValueId = c.Status
                                       AND (
                                               @Order_Status_ValueId IS NULL
                                               OR mt.ValueId = @Order_Status_ValueId
                                           )
                                INNER JOIN SubCategory as cp_sc
                                    ON cp_sc.SubCategoryCode = 'SC-2024-02-01262'
                                INNER JOIN SubCategory as tp_sc
                                    ON tp_sc.SubCategoryCode = 'SC-2023-08-11131'
                                       AND (
                                               @Order_Type_Id IS NULL
                                               OR tp_sc.Id = @Order_Type_Id
                                           )
                            WHERE 1 = 1
                                  AND (
                                          @Order_No IS NULL
                                          OR c.ContractNo = @Order_No
                                      )
                                  AND c.UploadFinalDate >= @Order_Date_Begin
                                  AND c.UploadFinalDate <= @Order_Date_End
                            UNION ALL
                            SELECT 1 as x,
                                   cp_sc.Id as _CategoryProcess_SubCategoryId,
                                   cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode,
                                   cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName,
                                   tp_sc.Id as _TypeProcess_SubCategoryId,
                                   tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode,
                                   tp_sc.SubCategoryName as _TypeProcess_SubCategoryName,
                                   -- PurchaseOrder
                                   NULL as _PurchaseRequestItemDetailId,
                                   NULL as _PurchaseOrderDetailId,
                                   NULL as _PurchaseOrderTOPId,
                                   -- PONonShopping
                                   NULL as _PONonShoppingDetailId,
                                   NULL as _PONonShoppingTOPId,
                                   -- PONonShopping , GuaranteeLetter
                                   NULL as _PRFSummaryDetailId,
                                   -- Contract
                                   NULL as _PRFSummaryId,
                                   -- PAP
                                   papd.PRFVendorQuotationDetailId as _PRFVendorQuotationDetailId,
                                   --
                                   pap.Id as _Id,
                                   papd.Id as _DetailId,
                                   tp_sc.SubCategoryName as 'Order_Type',
                                   pap.PAPNo as 'Order_No',
                                   mt.ShortDescription as 'Order_Status',
                                   pap.GenerateDate as 'Order_Date',
                                   papd.TotalBaseAmount as 'Order_Grand_Total_Amount',
                                   argm.Approver_Date as 'Approver_Date',
                                   argm.Approver_Name as 'Approver_Name'
                            FROM PAP as pap
                                INNER JOIN SubCategory as cp_sc
                                    ON cp_sc.SubCategoryCode = 'SC-2024-02-01262'
                                INNER JOIN SubCategory as tp_sc
                                    ON tp_sc.SubCategoryCode = 'SC-2023-08-11134'
                                       AND (
                                               @Order_Type_Id IS NULL
                                               OR tp_sc.Id = @Order_Type_Id
                                           )
                                INNER JOIN MasterTable as mt
                                    ON mt.Category = 'PRFSummary.Status'
                                       AND mt.ValueId = pap.Status
                                       AND (
                                               @Order_Status_ValueId IS NULL
                                               OR mt.ValueId = @Order_Status_ValueId
                                           )
                                LEFT JOIN #w_argm as argm
                                    ON argm._ApprovalRequestId = pap.ApprovalRequestId
                                INNER JOIN PAPDetail as papd
                                    ON papd.PAPId = pap.Id
                            WHERE 1 = 1
                                  AND (
                                          @Order_No IS NULL
                                          OR pap.PAPNo = @Order_No
                                      )
                                  AND pap.GenerateDate >= @Order_Date_Begin
                                  AND pap.GenerateDate <= @Order_Date_End

                            CREATE NONCLUSTERED INDEX IX_w_type_process_Id ON #w_type_process (_Id);
                            CREATE NONCLUSTERED INDEX IX_w_type_process_Order_No
                            ON #w_type_process (Order_No);


                            /* 
                               --- #w_dn ---
                               Fungsi: Mengambil data Delivery Notes (Surat Jalan) beserta detail dan statusnya.
                            */
                            SELECT 1 as x,
                                   cp_sc.Id as _CategoryProcess_SubCategoryId,
                                   cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode,
                                   cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName,
                                   dnd.PurchaseOrderDetailId as _PurchaseOrderDetailId,
                                   dnp.PurchaseOrderTOPId as _PurchaseOrderTOPId,
                                   dn.Id as _Id,
                                   dnd.Id as _DetailId,
                                   dn.DeliveryNumber as 'DN_No',
                                   mt.Name as 'DN_Status',
                                   dnd.ReceivedDate as 'DN_Date',
                                   dnd.QtyReceive as 'DN_Qty'
                            INTO #w_dn
                            FROM DeliveryNotesDetail as dnd
                                INNER JOIN MasterTable as mt
                                    ON mt.Category = 'DeliveryNote.Status'
                                       AND mt.ValueId = dnd.Status
                                INNER JOIN DeliveryNotesPayment as dnp
                                    ON dnp.Id = dnd.DeliveryNotesPaymentId
                                INNER JOIN DeliveryNotes as dn
                                    ON dn.Id = dnp.DeliveryNotesId
                                INNER JOIN SubCategory as cp_sc
                                    ON cp_sc.Id = dn.CategoryProcess_SubCategoryId;

                            CREATE NONCLUSTERED INDEX IX_w_dn_PurchaseOrderDetailId
                            ON #w_dn (_PurchaseOrderDetailId);
                            CREATE NONCLUSTERED INDEX IX_w_dn_Id ON #w_dn (_Id);


                            /* 
                               --- #w_ipo ---
                               Fungsi: Mengambil data Invoice Purchase Order beserta detail pajak (PPN, PPh).
                            */
                            SELECT 1 as x,
                                   cp_sc.Id as _CategoryProcess_SubCategoryId,
                                   cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode,
                                   cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName,
                                   ipo.PurchaseOrderTOPId as _PurchaseOrderTOPId,
                                   ipo.Id as _InvoicePOId,
                                   ipod.Id as _InvoicePODetailId,
                                   ipod.PODetailId as _PODetailId,
                                   ipo.InvoiceNumber as 'Invoice_No',
                                   mt.Name as 'Invoice_Status',
                                   ipo.InvoiceDate as 'Invoice_Date',
                                   ipo.InvoiceAmmount as 'Invoice_Amount',
                                   ipoioc.PPN as 'PPn',
                                   ipoioc.PPH23 as 'PPh_23',
                                   ipoioc.PPH42 as 'PPh_42',
                                   ipo.Remark as 'Remarks',
                                   (ipo.InvoiceAmmount + (SUM(ipoioc.PPN + ipoioc.PPH23 + ipoioc.PPH42) OVER (PARTITION BY ipo.Id))) as 'Invoice_After_Tax_Or_Grand_Total'
                            INTO #w_ipo
                            FROM InvoicePO as ipo
                                INNER JOIN SubCategory as cp_sc
                                    ON cp_sc.Id = ipo.CategoryProcess_SubCategoryId
                                INNER JOIN MasterTable as mt
                                    ON mt.Category = 'InvoiceManagement.Status'
                                       AND mt.ValueId = ipo.Status
                                INNER JOIN InvoicePODetail as ipod
                                    ON ipod.InvoicePOId = ipo.Id
                                INNER JOIN
                                (
                                    SELECT p.InvoicePODetailId,
                                           COALESCE(p.PPN, 0) as 'PPN',
                                           COALESCE(p.PPH23, 0) as 'PPH23',
                                           COALESCE(p.PPH42, 0) as 'PPH42'
                                    FROM
                                    (
                                        SELECT ipoioc.InvoicePODetailId,
                                               (CASE sc.SubCategoryCode
                                                    WHEN 'SC-2023-10-01228' THEN
                                                        'PPN'
                                                    WHEN 'SC-2023-10-01230' THEN
                                                        'PPH23'
                                                    WHEN 'SC-2023-10-01229' THEN
                                                        'PPH42'
                                                    ELSE
                                                        sc.SubCategoryCode
                                                END
                                               ) as 'Pivoted',
                                               ipoioc.Amount
                                        FROM InvoicePOItemOtherCost as ipoioc
                                            INNER JOIN SubCategory as sc
                                                ON sc.SubCategoryCode IN ( 'PPN', 'PPH23', 'PPH42', 'SC-2023-10-01230', 'SC-2023-10-01229',
                                                                           'SC-2023-10-01228'
                                                                         )
                                                   AND sc.id = ipoioc.OtherCost_SubCategoryId
                                    ) as s
                                    PIVOT
                                    (
                                        MAX(s.Amount)
                                        FOR Pivoted IN ([PPN], [PPH23], [PPH42])
                                    ) as p
                                ) as ipoioc
                                    ON ipoioc.InvoicePODetailId = ipod.Id;

                            -- ============================================================================
                            -- OPTIMASI TEMP TABLE SEBELUM QUERY UTAMA
                            -- ============================================================================

                            -- Update statistics agar query planner punya info distribusi data terbaru
                            /* --- TAMBAHKAN BLOK INI SEBELUM FINAL SELECT --- */
                            UPDATE STATISTICS #w_pr
                            WITH FULLSCAN;
                            UPDATE STATISTICS #w_type_process
                            WITH FULLSCAN;
                            UPDATE STATISTICS #w_dn
                            WITH FULLSCAN;
                            UPDATE STATISTICS #w_ipo
                            WITH FULLSCAN;

                            -- Tambah index khusus untuk query JOIN & WHERE ini
                            CREATE NONCLUSTERED INDEX IX_w_pr_PR_Date
                            ON #w_pr (PR_Date)
                            INCLUDE
                            (
                                PR_No,
                                _Id,
                                _DetailId
                            );
                            CREATE NONCLUSTERED INDEX IX_w_type_process_Order_Date
                            ON #w_type_process (Order_Date)
                            INCLUDE
                            (
                                Order_No,
                                _Id
                            );
                            CREATE NONCLUSTERED INDEX IX_w_type_process_JoinKeys
                            ON #w_type_process
                            (
                                _CategoryProcess_SubCategoryId,
                                _TypeProcess_SubCategoryId
                            );
                            CREATE NONCLUSTERED INDEX IX_w_dn_JoinKeys
                            ON #w_dn
                            (
                                _CategoryProcess_SubCategoryId,
                                _PurchaseOrderTOPId,
                                _PurchaseOrderDetailId
                            );
                            CREATE NONCLUSTERED INDEX IX_w_ipo_JoinKeys
                            ON #w_ipo
                            (
                                _CategoryProcess_SubCategoryId,
                                _PurchaseOrderTOPId,
                                _PODetailId
                            );
                            CREATE NONCLUSTERED INDEX IX_w_ipo_PurchaseOrderTOPId
                            ON #w_ipo (_PurchaseOrderTOPId);
                            CREATE NONCLUSTERED INDEX IX_w_ipo_InvoicePOId ON #w_ipo (_InvoicePOId);

                            /* --- INDEX KHUSUS UNTUK QUERY FINAL --- */
                            -- Index untuk #w_pr (Covering Index)
                            CREATE NONCLUSTERED INDEX IX_w_pr_Optimized
                            ON #w_pr
                            (
                                PR_Date,
                                _CategoryProcess_SubCategoryId,
                                _TypeProcess_SubCategoryId,
                                _Id
                            )
                            INCLUDE
                            (
                                PR_No,
                                PR_Status,
                                Requester,
                                Department,
                                Total_Budget_Estimation,
                                Total_Price,
                                Total_Price_Inc_Other_Cost,
                                _TypeProcess_SubCategoryCode,
                                _PurchaseRequestItemDetailId,
                                _PRFSummaryDetailId,
                                _PRFSummaryId,
                                _PRFVendorQuotationDetailId
                            );

                            -- Index untuk #w_type_process (Covering Index)
                            CREATE NONCLUSTERED INDEX IX_w_type_process_Optimized
                            ON #w_type_process
                            (
                                Order_Date,
                                _CategoryProcess_SubCategoryId,
                                _TypeProcess_SubCategoryId
                            )
                            INCLUDE
                            (
                                Order_No,
                                Order_Status,
                                Order_Grand_Total_Amount,
                                Approver_Date,
                                _PurchaseOrderTOPId,
                                _PurchaseOrderDetailId,
                                _PONonShoppingTOPId,
                                _PONonShoppingDetailId,
                                _DetailId,
                                _TypeProcess_SubCategoryCode
                            );

                            -- Hitung total count terpisah (tidak memaksa scan semua row saat pagination)
                            DECLARE @TotalCount INT;
                            SELECT @TotalCount = COUNT(*)
                            FROM #w_pr AS pr
                                LEFT JOIN #w_type_process AS tp
                                    ON tp._CategoryProcess_SubCategoryId = pr._CategoryProcess_SubCategoryId
                                       AND tp._TypeProcess_SubCategoryId = pr._TypeProcess_SubCategoryId
                                       AND (
                                           (pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' AND pr._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261' AND tp._PurchaseRequestItemDetailId = pr._PurchaseRequestItemDetailId)
                                           OR (pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' AND pr._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262' AND tp._PRFSummaryDetailId = pr._PRFSummaryDetailId)
                                           OR (pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11133' AND tp._PRFSummaryDetailId = pr._PRFSummaryDetailId)
                                           OR (pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11131' AND tp._PRFSummaryId = pr._PRFSummaryId)
                                           OR (pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11134' AND tp._PRFVendorQuotationDetailId = pr._PRFVendorQuotationDetailId)
                                       )
                            WHERE pr.PR_Date >= '2024-01-01'
                  

                            DECLARE @EffectiveLength INT;
                            IF @Length IS NOT NULL AND @Length > 0
                                SET @EffectiveLength = @Length;
                            ELSE
                                -- Jika 0/NULL, ambil tepat sebanyak data yang ada. 
                                -- Fallback ke 1 agar FETCH NEXT tidak error (harus >= 1)
                                SET @EffectiveLength = CASE WHEN @TotalCount > 0 THEN @TotalCount ELSE 1 END;

                            SELECT 1 as x,
                                   @TotalCount AS 'Count',
                                   x.PR_No,
                                   x.PR_Status,
                                   x.PR_Date,
                                   x.Requester,
                                   x.Department,
                                   x.Type_Of_Transaction,
                                   x.Buyer_User_Name,
                                   (CASE
                                        WHEN x.PR_No_partition_row_number <> 1 THEN
                                            NULL
                                        ELSE
                                            x.Total_Budget_Estimation
                                    END
                                   ) AS Total_Budget_Estimation,
                                   x.Critical,
                                   x.DPIA,
                                   x.VSDDT,
                                   x.Outsourcing_Status,
                                   x.Category,
                                   x.Item_Name,
                                   x.Account_Code,
                                   x.Cost_Center,
                                   x.Vendor_Selection,
                                   x.Currency,
                                   x.PR_Posted_Date,
                                   x.Delivery_Request_Date,
                                   x.Final_Spec_Req_Date,
                                   x.Generate_Proc_Sum_Date,
                                   x.TAT_WD,
                                   x.SLA_WD,
                                   x.SLA_Status,
                                   x.Vendor,
                                   x.Selected,
                                   (CASE
                                        WHEN x._pr_partition_row_number <> 1 THEN
                                            NULL
                                        ELSE
                                            x.Total_Price
                                    END
                                   ) AS Total_Price,
                                   x.Price_Per_Item,
                                   (CASE
                                        WHEN x._pr_partition_row_number <> 1 THEN
                                            NULL
                                        ELSE
                                            x.Total_Price_Inc_Other_Cost
                                    END
                                   ) AS Total_Price_Inc_Other_Cost,
                                   x.Price_Per_Item_Inc_Other_Cost,
                                   (CASE
                                        WHEN x._pr_partition_row_number <> 1 THEN
                                            NULL
                                        ELSE
                                            x.Realised_Saving
                                    END
                                   ) AS Realised_Saving,
                                   x.Order_Type,
                                   x.Order_No,
                                   x.Order_Status,
                                   x.Order_Date,
                                   (CASE
                                        WHEN x.Order_No_partition_row_number <> 1 THEN
                                            NULL
                                        ELSE
                                            x.Order_Grand_Total_Amount
                                    END
                                   ) AS Order_Grand_Total_Amount,
                                   x.Approver_Date,
                                   x.Approver_Name,
                                   x.DN_No,
                                   x.DN_Status,
                                   x.DN_Date,
                                   x.DN_Qty,
                                   x.Invoice_No,
                                   x.Invoice_Status,
                                   x.Invoice_Date,
                                   (CASE
                                        WHEN x.Order_No_partition_row_number <> 1 THEN
                                            NULL
                                        ELSE
                                            x.Invoice_Amount
                                    END
                                   ) AS Invoice_Amount,
                                   x.PPn,
                                   x.PPh_23,
                                   x.PPh_42,
                                   (CASE
                                        WHEN x.Order_No_partition_row_number <> 1 THEN
                                            NULL
                                        ELSE
                                            x.Invoice_After_Tax_Or_Grand_Total
                                    END
                                   ) AS Invoice_After_Tax_Or_Grand_Total,
                                   x.Remarks,
                                   x.ReasonCancel
                            FROM
                            (
                                SELECT 1 as x,
                                       -- HAPUS: COUNT(*) OVER () AS 'Count'
                                       -- HAPUS: ROW_NUMBER() OVER (PARTITION BY pr.PR_No, dn._Id ORDER BY pr.PR_No) AS PR_No_DN_Id_partition_row_number
                                       -- HAPUS: ROW_NUMBER() OVER (PARTITION BY pr._CategoryProcess_SubCategoryId, pr._TypeProcess_SubCategoryId, pr._Id, pr._DetailId ...) AS _pr_detail_partition_row_number

                                       -- PERTAHANKAN yang DIPAKAI saja:
                                       ROW_NUMBER() OVER (PARTITION BY pr.PR_No ORDER BY pr.PR_No) AS PR_No_partition_row_number,
                                       ROW_NUMBER() OVER (PARTITION BY tp.Order_No ORDER BY tp.Order_No) AS Order_No_partition_row_number,
                                       ROW_NUMBER() OVER (PARTITION BY pr._CategoryProcess_SubCategoryId,
                                                                       pr._TypeProcess_SubCategoryId,
                                                                       pr._Id
                                                          ORDER BY pr._Id
                                                         ) AS _pr_partition_row_number,
                                       pr.PR_No,
                                       pr.PR_Status,
                                       pr.PR_Date,
                                       pr.Requester,
                                       pr.Department,
                                       pr.Type_Of_Transaction,
                                       pr.Buyer_User_Name,
                                       pr.Total_Budget_Estimation,
                                       pr.Critical,
                                       pr.DPIA,
                                       pr.VSDDT,
                                       pr.Outsourcing_Status,
                                       pr.Category,
                                       pr.Item_Name,
                                       pr.Account_Code,
                                       pr.Cost_Center,
                                       pr.Vendor_Selection,
                                       pr.Currency,
                                       pr.PR_Posted_Date,
                                       pr.Delivery_Request_Date,
                                       pr.Final_Spec_Req_Date,
                                       pr.Generate_Proc_Sum_Date,
                                       pr.TAT_WD,
                                       pr.SLA_WD,
                                       pr.SLA_Status,
                                       pr.Vendor,
                                       pr.Selected,
                                       pr.Total_Price,
                                       pr.Price_Per_Item,
                                       pr.Total_Price_Inc_Other_Cost,
                                       pr.Price_Per_Item_Inc_Other_Cost,
                                       pr.Realised_Saving,
                                       pr._TypeProcess_SubCategoryName AS Order_Type,
                                       tp.Order_No,
                                       tp.Order_Status,
                                       tp.Order_Date,
                                       tp.Order_Grand_Total_Amount,
                                       tp.Approver_Date,
                                       tp.Approver_Name,
                                       dn.DN_No,
                                       dn.DN_Status,
                                       dn.DN_Date,
                                       dn.DN_Qty,
                                       ipo.Invoice_No,
                                       ipo.Invoice_Status,
                                       ipo.Invoice_Date,
                                       ipo.Invoice_Amount,
                                       ipo.PPn,
                                       ipo.PPh_23,
                                       ipo.PPh_42,
                                       ipo.Invoice_After_Tax_Or_Grand_Total,
                                       ipo.Remarks,
                                       pr.ReasonCancel
                                FROM #w_pr AS pr
                                    LEFT JOIN #w_type_process AS tp
                                        ON tp._CategoryProcess_SubCategoryId = pr._CategoryProcess_SubCategoryId
                                           AND tp._TypeProcess_SubCategoryId = pr._TypeProcess_SubCategoryId
                                           AND (
                                                   (
                                                       pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11132'
                                                       AND pr._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261'
                                                       AND tp._PurchaseRequestItemDetailId = pr._PurchaseRequestItemDetailId
                                                   )
                                                   OR (
                                                          pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11132'
                                                          AND pr._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262'
                                                          AND tp._PRFSummaryDetailId = pr._PRFSummaryDetailId
                                                      )
                                                   OR (
                                                          pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11133'
                                                          AND tp._PRFSummaryDetailId = pr._PRFSummaryDetailId
                                                      )
                                                   OR (
                                                          pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11131'
                                                          AND tp._PRFSummaryId = pr._PRFSummaryId
                                                      )
                                                   OR (
                                                          pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11134'
                                                          AND tp._PRFVendorQuotationDetailId = pr._PRFVendorQuotationDetailId
                                                      )
                                               )
                                    LEFT JOIN #w_dn AS dn
                                        ON dn._CategoryProcess_SubCategoryId = tp._CategoryProcess_SubCategoryId
                                           AND (
                                                   (
                                                       tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132'
                                                       AND tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261'
                                                       AND dn._PurchaseOrderTOPId = tp._PurchaseOrderTOPId
                                                       AND dn._PurchaseOrderDetailId = tp._PurchaseOrderDetailId
                                                   )
                                                   OR (
                                                          tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132'
                                                          AND tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262'
                                                          AND dn._PurchaseOrderTOPId = tp._PONonShoppingTOPId
                                                          AND dn._PurchaseOrderDetailId = tp._PONonShoppingDetailId
                                                      )
                                               )
                                    LEFT JOIN #w_ipo AS ipo
                                        ON ipo._CategoryProcess_SubCategoryId = tp._CategoryProcess_SubCategoryId
                                           AND (
                                                   (
                                                       tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132'
                                                       AND tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261'
                                                       AND ipo._PurchaseOrderTOPId = tp._PurchaseOrderTOPId
                                                       AND ipo._PODetailId = tp._DetailId
                                                   )
                                                   OR (
                                                          tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132'
                                                          AND tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262'
                                                          AND ipo._PurchaseOrderTOPId = tp._PONonShoppingTOPId
                                                          AND ipo._PODetailId = tp._DetailId
                                                      )
                                               )
                                WHERE pr.PR_Date >= '2024-01-01'
                                      AND 1=1 AND 1=1 AND 1=1
                            ) AS x
                            ORDER BY x.PR_Date DESC, x.Order_Date DESC, x.Order_No ASC OFFSET @Start ROWS FETCH NEXT @EffectiveLength ROWS ONLY
                            OPTION (RECOMPILE);
