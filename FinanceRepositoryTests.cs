using APS_Common;
using APS_Entities.Models;
using APS_REST_API.Contracts;
using APS_REST_API.Contracts.v1;
using APS_REST_API.Models;
using APS_REST_API.Models.Finance;
using APS_REST_API.Models.FinanceVoucher;
using APS_REST_API.Models.InvoiceManagementModel;
using APS_REST_API.Models.ReportBsmModel;
using APS_REST_API.Models.ReportMcmModel;
using APS_REST_API.Models.Request;
using APS_REST_API.Models.ResponseData;
using APS_REST_API.Payloads.Response.Attachment;
using APS_REST_API.Payloads.Response.Finance;
using APS_REST_API.Repository;
using APS_REST_API.Tests.Mocks.Finance;
using APS_SharedServices.Repositories.Contracts;
using APS_SharedServices.Services.Contracts;
using APS_TrexConsumer.Services.Contracts;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Data;
using System.Threading;
using Xunit;
using ApprovalRequest = APS_Entities.Models.ApprovalRequest;

namespace APS_REST_API.Tests.Repositories
{
    public class FinanceRepositoryTests
    {
        private readonly FinanceRepository _financeRepository;
        private readonly Mock<IDapper> _dapper = new Mock<IDapper>();
        private readonly Mock<INotificationService> _notificationService = new Mock<INotificationService>();
        private readonly Mock<IExternalService> _externalServices = new Mock<IExternalService>();
        private readonly Mock<IBudgetTransactionRepository> _budgetTransactionRepository = new Mock<IBudgetTransactionRepository>();
        private readonly Mock<ISubmissionApprovalRepository> _submissionApprovalRepository = new Mock<ISubmissionApprovalRepository>();
        private readonly Mock<IApprovalMatrixRepository> _approvalMatrixRepository = new();
        private readonly Mock<IInvoiceManagementRepository> _invoiceManagementRepository = new();
        private readonly Mock<IUpdatePaymentTrexConsumerService> _updatePaymentTrexConsumerService = new();
        private readonly Mock<IConfiguration> _config = new();

        public FinanceRepositoryTests()
        {
            _financeRepository = new FinanceRepository(
                _dapper.Object,
                _notificationService.Object,
                _externalServices.Object,
                _submissionApprovalRepository.Object,
                _budgetTransactionRepository.Object,
                _approvalMatrixRepository.Object,
                _invoiceManagementRepository.Object,
                _updatePaymentTrexConsumerService.Object,
                _config.Object
                );
        }

        [Fact]
        public void GetRequestList_TypeRI_ReturnOk()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("RI");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetRequestList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetRequestList_TypeCA_ReturnOk()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("CA");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetRequestList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetRequestList_TypeTR_ReturnOk()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("TR");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetRequestList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetRequestList_TypeSC_ReturnOk()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("SC");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetRequestList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetRequestList_TypeVC_ReturnOk()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("VC");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetRequestList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetRequestList_TypeSTL_ReturnOk()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("STL");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetRequestList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetRequestList_TypeTRSTL_ReturnOk()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("TRSTL");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetRequestList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetRequestList_TypeINVTR_ReturnOk()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("INVTR");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetRequestList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetRequestList_TypeNON_ReturnOk()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("NON");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetRequestList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetRequestList_TypeNONVC_ReturnOk()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("NONVC");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetRequestList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetRequestList_All_ReturnOk()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("ALL");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetRequestList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetSettlementList()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("STL");
            _dapper.Setup(repo => repo.GetAll<FinanceListResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetRequestList);

            //Act
            var result = _financeRepository.GetSettlementList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetReimbursementDetail()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestList("RI");
            _dapper.Setup(repo => repo.GetAll<FinanceReimbursementDetailResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetReimbursementDetail);

