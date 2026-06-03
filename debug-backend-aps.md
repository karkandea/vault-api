using APS_Common;
using APS_Common.Const;
using APS_Common.Extensions.DataTables.Request;
using APS_Common.Extensions.DataTables.Response;
using APS_Common.Helper;
using APS_Entities.Models;
using APS_REST_API.Contracts;
using APS_REST_API.Models.Report.DueDiligence;
using APS_REST_API.Models.Request;
using APS_REST_API.Payloads.Request.Report;
using APS_REST_API.Payloads.Response.Report;
using APS_REST_API.Queries.Report;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace APS_REST_API.Repository.Report
{
    public class ReportRepository : IReportRepository
    {
        private readonly IDapper _dapper;
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly string CutOffHour;
        public ReportRepository(IDapper dapper, ISubCategoryRepository subCategoryRepository, IConfiguration configuration)
        {
            _dapper = dapper;
            _subCategoryRepository = subCategoryRepository;
            CutOffHour = $"{configuration.GetValue<string>($"CutOffHour")}";
        }

        private readonly Logging log = new()
        {
            objectName = nameof(ReportRepository)
        };

        /// <summary>
        /// Get Report Payment List
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReportPaymentSummaryListResponse>> GetReportPaymentList(ReportPaymentListRequest param)
        {
            try
            {
                string qry = "[usp_GetInquiryPayment]";
                var paymentList = await Task.FromResult(_dapper.GetAll<ReportPaymentSummaryListResponse>(qry, new Dapper.DynamicParameters(new
                {
                    param.Page,
                    param.PageSize,
                    param.RequestType,
                    param.RequestNumber,
                    param.VoucherNumber,
                    param.VoucherId,
                    param.TransferNumber,
                    param.VendorCategoryId,
                    param.VendorId,
                    param.AccountMasterId,
                    param.BusinessUnitId,
                    param.CostCenterId,
                    RequestStatus = param.Status,
                    param.StatusTransfer,
                    LCurrencyCode = param.Currency,
                    param.RequestorName,
                    param.MakerFinance,
                    param.RequestDateFrom,
                    param.RequestDateTo,
                    CutOffHour,
                    param.IsExport,
                    SortColumn = param.SortColumn ?? AppSystem.DefaultSortColumnFinance,
                    SortDirection = param.SortDirection ?? AppSystem.DefaultSortColumnFinance
                }), commandType: CommandType.StoredProcedure));

                if (paymentList.Count != 0)
                {
                    paymentList = await SetPaymentResponseItems(paymentList);
                }
                return paymentList;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(log.objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Set Payment List Detail
        /// </summary>
        /// <param name="paymentList"></param>
        /// <returns></returns>
        private async Task<List<ReportPaymentSummaryListResponse>> SetPaymentResponseItems(List<ReportPaymentSummaryListResponse> paymentList)
        {
            var costSplits = new List<ReportCostCenterSplitResponse>();
            var otherCosts = new List<ReportOtherCostResponse>();
            var approvals = new List<ApprovalRequestGroupMember>();
            foreach (var itemPayment in paymentList.ToList())
            {
                // set BusinessUnit and CostCenter from SplitCostCenter
                if (!string.IsNullOrEmpty(itemPayment.CostSplit)) costSplits = JsonConvert.DeserializeObject<List<ReportCostCenterSplitResponse>>(itemPayment.CostSplit);
                itemPayment.CostSplit = null;
                ReportPaymentListResponse paymentListResponse = await SetterCostCenterSplit(costSplits);
                itemPayment.BusinessUnitCode = paymentListResponse.BusinessUnitCode;
                itemPayment.BusinessUnitName = paymentListResponse.BusinessUnitName;
                itemPayment.CostCenterCode = paymentListResponse.CostCenterCode;
                itemPayment.CostCenterName = paymentListResponse.CostCenterName;
                costSplits = [];

                // set default other costs
                if (!string.IsNullOrEmpty(itemPayment.OtherCosts))
                    otherCosts = JsonConvert.DeserializeObject<List<ReportOtherCostResponse>>(itemPayment.OtherCosts);
                itemPayment.OtherCosts = "see on otherCostModels";
                itemPayment.OtherCostModels = otherCosts;
                ReportDefaultOtherCost defaultOtherCost = await SetterDefaultOtherCosts(otherCosts, itemPayment.VoucherNumber);
                itemPayment.Ppn = defaultOtherCost.Ppn;
                itemPayment.Pph23 = defaultOtherCost.Pph23;
                itemPayment.Pph42 = defaultOtherCost.Pph42;
                itemPayment.Pph21 = defaultOtherCost.Pph21;
                itemPayment.GrossUp = defaultOtherCost.GrossUp;

                itemPayment.StampDuty = defaultOtherCost.StampDuty;
                otherCosts = [];

                // set approval group members
                if (!string.IsNullOrEmpty(itemPayment.ApprovalGroupMembers))
                    approvals = JsonConvert.DeserializeObject<List<ApprovalRequestGroupMember>>(itemPayment.ApprovalGroupMembers);
                itemPayment.ApprovalGroupMembers = "see on approvalRequestGroupMemberModels";
                itemPayment.ApprovalRequestGroupMemberModel = approvals;
                approvals = [];
            }
            return paymentList;
        }

        /// <summary>
        /// Get Report Payment List On Summary
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReportPaymentSummaryListResponse>> GetReportPaymentSummaryList(ReportPaymentListRequest param)
        {
            try
            {
                string qry = "[usp_GetInquiryPaymentSummary]";
                var paymentList = await Task.FromResult(_dapper.GetAll<ReportPaymentSummaryListResponse>(qry, new Dapper.DynamicParameters(new
                {
                    param.Page,
                    param.PageSize,
                    param.RequestType,
                    param.RequestNumber,
                    param.VoucherNumber,
                    param.VoucherId,
                    param.TransferNumber,
                    param.VendorCategoryId,
                    param.VendorId,
                    param.AccountMasterId,
                    param.BusinessUnitId,
                    param.CostCenterId,
                    RequestStatus = param.Status,
                    param.StatusTransfer,
                    LCurrencyCode = param.Currency,
                    param.RequestorName,
                    param.MakerFinance,
                    param.RequestDateFrom,
                    param.RequestDateTo,
                    param.PaymentDateFrom,
					param.PaymentDateTo,
					CutOffHour,
                    param.IsExport,
                    SortColumn = param.SortColumn ?? AppSystem.DefaultSortColumnFinance,
                    SortDirection = param.SortDirection ?? AppSystem.DefaultSortColumnFinance
                }), commandType: CommandType.StoredProcedure));

                if (paymentList.Count != 0)
                {
                    paymentList = await SetPaymentSummaryResponseItems(paymentList);
                }
                return paymentList;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(log.objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Set Summary Payment List
        /// </summary>
        /// <param name="paymentList"></param>
        /// <returns></returns>
        private async Task<List<ReportPaymentSummaryListResponse>> SetPaymentSummaryResponseItems(List<ReportPaymentSummaryListResponse> paymentList)
        {
            string delimiter = ", ";
            var details = new List<ReportSummaryDetailResponse>();
            var costSplits = new List<ReportCostCenterSplitResponse>();
            var otherCosts = new List<ReportOtherCostResponse>();
            var approvals = new List<ApprovalRequestGroupMember>();
            foreach (var itemPaymentSummary in paymentList.ToList())
            {
                // set AccountMaster and Invoice from Detail
                if (!string.IsNullOrEmpty(itemPaymentSummary.Detail)) details = JsonConvert.DeserializeObject<List<ReportSummaryDetailResponse>>(itemPaymentSummary.Detail);
                itemPaymentSummary.Detail = null;
                if (details.Count != 0)
                {
                    itemPaymentSummary.AccountMasterId = details.Select(x => x.AccountMasterId).Distinct().Aggregate((i, j) => i + delimiter + j);
                    itemPaymentSummary.AccountMasterCode = details.Select(x => x.AccountMasterCode).Distinct().Aggregate((i, j) => i + delimiter + j);
                    itemPaymentSummary.AccountMasterName = details.Select(x => x.AccountMasterName).Distinct().Aggregate((i, j) => i + delimiter + j);
                    itemPaymentSummary.MtAccountType = details.Select(x => x.MtAccountType).Distinct().Aggregate((i, j) => i + delimiter + j);
                    itemPaymentSummary.InvoiceNo = details.Select(x => x.InvoiceNo).Distinct().Aggregate((i, j) => i + delimiter + j);
                    itemPaymentSummary.DescriptionDetail = details.Select(x => x.DescriptionDetail).Distinct().Aggregate((i, j) => i + delimiter + j);
                    if (itemPaymentSummary.RequestType.Equals("travel settlement", StringComparison.CurrentCultureIgnoreCase))
                    {
                        itemPaymentSummary.VendorType = details.Select(x => x.VendorType).Distinct().Aggregate((i, j) => i + delimiter + j);
                    }
                }
                details = [];

                // set BusinessUnit and CostCenter from SplitCostCenter
                if (!string.IsNullOrEmpty(itemPaymentSummary.CostSplit))
                    costSplits = JsonConvert.DeserializeObject<List<ReportCostCenterSplitResponse>>(itemPaymentSummary.CostSplit);
                itemPaymentSummary.CostSplit = null;
                ReportPaymentListResponse paymentListResponse = await SetterCostCenterSplit(costSplits);
                itemPaymentSummary.BusinessUnitCode = paymentListResponse.BusinessUnitCode;
                itemPaymentSummary.BusinessUnitName = paymentListResponse.BusinessUnitName;
                itemPaymentSummary.CostCenterCode = paymentListResponse.CostCenterCode;
                itemPaymentSummary.CostCenterName = paymentListResponse.CostCenterName;
                costSplits = [];

                // set default other costs
                if (!string.IsNullOrEmpty(itemPaymentSummary.OtherCosts))
                    otherCosts = JsonConvert.DeserializeObject<List<ReportOtherCostResponse>>(itemPaymentSummary.OtherCosts);
                itemPaymentSummary.OtherCosts = "see on otherCostModels";
                itemPaymentSummary.OtherCostModels = otherCosts;
                ReportDefaultOtherCost defaultOtherCost = await SetterDefaultOtherCosts(otherCosts, itemPaymentSummary.VoucherNumber);
                itemPaymentSummary.Ppn = defaultOtherCost.Ppn;
                itemPaymentSummary.Pph23 = defaultOtherCost.Pph23;
                itemPaymentSummary.Pph42 = defaultOtherCost.Pph42;
                itemPaymentSummary.Pph21 = defaultOtherCost.Pph21;
                itemPaymentSummary.GrossUp = defaultOtherCost.GrossUp;
                itemPaymentSummary.StampDuty = defaultOtherCost.StampDuty;
                otherCosts = [];

                // set approval group members
                if (!string.IsNullOrEmpty(itemPaymentSummary.ApprovalGroupMembers))
                    approvals = JsonConvert.DeserializeObject<List<ApprovalRequestGroupMember>>(itemPaymentSummary.ApprovalGroupMembers);
                itemPaymentSummary.ApprovalGroupMembers = "see on approvalRequestGroupMemberModels";
                itemPaymentSummary.ApprovalRequestGroupMemberModel = approvals;
                approvals = [];

                // set attachmentIds
                itemPaymentSummary.AttachmentIds = SetAttachmentIds(itemPaymentSummary.AttachmentIds);
            }
            return paymentList;
        }

        /// <summary>
        /// Getter Setter Default Other Cost
        /// Redundant condition, can using same category tax for all requests
        /// </summary>
        /// <param name="otherCosts"></param>
        /// <param name="voucherNumber"></param>
        /// <returns></returns>
        public async Task<ReportDefaultOtherCost> SetterDefaultOtherCosts(List<ReportOtherCostResponse> otherCosts, string voucherNumber)
        {
            ReportDefaultOtherCost defaultOtherCost = new()
            {
                Ppn = string.Empty,
                Pph23 = string.Empty,
                Pph42 = string.Empty,
                StampDuty = string.Empty
            };
            if (otherCosts.Count != 0)
            {
				if (!string.IsNullOrEmpty(voucherNumber) && voucherNumber.Contains("NON SHOPPING CART"))
				{
					foreach (var other in otherCosts)
					{
						var cleaned = other.OtherCostSubCategoryCode;

						cleaned = cleaned.Replace("-", "");
						cleaned = cleaned.Replace(" ", "");
						cleaned = cleaned.Replace("final", "", StringComparison.OrdinalIgnoreCase);
						cleaned = cleaned.Replace(".", "");
						cleaned = cleaned.Replace("(", "").Replace(")", "");

						other.OtherCostSubCategoryCode = cleaned;
					}
				}

				var itemType = defaultOtherCost.GetType();
				foreach (var cost in otherCosts)
				{
					var prop = itemType.GetProperty(cost.OtherCostSubCategoryCode, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

					if (prop != null && prop.CanWrite)
					{
						prop.SetValue(defaultOtherCost, cost.Amount);
					}
				}
				defaultOtherCost.GrossUp = otherCosts.ToDictionary(item => item.OtherCostSubCategoryCode, item => item.GrossUp);
			}
            await Task.Delay(0);
            return defaultOtherCost;
        }

        /// <summary>
        /// Getter Setter Cost Center Split
        /// </summary>
        /// <param name="costSplits"></param>
        /// <returns></returns>
        public async Task<ReportPaymentListResponse> SetterCostCenterSplit(List<ReportCostCenterSplitResponse> costSplits)
        {
            string delimiter = ", ";
            ReportPaymentListResponse paymentListResponse = new()
            {
                BusinessUnitCode = string.Empty,
                BusinessUnitName = string.Empty,
                CostCenterCode = string.Empty,
                CostCenterName = string.Empty
            };
            if (costSplits.Count != 0)
            {
                paymentListResponse.BusinessUnitCode = costSplits.Select(x => x.BusinessUnitCode).Distinct().Aggregate((i, j) => i + delimiter + j);
                paymentListResponse.BusinessUnitName = costSplits.Select(x => x.BusinessUnitName).Distinct().Aggregate((i, j) => i + delimiter + j);
                paymentListResponse.CostCenterCode = costSplits.Select(x => x.CostCenterCode).Distinct().Aggregate((i, j) => i + delimiter + j);
                paymentListResponse.CostCenterName = costSplits.Select(x => x.CostCenterName).Distinct().Aggregate((i, j) => i + delimiter + j);
            }
            await Task.Delay(0);
            return paymentListResponse;
        }

        /// <summary>
        /// Get Query Inquiry Budget Transactions
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private async Task<string> GetQueryReportBudgetTransactions(ParamGetRequestList param)
        {
            string subQuery = $@"1 = 1
									AND p.Id = @ProjectId
									AND am.Id = @AccountMasterId
									AND bu.Id = @BusinessUnitId
									AND cc.Id = @CostCenterId
									AND CAST(@RequestDateFrom as date) BETWEEN CAST(PeriodFrom as date) AND CAST(PeriodTo as date)
									AND CAST(@RequestDateTo as date) BETWEEN CAST(PeriodFrom as date) AND CAST(PeriodTo as date)";

            if (string.IsNullOrEmpty(param.ProjectId) || param.ProjectId == "all")
                subQuery = subQuery.Replace("p.Id = @ProjectId", "1=1");
            if (string.IsNullOrEmpty(param.AccountMasterId) || param.AccountMasterId == "all")
                subQuery = subQuery.Replace("am.Id = @AccountMasterId", "1=1");
            if (string.IsNullOrEmpty(param.BusinessUnitId) || param.BusinessUnitId == "all")
                subQuery = subQuery.Replace("bu.Id = @BusinessUnitId", "1=1");
            if (string.IsNullOrEmpty(param.CostCenterId) || param.CostCenterId == "all")
                subQuery = subQuery.Replace("cc.Id = @CostCenterId", "1=1");

            if (string.IsNullOrEmpty(param.RequestDateFrom))
                subQuery = subQuery.Replace("CAST(@RequestDateFrom as date) BETWEEN CAST(PeriodFrom as date) AND CAST(PeriodTo as date)", "1=1");
            if (string.IsNullOrEmpty(param.RequestDateTo))
                subQuery = subQuery.Replace("CAST(@RequestDateTo as date) BETWEEN CAST(PeriodFrom as date) AND CAST(PeriodTo as date)", "1=1");

            if (!string.IsNullOrEmpty(param.Status) && param.Status != "all")
            {
                var statusSubCategory = await _subCategoryRepository.GetSubCategory(int.Parse(param.Status));

                string subQueryStatus = statusSubCategory?.SubCategoryCode.ToLower() switch
                {
                    "actual" => $" AND r.Status != 0 AND vd.StatusTransfer = 1 AND bt.InBudget = 1 ",
                    "additional" => $" AND b.AditionalBudget != null OR b.AditionalBudget != 0 ",
                    "process" => $@" AND r.Status != 0 AND (r.Status NOT IN (3,5) OR vd.StatusTransfer IS NULL) 
									 AND (r.Status NOT IN (9) OR vd.StatusTransfer NOT IN (0,1)) 
									 AND bt.InBudget = 1 ",
                    "cancel" => $" AND (r.Status IN (3,5) OR vd.StatusTransfer = 0) AND bt.InBudget = 0 ",
                    _ => string.Empty
                };
                subQuery = string.Concat(subQuery, subQueryStatus);
            }

            if (!string.IsNullOrEmpty(param.Search?.Value))
            {
                subQuery = string.Concat(subQuery, " AND (am.AccountCode LIKE @Search OR am.ShortDescription LIKE @Search OR am.MtAccountType LIKE @Search OR bu.Name LIKE @Search OR cc.Name LIKE @Search OR vh.TransferNumber LIKE @Search OR r.RequestNumber LIKE @Search) ");
            }

            #region shopNonShopQuery
            string shopNonShopQuery = "and 1 = 1 -- conjunction init syntax dummy";
            if (!string.IsNullOrEmpty(param.ProjectId) && param.ProjectId != "all") shopNonShopQuery += $"{Environment.NewLine}and mp.Id = @ProjectId";
            if (!string.IsNullOrEmpty(param.AccountMasterId) && param.AccountMasterId != "all") shopNonShopQuery += $"{Environment.NewLine}and am.Id = @AccountMasterId";
            if (!string.IsNullOrEmpty(param.BusinessUnitId) && param.BusinessUnitId != "all") shopNonShopQuery += $"{Environment.NewLine}and bu.Id = @BusinessUnitId";
            if (!string.IsNullOrEmpty(param.CostCenterId) && param.CostCenterId != "all") shopNonShopQuery += $"{Environment.NewLine}and cc.Id = @CostCenterId";
            if (!string.IsNullOrEmpty(param.RequestDateFrom)) shopNonShopQuery += @$"
-- M_Budget -- and cast(@RequestDateFrom as date) between cast(mb.PeriodFrom as date) and cast(mb.PeriodTo as date) -- M_Budget
-- PurchaseRequest -- and cast(@RequestDateFrom as date) <= cast(pr.RequestDates as date) -- PurchaseRequest
-- PurchaseOrder -- and cast(@RequestDateFrom as date) <= cast(pr.RequestDates as date) -- PurchaseOrder use PurchaseRequest
-- PRF -- and cast(@RequestDateFrom as date) <= cast(p.RequestDate as date) -- PRF
-- PONonShopping M_BudgetId is null -- and cast(@RequestDateFrom as date) <= cast(p.RequestDate as date) -- PONonShopping use PRF
";
            if (!string.IsNullOrEmpty(param.RequestDateTo)) shopNonShopQuery += @$"
-- M_Budget -- and cast(@RequestDateTo as date) between cast(mb.PeriodFrom as date) and cast(mb.PeriodTo as date) -- M_Budget
-- PurchaseRequest -- and cast(pr.RequestDates as date) <= cast(@RequestDateTo as date) -- PurchaseRequest
-- PurchaseOrder -- and cast(pr.RequestDates as date) <= cast(@RequestDateTo as date) -- PurchaseOrder use PurchaseRequest
-- PRF -- and cast(p.RequestDate as date) <= cast(@RequestDateTo as date) -- PRF
-- PONonShopping M_BudgetId is null -- and cast(p.RequestDate as date) <= cast(@RequestDateTo as date) -- PONonShopping use PRF
";
            if (!string.IsNullOrEmpty(param.Status) && param.Status != "all")
            {
                var subCategoryCode = (await _subCategoryRepository.GetSubCategory(int.Parse(param.Status)))?.SubCategoryCode.ToLower();

                shopNonShopQuery += subCategoryCode switch
                {
                    "actual" => $"{Environment.NewLine}/* {subCategoryCode} */ and bt.InBudget = 1 and coalesce(vd.StatusTransfer,0) = 1",
                    "process" => $"{Environment.NewLine}/* {subCategoryCode} */ and bt.InBudget = 1 and coalesce(vd.StatusTransfer,0) = 0",
                    "cancel" => $"{Environment.NewLine}/* {subCategoryCode} */ and bt.InBudget = 0",
                    _ => $"{Environment.NewLine}/* {subCategoryCode} */ and 1 = 0 -- not available"
                };
            }
            if (!string.IsNullOrEmpty(param.Search?.Value))
            {
                shopNonShopQuery += @"
and (
	1 = 0 -- disjunction init syntax dummy
	or am.AccountCode like @Search
	or am.ShortDescription like @Search
	or am.MtAccountType like @Search
	or bu.Name like @Search
	or cc.Name like @Search
	or vh.TransferNumber like @Search
	-- PurchaseRequest -- or pr.RequestCode like @Search
	-- PurchaseOrder -- or po.PONumber like @Search
	-- PRF -- or p.PRFNo like @Search
	-- PONonShopping -- or pons.PONumber like @Search
)
";
            }
            shopNonShopQuery = $@"
-- SHOP , PR , DETAILCC , BUDGET
union all
select
am.AccountCode as AccountMasterCode
, am.Description as AccountMasterName
, am.MtAccountType as MtAccountType
, bu.Code as BusinessUnitCode
, bu.Name as BusinessUnitName
, cc.Code as CostCenterCode
, cc.Name as CostCenterName
, bt.L_Currency_Code as LCurrencyCode
, bt.RateAmount as Rate
, bt.BasicAmount as NettAmount
, bt.Amount as NettAmountCurrency
, mt.Name as StatusRequestDesc
, null as StatusTransfer -- vd
, null as StatusTransferDesc -- vd
, null as ReceivedByFinance -- ipo
, null as PaidByFinance -- vh
, null as TransferNumber -- vh
, pr.RequestCode as RequestNumber
, v_sc.SubCategoryName as VendorType
, v.Name as VendorName
, v.Code as VendorCode
, sc.SubCategoryName as RequestType
, null as InvoiceNo -- ipo
, prid.RequestorNotes as Description
, prid.IsBudgeted as IsBudget
, coalesce(argm.ApprovalGroupMembers, '[]') as ApprovalGroupMembers
, argm.CountApprovalNoBudget as CountApprovalNoBudget

from BudgetTransaction as bt

inner join SubCategory as sc on sc.Id = bt.Transaction_SubCategoryId and sc.SubCategoryCode = 'Transaction.SC' -- SHOP Transaction.SC / NONSHOP Transaction.PRF

inner join M_Budget as mb on mb.Id = bt.M_BudgetId
inner join M_Project as mp on mp.ProjectCode = mb.ProjectCode
inner join AccountMaster as am on am.Id = mb.AccountMasterId
inner join CostCenter as cc on cc.Id = mb.CostCenterId
inner join BusinessUnit as bu on bu.Id = cc.BusinessUnitId

inner join PurchaseRequestItemCostCenter as pricc on pricc.Id = bt.RequestDetailCostCenterId
inner join PurchaseRequestItemDetail as prid on prid.Id = pricc.PurchaseRequestItemDetailId
inner join Vendor as v on v.Id = prid.VendorId
inner join SubCategory as v_sc on v_sc.Id = v.SubCategoryId
inner join PurchaseRequest as pr on pr.Id = prid.PurchaseRequestId
inner join MasterTable as mt on mt.Category = 'PurchaseRequest.Status' and mt.ValueId = pr.Status -- PRF.Status / PurchaseOrder.Status / PurchaseRequest.Status

outer apply (
	select (
		select
		argm.UserName
		, argm.Comment
		, (case when sc.SubCategoryCode = 'ApprovalNoBudget' then 0 else 1 end) as IsApprovalBudget
		from ApprovalRequestGroupMember as argm
		inner join SubCategory as sc on argm.ApprovaGroup_SubCategoryId = sc.Id
		where argm.ApprovalRequestId = pr.ApprovalRequestId
		order by argm.CreatedTime asc
		for json path
	) as ApprovalGroupMembers
	, (
		select
		count(1) as CountApprovalNoBudget
		from ApprovalRequestGroupMember as argm
		inner join SubCategory as sc on argm.ApprovaGroup_SubCategoryId = sc.Id and sc.SubCategoryCode = 'ApprovalNoBudget'
		where argm.ApprovalRequestId = pr.ApprovalRequestId
	) as CountApprovalNoBudget
) as argm

-- dummy for filter
left join (select top 0 * from VoucherHeader) as vh on 1 = 1
left join (select top 0 * from VoucherDetail) as vd on 1 = 1

where left(bt.RefNumber,3) = 'PR_' -- PR
and bt.RequestDetailCostCenterId is not null -- DETAILCC
and bt.M_BudgetId is not null -- BUDGET
{shopNonShopQuery.Replace("-- PurchaseRequest -- ", "").Replace("-- M_Budget -- ", "")}

-- SHOP , PR , DETAILCC , NONBUDGET
union all
select
am.AccountCode as AccountMasterCode
, am.Description as AccountMasterName
, am.MtAccountType as MtAccountType
, bu.Code as BusinessUnitCode
, bu.Name as BusinessUnitName
, cc.Code as CostCenterCode
, cc.Name as CostCenterName
, bt.L_Currency_Code as LCurrencyCode
, bt.RateAmount as Rate
, bt.BasicAmount as NettAmount
, bt.Amount as NettAmountCurrency
, mt.Name as StatusRequestDesc
, null as StatusTransfer -- vd
, null as StatusTransferDesc -- vd
, null as ReceivedByFinance -- ipo
, null as PaidByFinance -- vh
, null as TransferNumber -- vh
, pr.RequestCode as RequestNumber
, v_sc.SubCategoryName as VendorType
, v.Name as VendorName
, v.Code as VendorCode
, sc.SubCategoryName as RequestType
, null as InvoiceNo -- ipo
, prid.RequestorNotes as Description
, prid.IsBudgeted as IsBudget

, coalesce(argm.ApprovalGroupMembers, '[]') as ApprovalGroupMembers
, argm.CountApprovalNoBudget as CountApprovalNoBudget

from BudgetTransaction as bt

inner join SubCategory as sc on sc.Id = bt.Transaction_SubCategoryId and sc.SubCategoryCode = 'Transaction.SC' -- SHOP Transaction.SC / NONSHOP Transaction.PRF

inner join PurchaseRequestItemCostCenter as pricc on pricc.Id = bt.RequestDetailCostCenterId
inner join CostCenter as cc on cc.Id = pricc.CostCenterId
inner join BusinessUnit as bu on bu.Id = cc.BusinessUnitId
inner join PurchaseRequestItemDetail as prid on prid.Id = pricc.PurchaseRequestItemDetailId
inner join M_Project as mp on mp.Id = prid.M_ProjectId
inner join AccountMaster as am on am.Id = prid.AccountMasterId
inner join Vendor as v on v.Id = prid.VendorId
inner join SubCategory as v_sc on v_sc.Id = v.SubCategoryId
inner join PurchaseRequest as pr on pr.Id = prid.PurchaseRequestId
inner join MasterTable as mt on mt.Category = 'PurchaseRequest.Status' and mt.ValueId = pr.Status -- PRF.Status / PurchaseOrder.Status / PurchaseRequest.Status

outer apply (
	select (
		select
		argm.UserName
		, argm.Comment
		, (case when sc.SubCategoryCode = 'ApprovalNoBudget' then 0 else 1 end) as IsApprovalBudget
		from ApprovalRequestGroupMember as argm
		inner join SubCategory as sc on argm.ApprovaGroup_SubCategoryId = sc.Id
		where argm.ApprovalRequestId = pr.ApprovalRequestId
		order by argm.CreatedTime asc
		for json path
	) as ApprovalGroupMembers
	, (
		select
		count(1) as CountApprovalNoBudget
		from ApprovalRequestGroupMember as argm
		inner join SubCategory as sc on argm.ApprovaGroup_SubCategoryId = sc.Id and sc.SubCategoryCode = 'ApprovalNoBudget'
		where argm.ApprovalRequestId = pr.ApprovalRequestId
	) as CountApprovalNoBudget
) as argm

-- dummy for filter
left join (select top 0 * from VoucherHeader) as vh on 1 = 1
left join (select top 0 * from VoucherDetail) as vd on 1 = 1

where left(bt.RefNumber,3) = 'PR_' -- PR
and bt.RequestDetailCostCenterId is not null -- DETAILCC
and bt.M_BudgetId is null -- NONBUDGET
{shopNonShopQuery.Replace("-- PurchaseRequest -- ", "")}

-- SHOP , POTOP , DETAILCC , BUDGET
union all
select
am.AccountCode as AccountMasterCode
, am.Description as AccountMasterName
, am.MtAccountType as MtAccountType
, bu.Code as BusinessUnitCode
, bu.Name as BusinessUnitName
, cc.Code as CostCenterCode
, cc.Name as CostCenterName
, bt.L_Currency_Code as LCurrencyCode
, bt.RateAmount as Rate
, bt.BasicAmount as NettAmount
, bt.Amount as NettAmountCurrency
, mt.Name as StatusRequestDesc
, vd.StatusTransfer as StatusTransfer
, (case vd.StatusTransfer when 1 then 'Success' when 0 then 'Failed' else '' end) as StatusTransferDesc
, ipo.SubmitDate as ReceivedByFinance
, convert(varchar(20), vh.TransferTime, 113) as PaidByFinance
, vh.TransferNumber as TransferNumber
, (pr.RequestCode + ' - ' + po.PONumber) as RequestNumber
, v_sc.SubCategoryName as VendorType
, v.Name as VendorName
, v.Code as VendorCode
, sc.SubCategoryName as RequestType
, ipo.InvoiceNumber as InvoiceNo
, prid.RequestorNotes as Description
, pod.IsBudgeted as IsBudget

, json_query((
	select top 1
	coalesce(po.ApproverUserName, '') as UserName
	, '' as Comment
	, 0 as IsApprovalBudget
	for json path
)) as ApprovalGroupMembers
, 0 as CountApprovalNoBudget

from BudgetTransaction as bt

inner join SubCategory as sc on sc.Id = bt.Transaction_SubCategoryId and sc.SubCategoryCode = 'Transaction.SC' -- SHOP Transaction.SC / NONSHOP Transaction.PRF

inner join M_Budget as mb on mb.Id = bt.M_BudgetId
inner join M_Project as mp on mp.ProjectCode = mb.ProjectCode
inner join AccountMaster as am on am.Id = mb.AccountMasterId
inner join CostCenter as cc on cc.Id = mb.CostCenterId
inner join BusinessUnit as bu on bu.Id = cc.BusinessUnitId

inner join PurchaseOrderDetailCostCenter as podcc on podcc.Id = bt.RequestDetailCostCenterId
inner join PurchaseOrderDetail as pod on pod.Id = podcc.PurchaseOrderDetailId
inner join PurchaseRequestItemDetail as prid on prid.Id = pod.PurchaseRequestItemDetailId
inner join PurchaseRequest as pr on pr.Id = prid.PurchaseRequestId
inner join PurchaseOrder as po on po.Id = pod.PurchaseOrderId
inner join MasterTable as mt on mt.Category = 'PurchaseOrder.Status' and mt.ValueId = po.Status -- PRF.Status / PurchaseOrder.Status / PurchaseRequest.Status
inner join Vendor as v on v.Id = po.VendorId
inner join SubCategory as v_sc on v_sc.Id = v.SubCategoryId

inner join PurchaseOrderTOP as pot on pot.Id = cast(right(bt.RefNumber, charindex('_', reverse(bt.RefNumber)) - 1) as bigint)
inner join SubCategory as cp_sc on cp_sc.SubCategoryCode = 'SC-2024-02-01261' -- SC SC-2024-02-01261 / NSC SC-2024-02-01262
left join InvoicePO as ipo on ipo.CategoryProcess_SubCategoryId = cp_sc.Id and ipo.PurchaseOrderTOPId = pot.Id
left join VoucherDetail as vd on vd.VoucherRefId like '% - %' and cast(ipo.Id as varchar(100)) = substring(vd.VoucherRefId,0,charindex(' - ',vd.VoucherRefId))
left join VoucherHeader as vh on vh.Id = vd.VoucherId

where left(bt.RefNumber,3) = 'PO_' -- POTOP
and bt.RequestDetailCostCenterId is not null -- DETAILCC
and bt.M_BudgetId is not null -- BUDGET
{shopNonShopQuery.Replace("-- PurchaseOrder -- ", "").Replace("-- M_Budget -- ", "")}

-- SHOP , POTOP , DETAILCC , NONBUDGET
union all
select
am.AccountCode as AccountMasterCode
, am.Description as AccountMasterName
, am.MtAccountType as MtAccountType
, bu.Code as BusinessUnitCode
, bu.Name as BusinessUnitName
, cc.Code as CostCenterCode
, cc.Name as CostCenterName
, bt.L_Currency_Code as LCurrencyCode
, bt.RateAmount as Rate
, bt.BasicAmount as NettAmount
, bt.Amount as NettAmountCurrency
, mt.Name as StatusRequestDesc
, vd.StatusTransfer as StatusTransfer
, (case vd.StatusTransfer when 1 then 'Success' when 0 then 'Failed' else '' end) as StatusTransferDesc
, ipo.SubmitDate as ReceivedByFinance
, convert(varchar(20), vh.TransferTime, 113) as PaidByFinance
, vh.TransferNumber as TransferNumber
, (pr.RequestCode + ' - ' + po.PONumber) as RequestNumber
, v_sc.SubCategoryName as VendorType
, v.Name as VendorName
, v.Code as VendorCode
, sc.SubCategoryName as RequestType
, ipo.InvoiceNumber as InvoiceNo
, prid.RequestorNotes as Description
, pod.IsBudgeted as IsBudget

, json_query((
	select top 1
	coalesce(po.ApproverUserName, '') as UserName
	, '' as Comment
	, 0 as IsApprovalBudget
	for json path
)) as ApprovalGroupMembers
, 0 as CountApprovalNoBudget

from BudgetTransaction as bt

inner join SubCategory as sc on sc.Id = bt.Transaction_SubCategoryId and sc.SubCategoryCode = 'Transaction.SC' -- SHOP Transaction.SC / NONSHOP Transaction.PRF

inner join PurchaseOrderDetailCostCenter as podcc on podcc.Id = bt.RequestDetailCostCenterId
inner join CostCenter as cc on cc.Id = podcc.CostCenterId
inner join BusinessUnit as bu on bu.Id = cc.BusinessUnitId
inner join PurchaseOrderDetail as pod on pod.Id = podcc.PurchaseOrderDetailId
inner join PurchaseRequestItemDetail as prid on prid.Id = pod.PurchaseRequestItemDetailId
inner join PurchaseRequest as pr on pr.Id = prid.PurchaseRequestId
inner join M_Project as mp on mp.Id = prid.M_ProjectId
inner join AccountMaster as am on am.Id = prid.AccountMasterId
inner join PurchaseOrder as po on po.Id = pod.PurchaseOrderId
inner join MasterTable as mt on mt.Category = 'PurchaseOrder.Status' and mt.ValueId = po.Status -- PRF.Status / PurchaseOrder.Status / PurchaseRequest.Status
inner join Vendor as v on v.Id = po.VendorId
inner join SubCategory as v_sc on v_sc.Id = v.SubCategoryId

inner join PurchaseOrderTOP as pot on pot.Id = cast(right(bt.RefNumber, charindex('_', reverse(bt.RefNumber)) - 1) as bigint)
inner join SubCategory as cp_sc on cp_sc.SubCategoryCode = 'SC-2024-02-01261' -- SC SC-2024-02-01261 / NSC SC-2024-02-01262
left join InvoicePO as ipo on ipo.CategoryProcess_SubCategoryId = cp_sc.Id and ipo.PurchaseOrderTOPId = pot.Id
left join VoucherDetail as vd on vd.VoucherRefId like '% - %' and cast(ipo.Id as varchar(100)) = substring(vd.VoucherRefId,0,charindex(' - ',vd.VoucherRefId))
left join VoucherHeader as vh on vh.Id = vd.VoucherId

where left(bt.RefNumber,3) = 'PO_' -- POTOP
and bt.RequestDetailCostCenterId is not null -- DETAILCC
and bt.M_BudgetId is null -- NONBUDGET
{shopNonShopQuery.Replace("-- PurchaseOrder -- ", "")}

-- NONSHOP , PRF , NONDETAILCC , BUDGET
union all
select
am.AccountCode as AccountMasterCode
, am.Description as AccountMasterName
, am.MtAccountType as MtAccountType
, bu.Code as BusinessUnitCode
, bu.Name as BusinessUnitName
, cc.Code as CostCenterCode
, cc.Name as CostCenterName
, bt.L_Currency_Code as LCurrencyCode
, bt.RateAmount as Rate
, bt.BasicAmount as NettAmount
, bt.Amount as NettAmountCurrency
, mt.Name as StatusRequestDesc
, null as StatusTransfer -- vd
, null as StatusTransferDesc -- vd
, null as ReceivedByFinance -- ipo
, null as PaidByFinance -- vh
, null as TransferNumber -- vh
, p.PRFNo as RequestNumber
, null as VendorType -- v
, null as VendorName -- v
, null as VendorCode -- v
, sc.SubCategoryName as RequestType
, null as InvoiceNo -- ipo

-- ?
, null as Description

, p.IsBudgetedSpend as IsBudget
, coalesce(argm.ApprovalGroupMembers, '[]') as ApprovalGroupMembers
, argm.CountApprovalNoBudget

from BudgetTransaction as bt

inner join SubCategory as sc on sc.Id = bt.Transaction_SubCategoryId and sc.SubCategoryCode = 'Transaction.PRF' -- SHOP Transaction.SC / NONSHOP Transaction.PRF

inner join PRF as p on p.PRFNo = bt.RefNumber
inner join MasterTable as mt on mt.Category = 'PRF.Status' and mt.ValueId = p.Status -- PRF.Status / PurchaseOrder.Status / PurchaseRequest.Status

inner join M_Budget as mb on mb.Id = bt.M_BudgetId
inner join M_Project as mp on mp.ProjectCode = mb.ProjectCode
inner join AccountMaster as am on am.Id = mb.AccountMasterId
inner join CostCenter as cc on cc.Id = mb.CostCenterId
inner join BusinessUnit as bu on bu.Id = cc.BusinessUnitId

left join PRFSummary as ps on ps.PRFId = p.Id

outer apply (
	select (
		select
		argm.UserName
		, argm.Comment
		, (case when sc.SubCategoryCode = 'ApprovalNoBudget' then 0 else 1 end) as IsApprovalBudget
		from ApprovalRequestGroupMember as argm
		inner join SubCategory as sc on argm.ApprovaGroup_SubCategoryId = sc.Id
		where argm.ApprovalRequestId in (p.ApprovalRequestId, ps.ApprovalRequestId)
		order by argm.CreatedTime asc
		for json path
	) as ApprovalGroupMembers
	, (
		select
		count(1) as CountApprovalNoBudget
		from ApprovalRequestGroupMember as argm
		inner join SubCategory as sc on argm.ApprovaGroup_SubCategoryId = sc.Id and sc.SubCategoryCode = 'ApprovalNoBudget'
		where argm.ApprovalRequestId in (p.ApprovalRequestId, ps.ApprovalRequestId)
	) as CountApprovalNoBudget
) as argm

-- dummy for filter
left join (select top 0 * from VoucherHeader) as vh on 1 = 1
left join (select top 0 * from VoucherDetail) as vd on 1 = 1

where left(bt.RefNumber,3) = 'PR_' -- PR
and bt.RequestDetailCostCenterId is null -- NONDETAILCC
and bt.M_BudgetId is not null -- BUDGET
{shopNonShopQuery.Replace("-- PRF -- ", "").Replace("-- M_Budget -- ", "")}

-- NONSHOP , PRF , NONDETAILCC , NONBUDGET
union all
select
am.AccountCode as AccountMasterCode
, am.Description as AccountMasterName
, am.MtAccountType as MtAccountType
, bu.Code as BusinessUnitCode
, bu.Name as BusinessUnitName
, cc.Code as CostCenterCode
, cc.Name as CostCenterName
, bt.L_Currency_Code as LCurrencyCode
, bt.RateAmount as Rate
, bt.BasicAmount as NettAmount
, bt.Amount as NettAmountCurrency
, mt.Name as StatusRequestDesc
, null as StatusTransfer -- vd
, null as StatusTransferDesc -- vd
, null as ReceivedByFinance -- ipo
, null as PaidByFinance -- vh
, null as TransferNumber -- vh
, p.PRFNo as RequestNumber
, null as VendorType -- v
, null as VendorName -- v
, null as VendorCode -- v
, sc.SubCategoryName as RequestType
, null as InvoiceNo -- ipo

-- ?
, null as Description

, p.IsBudgetedSpend as IsBudget
, coalesce(argm.ApprovalGroupMembers, '[]') as ApprovalGroupMembers
, argm.CountApprovalNoBudget

from BudgetTransaction as bt

inner join SubCategory as sc on sc.Id = bt.Transaction_SubCategoryId and sc.SubCategoryCode = 'Transaction.PRF' -- SHOP Transaction.SC / NONSHOP Transaction.PRF

inner join PRF as p on p.PRFNo = bt.RefNumber
inner join M_Project as mp on mp.ProjectCode = p.ProjectCode
inner join MasterTable as mt on mt.Category = 'PRF.Status' and mt.ValueId = p.Status -- PRF.Status / PurchaseOrder.Status / PurchaseRequest.Status
inner join AccountMaster as am on am.AccountCode = p.BudgetCode
inner join CostCenter as cc on cc.Id = p.CostCenterId
inner join BusinessUnit as bu on bu.Id = cc.BusinessUnitId

left join PRFSummary as ps on ps.PRFId = p.Id

outer apply (
	select (
		select
		argm.UserName
		, argm.Comment
		, (case when sc.SubCategoryCode = 'ApprovalNoBudget' then 0 else 1 end) as IsApprovalBudget
		from ApprovalRequestGroupMember as argm
		inner join SubCategory as sc on argm.ApprovaGroup_SubCategoryId = sc.Id
		where argm.ApprovalRequestId in (p.ApprovalRequestId, ps.ApprovalRequestId)
		order by argm.CreatedTime asc
		for json path
	) as ApprovalGroupMembers
	, (
		select
		count(1) as CountApprovalNoBudget
		from ApprovalRequestGroupMember as argm
		inner join SubCategory as sc on argm.ApprovaGroup_SubCategoryId = sc.Id and sc.SubCategoryCode = 'ApprovalNoBudget'
		where argm.ApprovalRequestId in (p.ApprovalRequestId, ps.ApprovalRequestId)
	) as CountApprovalNoBudget
) as argm

-- dummy for filter
left join (select top 0 * from VoucherHeader) as vh on 1 = 1
left join (select top 0 * from VoucherDetail) as vd on 1 = 1

where left(bt.RefNumber,3) = 'PR_' -- PR
and bt.RequestDetailCostCenterId is null -- NONDETAILCC
and bt.M_BudgetId is null -- NONBUDGET
{shopNonShopQuery.Replace("-- PRF -- ", "")}

-- NONSHOP , PONSTOP , DETAILCC , BUDGET
union all
select
am.AccountCode as AccountMasterCode
, am.Description as AccountMasterName
, am.MtAccountType as MtAccountType
, bu.Code as BusinessUnitCode
, bu.Name as BusinessUnitName
, cc.Code as CostCenterCode
, cc.Name as CostCenterName
, bt.L_Currency_Code as LCurrencyCode
, bt.RateAmount as Rate
, bt.BasicAmount as NettAmount
, bt.Amount as NettAmountCurrency
, mt.Name as StatusRequestDesc
, vd.StatusTransfer as StatusTransfer
, (case vd.StatusTransfer when 1 then 'Success' when 0 then 'Failed' else '' end) as StatusTransferDesc
, ipo.SubmitDate as ReceivedByFinance
, convert(varchar(20), vh.TransferTime, 113) as PaidByFinance
, vh.TransferNumber as TransferNumber
, (p.PRFNo + ' - ' + pons.PONumber) as RequestNumber
, v_sc.SubCategoryName as VendorType
, v.Name as VendorName
, v.Code as VendorCode
, sc.SubCategoryName as RequestType
, ipo.InvoiceNumber as InvoiceNo
, ponsd.ItemDescription as Description
, ponsd.IsBudgeted as IsBudget

, json_query((
	select top 1
	coalesce(pons.ApproverUserName, '') as UserName
	, coalesce(pons.RemarksApproval, '') as Comment
	, 0 as IsApprovalBudget
	for json path
)) as ApprovalGroupMembers
, 0 as CountApprovalNoBudget

from BudgetTransaction as bt

inner join SubCategory as sc on sc.Id = bt.Transaction_SubCategoryId and sc.SubCategoryCode = 'Transaction.PRF' -- SHOP Transaction.SC / NONSHOP Transaction.PRF

inner join M_Budget as mb on mb.Id = bt.M_BudgetId
inner join M_Project as mp on mp.ProjectCode = mb.ProjectCode
inner join AccountMaster as am on am.Id = mb.AccountMasterId
inner join CostCenter as cc on cc.Id = mb.CostCenterId
inner join BusinessUnit as bu on bu.Id = cc.BusinessUnitId

inner join PONonShoppingDetailCostCenter as ponsdcc on ponsdcc.Id = bt.RequestDetailCostCenterId
inner join PONonShoppingDetail as ponsd on ponsd.Id = ponsdcc.PONonShoppingDetailId
inner join PONonShopping as pons on pons.Id = ponsd.PONonShoppingId
inner join MasterTable as mt on mt.Category = 'PurchaseOrder.Status' and mt.ValueId = pons.Status -- PRF.Status / PurchaseOrder.Status / PurchaseRequest.Status
inner join Vendor as v on v.Id = pons.VendorId
inner join SubCategory as v_sc on v_sc.Id = v.SubCategoryId
inner join PRFSummary as ps on ps.Id = pons.PRFSummaryId
inner join PRF as p on p.Id = ps.PRFId

inner join PONonShoppingTOP as ponst on ponst.Id = cast(right(bt.RefNumber, charindex('_', reverse(bt.RefNumber)) - 1) as bigint)
inner join SubCategory as cp_sc on cp_sc.SubCategoryCode = 'SC-2024-02-01262' -- SC SC-2024-02-01261 / NSC SC-2024-02-01262
left join InvoicePO as ipo on ipo.CategoryProcess_SubCategoryId = cp_sc.Id and ipo.PurchaseOrderTOPId = ponst.Id
left join VoucherDetail as vd on vd.VoucherRefId like '% - %' and cast(ipo.Id as varchar(100)) = substring(vd.VoucherRefId,0,charindex(' - ',vd.VoucherRefId))
left join VoucherHeader as vh on vh.Id = vd.VoucherId

where left(bt.RefNumber,3) = 'PO_' -- POTOP
and bt.RequestDetailCostCenterId is not null -- DETAILCC
and bt.M_BudgetId is not null -- BUDGET
{shopNonShopQuery.Replace("-- PONonShopping -- ", "").Replace("-- M_Budget -- ", "")}

-- NONSHOP , PONSTOP , DETAILCC , NONBUDGET
union all
select
am.AccountCode as AccountMasterCode
, am.Description as AccountMasterName
, am.MtAccountType as MtAccountType
, bu.Code as BusinessUnitCode
, bu.Name as BusinessUnitName
, cc.Code as CostCenterCode
, cc.Name as CostCenterName
, bt.L_Currency_Code as LCurrencyCode
, bt.RateAmount as Rate
, bt.BasicAmount as NettAmount
, bt.Amount as NettAmountCurrency
, mt.Name as StatusRequestDesc
, vd.StatusTransfer as StatusTransfer
, (case vd.StatusTransfer when 1 then 'Success' when 0 then 'Failed' else '' end) as StatusTransferDesc
, ipo.SubmitDate as ReceivedByFinance
, convert(varchar(20), vh.TransferTime, 113) as PaidByFinance
, vh.TransferNumber as TransferNumber
, (p.PRFNo + ' - ' + pons.PONumber) as RequestNumber
, v_sc.SubCategoryName as VendorType
, v.Name as VendorName
, v.Code as VendorCode
, sc.SubCategoryName as RequestType
, ipo.InvoiceNumber as InvoiceNo
, ponsd.ItemDescription as Description
, ponsd.IsBudgeted as IsBudget

, json_query((
	select top 1
	coalesce(pons.ApproverUserName, '') as UserName
	, coalesce(pons.RemarksApproval, '') as Comment
	, 0 as IsApprovalBudget
	for json path
)) as ApprovalGroupMembers
, 0 as CountApprovalNoBudget

from BudgetTransaction as bt

inner join SubCategory as sc on sc.Id = bt.Transaction_SubCategoryId and sc.SubCategoryCode = 'Transaction.PRF' -- SHOP Transaction.SC / NONSHOP Transaction.PRF

inner join PONonShoppingDetailCostCenter as ponsdcc on ponsdcc.Id = bt.RequestDetailCostCenterId
inner join PONonShoppingDetail as ponsd on ponsd.Id = ponsdcc.PONonShoppingDetailId
inner join PONonShopping as pons on pons.Id = ponsd.PONonShoppingId
inner join MasterTable as mt on mt.Category = 'PurchaseOrder.Status' and mt.ValueId = pons.Status -- PRF.Status / PurchaseOrder.Status / PurchaseRequest.Status
inner join Vendor as v on v.Id = pons.VendorId
inner join SubCategory as v_sc on v_sc.Id = v.SubCategoryId
inner join PRFSummary as ps on ps.Id = pons.PRFSummaryId
inner join PRF as p on p.Id = ps.PRFId
inner join M_Project as mp on mp.ProjectCode = p.ProjectCode
inner join AccountMaster as am on am.AccountCode = p.BudgetCode
inner join CostCenter as cc on cc.Id = p.CostCenterId
inner join BusinessUnit as bu on bu.Id = cc.BusinessUnitId

inner join PONonShoppingTOP as ponst on ponst.Id = cast(right(bt.RefNumber, charindex('_', reverse(bt.RefNumber)) - 1) as bigint)
inner join SubCategory as cp_sc on cp_sc.SubCategoryCode = 'SC-2024-02-01262' -- SC SC-2024-02-01261 / NSC SC-2024-02-01262
left join InvoicePO as ipo on ipo.CategoryProcess_SubCategoryId = cp_sc.Id and ipo.PurchaseOrderTOPId = ponst.Id
left join VoucherDetail as vd on vd.VoucherRefId like '% - %' and cast(ipo.Id as varchar(100)) = substring(vd.VoucherRefId,0,charindex(' - ',vd.VoucherRefId))
left join VoucherHeader as vh on vh.Id = vd.VoucherId

where left(bt.RefNumber,3) = 'PO_' -- POTOP
and bt.RequestDetailCostCenterId is not null -- DETAILCC
and bt.M_BudgetId is null -- NONBUDGET
{shopNonShopQuery.Replace("-- PONonShopping M_BudgetId is null -- ", "")}

";
            #endregion

            var qry = $@"-- NON BENEFIT
							SELECT *, COUNT(*) OVER() AS CountData FROM (
								-- budget
								SELECT DISTINCT 
								am.AccountCode [AccountMasterCode],
								am.ShortDescription [AccountMasterName],
								am.MtAccountType, 
								bu.Code [BusinessUnitCode], 
								bu.Name [BusinessUnitName], 
								cc.Code [CostCenterCode], 
								cc.Name [CostCenterName], 
								rd.L_Currency [LCurrencyCode],
								REPLACE(FORMAT(rd.RateAmount, 'C'),'$','') [Rate],
								SUM(rcc.Amount) NettAmount,
								REPLACE(FORMAT(SUM(rcc.Amount), 'C'),'$','') [NettAmountCurrency],
								MasterStatus.Description StatusRequestDesc,
								vd.StatusTransfer,
								(CASE WHEN vd.StatusTransfer = 1 THEN 'Success' 
								WHEN vd.StatusTransfer = 0 THEN 'Failed' ELSE '' END) StatusTransferDesc,
								CONVERT(VARCHAR(20),lastApprove.ApprovalDate,113) ReceivedByFinance,
								CONVERT(VARCHAR(20),vh.TransferTime,113) PaidByFinance,
								vh.TransferNumber,
								r.RequestNumber,
								scv.SubCategoryName VendorType, 
								(SELECT DISTINCT TOP 1 v.[Name]) VendorName,
								(CASE WHEN scv.SubCategoryName = 'Staff' THEN (SELECT DISTINCT TOP 1 v.EmployeeCode)
								 ELSE (SELECT DISTINCT TOP 1 v.Code) END) VendorCode,
								scr.SubCategoryName RequestType,
								rd.InvoiceNo,
								rd.Description,
								rcc.IsBudget IsBudget,
								approvals.member ApprovalGroupMembers,
								CountApprovalNoBudget.CountData CountApprovalNoBudget
								FROM M_Budget b
								JOIN M_Project p ON b.ProjectCode = p.ProjectCode
								JOIN AccountMaster am ON b.AccountMasterId = am.Id
								JOIN CostCenter cc ON b.CostCenterId = cc.Id
								JOIN BusinessUnit bu ON b.BusinessUnitId = bu.Id
								JOIN ReimbursementDetail rd ON b.AccountMasterId = rd.AccountMasterId 
									AND rd.TransactionDate BETWEEN b.PeriodFrom AND b.PeriodTo
								JOIN ReimbursementDetailCostCenter rcc ON rd.Id = rcc.ReimbursementDetailId 
									AND b.BusinessUnitId = rcc.BusinessUnitId
									AND b.CostCenterId = rcc.CostCenterId
								JOIN Reimbursement r ON rd.ReimbursementId = r.Id
								JOIN SubCategory scv ON rd.ExpenseGeneral_SubCategoryId = scv.Id
								JOIN Vendor v ON rd.VendorId = v.Id
								JOIN SubCategory scr ON r.ReimbursementType_SubCategoryId = scr.Id
								JOIN BudgetTransaction bt ON b.Id = bt.M_BudgetId AND r.RequestNumber = bt.RefNumber
								LEFT JOIN VoucherDetail vd ON r.RequestNumber = vd.VoucherRefId
								LEFT JOIN VoucherHeader vh ON vd.VoucherId = vh.Id
								OUTER APPLY (SELECT TOP 1 argm1.ApprovalDate 
											 FROM ApprovalRequestGroupMember argm1 
											 WHERE argm1.ApprovalRequestId = r.ApprovalRequestId
											 ORDER BY ApprovalDate DESC
											) lastApprove
								OUTER APPLY (SELECT TOP 1 Description 
											 FROM MasterTable mt 
											 WHERE mt.Category = 'ApprovalRequest.Status'
											 AND mt.ValueId = r.Status
											) MasterStatus
								OUTER APPLY (SELECT (SELECT argm.UserName, argm.Comment, (CASE WHEN scm.SubCategoryCode = 'ApprovalNoBudget' THEN 0 ELSE 1 END) IsApprovalBudget
											 FROM ApprovalRequest ar 
											 JOIN ApprovalRequestGroupMember argm ON ar.Id = argm.ApprovalRequestId
											 JOIN SubCategory scm ON argm.ApprovaGroup_SubCategoryId = scm.Id
											 AND ar.RequestNo = r.RequestNumber ORDER BY argm.CreatedTime FOR JSON PATH) member
											) approvals
								OUTER APPLY (SELECT COUNT(*) AS CountData
											 FROM ApprovalRequest ar 
											 JOIN ApprovalRequestGroupMember argm ON ar.Id = argm.ApprovalRequestId
											 JOIN SubCategory scm ON argm.ApprovaGroup_SubCategoryId = scm.Id
											 WHERE ar.RequestNo = r.RequestNumber AND scm.SubCategoryCode = 'ApprovalNoBudget'
											) CountApprovalNoBudget
								WHERE {subQuery}
								GROUP BY bt.Id, am.AccountCode, am.ShortDescription, am.MtAccountType, bu.Name, bu.Code, cc.Name, cc.Code,
								rd.L_Currency, rd.RateAmount, MasterStatus.Description, vd.StatusTransfer, lastApprove.ApprovalDate, vh.TransferTime,
								vh.TransferNumber, r.RequestNumber, scv.SubCategoryName, v.Name, scr.SubCategoryName, 
								v.EmployeeCode, v.Code, rd.InvoiceNo, rd.Description, rcc.IsBudget, approvals.member, CountApprovalNoBudget.CountData
								-- no budget
								UNION ALL
								SELECT DISTINCT 
								ISNULL(am.AccountCode,
									 (SELECT TOP 1 am2.AccountCode
									  FROM AccountMaster am2 
										 JOIN ReimbursementDetail rd2 ON am2.Id = rd2.AccountMasterId
										 JOIN Reimbursement r2
											 ON rd2.ReimbursementId = r2.Id
												AND r2.RequestNumber = r.RequestNumber)
								) [AccountMasterCode],
								ISNULL(am.ShortDescription,
									 (SELECT TOP 1 am2.ShortDescription
									  FROM AccountMaster am2 
										 JOIN ReimbursementDetail rd2 ON am2.Id = rd2.AccountMasterId
										 JOIN Reimbursement r2
											 ON rd2.ReimbursementId = r2.Id
												AND r2.RequestNumber = r.RequestNumber)
								) [AccountMasterName],
								ISNULL(am.MtAccountType,
									 (SELECT TOP 1 am2.MtAccountType
									  FROM AccountMaster am2 
										 JOIN ReimbursementDetail rd2 ON am2.Id = rd2.AccountMasterId
										 JOIN Reimbursement r2
											 ON rd2.ReimbursementId = r2.Id
												AND r2.RequestNumber = r.RequestNumber)
								) [MtAccountType],
								ISNULL(bu.Code,
									 (SELECT TOP 1 bu2.Code
									  FROM BusinessUnit bu2 
										 JOIN ReimbursementDetailCostCenter rcc2 ON bu2.Id = rcc2.BusinessUnitId
										 JOIN ReimbursementDetail rd2 ON rcc2.ReimbursementDetailId = rd2.Id
										 JOIN Reimbursement r2
											 ON rd2.ReimbursementId = r2.Id
												AND r2.RequestNumber = r.RequestNumber)
								) [BusinessUnitCode],
								ISNULL(bu.Name,
									 (SELECT TOP 1 bu2.Name
									  FROM BusinessUnit bu2 
										 JOIN ReimbursementDetailCostCenter rcc2 ON bu2.Id = rcc2.BusinessUnitId
										 JOIN ReimbursementDetail rd2 ON rcc2.ReimbursementDetailId = rd2.Id
										 JOIN Reimbursement r2
											 ON rd2.ReimbursementId = r2.Id
												AND r2.RequestNumber = r.RequestNumber)
								) [BusinessUnitName],
								ISNULL(cc.Code,
									 (SELECT TOP 1 cc2.Code
									  FROM CostCenter cc2 
										 JOIN ReimbursementDetailCostCenter rcc2 ON cc2.Id = rcc2.CostCenterId
										 JOIN ReimbursementDetail rd2 ON rcc2.ReimbursementDetailId = rd2.Id
										 JOIN Reimbursement r2
											 ON rd2.ReimbursementId = r2.Id
												AND r2.RequestNumber = r.RequestNumber)
								) [CostCenterCode],
								ISNULL(cc.Name,
									 (SELECT TOP 1 cc2.Name
									  FROM CostCenter cc2 
										 JOIN ReimbursementDetailCostCenter rcc2 ON cc2.Id = rcc2.CostCenterId
										 JOIN ReimbursementDetail rd2 ON rcc2.ReimbursementDetailId = rd2.Id
										 JOIN Reimbursement r2
											 ON rd2.ReimbursementId = r2.Id
												AND r2.RequestNumber = r.RequestNumber)
								) [CostCenterName],
								rd.L_Currency [LCurrencyCode],
								REPLACE(FORMAT(rd.RateAmount, 'C'),'$','') [Rate],
								(CASE
									 WHEN SUM(rcc.Amount) IS NOT NULL THEN
										 SUM(rcc.Amount)
									 ELSE
								 (
									 SELECT SUM(rcc2.Amount)
									 FROM ReimbursementDetailCostCenter rcc2
										 JOIN ReimbursementDetail rd2
											 On rcc2.ReimbursementDetailId = rd2.Id
										 JOIN Reimbursement r2
											 ON rd2.ReimbursementId = r2.Id
												AND r2.RequestNumber = r.RequestNumber
								 )
								 END
								) NettAmount,
								(CASE
									 WHEN SUM(rcc.Amount) IS NOT NULL THEN
										 REPLACE(FORMAT(SUM(rcc.Amount), 'C'), '$', '')
									 ELSE
								 (
									 SELECT REPLACE(FORMAT(SUM(rcc2.Amount), 'C'), '$', '')
									 FROM ReimbursementDetailCostCenter rcc2
										 JOIN ReimbursementDetail rd2
											 On rcc2.ReimbursementDetailId = rd2.Id
										 JOIN Reimbursement r2
											 ON rd2.ReimbursementId = r2.Id
												AND r2.RequestNumber = r.RequestNumber
								 )
								 END
								) [NettAmountCurrency],
								MasterStatus.Description StatusRequestDesc,
								vd.StatusTransfer,
								(CASE WHEN vd.StatusTransfer = 1 THEN 'Success' 
								WHEN vd.StatusTransfer = 0 THEN 'Failed' ELSE '' END) StatusTransferDesc,
								CONVERT(VARCHAR(20),lastApprove.ApprovalDate,113) ReceivedByFinance,
								CONVERT(VARCHAR(20),vh.TransferTime,113) PaidByFinance,
								vh.TransferNumber,
								r.RequestNumber,
								scv.SubCategoryName VendorType, 
								(SELECT DISTINCT TOP 1 v.[Name]) VendorName,
								(CASE WHEN scv.SubCategoryName = 'Staff' THEN (SELECT DISTINCT TOP 1 v.EmployeeCode)
								 ELSE (SELECT DISTINCT TOP 1 v.Code) END) VendorCode,
								scr.SubCategoryName RequestType,
								rd.InvoiceNo,
								rd.Description,
								ISNULL(rcc.IsBudget,0) IsBudget,
								approvals.member ApprovalGroupMembers,
								CountApprovalNoBudget.CountData CountApprovalNoBudget
								FROM M_Budget b
								JOIN M_Project p ON b.ProjectCode = p.ProjectCode
								RIGHT JOIN ReimbursementDetail rd ON b.AccountMasterId = rd.AccountMasterId 
									AND rd.TransactionDate BETWEEN b.PeriodFrom AND b.PeriodTo
								LEFT JOIN ReimbursementDetailCostCenter rcc ON rd.Id = rcc.ReimbursementDetailId 
									AND b.BusinessUnitId = rcc.BusinessUnitId
									AND b.CostCenterId = rcc.CostCenterId
								JOIN Reimbursement r ON rd.ReimbursementId = r.Id
								LEFT JOIN AccountMaster am ON rd.AccountMasterId = am.Id
								LEFT JOIN CostCenter cc ON rcc.CostCenterId = cc.Id
								LEFT JOIN BusinessUnit bu ON rcc.BusinessUnitId = bu.Id
								JOIN SubCategory scv ON rd.ExpenseGeneral_SubCategoryId = scv.Id
								JOIN Vendor v ON rd.VendorId = v.Id
								JOIN SubCategory scr ON r.ReimbursementType_SubCategoryId = scr.Id
								LEFT JOIN VoucherDetail vd ON r.RequestNumber = vd.VoucherRefId
								LEFT JOIN VoucherHeader vh ON vd.VoucherId = vh.Id
								LEFT JOIN BudgetTransaction bt ON r.RequestNumber = bt.RefNumber
								OUTER APPLY (SELECT TOP 1 argm1.ApprovalDate 
											 FROM ApprovalRequestGroupMember argm1 
											 WHERE argm1.ApprovalRequestId = r.ApprovalRequestId
											 ORDER BY ApprovalDate DESC
											) lastApprove
								OUTER APPLY (SELECT TOP 1 Description 
											 FROM MasterTable mt 
											 WHERE mt.Category = 'ApprovalRequest.Status'
											 AND mt.ValueId = r.Status
											) MasterStatus
								OUTER APPLY (SELECT (SELECT argm.UserName, argm.Comment, (CASE WHEN scm.SubCategoryCode = 'ApprovalNoBudget' THEN 0 ELSE 1 END) IsApprovalBudget
											 FROM ApprovalRequest ar 
											 JOIN ApprovalRequestGroupMember argm ON ar.Id = argm.ApprovalRequestId
											 JOIN SubCategory scm ON argm.ApprovaGroup_SubCategoryId = scm.Id
											 AND ar.RequestNo = r.RequestNumber ORDER BY argm.CreatedTime FOR JSON PATH) member
											) approvals
								OUTER APPLY (SELECT COUNT(*) AS CountData
											 FROM ApprovalRequest ar 
											 JOIN ApprovalRequestGroupMember argm ON ar.Id = argm.ApprovalRequestId
											 JOIN SubCategory scm ON argm.ApprovaGroup_SubCategoryId = scm.Id
											 WHERE ar.RequestNo = r.RequestNumber AND scm.SubCategoryCode = 'ApprovalNoBudget'
											) CountApprovalNoBudget
								WHERE M_BudgetId IS NULL AND r.Status != 0 AND {subQuery}
								GROUP BY bt.Id, am.AccountCode, am.ShortDescription, am.MtAccountType, bu.Name, bu.Code, cc.Name, cc.Code,
								rd.L_Currency, rd.RateAmount, MasterStatus.Description, vd.StatusTransfer, lastApprove.ApprovalDate, vh.TransferTime,
								vh.TransferNumber, r.RequestNumber, scv.SubCategoryName, v.Name, scr.SubCategoryName, 
								v.EmployeeCode, v.Code, rd.InvoiceNo, rd.Description, rcc.IsBudget, approvals.member, CountApprovalNoBudget.CountData

{shopNonShopQuery}

								) combined_results 
							GROUP BY RequestNumber, AccountMasterCode, AccountMasterName, MtAccountType, BusinessUnitName, BusinessUnitCode, CostCenterName, CostCenterCode,
							LCurrencyCode, Rate, NettAmount, NettAmountCurrency, StatusRequestDesc, StatusTransfer, StatusTransferDesc, ReceivedByFinance, PaidByFinance, TransferNumber, 
							VendorType, VendorName, VendorCode, RequestType, InvoiceNo, Description, IsBudget, ApprovalGroupMembers, CountApprovalNoBudget";
            return qry;
        }

        /// <summary>
        /// Get Report Budget Transactions
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReportBudgetResponse>> GetReportBudgetTransactions(ParamGetRequestList param)
        {
            try
            {
                string qry = await GetQueryReportBudgetTransactions(param);
                if (param.IsExport)
                {
                    qry = $"{qry} ORDER BY RequestNumber DESC";
                }
                else
                {
                    qry = $"{qry} ORDER BY {param.SortColumn ?? "RequestNumber"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
                }
                var budgetList = await Task.FromResult(_dapper.GetAll<ReportBudgetResponse>(qry, new Dapper.DynamicParameters(new
                {
                    param.Page,
                    param.PageSize,
                    param.ProjectId,
                    param.AccountMasterId,
                    param.BusinessUnitId,
                    param.CostCenterId,
                    param.Status,
                    param.StatusTransfer,
                    param.RequestDateFrom,
                    param.RequestDateTo,
                    Search = string.IsNullOrEmpty(param.Search?.Value) ? string.Empty : "%" + param.Search?.Value + "%",
                }), commandType: CommandType.Text));

                var approvals = new List<ApprovalGroupMemberBudgetResponse>();
                if (budgetList.Count != 0)
                {
                    foreach (var item in budgetList.ToList())
                    {
                        // set approval group members
                        if (!string.IsNullOrEmpty(item.ApprovalGroupMembers))
                            approvals = JsonConvert.DeserializeObject<List<ApprovalGroupMemberBudgetResponse>>(item.ApprovalGroupMembers);
                        item.ApprovalGroupMembers = "see on approvalGroupMemberBudgetResponses";
                        item.ApprovalGroupMemberBudgetResponses = approvals;
                    }
                }
                return budgetList;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(log.objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Get Report Voucher List
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<ReportVoucherListResponse>> GetReportVoucherList(ReportVoucherListRequest param)
        {
            try
            {
                string qry = ReportPajak.GetQueryReportVoucherList(param);

                if (param.IsExport)
                {
                    qry = $"{qry} ORDER BY VoucherNumber DESC";
                }
                else
                {
                    qry = $"{qry} ORDER BY {param.SortColumn ?? "VoucherNumber"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
                }
                var voucherList = await Task.FromResult(_dapper.GetAll<ReportVoucherListResponse>(qry, new Dapper.DynamicParameters(new
                {
                    param.VoucherNumber,
                    param.Page,
                    param.PageSize,
                    param.StartDate,
                    param.EndDate,
                    Search = string.IsNullOrEmpty(param.Search?.Value) ? string.Empty : "%" + param.Search?.Value + "%",
                }), commandType: CommandType.Text));

                return voucherList;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(log.objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Get Report Pajak List
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<ReportPajakListResponse>> GetReportPajakList(ReportPajakListRequest param)
        {
            try
            {
                string qry = ReportPajak.GetQueryReportPajakList(param);

                if (param.IsExport)
                {
                    if (param.ReportType.Equals(AppSystem.ReportPajakGeneral, StringComparison.CurrentCultureIgnoreCase))
                        qry = ReportPajak.GetQueryReportPajakList(param);
                    else if (param.ReportType.Equals(AppSystem.ReportPajakPph21, StringComparison.CurrentCultureIgnoreCase))
                        qry = ReportPajak.GetQueryReportPph21List(param);
                    else if (param.ReportType.Equals(AppSystem.ReportPajakPph23, StringComparison.CurrentCultureIgnoreCase))
                        qry = ReportPajak.GetQueryReportPph23List(param);
                    else if (param.ReportType.Equals(AppSystem.ReportPajakPph26, StringComparison.CurrentCultureIgnoreCase))
                        qry = ReportPajak.GetQueryReportPph26List(param);

                    qry = $"{qry} ORDER BY CreatedTime DESC";
                }
                else
                {
                    qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
                }
                var pajakList = await Task.FromResult(_dapper.GetAll<ReportPajakListResponse>(qry, new Dapper.DynamicParameters(new
                {
                    param.RequestNumber,
                    param.TransferNumber,
                    param.OtherCostSubCategoryId,
                    param.VendorTypeSubCategoryId,
                    param.VendorId,
                    param.MakerFinance,
                    param.Page,
                    param.PageSize,
                    param.TransferTimeStart,
                    param.TransferTimeEnd,
                    Search = string.IsNullOrEmpty(param.Search?.Value) ? string.Empty : "%" + param.Search?.Value + "%",
                }), commandType: CommandType.Text));

                return pajakList;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(log.objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Get Count Not Updated Voucher
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetCountNotUpdatedVoucher()
        {
            try
            {
                var qry = $@"SELECT COUNT(DISTINCT vh.Id)
							 FROM VoucherHeader vh
							 JOIN VoucherDetail vd on vd.VoucherId = vh.Id
							 JOIN MasterTable mt on mt.ValueId = vh.Status
							 WHERE mt.Category = 'Voucher.Status' 
							 AND (ApproveDate IS NOT NULL AND TransferNumber IS NULL)  
							 ";

                var count = await Task.FromResult(_dapper.Get<int>(qry, new Dapper.DynamicParameters(new
                {
                }), commandType: CommandType.Text));

                return count;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(log.objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Get Count Per Year Voucher
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetCountPerYearVoucher()
        {
            try
            {
                var qry = $@"SELECT COUNT(vh.Id)
							 FROM VoucherHeader vh
							 JOIN VoucherDetail vd on vd.VoucherId = vh.Id
							 JOIN MasterTable mt on mt.ValueId = vh.Status
							 WHERE mt.Category = 'Voucher.Status' 
							 AND vd.StatusTransfer is not null
							 AND YEAR(vh.CreatedTime) = YEAR(GETDATE()) 
							 ";

                var count = await Task.FromResult(_dapper.Get<int>(qry, new Dapper.DynamicParameters(new
                {
                }), commandType: CommandType.Text));

                return count;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(log.objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Get Report PO Shopping Cart List
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReportPOShoppingCartListResponse>> GetReportPOShoppingCartList(ReportPORequest request)
        {
            return (await GetReportPOShoppingCartExport(request)).Select(x => (ReportPOShoppingCartListResponse)x).ToList();
        }

        /// <summary>
        /// Get Report PO Shopping Cart Export
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReportPOShoppingCartExportResponse>> GetReportPOShoppingCartExport(ReportPORequest request)
        {
            try
            {
                var query = $@"
select 
count(*) over() as CountData
--
, a.pr_RequestCode as PRNo
, a.pr_Status as PRStatus
, a.pr_Status_mt_Name as PRStatusName
, convert(varchar,cast(a.pr_RequestDates as date),20) as PRDate
, a.pr_RequestorUserName as Requester
, a.pr_RequestorAccountId_f_ua_CostCenterId as RequestorAccountCostCenterId
, a.pr_RequestorAccountId_f_ua_CostCenterName as Department
--
, a.pr_prid_L_Currency_Code as Currency
, a.prid_i_Name as ItemName
, a.i_am_Id as ItemAccountMasterId
, a.i_am_AccountCode_Description as AccountCode
, a.prid_v_Id as ItemVendorId
, a.prid_v_Name as VendorSelection
, a.prid_pricc_cc_Id as ItemCostCenterId
, a.pricc_cc_Name as CostCenter
, a.prid_argm_UserName as PRApprovalName
, a.ua_p_Name as PRApprovalTitle
, convert(varchar,cast(a.prid_argm_ApprovalDate as date),20) as PRApprovalDate
--
, a.prtpo_po_PONumber as PONo
, a.prtpo_po_Status as POStatus
, a.po_Status_mt_Name as POStatusName
, convert(varchar,cast(a.prtpo_po_PoDate as date),20) as PODate
, a.prtpo_po_TotalAmount as POAmount
, convert(varchar,cast(a.prtpo_po_ApproverDate as date),20) as POApproverDate
--
, datediff(day,a.prid_argm_ApprovalDate,a.prtpo_po_ApproverDate) as TATWD
, 2 as SLAWD
, case when datediff(day,a.prid_argm_ApprovalDate,a.prtpo_po_ApproverDate) is null then '' when datediff(day,a.prid_argm_ApprovalDate,a.prtpo_po_ApproverDate) <= 2 then 'MEET' else 'NOT MEET' end as SLAStatus
--
, a.po_dn_DeliveryNumber as DNNo
, a.pod_dnd_Status as DNStatus
, a.dnd_Status_mt_Name as DNStatusName
, convert(varchar,cast(a.pod_dnd_ReceivedDate as date),20) as DNDate
, a.pod_dnd_QtyReceive as DNQty
--
, a.po_ipo_InvoiceNumber as InvoiceNo
, a.po_ipo_Status as InvoiceStatus
, a.ipo_Status_mt_Name as InvoiceStatusName
, convert(varchar,cast(a.po_ipo_InvoiceDate as date),20) as InvoiceDate
, a.po_ipo_InvoiceAmmount as InvoiceAmount
--
, a.ipoioc_ppn_TotalBaseAmount as PPN
, a.ipoioc_pph23_TotalBaseAmount as PPH23
, a.ipoioc_pph42_TotalBaseAmount as PP42
, a.ipo_InvoiceAmmount_ipoioc_ppn_pph23_pph42_TotalBaseAmount as InvoiceAmountAfterTax
from 
(
select
1 as x
--
, pr.id as pr_id
, pr.RequestCode as pr_RequestCode
, pr.Status as pr_Status
, coalesce(pr_Status_mt.Name,cast(pr.Status as varchar)) as pr_Status_mt_Name
, pr.RequestDates as pr_RequestDates
, pr.RequestorUserName as pr_RequestorUserName
, pr.RequestorAccountId as pr_RequestorAccountId
, pr_RequestorAccountId_f_ua.CostCenterId as pr_RequestorAccountId_f_ua_CostCenterId
, pr_RequestorAccountId_f_ua.CostCenterName as pr_RequestorAccountId_f_ua_CostCenterName
--
, pr_prid.Id as pr_prid_Id
, pr_prid.L_Currency_Code as pr_prid_L_Currency_Code
--
, prid_i.Id as prid_i_Id
, prid_i.Name as prid_i_Name
, prid_v.Id as prid_v_Id
, prid_v.Name as prid_v_Name
, i_am.Id as i_am_Id
, concat (i_am.AccountCode,' - ',i_am.Description) as i_am_AccountCode_Description
--
, prid_pricc.Id as pricc_Id
, prid_pricc.CostCenterId as CostCenterItemPRID
, prid_pricc.cc_Id as prid_pricc_cc_Id
, prid_pricc.cc_Name as pricc_cc_Name
--
, prid_argm.UserName as prid_argm_UserName
, ua_p.Name as ua_p_Name
, prid_argm.ApprovalDate as prid_argm_ApprovalDate
--
, pr_prtpo.Id as pr_prtpo_Id
--
, prtpo_po.Id as prtpo_po_Id
, prtpo_po.PONumber as prtpo_po_PONumber
, prtpo_po.Status as prtpo_po_Status
, coalesce(po_Status_mt.Name,cast(prtpo_po.Status as varchar)) as po_Status_mt_Name
, prtpo_po.PoDate as prtpo_po_PoDate
, prtpo_po.TotalAmount as prtpo_po_TotalAmount
, prtpo_po.ApproverDate as prtpo_po_ApproverDate
--
, po_prid_pod.Id as pod_Id
--
, po_dn.Id as po_dn_Id
, po_dn.DeliveryNumber as po_dn_DeliveryNumber
--
, pod_dnd.Id as pod_dnd_Id
, pod_dnd.Status as pod_dnd_Status
, coalesce(dnd_Status_mt.Name,cast(pod_dnd.Status as varchar)) as dnd_Status_mt_Name
, pod_dnd.QtyReceive as pod_dnd_QtyReceive
, pod_dnd.ReceivedDate as pod_dnd_ReceivedDate
--
, po_ipo.Id as po_ipo_Id
, po_ipo.InvoiceNumber as po_ipo_InvoiceNumber
, po_ipo.InvoiceDate as po_ipo_InvoiceDate 
, po_ipo.Status as po_ipo_Status
, coalesce(ipo_Status_mt.Name,cast(po_ipo.Status as varchar)) as ipo_Status_mt_Name
, po_ipo.InvoiceAmmount as po_ipo_InvoiceAmmount
--
, ipo_pod_ipoioc_ppn.Id as ipoioc_ppn_Id
, ipo_pod_ipoioc_ppn.TotalBaseAmount as ipoioc_ppn_TotalBaseAmount
, ipo_pod_ipoioc_pph23.Id as ipoioc_pph23_Id
, ipo_pod_ipoioc_pph23.TotalBaseAmount as ipoioc_pph23_TotalBaseAmount
, ipo_pod_ipoioc_pph42.Id as ipoioc_pph42_Id
, ipo_pod_ipoioc_pph42.TotalBaseAmount as ipoioc_pph42_TotalBaseAmount
--
, 
(0
+ isnull(po_ipo.InvoiceAmmount,0)
+ isnull(ipo_pod_ipoioc_ppn.TotalBaseAmount,0)
+ isnull(ipo_pod_ipoioc_pph23.TotalBaseAmount,0)
+ isnull(ipo_pod_ipoioc_pph42.TotalBaseAmount,0)
) as ipo_InvoiceAmmount_ipoioc_ppn_pph23_pph42_TotalBaseAmount
-- pr
from 
(
select 
pr.*
from PurchaseRequest as pr
where 1 = 1
) as pr
-- pr > mt
left join
(
select 
mt.Valueid 
, mt.Name
from MasterTable as mt
where mt.Category = 'PurchaseRequest.Status'
) as pr_Status_mt
on pr_Status_mt.ValueId = pr.Status
-- pr > f_ua
inner join 
(
select 
f_ua.Id
, f_ua.CostCenterId
, f_ua.CostCenterName
from Flips.UserAccount as f_ua
where 1 = 1
) as pr_RequestorAccountId_f_ua
on pr.RequestorAccountId = pr_RequestorAccountId_f_ua.Id
-- pr > prid
inner join PurchaseRequestItemDetail as pr_prid
on pr_prid.PurchaseRequestId = pr.id
-- prid > i
inner join Item as prid_i
on prid_i.Id = pr_prid.ItemId
-- i > am
inner join
(
select
am.Id
, am.AccountCode
, am.Description
from AccountMaster as am
where 1 = 1
) as i_am
on i_am.Id = prid_i.AccountMasterID
-- prid > pricc
inner join 
(
select
pricc.PurchaseRequestItemDetailId
, pricc.Id
, pricc.CostCenterId
, pricc.L_Currency_Code
, pricc.TotalAmount
, cc.Id as cc_Id
, cc.Name as cc_Name
from PurchaseRequestItemCostCenter as pricc
inner join CostCenter as cc
on cc.Id = pricc.CostCenterid
where 1 = 1
) as prid_pricc
on prid_pricc.PurchaseRequestItemDetailId = pr_prid.Id
-- prid > v
left join
(
select
v.Id
, v.Code
, v.Name
from Vendor as v
where 1 = 1
) as prid_v
on prid_v.id = pr_prid.VendorId
-- prid > argm
left join
(
select argm.*
from ApprovalRequestGroupMember as argm
where argm.ApprovaGroup_SubCategoryId not in
(
select sc.Id
from SubCategory as sc
where sc.SubCategoryCode = 'SC-2024-01-01254'
)
)
as prid_argm
on prid_argm.ApprovalRequestId = pr_prid.ApprovalRequestId
-- argm > ua
left join Flips.UserAccount as argm_ua
on argm_ua.Username = prid_argm.UserName
-- ua > p
left join Flips.Position as ua_p
on ua_p.Id= argm_ua._Position
-- pr > prtpo
left join
(
select prtpo.*
from PurchaseOrderToPurchaseRequest as prtpo
inner join PurchaseOrder as po
on po.CategoryProcess_SubCategoryId = 1
and po.Id = prtpo.PurchaseOrderId
)
as pr_prtpo
on pr_prtpo.PurchaseRequestlId = pr.Id
-- prtpo > po
left join
(
select
po.Id
, po.PONumber
, po.Status
, po.PoDate
, po.ApproverDate
, po.TotalAmount
from PurchaseOrder as po
where 1 = 1
and po.CategoryProcess_SubCategoryId = 1
) as prtpo_po
on prtpo_po.Id = pr_prtpo.PurchaseOrderId
-- po > mt
left join
(
select 
mt.Valueid
, mt.Name
from MasterTable as mt
where mt.Category = 'PurchaseOrder.Status'
) as po_Status_mt
on prtpo_po.Status = po_Status_mt.ValueId 
-- po , prid > pod
left join PurchaseOrderDetail as po_prid_pod
on po_prid_pod.PurchaseOrderId = prtpo_po.Id
and po_prid_pod.PurchaseRequestItemDetailId = pr_prid.Id
-- po > dn
left join DeliveryNotes as po_dn
on po_dn.PurchaseOrderId = prtpo_po.Id
left join MasterTable as dn_Status_mt
on dn_Status_mt.Category = 'DeliveryNote.Status'
and dn_Status_mt.Valueid = po_dn.Status
-- pod > dnd
left join DeliveryNotesDetail as pod_dnd
on pod_dnd.PurchaseOrderDetailId = po_prid_pod.Id
left join MasterTable as dnd_Status_mt
on dnd_Status_mt.Category = 'DeliveryNote.Status'
and dnd_Status_mt.Valueid = po_dn.Status
-- po > ipo
left join InvoicePO as po_ipo
on po_ipo.PurchaeseOrderId = prtpo_po.Id
left join MasterTable as ipo_Status_mt
on ipo_Status_mt.Category = 'InvoiceManagement.Status'
and ipo_Status_mt.Valueid = po_ipo.Status
-- ipo , pod > ipoioc
left join
(
select
ipoioc.Id
, ipoioc.InvoicePOId
, ipoioc.ItemId
, sc.SubCategoryName
, ipoioc.TotalBaseAmount
from InvoicePOItemOtherCost as ipoioc
inner join SubCategory as sc
on sc.id = ipoioc.OtherCost_SubCategoryId
and sc.SubCategoryCode  =  'PPN'
) as ipo_pod_ipoioc_ppn
on ipo_pod_ipoioc_ppn.InvoicePOId = po_ipo.id
and ipo_pod_ipoioc_ppn.itemid = po_prid_pod.ItemId
-- ipo , pod > ipoioc
left join
(
select
ipoioc.Id
, ipoioc.InvoicePOId
, ipoioc.ItemId
, sc.SubCategoryName
, ipoioc.TotalBaseAmount
from InvoicePOItemOtherCost as ipoioc
inner join SubCategory as sc
on sc.id = ipoioc.OtherCost_SubCategoryId
and sc.SubCategoryCode = 'PPH23'
) as ipo_pod_ipoioc_pph23
on ipo_pod_ipoioc_pph23.InvoicePOId = po_ipo.id
and ipo_pod_ipoioc_pph23.itemid = po_prid_pod.ItemId
-- ipo , pod > ipoioc
left join
(
select 
ipoioc.Id
, ipoioc.InvoicePOId
, ipoioc.ItemId
, sc.SubCategoryName
, ipoioc.TotalBaseAmount
from InvoicePOItemOtherCost as ipoioc
inner join SubCategory as sc
on sc.id = ipoioc.OtherCost_SubCategoryId
and sc.SubCategoryCode = 'PPH42'
) as ipo_pod_ipoioc_pph42
on ipo_pod_ipoioc_pph42.InvoicePOId = po_ipo.id
and ipo_pod_ipoioc_pph42.ItemId = po_prid_pod.ItemId

) as a
where 1 = 1
--and a.pr_RequestCode = ''
--and a.pr_Status = 0
--and a.pr_RequestDates = ''
--and a.pr_RequestorAccountId_f_ua_CostCenterId = 0
--and a.i_am_Id = 0
--and a.prid_pricc_cc_Id = 0
--and a.prid_v_Id = 0
--and a.prtpo_po_PONumber = ''
--and a.prtpo_po_Status = 0
--and a.prtpo_po_PoDate = ''
{(!string.IsNullOrWhiteSpace(request.FilterPRNo) ? @$"and a.pr_RequestCode like '%{request.FilterPRNo}%'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterPRStatus) ? @$"and a.pr_Status = '{request.FilterPRStatus}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterRequestorAccountCostCenterId) ? @$"and a.pr_RequestorAccountId_f_ua_CostCenterId = '{request.FilterRequestorAccountCostCenterId}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterItemAccountMasterId) ? @$"and a.i_am_Id = '{request.FilterItemAccountMasterId}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterItemCostCenterId) ? @$"and a.prid_pricc_cc_Id = '{request.FilterItemCostCenterId}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterItemVendorId) ? @$"and a.prid_v_Id = '{request.FilterItemVendorId}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterPONo) ? @$"and a.prtpo_po_PONumber like '%{request.FilterPONo}%'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterPOStatus) ? @$"and a.prtpo_po_Status = '{request.FilterPOStatus}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterStartPODate) ? @$"and convert(varchar,a.prtpo_po_PoDate,23) >= '{request.FilterStartPODate}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterEndPODate) ? @$"and convert(varchar,a.prtpo_po_PoDate,23) <= '{request.FilterEndPODate}'" : string.Empty)}

-- sort
order by {request.SortColumn ?? "1"} {request.SortDirection ?? "asc"}

";
                if (!request.IsExport)
                {
                    query += $@"
-- paging
offset @Page rows
fetch next @PageSize rows only
";
                }
                var parms = new Dapper.DynamicParameters(new { request.Page, request.PageSize });
                var responses = await Task.FromResult(_dapper.GetAll<ReportPOShoppingCartExportResponse>(query, parms));
                return responses;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(nameof(ReportRepository), ex.InnerException);
            }
        }

        /// <summary>
        /// Get Report PO Non Shopping Cart List
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReportPONonShoppingCartListResponse>> GetReportPONonShoppingCartList(ReportPORequest request)
        {
            return (await GetReportPONonShoppingCartExport(request)).Select(x => (ReportPONonShoppingCartListResponse)x).ToList();
        }

        /// <summary>
        /// Get Report PO Non Shopping Cart Export
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReportPONonShoppingCartExportResponse>> GetReportPONonShoppingCartExport(ReportPORequest request)
        {
            try
            {
                var query = $@"
--
with
w as (select 1 as x)
, w_InvoicePOItemOtherCost_pivot_PPN_PPH23_PPH42 as (
select
p.InvoicePOId
, p.ItemId
, p.PPN
, p.PPH23
, p.PPH42
from
(
select 
ipoioc.InvoicePOId
, ipoioc.ItemId
, sc.SubCategoryCode
, ipoioc.TotalBaseAmount
from InvoicePOItemOtherCost as ipoioc
inner join SubCategory as sc
on ipoioc.OtherCost_SubCategoryId = sc.Id
and sc.SubCategoryCode in ('PPN','PPH23','PPH42')
) as f
pivot(max(f.TotalBaseAmount) for f.SubCategoryCode in ([PPN],[PPH23],[PPH42])) as p
)
--
select 
count(*) over() as CountData
--
, a.prf_PRFNo as PRNo
, a.prf_Status as PRStatus
, a.prf_Status_mt_Name as PRStatusName
, convert(varchar,cast(a.prf_RequestDate as date),20) as PRDate
, a.prf_RequestorUserName as Requester
, a.prf_ua_cc_Name as Department
, a.prf_L_Currency_Code as Currency
--
, a.prf_TypeOfTransaction as TypeOfTransaction
, a.prf_BuyerUserName as BuyerUserName
, a.prf_pra_prad_MtCriticalCategory as Critical
, a.prf_TotalBudgetEstimation as TotalBudgetEstimation
, a.prf_sc_Category as Category
--
, a.prf_ari_prfd_RequestItemNotes as ItemName
--
--, a.prf_argm_UserName as ApprovalUserName
--, a.prf_argm_ua_p_Name as ApprovalPosition
, a.prf_bu_ShortName as BusinessUnitShortName
--
, convert(varchar,cast(a.prf_ar_LastUpdatedTime  as date),20) as PRPostedDate
, convert(varchar,cast(a.prf_ari_prfd_DeliveryRequestDate  as date),20) as DeliveryRequestDate
, convert(varchar,cast(a.prf_LastUpdatedTime  as date),20) as FinalSpecReqDate
, convert(varchar,cast(a.prf_prfs_LastUpdatedTime  as date),20) as GenerateProcSumDate
--
, a.prf_am_AccountCode as AccountCode
, a.prf_cc_Name as CostCenter
--
, a.po_PONumber as PONo
, a.po_Status as POStatus
, a.po_Status_mt_Name as POStatusName
, convert(varchar,(cast(a.po_PoDate as datetime)),20) as PODate
, a.po_TotalAmount as POAmount
, convert(varchar,(cast(a.po_ApproverDate as datetime)),20) as POApproverDate
--
, a.TAT as TATWD
, a.SLA as SLAWD
, a.SLAStatus
--
, a.v_Name as VendorSelection
, a.v_Name as Vendor1
, case when a.prfsd_IsSelected = 1 then 'Selected' end as SelectedVendor1
, a.prfsd_TotalBaseAmmount as SelectedVendorsTotalBudget
, case when a.prfsd_TotalBaseAmmount is not null then coalesce(a.prf_TotalBudgetEstimation,0) - coalesce(a.prfsd_TotalBaseAmmount,0) end as RealisedSaving
--
, a.dn_DeliveryNumber as DNNo
, a.dnd_Status as DNStatus
, a.dnd_Status_mt_Name as DNStatusName
, convert(varchar,cast(a.dnd_ReceivedDate as date),20) as DNDate
, a.dnd_QtyReceive as DNQty
--
, a.ipo_InvoiceNumber as InvoiceNo
, a.ipo_Status as InvoiceStatus
, a.ipo_Status_mt_Name as InvoiceStatusName
, convert(varchar,cast(a.ipo_InvoiceDate as date),20) as InvoiceDate
, a.ipo_InvoiceAmmount as InvoiceAmount
--
, a.ipoioc_PPN as PPN
, a.ipoioc_PPH23 as PPH23
, a.ipoioc_PPH42 as PPH42
, a.ipo_InvoiceAmmount_ipoioc_ppn_pph23_pph42_TotalBaseAmount as InvoiceAmountAfterTax
--
from
(
--
select 
1 as x
--
, prf.Id as prf_Id
, prf_ua.Id as prf_ua_Id
, prf_ua_cc.Id as prf_ua_cc_Id
, prf_Status_mt.Id as prf_Status_mt_Id
, prf_am.Id as prf_am_Id
, prf_cc.Id as prf_cc_Id
, prf_bu.Id as prf_bu_Id
, prf_sc.Id as prf_sc_Id
, prf_pra.Id as pra_Id
, prf_pra_prad.Id as prad_Id

, prf_prfd.Id as prfd_Id
, prfd_ari.Id as ari_Id

--, prf_argm.Id as argm_Id
--, prf_argm_ua.Id as argm_ua_Id
--, prf_argm_ua_p.Id as argm_ua_p_Id

, prf_prfvq.Id as prfvq_Id
, prfd_prfvq_prfvqd.Id as prfvqd_Id
, prf_prfs.Id as prfs_Id
, prfs_prfvqd_prfsd.Id as prfsd_Id
, prfsd_v.Id as prfsd_v_Id

, prfsd_prfstpo.Id as prfstpo_Id
, prfstpo_po.Id as po_Id
, po_Status_mt.Id as po_Status_mt_Id
, po_pod.Id as pod_Id
, po_dn.Id as dn_Id
, pod_dnd.Id as dnd_Id
, dnd_Status_mt.Id as dnd_Status_mt_Id
, po_ipo.Id as ipo_Id
, ipo_Status_mt.Id as ipo_Status_mt_Id
-- prf
, prf.PRFNo as prf_PRFNo
, prf.Status as prf_Status
, prf_Status_mt.Name as prf_Status_mt_Name
, prf.RequestDate as prf_RequestDate
, prf.BuyerUserName as prf_BuyerUserName
, prf.TypeOfTransaction as prf_TypeOfTransaction
, prf.TotalBudgetEstimation as prf_TotalBudgetEstimation
, prf.RequestorUserName as prf_RequestorUserName
, prf.L_Currency_Code as prf_L_Currency_Code
-- prf ++
, prf_ua_cc.Name as prf_ua_cc_Name -- as dept
, prf_cc.Name as prf_cc_Name -- as CostCenter
, prf_bu.ShortName as prf_bu_ShortName -- as Entity
, prf_sc.Category as prf_sc_Category
--, prf_argm.UserName as prf_argm_UserName -- as approver
--, prf_argm_ua_p.Name as prf_argm_ua_p_Name -- as titleApproval
--
, prf_pra_prad.MtCriticalCategory as prf_pra_prad_MtCriticalCategory -- as critical
, prf_prfd.RequestItemNotes as prf_ari_prfd_RequestItemNotes
, prf_prfd.DeliveryRequestDate as prf_ari_prfd_DeliveryRequestDate
, concat(prf_am.AccountCode,' - ',prf_am.Description) as prf_am_AccountCode -- as AccountCode
-- po
, prfstpo_po.PONumber as po_PONumber
, prfstpo_po.Status as po_Status
, po_Status_mt.Name as po_Status_mt_Name
, prfstpo_po.PoDate as po_PoDate
, prfstpo_po.TotalAmount as po_TotalAmount
, prfstpo_po.ApproverDate as po_ApproverDate
-- ???
, (case when prf_ar.Status = 2 then cast(prf_ar.LastUpdatedTime as date) end) as prf_ar_LastUpdatedTime -- as PrPostedDate
, (case when ltrim(rtrim(coalesce(prf.BuyerUserName,''))) <> '' then cast(prf.LastUpdatedTime as date) end) as prf_LastUpdatedTime -- as FinalSpecReq
, (case when prf_prfs.Status = 2 then cast(prf_prfs.LastUpdatedTime as date) end ) as prf_prfs_LastUpdatedTime -- as GenerateProcSum
-- sla
, datediff(day,(case when ltrim(rtrim(coalesce(prf.BuyerUserName,''))) <> '' then cast(prf.LastUpdatedTime as date) end),cast(prf_prfs.LastUpdatedTime as date)) as TAT
, 5 as SLA
, (case when datediff(day,(case when ltrim(rtrim(coalesce(prf.BuyerUserName,''))) <> '' then cast(prf.LastUpdatedTime as date) end),cast(prf_prfs.LastUpdatedTime as date)) <= 5 then 'MEET' else 'NON MEET' end) as SLAStatus
-- prfsd
, prfs_prfvqd_prfsd.IsSelected as prfsd_IsSelected
, prfs_prfvqd_prfsd.TotalBaseAmmount as prfsd_TotalBaseAmmount
-- v
, prfsd_v.Name as v_Name
-- dn & dnd
, po_dn.DeliveryNumber as dn_DeliveryNumber
, pod_dnd.Status as dnd_Status
, dnd_Status_mt.Name as dnd_Status_mt_Name
, pod_dnd.ReceivedDate as dnd_ReceivedDate
, pod_dnd.QtyReceive as dnd_QtyReceive
-- ipo
, po_ipo.InvoiceNumber as ipo_InvoiceNumber
, po_ipo.Status as ipo_Status
, ipo_Status_mt.Name as ipo_Status_mt_Name
, po_ipo.InvoiceDate as ipo_InvoiceDate
, po_ipo.InvoiceAmmount as ipo_InvoiceAmmount
-- ipoioc
, ipo_pod_ipoioc.PPN as ipoioc_PPN
, ipo_pod_ipoioc.PPH23 as ipoioc_PPH23
, ipo_pod_ipoioc.PPH42 as ipoioc_PPH42
, 
( 0
+ coalesce(po_ipo.InvoiceAmmount,0)
+ coalesce(ipo_pod_ipoioc.PPN,0)
+ coalesce(ipo_pod_ipoioc.PPH23,0)
+ coalesce(ipo_pod_ipoioc.PPH42,0)
) as ipo_InvoiceAmmount_ipoioc_ppn_pph23_pph42_TotalBaseAmount
-- prf
from PRF as prf
-- prf > ua
left join Flips.UserAccount as prf_ua on prf_ua.Username = prf.RequestorUserName
-- prf > ua > cc
left join CostCenter as prf_ua_cc on prf_ua_cc.Id = prf_ua.CostCenterid
-- prf > mt
left join MasterTable as prf_Status_mt on prf_Status_mt.ValueId = prf.Status and prf_Status_mt.Category = 'PRF.Status'
-- prf > am
left join AccountMaster as prf_am on prf_am.AccountCode = prf.BudgetCode and prf_am.Status = 1
-- prf > cc
left join CostCenter as prf_cc on prf_cc.Id = prf.CostCenterId
-- prf > bu
left join BusinessUnit as prf_bu on prf_bu.Id = prf.BusinesUnitId
-- prf > sc
left join Spending_Category as prf_sc on prf_sc.id = prf.Spending_Category and prf_sc.Status = 1

-- prf > pra
left join ProcurementRiskAssesment as prf_pra on prf_pra.PRFNo = prf.PRFNo
---- prf > pra > prad
left join ProcurementRiskAssesmentDetail as prf_pra_prad on prf_pra_prad.ProcurementRiskAssesmentId = prf_pra.Id and prf_pra_prad.Sequence = 6

-- prf > prfd
left join PRFDetail as prf_prfd on prf_prfd.PRFId = prf.Id
-- prfd > ari
left join ApprovalRequestItem as prfd_ari
on prfd_ari.Id = prf_prfd.ApprovalRequestItemId

-- prf > ar
left join ApprovalRequest as prf_ar on prf_ar.Id = prf.ApprovalRequestId

---- prf > argm
--left join  ApprovalRequestGroupMember as prf_argm on prf_argm.ApprovalRequestId = prfd_ari.ApprovalRequestId
---- prf > argm > ua
--left join Flips.UserAccount as prf_argm_ua on prf_argm_ua.Id = prf_argm.AccountId
---- prf > argm > ua > p
--left join Flips.Position as prf_argm_ua_p on prf_argm_ua_p.Id = prf_argm_ua._Position

-- prf > prfvq
left join PRFVendorQuotation as prf_prfvq on prf_prfvq.PRFId = prf.Id
-- prfd & prfvq > prfvqd
left join PRFVendorQuotationDetail as prfd_prfvq_prfvqd on prfd_prfvq_prfvqd.PRFDetailId = prf_prfd.Id and prfd_prfvq_prfvqd.PRFVendorQuotationId = prf_prfvq.Id
-- prf > prfs
left join PRFSummary as prf_prfs on prf_prfs.PRFId = prf.Id
-- prfs & prfvqd > prfs
left join PRFSummaryDetail as prfs_prfvqd_prfsd on prfs_prfvqd_prfsd.PRFSummaryId = prf_prfs.Id and prfs_prfvqd_prfsd.PRFVendorQuotationDetailId = prfd_prfvq_prfvqd.Id
-- prfsd > v
left join Vendor as prfsd_v on prfsd_v.Id = prfs_prfvqd_prfsd.VendorId

-- prfsd > prfstpo
left join
(
select
prtpo.Id
, prtpo.PurchaseRequestlId as PRFSummaryId
, prtpo.PurchaseOrderId
, po.VendorId
from PurchaseOrderToPurchaseRequest as prtpo
inner join PurchaseOrder as po
on prtpo.PurchaseOrderId = po.Id
inner join SubCategory as sc
on sc.Id = po.CategoryProcess_SubCategoryId
and sc.SubCategoryCode = 'CA-00001-002'
)
as prfsd_prfstpo
on prfsd_prfstpo.PRFSummaryId = prfs_prfvqd_prfsd.PRFSummaryId
and prfsd_prfstpo.VendorId = prfs_prfvqd_prfsd.VendorId

-- prfstpo > po
left join PurchaseOrder as prfstpo_po on prfstpo_po.Id = prfsd_prfstpo.PurchaseOrderId
-- po > mt
left join MasterTable as po_Status_mt on po_Status_mt.ValueId = prfstpo_po.Status and po_Status_mt.Category = 'PurchaseOrder.Status'
-- po > pod
left join PurchaseOrderDetail as po_pod on po_pod.PurchaseOrderId = prfstpo_po.Id
and po_pod.Id = prfs_prfvqd_prfsd.Id
-- po > dn
left join DeliveryNotes as po_dn on po_dn.PurchaseOrderId = prfstpo_po.Id
-- pod > dnd
left join DeliveryNotesDetail as pod_dnd on pod_dnd.PurchaseOrderDetailId = po_pod.Id
-- dnd > mt
left join MasterTable as dnd_Status_mt on dnd_Status_mt.ValueId = pod_dnd.Status and dnd_Status_mt.Category = 'DeliveryNote.Status'
-- po > ipo
left join InvoicePO as po_ipo on po_ipo.PurchaeseOrderId = prfstpo_po.Id
-- ipo > mt
left join MasterTable as ipo_Status_mt on ipo_Status_mt.ValueId = po_ipo.Status and ipo_Status_mt.Category = 'InvoiceManagement.Status'
-- ipo & pod > ipoioc
left join w_InvoicePOItemOtherCost_pivot_PPN_PPH23_PPH42 as ipo_pod_ipoioc on ipo_pod_ipoioc.InvoicePOId = po_ipo.Id and ipo_pod_ipoioc.ItemId = po_pod.ItemId
) as a
where 1 = 1
{(!string.IsNullOrWhiteSpace(request.FilterPRNo) ? @$"and a.prf_PRFNo like '%{request.FilterPRNo}%'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterPRStatus) ? @$"and a.prf_Status = '{request.FilterPRStatus}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterRequestorAccountCostCenterId) ? @$"and a.prf_ua_cc_Id = '{request.FilterRequestorAccountCostCenterId}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterItemAccountMasterId) ? @$"and a.prf_am_Id = '{request.FilterItemAccountMasterId}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterItemCostCenterId) ? @$"and a.prf_cc_Id = '{request.FilterItemCostCenterId}'" : string.Empty)}
--{(!string.IsNullOrWhiteSpace(request.FilterItemVendorId) ? @$"and a.prid_v_Id = '{request.FilterItemVendorId}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterPONo) ? @$"and a.po_PONumber like '%{request.FilterPONo}%'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterPOStatus) ? @$"and a.po_Status = '{request.FilterPOStatus}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterStartPODate) ? @$"and convert(varchar,a.po_PoDate,23) >= '{request.FilterStartPODate}'" : string.Empty)}
{(!string.IsNullOrWhiteSpace(request.FilterEndPODate) ? @$"and convert(varchar,a.po_PoDate,23) <= '{request.FilterEndPODate}'" : string.Empty)}

-- sort
order by {request.SortColumn ?? "1"} {request.SortDirection ?? "asc"}

";
                if (!request.IsExport)
                {
                    query += $@"
-- paging
offset @Page rows
fetch next @PageSize rows only
";
                }
                var parms = new Dapper.DynamicParameters(new { request.Page, request.PageSize });
                var responses = await Task.FromResult(_dapper.GetAll<ReportPONonShoppingCartExportResponse>(query, parms));
                return responses;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(nameof(ReportRepository), ex.InnerException);
            }
        }

        /// <summary>
        /// Export Excel Report Inquiry Payment
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <exception cref="GlobalExceptions"></exception>
        public async Task<MemoryStream> ExportInquiryPaymentToExcel(ReportPaymentListRequest param)
        {
            try
            {
                log.LogInitialize(nameof(ExportInquiryPaymentToExcel), AppSystem.StartInquiryPayment, LogType.Info);

                List<ReportPaymentSummaryListResponse> inquiryPayment;
                ExcelWorksheet worksheetReport;
                param.IsExport = true;
                var stream = new MemoryStream();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var xlPackage = new ExcelPackage(stream))
                {
                    if (param.ReportType.Contains(AppSystem.ReportTypeSummaryTextString, StringComparison.CurrentCultureIgnoreCase))
                    {
                        worksheetReport = xlPackage.Workbook.Worksheets.Add(AppSystem.WorksheetNameInquiryPaymentSummary);
                        param.ReportType = AppSystem.ReportTypeSummary;
                        inquiryPayment = await GetReportPaymentSummaryList(param);
                    }
                    else
                    {
                        worksheetReport = xlPackage.Workbook.Worksheets.Add(AppSystem.WorksheetNameInquiryPaymentDetail);
                        param.ReportType = AppSystem.ReportTypeDetail;
                        inquiryPayment = await GetReportPaymentList(param);
                    }

                    int row = 6;
                    #region worksheet header

                    worksheetReport.Cells["A1"].Value = "Payment List";
                    worksheetReport.Cells["A2"].Value = "APS Revamp";
                    worksheetReport.Cells["A3"].Value = $"Date: {DateTime.Now.ToString("dd-MM-yyyy")}";
                    worksheetReport.Cells["A4"].Value = $"Payment List From {param.RequestDateFrom ?? string.Empty} - To {param.RequestDateTo ?? string.Empty}";

                    int rowIndex = 5;
                    int columnIndex = 0;
                    string cellAddress = string.Empty;

                    string[] columnHeaders = { "No", "Transaction Number (MCM Number)", "Voucher Number", "Request Number", "VENDOR / STAFF / OTHER", "VENDOR / STAFF / OTHER -ID-", "VENDOR / STAFF / OTHER -NAME-",
                    "Request Type", "Month", "Date Of Settlement", "Requestor Name", "Maker Finance", "Request Date", "Received By Finance", "Paid By Finance", "SLA / TAT", "Account Code", "Account Name",
                    "Business Unit Code", "Business Unit Name", "Cost Center Code", "Cost Center Name", "Benefeciares", "Account Number", "Bank Name", "Currency", "Rate", "Amount", "PPN", "Pph23", "Pph21", "PPh42", "Stamp Duty",
                    "Nett Amount", "PPN Gross Up", "PPh23 Gross Up", "PPh21 Gross Up", "PPh42 Gross Up", "Invoice Detail No.", "Description", "Description Detail", "Document Number", "Status Payment" };

                    for (int i = 0; i < columnHeaders.Length; i++)
                    {
                        string columnName = ReportHelper.GetExcelColumnName(columnIndex);
                        cellAddress = columnName + rowIndex;
                        // Set the value in the cell
                        worksheetReport.Cells[cellAddress].Value = columnHeaders[i];
                        columnIndex++;
                    }

                    if (param.IsCashAdvance)
                    {
                        string[] columnHeadersCA = { "Settlement Number", "Date Of Settlement", "Received Settlement By Finance", "Status Repair", "Due Date", "Balance", "Settlement Term", "Overdue Days", "Status", "Due To Company", "Realization Amount",
                            "Transfer Date Due To Company", "New Advance Number", "New Advance Amount", "New Advance Transfer Date"};

                        for (int i = 0; i < columnHeadersCA.Length; i++)
                        {
                            string columnNameCA = ReportHelper.GetExcelColumnName(columnIndex);
                            cellAddress = columnNameCA + rowIndex;
                            // Set the value in the cell
                            worksheetReport.Cells[cellAddress].Value = columnHeadersCA[i];
                            columnIndex++;
                        }
                    }

                    #endregion

                    #region worksheet body
                    string cleanedInput;
                    foreach (var r in inquiryPayment)
                    {
                        var colBody = 1;
                        worksheetReport.Cells[row, colBody++].Value = row - 5;
                        worksheetReport.Cells[row, colBody++].Value = r.TransferNumber;
                        worksheetReport.Cells[row, colBody++].Value = r.VoucherNumber;
                        worksheetReport.Cells[row, colBody++].Value = r.RequestNumber;
                        worksheetReport.Cells[row, colBody++].Value = r.VendorType;
                        worksheetReport.Cells[row, colBody++].Value = r.VendorCode;
                        worksheetReport.Cells[row, colBody++].Value = r.VendorName;
                        worksheetReport.Cells[row, colBody++].Value = r.RequestType;
                        worksheetReport.Cells[row, colBody++].Value = r.Month;
                        worksheetReport.Cells[row, colBody++].Value = r.SettlementDate;
                        worksheetReport.Cells[row, colBody++].Value = r.RequestorName;
                        worksheetReport.Cells[row, colBody++].Value = r.MakerFinance;
                        worksheetReport.Cells[row, colBody++].Value = r.RequestDateString;
                        worksheetReport.Cells[row, colBody++].Value = r.ReceivedByFinance;
                        worksheetReport.Cells[row, colBody++].Value = r.PaidByFinance;
                        worksheetReport.Cells[row, colBody++].Value = r.SLA;
                        worksheetReport.Cells[row, colBody++].Value = r.AccountMasterCode;
                        worksheetReport.Cells[row, colBody++].Value = r.AccountMasterName;
                        worksheetReport.Cells[row, colBody++].Value = r.BusinessUnitCode;
                        worksheetReport.Cells[row, colBody++].Value = r.BusinessUnitName;
                        worksheetReport.Cells[row, colBody++].Value = r.CostCenterCode;
                        worksheetReport.Cells[row, colBody++].Value = r.CostCenterName;
                        worksheetReport.Cells[row, colBody++].Value = r.Beneficiaries;
                        worksheetReport.Cells[row, colBody++].Value = r.BankAccountNumber;
                        worksheetReport.Cells[row, colBody++].Value = r.BankName;
                        worksheetReport.Cells[row, colBody++].Value = r.LCurrencyCode;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        cleanedInput = r.Rate.ReplaceCommaToEmpty();
                        decimal rate = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = rate;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        r.Amount = string.IsNullOrEmpty(r.Amount) ? 0.ToString() : r.Amount;
                        cleanedInput = r.Amount.ReplaceCommaToEmpty();
                        decimal amount = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = amount;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        r.Ppn = string.IsNullOrEmpty(r.Ppn) ? 0.ToString() : r.Ppn;
                        cleanedInput = r.Ppn.ReplaceCommaToEmpty();
                        decimal ppn = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = ppn;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        r.Pph23 = string.IsNullOrEmpty(r.Pph23) ? 0.ToString() : r.Pph23;
                        cleanedInput = r.Pph23.ReplaceCommaToEmpty();
                        decimal pph23 = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = pph23;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        r.Pph21 = string.IsNullOrEmpty(r.Pph21) ? 0.ToString() : r.Pph21;
                        cleanedInput = r.Pph21.ReplaceCommaToEmpty();
                        decimal pph21 = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = pph21;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        r.Pph42 = string.IsNullOrEmpty(r.Pph42) ? 0.ToString() : r.Pph42;
                        cleanedInput = r.Pph42.ReplaceCommaToEmpty();
                        decimal pph42 = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = pph42;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        r.StampDuty = string.IsNullOrEmpty(r.StampDuty) ? 0.ToString() : r.StampDuty;
                        cleanedInput = r.StampDuty.ReplaceCommaToEmpty();
                        decimal stampDuty = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = stampDuty;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        cleanedInput = r.NettAmount.ReplaceCommaToEmpty();
                        decimal nettAmount = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = nettAmount;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        cleanedInput = OtherCostConst.PPN.GetValueGrossUp(r.GrossUp).ReplaceCommaToEmpty();
                        decimal grossUpPpn = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = grossUpPpn;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        cleanedInput = OtherCostConst.PPH23.GetValueGrossUp(r.GrossUp).ReplaceCommaToEmpty();
                        decimal grossUpPph23 = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = grossUpPph23;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        cleanedInput = OtherCostConst.PPH21.GetValueGrossUp(r.GrossUp).ReplaceCommaToEmpty();
                        decimal grossUpPph21 = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = grossUpPph21;

                        worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                        cleanedInput = OtherCostConst.PPH42.GetValueGrossUp(r.GrossUp).ReplaceCommaToEmpty();
                        decimal grossUpPph42 = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                        worksheetReport.Cells[row, colBody++].Value = grossUpPph42;

                        worksheetReport.Cells[row, colBody++].Value = r.InvoiceNo;
                        worksheetReport.Cells[row, colBody++].Value = r.Description;
                        worksheetReport.Cells[row, colBody++].Value = r.DescriptionDetail;
                        worksheetReport.Cells[row, colBody++].Value = r.DocumentNumber;
                        worksheetReport.Cells[row, colBody++].Value = r.StatusTransferDesc;

                        if (param.IsCashAdvance)
                        {
                            worksheetReport.Cells[row, colBody++].Value = r.SettlementNumber;
                            worksheetReport.Cells[row, colBody++].Value = r.SettlementDate;
                            worksheetReport.Cells[row, colBody++].Value = r.ReceivedSettlementByFinance;
                            worksheetReport.Cells[row, colBody++].Value = r.StatusRepair;
                            worksheetReport.Cells[row, colBody++].Value = r.DueDate;

                            worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                            r.BalanceAmount = string.IsNullOrEmpty(r.BalanceAmount) ? 0.ToString() : r.BalanceAmount;
                            cleanedInput = r.BalanceAmount.ReplaceCommaToEmpty();
                            decimal balanceAmount = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                            worksheetReport.Cells[row, colBody++].Value = balanceAmount;

                            worksheetReport.Cells[row, colBody++].Value = "31 Days"; // will be change refer to settlement term
                            worksheetReport.Cells[row, colBody++].Value = r.OverdueDays;
                            if (!string.IsNullOrEmpty(r.OverdueDays) && int.Parse(r.OverdueDays) > 3)
                            {
                                worksheetReport.Cells[row, colBody].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                worksheetReport.Cells[row, colBody].Style.Fill.BackgroundColor.SetColor(Color.Red);
                            }
                            worksheetReport.Cells[row, colBody++].Value = r.StatusOverdue;

                            worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                            r.DueToCompany = string.IsNullOrEmpty(r.DueToCompany) ? 0.ToString() : r.DueToCompany;
                            cleanedInput = r.DueToCompany.ReplaceCommaToEmpty();
                            decimal dueToCompany = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                            worksheetReport.Cells[row, colBody++].Value = dueToCompany;

                            worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                            r.RealizationAmount = string.IsNullOrEmpty(r.RealizationAmount) ? 0.ToString() : r.RealizationAmount;
                            cleanedInput = r.RealizationAmount.ReplaceCommaToEmpty();
                            decimal realitationAmount = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                            worksheetReport.Cells[row, colBody++].Value = realitationAmount;

                            worksheetReport.Cells[row, colBody++].Value = r.TransferDateDueToCompany;
                            worksheetReport.Cells[row, colBody++].Value = r.NewAdvanceNumber;

                            worksheetReport.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                            r.NewAdvanceAmount = string.IsNullOrEmpty(r.NewAdvanceAmount) ? 0.ToString() : r.NewAdvanceAmount;
                            cleanedInput = r.NewAdvanceAmount.ReplaceCommaToEmpty();
                            decimal newAdvanceAmount = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                            worksheetReport.Cells[row, colBody++].Value = newAdvanceAmount;

                            worksheetReport.Cells[row, colBody++].Value = r.NewAdvanceTransferDate;
                        }

                        #region Report AUDIT
                        // optional, just for audit
                        if (param.IsReportAudit)
                        {
                            for (int i = 0; i < r.ApprovalRequestGroupMemberModel?.Count; i++)
                            {
                                string columnName = ReportHelper.GetExcelColumnName(columnIndex);
                                cellAddress = columnName + rowIndex;
                                // Set the value in the cell
                                worksheetReport.Cells[cellAddress].Value = $"Approval {i + 1}";
                                worksheetReport.Cells[row, colBody].Value = r.ApprovalRequestGroupMemberModel[i].UserName;
                                colBody++;
                                columnIndex++;
                            }
                        }
                        #endregion

                        worksheetReport.Cells[$"A5:{cellAddress}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheetReport.Cells[$"A5:{cellAddress}"].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);

                        row++;
                    }
                    #endregion

                    await xlPackage.SaveAsync();
                }

                stream.Position = 0;
                log.LogInitialize(nameof(ExportInquiryPaymentToExcel), AppSystem.FinishInquiryPayment, LogType.Info);
                return stream;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(nameof(ReportRepository), ex.InnerException);
            }
        }

        /// <summary>
        /// Export Excel Report Inquiry Payment
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <exception cref="GlobalExceptions"></exception>
        public async Task<MemoryStream> ExportPajakToExcel(ReportPajakListRequest param)
        {
            try
            {
                log.LogInitialize(nameof(ExportInquiryPaymentToExcel), AppSystem.StartInquiryPajak, LogType.Info);

                List<ReportPajakListResponse> inquiryPajak;
                ExcelWorksheet worksheetReport;
                param.IsExport = true;
                var stream = new MemoryStream();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                inquiryPajak = await GetReportPajakList(param);
                using (var xlPackage = new ExcelPackage(stream))
                {
                    if (param.ReportType.Equals(AppSystem.ReportPajakGeneral, StringComparison.CurrentCultureIgnoreCase))
                    {
                        worksheetReport = xlPackage.Workbook.Worksheets.Add(AppSystem.WorksheetNameInquiryPajakGeneral);
                        param.ReportType = AppSystem.ReportTypeSummary;
                        ReportPajakGeneralStream(param, inquiryPajak, worksheetReport);
                    }
                    else if (param.ReportType.Equals(AppSystem.ReportPajakPph21, StringComparison.CurrentCultureIgnoreCase))
                    {
                        worksheetReport = xlPackage.Workbook.Worksheets.Add(AppSystem.WorksheetNameInquiryPajakPph21);
                        param.ReportType = AppSystem.ReportTypeSummary;
                        ReportPajakPph21Stream(inquiryPajak, worksheetReport);
                    }
                    else if (param.ReportType.Equals(AppSystem.ReportPajakPph23, StringComparison.CurrentCultureIgnoreCase))
                    {
                        worksheetReport = xlPackage.Workbook.Worksheets.Add(AppSystem.WorksheetNameInquiryPajakPph23);
                        param.ReportType = AppSystem.ReportTypeSummary;
                        ReportPajakPph23Stream(inquiryPajak, worksheetReport);
                    }
                    else if (param.ReportType.Equals(AppSystem.ReportPajakPph26, StringComparison.CurrentCultureIgnoreCase))
                    {
                        worksheetReport = xlPackage.Workbook.Worksheets.Add(AppSystem.WorksheetNameInquiryPajakPph26);
                        param.ReportType = AppSystem.ReportTypeSummary;
                        ReportPajakPph26Stream(inquiryPajak, worksheetReport);
                    }
                    await xlPackage.SaveAsync();
                }

                stream.Position = 0;
                log.LogInitialize(nameof(ExportInquiryPaymentToExcel), AppSystem.FinishInquiryPajak, LogType.Info);
                return stream;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(nameof(ReportRepository), ex.InnerException);
            }
        }

        private static void ReportPajakGeneralStream(ReportPajakListRequest param, List<ReportPajakListResponse> inquiryPajak, ExcelWorksheet worksheetReport)
        {
            int row = 6;

            #region worksheet header

            worksheetReport.Cells["A1"].Value = "Report Pajak - General Non Benefit";
            worksheetReport.Cells["A2"].Value = "Flips 2.0";
            worksheetReport.Cells["A3"].Value = $"Date: {DateTime.Now.ToString("dd-MM-yyyy")}";
            worksheetReport.Cells["A4"].Value = $"Report Pajak From {param.TransferTimeStart ?? string.Empty} - To {param.TransferTimeEnd ?? string.Empty}";

            int rowIndex = 5;
            int columnIndex = 0;
            string cellAddress = string.Empty;

            string[] columnHeaders = { "No", "NPWP", "NIK (If NPWP not available)", "Nama", "Alamat", "Keterangan", "No Giro/BMT", "Pay Date", "Jumlah", "Tarif", "Nilai Pajak", "Jenis Pajak",
                                          "Jenis Jasa", "Invoice", "Maker", "Invoice Date", "SKB/Suket PP23 (If any)", "Request Number"};

            for (int i = 0; i < columnHeaders.Length; i++)
            {
                string columnName = ReportHelper.GetExcelColumnName(columnIndex);
                cellAddress = columnName + rowIndex;
                // Set the value in the cell
                worksheetReport.Cells[cellAddress].Value = columnHeaders[i];
                columnIndex++;
            }

            #endregion

            #region worksheet body
            string cleanedInput;
            foreach (var r in inquiryPajak)
            {
                var colBody = 1;
                worksheetReport.Cells[row, colBody++].Value = row - 5;
                worksheetReport.Cells[row, colBody++].Value = r.Npwp;
                worksheetReport.Cells[row, colBody++].Value = r.Nik;
                worksheetReport.Cells[row, colBody++].Value = r.Nama;
                worksheetReport.Cells[row, colBody++].Value = r.Alamat;
                worksheetReport.Cells[row, colBody++].Value = r.Keterangan;
                worksheetReport.Cells[row, colBody++].Value = r.Mcm;

                DateTime payDate = DateTime.ParseExact(r.PayDate, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                DateTime invoiceDate = DateTime.ParseExact(r.InvoiceDate, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                worksheetReport.Cells[row, colBody++].Value = payDate.ToString("M/dd/yyyy", CultureInfo.InvariantCulture);

                cleanedInput = r.Jumlah.ReplaceCommaToEmpty();
                decimal jumlah = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                worksheetReport.Cells[row, colBody++].Value = jumlah;

                worksheetReport.Cells[row, colBody++].Value = Math.Round(Convert.ToDecimal(r.Tarif), 2);

                cleanedInput = r.NilaiPajak.ReplaceCommaToEmpty();

                decimal nilaiPajak = decimal.Parse(
                    cleanedInput,
                    NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture
                );
                worksheetReport.Cells[row, colBody++].Value = nilaiPajak;
                worksheetReport.Cells[row, colBody++].Value = r.JenisPajak;
                worksheetReport.Cells[row, colBody++].Value = string.Empty;
                worksheetReport.Cells[row, colBody++].Value = r.Invoice;
                worksheetReport.Cells[row, colBody++].Value = r.MakerFinance;
                worksheetReport.Cells[row, colBody++].Value = invoiceDate.ToString("M/dd/yyyy", CultureInfo.InvariantCulture);
                worksheetReport.Cells[row, colBody++].Value = r.Skb;
                worksheetReport.Cells[row, colBody].Value = r.RequestNumber;

                worksheetReport.Cells[$"A5:{cellAddress}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetReport.Cells[$"A5:{cellAddress}"].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);

                row++;
            }
            #endregion
        }

        private static void ReportPajakPph21Stream(List<ReportPajakListResponse> inquiryPajak, ExcelWorksheet worksheetReport)
        {
            int row = 4;

            #region worksheet header
            SetDefaultReport(worksheetReport);

            int rowIndex = 3;
            int columnIndex = 1;
            string cellAddress = string.Empty;

            string[] columnHeaders = { "Masa Pajak", "Tahun Pajak", "NPWP", "ID TKU Penerima Penghasilan", "Status PTKP", "Fasilitas", "Kode Objek Pajak", "Penghasilan",
                                       "Deemed", "Tarif", "Jenis Dok. Referensi", "Nomor Dok. Referensi", "Tanggal Dok. Referensi", "ID TKU Pemotong", "Tanggal Pemotongan",
                                       "", "DPP", "JNSTARIF", "TER A", "TER B", "TER C", "PS17", "HARIAN", "PESANGON", "PENSIUN"};

            for (int i = 0; i < columnHeaders.Length; i++)
            {
                string columnName = ReportHelper.GetExcelColumnName(columnIndex);
                cellAddress = columnName + rowIndex;
                // Set the value in the cell
                worksheetReport.Cells[cellAddress].Value = columnHeaders[i];

                if (i < 15)
                    SetBackgroundColorHeader(worksheetReport, cellAddress);


                columnIndex++;
            }

            #endregion

            #region worksheet body
            string cleanedInput;
            foreach (var r in inquiryPajak)
            {
                var colBody = 2;
                worksheetReport.Cells[row, colBody++].Value = r.MasaPajak;
                worksheetReport.Cells[row, colBody++].Value = r.TahunPajak;
                worksheetReport.Cells[row, colBody++].Value = r.Npwp;
                worksheetReport.Cells[row, colBody++].Value = r.IdTkuPenerimaPenghasilan;
                worksheetReport.Cells[row, colBody++].Value = r.StatusPtkp;
                worksheetReport.Cells[row, colBody++].Value = r.Fasilitas;
                worksheetReport.Cells[row, colBody++].Value = string.Empty;

                cleanedInput = r.Penghasilan.ReplaceCommaToEmpty();
                decimal penghasilan = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                worksheetReport.Cells[row, colBody++].Value = penghasilan;

                worksheetReport.Cells[row, colBody++].Value = string.Empty;
                worksheetReport.Cells[row, colBody++].Value = Math.Round(Convert.ToDecimal(r.Tarif), 2);

                worksheetReport.Cells[row, colBody++].Value = r.JenisDokReferensi;
                worksheetReport.Cells[row, colBody++].Value = r.NomorDokReferensi;
                worksheetReport.Cells[row, colBody++].Value = r.TanggalDokReferensi;
                worksheetReport.Cells[row, colBody++].Value = r.IdTkuPemotong;
                worksheetReport.Cells[row, colBody++].Value = r.TanggalPemotongan;

                worksheetReport.Cells[row, colBody++].Value = string.Empty;
                worksheetReport.Cells[row, colBody++].Value = string.Empty;
                worksheetReport.Cells[row, colBody++].Value = AppSystem.ReportPajakDefaultValue2;
                worksheetReport.Cells[row, colBody++].Value = AppSystem.ReportPajakDefaultValue3;
                worksheetReport.Cells[row, colBody++].Value = AppSystem.ReportPajakDefaultValue3;
                worksheetReport.Cells[row, colBody++].Value = AppSystem.ReportPajakDefaultValue3;
                worksheetReport.Cells[row, colBody++].Value = AppSystem.ReportPajakDefaultValue3;
                worksheetReport.Cells[row, colBody++].Value = AppSystem.ReportPajakDefaultValue3;
                worksheetReport.Cells[row, colBody++].Value = AppSystem.ReportPajakDefaultValue3;
                worksheetReport.Cells[row, colBody].Value = AppSystem.ReportPajakDefaultValue3;

                int totalColumns = colBody - 10;
                SetBackgroudColorBody(worksheetReport, row, totalColumns);

                row++;
            }
            #endregion
        }

        private static void ReportPajakPph23Stream(List<ReportPajakListResponse> inquiryPajak, ExcelWorksheet worksheetReport)
        {
            int row = 4;

            #region worksheet header
            SetDefaultReport(worksheetReport);

            int rowIndex = 3;
            int columnIndex = 1;
            string cellAddress = string.Empty;

            string[] columnHeaders = { "Masa Pajak", "Tahun Pajak", "NPWP", "ID TKU Penerima Penghasilan", "Fasilitas", "Kode Objek Pajak", "DPP",
                                       "Tarif", "Jenis Dok. Referensi", "Nomor Dok. Referensi", "Tanggal Dok. Referensi", "ID TKU Pemotong", "Opsi Pembayaran (IP)",
                                       "Nomor SP2D (IP)", "Tanggal Pemotongan", "", "", ""};

            for (int i = 0; i < columnHeaders.Length; i++)
            {
                string columnName = ReportHelper.GetExcelColumnName(columnIndex);
                cellAddress = columnName + rowIndex;
                // Set the value in the cell
                worksheetReport.Cells[cellAddress].Value = columnHeaders[i];

                if (i < 15)
                    SetBackgroundColorHeader(worksheetReport, cellAddress);


                columnIndex++;
            }

            #endregion

            #region worksheet body
            string cleanedInput;
            foreach (var r in inquiryPajak)
            {
                var colBody = 2;
                worksheetReport.Cells[row, colBody++].Value = r.MasaPajak;
                worksheetReport.Cells[row, colBody++].Value = r.TahunPajak;
                worksheetReport.Cells[row, colBody++].Value = r.Npwp;
                worksheetReport.Cells[row, colBody++].Value = r.IdTkuPenerimaPenghasilan;
                worksheetReport.Cells[row, colBody++].Value = r.Fasilitas;
                worksheetReport.Cells[row, colBody++].Value = string.Empty;

                cleanedInput = r.Jumlah.ReplaceCommaToEmpty();
                decimal jumlah = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                worksheetReport.Cells[row, colBody++].Value = jumlah;

                worksheetReport.Cells[row, colBody++].Value = Math.Round(Convert.ToDecimal(r.Tarif), 2);

                worksheetReport.Cells[row, colBody++].Value = r.JenisDokReferensi;
                worksheetReport.Cells[row, colBody++].Value = r.NomorDokReferensi;
                worksheetReport.Cells[row, colBody++].Value = r.TanggalDokReferensi;
                worksheetReport.Cells[row, colBody++].Value = r.IdTkuPemotong;
                worksheetReport.Cells[row, colBody++].Value = r.OpsiPembayaran;
                worksheetReport.Cells[row, colBody++].Value = r.NomorSp2d;
                worksheetReport.Cells[row, colBody++].Value = r.TanggalPemotongan;
                worksheetReport.Cells[row, colBody++].Value = string.Empty;
                worksheetReport.Cells[row, colBody++].Value = r.Formula;
                worksheetReport.Cells[row, colBody].Value = AppSystem.ReportPajakDefaultValue1;

                int totalColumns = colBody - 3;
                SetBackgroudColorBody(worksheetReport, row, totalColumns);

                row++;
            }
            #endregion
        }

        private static void ReportPajakPph26Stream(List<ReportPajakListResponse> inquiryPajak, ExcelWorksheet worksheetReport)
        {
            int row = 4;

            #region worksheet header
            SetDefaultReport(worksheetReport);

            int rowIndex = 3;
            int columnIndex = 1;
            string cellAddress = string.Empty;

            string[] columnHeaders = { "Masa Pajak", "Tahun Pajak", "NPWP/TIN", "Nama Penerima Penghasilan", "Kode Negara", "Alamat Penerima Penghasilan", "Tempat Lahir",
                                       "Tanggal Lahir", "No. Paspor", "No. Kitas", "Fasilitas", "Nomor Fasilitas", "Kode Objek Pajak",
                                       "Penghasilan", "Deemed", "Tarif", "Jenis Dok. Referensi", "Nomor Dok. Referensi", "Tanggal Dok. Referensi", "ID TKU Pemotong" , "Opsi Pembayaran (IP)",
                                       "Nomor SP2D (IP)", "Tanggal Pemotongan",};

            for (int i = 0; i < columnHeaders.Length; i++)
            {
                string columnName = ReportHelper.GetExcelColumnName(columnIndex);
                cellAddress = columnName + rowIndex;
                // Set the value in the cell
                worksheetReport.Cells[cellAddress].Value = columnHeaders[i];

                SetBackgroundColorHeader(worksheetReport, cellAddress);

                columnIndex++;
            }

            #endregion

            #region worksheet body
            string cleanedInput;
            foreach (var r in inquiryPajak)
            {
                var colBody = 2;
                worksheetReport.Cells[row, colBody++].Value = r.MasaPajak;
                worksheetReport.Cells[row, colBody++].Value = r.TahunPajak;
                worksheetReport.Cells[row, colBody++].Value = r.Npwp;
                worksheetReport.Cells[row, colBody++].Value = r.Nama;
                worksheetReport.Cells[row, colBody++].Value = r.KodeNegara;
                worksheetReport.Cells[row, colBody++].Value = r.Alamat;
                worksheetReport.Cells[row, colBody++].Value = r.TempatLahir;
                worksheetReport.Cells[row, colBody++].Value = r.TanggalLahir;
                worksheetReport.Cells[row, colBody++].Value = r.NoPaspor;
                worksheetReport.Cells[row, colBody++].Value = r.NoKitas;
                worksheetReport.Cells[row, colBody++].Value = r.Fasilitas;
                worksheetReport.Cells[row, colBody++].Value = r.NoFasilitas;
                worksheetReport.Cells[row, colBody++].Value = r.KodeObjekPajak;

                cleanedInput = r.Penghasilan.ReplaceCommaToEmpty();
                decimal penghasilan = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                worksheetReport.Cells[row, colBody++].Value = penghasilan;
                worksheetReport.Cells[row, colBody++].Value = string.Empty;
                worksheetReport.Cells[row, colBody++].Value = Math.Round(Convert.ToDecimal(r.Tarif), 2);

                worksheetReport.Cells[row, colBody++].Value = r.JenisDokReferensi;
                worksheetReport.Cells[row, colBody++].Value = r.NomorDokReferensi;
                worksheetReport.Cells[row, colBody++].Value = r.TanggalDokReferensi;
                worksheetReport.Cells[row, colBody++].Value = r.IdTkuPemotong;
                worksheetReport.Cells[row, colBody++].Value = r.OpsiPembayaran;
                worksheetReport.Cells[row, colBody++].Value = r.NomorSp2d;
                worksheetReport.Cells[row, colBody].Value = r.TanggalPemotongan;

                int totalColumns = colBody;
                SetBackgroudColorBody(worksheetReport, row, totalColumns);

                row++;
            }
            #endregion
        }

        private static void SetBackgroundColorHeader(ExcelWorksheet worksheetReport, string cellAddress)
        {
            // Set background color
            worksheetReport.Cells[cellAddress].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheetReport.Cells[cellAddress].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(91, 155, 213));
            // Set font color
            worksheetReport.Cells[cellAddress].Style.Font.Color.SetColor(Color.White);
            worksheetReport.Cells[cellAddress].Style.Font.Bold = true;
        }

        private static void SetDefaultReport(ExcelWorksheet worksheetReport)
        {
            worksheetReport.Cells["A1:B1"].Merge = true;
            worksheetReport.Cells["A1"].Value = AppSystem.ReportPajakNpwpPemotong;
            worksheetReport.Cells["C1"].Value = AppSystem.ReportPajakDefaultNpwp;
        }

        private static void SetBackgroudColorBody(ExcelWorksheet worksheetReport, int row, int totalColumns)
        {
            if (row % 2 != 0)
            {
                worksheetReport.Cells[row, 2, row, totalColumns].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheetReport.Cells[row, 2, row, totalColumns].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(217, 225, 242));

            }
        }

        /// <summary>
        /// Export Excel Report Inquiry Budget
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<MemoryStream> ExportInquiryBudgetToExcel(ParamGetRequestList param)
        {
            param.IsExport = true;
            var res = await GetReportBudgetTransactions(param);
            var stream = new MemoryStream();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var xlPackage = new ExcelPackage(stream))
            {
                var worksheetReportBudget = xlPackage.Workbook.Worksheets.Add(AppSystem.WorksheetNameInquiryBudget);
                var row = 7;

                #region worksheet report budget
                worksheetReportBudget.Cells["A1"].Value = "Budget Detail";
                worksheetReportBudget.Cells["A2"].Value = "APS Revamp";
                var statusSubCategory = await _subCategoryRepository.GetSubCategory(int.Parse((param.Status ?? 0.ToString()).Replace("all", 0.ToString())));
                worksheetReportBudget.Cells["A3"].Value = $"{statusSubCategory?.SubCategoryName ?? "All"} Budget";
                if (!string.IsNullOrEmpty(param.RequestDateFrom) && !string.IsNullOrEmpty(param.RequestDateTo))
                    worksheetReportBudget.Cells["A4"].Value = $"Budget From {param.RequestDateFrom ?? string.Empty} - To {param.RequestDateTo ?? string.Empty}";

                worksheetReportBudget.Cells["A5"].Value = "No";
                worksheetReportBudget.Cells["A5:A6"].Merge = true;
                worksheetReportBudget.Cells["B5"].Value = "Account Code";
                worksheetReportBudget.Cells["B5:B6"].Merge = true;
                worksheetReportBudget.Cells["C5"].Value = "Account Name";
                worksheetReportBudget.Cells["C5:C6"].Merge = true;
                worksheetReportBudget.Cells["D5"].Value = "Business Unit Code";
                worksheetReportBudget.Cells["D5:D6"].Merge = true;
                worksheetReportBudget.Cells["E5"].Value = "Business Unit Name";
                worksheetReportBudget.Cells["E5:E6"].Merge = true;
                worksheetReportBudget.Cells["F5"].Value = "Cost Center Code";
                worksheetReportBudget.Cells["F5:F6"].Merge = true;
                worksheetReportBudget.Cells["G5"].Value = "Cost Center Name";
                worksheetReportBudget.Cells["G5:G6"].Merge = true;
                worksheetReportBudget.Cells["H5"].Value = "Currency";
                worksheetReportBudget.Cells["H5:H6"].Merge = true;
                worksheetReportBudget.Cells["I5"].Value = "Rate";
                worksheetReportBudget.Cells["I5:I6"].Merge = true;
                worksheetReportBudget.Cells["J5"].Value = "Nett Amount";
                worksheetReportBudget.Cells["J5:J6"].Merge = true;
                worksheetReportBudget.Cells["K5"].Value = "Status Payment";
                worksheetReportBudget.Cells["K5:K6"].Merge = true;
                worksheetReportBudget.Cells["L5"].Value = "Received By Finance";
                worksheetReportBudget.Cells["L5:L6"].Merge = true;
                worksheetReportBudget.Cells["M5"].Value = "Paid By Finance";
                worksheetReportBudget.Cells["M5:M6"].Merge = true;
                worksheetReportBudget.Cells["N5"].Value = "Transaction Number (MCM Number)";
                worksheetReportBudget.Cells["N5:N6"].Merge = true;
                worksheetReportBudget.Cells["O5"].Value = "Request Number";
                worksheetReportBudget.Cells["O5:O6"].Merge = true;
                worksheetReportBudget.Cells["P5"].Value = "Category";
                worksheetReportBudget.Cells["P5:P6"].Merge = true;
                worksheetReportBudget.Cells["Q5"].Value = "VENDOR / STAFF / OTHER";
                worksheetReportBudget.Cells["Q5:R5"].Merge = true;
                worksheetReportBudget.Cells["Q6"].Value = "Code";
                worksheetReportBudget.Cells["R6"].Value = "Name";
                worksheetReportBudget.Cells["S5"].Value = "Request Type";
                worksheetReportBudget.Cells["S5:S6"].Merge = true;
                worksheetReportBudget.Cells["T5"].Value = "Invoice Detail No.";
                worksheetReportBudget.Cells["T5:T6"].Merge = true;
                worksheetReportBudget.Cells["U5"].Value = "Description";
                worksheetReportBudget.Cells["U5:U6"].Merge = true;
                worksheetReportBudget.Cells["V5"].Value = "Budget";
                worksheetReportBudget.Cells["V5:V6"].Merge = true;

                string cleanedInput;
                foreach (var r in res)
                {
                    var colBody = 1;
                    worksheetReportBudget.Cells[row, colBody++].Value = row - 6;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.AccountMasterCode;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.AccountMasterName;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.BusinessUnitCode;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.BusinessUnitName;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.CostCenterCode;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.CostCenterName;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.LCurrencyCode;

                    worksheetReportBudget.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                    cleanedInput = r.Rate.ReplaceCommaToEmpty();
                    decimal rate = decimal.Parse(cleanedInput, NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                    worksheetReportBudget.Cells[row, colBody++].Value = rate;

                    worksheetReportBudget.Cells[row, colBody].Style.Numberformat.Format = AppSystem.NumberFormatMoneyIdr;
                    cleanedInput = r.NettAmountCurrency.ReplaceCommaToEmpty();
                    decimal nettAmount = decimal.Parse(cleanedInput, NumberStyles.AllowLeadingSign | NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                    worksheetReportBudget.Cells[row, colBody++].Value = nettAmount;

                    worksheetReportBudget.Cells[row, colBody++].Value = r.StatusTransferDesc;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.ReceivedByFinance;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.PaidByFinance;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.TransferNumber;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.RequestNumber;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.VendorType;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.VendorCode;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.VendorName;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.RequestType;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.InvoiceNo;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.Description;
                    worksheetReportBudget.Cells[row, colBody++].Value = r.IsBudget ? "Yes" : "No";

                    int columnIndex = colBody - 1;
                    string cellAddress = string.Empty, cellAddress2 = string.Empty;
                    int rowIndex = 5;
                    string columnName = string.Empty;

                    for (int i = 0; i < r.ApprovalGroupMemberBudgetResponses.Count; i++)
                    {
                        columnName = ReportHelper.GetExcelColumnName(columnIndex);
                        cellAddress = columnName + rowIndex;

                        columnName = ReportHelper.GetExcelColumnName(columnIndex);
                        cellAddress2 = columnName + (rowIndex + 1);
                        // Set the value in the cell
                        worksheetReportBudget.Cells[cellAddress].Value = $"Approval {i + 1}";
                        worksheetReportBudget.Cells[$"{cellAddress}:{cellAddress2}"].Merge = true;
                        worksheetReportBudget.Cells[row, colBody].Value = r.ApprovalGroupMemberBudgetResponses[i].UserName;
                        colBody++;
                        columnIndex++;

                        columnName = ReportHelper.GetExcelColumnName(columnIndex);
                        cellAddress = columnName + rowIndex;

                        columnName = ReportHelper.GetExcelColumnName(columnIndex);
                        cellAddress2 = columnName + (rowIndex + 1);
                        // Set the value in the cell
                        worksheetReportBudget.Cells[cellAddress].Value = $"Reason Approval {i + 1}";
                        worksheetReportBudget.Cells[$"{cellAddress}:{cellAddress2}"].Merge = true;
                        string flagNoBudget = !r.ApprovalGroupMemberBudgetResponses[i].IsApprovalBudget ? "[No Budget] - " : string.Empty;
                        worksheetReportBudget.Cells[row, colBody].Value = $"{flagNoBudget}{r.ApprovalGroupMemberBudgetResponses[i].Comment}";
                        colBody++;
                        columnIndex++;
                    }
                    if (!string.IsNullOrWhiteSpace(cellAddress2))
                        worksheetReportBudget.Cells[$"A5:{cellAddress2}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    if (!string.IsNullOrWhiteSpace(cellAddress2))
                        worksheetReportBudget.Cells[$"A5:{cellAddress2}"].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);

                    row++;
                }
                #endregion

                await xlPackage.SaveAsync();
            }

            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Get Report PO Shopping Cart Export
        /// </summary>
        /// <returns></returns>
        public async Task<List<DueDiligenceReportResponseModel>> DueDiligenceReportJsonPost(DueDiligenceReportRequestModel request)
        {
            try
            {
                var q = @$"
select
json_query((

	select '' as x
	
	, count(1) over()	as 'Count'
	
	, json_query((

		select '' as x
		, v.Id
		, v.Name
		, v.Address
		, v.eMail

		, v.SupplierType_SubCategoryId
		, json_query((
			select '' as x
			, sc.Id
			, sc.SubCategoryName
			for json path , include_null_values , without_array_wrapper
		)) as 'SupplierType_SubCategory'

		, json_query((
			select '' as x
			, vdd.Id

			, coalesce(vdd.Status, 0) as Status
			, json_query((
				select '' as x
				, mt.Id
				, mt.Name
				for json path , include_null_values , without_array_wrapper
			)) as 'StatusMasterTable'

			, convert(varchar, vdd.PeriodValidFrom, 23) as PeriodValidFromDateText
			, convert(varchar, vdd.PeriodValidTo, 23) as PeriodValidToDateText
			, convert(varchar, vdd.CreatedTime, 20) as CreatedTimeText
			, convert(varchar, vdd.LastUpdatedTime, 20) as LastUpdatedTimeText

			, json_query((
				select '' as x
				, vsdd.Id
				, vsdd.PepResult
				for json path , include_null_values , without_array_wrapper
			)) as 'VendorSimpleDueDiligence'

			, vdd.OrderNumberText

			for json path , include_null_values , without_array_wrapper
		)) as 'VendorDueDeligence'

		for json path , include_null_values , without_array_wrapper
	)) as 'Vendor'


	from Vendor as v
	left join SubCategory as sc
	on sc.Id = v.SupplierType_SubCategoryId

	inner join (
		select distinct
		vddr.VendorId
		from VendorDueDiligenceRequest as vddr
	) as vddr
	on vddr.VendorId = v.Id

	left join (
		select '' as x
		, vdd.Id
		, vdd.VendorId
		, vdd.Status
		, vdd.PeriodValidFrom
		, vdd.PeriodValidTo
		, vdd.CreatedTime
		, vdd.LastUpdatedTime

		, (
			case
			when vdd.Id is not null
			then cast(row_number() over(partition by vdd.VendorId order by vdd.Id asc) as varchar(11))
			else ''
			end
		) as OrderNumberText

		from VendorDueDeligence as vdd
	) as vdd
	on vdd.VendorId = v.Id

	left join MasterTable as mt
	on mt.Category = 'VendorDueDiligence.Status'
	and mt.ValueId = coalesce(vdd.Status, 0)

	left join VendorSimpleDueDiligence as vsdd
	on vsdd.VendorDueDiligenceId = vdd.Id

	where 1 = 1

	{(request.Vendor.Id is null ? "--" : "")}and v.Id = @VendorId
	{(request.Vendor.SupplierType_SubCategoryId is null ? "--" : "")}and v.SupplierType_SubCategoryId = @SupplierType_SubCategoryId
	{(request.Vendor.VendorDueDeligence.Status is null ? "--" : "")}and coalesce(vdd.Status, 0) = @VendorDueDeligenceStatus
	
	{(request.Vendor.VendorDueDeligence.PeriodValidFromBeginEnd.Begin is null ? "--" : "")}and cast(vdd.PeriodValidFrom as date) >= @PeriodValidFromBegin
	{(request.Vendor.VendorDueDeligence.PeriodValidFromBeginEnd.End is null ? "--" : "")}and cast(vdd.PeriodValidFrom as date) <= @PeriodValidFromEnd

	{(request.Vendor.VendorDueDeligence.PeriodValidToBeginEnd.Begin is null ? "--" : "")}and cast(vdd.PeriodValidTo as date) >= @PeriodValidToBegin
	{(request.Vendor.VendorDueDeligence.PeriodValidToBeginEnd.End is null ? "--" : "")}and cast(vdd.PeriodValidTo as date) <= @PeriodValidToEnd

	{(request.Vendor.VendorDueDeligence.CreatedTimeBeginEnd.Begin is null ? "--" : "")}and cast(vdd.CreatedTime as date) >= @CreatedTimeBegin
	{(request.Vendor.VendorDueDeligence.CreatedTimeBeginEnd.End is null ? "--" : "")}and cast(vdd.CreatedTime as date) <= @CreatedTimeEnd

	{(request.Vendor.VendorDueDeligence.LastUpdatedTimeBeginEnd.Begin is null ? "--" : "")}and cast(vdd.LastUpdatedTime as date) >= @LastUpdatedTimeBegin
	{(request.Vendor.VendorDueDeligence.LastUpdatedTimeBeginEnd.End is null ? "--" : "")}and cast(vdd.LastUpdatedTime as date) <= @LastUpdatedTimeEnd

	{(string.IsNullOrWhiteSpace(request.Vendor.VendorDueDeligence.VendorSimpleDueDiligence.PepResult) ? "--" : "")}and vsdd.PepResult = @PepResult

	{(string.IsNullOrWhiteSpace(request.Vendor.VendorDueDeligence.OrderNumberText) ? "--" : "")}and vdd.OrderNumberText = @OrderNumberText

	order by
	v.Name asc
	, vdd.Id asc

	{(request.Length == 0 ? "--" : "")}offset @Start rows
	{(request.Length == 0 ? "--" : "")}fetch next @Length rows only

	for json path , include_null_values
)) as 'DueDiligenceReportView'
";
                Dapper.DynamicParameters p = new(new
                {
                    request.Start,
                    request.Length,
                    VendorId = request.Vendor.Id,
                    request.Vendor.SupplierType_SubCategoryId,
                    VendorDueDeligenceStatus = request.Vendor.VendorDueDeligence.Status,
                    PeriodValidFromBegin = request.Vendor.VendorDueDeligence.PeriodValidFromBeginEnd.Begin.ToString(),
                    PeriodValidFromEnd = request.Vendor.VendorDueDeligence.PeriodValidFromBeginEnd.End.ToString(),
                    PeriodValidToBegin = request.Vendor.VendorDueDeligence.PeriodValidToBeginEnd.Begin.ToString(),
                    PeriodValidToEnd = request.Vendor.VendorDueDeligence.PeriodValidToBeginEnd.End.ToString(),
                    CreatedTimeBegin = request.Vendor.VendorDueDeligence.CreatedTimeBeginEnd.Begin.ToString(),
                    CreatedTimeEnd = request.Vendor.VendorDueDeligence.CreatedTimeBeginEnd.End.ToString(),
                    LastUpdatedTimeBegin = request.Vendor.VendorDueDeligence.LastUpdatedTimeBeginEnd.Begin.ToString(),
                    LastUpdatedTimeEnd = request.Vendor.VendorDueDeligence.LastUpdatedTimeBeginEnd.End.ToString(),
                    request.Vendor.VendorDueDeligence.OrderNumberText,
                    request.Vendor.VendorDueDeligence.VendorSimpleDueDiligence.PepResult
                });
                var json = await Task.FromResult(_dapper.Get<string>(q, p));

                var r = JsonConvert.DeserializeObject<List<DueDiligenceReportResponseModel>>(json);
                return r;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }

        public async Task<IEnumerable<Models.Report.Procurement.DataTables.Data>> ProcurementDataTablesData(Models.Report.Procurement.DataTables.Request request)
        {
            try
            {
                string q = string.Empty;
                q = $@"

declare @money_culture nvarchar(max) = 'en' ;
--set @money_culture = 'id' ;

declare @date_time_style int = 23 ;
--set @date_time_style = 20 ;

with
x as (select 1 as x)

, w_pr as (
	select
	cp_sc.Id as _CategoryProcess_SubCategoryId
	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
	, tp_sc.Id as _TypeProcess_SubCategoryId
	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName
	, prid.Id as _PurchaseRequestItemDetailId
	, pr.Id as _Id
	, prid.Id as _DetailId
	, pr.RequestCode as 'PR_No'
	, mt.Name as 'PR_Status'
	, convert(varchar,pr.RequestDates,@date_time_style) as 'PR_Date'
	, Requestor_ua.Username as 'Requester'
	, (Requestor_ua_cc.Code + ' - ' + Requestor_ua_cc.Name) as 'Department'
	, '' as 'Type_Of_Transaction'
	, '' as 'Buyer_User_Name'
	, '' as 'Total_Budget_Estimation'
	, '' as 'Critical'
	, '' as 'Category'
	, (i.ItemCode + ' - ' + i.Name) as 'Item_Name'
	, (am.AccountCode + ' - ' + am.Description) as 'Account_Code'
	, (cc.Code + ' - ' + cc.Name) as 'Cost_Center'
	, (v.Code + ' - ' + v.Name) as 'Vendor_Selection'
	, i.L_Currency_Code as 'Currency'
	, convert(varchar,pr.CreatedTime,@date_time_style) as 'PR_Posted_Date'
	, (SELECT TOP 1 convert(varchar,pod.DeliveryRequestDate,@date_time_style) FROM PurchaseOrderDetail pod WHERE pod.PurchaseOrderId = prtopo.PurchaseOrderId) as 'Delivery_Request_Date'
	, '' as 'Final_Spec_Req_Date'
	, '' as 'Generate_Proc_Sum_Date'
	, '' as 'TAT_WD'
	, '' as 'SLA_WD'
	, '' as 'SLA_Status'
	, (v.Code + ' - ' + v.Name) as 'Vendor'
	, '' as 'Selected'
	, '' as 'Selected_Vendors_Total_Budget'
	, '' as 'Realised_Saving'

	from PurchaseRequest as pr
	inner join Flips.UserAccount as Requestor_ua on Requestor_ua.Id = pr.RequestorAccountId
	inner join CostCenter as Requestor_ua_cc on Requestor_ua_cc.Id = Requestor_ua.CostCenterId
	{(request.Department_Id is null ? "--" : "")}and Requestor_ua_cc.Id = @Department_Id
	inner join MasterTable as mt on mt.Category = 'PurchaseRequest.Status' and mt.ValueId = pr.Status
	{(request.PR_Status_ValueId is null ? "--" : "")}and mt.ValueId = @PR_Status_ValueId
	inner join PurchaseRequestItemDetail as prid on prid.PurchaseRequestId = pr.Id
	inner join Item as i on i.Id = prid.ItemId
	inner join Vendor as v on v.Id = i.VendorId
	{(request.Vendor_Id is null ? "--" : "")}and v.Id = @Vendor_Id
	inner join AccountMaster as am on am.Id = prid.AccountMasterId
	{(request.Account_Code_Id is null ? "--" : "")}and am.Id = @Account_Code_Id
	inner join PurchaseRequestItemCostCenter as pricc on pricc.PurchaseRequestItemDetailId = prid.Id
	inner join CostCenter as cc on cc.Id = pricc.CostCenterId
	{(request.Cost_Center_Id is null ? "--" : "")}and cc.Id = @Cost_Center_Id
	-- Shopping Cart
	inner join SubCategory as cp_sc on cp_sc.SubCategoryCode = 'SC-2024-02-01261'
	{(request.PR_Category_Id is null ? "--" : "")}and cp_sc.Id = @PR_Category_Id
	-- Purchase Order
	inner join SubCategory as tp_sc on tp_sc.SubCategoryCode = 'SC-2023-08-11132'
	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id
    left join PurchaseOrderToPurchaseRequest prtopo on pr.Id = prtopo.PurchaseRequestlId
	where 1 = 1
	{(string.IsNullOrWhiteSpace(request.PR_No) ? "--" : "")}and pr.RequestCode like @PR_No
	{(request.PR_Date_Begin is null ? "--" : "")}and @PR_Date_Begin <= cast(pr.RequestDates as date)
	{(request.PR_Date_End is null ? "--" : "")}and cast(pr.RequestDates as date) <= @PR_Date_End

	union all

	select
	cp_sc.Id as _CategoryProcess_SubCategoryId
	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
	, tp_sc.Id as _TypeProcess_SubCategoryId
	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName
	, psd.Id as _PurchaseRequestItemDetailId
	, p.Id as _Id
	, pd.Id as _DetailId
	, p.PRFNo as 'PR_No'
	, mt.Name as 'PR_Status'
	, convert(varchar,cast(p.RequestDate as date),@date_time_style) as 'PR_Date'
	, ua.Username as 'Requester'
	, (ua_cc.Code + ' - ' + ua_cc.Name) as 'Department'
	, p.TypeOfTransaction as 'Type_Of_Transaction'
	, b_ua.Username as 'Buyer_User_Name'
	, format(p.TotalBudgetEstimation,'n',@money_culture) as 'Total_Budget_Estimation'
	, (case coalesce(p.IsRiskAssementForm,0) when 1 then 'Yes' else 'No' end) as 'Critical'
	, s_c.Category as 'Category'
	, pd.RequestItemName as 'Item_Name'
	, (am.AccountCode + ' - ' + am.Description) as 'Account_Code'
	, (cc.Code + ' - ' + cc.Name) as 'Cost_Center'
	, (v.Code + ' - ' + v.Name) as 'Vendor_Selection'
	, p.L_Currency_Code as 'Currency'
	, convert(varchar , p.CreatedTime , @date_time_style) as 'PR_Posted_Date'
	, convert(varchar , pd.DeliveryRequestDate , @date_time_style) as 'Delivery_Request_Date'
	, convert(varchar , psc.CreatedTime , @date_time_style) as 'Final_Spec_Req_Date'
	, convert(varchar , ps.PRFSummaryDate , @date_time_style) as 'Generate_Proc_Sum_Date'
	, (
		(DATEDIFF(dd , psc.CreatedTime , ps.PRFSummaryDate) + 1)
		  -(DATEDIFF(wk , psc.CreatedTime , ps.PRFSummaryDate) * 2)
		  -(CASE WHEN DATENAME(dw , psc.CreatedTime) = 'Sunday' THEN 1 ELSE 0 END)
		  -(CASE WHEN DATENAME(dw , ps.PRFSummaryDate) = 'Saturday' THEN 1 ELSE 0 END)
		  -(SELECT COUNT(*) FROM MasterHoliday WHERE CAST(DateHoliday as date) BETWEEN CAST(psc.CreatedTime as date) AND CAST(ps.PRFSummaryDate as date))
	  ) as 'TAT_WD' --TAT (WD) Berapa lama proses dari final spec s/d generate ProcSum
	, '5' as 'SLA_WD'
	, (CASE 
		WHEN (
				(DATEDIFF(dd , psc.CreatedTime , ps.PRFSummaryDate) + 1)
			   -(DATEDIFF(wk , psc.CreatedTime , ps.PRFSummaryDate) * 2)
			   -(CASE WHEN DATENAME(dw , psc.CreatedTime) = 'Sunday' THEN 1 ELSE 0 END)
			   -(CASE WHEN DATENAME(dw , ps.PRFSummaryDate) = 'Saturday' THEN 1 ELSE 0 END)
			   -(SELECT COUNT(*) FROM MasterHoliday WHERE CAST(DateHoliday as date) BETWEEN CAST(psc.CreatedTime as date) AND CAST(ps.PRFSummaryDate as date))
		     ) <= 5
		THEN 'Meet'
		ELSE 'Not Meet'
	  END
	  ) as 'SLA_Status' --Status SLA: meet / not meet.Meet: 1-5 Hari Not Meet: diatas 5 hari kerja
	, (v.Code + ' - ' + v.Name) as 'Vendor'
	, (case coalesce(psd.IsSelected,0) when 1 then 'Yes' else '' end) as 'Selected'
	, format(psd.TotalBaseAmmount,'n',@money_culture) as 'Selected_Vendors_Total_Budget'

	, (
	case
	when (psd.TotalBaseAmmount is null) then ''
	when (psd.TotalBaseAmmount > p.TotalBudgetEstimation) then '0'
	else format((p.TotalBudgetEstimation - psd.TotalBaseAmmount),'n',@money_culture)
	end
	) as 'Realised_Saving'

	from PRF as p
	-- Non Shopping Cart
	inner join SubCategory as cp_sc on cp_sc.SubCategoryCode = 'SC-2024-02-01262'
	{(request.PR_Category_Id is null ? "--" : "")}and cp_sc.Id = @PR_Category_Id
	left join SubCategory as tp_sc on tp_sc.Id = p.TypeProcess_SubCategory
	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id
	inner join Flips.UserAccount as ua on ua.Id = p.RequestorAccountId
	inner join CostCenter as ua_cc on ua_cc.Id = ua.CostCenterId
	{(request.Department_Id is null ? "--" : "")}and ua_cc.Id = @Department_Id
	left join Flips.UserAccount as b_ua on b_ua.Id = p.BuyerAccountId
	inner join MasterTable as mt on mt.Category = 'PRF.Status' and mt.ValueId = p.Status
	{(request.PR_Status_ValueId is null ? "--" : "")}and mt.ValueId = @PR_Status_ValueId
	inner join AccountMaster as am on am.AccountCode = p.BudgetCode
	{(request.Account_Code_Id is null ? "--" : "")}and am.Id = @Account_Code_Id
	inner join CostCenter as cc on cc.Id = p.CostCenterId
	{(request.Cost_Center_Id is null ? "--" : "")}and cc.Id = @Cost_Center_Id
	inner join Spending_Category as s_c on s_c.Id = p.Spending_Category
	inner join (
		select
		psc.PRFId
		, max(psc.Id) as LastPRFSpendingCategoryId
		from PRFSpendingCategory as psc
		where psc.Status = 1
		group by psc.PRFId
	) as last_psc on last_psc.PRFId = p.id
	inner join PRFSpendingCategory as psc on psc.Id = last_psc.LastPRFSpendingCategoryId
	inner join PRFDetail as pd on pd.PRFId = p.Id

	left join PRFVendorQuotation as pvq on pvq.PRFId = p.Id
	left join PRFVendorQuotationDetail as pvqd on pvqd.PRFVendorQuotationId = pvq.Id and pvqd.PRFDetailId = pd.Id and pvqd.Status = 1 and pvqd.IsSelected = 1
	left join Vendor as v on v.Id = pvqd.VendorId
	{(request.Vendor_Id is null ? "--" : "")}and v.Id = @Vendor_Id

	left join (
		select
		ps.PRFId
		, max(ps.Id) as LastPRFSummaryId
		from PRFSummary as ps
		group by ps.PRFId
	) as last_ps on last_ps.PRFId = p.Id
	left join PRFSummary as ps on ps.Id = last_ps.LastPRFSummaryId
	left join PRFSummaryDetail as psd on psd.PRFSummaryId = ps.Id and psd.PRFVendorQuotationDetailId = pvqd.Id and psd.IsSelected = 1

	where 1 = 1
	{(string.IsNullOrWhiteSpace(request.PR_No) ? "--" : "")}and p.PRFNo like @PR_No
	{(request.PR_Date_Begin is null ? "--" : "")}and @PR_Date_Begin <= cast(p.RequestDate as date)
	{(request.PR_Date_End is null ? "--" : "")}and cast(p.RequestDate as date) <= @PR_Date_End
)

, w_argm as (
	select
	argm.ApprovalRequestId as _ApprovalRequestId
	, (convert(varchar,coalesce(argm.ApprovalDate, argm.CancelDate, argm.RejectionDate),@date_time_style) + ' - ' + ua.Username) as 'Approver_Order_And_Date'
	from ApprovalRequestGroupMember as argm
	inner join (
		select
		argm.ApprovalRequestId
		, max(argm.Id) as Id
		from ApprovalRequestGroupMember as argm
		where (argm.ApprovalDate is not null or argm.CancelDate is not null or argm.RejectionDate is not null)
		group by argm.ApprovalRequestId
	) as current_argm on current_argm.Id = argm.Id
	inner join Flips.UserAccount as ua on ua.Id = argm.AccountId
)

-- w_type_process = po / pons / gl / (pap)
, w_type_process as (
	select
	cp_sc.Id as _CategoryProcess_SubCategoryId
	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
	, tp_sc.Id as _TypeProcess_SubCategoryId
	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName
	, pod.PurchaseRequestItemDetailId as _PurchaseRequestItemDetailId
	, pod.Id as _PurchaseOrderDetailId
	, pot.Id as _PurchaseOrderTOPId
	, po.Id as _Id
	, pod.Id as _DetailId
	, tp_sc.SubCategoryName as 'Order_Type'
	, po.PONumber as 'Order_No'
	, mt.Name as 'Order_Status'
	, convert(varchar,po.PoDate,@date_time_style) as 'Order_Date'
	, format(pod.TotalAmount,'n',@money_culture) as 'Order_Grand_Total_Amount'
	, (convert(varchar,po.ApproverDate,@date_time_style) + ' - ' + ua.Username) as 'Approver_Order_And_Date'
	from PurchaseOrder as po
	inner join Flips.UserAccount as ua on ua.Id = po.ApproverAccountId
	inner join MasterTable as mt on mt.Category = 'PurchaseOrder.Status' and mt.ValueId = po.Status
	{(request.Order_Status_ValueId is null ? "--" : "")}and mt.ValueId = @Order_Status_ValueId
	inner join PurchaseOrderTOP as pot on pot.PurchaseOrderId = po.Id
	inner join PurchaseOrderDetail as pod on pod.PurchaseOrderId = po.Id
	inner join SubCategory as cp_sc on cp_sc.SubCategoryCode = 'SC-2024-02-01261' -- Shopping Cart
	inner join SubCategory as tp_sc on tp_sc.SubCategoryCode = 'SC-2023-08-11132' -- Purchase Order
	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id
	where 1 = 1
	{(string.IsNullOrWhiteSpace(request.Order_No) ? "--" : "")}and po.PONumber like @Order_No
	{(request.Order_Date_Begin is null ? "--" : "")}and @Order_Date_Begin <= cast(po.PoDate as date)
	{(request.Order_Date_End is null ? "--" : "")}and cast(po.PoDate as date) <= @Order_Date_End

	union all

	select
	cp_sc.Id as _CategoryProcess_SubCategoryId
	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
	, tp_sc.Id as _TypeProcess_SubCategoryId
	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName
	, ponsd.PRFSummaryDetailId as _PurchaseRequestItemDetailId
	, ponsd.Id as _PurchaseOrderDetailId
	, ponst.Id as _PurchaseOrderTOPId
	, pons.Id as _Id
	, ponsd.Id as _DetailId
	, tp_sc.SubCategoryName as 'Order_Type'
	, pons.PONumber as 'Order_No'
	, mt.Name as 'Order_Status'
	, convert(varchar,pons.PoDate,@date_time_style) as 'Order_Date'
	, format(ponsd.TotalAmount,'n',@money_culture) as 'Order_Grand_Total_Amount'
	, (convert(varchar,pons.ApproverDate,@date_time_style) + ' - ' + ua.Username) as 'Approver_Order_And_Date'
	from PONonShopping as pons
	inner join Flips.UserAccount as ua on ua.Id = pons.ApproverAccountId
	inner join MasterTable as mt on mt.Category = 'PurchaseOrder.Status' and mt.ValueId = pons.Status
	{(request.Order_Status_ValueId is null ? "--" : "")}and mt.ValueId = @Order_Status_ValueId
	inner join PONonShoppingTOP as ponst on ponst.PONonShoppingId = pons.Id
	inner join PONonShoppingDetail as ponsd on ponsd.PONonShoppingId = pons.Id
	inner join SubCategory as cp_sc on cp_sc.SubCategoryCode = 'SC-2024-02-01262' -- Non Shopping Cart
	inner join SubCategory as tp_sc on tp_sc.SubCategoryCode = 'SC-2023-08-11132' -- Purchase Order
	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id
	where 1 = 1
	{(string.IsNullOrWhiteSpace(request.Order_No) ? "--" : "")}and pons.PONumber like @Order_No
	{(request.Order_Date_Begin is null ? "--" : "")}and @Order_Date_Begin <= cast(pons.PoDate as date)
	{(request.Order_Date_End is null ? "--" : "")}and cast(pons.PoDate as date) <= @Order_Date_End

	union all

	select
	cp_sc.Id as _CategoryProcess_SubCategoryId
	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
	, tp_sc.Id as _TypeProcess_SubCategoryId
	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName
	, gld.PRFSummaryDetailId as _PurchaseRequestItemDetailId
	, null as _PurchaseOrderDetailId
	, null as _PurchaseOrderTOPId
	, gl.Id as _Id
	, gld.Id as _DetailId
	, tp_sc.SubCategoryName as 'Order_Type'
	, gl.GLNumber as 'Order_No'
	, mt.Name as 'Order_Status'
	, convert(varchar,gl.GLDate,@date_time_style) as 'Order_Date'
	, format(gld.TotalBaseAmount,'n',@money_culture) as 'Order_Grand_Total_Amount'
	, argm.Approver_Order_And_Date
	from GuaranteeLetter as gl
	inner join MasterTable as mt on mt.Category = 'GuaranteeLetter.Status' and mt.ValueId = gl.Status
	{(request.Order_Status_ValueId is null ? "--" : "")}and mt.ValueId = @Order_Status_ValueId
	inner join GuaranteeLetterDetail as gld on gld.GuaranteeLetterId = gl.Id
	inner join SubCategory as cp_sc on cp_sc.SubCategoryCode = 'SC-2024-02-01262' -- Non Shopping Cart
	inner join SubCategory as tp_sc on tp_sc.SubCategoryCode = 'SC-2023-08-11133' -- GL
	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id
	inner join w_argm as argm on argm._ApprovalRequestId = gl.ApprovalRequestId
	where 1 = 1
	{(string.IsNullOrWhiteSpace(request.Order_No) ? "--" : "")}and gl.GLNumber like @Order_No
	{(request.Order_Date_Begin is null ? "--" : "")}and @Order_Date_Begin <= cast(gl.GLDate as date)
	{(request.Order_Date_End is null ? "--" : "")}and cast(gl.GLDate as date) <= @Order_Date_End
)

, w_dn as (
	select
	dn.CategoryProcess_SubCategoryId as _CategoryProcess_SubCategoryId
	, dnd.PurchaseOrderDetailId as _PurchaseOrderDetailId
	, dnp.PurchaseOrderTOPId as _PurchaseOrderTOPId
	, dn.Id as _Id
	, dnd.Id as _DetailId
	, dn.DeliveryNumber as 'DN_No'
	, mt.Name as 'DN_Status'
	, convert(varchar,dnd.ReceivedDate,@date_time_style) as 'DN_Date'
	, cast(dnd.QtyReceive as varchar) as 'DN_Qty'
	from DeliveryNotesDetail as dnd
	inner join MasterTable as mt on mt.Category = 'DeliveryNote.Status' and mt.ValueId = dnd.Status
	inner join DeliveryNotesPayment as dnp on dnp.Id = dnd.DeliveryNotesPaymentId
	--and dnp.PurchaseOrderTOPId -- ignore ?
	inner join DeliveryNotes as dn on dn.Id = dnp.DeliveryNotesId
)

, w_ipo as (
	select
	ipo.CategoryProcess_SubCategoryId as _CategoryProcess_SubCategoryId
	, ipo.PurchaseOrderTOPId as _PurchaseOrderTOPId
	, ipo.Id as _Id
	, ipo.InvoiceNumber as 'Invoice_No'
	, mt.Name as 'Invoice_Status'
	, convert(varchar,ipo.InvoiceDate,@date_time_style) as 'Invoice_Date'
	, format(ipo.InvoiceAmmount,'n',@money_culture) as 'Invoice_Amount'
	, format(ipoioc.PPN,'n',@money_culture) as 'PPn'
	, format(ipoioc.PPH23,'n',@money_culture) as 'PPh_23'
	, format(ipoioc.PPH42,'n',@money_culture) as 'PPh_42'
	, ipo.Remark as 'Remarks'
	, format((ipo.InvoiceAmmount + ipoioc.PPN + ipoioc.PPH23 + ipoioc.PPH42),'n',@money_culture) as 'Invoice_After_Tax_Or_Grand_Total'
	from InvoicePO as ipo
	inner join MasterTable as mt on mt.Category = 'InvoiceManagement.Status' and mt.ValueId = ipo.Status
	inner join (
		select
		p.InvoicePOId
		, p.ItemId
		, p.PONonShoppingDetailId
		, p.PPN
		, p.PPH23
		, p.PPH42
		from (
			select
			ipoioc.InvoicePOId
			, ipoioc.ItemId
			, ipoioc.PONonShoppingDetailId
			, (
				case sc.SubCategoryCode
				when 'SC-2023-10-01228' then 'PPN'
				when 'SC-2023-10-01230' then 'PPH23'
				when 'SC-2023-10-01229' then 'PPH42'
				else sc.SubCategoryCode
				end
			) as 'Pivoted'
			, ipoioc.Amount
			from InvoicePOItemOtherCost as ipoioc
			inner join SubCategory as sc on sc.SubCategoryCode in ('PPN','PPH23','PPH42','SC-2023-10-01230','SC-2023-10-01229','SC-2023-10-01228') and sc.id = ipoioc.OtherCost_SubCategoryId
		) as s
		pivot (max(s.Amount) for Pivoted in ([PPN],[PPH23],[PPH42])) as p
	) as ipoioc on ipoioc.InvoicePOId = ipo.Id
)

--
select
count(*) over() as 'Count'
--, count(*) over(partition by pr.PR_No) as 'PR_No_Count'
--
, coalesce(pr.PR_No,'') as PR_No
, coalesce(pr.PR_Status,'') as PR_Status
, coalesce(pr.PR_Date,'') as PR_Date
, coalesce(pr.Requester,'') as Requester
, coalesce(pr.Department,'') as Department
, coalesce(pr.Type_Of_Transaction,'') as Type_Of_Transaction
, coalesce(pr.Buyer_User_Name,'') as Buyer_User_Name

--, coalesce(pr.Total_Budget_Estimation,'') as Total_Budget_Estimation
, (
case
when 
(row_number() over(partition by pr.PR_No
order by
-- default order -- pr._CategoryProcess_SubCategoryName asc
-- default order -- , pr._Id desc
-- default order -- , pr._DetailId desc
-- default order -- , tp._Id desc
-- default order -- , tp._DetailId desc
-- default order -- , dn._Id desc
-- default order -- , dn._DetailId desc
-- default order -- , ipo._Id desc
-- input order -- 
)) = 1
then coalesce(pr.Total_Budget_Estimation,'') else '' 
end
) as Total_Budget_Estimation

, coalesce(pr.Critical,'') as Critical
, coalesce(pr.Category,'') as Category
, coalesce(pr.Item_Name,'') as Item_Name
, coalesce(pr.Account_Code,'') as Account_Code
, coalesce(pr.Cost_Center,'') as Cost_Center
, coalesce(pr.Vendor_Selection,'') as Vendor_Selection
, coalesce(pr.Currency,'') as Currency
, coalesce(pr.PR_Posted_Date,'') as PR_Posted_Date
, coalesce(pr.Delivery_Request_Date,'') as Delivery_Request_Date
, coalesce(pr.Final_Spec_Req_Date,'') as Final_Spec_Req_Date
, coalesce(pr.Generate_Proc_Sum_Date,'') as Generate_Proc_Sum_Date
--
, coalesce(pr.TAT_WD,'') as TAT_WD
, coalesce(pr.SLA_WD,'') as SLA_WD
, coalesce(pr.SLA_Status,'') as SLA_Status
, coalesce(pr.Vendor,'') as Vendor
, coalesce(pr.Selected,'') as Selected
, coalesce(pr.Selected_Vendors_Total_Budget,'') as Selected_Vendors_Total_Budget
, coalesce(pr.Realised_Saving,'') as Realised_Saving
--
, coalesce( tp.Order_Type,'') as Order_Type
, coalesce( tp.Order_No,'') as Order_No
, coalesce( tp.Order_Status,'') as Order_Status
, coalesce( tp.Order_Date,'') as Order_Date
, coalesce( tp.Order_Grand_Total_Amount,'') as Order_Grand_Total_Amount
, coalesce( tp.Approver_Order_And_Date,'') as Approver_Order_And_Date
--
, coalesce( dn.DN_No,'') as DN_No
, coalesce( dn.DN_Status,'') as DN_Status
, coalesce( dn.DN_Date,'') as DN_Date
, coalesce( dn.DN_Qty,'') as DN_Qty
--
, coalesce(ipo.Invoice_No,'') as Invoice_No
, coalesce(ipo.Invoice_Status,'') as Invoice_Status
, coalesce(ipo.Invoice_Date,'') as Invoice_Date
, coalesce(ipo.Invoice_Amount,'') as Invoice_Amount
, coalesce(ipo.PPn,'') as PPn
, coalesce(ipo.PPh_23,'') as PPh_23
, coalesce(ipo.PPh_42,'') as PPh_42
, coalesce(ipo.Invoice_After_Tax_Or_Grand_Total,'') as Invoice_After_Tax_Or_Grand_Total
, coalesce(ipo.Remarks,'') as Remarks

from w_pr as pr
left join w_type_process as tp on tp._CategoryProcess_SubCategoryId = pr._CategoryProcess_SubCategoryId and tp._TypeProcess_SubCategoryId = pr._TypeProcess_SubCategoryId and tp._PurchaseRequestItemDetailId = pr._PurchaseRequestItemDetailId
left join w_dn as dn on dn._CategoryProcess_SubCategoryId = tp._CategoryProcess_SubCategoryId and dn._PurchaseOrderTOPId = tp._PurchaseOrderTOPId
left join w_ipo as ipo on ipo._CategoryProcess_SubCategoryId = tp._CategoryProcess_SubCategoryId and ipo._PurchaseOrderTOPId = tp._PurchaseOrderTOPId

where '2024-01-01' <= pr.PR_Date

order by
-- default order -- pr._CategoryProcess_SubCategoryName asc
-- default order -- , pr._Id desc
-- default order -- , pr._DetailId desc
-- default order -- , tp._Id desc
-- default order -- , tp._DetailId desc
-- default order -- , dn._Id desc
-- default order -- , dn._DetailId desc
-- default order -- , ipo._Id desc
-- input order -- 

offset @Start rows
fetch next @Length rows only

";

                //request.Columns[0].Data
                string order = (request.Order ?? Array.Empty<Models.DataTables.DtOrder>())
                    .Select((o, i) => ((request.Columns ?? Array.Empty<Models.DataTables.DtColumn>())[o.Column].Data == 0.ToString()) ? string.Empty : $"/* {i} */ {(request.Columns ?? Array.Empty<Models.DataTables.DtColumn>())[o.Column].Data} {o.Dir}")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Aggregate(string.Empty, (a, s) => string.IsNullOrWhiteSpace(a) ? s : $"{a}{Environment.NewLine},{s}");

                q = string.IsNullOrWhiteSpace(order) ? q.Replace($"-- default order --", string.Empty) : q.Replace($"-- input order -- ", order);

                q = (request.Length == 0) ? q.Replace("fetch next", "-- fetch next") : q;

                Dapper.DynamicParameters p = new(new
                {
                    request.Start,
                    request.Length,
                    request.PR_Category_Id,
                    PR_No = $"%{request.PR_No}%",
                    request.PR_Status_ValueId,
                    PR_Date_Begin = request.PR_Date_Begin?.ToString("yyyy-MM-dd"),
                    PR_Date_End = request.PR_Date_End?.ToString("yyyy-MM-dd"),

                    request.Department_Id,
                    request.Account_Code_Id,
                    request.Cost_Center_Id,
                    request.Vendor_Id,

                    request.Order_Type_Id,
                    Order_No = $"%{request.Order_No}%",
                    request.Order_Status_ValueId,
                    Order_Date_Begin = request.Order_Date_Begin?.ToString("yyyy-MM-dd"),
                    Order_Date_End = request.Order_Date_End?.ToString("yyyy-MM-dd")

                });
                return await Task.FromResult(_dapper.GetAll<Models.Report.Procurement.DataTables.Data>(q, p));
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }

        public APS_Common.Models.Report.Contract.Index.Model.Response.Root Report_Contract_Index_Model(APS_Common.Models.Report.Contract.Index.Model.Request.Root request)
        {
            var id = request.UUID;
            var c = GetType().Name;
            var m = MethodBase.GetCurrentMethod().Name;
            var nl = Environment.NewLine;

            APS_Common.BaseLogging.LogInfo(id, c, m, nameof(request));

            var sql = string.Empty;
            var json = string.Empty;

            var response = new APS_Common.Models.Report.Contract.Index.Model.Response.Root();

            try
            {
                sql = Report_Contract_Index_Model_Sql;
                json = _dapper.Get<string>(sql, new());
                response = System.Text.Json.JsonSerializer.Deserialize<APS_Common.Models.Report.Contract.Index.Model.Response.Root>(json);
                return response;
            }
            catch (Exception e)
            {
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.Message) + nl + e.Message);
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.StackTrace) + nl + e.StackTrace);

                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(request) + nl + System.Text.Json.JsonSerializer.Serialize(request));
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(sql) + nl + sql);
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(json) + nl + json);
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(response) + nl + System.Text.Json.JsonSerializer.Serialize(response));

                var be = e.GetBaseException();
                throw new APS_Common.GlobalExceptions(be.Message, be);
            }
        }

        const string Report_Contract_Index_Model_Sql = @$"
select
json_query(coalesce((

	select 1 as x

	-- CostCenterArray
	, json_query(coalesce((
		select 1 as x
		, cc.Id
		, cc.Code
		, cc.Name
		from CostCenter as cc
		for json path ,  include_null_values --, without_array_wrapper
	),'[]')) as 'CostCenterArray'

	-- BusinessUnitArray
	, json_query(coalesce((
		select 1 as x
		, bu.Id
		, bu.Code
		, bu.Name
		from BusinessUnit as bu
		for json path ,  include_null_values --, without_array_wrapper
	),'[]')) as 'BusinessUnitArray'

	-- VendorArray
	, json_query(coalesce((
		select 1 as x
		, v.Id
		, coalesce(v.Code,'') as Code
		, v.Name
		from Vendor as v
		for json path ,  include_null_values --, without_array_wrapper
	),'[]')) as 'VendorArray'

	-- Contract_Status_MasterTableArray
	, json_query(coalesce((
		select 1 as x
		, mt.Id
		, mt.Category
		, mt.Sequence
		, mt.Name
		, mt.Code
		, mt.ValueId
		from MasterTable as mt
		where mt.Category = 'GuaranteeLetter.Status'
		for json path ,  include_null_values --, without_array_wrapper
	),'[]')) as 'Contract_Status_MasterTableArray'

	for json path ,  include_null_values , without_array_wrapper
),'{{}}')) as 'json'
";

        public APS_Common.Models.DataTables.Response<APS_Common.Models.Report.Contract.Index.DataTables.Row.Root> Report_Contract_Index_DataTables(APS_Common.Models.Report.Contract.Index.DataTables.Request.Root request)
        {
            var id = request.UUID;
            var c = GetType().Name;
            var m = MethodBase.GetCurrentMethod().Name;
            var nl = Environment.NewLine;

            APS_Common.BaseLogging.LogInfo(id, c, m, nameof(request));

            var sql = string.Empty;
            var param = new object();
            var json = string.Empty;

            var response = new APS_Common.Models.DataTables.Response<APS_Common.Models.Report.Contract.Index.DataTables.Row.Root>();

            try
            {
                request.PRF.PRFNo = (request.PRF.PRFNo ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(request.PRF.PRFNo)) request.PRF.PRFNo = null;

                sql = Report_Contract_Index_DataTables_Sql(request);
                param = new
                {
                    PRF_PRFNo = $"%{request.PRF.PRFNo ?? ""}%",
                    PRF_RequestDate_Begin = request.PRF.RequestDate.Begin,
                    PRF_RequestDate_End = request.PRF.RequestDate.End,
                    PRF_CostCenter_IdArray = request.PRF.CostCenter.IdArray,
                    PRF_RequestorAccount_CostCenter_BusinessUnit_IdArray = request.PRF.RequestorAccount.CostCenter.BusinessUnit.IdArray,
                    PRF_PRFSummary_PRFSummaryDetail_Vendor_IdArray = request.PRF.PRFSummary.PRFSummaryDetail.Vendor.IdArray,
                    PRF_PRFSummary_PRFSummaryDetail_Contract_Status_MasterTable_ValueIdArray = request.PRF.PRFSummary.PRFSummaryDetail.Contract.Status_MasterTable.ValueIdArray,
                    x = 1
                };
                json = _dapper.Get<string>(sql, new(param));

                var data = System.Text.Json.JsonSerializer.Deserialize<APS_Common.Models.Report.Contract.Index.DataTables.Row.Root[]>(json);
                response = response.Data(data);
                return response;
            }
            catch (Exception e)
            {
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.Message) + nl + e.Message);
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.StackTrace) + nl + e.StackTrace);

                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(request) + nl + System.Text.Json.JsonSerializer.Serialize(request));
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(sql) + nl + sql);
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(param) + nl + System.Text.Json.JsonSerializer.Serialize(param));
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(json) + nl + json);
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(response) + nl + System.Text.Json.JsonSerializer.Serialize(response));

                var be = e.GetBaseException();
                throw new APS_Common.GlobalExceptions(be.Message, be);
            }
        }

        public static string Report_Contract_Index_DataTables_Sql(APS_Common.Models.Report.Contract.Index.DataTables.Request.Root request)
        {
            var jsonToSql = new Dictionary<string, string>()
                {
                    { "", "" }
                    , { "PRF.PRFNo", "p.PRFNo" } // PR No
                    , { "PRF.RequestDate", "p.RequestDate" } // PR Date
                    , { "PRF.Status_MasterTable.Name", "PRF_Status_mt.Name" } // PR Status
                    , { "PRF.RequestorAccount.Username", "Requestor_a.Username" } // Requestor Name
                    , { "PRF.BuyerAccount.Username", "Buyer_a.Username" } // Procurement Buyer Name
                    , { "PRF.RequestorAccount.CostCenter.CodeAndName", "Requestor_a_cc.CodeAndName" } // Requestor Department
                    , { "PRF.AccountMaster.AccountCodeAndDescription", "p_am.AccountCodeAndDescription" } // Account Code
                    , { "PRF.CostCenter.CodeAndName", "p_cc.CodeAndName" } // Cost Center
                    , { "PRF.L_Currency_Code", "p.L_Currency_Code" } // Currency
                    , { "PRF.ApprovalRequestGroupMember.ApprovalDateAndUserName", "p_argm.ApprovalDateAndUserName" } // PR Approval Name & Date
                    , { "PRF.PRFSummary.PRFSummaryDetail.Contract.ContractTitle", "c.ContractTitle" } // Contract Title
                    , { "PRF.PRFSummary.PRFSummaryDetail.Contract.ContractType_MasterTable.Name", "ContractType_mt.Name" } // Contract Type
                    , { "PRF.PRFSummary.PRFSummaryDetail.Contract.ContractNo", "c.ContractNo" } // No.Contract
                    , { "PRF.PRFSummary.PRFSummaryDetail.Vendor.Name", "v.Name" } // Vendor Name
                    , { "PRF.PRFSummary.PRFSummaryDetail.Contract.ContractPeriodStart", "c.ContractPeriodStart" } // Contract Period Start
                    , { "PRF.PRFSummary.PRFSummaryDetail.Contract.ContractPeriodEnd", "c.ContractPeriodEnd" } // Contract Period End
                    , { "PRF.PRFSummary.PRFSummaryDetail.Contract.AmountContract", "c.AmountContract" } // Amount Contract
                    , { "PRF.PRFSummary.PRFSummaryDetail.Contract.ALPeriodStart", "c.ALPeriodStart" } // AL Period Start
                    , { "PRF.PRFSummary.PRFSummaryDetail.Contract.ALPeriodEnd", "c.ALPeriodEnd" } // AL Period End
                    , { "PRF.PRFSummary.PRFSummaryDetail.Contract.UploadFinalDate", "c.UploadFinalDate" } // Upload Contract Date
                    , { "PRF.IsSecurityAssessment", "p.IsSecurityAssessment" } // Status SAT PR
                    , { "PRF.TypeOrder", "p.TypeOrder" } // Outsourcing/Non Out Sourcing
                    , { "PRF.PRFSummary.PRFSummaryDetail.Contract.Remark", "c.Remark" } // Remarks
                };

            var sql = @$"

-- Report/Contract

declare @Contract_TypeProcess_SubCategory as int = 
(
select top (1) sc.Id
from SubCategory as sc
where sc.SubCategoryCode = 'SC-2023-08-11131'
) ;


with x(x) as (select 1 as x)

, am
(
    Id
    , AccountCode
    , Description
    , AccountCodeAndDescription
)
as
(
	select
	am.Id
	, am.AccountCode
	, am.Description
	, (am.AccountCode + ' - ' + am.Description) as AccountCodeAndDescription
    from AccountMaster as am
)

, cc
(
    Id
    , Code
    , Name
    , CodeAndName
    , BusinessUnitId
)
as
(
    select
    cc.Id
    , cc.Code
    , cc.Name
    , (cc.Code + ' - ' + cc.Name) as CodeAndName
    , cc.BusinessUnitId
    from CostCenter as cc
)

, bu
(
    Id
    , Code
    , Name
    , CodeAndName
)
as
(
    select
    bu.Id
    , bu.Code
    , bu.Name
    , (bu.Code + ' - ' + bu.Name) as CodeAndName
    from BusinessUnit as bu
)

select json_query(coalesce((

	select 1 as x
	, count(1) over() as 'RecordsFiltered'

	-- PRF
	, json_query(coalesce((
		select 1 as x
		, p.Id
		, p.PRFNo
		, p.RequestDate
		, p.L_Currency_Code
		, p.IsRiskAssementForm
		, p.IsSecurityAssessment
		, p.TypeOrder

		, p.BudgetCode
		-- AccountMaster
		, json_query(coalesce((
			select 1 as x
			, p_am.Id
			, p_am.AccountCode
			, p_am.Description
			, p_am.AccountCodeAndDescription
			for json path , include_null_values , without_array_wrapper
		),'{{}}')) as 'AccountMaster'

		, p.Status
		-- Status_MasterTable
		, json_query(coalesce((
			select 1 as x
			, PRF_Status_mt.Id
			, PRF_Status_mt.Category
			, PRF_Status_mt.Code
			, PRF_Status_mt.Name
			, PRF_Status_mt.ValueId
			for json path , include_null_values , without_array_wrapper
		),'{{}}')) as 'Status_MasterTable'

		, p.RequestorAccountId
		-- RequestorAccount
		, json_query(coalesce((
			select 1 as x
			, Requestor_a.Id
			, Requestor_a.Username

			, Requestor_a.CostCenterId
			-- CostCenter
			, json_query(coalesce((
				select 1 as x
				, Requestor_a_cc.Id
				, Requestor_a_cc.Code
				, Requestor_a_cc.Name
				, Requestor_a_cc.CodeAndName

				, Requestor_a_cc.BusinessUnitId
		        -- BusinessUnit
		        , json_query(coalesce((
			        select 1 as x
			        , Requestor_a_cc_bu.Id
				    , Requestor_a_cc_bu.Code
				    , Requestor_a_cc_bu.Name
				    , Requestor_a_cc_bu.CodeAndName
			        for json path , include_null_values , without_array_wrapper
		        ),'{{}}')) as 'BusinessUnit'

				for json path , include_null_values , without_array_wrapper
			),'{{}}')) as 'CostCenter'

			for json path , include_null_values , without_array_wrapper
		),'{{}}')) as 'RequestorAccount'

		, p.BuyerAccountId
		-- BuyerAccount
		, json_query(coalesce((
			select 1 as x
			, Buyer_a.Id
			, Buyer_a.Username

			, Buyer_a.CostCenterId
			-- CostCenter
			, json_query(coalesce((
				select 1 as x
				, Buyer_a_cc.Id
				, Buyer_a_cc.Code
				, Buyer_a_cc.Name
				, Buyer_a_cc.CodeAndName

				, Buyer_a_cc.BusinessUnitId
		        -- BusinessUnit
		        , json_query(coalesce((
			        select 1 as x
			        , Buyer_a_cc_bu.Id
				    , Buyer_a_cc_bu.Code
				    , Buyer_a_cc_bu.Name
				    , Buyer_a_cc_bu.CodeAndName
			        for json path , include_null_values , without_array_wrapper
		        ),'{{}}')) as 'BusinessUnit'

				for json path , include_null_values , without_array_wrapper
			),'{{}}')) as 'CostCenter'

			for json path , include_null_values , without_array_wrapper
		),'{{}}')) as 'BuyerAccount'

		, p.BusinesUnitId

		, p.CostCenterId
		-- CostCenter
		, json_query(coalesce((
			select 1 as x
			, p_cc.Id
			, p_cc.Code
			, p_cc.Name
			, p_cc.CodeAndName

			, p_cc.BusinessUnitId
		    -- BusinessUnit
		    , json_query(coalesce((
			    select 1 as x
			    , p_cc_bu.Id
			    , p_cc_bu.Code
			    , p_cc_bu.Name
			    , p_cc_bu.CodeAndName
			    for json path , include_null_values , without_array_wrapper
		    ),'{{}}')) as 'BusinessUnit'

			for json path , include_null_values , without_array_wrapper
		),'{{}}')) as 'CostCenter'

        , p.ApprovalRequestId
        -- ApprovalRequestGroupMember
        , json_query((
            select 1 as x
            , p_argm.Id
            , p_argm.ApprovalRequestId
            , p_argm.ApprovalDate
            , p_argm.UserName
            , p_argm.Status
            , p_argm.ApprovalDateAndUserName
	        for json path , include_null_values , without_array_wrapper
        )) as 'ApprovalRequestGroupMember'

		--PRFSummary
		, json_query(coalesce((
			select 1 as x
			, ps.Id
			, ps.PRFId

			-- PRFSummaryDetail
			, json_query(coalesce((
				select 1 as x
				, psd.PRFSummaryId

				, psd.VendorId
				-- Vendor
				, json_query(coalesce((
					select 1 as x
					, v.Id
					, v.Code
					, v.Name
					for json path , include_null_values , without_array_wrapper
				),'{{}}')) as 'Vendor'

				-- Contract
				, json_query(coalesce((
					select top (1) 1 as x
					, c.Id
					, c.PRFSummaryId
					, c.VendorId
					, c.ContractNo
					, c.ALPeriodStart
					, c.ALPeriodEnd
					, c.UploadFinalDate
					, c.ContractTitle
					, c.AmountContract
					, c.Remark
					, c.ContractPeriodStart
					, c.ContractPeriodEnd

					, c.ContractType_SubCategory
					-- ContractType_MasterTable
					, json_query(coalesce((
						select 1 as x
						, ContractType_mt.Id
						, ContractType_mt.Category
						, ContractType_mt.Code
						, ContractType_mt.Name
						, ContractType_mt.ValueId
						for json path , include_null_values , without_array_wrapper
					),'{{}}')) as 'ContractType_MasterTable'

					, c.Status
					-- Status_MasterTable
					, json_query(coalesce((
						select 1 as x
						, Contract_Status_mt.Id
						, Contract_Status_mt.Category
						, Contract_Status_mt.Code
						, Contract_Status_mt.Name
						, Contract_Status_mt.ValueId
						for json path , include_null_values , without_array_wrapper
					),'{{}}')) as 'Status_MasterTable'

					for json path , include_null_values , without_array_wrapper
				),'{{}}')) as 'Contract'

				for json path , include_null_values , without_array_wrapper
			),'{{}}')) as 'PRFSummaryDetail'

			for json path , include_null_values , without_array_wrapper
		),'{{}}')) as 'PRFSummary'

		for json path , include_null_values , without_array_wrapper
	),'{{}}')) as 'PRF'

	from PRF as p

	inner join am as p_am on p_am.AccountCode = p.BudgetCode
	inner join MasterTable as PRF_Status_mt on PRF_Status_mt.Category = 'PRF.Status' and PRF_Status_mt.ValueId = p.Status

	inner join Flips.UserAccount as Requestor_a on Requestor_a.Id = p.RequestorAccountId
	inner join cc as Requestor_a_cc on Requestor_a_cc.Id = Requestor_a.CostCenterId
	inner join bu as Requestor_a_cc_bu on Requestor_a_cc_bu.Id = Requestor_a_cc.BusinessUnitId
	{(request.PRF.RequestorAccount.CostCenter.BusinessUnit.IdArray.Length == 0 ? "--" : "")}and Requestor_a_cc_bu.Id in @PRF_RequestorAccount_CostCenter_BusinessUnit_IdArray

	inner join Flips.UserAccount as Buyer_a on Buyer_a.Id = p.BuyerAccountId
	inner join cc as Buyer_a_cc on Buyer_a_cc.Id = Buyer_a.CostCenterId
	inner join bu as Buyer_a_cc_bu on Buyer_a_cc_bu.Id = Buyer_a_cc.BusinessUnitId

	inner join cc as p_cc on p_cc.Id = p.CostCenterId
	{(request.PRF.CostCenter.IdArray.Length == 0 ? "--" : "")}and p_cc.Id in @PRF_CostCenter_IdArray
	inner join bu as p_cc_bu on p_cc_bu.Id = p_cc.BusinessUnitId

	inner join PRFSummary as ps on ps.PRFId = p.Id
	inner join (
		select 1 as x
		, psd.PRFSummaryId
		, psd.VendorId
		from PRFSummaryDetail as psd
		where psd.IsSelected = 1
		group by
		psd.PRFSummaryId
		, psd.VendorId
	) as psd on psd.PRFSummaryId = ps.Id

	inner join Vendor as v on v.Id = psd.VendorId
	{(request.PRF.PRFSummary.PRFSummaryDetail.Vendor.IdArray.Length == 0 ? "--" : "")}and v.Id in @PRF_PRFSummary_PRFSummaryDetail_Vendor_IdArray

    -- report contract bisa ditarik semanjak dari procsum full approve
    inner join [Contract] as c on c.PRFSummaryId = ps.Id and c.VendorId = psd.VendorId
	--left join SubCategory as ContractType_sc on ContractType_sc.Id = c.ContractType_SubCategory
    left join MasterTable as ContractType_mt on ContractType_mt.Category = 'ContractType' and ContractType_mt.Id = c.ContractType_SubCategory

	left join MasterTable as Contract_Status_mt on Contract_Status_mt.Category = 'Contract.Status' and Contract_Status_mt.ValueId = c.Status
	{(request.PRF.PRFSummary.PRFSummaryDetail.Contract.Status_MasterTable.ValueIdArray.Length == 0 ? "--" : "")}and Contract_Status_mt.ValueId in @PRF_PRFSummary_PRFSummaryDetail_Contract_Status_MasterTable_ValueIdArray

    left join (
        select 1 as x
        , argm.Id
        , argm.ApprovalRequestId
        , argm.ApprovalDate
        , argm.UserName
        , argm.Status
        , case when argm.Status = 2 then (format(argm.ApprovalDate,'yyyy-MM-dd') + ' - ' + argm.UserName) else '' end as ApprovalDateAndUserName
        , row_number() over(partition by argm.ApprovalRequestId order by argm.Id) as RowNumber
        from ApprovalRequestGroupMember as argm
    ) as p_argm
    on p_argm.RowNumber = 1
    and p_argm.ApprovalRequestId = p.ApprovalRequestId

	where 1 = 1
	and p.TypeProcess_SubCategory = @Contract_TypeProcess_SubCategory
	{(request.PRF.PRFNo is null ? "--" : "")}and p.PRFNo like @PRF_PRFNo
	{(request.PRF.RequestDate.Begin is null ? "--" : "")}and cast(p.RequestDate as date) >= cast(@PRF_RequestDate_Begin as date)
	{(request.PRF.RequestDate.End is null ? "--" : "")}and cast(p.RequestDate as date) <= cast(@PRF_RequestDate_End as date)

	{request.ToSqlOrderByAndOffsetAndFetchNext(jsonToSql, new() { ColumnName = "ps.Id" }, 1)}		

	for json path , include_null_values --, without_array_wrapper
),'[]')) as 'array'

";

            return sql;
        }

//        public async Task<IEnumerable<Models.Report.Procurement.DataTables.Data_WIP>> ProcurementDataTablesData_WIP2(Models.Report.Procurement.DataTables.Request_WIP request)
//        {
//            try
//            {
//                string q = string.Empty;
//                q = $@"

//--declare @Start int = 0
//--declare @Length int = 10;

//with
//x as (select 1 as x)

//, w_bt as 
//(
//	select 1 as x
//	, bt.RefNumber
//	, bt.L_Currency_Code
//	, bt.RateAmount
//	from BudgetTransaction as bt
//	inner join SubCategory as Transaction_sc
//	on Transaction_sc.SubCategoryCode in ('Transaction.SC','Transaction.PRF')
//	and Transaction_sc.Id = bt.Transaction_SubCategoryId
//	group by
//	bt.RefNumber
//	, bt.L_Currency_Code
//	, bt.RateAmount
//)

//, w_p_last_psc as
//(
//	select
//	1 as x
//	, psc.PRFId
//	, max(psc.Id) as PRFSpendingCategoryId
//	from PRFSpendingCategory as psc
//	where psc.Status = 1
//	group by psc.PRFId
//)

//, w_ps_TAT as
//(
//	select
//	1 as x
//	, ps.Id as PRFSummaryId

//	, psc.CreatedTime
//	, ps.PRFSummaryDate

//	, (DATEDIFF(dd, psc.CreatedTime, ps.PRFSummaryDate) + 1) as 'dd'
//	, (DATEDIFF(wk, psc.CreatedTime, ps.PRFSummaryDate) * 2) as 'wk'
//	, (CASE WHEN DATENAME(dw, psc.CreatedTime) = 'Sunday' THEN 1 ELSE 0 END) as 'Sunday'
//	, (CASE WHEN DATENAME(dw, ps.PRFSummaryDate) = 'Saturday' THEN 1 ELSE 0 END) as 'Saturday'
//	, (SELECT COUNT(*) FROM MasterHoliday WHERE CAST(DateHoliday as date) BETWEEN CAST(psc.CreatedTime as date) AND CAST(ps.PRFSummaryDate as date)) as 'MasterHoliday'

//	--TAT (WD) Berapa lama proses dari final spec s/d generate ProcSum
//	,
//	(
//		0
//		+ (DATEDIFF(dd, psc.CreatedTime, ps.PRFSummaryDate) + 1)
//		- (DATEDIFF(wk, psc.CreatedTime, ps.PRFSummaryDate) * 2)
//		- (CASE WHEN DATENAME(dw, psc.CreatedTime) = 'Sunday' THEN 1 ELSE 0 END)
//		- (CASE WHEN DATENAME(dw, ps.PRFSummaryDate) = 'Saturday' THEN 1 ELSE 0 END)
//		- (SELECT COUNT(*) FROM MasterHoliday WHERE CAST(DateHoliday as date) BETWEEN CAST(psc.CreatedTime as date) AND CAST(ps.PRFSummaryDate as date))
//	) as 'TAT_WD'

//	from PRFSummary as ps
//	inner join w_p_last_psc as p_last_psc
//	on p_last_psc.PRFId = ps.PRFId
//	inner join PRFSpendingCategory as psc
//	on psc.Id = p_last_psc.PRFSpendingCategoryId
//)

//, w_p_FinalSpesificationDate_GenerateDate as
//(
//	select
//	1 as x
//	, p.Id as PRFId

//	/*
//	TAT (WD) = generate ProcSum - final spec
//	TAT (WD) = ps.PRFSummaryDate - psc.CreatedTime
//	TAT (WD) (baru) = ps.PRFSummaryDate - pvq.FinalSpesificationDate

//	TAT (WD) (PAP) = generate PAP - final spec
//	TAT (WD) (PAP) = PAP.GenerateDate - pvq.FinalSpesificationDate
//	*/

//	, cast(coalesce(pvq.FinalSpesificationDate, psc.CreatedTime) as date) as 'FinalSpesificationDate'

//	, (
//	case
//	when tp_sc.SubCategoryCode = 'SC-2023-08-11134' -- PAP
//	then pap.GenerateDate
//	else ps.PRFSummaryDate
//	end
//	)  as 'GenerateDate'


//	from PRF as p
//	inner join SubCategory as tp_sc on tp_sc.Id = p.TypeProcess_SubCategory
//	inner join w_p_last_psc as p_last_psc on p_last_psc.PRFId = p.Id
//	inner join PRFSpendingCategory as psc on psc.Id = p_last_psc.PRFSpendingCategoryId
//	inner join PRFVendorQuotation as pvq on pvq.PRFId = p.Id
//	left join (
//		select ps.PRFId , max(ps.Id) as Id
//		from PRFSummary as ps
//		group by ps.PRFId
//	) as last_ps
//	on last_ps.PRFId = p.Id
//	left join PRFSummary as ps on ps.PRFVendorQuotationId = pvq.Id and last_ps.Id = ps.Id
//	left join PAP as pap on pap.PRFVendorQuotationId = pvq.Id
//)

//, w_p_FinalSpesificationDate_GenerateDate_TATWD as
//(
//	select 1 as x
//	, x.PRFId
//	, x.FinalSpesificationDate
//	, x.GenerateDate

//	, x.datediff_day
//	, x.datediff_week
//	, x.datename_Sunday
//	, x.datename_Saturday
//	, x.MasterHoliday_count

//	--TAT (WD) Berapa lama proses dari final spec s/d generate ProcSum
//	, (x.datediff_day - x.datediff_week - x.datename_Sunday - x.datename_Saturday - x.MasterHoliday_count) as 'TAT_WD'

//	from (

//		select
//		1 as x
//		, p_FinalSpesificationDate_GenerateDate.PRFId
//		, p_FinalSpesificationDate_GenerateDate.FinalSpesificationDate
//		, p_FinalSpesificationDate_GenerateDate.GenerateDate

//		, (datediff(day, p_FinalSpesificationDate_GenerateDate.FinalSpesificationDate, p_FinalSpesificationDate_GenerateDate.GenerateDate)) as 'datediff_day'
//		, (datediff(week, p_FinalSpesificationDate_GenerateDate.FinalSpesificationDate, p_FinalSpesificationDate_GenerateDate.GenerateDate) * 2) as 'datediff_week'
//		, (case when datename(dw, p_FinalSpesificationDate_GenerateDate.FinalSpesificationDate) = 'Sunday' then 1 else 0 end) as 'datename_Sunday'
//		, (case when datename(dw, p_FinalSpesificationDate_GenerateDate.GenerateDate) = 'Saturday' then 1 else 0 end) as 'datename_Saturday'
//		, (
//			select count(*) as c
//			from MasterHoliday as mh
//			where cast(mh.DateHoliday as date)
//			between cast(p_FinalSpesificationDate_GenerateDate.FinalSpesificationDate as date)
//			and cast(p_FinalSpesificationDate_GenerateDate.GenerateDate as date)
//		) as 'MasterHoliday_count'

//		from w_p_FinalSpesificationDate_GenerateDate as p_FinalSpesificationDate_GenerateDate

//	) as x
//)

//, w_prid_group_prioc as
//(
//	select
//	1 as x
//	, prid.PurchaseRequestId
//	, prioc.PurchaseRequestItemDetailId
//	, sum(prioc.Amount) as Amount_sum
//	from PurchaseRequestItemDetail as prid
//	inner join PurchaseRequestItemOtherCost as prioc
//	on prioc.PurchaseRequestItemDetailId = prid.Id
//	group by
//	prid.PurchaseRequestId
//	, prioc.PurchaseRequestItemDetailId
//)

//, w_pr_group_prid as
//(
//	select
//	1 as x
//	, prid.PurchaseRequestId
//	, sum(prid.QtyRequest * prid.ItemPrice /* * prid.RateAmount */) as QtyRequest_x_ItemPrice_x_RateAmount_sum
//	, sum(prid_group_prioc.Amount_sum) as group_prioc_Amount_sum
//	from PurchaseRequestItemDetail as prid
//	inner join w_prid_group_prioc as prid_group_prioc
//	on prid_group_prioc.PurchaseRequestItemDetailId = prid.Id
//	group by
//	prid.PurchaseRequestId
//)

//, w_psd_group_psoc as
//(
//	select
//	1 as x
//	, psd.PRFSummaryId
//	, psoc.PRFSummaryDetailId
//	, sum(psoc.OtherCostAmount) as OtherCostAmount_sum
//	from PRFSummaryDetail as psd
//	inner join PRFSummaryOtherCost as psoc on psoc.PRFSummaryDetailId = psd.Id
//	group by
//	psd.PRFSummaryId
//	, psoc.PRFSummaryDetailId
//)

//, w_ps_group_psd as
//(
//	select
//	1 as x
//	, psd.PRFSummaryId
//	, sum(psd.Qty * psd.ItemPrice /* * psd.RateAmmount */) as Qty_x_ItemPrice_x_RateAmmount_sum
//	, sum(psd_group_psoc.OtherCostAmount_sum) as group_psoc_OtherCostAmount_sum
//	from PRFSummaryDetail as psd
//	inner join w_psd_group_psoc as psd_group_psoc on psd_group_psoc.PRFSummaryDetailId = psd.Id
//	where psd.IsSelected = 1
//	group by psd.PRFSummaryId
//)

//, w_pvqd_group_pvqoc as
//(
//	select
//	1 as x
//	, pvqd.PRFVendorQuotationId
//	, pvqoc.PRFVendorQuotationDetailId
//	, sum(pvqoc.OtherCostAmount) as OtherCostAmount_sum
//	from PRFVendorQuotationDetail as pvqd
//	inner join PRFVendorQuotationOtherCost as pvqoc on pvqoc.PRFVendorQuotationDetailId = pvqd.Id
//	group by
//	pvqd.PRFVendorQuotationId
//	, pvqoc.PRFVendorQuotationDetailId
//)

//, w_pvq_group_pvqd as
//(
//	select
//	1 as x
//	, pvqd.PRFVendorQuotationId
//	, sum(pvqd.Qty * pvqd.ItemPrice /* * pvqd.RateAmmount */) as Qty_x_ItemPrice_x_RateAmmount_sum
//	, sum(pvqd_group_pvqoc.OtherCostAmount_sum) as group_pvqoc_OtherCostAmount_sum
//	from PRFVendorQuotationDetail as pvqd
//	inner join w_pvqd_group_pvqoc as pvqd_group_pvqoc on pvqd_group_pvqoc.PRFVendorQuotationDetailId = pvqd.Id
//	group by pvqd.PRFVendorQuotationId
//)

//, w_pr as
//(
//	select
//	1 as x
//	, cp_sc.Id as _CategoryProcess_SubCategoryId
//	, cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode
//	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
//	, tp_sc.Id as _TypeProcess_SubCategoryId
//	, tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode
//	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName

//	-- PurchaseOrder
//	, prid.Id as _PurchaseRequestItemDetailId
//	, null as _PurchaseOrderDetailId
//	, null as _PurchaseOrderTOPId
//	-- PONonShopping
//	, null as _PONonShoppingDetailId
//	, null as _PONonShoppingTOPId
//	-- PONonShopping, GuaranteeLetter
//	, null as _PRFSummaryDetailId
//	-- Contract
//	, null as _PRFSummaryId
//	-- PAP
//	, null as _PRFVendorQuotationDetailId
//	--

//	, pr.Id as _Id
//	, prid.Id as _DetailId

//	, pr.RequestCode as 'PR_No'
//	, mt.Name as 'PR_Status'
//	, cast(pr.RequestDates as datetime) as 'PR_Date'
//	, Requestor_ua.Username as 'Requester'
//	, (Requestor_ua_cc.Code + ' - ' + Requestor_ua_cc.Name) as 'Department'
//	, null as 'Type_Of_Transaction'
//	, null as 'Buyer_User_Name'
//	, null as 'Total_Budget_Estimation'
//	, null as 'Critical'
//	, null as 'DPIA'
//	, null as 'VSDDT'
//	, null as 'Outsourcing_Status'
//	, null as 'Category'
//	, (i.ItemCode + ' - ' + i.Name) as 'Item_Name'
//	, (am.AccountCode + ' - ' + am.Description) as 'Account_Code'
//	, (cc.Code + ' - ' + cc.Name) as 'Cost_Center'
//	, v.Id as _VendorId
//	, (v.Code + ' - ' + v.Name) as 'Vendor_Selection'
//	, i.L_Currency_Code as 'Currency'
//	, pr.CreatedTime as 'PR_Posted_Date'
//	, (SELECT TOP 1 pod.DeliveryRequestDate FROM PurchaseOrderDetail pod WHERE pod.PurchaseOrderId = prtopo.PurchaseOrderId) as 'Delivery_Request_Date'
//	, null as 'Final_Spec_Req_Date'
//	, null as 'Generate_Proc_Sum_Date'
//	, null as 'TAT_WD'
//	, null as 'SLA_WD'
//	, null as 'SLA_Status'
//	, (v.Code + ' - ' + v.Name) as 'Vendor'
//	, null as 'Selected'

//	, pr_group_prid.QtyRequest_x_ItemPrice_x_RateAmount_sum as 'Total_Price'
//	, (prid.QtyRequest * prid.ItemPrice /* * prid.RateAmount */) as 'Price_Per_Item'
//	, pr_group_prid.QtyRequest_x_ItemPrice_x_RateAmount_sum + pr_group_prid.group_prioc_Amount_sum as 'Total_Price_Inc_Other_Cost'
//	, (prid.QtyRequest * prid.ItemPrice /* * prid.RateAmount */) + prid_group_prioc.Amount_sum as 'Price_Per_Item_Inc_Other_Cost'
//	, null as 'Realised_Saving'

//	, '' as 'ReasonCancel'

//	from PurchaseRequest as pr

//	left join w_pr_group_prid as pr_group_prid
//	on pr_group_prid.PurchaseRequestId = pr.Id

//	inner join Flips.UserAccount as Requestor_ua
//	on Requestor_ua.Id = pr.RequestorAccountId
	
//	inner join CostCenter as Requestor_ua_cc
//	on Requestor_ua_cc.Id = Requestor_ua.CostCenterId
//	{(request.Department_Id is null ? "--" : "")}and Requestor_ua_cc.Id = @Department_Id
	
//	inner join MasterTable as mt
//	on mt.Category = 'PurchaseRequest.Status'
//	and mt.ValueId = pr.Status
//	{(request.PR_Status_ValueId is null ? "--" : "")}and mt.ValueId = @PR_Status_ValueId
	
//	inner join PurchaseRequestItemDetail as prid
//	on prid.PurchaseRequestId = pr.Id

//	left join w_prid_group_prioc as prid_group_prioc
//	on prid_group_prioc.PurchaseRequestItemDetailId = prid.Id
	
//	inner join Item as i
//	on i.Id = prid.ItemId
	
//	inner join Vendor as v
//	on v.Id = i.VendorId
//	{(request.Vendor_Id is null ? "--" : "")}and v.Id = @Vendor_Id
	
//	inner join AccountMaster as am
//	on am.Id = prid.AccountMasterId
//	{(request.Account_Code_Id is null ? "--" : "")}and am.Id = @Account_Code_Id
	
//	inner join PurchaseRequestItemCostCenter as pricc
//	on pricc.PurchaseRequestItemDetailId = prid.Id
	
//	inner join CostCenter as cc
//	on cc.Id = pricc.CostCenterId
//	{(request.Cost_Center_Id is null ? "--" : "")}and cc.Id = @Cost_Center_Id
	
//	-- Shopping Cart
//	inner join SubCategory as cp_sc
//	on cp_sc.SubCategoryCode = 'SC-2024-02-01261'
//	{(request.PR_Category_Id is null ? "--" : "")}and cp_sc.Id = @PR_Category_Id
	
//	-- Purchase Order
//	inner join SubCategory as tp_sc
//	on tp_sc.SubCategoryCode = 'SC-2023-08-11132'
//	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id
    
//	left join PurchaseOrderToPurchaseRequest as prtopo
//	on pr.Id = prtopo.PurchaseRequestlId
	
//	where 1 = 1
//	{(string.IsNullOrWhiteSpace(request.PR_No) ? "--" : "")}and pr.RequestCode like @PR_No
//	{(request.PR_Date_Begin is null ? "--" : "")}and @PR_Date_Begin <= cast(pr.RequestDates as date)
//	{(request.PR_Date_End is null ? "--" : "")}and cast(pr.RequestDates as date) <= @PR_Date_End

//	union all

//	select
//	1 as x
//	, cp_sc.Id as _CategoryProcess_SubCategoryId
//	, cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode
//	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
//	, tp_sc.Id as _TypeProcess_SubCategoryId
//	, tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode
//	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName

//	-- PurchaseOrder
//	, null as _PurchaseRequestItemDetailId
//	, null as _PurchaseOrderDetailId
//	, null as _PurchaseOrderTOPId
//	-- PONonShopping
//	, null as _PONonShoppingDetailId
//	, null as _PONonShoppingTOPId
//	-- PONonShopping, GuaranteeLetter
//	, psd.Id as _PRFSummaryDetailId
//	-- Contract
//	, ps.Id as _PRFSummaryId
//	-- PAP
//	, pvqd.Id as _PRFVendorQuotationDetailId
//	--

//	, p.Id as _Id
//	, pd.Id as _DetailId

//	, p.PRFNo as 'PR_No'
//	, mt.Name as 'PR_Status'
//	, p.RequestDate as 'PR_Date'
//	, ua.Username as 'Requester'
//	, (ua_cc.Code + ' - ' + ua_cc.Name) as 'Department'
//	, p.TypeOfTransaction as 'Type_Of_Transaction'
//	, b_ua.Username as 'Buyer_User_Name'
//	, (p.TotalBudgetEstimation /* * bt.RateAmount */) as 'Total_Budget_Estimation'
//	, (case coalesce(p.IsRiskAssementForm,0) when 1 then 'Yes' else 'No' end) as 'Critical'
//	, (case when p.DataActivity is null then '' when p.DataActivity = 1 then 'Yes' else 'No' end) as 'DPIA'
//	, (case when p.ITSecurityActivity is null then '' when p.ITSecurityActivity = 1 then 'Yes' else 'No' end) as 'VSDDT'
//	, p.TypeOrder as 'Outsourcing_Status'
//	, s_c.Category as 'Category'
//	, pd.RequestItemName as 'Item_Name'
//	, (am.AccountCode + ' - ' + am.Description) as 'Account_Code'
//	, (cc.Code + ' - ' + cc.Name) as 'Cost_Center'
//	, v.Id as _VendorId
//	, (v.Code + ' - ' + v.Name) as 'Vendor_Selection'
//	, p.L_Currency_Code as 'Currency'
//	, p.CreatedTime as 'PR_Posted_Date'
//	, pd.DeliveryRequestDate as 'Delivery_Request_Date'

//	, p_FinalSpesificationDate_GenerateDate_TATWD.FinalSpesificationDate as 'Final_Spec_Req_Date'
//	, p_FinalSpesificationDate_GenerateDate_TATWD.GenerateDate as 'Generate_Proc_Sum_Date'

//	--TAT (WD) Berapa lama proses dari final spec s/d generate ProcSum
//	, p_FinalSpesificationDate_GenerateDate_TATWD.TAT_WD as 'TAT_WD'
//	, (case when p_FinalSpesificationDate_GenerateDate_TATWD.TAT_WD is null then null else 5 end) as 'SLA_WD'
//	--Status SLA: meet / not meet.Meet: 1-5 Hari Not Meet: diatas 5 hari kerja
//	, (case when p_FinalSpesificationDate_GenerateDate_TATWD.TAT_WD is null then null when p_FinalSpesificationDate_GenerateDate_TATWD.TAT_WD <= 5 then 'Meet' else 'Not Meet' end) as 'SLA_Status'

//	, (v.Code + ' - ' + v.Name) as 'Vendor'
//	, (
//		case
//		when tp_sc.SubCategoryCode = 'SC-2023-08-11134' /* PAP */ and pvqd.IsSelected = 1
//		then 'Yes'
//		when psd.IsSelected = 1
//		then 'Yes'
//		else ''
//		end
//	) as 'Selected'

//	, (
//		case
//		when tp_sc.SubCategoryCode = 'SC-2023-08-11134' -- PAP
//		then pvq_group_pvqd.Qty_x_ItemPrice_x_RateAmmount_sum
//		when tp_sc.SubCategoryCode = 'SC-2023-08-11135' -- PO PADI < 10 Juta
//		then p.GRAmount
//		else ps_group_psd.Qty_x_ItemPrice_x_RateAmmount_sum
//		end
//	) as 'Total_Price'
//	, (
//		case
//		when tp_sc.SubCategoryCode = 'SC-2023-08-11134' -- PAP
//		then (pvqd.Qty * pvqd.ItemPrice /* * pvqd.RateAmmount */)
//		else (psd.Qty * psd.ItemPrice /* * psd.RateAmmount */)
//		end
//	) as 'Price_Per_Item'
//	, (
//		case
//		when tp_sc.SubCategoryCode = 'SC-2023-08-11134' -- PAP
//		then (pvq_group_pvqd.Qty_x_ItemPrice_x_RateAmmount_sum + pvq_group_pvqd.group_pvqoc_OtherCostAmount_sum)
//		else (ps_group_psd.Qty_x_ItemPrice_x_RateAmmount_sum + ps_group_psd.group_psoc_OtherCostAmount_sum)
//		end
//	) as 'Total_Price_Inc_Other_Cost'
//	, (
//		case
//		when tp_sc.SubCategoryCode = 'SC-2023-08-11134' -- PAP
//		then ((pvqd.Qty * pvqd.ItemPrice /* * pvqd.RateAmmount */) + pvqd_group_pvqoc.OtherCostAmount_sum)
//		else ((psd.Qty * psd.ItemPrice /* * psd.RateAmmount */) + psd_group_psoc.OtherCostAmount_sum)
//		end
//	) as 'Price_Per_Item_Inc_Other_Cost'
//	, (
//		case
//		when tp_sc.SubCategoryCode = 'SC-2023-08-11134' -- PAP
//		then (
//			case
//			when pvq_group_pvqd.PRFVendorQuotationId is null
//			then null
//			when p.TotalBudgetEstimation > (pvq_group_pvqd.Qty_x_ItemPrice_x_RateAmmount_sum + pvq_group_pvqd.group_pvqoc_OtherCostAmount_sum)
//			then p.TotalBudgetEstimation - (pvq_group_pvqd.Qty_x_ItemPrice_x_RateAmmount_sum + pvq_group_pvqd.group_pvqoc_OtherCostAmount_sum)
//			else 0
//			end
//		)
//		else (
//			case
//			when ps_group_psd.PRFSummaryId is null
//			then null
//			when p.TotalBudgetEstimation > (ps_group_psd.Qty_x_ItemPrice_x_RateAmmount_sum + ps_group_psd.group_psoc_OtherCostAmount_sum)
//			then p.TotalBudgetEstimation - (ps_group_psd.Qty_x_ItemPrice_x_RateAmmount_sum + ps_group_psd.group_psoc_OtherCostAmount_sum)
//			else 0
//			end
//		)
//		end
//	) as 'Realised_Saving'

//	, p.ReasonCancel as 'ReasonCancel'

//	from PRF as p

//	left join w_p_FinalSpesificationDate_GenerateDate_TATWD as p_FinalSpesificationDate_GenerateDate_TATWD
//	on p_FinalSpesificationDate_GenerateDate_TATWD.PRFId = p.Id

//	left join w_bt as bt
//	on bt.RefNumber = p.PRFNo

//	-- Non Shopping Cart
//	inner join SubCategory as cp_sc
//	on cp_sc.SubCategoryCode = 'SC-2024-02-01262'
//	{(request.PR_Category_Id is null ? "--" : "")}and cp_sc.Id = @PR_Category_Id
	
//	left join SubCategory as tp_sc
//	on tp_sc.Id = p.TypeProcess_SubCategory
//	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id
	
//	inner join Flips.UserAccount as ua
//	on ua.Id = p.RequestorAccountId
	
//	inner join CostCenter as ua_cc
//	on ua_cc.Id = ua.CostCenterId
//	{(request.Department_Id is null ? "--" : "")}and ua_cc.Id = @Department_Id
	
//	left join Flips.UserAccount as b_ua
//	on b_ua.Id = p.BuyerAccountId
	
//	inner join MasterTable as mt
//	on mt.Category = 'PRF.Status'
//	and mt.ValueId = p.Status
//	{(request.PR_Status_ValueId is null ? "--" : "")}and mt.ValueId = @PR_Status_ValueId
	
//	inner join AccountMaster as am
//	on am.AccountCode = p.BudgetCode
//	{(request.Account_Code_Id is null ? "--" : "")}and am.Id = @Account_Code_Id
	
//	inner join CostCenter as cc
//	on cc.Id = p.CostCenterId
//	{(request.Cost_Center_Id is null ? "--" : "")}and cc.Id = @Cost_Center_Id
	
//	inner join Spending_Category as s_c
//	on s_c.Id = p.Spending_Category
	
//	inner join w_p_last_psc as last_psc
//	on last_psc.PRFId = p.id
	
//	inner join PRFSpendingCategory as psc
//	on psc.Id = last_psc.PRFSpendingCategoryId
	
//	inner join PRFDetail as pd
//	on pd.PRFId = p.Id

//	left join PRFVendorQuotation as pvq
//	on pvq.PRFId = p.Id
	
//	left join PRFVendorQuotationDetail as pvqd
//	on pvqd.PRFVendorQuotationId = pvq.Id
//	and pvqd.PRFDetailId = pd.Id
//	and pvqd.Status = 1
//	and pvqd.IsSelected = 1
	
//	left join Vendor as v
//	on v.Id = pvqd.VendorId
//	{(request.Vendor_Id is null ? "--" : "")}and v.Id = @Vendor_Id

//	left join (
//		select
//		ps.PRFId
//		, max(ps.Id) as LastPRFSummaryId
//		from PRFSummary as ps
//		group by ps.PRFId
//	) as last_ps on last_ps.PRFId = p.Id
	
//	left join PRFSummary as ps
//	on ps.Id = last_ps.LastPRFSummaryId

//	left join w_ps_group_psd as ps_group_psd
//	on ps_group_psd.PRFSummaryId = ps.Id

//	left join w_pvq_group_pvqd as pvq_group_pvqd
//	on pvq_group_pvqd.PRFVendorQuotationId = pvq.Id

//	left join PRFSummaryDetail as psd
//	on psd.PRFSummaryId = ps.Id
//	and psd.PRFVendorQuotationDetailId = pvqd.Id
//	and psd.IsSelected = 1

//	left join w_psd_group_psoc as psd_group_psoc
//	on psd_group_psoc.PRFSummaryDetailId = psd.Id

//	left join w_pvqd_group_pvqoc as pvqd_group_pvqoc
//	on pvqd_group_pvqoc.PRFVendorQuotationDetailId = pvqd.Id

//	where 1 = 1
//	{(string.IsNullOrWhiteSpace(request.PR_No) ? "--" : "")}and p.PRFNo like @PR_No
//	{(request.PR_Date_Begin is null ? "--" : "")}and @PR_Date_Begin <= cast(p.RequestDate as date)
//	{(request.PR_Date_End is null ? "--" : "")}and cast(p.RequestDate as date) <= @PR_Date_End
//)

//, w_argm as
//(
//	select
//	1 as x
//	, argm.ApprovalRequestId as _ApprovalRequestId
//	, argm.LastUpdatedTime as 'Approver_Date'
//	, ua.Username as 'Approver_Name'

//	from ApprovalRequestGroupMember as argm

//	inner join (
//		select
//		1 as x
//		, argm.ApprovalRequestId
//		, max(argm.Id) as Id

//		from ApprovalRequestGroupMember as argm
//		where argm.Status not in (0,1)
//		group by argm.ApprovalRequestId
//	) as current_argm
//	on current_argm.Id = argm.Id

//	inner join Flips.UserAccount as ua
//	on ua.Id = argm.AccountId
//)

//-- w_type_process = po / pons / gl / (pap)
//, w_type_process as
//(
//	select
//	1 as x
//	, cp_sc.Id as _CategoryProcess_SubCategoryId
//	, cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode
//	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
//	, tp_sc.Id as _TypeProcess_SubCategoryId
//	, tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode
//	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName

//	-- PurchaseOrder
//	, pod.PurchaseRequestItemDetailId as _PurchaseRequestItemDetailId
//	, pod.Id as _PurchaseOrderDetailId
//	, pot.Id as _PurchaseOrderTOPId
//	-- PONonShopping
//	, null as _PONonShoppingDetailId
//	, null as _PONonShoppingTOPId
//	-- PONonShopping, GuaranteeLetter
//	, null as _PRFSummaryDetailId
//	-- Contract
//	, null as _PRFSummaryId
//	-- PAP
//	, null as _PRFVendorQuotationDetailId
//	--

//	, po.Id as _Id
//	, pod.Id as _DetailId
	
//	, tp_sc.SubCategoryName as 'Order_Type'
//	, po.PONumber as 'Order_No'
//	, mt.ShortDescription as 'Order_Status'
//	, po.PoDate as 'Order_Date'
//	, (pod.Qty * pod.ItemPrice /* * pod.RateAmount */) as 'Order_Grand_Total_Amount'
//	, po.ApproverDate as 'Approver_Date'
//	, ua.Username as 'Approver_Name'

//	from PurchaseOrder as po
	
//	inner join Flips.UserAccount as ua
//	on ua.Id = po.ApproverAccountId
	
//	inner join MasterTable as mt
//	on mt.Category = 'PurchaseOrder.Status'
//	and mt.ValueId = po.Status
//	{(request.Order_Status_ValueId is null ? "--" : "")}and mt.ValueId = @Order_Status_ValueId
	
//	inner join PurchaseOrderTOP as pot
//	on pot.PurchaseOrderId = po.Id
	
//	inner join PurchaseOrderDetail as pod
//	on pod.PurchaseOrderId = po.Id
	
//	inner join SubCategory as cp_sc
//	on cp_sc.SubCategoryCode = 'SC-2024-02-01261' -- Shopping Cart
	
//	inner join SubCategory as tp_sc
//	on tp_sc.SubCategoryCode = 'SC-2023-08-11132' -- Purchase Order
//	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id
	
//	where 1 = 1
//	{(string.IsNullOrWhiteSpace(request.Order_No) ? "--" : "")}and po.PONumber like @Order_No
//	{(request.Order_Date_Begin is null ? "--" : "")}and @Order_Date_Begin <= cast(po.PoDate as date)
//	{(request.Order_Date_End is null ? "--" : "")}and cast(po.PoDate as date) <= @Order_Date_End

//	union all

//	select
//	1 as x
//	, cp_sc.Id as _CategoryProcess_SubCategoryId
//	, cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode
//	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
//	, tp_sc.Id as _TypeProcess_SubCategoryId
//	, tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode
//	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName

//	-- PurchaseOrder
//	, null as _PurchaseRequestItemDetailId
//	, null as _PurchaseOrderDetailId
//	, null as _PurchaseOrderTOPId
//	-- PONonShopping
//	, ponsd.Id as _PONonShoppingDetailId
//	, ponst.Id as _PONonShoppingTOPId
//	-- PONonShopping , GuaranteeLetter
//	, ponsd.PRFSummaryDetailId as _PRFSummaryDetailId
//	-- Contract
//	, null as _PRFSummaryId
//	-- PAP
//	, null as _PRFVendorQuotationDetailId
//	--

//	, pons.Id as _Id
//	, ponsd.Id as _DetailId
	
//	, tp_sc.SubCategoryName as 'Order_Type'
//	, pons.PONumber as 'Order_No'
//	, mt.ShortDescription as 'Order_Status'
//	, pons.PoDate as 'Order_Date'
//	--, (ponsd.Qty * ponsd.ItemPrice /* * ponsd.RateAmount */) + ponsioc.Amount_sum as 'Order_Grand_Total_Amount'
//	, pons.TotalAmount as 'Order_Grand_Total_Amount'
//	, pons.ApproverDate as 'Approver_Date'
//	, ua.Username as 'Approver_Name'

//	from PONonShopping as pons
	
//	inner join Flips.UserAccount as ua
//	on ua.Id = pons.ApproverAccountId
	
//	inner join MasterTable as mt
//	on mt.Category = 'PurchaseOrder.Status'
//	and mt.ValueId = pons.Status
//	{(request.Order_Status_ValueId is null ? "--" : "")}and mt.ValueId = @Order_Status_ValueId
	
//	inner join PONonShoppingTOP as ponst
//	on ponst.PONonShoppingId = pons.Id
	
//	inner join PONonShoppingDetail as ponsd
//	on ponsd.PONonShoppingId = pons.Id
	
//	inner join (
//		select ponsioc.PONonShoppingDetailId
//		, sum(coalesce(ponsioc.Amount,0)) as Amount_sum
//		from PONonShoppingItemOtherCost as ponsioc
//		group by ponsioc.PONonShoppingDetailId
//	) as ponsioc
//	on ponsioc.PONonShoppingDetailId = ponsd.Id

//	inner join SubCategory as cp_sc
//	on cp_sc.SubCategoryCode = 'SC-2024-02-01262' -- Non Shopping Cart
	
//	inner join SubCategory as tp_sc
//	on tp_sc.SubCategoryCode = 'SC-2023-08-11132' -- Purchase Order
//	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id
	
//	where 1 = 1
//	{(string.IsNullOrWhiteSpace(request.Order_No) ? "--" : "")}and pons.PONumber like @Order_No
//	{(request.Order_Date_Begin is null ? "--" : "")}and @Order_Date_Begin <= cast(pons.PoDate as date)
//	{(request.Order_Date_End is null ? "--" : "")}and cast(pons.PoDate as date) <= @Order_Date_End

//	union all

//	select
//	1 as x
//	, cp_sc.Id as _CategoryProcess_SubCategoryId
//	, cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode
//	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
//	, tp_sc.Id as _TypeProcess_SubCategoryId
//	, tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode
//	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName

//	-- PurchaseOrder
//	, null as _PurchaseRequestItemDetailId
//	, null as _PurchaseOrderDetailId
//	, null as _PurchaseOrderTOPId
//	-- PONonShopping
//	, null as _PONonShoppingDetailId
//	, null as _PONonShoppingTOPId
//	-- PONonShopping , GuaranteeLetter
//	, gld.PRFSummaryDetailId as _PRFSummaryDetailId
//	-- Contract
//	, null as _PRFSummaryId
//	-- PAP
//	, null as _PRFVendorQuotationDetailId
//	--

//	, gl.Id as _Id
//	, gld.Id as _DetailId

//	, tp_sc.SubCategoryName as 'Order_Type'
//	, gl.GLNumber as 'Order_No'
//	, mt.ShortDescription as 'Order_Status'
//	, gl.GLDate as 'Order_Date'
//	, (gld.Qty * gld.ItemPrice /* * gld.RateAmount */) as 'Order_Grand_Total_Amount'
//	, argm.Approver_Date as 'Approver_Date'
//	, argm.Approver_Name as 'Approver_Name'

//	from GuaranteeLetter as gl

//	inner join MasterTable as mt
//	on mt.Category = 'GuaranteeLetter.Status'
//	and mt.ValueId = gl.Status
//	{(request.Order_Status_ValueId is null ? "--" : "")}and mt.ValueId = @Order_Status_ValueId
	
//	inner join GuaranteeLetterDetail as gld
//	on gld.GuaranteeLetterId = gl.Id
	
//	inner join SubCategory as cp_sc
//	on cp_sc.SubCategoryCode = 'SC-2024-02-01262' -- Non Shopping Cart
	
//	inner join SubCategory as tp_sc
//	on tp_sc.SubCategoryCode = 'SC-2023-08-11133' -- GL
//	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id
	
//	left join w_argm as argm
//	on argm._ApprovalRequestId = gl.ApprovalRequestId
	
//	where 1 = 1
//	{(string.IsNullOrWhiteSpace(request.Order_No) ? "--" : "")}and gl.GLNumber like @Order_No
//	{(request.Order_Date_Begin is null ? "--" : "")}and @Order_Date_Begin <= cast(gl.GLDate as date)
//	{(request.Order_Date_End is null ? "--" : "")}and cast(gl.GLDate as date) <= @Order_Date_End

//	union all

//	select
//	1 as x
//	, cp_sc.Id as _CategoryProcess_SubCategoryId
//	, cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode
//	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
//	, tp_sc.Id as _TypeProcess_SubCategoryId
//	, tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode
//	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName

//	-- PurchaseOrder
//	, null as _PurchaseRequestItemDetailId
//	, null as _PurchaseOrderDetailId
//	, null as _PurchaseOrderTOPId
//	-- PONonShopping
//	, null as _PONonShoppingDetailId
//	, null as _PONonShoppingTOPId
//	-- PONonShopping , GuaranteeLetter
//	, null as _PRFSummaryDetailId
//	-- Contract
//	, c.PRFSummaryId as _PRFSummaryId
//	-- PAP
//	, null as _PRFVendorQuotationDetailId
//	--

//	, c.Id as _Id
//	, null as _DetailId

//	, tp_sc.SubCategoryName as 'Order_Type'
//	, c.ContractNo as 'Order_No'
//	, mt.ShortDescription as 'Order_Status'
//	, c.UploadFinalDate as 'Order_Date'
//	, c.AmountContract as 'Order_Grand_Total_Amount'
//	, null as 'Approver_Date'
//	, null as 'Approver_Name'

//	from Contract as c

//	inner join MasterTable as mt
//	on mt.Category = 'Contract.Status'
//	and mt.ValueId = c.Status
//	{(request.Order_Status_ValueId is null ? "--" : "")}and mt.ValueId = @Order_Status_ValueId

//	inner join SubCategory as cp_sc
//	on cp_sc.SubCategoryCode = 'SC-2024-02-01262' -- Non Shopping Cart

//	inner join SubCategory as tp_sc
//	on tp_sc.SubCategoryCode = 'SC-2023-08-11131' -- Contract
//	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id

//	where 1 = 1
//	{(string.IsNullOrWhiteSpace(request.Order_No) ? "--" : "")}and c.ContractNo like @Order_No
//	{(request.Order_Date_Begin is null ? "--" : "")}and @Order_Date_Begin <= cast(c.UploadFinalDate as date)
//	{(request.Order_Date_End is null ? "--" : "")}and cast(c.UploadFinalDate as date) <= @Order_Date_End

//	union all

//	select
//	1 as x
//	, cp_sc.Id as _CategoryProcess_SubCategoryId
//	, cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode
//	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName
//	, tp_sc.Id as _TypeProcess_SubCategoryId
//	, tp_sc.SubCategoryCode as _TypeProcess_SubCategoryCode
//	, tp_sc.SubCategoryName as _TypeProcess_SubCategoryName

//	-- PurchaseOrder
//	, null as _PurchaseRequestItemDetailId
//	, null as _PurchaseOrderDetailId
//	, null as _PurchaseOrderTOPId
//	-- PONonShopping
//	, null as _PONonShoppingDetailId
//	, null as _PONonShoppingTOPId
//	-- PONonShopping , GuaranteeLetter
//	, null as _PRFSummaryDetailId
//	-- Contract
//	, null as _PRFSummaryId
//	-- PAP
//	, papd.PRFVendorQuotationDetailId as _PRFVendorQuotationDetailId
//	--

//	, pap.Id as _Id
//	, papd.Id as _DetailId

//	, tp_sc.SubCategoryName as 'Order_Type'
//	, pap.PAPNo as 'Order_No'
//	, mt.ShortDescription as 'Order_Status'
//	, pap.GenerateDate as 'Order_Date'
//	, papd.TotalBaseAmount as 'Order_Grand_Total_Amount'
//	, argm.Approver_Date as 'Approver_Date'
//	, argm.Approver_Name as 'Approver_Name'

//	from PAP as pap

//	inner join SubCategory as cp_sc
//	on cp_sc.SubCategoryCode = 'SC-2024-02-01262' -- Non Shopping Cart

//	inner join SubCategory as tp_sc
//	on tp_sc.SubCategoryCode = 'SC-2023-08-11134' -- PAP
//	{(request.Order_Type_Id is null ? "--" : "")}and tp_sc.Id = @Order_Type_Id

//	inner join MasterTable as mt
//	on mt.Category = 'PRFSummary.Status'
//	and mt.ValueId = pap.Status
//	{(request.Order_Status_ValueId is null ? "--" : "")}and mt.ValueId = @Order_Status_ValueId

//	left join w_argm as argm
//	on argm._ApprovalRequestId = pap.ApprovalRequestId

//	inner join PAPDetail as papd
//	on papd.PAPId = pap.Id

//	where 1 = 1
//	{(string.IsNullOrWhiteSpace(request.Order_No) ? "--" : "")}and pap.PAPNo like @Order_No
//	{(request.Order_Date_Begin is null ? "--" : "")}and @Order_Date_Begin <= cast(pap.GenerateDate as date)
//	{(request.Order_Date_End is null ? "--" : "")}and cast(pap.GenerateDate as date) <= @Order_Date_End
//)

//, w_dn as
//(
//	select
//	1 as x
//	, cp_sc.Id as _CategoryProcess_SubCategoryId
//	, cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode
//	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName

//	, dnd.PurchaseOrderDetailId as _PurchaseOrderDetailId
//	, dnp.PurchaseOrderTOPId as _PurchaseOrderTOPId

//	, dn.Id as _Id
//	, dnd.Id as _DetailId
	
//	, dn.DeliveryNumber as 'DN_No'
//	, mt.Name as 'DN_Status'
//	, dnd.ReceivedDate as 'DN_Date'
//	, dnd.QtyReceive as 'DN_Qty'
//	from DeliveryNotesDetail as dnd
	
//	inner join MasterTable as mt
//	on mt.Category = 'DeliveryNote.Status'
//	and mt.ValueId = dnd.Status
	
//	inner join DeliveryNotesPayment as dnp
//	on dnp.Id = dnd.DeliveryNotesPaymentId
//	--and dnp.PurchaseOrderTOPId -- ignore ?
	
//	inner join DeliveryNotes as dn
//	on dn.Id = dnp.DeliveryNotesId

//	inner join SubCategory as cp_sc
//	on cp_sc.Id = dn.CategoryProcess_SubCategoryId
//)

//, w_ipo as
//(
//	select
//	1 as x
//	, cp_sc.Id as _CategoryProcess_SubCategoryId
//	, cp_sc.SubCategoryCode as _CategoryProcess_SubCategoryCode
//	, cp_sc.SubCategoryName as _CategoryProcess_SubCategoryName

//	, ipo.PurchaseOrderTOPId as _PurchaseOrderTOPId
//	, ipo.Id as _InvoicePOId
//	, ipod.Id as _InvoicePODetailId
//	, ipod.PODetailId as _PODetailId

//	, ipo.InvoiceNumber as 'Invoice_No'
//	, mt.Name as 'Invoice_Status'
//	, ipo.InvoiceDate as 'Invoice_Date'
//	, ipo.InvoiceAmmount as 'Invoice_Amount'
//	, ipoioc.PPN as 'PPn'
//	, ipoioc.PPH23 as 'PPh_23'
//	, ipoioc.PPH42 as 'PPh_42'
//	, ipo.Remark as 'Remarks'
//	, (ipo.InvoiceAmmount + (sum(ipoioc.PPN + ipoioc.PPH23 + ipoioc.PPH42) over(partition by ipo.Id))) as 'Invoice_After_Tax_Or_Grand_Total'
	
//	from InvoicePO as ipo
	
//	inner join SubCategory as cp_sc
//	on cp_sc.Id = ipo.CategoryProcess_SubCategoryId

//	inner join MasterTable as mt
//	on mt.Category = 'InvoiceManagement.Status'
//	and mt.ValueId = ipo.Status
	
//	inner join InvoicePODetail as ipod
//	on ipod.InvoicePOId = ipo.Id

//	inner join
//	(
//		select
//		p.InvoicePODetailId
//		, coalesce(p.PPN , 0) as 'PPN'
//		, coalesce(p.PPH23 , 0) as 'PPH23'
//		, coalesce(p.PPH42 , 0) as 'PPH42'
//		from
//		(
//			select
//			ipoioc.InvoicePODetailId
//			, (
//				case sc.SubCategoryCode
//				when 'SC-2023-10-01228' then 'PPN'
//				when 'SC-2023-10-01230' then 'PPH23'
//				when 'SC-2023-10-01229' then 'PPH42'
//				else sc.SubCategoryCode
//				end
//			) as 'Pivoted'
//			, ipoioc.Amount
//			from InvoicePOItemOtherCost as ipoioc
			
//			inner join SubCategory as sc
//			on sc.SubCategoryCode in ('PPN','PPH23','PPH42','SC-2023-10-01230','SC-2023-10-01229','SC-2023-10-01228')
//			and sc.id = ipoioc.OtherCost_SubCategoryId
//		) as s
//		pivot (max(s.Amount) for Pivoted in ([PPN],[PPH23],[PPH42])) as p
//	) as ipoioc on ipoioc.InvoicePODetailId = ipod.Id

//)

//--
//select
//1 as x

//, x.Count

//, x.PR_No
//, x.PR_Status
//, x.PR_Date
//, x.Requester
//, x.Department
//, x.Type_Of_Transaction
//, x.Buyer_User_Name

//, (case when x.PR_No_partition_row_number <> 1 then null else x.Total_Budget_Estimation end) as Total_Budget_Estimation

//, x.Critical
//, x.DPIA
//, x.VSDDT
//, x.Outsourcing_Status
//, x.Category
//, x.Item_Name
//, x.Account_Code
//, x.Cost_Center
//, x.Vendor_Selection
//, x.Currency
//, x.PR_Posted_Date
//, x.Delivery_Request_Date
//, x.Final_Spec_Req_Date
//, x.Generate_Proc_Sum_Date

//, x.TAT_WD
//, x.SLA_WD
//, x.SLA_Status
//, x.Vendor
//, x.Selected

//, (case when x._pr_partition_row_number <> 1 then null else x.Total_Price end) as Total_Price
//--, (case when x._pr_detail_partition_row_number <> 1 then null else x.Price_Per_Item end) as Price_Per_Item
//, x.Price_Per_Item
//, (case when x._pr_partition_row_number <> 1 then null else x.Total_Price_Inc_Other_Cost end) as Total_Price_Inc_Other_Cost
//--, (case when x._pr_detail_partition_row_number <> 1 then null else x.Price_Per_Item_Inc_Other_Cost end) as Price_Per_Item_Inc_Other_Cost
//, x.Price_Per_Item_Inc_Other_Cost
//, (case when x._pr_partition_row_number <> 1 then null else x.Realised_Saving end) as Realised_Saving

//, x.Order_Type
//, x.Order_No
//, x.Order_Status
//, x.Order_Date
//, (case when x.Order_No_partition_row_number <> 1 then null else x.Order_Grand_Total_Amount end) as Order_Grand_Total_Amount
//, x.Approver_Date
//, x.Approver_Name

//, x.DN_No
//, x.DN_Status
//, x.DN_Date
//, x.DN_Qty

//, x.Invoice_No
//, x.Invoice_Status
//, x.Invoice_Date
//, (case when x.Order_No_partition_row_number <> 1 then null else x.Invoice_Amount end) as Invoice_Amount
//, x.PPn
//, x.PPh_23
//, x.PPh_42
//, (case when x.Order_No_partition_row_number <> 1 then null else x.Invoice_After_Tax_Or_Grand_Total end) as Invoice_After_Tax_Or_Grand_Total
//, x.Remarks

//, x.ReasonCancel

//from (

//	select
//	1 as x
//	, count(*) over() as 'Count'

//	, row_number() over(partition by pr.PR_No order by pr.PR_No) as PR_No_partition_row_number
//	, row_number() over(partition by pr.PR_No , dn._Id order by pr.PR_No) as PR_No_DN_Id_partition_row_number
//	, row_number() over(partition by tp.Order_No order by tp.Order_No) as Order_No_partition_row_number

//	, row_number() over(
//		partition by
//		pr._CategoryProcess_SubCategoryId
//		, pr._TypeProcess_SubCategoryId
//		, pr._Id
//		order by
//		pr._CategoryProcess_SubCategoryId
//		, pr._TypeProcess_SubCategoryId
//		, pr._Id
//	) as _pr_partition_row_number
//	, row_number() over(
//		partition by
//		pr._CategoryProcess_SubCategoryId
//		, pr._TypeProcess_SubCategoryId
//		, pr._Id
//		, pr._DetailId
//		order by
//		pr._CategoryProcess_SubCategoryId
//		, pr._TypeProcess_SubCategoryId
//		, pr._Id
//		, pr._DetailId
//	) as _pr_detail_partition_row_number

//	, pr.PR_No
//	, pr.PR_Status
//	, pr.PR_Date
//	, pr.Requester
//	, pr.Department
//	, pr.Type_Of_Transaction
//	, pr.Buyer_User_Name

//	-- top 1 row only
//	, pr.Total_Budget_Estimation

//	, pr.Critical
//	, pr.DPIA
//	, pr.VSDDT
//	, pr.Outsourcing_Status
//	, pr.Category
//	, pr.Item_Name
//	, pr.Account_Code
//	, pr.Cost_Center
//	, pr.Vendor_Selection
//	, pr.Currency
//	, pr.PR_Posted_Date
//	, pr.Delivery_Request_Date
//	, pr.Final_Spec_Req_Date
//	, pr.Generate_Proc_Sum_Date

//	, pr.TAT_WD
//	, pr.SLA_WD
//	, pr.SLA_Status
//	, pr.Vendor
//	, pr.Selected

//	, pr.Total_Price
//	, pr.Price_Per_Item
//	, pr.Total_Price_Inc_Other_Cost
//	, pr.Price_Per_Item_Inc_Other_Cost
//	, pr.Realised_Saving

//	, pr._TypeProcess_SubCategoryName as Order_Type
//	--, tp.Order_Type
//	, tp.Order_No
//	, tp.Order_Status
//	, tp.Order_Date
//	, tp.Order_Grand_Total_Amount
//	, tp.Approver_Date
//	, tp.Approver_Name

//	, dn.DN_No
//	, dn.DN_Status
//	, dn.DN_Date
//	, dn.DN_Qty

//	, ipo.Invoice_No
//	, ipo.Invoice_Status
//	, ipo.Invoice_Date
//	, ipo.Invoice_Amount
//	, ipo.PPn
//	, ipo.PPh_23
//	, ipo.PPh_42
//	, ipo.Invoice_After_Tax_Or_Grand_Total
//	, ipo.Remarks

//	, pr.ReasonCancel

//	from w_pr as pr

//	left join w_type_process as tp
//	on tp._CategoryProcess_SubCategoryId = pr._CategoryProcess_SubCategoryId
//	and tp._TypeProcess_SubCategoryId = pr._TypeProcess_SubCategoryId

//	and
//	(
//	case
//	when pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and pr._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261' then tp._PurchaseRequestItemDetailId
//	when pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and pr._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262' then tp._PRFSummaryDetailId
//	when pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11133' then tp._PRFSummaryDetailId
//	when pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11131' then tp._PRFSummaryId
//	when pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11134' then tp._PRFVendorQuotationDetailId
//	else null
//	end
//	)
//	=
//	(
//	case
//	when pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and pr._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261' then pr._PurchaseRequestItemDetailId
//	when pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and pr._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262' then pr._PRFSummaryDetailId
//	when pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11133' then pr._PRFSummaryDetailId
//	when pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11131' then pr._PRFSummaryId
//	when pr._TypeProcess_SubCategoryCode = 'SC-2023-08-11134' then pr._PRFVendorQuotationDetailId
//	else null
//	end
//	)

//	/*

//	category
//	SC-2024-02-01261 Shopping Cart
//	SC-2024-02-01262 Non Shopping Cart

//	type
//	SC-2023-08-11132 Purchase Order
//	SC-2023-08-11133 GL
//	SC-2023-08-11131 Contract
//	SC-2023-08-11134 PAP
//	SC-2023-08-11135 PO PADI < 10 Juta
//	SC-2023-08-11136 PO PADI > 10 Juta

//		-- PurchaseOrder
//		, null as _PurchaseRequestItemDetailId
//		, null as _PurchaseOrderDetailId
//		, null as _PurchaseOrderTOPId
//		-- PONonShopping
//		, null as _PONonShoppingDetailId
//		, null as _PONonShoppingTOPId
//		-- PONonShopping, GuaranteeLetter
//		, null as _PRFSummaryDetailId
//		-- Contract
//		, null as _PRFSummaryId
//		-- PAP
//		, null as _PRFVendorQuotationDetailId
//		--

//	*/

//	left join w_dn as dn
//	on dn._CategoryProcess_SubCategoryId = tp._CategoryProcess_SubCategoryId
//	and
//	(
//	case
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261' then dn._PurchaseOrderTOPId
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262' then dn._PurchaseOrderTOPId -- emang sama
//	else null
//	end
//	)
//	=
//	(
//	case
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261' then tp._PurchaseOrderTOPId
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262' then tp._PONonShoppingTOPId
//	else null
//	end
//	)
//	and
//	(
//	case
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261' then dn._PurchaseOrderDetailId
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262' then dn._PurchaseOrderDetailId -- emang sama
//	else null
//	end
//	)
//	=
//	(
//	case
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261' then tp._PurchaseOrderDetailId
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262' then tp._PONonShoppingDetailId
//	else null
//	end
//	)

//	left join w_ipo as ipo
//	on 1 = 1
//	and ipo._CategoryProcess_SubCategoryId = tp._CategoryProcess_SubCategoryId
//	and
//	(
//	case
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261' then ipo._PurchaseOrderTOPId
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262' then ipo._PurchaseOrderTOPId -- emang sama
//	else null
//	end
//	)
//	=
//	(
//	case
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261' then tp._PurchaseOrderTOPId
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262' then tp._PONonShoppingTOPId
//	else null
//	end
//	)

//	and
//	(
//	case
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261' then ipo._PODetailId
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262' then ipo._PODetailId -- emang sama
//	else null
//	end
//	)
//	=
//	(
//	case
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01261' then tp._DetailId
//	when tp._TypeProcess_SubCategoryCode = 'SC-2023-08-11132' and tp._CategoryProcess_SubCategoryCode = 'SC-2024-02-01262' then tp._DetailId
//	else null
//	end
//	)

//	where pr.PR_Date >= '2024-01-01' 
//	{(request.Order_Type_Id is null ? "--" : "")}and pr._TypeProcess_SubCategoryId = @Order_Type_Id
//	{(string.IsNullOrWhiteSpace(request.Order_No) ? "--" : "")}and tp.Order_No like @Order_No
//	{(request.Order_Date_Begin is null ? "--" : "")}and cast(tp.Order_Date as date) >= @Order_Date_Begin
//	{(request.Order_Date_End is null ? "--" : "")}and cast(tp.Order_Date as date) <= @Order_Date_End
//	{(request.Vendor_Id is null ? "--" : "")}and pr._VendorId = @Vendor_Id


//	order by
//	pr.PR_Date desc

//	offset @Start rows
//	fetch next @Length rows only

//) as x
//order by
//x.PR_Date desc
//, x._pr_partition_row_number asc

//";

//                //request.Columns[0].Data
//                string order = (request.Order ?? Array.Empty<Models.DataTables.DtOrder>())
//                    .Select((o, i) => ((request.Columns ?? Array.Empty<Models.DataTables.DtColumn>())[o.Column].Data == 0.ToString()) ? string.Empty : $"/* {i} */ {(request.Columns ?? Array.Empty<Models.DataTables.DtColumn>())[o.Column].Data} {o.Dir}")
//                    .Where(s => !string.IsNullOrWhiteSpace(s))
//                    .Aggregate(string.Empty, (a, s) => string.IsNullOrWhiteSpace(a) ? s : $"{a}{Environment.NewLine},{s}");

//                q = string.IsNullOrWhiteSpace(order) ? q.Replace($"-- default order --", string.Empty) : q.Replace($"-- input order -- ", order);

//                q = (request.Length == 0) ? q.Replace("fetch next", "-- fetch next") : q;

//                Dapper.DynamicParameters p = new(new
//                {
//                    request.Start,
//                    request.Length,
//                    request.PR_Category_Id,
//                    PR_No = $"%{request.PR_No}%",
//                    request.PR_Status_ValueId,
//                    PR_Date_Begin = request.PR_Date_Begin?.ToString("yyyy-MM-dd"),
//                    PR_Date_End = request.PR_Date_End?.ToString("yyyy-MM-dd"),

//                    request.Department_Id,
//                    request.Account_Code_Id,
//                    request.Cost_Center_Id,
//                    request.Vendor_Id,

//                    request.Order_Type_Id,
//                    Order_No = $"%{request.Order_No}%",
//                    request.Order_Status_ValueId,
//                    Order_Date_Begin = request.Order_Date_Begin?.ToString("yyyy-MM-dd"),
//                    Order_Date_End = request.Order_Date_End?.ToString("yyyy-MM-dd")

//                });
//                return await Task.FromResult(_dapper.GetAll<Models.Report.Procurement.DataTables.Data_WIP>(q, p));
//            }
//            catch (Exception e)
//            {
//                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
//            }
//		}

		public async Task<IEnumerable<Models.Report.Procurement.DataTables.Data_WIP>> ProcurementDataTablesData_WIP(Models.Report.Procurement.DataTables.Request_WIP request)
		{
			try
			{
				string query = ReportPurchaseOrder.GetQueryReportPurchaseOrder(request);
				Dapper.DynamicParameters param = new(new
				{
					request.Start,
					request.Length,
					request.PR_Category_Id,
					PR_No = $"%{request.PR_No}%",
					request.PR_Status_ValueId,
					PR_Date_Begin = request.PR_Date_Begin?.ToString("yyyy-MM-dd"),
					PR_Date_End = request.PR_Date_End?.ToString("yyyy-MM-dd"),

					request.Department_Id,
					request.Account_Code_Id,
					request.Cost_Center_Id,
					request.Vendor_Id,

					request.Order_Type_Id,
					Order_No = $"%{request.Order_No}%",
					request.Order_Status_ValueId,
					Order_Date_Begin = request.Order_Date_Begin?.ToString("yyyy-MM-dd"),
					Order_Date_End = request.Order_Date_End?.ToString("yyyy-MM-dd")
				});
				return await Task.FromResult(_dapper.GetAll<Models.Report.Procurement.DataTables.Data_WIP>(query, param));
			}
			catch(Exception ex)
			{
				throw new GlobalExceptions(nameof(ReportRepository), ex.InnerException);
			}
		}

		public string SetAttachmentIds(string attachmentIds)
        {
            List<AttachmentIds> attachmentList = [];
            if (!string.IsNullOrEmpty(attachmentIds))
                attachmentList = JsonConvert.DeserializeObject<List<AttachmentIds>>(attachmentIds);
            List<string> attachments = [];
            foreach (AttachmentIds attachmentId in attachmentList)
            {
                if (!string.IsNullOrEmpty(attachmentId.AttachmentIdRequest))
                    attachments.Add(attachmentId.AttachmentIdRequest);
                if (!string.IsNullOrEmpty(attachmentId.AttachmentIdExpenseDetail))
                    attachments.Add(attachmentId.AttachmentIdExpenseDetail);
                if (!string.IsNullOrEmpty(attachmentId.AttachmentIdTransportation))
                    attachments.Add(attachmentId.AttachmentIdTransportation);
                if (!string.IsNullOrEmpty(attachmentId.AttachmentIdAccommodation))
                    attachments.Add(attachmentId.AttachmentIdAccommodation);
                if (!string.IsNullOrEmpty(attachmentId.AttachmentIdVoucher))
                    attachments.Add(attachmentId.AttachmentIdVoucher);
            }
            attachmentIds = JsonConvert.SerializeObject(attachments);
            return attachmentIds;
        }


        #region Refactor for Background Job
        public async Task<MemoryStream> ExportProcurementWipToExcel(Models.Report.Procurement.DataTables.Request_WIP request)
        {
            try
            {
                log.LogInitialize(nameof(ExportProcurementWipToExcel), "Start Export Procurement WIP", LogType.Info);

                var list = await ProcurementDataTablesData_WIP(request);
                var stream = new MemoryStream();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                string wsName = "Report PO";

                using (ExcelPackage ep = new(stream))
                {
                    var ws = ep.Workbook.Worksheets.Add(wsName);
                    ws.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Top;

                    int r = 0;
                    int c = 1;
                    var d = new Dictionary<string, int>();

                    #region Column Mapping
                    var m = new Models.Report.Procurement.DataTables.Data_WIP();

                    d.Add(nameof(m.PR_No), ++c);
                    d.Add(nameof(m.PR_Status), ++c);
                    d.Add(nameof(m.PR_Date), ++c);
                    d.Add(nameof(m.Requester), ++c);
                    d.Add(nameof(m.Department), ++c);
                    d.Add(nameof(m.Type_Of_Transaction), ++c);
                    d.Add(nameof(m.Buyer_User_Name), ++c);
                    d.Add(nameof(m.Total_Budget_Estimation), ++c);
                    d.Add(nameof(m.Critical), ++c);
                    d.Add(nameof(m.DPIA), ++c);
                    d.Add(nameof(m.VSDDT), ++c);
                    d.Add(nameof(m.Outsourcing_Status), ++c);
                    d.Add(nameof(m.Category), ++c);
                    d.Add(nameof(m.Item_Name), ++c);
                    d.Add(nameof(m.Account_Code), ++c);
                    d.Add(nameof(m.Cost_Center), ++c);
                    d.Add(nameof(m.Vendor_Selection), ++c);
                    d.Add(nameof(m.Currency), ++c);
                    d.Add(nameof(m.PR_Posted_Date), ++c);
                    d.Add(nameof(m.Delivery_Request_Date), ++c);
                    d.Add(nameof(m.Final_Spec_Req_Date), ++c);
                    d.Add(nameof(m.Generate_Proc_Sum_Date), ++c);
                    d.Add(nameof(m.TAT_WD), ++c);
                    d.Add(nameof(m.SLA_WD), ++c);
                    d.Add(nameof(m.SLA_Status), ++c);
                    d.Add(nameof(m.Vendor), ++c);
                    d.Add(nameof(m.Selected), ++c);
                    d.Add(nameof(m.Price_Per_Item), ++c);
                    d.Add(nameof(m.Total_Price), ++c);
                    d.Add(nameof(m.Price_Per_Item_Inc_Other_Cost), ++c);
                    d.Add(nameof(m.Total_Price_Inc_Other_Cost), ++c);
                    d.Add(nameof(m.Realised_Saving), ++c);
                    d.Add(nameof(m.Order_Type), ++c);
                    d.Add(nameof(m.Order_No), ++c);
                    d.Add(nameof(m.Order_Status), ++c);
                    d.Add(nameof(m.Order_Date), ++c);
                    d.Add(nameof(m.Order_Grand_Total_Amount), ++c);
                    d.Add(nameof(m.Approver_Date), ++c);
                    d.Add(nameof(m.Approver_Name), ++c);
                    d.Add(nameof(m.DN_No), ++c);
                    d.Add(nameof(m.DN_Status), ++c);
                    d.Add(nameof(m.DN_Date), ++c);
                    d.Add(nameof(m.DN_Qty), ++c);
                    d.Add(nameof(m.Invoice_No), ++c);
                    d.Add(nameof(m.Invoice_Status), ++c);
                    d.Add(nameof(m.Invoice_Date), ++c);
                    d.Add(nameof(m.Invoice_Amount), ++c);
                    d.Add(nameof(m.PPn), ++c);
                    d.Add(nameof(m.PPh_23), ++c);
                    d.Add(nameof(m.PPh_42), ++c);
                    d.Add(nameof(m.Invoice_After_Tax_Or_Grand_Total), ++c);
                    d.Add(nameof(m.Remarks), ++c);
                    d.Add(nameof(m.ReasonCancel), c + 1);
                    #endregion

                    #region Header Section
                    ws.Cells[++r, 1].Value = wsName;
                    ws.Cells[++r, 1].Value = "APS Revamp";
                    ws.Cells[++r, 1].Value = $"Date : {DateTime.Now.ToString("yyyy-MM-dd")}";
                    ws.Cells[++r, 1].Value = $"PR Category : {request.PR_Category_Text}";
                    ws.Cells[++r, 1].Value = $"PR No : {request.PR_No}";
                    ws.Cells[++r, 1].Value = $"PR Status : {request.PR_Status_Text}";
                    ws.Cells[++r, 1].Value = $"PR Date From {request.PR_Date_Begin?.ToString("yyyy-MM-dd") ?? string.Empty} - To {request.PR_Date_End?.ToString("yyyy-MM-dd") ?? string.Empty}";
                    ws.Cells[++r, 1].Value = $"Departmet : {request.Department_Text}";
                    ws.Cells[++r, 1].Value = $"Account Code : {request.Account_Code_Text}";
                    ws.Cells[++r, 1].Value = $"Cost Center : {request.Cost_Center_Text}";
                    ws.Cells[++r, 1].Value = $"Vendor : {request.Vendor_Text}";
                    ws.Cells[++r, 1].Value = $"Order Type : {request.Order_Type_Text}";
                    ws.Cells[++r, 1].Value = $"Order No : {request.Order_No}";
                    ws.Cells[++r, 1].Value = $"Order Status : {request.Order_Status_Text}";
                    ws.Cells[++r, 1].Value = $"Order Date From {request.Order_Date_Begin?.ToString() ?? string.Empty} - To {request.Order_Date_End?.ToString() ?? string.Empty}";

                    // Header Row
                    ws.Cells[++r, 1].Value = "No";
                    ws.Columns[1].Width = 5;

                    foreach (var x in d)
                    {
                        ws.Cells[r, x.Value].Value = x.Key.Replace("_", " ");
                        if (x.Key == nameof(m.Price_Per_Item)) ws.Cells[r, x.Value].Value = "Unit Price";
                        if (x.Key == nameof(m.Price_Per_Item_Inc_Other_Cost)) ws.Cells[r, x.Value].Value = "Unit Price Inc Other Cost";
                    }
                    ws.Cells[r, 2, r, d.Count + 1].Style.WrapText = true;
                    #endregion

                    #region Column Widths
                    ws.Columns[d[nameof(m.PR_No)]].Width = 20;
                    ws.Columns[d[nameof(m.PR_Status)]].Width = 15;
                    ws.Columns[d[nameof(m.PR_Date)]].Width = 20;
                    ws.Columns[d[nameof(m.Requester)]].Width = 20;
                    ws.Columns[d[nameof(m.Department)]].Width = 30;
                    ws.Columns[d[nameof(m.Type_Of_Transaction)]].Width = 10;
                    ws.Columns[d[nameof(m.Buyer_User_Name)]].Width = 20;
                    ws.Columns[d[nameof(m.Total_Budget_Estimation)]].Width = 20;
                    ws.Columns[d[nameof(m.Critical)]].Width = 10;
                    ws.Columns[d[nameof(m.DPIA)]].Width = 10;
                    ws.Columns[d[nameof(m.VSDDT)]].Width = 10;
                    ws.Columns[d[nameof(m.Outsourcing_Status)]].Width = 10;
                    ws.Columns[d[nameof(m.Category)]].Width = 30;
                    ws.Columns[d[nameof(m.Item_Name)]].Width = 30;
                    ws.Columns[d[nameof(m.Account_Code)]].Width = 30;
                    ws.Columns[d[nameof(m.Cost_Center)]].Width = 30;
                    ws.Columns[d[nameof(m.Vendor_Selection)]].Width = 30;
                    ws.Columns[d[nameof(m.Currency)]].Width = 10;
                    ws.Columns[d[nameof(m.PR_Posted_Date)]].Width = 20;
                    ws.Columns[d[nameof(m.Delivery_Request_Date)]].Width = 20;
                    ws.Columns[d[nameof(m.Final_Spec_Req_Date)]].Width = 20;
                    ws.Columns[d[nameof(m.Generate_Proc_Sum_Date)]].Width = 20;
                    ws.Columns[d[nameof(m.TAT_WD)]].Width = 5;
                    ws.Columns[d[nameof(m.SLA_WD)]].Width = 5;
                    ws.Columns[d[nameof(m.SLA_Status)]].Width = 10;
                    ws.Columns[d[nameof(m.Vendor)]].Width = 30;
                    ws.Columns[d[nameof(m.Selected)]].Width = 10;
                    ws.Columns[d[nameof(m.Price_Per_Item)]].Width = 20;
                    ws.Columns[d[nameof(m.Total_Price)]].Width = 20;
                    ws.Columns[d[nameof(m.Price_Per_Item_Inc_Other_Cost)]].Width = 20;
                    ws.Columns[d[nameof(m.Total_Price_Inc_Other_Cost)]].Width = 20;
                    ws.Columns[d[nameof(m.Realised_Saving)]].Width = 20;
                    ws.Columns[d[nameof(m.Order_Type)]].Width = 20;
                    ws.Columns[d[nameof(m.Order_No)]].Width = 30;
                    ws.Columns[d[nameof(m.Order_Status)]].Width = 15;
                    ws.Columns[d[nameof(m.Order_Date)]].Width = 20;
                    ws.Columns[d[nameof(m.Order_Grand_Total_Amount)]].Width = 20;
                    ws.Columns[d[nameof(m.Approver_Date)]].Width = 20;
                    ws.Columns[d[nameof(m.Approver_Name)]].Width = 20;
                    ws.Columns[d[nameof(m.DN_No)]].Width = 20;
                    ws.Columns[d[nameof(m.DN_Status)]].Width = 10;
                    ws.Columns[d[nameof(m.DN_Date)]].Width = 20;
                    ws.Columns[d[nameof(m.DN_Qty)]].Width = 5;
                    ws.Columns[d[nameof(m.Invoice_No)]].Width = 20;
                    ws.Columns[d[nameof(m.Invoice_Status)]].Width = 10;
                    ws.Columns[d[nameof(m.Invoice_Date)]].Width = 20;
                    ws.Columns[d[nameof(m.Invoice_Amount)]].Width = 20;
                    ws.Columns[d[nameof(m.PPn)]].Width = 20;
                    ws.Columns[d[nameof(m.PPh_23)]].Width = 20;
                    ws.Columns[d[nameof(m.PPh_42)]].Width = 20;
                    ws.Columns[d[nameof(m.Invoice_After_Tax_Or_Grand_Total)]].Width = 20;
                    ws.Columns[d[nameof(m.Remarks)]].Width = 30;
                    ws.Columns[d[nameof(m.ReasonCancel)]].Width = 30;
                    #endregion

                    #region Header Styling
                    ws.Cells[r, 1, r, d.Count + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[r, 1, r, d.Count + 1].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);
                    ws.Cells[r, 1, r, d.Count + 1].Style.Font.Color.SetColor(Color.White);
                    ws.Cells[r, 2, r, d.Count + 1].AutoFilter = true;
                    #endregion

                    #region Body Data
                    int b = r;
                    foreach (var x in list)
                    {
                        ++r;
                        ws.Cells[r, 1].Value = r - b;
                        ws.Cells[r, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        // PR Status
                        ws.Cells[r, d[nameof(x.PR_No)]].Value = x.PR_No;
                        ws.Cells[r, d[nameof(x.PR_Status)]].Value = x.PR_Status;

                        if ((x.PR_Status ?? "").Trim().ToLower() == "cancel")
                        {
                            ws.Cells[r, 1, r, d.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[r, 1, r, d.Count].Style.Fill.BackgroundColor.SetColor(Color.Red);
                        }

                        // Date & Text Fields
                        SetDateCell(ws, r, d[nameof(x.PR_Date)], x.PR_Date, "yyyy-MM-dd HH:mm:ss");
                        ws.Cells[r, d[nameof(x.Requester)]].Value = x.Requester;
                        ws.Cells[r, d[nameof(x.Department)]].Value = x.Department;
                        ws.Cells[r, d[nameof(x.Type_Of_Transaction)]].Value = x.Type_Of_Transaction;
                        ws.Cells[r, d[nameof(x.Buyer_User_Name)]].Value = x.Buyer_User_Name;

                        // Currency Fields Helper
                        SetCurrencyCell(ws, r, d[nameof(x.Total_Budget_Estimation)], x.Total_Budget_Estimation);

                        ws.Cells[r, d[nameof(x.Critical)]].Value = x.Critical;
                        ws.Cells[r, d[nameof(x.DPIA)]].Value = x.DPIA;
                        ws.Cells[r, d[nameof(x.VSDDT)]].Value = x.VSDDT;
                        ws.Cells[r, d[nameof(x.Outsourcing_Status)]].Value = x.Outsourcing_Status;
                        ws.Cells[r, d[nameof(x.Category)]].Value = x.Category;
                        ws.Cells[r, d[nameof(x.Item_Name)]].Value = x.Item_Name;
                        ws.Cells[r, d[nameof(x.Account_Code)]].Value = x.Account_Code;
                        ws.Cells[r, d[nameof(x.Cost_Center)]].Value = x.Cost_Center;
                        ws.Cells[r, d[nameof(x.Vendor_Selection)]].Value = x.Vendor_Selection;
                        ws.Cells[r, d[nameof(x.Currency)]].Value = x.Currency;

                        // More Dates
                        SetDateCell(ws, r, d[nameof(x.PR_Posted_Date)], x.PR_Posted_Date, "yyyy-MM-dd HH:mm:ss");
                        SetDateCell(ws, r, d[nameof(x.Delivery_Request_Date)], x.Delivery_Request_Date, "yyyy-MM-dd");
                        SetDateCell(ws, r, d[nameof(x.Final_Spec_Req_Date)], x.Final_Spec_Req_Date, "yyyy-MM-dd");
                        SetDateCell(ws, r, d[nameof(x.Generate_Proc_Sum_Date)], x.Generate_Proc_Sum_Date, "yyyy-MM-dd");

                        // SLA/TAT
                        ws.Cells[r, d[nameof(x.TAT_WD)]].Value = x.TAT_WD;
                        ws.Cells[r, d[nameof(x.SLA_WD)]].Value = x.SLA_WD;

                        ws.Cells[r, d[nameof(x.SLA_Status)]].Value = x.SLA_Status;
                        ws.Cells[r, d[nameof(x.Vendor)]].Value = x.Vendor;
                        ws.Cells[r, d[nameof(x.Selected)]].Value = x.Selected;

                        // Price Fields
                        SetCurrencyCell(ws, r, d[nameof(x.Price_Per_Item)], x.Price_Per_Item);
                        SetCurrencyCell(ws, r, d[nameof(x.Total_Price)], x.Total_Price);
                        SetCurrencyCell(ws, r, d[nameof(x.Price_Per_Item_Inc_Other_Cost)], x.Price_Per_Item_Inc_Other_Cost);
                        SetCurrencyCell(ws, r, d[nameof(x.Total_Price_Inc_Other_Cost)], x.Total_Price_Inc_Other_Cost);
                        SetCurrencyCell(ws, r, d[nameof(x.Realised_Saving)], x.Realised_Saving);

                        // Order Section
                        ws.Cells[r, d[nameof(x.Order_Type)]].Value = x.Order_Type;
                        ws.Cells[r, d[nameof(x.Order_No)]].Value = x.Order_No;
                        ws.Cells[r, d[nameof(x.Order_Status)]].Value = x.Order_Status;

                        if ((x.Order_Status ?? "").Trim().ToLower() == "regenerate")
                        {
                            ws.Cells[r, 1, r, d.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[r, 1, r, d.Count].Style.Fill.BackgroundColor.SetColor(Color.Red);
                        }

                        SetDateCell(ws, r, d[nameof(x.Order_Date)], x.Order_Date, "yyyy-MM-dd");
                        SetCurrencyCell(ws, r, d[nameof(x.Order_Grand_Total_Amount)], x.Order_Grand_Total_Amount);
                        SetDateCell(ws, r, d[nameof(x.Approver_Date)], x.Approver_Date, "yyyy-MM-dd HH:mm:ss");
                        ws.Cells[r, d[nameof(x.Approver_Name)]].Value = x.Approver_Name;

                        // Delivery Note
                        ws.Cells[r, d[nameof(x.DN_No)]].Value = x.DN_No;
                        ws.Cells[r, d[nameof(x.DN_Status)]].Value = x.DN_Status;
                        SetDateCell(ws, r, d[nameof(x.DN_Date)], x.DN_Date, "yyyy-MM-dd");
                        SetNumericCell(ws, r, d[nameof(x.DN_Qty)], x.DN_Qty);

                        // Invoice Section
                        ws.Cells[r, d[nameof(x.Invoice_No)]].Value = x.Invoice_No;
                        ws.Cells[r, d[nameof(x.Invoice_Status)]].Value = x.Invoice_Status;
                        SetDateCell(ws, r, d[nameof(x.Invoice_Date)], x.Invoice_Date, "yyyy-MM-dd HH:mm:ss");
                        SetCurrencyCell(ws, r, d[nameof(x.Invoice_Amount)], x.Invoice_Amount);
                        SetCurrencyCell(ws, r, d[nameof(x.PPn)], x.PPn);
                        SetCurrencyCell(ws, r, d[nameof(x.PPh_23)], x.PPh_23);
                        SetCurrencyCell(ws, r, d[nameof(x.PPh_42)], x.PPh_42);
                        SetCurrencyCell(ws, r, d[nameof(x.Invoice_After_Tax_Or_Grand_Total)], x.Invoice_After_Tax_Or_Grand_Total);

                        ws.Cells[r, d[nameof(x.Remarks)]].Value = x.Remarks;
                        ws.Cells[r, d[nameof(x.ReasonCancel)]].Value = x.ReasonCancel;
                    }
                    #endregion

                    await ep.SaveAsync();
                }

                stream.Position = 0;
                log.LogInitialize(nameof(ExportProcurementWipToExcel), "Finish Export Procurement WIP", LogType.Info);
                return stream;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(nameof(ReportRepository), ex.InnerException);
            }
        }
        private void SetDateCell(ExcelWorksheet ws, int row, int col, DateTime? value, string format)
        {
            ws.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws.Cells[row, col].Style.Numberformat.Format = format;
            ws.Cells[row, col].Value = value;
        }

        private void SetCurrencyCell(ExcelWorksheet ws, int row, int col, decimal? value)
        {
            ws.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws.Cells[row, col].Style.Indent = 1;
            ws.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, col].Value = value;
        }

        private void SetNumericCell(ExcelWorksheet ws, int row, int col, decimal? value)
        {
            ws.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws.Cells[row, col].Style.Indent = 1;
            ws.Cells[row, col].Value = value;
        }
        #endregion
    }
}
