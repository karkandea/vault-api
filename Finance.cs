using APS_REST_API.Payloads.Request.Finance;
using APS_REST_API.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APS_REST_API.Models.FinanceVoucherRequest;
using DocumentFormat.OpenXml.Office2019.Drawing.Model3D;

namespace APS_REST_API.Queries
{
    public class Finance
    {
        #region Fin Maker Get Request
        public static string GetApprovalRequestListRI(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            if (param.RequestType == "RI")
            { subQuery = String.Concat(subQuery, " AND RequestType = 'Reimbursement' "); }
            else if (param.RequestType == "CATR") { subQuery = String.Concat(subQuery, " AND RequestType = 'Cash Advance Travel' "); }
            else { subQuery = String.Concat(subQuery, " AND RequestType = 'Cash Advance' "); }

            string qry = $@"SELECT DISTINCT 
							    sub.RequestNumber,
							    sub.RequestorName, 
							    sub.RequestorCostCenter, 
								sub.Status, 
								sub.StatusId, 
								sub.VendorName, 
								sub.VendorId, 
								sub.GrandTotal, 
								sub.Description, 
								sub.TransactionDate, 
								sub.CreatedTime, 
								sub.RequestType, 
								sub.Category,
								sub.TotalApproval, 
								sub.TotalApprovalApproved, 
								sub.TotalApprovalRejected, 
								sub.ReasonReject, 
								sub.LastUpdatedTime,
								sub.LastUpdatedBy,
								COUNT(*) OVER () as CountData FROM (
		
								  SELECT 
								  DISTINCT r.RequestNumber AS RequestNumber, 
								  ISNULL(
								    ar.RequestorUserName, r.CreatedBy
								  ) AS RequestorName, 
								  cc.[Name] AS RequestorCostCenter, 
								  (
								    SELECT 
								      TOP 1 ShortDescription 
								    FROM 
								      MasterTable 
								    WHERE 
								      Category = 'ApprovalRequest.Status' 
								      AND ValueId = ISNULL(ar.Status, r.Status)
								  ) AS [Status], 
								  ar.Status [StatusId], 
							      r.ReasonReject, 
								  v.Name as VendorName, 
								  v.Id as VendorId,
								  (
								    SELECT 
								      SUM(rdcsub.Amount) 
								    FROM 
								      ReimbursementDetailCostCenter rdcsub 
								      JOIN ReimbursementDetail rdsub on rdcsub.ReimbursementDetailId = rdsub.Id 
								    WHERE 
								      rdsub.ReimbursementId = r.id
								  ) as [GrandTotal], 
								  r.Description, 
								 
								  (SELECT CONVERT(datetime, MAX(ApprovalDate), 103)  from [ApprovalRequestGroupMember] where ApprovalRequestId =  ar.id) as [TransactionDate], 
								  r.CreatedTime, 
								  r.LastUpdatedTime, 
								  r.LastUpdatedBy, 
								  sc.SubCategoryName [RequestType], 
								  scv.SubCategoryName [Category],
								  CONVERT(
								    INT, 
								    ISNULL(ta.TotalApproval, 0)
								  ) [TotalApproval], 
								  CONVERT(
								    INT, 
								    ISNULL(ta2.TotalApprovalApproved, 0)
								  ) [TotalApprovalApproved], 
								  CONVERT(
								    INT, 
								    ISNULL(ta3.TotalApprovalRejected, 0)
								  ) [TotalApprovalRejected] 
								FROM 
								  dbo.[Reimbursement] r 
								  JOIN dbo.ReimbursementDetail rd on rd.ReimbursementId = r.Id 
								  LEFT JOIN dbo.[ApprovalRequest] ar ON ar.Id = r.ApprovalRequestId 
								  LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id 
								  LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id 
								  JOIN dbo.[CostCenter] cc ON r.CostCenterId = cc.Id 
								  JOIN dbo.[SubCategory] sc ON r.ReimbursementType_SubCategoryId = sc.Id 
								  JOIN dbo.[Vendor] v on v.Id = rd.VendorId 
								  JOIN dbo.[SubCategory] scv ON scv.Id = v.SubCategoryId
								  LEFT JOIN (
								    SELECT 
								      ar.Id, 
								      COUNT(*) TotalApproval 
								    FROM 
								      ApprovalRequestGroupMember argm 
								      JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id 
								    GROUP BY 
								      ar.Id
								  ) ta ON ar.Id = ta.Id 
								  LEFT JOIN (
								    SELECT 
								      ar.Id, 
								      COUNT(*) TotalApprovalApproved 
								    FROM 
								      ApprovalRequestGroupMember argm 
								      JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id 
								    WHERE 
								      argm.Status = 2 
								    GROUP BY 
								      ar.Id
								  ) ta2 ON ar.Id = ta2.Id 
								  LEFT JOIN (
								    SELECT 
								      ar.Id, 
								      COUNT(*) TotalApprovalRejected 
								    FROM 
								      ApprovalRequestGroupMember argm 
								      JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id 
								    WHERE 
								      argm.Status = 3 
								    GROUP BY 
								      ar.Id
								  ) ta3 ON ar.Id = ta3.Id 
								WHERE 
								  r.RequestNumber IS NOT NULL 
								  AND ar.Status IS NOT NULL
								  AND TotalApproval = TotalApprovalApproved
					  ) sub
					  WHERE 1=1  AND {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public static string GetApprovalRequestListTR(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"SELECT DISTINCT 
							    sub.RequestNumber,
							    sub.RequestorName, 
							    sub.RequestorCostCenter, 
								sub.Status, 
								sub.StatusId, 
								sub.VendorName, 
								sub.VendorId, 
								sub.GrandTotal, 
								sub.Description, 
								sub.TransactionDate, 
								sub.CreatedTime, 
								sub.RequestType, 
								sub.TotalApproval, 
								sub.TotalApprovalApproved, 
								sub.TotalApprovalRejected, 
								sub.ReasonReject, 
								sub.LastUpdatedTime, 
								sub.LastUpdatedBy, 
								COUNT(*) OVER () as CountData FROM (
		
						     SELECT DISTINCT ar.RequestNo AS RequestNumber, ar.RequestorUserName AS RequestorName, cc.[Name] AS RequestorCostCenter, 
							(	SELECT TOP 1 ShortDescription FROM MasterTable
								WHERE Category = 'ApprovalRequest.Status'
								AND ValueId = ar.Status
							)	AS [Status],
							ar.Status [StatusId], 
							'-' VendorName,
							'' VendorId,
							'' ReasonReject,
							0 [GrandTotal],
							'-' [Description],
							tr.LastUpdatedTime,
							tr.LastUpdatedBy,
							CONVERT(VARCHAR(20),ar.CreatedTime,106) AS TransactionDate, ar.CreatedTime, s.CountData, c.RequestType, CONVERT(INT, ISNULL(ta.TotalApproval, 0)) [TotalApproval], CONVERT(INT, ISNULL(ta2.TotalApprovalApproved, 0)) [TotalApprovalApproved], CONVERT(INT, ISNULL(ta3.TotalApprovalRejected, 0)) [TotalApprovalRejected]
							FROM  dbo.[TravelRequest] tr 
							LEFT JOIN dbo.[ApprovalRequest] ar ON ar.Id = tr.ApprovalRequestId
							LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id
							LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id
							JOIN dbo.[CostCenter] cc ON ar.CostCenterId = cc.Id
							OUTER APPLY (
								SELECT COUNT(distinct ar.Id) AS CountData
								FROM dbo.[TravelRequest] tr 
								LEFT JOIN dbo.[ApprovalRequest] ar ON ar.Id = tr.ApprovalRequestId
								LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id
								LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id
								JOIN dbo.[CostCenter] cc ON ar.CostCenterId = cc.Id
								WHERE ar.RequestNo IS NOT NULL 
							) s
							JOIN (
										SELECT ar.Id, (CASE WHEN ar.RequestNo LIKE '%RI%' THEN 'Reimbursement' 
															WHEN ar.RequestNo LIKE '%CA%' THEN 'Cash Advance'
															WHEN ar.RequestNo LIKE '%TR%' THEN 'Travel'
															WHEN ar.RequestNo LIKE '%SC%' THEN 'Shopping Cart'
														END) AS RequestType
										FROM ApprovalRequest ar
										where ar.RequestNo LIKE '%TR%' 
								 ) c ON ar.Id = c.Id
							LEFT JOIN (
								SELECT ar.Id, COUNT(*) TotalApproval FROM ApprovalRequestGroupMember argm 
								JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id
								GROUP BY ar.Id
							) ta ON ar.Id = ta.Id
							LEFT JOIN (
								SELECT ar.Id, COUNT(*) TotalApprovalApproved FROM ApprovalRequestGroupMember argm 
								JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id
								WHERE argm.Status = 2
								GROUP BY ar.Id
							) ta2 ON ar.Id = ta2.Id
							LEFT JOIN (
								SELECT ar.Id, COUNT(*) TotalApprovalRejected FROM ApprovalRequestGroupMember argm 
								JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id
								WHERE argm.Status = 3
								GROUP BY ar.Id
							) ta3 ON ar.Id = ta3.Id
							WHERE ar.RequestNo IS NOT NULL
						  ) sub
						  WHERE 1=1 AND {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public static string GetApprovalRequestListSTL(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"SELECT DISTINCT 
							    sub.RequestNumber,
							    sub.RequestorName, 
							    sub.RequestorCostCenter, 
								sub.Status, 
								sub.StatusId, 
								sub.VendorName, 
								sub.VendorId, 
								sub.GrandTotal, 
								sub.Description, 
								sub.TransactionDate, 
								sub.CreatedTime, 
								sub.RequestType, 
								sub.TotalApproval, 
								sub.TotalApprovalApproved, 
								sub.TotalApprovalRejected, 
								sub.ReasonReject, 
								sub.LastUpdatedTime, 
								sub.LastUpdatedBy, 
								sub.Category,
								COUNT(*) OVER () as CountData FROM (
		
								--Settlement
								  SELECT 
								  DISTINCT s.SettlementNumber AS RequestNumber, 
								  ISNULL(s.CreatedBy, r.CreatedBy) AS RequestorName, 
								  cc.[Name] AS RequestorCostCenter, 
								  (SELECT TOP 1 ShortDescription FROM MasterTable WHERE Category = 'ApprovalRequest.Status' AND ValueId = ISNULL(s.Status, r.Status)) AS [Status], 
								  s.Status [StatusId], 
								  v.Name as VendorName, 
								  v.Id as VendorId,
								  s.ReasonReject, 
								  (SELECT SUM(Amount) FROM SettlementDetail WHERE SettlementId = s.Id) as [GrandTotal], 
								  r.Description, 
								  '-' [GenerateId], 
								  '-' [InvoiceStatus], 
								  (SELECT CONVERT(datetime, MAX(ApprovalDate), 103)  from [ApprovalRequestGroupMember] where ApprovalRequestId =  ar.id) as [TransactionDate], 
								  r.CreatedTime, 
								  s.LastUpdatedTime, 
								  s.LastUpdatedBy, 
								  --rd.InvoiceNo,
								  'Settlement' as [RequestType], 
								  scv.SubCategoryName [Category],
								  CONVERT(INT,ISNULL(ta.TotalApproval, 0)) [TotalApproval], 
								  CONVERT(INT,ISNULL(ta2.TotalApprovalApproved, 0)) [TotalApprovalApproved], 
								  CONVERT(INT,ISNULL(ta3.TotalApprovalRejected, 0)) [TotalApprovalRejected] 
								FROM 
								  dbo.[Reimbursement] r 
								  JOIN dbo.ReimbursementDetail rd on rd.ReimbursementId = r.Id 
								  JOIN Settlement s on s.ReimbursementId = r.Id
								  JOIN SettlementDetail sd on sd.SettlementId = s.Id
								  LEFT JOIN dbo.[ApprovalRequest] ar ON ar.RequestNo = s.SettlementNumber 
								  LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id 
								  LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id
								  JOIN dbo.[CostCenter] cc ON r.CostCenterId = cc.Id 
								  JOIN dbo.[SubCategory] sc ON r.ReimbursementType_SubCategoryId = sc.Id 
								  JOIN dbo.[Vendor] v on v.Id = rd.VendorId 
								  JOIN dbo.[SubCategory] scv on scv.Id  = v.SubCategoryId
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApproval FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id GROUP BY ar.Id) ta ON ar.Id = ta.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalApproved FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 2 GROUP BY ar.Id) ta2 ON ar.Id = ta2.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalRejected FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 3 GROUP BY ar.Id) ta3 ON ar.Id = ta3.Id 
								WHERE 
								  r.RequestNumber IS NOT NULL 
								  AND ar.Status IS NOT NULL
								  AND NOT EXISTS
									 ( SELECT *
									   FROM   ApprovalRequest ar2
									   WHERE  s.SettlementNumber = ar2.RequestNo AND ar.ApprovalFlowId <> ar2.ApprovalFlowId)
								  AND TotalApproval = TotalApprovalApproved --All Approval Completed
								  ) sub
							WHERE 1=1 AND {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public static string GetApprovalRequestListTRSTL(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"SELECT DISTINCT 
							    sub.RequestNumber,
							    sub.RequestorName, 
							    sub.RequestorCostCenter, 
								sub.Status, 
								sub.StatusId, 
								sub.VendorName, 
								sub.VendorId, 
								sub.GrandTotal, 
								sub.Description, 
								sub.TransactionDate, 
								sub.CreatedTime, 
								sub.RequestType, 
								sub.TotalApproval, 
								sub.TotalApprovalApproved, 
								sub.TotalApprovalRejected, 
								sub.ReasonReject, 
								sub.LastUpdatedTime, 
								sub.LastUpdatedBy, 
								sub.Category,
								COUNT(*) OVER () as CountData FROM (
		
								--Travel Settlement
								  SELECT 
								  DISTINCT r.RequestNumber,
								  ISNULL(ar.CreatedBy, r.CreatedBy) AS RequestorName, 
								  cc.[Name] AS RequestorCostCenter, 
								  (SELECT TOP 1 ShortDescription FROM MasterTable WHERE Category = 'ApprovalRequest.Status' AND ValueId = ISNULL(ar.Status, r.Status)) AS [Status], 
								  ar.Status [StatusId], 
								  '-' [VendorName], 
								  '-' [VendorId],
								  r.ReasonReject, 
								  r.GrandTotal, 
								  ar2.RequestNo [Description], 
								  '-' [GenerateId],  
								  '-' [InvoiceStatus], 
								  (SELECT CONVERT(datetime, MAX(ApprovalDate), 103)  from [ApprovalRequestGroupMember] where ApprovalRequestId =  ar.id) as [TransactionDate], 
								  r.CreatedTime, 
								  r.LastUpdatedTime, 
								  r.LastUpdatedBy, 
								  --rd.InvoiceNo,
								  'Travel Settlement' as [RequestType], 
								  'Staff' [Category],
								  CONVERT(INT,ISNULL(ta.TotalApproval, 0)) [TotalApproval], 
								  CONVERT(INT,ISNULL(ta2.TotalApprovalApproved, 0)) [TotalApprovalApproved], 
								  CONVERT(INT,ISNULL(ta3.TotalApprovalRejected, 0)) [TotalApprovalRejected] 
								FROM 
								  dbo.TravelRequestExpense r 
								  LEFT JOIN dbo.[ApprovalRequest] ar ON ar.Id = r.ApprovalRequestId 
								  LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id 
								  LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id 
								  JOIN dbo.[CostCenter] cc ON r.CostCenterId = cc.Id 
								  JOIN TravelRequest tr on tr.Id = r.TravelRequestId
								  JOIN ApprovalRequest ar2 on ar2.Id = tr.ApprovalRequestId
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApproval FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id GROUP BY ar.Id) ta ON ar.Id = ta.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalApproved FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 2 GROUP BY ar.Id) ta2 ON ar.Id = ta2.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalRejected FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 3 GROUP BY ar.Id) ta3 ON ar.Id = ta3.Id 
								WHERE 
								  r.RequestNumber IS NOT NULL 
								  AND ar.Status IS NOT NULL
								  AND NOT EXISTS
									 ( SELECT *
									   FROM   ApprovalRequest ar2
									   WHERE  ar.RequestNo = ar2.RequestNo AND ar.ApprovalFlowId <> ar2.ApprovalFlowId AND ar2.Status IN (1,2) )
								  AND TotalApproval = TotalApprovalApproved --All Approval Completed
								  ) sub
							WHERE 1=1 AND {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public static string GetApprovalRequestListINVTR(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"SELECT DISTINCT 
							    sub.RequestNumber,
							    sub.RequestorName, 
							    sub.RequestorCostCenter, 
								sub.Status, 
								sub.StatusId, 
								sub.VendorName, 
								sub.VendorId, 
								sub.GrandTotal, 
								sub.Description, 
								sub.TransactionDate, 
								sub.CreatedTime, 
								sub.RequestType, 
								sub.TotalApproval, 
								sub.TotalApprovalApproved, 
								sub.TotalApprovalRejected, 
								sub.ReasonReject, 
								sub.LastUpdatedTime, 
								sub.LastUpdatedBy, 
								sub.Category,
								COUNT(*) OVER () as CountData FROM (
		
								--Travel Settlement
								  SELECT 
								  DISTINCT r.RequestNumber,
								  ISNULL(ar.CreatedBy, r.CreatedBy) AS RequestorName, 
								  fu.CostCenterName AS RequestorCostCenter, 
								  (SELECT TOP 1 ShortDescription FROM MasterTable WHERE Category = 'ApprovalRequest.Status' AND ValueId = ISNULL(ar.Status, r.Status)) AS [Status], 
								  ar.Status [StatusId], 
								  '-' [VendorName], 
								  '-' [VendorId],
								  r.ReasonReject, 
								  r.Amount [GrandTotal], 
								  r.Description, 
								  '-' [GenerateId],  
								  '-' [InvoiceStatus], 
								  (SELECT CONVERT(datetime, MAX(ApprovalDate), 103)  from [ApprovalRequestGroupMember] where ApprovalRequestId =  ar.id) as [TransactionDate], 
								  r.CreatedTime, 
								  r.LastUpdatedTime, 
								  r.LastUpdatedBy, 
								  --rd.InvoiceNo,
								  'Invoice Travel' as [RequestType], 
								  'Staff' [Category],
								  CONVERT(INT,ISNULL(ta.TotalApproval, 0)) [TotalApproval], 
								  CONVERT(INT,ISNULL(ta2.TotalApprovalApproved, 0)) [TotalApprovalApproved], 
								  CONVERT(INT,ISNULL(ta3.TotalApprovalRejected, 0)) [TotalApprovalRejected] 
								FROM 
								  dbo.InvoiceTravel r 
								  LEFT JOIN dbo.[ApprovalRequest] ar ON ar.Id = r.ApprovalRequestId 
								  LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id 
								  LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id 
								  LEFT JOIN Flips.UserAccount fu on fu.Id = r.RequestorAccountId
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApproval FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id GROUP BY ar.Id) ta ON ar.Id = ta.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalApproved FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 2 GROUP BY ar.Id) ta2 ON ar.Id = ta2.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalRejected FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 3 GROUP BY ar.Id) ta3 ON ar.Id = ta3.Id 
								WHERE 
								  r.RequestNumber IS NOT NULL 
								  AND ar.Status IS NOT NULL
								  AND NOT EXISTS
									 ( SELECT *
									   FROM   ApprovalRequest ar2
									   WHERE  ar.RequestNo = ar2.RequestNo AND ar.ApprovalFlowId <> ar2.ApprovalFlowId AND ar2.Status IN (1,2) )
								  AND TotalApproval = TotalApprovalApproved --All Approval Completed
								  ) sub
							WHERE 1=1 AND {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public static string GetApprovalRequestListGER(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"SELECT DISTINCT 
							    sub.RequestNumber,
							    sub.RequestorName, 
							    sub.RequestorCostCenter, 
								sub.Status, 
								sub.StatusId, 
								sub.VendorName, 
								sub.VendorId, 
								sub.GrandTotal, 
								sub.Description, 
								sub.TransactionDate, 
								sub.CreatedTime, 
								sub.RequestType, 
								sub.TotalApproval, 
								sub.TotalApprovalApproved, 
								sub.TotalApprovalRejected, 
								sub.ReasonReject, 
								sub.LastUpdatedTime, 
								sub.LastUpdatedBy, 
								sub.Category,
								COUNT(*) OVER () as CountData FROM (
		
								--GER
								SELECT 
								 DISTINCT r.RequestNumber,
								 ISNULL(ar.CreatedBy, r.CreatedBy) AS RequestorName, 
								 fu.CostCenterName AS RequestorCostCenter, 
								 (SELECT TOP 1 ShortDescription FROM MasterTable WHERE Category = 'ApprovalRequest.Status' AND ValueId = ISNULL(ar.Status, r.Status)) AS [Status], 
								 ar.Status [StatusId], 
								 r.ReasonReject, 
								 '-' [VendorName], 
								 '-' [VendorId],
								 ISNULL((SELECT SUM(NettAmount) FROM GerDetail WHERE GerHeaderId =  r.Id ), 0) [GrandTotal],
								 r.[Description], 
								 '-' [GenerateId], 
								 '-' [InvoiceStatus], 
								 (SELECT CONVERT(datetime, MAX(ApprovalDate), 103)  from [ApprovalRequestGroupMember] where ApprovalRequestId =  ar.id) as [TransactionDate], 
								 r.CreatedTime, 
								 r.LastUpdatedTime, 
								 r.LastUpdatedBy, 
								 --rd.InvoiceNo,
								 'GER' as [RequestType], 
								 'GER' as [Type], 
								 'Staff' [Category],
								 CONVERT(INT,ISNULL(ta.TotalApproval, 0)) [TotalApproval], 
								 CONVERT(INT,ISNULL(ta2.TotalApprovalApproved, 0)) [TotalApprovalApproved], 
								 CONVERT(INT,ISNULL(ta3.TotalApprovalRejected, 0)) [TotalApprovalRejected],
								 (SELECT TOP 1 lcurrencycode from GerDetail WHERE GerHeaderId =  r.Id) [L_Currency]
								FROM 
								 dbo.GerHeader r 
						         JOIN dbo.[SubCategory] sc ON r.ExpenseType_SubCategoryId = sc.Id AND sc.SubCategoryCode = @ExpenseType
								 LEFT JOIN dbo.[ApprovalRequest] ar ON ar.RequestNo = r.RequestNumber
								 LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id 
								 LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id 
								 LEFT JOIN Flips.UserAccount fu on fu.Id = r.RequestorAccountId
								 LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApproval FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id GROUP BY ar.Id) ta ON ar.Id = ta.Id 
								 LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalApproved FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 2 GROUP BY ar.Id) ta2 ON ar.Id = ta2.Id 
								 LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalRejected FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 3 GROUP BY ar.Id) ta3 ON ar.Id = ta3.Id 

