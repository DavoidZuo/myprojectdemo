using Aspose.Words;
using Aspose.Words.Drawing;
using Common;
using DataService;
using JSAJ.Core.Common;
using JSAJ.Core.Controllers;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels;

namespace JSAJNanJing.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class ProjectCheckController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        private readonly ILogger<MenusController> _logger;
        private readonly JssanjianmanagerContext _context;
        private readonly string _wordTemplte;
        private JwtSettings settings;
        private readonly string _buildWords;
        public ProjectCheckController(IWebHostEnvironment environment, ILogger<MenusController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            settings = options.Value;
            _wordTemplte = environment.WebRootPath + Path.DirectorySeparatorChar + "doc" + Path.DirectorySeparatorChar;
            _buildWords = environment.WebRootPath + Path.DirectorySeparatorChar + "BuildPdf" + Path.DirectorySeparatorChar;
        }



        /// <summary>
        /// css-检查信息
        /// </summary>
        /// <param name="belongedTo">项目机构编号</param>
        /// <param name="recordNumber">备案号</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<object>> GetCheckInfo(string belongedTo, string recordNumber)
        {
            try
            {
                CheckInfoViewModel model = new CheckInfoViewModel();
                //说明企业下有这个项目则可以查看这个项目的危大工程
                var projectOverview = await _context.ProjectOverview.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                if (projectOverview == null)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "项目不存在");
                }
                var proSupervisionPlan = await _context.ProSupervisionPlan.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                var cityCivilizationApply = await _context.CityCivilizationApply.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                var standardizedApplyRec = await _context.StandardizedApplyRec.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).OrderByDescending(x => x.ApplyDate).FirstOrDefaultAsync();
                var starBasicInfo = await _context.StarBasicInfo.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).OrderByDescending(x => x.ApplyTime).FirstOrDefaultAsync();
                var suspendRecord = await _context.SuspendRecord.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                var supervisionInformed = await _context.SupervisionInformed.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).OrderByDescending(x => x.Id).FirstOrDefaultAsync();


                //安监抽查/安监巡查次数
                var dailyInspect = await _context.DailyInspect.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber && !string.IsNullOrEmpty(x.Tzsno) && x.TreatmentType > 0).ToListAsync();
                ////标准化月评数据-旧的
                //var secInspectList = await _context.SecInspectList.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).ToListAsync();

                //标准化月评数据-新表
                var secInspectList = await _context.MonthlyChecklist.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber && x.IsComplete == 1).ToListAsync();


                model.RecordDate = projectOverview.RecordDate == null ? "无" : Convert.ToDateTime(projectOverview.RecordDate).ToString("yyyy-MM-dd");
                model.InformDateTime = (supervisionInformed == null || supervisionInformed.InformDateTime == null) ? "无" : Convert.ToDateTime(supervisionInformed.InformDateTime).ToString("yyyy-MM-dd");
                model.FactCompletionDate = projectOverview.FactCompletionDate == null ? "无" : Convert.ToDateTime(projectOverview.FactCompletionDate).ToString("yyyy-MM-dd");
                model.SurpPlanDate = (proSupervisionPlan == null || proSupervisionPlan.MakeTime == null) ? "无" : Convert.ToDateTime(proSupervisionPlan.MakeTime).ToString("yyyy-MM-dd");
                model.CityCivilizationApplyTime = (cityCivilizationApply == null || cityCivilizationApply.ApplyTime == null) ? "无" : Convert.ToDateTime(cityCivilizationApply.ApplyTime).ToString("yyyy-MM-dd");
                model.MunicipalCivilizationDate = (cityCivilizationApply == null || cityCivilizationApply.ApprovePassDate == null) ? "无" : Convert.ToDateTime(cityCivilizationApply.ApprovePassDate).ToString("yyyy-MM-dd");
                model.ExamResults = cityCivilizationApply == null ? "" : cityCivilizationApply.ExamResults;//评定结果
                model.ApplyDate = (standardizedApplyRec == null || standardizedApplyRec.ApplyDate == null) ? "无" : Convert.ToDateTime(standardizedApplyRec.ApplyDate).ToString("yyyy-MM-dd");
                model.ReportDate = (standardizedApplyRec == null || standardizedApplyRec.PassEvaluationDate == null) ? "无" : Convert.ToDateTime(standardizedApplyRec.PassEvaluationDate).ToString("yyyy-MM-dd");
                model.SuperOrgCompletion = standardizedApplyRec == null ? "" : standardizedApplyRec.SuperOrgCompletion;//安监站结果
                model.StarBasicApplyTime = (starBasicInfo == null || starBasicInfo.ApplyTime == null) ? "无" : Convert.ToDateTime(starBasicInfo.ApplyTime).ToString("yyyy-MM-dd");
                model.BatchName = starBasicInfo == null ? "" : starBasicInfo.BatchName;//申报批次
                model.StarLevel = starBasicInfo == null ? "" : starBasicInfo.StarLevel;//星级              
                model.SecurityAuditPasDate = (suspendRecord == null || suspendRecord.SecurityAuditPasDate == null) ? "无" : Convert.ToDateTime(suspendRecord.SecurityAuditPasDate).ToString("yyyy-MM-dd");
                model.RestoreAuditPasDate = (suspendRecord == null || suspendRecord.RestoreAuditPasDate == null) ? "无" : Convert.ToDateTime(suspendRecord.RestoreAuditPasDate).ToString("yyyy-MM-dd");

                model.PlanCheckTimes = proSupervisionPlan == null ? "0" : proSupervisionPlan.PlanCheckTimes;
                model.DailyInspectCount = dailyInspect == null ? 0 : dailyInspect.Count();
                model.SecInspectCount = secInspectList == null ? 0 : secInspectList.Count();
                //最近一次安监巡查检查日期
                model.NearCheckData = (secInspectList == null || secInspectList.Count == 0) ? "无" : Convert.ToDateTime(secInspectList.OrderByDescending(x => x.CheckDate).FirstOrDefault().CheckDate).ToString("yyyy-MM-dd");


                //按省标检查
                var shengBiaoCheck = await _context.AppDailyInspect
                               .Where(w => w.RecordNumber == recordNumber && w.BelongedTo == belongedTo && w.Tzsno != null).ToListAsync();
                //按项目内部检查ProEntInspectList
                var projectInspect = await _context.ProEntInspectList
                               .Where(w => w.RecordNumber == recordNumber && w.BelongedTo == belongedTo && w.DeleteMark == 0).ToListAsync();
                ////按JGJ59检查-旧
                //var jgj59Check = await _context.Jgj59secInspectList
                //               .Where(w => w.RecordNumber == recordNumber && w.BelongedTo == belongedTo).ToListAsync();
                //按JGJ59检查
                var jgj59Check = await _context.JGJ59Checklist
                               .Where(w => w.RecordNumber == recordNumber && w.BelongedTo == belongedTo).ToListAsync();


                //按扬尘检查RaiseDustSecInspectList
                var raiseCheck = await _context.RaiseDustSecInspectList
                               .Where(w => w.RecordNumber == recordNumber && w.BelongedTo == belongedTo).ToListAsync();

                int zichaCount = shengBiaoCheck.Where(x => x.InspectDept == "项目自查").Count()
                    + projectInspect.Where(x => x.InspectDept == "项目自查").Count() +
                    +jgj59Check.Where(x => x.InspectDept == "项目自查").Count() + raiseCheck.Where(x => x.InspectDept == "项目自查").Count();

                int qiyecheckCount = shengBiaoCheck.Where(x => x.InspectDept == "企业检查").Count()
                    + projectInspect.Where(x => x.InspectDept == "企业检查").Count() +
                    +jgj59Check.Where(x => x.InspectDept == "企业检查").Count()
                    + raiseCheck.Where(x => x.InspectDept == "企业检查").Count();

                model.ProjectSelfCheckCount = zichaCount; //项目自查次数
                model.EntCheckCount = qiyecheckCount;//企业检查次数
                model.InterviewCount = 0;//约谈次数
                model.AccidentCount = 0;//约谈次数

                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, model);
            }
            catch (Exception ex)
            {
                _logger.LogError("备案号跟踪--检查信息ProjectCheckController/GetCheckInfo：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// 下载施工许可证
        /// </summary>
        /// <param name="belongedTo">机构编号</param>
        /// <param name="recordNumber">备案号</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<object>> GetConstructionPermit(string recordNumber)
        {
            try
            {
                ReceiveDataServicePortTypeClient client = new ReceiveDataServicePortTypeClient();
                string result = await client.getSGXKZByAjbmAsync("AJ320000", "es34^HGt23498", recordNumber);
                if (result == "<?xml version=\"1.0\" encoding=\"GB2312\"?><body></body>")
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "当前许可证暂未发放");
                }
                DataTable dtSGXK = SecurityManage.ConvertXMLToDataSet(result).Tables[0];
                if (dtSGXK == null)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "当前许可证暂未发放");
                }
                else if (dtSGXK.Rows.Count > 0)
                {
                    string sgxkNo = "";
                    string sgxkpdf = "";
                    string sgxkzhText = "";
                    //林元征  防止该字段不存在导致报错
                    if (dtSGXK.Rows[0].Table.Columns.Contains("sgxkzh"))
                    {
                        sgxkNo = SecurityManage.DecodeBase64(Encoding.GetEncoding("GB2312"), dtSGXK.Rows[0]["sgxkzh"].ToString());

                    }
                    if (dtSGXK.Rows[0].Table.Columns.Contains("sgxkpdf"))
                    {
                        sgxkpdf = SecurityManage.DecodeBase64(Encoding.GetEncoding("GB2312"), dtSGXK.Rows[0]["sgxkpdf"].ToString());
                    }
                    if (dtSGXK.Rows[0].Table.Columns.Contains("sgxkzh_Text"))
                    {
                        sgxkzhText = SecurityManage.DecodeBase64(Encoding.GetEncoding("GB2312"), dtSGXK.Rows[0]["sgxkzh_Text"].ToString());
                    }
                    if (sgxkpdf.Trim() == "")
                    {
                        return ResponseViewModel<object>.Create(Status.FAIL, "当前许可证暂未发放");

                    }
                    else
                    {
                        var newName = recordNumber + "CanDelete_SGXKZ.pdf";

                        Util.FileDownSave(sgxkpdf, _buildWords + newName);
                        return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, Util.GetBaseUrl(Request) + "BuildPdf/" + newName);
                        //return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, sgxkpdf);
                    }
                }
                return ResponseViewModel<object>.Create(Status.FAIL, "当前许可证暂未发放");
            }
            catch (Exception ex)
            {
                _logger.LogError("下载施工许可证ProjectCheckController/GetConstructionPermit：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 下载监督告知
        /// </summary>
        /// <param name="belongedTo">机构编号</param>
        /// <param name="recordNumber">备案号</param>
        /// <param name="superviseInformTime">监督告知日期</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<object>> GetSuperviseInform(string belongedTo, string recordNumber)
        {

            try
            {
                if (string.IsNullOrEmpty(belongedTo) || string.IsNullOrEmpty(recordNumber))
                    return ResponseViewModel<object>.Create(Status.FAIL, "参数错误");
                var jianduGaozhi = await (from a in _context.SupervisionInformed
                                          join b in _context.ProjectOverview
                                          on new { a.BelongedTo, a.RecordNumber } equals new { b.BelongedTo, b.RecordNumber }
                                          join c in _context.CityZone
                                          on b.BelongedTo equals c.BelongedTo
                                          into t1
                                          from c1 in t1.DefaultIfEmpty()
                                          where b.BelongedTo == belongedTo
                                          && b.RecordNumber == recordNumber
                                          select new
                                          {
                                              BelongedTo = b.BelongedTo,
                                              RecordNumber = b.RecordNumber,
                                              ProjectName = b.ProjectName,
                                              ProjectAddress = b.ProjectAddress,
                                              SuperOrganName = c1.SuperOrganName,
                                              SYear = a.InformDateTime == null ? "" : ((DateTime)a.InformDateTime).Year.ToString(),
                                              SMonth = a.InformDateTime == null ? "" : Convert.ToDateTime(a.InformDateTime).Month.ToString(),
                                              SDay = a.InformDateTime == null ? "" : Convert.ToDateTime(a.InformDateTime).Day.ToString(),

                                          }).ToListAsync();
                if (jianduGaozhi == null || jianduGaozhi.Count == 0)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "该项目还未生成监督告知书");
                }
                var unitList = await _context.ProjectEntSnapshot.Where(y => y.BelongedTo == belongedTo && y.RecordNumber == recordNumber && y.MainUnit == "是").ToListAsync();
                string webRootPath = _wordTemplte + "anquanjiandubaogaoNew.doc";

                Aspose.Words.Document doc = new Aspose.Words.Document(webRootPath);
                DocumentBuilder builder = new DocumentBuilder(doc);
                //公章
                var query = await _context.OfficialSealInfos
                    .Where(w => w.BelongedTo == belongedTo && w.DeleteMark == 0 && w.IsEnable == "0")
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                if (query != null && !string.IsNullOrWhiteSpace(query.IsDailyInspection))
                {
                    //添加电子章

                    if (doc.Range.Bookmarks["PO_OfficialSeal"] != null)
                    {
                        builder.MoveToParagraph(1, 0);



                        builder.MoveToBookmark("PO_OfficialSeal");// 定位到书签去
                        var bate = Util.GetUrlMemoryStream(query.Url);
                        //builder.InsertImage(bate,150,150);
                        builder.InsertImage(bate,
                            RelativeHorizontalPosition.Margin,
                            300,
                            RelativeVerticalPosition.Margin,
                            500,
                            100,
                            100,
                            WrapType.None);
                    }

                }
                DataTable jianduGaozhidt = Util.ListToDataTable(jianduGaozhi);

                if (jianduGaozhidt != null && jianduGaozhidt.Rows.Count > 0)
                {
                    for (int i = 0; i < jianduGaozhidt.Columns.Count; i++)
                    {
                        string fieldName = jianduGaozhidt.Columns[i].ToString();// 字段名                           
                        if (jianduGaozhidt.Rows.Count > 0)
                        {
                            if (doc.Range.Bookmarks["PO_" + fieldName + ""] != null)
                            {
                                Bookmark mark = doc.Range.Bookmarks["PO_" + fieldName + ""];
                                mark.Text = jianduGaozhidt.Columns[i].Table.Rows[0][i].ToString();
                            }
                        }
                    }
                }
                #region 五方单位信息
                if (unitList.Count > 0)
                {
                    var jsdw = unitList.Where(x => x.EnterpriseType == "建设单位").OrderByDescending(x => x.Id).FirstOrDefault();
                    var kcdw = unitList.Where(x => x.EnterpriseType == "勘察单位").OrderByDescending(x => x.Id).FirstOrDefault();
                    var sjdw = unitList.Where(x => x.EnterpriseType == "设计单位").OrderByDescending(x => x.Id).FirstOrDefault();
                    var sgdw = unitList.Where(x => x.EnterpriseType == "施工单位").OrderByDescending(x => x.Id).FirstOrDefault();
                    var jldw = unitList.Where(x => x.EnterpriseType == "监理单位").OrderByDescending(x => x.Id).FirstOrDefault();
                    //建设单位
                    if (jsdw != null && doc.Range.Bookmarks["PO_JSDW"] != null)
                    {
                        Bookmark mark = doc.Range.Bookmarks["PO_JSDW"];
                        mark.Text = jsdw.EnterpriseName;
                    }
                    //勘察单位
                    if (kcdw != null && doc.Range.Bookmarks["PO_KCDW"] != null)
                    {
                        Bookmark mark = doc.Range.Bookmarks["PO_KCDW"];
                        mark.Text = kcdw.EnterpriseName;
                    }
                    //设计单位
                    if (sjdw != null && doc.Range.Bookmarks["PO_SJDW"] != null)
                    {
                        Bookmark mark = doc.Range.Bookmarks["PO_SJDW"];
                        mark.Text = sjdw.EnterpriseName;
                    }
                    //施工单位
                    if (sgdw != null && doc.Range.Bookmarks["PO_SGDW"] != null)
                    {
                        Bookmark mark = doc.Range.Bookmarks["PO_SGDW"];
                        mark.Text = sgdw.EnterpriseName;
                    }
                    //监理单位
                    if (jldw != null && doc.Range.Bookmarks["PO_JLDW"] != null)
                    {
                        Bookmark mark = doc.Range.Bookmarks["PO_JLDW"];
                        mark.Text = jldw.EnterpriseName;
                    }
                }
                #endregion

                //var personList= _context.ProjectPersonSnapshot.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).ToListAsync();

                var newName = belongedTo + "_" + recordNumber + "_JDBG.pdf";
                var newName1 = belongedTo + "_" + recordNumber + "_JDBG.docx";
                doc.Save(_buildWords + newName, SaveFormat.Pdf);
                doc.Save(_buildWords + newName1, SaveFormat.Docx);
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, Util.GetBaseUrl(Request) + "BuildPdf/" + newName);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取监督告知：" + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }
    }
}
