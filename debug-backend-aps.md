using APS_Common;
using APS_Common.Const;
using APS_Common.Extensions;
using APS_LogHistory.FilterLogger;
using APS_REST_API.Contracts;
using APS_REST_API.Models.Report.DueDiligence;
using APS_REST_API.Models.Request;
using APS_REST_API.Payloads.Request.Report;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace APS_REST_API.Controllers
{
    [Route("v1")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportRepository _reportRepository;
        private readonly GlobalExceptions globalExceptions = new();

        /// <summary>
        /// Logging Report Controller
        /// </summary>
        /// <returns></returns>
        private readonly Logging log = new()
        {
            objectName = "Report"
        };

        public ReportController(
            IReportRepository reportRepository)
        {
            string methodName = nameof(ReportController);
            try
            {
                _reportRepository = reportRepository;
                log.LogInitialize(methodName, null, LogType.Info);
            }
            catch (Exception e)
            {
                log.LogInitialize(methodName, globalExceptions.StatusData(e), LogType.Error);
            }
        }

        /// <summary>
        /// Get Report Payment List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("report/payment/list")]
        public async Task<IActionResult> GetReportPaymentList([FromBody] ReportPaymentListRequest param)
        {
            string methodName = nameof(GetReportPaymentList);
            CommonResponse cr = new();

            try
            {
                var dataReport = await _reportRepository.GetReportPaymentSummaryList(param);
                if (dataReport.Count == 0)
                {
                    cr.Code = StatusCodes.Status204NoContent;
                    cr.Status = globalExceptions.StatusCode(cr.Code);
                    cr.Data = dataReport;
                    log.LogInitialize(methodName, dataReport, LogType.Info);
                    return Ok(cr);
                }

                log.LogInitialize(methodName, dataReport, LogType.Info);

                cr.Code = StatusCodes.Status200OK;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                cr.Data = dataReport;
                return Ok(cr);
            }
            catch (Exception e)
            {
                cr.Code = StatusCodes.Status400BadRequest;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                cr.Data = globalExceptions.StatusData(e);

                log.LogInitialize(methodName, cr.Data, LogType.Error);

                return BadRequest(cr);
            }
        }

        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("report/payment/export")]
        public async Task<IActionResult> ExportExcelPaymentList([FromBody] ReportPaymentListRequest param)
        {
            string methodName = nameof(ExportExcelPaymentList);
            GetListResponse result = new();

            try
            {
                var inquiryPaymentStream = await _reportRepository.ExportInquiryPaymentToExcel(param);
                if (param.IsCashAdvance)
                    return File(inquiryPaymentStream, AppSystem.ContentTypeExcel, AppSystem.FileNameInquiryPaymentCashAdvance(param.ReportType).ToXlsxFileName());
                else
                    return File(inquiryPaymentStream, AppSystem.ContentTypeExcel, AppSystem.FileNameInquiryPaymentTransaction(param.ReportType).ToXlsxFileName());
            }
            catch (Exception ex)
            {
                result.Code = StatusCodes.Status400BadRequest;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = globalExceptions.StatusData(ex);

                log.LogPagination(methodName, result.Data, param.Page, param.PageSize, LogType.Error);

                return BadRequest(result);
            }
        }

        /// <summary>
        /// Get Report Budget Transaction
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("report/budget/list")]
        public async Task<IActionResult> GetReportBudgetTransactions([FromBody] ParamGetRequestList param)
        {
            string methodName = nameof(GetReportBudgetTransactions);
            CommonResponse result = new();

            try
            {
                var dataReportBudget = await _reportRepository.GetReportBudgetTransactions(param);
                if (dataReportBudget.Count == 0)
                {
                    result.Code = StatusCodes.Status204NoContent;
                    result.Status = globalExceptions.StatusCode(result.Code);
                    result.Data = dataReportBudget;
                    log.LogInitialize(methodName, dataReportBudget, LogType.Info);
                    return Ok(result);
                }

                log.LogInitialize(methodName, dataReportBudget, LogType.Info);

                result.Code = StatusCodes.Status200OK;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = dataReportBudget;
                return Ok(result);
            }
            catch (Exception e)
            {
                result.Code = StatusCodes.Status400BadRequest;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = globalExceptions.StatusData(e);

                log.LogInitialize(methodName, result.Data, LogType.Error);

                return BadRequest(result);
            }
        }
        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("report/budget/export")]
        public async Task<IActionResult> ExportExcelBudgetTransactions([FromBody] ParamGetRequestList param)
        {
            string methodName = nameof(ExportExcelBudgetTransactions);
            GetListResponse result = new();
            try
            {
                MemoryStream inquiryBudgetStream = await _reportRepository.ExportInquiryBudgetToExcel(param);
                return File(inquiryBudgetStream, AppSystem.ContentTypeExcel, AppSystem.FileNameInquiryBudget.ToXlsxFileName());
            }
            catch (Exception ex)
            {
                result.Code = StatusCodes.Status400BadRequest;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = globalExceptions.StatusData(ex);

                log.LogPagination(methodName, result.Data, param.Page, param.PageSize, LogType.Error);

                return BadRequest(result);
            }
        }

        /// <summary>
        /// Get Report Voucher List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("report/voucher/list")]
        public async Task<IActionResult> GetVoucherList([FromBody] ReportVoucherListRequest param)
        {
            string methodName = nameof(GetVoucherList);
            CommonResponse result = new();

            try
            {
                var data = await _reportRepository.GetReportVoucherList(param);
                if (data.Count == 0)
                {
                    result.Code = StatusCodes.Status204NoContent;
                    result.Status = globalExceptions.StatusCode(result.Code);
                    result.Data = data;
                    log.LogInitialize(methodName, data, LogType.Info);
                    return Ok(result);
                }

                log.LogInitialize(methodName, data, LogType.Info);

                result.Code = StatusCodes.Status200OK;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = data;
                return Ok(result);
            }
            catch (Exception e)
            {
                result.Code = StatusCodes.Status400BadRequest;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = globalExceptions.StatusData(e);

                log.LogInitialize(methodName, result.Data, LogType.Error);

                return BadRequest(result);
            }
        }

        /// <summary>
        /// Get Report Pajak List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("report/pajak/list")]
        public async Task<IActionResult> GetPajakList([FromBody] ReportPajakListRequest param)
        {
            string methodName = nameof(GetVoucherList);
            CommonResponse result = new();

            try
            {
                var data = await _reportRepository.GetReportPajakList(param);
                if (data.Count == 0)
                {
                    result.Code = StatusCodes.Status204NoContent;
                    result.Status = globalExceptions.StatusCode(result.Code);
                    result.Data = data;
                    log.LogInitialize(methodName, data, LogType.Info);
                    return Ok(result);
                }

                log.LogInitialize(methodName, data, LogType.Info);

                result.Code = StatusCodes.Status200OK;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = data;
                return Ok(result);
            }
            catch (Exception e)
            {
                result.Code = StatusCodes.Status400BadRequest;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = globalExceptions.StatusData(e);

                log.LogInitialize(methodName, result.Data, LogType.Error);

                return BadRequest(result);
            }
        }

        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("report/pajak/export")]
        public async Task<IActionResult> ExportExcelPajak([FromBody] ReportPajakListRequest param)
        {
            string methodName = nameof(ExportExcelPaymentList);
            GetListResponse result = new();

            try
            {
                var inquiryPaymentStream = await _reportRepository.ExportPajakToExcel(param);

                return File(inquiryPaymentStream, AppSystem.ContentTypeExcel, AppSystem.FileNameInquiryPajak(param.TransferTimeStart, param.TransferTimeEnd).ToXlsxFileName());
            }
            catch (Exception ex)
            {
                result.Code = StatusCodes.Status400BadRequest;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = globalExceptions.StatusData(ex);

                log.LogPagination(methodName, result.Data, param.Page, param.PageSize, LogType.Error);

                return BadRequest(result);
            }
        }

        /// <summary>
        /// Get Count Not Updated Voucher
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpGet("report/voucher/countnotupdated")]
        public async Task<IActionResult> GetCountNotUpdatedVoucher()
        {
            string methodName = nameof(GetCountNotUpdatedVoucher);
            CommonResponse result = new();

            try
            {
                var data = await _reportRepository.GetCountNotUpdatedVoucher();
                if (data > 0)
                {
                    result.Code = StatusCodes.Status204NoContent;
                    result.Status = globalExceptions.StatusCode(result.Code);
                    result.Data = data;
                    log.LogInitialize(methodName, data, LogType.Info);
                    return Ok(result);
                }

                log.LogInitialize(methodName, data, LogType.Info);

                result.Code = StatusCodes.Status200OK;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = data;
                return Ok(result);
            }
            catch (Exception e)
            {
                result.Code = StatusCodes.Status400BadRequest;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = globalExceptions.StatusData(e);

                log.LogInitialize(methodName, result.Data, LogType.Error);

                return BadRequest(result);
            }
        }

        /// <summary>
        /// Get Count Total Voucher Per Year
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpGet("report/voucher/countperyear")]
        public async Task<IActionResult> GetCountPerYearVoucher()
        {
            string methodName = nameof(GetCountPerYearVoucher);
            CommonResponse result = new();

            try
            {
                var data = await _reportRepository.GetCountPerYearVoucher();
                if (data > 0)
                {
                    result.Code = StatusCodes.Status204NoContent;
                    result.Status = globalExceptions.StatusCode(result.Code);
                    result.Data = data;
                    log.LogInitialize(methodName, data, LogType.Info);
                    return Ok(result);
                }

                log.LogInitialize(methodName, data, LogType.Info);

                result.Code = StatusCodes.Status200OK;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = data;
                return Ok(result);
            }
            catch (Exception e)
            {
                result.Code = StatusCodes.Status400BadRequest;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = globalExceptions.StatusData(e);

                log.LogInitialize(methodName, result.Data, LogType.Error);

                return BadRequest(result);
            }
        }

        /// <summary>
        /// Get Report PO Shopping Cart List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("report/po/shoppingcart/list")]
        public async Task<IActionResult> GetReportPOShoppingCartList([FromBody] ReportPORequest request)
        {
            CommonResponse response = new();
            try
            {
                var data = await _reportRepository.GetReportPOShoppingCartList(request);
                if (data.Count == 0)
                {
                    response.Code = StatusCodes.Status204NoContent;
                    response.Status = globalExceptions.StatusCode(response.Code);
                    response.Data = data;
                    log.LogInitialize(nameof(GetReportPOShoppingCartList), data, LogType.Info);
                    return Ok(response);
                }
                response.Code = StatusCodes.Status200OK;
                response.Status = globalExceptions.StatusCode(response.Code);
                response.Data = data;
                log.LogInitialize(nameof(GetReportPOShoppingCartList), data, LogType.Info);
                return Ok(response);
            }
            catch (Exception e)
            {
                response.Code = StatusCodes.Status400BadRequest;
                response.Status = globalExceptions.StatusCode(response.Code);
                response.Data = globalExceptions.StatusData(e);
                log.LogInitialize(nameof(GetReportPOShoppingCartList), response.Data, LogType.Error);
                return BadRequest(response);
            }
        }

        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("report/po/shoppingcart/export")]
        public async Task<IActionResult> GetReportPOShoppingCartExport(ReportPORequest request)
        {
            var worksheetName = "Report PO - Shopping Cart";
            var fileDownloadName = worksheetName + " - Export".ToXlsxFileName();
            GetListResponse result = new();
            try
            {
                request.IsExport = true;
                var responseList = await _reportRepository.GetReportPOShoppingCartExport(request);
                var stream = new MemoryStream();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var excelPackage = new ExcelPackage(stream))
                {
                    var excelWorksheet = excelPackage.Workbook.Worksheets.Add(worksheetName);

                    #region header
                    excelWorksheet.Cells["A5:AI5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    excelWorksheet.Cells["A5:AI5"].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);

                    excelWorksheet.Cells["A1"].Value = worksheetName;
                    excelWorksheet.Cells["A2"].Value = "APS Revamp";
                    excelWorksheet.Cells["A3"].Value = $"Date: {DateTime.Now.ToString("yyyy-MM-dd")}";
                    excelWorksheet.Cells["A4"].Value = $"Report PO From {request.FilterStartPODate ?? string.Empty} - To {request.FilterEndPODate ?? string.Empty}";

                    var headerCol = 1;
                    excelWorksheet.Columns[headerCol].Width = 5;
                    excelWorksheet.Cells[5, headerCol++].Value = "No";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PR No";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PR Status";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PR Date";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "Requester";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "Department";

                    excelWorksheet.Columns[headerCol].Width = 50;
                    excelWorksheet.Cells[5, headerCol++].Value = "Item Name";
                    excelWorksheet.Columns[headerCol].Width = 30;
                    excelWorksheet.Cells[5, headerCol++].Value = "Account Code";
                    excelWorksheet.Columns[headerCol].Width = 30;
                    excelWorksheet.Cells[5, headerCol++].Value = "Cost Center";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Vendor Selection";
                    excelWorksheet.Columns[headerCol].Width = 10;
                    excelWorksheet.Cells[5, headerCol++].Value = "Currency";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PR Approval Name";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PR Approval Title";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PR Approval Date";

                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PO No";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PO Status";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PO Date";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PO Amount";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PO Approver Date";

                    excelWorksheet.Columns[headerCol].Width = 10;
                    excelWorksheet.Cells[5, headerCol++].Value = "TAT WD";
                    excelWorksheet.Columns[headerCol].Width = 10;
                    excelWorksheet.Cells[5, headerCol++].Value = "SLA WD";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "SLA Status";

                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "DN No";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "DN Status";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "DN Date";
                    excelWorksheet.Columns[headerCol].Width = 10;
                    excelWorksheet.Cells[5, headerCol++].Value = "DN Qty";

                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Invoice No";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Invoice Status";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "Invoice Date";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Invoice Amount";

                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PPn";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PPh 23";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PPh 42";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Invoice after Tax";

                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol].Value = "Remarks";
                    #endregion

                    #region body
                    var row = 6;
                    foreach (var response in responseList)
                    {
                        var bodyCol = 1;
                        excelWorksheet.Cells[row, bodyCol++].Value = row - 5;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.PRNo;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PRStatusName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PRDate;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.Requester;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.Department;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.ItemName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.AccountCode;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.CostCenter;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.VendorSelection;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.Currency;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PRApprovalName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PRApprovalTitle;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PRApprovalDate;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.PONo;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.POStatusName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PODate;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.POAmount;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.POApproverDate;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.TATWD;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.SLAWD;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.SLAStatus;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.DNNo;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.DNStatusName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.DNDate;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.DNQty;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.InvoiceNo;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.InvoiceStatusName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.InvoiceDate;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.InvoiceAmount;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.PPN;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PPH23;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PPH42;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.InvoiceAmountAfterTax;

                        excelWorksheet.Cells[row, bodyCol].Value = string.Empty; // Remarks

                        row++;
                    }
                    #endregion

                    await excelPackage.SaveAsync();
                }

                stream.Position = 0;
                return File(stream, AppSystem.ContentTypeExcel, fileDownloadName);
            }
            catch (Exception ex)
            {
                result.Code = StatusCodes.Status400BadRequest;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = globalExceptions.StatusData(ex);
                log.LogPagination(nameof(GetReportPOShoppingCartExport), result.Data, request.Page, request.PageSize, LogType.Error);
                return BadRequest(result);
            }
        }

        /// <summary>
        /// Get Report PO Non Shopping Cart List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("report/po/nonshoppingcart/list")]
        public async Task<IActionResult> GetReportPONonShoppingCartList([FromBody] ReportPORequest request)
        {
            CommonResponse response = new();
            try
            {
                var data = await _reportRepository.GetReportPONonShoppingCartList(request);
                if (data.Count == 0)
                {
                    response.Code = StatusCodes.Status204NoContent;
                    response.Status = globalExceptions.StatusCode(response.Code);
                    response.Data = data;
                    log.LogInitialize(nameof(GetReportPOShoppingCartList), data, LogType.Info);
                    return Ok(response);
                }
                response.Code = StatusCodes.Status200OK;
                response.Status = globalExceptions.StatusCode(response.Code);
                response.Data = data;
                log.LogInitialize(nameof(GetReportPOShoppingCartList), data, LogType.Info);
                return Ok(response);
            }
            catch (Exception e)
            {
                response.Code = StatusCodes.Status400BadRequest;
                response.Status = globalExceptions.StatusCode(response.Code);
                response.Data = globalExceptions.StatusData(e);
                log.LogInitialize(nameof(GetReportPOShoppingCartList), response.Data, LogType.Error);
                return BadRequest(response);
            }
        }

        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("report/po/nonshoppingcart/export")]
        public async Task<IActionResult> GetReportPONonShoppingCartExport(ReportPORequest request)
        {
            var worksheetName = "Report PO - Non Shopping Cart";
            var fileDownloadName = worksheetName + " - Export".ToXlsxFileName();
            GetListResponse result = new();
            try
            {
                request.IsExport = true;
                var responseList = await _reportRepository.GetReportPONonShoppingCartExport(request);
                var stream = new MemoryStream();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var excelPackage = new ExcelPackage(stream))
                {
                    var excelWorksheet = excelPackage.Workbook.Worksheets.Add(worksheetName);

                    #region header
                    excelWorksheet.Cells["A5:AS5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    excelWorksheet.Cells["A5:AS5"].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);

                    excelWorksheet.Cells["A1"].Value = worksheetName;
                    excelWorksheet.Cells["A2"].Value = "APS Revamp";
                    excelWorksheet.Cells["A3"].Value = $"Date: {DateTime.Now.ToString("yyyy-MM-dd")}";
                    excelWorksheet.Cells["A4"].Value = $"Report PO From {request.FilterStartPODate ?? string.Empty} - To {request.FilterEndPODate ?? string.Empty}";

                    var headerCol = 1;
                    excelWorksheet.Columns[headerCol].Width = 5;
                    excelWorksheet.Cells[5, headerCol++].Value = "No";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PR No";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PR Status";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PR Date";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "Requester";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "Department";

                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Type Of Transaction";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Buyer User Name";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Total Budget Estimation";

                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Critical";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Category";

                    excelWorksheet.Columns[headerCol].Width = 50;
                    excelWorksheet.Cells[5, headerCol++].Value = "Item Name";
                    excelWorksheet.Columns[headerCol].Width = 30;
                    excelWorksheet.Cells[5, headerCol++].Value = "Account Code";
                    excelWorksheet.Columns[headerCol].Width = 30;
                    excelWorksheet.Cells[5, headerCol++].Value = "Cost Center";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Vendor Selection";
                    excelWorksheet.Columns[headerCol].Width = 10;
                    excelWorksheet.Cells[5, headerCol++].Value = "Currency";

                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PR Posted Date";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "Delivery Request Date";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "Final Spec Req Date";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "Generate Proc Sum Date";

                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PO No";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PO Status";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PO Date";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PO Amount";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "PO Approver Date";

                    excelWorksheet.Columns[headerCol].Width = 10;
                    excelWorksheet.Cells[5, headerCol++].Value = "TAT WD";
                    excelWorksheet.Columns[headerCol].Width = 10;
                    excelWorksheet.Cells[5, headerCol++].Value = "SLA WD";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "SLA Status";

                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Vendor";

                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Selected";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Selected Vendors Total Budget";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Realised Saving";

                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "DN No";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "DN Status";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "DN Date";
                    excelWorksheet.Columns[headerCol].Width = 10;
                    excelWorksheet.Cells[5, headerCol++].Value = "DN Qty";

                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Invoice No";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Invoice Status";
                    excelWorksheet.Columns[headerCol].Width = 20;
                    excelWorksheet.Cells[5, headerCol++].Value = "Invoice Date";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Invoice Amount";

                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PPn";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PPh 23";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "PPh 42";
                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol++].Value = "Invoice after Tax";

                    excelWorksheet.Columns[headerCol].Width = 15;
                    excelWorksheet.Cells[5, headerCol].Value = "Remarks";
                    #endregion

                    #region body
                    var row = 6;
                    foreach (var response in responseList)
                    {
                        var bodyCol = 1;
                        excelWorksheet.Cells[row, bodyCol++].Value = row - 5;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.PRNo;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PRStatusName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PRDate;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.Requester;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.Department;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.TypeOfTransaction;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.BuyerUserName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.TotalBudgetEstimation;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.Critical;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.Category;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.ItemName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.AccountCode;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.CostCenter;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.VendorSelection;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.Currency;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.PRPostedDate;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.DeliveryRequestDate;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.FinalSpecReqDate;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.GenerateProcSumDate;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.PONo;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.POStatusName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PODate;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.POAmount;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.POApproverDate;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.TATWD;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.SLAWD;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.SLAStatus;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.Vendor1;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.SelectedVendor1;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.SelectedVendorsTotalBudget;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.RealisedSaving;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.DNNo;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.DNStatusName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.DNDate;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.DNQty;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.InvoiceNo;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.InvoiceStatusName;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.InvoiceDate;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.InvoiceAmount;

                        excelWorksheet.Cells[row, bodyCol++].Value = response.PPN;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PPH23;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.PPH42;
                        excelWorksheet.Cells[row, bodyCol++].Value = response.InvoiceAmountAfterTax;

                        excelWorksheet.Cells[row, bodyCol].Value = string.Empty; // Remarks

                        row++;
                    }
                    #endregion

                    await excelPackage.SaveAsync();
                }

                stream.Position = 0;
                return File(stream, AppSystem.ContentTypeExcel, fileDownloadName);
            }
            catch (Exception ex)
            {
                result.Code = StatusCodes.Status400BadRequest;
                result.Status = globalExceptions.StatusCode(result.Code);
                result.Data = globalExceptions.StatusData(ex);
                log.LogPagination(nameof(GetReportPOShoppingCartExport), result.Data, request.Page, request.PageSize, LogType.Error);
                return BadRequest(result);
            }
        }

        /// <summary>
        /// Get Report PO Non Shopping Cart List
        /// </summary>
        /// <returns></returns>
        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("Report/DueDiligence/Json")]
        public async Task<IActionResult> DueDiligenceReportJsonPost([FromBody] DueDiligenceReportRequestModel request)
        {
            try
            {
                CommonResponse cr = new();
                var data = await _reportRepository.DueDiligenceReportJsonPost(request);
                cr.Data = data;
                cr.Code = (data.Count == 0) ? StatusCodes.Status204NoContent : StatusCodes.Status200OK;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                log.LogInitialize(nameof(GetReportPOShoppingCartList), cr.Data, LogType.Info);
                return Ok(cr);
            }
            catch (Exception e)
            {
                CommonResponse cr = new();
                cr.Data = globalExceptions.StatusData(e);
                cr.Code = StatusCodes.Status400BadRequest;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                log.LogInitialize(nameof(GetReportPOShoppingCartList), cr.Data, LogType.Error);
                return BadRequest(cr);
            }
        }

        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("Report/DueDiligence/Xlsx")]
        public async Task<IActionResult> DueDiligenceReportXlsxPost([FromBody] DueDiligenceReportRequestModel request)
        {
            GetListResponse response = new();
            try
            {
                List<DueDiligenceReportResponseModel> list = await _reportRepository.DueDiligenceReportJsonPost(request);
                MemoryStream stream = new();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                string wName = "Report Due Diligence";
                using (ExcelPackage ep = new(stream))
                {
                    var ws = ep.Workbook.Worksheets.Add(wName);

                    #region header

                    int r = 0;
                    int c = 0;
                    ++c;

                    ws.Cells[++r, c].Value = wName;
                    ws.Cells[++r, c].Value = "APS Revamp";
                    ws.Cells[++r, c].Value = $"Date: {DateTime.Now.ToString("yyyy-MM-dd")}";
                    ws.Cells[++r, c].Value = $"From {string.Empty} - To {string.Empty}";

                    ++r;
                    --c;

                    ws.Cells[r, ++c].Value = "No";
                    ws.Columns[c].Width = 5;

                    ws.Cells[r, ++c].Value = "Vendor Name";
                    ws.Columns[c].Width = 40;
                    ws.Cells[r, ++c].Value = "Company Address";
                    ws.Columns[c].Width = 100;
                    ws.Cells[r, ++c].Value = "Email Address";
                    ws.Columns[c].Width = 40;
                    ws.Cells[r, ++c].Value = "Type Supplier";
                    ws.Columns[c].Width = 15;

                    ws.Cells[r, ++c].Value = "DD Status";
                    ws.Columns[c].Width = 25;
                    ws.Cells[r, ++c].Value = "PEP / Non-PEP";
                    ws.Columns[c].Width = 15;
                    ws.Cells[r, ++c].Value = "DD Period Start";
                    ws.Columns[c].Width = 15;
                    ws.Cells[r, ++c].Value = "DD Period End";
                    ws.Columns[c].Width = 15;

                    ws.Cells[r, ++c].Value = "DD Created Date";
                    ws.Columns[c].Width = 20;
                    ws.Cells[r, ++c].Value = "DD Last Update";
                    ws.Columns[c].Width = 20;
                    ws.Cells[r, ++c].Value = "DD Activity";
                    ws.Columns[c].Width = 10;

                    ws.Cells[r, 1, r, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[r, 1, r, c].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);

                    #endregion

                    #region body
                    var h = r;
                    foreach (DueDiligenceReportResponseModel.VendorModel each in list.Select(x => x.Vendor))
                    {
                        ++r;
                        c = 0;

                        ws.Cells[r, ++c].Value = r - 5;

                        ws.Cells[r, ++c].Value = each.Name;
                        ws.Cells[r, ++c].Value = each.Address;
                        ws.Cells[r, ++c].Value = each.eMail;
                        ws.Cells[r, ++c].Value = each.SupplierType_SubCategory.SubCategoryName;

                        ws.Cells[r, ++c].Value = each.VendorDueDeligence.StatusMasterTable.Name;
                        ws.Cells[r, ++c].Value = each.VendorDueDeligence.VendorSimpleDueDiligence.PepResult;
                        ws.Cells[r, ++c].Value = each.VendorDueDeligence.PeriodValidFromDateText;
                        ws.Cells[r, ++c].Value = each.VendorDueDeligence.PeriodValidToDateText;

                        ws.Cells[r, ++c].Value = each.VendorDueDeligence.CreatedTimeText;
                        ws.Cells[r, ++c].Value = each.VendorDueDeligence.LastUpdatedTimeText;
                        ws.Cells[r, c].Value = each.VendorDueDeligence.OrderNumberText;
                    }
                    #endregion

                    ws.Cells[h, 1, r, c].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[h, 1, r, c].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    await ep.SaveAsync();
                }

                stream.Position = 0;
                string contentType = AppSystem.ContentTypeExcel;
                string fName = wName + " - Export".ToXlsxFileName();
                return File(stream, contentType, fName);
            }
            catch (Exception e)
            {
                response.Data = globalExceptions.StatusData(e);
                response.Code = StatusCodes.Status400BadRequest;
                response.Status = globalExceptions.StatusCode(response.Code);
                log.LogPagination(nameof(GetReportPOShoppingCartExport), response.Data, request.Start, request.Length, LogType.Error);
                return BadRequest(response);
            }
        }

        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("Report/Procurement/DataTables/Data")]
        public async Task<IActionResult> ProcurementDataTablesData([FromBody] Models.Report.Procurement.DataTables.Request request)
        {
            CommonResponse cr = new();
            try
            {
                var data = await _reportRepository.ProcurementDataTablesData(request);

                cr.Data = data;
                cr.Code = data.Any() ? StatusCodes.Status200OK : StatusCodes.Status204NoContent;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                log.LogInitialize(nameof(ProcurementDataTablesData), cr.Data, LogType.Info);
                return Ok(cr);
            }
            catch (Exception e)
            {
                cr.Data = globalExceptions.StatusData(e);
                cr.Code = StatusCodes.Status400BadRequest;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                log.LogInitialize(nameof(ProcurementDataTablesData), cr.Data, LogType.Error);
                return BadRequest(cr);
            }
        }

        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("Report/Procurement/Xlsx")]
        public async Task<IActionResult> ProcurementXlsx([FromBody] Models.Report.Procurement.DataTables.Request request)
        {
            GetListResponse response = new();
            try
            {
                var list = await _reportRepository.ProcurementDataTablesData(request);
                MemoryStream stream = new();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                string wsName = "Report PO";
                using (ExcelPackage ep = new(stream))
                {
                    var ws = ep.Workbook.Worksheets.Add(wsName);

                    int r = 0;
                    int c = 1;

                    Dictionary<string, int> d = new();
                    #region column
                    var m = new Models.Report.Procurement.DataTables.Data();

                    d.Add(nameof(m.PR_No), ++c);
                    d.Add(nameof(m.PR_Status), ++c);
                    d.Add(nameof(m.PR_Date), ++c);
                    d.Add(nameof(m.Requester), ++c);
                    d.Add(nameof(m.Department), ++c);
                    d.Add(nameof(m.Type_Of_Transaction), ++c);
                    d.Add(nameof(m.Buyer_User_Name), ++c);
                    d.Add(nameof(m.Total_Budget_Estimation), ++c);
                    d.Add(nameof(m.Critical), ++c);
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
                    d.Add(nameof(m.Selected_Vendors_Total_Budget), ++c);
                    d.Add(nameof(m.Realised_Saving), ++c);

                    d.Add(nameof(m.Order_Type), ++c);
                    d.Add(nameof(m.Order_No), ++c);
                    d.Add(nameof(m.Order_Status), ++c);
                    d.Add(nameof(m.Order_Date), ++c);
                    d.Add(nameof(m.Order_Grand_Total_Amount), ++c);
                    d.Add(nameof(m.Approver_Order_And_Date), ++c);

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
                    d.Add(nameof(m.Remarks), c);

                    #endregion

                    #region header

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

                    // Value & Width
                    ws.Cells[++r, 1].Value = "No";
                    ws.Columns[1].Width = 5;
                    foreach (var x in d)
                    {
                        ws.Cells[r, x.Value].Value = x.Key.Replace("_", " ");
                        ws.Columns[x.Value].Width = 30;
                    }

                    // Fill
                    ws.Cells[r, 1, r, d.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[r, 1, r, d.Count].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);

                    #endregion

                    #region body
                    int b = r;
                    foreach (var x in list)
                    {
                        ++r;

                        ws.Cells[r, 1].Value = r - b;

                        ws.Cells[r, d[nameof(x.PR_No)]].Value = x.PR_No;
                        ws.Cells[r, d[nameof(x.PR_Status)]].Value = x.PR_Status;

                        if ((x.PR_Status ?? "").Trim().ToLower() == "cancel")
                        {
                            ws.Cells[r, 1, r, d.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[r, 1, r, d.Count].Style.Fill.BackgroundColor.SetColor(Color.Red);
                        }

                        ws.Cells[r, d[nameof(x.PR_Date)]].Value = x.PR_Date;
                        ws.Cells[r, d[nameof(x.Requester)]].Value = x.Requester;
                        ws.Cells[r, d[nameof(x.Department)]].Value = x.Department;
                        ws.Cells[r, d[nameof(x.Type_Of_Transaction)]].Value = x.Type_Of_Transaction;
                        ws.Cells[r, d[nameof(x.Buyer_User_Name)]].Value = x.Buyer_User_Name;
                        ws.Cells[r, d[nameof(x.Total_Budget_Estimation)]].Value = x.Total_Budget_Estimation;
                        ws.Cells[r, d[nameof(x.Critical)]].Value = x.Critical;
                        ws.Cells[r, d[nameof(x.Category)]].Value = x.Category;
                        ws.Cells[r, d[nameof(x.Item_Name)]].Value = x.Item_Name;
                        ws.Cells[r, d[nameof(x.Account_Code)]].Value = x.Account_Code;
                        ws.Cells[r, d[nameof(x.Cost_Center)]].Value = x.Cost_Center;
                        ws.Cells[r, d[nameof(x.Vendor_Selection)]].Value = x.Vendor_Selection;
                        ws.Cells[r, d[nameof(x.Currency)]].Value = x.Currency;
                        ws.Cells[r, d[nameof(x.PR_Posted_Date)]].Value = x.PR_Posted_Date;
                        ws.Cells[r, d[nameof(x.Delivery_Request_Date)]].Value = x.Delivery_Request_Date;
                        ws.Cells[r, d[nameof(x.Final_Spec_Req_Date)]].Value = x.Final_Spec_Req_Date;
                        ws.Cells[r, d[nameof(x.Generate_Proc_Sum_Date)]].Value = x.Generate_Proc_Sum_Date;

                        ws.Cells[r, d[nameof(x.TAT_WD)]].Value = x.TAT_WD;
                        ws.Cells[r, d[nameof(x.SLA_WD)]].Value = x.SLA_WD;
                        ws.Cells[r, d[nameof(x.SLA_Status)]].Value = x.SLA_Status;
                        ws.Cells[r, d[nameof(x.Vendor)]].Value = x.Vendor;
                        ws.Cells[r, d[nameof(x.Selected)]].Value = x.Selected;
                        ws.Cells[r, d[nameof(x.Selected_Vendors_Total_Budget)]].Value = x.Selected_Vendors_Total_Budget;
                        ws.Cells[r, d[nameof(x.Realised_Saving)]].Value = x.Realised_Saving;

                        ws.Cells[r, d[nameof(x.Order_Type)]].Value = x.Order_Type;
                        ws.Cells[r, d[nameof(x.Order_No)]].Value = x.Order_No;
                        ws.Cells[r, d[nameof(x.Order_Status)]].Value = x.Order_Status;
                        ws.Cells[r, d[nameof(x.Order_Date)]].Value = x.Order_Date;
                        ws.Cells[r, d[nameof(x.Order_Grand_Total_Amount)]].Value = x.Order_Grand_Total_Amount;
                        ws.Cells[r, d[nameof(x.Approver_Order_And_Date)]].Value = x.Approver_Order_And_Date;

                        ws.Cells[r, d[nameof(x.DN_No)]].Value = x.DN_No;
                        ws.Cells[r, d[nameof(x.DN_Status)]].Value = x.DN_Status;
                        ws.Cells[r, d[nameof(x.DN_Date)]].Value = x.DN_Date;
                        ws.Cells[r, d[nameof(x.DN_Qty)]].Value = x.DN_Qty;

                        ws.Cells[r, d[nameof(x.Invoice_No)]].Value = x.Invoice_No;
                        ws.Cells[r, d[nameof(x.Invoice_Status)]].Value = x.Invoice_Status;
                        ws.Cells[r, d[nameof(x.Invoice_Date)]].Value = x.Invoice_Date;
                        ws.Cells[r, d[nameof(x.Invoice_Amount)]].Value = x.Invoice_Amount;
                        ws.Cells[r, d[nameof(x.PPn)]].Value = x.PPn;
                        ws.Cells[r, d[nameof(x.PPh_23)]].Value = x.PPh_23;
                        ws.Cells[r, d[nameof(x.PPh_42)]].Value = x.PPh_42;
                        ws.Cells[r, d[nameof(x.Invoice_After_Tax_Or_Grand_Total)]].Value = x.Invoice_After_Tax_Or_Grand_Total;
                        ws.Cells[r, d[nameof(x.Remarks)]].Value = x.Remarks;
                    }
                    #endregion
                    ws.Cells[b, 1, r, d.Count].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[b, 1, r, d.Count].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    await ep.SaveAsync();
                }

                stream.Position = 0;
                string contentType = AppSystem.ContentTypeExcel;
                string fName = wsName + " - Export".ToXlsxFileName();
                return File(stream, contentType, fName);
            }
            catch (Exception e)
            {
                response.Data = globalExceptions.StatusData(e);
                response.Code = StatusCodes.Status400BadRequest;
                response.Status = globalExceptions.StatusCode(response.Code);
                log.LogPagination(nameof(ProcurementXlsx), response.Data, request.Start, request.Length, LogType.Error);
                return BadRequest(response);
            }
        }

        [HttpPost(Routes.Report_Contract_Index_Model)]
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
                APS_Common.BaseLogging.LogInfo(id, c, m, nameof(response) + nl + JsonSerializer.Serialize(response));
                return StatusCode(response.StatusCode, response);
            }
            try
            {
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
                APS_Common.BaseLogging.LogInfo(id, c, m, nameof(response) + nl + JsonSerializer.Serialize(response));
                return StatusCode(response.StatusCode, response);
            }
        }

        [HttpPost(Routes.Report_Contract_Index_DataTables)]
        public IActionResult Report_Contract_Index_DataTables(APS_Common.Models.Report.Contract.Index.DataTables.Request.Root request)
        {
            var id = request.UUID;
            var c = GetType().Name;
            var m = MethodBase.GetCurrentMethod().Name;
            var nl = Environment.NewLine;
            var response = new APS_Common.Models.DataTables.Response<APS_Common.Models.Report.Contract.Index.DataTables.Row.Root>();
            if (!ModelState.IsValid)
            {
                response.MessageArray = ModelState.Values.SelectMany(mse => mse.Errors).Select(me => me.ErrorMessage).ToArray();
                response.StatusCode = StatusCodes.Status400BadRequest;
                APS_Common.BaseLogging.LogInfo(id, c, m, nameof(response) + nl + JsonSerializer.Serialize(response));
                return StatusCode(response.StatusCode, response);
            }
            try
            {
                response = _reportRepository.Report_Contract_Index_DataTables(request);
                response.StatusCode = StatusCodes.Status200OK;
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception e)
            {
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.Message) + nl + e.Message);
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.StackTrace) + nl + e.StackTrace);
                response.MessageArray = [.. response.MessageArray, e.GetBaseException().Message];
                response.StatusCode = StatusCodes.Status400BadRequest;
                APS_Common.BaseLogging.LogInfo(id, c, m, nameof(response) + nl + JsonSerializer.Serialize(response));
                return StatusCode(response.StatusCode, response);
            }
        }

        [HttpPost(Routes.Report_Contract_Index_DataTables_Excel)]
        public IActionResult Report_Contract_Index_Excel(APS_Common.Models.Report.Contract.Index.DataTables.Request.Root request)
        {
            var id = request.UUID;
            var c = GetType().Name;
            var m = MethodBase.GetCurrentMethod().Name;
            var nl = Environment.NewLine;
            if (!ModelState.IsValid)
            {
                var errorMessageArray = ModelState.Values.SelectMany(mse => mse.Errors).Select(me => me.ErrorMessage).ToArray();
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(errorMessageArray) + nl + JsonSerializer.Serialize(errorMessageArray));
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(request) + nl + JsonSerializer.Serialize(request));
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            try
            {
                var name = "Report Contract";
                var stream = new MemoryStream();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var excelPackage = new ExcelPackage(stream);
                var excelWorksheet = excelPackage.Workbook.Worksheets.Add(name);
                excelWorksheet.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Top;

                var row = 0;
                var col = 0;

                excelWorksheet.Cells[++row, 1].Value = name;
                excelWorksheet.Cells[++row, 1].Value = "APS Revamp Procurement";
                excelWorksheet.Cells[++row, 1].Value = $"Date : {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                var begin = request.PRF.RequestDate.Begin is null ? "" : ((DateTime)request.PRF.RequestDate.Begin).ToString("yyyy-MM-dd");
                var end = request.PRF.RequestDate.End is null ? "" : ((DateTime)request.PRF.RequestDate.End).ToString("yyyy-MM-dd");
                excelWorksheet.Cells[++row, 1].Value = $"Report Contract From {begin} - To {end}";

                #region header
                ++row;
                col = 0;

                excelWorksheet.Cells[row, ++col].Value = "No";
                excelWorksheet.Columns[col].Width = 5;
                excelWorksheet.Cells[row, ++col].Value = "PR No";
                excelWorksheet.Columns[col].Width = 20;
                excelWorksheet.Cells[row, ++col].Value = "PR Status";
                excelWorksheet.Columns[col].Width = 10;
                excelWorksheet.Cells[row, ++col].Value = "PR Date";
                excelWorksheet.Columns[col].Width = 20;
                excelWorksheet.Cells[row, ++col].Value = "Requestor Name";
                excelWorksheet.Columns[col].Width = 20;
                excelWorksheet.Cells[row, ++col].Value = "Procurement Buyer Name";
                excelWorksheet.Columns[col].Width = 30;
                excelWorksheet.Cells[row, col].Style.WrapText = true;
                excelWorksheet.Cells[row, ++col].Value = "Requestor Department";
                excelWorksheet.Columns[col].Width = 30;
                excelWorksheet.Cells[row, col].Style.WrapText = true;
                excelWorksheet.Cells[row, ++col].Value = "Account Code";
                excelWorksheet.Columns[col].Width = 30;
                excelWorksheet.Cells[row, ++col].Value = "Cost Center";
                excelWorksheet.Columns[col].Width = 30;
                excelWorksheet.Cells[row, ++col].Value = "Currency";
                excelWorksheet.Columns[col].Width = 10;
                excelWorksheet.Cells[row, ++col].Value = "PR Approval Date - Name";
                excelWorksheet.Columns[col].Width = 30;
                excelWorksheet.Cells[row, col].Style.WrapText = true;
                excelWorksheet.Cells[row, ++col].Value = "Contract Title";
                excelWorksheet.Columns[col].Width = 30;
                excelWorksheet.Cells[row, ++col].Value = "Contract Type";
                excelWorksheet.Columns[col].Width = 30;
                excelWorksheet.Cells[row, ++col].Value = "No.Contract";
                excelWorksheet.Columns[col].Width = 30;
                excelWorksheet.Cells[row, ++col].Value = "Vendor Name";
                excelWorksheet.Columns[col].Width = 30;
                excelWorksheet.Cells[row, ++col].Value = "Contract Period Start";
                excelWorksheet.Columns[col].Width = 15;
                excelWorksheet.Cells[row, col].Style.WrapText = true;
                excelWorksheet.Cells[row, ++col].Value = "Contract Period End";
                excelWorksheet.Columns[col].Width = 15;
                excelWorksheet.Cells[row, col].Style.WrapText = true;
                excelWorksheet.Cells[row, ++col].Value = "Amount Contract";
                excelWorksheet.Columns[col].Width = 20;
                excelWorksheet.Cells[row, ++col].Value = "AL Period Start";
                excelWorksheet.Columns[col].Width = 15;
                excelWorksheet.Cells[row, col].Style.WrapText = true;
                excelWorksheet.Cells[row, ++col].Value = "AL Period End";
                excelWorksheet.Columns[col].Width = 15;
                excelWorksheet.Cells[row, col].Style.WrapText = true;
                excelWorksheet.Cells[row, col].Style.WrapText = true;
                excelWorksheet.Cells[row, ++col].Value = "Upload Contract Date";
                excelWorksheet.Columns[col].Width = 20;
                excelWorksheet.Cells[row, col].Style.WrapText = true;
                excelWorksheet.Cells[row, ++col].Value = "Status SAT PR";
                excelWorksheet.Columns[col].Width = 15;
                excelWorksheet.Cells[row, col].Style.WrapText = true;
                excelWorksheet.Cells[row, ++col].Value = "Outsourcing Status";
                excelWorksheet.Columns[col].Width = 15;
                excelWorksheet.Cells[row, col].Style.WrapText = true;
                excelWorksheet.Cells[row, ++col].Value = "Remarks";
                excelWorksheet.Columns[col].Width = 30;

                excelWorksheet.Cells[row, 1, row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                excelWorksheet.Cells[row, 1, row, col].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);
                excelWorksheet.Cells[row, 1, row, col].Style.Font.Color.SetColor(Color.White);
                excelWorksheet.Cells[row, 1, row, col].AutoFilter = true;
                #endregion

                #region body
                request.UseLength = false;
                var response = _reportRepository.Report_Contract_Index_DataTables(request);
                var no = 0;
                foreach (var prf in response.Data.Select(root => root.PRF))
                {
                    ++row;
                    col = 0;

                    excelWorksheet.Cells[row, ++col].Value = ++no;
                    excelWorksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    excelWorksheet.Cells[row, ++col].Value = prf.PRFNo;
                    excelWorksheet.Cells[row, ++col].Value = prf.Status_MasterTable.Name;
                    excelWorksheet.Cells[row, ++col].Value = prf.RequestDate;
                    excelWorksheet.Cells[row, col].Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                    excelWorksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    excelWorksheet.Cells[row, ++col].Value = prf.RequestorAccount.Username;
                    excelWorksheet.Cells[row, ++col].Value = prf.BuyerAccount.Username;
                    excelWorksheet.Cells[row, ++col].Value = prf.RequestorAccount.CostCenter.CodeAndName;
                    excelWorksheet.Cells[row, col].Style.WrapText = true;
                    excelWorksheet.Cells[row, ++col].Value = prf.AccountMaster.AccountCodeAndDescription;
                    excelWorksheet.Cells[row, col].Style.WrapText = true;
                    excelWorksheet.Cells[row, ++col].Value = prf.CostCenter.CodeAndName;
                    excelWorksheet.Cells[row, col].Style.WrapText = true;
                    excelWorksheet.Cells[row, ++col].Value = prf.L_Currency_Code;
                    excelWorksheet.Cells[row, ++col].Value = prf.ApprovalRequestGroupMember.ApprovalDateAndUserName;
                    excelWorksheet.Cells[row, col].Style.WrapText = true;
                    excelWorksheet.Cells[row, ++col].Value = prf.PRFSummary.PRFSummaryDetail.Contract.ContractTitle;
                    excelWorksheet.Cells[row, col].Style.WrapText = true;
                    excelWorksheet.Cells[row, ++col].Value = prf.PRFSummary.PRFSummaryDetail.Contract.ContractType_MasterTable.Name;
                    excelWorksheet.Cells[row, col].Style.WrapText = true;
                    excelWorksheet.Cells[row, ++col].Value = prf.PRFSummary.PRFSummaryDetail.Contract.ContractNo;
                    excelWorksheet.Cells[row, col].Style.WrapText = true;
                    excelWorksheet.Cells[row, ++col].Value = prf.PRFSummary.PRFSummaryDetail.Vendor.Name;

                    excelWorksheet.Cells[row, ++col].Value = prf.PRFSummary.PRFSummaryDetail.Contract.ContractPeriodStart;
                    excelWorksheet.Cells[row, col].Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                    excelWorksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    excelWorksheet.Cells[row, ++col].Value = prf.PRFSummary.PRFSummaryDetail.Contract.ContractPeriodEnd;
                    excelWorksheet.Cells[row, col].Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                    excelWorksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    excelWorksheet.Cells[row, ++col].Value = prf.PRFSummary.PRFSummaryDetail.Contract.AmountContract;
                    excelWorksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                    excelWorksheet.Cells[row, col].Style.WrapText = false;
                    excelWorksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    excelWorksheet.Cells[row, ++col].Value = prf.PRFSummary.PRFSummaryDetail.Contract.ALPeriodStart;
                    excelWorksheet.Cells[row, col].Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                    excelWorksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    excelWorksheet.Cells[row, ++col].Value = prf.PRFSummary.PRFSummaryDetail.Contract.ALPeriodEnd;
                    excelWorksheet.Cells[row, col].Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                    excelWorksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    excelWorksheet.Cells[row, ++col].Value = prf.PRFSummary.PRFSummaryDetail.Contract.UploadFinalDate;
                    excelWorksheet.Cells[row, col].Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                    excelWorksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    if (prf.IsSecurityAssessment is null)
                    {
                        excelWorksheet.Cells[row, ++col].Value = string.Empty;
                    }
                    else if (prf.IsSecurityAssessment ?? true)
                    {
                        excelWorksheet.Cells[row, ++col].Value = "Critical";
                    }
                    else
                    {
                        excelWorksheet.Cells[row, ++col].Value = "Non Critical";
                    }

                    excelWorksheet.Cells[row, ++col].Value = prf.TypeOrder;
                    excelWorksheet.Cells[row, ++col].Value = prf.PRFSummary.PRFSummaryDetail.Contract.Remark;
                    excelWorksheet.Cells[row, col].Style.WrapText = true;
                }
                #endregion

                excelPackage.SaveAsync().Wait();
                stream.Position = 0;
                return File(stream, AppSystem.ContentTypeExcel, name.ToXlsxFileName());
            }
            catch (Exception e)
            {
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.Message) + nl + e.Message);
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.StackTrace) + nl + e.StackTrace);
                APS_Common.BaseLogging.LogDebug(id, c, m, nameof(request) + nl + JsonSerializer.Serialize(request));
                return BadRequest();
            }
        }

        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("Report/Procurement/DataTables/Data/WIP")]
        public async Task<IActionResult> ProcurementDataTablesData_Wip([FromBody] Models.Report.Procurement.DataTables.Request_WIP request)
        {
            CommonResponse cr = new();
            try
            {
                var data = await _reportRepository.ProcurementDataTablesData_WIP(request);

                cr.Data = data;
                cr.Code = data.Any() ? StatusCodes.Status200OK : StatusCodes.Status204NoContent;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                log.LogInitialize(nameof(ProcurementDataTablesData), cr.Data, LogType.Info);
                return Ok(cr);
            }
            catch (Exception e)
            {
                cr.Data = globalExceptions.StatusData(e);
                cr.Code = StatusCodes.Status400BadRequest;
                cr.Status = globalExceptions.StatusCode(cr.Code);
                log.LogInitialize(nameof(ProcurementDataTablesData), cr.Data, LogType.Error);
                return BadRequest(cr);
            }
        }

        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("Report/Procurement/Xlsx/WIPs")]
        public async Task<IActionResult> ProcurementXlsx_Wips([FromBody] Models.Report.Procurement.DataTables.Request_WIP request)
        {
            GetListResponse response = new();
            try
            {
                var list = await _reportRepository.ProcurementDataTablesData_WIP(request);
                MemoryStream stream = new();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                string wsName = "Report PO";
                using (ExcelPackage ep = new(stream))
                {
                    var ws = ep.Workbook.Worksheets.Add(wsName);
                    ws.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Top;

                    int r = 0;
                    int c = 1;

                    Dictionary<string, int> d = new();

                    #region column
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

                    #region header

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

                    // Value & Width
                    ws.Cells[++r, 1].Value = "No";
                    ws.Columns[1].Width = 5;
                    foreach (var x in d)
                    {
                        ws.Cells[r, x.Value].Value = x.Key.Replace("_", " ");
                        if (x.Key == nameof(m.Price_Per_Item)) ws.Cells[r, x.Value].Value = "Unit Price";
                        if (x.Key == nameof(m.Price_Per_Item_Inc_Other_Cost)) ws.Cells[r, x.Value].Value = "Unit Price Inc Other Cost";
                        ws.Columns[d[nameof(m.PR_No)]].Style.WrapText = true;
                    }

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

                    // Fill
                    ws.Cells[r, 1, r, d.Count + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[r, 1, r, d.Count + 1].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);
                    ws.Cells[r, 1, r, d.Count + 1].Style.Font.Color.SetColor(Color.White);
                    ws.Cells[r, 2, r, d.Count + 1].AutoFilter = true;
                    ws.Cells[r, 2, r + list.Count(), d.Count + 1].Style.WrapText = true;

                    #endregion

                    #region body
                    int b = r;
                    foreach (var x in list)
                    {
                        ++r;

                        ws.Cells[r, 1].Value = r - b;
                        ws.Cells[r, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        ws.Cells[r, d[nameof(x.PR_No)]].Value = x.PR_No;
                        ws.Cells[r, d[nameof(x.PR_Status)]].Value = x.PR_Status;

                        if ((x.PR_Status ?? "").Trim().ToLower() == "cancel")
                        {
                            ws.Cells[r, 1, r, d.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[r, 1, r, d.Count].Style.Fill.BackgroundColor.SetColor(Color.Red);
                        }

                        ws.Cells[r, d[nameof(x.PR_Date)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws.Cells[r, d[nameof(x.PR_Date)]].Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                        ws.Cells[r, d[nameof(x.PR_Date)]].Value = x.PR_Date;
                        ws.Cells[r, d[nameof(x.Requester)]].Value = x.Requester;
                        ws.Cells[r, d[nameof(x.Department)]].Value = x.Department;
                        ws.Cells[r, d[nameof(x.Type_Of_Transaction)]].Value = x.Type_Of_Transaction;
                        ws.Cells[r, d[nameof(x.Buyer_User_Name)]].Value = x.Buyer_User_Name;
                        ws.Cells[r, d[nameof(x.Total_Budget_Estimation)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.Total_Budget_Estimation)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.Total_Budget_Estimation)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.Total_Budget_Estimation)]].Value = x.Total_Budget_Estimation;
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

                        ws.Cells[r, d[nameof(x.PR_Posted_Date)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws.Cells[r, d[nameof(x.PR_Posted_Date)]].Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                        ws.Cells[r, d[nameof(x.PR_Posted_Date)]].Value = x.PR_Posted_Date;
                        ws.Cells[r, d[nameof(x.Delivery_Request_Date)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws.Cells[r, d[nameof(x.Delivery_Request_Date)]].Style.Numberformat.Format = "yyyy-MM-dd";
                        ws.Cells[r, d[nameof(x.Delivery_Request_Date)]].Value = x.Delivery_Request_Date;
                        ws.Cells[r, d[nameof(x.Final_Spec_Req_Date)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws.Cells[r, d[nameof(x.Final_Spec_Req_Date)]].Style.Numberformat.Format = "yyyy-MM-dd";
                        ws.Cells[r, d[nameof(x.Final_Spec_Req_Date)]].Value = x.Final_Spec_Req_Date;
                        ws.Cells[r, d[nameof(x.Generate_Proc_Sum_Date)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws.Cells[r, d[nameof(x.Generate_Proc_Sum_Date)]].Style.Numberformat.Format = "yyyy-MM-dd";
                        ws.Cells[r, d[nameof(x.Generate_Proc_Sum_Date)]].Value = x.Generate_Proc_Sum_Date;

                        ws.Cells[r, d[nameof(x.TAT_WD)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.TAT_WD)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.TAT_WD)]].Value = x.TAT_WD;
                        ws.Cells[r, d[nameof(x.SLA_WD)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.SLA_WD)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.SLA_WD)]].Value = x.SLA_WD;
                        ws.Cells[r, d[nameof(x.SLA_Status)]].Value = x.SLA_Status;
                        ws.Cells[r, d[nameof(x.Vendor)]].Value = x.Vendor;
                        ws.Cells[r, d[nameof(x.Selected)]].Value = x.Selected;

                        ws.Cells[r, d[nameof(x.Price_Per_Item)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.Price_Per_Item)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.Price_Per_Item)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.Price_Per_Item)]].Value = x.Price_Per_Item;
                        ws.Cells[r, d[nameof(x.Total_Price)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.Total_Price)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.Total_Price)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.Total_Price)]].Value = x.Total_Price;
                        ws.Cells[r, d[nameof(x.Price_Per_Item_Inc_Other_Cost)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.Price_Per_Item_Inc_Other_Cost)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.Price_Per_Item_Inc_Other_Cost)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.Price_Per_Item_Inc_Other_Cost)]].Value = x.Price_Per_Item_Inc_Other_Cost;
                        ws.Cells[r, d[nameof(x.Total_Price_Inc_Other_Cost)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.Total_Price_Inc_Other_Cost)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.Total_Price_Inc_Other_Cost)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.Total_Price_Inc_Other_Cost)]].Value = x.Total_Price_Inc_Other_Cost;
                        ws.Cells[r, d[nameof(x.Realised_Saving)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.Realised_Saving)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.Realised_Saving)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.Realised_Saving)]].Value = x.Realised_Saving;

                        ws.Cells[r, d[nameof(x.Order_Type)]].Value = x.Order_Type;
                        ws.Cells[r, d[nameof(x.Order_No)]].Value = x.Order_No;
                        ws.Cells[r, d[nameof(x.Order_Status)]].Value = x.Order_Status;

                        if ((x.Order_Status ?? "").Trim().ToLower() == "regenerate")
                        {
                            ws.Cells[r, 1, r, d.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[r, 1, r, d.Count].Style.Fill.BackgroundColor.SetColor(Color.Red);
                        }

                        ws.Cells[r, d[nameof(x.Order_Date)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws.Cells[r, d[nameof(x.Order_Date)]].Style.Numberformat.Format = "yyyy-MM-dd";
                        ws.Cells[r, d[nameof(x.Order_Date)]].Value = x.Order_Date;
                        ws.Cells[r, d[nameof(x.Order_Grand_Total_Amount)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.Order_Grand_Total_Amount)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.Order_Grand_Total_Amount)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.Order_Grand_Total_Amount)]].Value = x.Order_Grand_Total_Amount;
                        ws.Cells[r, d[nameof(x.Approver_Date)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws.Cells[r, d[nameof(x.Approver_Date)]].Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                        ws.Cells[r, d[nameof(x.Approver_Date)]].Value = x.Approver_Date;
                        ws.Cells[r, d[nameof(x.Approver_Name)]].Value = x.Approver_Name;

                        ws.Cells[r, d[nameof(x.DN_No)]].Value = x.DN_No;
                        ws.Cells[r, d[nameof(x.DN_Status)]].Value = x.DN_Status;
                        ws.Cells[r, d[nameof(x.DN_Date)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws.Cells[r, d[nameof(x.DN_Date)]].Style.Numberformat.Format = "yyyy-MM-dd";
                        ws.Cells[r, d[nameof(x.DN_Date)]].Value = x.DN_Date;
                        ws.Cells[r, d[nameof(x.DN_Qty)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.DN_Qty)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.DN_Qty)]].Value = x.DN_Qty;

                        ws.Cells[r, d[nameof(x.Invoice_No)]].Value = x.Invoice_No;
                        ws.Cells[r, d[nameof(x.Invoice_Status)]].Value = x.Invoice_Status;
                        ws.Cells[r, d[nameof(x.Invoice_Date)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws.Cells[r, d[nameof(x.Invoice_Date)]].Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                        ws.Cells[r, d[nameof(x.Invoice_Date)]].Value = x.Invoice_Date;
                        ws.Cells[r, d[nameof(x.Invoice_Amount)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.Invoice_Amount)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.Invoice_Amount)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.Invoice_Amount)]].Value = x.Invoice_Amount;
                        ws.Cells[r, d[nameof(x.PPn)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.PPn)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.PPn)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.PPn)]].Value = x.PPn;
                        ws.Cells[r, d[nameof(x.PPh_23)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.PPh_23)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.PPh_23)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.PPh_23)]].Value = x.PPh_23;
                        ws.Cells[r, d[nameof(x.PPh_42)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.PPh_42)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.PPh_42)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.PPh_42)]].Value = x.PPh_42;
                        ws.Cells[r, d[nameof(x.Invoice_After_Tax_Or_Grand_Total)]].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        ws.Cells[r, d[nameof(x.Invoice_After_Tax_Or_Grand_Total)]].Style.Indent = 1;
                        ws.Cells[r, d[nameof(x.Invoice_After_Tax_Or_Grand_Total)]].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[r, d[nameof(x.Invoice_After_Tax_Or_Grand_Total)]].Value = x.Invoice_After_Tax_Or_Grand_Total;
                        ws.Cells[r, d[nameof(x.Remarks)]].Value = x.Remarks;

                        ws.Cells[r, d[nameof(x.ReasonCancel)]].Value = x.ReasonCancel;
                    }
                    #endregion

                    await ep.SaveAsync();
                }

                stream.Position = 0;
                string contentType = AppSystem.ContentTypeExcel;
                string fName = wsName + " - Export".ToXlsxFileName();
                return File(stream, contentType, fName);
            }
            catch (Exception e)
            {
                response.Data = globalExceptions.StatusData(e);
                response.Code = StatusCodes.Status400BadRequest;
                response.Status = globalExceptions.StatusCode(response.Code);
                log.LogPagination(nameof(ProcurementXlsx), response.Data, request.Start, request.Length, LogType.Error);
                return BadRequest(response);
            }
        }


        #region Refactor for background Job
        [TypeFilter(typeof(LoggerApiAttribute))]
        [HttpPost("Report/Procurement/Xlsx/WIP")]
        public async Task<IActionResult> ProcurementXlsx_Wip([FromBody] Models.Report.Procurement.DataTables.Request_WIP request)
        {
            GetListResponse response = new();
            try
            {
                var stream = await _reportRepository.ExportProcurementWipToExcel(request);

                string contentType = AppSystem.ContentTypeExcel;
                string fName = "Report PO - Export".ToXlsxFileName();

                return File(stream, contentType, fName);
            }
            catch (Exception e)
            {
                response.Data = globalExceptions.StatusData(e);
                response.Code = StatusCodes.Status400BadRequest;
                response.Status = globalExceptions.StatusCode(response.Code);
                log.LogPagination(nameof(ProcurementXlsx_Wip), response.Data, request.Start, request.Length, LogType.Error);
                return BadRequest(response);
            }
        }
        #endregion

    }
}
