using APS_Common;
using APS_Common.Const;
using APS_Common.Sanitizer;
using APS_Entities.Models;
using APS_REST_API.Contracts;
using APS_REST_API.Contracts.v1;
using APS_REST_API.Models.Finance;
using APS_REST_API.Models.FinanceRequest;
using APS_REST_API.Models.FinanceVoucher;
using APS_REST_API.Models.FinanceVoucherRequest;
using APS_REST_API.Models.InvoiceManagementModel;
using APS_REST_API.Models.ReportBsmModel;
using APS_REST_API.Models.ReportCsvVoucherModel;
using APS_REST_API.Models.ReportMcmModel;
using APS_REST_API.Models.Request;
using APS_REST_API.Models.ResponseData;
using APS_REST_API.Payloads.Request.Finance;
using APS_REST_API.Payloads.Response.Attachment;
using APS_REST_API.Payloads.Response.Finance;
using APS_REST_API.Queries;
using APS_SharedServices.Models;
using APS_SharedServices.Models.RequestModels;
using APS_SharedServices.Models.ResponseModels;
using APS_SharedServices.Repositories.Contracts;
using APS_SharedServices.Services.Contracts;
using APS_TrexConsumer.Services.Contracts;
using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace APS_REST_API.Repository
{
    public class FinanceRepository : IFinanceRepository
    {
        private readonly IDapper _dapper;
        private readonly INotificationService _notificationService;
        private readonly IExternalService _externalService;
        private readonly IBudgetTransactionRepository _budgetTransactionRepository;
        private readonly IApprovalMatrixRepository _approvalMatrixRepository;
        private readonly string objectName = nameof(FinanceRepository);
        private readonly ISubmissionApprovalRepository _submissionApprovalRepository;
        private readonly IInvoiceManagementRepository _invoiceManagementRepository;
        private readonly IUpdatePaymentTrexConsumerService _kafkaService;
        private readonly Finance queryFinance = new Finance();
        private readonly List<string> ExpenseGER = ["COMBEN", "CONTEST", "OTHERS"];
        private readonly bool _trexToggle;

        public FinanceRepository(
            IDapper dapper
            , INotificationService notificationService
            , IExternalService externalService
            , ISubmissionApprovalRepository submissionApprovalRepository
            , IBudgetTransactionRepository budgetTransactionRepository
            , IApprovalMatrixRepository approvalMatrixRepository
            , IInvoiceManagementRepository invoiceManagementRepository
            , IUpdatePaymentTrexConsumerService kafkaService
            , IConfiguration configuration
            )
        {
            _dapper = dapper;
            _notificationService = notificationService;
            _externalService = externalService;
            _submissionApprovalRepository = submissionApprovalRepository;
            _budgetTransactionRepository = budgetTransactionRepository;
            _approvalMatrixRepository = approvalMatrixRepository;
            _invoiceManagementRepository = invoiceManagementRepository;
            _kafkaService = kafkaService;
            _trexToggle = configuration.GetValue<bool>("ExternalService:TrexToggle");
        }

        /// <summary>
        /// Logging Approval Matrix Repository
        /// </summary>
        /// <returns></returns>
        private readonly Logging log = new()
        {
            objectName = nameof(FinanceRepository)
        };

        //Approval Request

        /// <summary>
        /// Get Request List
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<FinanceListResponse>> GetRequestList(ParamGetRequestList param)
        {
            try
            {
                string qry = param.RequestType switch
                {
                    null => Finance.GetApprovalRequestListAll(param),
                    "RI" or "CA" or "CATR" => Finance.GetApprovalRequestListRI(param),
                    "TR" => Finance.GetApprovalRequestListTR(param),
                    "STL" => Finance.GetApprovalRequestListSTL(param),
                    "TRSTL" => Finance.GetApprovalRequestListTRSTL(param),
                    "INVTR" => Finance.GetApprovalRequestListINVTR(param),
                    "TREXAPR" => Finance.GetApprovalRequestListTREXAPR(param),
                    "TREXEER" => Finance.GetApprovalRequestListTREXEER(param),
                    "TREXTER" => Finance.GetApprovalRequestListTREXTER(param),
                    var s when s.Contains("TREXGER") => Finance.GetApprovalRequestListTREXGER(param),
                    "SC" => queryFinance.GetApprovalRequestListSC(param),
                    "VC" => queryFinance.GetApprovalRequestListVC(param),
                    "NON" => queryFinance.GetApprovalRequestListNonShop(param),
                    "NONVC" => queryFinance.GetApprovalRequestListNonVC(param),
                    "PO" => queryFinance.GetApprovalRequestListPO(param),
                    "POVC" => queryFinance.GetApprovalRequestListPOVC(param),
                    _ => Finance.GetApprovalRequestListAll(param),

                };
                if (ExpenseGER.Contains(param.RequestType))
                {
                    param.ExpenseType = param.RequestType;
                    qry = Finance.GetApprovalRequestListGER(param);
                }
                var FinanceList = await Task.FromResult(_dapper.GetAll<FinanceListResponse>(@$"{qry}", new Dapper.DynamicParameters(new
                {
                    RequestNumber = param.RequestNumber ?? string.Empty,
                    Search = param.Search?.Value ?? string.Empty,
                    Type = param.RequestType ?? string.Empty,
                    Status = param.Status ?? string.Empty,
                    RequestDateFrom = param.RequestDateFrom ?? string.Empty,
                    RequestDateTo = param.RequestDateTo ?? string.Empty,
                    RequestorName = param.RequestorName ?? string.Empty,
                    VendorId = param.VendorId ?? string.Empty,
                    Category = param.Category ?? string.Empty,
                    param.ExpenseType,
                    param.MakerFinance,
                    param.IsExport,
                    param.Page,
                    param.PageSize,
                    SortColumn = param.SortColumn ?? AppSystem.CreatedTime,
                    SortDirection = param.SortDirection ?? AppSystem.Desc
                }), commandType: CommandType.Text));
                return FinanceList;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Get Request List
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<FinanceListResponse>> GetSettlementList(ParamGetRequestList param)
        {
            try
            {
                string qry = Finance.GetApprovalFinanceSTL(param);
                if (param.IsExport)
                {
                    qry = $"{qry} ORDER BY CreatedTime DESC";
                }
                else
                {
                    qry = $"{qry} ORDER BY {param.SortColumn ?? AppSystem.CreatedTime} {param.SortDirection ?? AppSystem.Desc} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
                }
                var SettlementList = await Task.FromResult(_dapper.GetAll<FinanceListResponse>(@$"{qry}", new Dapper.DynamicParameters(new
                {
                    ApprovalFlowName = AppSystem.FlowFinanceSettlement,
                    RequestNumber = param.RequestNumber ?? string.Empty,
                    Search = param.Search?.Value ?? string.Empty,
                    Type = param.RequestType ?? string.Empty,
                    Status = param.Status ?? string.Empty,
                    ApprovalName = param.ApprovalName ?? string.Empty,
                    RequestDateFrom = param.RequestDateFrom ?? string.Empty,
                    RequestDateTo = param.RequestDateTo ?? string.Empty,
                    RequestorName = param.RequestorName ?? string.Empty,
                    VendorId = param.VendorId ?? string.Empty,
                    param.IsExport,
                    param.Page,
                    param.PageSize,
                    SortColumn = param.SortColumn ?? AppSystem.CreatedTime,
                    SortDirection = param.SortDirection ?? AppSystem.Desc
                }), commandType: CommandType.Text));
                return SettlementList;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Get Reimbursement Detail List
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<FinanceReimbursementDetailResponse>> GetReimbursementDetail(ParamGetRequestList param)
        {
            try
            {
                var subquery = "r.RequestNumber = @RequestNumber";
                if (!String.IsNullOrEmpty(param.Search?.Value))
                {
                    subquery = String.Concat(subquery, @$" AND (rcc.ReimbursementDetailId LIKE @Search  
                                                                OR rcc.L_Currency_Code LIKE @Search  
                                                                OR rcc.BasicAmount LIKE @Search  
                                                                OR bu.Name LIKE @Search  
                                                                OR cc.Name LIKE @Search  
                                                                OR rd.AccountMasterId LIKE @Search  
                                                                )");
                }
                string query = @$"SELECT sub.*, COUNT(*) OVER () as CountData FROM (	
                                  SELECT  rcc.[Id]
                                         ,rcc.ReimbursementDetailId
                                         ,rd.Description as [Quantity]
	                                 	 ,rcc.L_Currency_Code
	                                 	 ,rcc.Amount
	                                 	 ,bu.Name as [BusinessUnitName]
	                                 	 ,cc.Name as [CostCenterName]
	                                 	 ,coa.AccountCode as [Account]
	                                 	 ,rd.AttachmentId as [AttachmentId]
	                                 	 ,'N/A' as [FinanceExportStrategy]
	                                 	 ,'N/A' as [Product]
	                                 	 ,'N/A' as [Project]
	                                 	 ,CASE 
											WHEN ISNULL((SELECT SUM(GrossUp) from ReimbursementDetailOtherCost WHERE ReimbursementDetailId =  rd.Id), 0) = 0 THEN 'N'
										    ELSE 'Y'
										  END as [Affiliate]
                                         ,(SELECT (Amount * (rcc.Percentage / 100)) + ISNULL(GrossUp, 0) from ReimbursementDetailOtherCost WHERE ReimbursementDetailId =  rd.Id AND OtherCost_SubCategoryId = 75) as [Pph23]
                                         ,(SELECT (Amount * (rcc.Percentage / 100)) + ISNULL(GrossUp, 0) from ReimbursementDetailOtherCost WHERE ReimbursementDetailId =  rd.Id AND OtherCost_SubCategoryId = 77) as [Ppn]
                                         ,(SELECT (Amount * (rcc.Percentage / 100)) + ISNULL(GrossUp, 0) from ReimbursementDetailOtherCost WHERE ReimbursementDetailId =  rd.Id AND OtherCost_SubCategoryId = 78) as [PphFinal]
	                                 	 ,(SELECT  Amount * (rcc.Percentage / 100) from ReimbursementDetailOtherCost WHERE ReimbursementDetailId =  rd.Id AND OtherCost_SubCategoryId = 76) as [StampDuty]
                                         ,CASE WHEN LAG(rd.Id) over(order by rcc.[Id]) = rd.Id 
                                               THEN '' 
                                               ELSE rd.Description 
                                               END  as [EditAble]
                                  FROM [ReimbursementDetailCostCenter] rcc 
	                              JOIN [ReimbursementDetail] rd on rd.Id = rcc.ReimbursementDetailId
	                              JOIN [CostCenter] cc on rcc.CostCenterId = cc.Id
	                              JOIN [BusinessUnit] bu on cc.BusinessUnitId = bu.Id
                                  JOIN [Reimbursement] r on r.Id = rd.ReimbursementId
                                  JOIN [AccountMaster] coa on coa.Id = rd.AccountMasterId
                                  WHERE {subquery} ) sub
                                ";

                var data = await Task.FromResult(_dapper.GetAll<FinanceReimbursementDetailResponse>(@$"{query} 
                    ORDER BY {param.SortColumn} {param.SortDirection} OFFSET @PageNumber ROWS FETCH NEXT @Rows ROWS ONLY",
                     new Dapper.DynamicParameters(new
                     {
                         RequestNumber = param.RequestNumber,
                         PageNumber = param.Page,
                         Rows = param.PageSize,
                         Status = param.Status,
                         Search = "%" + param.Search?.Value + "%"
                     })));
                return data;

            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Settlement Detail Other Cost
        /// </summary>
        /// <param name="settlementDetailId"></param>
        /// <returns></returns>
        public async Task<List<FinanceSettlementOtherCostResponse>> GetSettlementDetailOtherCost(string settlementDetailId)
        {
            try
            {
                string query = @$"SELECT sdo.* from Settlement s
								  JOIN   SettlementDetail sd on sd.SettlementId = s.Id
								  JOIN   SettlementDetailOtherCost sdo on sdo.SettlementDetailId = sd.Id
								  WHERE  sdo.SettlementDetailId = @SettlementDetailId
                                ";
                var data = await Task.FromResult(_dapper.GetAll<FinanceSettlementOtherCostResponse>(query,
                     new Dapper.DynamicParameters(new
                     {
                         SettlementDetailId = settlementDetailId
                     })));
                return data;

            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Settlement Detail Other Cost by 
        /// </summary>
        /// <param name="settlementId"></param>
        /// <returns></returns>
        public async Task<List<FinanceSettlementOtherCostResponse>> GetSettlementDetailOtherCostBySettlementId(string settlementId)
        {
            try
            {
                string query = @$"SELECT * 
                                  FROM SettlementDetailOtherCost
                                  WHERE SettlementDetailId in (SELECT id FROM SettlementDetail WHERE SettlementId = @SettlementId)
                                ";
                var data = await Task.FromResult(_dapper.GetAll<FinanceSettlementOtherCostResponse>(query,
                    new Dapper.DynamicParameters(new
                    {
                        SettlementId = settlementId
                    })));
                return data;

            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }


        /// <summary>
        /// Detail Approval Request
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<ApprovalRequest> ApprovalRequestDetail(ParamGetRequestDetail param)
        {
            try
            {
                string subquery = $@"ar.Status = @Status AND r.Status = @Status";
                if (String.IsNullOrEmpty(param.Status))
                    subquery = subquery.Replace("ar.Status = @Status AND r.Status = @Status", "1 = 1");
                if (param.RequestType.ToLower().Contains("trex"))
                    subquery = subquery.Replace("r.Status = @Status", "1 = 1");
                string query;
                switch (param.RequestType.ToLower())
                {
                    case "reimbursement":
                        query = $@" SELECT ar.* FROM ApprovalRequest ar JOIN Reimbursement r ON ar.Id = r.ApprovalRequestId
                                    WHERE ar.RequestNo = @RequestNumber
                                    AND {subquery}";
                        break;
                    case "travel":
                        query = $@" SELECT ar.* FROM ApprovalRequest ar JOIN TravelRequest r ON ar.Id = r.ApprovalRequestId
                                    WHERE ar.RequestNo = @RequestNumber
                                    AND {subquery}";
                        break;
                    case "travel settlement":
                        query = $@" SELECT ar.* FROM ApprovalRequest ar JOIN TravelRequestExpense r ON ar.Id = r.ApprovalRequestId
                                    WHERE ar.RequestNo = @RequestNumber
                                    AND {subquery}";
                        break;
                    case "cash advance":
                    case "cash advance travel":
                        query = $@" SELECT ar.* FROM ApprovalRequest ar JOIN Reimbursement r ON ar.Id = r.ApprovalRequestId
                                    WHERE ar.RequestNo = @RequestNumber
                                    AND {subquery}";
                        break;
                    case "invoice travel":
                        query = $@" SELECT ar.* FROM ApprovalRequest ar JOIN Invoicetravel r ON ar.Id = r.ApprovalRequestId
                                    WHERE ar.RequestNo = @RequestNumber
                                    AND {subquery}";
                        break;
                    case "ger":
                        query = $@" SELECT ar.* FROM ApprovalRequest ar JOIN GerHeader r ON ar.requestNo = r.requestNumber
                                    WHERE ar.RequestNo = @RequestNumber
                                    AND {subquery}";
                        break;
                    case "trexapr":
                        query = $@" SELECT ar.* FROM TrexApr ar
                                    WHERE ar.NoAPR = @RequestNumber
                                    AND {subquery}";
                        break;
                    case "trexeer":
                        query = $@" SELECT ar.* FROM TrexEerHeader ar
                                    WHERE ar.NoEER = @RequestNumber
                                    AND {subquery}";
                        break;
                    case "trexger":
                        query = $@" SELECT ar.* FROM TrexGerHeader ar
                                    WHERE ar.NoGER = @RequestNumber
                                    AND {subquery}";
                        break;
                    case "trexter":
                        query = $@" SELECT ar.* FROM TrexTerHeader ar
                                    WHERE ar.NoTER = @RequestNumber
                                    AND {subquery}";
                        break;
                    default:
                        query = String.Empty;
                        break;
                }
                var data = await Task.FromResult(_dapper.Get<ApprovalRequest>(query, new Dapper.DynamicParameters(new
                {
                    RequestNumber = param.RequestNumber,
                    Status = param.Status
                }), commandType: CommandType.Text));

                return data;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        //Approval Request Group Member

        /// <summary>
        /// Get Approval Finance Group Member
        /// </summary>
        /// <returns></returns>
        public async Task<List<Models.ApprovalGroupModel>> GetApprovalFinanceGroupMember()
        {
            try
            {
                string queryGetGroupMember = $@"select * from ApprovalGroup
                                                where ApprovalGroup_SubCategoryId = (SELECT ID from Subcategory WHERE SubCategoryName = @SubCategoryName)";


                var data = await Task.FromResult(_dapper.GetAll<Models.ApprovalGroupModel>(queryGetGroupMember, new Dapper.DynamicParameters(new
                {
                    SubCategoryName = AppSystem.FinanceApprovalSubCategoryName
                }), commandType: CommandType.Text));
                return data;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Get Approval Finance Settlement Group Member
        /// </summary>
        /// <returns></returns>
        public async Task<List<Models.ApprovalGroupModel>> GetApprovalFinanceSettlementGroupMember(string level)
        {
            try
            {
                string queryGetGroupMember = $@"select * from ApprovalGroup
                                                where Level = @Level AND ApprovalGroup_SubCategoryId = (SELECT ID from Subcategory WHERE SubCategoryName = @SubCategoryName)";
                return await Task.FromResult(_dapper.GetAll<Models.ApprovalGroupModel>(queryGetGroupMember, new Dapper.DynamicParameters(new
                {
                    SubCategoryName = AppSystem.FinanceSettlementApprovalSubCategoryName,
                    Level = level
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        public async Task<List<AttachmentModel>> GetAttachment(string requestNumber, string typeRequest)
        {
            try
            {
                string query = string.Empty;
                if (typeRequest.Equals("reimbursement", StringComparison.CurrentCultureIgnoreCase) || typeRequest.Contains("cash advance", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT a.*
                                FROM ApprovalRequest ar
                                JOIN Reimbursement r on ar.Id = r.ApprovalRequestId
                                JOIN ReimbursementDetail rd on rd.ReimbursementId = r.Id
                                JOIN Attachment a on a.id = rd.AttachmentId
                                WHERE ar.RequestNo = @RequestNumber";
                }
                else if (typeRequest.Equals("shopping cart", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = queryFinance.GetAttachmentShop();
                }
                else if (typeRequest.Equals("shopping cart vc", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@"
                        DECLARE @InvoicePOId int,
                                @PurchaeseOrderId INT,
                                @PurchaseRequestlId INT;
                        SELECT @InvoicePOId = IPO.Id,
                               @PurchaeseOrderId = IPO.PurchaeseOrderId,
                               @PurchaseRequestlId = potpr.PurchaseRequestlId
                        FROM InvoicePO IPO
                            JOIN PurchaseOrderToPurchaseRequest POTPR
                                ON POTPR.PurchaseOrderId = IPO.PurchaeseOrderId
                        WHERE CAST(IPO.Id as varchar(100)) = SUBSTRING(@RequestNumber, 0, CHARINDEX(' - ', @RequestNumber))
                        DECLARE @SubCategory INT = (
                                                       SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01261'
                                                   )
                        SELECT Id,
                               FullPath,
                               Category
                        FROM
                        (
                            SELECT a.Id,
                                   a.FullPath,
                                   a.Category
                            FROM Attachment as a
                                JOIN PurchaseRequestItemDetail as prid
                                    ON prid.AttachmentId = a.Id
                                       AND prid.PurchaseRequestId = @PurchaseRequestlId
                            UNION ALL
                            SELECT a.Id,
                                   a.FullPath,
                                   a.Category
                            FROM Attachment as a
                                JOIN PurchaseOrder as po
                                    ON po.AttachmentId = a.Id
                                       AND po.Id = @PurchaeseOrderId
                            where a.Category = 'PO'
                            UNION ALL
                            SELECT a.Id,
                                   a.FullPath,
                                   a.Category
                            FROM Attachment as a
                                JOIN DeliveryNotesDetail as dnd
                                    ON dnd.Id = a.RefId
                                JOIN PurchaseOrderDetail as pod
                                    ON pod.Id = dnd.PurchaseOrderDetailId
                                       AND pod.PurchaseOrderId = @PurchaeseOrderId
                            where a.Category = 'DN'
                            UNION ALL
                            SELECT a.Id,
                                   a.FullPath,
                                   a.Category
                            FROM Attachment as a
                                JOIN InvoicePO as ipo
                                    ON ipo.PurchaeseOrderId = a.RefId
                            WHERE a.Category = 'INV'
                                  AND a.Description = 'ShoppingCart'
                                  AND IPO.Id = @InvoicePOId
                                  AND CAST(a.CreatedTime AS DATE)
                                  between CAST(ipo.CreateTime AS DATE) and CAST(ipo.LastUpdateTime AS DATE)
                            UNION ALL
                            SELECT ATH.Id,
                                   ATH.FullPath,
                                   ATH.Category
                            FROM InvoicePO INV
                                JOIN Attachment ATH
                                    ON ATH.RefId = INV.Id
                            WHERE INV.PurchaeseOrderId = @PurchaeseOrderId
                                  AND ATH.Category = 'INV'
                                  AND INV.CategoryProcess_SubCategoryId = @SubCategory
                        ) datax
                    ";
                }
                else if (typeRequest.Equals("travel", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT prid.AttachmentId as [Id]
                                FROM ApprovalRequest ar
                                JOIN PurchaseRequestItemDetail prid on ar.Id = prid.ApprovalRequestId
                                WHERE ar.RequestNo = @RequestNumber";
                }
                else if (typeRequest.Equals("settlement", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT a.*
							    FROM   Settlement s
							    JOIN   SettlementDetail sd on sd.SettlementId = s.Id
                                JOIN   Attachment a on a.Id = sd.AttachmentId
							    WHERE  s.SettlementNumber = @RequestNumber";
                }
                else if (typeRequest.Equals("travel settlement", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT a.*
							    FROM   TravelRequestExpense tre
							    JOIN   TravelRequestExpenseDetail tred on tred.TravelRequestExpenseId = tre.Id
                                JOIN   Attachment a on a.Id = tred.AttachmentId
							    WHERE  tre.RequestNumber = @RequestNumber

                                UNION ALL								
                                SELECT  a.*
							    FROM   TravelRequestExpense tre
							    JOIN   TravelRequest tr on tr.Id =  tre.TravelRequestId
								JOIN   TravelRequestTransportation trt on trt.TravelRequestId = tr.Id
                                JOIN   Attachment a on a.Id = tr.AttachmentId
							    WHERE  tre.RequestNumber = @RequestNumber

                                UNION ALL								
                                SELECT  a.*
							    FROM   TravelRequestExpense tre
							    JOIN   TravelRequest tr on tr.Id =  tre.TravelRequestId
								JOIN   TravelRequestTransportation trt on trt.TravelRequestId = tr.Id
                                JOIN   Attachment a on a.Id = trt.AttachmentId
							    WHERE  tre.RequestNumber = @RequestNumber
                                
                                UNION ALL								
                                SELECT  a.*
							    FROM   TravelRequestExpense tre
							    JOIN   TravelRequest tr on tr.Id =  tre.TravelRequestId
								JOIN   TravelRequestAccomodation tra on tra.TravelRequestId = tr.Id
                                JOIN   Attachment a on a.Id = tra.AttachmentId
							    WHERE  tre.RequestNumber = @RequestNumber ";
                }
                else if (typeRequest.Equals("invoice travel", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT a.* 
                                FROM   InvoiceTravel inv
                                JOIN   Attachment a on a.Id = inv.AttachmentId
							    WHERE  inv.RequestNumber = @RequestNumber ";
                }
                else if (typeRequest.Equals("ger", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT a.* 
                                FROM   GerHeader g
                                JOIN   GerDetail gd on gd.GerHeaderId = g.Id
                                JOIN   Attachment a on a.Id = g.AttachmentId
							    WHERE  CONCAT(g.RequestNumber, ' - ', gd.Id) = @RequestNumber 

                                UNION 

                                SELECT a.* 
                                FROM   GerHeader g
                                JOIN   GerDetail gd on gd.GerHeaderId = g.Id
                                JOIN   Attachment a on a.Id = g.AttachmentId
							    WHERE  g.RequestNumber = @RequestNumber";
                }
                else if (typeRequest.Contains("trex", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = Finance.GetTrexAttachment();
                }
                else if (typeRequest.Equals("non shopping cart", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = queryFinance.GetAttachmentNonShop();
                }
                else if (typeRequest.Equals("non shopping cart vc", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = queryFinance.GetAttachmentNonShopVoucher();
                }
                return await Task.FromResult(_dapper.GetAll<AttachmentModel>(query, new Dapper.DynamicParameters(new
                {
                    RequestNumber = requestNumber
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        public async Task<List<AttachmentModel>> GetAttachmentVoucher(string voucherId)
        {
            try
            {
                var voucherHeader = await GetVoucherHeader(voucherId, string.Empty);
                var type = voucherHeader.Category;
                string query = string.Empty;

                if (type.Equals("reimbursement", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT a.* 
                                FROM ApprovalRequest ar
                                JOIN Reimbursement r on ar.Id = r.ApprovalRequestId
                                JOIN ReimbursementDetail rd on rd.ReimbursementId = r.Id
                                JOIN Attachment a on a.Id = rd.AttachmentId
                                WHERE ar.RequestNo in (
                                                         SELECT VoucherRefId
                                                         FROM   VoucherDetail
                                                         WHERE  VoucherId = @VoucherId
                                                      )";
                }
                else if (type.Equals("shopping cart", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@"
                        DECLARE @SubCategory INT = (
                            SELECT Id 
                            FROM SubCategory 
                            WHERE SubCategoryCode = 'SC-2024-02-01261'
                        );

                        -- Temporary tables
                        CREATE TABLE #PurchaseOrderId (PurchaseOrderId BIGINT, CreateTime Date, LastUpdateTime Date);
                        CREATE TABLE #PurchaseRequestlId (PurchaseRequestlId BIGINT);

                        -- Insert PurchaseOrderId
                        INSERT INTO #PurchaseOrderId (PurchaseOrderId, CreateTime, LastUpdateTime)
                        SELECT INV.PurchaeseOrderId, INV.CreateTime, INV.LastUpdateTime
                        FROM InvoicePO INV
                        WHERE INV.Id IN (
                            SELECT RTRIM(SUBSTRING(vd.VoucherRefId, 0, CHARINDEX('-', vd.VoucherRefId, 1)))
                            FROM VoucherHeader VH
                            JOIN VoucherDetail VD ON VH.Id = VD.VoucherId
                            WHERE VH.Id = @VoucherId
                        )
                        AND INV.CategoryProcess_SubCategoryId = @SubCategory;

                        -- Insert PurchaseRequestlId
                        INSERT INTO #PurchaseRequestlId (PurchaseRequestlId)
                        SELECT PRPO.PurchaseRequestlId
                        FROM PurchaseOrderToPurchaseRequest PRPO
                        WHERE PRPO.PurchaseOrderId IN (
                                SELECT PurchaseOrderId FROM #PurchaseOrderId
                        );

                        -- Select attachments
                        SELECT Id, FullPath
                        FROM (
                            -- PR attachments
                            SELECT ATT.Id, ATT.FullPath
                            FROM PurchaseRequestItemDetail PRD
                            JOIN Attachment ATT ON PRD.AttachmentId = ATT.Id
                            WHERE PRD.PurchaseRequestId IN (
                                SELECT PurchaseRequestlId FROM #PurchaseRequestlId
                            )
                            AND ATT.Category = 'PR'

                            UNION ALL

                            -- PurchaseRequest attachments
                            SELECT ATT.Id, ATT.FullPath
                            FROM Attachment ATT
                            WHERE ATT.RefId IN (
                                SELECT PurchaseRequestlId FROM #PurchaseRequestlId
                            )
                            AND ATT.Category = 'PurchaseRequest'

                            UNION ALL

                            -- PO attachments
                            SELECT ATT.Id, ATT.FullPath
                            FROM PurchaseOrder PO
                            JOIN Attachment ATT ON ATT.Id = PO.AttachmentId
                            WHERE PO.Id IN (
                                SELECT PurchaseOrderId FROM #PurchaseOrderId
                            )
                            AND ATT.Category = 'PO'

                            UNION ALL

                            -- DN attachments
                            SELECT ATT.Id, ATT.FullPath
                            FROM DeliveryNotesDetail DND
                            JOIN Attachment ATT ON DND.Id = ATT.RefId
                            WHERE DND.PurchaseOrderDetailId IN (
                                SELECT PRD.Id
                                FROM PurchaseOrderDetail PRD
                                WHERE PRD.PurchaseOrderId IN (
                                SELECT PurchaseOrderId FROM #PurchaseOrderId
                                )
                            )
                            AND ATT.Category = 'DN'

                            UNION ALL

                            -- INV attachments
                            SELECT ATT.Id, ATT.FullPath
                            FROM Attachment ATT
	                        JOIN #PurchaseOrderId POI ON ATT.RefId = POI.PurchaseOrderId
                            WHERE ATT.Category = 'INV'
	                        AND Description = 'ShoppingCart'
	                        AND CAST(ATT.CreatedTime AS DATE) between CAST(POI.CreateTime AS DATE) and CAST(POI.LastUpdateTime AS DATE) 

                            UNION ALL
                            SELECT ATH.Id,
                                   ATH.FullPath
                            FROM InvoicePO INV
                                JOIN Attachment ATH
                                    ON ATH.RefId = INV.Id
                            WHERE INV.PurchaeseOrderId IN (
                                                              SELECT PurchaseOrderId FROM #PurchaseOrderId
                                                          )
                                  AND ATH.Category = 'INV'
                                  AND INV.CategoryProcess_SubCategoryId = @SubCategory
                        ) AS ATH;

                        -- Cleanup
                        DROP TABLE #PurchaseOrderId;
                        DROP TABLE #PurchaseRequestlId;
                    ";
                }
                else if (type.Equals("travel", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT a.*
                                FROM ApprovalRequest ar
                                JOIN Reimbursement r on ar.Id = r.ApprovalRequestId
                                JOIN ReimbursementDetail rd on rd.ReimbursementId = r.Id
                                JOIN Attachment a on a.Id = rd.AttachmentId
                                WHERE ar.RequestNo in (
                                                         SELECT VoucherRefId
                                                         FROM   VoucherDetail
                                                         WHERE  VoucherId = @VoucherId
                                                      )";
                }
                else if (type.Equals("travel settlement", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT a.*
							    FROM   TravelRequestExpense tre
							    JOIN   TravelRequestExpenseDetail tred on tred.TravelRequestExpenseId = tre.Id
								JOIN   Attachment a on a.Id = tred.AttachmentId
							    WHERE  tre.RequestNumber in (
                                                         SELECT VoucherRefId
                                                         FROM   VoucherDetail
                                                         WHERE  VoucherId = @VoucherId
                                                      )

                                UNION ALL								
                                SELECT a.*
							    FROM   TravelRequestExpense tre
							    JOIN   TravelRequest tr on tr.Id =  tre.TravelRequestId
								JOIN   Attachment a on a.Id = tr.AttachmentId
							    WHERE  tre.RequestNumber in (
                                                         SELECT VoucherRefId
                                                         FROM   VoucherDetail
                                                         WHERE  VoucherId = @VoucherId
                                                      )

                                UNION ALL								
                                SELECT a.*
							    FROM   TravelRequestExpense tre
							    JOIN   TravelRequest tr on tr.Id =  tre.TravelRequestId
								JOIN   TravelRequestTransportation trt on trt.TravelRequestId = tr.Id
								JOIN   Attachment a on a.Id = trt.AttachmentId
							    WHERE  tre.RequestNumber in (
                                                         SELECT VoucherRefId
                                                         FROM   VoucherDetail
                                                         WHERE  VoucherId = @VoucherId
                                                      )
                                
                                UNION ALL								
                                SELECT a.*
							    FROM   TravelRequestExpense tre
							    JOIN   TravelRequest tr on tr.Id =  tre.TravelRequestId
								JOIN   TravelRequestAccomodation tra on tra.TravelRequestId = tr.Id
								JOIN   Attachment a on a.Id = tra.AttachmentId
							    WHERE  tre.RequestNumber in (
                                                         SELECT VoucherRefId
                                                         FROM   VoucherDetail
                                                         WHERE  VoucherId = @VoucherId
                                                      )";
                }
                else if (type.StartsWith("cash advance", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT a.*
                                FROM ApprovalRequest ar
                                JOIN Reimbursement r on ar.Id = r.ApprovalRequestId
                                JOIN ReimbursementDetail rd on rd.ReimbursementId = r.Id
                                JOIN Attachment a on a.Id = rd.AttachmentId
                                WHERE ar.RequestNo in (
                                                         SELECT VoucherRefId
                                                         FROM   VoucherDetail
                                                         WHERE  VoucherId = @VoucherId
                                                      )";
                }
                else if (type.StartsWith("invoice travel", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT a.*
                                FROM InvoiceTravel inv
                                JOIN Attachment a on a.Id = inv.AttachmentId
                                WHERE inv.RequestNumber in (
                                                         SELECT VoucherRefId
                                                         FROM   VoucherDetail
                                                         WHERE  VoucherId = @VoucherId
                                                      )";
                }
                else if (type.StartsWith("ger", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT a.*
                                FROM GerHeader ger
                                JOIN Attachment a on a.Id = ger.AttachmentId
                                WHERE ger.RequestNumber in (
                                                         SELECT  CASE WHEN CHARINDEX(' - ', VoucherRefId) > 0 
															     THEN LEFT(VoucherRefId, CHARINDEX(' - ', VoucherRefId) - 1) 
															     ELSE VoucherRefId 
															     END AS VoucherRefId
                                                         FROM   VoucherDetail
                                                         WHERE  VoucherId = @VoucherId
                                                      )";
                }
                else if (type.StartsWith("trex", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@" SELECT 
						          Category,
						          0 AS Id,
						          0 AS RefId,
						          '-' AS RelatedTableName,
						          '' AS FullPath,
						          '' AS Description,
						          '' AS OriginalFileName,
						          '' AS Checksum,
						          '' AS CreatedBy,
						          GETDATE() AS CreatedTime,
						          '' AS LastUpdatedBy,
						          GETDATE() AS LastUpdatedTime
						      FROM (
						          SELECT NoAPR AS Category, NoAPR AS RequestNumber FROM TrexAPR
						          UNION
						          SELECT NoEER AS Category, NoEER FROM TrexEERHeader
						          UNION
						          SELECT NoGER AS Category, NoGER FROM TrexGERHeader
						          UNION
						          SELECT NoTER AS Category, NoTER FROM TrexTERHeader
						      ) AS Combined
						      WHERE RequestNumber in (
											 SELECT VoucherRefId
											 FROM   VoucherDetail
											 WHERE  VoucherId = @VoucherId
                                                      )";
                }
                else if (type.StartsWith("non shopping cart", StringComparison.CurrentCultureIgnoreCase))
                {
                    query = $@"
                        DECLARE @SubCategory INT = (
                            SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01262'
                        )

                        CREATE TABLE #HEADER (PRFId INT, PurchaseOrderId INT, DeliveryNotesId INT, CreateTime Date, LastUpdateTime Date)
                        INSERT INTO #HEADER
                        (
                            PRFId,
	                        PurchaseOrderId,
	                        DeliveryNotesId,
	                        CreateTime,
	                        LastUpdateTime
                        )
                        SELECT 
	                        PFS.PRFId,
	                        INV.PurchaeseOrderId,
	                        DND.Id,
	                        INV.CreateTime,
	                        INV.LastUpdateTime
                        FROM InvoicePO INV
                            JOIN PONonShopping PO
                                ON INV.PurchaeseOrderId = PO.Id
                            JOIN PRFSummary PFS
                                ON PO.PRFSummaryId = PFS.Id
	                        JOIN PONonShoppingTOP POT
		                        ON PO.Id = POT.PONonShoppingId
	                        JOIN DeliveryNotesPayment DNP
		                        ON POT.Id = DNP.PurchaseOrderTOPId
	                        JOIN DeliveryNotesDetail DND
		                        ON DNP.Id = DND.DeliveryNotesPaymentId
                        WHERE INV.Id IN (
                                            SELECT RTRIM(SUBSTRING(VD.VoucherRefId, 0, CHARINDEX('-', VD.VoucherRefId, 1)))
                                            FROM VoucherHeader VH
                                                JOIN VoucherDetail VD
                                                    ON VH.Id = VD.VoucherId
                                            WHERE VH.Id = @VoucherId
                                        )
                                AND INV.CategoryProcess_SubCategoryId = @SubCategory
                                AND DNP.CategoryProcess_SubCategoryId = @SubCategory


                        SELECT ATCH.Id,
                                ATCH.FullPath
                        FROM
                        (
                            SELECT ATH.Id,
                                    ATH.FullPath
                            FROM Attachment ATH
                            WHERE ATH.RefId IN (
                                                    SELECT PRFId FROM #HEADER
                                                )
                                    AND ATH.Category IN('PurchaseRequestForm', 'QuotationFormVendor', 'ProcurementSummary')
                        ) ATCH
                        UNION ALL
                        SELECT ATCH.Id,
                                ATCH.FullPath
                        FROM
                        (
                            SELECT ATH.Id,
                                    ATH.FullPath
                            FROM Attachment ATH
                            WHERE ATH.RefId IN (
                                                    SELECT PurchaseOrderId FROM #HEADER
                                                )
                                    AND ATH.Category IN('PO') AND ATH.Description = 'Non-ShoppingCart'
                        ) ATCH

                        UNION ALL
                        SELECT ATCH.Id,
                                ATCH.FullPath
                        FROM
                        (
                            SELECT ATH.Id,
                                    ATH.FullPath
                            FROM Attachment ATH
                            WHERE ATH.RefId IN (
                                                    SELECT DeliveryNotesId FROM #HEADER
                                                )
                                    AND ATH.Category IN('DN')
                        ) ATCH
                        UNION ALL
                        SELECT ATCH.Id,
                                ATCH.FullPath
                        FROM
                        (
                            SELECT ATH.Id,
                                    ATH.FullPath
                            FROM Attachment ATH
	                        JOIN #HEADER H ON ATH.RefId = H.PurchaseOrderId
                            WHERE ATH.Category IN('INV')
			                        AND ATH.Description LIKE 'Non%Shopping%Cart'
			                        AND CAST(ATH.CreatedTime AS DATE) between CAST(H.CreateTime AS DATE) and CAST(H.LastUpdateTime AS DATE) 
                        ) ATCH
                        DROP TABLE #HEADER
                    ";
                }
                return await Task.FromResult(_dapper.GetAll<AttachmentModel>(query, new Dapper.DynamicParameters(new
                {
                    VoucherId = voucherId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        public async Task<HttpResponseMessage> GetAttachmentTrex(string requestNumber, string typeRequest)
        {
            try
            {
                var trexTransaction = await GetAttachment(requestNumber, typeRequest);
                if (trexTransaction != null)
                {
                    var type = trexTransaction.Select(q => q.Category).FirstOrDefault();
                    var id = trexTransaction.Select(q => q.Description).FirstOrDefault();
                    var token = await _externalService.GetTokenTrexAttachment();
                    var response = await _externalService.GetTrexAttachment(token, type, id);
                    return response;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Update Reimbursement Detail Other Cost
        /// </summary>
        /// <param name="reimbursementDetailId"></param>
        /// CHANGELOG:
        /// -Added new field for update DPP (Billy 1-4-2024)
        /// -Added new logic: sum if isCalculate true (Billy 31-1-2025)
        /// <returns></returns>
        public async Task<int> UpdateOtherCostReimbursementDetail(int reimbursementDetailId, FinanceOtherCost param)
        {
            try
            {
                if (param == null)
                    return 1;

                foreach (var reimbursement in param.OtherCost)
                {
                    //update if ID exist, insert if ID not exist
                    if (reimbursement.Id != 0)
                    {
                        var queryUpdateOtherCost = $@"UPDATE [dbo].[ReimbursementDetailOtherCost]
                                   SET 
                                   [L_Currency_Code] = @Currency
                                  ,[Amount] = @Amount
                                  ,[BasicAmount] = @Amount * @RateAmount
                                  ,[RateAmount] = @RateAmount
                                  ,[Qty] = @Qty
                                  ,[GrossUp] =    CASE WHEN @GrossUp > 0 
                                                       THEN @GrossUp 
                                                       ELSE NULL
                                                  END
                                  ,[DppGrossUp] = CASE WHEN @GrossUp > 0 
                                                       THEN @GrossUp + @BasicAmount 
                                                       ELSE NULL
                                                  END
                                  ,[OtherCost_SubCategoryId] = @OtherCostSubCategoryId
                                  ,[LastUpdatedBy] = @LastUpdatedBy
                                  ,[LastUpdatedTime] = @LastUpdatedTime
                                  ,[IsCalculate] = @IsCalculate
                                  ,[IsIncludeGrandTotal] = @IsIncludeGrandTotal
                                  ,[DppFinance] = @DppFinance
                                   WHERE Id = @Id";
                        await Task.FromResult(_dapper.Update<FinanceOtherCost>(queryUpdateOtherCost, new Dapper.DynamicParameters(new
                        {
                            Currency = reimbursement.L_Currency_Code,
                            BasicAmount = reimbursement.BasicAmount,
                            RateAmount = reimbursement.RateAmount,
                            Amount = reimbursement.Amount,
                            Qty = reimbursement.Qty,
                            GrossUp = reimbursement.GrossUp,
                            OtherCostSubCategoryId = reimbursement.OtherCost_SubCategoryId,
                            LastUpdatedBy = reimbursement.LastUpdatedBy,
                            LastUpdatedTime = DateTime.Now,
                            Id = reimbursement.Id,
                            IsCalculate = reimbursement.IsCalculate,
                            IsIncludeGrandTotal = reimbursement.IsIncludeGrandTotal,
                            DppFinance = reimbursement.DppFinance,
                        }), commandType: CommandType.Text));
                    }
                    else
                    {
                        var queryInsertOtherCost = $@"INSERT INTO [dbo].[ReimbursementDetailOtherCost]
                                ( [ReimbursementDetailId]
                                ,[L_Currency_Code]
                                ,[BasicAmount]
                                ,[RateAmount]
                                ,[Amount]
                                ,[Qty]
                                ,[OtherCost_SubCategoryId]
                                ,[CreatedTime]
                                ,[CreatedBy]
                                ,[LastUpdatedBy]
                                ,[LastUpdatedTime]
                                ,[Notes]
                                ,[GrossUp]
                                ,[DppGrossUp]
                                ,[IsCalculate]
                                ,[IsIncludeGrandTotal]
                                ,[DppFinance] )
                                VALUES ( @ReimbursementDetailId
                                        ,@Currency
                                        ,@Amount * @RateAmount
                                        ,@RateAmount
                                        ,@Amount
                                        ,@Qty
                                        ,@OtherCostSubCategoryId
                                        ,@LastUpdatedTime
                                        ,@LastUpdatedBy
                                        ,@LastUpdatedBy
                                        ,@LastUpdatedTime
                                        ,''
                                        ,CASE WHEN @GrossUp > 0 
                                                       THEN @GrossUp 
                                                       ELSE NULL
                                                  END
                                        ,CASE WHEN @GrossUp > 0 
                                                       THEN @GrossUp + @BasicAmount 
                                                       ELSE NULL
                                                  END
                                        ,@IsCalculate
                                        ,@IsIncludeGrandTotal
                                        ,@DppFinance )
                                   ";
                        await Task.FromResult(_dapper.Insert<FinanceOtherCost>(queryInsertOtherCost, new Dapper.DynamicParameters(new
                        {
                            Currency = reimbursement.L_Currency_Code,
                            BasicAmount = reimbursement.BasicAmount,
                            RateAmount = reimbursement.RateAmount,
                            Amount = reimbursement.Amount,
                            Qty = reimbursement.Qty,
                            GrossUp = reimbursement.GrossUp,
                            OtherCostSubCategoryId = reimbursement.OtherCost_SubCategoryId,
                            LastUpdatedBy = reimbursement.LastUpdatedBy,
                            LastUpdatedTime = DateTime.Now,
                            ReimbursementDetailId = reimbursement.ReimbursementDetailId,
                            IsCalculate = reimbursement.IsCalculate,
                            IsIncludeGrandTotal = reimbursement.IsIncludeGrandTotal,
                            DppFinance = reimbursement.DppFinance,
                        }), commandType: CommandType.Text));
                    }

                }
                decimal amountDetail = param.OtherCost.Select(q => q.BasicAmount).FirstOrDefault();
                string reason = param.Reason;
                string lastUpdatedBy = param.OtherCost != null ? param.OtherCost[0].LastUpdatedBy : string.Empty;

                var queryUpdateAmount = $@"DECLARE @AmountCoefficient float = @AmountDetail
                                           DECLARE @SumOtherCosts float     = (SELECT COALESCE(SUM(Amount),0)  FROM ReimbursementDetailOtherCost WHERE ReimbursementDetailId = @ReimbursementDetailId AND IsIncludeGrandTotal = 1 )
                                           DECLARE @TotalAmount float       = @AmountCoefficient + @SumOtherCosts

                                           UPDATE ReimbursementDetailCostCenter
                                           SET BasicAmount     = @TotalAmount * Rate * ([Percentage] / 100),
                                               Amount          = @TotalAmount * ([Percentage] / 100),
                                               LastUpdatedBy   = @LastUpdatedBy,
                                               LastUpdatedTime = GETDATE()
                                           WHERE ReimbursementDetailId = @ReimbursementDetailId
                                            
                                           UPDATE ReimbursementDetail
                                           SET Amount          = @AmountDetail,
                                               BaseAmount      = @AmountDetail * RateAmount,
                                               GrandTotal      = @TotalAmount,
                                               ReasonUpdate    = @Reason,
                                               LastUpdatedBy   = @LastUpdatedBy,
                                               LastUpdatedTime = GETDATE()
                                           WHERE Id = @ReimbursementDetailId
                                        ";
                return await Task.FromResult(_dapper.Update<int>(queryUpdateAmount, new Dapper.DynamicParameters(new
                {
                    ReimbursementDetailId = reimbursementDetailId,
                    LastUpdatedBy = lastUpdatedBy,
                    AmountDetail = amountDetail,
                    Reason = reason,
                }), commandType: CommandType.Text));
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Delete Reimbursement Detail Other Cost
        /// </summary>
        /// <param name="otherCostId"></param>
        /// <returns></returns>
        public async Task<int> DeleteOtherCostReimbursementDetail(int otherCostId, string userName)
        {
            try
            {
                var query = @$" DELETE FROM ReimbursementDetailOtherCost WHERE Id = @ReimbursementDetailOtherCostId";
                await Task.FromResult(_dapper.Execute(query, new Dapper.DynamicParameters(new
                {
                    ReimbursementDetailOtherCostId = otherCostId
                }), commandType: CommandType.Text));

                var queryRecalculate = $@" DECLARE @ReimbursementDetailId int  = (SELECT TOP 1 ReimbursementDetailId FROM ReimbursementDetailOtherCost WHERE Id = @ReimbursementDetailOtherCostId)
                                           DECLARE @SumOtherCosts float        = (SELECT COALESCE(SUM(Amount),0)  FROM ReimbursementDetailOtherCost WHERE ReimbursementDetailId = @ReimbursementDetailId AND IsIncludeGrandTotal = 1 )
                                           DECLARE @AmountCoefficient float    = (SELECT Amount FROM ReimbursementDetail WHERE Id = @ReimbursementDetailId)
                                           DECLARE @TotalAmount float          = @AmountCoefficient + @SumOtherCosts

                                           UPDATE ReimbursementDetailCostCenter
                                           SET BasicAmount     = @TotalAmount * Rate * ([Percentage] / 100),
                                               Amount          = @TotalAmount * ([Percentage] / 100),
                                               LastUpdatedBy   = @LastUpdatedBy,
                                               LastUpdatedTime = GETDATE()
                                           WHERE ReimbursementDetailId = @ReimbursementDetailId
                                            
                                           UPDATE ReimbursementDetail
                                           SET GrandTotal      = @TotalAmount,
                                               LastUpdatedBy   = @LastUpdatedBy,
                                               LastUpdatedTime = GETDATE()
                                           WHERE Id = @ReimbursementDetailId
                                        ";
                return await Task.FromResult(_dapper.Update<int>(queryRecalculate, new Dapper.DynamicParameters(new
                {
                    ReimbursementDetailOtherCostId = otherCostId,
                    LastUpdatedBy = userName
                }), commandType: CommandType.Text));

            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Update Settlement Detail Other Cost
        /// </summary>
        /// <param name="settlementDetailId"></param>
        /// <returns></returns>
        public async Task<int> UpdateSettlementDetailOtherCost(int settlementDetailId, FinanceOtherCost param)
        {
            try
            {
                foreach (var settlement in param.OtherCost)
                {
                    var query = $@"
                                   DECLARE @Rate MONEY               = (SELECT TOP 1 rd.RateAmount from Settlement s
								                                        JOIN   SettlementDetail sd on sd.SettlementId = s.Id
								                                        JOIN   Reimbursement r on s.ReimbursementId = r.Id
								                                        JOIN   ReimbursementDetail rd on rd.ReimbursementId = r.Id
								                                        WHERE  sd.Id = @SettlementDetailId)
                                   DECLARE @AmountDetail DECIMAL     = (SELECT Amount from SettlementDetail 
                                                                        WHERE  Id = @SettlementDetailId)
               
                                   IF NOT EXISTS (
                                                    SELECT * FROM SettlementDetailOtherCost
                                                    WHERE SettlementDetailId = @SettlementDetailId AND OtherCost_SubCategoryId = @OtherCostSubCategoryId
                                                 )
                                  
                                   BEGIN
                                        INSERT INTO SettlementDetailOtherCost 
                                                    ( SettlementDetailId
                                                     ,BasicAmount
                                                     ,Amount
                                                     ,Qty
                                                     ,OtherCost_SubCategoryId
                                                     ,GrossUp
                                                     ,DppGrossUp
                                                     ,CreatedTime
                                                     ,CreatedBy )
                                        VALUES     (  @SettlementDetailId
                                                     ,@Amount * @Rate
                                                     ,@Amount
                                                     ,@Qty
                                                     ,@OtherCostSubCategoryId
                                                     ,CASE WHEN @GrossUp > 0
                                                           THEN @GrossUp
                                                           ELSE NULL
                                                      END
                                                     ,CASE WHEN @GrossUp > 0
                                                           THEN @GrossUp + @AmountDetail 
                                                           ELSE NULL
                                                      END
                                                     ,GETDATE()
                                                     ,@LastUpdatedBy )
                                   END
                                   ELSE
                                   BEGIN
                                        UPDATE [dbo].[SettlementDetailOtherCost]
                                        SET 
                                             [BasicAmount] = @Amount * @Rate
                                            ,[Amount] = @Amount
                                            ,[Qty] = @Qty
                                            ,[OtherCost_SubCategoryId] = @OtherCostSubCategoryId
                                            ,[GrossUp] =    CASE WHEN @GrossUp > 0 
                                                                 THEN @GrossUp 
                                                                 ELSE NULL
                                                            END
                                            ,[DppGrossUp] = CASE WHEN @GrossUp > 0 
                                                                 THEN @GrossUp + @AmountDetail 
                                                                 ELSE NULL
                                                            END
                                            ,[LastUpdatedBy] = @LastUpdatedBy
                                            ,[LastUpdatedTime] = @LastUpdatedTime
                                        WHERE Id = @Id
                                   END 
                                   ";
                    await Task.FromResult(_dapper.Update<FinanceOtherCost>(query, new Dapper.DynamicParameters(new
                    {
                        BasicAmount = settlement.BasicAmount,
                        Amount = settlement.Amount,
                        Qty = settlement.Qty,
                        OtherCostSubCategoryId = settlement.OtherCost_SubCategoryId,
                        GrossUp = settlement.GrossUp,
                        LastUpdatedBy = settlement.LastUpdatedBy,
                        LastUpdatedTime = DateTime.Now,
                        Id = settlement.Id,
                        SettlementDetailId = settlement.SettlementDetailId
                    }), commandType: CommandType.Text));
                }
                return 200;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Submit Maker Finance
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<FinanceResponseActionRequest> SubmitTransactionMaker(FinanceRequest param)
        {
            FinanceResponseActionRequest result = new FinanceResponseActionRequest();
            try
            {
                string statusRequest = StatusRequest.SetStatusRequest(param.StatusRequest);
                string querySubmitTransaction = @"UPDATE ApprovalRequest
                                                  SET [Status] = @Status
                                                  WHERE RequestNo = @RequestNumber ";

                string additionalQuery = param.RequestType?.ToLower() switch
                {
                    "travel settlement" => Finance.SubmitFinMakerTRSTL(),
                    "invoice travel" => Finance.SubmitFinMakerINVTR(),
                    "ger" => Finance.SubmitFinMakerGER(),
                    "trexapr" => Finance.SubmitFinMakerTrexAPR(),
                    "trexger" => Finance.SubmitFinMakerTrexGER(),
                    "trexeer" => Finance.SubmitFinMakerTrexEER(),
                    "trexter" => Finance.SubmitFinMakerTrexTER(),
                    _ => Finance.SubmitFinMakerRI() // Default RI/CA/CATR
                };

                string finalQuery = querySubmitTransaction + additionalQuery;
                await Task.FromResult(_dapper.Update<int>(
                    finalQuery,
                    new Dapper.DynamicParameters(new
                    {
                        RequestNumber = param.RequestNumber,
                        LastUpdatedBy = param.LastUpdatedBy,
                        Status = param.StatusRequest
                    }),
                    commandType: CommandType.Text
                ));

                result.RequestNumber = param.RequestNumber;
                result.Action = statusRequest;
                result.Message = "Successfully Submitted";

                return result;

            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Reject Maker Finance
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<FinanceResponseActionRequest> RejectTransactionMaker(FinanceRequest param)
        {
            FinanceResponseActionRequest financeResponse = new FinanceResponseActionRequest();
            try
            {
                NotificationModel notification = new NotificationModel();

                if (param.StatusRequest == 3)
                {
                    await HandleRejectTransactionMaker(param, financeResponse, notification);
                }
                return financeResponse;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Repair Maker Finance
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<FinanceResponseActionRequest> RepairTransactionMaker(FinanceRequest param)
        {
            FinanceResponseActionRequest result = new FinanceResponseActionRequest();
            try
            {
                NotificationModel notification = new NotificationModel();

                if (param.StatusRequest == 4)
                {
                    await HandleRepairTransactionMaker(param, result, notification);
                }
                return result;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Take Out Maker Finance
        /// Currently only for request type GER ( 10-7-2025 )
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<FinanceResponseActionRequest> TakeOutTransactionMaker(FinanceRequest param)
        {
            FinanceResponseActionRequest result = new FinanceResponseActionRequest();
            try
            {
                NotificationModel notification = new NotificationModel();

                if (param.StatusRequest == 2 || param.StatusRequest == 7)
                {
                    await HandleTakeOutTransactionMaker(param, result, notification);
                }
                return result;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        private async Task HandleTakeOutTransactionMaker(FinanceRequest param, FinanceResponseActionRequest financeResponse, NotificationModel notification)
        {
            // get detail request
            var detRequest = await Task.FromResult(_dapper.Get<ApprovalRequest>("SELECT *FROM ApprovalRequest WHERE RequestNo = @RequestNumber", new Dapper.DynamicParameters(new
            {
                RequestNumber = param.RequestNumber
            }), CommandType.Text));

            string statusRequest = AppSystem.StatusTakeOut;
            List<AttachmentModel> attachment = new List<AttachmentModel>();
            var paramReimbursement = new ParamEmailReimbursement();
            var paramTravelSettlement = new ParamEmailTravelSettlement();

            if (param.RequestType.Equals("ger", StringComparison.CurrentCultureIgnoreCase))
            {
                await _submissionApprovalRepository.UpdateStatusGer(param.RequestNumber, param.StatusRequest, param.LastUpdatedBy, param.ReasonReject);
                paramReimbursement = await _submissionApprovalRepository.SetBodyEmailGer(param.RequestNumber, param.RequestorName, param.RequestDetailId);
                attachment = paramReimbursement.Attachments;
                notification.RequestId = paramReimbursement.RequestId;
            }

            var attachments = new Dictionary<string, string>();
            if (attachment.Count > 0)
                attachments.Add(attachment[0].OriginalFileName, attachment[0].FullPath);

            // parameter send email notification
            notification.SubjectEmail = $"{statusRequest} Request";
            notification.RequestType = param.RequestType;
            notification.RequestNumber = param.RequestNumber;
            notification.ParamReimbursement = paramReimbursement;
            notification.ParamTravel = null;
            notification.ParamTravelSettlement = null;
            notification.ApprovalRequestGroupMember = null;
            notification.RequestorName = param.RequestorName;
            notification.RequestorEmail = param.RequestorEmail;
            notification.StatusRequest = statusRequest;
            notification.Attachments = attachments;
            notification.ActionBy = param.LastUpdatedBy;
            notification.CreatorEmail = detRequest.CreatorEmail;

            var saveToLog = JsonConvert.SerializeObject(notification);
            log.LogInitialize(methodName: "Take Out Detail", saveToLog, LogType.Info);

            financeResponse.RequestNumber = param.RequestNumber;
            financeResponse.Action = statusRequest;
            financeResponse.Message = "Successfully Take Out";
            _notificationService.SendEmail(notification);
        }

        private async Task HandleRejectTransactionMaker(FinanceRequest param, FinanceResponseActionRequest financeResponse, NotificationModel notification)
        {
            // get detail request
            var detRequest = await Task.FromResult(_dapper.Get<ApprovalRequest>("SELECT *FROM ApprovalRequest WHERE RequestNo = @RequestNumber", new Dapper.DynamicParameters(new
            {
                RequestNumber = param.RequestNumber
            }), CommandType.Text));

            string statusRequest = StatusRequest.SetStatusRequest(param.StatusRequest);
            decimal amount = 0;
            List<AttachmentModel> attachment = new List<AttachmentModel>();
            var paramReimbursement = new ParamEmailReimbursement();
            var paramTravelSettlement = new ParamEmailTravelSettlement();

            #region update request to status reject (3)
            if (param.RequestType.Equals("reimbursement", StringComparison.CurrentCultureIgnoreCase) || param.RequestType.Equals("cash advance", StringComparison.CurrentCultureIgnoreCase))
            {
                _ = await Task.FromResult(_dapper.Update<ReimbursementModel>($@" UPDATE [dbo].[Reimbursement]
                                                                                         SET [Status] = @Status
                                                                                            ,[LastUpdatedBy] = @LastUpdatedBy
                                                                                            ,[LastUpdatedTime] = @LastUpdatedTime
                                                                                            ,[ReasonReject] = @ReasonReject
                                                                                         WHERE ApprovalRequestId = @RequestId", new Dapper.DynamicParameters(new
                {
                    Status = param.StatusRequest,
                    LastUpdatedTime = DateTime.Now,
                    LastUpdatedBy = param.LastUpdatedBy,
                    RequestId = detRequest.Id,
                    ReasonReject = param.ReasonReject,
                }), commandType: CommandType.Text));
                // get amount to send as param in body email
                var queryGetAmount = $@"SELECT ISNULL(SUM(rcc.BasicAmount),0) AS TotalAmount FROM ReimbursementDetailCostCenter rcc
                                                JOIN ReimbursementDetail rd ON rd.Id = rcc.ReimbursementDetailId
                                                JOIN Reimbursement r ON r.Id = rd.ReimbursementId
                                                WHERE r.RequestNumber = @RequestNumber";
                amount = await Task.FromResult(_dapper.Get<decimal>(queryGetAmount, new Dapper.DynamicParameters(new
                {
                    RequestNumber = param.RequestNumber
                }), commandType: CommandType.Text));

                // get attachment url path to send as param in body email
                var queryGetAttachment = $@"SELECT * FROM Attachment a 
                                                    JOIN ReimbursementDetail rd ON a.Id = rd.AttachmentId 
                                                    JOIN Reimbursement r ON rd.ReimbursementId = r.Id
                                                    WHERE r.RequestNumber = @RequestNumber";
                attachment = await Task.FromResult(_dapper.GetAll<AttachmentModel>(queryGetAttachment, new Dapper.DynamicParameters(new
                {
                    RequestNumber = param.RequestNumber
                }), commandType: CommandType.Text));

                paramReimbursement.Remark = "Reject Transaction";
                paramReimbursement.AmountString = string.Format("{0:C}", amount).Replace("$", "Rp. ");
                paramReimbursement.RequestNumber = param.RequestNumber;
            }
            else if (param.RequestType.Equals("travel", StringComparison.CurrentCultureIgnoreCase))
            {
                var queryUpdate = $@" UPDATE TravelRequest
                                          SET 
                                            Status = @Status,
                                            LastUpdatedTime = @LastUpdatedTime,
                                            LastUpdatedBy = @LastUpdatedBy
                                        WHERE ApprovalRequestId = @ApprovalRequestId";
                _ = await Task.FromResult(_dapper.Execute(queryUpdate, new Dapper.DynamicParameters(new
                {
                    ApprovalRequestId = detRequest.Id,
                    Status = param.StatusRequest,
                    LastUpdatedTime = DateTime.Now,
                    LastUpdatedBy = param.RequestorName,
                    RequestNumber = param.RequestNumber
                }), commandType: CommandType.Text));
            }
            else if (param.RequestType.Equals("travel settlement", StringComparison.CurrentCultureIgnoreCase))
            {
                var queryUpdate = $@" UPDATE TravelRequestExpense
                                          SET 
                                            Status = @Status,
                                            LastUpdatedTime = @LastUpdatedTime,
                                            LastUpdatedBy = @LastUpdatedBy,
                                            ReasonReject = @ReasonReject
                                        WHERE ApprovalRequestId = @ApprovalRequestId";
                _ = await Task.FromResult(_dapper.Execute(queryUpdate, new Dapper.DynamicParameters(new
                {
                    ApprovalRequestId = detRequest.Id,
                    Status = param.StatusRequest,
                    LastUpdatedTime = DateTime.Now,
                    LastUpdatedBy = param.RequestorName,
                    ReasonReject = param.ReasonReject
                }), commandType: CommandType.Text));

                paramTravelSettlement = await _submissionApprovalRepository.SetBodyEmailTRSTL(detRequest.Id.ToString());
                paramTravelSettlement.Remark = param.ReasonReject;
                attachment = paramTravelSettlement.Attachments;
                notification.RequestId = paramTravelSettlement.RequestId;

            }
            else if (param.RequestType.Equals("invoice travel", StringComparison.CurrentCultureIgnoreCase))
            {
                await _submissionApprovalRepository.UpdateStatusInvoiceTravel(param.RequestNumber, param.StatusRequest, param.LastUpdatedBy, param.ReasonReject);
                paramReimbursement = await _submissionApprovalRepository.SetBodyEmailInvoiceTravel(param.RequestNumber, param.RequestorName);
                attachment = paramReimbursement.Attachments;
                notification.RequestId = paramReimbursement.RequestId;
            }
            else if (param.RequestType.Equals("ger", StringComparison.CurrentCultureIgnoreCase))
            {
                await _submissionApprovalRepository.UpdateStatusGer(param.RequestNumber, param.StatusRequest, param.LastUpdatedBy, param.ReasonReject);
                paramReimbursement = await _submissionApprovalRepository.SetBodyEmailGer(param.RequestNumber, param.RequestorName);
                attachment = paramReimbursement.Attachments;
                notification.RequestId = paramReimbursement.RequestId;
            }
            else if (param.RequestType.Equals("settlement", StringComparison.CurrentCultureIgnoreCase))
            {
                RequestApprovalMatrix requestApprovalMatrix = new RequestApprovalMatrix();
                requestApprovalMatrix.RequestType = param.RequestType;
                requestApprovalMatrix.RequestNumber = param.RequestNumber;
                requestApprovalMatrix.RequestorName = param.RequestorName;
                requestApprovalMatrix.RequestorEmail = param.RequestorEmail;
                requestApprovalMatrix.SettlementNumber = param.SettlementNumber;
                requestApprovalMatrix.StatusRequest = param.StatusRequest;
                requestApprovalMatrix.ReasonReject = param.ReasonReject;
                requestApprovalMatrix.RequestType = param.RequestType;
                requestApprovalMatrix.ApprovalFlowName = param.ApprovalFlowName;
                requestApprovalMatrix.AccountApprovalId = param.AccountApprovalId;
                requestApprovalMatrix.LastUpdatedBy = param.LastUpdatedBy;

                _ = await _approvalMatrixRepository.RejectRequestSettlement(requestApprovalMatrix);
                financeResponse.RequestNumber = param.RequestNumber;
                financeResponse.Action = statusRequest;
                financeResponse.Message = "Successfully Rejected";
            }
            else if (param.RequestType.StartsWith("trex", StringComparison.CurrentCultureIgnoreCase))
            {
                string queryUpdate = param.RequestType.ToLower() switch
                {
                    "trexapr" => Finance.RejectFinanceTrexApr(),
                    "trexeer" => Finance.RejectFinanceTrexEer(),
                    "trexger" => Finance.RejectFinanceTrexGer(),
                    "trexter" => Finance.RejectFinanceTrexTer(),
                    _ => "",
                };

                _dapper.Execute(queryUpdate, new Dapper.DynamicParameters(new
                {
                    RequestNumber = param.RequestNumber,
                    Status = param.StatusRequest,
                    LastUpdatedTime = DateTime.Now,
                    LastUpdatedBy = param.LastUpdatedBy,
                }), commandType: CommandType.Text);

                _ = RejectToTrex(param);
                return;
            }

            _ = await Task.FromResult(_dapper.Update<ApprovalRequest>($@"   UPDATE [dbo].[ApprovalRequest]
                                                                                    SET [Status] = @Status
                                                                                    ,[LastUpdatedBy] = @LastUpdatedBy
                                                                                    ,[LastUpdatedTime] = @LastUpdatedTime
                                                                                    WHERE Id = @RequestId", new Dapper.DynamicParameters(new
            {
                Status = param.StatusRequest,
                LastUpdatedTime = DateTime.Now,
                LastUpdatedBy = param.LastUpdatedBy,
                RequestId = detRequest.Id
            }), commandType: CommandType.Text));

            #endregion

            var members = new List<ResponseApprovalRequestGroupMember>();
            string queryGetMember = $@" SELECT (SELECT argm.[Id], ApprovaGroup_SubCategoryId [ApprovalGroup_SubCategoryId], argm.CostCenterId, AccountId, UserName, email, argm.[Level], argm.[Sequence], argm.[Status], 
                                                ApprovalRequestEmail = (SELECT are.[Guid], are.[Action], are.[LinkType], are.[URLAction], argm.[AccountId], are.[Status] FROM ApprovalRequestEmail are WHERE are.ApprovalRequestGroupMemberId = argm.Id FOR JSON PATH)
                                                FROM ApprovalRequestGroupMember argm
                                                JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id
                                                WHERE ar.RequestNo = @RequestNumber
                                                AND argm.AccountId = @AccountApprovalId
                                                FOR JSON PATH) AS ApprovalMatrix";
            var memberReject = await Task.FromResult(_dapper.Get<Workflow>(queryGetMember, new Dapper.DynamicParameters(new
            {
                RequestNumber = param.RequestNumber,
                AccountApprovalId = param.AccountApprovalId,
            }), commandType: CommandType.Text));
            if (memberReject.ApprovalMatrix != null)
            {
                members = JsonConvert.DeserializeObject<List<ResponseApprovalRequestGroupMember>>(memberReject.ApprovalMatrix);
            }

            var attachments = new Dictionary<string, string>();
            if (attachment.Count > 0)
                attachments.Add(attachment[0].OriginalFileName, attachment[0].FullPath);

            // parameter send email notification
            notification.SubjectEmail = $"{statusRequest} Request";
            notification.RequestType = param.RequestType;
            notification.RequestNumber = param.RequestNumber;
            notification.ParamReimbursement = paramReimbursement;
            notification.ParamTravel = null;
            notification.ParamTravelSettlement = paramTravelSettlement;
            notification.RequestorName = param.RequestorName;
            notification.RequestorEmail = param.RequestorEmail;
            notification.StatusRequest = statusRequest;
            notification.Attachments = attachments;
            notification.ActionBy = param.LastUpdatedBy;
            notification.CreatorEmail = detRequest.CreatorEmail;
            notification.ApprovalRequestGroupMember = members.Count > 0 ? members : null;

            var saveToLog = JsonConvert.SerializeObject(notification);
            log.LogInitialize(methodName: "Reject Request", saveToLog, LogType.Info);

            #region subtraction budget on process transaction
            await _budgetTransactionRepository.InsertBudgetTransactionByRequestType(param.RequestType, param.RequestNumber, param.RequestorName, false);
            #endregion

            financeResponse.RequestNumber = param.RequestNumber;
            financeResponse.Action = statusRequest;
            financeResponse.Message = "Successfully Rejected";
            _notificationService.SendEmail(notification);
        }

        private async Task HandleRepairTransactionMaker(FinanceRequest param, FinanceResponseActionRequest result, NotificationModel notification)
        {
            // get detail request
            var detRequest = await Task.FromResult(_dapper.Get<ApprovalRequest>("SELECT *FROM ApprovalRequest WHERE RequestNo = @RequestNumber", new Dapper.DynamicParameters(new
            {
                RequestNumber = param.RequestNumber
            }), CommandType.Text));

            string statusRequest = StatusRequest.SetStatusRequest(param.StatusRequest);
            List<AttachmentModel> attachment = new List<AttachmentModel>();
            var paramReimbursement = new ParamEmailReimbursement();
            var paramTravelSettlement = new ParamEmailTravelSettlement();

            #region update request by type to status repair (4)
            if (param.RequestType.Equals("reimbursement", StringComparison.CurrentCultureIgnoreCase) || param.RequestType.StartsWith("cash advance", StringComparison.CurrentCultureIgnoreCase))
            {
                _ = await Task.FromResult(_dapper.Update<ReimbursementModel>($@" UPDATE [dbo].[Reimbursement]
                                                                                         SET [Status] = @Status
                                                                                            ,[LastUpdatedBy] = @LastUpdatedBy
                                                                                            ,[LastUpdatedTime] = @LastUpdatedTime
                                                                                            ,[ReasonReject] = @ReasonReject
                                                                                         WHERE ApprovalRequestId = @RequestId", new Dapper.DynamicParameters(new
                {
                    Status = param.StatusRequest,
                    LastUpdatedTime = DateTime.Now,
                    LastUpdatedBy = param.LastUpdatedBy,
                    RequestId = detRequest.Id,
                    ReasonReject = param.ReasonReject,
                }), commandType: CommandType.Text));

                _ = await Task.FromResult(_dapper.Update<ReimbursementModel>($@" UPDATE [dbo].[ReimbursementDetail]
                                                                                         SET [Status] = @Status
                                                                                            ,[LastUpdatedBy] = @LastUpdatedBy
                                                                                            ,[LastUpdatedTime] = @LastUpdatedTime
                                                                                         WHERE ReimbursementId = (SELECT Id from Reimbursement WHERE ApprovalRequestId = @RequestId)", new Dapper.DynamicParameters(new
                {
                    Status = param.StatusRequest,
                    LastUpdatedTime = DateTime.Now,
                    LastUpdatedBy = param.LastUpdatedBy,
                    RequestId = detRequest.Id,
                    ReasonReject = param.ReasonReject,
                }), commandType: CommandType.Text));

                paramReimbursement = await _submissionApprovalRepository.SetBodyEmailRI(param.RequestNumber, param.RequestorName);
                attachment = paramReimbursement.Attachments;
                notification.RequestId = paramReimbursement.RequestId;
            }
            else if (param.RequestType.Equals("travel", StringComparison.CurrentCultureIgnoreCase))
            {
                var queryUpdate = $@" UPDATE TravelRequest
                                          SET 
                                            Status = @Status,
                                            LastUpdatedTime = @LastUpdatedTime,
                                            LastUpdatedBy = @LastUpdatedBy
                                        WHERE ApprovalRequestId = @ApprovalRequestId";
                _ = await Task.FromResult(_dapper.Execute(queryUpdate, new Dapper.DynamicParameters(new
                {
                    ApprovalRequestId = detRequest.Id,
                    Status = param.StatusRequest,
                    LastUpdatedTime = DateTime.Now,
                    LastUpdatedBy = param.RequestorName,
                    RequestNumber = param.RequestNumber
                }), commandType: CommandType.Text));
            }
            else if (param.RequestType.Equals("travel settlement", StringComparison.CurrentCultureIgnoreCase))
            {
                var queryUpdate = $@" UPDATE TravelRequestExpense
                                          SET 
                                            Status = @Status,
                                            LastUpdatedTime = @LastUpdatedTime,
                                            LastUpdatedBy = @LastUpdatedBy,
                                            ReasonReject = @ReasonReject
                                        WHERE ApprovalRequestId = @ApprovalRequestId";
                _ = await Task.FromResult(_dapper.Execute(queryUpdate, new Dapper.DynamicParameters(new
                {
                    ApprovalRequestId = detRequest.Id,
                    Status = param.StatusRequest,
                    LastUpdatedTime = DateTime.Now,
                    LastUpdatedBy = param.RequestorName,
                    RequestNumber = param.RequestNumber,
                    ReasonReject = param.ReasonReject
                }), commandType: CommandType.Text));

                paramTravelSettlement = await _submissionApprovalRepository.SetBodyEmailTRSTL(detRequest.Id.ToString());
                paramTravelSettlement.Remark = param.ReasonReject;
                attachment = paramTravelSettlement.Attachments;
                notification.RequestId = paramTravelSettlement.RequestId;
            }
            else if (param.RequestType.Equals("ger", StringComparison.CurrentCultureIgnoreCase))
            {
                var queryUpdate = $@" UPDATE GerHeader
                                          SET 
                                            Status = @Status,
                                            ReasonReject = @Reason,
                                            LastUpdatedTime = @LastUpdatedTime,
                                            LastUpdatedBy = @LastUpdatedBy
                                        WHERE RequestNumber = @RequestNumber";
                _ = await Task.FromResult(_dapper.Execute(queryUpdate, new Dapper.DynamicParameters(new
                {
                    Reason = param.ReasonReject,
                    Status = param.StatusRequest,
                    LastUpdatedTime = DateTime.Now,
                    LastUpdatedBy = param.RequestorName,
                    RequestNumber = param.RequestNumber
                }), commandType: CommandType.Text));

                paramReimbursement = await _submissionApprovalRepository.SetBodyEmailGer(param.RequestNumber, param.RequestorName);
                attachment = paramReimbursement.Attachments;
                notification.RequestId = paramReimbursement.RequestId;
            }

            _ = await Task.FromResult(_dapper.Update<ApprovalRequest>($@"UPDATE [dbo].[ApprovalRequest]
                                                                                 SET [Status] = @Status
                                                                                    ,[LastUpdatedBy] = @LastUpdatedBy
                                                                                    ,[LastUpdatedTime] = @LastUpdatedTime
                                                                                 WHERE Id = @RequestId", new Dapper.DynamicParameters(new
            {
                Status = param.StatusRequest,
                LastUpdatedTime = DateTime.Now,
                LastUpdatedBy = param.LastUpdatedBy,
                RequestId = detRequest.Id
            }), commandType: CommandType.Text));

            #endregion

            var members = new List<ResponseApprovalRequestGroupMember>();
            string queryGetMember = $@" SELECT (SELECT argm.[Id], ApprovaGroup_SubCategoryId [ApprovalGroup_SubCategoryId], argm.CostCenterId, AccountId, UserName, email, argm.[Level], argm.[Sequence], argm.[Status], 
                                                ApprovalRequestEmail = (SELECT are.[Guid], are.[Action], are.[LinkType], are.[URLAction], argm.[AccountId], are.[Status] FROM ApprovalRequestEmail are WHERE are.ApprovalRequestGroupMemberId = argm.Id FOR JSON PATH)
                                                FROM ApprovalRequestGroupMember argm
                                                JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id
                                                WHERE ar.RequestNo = @RequestNumber
												AND argm.AccountId = @AccountApprovalId
                                                FOR JSON PATH) AS ApprovalMatrix";
            var memberRepair = await Task.FromResult(_dapper.Get<Workflow>(queryGetMember, new Dapper.DynamicParameters(new
            {
                RequestNumber = param.RequestNumber,
                AccountApprovalId = param.AccountApprovalId
            }), commandType: CommandType.Text));
            if (memberRepair.ApprovalMatrix != null)
            {
                members = JsonConvert.DeserializeObject<List<ResponseApprovalRequestGroupMember>>(memberRepair.ApprovalMatrix);
            }

            var attachments = new Dictionary<string, string>();
            if (attachment.Count > 0)
                attachments.Add(attachment[0].OriginalFileName, attachment[0].FullPath);

            // parameter send email notification
            notification.SubjectEmail = $"{statusRequest} Request";
            notification.RequestType = param.RequestType;
            notification.RequestNumber = param.RequestNumber;
            notification.ParamReimbursement = paramReimbursement;
            notification.ParamTravel = null;
            notification.ParamTravelSettlement = paramTravelSettlement;
            notification.RequestorName = param.RequestorName;
            notification.RequestorEmail = param.RequestorEmail;
            notification.StatusRequest = statusRequest;
            notification.Attachments = null;
            notification.ActionBy = param.LastUpdatedBy;
            notification.CreatorEmail = detRequest.CreatorEmail;
            notification.ApprovalRequestGroupMember = members.Count > 0 ? members : null;

            var saveToLog = JsonConvert.SerializeObject(notification);
            log.LogInitialize(methodName: "Repair Request", saveToLog, LogType.Info);
            _notificationService.SendEmail(notification);

            _ = await Task.FromResult(_dapper.Execute($@"UPDATE [dbo].[ApprovalRequest]
                                                                                 SET [Status] = @Status
                                                                                    ,[LastUpdatedBy] = @LastUpdatedBy
                                                                                    ,[LastUpdatedTime] = @LastUpdatedTime
                                                                                 WHERE Id = @RequestId", new Dapper.DynamicParameters(new
            {
                Status = param.StatusRequest,
                LastUpdatedTime = DateTime.Now,
                LastUpdatedBy = param.LastUpdatedBy,
                RequestId = detRequest.Id
            }), commandType: CommandType.Text));

            // update status member group approval
            string queryUpdateStatusMember = $@"UPDATE [dbo].[ApprovalRequestGroupMember]
                                                            SET [Status] = @Status
                                                            ,[LastUpdatedBy] = @LastUpdatedBy
                                                            ,[LastUpdatedTime] = @LastUpdatedTime
                                                        WHERE ApprovalRequestId = @RequestId AND AccountId = @AccountApprovalId";
            _ = await Task.FromResult(_dapper.Execute(queryUpdateStatusMember, new Dapper.DynamicParameters(new
            {
                Status = param.StatusRequest,
                LastUpdatedTime = DateTime.Now,
                LastUpdatedBy = param.LastUpdatedBy,
                RequestId = detRequest.Id,
                AccountApprovalId = param.AccountApprovalId
            }), commandType: CommandType.Text));

            #region update request header
            //Khusus transaksi di table Reimbursement (type reimbursement)
            await UpdateStatusTypeRI(param);
            #endregion

            result.RequestNumber = param.RequestNumber;
            result.Action = statusRequest;
            result.IsSuccess = true;
            result.Message = "Send Request to Repair";
        }

        private async Task UpdateStatusTypeRI(FinanceRequest param)
        {
            if (param.RequestType.Equals("reimbursement", StringComparison.CurrentCultureIgnoreCase) || param.RequestType.Equals("cash advance", StringComparison.CurrentCultureIgnoreCase))
            {
                await _submissionApprovalRepository.UpdateStatusRI(param.RequestNumber, param.StatusRequest, param.LastUpdatedBy, String.Empty);
            }
        }

        /// <summary>
        /// Get Voucher Header
        /// </summary>
        /// <param name="voucherId"></param>
        /// <returns></returns>
        private async Task<FinanceVoucherHeaderResponse> GetVoucherHeader(string voucherId, string voucherNumber)
        {
            try
            {
                var subQuery = @" Id = @Id AND VoucherNumber = @VoucherNumber";
                if (string.IsNullOrEmpty(voucherId))
                {
                    subQuery = subQuery.Replace("Id = @Id", "1=1");
                }
                if (string.IsNullOrEmpty(voucherNumber))
                {
                    subQuery = subQuery.Replace("VoucherNumber = @VoucherNumber", "1=1");
                }

                var queryGetVoucher = $@"SELECT * FROM VoucherHeader 
                                         WHERE {subQuery}";
                var data = await Task.FromResult(_dapper.Get<FinanceVoucherHeaderResponse>(queryGetVoucher, new Dapper.DynamicParameters(new
                {
                    Id = string.IsNullOrEmpty(voucherId) ? 0 : Convert.ToInt32(voucherId),
                    VoucherNumber = voucherNumber
                }), commandType: CommandType.Text));
                return data;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Voucher by Request No
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<FinanceVoucherHeaderResponse>> GetVoucherList(ParamGetVoucherList param)
        {
            try
            {
                string category = param.VoucherCategory switch
                {
                    "RI" => "Reimbursement",
                    "TR" => "Travel",
                    "TRSTL" => "Travel Settlement",
                    "SC" => "Shopping Cart",
                    "CA" => "Cash Advance",
                    "CATR" => "Cash Advance",
                    "NON" => "Non Shopping Cart",
                    "PO" => "Purchase Order",
                    "INVTR" => "Invoice Travel",
                    "TREXAPR" => "Trex Apr",
                    "TREXEER" => "Trex Eer",
                    "TREXGERRI" => "Trex Ger",
                    "TREXGERSTL" => "Trex Ger",
                    "TREXTER" => "Trex Ter",
                    _ => "",
                };
                if (ExpenseGER.Contains(param.VoucherCategory))
                {
                    category = "Ger";
                    param.ExpenseType = param.VoucherCategory;
                }
                var queryGetVoucher = Finance.GetVoucherList(param);
                var data = await Task.FromResult(_dapper.GetAll<FinanceVoucherHeaderResponse>(queryGetVoucher, new Dapper.DynamicParameters(new
                {
                    VoucherNumber = param.VoucherNumber,
                    VoucherCategory = category,
                    ExpenseType = "%" + param.ExpenseType + "%",
                    CheckerMCM = param.CheckerMCM,
                    Status = param.Status,
                    IsExport = param.IsExport,
                    StartDate = param.StartDate,
                    EndDate = param.EndDate,
                    Page = param.Page,
                    PageSize = param.PageSize,
                    SortColumn = param.SortColumn ?? "vh.CreatedTime",
                    SortDirection = param.SortDirection ?? "desc",
                    Search = param.Search?.Value

                }), commandType: CommandType.Text));
                return data;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Update Payment Top 1
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<FinanceUpdatePaymentResponse> GetUpdatePayment(ParamGetUpdatePaymentList param)
        {
            try
            {
                param.IsExport = true; // Set to true
                var voucherHeader = await GetVoucherHeader(null, param.VoucherNumber);

                string queryGetPaymentList = voucherHeader.Category switch
                {
                    "Reimbursement" => Finance.GetUpdatePaymentListRI(param),
                    "Cash Advance" => Finance.GetUpdatePaymentListRI(param),
                    "Cash Advance Travel" => Finance.GetUpdatePaymentListRI(param),
                    "Invoice Travel" => Finance.GetUpdatePaymentListINVTR(param),
                    "Ger" => Finance.GetUpdatePaymentListGER(param),
                    "Travel Settlement" => Finance.GetUpdatePaymentListTRSTL(param),
                    "Trex Apr" => Finance.GetUpdatePaymentListTrexAPR(param),
                    "Trex Eer" => Finance.GetUpdatePaymentListTrexEER(param),
                    "Trex Ger" => Finance.GetUpdatePaymentListTrexGER(param),
                    "Trex Ter" => Finance.GetUpdatePaymentListTrexTER(param),
                    "Shopping Cart" => Finance.GetUpdatePaymentListSC(param),
                    "Non Shopping Cart" => Finance.GetUpdatePaymentListNON(param),
                    _ => "",
                };
                var data = await Task.FromResult(_dapper.Get<FinanceUpdatePaymentResponse>(queryGetPaymentList, new Dapper.DynamicParameters(new
                {
                    VoucherNumber = param.VoucherNumber ?? string.Empty,
                    RequestNumber = param.RequestNumber ?? string.Empty,
                    BankAccountNumber = param.BankAccountNumber ?? string.Empty,
                    VoucherDetailId = param.VoucherDetailId ?? string.Empty,
                    Status = "1", //Status Approved Voucher
                }), commandType: CommandType.Text));
                return data;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get No Trans Exist on GLINonBenefit
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<FinanceUpdatePaymentResponse> GetTransferNumberExistGliProcess(ParamSubmitUpdatePaymentList param)
        {
            try
            {
                var queryGetExistTrfNo = Finance.GetTransferNumberExistGliProcess(param);
                var data = await Task.FromResult(_dapper.Get<FinanceUpdatePaymentResponse>(queryGetExistTrfNo, new Dapper.DynamicParameters(new
                {
                    TransferNumber = param.TransferNumber.Sanitize() ?? string.Empty,
                }), commandType: CommandType.Text));
                return data;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Update Payment List
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<FinanceUpdatePaymentResponse>> GetUpdatePaymentList(ParamGetUpdatePaymentList param)
        {
            try
            {
                var voucherHeader = await GetVoucherHeader(null, param.VoucherNumber);

                string queryGetPaymentList = voucherHeader.Category switch
                {
                    "Reimbursement" => Finance.GetUpdatePaymentListRI(param),
                    "Cash Advance" => Finance.GetUpdatePaymentListRI(param),
                    "Cash Advance Travel" => Finance.GetUpdatePaymentListRI(param),
                    "Invoice Travel" => Finance.GetUpdatePaymentListINVTR(param),
                    "Ger" => Finance.GetUpdatePaymentListGER(param),
                    "Travel Settlement" => Finance.GetUpdatePaymentListTRSTL(param),
                    "Trex Apr" => Finance.GetUpdatePaymentListTrexAPR(param),
                    "Trex Eer" => Finance.GetUpdatePaymentListTrexEER(param),
                    "Trex Ger" => Finance.GetUpdatePaymentListTrexGER(param),
                    "Trex Ter" => Finance.GetUpdatePaymentListTrexTER(param),
                    "Shopping Cart" => Finance.GetUpdatePaymentListSC(param),
                    "Non Shopping Cart" => Finance.GetUpdatePaymentListNON(param),
                    _ => "",
                };

                var data = await Task.FromResult(_dapper.GetAll<FinanceUpdatePaymentResponse>(queryGetPaymentList, new Dapper.DynamicParameters(new
                {
                    VoucherNumber = param.VoucherNumber ?? string.Empty,
                    RequestNumber = param.RequestNumber ?? string.Empty,
                    BankAccountNumber = param.BankAccountNumber ?? string.Empty,
                    VoucherDetailId = param.VoucherDetailId ?? string.Empty,
                    Status = "1", //Status Approved Voucher
                    Page = param.Page,
                    PageSize = param.PageSize,
                    SortColumn = param.SortColumn ?? "CreatedTime",
                    SortDirection = param.SortDirection ?? "desc",
                }), commandType: CommandType.Text));
                return data;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        public async Task<FinanceUpdatePaymentResponse> UpdatePayment(ParamSubmitUpdatePaymentList param, CancellationToken cancellationToken)
        {
            try
            {
                //Setup for Budget Transaction
                var getUpdatePayment = GetUpdatePaymentList(param).Result;
                string qryLastStatusTransfer = $@"DECLARE @GetVoucherId varchar(max) = (SELECT Id FROM VoucherHeader WHERE VoucherNumber = @VoucherNumber )
                                                  SELECT StatusTransfer FROM VoucherDetail WHERE VoucherId = @GetVoucherId AND VoucherRefId = @RequestNumber AND Id = @VoucherDetailId";
                if (string.IsNullOrEmpty(param.RequestNumber.Sanitize()))
                {
                    qryLastStatusTransfer = qryLastStatusTransfer.Replace("VoucherRefId = @RequestNumber", "1 = 1");
                }
                if (string.IsNullOrEmpty(param.BankAccountNumber.Sanitize()) && string.IsNullOrEmpty(param.VoucherDetailId.Sanitize()))
                {
                    qryLastStatusTransfer = qryLastStatusTransfer.Replace("Id = @VoucherDetailId", "1 = 1");
                }

                int? lastStatusTransfer = await Task.FromResult(_dapper.Get<int?>(qryLastStatusTransfer, new Dapper.DynamicParameters(new
                {
                    VoucherNumber = param.VoucherNumber.Sanitize(),
                    RequestNumber = param.RequestNumber.Sanitize(),
                    VoucherDetailId = getUpdatePayment.Select(x => x.VoucherDetailId).First(),
                }), commandType: CommandType.Text));

                //Update Payment
                var queryUpdatePayment = @" DECLARE @GetVoucherId varchar(max) = (SELECT Id FROM VoucherHeader WHERE VoucherNumber = @VoucherNumber )                
                                            UPDATE VoucherHeader
                                            SET TransferNumber  = @TransferNumber,
                                            	TransferType    = @TransferType,
                                            	TransferTime    = @TransferTime,
                                            	Attachment      = @AttachmentId,
                                            	LastUpdatedBy   = @Username,
                                            	LastUpdatedTime = GETDATE()
                                            WHERE Id = @GetVoucherId
                                            
                                            UPDATE VoucherDetail
                                            SET StatusTransfer  = @StatusTransfer,
                                                TransferNote    = @TransferNote,
                                            	LastUpdatedBy   = @Username,
                                            	LastUpdatedTime = GETDATE()  
                                            OUTPUT INSERTED.*
                                            WHERE VoucherId = @GetVoucherId AND VoucherRefId = @RequestNumber AND Id = @VoucherDetailId AND Id = @VoucherDetailId
                                                ";
                if (string.IsNullOrEmpty(param.RequestNumber.Sanitize()))
                {
                    queryUpdatePayment = queryUpdatePayment.Replace("VoucherRefId = @RequestNumber", "1 = 1");
                }
                if (string.IsNullOrEmpty(param.BankAccountNumber.Sanitize()) && string.IsNullOrEmpty(param.VoucherDetailId.Sanitize()))
                {
                    queryUpdatePayment = queryUpdatePayment.Replace("Id = @VoucherDetailId", "1 = 1");
                }

                var data = await Task.FromResult(_dapper.Update<FinanceUpdatePaymentResponse>(queryUpdatePayment, new Dapper.DynamicParameters(new
                {
                    StatusTransfer = param.StatusTransfer.Sanitize() ?? string.Empty,
                    TransferType = param.TransferType.Sanitize() ?? string.Empty,
                    TransferNote = param.TransferNote.Sanitize() ?? string.Empty,
                    TransferNumber = param.TransferNumber.Sanitize() ?? string.Empty,
                    TransferTime = param.TransferTime.Sanitize() ?? string.Empty,
                    AttachmentId = param.AttachmentId,
                    Username = param.Username.Sanitize(),
                    VoucherNumber = param.VoucherNumber.Trim().Sanitize(),
                    RequestNumber = param.RequestNumber.Trim().Sanitize(),
                    VoucherDetailId = getUpdatePayment.Select(x => x.VoucherDetailId).First(),
                }), commandType: CommandType.Text));

                if (data != null)
                {
                    #region subtraction budget on process transaction, shoppingcart not included as per 14-12-2023
                    await ActionUpdateBudgetTransactionOnPayment(getUpdatePayment, param.Username, lastStatusTransfer, param.StatusTransfer);
                    #endregion

                    #region kafka broker to trex
                    _ = UpdatePaymentToTrex(getUpdatePayment, param, cancellationToken);
                    #endregion
                }

                return data;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        private async Task UpdatePaymentToTrex(List<FinanceUpdatePaymentResponse> updatePaymentResponse, ParamSubmitUpdatePaymentList paramUpdatePayment, CancellationToken cancellationToken)
        {
            if (_trexToggle)
            {
                var firstResponse = updatePaymentResponse.FirstOrDefault();
                if (firstResponse?.Category?.Contains("trex", StringComparison.OrdinalIgnoreCase) != true)
                    return;

                var voucherHeader = await GetVoucherHeader(string.Empty, firstResponse.VoucherNumber);
                var type = firstResponse.Category.Split(' ').ElementAtOrDefault(1)?.ToUpper() ?? string.Empty;

                var trexResponse = new UpdatePaymentTrexResponse
                {
                    Type = type,
                    Data = updatePaymentResponse.Select(r => new UpdatePaymentTrexResponse.UpdatePaymentData
                    {
                        RequestNumber = r.VoucherRefId,
                        ValidateFinance = voucherHeader.ApproveDate,
                        Status = paramUpdatePayment.StatusTransfer == "0" ? AppSystem.Failed : AppSystem.Success,
                        Message = paramUpdatePayment.TransferNote
                    }).ToList()
                };

                await _kafkaService.UpdatePaymentAsync(trexResponse, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RejectToTrex(FinanceRequest request)
        {
            if (_trexToggle)
            {
                if (!request.RequestType?.StartsWith("trex", StringComparison.OrdinalIgnoreCase) ?? true)
                    return;

                var response = new UpdatePaymentTrexResponse
                {
                    Type = request.RequestType[4..].ToUpper(), // Extracts substring after "Trex"
                    Data = new List<UpdatePaymentTrexResponse.UpdatePaymentData>()
                       {
                           new UpdatePaymentTrexResponse.UpdatePaymentData
                           {
                               RequestNumber = request.RequestNumber,
                               ValidateFinance = DateTime.Now,
                               Status = AppSystem.StatusReject,
                               Message = request.ReasonReject
                           }
                       }
                };

                await _kafkaService.UpdatePaymentAsync(response, CancellationToken.None).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Status Pembayaran bisa terus berubah, 
        /// 1. ketika status terakhir belum ada pembayaran dan dilakukan aksi Failed Payment maka akan insert ke budget transaction InBudget = 0
        /// 2. ketika status terakhir sudah success pembayaran namun update menjadi failed maka ada 2 kondisi;
        ///     - jika dicek budget transaction atas request number dan InBudget (0 / budget keluar) data tsb tidak ada maka akan insert ke budget transaction InBudget = 0
        ///     - sebaliknya jika ada maka akan delete budget transaction atas request number dan InBudget = 0
        /// 3. ketika status terakhir failed pembayaran namun update menjadi success maka akan delete budget transaction atas request number dan InBudget = 0
        /// </summary>
        /// <param name="financeUpdatePayments"></param>
        /// <param name="username"></param>
        /// <param name="lastStatusTransfer"></param>
        /// <param name="reqStatusTransfer"></param>
        /// <returns></returns>
        public async Task ActionUpdateBudgetTransactionOnPayment(List<FinanceUpdatePaymentResponse> financeUpdatePayments, string username, int? lastStatusTransfer, string reqStatusTransfer)
        {
            if (financeUpdatePayments[0].VoucherRefId.Contains("INV"))
            {
                foreach (var v in financeUpdatePayments)
                {
                    await _budgetTransactionRepository.InsertBudgetTransactionPayment(v.VoucherRefId, lastStatusTransfer, reqStatusTransfer, username);
                }
            }
            else if (lastStatusTransfer is null && int.Parse(reqStatusTransfer) == 0)
            {
                await InsertBudgetOnUpdatePayment(financeUpdatePayments, username, false);
            }
            if (lastStatusTransfer == 1 && int.Parse(reqStatusTransfer) == 0)
            {
                foreach (var v in financeUpdatePayments)
                {
                    var checkBudget = await _budgetTransactionRepository.CheckBudgetTransaction(v.VoucherRefId, false);
                    if (checkBudget.Count == 0)
                        await InsertBudgetOnUpdatePayment(financeUpdatePayments, username, false);
                    else
                        await DeleteBudgetOnUpdatePayment(financeUpdatePayments, username, false);
                }
            }
            else if (lastStatusTransfer == 0 && int.Parse(reqStatusTransfer) == 1)
            {
                await DeleteBudgetOnUpdatePayment(financeUpdatePayments, username, false);
            }
        }

        public async Task DeleteBudgetOnUpdatePayment(List<FinanceUpdatePaymentResponse> financeUpdatePayments, string username, bool inBudget)
        {
            foreach (var requestNumber in financeUpdatePayments)
            {
                log.LogInitialize(methodName: "DeleteBudgetOnUpdatePayment", $"Delete with username {username}", LogType.Info);
                await _budgetTransactionRepository.DeleteBudgetTransactionOnPayment(requestNumber.VoucherRefId, inBudget);
            }
        }

        public async Task InsertBudgetOnUpdatePayment(List<FinanceUpdatePaymentResponse> financeUpdatePayments, string username, bool inBudget)
        {
            foreach (var voucherRefId in financeUpdatePayments.Select(q => q.VoucherRefId))
            {
                string requestType = string.Empty;
                if (voucherRefId.Contains("RI"))
                    requestType = "Reimbursement";
                else if (voucherRefId.Contains("CA"))
                    requestType = "Cash Advance";
                else if (voucherRefId.Contains("GER"))
                    requestType = "Ger";
                else if (voucherRefId.Contains("INV"))
                    requestType = "invoice procurement";
                await _budgetTransactionRepository.InsertBudgetTransactionByRequestType(requestType, voucherRefId, username, inBudget);
            }
        }

        public CommonResponse Finance_CreateVoucher_IsMixedWithMandiriVA(long[] invoicePOIdArray)
        {
            try
            {
                var conditionArrayQuery = @$"
select 'and ' + mt.Name + ' = ''' + mt.ValueStr + '''' as Condition
from MasterTable as mt
where 1 = 1
and mt.Status = 1
and mt.Category = 'ValidasiVAMandiri'
";
                var conditionArray = _dapper.GetDbconnection().Query(conditionArrayQuery).ToArray();
                var conditions = string.Join(Microsoft.VisualBasic.Strings.Space(1), conditionArray.Select(c => (string)c.Condition));

                var invoicePOArrayQuery = @$"
select
ipo.Id
, ipo.InvoiceNumber
, ipo.BankCode
, ipo.BankName
, (case when (1 = 1 and ipo.BankType = 'VA' /* conditions */ ) then cast(1 as bit) else cast(0 as bit) end) as ValidasiVAMandiri
from InvoicePO as ipo
where 1 = 1
and ipo.Id in @InvoicePOIdArray
";
                invoicePOArrayQuery = invoicePOArrayQuery.Replace("/* conditions */", conditions);
                var invoicePOArrayParamObject = new { InvoicePOIdArray = invoicePOIdArray };
                var invoicePOArrayDynamicParameters = new DynamicParameters(invoicePOArrayParamObject);
                var invoicePOArray = _dapper.GetDbconnection().Query(invoicePOArrayQuery, invoicePOArrayDynamicParameters).ToArray();

                var invoicePOVaMandiriArray = invoicePOArray.Where(x => (bool)x.ValidasiVAMandiri).ToArray();

                var isMixedWithMandiriVA = invoicePOVaMandiriArray.Length > 0 && invoicePOArray.Length != invoicePOVaMandiriArray.Length;

                var status = string.Empty;
                if (isMixedWithMandiriVA) status = @$"Kalo VA Bank Mandiri ({string.Join(" , ", invoicePOVaMandiriArray.Select(r => (string)r.InvoiceNumber))}) dipisah di, tapi kalo VA selain Bank Mandiri bisa di gabung";

                var cr = new CommonResponse() { Code = Microsoft.AspNetCore.Http.StatusCodes.Status200OK, Data = isMixedWithMandiriVA, Status = status };
                return cr;
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                throw new GlobalExceptions(baseException.Message, baseException);
            }
        }

        /// <summary>
        /// Get Voucher by Request No
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<FinanceVoucherDetail>> GetVoucherByRequestNumber(FinanceVoucherRequest param)
        {
            try
            {
                string stringJoin = string.Join(", ", param.RequestNumbers);
                var queryGetVoucher = "";
                if (param.Category != "SC")
                {
                    queryGetVoucher = $@" SELECT vh.* 
                                          FROM  VoucherHeader vh
                                          JOIN  VoucherDetail vd on vh.Id = vd.VoucherId
                                          WHERE vd.VoucherRefId in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))";
                }
                else
                {
                    queryGetVoucher = $@"SELECT vh.* 
                                          FROM  VoucherHeader vh
                                          JOIN  VoucherDetail vd on vh.Id = vd.VoucherId
                                          WHERE vd.VoucherRefId in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))";
                }
                return await Task.FromResult(_dapper.GetAll<FinanceVoucherDetail>(queryGetVoucher, new Dapper.DynamicParameters(new
                {
                    RequestNumbers = stringJoin
                }), commandType: CommandType.Text));
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Voucher Detail List by ID
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<FinanceVoucherDetailResponse>> GetVoucherDetailList(ParamGetVoucherList param)
        {
            try
            {
                string voucherHeader = $@"SELECT Category FROM VoucherHeader WHERE Id = @Id OR VoucherNumber = @VoucherNumber";
                string voucherCategory = await Task.FromResult(_dapper.Get<string>(voucherHeader,
                                         new Dapper.DynamicParameters(new
                                         {
                                             Id = param.VoucherId,
                                             VoucherNumber = param.VoucherNumber,
                                         }), commandType: CommandType.Text));

                string queryGetVoucher = voucherCategory switch
                {
                    "Reimbursement" => Finance.GetVoucherDetailReimbursementOrCashAdvance(),
                    "Cash Advance" => Finance.GetVoucherDetailReimbursementOrCashAdvance(),
                    "Cash Advance Travel" => Finance.GetVoucherDetailReimbursementOrCashAdvance(),
                    "Invoice Travel" => Finance.GetVoucherDetailInvoiceTravel(),
                    "Ger" => Finance.GetVoucherDetailGer(),
                    "Travel Settlement" => Finance.GetVoucherDetailTravelSettlement(),
                    "Travel" => Finance.GetVoucherDetail(),
                    "Trex Apr" => Finance.GetVoucherDetailTrexApr(),
                    "Trex Eer" => Finance.GetVoucherDetailTrexEer(),
                    "Trex Ger" => Finance.GetVoucherDetailTrexGer(),
                    "Trex Ter" => Finance.GetVoucherDetailTrexTer(),
                    "Shopping Cart" => Finance.GetVoucherDetailShoppingCart(),
                    "Non Shopping Cart" => Finance.GetVoucherDetailNonShoppingCart(),
                    _ => "",
                };

                if (param.IsExport)
                {
                    queryGetVoucher = $"{queryGetVoucher} ORDER BY {AppSystem.Id} ASC";
                }
                else
                {
                    queryGetVoucher = $"{queryGetVoucher} ORDER BY {param.SortColumn ?? "VoucherRefId"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
                }

                return await Task.FromResult(_dapper.GetAll<FinanceVoucherDetailResponse>(@$"{queryGetVoucher} ",
                new Dapper.DynamicParameters(new
                {
                    Id = param.VoucherId,
                    VoucherNumber = param.VoucherNumber,
                    Page = param.Page,
                    PageSize = param.PageSize == -1 ? 99999 : param.PageSize,
                }), commandType: CommandType.Text));
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Voucher Detail by ID
        /// </summary>
        /// <param name="voucherId"></param>
        /// <returns></returns>
        public async Task<FinanceVoucherDetail> GetVoucherDetail(int voucherId)
        {
            try
            {
                var queryGetVoucher = $@" SELECT  vd.* 
                                                 ,vh.Category
                                          FROM   VoucherHeader vh
                                          JOIN   VoucherDetail vd on vh.Id = vd.VoucherId
                                          WHERE  vd.VoucherId = @Id ";

                return await Task.FromResult(_dapper.Get<FinanceVoucherDetailResponse>(queryGetVoucher, new Dapper.DynamicParameters(new
                {
                    Id = voucherId
                }), commandType: CommandType.Text));
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Voucher Detail by Voucher Ref Id
        /// </summary>
        /// <param name="voucherRefId"></param>
        /// <returns></returns>
        public async Task<FinanceVoucherDetail> GetVoucherDetailByVoucherRefId(string voucherRefId)
        {
            try
            {
                var queryGetVoucher = $@" SELECT vd.* 
                                          FROM   VoucherHeader vh
                                          JOIN   VoucherDetail vd on vh.Id = vd.VoucherId
                                          WHERE  vd.VoucherRefId = @VoucherRefId ";

                return await Task.FromResult(_dapper.Get<FinanceVoucherDetail>(queryGetVoucher, new Dapper.DynamicParameters(new
                {
                    VoucherRefId = voucherRefId
                }), commandType: CommandType.Text));
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Travel Expense Detail by ID
        /// </summary>
        /// <param name="travelExpenseDetailId"></param>
        /// <returns></returns>
        public async Task<TravelRequestExpenseDetail> GetTravelExpenseDetail(int travelExpenseDetailId)
        {
            try
            {
                var queryGetVoucher = $@" SELECT * 
                                          FROM   TravelRequestExpenseDetail tred
                                          WHERE  tred.Id = @Id ";
                return await Task.FromResult(_dapper.Get<TravelRequestExpenseDetail>(queryGetVoucher, new Dapper.DynamicParameters(new
                {
                    Id = travelExpenseDetailId
                }), commandType: CommandType.Text));
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Update Expense Detail by ID
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<TravelRequestExpenseDetail> UpdateTravelExpenseDetail(TravelRequestExpenseDetail param)
        {
            try
            {
                var queryUpdateExpenseDetail = $@" UPDATE TravelRequestExpense
                                                   SET [GrandTotal] = @RequestedAmount
                                                      ,[LastUpdatedTime] = @LastUpdatedTime
                                                      ,[LastUpdatedBy] = @LastUpdatedBy 
                                                   WHERE Id = @TravelRequestExpenseId

                                                   UPDATE TravelRequestExpenseDetail
                                                   SET [TypeExpense_SubCategoryId] = @TypeExpense
                                                      ,[Description] = @Description
                                                      ,[DayKm] = @DayKm
                                                      ,[AmountType] = @AmountType
                                                      ,[Amount] = @Amount
                                                      ,[AttachmentId] = @AttachmentId
                                                      ,[ReasonUpdate] = @ReasonUpdate
                                                      ,[Status] = @Status
                                                      ,[Day] = @Day
                                                      ,[FullDay] = @FullDay
                                                      ,[LastUpdatedTime] = @LastUpdatedTime
                                                      ,[LastUpdatedBy] = @LastUpdatedBy
                                                    OUTPUT INSERTED.*
                                                    WHERE Id = @Id";
                var expenseDetail = await Task.FromResult(_dapper.Update<TravelRequestExpenseDetail>(queryUpdateExpenseDetail, new Dapper.DynamicParameters(new
                {
                    TravelRequestExpenseId = param.TravelRequestExpenseId,
                    TypeExpense = param.TypeExpense_SubCategoryId,
                    Description = param.Description,
                    DayKm = param.DayKm,
                    AmountType = param.AmountType,
                    Amount = param.Amount,
                    AttachmentId = param.AttachmentId,
                    ReasonUpdate = param.ReasonUpdate,
                    RequestedAmount = param.RequestedAmount, //Placeholder for GrandTotal
                    Status = param.Status,
                    LastUpdatedTime = DateTime.Now,
                    LastUpdatedBy = param.LastUpdatedBy,
                    Day = param.Day,
                    FullDay = param.Fullday,
                    Id = param.Id
                }), commandType: CommandType.Text));
                return expenseDetail;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Sum Amount Voucher
        /// </summary>
        /// <param name="voucherId"></param>
        /// <returns></returns>
        public async Task<decimal> GetSumAmountVoucher(int voucherId)
        {
            try
            {
                decimal totalOriginalAmount = 0;
                var queryGetVoucher = $@"
--declare @VoucherId int = 0

select
coalesce(sum(vd.TotalBaseAmmount),0) as TotalBaseAmmount
from VoucherDetail as vd
where vd.VoucherId = @VoucherId
";

                totalOriginalAmount = await Task.FromResult(_dapper.Get<decimal>(queryGetVoucher, new Dapper.DynamicParameters(new
                {
                    VoucherId = voucherId
                }), commandType: CommandType.Text));
                return totalOriginalAmount;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Submit Voucher Maker
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<FinanceVoucherHeader> SubmitVoucherMaker(FinanceVoucherRequest param)
        {
            try
            {
                string category = param.Category switch
                {
                    "RI" => "Reimbursement",
                    "CA" => "Cash Advance",
                    "CATR" => "Cash Advance Travel",
                    "TR" => "Travel",
                    "TRSTL" => "Travel Settlement",
                    "INVTR" => "Invoice Travel",
                    "TREXAPR" => "Trex Apr",
                    "TREXEER" => "Trex Eer",
                    "TREXGERRI" => "Trex Ger",
                    "TREXGERSTL" => "Trex Ger",
                    "TREXTER" => "Trex Ter",
                    "SC" => "Shopping Cart",
                    "NON" => "Non Shopping Cart",
                    _ => "",
                };

                if (ExpenseGER.Contains(param.Category))
                {
                    category = "Ger";
                }

                string stringJoin = string.Join(", ", param.RequestNumbers);
                string query = string.Empty;
                string qryGetLastVoucherNumber = await Task.FromResult(_dapper.Get<string>("SELECT TOP 1 ISNULL(vouchernumber, '0 0') from VoucherHeader order by Id desc", null, commandType: CommandType.Text));
                string lastVoucherNumber = String.IsNullOrEmpty(qryGetLastVoucherNumber) ? "0" : qryGetLastVoucherNumber.Split(' ')[0];

                query = param.Category switch
                {
                    var cat when cat.StartsWith("CA") => Finance.CreateVoucherRI(),
                    "RI" => Finance.CreateVoucherRI(),
                    "TRSTL" => Finance.CreateVoucherTRSTL(),
                    "INVTR" => Finance.CreateVoucherINVTR(),
                    "TREXAPR" => Finance.CreateVoucherTrexAPR(),
                    "TREXEER" => Finance.CreateVoucherTrexEER(),
                    var cat when cat.StartsWith("TREXGER") => Finance.CreateVoucherTrexGER(),
                    "TREXTER" => Finance.CreateVoucherTrexTER(),
                    "SC" => Finance.CreateVoucherSC(),
                    "NON" => Finance.CreateVoucherNON(),
                    _ => query,
                };

                if (ExpenseGER.Contains(param.Category))
                {
                    query = Finance.CreateVoucherGER();
                }

                if (param.Category == "SC" || param.Category == "NON")
                {
                    stringJoin = string.Join(", ", param.Id);
                }

                var voucherInserted = await Task.FromResult(_dapper.Insert<FinanceVoucherHeader>(query, new Dapper.DynamicParameters(new
                {
                    LastVoucherNumber = lastVoucherNumber,
                    Category = category,
                    IsEmail = param.IsEmail,
                    BankTransferCode = param.BankTransferCode.Trim(),
                    BankTransferName = param.BankTransferName.Trim(),
                    CheckerMCM = param.CheckerMCM,
                    CreatedBy = param.CreatedBy,
                    RequestNumbers = stringJoin
                }), commandType: CommandType.Text));

                await SendEmail(param, voucherInserted);
                return voucherInserted;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        private async Task SendEmail(FinanceVoucherRequest param, FinanceVoucherHeader voucherInserted)
        {
            //Setup email parameter(s)
            var paramFinance = new ParamEmailFinance();
            paramFinance.Amount = await GetSumAmountVoucher(voucherInserted.Id);
            paramFinance.Currency = voucherInserted.L_Currency;
            paramFinance.BankAccount = voucherInserted.BankTransferCode;
            paramFinance.VoucherNumber = voucherInserted.VoucherNumber;

            //Send email to Checker
            var accountDetail = await _externalService.GetAccountDetail(param.CheckerMCM);
            if (accountDetail.Id != null)
            {
                string statusRequest = StatusRequest.SetStatusRequest((Int16)(voucherInserted.Status));
                NotificationModel notification = new NotificationModel();
                // parameter send email notification
                notification.SubjectEmail = $"{statusRequest} Voucher";
                notification.RequestType = voucherInserted.Category;
                notification.RequestNumber = voucherInserted.VoucherNumber;
                notification.ParamEmailFinance = paramFinance;

                notification.RequestorName = accountDetail.Fullname;
                notification.RequestorEmail = accountDetail.Email;
                notification.StatusRequest = statusRequest;
                notification.Attachments = null;
                notification.ApprovalRequestGroupMember = null;

                var saveToLog = JsonConvert.SerializeObject(notification);
                log.LogInitialize(methodName: "Pending Voucher", saveToLog, LogType.Info);
                await _notificationService.SendEmailNotificationFinance(notification);
            }
        }

        /// <summary>
        /// Submit Voucher Checker
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<FinanceVoucherHeader> SubmitVoucherChecker(FinanceVoucherRequest param)
        {
            try
            {
                var queryGetVoucher = $@"--Update VoucherHeader 
                                         UPDATE VoucherHeader
                                         SET [Status] = 1
                                            ,LastUpdatedBy = @CheckerName
                                            ,LastUpdatedTime = GETDATE()
                                            ,ApproveDate = GETDATE()
                                         WHERE Id = @VoucherId

										 --Update VoucherDetail 
                                         UPDATE VoucherDetail
                                         SET [Status] = 1
                                            ,LastUpdatedBy = @CheckerName
                                            ,LastUpdatedTime = GETDATE()
                                         WHERE VoucherId = @VoucherId";

                var voucherSubmitted = await Task.FromResult(_dapper.Update<FinanceVoucherHeader>(queryGetVoucher, new Dapper.DynamicParameters(new
                {
                    CheckerName = param.CheckerName,
                    VoucherId = param.VoucherId
                }), commandType: CommandType.Text));

                return voucherSubmitted;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Switch Checker
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<FinanceVoucherHeader> SwitchChecker(FinanceVoucherRequest param)
        {
            try
            {
                var querySwitch = $@"UPDATE VoucherHeader
                                         SET [CheckerMCM] = @CheckerMcm
                                            ,LastUpdatedBy = @CreatedBy
                                            ,LastUpdatedTime = GETDATE()
                                         WHERE Id = @VoucherId
                                         ";

                var newChecker = await Task.FromResult(_dapper.Insert<FinanceVoucherHeader>(querySwitch, new Dapper.DynamicParameters(new
                {
                    CheckerMcm = param.CheckerMCM,
                    VoucherId = param.VoucherId,
                    CreatedBy = param.CreatedBy
                }), commandType: CommandType.Text));

                return newChecker;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Takeout Voucher Checker
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<FinanceVoucherHeader> TakeOutVoucherChecker(FinanceVoucherRequest param)
        {
            try
            {
                //Transaction 
                string stringJoin = string.Join(", ", param.RequestNumbers);
                string category = await GetCategoryByVoucherId(param.VoucherId);

                var queryTakeout = $@"
                                    DECLARE @DateUpdate datetime =  GETDATE()
                                    --Delete VoucherDetail 
                                    DELETE FROM VoucherDetail
                                    WHERE VoucherRefId in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')) 
                                   ";

                if (category == "Ger")
                {
                    queryTakeout = queryTakeout.Replace("WHERE VoucherRefId in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ','))", "WHERE VoucherId = @VoucherId");
                }
                queryTakeout += category switch
                {
                    var cat when cat.StartsWith("Cash Advance") => Finance.TakeoutVoucherCheckerRI(),
                    "Reimbursement" => Finance.TakeoutVoucherCheckerRI(),
                    "Travel Settlement" => Finance.TakeoutVoucherCheckerTRSTL(),
                    "Invoice Travel" => Finance.TakeoutVoucherCheckerINVTR(),
                    "Ger" => Finance.TakeoutVoucherCheckerGER(),
                    "Trex Apr" => Finance.TakeoutVoucherCheckerTrexAPR(),
                    "Trex Eer" => Finance.TakeoutVoucherCheckerTrexEER(),
                    "Trex Ger" => Finance.TakeoutVoucherCheckerTrexGER(),
                    "Trex Ter" => Finance.TakeoutVoucherCheckerTrexTER(),
                    "Shopping Cart" => Finance.TakeoutVoucherCheckerSC(),
                    "Non Shopping Cart" => Finance.TakeoutVoucherCheckerNonSC(),
                    _ => throw new InvalidOperationException($"Unknown category: {param.Category}")
                };

                var data = await Task.FromResult(_dapper.Insert<FinanceVoucherHeader>(queryTakeout, new Dapper.DynamicParameters(new
                {
                    RequestNumbers = stringJoin,
                    CheckerName = param.CheckerName,
                    VoucherId = param.VoucherId,
                    Reason = param.Reason
                }), commandType: CommandType.Text));

                return data;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Revert Voucher
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<FinanceVoucherHeader> RevertVoucher(ParamGetVoucherList param)
        {
            try
            {
                var queryRevert = $@"    DECLARE @VoucherId varchar(20) = (SELECT Id FROM VoucherHeader WHERE VoucherNumber = @VoucherNumber)

                                         --Update Voucher Header 
                                         UPDATE VoucherHeader
                                         SET ApproveDate = NULL,
                                             [Status] = 0
                                         WHERE VoucherNumber = @VoucherNumber
                                         
                                         --Update Voucher Detail 
                                         UPDATE VoucherDetail
                                         SET [Status] = 0
                                         WHERE VoucherId =  @VoucherId
                                         ";

                var data = await Task.FromResult(_dapper.Update<FinanceVoucherHeader>(queryRevert, new Dapper.DynamicParameters(new
                {
                    VoucherNumber = param.VoucherNumber
                }), commandType: CommandType.Text));

                return data;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Report Mcm Header
        /// </summary>
        /// <param name="voucherId"></param>
        /// <returns></returns>
        public async Task<List<ReportMcmHeader>> GetReportMcmHeader(int voucherId)
        {
            try
            {

                var queryReportMcmHeader = $@"  SELECT 'P' as [Header]
                                                	   ,format(cast(GETDATE() as date),'yyyyMMdd') as [CreatedTime]
                                                	   ,vh.BankTransferCode as [SourceFund]
                                                	   ,CAST(SUM(vd.TotalBaseAmmount) as decimal) [SumAmount]
		                                               ,vh.Category
                                                FROM VoucherDetail vd
                                                JOIN VoucherHeader vh ON vd.VoucherId = vh.Id
                                                WHERE vd.VoucherId = @VoucherId
                                                GROUP BY VoucherId, vh.BankTransferCode, vh.Category";

                var reportMcmHeader = await Task.FromResult(_dapper.GetAll<ReportMcmHeader>(queryReportMcmHeader, new Dapper.DynamicParameters(new
                {
                    VoucherId = voucherId
                }), commandType: CommandType.Text));

                return reportMcmHeader;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Report Mcm Detail
        /// UPDATE
        /// 18 Mar 23 -> Kolom AO ganti jadi transaction type
        /// 18 Mar 23 -> Kolom R  ganti jadi email si requestor
        /// 31 Jul 24 -> Tambah Buat Non Shop
        /// </summary>
        /// <param name="voucherId"></param>
        /// <returns></returns>
        public async Task<List<ReportMcmDetail>> GetReportMcmDetail(int voucherId)
        {
            try
            {
                var category = await GetCategoryByVoucherId(voucherId);
                var recipentBank = _externalService.GetRecipientBank().Result;
                string qry = category switch
                {
                    string c when c == "Reimbursement" => Finance.GetReportMcmDetailRI(),
                    string c when c.Contains("Cash Advance") => Finance.GetReportMcmDetailRI(),
                    string c when c == "Travel Settlement" => Finance.GetReportMcmDetailTRSTL(),
                    string c when c == "Invoice Travel" => Finance.GetReportMcmDetailINVTR(),
                    string c when c == "Ger" => Finance.GetReportMcmDetailGER(),
                    string c when c == "Trex Ter" => Finance.GetReportMcmDetailTrexTER(),
                    string c when c == "Trex Apr" => Finance.GetReportMcmDetailTrexAPR(),
                    string c when c == "Trex Eer" => Finance.GetReportMcmDetailTrexEER(),
                    string c when c == "Trex Ger" => Finance.GetReportMcmDetailTrexGER(),
                    string c when c == "Shopping Cart" => Finance.GetReportMcmDetailSC(),
                    string c when c == "Non Shopping Cart" => Finance.GetReportMcmDetailNON(),
                    _ => Finance.GetReportMcmDetailRI(),
                };
                qry += " ORDER BY vd.Id";

                var reportMcmDetail = await Task.FromResult(_dapper.GetAll<ReportMcmDetail>(qry, new Dapper.DynamicParameters(new
                {
                    VoucherId = voucherId
                }), commandType: CommandType.Text));

                foreach (var item in reportMcmDetail)
                {
                    item.BankName = item.BankName.Replace(',', '.');
                    var lookupName = recipentBank.Where(q => q.Code == item.BankCode).Select(q => q.Name).FirstOrDefault();
                    var lookupShortName = recipentBank.Where(q => q.Code == item.BankCode).Select(q => q.ShortName).FirstOrDefault();
                    var lookupBranch = recipentBank.Where(q => q.Code == item.BankCode).Select(q => q.Branch).FirstOrDefault();
                    var lookupCode = recipentBank.Where(q => q.ShortName.Contains(item.BankName)).Select(q => q.Code).FirstOrDefault();

                    item.BankShortName = string.IsNullOrEmpty(lookupShortName) ? item.BankShortName : lookupShortName;
                    item.BankBranch = string.IsNullOrEmpty(lookupBranch) ? item.BankBranch : lookupBranch;
                    item.BankName = string.IsNullOrEmpty(lookupName) ? item.BankName : lookupName.Replace(',', '.');
                    item.BankCode = string.IsNullOrEmpty(lookupCode) ? item.BankCode : lookupCode.Replace(',', '.');
                }

                return reportMcmDetail;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        private async Task<string> GetCategoryByVoucherId(int voucherId)
        {
            var queryGetCategoryByVoucherId = $@"SELECT Category FROM VoucherHeader WHERE Id = @VoucherId";
            return await Task.FromResult(_dapper.Get<string>(queryGetCategoryByVoucherId, new Dapper.DynamicParameters(new
            {
                VoucherId = voucherId
            }), commandType: CommandType.Text));
        }

        /// <summary>
        /// Get Report BSM
        /// </summary>
        /// <param name="voucherId"></param>
        /// <returns></returns>
        public async Task<List<ReportBsm>> GetReportBsm(int voucherId)
        {
            try
            {
                var queryReportBsm = $@" DECLARE @Category varchar(100) = (SELECT Category FROM VoucherHeader WHERE Id = @VoucherId)
                                         IF (@Category = 'Reimbursement' OR @Category LIKE '%Cash Advance%')
										 BEGIN
										      SELECT DISTINCT rd.BankAccountNumber [Norek]
                                                  	       ,vd.TotalBaseAmmount [JumlahSaldo]
                                                  	       ,STUFF((
																     SELECT ';' + LTRIM(RTRIM(InvoiceNo))
																     FROM ReimbursementDetail rdsub
																     WHERE rdsub.ReimbursementId = rd.ReimbursementId
																     FOR XML PATH('')
																     ), 1, 1, '') as [Keterangan]
                                                  	       ,vh.Email
                                                  	       ,rd.BankAccountOwnerName [BeneficiaryName]
                                              FROM VoucherDetail vd
                                              JOIN VoucherHeader vh on vh.Id = vd.VoucherId
                                              JOIN Reimbursement r on r.RequestNumber = vd.VoucherRefId
                                              JOIN ReimbursementDetail rd on rd.ReimbursementId = r.Id
                                              JOIN vendor vn on vn.Id = rd.VendorId
                                              WHERE vd.VoucherId = @VoucherId
										 END
                                         ELSE IF (@Category = 'Travel Settlement')
										 BEGIN
										      SELECT DISTINCT r.BankAccountNumber [Norek]
                                                     	       ,vd.TotalBaseAmmount [JumlahSaldo]
                                                     	       ,r.RequestNumber as [Keterangan]
                                                     	       ,vh.Email
                                                     	       ,r.BankAccountOwnerName [BeneficiaryName]
                                              FROM VoucherDetail vd
                                              JOIN VoucherHeader vh on vh.Id = vd.VoucherId
                                              JOIN TravelRequestExpense r on r.RequestNumber = vd.VoucherRefId
                                              JOIN TravelRequestExpenseDetail rd on rd.TravelRequestExpenseId = r.Id
                                              WHERE vd.VoucherId = @VoucherId
										 END
                                         ELSE IF (@Category = 'Ger')
                                         BEGIN
                                              SELECT  ger.AccountNumber [Norek]
                                                       	  ,vd.TotalOriginalAmmount [JumlahSaldo]
                                                       	  ,ger.Description [Keterangan]
                                                       	  ,vh.Email
                                                       	  ,ger.BankAccountOwnerName [BeneficiaryName]
                                              FROM VoucherDetail vd
                                              JOIN VoucherHeader vh on vh.Id = vd.VoucherId
                                              JOIN ( SELECT  gh.RequestNumber
                                              			  ,gd.Id 
                                              			  ,gd.BankAccountOwnerName
                                              			  ,gd.AccountNumber
                                              			  ,gd.Description
                                              	   FROM GerHeader gh
                                              	   JOIN GerDetail gd on gh.Id = gd.GerHeaderId
                                              ) ger on CONCAT(ger.RequestNumber , ' - ', ger.Id) = vd.VoucherRefId 
                                              WHERE vd.VoucherId = @VoucherId
                                         END
                                         ELSE IF (@Category = 'Trex Apr')
                                         BEGIN
                                              SELECT  r.BankAccountNumber [Norek]
                                                     ,vd.TotalOriginalAmmount [JumlahSaldo]
                                                     ,r.Description [Keterangan]
                                                     ,vh.Email
                                                     ,r.BankAccountName [BeneficiaryName]
                                              FROM VoucherDetail vd
                                              JOIN VoucherHeader vh on vh.Id = vd.VoucherId
                                              JOIN TrexAPR r on r.NoAPR = vd.VoucherRefId
                                              WHERE vd.VoucherId = @VoucherId
                                         END
                                         ELSE IF (@Category = 'Trex Eer')
                                         BEGIN
                                              SELECT  r.BankAccountNumber [Norek]
                                                     ,vd.TotalOriginalAmmount [JumlahSaldo]
                                                     ,r.Description [Keterangan]
                                                     ,vh.Email
                                                     ,r.BankAccountName [BeneficiaryName]
                                              FROM VoucherDetail vd
                                              JOIN VoucherHeader vh on vh.Id = vd.VoucherId
                                              JOIN TrexEERHeader r on r.NoEER = vd.VoucherRefId
                                              WHERE vd.VoucherId = @VoucherId
                                         END
                                         ELSE IF (@Category = 'Trex Ger')
                                         BEGIN
                                              SELECT  r.BankAccountNumber [Norek]
                                                     ,vd.TotalOriginalAmmount [JumlahSaldo]
                                                     ,r.Description [Keterangan]
                                                     ,vh.Email
                                                     ,r.BankAccountName [BeneficiaryName]
                                              FROM VoucherDetail vd
                                              JOIN VoucherHeader vh on vh.Id = vd.VoucherId
                                              JOIN TrexGERHeader r on r.NoGER = vd.VoucherRefId
                                              WHERE vd.VoucherId = @VoucherId
                                         END
                                         ELSE IF (@Category = 'Trex Ter')
                                         BEGIN
                                              SELECT  r.BankAccountNumber [Norek]
                                                     ,vd.TotalOriginalAmmount [JumlahSaldo]
                                                     ,CONCAT(btr.NoBTR, ' - ' , btr.TravelDestination) [Keterangan]
                                                     ,vh.Email
                                                     ,r.BankAccountName [BeneficiaryName]
                                              FROM VoucherDetail vd
                                              JOIN VoucherHeader vh on vh.Id = vd.VoucherId
                                              JOIN TrexTERHeader r on r.NoTER = vd.VoucherRefId
                                              JOIN TrexBTRHeader btr on btr.BTRId = r.BTRId
                                              WHERE vd.VoucherId = @VoucherId
                                         END
										 ELSE IF (@Category = 'Shopping Cart')
                                         BEGIN
                                               SELECT  ipo.BankAccountNumber [Norek]
                                                  	  ,vd.TotalOriginalAmmount [JumlahSaldo]
                                                  	  ,ipo.InvoiceNumber [Keterangan]
                                                  	  ,vh.Email
                                                  	  ,ipo.BankAccountOwnerName [BeneficiaryName]
                                              FROM VoucherDetail vd
                                              JOIN VoucherHeader vh on vh.Id = vd.VoucherId
										      JOIN InvoicePO ipo on CONCAT(ipo.id , ' - ', ipo.InvoiceNumber) = vd.VoucherRefId
                                              WHERE vd.VoucherId = @VoucherId AND IPO.CategoryProcess_SubCategoryId = (SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01261')
                                         END
                                         ELSE IF (@Category = 'Non Shopping Cart')
                                         BEGIN
                                               SELECT  ipo.BankAccountNumber [Norek]
                                                  	  ,vd.TotalOriginalAmmount [JumlahSaldo]
                                                  	  ,ipo.InvoiceNumber [Keterangan]
                                                  	  ,vh.Email
                                                  	  ,ipo.BankAccountOwnerName [BeneficiaryName]
                                              FROM VoucherDetail vd
                                              JOIN VoucherHeader vh on vh.Id = vd.VoucherId
										      JOIN InvoicePO ipo on CONCAT(ipo.id , ' - ', ipo.InvoiceNumber) = vd.VoucherRefId
                                              WHERE vd.VoucherId = @VoucherId AND IPO.CategoryProcess_SubCategoryId = (SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01262')
                                         END  ";

                return await Task.FromResult(_dapper.GetAll<ReportBsm>(queryReportBsm, new Dapper.DynamicParameters(new
                {
                    VoucherId = voucherId
                }), commandType: CommandType.Text));
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        /// <summary>
        /// Get Report Csv Voucher
        /// </summary>
        /// <param name="voucherId"></param>
        /// <returns></returns>
        public async Task<List<ReportCsvVoucher>> GetReportCsvVoucher(int voucherId)
        {
            try
            {
                var queryReportCsvVoucher = $@"DECLARE @Category varchar(100) = (SELECT Category FROM VoucherHeader WHERE Id = @VoucherId)
                                               IF (@Category = 'Reimbursement' OR @Category = 'Cash Advance')
										          BEGIN
										             SELECT  rd.BankAccountNumber [BankAccountNumber]
                                                     FROM VoucherDetail vd
                                                     JOIN VoucherHeader vh on vh.Id = vd.VoucherId
                                                     JOIN Reimbursement r on r.RequestNumber = vd.VoucherRefId
                                                     JOIN ReimbursementDetail rd on rd.ReimbursementId = r.Id
                                                     JOIN vendor vn on vn.Id = rd.VendorId
                                                     WHERE vd.VoucherId = @VoucherId
										          END
                                               ELSE IF (@Category = 'Ger')
                                                  BEGIN
                                                     SELECT  rd.AccountNumber [BankAccountNumber]
                                                     FROM VoucherDetail vd
                                                     JOIN VoucherHeader vh on vh.Id = vd.VoucherId
                                                     JOIN (
															SELECT gd.Id
																  ,gh.RequestNumber
																  ,gd.BankName
																  ,gd.PolicyNumber
																  ,gd.NettAmount
																  ,gd.AccountNumber
																  ,gd.BankAccountOwnerName
															FROM GerHeader gh
															JOIN GerDetail gd on gh.Id = gd.GerHeaderId
														 ) rd on CONCAT(rd.RequestNumber, ' - ', rd.Id) = vd.VoucherRefId
                                                     WHERE vd.VoucherId = @VoucherId
                                                  END
										        ELSE IF (@Category = 'Shopping Cart')
                                                  BEGIN
                                                     SELECT  ipo.BankAccountNumber [BankAccountNumber]
                                                     FROM VoucherDetail vd
                                                     JOIN VoucherHeader vh on vh.Id = vd.VoucherId
                                                     JOIN InvoicePO ipo on CONCAT(ipo.id , ' - ', ipo.InvoiceNumber) = vd.VoucherRefId
                                                     WHERE vd.VoucherId = @VoucherId
                                                  END";

                return await Task.FromResult(_dapper.GetAll<ReportCsvVoucher>(queryReportCsvVoucher, new Dapper.DynamicParameters(new
                {
                    VoucherId = voucherId
                }), commandType: CommandType.Text));
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        #region GENERERATE INVOICE SHOPPINGCART & NON SHOP
        /// <summary>
        /// GetStatusByPoNumber
        /// </summary>
        /// <param name="PoNumber"></param>
        /// <returns></returns>
        public async Task<ShoppingCartStatusDetail> GetStatusByPoNumber(string PoNumber)
        {

            try
            {
                string query = $@"select
                                        (
                                            select 
		                                    concat('Purchase Request Details : ', pr.RequestCode) HeaderPurchaeseRequest,
		                                    CONVERT(VARCHAR(20), pr.RequestDates, 106) SubHeaderPr,
		                                    concat('Purchaese Order Details : ', po.PONumber) HeaderPurchaeseOrder,
		                                    concat(CONVERT(VARCHAR(20), po.ApproverDate, 106), ' ', '('+mt.Name+')') SubHeaderPo,
		                                    concat('Delivery Note Details : ', dn.DeliveryNumber) HeaderDeliveryNote,
		                                    concat(CONVERT(VARCHAR(20), dn.CreatedTime, 106), ' ', '('+(select Name from MasterTable where Category = 'DeliveryNote.Status' and ValueId = dn.Status)+')') SubHeaderDn,
		                                    concat('Invoice Details : ', (select top 1 InvoiceNumber from invoicePO where PurchaeseOrderId = po.Id)) HeaderInvoicePo,
		                                    concat(CONVERT(VARCHAR(20), (select top 1 InvoiceDate from invoicePO where PurchaeseOrderId = po.Id), 106), ' ', '('+(select Name from MasterTable where Category = 'InvoiceManagement.Status' and ValueId = (select top 1 Status from invoicePO where PurchaeseOrderId = po.Id))+')') SubHeaderInv,
		                                    JSON_QUERY(
		                                    (
                                                       select 'AMFS' BusinessUnitName,
                                                              cc.Name CostCenterName,
                                                              argm.UserName ApprovalName,
						                                      argm.Level LevelApproval,
                                                              mtb.Name ApprovalStatus,
                                                              CONVERT(VARCHAR(20), argm.ApprovalDate, 106) ApprovalDate
                                                           from PurchaseRequestItemDetail prd
                                                           join Item itm
                                                               on prd.ItemId = itm.Id
                                                           join ApprovalRequest ar
                                                               on prd.ApprovalRequestId = ar.Id
                                                           join ApprovalRequestGroupMember argm
                                                               on ar.Id = argm.ApprovalRequestId
                                                           join MasterTable mtb
                                                               on argm.Status = mtb.ValueId
                                                           join CostCenter cc
                                                               on argm.CostCenterId = cc.Id
                                                       where prd.PurchaseRequestId = pr.Id
                                                             and mtb.Category = 'ApprovalRequestGroupMember.Status'
                                                       for json path, include_null_values
                                                   )) PurchaeseRequestStatus,
                                                   JSON_QUERY(
                                                   (
                                                       select 'AMFS' BusinessUnitName,
                                                              '' CostCenterName,
                                                              uac.UserName ApprovalName,
                                                              '' ApprovalStatus,
                                                              CONVERT(VARCHAR(20), GETDATE(), 106) ApprovalDate
                                                       from PurchaseOrderDetail pod
                                                       where pod.PurchaseOrderId = po.Id
                                                       for json path, include_null_values
                                                   )) PurchaeseOrderStatus,
                                                   JSON_QUERY(
                                                   (
                                                       select itm.Name ItemName,
                                                              pod.Qty PoQty,
                                                              dnd.QtyReceive QtyReceive,
                                                              CONVERT(VARCHAR(20), dnd.DueDate, 106) DeliveryDate,
                                                              dnd.Notes Comment, mtb.Name DeliveryNoteStatus
                                                       from DeliveryNotesDetail dnd
                                                           join PurchaseOrderDetail pod
                                                               on dnd.PurchaseOrderDetailId = pod.Id
                                                           join Item itm
                                                               on pod.ItemId = itm.Id
                                                           join MasterTable mtb
                                                               on dnd.Status = mtb.ValueId
                                                       where mtb.Category = 'DeliveryNote.Status'
                                                             and dnd.DeliveryNotesPaymentId =
                                                             (
                                                                 select top 1 Id from DeliveryNotesPayment where DeliveryNotesId = dn.Id
                                                             )
                                                       for json path, include_null_values
                                                   )
                                                             ) DeliveryNoteStatus,
                                                   JSON_QUERY(
                                                   (
                                                       select po.PONumber PoNumber,
                                                              dn.DeliveryNumber DeliveryNumber,
                                                              CONVERT(VARCHAR(20), inv.CreateTime, 106) InvoiceDate,
                                                              FORMAT(inv.InvoiceAmmount, 'N', 'id-ID') InvoiceAmmount,
                                                              inv.InvoiceNumber InvoiceNumber,
                                                              '' BankName,
                                                              '' BankAccount,
                                                              '' BankOwner,
                                                              inv.Remark Remarks
                                                       from InvoicePO inv
                                                       where inv.PurchaeseOrderId = po.Id
                                                       for json path, include_null_values
                                                   )) InvoiceStatus
                                            from PurchaseOrder po
                                                join PurchaseOrderToPurchaseRequest prtopo
                                                    on po.Id = prtopo.PurchaseOrderId
			                                    join PurchaseRequest pr
				                                    on prtopo.PurchaseRequestlId = pr.Id
                                                join DeliveryNotes dn
                                                    on po.Id = dn.PurchaseOrderId
			                                    join MasterTable mt
				                                    on po.Status = mt.ValueId
			                                    join Flips.UserAccount uac
				                                    on po.ApproverAccountId = uac.Id
                                            where po.PONumber = @PoNumber and mt.Category in('PurchaseOrder.Status')
                                            FOR JSON PATH, INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER
                                        ) PopulateStatus";
                var resData = await Task.FromResult(_dapper.Get<JsonPopulateShoppingCart>(query, new Dapper.DynamicParameters(new
                {
                    PoNumber = PoNumber
                }), commandType: CommandType.Text));

                var response = JsonConvert.DeserializeObject<ShoppingCartStatusDetail>(resData.PopulateStatus);
                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// GenerateInvoice
        /// </summary>
        /// <param name="PoNumber"></param>
        /// <returns></returns>
        public async Task<GenerateInvoice> GenerateInvoices(string PoNumber, int InvoiceId, string Category)
        {
            try
            {
                var resData = await _invoiceManagementRepository.GenerateInvoices(PoNumber, InvoiceId, Category);
                var Serialize = JsonConvert.SerializeObject(resData);
                var response = JsonConvert.DeserializeObject<GenerateInvoice>(Serialize);
                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// InvoiceAttachment
        /// </summary>
        /// <param name="RequestDetailId"></param>
        /// <param name="Category"></param>
        /// <returns></returns>
        public async Task<List<AttachmentResponse>> InvoiceAttachment(int RequestDetailId, string Category)
        {

            try
            {
                var resData = await _invoiceManagementRepository.GenerateInvoiceAttachment(RequestDetailId, Category);
                var Serialize = JsonConvert.SerializeObject(resData);
                var response = JsonConvert.DeserializeObject<List<AttachmentResponse>>(Serialize);
                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// GenerateInvoiceSummary
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<InvoiceSummaryResponse> InvoiceSummary(InvoiceSummary param)
        {
            try
            {
                var resData = await _invoiceManagementRepository.InvoiceSummary(param);
                var Serialize = JsonConvert.SerializeObject(resData);
                var response = JsonConvert.DeserializeObject<InvoiceSummaryResponse>(Serialize);
                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        public async Task<ResponseData> GenerateInvoiceSaves(GenerateInvoiceSave param)
        {
            try
            {
                var resData = await _invoiceManagementRepository.GenerateInvoiceSaves(param);
                var Serialize = JsonConvert.SerializeObject(resData);
                var response = JsonConvert.DeserializeObject<ResponseData>(Serialize);
                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        public async Task<APS_Common.Models.Controller.Finance.GenerateInvoice.Submit.Response.Root> Finance_GenerateInvoice_Submit(APS_Common.Models.Controller.Finance.GenerateInvoice.Submit.Request.Root request)
        {
            var logArgs = new string[] { request.TraceId, GetType().Name, MethodBase.GetCurrentMethod().Name };
            try
            {
                var response = await _invoiceManagementRepository.Finance_GenerateInvoice_Submit(request);
                return response;
            }
            catch (Exception e)
            {
                if (e.InnerException is null)
                {
                    APS_Common.BaseLogging.LogError([.. logArgs, $"{nameof(e.Message)}{Environment.NewLine}{e.Message}"]);
                    APS_Common.BaseLogging.LogError([.. logArgs, $"{nameof(e.StackTrace)}{Environment.NewLine}{e.StackTrace}"]);
                }
                throw new GlobalExceptions(e.GetBaseException().Message);
            }
        }

        /// <summary>
        /// Repair To Invoice Management
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<ResponseData> RepairToInvoiceManagement(InvoiceRepair param)
        {
            try
            {
                var response = await _invoiceManagementRepository.RepairToInvoiceManagement(param);
                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Repair To Delivery Note
        /// </summary>
        /// <param name="param"></param  
        /// <returns></returns>
        public async Task<ResponseData> RepairToDeliveryNote(InvoiceRepair param)
        {
            try
            {
                var response = await _invoiceManagementRepository.RepairToDeliveryNote(param);
                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        #endregion

        #region TREX
        /// <summary>
        /// Get TREX Detail
        /// </summary>
        /// <param name="requestNumber"></param  
        /// <returns></returns>
        public async Task<FinanceTrexDetailResponse> GetTrexDetail(string requestNumber)
        {
            try
            {
                var recipentBank = _externalService.GetRecipientBank().Result;

                string qry = requestNumber switch
                {
                    string c when c.Contains("APR") => Finance.GetTrexAprDetail(),
                    string c when c.Contains("EER") => Finance.GetTrexEerDetail(),
                    string c when c.Contains("GER") => Finance.GetTrexGerDetail(),
                    string c when c.Contains("TER") => Finance.GetTrexTerDetail(),
                    _ => "",
                };

                var trexDetail = await Task.FromResult(_dapper.Get<FinanceTrexDetailResponse>(qry, new Dapper.DynamicParameters(new
                {
                    RequestNumber = requestNumber,
                }), commandType: CommandType.Text));

                trexDetail.BankName = trexDetail.BankName?.Replace(',', '.');

                if (trexDetail.BankName?.Contains("mandiri", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    var mandiriBank = recipentBank
                        .FirstOrDefault(q => q.Name == AppSystem.DefaultNameBankMandiri);

                    trexDetail.BankCode = mandiriBank?.Code ?? trexDetail.BankCode;
                }
                else
                {
                    var matchedBank = recipentBank
                        .FirstOrDefault(q =>
                            q.Name?.Contains(trexDetail.BankName ?? string.Empty) == true ||
                            q.ShortName?.Contains(trexDetail.BankName ?? string.Empty) == true);

                    if (matchedBank != null)
                    {
                        trexDetail.BankName = matchedBank.Name?.Replace(',', '.') ?? trexDetail.BankName;
                        trexDetail.BankCode = matchedBank.Code?.Replace(',', '.') ?? trexDetail.BankCode;
                    }
                }

                return trexDetail;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Get TREX Detail List
        /// </summary>
        /// <param name="requestNumber"></param  
        /// <returns></returns>
        public async Task<List<FinanceTrexDetailListResponse>> GetTrexDetailList(ParamGetRequestList param)
        {
            try
            {
                string qry = param.RequestNumber switch
                {
                    string c when c.Contains("APR") => Finance.GetTrexAprDetailList(),
                    string c when c.Contains("EER") => Finance.GetTrexEerDetailList(),
                    string c when c.Contains("GER") => Finance.GetTrexGerDetailList(),
                    string c when c.Contains("TER") => Finance.GetTrexTerDetailList(),
                    _ => "",
                };

                if (param.IsExport)
                {
                    qry = $"{qry} ORDER BY CreatedTime DESC";
                }
                else
                {
                    qry = $"{qry} ORDER BY {param.SortColumn ?? AppSystem.CreatedTime} {param.SortDirection ?? AppSystem.Desc} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
                }
                return await Task.FromResult(_dapper.GetAll<FinanceTrexDetailListResponse>(qry, new Dapper.DynamicParameters(new
                {
                    RequestNumber = param.RequestNumber,
                    param.IsExport,
                    param.Page,
                    param.PageSize,
                    SortColumn = param.SortColumn ?? AppSystem.CreatedTime,
                    SortDirection = param.SortDirection ?? AppSystem.Desc
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        /// <summary>
        /// Get ApprovalList TREX
        /// </summary>
        /// <param name="param"></param  
        /// <returns></returns>
        public async Task<List<ResponseApprovalRequestGroupMember>> GetTrexApprovalList(ParamGetRequestDetail param)
        {
            try
            {
                string qry = param.RequestNumber switch
                {
                    string c when c.Contains("APR") => Finance.GetTrexAprApprovalList(),
                    string c when c.Contains("EER") => Finance.GetTrexEerApprovalList(),
                    string c when c.Contains("GER") => Finance.GetTrexGerApprovalList(),
                    string c when c.Contains("TER") => Finance.GetTrexTerApprovalList(),
                    _ => "",
                };

                return await Task.FromResult(_dapper.GetAll<ResponseApprovalRequestGroupMember>(qry, new Dapper.DynamicParameters(new
                {
                    RequestNumber = param.RequestNumber,
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        #endregion
    }
}



