@{
    Layout = "~/Views/Shared/_Layout.cshtml";
    ViewBag.Title = "Create New PRF";
}

<style>
    .borderless td, .borderless th {
        border: none !important;
    }

    .box-primary {
        background: #FFFFFF;
        box-shadow: 0px 0px 4px rgb(0 0 0 / 20%);
        border-radius: 5px;
        padding: 15px 15px 15px 15px;
        width: 100%;
    }

    .bar-progress {
        border: 1px solid grey;
        height: 8px;
        background-color: blue;
        max-width: 398px;
        border-radius: 7px;
        padding: 5px 10px 6px;
        margin-bottom: -10px;
    }

    .bar-progress-non-active {
        border: 1px solid grey;
        height: 8px;
        background-color: grey;
        max-width: 398px;
        border-radius: 7px;
        padding: 5px 10px 6px;
        margin-bottom: -10px;
    }

    .border-bottom {
        border-bottom: solid;
        border-color: #999;
        border-width: initial;
        vertical-align: text-bottom;
    }

    .table.table-borderless td.content-bottom {
        vertical-align: bottom;
        border: none;
    }


    .switch {
        position: relative;
        display: inline-block;
        width: 53px;
        height: 26px;
    }

        .switch input {
            opacity: 0;
            width: 0;
            height: 0;
        }

    .slider {
        position: absolute;
        cursor: pointer;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background-color: #ccc;
        -webkit-transition: .4s;
        transition: .4s;
    }

        .slider:before {
            position: absolute;
            content: "";
            height: 19px;
            width: 19px;
            left: 4px;
            bottom: 4px;
            background-color: white;
            -webkit-transition: .4s;
            transition: .4s;
        }

    input:checked + .slider {
        background-color: #2196F3;
    }

    input:focus + .slider {
        box-shadow: 0 0 1px #2196F3;
    }

    input:checked + .slider:before {
        -webkit-transform: translateX(26px);
        -ms-transform: translateX(26px);
        transform: translateX(26px);
    }

    /* Rounded sliders */
    .slider.round {
        border-radius: 34px;
    }

        .slider.round:before {
            border-radius: 50%;
        }

    .blink {
        animation: blink 1s infinite;
        color: red;
        font-size: 20px;
        float: right;
    }

    @@keyframes blink {
        0% {
            opacity: 1;
        }

        50% {
            opacity: 0;
        }

        100% {
            opacity: 1;
        }
    }

    span.select2-container--focus {
        border-color: #66afe9;
        outline: 0;
        -webkit-box-shadow: inset 0 1px 1px rgba(0,0,0,0.075),0 0 8px rgba(102,175,233,0.6);
        box-shadow: inset 0 1px 1px rgba(0,0,0,0.075),0 0 8px rgba(102,175,233,0.6)
    }

</style>

<div id="page-wrapper">
    <div class="row">
        <div class="col-lg-12">
            <h3 style="margin-top: 2%;"><b style="color: #7099C9">Purchase Requisition</b></h3><hr />
            <nav aria-label="breadcrumb">
                <ol class="breadcrumb">
                    <li class="breadcrumb-item"><a href="~/Home/Index">Home</a></li>
                    <li class="breadcrumb-item active" aria-current="page">@ViewBag.Title</li>
                </ol>
            </nav>
        </div>
        <div class="col-lg-12">
            <div class="row form-group">
                <table class="table borderless">
                    <tr>
                        <td colspan="4">
                            <label class="text-muted"><span id="spanStepsProgress">1</span> of 4 steps</label>
                        </td>
                    </tr>
                    <tr>
                        <td width="25%">
                            <div class="div-progress bar-progress" id="progress1"></div>
                        </td>
                        <td width="25%">
                            <div class="div-progress bar-progress-non-active" id="progress2"></div>
                        </td>
                        <td width="25%">
                            <div class="div-progress bar-progress-non-active" id="progress3"></div>
                        </td>
                        <td width="25%">
                            <div class="div-progress bar-progress-non-active" id="progress4"></div>
                        </td>
                    </tr>
                    <tr>
                        <td>PRF & URS</td>
                        <td>Risk Assesment</td>
                        <td>Upload Document</td>
                        <td>Choose Approver</td>
                    </tr>
                </table>
            </div>
            <div class="row form-group">
                <div class="box-primary">
                    <div class="form-group">
                        <span class="text-danger">
                            Harap diisi lengkap untuk kelancaran proses
                        </span>
                    </div>
                    <div class="form-group" style="margin-bottom: 3%;">
                        <table class="table borderless">
                            <thead>
                                <tr>
                                    <td>
                                        <h4 class="tittle-content-partial" style="color: #425CAA;">
                                            Lengkapi PRF
                                        </h4>
                                    </td>
                                    <td>
                                        <label class="blink" id="labelBudget" hidden>Non Budget</label>
                                    </td>
                                </tr>
                            </thead>
                        </table>
                    </div>
                    <div class="form-group div-content-partial">
                        <div id="step1" class="tab-pane fade in active">
                            @await Html.PartialAsync("/Views/NonShoppingCart/PRF/CreateRequest/_CreateStep1.cshtml")
                        </div>
                        <div id="step2" class="tab-pane fade" hidden>
                            @await Html.PartialAsync("/Views/NonShoppingCart/PRF/CreateRequest/_CreateStep2.cshtml")
                        </div>
                        <div id="step3" class="tab-pane fade" hidden>
                            @await Html.PartialAsync("/Views/NonShoppingCart/PRF/CreateRequest/_CreateStep3.cshtml")
                        </div>
                        <div id="step4" class="tab-pane fade" hidden>
                            @await Html.PartialAsync("/Views/NonShoppingCart/PRF/CreateRequest/_CreateStep4.cshtml")
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <span class="span-prf-no" hidden></span>
    <span class="span-prf-id" hidden></span>
</div>

