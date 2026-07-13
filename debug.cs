using APS_Common;
using APS_Common.EmailNotification;
using APS_Common.EncryptionData;
using APS_Common.Models.Master;
using APS_Common.Models.NonShoppingCart.PAP;
using APS_Entities.Models;
using APS_REST_API.Contracts.NonShoppingCart;
using APS_REST_API.Queries.NonShoppingCart;
using APS_SharedServices.Models;
using APS_SharedServices.Models.Procurement;
using APS_SharedServices.Models.RequestModels;
using APS_SharedServices.Models.ResponseModels;
using APS_SharedServices.Repositories.Contracts;
using APS_SharedServices.Services.Contracts;
using Dapper;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static APS_Common.Enums.ActionCategoryEnum;
using static APS_Common.Enums.FiturCategoryEnum;
using static APS_REST_API.Models.ProcurmentBuyerModel;
using static APS_REST_API.Models.PurchaseRequestFormModel;

namespace APS_REST_API.Repository.NonShoppingCart
{
	public class NonShopNotificationRepository : INonShopNotificationRepository
	{
		private readonly IDapper _dapper;
		private readonly INotificationService _notificationService;
		private readonly IAttachmentRepository _attachmentRepository;
		private readonly IAttachmentService _attachmentService;
		private readonly IEncryptionData _encryption;

		private readonly string WebAppUrl;
		private readonly string BoxerWebAppUrl;
		private readonly string objectName = "NonShopNotificationRepository";
		private readonly Logging log = new Logging
		{
			objectName = "ShopNotificationRepository"
		};

		public NonShopNotificationRepository(IConfiguration configuration, IDapper dapper, INotificationService notificationService, IAttachmentRepository attachmentRepository, IEncryptionData encryption, IAttachmentService attachmentService)
		{
			_dapper = dapper;
			_notificationService = notificationService;
			_attachmentRepository = attachmentRepository;
			_encryption = encryption;

			WebAppUrl = $"{configuration.GetValue<string>($"WebAppAPS")}";
			BoxerWebAppUrl = $"{configuration.GetValue<string>($"BoxerWebAppAPS")}";
			_attachmentService = attachmentService;
		}
		public async Task SendEmailPRFSwitchApproval(string PRFNumber, ParamListApprover listApprover)
		{
			NotificationModel notification = new NotificationModel();
			notification.SubjectEmail = "PRF Status Request";
			notification.StatusRequest = "Switch";
			notification.RequestNumber = PRFNumber;
			await SendEmailNotification(notification);
			await SendEmailNotificationForApprover(notification, listApprover);
		}
		public async Task SendEmailPRFApproval(string PRFNumber, string StatusApproval)
		{
			NotificationModel notification = new NotificationModel();
			notification.SubjectEmail = "PRF Status Request";
			notification.StatusRequest = StatusApproval;
			notification.RequestNumber = PRFNumber;
			await SendEmailNotification(notification);
			if (StatusApproval == "Process" || StatusApproval == "Approve")
			{
				await SendEmailNotificationForApprover(notification, null);
			}
		}
		public async Task SendEmailPRFCancel(string PRFNumber, string StatusRequest)
		{
			NotificationModel notification = new();
			notification.SubjectEmail = "PRF Status Request";
			notification.StatusRequest = StatusRequest;
			notification.RequestNumber = PRFNumber;
			await SendEmailNotification(notification);
		}
		public async Task SendEmailPRFNewVendor(InsertNewVendorCandidate request)
		{
			NotificationModel notification = new();
			notification.SubjectEmail = "PRF Register New Vendor";
			notification.StatusRequest = "New";
			notification.RequestNumber = request.PRFNumber;
			notification.ParamPOToVendor = new()
			{
				VendorName = request.Name,
				RequestorName = request.CreatedBy
			};
			await SendEmailNotificationPRFNewVendor(notification);
		}
		public async Task SendEmailNewVendor(string VendorName)
		{
			NotificationModel notification = new NotificationModel();
			notification.RequestId = 0;
			notification.SubjectEmail = "Register for New Vendor Registration";
			notification.StatusRequest = "New";
			notification.ParamPOToVendor = new ParamEmailPOToVendor()
			{
				VendorName = VendorName
			};
			await SendEmailNotificationNewVendor(notification);
		}
		public async Task SendEmailPRFDDRequest(int PRFId, string ActionBy)
		{
			NotificationModel notification = new NotificationModel();
			notification.SubjectEmail = "PRF DD Request";
			notification.StatusRequest = "New";
			notification.RequestId = PRFId;
			notification.ActionBy = ActionBy;
			await SendEmailNotificationDDRequest(notification);
		}
		public async Task<UploadFileResponse> UploadAttachment(AttachmentRequest attachment)
		{
			return await _attachmentService.UploadAttachment(attachment);
		}

		public async Task SaveAndSendEmailPrf(sendEmailTest param)
		{
			SendEmailModel paramEmail = new SendEmailModel();
			paramEmail.CCEmails = param.mailHeader.CCEmails;
			paramEmail.ToEmail = param.mailHeader.ToEmail;
			paramEmail.Html = param.mailBody;
			paramEmail.Subject = param.mailHeader.Subject;
			paramEmail.ReceiverType = "internal";
			await _notificationService.SendEmailHtml(paramEmail);
		}
		public async Task SendEmailPRFProcsum(string StatusApproval, string prfNumber, int? prfSummaryId = null)
		{
			NotificationModel notification = new NotificationModel();
			notification.StatusRequest = StatusApproval;
			notification.RequestNumber = prfNumber;
			await SendEmailNotificationProcsum(notification);

			PRF getPRF = await GetPRF(prfNumber);
			if (getPRF.IsRiskAssementForm || getPRF.DataActivity || getPRF.ITSecurityActivity)
			{
				await SendEmailProcsumDocumentReminder(notification, getPRF);
			}
		}
		public async Task SendEmailPAP(string StatusApproval, int PAPId, string prfNumber)
		{
			NotificationModel notification = new NotificationModel();
			notification.StatusRequest = StatusApproval;
			notification.RequestNumber = prfNumber;
			await SendEmailNotificationPAP(notification, PAPId);

			if (!prfNumber.IsNullOrEmpty())
			{
				PRF getPRF = await GetPRF(prfNumber);
				if (getPRF.IsRiskAssementForm || getPRF.DataActivity || getPRF.ITSecurityActivity)
				{
					await SendEmailProcsumDocumentReminder(notification, getPRF);
				}
			}
		}
		public async Task SendEmailPAPReleased(string StatusApproval, int PAPId)
		{
			NotificationModel notification = new NotificationModel();
			notification.StatusRequest = StatusApproval;
			await SendEmailNotificationClosePAP(notification, PAPId);
		}
		public async Task SendEmailPadiBelow(string StatusApproval, int PRFId)
		{
			NotificationModel notification = new NotificationModel();
			notification.StatusRequest = StatusApproval;
			await SendEmailNotificationPadiBelow(notification, PRFId);
		}
		public async Task SendEmailPadiAbove(string StatusApproval, string PRFNo)
		{
			NotificationModel notification = new NotificationModel();
			notification.StatusRequest = StatusApproval;
			await SendEmailNotificationPadiAbove(notification, PRFNo);
		}
		public async Task SendEmailGL(string StatusApproval, int GLId)
		{
			NotificationModel notification = new NotificationModel();
			notification.StatusRequest = StatusApproval;
			await SendEmailNotificationGL(notification, GLId);
		}
		public async Task SendEmailPOApprovalStatus(int PRFSummaryId, string StatusApproval)
		{
			NotificationModel notification = new NotificationModel();
			notification.StatusRequest = StatusApproval;
			notification.RequestId = PRFSummaryId;
			await SendEmailNotificationPOApprovalStatus(notification);
		}

