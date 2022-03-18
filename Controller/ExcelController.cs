using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Common.DataUtil;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using ViewModels;

namespace JSAJ.Core.Controllers.Excel
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ExcelController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        private readonly ILogger<MenusController> _logger;
        private readonly JssanjianmanagerContext _context;
        private readonly string _wordTemplte;
        private JwtSettings settings;
        private readonly string _buildWords;
        public ExcelController(IWebHostEnvironment environment, ILogger<MenusController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options, IOptions<OssFileSetting> oss)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            settings = options.Value;
            _wordTemplte = environment.WebRootPath + "\\doc\\";
            _buildWords = environment.WebRootPath + "\\BuildPdf\\";
        }



        /// <summary>
        /// 检测所机构导入
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<object>> ImportPostTestingInstituteInfo([FromForm]IFormCollection files)
        {
            DateTime nowTime = DateTime.Now;
            List<TestingInstituteInfo> jixieList = new List<TestingInstituteInfo>();
            IFormFile excelfile = files.Files[0];
            string isXls = System.IO.Path.GetExtension(excelfile.FileName).ToString().ToLower();//System.IO.Path.GetExtension获得文件的扩展名
            if (isXls != ".xls" && isXls != ".xlsx")
            {
                return ResponseViewModel<object>.Create(Status.FAIL, "只可以选择Excel文件！");
            }
            string path = _environment.WebRootPath + Path.DirectorySeparatorChar + "ImportPostData";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fileName = "ImportProject_TestingInstituteInfo.xls";
            var fullPath = path + "\\" + fileName;
            FileInfo file = new FileInfo(Path.Combine(path, fileName));
            using (FileStream fss = new FileStream(file.ToString(), FileMode.Create))
            {
                excelfile.CopyTo(fss);
                fss.Flush();
            }

            //IWorkbook wk = null;
            string extension = System.IO.Path.GetExtension(fullPath);
            FileStream fs = new FileStream(fullPath, FileMode.Open);
            HSSFWorkbook wk = new HSSFWorkbook(fs);
            //if (extension.Equals(".xls"))
            //{
            //    //把xls文件中的数据写入wk中
            //    wk = new HSSFWorkbook(fs);
            //}
            //else
            //{
            //    //把xlsx文件中的数据写入wk中
            //    wk = new XSSFWorkbook(fs);
            //}
            fs.Close();
            for (int a = 0; a < wk.NumberOfSheets; a++)
            {   //循环所有的工作表
                ISheet sheet = wk.GetSheetAt(a);
                DataTable dt = NPOIHelper.ExcelToDataTable(sheet, true);
                DataRow[] dr = dt.Select();            //定义一个DataRow数组
                //获取列名
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    try
                    {
                        string DRV = "";
                        for (int j = 0; j < dr[i].ItemArray.Length; j++)
                        {
                            DRV = DRV + dr[i].ItemArray[j].ToString();
                        }
                        if (DRV.Trim() == "")//用于判断整行数据是否为空
                            break;

                        string jigouName = dr[i]["单位名称"].ToString().Trim();
                        string dizhi = dr[i]["单位地址"].ToString().Trim();
                        string youzhengbianma = dr[i]["邮政编码"].ToString().Trim();
                        string lianxiren = dr[i]["联系人"].ToString().Trim();
                        string lianxiDianhua = dr[i]["联系电话"].ToString().Trim();
                        string faren = dr[i]["法人代表"].ToString().Trim();
                        string jishufuzeren = dr[i]["技术负责人"].ToString().Trim();
                        string jishuzhicheng = dr[i]["技术职称"].ToString().Trim();
                        string zhucehao = dr[i]["企业法人营业执照注册号"].ToString().Trim();
                        string jiancefanwei = dr[i]["检测范围"].ToString().Trim();
                        string zhengshuhaoma = dr[i]["证书号码"].ToString().Trim();
                        string fazhengriqi = dr[i]["发证日期"].ToString().Trim();
                        string youxiaoqi = dr[i]["有效期"].ToString().Replace(" ", "").Trim();
                        string qushubianhao = dr[i]["区属编号"].ToString().Trim();
                        string citycode = dr[i]["区属编码"].ToString().Trim();

                        TestingInstituteInfo danwei = new TestingInstituteInfo();
                        danwei.TestingInstituteInfoId = SecurityManage.GuidUpper();

                        danwei.BelongedTo = qushubianhao;
                        danwei.MechanismNumber = zhengshuhaoma;
                        danwei.MechanismName = jigouName;
                        danwei.Address = dizhi;
                        danwei.PostalCode = youzhengbianma;
                        danwei.TechnicalDirector = jishufuzeren;
                        danwei.TechnicalTitle = jishuzhicheng;
                        danwei.LegalPerson = faren;
                        danwei.LegalPersonPhone = "";
                        danwei.OrganizationCode = zhucehao;
                        danwei.InspectionScope = jiancefanwei;
                        DateTime beginTime = DateTime.ParseExact(youxiaoqi.Substring(0, 8), "yyyyMMdd", Thread.CurrentThread.CurrentCulture);
                        DateTime endDateTime = DateTime.ParseExact(youxiaoqi.Substring(8, 8), "yyyyMMdd", Thread.CurrentThread.CurrentCulture);
                        danwei.BeginDateTime = beginTime;
                        danwei.EndDateTime = endDateTime;
                        danwei.DeleteTag = 0;
                        danwei.CreateDate = nowTime;
                        danwei.UpdateDate = nowTime;
                        danwei.CityCode = citycode;
                        jixieList.Add(danwei);
                    }
                    catch (Exception ex)
                    {

                        return ResponseViewModel<object>.Create(Status.ERROR, "导入失败");
                    }

                }

            }


            await _context.TestingInstituteInfo.AddRangeAsync(jixieList);
            await _context.SaveChangesAsync();
            return ResponseViewModel<object>.Create(Status.SUCCESS, "导入成功");
        }

        /// <summary>
        /// 项目管理导入
        /// </summary>
        /// <param name="excelfile"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<object>> ImportPost([FromForm]IFormCollection files)
        {
            try
            {
                var userId = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                if (string.IsNullOrWhiteSpace(belongedTo))
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "仅限安监管理人员操作！");
                }
                var errorList = new List<string>();

                IFormFile excelfile = files.Files[0];
                string isXls = System.IO.Path.GetExtension(excelfile.FileName).ToString().ToLower();//System.IO.Path.GetExtension获得文件的扩展名
                if (isXls != ".xls" && isXls != ".xlsx")
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "只可以选择Excel文件！");
                }

                string path = _environment.WebRootPath + Path.DirectorySeparatorChar + "ImportPostData";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = "ImportProject_" + belongedTo + DateTime.Now.ToString("yyyyMMddHHmm") + ".xls";
                var fullPath = path + "\\" + fileName;
                FileInfo file = new FileInfo(Path.Combine(path, fileName));
                using (FileStream fss = new FileStream(file.ToString(), FileMode.Create))
                {
                    excelfile.CopyTo(fss);
                    fss.Flush();
                }
                IWorkbook wk = null;
                string extension = System.IO.Path.GetExtension(fullPath);

                FileStream fs = new FileStream(fullPath, FileMode.Open);
                if (extension.Equals(".xls"))
                {
                    //把xls文件中的数据写入wk中
                    wk = new HSSFWorkbook(fs);
                }
                else
                {
                    //把xlsx文件中的数据写入wk中
                    wk = new XSSFWorkbook(fs);
                }
                fs.Close();
                //读取当前表数据
                ISheet sheet = wk.GetSheetAt(0);
                IRow row = sheet.GetRow(0);

                //DataTable dt = NPOIHelper.ReadExcelDataTable(sheet, 2, null);

                DataTable dt = NPOIHelper.ExcelToDataTable(sheet, true);

                DataRow[] dr = dt.Select();            //定义一个DataRow数组
                int rowsnum = dt.Rows.Count;
                string result = string.Empty; //返回结果
                string resultCardId = string.Empty; // 身份证返回结果
                int count = 0;
                if (rowsnum == 0)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "导入数据为空！");
                }
                else
                {
                    var projectListData = await _context.ProjectOverview.Where(x => x.BelongedTo == belongedTo).ToListAsync();
                    var keshiList = await _context.SupervisionDepartment.Where(x => x.BelongedTo == belongedTo && x.DeleteMark == 0).ToListAsync();
                    var entProInfoListData = await _context.EntProInfo.Where(x => x.BelongedTo == belongedTo).ToListAsync();
                    var projectEntSnapshotListData = await _context.ProjectEntSnapshot.Where(x => x.BelongedTo == belongedTo).ToListAsync();
                    var projectPersonSnapshotListData = await _context.ProjectPersonSnapshot.Where(x => x.BelongedTo == belongedTo).ToListAsync();



                    List<ProjectOverview> addProject = new List<ProjectOverview>();
                    List<EntProInfo> addEntProInfo = new List<EntProInfo>();
                    List<ProjectEntSnapshot> addProjectEntSnapshot = new List<ProjectEntSnapshot>();
                    List<ProjectPersonSnapshot> addProjectPersonSnapshot = new List<ProjectPersonSnapshot>();

                    for (int i = 0; i < dr.Length; i++)
                    {
                        string DRV = "";
                        for (int j = 0; j < dr[i].ItemArray.Length; j++)
                        {
                            DRV = DRV + dr[i].ItemArray[j].ToString();
                        }
                        if (DRV.Trim() == "")//用于判断整行数据是否为空
                            break;
                        //前面除了你需要在建立一个“upfiles”的文件夹外，其他的都不用管了，你只需要通过下面的方式获取Excel的值，然后再将这些值用你的方式去插入到数据库里面
                        string xuhao = dr[i]["序号"].ToString();
                        string RecordNumber = dr[i]["备案号"].ToString();
                        RecordNumber = RecordNumber.Replace(",", "")
                     .Replace("，", "")
                     .Replace(" ", "")
                     .Replace(".", "")
                     .Replace("。", "")
                     .Replace("!", "")
                     .Replace("！", "")
                     .Replace("?", "")
                     .Replace("？", "")
                     .Replace(":", "")
                     .Replace("：", "")
                     .Replace(";", "")
                     .Replace("；", "")
                     .Replace("～", "")
                     .Replace("——", "")
                     .Replace("--", "")
                     .Replace("\\", "")
                     .Replace("#", "")
                     .Replace("$", "");
                        RecordNumber = RecordNumber.Trim();
                        string ProjectName = dr[i]["工程名称"].ToString();
                        string ProjectAddress = dr[i]["工程地点"].ToString();
                        string RecordDate = dr[i]["备案日期"].ToString();//lufn 2016-02-26
                        string ProjectTarget = dr[i]["安全文明施工目标"].ToString();//lufn 2016-02-26
                        string ProjectAcreage = dr[i]["建筑面积（平方米）"].ToString();
                        string ProjectPrice = dr[i]["工程造价（万元）"].ToString();
                        string ProjectCategory = dr[i]["工程类别"].ToString();
                        string ProjectStartDateTimne = dr[i]["开工日期"].ToString();
                        string ProjectEndDateTimne = dr[i]["竣工日期"].ToString();
                        string ProjectHierarchy = dr[i]["结构/层次"].ToString();
                        string BelongsDepartments = dr[i]["工程所属科室"].ToString().Trim();
                        string GroupName = dr[i]["组别"].ToString();

                        string SGEntCode = dr[i]["施工单位组织机构代码"].ToString().Trim();
                        string ShiGongDW = dr[i]["施工单位名称"].ToString().Trim();
                        string LeaderManager = dr[i]["项目经理"].ToString().Trim();
                        string LeaderManagerCard = dr[i]["项目经理身份证"].ToString().Trim();
                        string LeaderManagerTel = dr[i]["项目经理电话"].ToString().Trim();
                        string LeaderManagerZhengShu = dr[i]["项目经理安全考核证号"].ToString().Trim();
                        string anquanyuan1 = dr[i]["安全员1"].ToString().Trim();
                        string anquanyuan2 = dr[i]["安全员2"].ToString().Trim();
                        string anquanyuan3 = dr[i]["安全员3"].ToString().Trim();

                        string JSEntCode = dr[i]["建设单位组织机构代码"].ToString().Trim();
                        string JianSheDW = dr[i]["建设单位名称"].ToString().Trim();
                        string ProManager = dr[i]["项目负责人"].ToString().Trim();
                        string ProManagerTel = dr[i]["项目负责人手机号"].ToString().Trim();

                        string JLEntCode = dr[i]["监理单位组织机构代码"].ToString().Trim();
                        string JianLiDW = dr[i]["监理单位名称"].ToString().Trim();
                        string ProJLManager = dr[i]["项目总监"].ToString().Trim();
                        string ProJLManagerCard = dr[i]["项目总监身份证"].ToString().Trim();
                        string ProJLManagerTel = dr[i]["项目总监手机号"].ToString().Trim();
                        string ProJLManagerZhengShu = dr[i]["项目总监证书号"].ToString().Trim();
                        string JLGCS1 = dr[i]["监理工程师1"].ToString();
                        string JLGCS2 = dr[i]["监理工程师2"].ToString();
                        string JLGCS3 = dr[i]["监理工程师3"].ToString();
                        string IsTrue = NPOIHelper.CheckIsTrue(RecordNumber, ProjectName, ProjectAddress, ProjectAcreage, ProjectPrice, ProjectCategory,
                            ProjectStartDateTimne, ProjectEndDateTimne, BelongsDepartments, ShiGongDW, LeaderManager, LeaderManagerCard, LeaderManagerTel, LeaderManagerZhengShu,
                            anquanyuan1, JianSheDW, ProManager, ProManagerTel, JianLiDW, ProJLManager, ProJLManagerCard, ProJLManagerTel, ProJLManagerZhengShu, JLGCS1,
                            SGEntCode, JSEntCode, JLEntCode);
                        // 验证数据的准确性
                        if (IsTrue.Trim() != "、")
                        {
                            resultCardId += "项目备案号[" + RecordNumber + "]，项目名称[" + ProjectName + "]中:" + IsTrue + "";
                        }
                        else
                        {
                            string[] arry = { "施工单位", "建设单位", "监理单位" };
                            string[] arryDw = { ShiGongDW, JianSheDW, JianLiDW };
                            string[] arryEntCode = { SGEntCode, JSEntCode, JLEntCode };
                            var isCunzai = projectListData.Where(x => x.RecordNumber == RecordNumber).ToList();
                            if (isCunzai.Count > 0)
                            {
                                return ResponseViewModel<object>.Create(Status.ERROR, "项目【" + RecordNumber + "】已存在系统中不可导入");
                            }
                            var keshi = keshiList.Where(x => x.Name == BelongsDepartments).OrderByDescending(x => x.Id).FirstOrDefault();

                            // 插入工程概况表
                            ProjectOverview project1 = new ProjectOverview
                            {
                                BelongedTo = belongedTo,
                                RecordNumber = RecordNumber,
                                ProjectName = ProjectName,
                                ProjectAddress = ProjectAddress,
                                ProjectTarget = ProjectTarget,
                                RecordDate = (string.IsNullOrEmpty(RecordDate) ? Convert.ToDateTime(ProjectStartDateTimne) : Convert.ToDateTime(RecordDate)),
                                ProjectAcreage = Convert.ToDouble(ProjectAcreage),
                                ProjectPrice = Convert.ToDouble(ProjectPrice),
                                ProjectCategory = ProjectCategory,
                                ProBigCategory = ProjectCategory,
                                ProjectStartDateTimne = Convert.ToDateTime(ProjectStartDateTimne),
                                ProjectEndDateTimne = Convert.ToDateTime(ProjectEndDateTimne),
                                ProjectHierarchy = ProjectHierarchy,
                                BelongsDepartments = BelongsDepartments,
                                SupervisionDepartmentId = keshi == null ? null : keshi.SupervisionDepartmentId,
                                GroupName = GroupName,
                                IsPrint = 0,
                                Remark = "新省系统-项目导入"
                            };
                            addProject.Add(project1);


                            var isCunzaientProInfo1 = entProInfoListData.Where(x => x.BelongedTo == belongedTo
                            && x.RecordNumber == RecordNumber && x.ConstructionEntCode == SGEntCode).ToList();
                            if (isCunzaientProInfo1.Count == 0)
                            {
                                EntProInfo entProInfo1 = new EntProInfo
                                {
                                    RecordNumber = RecordNumber,
                                    BelongedTo = belongedTo,
                                    ProjectName = ProjectName,
                                    ConstructionEntCode = SGEntCode,
                                    ConstructionUnit = ShiGongDW,
                                    EntType = "施工单位",
                                    Remarks = "新省系统-项目导入"
                                };
                                addEntProInfo.Add(entProInfo1);
                            }

                            var isCunzaientProInfo2 = entProInfoListData.Where(x => x.BelongedTo == belongedTo
                           && x.RecordNumber == RecordNumber && x.ConstructionEntCode == JSEntCode).ToList();
                            if (isCunzaientProInfo2.Count == 0)
                            {
                                EntProInfo entProInfo2 = new EntProInfo
                                {
                                    RecordNumber = RecordNumber,
                                    BelongedTo = belongedTo,
                                    ProjectName = ProjectName,
                                    ConstructionEntCode = JSEntCode,
                                    ConstructionUnit = JianSheDW,
                                    EntType = "建设单位",
                                    Remarks = "新省系统-项目导入"
                                };
                                addEntProInfo.Add(entProInfo2);
                            }
                            if (result != "0")
                            {
                                count += 1;
                            }
                            for (int j = 0; j < arry.Length; j++)
                            {
                                if (!string.IsNullOrEmpty(arryDw[j]) && !string.IsNullOrEmpty(arryEntCode[j]))
                                {
                                    var isCunzai2 = projectEntSnapshotListData.Where(x => x.RecordNumber == RecordNumber && x.OrganizationCode == arryEntCode[j]).ToList();
                                    if (isCunzai2.Count == 0)
                                    {
                                        ProjectEntSnapshot danwei = new ProjectEntSnapshot();
                                        danwei.BelongedTo = belongedTo;
                                        danwei.RecordNumber = RecordNumber;
                                        danwei.ProjectName = ProjectName;
                                        danwei.ProjectAddress = ProjectAddress;
                                        danwei.EnterpriseType = arry[j];
                                        danwei.MainUnit = "是";
                                        danwei.Remark = "新省系统-项目导入";
                                        danwei.EnterpriseName = arryDw[j];
                                        danwei.OrganizationCode = arryEntCode[j];
                                        addProjectEntSnapshot.Add(danwei);
                                    }

                                }


                            }
                            string[] arryPersonType = { "项目经理", "项目负责人", "项目总监" };
                            string[] arryPersonName = { LeaderManager, ProManager, ProJLManager };
                            string[] arryPersonTel = { LeaderManagerTel, ProManagerTel, ProJLManagerTel };
                            string[] arryPersonCard = { LeaderManagerCard, "", ProJLManagerCard };
                            string[] CertificateNumber = { LeaderManagerZhengShu, "", ProJLManagerZhengShu };
                            for (int k = 0; k < arryPersonType.Length; k++)
                            {
                                var isCunzai3 = projectPersonSnapshotListData.Where(x => 
                                x.RecordNumber == RecordNumber && 
                                ((x.PersonPhone == arryPersonTel[k] && x.EnterpriseType == "建设单位")
                                || (x.PersonCardId == arryPersonCard[k] && x.EnterpriseType != "建设单位"))
                                  && x.EntCode == arryEntCode[k]).ToList();
                                if (isCunzai3.Count == 0)
                                {
                                    if (!string.IsNullOrEmpty(arryPersonType[k])
                                        && !string.IsNullOrEmpty(arryPersonName[k])
                                        && !string.IsNullOrEmpty(arryPersonTel[k]))
                                    {
                                        ProjectPersonSnapshot renyuan1 = new ProjectPersonSnapshot();
                                        renyuan1.BelongedTo = belongedTo;
                                        renyuan1.RecordNumber = RecordNumber;
                                        renyuan1.EnterpriseType = arry[k];
                                        renyuan1.EnterpriseName = arryDw[k].Trim();
                                        renyuan1.EntCode = arryEntCode[k].Trim();
                                        renyuan1.PersonType = arryPersonType[k];
                                        renyuan1.PersonName = arryPersonName[k];
                                        renyuan1.PersonCardId = arryPersonCard[k].Trim();
                                        renyuan1.CertificateNumber = CertificateNumber[k];
                                        renyuan1.PersonPhone = arryPersonTel[k];
                                        renyuan1.Remark = "新省系统-项目导入";
                                        addProjectPersonSnapshot.Add(renyuan1);
                                    }
                                }

                            }
                            string[] aqy = { anquanyuan1, anquanyuan2, anquanyuan3 };
                            for (int m = 0; m < aqy.Length; m++)
                            {
                                if (!String.IsNullOrEmpty(aqy[m]))
                                {
                                    var isCunzai3 = projectPersonSnapshotListData.Where(x => x.RecordNumber == RecordNumber
                              && x.PersonName == aqy[m] && x.EntCode == arryEntCode[0]).ToList();
                                    if (isCunzai3.Count == 0)
                                    {
                                        ProjectPersonSnapshot renyuan2 = new ProjectPersonSnapshot();

                                        renyuan2.RecordNumber = RecordNumber;
                                        renyuan2.EnterpriseType = arry[0];
                                        renyuan2.EnterpriseName = arryDw[0].Trim();
                                        renyuan2.EntCode = arryEntCode[0].Trim();
                                        renyuan2.PersonType = "安全员";
                                        renyuan2.PersonName = aqy[m];
                                        renyuan2.BelongedTo = belongedTo;
                                        renyuan2.PersonPhone = "";
                                        renyuan2.Remark = "安全员" + (m + 1);
                                        renyuan2.Remark = "新省系统-项目导入";
                                        addProjectPersonSnapshot.Add(renyuan2);
                                    }
                                }

                            }
                            string[] jlgcs = { JLGCS1, JLGCS2, JLGCS3 };
                            for (int m = 0; m < aqy.Length; m++)
                            {
                                if (!String.IsNullOrEmpty(jlgcs[m]))
                                {
                                    var isCunzai4 = projectPersonSnapshotListData.Where(x => x.RecordNumber == RecordNumber
                            && x.PersonName == jlgcs[m] && x.EntCode == arryEntCode[m]).ToList();
                                    if (isCunzai4.Count == 0)
                                    {
                                        ProjectPersonSnapshot renyuan3 = new ProjectPersonSnapshot();

                                        renyuan3.RecordNumber = RecordNumber;
                                        renyuan3.EnterpriseType = arry[2];
                                        renyuan3.EnterpriseName = arryDw[m];
                                        renyuan3.EntCode = arryEntCode[m];
                                        renyuan3.PersonType = "监理工程师";
                                        renyuan3.PersonName = jlgcs[m];
                                        renyuan3.BelongedTo = belongedTo;
                                        renyuan3.PersonPhone = "";
                                        renyuan3.Remark = "监理工程师" + (m + 1);
                                        renyuan3.Remark = "新省系统-项目导入";
                                        addProjectPersonSnapshot.Add(renyuan3);
                                    }
                                }

                            }
                        }
                    }
                    if (count > 0)
                    {
                        result = "成功导入" + count + "条数据！" + resultCardId;
                    }
                    else
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "导入失败" + resultCardId);
                    }
                    await _context.ProjectOverview.AddRangeAsync(addProject);
                    await _context.EntProInfo.AddRangeAsync(addEntProInfo);
                    await _context.ProjectEntSnapshot.AddRangeAsync(addProjectEntSnapshot);
                    await _context.ProjectPersonSnapshot.AddRangeAsync(addProjectPersonSnapshot);
                    await _context.SaveChangesAsync();
                    return ResponseViewModel<object>.Create(Status.SUCCESS, result);

                }
            }
            catch (Exception e)
            {
                _logger.LogError("批量导入：" + e.Message + e.StackTrace, e);
                return ResponseViewModel<object>.Create(Status.ERROR, "导入失败,请检查您导入的数据格式");
            }
        }

        /// <summary>
        ///查看导入的项目列表
        /// </summary>
        /// <param name="pageIndex">起始页</param>
        /// <param name="pageSize">条数</param>
        /// <param name="projectName">工程名称</param>
        /// <param name="state">状态</param> 
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<ResponseViewModel<List<ExcelSuperviseListViemModel>>> GetImportPostProjectInfo(int pageIndex, int pageSize
            , string recordNumber
            , string projectName, string enterpriseName, string xiangmuJIngli)
        {
            try
            {
                //登录人belongedto
                var belongedto = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                //登录人科室id
                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;

                if (string.IsNullOrWhiteSpace(belongedto))
                {
                    return ResponseViewModel<List<ExcelSuperviseListViemModel>>.Create(Status.FAIL, "仅限安监管理人员操作");
                }



                var projectEntSnapshotListData = await _context.ProjectEntSnapshot.Where(x =>
                x.BelongedTo == belongedto && x.MainUnit == "是" && x.EnterpriseType == "施工单位").ToListAsync();


                var projectPersonSnapshotListData = await _context.ProjectPersonSnapshot.Where(x =>
                x.BelongedTo == belongedto && x.EnterpriseType == "施工单位" && x.PersonType == "项目经理").ToListAsync();

                var jianduGaozhi = _context.ProjectOverview.Where(a => a.RecordDate != null && a.BelongedTo == belongedto && a.Remark == "新省系统-项目导入")
                                   .Select(a => new ExcelSuperviseListViemModel
                                   {
                                       Id = a.Id,
                                       BelongedTo = a.BelongedTo,
                                       SupervisionDepartmentId = a.SupervisionDepartmentId,
                                       RecordNumber = a.RecordNumber,
                                       ProjectName = a.ProjectName,
                                       //EnterpriseName = b1.EnterpriseName,
                                       //OrganizationCode = b1.OrganizationCode,
                                       //XiangMuJingLi = b2.PersonName,
                                       //XiangMuJingLiTel = b2.PersonPhone,
                                       ProjectAddress = a.ProjectAddress,
                                       RecordDate = a.RecordDate,
                                       ProjectStartDateTimne = a.ProjectStartDateTimne,
                                       ProjectEndDateTimne = a.ProjectEndDateTimne,
                                       ShiGongXuekeNo = a.ShiGongXuekeNo//施工许可证
                                   });
                if (!string.IsNullOrWhiteSpace(belongedto))
                {
                    jianduGaozhi = jianduGaozhi.Where(x => x.BelongedTo == belongedto);
                }
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    jianduGaozhi = jianduGaozhi.Where(x => x.RecordNumber.Contains(recordNumber));
                }

                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    jianduGaozhi = jianduGaozhi.Where(x => x.ProjectName.Contains(projectName));
                }
                var resultList = await jianduGaozhi.Distinct().ToListAsync();
                int totalCount = resultList.Count();
                resultList = resultList.OrderByDescending(t => t.InformDateTime).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                resultList.ForEach(s =>
                {
                    var danwei = projectEntSnapshotListData.Where(k => k.RecordNumber == s.RecordNumber && k.EnterpriseType == "施工单位").FirstOrDefault();
                    if (danwei != null)
                    {
                        s.EnterpriseName = danwei.EnterpriseName;
                        s.OrganizationCode = danwei.OrganizationCode;
                        var jingliUser = projectPersonSnapshotListData.Where(k => k.RecordNumber == s.RecordNumber && k.EntCode == s.OrganizationCode).FirstOrDefault();

                        if (jingliUser != null)
                        {
                            s.XiangMuJingLi = jingliUser.PersonName;
                            s.XiangMuJingLiTel = jingliUser.PersonPhone;

                        }


                    }


                });
                return ResponseViewModel<List<ExcelSuperviseListViemModel>>.Create(Status.SUCCESS, Message.SUCCESS, resultList, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError("项目导入列表" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<ExcelSuperviseListViemModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        ///删除导入的项目数据
        /// </summary>
        /// <param name="pageIndex">起始页</param>
        /// <param name="pageSize">条数</param>
        /// <param name="projectName">工程名称</param>
        /// <param name="state">状态</param> 
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<ResponseViewModel<string>> DelprojectInfo(List<int> projectIds)
        {
            try
            {
                //登录人belongedto
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var projectListData = await _context.ProjectOverview.Where(x => x.BelongedTo == belongedTo
                && projectIds.Contains(x.Id)).ToListAsync();
                if (projectListData.Count == 0)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "无导入的数据");


                }
                var beianhaoList = projectListData.Select(x => x.RecordNumber).ToList();
                string[] arryDanwei = { "施工单位", "建设单位", "监理单位" };
                var entProInfoListData = await _context.EntProInfo.Where(x => x.BelongedTo == belongedTo
                && beianhaoList.Contains(x.RecordNumber) && x.Remarks == "新省系统-项目导入").ToListAsync();
                var projectEntSnapshotListData = await _context.ProjectEntSnapshot.Where(x => x.BelongedTo == belongedTo
                && beianhaoList.Contains(x.RecordNumber) && arryDanwei.Contains(x.EnterpriseType)
                && x.MainUnit == "是" && x.Remark == "新省系统-项目导入").ToListAsync();

                if (projectEntSnapshotListData.Count > 0)
                {
                    var projectPersonSnapshotListData = await _context.ProjectPersonSnapshot.Where(x => x.BelongedTo == belongedTo
                    && beianhaoList.Contains(x.RecordNumber) &&
                    projectEntSnapshotListData.Select(p => p.OrganizationCode).Contains(x.EntCode)
                    && x.Remark == "新省系统-项目导入").ToListAsync();
                    _context.ProjectPersonSnapshot.RemoveRange(projectPersonSnapshotListData);
                }
                _context.ProjectOverview.RemoveRange(projectListData);
                _context.EntProInfo.RemoveRange(entProInfoListData);
                _context.ProjectEntSnapshot.RemoveRange(projectEntSnapshotListData);

                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("项目导入列表" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

    }
}