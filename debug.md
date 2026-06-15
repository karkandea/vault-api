
	@using APS_WEB_APP.Common
	@using APS_WEB_APP.Models.MasterSource;
	@using APS_WEB_APP.Models.NonShoppingCart.ProcurmentBuyer.PRFVendorEnhanced;
@{
	var UserDetail = (AuthEntities)(ViewBag.UserDetail);
	var PRF = (PurchaseRequestFormModel.PurchaseRequestFormView)ViewBag.PRFResponse;

	var pvev = (PRFVendorEnhancedViewGetResponseModel)(ViewBag.PRFVendorEnhancedView);
	var pvedList = pvev.PRF.PRFVendorEnhanced.PRFVendorEnhancedDetailList;
	var aList = pvev.PRF.PRFVendorEnhanced.AttachmentList;
	int accessLevel = (int)ViewBag.AccessLevel;
	bool requestor = false;

	if (accessLevel == 4)
	{
		pvedList = pvedList.Where(e => e.SubCategory.Sequence == 2).ToList();
		requestor = true;
	}
	// else
	// {
	// 	pvedList = pvedList.Where(e => e.SubCategory.Sequence == 1).ToList();
	// }
}

<div class="row">
	<div class="col-lg-12">
		<span class="PRFVendorEnhancedId" hidden>@pvev.PRF.PRFVendorEnhanced.Id</span>
		<table class="table table-striped cost-center table-enhanced-dd">
			<thead>
				<tr>
					<th>Due Diligence List</th>
					<th>Responbility</th>
					<th>Rationale of Risk Acceptance</th>
					<th>Remarks <label style="color: red;">*</label></th>
					<th>Attachment</th>
				</tr>
			</thead>
			<tbody id="PRFVendorEnhancedDetailTBody">

				@{
					foreach (var pved in pvedList)
					{
						#region c#
						/*
						SC-2023-07-00076 Compliance Risk (Sanction Screening using AXA Watch List & World Check)
						SC-2023-07-00077 Reputational Risk
						SC-2023-07-00078 Financial Risk
						SC-2023-07-00080 Enhanced ABC Due Diligence
						SC-2023-07-00081 DPIA
						SC-2023-07-00082 VSDDT
						*/
						string validateBy = "";
						bool disabledRadio = false;
						List<string> radioInput = ["Critical", "Non Critical"];

						if (pved.SubCategory.Sequence == 1)
						{
							validateBy = "Procurement";
						}
						else if (pved.SubCategory.Sequence == 2)
						{
							validateBy = "User";
						}

						if (pved.SubCategory.SubCategoryCode == "SC-2023-07-00076")
						{
							radioInput = ["PEP", "Non PEP"];
						}
						if (pved.SubCategory.SubCategoryCode == "SC-2023-07-00077")
						{
							radioInput = ["Extremely Serious", "Very Serious", "Serious", "Minor", "Not significant"];
						}
						else if (pved.SubCategory.SubCategoryCode == "SC-2023-07-00078")
						{
							radioInput = ["Healthy Company", "Warning Signs", "Potential Bankrcuptcy"];
						}
						else if (pved.SubCategory.SubCategoryCode == "SC-2023-07-00080")
						{
							radioInput = ["Yes", "No"];
						}
						else if (pved.SubCategory.SubCategoryCode == "SC-2023-07-00081")
						{
							radioInput = ["Yes", "No"];
							pved.RationalOfRisk = pved.RationalOfRisk ?? (PRF.DataActivity == 1 ? "Yes" : "No");
							disabledRadio = true;
						}
						else if (pved.SubCategory.SubCategoryCode == "SC-2023-07-00082")
						{
							radioInput = ["Yes", "No"];
							pved.RationalOfRisk = pved.RationalOfRisk ?? (PRF.ITSecurityActivity == 1 ? "Yes" : "No");
							disabledRadio = true;
						}
						#endregion

							<tr>

								<td>
									<span class="PRFVendorEnhancedDetailSubCategoryName">@pved.SubCategory.SubCategoryName<sup><label style="color: red;">@(@pved.SubCategory.Description == "MANDATORY" ? "PO" : "")</label></sup></span>
									<span class="PRFVendorEnhancedDetailSubCategoryId" hidden>@pved.SubCategory.Id</span>
									<span class="PRFVendorEnhancedDetailId" hidden>@pved.Id</span>
								</td>

								<td>@validateBy</td>

								<td>
									@foreach (var radio in radioInput)
								{
										<label class="radio-inline">
											<input type="radio"
											   	class="PRFVendorEnhancedDetailRationalOfRisk"
											   	value="@radio"
											@((pved.RationalOfRisk == radio) ? "checked" : "")
											@((!requestor && pved.SubCategory.Sequence == 2) || disabledRadio ? "disabled" : "")
											   	name="@pved.SubCategory.SubCategoryCode">
											@radio
										</label>
								}
								</td>

								<td>
									<textarea class="form-control PRFVendorEnhancedDetailRemarks" @(!requestor && pved.SubCategory.Sequence == 2 ? "disabled" : "")>@pved.Remarks</textarea>
								</td>

								<td>
									<span class="PRFVendorEnhancedDetailAttachmentId" hidden>@(pved.AttachmentId.ToString() ?? "")</span>

									@if (!(!requestor && pved.SubCategory.Sequence == 2))
								{
										<button type="button"
												class="btn btn-sm btn-primary"
												data-toggle="modal"
												data-target="#PRFVendorEnhancedDetailAttachmentModal"
												onclick="PRFVendorEnhancedDetailAttachmentOpenOnClick(this, @(pved.AttachmentId is null ? "null" : pved.AttachmentId.ToString()))"
												id="PRFVendorEnhancedDetailAttachmentOpen_@pved.SubCategory.Id">
											Attachments
										</button>
								}
								else
								{
										<button type="button"
												class="btn btn-sm btn-primary"
												data-toggle="modal"
												data-target="#PRFVendorEnhancedDetailAttachmentModal"
												onclick="DetailAttacmentRequestorForProcurment(this, @(pved.AttachmentId is null ? "null" : pved.AttachmentId.ToString()))"
												id="PRFVendorEnhancedDetailAttachmentOpen_@pved.SubCategory.Id">
											Attachments
										</button>
								}

									<span class="glyphicon glyphicon-@(pved.AttachmentId is null ? "remove" : "ok") icon-attachment"
									  	style="color: @(pved.AttachmentId is null ? "red" : "green")">
									</span>
								</td>
							</tr>
					}

				}
			</tbody>
		</table>
	</div>
