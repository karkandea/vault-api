using APS_Common;
using APS_Common.EncryptionData;
using APS_Common.Helper;
using APS_Common.Models.Master;
using APS_Common.Models.NonShoppingCart;
using APS_Common.Models.NonShoppingCart.PAP;
using APS_Common.Models.NonShoppingCart.ProcurmentBuyer;
using APS_Entities.Models;
using APS_REST_API.Contracts;
using APS_REST_API.Contracts.NonShoppingCart;
using APS_REST_API.Contracts.v1;
using APS_REST_API.Models;
using APS_REST_API.Models.BudgetTransaction;
using APS_REST_API.Models.NonShoppingCart;
using APS_REST_API.Models.NonShoppingCart.ProcurmentBuyer.PAP;
using APS_REST_API.Models.NonShoppingCart.ProcurmentBuyer.PRFVendorEnhanced;
using APS_REST_API.Models.ResponseData;
using APS_REST_API.Payloads.Response.BudgetTransaction;
using APS_REST_API.Queries.NonShoppingCart;
using APS_REST_API.Validations;
using APS_SharedServices.Models;
using APS_SharedServices.Models.RequestModels;
using APS_SharedServices.Models.ResponseModels;
using APS_SharedServices.Services.Contracts;
using APS_SharedServices.Services.Implementations;
using Dapper;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static APS_Common.Enums.MasterTableEnum;
using static APS_Common.Enums.ProcurmentBuyerEnum;
using static APS_REST_API.Models.MyRequestV2Model;
using static APS_REST_API.Models.PrfSummaryDetailModel;
using static APS_REST_API.Models.ProcurmentBuyerModel;
using static APS_REST_API.Models.SpendingCategoryModel;

namespace APS_REST_API.Repository
{
    public class ProcurmentBuyerRepository : IProcurmentBuyerRepository
    {
        private readonly IDapper _iDapper;
        private readonly IAttachmentService _attachmentService;
        private readonly IPrfVendorQuotationRepository _prfVendorQuotatiorRepository;
        private readonly IBudgetTransactionRepository _budgetTransactionRepository;
        private readonly IApprovalMatrixRepository _approvalMatrixRepository;
        private readonly INonShopNotificationRepository _nonShopNotificationRepository;
        private readonly INotificationService _notificationService;
        private readonly IEncryptionData _encryption;
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly IExternalService _externalService;
        private readonly GlobalExceptions _globalExceptions = new GlobalExceptions();
        private readonly string objectName = nameof(ProcurmentBuyerRepository);
        private readonly string _baseUrlBoxerInternal;
        private readonly string _baseUrlBoxerExternal;

        public ProcurmentBuyerRepository(
            IDapper dapper
            , IAttachmentService attachmentService
            , IPrfVendorQuotationRepository prfVendorQuotatiorRepository
            , IConfiguration configuration
            , IBudgetTransactionRepository budgetTransactionRepository
            , IApprovalMatrixRepository approvalMatrixRepository
            , INonShopNotificationRepository nonShopNotificationRepository
            , IEncryptionData encryption
            , INotificationService notificationService
            , ISubCategoryRepository subCategoryRepository
            , IExternalService externalService
            )
        {
            _iDapper = dapper;
            _attachmentService = attachmentService;
            _encryption = encryption;
            _prfVendorQuotatiorRepository = prfVendorQuotatiorRepository;
            _baseUrlBoxerInternal = $"{configuration.GetValue<string>("WebAppAPS")}";
            _baseUrlBoxerExternal = $"{configuration.GetValue<string>("BoxerWebAppAPS")}";
            _budgetTransactionRepository = budgetTransactionRepository;
            _approvalMatrixRepository = approvalMatrixRepository;
            _nonShopNotificationRepository = nonShopNotificationRepository;
            _notificationService = notificationService;
            _subCategoryRepository = subCategoryRepository;
            _externalService = externalService;
        }