<script>
    $(document).ready(function () {
        DatepickerChanger();
        SetRequisitionerDetails();
        SetOptionBudgetCode(@Html.Raw(Json.Serialize(ViewBag.SelectListCoa)));
        setTimeout(function () { $('#selectBusinessUnit').focus() }, (2 * 1000));
    });

    function ChangeBarProgress(newStep) {
        let divPartialActiveNow = $('.div-content-partial').find('.active');
        let divProgressBarActiveNow = $('.bar-progress');
        let divPartialActiveNext = $(`#${newStep}`);
        let divProgressBarActiveNext;
        let tittleContent;

        if (newStep == "step1") {
            divProgressBarActiveNext = $('#progress1');
            tittleContent = 'Lengkapi PRF';
        } else if (newStep == "step2") {
            divProgressBarActiveNext = $('#progress2');
            tittleContent = 'Risk Assesment of Requested Service/Good';
        } else if (newStep == "step3") {
            divProgressBarActiveNext = $('#progress3');
            tittleContent = 'Upload Document';
        } else if (newStep == "step4") {
            divProgressBarActiveNext = $('#progress4');
            tittleContent = 'Choose Approver';
          
            let budgets = GetBudgetInfoCheckCoa('prf', $('.span-prf-no').text(), null);
            let ccName = $('#selectCostCenter').find('option:selected').text();
            let buName = $('#selectBudgetCode option:selected').text();
            if (budgets.length == 0) {

                $('.blink').attr('hidden', false);
                $("#IsBudget").text(0);
                $('#btnContinueStep1').show();
                $('#IsBudgetUpdate').text(false);
                return false;
            }

            if (budgets[0].budgetId == 0) {
                Swal.fire(
                    'Data Not Complete!',
                    `${ccName} tidak terdaftar atas ${buName}. Proses tidak dapat di lanjut mohon daftarkan terlebih dahulu.`,
                    'warning'
                );
                $('#btnContinueStep1').hide();
                return false;
            }
            else if (!budgets[0].isBudget) {
                $('.blink').attr('hidden', false);
                $("#IsBudget").text(0);
                $('#IsBudgetUpdate').text(false);
                $('#btnContinueStep1').show();
            } else {
                $('.blink').attr('hidden', true);
                $("#IsBudget").text(1);
                $('#btnContinueStep1').show();
                $('#IsBudgetUpdate').text(true);
            }

            GetApprovalMatrixPrf();
            $('#btnValidate').show();
            $('#btnSaveStep4').hide();

        }

        divProgressBarActiveNow.removeClass('bar-progress').addClass('bar-progress-non-active');
        divProgressBarActiveNext.removeClass('bar-progress-non-active').addClass('bar-progress');
        divPartialActiveNow.prop('hidden', true);
        divPartialActiveNext.prop('hidden', false);
        $('.tittle-content-partial').text(tittleContent);

        if (newStep == "step1") {
            $('#selectBusinessUnit').focus();
        } else if (newStep == "step2") {
            $('#selectQuetion1').focus();
        } else if (newStep == "step3") {
            $('#inputUploadAttachmentPRF').focus();
        } else if (newStep == "step4") {
            $('#selectUserNameApprover').focus();
        }

        // $('html, body').animate({ scrollTop: $('#page-wrapper').offset().top }, 'slow');
        window.scrollTo(0, 0);

    }

    function DatepickerChanger() {
        $('.datepicker').datepicker({
            format: 'yyyy/mm/dd',
            todayHighlight: true,
            autoclose: true,
            orientation: 'bottom'
        });
    };

</script>



createstep1 di bawah ini




<h5 style="color: #425CAA;">
    Requisitioner Details
</h5>

<table class="table borderless text-primary">
    <tr>
        <td>
            Name :
            <span class="spanRequesterDetailUsername"></span>
        </td>
        <td>
            Business Unit :
            <span class="spanRequesterDetailBusinessUnit"></span>
        </td>
        <td>
            Department :
            <span class="spanRequesterDetailDepartment"></span>
        </td>
        <td>
            Request Date :
            <span class="spanRequesterDetailRequestDate"></span>
        </td>
    </tr>
</table>

<hr />

<table class="table borderless">
    <tr>
        <td width="25%">
            <label class="text-muted" for="selectBusinessUnit">Business Unit<em style="color: red;">*</em></label>
            <select class="form-control select2" id="selectBusinessUnit" asp-items="ViewBag.SelectListBusinessUnit" onchange="GetListOptionCostCenter(this); SetSpanBusinessUnitAndCostCenterApproval();">
                <option selected disabled></option>
            </select>
        </td>
        <td width="25%">
            <label class="text-muted" for="selectCostCenter">Cost Center<em style="color: red;">*</em></label>
            <select class="form-control select2" id="selectCostCenter" disabled>
                <option selected disabled></option>
            </select>
        </td>
        <td width="20%" hidden>
            <label class="text-muted">Is this request a budgeted spend?</label>
            <div>
                <em>No</em>
                <label class="switch">
                    <input type="checkbox" id="radioBudgetedSpend">
                    <span class="slider round"></span>
                </label>
                <em>Yes</em>
            </div>
        </td>
        <td>
            <label class="text-muted" for="selectCurrency">Currency<em style="color: red;">*</em></label>
            <select class="form-control select2" id="selectCurrency" asp-items="ViewBag.SelectListCurrency">
                <option selected disabled></option>
            </select>
        </td>
    </tr>
    <tr>
        <td colspan="2">
            <label class="text-muted" for="selectBudgetCode">Budget Code<em style="color: red;">*</em></label>
            <select class="form-control select2" id="selectBudgetCode">
                <option selected disabled></option>
            </select>
        </td>
        <td colspan="2">
            <label class="text-muted" for="inputEstimatedTotalBudget">Estimated total budget for this request<em style="color: red;">*</em></label>
            <input type="text" class="form-control" id="inputEstimatedTotalBudget" onkeyup="FormatInputMoney(this, null)" />
        </td>
    </tr>
</table>

<hr />