		public async Task SendEmailNotification(NotificationModel notification)
		{
			try
			{
				ParamEmailNonShopCart resultPRF = await GetRequestionerDetail(notification.RequestNumber);
				List<ParamListPrfDetail> resultListPRFDetail = await GetPRFRequestDetail((int)resultPRF.RequestId);
				List<ParamListPrfApproverBudget> resultListPRFApproverBudget = await GetPRFApproverBudget(notification.RequestNumber);
				List<ParamListPrfApproverHistory> resultListPRFApproverHistory = await GetPRFApproverHistory(notification.RequestNumber);

				notification.ParamNonShopCart = resultPRF;
				notification.ParamNonShopCart.ListPRFDetail = resultListPRFDetail;
				notification.ParamNonShopCart.ListPRFApproverBudget = resultListPRFApproverBudget.Where(x => x.ApprovalGroupSubCategoryId == 1209).ToList();
				notification.ParamNonShopCart.ListPRFApproverNonBudget = resultListPRFApproverBudget.Where(x => x.ApprovalGroupSubCategoryId == 1256).ToList();
				notification.ParamNonShopCart.ListPRFApproverHistory = resultListPRFApproverHistory;

				notification.RequestId = resultPRF.RequestId;
				notification.RequestorName = resultPRF.RequestorUserName;
				notification.RequestorEmail = resultPRF.RequestorEmail;
				notification.RequestType = $"Non Shoping Cart - {(resultPRF.IsBudgetedSpend == 1 ? "PR" : "Non Budget")}";
				notification.IsUsingBoxer = false;

				var saveToLog = JsonConvert.SerializeObject(notification);
				log.LogInitialize(methodName: "Send Email Notification Pending Approval Purchase Request", saveToLog, LogType.Info);

				await _notificationService.SendEmailNotificationForNonShop(notification);
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task SendEmailNotificationForApprover(NotificationModel notification, ParamListApprover listApprover)
		{
			try
			{
				ParamEmailNonShopCart resultPRF = await GetRequestionerDetail(notification.RequestNumber);
				List<ParamListPrfDetail> resultListPRFDetail = await GetPRFRequestDetail((int)resultPRF.RequestId);
				List<ParamListPrfApproverBudget> resultListPRFApproverBudget = await GetPRFApproverBudget(notification.RequestNumber);
				List<ParamListPrfApproverHistory> resultListPRFApproverHistory = await GetPRFApproverHistory(notification.RequestNumber);

				notification.ParamNonShopCart = resultPRF;
				notification.ParamNonShopCart.ListPRFDetail = resultListPRFDetail;
				notification.ParamNonShopCart.ListPRFApproverBudget = resultListPRFApproverBudget.Where(x => x.ApprovalGroupSubCategoryId == 1209).ToList();
				notification.ParamNonShopCart.ListPRFApproverNonBudget = resultListPRFApproverBudget.Where(x => x.ApprovalGroupSubCategoryId == 1256).ToList();
				notification.ParamNonShopCart.ListPRFApproverHistory = resultListPRFApproverHistory;

				notification.RequestId = resultPRF.RequestId;
				notification.RequestType = $"Non Shoping Cart - {(resultPRF.IsBudgetedSpend == 1 ? "PR" : "Non Budget")}";

				if (listApprover != null)
				{
					notification.SubjectEmail = "PRF Pending Approval Request";
					notification.RequestorName = listApprover.ApproverName;
					notification.RequestorEmail = listApprover.ApproverEmail;
					notification.ApprovalRequestGroupMember = SetListApprover(listApprover, FiturCategory.NonShoppingCart);
					notification.IsUsingBoxer = listApprover.IsUsingBoxer;

					var saveToLog = JsonConvert.SerializeObject(notification);
					log.LogInitialize(methodName: "Send Email Notification Pending Approval Purchase Request", saveToLog, LogType.Info);

					await _notificationService.SendEmailNotificationForNonShop(notification);
				}
				else
				{
					var nextApprover = resultListPRFApproverBudget.Find(x => x.StatusSequence == 1);
					if (nextApprover != null)
					{
						ParamListApprover paramApprover = new ParamListApprover()
						{
							NoPR = notification.RequestNumber,
							ApproverAccountId = nextApprover.ApproverAccountId,
							RequestorAccountId = nextApprover.RequestorAccountId,
							ApproverName = nextApprover.ApproverUsername,
							ApproverEmail = nextApprover.ApproverEmail,
						};

						notification.SubjectEmail = "PRF Pending Approval Request";
						notification.RequestorName = nextApprover.ApproverUsername;
						notification.RequestorEmail = nextApprover.ApproverEmail;
						notification.ApprovalRequestGroupMember = SetListApprover(paramApprover, FiturCategory.NonShoppingCart);
						notification.IsUsingBoxer = true;

						var saveToLog = JsonConvert.SerializeObject(notification);
						log.LogInitialize(methodName: "Send Email Notification Pending Approval Purchase Request", saveToLog, LogType.Info);

						await _notificationService.SendEmailNotificationForNonShop(notification);
					}
				}
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public List<ResponseApprovalRequestGroupMember> SetListApprover(ParamListApprover list, FiturCategory fitur)
		{
			var members = new List<ResponseApprovalRequestGroupMember>(){ new ResponseApprovalRequestGroupMember()
			{
				AccountId = list.ApproverAccountId,
				UserName = list.ApproverName,
				Email = list.ApproverEmail,
				ApprovalRequestEmail = new List<ResponseApprovalRequestGroupMemberEmail>()
					{

						new ResponseApprovalRequestGroupMemberEmail()
						{
							AccountId = list.ApproverAccountId,
							URLAction = $@"{BoxerWebAppUrl}/ActionEmail/ActionBoxer?request={_encryption.GcmEncrypt($"{{\"Action\":{(int)ActionCategory.Approve},\"Fitur\":{(int)fitur},\"RequestNumber\":\"{list.NoPR}\",\"ApprovalAccountId\":{list.ApproverAccountId},\"RequestorAccountId\":{list.RequestorAccountId}}}")}",
							Action = "Approved",
							LinkType = 1
						},
						new ResponseApprovalRequestGroupMemberEmail()
						{
							AccountId = list.ApproverAccountId,
							URLAction = $@"{BoxerWebAppUrl}/ActionEmail/ActionBoxer?request={_encryption.GcmEncrypt($"{{\"Action\":{(int)ActionCategory.Reject},\"Fitur\":{(int)fitur},\"RequestNumber\":\"{list.NoPR}\",\"ApprovalAccountId\":{list.ApproverAccountId},\"RequestorAccountId\":{list.RequestorAccountId}}}")}",
							Action = "Rejected",
							LinkType = 1
						},
						new ResponseApprovalRequestGroupMemberEmail()
						{
							AccountId = list.ApproverAccountId,
							URLAction = $@"{WebAppUrl}/ActionEmail/ActionBoxer?request={_encryption.GcmEncrypt($"{{\"Action\":{(int)ActionCategory.Approve},\"Fitur\":{(int)fitur},\"RequestNumber\":\"{list.NoPR}\",\"ApprovalAccountId\":{list.ApproverAccountId},\"RequestorAccountId\":{list.RequestorAccountId}}}")}",
							Action = "Approved",
							LinkType = 0
						},
						new ResponseApprovalRequestGroupMemberEmail()
						{
							AccountId = list.ApproverAccountId,
							URLAction = $@"{WebAppUrl}/ActionEmail/ActionBoxer?request={_encryption.GcmEncrypt($"{{\"Action\":{(int)ActionCategory.Reject},\"Fitur\":{(int)fitur},\"RequestNumber\":\"{list.NoPR}\",\"ApprovalAccountId\":{list.ApproverAccountId},\"RequestorAccountId\":{list.RequestorAccountId}}}")}",
							Action = "Rejected",
							LinkType = 0
						}
					}
			} };
			return members;
		}
		public async Task SendEmailNotificationPRFNewVendor(NotificationModel notification)
		{
			try
			{
				ParamEmailNonShopCart resultPRF = await GetRequestionerDetail(notification.RequestNumber);
				List<ParamListApprover> resultListPRFApproverVendor = await GetPRFApproverVendor();

				notification.ParamNonShopCart = resultPRF;

				notification.RequestId = resultPRF.RequestId;
				notification.RequestType = $"Non Shoping Cart - {(resultPRF.IsBudgetedSpend == 1 ? "PR" : "Non Budget")}";

				foreach (var item in resultListPRFApproverVendor)
				{
					notification.RequestorName = item.ApproverName;
					notification.RequestorEmail = item.ApproverEmail;
					notification.IsUsingBoxer = false;

					await _notificationService.SendEmailNotificationForNonShop(notification);

					var saveToLog = JsonConvert.SerializeObject(notification);
					log.LogInitialize(methodName: objectName, saveToLog, LogType.Info);
				}
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task SendEmailNotificationNewVendor(NotificationModel notification)
		{
			try
			{
				List<ParamListApprover> resultListPRFApproverVendor = await GetPRFApproverVendor();
				notification.RequestId = 0;
				notification.RequestType = $"New Vendor";

				foreach (var item in resultListPRFApproverVendor)
				{
					notification.RequestorName = item.ApproverName;
					notification.RequestorEmail = item.ApproverEmail;
					notification.IsUsingBoxer = false;

					await _notificationService.SendEmailNotificationForNonShop(notification);

					var saveToLog = JsonConvert.SerializeObject(notification);
					log.LogInitialize(methodName: objectName, saveToLog, LogType.Info);
				}
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task SendEmailNotificationDDRequest(NotificationModel notification)
		{
			try
			{
				string query = $@"SELECT PRFNo FROM PRF WHERE Id = @PRFId";
				notification.RequestNumber = await Task.FromResult(_dapper.Get<string>(query, new DynamicParameters(new
				{
					PRFId = notification.RequestId
				})));

				string query2 = $@"SELECT UA.Username,
										   UA.eMail
									FROM SubCategory SCY
										JOIN Category CAT
											ON SCY.CategoryId = CAT.Id
										JOIN Flips.UserAccount UA
											ON SCY.SubCategoryName = UA.Username
									WHERE CAT.CategoryName = 'VendorManagement' AND SCY.SubCategoryCode NOT IN('SC-2025-03-01417')";
				var vendorManagements = await Task.FromResult(_dapper.GetAll<VendorManagement>(query2, new DynamicParameters(new
				{ }), commandType: CommandType.Text));

				List<ParamEmailDDRequest> resultListPRFDetail = await GetPRFDDRequest(notification.RequestId ?? 0);
				List<ParamListApprover> resultListPRFApproverVendor = await GetPRFApproverVendor();

				notification.ParamNonShopCartDDRequest = resultListPRFDetail;
				notification.RequestType = $"Due Diligence Request";

				List<string> vendors = new List<string>();
				foreach (var vendor in vendorManagements)
				{
					vendors.Add(vendor.eMail);
				}
				vendors.Add("procurement_amfs@axa-mandiri.co.id");

				notification.CcEmail = vendors;
				foreach (var item in resultListPRFApproverVendor)
				{
					notification.RequestorName = item.ApproverName;
					notification.RequestorEmail = item.ApproverEmail;
					notification.IsUsingBoxer = false;
					await _notificationService.SendEmailNotificationForNonShop(notification);

					var saveToLog = JsonConvert.SerializeObject(notification);
					log.LogInitialize(methodName: objectName, saveToLog, LogType.Info);
				}
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task SendEmailNotificationPAP(NotificationModel notification, int PAPId)
		{
			try
			{
				notification.ParamPAP = await GetPAPDetail(PAPId);
				notification.ParamPAP.Approver = await GetApproverPAP(PAPId);

				notification.SubjectEmail = "PAP Feedback Approval Request";
				notification.RequestId = notification.ParamPAP?.ApprovalRequestId;
				notification.RequestNumber = notification.ParamPAP.PRFNo;
				notification.RequestorName = notification.ParamPAP?.RequestorUserName;
				notification.RequestorEmail = notification.ParamPAP?.RequestorEmail;
				notification.RequestType = $"Non Shoping Cart PAP";
				notification.IsUsingBoxer = false;

				List<string> CcEmail = new List<string>()
				{
					notification.ParamPAP?.BuyerEmail
				};
				notification.CcEmail = CcEmail;

				var saveToLog = JsonConvert.SerializeObject(notification);
				log.LogInitialize(methodName: "Send Email Notification Pending Approval Purchase Request", saveToLog, LogType.Info);

				await _notificationService.SendEmailNotificationForNonShop(notification);

				#region forApprover
				PAPApproverMemberResponse nextApprover = notification?.ParamPAP?.Approver?.FirstOrDefault(e => e.Status == 1)?.Members?.FirstOrDefault(v => v.Status == 1);
				if (nextApprover != null && (notification.StatusRequest == "Approve" || notification.StatusRequest == "Pending"))
				{
					ParamListApprover paramApprover = new ParamListApprover()
					{
						NoPR = notification.RequestNumber,
						ApproverAccountId = (int)nextApprover.AccountId,
						RequestorAccountId = (int)notification?.ParamPAP?.ApprovalRequestId,
						ApproverName = nextApprover.UserName,
						ApproverEmail = nextApprover.Email,
					};

					notification.RequestId = notification?.ParamPAP?.ApprovalRequestId;
					notification.SubjectEmail = "Request for Approval - Purchase for Advance Payment (PAP)";
					notification.RequestorName = nextApprover.UserName;
					notification.RequestorEmail = nextApprover.Email;
					notification.ApprovalRequestGroupMember = SetListApprover(paramApprover, FiturCategory.PAP);
					notification.IsUsingBoxer = true;

					saveToLog = JsonConvert.SerializeObject(notification);
					log.LogInitialize(methodName: "Send Email Notification Pending Approval Purchase Request", saveToLog, LogType.Info);

					await _notificationService.SendEmailNotificationForNonShop(notification);
				}
				#endregion

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task SendEmailNotificationClosePAP(NotificationModel notification, int PAPId)
		{
			try
			{
				notification.ParamClosePAP = await GetRequestionerDetailClosePAP(PAPId);

				notification.SubjectEmail = "Your Purchase for Advaced Payment (PAP) is Completed";
				notification.RequestId = PAPId;
				notification.RequestorName = notification.ParamClosePAP?.RequestorUserName;
				notification.RequestorEmail = notification.ParamClosePAP?.RequestorEmail;
				notification.RequestType = $"Your Purchase for Advaced Payment (PAP) is Completed";
				notification.IsUsingBoxer = false;

				List<string> CcEmail = new List<string>()
				{
					notification.ParamClosePAP?.BuyerEmail
				};
				notification.CcEmail = CcEmail;

				var saveToLog = JsonConvert.SerializeObject(notification);
				log.LogInitialize(methodName: "Send Email Notification PAP Released", saveToLog, LogType.Info);

				await _notificationService.SendEmailNotificationForNonShop(notification);
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task SendEmailNotificationPadiBelow(NotificationModel notification, int PRFId)
		{
			try
			{
				notification.ParamPADIBelow = await GetRequestionerDetailPADIBelow(PRFId);

				notification.SubjectEmail = "Padi Below 10 Million";
				notification.RequestId = PRFId;
				notification.RequestorName = notification.ParamPADIBelow?.RequestorUserName;
				notification.RequestorEmail = notification.ParamPADIBelow?.RequestorEmail;
				notification.RequestType = $"Padi Below 10 Million";
				notification.IsUsingBoxer = false;

				List<string> CcEmail = new List<string>()
				{
					notification.ParamPADIBelow?.BuyerEmail
				};
				notification.CcEmail = CcEmail;

				var saveToLog = JsonConvert.SerializeObject(notification);
				log.LogInitialize(methodName: "Send Email Notification PAP Released", saveToLog, LogType.Info);

				await _notificationService.SendEmailNotificationForNonShop(notification);
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task SendEmailNotificationPadiAbove(NotificationModel notification, string PRFNo)
		{
			try
			{
				notification.ParamPADIAbove = await GetRequestionerDetailPADIAbove(PRFNo);

				notification.SubjectEmail = "Padi Above 10 Million";
				notification.RequestNumber = PRFNo;
				notification.RequestorName = notification.ParamPADIAbove?.RequestorUserName;
				notification.RequestorEmail = notification.ParamPADIAbove?.RequestorEmail;
				notification.RequestType = $"Padi Above 10 Million";
				notification.IsUsingBoxer = false;

				List<string> CcEmail = new List<string>()
				{
					notification.ParamPADIAbove?.BuyerEmail
				};
				notification.CcEmail = CcEmail;

				var saveToLog = JsonConvert.SerializeObject(notification);
				log.LogInitialize(methodName: "Send Email Notification PAP Released", saveToLog, LogType.Info);

				await _notificationService.SendEmailNotificationForNonShop(notification);
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task SendEmailNotificationGL(NotificationModel notification, int GLId)
		{
			try
			{
				notification.ParamGL = await GetRequestionerDetailGL(GLId);
				notification.ParamGL.Approver = await GetApproverGL(GLId);

				notification.SubjectEmail = "Your Guarantee Letter Feedback";
				notification.RequestId = GLId;
				notification.RequestorName = notification.ParamGL?.RequestorUserName;
				notification.RequestorEmail = notification.ParamGL?.RequestorEmail;
				notification.RequestType = $"Your Guarantee Letter Feedback";
				notification.IsUsingBoxer = false;

				List<string> CcEmail = new List<string>()
				{
					notification.ParamGL?.BuyerEmail
				};
				notification.CcEmail = CcEmail;

				var saveToLog = JsonConvert.SerializeObject(notification);
				log.LogInitialize(methodName: "Send Email Notification Feedback GL", saveToLog, LogType.Info);

				await _notificationService.SendEmailNotificationForNonShop(notification);

				#region forApprover
				PAPApproverMemberResponse nextApprover = notification?.ParamGL?.Approver?.FirstOrDefault(e => e.Status == 1)?.Members?.FirstOrDefault(v => v.Status == 1);
				if (nextApprover != null && (notification.StatusRequest == "Approve" || notification.StatusRequest == "Pending"))
				{
					ParamListApprover paramApprover = new ParamListApprover()
					{
						NoPR = notification.ParamGL.PRFNo,
						ApproverAccountId = (int)nextApprover.AccountId,
						RequestorAccountId = (int)notification!.ParamGL!.ApprovalRequestId!,
						ApproverName = nextApprover.UserName,
						ApproverEmail = nextApprover.Email,
					};

					notification.RequestId = notification?.ParamGL?.ApprovalRequestId;
					notification.SubjectEmail = "Guarantee Letter Pending Approval";
					notification.RequestorName = nextApprover.UserName;
					notification.RequestorEmail = nextApprover.Email;
					notification.ApprovalRequestGroupMember = SetListApprover(paramApprover, FiturCategory.GL);
					notification.IsUsingBoxer = true;
					notification.CcEmail = new List<string>();
					saveToLog = JsonConvert.SerializeObject(notification);
					log.LogInitialize(methodName: "Send Email Notification Pending Approval Purchase Request", saveToLog, LogType.Info);

					await _notificationService.SendEmailNotificationForNonShop(notification);
				}
				#endregion
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}


		public async Task SendEmailNotificationProcsum(NotificationModel notification)
		{
			try
			{
				ApprovalRequestProcSum resultApprovalRequest = await GetProcsumApprovalRequest(notification.RequestNumber);
				ParamEmailNonShopCart resultPRF = await GetRequestionerDetail(notification.RequestNumber);
				List<ProcsumDetailEmailResponse> resultProcsumDetail = await GetProcsumDetail(notification.RequestNumber);
				List<ApprovalRequestGroupProcSum> resultApprovalRequestGroup = await GetProcsumApprovalRequestGroup(resultApprovalRequest.ApprovalRequestId);
				List<ApprovalRequestGroupMemberProcSum> listApprover = new List<ApprovalRequestGroupMemberProcSum>();
				foreach (var group in resultApprovalRequestGroup)
				{
					group.ApprovalRequestGroupMemberList = await GetProcsumApprovalRequestGroupMember(group, resultApprovalRequest.ApprovalRequestId);
				}

				//#region attachments
				//List<AttachmentModel> listAttachments = await GetPRFAttachment(notification.RequestNumber);
				//var attachments = new Dictionary<string, string>();
				//foreach (var item in listAttachments)
				//{
				//    attachments.Add(item.OriginalFileName, item.FullPath);
				//}
				//notification.Attachments = attachments;
				//#endregion

				notification.ParamNonShopCartProcsum = resultApprovalRequest;
				notification.ParamNonShopCartProcsum.RequestionerDetail = resultPRF;
				notification.ParamNonShopCartProcsum.ProcsumDetail = resultProcsumDetail;
				notification.ParamNonShopCartProcsum.ApprovalRequestGroupList = resultApprovalRequestGroup;

				notification.SubjectEmail = "Procsum Status Approval Request";
				notification.RequestId = (int)resultApprovalRequest.ApprovalRequestId;
				notification.RequestorName = resultApprovalRequest.RequestorUserName;
				notification.RequestorEmail = resultApprovalRequest.RequestorEmail;
				notification.RequestType = $"Non Shoping Cart Procsum - {(resultApprovalRequest.IsBudgetedSpend == 1 ? "PR" : "Non Budget")}";
				notification.IsUsingBoxer = false;

				List<string> CcEmail = new List<string>()
				{
					notification.ParamNonShopCartProcsum?.RequestionerDetail?.BuyerEmail!
				};
				notification.CcEmail = CcEmail;

				var saveToLog = JsonConvert.SerializeObject(notification);
				log.LogInitialize(methodName: "Send Email Notification Pending Approval Purchase Request", saveToLog, LogType.Info);

				await _notificationService.SendEmailNotificationForNonShop(notification);

				#region forApprover
				foreach (var group in resultApprovalRequestGroup)
				{
					foreach (var item in group.ApprovalRequestGroupMemberList)
					{
						listApprover.Add(item);
					}
				}

				var nextApprover = listApprover.Find(x => x.Status == 1);
				if (nextApprover != null && (notification.StatusRequest == "Approve" || notification.StatusRequest == "Pending"))
				{
					ParamListApprover paramApprover = new ParamListApprover()
					{
						NoPR = notification.RequestNumber,
						ApproverAccountId = (int)nextApprover.AccountId,
						RequestorAccountId = (int)nextApprover.ApprovalRequestId,
						ApproverName = nextApprover.UserName,
						ApproverEmail = nextApprover.Email,
					};

					notification.RequestId = (int)nextApprover.ApprovalRequestId;
					notification.SubjectEmail = "Pending Approval Request - Procurement Summary (ProcSum)";
					notification.RequestorName = nextApprover.UserName;
					notification.RequestorEmail = nextApprover.Email;
					notification.ApprovalRequestGroupMember = SetListApprover(paramApprover, FiturCategory.ProcurementSummary);
					notification.IsUsingBoxer = true;
					notification.CcEmail = new List<string>();

					saveToLog = JsonConvert.SerializeObject(notification);
					log.LogInitialize(methodName: "Send Email Notification Pending Approval Purchase Request", saveToLog, LogType.Info);

					await _notificationService.SendEmailNotificationForNonShop(notification);
				}
				#endregion

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task SendEmailNotificationPOApprovalStatus(NotificationModel notification)
		{
			try
			{
				PurchaseRequestForm resultPRF = await GetPRFPOSummary((int)notification.RequestId);

				#region Requestor
				notification.SubjectEmail = "PO Status Approval";
				notification.RequestId = (int)resultPRF.PRFId;
				notification.RequestNumber = resultPRF.PRFNumber;
				notification.RequestorName = resultPRF.RequestorUserName;
				notification.RequestorEmail = resultPRF.RequestorEmail;
				notification.RequestType = $"Non Shoping Cart PO";
				notification.IsUsingBoxer = false;

				var saveToLog = JsonConvert.SerializeObject(notification);
				log.LogInitialize(methodName: "Send Email Notification Pending Approval Purchase Request", saveToLog, LogType.Info);

				await _notificationService.SendEmailNotificationForNonShop(notification);
				#endregion
				#region Buyer
				notification.RequestorName = resultPRF.BuyerUserName;
				notification.RequestorEmail = resultPRF.BuyerEmail;
				notification.IsUsingBoxer = false;

				saveToLog = JsonConvert.SerializeObject(notification);
				log.LogInitialize(methodName: "Send Email Notification Pending Approval Purchase Request", saveToLog, LogType.Info);

				await _notificationService.SendEmailNotificationForNonShop(notification);
				#endregion
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}

		public async Task SendEmailProcsumDocumentReminder(NotificationModel notification, PRF prf)
		{
			try
			{
				notification.SubjectEmail = "Document Reminder";
				notification.RequestId = (int)prf.Id;
				notification.RequestType = $"Document Reminder";
				notification.IsUsingBoxer = false;
				#region For Requestor
				notification.RequestorName = prf.RequestorUserName;
				notification.RequestorEmail = prf.RequestorEmail;

				notification.ParamEnhanceDocument = new ParamEnhanceDocument()
				{
					RiskAssesment = (prf.IsRiskAssementForm == true ? "Critical" : "Non Critical")
				};

				var saveToLog = JsonConvert.SerializeObject(notification);
				log.LogInitialize(methodName: "Send Email Notification Procsum Document Reminder", saveToLog, LogType.Info);

				await _notificationService.SendEmailNotificationForProcsumEnhanceDocument(notification);
				#endregion

				#region For Buyer
				notification.RequestorName = prf.BuyerUserName;
				notification.RequestorEmail = prf.BuyerEmail;
				saveToLog = JsonConvert.SerializeObject(notification);
				log.LogInitialize(methodName: "Send Email Notification Procsum Document Reminder", saveToLog, LogType.Info);

				await _notificationService.SendEmailNotificationForProcsumEnhanceDocument(notification);
				#endregion

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<ParamEmailNonShopCart> GetRequestionerDetail(string PRFNumber)
		{
			try
			{
				string query = $@"
                    SELECT 
	                    p.Id [RequestId],
	                    p.IsBudgetedSpend [IsBudgetedSpend],
	                    p.RequestorUserName,
	                    ua.Email [RequestorEmail],
	                    p.BuyerUserName,
	                    uab.Email [BuyerEmail],
	                    format(p.RequestDate, 'dd MMM yyyy') as RequestDate,
	                    cc.Name [CostCenterName],
	                    bu.Name [BusinesUnitName],
	                    p.TotalBudgetEstimation,
	                    p.TypeOfRequest,
	                    sc.Category [SpendingCategory],
	                    ssc.Spending_SubCategory [SpendingSubCategory],
	                    p.TypeOfTransaction,
	                    p.L_Currency_Code,
	                    PFS.Title
                    FROM PRF p
	                    LEFT JOIN BusinessUnit as bu on bu.Id = p.BusinesUnitId
	                    LEFT JOIN CostCenter as cc on cc.Id = p.CostCenterId
	                    LEFT JOIN MasterTable as mt on mt.ValueId = p.Status
	                    LEFT JOIN Spending_Category AS sc on sc.id = p.Spending_Category
	                    LEFT JOIN Spending_SubCategory AS ssc on ssc.id = p.Spending_SubCategory
	                    LEFT JOIN Flips.UserAccount as ua on ua.Id = p.RequestorAccountId
	                    LEFT JOIN Flips.UserAccount as uab on uab.Id = p.BuyerAccountId
	                    LEFT JOIN PRFSummary PFS ON p.Id = PFS.PRFId
                    WHERE mt.Category LIKE '%PRF.Status%'
	                    AND p.PRFNo = @PRFNumber
                ";
				return await Task.FromResult(_dapper.Get<ParamEmailNonShopCart>(query, new DynamicParameters(new
				{
					PRFNumber = PRFNumber
				})));

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<List<ParamListPrfDetail>> GetPRFRequestDetail(int PRFId)
		{
			try
			{
				string query = $@"SELECT PRFD.RequestItemName,
	                                PRFD.RequestItemNotes,
	                                SC.SubCategoryName [TypeOfGoods],
	                                PRFD.Qty,
	                                PRFD.Unit,
	                                FORMAT(PRFD.DeliveryRequestDate, 'MM/dd/yyyy') [DeliveryRequestDate],
	                                PRFD.DeliveryNotes
                                FROM PRFDetail PRFD
                                LEFT JOIN SubCategory AS SC ON SC.Id = PRFD.TypeOfGoods_SubCategoryId
                                WHERE PRFD.PRFId = @PRFId
                                ";
				return await Task.FromResult(_dapper.GetAll<ParamListPrfDetail>(query, new DynamicParameters(new
				{
					PRFId = PRFId
				})));

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<List<ParamListPrfApproverBudget>> GetPRFApproverBudget(string PRFNumber)
		{
			try
			{
				string query = $@"
SELECT
1 as x
, BU.Name [BusinesUnitName]
, CC.Name [CostCenterName]
, PRF.RequestorAccountId [RequestorAccountId]
, ARGM.Id [ApprovalRequestGroupMemberId]
, ARGM.AccountId [ApproverAccountId]
, ARGM.UserName [ApproverUsername]
, UA.Email [ApproverEmail]
, MT.Name [Status]
, MT.Sequence [StatusSequence]
, FORMAT(ARGM.ApprovalDate, 'MM/dd/yyyy') [ApprovalDate]
, (case when ARGM.Comment = 'NONBUDGETSHOPPINGCARTV2' OR ARGM.Comment = 'BUDGETSHOPPINGCARTV2' then '' else ARGM.Comment end) as ApproverNote
, ARGM.Level [Level]
, ARG.ApprovalGroup_SubCategoryId [ApprovalGroupSubCategoryId]
, SCB.SubCategoryName [ApprovalGroupSubCategoryName]

from ApprovalRequest AR
left join PRF PRF ON PRF.ApprovalRequestId = AR.Id
left join BusinessUnit BU ON BU.Id = PRF.BusinesUnitId
left join CostCenter CC ON CC.Id = PRF.CostCenterId
left join ApprovalRequestGroup ARG ON ARG.ApprovalRequestId = PRF.ApprovalRequestId
left join ApprovalRequestGroupMember ARGM ON ARGM.ApprovalRequestId = ARG.ApprovalRequestId  AND ARGM.ApprovaGroup_SubCategoryId = ARG.ApprovalGroup_SubCategoryId
left join MasterTable MT ON MT.Category LIKE '%PRF.Status%' AND MT.ValueId = ARGM.Status
left join SubCategory SCB ON ARG.ApprovalGroup_SubCategoryId = SCB.Id
left join Flips.UserAccount as ua on UA.Id = ARGM.AccountId

WHERE PRF.PRFNo = @PRFNumber
";
				return await Task.FromResult(_dapper.GetAll<ParamListPrfApproverBudget>(query, new DynamicParameters(new
				{
					PRFNumber = PRFNumber
				})));

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<List<ParamListPrfApproverHistory>> GetPRFApproverHistory(string PRFNumber)
		{
			try
			{
				string query = $@"
                                    SELECT ARGMH.UserName [UserName],
	                                    BU.Name [BusinessUnitName],
	                                    CC.Name [CostCenterName],
                                        ARGMH.Level [Level],
	                                    FORMAT(ARGMH.CreatedTime, 'MM/dd/yyyy') [CreatedTime],
	                                    MT.Name [StatusName],
                                        ARGMH.AssignedToUserName [AssignedToUserName],
	                                    ARGMH.Comment [Comment],
	                                    A.Description [Attachment]
                                    FROM dbo.ApprovalRequestGroupMemberHistory ARGMH
	                                    JOIN PRF PRF ON PRF.ApprovalRequestId = ARGMH.ApprovalRequestId
	                                    JOIN dbo.CostCenter CC ON CC.Id = ARGMH.CostCenterId
	                                    JOIN dbo.BusinessUnit BU ON BU.Id = CC.BusinessUnitId
	                                    JOIN dbo.MasterTable MT ON MT.ValueId = ARGMH.Status
	                                    JOIN Attachment A ON A.Id = ARGMH.AttachmentId
                                    WHERE MT.Category LIKE '%ApprovalRequestGroupMember.Status%' AND PRF.PRFNo = @PRFNumber
                                ";
				return await Task.FromResult(_dapper.GetAll<ParamListPrfApproverHistory>(query, new DynamicParameters(new
				{
					PRFNumber = PRFNumber
				})));

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<List<ParamListApprover>> GetPRFApproverVendor()
		{
			try
			{
				string query = $@"SELECT 
	                                    MTL.Name [ApproverName],
	                                    UA.eMail [ApproverEmail]
                                    FROM MasterTable MTL
                                    JOIN Flips.UserAccount UA ON MTL.Name = UA.Username
                                    WHERE MTL.Category = 'Admin.Support'";
				return await Task.FromResult(_dapper.GetAll<ParamListApprover>(query, new DynamicParameters()));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<List<ParamEmailDDRequest>> GetPRFDDRequest(int PRFId)
		{
			try
			{
				string query = $@"
select
PRF.PRFNo as [PRFNumber]
, VE.Name as [VendorName]
, MT.Name as [DDStatus]
, MT2.Name as [VendorStatus]
from VendorDueDiligenceRequest as VDDR with(nolock)
left join VendorDueDeligence as VDD with(nolock) on VDD.Id = VDDR.VendorDueDiligenceId
inner join Vendor as VE with(nolock) on VE.Id = VDDR.VendorId
inner join PRF as PRF with(nolock) on PRF.Id = VDDR.PrfId
left join PRFVendorQuotation as pvq with(nolock) on pvq.PRFId = PRF.Id
left join MasterTable as MT with(nolock) on MT.Category = 'VendorDueDiligence.Status' and MT.ValueId = VDDR.Status
left join MasterTable as MT2 with(nolock) on MT2.Category = 'VendorDueDiligence.Status' and MT2.ValueId = VE.Status
where PRF.Id = @PRFId
";
				return await Task.FromResult(_dapper.GetAll<ParamEmailDDRequest>(query, new DynamicParameters(new
				{
					PRFId = PRFId
				})));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<List<AttachmentModel>> GetPRFAttachment(string PRFNumber)
		{
			try
			{
				string query = $@"
                                    SELECT PRF.PRFNo,
		                                    AT.Id [Id]
		                                    ,AT.RefId [RefId]
		                                    ,AT.Category [Category]
		                                    ,AT.RelatedTableName [RelatedTableName]
		                                    ,AT.FullPath [FullPath]
		                                    ,AT.Description [Description]
		                                    ,AT.OriginalFileName [OriginalFileName]
		                                    ,AT.Checksum [Checksum]
		                                    ,AT.CreatedBy [CreatedBy]
		                                    ,AT.CreatedTime [CreatedTime]
		                                    ,AT.LastUpdatedBy [LastUpdatedBy]
		                                    ,AT.LastUpdatedTime [LastUpdatedTime]
                                    FROM dbo.PRF PRF
                                    JOIN dbo.Attachment AT ON AT.RefId = PRF.Id
                                    WHERE AT.RelatedTableName IN ('PRFSummary')
	                                    AND PRF.PRFNo = @PRFNumber
                                ";
				return await Task.FromResult(_dapper.GetAll<AttachmentModel>(query, new DynamicParameters(new
				{
					PRFNumber = PRFNumber
				})));

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<ApprovalRequestProcSum> GetProcsumApprovalRequest(string prfNumber)
		{
			try
			{
				string query = $@"
                    SELECT 
                        AR.Id [ApprovalRequestId]
                        ,AR.RequestNo [RequestNo]
                        ,AR.Remark [Remark]
                        ,AR.Status [Status]
                        ,AR.ApprovalFlowId [ApprovalFlowId]
                        ,AR.CreatedTime [CreatedTime]
                        ,AR.CreatedBy [CreatedBy]
                        ,AR.LastUpdatedBy [LastUpdatedBy]
                        ,AR.LastUpdatedTime [LastUpdatedTime]
                        ,AR.RequestorAccountId [RequestorAccountId]
                        ,AR.RequestorUserName [RequestorUserName]
                        ,AR.RequestorEmail [RequestorEmail]
                        ,AR.CreatorAccountId [CreatorAccountId]
                        ,AR.CreatorUserName [CreatorUserName]
                        ,AR.CreatorEmail [CreatorEmail]
                        ,AR.CostCenterId [CostCenterId]
                        ,AR.VendorId [VendorId]
                        ,AR.VendorEmail [VendorEmail]
                        ,AR.RefApprovalRequestId [RefApprovalRequestId]
                        ,PRF.IsBudgetedSpend [IsBudgetedSpend]
                    FROM PRF PRF
                    OUTER APPLY (SELECT TOP 1 * FROM PRFSummary WHERE PRFId = PRF.Id ORDER BY Id DESC) PS
                    JOIN ApprovalRequest AR ON AR.Id = PS.ApprovalRequestId
                    WHERE PRF.PRFNo = @PRFNumber
                ";
				return await Task.FromResult(_dapper.Get<ApprovalRequestProcSum>(query, new DynamicParameters(new
				{
					PRFNumber = prfNumber
				}), commandType: CommandType.Text));

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<List<ApprovalRequestGroupProcSum>> GetProcsumApprovalRequestGroup(long? ApprovalRequestId = null)
		{
			try
			{
				string query = $@"
                                    SELECT ARG.Id [ApprovalRequestGroupId]
                                            ,ARG.ApprovalRequestId [ApprovalRequestId]
                                            ,ARG.ApprovalGroup_SubCategoryId [ApprovalGroupSubCategoryId]
	                                        ,SC.SubCategoryName [ApprovalGroupSubCategoryName]
                                            ,ARG.Sequence [Sequence]
                                            ,ARG.Remark [Remark]
                                            ,ARG.Status [Status]
                                            ,MT.Name [StatusName]
                                            ,ARG.CreatedTime [CreatedTime]
                                            ,ARG.CreatedBy [CreatedBy]
                                            ,ARG.LastUpdatedBy [LastUpdatedBy]
                                            ,ARG.LastUpdatedTime [LastUpdatedTime]
                                    FROM dbo.ApprovalRequestGroup ARG
                                    JOIN dbo.SubCategory SC ON SC.Id = ARG.ApprovalGroup_SubCategoryId
                                    JOIN dbo.MasterTable MT ON MT.ValueId = ARG.Status
                                    WHERE MT.Category LIKE '%ApprovalRequestGroup.Status%' AND ARG.ApprovalRequestId = @ApprovalRequestId
                                    ORDER BY ARG.Sequence
                                ";
				return await Task.FromResult(_dapper.GetAll<ApprovalRequestGroupProcSum>(query, new DynamicParameters(new
				{
					ApprovalRequestId = ApprovalRequestId
				}), commandType: CommandType.Text));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<List<ApprovalRequestGroupMemberProcSum>> GetProcsumApprovalRequestGroupMember(ApprovalRequestGroupProcSum group = null, long? ApprovalRequestId = null)
		{
			try
			{
				string query = $@"
                                    SELECT ARGM.Id [ApprovalRequestGroupMemberId]
                                            ,ARGM.ApprovalRequestId [ApprovalRequestId]
                                            ,ARGM.CostCenterId [CostCenterId]
                                            ,ARGM.AccountId [AccountId]
                                            ,ARGM.UserName [UserName]
                                            ,ARGM.email [Email]
                                            ,ARGM.ApprovaGroup_SubCategoryId [ApprovaGroupSubCategoryId]
                                            ,ARGM.Status [Status]
                                            ,CASE
			                                    WHEN MT.Name LIKE '%new%' THEN 'Pending'
			                                    ELSE MT.Name
		                                    END [StatusName]
                                            ,ARGM.ApprovalDate [ApprovalDate]
                                            ,ARGM.RejectionDate [RejectionDate]
                                            ,ARGM.CancelDate [CancelDate]
                                            ,ARGM.Sequence [Sequence]
                                            ,ARGM.Comment [Comment]
                                            ,ARGM.CreatedTime [CreatedTime]
                                            ,ARGM.CreatedBy [CreatedBy]
                                            ,ARGM.LastUpdatedBy [LastUpdatedBy]
                                            ,ARGM.LastUpdatedTime [LastUpdatedTime]
                                            ,ARGM.Level [Level]
                                            ,CC.Name [CostCenterName]
	                                        ,BU.Name [BusinessUnitName]
                                            {(group.ApprovalGroupSubCategoryName.Contains("shopping cart - request", StringComparison.CurrentCultureIgnoreCase) ? ",PSD.Id [PRFSummaryDetailId] ,CONCAT(PSD.ItemDescription, ' ', PSD.Qty, ' ', PSD.Unit, '(', VE.Name, ')') [ItemDescription]" : "")}
                                    FROM dbo.{(group.ApprovalGroupSubCategoryName.Contains("shopping cart - request", StringComparison.CurrentCultureIgnoreCase) ? "ApprovalRequestGroupMemberDAP" : "ApprovalRequestGroupMember")} ARGM
                                    JOIN dbo.CostCenter CC ON CC.Id = ARGM.CostCenterId
                                    JOIN dbo.BusinessUnit BU ON BU.Id = CC.BusinessUnitId
                                    JOIN dbo.MasterTable MT ON MT.ValueId = ARGM.Status
                                    {(group.ApprovalGroupSubCategoryName.Contains("shopping cart - request", StringComparison.CurrentCultureIgnoreCase) ? "JOIN dbo.PRFSummaryDetail PSD ON PSD.Id = ARGM.PRFSummaryDetailId JOIN dbo.Vendor VE ON VE.Id = PSD.VendorId" : "")}
                                    WHERE MT.Category = 'ApprovalRequestGroupMember.Status' AND ARGM.ApprovalRequestId = @ApprovalRequestId AND ARGM.ApprovaGroup_SubCategoryId = @ApprovaGroupSubCategoryId
                                    ORDER BY 
                                    {(group.ApprovalGroupSubCategoryName.Contains("shopping cart - request", StringComparison.CurrentCultureIgnoreCase) ? "PSD.Id, ARGM.Level ASC" : "ARGM.Level ASC")}
                                ";
				return await Task.FromResult(_dapper.GetAll<ApprovalRequestGroupMemberProcSum>(query, new DynamicParameters(new
				{
					ApprovalRequestId = ApprovalRequestId,
					ApprovaGroupSubCategoryId = group.ApprovalGroupSubCategoryId
				}), commandType: CommandType.Text));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<PurchaseRequestForm> GetPRFPOSummary(int PRFSummaryId)
		{
			try
			{
				string query = $@"SELECT PRF.Id [PRFId],
                                    PRF.PRFNo [PRFNumber],
                                    PRF.RequestorAccountId,
	                                PRF.RequestorUserName,
	                                UA.Email [RequestorEmail],
	                                PRF.BuyerAccountId,
	                                PRF.BuyerUserName,
	                                UA2.Email [BuyerEmail]
                                FROM PRFSummary PS
	                                JOIN PRF PRF ON PS.PRFId = PRF.Id
	                                JOIN Flips.UserAccount UA ON UA.Id = PRF.RequestorAccountId
	                                JOIN Flips.UserAccount UA2 ON UA2.Id = PRF.BuyerAccountId
                                WHERE PS.Id = @PRFSummaryId";
				return await Task.FromResult(_dapper.Get<PurchaseRequestForm>(query, new DynamicParameters(new
				{
					PRFSummaryId = PRFSummaryId
				})));

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<List<ProcsumDetailEmailResponse>> GetProcsumDetail(string RequestNumber)
		{
			try
			{
				string query = $@"select 1 as x,
                                           psd.ItemName,
                                           psd.ItemDescription,
                                           psd.Qty,
                                           psd.Unit,
                                           v.Name as VendorName,
                                           TypeOfGoods_sc.SubCategoryName as TypeOfGoods_SubCategoryName,
                                           pscc.GrandTotalAmount,
	                                       psd.Remarks
                                    from PRF as p
                                        cross apply
                                    (
                                        select top 1
                                            ps.*
                                        from PRFSummary as ps
                                        where ps.PRFId = p.Id
                                        order by ps.Id desc
                                    ) as ps
                                        inner join PRFSummaryDetail as psd
                                            on psd.PRFSummaryId = ps.Id
                                        inner join Vendor as v
                                            on v.Id = psd.VendorId
                                        left join SubCategory as TypeOfGoods_sc
                                            on TypeOfGoods_sc.Id = psd.TypeOfGoods_SubCategoryId
                                        outer apply
                                    (
                                        select sum(pscc.TotalAmmount) as GrandTotalAmount
                                        from PRFSummaryCostCenter as pscc
                                        where pscc.PRFSummaryDetailId = psd.Id
                                    ) as pscc
                                    where psd.IsSelected = 1
                                          AND p.PRFNo = @RequestNumber";
				return await Task.FromResult(_dapper.GetAll<ProcsumDetailEmailResponse>(query, new DynamicParameters(new
				{
					RequestNumber = RequestNumber
				})));

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<PRF> GetPRF(string PRFNo)
		{
			try
			{
				string query = $@"
                    SELECT 
                        PRF.Id,
                        PRF.PRFNo, 
                        PRF.RequestorAccountId,
                        PRF.RequestorUserName,
                        UARequestor.Email AS RequestorEmail,
                        PRF.BuyerAccountId,
                        PRF.BuyerUserName,
                        UABuyer.Email AS BuyerEmail,
                        PRF.IsRiskAssementForm, 
                        PRF.DataActivity, 
                        PRF.ITSecurityActivity
                    FROM 
                        PRF PRF
                    INNER JOIN 
                        Flips.UserAccount UARequestor ON UARequestor.id = PRF.RequestorAccountId
                    INNER JOIN 
                        Flips.UserAccount UABuyer ON UABuyer.id = PRF.BuyerAccountId
                    WHERE
	                    PRF.PRFNo = @PRFNo;
                ";
				return await Task.FromResult(_dapper.Get<PRF>(query, new DynamicParameters(new
				{
					PRFNo = PRFNo
				})));

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}

		public async Task<ParamEmailNonShopCartPAP> GetPAPDetail(int PAPId)
		{
			try
			{
				string query = $@"
                    SELECT 
                        PRF.PRFNo,
                        PRF.RequestorUserName,
                        PRF.RequestorEmail,
                        PRF.BuyerUserName,
                        PRF.BuyerEmail,
                        PRF.RequestDate,
	                    REPLACE(FORMAT(SUM(PAPD.TotalBaseAmount), 'C'), '$', '') TotalBaseAmount,
                        CC.Name AS CostCenterName,
                        BU.Name AS BusinessUnitName,
                        SC.Category AS SpendingCategory,
                        PAP.L_Currency_Code,
                        PAP.PAPNo,
                        PAP.ApprovalRequestId,
                        ARGM1.TotalApproval,
                        ARGM2.TotalApprovalAction
                    FROM PAP PAP
                    JOIN PAPDetail PAPD 
                        ON PAPD.PAPId = PAP.Id
                    JOIN PRF PRF 
                        ON PRF.Id = PAP.PRFId
                    JOIN Flips.UserAccount UA
                        ON UA.Id = PRF.RequestorAccountId
                    JOIN CostCenter CC
                        ON CC.Id = UA.CostCenterId
                    JOIN BusinessUnit BU
                        ON BU.Id = CC.BusinessUnitId
                    JOIN Spending_Category SC
                        ON SC.Id = PRF.Spending_Category
                    OUTER APPLY (
                        SELECT COUNT(Id) AS TotalApproval 
                        FROM ApprovalRequestGroupMember 
                        WHERE ApprovalRequestId = PAP.ApprovalRequestId
                    ) ARGM1
                    OUTER APPLY (
                        SELECT COUNT(Id) AS TotalApprovalAction 
                        FROM ApprovalRequestGroupMember 
                        WHERE ApprovalRequestId = PAP.ApprovalRequestId 
                          AND Status >= 2
                    ) ARGM2
                    WHERE PAP.Id = @PAPId
                    GROUP BY
                        PRF.PRFNo,
                        PRF.RequestorUserName,
                        PRF.RequestorEmail,
                        PRF.BuyerUserName,
                        PRF.BuyerEmail,
                        PRF.RequestDate,
                        CC.Name,
                        BU.Name,
                        SC.Category,
                        PAP.L_Currency_Code,
                        PAP.PAPNo,
                        ARGM1.TotalApproval,
                        ARGM2.TotalApprovalAction,
                        PAP.ApprovalRequestId
                ";
				return await Task.FromResult(_dapper.Get<ParamEmailNonShopCartPAP>(query, new DynamicParameters(new
				{
					PAPId
				}), commandType: CommandType.Text));

			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<List<PAPApproverResponse>> GetApproverPAP(int PAPId)
		{
			try
			{
				List<PAPApproverResponse> PAPApprover = await GetApprovalRequestGroup(PAPId);
				if (PAPApprover != null)
				{
					foreach (var Approver in PAPApprover)
					{
						Approver.Members = await GetApprovalRequestGroupMember((int)Approver.ApprovalRequestId, (int)Approver.ApprovalGroup_SubCategoryId);
					}

					return PAPApprover;
				}
				else
				{
					return null;
				}
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		private async Task<List<PAPApproverResponse>> GetApprovalRequestGroup(int PAPId)
		{
			try
			{
				return await Task.FromResult(_dapper.GetAll<PAPApproverResponse>(PAPApproverQuery.GetApprovalRequestGroup, new DynamicParameters(new
				{
					PAPId
				}), commandType: CommandType.Text));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		private async Task<List<PAPApproverMemberResponse>> GetApprovalRequestGroupMember(int ApprovalRequestId, int ApprovaGroup_SubCategoryId)
		{
			try
			{
				return await Task.FromResult(_dapper.GetAll<PAPApproverMemberResponse>(PAPApproverQuery.GetApprovalRequestGroupMember, new DynamicParameters(new
				{
					ApprovalRequestId,
					ApprovaGroup_SubCategoryId
				}), commandType: CommandType.Text));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<List<PAPApproverResponse>> GetApproverGL(int GLId)
		{
			try
			{
				List<PAPApproverResponse> GLApprover = await GetApprovalRequestGroupGL(GLId);
				if (GLApprover != null)
				{
					foreach (var Approver in GLApprover)
					{
						Approver.Members = await GetApprovalRequestGroupMember((int)Approver.ApprovalRequestId, (int)Approver.ApprovalGroup_SubCategoryId);
					}

					return GLApprover;
				}
				else
				{
					return null;
				}
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		private async Task<List<PAPApproverResponse>> GetApprovalRequestGroupGL(int GLId)
		{
			try
			{
				return await Task.FromResult(_dapper.GetAll<PAPApproverResponse>(GLApproverQuery.GetApprovalRequestGroup, new DynamicParameters(new
				{
					GLId
				}), commandType: CommandType.Text));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}

		public async Task<ParamPAPRelease> GetRequestionerDetailClosePAP(int PAPId)
		{
			try
			{
				string query = $@"
                    SELECT 
	                    PAP.PAPNo,
                    	PRF.PRFNo,
	                    PRF.RequestorUserName,
	                    UAR.Email AS RequestorEmail,
	                    PRF.BuyerUserName,
	                    UAB.Email As BuyerEmail,
	                    CCR.Name AS CostCenterName,
	                    BUR.Name AS BusinessUnitName,
	                    PRF.RequestDate,
                        S_C.Category AS SpendingCategory,
	                    PAPD.TotalBaseAmount,
	                    PAP.L_Currency_Code,
	                    SC.SubCategoryName AS FinanceType,
                        ARGM1.TotalApproval,
                        ARGM2.TotalApprovalAction
                    FROM PAP PAP
                    JOIN PRF PRF ON PRF.Id = PAP.PRFId
                    JOIN Flips.UserAccount UAR ON UAR.Id = PRF.RequestorAccountId
                    JOIN CostCenter CCR ON CCR.Id = UAR.CostCenterId
                    JOIN BusinessUnit BUR ON BUR.Id = CCR.BusinessUnitId
                    JOIN Spending_Category S_C ON S_C.Id = PRF.Spending_Category
                    JOIN SubCategory SC ON SC.Id = PAP.FinanceType_SubCategory
                    JOIN Flips.UserAccount UAB ON UAB.Id = PRF.RequestorAccountId
                    OUTER APPLY (
                        SELECT REPLACE(FORMAT(SUM(PAPD1.TotalBaseAmount), 'C'), '$', '') AS TotalBaseAmount 
                        FROM PAPDetail PAPD1 
                        WHERE PAPD1.PAPId = PAP.Id
                    ) AS PAPD
                    OUTER APPLY (
                        SELECT COUNT(Id) AS TotalApproval 
                        FROM ApprovalRequestGroupMember 
                        WHERE ApprovalRequestId = PAP.ApprovalRequestId
                    ) ARGM1
                    OUTER APPLY (
                        SELECT COUNT(Id) AS TotalApprovalAction 
                        FROM ApprovalRequestGroupMember 
                        WHERE ApprovalRequestId = PAP.ApprovalRequestId 
                          AND Status >= 2
                    ) ARGM2
                    WHERE PAP.Id = @PAPId
                ";
				return await Task.FromResult(_dapper.Get<ParamPAPRelease>(query, new DynamicParameters(new
				{
					PAPId
				}), commandType: CommandType.Text));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}

		public async Task<ParamPADIBelow> GetRequestionerDetailPADIBelow(int PRFId)
		{
			try
			{
				string query = $@"
                    SELECT 
	                    PRF.PRFNo,
	                    PRF.RequestorUserName,
	                    UAR.Email AS RequestorEmail,
	                    PRF.BuyerUserName,
	                    UAB.Email As BuyerEmail,
	                    CCR.Name AS CostCenterName,
	                    BUR.Name AS BusinessUnitName,
	                    PRF.RequestDate,
                        S_C.Category AS SpendingCategory,
	                    REPLACE(FORMAT(PRF.TotalBudgetEstimation, 'C'), '$', '') TotalBaseAmount,
	                    PRF.L_Currency_Code,
	                    SC.SubCategoryName AS SubCategoryType
                    FROM PRF PRF
                    JOIN Flips.UserAccount UAR ON UAR.Id = PRF.RequestorAccountId
                    JOIN Flips.UserAccount UAB ON UAB.Id = PRF.RequestorAccountId
                    JOIN CostCenter CCR ON CCR.Id = UAR.CostCenterId
                    JOIN BusinessUnit BUR ON BUR.Id = CCR.BusinessUnitId
                    JOIN Spending_Category S_C ON S_C.Id = PRF.Spending_Category
                    JOIN SubCategory SC ON SC.Id = PRF.TypeProcess_SubCategory
                    WHERE PRF.Id = @PRFId
                ";
				return await Task.FromResult(_dapper.Get<ParamPADIBelow>(query, new DynamicParameters(new
				{
					PRFId
				}), commandType: CommandType.Text));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<ParamPADIAbove> GetRequestionerDetailPADIAbove(string PRFNo)
		{
			try
			{
				string query = $@"
                    SELECT 
	                    PRF.PRFNo,
	                    PRF.RequestorUserName,
	                    UAR.Email AS RequestorEmail,
	                    PRF.BuyerUserName,
	                    UAB.Email As BuyerEmail,
	                    CCR.Name AS CostCenterName,
	                    BUR.Name AS BusinessUnitName,
	                    PRF.RequestDate,
                        S_C.Category AS SpendingCategory,
	                    PSD.TotalBaseAmount,
	                    PRF.L_Currency_Code,
	                    SC.SubCategoryName AS SubCategoryType
                    FROM PRF PRF
                    JOIN Flips.UserAccount UAR ON UAR.Id = PRF.RequestorAccountId
                    JOIN Flips.UserAccount UAB ON UAB.Id = PRF.RequestorAccountId
                    JOIN CostCenter CCR ON CCR.Id = UAR.CostCenterId
                    JOIN BusinessUnit BUR ON BUR.Id = CCR.BusinessUnitId
                    JOIN Spending_Category S_C ON S_C.Id = PRF.Spending_Category
                    JOIN SubCategory SC ON SC.Id = PRF.TypeProcess_SubCategory
                    OUTER APPLY (
	                    SELECT 
		                    REPLACE(FORMAT(SUM(PSD1.TotalBaseAmmount), 'C'), '$', '') TotalBaseAmount
	                    FROM PRFSummary PS1
	                    JOIN PRFSummaryDetail PSD1 ON PSD1.PRFSummaryId = PS1.Id
	                    WHERE PS1.Status != 4
		                    AND PS1.PRFId = PRF.Id
                    ) AS PSD
                    WHERE PRF.PRFNo = @PRFNo
                ";
				return await Task.FromResult(_dapper.Get<ParamPADIAbove>(query, new DynamicParameters(new
				{
					PRFNo
				}), commandType: CommandType.Text));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
		public async Task<ParamGL> GetRequestionerDetailGL(int GLId)
		{
			try
			{
				string query = $@"
                    SELECT 
	                    PRF.PRFNo,
	                    PRF.RequestorUserName,
	                    UAR.Email AS RequestorEmail,
	                    PRF.BuyerUserName,
	                    UAB.Email As BuyerEmail,
	                    CCR.Name AS CostCenterName,
	                    BUR.Name AS BusinessUnitName,
	                    PRF.RequestDate,
	                    V.Name AS VendorName,
	                    GLD.TotalBaseAmount,
	                    PRF.L_Currency_Code,
	                    GL.GLNumber,
	                    GL.Description,
                        S_C.Category AS SpendingCategory,
                        GL.ApprovalRequestId,
                        ARGM1.TotalApproval,
                        ARGM2.TotalApprovalAction
                    FROM GuaranteeLetter GL
                    JOIN PRFSummary PS ON PS.Id = GL.PRFSummaryId
                    JOIN PRF PRF ON PRF.id = PS.PRFId
                    JOIN Flips.UserAccount UAR ON UAR.Id = PRF.RequestorAccountId
                    JOIN Flips.UserAccount UAB ON UAB.Id = PRF.RequestorAccountId
                    JOIN CostCenter CCR ON CCR.Id = UAR.CostCenterId
                    JOIN BusinessUnit BUR ON BUR.Id = CCR.BusinessUnitId
                    JOIN Vendor V ON V.Id = GL.VendorId
                    JOIN Spending_Category S_C ON S_C.Id = PRF.Spending_Category
                    OUTER APPLY (
	                    SELECT REPLACE(FORMAT(SUM(GLD1.TotalBaseAmount), 'C'), '$', '') TotalBaseAmount FROM GuaranteeLetterDetail GLD1 WHERE GLD1.GuaranteeLetterId = GL.Id
                    ) AS GLD
                    OUTER APPLY (
                        SELECT COUNT(Id) AS TotalApproval 
                        FROM ApprovalRequestGroupMember 
                        WHERE ApprovalRequestId = GL.ApprovalRequestId
                    ) ARGM1
                    OUTER APPLY (
                        SELECT COUNT(Id) AS TotalApprovalAction 
                        FROM ApprovalRequestGroupMember 
                        WHERE ApprovalRequestId = GL.ApprovalRequestId 
                            AND Status >= 2
                    ) ARGM2
                    WHERE GL.Id = @GLId
                ";
				return await Task.FromResult(_dapper.Get<ParamGL>(query, new DynamicParameters(new
				{
					GLId
				}), commandType: CommandType.Text));
			}
			catch (Exception ex)
			{
				throw new GlobalExceptions(objectName, ex.InnerException);
			}
		}
	}
}