</div>

<div class="row">
	<div class="col-lg-12">
		Summary:
		<br />
		Based on VENDOR RISK FRAMEWORK ANALYSIS, the Vendor is under category NON-CRITICAL / CRITICAL category and has passed the enhanced due diligence.
	</div>
</div>

<hr />

<div class="row" style="display: none">
	<div class="col-lg-12">
		<h3 class="text-primary">Upload Enhanced DD Documents</h3>
		<br />
		<div class="text-muted">Upload Enhanced DD<label style="color: red;">*</label></div>
	</div>
</div>

<div class="row" style="display: none">
	<div class="col-lg-6">
		<form onsubmit="return false" id="formPRFVendorEnhancedAttachmentUploadOnClick">
			<table class="table table-borderless">
				<tbody>
					<tr>
						<td>Upload file</td>
						<td>
							<input type="file"
								   class="form-control"
								   required
							@(aList.Count > 0 ? "disabled" : "")
								   id="PRFVendorEnhancedAttachmentFile" />
						</td>
					</tr>
					<tr>
						<td>Description</td>
						<td>
							<input type="text"
								   class="form-control"
							@(aList.Count > 0 ? "disabled" : "")
								   id="PRFVendorEnhancedAttachmentDescription" />
						</td>
					</tr>
					<tr>
						<td></td>
						<td>
							<button class="btn btn-primary"
									type="button"
									onclick="PRFVendorEnhancedAttachmentUploadOnClick();"
							@(aList.Count > 0 ? "disabled" : "")
									id="PRFVendorEnhancedAttachmentUpload">
								Upload
							</button>
						</td>
					</tr>
				</tbody>
			</table>
		</form>

		<table class="table">
			<tbody id="PRFVendorEnhancedAttachmentsTBody" class="tbody-list-attachment-enhanced-dd">

				@{
					foreach (var a in aList)
					{

							<tr>
								<td>
									<a onclick="DownloadAttachment(@a.Id)">
										<span class="glyphicon glyphicon-file" />
										@a.OriginalFileName
									</a>
								</td>
								<td><span class="spanDDEnhancedAttachmentId" hidden>@a.Id</span></td>
								<td>@a.CreatedTimeText</td>
								<td><span class="spanDDEnhancedCommentDoc">@a.Description</span></td>
								<td>
									<a class="btn-delete-attachment-DDEhnhanced"
								   	onclick="DeleteAttachment(@a.Id); DeleteDDEnhancedAttachment(this)">
										<span class="glyphicon glyphicon-trash text-danger" />
									</a>
								</td>
							</tr>

					}
				}

			</tbody>
		</table>
	</div>
