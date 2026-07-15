using System;

namespace APS_REST_API.Queries.NonShoppingCart
{
    public static class ProcurmentBuyerQuery
    {
        public static string SelectPRFVendorQuotationDetail { get => $@"
			SELECT 
				VQD.*,
				V.Name [VendorName],
				V.Id [VendorId]
			FROM 
				PRFVendorQuotationDetail VQD
			INNER JOIN 
				PRFVendorQuotation VQ ON VQ.Id = VQD.PRFVendorQuotationId
			INNER JOIN 
				PRF PRF ON PRF.Id = VQ.PRFId
			INNER JOIN 
				Vendor V ON V.id = VQD.VendorId
			WHERE 
				PRF.Id = @PRFId
				AND VQD.IsSelected = 1
				AND VQD.Status = 1;
		"; }
        public static string SelectApprovalFlow { get => @"
			SELECT
				SC.Id,
				SC.SubCategoryCode,
				SC.SubCategoryName,
				AFD.Sequence,
				AFD.Notes
			FROM
				ApprovalFlow AF
				JOIN ApprovalFlowDetail AFD ON AFD.ApprovalFlowID = AF.Id
				JOIN SubCategory SC ON SC.Id = AFD.ApprovalGroup_SubCategoryId
			WHERE
				AF.Name = @Name
				AND AF.Status = 1
			ORDER BY
				AFD.Sequence ASC;
		"; }
        public static string SelectApprovalGroup { get => @"
			SELECT
				AG.CostCenterId AS CostCenterId,
				CC.Name AS CostCenterName,
				CC.BusinessUnitId AS BusinessUnitId,
				BU.Name AS BusinessUnitName,
				AG.Level AS Level,
				AG.UserName AS Username,
				AG.AccountId AS AccountId,
				AG.Sequence AS Sequence
			FROM
				ApprovalGroup AG
				JOIN CostCenter CC ON CC.Id = AG.CostCenterId
				JOIN BusinessUnit BU ON BU.Id = CC.BusinessUnitId
			WHERE
				AG.ApprovalGroup_SubCategoryId = @ApprovalGroup_SubCategoryId
				AND AG.STATUS = 1
			ORDER BY
				AG.Level ASC,
				AG.Sequence ASC;
		"; }
        public static string SelectApprovalGroupWithPRFNumber { get => @"
			SELECT
				AG.CostCenterId AS CostCenterId,
				CC.Name AS CostCenterName,
				CC.BusinessUnitId AS BusinessUnitId,
				BU.Name AS BusinessUnitName,
				AG.Level AS Level,
				AG.UserName AS Username,
				AG.AccountId AS AccountId,
				AG.Sequence AS Sequence
			FROM
				ApprovalGroup AG
				JOIN CostCenter CC ON CC.Id = AG.CostCenterId
				JOIN BusinessUnit BU ON BU.Id = CC.BusinessUnitId
				JOIN PRF PRF ON PRF.CostCenterId = AG.CostCenterId
			WHERE
				AG.ApprovalGroup_SubCategoryId = @ApprovalGroup_SubCategoryId
				AND PRF.PRFNo = @PRFNumber
				AND AG.Status = 1
			ORDER BY
				AG.Level ASC,
				AG.Sequence ASC;
		"; }
        public static string SelectApprovalGroupBuyer { get => @"
			SELECT
				PRF.CostCenterId AS CostCenterId,
				CC.Name AS CostCenterName,
				CC.BusinessUnitId AS BusinessUnitId,
				BU.Name AS BusinessUnitName,
				1 AS Level,
				PRF.BuyerUserName AS Username,
				PRF.BuyerAccountId AS AccountId,
				1 AS Sequence
			FROM 
				PRF PRF
				JOIN CostCenter CC ON CC.Id = PRF.CostCenterId
				JOIN BusinessUnit BU ON BU.Id = CC.BusinessUnitId
			WHERE
				PRF.PRFNo = @PRFNumber;
		"; }
		public static string SelectApprovalGroupDAP { get => @"
			SELECT
				AG.CostCenterId AS CostCenterId,
				CC.Name AS CostCenterName,
				CC.BusinessUnitId AS BusinessUnitId,
				BU.Name AS BusinessUnitName,
				AG.Level AS Level,
				AG.UserName AS Username,
				AG.AccountId AS AccountId,
				AG.Sequence AS Sequence
			FROM ApprovalGroup AG
			JOIN CostCenter CC 
				ON CC.Id = AG.CostCenterId
			JOIN BusinessUnit BU 
				ON BU.Id = CC.BusinessUnitId
			JOIN PRF PRF
				ON PRF.CostCenterId = AG.CostCenterId
			OUTER APPLY (
				SELECT SUM(PVQCC.TotalAmmount * BT.RateAmount) AS GrandTotalAmount
				FROM PRFVendorQuotation PVQ 
				JOIN PRFVendorQuotationDetail PVQD 
					ON PVQD.PRFVendorQuotationId = PVQ.Id 
					AND PVQD.IsSelected = 1 
					AND PVQD.Status != 0
				JOIN PRFVendorQuotationCostCenter PVQCC 
					ON PVQCC.PRFVendorQuotationDetailId = PVQD.Id
				OUTER APPLY (
					SELECT TOP 1 BT.RateAmount
					FROM BudgetTransaction BT
					WHERE BT.RefNumber = PRF.PRFNo
				) AS BT
				WHERE PVQ.PRFId = PRF.Id
			) AS Amount
			WHERE AG.ApprovalGroup_SubCategoryId = @ApprovalGroup_SubCategoryId
				AND AG.Status = 1
				AND Amount.GrandTotalAmount >= AG.MinAmount
				AND PRF.PRFNo = @PRFNumber
			ORDER BY
				AG.Level ASC,
				AG.Sequence ASC;
		"; }
		public static string SelectApprovalGroupRequestor { get => @"
			SELECT
				PRF.CostCenterId AS CostCenterId,
				CC.Name AS CostCenterName,
				CC.BusinessUnitId AS BusinessUnitId,
				BU.Name AS BusinessUnitName,
				1 AS Level,
				PRF.RequestorUserName AS Username,
				PRF.RequestorAccountId AS AccountId,
				1 AS Sequence
			FROM PRF PRF
			JOIN CostCenter CC ON CC.Id = PRF.CostCenterId
			JOIN BusinessUnit BU ON BU.Id = CC.BusinessUnitId
			WHERE PRF.PRFNo = @PRFNumber
		"; }
		public static string SelectApprovalGroupWithMinimumAmmount { get => @"
			SELECT
				AG.CostCenterId AS CostCenterId,
				CC.Name AS CostCenterName,
				CC.BusinessUnitId AS BusinessUnitId,
				BU.Name AS BusinessUnitName,
				AG.Level AS Level,
				AG.UserName AS Username,
				AG.AccountId AS AccountId,
				AG.Sequence AS Sequence
			FROM ApprovalGroup AG
			JOIN CostCenter CC 
				ON CC.Id = AG.CostCenterId
			JOIN BusinessUnit BU 
				ON BU.Id = CC.BusinessUnitId
			OUTER APPLY (
				SELECT SUM(PVQCC.TotalAmmount * BT.RateAmount) AS GrandTotalAmount
				FROM PRF PRF
				JOIN PRFVendorQuotation PVQ 
					ON PVQ.PRFId = PRF.Id
				JOIN PRFVendorQuotationDetail PVQD 
					ON PVQD.PRFVendorQuotationId = PVQ.Id 
					AND PVQD.IsSelected = 1 
					AND PVQD.Status != 0
				JOIN PRFVendorQuotationCostCenter PVQCC 
					ON PVQCC.PRFVendorQuotationDetailId = PVQD.Id
				OUTER APPLY (
					SELECT TOP 1 BT.RateAmount
					FROM BudgetTransaction BT
					WHERE BT.RefNumber = PRF.PRFNo
				) AS BT
				WHERE PRF.PRFNo = @PRFNumber
			) AS Amount
			WHERE AG.ApprovalGroup_SubCategoryId = @ApprovalGroup_SubCategoryId
				AND AG.Status = 1
				AND Amount.GrandTotalAmount >= AG.MinAmount
			ORDER BY
				AG.Level ASC,
				AG.Sequence ASC;
		"; }
		public static string GetValidationPRF { get => @"
			SELECT 
				SC.SubCategoryCode,
				PRF.isRiskAssementForm,
				PRF.IsBudgetedSpend,
				PRFVQD.GrandTotal,
				(CASE 
					WHEN PRFVQD.VendorCount > 0 THEN 0
					ELSE 1
				END) VendorStatus,
				PRFVQ.Id
			FROM 
				PRF PRF
			JOIN 
				PRFVendorQuotation PRFVQ 
				ON PRFVQ.PRFId = PRF.Id
			OUTER APPLY (
				SELECT 
					SUM(
						PVQCC1.TotalAmmount * 
						(CASE 
							WHEN PRFVQD1.RateAmmount = 0 THEN 1 
							ELSE PRFVQD1.RateAmmount 
						END)
					) AS GrandTotal,
					COUNT(V1.Id) VendorCount
				FROM PRFVendorQuotationDetail PRFVQD1
					JOIN PRFVendorQuotationCostCenter PVQCC1
						ON PVQCC1.PRFVendorQuotationDetailId = PRFVQD1.Id
						AND PRFVQD1.Status = 1
					LEFT JOIN Vendor V1
						ON V1.Id = PRFVQD1.VendorId
						AND V1.Status = 0
				WHERE PRFVQD1.PRFVendorQuotationId = PRFVQ.Id
			) PRFVQD
			JOIN 
				SubCategory SC 
				ON SC.Id = PRF.TypeProcess_SubCategory
			WHERE 
				PRF.PRFNo = @PRFNumber
		"; }
		public static string SelectApprovalRequestGroup { get => @"
			SELECT 
				SC.SubCategoryCode,
				SC.SubCategoryName,
				ARG.Sequence,
				AFD.Notes
			FROM (SELECT TOP 1 * FROM PRFSummary WHERE PRFId = @PRFId AND Status != 4 ORDER BY Id DESC) PS
				JOIN ApprovalRequest AR ON AR.Id = PS.ApprovalRequestId 
				JOIN ApprovalRequestGroup ARG ON ARG.ApprovalRequestId = AR.Id
				LEFT JOIN ApprovalFlowDetail AFD ON AFD.ApprovalFlowID = AR.ApprovalFlowId AND AFD.ApprovalGroup_SubCategoryId = ARG.ApprovalGroup_SubCategoryId
				JOIN SubCategory SC ON SC.Id = ARG.ApprovalGroup_SubCategoryId
			WHERE 
				PS.PRFId = @PRFId
			ORDER BY ARG.Sequence
		"; }
		public static string SelectApprovalRequestGroupMember { get => @"
			SELECT 
			PS.Id,
			BU.Name AS BusinessUnitName,
			CC.Name AS CostCenterName,
			ARGM.Level,
			ARGM.UserName,
			ARGM.Status,
			ARGM.Comment
			FROM (SELECT TOP 1 * FROM PRFSummary WHERE PRFId = @PRFId ORDER BY Id DESC) PS
			JOIN ApprovalRequest AR ON AR.Id = PS.ApprovalRequestId 
			JOIN ApprovalRequestGroup ARG ON ARG.ApprovalRequestId = AR.Id
			JOIN ApprovalRequestGroupMember ARGM ON ARGM.ApprovalRequestId = AR.Id 
				AND ARGM.ApprovaGroup_SubCategoryId = ARG.ApprovalGroup_SubCategoryId
			JOIN SubCategory SC ON SC.Id = ARGM.ApprovaGroup_SubCategoryId
			JOIN CostCenter CC ON CC.Id = ARGM.CostCenterId
			JOIN BusinessUnit BU ON BU.Id = CC.BusinessUnitId
			WHERE PS.PRFId = @PRFId
			AND SC.SubCategoryCode = @SubCategoryCode
			ORDER BY ARGM.Level
		"; }
		public static string SelectApprovalRequestGroupMemberDAP { get => @"
			SELECT 
			BU.Name AS BusinessUnitName,
			CC.Name AS CostCenterName,
			ARGM.Level,
			ARGM.UserName,
			ARGM.Status,
			ARGM.Comment
			FROM (SELECT TOP 1 * FROM PRFSummary WHERE PRFId = @PRFId ORDER BY Id DESC) PS
			JOIN ApprovalRequest AR ON AR.Id = PS.ApprovalRequestId 
			JOIN ApprovalRequestGroup ARG ON ARG.ApprovalRequestId = AR.Id
			JOIN ApprovalRequestGroupMemberDAP ARGM ON ARGM.ApprovalRequestId = AR.Id 
				AND ARGM.ApprovaGroup_SubCategoryId = ARG.ApprovalGroup_SubCategoryId
			JOIN SubCategory SC ON SC.Id = ARGM.ApprovaGroup_SubCategoryId
			JOIN CostCenter CC ON CC.Id = ARGM.CostCenterId
			JOIN BusinessUnit BU ON BU.Id = CC.BusinessUnitId
			WHERE 
			PS.PRFId = @PRFId
			AND SC.SubCategoryCode = @SubCategoryCode
			ORDER BY ARGM.Level
		"; }

	}

    public static class ListProcurmentBuyerQuery
    {
        public static string IsProcurement { get => $@"
			SELECT COUNT(1)
			FROM FLIPS.UserAccount UA
			INNER JOIN FLIPS.Role R ON R.Id = UA._Role
			WHERE UA.Id = @AccountId
				AND R.Roles LIKE '%Procurement%'
		"; }
        public static string IsViewAllPurchaseRequest { get => $@"
			SELECT COUNT(1)
			FROM SubCategory AS sc
			INNER JOIN Category AS c
			  ON c.Id = sc.CategoryId
			  AND c.CategoryCode = 'ViewAllPurchaseRequest'
			INNER JOIN Flips.UserAccount ua
			  ON ua.Username = sc.SubCategoryName
			  AND ua.Id = @AccountId;
		"; }
        public static string IsVendorManagement { get => $@"
			SELECT COUNT(1)
			FROM SubCategory SC
			JOIN Category C ON C.Id = SC.CategoryId
			JOIN Flips.UserAccount UA ON UA.UserName = SC.SubCategoryName
			WHERE C.CategoryCode = 'VendorManagement'
			  AND UA.Id = @AccountId;
		"; }

    }

    public static class SaveTypeProcessQuery
    {
        public static string InsertPRFVendorQuotation { get => $@"
																	INSERT INTO PRFVendorQuotation
																	(
   																		PRFId,
   																		QuotationNo,
   																		CreatedTime,
   																		CreatedBy
																	)
																	SELECT TOP (1)
   																		@PRFId AS PRFId,
   																		(SELECT dbo.[fn_Transaction_Generate_RecNo] ('QTS')) AS QuotationNo,
   																		GETDATE() AS CreatedTime,
   																		@UserName AS CreatedBy;
																"; }
        public static string UpdatePRFTypeProcessMoreTen { get => @"
																UPDATE PRF
																SET
   																	TypeProcess_SubCategory = @TypeProcess_SubCategory,
   																	ContractCriteria_SubCategory = @ContractCriteria_SubCategory,
   																	LastUpdatedBy = @UserName,
   																	LastUpdatedTime = @DateInserted,
   																	BuyerAccountId = @BuyerAccountId,
   																	BuyerUserName = buyer.UserName,
   																	BuyerEmail = buyer.Email
																FROM
   																	(SELECT UserName, Email FROM Flips.UserAccount WHERE Id = @BuyerAccountId) buyer
																WHERE
   																	Id = @PRFId;
															"; }
        public static string UpdatePRFTypeProcessLessTen { get => @"
																UPDATE PRF
																SET
   																	TypeProcess_SubCategory = @TypeProcess_SubCategory,
   																	ContractCriteria_SubCategory = @ContractCriteria_SubCategory,
   																	LastUpdatedBy = @UserName,
   																	LastUpdatedTime = @DateInserted,
   																	BuyerAccountId = @BuyerAccountId,
   																	BuyerUserName = buyer.UserName,
   																	BuyerEmail = buyer.Email,
																	Status = @Status
																FROM
   																	(SELECT UserName, Email FROM Flips.UserAccount WHERE Id = @BuyerAccountId) buyer
																WHERE
   																	Id = @PRFId;
															"; }
        public static string UpdatePRFVendorCandidateSetNonActive { get => @"
																				UPDATE PRFVendorCandidate
																				SET
																					LastUpdatedBy = @UserName,
																					LastUpdatedTime = @DateInserted,
																					Status = 0
																				WHERE
																					PRFId = @PRFId
																					AND Status = 1;
																			"; }
        public static string UpdatePRFVendorCandidateSetActive { get => @"
																			UPDATE PRFVendorCandidate
																			SET LastUpdatedBy = @UserName,
																				LastUpdatedTime = @DateInserted,
																				Status = 1
																			WHERE PRFId = @PRFId
																				AND VendorId = @VendorId
																				AND Status = 0
																			"; }
        public static string UpdatePRFVendorQuotationDetail { get => @"
																			UPDATE pvqd
																			SET
																				LastUpdatedTime = GETDATE(),
																				LastUpdatedBy = @UserName,
																				pvqd.Status = 0
																			FROM
																				PRFVendorQuotationDetail AS pvqd
																			INNER JOIN
																				PRFVendorQuotation AS pvq ON pvq.Id = pvqd.PRFVendorQuotationId
																			INNER JOIN
																				(
																					SELECT DISTINCT
																						pvc.PRFId,
																						pvc.VendorId
																					FROM
																						PRFVendorCandidate AS pvc
																					WHERE
																						pvc.Status = 0
																				) AS pvc ON pvc.PRFId = pvq.PRFId AND pvc.VendorId = pvqd.VendorId
																			WHERE
																				pvqd.Status = 1;
																		"; }
        public static string InsertPRFVendorCandidate { get => @"
																	CREATE TABLE #tempVendor 
																	(
																		VendorId BIGINT
																	);

																	INSERT INTO #tempVendor (VendorId)
																	SELECT DISTINCT VendorId
																	FROM OPENJSON(@jsonCandidate)
																	WITH (VendorId BIGINT '$.VendorId');

																	INSERT INTO PRFVendorCandidate
																	(
																		PRFId,
																		VendorId,
																		Status,
																		CreatedTime,
																		CreatedBy
																	)
																	SELECT 
																		@PRFId AS PRFId,
																		tv.VendorId AS VendorId,
																		1 AS Status, -- Default
																		@DateInserted AS CreatedTime,
																		@UserName AS CreatedBy
																	FROM 
																		#tempVendor AS tv
																	LEFT JOIN 
																		PRFVendorCandidate AS pvc ON pvc.PRFId = @PRFId AND pvc.Status = 1 AND pvc.VendorId = tv.VendorId
																	WHERE 
																		pvc.VendorId IS NULL;
																	"; }
		public static string DeleteAttachmentTypeProccess { get => $@"
			DELETE FROM Attachment 
			WHERE Category = 'PurchaseRequestForm' 
				AND Description = 'PRF Report' 
				AND RefId = @PRFId;
		"; }
	}

    public static class SaveVendorQuotationQuery
    {
        public static string UpdatePRFVendorQuotation { get => @"
			UPDATE PRFVendorQuotation
			SET Remarks = @Remarks,
				VersionNo = @VersionNo,
				Status = 1,
				Title = @Title,
				Summary = @Summary,
				EstimatedDeliveryDate = @EstimatedDeliveryDate,
				FinalSpesificationDate = @FinalSpesificationDate,
				LastUpdatedBy = @CreatedBy,
				LastUpdatedTime = @CreatedTime
			WHERE Id = @PRFVendorQuotationId;
		"; }
        public static string InsertPRFVendorQuotationDetail { get => @"
			INSERT INTO PRFVendorQuotationDetail
			(
				PRFVendorQuotationId,
				PRFDetailId,
				ItemDescription,
				ItemName,
				TypeOfGoods_SubCategoryId,
				Remarks,
				L_Currency_Code,
				QuotationAmount,
				Status,
				VendorId,
				IsSelected,
				ITRelated,
				QuotationDate,
				Qty,
				Unit,
				ItemPrice,
				TotalAmmount,
				RateAmmount,
				BaseAmmount,
				TotalBaseAmmount,
				IsAddMasterItem,
				CreatedTime,
				CreatedBy
			)
			OUTPUT inserted.Id
			VALUES
			(
				@PRFVendorQuotationId,
				@PRFDetailId,
				@ItemDescription,
				@ItemName,
				@TypeOfGoods_SubCategoryId,
				@Remarks,
				@L_Currency_Code,
				@QuotationAmount,
				1,
				@VendorId,
				@IsSelected,
				@ITRelated,
				@QuotationDate,
				@Qty,
				@Unit,
				@ItemPrice,
				@TotalAmmount,
				@RateAmmount,
				@BaseAmmount,
				@TotalBaseAmmount,
				@IsAddMasterItem,
				@CreatedTime,
				@CreatedBy
			);
		"; }
        public static string InsertPRFVendorQuotationCostCenter { get => @"
			INSERT INTO PRFVendorQuotationCostCenter
			(
				PRFVendorQuotationDetailId,
				CostCenterId,
				L_Currency_Code,
				Percentage,
				TotalAmmount,
				Remarks,
				CreatedTime,
				CreatedBy
			)
			VALUES
			(
				@PRFVendorQuotationDetailId,
				@CostCenterId,
				@L_Currency_Code,
				@Percentage,
				@TotalAmmount,
				@Remarks,
				@CreatedTime,
				@CreatedBy
			);
		"; }
        public static string InsertPRFVendorQuotationOtherCost { get => @"
			INSERT INTO [dbo].[PRFVendorQuotationOtherCost]
			(
				PRFVendorQuotationDetailId,
				OtherCost_SubCategoryId,
				MtOtherCostCode,
				L_Currency_Code,
				Included,
				VATPercentage,
				OtherCostAmount,
				Remarks,
				CreatedTime,
				CreatedBy
			)
			VALUES
			(
				@PRFVendorQuotationDetailId,
				@OtherCost_SubCategoryId,
				@MtOtherCostCode,
				@L_Currency_Code,
				@Included,
				@VATPercentage,
				@OtherCostAmount,
				@Remarks,
				@CreatedTime,
				@CreatedBy
			);
		"; }
        public static string InsertPRFVendorQuotationTOP { get => @"
			DECLARE @RateAmount money = (
													SELECT TOP 1 VQD.RateAmmount FROM PRFVendorQuotationDetail VQD WITH(NOLOCK)
													WHERE VQD.PRFVendorQuotationId = @PRFVendorQuotationId
														AND VQD.VendorId = @VendorId
														AND VQD.Status = @Status
												 )

			INSERT INTO PRFVendorQuotationTOP
			(
				PRFVendorQuotationId,
				TOPType_SubCategoryId,
				Percentage,
				TOPDays,
				RateAmount,
				BaseAmount,
				PaymentAmount,
				PaymentDate,
				PaymentMethod,
				Status,
				CreatedTime,
				CreatedBy,
				VendorId
			)
			VALUES
			(
				@PRFVendorQuotationId, 
				@TOPType_SubCategoryId, 
				@Percentage, 
				@TOPDays, 
				@RateAmount, 
				@RateAmount * @PaymentAmount, 
   				@PaymentAmount,
				@PaymentDate, 
				@PaymentMethod, 
				@Status, 
				@CreatedTime, 
				@CreatedBy, 
				@VendorId
			);
		"; }
        public static string DeletePRFVendorQuotationTOP { get => @"
			DELETE PRFVendorQuotationTOP 
			WHERE PRFVendorQuotationId = @PRFVendorQuotationId;
		"; }
    }

    public static class SaveProcsumQuery
	{
		public static string BudgetLevelValidaiton { get => @"
			SELECT TOP 1 MaxAmount
            FROM dbo.PRF PRF
            JOIN dbo.PRFVendorQuotation PVQ ON PVQ.PRFId = PRF.Id
            JOIN dbo.ApprovalGroup AG ON AG.CostCenterId = PRF.CostCenterId
            OUTER APPLY (SELECT SUM(PVQCC.TotalAmmount) [GrandTotalAmount] 
                        FROM dbo.PRFVendorQuotationDetail PVQD
                        JOIN dbo.PRFVendorQuotationCostCenter PVQCC ON PVQCC.PRFVendorQuotationDetailId = PVQD.Id AND PVQD.IsSelected = 1
                        WHERE PVQD.PRFVendorQuotationId = PVQ.Id and PVQD.Status != 0) PVQCC
            WHERE PRF.PRFNo = @PRFNumber
                AND AG.AccountId = @AccountId
                AND AG.CostCenterId = @CostCenterId
                AND AG.ApprovalGroup_SubCategoryId = @ApprovaGroupSubCategoryId
                AND AG.STATUS = 1
                AND PVQCC.GrandTotalAmount <= AG.MaxAmount
            ORDER BY AG.Id DESC
		"; }
        public static string InsertProcsumValidationQuery { get => @"
			SELECT COUNT(PRFS.Id)
			FROM PRFSummary PRFS
				JOIN PRF PRF ON PRF.Id = PRFS.PRFId
			WHERE PRF.PRFNo = @PRFNo
				AND PRFS.Status IN (1, 2)
		"; }
        public static string SelectSubCategoryCodePRFQuery { get => @"
			SELECT 
				SC.SubCategoryCode 
			FROM 
				PRF PRF
			JOIN 
				SubCategory SC ON SC.Id = PRF.TypeProcess_SubCategory
			WHERE 
				PRF.PRFNo = @PRFNo;
		"; }
        public static string InsertApprovalRequest { get => @"
			INSERT INTO ApprovalRequest
			(
				RequestNo,
				Remark,
				Status,
				ApprovalFlowId,
				CreatedTime,
				CreatedBy,
				RequestorAccountId,
				RequestorUserName,
				RequestorEmail,
				CreatorUserName,
				CreatorEmail,
				CreatorAccountId,
				CostCenterId
			)
			OUTPUT 
				INSERTED.*
			VALUES
			(
				(SELECT dbo.fn_ApprovalRequest_Generate_RecNo('PROCSUM')),
				@Remark,
				@Status,
				(SELECT AF.Id FROM dbo.ApprovalFlow AF WHERE AF.Name LIKE '%FlowProcsum%'),
				GETDATE(),
				@CreatedBy,
				(SELECT PRF.RequestorAccountId FROM dbo.PRF PRF WHERE PRF.PRFNo = @PRFNumber),
				(SELECT PRF.RequestorUserName FROM dbo.PRF PRF WHERE PRF.PRFNo = @PRFNumber),
				(SELECT PRF.RequestorEmail FROM dbo.PRF PRF WHERE PRF.PRFNo = @PRFNumber),
				@CreatedBy,
				(SELECT UA.Email FROM Flips.UserAccount UA WHERE UA.UserName = @CreatedBy),
				(SELECT UA.Id FROM Flips.UserAccount UA WHERE UA.UserName = @CreatedBy),
				(SELECT PRF.CostCenterId FROM dbo.PRF PRF WHERE PRF.PRFNo = @PRFNumber)
			);
		"; }
        public static string UpdatePRFSummary { get => @"
			UPDATE ps
			SET 
				ps.LastUpdatedTime = @DateNow,
				ps.LastUpdatedBy = @CreatedBy,
				ps.Status = 4
			FROM PRFSummary AS ps
			WHERE ps.PRFId = (SELECT TOP 1 p.Id FROM PRF AS p WHERE p.PRFNo = @PRFNumber)
			AND ps.Status = 0;
		"; }
        public static string InsertPRFSummary { get => @"
			INSERT INTO PRFSummary 
			(
				PRFId,
				Remarks,
				VersionNo,
				ApprovalRequestId,
				Status,
				ExecustionDateNotes,
				PRFSummaryDate,
				CreatedTime,
				CreatedBy,
				Title,
				Summary,
				PRFVendorQuotationId
				, FinalSpesificationDate
			)
			OUTPUT 
				INSERTED.*
			SELECT 
				PRFId,
				Remarks,
				VersionNo,
				@ApprovalRequestId,
				@Status,
				@DateNow,
				@DateNow,
				@DateNow,
				@CreatedBy,
				Title,
				Summary,
				Id
				, FinalSpesificationDate
			FROM PRFVendorQuotation
			WHERE PRFId = @PRFId;
		"; }
        public static string SelectPRFVendorQuotationDetail { get => @"
			SELECT 
				PVQD.*
			FROM PRFVendorQuotationDetail PVQD
			WHERE PVQD.Status <> 0
			AND PVQD.PRFVendorQuotationId = @PRFVendorQuotationId;
		"; }
        public static string SelectPRFVendorQuotationCostCenter { get => @"
			SELECT TOP 1
				PVQCC.*
			FROM PRFVendorQuotationCostCenter PVQCC
			WHERE PVQCC.PRFVendorQuotationDetailId = @PRFVendorQuotationDetailId;
		"; }
        public static string SelectPRFVendorQuotationOtherCost { get => @"
			SELECT 
				PVQOC.*
			FROM PRFVendorQuotationOtherCost PVQOC
			WHERE PVQOC.PRFVendorQuotationDetailId = @PRFVendorQuotationDetailId;
		"; }
        public static string InsertPRFSummaryDetail { get => @"
			INSERT INTO PRFSummaryDetail
			(
				PRFSummaryId,
				PRFVendorQuotationDetailId,
				Status,
				IsSelected,
				NeedDueDeligence,
				RegisterVendorStatus,
				CreatedTime,
				CreatedBy,
				ItemDescription,
				ItemName,
				TypeOfGoods_SubCategoryId,
				VendorId,
				ITRelated,
				QuotationDate,
				Qty,
				Unit,
				ItemPrice,
				TotalAmmount,
				RateAmmount,
				BaseAmmount,
				TotalBaseAmmount,
				IsAddMasterItem,
				Remarks
			)
			SELECT 
				PS.Id,
				PVQD.Id,
				@Status,
				PVQD.IsSelected,
				CASE WHEN V.Status = 0 THEN 1 WHEN V.Status = 1 THEN 0 END AS NeedDueDeligence,
				V.Status,
				@DateNow,
				@CreatedBy,
				PVQD.ItemDescription,
				PVQD.ItemName,
				PVQD.TypeOfGoods_SubCategoryId,
				PVQD.VendorId,
				PVQD.ITRelated,
				PVQD.QuotationDate,
				PVQD.Qty,
				PVQD.Unit,
				PVQD.ItemPrice,
				PVQD.TotalAmmount,
				PVQD.RateAmmount,
				PVQD.BaseAmmount,
				PVQD.TotalBaseAmmount,
				PVQD.IsAddMasterItem,
				PVQD.Remarks
			FROM PRFVendorQuotationDetail PVQD
			JOIN PRFVendorQuotation PVQ ON PVQ.Id = PVQD.PRFVendorQuotationId
			JOIN Vendor V ON V.Id = PVQD.VendorId
			JOIN PRFSummary PS ON PS.PRFId = PVQ.PRFId
			WHERE PVQ.PRFId = @PRFId
				AND PVQD.Status = 1
		"; }
        public static string InsertPRFSummaryCostCenter { get => @"
			INSERT INTO PRFSummaryCostCenter
			(
				PRFSummaryDetailId,
				CostCenterId,
				L_Currency_Code,
				Percentage,
				TotalAmmount,
				Remarks,
				CreatedTime,
				CreatedBy
			)
			SELECT 
				PSD.Id,
				PVQCC.CostCenterId,
				PVQCC.L_Currency_Code,
				PVQCC.Percentage,
				PVQCC.TotalAmmount,
				PVQCC.Remarks,
				@DateNow,
				@CreatedBy
			FROM PRFVendorQuotationCostCenter PVQCC
			JOIN PRFVendorQuotationDetail PVQD ON PVQD.Id = PVQCC.PRFVendorQuotationDetailId
			JOIN PRFVendorQuotation PVQ ON PVQ.Id = PVQD.PRFVendorQuotationId
			JOIN PRFSummaryDetail PSD ON PSD.PRFVendorQuotationDetailId = PVQD.Id
			WHERE PVQ.PRFId = @PRFId
				AND PVQD.Status = 1
		"; }
        public static string InsertPRFSummaryOtherCost { get => @"
			INSERT INTO PRFSummaryOtherCost
			(
				PRFSummaryId,
				PRFSummaryDetailId,
				OtherCost_SubCategoryId,
				MtOtherCostCode,
				L_Currency_Code,
				Included,
				VATPercentage,
				OtherCostAmount,
				Remarks,
				CreatedTime,
				CreatedBy
			)
			SELECT 
				PSD.PRFSummaryId,
				PSD.Id,
				PVQOC.OtherCost_SubCategoryId,
				PVQOC.MtOtherCostCode,
				PVQOC.L_Currency_Code,
				PVQOC.Included,
				PVQOC.VATPercentage,
				PVQOC.OtherCostAmount,
				PVQOC.Remarks,
				@DateNow,
				@CreatedBy
			FROM PRFVendorQuotationOtherCost PVQOC
			JOIN PRFVendorQuotationDetail PVQD ON PVQD.Id = PVQOC.PRFVendorQuotationDetailId
			JOIN PRFVendorQuotation PVQ ON PVQ.Id = PVQD.PRFVendorQuotationId
			JOIN PRFSummaryDetail PSD ON PSD.PRFVendorQuotationDetailId = PVQD.Id
			WHERE PVQ.PRFId = @PRFId
				AND PVQD.Status = 1
		"; }
        public static string InsertApprovalRequestItem { get => @"
			INSERT INTO ApprovalRequestItem
			(
				ApprovalRequestId,
				Notes,
				ApprovedQty,
				ApprovedAmount,
				Status,
				CreatedTime,
				CreatedBy,
				RequestQty,
				RequestTotalBasicAmount
			)
			SELECT 
				@ApprovalRequestId,
				@Notes,
				@ApprovedQty,
				@ApprovedAmount,
				@Status,
				@DateNow,
				@CreatedBy,
				PSD.Qty,
				@RequestTotalBasicAmount
			FROM PRFSummaryDetail PSD
			JOIN PRFSummary PS ON PS.Id = PSD.PRFSummaryId
			WHERE PS.PRFId = @PRFId
		"; }
        public static string InsertApprovalRequestGroup { get => @"
			INSERT INTO ApprovalRequestGroup
			(
				ApprovalRequestId,
				ApprovalGroup_SubCategoryId,
				Sequence,
				Remark,
				Status,
				CreatedTime,
				CreatedBy
			)
			OUTPUT 
				INSERTED.*
			VALUES
			(
				@ApprovalRequestId,
				@ApprovalGroup_SubCategoryId,
				@Sequence,
				@Remark,
				@Status,
				@DateNow,
				@CreatedBy
			);
		"; }
        public static string InsertApprovalRequestGroupMember { get => @"
			INSERT INTO ApprovalRequestGroupMember
			(
				ApprovalRequestId,
				CostCenterId,
				AccountId,
				UserName,
				Email,
				ApprovaGroup_SubCategoryId,
				Status,
				Sequence,
				CreatedTime,
				CreatedBy,
				Level
			)
			OUTPUT 
				INSERTED.*
			VALUES
			(
				@ApprovalRequestId,
				@CostCenterId,
				@ApproverAccountId,
				@ApproverUsername,
				(SELECT ISNULL((SELECT UA.Email FROM Flips.UserAccount UA WHERE UA.Id = @ApproverAccountId), '')),
				@ApprovaGroupSubCategoryId,
				@Status,
				@Sequence,
				@DateNow,
				@CreatedBy,
				@Level
			);
		"; }
        public static string InsertApprovalRequestGroupMemberDAP { get => @"
			INSERT INTO ApprovalRequestGroupMemberDAP
			(
				ApprovalRequestId,
				CostCenterId,
				AccountId,
				UserName,
				Email,
				ApprovaGroup_SubCategoryId,
				Status,
				Sequence,
				CreatedTime,
				CreatedBy,
				Level,
				PRFSummaryDetailId
			)
			OUTPUT 
				INSERTED.*
			VALUES
			(
				@ApprovalRequestId,
				@CostCenterId,
				@ApproverAccountId,
				@ApproverUsername,
				(SELECT ISNULL((SELECT UA.Email FROM Flips.UserAccount UA WHERE UA.Id = @ApproverAccountId), '')),
				@ApprovaGroupSubCategoryId,
				@Status,
				@Sequence,
				@DateNow,
				@CreatedBy,
				@Level,
				@PRFSummaryDetailId
			);
		"; }
        public static string InsertApprovalRequestEmail { get => @"
			INSERT INTO ApprovalRequestEmail
			(
				ApprovalRequestGroupMemberId,
				Action,
				Guid,
				LinkType,
				URLAction,
				Status,
				CreatedTime,
				CreatedBy,
				ApprovalRequestGroupMemberDAPId
			)
			OUTPUT 
				INSERTED.*
			VALUES
			(
				@ApprovalRequestGroupMemberId,
				@Action,
				@Guid,
				@LinkType,
				@URLAction,
				@Status,
				@DateNow,
				@CreatedBy,
				@ApprovalRequestGroupMemberDAPId
			);
		"; }
        public static string SelectPRFTotalAmount { get => @"
			SELECT 
				(SUM(COALESCE(PSD.TotalAmmount, 0)) +
				(
					SELECT SUM(POC.OtherCostAmount)
					FROM PRFSummaryOtherCost POC
					WHERE POC.PRFSummaryId = PFS.Id
				)) AS TotalAmount
			FROM PRF PRF
			JOIN PRFSummary PFS ON PRF.Id = PFS.PRFId
			JOIN PRFSummaryDetail PSD ON PFS.Id = PSD.PRFSummaryId
			WHERE PRF.PRFNo = @PRFNumber
				AND PSD.IsSelected = 1
			GROUP BY PFS.Id;
		"; }
		public static string InsertPRFSummaryTOP { get => @"
			INSERT INTO PRFSummaryTOP (
				PRFSummaryId,
				TOPType_SubCategoryId,
				Percentage,
				TOPDays,
				RateAmount,
				BaseAmount,
				PaymentAmount,
				PaymentDate,
				Notes,
				PaymentMethod,
				Status,
				CreatedBy,
				CreatedTime,
				VendorId
			)
			SELECT 
				PS.Id,
				PVQT.TOPType_SubCategoryId,
				PVQT.Percentage,
				PVQT.TOPDays,
				PVQT.RateAmount,
				PVQT.BaseAmount,
				PVQT.PaymentAmount,
				PVQT.PaymentDate,
				PVQT.Notes,
				PVQT.PaymentMethod,
				PVQT.Status,
				@CreatedBy,
				@DateNow,
				PVQT.VendorId
			FROM PRFVendorQuotationTOP PVQT
			JOIN PRFVendorQuotation PVQ ON PVQ.Id = PVQT.PRFVendorQuotationId
			JOIN PRFSummary PS ON PS.PRFId = PVQ.PRFId
			WHERE 
				PVQ.PRFId = @PRFId
		"; }
	}

	public static class SubmitProcsumQuery
    {
        public static string SelectPRFByPRFId { get => @"
			SELECT 
				PRF.PRFNo,
				PRF.RequestorAccountId,
				PRF.RequestorUserName,
				UA.Email AS RequestorEmail,
				PRF.CostCenterId
			FROM 
				PRF PRF
			JOIN 
				Flips.UserAccount UA ON UA.Id = PRF.RequestorAccountId
			WHERE 
				PRF.Id = @PRFId;
		"; }
        public static string SelectRequestNoProcsum { get => @"
			SELECT dbo.fn_ApprovalRequest_Generate_RecNo(@Name);
		"; }
    }

	public static class SavePAPQuery
	{
		public static string InsertPAP { get => @"
			INSERT INTO PAP (
				PRFId,
				PRFVendorQuotationId,
				PAPNo,
				VendorId,
				GenerateDate,
				PICVendorName,
				PICVendorMobile,
				PICVendorEmail,
				PeriodPaid,
				L_Currency_Code,
				BankAccountNumber,
				BankAccountOwnerName,
				BankCode,
				BankName,
				Description,
				FinanceType_SubCategory,
				InvoiceNo,
				Status,
				ReferenceNo,
				CreatedBy,
				CreatedTime
			)
			OUTPUT 
				INSERTED.*
			VALUES (
				@PRFId,
				@PRFVendorQuotationId,
				@PAPNo,
				@VendorId,
				@GenerateDate,
				@PICVendorName,
				@PICVendorMobile,
				@PICVendorEmail,
				@PeriodPaid,
				@L_Currency_Code,
				@BankAccountNumber,
				@BankAccountOwnerName,
				@BankCode,
				@BankName,
				@Description,
				@FinanceType_SubCategory,
				@InvoiceNo,
				@Status,
				@ReferenceNo,
				@CreatedBy,
				@CreatedTime
			);
		"; }
		public static string InsertPAPDetail { get => @"
			INSERT INTO PAPDetail (
				PAPId,
				PRFVendorQuotationDetailId,
				ItemDescription,
				ITRelated,
				ItemPrice,
				Qty,
				Unit,
				RateAmount,
				BaseAmount,
				TotalBaseAmount,
				TotalAmount,
				Remarks,
				Status,
				CreatedBy,
				CreatedTime
			)
			OUTPUT 
				INSERTED.*
			VALUES (
				@PAPId,
				@PRFVendorQuotationDetailId,
				@ItemDescription,
				@ITRelated,
				@ItemPrice,
				@Qty,
				@Unit,
				@RateAmount,
				@BaseAmount,
				@TotalBaseAmount,
				@TotalAmount,
				@Remarks,
				@Status,
				@CreatedBy,
				@CreatedTime
			);
		"; }
		public static string InsertPAPOtherCost { get => @"
			INSERT INTO PAPOtherCost (
				PAPDetailId,
				OtherCost_SubCategoryId,
				MtOtherCostCode,
				L_Currency_Code,
				Included,
				VATPercentage,
				OtherCostAmount,
				CreatedBy,
				CreatedTime,
				Status
			)
			VALUES (
				@PAPDetailId,
				@OtherCost_SubCategoryId,
				@MtOtherCostCode,
				@L_Currency_Code,
				@Included,
				@VATPercentage,
				@OtherCostAmount,
				@CreatedBy,
				@CreatedTime,
				@Status
			);
		"; }
		public static string InsertPAPCostCenter { get => @"
			INSERT INTO PAPCostCenter (
				PAPDetailId,
				CostCenterId,
				L_Currency_Code,
				Percentage,
				TotalAmmount,
				Status,
				CreatedBy,
				CreatedTime
			)
			VALUES (
				@PAPDetailId,
				@CostCenterId,
				@L_Currency_Code,
				@Percentage,
				@TotalAmmount,
				@Status,
				@CreatedBy,
				@CreatedTime
			);
		"; }
	}

	public static class UpdatePAPQuery
	{
		public static string UpdatePAP { get => @"
			UPDATE PAP
			SET 
				PRFId = @PRFId,
				PRFVendorQuotationId = @PRFVendorQuotationId,
				PAPNo = @PAPNo,
				VendorId = @VendorId,
				GenerateDate = @GenerateDate,
				PICVendorName = @PICVendorName,
				PICVendorMobile = @PICVendorMobile,
				PICVendorEmail = @PICVendorEmail,
				PeriodPaid = @PeriodPaid,
				L_Currency_Code = @L_Currency_Code,
				BankAccountNumber = @BankAccountNumber,
				BankAccountOwnerName = @BankAccountOwnerName,
				BankCode = @BankCode,
				BankName = @BankName,
				Description = @Description,
				FinanceType_SubCategory = @FinanceType_SubCategory,
				InvoiceNo = @InvoiceNo,
				Status = @Status,
				ReferenceNo = @ReferenceNo,
				ApprovalRequestId = null,
				LastUpdatedBy = @LastUpdatedBy,
				LastUpdatedTime = @LastUpdatedTime 
			WHERE Id = @Id
		"; }
		public static string UpdatePAPDetail { get => @"
			UPDATE PAPDetail
			SET 
				PAPId = @PAPId,
				PRFVendorQuotationDetailId = @PRFVendorQuotationDetailId,
				ItemDescription = @ItemDescription,
				ITRelated = @ITRelated,
				ItemPrice = @ItemPrice,
				Qty = @Qty,
				Unit = @Unit,
				RateAmount = @RateAmount,
				BaseAmount = @BaseAmount,
				TotalBaseAmount = @TotalBaseAmount,
				TotalAmount = @TotalAmount,
				Remarks = @Remarks,
				Status = @Status,
				LastUpdatedBy = @LastUpdatedBy,
				LastUpdatedTime = @LastUpdatedTime 
			WHERE Id = @Id
		"; }
		public static string UpdatePAPCostCenter { get => @"
			UPDATE PAPCostCenter
			SET
				PAPDetailId = @PAPDetailId,
				CostCenterId = @CostCenterId,
				L_Currency_Code = @L_Currency_Code,
				Percentage = @Percentage,
				TotalAmmount = @TotalAmmount,
				Status = @Status,
				LastUpdatedBy = @LastUpdatedBy,
				LastUpdatedTime = @LastUpdatedTime 
			WHERE Id = @Id
		"; }
		public static string UpdatePAPOtherCost { get => @"
			UPDATE PAPOtherCost
			SET
				PAPDetailId = @PAPDetailId,
				OtherCost_SubCategoryId = @OtherCost_SubCategoryId,
				MtOtherCostCode = @MtOtherCostCode,
				L_Currency_Code = @L_Currency_Code,
				Included = @Included,
				VATPercentage = @VATPercentage,
				OtherCostAmount = @OtherCostAmount,
				LastUpdatedBy = @LastUpdatedBy,
				LastUpdatedTime = @LastUpdatedTime,
				Status = @Status 
			WHERE Id = @Id
		"; }

	}
	public static class SubmitPAPQuery
	{
		public static string SelectPRFByPAPId { get => @"
			SELECT 
				PRF.PRFNo,
				PRF.RequestorAccountId,
				PRF.RequestorUserName,
				UA.Email AS RequestorEmail,
				PRF.CostCenterId
			FROM 
				PRF PRF
			JOIN 
				PAP PAP ON PAP.PRFId = PRF.Id
			JOIN 
				Flips.UserAccount UA ON UA.Id = PRF.RequestorAccountId
			WHERE 
				PAP.Id = @PAPId;
		"; }
		public static string GetPAPBudget { get => @"
			SELECT 
				PAP.ReferenceNo,
				PAP.L_Currency_Code,
				PAPD.RateAmount,
				SUM(PAPD.TotalBaseAmount) AS TotalBaseAmount
			FROM PAP PAP
			JOIN PAPDetail PAPD 
				ON PAPD.PAPId = PAP.Id
			WHERE PAP.Id = @PAPId
			GROUP BY 
				PAP.ReferenceNo,
				PAP.L_Currency_Code,
				PAPD.RateAmount;
		"; }
		public static string SelectUserAccountByUsername { get => @"
			SELECT 
   				Id AS CreatorAccountId, 
   				Username AS CreatorUserName, 
   				Email AS CreatorEmail
			FROM 
   				Flips.UserAccount
			WHERE 
   				Username = @Username;
		"; }
		public static string SelectApprovalFlowId { get => @"
			SELECT 
				Id 
			FROM 
				ApprovalFlow 
			WHERE 
				Name LIKE '%' + @Name + '%';
		"; }
		public static string InsertApprovalRequest { get => @"
			INSERT INTO ApprovalRequest (
				RequestNo,
				Remark,
				Status,
				ApprovalFlowId,
				CreatedTime,
				CreatedBy,
				RequestorAccountId,
				RequestorUserName,
				RequestorEmail,
				CreatorUserName,
				CreatorEmail,
				CreatorAccountId,
				CostCenterId
			)
			OUTPUT 
				INSERTED.*
			VALUES (
				@RequestNo,
				@Remark,
				@Status,
				@ApprovalFlowId,
				@CreatedTime,
				@CreatedBy,
				@RequestorAccountId,
				@RequestorUserName,
				@RequestorEmail,
				@CreatorUserName,
				@CreatorEmail,
				@CreatorAccountId,
				@CostCenterId
			);
		"; }
		public static string UpdatePAP { get => @"
			UPDATE 
				PAP
			SET 
				ApprovalRequestId = @ApprovalRequestId,
				Status = @Status,
				LastUpdatedBy = @LastUpdatedBy,
				LastUpdatedTime = @LastUpdatedTime
			WHERE 
				Id = @Id;
		"; }
		public static string InsertApprovalRequestGroup { get => @"
			INSERT INTO ApprovalRequestGroup
			(
				ApprovalRequestId,
				ApprovalGroup_SubCategoryId,
				Sequence,
				Remark,
				Status,
				CreatedTime,
				CreatedBy
			)
			VALUES
			(
				@ApprovalRequestId,
				@ApprovalGroup_SubCategoryId,
				@Sequence,
				@Remark,
				@Status,
				@DateNow,
				@CreatedBy
			);
		"; }
		public static string InsertApprovalRequestGroupMember { get => @"
			INSERT INTO ApprovalRequestGroupMember
			(
				ApprovalRequestId,
				CostCenterId,
				AccountId,
				UserName,
				Email,
				ApprovaGroup_SubCategoryId,
				Status,
				Sequence,
				CreatedTime,
				CreatedBy,
				Level
			)
			VALUES
			(
				@ApprovalRequestId,
				@CostCenterId,
				@ApproverAccountId,
				@ApproverUsername,
				(SELECT ISNULL((SELECT UA.Email FROM Flips.UserAccount UA WHERE UA.Id = @ApproverAccountId), '')),
				@ApprovalGroup_SubCategoryId,
				@Status,
				@Sequence,
				@DateNow,
				@CreatedBy,
				@Level
			);
		"; }
		public static string InsertApprovalRequestGroupMemberDAP { get => @"
			INSERT INTO ApprovalRequestGroupMemberDAP
			(
				ApprovalRequestId,
				CostCenterId,
				AccountId,
				UserName,
				Email,
				ApprovaGroup_SubCategoryId,
				Status,
				Sequence,
				CreatedTime,
				CreatedBy,
				Level
			)
			VALUES
			(
				@ApprovalRequestId,
				@CostCenterId,
				@ApproverAccountId,
				@ApproverUsername,
				(SELECT ISNULL((SELECT UA.Email FROM Flips.UserAccount UA WHERE UA.Id = @ApproverAccountId), '')),
				@ApprovalGroup_SubCategoryId,
				@Status,
				@Sequence,
				@DateNow,
				@CreatedBy,
				@Level
			);
		"; }
		public static string InsertApprovalRequestItem { get => @"
			INSERT INTO ApprovalRequestItem (
				ApprovalRequestId,
				Notes,
				ApprovedQty,
				ApprovedAmount,
				Status,
				CreatedTime,
				CreatedBy,
				RequestQty,
				RequestTotalBasicAmount
			)
			SELECT 
				@ApprovalRequestId,
				'',
				0,
				0,
				@Status,
				@CreatedTime,
				@CreatedBy,
				Qty,
				TotalBaseAmount
			FROM 
				PAPDetail
			WHERE 
				PAPId = @PAPId;
		"; }
	}

	public static class GetDataPAPQuery
	{
		public static string SelectPAP { get => @"
			SELECT 
   				* 
			FROM 
   				PAP 
			WHERE 
   				PRFId = @PRFId;
		"; }
        public static string SelectPAPDetail { get => @"
			SELECT 
				* 
			FROM 
				PAPDetail 
			WHERE 
				PAPId = @PAPId;
		"; }
        public static string SelectPAPCostCenter { get => @"
			SELECT 
				* 
			FROM 
				PAPCostCenter 
			WHERE 
				PAPDetailId = @PAPDetailId;
		"; }
        public static string SelectPAPOtherCost { get => @"
			SELECT 
				* 
			FROM 
				PAPOtherCost 
			WHERE 
				PAPDetailId = @PAPDetailId;
		"; }
		public static string SelectApprovalRequestGroupMember { get => @"
			SELECT 
				ARGM.UserName,
				ARGM.Level,
				ARGM.Status,
				ARGM.Comment,
				ARGM.ApprovaGroup_SubCategoryId [ApprovalGroup_SubCategoryId]
			FROM ApprovalRequestGroupMember ARGM
			JOIN ApprovalRequestGroup ARG 
				ON ARG.ApprovalRequestId = ARGM.ApprovalRequestId AND ARG.ApprovalGroup_SubCategoryId = ARGM.ApprovaGroup_SubCategoryId
			JOIN ApprovalRequest AR 
				ON AR.Id = ARG.ApprovalRequestId
			JOIN PAP PAP 
				ON AR.RequestNo = PAP.PAPNo
			WHERE PAP.Id = @PAPId
		"; }
	}

	public static class ReferenceProcurmentBuyerQuery
	{
		public static string SelectPAPNo { get => @"
			DECLARE 
				@TReqNo VARCHAR(50) = '',
				@Mounth VARCHAR(10) = '',  
				@VendorName VARCHAR(100) = '',  
				@CostCenterName VARCHAR(100) = '',  
				@BusinessUnitName VARCHAR(100) = '',
				@ReqDate DATETIME = DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE()), 0),
				@const VARCHAR(100) = '';
	
			-- Determine Roman numeral for the current month
			SELECT @Mounth = 
				CASE DATEPART(MONTH, @ReqDate)
					WHEN  1 THEN 'I'
					WHEN  2 THEN 'II'
					WHEN  3 THEN 'III'
					WHEN  4 THEN 'IV'
					WHEN  5 THEN 'V'
					WHEN  6 THEN 'VI'
					WHEN  7 THEN 'VII'
					WHEN  8 THEN 'VIII'
					WHEN  9 THEN 'IX'
					WHEN 10 THEN 'X'
					WHEN 11 THEN 'XI'
					WHEN 12 THEN 'XII'
				END
	
			-- Get vendor, cost center, and business unit names
			SELECT TOP 1
				@VendorName = UPPER(LEFT(REPLACE(V.ShortName, SPACE(1), ''), 5)),
				@CostCenterName = UPPER(CC.Name),
				@BusinessUnitName = UPPER(BU.ShortName)
			FROM PRFVendorQuotation VQ
			JOIN PRFVendorQuotationDetail VQD ON VQD.PRFVendorQuotationId = VQ.Id
			JOIN PRFVendorQuotationCostCenter VQCC ON VQCC.PRFVendorQuotationDetailId = VQD.Id
			JOIN Vendor V ON V.Id = VQD.VendorId
			JOIN CostCenter CC ON CC.Id = VQCC.CostCenterId
			JOIN BusinessUnit BU ON BU.Id = CC.BusinessUnitId
			WHERE VQ.PRFId = @PRFId;

			-- Construct the constant part of the request number
			SET @const = CONCAT(
				'/',
				@CostCenterName, '-', @BusinessUnitName, '/',
				@VendorName, '/',
				@Mounth, '/',
				DATEPART(YEAR, @ReqDate)
			);

			-- Get the latest TReqNo with matching pattern
			SELECT @TReqNo = MAX(a.TReqNo)
			FROM (
				SELECT TOP 1 
					SUBSTRING(a.PAPNo, 1, 3) AS TReqNo
				FROM PAP a WITH (ROWLOCK)
				WHERE a.CreatedTime >= @ReqDate
					AND a.PAPNo LIKE '%' + @const
				ORDER BY a.Id DESC
			) a;

			-- Generate the new TReqNo
			SET @TReqNo = CAST((CAST(REPLACE(ISNULL(@TReqNo, 0), @const, '') AS INT)) AS VARCHAR(3));
			SET @TReqNo = CONCAT(REPLICATE('0', 3 - LEN(@TReqNo)), @TReqNo);

			-- Output the final formatted request number
			SELECT RIGHT('000' + CAST(1 + COALESCE(@TReqNo, 0) AS VARCHAR), 3) + @const;
		"; }
	}
}