<h5 style="color: #425CAA;">
    Request Details
</h5>

<table class="table borderless">
    <tr>
        <td width="50%">
            <label class="text-muted">Type of Request<em style="color: red;">*</em></label>
            <div>
                <label class="radio-inline"><input type="radio" value="Goods" name="radioTypeRequest">Goods</label>
                <label class="radio-inline"><input type="radio" value="Services" name="radioTypeRequest">Services</label>
            </div>
        </td>
        <td>
            <label class="text-muted">Has the product / service been purchased previously?</label>
            <div>
                <em>No</em>
                <label class="switch">
                    <input type="checkbox" id="radioPurchasedPreviously">
                    <span class="slider round"></span>
                </label>
                <em>Yes</em>
            </div>
        </td>
    </tr>
    <tr>
        <td>
            <label class="text-muted" for="selectSpendingByCategory">Spending by Category<em style="color: red;">*</em></label>
            <select class="form-control select2" id="selectSpendingByCategory" asp-items="ViewBag.SelectListSpendingCategory" onchange="GetSelectListSpendingSubCategory(this);">
                <option selected disabled></option>
            </select>
        </td>
        <td rowspan="2">
            <label class="text-muted" for="textareaPreviousPurchaseYes">
                If yes, please provide details of previous purchase (e.g. Purchase Order no., Purchase date, Quantity, Supplier etc.)
            </label>
            <textarea class="form-control" id="textareaPreviousPurchaseYes" style="height: 70px;"></textarea>
        </td>
    </tr>
    <tr>
        <td>
            <label class="text-muted" for="selectSubCategory">Sub Category<em style="color: red;">*</em></label>
            <select class="form-control select2" id="selectSubCategory" onchange="SetTypeOfTransaction(this);" disabled>
                <option selected disabled></option>
            </select>
        </td>

    </tr>
    <tr>
        <td>
            <label class="text-muted" for="inputTypeTransaction">Type of Transaction<em style="color: red;">*</em></label>
            <input type="text" class="form-control" id="inputTypeTransaction" disabled />
        </td>
        <td rowspan="2">
            <label class="text-muted" for="textareaRecurrentPurchase">
                If recurrent purchase, please provide estimated annual volume and indenting frequency
            </label>
            <textarea class="form-control" id="textareaRecurrentPurchase" style="height: 70px;"></textarea>
        </td>
    </tr>
    <tr>
        <td id="tdProjectCode" style="display: none;">
            <label class="text-muted" for="selectProjectCode">Project Code<em style="color: red;">*</em></label>
            <select class="form-control select2" id="selectProjectCode" asp-items="ViewBag.SelectListProjectCode" disabled>
                <option selected disabled></option>
            </select>
        </td>
    </tr>
</table>

<hr />

<h5 style="color: #425CAA;">
    Item Request Details
</h5>

<div class="box-primary">
    <table class="table borderless table-input-item">
        <tr>
            <td>
                <label class="text-muted" for="requestItemName">Item Name<em style="color: red;">*</em></label>
                <input type="text" class="form-control" id="requestItemName" maxlength="250"/>
            </td>
            <td>
                <label class="text-muted" for="inputNameItem">Detailed of Goods Specification / Scope of Work Requested<em style="color: red;">*</em></label>
                <input type="text" class="form-control" id="inputNameItem" maxlength="250" />
            </td>
            <td>
                <label class="text-muted" for="inputQuantityItem">Quantity<em style="color: red;">*</em></label>
                <input type="number" class="form-control" id="inputQuantityItem" min="1" />
            </td>
            <td>
                <label class="text-muted" for="selectUnitItem">UOM (unit of measure)<em style="color: red;">*</em></label>
                <select class="form-control select2" id="selectUnitItem" asp-items="ViewBag.SelectListUnit">
                    <option selected disabled></option>
                </select>
            </td>
            <td>
                <label class="text-muted" for="selectTypeOfGoods">Type Of Goods<em style="color: red;">*</em></label>
                <select class="form-control select2" id="selectTypeOfGoods" asp-items="ViewBag.SelectListTypeOfGoods">
                    <option selected disabled></option>
                </select>
            </td>
        </tr>
        <tr>
            <td colspan="2">
                <label class="text-muted" for="inputDeliveryDateItem">Estimated Delivery Date / Period of Services<em style="color: red;">*</em></label>
                <input type="text" class="form-control datepicker" id="inputDeliveryDateItem" placeholder="yyyy/mm/dd" />
            </td>
            <td colspan="3">
                <label class="text-muted" for="textareaDeliveryRequirementItem">Delivery Requirements (Product packaging, Delivery frequency, Delivery method and etc.)<em style="color: red;">*</em></label>
                <textarea class="form-control" id="textareaDeliveryRequirementItem" maxlength="250"></textarea>
            </td>
        </tr>
        <tr>
            <td class="text-right" colspan="5">
                <button class="btn btn-primary" type="button" id="btnClearInputItem" onclick="ClearInputItem();">Clear</button>
                <button class="btn btn-primary" type="button" id="btnAddUnitItem" onclick="AddUnitItem();">Add</button>
            </td>
        </tr>
    </table>

    <table class="table table-bordered table-striped cost-center">
        <thead>
            <tr>
                <th>No.</th>
                <th>Item Name</th>
                <th>Detailed of Goods Specification / Scope of Work Requested</th>
                <th>Type Of Goods</th>
                <th>Quantity</th>
                <th>Unit</th>
                <th>Estimated Delivery Date / Period of Services</th>
                <th>Delivery Requirements (Product packaging, Delivery frequency, Delivery method and etc.)</th>
                <th>Action</th>
            </tr>
        </thead>
        <tbody id="tbodyListItem">
        </tbody>
    </table>
</div>