</div>

<div class="row" style="text-align: center;">
	<div class="col-lg-12">
		<button class="btn btn-default btn-step"
				type="button"
				onclick="RedirectToNav('nav3'); $('#navQuotation').show();"
				id="btnBackEnhancedDD"
				style="@(accessLevel == 2 || accessLevel == 4 ? "display:none;" : "")">
			Back
		</button>

		<button class="btn btn-primary btn-step"
				type="button"
				onclick="PRFVendorEnhancedSaveOnClick();"
				id="PRFVendorEnhancedSave">
			Save
		</button>

		@if (!requestor)
		{
			<button class="btn btn-primary btn-step"
					type="button"
					onclick="buttonNextProcsumOrPAP();"
					id="btnContinueEnhancedDD"
					style="@(accessLevel == 2 || accessLevel == 4 ? "display:none;" : "")">
				Continue
			</button>
		}
	</div>
</div>

<!-- Modal DD Enhanced Detail Attachment -->
<div class="modal fade" id="PRFVendorEnhancedDetailAttachmentModal" tabindex="-1" role="dialog" aria-labelledby="PRFVendorEnhancedDetailAttachmentModalTitle" aria-hidden="true">
	<div class="modal-dialog modal-dialog-centered" role="document">
		<div class="modal-content">
			<div class="modal-header">
				<h5 class="modal-title" id="PRFVendorEnhancedDetailAttachmentModalLongTitle">
					Attachment
					<button type="button" class="close" data-dismiss="modal" aria-label="Close">
						<span aria-hidden="true">&times;</span>
					</button>
				</h5>
			</div>
			<div class="modal-body">
				<table class="table table-striped cost-center">
					<thead style="background-color: steelblue; color: white;">
						<tr>
							<th>File Name</th>
							<th class="text-center">Upload Date</th>
							<th class="text-center">Action</th>
						</tr>
					</thead>
					<tbody id="PRFVendorEnhancedDetailAttachmentTBody"></tbody>
				</table>
			</div>
			<div class="modal-footer">
				<button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
			</div>
		</div>
	</div>
</div>

