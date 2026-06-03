using GlobalExceptions = APS_Common.GlobalExceptions;
using APS_WEB_APP.Common;
using APS_WEB_APP.Contracts;
using APS_WEB_APP.Helper;
using APS_WEB_APP.Models;
using APS_WEB_APP.Models.Invoice;
using APS_WEB_APP.Models.Reimbursement;
using APS_WEB_APP.Models.Report.DueDiligence;
using APS_WEB_APP.Models.Request;
using APS_WEB_APP.Payloads.Request.Report;
using APS_WEB_APP.Repository.Report;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace APS_WEB_APP.Repository.Report
{

    public class ReportRepository : IReportRepository
    {
        private readonly AppSettings _appSettings;
        private readonly IHttpClientHelper _httpClientHelper;
        
        public ReportRepository(
            IOptions<AppSettings> appSettings,
            ICookies cookies,
            IHttpClientHelper httpClientHelper)
        {
            _appSettings = appSettings.Value;
            _httpClientHelper = httpClientHelper;
        }

        public async Task<CommonResponse> GetPaymentList(ReportPaymentListRequest param)
        {
            try
            {
                string route = $"report/payment/list";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string jsonRequest = JsonConvert.SerializeObject(param);
                CommonResponse cr = await _httpClientHelper.PostJsonAsync(url, jsonRequest);
                return cr;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }
        
        public async Task<CommonResponse> GetBudgetTransactions(ParamGetRequestList param)
        {
            try
            {
                string route = $"report/budget/list";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string jsonRequest = JsonConvert.SerializeObject(param);
                CommonResponse cr = await _httpClientHelper.PostJsonAsync(url, jsonRequest);
                return cr;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }
        
        public async Task<CommonResponse> GetVoucherList(ReportVoucherListRequest param)
        {
            try
            {
                string route = $"report/voucher/list";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                var jsonRequest = JsonConvert.SerializeObject(param);
                CommonResponse cr = await _httpClientHelper.PostJsonAsync(url, jsonRequest);
                return cr;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }

        public async Task<CommonResponse> GetPajakList(ReportPajakListRequest param)
        {
            try
            {
                string route = $"report/pajak/list";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                var jsonRequest = JsonConvert.SerializeObject(param);
                CommonResponse cr = await _httpClientHelper.PostJsonAsync(url, jsonRequest);
                return cr;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }

        public async Task<CommonResponse> GetCountNotUpdatedVoucher()
        {
            try
            {
                string route = $"report/voucher/countnotupdated";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                CommonResponse cr = await _httpClientHelper.GetAsync(url);
                return cr;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }

        public async Task<CommonResponse> GetCountPerYearVoucher()
        {
            try
            {
                string route = $"report/voucher/countperyear";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                CommonResponse cr = await _httpClientHelper.GetAsync(url);
                return cr;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }

        public async Task<CommonResponse> GetPOList(ReportPORequest param)
        {
            try
            {
                string route = $"report/po/list";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string jsonRequest = JsonConvert.SerializeObject(param);
                CommonResponse cr = await _httpClientHelper.PostJsonAsync(url, jsonRequest);
                return cr;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }
        
        public async Task<CommonResponse> GetReportPOShoppingCartList(ReportPORequest request)
        {
            try
            {
                string route = $"report/po/shoppingcart/list";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string jsonRequest = JsonConvert.SerializeObject(request);
                CommonResponse cr = await _httpClientHelper.PostJsonAsync(url, jsonRequest);
                return cr;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }

        public async Task<CommonResponse> GetReportPONonShoppingCartList(ReportPORequest request)
        {
            try
            {
                string route = $"report/po/nonshoppingcart/list";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string jsonRequest = JsonConvert.SerializeObject(request);
                CommonResponse cr = await _httpClientHelper.PostJsonAsync(url, jsonRequest);
                return cr;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }

        public async Task<CommonResponse> DueDiligenceReportJsonPost(DueDiligenceReportRequestModel request)
        {
            try
            {
                string route = "Report/DueDiligence/Json";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string jsonRequest = JsonConvert.SerializeObject(request);
                CommonResponse cr = await _httpClientHelper.PostJsonAsync(url, jsonRequest);
                return cr;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }

        public async Task<CommonResponse> ProcurementDataTablesData(Models.Report.Procurement.DataTables.Request request)
        {
            try
            {
                string route = "Report/Procurement/DataTables/Data";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string jsonRequest = JsonConvert.SerializeObject(request);
                CommonResponse cr = await _httpClientHelper.PostJsonAsync(url, jsonRequest);
                return cr;
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
            try
            {
                var route = APS_Common.Const.Routes.Report_Contract_Index_Model;
                var url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                var hrm = _httpClientHelper.HttpClient(url).PostAsJsonAsync(url, request).Result;
                var response = hrm.Content.ReadAsAsync<APS_Common.Models.Report.Contract.Index.Model.Response.Root>().Result;
                return response;
            }
            catch (Exception e)
            {
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.Message) + nl + e.Message);
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.StackTrace) + nl + e.StackTrace);
                var be = e.GetBaseException();
                throw new APS_Common.GlobalExceptions(be.Message, be);
            }
        }

        public APS_Common.Models.DataTables.Response<APS_Common.Models.Report.Contract.Index.DataTables.Row.Root> Report_Contract_Index_DataTables(APS_Common.Models.Report.Contract.Index.DataTables.Request.Root request)
        {
            var id = request.UUID;
            var c = GetType().Name;
            var m = MethodBase.GetCurrentMethod().Name;
            var nl = Environment.NewLine;
            try
            {
                var route = APS_Common.Const.Routes.Report_Contract_Index_DataTables;
                var url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                var hrm = _httpClientHelper.HttpClient(url).PostAsJsonAsync(url, request).Result;
                var response = hrm.Content.ReadAsAsync<APS_Common.Models.DataTables.Response<APS_Common.Models.Report.Contract.Index.DataTables.Row.Root>>().Result;
                return response;
            }
            catch (Exception e)
            {
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.Message) + nl + e.Message);
                APS_Common.BaseLogging.LogError(id, c, m, nameof(e.StackTrace) + nl + e.StackTrace);
                var be = e.GetBaseException();
                throw new APS_Common.GlobalExceptions(be.Message, be);
            }
        }

        public async Task<CommonResponse> ProcurementDataTablesData_WIP(Models.Report.Procurement.DataTables.Request_WIP request)
        {
            try
            {
                string route = "Report/Procurement/DataTables/Data/WIP";
                string url = $"{_appSettings.BaseURL.RestApiAPS}/{route}";
                string jsonRequest = JsonConvert.SerializeObject(request);
                CommonResponse cr = await _httpClientHelper.PostJsonAsync(url, jsonRequest);
                return cr;
            }
            catch (Exception e)
            {
                throw new GlobalExceptions(nameof(ReportRepository), e.InnerException);
            }
        }

    }

}