								WHERE 
								  r.RequestNumber IS NOT NULL 
								  AND ar.Status IS NOT NULL
								  AND NOT EXISTS
									 ( SELECT *
									   FROM   ApprovalRequest ar2
									   WHERE  ar.RequestNo = ar2.RequestNo AND ar.ApprovalFlowId <> ar2.ApprovalFlowId AND ar2.Status IN (1,2) )
								  AND TotalApproval = TotalApprovalApproved --All Approval Completed
								  ) sub
							WHERE 1=1 AND {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public static string GetApprovalRequestListTREXAPR(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"SELECT 
							    sub.RequestNumber,
							    sub.RequestorName, 
							    sub.RequestorCostCenter, 
								sub.Status, 
								sub.StatusId, 
								sub.VendorName, 
								sub.VendorId, 
								sub.GrandTotal, 
								sub.Description, 
								sub.TransactionDate, 
								sub.CreatedTime, 
								sub.RequestType, 
								sub.TotalApproval, 
								sub.TotalApprovalApproved, 
								sub.TotalApprovalRejected, 
								sub.ReasonReject, 
								sub.LastUpdatedTime, 
								sub.LastUpdatedBy, 
								sub.Category,
								COUNT(*) OVER () as CountData FROM (
		
								SELECT  
								    r.NoAPR AS [RequestNumber],
								    r.CreatedBy AS [RequestorName],  
								    ISNULL(cc.Name, r.DepartmentCode) AS [RequestorCostCenter],  
								    mt.ShortDescription AS [Status],  
								    r.Status AS [StatusId],  
								    '' AS [ReasonReject],  
								    r.BeneficiaryVendorName AS [VendorName],  
								    '-' AS [VendorId],
								    r.TotalAmount AS [GrandTotal],
								    r.[Description],  
								    '-' AS [GenerateId],  
								    '-' AS [InvoiceStatus],  
								    MAX(a.ApprovedAt) AS [TransactionDate],  
								    r.CreatedAt AS [CreatedTime],  
								    r.UpdatedAt AS [LastUpdatedTime],  
								    r.UpdatedBy AS [LastUpdatedBy],  
								    'TREX-APR' AS [RequestType],  
								    'APR' AS [Type],  
								    CASE r.IsVendorPayment
									 	WHEN 1 THEN 'Vendor'
									 	ELSE 'Staff'
									END as [Category],
								    COUNT(a.ApprovedBy) AS [TotalApproval],  
								    COUNT(a.ApprovedBy) AS [TotalApprovalApproved],  
								    0 AS [TotalApprovalRejected],
								    'IDR' AS [L_Currency]
								FROM dbo.TrexAPR r
								JOIN MasterTable mt ON mt.ValueId = r.Status 
								LEFT JOIN CostCenter cc ON cc.Code = r.DepartmentCode
								LEFT JOIN (
								    SELECT a.AprId, v.ApprovedAt, v.ApprovedBy
								    FROM TrexAPR a
								    CROSS APPLY (VALUES
								        (a.Approval1At, a.Approval1By),
								        (a.Approval2At, a.Approval2By),
								        (a.Approval3At, a.Approval3By),
								        (a.Approval4At, a.Approval4By),
								        (a.Approval5At, a.Approval5By),
								        (a.Approval6At, a.Approval6By),
								        (a.Approval7At, a.Approval7By),
								        (a.Approval8At, a.Approval8By)
								    ) AS v(ApprovedAt, ApprovedBy)
								    WHERE v.ApprovedBy IS NOT NULL
								) AS a ON r.AprId = a.AprId
								WHERE mt.Category = 'ApprovalRequest.Status'
								GROUP BY  
								    r.NoAPR, r.CreatedBy, cc.Name, mt.ShortDescription, r.Status, r.TotalAmount,  r.DepartmentCode, r.IsVendorPayment,
								    r.[Description], r.CreatedAt, r.UpdatedAt, r.UpdatedBy, r.SendToFinanceAt, r.BeneficiaryVendorName
								  ) sub
							WHERE 1=1 AND {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public static string GetApprovalRequestListTREXEER(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"   SELECT 
							    sub.RequestNumber,
							    sub.RequestorName, 
							    sub.RequestorCostCenter, 
								sub.Status, 
								sub.StatusId, 
								sub.VendorName, 
								sub.VendorId, 
								sub.GrandTotal, 
								sub.Description, 
								sub.TransactionDate, 
								sub.CreatedTime, 
								sub.RequestType, 
								sub.TotalApproval, 
								sub.TotalApprovalApproved, 
								sub.TotalApprovalRejected, 
								sub.ReasonReject, 
								sub.LastUpdatedTime, 
								sub.LastUpdatedBy, 
								sub.Category,
								COUNT(*) OVER () as CountData FROM (
		
								SELECT   
							         r.NoEER AS [RequestNumber],
							         r.CreatedBy AS [RequestorName],  
							         ISNULL(cc.Name, r.DepartmentCode) AS [RequestorCostCenter],  
							         mt.ShortDescription AS [Status],  
							         r.Status AS [StatusId],  
							         '' AS [ReasonReject],  
							         '-' AS [VendorName],  
							         '-' AS [VendorId],
							         r.PaidAmount AS [GrandTotal],
							         r.[Description],  
							         '-' AS [GenerateId],  
							         '-' AS [InvoiceStatus],  
							         MAX(a.ApprovedAt) AS [TransactionDate],    
							         r.CreatedAt AS [CreatedTime],  
							         r.UpdatedAt AS [LastUpdatedTime],  
							         r.UpdatedBy AS [LastUpdatedBy],  
							         'TREX-EER' AS [RequestType],  
							         'EER' AS [Type],  
							         'Staff' AS [Category],
							         COUNT(a.ApprovedBy) AS [TotalApproval],  
							         COUNT(a.ApprovedBy) AS [TotalApprovalApproved],  
							         0 AS [TotalApprovalRejected],
							         'IDR' AS [L_Currency]
							     FROM dbo.TrexEERHeader r
							     JOIN MasterTable mt ON mt.ValueId = r.Status 
							     LEFT JOIN CostCenter cc ON cc.Code = r.DepartmentCode
							     LEFT JOIN (
							         SELECT a.EERId, v.ApprovedAt, v.ApprovedBy
							         FROM TrexEERHeader a
							         CROSS APPLY (VALUES
							             (a.Approval1At, a.Approval1By),
							             (a.Approval2At, a.Approval2By),
							             (a.Approval3At, a.Approval3By),
							             (a.Approval4At, a.Approval4By),
							             (a.Approval5At, a.Approval5By),
							             (a.Approval6At, a.Approval6By),
							             (a.Approval7At, a.Approval7By),
							             (a.Approval8At, a.Approval8By)
							         ) AS v(ApprovedAt, ApprovedBy)
							         WHERE v.ApprovedBy IS NOT NULL
							     ) AS a ON r.EERId = a.EERId
							     WHERE mt.Category = 'ApprovalRequest.Status'
							     GROUP BY  
							         r.NoEER, r.CreatedBy, cc.Name, mt.ShortDescription, r.Status, r.PaidAmount,  
							         r.[Description], r.CreatedAt, r.UpdatedAt, r.UpdatedBy, r.SendToFinanceAt, r.DepartmentCode
							     ) sub
							WHERE 1=1 AND {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public static string GetApprovalRequestListTREXGER(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);
            if (param.RequestType.Contains("STL"))
                subQuery = String.Concat(subQuery, " AND Type = 'Settlement' ");
            else
                subQuery = String.Concat(subQuery, " AND Type = 'Reimbursement' ");

            string qry = $@"   SELECT 
							    sub.RequestNumber,
							    sub.RequestorName, 
							    sub.RequestorCostCenter, 
								sub.Status, 
								sub.StatusId, 
								sub.VendorName, 
								sub.VendorId, 
								sub.GrandTotal, 
								sub.Description, 
								sub.TransactionDate, 
								sub.CreatedTime, 
								sub.RequestType, 
								sub.Type, 
								sub.TotalApproval, 
								sub.TotalApprovalApproved, 
								sub.TotalApprovalRejected, 
								sub.ReasonReject, 
								sub.LastUpdatedTime, 
								sub.LastUpdatedBy, 
								sub.Category,
								COUNT(*) OVER () as CountData FROM (
		
								SELECT   
								     r.NoGER AS [RequestNumber],
								     r.CreatedBy AS [RequestorName],  
								     ISNULL(cc.Name, r.RegionCode) AS [RequestorCostCenter],  
								     mt.ShortDescription AS [Status],  
								     r.Status AS [StatusId],  
								     '' AS [ReasonReject],  
								     r.BeneficiaryVendorName AS [VendorName],  
								     '-' AS [VendorId],
								     r.PaidAmount AS [GrandTotal],
								     r.[Description],  
								     '-' AS [GenerateId],  
								     '-' AS [InvoiceStatus],  
								     MAX(a.ApprovedAt) [TransactionDate],  
								     r.CreatedAt AS [CreatedTime],  
								     r.UpdatedAt AS [LastUpdatedTime],  
								     r.UpdatedBy AS [LastUpdatedBy],  
								     'TREX-GER' AS [RequestType],  
								     r.GERType AS [Type],  
								     CASE r.IsVendorPayment
									 	WHEN 1 THEN 'Vendor'
									 	ELSE 'Staff'
									 END as [Category],
								     COUNT(a.ApprovedBy) AS [TotalApproval],  
								     COUNT(a.ApprovedBy) AS [TotalApprovalApproved],  
								     0 AS [TotalApprovalRejected],
								     'IDR' AS [L_Currency]
								 FROM dbo.TrexGERHeader r
								 LEFT JOIN CostCenter cc ON cc.Code = r.DepartmentCode
								 LEFT JOIN MasterTable mt ON mt.ValueId = r.Status  
								 LEFT JOIN (
								     SELECT a.GERId, v.ApprovedAt, v.ApprovedBy
								     FROM TrexGERHeader a
								     CROSS APPLY (VALUES
								         (a.Approval1At, a.Approval1By),
								         (a.Approval2At, a.Approval2By),
								         (a.Approval3At, a.Approval3By),
								         (a.Approval4At, a.Approval4By),
								         (a.Approval5At, a.Approval5By),
								         (a.Approval6At, a.Approval6By),
								         (a.Approval7At, a.Approval7By),
								         (a.Approval8At, a.Approval8By)
								     ) AS v(ApprovedAt, ApprovedBy)
								     WHERE v.ApprovedBy IS NOT NULL
								 ) AS a ON r.GERId = a.GERId
								 WHERE mt.Category = 'ApprovalRequest.Status'
								 GROUP BY  
								     r.NoGER, r.CreatedBy, cc.Name, mt.ShortDescription, r.Status, r.PaidAmount, r.IsVendorPayment,
								     r.[Description], r.CreatedAt, r.UpdatedAt, r.UpdatedBy, r.BeneficiaryVendorName, r.GERType, r.RegionCode, r.DepartmentCode
								  ) sub
							WHERE 1=1 AND {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public static string GetApprovalRequestListTREXTER(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"   SELECT 
							    sub.RequestNumber,
							    sub.RequestorName, 
							    sub.RequestorCostCenter, 
								sub.Status, 
								sub.StatusId, 
								sub.VendorName, 
								sub.VendorId, 
								sub.GrandTotal, 
								sub.Description, 
								sub.TransactionDate, 
								sub.CreatedTime, 
								sub.RequestType, 
								sub.Type, 
								sub.TotalApproval, 
								sub.TotalApprovalApproved, 
								sub.TotalApprovalRejected, 
								sub.ReasonReject, 
								sub.LastUpdatedTime, 
								sub.LastUpdatedBy, 
								sub.Category,
								COUNT(*) OVER () as CountData FROM (
		
								SELECT   
								    r.NoTER AS [RequestNumber],
								    r.CreatedBy AS [RequestorName],  
								    trbh.RegionCode AS [RequestorCostCenter],  
								    mt.ShortDescription AS [Status],  
								    r.Status AS [StatusId],  
								    '' AS [ReasonReject],  
								    '-' AS [VendorName],  
								    '-' AS [VendorId],
								    COALESCE(trdt.TotalAmount, 0) +
								    COALESCE(trda.TotalAmount, 0) +
								    COALESCE(trdo.TotalAmount, 0) +
								    COALESCE(trdd.TotalAmount, 0) AS [GrandTotal],
								    '' AS [Description],  
								    '-' AS [GenerateId],  
								    '-' AS [InvoiceStatus],  
								    MAX(a.ApprovedAt) AS [TransactionDate],  
								    r.CreatedAt AS [CreatedTime],  
								    r.UpdatedAt AS [LastUpdatedTime],  
								    r.UpdatedBy AS [LastUpdatedBy],  
								    'TREX-TER' AS [RequestType],  
								    'TER' AS [Type],  
								    'Staff' AS [Category],
								    COUNT(DISTINCT a.ApprovedBy) AS [TotalApproval],  
								    COUNT(DISTINCT a.ApprovedBy) AS [TotalApprovalApproved],  
								    0 AS [TotalApprovalRejected],
								    'IDR' AS [L_Currency]
								FROM dbo.TrexTERHeader r
								LEFT JOIN (
								    SELECT TERId, SUM(Amount) AS TotalAmount
								    FROM TrexTERDetailTransportation
								    GROUP BY TERId
								) trdt ON r.TERId = trdt.TERId
								LEFT JOIN (
								    SELECT TERId, SUM(Amount) AS TotalAmount
								    FROM TrexTERDetailAkomodasi
								    GROUP BY TERId
								) trda ON r.TERId = trda.TERId
								LEFT JOIN (
								    SELECT TERId, SUM(Amount) AS TotalAmount
								    FROM TrexTERDetailOther
								    GROUP BY TERId
								) trdo ON r.TERId = trdo.TERId
								LEFT JOIN (
								    SELECT TERId, SUM(Amount) AS TotalAmount
								    FROM TrexTERDetailDurasi
								    GROUP BY TERId
								) trdd ON r.TERId = trdd.TERId
								JOIN TrexBTRHeader trbh ON r.BTRId = trbh.BTRId
								LEFT JOIN MasterTable mt ON mt.ValueId = r.Status  
								LEFT JOIN (
								    SELECT a.TERId, v.ApprovedAt, v.ApprovedBy
								    FROM TrexTERHeader a
								    CROSS APPLY (VALUES
								        (a.Approval1At, a.Approval1By),
								        (a.Approval2At, a.Approval2By),
								        (a.Approval3At, a.Approval3By),
								        (a.Approval4At, a.Approval4By),
								        (a.Approval5At, a.Approval5By),
								        (a.Approval6At, a.Approval6By),
								        (a.Approval7At, a.Approval7By),
								        (a.Approval8At, a.Approval8By)
								    ) AS v(ApprovedAt, ApprovedBy)
								    WHERE v.ApprovedBy IS NOT NULL
								) a ON r.TERId = a.TERId
								WHERE mt.Category = 'ApprovalRequest.Status'
								GROUP BY   
								    r.NoTER, r.CreatedBy, mt.ShortDescription, r.Status,  
								    r.CreatedAt, r.UpdatedAt, r.UpdatedBy, trbh.RegionCode,
								    trdt.TotalAmount, trda.TotalAmount, trdo.TotalAmount, trdd.TotalAmount
								  ) sub
							   WHERE 1=1 AND {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public static string GetApprovalRequestListAll(ParamGetRequestList param)
        {
            string subQuery = $@"(RequestNumber LIKE @RequestNumber) 
								AND VendorId = @VendorId 
								AND StatusId in (2, 7) 
								AND Category = @Category
								AND Type = @Type
								AND TotalApproval = TotalApprovalApproved 
								AND CONVERT(datetime, TransactionDate, 103) BETWEEN CONVERT(varchar(50), @RequestDateFrom, 103) AND CONVERT(varchar(50), @RequestDateTo, 103) ";
            if (String.IsNullOrEmpty(param.RequestNumber))
            {
                subQuery = subQuery.Replace("(RequestNumber LIKE @RequestNumber) ", " 1=1 ");
            }
            if (param.Status == "8")
            {
                subQuery = subQuery.Replace("StatusId in (2, 7)", " StatusId in (8) ");
            }
            if (String.IsNullOrEmpty(param.VendorId))
            {
                subQuery = subQuery.Replace("VendorId = @VendorId ", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.RequestType))
            {
                subQuery = subQuery.Replace("Type = @Type", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.Category))
            {
                subQuery = subQuery.Replace("Category = @Category", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.RequestDateFrom))
            {
                subQuery = subQuery.Replace("CONVERT(datetime, TransactionDate, 103) BETWEEN CONVERT(varchar(50), @RequestDateFrom, 103) AND CONVERT(varchar(50), @RequestDateTo, 103)", " 1=1 ");
            }
            if (!String.IsNullOrEmpty(param.Search?.Value))
            {
                subQuery = String.Concat(subQuery, " AND (RequestNumber LIKE @Search OR RequestorName LIKE @Search OR VendorName LIKE @Search) ");
            }
            string qry = $@"SELECT UNIONQUERY.*, COUNT(*) OVER () as CountData FROM (
		
								--Reimbursement, Cash Advance
								  SELECT 
								  DISTINCT r.RequestNumber AS RequestNumber, 
								  ISNULL(
								    ar.RequestorUserName, r.CreatedBy
								  ) AS RequestorName, 
								  cc.[Name] AS RequestorCostCenter, 
								  (
								    SELECT 
								      TOP 1 ShortDescription 
								    FROM 
								      MasterTable 
								    WHERE 
								      Category = 'ApprovalRequest.Status' 
								      AND ValueId = ISNULL(ar.Status, r.Status)
								  ) AS [Status], 
								  ar.Status [StatusId], 
								  r.ReasonReject, 
								  v.Name as VendorName, 
								  v.Id as VendorId,
								  (
								    SELECT 
								      SUM(BasicAmount) 
								    FROM 
								      ReimbursementDetailCostCenter rdcsub 
								      JOIN ReimbursementDetail rdsub on rdcsub.ReimbursementDetailId = rdsub.Id 
								    WHERE 
								      rdsub.ReimbursementId = r.id
								  ) as [GrandTotal], 
								  r.Description, 
								  '-' [GenerateId], 
								  '-' [InvoiceStatus], 
								  (SELECT CONVERT(datetime, MAX(ApprovalDate), 103)  from [ApprovalRequestGroupMember] where ApprovalRequestId =  ar.id) as [TransactionDate], 
								  r.CreatedTime, 
								  r.LastUpdatedTime, 
								  r.LastUpdatedBy, 
								  --rd.InvoiceNo,
								  sc.SubCategoryName [RequestType],
								  c.Type,
								  scv.SubCategoryName [Category],
								  CONVERT(
								    INT, 
								    ISNULL(ta.TotalApproval, 0)
								  ) [TotalApproval], 
								  CONVERT(
								    INT, 
								    ISNULL(ta2.TotalApprovalApproved, 0)
								  ) [TotalApprovalApproved], 
								  CONVERT(
								    INT, 
								    ISNULL(ta3.TotalApprovalRejected, 0)
								  ) [TotalApprovalRejected],
								  '' LCurrency
								FROM 
								  dbo.[Reimbursement] r 
								  JOIN dbo.ReimbursementDetail rd on rd.ReimbursementId = r.Id 
								  LEFT JOIN dbo.[ApprovalRequest] ar ON ar.Id = r.ApprovalRequestId 
								  LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id 
								  LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id 
								  JOIN dbo.[CostCenter] cc ON r.CostCenterId = cc.Id 
								  JOIN dbo.[SubCategory] sc ON r.ReimbursementType_SubCategoryId = sc.Id 
								  JOIN dbo.[Vendor] v on v.Id = rd.VendorId 
								  JOIN dbo.[SubCategory] scv ON scv.Id = v.SubCategoryId
								  JOIN (
										 SELECT ar.Id, (CASE WHEN ar.RequestNo LIKE '%RI%' THEN 'RI' 
															    WHEN ar.RequestNo LIKE '%CATR%' THEN 'CATR'
															    WHEN ar.RequestNo LIKE '%CA%' THEN 'CA'
															    WHEN ar.RequestNo LIKE '%TR%' THEN 'TR'
															    WHEN ar.RequestNo LIKE '%SC%' THEN 'SC'
															    WHEN ar.RequestNo LIKE '%STL%' THEN 'STL'
														    END) AS Type
										 FROM ApprovalRequest ar
									   ) c ON ar.Id = c.Id
								  LEFT JOIN (
								    SELECT 
								      ar.Id, 
								      COUNT(*) TotalApproval 
								    FROM 
								      ApprovalRequestGroupMember argm 
								      JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id 
								    GROUP BY 
								      ar.Id
								  ) ta ON ar.Id = ta.Id 
								  LEFT JOIN (
								    SELECT 
								      ar.Id, 
								      COUNT(*) TotalApprovalApproved 
								    FROM 
								      ApprovalRequestGroupMember argm 
								      JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id 
								    WHERE 
								      (argm.Status = 2 OR argm.Status = 7)
								    GROUP BY 
								      ar.Id
								  ) ta2 ON ar.Id = ta2.Id 
								  LEFT JOIN (
								    SELECT 
								      ar.Id, 
								      COUNT(*) TotalApprovalRejected 
								    FROM 
								      ApprovalRequestGroupMember argm 
								      JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id 
								    WHERE 
								      argm.Status = 3 
								    GROUP BY 
								      ar.Id
								  ) ta3 ON ar.Id = ta3.Id 
								WHERE 
								  r.RequestNumber IS NOT NULL 
								  AND ar.Status IS NOT NULL 
							
							 UNION
								 -- TREX APR
								 SELECT   
								     r.NoAPR AS [RequestNumber],
								     r.CreatedBy AS [RequestorName],  
								     cc.Name AS [RequestorCostCenter],  
								     mt.ShortDescription AS [Status],  
								     r.Status AS [StatusId],  
								     '' AS [ReasonReject],  
								     r.BeneficiaryVendorName AS [VendorName],
								     '-' AS [VendorId],
								     r.TotalAmount AS [GrandTotal],
								     r.[Description],  
								     '-' AS [GenerateId],  
								     '-' AS [InvoiceStatus],  
								     MAX(a.ApprovedAt) AS [TransactionDate],  
								     r.CreatedAt AS [CreatedTime],  
								     r.UpdatedAt AS [LastUpdatedTime],  
								     r.UpdatedBy AS [LastUpdatedBy],  
								     'TREX-APR' AS [RequestType],  
								     'APR' AS [Type],  
								     CASE r.IsVendorPayment
									 	WHEN 1 THEN 'Vendor'
									 	ELSE 'Staff'
									 END as [Category],
								     COUNT(a.ApprovedBy) AS [TotalApproval],  
								     COUNT(a.ApprovedBy) AS [TotalApprovalApproved],  
								     0 AS [TotalApprovalRejected],
								     'IDR' AS [L_Currency]
								 FROM dbo.TrexAPR r
								 JOIN MasterTable mt ON mt.ValueId = r.Status  
								 LEFT JOIN CostCenter cc ON cc.Code = r.DepartmentCode
								 LEFT JOIN (
								     SELECT a.AprId, v.ApprovedAt, v.ApprovedBy
								     FROM TrexAPR a
								     CROSS APPLY (VALUES
								         (a.Approval1At, a.Approval1By),
								         (a.Approval2At, a.Approval2By),
								         (a.Approval3At, a.Approval3By),
								         (a.Approval4At, a.Approval4By),
								         (a.Approval5At, a.Approval5By),
								         (a.Approval6At, a.Approval6By),
								         (a.Approval7At, a.Approval7By),
								         (a.Approval8At, a.Approval8By)
								     ) AS v(ApprovedAt, ApprovedBy)
								     WHERE v.ApprovedBy IS NOT NULL
								 ) AS a ON r.AprId = a.AprId
								 WHERE mt.Category = 'ApprovalRequest.Status'
								 GROUP BY  
								     r.NoAPR, r.CreatedBy, cc.Name, mt.ShortDescription, r.Status, r.TotalAmount, r.IsVendorPayment,
								     r.[Description], r.CreatedAt, r.UpdatedAt, r.UpdatedBy, r.BeneficiaryVendorName
							UNION
								 -- TREX EER
								 SELECT   
								    r.NoEER AS [RequestNumber],
								    r.CreatedBy AS [RequestorName],  
								    cc.Name AS [RequestorCostCenter],  
								    mt.ShortDescription AS [Status],  
								    r.Status AS [StatusId],  
								    '' AS [ReasonReject],  
								    '-' AS [VendorName],  
								    '-' AS [VendorId],
								    r.TotalAmount AS [GrandTotal],
								    r.[Description],  
								    '-' AS [GenerateId],  
								    '-' AS [InvoiceStatus],  
								    MAX(a.ApprovedAt) AS [TransactionDate],  
								    r.CreatedAt AS [CreatedTime],  
								    r.UpdatedAt AS [LastUpdatedTime],  
								    r.UpdatedBy AS [LastUpdatedBy],  
								    'TREX-EER' AS [RequestType],  
								    'EER' AS [Type],  
								    'Staff' AS [Category],
								    COUNT(a.ApprovedBy) AS [TotalApproval],  
								    COUNT(a.ApprovedBy) AS [TotalApprovalApproved],  
								    0 AS [TotalApprovalRejected],
								    'IDR' AS [L_Currency]
								FROM dbo.TrexEERHeader r
								JOIN MasterTable mt ON mt.ValueId = r.Status  
								LEFT JOIN CostCenter cc ON cc.Code = r.DepartmentCode
								LEFT JOIN (
								    SELECT a.EERId, v.ApprovedAt, v.ApprovedBy
								    FROM TrexEERHeader a
								    CROSS APPLY (VALUES
								        (a.Approval1At, a.Approval1By),
								        (a.Approval2At, a.Approval2By),
								        (a.Approval3At, a.Approval3By),
								        (a.Approval4At, a.Approval4By),
								        (a.Approval5At, a.Approval5By),
								        (a.Approval6At, a.Approval6By),
								        (a.Approval7At, a.Approval7By),
								        (a.Approval8At, a.Approval8By)
								    ) AS v(ApprovedAt, ApprovedBy)
								    WHERE v.ApprovedBy IS NOT NULL
								) AS a ON r.EERId = a.EERId
								WHERE mt.Category = 'ApprovalRequest.Status'
								GROUP BY  
								    r.NoEER, r.CreatedBy, cc.Name, mt.ShortDescription, r.Status, r.TotalAmount,  
								    r.[Description], r.CreatedAt, r.UpdatedAt, r.UpdatedBy

							UNION
								 -- TREX GER
								 SELECT   
								     r.NoGER AS [RequestNumber],
								     r.CreatedBy AS [RequestorName],  
								     cc.Name AS [RequestorCostCenter],  
								     mt.ShortDescription AS [Status],  
								     r.Status AS [StatusId],  
								     '' AS [ReasonReject],  
								     r.BeneficiaryVendorName AS [VendorName], 
								     '-' AS [VendorId],
								     r.TotalAmount AS [GrandTotal],
								     r.[Description],  
								     '-' AS [GenerateId],  
								     '-' AS [InvoiceStatus],  
								     MAX(a.ApprovedAt) AS [TransactionDate],  
								     r.CreatedAt AS [CreatedTime],  
								     r.UpdatedAt AS [LastUpdatedTime],  
								     r.UpdatedBy AS [LastUpdatedBy],  
								     'TREX-GER' AS [RequestType],  
								     'GER' AS [Type],  
								      CASE r.IsVendorPayment
									 	WHEN 1 THEN 'Vendor'
									 	ELSE 'Staff'
									 END as [Category],
								     COUNT(a.ApprovedBy) AS [TotalApproval],  
								     COUNT(a.ApprovedBy) AS [TotalApprovalApproved],  
								     0 AS [TotalApprovalRejected],
								     'IDR' AS [L_Currency]
								 FROM dbo.TrexGERHeader r
								 LEFT JOIN CostCenter cc ON cc.Code = r.DepartmentCode
								 LEFT JOIN MasterTable mt ON mt.ValueId = r.Status  
								 LEFT JOIN (
								     SELECT a.GERId, v.ApprovedAt, v.ApprovedBy
								     FROM TrexGERHeader a
								     CROSS APPLY (VALUES
								         (a.Approval1At, a.Approval1By),
								         (a.Approval2At, a.Approval2By),
								         (a.Approval3At, a.Approval3By),
								         (a.Approval4At, a.Approval4By),
								         (a.Approval5At, a.Approval5By),
								         (a.Approval6At, a.Approval6By),
								         (a.Approval7At, a.Approval7By),
								         (a.Approval8At, a.Approval8By)
								     ) AS v(ApprovedAt, ApprovedBy)
								     WHERE v.ApprovedBy IS NOT NULL
								 ) AS a ON r.GERId = a.GERId
								 WHERE mt.Category = 'ApprovalRequest.Status'
								 GROUP BY  
								     r.NoGER, r.CreatedBy, cc.Name, mt.ShortDescription, r.Status, r.TotalAmount,  r.IsVendorPayment,
								     r.[Description], r.CreatedAt, r.UpdatedAt, r.UpdatedBy, r.SendToFinanceAt, r.BeneficiaryVendorName
							
							UNION
								 -- TREX TER
								 SELECT   
								     r.NoTER AS [RequestNumber],
								     r.CreatedBy AS [RequestorName],  
								     '' [RequestorCostCenter],  
								     mt.ShortDescription AS [Status],  
								     r.Status AS [StatusId],  
								     '' AS [ReasonReject],  
								     '-' AS [VendorName],  
								     '-' AS [VendorId],
								     '' AS [GrandTotal],
								     '' [Description],  
								     '-' AS [GenerateId],  
								     '-' AS [InvoiceStatus],  
								     MAX(a.ApprovedAt) AS [TransactionDate],
								     r.CreatedAt AS [CreatedTime],  
								     r.UpdatedAt AS [LastUpdatedTime],  
								     r.UpdatedBy AS [LastUpdatedBy],  
								     'TREX-TER' AS [RequestType],  
								     'TER' AS [Type],  
								     'Staff' AS [Category],
								     COUNT(a.ApprovedBy) AS [TotalApproval],  
								     COUNT(a.ApprovedBy) AS [TotalApprovalApproved],  
								     0 AS [TotalApprovalRejected],
								     'IDR' AS [L_Currency]
								 FROM dbo.TrexTERHeader r
								 LEFT JOIN MasterTable mt ON mt.ValueId = r.Status  
								 LEFT JOIN (
								     SELECT a.TERId, v.ApprovedAt, v.ApprovedBy
								     FROM TrexTERHeader a
								     CROSS APPLY (VALUES
								         (a.Approval1At, a.Approval1By),
								         (a.Approval2At, a.Approval2By),
								         (a.Approval3At, a.Approval3By),
								         (a.Approval4At, a.Approval4By),
								         (a.Approval5At, a.Approval5By),
								         (a.Approval6At, a.Approval6By),
								         (a.Approval7At, a.Approval7By),
								         (a.Approval8At, a.Approval8By)
								     ) AS v(ApprovedAt, ApprovedBy)
								     WHERE v.ApprovedBy IS NOT NULL
								 ) AS a ON r.TERId = a.TERId
								 WHERE mt.Category = 'ApprovalRequest.Status'
								 GROUP BY  
								     r.NoTER, r.CreatedBy, mt.ShortDescription, r.Status,  
								     r.CreatedAt, r.UpdatedAt, r.UpdatedBy, r.SendToFinanceAt

							UNION
								--Travel Settlement
								  SELECT 
								  DISTINCT r.RequestNumber,
								  ISNULL(ar.CreatedBy, r.CreatedBy) AS RequestorName, 
								  cc.[Name] AS RequestorCostCenter, 
								  (SELECT TOP 1 ShortDescription FROM MasterTable WHERE Category = 'ApprovalRequest.Status' AND ValueId = ISNULL(ar.Status, r.Status)) AS [Status], 
								  ar.Status [StatusId], 
								  r.ReasonReject, 
								  '-' [VendorName], 
								  '-' [VendorId],
								  r.GrandTotal, 
								  ar2.RequestNo [Description], 
								  '-' [GenerateId], 
								  '-' [InvoiceStatus], 
								  (SELECT CONVERT(datetime, MAX(ApprovalDate), 103)  from [ApprovalRequestGroupMember] where ApprovalRequestId =  ar.id) as [TransactionDate], 
								  r.CreatedTime, 
								  r.LastUpdatedTime, 
								  r.LastUpdatedBy, 
								  --rd.InvoiceNo,
								  'Travel Settlement' as [RequestType], 
								  'TRSTL' as [Type], 
								  'Staff' [Category],
								  CONVERT(INT,ISNULL(ta.TotalApproval, 0)) [TotalApproval], 
								  CONVERT(INT,ISNULL(ta2.TotalApprovalApproved, 0)) [TotalApprovalApproved], 
								  CONVERT(INT,ISNULL(ta3.TotalApprovalRejected, 0)) [TotalApprovalRejected],
								  r.L_Currency
								FROM 
								  dbo.TravelRequestExpense r 
								  LEFT JOIN dbo.[ApprovalRequest] ar ON ar.Id = r.ApprovalRequestId 
								  LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id 
								  LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id 
								  JOIN TravelRequest tr on tr.Id = r.TravelRequestId
								  JOIN ApprovalRequest ar2 on ar2.Id = tr.ApprovalRequestId
								  JOIN dbo.[CostCenter] cc ON r.CostCenterId = cc.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApproval FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id GROUP BY ar.Id) ta ON ar.Id = ta.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalApproved FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE (argm.Status = 2 OR argm.Status = 7) GROUP BY ar.Id) ta2 ON ar.Id = ta2.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalRejected FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 3 GROUP BY ar.Id) ta3 ON ar.Id = ta3.Id 

							UNION
								--Invoice Travel
								SELECT 
								 DISTINCT r.RequestNumber,
								 ISNULL(ar.CreatedBy, r.CreatedBy) AS RequestorName, 
								 fu.CostCenterName AS RequestorCostCenter, 
								 (SELECT TOP 1 ShortDescription FROM MasterTable WHERE Category = 'ApprovalRequest.Status' AND ValueId = ISNULL(ar.Status, r.Status)) AS [Status], 
								 ar.Status [StatusId], 
								 r.ReasonReject, 
								 '-' [VendorName], 
								 '-' [VendorId],
								 r.Amount [GrandTotal], 
								 r.[Description], 
								 '-' [GenerateId], 
								 '-' [InvoiceStatus], 
								 (SELECT CONVERT(datetime, MAX(ApprovalDate), 103)  from [ApprovalRequestGroupMember] where ApprovalRequestId =  ar.id) as [TransactionDate], 
								 r.CreatedTime, 
								 r.LastUpdatedTime, 
								 r.LastUpdatedBy, 
								 --rd.InvoiceNo,
								 'Invoice Travel' as [RequestType], 
								 'INVTR' as [Type], 
								 'Staff' [Category],
								 CONVERT(INT,ISNULL(ta.TotalApproval, 0)) [TotalApproval], 
								 CONVERT(INT,ISNULL(ta2.TotalApprovalApproved, 0)) [TotalApprovalApproved], 
								 CONVERT(INT,ISNULL(ta3.TotalApprovalRejected, 0)) [TotalApprovalRejected],
								 (SELECT TOP 1 L_currency from InvoiceTravelDetail WHERE InvoiceTravelId =  r.Id) [L_Currency]
								FROM 
								 dbo.InvoiceTravel r 
								 LEFT JOIN dbo.[ApprovalRequest] ar ON ar.Id = r.ApprovalRequestId 
								 LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id 
								 LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id 
								 LEFT JOIN Flips.UserAccount fu on fu.Id = r.RequestorAccountId
								 LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApproval FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id GROUP BY ar.Id) ta ON ar.Id = ta.Id 
								 LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalApproved FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE (argm.Status = 2 OR argm.Status = 7) GROUP BY ar.Id) ta2 ON ar.Id = ta2.Id 
								 LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalRejected FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 3 GROUP BY ar.Id) ta3 ON ar.Id = ta3.Id 
							

							UNION
								 --GER
								 SELECT 
								  DISTINCT r.RequestNumber,
								  ISNULL(ar.CreatedBy, r.CreatedBy) AS RequestorName, 
								  fu.CostCenterName AS RequestorCostCenter, 
								  (SELECT TOP 1 ShortDescription FROM MasterTable WHERE Category = 'ApprovalRequest.Status' AND ValueId = ISNULL(ar.Status, r.Status)) AS [Status], 
								  ar.Status [StatusId], 
								  r.ReasonReject, 
								  '-' [VendorName], 
								  '-' [VendorId],
								  ISNULL((SELECT SUM(NettAmount) FROM GerDetail WHERE GerHeaderId =  r.Id ), 0) [GrandTotal],
								  r.[Description], 
								  '-' [GenerateId], 
								  '-' [InvoiceStatus], 
								  (SELECT CONVERT(datetime, MAX(ApprovalDate), 103)  from [ApprovalRequestGroupMember] where ApprovalRequestId =  ar.id) as [TransactionDate], 
								  r.CreatedTime, 
								  r.LastUpdatedTime, 
								  r.LastUpdatedBy, 
								  --rd.InvoiceNo,
								  'GER' as [RequestType], 
								  'GER' as [Type], 
								  'Staff' [Category],
								  CONVERT(INT,ISNULL(ta.TotalApproval, 0)) [TotalApproval], 
								  CONVERT(INT,ISNULL(ta2.TotalApprovalApproved, 0)) [TotalApprovalApproved], 
								  CONVERT(INT,ISNULL(ta3.TotalApprovalRejected, 0)) [TotalApprovalRejected],
								  (SELECT TOP 1 lcurrencycode from GerDetail WHERE GerHeaderId =  r.Id) [L_Currency]
								 FROM 
								  dbo.GerHeader r 
								  LEFT JOIN dbo.[ApprovalRequest] ar ON ar.RequestNo = r.RequestNumber
								  LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id 
								  LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id 
								  LEFT JOIN Flips.UserAccount fu on fu.Id = r.RequestorAccountId
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApproval FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id GROUP BY ar.Id) ta ON ar.Id = ta.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalApproved FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE (argm.Status = 2 OR argm.Status = 7) GROUP BY ar.Id) ta2 ON ar.Id = ta2.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalRejected FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 3 GROUP BY ar.Id) ta3 ON ar.Id = ta3.Id 