<script>
	function buttonNextProcsumOrPAP() {
		if ("@(PRF.TypeProcess_SubCategoryCode)" === "SC-2023-08-11134") {
			RedirectToNav('aNavPAP')
			$('#navProcSum').hide();
			$('#navPAP').show();
		} else {
			RedirectToNav('nav5');
			$('#navProcSum').show();
		}
	}

	// PRFVendorEnhancedDetail Attachment Open OnClick
	function PRFVendorEnhancedDetailAttachmentOpenOnClick(t, attachmentId, actionType) {
		t = $(t);
		let userDetail = @Html.Raw(Json.Serialize(ViewBag.AccountDetail));
		buttonAttachmentId = t.attr('id');
		$('tbody#PRFVendorEnhancedDetailAttachmentTBody').empty();

		let trUpload = `
							<tr>
								<td class="text-center" colspan="2">
									<input
										class="form-control inputPRFVendorEnhancedDetailAttachmentUpload"
										type="file"/>
								</td>
								<td>
									<button
										class="btn btn-success btn-upload-attachment-modal"
										onclick="PRFVendorEnhancedDetailAttachmentUploadOnClick(this, '${buttonAttachmentId}')"
										type="button">
										Upload
									</button>
								</td>
							</tr>
							`;
		//trUpload = (userDetail.costCenterName.toLowerCase() !== 'procurement buyer') ? '' : trUpload;

		if (!attachmentId) {
			let tr = `<tr><td class="text-center text-danger" colspan="3">Data not found</td></tr>${trUpload}`;
			$('tbody#PRFVendorEnhancedDetailAttachmentTBody').append(tr);
			return;
		}

		$.ajax({
			type: 'GET'
			, url: `${$baseurl}/Attachment/GetAttachmentDetail?attachmentId=${attachmentId}`
			, error: function (error) {
				Swal.fire('Failed!', result, 'error');
			}
			, success: function (response) {
				if (response.code !== 200) {
					let tr = `<tr><td class="text-center text-danger" colspan="3">Data not found</td></tr>${trUpload}`;
					$('tbody#PRFVendorEnhancedDetailAttachmentTBody').append(tr);
				} else if (response.code == 200) {
					let btnDeleteAttachment = `
										<button
											class="btn btn-danger btn-delete-attachment-modal"
											onclick="DeleteRowDDEnhacedDetailAttachment(this, ${response.data.id}, '${buttonAttachmentId}')"
											type="button">
											<span class="glyphicon glyphicon-trash">
											</span>
										</button>`;
					//btnDeleteAttachment = (actionType.toLowerCase() == 'detail') ? '' : btnDeleteAttachment;

					const createdTime_ISO = new Intl.DateTimeFormat('sv-SE', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(new Date(response.data.createdTime));

					let tr = `
										<tr>
											<td>${response.data.originalFileName}</td>
											<td>${createdTime_ISO}</td>
											<td>
												<button
													class="btn btn-primary"
													onclick="DownloadAttachment(${response.data.id})"
													type="button">
													<span class="glyphicon glyphicon-save">
													</span>
												</button>
												${btnDeleteAttachment}
											</td>
										</tr>
										`;

					$('tbody#PRFVendorEnhancedDetailAttachmentTBody').append(tr);
					attachmentId = response.data.id;
				}
			}
		});
	}

	// PRFVendorEnhancedDetail Attachment Open OnClick
	function DetailAttacmentRequestorForProcurment(t, attachmentId, actionType) {
		t = $(t);
		let userDetail = @Html.Raw(Json.Serialize(ViewBag.AccountDetail));
		buttonAttachmentId = t.attr('id');
		$('tbody#PRFVendorEnhancedDetailAttachmentTBody').empty();

		if (!attachmentId) {
			let tr = `<tr><td class="text-center text-danger" colspan="3">Data not found</td></tr>`;
			$('tbody#PRFVendorEnhancedDetailAttachmentTBody').append(tr);
			return;
		}

		$.ajax({
			type: 'GET'
			, url: `${$baseurl}/Attachment/GetAttachmentDetail?attachmentId=${attachmentId}`
			, error: function (error) {
				Swal.fire('Failed!', result, 'error');
			}
			, success: function (response) {
				if (response.code !== 200) {
					let tr = `<tr><td class="text-center text-danger" colspan="3">Data not found</td></tr>${trUpload}`;
					$('tbody#PRFVendorEnhancedDetailAttachmentTBody').append(tr);
				} else if (response.code == 200) {
					const createdTime_ISO = new Intl.DateTimeFormat('sv-SE', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(new Date(response.data.createdTime));

					let tr = `
											<tr>
												<td>${response.data.originalFileName}</td>
													<td>${createdTime_ISO}</td>
												<td>
													<button
														class="btn btn-primary"
														onclick="DownloadAttachment(${response.data.id})"
														type="button">
														<span class="glyphicon glyphicon-save">
														</span>
													</button>
												</td>
											</tr>
											`;

					$('tbody#PRFVendorEnhancedDetailAttachmentTBody').append(tr);
					attachmentId = response.data.id;
				}
			}
		});
	}

	// PRFVendorEnhanced Attachment Upload OnClick
	function PRFVendorEnhancedAttachmentUploadOnClick() {
		LoadingShow();
		let fileInput = $('#PRFVendorEnhancedAttachmentFile').get(0);
		let files = fileInput.files;
		let attachmentDescription = $('#PRFVendorEnhancedAttachmentDescription').val();

		// validationMessage
		let validationMessage = '';
		validationMessage += (files.length <= 0) ? 'Please input <b>File</b><label style="color: red;">*</label></br>' : '';
		if (validationMessage) {
			LoadingClose();
			Swal.fire('Data Not Complete!', validationMessage, 'warning');
			return;
		}

		// FormData
		let formData = new FormData();
		formData.append('MainCategory', 'PRFVendorEnhanced');
		formData.append('Category', 'PRFVendorEnhanced');
		formData.append('TableName', 'PRFVendorEnhanced');
		formData.append('Description', attachmentDescription);
		formData.append('CreatedBy', '@ViewBag.Username');
		formData.append('LastUpdatedBy', '@ViewBag.Username');
		formData.append('File', files[0]);

		// ajax
		$.ajax({
			type: 'POST'
			, url: `${$baseurl}/Attachment/UploadAttachment`
			, data: formData
			, contentType: false
			, processData: false
			, error: function (error) {
				LoadingClose();
				Swal.fire('Failed!', error.responseJSON.data, 'error');
				return;
			}
			, success: function (response) {
				LoadingClose();
				let attachmentId = response.data.id;
				let attachmentName = response.data.fileName;

				let tr = `
									<tr>
										<td>
											<a
												onclick="DownloadAttachment(${attachmentId})">
												<span class="glyphicon glyphicon-file" />
												${attachmentName}
											</a>
										</td>
										<td><span class="spanDDEnhancedAttachmentId" hidden>${attachmentId}</span></td>
										<td>${$('.spanPSCreatedDate').text()}</td>
										<td><span class="spanDDEnhancedCommentDoc">${attachmentDescription}</span></td>
										<td>
											<a
												class="btn-delete-attachment-DDEhnhanced"
												onclick="DeleteAttachment(${attachmentId}); DeleteDDEnhancedAttachment(this)">
												<span class="glyphicon glyphicon-trash text-danger" />
											</a>
										</td>
									</tr>
									`;
				$('#PRFVendorEnhancedAttachmentsTBody').append(tr);
				$('#PRFVendorEnhancedAttachmentFile').val('').prop('disabled', true);
				$('#PRFVendorEnhancedAttachmentDescription').val('').prop('disabled', true);
				$('#PRFVendorEnhancedAttachmentUpload').prop('disabled', true).prop('hidden', true);
			}
		});
	}

	// PRFVendorEnhanced Save OnClick
	function PRFVendorEnhancedSaveOnClick() {
		LoadingShow();
		const createdBy = '@ViewBag.Username';
		const prfId = @ViewBag.PRFID;
		let pvedTRList = $('#PRFVendorEnhancedDetailTBody tr');
		let prfVendorEnhancedDetailList = [];

		// validationMessage
		let validationMessage = '';
		pvedTRList.each(function (index, item) {
			item = $(item);
			let docType_SubCategoryId = parseInt(item.find('.PRFVendorEnhancedDetailSubCategoryId').text());
			let docType_SubCategoryName = item.find('.PRFVendorEnhancedDetailSubCategoryName').text();
			let remarks = item.find('.PRFVendorEnhancedDetailRemarks').val();
			let attachmentId = parseInt(item.find('.PRFVendorEnhancedDetailAttachmentId').text());
			let rationalOfRisk = item.find('.PRFVendorEnhancedDetailRationalOfRisk:checked').val() ?? '';
			let prfVendorEnhancedDetailId = parseInt(item.find('.PRFVendorEnhancedDetailId').text()) ?? null;

			// not fill all & not empty all
			// if (!(rationalOfRisk && remarks && attachmentId) && !(!rationalOfRisk && !remarks && !attachmentId) && rationalOfRisk != 'No') {
			// 	validationMessage += (!rationalOfRisk) ? `Please check ${docType_SubCategoryName} <b>Rationale of Risk Acceptance</b></br>` : '';
			// 	validationMessage += (!remarks) ? `Please fill ${docType_SubCategoryName} <b>Remarks</b></br>` : '';
			// 	validationMessage += (!attachmentId) ? `Please upload ${docType_SubCategoryName} <b>Attachment</b></br>` : '';
			// }

			if ((remarks && attachmentId) && !rationalOfRisk) {
				validationMessage += (!rationalOfRisk) ? `Please check ${docType_SubCategoryName} <b>Rationale of Risk Acceptance</b></br>` : '';
			}

			prfVendorEnhancedDetailList.push({
				id: prfVendorEnhancedDetailId
				, createdBy: createdBy
				, docType_SubCategoryId: docType_SubCategoryId
				, remarks: remarks
				, attachmentId: attachmentId
				, rationalOfRisk: rationalOfRisk
			});
		});
		let vendorDDId = parseInt($('.spanVendorDDId').text());
		let attachmentId = parseInt($('.spanDDEnhancedAttachmentId').text());
		let notes = $('.spanDDEnhancedCommentDoc').text();
		let prfVendorEnhancedId = parseInt($('.PRFVendorEnhancedId').text());

		// validationMessage
		if (validationMessage) {
			LoadingClose();
			Swal.fire('Data Not Complete!', validationMessage, 'warning');
			return;
		}

		// PRFVendorEnhancedPostRequestModel
		let data = {
			prf: {
				id: prfId
				, prfVendorEnhanced: {
					id: prfVendorEnhancedId
					, createdBy: createdBy
					, prfVendorEnhancedDetailList: prfVendorEnhancedDetailList
					, attachmentList: [
					]
				}
			}
		};

		// ajax
		$.ajax({
			type: 'POST'
			, contentType: 'application/json'
			, url: `${$baseurl}/ProcurmentBuyer/PRFVendorEnhancedPost`
			, data: JSON.stringify(data)
			, error: function (response) {
				LoadingClose();
				Swal.fire('Failed!', response.data, 'error');
			}
			, success: function (response) {
				LoadingClose();
				if (response.code !== 200) {
					Swal.fire('Error!', response.data, 'error');
				} else if (response.code === 200) {
					$('#btnContinueEnhancedDD').show().prop('disabled', false);
					$('#formPRFVendorEnhancedAttachmentUploadOnClick').hide();
					$('.btn-delete-attachment-DDEhnhanced').remove();
					$('.PRFVendorEnhancedId').text(response.data.prf.prfVendorEnhanced.id);
					$('#btnNav6').show();
					Swal.fire('Success!', `Upload Enhanced is success`, 'success');
				}
			}
		});
	}

	function DeleteRowAttachment(t) {
		t = $(t);
		t.closest('tr').remove();
	}

	function DeleteDDEnhancedAttachment(t) {
		DeleteRowAttachment(t);
		$('#PRFVendorEnhancedAttachmentFile').prop('disabled', false);
		$('#PRFVendorEnhancedAttachmentDescription').prop('disabled', false);
		$('#PRFVendorEnhancedAttachmentUpload').prop('disabled', false).prop('hidden', false);
	}

	function PRFVendorEnhancedDetailAttachmentUploadOnClick(t, buttonAttachmentId) {
		LoadingShow();
		t = $(t);
		buttonAttachmentId = $(`#${buttonAttachmentId}`);
		let fileInput = t.closest('tr').find('.inputPRFVendorEnhancedDetailAttachmentUpload').get(0);
		let files = fileInput.files;
		let remarks = buttonAttachmentId.parents().eq(1).find('.PRFVendorEnhancedDetailRemarks').val();

		// validationMessage
		let validationMessage = '';
		validationMessage += (files.length <= 0) ? 'Please input <b>File</b><label style="color: red;">*</label></br>' : '';
		validationMessage += (remarks.length <= 0) ? 'Please input <b>Remarks</b><label style="color: red;">*</label></br>' : '';
		if (validationMessage) {
			LoadingClose();
			Swal.fire('Data Not Complete!', validationMessage, 'warning');
			return;
		}

		// FormData
		let formData = new FormData();
		formData.append('MainCategory', 'PRFVendorEnhancedDetail');
		formData.append('Category', 'PRFVendorEnhancedDetail');
		formData.append('TableName', 'PRFVendorEnhancedDetail');
		formData.append('Description', remarks);
		formData.append('CreatedBy', '@ViewBag.Username');
		formData.append('LastUpdatedBy', '@ViewBag.Username');
		formData.append('File', files[0]);

		// ajax
		$.ajax({
			type: 'POST'
			, url: `${$baseurl}/Attachment/UploadAttachment`
			, data: formData
			, contentType: false
			, processData: false
			, error: function (error) {
				LoadingClose();
				Swal.fire('Failed!', error.responseJSON.data, 'error');
			}
			, success: function (response) {
				LoadingClose();
				let attachmentId = response.data.id;
				let attachmentName = response.data.fileName;
				$('tbody#PRFVendorEnhancedDetailAttachmentTBody').empty();
				const createdTime_ISO = new Intl.DateTimeFormat('sv-SE', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(new Date(response.data.createdTime));
				let tr = `
									<tr>
										<td>${attachmentName}</td>
										<td>${createdTime_ISO}</td>
										<td>
											<button
												type="button"
												class="btn btn-primary"
												onclick="DownloadAttachment(${attachmentId})">
												<span class="glyphicon glyphicon-save">
												</span>
											</button>

											<button
												type="button"
												class="btn btn-danger btn-delete-attachment-modal"
												onclick="DeleteRowDDEnhacedDetailAttachment(this, ${attachmentId}, '${buttonAttachmentId.attr('id')}')">
												<span class="glyphicon glyphicon-trash">
												</span>
											</button>
										</td>
									</tr>
									`;
				$('tbody#PRFVendorEnhancedDetailAttachmentTBody').append(tr);

				buttonAttachmentId.attr('onclick', `PRFVendorEnhancedDetailAttachmentOpenOnClick(this, '${attachmentId}')`);
				buttonAttachmentId.closest('td').find('.PRFVendorEnhancedDetailAttachmentId').text(attachmentId);
				buttonAttachmentId.closest('td').find('.icon-attachment').removeClass('glyphicon-remove');
				buttonAttachmentId.closest('td').find('.icon-attachment').addClass('glyphicon-ok');
				buttonAttachmentId.closest('td').find('.icon-attachment').css('color', 'green');
			}
		});
	}

	function DeleteRowDDEnhacedDetailAttachment(t, attachmentId, buttonAttachmentId) {
		DeleteAttachment(attachmentId);

		$('tbody#PRFVendorEnhancedDetailAttachmentTBody').empty();
		let tr = `
							<tr>
								<td class="text-center text-danger" colspan="3">Data not found</td>
							</tr>
							<tr>
								<td class="text-center" colspan="2">
									<input type="file" class="form-control inputPRFVendorEnhancedDetailAttachmentUpload" />
								</td>
								<td>
									<button
										class="btn btn-success btn-upload-attachment-modal"
										onclick="PRFVendorEnhancedDetailAttachmentUploadOnClick(this, '${buttonAttachmentId}')"
										type="button">
										Upload
									</button>
								</td>
							</tr>
							`;
		$('tbody#PRFVendorEnhancedDetailAttachmentTBody').append(tr);

		$(`#${buttonAttachmentId}`).attr('onclick', `PRFVendorEnhancedDetailAttachmentOpenOnClick(this)`);
		$(`#${buttonAttachmentId}`).closest('td').find('.PRFVendorEnhancedDetailAttachmentId').text('');
		$(`#${buttonAttachmentId}`).closest('td').find('.icon-attachment').removeClass('glyphicon-ok');
		$(`#${buttonAttachmentId}`).closest('td').find('.icon-attachment').addClass('glyphicon-remove');
		$(`#${buttonAttachmentId}`).closest('td').find('.icon-attachment').css('color', 'red');
	}

</script>
