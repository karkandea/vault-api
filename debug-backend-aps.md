@{
    Layout = "~/Views/Shared/_Layout.cshtml";
}
@model APS_WEB_APP.Models.Report.Procurement.View

<style>
    table.dataTable {
        table-layout: fixed;
    }

    th {
        vertical-align: top !important;
    }

    td.td-text_align-right {
        text-align: right;
    }

    td.td-text_align-center {
        text-align: center;
    }

    td.td-word_break-break_word {
        word-break: break-word;
    }

    table.dataTable tbody > tr > td {
        padding-left: 18px;
        padding-right: 18px;
    }
</style>

<div id="page-wrapper">

    <div class="row">
        <div class="col-lg-12">
            <h1 class="page-header">Report PO</h1>
        </div>
    </div>

    <div>
        <ol class="breadcrumb">
            <li><a href="@Url.Content("~/Home/Index")">Home</a></li>
            <li class="active">Report PO</li>
        </ol>
    </div>

    <div class="panel panel-primary mb-1 mt-1">

        <div class="panel-heading">
            <h3 class="panel-title">Filter</h3>
        </div>

        <div class="panel-body">

            <div class="row">
                <!-- PR Category -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">PR Category</b></span>
                    <br />
                    <select class="form-control select2" asp-items="@Model.CategoryProcess_SubCategory" id="PR_Category_Id" style="width: 50%;">
                        <option value=" " selected>ALL</option>
                    </select>
                </div>
                <!-- PR No -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">PR No</b></span>
                    <br />
                    <input class="form-control" type="text" id="PR_No" style="width: 100%;" />
                </div>
            </div>
            <div class="row">
                <!-- PR Date -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">PR Date</b></span>
                    <br />
                    <div class="input-daterange input-group">
                        <input type="text" class="form-control date-picker" autocomplete="off" data-plugin="datepicker" id="PR_Date_Begin" />
                        <span class="input-group-addon">To</span>
                        <input type="text" class="form-control date-picker" autocomplete="off" data-plugin="datepicker" id="PR_Date_End" />
                    </div>
                </div>
                <!-- PR Status -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">PR Status</b></span>
                    <br />
                    <select class="form-control select2" asp-items="@Model.PurchaseRequestStatus" id="PR_Status_ValueId" style="width: 50%;">
                        <option value=" " selected>ALL</option>
                    </select>
                </div>
            </div>

            <!-- hr -->
            <hr style="margin-top: 0px; border-top: 1px solid #428bca; margin-bottom: 10px;" />

            <div class="row">
                <!-- Department -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">Department</b></span>
                    <br />
                    <select class="form-control select2" asp-items="@Model.CostCenter" id="Department_Id">
                        <option value=" " selected>ALL</option>
                    </select>
                </div>
                <!-- Account Code -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">Account Code</b></span>
                    <br />
                    <select class="form-control select2" asp-items="@Model.AccountMaster" id="Account_Code_Id">
                        <option value=" " selected>ALL</option>
                    </select>
                </div>
            </div>
            <div class="row">
                <!-- Cost Center -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">Cost Center</b></span>
                    <br />
                    <select class="form-control select2" asp-items="@Model.CostCenter" id="Cost_Center_Id">
                        <option value=" " selected>ALL</option>
                    </select>
                </div>
                <!-- Vendor -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">Vendor</b></span>
                    <br />
                    <select class="form-control select2" asp-items="@Model.Vendor" id="Vendor_Id">
                        <option value=" " selected>ALL</option>
                    </select>
                </div>
            </div>

            <!-- hr -->
            <hr style="margin-top: 0px; border-top: 1px solid #428bca; margin-bottom: 10px;" />

            <div class="row">
                <!-- Order Type -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">Order Type</b></span>
                    <br />
                    <select class="form-control select2" asp-items="@Model.TypeProcess_SubCategory" id="Order_Type_Id" style="width: 50%;">
                        <option value=" " selected>ALL</option>
                    </select>
                </div>
                <!-- Order No -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">Order No</b></span>
                    <br />
                    <input class="form-control" type="text" id="Order_No" style="width: 100%;" />
                </div>
            </div>
            <div class="row">
                <!-- Order Date -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">Order Date</b></span>
                    <br />
                    <div class="input-daterange input-group">
                        <input type="text" class="form-control date-picker" autocomplete="off" data-plugin="datepicker" id="Order_Date_Begin" />
                        <span class="input-group-addon">To</span>
                        <input type="text" class="form-control date-picker" autocomplete="off" data-plugin="datepicker" id="Order_Date_End" />
                    </div>
                </div>
                <!-- Order Status -->
                <div class="col-md-3 col-xs-6" style="margin-bottom: 1.5em;">
                    <span><b style="font-size: 1.1em;">Order Status</b></span>
                    <br />
                    <select class="form-control select2" asp-items="@Model.PurchaseOrderStatus" id="Order_Status_ValueId" style="width: 50%;">
                        <option value=" " selected>ALL</option>
                    </select>
                </div>
            </div>

            <!-- hr -->
            <hr style="margin-top: 0px; border-top: 1px solid #428bca; margin-bottom: 10px;" />

            <!-- clear , search , export -->
            <div class="row">
                <div class="col-md-12 col-xs-12" style="margin-bottom: 0em;">
                    <button type="button" class="btn btn-default" id="clearButton" onclick="clearButtonOnClick()">Clear</button>
                    <button type="button" class="btn btn-primary" id="searchButton" onclick="searchButtonOnClick()">Search</button>
                    <button type="button" class="btn btn-info" id="exportButton" onclick="exportButtonOnClick()">Export To Excel</button>
                </div>
            </div>

        </div>
    </div>

    <div class="panel panel-primary mb-1 mt-1">
        <div class="panel-heading">
            <h3 class="panel-title">List</h3>
        </div>
        <div class="panel-body">
            <table name="ProcurementReportDataTable" class="table table-striped table-hover table-bordered table-condensed">
            </table>
        </div>
    </div>