							UNION
								--Settlement
								  SELECT 
								  DISTINCT s.SettlementNumber AS RequestNumber, 
								  ISNULL(
								    s.CreatedBy, r.CreatedBy
								  ) AS RequestorName, 
								  cc.[Name] AS RequestorCostCenter, 
								  (
								    SELECT 
								      TOP 1 ShortDescription 
								    FROM 
								      MasterTable 
								    WHERE 
								      Category = 'ApprovalRequest.Status'
								      AND ValueId = ISNULL(s.Status, r.Status)
								  ) AS [Status], 
								  s.Status [StatusId], 
								  s.ReasonReject,  
								  v.Name as VendorName, 
								  v.Id as VendorId,
								  (
								    SELECT 
								      SUM(Amount) 
								    FROM 
								      SettlementDetail  
								    WHERE 
								      SettlementId = s.Id
								  ) as [GrandTotal], 
								  r.Description, 
								  '-' [GenerateId], 
								  '-' [InvoiceStatus], 
								  (SELECT CONVERT(datetime, MAX(ApprovalDate), 103)  from [ApprovalRequestGroupMember] where ApprovalRequestId =  ar.id) as [TransactionDate], 
								  s.CreatedTime, 
								  s.LastUpdatedTime, 
								  s.LastUpdatedBy, 
								  --rd.InvoiceNo,
								  'Settlement' as [RequestType],
								  'STL' as [Type],
								  scv.SubCategoryName [Category],
								  CONVERT(
								    INT, 
								    ISNULL(ta.TotalApproval, 0)
								  ) [TotalApproval], 
								  CONVERT(
								    INT, 
								    ISNULL(ta2.TotalApprovalApproved, 0)
								  ) [TotalApprovalApproved], 
								  CONVERT(
								    INT, 
								    ISNULL(ta3.TotalApprovalRejected, 0)
								  ) [TotalApprovalRejected],
								  '' LCurrency
								FROM 
								  dbo.[Reimbursement] r 
								  JOIN dbo.ReimbursementDetail rd on rd.ReimbursementId = r.Id 
								  JOIN Settlement s on s.ReimbursementId = r.Id
								  JOIN SettlementDetail sd on sd.SettlementId = s.Id
								  LEFT JOIN dbo.[ApprovalRequest] ar ON ar.RequestNo = s.SettlementNumber 
								  LEFT JOIN dbo.[ApprovalRequestGroup] arg ON arg.ApprovalRequestId = ar.Id 
								  LEFT JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id 
								  JOIN dbo.[CostCenter] cc ON r.CostCenterId = cc.Id 
								  JOIN dbo.[SubCategory] sc ON r.ReimbursementType_SubCategoryId = sc.Id 
								  JOIN dbo.[Vendor] v on v.Id = rd.VendorId
								  JOIN dbo.[SubCategory] scv ON scv.Id = v.SubCategoryId
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApproval FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id GROUP BY ar.Id) ta ON ar.Id = ta.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalApproved FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE (argm.Status = 2 OR argm.Status = 7) GROUP BY ar.Id) ta2 ON ar.Id = ta2.Id 
								  LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApprovalRejected FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id WHERE argm.Status = 3 GROUP BY ar.Id) ta3 ON ar.Id = ta3.Id 
								WHERE 
								  r.RequestNumber IS NOT NULL 
								  AND ar.Status IS NOT NULL
								  AND NOT EXISTS
									 ( SELECT *
									   FROM   ApprovalRequest ar2
									   WHERE  s.SettlementNumber = ar2.RequestNo AND ar.ApprovalFlowId <> ar2.ApprovalFlowId)

						    UNION 
								--Shopping Cart
								select distinct po.PONumber AS RequestNumber, 
								pr.RequestorUserName AS RequestorName, 
								cc.Name RequestorCostCenter,
								(SELECT TOP 1 case when ShortDescription = 'Process' then 'Approve' end FROM MasterTable WHERE Category = 'PurchaseOrder.Status' AND ValueId = po.Status) AS [Status],
								po.Status as [StatusId],
								ipo.ReasonReject [ReasonReject],
								vn.Name AS VendorName, 
								vn.Id AS VendorId,
								0 [GrandTotal],
								'-' [Description],
								ipo.Id GenerateId,
								ipo.Status InvoiceStatus,
								CONVERT(datetime, ipo.InvoiceDate, 103) AS TransactionDate, 
								ipo.InvoiceDate as CreatedTime, 
								ipo.LastUpdateTime [LastUpdatedTime], 
								ipo.LastUpdateBy [LastUpdatedBy],
								c.RequestType,
							    'SC' as [Type],
								sc.SubCategoryName [Category],
								CONVERT(INT, ISNULL((ta.TotalApproval + 1 + tadn.TotalApprovalDn + taipo.TotalApprovalIpo), 0)) [TotalApproval], 
								CONVERT(INT,ISNULL(ta2.TotalApprovalApproved + tapo2.TotalPoApproved + tadn2.TotalApprovalDn+ taipo2.TotalApprovalIpo,0)) [TotalApprovalApproved],
								CONVERT(INT,ISNULL(ta3.TotalApprovalRejected + tapo3.TotalPoRejected + tadn3.TotalRejectedDn + taipo3.TotalRejectedIpo,0)) [TotalApprovalRejected],
								ipo.LCurrency
							FROM InvoicePO ipo
								join PurchaseOrder po on ipo.PurchaeseOrderId = po.Id
								join PurchaseOrderDetail pod on po.Id = pod.PurchaseOrderId
								join PurchaseRequestItemDetail prd on pod.PurchaseRequestItemDetailId = prd.Id
								join PurchaseRequest pr on prd.PurchaseRequestId = pr.Id
								join Flips.UserAccount ua on pr.RequestorAccountId = ua.Id
								join Vendor vn on ipo.VendorId = vn.Id
								join SubCategory sc on sc.Id =  vn.SubCategoryId
								join CostCenter cc on ua.CostCenterId = cc.Id
								join PurchaseOrderTOP pot on po.Id = pot.PurchaseOrderId
								join DeliveryNotes dn on dn.PurchaseOrderId = po.Id
								JOIN (SELECT ar.Id,(CASE WHEN ar.RequestNo LIKE '%RI%' THEN 'Reimbursement' WHEN ar.RequestNo LIKE '%CA%' THEN 'Cash Advance' WHEN ar.RequestNo LIKE '%TR%' THEN 'Travel' WHEN ar.RequestNo LIKE '%SC%' THEN 'Shopping Cart' END) AS RequestType FROM ApprovalRequest ar where ar.RequestNo LIKE '%SC%' ) c ON prd.ApprovalRequestId = c.Id
								LEFT JOIN (SELECT ar.Id, COUNT(*) TotalApproval FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id GROUP BY ar.Id ) ta ON prd.ApprovalRequestId = ta.Id
								OUTER APPLY
								(select COUNT(*) TotalApprovalDn from PurchaseOrderTOP pot1 join DeliveryNotesPayment dnp on pot1.Id = dnp.PurchaseOrderTOPId join DeliveryNotesDetail dnd on dnp.Id = dnd.DeliveryNotesPaymentId where pot1.PurchaseOrderId = po.Id) tadn 
								OUTER APPLY
								(select COUNT(*) TotalApprovalIpo  from InvoicePO ipoA  where ipoA.PurchaeseOrderId = po.Id) taipo 
								LEFT JOIN
								(SELECT ar1.Id,COUNT(*) TotalApprovalApproved  FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar1 ON argm.ApprovalRequestId = ar1.Id where argm.Status = 2 GROUP BY ar1.Id) ta2 ON prd.ApprovalRequestId = ta2.Id
								OUTER APPLY
								(select COUNT(*) TotalPoApproved from PurchaseOrder po1  where po1.Status = 2 and po1.Id = po.Id ) tapo2
								OUTER APPLY
								(select COUNT(*) TotalApprovalDn from PurchaseOrderTOP pot1 join DeliveryNotesPayment dnp on pot1.Id = dnp.PurchaseOrderTOPId join DeliveryNotesDetail dnd on dnp.Id = dnd.DeliveryNotesPaymentId where pot1.PurchaseOrderId = po.Id and dnd.Status = 2) tadn2
								OUTER APPLY
								(select COUNT(*) TotalApprovalIpo from InvoicePO ipoA where ipoA.PurchaeseOrderId = po.Id and ipoA.Status = 2) taipo2
								LEFT JOIN
								(SELECT ar.Id, COUNT(*) TotalApprovalRejected FROM ApprovalRequestGroupMember argm JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id where argm.Status = 3 GROUP BY ar.Id) ta3 ON prd.ApprovalRequestId = ta3.Id 
								OUTER APPLY
								(select COUNT(*) TotalPoRejected from PurchaseOrder po1 where po1.Status = 3 and po1.Id = po.Id) tapo3
								OUTER APPLY
								(select COUNT(*) TotalRejectedDn from PurchaseOrderTOP pot1 join DeliveryNotesPayment dnp on pot1.Id = dnp.PurchaseOrderTOPId join DeliveryNotesDetail dnd on dnp.Id = dnd.DeliveryNotesPaymentId where pot1.PurchaseOrderId = po.Id and dnd.Status = 3) tadn3 
								OUTER APPLY
								(select COUNT(*) TotalRejectedIpo from InvoicePO ipoA where ipoA.PurchaeseOrderId = po.Id and ipoA.Status = 5) taipo3 
								where po.PONumber IS NOT NULL AND  ipo.Status not in (8, 7) and dn.Status not in (5) 
								 UNION
								SELECT DISTINCT
									PON.PONumber AS RequestNumber,
									PON.RequestorName AS RequestorName,
									COST.Name RequestorCostCenter,
									MTL.Name [Status],
									PON.Status as [StatusId],
									IPO.ReasonReject [ReasonReject],
									VND.Name AS VendorName,
									VND.Id AS VendorId,
									0 [GrandTotal],
									'-' [Description],
									IPO.Id GenerateId,
									IPO.Status InvoiceStatus,
									CONVERT(DATETIME, IPO.InvoiceDate, 103) AS TransactionDate,
									IPO.InvoiceDate as CreatedTime,
									IPO.LastUpdateTime [LastUpdatedTime],
									IPO.LastUpdateBy [LastUpdatedBy],
									APR.RequestType,
									'SC' as [Type],
									SCV.SubCategoryName [Category],
									CONVERT(INT, ISNULL((TA.TotalApproval + 1 + TADN.ApprovalDelivery + TAIPO.InvoicApproval), 0)) [TotalApproval],
									CONVERT(
											   INT,
											   ISNULL(
														 TA2.TotalApproval + TAPO2.TotalPoApprove + TADN2.ApprovalDelivery
														 + taipo2.TotalApprovalIpo,
														 0
													 )
										   ) [TotalApprovalApproved],
									CONVERT(
											   INT,
											   ISNULL(
														 TA3.TotalApprovalRejected + TAPO3.TotalPoReject + TADN3.RejectDelivery
														 + TAIPO3.RejectInvoice,
														 0
													 )
										   ) [TotalApprovalRejected],
									IPO.LCurrency
							FROM InvoicePO IPO
							JOIN PONonShopping PON ON IPO.PurchaeseOrderId = PON.Id
							JOIN DeliveryNotes DELN ON PON.Id = DELN.PurchaseOrderId
							JOIN Vendor VND ON IPO.VendorId = VND.Id
							JOIN Flips.UserAccount UAC ON PON.RequestorName =  UAC.Username
							JOIN CostCenter COST ON UAC.CostCenterId = COST.Id 
							JOIN MasterTable MTL ON PON.Status = MTL.ValueId
							JOIN PRFSummary PRFS ON PON.PRFSummaryId = PRFS.Id
							JOIN SubCategory SCV ON scv.Id = VND.SubCategoryId
							JOIN (
										SELECT AR.Id,
											   (CASE
													WHEN AR.RequestNo LIKE '%RI%' THEN
														'Reimbursement'
													WHEN AR.RequestNo LIKE '%CA%' THEN
														'Cash Advance'
													WHEN AR.RequestNo LIKE '%TR%' THEN
														'Travel'
													WHEN AR.RequestNo LIKE '%SC%' THEN
														'Shopping Cart'
													WHEN AR.RequestNo LIKE '%PROCSUM%' THEN
														'Non Shopping Cart'
												END
											   ) AS RequestType
										FROM ApprovalRequest AR
										where AR.RequestNo LIKE '%PROCSUM%'
									) APR
									ON PRFS.ApprovalRequestId = APR.Id
									LEFT JOIN
									(
										SELECT AR.Id,
											   COUNT(*) TotalApproval
										FROM ApprovalRequestGroupMember ARGM
											JOIN ApprovalRequest AR
												ON ARGM.ApprovalRequestId = AR.Id
										GROUP BY AR.Id
									) TA
									ON PRFS.ApprovalRequestId = TA.Id
									OUTER APPLY
									(
										SELECT COUNT(*) ApprovalDelivery
										FROM PONonShoppingTOP POT1
											JOIN DeliveryNotesPayment dnp
												ON POT1.Id = DNP.PurchaseOrderTOPId
											JOIN DeliveryNotesDetail DND
												ON DNP.Id = DND.DeliveryNotesPaymentId
										where POT1.PONonShoppingId = PON.Id
									) TADN
									OUTER APPLY
									(
										SELECT COUNT(*) InvoicApproval
										FROM InvoicePO INV
										WHERE  INV.PurchaeseOrderId = PON.Id
									) TAIPO 
									LEFT JOIN
									(
										SELECT AR1.Id,
											   COUNT(*) TotalApproval
										FROM ApprovalRequestGroupMember ARGM
											JOIN ApprovalRequest ar1
												ON ARGM.ApprovalRequestId = ar1.Id
										where ARGM.Status = 2 /*Approved*/
										GROUP BY AR1.Id
									) TA2
									ON PRFS.ApprovalRequestId = TA2.Id
									 OUTER APPLY
									(
										SELECT COUNT(*) TotalPoApprove
										FROM PONonShopping PON1
										WHERE PON1.Status = 2
											  and PON1.Id = PON.Id
									) TAPO2
									OUTER APPLY
									(
										SELECT COUNT(*) ApprovalDelivery
										FROM PONonShoppingTOP POT1
											JOIN DeliveryNotesPayment dnp
												ON POT1.Id = DNP.PurchaseOrderTOPId
											JOIN DeliveryNotesDetail DND
												ON DNP.Id = DND.DeliveryNotesPaymentId
										where POT1.PONonShoppingId = PON.Id
											  AND DND.Status = 2
									) TADN2
									OUTER APPLY
									(
										SELECT COUNT(*) TotalApprovalIpo
										FROM InvoicePO ipoA
										WHERE ipoA.PurchaeseOrderId = PON.Id
											  AND ipoA.Status = 2
									) TAIPO2
									LEFT JOIN
									(
										SELECT AR.Id,
											   COUNT(*) TotalApprovalRejected
										FROM ApprovalRequestGroupMember ARGM
											JOIN ApprovalRequest AR
												ON ARGM.ApprovalRequestId = ar.Id
										WHERE ARGM.Status = 3
										GROUP BY AR.Id
									) TA3
									ON PRFS.ApprovalRequestId = TA3.Id
									OUTER APPLY
									(
										SELECT COUNT(*) TotalPoReject
										FROM PONonShopping PON1
										WHERE PON1.Status = 3
											  and PON1.Id = PON.Id
									) TAPO3
									OUTER APPLY
									(
										SELECT COUNT(*) RejectDelivery
										FROM PONonShoppingTOP POT1
											JOIN DeliveryNotesPayment dnp
												ON POT1.Id = DNP.PurchaseOrderTOPId
											JOIN DeliveryNotesDetail DND
												ON DNP.Id = DND.DeliveryNotesPaymentId
										where POT1.PONonShoppingId = PON.Id
											  AND DND.Status = 3
									) TADN3
									OUTER APPLY
									(
										SELECT COUNT(*) RejectInvoice
										FROM InvoicePO IPOA
										WHERE IPOA.PurchaeseOrderId = PON.Id
											  AND ipoA.Status = 5
									) TAIPO3
							WHERE IPO.CategoryProcess_SubCategoryId = (SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01262') AND  IPO.Status not in (8, 7) and DELN.Status not in (5) AND MTL.Category = 'PurchaseOrder.Status'

								) AS UNIONQUERY
								
								--CONDITION UNION
								WHERE TransactionDate <> ''
							    AND {subQuery} ";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        private static string ApprovalListSubQuery(ParamGetRequestList param)
        {
            var subQuery = $@"(RequestNumber LIKE @RequestNumber) 
								  AND VendorId = @VendorId
								  AND Category = @Category
								  AND StatusId in (2, 7) 
								  AND LastUpdatedBy = @MakerFinance
								  AND CONVERT(datetime, TransactionDate, 103) BETWEEN CONVERT(varchar(50), @RequestDateFrom, 103) AND CONVERT(varchar(50), @RequestDateTo, 103) ";
            if (String.IsNullOrEmpty(param.RequestNumber))
            {
                subQuery = subQuery.Replace("(RequestNumber LIKE @RequestNumber) ", " 1=1 ");
            }
            if (param.Status == "8")
            {
                subQuery = subQuery.Replace("StatusId in (2, 7)", " StatusId in (8) ");
            }
            if (String.IsNullOrEmpty(param.VendorId))
            {
                subQuery = subQuery.Replace("VendorId = @VendorId", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.Category))
            {
                subQuery = subQuery.Replace("Category = @Category", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.RequestDateFrom))
            {
                subQuery = subQuery.Replace("CONVERT(datetime, TransactionDate, 103) BETWEEN CONVERT(varchar(50), @RequestDateFrom, 103) AND CONVERT(varchar(50), @RequestDateTo, 103)", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.MakerFinance))
            {
                subQuery = subQuery.Replace("LastUpdatedBy = @MakerFinance", " 1 = 1");
            }
            if (!String.IsNullOrEmpty(param.Search?.Value))
            {
                subQuery = String.Concat(subQuery, " AND (RequestNumber LIKE @Search OR RequestorName LIKE @Search OR VendorName LIKE @Search) ");
            }
            return subQuery;
        }
        #endregion

        #region Fin Maker Submission
        public static string SubmitFinMakerRI()
        {
            string query = $@"UPDATE   Reimbursement
                              SET      [Status] = @Status,
                                       [LastUpdatedBy]   = @LastUpdatedBy,
                                       [LastUpdatedTime] = GETDATE()
                              WHERE    [RequestNumber]   = @RequestNumber";
            return query;
        }
        public static string SubmitFinMakerTRSTL()
        {
            string query = $@"UPDATE   TravelRequestExpense
                              SET      [Status] = @Status,
                                       [LastUpdatedBy]   = @LastUpdatedBy,
                                       [LastUpdatedTime] = GETDATE()
                              WHERE    [RequestNumber]   = @RequestNumber";
            return query;
        }
        public static string SubmitFinMakerINVTR()
        {
            string query = $@"UPDATE   InvoiceTravel
                              SET      [Status] = @Status,
                                       [LastUpdatedBy]   = @LastUpdatedBy,
                                       [LastUpdatedTime] = GETDATE()
                              WHERE    [RequestNumber]   = @RequestNumber";
            return query;
        }
        public static string SubmitFinMakerGER()
        {
            string query = $@"UPDATE   GerHeader
                              SET      [Status] = @Status,
                                       [LastUpdatedBy]   = @LastUpdatedBy,
                                       [LastUpdatedTime] = GETDATE()
                              WHERE    [RequestNumber]   = @RequestNumber";
            return query;
        }
        public static string SubmitFinMakerTrexAPR()
        {
            string query = $@"UPDATE   TrexApr
                              SET      [Status] = @Status,
                                       [UpdatedBy]       = @LastUpdatedBy,
                                       [UpdatedAt]       = GETDATE()
                              WHERE    [NoAPR]           = @RequestNumber";
            return query;
        }
        public static string SubmitFinMakerTrexEER()
        {
            string query = $@"UPDATE   TrexEerHeader
                              SET      [Status] = @Status,
                                       [UpdatedBy]       = @LastUpdatedBy,
                                       [UpdatedAt]       = GETDATE()
                              WHERE    [NoEER]           = @RequestNumber";
            return query;
        }
        public static string SubmitFinMakerTrexGER()
        {
            string query = $@"UPDATE   TrexGerHeader
                              SET      [Status] = @Status,
                                       [UpdatedBy]       = @LastUpdatedBy,
                                       [UpdatedAt]       = GETDATE()
                              WHERE    [NoGER]           = @RequestNumber";
            return query;
        }
        public static string SubmitFinMakerTrexTER()
        {
            string query = $@"UPDATE   TrexTerHeader
                              SET      [Status] = @Status,
                                       [UpdatedBy]       = @LastUpdatedBy,
                                       [UpdatedAt]       = GETDATE()
                              WHERE    [NoTER]           = @RequestNumber";
            return query;
        }
        public static string CreateVoucherRI()
        {
            string query = $@"DECLARE @NewVoucherIncrement varchar(max) 
                                         IF (YEAR(GETDATE()) > (SELECT TOP 1 YEAR(CreatedTime) FROM VoucherHeader order by Id desc)) --Reset to 1 after New Year
                                        	SET @NewVoucherIncrement = '1'
                                         ELSE
                                        	SET @NewVoucherIncrement = @LastVoucherNumber + 1
                                         DECLARE @DateFormat varchar (100)         = (SELECT format(cast(GETDATE() as date),'yyyyMMdd'))
                                         DECLARE @NewVoucher varchar (100)         = @NewVoucherIncrement + ' ' + UPPER(@Category) + ' ' + @DateFormat 
                                         DECLARE @Email      varchar (100)         = (SELECT top 1 rd.VendorEmail FROM Reimbursement r JOIN ReimbursementDetail rd on r.Id = rd.ReimbursementId WHERE r.RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                         
                                         DECLARE @Currency varchar(5)
                                         IF      (@Category = 'Reimbursement')
                                                  SET @Currency = (SELECT TOP 1 rd.L_Currency FROM Reimbursement r join ReimbursementDetail rd on r.Id = rd.ReimbursementId WHERE r.RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                         ELSE IF (@Category = 'Cash Advance')
                                                  SET @Currency = (SELECT TOP 1 rd.L_Currency FROM Reimbursement r join ReimbursementDetail rd on r.Id = rd.ReimbursementId WHERE r.RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                         ELSE IF (@Category = 'Cash Advance Travel')
                                                  SET @Currency = (SELECT TOP 1 rd.L_Currency FROM Reimbursement r join ReimbursementDetail rd on r.Id = rd.ReimbursementId WHERE r.RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                        
                                         --Insert Header
                                         INSERT INTO VoucherHeader ( VoucherNumber
                                         						   ,TransactionDate
                                         						   ,L_Currency
                                         						   ,Category
                                         						   ,IsEmail
                                         						   ,Email
                                         						   ,BankTransferCode
                                         						   ,BankTransferName
                                         						   ,CheckerMCM
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )
                                         OUTPUT INSERTED.*
                                         VALUES                    ( @NewVoucher
                                         						   ,GETDATE()
                                         						   ,@Currency
                                         						   ,@Category
                                         						   ,@IsEmail
                                         						   ,@Email
                                         						   ,@BankTransferCode
                                         						   ,@BankTransferName
                                         						   ,@CheckerMCM
                                         						   ,0
                                         						   ,GETDATE()
                                         						   ,@CreatedBy )
                                         
                                         --Insert Detail
                                         DECLARE @VoucherID int = SCOPE_IDENTITY()
                                         
                                         INSERT INTO VoucherDetail ( VoucherId
                                         						   ,VoucherRefId
                                         						   ,[Description]
                                         						   ,RateAmmount
                                         						   ,TotalBaseAmmount
                                         						   ,TotalOriginalAmmount
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )
                                         SELECT @VoucherID,
                                         	   LTRIM(RTRIM(i.value)),
                                         	   (SELECT [Description] FROM Reimbursement where RequestNumber = LTRIM(RTRIM(i.value))),
                                         	   (SELECT top 1 RateAmount FROM Reimbursement r 
                                         	    join ReimbursementDetail rd on r.Id = rd.ReimbursementId 
                                         	    where r.RequestNumber = LTRIM(RTRIM(i.value))) as [RateAmmount],
                                         	   (SELECT SUM(rdc.Amount) RateAmount FROM Reimbursement r 
                                         	    join ReimbursementDetail rd on r.Id = rd.ReimbursementId 
                                         	    join ReimbursementDetailCostCenter rdc on rd.Id = rdc.ReimbursementDetailId
                                         	    where r.RequestNumber = LTRIM(RTRIM(i.value))) as [TotalBaseAmmount],
                                         	   (SELECT SUM(rdc.BasicAmount) RateAmount FROM Reimbursement r 
                                         	    join ReimbursementDetail rd on r.Id = rd.ReimbursementId 
                                         	    join ReimbursementDetailCostCenter rdc on rd.Id = rdc.ReimbursementDetailId
                                         	    where r.RequestNumber = LTRIM(RTRIM(i.value))) as [TotalOriginalAmmount],
                                         	   0,
                                         	   GETDATE(),
                                         	   @CreatedBy
                                         FROM STRING_SPLIT(@RequestNumbers, ',') AS i; 

                                         --Update approval request 
                                         UPDATE ApprovalRequest
                                         SET [Status] = 9
                                            ,LastUpdatedBy = @CreatedBy
                                            ,LastUpdatedTime = GETDATE()
                                         WHERE RequestNo in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                         
                                         --Update table transaction
                                         UPDATE Reimbursement
                                         SET [Status] = 9
                                         	,LastUpdatedBy = @CreatedBy
                                         	,LastUpdatedTime = GETDATE()
                                           WHERE RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                        ";
            return query;
        }
        public static string CreateVoucherTRSTL()
        {
            string query = $@"DECLARE @NewVoucherIncrement varchar(max) 
                                         IF (YEAR(GETDATE()) > (SELECT TOP 1 YEAR(CreatedTime) FROM VoucherHeader order by Id desc)) --Reset to 1 after New Year
                                        	SET @NewVoucherIncrement = '1'
                                         ELSE
                                        	SET @NewVoucherIncrement = @LastVoucherNumber + 1
                                         DECLARE @DateFormat varchar (100)         = (SELECT format(cast(GETDATE() as date),'yyyyMMdd'))
                                         DECLARE @NewVoucher varchar (100)         = @NewVoucherIncrement + ' ' + UPPER(@Category) + ' ' + @DateFormat 
                                         DECLARE @Email      varchar (100)         = (SELECT top 1 tr.CreatorEmail FROM TravelRequest tr JOIN TravelRequestExpense tre on tr.Id = tre.TravelRequestId WHERE tre.RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                         DECLARE @Currency   varchar(5)            =  'IDR'  --Travel Settlement Always IDR

                                         --Insert Header
                                         INSERT INTO VoucherHeader ( VoucherNumber
                                         						   ,TransactionDate
                                         						   ,L_Currency
                                         						   ,Category
                                         						   ,IsEmail
                                         						   ,Email
                                         						   ,BankTransferCode
                                         						   ,BankTransferName
                                         						   ,CheckerMCM
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )
                                         OUTPUT INSERTED.*
                                         VALUES                    ( @NewVoucher
                                         						   ,GETDATE()
                                         						   ,@Currency
                                         						   ,@Category
                                         						   ,@IsEmail
                                         						   ,@Email
                                         						   ,@BankTransferCode
                                         						   ,@BankTransferName
                                         						   ,@CheckerMCM
                                         						   ,0
                                         						   ,GETDATE()
                                         						   ,@CreatedBy )
                                         
                                         --Insert Detail
                                         DECLARE @VoucherID int = SCOPE_IDENTITY()
                                         
                                         INSERT INTO VoucherDetail ( VoucherId
                                         						   ,VoucherRefId
                                         						   ,[Description]
                                         						   ,RateAmmount
                                         						   ,TotalBaseAmmount
                                         						   ,TotalOriginalAmmount
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )
                                         SELECT @VoucherID,
                                         	   LTRIM(RTRIM(i.value)),
                                         	   (SELECT tr.PurposeNotes FROM TravelRequestExpense r
                                                JOIN travelrequest tr on tr.Id = r.TravelRequestId
                                         	    where r.RequestNumber = LTRIM(RTRIM(i.value))) as [Description],
                                         	   1 as [RateAmmount],
                                         	   (SELECT GrandTotal FROM TravelRequestExpense r
                                         	    where r.RequestNumber = LTRIM(RTRIM(i.value))) as [TotalBaseAmmount],
                                         	   (SELECT GrandTotal FROM TravelRequestExpense r
                                         	    where r.RequestNumber = LTRIM(RTRIM(i.value))) as [TotalOriginalAmmount],
                                         	   0,
                                         	   GETDATE(),
                                         	   @CreatedBy
                                         FROM STRING_SPLIT(@RequestNumbers, ',') AS i; 

                                         --Update approval request 
                                         UPDATE ApprovalRequest
                                         SET [Status] = 9
                                            ,LastUpdatedBy = @CreatedBy
                                            ,LastUpdatedTime = GETDATE()
                                         WHERE RequestNo in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                         
                                         --Update table transaction
                                         UPDATE TravelRequestExpense
                                         SET [Status] = 9
                                         	,LastUpdatedBy = @CreatedBy
                                         	,LastUpdatedTime = GETDATE()
                                         WHERE RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                     ";
            return query;
        }
        public static string CreateVoucherINVTR()
        {
            string query = $@"DECLARE @NewVoucherIncrement varchar(max) 
                                         IF (YEAR(GETDATE()) > (SELECT TOP 1 YEAR(CreatedTime) FROM VoucherHeader order by Id desc)) --Reset to 1 after New Year
                                        	SET @NewVoucherIncrement = '1'
                                         ELSE
                                        	SET @NewVoucherIncrement = @LastVoucherNumber + 1
                                         DECLARE @DateFormat varchar (100)         = (SELECT format(cast(GETDATE() as date),'yyyyMMdd'))
                                         DECLARE @NewVoucher varchar (100)         = @NewVoucherIncrement + ' ' + UPPER(@Category) + ' ' + @DateFormat 
                                         DECLARE @Email      varchar (100)         = (SELECT top 1 tr.CreatorEmail FROM TravelRequest tr JOIN InvoiceTravelDetail invd on tr.Id = invd.TravelRequestId JOIN InvoiceTravel inv on inv.Id =  invd.InvoiceTravelId  WHERE inv.RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                         DECLARE @Currency   varchar(5)            = (SELECT top 1 invd.L_Currency FROM InvoiceTravel inv JOIN InvoiceTravelDetail invd on inv.Id = invd.InvoiceTravelId WHERE inv.RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM  string_split(@RequestNumbers,',')) )

                                         --Insert Header
                                         INSERT INTO VoucherHeader ( VoucherNumber
                                         						   ,TransactionDate
                                         						   ,L_Currency
                                         						   ,Category
                                         						   ,IsEmail
                                         						   ,Email
                                         						   ,BankTransferCode
                                         						   ,BankTransferName
                                         						   ,CheckerMCM
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )
                                         OUTPUT INSERTED.*
                                         VALUES                    ( @NewVoucher
                                         						   ,GETDATE()
                                         						   ,@Currency
                                         						   ,@Category
                                         						   ,@IsEmail
                                         						   ,@Email
                                         						   ,@BankTransferCode
                                         						   ,@BankTransferName
                                         						   ,@CheckerMCM
                                         						   ,0
                                         						   ,GETDATE()
                                         						   ,@CreatedBy )
                                         
                                         --Insert Detail
                                         DECLARE @VoucherID int = SCOPE_IDENTITY()
                                         
                                         INSERT INTO VoucherDetail ( VoucherId
                                         						   ,VoucherRefId
                                         						   ,[Description]
                                         						   ,RateAmmount
                                         						   ,TotalBaseAmmount
                                         						   ,TotalOriginalAmmount
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )
                                         SELECT @VoucherID,
                                         	   LTRIM(RTRIM(i.value)),
                                         	   (SELECT Description FROM InvoiceTravel r
                                         	    where r.RequestNumber = LTRIM(RTRIM(i.value))) as [Description],
                                         	   (SELECT TOP 1 RateAmount FROM InvoiceTravel r
                                                JOIN InvoiceTravelDetail invd on invd.InvoiceTravelId = r.Id
                                         	    where r.RequestNumber = LTRIM(RTRIM(i.value))) [RateAmmount],
                                         	   (SELECT Amount FROM InvoiceTravel r
                                         	    where r.RequestNumber = LTRIM(RTRIM(i.value))) as [TotalBaseAmmount],
                                         	   (SELECT Amount FROM InvoiceTravel r
                                         	    where r.RequestNumber = LTRIM(RTRIM(i.value))) as [TotalOriginalAmmount],
                                         	   0,
                                         	   GETDATE(),
                                         	   @CreatedBy
                                         FROM STRING_SPLIT(@RequestNumbers, ',') AS i; 

                                         --Update approval request 
                                         UPDATE ApprovalRequest
                                         SET [Status] = 9
                                            ,LastUpdatedBy = @CreatedBy
                                            ,LastUpdatedTime = GETDATE()
                                         WHERE RequestNo in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                         
                                         --Update table transaction
                                         UPDATE InvoiceTravel
                                         SET [Status] = 9
                                         	,LastUpdatedBy = @CreatedBy
                                         	,LastUpdatedTime = GETDATE()
                                         WHERE RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                     ";
            return query;
        }
        public static string CreateVoucherGER()
        {
            string query = $@"DECLARE @NewVoucherIncrement varchar(max) 
                                         IF (YEAR(GETDATE()) > (SELECT TOP 1 YEAR(CreatedTime) FROM VoucherHeader order by Id desc)) --Reset to 1 after New Year
                                        	SET @NewVoucherIncrement = '1'
                                         ELSE
                                        	SET @NewVoucherIncrement = @LastVoucherNumber + 1
                                         DECLARE @PaymentType varchar (100)         = (SELECT top 1 s.SubCategoryName from GerHeader g JOIN SubCategory s on g.PaymentType_SubCategoryId = s.Id WHERE g.RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                         DECLARE @DateFormat  varchar (100)         = (SELECT format(cast(GETDATE() as date),'yyyyMMdd'))
                                         DECLARE @NewVoucher  varchar (100)         = @NewVoucherIncrement + ' ' + UPPER(@Category) + ' ' + UPPER(@PaymentType) + ' ' + @DateFormat 
                                         DECLARE @Email       varchar (100)         = (SELECT top 1 fu.email from GerHeader g join flips.useraccount fu on g.RequestorUsername = fu.username WHERE g.RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                         DECLARE @Currency    varchar (5)           = (SELECT top 1 gd.LCurrencyCode FROM GerHeader g JOIN GerDetail gd on g.Id = gd.GerHeaderId WHERE g.RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM  string_split(@RequestNumbers,',')) )

                                         --Insert Header
                                         INSERT INTO VoucherHeader ( VoucherNumber
                                         						   ,TransactionDate
                                         						   ,L_Currency
                                         						   ,Category
                                         						   ,IsEmail
                                         						   ,Email
                                         						   ,BankTransferCode
                                         						   ,BankTransferName
                                         						   ,CheckerMCM
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )
                                         OUTPUT INSERTED.*
                                         VALUES                    ( @NewVoucher
                                         						   ,GETDATE()
                                         						   ,@Currency
                                         						   ,@Category
                                         						   ,@IsEmail
                                         						   ,@Email
                                         						   ,@BankTransferCode
                                         						   ,@BankTransferName
                                         						   ,@CheckerMCM
                                         						   ,0
                                         						   ,GETDATE()
                                         						   ,@CreatedBy )
                                         
                                         --Insert Detail
                                         DECLARE @VoucherID int = SCOPE_IDENTITY()
                                         
                                         INSERT INTO VoucherDetail ( VoucherId
                                         						   ,VoucherRefId
                                         						   ,[Description]
                                         						   ,RateAmmount
                                         						   ,TotalBaseAmmount
                                         						   ,TotalOriginalAmmount
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )

                                          SELECT @VoucherId
                                                ,CONCAT(gh.RequestNumber , ' - ', gd.Id)
                                                ,gh.Description
                                                ,'1'
                                                ,gd.NettAmount
                                                ,gd.NettAmount
                                                ,'0'
                                                ,GETDATE()
                                                ,@CreatedBy
                                          FROM GerDetail gd
                                          JOIN GerHeader gh on gd.GerHeaderId = gh.Id
                                          WHERE gh.RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))

                                         --Update approval request 
                                         UPDATE ApprovalRequest
                                         SET [Status] = 9
                                            ,LastUpdatedBy = @CreatedBy
                                            ,LastUpdatedTime = GETDATE()
                                         WHERE RequestNo in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                         
                                         --Update table transaction
                                         UPDATE GerHeader
                                         SET [Status] = 9
                                         	,LastUpdatedBy = @CreatedBy
                                         	,LastUpdatedTime = GETDATE()
                                         WHERE RequestNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                     ";
            return query;
        }
        public static string CreateVoucherTrexAPR()
        {
            string query = $@"DECLARE @NewVoucherIncrement varchar(max) 
                                         IF (YEAR(GETDATE()) > (SELECT TOP 1 YEAR(CreatedTime) FROM VoucherHeader order by Id desc)) --Reset to 1 after New Year
                                        	SET @NewVoucherIncrement = '1'
                                         ELSE
                                        	SET @NewVoucherIncrement = @LastVoucherNumber + 1
                                         DECLARE @DateFormat  varchar (100)         = (SELECT format(cast(GETDATE() as date),'yyyyMMdd'))
                                         DECLARE @NewVoucher  varchar (100)         = @NewVoucherIncrement + ' ' + UPPER(@Category) + ' ' + @DateFormat 
                                         DECLARE @Email       varchar (100)         = (SELECT top 1 r.RequesterEmail FROM TrexAPR r WHERE r.NoAPR in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                         DECLARE @Currency    varchar (5)           = 'IDR' -- Default IDR

                                         --Insert Header
                                         INSERT INTO VoucherHeader ( VoucherNumber
                                         						   ,TransactionDate
                                         						   ,L_Currency
                                         						   ,Category
                                         						   ,IsEmail
                                         						   ,Email
                                         						   ,BankTransferCode
                                         						   ,BankTransferName
                                         						   ,CheckerMCM
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )
                                         OUTPUT INSERTED.*
                                         VALUES                    ( @NewVoucher
                                         						   ,GETDATE()
                                         						   ,@Currency
                                         						   ,@Category
                                         						   ,@IsEmail
                                         						   ,@Email
                                         						   ,@BankTransferCode
                                         						   ,@BankTransferName
                                         						   ,@CheckerMCM
                                         						   ,0
                                         						   ,GETDATE()
                                         						   ,@CreatedBy )
                                         
                                         --Insert Detail
                                         DECLARE @VoucherID int = SCOPE_IDENTITY()
                                         
                                         INSERT INTO VoucherDetail ( VoucherId
                                         						   ,VoucherRefId
                                         						   ,[Description]
                                         						   ,RateAmmount
                                         						   ,TotalBaseAmmount
                                         						   ,TotalOriginalAmmount
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )

                                          SELECT @VoucherId
                                                ,trex.NoAPR
                                                ,trex.Description
                                                ,'1'
                                                ,trex.TotalAmount
                                                ,trex.TotalAmount
                                                ,'0'
                                                ,GETDATE()
                                                ,@CreatedBy
                                          FROM TrexApr trex
                                          WHERE trex.NoAPR in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                         
                                         --Update table transaction
                                         UPDATE TrexApr
                                         SET [Status] = 9
                                         	,UpdatedBy = @CreatedBy
                                         	,UpdatedAt = GETDATE()
                                         WHERE NoAPR in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                     ";
            return query;
        }
        public static string CreateVoucherTrexEER()
        {
            string query = $@"DECLARE @NewVoucherIncrement varchar(max) 
                                         IF (YEAR(GETDATE()) > (SELECT TOP 1 YEAR(CreatedTime) FROM VoucherHeader order by Id desc)) --Reset to 1 after New Year
                                        	SET @NewVoucherIncrement = '1'
                                         ELSE
                                        	SET @NewVoucherIncrement = @LastVoucherNumber + 1
                                         DECLARE @DateFormat  varchar (100)         = (SELECT format(cast(GETDATE() as date),'yyyyMMdd'))
                                         DECLARE @NewVoucher  varchar (100)         = @NewVoucherIncrement + ' ' + UPPER(@Category) + ' ' + @DateFormat 
                                         DECLARE @Email       varchar (100)         = (SELECT top 1 r.RequesterEmail FROM TrexEERHeader r WHERE r.NoEER in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                         DECLARE @Currency    varchar (5)           = 'IDR' -- Default IDR

                                         --Insert Header
                                         INSERT INTO VoucherHeader ( VoucherNumber
                                         						   ,TransactionDate
                                         						   ,L_Currency
                                         						   ,Category
                                         						   ,IsEmail
                                         						   ,Email
                                         						   ,BankTransferCode
                                         						   ,BankTransferName
                                         						   ,CheckerMCM
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )
                                         OUTPUT INSERTED.*
                                         VALUES                    ( @NewVoucher
                                         						   ,GETDATE()
                                         						   ,@Currency
                                         						   ,@Category
                                         						   ,@IsEmail
                                         						   ,@Email
                                         						   ,@BankTransferCode
                                         						   ,@BankTransferName
                                         						   ,@CheckerMCM
                                         						   ,0
                                         						   ,GETDATE()
                                         						   ,@CreatedBy )
                                         
                                         --Insert Detail
                                         DECLARE @VoucherID int = SCOPE_IDENTITY()
                                         
                                         INSERT INTO VoucherDetail ( VoucherId
                                         						   ,VoucherRefId
                                         						   ,[Description]
                                         						   ,RateAmmount
                                         						   ,TotalBaseAmmount
                                         						   ,TotalOriginalAmmount
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )

                                          SELECT @VoucherId
                                                ,trex.NoEER
                                                ,trex.Description
                                                ,'1'
                                                ,trex.PaidAmount
                                                ,trex.PaidAmount
                                                ,'0'
                                                ,GETDATE()
                                                ,@CreatedBy
                                          FROM TrexEerHeader trex
                                          WHERE trex.NoEER in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                         
                                         --Update table transaction
                                         UPDATE TrexEerHeader
                                         SET [Status] = 9
                                         	,UpdatedBy = @CreatedBy
                                         	,UpdatedAt = GETDATE()
                                         WHERE NoEER in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                     ";
            return query;
        }
        public static string CreateVoucherTrexGER()
        {
            string query = $@"DECLARE @NewVoucherIncrement varchar(max) 
                                         IF (YEAR(GETDATE()) > (SELECT TOP 1 YEAR(CreatedTime) FROM VoucherHeader order by Id desc)) --Reset to 1 after New Year
                                        	SET @NewVoucherIncrement = '1'
                                         ELSE
                                        	SET @NewVoucherIncrement = @LastVoucherNumber + 1
                                         DECLARE @DateFormat  varchar (100)         = (SELECT format(cast(GETDATE() as date),'yyyyMMdd'))
                                         DECLARE @NewVoucher  varchar (100)         = @NewVoucherIncrement + ' ' + UPPER(@Category) + ' ' + @DateFormat 
                                         DECLARE @Email       varchar (100)         = (SELECT top 1 r.RequesterEmail FROM TrexGERHeader r WHERE r.NoGER in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                         DECLARE @Currency    varchar (5)           = 'IDR' -- Default IDR

                                         --Insert Header
                                         INSERT INTO VoucherHeader ( VoucherNumber
                                         						   ,TransactionDate
                                         						   ,L_Currency
                                         						   ,Category
                                         						   ,IsEmail
                                         						   ,Email
                                         						   ,BankTransferCode
                                         						   ,BankTransferName
                                         						   ,CheckerMCM
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )
                                         OUTPUT INSERTED.*
                                         VALUES                    ( @NewVoucher
                                         						   ,GETDATE()
                                         						   ,@Currency
                                         						   ,@Category
                                         						   ,@IsEmail
                                         						   ,@Email
                                         						   ,@BankTransferCode
                                         						   ,@BankTransferName
                                         						   ,@CheckerMCM
                                         						   ,0
                                         						   ,GETDATE()
                                         						   ,@CreatedBy )
                                         
                                         --Insert Detail
                                         DECLARE @VoucherID int = SCOPE_IDENTITY()
                                         
                                         INSERT INTO VoucherDetail ( VoucherId
                                         						   ,VoucherRefId
                                         						   ,[Description]
                                         						   ,RateAmmount
                                         						   ,TotalBaseAmmount
                                         						   ,TotalOriginalAmmount
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )

                                          SELECT @VoucherId
                                                ,trex.NoGER
                                                ,trex.Description
                                                ,'1'
                                                ,COALESCE(trex.PaidAmount, 0)
                                                ,COALESCE(trex.PaidAmount, 0)
                                                ,'0'
                                                ,GETDATE()
                                                ,@CreatedBy
                                          FROM TrexGerHeader trex
                                          WHERE trex.NoGER in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                         
                                         --Update table transaction
                                         UPDATE TrexGerHeader
                                         SET [Status] = 9
                                         	,UpdatedBy = @CreatedBy
                                         	,UpdatedAt = GETDATE()
                                         WHERE NoGER in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                     ";
            return query;
        }
        public static string CreateVoucherTrexTER()
        {
            string query = $@"DECLARE @NewVoucherIncrement varchar(max) 
                                         IF (YEAR(GETDATE()) > (SELECT TOP 1 YEAR(CreatedTime) FROM VoucherHeader order by Id desc)) --Reset to 1 after New Year
                                        	SET @NewVoucherIncrement = '1'
                                         ELSE
                                        	SET @NewVoucherIncrement = @LastVoucherNumber + 1
                                         DECLARE @DateFormat  varchar (100)         = (SELECT format(cast(GETDATE() as date),'yyyyMMdd'))
                                         DECLARE @NewVoucher  varchar (100)         = @NewVoucherIncrement + ' ' + UPPER(@Category) + ' ' + @DateFormat 
                                         DECLARE @Email       varchar (100)         = (SELECT top 1 r.RequesterEmail FROM TrexTERHeader r WHERE r.NoTER in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) )
                                         DECLARE @Currency    varchar (5)           = 'IDR' -- Default IDR

                                         --Insert Header
                                         INSERT INTO VoucherHeader ( VoucherNumber
                                         						   ,TransactionDate
                                         						   ,L_Currency
                                         						   ,Category
                                         						   ,IsEmail
                                         						   ,Email
                                         						   ,BankTransferCode
                                         						   ,BankTransferName
                                         						   ,CheckerMCM
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )
                                         OUTPUT INSERTED.*
                                         VALUES                    ( @NewVoucher
                                         						   ,GETDATE()
                                         						   ,@Currency
                                         						   ,@Category
                                         						   ,@IsEmail
                                         						   ,@Email
                                         						   ,@BankTransferCode
                                         						   ,@BankTransferName
                                         						   ,@CheckerMCM
                                         						   ,0
                                         						   ,GETDATE()
                                         						   ,@CreatedBy )
                                         
                                         --Insert Detail
                                         DECLARE @VoucherID int = SCOPE_IDENTITY()
                                         
                                         INSERT INTO VoucherDetail ( VoucherId
                                         						   ,VoucherRefId
                                         						   ,[Description]
                                         						   ,RateAmmount
                                         						   ,TotalBaseAmmount
                                         						   ,TotalOriginalAmmount
                                         						   ,[Status]
                                         						   ,CreatedTime
                                         						   ,CreatedBy )

                                         SELECT 
										     @VoucherId,
										     trex.NoTER,
										     trex.Remarks,
										     '1',
										     COALESCE(trdt.TotalAmount, 0) +
										     COALESCE(trda.TotalAmount, 0) +
										     COALESCE(trdo.TotalAmount, 0) +
										     COALESCE(trdd.TotalAmount, 0) AS GrandTotal,
										     COALESCE(trdt.TotalAmount, 0) +
										     COALESCE(trda.TotalAmount, 0) +
										     COALESCE(trdo.TotalAmount, 0) +
										     COALESCE(trdd.TotalAmount, 0) AS NetAmount,
										     '0',
										     GETDATE(),
										     @CreatedBy
										 FROM TrexTERHeader trex
										 LEFT JOIN (
										     SELECT TERId, SUM(Amount) AS TotalAmount
										     FROM TrexTERDetailTransportation
										     GROUP BY TERId
										 ) trdt ON trex.TERId = trdt.TERId
										 LEFT JOIN (
										     SELECT TERId, SUM(Amount) AS TotalAmount
										     FROM TrexTERDetailAkomodasi
										     GROUP BY TERId
										 ) trda ON trex.TERId = trda.TERId
										 LEFT JOIN (
										     SELECT TERId, SUM(Amount) AS TotalAmount
										     FROM TrexTERDetailOther
										     GROUP BY TERId
										 ) trdo ON trex.TERId = trdo.TERId
										 LEFT JOIN (
										     SELECT TERId, SUM(Amount) AS TotalAmount
										     FROM TrexTERDetailDurasi
										     GROUP BY TERId
										 ) trdd ON trex.TERId = trdd.TERId
										 WHERE trex.NoTER IN (
										     SELECT LTRIM(RTRIM(value)) FROM string_split(@RequestNumbers, ',')
										 );
                                       
                                         --Update table transaction
                                         UPDATE TrexTerHeader
                                         SET [Status] = 9
                                         	,UpdatedBy = @CreatedBy
                                         	,UpdatedAt = GETDATE()
                                         WHERE NoTER in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,','))
                                     ";
            return query;
        }
        public static string CreateVoucherSC()
        {
            string query = $@" 
				DECLARE @SubCategory INT = (SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01261') 
                DECLARE @NewVoucherIncrement varchar(max) 
                IF (YEAR(GETDATE()) > (SELECT TOP 1 YEAR(CreatedTime) FROM VoucherHeader order by Id desc)) --Reset to 1 after New Year
                    SET @NewVoucherIncrement = '1'
                ELSE
                    SET @NewVoucherIncrement = @LastVoucherNumber + 1
                DECLARE @DateFormat varchar (100) = (SELECT format(cast(GETDATE() as date),'yyyyMMdd'))
                DECLARE @NewVoucher varchar (100) = @NewVoucherIncrement + ' ' + UPPER(@Category) + ' ' + @DateFormat 
                DECLARE @Email      varchar(100) = (  SELECT TOP 1 vn.eMail from InvoicePO ipo
											            JOIN Vendor vn on vn.Id = ipo.VendorId WHERE ipo.InvoiceNumber in (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers,',')) and ipo.CategoryProcess_SubCategoryId = @SubCategory)
                                         

                DECLARE @Currency varchar(5)
                IF (@Category = 'Reimbursement')
                    SET @Currency =
                (
                    SELECT TOP 1
                        rd.L_Currency
                    FROM Reimbursement r
                        join ReimbursementDetail rd
                            on r.Id = rd.ReimbursementId
                    WHERE r.RequestNumber in ( @RequestNumbers )
                )
                ELSE IF (@Category = 'Shopping Cart')
                    SET @Currency =
                (
                    SELECT TOP 1
                        r.LCurrency
                    FROM InvoicePO r
                    WHERE CONCAT(r.Id, ' - ',   r.InvoiceNumber)
					IN (SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')) 
					AND r.CategoryProcess_SubCategoryId = @SubCategory
                )

                --Insert Header
                INSERT INTO VoucherHeader
                (
                    VoucherNumber,
                    TransactionDate,
                    L_Currency,
                    Category,
                    IsEmail,
                    Email,
                    BankTransferCode,
                    BankTransferName,
                    CheckerMCM,
                    [Status],
                    CreatedTime,
                    CreatedBy
                )
                OUTPUT INSERTED.*
                VALUES
                (@NewVoucher,
                    GETDATE(),
                    @Currency,
                    @Category,
                    @IsEmail,
                    @Email,
                    @BankTransferCode,
                    @BankTransferName,
                    @CheckerMCM,
                    0  ,
                    GETDATE(),
                    @CreatedBy
                )

                --Insert Detail
                DECLARE @VoucherID int = SCOPE_IDENTITY()

                INSERT INTO VoucherDetail
                (
                    VoucherId,
                    VoucherRefId,
                    [Description],
                    RateAmmount,
                    TotalBaseAmmount,
                    TotalOriginalAmmount,
                    [Status],
                    CreatedTime,
                    CreatedBy
                )

                select @VoucherID,
	                    CONCAT(ipo.id , ' - ', ipo.InvoiceNumber),
                        ipo.Remark,
                        ipo.RateAmmount,
                        ipo.TotalAmount,
                        ipo.TotalAmount,
                        0,
                        GETDATE(),
                        @CreatedBy
                from InvoicePO ipo
                JOIN PurchaseOrder PO ON PO.Id = ipo.PurchaeseOrderId
                where IPO.Id in (SELECT LTRIM(RTRIM((value))) FROM STRING_SPLIT(@RequestNumbers, ','))
                AND IPO.CategoryProcess_SubCategoryId = @SubCategory
			";
            return query;
        }
        public static string CreateVoucherNON()
        {
            string query = $@"
				DECLARE @SubCategory INT = (
					SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01262'
				),
						@Currency varchar(5)

				DECLARE @NewVoucherIncrement varchar(max)
				IF (YEAR(GETDATE()) >
					(
						SELECT TOP 1 YEAR(CreatedTime) FROM VoucherHeader order by Id desc
					)
					) --Reset to 1 after New Year
					SET @NewVoucherIncrement = '1'
				ELSE
					SET @NewVoucherIncrement = @LastVoucherNumber + 1
				DECLARE @DateFormat varchar(100) = (
														SELECT format(cast(GETDATE() as date), 'yyyyMMdd')
													)

				DECLARE @NewVoucher varchar(100) = @NewVoucherIncrement + ' ' + UPPER(@Category) + ' ' + @DateFormat

				DECLARE @Email varchar(100)
					=   (
							SELECT TOP 1
								vn.eMail
							FROM InvoicePO ipo
								JOIN Vendor vn
									ON vn.Id = ipo.VendorId
							WHERE ipo.InvoiceNumber IN (
															SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')
														)
									AND IPO.CategoryProcess_SubCategoryId = @SubCategory
						)


				IF (@Category = 'Non Shopping Cart')
					SET @Currency =
				(
					SELECT TOP 1
						r.LCurrency
					FROM InvoicePO r
					WHERE REPLACE(CONCAT(r.Id, '-', r.InvoiceNumber), ' ', '') in (
																						SELECT * FROM STRING_SPLIT(REPLACE(
																															@RequestNumbers,
																															' ',
																															''
																														), ',')
																					)
				)

				--Insert Header
				INSERT INTO VoucherHeader
				(
					VoucherNumber,
					TransactionDate,
					L_Currency,
					Category,
					IsEmail,
					Email,
					BankTransferCode,
					BankTransferName,
					CheckerMCM,
					[Status],
					CreatedTime,
					CreatedBy
				)
				OUTPUT INSERTED.*
				VALUES
				(@NewVoucher,
					GETDATE(),
					@Currency,
					@Category,
					@IsEmail,
					@Email,
					@BankTransferCode,
					@BankTransferName,
					@CheckerMCM,
					0  ,
					GETDATE(),
					@CreatedBy
				)

				--Insert Detail
				DECLARE @VoucherID int = SCOPE_IDENTITY()

				INSERT INTO VoucherDetail
				(
					VoucherId,
					VoucherRefId,
					[Description],
					RateAmmount,
					TotalBaseAmmount,
					TotalOriginalAmmount,
					[Status],
					CreatedTime,
					CreatedBy
				)
				SELECT @VoucherID,
						CONCAT(IPO.id, ' - ', IPO.InvoiceNumber),
						IPO.Remark,
						IPO.RateAmmount,
						IPO.TotalAmount,
						IPO.TotalAmount,
						0,
						GETDATE(),
						@CreatedBy
				FROM InvoicePO IPO
				JOIN PONonShopping PO ON PO.Id = IPO.PurchaeseOrderId
				WHERE IPO.Id IN (SELECT LTRIM(RTRIM((value))) FROM STRING_SPLIT(@RequestNumbers, ','))
				AND IPO.CategoryProcess_SubCategoryId = @SubCategory
			";
            return query;
        }
        #endregion

        #region Fin TREX
        public static string RejectFinanceTrexApr()
        {
            string query = $@" UPDATE TrexApr
                               SET 
                                 Status     = @Status,
                                 UpdatedAt  = @LastUpdatedTime,
                                 UpdatedBy  = @LastUpdatedBy,
								 RejectedAt = @LastUpdatedTime,
                                 RejectedBy = @LastUpdatedBy
                               WHERE NoAPR  = @RequestNumber";
            return query;
        }
        public static string RejectFinanceTrexEer()
        {
            string query = $@" UPDATE TrexEerHeader
                               SET 
                                 Status     = @Status,
                                 UpdatedAt  = @LastUpdatedTime,
                                 UpdatedBy  = @LastUpdatedBy,
								 RejectedAt = @LastUpdatedTime,
                                 RejectedBy = @LastUpdatedBy
                               WHERE NoEER  = @RequestNumber";
            return query;
        }
        public static string RejectFinanceTrexGer()
        {
            string query = $@" UPDATE TrexGerHeader
                               SET 
                                 Status     = @Status,
                                 UpdatedAt  = @LastUpdatedTime,
                                 UpdatedBy  = @LastUpdatedBy,
								 RejectedAt = @LastUpdatedTime,
                                 RejectedBy = @LastUpdatedBy
                               WHERE NoGER  = @RequestNumber";
            return query;
        }
        public static string RejectFinanceTrexTer()
        {
            string query = $@" UPDATE TrexTerHeader
                               SET 
                                 Status     = @Status,
                                 UpdatedAt  = @LastUpdatedTime,
                                 UpdatedBy  = @LastUpdatedBy,
								 RejectedAt = @LastUpdatedTime,
                                 RejectedBy = @LastUpdatedBy
                               WHERE NoTER  = @RequestNumber";
            return query;
        }
        public static string GetTrexAprDetail()
        {
            string query = $@" SELECT  [NoAPR] [RequestNumber]
                                       ,[TotalAmount]
                                       ,[BankAccountName] [BankAccountOwnerName]
                                       ,[BankName]
                                       ,[BankAccountNumber]
                                       ,'IDR' [Currency]
                                       ,'0' [Vat]
                                       ,'TrexApr' [RequestType]
                                FROM   [TrexAPR] ta
                                WHERE  ta.NoAPR = @RequestNumber";
            return query;
        }
        public static string GetTrexAprDetailList()
        {
            string query = $@" SELECT 'edit' [EditAble]
                                	  ,'IDR' [LCurrencyCode]
                                	  ,cc.Name [CostCenterName]
                                	  ,bu.Name [BusinessUnitName]
                                	  ,ta.Description [Quantity]
									  ,ta.TotalAmount [Amount]
                                	  ,am.AccountCode [Account]
                                	  ,'N/A' [FinanceExportStrategy]
                                	  ,'N/A' [Product]
                                	  ,'N/A' [Affiliate]
                                	  ,'N/A' [Product]
                                	  ,'N/A' [Project]
                                	  ,0 [Pph23]
                                	  ,0 [Ppn]
                                	  ,0 [PphFinal]
                                	  ,0 [StampDuty]
                                	  ,ta.CreatedAt [CreatedTime]
                                	  ,'trexapr' [RequestType]
                                	  ,ROW_NUMBER() OVER (ORDER BY ta.AprId) AS [CountData]
                                  FROM [TrexAPR] ta
                                  LEFT JOIN CostCenter cc on cc.Code = ta.DepartmentCode
                                  LEFT JOIN BusinessUnit bu on bu.Id = cc.BusinessUnitId
                                  LEFT JOIN AccountMaster am on am.AccountCode =  ta.AccountCode
                                  WHERE  ta.NoAPR = @RequestNumber";
            return query;
        }
        public static string GetTrexEerDetail()
        {
            string query = $@" SELECT  [NoEER] [RequestNumber]
                                       ,COALESCE(te.PaidAmount, 0) [TotalAmount]
                                       ,[BankAccountName] [BankAccountOwnerName]
                                       ,[BankName]
                                       ,[BankAccountNumber]
                                       ,'IDR' [Currency]
                                       ,'0' [Vat]
                                       ,'TrexEer' [RequestType]
                                FROM   [TrexEERHeader] te
                                WHERE  te.NoEER = @RequestNumber";
            return query;
        }
        public static string GetTrexEerDetailList()
        {
            string query = $@" SELECT 'edit' [EditAble]
                                	  ,'IDR' [LCurrencyCode]
                                	  ,cc.Name [CostCenterName]
                                	  ,bu.Name [BusinessUnitName]
                                	  ,te.Description [Quantity]
									  ,te.TotalAmount [Amount]
                                	  ,am.AccountCode [Account]
                                	  ,'N/A' [FinanceExportStrategy]
                                	  ,'N/A' [Product]
                                	  ,'N/A' [Affiliate]
									  ,'N/A' [Project]
                                	  ,0 [Pph23]
                                	  ,0 [Ppn]
                                	  ,0 [PphFinal]
                                	  ,0 [StampDuty]
                                	  ,te.CreatedAt [CreatedTime]
                                	  ,'trexeer' [RequestType]
                                	  ,ROW_NUMBER() OVER (ORDER BY te.EERId) AS [CountData]
                                  FROM [TrexEERHeader] te
								  LEFT JOIN TrexEERMonthlyDetail temd on te.EERId= temd.EERId
								  LEFT JOIN TrexEERNonMonthlyDetail tenmd on te.EERId =  tenmd.EERId
                                  LEFT JOIN CostCenter cc on cc.Code = te.DepartmentCode
                                  LEFT JOIN BusinessUnit bu on bu.Id = cc.BusinessUnitId
                                  LEFT JOIN AccountMaster am on am.AccountCode = ISNULL(temd.AccountCode, tenmd.AccountCode)
                                WHERE  te.NoEER = @RequestNumber";
            return query;
        }
        public static string GetTrexGerDetail()
        {
            string query = $@" SELECT 
						       tg.NoGER AS RequestNumber,
						       COALESCE(tg.PaidAmount, 0) AS TotalAmount,
						       tg.BankAccountName AS BankAccountOwnerName,
						       tg.BankName,
						       tg.BankAccountNumber,
						       'IDR' AS Currency,
						       SUM(
								    CASE 
								        WHEN LOWER(tgcd.TaxCode1) LIKE '%ppn%' 
								          OR LOWER(tgcd.TaxCode2) LIKE '%ppn%' 
								        THEN COALESCE(tgcd.Amount,0) * (COALESCE(tgcd.TaxRate1, 0)/100)
								        ELSE 0
								    END
								) AS Vat,
						       'TrexGer' AS RequestType,
						       CASE 
						           WHEN tg.GERType = 'Settlement' THEN tra.NoAPR
						           ELSE ''
						       END AS InvoiceNumber,
						       CASE 
							       WHEN tg.GERType = 'Settlement' THEN CONVERT(VARCHAR, tra.TglAPR, 23) -- Format: YYYY-MM-DD
							       ELSE ''
							   END AS InvoiceDate
						   FROM TrexGERHeader tg
						   JOIN TrexGERAccountCodeDetail tgcd ON tgcd.GERId = tg.GERId
						   LEFT JOIN TrexAPR tra ON tg.NoAPR = tra.APRId
						   WHERE tg.NoGER = @RequestNumber
						   GROUP BY 
						       tg.NoGER,
						       tg.PaidAmount,
						       tg.BankAccountName,
						       tg.BankName,
						       tg.BankAccountNumber,
						       tg.GERType,
						       tra.NoAPR,
						       tra.TglAPR";
            return query;
        }
        public static string GetTrexGerDetailList()
        {
            string query = $@" SELECT  
							    'edit' AS EditAble,
							    'IDR' AS LCurrencyCode,
							    cc.Name AS CostCenterName,
							    bu.Name AS BusinessUnitName,
							    CASE 
							        WHEN tg.GERType = 'Settlement' THEN
							            CASE 
							                WHEN (COALESCE(tg.TotalAmount, 0) - COALESCE(tg.APRAmount, 0)) < 0 THEN 
							                    CONCAT(tg.Description, ' - Refund')
							                ELSE 
							                    tg.Description
							            END
							        ELSE 
							            tg.Description
							    END AS Quantity,
							    
							    CASE 
							        WHEN tg.GERType = 'Reimbursement' THEN SUM(COALESCE(tgcd.Amount, 0))
							        ELSE COALESCE(tgcd.Amount, 0)
							    END AS Amount,
							
							    '' AS Account,
							    'N/A' AS FinanceExportStrategy,
							    'N/A' AS Affiliate,
							    0 AS Pph23,
							    CASE
								    WHEN LOWER(tgcd.TaxCode1) LIKE '%ppn%' THEN tgcd.Amount * (COALESCE(tgcd.TaxRate1, 0)/100)
								    WHEN LOWER(tgcd.TaxCode2) LIKE '%ppn%' THEN tgcd.Amount * (COALESCE(tgcd.TaxRate2, 0)/100)
								    ELSE 0
								END AS Ppn,
								CASE
								    WHEN LOWER(tgcd.TaxCode1) LIKE '%pph final%' THEN tgcd.Amount * (COALESCE(tgcd.TaxRate1, 0)/100)
								    WHEN LOWER(tgcd.TaxCode2) LIKE '%pph final%' THEN tgcd.Amount * (COALESCE(tgcd.TaxRate2, 0)/100)
								    ELSE 0
								END AS PphFinal,
							    COALESCE(tgcd.Materai, 0) StampDuty,
							    tg.CreatedAt AS CreatedTime,
								'trexger' [RequestType],
							    tg.GERType AS Product,
							    tg.RegionCode AS Project,
							    ROW_NUMBER() OVER (ORDER BY tg.GERId) AS CountData
							
							FROM TrexGERHeader tg
							JOIN TrexGERAccountCodeDetail tgcd ON tgcd.GERId = tg.GERId
							LEFT JOIN CostCenter cc ON cc.Code = tg.DepartmentCode
							LEFT JOIN BusinessUnit bu ON bu.Id = cc.BusinessUnitId
							WHERE tg.NoGER = @RequestNumber
							GROUP BY  
								cc.Name,
								bu.Name,
								tg.Description,
								tg.PaidAmount,
								tg.CreatedAt,
								tg.GERType,
								tg.RegionCode,
								tg.GERId,
								tg.TotalAmount,
								tg.APRAmount,
								tgcd.Amount,
								tgcd.TaxCode1,
								tgcd.TaxCode2,
								tgcd.TaxRate1,
								tgcd.TaxRate2,
								tgcd.Materai
                                ";
            return query;
        }
        public static string GetTrexTerDetail()
        {
            string query = $@"  SELECT 
							    tt.NoTER AS RequestNumber,
							    COALESCE(tda.TotalAmount, 0) +
							    COALESCE(tdd.TotalAmount, 0) +
							    COALESCE(tdt.TotalAmount, 0) +
							    COALESCE(tdo.TotalAmount, 0) AS TotalAmount,
							    tt.BankAccountName AS BankAccountOwnerName,
							    tt.BankName,
							    tt.BankAccountNumber,
							    CONCAT(tb.NoBTR, ' - ', tb.TravelDestination) AS InvoiceNumber,
							    'IDR' AS Currency,
							    '0' AS Vat,
							    'TrexTer' AS RequestType
							FROM TrexTERHeader tt
							LEFT JOIN (
							    SELECT TERId, SUM(Amount) AS TotalAmount
							    FROM TrexTERDetailAkomodasi
							    GROUP BY TERId
							) tda ON tda.TERId = tt.TERId
							LEFT JOIN (
							    SELECT TERId, SUM(Amount) AS TotalAmount
							    FROM TrexTERDetailDurasi
							    GROUP BY TERId
							) tdd ON tdd.TERId = tt.TERId
							LEFT JOIN (
							    SELECT TERId, SUM(Amount) AS TotalAmount
							    FROM TrexTERDetailTransportation
							    GROUP BY TERId
							) tdt ON tdt.TERId = tt.TERId
							LEFT JOIN (
							    SELECT TERId, SUM(Amount) AS TotalAmount
							    FROM TrexTERDetailOther
							    GROUP BY TERId
							) tdo ON tdo.TERId = tt.TERId
							JOIN TrexBTRHeader tb ON tb.BTRId = tt.BTRId
							WHERE tt.NoTER = @RequestNumber;
							   ";
            return query;
        }
        public static string GetTrexTerDetailList()
        {
            string query = $@" WITH DetailUnified AS (
							       SELECT TERId, CONCAT(TipeAkomodasi, ' - ', Remarks) as Remarks, Amount, 'Akomodasi' AS Type FROM TrexTERDetailAkomodasi
							       UNION ALL
							       SELECT TERId, CONCAT(Durasi, ' - ', TipeZona, ' - ', Remarks) AS Remarks, Amount, 'Durasi' AS Type FROM TrexTERDetailDurasi
							       UNION ALL
							       SELECT TERId, CONCAT(TipeTransportation, ' - ', Remarks) AS Remarks, Amount, 'Transportasi' AS Type FROM TrexTERDetailTransportation
							       UNION ALL
							       SELECT TERId, CONCAT(TipeOther, ' - ', Remarks) AS Remarks, Amount, 'Other' AS Type FROM TrexTERDetailOther
							   )
							   SELECT 
							       'edit' AS EditAble,
							       'IDR' AS LCurrencyCode,
							       '' AS CostCenterName,
							       '' AS BusinessUnitName,
							       du.Remarks AS Quantity,
							       CASE du.Type WHEN 'Akomodasi' THEN du.Amount ELSE 0 END AS Akomodasi,
							       CASE du.Type WHEN 'Durasi' THEN du.Amount ELSE 0 END AS Durasi,
							       CASE du.Type WHEN 'Transportasi' THEN du.Amount ELSE 0 END AS Transportasi,
							       CASE du.Type WHEN 'Other' THEN du.Amount ELSE 0 END AS Other,
							       '' AS Account,
							       'N/A' AS FinanceExportStrategy,
							       'N/A' AS Product,
							       'N/A' AS Affiliate,
							       0 AS Pph23,
							       0 AS Ppn,
							       0 AS PphFinal,
							       0 AS StampDuty,
							       tt.CreatedAt AS CreatedTime,
							       'trexter' [RequestType],
							       tbh.RegionCode AS Project,
							       ROW_NUMBER() OVER (ORDER BY tt.TERId) AS CountData
							   FROM TrexTERHeader tt
							   LEFT JOIN TrexBTRHeader tbh ON tbh.BTRId = tt.BTRId
							   JOIN DetailUnified du ON du.TERId = tt.TERId
                               WHERE  tt.NoTER = @RequestNumber";
            return query;
        }
        public static string GetTrexAprApprovalList()
        {
            string query = $@" SELECT ApprovalData.ApprovalDateString, ApprovalData.Status, ApprovalData.Username
							   FROM TrexAPR trex
							   CROSS APPLY (
							       VALUES
							           (Approval1At, Status, Approval1By),
							           (Approval2At, Status, Approval2By),
							           (Approval3At, Status, Approval3By),
							           (Approval4At, Status, Approval4By),
							           (Approval5At, Status, Approval5By),
							           (Approval6At, Status, Approval6By),
							           (Approval7At, Status, Approval7By),
							           (Approval8At, Status, Approval8By),
							           (Approval9At, Status, Approval9By)
							   ) AS ApprovalData(ApprovalDateString, Status, Username)
							   JOIN MasterTable mt on mt.ValueId = ApprovalData.Status
							   WHERE mt.Category = 'ApprovalRequest.Status' 
							   AND trex.NoAPR = @RequestNumber
							   AND ApprovalData.ApprovalDateString IS NOT NULL
			";
            return query;
        }
        public static string GetTrexEerApprovalList()
        {
            string query = $@" SELECT ApprovalData.ApprovalDateString, ApprovalData.Status, ApprovalData.Username
							   FROM TrexEERHeader trex
							   CROSS APPLY (
							       VALUES
							           (Approval1At, Status, Approval1By),
							           (Approval2At, Status, Approval2By),
							           (Approval3At, Status, Approval3By),
							           (Approval4At, Status, Approval4By),
							           (Approval5At, Status, Approval5By),
							           (Approval6At, Status, Approval6By),
							           (Approval7At, Status, Approval7By),
							           (Approval8At, Status, Approval8By),
							           (Approval9At, Status, Approval9By)
							   ) AS ApprovalData(ApprovalDateString, Status, Username)
							   JOIN MasterTable mt on mt.ValueId = ApprovalData.Status
							   WHERE mt.Category = 'ApprovalRequest.Status' 
							   AND trex.NoEER = @RequestNumber
							   AND ApprovalData.ApprovalDateString IS NOT NULL
			";
            return query;
        }
        public static string GetTrexGerApprovalList()
        {
            string query = $@" SELECT ApprovalData.ApprovalDateString, ApprovalData.Status, ApprovalData.Username
							   FROM TrexGERHeader trex
							   CROSS APPLY (
							       VALUES
							           (Approval1At, Status, Approval1By),
							           (Approval2At, Status, Approval2By),
							           (Approval3At, Status, Approval3By),
							           (Approval4At, Status, Approval4By),
							           (Approval5At, Status, Approval5By),
							           (Approval6At, Status, Approval6By),
							           (Approval7At, Status, Approval7By),
							           (Approval8At, Status, Approval8By),
							           (Approval9At, Status, Approval9By)
							   ) AS ApprovalData(ApprovalDateString, Status, Username)
							   JOIN MasterTable mt on mt.ValueId = ApprovalData.Status
							   WHERE mt.Category = 'ApprovalRequest.Status' 
							   AND trex.NoGER = @RequestNumber
							   AND ApprovalData.ApprovalDateString IS NOT NULL
			";
            return query;
        }
        public static string GetTrexTerApprovalList()
        {
            string query = $@" SELECT ApprovalData.ApprovalDateString, ApprovalData.Status, ApprovalData.Username
							   FROM TrexTERHeader trex
							   CROSS APPLY (
							       VALUES
							           (Approval1At, Status, Approval1By),
							           (Approval2At, Status, Approval2By),
							           (Approval3At, Status, Approval3By),
							           (Approval4At, Status, Approval4By),
							           (Approval5At, Status, Approval5By),
							           (Approval6At, Status, Approval6By),
							           (Approval7At, Status, Approval7By),
							           (Approval8At, Status, Approval8By),
							           (Approval9At, Status, Approval9By)
							   ) AS ApprovalData(ApprovalDateString, Status, Username)
							   JOIN MasterTable mt on mt.ValueId = ApprovalData.Status
							   WHERE mt.Category = 'ApprovalRequest.Status' 
							   AND trex.NoTER = @RequestNumber
							   AND ApprovalData.ApprovalDateString IS NOT NULL
			";
            return query;
        }
        public static string GetTrexAttachment()
        {
            string query = $@"SELECT 
						          Category,
						          0 AS Id,
						          0 AS RefId,
						          '-' AS RelatedTableName,
						          '' AS FullPath,
						          Description,
						          OriginalFileName,
						          '' AS Checksum,
						          '' AS CreatedBy,
						          GETDATE() AS CreatedTime,
						          '' AS LastUpdatedBy,
						          GETDATE() AS LastUpdatedTime
						      FROM (
						          SELECT 'apr' AS Category, NoAPR as OriginalFileName, APRId AS Description FROM TrexAPR
						          UNION
						          SELECT 'eer' AS Category, NoEER, EERId FROM TrexEERHeader
						          UNION
						          SELECT 'ger' AS Category, NoGER, GERId FROM TrexGERHeader
						          UNION
						          SELECT 'ter' AS Category, NoTER, TERId FROM TrexTERHeader
						      ) AS Combined
						      WHERE OriginalFileName = @RequestNumber
			";
            return query;
        }
        #endregion

        #region SHOP & NON FINANCE TO VOUCHER
        public string GetApprovalRequestListVC(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"SELECT DISTINCT 
							    sub.RequestNumber,
							    sub.RequestorName, 
							    sub.RequestorCostCenter, 
								sub.Status, 
								sub.StatusId, 
								sub.VendorName, 
								sub.VendorId, 
								sub.GrandTotal, 
								sub.Description, 
								sub.TransactionDate, 
								sub.CreatedTime, 
								sub.RequestType, 
								sub.Category, 
								sub.TotalApproval, 
								sub.TotalApprovalApproved, 
								sub.TotalApprovalRejected, 
								sub.ReasonReject, 
								sub.LCurrency,
								sub.LastUpdatedTime,
								sub.LastUpdatedBy,
								sub.GenerateId,
								COUNT(*) OVER () as CountData FROM (
		
				      	  select distinct
				          ipo.InvoiceNumber AS RequestNumber,
				          CONVERT(datetime, ipo.InvoiceDate, 103) AS TransactionDate,
				          ipo.InvoiceDate as CreatedTime,
				          vn.Name AS VendorName,
				          pr.RequestorUserName AS RequestorName,
				          cc.Name RequestorCostCenter,
				          (
				              SELECT TOP 1
				                  case
				                      when ShortDescription = 'Process' then
				                          'Approve'
				                  end
				              FROM MasterTable
				              WHERE Category = 'PurchaseOrder.Status'
				                    AND ValueId = po.Status
				          ) AS [Status],
						  0 StatusId,
				          0 [TotalApproval],
				          0 [TotalApprovalApproved],
				          0 [TotalApprovalRejected],
						  ipo.VendorId,
						  '' [ReasonReject],
						  ipo.LastUpdateTime as [LastUpdatedTime],
						  ipo.LastUpdateBy as [LastUpdatedBy],
				          'Shopping Cart' RequestType,
						  scv.SubCategoryName [Category],
				          ipo.TotalAmount [GrandTotal],
				          CONCAT(po.PONumber, ' - ', vn.Name) [Description],
				          ipo.Id GenerateId,
				          ipo.Status InvoiceStatus,
				      	  ipo.LCurrency
				      FROM InvoicePO ipo
				          join PurchaseOrder po
				              on ipo.PurchaeseOrderId = po.Id
				          join PurchaseOrderToPurchaseRequest prtopo
				              on po.Id = prtopo.PurchaseOrderId
				          join PurchaseRequest pr
				              on prtopo.PurchaseRequestlId = pr.Id
				          join Flips.UserAccount ua
				              on pr.RequestorAccountId = ua.Id
				          join Vendor vn
				              on ipo.VendorId = vn.Id
				          join CostCenter cc
				              on ua.CostCenterId = cc.Id
				          join PurchaseOrderTOP pot
				              on po.Id = pot.PurchaseOrderId
				          join DeliveryNotes dn
				              on dn.PurchaseOrderId = po.Id
					      join SubCategory scv
				              on scv.Id = vn.SubCategoryId
				      where ipo.InvoiceNumber IS NOT NULL AND ipo.CategoryProcess_SubCategoryId = (SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01261') 
				            AND ipo.Status = 7 and not exists(select top 1 VoucherRefId from VoucherDetail where VoucherRefId = CONCAT(ipo.Id, ' - ' , ipo.InvoiceNumber)) 
				      					) sub
						  WHERE 1=1 AND {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public string GetApprovalRequestListSC(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"
				DECLARE @SCSubCategoryId int = (
					SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01261'
				)
				SELECT DISTINCT
					PO.PONumber [RequestNumber],
					(
						CASE WHEN IPO.InvoiceDate > DNP.LastUpdatedTime THEN
							IPO.InvoiceDate
						ELSE
							DNP.LastUpdatedTime
						END
					) [TransactionDate],
					IPO.InvoiceDate [CreatedTime],
					VND.Name [VendorName],
					PO.RequestorName [RequestorName],
					COST.Name [RequestorCostCenter],
					(CASE
						WHEN MTL.ValueId = 2 THEN
							'Approve'
						WHEN MTL.ValueId = 3 THEN
							'Approve'
						END
					) [Status],
					CONVERT(INT, ISNULL((TA.TotalApproval + 1 + TADN.ApprovalDelivery + TAIPO.InvoicApproval), 0)) [TotalApproval],
					CONVERT(
								INT,
								ISNULL(
											TA2.TotalApproval + TAPO2.TotalPoApprove + TADN2.ApprovalDelivery
											+ TAIPO2.TotalApprovalIpo,
											0
										)
							) [TotalApprovalApproved],
					CONVERT(
								INT,
								ISNULL(
											TA3.TotalApprovalRejected + TAPO3.TotalPoReject + TADN3.RejectDelivery
											+ TAIPO3.RejectInvoice,
											0
										)
							) [TotalApprovalRejected],
					APR.RequestType,
					0 [GrandTotal],
					IPO.ReasonReject [ReasonReject],
					'-' [Description],
					IPO.Id [GenerateId],
					IPO.Status [InvoiceStatus],
					IPO.LCurrency [LCurrency],
					IPO.LastUpdateTime [LastUpdatedTime],
					IPO.LastUpdateBy [LastUpdatedBy],
					PO.Status [StatusId],
					SCV.SubCategoryName [Category],
					PO.VendorId
				FROM InvoicePO IPO
					JOIN PurchaseOrder PO
						ON IPO.PurchaeseOrderId = PO.Id
					JOIN DeliveryNotes DELN
						ON PO.Id = DELN.PurchaseOrderId
					JOIN Vendor VND
						ON IPO.VendorId = VND.Id
					JOIN Flips.UserAccount UAC
						ON PO.RequestorName = UAC.Username
					JOIN CostCenter COST
						ON UAC.CostCenterId = COST.Id
					JOIN MasterTable MTL
						ON PO.Status = MTL.ValueId
					JOIN PurchaseOrderToPurchaseRequest PRPO
						ON PRPO.PurchaseOrderId = PO.Id
					JOIN PurchaseRequest PR
						ON PRPO.PurchaseRequestlId = PR.Id
					JOIN PurchaseRequestItemDetail PRD
						ON PRPO.PurchaseRequestlId = PRD.PurchaseRequestId
					JOIN SubCategory SCV
						ON SCV.Id = VND.SubCategoryId
					JOIN DeliveryNotesPayment DNP
						ON  DELN.Id = DNP.DeliveryNotesId 
						AND DNP.PurchaseOrderTOPId = IPO.PurchaseOrderTOPId
						AND DNP.Status = 2
					JOIN
					(
						SELECT AR.Id,
								(CASE
									WHEN AR.RequestNo LIKE '%RI%' THEN
										'Reimbursement'
									WHEN AR.RequestNo LIKE '%CA%' THEN
										'Cash Advance'
									WHEN AR.RequestNo LIKE '%TR%' THEN
										'Travel'
									WHEN AR.Remark LIKE '%BUDGETSHOPPINGCARTV2%' THEN
										'Shopping Cart'
									WHEN AR.Remark LIKE '%NONBUDGETSHOPPINGCARTV2%' THEN
										'Non Shopping Cart'
								END
								) AS RequestType
						FROM ApprovalRequest AR
						where AR.Remark LIKE '%BUDGETSHOPPINGCARTV2%'
					) APR
						ON PRD.ApprovalRequestId = APR.Id
				LEFT JOIN
				(
					SELECT AR.RequestNo,
							COUNT(*) TotalApproval
					FROM ApprovalRequestGroupMember ARGM
						JOIN ApprovalRequest AR
							ON ARGM.ApprovalRequestId = AR.Id
					GROUP BY AR.RequestNo
				) TA
					ON PR.RequestCode = TA.RequestNo

					OUTER APPLY
				(
					SELECT COUNT(*) ApprovalDelivery
					FROM PurchaseOrderTOP POT1
						JOIN DeliveryNotesPayment dnp
							ON POT1.Id = DNP.PurchaseOrderTOPId
						JOIN DeliveryNotesDetail DND
							ON DNP.Id = DND.DeliveryNotesPaymentId
					WHERE POT1.PurchaseOrderId = PO.Id
							AND DNP.CategoryProcess_SubCategoryId = @SCSubCategoryId
				) TADN
					OUTER APPLY
				(
					SELECT COUNT(*) InvoicApproval
					FROM InvoicePO INV
					WHERE INV.PurchaeseOrderId = PO.Id
							AND INV.CategoryProcess_SubCategoryId = @SCSubCategoryId
				) TAIPO
					LEFT JOIN
					(
						SELECT AR1.RequestNo,
								COUNT(*) TotalApproval
						FROM ApprovalRequestGroupMember ARGM
							JOIN ApprovalRequest ar1
								ON ARGM.ApprovalRequestId = ar1.Id
						where ARGM.Status = 2 /*Approved*/
						GROUP BY AR1.RequestNo
					) TA2
						ON PR.RequestCode = TA2.RequestNo
					OUTER APPLY
				(
					SELECT COUNT(*) TotalPoApprove
					FROM PurchaseOrder PON1
					WHERE PON1.Status IN (2, 3)
							AND PON1.Id = PO.Id
				) TAPO2
					OUTER APPLY
				(
					SELECT COUNT(*) ApprovalDelivery
					FROM PurchaseOrderTOP POT1
						JOIN DeliveryNotesPayment dnp
							ON POT1.Id = DNP.PurchaseOrderTOPId
						JOIN DeliveryNotesDetail DND
							ON DNP.Id = DND.DeliveryNotesPaymentId
					WHERE POT1.PurchaseOrderId = PO.Id
							AND DNP.CategoryProcess_SubCategoryId = @SCSubCategoryId
							AND DND.Status = 2
				) TADN2
					OUTER APPLY
				(
					SELECT COUNT(*) TotalApprovalIpo
					FROM InvoicePO IPOA
					WHERE IPOA.PurchaeseOrderId = PO.Id
							AND IPOA.Status = 2
							AND IPOA.CategoryProcess_SubCategoryId = @SCSubCategoryId
				) TAIPO2
					LEFT JOIN
					(
						SELECT AR.Id,
								COUNT(*) TotalApprovalRejected
						FROM ApprovalRequestGroupMember ARGM
							JOIN ApprovalRequest AR
								ON ARGM.ApprovalRequestId = ar.Id
						WHERE ARGM.Status = 3
						GROUP BY AR.Id
					) TA3
						ON PRD.ApprovalRequestId = TA3.Id
					OUTER APPLY
				(
					SELECT COUNT(*) TotalPoReject
					FROM PurchaseOrder PON1
					WHERE PON1.Status = 6
							and PON1.Id = PO.Id
				) TAPO3
					OUTER APPLY
				(
					SELECT COUNT(*) RejectDelivery
					FROM PurchaseOrderTOP POT1
						JOIN DeliveryNotesPayment DNP
							ON POT1.Id = DNP.PurchaseOrderTOPId
						JOIN DeliveryNotesDetail DND
							ON DNP.Id = DND.DeliveryNotesPaymentId
					WHERE POT1.PurchaseOrderId = PO.Id
							AND DND.Status = 3
							AND DNP.CategoryProcess_SubCategoryId = @SCSubCategoryId
				) TADN3
					OUTER APPLY
				(
					SELECT COUNT(*) RejectInvoice
					FROM InvoicePO IPOA
					WHERE IPOA.PurchaeseOrderId = PO.Id
							AND ipoA.Status = 5
							AND IPOA.CategoryProcess_SubCategoryId = @SCSubCategoryId
				) TAIPO3
				WHERE IPO.CategoryProcess_SubCategoryId = @SCSubCategoryId
						AND IPO.Status NOT IN ( 8, 7 )
						AND DELN.Status NOT IN ( 5 )
						AND DELN.CategoryProcess_SubCategoryId = @SCSubCategoryId
						AND MTL.Category = 'PurchaseOrder.Status'
				) sub
				WHERE 1 = 1 AND {subQuery}
			";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public string GetApprovalRequestListNonShop(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"
				DECLARE @NONSubCategoryId int = (
												SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01262'
											)

				SELECT DISTINCT
				sub.RequestNumber,
				sub.RequestorName,
				sub.RequestorCostCenter,
				sub.Status,
				sub.StatusId,
				sub.VendorName,
				sub.VendorId,
				sub.GrandTotal,
				sub.Description,
				sub.TransactionDate,
				sub.CreatedTime,
				sub.RequestType,
				sub.TotalApproval,
				sub.TotalApprovalApproved,
				sub.TotalApprovalRejected,
				sub.ReasonReject,
				sub.LastUpdatedTime,
				sub.LastUpdatedBy,
				sub.Category,
				sub.GenerateId,
				sub.LCurrency,
				COUNT(*) OVER () as CountData
				FROM
				(
				SELECT DISTINCT
					PON.PONumber [RequestNumber],
					IPO.InvoiceDate [TransactionDate],
					IPO.InvoiceDate [CreatedTime],
					VND.Name [VendorName],
					PON.RequestorName [RequestorName],
					COST.Name [RequestorCostCenter],
					(CASE
							WHEN MTL.ValueId = 2 THEN
								'Approve'
							WHEN MTL.ValueId = 3 THEN
								'Approve'
						END
					) [Status],
					CONVERT(INT, ISNULL((TA.TotalApproval + 1 + TADN.ApprovalDelivery + TAIPO.InvoicApproval), 0)) [TotalApproval],
					CONVERT(
								INT,
								ISNULL(
											TA2.TotalApproval + TAPO2.TotalPoApprove + TADN2.ApprovalDelivery
											+ TAIPO2.TotalApprovalIpo,
											0
										)
							) [TotalApprovalApproved],
					CONVERT(
								INT,
								ISNULL(
											TA3.TotalApprovalRejected + TAPO3.TotalPoReject + TADN3.RejectDelivery
											+ TAIPO3.RejectInvoice,
											0
										)
							) [TotalApprovalRejected],
					APR.RequestType,
					0 [GrandTotal],
					IPO.ReasonReject [ReasonReject],
					'-' [Description],
					IPO.Id [GenerateId],
					IPO.Status [InvoiceStatus],
					IPO.LCurrency [LCurrency],
					IPO.LastUpdateTime [LastUpdatedTime],
					IPO.LastUpdateBy [LastUpdatedBy],
					PON.Status [StatusId],
					scv.SubCategoryName [Category],
					PON.VendorId
				FROM InvoicePO IPO
					JOIN PONonShopping PON
						ON IPO.PurchaeseOrderId = PON.Id
					JOIN DeliveryNotes DELN
						ON PON.Id = DELN.PurchaseOrderId
					JOIN Vendor VND
						ON IPO.VendorId = VND.Id
					JOIN Flips.UserAccount UAC
						ON PON.RequestorName = UAC.Username
					JOIN CostCenter COST
						ON UAC.CostCenterId = COST.Id
					JOIN MasterTable MTL
						ON PON.Status = MTL.ValueId
					JOIN PRFSummary PRFS
						ON PON.PRFSummaryId = PRFS.Id
					JOIN SubCategory SCV
						ON SCV.Id = VND.SubCategoryId
					JOIN DeliveryNotesPayment DNP
						ON  DELN.Id = DNP.DeliveryNotesId 
						AND DNP.PurchaseOrderTOPId = IPO.PurchaseOrderTOPId
						AND DNP.Status = 2
					JOIN
					(
						SELECT AR.Id,
								(CASE
									WHEN AR.RequestNo LIKE '%RI%' THEN
										'Reimbursement'
									WHEN AR.RequestNo LIKE '%CA%' THEN
										'Cash Advance'
									WHEN AR.RequestNo LIKE '%TR%' THEN
										'Travel'
									WHEN AR.RequestNo LIKE '%SC%' THEN
										'Shopping Cart'
									WHEN AR.RequestNo LIKE '%PROCSUM%' THEN
										'Non Shopping Cart'
								END
								) AS RequestType
						FROM ApprovalRequest AR
						where AR.RequestNo LIKE '%PROCSUM%'
					) APR
						ON PRFS.ApprovalRequestId = APR.Id
					LEFT JOIN
					(
						SELECT AR.Id,
								COUNT(*) TotalApproval
						FROM ApprovalRequestGroupMember ARGM
							JOIN ApprovalRequest AR
								ON ARGM.ApprovalRequestId = AR.Id
						GROUP BY AR.Id
					) TA
						ON PRFS.ApprovalRequestId = TA.Id
					OUTER APPLY
				(
					SELECT COUNT(*) ApprovalDelivery
					FROM PONonShoppingTOP POT1
						JOIN DeliveryNotesPayment dnp
							ON POT1.Id = DNP.PurchaseOrderTOPId
						JOIN DeliveryNotesDetail DND
							ON DNP.Id = DND.DeliveryNotesPaymentId
					WHERE POT1.PONonShoppingId = PON.Id
							AND DNP.CategoryProcess_SubCategoryId = @NONSubCategoryId
				) TADN
					OUTER APPLY
				(
					SELECT COUNT(*) InvoicApproval
					FROM InvoicePO INV
					WHERE INV.PurchaeseOrderId = PON.Id
							AND INV.CategoryProcess_SubCategoryId = @NONSubCategoryId
				) TAIPO
					LEFT JOIN
					(
						SELECT AR1.Id,
								COUNT(*) TotalApproval
						FROM ApprovalRequestGroupMember ARGM
							JOIN ApprovalRequest ar1
								ON ARGM.ApprovalRequestId = ar1.Id
						where ARGM.Status = 2 /*Approved*/
						GROUP BY AR1.Id
					) TA2
						ON PRFS.ApprovalRequestId = TA2.Id
					OUTER APPLY
				(
					SELECT COUNT(*) TotalPoApprove
					FROM PONonShopping PON1
					WHERE PON1.Status IN (2, 3)
							and PON1.Id = PON.Id
				) TAPO2
					OUTER APPLY
				(
					SELECT COUNT(*) ApprovalDelivery
					FROM PONonShoppingTOP POT1
						JOIN DeliveryNotesPayment DNP
							ON POT1.Id = DNP.PurchaseOrderTOPId
						JOIN DeliveryNotesDetail DND
							ON DNP.Id = DND.DeliveryNotesPaymentId
					WHERE POT1.PONonShoppingId = PON.Id
							AND DND.Status = 2
							AND DNP.CategoryProcess_SubCategoryId = @NONSubCategoryId
				) TADN2
					OUTER APPLY
				(
					SELECT COUNT(*) TotalApprovalIpo
					FROM InvoicePO IPOA
					WHERE IPOA.PurchaeseOrderId = PON.Id
							AND ipoA.Status = 2
							AND IPOA.CategoryProcess_SubCategoryId = @NONSubCategoryId
				) TAIPO2
					LEFT JOIN
					(
						SELECT AR.Id,
								COUNT(*) TotalApprovalRejected
						FROM ApprovalRequestGroupMember ARGM
							JOIN ApprovalRequest AR
								ON ARGM.ApprovalRequestId = ar.Id
						WHERE ARGM.Status = 3
						GROUP BY AR.Id
					) TA3
						ON PRFS.ApprovalRequestId = TA3.Id
					OUTER APPLY
				(
					SELECT COUNT(*) TotalPoReject
					FROM PONonShopping PON1
					WHERE PON1.Status = 6
							AND PON1.Id = PON.Id
				) TAPO3
					OUTER APPLY
				(
					SELECT COUNT(*) RejectDelivery
					FROM PONonShoppingTOP POT1
						JOIN DeliveryNotesPayment dnp
							ON POT1.Id = DNP.PurchaseOrderTOPId
						JOIN DeliveryNotesDetail DND
							ON DNP.Id = DND.DeliveryNotesPaymentId
					WHERE POT1.PONonShoppingId = PON.Id
							AND DND.Status = 3
							AND DNP.CategoryProcess_SubCategoryId = @NONSubCategoryId
				) TADN3
					OUTER APPLY
				(
					SELECT COUNT(*) RejectInvoice
					FROM InvoicePO IPOA
					WHERE IPOA.PurchaeseOrderId = PON.Id
							AND ipoA.Status = 5
				) TAIPO3
				WHERE IPO.CategoryProcess_SubCategoryId = @NONSubCategoryId
						AND IPO.Status NOT IN ( 8, 7 )
						AND DELN.Status NOT IN ( 5 )
						AND DELN.CategoryProcess_SubCategoryId = @NONSubCategoryId
						AND MTL.Category = 'PurchaseOrder.Status'
				) sub
				WHERE 1 = 1 AND {subQuery}
			";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public string GetApprovalRequestListNonVC(ParamGetRequestList param)
        {
            string subQuery = ApprovalListSubQuery(param);

            string qry = $@"SELECT DISTINCT
								SUB.RequestNumber,
								SUB.RequestorName,
								SUB.RequestorCostCenter,
								SUB.Status,
								SUB.StatusId,
								SUB.VendorName,
								SUB.VendorId,
								SUB.GrandTotal,
								SUB.Description,
								SUB.TransactionDate,
								SUB.CreatedTime,
								SUB.RequestType,
								SUB.Category,
								SUB.TotalApproval,
								SUB.TotalApprovalApproved,
								SUB.TotalApprovalRejected,
								SUB.ReasonReject,
								SUB.LCurrency,
								SUB.LastUpdatedTime,
								SUB.LastUpdatedBy,
								SUB.GenerateId,
								COUNT(*) OVER () as CountData
							FROM
							(
							SELECT DISTINCT
								INV.InvoiceNumber [RequestNumber],
								CONVERT(datetime, INV.InvoiceDate, 103) [TransactionDate],
								INV.InvoiceDate [CreatedTime],
								VND.Name [VendorName],
								PO.RequestorName [RequestorName],
								COST.Name [RequestorCostCenter],
								(
									SELECT TOP 1
										CASE
											WHEN ShortDescription = 'Process' THEN
												'Approve'
										END
									FROM MasterTable
									WHERE Category = 'PurchaseOrder.Status'
										  AND ValueId = PO.Status
								) AS [Status],
								0 [StatusId],
								0 [TotalApproval],
								0 [TotalApprovalApproved],
								0 [TotalApprovalRejected],
								INV.VendorId,
								'' [ReasonReject],
								INV.LastUpdateTime as [LastUpdatedTime],
								INV.LastUpdateBy as [LastUpdatedBy],
								'Non Shopping Cart' RequestType,
								SCV.SubCategoryName [Category],
								INV.TotalAmount [GrandTotal],
								CONCAT(po.PONumber, ' - ', VND.Name) [Description],
								INV.Id GenerateId,
								INV.Status InvoiceStatus,
								INV.LCurrency
							FROM InvoicePO INV
							JOIN PONonShopping PO ON INV.PurchaeseOrderId = PO.Id
							JOIN Vendor VND ON INV.VendorId = VND.Id
							JOIN Flips.UserAccount UA ON PO.RequestorName = UA.Username
							JOIN CostCenter COST ON UA.CostCenterId = COST.Id
							JOIN BusinessUnit BU ON COST.BusinessUnitId = BU.Id
							JOIN SubCategory SCV ON VND.SubCategoryId = SCV.Id
							WHERE INV.InvoiceNumber IS NOT NULL
									 AND INV.Status = 7
									 AND INV.CategoryProcess_SubCategoryId = (SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01262')
									 AND NOT EXISTS
							   (
								   SELECT TOP 1
									   VoucherRefId
								   FROM VoucherDetail
								   WHERE VoucherRefId = CONCAT(INV.Id, ' - ', INV.InvoiceNumber)
							   )
							) SUB";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }

        public string GetApprovalRequestListPO(ParamGetRequestList param)
        {
            string subQuery = $@"(RequestNumber LIKE @RequestNumber)
								  AND VendorId = @VendorId
							      AND Category = @Category
								  AND CONVERT(datetime, TransactionDate, 103) BETWEEN CONVERT(varchar(50), @RequestDateFrom, 103) AND CONVERT(varchar(50), @RequestDateTo, 103) ";
            if (String.IsNullOrEmpty(param.RequestNumber))
            {
                subQuery = subQuery.Replace("(RequestNumber LIKE @RequestNumber)", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.VendorId))
            {
                subQuery = subQuery.Replace("VendorId = @VendorId", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.Category))
            {
                subQuery = subQuery.Replace("Category = @Category", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.RequestDateFrom))
            {
                subQuery = subQuery.Replace("CONVERT(datetime, TransactionDate, 103) BETWEEN CONVERT(varchar(50), @RequestDateFrom, 103) AND CONVERT(varchar(50), @RequestDateTo, 103)", " 1=1 ");
            }
            if (!String.IsNullOrEmpty(param.Search?.Value))
            {
                subQuery = String.Concat(subQuery, " AND (RequestNumber LIKE @Search OR RequestorName LIKE @Search OR VendorName LIKE @Search) ");
            }

            string qry = $@"
				DECLARE @SCSubCategoryId INT = (
						SELECT Id
						FROM SubCategory
						WHERE SubCategoryCode = 'SC-2024-02-01261'
						)
				DECLARE @NONSubCategoryId INT = (
						SELECT Id
						FROM SubCategory
						WHERE SubCategoryCode = 'SC-2024-02-01262'
						)

				SELECT 
					allSub.RequestNumber
					,allSub.RequestorName
					,allSub.RequestorCostCenter
					,allSub.STATUS
					,allSub.StatusId
					,allSub.VendorName
					,allSub.VendorId
					,allSub.GrandTotal
					,allSub.Description
					,allSub.TransactionDate
					,allSub.CreatedTime
					,allSub.RequestType
					,allSub.TotalApproval
					,allSub.TotalApprovalApproved
					,allSub.TotalApprovalRejected
					,allSub.ReasonReject
					,allSub.LastUpdatedTime
					,allSub.LastUpdatedBy
					,allSub.Category
					,allSub.GenerateId
					,allSub.LCurrency
					,COUNT(*) OVER () AS CountData
				FROM (
					SELECT DISTINCT sub.RequestNumber
						,sub.RequestorName
						,sub.RequestorCostCenter
						,sub.STATUS
						,sub.StatusId
						,sub.VendorName
						,sub.VendorId
						,sub.GrandTotal
						,sub.Description
						,sub.TransactionDate
						,sub.CreatedTime
						,sub.RequestType
						,sub.TotalApproval
						,sub.TotalApprovalApproved
						,sub.TotalApprovalRejected
						,sub.ReasonReject
						,sub.LastUpdatedTime
						,sub.LastUpdatedBy
						,sub.Category
						,sub.GenerateId
						,sub.LCurrency
						,COUNT(*) OVER () AS CountData
					FROM (
						SELECT DISTINCT PO.PONumber [RequestNumber]
							,(
								CASE 
									WHEN IPO.InvoiceDate > DNP.LastUpdatedTime
										THEN IPO.InvoiceDate
									ELSE DNP.LastUpdatedTime
									END
								) [TransactionDate]
							,IPO.InvoiceDate [CreatedTime]
							,VND.NAME [VendorName]
							,PO.RequestorName [RequestorName]
							,COST.NAME [RequestorCostCenter]
							,(
								CASE 
									WHEN MTL.ValueId = 2
										THEN 'Approve'
									WHEN MTL.ValueId = 3
										THEN 'Approve'
									END
								) [Status]
							,CONVERT(INT, ISNULL((TA.TotalApproval + 1 + TADN.ApprovalDelivery + TAIPO.InvoicApproval), 0)) [TotalApproval]
							,CONVERT(INT, ISNULL(TA2.TotalApproval + TAPO2.TotalPoApprove + TADN2.ApprovalDelivery + TAIPO2.TotalApprovalIpo, 0)) [TotalApprovalApproved]
							,CONVERT(INT, ISNULL(TA3.TotalApprovalRejected + TAPO3.TotalPoReject + TADN3.RejectDelivery + TAIPO3.RejectInvoice, 0)) [TotalApprovalRejected]
							,APR.RequestType
							,0 [GrandTotal]
							,IPO.ReasonReject [ReasonReject]
							,'-' [Description]
							,IPO.Id [GenerateId]
							,IPO.STATUS [InvoiceStatus]
							,IPO.LCurrency [LCurrency]
							,IPO.LastUpdateTime [LastUpdatedTime]
							,IPO.LastUpdateBy [LastUpdatedBy]
							,PO.STATUS [StatusId]
							,SCV.SubCategoryName [Category]
							,PO.VendorId
						FROM InvoicePO IPO
						INNER JOIN PurchaseOrder PO ON IPO.PurchaeseOrderId = PO.Id
						INNER JOIN DeliveryNotes DELN ON PO.Id = DELN.PurchaseOrderId
						INNER JOIN Vendor VND ON IPO.VendorId = VND.Id
						INNER JOIN Flips.UserAccount UAC ON PO.RequestorName = UAC.Username
						INNER JOIN CostCenter COST ON UAC.CostCenterId = COST.Id
						INNER JOIN MasterTable MTL ON PO.STATUS = MTL.ValueId
						INNER JOIN PurchaseOrderToPurchaseRequest PRPO ON PRPO.PurchaseOrderId = PO.Id
						INNER JOIN PurchaseRequest PR ON PRPO.PurchaseRequestlId = PR.Id
						INNER JOIN PurchaseRequestItemDetail PRD ON PRPO.PurchaseRequestlId = PRD.PurchaseRequestId
						INNER JOIN SubCategory SCV ON SCV.Id = VND.SubCategoryId
						INNER JOIN DeliveryNotesPayment DNP ON DELN.Id = DNP.DeliveryNotesId
							AND DNP.PurchaseOrderTOPId = IPO.PurchaseOrderTOPId
							AND DNP.STATUS = 2
						INNER JOIN (
							SELECT AR.Id
								,(
									CASE 
										WHEN AR.RequestNo LIKE '%RI%'
											THEN 'Reimbursement'
										WHEN AR.RequestNo LIKE '%CA%'
											THEN 'Cash Advance'
										WHEN AR.RequestNo LIKE '%TR%'
											THEN 'Travel'
										WHEN AR.Remark LIKE '%BUDGETSHOPPINGCARTV2%'
											THEN 'Shopping Cart'
										WHEN AR.Remark LIKE '%NONBUDGETSHOPPINGCARTV2%'
											THEN 'Non Shopping Cart'
										END
									) AS RequestType
							FROM ApprovalRequest AR
							WHERE AR.Remark LIKE '%BUDGETSHOPPINGCARTV2%'
							) APR ON PRD.ApprovalRequestId = APR.Id
						LEFT JOIN (
							SELECT AR.RequestNo
								,COUNT(*) TotalApproval
							FROM ApprovalRequestGroupMember ARGM
							INNER JOIN ApprovalRequest AR ON ARGM.ApprovalRequestId = AR.Id
							GROUP BY AR.RequestNo
							) TA ON PR.RequestCode = TA.RequestNo
						OUTER APPLY (
							SELECT COUNT(*) ApprovalDelivery
							FROM PurchaseOrderTOP POT1
							INNER JOIN DeliveryNotesPayment dnp ON POT1.Id = DNP.PurchaseOrderTOPId
							INNER JOIN DeliveryNotesDetail DND ON DNP.Id = DND.DeliveryNotesPaymentId
							WHERE POT1.PurchaseOrderId = PO.Id
								AND DNP.CategoryProcess_SubCategoryId = @SCSubCategoryId
							) TADN
						OUTER APPLY (
							SELECT COUNT(*) InvoicApproval
							FROM InvoicePO INV
							WHERE INV.PurchaeseOrderId = PO.Id
								AND INV.CategoryProcess_SubCategoryId = @SCSubCategoryId
							) TAIPO
						LEFT JOIN (
							SELECT AR1.RequestNo
								,COUNT(*) TotalApproval
							FROM ApprovalRequestGroupMember ARGM
							INNER JOIN ApprovalRequest ar1 ON ARGM.ApprovalRequestId = ar1.Id
							WHERE ARGM.STATUS = 2 /*Approved*/
							GROUP BY AR1.RequestNo
							) TA2 ON PR.RequestCode = TA2.RequestNo
						OUTER APPLY (
							SELECT COUNT(*) TotalPoApprove
							FROM PurchaseOrder PON1
							WHERE PON1.STATUS IN (
									2
									,3
									)
								AND PON1.Id = PO.Id
							) TAPO2
						OUTER APPLY (
							SELECT COUNT(*) ApprovalDelivery
							FROM PurchaseOrderTOP POT1
							INNER JOIN DeliveryNotesPayment dnp ON POT1.Id = DNP.PurchaseOrderTOPId
							INNER JOIN DeliveryNotesDetail DND ON DNP.Id = DND.DeliveryNotesPaymentId
							WHERE POT1.PurchaseOrderId = PO.Id
								AND DNP.CategoryProcess_SubCategoryId = @SCSubCategoryId
								AND DND.STATUS = 2
							) TADN2
						OUTER APPLY (
							SELECT COUNT(*) TotalApprovalIpo
							FROM InvoicePO IPOA
							WHERE IPOA.PurchaeseOrderId = PO.Id
								AND IPOA.STATUS = 2
								AND IPOA.CategoryProcess_SubCategoryId = @SCSubCategoryId
							) TAIPO2
						LEFT JOIN (
							SELECT AR.Id
								,COUNT(*) TotalApprovalRejected
							FROM ApprovalRequestGroupMember ARGM
							INNER JOIN ApprovalRequest AR ON ARGM.ApprovalRequestId = ar.Id
							WHERE ARGM.STATUS = 3
							GROUP BY AR.Id
							) TA3 ON PRD.ApprovalRequestId = TA3.Id
						OUTER APPLY (
							SELECT COUNT(*) TotalPoReject
							FROM PurchaseOrder PON1
							WHERE PON1.STATUS = 6
								AND PON1.Id = PO.Id
							) TAPO3
						OUTER APPLY (
							SELECT COUNT(*) RejectDelivery
							FROM PurchaseOrderTOP POT1
							INNER JOIN DeliveryNotesPayment DNP ON POT1.Id = DNP.PurchaseOrderTOPId
							INNER JOIN DeliveryNotesDetail DND ON DNP.Id = DND.DeliveryNotesPaymentId
							WHERE POT1.PurchaseOrderId = PO.Id
								AND DND.STATUS = 3
								AND DNP.CategoryProcess_SubCategoryId = @SCSubCategoryId
							) TADN3
						OUTER APPLY (
							SELECT COUNT(*) RejectInvoice
							FROM InvoicePO IPOA
							WHERE IPOA.PurchaeseOrderId = PO.Id
								AND ipoA.STATUS = 5
								AND IPOA.CategoryProcess_SubCategoryId = @SCSubCategoryId
							) TAIPO3
						WHERE IPO.CategoryProcess_SubCategoryId = @SCSubCategoryId
							AND IPO.STATUS NOT IN (
								8
								,7
								)
							AND DELN.STATUS NOT IN (5)
							AND DELN.CategoryProcess_SubCategoryId = @SCSubCategoryId
							AND MTL.Category = 'PurchaseOrder.Status'
						) sub
					WHERE 1 = 1 AND {subQuery}

					UNION ALL

					SELECT DISTINCT sub.RequestNumber
						,sub.RequestorName
						,sub.RequestorCostCenter
						,sub.STATUS
						,sub.StatusId
						,sub.VendorName
						,sub.VendorId
						,sub.GrandTotal
						,sub.Description
						,sub.TransactionDate
						,sub.CreatedTime
						,sub.RequestType
						,sub.TotalApproval
						,sub.TotalApprovalApproved
						,sub.TotalApprovalRejected
						,sub.ReasonReject
						,sub.LastUpdatedTime
						,sub.LastUpdatedBy
						,sub.Category
						,sub.GenerateId
						,sub.LCurrency
						,COUNT(*) OVER () AS CountData
					FROM (
						SELECT DISTINCT PON.PONumber [RequestNumber]
							,IPO.InvoiceDate [TransactionDate]
							,IPO.InvoiceDate [CreatedTime]
							,VND.NAME [VendorName]
							,PON.RequestorName [RequestorName]
							,COST.NAME [RequestorCostCenter]
							,(
								CASE 
									WHEN MTL.ValueId = 2
										THEN 'Approve'
									WHEN MTL.ValueId = 3
										THEN 'Approve'
									END
								) [Status]
							,CONVERT(INT, ISNULL((TA.TotalApproval + 1 + TADN.ApprovalDelivery + TAIPO.InvoicApproval), 0)) [TotalApproval]
							,CONVERT(INT, ISNULL(TA2.TotalApproval + TAPO2.TotalPoApprove + TADN2.ApprovalDelivery + TAIPO2.TotalApprovalIpo, 0)) [TotalApprovalApproved]
							,CONVERT(INT, ISNULL(TA3.TotalApprovalRejected + TAPO3.TotalPoReject + TADN3.RejectDelivery + TAIPO3.RejectInvoice, 0)) [TotalApprovalRejected]
							,APR.RequestType
							,0 [GrandTotal]
							,IPO.ReasonReject [ReasonReject]
							,'-' [Description]
							,IPO.Id [GenerateId]
							,IPO.STATUS [InvoiceStatus]
							,IPO.LCurrency [LCurrency]
							,IPO.LastUpdateTime [LastUpdatedTime]
							,IPO.LastUpdateBy [LastUpdatedBy]
							,PON.STATUS [StatusId]
							,scv.SubCategoryName [Category]
							,PON.VendorId
						FROM InvoicePO IPO
						INNER JOIN PONonShopping PON ON IPO.PurchaeseOrderId = PON.Id
						INNER JOIN DeliveryNotes DELN ON PON.Id = DELN.PurchaseOrderId
						INNER JOIN Vendor VND ON IPO.VendorId = VND.Id
						INNER JOIN Flips.UserAccount UAC ON PON.RequestorName = UAC.Username
						INNER JOIN CostCenter COST ON UAC.CostCenterId = COST.Id
						INNER JOIN MasterTable MTL ON PON.STATUS = MTL.ValueId
						INNER JOIN PRFSummary PRFS ON PON.PRFSummaryId = PRFS.Id
						INNER JOIN SubCategory SCV ON SCV.Id = VND.SubCategoryId
						INNER JOIN DeliveryNotesPayment DNP ON DELN.Id = DNP.DeliveryNotesId
							AND DNP.PurchaseOrderTOPId = IPO.PurchaseOrderTOPId
							AND DNP.STATUS = 2
						INNER JOIN (
							SELECT AR.Id
								,(
									CASE 
										WHEN AR.RequestNo LIKE '%RI%'
											THEN 'Reimbursement'
										WHEN AR.RequestNo LIKE '%CA%'
											THEN 'Cash Advance'
										WHEN AR.RequestNo LIKE '%TR%'
											THEN 'Travel'
										WHEN AR.RequestNo LIKE '%SC%'
											THEN 'Shopping Cart'
										WHEN AR.RequestNo LIKE '%PROCSUM%'
											THEN 'Non Shopping Cart'
										END
									) AS RequestType
							FROM ApprovalRequest AR
							WHERE AR.RequestNo LIKE '%PROCSUM%'
							) APR ON PRFS.ApprovalRequestId = APR.Id
						LEFT JOIN (
							SELECT AR.Id
								,COUNT(*) TotalApproval
							FROM ApprovalRequestGroupMember ARGM
							INNER JOIN ApprovalRequest AR ON ARGM.ApprovalRequestId = AR.Id
							GROUP BY AR.Id
							) TA ON PRFS.ApprovalRequestId = TA.Id
						OUTER APPLY (
							SELECT COUNT(*) ApprovalDelivery
							FROM PONonShoppingTOP POT1
							INNER JOIN DeliveryNotesPayment dnp ON POT1.Id = DNP.PurchaseOrderTOPId
							INNER JOIN DeliveryNotesDetail DND ON DNP.Id = DND.DeliveryNotesPaymentId
							WHERE POT1.PONonShoppingId = PON.Id
								AND DNP.CategoryProcess_SubCategoryId = @NONSubCategoryId
							) TADN
						OUTER APPLY (
							SELECT COUNT(*) InvoicApproval
							FROM InvoicePO INV
							WHERE INV.PurchaeseOrderId = PON.Id
								AND INV.CategoryProcess_SubCategoryId = @NONSubCategoryId
							) TAIPO
						LEFT JOIN (
							SELECT AR1.Id
								,COUNT(*) TotalApproval
							FROM ApprovalRequestGroupMember ARGM
							INNER JOIN ApprovalRequest ar1 ON ARGM.ApprovalRequestId = ar1.Id
							WHERE ARGM.STATUS = 2 /*Approved*/
							GROUP BY AR1.Id
							) TA2 ON PRFS.ApprovalRequestId = TA2.Id
						OUTER APPLY (
							SELECT COUNT(*) TotalPoApprove
							FROM PONonShopping PON1
							WHERE PON1.STATUS IN (
									2
									,3
									)
								AND PON1.Id = PON.Id
							) TAPO2
						OUTER APPLY (
							SELECT COUNT(*) ApprovalDelivery
							FROM PONonShoppingTOP POT1
							INNER JOIN DeliveryNotesPayment DNP ON POT1.Id = DNP.PurchaseOrderTOPId
							INNER JOIN DeliveryNotesDetail DND ON DNP.Id = DND.DeliveryNotesPaymentId
							WHERE POT1.PONonShoppingId = PON.Id
								AND DND.STATUS = 2
								AND DNP.CategoryProcess_SubCategoryId = @NONSubCategoryId
							) TADN2
						OUTER APPLY (
							SELECT COUNT(*) TotalApprovalIpo
							FROM InvoicePO IPOA
							WHERE IPOA.PurchaeseOrderId = PON.Id
								AND ipoA.STATUS = 2
								AND IPOA.CategoryProcess_SubCategoryId = @NONSubCategoryId
							) TAIPO2
						LEFT JOIN (
							SELECT AR.Id
								,COUNT(*) TotalApprovalRejected
							FROM ApprovalRequestGroupMember ARGM
							INNER JOIN ApprovalRequest AR ON ARGM.ApprovalRequestId = ar.Id
							WHERE ARGM.STATUS = 3
							GROUP BY AR.Id
							) TA3 ON PRFS.ApprovalRequestId = TA3.Id
						OUTER APPLY (
							SELECT COUNT(*) TotalPoReject
							FROM PONonShopping PON1
							WHERE PON1.STATUS = 6
								AND PON1.Id = PON.Id
							) TAPO3
						OUTER APPLY (
							SELECT COUNT(*) RejectDelivery
							FROM PONonShoppingTOP POT1
							INNER JOIN DeliveryNotesPayment dnp ON POT1.Id = DNP.PurchaseOrderTOPId
							INNER JOIN DeliveryNotesDetail DND ON DNP.Id = DND.DeliveryNotesPaymentId
							WHERE POT1.PONonShoppingId = PON.Id
								AND DND.STATUS = 3
								AND DNP.CategoryProcess_SubCategoryId = @NONSubCategoryId
							) TADN3
						OUTER APPLY (
							SELECT COUNT(*) RejectInvoice
							FROM InvoicePO IPOA
							WHERE IPOA.PurchaeseOrderId = PON.Id
								AND ipoA.STATUS = 5
							) TAIPO3
						WHERE IPO.CategoryProcess_SubCategoryId = @NONSubCategoryId
							AND IPO.STATUS NOT IN (
								8
								,7
								)
							AND DELN.STATUS NOT IN (5)
							AND DELN.CategoryProcess_SubCategoryId = @NONSubCategoryId
							AND MTL.Category = 'PurchaseOrder.Status'
						) sub
					WHERE 1 = 1 AND {subQuery}
				) allSub
			";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public string GetApprovalRequestListPOVC(ParamGetRequestList param)
        {
            string subQuery = $@"(RequestNumber LIKE @RequestNumber) 
								  AND VendorId = @VendorId
								  AND Category = @Category
								  AND LastUpdatedBy = @MakerFinance
								  AND CONVERT(datetime, TransactionDate, 103) BETWEEN CONVERT(varchar(50), @RequestDateFrom, 103) AND CONVERT(varchar(50), @RequestDateTo, 103) ";
            if (String.IsNullOrEmpty(param.RequestNumber))
            {
                subQuery = subQuery.Replace("(RequestNumber LIKE @RequestNumber)", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.VendorId))
            {
                subQuery = subQuery.Replace("VendorId = @VendorId", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.Category))
            {
                subQuery = subQuery.Replace("Category = @Category", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.MakerFinance))
            {
                subQuery = subQuery.Replace("LastUpdatedBy = @MakerFinance", " 1 = 1");
            }
            if (String.IsNullOrEmpty(param.RequestDateFrom))
            {
                subQuery = subQuery.Replace("CONVERT(datetime, TransactionDate, 103) BETWEEN CONVERT(varchar(50), @RequestDateFrom, 103) AND CONVERT(varchar(50), @RequestDateTo, 103)", " 1=1 ");
            }
            if (!String.IsNullOrEmpty(param.Search?.Value))
            {
                subQuery = String.Concat(subQuery, " AND (RequestNumber LIKE @Search OR RequestorName LIKE @Search OR VendorName LIKE @Search) ");
            }

            string qry = $@"
				SELECT DISTINCT
					sub.RequestNumber,
					sub.RequestorName, 
					sub.RequestorCostCenter, 
					sub.Status, 
					sub.StatusId, 
					sub.VendorName, 
					sub.VendorId, 
					sub.GrandTotal, 
					sub.Description, 
					sub.TransactionDate, 
					sub.CreatedTime, 
					sub.RequestType, 
					sub.Category, 
					sub.TotalApproval, 
					sub.TotalApprovalApproved, 
					sub.TotalApprovalRejected, 
					sub.ReasonReject, 
					sub.LCurrency,
					sub.LastUpdatedTime,
					sub.LastUpdatedBy,
					sub.GenerateId,
					COUNT(*) OVER () AS CountData
				FROM (
					SELECT DISTINCT
						PO.PONumber AS RequestNumber,
						CONVERT(DATETIME, ipo.InvoiceDate, 103) AS TransactionDate,
						ipo.InvoiceDate AS CreatedTime,
						vn.Name AS VendorName,
						pr.RequestorUserName AS RequestorName,
						cc.Name AS RequestorCostCenter,
						(
							SELECT TOP 1
								CASE 
									WHEN ShortDescription = 'Process' THEN 'Approve'
								END
							FROM MasterTable
							WHERE Category = 'PurchaseOrder.Status'
							  AND ValueId = po.Status
						) AS Status,
						0 AS StatusId,
						0 AS TotalApproval,
						0 AS TotalApprovalApproved,
						0 AS TotalApprovalRejected,
						ipo.VendorId,
						'' AS ReasonReject,
						ipo.LastUpdateTime AS LastUpdatedTime,
						ipo.LastUpdateBy AS LastUpdatedBy,
						'Shopping Cart' AS RequestType,
						scv.SubCategoryName AS Category,
						ipo.TotalAmount AS GrandTotal,
						CONCAT(po.PONumber, ' - ', vn.Name) AS Description,
						ipo.Id AS GenerateId,
						ipo.Status AS InvoiceStatus,
						ipo.LCurrency
					FROM InvoicePO ipo
					JOIN PurchaseOrder po ON ipo.PurchaeseOrderId = po.Id
					JOIN PurchaseOrderToPurchaseRequest prtopo ON po.Id = prtopo.PurchaseOrderId
					JOIN PurchaseRequest pr ON prtopo.PurchaseRequestlId = pr.Id
					JOIN Flips.UserAccount ua ON pr.RequestorAccountId = ua.Id
					JOIN Vendor vn ON ipo.VendorId = vn.Id
					JOIN CostCenter cc ON ua.CostCenterId = cc.Id
					JOIN PurchaseOrderTOP pot ON po.Id = pot.PurchaseOrderId
					JOIN DeliveryNotes dn ON dn.PurchaseOrderId = po.Id
					JOIN SubCategory scv ON scv.Id = vn.SubCategoryId
					WHERE ipo.InvoiceNumber IS NOT NULL
					  AND ipo.CategoryProcess_SubCategoryId = (
						  SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01261'
					  )
					  AND ipo.Status = 7
					  AND NOT EXISTS (
						  SELECT TOP 1 VoucherRefId 
						  FROM VoucherDetail 
						  WHERE VoucherRefId = CONCAT(ipo.Id, ' - ', ipo.InvoiceNumber)
					  )
				) sub
				WHERE 1=1 AND {subQuery}

				UNION ALL

				SELECT DISTINCT
					SUB.RequestNumber,
					SUB.RequestorName,
					SUB.RequestorCostCenter,
					SUB.Status,
					SUB.StatusId,
					SUB.VendorName,
					SUB.VendorId,
					SUB.GrandTotal,
					SUB.Description,
					SUB.TransactionDate,
					SUB.CreatedTime,
					SUB.RequestType,
					SUB.Category,
					SUB.TotalApproval,
					SUB.TotalApprovalApproved,
					SUB.TotalApprovalRejected,
					SUB.ReasonReject,
					SUB.LCurrency,
					SUB.LastUpdatedTime,
					SUB.LastUpdatedBy,
					SUB.GenerateId,
					COUNT(*) OVER () AS CountData
				FROM (
					SELECT DISTINCT
						PO.PONumber [RequestNumber],
						CONVERT(DATETIME, INV.InvoiceDate, 103) AS TransactionDate,
						INV.InvoiceDate AS CreatedTime,
						VND.Name AS VendorName,
						PO.RequestorName AS RequestorName,
						COST.Name AS RequestorCostCenter,
						(
							SELECT TOP 1
								CASE
									WHEN ShortDescription = 'Process' THEN 'Approve'
								END
							FROM MasterTable
							WHERE Category = 'PurchaseOrder.Status'
							  AND ValueId = PO.Status
						) AS Status,
						0 AS StatusId,
						0 AS TotalApproval,
						0 AS TotalApprovalApproved,
						0 AS TotalApprovalRejected,
						INV.VendorId,
						'' AS ReasonReject,
						INV.LastUpdateTime AS LastUpdatedTime,
						INV.LastUpdateBy AS LastUpdatedBy,
						'Non Shopping Cart' AS RequestType,
						SCV.SubCategoryName AS Category,
						INV.TotalAmount AS GrandTotal,
						CONCAT(PO.PONumber, ' - ', VND.Name) AS Description,
						INV.Id AS GenerateId,
						INV.Status AS InvoiceStatus,
						INV.LCurrency
					FROM InvoicePO INV
					JOIN PONonShopping PO ON INV.PurchaeseOrderId = PO.Id
					JOIN Vendor VND ON INV.VendorId = VND.Id
					JOIN Flips.UserAccount UA ON PO.RequestorName = UA.Username
					JOIN CostCenter COST ON UA.CostCenterId = COST.Id
					JOIN BusinessUnit BU ON COST.BusinessUnitId = BU.Id
					JOIN SubCategory SCV ON VND.SubCategoryId = SCV.Id
					WHERE INV.InvoiceNumber IS NOT NULL
					  AND INV.Status = 7
					  AND INV.CategoryProcess_SubCategoryId = (
						  SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01262'
					  )
					  AND NOT EXISTS (
						  SELECT TOP 1 VoucherRefId
						  FROM VoucherDetail
						  WHERE VoucherRefId = CONCAT(INV.Id, ' - ', INV.InvoiceNumber)
					  )
				) SUB
				WHERE 1=1 AND {subQuery}
			";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public string GetAttachmentShop()
        {
            string query = $@"DECLARE @SubCategory INT = (
                               SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01261'
                           )
							DECLARE @PurchaseOrderId INT = (
															   SELECT Id FROM PurchaseOrder WHERE PONumber = @RequestNumber
														   )
							DECLARE @PurchaseRequestId INT = (
																 SELECT PRTOPO.PurchaseRequestlId
																 FROM PurchaseOrder PO
																	 JOIN PurchaseOrderToPurchaseRequest PRTOPO
																		 ON PO.Id = PRTOPO.PurchaseOrderId
																 WHERE PO.PONumber = @RequestNumber
															 )

							CREATE TABLE #Header (PurchaseOrderDetailId INT)
							INSERT INTO #Header
							(
								PurchaseOrderDetailId
							)
							SELECT POD.Id
							FROM PurchaseOrder PO
								JOIN PurchaseOrderDetail POD
									ON PO.Id = POD.PurchaseOrderId
							WHERE PO.PONumber = @RequestNumber

							SELECT Id
							FROM
							(
								SELECT ATH.Id
								FROM PurchaseRequestItemDetail PRD
									JOIN Attachment ATH
										ON PRD.AttachmentId = ATH.Id
								WHERE PRD.PurchaseRequestId IN(@PurchaseRequestId)
									  AND ATH.Category = 'PR'
							) ATCH
							UNION ALL
							SELECT Id
							FROM
							(
								SELECT ATH.Id
								FROM Attachment ATH
								WHERE ATH.RefId = @PurchaseRequestId
									  AND ATH.Category = 'PurchaseRequest'
							) ATCH
							UNION ALL
							SELECT Id
							FROM
							(
								SELECT ATH.Id
								FROM PurchaseOrder PO
									JOIN Attachment ATH
										ON PO.AttachmentId = ATH.Id
								WHERE PO.Id IN ( @PurchaseOrderId )
									  AND ATH.Category = 'PO'
							) ATCH
							UNION ALL
							SELECT Id
							FROM
							(
								SELECT ATH.Id
								FROM DeliveryNotesDetail DND
									JOIN Attachment ATH
										ON DND.Id = ATH.RefId
								WHERE DND.PurchaseOrderDetailId IN (
																	   SELECT PurchaseOrderDetailId FROM #Header
																   )
									  AND ATH.Category = 'DN'
							) ATCH
							UNION ALL
							SELECT Id
							FROM
							(
								SELECT ATH.Id
								FROM PurchaseOrder PO
									 JOIN Attachment ATH
										ON PO.Id = ATH.RefId
								WHERE PO.Id = @PurchaseOrderId
									  AND ATH.Category = 'INV'
									  AND ATH.Description = 'ShoppingCart'
							) ATCH
							UNION ALL
							SELECT Id
							FROM
							(
								SELECT ATH.Id
								FROM InvoicePO INV
									JOIN Attachment ATH
										ON ATH.RefId = INV.Id
								WHERE INV.PurchaeseOrderId = @PurchaseOrderId
									  AND ATH.Category = 'INV'
									  AND INV.CategoryProcess_SubCategoryId = @SubCategory
							) ATCH
							DROP TABLE #Header";
            return query;
        }
        public string GetAttachmentNonShop()
        {
            string query = $@"DECLARE @SubCategory INT = (
                               SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01262'
                           )

							CREATE TABLE #HEADER
							(
								PurchaseOrderId INT,
								PRFId INT,
								DeliveryNotesId INT
							)
							INSERT INTO #HEADER
							(
								PurchaseOrderId,
								PRFId,
								DeliveryNotesId
							)
							SELECT PO.Id,
								   PFS.PRFId,
								   DND.Id
							FROM PONonShopping PO
								JOIN PRFSummary PFS
									ON PO.PRFSummaryId = PFS.Id
								JOIN PONonShoppingTOP POT
									ON PO.Id = POT.PONonShoppingId
								JOIN DeliveryNotesPayment DNP
									ON POT.Id = DNP.PurchaseOrderTOPId
								JOIN DeliveryNotesDetail DND
									ON DNP.Id = DND.DeliveryNotesPaymentId
							WHERE PO.PONumber = @RequestNumber
								  AND DNP.CategoryProcess_SubCategoryId = @SubCategory

							SELECT Id
							FROM
							(
								SELECT ATH.Id
								FROM Attachment ATH
								WHERE RefId IN (
												   SELECT PRFId FROM #HEADER
											   )
									  AND ATH.Category IN ( 'PurchaseRequestForm', 'QuotationFormVendor', 'ProcurementSummary' )
							) ATCH
							UNION ALL
							SELECT Id
							FROM
							(
								SELECT ATH.Id
								FROM Attachment ATH
								WHERE ATH.RefId IN (
													   SELECT PurchaseOrderId FROM #HEADER
												   )
									  AND ATH.Category IN ( 'PO' )
									  AND ATH.Description = 'Non-ShoppingCart'
							) ATCH
							UNION ALL
							SELECT Id
							FROM
							(
								SELECT ATH.Id
								FROM Attachment ATH
								WHERE ATH.RefId IN (
													   SELECT DeliveryNotesId FROM #HEADER
												   )
									  AND ATH.Category IN ( 'DN' )
							) ATCH
							UNION ALL
							SELECT Id
							FROM
							(
								SELECT ATH.Id
								FROM Attachment ATH
								WHERE ATH.RefId IN (
													   SELECT PurchaseOrderId FROM #HEADER
												   )
									  AND ATH.Category IN ( 'INV' )
									  AND ATH.Description LIKE 'Non%Shopping%Cart'
							) ATCH
							DROP TABLE #HEADER";
            return query;
        }
        public string GetAttachmentNonShopVoucher()
        {
            string query = $@"
				DECLARE @InvocePOId int, @PurchaeseOrderId INT, @PRFId INT;

				SELECT 
					@InvocePOId = IPO.Id,
					@PurchaeseOrderId = IPO.PurchaeseOrderId,
					@PRFId = PS.PRFId
				FROM InvoicePO IPO
					JOIN PONonShopping PO ON PO.Id = IPO.PurchaeseOrderId
					JOIN PRFSummary PS ON PS.Id = PO.PRFSummaryId
				WHERE CAST(IPO.Id as varchar(100)) = SUBSTRING(@RequestNumber,0,CHARINDEX(' - ',@RequestNumber));

				SELECT Id
				FROM (
					SELECT ATH.Id
					FROM Attachment ATH
					WHERE RefId IN (@PRFId)
						AND ATH.Category IN ( 'PurchaseRequestForm', 'QuotationFormVendor', 'ProcurementSummary' )

					UNION ALL
	
					SELECT ATH.Id
					FROM Attachment ATH
					WHERE ATH.RefId IN (@PurchaeseOrderId)
							AND ATH.Category IN ( 'PO' )
							AND ATH.Description = 'Non-ShoppingCart'

					UNION ALL

					SELECT ATH.Id
					FROM Attachment ATH
					WHERE ATH.RefId IN (
											SELECT DND.Id
											FROM PONonShoppingTOP POT
												JOIN DeliveryNotesPayment DNP
													ON POT.Id = DNP.PurchaseOrderTOPId
												JOIN DeliveryNotesDetail DND
													ON DNP.Id = DND.DeliveryNotesPaymentId
											WHERE POT.PONonShoppingId = @PurchaeseOrderId
										)
							AND ATH.Category IN ( 'DN' )

					UNION ALL
	
					SELECT ATH.Id
					FROM Attachment ATH
						JOIN InvoicePO as ipo ON ipo.PurchaeseOrderId = ATH.RefId
					WHERE ATH.RefId IN (@PurchaeseOrderId)
						AND ATH.Category IN ( 'INV' )
						AND ATH.Description = 'Non Shopping Cart'
						AND CAST(ATH.CreatedTime AS DATE) between CAST(ipo.CreateTime AS DATE) and CAST(ipo.LastUpdateTime AS DATE) 
				) datax
			";
            return query;
        }
        #endregion

        #region Fin Voucher
        public static string GetVoucherList(ParamGetVoucherList param)
        {
            string subQuery = $@"CheckerMCM = @CheckerMCM 
								AND VoucherNumber = @VoucherNumber
								AND Category = @VoucherCategory
								AND VoucherRefId LIKE @ExpenseType
								AND vh.Status = @Status
								AND CONVERT(datetime, vh.CreatedTime, 103) BETWEEN  CONVERT(varchar(50), @StartDate, 103) AND CONVERT(varchar(50), @EndDate, 103)";
            if (String.IsNullOrEmpty(param.CheckerMCM))
            {
                subQuery = subQuery.Replace("CheckerMCM = @CheckerMCM ", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.VoucherNumber))
            {
                subQuery = subQuery.Replace("VoucherNumber = @VoucherNumber", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.VoucherCategory))
            {
                subQuery = subQuery.Replace("Category = @VoucherCategory", " 1=1 ");
            }
            else if (param.VoucherCategory.Equals("PO", StringComparison.CurrentCultureIgnoreCase))
            {
                subQuery = subQuery.Replace("Category = @VoucherCategory", "Category IN ('Shopping Cart', 'Non Shopping Cart')");
            }

            //ExpenseType to filtering GER
            if (String.IsNullOrEmpty(param.ExpenseType))
            {
                subQuery = subQuery.Replace("VoucherRefId LIKE @ExpenseType", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.Status))
            {
                subQuery = subQuery.Replace("vh.Status = @Status", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.StartDate))
            {
                subQuery = subQuery.Replace("CONVERT(datetime, vh.CreatedTime, 103) BETWEEN  CONVERT(varchar(50), @StartDate, 103) AND CONVERT(varchar(50), @EndDate, 103)", " 1=1 ");
            }
            if (!String.IsNullOrEmpty(param.Search?.Value))
            {
                subQuery = String.Concat(subQuery, " AND (VoucherNumber LIKE @Search OR vh.CreatedBy LIKE @Search  OR L_Currency LIKE @Search) ");
            }

            string qry = $@"SELECT
							      vh.Id,
							      mt.ShortDescription AS StatusString,
							      vh.VoucherNumber,
							      vh.CreatedBy,
							      CONVERT(DATETIME, vh.CreatedTime, 103) AS TransactionDateString,
							      vh.L_Currency,
							      vh.Category,
							      vh.Status,
							      vh.BankTransferName,
							      vh.CreatedTime,
							      vh.CheckerMCM,
							      vd_summary.GrandTotal,
							      COUNT(*) OVER () AS CountData
							  FROM VoucherHeader vh
							  JOIN VoucherDetail vd ON vh.Id = vd.VoucherId
							  LEFT JOIN (
							      SELECT ValueId, ShortDescription
							      FROM MasterTable
							      WHERE Category = 'Voucher.Status'
							  ) mt ON mt.ValueId = vh.Status
							  LEFT JOIN (
							      SELECT VoucherId, SUM(TotalBaseAmmount) AS GrandTotal
							      FROM VoucherDetail
							      GROUP BY VoucherId
							  ) vd_summary ON vd_summary.VoucherId = vh.Id
							  WHERE 1=1 AND {subQuery}
							  GROUP BY
							      vh.Id, mt.ShortDescription, vh.VoucherNumber, vh.CreatedBy,
							      vh.CreatedTime, vh.L_Currency, vh.Category, vh.Status,
							      vh.BankTransferName, vh.CheckerMCM, vd_summary.GrandTotal
							  ";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY vh.CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "vh.CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            return qry;
        }
        public static string GetVoucherDetailReimbursementOrCashAdvance()
        {
            string qry = $@" SELECT sub.*, COUNT(*) OVER () as CountData  FROM (
                                          SELECT  vd.* 
                                                 ,vd.CreatedTime as [TransactionDateString]
                                                 ,vh.VoucherNumber
                                                 ,vh.Category
                                                 ,mt.Description as [StatusString]
                                                 ,CASE vd.StatusTransfer
                                                       WHEN '0' THEN 'Failed'
                                                       WHEN '1' THEN 'Success'
                                                       ELSE ''
                                                  END as [StatusTransferString]
												 ,(SELECT DISTINCT BankName
															 	     FROM ReimbursementDetail rdsub
															 	     WHERE rdsub.ReimbursementId = r.Id) [BankName]
												 ,(SELECT DISTINCT BankAccountNumber
															 	     FROM ReimbursementDetail rdsub
															 	     WHERE rdsub.ReimbursementId = r.Id) [BankAccountNumber]
												 ,(SELECT DISTINCT BankAccountOwnerName
															 	     FROM ReimbursementDetail rdsub
															 	     WHERE rdsub.ReimbursementId = r.Id) [BankAccountOwnerName]
												 ,vd.VoucherRefId [HyperlinkString]
												 ,r.Id [RequestId]
                                          FROM   VoucherHeader vh
                                          JOIN   VoucherDetail vd on vh.Id = vd.VoucherId
									      JOIN   Reimbursement r on r.RequestNumber = vd.VoucherRefId
                                          JOIN   MasterTable mt on mt.ValueId = vh.Status
                                          WHERE  mt.Category = 'Voucher.Status' AND (vd.VoucherId = @Id OR vh.VoucherNumber = @VoucherNumber)
                                          ) sub
                                          ";
            return qry;
        }
        public static string GetVoucherDetailInvoiceTravel()
        {
            string qry = $@" SELECT sub.*, COUNT(*) OVER () as CountData  FROM (
                                          SELECT  vd.* 
                                                 ,vd.CreatedTime as [TransactionDateString]
                                                 ,vh.VoucherNumber
                                                 ,vh.Category
                                                 ,mt.Description as [StatusString]
                                                 ,CASE vd.StatusTransfer
                                                       WHEN '0' THEN 'Failed'
                                                       WHEN '1' THEN 'Success'
                                                       ELSE ''
                                                  END as [StatusTransferString]
												 ,(SELECT DISTINCT BankName
															 	     FROM InvoiceTravelDetail rdsub
															 	     WHERE rdsub.InvoiceTravelId = r.Id) [BankName]
												 ,(SELECT DISTINCT BankAccountNumber
															 	     FROM InvoiceTravelDetail rdsub
															 	     WHERE rdsub.InvoiceTravelId = r.Id) [BankAccountNumber]
												 ,(SELECT DISTINCT BankAccountOwnerName
															 	     FROM InvoiceTravelDetail rdsub
															 	     WHERE rdsub.InvoiceTravelId = r.Id) [BankAccountOwnerName]
												 ,vd.VoucherRefId [HyperlinkString]
												 ,r.Id [RequestId]
                                          FROM   VoucherHeader vh
                                          JOIN   VoucherDetail vd on vh.Id = vd.VoucherId
									      JOIN   InvoiceTravel r on r.RequestNumber = vd.VoucherRefId
                                          JOIN   MasterTable mt on mt.ValueId = vh.Status
                                          WHERE  mt.Category = 'Voucher.Status' AND (vd.VoucherId = @Id OR vh.VoucherNumber = @VoucherNumber)
                                          ) sub
                                          ";
            return qry;
        }
        public static string GetVoucherDetailGer()
        {
            string qry = $@" SELECT sub.*, COUNT(*) OVER () as CountData  FROM (
                                          SELECT  vd.* 
										         ,vd.CreatedTime as [TransactionDateString]
										         ,vh.VoucherNumber
										         ,vh.Category
										         ,mt.Description as [StatusString]
										         ,CASE vd.StatusTransfer
										               WHEN '0' THEN 'Failed'
										               WHEN '1' THEN 'Success'
										               ELSE ''
										          END as [StatusTransferString]
										  		 ,gerjoin.BankName [BankName]
										  		 ,gerjoin.AccountNumber [BankAccountNumber]
										  		 ,gerjoin.BankAccountOwnerName [BankAccountOwnerName]
										  		 ,vd.VoucherRefId [HyperlinkString]
												 ,gerjoin.Id [RequestId]
										  FROM   VoucherHeader vh
										  JOIN   VoucherDetail vd on vh.Id = vd.VoucherId
										  JOIN   ( SELECT gh.RequestNumber
										  			   ,gh.RequestorUsername
										  			   ,gd.Id
										  			   ,gd.BankAccountOwnerName
										  			   ,gd.BankName
										  			   ,gd.AccountNumber
										  		 FROM GerHeader gh
										  	     JOIN GerDetail gd on gh.Id = gd.GerHeaderId
										         ) as gerjoin on CONCAT(gerjoin.RequestNumber, ' - ', gerjoin.Id) = vd.VoucherRefId
										  JOIN   MasterTable mt on mt.ValueId = vh.Status
										  WHERE  mt.Category = 'Voucher.Status' AND (vd.VoucherId = @Id OR vh.VoucherNumber = @VoucherNumber)
                                          ) sub
                                          ";
            return qry;
        }
        public static string GetVoucherDetailTravelSettlement()
        {
            string qry = $@" SELECT sub.*, COUNT(*) OVER () as CountData  FROM (
                                          SELECT  vd.* 
                                                 ,vd.CreatedTime as [TransactionDateString]
                                                 ,vh.VoucherNumber
                                                 ,vh.Category
                                                 ,mt.Description as [StatusString]
                                                 ,CASE vd.StatusTransfer
                                                       WHEN '0' THEN 'Failed'
                                                       WHEN '1' THEN 'Success'
                                                       ELSE ''
                                                  END as [StatusTransferString]
												 ,tre.[BankName]
												 ,tre.[BankAccountNumber]
												 ,tre.[BankAccountOwnerName]
												 ,ar.RequestNo [HyperlinkString]
												 ,tre.Id [RequestId]
                                          FROM   VoucherHeader vh
                                          JOIN   VoucherDetail vd on vh.Id = vd.VoucherId
									      JOIN   TravelRequestExpense tre on tre.RequestNumber = vd.VoucherRefId
										  JOIN   TravelRequest tr on tr.Id =  tre.TravelRequestId
										  JOIN   ApprovalRequest ar on ar.Id =  tr.ApprovalRequestId
                                          JOIN   MasterTable mt on mt.ValueId = vh.Status
                                          WHERE  mt.Category = 'Voucher.Status' AND (vd.VoucherId = @Id OR vh.VoucherNumber = @VoucherNumber)
                                          ) sub
                                          ";
            return qry;
        }
        public static string GetVoucherDetailTrexApr()
        {
            string qry = $@" SELECT sub.*, COUNT(*) OVER () as CountData  FROM (
                                          SELECT  vd.* 
                                                 ,vd.CreatedTime as [TransactionDateString]
                                                 ,vh.VoucherNumber
                                                 ,vh.Category
                                                 ,mt.Description as [StatusString]
                                                 ,CASE vd.StatusTransfer
                                                       WHEN '0' THEN 'Failed'
                                                       WHEN '1' THEN 'Success'
                                                       ELSE ''
                                                  END as [StatusTransferString]
												 ,r.BankName
												 ,r.BankAccountNumber
												 ,r.BankAccountName [BankAccountOwnerName]
												 ,vd.VoucherRefId [HyperlinkString]
												 ,r.APRId [RequestId]
                                          FROM   VoucherHeader vh
                                          JOIN   VoucherDetail vd on vh.Id = vd.VoucherId
									      JOIN   TrexApr r on r.NoApr = vd.VoucherRefId
                                          JOIN   MasterTable mt on mt.ValueId = vh.Status
                                          WHERE  mt.Category = 'Voucher.Status' AND (vd.VoucherId = @Id OR vh.VoucherNumber = @VoucherNumber)
                                          ) sub
                                          ";
            return qry;
        }
        public static string GetVoucherDetailTrexGer()
        {
            string qry = $@" SELECT sub.*, COUNT(*) OVER () as CountData  FROM (
                                          SELECT  vd.* 
                                                 ,vd.CreatedTime as [TransactionDateString]
                                                 ,vh.VoucherNumber
                                                 ,vh.Category
                                                 ,mt.Description as [StatusString]
                                                 ,CASE vd.StatusTransfer
                                                       WHEN '0' THEN 'Failed'
                                                       WHEN '1' THEN 'Success'
                                                       ELSE ''
                                                  END as [StatusTransferString]
												 ,r.BankName
												 ,r.BankAccountNumber
												 ,r.BankAccountName [BankAccountOwnerName]
												 ,vd.VoucherRefId [HyperlinkString]
												 ,r.GERId [RequestId]
                                          FROM   VoucherHeader vh
                                          JOIN   VoucherDetail vd on vh.Id = vd.VoucherId
									      JOIN   TrexGerHeader r on r.NoGer = vd.VoucherRefId
                                          JOIN   MasterTable mt on mt.ValueId = vh.Status
                                          WHERE  mt.Category = 'Voucher.Status' AND (vd.VoucherId = @Id OR vh.VoucherNumber = @VoucherNumber)
                                          ) sub
                                          ";
            return qry;
        }
        public static string GetVoucherDetailTrexEer()
        {
            string qry = $@" SELECT sub.*, COUNT(*) OVER () as CountData  FROM (
                                          SELECT  vd.* 
                                                 ,vd.CreatedTime as [TransactionDateString]
                                                 ,vh.VoucherNumber
                                                 ,vh.Category
                                                 ,mt.Description as [StatusString]
                                                 ,CASE vd.StatusTransfer
                                                       WHEN '0' THEN 'Failed'
                                                       WHEN '1' THEN 'Success'
                                                       ELSE ''
                                                  END as [StatusTransferString]
												 ,r.BankName
												 ,r.BankAccountNumber
												 ,r.BankAccountName [BankAccountOwnerName]
												 ,vd.VoucherRefId [HyperlinkString]
												 ,r.EERId [RequestId]
                                          FROM   VoucherHeader vh
                                          JOIN   VoucherDetail vd on vh.Id = vd.VoucherId
									      JOIN   TrexEerHeader r on r.NoEer = vd.VoucherRefId
                                          JOIN   MasterTable mt on mt.ValueId = vh.Status
                                          WHERE  mt.Category = 'Voucher.Status' AND (vd.VoucherId = @Id OR vh.VoucherNumber = @VoucherNumber)
                                          ) sub
                                          ";
            return qry;
        }
        public static string GetVoucherDetailTrexTer()
        {
            string qry = $@" SELECT sub.*, COUNT(*) OVER () as CountData  FROM (
                                          SELECT  vd.* 
                                                 ,vd.CreatedTime as [TransactionDateString]
                                                 ,vh.VoucherNumber
                                                 ,vh.Category
                                                 ,mt.Description as [StatusString]
                                                 ,CASE vd.StatusTransfer
                                                       WHEN '0' THEN 'Failed'
                                                       WHEN '1' THEN 'Success'
                                                       ELSE ''
                                                  END as [StatusTransferString]
												 ,r.BankName
												 ,r.BankAccountNumber
												 ,r.BankAccountName [BankAccountOwnerName]
												 ,vd.VoucherRefId [HyperlinkString]
												 ,r.TERId [RequestId]
                                          FROM   VoucherHeader vh
                                          JOIN   VoucherDetail vd on vh.Id = vd.VoucherId
									      JOIN   TrexTerHeader r on r.NoTer = vd.VoucherRefId
                                          JOIN   MasterTable mt on mt.ValueId = vh.Status
                                          WHERE  mt.Category = 'Voucher.Status' AND (vd.VoucherId = @Id OR vh.VoucherNumber = @VoucherNumber)
                                          ) sub
                                          ";
            return qry;
        }
        public static string GetUpdatePaymentListRI(ParamGetUpdatePaymentList param)
        {
            string subQuery = UpdatePaymentSubQuery(param);

            string qry = $@"    SELECT 
									sub.*, 
									COUNT(*) OVER () AS CountData  
								FROM (
										SELECT 
										rd.BankAccountNumber,
										rd.BankAccountOwnerName,
										vd.Id AS VoucherDetailId,
										vh.VoucherNumber,
										vd.VoucherRefId,
										rd.InvoiceNo,
										rd.BankName,
										vh.L_Currency,
										(SELECT SUM(rdc.Amount) 
										 FROM ReimbursementDetailCostCenter rdc 
										 WHERE rdc.ReimbursementDetailId = rd.Id) AS AmountAfterTaxes,
										(SELECT SUM(rdo.Amount) 
										 FROM ReimbursementDetailOtherCost rdo 
										 WHERE rdo.ReimbursementDetailId = rd.Id) AS Tax,
										'0' AS Charges,
										rd.Amount AS AmountBeforeTaxes,
										vh.TransferNumber,
										CASE vd.StatusTransfer
											WHEN '1' THEN 'Success'
											WHEN '0' THEN 'Fail'
											ELSE ''
										END AS StatusTransferString,
										vh.TransferTime,
										vh.TransferTime AS TransferTimeString,
										vh.TransferType,
										vd.TransferNote, vd.StatusTransfer,
										vd.CreatedTime,
										vh.Status,
										vh.Category,
										vh.LastUpdatedTime,
										vh.LastUpdatedBy
									FROM VoucherDetail vd
									JOIN VoucherHeader vh ON vh.Id = vd.VoucherId
									JOIN Reimbursement r ON r.RequestNumber = vd.VoucherRefId
									JOIN ReimbursementDetail rd ON rd.ReimbursementId = r.Id
                                    ) sub WHERE {subQuery}     ";

            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            return qry;
        }
        public static string GetUpdatePaymentListTRSTL(ParamGetUpdatePaymentList param)
        {
            string subQuery = UpdatePaymentSubQuery(param);

            string qry = $@" SELECT 
									sub.*, 
									COUNT(*) OVER () AS CountData  
							 FROM (
										SELECT 
										tre.BankAccountNumber,
										tre.BankAccountOwnerName,
										vd.Id AS VoucherDetailId,
										vh.VoucherNumber,
										vd.VoucherRefId,
										'' AS InvoiceNo,
										tre.BankName,
										vh.L_Currency,
										tre.GrandTotal AS AmountAfterTaxes,
										'0' AS Tax,
										'0' AS Charges,
										tre.GrandTotal AS AmountBeforeTaxes,
										vh.TransferNumber,
										CASE vd.StatusTransfer
											WHEN '1' THEN 'Success'
											WHEN '0' THEN 'Fail'
											ELSE ''
										END AS StatusTransferString,
										vh.TransferTime,
										vh.TransferTime AS TransferTimeString,
										vh.TransferType,
										vd.TransferNote, vd.StatusTransfer,
										vd.CreatedTime,
										vh.Status,
										vh.Category,
										vh.LastUpdatedTime,
										vh.LastUpdatedBy
									FROM VoucherDetail vd
									JOIN VoucherHeader vh ON vh.Id = vd.VoucherId
									JOIN TravelRequestExpense tre ON tre.RequestNumber = vd.VoucherRefId
                                    ) sub WHERE {subQuery}";


            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            return qry;
        }
        public static string GetUpdatePaymentListINVTR(ParamGetUpdatePaymentList param)
        {
            string subQuery = UpdatePaymentSubQuery(param);

            string qry = $@" SELECT 
									sub.*, 
									COUNT(*) OVER () AS CountData  
							 FROM (
									SELECT 
										r.BankAccountNumber,
										r.BankAccountOwnerName,
										vd.Id AS VoucherDetailId,
										vh.VoucherNumber,
										vd.VoucherRefId,
										'' AS InvoiceNo,
										r.BankName,
										vh.L_Currency,
										r.Amount AS AmountAfterTaxes,
										'0' AS Tax,
										'0' AS Charges,
										r.Amount AS AmountBeforeTaxes,
										vh.TransferNumber,
										CASE vd.StatusTransfer
											WHEN '1' THEN 'Success'
											WHEN '0' THEN 'Fail'
											ELSE ''
										END AS StatusTransferString,
										vh.TransferTime,
										vh.TransferTime AS TransferTimeString,
										vh.TransferType,
										vd.TransferNote, vd.StatusTransfer,
										vd.CreatedTime,
										vh.Status,
										vh.Category,
										vh.LastUpdatedTime,
										vh.LastUpdatedBy
									FROM VoucherDetail vd
									JOIN VoucherHeader vh ON vh.Id = vd.VoucherId
									JOIN InvoiceTravel r ON r.RequestNumber = vd.VoucherRefId
									JOIN InvoiceTravelDetail rd ON rd.InvoiceTravelId = r.Id
                               ) sub WHERE {subQuery}     ";

            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            return qry;
        }
        public static string GetUpdatePaymentListGER(ParamGetUpdatePaymentList param)
        {
            string subQuery = UpdatePaymentSubQuery(param);
            subQuery = subQuery.Replace("VoucherDetailId", "vp.Id");
            subQuery = subQuery.Replace("BankAccountNumber= @BankAccountNumber", "gd.AccountNumber= @BankAccountNumber");
            subQuery = subQuery.Replace("Status= @Status", "vp.VoucherStatus= @Status");

            string qry = $@" WITH VoucherParsed AS (
							     SELECT 
							         vd.Id,
							 		vd.VoucherRefId,
							 		vd.StatusTransfer,
							 		vd.TransferNote,
							 		vd.CreatedTime,
							        vh.VoucherNumber,
							        vh.L_Currency,
							        vh.TransferNumber,
							        vh.TransferTime,
							        vh.TransferType,
							        vh.Status AS VoucherStatus,
							        vh.Category,
							        vh.LastUpdatedTime,
							        vh.LastUpdatedBy,
							        SUBSTRING(vd.VoucherRefId, 1, CHARINDEX(' - ', vd.VoucherRefId) - 1) AS ParsedRequestNumber,
							        CAST(SUBSTRING(vd.VoucherRefId, CHARINDEX(' - ', vd.VoucherRefId) + 3, LEN(vd.VoucherRefId)) AS INT) AS ParsedDetailId
							     FROM VoucherDetail vd
							     INNER JOIN VoucherHeader vh ON vh.Id = vd.VoucherId
							 )
							 
							 SELECT 
							     gd.AccountNumber AS BankAccountNumber,
							     gd.BankAccountOwnerName,
							     vp.Id AS VoucherDetailId,
							     vp.VoucherNumber,
							     vp.VoucherRefId,
							     '' AS InvoiceNo,
							     gd.BankName,
							     vp.L_Currency,
							     gd.NettAmount AS AmountAfterTaxes,
							     '0' AS Tax,
							     '0' AS Charges,
							     gd.Amount AS AmountBeforeTaxes,
							     vp.TransferNumber,
							     CASE vp.StatusTransfer
							         WHEN '1' THEN 'Success'
							         WHEN '0' THEN 'Fail'
							         ELSE ''
							     END AS StatusTransferString,
							     vp.TransferTime,
							     vp.TransferTime AS TransferTimeString,
							     vp.TransferType,
							     vp.TransferNote,
							     vp.StatusTransfer,
							     vp.CreatedTime,
							     vp.VoucherStatus AS Status,
							     vp.Category,
							     vp.LastUpdatedTime,
							     vp.LastUpdatedBy,
							 	 rc.TotalCount AS CountData
							 FROM VoucherParsed vp
							 INNER JOIN GerDetail gd ON gd.Id = vp.ParsedDetailId
							 INNER JOIN GerHeader gh ON gh.Id = gd.GerHeaderId AND gh.RequestNumber = vp.ParsedRequestNumber
							 CROSS APPLY (
							     SELECT COUNT(*) AS TotalCount
							     FROM VoucherParsed
							 ) rc
							 WHERE {subQuery}";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            return qry;
        }
        public static string GetUpdatePaymentListSC(ParamGetUpdatePaymentList param)
        {
            string subQuery = UpdatePaymentSubQuery(param);

            string qry = $@" SELECT 
									sub.*, 
									COUNT(*) OVER () AS CountData  
								FROM (
									    SELECT 
										ipo.BankAccountNumber,
										ipo.BankAccountOwnerName,
										vd.Id AS VoucherDetailId,
										vh.VoucherNumber,
										PO.PONumber AS VoucherRefId,
										ipo.InvoiceNumber,
										ipo.BankName,
										vh.L_Currency,
										vd.TotalBaseAmmount AS AmountAfterTaxes,
										(SELECT SUM(ipoc.Amount) 
										 FROM InvoicePOItemOtherCost ipoc 
										 WHERE ipoc.InvoicePOId = ipo.Id) AS Tax,
										'0' AS Charges,
										vd.TotalOriginalAmmount AS AmountBeforeTaxes,
										vh.TransferNumber,
										CASE vd.StatusTransfer
											WHEN '1' THEN 'Success'
											WHEN '0' THEN 'Fail'
											ELSE ''
										END AS StatusTransferString,
										vh.TransferTime,
										vh.TransferTime AS TransferTimeString,
										vh.TransferType,
										vd.TransferNote, vd.StatusTransfer,
										vd.CreatedTime,
										vh.Status,
										vh.Category,
										vh.LastUpdatedTime,
										vh.LastUpdatedBy
									FROM VoucherDetail vd
									JOIN VoucherHeader vh ON vh.Id = vd.VoucherId
									JOIN InvoicePO ipo ON CONCAT(ipo.Id, ' - ', ipo.InvoiceNumber) = vd.VoucherRefId
									JOIN PurchaseOrder PO ON PO.Id = IPO.PurchaeseOrderId
									JOIN SubCategory SC ON SC.Id = IPO.CategoryProcess_SubCategoryId
										AND SC.SubCategoryCode = 'SC-2024-02-01261'
                                       ) sub WHERE {subQuery}  ";

            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }


            return qry;
        }
        public static string GetUpdatePaymentListNON(ParamGetUpdatePaymentList param)
        {
            string subQuery = UpdatePaymentSubQuery(param);

            string qry = $@" SELECT 
									sub.*, 
									COUNT(*) OVER () AS CountData  
								FROM (  SELECT 
										ipo.BankAccountNumber,
										ipo.BankAccountOwnerName,
										vd.Id AS VoucherDetailId,
										vh.VoucherNumber,
										PONS.PONumber AS VoucherRefId,
										ipo.InvoiceNumber,
										ipo.BankName,
										vh.L_Currency,
										vd.TotalBaseAmmount AS AmountAfterTaxes,
										(SELECT SUM(ipoc.Amount) 
										 FROM InvoicePOItemOtherCost ipoc 
										 WHERE ipoc.InvoicePOId = ipo.Id) AS Tax,
										'0' AS Charges,
										vd.TotalOriginalAmmount AS AmountBeforeTaxes,
										vh.TransferNumber,
										CASE vd.StatusTransfer
											WHEN '1' THEN 'Success'
											WHEN '0' THEN 'Fail'
											ELSE ''
										END AS StatusTransferString,
										vh.TransferTime,
										vh.TransferTime AS TransferTimeString,
										vh.TransferType,
										vd.TransferNote, vd.StatusTransfer,
										vd.CreatedTime,
										vh.Status,
										vh.Category,
										vh.LastUpdatedTime,
										vh.LastUpdatedBy
									FROM VoucherDetail vd
									JOIN VoucherHeader vh ON vh.Id = vd.VoucherId
									JOIN InvoicePO ipo ON CONCAT(ipo.Id, ' - ', ipo.InvoiceNumber) = vd.VoucherRefId
									JOIN PONonShopping PONS ON PONS.Id = IPO.PurchaeseOrderId
									JOIN SubCategory SC ON SC.Id = IPO.CategoryProcess_SubCategoryId
										AND SC.SubCategoryCode = 'SC-2024-02-01262'
                                          ) sub WHERE {subQuery}";

            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            return qry;
        }
        public static string GetUpdatePaymentListTrexAPR(ParamGetUpdatePaymentList param)
        {
            string subQuery = UpdatePaymentSubQuery(param);

            string qry = $@" SELECT 
									sub.*, 
									COUNT(*) OVER () AS CountData  
								FROM (
										SELECT 
										r.BankAccountNumber,
										r.BankAccountName [BankAccountOwnerName],
										vd.Id AS VoucherDetailId,
										vh.VoucherNumber,
										vd.VoucherRefId,
										'' [InvoiceNo],
										r.BankName,
										vh.L_Currency,
										vd.TotalBaseAmmount [AmountAfterTaxes],
										'0' AS Tax,
										'0' AS Charges,
										vd.TotalBaseAmmount [AmountBeforeTaxes],
										vh.TransferNumber,
										CASE vd.StatusTransfer
											WHEN '1' THEN 'Success'
											WHEN '0' THEN 'Fail'
											ELSE ''
										END AS StatusTransferString,
										vh.TransferTime,
										vh.TransferTime AS TransferTimeString,
										vh.TransferType,
										vd.TransferNote, vd.StatusTransfer,
										vd.CreatedTime,
										vh.Status,
										vh.Category,
										vh.LastUpdatedTime,
										vh.LastUpdatedBy
									FROM VoucherDetail vd
									JOIN VoucherHeader vh ON vh.Id = vd.VoucherId
									JOIN TrexApr r ON r.NoAPR = vd.VoucherRefId
                                     ) sub WHERE {subQuery}     ";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            return qry;
        }
        public static string GetUpdatePaymentListTrexEER(ParamGetUpdatePaymentList param)
        {
            string subQuery = UpdatePaymentSubQuery(param);

            string qry = $@" SELECT 
									sub.*, 
									COUNT(*) OVER () AS CountData  
								FROM (  SELECT 
										r.BankAccountNumber,
										r.BankAccountName [BankAccountOwnerName],
										vd.Id AS VoucherDetailId,
										vh.VoucherNumber,
										vd.VoucherRefId,
										'' [InvoiceNo],
										r.BankName,
										vh.L_Currency,
										vd.TotalBaseAmmount [AmountAfterTaxes],
										'0' AS Tax,
										'0' AS Charges,
										vd.TotalBaseAmmount [AmountBeforeTaxes],
										vh.TransferNumber,
										CASE vd.StatusTransfer
											WHEN '1' THEN 'Success'
											WHEN '0' THEN 'Fail'
											ELSE ''
										END AS StatusTransferString,
										vh.TransferTime,
										vh.TransferTime AS TransferTimeString,
										vh.TransferType,
										vd.TransferNote, vd.StatusTransfer,
										vd.CreatedTime,
										vh.Status,
										vh.Category,
										vh.LastUpdatedTime,
										vh.LastUpdatedBy
									FROM VoucherDetail vd
									JOIN VoucherHeader vh ON vh.Id = vd.VoucherId
									JOIN TrexEerHeader r ON r.NoEER = vd.VoucherRefId
                                    ) sub WHERE {subQuery}     ";
            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            return qry;
        }
        public static string GetUpdatePaymentListTrexGER(ParamGetUpdatePaymentList param)
        {
            string subQuery = UpdatePaymentSubQuery(param);

            string qry = $@"    SELECT 
									sub.*, 
									COUNT(*) OVER () AS CountData  
								FROM (
										SELECT 
										r.BankAccountNumber,
										r.BankAccountName [BankAccountOwnerName],
										vd.Id AS VoucherDetailId,
										vh.VoucherNumber,
										vd.VoucherRefId,
										'' [InvoiceNo],
										r.BankName,
										vh.L_Currency,
										vd.TotalBaseAmmount [AmountAfterTaxes],
										'0' AS Tax,
										'0' AS Charges,
										vd.TotalBaseAmmount [AmountBeforeTaxes],
										vh.TransferNumber,
										CASE vd.StatusTransfer
											WHEN '1' THEN 'Success'
											WHEN '0' THEN 'Fail'
											ELSE ''
										END AS StatusTransferString,
										vh.TransferTime,
										vh.TransferTime AS TransferTimeString,
										vh.TransferType,
										vd.TransferNote, vd.StatusTransfer,
										vd.CreatedTime,
										vh.Status,
										vh.Category,
										vh.LastUpdatedTime,
										vh.LastUpdatedBy
									FROM VoucherDetail vd
									JOIN VoucherHeader vh ON vh.Id = vd.VoucherId
									JOIN TrexGerHeader r ON r.NoGER = vd.VoucherRefId
                                    ) sub WHERE {subQuery}      ";

            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            return qry;
        }
        public static string GetUpdatePaymentListTrexTER(ParamGetUpdatePaymentList param)
        {
            string subQuery = UpdatePaymentSubQuery(param);

            string qry = $@"    SELECT 
									sub.*, 
									COUNT(*) OVER () AS CountData  
								FROM (  SELECT 
										r.BankAccountNumber,
										r.BankAccountName [BankAccountOwnerName],
										vd.Id AS VoucherDetailId,
										vh.VoucherNumber,
										vd.VoucherRefId,
										'' [InvoiceNo],
										r.BankName,
										vh.L_Currency,
										vd.TotalBaseAmmount [AmountAfterTaxes],
										'0' AS Tax,
										'0' AS Charges,
										vd.TotalBaseAmmount [AmountBeforeTaxes],
										vh.TransferNumber,
										CASE vd.StatusTransfer
											WHEN '1' THEN 'Success'
											WHEN '0' THEN 'Fail'
											ELSE ''
										END AS StatusTransferString,
										vh.TransferTime,
										vh.TransferTime AS TransferTimeString,
										vh.TransferType,
										vd.TransferNote, vd.StatusTransfer,
										vd.CreatedTime,
										vh.Status,
										vh.Category,
										vh.LastUpdatedTime,
										vh.LastUpdatedBy
									FROM VoucherDetail vd
									JOIN VoucherHeader vh ON vh.Id = vd.VoucherId
									JOIN TrexTerHeader r ON r.NoTER = vd.VoucherRefId
                                    ) sub WHERE {subQuery}     ";

            if (param.IsExport)
            {
                qry = $"{qry} ORDER BY CreatedTime DESC";
            }
            else
            {
                qry = $"{qry} ORDER BY {param.SortColumn ?? "CreatedTime"} {param.SortDirection ?? "DESC"} OFFSET @Page ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            return qry;
        }
        public static string UpdatePaymentSubQuery(ParamGetUpdatePaymentList param)
        {
            string subQuery = @$"VoucherNumber= @VoucherNumber 
								AND VoucherRefId= @RequestNumber 
								AND BankAccountNumber= @BankAccountNumber 
								AND VoucherDetailId= @VoucherDetailId 
								AND Status= @Status 
								";
            if (String.IsNullOrEmpty(param.RequestNumber))
            {
                subQuery = subQuery.Replace("VoucherRefId= @RequestNumber ", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.VoucherNumber))
            {
                subQuery = subQuery.Replace("VoucherNumber= @VoucherNumber ", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.BankAccountNumber))
            {
                subQuery = subQuery.Replace("BankAccountNumber= @BankAccountNumber ", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.VoucherDetailId))
            {
                subQuery = subQuery.Replace("VoucherDetailId= @VoucherDetailId ", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.Status))
            {
                subQuery = subQuery.Replace("Status= @Status ", " 1=1 ");
            }
            return subQuery;
        }
        public static string GetTransferNumberExistGliProcess(ParamGetUpdatePaymentList param)
        {

            string qry = @$"SELECT TransferNumber 
							FROM GLINonBenefit
							WHERE Status = 1 AND TransferNumber = @TransferNumber ";
            return qry;
        }

        #region Report MCM
        public static string GetReportMcmDetailRI()
        {
            string qry = $@"SELECT DISTINCT
                           rd.BankAccountNumber as [Code]
					      ,rd.BankAccountOwnerName as [Name]
					      ,vh.L_Currency
					      ,(SELECT SUM(TotalBaseAmmount) FROM VoucherDetail WHERE VoucherRefId = vd.VoucherRefId)  as [TotalBaseAmount]
                                         ,CASE sc.SubCategoryName
						   WHEN 'Staff'
								THEN vd.VoucherRefId
						   ELSE
								CASE WHEN LEN(STUFF((
							 	     SELECT ';' + LTRIM(RTRIM(InvoiceNo))
							 	     FROM ReimbursementDetail rdsub
							 	     WHERE rdsub.ReimbursementId = rd.ReimbursementId
							 	     FOR XML PATH('')
							 	     ), 1, 1, '')) > 19
							 	THEN LEFT(STUFF((
							 	     SELECT ';' + LTRIM(RTRIM(InvoiceNo))
							 	     FROM ReimbursementDetail rdsub
							 	     WHERE rdsub.ReimbursementId = rd.ReimbursementId
							 	     FOR XML PATH('')
							 	     ), 1, 1, ''), 19)
							    ELSE STUFF((
							 	     SELECT ';' + LTRIM(RTRIM(InvoiceNo))
							 	     FROM ReimbursementDetail rdsub
							 	     WHERE rdsub.ReimbursementId = rd.ReimbursementId
							 	     FOR XML PATH('')
							 	     ), 1, 1, '')
							 	END
						   END as InvoiceNoShort
					      ,sc.SubCategoryName [InvoiceNo] --Perubahan after Go-Live, as requested by Mba Tetty & Mba Shindi
					      ,CASE 
					              WHEN LTRIM(RTRIM(rd.BankName)) = 'MANDIRI'
					                     THEN 'IBU'
					              WHEN LTRIM(RTRIM(rd.BankName)) = 'BANK MANDIRI'
					                     THEN 'IBU'
					              WHEN LTRIM(RTRIM(rd.BankName)) = 'BANKMANDIRI'
					                     THEN 'IBU'
                                                 WHEN LTRIM(RTRIM(rd.BankName)) = 'PT. BANK MANDIRI (PERSERO) TBK'
					                     THEN 'IBU'
					              ELSE 'LBU'
					              END AS [FtService]
					      ,rd.BankCode
					      ,rd.BankName
					      ,vd.Id
					      ,'CEK' as [BankBranch]
					      ,'CEK' as [BankShortName]
					      ,CASE vh.IsEmail
					       WHEN 1 THEN 'Y' ELSE 'N'
					       END AS [IsEmail]
					      ,CASE vh.IsEmail
					       WHEN 1 THEN ar.RequestorEmail ELSE ''
					       END AS [Email]  --Perubahan after Go-Live, as requested by Mba Tetty & Mba Shindi & pak Suharman
					FROM VoucherHeader vh
					JOIN VoucherDetail vd on vh.Id = vd.VoucherId
					JOIN Reimbursement r on r.RequestNumber = vd.VoucherRefId
					JOIN ReimbursementDetail rd on rd.ReimbursementId = r.Id
					JOIN vendor vn on vn.Id = rd.VendorId
                                   JOIN subcategory sc on sc.Id = r.ReimbursementType_SubCategoryId
					JOIN ApprovalRequest ar on ar.RequestNo = r.RequestNumber
					WHERE vd.VoucherId = @VoucherId
			";
            return qry;
        }
        public static string GetReportMcmDetailTRSTL()
        {
            string qry = $@"SELECT DISTINCT
                            r.BankAccountNumber as [Code]
                           ,r.BankAccountOwnerName as [Name]
                           ,vh.L_Currency
                           ,(SELECT SUM(TotalBaseAmmount) FROM VoucherDetail WHERE VoucherRefId = vd.VoucherRefId)  as [TotalBaseAmount]
                           ,(SELECT TOP 1 mz.ZoneName FROM TravelRequestTransportation trt JOIN M_Zone mz on mz.Id = trt.To_M_ZoneId WHERE TravelRequestId = r.TravelRequestId) [InvoiceNoShort]
                           ,r.RequestNumber [InvoiceNo] --Perubahan after UAT, as requested by Mba Tetty & Mba Shindi (13-8-2024)
                           ,CASE 
                                   WHEN LTRIM(RTRIM(r.BankName)) = 'MANDIRI'
                                          THEN 'IBU'
                                   WHEN LTRIM(RTRIM(r.BankName)) = 'BANK MANDIRI'
                                          THEN 'IBU'
                                   WHEN LTRIM(RTRIM(r.BankName)) = 'BANKMANDIRI'
                                          THEN 'IBU'
                                   WHEN LTRIM(RTRIM(r.BankName)) = 'PT. BANK MANDIRI (PERSERO) TBK'
                                          THEN 'IBU'
                                   ELSE 'LBU'
                                   END AS [FtService]
                           ,r.BankCode
                           ,r.BankName
                           ,vd.Id
                           ,'CEK' as [BankBranch]
                           ,'CEK' as [BankShortName]
                           ,CASE vh.IsEmail
                            WHEN 1 THEN 'Y' ELSE 'N'
                            END AS [IsEmail]
                           ,CASE vh.IsEmail
                            WHEN 1 THEN ar.RequestorEmail ELSE ''
                            END AS [Email]  --Perubahan after Go-Live, as requested by Mba Tetty & Mba Shindi & pak Suharman
                     FROM VoucherHeader vh
                     JOIN VoucherDetail vd on vh.Id = vd.VoucherId
                     JOIN TravelRequestExpense r on r.RequestNumber = vd.VoucherRefId
                     JOIN TravelRequestExpenseDetail rd on rd.TravelRequestExpenseId = r.Id
                     JOIN ApprovalRequest ar on ar.RequestNo = r.RequestNumber
				     WHERE vd.VoucherId = @VoucherId
			";
            return qry;
        }
        public static string GetReportMcmDetailINVTR()
        {
            string qry = $@"SELECT DISTINCT
                              r.BankAccountNumber as [Code]
                             ,r.BankAccountOwnerName as [Name]
                             ,vh.L_Currency
                             ,(SELECT SUM(TotalBaseAmmount) FROM VoucherDetail WHERE VoucherRefId = vd.VoucherRefId)  as [TotalBaseAmount]
                             ,CASE WHEN LEN(STUFF((
                       		 SELECT ';' + LTRIM(RTRIM(InvoiceNumber))
                       		 FROM InvoiceTravelDetail rdsub
                       		 WHERE rdsub.InvoiceTravelId = rd.InvoiceTravelId
                       		 FOR XML PATH('')
                       		 ), 1, 1, '')) > 19
                       	     THEN LEFT(STUFF((
                       		 SELECT ';' + LTRIM(RTRIM(InvoiceNumber))
                       		 FROM InvoiceTravelDetail rdsub
                       		 WHERE rdsub.InvoiceTravelId = rd.InvoiceTravelId
                       		 FOR XML PATH('')
                       		 ), 1, 1, ''), 19)
                       	     ELSE STUFF((
                       		 SELECT ';' + LTRIM(RTRIM(InvoiceNumber))
                       		 FROM InvoiceTravelDetail rdsub
                       		 WHERE rdsub.InvoiceTravelId = rd.InvoiceTravelId
                       		 FOR XML PATH('')
                       		 ), 1, 1, '')
                          	   END as [InvoiceNoShort]
                             ,'Invoice Travel' [InvoiceNo] --Perubahan after Go-Live, as requested by Mba Tetty & Mba Shindi
                             ,CASE 
                                     WHEN LTRIM(RTRIM(r.BankName)) = 'MANDIRI'
                                            THEN 'IBU'
                                     WHEN LTRIM(RTRIM(r.BankName)) = 'BANK MANDIRI'
                                            THEN 'IBU'
                                     WHEN LTRIM(RTRIM(r.BankName)) = 'BANKMANDIRI'
                                            THEN 'IBU'
                                     WHEN LTRIM(RTRIM(r.BankName)) = 'PT. BANK MANDIRI (PERSERO) TBK'
                                            THEN 'IBU'
                                     ELSE 'LBU'
                                     END AS [FtService]
                             ,r.BankCode
                             ,r.BankName
                             ,vd.Id
                             ,'CEK' as [BankBranch]
                             ,'CEK' as [BankShortName]
                             ,CASE vh.IsEmail
                              WHEN 1 THEN 'Y' ELSE 'N'
                              END AS [IsEmail]
                             ,CASE vh.IsEmail
                              WHEN 1 THEN ar.RequestorEmail ELSE ''
                              END AS [Email]  --Perubahan after Go-Live, as requested by Mba Tetty & Mba Shindi & pak Suharman
                           FROM VoucherHeader vh
                           JOIN VoucherDetail vd on vh.Id = vd.VoucherId
                           JOIN InvoiceTravel r on r.RequestNumber = vd.VoucherRefId
                           JOIN InvoiceTravelDetail rd on rd.InvoiceTravelId = r.Id
                           JOIN ApprovalRequest ar on ar.RequestNo = r.RequestNumber
						WHERE vd.VoucherId = @VoucherId
			";
            return qry;
        }
        public static string GetReportMcmDetailGER()
        {
            string qry = $@"SELECT DISTINCT
                                                      rd.AccountNumber as [Code]
                                                     ,rd.BankAccountOwnerName as [Name]
                                                     ,vh.L_Currency
                                                     ,rd.NettAmount [TotalBaseAmount]
                                                     ,rd.PolicyNumber as [InvoiceNoShort]
                                                     ,'Ger' [InvoiceNo] --Perubahan after Go-Live, as requested by Mba Tetty & Mba Shindi
                                                     ,CASE 
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'MANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'BANK MANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'BANKMANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'PT. BANK MANDIRI (PERSERO) TBK'
                                                                    THEN 'IBU'
                                                             ELSE 'LBU'
                                                             END AS [FtService]
                                                     ,''
                                                     ,rd.BankName
                                                     ,vd.Id
                                                     ,'CEK' as [BankBranch]
                                                     ,'CEK' as [BankShortName]
                                                     ,CASE vh.IsEmail
                                                      WHEN 1 THEN 'Y' ELSE 'N'
                                                      END AS [IsEmail]
                                                     ,CASE vh.IsEmail
                                                      WHEN 1 THEN ar.RequestorEmail ELSE ''
                                                      END AS [Email]  --Perubahan after Go-Live, as requested by Mba Tetty & Mba Shindi & pak Suharman
                                                   FROM VoucherHeader vh
                                                   JOIN VoucherDetail vd on vh.Id = vd.VoucherId
                                                   JOIN (
														SELECT gd.Id
															  ,gh.RequestNumber
															  ,gd.BankName
															  ,gd.PolicyNumber
															  ,gd.NettAmount
															  ,gd.AccountNumber
															  ,gd.BankAccountOwnerName
														FROM GerHeader gh
														JOIN GerDetail gd on gh.Id = gd.GerHeaderId
													 ) rd on CONCAT(rd.RequestNumber, ' - ', rd.Id) = vd.VoucherRefId
                                                   JOIN ApprovalRequest ar on ar.RequestNo = rd.RequestNumber
                                                   WHERE vd.VoucherId = @VoucherId
			";
            return qry;
        }
        public static string GetReportMcmDetailTrexAPR()
        {
            string qry = $@"SELECT DISTINCT
                                                      rd.BankAccountNumber as [Code]
                                                     ,rd.BankAccountName as [Name]
                                                     ,vh.L_Currency
                                                     ,vd.TotalBaseAmmount [TotalBaseAmount]
                                                     ,rd.NoAPR as [InvoiceNoShort]
                                                     ,rd.Description [InvoiceNo]
                                                     ,CASE 
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'MANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'BANK MANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'BANKMANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'PT. BANK MANDIRI (PERSERO) TBK'
                                                                    THEN 'IBU'
                                                             ELSE 'LBU'
                                                             END AS [FtService]
                                                     ,''
                                                     ,rd.BankName
                                                     ,vd.Id
                                                     ,'CEK' as [BankBranch]
                                                     ,'CEK' as [BankShortName]
                                                     ,CASE vh.IsEmail
                                                      WHEN 1 THEN 'Y' ELSE 'N'
                                                      END AS [IsEmail]
                                                     ,vh.Email [Email]  --Perubahan after Go-Live, as requested by Mba Tetty & Mba Shindi & pak Suharman
                                                   FROM VoucherHeader vh
                                                   JOIN VoucherDetail vd on vh.Id = vd.VoucherId
                                                   JOIN TrexApr rd on rd.NoAPR = vd.VoucherRefId
                                                   WHERE vd.VoucherId = @VoucherId
			";
            return qry;
        }
        public static string GetReportMcmDetailTrexEER()
        {
            string qry = $@"SELECT DISTINCT
                                                      rd.BankAccountNumber as [Code]
                                                     ,rd.BankAccountName as [Name]
                                                     ,vh.L_Currency
                                                     ,vd.TotalBaseAmmount [TotalBaseAmount]
                                                     ,rd.NoEer as [InvoiceNoShort]
                                                     ,rd.Description [InvoiceNo]
                                                     ,CASE 
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'MANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'BANK MANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'BANKMANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'PT. BANK MANDIRI (PERSERO) TBK'
                                                                    THEN 'IBU'
                                                             ELSE 'LBU'
                                                             END AS [FtService]
                                                     ,''
                                                     ,rd.BankName
                                                     ,vd.Id
                                                     ,'CEK' as [BankBranch]
                                                     ,'CEK' as [BankShortName]
                                                     ,CASE vh.IsEmail
                                                      WHEN 1 THEN 'Y' ELSE 'N'
                                                      END AS [IsEmail]
                                                     ,vh.Email [Email]  --Perubahan after Go-Live, as requested by Mba Tetty & Mba Shindi & pak Suharman
                                                   FROM VoucherHeader vh
                                                   JOIN VoucherDetail vd on vh.Id = vd.VoucherId
                                                   JOIN TrexEerHeader rd on rd.NoEer = vd.VoucherRefId
                                                   WHERE vd.VoucherId = @VoucherId
			";
            return qry;
        }
        public static string GetReportMcmDetailTrexGER()
        {
            string qry = $@"SELECT DISTINCT
                                                      rd.BankAccountNumber as [Code]
                                                     ,rd.BankAccountName as [Name]
                                                     ,vh.L_Currency
                                                     ,vd.TotalBaseAmmount [TotalBaseAmount]
                                                     ,rd.NoGer as [InvoiceNoShort]
                                                     ,rd.Description [InvoiceNo]
                                                     ,CASE 
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'MANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'BANK MANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'BANKMANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'PT. BANK MANDIRI (PERSERO) TBK'
                                                                    THEN 'IBU'
                                                             ELSE 'LBU'
                                                             END AS [FtService]
                                                     ,''
                                                     ,rd.BankName
                                                     ,vd.Id
                                                     ,'CEK' as [BankBranch]
                                                     ,'CEK' as [BankShortName]
                                                     ,CASE vh.IsEmail
                                                      WHEN 1 THEN 'Y' ELSE 'N'
                                                      END AS [IsEmail]
                                                     ,vh.Email [Email]  --Perubahan after Go-Live, as requested by Mba Tetty & Mba Shindi & pak Suharman
                                                   FROM VoucherHeader vh
                                                   JOIN VoucherDetail vd on vh.Id = vd.VoucherId
                                                   JOIN TrexGerHeader rd on rd.NoGer = vd.VoucherRefId
                                                   WHERE vd.VoucherId = @VoucherId
			";
            return qry;
        }
        public static string GetReportMcmDetailTrexTER()
        {
            string qry = $@"SELECT DISTINCT
                                                      rd.BankAccountNumber as [Code]
                                                     ,rd.BankAccountName as [Name]
                                                     ,vh.L_Currency
                                                     ,vd.TotalBaseAmmount [TotalBaseAmount]
                                                     ,rd.NoTer as [InvoiceNoShort]
                                                     ,CONCAT(btr.NoBTR, ' - ' , btr.TravelDestination) [InvoiceNo]
                                                     ,CASE 
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'MANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'BANK MANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'BANKMANDIRI'
                                                                    THEN 'IBU'
                                                             WHEN LTRIM(RTRIM(rd.BankName)) = 'PT. BANK MANDIRI (PERSERO) TBK'
                                                                    THEN 'IBU'
                                                             ELSE 'LBU'
                                                             END AS [FtService]
                                                     ,''
                                                     ,rd.BankName
                                                     ,vd.Id
                                                     ,'CEK' as [BankBranch]
                                                     ,'CEK' as [BankShortName]
                                                     ,CASE vh.IsEmail
                                                      WHEN 1 THEN 'Y' ELSE 'N'
                                                      END AS [IsEmail]
                                                     ,vh.Email [Email]  --Perubahan after Go-Live, as requested by Mba Tetty & Mba Shindi & pak Suharman
                                                   FROM VoucherHeader vh
                                                   JOIN VoucherDetail vd  on vh.Id     = vd.VoucherId
                                                   JOIN TrexTerHeader rd  on rd.NoTer  = vd.VoucherRefId
												   JOIN TrexBTRHeader btr on btr.BTRId = rd.BTRId
                                                   WHERE vd.VoucherId = @VoucherId
			";
            return qry;
        }
        public static string GetReportMcmDetailSC()
        {
            string qry = $@"
				SELECT ipo.BankAccountNumber as [Code]
						,ipo.BankAccountOwnerName as [Name]
						,vh.L_Currency
						,(SELECT SUM(TotalBaseAmmount) FROM VoucherDetail WHERE VoucherRefId = vd.VoucherRefId)  as [TotalBaseAmount]
						,CASE sc.SubCategoryName
									WHEN 'Staff'
																THEN vd.VoucherRefId
									ELSE
							ipo.InvoiceNumber
						END AS [InvoiceNo]
									,CASE sc.SubCategoryName
									WHEN 'Staff'
																THEN ipo.Remark
									ELSE
							CASE WHEN LEN(ipo.InvoiceNumber) > 19
							THEN LEFT(ipo.InvoiceNumber, 19)
							ELSE ipo.InvoiceNumber
													END
						END AS [InvoiceNoShort]
						,CASE 
								WHEN LTRIM(RTRIM(ipo.BankName)) = 'MANDIRI'
										THEN 'IBU'
								WHEN LTRIM(RTRIM(ipo.BankName)) = 'BANK MANDIRI'
										THEN 'IBU'
								WHEN LTRIM(RTRIM(ipo.BankName)) = 'BANKMANDIRI'
										THEN 'IBU'
								WHEN LTRIM(RTRIM(ipo.BankName)) = 'PT. BANK MANDIRI (PERSERO) TBK'
										THEN 'IBU'
								ELSE 'LBU'
								END AS [FtService]
						,ipo.BankCode
						,ipo.BankName
						,'CEK' as [BankBranch]
						,'CEK' as [BankShortName]
						,CASE vh.IsEmail
						WHEN 1 THEN 'Y' ELSE 'N'
						END AS [IsEmail]
						,CASE vh.IsEmail
						WHEN 1 THEN UA.Email ELSE ''
						END AS [Email]
				FROM VoucherHeader vh
					JOIN VoucherDetail vd on vd.VoucherId = vh.Id
					JOIN InvoicePO ipo on cast(ipo.Id as varchar(100)) = substring(vd.VoucherRefId,0,charindex(' - ',vd.VoucherRefId))
					JOIN PurchaseOrder PO ON PO.Id = IPO.PurchaeseOrderId
					JOIN Flips.UserAccount UA ON UA.Username = PO.RequestorName
					JOIN Vendor vn on vn.Id = ipo.VendorId
					JOIN subcategory sc on sc.Id = vn.SubCategoryId
				WHERE vh.Category = 'Shopping Cart'
					AND vd.VoucherId = @VoucherId
			";
            return qry;
        }
        public static string GetReportMcmDetailNON()
        {
            string qry = $@"
				SELECT INV.BankAccountNumber as [Code],
						INV.BankAccountOwnerName as [Name],
						VH.L_Currency,
						(
							SELECT SUM(TotalBaseAmmount)
							FROM VoucherDetail
							WHERE VoucherRefId = VD.VoucherRefId
						) as [TotalBaseAmount],
						CASE SC.SubCategoryName
							WHEN 'Staff' THEN
								VD.VoucherRefId
							ELSE
								INV.InvoiceNumber
						END AS [InvoiceNo],
						CASE SC.SubCategoryName
							WHEN 'Staff' THEN
								INV.Remark
							ELSE
								CASE
									WHEN LEN(INV.InvoiceNumber) > 19 THEN
										LEFT(INV.InvoiceNumber, 19)
									ELSE
										INV.InvoiceNumber
								END
						END AS [InvoiceNoShort],
						CASE
							WHEN LTRIM(RTRIM(INV.BankName)) = 'MANDIRI' THEN
								'IBU'
							WHEN LTRIM(RTRIM(INV.BankName)) = 'BANK MANDIRI' THEN
								'IBU'
							WHEN LTRIM(RTRIM(INV.BankName)) = 'BANKMANDIRI' THEN
								'IBU'
							WHEN LTRIM(RTRIM(INV.BankName)) = 'PT. BANK MANDIRI (PERSERO) TBK' THEN
								'IBU'
							ELSE
								'LBU'
						END AS [FtService],
						INV.BankCode,
						INV.BankName,
						'CEK' as [BankBranch],
						'CEK' as [BankShortName],
						CASE VH.IsEmail
							WHEN 1 THEN
								'Y'
							ELSE
								'N'
						END AS [IsEmail],
						CASE VH.IsEmail
							WHEN 1 THEN
								UA.Email
							ELSE
								''
						END AS [Email]
				FROM VoucherHeader VH
					JOIN VoucherDetail VD
						ON VH.Id = VD.VoucherId
					JOIN InvoicePO INV
						ON cast(INV.Id as varchar(100)) = substring(vd.VoucherRefId,0,charindex(' - ',vd.VoucherRefId))
					JOIN PONonShopping PO
						ON PO.Id = INV.PurchaeseOrderId
					JOIN Flips.UserAccount UA 
						ON UA.Username = PO.RequestorName
					JOIN Vendor VN
						ON INV.VendorId = VN.Id
					JOIN SubCategory SC
						on SC.Id = VN.SubCategoryId
				WHERE vh.Category = 'Non Shopping Cart'
					AND VD.VoucherId = @VoucherId
					AND INV.CategoryProcess_SubCategoryId =
					(
						SELECT Id FROM SubCategory WHERE SubCategoryCode = 'SC-2024-02-01262'
					)
			";
            return qry;
        }
        #endregion

        public static string TakeoutVoucherCheckerRI()
        {
            string qry = $@"
                            --Update table transaction
                            UPDATE Reimbursement
                            SET [Status] = 2,
                                ReasonReject = @Reason,
                                LastUpdatedBy = @CheckerName,
                                LastUpdatedTime = @DateUpdate
                            WHERE RequestNumber in (
                                                       SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')
                                                   )
                            --Update approval request 
                            UPDATE ApprovalRequest
                            SET [Status] = 2,
                                LastUpdatedBy = @CheckerName,
                                LastUpdatedTime = @DateUpdate
                            WHERE RequestNo in (
                                                   SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')
                                               )
                            
			";
            return qry;
        }
        public static string TakeoutVoucherCheckerTRSTL()
        {
            string qry = $@"
                            --Update table transaction
                            UPDATE TravelRequestExpense
                                        SET [Status] = 2,
                                            ReasonReject = @Reason,
                                            LastUpdatedBy = @CheckerName,
                                            LastUpdatedTime = @DateUpdate
                                        WHERE RequestNumber in (
                                                                   SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')
                                                               )
                                        --Update approval request 
                                        UPDATE ApprovalRequest
                                        SET [Status] = 2,
                                            LastUpdatedBy = @CheckerName,
                                            LastUpdatedTime = @DateUpdate
                                        WHERE RequestNo in (
                                                               SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')
                                                           )
                            
			";
            return qry;
        }
        public static string TakeoutVoucherCheckerINVTR()
        {
            string qry = $@"
                            --Update table transaction
                                        UPDATE InvoiceTravel
                                        SET [Status] = 2,
                                            ReasonReject = @Reason,
                                            LastUpdatedBy = @CheckerName,
                                            LastUpdatedTime = @DateUpdate
                                        WHERE RequestNumber in (
                                                                   SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')
                                                               )
                                        --Update approval request 
                                        UPDATE ApprovalRequest
                                        SET [Status] = 2,
                                            LastUpdatedBy = @CheckerName,
                                            LastUpdatedTime = @DateUpdate
                                        WHERE RequestNo in (
                                                               SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')
                                                           )
                            
			";
            return qry;
        }
        public static string TakeoutVoucherCheckerGER()
        {
            string qry = $@"
                            --Update table transaction
                                        --Update GerHeader

                                        DECLARE @Temp TABLE (RequestNumber VARCHAR(100));
                                        UPDATE gh
                                        SET gh.[Status] = 2,
                                            gh.ReasonReject = @Reason,
                                            gh.LastUpdatedBy = @CheckerName,
                                            gh.LastUpdatedTime = @DateUpdate
                                        OUTPUT inserted.RequestNumber INTO @Temp
                                        FROM GerHeader gh
                                        JOIN GerDetail gd ON gd.GerHeaderId = gh.Id
                                        WHERE CONCAT(gh.RequestNumber, ' - ', gd.Id) IN (
                                            SELECT LTRIM(RTRIM(value)) FROM string_split(@RequestNumbers, ',')
                                        );
                                        
                                        -- Update GerDetail
                                        UPDATE gd
                                        SET gd.[Status] = 2,
                                            gd.LastUpdatedBy = @CheckerName,
                                            gd.LastUpdatedTime = @DateUpdate
                                        FROM GerDetail gd
                                        JOIN GerHeader gh ON gd.GerHeaderId = gh.Id
                                        WHERE CONCAT(gh.RequestNumber, ' - ', gd.Id) IN (
                                            SELECT LTRIM(RTRIM(value)) FROM string_split(@RequestNumbers, ',')
                                        );
            
                                        --Update approval request 
                                        UPDATE ApprovalRequest
                                        SET [Status] = 2,
                                            LastUpdatedBy = @CheckerName,
                                            LastUpdatedTime = @DateUpdate
                                        WHERE RequestNo in (
                                                              SELECT TOP 1 RequestNumber FROM @Temp
                                                           )
                            
			";
            return qry;
        }
        public static string TakeoutVoucherCheckerSC()
        {
            string qry = $@"
				UPDATE InvoicePO
				SET ReasonReject = @Reason,
					Status = (SELECT ValueId FROM MasterTable WHERE Name ='Invoice Open'),
					LastUpdateBy = @CheckerName,
					LastUpdateTime = @DateUpdate
				WHERE CONCAT(Id, ' - ', InvoiceNumber) = @RequestNumbers

				UPDATE IPOIOC
				SET Status =  (SELECT ValueId FROM MasterTable WHERE Name ='Repair' and Category ='InvoiceManagement.Status'),
				LastUpdatedBy = @CheckerName,
				LastUpdatedTime = @DateUpdate
				FROM InvoicePOItemOtherCost IPOIOC
				JOIN InvoicePO IPO ON IPO.Id = IPOIOC.InvoicePOId
				WHERE CONCAT(IPO.Id, ' - ', IPO.InvoiceNumber) = @RequestNumbers
			";
            return qry;
        }

        public static string TakeoutVoucherCheckerNonSC()
        {
            string qry = $@"
				UPDATE InvoicePO
				SET ReasonReject = @Reason,
					Status = (SELECT ValueId FROM MasterTable WHERE Name ='Invoice Open'),
					LastUpdateBy = @CheckerName,
					LastUpdateTime = @DateUpdate
				WHERE CONCAT(Id, ' - ', InvoiceNumber) = @RequestNumbers

				UPDATE IPOIOC
					SET Status =  (SELECT ValueId FROM MasterTable WHERE Name ='Repair' and Category ='InvoiceManagement.Status'),
					LastUpdatedBy = @CheckerName,
					LastUpdatedTime = @DateUpdate
				FROM InvoicePOItemOtherCost IPOIOC
					JOIN InvoicePO IPO ON IPO.Id = IPOIOC.InvoicePOId
				WHERE CONCAT(IPO.Id, ' - ', IPO.InvoiceNumber) = @RequestNumbers
			";
            return qry;
        }
        public static string TakeoutVoucherCheckerTrexAPR()
        {
            string qry = $@"
                            --Update table transaction
                                        UPDATE TrexAPR
                                        SET [Status] = 2,
                                            UpdatedBy = @CheckerName,
                                            UpdatedAt = @DateUpdate
                                        WHERE NoAPR in (
                                                                   SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')
                                                       )
			";
            return qry;
        }
        public static string TakeoutVoucherCheckerTrexEER()
        {
            string qry = $@"
                             --Update table transaction
                                        UPDATE TrexEERHeader
                                        SET [Status] = 2,
                                            UpdatedBy = @CheckerName,
                                            UpdatedAt = @DateUpdate
                                        WHERE NoEER in (
                                                                   SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')
                                                       )
			";
            return qry;
        }
        public static string TakeoutVoucherCheckerTrexGER()
        {
            string qry = $@"
                            --Update table transaction
                                        UPDATE TrexGERHeader
                                        SET [Status] = 2,
                                            UpdatedBy = @CheckerName,
                                            UpdatedAt = @DateUpdate
                                        WHERE NoGER in (
                                                                   SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')
                                                       )
			";
            return qry;
        }
        public static string TakeoutVoucherCheckerTrexTER()
        {
            string qry = $@"
                            --Update table transaction
                                        UPDATE TrexTERHeader
                                        SET [Status] = 2,
                                            UpdatedBy = @CheckerName,
                                            UpdatedAt = @DateUpdate
                                        WHERE NoTER in (
                                                                   SELECT LTRIM(RTRIM((value))) FROM string_split(@RequestNumbers, ',')
                                                       )
			";
            return qry;
        }

        #region SHOP & NON VOUCHER
        public static string GetVoucherDetail()
        {
            string qry = $@"SELECT sub.*, COUNT(*) OVER () as CountData  FROM (
								SELECT  vd.* 
										,vd.CreatedTime as [TransactionDateString]
										,vh.VoucherNumber
										,vh.Category
										,mt.Description as [StatusString]
										,CASE vd.StatusTransfer
											WHEN '0' THEN 'Failed'
											WHEN '1' THEN 'Success'
											ELSE ''
										END as [StatusTransferString]
										,vh.BankTransferName [BankName]
										,vh.BankTransferCode [BankAccountNumber]
										,ipo.BankAccountOwnerName [BankAccountOwnerName]
								FROM   VoucherHeader vh
								JOIN   VoucherDetail vd on vh.Id = vd.VoucherId
								JOIN   MasterTable mt on mt.ValueId = vh.Status
								JOIN InvoicePO ipo on CONCAT(ipo.Id, ' - ', ipo.InvoiceNumber)  =  vd.VoucherRefId
								WHERE  mt.Category = 'Voucher.Status' AND (vd.VoucherId = @Id OR vh.VoucherNumber = null)
							) sub";
            return qry;
        }
        public static string GetVoucherDetailShoppingCart()
        {
            string qry = $@"
							SELECT 
								sub.*, 
								COUNT(*) OVER () AS CountData  
							FROM (
								SELECT  
									vd.Id,
									vd.VoucherId,
									vd.RateAmmount,
									vd.TotalBaseAmmount,
									vd.TotalOriginalAmmount,
									vd.StatusTransfer,
									vd.Status,
									vd.TransferNote,
									vd.CreatedBy,
									vd.CreatedTime,
									vd.LastUpdatedBy,
									vd.LastUpdatedTime,
									vd.CreatedTime AS [TransactionDateString],
									vh.VoucherNumber,
									vh.Category,
									mt.Description AS [StatusString],
									CASE vd.StatusTransfer
										WHEN '0' THEN 'Failed'
										WHEN '1' THEN 'Success'
										ELSE ''
									END AS [StatusTransferString],
									vd.VoucherRefId AS [VoucherRefId],
									po.PONumber AS [HyperlinkString],
									ipo.BankName,
									ipo.BankAccountNumber,
									ipo.BankAccountOwnerName AS [BankAccountOwnerName],
									(
										SELECT STRING_AGG(I_I.Name, ', ')
										FROM PurchaseOrderDetail I_POD
										JOIN PurchaseRequestItemDetail I_PRID ON I_PRID.Id = I_POD.PurchaseRequestItemDetailId
										JOIN Item I_I ON I_I.Id = I_PRID.ItemId
										WHERE I_POD.PurchaseOrderId = po.Id
									) AS [Description]
								FROM VoucherHeader vh
								JOIN VoucherDetail vd ON vh.Id = vd.VoucherId
								JOIN MasterTable mt ON mt.ValueId = vh.Status
								JOIN InvoicePO ipo ON CONCAT(ipo.Id, ' - ', ipo.InvoiceNumber) = vd.VoucherRefId
								JOIN PurchaseOrder po ON po.Id = ipo.PurchaeseOrderId
								WHERE mt.Category = 'Voucher.Status' 
									AND (vd.VoucherId = @Id OR vh.VoucherNumber = @VoucherNumber)
							) sub
							";
            return qry;
        }
        public static string GetVoucherDetailNonShoppingCart()
        {
            string qry = $@"
								SELECT 
									sub.*, 
									COUNT(*) OVER () AS CountData  
								FROM (
									SELECT  
										vd.Id,
										vd.VoucherId,
										vd.RateAmmount,
										vd.TotalBaseAmmount,
										vd.TotalOriginalAmmount,
										vd.StatusTransfer,
										vd.Status,
										vd.TransferNote,
										vd.CreatedBy,
										vd.CreatedTime,
										vd.LastUpdatedBy,
										vd.LastUpdatedTime,
										vd.CreatedTime AS [TransactionDateString],
										vh.VoucherNumber,
										vh.Category,
										mt.Description AS [StatusString],
										CASE vd.StatusTransfer
											WHEN '0' THEN 'Failed'
											WHEN '1' THEN 'Success'
											ELSE ''
										END AS [StatusTransferString],
										ipo.BankName,
										ipo.BankAccountNumber,
										ipo.BankAccountOwnerName AS [BankAccountOwnerName],
										VD.VoucherRefId AS [VoucherRefId],
										PONS.PONumber AS [HyperlinkString],
										(
											SELECT STRING_AGG(i_PONSD.ItemDescription, ', ')
											FROM PONonShoppingDetail i_PONSD
											WHERE i_PONSD.PONonShoppingId = PONS.Id
										) AS [Description]
									FROM VoucherHeader vh
									JOIN VoucherDetail vd ON vh.Id = vd.VoucherId
									JOIN MasterTable mt ON mt.ValueId = vh.Status
									JOIN InvoicePO ipo ON CONCAT(ipo.Id, ' - ', ipo.InvoiceNumber) = vd.VoucherRefId
									JOIN PONonShopping PONS ON PONS.Id = ipo.PurchaeseOrderId
									WHERE mt.Category = 'Voucher.Status' 
										AND (vd.VoucherId = @Id OR vh.VoucherNumber = @VoucherNumber)
								) sub
							";
            return qry;
        }

        #endregion
        #endregion

        #region Fin Settlement
        public static string GetApprovalFinanceSTL(ParamGetRequestList param)
        {
            string subQuery = $@" ar.ApprovalFlowId = (SELECT Id from ApprovalFlow WHERE Name = @ApprovalFlowName)
								  AND s.SettlementNumber IS NOT NULL 
                                  AND s.SettlementNumber LIKE @RequestNumber 
							      AND ar.Status = @Status 
								  AND (argm.Username = @ApprovalName AND argm.ApprovalDate IS NULL)
                                  AND CAST(ISNULL(ar.CreatedTime,s.CreatedTime) as date) BETWEEN CAST(@RequestDateFrom as date) AND CAST(@RequestDateTo as date) ";
            if (String.IsNullOrEmpty(param.RequestNumber))
            {
                subQuery = subQuery.Replace("s.SettlementNumber LIKE @RequestNumber", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.Status))
            {
                subQuery = subQuery.Replace("ar.Status = @Status", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.ApprovalName))
            {
                subQuery = subQuery.Replace("(argm.Username = @ApprovalName AND argm.ApprovalDate IS NOT NULL)", " 1=1 ");
            }
            if (String.IsNullOrEmpty(param.RequestDateFrom))
            {
                subQuery = subQuery.Replace("CAST(ISNULL(ar.CreatedTime,s.CreatedTime) as date) BETWEEN CAST(@RequestDateFrom as date) AND CAST(@RequestDateTo as date)", " 1=1 ");
            }
            if (!String.IsNullOrEmpty(param.Search?.Value))
            {
                subQuery = String.Concat(subQuery, " AND (s.SettlementNumber LIKE @Search OR ar.RequestorUserName LIKE @Search OR cc.Name LIKE @Search) ");
            }
            string qry = $@"SELECT sub.*, COUNT(*) OVER () as CountData FROM (	
						        SELECT DISTINCT r.RequestNumber AS RequestNumber, s.SettlementNumber AS SettlementNumber, ISNULL(ar.RequestorUserName, s.CreatedBy) AS RequestorName, cc.[Name] AS RequestorCostCenter,
						        (	SELECT TOP 1 ShortDescription FROM MasterTable
							        WHERE Category = 'ApprovalRequest.Status'
							        AND ValueId = ISNULL(ar.Status, s.Status)
						        )	AS [Status], 
						        '-' VendorName,
						        (SELECT SUM(Amount) from SettlementDetail sdsub WHERE sdsub.SettlementId =  s.Id) [GrandTotal],
						        '' [Description],
						        CONVERT(VARCHAR(20),s.CreatedTime,106) AS TransactionDate, s.CreatedTime, 'Settlement' [RequestType], 
                                CONVERT(INT, ISNULL(ta.TotalApproval, 0)) [TotalApproval], 
                                CONVERT(INT, ISNULL(ta2.TotalApprovalApproved, 0)) [TotalApprovalApproved], 
                                CONVERT(INT, ISNULL(ta3.TotalApprovalRejected, 0)) [TotalApprovalRejected],
						          0 GenerateId,
						          0 InvoiceStatus,
								rd.[L_Currency] LCurrency
						        FROM dbo.[Settlement] s
								JOIN dbo.[SettlementDetail] sd ON s.Id = sd.SettlementId
								JOIN dbo.[Reimbursement] r ON s.ReimbursementId = r.Id
								JOIN dbo.[ReimbursementDetail] rd ON r.Id = rd.ReimbursementId
						        JOIN dbo.[CostCenter] cc ON r.CostCenterId = cc.Id
						        JOIN dbo.[ApprovalRequest] ar ON s.SettlementNumber = ar.RequestNo
						        JOIN dbo.[ApprovalRequestGroup] arg ON ar.Id = arg.ApprovalRequestId
						        JOIN dbo.[ApprovalRequestGroupMember] argm ON argm.ApprovalRequestId = ar.Id and argm.Status = 1
						        LEFT JOIN (
							        SELECT ar.Id, COUNT(*) TotalApproval FROM ApprovalRequestGroupMember argm 
							        JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id
							        GROUP BY ar.Id
						        ) ta ON ar.Id = ta.Id
						        LEFT JOIN (
							        SELECT ar.Id, COUNT(*) TotalApprovalApproved FROM ApprovalRequestGroupMember argm 
							        JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id
							        WHERE argm.Status = 2
							        GROUP BY ar.Id
						        ) ta2 ON ar.Id = ta2.Id
						        LEFT JOIN (
							        SELECT ar.Id, COUNT(*) TotalApprovalRejected FROM ApprovalRequestGroupMember argm 
							        JOIN ApprovalRequest ar ON argm.ApprovalRequestId = ar.Id
							        WHERE argm.Status = 3
							        GROUP BY ar.Id
						        ) ta3 ON ar.Id = ta3.Id
						        WHERE {subQuery}
					          ) sub";

            return qry;
        }
        #endregion
    }
}
