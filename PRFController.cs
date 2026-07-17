using APS_Common;
using APS_LogHistory.FilterLogger;
using APS_REST_API.Contracts;
using APS_REST_API.Models;
using APS_REST_API.Models.NonShoppingCart;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Linq;
using System.Reflection;
using static APS_REST_API.Models.PurchaseRequestFormModel;

namespace APS_REST_API.Controllers.NonShoppingCart;

[Route("v1")]
[ApiController]
public class PrfController : ControllerBase
{

    private readonly IPrfRepository _iPrfRepository;
    private readonly IBudgetTransactionRepository _iBudgetTransactionRepository;
    private readonly GlobalExceptions _globalExceptions = new();

    /// <summary>
    /// Logging My Request PRF Controller
    /// </summary>
    /// <returns></returns>
    private readonly Logging _logging = new() { objectName = nameof(PrfController) };

    public PrfController(
        IPrfRepository prfRepository,
        IBudgetTransactionRepository budgetTransactionRepository)
    {
        try
        {
            _iPrfRepository = prfRepository;
            _iBudgetTransactionRepository = budgetTransactionRepository;
            _logging.LogInitialize(_logging.objectName, null, LogType.Info);
        }
        catch (Exception e)
        {
            _logging.LogInitialize(_logging.objectName, _globalExceptions.StatusData(e), LogType.Error);
        }
    }

    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost("MyRequest/Index/NonShoppingCart/DataTables")]
    [ProducesResponseType<GetListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<GetListResponse>(StatusCodes.Status400BadRequest)]
    public IActionResult MyRequestIndexNonShoppingCartDataTables(APS_Common.Models.MyRequest.Index.NonShoppingCart.DataTables.Request request)
    {
        string methodName = MethodBase.GetCurrentMethod().Name;
        try
        {
            var response = _iPrfRepository.MyRequestIndexNonShoppingCartDataTables(request);
            if (!string.IsNullOrWhiteSpace(response.Error))
            {
                _logging.LogPagination(methodName, request, request.Start, request.Length, LogType.Error);
                return BadRequest(response);
            }
            return Ok(response);
        }
        catch (Exception e)
        {
            _logging.LogPagination(methodName, request, request.Start, request.Length, LogType.Error);
            _logging.LogPagination(methodName, e, request.Start, request.Length, LogType.Error);
            APS_Common.Models.DataTables.Response<APS_Common.Models.MyRequest.Index.NonShoppingCart.DataTables.Row> response = new() { Draw = request.Draw, Error = e.Message };
            return BadRequest(response);
        }
    }

    /// <summary>
    /// GetPRFList
    /// </summary>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <param name="prfNumber"></param>
    /// <param name="businessUnitId"></param>
    /// <param name="requestDateFrom"></param>
    /// <param name="requestDateTo"></param>
    /// <param name="status"></param>
    /// <param name="costCenterId"></param>
    /// <param name="fieldOrder"></param>
    /// <param name="order"></param>
    /// <param name="requestor"></param>
    /// <param name="approver"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet("prf/purchaserequestform/list/myapproval")]
    [ProducesResponseType<GetListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<GetListResponse>(StatusCodes.Status400BadRequest)]
    public IActionResult GetPRFListMyApproval(
        int page,
        int pageSize,
        string prfNumber,
        long? businessUnitId,
        string requestDateFrom,
        string requestDateTo,
        int? status,
        long? costCenterId,
        int? fieldOrder,
        string order,
        string requestor,
        string approver)
    {
        try
        {
            var data = _iPrfRepository.GetListPRFMyApproval(page, pageSize, prfNumber, businessUnitId, requestDateFrom, requestDateTo, status, costCenterId, fieldOrder, order, requestor, approver).Result.ToList();
            int totalCount = data.Count;

            if (!string.IsNullOrEmpty(prfNumber) ||
                businessUnitId > 0 ||
                !string.IsNullOrEmpty(requestDateFrom) ||
                !string.IsNullOrEmpty(requestDateTo) ||
                status >= 0 ||
                costCenterId > 0)
            {
                totalCount = data.Count;
            }

            int totalPage = 0;
            totalPage = (int)Math.Round((decimal)totalCount / (decimal)pageSize);
            if (totalPage == 0)
                totalPage = 1;

            GetListResponse glr = new();
            glr.Code = StatusCodes.Status200OK;
            glr.Status = _globalExceptions.StatusCode(glr.Code);
            glr.RecordTotal = totalCount;
            glr.RecordPage = totalPage;
            glr.Data = data;
            _logging.LogPagination(_logging.objectName, null, page, pageSize, LogType.Info);
            return Ok(glr);
        }
        catch (Exception e)
        {
            GetListResponse glr = new();
            glr.Code = StatusCodes.Status400BadRequest;
            glr.Status = _globalExceptions.StatusCode(glr.Code);
            glr.Data = _globalExceptions.StatusData(e);
            _logging.LogPagination(_logging.objectName, glr.Data, page, pageSize, LogType.Error);
            return BadRequest(glr);
        }
    }

    /// <summary>
    /// GetPRFByPRFNumber
    /// </summary>
    /// <param name="prfNumber"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet("prf/purchaserequestform/byprfnumber/{prfNumber}")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status404NotFound)]
    public IActionResult GetPRFByPRFNumber(string prfNumber)
    {
        string paramName = nameof(prfNumber);
        try
        {
            var t = _iPrfRepository.GetPRFByNumber(prfNumber);
            CommonResponse cr = new();
            if (t.Result == null)
            {
                cr.Code = StatusCodes.Status404NotFound;
                cr.Status = _globalExceptions.StatusCode(cr.Code);
                _logging.LogDetail(_logging.objectName, null, paramName, prfNumber, LogType.Warning);
                return NotFound(cr);
            }
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogDetail(_logging.objectName, null, paramName, prfNumber, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogDetail(_logging.objectName, cr.Data, paramName, prfNumber, LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// GetApproverPRFListByCostCenterId
    /// </summary>
    /// <param name="costCenterId"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet("prf/approverprflist/{costCenterId}")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status404NotFound)]
    public IActionResult GetApproverPRFListByCostCenterId(long costCenterId)
    {
        string methodName = MethodBase.GetCurrentMethod().Name;
        string paramName = nameof(costCenterId);
        try
        {
            var t = _iPrfRepository.GetApproverPRFListByCostCenterId(costCenterId);
            CommonResponse cr = new();
            if (t.Result == null)
            {
                cr.Code = StatusCodes.Status404NotFound;
                cr.Status = _globalExceptions.StatusCode(cr.Code);
                _logging.LogDetail(methodName, null, paramName, costCenterId.ToString(), LogType.Warning);
                return NotFound(cr);
            }
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogDetail(methodName, null, paramName, costCenterId.ToString(), LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogDetail(methodName, cr.Data, paramName, costCenterId.ToString(), LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// GetApproverPRFListByApprovalRequestGroupMemberId
    /// </summary>
    /// <param name="approvalRequestGroupMemberId"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet($"prf/{nameof(GetApproverPRFListByApprovalRequestGroupMemberId)}/{{{nameof(approvalRequestGroupMemberId)}}}")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status404NotFound)]
    public IActionResult GetApproverPRFListByApprovalRequestGroupMemberId(long approvalRequestGroupMemberId)
    {
        string methodName = MethodBase.GetCurrentMethod().Name;
        try
        {
            var t = _iPrfRepository.GetApproverPRFListByApprovalRequestGroupMemberId(approvalRequestGroupMemberId);
            CommonResponse cr = new();
            if (t.Result == null)
            {
                cr.Code = StatusCodes.Status404NotFound;
                cr.Status = _globalExceptions.StatusCode(cr.Code);
                _logging.LogDetail(methodName, null, nameof(approvalRequestGroupMemberId), approvalRequestGroupMemberId.ToString(), LogType.Warning);
                return NotFound(cr);
            }
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogDetail(methodName, null, nameof(approvalRequestGroupMemberId), approvalRequestGroupMemberId.ToString(), LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogDetail(methodName, cr.Data, nameof(approvalRequestGroupMemberId), approvalRequestGroupMemberId.ToString(), LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// GetSpendingCategoryList
    /// </summary>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet("prf/spendingcategory/all")]
    [ProducesResponseType<GetListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<GetListResponse>(StatusCodes.Status400BadRequest)]
    public IActionResult GetSpendingCategoryList()
    {
        try
        {
            var data = _iPrfRepository.GetListSpendingCategory().Result.ToList();
            int totalCountData = data.Count;
            GetListResponse glr = new();
            glr.Code = StatusCodes.Status200OK;
            glr.Status = _globalExceptions.StatusCode(glr.Code);
            glr.RecordTotal = totalCountData;
            glr.RecordPage = totalCountData;
            glr.Data = data;
            _logging.LogInitialize(_logging.objectName, null, LogType.Info);
            return Ok(glr);
        }
        catch (Exception e)
        {
            GetListResponse glr = new();
            glr.Code = StatusCodes.Status400BadRequest;
            glr.Status = _globalExceptions.StatusCode(glr.Code);
            glr.Data = _globalExceptions.StatusData(e);
            _logging.LogInitialize(_logging.objectName, glr.Data, LogType.Error);
            return BadRequest(glr);
        }
    }

    /// <summary>
    /// GetSpendingSubCategoryListBySpendingCategoryId
    /// </summary>
    /// <param name="spendingCategoryId"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet("prf/spendingsubcategory/{spendingCategoryId}")]
    [ProducesResponseType<GetListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<GetListResponse>(StatusCodes.Status400BadRequest)]
    public IActionResult GetSpendingSubCategoryListBySpendingCategoryId(long spendingCategoryId)
    {
        try
        {
            var data = _iPrfRepository.GetListSpendingSubCategoryBySpendingCategoryId(spendingCategoryId).Result.ToList();
            int totalCountData = data.Count;
            GetListResponse glr = new();
            glr.Code = StatusCodes.Status200OK;
            glr.Status = _globalExceptions.StatusCode(glr.Code);
            glr.RecordTotal = totalCountData;
            glr.RecordPage = totalCountData;
            glr.Data = data;
            _logging.LogInitialize(_logging.objectName, null, LogType.Info);
            return Ok(glr);
        }
        catch (Exception e)
        {
            GetListResponse glr = new();
            glr.Code = StatusCodes.Status400BadRequest;
            glr.Status = _globalExceptions.StatusCode(glr.Code);
            glr.Data = _globalExceptions.StatusData(e);
            _logging.LogInitialize(_logging.objectName, glr.Data, LogType.Error);
            return BadRequest(glr);
        }
    }

    /// <summary>
    /// GetPRFApprovalByPRFNumber
    /// </summary>
    /// <param name="prfNumber"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet("prf/approval/byprfnumber/{prfNumber}")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status404NotFound)]
    public IActionResult GetPRFApprovalByPRFNumber(string prfNumber)
    {
        string paramName = nameof(prfNumber);
        try
        {
            var t = _iPrfRepository.GetPRFApprovalByPRFNumber(prfNumber);
            CommonResponse cr = new();
            if (t.Result == null)
            {
                cr.Code = StatusCodes.Status404NotFound;
                cr.Status = _globalExceptions.StatusCode(cr.Code);
                _logging.LogDetail(_logging.objectName, null, paramName, prfNumber, LogType.Warning);
                return NotFound(cr);
            }
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogDetail(_logging.objectName, null, paramName, prfNumber, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogDetail(_logging.objectName, cr.Data, paramName, prfNumber, LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// GetHistoryPRFApprovalByPRFNumber
    /// </summary>
    /// <param name="prfNumber"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet("prf/historyapproval/byprfnumber/{prfNumber}")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status404NotFound)]
    public IActionResult GetHistoryPRFApprovalByPRFNumber(string prfNumber)
    {
        string paramName = nameof(prfNumber);
        try
        {
            var t = _iPrfRepository.GetHistoryPRFApprovalByPRFNumber(prfNumber);
            CommonResponse cr = new();
            if (t.Result == null)
            {
                cr.Code = StatusCodes.Status404NotFound;
                cr.Status = _globalExceptions.StatusCode(cr.Code);
                _logging.LogDetail(_logging.objectName, null, paramName, prfNumber, LogType.Warning);
                return NotFound(cr);
            }
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogDetail(_logging.objectName, null, paramName, prfNumber, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogDetail(_logging.objectName, cr.Data, paramName, prfNumber, LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// GetApprovalRequestGroupMemberHistory
    /// </summary>
    /// <param name="approvalRequestId"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet("prf/historyapprovalmember/approvalrequestid/{approvalRequestId}")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status404NotFound)]
    public IActionResult GetApprovalRequestGroupMemberHistory(long approvalRequestId)
    {
        string paramName = nameof(approvalRequestId);
        try
        {
            var t = _iPrfRepository.GetApprovalRequestGroupMemberHistory(approvalRequestId);
            CommonResponse cr = new();
            if (t.Result == null)
            {
                cr.Code = StatusCodes.Status404NotFound;
                cr.Status = _globalExceptions.StatusCode(cr.Code);
                _logging.LogDetail(_logging.objectName, null, paramName, approvalRequestId.ToString(), LogType.Warning);
                return NotFound(cr);
            }
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogDetail(_logging.objectName, null, paramName, approvalRequestId.ToString(), LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogDetail(_logging.objectName, cr.Data, paramName, approvalRequestId.ToString(), LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// AddPRF
    /// </summary>
    /// <param name="prfInput"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost("prf/purchaserequestform/addpurchaserequestform")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    [EnableRateLimiting("IpRateLimiting")]
    public IActionResult AddPRF(PurchaseRequestFormModel.PurchaseRequestForm prfInput)
    {
        try
        {
            var t = _iPrfRepository.AddPurchaseRequestForm(prfInput);
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogInsert(_logging.objectName, cr.Data, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogInsert(_logging.objectName, cr.Data, LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// UpdatePRF
    /// </summary>
    /// <param name="prfInput"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost("prf/purchaserequestform/updatepurchaserequestform")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    public IActionResult UpdatePRF(PurchaseRequestFormModel.PurchaseRequestForm prfInput)
    {
        try
        {
            var t = _iPrfRepository.UpdatePurchaseRequestForm(prfInput);
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogUpdate(_logging.objectName, cr.Data, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogUpdate(_logging.objectName, cr.Data, LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// CancelPRF
    /// </summary>
    /// <param name="prfNumber"></param>
    /// <param name="lastUpdatedBy"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost("prf/purchaserequestform/cancelpurchaserequestform/{prfNumber}/{lastUpdatedBy}")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    public IActionResult CancelPRF(string prfNumber, string lastUpdatedBy)
    {
        try
        {
            var t = _iPrfRepository.CancelPurchaseRequestForm(prfNumber, lastUpdatedBy);
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogUpdate(_logging.objectName, cr.Data, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogUpdate(_logging.objectName, cr.Data, LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// AddPRFApproval
    /// </summary>
    /// <param name="approvalRequest"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost("prf/purchaserequestform/addpurchaserequestformapproval")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    public IActionResult AddPRFApproval(ApprovalRequestModel.ApprovalRequestPrf approvalRequest)
    {
        try
        {
            var t = _iPrfRepository.AddPRFApproval(approvalRequest);
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogInsert(_logging.objectName, cr.Data, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogInsert(_logging.objectName, cr.Data, LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// UpdatePRFApproval
    /// </summary>
    /// <param name="approvalRequestInput"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost("prf/purchaserequestform/updatepurchaserequestformapproval")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    public IActionResult UpdatePRFApproval(ApprovalRequestUpdateModel.ApprovalRequestUpdate approvalRequestInput)
    {
        try
        {
            var t = _iPrfRepository.UpdateApprovalRequestBySP(approvalRequestInput);
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogUpdate(_logging.objectName, cr.Data, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogUpdate(_logging.objectName, cr.Data, LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// SwitchPRFApproval
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost("prf/purchaserequestform/switchpurchaserequestformapproval")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    public IActionResult SwitchPRFApproval(MyRequestModel.SwitchApprovalPRFRequest request)
    {
        try
        {
            var t = _iPrfRepository.SwitchPRFApproval(request);
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogUpdate(_logging.objectName, cr.Data, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogUpdate(_logging.objectName, cr.Data, LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// GetPrfHistoryTracking
    /// </summary>
    /// <param name="approvalRequestId"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet("prf/purchaserequestform/history/{RequestNumber}")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status404NotFound)]
    public IActionResult GetPrfHistoryTracking(string RequestNumber)
    {
        string paramName = "RequestNumber";
        try
        {
            var t = _iPrfRepository.GetListHistoryTracking(RequestNumber);
            CommonResponse cr = new();
            if (t.Result == null)
            {
                cr.Code = StatusCodes.Status404NotFound;
                cr.Status = _globalExceptions.StatusCode(cr.Code);
                _logging.LogDetail(_logging.objectName, null, paramName, RequestNumber.ToString(), LogType.Warning);
                return NotFound(cr);
            }
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogDetail(_logging.objectName, null, paramName, RequestNumber.ToString(), LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogDetail(_logging.objectName, cr.Data, paramName, RequestNumber.ToString(), LogType.Error);
            return BadRequest(cr);
        }
    }


    #region Changes For Budget
    /// <summary>
    /// AddPRFApproval
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost("prf/purchaserequestform/submitapprovalmatrixprf")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    public IActionResult SubmitApprovalMatrixPrf([FromBody] ApprovalBudgetPrfRequest param)
    {
        try
        {
            var data = _iPrfRepository.SubmitApprovalMatrixPrf(param).Result;
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = data;
            _logging.LogInsert(_logging.objectName, cr.Data, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogInsert(_logging.objectName, cr.Data, LogType.Error);
            return BadRequest(cr);
        }
    }

    /// <summary>
    /// GetPRFApprovalByPRFNumber
    /// </summary>
    /// <param name="prfNumber"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet("prf/approval-budget/byprfnumber/{prfNumber}")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status404NotFound)]
    public IActionResult GetPRFApprovalBudgetByPRFNumber(string prfNumber)
    {
        string paramName = nameof(prfNumber);
        try
        {
            var t = _iPrfRepository.GetPRFApprovalBudgetByPRFNumber(prfNumber);
            CommonResponse cr = new();
            if (t.Result == null)
            {
                cr.Code = StatusCodes.Status404NotFound;
                cr.Status = "Draft Status";
                _logging.LogDetail(_logging.objectName, null, paramName, prfNumber, LogType.Warning);
                return NotFound(cr);
            }
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogDetail(_logging.objectName, null, paramName, prfNumber, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogDetail(_logging.objectName, cr.Data, paramName, prfNumber, LogType.Error);
            return BadRequest(cr);
        }
    }

    #endregion

    /// <summary>
    /// UpdateBudgetCode
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost("prf/budgetcode/updatebudgetcode")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    public IActionResult UpdateBudgetCode(BudgetCodeUpdateModel param)
    {
        try
        {
            var t = _iBudgetTransactionRepository.UpdateBudgetCodeNonShoppingCart(param, "prf");
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status200OK;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = t.Result;
            _logging.LogUpdate(_logging.objectName, cr.Data, LogType.Info);
            return Ok(cr);
        }
        catch (Exception e)
        {
            CommonResponse cr = new();
            cr.Code = StatusCodes.Status400BadRequest;
            cr.Status = _globalExceptions.StatusCode(cr.Code);
            cr.Data = _globalExceptions.StatusData(e);
            _logging.LogUpdate(_logging.objectName, cr.Data, LogType.Error);
            return BadRequest(cr);
        }
    }

    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost(APS_Common.Const.Routes.PRF_Detail_Model)]
    [ProducesResponseType<APS_Common.Models.PRF.Detail.Model.Response>(StatusCodes.Status200OK)]
    public IActionResult PRF_Detail_Model(APS_Common.Models.PRF.Detail.Model.Request request)
    {
        try
        {
            var response = _iPrfRepository.PRF_Detail_Model(request);
            if (!string.IsNullOrWhiteSpace(response.Exception))
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        catch (Exception e)
        {
            var response = new APS_Common.Models.PRF.Detail.Model.Response() { Exception = e.Message };
            return BadRequest(response);
        }
    }

    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost(APS_Common.Const.Routes.PRF_Detail_PRF_Save)]
    [ProducesResponseType<APS_Common.Models.PRF.Detail.PRF.Save.Response>(StatusCodes.Status200OK)]
    public IActionResult PRF_Detail_PRF_Save([FromForm] APS_Common.Models.PRF.Detail.PRF.Save.Request request)
    {
        try
        {
            APS_Common.Models.PRF.Detail.PRF.Save.Response response = ((request.PRF.Id ?? 0) == 0)
                ? _iPrfRepository.PRF_Detail_PRF_Save_Create(request)
                : _iPrfRepository.PRF_Detail_PRF_Save_Edit(request);
            if (!string.IsNullOrWhiteSpace(response.Exception))
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        catch (Exception e)
        {
            var response = new APS_Common.Models.PRF.Detail.PRF.Save.Response() { Exception = e.Message };
            return BadRequest(response);
        }
    }

    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost(APS_Common.Const.Routes.PRF_Detail_SecurityAssessmentTable_Save)]
    [ProducesResponseType<APS_Common.Models.PRF.Detail.SecurityAssessmentTable.Save.Response>(StatusCodes.Status200OK)]
    public IActionResult PRF_Detail_SecurityAssessmentTable_Save([FromBody] APS_Common.Models.PRF.Detail.SecurityAssessmentTable.Save.Request request)
    {
        try
        {
            var response = _iPrfRepository.PRF_Detail_SecurityAssessmentTable_Save(request);
            if (!string.IsNullOrWhiteSpace(response.Exception))
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        catch (Exception e)
        {
            var response = new APS_Common.Models.PRF.Detail.SecurityAssessmentTable.Save.Response() { Exception = e.Message };
            return BadRequest(response);
        }
    }

    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost(APS_Common.Const.Routes.PRF_Detail_Document_Save)]
    [ProducesResponseType<APS_Common.Models.PRF.Detail.Document.Save.Response>(StatusCodes.Status200OK)]
    public IActionResult PRF_Detail_Document_Save([FromForm] APS_Common.Models.PRF.Detail.Document.Save.Request request)
    {
        try
        {
            var response = _iPrfRepository.PRF_Detail_Document_Save(request);
            if (!string.IsNullOrWhiteSpace(response.Exception))
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        catch (Exception e)
        {
            var response = new APS_Common.Models.PRF.Detail.Document.Save.Response() { Exception = e.Message };
            return BadRequest(response);
        }
    }

    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost(APS_Common.Const.Routes.PRF_Detail_Approver_Cancel)]
    [ProducesResponseType<APS_Common.Models.PRF.Detail.Approver.Cancel.Response>(StatusCodes.Status200OK)]
    public IActionResult PRF_Detail_Approver_Cancel([FromBody] APS_Common.Models.PRF.Detail.Approver.Cancel.Request request)
    {
        try
        {
            var response = _iPrfRepository.PRF_Detail_Approver_Cancel(request);
            if (!string.IsNullOrWhiteSpace(response.ExceptionMessage)) throw new GlobalExceptions(response.ExceptionMessage);
            return Ok(response);
        }
        catch (Exception e)
        {
            APS_Common.Models.PRF.Detail.Approver.Member.Submit.Response response = new();
            GlobalExceptions ge = new(e.Message, e);
            response.ExceptionMessage ??= ge.message ?? ge.Message;
            return BadRequest(response);
        }
    }

    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost(APS_Common.Const.Routes.PRF_Detail_Approver_Member_Submit)]
    [ProducesResponseType<APS_Common.Models.PRF.Detail.Approver.Member.Submit.Response>(StatusCodes.Status200OK)]
    public IActionResult PRF_Detail_Approver_Member_Submit([FromBody] APS_Common.Models.PRF.Detail.Approver.Member.Submit.Request request)
    {
        try
        {
            var response = _iPrfRepository.PRF_Detail_Approver_Member_Submit(request);
            if (!string.IsNullOrWhiteSpace(response.ExceptionMessage)) throw new GlobalExceptions(response.ExceptionMessage);
            return Ok(response);
        }
        catch (Exception e)
        {
            APS_Common.Models.PRF.Detail.Approver.Member.Submit.Response response = new();
            GlobalExceptions ge = new(e.Message, e);
            response.ExceptionMessage ??= ge.message ?? ge.Message;
            return BadRequest(response);
        }
    }

    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost(APS_Common.Const.Routes.PRF_Detail_Approver_Approval_Submit)]
    [ProducesResponseType<APS_Common.Models.PRF.Detail.Approver.Approval.Submit.Response>(StatusCodes.Status200OK)]
    public IActionResult PRF_Detail_Approver_Approval_Submit([FromBody] APS_Common.Models.PRF.Detail.Approver.Approval.Submit.Request request)
    {
        try
        {
            var response = _iPrfRepository.PRF_Detail_Approver_Approval_Submit(request);
            if (!string.IsNullOrWhiteSpace(response.ExceptionMessage)) throw new GlobalExceptions(response.ExceptionMessage);
            return Ok(response);
        }
        catch (Exception e)
        {
            APS_Common.Models.PRF.Detail.Approver.Approval.Submit.Response response = new();
            GlobalExceptions ge = new(e.Message, e);
            response.ExceptionMessage ??= ge.message ?? ge.Message;
            return BadRequest(response);
        }
    }


    #region New CR
    /// <summary>
    /// GetDetailGR
    /// </summary>
    /// <param name="RequestNumber"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpGet("prf/gr/{RequestNumber}")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status404NotFound)]

    public IActionResult GetDetailGR(string RequestNumber)
    {
        CommonResponse common = new();

        try
        {
            var t = _iPrfRepository.GetDetailGR(RequestNumber);
            common.Code = StatusCodes.Status200OK;
            common.Status = _globalExceptions.StatusCode(common.Code);
            common.Data = t.Result;
            _logging.LogUpdate(_logging.objectName, common.Data, LogType.Info);
            return Ok(common);
        }
        catch (Exception e)
        {
            common.Code = StatusCodes.Status400BadRequest;
            common.Status = _globalExceptions.StatusCode(common.Code);
            common.Data = _globalExceptions.StatusData(e);
            _logging.LogDetail(_logging.objectName, common.Data, nameof(RequestNumber), RequestNumber, LogType.Error);
            return BadRequest(common);
        }
    }

    /// <summary>
    /// GetPRFApprovalByPRFNumber
    /// </summary>
    /// <param name="RequestNumber"></param>
    /// <returns></returns>
    [TypeFilter(typeof(LoggerApiAttribute))]
    [HttpPost("prf/gr/update")]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommonResponse>(StatusCodes.Status404NotFound)]

    public IActionResult UpdateGR(GRRequest request)
    {
        CommonResponse common = new();

        try
        {
            var t = _iPrfRepository.UpdateGr(request);
            common.Code = StatusCodes.Status200OK;
            common.Status = _globalExceptions.StatusCode(common.Code);
            common.Data = t.Result;
            _logging.LogUpdate(_logging.objectName, common.Data, LogType.Info);
            return Ok(common);
        }
        catch (Exception e)
        {
            common.Code = StatusCodes.Status400BadRequest;
            common.Status = _globalExceptions.StatusCode(common.Code);
            common.Data = _globalExceptions.StatusData(e);
            _logging.LogDetail(_logging.objectName, common.Data, nameof(request), request.ToString(), LogType.Error);
            return BadRequest(common);
        }
    }
    #endregion
}