            //Act
            var result = _financeRepository.GetReimbursementDetail(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetSettlementDetailOtherCost()
        {
            //Arrange
            string settlementDetailId = "";
            _dapper.Setup(repo => repo.GetAll<FinanceSettlementOtherCostResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetSettlementDetailOtherCost);

            //Act
            var result = _financeRepository.GetSettlementDetailOtherCost(settlementDetailId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetSettlementDetailOtherCostBySettlementId()
        {
            //Arrange
            string settlementId = "";
            _dapper.Setup(repo => repo.GetAll<FinanceSettlementOtherCostResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetSettlementDetailOtherCostBySettlementId);

            //Act
            var result = _financeRepository.GetSettlementDetailOtherCostBySettlementId(settlementId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ApprovalRequestDetail_TypeRI()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestDetail("reimbursement");
            _dapper.Setup(repo => repo.Get<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest);

            //Act
            var result = _financeRepository.ApprovalRequestDetail(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ApprovalRequestDetail_TypeCA()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestDetail("cash advance");
            _dapper.Setup(repo => repo.Get<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest);

            //Act
            var result = _financeRepository.ApprovalRequestDetail(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ApprovalRequestDetail_TypeTR()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestDetail("travel");
            _dapper.Setup(repo => repo.Get<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest);

            //Act
            var result = _financeRepository.ApprovalRequestDetail(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetApprovalFinanceGroupMember()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetRequestDetail("travel");
            _dapper.Setup(repo => repo.GetAll<ApprovalGroupModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetApprovalFinanceGroupMember);

            //Act
            var result = _financeRepository.GetApprovalFinanceGroupMember();

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetApprovalFinanceSettlementGroupMember()
        {
            //Arrange
            string level = "";
            _dapper.Setup(repo => repo.GetAll<ApprovalGroupModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetApprovalFinanceSettlementGroupMember);

            //Act
            var result = _financeRepository.GetApprovalFinanceSettlementGroupMember(level);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachment_TypeRI()
        {
            //Arrange
            string requestNumber = "";
            string typeRequest = "reimbursement";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);

            //Act
            var result = _financeRepository.GetAttachment(requestNumber, typeRequest);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachment_TypeTR()
        {
            //Arrange
            string requestNumber = "";
            string typeRequest = "travel";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);

            //Act
            var result = _financeRepository.GetAttachment(requestNumber, typeRequest);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachment_TypeSC()
        {
            //Arrange
            string requestNumber = "";
            string typeRequest = "shopping cart";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);

            //Act
            var result = _financeRepository.GetAttachment(requestNumber, typeRequest);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachment_TypeVC()
        {
            //Arrange
            string requestNumber = "";
            string typeRequest = "shopping cart vc";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);

            //Act
            var result = _financeRepository.GetAttachment(requestNumber, typeRequest);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachment_TypeSTL()
        {
            //Arrange
            string requestNumber = "";
            string typeRequest = "settlement";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);

            //Act
            var result = _financeRepository.GetAttachment(requestNumber, typeRequest);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachment_TypeSHOP()
        {
            //Arrange
            string requestNumber = "";
            string typeRequest = "shopping cart";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);

            //Act
            var result = _financeRepository.GetAttachment(requestNumber, typeRequest);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachment_TypeNONSHOP()
        {
            //Arrange
            string requestNumber = "";
            string typeRequest = "non shopping cart";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);

            //Act
            var result = _financeRepository.GetAttachment(requestNumber, typeRequest);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachment_TypeNONSHOPVC()
        {
            //Arrange
            string requestNumber = "";
            string typeRequest = "non shopping cart vc";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);

            //Act
            var result = _financeRepository.GetAttachment(requestNumber, typeRequest);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachment_TypeSHOPVC()
        {
            //Arrange
            string requestNumber = "";
            string typeRequest = "shopping cart vc";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);

            //Act
            var result = _financeRepository.GetAttachment(requestNumber, typeRequest);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachment_TypeTRSTL()
        {
            //Arrange
            string requestNumber = "";
            string typeRequest = "travel settlement";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);

            //Act
            var result = _financeRepository.GetAttachment(requestNumber, typeRequest);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachmentVoucher_TypeRI()
        {
            //Arrange
            string voucherId = "20";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);
            _dapper.Setup(repo => repo.Get<FinanceVoucherHeaderResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("reimbursement"));

            //Act
            var result = _financeRepository.GetAttachmentVoucher(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachmentVoucher_TypeSC()
        {
            //Arrange
            string voucherId = "20";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);
            _dapper.Setup(repo => repo.Get<FinanceVoucherHeaderResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("shopping cart"));

            //Act
            var result = _financeRepository.GetAttachmentVoucher(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachmentVoucher_TypeTR()
        {
            //Arrange
            string voucherId = "20";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);
            _dapper.Setup(repo => repo.Get<FinanceVoucherHeaderResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("travel"));

            //Act
            var result = _financeRepository.GetAttachmentVoucher(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachmentVoucher_TypeCA()
        {
            //Arrange
            string voucherId = "20";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);
            _dapper.Setup(repo => repo.Get<FinanceVoucherHeaderResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("cash advance"));

            //Act
            var result = _financeRepository.GetAttachmentVoucher(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachmentVoucher_TypeTRSTL()
        {
            //Arrange
            string voucherId = "20";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);
            _dapper.Setup(repo => repo.Get<FinanceVoucherHeaderResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("travel settlement"));

            //Act
            var result = _financeRepository.GetAttachmentVoucher(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachmentVoucher_TypeINVTR()
        {
            //Arrange
            string voucherId = "20";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);
            _dapper.Setup(repo => repo.Get<FinanceVoucherHeaderResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("invoice travel"));

            //Act
            var result = _financeRepository.GetAttachmentVoucher(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetAttachmentVoucher_TypeNON()
        {
            //Arrange
            string voucherId = "20";
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment);
            _dapper.Setup(repo => repo.Get<FinanceVoucherHeaderResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("non shopping cart"));

            //Act
            var result = _financeRepository.GetAttachmentVoucher(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void UpdateReimbursementDetailOtherCost()
        {
            //Arrange
            int reimbursementDetailId = 1;
            var param = FinanceMockData.CreateFinanceOtherCost();
            _dapper.Setup(repo => repo.Update<FinanceOtherCost>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(param);
            _dapper.Setup(repo => repo.Update<int>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(1);

            //Act
            var result = _financeRepository.UpdateOtherCostReimbursementDetail(reimbursementDetailId, param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void UpdateSettlementDetailOtherCost()
        {
            //Arrange
            int settlementDetailId = 1;
            var param = FinanceMockData.CreateFinanceOtherCost();
            _dapper.Setup(repo => repo.Update<FinanceOtherCost>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(param);

            //Act
            var result = _financeRepository.UpdateSettlementDetailOtherCost(settlementDetailId, param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void SubmitTransactionMaker()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceRequest(1, "cash advance");
            _dapper.Setup(repo => repo.Update<int>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(200);

            //Act
            var result = _financeRepository.SubmitTransactionMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RejectTransactionMaker_TypeCA()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceRequest(3, "cash advance");
            _dapper.Setup(repo => repo.Get<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.Get<decimal>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(200);
            _dapper.Setup(repo => repo.Get<Workflow>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateWorkflow);
            _dapper.Setup(repo => repo.Update<ReimbursementModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateReimbursementModel());
            _dapper.Setup(repo => repo.Update<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment());
            _dapper.Setup(repo => repo.Execute(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text));

            _submissionApprovalRepository.Setup(repo => repo.SetBodyEmailRI(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(FinanceMockData.CreateParamEmailReimbursement());

            //Act
            var result = _financeRepository.RejectTransactionMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RejectTransactionMaker_TypeTR()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceRequest(3, "travel");
            _dapper.Setup(repo => repo.Get<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.Get<decimal>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(200);
            _dapper.Setup(repo => repo.Get<Workflow>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateWorkflow);
            _dapper.Setup(repo => repo.Update<ReimbursementModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateReimbursementModel());
            _dapper.Setup(repo => repo.Update<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment());
            _dapper.Setup(repo => repo.Execute(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text));

            _submissionApprovalRepository.Setup(repo => repo.SetBodyEmailTR(It.IsAny<string>())).ReturnsAsync(FinanceMockData.CreateParamEmailTravel());

            //Act
            var result = _financeRepository.RejectTransactionMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RejectTransactionMaker_TypeTRSTL()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceRequest(3, "travel settlement");
            _dapper.Setup(repo => repo.Get<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.Get<decimal>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(200);
            _dapper.Setup(repo => repo.Get<Workflow>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateWorkflow);
            _dapper.Setup(repo => repo.Update<ReimbursementModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateReimbursementModel());
            _dapper.Setup(repo => repo.Update<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment());
            _dapper.Setup(repo => repo.Execute(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text));

            _submissionApprovalRepository.Setup(repo => repo.SetBodyEmailTRSTL(It.IsAny<string>())).ReturnsAsync(FinanceMockData.CreateParamEmailTravelSettlement());

            //Act
            var result = _financeRepository.RejectTransactionMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RejectTransactionMaker_TypeINVTR()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceRequest(3, "invoice travel");
            _dapper.Setup(repo => repo.Get<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.Get<decimal>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(200);
            _dapper.Setup(repo => repo.Get<Workflow>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateWorkflow);
            _dapper.Setup(repo => repo.Update<ReimbursementModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateReimbursementModel());
            _dapper.Setup(repo => repo.Update<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment());
            _dapper.Setup(repo => repo.Execute(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text));
            _dapper.Setup(repo => repo.Execute(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text));

            _submissionApprovalRepository.Setup(repo => repo.SetBodyEmailInvoiceTravel(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(FinanceMockData.CreateParamEmailReimbursement());

            //Act
            var result = _financeRepository.RejectTransactionMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RepairTransactionMaker_TypeCA()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceRequest(4, "cash advance");
            _dapper.Setup(repo => repo.Get<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.Get<decimal>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(200);
            _dapper.Setup(repo => repo.Get<Workflow>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateWorkflow);
            _dapper.Setup(repo => repo.Update<ReimbursementModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateReimbursementModel());
            _dapper.Setup(repo => repo.Update<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment());
            _dapper.Setup(repo => repo.Execute(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text));
            _submissionApprovalRepository.Setup(repo => repo.SetBodyEmailRI(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(FinanceMockData.CreateParamEmailReimbursement());

            //Act
            var result = _financeRepository.RepairTransactionMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RepairTransactionMaker_TypeTR()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceRequest(4, "travel");
            _dapper.Setup(repo => repo.Get<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.Get<decimal>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(200);
            _dapper.Setup(repo => repo.Get<Workflow>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateWorkflow);
            _dapper.Setup(repo => repo.Update<ReimbursementModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateReimbursementModel());
            _dapper.Setup(repo => repo.Update<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment());
            _dapper.Setup(repo => repo.Execute(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text));
            _submissionApprovalRepository.Setup(repo => repo.SetBodyEmailRI(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(FinanceMockData.CreateParamEmailReimbursement());

            //Act
            var result = _financeRepository.RepairTransactionMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RepairTransactionMaker_TypeTRSTL()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceRequest(4, "travel settlement");
            _dapper.Setup(repo => repo.Get<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.Get<decimal>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(200);
            _dapper.Setup(repo => repo.Get<Workflow>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateWorkflow);
            _dapper.Setup(repo => repo.Update<ReimbursementModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateReimbursementModel());
            _dapper.Setup(repo => repo.Update<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment());
            _dapper.Setup(repo => repo.Execute(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text));
            _submissionApprovalRepository.Setup(repo => repo.SetBodyEmailRI(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(FinanceMockData.CreateParamEmailReimbursement());

            //Act
            var result = _financeRepository.RepairTransactionMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RepairTransactionMaker_TypeINVTR()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceRequest(4, "invoice travel");
            _dapper.Setup(repo => repo.Get<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.Get<decimal>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(200);
            _dapper.Setup(repo => repo.Get<Workflow>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateWorkflow);
            _dapper.Setup(repo => repo.Update<ReimbursementModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateReimbursementModel());
            _dapper.Setup(repo => repo.Update<ApprovalRequest>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateApprovalRequest());
            _dapper.Setup(repo => repo.GetAll<AttachmentModel>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetAttachment());
            _dapper.Setup(repo => repo.Execute(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text));
            _submissionApprovalRepository.Setup(repo => repo.SetBodyEmailRI(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(FinanceMockData.CreateParamEmailReimbursement());

            //Act
            var result = _financeRepository.RepairTransactionMaker(param);

            //Assert
            Assert.NotNull(result);
        }


        [Fact]
        public void GetVoucherList_TypeRI()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetVoucherList("RI");
            _dapper.Setup(repo => repo.GetAll<FinanceVoucherHeaderResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherList());

            //Act
            var result = _financeRepository.GetVoucherList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVoucherList_TypeTR()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetVoucherList("TR");
            _dapper.Setup(repo => repo.GetAll<FinanceVoucherHeaderResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherList());

            //Act
            var result = _financeRepository.GetVoucherList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVoucherList_TypeSC()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetVoucherList("SC");
            _dapper.Setup(repo => repo.GetAll<FinanceVoucherHeaderResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherList());

            //Act
            var result = _financeRepository.GetVoucherList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVoucherList_TypeCA()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetVoucherList("CA");
            _dapper.Setup(repo => repo.GetAll<FinanceVoucherHeaderResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherList());

            //Act
            var result = _financeRepository.GetVoucherList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetUpdatePayment()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetUpdatePaymentList();
            _dapper.Setup(repo => repo.Get<FinanceUpdatePaymentResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetUpdatePayment);

            //Act
            var result = _financeRepository.GetUpdatePayment(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetUpdatePaymentList()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetUpdatePaymentList();
            _dapper.Setup(repo => repo.GetAll<FinanceUpdatePaymentResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateListFinanceUpdatePaymentResponse());

            //Act
            var result = _financeRepository.GetUpdatePaymentList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void UpdatePayment()
        {
            //Arrange
            var param = FinanceMockData.CreateParamSubmitUpdatePaymentList();
            var token = new CancellationToken();
            _dapper.Setup(repo => repo.GetAll<FinanceUpdatePaymentResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateListFinanceUpdatePaymentResponse());
            _dapper.Setup(repo => repo.Get<int>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(1);
            _dapper.Setup(repo => repo.Update<FinanceUpdatePaymentResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetUpdatePayment);

            //Act
            var result = _financeRepository.UpdatePayment(param, token);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ActionUpdateBudgetTransactionOnPayment_InsertBudget_OnNull()
        {
            //Arrange
            var financeUpdatePayments = FinanceMockData.CreateListFinanceUpdatePaymentResponse();
            string username = "";
            int? lastStatusTransfer = null;
            string reqStatusTransfer = "0";
            _budgetTransactionRepository.Setup(repo => repo.CheckBudgetTransaction(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(FinanceMockData.CreateListBudgetTransactionModel());
            _budgetTransactionRepository.Setup(repo => repo.InsertBudgetTransactionByRequestType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));

            //Act
            var result = _financeRepository.ActionUpdateBudgetTransactionOnPayment(financeUpdatePayments, username, lastStatusTransfer, reqStatusTransfer);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ActionUpdateBudgetTransactionOnPayment_InsertBudget_OnSuccess()
        {
            //Arrange
            var financeUpdatePayments = FinanceMockData.CreateListFinanceUpdatePaymentResponse();
            string username = "";
            int? lastStatusTransfer = 1;
            string reqStatusTransfer = "0";
            _budgetTransactionRepository.Setup(repo => repo.CheckBudgetTransaction(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(FinanceMockData.CreateListBudgetTransactionModel());
            _budgetTransactionRepository.Setup(repo => repo.InsertBudgetTransactionByRequestType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));

            //Act
            var result = _financeRepository.ActionUpdateBudgetTransactionOnPayment(financeUpdatePayments, username, lastStatusTransfer, reqStatusTransfer);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ActionUpdateBudgetTransactionOnPayment_InsertBudget_OnFail()
        {
            //Arrange
            var financeUpdatePayments = FinanceMockData.CreateListFinanceUpdatePaymentResponse();
            string username = "";
            int? lastStatusTransfer = 0;
            string reqStatusTransfer = "1";
            _budgetTransactionRepository.Setup(repo => repo.CheckBudgetTransaction(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(FinanceMockData.CreateListBudgetTransactionModel());
            _budgetTransactionRepository.Setup(repo => repo.InsertBudgetTransactionByRequestType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));

            //Act
            var result = _financeRepository.ActionUpdateBudgetTransactionOnPayment(financeUpdatePayments, username, lastStatusTransfer, reqStatusTransfer);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVoucherByRequestNumber_CategoryTR()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceVoucherRequest("TR");
            _dapper.Setup(repo => repo.GetAll<FinanceVoucherDetail>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherByRequestNumber());

            //Act
            var result = _financeRepository.GetVoucherByRequestNumber(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVoucherByRequestNumber_CategorySC()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceVoucherRequest("SC");
            _dapper.Setup(repo => repo.GetAll<FinanceVoucherDetail>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherByRequestNumber());

            //Act
            var result = _financeRepository.GetVoucherByRequestNumber(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVoucherDetailList_TypeShoppingCart()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetVoucherList("Shopping Cart");
            _dapper.Setup(repo => repo.GetAll<FinanceVoucherDetailResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherDetailList());

            //Act
            var result = _financeRepository.GetVoucherDetailList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVoucherDetailList_TypeReimbursement()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetVoucherList("Reimbursement");
            _dapper.Setup(repo => repo.GetAll<FinanceVoucherDetailResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherDetailList());

            //Act
            var result = _financeRepository.GetVoucherDetailList(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVoucherDetail()
        {
            //Arrange
            int voucherId = 1;
            _dapper.Setup(repo => repo.Get<FinanceVoucherDetail>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherDetail());

            //Act
            var result = _financeRepository.GetVoucherDetail(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetSumAmountVoucher()
        {
            //Arrange
            int voucherId = 1;
            _dapper.Setup(repo => repo.Get<decimal>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(100);

            //Act
            var result = _financeRepository.GetSumAmountVoucher(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void SubmitVoucherMaker_TypeRI()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceVoucherRequest("RI");
            _dapper.Setup(repo => repo.Get<string>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns("100");
            _dapper.Setup(repo => repo.Insert<FinanceVoucherHeader>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("RI"));
            _externalServices.Setup(repo => repo.GetAccountDetail(It.IsAny<int>())).ReturnsAsync(FinanceMockData.CreateAccountResponse);

            //Act
            var result = _financeRepository.SubmitVoucherMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void SubmitVoucherMaker_TypeSC()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceVoucherRequest("SC");
            _dapper.Setup(repo => repo.Get<string>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns("100");
            _dapper.Setup(repo => repo.Insert<FinanceVoucherHeader>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("SC"));
            _externalServices.Setup(repo => repo.GetAccountDetail(It.IsAny<int>())).ReturnsAsync(FinanceMockData.CreateAccountResponse);

            //Act
            var result = _financeRepository.SubmitVoucherMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void SubmitVoucherMaker_TypeTRSTL()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceVoucherRequest("TRSTL");
            _dapper.Setup(repo => repo.Get<string>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns("100");
            _dapper.Setup(repo => repo.Insert<FinanceVoucherHeader>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("SC"));
            _externalServices.Setup(repo => repo.GetAccountDetail(It.IsAny<int>())).ReturnsAsync(FinanceMockData.CreateAccountResponse);

            //Act
            var result = _financeRepository.SubmitVoucherMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void SubmitVoucherMaker_TypeINVTR()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceVoucherRequest("INVTR");
            _dapper.Setup(repo => repo.Get<string>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns("100");
            _dapper.Setup(repo => repo.Insert<FinanceVoucherHeader>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("SC"));
            _externalServices.Setup(repo => repo.GetAccountDetail(It.IsAny<int>())).ReturnsAsync(FinanceMockData.CreateAccountResponse);

            //Act
            var result = _financeRepository.SubmitVoucherMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void SubmitVoucherMaker_TypeNON()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceVoucherRequest("NON");
            _dapper.Setup(repo => repo.Get<string>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns("100");
            _dapper.Setup(repo => repo.Insert<FinanceVoucherHeader>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("SC"));
            _externalServices.Setup(repo => repo.GetAccountDetail(It.IsAny<int>())).ReturnsAsync(FinanceMockData.CreateAccountResponse);

            //Act
            var result = _financeRepository.SubmitVoucherMaker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void SubmitVoucherChecker()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceVoucherRequest("");
            _dapper.Setup(repo => repo.Insert<FinanceVoucherHeader>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("SC"));

            //Act
            var result = _financeRepository.SubmitVoucherChecker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void SwitchChecker()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceVoucherRequest("");
            _dapper.Setup(repo => repo.Insert<FinanceVoucherHeader>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("SC"));

            //Act
            var result = _financeRepository.SwitchChecker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void TakeOutVoucherChecker()
        {
            //Arrange
            var param = FinanceMockData.CreateFinanceVoucherRequest("");
            _dapper.Setup(repo => repo.Insert<FinanceVoucherHeader>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("SC"));

            //Act
            var result = _financeRepository.TakeOutVoucherChecker(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RevertVoucher()
        {
            //Arrange
            var param = FinanceMockData.CreateParamGetVoucherList("");
            _dapper.Setup(repo => repo.Update<FinanceVoucherHeader>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherHeader("SC"));

            //Act
            var result = _financeRepository.RevertVoucher(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetReportMcmHeader()
        {
            //Arrange
            int voucherId = 1;
            _dapper.Setup(repo => repo.GetAll<ReportMcmHeader>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateListReportMcmHeader());

            //Act
            var result = _financeRepository.GetReportMcmHeader(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetReportMcmDetail()
        {
            //Arrange
            int voucherId = 1;
            _dapper.Setup(repo => repo.GetAll<ReportMcmDetail>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateListReportMcmDetail());
            _externalServices.Setup(repo => repo.GetRecipientBank()).ReturnsAsync(FinanceMockData.CreateListRecipientBankResponse());

            //Act
            var result = _financeRepository.GetReportMcmDetail(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetReportBsm()
        {
            //Arrange
            int voucherId = 1;
            _dapper.Setup(repo => repo.GetAll<ReportBsm>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateListReportBsm());
            _externalServices.Setup(repo => repo.GetRecipientBank()).ReturnsAsync(FinanceMockData.CreateListRecipientBankResponse());

            //Act
            var result = _financeRepository.GetReportBsm(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetReportCsvVoucher()
        {
            //Arrange
            int voucherId = 1;
            _dapper.Setup(repo => repo.GetAll<ReportBsm>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateListReportBsm());
            _externalServices.Setup(repo => repo.GetRecipientBank()).ReturnsAsync(FinanceMockData.CreateListRecipientBankResponse());

            //Act
            var result = _financeRepository.GetReportCsvVoucher(voucherId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GenerateInvoices()
        {
            //Arrange
            string RequestNumber = string.Empty;
            int InvoiceId = 0;
            string Category = string.Empty;
            _dapper.Setup(repo => repo.Get<GenerateInvoice>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateJsonGenereateInvoiceResponse());

            //Act
            var result = _financeRepository.GenerateInvoices(RequestNumber, InvoiceId, Category);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GenerateInvoiceGetAttahmentPR()
        {
            //Arrange
            int RequestDetailId = 0;
            string Category = string.Empty;
            _dapper.Setup(repo => repo.Get<AttachmentResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateJsonListAttachemnt());

            //Act
            var result = _financeRepository.InvoiceAttachment(RequestDetailId, Category);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GenerateInvoiceSummary()
        {
            //Arrange

            var param = FinanceMockData.CreateGenerateInvoiceSummaryRequest();
            _dapper.Setup(repo => repo.Get<JsonResponse>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateJsonSummaryResponse());

            //Act
            var result = _financeRepository.InvoiceSummary(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GenerateInvoiceSaves()
        {
            //Arrange
            var param = FinanceMockData.CreateGenerateInvoiceSave();
            _dapper.Setup(repo => repo.Insert<ResponseData>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateGenerateInvoiceResponse());
            _dapper.Setup(repo => repo.Get<ResponseData>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateGenerateInvoiceResponse());

            //Act
            var result = _financeRepository.GenerateInvoiceSaves(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RepairToInvoiceManagement()
        {
            //Arrange
            var param = FinanceMockData.CreateRepairBody();
            _dapper.Setup(repo => repo.Update<InvoiceRepair>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateRepairHeader());

            //Act
            var result = _financeRepository.RepairToInvoiceManagement(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RepairToDeliveryNote()
        {
            //Arrange
            var param = FinanceMockData.CreateRepairBody();
            _dapper.Setup(repo => repo.Update<InvoiceRepair>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateRepairHeader());
            //Act
            var result = _financeRepository.RepairToDeliveryNote(param);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetStatusByPoNumber()
        {
            //Arrange
            string poNumber = "";
            _dapper.Setup(repo => repo.Get<JsonPopulateShoppingCart>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateJsonPopulateShoppingCart());

            //Act
            var result = _financeRepository.GetStatusByPoNumber(poNumber);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVoucherDetailByVoucherRefId()
        {
            //Arrange
            string voucherRefId = "";
            _dapper.Setup(repo => repo.Get<FinanceVoucherDetail>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.GetVoucherDetail());

            //Act
            var result = _financeRepository.GetVoucherDetailByVoucherRefId(voucherRefId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetTravelExpenseDetail()
        {
            //Arrange
            int travelExpenseId = 0;
            _dapper.Setup(repo => repo.Get<TravelRequestExpenseDetail>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateTravelRequestExpenseDetail());

            //Act
            var result = _financeRepository.GetTravelExpenseDetail(travelExpenseId);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void UpdateTravelExpenseDetail()
        {
            //Arrange
            var param = FinanceMockData.CreateTravelRequestExpenseDetail();
            _dapper.Setup(repo => repo.Update<TravelRequestExpenseDetail>(It.IsAny<string>(), It.IsAny<Dapper.DynamicParameters>(), CommandType.Text)).Returns(FinanceMockData.CreateTravelRequestExpenseDetail());

            //Act
            var result = _financeRepository.UpdateTravelExpenseDetail(param);

            //Assert
            Assert.NotNull(result);
        }
    }
}
