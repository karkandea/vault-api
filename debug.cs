using APS_Common;
using APS_Common.Const;
using APS_Common.EmailNotification;
using APS_Common.Extensions;
using APS_Common.Utilities;
using APS_SharedServices.Helper;
using APS_SharedServices.Models.RequestModels;
using APS_SharedServices.Models.ResponseModels;
using APS_SharedServices.Repositories.Contracts;
using APS_SharedServices.Services.Contracts;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace APS_SharedServices.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly string currentClass = nameof(NotificationService).ToDescription(AppSystem.Class);
        private readonly IConfiguration _configuration;
        private readonly ISendEmailRepository _sendEmailRepository;
        private readonly ILogRepository _logRepository;
        private readonly IMasterRepository _subCategoryRepository;
        private readonly string SenderName = AppSystem.SystemAdministrator;
        private readonly List<string> typeRI;
        private readonly List<string> statusForSendEmailRequestor;
        private readonly string shareFolder;

        public NotificationService(
            IConfiguration configuration,
            ISendEmailRepository sendEmailRepository,
            ILogRepository logRepository,
            IMasterRepository subCategoryRepository
            )
        {
            _configuration = configuration;
            shareFolder = _configuration.GetValue<string>("UploadFile:ShareFolder")!;
            _sendEmailRepository = sendEmailRepository;
            _logRepository = logRepository;
            _subCategoryRepository = subCategoryRepository;
            typeRI = subCategoryRepository.GetSubCategories("ReimbursementType", string.Empty).Result.Select(e => e.SubCategoryName!.ToLower()).ToList();
            statusForSendEmailRequestor = new List<string> { "reject", "need revision", "takeout" };
        }

        #region Email notification
        public async Task<string> SendEmailHtml(SendEmailModel request)
        {
            string methodName = nameof(SendEmailHtml);
            var sendEmailSuccess = await _sendEmailRepository.SendEmail(request);
            string msg = string.Empty;
            if (sendEmailSuccess)
            {
                msg = $"Send Email Success to {request.ToEmail[0]}";
                _logRepository.InsertTempEmail(request.ToEmail[0], string.Empty, 1, request.Subject, string.Empty);

                BaseLogging.LogInfo(
                    currentClass,
                    methodName,
                    msg
                    );
            }
            else
            {
                msg = $"Send Email Failed to {request.ToEmail[0]}";
                _logRepository.InsertTempEmail(request.ToEmail[0], string.Empty, 0, request.Subject, string.Empty);

                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    msg
                    );
            }
            return msg;
        }

        /// <summary>
        /// Send Email Notification For Approval (for many receiver in parameter notification.ApprovalRequestGroupMember)
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        public async Task<string> SendEmailNotification(NotificationModel notification)
        {
            string methodName = nameof(SendEmailNotification);
            try
            {
                //Setup variables
                string table = string.Empty;
                string configUrl = $"{_configuration.GetValue<string>($"WebAppAPS")}";
                string reqTypeFolder = Regex.Replace(notification.RequestType!, @"\s+", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));
                string pathAttachment = $"{shareFolder}\\{reqTypeFolder}\\{notification.RequestId}";
                _logRepository.InsertTempEmail("after pathAttachment", "", 0, JsonConvert.SerializeObject(notification), pathAttachment);

                //Setup Body Email
                //typeRI khusus transaksi di table Reimbursement (type reimbursement)
                List<string> transactionTemplate = typeRI;
                transactionTemplate.Add("invoice travel");
                transactionTemplate.Add("ger");
                if (transactionTemplate.Contains(notification.RequestType!.ToLower()))
                {
                    StringBuilder tableRI = BodyEmail.BodyTableRI(notification.ParamReimbursement!);
                    var approvalNoBudgetId = await _subCategoryRepository.GetSubCategoryByCode("ApprovalNoBudget");
                    var approvalNoBudget = await _logRepository.GetChangesAccountMaster(notification.RequestNumber!);
                    AppendBodyForBudgetChanges(notification, tableRI, approvalNoBudgetId, approvalNoBudget);

                    table = tableRI.ToString();
                }
                else if (notification.RequestType.Equals("settlement", StringComparison.CurrentCultureIgnoreCase))
                {
                    StringBuilder tableSTL = BodyEmail.BodyTableSTL(notification.ParamSettlement!);
                    string tableSTLBalance = tableSTL.ToString();
                    StringBuilder tableSTLNotBalance = tableSTL.AppendLine($@"  <br>
                                                                                <span>The settlement amount not balance with request amount, here are the details:</span>
                                                                                <br>
                                                                                <br>
                                                                                <table style='width: 100%; border-spacing: 0px;'>
                                                                                    <tr style='height: 33px; width: 100%;'>
                                                                                        <th style='width: 30%; text-align: left; padding-left: 16px; background: #F3F7FF;'>Advance Amount</th>
                                                                                        <td style='width: 50%; text-align: left; padding-left: 16px; background: #C8DBFF;'>{notification.ParamSettlement!.AmountString}</td>
                                                                                        <td style='width: 20%; text-align: left; padding-left: 16px;'></td>
                                                                                    </tr>
                                                                                    <tr style='height: 33px; width: 100%;'>
                                                                                        <th style='width: 30%; text-align: left; padding-left: 16px; background: #F3F7FF;'>Settlement Amount</th>
                                                                                        <td style='width: 50%; text-align: left; padding-left: 16px; background: #C8DBFF;'>{notification.ParamSettlement.SettlementAmount}</td>
                                                                                        <td style='width: 20%; text-align: left; padding-left: 16px;'></td>
                                                                                    </tr>
                                                                                </table>
                                                                                <br>
                                                                                <span>New Request need to payment:</span>
                                                                                <table style='width: 100%; border-spacing: 0px;'>
                                                                                    <tr style='height: 33px; width: 100%;'>
                                                                                        <th style='width: 30%; text-align: left; padding-left: 16px; background: #F3F7FF;'>New Advance Number</th>
                                                                                        <td style='width: 50%; text-align: left; padding-left: 16px; background: #C8DBFF;'>{notification.ParamSettlement.NewRequestNumber}</td>
                                                                                        <td style='width: 20%; text-align: left; padding-left: 16px;'></td>
                                                                                    </tr>
                                                                                    <tr style='height: 33px; width: 100%;'>
                                                                                        <th style='width: 30%; text-align: left; padding-left: 16px; background: #F3F7FF;'>New Advance Amount</th>
                                                                                        <td style='width: 50%; text-align: left; padding-left: 16px; background: #C8DBFF;'>{notification.ParamSettlement.NewRequestAmount}</td>
                                                                                        <td style='width: 20%; text-align: left; padding-left: 16px;'></td>
                                                                                    </tr>
                                                                                </table>");

                    table = notification.ParamSettlement.IsBalance!.Value ? tableSTLBalance : tableSTLNotBalance.ToString();
                }
                else if (notification.RequestType.Equals("finance settlement", StringComparison.CurrentCultureIgnoreCase))
                {
                    StringBuilder tableFinSTL = BodyEmail.BodyTableFinSTL(notification.ParamSettlement!);
                    table = tableFinSTL.ToString();
                    pathAttachment = pathAttachment.Replace("FinanceSettlement", "Settlement");
                }
                else if (notification.RequestType.Equals("travel", StringComparison.CurrentCultureIgnoreCase))
                {
                    StringBuilder tableTR = BodyEmail.BodyTableTR(notification.ParamTravel!, notification.RequestType);
                    table = tableTR.ToString();
                }
                else if (notification.RequestType.Equals("travel settlement", StringComparison.CurrentCultureIgnoreCase))
                {
                    StringBuilder tableTR = BodyEmail.BodyTableTRSTL(notification.ParamTravelSettlement!, notification.RequestType);
                    table = tableTR.ToString();
                }
                else if (notification.RequestType.Equals("purchase request", StringComparison.CurrentCultureIgnoreCase) ||
                    notification.RequestType.Equals("purchase order", StringComparison.CurrentCultureIgnoreCase))
                {
                    StringBuilder itemTr = BodyEmail.BodyHtmlPurchaseRequestOrder(notification);
                    table = itemTr.ToString();
                }
                else if (notification.RequestType.StartsWith(AppSystem.ShopingCartRequest, StringComparison.CurrentCultureIgnoreCase))
                {
                    StringBuilder itemTr = BodyEmail.BodyHtmlPurchaseRequestOrderV2(notification);
                    table = itemTr.ToString();
                }
                else if (notification.RequestType.StartsWith("pick buyer", StringComparison.CurrentCultureIgnoreCase))
                {
                    StringBuilder itemTr = BodyEmail.BodyPickPRF(notification);
                    table = itemTr.ToString();
                }


                //Setup link sharefolder & link to apps
                string urlShareFolder = $@"Click <a href='{pathAttachment}'>here</a> to view the attachments <br>";
                //before attached url share folder in email, 
                //then url share folder replace with wording login to the apps to see attachment (request from user)
                urlShareFolder = urlShareFolder.Replace(urlShareFolder, "To see attachment please login to application. ");

                string urlApps = $@"Click <a href='{configUrl}'>here</a> to login the application";
                urlApps = notification.RequestId == 0 ? urlApps : string.Concat(urlShareFolder, urlApps);


                //Send email
                return await SendEmailNotificationForApproval(notification, table, urlApps);
            }
            catch (Exception ex)
            {
                ExceptionUtility exceptionData = new(ex);
                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    exceptionData.GetAll().ToDescription(AppSystem.ExceptionDetails)
                    );
                throw new GlobalExceptions(methodName, ex.InnerException);
            }
        }
        public async Task<string> SendEmailNotificationForApproval(NotificationModel notification, string table, string urlApps)
        {
            string methodName = nameof(SendEmailNotificationForApproval);
            try
            {
                string subHtmlApproval;
                string linkVpn = string.Empty, linkBoxer = string.Empty;
                SendEmailModel modelApproval = new();
                string msg = string.Empty;
                string htmlLinkApprove = string.Empty;
                foreach (var i in notification.ApprovalRequestGroupMember!.Where(e => e.ApprovalRequestEmail!.Count != 0))
                {
                    AppendBodyLinkVpnBoxer(out linkVpn, out linkBoxer, out string _, out string _, out string _, out string _, i);
                    htmlLinkApprove = $@"<div id='link-approve' style='margin-top: 14px; font-weight: 400; font-size: 14px; line-height: 25px;'>
                                                Or you can determine your action by clicking a button below:
                                                <br>
                                                With VPN
                                                <br>
                                                {linkVpn}
                                                <br>
                                                With Boxer
                                                <br>
                                                {linkBoxer}
                                            </div>";


                    var status = notification.StatusRequest;

                    if (notification.RequestType!.StartsWith(AppSystem.ShopingCartRequest, StringComparison.CurrentCultureIgnoreCase))
                    {
                        status = $@"{notification.RequestNumber} - {notification.StatusRequest}";
                    }

                    if (notification.StatusRequest!.Equals("cancel", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string message;
                        message = notification.StatusRequest.Equals("cancel", StringComparison.CurrentCultureIgnoreCase) ?
                            $"This request has been {notification.StatusRequest} by the requester" :
                            "The following requests are waiting for your approval, due to the unavailability of its previous Checker/Approver";

                        subHtmlApproval = string.Format(await File.ReadAllTextAsync("templates/subNotificationApprovalCancelRequest.html"),
                        i.UserName,
                        notification.RequestType,
                        status,
                        message,
                        table,
                        SenderName,
                        urlApps,
                        string.Empty
                        );
                    }
                    else
                    {
                        subHtmlApproval = string.Format(await File.ReadAllTextAsync("templates/subNotificationApproval.html"),
                        i.UserName,
                        notification.RequestType,
                        status,
                        table,
                        SenderName,
                        urlApps,
                        !notification.IsUsingBoxer ? string.Empty : htmlLinkApprove
                        );
                    }


                    var toEmail = new List<string>
                    {
                        i.Email!
                    };
                    modelApproval.Subject = notification.SubjectEmail;
                    modelApproval.ToEmail = toEmail;
                    modelApproval.CCEmails = notification.CcEmail;
                    modelApproval.Html = subHtmlApproval;
                    modelApproval.ReceiverType = "internal";
                    modelApproval.Attachments = notification.Attachments;
                    var sendEmailSuccess = await _sendEmailRepository.SendEmail(modelApproval);
                    if (sendEmailSuccess)
                    {
                        msg = $"Send Email Success to {i.UserName} - {i.Email}";
                        _logRepository.InsertTempEmail(i.Email!, i.UserName!, 1, notification.SubjectEmail!, notification.StatusRequest);

                        BaseLogging.LogInfo(
                            currentClass,
                            methodName,
                            msg
                            );
                    }
                    else
                    {
                        msg = $"Send Email Failed to {i.UserName} - {i.Email}";
                        _logRepository.InsertTempEmail(i.Email!, i.UserName!, 0, notification.SubjectEmail!, notification.StatusRequest);

                        BaseLogging.LogError(
                            AppSystem.Catch,
                            currentClass,
                            methodName,
                            methodName
                            );
                    }
                }
                return msg;
            }
            catch (Exception ex)
            {
                ExceptionUtility exceptionData = new(ex);
                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    exceptionData.GetAll().ToDescription(AppSystem.ExceptionDetails)
                    );
                throw new GlobalExceptions(methodName, ex.InnerException);
            }
        }

        /// <summary>
        /// Send Email Notification to Travel Agent
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        public async Task<string> SendEmailToTravelAgent(NotificationModel notification)
        {
            string methodName = nameof(SendEmailToTravelAgent);
            try
            {
                StringBuilder tableTR = BodyEmail.BodyTableTR(notification.ParamTravel!, notification.RequestType!);
                string table = tableTR.ToString();
                string subHtmlApproval = string.Format(await File.ReadAllTextAsync("templates/subNotificationTravelAgent.html"),
                    notification.ParamTravel!.TravelAgentName,
                    notification.RequestType,
                    notification.StatusRequest,
                    table,
                    SenderName,
                    string.Empty);

                SendEmailModel modelApproval = new()
                {
                    Subject = notification.SubjectEmail,
                    ToEmail = new List<string>() { notification.ParamTravel.TravelAgentEmail! },
                    CCEmails = notification.CcEmail,
                    Html = subHtmlApproval,
                    ReceiverType = "internal",
                    Attachments = notification.Attachments
                };
                var sendEmailSuccess = await _sendEmailRepository.SendEmail(modelApproval);
                string msg = string.Empty;
                if (sendEmailSuccess)
                {
                    msg = $"Send Email Success to {notification.ParamTravel.TravelAgentName} - {notification.ParamTravel.TravelAgentEmail}";
                    _logRepository.InsertTempEmail(notification.ParamTravel.TravelAgentEmail!, notification.ParamTravel.TravelAgentName!, 1, notification.SubjectEmail!, notification.StatusRequest!);

                    BaseLogging.LogInfo(
                        currentClass,
                        methodName,
                        msg
                        );
                }
                else
                {
                    msg = $"Send Email Failed to {notification.ParamTravel.TravelAgentName} - {notification.ParamTravel.TravelAgentEmail}";
                    _logRepository.InsertTempEmail(notification.ParamTravel.TravelAgentEmail!, notification.ParamTravel.TravelAgentName!, 0, notification.SubjectEmail!, notification.StatusRequest!);

                    BaseLogging.LogError(
                        currentClass,
                        methodName,
                        msg
                        );
                }
                return msg;
            }
            catch (Exception ex)
            {
                foreach (var item in notification.ApprovalRequestGroupMember!)
                {
                    _logRepository.InsertTempEmail(item.Email!, item.UserName!, 0, notification.SubjectEmail!, ex.Message);
                }

                ExceptionUtility exceptionData = new(ex);
                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    exceptionData.GetAll().ToDescription(AppSystem.ExceptionDetails)
                    );

                throw new GlobalExceptions(methodName, ex.InnerException);
            }
        }


        /// <summary>
        /// Send Email Notification For Requestor (just for 1 receiver)
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        public async Task<string> SendEmailNotificationForRequester(NotificationModel notification)
        {
            string methodName = nameof(SendEmailNotificationForRequester);
            try
            {
                //Setup variables
                string subHtmlApproval, table = string.Empty;
                string configUrl = $"{_configuration.GetValue<string>($"WebAppAPS")}";
                var reqType = notification.RequestType!.Equals("scheduler settlement", StringComparison.CurrentCultureIgnoreCase) ? "Cash Advance" : notification.RequestType;
                string reqTypeFolder = Regex.Replace(reqType, @"\s+", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));
                string pathAttachment = $"{shareFolder}\\{reqTypeFolder}\\{notification.RequestId}";
                SendEmailModel modelApproval = new SendEmailModel();

                //Setup Body Email (Finance)
                BodyEmailForRequester(notification, ref table, ref pathAttachment);

                //Setup Body Email (Procurement)
                string emailbody = EmailTemplate.TEMPLATE_DELIVERY_NOTE;
                emailbody = emailbody.Replace("{content_body}", table);
                emailbody = emailbody.Replace("{Regars}", notification.RequestorName);

                //Setup URL Attachment via link to sharefolder
                string urlShareFolder = $@"Click <a href='{pathAttachment}'>here</a> to view the attachments <br>";
                //before attached url share folder in email, 
                //then url share folder replace with wording login to the apps to see attachment (request from user)
                urlShareFolder = urlShareFolder.Replace(urlShareFolder, "To see attachment please login to application. ");

                //Setup link to apps
                string urlApps = $@"Click <a href='{configUrl}'>here</a> to login the application";
                urlApps = notification.RequestId == 0 ? urlApps : string.Concat(urlShareFolder, urlApps);

                var status = notification.StatusRequest;

                if (notification.RequestType.StartsWith(AppSystem.ShopingCartRequest, StringComparison.CurrentCultureIgnoreCase))
                {
                    status = $@"{notification.RequestNumber} - {notification.StatusRequest}";
                }

                subHtmlApproval = string.Format(await File.ReadAllTextAsync("templates/subNotificationRequestor.html"),
                         notification.RequestorName,
                         notification.RequestType,
                         status,
                         table,
                         SenderName,
                         urlApps
                    );

                //Setup recipient email
                var toEmail = new List<string>();
                if (reqType.StartsWith("travel", StringComparison.CurrentCultureIgnoreCase))
                    toEmail.Add(notification.CreatorEmail!);
                else
                    toEmail.Add(notification.RequestorEmail!);

                //Other conditions
                #region Send Email to email service with some condition
                string msg = string.Empty;
                if (notification.RequestType.Equals(AppSystem.DeliveryNoteRequest, StringComparison.CurrentCultureIgnoreCase) ||
                    notification.RequestType.Equals("invoice management", StringComparison.CurrentCultureIgnoreCase))
                {
                    modelApproval.Subject = notification.SubjectEmail;
                    modelApproval.ToEmail = toEmail;
                    modelApproval.CCEmails = notification.CcEmail;
                    modelApproval.Html = emailbody;
                    modelApproval.ReceiverType = "internal";
                    modelApproval.Attachments = notification.Attachments;
                    var sendEmailSuccess = await _sendEmailRepository.SendEmail(modelApproval);
                    LogSendEmail(notification, methodName, sendEmailSuccess, ref msg);
                }
                else if (notification.RequestType.Equals("repair to dn", StringComparison.CurrentCultureIgnoreCase) ||
                    notification.RequestType.Equals("repair to invoice", StringComparison.CurrentCultureIgnoreCase) ||
                    notification.RequestType.Equals("generate invoice", StringComparison.CurrentCultureIgnoreCase))
                {
                    modelApproval.Subject = notification.SubjectEmail;
                    modelApproval.ToEmail = toEmail;
                    modelApproval.Html = emailbody;
                    modelApproval.ReceiverType = "internal";
                    modelApproval.Attachments = notification.Attachments;
                    modelApproval.CCEmails = notification.CcEmail;
                    var sendEmailSuccess = await _sendEmailRepository.SendEmail(modelApproval);
                    LogSendEmail(notification, methodName, sendEmailSuccess, ref msg);
                }
                else
                {
                    modelApproval.Subject = notification.SubjectEmail;
                    modelApproval.ToEmail = toEmail;
                    modelApproval.Html = subHtmlApproval;
                    modelApproval.ReceiverType = "internal";
                    modelApproval.Attachments = notification.Attachments;
                    modelApproval.CCEmails = notification.CcEmail;
                    var sendEmailSuccess = await _sendEmailRepository.SendEmail(modelApproval);
                    LogSendEmail(notification, methodName, sendEmailSuccess, ref msg);
                }
                return msg;
                #endregion
            }
            catch (Exception ex)
            {
                _logRepository.InsertTempEmail(notification.RequestorEmail!, notification.RequestorName!, 0, notification.SubjectEmail!, ex.Message);

                ExceptionUtility exceptionData = new(ex);
                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    exceptionData.GetAll().ToDescription(AppSystem.ExceptionDetails)
                    );
                throw new GlobalExceptions(methodName, ex.InnerException);
            }
        }
        private void BodyEmailForRequester(NotificationModel notification, ref string table, ref string pathAttachment)
        {
            //typeRI khusus transaksi di table Reimbursement (type reimbursement)
            if (typeRI.Contains(notification.RequestType!.ToLower()) 
                || notification.RequestType.Equals("invoice travel", StringComparison.CurrentCultureIgnoreCase)
                || notification.RequestType.Equals("ger", StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder tableRI;
                if (notification.StatusRequest!.Equals(AppSystem.MessageApprovedAndSendToFinance, StringComparison.CurrentCultureIgnoreCase))
                {
                    tableRI = BodyEmail.BodyTableRI(notification.ParamReimbursement!);
                }
                else
                {
                    tableRI = BodyEmail.BodyTableRIForRequestor(notification.ParamReimbursement!, notification.StatusRequest!, notification.ActionBy!);
                }
                table = tableRI.ToString();
            }
            else if (notification.RequestType.Equals("travel", StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder tableRI = BodyEmail.BodyTableTRForRequestor(notification.ParamTravel!, notification.RequestType, notification.ActionBy!);
                table = tableRI.ToString();
            }
            else if (notification.RequestType.Equals("travel settlement", StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder tableRI = BodyEmail.BodyTableTRSTLForRequestor(notification.ParamTravelSettlement!, notification.RequestType, notification.ActionBy!);
                table = tableRI.ToString();
            }
            else if (notification.RequestType.Equals("settlement", StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder tableSTL = BodyEmail.BodyTableSTLForRequestor(notification.ParamSettlement!, notification.StatusRequest!, notification.ActionBy!);
                table = tableSTL.ToString();
            }
            else if (notification.RequestType.Equals("finance settlement", StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder tableSTL = BodyEmail.BodyTableSTLForRequestor(notification.ParamSettlement!, notification.StatusRequest!, notification.ActionBy!);
                table = tableSTL.ToString();
                pathAttachment = pathAttachment.Replace("FinanceSettlement", "Settlement");
            }
            else if (notification.RequestType.Equals("scheduler settlement", StringComparison.CurrentCultureIgnoreCase))
            {
                notification.RequestType = "Settlement";
                StringBuilder tableSTL = BodyEmail.BodyHtmlRemainSettlement(notification.ParamSettlement!);
                table = tableSTL.ToString();
            }
            else if (notification.RequestType.StartsWith("purchase", StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder tablePR = BodyEmail.BodyHtmlPurchaseRequestOrder(notification);
                table = tablePR.ToString();
            }
            else if (notification.RequestType.Equals(AppSystem.DeliveryNoteRequest, StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder tableDN = BodyEmail.BodyHtmlDeliveryNote(notification);
                table = tableDN.ToString();
            }
            else if (notification.RequestType.Equals("invoice management", StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder tableInvoice = BodyEmail.BodyHtmlInvoiceManagement(notification);
                table = tableInvoice.ToString();
            }
            else if (notification.RequestType.Equals("generate invoice", StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder tableInvoice = BodyEmail.BodyHtmlGenerateInvoice(notification);
                table = tableInvoice.ToString();
            }
            else if (notification.RequestType.Equals("repair to dn", StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder tableDN = BodyEmail.BodyHtmlRepairDN(notification);
                table = tableDN.ToString();
            }
            else if (notification.RequestType.Equals("repair to invoice", StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder tableInvoice = BodyEmail.BodyHtmlRepairInvoice(notification);
                table = tableInvoice.ToString();
            }
            else if (notification.RequestType.StartsWith(AppSystem.ShopingCartRequest, StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder itemTr = BodyEmail.BodyHtmlPurchaseRequestOrderV2(notification);
                table = itemTr.ToString();
            }
            else if (notification.RequestType.StartsWith("pick buyer", StringComparison.CurrentCultureIgnoreCase))
            {
                StringBuilder itemBuyer = BodyEmail.BodyPickPRF(notification);
                table = itemBuyer.ToString();
            }
        }
        private static void AppendBodyForBudgetChanges(NotificationModel notification, StringBuilder tableRI, SubCategoryResponse approvalNoBudgetId, BeforeAfterAccountMasterResponse approvalNoBudget)
        {
            foreach (var _ in from x in notification.ApprovalRequestGroupMember
                              where x.ApprovalGroup_SubCategoryId == approvalNoBudgetId.ID && approvalNoBudget != null
                              select new { })
            {
                tableRI.AppendLine($@"  <br>
                                                    <span>Changes Account Master (COA):</span>
                                                    <br>
                                                    <br>
                                                    <table style='width: 80%;'>
                                                        <tr style='height: 33px; width: 100%; background: #F3F7FF;'>
                                                            <th style='width: 200px;'>Account Master Before</th>
                                                            <th style='width: 200px;'>Account Master After</th>
                                                            <th style='width: 150px;'>Cost Center</th>
                                                            <th style='width: 150px;'>Amount</th>
                                                            <th style='width: 50px;'>Currency</th>
                                                        </tr>
                                                        <tr style='height: 33px; width: 100%; text-align: center; background: #C8DBFF;'>
                                                            <td>{approvalNoBudget.AccountMasterCodeBefore}</td>
                                                            <td>{approvalNoBudget.AccountMasterCodeAfter}</td>
                                                            <td>{approvalNoBudget.CostCenter}</td>
                                                            <td>{string.Format("{0:C}", approvalNoBudget.Amount).Replace("$", "Rp. ")}</td>
                                                            <td>{approvalNoBudget.Currency}</td>
                                                        </tr>
                                                    </table>");
            }
        }

        private static void AppendBodyLinkVpnBoxer(out string linkVpn, out string linkBoxer, out string vpnApprove, out string vpnReject, out string boxerApprove, out string boxerReject, ResponseApprovalRequestGroupMember i)
        {
            vpnApprove = i.ApprovalRequestEmail?.Where(e => e.AccountId == i.AccountId && e.LinkType == 0 && e.Action == "Approved").Select(e => e.URLAction).FirstOrDefault()!;
            vpnReject = i.ApprovalRequestEmail?.Where(e => e.AccountId == i.AccountId && e.LinkType == 0 && e.Action == "Rejected").Select(e => e.URLAction).FirstOrDefault()!;
            boxerApprove = i.ApprovalRequestEmail?.Where(e => e.AccountId == i.AccountId && e.LinkType == 1 && e.Action == "Approved").Select(e => e.URLAction).FirstOrDefault()!;
            boxerReject = i.ApprovalRequestEmail?.Where(e => e.AccountId == i.AccountId && e.LinkType == 1 && e.Action == "Rejected").Select(e => e.URLAction).FirstOrDefault()!;

            linkVpn = $@"<table width='100px' cellspacing='0' cellpadding='0'>
                                    <tr>
                                        <td>
                                            <table cellspacing='0' cellpadding='0'>
                                                <tr>
                                                    <td style='border-radius: 5px; width: 60px; text-align: center;' bgcolor = '#009900'>
                                                        <a href='{vpnApprove}' style='width: 60px; padding: 8px 12px; border: 1px solid #009900;border-radius: 5px;font-family: Helvetica, Arial, sans-serif;font-size: 14px;color: #ffffff;text-decoration: none;font-weight:bold;display: inline-block;'>
                                                            Approve
                                                        </a>    
                                                    </td>
                                                    <td width: '60px'>
                                                        &nbsp; &nbsp; 
                                                    </td>
                                                    <td style='border-radius: 5px; width: 60px; text-align: center;' bgcolor = '#ED2939'>
                                                        <a href='{vpnReject}' style='width: 60px; padding: 8px 12px; border: 1px solid #ED2939;border-radius: 5px;font-family: Helvetica, Arial, sans-serif;font-size: 14px;color: #ffffff;text-decoration: none;font-weight:bold;display: inline-block;'>
                                                            Reject
                                                        </a>
                                                    </td>    
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                </table>";

            linkBoxer = $@" <table width='100px' cellspacing='0' cellpadding='0'>
                                        <tr>
                                            <td>
                                                <table cellspacing='0' cellpadding='0'>
                                                    <tr>
                                                        <td style='border-radius: 5px; width: 60px; text-align: center;' bgcolor = '#009900'>
                                                            <a href='{boxerApprove}' style='width: 60px; padding: 8px 12px; border: 1px solid #009900;border-radius: 5px;font-family: Helvetica, Arial, sans-serif;font-size: 14px;color: #ffffff;text-decoration: none;font-weight:bold;display: inline-block;'>
                                                                Approve
                                                            </a>    
                                                        </td>
                                                        <td width: '60px'>
                                                            &nbsp; &nbsp; 
                                                        </td>
                                                        <td style='border-radius: 5px; width: 60px; text-align: center;' bgcolor = '#ED2939'>
                                                            <a href='{boxerReject}' style='width: 60px; padding: 8px 12px; border: 1px solid #ED2939;border-radius: 5px;font-family: Helvetica, Arial, sans-serif;font-size: 14px;color: #ffffff;text-decoration: none;font-weight:bold;display: inline-block;'>
                                                                Reject
                                                            </a>
                                                        </td>    
                                                    </tr>
                                                </table>
                                            </td>
                                        </tr>
                                    </table>";
        }

        private void LogSendEmail(NotificationModel notification, string methodName, bool sendEmailSuccess, ref string msg)
        {
            if (sendEmailSuccess)
            {
                msg = $"Send Email {notification.RequestType} Success to {notification.RequestorName} - {notification.RequestorEmail}";
                _logRepository.InsertTempEmail(notification.RequestorEmail!, notification.RequestorName!, 1, notification.SubjectEmail!, $"Email Success {notification.RequestType} - {notification.StatusRequest}");
                BaseLogging.LogInfo(
                    currentClass,
                    methodName,
                    msg
                    );
            }
            else
            {
                msg = $"Send Email {notification.RequestType} Failed to {notification.RequestorName} - {notification.RequestorEmail}";
                _logRepository.InsertTempEmail(notification.RequestorEmail!, notification.RequestorName!, 0, notification.SubjectEmail!, $"Email Failed {notification.RequestType} - {notification.StatusRequest}");
                BaseLogging.LogError(
                    currentClass,
                    methodName,
                    msg
                    );
            }
        }

        #region Procurement
        public async Task<string> SendEmailPOForVendor(NotificationModel notification)
        {
            string methodName = nameof(SendEmailPOForVendor);
            try
            {
                SendEmailModel modelApproval = new SendEmailModel();

                string bodyEmailHtml = string.Format(await File.ReadAllTextAsync("templates/requestPOToVendor.html"),
                         notification.ParamPOToVendor!.VendorName,
                         notification.ParamPOToVendor.PONumber,
                         notification.ParamPOToVendor.RequestorName,
                         notification.ParamPOToVendor.RequestDeliveryDate
                    );

                var toEmail = new List<string>();
                toEmail.Add(notification.ParamPOToVendor.VendorEmail!);

                List<string> CCEmail = new List<string>();
                CCEmail.Add("procurement_amfs@axa-mandiri.co.id");
                CCEmail.Add(notification.ParamPOToVendor.RequestorEmail!);

                modelApproval.Subject = notification.SubjectEmail;
                modelApproval.ToEmail = toEmail;
                modelApproval.Html = bodyEmailHtml;
                modelApproval.CCEmails = CCEmail;
                modelApproval.ReceiverType = "external";
                modelApproval.Attachment4 = notification.Attachment4;
                modelApproval.Attachments = notification.Attachments;
                var sendEmailSuccess = await _sendEmailRepository.SendEmail(modelApproval);
                string msg = string.Empty;
                if (sendEmailSuccess)
                {
                    msg = $"Send Email Success to {notification.ParamPOToVendor.VendorName} - {notification.ParamPOToVendor.VendorEmail}";
                    _logRepository.InsertTempEmail(notification.ParamPOToVendor.VendorEmail!, notification.ParamPOToVendor.VendorName!, 1, notification.SubjectEmail!, notification.StatusRequest!);

                    BaseLogging.LogInfo(
                        currentClass,
                        methodName,
                        msg
                        );
                }
                else
                {
                    msg = $"Send Email Failed to {notification.ParamPOToVendor.VendorName} - {notification.ParamPOToVendor.VendorEmail}";
                    _logRepository.InsertTempEmail(notification.ParamPOToVendor.VendorEmail!, notification.ParamPOToVendor.VendorName!, 0, notification.SubjectEmail!, notification.StatusRequest!);

                    BaseLogging.LogError(
                        currentClass,
                        methodName,
                        msg
                        );
                }
                return msg;
            }
            catch (Exception ex)
            {
                _logRepository.InsertTempEmail(notification.ParamPOToVendor!.VendorEmail!, notification.ParamPOToVendor.VendorName!, 0, notification.SubjectEmail!, ex.Message);

                ExceptionUtility exceptionData = new(ex);
                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    exceptionData.GetAll().ToDescription(AppSystem.ExceptionDetails)
                    );
                throw new GlobalExceptions(methodName, ex.InnerException);
            }
        }
        public async Task<string> SendEmailFromWeb(NotificationModel notification)
        {
            string methodName = nameof(SendEmailFromWeb);
            string table = string.Empty;
            SendEmailModel model = new SendEmailModel();
            try
            {
                if (notification.RequestType!.Equals(AppSystem.DeliveryNoteRequest, StringComparison.CurrentCultureIgnoreCase))
                {
                    StringBuilder itemTr = new StringBuilder();
                    foreach (ParamDeliveryNoteInCompleteItems obj in notification.ParamDeliveryNoteInComplete!.Body3!)
                    {
                        itemTr.Append($@"<tr style='height: 33px; background: #F3F7FF;'>
                                        <td>{obj.IncompleteDetail}</td>
                                    </tr>");
                    }

                    table = $@"
                           <table style='border:0px ;width:100%; height:100%;' bgcolor='#F0F0F0' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <td align='center' valign='top' style='background-color: #F0F0F0;'>
                                        <table cellpadding='0' cellspacing='0' class='container' style='width:100%;'>
                                            <tr>
                                                <td class='edit dropzone container-padding content' style='padding: 12px 24px; background-color: rgb(255, 255, 255); height: 70px; width: 600px; border-color: black; transform: translate(0px, 0px);' id='0' data-x='0' data-y='0' align='left'>
                                                    <div id='table-content' style='margin-top: 10px; font-weight: 400; font-size: 14px; line-height: 25px;'>
                                                        <div id='tittle-table-content' style='margin-top: 18px; font-size: 14px; line-height: 25px;'>
                                                            <span>{notification.ParamDeliveryNoteInComplete.Header}</span>
                                                            <br />
                                                            <span>
                                                                {notification.ParamDeliveryNoteInComplete.Body1}
                                                            </span>
                                                            <br />
                                                            <span>
                                                                {notification.ParamDeliveryNoteInComplete.Body2}
                                                            </span>
                                                        </div>
                                                        <div style='margin-top: 9px; font-weight: 400; font-size: 14px; line-height: 25px;'>
                                                            <table style='width: 100%; border-spacing: 0px;'>
                                                                <tbody>
                                                                    {itemTr}
                                                                </tbody>
                                                            </table>
                                                             <span>
                                                                {notification.ParamDeliveryNoteInComplete.Body4}
                                                            </span>
                                                        </div>
                                                    </div>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td class='container-padding footer-text' align='left' style='font-family:Helvetica, Arial, sans-serif;font-size:11px;line-height:11px;color:#aaaaaa;padding-left:24px;padding-right:24px; margin-bottom: 10px; background-color: rgb(255, 255, 255);'>
                                                    <div style='width: 100%;'>
                                                        <div class='edit dropzone' contenteditable='true' id='124' style='text-align: left; float: left; width: 100%; margin-top: 20px; margin-bottom: 10px;'>
                                                            <span>Regars</span>
                                                            <br />
                                                            <span>{notification.RequestorName}</span>
                                                            <br />
                                                            <br />
                                                            <span style='font-size: 10px;'>*Please do not reply tho this email at it has been sent form an unattended mailbox</span>
                                                        </div>
                                                        <div style='clear:both;'></div>
                                                    </div>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                            </table>";
                }

                var toEmail = new List<string>();

                toEmail.Add(notification.RequestorEmail!);
                model.CCEmails = notification.CcEmail;
                model.Subject = notification.SubjectEmail;
                model.ToEmail = toEmail;
                model.Html = table;
                model.ReceiverType = "internal";
                model.Attachments = notification.Attachments;

                var sendEmailSuccess = await _sendEmailRepository.SendEmail(model);
                string msg = string.Empty;
                if (sendEmailSuccess)
                {
                    msg = $"Send Email Success to {notification.RequestorName} - {notification.RequestorEmail}";
                    _logRepository.InsertTempEmail(notification.RequestorEmail!, notification.RequestorName!, 1, notification.SubjectEmail!, notification.StatusRequest!);

                    BaseLogging.LogInfo(
                        currentClass,
                        methodName,
                        msg
                        );
                }
                else
                {
                    msg = $"Send Email Failed to {notification.RequestorName} - {notification.RequestorEmail}";
                    _logRepository.InsertTempEmail(notification.RequestorEmail!, notification.RequestorName!, 0, notification.SubjectEmail!, notification.StatusRequest!);

                    BaseLogging.LogError(
                        currentClass,
                        methodName,
                        msg
                        );
                }
                return msg;
            }
            catch (Exception ex)
            {
                _logRepository.InsertTempEmail(notification.ParamPOToVendor!.VendorEmail!, notification.ParamPOToVendor.VendorName!, 0, notification.SubjectEmail!, ex.Message);

                ExceptionUtility exceptionData = new(ex);
                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    exceptionData.GetAll().ToDescription(AppSystem.ExceptionDetails)
                    );
                throw new GlobalExceptions(methodName, ex.InnerException);
            }
        }
        public async Task<string> SendEmailNotificationForScheduller(NotificationModel notification)
        {
            string methodName = nameof(SendEmailNotificationForScheduller);
            string table = string.Empty;
            SendEmailModel model = new SendEmailModel();
            try
            {
                if (notification.RequestType!.Equals(AppSystem.DeliveryNoteRequest, StringComparison.CurrentCultureIgnoreCase)) // For Blast Email Reminder DN
                {
                    foreach (ParamSchedullerDeliveryNote obj in notification.ParamSchedullerDeliveryNotes!)
                    {
                        table = $@"<table style='border:0px; width:100%; height:100%;' bgcolor='#F0F0F0' cellpadding='0' cellspacing='0'>
                                    <tr>
                                        <td  align='center' valign='top' style='background-color: #fff; color:#00007f; font-size: 25px;'>
                                            <span>
                                                Please  Confirm the Delivery Note
                                            </span>
                                            <br />
                                            <span>
                                                Delivery Note - Reminder : {obj.PoNumber}
                                            </span>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td align='center' valign='top' style='background-color: #F0F0F0;'>
                                            <table cellpadding='0' cellspacing='0' class='container' style='width:100%;'>
                                                <tr>
                                                    <td class='edit dropzone container-padding content' style='padding: 12px 24px; background-color: rgb(255, 255, 255); height: 70px; width: 600px; border-color: black; transform: translate(0px, 0px);' id='0' data-x='0' data-y='0' align='left'>
                                                        <div id='table-content' style='margin-top: 10px; font-weight: 400; font-size: 14px; line-height: 25px;'>
                                                            <div id='tittle-table-content' style='margin-top: 18px; font-size: 14px; line-height: 25px;'>
                                                                <span>Dear, {obj.RequestorName}</span>
                                                                <br />
                                                                <span>
                                                                    Berdasarkan {obj.PoNumber} sudah bisa di DN, silahkan konfirmasi DN-nya atas PO tersebut
                                                                </span>
                                                                <br />
                                                            </div>
                                                        </div>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                </table>";

                        string emailbody = EmailTemplate.TEMPLATE_DELIVERY_NOTE;
                        emailbody = emailbody.Replace("{content_body}", table);
                        emailbody = emailbody.Replace("{Regars}", obj.RequestorName);

                        var toEmail = new List<string>();
                        toEmail.Add(obj.Email!);

                        model.Subject = notification.SubjectEmail;
                        model.ToEmail = toEmail;
                        model.Html = emailbody;
                        model.ReceiverType = "internal";
                        model.Attachments = notification.Attachments;
                        await _sendEmailRepository.SendEmail(model);
                        _ = Task.Delay(5000);
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                _logRepository.InsertTempEmail(notification.ParamPOToVendor!.VendorEmail!, notification.ParamPOToVendor.VendorName!, 0, notification.SubjectEmail!, ex.Message);

                ExceptionUtility exceptionData = new(ex);
                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    exceptionData.GetAll().ToDescription(AppSystem.ExceptionDetails)
                    );
                throw new GlobalExceptions(methodName, ex.InnerException);
            }
        }
        #endregion

        /// <summary>
        /// Finance is not using approvalflow
        /// so ToEmail = RequestorEmail
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        public async Task<string> SendEmailNotificationFinance(NotificationModel notification)
        {
            string methodName = "SendEmailNotificationFinance";
            try
            {
                string subHtmlApproval, table = string.Empty;

                string configUrl = $"{_configuration.GetValue<string>($"WebAppAPS")}";
                string urlApps = $@"Click <a href='{configUrl}'>here</a> to login the application";

                table = $@" <table style='width: 100%; border-spacing: 0px;'>
                                            <tr style='height: 33px; width: 100%;'>
                                                <th style='width: 30%; text-align: left; padding-left: 16px; background: #F3F7FF;'>Voucher Number</th>
                                                <td style='width: 50%; text-align: left; padding-left: 16px; background: #C8DBFF;'>{notification.RequestNumber}</td>
                                                <td style='width: 20%; text-align: left; padding-left: 16px;'></td>
                                            </tr>
                                            <tr style='height: 33px; width: 100%;'>
                                                <th style='width: 30%; text-align: left; padding-left: 16px; background: #F3F7FF;'>Amount</th>
                                                <td style='width: 50%; text-align: left; padding-left: 16px; background: #C8DBFF;'>{notification.ParamEmailFinance!.Currency} {notification.ParamEmailFinance.Amount?.ToString("#,##0.00")}</td>
                                                <td style='width: 20%; text-align: left; padding-left: 16px;'></td>
                                            </tr>
                                            <tr style='height: 33px; width: 100%;'>
                                                <th style='width: 30%; text-align: left; padding-left: 16px; background: #F3F7FF;'>Bank Account</th>
                                                <td style='width: 50%; text-align: left; padding-left: 16px; background: #C8DBFF;'>{notification.ParamEmailFinance.BankAccount}</td>
                                                <td style='width: 20%; text-align: left; padding-left: 16px;'></td>
                                            </tr>
                                        </table>";


                string linkVpn = String.Empty, linkBoxer = String.Empty;
                SendEmailModel modelApproval = new SendEmailModel();

                // notification to approval
                subHtmlApproval = string.Format(await File.ReadAllTextAsync("templates/subNotificationApprovalFinance.html"),
                notification.RequestorName,
                notification.RequestType,
                notification.StatusRequest,
                table,
                SenderName,
                urlApps,
                linkVpn,
                linkBoxer
                );

                var toEmail = new List<string>();
                toEmail.Add(notification.RequestorEmail!);

                modelApproval.Subject = notification.SubjectEmail;
                modelApproval.ToEmail = toEmail;
                modelApproval.Html = subHtmlApproval;
                modelApproval.ReceiverType = "internal";
                modelApproval.Attachments = notification.Attachments;
                var sendEmailSuccess = await _sendEmailRepository.SendEmail(modelApproval);
                string msg = string.Empty;
                if (sendEmailSuccess)
                {
                    msg = $"Send Email Success {notification.RequestorName} - {notification.RequestorEmail}";
                    _logRepository.InsertTempEmail(notification.RequestorEmail!, notification.RequestorName!, 1, notification.SubjectEmail!, notification.StatusRequest!);

                    BaseLogging.LogInfo(
                        currentClass,
                        methodName,
                        msg
                        );
                }
                else
                {
                    msg = $"Send Email Failed {notification.RequestorName} - {notification.RequestorEmail}";
                    _logRepository.InsertTempEmail(notification.RequestorEmail!, notification.RequestorName!, 0, notification.SubjectEmail!, notification.StatusRequest!);

                    BaseLogging.LogError(
                        currentClass,
                        methodName,
                        msg
                        );
                }
                return msg;
            }
            catch (Exception ex)
            {

                _logRepository.InsertTempEmail(notification.RequestorEmail!, notification.RequestorName!, 0, notification.SubjectEmail!, ex.Message);

                ExceptionUtility exceptionData = new(ex);
                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    exceptionData.GetAll().ToDescription(AppSystem.ExceptionDetails)
                    );

                throw new GlobalExceptions(methodName, ex.InnerException);
            }
        }
        public void SendEmail(NotificationModel notification)
        {
            if (statusForSendEmailRequestor.Contains(notification.StatusRequest, StringComparer.CurrentCultureIgnoreCase))
            {
                _ = SendEmailNotificationForRequester(notification);
            }
            else
            {
                _ = SendEmailNotification(notification);
            }
        }
        #endregion

        #region NON SHOPPING CART
        /// <summary>
        /// Send Email Notification For Requestor (just for 1 receiver)
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        public async Task<string> SendEmailNotificationForNonShop(NotificationModel notification)
        {
            string methodName = "SendEmailNotificationForShop";
            try
            {
                //Setup variables
                SendEmailModel modelEmail = new SendEmailModel();
                string bodyHtml = string.Empty;
                string linkVpn = string.Empty, linkBoxer = string.Empty;

                ParamEmailHtmlNonShopCart paramEmailHTML = new ParamEmailHtmlNonShopCart();
                paramEmailHTML.Title = notification.RequestType;
                paramEmailHTML.Dear = notification.RequestorName;
                paramEmailHTML.Regards = SenderName;
                paramEmailHTML.LinkContent = $@"Click <a href='{_configuration.GetValue<string>($"WebAppAPS")}'>here</a> to login the application";

                //Setup Body Email
                BodyEmailForNonShop(ref notification, ref paramEmailHTML);

                //Setup Link Attacment
                //Handling null reqType from Create New Vendor
                LinkAttachmentForNonShop(notification, paramEmailHTML);

                if (notification.ApprovalRequestGroupMember != null)
                {
                    foreach (var item in notification.ApprovalRequestGroupMember)
                    {
                        if (item.ApprovalRequestEmail != null && item.ApprovalRequestEmail.Count != 0)
                        {
                            AppendBodyLinkVpnBoxer(out linkVpn, out linkBoxer, out string vpnApprove, out string vpnReject, out string boxerApprove, out string boxerReject, item);
                        }
                        string htmlLinkApprove = $@"<div id='link-approve' style='margin-top: 14px; font-weight: 400; font-size: 14px; line-height: 25px;'>
                                                    Or you can determine your action by clicking a button below:
                                                    <br>
                                                    With VPN
                                                    <br>
                                                    {linkVpn}
                                                    <br>
                                                    With Boxer
                                                    <br>
                                                    {linkBoxer}
                                                </div>";
                        paramEmailHTML.BodyContent = notification.StatusRequest!.Equals("cancel", StringComparison.CurrentCultureIgnoreCase) ||
                            item.ApprovalRequestEmail is null ||
                            !notification.IsUsingBoxer ? string.Empty : htmlLinkApprove;
                    }
                }

                bodyHtml = string.Format(await File.ReadAllTextAsync("templates/subNotificationApprovalCancelRequest.html"),
                paramEmailHTML.Dear,
                paramEmailHTML.Title,
                paramEmailHTML.SubTitle,
                paramEmailHTML.TitleContent,
                paramEmailHTML.TableContent,
                paramEmailHTML.Regards,
                paramEmailHTML.LinkContent,
                paramEmailHTML.BodyContent!
                );

                //Setup recipient email
                var toEmail = new List<string>()
                {
                    notification.RequestorEmail!
                };

                //Other conditions
                #region Send Email to email service with some condition
                string msg = string.Empty;
                modelEmail.Subject = notification.SubjectEmail;
                modelEmail.ToEmail = toEmail;
                modelEmail.Html = bodyHtml;
                modelEmail.ReceiverType = "internal";
                modelEmail.CCEmails = notification.CcEmail;
                var sendEmailSuccess = await _sendEmailRepository.SendEmail(modelEmail);

                LogSendEmail(notification, methodName, sendEmailSuccess, ref msg);

                return msg;
                #endregion
            }
            catch (Exception ex)
            {
                _logRepository.InsertTempEmail(notification.RequestorEmail!, notification.RequestorName!, 0, notification.SubjectEmail!, ex.Message);

                ExceptionUtility exceptionData = new(ex);
                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    exceptionData.GetAll().ToDescription(AppSystem.ExceptionDetails)
                    );
                throw new GlobalExceptions(methodName, ex.InnerException);
            }
        }

        private void LinkAttachmentForNonShop(NotificationModel notification, ParamEmailHtmlNonShopCart paramEmailHTML)
        {
            if (notification.RequestType != null)
            {
                var reqType = notification.RequestType.Equals("scheduler settlement", StringComparison.CurrentCultureIgnoreCase) ? "Cash Advance" : notification.RequestType;
                string reqTypeFolder = Regex.Replace(reqType, @"\s+", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));
                string pathAttachment = $"{shareFolder}\\{reqTypeFolder}\\{notification.RequestId}";
                string urlShareFolder = $@"Click <a href='{pathAttachment}'>here</a> to view the attachments <br>";
                urlShareFolder = urlShareFolder.Replace(urlShareFolder, "To see attachment please login to application. ");
                paramEmailHTML.LinkContent = notification.RequestId == 0 ? paramEmailHTML.LinkContent : string.Concat(urlShareFolder, paramEmailHTML.LinkContent);
            }
        }

        private void BodyEmailForNonShop(ref NotificationModel notification, ref ParamEmailHtmlNonShopCart paramEmailHTML)
        {
            StringBuilder item = new StringBuilder();
            paramEmailHTML.Title = $"Your {paramEmailHTML.Title}";
            List<string> CCNewVendor = new List<string>();

            switch (notification.SubjectEmail)
            {
                case "PRF Status Request":
                    item = BodyEmail.BodyHtmlNonShopPRF(notification);
                    PRFStatusRequest(ref notification, ref paramEmailHTML);
                    break;
                case "PRF Pending Approval Request":
                    item = BodyEmail.BodyHtmlNonShopPRF(notification);
                    PRFPendingApprovalRequest(ref notification, ref paramEmailHTML);
                    break;
                case "PRF Register New Vendor":
                    CCNewVendor = new List<string>()
                    {
                        "procurement_amfs@axa-mandiri.co.id"
                    };
                    notification.CcEmail = CCNewVendor;

                    //paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Request Register New Vendor"
                    //paramEmailHTML.TitleContent = $"The following requests are pending Create New Vendor."
                    //item = BodyEmail.BodyHtmlRegisterPRFNewVendor(notification)
                    paramEmailHTML.Title = "Request for New Vendor Registration";
                    paramEmailHTML.SubTitle = notification.ParamPOToVendor!.VendorName;
                    item = BodyEmail.BodyHtmlRegisterNewVendor(notification);
                    break;
                case "Register for New Vendor Registration":
                    CCNewVendor = new List<string>()
                    {
                        "procurement_amfs@axa-mandiri.co.id"
                    };
                    notification.CcEmail = CCNewVendor;

                    paramEmailHTML.SubTitle = $"Request Register New Vendor";
                    paramEmailHTML.TitleContent = $"";
                    item = BodyEmail.BodyHtmlRegisterNewVendor(notification);
                    break;
                case "PRF DD Request":
                    paramEmailHTML.SubTitle = $"{notification.RequestNumber}";
                    paramEmailHTML.TitleContent = $"Terlampir Due Diligence request atas {notification.RequestNumber} dari procurement buyer {notification.ActionBy}.";
                    item = BodyEmail.BodyHtmlPRFDDRequest(notification);
                    break;
                case "Pending Approval Request - Procurement Summary (ProcSum)":
                    paramEmailHTML.Title = $"Pending Approval: Procurement Summary <br/>";
                    paramEmailHTML.SubTitle = $"{notification.RequestNumber}";
                    paramEmailHTML.TitleContent = $"The following request are waiting for your approval.";
                    item = BodyEmail.BodyHtmlNonShopProcsum(notification);
                    break;
				case "Procsum Status Approval Request":
					ProcsumStatusApprovalRequest(ref notification, ref paramEmailHTML);
					item = BodyEmail.BodyHtmlNonShopProcsum(notification);
					break;
				case "PAP Feedback Approval Request":
					paramEmailHTML.Title = $"Your Purchase for Advance Payment (PAP) Feedback <br/>";
					paramEmailHTML.SubTitle = $"{notification?.ParamPAP?.PAPNo}, {notification?.ParamPAP?.PRFNo}";
					paramEmailHTML.TitleContent = $"Your document has been <b>{notification?.StatusRequest}</b>.";
                    if ((notification?.StatusRequest ?? "").Substring(0, 3).ToLower() == "rev") paramEmailHTML.TitleContent = $"Your document need to be <b>Revised</b>";
                    item = BodyEmail.BodyHtmlNonShopPAP(notification);
					break;
				case "Request for Approval - Purchase for Advance Payment (PAP)":
                    paramEmailHTML.Title = $"Request for Approval - Purchase for Advance Payment (PAP) <br/>";
                    paramEmailHTML.SubTitle = $"{notification?.ParamPAP?.PAPNo}";
					paramEmailHTML.TitleContent = $"The following request are waiting for your approval.";
					item = BodyEmail.BodyHtmlNonShopPAP(notification);
					break;
				case "Your Purchase for Advaced Payment (PAP) is Completed":
					paramEmailHTML.Title = $"Your Purchase for Advanced Payment (PAP) is Completed <br/>";
                    paramEmailHTML.SubTitle = $"{notification?.ParamClosePAP?.PAPNo}, {notification?.ParamClosePAP?.PRFNo}";
                    paramEmailHTML.TitleContent = $"Your Purchase for Advance Payment (<b>PAP</b>) has been <b>{notification?.StatusRequest}</b>. <br/> Please continue the process of creating a Reimbursement request in the FLIPS Non-Benefit module";
					item = BodyEmail.BodyHtmlPAPReleased(notification!);
					break;
				case "PO Status Approval":
                    paramEmailHTML.SubTitle = $"Your Your Request is Canceled";
                    paramEmailHTML.TitleContent = $"Your Non-Shopping Cart - {notification.RequestNumber} Has been successfully Canceled.";
                    item = BodyEmail.BodyHtmlProcsumReviceFromPSupport(notification);
                    break;
				case "Padi Below 10 Million":
                    paramEmailHTML.Title = $"{notification?.ParamPADIBelow?.PRFNo} Non-Shopping Cart <br/> PADI below 10 million";
                    paramEmailHTML.Title = $"Your Purchase via PaDi UMKM (Below 10 Million) is {notification?.StatusRequest} <br/>";
                    paramEmailHTML.SubTitle = $"{notification?.ParamPADIBelow?.PRFNo}";
                    paramEmailHTML.TitleContent = string.Empty;
                    item = BodyEmail.BodyHtmlPADIBelow(notification!);

					notification.SubjectEmail = $"PADI below 10 million {notification?.StatusRequest}";
					break;
				case "Padi Above 10 Million":
					paramEmailHTML.Title = $"{notification?.ParamPADIAbove?.PRFNo} Non-Shopping Cart <br/> PADI Above 10 million";
					paramEmailHTML.SubTitle = $"{notification?.StatusRequest}";
					paramEmailHTML.TitleContent = $"Your document PADI has been {notification?.StatusRequest}. <br/> Please continue the process of creating Reimbursement in the Non-Benefit module";
					item = BodyEmail.BodyHtmlPADIAbove(notification!);

					notification.SubjectEmail = $"PADI Above 10 million {notification?.StatusRequest}";
					break;
				case "Your Guarantee Letter Feedback":
                    paramEmailHTML.Title = $"Your Guarantee Letter Feedback <br/>";
                    paramEmailHTML.SubTitle = $"{notification?.ParamGL?.GLNumber ?? ""}, {notification?.ParamGL?.PRFNo ?? ""}";
					paramEmailHTML.TitleContent = $"Your Guarantee Letter (GL) has been <b>{notification?.StatusRequest}</b>";
                    if ((notification?.StatusRequest ?? "").Substring(0,3).ToLower() == "rev") paramEmailHTML.TitleContent = $"Your document need to be <b>Revised</b>";
                    item = BodyEmail.BodyHtmlGLFeedback(notification!);
					break;
				case "Guarantee Letter Pending Approval":
                    paramEmailHTML.Title = $"Request for Approval - Guarantee Letter (GL) <br/>";
                    paramEmailHTML.SubTitle = $"{notification?.ParamGL?.GLNumber ?? ""}, {notification?.ParamGL?.PRFNo ?? ""}";
                    paramEmailHTML.TitleContent = $"The following request are waiting for your approval.";
					item = BodyEmail.BodyHtmlGL(notification);
					break;
					
				default:
                    break;
            }

            paramEmailHTML.TableContent = item.ToString();
        }

        public static void PRFStatusRequest(ref NotificationModel notification, ref ParamEmailHtmlNonShopCart paramEmailHTML)
        {
            if (notification.StatusRequest == "Process")
            {
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Pending Approval";
                paramEmailHTML.TitleContent = $"Your non-shopping cart request has been Processed";
            }
            else if (notification.StatusRequest == "Approve")
            {
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Approved";
                paramEmailHTML.TitleContent = $"Your non-shopping cart request has been Approved";
            }
            else if (notification.StatusRequest == "Reject")
            {
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Rejected";
                paramEmailHTML.TitleContent = $"Your non-shopping cart request has been Rejected";
            }
            else if (notification.StatusRequest == "Revision")
            {
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - {notification.StatusRequest}";
                paramEmailHTML.TitleContent = $"Your non-shopping cart request needs to be Revised";
            }
            else if (notification.StatusRequest == "Switch")
            {
                paramEmailHTML.Title = $"{paramEmailHTML.Title} Switch Approval";
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Pending Approval";
                paramEmailHTML.TitleContent = $"Your non-shopping cart request needs to be Switch Approval";
            }
            else if (notification.StatusRequest == "Cancel")
            {
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Canceled";
                paramEmailHTML.TitleContent = $"{notification.RequestNumber} Has been successfully Canceled";
            }
        }
        public static void PRFPendingApprovalRequest(ref NotificationModel notification, ref ParamEmailHtmlNonShopCart paramEmailHTML)
        {
            if (notification.StatusRequest == "Process")
            {
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Pending Approval";
                paramEmailHTML.TitleContent = $"The following PR Non-Shopping Cart are waiting for your approval.";
            }
            else if (notification.StatusRequest == "Approve")
            {
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Pending Approval";
                paramEmailHTML.TitleContent = $"The following request are waiting for your approval.";
            }
            else if (notification.StatusRequest == "Switch")
            {
                paramEmailHTML.Title = $"{paramEmailHTML.Title} Switch Approval";
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Pending Approval";
                paramEmailHTML.TitleContent = $"{notification.RequestNumber} has been Switch Approval";
            }
        }
        public static void ProcsumStatusApprovalRequest(ref NotificationModel notification, ref ParamEmailHtmlNonShopCart paramEmailHTML)
        {
            if (notification.StatusRequest == "Approve")
            {
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Approved";
                paramEmailHTML.TitleContent = $"Your non-shopping cart - Procurement Summary is Approved.";
            }
            else if (notification.StatusRequest == "Reject")
            {
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Pending Rejected";
                paramEmailHTML.TitleContent = $"The following request are waiting for your Rejected.";
            }
            else if (notification.StatusRequest == "Revice")
            {
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Pending Revices";
                paramEmailHTML.TitleContent = $"The following request are waiting for your Revices.";
            }
            else
            {
                paramEmailHTML.SubTitle = $"{notification.RequestNumber} - Pending {notification.StatusRequest}";
                paramEmailHTML.TitleContent = $"The following request are waiting for your {notification.StatusRequest}.";
            }
        }

        #endregion

        #region Procurment Buyer
        public async Task<string> SendEmailNotificationForBuyer(NotificationModel notification)
        {
            string methodName = "SendEmailNotificationForBuyer";
            try
            {
                //Setup variables
                string subHtmlApproval, table = string.Empty;
                string configUrl = $"{_configuration.GetValue<string>($"WebAppAPS")}";
                var reqType = notification.RequestType!.Equals("scheduler settlement", StringComparison.CurrentCultureIgnoreCase) ? "Cash Advance" : notification.RequestType;
                string reqTypeFolder = Regex.Replace(reqType, @"\s+", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));
                string pathAttachment = $"{shareFolder}\\{reqTypeFolder}\\{notification.RequestId}";
                SendEmailModel modelApproval = new SendEmailModel();

                //Setup Body Email (Finance)
                BodyEmail.BodyPickPRF(notification);


                var status = notification.StatusRequest;


                subHtmlApproval = string.Format(await File.ReadAllTextAsync("templates/subNotificationBuyer.html"),
                         notification.RequestNumber,
                         notification.ActionBy,
                         notification.RequestorName,
                         BodyEmail.BodyPickPRF(notification),
                         SenderName
                    );

                //Setup recipient email
                var toEmail = new List<string>();
                toEmail.Add(notification.RequestorEmail!);

                //Other conditions
                #region Send Email to email service with some condition
                string msg = string.Empty;

                modelApproval.Subject = notification.SubjectEmail;
                modelApproval.ToEmail = toEmail;
                modelApproval.Html = subHtmlApproval;
                modelApproval.ReceiverType = "internal";
                modelApproval.Attachments = notification.Attachments;
                modelApproval.CCEmails = notification.CcEmail;
                var sendEmailSuccess = await _sendEmailRepository.SendEmail(modelApproval);
                LogSendEmail(notification, methodName, sendEmailSuccess, ref msg);

                return msg;
                #endregion
            }
            catch (Exception ex)
            {
                _logRepository.InsertTempEmail(notification.RequestorEmail!, notification.RequestorName!, 0, notification.SubjectEmail!, ex.Message);
                ExceptionUtility exceptionData = new(ex);
                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    exceptionData.GetAll().ToDescription(AppSystem.ExceptionDetails)
                    );
                throw new GlobalExceptions(methodName, ex.InnerException);
            }
        }

        public async Task<string> SendEmailNotificationForProcsumEnhanceDocument(NotificationModel notification)
        {
            string methodName = nameof(SendEmailNotificationForProcsumEnhanceDocument);
            try
            {
                //Setup variables
                string subHtmlApproval, table = string.Empty;
                //string configUrl = $"{_configuration.GetValue<string>($"WebAppAPS")}";
                var reqType = notification.RequestType!.Equals("scheduler settlement", StringComparison.CurrentCultureIgnoreCase) ? "Cash Advance" : notification.RequestType;
                string reqTypeFolder = Regex.Replace(reqType, @"\s+", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));
                //string pathAttachment = $"{shareFolder}\\{reqTypeFolder}\\{notification.RequestId}";
                SendEmailModel modelApproval = new SendEmailModel();

                //Setup Body Email (Finance)
                var status = notification.StatusRequest;


                subHtmlApproval = string.Format(await File.ReadAllTextAsync("templates/subNotificationProcsumEnhanceDocument.html"),
                         notification.RequestNumber,
                         notification.RequestorName,
                         SenderName,
                         notification?.ParamEnhanceDocument?.RiskAssesment
                    );

                //Setup recipient email
                var toEmail = new List<string>();
                toEmail.Add(notification.RequestorEmail!);

                //Other conditions
                #region Send Email to email service with some condition
                string msg = string.Empty;

                modelApproval.Subject = notification.SubjectEmail;
                modelApproval.ToEmail = toEmail;
                modelApproval.Html = subHtmlApproval;
                modelApproval.ReceiverType = "internal";
                //modelApproval.Attachments = notification.Attachments;
                modelApproval.CCEmails = notification.CcEmail;
                var sendEmailSuccess = await _sendEmailRepository.SendEmail(modelApproval);
                LogSendEmail(notification, methodName, sendEmailSuccess, ref msg);

                return msg;
                #endregion
            }
            catch (Exception ex)
            {
                _logRepository.InsertTempEmail(notification.RequestorEmail!, notification.RequestorName!, 0, notification.SubjectEmail!, ex.Message);
                ExceptionUtility exceptionData = new(ex);
                BaseLogging.LogError(
                    AppSystem.Catch,
                    currentClass,
                    methodName,
                    exceptionData.GetAll().ToDescription(AppSystem.ExceptionDetails)
                    );
                throw new GlobalExceptions(methodName, ex.InnerException);
            }
        }
        #endregion

        public string WebAppAPS() => $"{_configuration.GetValue<string>($"WebAppAPS")}";

    }
}