<table class="table borderless">
    <tr>
        <td>
            <label class="text-muted" for="inputAdditionalRequest">
                Any additional request requirement / instructions need to be shared with supplier:
            </label>
            <input type="text" class="form-control" id="inputAdditionalRequest" />
        </td>
        <td>
            <label class="text-muted" for="inputRequiredWarranty">
                Required warranty, support and Service Level Agreements (SLAs) from supplier:
            </label>&nbsp;&nbsp;&nbsp;
            <em>No</em>
            <label class="switch">
                <input type="checkbox" id="radioRequiredWarranty">
                <span class="slider round"></span>
            </label>
            <em>Yes</em>

            <input type="text" class="form-control" id="inputRequiredWarranty" />
        </td>
    </tr>
    <tr>
        <td>
            <label class="text-muted" for="inputRecommendedPenalty">
                Recommended penalty for non-compliance by supplier (if applicable):
            </label>&nbsp;&nbsp;&nbsp;
            <em>No</em>
            <label class="switch">
                <input type="checkbox" id="radioRecommendedPenalty">
                <span class="slider round"></span>
            </label>
            <em>Yes</em>

            <input type="text" class="form-control" id="inputRecommendedPenalty" />
        </td>
        <td></td>
    </tr>
</table>

<div style="width: 70%; background-color: #FEFFC0; margin-top: 30px; padding: 10px; ">
    Please attach relevant supporting documents (e.g. BOQ, drawing, photo etc.) with your purchase request submission.
    PRF received by Procurement before 14.00 will process at the same day, later than 14.00 will process a day after.
</div>

<div style="width: 70%; border: 0.675px solid black; margin-top: 15px; padding: 10px;">
    “I confirm that I have ensured that all goods or services requested (therefore all related reimbursement or payment) herein are for legitimate business purposes, comply with relevant AXA policies and under no circumstances form a payment for any bribe or facilitation payment of a government official”.
</div>

<div style="text-align: center; margin-top: 30px;">
    <a class="btn btn-default" id="btnBackStep1" href="@Url.Content("~/Home/Index")">Back</a>
    <button class="btn btn-primary" type="button" id="btnSaveStep1" onclick="SavePRF();">Save</button>
    <button class="btn btn-primary" type="button" id="btnUpdateStep1" onclick="UpdatePRF();" disabled style="display: none;">Update</button>
    <button class="btn btn-primary" type="button" id="btnContinueStep1" data-toggle="tab" href="#step2" onclick="ChangeBarProgress('step2');" style="display: none;">Continue</button>
    @*<button class="btn btn-primary" type="button" onclick="PRFSubmit();">Submit Testing</button>*@
    <label class="blink" id="labelBudget" hidden>Non Budget</label>
</div>

