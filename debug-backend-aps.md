using GlobalExceptions = APS_Common.GlobalExceptions;
using Logging = APS_Common.Logging;
using LogType = APS_Common.LogType;
using APS_LogHistory.FilterLogger;
using APS_WEB_APP.Common;
using APS_WEB_APP.Common.Constants;
using APS_WEB_APP.Contracts;
using APS_WEB_APP.Helper;
using APS_WEB_APP.Models;
using APS_WEB_APP.Models.DataTables;
using APS_WEB_APP.Models.Invoice;
using APS_WEB_APP.Models.Report.DueDiligence;
using APS_WEB_APP.Models.Report.Procurement.DataTables;
using APS_WEB_APP.Models.Request;
using APS_WEB_APP.Repository.Report;
using APS_WEB_APP.Payloads.Request.Report;
using APS_WEB_APP.Payloads.Response.Report;
using APS_WEB_APP.Repository;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Results;
using System.Net.Http.Json;
using APS_Common.Models.NonShoppingCart.PAP;
using System.Web.Helpers;
using DocumentFormat.OpenXml.Wordprocessing;

namespace APS_WEB_APP.Controllers
{
    [CheckAuthorize]
    public class ReportController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly AuthAps _auth;
        private readonly GlobalExceptions globalExceptions = new GlobalExceptions();
        private readonly IReportRepository _reportRepository;
        private readonly IApprovalRequestRepository _approvalRequestRepository;
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly ICoaRepository _coaRepository;
        private readonly ICostCenterRepository _costCenterRepository;
        private readonly IBusinessUnitRepository _businessUnitRepository;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IFinanceRepository _financeRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IProjectRepository _projectRepository;
        //private readonly IStatusRepository _statusRepository;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IMasterTableRepository _masterTableRepository;
        private const string CreatedTime = "CreatedTime";

        /// <summary>
        /// Logging Report Controller
        /// </summary>
        /// <returns></returns>
        private readonly Logging log = new Logging
        {
            objectName = "Report"
        };

