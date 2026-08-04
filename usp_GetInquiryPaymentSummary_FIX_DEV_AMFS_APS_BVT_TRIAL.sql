USE [AMFS_APS_BVT_TRIAL]
GO

/****** Object:  StoredProcedure [dbo].[usp_GetInquiryPaymentSummary]    Script Date: 8/4/2026 3:59:32 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

---- =============================================
---- Author:		UmiAtiyah
---- Create date: 30-09-2024
---- Last Updated date		  : 30-12-2024, 23-01-2025, 24-7-2025
---- Last Updated by          : Umi, Abdul, Billy
---- Last Updated Description : add filtering status payment on progress (define with '2'), add cost center in travel settlement
----							add inquiry for requesttype GER
----							update all temp table to include PPH21 & PPN to match UNION Query
----							enhancement 08/08/2025:
----							1. Optimize where condition in temp table -> faster when retrieving data with filtering
----							2. Joiner with selection column
----							3. Optimize the SLA calculation on get master holiday	
----							enhancement 10/11/2025
----							1. Add new condition for all Trex Transaction
----							2. remove redundant condition
----							fixing 24/11/2025
----							1. SELECT RequestType from subcategory for type payment GER
----							fixing 28/11/2025
----							1. change prefix trex from lower case to uppercase with dash (e.g: TREX-APR)
----							2. change requestType for trex from voucherHeader.Category to hardcode uppercase with dash (e.g: TREX-APR)
----							fixing 8/12/2025
----							1. add field GERType when inserting to temptable, concat RequestType for TREX-GER
----							fixing 10/12/2025
----							1. add GrossUp for transaction Purchase Order
----							2. change Conditoin status (2,7,9) on Cash Advance Travel
---- Description:	Get Inquiry Payment Summary
---- Fix 04-08-2026: prevent NULL @RequestType from nullifying Shopping/Non Shopping dynamic SQL
---- =============================================
--EXEC usp_GetInquiryPaymentSummary 0,100000,'Non Shopping Cart','','','','','','','','','','','','','','','','','','','',0,'RequestNumber','DESC'

ALTER PROCEDURE [dbo].[usp_GetInquiryPaymentSummary] 
	-- Add the parameters for the stored procedure here
	@Page int,
	@PageSize int,
	@RequestType VARCHAR(100),
	@RequestNumber VARCHAR(100) = '',
	@VoucherId VARCHAR(100),	@VoucherNumber VARCHAR(100),

	@TransferNumber VARCHAR(100),
	@VendorCategoryId VARCHAR(100),
	@VendorId VARCHAR(100),
	@AccountMasterId VARCHAR(100),
	@BusinessUnitId VARCHAR(100),
	@CostCenterId VARCHAR(100),
	@RequestStatus VARCHAR(100),
	@StatusTransfer VARCHAR(100),
	@LCurrencyCode VARCHAR(100),
	@RequestorName VARCHAR(100),
	@MakerFinance VARCHAR(100),
	@RequestDateFrom VARCHAR(100),
	@RequestDateTo VARCHAR(100),
	@PaymentDateFrom VARCHAR(100),
	@PaymentDateTo VARCHAR(100),
	@CutOffHour VARCHAR(100),
	@IsExport bit,
	@SortColumn VARCHAR(100),
	@SortDirection VARCHAR(100)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	IF OBJECT_ID('tempdb..#tbl_temp_holiday') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_holiday
	END
	CREATE TABLE #tbl_temp_holiday(
		DateHoliday date,
		Status smallint
	)
	;WITH temp_holiday AS(
		SELECT DateHoliday, Status
		FROM MasterHoliday WITH(NOLOCK)
		WHERE Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7)
	)
	INSERT INTO #tbl_temp_holiday
	SELECT * FROM temp_holiday
	--SELECT *FROM #tbl_temp_holiday

	IF OBJECT_ID('tempdb..#tbl_temp_approvalrequest') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_approvalrequest
	END
	CREATE TABLE #tbl_temp_approvalrequest(
		Id bigint,
		Status smallint,
		ApprovalFlowId int,
		RequestNo VARCHAR(100),
		RequestorUserName varchar(100),
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_approvalrequest AS(
		SELECT
		Id, Status, ApprovalFlowId, RequestNo, RequestorUserName
		FROM ApprovalRequest ar WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_approvalrequest
	SELECT * FROM temp_approvalrequest
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_approvalrequest (ApprovalFlowId, RequestNo);
	--SELECT *FROM #tbl_temp_approvalrequest

	IF OBJECT_ID('tempdb..#tmp_argm') IS NOT NULL
		DROP TABLE #tmp_argm;
	CREATE TABLE #tmp_argm (
		RequestNo VARCHAR(100),
		ApprovalFlowId INT,
		ApprovalRequestId INT,
		ApprovalDate DATETIME,
		member NVARCHAR(MAX)
	);
	INSERT INTO #tmp_argm
	SELECT
		ar.RequestNo,
		ar.ApprovalFlowId,
		argm.[ApprovalRequestId],
		MAX(argm.[ApprovalDate]) AS [ApprovalDate],
		(SELECT [UserName] FROM [ApprovalRequestGroupMember] js WITH(NOLOCK) 
			WHERE js.[ApprovalRequestId] = argm.[ApprovalRequestId] FOR JSON PATH) AS [member]
	FROM ApprovalRequestGroupMember argm WITH(NOLOCK)
	JOIN (SELECT Id, RequestNo, ApprovalFlowId FROM ApprovalRequest WITH(NOLOCK)) ar 
		ON argm.ApprovalRequestId = ar.Id
	GROUP BY argm.[ApprovalRequestId], ar.RequestNo, ar.ApprovalFlowId;
	--SELECT *FROM #tmp_argm
	
	IF OBJECT_ID('tempdb..#tmp_argm_dap') IS NOT NULL 
	BEGIN 
		DROP TABLE #tmp_argm_dap
	END
	SELECT
		argm.[ApprovalRequestId],
		MAX(argm.[ApprovalDate]) AS [ApprovalDate],
		(
			SELECT * FROM (
				SELECT [UserName] 
				FROM ApprovalRequestGroupMember argm1
				JOIN SubCategory SC1 ON SC1.Id = argm1.ApprovaGroup_SubCategoryId 
				WHERE argm1.ApprovalRequestId = argm.[ApprovalRequestId] 
					AND SC1.SubCategoryCode = 'Non-Shoppingcart-PRF-Non-Budget'

				UNION ALL
			
				SELECT [UserName] 
				FROM [ApprovalRequestGroupMemberDAP] argmDAP1 WITH(NOLOCK) 
				WHERE argmDAP1.[ApprovalRequestId] = argm.[ApprovalRequestId] 
			) as su

			FOR JSON PATH
		) AS [member]
	INTO #tmp_argm_dap
	FROM (
		SELECT ApprovalRequestId, ApprovalDate FROM ApprovalRequestGroupMember 
		UNION ALL 
		SELECT ApprovalRequestId, ApprovalDate FROM ApprovalRequestGroupMemberDAP
	) argm
	GROUP BY argm.[ApprovalRequestId]
	--SELECT *FROM #tmp_argm_dap

	IF (@RequestType IS NULL OR @RequestType = '' OR @RequestType IN ('reimbursement', 'cash advance', 'cash advance travel', 'settlement'))
	BEGIN
		IF OBJECT_ID('tempdb..#tbl_temp_reimbursement') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_reimbursement
		END
		CREATE TABLE #tbl_temp_reimbursement(
			Id bigint,
			RequestNumber VARCHAR(100),
			Status smallint,
			Description varchar(500),
			ReasonReject varchar(500),
			RequestDate DateTime,
			RefNumber VARCHAR(100),
			UNIQUE CLUSTERED (Id)
		)
		;WITH temp_reimbursement AS(
			SELECT
			r.Id,
			RequestNumber,
			r.Status,
			Description,
			ReasonReject,
			RequestDate,
			RefNumber
			FROM Reimbursement r WITH (NOLOCK)
			WHERE (@RequestNumber IS NULL OR @RequestNumber = '' OR RequestNumber = @RequestNumber)
				  AND (@RequestStatus IS NULL OR @RequestStatus = '' OR Status = @RequestStatus)
				  AND (@RequestDateFrom IS NULL OR @RequestDateFrom = '' OR RequestDate >= @RequestDateFrom)
				  AND (@RequestDateTo IS NULL OR @RequestDateTo = '' OR RequestDate <= @RequestDateTo)
		)
		INSERT INTO #tbl_temp_reimbursement
		SELECT * FROM temp_reimbursement
		CREATE NONCLUSTERED INDEX [IX_num] ON #tbl_temp_reimbursement (RequestNumber);
		--SELECT *FROM #tbl_temp_reimbursement

		IF OBJECT_ID('tempdb..#tbl_temp_reimbursement_detail') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_reimbursement_detail
		END
		CREATE TABLE #tbl_temp_reimbursement_detail(
			Id bigint,
			ReimbursementId bigint,
			AccountMasterId int,
			VendorId int,
			ExpenseGeneral_SubCategoryId int,
			AttachmentId int,
			BankAccountOwnerName VARCHAR(250),
			BankAccountNumber VARCHAR(100),
			BankName VARCHAR(100),
			L_Currency VARCHAR(100),
			RateAmount Money,
			Amount Money,
			GrandTotal Money,
			InvoiceNo varchar(200),
			Description varchar(500),
			UNIQUE CLUSTERED (Id) 

		)
		;WITH temp_reimbursement_detail AS(
			SELECT
			rd.Id, ReimbursementId, AccountMasterId, VendorId, ExpenseGeneral_SubCategoryId, AttachmentId,
			BankAccountOwnerName, BankAccountNumber, BankName, L_Currency, RateAmount, Amount, GrandTotal, InvoiceNo, rd.Description
			FROM ReimbursementDetail rd WITH (NOLOCK)
		)
		INSERT INTO #tbl_temp_reimbursement_detail
		SELECT * FROM temp_reimbursement_detail
		CREATE NONCLUSTERED INDEX [IX_num] ON #tbl_temp_reimbursement_detail (ReimbursementId, AccountMasterId, VendorId, ExpenseGeneral_SubCategoryId, AttachmentId);
		--SELECT *FROM #tbl_temp_reimbursement_detail

		IF OBJECT_ID('tempdb..#tbl_temp_reimbursement_detail_costcenter') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_reimbursement_detail_costcenter
		END
		CREATE TABLE #tbl_temp_reimbursement_detail_costcenter(
			Id bigint,
			ReimbursementDetailId bigint,
			BusinessUnitId int,
			CostCenterId int,
			Amount money,
			UNIQUE CLUSTERED (Id) 
		)
		;WITH tbl_temp_reimbursement_detail_costcenter AS(
			SELECT
			rcc.Id, ReimbursementDetailId, BusinessUnitId, CostCenterId, rcc.Amount
			FROM ReimbursementDetailCostCenter rcc WITH (NOLOCK)
		)
		INSERT INTO #tbl_temp_reimbursement_detail_costcenter
		SELECT * FROM tbl_temp_reimbursement_detail_costcenter
		CREATE NONCLUSTERED INDEX [IX_num] ON #tbl_temp_reimbursement_detail_costcenter (ReimbursementDetailId);
		--SELECT *FROM #tbl_temp_reimbursement_detail_costcenter

		IF OBJECT_ID('tempdb..#tbl_temp_reimbursement_detail_othercost') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_reimbursement_detail_othercost
		END
		CREATE TABLE #tbl_temp_reimbursement_detail_othercost(
			Id bigint,
			ReimbursementDetailId bigint,
			OtherCost_SubCategoryId int,
			BasicAmount money,
			Amount Money,
			GrossUp Money
			UNIQUE CLUSTERED (Id) 
		)
		;WITH tbl_temp_reimbursement_detail_othercost AS(
			SELECT roc.Id,ReimbursementDetailId, OtherCost_SubCategoryId, BasicAmount, roc.Amount, roc.GrossUp
			FROM ReimbursementDetailOtherCost roc WITH(NOLOCK)
		)
		INSERT INTO #tbl_temp_reimbursement_detail_othercost
		SELECT * FROM tbl_temp_reimbursement_detail_othercost
		CREATE NONCLUSTERED INDEX [IX_num] ON #tbl_temp_reimbursement_detail_othercost (ReimbursementDetailId, OtherCost_SubCategoryId);
		--SELECT *FROM #tbl_temp_reimbursement_detail_othercost
	END
	
	IF OBJECT_ID('tempdb..#tbl_temp_voucher_header') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_voucher_header
	END
	CREATE TABLE #tbl_temp_voucher_header(
		Id bigint,
		Attachment int,
		TransferNumber VARCHAR(100),
		VoucherNumber VARCHAR(100),
		TransferTime DateTime,
		Category VARCHAR(100),
		CreatedBy VARCHAR(100)
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_voucher_header AS(
		SELECT
		Id, Attachment, TransferNumber, VoucherNumber, TransferTime, Category, CreatedBy
		FROM VoucherHeader vh WITH (NOLOCK)
		WHERE (@VoucherNumber IS NULL OR @VoucherNumber = '' OR vh.VoucherNumber = @VoucherNumber)
          AND (@TransferNumber IS NULL OR @TransferNumber = '' OR vh.TransferNumber = @TransferNumber)
          AND (@MakerFinance IS NULL OR @MakerFinance = '' OR vh.CreatedBy = @MakerFinance)
	)
	INSERT INTO #tbl_temp_voucher_header
	SELECT * FROM temp_voucher_header
	CREATE NONCLUSTERED INDEX [IX_num] ON #tbl_temp_voucher_header (Attachment);
	--SELECT *FROM #tbl_temp_voucher_header

	IF OBJECT_ID('tempdb..#tbl_temp_voucher_detail') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_voucher_detail
	END
	CREATE TABLE #tbl_temp_voucher_detail(
		Id bigint,
		VoucherId bigint,
		StatusTransfer smallint,
		TotalBaseAmmount money,
		TotalOriginalAmmount money,
		VoucherRefId VARCHAR(100)
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_voucher_detail AS(
		SELECT
		vd.Id, VoucherId, StatusTransfer, TotalBaseAmmount, TotalOriginalAmmount, VoucherRefId
		FROM VoucherDetail vd WITH (NOLOCK)
		WHERE (@StatusTransfer IS NULL OR @StatusTransfer = '' OR vd.StatusTransfer = @StatusTransfer)
	)
	INSERT INTO #tbl_temp_voucher_detail
	SELECT * FROM temp_voucher_detail
	CREATE NONCLUSTERED INDEX [IX_num] ON #tbl_temp_voucher_detail (VoucherId);
	--SELECT *FROM #tbl_temp_voucher_detail

	IF (@RequestType IS NULL OR @RequestType = '' OR @RequestType IN ('reimbursement', 'cash advance', 'cash advance travel', 'settlement'))
	BEGIN
	IF OBJECT_ID('tempdb..#tbl_temp_settlement') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_settlement
	END
	CREATE TABLE #tbl_temp_settlement(
		Id bigint,
		SettlementNumber VARCHAR(100),
		Status smallint,
		ReasonReject varchar(500),
		SettlementDate Datetime,
		VoucherId bigint,
		ReimbursementId bigint,
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_settlement AS(
		SELECT
		s.Id, SettlementNumber, s.Status, s.ReasonReject, SettlementDate, VoucherId, ReimbursementId
		FROM Settlement s WITH (NOLOCK)
		JOIN Reimbursement r on r.Id = s.ReimbursementId
		WHERE (@RequestNumber IS NULL OR @RequestNumber = '' OR r.RequestNumber = @RequestNumber)
	)
	INSERT INTO #tbl_temp_settlement
	SELECT * FROM temp_settlement
	CREATE NONCLUSTERED INDEX [IX_num] ON #tbl_temp_settlement (SettlementNumber, ReimbursementId );
	--SELECT *FROM #tbl_temp_settlement

	IF OBJECT_ID('tempdb..#tbl_temp_settlement_detail') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_settlement_detail
	END
	CREATE TABLE #tbl_temp_settlement_detail(
		Id bigint,
		SettlementId bigint,
		Amount money,
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_settlement_detail AS(
		SELECT
		sd.Id, SettlementId, Amount
		FROM SettlementDetail sd WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_settlement_detail
	SELECT * FROM temp_settlement_detail
	CREATE NONCLUSTERED INDEX [IX_num] ON #tbl_temp_settlement_detail (SettlementId );
	--SELECT *FROM #tbl_temp_subcategory
	END

	IF OBJECT_ID('tempdb..#tbl_temp_subcategory') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_subcategory
	END
	CREATE TABLE #tbl_temp_subcategory(
		Id bigint,
		SubCategoryCode varchar(50),
		SubCategoryName varchar(100),
		CategoryId int
	)
	;WITH temp_subcategory AS(
		SELECT
		Id, SubCategoryCode, SubCategoryName, CategoryId
		FROM SubCategory sc WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_subcategory
	SELECT * FROM temp_subcategory
	--SELECT *FROM #tbl_temp_subcategory

	IF OBJECT_ID('tempdb..#tbl_temp_vendor') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_vendor
	END
	CREATE TABLE #tbl_temp_vendor(
		Id bigint,
		SubCategoryId int,
		EmployeeCode varchar(100),
		Code varchar(100),
		Name varchar(100)
	)
	;WITH temp_vendor AS(
		SELECT
		Id, SubCategoryId, EmployeeCode, Code, Name
		FROM Vendor v WITH (NOLOCK)
		WHERE (@VendorId IS NULL OR @VendorId = '' OR Id = @VendorId)
	)
	INSERT INTO #tbl_temp_vendor
	SELECT * FROM temp_vendor
	--SELECT *FROM #tbl_temp_vendor

	IF OBJECT_ID('tempdb..#tbl_temp_accountmaster') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_accountmaster
	END
	CREATE TABLE #tbl_temp_accountmaster(
		Id bigint,
		AccountCode varchar(100),
		ShortDescription varchar(100),
		MtAccountType varchar(100)
	)
	;WITH temp_accountmaster AS(
		SELECT
		Id, AccountCode, ShortDescription, MtAccountType
		FROM AccountMaster am WITH (NOLOCK)
		WHERE (@AccountMasterId IS NULL OR @AccountMasterId = '' OR am.Id = @AccountMasterId)
	)
	INSERT INTO #tbl_temp_accountmaster
	SELECT * FROM temp_accountmaster
	--SELECT *FROM #tbl_temp_accountmaster

	IF OBJECT_ID('tempdb..#tbl_temp_businessunit') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_businessunit
	END
	CREATE TABLE #tbl_temp_businessunit(
		Id bigint,
		Code varchar(100),
		Name varchar(100)
	)
	;WITH temp_businessunit AS(
		SELECT
		Id, Code, Name
		FROM BusinessUnit bu WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_businessunit
	SELECT * FROM temp_businessunit
	--SELECT *FROM #tbl_temp_businessunit

	IF OBJECT_ID('tempdb..#tbl_temp_costcenter') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_costcenter
	END
	CREATE TABLE #tbl_temp_costcenter(
		Id bigint,
		BusinessUnitId int,
		Code varchar(100),
		Name varchar(100)
	)
	;WITH temp_costcenter AS(
		SELECT
		Id, BusinessUnitId, Code, Name
		FROM CostCenter cc WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_costcenter
	SELECT * FROM temp_costcenter
	--SELECT *FROM #tbl_temp_costcenter

	IF OBJECT_ID('tempdb..#tbl_temp_attachment') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_attachment
	END
	CREATE TABLE #tbl_temp_attachment(
		Id bigint,
		RefId int,
		Category VARCHAR(500),
		Description VARCHAR(500)
	)
	;WITH temp_attachment AS(
		SELECT
		Id, RefId, Category, Description
		FROM Attachment a WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_attachment
	SELECT * FROM temp_attachment
	--SELECT *FROM #tbl_temp_attachment

	IF OBJECT_ID('tempdb..#tbl_temp_travelrequest') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_travelrequest
	END
	CREATE TABLE #tbl_temp_travelrequest(
		Id bigint,
		ApprovalRequestId int,
		VendorId int,
		IsOverseas bit,
		PurposeNotes VARCHAR(500)
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_travelrequest AS(
		SELECT
		Id, ApprovalRequestId, VendorId, IsOverseas, PurposeNotes
		FROM TravelRequest tr WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_travelrequest
	SELECT * FROM temp_travelrequest
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_travelrequest (ApprovalRequestId);
	--SELECT *FROM #tbl_temp_travelrequest
	
	IF OBJECT_ID('tempdb..#tbl_temp_travelexpense') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_travelexpense
	END
	CREATE TABLE #tbl_temp_travelexpense(
		Id bigint,
		TravelRequestId int,
		CostCenterId int,
		RequestNumber VARCHAR(100),
		Status smallint,
		RequestDate DateTime,
		BankAccountOwnerName VARCHAR(250),
		BankAccountNumber VARCHAR(100),
		BankCode VARCHAR(100),
		BankName VARCHAR(100),
		L_Currency VARCHAR(100),
		GrandTotal Money,
		ReasonReject VARCHAR(500),
		RefNoCA VARCHAR(100)
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_travelexpense AS(
		SELECT
		tre.Id, TravelRequestId, CostCenterId, RequestNumber, tre.Status, RequestDate, BankAccountOwnerName, BankAccountNumber, BankCode, BankName, L_Currency, GrandTotal,
		ReasonReject, RefNoCA
		FROM TravelRequestExpense tre WITH (NOLOCK)
		WHERE (@RequestStatus IS NULL OR @RequestStatus = '' OR Status = @RequestStatus)
          AND (@RequestDateFrom IS NULL OR @RequestDateFrom = '' OR RequestDate >= @RequestDateFrom)
          AND (@RequestDateTo IS NULL OR @RequestDateTo = '' OR RequestDate <= @RequestDateTo)
	)
	INSERT INTO #tbl_temp_travelexpense
	SELECT * FROM temp_travelexpense
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_travelexpense (TravelRequestId);
	--SELECT *FROM #tbl_temp_travelexpense


	IF OBJECT_ID('tempdb..#tbl_temp_travelexpense_detail') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_travelexpense_detail
	END
	CREATE TABLE #tbl_temp_travelexpense_detail(
		Id bigint,
		TravelRequestExpenseId int,
		TypeExpense_SubCategoryId int,
		AttachmentId int,
		Amount Money,
		Description VARCHAR(500)
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_travelexpense_detail AS(
		SELECT
		tred.Id, TravelRequestExpenseId, TypeExpense_SubCategoryId, AttachmentId, Amount, Description
		FROM TravelRequestExpenseDetail tred WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_travelexpense_detail
	SELECT * FROM temp_travelexpense_detail
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_travelexpense_detail (TravelRequestExpenseId, TypeExpense_SubCategoryId, AttachmentId);
	--SELECT *FROM #tbl_temp_travelexpense_detail

	IF (@RequestType IS NULL OR @RequestType = '' OR @RequestType IN ('invoice travel'))
	BEGIN
	IF OBJECT_ID('tempdb..#tbl_temp_invoice_travel') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_invoice_travel
	END
	CREATE TABLE #tbl_temp_invoice_travel (		
		[Id] bigint,
		[RequestNumber] [varchar](100) NULL,
		[RequestDate] [datetime] NULL,
		[VendorId] [int] NULL,
		[RequestorUsername] [varchar](150) NULL,
		[BankAccountOwnerName] [varchar](250) NULL,
		[BankAccountNumber] [varchar](150) NULL,
		[BankName] [varchar](250) NULL,
		[Description] [varchar](500) NULL,
		[ReasonReject] [varchar](500) NULL,
		[Status] [smallint] NULL,
		[AttachmentId] [int] NULL,
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_invoice_travel AS(
		SELECT 
		    it.Id,
			it.RequestNumber,
			it.RequestDate,
			it.VendorId,
			it.RequestorUsername,
			it.BankAccountOwnerName,
			it.BankAccountNumber,
			it.BankName,
			it.Description,
			it.ReasonReject,
			it.Status,
			it.AttachmentId
		FROM InvoiceTravel AS it WITH (NOLOCK)
		WHERE (@RequestNumber IS NULL OR @RequestNumber = '' OR RequestNumber = @RequestNumber)
			  AND (@RequestStatus IS NULL OR @RequestStatus = '' OR Status = @RequestStatus)
			  AND (@RequestDateFrom IS NULL OR @RequestDateFrom = '' OR RequestDate >= @RequestDateFrom)
			  AND (@RequestDateTo IS NULL OR @RequestDateTo = '' OR RequestDate <= @RequestDateTo)
	)
	INSERT INTO #tbl_temp_invoice_travel
	SELECT * FROM temp_invoice_travel
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_invoice_travel (Id);
	--SELECT *FROM #tbl_temp_invoice_travel

	IF OBJECT_ID('tempdb..#tbl_temp_invoice_travel_detail') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_invoice_travel_detail
	END
	CREATE TABLE #tbl_temp_invoice_travel_detail (		
		[Id] bigint,
		[InvoiceTravelId] [int] NULL,
		[CostCenter] [int] NULL,
		[L_Currency] [varchar](100) NULL,
		[RateAmount] [decimal](18, 2) NULL,
		[FullAmount] [decimal](18, 2) NULL,
		[VATAmount] [decimal](18, 2) NULL,
		[PPH23Amount] [decimal](18, 2) NULL,
		[TotalAmount] [decimal](18, 2) NULL,
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_invoice_travel_detail AS(
		SELECT 
				 itd.[Id],
				 itd.[InvoiceTravelId],
				 itd.[CostCenter],
				 itd.[L_Currency],
				 itd.[RateAmount],
				 itd.[FullAmount],
				 itd.[VATAmount],
				 itd.[PPH23Amount],
				 itd.[TotalAmount]
		FROM InvoiceTravelDetail itd WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_invoice_travel_detail
	SELECT * FROM temp_invoice_travel_detail
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_invoice_travel_detail (Id);
	--SELECT *FROM #tbl_temp_invoice_travel_detail
	END

	IF (@RequestType IS NULL OR @RequestType = '' OR @RequestType in ('ger', 'COMBEN', 'CONTEST', 'OTHERS'))
	BEGIN
	IF OBJECT_ID('tempdb..#tbl_temp_gerheader') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_gerheader
	END
	CREATE TABLE #tbl_temp_gerheader (		
		[Id] bigint NOT NULL,
		[RequestorUsername] [varchar](250) NULL,
		[CostCenterId] [int] NULL,
		[RequestDate] [datetime] NULL,
		[ExpenseType_SubCategoryId] [int] NULL,
		[PaymentType_SubCategoryId] [int] NULL,
		[Department_SubCategoryId] [int] NULL,
		[SalesForce_SubCategoryDetailId] [int] NULL,
		[Description] [varchar](MAX) NULL,
		[ReasonReject] [varchar](MAX) NULL,
		[RequestNumber] [varchar](250) NULL,
		[Status] [smallint] NULL,
		[AttachmentId] [int] NULL,
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_gerheader AS(
		SELECT 
		    g.Id,
		    g.RequestorUsername,
		    g.CostCenterId,
		    g.RequestDate,
		    g.ExpenseType_SubCategoryId,
		    g.PaymentType_SubCategoryId,
		    g.Department_SubCategoryId,
		    g.SalesForce_SubCategoryDetailId,
		    g.Description,
			g.ReasonReject,
		    g.RequestNumber,
		    g.Status,
			g.AttachmentId
		FROM GerHeader AS g WITH (NOLOCK)
		WHERE (@RequestNumber IS NULL OR @RequestNumber = '' OR RequestNumber = @RequestNumber)
			  AND (@RequestStatus IS NULL OR @RequestStatus = '' OR Status = @RequestStatus)
			  AND (@RequestDateFrom IS NULL OR @RequestDateFrom = '' OR RequestDate >= @RequestDateFrom)
			  AND (@RequestDateTo IS NULL OR @RequestDateTo = '' OR RequestDate <= @RequestDateTo)
	)
	INSERT INTO #tbl_temp_gerheader
	SELECT * FROM temp_gerheader
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_gerheader (Id);
	--SELECT * FROM #tbl_temp_gerheader

	IF OBJECT_ID('tempdb..#tbl_temp_gerdetail') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_gerdetail
	END
	CREATE TABLE #tbl_temp_gerdetail (		
		[Id] bigint NOT NULL,
		[GerHeaderId] [int] NULL,
		[ClaimNumber] [varchar](250) NULL,
		[PolicyNumber] [varchar](250) NULL,
		[InsuredName] [varchar](250) NULL,
		[OwnerName] [varchar](250) NULL,
		[AccountNumber] [varchar](250) NULL,
		[BankAccountOwnerName] [varchar](250) NULL,
		[BankName] [varchar](250) NULL,
		[Amount] [decimal](18, 2) NULL,
		[NettAmount] [decimal](18, 2) NULL,
		[LCurrencyCode] [nvarchar](10) NULL,
		[PaymentType_SubCategoryId] [int] NULL,
		[Description] [varchar](MAX) NULL,
		[Note] [varchar](MAX) NULL,
		[Status] [smallint] NULL,
		[Deduction] [decimal](18, 2) NULL,
		[PPN] [decimal](18, 2) NULL,
		[PPH21] [decimal](18, 2) NULL,
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_gerdetail AS(
		SELECT 
				 gd.[Id],
				 gd.[GerHeaderId],
				 gd.[ClaimNumber],
				 gd.[PolicyNumber],
				 gd.[InsuredName],
				 gd.[OwnerName],
				 gd.[AccountNumber],
				 gd.[BankAccountOwnerName],
				 gd.[BankName],
				 gd.[Amount],
				 gd.[NettAmount],
				 gd.[LCurrencyCode],
				 gd.[PaymentType_SubCategoryId],
				 gd.[Description],
				 gd.[Note],
				 gd.[Status],
				 gd.[Deduction],
				 gd.[PPN],
				 gd.[PPH21]		
		FROM GerDetail gd WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_gerdetail
	SELECT * FROM temp_gerdetail
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_gerdetail (Id);
	--SELECT * FROM #tbl_temp_gerdetail
	END

	IF (@RequestType = 'purchase order' OR @RequestType = 'Shopping Cart' OR @RequestType = 'Non Shopping Cart' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
	IF OBJECT_ID('tempdb..#tbl_temp_purchaserequest') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_purchaserequest
	END
	CREATE TABLE #tbl_temp_purchaserequest(
		Id bigint,
		RequestCode VARCHAR(100),
		RequestorUsername VARCHAR(100),
		Status smallint,
		Notes VARCHAR(500),
		RequestDates datetime,
		lastupdatedTime datetime,
		ApprovalRequestId bigint
	)
	;WITH temp_purchaserequest AS(
		SELECT
		pr.Id, RequestCode, pr.RequestorUserName, pr.Status, Notes, RequestDates, LastUpdatedTime, ApprovalRequestId
		FROM PurchaseRequest pr WITH (NOLOCK)
		WHERE (@RequestStatus IS NULL OR @RequestStatus = '' OR Status = @RequestStatus)
          AND (@RequestDateFrom IS NULL OR @RequestDateFrom = '' OR RequestDates >= @RequestDateFrom)
          AND (@RequestDateTo IS NULL OR @RequestDateTo = '' OR RequestDates <= @RequestDateTo)
		  AND (@RequestorName IS NULL OR @RequestorName = '' OR RequestorUserName = @RequestorName)
	)
	INSERT INTO #tbl_temp_purchaserequest
	SELECT * FROM temp_purchaserequest
	--SELECT *FROM #tbl_temp_purchaserequest

	IF OBJECT_ID('tempdb..#tbl_temp_purchaserequest_detail') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_purchaserequest_detail
	END
	CREATE TABLE #tbl_temp_purchaserequest_detail(
		Id bigint,
		PurchaseRequestId int,
		ApprovalRequestId int,
		AccountMasterId int,
		VendorId int,
		AttachmentId int,
		RateAmount money,
		ItemId int,
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_purchaserequest_detail AS(
		SELECT
		prd.Id, PurchaseRequestId, ApprovalRequestId, AccountMasterId, VendorId, AttachmentId, RateAmount, ItemId
		FROM PurchaseRequestItemDetail prd WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_purchaserequest_detail
	SELECT * FROM temp_purchaserequest_detail
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_purchaserequest_detail (PurchaseRequestId, ApprovalRequestId, AccountMasterId, VendorId, AttachmentId, ItemId);
	--SELECT *FROM #tbl_temp_purchaserequest_detail

	IF OBJECT_ID('tempdb..#tbl_temp_purchaserequest_purchaseorder') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_purchaserequest_purchaseorder
	END
	CREATE TABLE #tbl_temp_purchaserequest_purchaseorder(
		Id bigint,
		PurchaseOrderId int,
		PurchaseRequestlId int
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_purchaserequest_purchaseorder AS(
		SELECT
		Id, PurchaseOrderId, PurchaseRequestlId
		FROM PurchaseOrderToPurchaseRequest prt WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_purchaserequest_purchaseorder
	SELECT * FROM temp_purchaserequest_purchaseorder
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_purchaserequest_purchaseorder (PurchaseOrderId, PurchaseRequestlId);
	--SELECT *FROM #tbl_temp_purchaserequest_purchaseorder

	IF OBJECT_ID('tempdb..#tbl_temp_purchaseorder') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_purchaseorder
	END
	CREATE TABLE #tbl_temp_purchaseorder(
		Id bigint,
		AttachmentId int,
		PONumber VARCHAR(100),
		RequestorName VARCHAR(100),
		VendorId int
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_purchaseorder AS(
		SELECT Id, AttachmentId, PONumber, RequestorName, VendorId
		FROM PurchaseOrder po WITH (NOLOCK)
		WHERE (@RequestNumber IS NULL OR @RequestNumber = '' OR PONumber = @RequestNumber)
	)
	INSERT INTO #tbl_temp_purchaseorder
	SELECT * FROM temp_purchaseorder
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_purchaseorder (AttachmentId, VendorId);
	--SELECT *FROM #tbl_temp_purchaseorder

	IF OBJECT_ID('tempdb..#tbl_temp_purchaseorder_detail') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_purchaseorder_detail
	END
	CREATE TABLE #tbl_temp_purchaseorder_detail(
		Id bigint,
		PurchaseOrderId int
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_purchaseorder_detail AS(
		SELECT
		Id, PurchaseOrderId
		FROM PurchaseOrderDetail prd WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_purchaseorder_detail
	SELECT * FROM temp_purchaseorder_detail
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_purchaseorder_detail (PurchaseOrderId);
	--SELECT *FROM #tbl_temp_purchaseorder_detail

	IF OBJECT_ID('tempdb..#tbl_temp_purchaseorder_costcenter') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_purchaseorder_costcenter
	END
	CREATE TABLE #tbl_temp_purchaseorder_costcenter(
		Id bigint,
		PurchaseOrderDetailId int,
		CostCenterId int,
		TotalAmount money
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_purchaseorder_costcenter AS(
		SELECT
		prdcc.Id, PurchaseOrderDetailId, CostCenterId, TotalAmount
		FROM PurchaseOrderDetailCostCenter prdcc WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_purchaseorder_costcenter
	SELECT * FROM temp_purchaseorder_costcenter
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_purchaseorder_costcenter (PurchaseOrderDetailId, CostCenterId);
	--SELECT *FROM #tbl_temp_purchaseorder_costcenter

	IF OBJECT_ID('tempdb..#tbl_temp_purchaseorder_top') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_purchaseorder_top
	END
	CREATE TABLE #tbl_temp_purchaseorder_top(
		Id bigint,
		PurchaseOrderId int,
		PaymentAmount money
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_purchaseorder_top AS(
		SELECT
		pot.Id, PurchaseOrderId, PaymentAmount
		FROM PurchaseOrderTOP pot WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_purchaseorder_top
	SELECT * FROM temp_purchaseorder_top
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_purchaseorder_top (PurchaseOrderId);
	--SELECT *FROM #tbl_temp_purchaseorder_top

	
    IF OBJECT_ID('tempdb..#tbl_temp_invoicepo_othercost') IS NOT NULL 
    BEGIN 
     DROP TABLE #tbl_temp_invoicepo_othercost
    END
    CREATE TABLE #tbl_temp_invoicepo_othercost(
     Id bigint,
     PONonShoppingDetailId int,
     InvoicePOId int,
     OtherCost_SubCategoryId int,
     Amount money,
     TotalBaseAmount money,
     InvoicePODetailId int,
     AmountGrossUp money,
     UNIQUE CLUSTERED (Id) 
    )
    ;WITH temp_invoicepo_othercost AS(
     SELECT ipoioc.Id, ipoioc.PONonShoppingDetailId, ipod.InvoicePOId as InvoicePOId, ipoioc.OtherCost_SubCategoryId, ipoioc.Amount, ipoioc.TotalBaseAmount, ipoioc.InvoicePODetailId, ipoioc.AmountGrossUp
     FROM InvoicePOItemOtherCost as ipoioc WITH (NOLOCK)
     inner join InvoicePODetail as ipod on ipod.Id = ipoioc.InvoicePODetailId
     where ipoioc.InvoicePODetailId is not null
    )
    INSERT INTO #tbl_temp_invoicepo_othercost
    SELECT * FROM temp_invoicepo_othercost
    CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_invoicepo_othercost (PONonShoppingDetailId, InvoicePOId, OtherCost_SubCategoryId);
    --SELECT *FROM #tbl_temp_invoicepo_othercost

	IF OBJECT_ID('tempdb..#tbl_temp_prf') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_prf
	END
	CREATE TABLE #tbl_temp_prf(
		Id bigint,
		ApprovalRequestId int,
		BudgetCode varchar(100),
		RequestDate datetime,
		RequestorUserName varchar(100),
		RepurchaseNotes varchar(500),
		Status smallint
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_prf AS(
		SELECT prf.Id, ApprovalRequestId, BudgetCode, RequestDate, prf.RequestorUserName, RepurchaseNotes, prf.Status
		FROM PRF prf WITH (NOLOCK)
		WHERE (@RequestStatus IS NULL OR @RequestStatus = '' OR Status = @RequestStatus)
          AND (@RequestDateFrom IS NULL OR @RequestDateFrom = '' OR RequestDate >= @RequestDateFrom)
          AND (@RequestDateTo IS NULL OR @RequestDateTo = '' OR RequestDate <= @RequestDateTo)
		  AND (@RequestorName IS NULL OR @RequestorName = '' OR RequestorUserName = @RequestorName)
	)
	INSERT INTO #tbl_temp_prf
	SELECT * FROM temp_prf
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_prf (ApprovalRequestId);
	--SELECT *FROM #tbl_temp_prf

	IF OBJECT_ID('tempdb..#tbl_temp_prf_summary') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_prf_summary
	END
	CREATE TABLE #tbl_temp_prf_summary(
		Id bigint,
		PRFId int,
		ApprovalRequestId int
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_prf_summary AS(
		SELECT pfs.Id, PRFId, ApprovalRequestId
		FROM PRFSummary pfs WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_prf_summary
	SELECT * FROM temp_prf_summary
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_prf_summary (PRFId, ApprovalRequestId);
	--SELECT *FROM #tbl_temp_prf_summary

	IF OBJECT_ID('tempdb..#tbl_temp_po_nonshop') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_po_nonshop
	END
	CREATE TABLE #tbl_temp_po_nonshop(
		Id bigint,
		PRFSummaryId int,
		PONumber varchar(100),
		VendorId int,
		RequestorName varchar(100)
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_po_nonshop AS(
		SELECT pns.Id, PRFSummaryId, PONumber, VendorId, RequestorName
		FROM PONonShopping pns WITH (NOLOCK)
		WHERE (@RequestNumber IS NULL OR @RequestNumber = '' OR PONumber = @RequestNumber)
	)
	INSERT INTO #tbl_temp_po_nonshop
	SELECT * FROM temp_po_nonshop
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_po_nonshop (PRFSummaryId, VendorId);
	--SELECT *FROM #tbl_temp_po_nonshop

	IF OBJECT_ID('tempdb..#tbl_temp_po_nonshop_top') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_po_nonshop_top
	END
	CREATE TABLE #tbl_temp_po_nonshop_top(
		Id bigint,
		PONonShoppingId int,
		PaymentAmount money
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_po_nonshop_top AS(
		SELECT pnstop.Id, PONonShoppingId, PaymentAmount
		FROM PONonShoppingTOP pnstop WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_po_nonshop_top
	SELECT * FROM temp_po_nonshop_top
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_po_nonshop_top (PONonShoppingId);
	--SELECT *FROM #tbl_temp_po_nonshop_top

	IF OBJECT_ID('tempdb..#tbl_temp_po_nonshop_detail') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_po_nonshop_detail
	END
	CREATE TABLE #tbl_temp_po_nonshop_detail(
		Id bigint,
		PONonShoppingId int,
		RateAmount money,
		ItemDescription varchar(250)
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_po_nonshop_detail AS(
		SELECT pnsd.Id, PONonShoppingId, RateAmount, ItemDescription
		FROM PONonShoppingDetail pnsd WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_po_nonshop_detail
	SELECT * FROM temp_po_nonshop_detail
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_po_nonshop_detail (PONonShoppingId);
	--SELECT *FROM #tbl_temp_po_nonshop_detail

	IF OBJECT_ID('tempdb..#tbl_temp_po_nonshop_costcenter') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_po_nonshop_costcenter
	END
	CREATE TABLE #tbl_temp_po_nonshop_costcenter(
		Id bigint,
		PONonShoppingDetailId int,
		CostCenterId int,
		TotalAmount money
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_po_nonshop_costcenter AS(
		SELECT pnscc.Id, PONonShoppingDetailId, CostCenterId, TotalAmount
		FROM PONonShoppingDetailCostCenter pnscc WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_po_nonshop_costcenter
	SELECT * FROM temp_po_nonshop_costcenter
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_po_nonshop_costcenter (PONonShoppingDetailId, CostCenterId);
	--SELECT *FROM #tbl_temp_po_nonshop_costcenter

	IF OBJECT_ID('tempdb..#tbl_temp_invoice_po') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_invoice_po
	END
	CREATE TABLE #tbl_temp_invoice_po(
		Id bigint,
		PurchaeseOrderId int,
		PurchaseOrderTOPId int,
		CategoryProcess_SubCategoryId int,
		InvoiceNumber varchar(100),
		InvoiceDate datetime,
		BankAccountNumber varchar(100),
		BankAccountOwnerName VARCHAR(250),
		BankCode varchar(100),
		BankName varchar(100),
		Remark varchar(500),
		LCurrency varchar(100),
		RateAmmount money,
		TotalAmount money
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_invoice_po AS(
		SELECT invpo.Id, PurchaeseOrderId, PurchaseOrderTOPId, CategoryProcess_SubCategoryId, InvoiceNumber, InvoiceDate, BankAccountNumber, BankAccountOwnerName, BankCode, BankName,
		Remark, LCurrency, RateAmmount, TotalAmount
		FROM InvoicePO invpo WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_invoice_po
	SELECT * FROM temp_invoice_po
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_invoice_po (PurchaeseOrderId, PurchaseOrderTOPId, CategoryProcess_SubCategoryId);
	--SELECT *FROM #tbl_temp_invoice_po

	IF OBJECT_ID('tempdb..#tbl_temp_deliverynotes_detail') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_deliverynotes_detail
	END
	CREATE TABLE #tbl_temp_deliverynotes_detail(
		Id bigint,
		PurchaseOrderDetailId int
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_deliverynotes_detail AS(
		SELECT Id, PurchaseOrderDetailId
		FROM DeliveryNotesDetail dnd WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_deliverynotes_detail
	SELECT * FROM temp_deliverynotes_detail
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_deliverynotes_detail (PurchaseOrderDetailId);
	--SELECT *FROM #tbl_temp_deliverynotes_detail

	IF OBJECT_ID('tempdb..#tbl_temp_deliverynotes_payment') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_deliverynotes_payment
	END
	CREATE TABLE #tbl_temp_deliverynotes_payment(
		Id bigint,
		PurchaseOrderTOPId bigint,
		Status smallint,
		CategoryProcess_SubCategoryId int,
		LastUpdatedTime datetime,
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_deliverynotes_payment AS(
		SELECT Id, PurchaseOrderTOPId, Status, CategoryProcess_SubCategoryId, LastUpdatedTime
		FROM DeliveryNotesPayment dnd WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_deliverynotes_payment
	SELECT * FROM temp_deliverynotes_payment
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_deliverynotes_payment (PurchaseOrderTOPId);
	--SELECT *FROM #tbl_temp_deliverynotes_payment

	IF OBJECT_ID('tempdb..#tbl_temp_item') IS NOT NULL 
	BEGIN 
		DROP TABLE #tbl_temp_item
	END
	CREATE TABLE #tbl_temp_item(
		Id bigint,
		Name varchar(100),
		UNIQUE CLUSTERED (Id) 
	)
	;WITH temp_item AS(
		SELECT
		Id, Name
		FROM Item WITH (NOLOCK)
	)
	INSERT INTO #tbl_temp_item
	SELECT * FROM temp_item
	CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_item (Id);
	--SELECT *FROM #tbl_temp_item
	END

	IF (@RequestType = 'TREX-APR' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		IF OBJECT_ID('tempdb..#tbl_temp_trexapr') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_trexapr
		END
		CREATE TABLE #tbl_temp_trexapr(
			[APRId] [varchar](50) NOT NULL,
			[NoAPR] [varchar](50) NULL,
			[TglAPR] [date] NULL,
			[RegionCode] [varchar](10) NULL,
			[DepartmentCode] [varchar](20) NULL,
			[AccountCode] [varchar](50) NULL,
			[AccountDescription] [varchar](50) NULL,
			[Description] [varchar](500) NULL,
			[Amount] [float] NULL,
			[TotalAmount] [float] NULL,
			[IsVendorPayment] [bit] NULL,
			[BankAccountNumber] [varchar](150) NULL,
			[BankName] [varchar](150) NULL,
			[BankAccountName] [varchar](150) NULL,
			[BeneficiaryEmployeeName] [varchar](150) NULL,
			[BeneficiaryVendorName] [varchar](150) NULL,
			[Remarks] [varchar](500) NULL,
			[SendToFinanceDates] DATETIME NULL,
			[RequestDates] DATETIME NULL,
			[RequestorName] [varchar](150) NULL,
			[Status] INT NULL
		)
		;WITH temp_trexapr AS(
			SELECT
			r.[APRId], r.[NoAPR], r.[TglAPR], r.[RegionCode], r.[DepartmentCode], r.[AccountCode], r.[AccountDescription], r.[Description], r.[Amount], r.[TotalAmount], r.[IsVendorPayment],
			r.BankAccountNumber, r.BankName, r.BankAccountName,
			r.[BeneficiaryEmployeeName], r.[BeneficiaryVendorName], r.[Remarks],  a.ApprovedAt [SendToFinanceDates], r.CreatedAt [RequestDates], r.CreatedBy [RequestorName], r.Status [Status]
			FROM TrexAPR r WITH (NOLOCK)
			LEFT JOIN (
			    SELECT a.AprId, MAX(v.ApprovedAt) [ApprovedAt]
			    FROM TrexAPR a
			    CROSS APPLY (VALUES
			        (a.Approval1At, a.Approval1By),
			        (a.Approval2At, a.Approval2By),
			        (a.Approval3At, a.Approval3By),
			        (a.Approval4At, a.Approval4By),
			        (a.Approval5At, a.Approval5By),
			        (a.Approval6At, a.Approval6By),
			        (a.Approval7At, a.Approval7By),
			        (a.Approval8At, a.Approval8By)
			    ) AS v(ApprovedAt, ApprovedBy)
			    WHERE v.ApprovedBy IS NOT NULL
				GROUP BY a.AprId
			) AS a ON r.AprId = a.AprId
			 )
		INSERT INTO #tbl_temp_trexapr
		SELECT * FROM temp_trexapr
		CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_trexapr (APRId);
	END

	IF (@RequestType = 'TREX-EER' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		IF OBJECT_ID('tempdb..#tbl_temp_trexeer') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_trexeer
		END
		CREATE TABLE #tbl_temp_trexeer(
			[EERId] [varchar](50) NOT NULL,
			[NoEER] [varchar](50) NULL,
			[TglEER] [date] NULL,
			[RegionCode] [varchar](10) NULL,
			[DepartmentCode] [varchar](20) NULL,
			[AccountCode] [varchar](50) NULL,
			[AccountName] [varchar](50) NULL,
			[Description] [varchar](500) NULL,
			[Amount] [float] NULL,
			[TotalAmount] [float] NULL,
			[BankAccountNumber] [varchar](150) NULL,
			[BankName] [varchar](150) NULL,
			[BankAccountName] [varchar](150) NULL,
			[BeneficiaryEmployeeName] [varchar](150) NULL,
			[Remarks] [varchar](500) NULL,
			[SendToFinanceDates] DATETIME NULL,
			[RequestDates] DATETIME NULL,
			[RequestorName] [varchar](150) NULL,
			[Status] INT NULL
		)
		;WITH temp_trexeer AS(
			SELECT
			r.[EERId], r.[NoEER], r.[TglEER], r.[RegionCode], r.[DepartmentCode], ISNULL(rm.AccountCode, rn.AccountCode) [AccountCode], ISNULL(rm.AccountDescription, rn.AccountDescription) [AccountName], 
			r.[Description], ISNULL(rn.[Amount],rm.Amount) [Amount], r.[TotalAmount], r.BankAccountNumber, r.BankName, r.BankAccountName,
			r.[BeneficiaryEmployeeName], r.[Remarks],  a.ApprovedAt [SendToFinanceDates], r.CreatedAt [RequestDates], r.CreatedBy [RequestorName], r.Status [Status]
			FROM TrexEERHeader r WITH (NOLOCK)
		    LEFT JOIN TrexEERNonMonthlyDetail rn on r.EERId = rn.EERId
		    LEFT JOIN TrexEERMonthlyDetail    rm on r.EERId = rm.EERId
			LEFT JOIN (
			    SELECT a.EERId, MAX(v.ApprovedAt) [ApprovedAt]
			    FROM TrexEERHeader a
			    CROSS APPLY (VALUES
			        (a.Approval1At, a.Approval1By),
			        (a.Approval2At, a.Approval2By),
			        (a.Approval3At, a.Approval3By),
			        (a.Approval4At, a.Approval4By),
			        (a.Approval5At, a.Approval5By),
			        (a.Approval6At, a.Approval6By),
			        (a.Approval7At, a.Approval7By),
			        (a.Approval8At, a.Approval8By)
			    ) AS v(ApprovedAt, ApprovedBy)
			    WHERE v.ApprovedBy IS NOT NULL
				GROUP BY a.EERId
			) AS a ON r.EERId = a.EERId
			 )
		INSERT INTO #tbl_temp_trexeer
		SELECT * FROM temp_trexeer
		CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_trexeer (EERId);
	END

	IF (@RequestType LIKE 'TREX-GER%'  OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		IF OBJECT_ID('tempdb..#tbl_temp_trexger') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_trexger
		END
		CREATE TABLE #tbl_temp_trexger(
			[GERId] [varchar](50) NOT NULL,
			[NoGER] [varchar](50) NULL,
			[TglGER] [date] NULL,
			[RegionCode] [varchar](10) NULL,
			[DepartmentCode] [varchar](20) NULL,
			[AccountCode] [varchar](50) NULL,
			[AccountName] [varchar](50) NULL,
			[GERType] [varchar](50) NULL,
			[Description] [varchar](500) NULL,
			[Amount] [float] NULL,
			[TotalAmount] [float] NULL,
			[BankAccountNumber] [varchar](150) NULL,
			[BankName] [varchar](150) NULL,
			[BankAccountName] [varchar](150) NULL,
			[BeneficiaryEmployeeName] [varchar](150) NULL,
			[Remarks] [varchar](500) NULL,
			[SendToFinanceDates] DATETIME NULL,
			[RequestDates] DATETIME NULL,
			[RequestorName] [varchar](150) NULL,
			[Status] INT NULL
		)
		;WITH temp_trexger AS(
			SELECT
			r.[GERId], r.[NoGER], r.[TglGER], r.[RegionCode], r.[DepartmentCode], '' [AccountCode], '' [AccountName], [GERType],
			r.[Description], r.PaidAmount [Amount], r.PaidAmount [TotalAmount], r.BankAccountNumber, r.BankName, r.BankAccountName,
			r.BeneficiaryVendorName [BeneficiaryEmployeeName], r.[Remarks],  a.ApprovedAt [SendToFinanceDates], r.CreatedAt [RequestDates], r.CreatedBy [RequestorName], r.Status [Status]
			FROM TrexGERHeader r WITH (NOLOCK)
			LEFT JOIN (
			    SELECT a.GERId, MAX(v.ApprovedAt) [ApprovedAt]
			    FROM TrexGERHeader a
			    CROSS APPLY (VALUES
			        (a.Approval1At, a.Approval1By),
			        (a.Approval2At, a.Approval2By),
			        (a.Approval3At, a.Approval3By),
			        (a.Approval4At, a.Approval4By),
			        (a.Approval5At, a.Approval5By),
			        (a.Approval6At, a.Approval6By),
			        (a.Approval7At, a.Approval7By),
			        (a.Approval8At, a.Approval8By)
			    ) AS v(ApprovedAt, ApprovedBy)
			    WHERE v.ApprovedBy IS NOT NULL
				GROUP BY a.GERId
			) AS a ON r.GERId = a.GERId
			 )
		INSERT INTO #tbl_temp_trexger
		SELECT * FROM temp_trexger
		CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_trexger (GERId);

		IF OBJECT_ID('tempdb..#tbl_temp_trexger_accountcode_detail') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_trexger_accountcode_detail
		END
		CREATE TABLE #tbl_temp_trexger_accountcode_detail(
			[GERAccountCodeId] [varchar](50) NOT NULL,
			[GERId] [varchar](50) NOT NULL,
			[AccountCode] [varchar](50) NULL,
			[AccountDescription] [varchar](500) NULL,
			[Description] [varchar](500) NULL,
			[Amount] [float] NULL,
			[Materai] [varchar](5) NULL,
			[TaxName1] [varchar](50) NULL,
			[TaxCode1] [varchar](50) NULL,
			[TaxRate1] [float] NULL,
			[TaxOperator1] [varchar](1) NULL,
			[TaxName2] [varchar](50) NULL,
			[TaxCode2] [varchar](50) NULL,
			[TaxRate2] [float] NULL,
			[TaxOperator2] [varchar](1) NULL,
			[TaxAmount] [float] NULL,
		)
		;WITH temp_trexger_accountcode_detail AS (
		SELECT
		    r.[GERAccountCodeId],
		    r.[GERId],
		    r.[AccountCode],
		    r.[AccountDescription],
		    r.[Description],
		    r.[Amount],
		    r.[Materai],
		    r.[TaxName1],
		    r.[TaxCode1],
		    r.[TaxRate1],
		    r.[TaxOperator1],
		    r.[TaxName2],
		    r.[TaxCode2],
		    r.[TaxRate2],
		    r.[TaxOperator2],
		    r.[TaxAmount]
		FROM TrexGERAccountCodeDetail r WITH (NOLOCK)
		)

		INSERT INTO #tbl_temp_trexger_accountcode_detail
		SELECT * FROM temp_trexger_accountcode_detail
		CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_trexger_accountcode_detail (GERId);

		IF OBJECT_ID('tempdb..#tbl_temp_trexger_entertain_detail') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_trexger_entertain_detail
		END
		CREATE TABLE #tbl_temp_trexger_entertain_detail(
			[GEREntertainId] [varchar](50) NOT NULL,
			[GERId] [varchar](50) NOT NULL,
			[TglKwitansi] [date] NULL,
			[AccountCode] [varchar](50) NULL,
			[AccountDescription] [varchar](500) NULL,
			[NameOfPersonEntertaint] [varchar](max) NULL,
			[Company] [varchar](100) NULL,
			[Place] [varchar](100) NULL,
			[Amount] [float] NULL,
			[PurposeOfGift] [varchar](100) NULL,
			[Remarks] [varchar](500) NULL,
			[Jabatan] [varchar](50) NULL,
			[OtherPurpose] [varchar](400) NULL,
			[EntertainmentType] [varchar](20) NULL,
			[TransactionType] [varchar](20) NULL,
			[IsRepresentBreachOfPolicy] [bit] NULL,
			[IsOver1MPerPerson] [bit] NULL,
			[TotalPerson] [int] NULL,
		)
		;WITH temp_trexger_entertain_detail AS (
		SELECT
		    r.[GEREntertainId],
		    r.[GERId],
		    r.[TglKwitansi],
		    r.[AccountCode],
		    r.[AccountDescription],
		    r.[NameOfPersonEntertaint],
		    r.[Company],
		    r.[Place],
		    r.[Amount],
		    r.[PurposeOfGift],
		    r.[Remarks],
		    r.[Jabatan],
		    r.[OtherPurpose],
		    r.[EntertainmentType],
		    r.[TransactionType],
		    r.[IsRepresentBreachOfPolicy],
		    r.[IsOver1MPerPerson],
		    r.[TotalPerson]
		FROM TrexGEREntertainDetail r WITH (NOLOCK)
	)
		INSERT INTO #tbl_temp_trexger_entertain_detail
		SELECT * FROM temp_trexger_entertain_detail
		CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_trexger_entertain_detail (GERId);
	END

	IF (@RequestType = 'TREX-TER' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		IF OBJECT_ID('tempdb..#tbl_temp_trexter') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_trexter
		END
		CREATE TABLE #tbl_temp_trexter(
			[TERId] [varchar](50) NOT NULL,
			[NoTER] [varchar](50) NULL,
			[TglPengajuan] [date] NULL,
			[RegionCode] [varchar](10) NULL,
			[DepartmentCode] [varchar](20) NULL,
			[AccountCode] [varchar](50) NULL,
			[AccountName] [varchar](50) NULL,
			[Description] [varchar](500) NULL,
			[Amount] [float] NULL,
			[TotalAmount] [float] NULL,
			[BankAccountNumber] [varchar](150) NULL,
			[BankName] [varchar](150) NULL,
			[BankAccountName] [varchar](150) NULL,
			[BeneficiaryEmployeeName] [varchar](150) NULL,
			[Remarks] [varchar](500) NULL,
			[SendToFinanceDates] DATETIME NULL,
			[RequestDates] DATETIME NULL,
			[RequestorName] [varchar](150) NULL,
			[Status] INT NULL
		)
		;WITH temp_trexter AS(
			SELECT
			r.[TERId], r.[NoTER], r.[TglPengajuan], '' [RegionCode], '' [DepartmentCode], '' [AccountCode], '' [AccountName], 
			'' [Description], r.[TotalExpense] [Amount], r.[TotalExpense] [TotalAmount], r.BankAccountNumber, r.BankName, r.BankAccountName,
			'' [BeneficiaryEmployeeName], r.[Remarks],  a.ApprovedAt [SendToFinanceDates], r.CreatedAt [RequestDates], r.CreatedBy [RequestorName], r.Status [Status]
			FROM TrexTERHeader r WITH (NOLOCK)
			LEFT JOIN (
			    SELECT a.TERId, MAX(v.ApprovedAt) [ApprovedAt]
			    FROM TrexTERHeader a
			    CROSS APPLY (VALUES
			        (a.Approval1At, a.Approval1By),
			        (a.Approval2At, a.Approval2By),
			        (a.Approval3At, a.Approval3By),
			        (a.Approval4At, a.Approval4By),
			        (a.Approval5At, a.Approval5By),
			        (a.Approval6At, a.Approval6By),
			        (a.Approval7At, a.Approval7By),
			        (a.Approval8At, a.Approval8By)
			    ) AS v(ApprovedAt, ApprovedBy)
			    WHERE v.ApprovedBy IS NOT NULL
				GROUP BY a.TERId
			) AS a ON r.TERId = a.TERId
			 )
		INSERT INTO #tbl_temp_trexter
		SELECT * FROM temp_trexter
		CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_trexter (TERId);
		IF OBJECT_ID('tempdb..#tbl_temp_trexter_detail_transportation') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_trexter_detail_transportation
		END
		CREATE TABLE #tbl_temp_trexter_detail_transportation(
			[TERDetailTransportationId] [varchar](50) NOT NULL,
			[TERId] [varchar](50) NULL,
			[TglTransportation] [date] NULL,
			[TipeTransportation] [varchar](50) NULL,
			[Amount] [float] NULL,
			[Remarks] [varchar](5000) NULL,
			[TotalKM] [float] NULL,
			[HargaBBM] [float] NULL,
			[MaxAmount] [float] NULL,
			[SesuaiTglBTR] [bit] NULL,
		)
		;WITH temp_trexter_detail_transportation AS (
		SELECT
		    r.[TERDetailTransportationId],
		    r.[TERId],
		    r.[TglTransportation],
		    r.[TipeTransportation],
		    r.[Amount],
		    r.[Remarks],
		    r.[TotalKM],
		    r.[HargaBBM],
		    r.[MaxAmount],
		    r.[SesuaiTglBTR]
		FROM TrexTERDetailTransportation r WITH (NOLOCK)
		)

		INSERT INTO #tbl_temp_trexter_detail_transportation
		SELECT * FROM temp_trexter_detail_transportation
		CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_trexter_detail_transportation (TERId);
		IF OBJECT_ID('tempdb..#tbl_temp_trexter_detail_akomodasi') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_trexter_detail_akomodasi
		END
		CREATE TABLE #tbl_temp_trexter_detail_akomodasi(
			[TERDetailAkomodasiId] [varchar](50) NOT NULL,
			[TERId] [varchar](50) NULL,
			[TglAkomodasi] [date] NULL,
			[TipeAkomodasi] [varchar](50) NULL,
			[Amount] [float] NULL,
			[Remarks] [varchar](5000) NULL,
			[NonRekanan] [bit] NULL,
			[SesuaiTglBTR] [bit] NULL,
		)
		;WITH temp_trexter_detail_akomodasi AS (
		SELECT
		    r.[TERDetailAkomodasiId],
		    r.[TERId], 
		    r.[TglAkomodasi], 
		    r.[TipeAkomodasi],
		    r.[Amount],
		    r.[Remarks],
		    r.NonRekanan,       
		    r.SesuaiTglBTR  
		FROM TrexTERDetailAkomodasi r WITH (NOLOCK)
		)

		INSERT INTO #tbl_temp_trexter_detail_akomodasi
		SELECT * FROM temp_trexter_detail_akomodasi
		CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_trexter_detail_akomodasi (TERId);
		IF OBJECT_ID('tempdb..#tbl_temp_trexter_detail_durasi') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_trexter_detail_durasi
		END
		CREATE TABLE #tbl_temp_trexter_detail_durasi(
			[TERDetailDurasiId] [varchar](50) NOT NULL,
			[TERId] [varchar](50) NULL,
			[TglDurasi] [date] NULL,
			[Durasi] [varchar](30) NULL,
			[TipeZona] [varchar](50) NULL,
			[Amount] [float] NULL,
			[Remarks] [varchar](5000) NULL,
			[IsHaveWork] [bit] NULL,
			[SesuaiTglBTR] [bit] NULL,
		)		
		;WITH temp_trexter_detail_durasi AS (
		SELECT
		    r.[TERDetailDurasiId],
		    r.[TERId],
		    r.[TglDurasi],
		    r.[Durasi],
		    r.[TipeZona],
		    r.[Amount],
		    r.[Remarks],
		    r.[IsHaveWork],
		    r.[SesuaiTglBTR]
		FROM TrexTERDetailDurasi r WITH (NOLOCK)
		)

		INSERT INTO #tbl_temp_trexter_detail_durasi
		SELECT * FROM temp_trexter_detail_durasi
		CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_trexter_detail_durasi (TERId);
		IF OBJECT_ID('tempdb..#tbl_temp_trexter_detail_other') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_trexter_detail_other
		END
		CREATE TABLE #tbl_temp_trexter_detail_other(
			[TERDetailOtherId] [varchar](50) NOT NULL,
			[TERId] [varchar](50) NULL,
			[TglOther] [date] NULL,
			[TipeOther] [varchar](50) NULL,
			[Amount] [float] NULL,
			[Remarks] [varchar](5000) NULL,
			[TotalKM] [float] NULL,
			[HargaBBM] [float] NULL,
			[MaxAmount] [float] NULL,
			[SesuaiTglBTR] [bit] NULL,
		)	
		;WITH temp_trexter_detail_other AS (
		SELECT
		    r.[TERDetailOtherId],
		    r.[TERId],
		    r.[TglOther],
		    r.[TipeOther],
		    r.[Amount],
		    r.[Remarks],
		    r.[TotalKM],
		    r.[HargaBBM],
		    r.[MaxAmount],
		    r.[SesuaiTglBTR]
		FROM TrexTERDetailOther r WITH (NOLOCK)
		)

		INSERT INTO #tbl_temp_trexter_detail_other
		SELECT * FROM temp_trexter_detail_other
		CREATE  NONCLUSTERED INDEX [IX_num] ON #tbl_temp_trexter_detail_other (TERId);
	END

	DECLARE @subquery2 VARCHAR(MAX) = 'am.Id = @AccountMasterId'
	DECLARE @paramSLA NVARCHAR(MAX) = N'@CutOffHour NVARCHAR(100), @BusinessUnitId NVARCHAR(100), @CostCenterId NVARCHAR(100), @AccountMasterId NVARCHAR(100)'

	IF (@RequestType = 'reimbursement' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		DECLARE @subquery VARCHAR(MAX) = 'bu.Id = @BusinessUnitId AND cc.Id = @CostCenterId'
		IF (@BusinessUnitId IS NULL OR @BusinessUnitId = '')
		BEGIN
			SET @subquery = REPLACE(@subquery, 'bu.Id = @BusinessUnitId', '1=1')
		END
		IF (@CostCenterId IS NULL OR @CostCenterId = '')
		BEGIN
			SET @subquery = REPLACE(@subquery, 'cc.Id = @CostCenterId', '1=1')
		END
		IF (@AccountMasterId IS NULL OR @AccountMasterId = '')
		BEGIN
			SET @subquery2 = REPLACE(@subquery2, 'am.Id = @AccountMasterId', '1=1')
		END

		DECLARE @sqlQueryRI NVARCHAR(MAX) 
		SET @sqlQueryRI = '			
		SELECT DISTINCT 
		vh.TransferNumber, vh.VoucherNumber, vh.Id [VoucherId], ar.RequestNo RequestNumber, ar.RequestorUserName RequestorName, 
		'''' SettlementNumber, vh.CreatedBy MakerFinance,
		r.Status RequestStatus, r.Description, vd.StatusTransfer,
		(	CASE WHEN vd.StatusTransfer = 1 THEN ''Success''
					WHEN vd.StatusTransfer = 0 THEN ''Failed''
					ELSE ''''
					END
		)	[StatusTransferDesc], 
		scv.Id [VendorCategoryId], scv.SubCategoryName VendorType, (SELECT DISTINCT TOP 1 v.[Id]) VendorId, (SELECT DISTINCT TOP 1 v.[Name]) VendorName,
		(CASE WHEN scv.SubCategoryName = ''Staff'' THEN (SELECT DISTINCT TOP 1 v.EmployeeCode)
		ELSE (SELECT DISTINCT TOP 1 v.Code) END) VendorCode, 
		''Reimbursement'' RequestType,
		MONTH(apprv.ApprovalDate) [Month],
		r.RequestDate,
		CONVERT(VARCHAR(20),r.RequestDate,113) RequestDateString,
		apprv.ApprovalDate ReceivedByFinanceDate,
		CONVERT(VARCHAR(20),apprv.ApprovalDate,113) ReceivedByFinance,
		CONVERT(VARCHAR(20),vh.TransferTime,113) PaidByFinance,
		'''' SettlementDate,
		'''' ReceivedSettlementByFinance,
		(CASE WHEN r.Status = 4 THEN CONCAT(''[Repair]'', '' - '', r.ReasonReject) ELSE '''' END) StatusRepair,
		CONVERT(VARCHAR(20),DATEADD(DAY, 31, vh.TransferTime),113) DueDate,
		'''' OverdueDays,
		'''' StatusOverdue,
		'''' [BalanceAmount],
		'''' [DueToCompany],
		'''' [TransferDateDueToCompany],
		'''' [RealizationAmount],
		(CASE WHEN (DATEDIFF(year,apprv.ApprovalDate, vh.TransferTime))>=1
		THEN
		((DATEDIFF(DAY, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime))
		- 2 * ((DATEPART(week, vh.TransferTime) + 52 * 
		DATEDIFF(year,
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime)) 
		- DATEPART(week, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END))))
		- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
		ELSE
		((DATEDIFF(DAY, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime)) 
		- 2 * (DATEPART(week, vh.TransferTime) 
		- DATEPART(week, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END))))
		- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
		END) [SLA],
		'''' NewAdvanceNumber, '''' NewAdvanceAmount, '''' NewAdvanceTransferDate, 
		detail.x Detail,
		''-'' [Product], ''-'' [ProjectNo], ''-'' [Affiliate],
		(SELECT DISTINCT TOP 1 rd.BankAccountOwnerName) [Beneficiaries],
		(SELECT DISTINCT TOP 1 rd.BankAccountNumber) [BankAccountNumber],
		(SELECT DISTINCT TOP 1 rd.BankName) [BankName],
		(SELECT DISTINCT TOP 1 rd.L_Currency) [LCurrencyCode],
		(SELECT DISTINCT TOP 1 REPLACE(FORMAT(rd.RateAmount, ''C''),''$'','''')) [Rate],
		REPLACE(FORMAT(detailamount.TotalAmount, ''C''),''$'','''') [Amount], 
		REPLACE(FORMAT(costcenter.TotalAmount, ''C''),''$'','''') [NettAmount],
		othercost.ot [OtherCosts], costsplit.x [CostSplit], apprv.member ApprovalGroupMembers,
		'''' DocumentNumber,
		Attachments.AttachmentIds,
		'''' PPH21,
		'''' PPN
		FROM #tbl_temp_approvalrequest ar 
		JOIN #tbl_temp_reimbursement r ON ar.RequestNo = r.RequestNumber
		JOIN #tbl_temp_reimbursement_detail rd ON r.Id = rd.ReimbursementId
		JOIN #tbl_temp_subcategory scv ON rd.ExpenseGeneral_SubCategoryId = scv.Id
		JOIN #tbl_temp_vendor v ON rd.VendorId = v.Id
		OUTER APPLY (SELECT SUM(rd1.Amount) TotalAmount
						FROM #tbl_temp_reimbursement_detail rd1
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						WHERE r1.RequestNumber = r.RequestNumber
					) detailamount
		OUTER APPLY (	SELECT (SELECT DISTINCT AccountMasterId, am.AccountCode AccountMasterCode, am.ShortDescription AccountMasterName, MtAccountType, rd1.InvoiceNo, rd1.[Description] DescriptionDetail
						FROM #tbl_temp_reimbursement_detail rd1
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						JOIN #tbl_temp_accountmaster am ON rd1.AccountMasterId = am.Id
						WHERE r1.RequestNumber = r.RequestNumber 
						AND ' + @subquery2 + '
						FOR JSON PATH) x
					) detail
		JOIN #tbl_temp_voucher_detail vd ON ar.RequestNo = vd.VoucherRefId
		JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
		JOIN #tmp_argm apprv ON ar.[Id] = apprv.[ApprovalRequestId]
		OUTER APPLY ( SELECT(
						SELECT 
							REPLACE(FORMAT(SUM(roc1.BasicAmount), ''C''),''$'','''') BasicAmount, 
							REPLACE(FORMAT(SUM(roc1.Amount), ''C''),''$'','''') Amount, 
							REPLACE(FORMAT(SUM(ISNULL(roc1.GrossUp,0)), ''C''),''$'','''') GrossUp,
							scoc.SubCategoryCode OtherCostSubCategoryCode
						FROM #tbl_temp_reimbursement_detail_othercost roc1
						JOIN #tbl_temp_reimbursement_detail rd1 ON roc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_subcategory scoc ON roc1.OtherCost_SubCategoryId = scoc.Id
						WHERE rd1.ReimbursementId = r.Id
						GROUP BY scoc.SubCategoryCode
						FOR JSON PATH) ot
						) othercost
		OUTER APPLY (SELECT SUM(rcc1.Amount) TotalAmount
						FROM #tbl_temp_reimbursement_detail_costcenter rcc1
						JOIN #tbl_temp_reimbursement_detail rd1 ON rcc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						WHERE r1.RequestNumber = r.RequestNumber
					) costcenter
		OUTER APPLY (	SELECT (SELECT DISTINCT bu.Id BusinessUnitId, bu.Name BusinessUnitName, bu.Code BusinessUnitCode, 
						cc.Id CostCenterId, cc.[Name] CostCenterName, cc.Code CostCenterCode
						FROM #tbl_temp_reimbursement_detail_costcenter rcc1
						JOIN #tbl_temp_businessunit bu ON rcc1.BusinessUnitId = bu.Id
						JOIN #tbl_temp_costcenter cc ON rcc1.CostCenterId = cc.Id
						JOIN #tbl_temp_reimbursement_detail rd1 ON rcc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						WHERE r1.RequestNumber = r.RequestNumber 
						AND ' + @subquery + '
						FOR JSON PATH) x
					) costsplit
		OUTER APPLY (	SELECT (SELECT DISTINCT rd1.AttachmentId AttachmentIdRequest, vh.Attachment AttachmentIdVoucher
						FROM #tbl_temp_reimbursement_detail_costcenter rcc1
						JOIN #tbl_temp_reimbursement_detail rd1 ON rcc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						JOIN #tbl_temp_voucher_detail vd ON r1.RequestNumber = vd.VoucherRefId
						JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
						WHERE r1.RequestNumber = r.RequestNumber 
						FOR JSON PATH) AttachmentIds
					) Attachments
		WHERE costsplit.x IS NOT NULL AND RequestNumber LIKE ''RI%'''

		IF OBJECT_ID('tempdb..#tbl_temp_transaction_reimbursement') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_reimbursement
		END
		CREATE TABLE #tbl_temp_transaction_reimbursement(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(500),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(100),
			VendorId int,
			VendorName varchar(100),
			VendorCode varchar(100),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(100),
			ProjectNo varchar(100),
			Affiliate varchar(100),
			Beneficiaries varchar(250),
			BankAccountNumber varchar(100),
			BankName varchar(100),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(100),
			PPN varchar(100)
		)
		INSERT INTO #tbl_temp_transaction_reimbursement
		EXEC sp_executesql @sqlQueryRI, @paramSLA, @CutOffHour, @BusinessUnitId, @CostCenterId, @AccountMasterId

		--SELECT *FROM #tbl_temp_transaction_reimbursement
	END

	IF (@RequestType = 'cash advance' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		DECLARE @subqueryCA VARCHAR(MAX) = 'bu.Id = @BusinessUnitId AND cc.Id = @CostCenterId'
		IF (@BusinessUnitId IS NULL OR @BusinessUnitId = '')
		BEGIN
			SET @subqueryCA = REPLACE(@subqueryCA, 'bu.Id = @BusinessUnitId', '1=1')
		END
		IF (@CostCenterId IS NULL OR @CostCenterId = '')
		BEGIN
			SET @subqueryCA = REPLACE(@subqueryCA, 'cc.Id = @CostCenterId', '1=1')
		END
		IF (@AccountMasterId IS NULL OR @AccountMasterId = '')
		BEGIN
			SET @subquery2 = REPLACE(@subquery2, 'am.Id = @AccountMasterId', '1=1')
		END

		DECLARE @sqlQueryCA NVARCHAR(MAX) 
		SET @sqlQueryCA = '			
		SELECT DISTINCT 
		vh.TransferNumber, vh.VoucherNumber, vh.Id [VoucherId], ar.RequestNo RequestNumber, ar.RequestorUserName RequestorName, 
		(CASE WHEN apprvSTL.ApprovalDate IS NULL OR apprvSTL.ApprovalDate = '''' THEN '''' ELSE s.SettlementNumber END) SettlementNumber, vh.CreatedBy MakerFinance,
		r.Status RequestStatus, r.Description, vd.StatusTransfer,
		(	CASE WHEN vd.StatusTransfer = 1 THEN ''Success''
					WHEN vd.StatusTransfer = 0 THEN ''Failed''
					ELSE ''''
					END
		)	[StatusTransferDesc], 
		scv.Id [VendorCategoryId], scv.SubCategoryName VendorType, (SELECT DISTINCT TOP 1 v.[Id]) VendorId, (SELECT DISTINCT TOP 1 v.[Name]) VendorName,
		(CASE WHEN scv.SubCategoryName = ''Staff'' THEN (SELECT DISTINCT TOP 1 v.EmployeeCode)
		ELSE (SELECT DISTINCT TOP 1 v.Code) END) VendorCode, 
		''Cash Advance'' RequestType,
		MONTH(apprv.ApprovalDate) [Month],
		r.RequestDate,
		CONVERT(VARCHAR(20),r.RequestDate,113) RequestDateString,
		apprv.ApprovalDate ReceivedByFinanceDate,
		CONVERT(VARCHAR(20),apprv.ApprovalDate,113) ReceivedByFinance,
		CONVERT(VARCHAR(20),vh.TransferTime,113) PaidByFinance,
		CONVERT(VARCHAR(20),apprvSTL.ApprovalDate,113) SettlementDate,
		CONVERT(VARCHAR(20), (
								CASE 
									WHEN apprvSTL.ApprovalDate IS NULL THEN NULL
									WHEN s.SettlementDate < apprvSTL.ApprovalDate THEN apprvSTL.ApprovalDate 
									ELSE s.SettlementDate 
								END)
							,113) ReceivedSettlementByFinance,
		(CASE WHEN s.Status = 4 THEN CONCAT(''[Repair]'', '' - '', s.ReasonReject) ELSE '''' END) StatusRepair,
		CONVERT(VARCHAR(20),DATEADD(DAY, 31, vh.TransferTime),113) DueDate,
		(CASE WHEN (r.RefNumber != null OR r.RefNumber != '''') THEN ''''
				WHEN DATEDIFF(DAY, DATEADD(DAY, 31, vh.TransferTime), getdate() ) > 0 AND apprvSTL.ApprovalDate IS NULL THEN CAST(DATEDIFF(DAY, DATEADD(DAY, 31, vh.TransferTime), getdate())AS VARCHAR) 
				ELSE CAST(0 AS VARCHAR) END) OverdueDays,
		(CASE WHEN ar.RequestNo NOT LIKE ''CA%'' THEN ''''
				WHEN (apprvSTL.ApprovalDate IS NOT NULL) OR (r.RefNumber != null OR r.RefNumber != '''') THEN ''Settled''
				WHEN (DATEDIFF(DAY, DATEADD(DAY, 31, vh.TransferTime), getdate())) > 0 THEN ''Overdue'' 
				ELSE ''Current'' END) StatusOverdue,
		(SELECT REPLACE(FORMAT(ISNULL(ISNULL(vd.TotalOriginalAmmount, 0)- ISNULL(SUM(sd.Amount), 0), 0), ''C''), ''$'', '''') FROM SettlementDetail sd WHERE sd.SettlementId = s.Id) [BalanceAmount],
		(SELECT TOP 1 REPLACE(FORMAT(ISNULL(SUM(sd1.Amount), 0), ''C''), ''$'', '''') FROM SettlementDetail sd1 WHERE sd1.SettlementId = s.Id AND sd1.TransferDate IS NOT NULL) [DueToCompany],
		(SELECT TOP 1 CONVERT(VARCHAR(20),sd1.TransferDate,106) FROM SettlementDetail sd1 WHERE sd1.SettlementId = s.Id AND sd1.TransferDate IS NOT NULL) [TransferDateDueToCompany],
		(SELECT TOP 1 REPLACE(FORMAT(ISNULL(SUM(sd1.Amount), 0), ''C''), ''$'', '''') FROM SettlementDetail sd1 WHERE sd1.SettlementId = s.Id AND sd1.TransferDate IS NULL) [RealizationAmount],
		(CASE WHEN (DATEDIFF(year,apprv.ApprovalDate, vh.TransferTime))>=1
		THEN
		((DATEDIFF(DAY, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime))
		- 2 * ((DATEPART(week, vh.TransferTime) + 52 * 
		DATEDIFF(year,
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime)) 
		- DATEPART(week, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END))))
		- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
		ELSE
		((DATEDIFF(DAY, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime)) 
		- 2 * (DATEPART(week, vh.TransferTime) 
		- DATEPART(week, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END))))
		- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
		END) [SLA],
		newCashAdvance.NewAdvanceNumber, REPLACE(FORMAT(ISNULL(newCashAdvance.NewAdvanceAmount, 0), ''C''), ''$'', '''') NewAdvanceAmount, CONVERT(VARCHAR(20),newCashAdvance.TransferTime,113) NewAdvanceTransferDate, 
		detail.x Detail,
		''-'' [Product], ''-'' [ProjectNo], ''-'' [Affiliate],
		(SELECT DISTINCT TOP 1 rd.BankAccountOwnerName) [Beneficiaries],
		(SELECT DISTINCT TOP 1 rd.BankAccountNumber) [BankAccountNumber],
		(SELECT DISTINCT TOP 1 rd.BankName) [BankName],
		(SELECT DISTINCT TOP 1 rd.L_Currency) [LCurrencyCode],
		(SELECT DISTINCT TOP 1 REPLACE(FORMAT(rd.RateAmount, ''C''),''$'','''')) [Rate],
		REPLACE(FORMAT(detailamount.TotalAmount, ''C''),''$'','''') [Amount], 
		REPLACE(FORMAT(costcenter.TotalAmount, ''C''),''$'','''') [NettAmount],
		othercost.ot [OtherCosts], costsplit.x [CostSplit], apprv.member ApprovalGroupMembers,
		'''' DocumentNumber,
		Attachments.AttachmentIds,
		'''' PPH21,
		'''' PPN
		FROM #tbl_temp_approvalrequest ar 
		JOIN #tbl_temp_reimbursement r ON ar.RequestNo = r.RequestNumber
		JOIN #tbl_temp_reimbursement_detail rd ON r.Id = rd.ReimbursementId
		JOIN #tbl_temp_subcategory scv ON rd.ExpenseGeneral_SubCategoryId = scv.Id
		JOIN #tbl_temp_vendor v ON rd.VendorId = v.Id
		OUTER APPLY (SELECT SUM(rd1.Amount) TotalAmount
						FROM #tbl_temp_reimbursement_detail rd1
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						WHERE r1.RequestNumber = r.RequestNumber
					) detailamount
		OUTER APPLY (	SELECT (SELECT DISTINCT AccountMasterId, am.AccountCode AccountMasterCode, am.ShortDescription AccountMasterName, MtAccountType, rd1.InvoiceNo, rd1.[Description] DescriptionDetail
						FROM #tbl_temp_reimbursement_detail rd1
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						JOIN #tbl_temp_accountmaster am ON rd1.AccountMasterId = am.Id
						WHERE r1.RequestNumber = r.RequestNumber 
						AND ' + @subquery2 + '
						FOR JSON PATH) x
					) detail
		JOIN #tbl_temp_voucher_detail vd ON ar.RequestNo = vd.VoucherRefId
		JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
		JOIN #tmp_argm apprv ON ar.[Id] = apprv.[ApprovalRequestId]
		OUTER APPLY ( SELECT(
						SELECT 
							REPLACE(FORMAT(SUM(roc1.BasicAmount), ''C''),''$'','''') BasicAmount, 
							REPLACE(FORMAT(SUM(roc1.Amount), ''C''),''$'','''') Amount, 
							REPLACE(FORMAT(SUM(ISNULL(roc1.GrossUp,0)), ''C''),''$'','''') GrossUp,
							scoc.SubCategoryCode OtherCostSubCategoryCode
						FROM #tbl_temp_reimbursement_detail_othercost roc1
						JOIN #tbl_temp_reimbursement_detail rd1 ON roc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_subcategory scoc ON roc1.OtherCost_SubCategoryId = scoc.Id
						WHERE rd1.ReimbursementId = r.Id
						GROUP BY scoc.SubCategoryCode
						FOR JSON PATH) ot
						) othercost
		OUTER APPLY (SELECT SUM(rcc1.Amount) TotalAmount
						FROM #tbl_temp_reimbursement_detail_costcenter rcc1
						JOIN #tbl_temp_reimbursement_detail rd1 ON rcc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						WHERE r1.RequestNumber = r.RequestNumber
					) costcenter
		OUTER APPLY (	SELECT (SELECT DISTINCT bu.Id BusinessUnitId, bu.Name BusinessUnitName, bu.Code BusinessUnitCode, 
						cc.Id CostCenterId, cc.[Name] CostCenterName, cc.Code CostCenterCode
						FROM #tbl_temp_reimbursement_detail_costcenter rcc1
						JOIN #tbl_temp_businessunit bu ON rcc1.BusinessUnitId = bu.Id
						JOIN #tbl_temp_costcenter cc ON rcc1.CostCenterId = cc.Id
						JOIN #tbl_temp_reimbursement_detail rd1 ON rcc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						WHERE r1.RequestNumber = r.RequestNumber 
						AND ' + @subqueryCA + '
						FOR JSON PATH) x
					) costsplit
		LEFT JOIN #tbl_temp_settlement s ON vh.Id = s.VoucherId AND r.Id = s.ReimbursementId
		LEFT JOIN #tmp_argm apprvSTL  ON s.SettlementNumber = apprvSTL.[RequestNo] 
			AND s.Status IN (2,7,9)
			AND apprvSTL.ApprovalFlowId != (SELECT DISTINCT [Id] 
											FROM ApprovalFlow 
											WHERE [Name] = ''FlowFinanceSettlement'')
		OUTER APPLY (SELECT TOP 1 Id FROM ApprovalFlow af WHERE af.Name = ''FlowFinanceSettlement'') flowstl
		OUTER APPLY (SELECT SUM(rccs.Amount) NewAdvanceAmount, rs.RequestNumber NewAdvanceNumber, vhs.TransferTime 
						FROM #tbl_temp_reimbursement rs 
						JOIN #tbl_temp_reimbursement_detail rds ON rs.Id = rds.ReimbursementId 
						JOIN #tbl_temp_reimbursement_detail_costcenter rccs ON rds.Id = rccs.ReimbursementDetailId
						LEFT JOIN #tbl_temp_voucher_detail vds ON rs.RequestNumber = vds.VoucherRefId
						LEFT JOIN #tbl_temp_voucher_header vhs ON vds.VoucherId = vhs.Id
						WHERE rs.RefNumber = s.SettlementNumber AND rs.Status NOT IN (3, 5)
						GROUP BY rs.RequestNumber, vhs.TransferTime) newCashAdvance
		LEFT JOIN #tbl_temp_approvalrequest ars ON s.SettlementNumber = ars.RequestNo AND ars.ApprovalFlowId = flowstl.Id
		OUTER APPLY (	SELECT (SELECT DISTINCT rd1.AttachmentId AttachmentIdRequest, vh.Attachment AttachmentIdVoucher
						FROM #tbl_temp_reimbursement_detail_costcenter rcc1
						JOIN #tbl_temp_reimbursement_detail rd1 ON rcc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						JOIN #tbl_temp_voucher_detail vd ON r1.RequestNumber = vd.VoucherRefId
						JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
						WHERE r1.RequestNumber = r.RequestNumber 
						FOR JSON PATH) AttachmentIds
					) Attachments
		WHERE costsplit.x IS NOT NULL AND RequestNumber LIKE ''CA%'' AND RequestNumber NOT LIKE ''CATR%'''

		IF OBJECT_ID('tempdb..#tbl_temp_transaction_cash_advance') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_cash_advance
		END
		CREATE TABLE #tbl_temp_transaction_cash_advance(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(500),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(100),
			VendorId int,
			VendorName varchar(100),
			VendorCode varchar(100),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(100),
			ProjectNo varchar(100),
			Affiliate varchar(100),
			Beneficiaries varchar(250),
			BankAccountNumber varchar(100),
			BankName varchar(100),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(100),
			PPN varchar(100)
		)
		INSERT INTO #tbl_temp_transaction_cash_advance
		EXEC sp_executesql @sqlQueryCA, @paramSLA, @CutOffHour, @BusinessUnitId, @CostCenterId, @AccountMasterId

		--SELECT *FROM #tbl_temp_transaction_cash_advance
	END

	IF (@RequestType = 'cash advance travel' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		DECLARE @subqueryCATR VARCHAR(MAX) = 'bu.Id = @BusinessUnitId AND cc.Id = @CostCenterId'
		IF (@BusinessUnitId IS NULL OR @BusinessUnitId = '')
		BEGIN
			SET @subqueryCATR = REPLACE(@subqueryCATR, 'bu.Id = @BusinessUnitId', '1=1')
		END
		IF (@CostCenterId IS NULL OR @CostCenterId = '')
		BEGIN
			SET @subqueryCATR = REPLACE(@subqueryCATR, 'cc.Id = @CostCenterId', '1=1')
		END
		IF (@AccountMasterId IS NULL OR @AccountMasterId = '')
		BEGIN
			SET @subquery2 = REPLACE(@subquery2, 'am.Id = @AccountMasterId', '1=1')
		END

		DECLARE @sqlQueryCATR NVARCHAR(MAX) 
		SET @sqlQueryCATR = '			
		SELECT DISTINCT 
		vh.TransferNumber, vh.VoucherNumber, vh.Id [VoucherId], ar.RequestNo RequestNumber, ar.RequestorUserName RequestorName, 
		(CASE WHEN apprvSTL.ApprovalDate IS NULL OR apprvSTL.ApprovalDate = '''' THEN '''' ELSE tre.RequestNumber END) SettlementNumber, vh.CreatedBy MakerFinance,
		r.Status RequestStatus, r.Description, vd.StatusTransfer,
		(	CASE WHEN vd.StatusTransfer = 1 THEN ''Success''
					WHEN vd.StatusTransfer = 0 THEN ''Failed''
					ELSE ''''
					END
		)	[StatusTransferDesc], 
		scv.Id [VendorCategoryId], scv.SubCategoryName VendorType, (SELECT DISTINCT TOP 1 v.[Id]) VendorId, (SELECT DISTINCT TOP 1 v.[Name]) VendorName,
		(CASE WHEN scv.SubCategoryName = ''Staff'' THEN (SELECT DISTINCT TOP 1 v.EmployeeCode)
		ELSE (SELECT DISTINCT TOP 1 v.Code) END) VendorCode, 
		''Cash Advance Travel'' RequestType,
		MONTH(apprv.ApprovalDate) [Month],
		r.RequestDate,
		CONVERT(VARCHAR(20),r.RequestDate,113) RequestDateString,
		apprv.ApprovalDate ReceivedByFinanceDate,
		CONVERT(VARCHAR(20),apprv.ApprovalDate,113) ReceivedByFinance,
		CONVERT(VARCHAR(20),vh.TransferTime,113) PaidByFinance,
		CONVERT(VARCHAR(20),apprvSTL.ApprovalDate,113) SettlementDate,
		CONVERT(VARCHAR(20),apprvSTL.ApprovalDate,113) ReceivedSettlementByFinance,
		(CASE WHEN tre.Status = 4 THEN CONCAT(''[Repair]'', '' - '', tre.ReasonReject) ELSE '''' END) StatusRepair,
		CONVERT(VARCHAR(20),DATEADD(DAY, 31, vh.TransferTime),113) DueDate,
		(CASE WHEN (r.RefNumber != null OR r.RefNumber != '''') THEN ''''
				WHEN DATEDIFF(DAY, DATEADD(DAY, 31, vh.TransferTime), getdate() ) > 0 AND apprvSTL.ApprovalDate IS NULL THEN CAST(DATEDIFF(DAY, DATEADD(DAY, 31, vh.TransferTime), getdate())AS VARCHAR) 
				ELSE CAST(0 AS VARCHAR) END) OverdueDays,
		(CASE WHEN ar.RequestNo NOT LIKE ''CA%'' THEN ''''
				WHEN (apprvSTL.ApprovalDate IS NOT NULL) OR (r.RefNumber != null OR r.RefNumber != '''') THEN ''Settled''
				WHEN (DATEDIFF(DAY, DATEADD(DAY, 31, vh.TransferTime), getdate())) > 0 THEN ''Overdue'' 
				ELSE ''Current'' END) StatusOverdue,
		'''' [BalanceAmount],
		'''' [DueToCompany],
		'''' [TransferDateDueToCompany],
		'''' [RealizationAmount],
		(CASE WHEN (DATEDIFF(year,apprv.ApprovalDate, vh.TransferTime))>=1
		THEN
		((DATEDIFF(DAY, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime))
		- 2 * ((DATEPART(week, vh.TransferTime) + 52 * 
		DATEDIFF(year,
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime)) 
		- DATEPART(week, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END))))
		- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
		ELSE
		((DATEDIFF(DAY, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime)) 
		- 2 * (DATEPART(week, vh.TransferTime) 
		- DATEPART(week, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END))))
		- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
		END) [SLA],
		'''' NewAdvanceNumber, '''' NewAdvanceAmount, '''' NewAdvanceTransferDate, 
		detail.x Detail,
		''-'' [Product], ''-'' [ProjectNo], ''-'' [Affiliate],
		(SELECT DISTINCT TOP 1 rd.BankAccountOwnerName) [Beneficiaries],
		(SELECT DISTINCT TOP 1 rd.BankAccountNumber) [BankAccountNumber],
		(SELECT DISTINCT TOP 1 rd.BankName) [BankName],
		(SELECT DISTINCT TOP 1 rd.L_Currency) [LCurrencyCode],
		(SELECT DISTINCT TOP 1 REPLACE(FORMAT(rd.RateAmount, ''C''),''$'','''')) [Rate],
		REPLACE(FORMAT(detailamount.TotalAmount, ''C''),''$'','''') [Amount], 
		REPLACE(FORMAT(costcenter.TotalAmount, ''C''),''$'','''') [NettAmount],
		othercost.ot [OtherCosts], costsplit.x [CostSplit], apprv.member ApprovalGroupMembers,
		'''' DocumentNumber,
		Attachments.AttachmentIds,
		'''' PPH21,
		'''' PPN
		FROM #tbl_temp_approvalrequest ar 
		JOIN #tbl_temp_reimbursement r ON ar.RequestNo = r.RequestNumber
		JOIN #tbl_temp_reimbursement_detail rd ON r.Id = rd.ReimbursementId
		JOIN #tbl_temp_subcategory scv ON rd.ExpenseGeneral_SubCategoryId = scv.Id
		JOIN #tbl_temp_vendor v ON rd.VendorId = v.Id
		OUTER APPLY (SELECT SUM(rd1.Amount) TotalAmount
						FROM #tbl_temp_reimbursement_detail rd1
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						WHERE r1.RequestNumber = r.RequestNumber
					) detailamount
		OUTER APPLY (	SELECT (SELECT DISTINCT AccountMasterId, am.AccountCode AccountMasterCode, am.ShortDescription AccountMasterName, MtAccountType, rd1.InvoiceNo, rd1.[Description] DescriptionDetail
						FROM #tbl_temp_reimbursement_detail rd1
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						JOIN #tbl_temp_accountmaster am ON rd1.AccountMasterId = am.Id
						WHERE r1.RequestNumber = r.RequestNumber 
						AND ' + @subquery2 + '
						FOR JSON PATH) x
					) detail
		JOIN #tbl_temp_voucher_detail vd ON ar.RequestNo = vd.VoucherRefId
		JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
		JOIN #tmp_argm apprv ON ar.[Id] = apprv.[ApprovalRequestId]
		OUTER APPLY ( SELECT(
						SELECT 
							REPLACE(FORMAT(SUM(roc1.BasicAmount), ''C''),''$'','''') BasicAmount, 
							REPLACE(FORMAT(SUM(roc1.Amount), ''C''),''$'','''') Amount, 
							REPLACE(FORMAT(SUM(ISNULL(roc1.GrossUp,0)), ''C''),''$'','''') GrossUp,
							scoc.SubCategoryCode OtherCostSubCategoryCode
						FROM #tbl_temp_reimbursement_detail_othercost roc1
						JOIN #tbl_temp_reimbursement_detail rd1 ON roc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_subcategory scoc ON roc1.OtherCost_SubCategoryId = scoc.Id
						WHERE rd1.ReimbursementId = r.Id
						GROUP BY scoc.SubCategoryCode
						FOR JSON PATH) ot
						) othercost
		OUTER APPLY (SELECT SUM(rcc1.Amount) TotalAmount
						FROM #tbl_temp_reimbursement_detail_costcenter rcc1
						JOIN #tbl_temp_reimbursement_detail rd1 ON rcc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						WHERE r1.RequestNumber = r.RequestNumber
					) costcenter
		OUTER APPLY (	SELECT (SELECT DISTINCT bu.Id BusinessUnitId, bu.Name BusinessUnitName, bu.Code BusinessUnitCode, 
						cc.Id CostCenterId, cc.[Name] CostCenterName, cc.Code CostCenterCode
						FROM #tbl_temp_reimbursement_detail_costcenter rcc1
						JOIN #tbl_temp_businessunit bu ON rcc1.BusinessUnitId = bu.Id
						JOIN #tbl_temp_costcenter cc ON rcc1.CostCenterId = cc.Id
						JOIN #tbl_temp_reimbursement_detail rd1 ON rcc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						WHERE r1.RequestNumber = r.RequestNumber 
						AND ' + @subqueryCATR + '
						FOR JSON PATH) x
					) costsplit
		LEFT JOIN #tbl_temp_travelexpense tre ON r.RequestNumber = tre.RefNoCA AND tre.Status IN (2,7,9)
		LEFT JOIN #tmp_argm apprvSTL  ON tre.RequestNumber = apprvSTL.[RequestNo] 
			
		OUTER APPLY (	SELECT (SELECT DISTINCT rd1.AttachmentId AttachmentIdRequest, vh.Attachment AttachmentIdVoucher
						FROM #tbl_temp_reimbursement_detail_costcenter rcc1
						JOIN #tbl_temp_reimbursement_detail rd1 ON rcc1.ReimbursementDetailId = rd1.Id
						JOIN #tbl_temp_reimbursement r1 ON rd1.ReimbursementId = r1.Id
						JOIN #tbl_temp_voucher_detail vd ON r1.RequestNumber = vd.VoucherRefId
						JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
						WHERE r1.RequestNumber = r.RequestNumber 
						FOR JSON PATH) AttachmentIds
					) Attachments
		WHERE costsplit.x IS NOT NULL AND r.RequestNumber LIKE ''CATR%'''

		IF OBJECT_ID('tempdb..#tbl_temp_transaction_cash_advance_travel') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_cash_advance_travel
		END
		CREATE TABLE #tbl_temp_transaction_cash_advance_travel(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(500),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(100),
			VendorId int,
			VendorName varchar(100),
			VendorCode varchar(100),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(100),
			ProjectNo varchar(100),
			Affiliate varchar(100),
			Beneficiaries varchar(250),
			BankAccountNumber varchar(100),
			BankName varchar(100),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(100),
			PPN varchar(100)
		)
		INSERT INTO #tbl_temp_transaction_cash_advance_travel
		EXEC sp_executesql @sqlQueryCATR, @paramSLA, @CutOffHour, @BusinessUnitId, @CostCenterId, @AccountMasterId

		--SELECT *FROM #tbl_temp_transaction_cash_advance_travel
	END

	IF (@RequestType = 'invoice travel' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		DECLARE @subqueryInvoiceTravel VARCHAR(MAX) = 'bu.Id = @BusinessUnitId AND cc.Id = @CostCenterId'
		IF (@BusinessUnitId IS NULL OR @BusinessUnitId = '')
		BEGIN
			SET @subqueryInvoiceTravel = REPLACE(@subqueryInvoiceTravel, 'bu.Id = @BusinessUnitId', '1=1')
		END
		IF (@CostCenterId IS NULL OR @CostCenterId = '')
		BEGIN
			SET @subqueryInvoiceTravel = REPLACE(@subqueryInvoiceTravel, 'cc.Id = @CostCenterId', '1=1')
		END
		IF (@AccountMasterId IS NULL OR @AccountMasterId = '')
		BEGIN
			SET @subquery2 = REPLACE(@subquery2, 'am.Id = @AccountMasterId', '1=1')
		END

		DECLARE @sqlQueryInvoiceTravel NVARCHAR(MAX) 
		SET @sqlQueryInvoiceTravel = '			
		SELECT DISTINCT 
		vh.TransferNumber, vh.VoucherNumber, vh.Id [VoucherId], ar.RequestNo RequestNumber, ar.RequestorUserName RequestorName, 
		'''' AS SettlementNumber, vh.CreatedBy MakerFinance,
		it.Status RequestStatus, it.Description, 
		vd.StatusTransfer,
		(	CASE WHEN vd.StatusTransfer = 1 THEN ''Success''
					WHEN vd.StatusTransfer = 0 THEN ''Failed''
					ELSE ''''
					END
		)	[StatusTransferDesc], 
		scv.Id [VendorCategoryId], scv.SubCategoryName VendorType, (SELECT DISTINCT TOP 1 v.[Id]) VendorId, (SELECT DISTINCT TOP 1 v.[Name]) VendorName,
		(CASE WHEN scv.SubCategoryName = ''Staff'' THEN (SELECT DISTINCT TOP 1 v.EmployeeCode)
		ELSE (SELECT DISTINCT TOP 1 v.Code) END) VendorCode, 
		''Invoice Travel'' AS RequestType,
		MONTH(apprv.ApprovalDate) [Month],
		it.RequestDate,
		CONVERT(VARCHAR(20),it.RequestDate,113) RequestDateString,
		apprv.ApprovalDate ReceivedByFinanceDate,
		CONVERT(VARCHAR(20),apprv.ApprovalDate,113) ReceivedByFinance,
		CONVERT(VARCHAR(20),vh.TransferTime,113) PaidByFinance,
		'''' SettlementDate,
		'''' ReceivedSettlementByFinance,
		(CASE WHEN it.Status = 4 THEN CONCAT(''[Repair]'', '' - '', it.ReasonReject) ELSE '''' END) StatusRepair,
		CONVERT(VARCHAR(20),DATEADD(DAY, 31, vh.TransferTime),113) DueDate,
		'''' [OverdueDays],
		'''' [StatusOverdue],
		'''' [BalanceAmount],
		'''' [DueToCompany],
		'''' [TransferDateDueToCompany],
		'''' [RealizationAmount],
		(CASE WHEN (DATEDIFF(year,apprv.ApprovalDate, vh.TransferTime))>=1
		THEN
		((DATEDIFF(DAY, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime))
		- 2 * ((DATEPART(week, vh.TransferTime) + 52 * 
		DATEDIFF(year,
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime)) 
		- DATEPART(week, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END))))
		- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
		ELSE
		((DATEDIFF(DAY, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime)) 
		- 2 * (DATEPART(week, vh.TransferTime) 
		- DATEPART(week, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END))))
		- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
		END) [SLA],
		'''' NewAdvanceNumber, 
		'''' NewAdvanceAmount,
		'''' NewAdvanceTransferDate, 
		'''' Detail,
		''-'' [Product], ''-'' [ProjectNo], ''-'' [Affiliate],
		'''' [Beneficiaries],
		'''' [BankAccountNumber],
		'''' [BankName],
		itd.L_Currency [LCurrencyCode],
		itd.RateAmount [Rate],
		REPLACE(FORMAT(detailamount.TotalAmount, ''C''),''$'','''') [Amount], 
		REPLACE(FORMAT(detailnettamount.NettAmount, ''C''),''$'','''')  [NettAmount],
		othercost.ot [OtherCosts], 
		costsplit.x  [CostSplit], 
		apprv.member ApprovalGroupMembers,
		'''' [DocumentNumber],
		Attachments.AttachmentIds,
		'''' PPH21,
		'''' PPN
		FROM #tbl_temp_approvalrequest ar 
		JOIN #tbl_temp_invoice_travel it ON ar.RequestNo = it.RequestNumber
		JOIN #tbl_temp_invoice_travel_detail itd ON it.Id = itd.InvoiceTravelId
		JOIN #tbl_temp_vendor v  ON it.VendorId = v.Id
		JOIN #tbl_temp_subcategory scv  ON v.SubCategoryId = scv.Id
		OUTER APPLY (SELECT SUM(itd1.FullAmount) TotalAmount
						FROM #tbl_temp_invoice_travel_detail itd1
						JOIN #tbl_temp_invoice_travel it1 ON itd1.InvoiceTravelId = it1.Id
						WHERE it1.RequestNumber = it.RequestNumber
					) detailamount
		OUTER APPLY (SELECT SUM(itd1.TotalAmount) NettAmount
						FROM #tbl_temp_invoice_travel_detail itd1
						JOIN #tbl_temp_invoice_travel it1 ON itd1.InvoiceTravelId = it1.Id
						WHERE it1.RequestNumber = it.RequestNumber
					) detailnettamount
		OUTER APPLY (
				        SELECT (
				            SELECT BasicAmount, Amount, GrossUp, OtherCostSubCategoryCode
				            FROM (
				                SELECT  
				                    REPLACE(FORMAT(SUM(VATAmount), ''C''),''$'','''') BasicAmount, 
				                    REPLACE(FORMAT(SUM(VATAmount), ''C''),''$'','''') Amount,
				                    0 AS GrossUp,
				                    ''PPN'' AS OtherCostSubCategoryCode
				                FROM #tbl_temp_invoice_travel_detail itd1
				    			JOIN #tbl_temp_invoice_travel it1 on itd1.InvoiceTravelId = it.Id
				                WHERE it1.Id = it.Id
				                UNION ALL
				                SELECT  
				                    REPLACE(FORMAT(SUM(PPH23Amount), ''C''),''$'','''') BasicAmount, 
				                    REPLACE(FORMAT(SUM(PPH23Amount), ''C''),''$'','''') Amount,
				                    0 AS GrossUp,
				                    ''PPH23'' AS OtherCostSubCategoryCode
				                FROM #tbl_temp_invoice_travel_detail itd1
				    			JOIN #tbl_temp_invoice_travel it1 on itd1.InvoiceTravelId = it.Id
				                WHERE it1.Id = it.Id				    
				            ) AS Combined
				            FOR JSON PATH
				        ) AS ot
				    ) othercost
		OUTER APPLY
			(	SELECT (
					SELECT DISTINCT
						bu.Id BusinessUnitId,
						bu.Name BusinessUnitName,
						bu.Code BusinessUnitCode, 
						cc.Id CostCenterId,
						cc.[Name] CostCenterName,
						cc.Code CostCenterCode
					FROM #tbl_temp_invoice_travel it1
					JOIN #tbl_temp_invoice_travel_detail itd1 ON it1.Id = itd1.InvoiceTravelId
					JOIN #tbl_temp_costcenter cc ON itd1.CostCenter = cc.Id
					JOIN #tbl_temp_businessunit bu ON cc.BusinessUnitId = bu.Id
					WHERE it1.RequestNumber = it.RequestNumber
					AND ' + @subqueryInvoiceTravel + '
					FOR JSON PATH
				) x
			) costsplit 
		JOIN #tbl_temp_voucher_detail vd ON it.RequestNumber = vd.VoucherRefId
		JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
		JOIN #tmp_argm apprv ON ar.[Id] = apprv.[ApprovalRequestId]
		OUTER APPLY (	SELECT (SELECT DISTINCT it1.AttachmentId AttachmentIdRequest, vh.Attachment AttachmentIdVoucher
						FROM #tbl_temp_invoice_travel it1
						JOIN #tbl_temp_voucher_detail vd ON it1.RequestNumber = vd.VoucherRefId
						JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
						WHERE it1.RequestNumber = it.RequestNumber 
						FOR JSON PATH) AttachmentIds
					) Attachments
		'

		IF OBJECT_ID('tempdb..#tbl_temp_transaction_invoice_travel') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_invoice_travel
		END
		CREATE TABLE #tbl_temp_transaction_invoice_travel(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(500),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(500),
			VendorId int,
			VendorName varchar(500),
			VendorCode varchar(500),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(500),
			ProjectNo varchar(500),
			Affiliate varchar(500),
			Beneficiaries varchar(500),
			BankAccountNumber varchar(500),
			BankName varchar(500),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(500),
			PPN varchar(500)
		)
		INSERT INTO #tbl_temp_transaction_invoice_travel
		EXEC sp_executesql @sqlQueryInvoiceTravel, @paramSLA, @CutOffHour, @BusinessUnitId, @CostCenterId, @AccountMasterId

		--SELECT @sqlQueryInvoiceTravel
		--SELECT *FROM #tbl_temp_transaction_invoice_travel
	END

	IF (@RequestType in ('ger', 'COMBEN', 'CONTEST', 'OTHERS') OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		DECLARE @subqueryGER VARCHAR(MAX) = 'bu.Id = @BusinessUnitId AND cc.Id = @CostCenterId'
		IF (@BusinessUnitId IS NULL OR @BusinessUnitId = '')
		BEGIN
			SET @subqueryGER = REPLACE(@subqueryGER, 'bu.Id = @BusinessUnitId', '1=1')
		END
		IF (@CostCenterId IS NULL OR @CostCenterId = '')
		BEGIN
			SET @subqueryGER = REPLACE(@subqueryGER, 'cc.Id = @CostCenterId', '1=1')
		END
		IF (@AccountMasterId IS NULL OR @AccountMasterId = '')
		BEGIN
			SET @subquery2 = REPLACE(@subquery2, 'am.Id = @AccountMasterId', '1=1')
		END

		DECLARE @sqlQueryGER NVARCHAR(MAX) 
		SET @sqlQueryGER = '			
		SELECT DISTINCT 
		vh.TransferNumber, vh.VoucherNumber, vh.Id [VoucherId], ar.RequestNo RequestNumber, ar.RequestorUserName RequestorName, 
		'''' AS SettlementNumber, vh.CreatedBy MakerFinance,
		r.Status RequestStatus, r.Description, 
		'''' [StatusTransfer],
		'''' [StatusTransferDesc], 
		'''' [VendorCategoryId], 
		'''' [VendorType], 
		'''' [VendorId],
		'''' [VendorName],
		'''' [VendorCode], 
		(SELECT TOP 1 SubCategoryName FROM SubCategory WHERE Id = r.ExpenseType_SubCategoryId) AS RequestType,
		MONTH(apprv.ApprovalDate) [Month],
		r.RequestDate,
		CONVERT(VARCHAR(20),r.RequestDate,113) RequestDateString,
		apprv.ApprovalDate ReceivedByFinanceDate,
		CONVERT(VARCHAR(20),apprv.ApprovalDate,113) ReceivedByFinance,
		CONVERT(VARCHAR(20),vh.TransferTime,113) PaidByFinance,
		'''' SettlementDate,
		'''' ReceivedSettlementByFinance,
		(CASE WHEN r.Status = 4 THEN CONCAT(''[Repair]'', '' - '', r.ReasonReject) ELSE '''' END) StatusRepair,
		CONVERT(VARCHAR(20),DATEADD(DAY, 31, vh.TransferTime),113) DueDate,
		'''' [OverdueDays],
		'''' [StatusOverdue],
		'''' [BalanceAmount],
		'''' [DueToCompany],
		'''' [TransferDateDueToCompany],
		'''' [RealizationAmount],
		(CASE WHEN (DATEDIFF(year,apprv.ApprovalDate, vh.TransferTime))>=1
		THEN
		((DATEDIFF(DAY, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime))
		- 2 * ((DATEPART(week, vh.TransferTime) + 52 * 
		DATEDIFF(year,
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime)) 
		- DATEPART(week, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END))))
		- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
		ELSE
		((DATEDIFF(DAY, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END), vh.TransferTime)) 
		- 2 * (DATEPART(week, vh.TransferTime) 
		- DATEPART(week, 
			(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
				ELSE apprv.ApprovalDate END))))
		- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
		END) [SLA],
		'''' NewAdvanceNumber, 
		'''' NewAdvanceAmount,
		'''' NewAdvanceTransferDate, 
		'''' Detail,
		''-'' [Product], ''-'' [ProjectNo], ''-'' [Affiliate],
		'''' [Beneficiaries],
		'''' [BankAccountNumber],
		'''' [BankName],
		rd.LCurrencyCode [LCurrencyCode],
		1 [Rate],
		REPLACE(FORMAT(detailamount.TotalAmount, ''C''),''$'','''') [Amount], 
		REPLACE(FORMAT(detailnettamount.NettAmount, ''C''),''$'','''')  [NettAmount],
		othercost.ot [OtherCosts], 
		costsplit.x  [CostSplit], 
		apprv.member ApprovalGroupMembers,
		'''' [DocumentNumber],
		Attachments.AttachmentIds,
		'''' PPH21,
		'''' PPN
		FROM #tbl_temp_approvalrequest ar 
		JOIN #tbl_temp_gerheader r ON ar.RequestNo = r.RequestNumber
		JOIN #tbl_temp_gerdetail rd ON r.Id = rd.GerHeaderId
		OUTER APPLY (SELECT SUM(rd1.Amount) TotalAmount
						FROM #tbl_temp_gerdetail rd1
						JOIN #tbl_temp_gerheader r1 ON rd1.GerHeaderId = r1.Id
						WHERE r1.RequestNumber = r.RequestNumber
					) detailamount
		OUTER APPLY (SELECT SUM(rd1.NettAmount) NettAmount
						FROM #tbl_temp_gerdetail rd1
						JOIN #tbl_temp_gerheader r1 ON rd1.GerHeaderId = r1.Id
						WHERE r1.RequestNumber = r.RequestNumber
					) detailnettamount
		OUTER APPLY (
				        SELECT (
				            SELECT BasicAmount, Amount, GrossUp, OtherCostSubCategoryCode
				            FROM (
				                SELECT  
				                    REPLACE(FORMAT(SUM(PPN), ''C''),''$'','''') BasicAmount, 
				                    REPLACE(FORMAT(SUM(PPN), ''C''),''$'','''') Amount,
				                    0 AS GrossUp,
				                    ''PPN'' AS OtherCostSubCategoryCode
				                FROM GerDetail gd
				    			JOIN GerHeader gh on gd.GerHeaderId = gh.Id
				                WHERE gh.Id = r.Id 
				    			
				    
				                UNION ALL
				    
				                SELECT  
				                    REPLACE(FORMAT(SUM(PPH21), ''C''),''$'','''') BasicAmount, 
				                    REPLACE(FORMAT(SUM(PPH21), ''C''),''$'','''') Amount,
				                    0 AS GrossUp,
				                    ''PPH21'' AS OtherCostSubCategoryCode
				                FROM GerDetail gd
				    			JOIN GerHeader gh on gd.GerHeaderId = gh.Id
				                WHERE gh.Id = r.Id 
				    
				            ) AS Combined
				            FOR JSON PATH
				        ) AS ot
				    ) othercost
		OUTER APPLY
			(	SELECT (
					SELECT DISTINCT
						bu.Id BusinessUnitId,
						bu.Name BusinessUnitName,
						bu.Code BusinessUnitCode, 
						cc.Id CostCenterId,
						cc.[Name] CostCenterName,
						cc.Code CostCenterCode
					FROM #tbl_temp_gerheader gh1 
					JOIN #tbl_temp_costcenter cc ON gh1.CostCenterId = cc.Id
					JOIN #tbl_temp_businessunit bu ON cc.BusinessUnitId = bu.Id
					WHERE gh1.RequestNumber = r.RequestNumber
					AND ' + @subqueryGER + '
					FOR JSON PATH
				) x
			) costsplit 
		JOIN #tbl_temp_voucher_detail vd ON CONCAT(r.RequestNumber, '' - '' , rd.Id) = vd.VoucherRefId
		JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
		JOIN #tmp_argm apprv ON ar.[Id] = apprv.[ApprovalRequestId]
		OUTER APPLY (	SELECT (SELECT DISTINCT r1.AttachmentId AttachmentIdRequest, vh.Attachment AttachmentIdVoucher
						FROM #tbl_temp_gerheader r1
						JOIN #tbl_temp_gerdetail rd on r1.Id = rd.GerHeaderId
						JOIN #tbl_temp_voucher_detail vd ON CONCAT(r1.RequestNumber, '' - '' , rd.Id) = vd.VoucherRefId
						JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
						WHERE r1.RequestNumber = r.RequestNumber 
						FOR JSON PATH) AttachmentIds
					) Attachments
		'

		IF OBJECT_ID('tempdb..#tbl_temp_transaction_ger') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_ger
		END
		CREATE TABLE #tbl_temp_transaction_ger(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(500),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(500),
			VendorId int,
			VendorName varchar(500),
			VendorCode varchar(500),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(500),
			ProjectNo varchar(500),
			Affiliate varchar(500),
			Beneficiaries varchar(500),
			BankAccountNumber varchar(500),
			BankName varchar(500),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(500),
			PPN varchar(500)
		)
		INSERT INTO #tbl_temp_transaction_ger
		EXEC sp_executesql @sqlQueryGER, @paramSLA, @CutOffHour, @BusinessUnitId, @CostCenterId, @AccountMasterId

		--SELECT @sqlQueryGER
		--SELECT *FROM #tbl_temp_transaction_ger
	END

	IF (@RequestType = 'travel settlement' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN

		DECLARE @subqueryTRSTL VARCHAR(MAX) = 'bu.Id = @BusinessUnitId AND cc.Id = @CostCenterId'
		IF (@BusinessUnitId IS NULL OR @BusinessUnitId = '')
		BEGIN
			SET @subqueryTRSTL = REPLACE(@subqueryTRSTL, 'bu.Id = @BusinessUnitId', '1=1')
		END
		IF (@CostCenterId IS NULL OR @CostCenterId = '')
		BEGIN
			SET @subqueryTRSTL = REPLACE(@subqueryTRSTL, 'cc.Id = @CostCenterId', '1=1')
		END

		DECLARE @sqlQueryTRSTL NVARCHAR(MAX) 
		SET @sqlQueryTRSTL = '
		SELECT DISTINCT 
			vh.TransferNumber, vh.VoucherNumber, vh.Id [VoucherId], (SELECT TOP 1 RequestNo FROM ApprovalRequest WHERE Id = tr.ApprovalRequestId) RequestNumber, ar.RequestorUserName RequestorName, ar.RequestNo SettlementNumber, vh.CreatedBy MakerFinance,
			tre.Status RequestStatus, tr.PurposeNotes Description, vd.StatusTransfer,
			(	CASE WHEN vd.StatusTransfer = 1 THEN ''Success''
						WHEN vd.StatusTransfer = 0 THEN ''Failed''
						ELSE ''''
						END
			)	[StatusTransferDesc], 
			'''' [VendorCategoryId], '''' VendorType, (SELECT DISTINCT TOP 1 v.[Id]) VendorId, (SELECT DISTINCT TOP 1 v.[Name]) VendorName,
			(SELECT DISTINCT TOP 1 v.EmployeeCode) VendorCode, ''Travel Settlement'' RequestType,
			MONTH(apprv.ApprovalDate) [Month],
			tre.RequestDate,
			CONVERT(VARCHAR(20),tre.RequestDate,113) RequestDateString,
			apprv.ApprovalDate ReceivedByFinanceDate,
			CONVERT(VARCHAR(20),apprv.ApprovalDate,113) ReceivedByFinance,
			CONVERT(VARCHAR(20),vh.TransferTime,113) PaidByFinance,
			CONVERT(VARCHAR(20),tre.RequestDate,113) SettlementDate,
			'''' ReceivedSettlementByFinance,
			'''' StatusRepair,
			CONVERT(VARCHAR(20),DATEADD(DAY, 31, vh.TransferTime),113) DueDate,
			(CASE WHEN tre.RequestDate IS NOT NULL THEN ''''
					WHEN tre.RequestDate IS NULL AND DATEDIFF(DAY, DATEADD(DAY, 31, vh.TransferTime), getdate() ) > 0 THEN CAST(DATEDIFF(DAY, DATEADD(DAY, 31, vh.TransferTime), getdate())AS VARCHAR) 
					ELSE CAST(0 AS VARCHAR) END) OverdueDays,
			(CASE WHEN tre.RequestDate IS NOT NULL AND tre.Status = 2 THEN ''Settled''
					WHEN tre.RequestDate IS NULL AND (DATEDIFF(DAY, DATEADD(DAY, 31, vh.TransferTime), getdate())) > 0 THEN ''Overdue'' 
					ELSE ''Current'' END) StatusOverdue,
			(SELECT REPLACE(FORMAT(ISNULL(ISNULL(vd.TotalOriginalAmmount, 0)- ISNULL(SUM(sd.Amount), 0), 0), ''C''), ''$'', '''') FROM TravelRequestExpenseDetail sd WHERE sd.TravelRequestExpenseId = tre.Id) [BalanceAmount],
			(REPLACE(FORMAT(0, ''C''), ''$'', '''')) [DueToCompany],
			null [TransferDateDueToCompany],
			'''' [RealizationAmount],
			(CASE WHEN (DATEDIFF(year,apprv.ApprovalDate, vh.TransferTime))>=1
			THEN
			((DATEDIFF(DAY, 
				(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
					ELSE apprv.ApprovalDate END), vh.TransferTime))
			- 2 * ((DATEPART(week, vh.TransferTime) + 52 * 
			DATEDIFF(year,
				(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
					ELSE apprv.ApprovalDate END), vh.TransferTime)) 
			- DATEPART(week, 
				(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
					ELSE apprv.ApprovalDate END))))
			- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
			ELSE
			((DATEDIFF(DAY, 
				(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
					ELSE apprv.ApprovalDate END), vh.TransferTime)) 
			- 2 * (DATEPART(week, vh.TransferTime) 
			- DATEPART(week, 
				(CASE WHEN CAST(apprv.ApprovalDate as time) > CAST(@CutOffHour as time) AND DATEDIFF(DAY, apprv.ApprovalDate, vh.TransferTime) > 0 THEN DATEADD(DAY, 1, apprv.ApprovalDate) 
					ELSE apprv.ApprovalDate END))))
			- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
			END) [SLA],
			'''' NewAdvanceNumber, (REPLACE(FORMAT(0, ''C''), ''$'', '''')) NewAdvanceAmount, null NewAdvanceTransferDate, 
			detail.x Detail,
			''-'' [Product], ''-'' [ProjectNo], ''-'' [Affiliate],
			tre.BankAccountOwnerName [Beneficiaries],
			tre.BankAccountNumber [BankAccountNumber],
			tre.BankName [BankName],
			tre.L_Currency [LCurrencyCode],
			(REPLACE(FORMAT(1, ''C''),''$'','''')) [Rate],
			(REPLACE(FORMAT(tre.GrandTotal, ''C''),''$'','''')) [Amount], 
			(REPLACE(FORMAT(tre.GrandTotal, ''C''),''$'','''')) [NettAmount],
			'''' [OtherCosts], costsplit.x [CostSplit], apprv.member ApprovalGroupMembers,
			'''' DocumentNumber,
			Attachments.AttachmentIds,
			'''' PPH21,
			'''' PPN
			FROM #tbl_temp_approvalrequest ar 
			JOIN #tbl_temp_travelexpense tre ON ar.RequestNo = tre.RequestNumber
			JOIN #tbl_temp_travelexpense_detail trd ON tre.Id = trd.TravelRequestExpenseId
			JOIN #tbl_temp_travelrequest tr ON tre.TravelRequestId = tr.Id
			JOIN #tbl_temp_vendor v ON tr.VendorId = v.Id
			OUTER APPLY (SELECT SUM(trd1.Amount) TotalAmount
							FROM #tbl_temp_travelexpense_detail trd1
							JOIN #tbl_temp_travelexpense tre1 ON trd1.TravelRequestExpenseId = tre1.Id
							WHERE tre1.RequestNumber = tre.RequestNumber
						) detailamount
			OUTER APPLY (	SELECT (SELECT DISTINCT am.Id AccountMasterId, am.AccountCode AccountMasterCode, am.ShortDescription AccountMasterName, MtAccountType, 
							'''' InvoiceNo, trd1.[Description] DescriptionDetail, scv.Id [VendorCategoryId], scv.SubCategoryName VendorType
							FROM #tbl_temp_travelexpense tre1
							JOIN #tbl_temp_travelexpense_detail trd1 ON tre1.Id = trd1.TravelRequestExpenseId
							JOIN #tbl_temp_subcategory scv ON trd1.TypeExpense_SubCategoryId = scv.Id
							OUTER APPLY (SELECT *FROM #tbl_temp_accountmaster am 
							WHERE am.AccountCode = 
							(CASE WHEN scv.SubCategoryCode = ''TravelExpenses-Plane'' AND tr.IsOverseas = 1 THEN (SELECT TOP 1 sc.SubCategoryName FROM #tbl_temp_subcategory sc WHERE sc.SubCategoryCode = ''AccountPlaneOverseas'')
									WHEN scv.SubCategoryCode = ''TravelExpenses-Plane'' AND tr.IsOverseas = 0 THEN (SELECT TOP 1 sc.SubCategoryName FROM #tbl_temp_subcategory sc WHERE sc.SubCategoryCode = ''AccountPlaneDomestic'')
									WHEN scv.SubCategoryCode = ''TravelExpenses-Hotel'' AND tr.IsOverseas = 1 THEN (SELECT TOP 1 sc.SubCategoryName FROM #tbl_temp_subcategory sc WHERE sc.SubCategoryCode = ''AccountHotelOverseas'')
									WHEN scv.SubCategoryCode = ''TravelExpenses-Hotel'' AND tr.IsOverseas = 0 THEN (SELECT TOP 1 sc.SubCategoryName FROM #tbl_temp_subcategory sc WHERE sc.SubCategoryCode = ''AccountHotelDomestic'')
									WHEN scv.SubCategoryCode != ''TravelExpenses-Hotel'' AND scv.SubCategoryCode != ''TravelExpenses-Plane'' AND tr.IsOverseas = 1 THEN (SELECT TOP 1 sc.SubCategoryName FROM #tbl_temp_subcategory sc WHERE sc.SubCategoryCode = ''AccountOtherOverseas'')
									WHEN scv.SubCategoryCode != ''TravelExpenses-Hotel'' AND scv.SubCategoryCode != ''TravelExpenses-Plane'' AND tr.IsOverseas = 0 THEN (SELECT TOP 1 sc.SubCategoryName FROM #tbl_temp_subcategory sc WHERE sc.SubCategoryCode = ''AccountOtherDomestic'')
								END)) am
							WHERE tre1.RequestNumber = tre.RequestNumber 
							FOR JSON PATH) x
						) detail
			JOIN #tbl_temp_voucher_detail vd ON ar.RequestNo = vd.VoucherRefId
			JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
			LEFT JOIN #tmp_argm apprv  ON ar.[Id] = apprv.[ApprovalRequestId]
			OUTER APPLY (	SELECT (SELECT DISTINCT trd1.AttachmentId AttachmentIdRequest, vh.Attachment AttachmentIdVoucher
							FROM #tbl_temp_travelexpense_detail trd1
							JOIN #tbl_temp_travelexpense tre1 ON trd1.TravelRequestExpenseId = tre1.Id
							JOIN #tbl_temp_travelrequest tr1 ON tr1.Id = tre1.TravelRequestId
							JOIN #tbl_temp_voucher_detail vd ON tre1.RequestNumber = vd.VoucherRefId
							JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
							WHERE tre1.RequestNumber = tre.RequestNumber 
							FOR JSON PATH) AttachmentIds
						) Attachments
			OUTER APPLY
			(	SELECT (
					SELECT DISTINCT
						bu.Id BusinessUnitId,
						bu.Name BusinessUnitName,
						bu.Code BusinessUnitCode, 
						cc.Id CostCenterId,
						cc.[Name] CostCenterName,
						cc.Code CostCenterCode
					FROM #tbl_temp_travelexpense tre1 
					JOIN #tbl_temp_costcenter cc ON tre1.CostCenterId = cc.Id
					JOIN #tbl_temp_businessunit bu ON cc.BusinessUnitId = bu.Id
					WHERE tre1.RequestNumber = tre.RequestNumber
					AND ' + @subqueryTRSTL + '
					FOR JSON PATH
				) x
			) costsplit 
		WHERE costsplit.x IS NOT NULL'
		DECLARE @paramTRSTL NVARCHAR(MAX)
		SET @paramTRSTL = N'@CutOffHour NVARCHAR(100), @BusinessUnitId NVARCHAR(100), @CostCenterId NVARCHAR(100)'

		IF OBJECT_ID('tempdb..#tbl_temp_transaction_travelsettlement') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_travelsettlement
		END
		CREATE TABLE #tbl_temp_transaction_travelsettlement(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(500),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(100),
			VendorId int,
			VendorName varchar(100),
			VendorCode varchar(100),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(100),
			ProjectNo varchar(100),
			Affiliate varchar(100),
			Beneficiaries varchar(250),
			BankAccountNumber varchar(100),
			BankName varchar(100),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(100),
			PPN varchar(100)
		)
		INSERT INTO #tbl_temp_transaction_travelsettlement
		EXEC sp_executesql @sqlQueryTRSTL, @paramTRSTL, @CutOffHour, @BusinessUnitId, @CostCenterId
		--SELECT *FROM #tbl_temp_transaction_travelsettlement
	END

	IF (@RequestType = 'purchase order' OR @RequestType = 'Shopping Cart' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		IF (@AccountMasterId IS NULL OR @AccountMasterId = '')
		BEGIN
			SET @subquery2 = REPLACE(@subquery2, 'am.Id = @AccountMasterId', '1=1')
		END

		DECLARE @sqlQueryShoppingCart NVARCHAR(MAX)
		SET @sqlQueryShoppingCart = '
			SELECT DISTINCT
				vh.TransferNumber,
				vh.VoucherNumber,
				vh.Id [VoucherId],
				po.PONumber RequestNumber,
				po.RequestorName RequestorName,
				'''' SettlementNumber,
				vh.CreatedBy MakerFinance,
				pr.Status RequestStatus,
				PRD.CombinedItem [Description],
				vd.StatusTransfer,
				(CASE
						WHEN vd.StatusTransfer = 1 THEN
							''Success''
						WHEN vd.StatusTransfer = 0 THEN
							''Failed''
						ELSE
							''''
					END
				) [StatusTransferDesc],
				scv.Id [VendorCategoryId],
				scv.SubCategoryName VendorType,
				(
					SELECT DISTINCT TOP 1 v.[Id]
				) VendorId,
				(
					SELECT DISTINCT TOP 1 v.[Name]
				) VendorName,
				(CASE
						WHEN scv.SubCategoryName = ''Staff'' THEN
						(
							SELECT DISTINCT TOP 1 v.EmployeeCode
						)
						ELSE
					(
						SELECT DISTINCT TOP 1 v.Code
					)
					END
				) VendorCode,
				''' + ISNULL(NULLIF(@RequestType, ''), 'Shopping Cart') + ''' AS [RequestType],
				MONTH(pr.LastUpdatedTime) [Month],
				PR.RequestDates [RequestDate],
				CONVERT(VARCHAR(20), PR.RequestDates, 113) RequestDateString,
				pr.LastUpdatedTime ReceivedByFinanceDate,
				(
					CASE WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN
						IPO.InvoiceDate
					ELSE
						DNP.LastUpdatedTime
					END
				) ReceivedByFinance,
				CONVERT(VARCHAR(20), vh.TransferTime, 113) PaidByFinance,
				'''' SettlementDate,
				'''' ReceivedSettlementByFinance,
				'''' StatusRepair,
				'''' DueDate,
				'''' OverdueDays,
				'''' StatusOverdue,
				'''' [BalanceAmount],
				'''' [DueToCompany],
				'''' [TransferDateDueToCompany],
				'''' [RealizationAmount],
				(
					CASE
						WHEN DATEDIFF(
								 YEAR,
								 CASE 
									 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
									 ELSE DNP.LastUpdatedTime
								 END,
								 vh.TransferTime
							 ) >= 1
						THEN
							(
								DATEDIFF(
									DAY,
									CASE
										WHEN CAST(
												 CASE 
													 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
													 ELSE DNP.LastUpdatedTime
												 END AS TIME
											 ) > CAST(@CutOffHour AS TIME)
											 AND DATEDIFF(
												 DAY,
												 CASE 
													 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
													 ELSE DNP.LastUpdatedTime
												 END,
												 vh.TransferTime
											 ) > 0
										THEN DATEADD(
												 DAY, 1,
												 CASE 
													 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
													 ELSE DNP.LastUpdatedTime
												 END
											 )
										ELSE
											CASE 
												WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
												ELSE DNP.LastUpdatedTime
											END
									END,
									vh.TransferTime
								)
								- 2 * (
									(DATEPART(WEEK, vh.TransferTime) + 52 * DATEDIFF(
										YEAR,
										CASE
											WHEN CAST(
													 CASE 
														 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
														 ELSE DNP.LastUpdatedTime
													 END AS TIME
												 ) > CAST(@CutOffHour AS TIME)
												 AND DATEDIFF(
													 DAY,
													 CASE 
														 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
														 ELSE DNP.LastUpdatedTime
													 END,
													 vh.TransferTime
												 ) > 0
											THEN DATEADD(
													 DAY, 1,
													 CASE 
														 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
														 ELSE DNP.LastUpdatedTime
													 END
												 )
											ELSE
												CASE 
													WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
													ELSE DNP.LastUpdatedTime
												END
										END,
										vh.TransferTime
									))
									- DATEPART(
										WEEK,
										CASE
											WHEN CAST(
													 CASE 
														 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
														 ELSE DNP.LastUpdatedTime
													 END AS TIME
												 ) > CAST(@CutOffHour AS TIME)
												 AND DATEDIFF(
													 DAY,
													 CASE 
														 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
														 ELSE DNP.LastUpdatedTime
													 END,
													 vh.TransferTime
												 ) > 0
											THEN DATEADD(
													 DAY, 1,
													 CASE 
														 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
														 ELSE DNP.LastUpdatedTime
													 END
												 )
											ELSE
												CASE 
													WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
													ELSE DNP.LastUpdatedTime
												END
										END
									)
								)
								- (
									SELECT COUNT(*)
									FROM #tbl_temp_holiday
									WHERE DateHoliday BETWEEN pr.LastUpdatedTime AND vh.TransferTime
									  AND Status = 1
									  AND DATEPART(WEEKDAY, DateHoliday) NOT IN (1, 7)
								)
							)
						ELSE
							(
								DATEDIFF(
									DAY,
									CASE
										WHEN CAST(
												 CASE 
													 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
													 ELSE DNP.LastUpdatedTime
												 END AS TIME
											 ) > CAST(@CutOffHour AS TIME)
											 AND DATEDIFF(
												 DAY,
												 CASE 
													 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
													 ELSE DNP.LastUpdatedTime
												 END,
												 vh.TransferTime
											 ) > 0
										THEN DATEADD(
												 DAY, 1,
												 CASE 
													 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
													 ELSE DNP.LastUpdatedTime
												 END
											 )
										ELSE
											CASE 
												WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
												ELSE DNP.LastUpdatedTime
											END
									END,
									vh.TransferTime
								)
								- 2 * (
									DATEPART(WEEK, vh.TransferTime)
									- DATEPART(
										WEEK,
										CASE
											WHEN CAST(
													 CASE 
														 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
														 ELSE DNP.LastUpdatedTime
													 END AS TIME
												 ) > CAST(@CutOffHour AS TIME)
												 AND DATEDIFF(
													 DAY,
													 CASE 
														 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
														 ELSE DNP.LastUpdatedTime
													 END,
													 vh.TransferTime
												 ) > 0
											THEN DATEADD(
													 DAY, 1,
													 CASE 
														 WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
														 ELSE DNP.LastUpdatedTime
													 END
												 )
											ELSE
												CASE 
													WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
													ELSE DNP.LastUpdatedTime
												END
										END
									)
								)
								- (
									SELECT COUNT(*)
									FROM #tbl_temp_holiday
									WHERE DateHoliday BETWEEN pr.LastUpdatedTime AND vh.TransferTime
									  AND Status = 1
									  AND DATEPART(WEEKDAY, DateHoliday) NOT IN (1, 7)
								)
							)
					END
				) [SLA],
				'''' NewAdvanceNumber,
				'''' NewAdvanceAmount,
				'''' NewAdvanceTransferDate,
				detail.x Detail,
				''-'' [Product],
				''-'' [ProjectNo],
				''-'' [Affiliate],
				(
					SELECT DISTINCT TOP 1 ipo.BankAccountOwnerName
				) [Beneficiaries],
				(
					SELECT DISTINCT TOP 1 ipo.BankAccountNumber
				) [BankAccountNumber],
				(
					SELECT DISTINCT TOP 1 ipo.BankName
				) [BankName],
				(
					SELECT DISTINCT TOP 1 ipo.LCurrency
				) [LCurrencyCode],
				(
					SELECT DISTINCT TOP 1 REPLACE(FORMAT(prd.RateAmount, ''C''), ''$'', '''')
				) [Rate],
				REPLACE(FORMAT(detailamount.TotalAmount, ''C''), ''$'', '''') [Amount],
				REPLACE(FORMAT(costcenter.TotalAmount, ''C''), ''$'', '''') [NettAmount],
				othercost.ot [OtherCosts],
				costsplit.x [CostSplit],
				apprv.member ApprovalGroupMembers,
				'''' DocumentNumber,
				Attachments.AttachmentIds,
				'''' PPH21,
				'''' PPN
			FROM #tbl_temp_purchaseorder po WITH(NOLOCK)
				JOIN #tbl_temp_purchaserequest_purchaseorder prtopo WITH(NOLOCK)
					 ON PRTOPO.PurchaseOrderId = PO.Id
				JOIN #tbl_temp_purchaserequest pr WITH(NOLOCK)
					 ON PR.Id = PRTOPO.PurchaseRequestlId
				JOIN #tbl_temp_vendor v WITH(NOLOCK)
					 ON V.Id = PO.VendorId
				JOIN #tbl_temp_invoice_po ipo WITH (NOLOCK)
					 ON IPO.PurchaeseOrderId = PRTOPO.PurchaseOrderId
				JOIN #tbl_temp_subcategory scv WITH(NOLOCK)
					 ON SCV.Id = V.SubCategoryId
				JOIN #tbl_temp_voucher_detail vd WITH (NOLOCK)
					 ON cast(ipo.Id as varchar(100)) = substring(vd.VoucherRefId,0,charindex('' - '',vd.VoucherRefId))
				JOIN #tbl_temp_voucher_header vh WITH (NOLOCK)
					 ON VH.Id = VD.VoucherId
				JOIN #tbl_temp_purchaseorder_top POTOP
					 ON POTOP.PurchaseOrderId = PO.Id
					 AND POTOP.id = IPO.PurchaseOrderTOPId
				JOIN #tbl_temp_deliverynotes_payment DNP
					  ON DNP.PurchaseOrderTOPId = IPO.PurchaseOrderTOPId
					 AND (SELECT SCX.SubCategoryCode FROM #tbl_temp_subcategory SCX WHERE SCX.Id = DNP.CategoryProcess_SubCategoryId) = ''SC-2024-02-01261''
					 AND DNP.Status = 2
				OUTER APPLY (
						SELECT PRD1.RateAmount, STRING_AGG(I1.Name, '', '') AS CombinedItem 
						FROM #tbl_temp_purchaserequest_detail PRD1 
						JOIN #tbl_temp_item I1
							ON I1.Id = PRD1.ItemId
						WHERE PRD1.PurchaseRequestId = PR.Id 
							AND PRD1.VendorId = V.Id
						GROUP BY PRD1.RateAmount
					) PRD
				OUTER APPLY 
				(
					SELECT TOP 1 * FROM #tmp_argm apprv
					WHERE APPRV.ApprovalRequestId in (
						select Id from ApprovalRequest where RequestNo = pr.RequestCode
					) 
					ORDER BY LEN(apprv.member) DESC
				) APPRV
				OUTER APPLY
			(
				SELECT SUM(IPDD.DPPAmount) [TotalAmount] FROM InvoicePODetail IPDD
				WHERE IPDD.InvoicePOId = IPO.ID
			) detailamount
				OUTER APPLY
			(
				SELECT
					(
						SELECT * FROM (
							SELECT DISTINCT
								prd1.Id,
								prd1.AccountMasterId,
								am.AccountCode AccountMasterCode,
								am.ShortDescription AccountMasterName,
								MtAccountType,
								ipo1.InvoiceNumber [InvoiceNo],
								i1.Name DescriptionDetail
							FROM #tbl_temp_purchaserequest_detail prd1 WITH(NOLOCK)
								JOIN #tbl_temp_accountmaster am WITH(NOLOCK)
									ON prd1.AccountMasterId = am.Id
								JOIN #tbl_temp_purchaserequest_purchaseorder prpo1 WITH(NOLOCK)
									on prd1.PurchaseRequestId = prpo1.PurchaseRequestlId
								JOIN #tbl_temp_invoice_po ipo1 WITH(NOLOCK)
									on prpo1.PurchaseOrderId = ipo1.PurchaeseOrderId
									AND POTOP.id = IPO1.PurchaseOrderTOPId
								JOIN #tbl_temp_item i1 WITH(NOLOCK)
									ON i1.Id = prd1.ItemId
							WHERE ipo1.CategoryProcess_SubCategoryId = (
																			SELECT Id
																			FROM #tbl_temp_subcategory SCX
																			WHERE SCX.SubCategoryCode = ''SC-2024-02-01261''
																		)
									and PRD1.PurchaseRequestId = PR.Id 
									AND PRD1.VendorId = V.Id
									and ' + @subquery2 + '
						) AS OrderedData
							ORDER BY OrderedData.Id ASC
						FOR JSON PATH
					) x
			) detail
				OUTER APPLY
			(
				SELECT
					(
						SELECT  REPLACE(FORMAT(SUM(ipoc.TotalBaseAmount), ''C''), ''$'', '''') BasicAmount,
								REPLACE(FORMAT(SUM(ipoc.Amount), ''C''), ''$'', '''') Amount,
								REPLACE(FORMAT(SUM(ipoc.AmountGrossUp), ''C''), ''$'', '''') GrossUp,
								scoc.SubCategoryCode OtherCostSubCategoryCode
						FROM #tbl_temp_invoicepo_othercost ipoc WITH (NOLOCK)
							JOIN #tbl_temp_subcategory scoc WITH (NOLOCK)
								ON ipoc.OtherCost_SubCategoryId = scoc.Id
						WHERE ipoc.InvoicePOId = ipo.Id
								and ipoc.PONonShoppingDetailId is null
						GROUP BY scoc.SubCategoryCode
						FOR JSON PATH
					) ot
			) othercost
				OUTER APPLY
			(
				SELECT SUM(IPO2.TotalAmount) [TotalAmount] 
				FROM #tbl_temp_invoice_po IPO2 
				WHERE IPO2.PurchaeseOrderId = prtopo.PurchaseOrderId 
				AND POTOP.id = IPO2.PurchaseOrderTOPId
			) costcenter
				OUTER APPLY
			(
				SELECT
					(
						SELECT DISTINCT
							bu.Id BusinessUnitId,
							bu.Name BusinessUnitName,
							bu.Code BusinessUnitCode,
							cc.Id CostCenterId,
							cc.[Name] CostCenterName,
							cc.Code CostCenterCode
						FROM #tbl_temp_purchaseorder_costcenter pocc WITH (NOLOCK)
							JOIN #tbl_temp_purchaseorder_detail podc WITH (NOLOCK)
								on pocc.PurchaseOrderDetailId = podc.Id
							JOIN #tbl_temp_costcenter cc WITH (NOLOCK)
								ON pocc.CostCenterId = cc.Id
							JOIN #tbl_temp_businessunit bu WITH (NOLOCK)
								ON cc.BusinessUnitId = bu.Id
						WHERE podc.PurchaseOrderId = prtopo.PurchaseOrderId
								AND ' + @subquery2 + '
						FOR JSON PATH
					) x
			) costsplit
				OUTER APPLY
			(
				SELECT JSON_QUERY(
						(
							SELECT ATT.*
							FROM
							(
								SELECT ATH.Id AttachmentIdRequest,
										NULL AttachmentIdVoucher
								FROM #tbl_temp_purchaserequest_detail PRD WITH (NOLOCK)
									JOIN #tbl_temp_attachment ATH WITH (NOLOCK)
										ON PRD.AttachmentId = ATH.Id
								WHERE PRD.PurchaseRequestId IN ( prtopo.PurchaseRequestlId )
										AND ATH.Category = ''PR''
								UNION ALL
								SELECT ATH.Id AttachmentIdRequest,
										NULL AttachmentIdVoucher
								FROM #tbl_temp_attachment ATH WITH (NOLOCK)
								WHERE ATH.RefId = prtopo.PurchaseRequestlId
										AND ATH.Category = ''PurchaseRequest''
								UNION ALL
								SELECT ATH.Id AttachmentIdRequest,
										NULL AttachmentIdVoucher
								FROM #tbl_temp_purchaseorder PO WITH (NOLOCK)
									JOIN #tbl_temp_attachment ATH WITH (NOLOCK)
										ON PO.AttachmentId = ATH.Id
								WHERE PO.Id IN ( prtopo.PurchaseOrderId )
										AND ATH.Category = ''PO''
								UNION ALL
								SELECT ATH.Id AttachmentIdRequest,
										NULL AttachmentIdVoucher
								FROM #tbl_temp_deliverynotes_detail DND WITH (NOLOCK)
									JOIN #tbl_temp_attachment ATH WITH (NOLOCK)
										ON DND.Id = ATH.RefId
								WHERE DND.PurchaseOrderDetailId IN (
																		SELECT POD1.Id
																		FROM #tbl_temp_purchaseorder_detail POD1 WITH (NOLOCK)
																		WHERE POD1.PurchaseOrderId = prtopo.PurchaseOrderId
																	)
										AND ATH.Category = ''DN''
								UNION ALL
								SELECT ATH.Id AttachmentIdRequest,
										NULL AttachmentIdVoucher
								FROM #tbl_temp_purchaseorder PO WITH (NOLOCK)
									JOIN #tbl_temp_attachment ATH
										ON PO.Id = ATH.RefId
								WHERE PO.Id = prtopo.PurchaseOrderId
										AND ATH.Category = ''INV''
										AND ATH.Description = ''ShoppingCart''

								UNION ALL
								SELECT ATH.Id AttachmentIdRequest,
										NULL AttachmentIdVoucher
								FROM #tbl_temp_attachment ATH WITH(NOLOCK)
								where ATH.Id = vh.Attachment

							) ATT
							FOR JSON PATH
						)) AttachmentIds
			) Attachments
			WHERE ipo.CategoryProcess_SubCategoryId =
			(
				SELECT Id
				FROM #tbl_temp_subcategory SCX
				WHERE SCX.SubCategoryCode = ''SC-2024-02-01261''
			)'
		IF OBJECT_ID('tempdb..#tbl_temp_transaction_shopcart') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_shopcart
		END
		CREATE TABLE #tbl_temp_transaction_shopcart(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(max),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(100),
			VendorId int,
			VendorName varchar(100),
			VendorCode varchar(100),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(100),
			ProjectNo varchar(100),
			Affiliate varchar(100),
			Beneficiaries varchar(250),
			BankAccountNumber varchar(100),
			BankName varchar(100),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(100),
			PPN varchar(100)
		)
		INSERT INTO #tbl_temp_transaction_shopcart
		EXEC sp_executesql @sqlQueryShoppingCart, @paramSLA, @CutOffHour, @BusinessUnitId, @CostCenterId, @AccountMasterId
		--SELECT *FROM #tbl_temp_transaction_shopcart
	END

	IF (@RequestType = 'purchase order' OR @RequestType = 'Non Shopping Cart'   OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		IF (@AccountMasterId IS NULL OR @AccountMasterId = '')
		BEGIN
			SET @subquery2 = REPLACE(@subquery2, 'am.Id = @AccountMasterId', '1=1')
		END
		DECLARE @sqlQueryNonShoppingCart NVARCHAR(MAX) 
		SET @sqlQueryNonShoppingCart = '
			SELECT DISTINCT
			vh.TransferNumber,
			vh.VoucherNumber,
			vh.Id [VoucherId],
			po.PONumber RequestNumber,
			po.RequestorName RequestorName,
			'''' SettlementNumber,
			vh.CreatedBy MakerFinance,
			PRF.Status RequestStatus,
			(select replace(
				replace(
					replace (
						replace (
							replace(
		(select 
			(select STRING_AGG(ItemDescription, '';'') Item
				from #tbl_temp_po_nonshop_detail pond 
				where pond.PONonShoppingId = PO.Id 
				group by pond.Id
				FOR JSON PATH
			)
		) 
			, ''[{{'', ''''), ''}}]'', ''''), ''}},{{'', ''; ''), ''""Item"":""'', ''''), ''""'', '''')
		)
	[Description],
			vd.StatusTransfer,
			(CASE
					WHEN vd.StatusTransfer = 1 THEN
						''Success''
					WHEN vd.StatusTransfer = 0 THEN
						''Failed''
					ELSE
						''''
				END
			) [StatusTransferDesc],
			scv.Id [VendorCategoryId],
			scv.SubCategoryName VendorType,
			(
				SELECT DISTINCT TOP 1 v.[Id]
			) VendorId,
			(
				SELECT DISTINCT TOP 1 v.[Name]
			) VendorName,
			(CASE
					WHEN scv.SubCategoryName = ''Staff'' THEN
					(
						SELECT DISTINCT TOP 1 v.EmployeeCode
					)
					ELSE
				(
					SELECT DISTINCT TOP 1 v.Code
				)
				END
			) VendorCode,
			''' + ISNULL(NULLIF(@RequestType, ''), 'Non Shopping Cart') + ''' AS [RequestType],
			MONTH(apprv.ApprovalDate) [Month],
			PRF.RequestDate [RequestDate],
			CONVERT(VARCHAR(20), PRF.RequestDate, 113) RequestDateString,
			apprv.ApprovalDate ReceivedByFinanceDate,
			(
				CASE WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN
					IPO.InvoiceDate
				ELSE
					DNP.LastUpdatedTime
				END
			) ReceivedByFinance,
			CONVERT(VARCHAR(20), vh.TransferTime, 113) PaidByFinance,
			'''' SettlementDate,
			'''' ReceivedSettlementByFinance,
			'''' StatusRepair,
			'''' DueDate,
			'''' OverdueDays,
			'''' StatusOverdue,
			'''' [BalanceAmount],
			''''[DueToCompany],
			null [TransferDateDueToCompany],
			'''' [RealizationAmount],
			(
				CASE
					WHEN DATEDIFF(
						YEAR,
						CASE 
							WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
							ELSE DNP.LastUpdatedTime
						END,
						vh.TransferTime
					) >= 1
					THEN (
						DATEDIFF(
							DAY,
							CASE
								WHEN CAST(
									CASE 
										WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
										ELSE DNP.LastUpdatedTime
									END AS TIME
								) > CAST(@CutOffHour AS TIME)
								AND DATEDIFF(
									DAY,
									CASE 
										WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
										ELSE DNP.LastUpdatedTime
									END,
									vh.TransferTime
								) > 0
								THEN DATEADD(
									DAY, 1,
									CASE 
										WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
										ELSE DNP.LastUpdatedTime
									END
								)
								ELSE
									CASE 
										WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
										ELSE DNP.LastUpdatedTime
									END
							END,
							vh.TransferTime
						)
						- 2 * (
							(DATEPART(WEEK, vh.TransferTime) + 52 * DATEDIFF(
								YEAR,
								CASE
									WHEN CAST(
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END AS TIME
									) > CAST(@CutOffHour AS TIME)
									AND DATEDIFF(
										DAY,
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END,
										vh.TransferTime
									) > 0
									THEN DATEADD(
										DAY, 1,
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END
									)
									ELSE
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END
								END,
								vh.TransferTime
							))
							- DATEPART(
								WEEK,
								CASE
									WHEN CAST(
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END AS TIME
									) > CAST(@CutOffHour AS TIME)
									AND DATEDIFF(
										DAY,
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END,
										vh.TransferTime
									) > 0
									THEN DATEADD(
										DAY, 1,
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END
									)
									ELSE
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END
								END
							)
						)
						- (
							SELECT COUNT(*)
							FROM #tbl_temp_holiday
							WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime
							  AND Status = 1
							  AND DATEPART(WEEKDAY, DateHoliday) NOT IN (1, 7)
						)
					)
					ELSE (
						DATEDIFF(
							DAY,
							CASE
								WHEN CAST(
									CASE 
										WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
										ELSE DNP.LastUpdatedTime
									END AS TIME
								) > CAST(@CutOffHour AS TIME)
								AND DATEDIFF(
									DAY,
									CASE 
										WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
										ELSE DNP.LastUpdatedTime
									END,
									vh.TransferTime
								) > 0
								THEN DATEADD(
									DAY, 1,
									CASE 
										WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
										ELSE DNP.LastUpdatedTime
									END
								)
								ELSE
									CASE 
										WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
										ELSE DNP.LastUpdatedTime
									END
							END,
							vh.TransferTime
						)
						- 2 * (
							DATEPART(WEEK, vh.TransferTime)
							- DATEPART(
								WEEK,
								CASE
									WHEN CAST(
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END AS TIME
									) > CAST(@CutOffHour AS TIME)
									AND DATEDIFF(
										DAY,
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END,
										vh.TransferTime
									) > 0
									THEN DATEADD(
										DAY, 1,
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END
									)
									ELSE
										CASE 
											WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN IPO.InvoiceDate
											ELSE DNP.LastUpdatedTime
										END
								END
							)
						)
						- (
							SELECT COUNT(*)
							FROM #tbl_temp_holiday
							WHERE DateHoliday BETWEEN apprv.ApprovalDate AND vh.TransferTime
							  AND Status = 1
							  AND DATEPART(WEEKDAY, DateHoliday) NOT IN (1, 7)
						)
					)
				END
			) [SLA],
			'''' NewAdvanceNumber,
			'''' NewAdvanceAmount,
			'''' NewAdvanceTransferDate,
			detail.x Detail,
			''-'' [Product],
			''-'' [ProjectNo],
			''-'' [Affiliate],
			(
				SELECT DISTINCT TOP 1 ipo.BankAccountOwnerName
			) [Beneficiaries],
			(
				SELECT DISTINCT TOP 1 ipo.BankAccountNumber
			) [BankAccountNumber],
			(
				SELECT DISTINCT TOP 1 ipo.BankName
			) [BankName],
			(
				SELECT DISTINCT TOP 1 ipo.LCurrency
			) [LCurrencyCode],
			(
				SELECT DISTINCT TOP 1 REPLACE(FORMAT(POD.RateAmount, ''C''), ''$'', '''')
			) [Rate],
			REPLACE(FORMAT(detailamount.TotalAmount, ''C''), ''$'', '''') [Amount],
			REPLACE(FORMAT(costcenter.TotalAmount, ''C''), ''$'', '''') [NettAmount],
			othercost.ot [OtherCosts],
			costsplit.x [CostSplit],
			apprv.member ApprovalGroupMembers,
			'''' DocumentNumber,
			Attachments.AttachmentIds,
			'''' PPH21,
			'''' PPN
			FROM #tbl_temp_approvalrequest AR
				WITH(NOLOCK)
							JOIN #tbl_temp_prf PRF WITH(NOLOCK)
								ON AR.Id = PRF.ApprovalRequestId
							JOIN #tbl_temp_accountmaster AM WITH(NOLOCK)
								ON PRF.BudgetCode = AM.AccountCode
							JOIN #tbl_temp_prf_summary PFS WITH(NOLOCK)
								ON PRF.Id = PFS.PRFId
							JOIN #tbl_temp_po_nonshop PO WITH(NOLOCK)
								ON PFS.Id = PO.PRFSummaryId
							JOIN #tbl_temp_vendor v WITH(NOLOCK)
								ON PO.VendorId = v.Id
							JOIN #tbl_temp_subcategory scv WITH(NOLOCK)
								ON v.SubCategoryId = scv.Id
							JOIN #tbl_temp_po_nonshop_detail POD WITH(NOLOCK)
								ON PO.Id = POD.PONonShoppingId
							JOIN #tbl_temp_invoice_po IPO WITH(NOLOCK)
									ON PO.Id = IPO.PurchaeseOrderId
							JOIN #tbl_temp_po_nonshop_top POTOP
								 ON POTOP.PONonShoppingId = PO.Id
							JOIN #tbl_temp_deliverynotes_payment DNP
								 ON DNP.PurchaseOrderTOPId = IPO.PurchaseOrderTOPId
								 AND (SELECT SubCategoryCode FROM #tbl_temp_subcategory WHERE Id = DNP.CategoryProcess_SubCategoryId) = ''SC-2024-02-01262''
								 AND DNP.Status = 2
								OUTER APPLY
							(
								SELECT SUM(IPDD.DPPAmount) [TotalAmount] FROM InvoicePODetail IPDD
								WHERE IPDD.InvoicePOId = IPO.Id
							) 
							detailamount
								OUTER APPLY
							(
								SELECT
									(
										SELECT DISTINCT
											AM.Id [AccountMasterId],
											AM.AccountCode [AccountMasterCode],
											AM.ShortDescription [AccountMasterName],
											AM.MtAccountType,
											IPO1.InvoiceNumber [InvoiceNo],
											(select replace(
				replace(
					replace (
						replace (
							replace(
		(select 
			(select STRING_AGG(ItemDescription, '';'') Item
				from #tbl_temp_po_nonshop_detail pond 
				where pond.PONonShoppingId = PO.Id 
				group by pond.Id
				FOR JSON PATH
			)
		) 
			, ''[{{'', ''''), ''}}]'', ''''), ''}},{{'', ''; ''), ''""Item"":""'', ''''), ''""'', '''')
		) DescriptionDetail
										FROM #tbl_temp_invoice_po IPO1
										WITH(NOLOCK)
										WHERE IPO1.Id = IPO.Id
												AND IPO1.CategoryProcess_SubCategoryId = (SELECT Id FROM SubCategory SCX WHERE SCX.SubCategoryCode = ''SC-2024-02-01262'')
										FOR JSON PATH
									) x
							) detail
							JOIN #tbl_temp_voucher_detail vd  WITH(NOLOCK)
								ON cast(ipo.Id as varchar(100)) = substring(vd.VoucherRefId,0,charindex('' - '',vd.VoucherRefId))
							JOIN #tbl_temp_voucher_header vh  WITH(NOLOCK)
								ON vd.VoucherId = vh.Id and vh.Category = ''Non Shopping Cart''
							LEFT JOIN #tmp_argm_dap apprv
								ON PFS.[ApprovalRequestId] = apprv.[ApprovalRequestId]
							OUTER APPLY
			(
							SELECT
								(
									SELECT  REPLACE(FORMAT(SUM(ipoc.TotalBaseAmount), ''C''), ''$'', '''') BasicAmount,
										    REPLACE(FORMAT(SUM(ipoc.Amount), ''C''), ''$'', '''') Amount,
											REPLACE(FORMAT(SUM(ipoc.AmountGrossUp), ''C''), ''$'', '''') GrossUp,
											scoc.SubCategoryName OtherCostSubCategoryCode
									FROM #tbl_temp_invoicepo_othercost ipoc
										WITH(NOLOCK)
										JOIN #tbl_temp_subcategory scoc WITH(NOLOCK)
											ON ipoc.OtherCost_SubCategoryId = scoc.Id
									WHERE ipoc.InvoicePOId = IPO.Id
									GROUP BY scoc.SubCategoryName
									FOR JSON PATH
								) ot
			) othercost
							OUTER APPLY
			(
							SELECT SUM(INPO.TotalAmount) [TotalAmount] FROM InvoicePO INPO WHERE INPO.PurchaeseOrderId = PO.Id and INPO.Id = IPO.Id 
			) costcenter
							OUTER APPLY
			(
							SELECT
								(
									SELECT DISTINCT
										bu.Id BusinessUnitId,
										bu.Name BusinessUnitName,
										bu.Code BusinessUnitCode,
										cc.Id CostCenterId,
										cc.[Name] CostCenterName,
										cc.Code CostCenterCode
									FROM PONonShoppingDetailCostCenter pocc
									WITH(NOLOCK)
										JOIN PONonShoppingDetail podc WITH(NOLOCK)
											on pocc.PONonShoppingDetailId = podc.Id
										JOIN #tbl_temp_costcenter cc WITH(NOLOCK)
											ON pocc.CostCenterId = cc.Id
										JOIN #tbl_temp_businessunit bu WITH(NOLOCK)
											ON cc.BusinessUnitId = bu.Id
									WHERE podc.PONonShoppingId = po.Id 
										AND ' + @subquery2 + '
									FOR JSON PATH
								) x
			) costsplit
							OUTER APPLY
			(
							SELECT JSON_QUERY(
									(
										SELECT ATT.*
										FROM
										(
											SELECT ATH.Id AttachmentIdRequest,
													NULL AttachmentIdVoucher
											FROM #tbl_temp_attachment ATH WITH(NOLOCK)
											WHERE RefId IN ( PRF.Id )
													AND ATH.Category IN ( ''PurchaseRequestForm'', ''QuotationFormVendor'', ''ProcurementSummary'' )
											UNION ALL
											SELECT ATH.Id AttachmentIdRequest,
													NULL AttachmentIdVoucher
											FROM #tbl_temp_attachment ATH WITH(NOLOCK)
											WHERE ATH.RefId IN ( PO.Id )
													AND ATH.Category IN ( ''PO'' )
													AND ATH.Description = ''Non-ShoppingCart''
											UNION ALL
											SELECT ATH.Id AttachmentIdRequest,
													NULL AttachmentIdVoucher
											FROM #tbl_temp_attachment ATH WITH(NOLOCK)
											WHERE ATH.RefId IN (
																	SELECT DN.Id
																	FROM DeliveryNotes DN WITH(NOLOCK)
																	WHERE DN.PurchaseOrderId = PO.Id
																		AND DN.CategoryProcess_SubCategoryId = (SELECT Id FROM SubCategory SCX WHERE SCX.SubCategoryCode = ''SC-2024-02-01262'')
																)
													AND ATH.Category IN ( ''DN'' )
											UNION ALL
											SELECT ATH.Id AttachmentIdRequest,
													NULL AttachmentIdVoucher
											FROM #tbl_temp_attachment ATH WITH(NOLOCK)
											WHERE ATH.RefId IN ( PO.Id )
													AND ATH.Category IN ( ''INV'' )
													AND ATH.Description = ''Non Shopping Cart''
													
											UNION ALL
											SELECT ATH.Id AttachmentIdRequest,
													NULL AttachmentIdVoucher
											FROM #tbl_temp_attachment ATH WITH(NOLOCK)
											where ATH.Id = vh.Attachment

										) ATT
                   
										FOR JSON PATH
									)) AttachmentIds
			) Attachments
			WHERE IPO.CategoryProcess_SubCategoryId = (SELECT Id FROM #tbl_temp_subcategory SCX WHERE SCX.SubCategoryCode = ''SC-2024-02-01262'') 
			and ' + @subquery2 + ' '

		IF OBJECT_ID('tempdb..#tbl_temp_transaction_nonshopcart') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_nonshopcart
		END
		CREATE TABLE #tbl_temp_transaction_nonshopcart(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(max),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(100),
			VendorId int,
			VendorName varchar(100),
			VendorCode varchar(100),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(100),
			ProjectNo varchar(100),
			Affiliate varchar(100),
			Beneficiaries varchar(250),
			BankAccountNumber varchar(100),
			BankName varchar(100),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(100),
			PPN varchar(100)
		)
		INSERT INTO #tbl_temp_transaction_nonshopcart
		EXEC sp_executesql @sqlQueryNonShoppingCart, @paramSLA, @CutOffHour, @BusinessUnitId, @CostCenterId, @AccountMasterId
		--SELECT *FROM #tbl_temp_transaction_nonshopcart
	END

	IF (@RequestType = 'TREX-APR' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		IF (@AccountMasterId IS NULL OR @AccountMasterId = '')
		BEGIN
			SET @subquery2 = REPLACE(@subquery2, 'am.Id = @AccountMasterId', '1=1')
		END
		DECLARE @sqlQueryTrexApr NVARCHAR(MAX) 
		SET @sqlQueryTrexApr = '
		SELECT
			  DISTINCT vh.TransferNumber,
			  vh.VoucherNumber,
			  vh.Id AS VoucherId,
			  r.NoAPR AS RequestNumber,
			  r.[RequestorName] AS RequestorName,
			  '''' AS SettlementNumber,
			  vh.CreatedBy AS MakerFinance,
			  r.Status AS RequestStatus,
			  r.Description,
			  vd.StatusTransfer,
			  CASE
			    WHEN vd.StatusTransfer = 1 THEN ''Success''
			    WHEN vd.StatusTransfer = 0 THEN ''Failed''
			    ELSE ''''
			  END AS StatusTransferDesc,
			  r.IsVendorPayment AS VendorCategoryId,
			  CASE
			    r.IsVendorPayment
			    WHEN 0 THEN ''Staff''
			    ELSE ''Vendor''
			  END AS VendorType,
			  '''' AS VendorId,
			  CASE
			    r.IsVendorPayment
			    WHEN 0 THEN r.BeneficiaryEmployeeName
			    ELSE r.BeneficiaryVendorName
			  END AS VendorName,
			  '''' AS VendorCode,
			  ''TREX-APR'' AS RequestType,
			  MONTH(r.[SendToFinanceDates]) AS MONTH,
			  r.[RequestDates] AS RequestDate,
			  CONVERT(VARCHAR(20), r.[RequestDates], 113) AS RequestDateString,
			  r.[SendToFinanceDates] AS ReceivedByFinanceDate,
			  CONVERT(VARCHAR(20), r.[SendToFinanceDates], 113) AS ReceivedByFinance,
			  CONVERT(VARCHAR(20), vh.TransferTime, 113) AS PaidByFinance,
			  '''' AS SettlementDate,
			  '''' AS ReceivedSettlementByFinance,
			  '''' AS StatusRepair,
			  CONVERT(VARCHAR(20), DATEADD(DAY, 31, vh.TransferTime), 113) AS DueDate,
			  '''' AS OverdueDays,
			  '''' AS StatusOverdue,
			  '''' AS BalanceAmount,
			  '''' AS DueToCompany,
			  '''' AS TransferDateDueToCompany,
			  '''' AS RealizationAmount,
			  (CASE WHEN (DATEDIFF(year,r.[SendToFinanceDates], vh.TransferTime))>=1
				THEN
				((DATEDIFF(DAY, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime))
				- 2 * ((DATEPART(week, vh.TransferTime) + 52 * 
				DATEDIFF(year,
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime)) 
				- DATEPART(week, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END))))
				- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN r.[SendToFinanceDates] AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
				ELSE
				((DATEDIFF(DAY, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime)) 
				- 2 * (DATEPART(week, vh.TransferTime) 
				- DATEPART(week, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END))))
				- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN r.[SendToFinanceDates] AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
			  END) [SLA],
			  '''' AS NewAdvanceNumber,
			  '''' AS NewAdvanceAmount,
			  '''' AS NewAdvanceTransferDate,
			  detail.x AS Detail,
			  ''-'' AS Product,
			  ''-'' AS ProjectNo,
			  ''-'' AS Affiliate,
			  r.BankAccountName AS Beneficiaries,
			  r.BankAccountNumber AS BankAccountNumber,
			  r.BankName AS BankName,
			  ''IDR'' AS LCurrencyCode,
			  1 AS Rate,
			  REPLACE(FORMAT(vd.TotalBaseAmmount, ''C''), ''$'', '''') AS Amount,
			  REPLACE(FORMAT(vd.TotalBaseAmmount, ''C''), ''$'', '''') AS NettAmount,
			  '''' AS OtherCosts,
			  costsplit.x AS CostSplit,
			  '''' AS ApprovalGroupMembers,
			  '''' AS DocumentNumber,
			  '''' AS AttachmentIds,
			  '''' AS PPH21,
			  '''' AS PPN
			FROM
			  #tbl_temp_trexapr r
			  JOIN #tbl_temp_voucher_detail vd ON r.NoAPR = vd.VoucherRefId
			  JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
			  OUTER APPLY (	SELECT (SELECT DISTINCT '''' AccountMasterId, [AccountCode] AccountMasterCode, [AccountDescription] AccountMasterName, ''''MtAccountType, '''' InvoiceNo, rd1.[Description] DescriptionDetail
						FROM #tbl_temp_trexapr rd1
						WHERE r.NoAPR = rd1.NoAPR
						FOR JSON PATH) x
					) detail
			  OUTER APPLY (	SELECT (SELECT DISTINCT bu.Id BusinessUnitId, bu.Name BusinessUnitName, bu.Code BusinessUnitCode, 
						cc.Id CostCenterId, cc.[Name] CostCenterName, cc.Code CostCenterCode
						FROM #tbl_temp_trexapr rcc1
						JOIN #tbl_temp_costcenter cc ON rcc1.DepartmentCode = cc.Code
						JOIN #tbl_temp_businessunit bu ON cc.BusinessUnitId = bu.Id
						WHERE r.NoAPR = rcc1.NoAPR
						FOR JSON PATH) x
					) costsplit
			WHERE
			  1 = 1
		'

		IF OBJECT_ID('tempdb..#tbl_temp_transaction_trexapr') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_trexapr
		END
		CREATE TABLE #tbl_temp_transaction_trexapr(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(500),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(100),
			VendorId int,
			VendorName varchar(100),
			VendorCode varchar(100),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(100),
			ProjectNo varchar(100),
			Affiliate varchar(100),
			Beneficiaries varchar(250),
			BankAccountNumber varchar(100),
			BankName varchar(100),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(100),
			PPN varchar(100)
		)
		--select @sqlQueryTrexApr
		INSERT INTO #tbl_temp_transaction_trexapr
		EXEC sp_executesql @sqlQueryTrexApr,  @paramSLA, @CutOffHour, @BusinessUnitId, @CostCenterId, @AccountMasterId
		--SELECT * FROM #tbl_temp_transaction_trexapr
	END

	IF (@RequestType = 'TREX-EER' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		IF (@AccountMasterId IS NULL OR @AccountMasterId = '')
		BEGIN
			SET @subquery2 = REPLACE(@subquery2, 'am.Id = @AccountMasterId', '1=1')
		END
		DECLARE @sqlQueryTrexEer NVARCHAR(MAX) 
		SET @sqlQueryTrexEer = '
		SELECT
			  DISTINCT vh.TransferNumber,
			  vh.VoucherNumber,
			  vh.Id AS VoucherId,
			  r.NoEER AS RequestNumber,
			  r.[RequestorName] AS RequestorName,
			  '''' AS SettlementNumber,
			  vh.CreatedBy AS MakerFinance,
			  r.Status AS RequestStatus,
			  r.Description,
			  vd.StatusTransfer,
			  CASE
			    WHEN vd.StatusTransfer = 1 THEN ''Success''
			    WHEN vd.StatusTransfer = 0 THEN ''Failed''
			    ELSE ''''
			  END AS StatusTransferDesc,
			  '''' AS VendorCategoryId,
			  ''Staff'' VendorType,
			  '''' AS VendorId,
			  r.RequestorName VendorName,
			  '''' AS VendorCode,
			  ''TREX-EER'' AS RequestType,
			  MONTH(r.[SendToFinanceDates]) AS MONTH,
			  r.[RequestDates] AS RequestDate,
			  CONVERT(VARCHAR(20), r.[RequestDates], 113) AS RequestDateString,
			  r.[SendToFinanceDates] AS ReceivedByFinanceDate,
			  CONVERT(VARCHAR(20), r.[SendToFinanceDates], 113) AS ReceivedByFinance,
			  CONVERT(VARCHAR(20), vh.TransferTime, 113) AS PaidByFinance,
			  '''' AS SettlementDate,
			  '''' AS ReceivedSettlementByFinance,
			  '''' AS StatusRepair,
			  CONVERT(VARCHAR(20), DATEADD(DAY, 31, vh.TransferTime), 113) AS DueDate,
			  '''' AS OverdueDays,
			  '''' AS StatusOverdue,
			  '''' AS BalanceAmount,
			  '''' AS DueToCompany,
			  '''' AS TransferDateDueToCompany,
			  '''' AS RealizationAmount,
			  (CASE WHEN (DATEDIFF(year,r.[SendToFinanceDates], vh.TransferTime))>=1
				THEN
				((DATEDIFF(DAY, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime))
				- 2 * ((DATEPART(week, vh.TransferTime) + 52 * 
				DATEDIFF(year,
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime)) 
				- DATEPART(week, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END))))
				- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN r.[SendToFinanceDates] AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
				ELSE
				((DATEDIFF(DAY, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime)) 
				- 2 * (DATEPART(week, vh.TransferTime) 
				- DATEPART(week, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END))))
				- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN r.[SendToFinanceDates] AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
			  END) [SLA],
			  '''' AS NewAdvanceNumber,
			  '''' AS NewAdvanceAmount,
			  '''' AS NewAdvanceTransferDate,
			  detail.x AS Detail,
			  ''-'' AS Product,
			  ''-'' AS ProjectNo,
			  ''-'' AS Affiliate,
			  r.BankAccountName AS Beneficiaries,
			  r.BankAccountNumber AS BankAccountNumber,
			  r.BankName AS BankName,
			  ''IDR'' AS LCurrencyCode,
			  1 AS Rate,
			  REPLACE(FORMAT(vd.TotalBaseAmmount, ''C''), ''$'', '''') AS Amount,
			  REPLACE(FORMAT(vd.TotalBaseAmmount, ''C''), ''$'', '''') AS NettAmount,
			  '''' AS OtherCosts,
			  costsplit.x AS CostSplit,
			  '''' AS ApprovalGroupMembers,
			  '''' AS DocumentNumber,
			  '''' AS AttachmentIds,
			  '''' AS PPH21,
			  '''' AS PPN
			FROM
			  #tbl_temp_trexeer r
			  JOIN #tbl_temp_voucher_detail vd ON r.NoEER = vd.VoucherRefId
			  JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
			  OUTER APPLY (	SELECT (SELECT DISTINCT '''' AccountMasterId, '''' AccountMasterCode, '''' AccountMasterName, ''''MtAccountType, '''' InvoiceNo, rd1.[Description] DescriptionDetail
						FROM #tbl_temp_trexeer rd1
						WHERE r.NoEER = rd1.NoEER
						FOR JSON PATH) x
					) detail
			  OUTER APPLY (	SELECT (SELECT DISTINCT bu.Id BusinessUnitId, bu.Name BusinessUnitName, bu.Code BusinessUnitCode, 
						cc.Id CostCenterId, cc.[Name] CostCenterName, cc.Code CostCenterCode
						FROM #tbl_temp_trexeer rcc1
						JOIN #tbl_temp_costcenter cc ON rcc1.DepartmentCode = cc.Code
						JOIN #tbl_temp_businessunit bu ON cc.BusinessUnitId = bu.Id
						WHERE r.NoEER = rcc1.NoEER
						FOR JSON PATH) x
					) costsplit
			WHERE
			  1 = 1
		'

		IF OBJECT_ID('tempdb..#tbl_temp_transaction_trexeer') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_trexeer
		END
		CREATE TABLE #tbl_temp_transaction_trexeer(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(500),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(100),
			VendorId int,
			VendorName varchar(100),
			VendorCode varchar(100),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(100),
			ProjectNo varchar(100),
			Affiliate varchar(100),
			Beneficiaries varchar(250),
			BankAccountNumber varchar(100),
			BankName varchar(100),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(100),
			PPN varchar(100)
		)
		--select @sqlQueryTrexEer
		INSERT INTO #tbl_temp_transaction_trexeer
		EXEC sp_executesql @sqlQueryTrexEer , @paramSLA, @CutOffHour, @BusinessUnitId, @CostCenterId, @AccountMasterId
		--SELECT * FROM #tbl_temp_transaction_trexeer
	END

	IF (@RequestType LIKE 'TREX-GER%' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		IF (@AccountMasterId IS NULL OR @AccountMasterId = '')
		BEGIN
			SET @subquery2 = REPLACE(@subquery2, 'am.Id = @AccountMasterId', '1=1')
		END
		DECLARE @sqlQueryTrexGer NVARCHAR(MAX) 
		SET @sqlQueryTrexGer = '
		SELECT
			  DISTINCT vh.TransferNumber,
			  vh.VoucherNumber,
			  vh.Id AS VoucherId,
			  r.NoGER AS RequestNumber,
			  r.[RequestorName] AS RequestorName,
			  '''' AS SettlementNumber,
			  vh.CreatedBy AS MakerFinance,
			  r.Status AS RequestStatus,
			  r.Description,
			  vd.StatusTransfer,
			  CASE
			    WHEN vd.StatusTransfer = 1 THEN ''Success''
			    WHEN vd.StatusTransfer = 0 THEN ''Failed''
			    ELSE ''''
			  END AS StatusTransferDesc,
			  '''' AS VendorCategoryId,
			  ''Staff'' VendorType,
			  '''' AS VendorId,
			  r.RequestorName VendorName,
			  '''' AS VendorCode,
			  CONCAT(''TREX-GER '', r.GERType ) AS RequestType,
			  MONTH(r.[SendToFinanceDates]) AS MONTH,
			  r.[RequestDates] AS RequestDate,
			  CONVERT(VARCHAR(20), r.[RequestDates], 113) AS RequestDateString,
			  r.[SendToFinanceDates] AS ReceivedByFinanceDate,
			  CONVERT(VARCHAR(20), r.[SendToFinanceDates], 113) AS ReceivedByFinance,
			  CONVERT(VARCHAR(20), vh.TransferTime, 113) AS PaidByFinance,
			  '''' AS SettlementDate,
			  '''' AS ReceivedSettlementByFinance,
			  '''' AS StatusRepair,
			  CONVERT(VARCHAR(20), DATEADD(DAY, 31, vh.TransferTime), 113) AS DueDate,
			  '''' AS OverdueDays,
			  '''' AS StatusOverdue,
			  '''' AS BalanceAmount,
			  '''' AS DueToCompany,
			  '''' AS TransferDateDueToCompany,
			  '''' AS RealizationAmount,
			  (CASE WHEN (DATEDIFF(year,r.[SendToFinanceDates], vh.TransferTime))>=1
				THEN
				((DATEDIFF(DAY, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime))
				- 2 * ((DATEPART(week, vh.TransferTime) + 52 * 
				DATEDIFF(year,
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime)) 
				- DATEPART(week, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END))))
				- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN r.[SendToFinanceDates] AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
				ELSE
				((DATEDIFF(DAY, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime)) 
				- 2 * (DATEPART(week, vh.TransferTime) 
				- DATEPART(week, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END))))
				- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN r.[SendToFinanceDates] AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
			  END) [SLA],
			  '''' AS NewAdvanceNumber,
			  '''' AS NewAdvanceAmount,
			  '''' AS NewAdvanceTransferDate,
			  detail.x AS Detail,
			  ''-'' AS Product,
			  ''-'' AS ProjectNo,
			  ''-'' AS Affiliate,
			  r.BankAccountName AS Beneficiaries,
			  r.BankAccountNumber AS BankAccountNumber,
			  r.BankName AS BankName,
			  ''IDR'' AS LCurrencyCode,
			  1 AS Rate,
			  REPLACE(FORMAT(vd.TotalBaseAmmount, ''C''), ''$'', '''') AS Amount,
			  REPLACE(FORMAT(vd.TotalBaseAmmount, ''C''), ''$'', '''') AS NettAmount,
			  othercost.ot AS OtherCosts,
			  costsplit.x AS CostSplit,
			  '''' AS ApprovalGroupMembers,
			  '''' AS DocumentNumber,
			  '''' AS AttachmentIds,
			  '''' AS PPH21,
			  '''' AS PPN
			FROM
			  #tbl_temp_trexger r
			  JOIN #tbl_temp_voucher_detail vd ON r.NoGER = vd.VoucherRefId
			  JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
			  OUTER APPLY (	SELECT (SELECT DISTINCT '''' AccountMasterId, '''' AccountMasterCode, '''' AccountMasterName, ''''MtAccountType, '''' InvoiceNo, rd1.[Description] DescriptionDetail
						FROM #tbl_temp_trexger rd1
						WHERE r.NoGER = rd1.NoGER
						FOR JSON PATH) x
					) detail
			  OUTER APPLY (	SELECT (SELECT DISTINCT bu.Id BusinessUnitId, bu.Name BusinessUnitName, bu.Code BusinessUnitCode, 
						cc.Id CostCenterId, cc.[Name] CostCenterName, cc.Code CostCenterCode
						FROM #tbl_temp_trexger rcc1
						JOIN #tbl_temp_costcenter cc ON rcc1.DepartmentCode = cc.Code
						JOIN #tbl_temp_businessunit bu ON cc.BusinessUnitId = bu.Id
						WHERE r.NoGER = rcc1.NoGER
						FOR JSON PATH) x
					) costsplit
			  OUTER APPLY ( SELECT(
						SELECT REPLACE(FORMAT(SUM(combined.BasicAmount), ''C''),''$'','''') BasicAmount, 
							   REPLACE(FORMAT(SUM(combined.Amount), ''C''),''$'','''') Amount, 
							   REPLACE(FORMAT(SUM(ISNULL(combined.GrossUp,0)), ''C''),''$'','''') GrossUp,
							   scoc.SubCategoryCode OtherCostSubCategoryCode
						FROM (
						    SELECT  tgcd.Amount * (COALESCE(tgcd.TaxRate1, 0)/100) [BasicAmount]
								   ,tgcd.Amount * (COALESCE(tgcd.TaxRate1, 0)/100) [Amount]
								   ,0 [GrossUp]
								   ,CASE WHEN LOWER(tgcd.TaxCode1) LIKE ''%ppn%'' THEN ''PPN''
										 WHEN LOWER(tgcd.TaxCode1) LIKE ''%pph final%'' THEN ''PPH42''
									END AS TaxName
						    FROM #tbl_temp_trexger_accountcode_detail tgcd
						    WHERE TaxName1 IS NOT NULL AND tgcd.GERId = r.GERId
						
						    UNION ALL
						
						    SELECT  tgcd.Amount * (COALESCE(tgcd.TaxRate2, 0)/100) [BasicAmount]
								   ,tgcd.Amount * (COALESCE(tgcd.TaxRate2, 0)/100) [Amount]
								   ,0 [GrossUp]
								   ,CASE WHEN LOWER(tgcd.TaxCode2) LIKE ''%ppn%'' THEN ''PPN''
										 WHEN LOWER(tgcd.TaxCode2) LIKE ''%pph final%'' THEN ''PPH42''
									END AS TaxName
						    FROM #tbl_temp_trexger_accountcode_detail tgcd
						    WHERE TaxName2 IS NOT NULL AND tgcd.GERId = r.GERId
						) AS combined
						JOIN #tbl_temp_subcategory scoc on scoc.SubCategoryCode = combined.TaxName
						WHERE scoc.CategoryId = (SELECT TOP 1 Id FROM Category WHERE CategoryName = ''OtherCost'')
						GROUP BY TaxName, SubCategoryCode

						FOR JSON PATH) ot
						) othercost
			  WHERE 1 = 1
		'

		IF OBJECT_ID('tempdb..#tbl_temp_transaction_trexger') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_trexger
		END
		CREATE TABLE #tbl_temp_transaction_trexger(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(500),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(100),
			VendorId int,
			VendorName varchar(100),
			VendorCode varchar(100),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(100),
			ProjectNo varchar(100),
			Affiliate varchar(100),
			Beneficiaries varchar(250),
			BankAccountNumber varchar(100),
			BankName varchar(100),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(100),
			PPN varchar(100)
		)
		--select @sqlQueryTrexGer
		INSERT INTO #tbl_temp_transaction_trexger
		EXEC sp_executesql @sqlQueryTrexGer , @paramSLA, @CutOffHour, @BusinessUnitId, @CostCenterId, @AccountMasterId
		--SELECT * FROM #tbl_temp_transaction_trexger
	END

	IF (@RequestType = 'TREX-TER' OR @RequestType = '' OR @RequestType IS NULL)
	BEGIN
		IF (@AccountMasterId IS NULL OR @AccountMasterId = '')
		BEGIN
			SET @subquery2 = REPLACE(@subquery2, 'am.Id = @AccountMasterId', '1=1')
		END
		DECLARE @sqlQueryTrexTer NVARCHAR(MAX) 
		SET @sqlQueryTrexTer = '
		SELECT
			  DISTINCT vh.TransferNumber,
			  vh.VoucherNumber,
			  vh.Id AS VoucherId,
			  r.NoTER AS RequestNumber,
			  r.[RequestorName] AS RequestorName,
			  '''' AS SettlementNumber,
			  vh.CreatedBy AS MakerFinance,
			  r.Status AS RequestStatus,
			  r.Description,
			  vd.StatusTransfer,
			  CASE
			    WHEN vd.StatusTransfer = 1 THEN ''Success''
			    WHEN vd.StatusTransfer = 0 THEN ''Failed''
			    ELSE ''''
			  END AS StatusTransferDesc,
			  '''' AS VendorCategoryId,
			  ''Staff'' VendorType,
			  '''' AS VendorId,
			  r.RequestorName VendorName,
			  '''' AS VendorCode,
			  ''TREX-TER'' AS RequestType,
			  MONTH(r.[SendToFinanceDates]) AS MONTH,
			  r.[RequestDates] AS RequestDate,
			  CONVERT(VARCHAR(20), r.[RequestDates], 113) AS RequestDateString,
			  r.[SendToFinanceDates] AS ReceivedByFinanceDate,
			  CONVERT(VARCHAR(20), r.[SendToFinanceDates], 113) AS ReceivedByFinance,
			  CONVERT(VARCHAR(20), vh.TransferTime, 113) AS PaidByFinance,
			  '''' AS SettlementDate,
			  '''' AS ReceivedSettlementByFinance,
			  '''' AS StatusRepair,
			  CONVERT(VARCHAR(20), DATEADD(DAY, 31, vh.TransferTime), 113) AS DueDate,
			  '''' AS OverdueDays,
			  '''' AS StatusOverdue,
			  '''' AS BalanceAmount,
			  '''' AS DueToCompany,
			  '''' AS TransferDateDueToCompany,
			  '''' AS RealizationAmount,
			  (CASE WHEN (DATEDIFF(year,r.[SendToFinanceDates], vh.TransferTime))>=1
				THEN
				((DATEDIFF(DAY, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime))
				- 2 * ((DATEPART(week, vh.TransferTime) + 52 * 
				DATEDIFF(year,
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime)) 
				- DATEPART(week, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END))))
				- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN r.[SendToFinanceDates] AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
				ELSE
				((DATEDIFF(DAY, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END), vh.TransferTime)) 
				- 2 * (DATEPART(week, vh.TransferTime) 
				- DATEPART(week, 
					(CASE WHEN CAST(r.[SendToFinanceDates] as time) > CAST( @CutOffHour as time) AND DATEDIFF(DAY, r.[SendToFinanceDates], vh.TransferTime) > 0 THEN DATEADD(DAY, 1, r.[SendToFinanceDates]) 
						ELSE r.[SendToFinanceDates] END))))
				- (SELECT COUNT(*) FROM #tbl_temp_holiday WHERE DateHoliday BETWEEN r.[SendToFinanceDates] AND vh.TransferTime AND Status = 1 AND DATEPART(dw, DateHoliday) NOT IN (1, 7))
			  END) [SLA],
			  '''' AS NewAdvanceNumber,
			  '''' AS NewAdvanceAmount,
			  '''' AS NewAdvanceTransferDate,
			  detail.x AS Detail,
			  ''-'' AS Product,
			  ''-'' AS ProjectNo,
			  ''-'' AS Affiliate,
			  r.BankAccountName AS Beneficiaries,
			  r.BankAccountNumber AS BankAccountNumber,
			  r.BankName AS BankName,
			  ''IDR'' AS LCurrencyCode,
			  1 AS Rate,
			  REPLACE(FORMAT(vd.TotalBaseAmmount, ''C''), ''$'', '''') AS Amount,
			  REPLACE(FORMAT(vd.TotalBaseAmmount, ''C''), ''$'', '''') AS NettAmount,
			  othercost.ot AS OtherCosts,
			  costsplit.x AS CostSplit,
			  '''' AS ApprovalGroupMembers,
			  '''' AS DocumentNumber,
			  '''' AS AttachmentIds,
			  '''' AS PPH21,
			  '''' AS PPN
			FROM
			  #tbl_temp_trexter r
			  JOIN #tbl_temp_voucher_detail vd ON r.NoTER = vd.VoucherRefId
			  JOIN #tbl_temp_voucher_header vh ON vd.VoucherId = vh.Id
			  OUTER APPLY (	SELECT (SELECT DISTINCT '''' AccountMasterId, '''' AccountMasterCode, '''' AccountMasterName, ''''MtAccountType, '''' InvoiceNo, rd1.[Description] DescriptionDetail
						FROM #tbl_temp_trexter rd1
						WHERE r.NoTER = rd1.NoTER
						FOR JSON PATH) x
					) detail
			  OUTER APPLY (	SELECT (SELECT DISTINCT bu.Id BusinessUnitId, bu.Name BusinessUnitName, bu.Code BusinessUnitCode, 
						cc.Id CostCenterId, cc.[Name] CostCenterName, cc.Code CostCenterCode
						FROM #tbl_temp_trexter rcc1
						JOIN #tbl_temp_costcenter cc ON rcc1.DepartmentCode = cc.Code
						JOIN #tbl_temp_businessunit bu ON cc.BusinessUnitId = bu.Id
						WHERE r.NoTER = rcc1.NoTER
						FOR JSON PATH) x
					) costsplit
			  OUTER APPLY ( SELECT(
						SELECT REPLACE(FORMAT(SUM(0), ''C''),''$'','''') BasicAmount, 
							   REPLACE(FORMAT(SUM(0), ''C''),''$'','''') Amount, 
							   REPLACE(FORMAT(SUM(0), ''C''),''$'','''') GrossUp,
							   '''' OtherCostSubCategoryCode
						FROM #tbl_temp_trexter a
						WHERE a.TERId = r.TERId

						FOR JSON PATH) ot
						) othercost
			  WHERE 1 = 1
		'

		IF OBJECT_ID('tempdb..#tbl_temp_transaction_trexter') IS NOT NULL 
		BEGIN 
			DROP TABLE #tbl_temp_transaction_trexter
		END
		CREATE TABLE #tbl_temp_transaction_trexter(
			TransferNumber varchar(100),
			VoucherNumber varchar(100),
			VoucherId int,
			RequestNumber varchar(100),
			RequestorName varchar(100),
			SettlementNumber varchar(100),
			MakerFinance varchar(100),
			RequestStatus smallint,
			Description varchar(500),
			StatusTransfer smallint,
			StatusTransferDesc varchar(100),
			VendorCategoryId int,
			VendorType varchar(100),
			VendorId int,
			VendorName varchar(100),
			VendorCode varchar(100),
			RequestType varchar(100),
			[Month] int,
			RequestDate datetime,
			RequestDateString varchar(100),
			ReceivedByFinanceDate datetime,
			ReceivedByFinance varchar(100),
			PaidByFinance varchar(100),
			SettlementDate varchar(100),
			ReceivedSettlementByFinance datetime,
			StatusRepair varchar(500),
			DueDate varchar(100),
			OverdueDays int,
			StatusOverdue varchar(100),
			BalanceAmount varchar(100),
			DueToCompany varchar(100),
			TransferDateDueToCompany varchar(100),
			RealizationAmount varchar(100),
			SLA int, 
			NewAdvanceNumber varchar(100),
			NewAdvanceAmount varchar(100),
			NewAdvanceTransferDate varchar(100),
			Detail varchar(max),
			Product varchar(100),
			ProjectNo varchar(100),
			Affiliate varchar(100),
			Beneficiaries varchar(250),
			BankAccountNumber varchar(100),
			BankName varchar(100),
			LCurrencyCode varchar(100),
			Rate varchar(100),
			Amount varchar(100),
			NettAmount varchar(100),
			OtherCosts varchar(MAX),
			CostSplit varchar(MAX),
			ApprovalGroupMembers varchar(MAX),
			DocumentNumber varchar(100),
			AttachmentIds varchar(max),
			PPH21 varchar(100),
			PPN varchar(100)
		)
		--select @sqlQueryTrexTer
		INSERT INTO #tbl_temp_transaction_trexter
		EXEC sp_executesql @sqlQueryTrexTer , @paramSLA, @CutOffHour, @BusinessUnitId, @CostCenterId, @AccountMasterId
		--SELECT * FROM #tbl_temp_transaction_trexter
	END


	DECLARE @filter VARCHAR(MAX) = 'RequestType = @RequestType AND (RequestNumber = @RequestNumber OR SettlementNumber = @RequestNumber) 
				AND VoucherNumber = @VoucherNumber AND VoucherId = @VoucherId AND TransferNumber = @TransferNumber 
				AND RequestorName = @RequestorName AND MakerFinance = @MakerFinance 
				AND VendorCategoryId = @VendorCategoryId AND VendorId = @VendorId 
				AND RequestStatus = @RequestStatus AND StatusTransfer = @StatusTransfer AND LCurrencyCode = @LCurrencyCode 
				AND CAST(RequestDate as date) BETWEEN CAST(@RequestDateFrom as date) AND CAST(@RequestDateTo as date)
				AND CAST(PaidByFinance as date) BETWEEN CAST(@PaymentDateFrom as date) AND CAST(@PaymentDateTo as date)
				AND Detail IS NOT NULL'

	IF (@RequestType IS NULL OR @RequestType = '')
	BEGIN
		SET @filter = REPLACE(@filter, 'RequestType = @RequestType', '1=1')
	END
	IF (@RequestNumber IS NULL OR @RequestNumber = '')
	BEGIN
		SET @filter = REPLACE(@filter, '(RequestNumber = @RequestNumber OR SettlementNumber = @RequestNumber)', '1=1')
	END
	IF (@VoucherNumber IS NULL OR @VoucherNumber = '')
	BEGIN
		SET @filter = REPLACE(@filter, 'VoucherNumber = @VoucherNumber', '1=1')
	END
	IF (@VoucherId IS NULL OR @VoucherId = '')
	BEGIN
		SET @filter = REPLACE(@filter, 'VoucherId = @VoucherId', '1=1')
	END
	IF (@TransferNumber IS NULL OR @TransferNumber = '')
	BEGIN
		SET @filter = REPLACE(@filter, 'TransferNumber = @TransferNumber', '1=1')
	END
	IF (@RequestorName IS NULL OR @RequestorName = '')
	BEGIN
		SET @filter = REPLACE(@filter, 'RequestorName = @RequestorName', '1=1')
	END
	IF (@MakerFinance IS NULL OR @MakerFinance = '')
	BEGIN
		SET @filter = REPLACE(@filter, 'MakerFinance = @MakerFinance', '1=1')
	END
	IF (@VendorCategoryId IS NULL OR @VendorCategoryId = '')
	BEGIN
		SET @filter = REPLACE(@filter, 'VendorCategoryId = @VendorCategoryId', '1=1')
	END
	IF (@VendorId IS NULL OR @VendorId = '')
	BEGIN
		SET @filter = REPLACE(@filter, 'VendorId = @VendorId', '1=1')
	END
	IF (@RequestStatus IS NULL OR @RequestStatus = '')
	BEGIN
		SET @filter = REPLACE(@filter, 'RequestStatus = @RequestStatus', '1=1')
	END
	IF (@StatusTransfer IS NULL OR @StatusTransfer = '')
	BEGIN
		SET @filter = REPLACE(@filter, 'StatusTransfer = @StatusTransfer', '1=1')
	END
	IF (@StatusTransfer IS NOT NULL AND @StatusTransfer = '2')
	BEGIN
		SET @filter = REPLACE(@filter, 'StatusTransfer = @StatusTransfer', 'StatusTransfer is null')
	END
	IF (@LCurrencyCode IS NULL OR @LCurrencyCode = '')
	BEGIN
		SET @filter = REPLACE(@filter, 'LCurrencyCode = @LCurrencyCode', '1=1')
	END
	IF ((@RequestDateFrom IS NULL OR @RequestDateFrom = '') AND (@RequestDateTo IS NULL OR @RequestDateTo = ''))
	BEGIN
		SET @filter = REPLACE(@filter, 'CAST(RequestDate as date) BETWEEN CAST(@RequestDateFrom as date) AND CAST(@RequestDateTo as date)', '1=1')
	END
	IF ((@PaymentDateFrom IS NULL OR @PaymentDateFrom = '') AND (@PaymentDateTo IS NULL OR @PaymentDateTo = ''))
	BEGIN
		SET @filter = REPLACE(@filter, 'CAST(PaidByFinance as date) BETWEEN CAST(@PaymentDateFrom as date) AND CAST(@PaymentDateTo as date)', '1=1')
	END

	-- query select inquiry payment
	DECLARE @sqlQuery NVARCHAR(MAX)

	IF (@RequestType = 'reimbursement')
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT * FROM #tbl_temp_transaction_reimbursement
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE IF (@RequestType = 'cash advance')
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT * FROM #tbl_temp_transaction_cash_advance
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE IF (@RequestType = 'cash advance travel')
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT * FROM #tbl_temp_transaction_cash_advance_travel
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE IF (@RequestType = 'travel settlement')
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT * FROM #tbl_temp_transaction_travelsettlement
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE IF (@RequestType = 'invoice travel')
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT * FROM #tbl_temp_transaction_invoice_travel
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE IF (@RequestType in ('ger', 'COMBEN', 'CONTEST', 'OTHERS'))
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT * FROM #tbl_temp_transaction_ger
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE IF (@RequestType = 'purchase order')
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT *FROM #tbl_temp_transaction_shopcart
							UNION ALL
							SELECT *FROM #tbl_temp_transaction_nonshopcart
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE IF (@RequestType = 'Shopping Cart')
	BEGIN
	--SELECT *FROM #tbl_temp_transaction_shopcart; return;
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT *FROM #tbl_temp_transaction_shopcart
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE IF (@RequestType = 'Non Shopping Cart')
	BEGIN
	--SELECT * FROM #tbl_temp_transaction_nonshopcart; return;
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT * FROM #tbl_temp_transaction_nonshopcart
						 ) sub
						 WHERE ' + @filter + ''
	END
	
	ELSE IF (@RequestType = 'TREX-APR')
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT *FROM #tbl_temp_transaction_trexapr
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE IF (@RequestType = 'TREX-EER')
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT *FROM #tbl_temp_transaction_trexeer
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE IF (@RequestType LIKE 'TREX-GER%')
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT *FROM #tbl_temp_transaction_trexger
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE IF (@RequestType = 'TREX-TER')
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT *FROM #tbl_temp_transaction_trexter
						 ) sub
						 WHERE ' + @filter + ''
	END

	ELSE 
	BEGIN
		SET @sqlQuery = 'SELECT *,COUNT(*) OVER () as CountData FROM (
							SELECT *FROM #tbl_temp_transaction_reimbursement
							UNION ALL
							SELECT *FROM #tbl_temp_transaction_cash_advance
							UNION ALL
							SELECT *FROM #tbl_temp_transaction_cash_advance_travel
							UNION ALL
							SELECT *FROM #tbl_temp_transaction_travelsettlement
							UNION ALL
							SELECT *FROM #tbl_temp_transaction_invoice_travel
							UNION ALL
							SELECT *FROM #tbl_temp_transaction_ger
							UNION ALL
							SELECT *FROM #tbl_temp_transaction_shopcart
							UNION ALL
							SELECT *FROM #tbl_temp_transaction_nonshopcart
						 ) sub
						 WHERE ' + @filter + ''
	END

	IF (@IsExport = 1)
	BEGIN
		SET @sqlQuery = CONCAT(@sqlQuery, ' ORDER BY ReceivedByFinanceDate ASC')
	END
	ELSE
	BEGIN
		SET @sqlQuery = CONCAT(@sqlQuery, ' ORDER BY ', @SortColumn,' ', @SortDirection,' OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY')		
	END
	DECLARE @params NVARCHAR(MAX)
	SET @params = N'@RequestType NVARCHAR(100), @RequestNumber NVARCHAR(100), @VoucherNumber NVARCHAR(100), @VoucherId NVARCHAR(100), @TransferNumber NVARCHAR(100), 
				@RequestorName NVARCHAR(100), @MakerFinance NVARCHAR(100), @VendorCategoryId NVARCHAR(100), @VendorId NVARCHAR(100), 
				@AccountMasterId NVARCHAR(100), @RequestStatus NVARCHAR(100), @StatusTransfer NVARCHAR(100), @LCurrencyCode NVARCHAR(100), 
				@RequestDateFrom NVARCHAR(100), @RequestDateTo NVARCHAR(100), @PaymentDateFrom NVARCHAR(100), @PaymentDateTo NVARCHAR(100), 
				@SortColumn NVARCHAR(100), @SortDirection NVARCHAR(100), @Page int, @PageSize int'

	--SELECT @sqlQuery

	EXEC sp_executesql @sqlQuery, @params, @RequestType, @RequestNumber, @VoucherNumber, @VoucherId, @TransferNumber, @RequestorName, @MakerFinance, 
	@VendorCategoryId, @VendorId, @AccountMasterId, @RequestStatus, @StatusTransfer, @LCurrencyCode, @RequestDateFrom, @RequestDateTo, @PaymentDateFrom, @PaymentDateTo, 
	@SortColumn, @SortDirection, @Page, @PageSize

END
GO