        public async Task<DtResult<SpendingCategoryListView>> ProcurementBuyerIndexDtResult(ProcurementBuyerIndexDtParameters dtParameters)
        {
            string query = string.Empty;
            string queryCondition = string.Empty;
            DtResult<SpendingCategoryListView> dtResult = new() { Draw = dtParameters.Draw };
            dtResult.SetNoData();
            try
            {
                int level = await AccessLevelPurchaseRequest((int)dtParameters.AccountId);

                switch (level)
                {
                    case (int)FiturPruchaseRequest.ViewAll:
                        queryCondition = $@"";
                        break;
                    case (int)FiturPruchaseRequest.VendorManagement:
                        queryCondition = $@"
							AND (PRF.IsRiskAssementForm = 1 OR PRF.DataActivity = 1 OR PRF.ITSecurityActivity = 1)
							AND PS.Id IS NOT NULL
						";
                        break;
                    case (int)FiturPruchaseRequest.Procurment:
                        queryCondition = $@"
						    AND (latest_psc.AccountId = @AccountId
						    OR PRF.RequestorAccountId = @AccountId)
						";
                        break;
                    default:
                        queryCondition = $@"
							AND PRF.RequestorAccountId = @AccountId
							AND (PRF.IsRiskAssementForm = 1 OR PRF.DataActivity = 1 OR PRF.ITSecurityActivity = 1)
							AND PS.Id IS NOT NULL
						";
                        break;
                }

                query = $@"
							SELECT 
								1 AS x,
								PRF.Id AS PRFId,
								PRF.PRFNo AS PRFNumber,
								PRF.RequestorUserName AS RequestorUserName,
								PRF.RequestorEmail AS RequestorEmail,
								PRF.BusinesUnitId AS BusinesUnitId,
								BU.Name AS BusinesUnitName,
								PRF.CostCenterId AS CostCenterId,
								CC.Name AS CostCenterName,
								FORMAT(PRF.RequestDate, 'yyyy-MM-dd HH:mm:ss') AS RequestDate,
								PRF.GLaccountCode AS GLaccountCode,
								PRF.BudgetCode AS BudgetCode,
								PRF.IsBudgetedSpend AS IsBudgetedSpend,
								PRF.TotalBudgetEstimation AS TotalBudgetEstimation,
								MT.Name AS Status,
								MT.Code AS StatusCode,
								MT.Name AS StatusName,
								PRF.Status AS StatusId,
								PRF.SLARequired AS SLARequired,
								PRF.ReuqestTitle AS ReuqestTitle,
								PRF.SLANotes AS SLANotes,
								PRF.AditionalRequestRequirement AS AditionalRequestRequirement,
								PRF.PenaltyBySuplier AS PenaltyBySuplier,
								PRF.PenaltyBySuplierNotes AS PenaltyBySuplierNotes,
								PRF.MtPrfProcessType_SubCategory AS MtPrfProcessType_SubCategory,
								PRF.BuyerAccountId AS BuyerAccountId,
								PRF.BuyerUserName AS BuyerUserName,
								PRF.BuyerEmail AS BuyerEmail,
								FORMAT(PRF.CreatedTime, 'dd MMM , yyyy', 'en-US') AS CreatedTime,
								PRF.CreatedBy AS CreatedBy,
								PRF.LastUpdatedBy AS LastUpdatedBy,
								FORMAT(PRF.LastUpdatedTime, 'dd MMM, yyyy', 'en-US') AS LastUpdatedTime,
								PRF.IsPurchasedPreviously AS IsPurchasedPreviously,
								PRF.DetailPreviously AS DetailPreviously,
								PRF.RepurchaseNotes AS RepurchaseNotes,
								PRF.TypeOfRequest AS TypeOfRequest,
								PRF.Spending_Category AS SpendingCategory,
								PRF.Spending_SubCategory AS SpendingSubCategory,
								PRF.TypeOfTransaction AS TypeOfTransaction,
								PRF.ProjectCode AS ProjectCode,
								PRF.TypeProcess_SubCategory AS TypeProcess_SubCategory,
								(
									SELECT TOP (1)
										SC.SubCategoryName
									FROM SubCategory AS SC
									WHERE SC.Id = PRF.TypeProcess_SubCategory
								) AS TypeProcessName,
								(
									SELECT TOP (1)
										SC.SubCategoryCode
									FROM SubCategory AS SC
									WHERE SC.Id = PRF.TypeProcess_SubCategory
								) AS typeProcess_SubCategoryCode,
								PRF.ContractCriteria_SubCategory AS ContractCriteria_SubCategory,
								FORMAT(PRF.TotalBudgetEstimation, 'N', 'id-ID') AS TotalBudgetEstimationString,
								PSC.Id AS SpendingCategoryId,
								UA.UserName AS ReciverBuyer,
								PS.Id AS PRFSummaryId,
								PS.Status AS PRFSummaryStatus,
								COALESCE(PS.ShortDescription, '-') AS SummaryStatus,
								COALESCE(PSp.ShortDescription, '-') AS PapStatus,
								COALESCE(PRF.ReasonCancel, '') AS ReasonCancel,
								COALESCE(PRF.ReasonRevise, '') AS ReasonRevise,
								COUNT(*) OVER () AS CountData,
								@AccountId AS ParameterAccountId,
								PSC.AccountId AS PRFSpendingCategoryAccountId,
								SPGC.Category AS SpendingCategoryName,
								SPSC.Spending_SubCategory AS SpendingSubCategoryName
							FROM PRF AS PRF
							INNER JOIN (
								SELECT 
									PSC.PRFId,
									PSC.AccountId,
									MAX(PSC.Id) AS PRFSpendingCategoryLatestId
								FROM PRFSpendingCategory PSC
								WHERE PSC.Status = 1
								GROUP BY PSC.PRFId, PSC.AccountId
							) AS latest_psc
							ON latest_psc.PRFId = PRF.Id
							INNER JOIN PRFSpendingCategory AS PSC
							ON PSC.Id = latest_psc.PRFSpendingCategoryLatestId
							INNER JOIN CostCenter AS CC
							ON CC.Id = PRF.CostCenterId
							INNER JOIN BusinessUnit AS BU
							ON BU.Id = PRF.BusinesUnitId
							LEFT JOIN Flips.UserAccount AS UA
							ON UA.Id = PRF.BuyerAccountId
							OUTER APPLY (
								SELECT TOP 1
									MT.ShortDescription,
									PS.*
								FROM PRFSummary PS
								LEFT JOIN MasterTable AS MT
								ON MT.ValueId = PS.Status
								AND MT.Category = 'PRFSummary.Status'
								WHERE PS.PRFId = PRF.Id
								ORDER BY PS.Id DESC
							) PS
							LEFT JOIN MasterTable AS MT
							ON MT.ValueId = PRF.Status AND MT.Category = 'PRF.Status'
							INNER JOIN Spending_Category AS SPGC
							ON PRF.Spending_Category = SPGC.Id
							INNER JOIN Spending_SubCategory AS SPSC
							ON PRF.Spending_SubCategory = SPSC.Id
							LEFT JOIN PAP PAP
								ON PRF.Id = PAP.PRFId
							OUTER APPLY
								(
									SELECT TOP 1
										MT.ShortDescription,
										PSP.*
									FROM PAP PSP
										LEFT JOIN MasterTable AS MT
											ON MT.ValueId = PSP.Status
											   AND MT.Category = 'PRFSummary.Status'
									WHERE PSP.PRFId = PRF.Id
									ORDER BY PS.Id DESC
								) PSP
							WHERE PRF.Status >= 2
							{queryCondition}

                            {(string.IsNullOrEmpty(dtParameters.TypeProccess) ? "--" : "")}and PRF.TypeOfRequest = @TypeProccess
                            {(string.IsNullOrEmpty(dtParameters.StatusId) ? "--" : "")} and (PS.Status = @StatusId or PAP.Status = @StatusId)
                            {(string.IsNullOrEmpty(dtParameters.CostCenterId) ? "--" : "")}and PRF.CostCenterId = @CostCenterId
                            {(string.IsNullOrEmpty(dtParameters.BusinessUnitId) ? "--" : "")}and PRF.BusinesUnitId = @BusinessUnitId

                            {(string.IsNullOrEmpty(dtParameters.RequestNumber) ? "--" : "")}and PRF.PRFNo LIKE '%' + @RequestNumber + '%'

                            {(string.IsNullOrEmpty(dtParameters.RequestDateFrom) ? "--" : "")}and cast(PRF.RequestDate as date) >= CAST(@RequestDateFrom as date)
                            {(string.IsNullOrEmpty(dtParameters.RequestDateTo) ? "--" : "")}and cast(PRF.RequestDate as date) <= CAST(@RequestDateTo as date)
                             {(string.IsNullOrEmpty(dtParameters.Spending_Category) ? "--" : "")}and PRF.Spending_Category = @Spending_Category
                             {(string.IsNullOrEmpty(dtParameters.Spending_SubCategory) ? "--" : "")}and PRF.Spending_SubCategory = @Spending_SubCategory

                            {dtParameters.OrderBySql("RequestDate")}";
                if (!dtParameters.IsExport)
                {
                    query += " OFFSET @Start ROWS FETCH NEXT @Length ROWS ONLY";
                }
                dtResult.Data = await Task.FromResult(_iDapper.GetAll<SpendingCategoryListView>(query, new(dtParameters), commandType: CommandType.Text));

                dtResult.RecordsFiltered = dtResult.Data.Any() ? dtResult.Data.ElementAtOrDefault(0).CountData : 0;
                dtResult.RecordsTotal = dtResult.RecordsFiltered;
                return dtResult;
            }
            catch (Exception e)
            {
                dtResult.Error = JsonConvert.SerializeObject(e);
                string nl = Environment.NewLine;
                dtResult.Error += $"{nl}sql:{nl}{query}{nl}param:{nl}{JsonConvert.SerializeObject(dtParameters)}";
                return dtResult;
            }
        }
        public async Task<List<SpendingCategoryMappingModel>> SpendingCategoryMapping(SpendingCategoryMappingParam param)
        {
            try
            {
                string query = $@"select(SELECT 
                                 PSCM.Id [MappingId],
                                 PSCM.CategoryManagementId [CategoryManagementId],
                                 PSCM.Spending_CategoryId [Spending_CategoryId],
                                 PSCM.SpendingSubCategoryId [SpendingSubCategoryId],
                                 PSCM.AccountId [AccountId],
                                 PSCM.Email [Email],
                                 PSCM.Seq [Seq],
                                 PSCM.Role [Role],
                                 PSCM.StartegicSourcingId [StartegicSourcingId],
                                 PSCM.Status [Status],
                                 FORMAT(PSCM.CreatedTime, 'dd MMM, yyyy', 'en-US') [CreatedTime],
                                 PSCM.CreatedBy [CreatedBy],
                                 FORMAT(PSCM.LastUpdatedTime, 'dd MMM, yyyy', 'en-US') [LastUpdatedTime],
                                 PSCM.LastUpdatedBy [LastUpdatedBy],UA.UserName 
                                 FROM PRFSpendingCategoryMapping PSCM
                                 Join Flips.UserAccount UA on UA.Id = PSCM.AccountId
                                 WHERE PSCM.Spending_CategoryId = @Spending_Category AND PSCM.SpendingSubCategoryId = @Spending_SubCategoryDetail for json path,INCLUDE_NULL_VALUES)JsonResponses";
                var resData = await Task.FromResult(_iDapper.Get<JsonResponse>(@$"{query}", new Dapper.DynamicParameters(new
                {
                    Spending_Category = param.Spending_Category,
                    Spending_SubCategoryDetail = param.Spending_SubCategoryDetail
                }), commandType: CommandType.Text));

                var response = JsonConvert.DeserializeObject<List<SpendingCategoryMappingModel>>(resData.JsonResponses);
                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task SaveAndSendEmailPrf(sendEmailTest param)
        {
            await _nonShopNotificationRepository.SaveAndSendEmailPrf(param);
        }
        public async Task<ResponseData> PickUserPRF(PickUserAddEdit param)
        {
            ResponseData response = new ResponseData();
            try
            {
                string query = $@"DECLARE @dateInserted datetime = GETDATE();
                              
	                                UPDATE PRFSpendingCategory SET Status = 0, LastUpdatedBy = @Username, LastUpdatedTime = @dateInserted Where Id = @spendingCategoryId
	                                IF @@ROWCOUNT < 0
	                                BEGIN
		                                select 'error_id' = 400, 'error_msg' = 'Failed'
		                                return;
	                                END
	                                INSERT INTO PRFSpendingCategory
	                                (
		                                PRFSpendingCategoryMapId,
		                                PRFId,
		                                AccountId,
		                                Username,
		                                Email,
		                                Status,
		                                CreatedTime,
		                                CreatedBy
	                                )	
	                                select 
		                                @mappingId,
		                                PRFId,
		                                @mappingAccountId,
		                                (select top 1 Username from Flips.UserAccount where Id = @mappingAccountId),
		                                (select top 1 Email from Flips.UserAccount where Id = @mappingAccountId),--map
		                                1,
		                                @dateInserted,
		                                @Username
	                                from PRFSpendingCategory where Id = @spendingCategoryId

	                                IF SCOPE_IDENTITY() > 0
	                                BEGIN
		                                select 'error_id' = 200, 'error_msg' = 'Succes'
	                                END
	                                ELSE
	                                BEGIN
		                                select 'error_id' = 400, 'error_msg' = 'Failed'
	                                END";

                response = await Task.FromResult(_iDapper.Insert<ResponseData>(query, new Dapper.DynamicParameters(new
                {
                    mappingId = param.mappingId,
                    mappingAccountId = param.mappingAccountId,
                    Username = param.Username,
                    spendingCategoryId = param.spendingCategoryId
                }), commandType: CommandType.Text));

                await updateBuyerAccount(param.prf_id, param.mappingAccountId);
                await SendEmailNotificationBuyer(param.prf_id);
                return response;

            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public FileModel GeneratePdfPRF(long prfId)
        {
            try
            {
                #region Get PRF
                string queryGetPRF = @"SELECT PRF.PRFNo [PRFNumber]
		                                        ,FORMAT(PRF.RequestDate, 'dd-MMM-yyyy') [RequestDate]
		                                        ,PRF.RequestorUserName [RequestorUserName]
		                                        ,UA.CostCenterName [Department]
		                                        ,BU.Name [BusinesUnitName]
		                                        ,CC.Name [CostCenterName]
		                                        ,CASE
			                                        WHEN PRF.IsBudgetedSpend = 1 THEN 'Yes'
			                                        ELSE 'No'
		                                        END [IsBudgetedSpendString]
		                                        ,PRF.BudgetCode [BudgetCode]
		                                        ,FORMAT(PRF.TotalBudgetEstimation, 'N', 'id-ID') [TotalBudgetEstimationString]
		                                        ,PRF.TypeOfRequest [TypeOfRequest]
		                                        ,CASE
			                                        WHEN PRF.IsPurchasedPreviously = 1 THEN 'Yes'
			                                        ELSE 'No'
		                                        END [IsPurchasedPreviouslyString]
		                                        ,PRF.DetailPreviously [DetailPreviously]
		                                        ,PRF.RepurchaseNotes [RepurchaseNotes]
		                                        ,SC.Category [SpendingCategoryName]
		                                        ,SSC.Spending_SubCategory [SpendingSubCategoryName]
		                                        ,PRF.TypeOfTransaction [TypeOfTransaction]
		                                        ,ISNULL(PRF.ProjectCode, '-') [ProjectCode]
		                                        ,PRF.AditionalRequestRequirement [AditionalRequestRequirement]
		                                        ,CASE
			                                        WHEN PRF.SLARequired = 1 THEN 'Yes'
			                                        ELSE 'No'
		                                        END [SLARequiredString]
		                                        ,ISNULL(PRF.SLANotes, '-') [SLANotes]
		                                        ,CASE
			                                        WHEN PRF.PenaltyBySuplier = 1 THEN 'Yes'
			                                        ELSE 'No'
		                                        END [PenaltyBySuplierString]
		                                        ,ISNULL(PRF.PenaltyBySuplierNotes, '-') [PenaltyBySuplierNotes]
                                                ,PRF.ApprovalRequestId [ApprovalRequestId]
                                                ,PRF.BuyerUserName
                                                ,FORMAT(PRF.LastUpdatedTime, 'dd MMM yyyy') [LastUpdatedTime]
                                        FROM dbo.PRF PRF
                                        JOIN Flips.UserAccount UA ON UA.Id = PRF.RequestorAccountId
                                        JOIN dbo.CostCenter CC ON CC.Id = PRF.CostCenterId
                                        JOIN dbo.BusinessUnit BU ON BU.Id = CC.BusinessUnitId
                                        JOIN dbo.Spending_Category SC ON SC.Id = PRF.Spending_Category
                                        JOIN dbo.Spending_SubCategory SSC ON SSC.Id = PRF.Spending_SubCategory
                                        WHERE PRF.Id = @PRFId";

                var resultPRF = _iDapper.Get<PurchaseRequestFormModel.PurchaseRequestFormView>(queryGetPRF, new Dapper.DynamicParameters(new
                {
                    PRFId = prfId
                }));
                #endregion

                #region GET PRFDetail
                string queryGetPRFDetail = @"SELECT PRFD.RequestItemNotes [RequestItemNotes]
		                                            ,PRFD.Qty [Qty]
		                                            ,PRFD.Unit [Unit]
		                                            ,FORMAT(PRFD.DeliveryRequestDate, 'dd-MM-yyyy') [DeliveryRequestDate]
		                                            ,PRFD.DeliveryNotes [DeliveryNotes]
                                            FROM dbo.PRFDetail PRFD
                                            JOIN dbo.PRF PRF ON PRF.ID = PRFD.PRFId
                                            WHERE PRF.PRFNo = @PRFNumber";

                var resultPRFDetail = _iDapper.GetAll<PurchaseRequestFormDetailModel.PurchaseRequestFormDetail>(queryGetPRFDetail, new Dapper.DynamicParameters(new
                {
                    PRFNumber = resultPRF.PRFNumber
                }));
                #endregion

                #region Get Approver
                string queryGetApprovalRequestGroupMember = @"SELECT ARGM.UserName [UserName]
		                                                            ,FORMAT(ARGM.ApprovalDate, 'dd MMM yyyy') [ApprovalActionDate]
                                                            FROM dbo.ApprovalRequestGroupMember ARGM
                                                            WHERE ARGM.ApprovalRequestId = @ApprovalRequestId";

                var resultApprover = _iDapper.Get<APS_REST_API.Models.ApprovalRequestGroupMemberModel.ApprovalRequestGroupMemberPrf>(queryGetApprovalRequestGroupMember, new Dapper.DynamicParameters(new
                {
                    ApprovalRequestId = resultPRF.ApprovalRequestId
                }));
                #endregion

                StringBuilder trPRFDetail = new StringBuilder();
                int indexPrfDetail = 1;

                foreach (var prfDetail in resultPRFDetail)
                {
                    trPRFDetail.Append(
                        $@"<tr>
				            <td>{indexPrfDetail}</td>
				            <td>{prfDetail.RequestItemNotes}</td>
				            <td>{prfDetail.Qty}</td>
				            <td>{prfDetail.Unit}</td>
				            <td>{prfDetail.DeliveryRequestDate}</td>
				            <td>{prfDetail.DeliveryNotes}</td>
			            </tr>"
                    );
                    indexPrfDetail++;
                }

                StringBuilder htmlString = new StringBuilder();

                htmlString.Append(
                    $@"<div style='padding-top: 15px; padding-right: 27px; padding-left: 27px; font-family: sans-serif; font-style: normal;'>
	                    <div style='text-align: end; margin-bottom: 8px'>
		                    <img src='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAANQAAABACAYAAABiKVlNAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAABS2SURBVHgB7Z0JXFTVGsC/cy4IyDqgMLg9NEszy1LTFEjMUpbMcu35smzRygC1TSsXUlvstSik+Wzz2fJLzayngkslAUq5m2mWpmQqiwIjq8Dcc95378AMM8AwA4MOdv6/38zcufcsd+493/2WswyAQCAQCAQCgUAgEAgEAoFAIBAIBAKBQCAQCAQCwd8F0miK0e//hKluAiels74MNhZ8LwWzcum01Bbm+Q64I2Xjiz+AQHAFcGk0BYc2+O4OTsrs0sPQRS6BPMkDxmgi4LSrZ+MPCYGghaDQinm+5Bf4Z9kpdft1r95wWvIEgeBK0riGclJ6ykUwGwVK4Q0UplUe14Dg6oNv8PMDd3qXHiAMTQ8fZR8Bkk0Z30FiCraDk9EqBaqzXApfFBjcpCzJCxZ73QCCloEn+w1hXIrklPfFhnyduo/DBULYbuoqLSV3XvgdWgC+2VPLiPtMRmEquh1+lqYUo+QFOcX/JOX8QRJduBOchFYnUASv7ntFu9FvKlVNvHv9h+Ie4TY5Gr5FE8M4XcAA+iqXl9S6xoRACHoL/VkVnyanaJKkqMJ4cBCKRmIedCbjZDp+9cWbawXSjRGSIW8OiJVi8peBE9DqBEox8wZX5Krbr3jfKPwmB8O3anrLjCzFBn2HbTlonJzs7yVFFzwCzYRv1EQziSQBJ93sykhgEf/Wezu5s7jJ2pJv8hsqU+lpQshtqIKrOOFbJcYSSIzuT3vKaVVBiWGVOfBcyRF1+20089a5h4DAMSiaAbXNq4yRQ6iNbBSmagh5GIVqBjQRRYj1Kf7fMRe6GcuyT5jU+sGPVbluLf7KKxDsRDFp9SkBO5kkfY+a927c0w7LC8ZrMJlRmso3+/3DnvJajUApftOyiz+p28dc/WCR140gaD7oDxEUpNnMQ8JwKX0BW2fT2gQh7yiN054sqhAnByQyRg/bLcR1TyDEw8NtI1+rdvM0XvfGgI4oSOsZkVLRmB3cUJkoVFvUwIiNtAqBUvymT3QZECiXw1/YeTteczsImg9P1kSxLf4HsBm8pjj+0EwYoRuw8YU0lu7UxyHuGFCYrwoxgThwECgYA2Rv/+XW0hSs1fiqdUtwHNOPhsZL7Sm70ffBRlqFQCUUH4KbqgrV7Sl+g+EMbQuCpmM0sQhNxgbTBxwG0TB3+r01M4kn+w/voi1Cu50kOEKI65wBkEdR83xteQ4Gbah5ydeHZql1E/CwuUxCxspbNC/aktbpgxJjK05DXOkxdXuRTx/Y7doOBE1DjaC5S68wBtNaLi5Kuiq+hz7Z/1WJVKWRqOLfeLqvBkqlgTInszBqGAGOpQJ/mYT1Gtsy/rZRjEqjUBP9hhZtNuHcjxFAH4FI1qOGVmB0IZq0O0m0zuqwNqcWKMVvekO3V93e4aaFt9teDwL7+WttJ48OXuWPYZ/OAodpBQ7l6MSfxa1f1Zg6By0HjtqOuCq+Bx5bydCdkVMCgJUYsjhGiPlJLGkN42yzi4fvPjI065K6F30i2YWFU6DPoMz0r66xB9bZQ4nzNxsCF1EeuzSWzGkFyh0YrNOlgR+vVEPjM3wGgMB+1I5ZUp6Emzc2+elcA4ESwmCLTPmHLpfYj+Q+nc6srox23lDMR6MWSsCvIeBAOOdpEqUJMCI/lZCaX1JoOrWR+Ypwf6G8eErAZNRIKzCVGzQfrIsnUV65iESVnG8ssdMK1NySn+G6qosg49V7RBOmBiMEtsM3a26UKU10iImF2ggIX0ZJ5b9JdEleQ8lI2IVi/PgvT/H7gYP0AbbEYdBcCMmlsjyfxBT+x+YsUfmr+Ea/AxgKT8eHgDc0EdRrqYSyODKi8Bdb8zilQE0vOwZPlhj8ptcxPL7fRQMC2+HbNKFMpt8SB8wSwLD6dkmvf5zcc/GUrXlIlC4L893Ft/qvxE7ax6BpFGGLTqLl+jctNaFN5zBSdwgfKhMx8LIR7ASv2zG0Yp8kURdSwU6cLsqn+E2ziw0PhC/bdoW3PHuBwD7IcGVsG18NzQC1y25KyB0u0fnD7REm4zmgWUaLCp5CwdoHdoLm3QbK5JukyPw5TREm4znEFG5CzTrf9gygA85mE28yoCnCpOBUAtWRqZMFwY3rVb9prvfNIGgatLhgGjbNQ2A/6Jjwp6X2IWEk8sIOaAZkPFRKRB6Lm1k2pQf+MwMy1CW6YLS9Q34aQoosWIAPh68aSYaWMUsqray8VoouXFxtujYJpxKoWSW/qINey6kL3ON/B+QSR/iUf0+wMcu0DRuFrTTf9lz8TXpJ7iZFFbxD+u+rAgegmH965nIPbpZZSXUB36aTyIKbXZuoGawhFef/Ez/W1XcMw+oZVObhygBfn3uKL0AzcRof6qmy3+CBspPq9nzUTGLQa/Mhw3R/8m3t72SM7bAWLledb5m/RO4u2NVYmdqw6bPwaX6vKTM5k5OeOM5aHreY3MN8i38s4+SjOgc5vEdp5RwSWVQALYSiKTnPnwBbNKkc6L9QY3VBbfgL9k8tJ9GFdvtY1nAKgQqRS2Bh0QF1+2PP7vCBR3cQOAYy/PxBjLrdglG3t3idoTY8izM2Q4rRfWN7gdAT324zFcGP2ZQtsuDjqpSA05TARGzIPuj0HwImf+Io067R+tVQe6EyLGk5tCBXXKB8eRV8eDET8iV3OE/d4HnvfiBwLIrZhR9jULBC9Iz2JJRomR6OthlZsBsuI65R+d/hx3dwFXPFBeoidqwP878LBC1PtWBlgaDFcKhAJU4eBN2D1Wn/cDKnCBb/7zD8Z2qo8fieE+dh/rr9xu+B3u6wKtYw4v/P88Xw7Ce7YebdN8Lg6wzTWtKOZsPr3/wMAkFrwWECNeyGYIgbaVrbISuvGGI/zgQPDxeIwGMKPTv5mQnU48Ovh6j+naFKz6Bn/DroqfWFhRNNJl97P3chUIJWhUMEys2FwvLHw8z2hQR6Q7CfB3z14ymjQHXVekMH3HdOVw6399LCgmrheeajH6GsXA/fJUSbldG/e3vwa9sGdGWVcCUICZnsXtHBM0iv4/nnjy4vsZY2OOKZdlB5ydMzj+acOJFUAU2op8LFjXUtLcnZt29ls0PW7cJnBDNJlgpSk85AEwga/GwghXIPt3OluVlZqy6BwCYcIlDzx/WD6zr6QmFRBRCJgJ+nYdLk2EHdYE3GH5D4qGlC5LjQa+Cb3VmwOs5g6iWs2Q9JW4/Ce1NCVSG8cPEStPM1jZiJvqUzfL7zD2gJtGHxH9XMi8EQkD43PXGSsh0cFn8ffp9+ifCB6Hm4SwGYNjx+D+OQlJeR+ElN/va9pnlRf5fpGHaexOWqHiBJUBLML2m18amyLMWdz3znRH31+kXM8POo4hM4YcMx/NS3nGMYlwBtgx3aZ9q667WhcYc5Iesl6rL6XNrbf1nmx/ObzgkYI22S5P7c2dQ3znSMiOukLJwChD7IgXUEmSi/sRjPb6MMdG5expKT1q5H0OAnAgl1eRLzTwaoDMHIIJR39tFrO8cfAJkvz9mVtAoav6ar8Zq61nwnHJKzq6+ZdnBcDEjkgZpjTCZf5u1aur76N0UywkcDI3g/+L7cnUlLOobF9ZEJmV27fFmS5p5Prf+6OgPN7ti9H4XmhbGGOWrz1+6HPJ2p/27UrV0gr+gSpB/NMe4b2a8zbJsXBf9A4dl+8Cy8jHleHH0zPBFpmJox5rVtUFElG9OH9rR7mQDboWSo8hOUFzY65UaToPC497GxfoWNYogiTLVS34oh39XasFh1KIs2YkaIFOCShoKwCNP2MCXDPAQiJRf5eFBYXJ3VgLThcUM8ZHaKU74ChUkJYytTHYz3gSgPOUpuUcplXJ8WiI3KsgwUtptrzlt56VlZaFBo7KPYQA9zSl/A8+9oOh3wxu8TKWF/BIXFvgQNEBj21CQqtfkNhSkBao0UJ4aH7q0oCB9rQ+NXKbWDNQgfV/vcGD4wTOdNtbWPUcp7twuN7YAPq1QsNQVD6VMIhQewAvU34/PAr3Z65SXpeSdwYpolUCHtveCthweq229jACIp5QiszzR1K9yGwQXFZEvZb3rIDuvTEa7t4Is+Vgk8sjxNLePlCYZrrmirtON5kPm7aUDzxCHXwuUCb+wbeFOtD+bEBhcYPn0MyEwZlnOL1aSEvKK97YmQ2vvcJekQaj9b5ySFoBB/1y58arDVVBz6EEo/aKxcQuiiwPDYUMv9ijBSIq1u9LwoPITvY6CJ4EOiyHKfC1Wnl9u1FoUz02SBcsU7/fXzd0GHAE/4/exFWLDO0DGbfMAkPJ4erjAU/ae1u8zHVpZX6CFmYQo+9AjsWBADLuiD/S8zS9VWCorfVYNiPvbvdtlm6T5b/VmAjQsbKFFudpZlIgrsQzA+xUkmvq3Ax68ytKXcIqkXd2kzr/aOrNQlOpSATOwQ3Y+CkIDbEVBFu1ZKvDNh0q2cw0rzIkiAxN1mgxWIurhKNRyUURHL8MHwNX6yOufOyaLa3xVNSyW6uJ5i0/A1C3M8TIC9Bqbr4AVNhRGz4UfVHc3RcBXRZB8qYUI/6NMtAHSllTD3873QI9hX3V+J5lpRSQX4eBnG4d03MAQ27PkTDp7Kh5u7BhjSYFTv13MXIfnFEarfpJTxafofMOCa9urx38+aP8iG9g6GvSebPczKJtDcSCuX6Cid2vABOtw+rTPjLoo2qrXWM/Gt3liYM0yTAAkJasNVGifI8k94PLBWgfdBRMRUSE3V1+xCv+Ch8+mJx+upXgkg7EWzEKMwJLZWfZPxTVmmy8oUQa5MfhuXk5FknKKtnA+R2QEzzUNIhBJwyN31psEMkPl8PB5gVhLni9CfnIdpea2yVuJv24wFNHn4P6FMrv0DiPFfXUgxVvo2+p2fSpzrGefl0EppkkCN6tcFXhxrGAmuaJA1zzU8jyy6fxdYNKG/UZgUfDHPp/EREIn+VE0Za59vuIwYrO/faFJeFmT5QV16onHKwLm05X+hwxyr2PgWKU/mKI0u3bQjJ3VJFjrly1GIEmr2oYD6BVfe0DEbUo228Pm0eoXJCCfSBsKZUaCw4fkEDYrtnZv5boMXAc3LOdlpiWbrHSjnExQ+A81Y9qp54kuK77im04iZ/vpS+UGzuemEH8tNT5oLJAksy0JBV/wjpR9DAgdCQY46l/Gu0yyn3BzsFijF51n5pCFEfjArX9Uu9VETKg/wdoOXxtedhjFxiGG8XuaxPKiQ5TrHFd+rRgiHoIa6LOFzTlKydy2rO7bMxXUvyBaRbA5f1lcEqYJNvI1JoBSoJCk/tsExa+1vj78Wzci+hJEQNajB5XaWKzCgWWwtOiMzXeFn9R3gwDdjSeYCBZJqtrEyORyIhdkvk9ehAXLSk44Gh03/kRMeCg4Cz+2zc+lXhzAp2CVQit+k+DyBmrawFQMNka9srTedLzb+rBX3G8PnCombj8BNIf5GQVN4L+VXmPZB/ddSyZu/ahJQamhYDw29DpZutnkmcpPAmuqdP5Sd+taF4PB4nYXTXu90BOrBc2XZXBj09fgdwYOf+gdG5OJRszzC1ZHgtJYM1Q2k6a37Ltm5P39SWt8B4uH6B5RbPIgIV8vijAxAp8rikJwKVkBNrWhXhwkUSvyncBVhV1Divalhqs9jiNClN5juImqSjCPZxu8yduDM+WwPfJFm3p+0AfujGkLRfD8cMYXbR93a4FJvTkUlyPrG0gSHxs1gknQC7bSn7Yj4WaPBOnO3vVna0DFOoYPlvno1dAvCgBTCVYTNAjVnzC3w6F2G7pZRi7fDucIyq+lPXzDdRwm1zKAeQdAxwHyO09AbrEeDv9lrurcRvbWq2dfawdD8XE7JO8TcOlCc9W1K1A/3P4l6wmGrqTo7jBG7RpU4OzYJ1MDu7Y1j7GZ+mAk/Z1mfBDpuYFeYFm0eDHp6ZG+YO9682+beAda1zpcZpo59NI3g/lD715F3JtQoIMACs50cMmRJ3wmjaiNyMhJfzk5PXIH9CZvgMkCA19FsQTdNEjM7m0GjAtWjoy988YxhHfd3Nx2BJclHrKZXghbvThmkbh88ZZqEOaKvIaJ3Js80JO76Lhro1aFhi+esrgwOnDQJ74SwVv4vhTIbX/sraqUSztqMOZ+6PAeuALyeNSckn4BB1jMx8VeRVmhUoNIWjHSp8ZvmrbG+gE0nf09j0GI9apfHlpmvWquUMexl8+jzv263fn9+OGryxSKqo32tFc6JWbgTO1/3GvuDaiFVOn7N7waoE4ZnRP9oQ4kVDcsJtS5wf3MajfLtPZ7bhkgUXl1/EAobCVtPj7oBfv1Lp76ewSDEOfSjUvaZRk4sXHcAfs+5CJ/uOA4BPoZhct2CfKyWqYTVewSbygi7Xgub9p2G1gj2k5qH7wivd66/TNkkuAzkDAtID/624KzZ2D+g9weGxSfXHgSs0K/fVNdzVfISoMSizZDW79g6kEYFKub17WW2Lkr93Gd1Z1RHv1o3tD7pXavrrZuxNvOk+rpKOGr2jUOnoLC4ObkZSepwIM2ds3zdK8qeR1PwabgcJCQwFhY3DzXlh7V3q4OAQ+PuYMA+RxPmHEhS/7MclL/orDt2kbSuP+1racTFuIwQBv+ts4+QhUHhcbnasLhf3SvKsziQev82BU2tpo+hswIKs7ISUVqdA5RMplTahr3Sv6Dgr4JqYUJttsEiZYucV2tFCNRlJHtnomKrzrLcT5Sxf4T0NPVJ8XXYcLeZpWE8AFoILleOQwfviA0p91CgllNAmr3c89WELQKla2UvpyYnPfENQ18T1JlJi/t1nPGZOelJ4wmHk+bHuD+0ELm7VuTlZCT1xlrm17cwpnpe2DfvlQ3hVbzONRYaqhaNDz3aMGUoXKW4U43ZH06FAOizG0jrJvkHW6atL9athMBDIhI8LNPm1vqu9DX16jXuo0L/YPRTaD8KrIQTfhDKKvbk7lup9pj3lPzjsrDbr776ekqaKVkGobQJd8nf6vnUkJO+dEH37nGLSzvAEAakL2W0ErXm/gpveX9BSlKRmucEZFv+PvO6AjQN1ZWdvnRryNCXzfJmDYNKaGB5TXyw/FDftbwifQwCgUAgEAgEAoFAIBAIBAKBQCAQCAQCgUAguDr4P/3CmvEqdqaNAAAAAElFTkSuQmCC'
		                    style='width: 89px; height: 27px;' />
	                    </div>
	                    <div style='background: #0070C0; height: 18px; display: flex; justify-content: center; align-items: center; margin-bottom: 6px; font-size: 11px; color: white; font-weight:bold'>
                            Purchase Request Form
                        </div>
	                    <div style='font-size: 9px; display: flex; flex-direction: row; margin-bottom: 6px;'>
		                    <div style='height: 20px; padding-left: 14px; padding-right: 14px; display: flex; align-items: center; font-weight:bold'>
			                    No.PRF : {resultPRF.PRFNumber}
		                    </div>
		                    <div style='background: #DDEBF7; height: 20px; margin-left: 321px; width:124px; padding-left: 14px; padding-right: 14px; display: flex; align-items: center; font-style: italic;'>
			                    Date of Request : {resultPRF.RequestDate}
		                    </div>
	                    </div>
	                    <div style='background: #DDEBF7; font-size: 9px; height: 18px; display: flex; align-items: center; margin-bottom: 6px; padding-left: 14px; font-weight:bold;'>
		                    Requisitioner Details
	                    </div>
	                    <div style='font-size: 9px; display: flex; align-items: center; margin-bottom: 6px; padding-left: 11px;'>
		                    <table style='font-size: 9px;'>
			                    <tr>
				                    <td width='200px'>Requisitioner Name</td>
				                    <td>:</td>
				                    <td>{resultPRF.RequestorUserName}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>Department</td>
				                    <td>:</td>
				                    <td>{resultPRF.Department}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>Entity</td>
				                    <td>:</td>
				                    <td>{resultPRF.BusinesUnitName}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>Cost Center</td>
				                    <td>:</td>
				                    <td>{resultPRF.CostCenterName}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>Is this request a budgeted spend?</td>
				                    <td>:</td>
				                    <td>{resultPRF.IsBudgetedSpendString}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>Budget Code / account code</td>
				                    <td>:</td>
				                    <td>{resultPRF.BudgetCode}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>Estimated total budget for this request</td>
				                    <td>:</td>
				                    <td>{resultPRF.TotalBudgetEstimationString}</td>
			                    </tr>
		                    </table>
	                    </div>
	                    <div style='background: #DDEBF7; font-size: 9px; height: 18px; display: flex; align-items: center; margin-bottom: 6px; padding-left: 14px; font-weight:bold;'>
		                    Request Details
	                    </div>
	                    <div style='font-size: 9px; display: flex; align-items: center; margin-bottom: 6px; padding-left: 11px;'>
		                    <table style='font-size: 9px;'>
			                    <tr>
				                    <td width='200px'>Type of Request</td>
				                    <td>:</td>
				                    <td>{resultPRF.TypeOfRequest}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>Has the product / service been purchased previously?</td>
				                    <td>:</td>
				                    <td>{resultPRF.IsPurchasedPreviouslyString}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>If yes, please provide details of previous purchase (e.g. Purchase Order no., Purchase date, Quantity, Supplier etc.)</td>
				                    <td>:</td>
				                    <td>{resultPRF.DetailPreviously}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>If recurrent purchase, please provide estimated annual volume and indenting frequency</td>
				                    <td>:</td>
				                    <td>{resultPRF.RepurchaseNotes}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>Spending by Category</td>
				                    <td>:</td>
				                    <td>{resultPRF.SpendingCategoryName}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>Sub Category</td>
				                    <td>:</td>
				                    <td>{resultPRF.SpendingSubCategoryName}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>Type of Transaction</td>
				                    <td>:</td>
				                    <td>{resultPRF.TypeOfTransaction}</td>
			                    </tr>
			                    <tr>
				                    <td width='200px'>Project Code</td>
				                    <td>:</td>
				                    <td>{resultPRF.ProjectCode}</td>
			                    </tr>
		                    </table>
	                    </div>
	                    <div style='background: #DDEBF7; font-size: 9px; height: 18px; display: flex; align-items: center; margin-bottom: 6px; padding-left: 14px; font-weight:bold;'>
		                    Details Request
	                    </div>
	                    <div style='font-size: 9px; display: flex; align-items: center; margin-bottom: 6px; padding-left: 11px;'>
		                    <table style='font-size: 8px; border-spacing: 0px;' border='1' cellpadding='5'>
			                    <tr style='background: #eaeaea;'>
				                    <td>No.</td>
				                    <td>Detailed of Goods Specification / Scope of Work Requested</td>
				                    <td>Quantity</td>
				                    <td>Unit</td>
				                    <td>Estimated Delivery Date / Period Of Services</td>
				                    <td>Delivery Requirements (Product packaging, Delivery frequency, Delivery method and etc.)</td>
			                    </tr>
			                    {trPRFDetail}
		                    </table>
	                    </div>
	                    <div style='font-size: 9px; margin-bottom: 6px;'>
		                    <b>
			                    <ul>
				                    <li>Any additional request requirement / instructions need to be shared with supplier: {resultPRF.AditionalRequestRequirement}</li>
				                    <li>Required warranty, support and Service Level Agreements (SLAs) from supplier: {resultPRF.SLARequiredString} : {resultPRF.SLANotes}</li>
				                    <li>Recommended penalty for non-compliance by supplier (if applicable): {resultPRF.PenaltyBySuplierString} : {resultPRF.PenaltyBySuplierNotes}</li>
			                    </ul>
		                    </b>
	                    </div>
	                    <div style='font-size: 9px; margin-bottom: 6px; padding-left: 14px;'>
		                    Please attach relevant supporting documents (e.g. BOQ, drawing, photo etc.) with your purchase request submission. PRF received by Procurement before 14.00 will process at the same day, later than 14.00 will process a day after.
	                    </div>
	                    <div style='font-size: 9px; margin-bottom: 20px; padding-left: 14px;'>
		                    “I confirm that I have ensured that all goods or services requested (therefore all related reimbursement or payment) herein are for legitimate business purposes, comply with relevant AXA policies and under no circumstances form a payment for any bribe or facilitation payment of a government official”.
	                    </div>
	                    <div style='font-size: 9px; display: flex; flex-direction: row; margin-bottom: 6px;'>
		                    <table style='font-size: 9px;'>
			                    <tr>
				                    <td><b>Approve by :</b></td>
			                    </tr>
			                    <tr>
				                    <td><b>{resultApprover.UserName}</b></td>
			                    </tr>
			                    <tr>
				                    <td><b>HOU</b></td>
			                    </tr>
			                    <tr>
				                    <td><b>{resultApprover.ApprovalActionDate}</b></td>
			                    </tr>
		                    </table>
		                    <table style='font-size: 9px; margin-left: auto;'>
			                    <tr>
				                    <td><b>Received by :</b></td>
			                    </tr>
			                    <tr>
				                    <td><b>{resultPRF.BuyerUserName}</b></td>
			                    </tr>
			                    <tr>
				                    <td><b>Procurement</b></td>
			                    </tr>
			                    <tr>
				                    <td><b>{resultPRF.LastUpdatedTime}</b></td>
			                    </tr>
		                    </table>
	                    </div>
                    </div>"
                );

                var pdfBytesArray = GeneratePdf.GenerateFromHtml(htmlString.ToString());

                var result = new FileModel()
                {
                    FileName = $"{resultPRF.PRFNumber}.pdf",
                    FileByteArray = pdfBytesArray,
                    FileType = "application/pdf"
                };

                return result;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task<string> UploadPdfPRF(FileModel fileInput, long prfId)
        {
            try
            {
                var paramsUploadAttachment = new AttachmentRequest();

                using (MemoryStream stream = new MemoryStream(fileInput.FileByteArray))
                {
                    System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = fileInput.FileName,
                        Inline = false // false = prompt the user for downloading;  true = browser to try to show the file inline
                    };
                    IFormFile formFile = new FormFile(stream, 0, fileInput.FileByteArray.Length, null, fileInput.FileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = fileInput.FileType,
                        ContentDisposition = cd.ToString()
                    };

                    paramsUploadAttachment.File = formFile;
                    paramsUploadAttachment.FileName = fileInput.FileName;
                    paramsUploadAttachment.Description = "PRF Report";
                    paramsUploadAttachment.MainCategory = "PRF";
                    paramsUploadAttachment.Category = "PurchaseRequestForm";
                    paramsUploadAttachment.CreatedBy = "System";
                    paramsUploadAttachment.RelatedTableName = "PRF";
                    paramsUploadAttachment.RefId = prfId;
                    var responseFileUpload = await _attachmentService.UploadAttachment(paramsUploadAttachment);

                    if (responseFileUpload != null)
                    {
                        return "Upload Success";
                    }
                    else
                    {
                        return "Upload Failed";
                    }
                }
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task<VendorQuotaionDetail> GetVendorQuotationDetail(long PRFID)
        {
            VendorQuotaionDetail response = null;
            try
            {
                string query = @"select(SELECT PVQ.Id,
                                               PVQ.PRFId,
                                               PVQ.Remarks,
                                               PVQ.QuotationNo,
                                               PVQ.Status,
											   FORMAT(PVQ.CreatedTime, 'MM/dd/yyyy') CreatedTime,
                                               PVQ.CreatedBy,
                                               PVQ.LastUpdatedBy,
                                               FORMAT(PVQ.LastUpdatedTime, 'MM/dd/yyyy') LastUpdatedTime,
                                               PVQ.Title,
                                               PVQ.Summary,
                                               FORMAT(PVQ.EstimatedDeliveryDate, 'MM/dd/yyyy') EstimatedDeliveryDate,
                                               FORMAT(PVQ.FinalSpesificationDate, 'MM/dd/yyyy') FinalSpesificationDate,
                                               JSON_QUERY(
                                               (
                                                   select PVD.Id PRFVendorQuotationDetailId,
                                                          PVD.PRFVendorQuotationId,
                                                          PVD.PRFDetailId,
                                                          PVD.ItemDescription,
                                                          PVD.ItemName,
                                                          PVD.TypeOfGoods_SubCategoryId,
                                                          sc.SubCategoryName as TypeOfGoods_SubCategoryName,
                                                          PVD.Remarks,
                                                          PVD.L_Currency_Code,
                                                          FORMAT(PVD.QuotationAmount, 'N', 'id-ID') QuotationAmount,
                                                          PVD.Status,
                                                          FORMAT(PVD.CreatedTime, 'MM/dd/yyyy') CreatedTime,
                                                          PVD.CreatedBy,
                                                          PVD.LastUpdatedBy,
                                                          FORMAT(PVD.LastUpdatedTime, 'MM/dd/yyyy') LastUpdatedTime,
                                                          PVD.VendorId,
                                                          (select VE.Name from dbo.Vendor VE where VE.Id = PVD.VendorId) VendorName,
                                                          PVD.IsSelected,
                                                          PVD.ITRelated,
                                                          FORMAT(PVD.QuotationDate, 'MM/dd/yyyy') QuotationDate,
                                                          PVD.Qty,
                                                          PVD.Unit,
                                                          FORMAT(PVD.ItemPrice, 'N', 'id-ID') ItemPrice,
                                                          FORMAT(PVD.TotalAmmount, 'N', 'id-ID') TotalAmmount,
                                                          FORMAT(PVD.RateAmmount, 'N', 'id-ID') RateAmmount,
                                                          FORMAT(PVD.BaseAmmount, 'N', 'id-ID') BaseAmmount,
                                                          FORMAT(PVD.TotalBaseAmmount, 'N', 'id-ID') TotalBaseAmmount,
                                                          PVD.IsAddMasterItem,
                                                          JSON_QUERY(
                                                          (
                                                              select PVQC.Id OtherCostId,
                                                                     PVQC.PRFVendorQuotationDetailId,
                                                                     PVQC.MtOtherCostCode,
                                                                     PVQC.L_Currency_Code,
                                                                     PVQC.Included,
                                                                     PVQC.VATPercentage,
                                                                     FORMAT(PVQC.OtherCostAmount, 'N', 'id-ID')OtherCostAmount,
                                                                     PVQC.Remarks,
                                                                     PVQC.CreatedTime,
                                                                     PVQC.CreatedBy,
                                                                     PVQC.LastUpdatedBy,
                                                                     PVQC.LastUpdatedTime
                                                                    , PVQC.OtherCost_SubCategoryId
                                                                    , sc.SubCategoryCode as OtherCost_SubCategoryCode
                                                              from PRFVendorQuotationOtherCost PVQC
                                                                left join SubCategory as _sc
                                                                on _sc.Id = PVQC.OtherCost_SubCategoryId
                                                              WHERE PVQC.PRFVendorQuotationDetailId = PVD.Id
                                                              FOR JSON PATH, INCLUDE_NULL_VALUES
                                                          )) vendorQuotationOtherCost,
                                                          JSON_QUERY(
                                                          (
                                                              SELECT PRFCC.Id VendorCostCenterId,
                                                                     PRFCC.PRFVendorQuotationDetailId,
                                                                     PRFCC.CostCenterId,
                                                                     PRFCC.L_Currency_Code,
                                                                     PRFCC.Percentage,
                                                                     FORMAT(PRFCC.TotalAmmount, 'N', 'id-ID')TotalAmmount,
                                                                     PRFCC.Remarks,
                                                                     PRFCC.CreatedTime,
                                                                     PRFCC.CreatedBy,
                                                                     PRFCC.LastUpdatedTime,
                                                                     PRFCC.LastUpdatedBy
                                                              FROM PRFVendorQuotationCostCenter PRFCC
                                                              WHERE PRFCC.PRFVendorQuotationDetailId = PVD.Id
					                                          FOR JSON PATH, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER
                                                          )) vendorQuotationCostCenter
                                                   from PRFVendorQuotationDetail PVD
			                                           --left join PRFSummaryDetail PSD on PVD.Id = PSD.PRFVendorQuotationDetailId and PSD.Status != 0
                                                       left join SubCategory as sc on sc.Id = PVD.TypeOfGoods_SubCategoryId
                                                   WHERE PVD.PRFVendorQuotationId = PVQ.Id
                                                    and PVD.Status not in (0)
                                                   FOR JSON PATH, INCLUDE_NULL_VALUES
                                               )) VendorQuotationDetail
                                        FROM PRFVendorQuotation PVQ
                                        WHERE PVQ.Status != 0 and PVQ.PRFId = @PRFID  FOR JSON PATH, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER)JsonResponses";

                var resData = await Task.FromResult(_iDapper.Get<JsonResponse>(@$"{query}", new Dapper.DynamicParameters(new
                {
                    PRFID = PRFID
                }), commandType: CommandType.Text));

                if (resData.JsonResponses != null)
                {
                    response = JsonConvert.DeserializeObject<VendorQuotaionDetail>(resData.JsonResponses);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task<ResponseData> UpdateVendorQuotationDetail(UpdateVendorQuotation param)
        {
            using IDbConnection db = _iDapper.GetDbconnection();
            ResponseData result = new ResponseData();

            if (db.State == ConnectionState.Closed)
            {
                db.Open();
            }

            using var tran = db.BeginTransaction();

            try
            {
                #region PRFVendorQuotation
                string queryUpdatePRFVendorQuotation = @"UPDATE PRFVendorQuotation
	                                                        SET Remarks = @Remarks,
		                                                        Title = @Title,
		                                                        Summary = @Summary,
		                                                        EstimatedDeliveryDate = @EstimatedDeliveryDate,
		                                                        FinalSpesificationDate = @FinalSpesificationDate,
		                                                        LastUpdatedBy = @CreatedBy,
		                                                        LastUpdatedTime = @CreatedTime
	                                                        WHERE Id = @PRFVendorQuotationId";

                await Task.FromResult(await db.QueryAsync<int>(queryUpdatePRFVendorQuotation, new DynamicParameters(new
                {
                    PRFVendorQuotationId = param.PRFVendorQuotationId,
                    Remarks = param.Remarks,
                    Title = param.Title,
                    Summary = param.Summary,
                    EstimatedDeliveryDate = param.EstimatedDeliveryDate,
                    param.FinalSpesificationDate,
                    CreatedBy = param.CreatedBy,
                    CreatedTime = param.CreatedTime
                }), commandType: CommandType.Text, transaction: tran));
                #endregion

                #region PRFVendorQuotationDetail
                long resultQuotationDetailId = 0;
                string queryUpdatePRFVendorQuotationDetail = @"UPDATE [dbo].[PRFVendorQuotationDetail]
		                                                        SET PRFDetailId = @PRFDetailId,
			                                                        ItemDescription = @ItemDescription,
			                                                        Remarks = @Remarks,
			                                                        QuotationAmount = @QuotationAmount,
			                                                        VendorId = @VendorId,
			                                                        IsSelected = @IsSelected,
			                                                        ITRelated = @ITRelated,
			                                                        QuotationDate = @QuotationDate,
			                                                        Qty = @Qty,
			                                                        Unit = @Unit,
			                                                        ItemPrice = @ItemPrice,
			                                                        TotalAmmount = @TotalAmmount,
			                                                        RateAmmount = @RateAmmount,
			                                                        BaseAmmount = @BaseAmmount,
			                                                        TotalBaseAmmount = @TotalBaseAmmount,
			                                                        IsAddMasterItem = @IsAddMasterItem,
			                                                        LastUpdatedBy = @CreatedBy,
			                                                        LastUpdatedTime = @CreatedTime
		                                                        WHERE Id = @PRFVendorQuotationDetailId";

                string queryInsertPRFVendorQuotationDetail = @"INSERT INTO [dbo].[PRFVendorQuotationDetail] 
		                                                        (
			                                                        PRFVendorQuotationId,
			                                                        PRFDetailId,
			                                                        ItemDescription,
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
                                                                    ItemName,
                                                                    TypeOfGoods_SubCategoryId,
			                                                        CreatedTime,
			                                                        CreatedBy	
		                                                        )
                                                                OUTPUT INSERTED.Id [PRFVendorQuotationDetailId]
		                                                        VALUES
		                                                        (
			                                                        @PRFVendorQuotationId,
			                                                        @PRFDetailId,
			                                                        @ItemDescription,
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
                                                                    @ItemName,
                                                                    @TypeOfGoods_SubCategoryId,
			                                                        @CreatedTime,
			                                                        @CreatedBy
		                                                        )";

                string queryUpdatePRFVendorQuotationCostCenter = @"UPDATE [dbo].[PRFVendorQuotationCostCenter]
	                                                                SET Percentage = @Percentage,
		                                                                TotalAmmount = @TotalAmmount,
		                                                                LastUpdatedBy = @CreatedBy,
		                                                                LastUpdatedTime = @CreatedTime
	                                                                WHERE Id = @VendorCostCenterId";

                string queryInsertPRFVendorQuotationCostCenter = @"INSERT INTO [dbo].[PRFVendorQuotationCostCenter]
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
                                                                    values
                                                                    (
                                                                        @PRFVendorQuotationDetailId,
                                                                        @CostCenterId,
                                                                        @L_Currency_Code,
                                                                        @Percentage,
                                                                        @TotalAmmount,
                                                                        @Remarks,
                                                                        @CreatedTime,
                                                                        @CreatedBy
                                                                    )";

                string queryUpdatePRFVendorQuotationOtherCost = @"UPDATE [dbo].[PRFVendorQuotationOtherCost]
	                                                                SET VATPercentage = @VATPercentage,
		                                                                OtherCostAmount = @OtherCostAmount,
		                                                                LastUpdatedBy = @CreatedBy,
		                                                                LastUpdatedTime = @CreatedTime
	                                                                where Id = @OtherCostId";

                string queryInsertPRFVendorQuotationOtherCost = @"INSERT INTO [dbo].[PRFVendorQuotationOtherCost]
                                                                    (
                                                                        PRFVendorQuotationDetailId,
                                                                        MtOtherCostCode,
                                                                        L_Currency_Code,
                                                                        Included,
                                                                        VATPercentage,
                                                                        OtherCostAmount,
                                                                        Remarks,
                                                                        OtherCost_SubCategoryId,
                                                                        CreatedTime,
                                                                        CreatedBy
                                                                    )
                                                                    VALUES
                                                                    (
                                                                        @PRFVendorQuotationDetailId,
                                                                        @MtOtherCostCode,
                                                                        @L_Currency_Code,
                                                                        @Included,
                                                                        @VATPercentage,
                                                                        @OtherCostAmount,
                                                                        @Remarks,
                                                                        @OtherCost_SubCategoryId,
                                                                        @CreatedTime,
	                                                                    @CreatedBy
                                                                    )";
                foreach (var quotationDetail in param.ParamQuotationDetail)
                {
                    if (quotationDetail.PRFVendorQuotationDetailId != null && quotationDetail.PRFVendorQuotationDetailId > 0)
                    {
                        await Task.FromResult(await db.QueryAsync<int>(queryUpdatePRFVendorQuotationDetail, new DynamicParameters(new
                        {
                            PRFVendorQuotationDetailId = quotationDetail.PRFVendorQuotationDetailId,
                            PRFDetailId = quotationDetail.PRFDetailId,
                            ItemDescription = quotationDetail.ItemDescription,
                            Remarks = quotationDetail.Remarks,
                            QuotationAmount = quotationDetail.QuotationAmount,
                            VendorId = quotationDetail.VendorId,
                            IsSelected = quotationDetail.IsSelected,
                            ITRelated = quotationDetail.ITRelated,
                            QuotationDate = quotationDetail.QuotationDate,
                            Qty = quotationDetail.Qty,
                            Unit = quotationDetail.Unit,
                            ItemPrice = quotationDetail.ItemPrice,
                            TotalAmmount = quotationDetail.TotalAmmount,
                            RateAmmount = quotationDetail.RateAmmount,
                            BaseAmmount = quotationDetail.BaseAmmount,
                            TotalBaseAmmount = quotationDetail.TotalBaseAmmount,
                            IsAddMasterItem = quotationDetail.IsAddMasterItem,
                            CreatedBy = param.CreatedBy,
                            CreatedTime = param.CreatedTime
                        }), commandType: CommandType.Text, transaction: tran));
                    }
                    else
                    {
                        var resultQuotationDetail = await Task.FromResult(await db.QueryAsync<long>(queryInsertPRFVendorQuotationDetail, new DynamicParameters(new
                        {
                            PRFVendorQuotationId = param.PRFVendorQuotationId,
                            PRFDetailId = quotationDetail.PRFDetailId,
                            ItemDescription = quotationDetail.ItemDescription,
                            Remarks = quotationDetail.Remarks,
                            L_Currency_Code = quotationDetail.L_Currency_Code,
                            QuotationAmount = quotationDetail.QuotationAmount,
                            VendorId = quotationDetail.VendorId,
                            IsSelected = quotationDetail.IsSelected,
                            ITRelated = quotationDetail.ITRelated,
                            QuotationDate = quotationDetail.QuotationDate,
                            Qty = quotationDetail.Qty,
                            Unit = quotationDetail.Unit,
                            ItemPrice = quotationDetail.ItemPrice,
                            TotalAmmount = quotationDetail.TotalAmmount,
                            RateAmmount = quotationDetail.RateAmmount,
                            BaseAmmount = quotationDetail.BaseAmmount,
                            TotalBaseAmmount = quotationDetail.TotalBaseAmmount,
                            IsAddMasterItem = quotationDetail.IsAddMasterItem,
                            quotationDetail.ItemName,
                            quotationDetail.TypeOfGoods_SubCategoryId,
                            CreatedBy = param.CreatedBy,
                            CreatedTime = param.CreatedTime
                        }), commandType: CommandType.Text, transaction: tran));

                        resultQuotationDetailId = resultQuotationDetail.FirstOrDefault();
                    }

                    #region PRFVendorQuotationCostCenter
                    if (quotationDetail.ParamCostCenter.VendorCostCenterId != null && quotationDetail.ParamCostCenter.VendorCostCenterId > 0)
                    {
                        await Task.FromResult(await db.QueryAsync<int>(queryUpdatePRFVendorQuotationCostCenter, new DynamicParameters(new
                        {
                            VendorCostCenterId = quotationDetail.ParamCostCenter.VendorCostCenterId,
                            Percentage = quotationDetail.ParamCostCenter.Percentage,
                            TotalAmmount = quotationDetail.ParamCostCenter.TotalAmmount,
                            CreatedBy = param.CreatedBy,
                            CreatedTime = param.CreatedTime
                        }), commandType: CommandType.Text, transaction: tran));
                    }
                    else
                    {
                        await Task.FromResult(await db.QueryAsync<int>(queryInsertPRFVendorQuotationCostCenter, new DynamicParameters(new
                        {
                            PRFVendorQuotationDetailId = resultQuotationDetailId,
                            CostCenterId = quotationDetail.ParamCostCenter.CostCenterId,
                            L_Currency_Code = quotationDetail.ParamCostCenter.L_Currency_Code,
                            Percentage = quotationDetail.ParamCostCenter.Percentage,
                            TotalAmmount = quotationDetail.ParamCostCenter.TotalAmmount,
                            Remarks = quotationDetail.ParamCostCenter.Remarks,
                            CreatedBy = param.CreatedBy,
                            CreatedTime = param.CreatedTime
                        }), commandType: CommandType.Text, transaction: tran));
                    }
                    #endregion

                    #region PRFVendorQuotationOtherCost
                    foreach (var otherCost in quotationDetail.ParamOtherCost)
                    {
                        if (otherCost.OtherCostId != null && otherCost.OtherCostId > 0)
                        {
                            await Task.FromResult(await db.QueryAsync<int>(queryUpdatePRFVendorQuotationOtherCost, new DynamicParameters(new
                            {
                                OtherCostId = otherCost.OtherCostId,
                                VATPercentage = otherCost.VATPercentage,
                                OtherCostAmount = otherCost.OtherCostAmount,
                                CreatedBy = param.CreatedBy,
                                CreatedTime = param.CreatedTime
                            }), commandType: CommandType.Text, transaction: tran));
                        }
                        else
                        {
                            await Task.FromResult(await db.QueryAsync<int>(queryInsertPRFVendorQuotationOtherCost, new DynamicParameters(new
                            {
                                PRFVendorQuotationDetailId = resultQuotationDetailId,
                                MtOtherCostCode = otherCost.MtOtherCostCode,
                                L_Currency_Code = otherCost.L_Currency_Code,
                                Included = otherCost.Included,
                                VATPercentage = otherCost.VATPercentage,
                                OtherCostAmount = otherCost.OtherCostAmount,
                                Remarks = otherCost.Remarks,
                                otherCost.OtherCost_SubCategoryId,
                                CreatedBy = param.CreatedBy,
                                CreatedTime = param.CreatedTime
                            }), commandType: CommandType.Text, transaction: tran));
                        }
                    }
                    #endregion
                }

                await DeletePRFVendorQuotationTOP((int)param.PRFVendorQuotationId);
                foreach (var topItem in param.TOPRequests)
                {
                    await UpdatePRFVendorQuotationTOP(param, topItem);
                }
                #endregion

                tran.Commit();
                result.error_id = 200;
                result.error_msg = "Success";
                return result;

            }
            catch (Exception ex)
            {
                tran.Rollback();
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
            finally
            {
                if (db.State == ConnectionState.Open)
                {
                    db.Close();
                }
            }
        }
        private async Task UpdatePRFVendorQuotationTOP(UpdateVendorQuotation request, SavePRFVendorQuotationTOPRequest topRequest)
        {
            try
            {
                var PaymentDate = DateTime.ParseExact(request.EstimatedDeliveryDate, "MM/dd/yyyy", null).AddDays((int)topRequest.TOPDays);

                await Task.FromResult(_iDapper.Execute(SaveVendorQuotationQuery.InsertPRFVendorQuotationTOP, new DynamicParameters(new
                {
                    request.PRFVendorQuotationId,
                    topRequest.TOPType_SubCategoryId,
                    topRequest.Percentage,
                    topRequest.TOPDays,
                    topRequest.PaymentAmount,
                    PaymentDate,
                    topRequest.PaymentMethod,
                    Status = 1,
                    CreatedTime = DateTime.Now,
                    request.CreatedBy,
                    topRequest.VendorId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task<int> DeleteQuotationVendor(DeleteQuotationVendor param)
        {
            try
            {
                string query = $@"
-- gara2 revise , kalo delete malah kena error FK
-- DELETE FROM PRFVendorQuotationCostCenter WHERE PRFVendorQuotationDetailId = @PRFVendorQuotationDetailId
-- DELETE FROM PRFVendorQuotationOtherCost WHERE PRFVendorQuotationDetailId = @PRFVendorQuotationDetailId
-- DELETE FROM PRFVendorQuotationDetail where Id = @PRFVendorQuotationDetailId
update pvqd set pvqd.Status = 0 from PRFVendorQuotationDetail as pvqd where pvqd.Id = @PRFVendorQuotationDetailId
";
                return await Task.FromResult(_iDapper.Insert<int>(query, new(param), commandType: CommandType.Text));
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }
        public async Task<ApprovalProcSumGroup> GetApproverProcSum(string prfNumber)
        {
            try
            {
                var result = new ApprovalProcSumGroup();
                result.ApprovalGroupNonBudgetProcSum = new ApprovalGroupNonBudgetProcSum();
                result.ApprovalProcurementSeniorManagerProcSum = new ApprovalGroupBudgetProcSum();
                result.ApprovalHeadOfProcurementProcSum = new ApprovalGroupBudgetProcSum();
                result.ApprovalRequesterProcSum = new ApprovalGroupBudgetProcSum();
                result.ApprovalAdditionalProcSum = new ApprovalGroupBudgetProcSum();
                result.ApprovalHeadOfUserProcSum = new ApprovalGroupBudgetProcSum();
                result.ApprovalDAPRequesterProcSum = new ApprovalGroupDAPProcSum();
                result.ApprovalCreatorProcSum = new ApprovalGroupBudgetProcSum();

                //Get Approval Flow
                string queryGetApprovalFlow = @"SELECT  SC.Id [ApprovalGroupSubCategoryId],
		                                                AFD.Sequence [Sequence]
                                                FROM dbo.ApprovalFlow AF
                                                JOIN dbo.ApprovalFlowDetail AFD ON AFD.ApprovalFlowID = AF.Id
                                                JOIN dbo.SubCategory SC ON SC.Id = AFD.ApprovalGroup_SubCategoryId
                                                WHERE AF.Name LIKE '%flowprocsum%'
                                                ORDER BY AFD.Sequence ASC";

                var resultApprovalFlowList = await Task.FromResult(_iDapper.GetAll<ApprovalGroupBudgetProcSum>(queryGetApprovalFlow, null, commandType: CommandType.Text));

                //Approver Non Budget
                var approvalFlowSequence1 = resultApprovalFlowList.SingleOrDefault(x => x.Sequence == 1);
                result.ApprovalGroupNonBudgetProcSum.Sequence = approvalFlowSequence1 is not null ? approvalFlowSequence1.Sequence : 1;
                result.ApprovalGroupNonBudgetProcSum.ApprovalGroupSubCategoryId = approvalFlowSequence1 is not null ? approvalFlowSequence1.ApprovalGroupSubCategoryId : 0;

                string queryGetApproverNonBudget = @"
					SELECT AG.CostCenterId [CostCenterId],
							CC2.NAME [CostCenterName],
							CC2.BusinessUnitId [BusinessUnitId],
							BU2.NAME [BusinessUnitName],
							AG.UserName [Username],
							AG.AccountId [AccountId],
							(AG.Level) [Level],
							AG.Sequence [Sequence]
					FROM dbo.ApprovalGroup AG
					JOIN dbo.CostCenter CC2 ON CC2.Id = AG.CostCenterId
					JOIN dbo.BusinessUnit BU2 ON BU2.Id = CC2.BusinessUnitId
					WHERE ApprovalGroup_SubCategoryId = @ApprovalGroupSubCategoryId AND AG.STATUS = 1
					ORDER BY Level ASC, Sequence ASC
				";

                var resultApproverNonBudget = await Task.FromResult(_iDapper.GetAll<ApprovalNoBudgetModel>(queryGetApproverNonBudget, new DynamicParameters(new
                {
                    ApprovalGroupSubCategoryId = result.ApprovalGroupNonBudgetProcSum.ApprovalGroupSubCategoryId
                }), commandType: CommandType.Text));

                if (resultApproverNonBudget.Count > 0)
                {
                    result.ApprovalGroupNonBudgetProcSum.ApprovalNonBudgetMemberList = resultApproverNonBudget;
                }

                //Approver Procurement Senior Manager
                var approvalFlowSequence2 = resultApprovalFlowList.SingleOrDefault(x => x.Sequence == 2);
                result.ApprovalProcurementSeniorManagerProcSum.Sequence = approvalFlowSequence2 is not null ? approvalFlowSequence2.Sequence : 2;
                result.ApprovalProcurementSeniorManagerProcSum.ApprovalGroupSubCategoryId = approvalFlowSequence2 is not null ? approvalFlowSequence2.ApprovalGroupSubCategoryId : 0;

                string queryGetApproverProcurementSeniorManager = @"SELECT AG.CostCenterId [CostCenterId]
		                                                                    ,CC.NAME [CostCenterName]
		                                                                    ,CC.BusinessUnitId [BusinessUnitId]
		                                                                    ,BU.NAME [BusinessUnitName]
		                                                                    ,AG.Level [Level]
		                                                                    ,AG.UserName [Username]
		                                                                    ,AG.AccountId [AccountId]
		                                                                    ,AG.Sequence [Sequence]
                                                                    FROM dbo.ApprovalGroup AG
                                                                    JOIN dbo.CostCenter CC ON CC.Id = AG.CostCenterId
                                                                    join DBO.BusinessUnit BU ON BU.Id = CC.BusinessUnitId
                                                                    WHERE ApprovalGroup_SubCategoryId = @ApprovalGroupSubCategoryId AND AG.STATUS = 1
                                                                    ORDER BY AG.Level ASC";

                var resultApproverProcurementSeniorManager = await Task.FromResult(_iDapper.GetAll<ApprovalGroupModel>(queryGetApproverProcurementSeniorManager, new DynamicParameters(new
                {
                    ApprovalGroupSubCategoryId = result.ApprovalProcurementSeniorManagerProcSum.ApprovalGroupSubCategoryId
                }), commandType: CommandType.Text));

                if (resultApproverProcurementSeniorManager.Count > 0)
                {
                    result.ApprovalProcurementSeniorManagerProcSum.ApprovalMemberList = resultApproverProcurementSeniorManager;
                }

                //Approver HOP
                var approvalFlowSequence3 = resultApprovalFlowList.SingleOrDefault(x => x.Sequence == 3);
                result.ApprovalHeadOfProcurementProcSum.Sequence = approvalFlowSequence3 is not null ? approvalFlowSequence3.Sequence : 3;
                result.ApprovalHeadOfProcurementProcSum.ApprovalGroupSubCategoryId = approvalFlowSequence3 is not null ? approvalFlowSequence3.ApprovalGroupSubCategoryId : 0;

                string queryGetApproverHeadOfProcurement = @"SELECT AG.CostCenterId [CostCenterId]
		                                                            ,CC.Name [CostCenterName]
		                                                            ,CC.BusinessUnitId [BusinessUnitId]
		                                                            ,BU.NAME [BusinessUnitName]
		                                                            ,AG.Level [Level]
		                                                            ,AG.AccountId [AccountId]
		                                                            ,AG.UserName [Username]
		                                                            ,AG.Sequence [Sequence]
                                                            FROM dbo.ApprovalGroup AG
                                                            JOIN dbo.CostCenter CC ON CC.Id = AG.CostCenterId
                                                            JOIN dbo.BusinessUnit BU ON BU.Id = CC.BusinessUnitId
                                                            WHERE AG.ApprovalGroup_SubCategoryId = @ApprovalGroupSubCategoryId AND AG.STATUS = 1";

                var resultApproverHeadOfProcurement = await Task.FromResult(_iDapper.GetAll<ApprovalGroupModel>(queryGetApproverHeadOfProcurement, new DynamicParameters(new
                {
                    ApprovalGroupSubCategoryId = result.ApprovalHeadOfProcurementProcSum.ApprovalGroupSubCategoryId
                }), commandType: CommandType.Text));

                if (resultApproverHeadOfProcurement.Count > 0)
                {
                    result.ApprovalHeadOfProcurementProcSum.ApprovalMemberList = resultApproverHeadOfProcurement;
                }

                //Approver Requester
                var approvalFlowSequence4 = resultApprovalFlowList.SingleOrDefault(x => x.Sequence == 4);
                result.ApprovalRequesterProcSum.Sequence = approvalFlowSequence4 is not null ? approvalFlowSequence4.Sequence : 4;
                result.ApprovalRequesterProcSum.ApprovalGroupSubCategoryId = approvalFlowSequence4 is not null ? approvalFlowSequence4.ApprovalGroupSubCategoryId : 0;

                string queryGetApproverRequester = @"SELECT CC.Id [CostCenterId]
		                                                        ,CC.Name [CostCenterName]
		                                                        ,BU.Id [BusinessUnitId]
		                                                        ,BU.Name [BusinessUnitName]
		                                                        ,1 [Level]
		                                                        ,PRF.RequestorAccountId [AccountId]
		                                                        ,PRF.RequestorUserName [Username]
		                                                        ,1 [Sequence]
                                                        FROM dbo.PRF PRF
                                                        JOIN Flips.UserAccount UA ON UA.Id = PRF.RequestorAccountId
                                                        JOIN dbo.CostCenter CC ON CC.Id = UA.CostCenterId
                                                        JOIN dbo.BusinessUnit BU ON BU.Id = CC.BusinessUnitId
                                                        WHERE PRF.PRFNo = @PRFNumber";

                var resultApproverRequester = await Task.FromResult(_iDapper.GetAll<ApprovalGroupModel>(queryGetApproverRequester, new DynamicParameters(new
                {
                    PRFNumber = prfNumber
                }), commandType: CommandType.Text));

                if (resultApproverRequester.Count > 0)
                {
                    result.ApprovalRequesterProcSum.ApprovalMemberList = resultApproverRequester;
                }

                //Approver Additional
                var approvalFlowSequence5 = resultApprovalFlowList.SingleOrDefault(x => x.Sequence == 5);
                result.ApprovalAdditionalProcSum.Sequence = approvalFlowSequence5 is not null ? approvalFlowSequence5.Sequence : 5;
                result.ApprovalAdditionalProcSum.ApprovalGroupSubCategoryId = approvalFlowSequence5 is not null ? approvalFlowSequence5.ApprovalGroupSubCategoryId : 0;

                //Approver Head Of User
                var approvalFlowSequence6 = resultApprovalFlowList.SingleOrDefault(x => x.Sequence == 6);
                result.ApprovalHeadOfUserProcSum.Sequence = approvalFlowSequence6 is not null ? approvalFlowSequence6.Sequence : 6;
                result.ApprovalHeadOfUserProcSum.ApprovalGroupSubCategoryId = approvalFlowSequence6 is not null ? approvalFlowSequence6.ApprovalGroupSubCategoryId : 0;

                string queryGetApproverHeadOfUser = @"SELECT AG.CostCenterId [CostCenterId]
		                                                    ,CC.NAME [CostCenterName]
		                                                    ,CC.BusinessUnitId [BusinessUnitId]
		                                                    ,BU.NAME [BusinessUnitName]
		                                                    ,AG.Level [Level] 
		                                                    ,AG.AccountId [AccountId]
		                                                    ,AG.UserName [UserName]
		                                                    ,AG.Sequence [Sequence]
                                                    FROM dbo.ApprovalGroup AG
                                                    JOIN dbo.CostCenter CC ON CC.Id = AG.CostCenterId
                                                    JOIN dbo.BusinessUnit BU ON BU.Id = CC.BusinessUnitId
                                                    JOIN dbo.PRF PRF ON PRF.CostCenterId = AG.CostCenterId
                                                    WHERE AG.ApprovalGroup_SubCategoryId = @ApprovalGroupSubCategoryId AND PRF.PRFNo = @PRFNumber AND AG.STATUS = 1";

                var resultApproverHeadOfUser = await Task.FromResult(_iDapper.GetAll<ApprovalGroupModel>(queryGetApproverHeadOfUser, new DynamicParameters(new
                {
                    PRFNumber = prfNumber,
                    ApprovalGroupSubCategoryId = result.ApprovalHeadOfUserProcSum.ApprovalGroupSubCategoryId
                }), commandType: CommandType.Text));

                if (resultApproverHeadOfUser.Count > 0)
                {
                    result.ApprovalHeadOfUserProcSum.ApprovalMemberList = resultApproverHeadOfUser;
                }

                //Approver DAP
                var approvalFlowSequence7 = resultApprovalFlowList.SingleOrDefault(x => x.Sequence == 7);
                result.ApprovalDAPRequesterProcSum.Sequence = approvalFlowSequence7 is not null ? approvalFlowSequence7.Sequence : 7;
                result.ApprovalDAPRequesterProcSum.ApprovalGroupSubCategoryId = approvalFlowSequence7 is not null ? approvalFlowSequence7.ApprovalGroupSubCategoryId : 0;

                string queryGetApproverDAPRequester = @"
declare @RateAmount as money = (select top(1) bt.RateAmount from BudgetTransaction as bt where bt.RefNumber = @PRFNumber)

                        select
                        1 as x
                        , PRF.CostCenterId
                        , CC.NAME as CostCenterName
                        , CC.BusinessUnitId
                        , BU.NAME as BusinessUnitName
                        , AG.Level
                        , AG.AccountId
                        , AG.UserName
                        , AG.Sequence
                        , PVQCC.GrandTotalAmount as TotalAmount
                        , 'DAP Requester' as ItemDescription
                        , 0 as PRFVendorQuotationDetailId
                        , PRF.L_Currency_Code

                        FROM PRF as PRF
                        JOIN CostCenter as CC ON CC.Id = PRF.CostCenterId
                        JOIN BusinessUnit as BU ON BU.Id = CC.BusinessUnitId
                        JOIN PRFVendorQuotation as PVQ ON PVQ.PRFId = PRF.Id
                        JOIN ApprovalGroup as AG ON AG.CostCenterId = PRF.CostCenterId
                        OUTER APPLY (
	                        SELECT SUM(PVQCC.TotalAmmount) as GrandTotalAmount
	                        FROM PRFVendorQuotationDetail as PVQD
	                        JOIN PRFVendorQuotationCostCenter as PVQCC ON PVQCC.PRFVendorQuotationDetailId = PVQD.Id AND PVQD.IsSelected = 1
	                        WHERE PVQD.PRFVendorQuotationId = PVQ.Id and PVQD.Status != 0
                        ) as PVQCC

                        WHERE PRF.PRFNo = @PRFNumber
                        AND AG.ApprovalGroup_SubCategoryId = @ApprovalGroupSubCategoryId
                        AND AG.STATUS = 1
                        AND (PVQCC.GrandTotalAmount * @RateAmount) >= AG.MinAmount

                        ORDER BY
                        AG.Level
                        , AG.Sequence ASC
                        ";

                var resultApproverDAPRequester = await Task.FromResult(_iDapper.GetAll<ApprovalQuotationDetailGroup>(queryGetApproverDAPRequester, new DynamicParameters(new
                {
                    PRFNumber = prfNumber,
                    ApprovalGroupSubCategoryId = result.ApprovalDAPRequesterProcSum.ApprovalGroupSubCategoryId
                }), commandType: CommandType.Text));

                if (resultApproverDAPRequester.Count > 0)
                {
                    result.ApprovalDAPRequesterProcSum.ApprovalMemberList = resultApproverDAPRequester;
                }

                //Approver Creator
                var approvalFlowSequence8 = resultApprovalFlowList.SingleOrDefault(x => x.Sequence == 8);
                result.ApprovalCreatorProcSum.Sequence = approvalFlowSequence8 is not null ? approvalFlowSequence8.Sequence : 8;
                result.ApprovalCreatorProcSum.ApprovalGroupSubCategoryId = approvalFlowSequence8 is not null ? approvalFlowSequence8.ApprovalGroupSubCategoryId : 0;

                return result;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task<string> CancleRequest(PurchaseRequestFormModel.PurchaseRequestForm param)
        {
            try
            {
                string queryCancelRequest = @"DECLARE @ResultMessage NVARCHAR(100);
                                                BEGIN TRY
                                                    UPDATE dbo.PRF
                                                    SET Status = (SELECT MT.ValueId FROM dbo.MasterTable MT WHERE Category = 'PRF.Status' AND MT.Name = 'cancel')
                                                        ,LastUpdatedBy = @LastUpdatedBy
                                                        ,LastUpdatedTime = GETDATE()
                                                        ,ReasonCancel = @ReasonCancel
                                                    WHERE PRFNo = @PRFNumber

                                                    SET @ResultMessage = 'Cancel Request Success';
                                                END TRY
                                                BEGIN CATCH
                                                    SET @ResultMessage = 'Cancel Request Failed';
                                                END CATCH

                                                SELECT @ResultMessage AS ResultMessage;";

                string resultCancel = await Task.FromResult(_iDapper.Update<string>(queryCancelRequest, new DynamicParameters(new
                {
                    PRFNumber = param.PRFNumber,
                    LastUpdatedBy = param.LastUpdatedBy,
                    ReasonCancel = param.ReasonCancel
                }), commandType: CommandType.Text));

                if (resultCancel.Contains("success", StringComparison.CurrentCultureIgnoreCase))
                {
                    //balikin budget
                    await _budgetTransactionRepository.InsertBudgetTransactionByRequestType("prf", param.PRFNumber, param.LastUpdatedBy, false);
                }
                await _nonShopNotificationRepository.SendEmailPRFCancel(param.PRFNumber, "Cancel");

                return resultCancel;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task<string> ReviseRequest(PurchaseRequestFormModel.PurchaseRequestForm prf)
        {
            try
            {
                string query = @"declare @DateNow datetime = getdate();
                                    begin try
                                     declare @PRFId bigint ;
                                     select top 1 @PRFId = PRF.Id from PRF as PRF where PRF.PRFNo = @PRFNumber ;

                                     update dbo.PRF 
                                     set LastUpdatedTime = @DateNow
                                      ,LastUpdatedBy = @ReviseBy
                                      ,Status = (select top 1 mt.ValueId from MasterTable as mt where mt.Category = 'PRF.Status' and mt.Code = 'P-4') -- revise
                                      ,RequestReviseDate = @DateNow
                                      ,ReasonRevise = @ReasonRevise
                                            ,ReviseBy = @ReviseBy
                                            ,ReviseTo = RequestorUserName
                                      ,ApprovalRequestId = null
                                      ,BuyerAccountId = null
                                      ,BuyerUserName = null
                                      ,BuyerEmail = null
                                      ,TypeProcess_SubCategory = null
                                     where PRF.Id = @PRFId ;

                                     update dbo.PRFDetail
                                     set ApprovalRequestItemId = null 
                                     where PRFId = @PRFId ;

                                     update dbo.PRFVendorCandidate 
                                     set Status = 0 
                                     where PRFId = @PRFId ;

                                     update dbo.PRFVendorQuotation
                                     set Status = 0 
                                     where PRFId = @PRFId ;

                                     update dbo.PRFVendorQuotationDetail
                                     set Status = 0 
                                     where PRFVendorQuotationId in (select PVQ.Id from dbo.PRFVendorQuotation as PVQ where PVQ.PRFId = @PRFId)

                                     update dbo.PRFSummary 
                                     set Status = 0 
                                     where PRFId = @PRFId ;

                                     select 'Revise Request Success' ;
                                    end try

                                    begin catch
                                     select 'Revise Request Failed' ;
                                    end catch";
                DynamicParameters parms = new(new
                {
                    prf.PRFNumber,
                    prf.ReviseBy,
                    prf.ReasonRevise
                });
                string reviseResponse = await Task.FromResult(_iDapper.Update<string>(query, parms, commandType: CommandType.Text));
                if (reviseResponse.Contains("success", StringComparison.CurrentCultureIgnoreCase))
                {
                    //balikin budget
                    await _budgetTransactionRepository.InsertBudgetTransactionByRequestType(
                        "prf",
                        prf.PRFNumber,
                        prf.LastUpdatedBy,
                        false);
                }

                await _nonShopNotificationRepository.SendEmailPRFCancel(prf.PRFNumber, "Revision");
                return reviseResponse;
            }
            catch (Exception exception)
            {
                throw new GlobalExceptions(nameof(ProcurmentBuyerRepository), exception.InnerException);
            }
        }
        public async Task<List<AttachmentModel>> GetListAttachmentProcurementBuyer(
            string prfNumber,
            int? prfId = null,
            int? prfSummaryId = null,
            int? poNonShoppingId = null)
        {
            try
            {
                string q = $@"SELECT ATH.Id,
								   ATH.RefId,
								   ATH.Category,
								   ATH.RelatedTableName,
								   ATH.FullPath,
								   ATH.Description,
								   ATH.OriginalFileName,
								   ATH.CreatedBy,
								   ATH.CreatedTime,
								   ATH.LastUpdatedBy,
								   ATH.LastUpdatedTime,
								   (
									   SELECT CASE
												  WHEN Id IS NOT NULL THEN
													  '1'
												  ELSE
													  '0'
											  END
									   FROM PRFVendorQuotation PVQ
									   WHERE PVQ.PRFId = PRF.Id
								   ) Checksum
							FROM PRF PRF
								JOIN Attachment ATH
									ON PRF.Id = ATH.RefId
							WHERE PRF.PRFNo = @PRFNo
								  AND ATH.Category IN ( 'PurchaseRequestForm', 'QuotationFormVendor', 'ProcurementSummary', 'GR', 'GR_Generate')
							UNION ALL
							SELECT ATH.Id,
								   ATH.RefId,
								   ATH.Category,
								   ATH.RelatedTableName,
								   ATH.FullPath,
								   ATH.Description,
								   ATH.OriginalFileName,
								   ATH.CreatedBy,
								   ATH.CreatedTime,
								   ATH.LastUpdatedBy,
								   ATH.LastUpdatedTime,
								   (
									   SELECT CASE
												  WHEN Id IS NOT NULL THEN
													  '1'
												  ELSE
													  '0'
											  END
									   FROM PRFVendorQuotation PVQ
									   WHERE PVQ.PRFId = PRF.Id
								   ) Checksum
							FROM PRF PRF
								JOIN PRFSummary PFS
									ON PRF.Id = PFS.PRFId
								LEFT JOIN PONonShopping PO
									ON PO.PRFSummaryId = PFS.Id
								JOIN Attachment ATH
									ON (
										   ATH.RefId = PO.Id
										   AND ATH.Category IN ( 'PO', 'INV')
										   AND ATH.Description IN('Non-ShoppingCart', 'Non Shopping Cart')
									   )
							WHERE PRF.PRFNo = @PRFNo
							UNION ALL
							SELECT ATH.Id,
								   ATH.RefId,
								   ATH.Category,
								   ATH.RelatedTableName,
								   ATH.FullPath,
								   ATH.Description,
								   ATH.OriginalFileName,
								   ATH.CreatedBy,
								   ATH.CreatedTime,
								   ATH.LastUpdatedBy,
								   ATH.LastUpdatedTime,
								   (
									   SELECT CASE
												  WHEN Id IS NOT NULL THEN
													  '1'
												  ELSE
													  '0'
											  END
									   FROM PRFVendorQuotation PVQ
									   WHERE PVQ.PRFId = PRF.Id
								   ) Checksum
							FROM PRF PRF
								JOIN PRFSummary PFS
									ON PRF.Id = PFS.PRFId
								JOIN PONonShopping PO
									ON PO.PRFSummaryId = PFS.Id
								JOIN PONonShoppingDetail PSD
									ON PO.Id = PSD.PONonShoppingId
								JOIN DeliveryNotesDetail DND
									ON DND.PurchaseOrderDetailId = PSD.Id
								JOIN Attachment ATH
									ON (
										   ATH.RefId = DND.Id
										   AND ATH.Category IN ( 'DN' )
										   AND ATH.Description = 'NonShoppingCart'
									   )
							WHERE PRF.PRFNo = @PRFNo";

                DynamicParameters p = new(new
                {
                    PRFNo = prfNumber,
                    PRFId = prfId,
                    PRFSummaryId = prfSummaryId,
                    PONonShoppingId = poNonShoppingId
                });
                var attachments = await Task.FromResult(_iDapper.GetAll<AttachmentModel>(q, p, commandType: CommandType.Text));

                if (attachments.Count > 0)
                {
                    double fileLengthBytes;
                    foreach (var a in attachments)
                    {
                        try
                        {
                            var itemFullPath = await File.ReadAllBytesAsync(a.FullPath);
                            fileLengthBytes = Convert.ToDouble(itemFullPath.Length);
                            a.FileSize = await Task.FromResult<string>(FileHelper.FormatFileSize(fileLengthBytes));
                        }
                        catch
                        {
                            a.FileSize = await Task.FromResult<string>(FileHelper.FormatFileSize(0));
                        }
                    }
                }

                return attachments;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(objectName, e.InnerException);
            }
        }

        public async Task<PRFVendorEnhancedViewGetResponseModel> PRFVendorEnhancedViewGet(long prfId)
        {
            try
            {
                CommandType ct = CommandType.Text;
                string q = @"
--declare @PRFId int = 789

select json_query((
	select json_query((
		select
		p.Id
		, json_query((
			select
			pve.Id
			, pve.Status
			, json_query((
				select
				pved.Id
				, pved.Status
				, pved.AttachmentId
				, pved.RationalOfRisk
				, pved.DocType_SubCategoryId
				, pved.Remarks
				, json_query((
					select
					sc.Id
					, sc.SubCategoryCode
					, sc.SubCategoryName
					, sc.Description
					, sc.Sequence
					for json path , include_null_values , without_array_wrapper
				)) as 'SubCategory'
				from Category as c
				inner join SubCategory as sc on sc.CategoryId = c.Id
				left join PRFVendorEnhancedDetail as pved on pved.PRFVendorEnhancedId = pve.Id and pved.DocType_SubCategoryId = sc.Id
				where c.CategoryCode = 'CA-2023-07-00068'
				for json path , include_null_values
			)) as 'PRFVendorEnhancedDetailList'
			, json_query(
                coalesce(
                    (
				        select
				        a.Id
				        , a.Description
				        , a.OriginalFileName
                        , convert(varchar, CreatedTime, 20) as CreatedTimeText
				        from Attachment as a
				        where a.RelatedTableName = 'PRFVendorEnhanced'
				        and a.RefId = pve.Id
				        for json path , include_null_values
                    )
                , '[]')
            ) as 'AttachmentList'
			for json path , include_null_values , without_array_wrapper
		)) as 'PRFVendorEnhanced'
		from PRF as p
		left join PRFVendorEnhanced as pve on pve.PRFId = p.Id
		where p.Id = @PRFId
		for json path , include_null_values , without_array_wrapper
	)) as 'PRF'
	for json path , include_null_values , without_array_wrapper
)) as 'PRFVendorEnhancedView'
";
                DynamicParameters p = new(new
                {
                    PRFId = prfId
                });
                string json = await Task.FromResult(_iDapper.Get<string>(q, p, ct));
                var r = JsonConvert.DeserializeObject<PRFVendorEnhancedViewGetResponseModel>(json);
                return r;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ProcurmentBuyerRepository), e);
            }
        }
        public async Task<PRFVendorEnhancedPostRequestModel> PRFVendorEnhancedPost(PRFVendorEnhancedPostRequestModel request)
        {
            using IDbConnection db = _iDapper.GetDbconnection();
            if (db.State == ConnectionState.Closed)
                db.Open();
            using IDbTransaction t = db.BeginTransaction();
            CommandType ct = CommandType.Text;

            try
            {
                // PRFVendorEnhanced
                string pveQ = @"";

                if (request.PRF.PRFVendorEnhanced.Id is null)
                {
                    pveQ = @"
						insert into PRFVendorEnhanced
						( PRFId
						, Status
						, CreatedTime
						, CreatedBy
						, LastUpdatedTime
						, LastUpdatedBy
						)

						output
						inserted.*

						select top 1
						  @PRFId as PRFId
						, 1 as Status
						, getdate() as CreatedTime
						, @CreatedBy as CreatedBy
						, null as LastUpdatedTime
						, null as LastUpdatedBy
						";
                    DynamicParameters pveP = new(new
                    {
                        PRFId = request.PRF.Id
                        ,
                        request.PRF.PRFVendorEnhanced.CreatedBy
                        ,
                        PRFVendorEnhancedId = request.PRF.PRFVendorEnhanced.Id
                    });
                    var pve = await Task.FromResult(db.Query<PRFVendorEnhancedPostRequestModel.PRFVendorEnhancedModel>(pveQ, pveP, t, true, null, ct).FirstOrDefault());
                    request.PRF.PRFVendorEnhanced.Id = pve.Id;
                }
                else
                {
                    pveQ = @"
						update pve set
						pve.LastUpdatedTime = getdate()
						, pve.LastUpdatedBy = @CreatedBy
						from PRFVendorEnhanced as pve
						where pve.Id = @PRFVendorEnhancedId

						select
						  pve.PRFId
						, pve.Status
						, pve.CreatedTime
						, pve.CreatedBy
						, pve.LastUpdatedTime
						, pve.LastUpdatedBy
						from PRFVendorEnhanced as pve
						where pve.Id = @PRFVendorEnhancedId
						";
                    DynamicParameters pveP = new(new
                    {
                        PRFId = request.PRF.Id
                        ,
                        request.PRF.PRFVendorEnhanced.CreatedBy
                        ,
                        PRFVendorEnhancedId = request.PRF.PRFVendorEnhanced.Id
                    });
                    await Task.FromResult(db.Query<PRFVendorEnhancedPostRequestModel.PRFVendorEnhancedModel>(pveQ, pveP, t, true, null, ct).FirstOrDefault());
                }

                string aQ = @"
update a set
a.RefId = @PRFVendorEnhancedId
from Attachment as a
where a.Id = @AttachmentId
";
                foreach (var a in request.PRF.PRFVendorEnhanced.AttachmentList)
                {
                    DynamicParameters aP = new(new
                    {
                        AttachmentId = a.Id
                        ,
                        PRFVendorEnhancedId = request.PRF.PRFVendorEnhanced.Id
                    });
                    _ = await Task.FromResult(await db.ExecuteAsync(aQ, aP, t, null, ct));
                }

                // PRFVendorEnhancedDetail

                string pvedVal = @"SELECT Id FROM 
									PRFVendorEnhancedDetail
									WHERE PRFVendorEnhancedId = @PRFVendorEnhancedId
										AND DocType_SubCategoryId = @DocType_SubCategoryId";
                string pvedQ = @"
insert into PRFVendorEnhancedDetail
( PRFVendorEnhancedId
, Status
, CreatedTime
, CreatedBy
, LastUpdatedBy
, LastUpdatedTime
, DocType_SubCategoryId
, Remarks
, AttachmentId
, RationalOfRisk
)

output
inserted.*

select top 1
  @PRFVendorEnhancedId as PRFVendorEnhancedId
, 1 as Status
, getdate() as CreatedTime
, @CreatedBy as CreatedBy
, null as LastUpdatedBy
, null as LastUpdatedTime
, @DocType_SubCategoryId as DocType_SubCategoryId
, @Remarks as Remarks
, @AttachmentId as AttachmentId
, @RationalOfRisk as RationalOfRisk
";
                string pvedUpdateQ = @"
update pved set
  pved.LastUpdatedBy = @CreatedBy
, pved.LastUpdatedTime = getdate()
, pved.DocType_SubCategoryId = @DocType_SubCategoryId
, pved.Remarks = @Remarks
, pved.AttachmentId = @AttachmentId
, pved.RationalOfRisk = @RationalOfRisk
from PRFVendorEnhancedDetail as pved
where pved.PRFVendorEnhancedId = @PRFVendorEnhancedId
	AND pved.DocType_SubCategoryId = @DocType_SubCategoryId

select
TOP 1
  pved.Id
, pved.Status
, pved.CreatedTime
, pved.CreatedBy
, pved.LastUpdatedBy
, pved.LastUpdatedTime
, pved.DocType_SubCategoryId
, pved.Remarks
, pved.AttachmentId
, pved.RationalOfRisk
from PRFVendorEnhancedDetail as pved
where pved.PRFVendorEnhancedId = @PRFVendorEnhancedId
	AND pved.DocType_SubCategoryId = @DocType_SubCategoryId
";
                string adQ = @"
update a set
a.RefId = 0
from Attachment as a
where a.RefId = @PRFVendorEnhancedDetailId

update a set
a.RefId = @PRFVendorEnhancedDetailId
from Attachment as a
where a.Id = @AttachmentId
";
                foreach (var pved in request.PRF.PRFVendorEnhanced.PRFVendorEnhancedDetailList)
                {
                    DynamicParameters pvedPVal = new(new
                    {
                        PRFVendorEnhancedId = request.PRF.PRFVendorEnhanced.Id,
                        pved.DocType_SubCategoryId
                    });
                    int pvedId = await Task.FromResult(db.Query<int>(pvedVal, pvedPVal, t, true, null, ct).FirstOrDefault());

                    pvedQ = pvedId == 0 ? pvedQ : pvedUpdateQ;
                    DynamicParameters pvedP = new(new
                    {
                        PRFVendorEnhancedId = request.PRF.PRFVendorEnhanced.Id
                        ,
                        PRFVendorEnhancedDetailId = pved.Id
                        ,
                        pved.CreatedBy
                        ,
                        pved.DocType_SubCategoryId
                        ,
                        pved.Remarks
                        ,
                        pved.AttachmentId
                        ,
                        pved.RationalOfRisk
                    });
                    var pved2 = await Task.FromResult(db.Query<PRFVendorEnhancedPostRequestModel.PRFVendorEnhancedDetailModel>(pvedQ, pvedP, t, true, null, ct).FirstOrDefault());
                    pved.Id = pved2.Id;

                    DynamicParameters adP = new(new
                    {
                        pved.AttachmentId
                        ,
                        PRFVendorEnhancedDetailId = pved.Id
                    });
                    _ = await Task.FromResult(db.Query<PRFVendorEnhancedPostRequestModel.PRFVendorEnhancedModel>(adQ, adP, t, true, null, ct).FirstOrDefault());
                }

                t.Commit();
                return request;
            }
            catch (Exception e)
            {
                t.Rollback();
                throw new GlobalExceptions(nameof(ProcurmentBuyerRepository), e);
            }
            finally
            {
                if (db.State == ConnectionState.Open)
                    db.Close();
            }
        }

        #region CR
        public async Task<List<ViewItems>> ViewItem(string RequestNumber)
        {
            List<ViewItems> response = new List<ViewItems>();
            string query = $@"
                            SELECT 
	                            PFD.RequestItemName [RequestItemName] ,
	                            PFD.RequestItemNotes [DetailGoods],
	                            SCY.SubCategoryName [CategoryName],
	                            PFD.Qty [Quantity],
	                            PFD.Unit [UnitDesc],
	                            PFD.DeliveryNotes [DeliveryRequirements],
                                FORMAT(PFD.DeliveryRequestDate, 'dd-MMM-yyyy') [DeliveryRequestDate]
                            FROM PRF PRF
                            JOIN PRFDetail PFD ON PRF.Id = PFD.PRFId
                            JOIN SubCategory SCY ON PFD.TypeOfGoods_SubCategoryId = SCY.Id
                            WHERE PRF.PRFNo = @RequestNumber";
            response = await Task.FromResult(_iDapper.GetAll<ViewItems>(query, new DynamicParameters(new
            {
                RequestNumber = RequestNumber
            }), commandType: CommandType.Text));
            return response;
        }
        public async Task updateBuyerAccount(int prf_id, long buyer_account)
        {
            try
            {
                string query = $@"DECLARE @dateInserted DATETIME = GETDATE()
                                CREATE TABLE #buyer
                                (
                                    BuyerAccountId bigint,
                                    BuyerUserName varchar(100),
                                    BuyerEmail varchar(200)
                                )
                                INSERT INTO #buyer
                                (
                                    BuyerAccountId,
                                    BuyerUserName,
                                    BuyerEmail
                                )
                                SELECT Id as BuyerAccountId,
                                       UserName as BuyerUserName,
                                       Email as BuyerEmail
                                FROM Flips.UserAccount
                                WHERE Id = @BuyerAccountId

                                UPDATE PRF
                                SET LastUpdatedBy = buyer.BuyerUserName,
                                    LastUpdatedTime = @dateInserted,
                                    BuyerAccountId = buyer.BuyerAccountId,
                                    BuyerUserName = buyer.BuyerUserName,
                                    BuyerEmail = buyer.BuyerEmail
                                FROM #buyer buyer
                                WHERE Id = @PRFId

                                IF @@ROWCOUNT > 0
                                BEGIN
                                    SELECT 1
                                END
                                ELSE
                                BEGIN
                                   SELECT 0
                                    DROP TABLE #buyer
                                    RETURN;
                                END

                                DROP TABLE #buyer";
                await Task.FromResult(_iDapper.Insert<int>(query, new Dapper.DynamicParameters(new
                {
                    PRFId = prf_id,
                    BuyerAccountId = buyer_account
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task SendEmailNotificationBuyer(int prf_id)
        {
            try
            {
                NotificationModel notification = new();
                string query_ = $@"SELECT 
	                                PRF.RequestorEmail [RequestorEmail],
                                    PRF.PRFNo [PrfNumber],
	                                PRF.RequestorUserName [RequestorUserName],
	                                MTL.Name [RequestStatus],
									PRF.BuyerUserName,
									PRF.BuyerEmail
                                FROM PRF PRF
                                JOIN MasterTable MTL
	                                ON PRF.Status = MTL.ValueId
	                                AND MTL.Category = 'PRF.Status'
                                WHERE PRF.Id = @prf_id";

                notification.ParamNonShopCart = await Task.FromResult(_iDapper.Get<ParamEmailNonShopCart>(query_, new Dapper.DynamicParameters(new
                {
                    prf_id = prf_id
                }), commandType: CommandType.Text));

                List<string> email_cc = new List<string>()
                    {
                        notification.ParamNonShopCart.BuyerEmail
                    };
                notification.CcEmail = email_cc;
                notification.RequestType = "pick buyer";
                notification.RequestorEmail = notification.ParamNonShopCart.RequestorEmail;
                notification.RequestNumber = notification.ParamNonShopCart.PrfNumber;
                notification.ActionBy = notification.ParamNonShopCart.BuyerUserName;
                notification.StatusRequest = notification.ParamNonShopCart.RequestStatus;
                notification.RequestorName = notification.ParamNonShopCart.RequestorUserName;

                notification.SubjectEmail = $@"{notification.ParamNonShopCart.PrfNumber} Non-Shopping Cart has been picked by {notification.ParamNonShopCart.BuyerUserName} as the Procurement Buyer";
                await _notificationService.SendEmailNotificationForBuyer(notification);
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task<ApprovalHistoryProcSumHeader> ApprovalHistoryPAP(string RequestNumber)
        {
            ApprovalHistoryProcSumHeader response = new ApprovalHistoryProcSumHeader();
            try
            {
                string query = $@"SELECT Id FROM PRF WHERE PRFNo = @reqNumber";
                int prf_id = await Task.FromResult(_iDapper.Insert<int>(query, new Dapper.DynamicParameters(new
                {
                    reqNumber = RequestNumber
                }), commandType: CommandType.Text));

                string queryPRF = $@"SELECT
                                        (
                                            SELECT 'PRF' Types,
                                                   SCY.SubCategoryName,
                                                   JSON_QUERY(
                                                   (
                                                       SELECT ARGM.UserName [UserName],
                                                              MTL.Description [Status],
                                                              FORMAT(ARGM.ApprovalDate, 'dd MMM yyyy') [ApprovalDate],
															  ARGM.Comment
                                                       FROM PRF PRF
                                                           JOIN ApprovalRequest AR
                                                               ON PRF.ApprovalRequestId = AR.Id
                                                           JOIN ApprovalRequestGroupMember ARGM
                                                               ON AR.Id = ARGM.ApprovalRequestId
                                                                  AND ARGM.ApprovaGroup_SubCategoryId = SCY.Id
                                                           JOIN MasterTable MTL
                                                               ON ARGM.Status = MTL.ValueId
                                                                  AND MTL.Category = 'ApprovalRequest.Status'
                                                       WHERE PRF.Id = @prf_id
                                                       FOR JSON PATH
                                                   )) ApprovalMember
                                            FROM SubCategory SCY
                                            WHERE SCY.SubCategoryCode IN ( 'SC-2023-08-11141', 'Non-Shoppingcart-PRF-Non-Budget' )
                                            ORDER BY CASE
                                                         WHEN SCY.SubCategoryCode = 'Non-Shoppingcart-PRF-Non-Budget' THEN
                                                             0
                                                         ELSE
                                                             1
                                                     END,
                                                     SCY.Id ASC
                                            FOR JSON PATH, INCLUDE_NULL_VALUES
                                        ) [JsonResponses]";

                JsonResponse res_prf = await Task.FromResult(_iDapper.Get<JsonResponse>(queryPRF, new Dapper.DynamicParameters(new
                {
                    prf_id = prf_id
                }), commandType: CommandType.Text));

                string queryPAP = $@"SELECT
											(
												SELECT 'PAP' Types,
													   SCY.SubCategoryName,
													   SCY.SubCategoryCode,
													   SCY.Id,
													   JSON_QUERY(
													   (
														   SELECT ARGM.UserName,
																  MTL.Name [Status],
																  FORMAT(   (CASE
                                                                             WHEN MTL.Name = 'Approve' THEN
                                                                                  ARGM.ApprovalDate
                                                                             ELSE
                                                                                ARGM.LastUpdatedTime
                                                                         END
                                                                        ),
                                                                        'dd MMM yyyy hh:mm:ss'
                                                                    ) [ApprovalDate],
																  ARGM.Comment
														   FROM ApprovalRequestGroupMember ARGM
															   JOIN MasterTable MTL
																   ON ARGM.Status = MTL.ValueId
																	  AND MTL.Category = 'ApprovalRequest.Status'
														   WHERE ARGM.ApprovalRequestId = ARG.ApprovalRequestId
																 AND ARGM.ApprovaGroup_SubCategoryId = SCY.Id
														   FOR JSON PATH, INCLUDE_NULL_VALUES
													   )) ApprovalMember
												FROM PAP PAP
													JOIN PRF PRF
														ON PAP.PRFId = PRF.Id
													JOIN ApprovalRequestGroup ARG
														ON PAP.ApprovalRequestId = ARG.ApprovalRequestId
													JOIN SubCategory SCY
														ON ARG.ApprovalGroup_SubCategoryId = SCY.Id
												WHERE PRF.Id = @prf_id
												ORDER BY ARG.Sequence ASC
												FOR JSON PATH, INCLUDE_NULL_VALUES
											) [JsonResponses]";
                JsonResponse res_pap = await Task.FromResult(_iDapper.Get<JsonResponse>(queryPAP, new Dapper.DynamicParameters(new
                {
                    prf_id = prf_id
                }), commandType: CommandType.Text));

                response = new ApprovalHistoryProcSumHeader()
                {
                    PRF = JsonConvert.DeserializeObject<List<ApprovalHistoryProcSum>>(res_prf.JsonResponses),
                    PAP = JsonConvert.DeserializeObject<List<ApprovalHistoryProcSum>>(res_pap.JsonResponses)
                };

            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
            return response;
        }

        public async Task<ApprovalHistoryProcSumHeader> ApprovalHistoryProcsum(string RequestNumber)
        {
            ApprovalHistoryProcSumHeader response = new ApprovalHistoryProcSumHeader();

            try
            {
                string query = $@"SELECT Id FROM PRF WHERE PRFNo = @reqNumber";
                int prf_id = await Task.FromResult(_iDapper.Insert<int>(query, new Dapper.DynamicParameters(new
                {
                    reqNumber = RequestNumber
                }), commandType: CommandType.Text));

                string queryPRF = $@"SELECT
                                        (
                                            SELECT 'PRF' Types,
                                                   SCY.SubCategoryName,
                                                   JSON_QUERY(
                                                   (
                                                       SELECT ARGM.UserName [UserName],
                                                              MTL.Description [Status],
                                                              FORMAT(ARGM.ApprovalDate, 'dd MMM yyyy') [ApprovalDate],
															  ARGM.Comment
                                                       FROM PRF PRF
                                                           JOIN ApprovalRequest AR
                                                               ON PRF.ApprovalRequestId = AR.Id
                                                           JOIN ApprovalRequestGroupMember ARGM
                                                               ON AR.Id = ARGM.ApprovalRequestId
                                                                  AND ARGM.ApprovaGroup_SubCategoryId = SCY.Id
                                                           JOIN MasterTable MTL
                                                               ON ARGM.Status = MTL.ValueId
                                                                  AND MTL.Category = 'ApprovalRequest.Status'
                                                       WHERE PRF.Id = @prf_id
                                                       FOR JSON PATH
                                                   )) ApprovalMember
                                            FROM SubCategory SCY
                                            WHERE SCY.SubCategoryCode IN ( 'SC-2023-08-11141', 'Non-Shoppingcart-PRF-Non-Budget' )
                                            ORDER BY CASE
                                                         WHEN SCY.SubCategoryCode = 'Non-Shoppingcart-PRF-Non-Budget' THEN
                                                             0
                                                         ELSE
                                                             1
                                                     END,
                                                     SCY.Id ASC
                                            FOR JSON PATH, INCLUDE_NULL_VALUES
                                        ) [JsonResponses]";

                JsonResponse res_prf = await Task.FromResult(_iDapper.Get<JsonResponse>(queryPRF, new Dapper.DynamicParameters(new
                {
                    prf_id = prf_id
                }), commandType: CommandType.Text));

                string queryProcsum = $@";WITH CTE
                                        AS (SELECT 'Procsum' Types,
                                                   afd.Notes as SubCategoryName,
                                                   SCY.SubCategoryCode,
                                                   SCY.Id,
                                                   ROW_NUMBER() OVER (PARTITION BY SCY.SubCategoryCode ORDER BY PFS.CreatedTime DESC) AS Rn,
                                                   JSON_QUERY(
                                                   (
                                                       SELECT MEMBER2.*
                                                       FROM
                                                       (
                                                           SELECT ARGM.UserName,
                                                                  MTL.Name [Status],
                                                                  FORMAT(   (CASE
                                                                                 WHEN MTL.Name = 'Approve' THEN
                                                                                     ARGM.ApprovalDate
                                                                                 WHEN MTL.Name = 'Process' THEN
                                                                                     null
                                                                                 ELSE
                                                                                     ARGM.LastUpdatedTime
                                                                             END
                                                                            ),
                                                                            'dd MMM yyyy hh:mm:ss'
                                                                        ) [ApprovalDate],
                                                                  ARGM.Comment,
                                                                  ARGM.Level
                                                           FROM ApprovalRequestGroupMember ARGM
                                                               JOIN MasterTable MTL
                                                                   ON ARGM.Status = MTL.ValueId
                                                                      AND MTL.Category = 'ApprovalRequest.Status'
                                                           WHERE ARGM.ApprovalRequestId = ARG.ApprovalRequestId
                                                                 AND ARGM.ApprovaGroup_SubCategoryId = SCY.Id
                                                           UNION ALL
                                                           SELECT DISTINCT
                                                               (DAP.UserName) UserName,
                                                               MTL.Description [Status],
                                                               FORMAT(DAP.ApprovalDate, ' dd MMM yyyy hh:mm:ss') [ApprovalDate],
                                                               DAP.Comment,
                                                               DAP.Level
                                                           FROM ApprovalRequestGroupMemberDAP DAP
                                                               JOIN MasterTable MTL
                                                                   ON DAP.Status = MTL.ValueId
                                                                      AND MTL.Category = 'ApprovalRequest.Status'
                                                           WHERE DAP.ApprovalRequestId = ARG.ApprovalRequestId
                                                                 AND DAP.ApprovaGroup_SubCategoryId = SCY.Id
                                                       ) MEMBER2
                                                       ORDER BY MEMBER2.Level ASC
                                                       FOR JSON PATH, INCLUDE_NULL_VALUES
                                                   )
                                                             ) ApprovalMember,
                                                   ARG.Sequence
                                            FROM PRFSummary PFS
                                                JOIN ApprovalRequestGroup ARG
                                                    ON PFS.ApprovalRequestId = ARG.ApprovalRequestId
                                                JOIN SubCategory SCY
                                                    ON ARG.ApprovalGroup_SubCategoryId = SCY.Id
                                                inner join
                                                (
                                                    select afd.Id,
                                                           afd.Sequence,
                                                           afd.ApprovalGroup_SubCategoryId,
                                                           afd.Notes
                                                    from ApprovalFlowDetail as afd
                                                        inner join ApprovalFlow as af
                                                            on af.Id = afd.ApprovalFlowID
                                                               and af.Name = 'FlowProcsum'
                                                ) as afd
                                                    on afd.ApprovalGroup_SubCategoryId = SCY.Id
                                            WHERE PFS.PRFId = @prf_id
                                           )
                                        SELECT
                                            (
                                                SELECT Types,
                                                       SubCategoryName,
                                                       SubCategoryCode,
                                                       Id,
                                                       Rn,
                                                       ApprovalMember,
                                                       Sequence
                                                FROM CTE
                                                WHERE Rn = 1
                                                ORDER BY Sequence ASC
                                                FOR JSON PATH, INCLUDE_NULL_VALUES
                                            ) [JsonResponses]";

                //--and PFS.Status not in (4) 

                JsonResponse res_procsum = await Task.FromResult(_iDapper.Get<JsonResponse>(queryProcsum, new Dapper.DynamicParameters(new
                {
                    prf_id = prf_id
                }), commandType: CommandType.Text));


                response = new ApprovalHistoryProcSumHeader()
                {
                    PRF = JsonConvert.DeserializeObject<List<ApprovalHistoryProcSum>>(res_prf.JsonResponses),
                    ProcSum = JsonConvert.DeserializeObject<List<ApprovalHistoryProcSum>>(res_procsum.JsonResponses)
                };

                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        public async Task<SummaryTracking> ProcsumTracking(string RequestNumber)
        {
            SummaryTracking response = new SummaryTracking();
            try
            {

				string query = $@"SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01262'";

                int category_Id = await Task.FromResult(_iDapper.Insert<int>(query, new Dapper.DynamicParameters(new
                {
                    reqNumber = RequestNumber
                }), commandType: CommandType.Text));


                string query1 = $@"SELECT Id FROM PRF WHERE PRFNo = @reqNumber";
                int prf_id = await Task.FromResult(_iDapper.Insert<int>(query1, new Dapper.DynamicParameters(new
                {
                    reqNumber = RequestNumber
                }), commandType: CommandType.Text));


                string trackingPRF = $@"SELECT
                                            (
                                                SELECT 'Purchase Request Form' [Types],
                                                       CONCAT(PRF.PRFNo, ' - ', FORMAT(AR.LastUpdatedTime, 'MMM, dd yyyy'), ' - ', MTL.ShortDescription) [StatusHistory]
                                                FROM PRF PRF
                                                    JOIN ApprovalRequest AR
                                                        ON PRF.ApprovalRequestId = AR.Id
                                                    JOIN MasterTable MTL
                                                        ON AR.Status = MTL.ValueId
                                                           AND MTL.Category = 'ApprovalRequest.Status'
                                                WHERE PRF.PRFNo = @reqNumber
                                                FOR JSON PATH, INCLUDE_NULL_VALUES
                                            ) [JsonResponses]";
                JsonResponse res_prf = await Task.FromResult(_iDapper.Get<JsonResponse>(trackingPRF, new Dapper.DynamicParameters(new
                {
                    reqNumber = RequestNumber
                }), commandType: CommandType.Text));

                string trackingProcsum = $@"SELECT
                                                (
                                                    SELECT 'Procurement Summary' [Types],
                                                           CONCAT(PRF.PRFNo, ' - ', FORMAT(AR.LastUpdatedTime, 'MMM, dd yyyy'), ' - ', MTL.ShortDescription) [StatusHistory]
                                                    FROM PRFSummary PFS
                                                        JOIN PRF PRF
                                                            ON PFS.PRFId = PRF.Id
                                                        JOIN ApprovalRequest AR
                                                            ON AR.Id = PFS.ApprovalRequestId
                                                        JOIN MasterTable MTL
                                                            ON AR.Status = MTL.ValueId
                                                               AND MTL.Category = 'ApprovalRequest.Status'
                                                    WHERE PFS.PRFId = @PrfId
                                                    FOR JSON PATH, INCLUDE_NULL_VALUES
                                                ) [JsonResponses]";
                JsonResponse res_procsum = await Task.FromResult(_iDapper.Get<JsonResponse>(trackingProcsum, new Dapper.DynamicParameters(new
                {
                    PrfId = prf_id
                }), commandType: CommandType.Text));

                string trackingPo = $@"SELECT
                                                (
                                                    SELECT 'Purchase Order' [Types],
                                                           CONCAT(PON.PONumber, ' - ', FORMAT(PON.ApproverDate, 'MMM, dd yyyy'), ' - ', MTL.ShortDescription) [StatusHistory]
                                                    FROM PRFSummary PFS
                                                        JOIN PONonShopping PON
                                                            ON PFS.Id = PON.PRFSummaryId
                                                        JOIN MasterTable MTL
                                                            ON PON.Status = MTL.ValueId
                                                               AND MTL.Category = 'PurchaseOrder.Status'
                                                    WHERE PFS.PRFId = @PrfId
                                                    FOR JSON PATH, INCLUDE_NULL_VALUES
                                                ) [JsonResponses]";
                JsonResponse res_po = await Task.FromResult(_iDapper.Get<JsonResponse>(trackingPo, new Dapper.DynamicParameters(new
                {
                    PrfId = prf_id
                }), commandType: CommandType.Text));

				string trackingGl = $@"SELECT
										(
											SELECT 'Guarantee Letter' [Types],
												   CONCAT(GL.GLNumber, ' - ', FORMAT(GL.LastUpdatedTime, 'MMM, dd yyyy'), ' - ', MTL.ShortDescription) [StatusHistory]
											FROM PRFSummary PFS
												JOIN GuaranteeLetter GL
													ON PFS.Id = GL.PRFSummaryId
												JOIN MasterTable MTL
													ON GL.Status = MTL.ValueId
													   AND MTL.Category = 'GuaranteeLetter.Status'
											WHERE PFS.PRFId = @PrfId
											FOR JSON PATH, INCLUDE_NULL_VALUES
										) [JsonResponses]";

                JsonResponse res_gl = await Task.FromResult(_iDapper.Get<JsonResponse>(trackingGl, new Dapper.DynamicParameters(new
                {
                    PrfId = prf_id
                }), commandType: CommandType.Text));

                string trackingDN = $@"SELECT
                                            (
                                                SELECT 'Delivery Note' [Types],
                                                       CONCAT(
                                                                 DEN.DeliveryNumber,
                                                                 ' - ',
                                                                 FORMAT(DEN.LastUpdatedTime, 'MMM, dd yyyy'),
                                                                 ' - ',
                                                                 MTL.ShortDescription
                                                             ) [StatusHistory]
                                                FROM PRFSummary PFS
                                                    JOIN PONonShopping PON
                                                        ON PFS.Id = PON.PRFSummaryId
                                                    JOIN DeliveryNotes DEN
                                                        ON PON.Id = DEN.PurchaseOrderId
                                                           AND CategoryProcess_SubCategoryId = @CategoryId
                                                    JOIN MasterTable MTL
                                                        ON DEN.Status = MTL.ValueId
                                                           AND MTL.Category = 'DeliveryNote.Status'
                                                WHERE PFS.PRFId = @PrfId
                                                FOR JSON PATH, INCLUDE_NULL_VALUES
                                            ) [JsonResponses]";

                JsonResponse res_dn = await Task.FromResult(_iDapper.Get<JsonResponse>(trackingDN, new Dapper.DynamicParameters(new
                {
                    PrfId = prf_id,
                    CategoryId = category_Id
                }), commandType: CommandType.Text));


                string trackingInv = $@"SELECT
                                            (
                                                SELECT 'Invoice Management' [Types],
                                                       CONCAT(IPO.InvoiceNumber, ' - ', FORMAT(IPO.InvoiceDate, 'MMM, dd yyyy'), ' - ', MTL.ShortDescription) [StatusHistory]
                                                FROM PRFSummary PFS
                                                    JOIN PONonShopping PON
                                                        ON PFS.Id = PON.PRFSummaryId
                                                    JOIN InvoicePO IPO
                                                        ON PON.Id = IPO.PurchaeseOrderId
                                                           AND IPO.CategoryProcess_SubCategoryId = @CategoryId
                                                    JOIN MasterTable MTL
                                                        ON IPO.Status = MTL.ValueId
                                                           AND MTL.Category = 'InvoiceManagement.Status'
                                                WHERE PFS.PRFId = @PrfId
                                                FOR JSON PATH, INCLUDE_NULL_VALUES
                                            ) [JsonResponses]";

                JsonResponse res_inv = await Task.FromResult(_iDapper.Get<JsonResponse>(trackingInv, new Dapper.DynamicParameters(new
                {
                    PrfId = prf_id,
                    CategoryId = category_Id
                }), commandType: CommandType.Text));

				string trackingPAP = $@"SELECT
										(
											SELECT 'PAP' [Types],
												   CONCAT(PAP.PAPNo, ' - ', FORMAT(PAP.LastUpdatedTime, 'MMM, dd yyyy'), ' - ', MTL.ShortDescription) [StatusHistory]
											FROM PRF PRF
												JOIN PAP PAP
													ON PAP.PRFId = PRF.Id
												JOIN MasterTable MTL
													ON PAP.Status = MTL.ValueId
													   AND MTL.Category = 'PRFSummary.Status'
											WHERE PRF.PRFNo = @reqNumber
											FOR JSON PATH, INCLUDE_NULL_VALUES
										) [JsonResponses]";
                JsonResponse res_pap = await Task.FromResult(_iDapper.Get<JsonResponse>(trackingPAP, new Dapper.DynamicParameters(new
                {
                    reqNumber = RequestNumber
                }), commandType: CommandType.Text));

                response = new SummaryTracking()
				{
					trackingPRF = res_prf.JsonResponses is not null ? JsonConvert.DeserializeObject<List<TrackingHistory>>(res_prf.JsonResponses) : null,
					trackingProcsum = res_procsum.JsonResponses is not null ? JsonConvert.DeserializeObject<List<TrackingHistory>>(res_procsum.JsonResponses) : null,
					trackingPo = res_po.JsonResponses is not null ? JsonConvert.DeserializeObject<List<TrackingHistory>>(res_po.JsonResponses) : null,
					trackingDelivery = res_dn.JsonResponses is not null ? JsonConvert.DeserializeObject<List<TrackingHistory>>(res_dn.JsonResponses) : null,
					trackingInvoice = res_inv.JsonResponses is not null ? JsonConvert.DeserializeObject<List<TrackingHistory>>(res_inv.JsonResponses) : null,
					trackingPap = res_pap.JsonResponses is not null ? JsonConvert.DeserializeObject<List<TrackingHistory>>(res_pap.JsonResponses) : null,
					trackingGl = res_gl.JsonResponses is not null ? JsonConvert.DeserializeObject<List<TrackingHistory>>(res_gl.JsonResponses) : null
                };


                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task<List<ApprovalQuotationDetailGroup>> GetSwitchApprovalDAP(string RequestNumber)
        {
            List<ApprovalQuotationDetailGroup> response = new List<ApprovalQuotationDetailGroup>();
            try
            {
                string query = $@"
--declare @PRFNumber varchar(100) = 'PR_0120250800089'

select
'' as x

, argmd.ApprovalRequestId
, argmd.Id
, argmd.Level
, argmd.AccountId
, argmd.UserName
, argmd.Status
, argmd.CostCenterId
, cc.Name as CostCenterName
, cc.BusinessUnitId
, bu.Name as BusinessUnitName
, mt.ShortDescription as ProcsumStatus

from PRF as p
inner join PRFSummary as ps on ps.PRFId = p.Id and ps.Status <> 4
inner join (
	select '' as x
	, argmd.Id
	, argmd.ApprovalRequestId
	, argmd.Level
	, argmd.AccountId
	, argmd.UserName
	, argmd.Status
	, argmd.CostCenterId
	from ApprovalRequestGroupMemberDAP as argmd
	union all
	select '' as x
	, argm.Id
	, argm.ApprovalRequestId
	, argm.Level
	, argm.AccountId
	, argm.UserName
	, argm.Status
	, argm.CostCenterId
	from ApprovalRequestGroupMember as argm
	inner join SubCategory as sc on sc.Id = argm.ApprovaGroup_SubCategoryId
	and sc.SubCategoryCode in ('ApprovalBudgeted','GA-0008')
) as argmd on argmd.ApprovalRequestId = ps.ApprovalRequestId
inner join CostCenter as cc on cc.Id = argmd.CostCenterId
inner join BusinessUnit as bu on bu.Id = cc.BusinessUnitId
inner join MasterTable as mt on mt.ValueId = argmd.Status and mt.Category = 'ApprovalRequest.Status'

where p.PRFNo = @PRFNumber
order by
argmd.ApprovalRequestId asc
, argmd.Level asc
";
                response = await Task.FromResult(_iDapper.GetAll<ApprovalQuotationDetailGroup>(query, new Dapper.DynamicParameters(new
                {
                    PRFNumber = RequestNumber
                }), commandType: CommandType.Text));


                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task<SwitchHeader> GetApprovalDap(string RequestNumber, int Level)
        {
            SwitchHeader response = new SwitchHeader();
            try
            {
                string queryCategory = $@"SELECT Id FROM SubCategory WHERE SubCategoryCode = @code";
                int ApprovalCategory = await Task.FromResult(_iDapper.Get<int>(queryCategory, new Dapper.DynamicParameters(new
                {
                    code = "GA-0008"
                }), commandType: CommandType.Text));

                string query = $@"
                    declare @RateAmount as money = (
                                                       select top (1)
                                                           bt.RateAmount
                                                       from BudgetTransaction as bt
                                                       where bt.RefNumber = @PRFNumber
                                                   )
                    SELECT
                        (
                        SELECT DISTINCT
                            PFS.ApprovalRequestId [ApprovalRequestId],
                            COST.Name [CostCenterName],
                            BU.Name [BusinessUnit],
                            @Level [Level],
                            0 [OldApprovalId],
                            DAP.UserName [OldMemberName],
                            JSON_QUERY(
                            (
                                SELECT AG.UserName [ApprovalUserName],
                                       AG.AccountId [AccountId],
                                       AG.Id [ApprovalGroupId],
				                       AG.eMail [Email],
                                       AG.Sequence,
                                       AG.Level
                                FROM ApprovalGroup AG
                                WHERE AG.ApprovalGroup_SubCategoryId = @ApprovalGroupSubCategoryId
                                      AND AG.Level = @Level
                                      AND (PVQCC.GrandTotalAmount * @RateAmount) >= AG.MinAmount
                                      AND AG.Status = 1
                                      AND AG.CostCenterId = PRF.CostCenterId
                                FOR JSON PATH
                            )) ApprovalMember
                        FROM PRF PRF
                            JOIN PRFSummary PFS
                                ON PRF.Id = PFS.PRFId
and PFS.Status not in (4)
                            JOIN PRFSummaryDetail PFD
                                ON PFS.Id = PFD.PRFSummaryId

inner join (
	select '' as x
	, argmd.ApprovalRequestId
	, argmd.UserName
	, argmd.CostCenterId
	, argmd.Level
	from ApprovalRequestGroupMemberDAP as argmd
	union all
	select '' as x
	, argm.ApprovalRequestId
	, argm.UserName
	, argm.CostCenterId
	, argm.Level
	from ApprovalRequestGroupMember as argm
	inner join SubCategory as sc on sc.Id = argm.ApprovaGroup_SubCategoryId and sc.SubCategoryCode in ('ApprovalBudgeted','GA-0008')
) DAP
on DAP.ApprovalRequestId = PFS.ApprovalRequestId

                            JOIN CostCenter COST
                                ON DAP.CostCenterId = COST.Id
                            JOIN BusinessUnit as BU
                                ON BU.Id = COST.BusinessUnitId
                            JOIN PRFVendorQuotation as PVQ
                                ON PVQ.PRFId = PRF.Id
                            OUTER APPLY
                        (
                            SELECT SUM(PVQCC.TotalAmmount) as GrandTotalAmount
                            FROM PRFVendorQuotationDetail as PVQD
                                JOIN PRFVendorQuotationCostCenter as PVQCC
                                    ON PVQCC.PRFVendorQuotationDetailId = PVQD.Id
                                       AND PVQD.IsSelected = 1
                            WHERE PVQD.PRFVendorQuotationId = PVQ.Id
                                  and PVQD.Status != 0
                        ) as PVQCC
                        WHERE PRF.PRFNo = @PRFNumber
                              AND DAP.Level = @Level
                        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                    ) JsonResponses";

                var dap_ = await Task.FromResult(_iDapper.Get<JsonResponse>(query, new Dapper.DynamicParameters(new
                {
                    PRFNumber = RequestNumber,
                    ApprovalGroupSubCategoryId = ApprovalCategory,
                    Level = Level
                }), commandType: CommandType.Text));

                response = JsonConvert.DeserializeObject<SwitchHeader>(dap_.JsonResponses);
                string query2 = @"SELECT DISTINCT
                                        ARGMH.UserName [ApprovalUserName],
                                        COST.Name [CostCenterName],
                                        ARGMH.Level [Level],
                                        FORMAT(ARGMH.CreatedTime, 'yyyy-MMM-dd') [ApprovalDate],
                                        MT.Name [StatusApproval],
                                        ARGMH.AssignedToUserName [AssignedToUserName],
                                        ARGMH.Comment [ApprovalComment]
                                    FROM ApprovalRequestGroupMemberHistory ARGMH
                                        JOIN CostCenter COST
                                            ON ARGMH.CostCenterId = COST.Id
                                        JOIN MasterTable MT
                                            ON ARGMH.Status = MT.ValueId
                                        JOIN PRFSummary PFS
                                            ON ARGMH.ApprovalRequestId = PFS.ApprovalRequestId
                                        JOIN PRF PRF
                                            ON PRF.Id = PFS.PRFId
                                    WHERE PRF.PRFNo = @PRFNumber
                                          AND MT.Category = 'ApprovalRequestGroupMember.Status'";
                response.ApprovalHistory = await Task.FromResult(_iDapper.GetAll<GetApprovalActivity>(query2, new DynamicParameters(new
                {
                    PRFNumber = RequestNumber
                }), commandType: CommandType.Text));
                return response;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task<int> SubmitSwitchApproval(ProcurementSummarySwitchApproval request)
        {
            try
            {
                string query = @"

                       declare @Date datetime = getdate();

                        insert into ApprovalRequestGroupMemberHistory
                        (
                            ApprovalRequestGroupMemberId,
                            ApprovalRequestId,
                            CostCenterId,
                            AccountId,
                            UserName,
                            email,
                            ApprovaGroup_SubCategoryId,
                            Status,
                            Sequence,
                            Comment,
                            AttachmentId,
                            AssignedToAccountId,
                            AssignedToEmail,
                            AssignedToUserName,
                            CreatedBy,
                            CreatedTime,
                            Level
                        )
                        SELECT argm.Id,
                               ps.ApprovalRequestId,
                               argm.CostCenterId,
                               argm.AccountId,
                               argm.UserName,
                               argm.email,
                               argm.ApprovaGroup_SubCategoryId,
                               argm.Status,
                               argm.Sequence,
                               @ReasonSwitch,
                               @AttachmentId,
                               @AssignedToAccountId,
                               @AssignedToEmail,
                               @AssignedToUserName,
                               @CreateUser,
                               @Date,
                               argm.Level
                        from PRFSummary as ps
                            inner join
                            (
                                select '' as x,
                                       argmd.Id,
                                       argmd.ApprovalRequestId,
                                       argmd.Level,
                                       argmd.CostCenterId,
                                       argmd.AccountId,
                                       argmd.UserName,
                                       argmd.email,
                                       argmd.ApprovaGroup_SubCategoryId,
                                       argmd.Status,
                                       argmd.Sequence
                                from ApprovalRequestGroupMemberDAP as argmd
                                union all
                                select '' as x,
                                       argm.Id,
                                       argm.ApprovalRequestId,
                                       argm.Level,
                                       argm.CostCenterId,
                                       argm.AccountId,
                                       argm.UserName,
                                       argm.email,
                                       argm.ApprovaGroup_SubCategoryId,
                                       argm.Status,
                                       argm.Sequence
                                from ApprovalRequestGroupMember as argm
                                    inner join SubCategory as sc
                                        on sc.Id = argm.ApprovaGroup_SubCategoryId
                                           and sc.SubCategoryCode in ( 'ApprovalBudgeted', 'GA-0008' )
                            ) as argm
                                on argm.ApprovalRequestId = ps.ApprovalRequestId
                                   and argm.UserName = @OldMemberName
                        where ps.ApprovalRequestId = @ApprovalRequestId


                        if (scope_identity() > 1)
                        begin

                            declare @ApprovalRequestGroupMemberDAP bit
                            select top (1)
                                @ApprovalRequestGroupMemberDAP = 1
                            from ApprovalRequestGroupMemberDAP as argmd
                            where argmd.ApprovalRequestId = @ApprovalRequestId
                                  and argmd.UserName = @OldMemberName
                                  and argmd.Level = @Level

                            declare @ApprovalRequestGroupMember bit
                            select top (1)
                                @ApprovalRequestGroupMember = 1
                            from ApprovalRequestGroupMember as argm
                                inner join SubCategory as sc
                                    on sc.Id = argm.ApprovaGroup_SubCategoryId
                                       and sc.SubCategoryCode in ( 'ApprovalBudgeted', 'GA-0008' )
                            where argm.ApprovalRequestId = @ApprovalRequestId
                                  and argm.UserName = @OldMemberName
                                  and argm.Level = @Level

                            if (@ApprovalRequestGroupMemberDAP = 1)
                            begin
                                update argmd
                                set argmd.AccountId = @AssignedToAccountId,
                                    argmd.UserName = @AssignedToUserName,
                                    argmd.email = @AssignedToEmail,
                                    argmd.LastUpdatedBy = @CreateUser,
                                    argmd.LastUpdatedTime = @Date,
                                    argmd.Sequence = @Sequence
                                from ApprovalRequestGroupMemberDAP as argmd
                                where argmd.ApprovalRequestId = @ApprovalRequestId
                                      and argmd.UserName = @OldMemberName
                                      and argmd.Level = @Level
                            end

                            if (@ApprovalRequestGroupMember = 1)
                            begin
                                update argm
                                set argm.AccountId = @AssignedToAccountId,
                                    argm.UserName = @AssignedToUserName,
                                    argm.email = @AssignedToEmail,
                                    argm.LastUpdatedBy = @CreateUser,
                                    argm.LastUpdatedTime = @Date,
                                    argm.Sequence = @Sequence
                                from ApprovalRequestGroupMember as argm
                                where argm.ApprovalRequestId = @ApprovalRequestId
                                      and argm.UserName = @OldMemberName
                                      and argm.Level = @Level
                            end

                            select (case
                                        when @@rowcount > 0 then
                                            0
                                        else
                                            2
                                    end
                                   ) as x

                        end
                        else
                        begin
                            select 3;
                        end

                        return;";

                var dap_ = await Task.FromResult(_iDapper.Get<int>(query, new Dapper.DynamicParameters(new
                {
                    ReasonSwitch = request.ReasonSwitch,
                    AttachmentId = request.AttachmentId,
                    AssignedToAccountId = request.AssignedToAccountId,
                    AssignedToEmail = request.AssignedToEmail,
                    AssignedToUserName = request.AssignedToUserName,
                    CreateUser = request.CreateUser,
                    OldMemberName = request.OldMemberName,
                    ApprovalRequestId = request.ApprovalRequestId,
                    Sequence = request.Sequence,
                    Level = request.Level
                }), commandType: CommandType.Text));
                return dap_;
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        #endregion

        #region Save Type Process & Candidate Vendor
        public async Task<CommonResponse> InsertUpdateTypeProccess(PrfTypeProccesAddEdit request)
        {
            CommonResponse response = new CommonResponse();

            try
            {
                var subCategory = await _subCategoryRepository.GetSubCategory((int)request.TypeProcess_SubCategory);

                var validation = await InsertUpdateTypeProccessValidation(request, subCategory);
                if (!validation.IsNullOrEmpty())
                {
                    return new CommonResponse
                    {
                        Code = StatusCodes.Status400BadRequest,
                        Status = _globalExceptions.StatusCode(StatusCodes.Status400BadRequest),
                        Data = validation
                    };
                }

                var vendorQuotation = await _prfVendorQuotatiorRepository.GetVendorQuotationByPrfId(request.PRFId);
                var PRFNumber = await GetPRFNumber(request.PRFId);

                if (subCategory.SubCategoryCode == "SC-2023-08-11135")
                {
                    request.saveVendorCandidates = new List<SaveVendorCandidate>();
                }

                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, _iDapper.GetTransactionOptions()))
                {
                    if (vendorQuotation == null)
                    {
                        await InsertPRFVendorQuotation(request);
                    }

                    await UpdatePRFVendorCandidateSetNonActive(request);

                    if (subCategory.SubCategoryCode == "SC-2023-08-11135")
                    {
                        await UpdatePRFTypeProcessLessTen(request, 6);

                        await _budgetTransactionRepository.InsertBudgetTransactionByRequestType("prf", PRFNumber, request.UserName, false);
                    }
                    else
                    {
                        await UpdatePRFTypeProcessMoreTen(request);

                        foreach (var vendor in request.saveVendorCandidates)
                        {
                            await UpdatePRFVendorCandidateSetActive(request, (int)vendor.VendorId);
                        }

                        await InsertPRFVendorCandidate(request);
                    }

                    await UpdatePRFVendorQuotationDetail(request);

                    scope.Complete();

                    response = new CommonResponse
                    {
                        Code = StatusCodes.Status200OK,
                        Status = _globalExceptions.StatusCode(StatusCodes.Status200OK),
                        Data = "Success Save Type Process & Candidate Vendor"
                    };
                }

                //Generate PDF PRF
                var resultPdfPRF = await Task.FromResult(GeneratePdfPRF(request.PRFId));

                if (subCategory.SubCategoryCode == "SC-2023-08-11135")
                {
                    await _nonShopNotificationRepository.SendEmailPadiBelow("Complete", request.PRFId);
                }

                //Upload PDF PRF
                await DeleteAttachmentTypeProccess(request.PRFId);
				await Task.FromResult(UploadPdfPRF(resultPdfPRF, request.PRFId));
            }
            catch (Exception e)
            {
                response = new CommonResponse
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = _globalExceptions.StatusCode(StatusCodes.Status400BadRequest),
                    Data = "Failed Save Type Process & Candidate Vendor"
                };
                throw new GlobalExceptions(objectName, e.InnerException);
            }
            return response;
		}
		private async Task DeleteAttachmentTypeProccess(int PRFId)
		{
			try
			{
				await Task.FromResult(_iDapper.Execute(SaveTypeProcessQuery.DeleteAttachmentTypeProccess, new DynamicParameters(new
				{
					PRFId,
				}), commandType: CommandType.Text));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		private async Task<string> InsertUpdateTypeProccessValidation(PrfTypeProccesAddEdit request, SubCategoryResponse subCategory)
        {
            List<string> errors = new List<string>();

            try
            {
                ////Validation Type Process
                errors.Add(await SubCategoryValidation.ById(_iDapper, (int)request.TypeProcess_SubCategory, "Type Process Type is Invalid"));

                if (subCategory.SubCategoryCode != "SC-2023-08-11135" && request.saveVendorCandidates.Count <= 0)
                {
                    errors.Add("Please Input Candidate Vendor");
                }
            }
            catch (Exception ex)
            {
                errors.Add("Vendor Validation Failed");
            }
            return string.Join("<br>", errors.Where(s => !s.IsNullOrEmpty()));
        }
        private async Task InsertPRFVendorQuotation(PrfTypeProccesAddEdit request)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SaveTypeProcessQuery.InsertPRFVendorQuotation, new DynamicParameters(new
                {
                    request.PRFId,
                    request.UserName,
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task UpdatePRFTypeProcessMoreTen(PrfTypeProccesAddEdit request)
        {

            try
            {
                await Task.FromResult(_iDapper.Execute(SaveTypeProcessQuery.UpdatePRFTypeProcessMoreTen, new DynamicParameters(new
                {
                    request.PRFId,
                    request.TypeProcess_SubCategory,
                    request.ContractCriteria_SubCategory,
                    request.UserName,
                    request.BuyerAccountId,
                    DateInserted = DateTime.Now,
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task UpdatePRFTypeProcessLessTen(PrfTypeProccesAddEdit request, int Status)
        {

            try
            {
                await Task.FromResult(_iDapper.Execute(SaveTypeProcessQuery.UpdatePRFTypeProcessLessTen, new DynamicParameters(new
                {
                    request.PRFId,
                    request.TypeProcess_SubCategory,
                    request.ContractCriteria_SubCategory,
                    request.UserName,
                    request.BuyerAccountId,
                    DateInserted = DateTime.Now,
                    Status
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task UpdatePRFVendorCandidateSetNonActive(PrfTypeProccesAddEdit request)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SaveTypeProcessQuery.UpdatePRFVendorCandidateSetNonActive, new DynamicParameters(new
                {
                    request.PRFId,
                    request.UserName,
                    DateInserted = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task UpdatePRFVendorCandidateSetActive(PrfTypeProccesAddEdit request, int VendorId)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SaveTypeProcessQuery.UpdatePRFVendorCandidateSetActive, new DynamicParameters(new
                {
                    request.PRFId,
                    request.UserName,
                    DateInserted = DateTime.Now,
                    VendorId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task UpdatePRFVendorQuotationDetail(PrfTypeProccesAddEdit request)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SaveTypeProcessQuery.UpdatePRFVendorQuotationDetail, new DynamicParameters(new
                {
                    request.UserName
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertPRFVendorCandidate(PrfTypeProccesAddEdit request)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SaveTypeProcessQuery.InsertPRFVendorCandidate, new DynamicParameters(new
                {
                    jsonCandidate = JsonConvert.SerializeObject(request.saveVendorCandidates),
                    request.PRFId,
                    request.UserName,
                    DateInserted = DateTime.Now,
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<string> GetPRFNumber(int Id)
        {
            try
            {
                string queryPRFGetNumber = $@"SELECT PRFNo FROM PRF WHERE Id = @Id";
                return await Task.FromResult(_iDapper.Get<string>(queryPRFGetNumber, new Dapper.DynamicParameters(new
                {
                    Id
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        #endregion

        #region Get Vendor Term Of Payment
        public async Task<CommonResponse> GetVendorTermOfPayment(GetVendorTermOfPaymentRequest request)
        {
            CommonResponse result = new CommonResponse();

            try
            {
                string query = $@"
									SELECT * 
									FROM VendorTermOfPayment 
									WHERE VendorId = @VendorId
										AND TOPType_SubCategoryId = @TOPType_SubCategoryId
								";

                VendorTermOfPayment vendorTop = await Task.FromResult(_iDapper.Get<VendorTermOfPayment>(query, new DynamicParameters(new
                {
                    request.VendorId,
                    request.TOPType_SubCategoryId
                }), commandType: CommandType.Text));

                if (vendorTop != null)
                {
                    result = new CommonResponse()
                    {
                        Code = StatusCodes.Status200OK,
                        Status = _globalExceptions.StatusCode(StatusCodes.Status200OK),
                        Data = vendorTop
                    };
                }
                else
                {
                    result = new CommonResponse()
                    {
                        Code = StatusCodes.Status404NotFound,
                        Status = _globalExceptions.StatusCode(StatusCodes.Status404NotFound),
                        Data = "Vendor Term Of Payment Not Found"
                    };
                }
            }
            catch (Exception ex)
            {
                result = new CommonResponse()
                {
                    Code = StatusCodes.Status404NotFound,
                    Status = _globalExceptions.StatusCode(StatusCodes.Status404NotFound),
                    Data = "Failed Get Vendor Term Of Payment"
                };

                throw new GlobalExceptions(objectName, ex.InnerException);
            }

            return result;
        }
        #endregion

        #region Save Vendor Quotation
        public async Task<CommonResponse> SaveQuotationVendor(VendorQuotationSave param)
        {
            CommonResponse response = new CommonResponse();

            try
            {
                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, _iDapper.GetTransactionOptions()))
                {
                    await UpdatePRFVendorQuotation(param);

                    foreach (QuotationDetail objDetail in param.VendorQuotationDetail)
                    {
                        int detailId = await InsertPRFVendorQuotationDetail(param, objDetail);

                        await InsertPRFVendorQuotationCostCenter(param, objDetail, detailId);

                        foreach (VendorQuotationOtherCostModel other in objDetail.vendorQuotationOtherCost)
                        {
                            await InsertPRFVendorQuotationOtherCost(param, other, detailId);
                        }
                    }

                    await DeletePRFVendorQuotationTOP((int)param.PRFVendorQuotationId);
                    foreach (var topItem in param.TOPRequests)
                    {
                        await InsertPRFVendorQuotationTOP(param, topItem);
                    }

                    scope.Complete();

                    response = new CommonResponse
                    {
                        Code = StatusCodes.Status200OK,
                        Status = _globalExceptions.StatusCode(response.Code),
                        Data = "Successfully save quotation vendor"
                    };
                }
            }
            catch (Exception ex)
            {
                response = new CommonResponse
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = _globalExceptions.StatusCode(response.Code),
                    Data = "Failed to save quotation vendor"
                };
                throw new GlobalExceptions(objectName, ex.InnerException);
            }

            return response;
        }
        private async Task UpdatePRFVendorQuotation(VendorQuotationSave request)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SaveVendorQuotationQuery.UpdatePRFVendorQuotation, new DynamicParameters(new
                {
                    request.PRFId,
                    request.Remarks,
                    request.VersionNo,
                    request.Title,
                    request.Summary,
                    request.EstimatedDeliveryDate,
                    request.FinalSpesificationDate,
                    request.CreatedBy,
                    request.CreatedTime,
                    request.PRFVendorQuotationId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<int> InsertPRFVendorQuotationDetail(VendorQuotationSave request, QuotationDetail quotationDetail)
        {
            try
            {
                return await Task.FromResult(_iDapper.Insert<int>(SaveVendorQuotationQuery.InsertPRFVendorQuotationDetail, new DynamicParameters(new
                {
                    request.PRFVendorQuotationId,
                    quotationDetail.PRFDetailId,
                    quotationDetail.ItemDescription,
                    quotationDetail.ItemName,
                    quotationDetail.TypeOfGoods_SubCategoryId,
                    quotationDetail.Remarks,
                    quotationDetail.L_Currency_Code,
                    quotationDetail.QuotationAmount,
                    quotationDetail.VendorId,
                    quotationDetail.IsSelected,
                    quotationDetail.ITRelated,
                    quotationDetail.QuotationDate,
                    quotationDetail.Qty,
                    quotationDetail.Unit,
                    quotationDetail.ItemPrice,
                    quotationDetail.TotalAmmount,
                    quotationDetail.RateAmmount,
                    quotationDetail.BaseAmmount,
                    quotationDetail.TotalBaseAmmount,
                    quotationDetail.IsAddMasterItem,
                    request.CreatedBy,
                    request.CreatedTime,
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertPRFVendorQuotationCostCenter(VendorQuotationSave request, QuotationDetail quotationDetail, int detailId)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SaveVendorQuotationQuery.InsertPRFVendorQuotationCostCenter, new DynamicParameters(new
                {
                    PRFVendorQuotationDetailId = detailId,
                    quotationDetail.vendorQuotationCostCenter.CostCenterId,
                    quotationDetail.vendorQuotationCostCenter.L_Currency_Code,
                    quotationDetail.vendorQuotationCostCenter.Percentage,
                    quotationDetail.vendorQuotationCostCenter.TotalAmmount,
                    quotationDetail.vendorQuotationCostCenter.Remarks,
                    request.CreatedBy,
                    request.CreatedTime,
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertPRFVendorQuotationOtherCost(VendorQuotationSave request, VendorQuotationOtherCostModel otherCost, int detailId)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SaveVendorQuotationQuery.InsertPRFVendorQuotationOtherCost, new Dapper.DynamicParameters(new
                {
                    PRFVendorQuotationDetailId = detailId,
                    otherCost.OtherCost_SubCategoryId,
                    otherCost.MtOtherCostCode,
                    otherCost.L_Currency_Code,
                    otherCost.Included,
                    otherCost.VATPercentage,
                    otherCost.OtherCostAmount,
                    otherCost.Remarks,
                    request.CreatedTime,
                    request.CreatedBy
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertPRFVendorQuotationTOP(VendorQuotationSave request, SavePRFVendorQuotationTOPRequest topRequest)
        {
            try
            {
                var PaymentDate = DateTime.ParseExact(request.EstimatedDeliveryDate, "MM/dd/yyyy", null).AddDays((int)topRequest.TOPDays);

                await Task.FromResult(_iDapper.Execute(SaveVendorQuotationQuery.InsertPRFVendorQuotationTOP, new DynamicParameters(new
                {
                    request.PRFVendorQuotationId,
                    topRequest.TOPType_SubCategoryId,
                    topRequest.Percentage,
                    topRequest.TOPDays,
                    topRequest.PaymentAmount,
                    PaymentDate,
                    topRequest.PaymentMethod,
                    Status = 1,
                    CreatedTime = DateTime.Now,
                    request.CreatedBy,
                    topRequest.VendorId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task DeletePRFVendorQuotationTOP(int PRFVendorQuotationId)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SaveVendorQuotationQuery.DeletePRFVendorQuotationTOP, new DynamicParameters(new
                {
                    PRFVendorQuotationId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        #endregion

        #region Get Vendor Quotation Term Of Payment
        public async Task<List<PRFVendorQuotationTOP>> GetPRFVendorQuotationTOP(int PRFVendorQuotationId)
        {
            try
            {
                string query = $@"
									SELECT *
									FROM PRFVendorQuotationTOP
									WHERE PRFVendorQuotationId = @PRFVendorQuotationId
								";

                return await Task.FromResult(_iDapper.GetAll<PRFVendorQuotationTOP>(query, new DynamicParameters(new
                {
                    PRFVendorQuotationId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        #endregion

        #region Submit Procsum
        public async Task<CommonResponse> SubmitProcSum(ProcsumApprovalModel request)
        {

            CommonResponse response = new CommonResponse();

            try
            {
                string RequestNo = await SelectRequestNoProcsum("PROCSUM");
                int ApprovalFlowId = await SelectApprovalFlowId("FlowProcsum");
                PRF PRF = await SelectPRFByPRFId((int)request.PRFId);
                PAPApprovalRequestModel Creator = await SelectUserAccountByUsername(request.CreatedBy);

                var budgetResponses = await _budgetTransactionRepository.GetBudgetResponse(request.PRFNo, "prf");

                PrfSummaryModel.PrfSummary resultInsertPRFSummary = new PrfSummaryModel.PrfSummary();
                decimal TotalAmount = 0;

                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, _iDapper.GetTransactionOptions()))
                {
                    PAPApprovalRequestModel ApprovalRequest = await InsertApprovalRequestPAP(RequestNo, ApprovalFlowId, PRF, Creator, "PROCSUM");

                    foreach (PAPApprovalRequestGroupModel group in request.Groups)
                    {
                        if (group?.Members.Count > 0)
                        {
                            await InsertApprovalRequestGroupPAP(group, (int)ApprovalRequest.Id);
                        }

                        SubCategoryResponse subCategory = await _subCategoryRepository.GetSubCategory((int)group.ApprovalGroup_SubCategoryId);
                        if (subCategory.SubCategoryCode.Equals("GA-0008", StringComparison.CurrentCultureIgnoreCase))
                        {
                            foreach (var member in group.Members)
                            {
                                await InsertApprovalRequestGroupMemberDAPPAP(member, (int)ApprovalRequest.Id, (int)group.Sequence);
                            }
                        }
                        else
                        {
                            foreach (var member in group.Members)
                            {
                                await InsertApprovalRequestGroupMemberPAP(member, (int)ApprovalRequest.Id, (int)group.Sequence);
                            }
                        }
                    }

                    resultInsertPRFSummary = await InsertPRFSummary(request, (int)ApprovalRequest.Id);
                    await InsertPRFSummaryTOP(request);
                    await InsertPRFSummaryDetail(request);
                    await InsertPRFSummaryCostCenter(request);
                    await InsertPRFSummaryOtherCost(request);
                    await InsertApprovalRequestItem(request, (int)ApprovalRequest.Id);

                    TotalAmount = await SelectPRFTotalAmount(request.PRFNo);

                    scope.Complete();
                }

                var paramBudgetUpdate = new BudgetTransactionModel()
                {
                    LastUpdatedBy = request.CreatedBy,
                    MBudgetId = budgetResponses[0].BudgetId,
                    LCurrencyCode = budgetResponses[0].LCurrencyCode,
                    BasicAmount = TotalAmount * budgetResponses[0].RateAmount,
                    Amount = TotalAmount,
                    RateAmount = budgetResponses[0].RateAmount,
                    RefNumber = request.PRFNo
                };
                await _budgetTransactionRepository.UpdateBudgetTransaction(paramBudgetUpdate);

                await _nonShopNotificationRepository.SendEmailPRFProcsum("Pending", request.PRFNo, (int)resultInsertPRFSummary.Id);

                response = new CommonResponse
                {
                    Code = StatusCodes.Status200OK,
                    Status = _globalExceptions.StatusCode(response.Code),
                    Data = "Successfully Submit Procsum"
                };
            }
            catch (Exception ex)
            {
                response = new CommonResponse
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = _globalExceptions.StatusCode(response.Code),
                    Data = "Failed to Submit Procsum"
                };
                throw new GlobalExceptions(objectName, ex.InnerException);
            }

            return response;
        }
        private async Task<string> SelectRequestNoProcsum(string Name)
        {

            try
            {
                return await Task.FromResult(_iDapper.Get<string>(SubmitProcsumQuery.SelectRequestNoProcsum, new DynamicParameters(new
                {
                    Name
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<PRF> SelectPRFByPRFId(int PRFId)
        {

            try
            {
                return await Task.FromResult(_iDapper.Get<PRF>(SubmitProcsumQuery.SelectPRFByPRFId, new DynamicParameters(new
                {
                    PRFId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<decimal> SelectPRFTotalAmount(string PRFNumber)
        {

            try
            {
                return await Task.FromResult(_iDapper.Get<decimal>(SaveProcsumQuery.SelectPRFTotalAmount, new DynamicParameters(new
                {
                    PRFNumber
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<PrfSummaryModel.PrfSummary> InsertPRFSummary(ProcsumApprovalModel request, int ApprovalRequestId)
        {

            try
            {
                return await Task.FromResult(_iDapper.Insert<PrfSummaryModel.PrfSummary>(SaveProcsumQuery.InsertPRFSummary, new DynamicParameters(new
                {
                    request.PRFId,
                    ApprovalRequestId,
                    Status = (int)PRFSummaryStatus.Process,
                    DateNow = DateTime.Now,
                    request.CreatedBy
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertPRFSummaryTOP(ProcsumApprovalModel request)
        {

            try
            {
                await Task.FromResult(_iDapper.Execute(SaveProcsumQuery.InsertPRFSummaryTOP, new DynamicParameters(new
                {
                    request.PRFId,
                    request.CreatedBy,
                    DateNow = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertPRFSummaryDetail(ProcsumApprovalModel request)
        {

            try
            {
                await Task.FromResult(_iDapper.Execute(SaveProcsumQuery.InsertPRFSummaryDetail, new DynamicParameters(new
                {
                    request.PRFId,
                    Status = (int)PRFSummaryStatus.Process,
                    request.CreatedBy,
                    DateNow = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertPRFSummaryCostCenter(ProcsumApprovalModel request)
        {

            try
            {
                await Task.FromResult(_iDapper.Execute(SaveProcsumQuery.InsertPRFSummaryCostCenter, new DynamicParameters(new
                {
                    request.PRFId,
                    request.CreatedBy,
                    DateNow = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertPRFSummaryOtherCost(ProcsumApprovalModel request)
        {

            try
            {
                await Task.FromResult(_iDapper.Execute(SaveProcsumQuery.InsertPRFSummaryOtherCost, new DynamicParameters(new
                {
                    request.PRFId,
                    request.CreatedBy,
                    DateNow = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertApprovalRequestItem(ProcsumApprovalModel request, int ApprovalRequestId)
        {

            try
            {
                await Task.FromResult(_iDapper.Execute(SaveProcsumQuery.InsertApprovalRequestItem, new DynamicParameters(new
                {
                    request.PRFId,
                    ApprovalRequestId,
                    Notes = string.Empty,
                    ApprovedQty = 0,
                    ApprovedAmount = 0,
                    Status = 1,
                    request.CreatedBy,
                    DateNow = DateTime.Now,
                    RequestTotalBasicAmount = 0
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        #endregion

        #region Submit Procsum Old
        private async Task<string> SubmitProcsumValidation(ApprovalBudgetProcsumRequest request)
        {
            List<string> errors = new List<string>();

            try
            {
                //Validation Budget Level
                string subCategory = await SelectSubCategoryCodePRFQuery(request.PRFNumber);

                if (subCategory != "SC-2023-08-11133")
                {
                    errors.Add(await BudgetLevelValidaiton(request));
                }

                errors.Add(await DuplicateValidaiton(request.PRFNumber));
            }
            catch (Exception ex)
            {
                errors.Add("Submit Procsum Validation Failed");
            }
            return string.Join("<br>", errors.Where(s => !s.IsNullOrEmpty()));
        }
        private async Task<string> DuplicateValidaiton(string PRFNo)
        {
            string result = null;
            try
            {
                var count = await Task.FromResult(_iDapper.Get<int>(SaveProcsumQuery.InsertProcsumValidationQuery,
                new DynamicParameters(new
                {
                    PRFNo
                }), commandType: CommandType.Text));

                if (count > 0)
                {
                    result = "Procsum Already";
                }
            }
            catch (Exception ex)
            {
                result = "Failed validation Procsum";
            }

            return result;
        }
        private async Task<string> BudgetLevelValidaiton(ApprovalBudgetProcsumRequest request)
        {
            string result = null;
            try
            {
                var dapApproval = request.ApprovalRequestGroupList.FirstOrDefault(x => x.ApprovalGroup_SubCategoryId == 73);
                var lastApproval = dapApproval!.ApprovalRequestGroupMemberList.LastOrDefault();

                var maxAmount = await Task.FromResult(_iDapper.Get<string>(SaveProcsumQuery.BudgetLevelValidaiton,
                            new DynamicParameters(new
                            {
                                lastApproval!.AccountId,
                                lastApproval!.CostCenterId,
                                lastApproval!.ApprovaGroupSubCategoryId,
                                request.PRFNumber
                            }), commandType: CommandType.Text));

                if (maxAmount.IsNullOrEmpty())
                {
                    result = "Budget exceeds approval level, Please confirm to Administrator";
                }
            }
            catch (Exception ex)
            {
                result = "Failed validation Budget Level";
            }

            return result;
        }
        private async Task<string> SelectSubCategoryCodePRFQuery(string PRFNo)
        {
            string result = null;
            try
            {
                result = await Task.FromResult(_iDapper.Get<string>(SaveProcsumQuery.SelectSubCategoryCodePRFQuery,
                new DynamicParameters(new
                {
                    PRFNo
                }), commandType: CommandType.Text));

            }
            catch (Exception ex)
            {
                result = "Failed Get SubCategory";
            }

            return result;
        }
        private async Task<ApprovalRequestProcSum> InsertApprovalRequest(ApprovalBudgetProcsumRequest request)
        {

            try
            {
                return await Task.FromResult(_iDapper.Insert<ApprovalRequestProcSum>(SaveProcsumQuery.InsertApprovalRequest, new DynamicParameters(new
                {
                    Remark = "PROCSUM",
                    Status = 0,
                    request.CreatedBy,
                    request.PRFNumber
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task UpdatePRFSummary(ApprovalBudgetProcsumRequest request)
        {

            try
            {
                await Task.FromResult(_iDapper.Execute(SaveProcsumQuery.UpdatePRFSummary, new DynamicParameters(new
                {
                    request.PRFNumber,
                    request.CreatedBy,
                    DateNow = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<VendorQuotationDetailModel>> SelectPRFVendorQuotationDetail(int PRFVendorQuotationId)
        {

            try
            {
                return await Task.FromResult(_iDapper.GetAll<VendorQuotationDetailModel>(SaveProcsumQuery.SelectPRFVendorQuotationDetail, new DynamicParameters(new
                {
                    PRFVendorQuotationId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<VendorQuotationCostCenter> SelectPRFVendorQuotationCostCenter(int PRFVendorQuotationDetailId)
        {

            try
            {
                return await Task.FromResult(_iDapper.Get<VendorQuotationCostCenter>(SaveProcsumQuery.SelectPRFVendorQuotationCostCenter, new DynamicParameters(new
                {
                    PRFVendorQuotationDetailId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<VendorQuotationOtherCostModel>> SelectPRFVendorQuotationOtherCost(int PRFVendorQuotationDetailId)
        {

            try
            {
                return await Task.FromResult(_iDapper.GetAll<VendorQuotationOtherCostModel>(SaveProcsumQuery.SelectPRFVendorQuotationOtherCost, new DynamicParameters(new
                {
                    PRFVendorQuotationDetailId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<ApprovalRequestGroupProcSum> InsertApprovalRequestGroup(AddApprovalGroupProcsum approval, int ApprovalRequestId, string CreatedBy)
        {
            try
            {
                return await Task.FromResult(_iDapper.Insert<ApprovalRequestGroupProcSum>(SaveProcsumQuery.InsertApprovalRequestGroup, new DynamicParameters(new
                {
                    ApprovalRequestId,
                    approval.ApprovalGroup_SubCategoryId,
                    approval.Sequence,
                    Remark = approval.Remarks,
                    Status = 0,
                    CreatedBy,
                    DateNow = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<ApprovalRequestGroupMemberProcSum> InsertApprovalRequestGroupMemberDAP(ApprovalRequestGroupMemberProcSum argm, int ApprovalRequestId, int PRFSummaryDetailId, string CreatedBy)
        {
            try
            {
                return await Task.FromResult(_iDapper.Insert<ApprovalRequestGroupMemberProcSum>(SaveProcsumQuery.InsertApprovalRequestGroupMemberDAP, new DynamicParameters(new
                {
                    ApprovalRequestId,
                    argm.CostCenterId,
                    argm.ApprovaGroupSubCategoryId,
                    argm.Sequence,
                    argm.Level,
                    PRFSummaryDetailId,
                    CreatedBy,
                    ApproverAccountId = argm.AccountId,
                    ApproverUsername = argm.UserName,
                    Status = 0,
                    DateNow = DateTime.Now,
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<ApprovalRequestGroupMemberProcSum> InsertApprovalRequestGroupMember(ApprovalRequestGroupMemberProcSum argm, int ApprovalRequestId, string CreatedBy)
        {
            try
            {
                return await Task.FromResult(_iDapper.Insert<ApprovalRequestGroupMemberProcSum>(SaveProcsumQuery.InsertApprovalRequestGroupMember, new DynamicParameters(new
                {
                    ApprovalRequestId,
                    argm.CostCenterId,
                    argm.ApprovaGroupSubCategoryId,
                    argm.Sequence,
                    argm.Level,
                    CreatedBy,
                    ApproverUsername = argm.UserName,
                    ApproverAccountId = argm.AccountId,
                    Status = 0,
                    DateNow = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<ApprovalRequestEmail> InsertApprovalRequestEmail(ApprovalRequestGroupMemberProcSum argm, KeyValuePair<int, string> url, string actionKey, string CreatedBy)
        {
            try
            {
                return await Task.FromResult(_iDapper.Insert<ApprovalRequestEmail>(SaveProcsumQuery.InsertApprovalRequestEmail, new DynamicParameters(new
                {
                    argm.ApprovalRequestGroupMemberId,
                    argm.ApprovalRequestGroupMemberDAPId,
                    Action = actionKey,
                    Guid = Guid.NewGuid(),
                    LinkType = url.Key,
                    URLAction = url.Value,
                    Status = 0,
                    CreatedBy,
                    DateNow = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        #endregion

        #region Get Access Level For Purchase Request
        public async Task<int> AccessLevelPurchaseRequest(int AccountId)
        {
            int level = 0;
            try
            {
                bool isProcurement = await IsProcurementByAccountId(AccountId);
                bool isViewAll = await IsViewAllPurchaseRequest(AccountId);
                bool IsVM = await IsVendorManagement(AccountId);

                if (isProcurement)
                {
                    if (isViewAll)
                    {
                        level = (int)FiturPruchaseRequest.ViewAll;
                    }
                    else if (IsVM)
                    {
                        level = (int)FiturPruchaseRequest.VendorManagement;
                    }
                    else
                    {
                        level = (int)FiturPruchaseRequest.Procurment;
                    }
                }
                else
                {
                    level = (int)FiturPruchaseRequest.Requestor;
                }
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
            return level;
        }
        private async Task<bool> IsProcurementByAccountId(int AccountId)
        {
            try
            {
                return await Task.FromResult(_iDapper.Get<bool>(ListProcurmentBuyerQuery.IsProcurement, new DynamicParameters(new
                {
                    AccountId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<bool> IsViewAllPurchaseRequest(int AccountId)
        {
            try
            {
                return await Task.FromResult(_iDapper.Get<bool>(ListProcurmentBuyerQuery.IsViewAllPurchaseRequest, new DynamicParameters(new
                {
                    AccountId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<bool> IsVendorManagement(int AccountId)
        {
            try
            {
                return await Task.FromResult(_iDapper.Get<bool>(ListProcurmentBuyerQuery.IsVendorManagement, new DynamicParameters(new
                {
                    AccountId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        #endregion

        #region PAP

        #region Save PAP
        public async Task<CommonResponse> SavePAP(PapModel request)
        {
            CommonResponse response = new CommonResponse();

            try
            {
                var validation = await SavePAPValidation(request);
                if (!validation.IsNullOrEmpty())
                {
                    return new CommonResponse
                    {
                        Code = StatusCodes.Status400BadRequest,
                        Status = _globalExceptions.StatusCode(response.Code),
                        Data = validation
                    };
                }

                var RateAmount = await _externalService.GetCurrencyExchange(request.L_Currency_Code, "IDR", DateTime.Now);

                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, _iDapper.GetTransactionOptions()))
                {
                    //Insert PAP
                    PAP pap = await InsertPAP(request);

                    foreach (PAPDetailModel detail in request.Details)
                    {
                        //Insert PAP Detail
                        detail.PAPId = pap.Id;
                        detail.RateAmount = RateAmount != null ? RateAmount.RateValue : 1;
                        PAPDetail papDetail = await InsertPAPDetail(detail);

                        //Insert PAP Cost Center
                        detail.CostCenter.PAPDetailId = papDetail.Id;
                        await InsertPAPCostCenter(detail.CostCenter);

                        //Insert PAP Other Cost
                        foreach (PAPOtherCostModel otherCost in detail.OtherCosts)
                        {
                            otherCost.PAPDetailId = papDetail.Id;
                            await InsertPAPOtherCost(otherCost);
                        }
                    }
                    scope.Complete();

                    response = new CommonResponse
                    {
                        Code = StatusCodes.Status200OK,
                        Status = _globalExceptions.StatusCode(response.Code),
                        Data = "Successfully save PAP"
                    };
                }
            }
            catch (Exception ex)
            {
                response = new CommonResponse
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = _globalExceptions.StatusCode(response.Code),
                    Data = "Failed to save PAP"
                };
                throw new GlobalExceptions(objectName, ex.InnerException);
            }

            return response;
        }
        private async Task<PAP> InsertPAP(PapModel request)
        {
            try
            {

                return await Task.FromResult(_iDapper.Insert<PAP>(SavePAPQuery.InsertPAP, new DynamicParameters(new
                {
                    request.PRFId,
                    request.PRFVendorQuotationId,
                    request.PAPNo,
                    request.VendorId,
                    request.GenerateDate,
                    request.PICVendorName,
                    request.PICVendorMobile,
                    request.PICVendorEmail,
                    request.PeriodPaid,
                    request.L_Currency_Code,
                    request.BankAccountNumber,
                    request.BankAccountOwnerName,
                    request.BankCode,
                    request.BankName,
                    request.Description,
                    request.FinanceType_SubCategory,
                    request.InvoiceNo,
                    request.Status,
                    request.ReferenceNo,
                    request.CreatedBy,
                    CreatedTime = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<PAPDetail> InsertPAPDetail(PAPDetailModel request)
        {
            try
            {

                return await Task.FromResult(_iDapper.Insert<PAPDetail>(SavePAPQuery.InsertPAPDetail, new DynamicParameters(new
                {
                    request.PAPId,
                    request.PRFVendorQuotationDetailId,
                    request.ItemDescription,
                    request.ITRelated,
                    request.ItemPrice,
                    request.Qty,
                    request.Unit,
                    request.RateAmount,
                    request.BaseAmount,
                    TotalAmount = request.RateAmount * request.BaseAmount,
                    request.TotalBaseAmount,
                    request.Remarks,
                    request.Status,
                    request.CreatedBy,
                    CreatedTime = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertPAPOtherCost(PAPOtherCostModel request)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SavePAPQuery.InsertPAPOtherCost, new DynamicParameters(new
                {
                    request.PAPDetailId,
                    request.OtherCost_SubCategoryId,
                    request.MtOtherCostCode,
                    request.L_Currency_Code,
                    request.Included,
                    request.VATPercentage,
                    request.OtherCostAmount,
                    request.Status,
                    request.CreatedBy,
                    CreatedTime = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertPAPCostCenter(PAPCostCenterModel request)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SavePAPQuery.InsertPAPCostCenter, new DynamicParameters(new
                {
                    request.PAPDetailId,
                    request.CostCenterId,
                    request.L_Currency_Code,
                    request.Percentage,
                    request.TotalAmmount,
                    request.Status,
                    request.CreatedBy,
                    CreatedTime = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<string> SavePAPValidation(PapModel request)
        {
            List<string> errors = new List<string>();

            try
            {
                errors.Add(await DuplicatePAPValidation((int)request.PRFId, (int)request.PRFVendorQuotationId, 0));
            }
            catch (Exception ex)
            {
                errors.Add("Save PAP Validation Failed");
            }
            return string.Join("<br>", errors.Where(s => !s.IsNullOrEmpty()));
        }
        public async Task<string> DuplicatePAPValidation(int PRFId, int PRFVendorQuotationId, int Status)
        {
            string result = null;
            try
            {
                string query = $@"
                                    SELECT COUNT(Id)
									FROM PAP
									WHERE PRFId = @PRFId
									  AND PRFVendorQuotationId = @PRFVendorQuotationId;
                                ";

                var res = await Task.FromResult(_iDapper.Get<int>(query, new DynamicParameters(new
                {
                    PRFId,
                    PRFVendorQuotationId
                }), commandType: CommandType.Text));

                result = Status switch
                {
                    0 => res > 0 ? "Duplicate PAP" : null,
                    1 => res < 1 ? "PAP Not Found" : null,
                    _ => null
                };

            }
            catch (Exception ex)
            {
                result = "PAP Validation Filed";
            }

            return result;
        }
        #endregion

        #region Update PAP

        public async Task<CommonResponse> EditPAP(PapModel request)
        {
            CommonResponse response = new CommonResponse();

            try
            {
                var validation = await UpdatePAPValidation(request);
                if (!validation.IsNullOrEmpty())
                {
                    return new CommonResponse
                    {
                        Code = StatusCodes.Status400BadRequest,
                        Status = _globalExceptions.StatusCode(response.Code),
                        Data = validation
                    };
                }

                var RateAmount = await _externalService.GetCurrencyExchange(request.L_Currency_Code, "IDR", DateTime.Now);

                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, _iDapper.GetTransactionOptions()))
                {
                    await UpdatePAP(request);

                    foreach (PAPDetailModel detail in request.Details)
                    {
                        detail.PAPId = request.Id;
                        detail.RateAmount = RateAmount != null ? (decimal)RateAmount.RateValue : 1;
                        await UpdatePAPDetail(detail);

                        detail.CostCenter.PAPDetailId = detail.Id;
                        await UpdatePAPCostCenter(detail.CostCenter);

                        foreach (PAPOtherCostModel otherCost in detail.OtherCosts)
                        {
                            otherCost.PAPDetailId = detail.Id;
                            await UpdatePAPOtherCost(otherCost);
                        }
                    }

                    scope.Complete();

                    response = new CommonResponse
                    {
                        Code = StatusCodes.Status200OK,
                        Status = _globalExceptions.StatusCode(response.Code),
                        Data = "Successfully save PAP"
                    };
                }
            }
            catch (Exception ex)
            {
                response = new CommonResponse
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = _globalExceptions.StatusCode(response.Code),
                    Data = "Failed to save PAP"
                };
                throw new GlobalExceptions(objectName, ex.InnerException);
            }

            return response;
        }
        private async Task UpdatePAP(PapModel request)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(UpdatePAPQuery.UpdatePAP, new DynamicParameters(new
                {
                    request.Id,
                    request.PRFId,
                    request.PRFVendorQuotationId,
                    request.PAPNo,
                    request.VendorId,
                    request.GenerateDate,
                    request.PICVendorName,
                    request.PICVendorMobile,
                    request.PICVendorEmail,
                    request.PeriodPaid,
                    request.L_Currency_Code,
                    request.BankAccountNumber,
                    request.BankAccountOwnerName,
                    request.BankCode,
                    request.BankName,
                    request.Description,
                    request.FinanceType_SubCategory,
                    request.InvoiceNo,
                    request.Status,
                    request.ReferenceNo,
                    request.LastUpdatedBy,
                    LastUpdatedTime = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task UpdatePAPDetail(PAPDetailModel request)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(UpdatePAPQuery.UpdatePAPDetail, new DynamicParameters(new
                {
                    request.Id,
                    request.PAPId,
                    request.PRFVendorQuotationDetailId,
                    request.ItemDescription,
                    request.ITRelated,
                    request.ItemPrice,
                    request.Qty,
                    request.Unit,
                    request.RateAmount,
                    request.BaseAmount,
                    TotalAmount = request.RateAmount * request.BaseAmount,
                    request.TotalBaseAmount,
                    request.Remarks,
                    request.Status,
                    request.LastUpdatedBy,
                    LastUpdatedTime = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task UpdatePAPCostCenter(PAPCostCenterModel request)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(UpdatePAPQuery.UpdatePAPCostCenter, new DynamicParameters(new
                {
                    request.Id,
                    request.PAPDetailId,
                    request.CostCenterId,
                    request.L_Currency_Code,
                    request.Percentage,
                    request.TotalAmmount,
                    request.Status,
                    request.LastUpdatedBy,
                    LastUpdatedTime = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task UpdatePAPOtherCost(PAPOtherCostModel request)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(UpdatePAPQuery.UpdatePAPOtherCost, new DynamicParameters(new
                {
                    request.Id,
                    request.PAPDetailId,
                    request.OtherCost_SubCategoryId,
                    request.MtOtherCostCode,
                    request.L_Currency_Code,
                    request.Included,
                    request.VATPercentage,
                    request.OtherCostAmount,
                    request.Status,
                    request.LastUpdatedBy,
                    LastUpdatedTime = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        private async Task<string> UpdatePAPValidation(PapModel request)
        {
            List<string> errors = new List<string>();

            try
            {
                errors.Add(await DuplicatePAPValidation((int)request.PRFId, (int)request.PRFVendorQuotationId, 1));
            }
            catch (Exception ex)
            {
                errors.Add("Update PAP Validation Failed");
            }
            return string.Join("<br>", errors.Where(s => !s.IsNullOrEmpty()));
        }
        #endregion

        #region Submit Approval PAP
        public async Task<CommonResponse> SubmitPAP(PapApprovalModel request)
        {
            CommonResponse response = new CommonResponse();

            try
            {
                PRF PRF = await SelectPRFByPAPId((int)request.PAPId);
                PAPApprovalRequestModel Creator = await SelectUserAccountByUsername(request.CreatedBy);
                int ApprovalFlowId = await SelectApprovalFlowId("FlowPAP");
                List<BudgetResponse> BudgetsResponse = await _budgetTransactionRepository.GetBudgetResponse(PRF.PRFNo, "prf");
                PAPBudgetModel PAPBudget = await GetPAPBudget((int)request.PAPId);

                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, _iDapper.GetTransactionOptions()))
                {
                    PAPApprovalRequestModel ApprovalRequest = await InsertApprovalRequestPAP(request.PAPNo, ApprovalFlowId, PRF, Creator, string.Empty);

                    await UpdatePAP(request, (int)ApprovalRequest.Id);

                    foreach (PAPApprovalRequestGroupModel group in request.Groups)
                    {
                        await InsertApprovalRequestGroupPAP(group, (int)ApprovalRequest.Id);

                        foreach (var member in group.Members)
                        {
                            await InsertApprovalRequestGroupMemberPAP(member, (int)ApprovalRequest.Id, (int)group.Sequence);
                        }
                    }
                    await InsertApprovalRequestItemPAP((int)request.PAPId, (int)ApprovalRequest.Id, request.CreatedBy);

                    var Budget = BudgetsResponse?.FirstOrDefault();
                    var paramBudgetUpdate = new BudgetTransactionModel()
                    {
                        LastUpdatedBy = request.CreatedBy,
                        MBudgetId = Budget?.BudgetId,
                        LCurrencyCode = PAPBudget?.L_Currency_Code,
                        BasicAmount = (decimal)(PAPBudget?.TotalBaseAmount * PAPBudget?.RateAmount),
                        Amount = (decimal)PAPBudget?.TotalBaseAmount,
                        RateAmount = (decimal)PAPBudget?.RateAmount,
                        RefNumber = PRF.PRFNo
                    };
                    await _budgetTransactionRepository.UpdateBudgetTransaction(paramBudgetUpdate);

                    scope.Complete();

                    response = new CommonResponse
                    {
                        Code = StatusCodes.Status200OK,
                        Status = _globalExceptions.StatusCode(response.Code),
                        Data = "Successfully Submit PAP"
                    };
                }

                await _nonShopNotificationRepository.SendEmailPAP("Pending", (int)request.PAPId, PRF.PRFNo);

            }
            catch (Exception ex)
            {
                response = new CommonResponse
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = _globalExceptions.StatusCode(response.Code),
                    Data = "Failed to Submit PAP"
                };
                throw new GlobalExceptions(objectName, ex.InnerException);
            }

            return response;
        }
        private async Task<PRF> SelectPRFByPAPId(int PAPId)
        {

            try
            {
                return await Task.FromResult(_iDapper.Get<PRF>(SubmitPAPQuery.SelectPRFByPAPId, new DynamicParameters(new
                {
                    PAPId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<PAPBudgetModel> GetPAPBudget(int PAPId)
        {

            try
            {
                return await Task.FromResult(_iDapper.Get<PAPBudgetModel>(SubmitPAPQuery.GetPAPBudget, new DynamicParameters(new
                {
                    PAPId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<PAPApprovalRequestModel> SelectUserAccountByUsername(string Username)
        {

            try
            {
                return await Task.FromResult(_iDapper.Get<PAPApprovalRequestModel>(SubmitPAPQuery.SelectUserAccountByUsername, new DynamicParameters(new
                {
                    Username
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<int> SelectApprovalFlowId(string Name)
        {

            try
            {
                return await Task.FromResult(_iDapper.Get<int>(SubmitPAPQuery.SelectApprovalFlowId, new DynamicParameters(new
                {
                    Name
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<PAPApprovalRequestModel> InsertApprovalRequestPAP(string RequestNo, int ApprovalFlowId, PRF prf, PAPApprovalRequestModel creator, string Remark)
        {

            try
            {
                return await Task.FromResult(_iDapper.Insert<PAPApprovalRequestModel>(SubmitPAPQuery.InsertApprovalRequest, new DynamicParameters(new
                {
                    RequestNo,
                    Remark,
                    Status = (int)PRFSummaryStatus.Process,
                    ApprovalFlowId = ApprovalFlowId,
                    CreatedTime = DateTime.Now,
                    CreatedBy = creator.CreatorUserName,
                    RequestorAccountId = prf.RequestorAccountId,
                    RequestorUserName = prf.RequestorUserName,
                    RequestorEmail = prf.RequestorEmail,
                    CreatorUserName = creator.CreatorUserName,
                    CreatorEmail = creator.CreatorEmail,
                    CreatorAccountId = creator.CreatorAccountId,
                    CostCenterId = prf.CostCenterId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task UpdatePAP(PapApprovalModel request, int ApprovalRequestId)
        {
            try
            {
                await Task.FromResult(_iDapper.Execute(SubmitPAPQuery.UpdatePAP, new DynamicParameters(new
                {
                    Id = request.PAPId,
                    ApprovalRequestId,
                    Status = (int)PRFSummaryStatus.Process,
                    LastUpdatedBy = request.CreatedBy,
                    LastUpdatedTime = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<ApprovalRequestGroupProcSum> InsertApprovalRequestGroupPAP(PAPApprovalRequestGroupModel group, int ApprovalRequestId)
        {
            try
            {
                return await Task.FromResult(_iDapper.Insert<ApprovalRequestGroupProcSum>(SubmitPAPQuery.InsertApprovalRequestGroup, new DynamicParameters(new
                {
                    ApprovalRequestId,
                    group.ApprovalGroup_SubCategoryId,
                    group.Sequence,
                    group.Remark,
                    Status = group.Sequence == 1 ? (int)PRFSummaryStatus.Process : (int)PRFSummaryStatus.New,
                    group.CreatedBy,
                    DateNow = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<ApprovalRequestGroupMemberProcSum> InsertApprovalRequestGroupMemberPAP(PAPApprovalRequestGroupMemberModel Member, int ApprovalRequestId, int SquenceGroup)
        {
            try
            {
                return await Task.FromResult(_iDapper.Insert<ApprovalRequestGroupMemberProcSum>(SubmitPAPQuery.InsertApprovalRequestGroupMember, new DynamicParameters(new
                {
                    ApprovalRequestId,
                    Member.CostCenterId,
                    Member.ApprovalGroup_SubCategoryId,
                    Member.Sequence,
                    Member.Level,
                    Member.CreatedBy,
                    ApproverUsername = Member.UserName,
                    ApproverAccountId = Member.AccountId,
                    Status = SquenceGroup == 1 && Member.Level == 1 ? (int)PRFSummaryStatus.Process : (int)PRFSummaryStatus.New,
                    DateNow = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<ApprovalRequestGroupMemberProcSum> InsertApprovalRequestGroupMemberDAPPAP(PAPApprovalRequestGroupMemberModel Member, int ApprovalRequestId, int SquenceGroup)
        {
            try
            {
                return await Task.FromResult(_iDapper.Insert<ApprovalRequestGroupMemberProcSum>(SubmitPAPQuery.InsertApprovalRequestGroupMemberDAP, new DynamicParameters(new
                {
                    ApprovalRequestId,
                    Member.CostCenterId,
                    Member.ApprovalGroup_SubCategoryId,
                    Member.Sequence,
                    Member.Level,
                    Member.CreatedBy,
                    ApproverUsername = Member.UserName,
                    ApproverAccountId = Member.AccountId,
                    Status = SquenceGroup == 1 && Member.Level == 1 ? (int)PRFSummaryStatus.Process : (int)PRFSummaryStatus.New,
                    DateNow = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task InsertApprovalRequestItemPAP(int PAPId, int ApprovalRequestId, string CreatedBy)
        {

            try
            {
                await Task.FromResult(_iDapper.Execute(SubmitPAPQuery.InsertApprovalRequestItem, new DynamicParameters(new
                {
                    PAPId,
                    ApprovalRequestId,
                    Status = 1,
                    CreatedBy,
                    CreatedTime = DateTime.Now
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        #endregion

        #region Get PAP Number
        public async Task<CommonResponse> GetPAPNo(int PRFId)
        {
            CommonResponse response = new CommonResponse();
            try
            {
                string PAPNo = await Task.FromResult(_iDapper.Get<string>(ReferenceProcurmentBuyerQuery.SelectPAPNo, new DynamicParameters(new
                {
                    PRFId
                }), commandType: CommandType.Text));

                response = new CommonResponse
                {
                    Code = StatusCodes.Status200OK,
                    Status = _globalExceptions.StatusCode(StatusCodes.Status200OK),
                    Data = PAPNo
                };
            }
            catch (Exception ex)
            {
                response = new CommonResponse
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = _globalExceptions.StatusCode(response.Code),
                    Data = "Failed to PAP Number"
                };
                throw new GlobalExceptions(objectName, ex.InnerException);
            }

            return response;
        }
        #endregion

        #region Get Detail PAP
        public async Task<CommonResponse> GetDetailPAP(int PRFId)
        {
            CommonResponse response = new CommonResponse();

            try
            {
                PapModel PAP = await GetPAPSelectPAP(PRFId);
                if (PAP != null)
                {
                    PAP.Details = await GetPAPSelectPAPDetail((int)PAP.Id);
                    foreach (var detail in PAP.Details)
                    {
                        detail.OtherCosts = await GetPAPSelectPAPOtherCost((int)detail.Id);
                        detail.CostCenter = await GetPAPSelectPAPCostCenter((int)detail.Id);
                    }

                    response = new CommonResponse
                    {
                        Code = StatusCodes.Status200OK,
                        Status = _globalExceptions.StatusCode(StatusCodes.Status200OK),
                        Data = PAP
                    };
                }
                else
                {
                    response = new CommonResponse
                    {
                        Code = StatusCodes.Status404NotFound,
                        Status = _globalExceptions.StatusCode(StatusCodes.Status404NotFound),
                        Data = PAP
                    };
                }
            }
            catch (Exception ex)
            {
                response = new CommonResponse
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = _globalExceptions.StatusCode(response.Code),
                    Data = "Failed to Get Data PAP"
                };
                throw new GlobalExceptions(objectName, ex.InnerException);
            }

            return response;
        }
        private async Task<PapModel> GetPAPSelectPAP(int PRFId)
        {
            try
            {
                return await Task.FromResult(_iDapper.Get<PapModel>(GetDataPAPQuery.SelectPAP, new DynamicParameters(new
                {
                    PRFId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<PAPDetailModel>> GetPAPSelectPAPDetail(int PAPId)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<PAPDetailModel>(GetDataPAPQuery.SelectPAPDetail, new DynamicParameters(new
                {
                    PAPId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<PAPOtherCostModel>> GetPAPSelectPAPOtherCost(int PAPDetailId)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<PAPOtherCostModel>(GetDataPAPQuery.SelectPAPOtherCost, new DynamicParameters(new
                {
                    PAPDetailId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<PAPCostCenterModel> GetPAPSelectPAPCostCenter(int PAPDetailId)
        {
            try
            {
                return await Task.FromResult(_iDapper.Get<PAPCostCenterModel>(GetDataPAPQuery.SelectPAPCostCenter, new DynamicParameters(new
                {
                    PAPDetailId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }

        #endregion

        #region Get PAP Approver
        public async Task<CommonResponse> GetApprovalRequestGroupMemberPAP(int PAPId)
        {
            CommonResponse response = new CommonResponse();

            try
            {
                List<PAPApprovalRequestGroupMemberModel> ARGM = await Task.FromResult(_iDapper.GetAll<PAPApprovalRequestGroupMemberModel>(GetDataPAPQuery.SelectApprovalRequestGroupMember, new DynamicParameters(new
                {
                    PAPId
                }), commandType: CommandType.Text));

                response = new CommonResponse
                {
                    Code = StatusCodes.Status200OK,
                    Status = _globalExceptions.StatusCode(StatusCodes.Status200OK),
                    Data = ARGM
                };
            }
            catch (Exception ex)
            {
                response = new CommonResponse
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = _globalExceptions.StatusCode(response.Code),
                    Data = "Failed to Get Approver PAP"
                };
                throw new GlobalExceptions(objectName, ex.InnerException);
            }

            return response;
        }
        #endregion

        #endregion
        #region Get Approver
        public async Task<CommonResponse> GetApprovalRequestGroupMemberProcsum(int PRFId)
        {
            CommonResponse response = new CommonResponse();

            try
            {
                List<ApprovalProcsumProcBuyerModel> approvalFlows = await SelectApprovalGroupProcbuyer(PRFId);

                var tasks = approvalFlows.Select(async flow =>
                {
                    flow.Members = flow.SubCategoryCode switch
                    {
                        "GA-0008" => await SelectApprovalRequestGroupMemberDAPProcbuyer(PRFId, flow.SubCategoryCode),
                        _ => await SelectApprovalRequestGroupMemberProcbuyer(PRFId, flow.SubCategoryCode)
                    };
                    return flow;
                });

                var updatedFlows = await Task.WhenAll(tasks);

                response = new CommonResponse
                {
                    Code = StatusCodes.Status200OK,
                    Status = _globalExceptions.StatusCode(StatusCodes.Status200OK),
                    Data = updatedFlows
                };
            }
            catch (Exception ex)
            {
                response = new CommonResponse
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = _globalExceptions.StatusCode(response.Code),
                    Data = "Failed to Get Approver PAP"
                };
                throw new GlobalExceptions(objectName, ex.InnerException);
            }

            return response;
        }
        private async Task<List<ApprovalProcsumProcBuyerModel>> SelectApprovalGroupProcbuyer(int PRFId)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<ApprovalProcsumProcBuyerModel>(ProcurmentBuyerQuery.SelectApprovalRequestGroup, new DynamicParameters(new
                {
                    PRFId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<ARGMApprovalProcsumProcBuyerModel>> SelectApprovalRequestGroupMemberProcbuyer(int PRFId, string SubCategoryCode)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<ARGMApprovalProcsumProcBuyerModel>(ProcurmentBuyerQuery.SelectApprovalRequestGroupMember, new DynamicParameters(new
                {
                    PRFId,
                    SubCategoryCode
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<ARGMApprovalProcsumProcBuyerModel>> SelectApprovalRequestGroupMemberDAPProcbuyer(int PRFId, string SubCategoryCode)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<ARGMApprovalProcsumProcBuyerModel>(ProcurmentBuyerQuery.SelectApprovalRequestGroupMemberDAP, new DynamicParameters(new
                {
                    PRFId,
                    SubCategoryCode
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        public async Task<CommonResponse> GetApproverProcsum(string prfNumber)
        {
            try
            {
                var approvalFlows = await SelectApprovalFlow("FlowProcsum");
                int SequenceDAP = approvalFlows!.Find(e => e.SubCategoryCode == "GA-0008" || e.SubCategoryCode == "ApprovalBudgeted")!.Sequence;
                int SequenceCommitte = approvalFlows!.Find(e => e.SubCategoryCode == "SC-2025-06-01448")!.Sequence;
                int SequenceNonBudget = approvalFlows!.Find(e => e.SubCategoryCode == "Non-Shoppingcart-PRF-Non-Budget")!.Sequence;

                var tasks = approvalFlows.Select(async flow =>
                {
                    flow.ApprovalGroups = flow.SubCategoryCode switch
                    {
                        "SC-2024-02-01267" => await SelectApprovalGroupBuyer(prfNumber),
                        "SC-2023-08-11141" => await SelectApprovalGroupWithPRFNumber(flow.ID, prfNumber),
                        "SC-2024-02-01263" => await SelectApprovalGroupRequestor(prfNumber),
                        "GA-0008" => await SelectApprovalGroupDAP(flow.ID, prfNumber),
                        "ApprovalBudgeted" => await SelectApprovalGroupDAP(flow.ID, prfNumber),
                        _ => flow.Sequence != SequenceDAP ? await SelectApprovalGroup(flow.ID) : await SelectApprovalGroupDAP(flow.ID, prfNumber)
                    };
                    return flow;
                });

                var updatedFlows = await Task.WhenAll(tasks);

                var approvalFlowsFilter = updatedFlows
                    .GroupBy(e => e.Sequence)
                    .Select(g =>
                    {
                        if (g.Count() == 1)
                        {
                            return g.First();
                        }
                        return g.FirstOrDefault(x => x.ApprovalGroups != null);
                    })
                    .Where(x => x != null)
                    .ToList();

                ProcurmentSummaryValidaationApprovalModel PRF = await GetValidationPRF(prfNumber);
                if (PRF.SubCategoryCode == "SC-2023-08-11136" || PRF.SubCategoryCode == "SC-2023-08-11133")
                {
                    approvalFlowsFilter.RemoveAll(obj => obj.Sequence == SequenceDAP);
                }
                if (!(PRF.isRiskAssementForm || (PRF.GrandTotal >= 2000000000 && !PRF.VendorStatus)))
                {
                    approvalFlowsFilter.RemoveAll(obj => obj.Sequence == SequenceCommitte);
                }
                if (PRF.IsBudgetedSpend)
                {
                    approvalFlowsFilter.RemoveAll(obj => obj.Sequence == SequenceNonBudget);
                }

                return new CommonResponse
                {
                    Code = StatusCodes.Status200OK,
                    Status = _globalExceptions.StatusCode(StatusCodes.Status200OK),
                    Data = approvalFlowsFilter
                };
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException ?? ex);
            }
        }
        public async Task<CommonResponse> GetApproverPAP(string prfNumber)
        {
            try
            {
                var approvalFlows = await SelectApprovalFlow("FlowPAP");

                var tasks = approvalFlows.Select(async flow =>
                {
                    flow.ApprovalGroups = flow.SubCategoryCode switch
                    {
                        "SC-2024-02-01267" => await SelectApprovalGroupBuyer(prfNumber),
                        "SC-2023-08-11141" => await SelectApprovalGroupWithPRFNumber(flow.ID, prfNumber),
                        "SC-2024-02-01263" => await SelectApprovalGroupRequestor(prfNumber),
                        "GA-0008" => await SelectApprovalGroupDAP(flow.ID, prfNumber),
                        _ => await SelectApprovalGroup(flow.ID)
                    };
                    return flow;
                });

                var updatedFlows = await Task.WhenAll(tasks);

                var approvalFlowsFilter = updatedFlows
                    .GroupBy(e => e.Sequence)
                    .Select(g =>
                    {
                        if (g.Count() == 1)
                        {
                            return g.First();
                        }
                        return g.FirstOrDefault(x => x.ApprovalGroups != null);
                    })
                    .Where(x => x != null)
                    .ToList();

                ProcurmentSummaryValidaationApprovalModel PRF = await GetValidationPRF(prfNumber);
                int SequenceNonBudget = approvalFlows!.Find(e => e.SubCategoryCode == "Non-Shoppingcart-PRF-Non-Budget")!.Sequence;
                if (PRF.IsBudgetedSpend)
                {
                    approvalFlowsFilter.RemoveAll(obj => obj.Sequence == SequenceNonBudget);
                }

                return new CommonResponse
                {
                    Code = StatusCodes.Status200OK,
                    Status = _globalExceptions.StatusCode(StatusCodes.Status200OK),
                    Data = approvalFlowsFilter
                };
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException ?? ex);
            }
        }
        private async Task<List<ApprovalFlowPAP>> SelectApprovalFlow(string Name)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<ApprovalFlowPAP>(ProcurmentBuyerQuery.SelectApprovalFlow, new DynamicParameters(new
                {
                    Name
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<ApprovalGroupPAP>> SelectApprovalGroup(int ApprovalGroup_SubCategoryId)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<ApprovalGroupPAP>(ProcurmentBuyerQuery.SelectApprovalGroup, new DynamicParameters(new
                {
                    ApprovalGroup_SubCategoryId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<ApprovalGroupPAP>> SelectApprovalGroupDAP(int ApprovalGroup_SubCategoryId, string PRFNumber)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<ApprovalGroupPAP>(ProcurmentBuyerQuery.SelectApprovalGroupDAP, new DynamicParameters(new
                {
                    PRFNumber,
                    ApprovalGroup_SubCategoryId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<ApprovalGroupPAP>> SelectApprovalGroupWithMinimumAmmount(int ApprovalGroup_SubCategoryId, string PRFNumber)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<ApprovalGroupPAP>(ProcurmentBuyerQuery.SelectApprovalGroupWithMinimumAmmount, new DynamicParameters(new
                {
                    PRFNumber,
                    ApprovalGroup_SubCategoryId
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<ApprovalGroupPAP>> SelectApprovalGroupWithPRFNumber(int ApprovalGroup_SubCategoryId, string PRFNumber)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<ApprovalGroupPAP>(ProcurmentBuyerQuery.SelectApprovalGroupWithPRFNumber, new DynamicParameters(new
                {
                    ApprovalGroup_SubCategoryId,
                    PRFNumber
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<ApprovalGroupPAP>> SelectApprovalGroupBuyer(string PRFNumber)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<ApprovalGroupPAP>(ProcurmentBuyerQuery.SelectApprovalGroupBuyer, new DynamicParameters(new
                {
                    PRFNumber
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<List<ApprovalGroupPAP>> SelectApprovalGroupRequestor(string PRFNumber)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<ApprovalGroupPAP>(ProcurmentBuyerQuery.SelectApprovalGroupRequestor, new DynamicParameters(new
                {
                    PRFNumber
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        private async Task<ProcurmentSummaryValidaationApprovalModel> GetValidationPRF(string PRFNumber)
        {
            try
            {
                return await Task.FromResult(_iDapper.Get<ProcurmentSummaryValidaationApprovalModel>(ProcurmentBuyerQuery.GetValidationPRF, new DynamicParameters(new
                {
                    PRFNumber
                }), commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                throw new GlobalExceptions(objectName, ex.InnerException);
            }
        }
        #endregion
        #region Reference
        public async Task<List<PAPPRFVendorQuotationDetail>> SelectPRFVendorQuotationDetailByPRFId(int PRFId)
        {
            try
            {
                return await Task.FromResult(_iDapper.GetAll<PAPPRFVendorQuotationDetail>(ProcurmentBuyerQuery.SelectPRFVendorQuotationDetail, new DynamicParameters(new
                {
                    PRFId
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