        public ReportController(
            ICookies cookies
            , IOptions<AppSettings> appSettings
            , IReportRepository reportRepository
            , IApprovalRequestRepository approvalRequestRepository
            , ISubCategoryRepository subCategoryRepository
            , ICoaRepository coaRepository
            , ICostCenterRepository costCenterRepository
            , IBusinessUnitRepository businessUnitRepository
            , ICurrencyRepository currencyRepository
            , IAccountRepository accountRepository
            , IFinanceRepository financeRepository
            , IVendorRepository vendorRepository
            , IProjectRepository projectRepository
            //, IStatusRepository statusRepository
            , IHttpClientHelper httpClientHelper
            , IMasterTableRepository masterTableRepository
            )
        {
            _appSettings = appSettings.Value;
            _auth = cookies.GetCookies();
            _reportRepository = reportRepository;
            _approvalRequestRepository = approvalRequestRepository;
            _subCategoryRepository = subCategoryRepository;
            _coaRepository = coaRepository;
            _costCenterRepository = costCenterRepository;
            _businessUnitRepository = businessUnitRepository;
            _currencyRepository = currencyRepository;
            _accountRepository = accountRepository;
            _financeRepository = financeRepository;
            _vendorRepository = vendorRepository;
            _projectRepository = projectRepository;
            //_statusRepository = statusRepository;
            _httpClientHelper = httpClientHelper;
            _masterTableRepository = masterTableRepository;
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [CustomAuthorize]
        [HttpGet]
        public async Task<IActionResult> ReportPaymentList()
        {
            ViewBag.RequestStatusList = _approvalRequestRepository.GetSelectListRequestStatus();
            ViewBag.RequestTypeList = _approvalRequestRepository.GetSelectListRequestType(true).ToList(); // TREX Transaction added to report
            ViewBag.VendorCategoryList = await _subCategoryRepository.GetSelectListSubCategory("VendorCategory");
            ViewBag.AccountMasterList = await _coaRepository.GetSelectListCoa();
            ViewBag.CostCenterList = await _costCenterRepository.GetSelectListCostCenterUser(String.Empty, String.Empty, null);
            ViewBag.BusinessUnitList = await _businessUnitRepository.GetSelectListBusinessUnit(String.Empty);
            ViewBag.CurrencyList = await _currencyRepository.GetSelectListCurrency();

            // get account list with role Maker Finance
            ViewBag.MakerFinanceList = _accountRepository.GetSelectListAccountByRoleName("Maker Finance");
            ViewBag.UserAccountList = _accountRepository.GetSelectListAccount();

            ViewBag.ReportType = new List<SelectListItem>
            {
                new SelectListItem { Text = "Export Transaction", Value = "transaction_summary" },
                new SelectListItem { Text = "Export Transaction (Details)", Value = "transaction_detail" },
                new SelectListItem { Text = "Export Cash Advance List", Value = "cash_summary" },
                new SelectListItem { Text = "Export Cash Advance List (Details)", Value = "cash_detail" },
                new SelectListItem { Text = "Export Transaction - Audit", Value = "audit_summary" },
                new SelectListItem { Text = "Export Transaction (Details) - Audit", Value = "audit_detail" }
            };

            return View("~/Views/Report/ReportPaymentList.cshtml");
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [CustomAuthorize]
        [HttpGet]
        public async Task<IActionResult> ReportVoucher()
        {
            ViewBag.NotUpdatedCount = _reportRepository.GetCountNotUpdatedVoucher().Result.Data;
            ViewBag.PerYearCount = _reportRepository.GetCountPerYearVoucher().Result.Data;
            ViewBag.SelectApprovalFinanceGroupMember = await _financeRepository.GetSelectListApprovalFinanceGroupMember();
            return View("~/Views/Report/ReportVoucher.cshtml");
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [CustomAuthorize]
        [HttpGet]
        public async Task<IActionResult> ReportPajak()
        {
            ViewBag.VendorType = await _subCategoryRepository.GetSelectListSubCategory("VendorCategory");
            ViewBag.OtherCostType = await _subCategoryRepository.GetSelectListSubCategory("OtherCost");
            ViewBag.MakerFinanceList = _accountRepository.GetSelectListAccountByRoleName("Maker Finance");

            List<SelectListItem> reportType = new List<SelectListItem>();
            reportType.Add(new SelectListItem { Text = "General Report", Value = "general" });
            reportType.Add(new SelectListItem { Text = "PPH 23 & PPH 4.2", Value = "pph23" });
            reportType.Add(new SelectListItem { Text = "PPH 26", Value = "pph26" });
            reportType.Add(new SelectListItem { Text = "PPH 21", Value = "pph21" });
            ViewBag.ReportType = reportType;

            return View("~/Views/Report/ReportPajak.cshtml");
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [CustomAuthorize]
        [HttpGet]
        public async Task<IActionResult> ReportBudgetTransactions()
        {
            var projects = await _projectRepository.GetSelectListProject("id");
            ViewBag.ProjectList = projects.Where(e => e.Text.ToLower() != "all").ToList();
            ViewBag.StatusBudgetList = await _subCategoryRepository.GetSelectListSubCategory("StatusBudget");
            ViewBag.AccountMasterList = await _coaRepository.GetSelectListCoa();
            ViewBag.BusinessUnitList = await _businessUnitRepository.GetSelectListBusinessUnit(String.Empty);
            ViewBag.CostCenterList = await _costCenterRepository.GetSelectListCostCenterUser(String.Empty, String.Empty, null);
            return View("~/Views/Report/ReportBudgetTransactions.cshtml");
        }

        /// <summary>
        /// Payment List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpPost]
        public async Task<IActionResult> GetPaymentList(ReportPaymentListRequest param)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonResponse() { Status = globalExceptions.StatusCode(400), Code = 400, Data = globalExceptions.StatusData(new Exception(WebAppSystem.MessageValidationModel)) });
            }

            string methodName = "GetPaymentList";
            var result = new List<ReportPaymentListResponse>();
            var searchValue = param.Search?.Value;
            var draw = param.Draw;
            var pageSize = param.Length;
            var page = param.Start;
            var sortColumnIndex = param.Order?[0].Column;
            var sortDirection = param.Order?[0].Dir.ToString();

            string sortColumnGetPaymentList;
            switch (sortColumnIndex)
            {
                case 2:
                    sortColumnGetPaymentList = "TransferNumber";
                    break;
                case 3:
                    sortColumnGetPaymentList = "VoucherNumber";
                    break;
                case 4:
                    sortColumnGetPaymentList = "RequestNumber";
                    break;
                case 5:
                    sortColumnGetPaymentList = "SettlementNumber";
                    break;
                case 6:
                    sortColumnGetPaymentList = "RequestDate";
                    break;
                case 7:
                    sortColumnGetPaymentList = "ReceivedByFinanceDate";
                    break;
                case 8:
                    sortColumnGetPaymentList = "RequestorName";
                    break;
                case 9:
                    sortColumnGetPaymentList = "MakerFinance";
                    break;
                case 10:
                    sortColumnGetPaymentList = "VendorType";
                    break;
                case 11:
                    sortColumnGetPaymentList = "VendorName";
                    break;
                case 12:
                    sortColumnGetPaymentList = "RequestType";
                    break;
                case 13:
                    sortColumnGetPaymentList = "AccountCode";
                    break;
                case 14:
                    sortColumnGetPaymentList = "CostCenterName";
                    break;
                case 15:
                    sortColumnGetPaymentList = "NettAmount";
                    break;
                case 16:
                    sortColumnGetPaymentList = "SettlementDate";
                    break;
                case 17:
                    sortColumnGetPaymentList = "StatusTransfer";
                    break;
                case 18:
                    sortColumnGetPaymentList = "StatusOverdue";
                    break;
                default:
                    sortColumnGetPaymentList = "ReceivedByFinanceDate"; sortDirection = "asc";
                    break;
            }

            CommonResponse cr = new();
            try
            {
                param.IsExport = false;
                param.Page = page;
                param.PageSize = pageSize;
                param.SortColumn = sortColumnGetPaymentList;
                param.SortDirection = sortDirection;
                if (!string.IsNullOrEmpty(searchValue))
                    param.Search = new APS_Common.DtSearch() { Value = searchValue };
                cr = await _reportRepository.GetPaymentList(param);
                var resData = JsonConvert.SerializeObject(cr.Data);
                int totalRecords = 0;
                // if List is null
                if (cr.Code != 204)
                {
                    result = JsonConvert.DeserializeObject<List<ReportPaymentListResponse>>(resData);
                    totalRecords = result != null ? result[0].CountData : 0;
                }
                log.LogPagination(methodName, null, page, pageSize, LogType.Info);
                return Json(new { draw = draw, recordsFiltered = totalRecords, recordsTotal = totalRecords, data = result });
            }
            catch (Exception e)
            {
                cr.Code = 400;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                cr.Data = globalExceptions.StatusData(e);

                log.LogInitialize(methodName, cr.Data, LogType.Info);
                return Json(new { draw = draw, recordsFiltered = 0, recordsTotal = 0, data = result });
            }
        }

        /// <summary>
        /// Payment List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpPost]
        public async Task<IActionResult> GetBudgetTransactions(ReportPaymentListRequest param)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonResponse() { Status = globalExceptions.StatusCode(400), Code = 400, Data = globalExceptions.StatusData(new Exception(WebAppSystem.MessageValidationModel)) });
            }

            string methodName = "GetBudgetTransactions";
            var result = new List<ReportBudgetResponse>();
            var searchValue = param.Search?.Value;
            var draw = param.Draw;
            var pageSize = param.Length;
            var page = param.Start;
            var sortColumnIndex = param.Order?[0].Column;
            var sortDirection = param.Order?[0].Dir.ToString();

            string sortColumn;
            switch (sortColumnIndex)
            {
                case 1:
                    sortColumn = "TransferNumber";
                    break;
                case 2:
                    sortColumn = "RequestNumber";
                    break;
                case 3:
                    sortColumn = "AccountMasterCode";
                    break;
                case 4:
                    sortColumn = "AccountMasterName";
                    break;
                case 5:
                    sortColumn = "MtAccountType";
                    break;
                case 6:
                    sortColumn = "BusinessUnitName";
                    break;
                case 7:
                    sortColumn = "CostCenterName";
                    break;
                case 8:
                    sortColumn = "Rate";
                    break;
                case 9:
                    sortColumn = "NettAmount";
                    break;
                case 10:
                    sortColumn = "StatusTransfer";
                    break;
                case 11:
                    sortColumn = "IsBudget";
                    break;
                default:
                    sortColumn = "RequestNumber"; sortDirection = "desc";
                    break;
            }

            CommonResponse cr = new CommonResponse();
            try
            {
                param.IsExport = false;
                param.Page = page;
                param.PageSize = pageSize;
                param.SortColumn = sortColumn;
                param.SortDirection = sortDirection;
                if (!string.IsNullOrEmpty(searchValue))
                    param.Search = new APS_Common.DtSearch() { Value = searchValue };
                cr = await _reportRepository.GetBudgetTransactions(param);
                var resData = JsonConvert.SerializeObject(cr.Data);
                int totalRecords = 0;
                // if List is null
                if (cr.Code != 204)
                {
                    result = JsonConvert.DeserializeObject<List<ReportBudgetResponse>>(resData);
                    totalRecords = result != null ? result[0].CountData : 0;
                }
                log.LogPagination(methodName, null, page, pageSize, LogType.Info);
                return Json(new { draw = draw, recordsFiltered = totalRecords, recordsTotal = totalRecords, data = result });
            }
            catch (Exception e)
            {
                cr.Code = 400;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                cr.Data = globalExceptions.StatusData(e);

                log.LogInitialize(methodName, cr.Data, LogType.Info);
                return Json(new { draw = draw, recordsFiltered = 0, recordsTotal = 0, data = result });
            }
        }

        /// <summary>
        /// Voucher List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpPost]
        public async Task<IActionResult> GetVoucherList(ReportVoucherListRequest param)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonResponse() { Status = globalExceptions.StatusCode(400), Code = 400, Data = globalExceptions.StatusData(new Exception(WebAppSystem.MessageValidationModel)) });
            }
            string methodName = "GetVoucherList";
            var searchValue = param.Search?.Value;
            var draw = param.Draw;
            var pageSize = param.Length;
            var page = param.Start;
            var sortColumnIndex = param.Order?[0].Column;
            var sortDirection = param.Order?[0].Dir.ToString();

            string sortColumnGetListVoucher;
            switch (sortColumnIndex)
            {
                case 1:
                    sortColumnGetListVoucher = CreatedTime;
                    break;
                case 2:
                    sortColumnGetListVoucher = CreatedTime;
                    break;
                case 3:
                    sortColumnGetListVoucher = CreatedTime;
                    break;
                case 4:
                    sortColumnGetListVoucher = "VoucherNumber";
                    break;
                case 5:
                    sortColumnGetListVoucher = "IsEmail";
                    break;
                case 6:
                    sortColumnGetListVoucher = "BankTransferCode";
                    break;
                case 7:
                    sortColumnGetListVoucher = "CostCenterName";
                    break;
                case 8:
                    sortColumnGetListVoucher = "Status";
                    break;
                case 9:
                    sortColumnGetListVoucher = "ApprovedDate";
                    break;
                case 10:
                    sortColumnGetListVoucher = "CreatedBy";
                    break;
                case 11:
                    sortColumnGetListVoucher = CreatedTime;
                    break;
                case 12:
                    sortColumnGetListVoucher = "TransferNumber";
                    break;
                case 13:
                    sortColumnGetListVoucher = CreatedTime;
                    break;
                default:
                    sortColumnGetListVoucher = CreatedTime; sortDirection = "desc";
                    break;
            }

            CommonResponse cr = new();
            try
            {
                param.IsExport = false;
                param.Page = page;
                param.PageSize = pageSize;
                param.SortColumn = sortColumnGetListVoucher;
                param.SortDirection = sortDirection;
                if (!string.IsNullOrEmpty(searchValue))
                    param.Search = new APS_Common.DtSearch() { Value = searchValue };
                cr = await _reportRepository.GetVoucherList(param);
                var result = new List<ReportVoucherListResponse>();
                var resData = JsonConvert.SerializeObject(cr.Data);
                int totalRecords = 0;
                // if List is null
                if (cr.Code != 204)
                {
                    result = JsonConvert.DeserializeObject<List<ReportVoucherListResponse>>(resData);
                    totalRecords = result != null ? result[0].CountData : 0;
                }
                log.LogPagination(methodName, null, page, pageSize, LogType.Info);
                return Json(new { draw = draw, recordsFiltered = totalRecords, recordsTotal = totalRecords, data = result });
            }
            catch (Exception e)
            {
                cr.Code = 400;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                cr.Data = globalExceptions.StatusData(e);

                log.LogInitialize(methodName, cr.Data, LogType.Info);
                return BadRequest(cr);
            }
        }

        /// <summary>
        /// Pajak List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpPost]
        public async Task<IActionResult> GetPajakList(ReportPajakListRequest param)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonResponse() { Status = globalExceptions.StatusCode(400), Code = 400, Data = globalExceptions.StatusData(new Exception(WebAppSystem.MessageValidationModel)) });
            }
            string methodName = nameof(GetPajakList);
            var searchValue = param.Search?.Value;
            var draw = param.Draw;
            var pageSize = param.Length;
            var page = param.Start;
            var sortColumnIndex = param.Order?[0].Column;
            var sortDirection = param.Order?[0].Dir.ToString();

            string sortColumnGetListPajak;
            switch (sortColumnIndex)
            {
                case 1:
                    sortColumnGetListPajak = "Npwp";
                    break;
                case 2:
                    sortColumnGetListPajak = "Nik";
                    break;
                case 3:
                    sortColumnGetListPajak = "Nama";
                    break;
                case 4:
                    sortColumnGetListPajak = "Alamat";
                    break;
                case 5:
                    sortColumnGetListPajak = "Keterangan";
                    break;
                case 6:
                    sortColumnGetListPajak = "Mcm";
                    break;
                case 7:
                    sortColumnGetListPajak = "PayDate";
                    break;
                case 8:
                    sortColumnGetListPajak = "Jumlah";
                    break;
                case 9:
                    sortColumnGetListPajak = "Tarif";
                    break;
                case 10:
                    sortColumnGetListPajak = "NilaiPajak";
                    break;
                case 11:
                    sortColumnGetListPajak = "JenisPajak";
                    break;
                case 12:
                    sortColumnGetListPajak = "Invoice";
                    break;
                case 13:
                    sortColumnGetListPajak = "MakerFinance";
                    break;
                case 14:
                    sortColumnGetListPajak = "InvoiceDate";
                    break;
                case 15:
                    sortColumnGetListPajak = "Skb";
                    break;
                case 16:
                    sortColumnGetListPajak = "RequestNumber";
                    break;
                default:
                    sortColumnGetListPajak = CreatedTime; sortDirection = "desc";
                    break;
            }

            CommonResponse cr = new();
            try
            {
                param.IsExport = false;
                param.Page = page;
                param.PageSize = pageSize;
                param.SortColumn = sortColumnGetListPajak;
                param.SortDirection = sortDirection;
                if (!string.IsNullOrEmpty(searchValue))
                    param.Search = new APS_Common.DtSearch() { Value = searchValue };
                cr = await _reportRepository.GetPajakList(param);
                var result = new List<ReportPajakListResponse>();
                var resData = JsonConvert.SerializeObject(cr.Data);
                int totalRecords = 0;
                // if List is null
                if (cr.Code != 204)
                {
                    result = JsonConvert.DeserializeObject<List<ReportPajakListResponse>>(resData);
                    totalRecords = result != null ? result[0].CountData : 0;
                }
                log.LogPagination(methodName, null, page, pageSize, LogType.Info);
                return Json(new { draw = draw, recordsFiltered = totalRecords, recordsTotal = totalRecords, data = result });
            }
            catch (Exception e)
            {
                cr.Code = 400;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                cr.Data = globalExceptions.StatusData(e);

                log.LogInitialize(methodName, cr.Data, LogType.Info);
                return BadRequest(cr);
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpGet]
        public async Task<IActionResult> ExportExcelPaymentList(string json)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonResponse() { Status = globalExceptions.StatusCode(400), Code = 400, Data = globalExceptions.StatusData(new Exception("ModelState Not Valid")) });
            }

            string methodName = "GetExportExcel";
            try
            {
                var param = JsonConvert.DeserializeObject<ReportPaymentListRequest>(json);
                string url = _appSettings.BaseURL.RestApiAPS + "/report/payment/export";
                param.IsExport = true;
                var jsonObject = JsonConvert.SerializeObject(param);
                var stream = await _httpClientHelper.PostStreamAsync(url, jsonObject);
                if (param.IsCashAdvance)
                    return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, $"Export Advance List ({param.ReportType}).xlsx");
                else
                    return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, $"Export Transaction ({param.ReportType}).xlsx");
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(methodName, e.InnerException);
            }
        }


        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpGet]
        public async Task<IActionResult> ExportPajak(string json)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonResponse() { Status = globalExceptions.StatusCode(400), Code = 400, Data = globalExceptions.StatusData(new Exception("ModelState Not Valid")) });
            }

            string methodName = "GetExportExcel";
            try
            {
                var param = JsonConvert.DeserializeObject<ReportPajakListRequest>(json);
                string url = _appSettings.BaseURL.RestApiAPS + "/report/pajak/export";
                param.IsExport = true;
                var jsonObject = JsonConvert.SerializeObject(param);
                var stream = await _httpClientHelper.PostStreamAsync(url, jsonObject);
                return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, $"Export Pajak ({param.TransferTimeStart} - {param.TransferTimeEnd}).xlsx");
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(methodName, e.InnerException);
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpGet]
        public async Task<IActionResult> ExportExcelBudgetTransactions(string json)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonResponse() { Status = globalExceptions.StatusCode(400), Code = 400, Data = globalExceptions.StatusData(new Exception("ModelState Not Valid")) });
            }

            string methodName = "ExportExcelBudgetTransactions";
            try
            {
                var param = JsonConvert.DeserializeObject<ParamGetRequestList>(json);
                string url = _appSettings.BaseURL.RestApiAPS + "/report/budget/export";
                param.IsExport = true;
                var jsonObject = JsonConvert.SerializeObject(param);
                var stream = await _httpClientHelper.PostStreamAsync(url, jsonObject);
                return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, "Export Budget Detail Transaction.xlsx");
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(methodName, e.InnerException);
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [CustomAuthorize]
        public async Task<IActionResult> ReportPO()
        {
            ViewBag.VendorList = await _vendorRepository.GetSelectListVendor(string.Empty, string.Empty);
            ViewBag.CostCenterList = await _costCenterRepository.GetSelectListCostCenterUser(String.Empty, String.Empty, null);
            ViewBag.AccountMasterList = await _coaRepository.GetSelectListCoa();
            ViewBag.PRStatusList = await _masterTableRepository.SelectlistMasterTable("PurchaseRequest.Status");
            ViewBag.POStatusList = await _masterTableRepository.SelectlistMasterTable("PurchaseOrder.Status");
            return View("~/Views/Report/ReportPO.cshtml");
        }

        /// <summary>
        /// Report PO Shopping Cart List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpPost]
        public async Task<IActionResult> GetReportPOShoppingCartList(ReportPORequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonResponse() { Status = globalExceptions.StatusCode(400), Code = 400, Data = globalExceptions.StatusData(new Exception(WebAppSystem.MessageValidationModel)) });
            }

            var searchValue = request.Search?.Value;
            var draw = request.Draw;
            var pageSize = request.Length;
            var page = request.Start;

            CommonResponse cr = new();
            try
            {
                var form = HttpContext.Request.Form;
                request.IsExport = false;
                request.Page = page;
                request.PageSize = pageSize;
                request.SortColumn = form[nameof(ReportPORequest.SortColumn)].FirstOrDefault();
                request.SortDirection = form[nameof(ReportPORequest.SortDirection)].FirstOrDefault();
                if (!string.IsNullOrEmpty(searchValue))
                    request.Search.Value = searchValue;

                cr = await _reportRepository.GetReportPOShoppingCartList(request);

                var data = new List<ReportPOShoppingCartListResponse>();
                var json = JsonConvert.SerializeObject(cr.Data);
                var recordsTotal = 0;
                if (cr.Code != 204)
                {
                    data = JsonConvert.DeserializeObject<List<ReportPOShoppingCartListResponse>>(json);
                    recordsTotal = data != null ? data[0].CountData : 0;
                }
                log.LogPagination(nameof(GetReportPOShoppingCartList), null, page, pageSize, LogType.Info);
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
            }
            catch (Exception e)
            {
                cr.Code = 400;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                cr.Data = globalExceptions.StatusData(e);
                log.LogInitialize(nameof(GetReportPOShoppingCartList), cr.Data, LogType.Info);
                return BadRequest(cr);
            }
        }

        /// <summary>
        /// Report PO Non Shopping Cart List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpPost]
        public async Task<IActionResult> GetReportPONonShoppingCartList(ReportPORequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonResponse() { Status = globalExceptions.StatusCode(400), Code = 400, Data = globalExceptions.StatusData(new Exception(WebAppSystem.MessageValidationModel)) });
            }

            var searchValue = request.Search?.Value;
            var draw = request.Draw;
            var pageSize = request.Length;
            var page = request.Start;

            CommonResponse cr = new();
            try
            {
                var form = HttpContext.Request.Form;

                request.IsExport = false;
                request.Page = page;
                request.PageSize = pageSize;
                request.SortColumn = form[nameof(ReportPORequest.SortColumn)].FirstOrDefault();
                request.SortDirection = form[nameof(ReportPORequest.SortDirection)].FirstOrDefault();
                if (!string.IsNullOrEmpty(searchValue))
                    request.Search.Value = searchValue;

                cr = await _reportRepository.GetReportPONonShoppingCartList(request);

                var data = new List<ReportPONonShoppingCartListResponse>();
                var json = JsonConvert.SerializeObject(cr.Data);
                var recordsTotal = 0;
                if (cr.Code != 204)
                {
                    data = JsonConvert.DeserializeObject<List<ReportPONonShoppingCartListResponse>>(json);
                    recordsTotal = data != null ? data[0].CountData : 0;
                }
                log.LogPagination(nameof(GetReportPOShoppingCartList), null, page, pageSize, LogType.Info);
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
            }
            catch (Exception e)
            {
                cr.Code = 400;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                cr.Data = globalExceptions.StatusData(e);
                log.LogInitialize(nameof(GetReportPOShoppingCartList), cr.Data, LogType.Info);
                return BadRequest(cr);
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpGet]
        public async Task<IActionResult> GetReportPOShoppingCartExport(string json)
        {
            if (!ModelState.IsValid) { }

            try
            {
                // content
                var request = JsonConvert.DeserializeObject<ReportPORequest>(json);
                request.IsExport = true;
                var requestJson = JsonConvert.SerializeObject(request);

                // response
                var requestUri = _appSettings.BaseURL.RestApiAPS + "/report/po/shoppingcart/export";
                var fileStream = await _httpClientHelper.PostStreamAsync(requestUri, requestJson);

                // file
                var fileDownloadName = "Report PO - Shopping Cart - Export.xlsx";
                return File(fileStream, System.Net.Mime.MediaTypeNames.Application.Octet, fileDownloadName);
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(GetReportPOShoppingCartExport), e.InnerException);
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpGet]
        public async Task<IActionResult> GetReportPONonShoppingCartExport(string json)
        {
            if (!ModelState.IsValid) { }

            try
            {
                // content
                var request = JsonConvert.DeserializeObject<ReportPORequest>(json);
                request.IsExport = true;
                var requestJson = JsonConvert.SerializeObject(request);

                // response
                var requestUri = _appSettings.BaseURL.RestApiAPS + "/report/po/nonshoppingcart/export";
                var fileStream = await _httpClientHelper.PostStreamAsync(requestUri, requestJson);

                // file
                var fileDownloadName = "Report PO - Non Shopping Cart - Export.xlsx";
                return File(fileStream, System.Net.Mime.MediaTypeNames.Application.Octet, fileDownloadName);
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(GetReportPOShoppingCartExport), e.InnerException);
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [CustomAuthorize]
        [HttpGet]
        public async Task<IActionResult> DueDiligence()
        {
            DueDiligenceReportViewModel model = new();
            model.Vendor.SelectList = (await _vendorRepository.GetSelectListVendor(string.Empty, string.Empty)).ToList();
            model.Vendor.SupplierType_SubCategory.SelectList = (await _subCategoryRepository.GetSelectListSubCategory(null, "CA-2023-06-00050")).ToList(); // CA-2023-06-00050 SupplierType
            model.Vendor.VendorDueDiligence.StatusMasterTable.SelectList = (await _masterTableRepository.SelectlistMasterTable("VendorDueDiligence.Status")).ToList();
            model.Vendor.VendorDueDiligence.VendorSimpleDueDiligence.PepResult.SelectList = (new SelectListItem[]
            {
                new("Pending", "Pending")
                , new("PEP", "PEP")
                , new("NON PEP", "NON PEP")
            }).ToList();
            model.Title = "Report Due Diligence";
            return View("~/Views/Report/DueDiligenceReportView.cshtml", model);
        }

        /// <summary>
        /// Report PO Shopping Cart List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpPost("Report/DueDiligence/Json")]
        public async Task<IActionResult> DueDiligenceReportJsonPost(DueDiligenceReportRequestModel request)
        {
            if (!ModelState.IsValid) { }

            try
            {
                CommonResponse cr = await _reportRepository.DueDiligenceReportJsonPost(request);
                string json = JsonConvert.SerializeObject(cr.Data);
                DataTablesResponseModel<DueDiligenceReportResponseModel> response = new();
                response.Data = JsonConvert.DeserializeObject<DueDiligenceReportResponseModel[]>(json);
                response.RecordsTotal = (response.Data.Length == 0) ? 0 : response.Data[0].Count;
                response.RecordsFiltered = response.RecordsTotal;
                response.Draw = request.Draw;
                log.LogPagination(nameof(DueDiligenceReportJsonPost), null, request.Start, request.Length, LogType.Info);
                return Json(response);
            }
            catch (Exception e)
            {
                DataTablesResponseModel<DueDiligenceReportResponseModel> response = new();
                response.Error = e.Message;
                response.RecordsTotal = 0;
                response.RecordsFiltered = 0;
                response.Draw = request.Draw;
                log.LogInitialize(nameof(DueDiligenceReportJsonPost), globalExceptions.StatusData(e), LogType.Info);
                return Json(response);
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpGet("Report/DueDiligence/Xlsx")]
        public async Task<IActionResult> DueDiligenceReportXlsxPost(string json)
        {
            if (!ModelState.IsValid) { }

            try
            {
                var request = JsonConvert.DeserializeObject<DueDiligenceReportRequestModel>(json);

                string route = "Report/DueDiligence/Xlsx";
                string requestUri = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string requestJson = JsonConvert.SerializeObject(request);

                Stream stream = await _httpClientHelper.PostStreamAsync(requestUri, requestJson);
                string name = "Report Due Diligence - Export.xlsx";
                return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, name);
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(DueDiligenceReportXlsxPost), e.InnerException);
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [Route("Report/Procurement/backup")]
        public async Task<IActionResult> Procurement()
        {
            var v = new Models.Report.Procurement.View();
            v.CategoryProcess_SubCategory = await _subCategoryRepository.GetSelectListSubCategory(null, "CA-2024-02-00096"); // CategoryProcessNew
            v.PurchaseRequestStatus = await _masterTableRepository.SelectlistMasterTable("PurchaseRequest.Status");
            v.CostCenter = await _costCenterRepository.GetSelectListCostCenterUser(String.Empty, String.Empty, null);
            v.AccountMaster = await _coaRepository.GetSelectListCoa();
            v.Vendor = await _vendorRepository.GetSelectListVendor(string.Empty, string.Empty);
            v.TypeProcess_SubCategory = await _subCategoryRepository.GetSelectListSubCategory(null, "CA-2023-08-00070"); // Type Proccess
            v.PurchaseOrderStatus = await _masterTableRepository.SelectlistMasterTable("PurchaseOrder.Status");
            return View("~/Views/Report/Procurement.cshtml", v);
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpPost("Report/Procurement/DataTables")]
        public async Task<IActionResult> ProcurementDataTables(Models.Report.Procurement.DataTables.Request request)
        {
            if (!ModelState.IsValid) { }

            try
            {
                CommonResponse cr = await _reportRepository.ProcurementDataTablesData(request);

                string json = JsonConvert.SerializeObject(cr.Data);
                Models.DataTables.DtResult<Data> dtResult = new();
                dtResult.Data = JsonConvert.DeserializeObject<IEnumerable<Data>>(json);
                dtResult.RecordsTotal = dtResult.Data.Any() ? dtResult.Data.ToArray()[0].Count : 0;
                dtResult.RecordsFiltered = dtResult.RecordsTotal;
                dtResult.Draw = request.Draw;
                log.LogPagination(nameof(DueDiligenceReportJsonPost), null, request.Start, request.Length, LogType.Info);
                return Json(dtResult);
            }
            catch (Exception e)
            {
                Models.DataTables.DtResult<Data> dtResult = new();
                dtResult.Error = e.Message;
                dtResult.Data = Array.Empty<Data>();
                dtResult.RecordsTotal = 0;
                dtResult.RecordsFiltered = dtResult.RecordsTotal;
                dtResult.Draw = request.Draw;
                log.LogInitialize(nameof(DueDiligenceReportJsonPost), globalExceptions.StatusData(e), LogType.Info);
                return Json(dtResult);
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpGet("Report/Procurement/Xlsx")]
        public async Task<IActionResult> ProcurementXlsx(string json)
        {
            if (!ModelState.IsValid) { }

            try
            {
                var request = JsonConvert.DeserializeObject<Models.Report.Procurement.DataTables.Request>(json);
                string route = "Report/Procurement/Xlsx";
                string requestUri = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string requestJson = JsonConvert.SerializeObject(request);
                Stream stream = await _httpClientHelper.PostStreamAsync(requestUri, requestJson);

                string name = "Report PO - Export.xlsx";
                return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, name);
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ProcurementXlsx), e.InnerException);
            }
        }

        [Route(APS_Common.Const.Routes.Report_Contract_Index)]
        public IActionResult Report_Contract_Index()
        {
            ViewBag.AuthEntities = _auth.AuthEntities;
            return View("~/Views/Report/Contract/Index.cshtml");
        }

        [HttpPost(APS_Common.Const.Routes.Report_Contract_Index_Model)]
        public IActionResult Report_Contract_Index_Model(APS_Common.Models.Report.Contract.Index.Model.Request.Root request)
        {
            var id = request.UUID;
            var c = GetType().Name;
            var m = MethodBase.GetCurrentMethod().Name;
            var nl = Environment.NewLine;
            var response = new APS_Common.Models.Report.Contract.Index.Model.Response.Root();
            if (!ModelState.IsValid)
            {
                response.MessageArray = ModelState.Values.SelectMany(mse => mse.Errors).Select(me => me.ErrorMessage).ToArray();
                response.StatusCode = StatusCodes.Status400BadRequest;
                APS_Common.BaseLogging.LogInfo(id, c, m, nameof(response) + nl + System.Text.Json.JsonSerializer.Serialize(response));
                return StatusCode(response.StatusCode, response);
            }
            try
            {
                //request.Account.Username = iExternalService.GetAccountDetail(request.Account.Id).Result.Username;
                response = _reportRepository.Report_Contract_Index_Model(request);
                response.StatusCode = StatusCodes.Status200OK;
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception e)
            {
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.Message) + nl + e.Message);
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.StackTrace) + nl + e.StackTrace);
                response.MessageArray = [.. response.MessageArray, e.GetBaseException().Message];
                response.StatusCode = StatusCodes.Status400BadRequest;
                APS_Common.BaseLogging.LogInfo(id, c, m, nameof(response) + nl + System.Text.Json.JsonSerializer.Serialize(response));
                return StatusCode(response.StatusCode, response);
            }
        }

        [HttpPost(APS_Common.Const.Routes.Report_Contract_Index_DataTables)]
        public IActionResult Report_Contract_Index_DataTables(APS_Common.Models.Report.Contract.Index.DataTables.Request.Root request)
        {
            string id = request.UUID;
            string c = GetType().Name;
            string m = MethodBase.GetCurrentMethod().Name;
            var nl = Environment.NewLine;
            var response = new APS_Common.Models.DataTables.Response<APS_Common.Models.Report.Contract.Index.DataTables.Row.Root>();
            if (!ModelState.IsValid)
            {
                response.Draw = request.Draw;
                response.TraceId = request.TraceId;
                response.MessageArray = ModelState.Values.SelectMany(mse => mse.Errors).Select(me => me.ErrorMessage).ToArray();
                response.StatusCode = StatusCodes.Status400BadRequest;
                APS_Common.BaseLogging.LogInfo(id, c, m, nameof(response) + nl + System.Text.Json.JsonSerializer.Serialize(response));
                return StatusCode(response.StatusCode, response);
            }
            try
            {
                response = _reportRepository.Report_Contract_Index_DataTables(request);
                response.Draw = request.Draw;
                response.TraceId = request.TraceId;
                response.StatusCode = StatusCodes.Status200OK;
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception e)
            {
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.Message) + nl + e.Message);
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.StackTrace) + nl + e.StackTrace);
                response.Draw = request.Draw;
                response.TraceId = request.TraceId;
                response.MessageArray = [.. response.MessageArray, e.GetBaseException().Message];
                response.StatusCode = StatusCodes.Status400BadRequest;
                APS_Common.BaseLogging.LogInfo(id, c, m, nameof(response) + nl + System.Text.Json.JsonSerializer.Serialize(response));
                return StatusCode(response.StatusCode, response);
            }
        }

        [HttpGet(APS_Common.Const.Routes.Report_Contract_Index_DataTables_Excel)]
        public async Task<IActionResult> Report_Contract_Index_DataTables_Excel(string json)
        {
            var request = JsonConvert.DeserializeObject<APS_Common.Models.Report.Contract.Index.DataTables.Request.Root>(json);

            var id = request.UUID;
            var c = GetType().Name;
            var m = MethodBase.GetCurrentMethod().Name;
            var nl = Environment.NewLine;
            if (!ModelState.IsValid)
            {
                var errorMessageArray = ModelState.Values.SelectMany(mse => mse.Errors).Select(me => me.ErrorMessage).ToArray();
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(errorMessageArray) + nl + System.Text.Json.JsonSerializer.Serialize(errorMessageArray));
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(request) + nl + System.Text.Json.JsonSerializer.Serialize(request));
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            try
            {
                string route = APS_Common.Const.Routes.Report_Contract_Index_DataTables_Excel;
                string requestUri = $"{_appSettings.BaseURL.RestApiAPS}/{route}";

                string requestJson = System.Text.Json.JsonSerializer.Serialize(request);
                Stream stream = await _httpClientHelper.PostStreamAsync(requestUri, requestJson);

                string name = "Report Contract - Export.xlsx";
                return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, name);
            }
            catch (Exception e)
            {
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.Message) + nl + e.Message);
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.StackTrace) + nl + e.StackTrace);
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(request) + nl + System.Text.Json.JsonSerializer.Serialize(request));
                return BadRequest();
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [Route("Report/Procurement")]
        public async Task<IActionResult> Procurement_WIP()
        {
            var v = new Models.Report.Procurement.View();
            v.CategoryProcess_SubCategory = await _subCategoryRepository.GetSelectListSubCategory(null, "CA-2024-02-00096"); // CategoryProcessNew
            v.PurchaseRequestStatus = await _masterTableRepository.SelectlistMasterTable("PurchaseRequest.Status");
            v.CostCenter = await _costCenterRepository.GetSelectListCostCenterUser(String.Empty, String.Empty, null);
            v.AccountMaster = await _coaRepository.GetSelectListCoa();
            v.Vendor = await _vendorRepository.GetSelectListVendor(string.Empty, string.Empty);
            v.TypeProcess_SubCategory = await _subCategoryRepository.GetSelectListSubCategory(null, "CA-2023-08-00070"); // Type Proccess
            v.PurchaseOrderStatus = await _masterTableRepository.SelectlistMasterTable("PurchaseOrder.Status");
            return View("~/Views/Report/Procurement_WIP.cshtml", v);
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpPost("Report/Procurement/DataTables/WIP")]
        public async Task<IActionResult> ProcurementDataTables_WIP(Models.Report.Procurement.DataTables.Request_WIP request)
        {
            if (!ModelState.IsValid) { }

            try
            {
                CommonResponse cr = await _reportRepository.ProcurementDataTablesData_WIP(request);

                string json = JsonConvert.SerializeObject(cr.Data);
                Models.DataTables.DtResult<Data_WIP> dtResult = new();
                dtResult.Data = JsonConvert.DeserializeObject<IEnumerable<Data_WIP>>(json);
                dtResult.RecordsTotal = dtResult.Data.Any() ? dtResult.Data.ToArray()[0].Count : 0;
                dtResult.RecordsFiltered = dtResult.RecordsTotal;
                dtResult.Draw = request.Draw;
                log.LogPagination(nameof(DueDiligenceReportJsonPost), null, request.Start, request.Length, LogType.Info);
                return Json(dtResult);
            }
            catch (Exception e)
            {
                Models.DataTables.DtResult<Data> dtResult = new();
                dtResult.Error = e.Message;
                dtResult.Data = Array.Empty<Data>();
                dtResult.RecordsTotal = 0;
                dtResult.RecordsFiltered = dtResult.RecordsTotal;
                dtResult.Draw = request.Draw;
                log.LogInitialize(nameof(DueDiligenceReportJsonPost), globalExceptions.StatusData(e), LogType.Info);
                return Json(dtResult);
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpGet("Report/Procurement/Xlsx/WIP")]
        public async Task<IActionResult> ProcurementXlsx_WIP(string json)
        {
            if (!ModelState.IsValid) { }

            try
            {
                var request = JsonConvert.DeserializeObject<Models.Report.Procurement.DataTables.Request_WIP>(json);
                string route = "Report/Procurement/Xlsx/WIP";
                string requestUri = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string requestJson = JsonConvert.SerializeObject(request);
                Stream stream = await _httpClientHelper.PostStreamAsync(requestUri, requestJson);

                string name = "Report PO - Export.xlsx";
                return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, name);
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ProcurementXlsx), e.InnerException);
            }
        }



        [HttpPost]
        public async Task<IActionResult> RequestExport([FromBody] object payload)
        {
            try
            {
                string route = "RequestExport";
                string requestUri = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string requestJson = JsonConvert.SerializeObject(payload);
                var httpContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var responseContent = await _httpClientHelper.PostJsonAsync(requestUri, requestJson);
                //if (responseContent)
                //{
                var responseData = JsonConvert.DeserializeObject<object>(responseContent.Data.ToString());


                return StatusCode(202, responseData);
                //}
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ProcurementXlsx), e.InnerException);
            }
        }



        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpGet]
        public async Task<IActionResult> GetStatus(string jobsId)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonResponse() { Status = globalExceptions.StatusCode(400), Code = 400, Data = globalExceptions.StatusData(new Exception("ModelState Not Valid")) });
            }
            try
            {
                string route = "status";
                string requestUri = $"{_appSettings.BaseURL.RestApiAPS}/{jobsId}/{route}";
                var responseContent = await _httpClientHelper.GetAsync(requestUri);

                var responseData = JsonConvert.DeserializeObject<object>(responseContent.Data.ToString());

                return StatusCode(200, responseData);
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(GetStatus), e.InnerException);
            }
        }

        [TypeFilter(typeof(LoggerActivityAttribute))]
        [HttpGet]
        public async Task<IActionResult> Download(string jobId)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonResponse()
                {
                    Status = globalExceptions.StatusCode(400),
                    Code = 400,
                    Data = globalExceptions.StatusData(new Exception("ModelState Not Valid"))
                });
            }

            try
            {
                string route = "download";
                string requestUri = $"{_appSettings.BaseURL.RestApiAPS}/{jobId}/{route}";

                // 🔄 Setup HttpClient dengan SSL bypass untuk dev
                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    if (_appSettings.Environment == "Development") return true;
                    return sslPolicyErrors == SslPolicyErrors.None;
                };

                using var httpClient = new HttpClient(handler);
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(_auth.TokenResponse.token_type, _auth.TokenResponse.access_token);

                // 📡 Request ke API
                var response = await httpClient.GetAsync(requestUri);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    //_logger.LogWarning("⚠️ Proxy download failed: {Status} - {Content}",
                    //    response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, new { error = "Failed to fetch file", detail = errorContent });
                }

                // 📦 Baca sebagai byte array (lebih aman daripada stream untuk proxy)
                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                // 🏷️ Extract filename dari Content-Disposition header API
                string fileName = $"payment_export_{jobId}.xlsx"; // default pattern
                var contentDisposition = response.Content.Headers.ContentDisposition;
                if (!string.IsNullOrEmpty(contentDisposition?.FileNameStar))
                {
                    fileName = contentDisposition.FileNameStar.Trim('"', '\'');
                }
                else if (!string.IsNullOrEmpty(contentDisposition?.FileName))
                {
                    fileName = contentDisposition.FileName.Trim('"', '\'');
                }

                //_logger.LogInformation("✅ Proxy download success: {JobId} → {FileName} ({Bytes} bytes)",
                //    jobsId, fileName, fileBytes.Length);

                // 📤 Return file - PENTING: gunakan File() dengan parameter fileName
                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception e)
            {
                //_logger.LogError(e, "❌ Proxy download error for job {JobId}", jobsId);
                // Return JSON error agar jQuery bisa parse
                return StatusCode(500, new { error = "Download failed", detail = e.Message });
            }
        }

    }
}