</div>

<script>

    function procurementReportDataTableColumns() {
        function Row_Number_render(data, type, row, meta) {
            return (meta.row + meta.settings._iDisplayStart + 1);
        }
        function Intl_DateTimeFormat_render(data, type, row, meta) {
            if (!data) return '';
            // yyyy-MM-dd HH:mm:ss
            const locales = 'sv-SE';
            const options = { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false };
            const render = new Intl.DateTimeFormat('sv-SE', options).format(new Date(data));
            return render;
        }
        function Intl_NumberFormat_render(data, type, row, meta) {
            if (!(data ?? '').toString()) return '';
            // 123.123.123,1231
            const locales = 'en-US';
            const options = { style: 'decimal', minimumFractionDigits: 2, maximumFractionDigits: 2 };
            const render = new Intl.NumberFormat(locales, options).format(data);
            return render;
        }
        return [
            { orderable: false, width: '20px', title: 'No', className: 'td-text_align-center', render: Row_Number_render }
            , { orderable: false, width: '100px', data: 'PR_No' }
            , { orderable: false, width: '50px', data: 'PR_Status' }
            , { orderable: false, width: '150px', data: 'PR_Date', render: Intl_DateTimeFormat_render }
            , { orderable: false, width: '100px', data: 'Requester' }
            , { orderable: false, width: '200px', data: 'Department' }
            , { orderable: false, width: '50px', data: 'Type_Of_Transaction' }
            , { orderable: false, width: '100px', data: 'Buyer_User_Name' }
            , { orderable: false, width: '100px', data: 'Total_Budget_Estimation', className: 'td-text_align-right', render: Intl_NumberFormat_render }
            , { orderable: false, width: '50px', data: 'Critical' }
            , { orderable: false, width: '200px', data: 'Category' }
            , { orderable: false, width: '200px', data: 'Item_Name' }
            , { orderable: false, width: '200px', data: 'Account_Code' }
            , { orderable: false, width: '200px', data: 'Cost_Center' }
            , { orderable: false, width: '200px', data: 'Vendor_Selection' }
            , { orderable: false, width: '50px', data: 'Currency' }
            , { orderable: false, width: '150px', data: 'PR_Posted_Date', render: Intl_DateTimeFormat_render }
            , { orderable: false, width: '150px', data: 'Delivery_Request_Date', render: Intl_DateTimeFormat_render }
            , { orderable: false, width: '150px', data: 'Final_Spec_Req_Date', render: Intl_DateTimeFormat_render }
            , { orderable: false, width: '150px', data: 'Generate_Proc_Sum_Date', render: Intl_DateTimeFormat_render }
            , { orderable: false, width: '20px', data: 'TAT_WD', className: 'td-text_align-right' }
            , { orderable: false, width: '20px', data: 'SLA_WD', className: 'td-text_align-right' }
            , { orderable: false, width: '50px', data: 'SLA_Status' }
            , { orderable: false, width: '200px', data: 'Vendor' }
            , { orderable: false, width: '50px', data: 'Selected' }

            , { orderable: false, width: '100px', data: 'Total_Price', className: 'td-text_align-right', render: Intl_NumberFormat_render }
            , { orderable: false, width: '100px', data: 'Price_Per_Item', className: 'td-text_align-right', render: Intl_NumberFormat_render }
            , { orderable: false, width: '100px', data: 'Total_Price_Inc_Other_Cost', className: 'td-text_align-right', render: Intl_NumberFormat_render }
            , { orderable: false, width: '100px', data: 'Price_Per_Item_Inc_Other_Cost', className: 'td-text_align-right', render: Intl_NumberFormat_render }
            , { orderable: false, width: '100px', data: 'Realised_Saving', className: 'td-text_align-right', render: Intl_NumberFormat_render }

            , { orderable: false, width: '100px', data: 'Order_Type' }
            , { orderable: false, width: '150px', data: 'Order_No', className: 'td-word_break-break_word' }
            , { orderable: false, width: '50px', data: 'Order_Status' }
            , { orderable: false, width: '150px', data: 'Order_Date', render: Intl_DateTimeFormat_render }
            , { orderable: false, width: '100px', data: 'Order_Grand_Total_Amount', className: 'td-text_align-right', render: Intl_NumberFormat_render }
            , { orderable: false, width: '150px', data: 'Approver_Date', render: Intl_DateTimeFormat_render }
            , { orderable: false, width: '100px', data: 'Approver_Name' }

            , { orderable: false, width: '150px', data: 'DN_No', className: 'td-word_break-break_word' }
            , { orderable: false, width: '50px', data: 'DN_Status' }
            , { orderable: false, width: '150px', data: 'DN_Date', render: Intl_DateTimeFormat_render }
            , { orderable: false, width: '20px', data: 'DN_Qty', className: 'td-text_align-right' }

            , { orderable: false, width: '150px', data: 'Invoice_No', className: 'td-word_break-break_word' }
            , { orderable: false, width: '50px', data: 'Invoice_Status' }
            , { orderable: false, width: '150px', data: 'Invoice_Date', render: Intl_DateTimeFormat_render }
            , { orderable: false, width: '100px', data: 'Invoice_Amount', className: 'td-text_align-right', render: Intl_NumberFormat_render }
            , { orderable: false, width: '100px', data: 'PPn', className: 'td-text_align-right', render: Intl_NumberFormat_render }
            , { orderable: false, width: '100px', data: 'PPh_23', className: 'td-text_align-right', render: Intl_NumberFormat_render }
            , { orderable: false, width: '100px', data: 'PPh_42', className: 'td-text_align-right', render: Intl_NumberFormat_render }
            , { orderable: false, width: '100px', data: 'Invoice_After_Tax_Or_Grand_Total', className: 'td-text_align-right', render: Intl_NumberFormat_render }
            , { orderable: false, width: '200px', data: 'Remarks', className: 'td-word_break-break_word' }

            , { orderable: false, width: '200px', data: 'ReasonCancel', className: 'td-word_break-break_word' }

        ].map(c => {
            if (c.data) {
                c.title = c.data;
                c.title = c.title.replaceAll('_Or_', '_/_');
                c.title = c.title.replaceAll('_', ' ');
            }
            return c;
        });
    }

    function procurementReportDataTableAjaxData() {

        let _data = {
            x: 1
            , PR_Category_Id: $('select#PR_Category_Id :selected').val()?.trim() ?? ''
            , PR_Category_Text: $('select#PR_Category_Id :selected').text()
            , PR_No: $('input#PR_No').val().trim()
            , PR_Status_ValueId: $('select#PR_Status_ValueId :selected').val()?.trim() ?? ''
            , PR_Status_Text: $('select#PR_Status_ValueId :selected').text()
            , PR_Date_Begin: $('input#PR_Date_Begin').val()?.trim() ?? ''
            , PR_Date_End: $('input#PR_Date_End').val()?.trim() ?? ''
            , Department_Id: $('select#Department_Id :selected').val()?.trim() ?? ''
            , Department_Text: $('select#Department_Id :selected').text()
            , Account_Code_Id: $('select#Account_Code_Id :selected').val()?.trim() ?? ''
            , Account_Code_Text: $('select#Account_Code_Id :selected').text()
            , Cost_Center_Id: $('select#Cost_Center_Id :selected').val()?.trim() ?? ''
            , Cost_Center_Text: $('select#Cost_Center_Id :selected').text()
            , Vendor_Id: $('select#Vendor_Id :selected').val()?.trim() ?? ''
            , Vendor_Text: $('select#Vendor_Id :selected').text()
            , Order_Type_Id: $('select#Order_Type_Id :selected').val()?.trim() ?? ''
            , Order_Type_Text: $('select#Order_Type_Id :selected').text()
            , Order_No: $('input#Order_No').val()?.trim() ?? ''
            , Order_Status_Text: $('select#Order_Status_ValueId :selected').text()
            , Order_Status_ValueId: $('select#Order_Status_ValueId :selected').val()?.trim() ?? ''
            , Order_Date_Begin: $('input#Order_Date_Begin').val()?.trim() ?? ''
            , Order_Date_End: $('input#Order_Date_End').val()?.trim() ?? ''
        };
        _data.PR_Category_Id = (_data.PR_Category_Id === '') ? null : _data.PR_Category_Id;
        _data.PR_No = (_data.PR_No === '') ? null : _data.PR_No;
        _data.PR_Status_ValueId = (_data.PR_Status_ValueId === '') ? null : _data.PR_Status_ValueId;
        _data.PR_Date_Begin = (_data.PR_Date_Begin === '') ? null : _data.PR_Date_Begin;
        _data.PR_Date_End = (_data.PR_Date_End === '') ? null : _data.PR_Date_End;
        _data.Department_Id = (_data.Department_Id === '') ? null : _data.Department_Id;
        _data.Account_Code_Id = (_data.Account_Code_Id === '') ? null : _data.Account_Code_Id;
        _data.Cost_Center_Id = (_data.Cost_Center_Id === '') ? null : _data.Cost_Center_Id;
        _data.Vendor_Id = (_data.Vendor_Id === '') ? null : _data.Vendor_Id;
        _data.Order_Type_Id = (_data.Order_Type_Id === '') ? null : _data.Order_Type_Id;
        _data.Order_No = (_data.Order_No === '') ? null : _data.Order_No;
        _data.Order_Status_ValueId = (_data.Order_Status_ValueId === '') ? null : _data.Order_Status_ValueId;
        _data.Order_Date_Begin = (_data.Order_Date_Begin === '') ? null : _data.Order_Date_Begin;
        _data.Order_Date_End = (_data.Order_Date_End === '') ? null : _data.Order_Date_End;
        return _data;
    }

    function procurementReportDataTable() {
        const url = `${$baseurl}/Report/Procurement/DataTables/WIP`;
        LoadingShow();
        $('.dataTables_scrollBody').css('display', 'none');
        $('[name="ProcurementReportDataTable"]')
            .DataTable({
                x: 1
                , destroy: true
                , searching: false
                , processing: true
                , serverSide: true
                , lengthMenu: [10, 50, 100, 200]
                , order: []

                , autoWidth: false
                , scrollX: true
                , initComplete: function () {
                    $('.dataTables_scrollBody').css('display', '');
                    $('.dataTables_scrollBody thead tr').css('visibility', 'collapse');
                    LoadingClose();
                }
                , preDrawCallback: function () {
                    LoadingShow();
                    $('.dataTables_scrollBody').css('display', 'none');
                }
                , drawCallback: function () {
                    $('.dataTables_scrollBody').css('display', '');
                    $('.dataTables_scrollBody thead tr').css('visibility', 'collapse');
                    LoadingClose();
                }

                , ajax: { type: 'POST', url: url, datatype: 'json', data: procurementReportDataTableAjaxData() }
                , dataSrc: function (response) { /* console.log({ response }); */ return response.data; }
                , columns: procurementReportDataTableColumns()
            });
    }

    function exportButtonOnClick() {
        Swal.fire({
            title: 'Export to Excel ?'
            , icon: 'warning'
            , showDenyButton: true
            , showCancelButton: true
            , confirmButtonText: `Yes`
            , denyButtonText: `Cancel`
        }).then((result) => {
            if (result.value) {
                var json = procurementReportDataTableAjaxData();
                json.start = 0;
                json.length = 0;
                json.draw = 0;
                var stringify = JSON.stringify(json)
                window.open(`${$baseurl}/Report/Procurement/Xlsx/WIP?` + encryptUsingAES256(`json=${stringify}`));
            }
        });
    }

    function searchButtonOnClick() {
        procurementReportDataTable();
    }

    function clearButtonOnClick() {
        $('select#PR_Category_Id').val('').trigger('change');
        $('input#PR_No').val('');
        $('select#PR_Status_ValueId').val('').trigger('change');
        $('input#PR_Date_Begin').val('');
        $('input#PR_Date_End').val('');

        $('select#Department_Id').val('').trigger('change');
        $('select#Account_Code_Id').val('').trigger('change');
        $('select#Cost_Center_Id').val('').trigger('change');
        $('select#Vendor_Id').val('').trigger('change');

        $('select#Order_Type_Id').val('').trigger('change');
        $('input#Order_No').val('');
        $('select#Order_Status_ValueId').val('').trigger('change');
        $('input#Order_Date_Begin').val('');
        $('input#Order_Date_End').val('');

        procurementReportDataTable();
    }

    $(document).ready(function () {
        $('div.input-daterange').datepicker(
            {
                x:0
                , orientation: 'bottom'
                , autoclose: true
                , format: 'yyyy-mm-dd'
                , dayOfWeekStart: 1
                , todayHighlight: true
            }
        )
        .on('changeDate', function (selected) {
        });
        $('#Order_Date_Begin').val(new Intl.DateTimeFormat('sv-SE').format(new Date())).change();
        $('#Order_Date_End').val(new Intl.DateTimeFormat('sv-SE').format(new Date())).change();
        procurementReportDataTable();
    });

</script>
