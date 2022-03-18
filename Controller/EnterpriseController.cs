using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Models;
using JSAJ.Core.Models.LargeMachinery;
using JSAJ.Core.ViewModels;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViewModels;

namespace JSAJNanJing.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class EnterpriseController : ControllerBase
    {
        private readonly JssanjianmanagerContext _context;

        private readonly ILogger<EnterpriseController> _logger;
        private readonly IWebHostEnvironment _environment;


        public EnterpriseController(ILogger<EnterpriseController> logger, JssanjianmanagerContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }


        /// <summary>
        /// 备案号跟踪(项目基本信息)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<EssentialInformation>> SearchEssentialInformation(string recordNumber, string belongedTo)
        {
            try
            {
                //查项目基本信息
                var query = await _context.ProjectOverview
                    .Where(s => s.RecordNumber == recordNumber && s.BelongedTo == belongedTo)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();


                EssentialInformation information = new EssentialInformation();
                if (query == null)
                {
                    return ResponseViewModel<EssentialInformation>.Create(Status.SUCCESS, Message.SUCCESS, information);
                }
                var blongtoName = await _context.SupervisionDepartment
                    .Where(s => s.SupervisionDepartmentId == query.SupervisionDepartmentId)
                    .Select(k => k.Name)
                    .FirstOrDefaultAsync();
                information.RecordNumber = query.RecordNumber;
                information.ProjectName = query.ProjectName;
                information.ProjectAddress = query.ProjectAddress;
                information.ItemNumber = query.ItemNumber;
                information.ProBigCategory = query.ProBigCategory;
                information.ProjectAcreage = query.ProjectAcreage;
                information.ProjectPrice = query.ProjectPrice;
                information.ProjectHierarchy = query.ProjectHierarchy;
                information.ProjectTarget = query.ProjectTarget;
                information.LongitudeCoordinate = query.LongitudeCoordinate;
                information.LatitudeCoordinate = query.LatitudeCoordinate;
                information.BelongsDepartments = blongtoName;
                //查询项目3方公司
                var query1 = await _context.ProjectEntSnapshot
                    .Where(s => s.RecordNumber == recordNumber
                    && s.BelongedTo == belongedTo
                    && s.EnterpriseType == "建设单位" && s.MainUnit == "是")
                    .FirstOrDefaultAsync();
                if (query1 != null)
                { ///建设单位
                    var buildUnit = await _context.ProjectPersonSnapshot
                        .Where(s => s.RecordNumber == recordNumber
                        && s.BelongedTo == belongedTo
                        && s.EnterpriseName == query1.EnterpriseName
                        && s.PersonType == "项目负责人")
                        .FirstOrDefaultAsync();
                    information.JsEntName = query1.EnterpriseName;
                    if (buildUnit != null)
                    {
                        information.JsperName = buildUnit.PersonName;
                        information.JsperPhone = buildUnit.PersonPhone;
                    }
                }



                var query2 = await _context.ProjectEntSnapshot
                    .Where(s => s.RecordNumber == recordNumber
                    && s.BelongedTo == belongedTo
                    && s.EnterpriseType == "监理单位" && s.MainUnit == "是")
                    .FirstOrDefaultAsync();

                if (query2 != null)
                {
                    //监理单位
                    var supervisorUnit = await _context.ProjectPersonSnapshot
                    .Where(s => s.RecordNumber == recordNumber
                    && s.BelongedTo == belongedTo
                    && s.EnterpriseName == query2.EnterpriseName
                    && s.PersonType == "项目总监")
                    .FirstOrDefaultAsync();
                    information.JlEntName = query2.EnterpriseName;
                    if (supervisorUnit != null)
                    {
                        information.JlperName = supervisorUnit.PersonName;
                        information.JlperPhone = supervisorUnit.PersonPhone;
                    }
                }



                var query3 = await _context.ProjectEntSnapshot
                    .Where(s => s.RecordNumber == recordNumber
                    && s.BelongedTo == belongedTo
                    && s.EnterpriseType == "施工单位" && s.MainUnit == "是")
                    .FirstOrDefaultAsync();

                if (query3 != null)
                {
                    //施工单位
                    var constructionUnit = await _context.ProjectPersonSnapshot
                         .Where(s => s.RecordNumber == recordNumber
                         && s.BelongedTo == belongedTo
                         && s.EnterpriseName == query3.EnterpriseName
                         && s.PersonType == "项目经理")
                         .FirstOrDefaultAsync();

                    //安全员
                    information.AqName = await _context.ProjectPersonSnapshot
                     .Where(s => s.RecordNumber == recordNumber
                     && s.BelongedTo == belongedTo
                     && s.EnterpriseName == query3.EnterpriseName
                     && s.PersonType == "安全员")
                     .Select(k => k.PersonName)
                     .ToListAsync();
                    information.SgEntName = query3.EnterpriseName;
                    if (constructionUnit != null)
                    {
                        information.SgperName = constructionUnit.PersonName;
                        information.SgperPhone = constructionUnit.PersonPhone;
                    }
                }

                var query4 = await _context.ProjectEntSnapshot
                   .Where(s => s.RecordNumber == recordNumber
                   && s.BelongedTo == belongedTo
                   && s.EnterpriseType == "设计单位" && s.MainUnit == "是")
                   .FirstOrDefaultAsync();
                if (query4 != null)
                {
                    //设计单位
                    var designUnit = await _context.ProjectPersonSnapshot
                       .Where(s => s.RecordNumber == recordNumber
                       && s.BelongedTo == belongedTo
                       && s.EnterpriseName == query4.EnterpriseName
                       && s.PersonType == "项目负责人")
                       .FirstOrDefaultAsync();
                    information.SjEntName = query4.EnterpriseName;

                    if (designUnit != null)
                    {
                        information.SjperName = designUnit.PersonName;
                        information.SjperPhone = designUnit.PersonPhone;
                    }
                }

                var query5 = await _context.ProjectEntSnapshot
                   .Where(s => s.RecordNumber == recordNumber
                   && s.BelongedTo == belongedTo
                   && s.EnterpriseType == "勘察单位" && s.MainUnit == "是")
                   .FirstOrDefaultAsync();
                if (query5 != null)
                {
                    //勘察单位
                    var surveyUnit = await _context.ProjectPersonSnapshot
                        .Where(s => s.RecordNumber == recordNumber
                        && s.BelongedTo == belongedTo
                        && s.EnterpriseName == query5.EnterpriseName
                        && s.PersonType == "项目负责人")
                        .FirstOrDefaultAsync();
                    information.KcEntName = query5.EnterpriseName;
                    if (surveyUnit != null)
                    {
                        information.KcperName = surveyUnit.PersonName;
                        information.KcperPhone = surveyUnit.PersonPhone;
                    }
                }






                return ResponseViewModel<EssentialInformation>.Create(Status.SUCCESS, Message.SUCCESS, information);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取备案号跟踪(项目基本信息)错误：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<EssentialInformation>.Create(Status.ERROR, Message.ERROR);
            }

        }

        public ResponseViewModel<string> UpLoadZiZhiZhengShu([FromForm] IFormCollection iform)
        {
            try
            {

                var url = Util.UploadFileToServer(iform.Files[0], _environment, Request, "QualificationCertificate");

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, url);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取配置文件信息：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        [HttpGet]
        [Authorize]
        /// <summary>
        /// 获取安监站菜单
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseViewModel<List<node>>> SearchSuperOrganList()
        {
            try
            {
                //解析Token
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//企业表Id                                                          
                var entCode = await _context.EntRegisterInfoMag
                    .Where(s => s.EntRegisterInfoMagId == tokenId).FirstOrDefaultAsync();//企业备案号
                if (entCode == null)
                {
                    return ResponseViewModel<List<node>>.Create(3, Message.WARN);
                }

                var obj = from A in _context.ProjectOverview
                          join B in _context.CityZone on A.BelongedTo equals B.BelongedTo
                          into t1
                          from r in t1.DefaultIfEmpty()
                          join C in _context.ProjectPersonSnapshot on A.RecordNumber equals C.RecordNumber
                          where A.BelongedTo == C.BelongedTo && C.EntCode == entCode.EntCode && C.PersonType == "项目经理"
                          select new EntProjectInformation
                          {
                              SuperOrganName = r.SuperOrganName,
                              RecordNumber = A.RecordNumber,
                              BelongedTo = A.BelongedTo,
                              ProjectName = A.ProjectName,
                              ProjectAddress = A.ProjectAddress,
                              ConstructionPermitState = A.ShiGongXuekeNo,
                              PersonName = C.PersonName,
                              CityCode = r.CityCode,
                              ParentCityCode = r.ParentCityCode

                          };

                int queryCount1 = obj.Count();

                //获取安监站信息
                var superOrgan = (from a in obj
                                  group a by new { a.SuperOrganName, a.ParentCityCode, a.BelongedTo, a.CityCode }
                            into g
                                  select new
                                  { g.Key.SuperOrganName, g.Key.ParentCityCode, g.Key.BelongedTo, g.Key.CityCode })
                                  .ToList();


                var aa = await _context.CityZone.Where(s =>
                superOrgan.Select(x => x.ParentCityCode).Contains(s.CityCode)).Distinct().ToListAsync();//获取安监站的父级集合（市）

                var shengs = await _context.CityZone
                    .Where(s => aa.Select(x => x.ParentCityCode).Contains(s.CityCode)).ToListAsync();//获取省

                List<node> list = new List<node>();
                shengs.ForEach(x =>
                {
                    node sheng = new node();
                    sheng.Value = x.SuperOrganName;
                    sheng.Key = x.BelongedTo;
                    sheng.Title = x.SuperOrganName;
                    sheng.CityCode = x.CityCode;
                    list.Add(sheng);

                });

                aa.ForEach(x =>
                {
                    var shengobject = list.Find(k => k.CityCode == x.ParentCityCode);
                    if (shengobject.Children == null)
                    {
                        List<node> list1 = new List<node>();
                        node shiNode = new node();
                        shiNode.Value = x.SuperOrganName;

                        shiNode.Key = x.BelongedTo;

                        shiNode.Title = x.SuperOrganName;

                        shiNode.CityCode = x.CityCode;

                        shiNode.Children = (superOrgan.Where(
                            k2 => k2.ParentCityCode == shiNode.CityCode).Select(k2 => new node
                            {
                                Value = k2.SuperOrganName,

                                Key = k2.BelongedTo,

                                Title = k2.SuperOrganName,

                                CityCode = k2.CityCode

                            })).ToList();
                        list1.Add(shiNode);
                        shengobject.Children = list1;
                    }
                    else
                    {
                        var shiObj = shengobject.Children.Find(k1 => k1.Key == x.BelongedTo);
                        if (shiObj == null)
                        {
                            node shiNode = new node();
                            shiNode.Value = x.SuperOrganName;

                            shiNode.Key = x.BelongedTo;

                            shiNode.Title = x.SuperOrganName;

                            shiNode.CityCode = x.CityCode;

                            shiNode.Children = (superOrgan.Where(
                                k2 => k2.ParentCityCode == shiNode.CityCode).Select(k2 => new node
                                {
                                    Value = k2.SuperOrganName,

                                    Key = k2.BelongedTo,

                                    Title = k2.SuperOrganName,

                                    CityCode = k2.CityCode

                                })).ToList();
                            shengobject.Children.Add(shiNode);

                        }
                    }



                });
                return ResponseViewModel<List<node>>.Create(Status.SUCCESS, Message.SUCCESS, list);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取安监站错误：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<List<node>>.Create(Status.ERROR, Message.ERROR);
            }


        }



        /// <summary>
        /// 获取企业项目信息
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="superOrganName"></param>
        /// <param name="projectName"></param>
        /// <param name="projectAddress"></param>
        /// <param name="searchType">0全部 1未竣工  2已竣工</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<EntProjectInformation>>> SearchProjectInformation(int pageIndex, int pageSize, string superOrganName, string projectName, string projectAddress, int searchType = 1)
        {
            try
            {
                DateTime now = DateTime.Now;
                //解析Token
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//企业表Id   "98285BE9472A4C768E4F9F9BFEA542D3"

                var entCode = await _context.EntRegisterInfoMag
                    .Where(s => s.EntRegisterInfoMagId == tokenId).FirstOrDefaultAsync();//企业备案号
                if (entCode == null || string.IsNullOrEmpty(entCode.EntCode))
                {
                    return ResponseViewModel<List<EntProjectInformation>>.Create(Status.WARN, Message.WARN);
                }

                var obj = from A in _context.ProjectEntSnapshot
                          join b in _context.ProjectOverview on new { A.BelongedTo, A.RecordNumber } equals new { b.BelongedTo, b.RecordNumber }
                          join C in _context.CityZone on b.BelongedTo equals C.BelongedTo
                          where A.OrganizationCode == entCode.EntCode && A.EnterpriseType == "施工单位"
                          && b.RecordDate < now
                          select new EntProjectInformation
                          {
                              SerialNumber = A.Id,
                              SuperOrganName = C.SuperOrganName,
                              RecordNumber = b.RecordNumber,
                              BelongedTo = b.BelongedTo,
                              ProjectName = b.ProjectName,
                              ProjectAddress = b.ProjectAddress,
                              CityCode = C.CityCode,
                              FactCompletionDate = b.FactCompletionDate,
                              ParentCityCode = C.ParentCityCode
                          };

                if (searchType == 1)
                {
                    obj = obj.Where(a => a.FactCompletionDate == null);
                }
                else if (searchType == 2)
                {
                    obj = obj.Where(a => a.FactCompletionDate != null);
                }
                //判断搜索条件
                IEnumerable<EntProjectInformation> projectIEn = obj;
                //判断搜索条件
                if (!string.IsNullOrWhiteSpace(superOrganName))
                {
                    if (superOrganName.Length > 0 && superOrganName.Length < 8 && superOrganName != "江苏省监督站")
                    {
                        var query = await _context.CityZone
                            .Where(s => s.SuperOrganName == superOrganName)
                            .FirstOrDefaultAsync();

                        projectIEn = projectIEn
                            .Where(s => !string.IsNullOrEmpty(s.ParentCityCode) && s.ParentCityCode.Contains(query.CityCode));
                    }
                    else
                    {

                        projectIEn = projectIEn
                            .Where(s => !string.IsNullOrEmpty(s.SuperOrganName) && s.SuperOrganName.Contains(superOrganName));
                    }
                }


                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    projectIEn = projectIEn
                        .Where(s => !string.IsNullOrEmpty(s.ProjectName) && s.ProjectName.Contains(projectName));
                }
                if (!string.IsNullOrWhiteSpace(projectAddress))
                {
                    projectIEn = projectIEn
                        .Where(s => !string.IsNullOrEmpty(s.ProjectAddress) && s.ProjectAddress.Contains(projectAddress));
                }

                int queryCount = projectIEn.Count();

                var resuft = projectIEn
                    .OrderByDescending(t => t.RecordNumber)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                var query2 = await _context.ProjectPersonSnapshot
                    .Where(s => s.EnterpriseType == "施工单位" && s.EntCode == entCode.EntCode && s.PersonType == "项目经理")
                    .Select(k => new { k.PersonName, k.RecordNumber })
                    .ToListAsync();
                resuft.ForEach(x =>
                {
                    x.PersonName = query2.Where(s => s.RecordNumber == x.RecordNumber).Select(k => k.PersonName).FirstOrDefault();
                });
                return ResponseViewModel<List<EntProjectInformation>>.Create(Status.SUCCESS, Message.SUCCESS, resuft, queryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError("企业项目信息错误：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<List<EntProjectInformation>>.Create(Status.ERROR, Message.ERROR);
            }
        }
        public async Task<ResponseViewModel<string>> SubimtZiZhi(string url, string entName)
        {
            try
            {
                var entCode = User.FindFirst(nameof(ClaimTypeEnum.EntCode))?.Value;
                if (entCode == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                var query = await _context.InstallationQualifications
                    .Where(s => s.Entcode == entCode).FirstOrDefaultAsync();
                var guid = SecurityManage.GuidUpper();
                if (query == null)
                {
                    InstallationQualification mode = new InstallationQualification();
                    mode.CreateDate = DateTime.Now;
                    mode.UpdateDate = DateTime.Now;
                    mode.DeleteMark = 0;
                    mode.Entcode = entCode;
                    mode.EntName = entName;
                    mode.State = "1";
                    mode.LetterOfCommitmentId = guid;
                    LetterOfCommitment mode1 = new LetterOfCommitment();
                    mode1.LetterOfCommitmentId = guid;
                    mode1.Type = "安装资质图片";
                    mode1.Url = url;
                    _context.InstallationQualifications.AddRange(mode);
                    _context.LetterOfCommitments.AddRange(mode1);
                    _context.SaveChanges();
                    return ResponseViewModel<string>.Create(Status.SUCCESS, "提交成功！");
                }
                else
                {
                    if (query.State == "0" || query.State == "3")
                    {
                        query.State = "1";
                        var query2 = await _context.LetterOfCommitments
                            .Where(s => s.LetterOfCommitmentId == query.LetterOfCommitmentId)
                            .FirstOrDefaultAsync();
                        query2.Url = url;
                        query.UpdateDate = DateTime.Now;
                        _context.InstallationQualifications.UpdateRange(query);
                        _context.LetterOfCommitments.UpdateRange(query2);
                        _context.SaveChanges();
                        return ResponseViewModel<string>.Create(Status.SUCCESS, "提交成功！");
                    }
                    else if (query.State == "1")
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "等待审核中，请勿重复申请！");
                    }
                    else
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "已审核通过，请勿重复申请！");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("提交：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 获取备案信息(项目基本信息)1
        /// </summary>
        /// <param name="recordNumber"></param>
        /// <param name="belongedTo"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<ProjectInformation>> SearchRecordOne(string recordNumber, string belongedTo)
        {
            ProjectInformation information = new ProjectInformation();
            try
            {
                // 查项目基本信息
                var query = await _context.ProjectOverview.Where
                    (s => s.RecordNumber == recordNumber && s.BelongedTo == belongedTo)
                    .FirstOrDefaultAsync();
                //查询安监站
                var query1 = await _context.CityZone
                    .Where(s => s.BelongedTo == belongedTo)
                    .FirstOrDefaultAsync();
                if (query == null)
                {
                    return ResponseViewModel<ProjectInformation>.Create(Status.SUCCESS, Message.SUCCESS, information);
                }
                information.RecordNumber = query.RecordNumber;
                information.ItemNumber = query.ItemNumber;
                information.ProjectName = query.ProjectName;
                information.ProjectAddress = query.ProjectAddress;
                information.ProBigCategory = query.ProBigCategory;
                information.ProjectAcreage = query.ProjectAcreage;
                information.ProjectPrice = query.ProjectPrice;
                information.ProjectHierarchy = query.ProjectHierarchy;
                information.ProjectTarget = query.ProjectTarget;
                information.SubmitDate = query.SubmitDate;
                information.Contacts = query.Contacts;
                information.ContactsTel = query.ContactsTel;
                information.SuperOrganName = query1.SuperOrganName;
                information.RemoteMonitorAccord = query.RemoteMonitorAccord;
                information.ProjectStartDateTimne = query.ProjectStartDateTimne;
                information.ProjectEndDateTimne = query.ProjectEndDateTimne;
                information.Uuid = query.Uuid;
            }
            catch (Exception ex)
            {
                _logger.LogError("获取查项目基本信息错误：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<ProjectInformation>.Create(Status.ERROR, Message.ERROR);
            }
            return ResponseViewModel<ProjectInformation>.Create(Status.SUCCESS, Message.SUCCESS, information);
        }

        /// <summary>
        /// 获取备案信息(项目基本信息)2
        /// </summary>
        /// <param name="recordNumber"></param>
        /// <param name="belongedTo"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<ThreeEntInformation>> SearchRecordTwo(string recordNumber, string belongedTo)
        {
            ThreeEntInformation information = new ThreeEntInformation();
            try
            {

                //查询项目5方单位及人源信息
                //建设单位
                var buildUnit = await _context.ProjectEntSnapshot
                    .Where(s => s.RecordNumber == recordNumber
                    && s.BelongedTo == belongedTo
                    && s.EnterpriseType == "建设单位" && s.MainUnit == "是")
                    .ToListAsync();
                List<JsEntInfo> JsList = new List<JsEntInfo>();
                if (buildUnit != null)
                {
                    buildUnit.ForEach(x =>
                    {
                        JsEntInfo mode = new JsEntInfo();
                        mode.BuildUnitName = x.EnterpriseName;
                        mode.BuildUnitFaRen = x.FaRen;
                        mode.BuildUnitOrganizationCode = x.OrganizationCode;
                        var buildPreson1 = _context.ProjectPersonSnapshot
                     .Where(s => s.RecordNumber == recordNumber
                     && s.BelongedTo == belongedTo
                     && s.EnterpriseType == "建设单位"
                     && s.EnterpriseName == x.EnterpriseName
                     && (s.PersonType == "项目负责人" || s.PersonType == "现场负责人"))
                     .Select(k => new { k.PersonType, k.PersonName, k.PersonPhone, k.PersonCardId })
                     .ToList();

                        mode.JsProjectLeader = buildPreson1
                        .Where(s => s.PersonType == "项目负责人").Select(k => k.PersonName).FirstOrDefault();
                        mode.JsProjectLeaderPhone = buildPreson1
                    .Where(s => s.PersonType == "项目负责人").Select(k => k.PersonPhone).FirstOrDefault();


                        mode.JsXcLeader = buildPreson1
                        .Where(s => s.PersonType == "现场负责人").Select(k => k.PersonName).FirstOrDefault();
                        mode.JsXcLeaderCode = buildPreson1
                    .Where(s => s.PersonType == "现场负责人").Select(k => k.PersonCardId).FirstOrDefault();
                        mode.JsXcLeaderPhone = buildPreson1
                        .Where(s => s.PersonType == "现场负责人").Select(k => k.PersonPhone).FirstOrDefault();

                        JsList.Add(mode);

                    });



                }




                //勘察单位
                var supervisorUnit = await _context.ProjectEntSnapshot
                    .Where(s => s.RecordNumber == recordNumber
                    && s.BelongedTo == belongedTo
                    && s.EnterpriseType == "勘察单位" && s.MainUnit == "是")
                    .ToListAsync();
                List<KcEntInfo> KcList = new List<KcEntInfo>();
                if (supervisorUnit != null)
                {
                    supervisorUnit.ForEach(x =>
                    {
                        KcEntInfo mode = new KcEntInfo();
                        mode.SurveyUnitName = x.EnterpriseName;
                        mode.SurveyUnitFaRen = x.FaRen;
                        mode.SurveyUnitOrganizationCode = x.OrganizationCode;
                        //勘察单位人员信息
                        var supervisorPreson1 = _context.ProjectPersonSnapshot
                            .Where(s => s.RecordNumber == recordNumber
                            && s.BelongedTo == belongedTo
                            && s.EnterpriseType == "勘察单位"
                            && s.EnterpriseName == x.EnterpriseName
                            && (s.PersonType == "项目负责人" || s.PersonType == "现场负责人"))
                            .Select(k => new { k.PersonType, k.PersonName, k.PersonPhone })
                            .ToList();

                        mode.KcProjectLeader = supervisorPreson1
                        .Where(s => s.PersonType == "项目负责人").Select(k => k.PersonName).FirstOrDefault();
                        mode.KcProjectLeaderPhone = supervisorPreson1
                        .Where(s => s.PersonType == "项目负责人").Select(k => k.PersonPhone).FirstOrDefault();


                        mode.KcXcLeader = supervisorPreson1
                        .Where(s => s.PersonType == "现场负责人").Select(k => k.PersonName).FirstOrDefault();
                        mode.KcXcLeaderPhone = supervisorPreson1
                            .Where(s => s.PersonType == "现场负责人").Select(k => k.PersonPhone).FirstOrDefault();

                        KcList.Add(mode);

                    });



                }


                //设计单位
                var constructionUnit = await _context.ProjectEntSnapshot
                    .Where(s => s.RecordNumber == recordNumber
                    && s.BelongedTo == belongedTo
                    && s.EnterpriseType == "设计单位" && s.MainUnit == "是")
                    .ToListAsync();
                List<SjEntInfo> SjList = new List<SjEntInfo>();
                if (constructionUnit != null)
                {
                    constructionUnit.ForEach(x =>
                    {
                        SjEntInfo mode = new SjEntInfo();
                        mode.ConstructionUnitName = x.EnterpriseName;
                        mode.ConstructionUnitFaRen = x.FaRen;
                        mode.ConstructionUnitOrganizationCode = x.OrganizationCode;
                        //设计单位人员信息
                        var constructionPreson1 = _context.ProjectPersonSnapshot
                                .Where(s => s.RecordNumber == recordNumber
                                && s.BelongedTo == belongedTo
                                && s.EnterpriseType == "设计单位"
                                && s.EnterpriseName == x.EnterpriseName
                                && (s.PersonType == "项目负责人" || s.PersonType == "现场负责人"))
                                .Select(k => new { k.PersonType, k.PersonName, k.PersonPhone })
                                .ToList();

                        mode.SjProjectLeader = constructionPreson1
                        .Where(s => s.PersonType == "项目负责人").Select(k => k.PersonName).FirstOrDefault();

                        mode.SjProjectLeaderPhone = constructionPreson1
                    .Where(s => s.PersonType == "项目负责人").Select(k => k.PersonPhone).FirstOrDefault();

                        mode.SjXcLeader = constructionPreson1
                        .Where(s => s.PersonType == "现场负责人").Select(k => k.PersonName).FirstOrDefault();
                        mode.SjXcLeaderPhone = constructionPreson1
                    .Where(s => s.PersonType == "现场负责人").Select(k => k.PersonPhone).FirstOrDefault();

                        SjList.Add(mode);

                    });



                }

                information.JsEntInfos = JsList;
                information.kcEntInfos = KcList;
                information.SjEntInfos = SjList;


                return ResponseViewModel<ThreeEntInformation>.Create(Status.SUCCESS, Message.SUCCESS, information);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取单位及人源信息错误：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<ThreeEntInformation>.Create(Status.ERROR, Message.ERROR);
            }

        }

        /// 获取备案信息(项目基本信息)3
        /// </summary>
        /// <param name="recordNumber"></param>
        /// <param name="belongedTo"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<JlEntInformation>>> SearchRecordThree(string recordNumber, string belongedTo)
        {


            try
            {
                List<JlEntInformation> JlList = new List<JlEntInformation>();
                //监理单位
                var supervisionUnit = await _context.ProjectEntSnapshot
                    .Where(s => s.RecordNumber == recordNumber
                    && s.BelongedTo == belongedTo
                    && s.EnterpriseType == "监理单位" && s.MainUnit == "是")
                    .ToListAsync();
                supervisionUnit.ForEach(x =>
                {
                    JlEntInformation jlEntInformation = new JlEntInformation();
                    jlEntInformation.EntName = x.EnterpriseName;
                    jlEntInformation.PersonName = x.FaRen;
                    jlEntInformation.EntCode = x.OrganizationCode;
                    jlEntInformation.MainCredentialsLevel = x.MainQualificationLevel;
                    jlEntInformation.CertificateNo = x.CertificateNo;
                    //监理单位人员信息
                    var supervisionPreson1 = _context.ProjectPersonSnapshot
                         .Where(s => s.RecordNumber == recordNumber
                         && s.BelongedTo == belongedTo
                         && s.EnterpriseType == "监理单位"
                         && s.EnterpriseName == x.EnterpriseName
                         && (s.PersonType == "项目总监" || s.PersonType == "总监代表" || s.PersonType == "监理工程师"))
                         .ToList();

                    var supervisionPreson3 = supervisionPreson1
                    .Where(s => s.RecordNumber == recordNumber
                    && s.BelongedTo == belongedTo && s.PersonType == "监理工程师")
                    .ToList();

                    jlEntInformation.Projectdirector = supervisionPreson1
                    .Where(s => s.PersonType == "项目总监").Select(k => k.PersonName).FirstOrDefault();
                    jlEntInformation.IdCard = supervisionPreson1
                    .Where(s => s.PersonType == "项目总监").Select(k => k.PersonCardId).FirstOrDefault();
                    jlEntInformation.CertificateNumber = supervisionPreson1
                    .Where(s => s.PersonType == "项目总监").Select(k => k.CertificateNumber).FirstOrDefault();

                    jlEntInformation.PresonPhone = supervisionPreson1
                    .Where(s => s.PersonType == "项目总监").Select(k => k.PersonPhone).FirstOrDefault();



                    jlEntInformation.InspectorGener = supervisionPreson1
                    .Where(s => s.PersonType == "总监代表").Select(k => k.PersonName).FirstOrDefault();
                    jlEntInformation.IdCard1 = supervisionPreson1
                    .Where(s => s.PersonType == "总监代表").Select(k => k.PersonCardId).FirstOrDefault();

                    jlEntInformation.CertificateNumber1 = supervisionPreson1
                    .Where(s => s.PersonType == "总监代表").Select(k => k.CertificateNumber).FirstOrDefault();
                    jlEntInformation.PresonPhone1 = supervisionPreson1
                    .Where(s => s.PersonType == "总监代表").Select(k => k.PersonPhone).FirstOrDefault();


                    List<Preson> prelist = new List<Preson>();
                    supervisionPreson3.ForEach(x =>
                    {
                        Preson preson = new Preson();
                        preson.People = x.PersonName;
                        preson.IdCard = x.PersonCardId;
                        preson.CertificateNumber = x.CertificateNumber;
                        preson.PresonPhone = x.PersonPhone;
                        prelist.Add(preson);
                    });
                    jlEntInformation.Preson = prelist;
                    JlList.Add(jlEntInformation);
                });
                return ResponseViewModel<List<JlEntInformation>>.Create(Status.SUCCESS, Message.SUCCESS, JlList);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取监理单位及人源信息错误：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<List<JlEntInformation>>.Create(Status.ERROR, Message.ERROR);
            }

        }
        /// 获取备案信息(项目基本信息)4
        /// </summary>
        /// <param name="recordNumber"></param>
        /// <param name="belongedTo"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<SgEntInformation>>> SearchRecordFour(string recordNumber, string belongedTo)
        {


            try
            {
                List<SgEntInformation> SgList = new List<SgEntInformation>();
                //施工单位
                var constructionUnit = await _context.ProjectEntSnapshot
                    .Where(s => s.RecordNumber == recordNumber
                    && s.BelongedTo == belongedTo
                    && s.EnterpriseType == "施工单位" && s.MainUnit == "是")
                    .ToListAsync();

                constructionUnit.ForEach(x =>
                {
                    SgEntInformation sgEntInformation = new SgEntInformation();
                    var safetyProductionNo = _context.ElementsData
                   .Where(s => s.ZhiLiaoMingCheng == "施工单位安全生产许可证" && s.BelongedTo == belongedTo && s.RecordNumber == recordNumber)
                   .Select(k => k.ElementsContent)
                   .FirstOrDefault();

                    sgEntInformation.SafetyProductionNo = safetyProductionNo;
                    sgEntInformation.EntName = x.EnterpriseName;
                    sgEntInformation.EntCode = x.OrganizationCode;
                    sgEntInformation.MainCredentialsLevel = x.MainQualificationLevel;
                    sgEntInformation.CertificateNo = x.CertificateNo;
                    //施工单位人员信息
                    var constructionPreson1 = _context.ProjectPersonSnapshot
                        .Where(s => s.RecordNumber == recordNumber
                        && s.BelongedTo == belongedTo
                        && s.EnterpriseType == "施工单位"
                        && s.EnterpriseName == x.EnterpriseName
                        )
                        .ToList();
                    sgEntInformation.Projectmanager = constructionPreson1
                    .Where(s => s.PersonType == "项目经理").Select(k => k.PersonName).FirstOrDefault();

                    sgEntInformation.JlIdCard = constructionPreson1
                    .Where(s => s.PersonType == "项目经理").Select(k => k.PersonCardId).FirstOrDefault();

                    sgEntInformation.JlCertificateNumber = constructionPreson1
                     .Where(s => s.PersonType == "项目经理").Select(k => k.CertificateNumber).FirstOrDefault();

                    sgEntInformation.JlPresonPhone = constructionPreson1
                    .Where(s => s.PersonType == "项目经理").Select(k => k.PersonPhone).FirstOrDefault();

                    sgEntInformation.QualificationCertificateNo = constructionPreson1
                    .Where(s => s.PersonType == "项目经理").Select(k => k.QualificationCertificateNo).FirstOrDefault();

                    sgEntInformation.QualificationCertificateLevel = constructionPreson1
                    .Where(s => s.PersonType == "项目经理").Select(k => k.QualificationCertificateLevel).FirstOrDefault();

                    sgEntInformation.FaRen = constructionPreson1
                    .Where(s => (s.PersonType == "法人或委托负责人" || s.PersonType == "法人")).Select(k => k.PersonName).FirstOrDefault();

                    sgEntInformation.FaRenCertificateNumber = constructionPreson1
                    .Where(s => (s.PersonType == "法人或委托负责人" || s.PersonType == "法人")).Select(k => k.CertificateNumber).FirstOrDefault();
                    sgEntInformation.EntTechnicaldirector = constructionPreson1
                    .Where(s => s.PersonType == "企业技术负责人").Select(k => k.PersonName).FirstOrDefault();

                    sgEntInformation.EntTechnicaldirectorNumber = constructionPreson1
                    .Where(s => s.PersonType == "企业技术负责人").Select(k => k.CertificateNumber).FirstOrDefault();

                    sgEntInformation.Safetydirector = constructionPreson1
                    .Where(s => s.PersonType == "企业分管安全负责人").Select(k => k.PersonName).FirstOrDefault();

                    sgEntInformation.SafetydirectorNumber = constructionPreson1
                    .Where(s => s.PersonType == "企业分管安全负责人").Select(k => k.CertificateNumber).FirstOrDefault();

                    sgEntInformation.ProTechnicaldirector = constructionPreson1
                    .Where(s => s.PersonType == "项目技术负责人").Select(k => k.PersonName).FirstOrDefault();

                    sgEntInformation.ProTechnicaldirectorNumber = constructionPreson1
                    .Where(s => s.PersonType == "项目技术负责人").Select(k => k.CertificateNumber).FirstOrDefault();
                    var constructionPreson6 = constructionPreson1.Where(s => s.RecordNumber == recordNumber
                        && s.BelongedTo == belongedTo
                        && s.EnterpriseType == "施工单位"
                        && s.EnterpriseName == x.EnterpriseName
                        && s.PersonType == "安全员")
                        .ToList();
                    List<Preson> prelist = new List<Preson>();
                    if (constructionPreson6.Count > 0)
                    {
                        constructionPreson6.ForEach(x =>
                        {
                            Preson preson = new Preson();
                            preson.People = x.PersonName;
                            preson.IdCard = x.PersonCardId;
                            preson.CertificateNumber = x.CertificateNumber;
                            preson.PresonPhone = x.PersonPhone;
                            prelist.Add(preson);
                        });
                    }

                    sgEntInformation.Preson = prelist;
                    SgList.Add(sgEntInformation);
                });
                return ResponseViewModel<List<SgEntInformation>>.Create(Status.SUCCESS, Message.SUCCESS, SgList);

            }
            catch (Exception ex)
            {
                _logger.LogError("获取施工单位及人源信息错误：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<List<SgEntInformation>>.Create(Status.ERROR, Message.ERROR);
            }

        }


        /// 获取备案信息(项目基本信息)5
        /// </summary>
        /// <param name="recordNumber"></param>
        /// <param name="belongedTo"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<DeclaredInformation>>> SearchRecordFive(string recordNumber, string belongedTo)
        {
            List<DeclaredInformation> declaredInformation = new List<DeclaredInformation>();
            try
            {
                var query = await _context.ElementsData
                    .Where(s => s.RecordNumber == recordNumber && s.BelongedTo == belongedTo)
                    .AsNoTracking()
                    .ToListAsync();
                query.ForEach(x =>
                {
                    DeclaredInformation declared = new DeclaredInformation();
                    declared.SerialNumber = x.XuHao;

                    declared.DataName = x.ZhiLiaoMingCheng;

                    declared.ContractNumber = x.ElementsContent;
                    declaredInformation.Add(declared);
                });


            }
            catch (Exception ex)
            {
                _logger.LogError("获取合同资料信息错误：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<List<DeclaredInformation>>.Create(Status.ERROR, Message.ERROR);
            }

            return ResponseViewModel<List<DeclaredInformation>>.Create(Status.SUCCESS, Message.SUCCESS, declaredInformation);

        }
    }
}