<label id="IsBudget" hidden>1</label>
<label id="IsBudgetUpdate" hidden>true</label>
<script src="~/Scripts/app/budget.js"></script>
<script>
    $(document).ready(function () {
        $('#inputDeliveryDateItem').datepicker({
            format: 'yyyy/mm/dd',
            autoclose: true,
            orientation: 'bottom',
            startDate: new Date(new Date().setDate(new Date().getDate() + 15)),
            todayHighlight: true
        });
    });

    function GetListOptionCostCenter(t) {
        let selectCostCenter = $('#selectCostCenter');
        let businessUnitId = parseInt($(t).find('option:selected').val());

        selectCostCenter.find('option').remove();

        if (!isNaN(businessUnitId)) {
            $.ajax({
                type: "GET",
                url: `${$baseurl}/CostCenter/GetSelectListItemCostCenter?businessUnitId=${businessUnitId}`,
                success: function (r) {
                    if (r.code == 200) {
                        selectCostCenter.prop('disabled', false);
                        selectCostCenter.append('<option selected disabled></option>');

                        r.data.forEach(function (data) { selectCostCenter.append(`<option value="${data.id}">${data.code + ' - ' + data.name}</option>`); });
                    } else {
                        Swal.fire('Failed!', r.data, 'warning');
                    }
                },
                error: function (r) {
                    Swal.fire('Failed!', r.responseJSON.data, 'error');
                }
            });
        } else {
            selectCostCenter.append('<option selected disabled></option>');
            selectCostCenter.prop('disabled', true);
        }
    }

    function ClearInputItem() {
        $('.table-input-item').find('input, select, textarea').val('');
        $("#selectUnitItem").val('').change();
        $("#selectTypeOfGoods").val('').change();
    }

    function AddUnitItem() {
        let noItem = parseInt($('#tbodyListItem tr').length) + 1;
        let requestItemName = $('#requestItemName').val();
        let nameItem = $('#inputNameItem').val();
        let quantityItem = parseInt($('#inputQuantityItem').val());
        let unitItem = $('#selectUnitItem option:selected').text();
        let typeOfGoods = $('#selectTypeOfGoods option:selected').text();
        let typeOfGoodsId = $('#selectTypeOfGoods option:selected').val();
        let deliveryDateItem = $('#inputDeliveryDateItem').val();
        let deliveryRequirementItem = $('#textareaDeliveryRequirementItem').val();

        let dateNow = moment().format('YYYY/MM/DD');
        let dateAfter14Days = moment().add(14, 'days').format('YYYY/MM/DD');
        if (nameItem != '' && nameItem != null &&
            requestItemName != '' && requestItemName != null &&
            !isNaN(quantityItem) && quantityItem != null &&
            unitItem != '' && unitItem != null &&
            typeOfGoods != '' && typeOfGoods != null &&
            deliveryDateItem != '' && deliveryDateItem != null &&
            deliveryRequirementItem != '' && deliveryRequirementItem != null) {
                
            if (!XssValidation(requestItemName)) {
                let messageMandatoryInput = '<b>Model State Invalid</b></br>';
                Swal.fire(
                    'Data Invalid!',
                    messageMandatoryInput,
                    'warning'
                )
                return false;
            }
            if (!XssValidation(nameItem)) {
                let messageMandatoryInput = '<b>Model State Invalid</b></br>';
                Swal.fire(
                    'Data Invalid!',
                    messageMandatoryInput,
                    'warning'
                )
                return false;
            }
            if (!XssValidation(deliveryRequirementItem)) {
                let messageMandatoryInput = '<b>Model State Invalid</b></br>';
                Swal.fire(
                    'Data Invalid!',
                    messageMandatoryInput,
                    'warning'
                )
                return false;
            }
            if ((deliveryDateItem) <= (dateNow)) {
                let messageMandatoryInput = '<b>Back dated is not allowed!!!</b><label style="color: red;">*</label></br>';
                Swal.fire(
                    'Data Not Complete!',
                    messageMandatoryInput,
                    'warning'
                )
                return false;
            }
            if ((deliveryDateItem) <= (dateAfter14Days)) {
                let messageMandatoryInput = '<b>Please select the date H+14 from the request date!!!</b><label style="color: red;">*</label></br>';
                Swal.fire(
                    'Data Not Complete!',
                    messageMandatoryInput,
                    'warning'
                )
                return false;
            }

            $('#tbodyListItem').append(`
            <tr>
            <td class="text-center"><span class="no-item">${noItem}</span></td>
            <td><span class="span-request-item-name">${requestItemName}</span></td>
            <td><span class="span-name-item">${nameItem}</span></td>
            <td><span hidden class="span-type-of-goods">${typeOfGoodsId}</span>${typeOfGoods}</td>
            <td class="text-center"><span class="span-qty-item">${quantityItem}</span></td>
            <td class="text-center"><span class="span-unit-item">${unitItem}</span></td>
            <td class="text-center"><span class="span-delivery-date-item">${deliveryDateItem}</span></td>
            <td><span class="span-delivery-requirement-item">${deliveryRequirementItem}<span></td>
            <td class="text-center"><button class="btn btn-sm btn-danger btn-delete-item" type="button" onclick="DeleteItem(this);"><span class="glyphicon glyphicon-trash"></span></button></td>
            </tr>
            `);

            ClearInputItem();
        } else {
            let messageMandatoryInput = '';

            if (requestItemName == '') {
                messageMandatoryInput += 'Please input <b>Item Name</b><label style="color: red;">*</label></br>';
            }

            if (nameItem == '') {
                messageMandatoryInput += 'Please input <b>Detailed of Goods Specification / Scope of Work Requested</b><label style="color: red;">*</label></br>';
            }

            if (isNaN(quantityItem)) {
                messageMandatoryInput += 'Please input <b>Quantity</b><label style="color: red;">*</label></br>';
            }

            if (unitItem == '') {
                messageMandatoryInput += 'Please input <b>UOM (unit of measure)</b><label style="color: red;">*</label></br>';
            }

            if (typeOfGoods == '') {
                messageMandatoryInput += 'Please input <b>Type Of Goods</b><label style="color: red;">*</label></br>';
            }

            if (deliveryDateItem == '') {
                messageMandatoryInput += 'Please input <b>Estimated Delivery Date / Period of Services</b><label style="color: red;">*</label></br>';
            }

            if (deliveryRequirementItem == '') {
                messageMandatoryInput += 'Please input <b>Delivery Requirements (Product packaging, Delivery frequency, Delivery method and etc.)</b><label style="color: red;">*</label></br>';
            }

            if (deliveryDateItem < dateNow) {
                messageMandatoryInput += "Cant Back adate";
            }

            Swal.fire(
                'Data Not Complete!',
                messageMandatoryInput,
                'warning'
            );
        }
        $('#requestItemName').focus();
    }

    function DeleteItem(t) {
        t = $(t);
        t.closest('tr').remove();

        $('#tbodyListItem').find('.no-item').each(function (index, spanNo) {
            $(spanNo).text(index + 1);
        });
    }

    function SetRequisitionerDetails() {
        let userDetail = @Html.Raw(Json.Serialize(ViewBag.AccountDetail));

        $('.spanRequesterDetailUsername').text(userDetail.username);
        $('.spanRequesterDetailBusinessUnit').text(userDetail.businessUnitName);
        $('.spanRequesterDetailDepartment').text(userDetail.costCenterName);
        $('.spanRequesterDetailRequestDate').text(`@DateTime.Now.ToString("dd MMMM yyyy")`);
    }

    function MinPickDatePlus10WorkingDay(typeOfTransaction) {
        typeOfTransaction = typeOfTransaction.toLowerCase();

        if (typeOfTransaction == "bau") {

            let minDate = new Date();
            let daysToAdd = 10;

            for (var i = 0; i <= 10; i++) {
                minDate.setDate(minDate.getDate() + 1);
                if (minDate.getDay() == 0 || minDate.getDay() == 6) {
                    daysToAdd++;
                }
            }

            minDate = new Date();
            minDate.setDate(minDate.getDate() + daysToAdd);

            $('#inputDeliveryDateItem').datepicker({
                format: 'yyyy/mm/dd',
                autoclose: true,
                orientation: 'bottom',
                minDate: minDate
            });
        }
    }

    function HiddenProjectCode(typeOfTransaction) {
        typeOfTransaction = typeOfTransaction.toLowerCase();

        if (typeOfTransaction == 'project') {
            $('#selectProjectCode').show().prop('disabled', false);
            $('#tdProjectCode').show();
        } else {
            $('#selectProjectCode').hide().prop('disabled', true);
            $('#tdProjectCode').hide();
        }
    }

    function GetSelectListSpendingSubCategory(t) {
        let selectSubCategory = $('#selectSubCategory');
        let spendingCategoryId = parseInt($(t).find('option:selected').val());

        selectSubCategory.find('option').remove();

        if (!isNaN(spendingCategoryId)) {
            $.ajax({
                type: "GET",
                url: `${$baseurl}/PRF/GetSelectListItemSpendingSubCategory?spendingCategoryId=${spendingCategoryId}`,
                success: function (r) {
                    if (r.code == 200) {
                        selectSubCategory.prop('disabled', false);
                        selectSubCategory.append('<option selected disabled></option>');

                        r.data.forEach(function (data) {
                            selectSubCategory.append(`
                            <option value="${data.spendingSubCategoryId}" data-TOT="${data.typeOfTransaction}">${data.subCategory}</option>
                            `);
                        });
                    } else {
                        Swal.fire('Failed!', r.data, 'warning');
                    }
                },
                error: function (r) { Swal.fire('Failed!', r.responseJSON.data, 'error'); }
            });
        } else {
            selectSubCategory.append('<option selected disabled></option>');
            selectSubCategory.prop('disabled', true);
        }
    }

    function SetTypeOfTransaction(t) {
        let typeOfTransaction = $(t).find('option:selected').attr('data-TOT');
        let inputTypeTransaction = $('#inputTypeTransaction');

        if (typeOfTransaction != undefined) {
            inputTypeTransaction.val(typeOfTransaction);

            HiddenProjectCode(typeOfTransaction);
            MinPickDatePlus10WorkingDay(typeOfTransaction);
        } else {
            inputTypeTransaction.val('');

            HiddenProjectCode(typeOfTransaction);
        }
    }

    function SetOptionBudgetCode(selectListCoa) {
        if (selectListCoa != null && selectListCoa.length > 0) {

            selectListCoa.forEach(function (data) {
                console.log({ data });
                $('#selectBudgetCode').append(`<option value="${data.text.split('-')[0].trim()}" data-id="${data.value}">${data.text}</option>`);
            });

        }
    }

    function SavePRF() {
        $('#btnSaveStep1').prop('disabled', true);
        LoadingShow();

        let businessUnitId = parseInt($('#selectBusinessUnit').find('option:selected').val());
        let costCenterId = parseInt($('#selectCostCenter').find('option:selected').val());
        let currency = $('#selectCurrency').find('option:selected').val();
        let budgetCode = parseInt($('#selectBudgetCode').find('option:selected').val());
        let estimatedTotalBudget = ParseFloatMoney($('#inputEstimatedTotalBudget').val());
        let typeOfRequest = $('[name="radioTypeRequest"]:checked').val();
        let spendingCategory = parseInt($('#selectSpendingByCategory').find('option:selected').val());
        let spendingSubCategory = parseInt($('#selectSubCategory').find('option:selected').val());
        let typeOfTransaction = $('#inputTypeTransaction').val();
        let projectCode = $('#selectProjectCode').val();
        let countItemRequest = parseInt($('#tbodyListItem tr').length);

        if (!isNaN(businessUnitId) &&
            !isNaN(costCenterId) && currency != '' &&
            !isNaN(budgetCode) && !isNaN(estimatedTotalBudget) &&
            typeOfRequest != undefined && !isNaN(spendingCategory) &&
            !isNaN(spendingSubCategory) && typeOfTransaction != '' &&
            !isNaN(countItemRequest) && countItemRequest > 0 &&
            ((typeOfTransaction.toLowerCase() == 'project' && projectCode != '') || (typeOfTransaction.toLowerCase() != 'project'))) {

            let listPRFDetail = [];

            $('#tbodyListItem tr').each(function (index, tr) {
                tr = $(tr);
                listPRFDetail.push({
                    requestItemName: tr.find('.span-request-item-name').text(),
                    requestItemNotes: tr.find('.span-name-item').text(),
                    typeOfGoods_SubCategoryId: parseInt(tr.find('.span-type-of-goods').text()),
                    qty: parseInt(tr.find('.span-qty-item').text()),
                    unit: tr.find('.span-unit-item').text(),
                    deliveryRequestDate: tr.find('.span-delivery-date-item').text(),
                    deliveryNotes: tr.find('.span-delivery-requirement-item').text(),
                    createdBy: $('.spanRequesterDetailUsername').text(),
                    lastUpdatedBy: $('.spanRequesterDetailUsername').text(),
                });
            });

            let prfInput = {
                BusinesUnitId: businessUnitId,
                CostCenterId: costCenterId,
                IsBudgetedSpend: $('#IsBudget').text() == 1 ? true : false,
                LCurrencyCode: currency,
                BudgetCode: $('#selectBudgetCode option:selected').val(),
                TotalBudgetEstimation: estimatedTotalBudget,
                TypeOfRequest: typeOfRequest,
                IsPurchasedPreviously: $('#radioPurchasedPreviously').is(":checked") == true ? true : false,
                SpendingCategory: spendingCategory,
                SpendingSubCategory: spendingSubCategory,
                TypeOfTransaction: typeOfTransaction,
                ProjectCode: typeOfTransaction.toLowerCase() == 'project' ? projectCode : null,
                DetailPreviously: $('#textareaPreviousPurchaseYes').val(),
                RepurchaseNotes: $('#textareaRecurrentPurchase').val(),
                AditionalRequestRequirement: $('#inputAdditionalRequest').val(),
                PenaltyBySuplier: $('#radioRecommendedPenalty').is(":checked") == true ? true : false,
                PenaltyBySuplierNotes: $('#inputRecommendedPenalty').val(),
                SLARequired: $('#radioRequiredWarranty').is(":checked") == true ? true : false,
                SLANotes: $('#inputRequiredWarranty').val(),
                CreatedBy: $('.spanRequesterDetailUsername').text(),
                LastUpdatedBy: $('.spanRequesterDetailUsername').text(),
                PurchaseRequestFormDetails: listPRFDetail
            };

            $.ajax({
                type: "POST",
                contentType: "application/json",
                url: `${$baseurl}/PRF/AddPurchaseRequestForm`,
                data: JSON.stringify(prfInput),
                success: function (result) {
                    LoadingClose();
                    if (result.code == 200) {

                        Swal.fire('Save PRF Success!', `PRF No : ${result.data.prfNumber}`, 'success');

                        $('.span-prf-no').text(result.data.prfNumber);
                        $('.span-prf-id').text(result.data.prfId);
                        $('#btnSaveStep1').remove();
                        $('#btnUpdateStep1').show().prop('disabled', false);
                        $('#btnContinueStep1').show();

                        let budgets = GetBudgetInfoCheckCoa('prf', result.data.prfNumber, null);
                        let ccName = $('#selectCostCenter').find('option:selected').text();
                        let buName = $('#selectBudgetCode option:selected').text();
                        if (budgets.length == 0) {
                            $('.blink').attr('hidden', false);
                            $("#IsBudget").text(0);
                            $('#btnContinueStep1').show();
                            $('#IsBudgetUpdate').text(false);
                            return false;
                        }

                        if (budgets[0].budgetId == 0 || budgets[0].budgetId == null) {
                            Swal.fire(
                                'Data Not Complete!',
                                `${ccName} tidak terdaftar atas ${buName}. Proses tidak dapat di lanjut mohon daftarkan terlebih dahulu.`,
                                'warning'
                            );
                            $('#btnContinueStep1').hide();
                            return false;
                        }
                        else if (!budgets[0].isBudget) {
                            $('.blink').attr('hidden', false);
                            $("#IsBudget").text(0);
                            $('#IsBudgetUpdate').text(false);
                            $('#btnContinueStep1').show();
                        } else {
                            $('.blink').attr('hidden', true);
                            $("#IsBudget").text(1);
                            $('#IsBudgetUpdate').text(true);
                            $('#btnContinueStep1').show()
                        }

                    } else {
                        $('#btnSaveStep1').prop('disabled', false);

                        Swal.fire('Error!', result.data, 'error');
                    }
                },
                error: function (result) {
                    LoadingClose();
                    $('#btnSaveStep1').prop('disabled', false);
                    Swal.fire( 'Failed!', result.data, 'error');
                }
            });

        } else {
            LoadingClose();
            $('#btnSaveStep1').prop('disabled', false);

            let messageMandatoryInput = '';

            if (isNaN(costCenterId)) {
                messageMandatoryInput += 'Please select <b>Cost Center</b><label style="color: red;">*</label></br>';
            }

            if (currency == '') {
                messageMandatoryInput += 'Please select <b>Currency</b><label style="color: red;">*</label></br>';
            }

            if (isNaN(budgetCode)) {
                messageMandatoryInput += 'Please select <b>Budget Code</b><label style="color: red;">*</label></br>';
            }

            if (isNaN(estimatedTotalBudget)) {
                messageMandatoryInput += 'Please input <b>Estimated total budget</b><label style="color: red;">*</label></br>';
            }

            if (typeOfRequest == undefined) {
                messageMandatoryInput += 'Please select <b>Type of Request</b><label style="color: red;">*</label></br>';
            }

            if (isNaN(spendingCategory)) {
                messageMandatoryInput += 'Please select <b>Spending by Category</b><label style="color: red;">*</label></br>';
            }

            if (isNaN(spendingSubCategory)) {
                messageMandatoryInput += 'Please select <b>Sub Category</b><label style="color: red;">*</label></br>';
            }

            if (typeOfTransaction == '') {
                messageMandatoryInput += 'Please input <b>Type of Transaction</b><label style="color: red;">*</label></br>';
            }

            if (isNaN(countItemRequest) || countItemRequest <= 0) {
                messageMandatoryInput += 'Please add <b>Item Request</b><label style="color: red;">*</label></br>';
            }

            if (typeOfTransaction.toLowerCase() == 'project' && projectCode == '') {
                messageMandatoryInput += 'Please select <b>Project Code</b><label style="color: red;">*</label></br>';
            }

            Swal.fire(
                'Data Not Complete!',
                messageMandatoryInput,
                'warning'
            );
        }
    }

    function UpdatePRF() {
        $('#btnSaveStep1').prop('disabled', true);
        LoadingShow();

        let businessUnitId = parseInt($('#selectBusinessUnit').find('option:selected').val());
        let costCenterId = parseInt($('#selectCostCenter').find('option:selected').val());
        let currency = $('#selectCurrency').find('option:selected').val();
        let budgetCode = parseInt($('#selectBudgetCode').find('option:selected').val());

        let budgetId = parseInt($('#selectBudgetCode').find('option:selected').data('id'));

        let estimatedTotalBudget = ParseFloatMoney($('#inputEstimatedTotalBudget').val());
        let typeOfRequest = $('[name="radioTypeRequest"]:checked').val();
        let spendingCategory = parseInt($('#selectSpendingByCategory').find('option:selected').val());
        let spendingSubCategory = parseInt($('#selectSubCategory').find('option:selected').val());
        let typeOfTransaction = $('#inputTypeTransaction').val();
        let projectCode = $('#selectProjectCode').val();
        let countItemRequest = parseInt($('#tbodyListItem tr').length);

        if (!isNaN(businessUnitId) &&
            !isNaN(costCenterId) && currency != '' &&
            !isNaN(budgetCode) && !isNaN(estimatedTotalBudget) &&
            typeOfRequest != undefined && !isNaN(spendingCategory) &&
            !isNaN(spendingSubCategory) && typeOfTransaction != '' &&
            !isNaN(countItemRequest) && countItemRequest > 0 &&
            ((typeOfTransaction.toLowerCase() == 'project' && projectCode != '') || (typeOfTransaction.toLowerCase() != 'project'))) {

            let listPRFDetail = [];

            $('#tbodyListItem tr').each(function (index, tr) {
                tr = $(tr);
                listPRFDetail.push({
                    requestItemName: tr.find('.span-request-item-name').text(),
                    requestItemNotes: tr.find('.span-name-item').text(),
                    typeOfGoods_SubCategoryId: parseInt(tr.find('.span-type-of-goods').text()),
                    qty: parseInt(tr.find('.span-qty-item').text()),
                    unit: tr.find('.span-unit-item').text(),
                    deliveryRequestDate: tr.find('.span-delivery-date-item').text(),
                    deliveryNotes: tr.find('.span-delivery-requirement-item').text(),
                    createdBy: $('.spanRequesterDetailUsername').text(),
                    lastUpdatedBy: $('.spanRequesterDetailUsername').text(),
                });
            });

            let prfInput = {
                PRFId: $('.span-prf-id').text(),
                BusinesUnitId: businessUnitId,
                CostCenterId: costCenterId,
                IsBudgetedSpend: $('#IsBudget').text() == 1 ? true : false,
                LCurrencyCode: currency,
                BudgetCode: $('#selectBudgetCode option:selected').val(),
                TotalBudgetEstimation: estimatedTotalBudget,
                TypeOfRequest: typeOfRequest,
                IsPurchasedPreviously: $('#radioPurchasedPreviously').is(":checked") == true ? true : false,
                SpendingCategory: spendingCategory,
                SpendingSubCategory: spendingSubCategory,
                TypeOfTransaction: typeOfTransaction,
                ProjectCode: typeOfTransaction.toLowerCase() == 'project' ? projectCode : null,
                DetailPreviously: $('#textareaPreviousPurchaseYes').val(),
                RepurchaseNotes: $('#textareaRecurrentPurchase').val(),
                AditionalRequestRequirement: $('#inputAdditionalRequest').val(),
                PenaltyBySuplier: $('#radioRecommendedPenalty').is(":checked") == true ? true : false,
                PenaltyBySuplierNotes: $('#inputRecommendedPenalty').val(),
                SLARequired: $('#radioRequiredWarranty').is(":checked") == true ? true : false,
                SLANotes: $('#inputRequiredWarranty').val(),
                LastUpdatedBy: $('.spanRequesterDetailUsername').text(),
                PurchaseRequestFormDetails: listPRFDetail
            };

            var paramCheckBudget = {
                requestAmount: estimatedTotalBudget,
                costCenterId: costCenterId,
                accountMasterId: budgetId,
                transactionDate: new Date()
            };

            var dataBudget = GetBudgetInfoV2(paramCheckBudget);
            if (dataBudget.budgetId == null || dataBudget.budgetId == 0) {
                let costCenterName = '';
                let buName = '';
                Swal.fire(
                    'Data Not Complete!',
                    `Cost Center ${costCenterName} tidak terdaftar atas ${buName}. Proses tidak dapat di lanjut mohon daftarkan terlebih dahulu.`,
                    'warning'
                );
                return false;
            }


            $.ajax({
                type: "POST",
                contentType: "application/json",
                url: `${$baseurl}/PRF/UpdatePurchaseRequestForm`,
                data: JSON.stringify(prfInput),
                success: function (result) {
                    LoadingClose();
                    if (result.code == 200) {
                        Swal.fire('Save PRF Success!', `PRF No : ${result.data.prfNumber}`, 'success');

                        let budgets = GetBudgetInfoCheckCoa('prf', result.data.prfNumber, null);
                        let ccName = $('#selectCostCenter').find('option:selected').text();
                        let buName = $('#selectBudgetCode option:selected').text();
                        if (budgets.length == 0) {

                            $('.blink').attr('hidden', false);
                            $("#IsBudget").text(0);
                            $('#btnContinueStep1').show();
                            return false;
                        }

                        if (budgets[0].budgetId == 0 || budgets[0].budgetId == null) {
                            Swal.fire(
                                'Data Not Complete!',
                                `${ccName} tidak terdaftar atas ${buName}. Proses tidak dapat di lanjut mohon daftarkan terlebih dahulu.`,
                                'warning'
                            );
                            $('#btnContinueStep1').hide();
                            return false;
                        }
                        else if (!budgets[0].isBudget) {
                            $('.blink').attr('hidden', false);
                            $("#IsBudget").text(0);
                            $('#btnContinueStep1').show();
                        } else {
                            $('.blink').attr('hidden', true);
                            $("#IsBudget").text(1);
                            $('#btnContinueStep1').show()
                        }

                    } else {
                        Swal.fire('Error!', result.data, 'error');
                    }
                },
                error: function (result) {
                    LoadingClose();
                    $('#btnSaveStep1').prop('disabled', false);

                    Swal.fire('Failed!', result.data, 'error');
                }
            });

        } else {
            LoadingClose();
            $('#btnSaveStep1').prop('disabled', false);

            let messageMandatoryInput = '';

            if (isNaN(costCenterId)) {
                messageMandatoryInput += 'Please select <b>Cost Center</b><label style="color: red;">*</label></br>';
            }

            if (currency == '') {
                messageMandatoryInput += 'Please select <b>Currency</b><label style="color: red;">*</label></br>';
            }

            if (isNaN(budgetCode)) {
                messageMandatoryInput += 'Please select <b>Budget Code</b><label style="color: red;">*</label></br>';
            }

            if (isNaN(estimatedTotalBudget)) {
                messageMandatoryInput += 'Please input <b>Estimated total budget</b><label style="color: red;">*</label></br>';
            }

            if (typeOfRequest == undefined) {
                messageMandatoryInput += 'Please select <b>Type of Request</b><label style="color: red;">*</label></br>';
            }

            if (isNaN(spendingCategory)) {
                messageMandatoryInput += 'Please select <b>Spending by Category</b><label style="color: red;">*</label></br>';
            }

            if (isNaN(spendingSubCategory)) {
                messageMandatoryInput += 'Please select <b>Sub Category</b><label style="color: red;">*</label></br>';
            }

            if (typeOfTransaction == '') {
                messageMandatoryInput += 'Please input <b>Type of Transaction</b><label style="color: red;">*</label></br>';
            }

            if (isNaN(countItemRequest) || countItemRequest <= 0) {
                messageMandatoryInput += 'Please add <b>Item Request</b><label style="color: red;">*</label></br>';
            }

            if (typeOfTransaction.toLowerCase() == 'project' && projectCode == '') {
                messageMandatoryInput += 'Please select <b>Project Code</b><label style="color: red;">*</label></br>';
            }

            Swal.fire(
                'Data Not Complete!',
                messageMandatoryInput,
                'warning'
            );
        }
    }
</script>
