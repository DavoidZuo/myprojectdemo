using Common;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
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
    public class ProjectInfoStatisticsController : ControllerBase
    {

        private readonly JssanjianmanagerContext _context;
        private readonly ILogger<ProjectInfoStatisticsController> _logger;

        public ProjectInfoStatisticsController(ILogger<ProjectInfoStatisticsController> logger, JssanjianmanagerContext context)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 获取项目信息统计列表
        /// </summary>
        /// <param name="page">页数</param>
        /// <param name="size">数量</param>
        /// <param name="superOrganName">监督机构号</param>
        /// <param name="recordNumber">备案号</param>
        /// <param name="projectName">项目名称</param>
        /// <param name="entType">单位类型</param>
        /// <param name="entName">单位名称</param>
        /// <param name="projectType">工程类型</param>
        /// <param name="projectState">工程状态0:全部,1:在建,2:已竣工</param>
        /// <param name="beginTime">备案开始日期</param>
        /// <param name="endTime">备案结束日期</param>
        ///// <param name="sgName">施工单位名称</param>
        /// <param name="roleId">科室Id</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ProjectInfoStatisticsNewViewModel>>> GetProjectInfoStatisList(int pageIndex, int pageSize, string superOrganName = "",
            string recordNumber = "", string projectName = "", string entType = "", string entName = "",
            string projectType = "", int projectState = 0, string beginTime = "", string endTime = ""
            , string roleId = "", string sgName = "")
        {
            try
            {
                DateTime now = DateTime.Now;
                var belongedToLogin = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                string supervisionDepartmentId = roleId;
                string belongedTo = superOrganName;
                if (string.IsNullOrWhiteSpace(belongedToLogin))
                {
                    return ResponseViewModel<List<ProjectInfoStatisticsNewViewModel>>.Create(Status.FAIL, "仅限安监管理人员操作");
                }
                //直接根据施工单位搜索
                if (sgName != "")
                {
                    entType = "施工单位";
                    entName = sgName;
                }
                //如果没有选择监督机构默认用登录人BelongedTo筛选,江苏省的则直接默认查看全部
                if (string.IsNullOrWhiteSpace(belongedTo))
                {
                    belongedTo = belongedToLogin == null ? "" : belongedToLogin;
                }
                //因为条件不好给所以不管他是市级-多个，还是区级-一个，都封装成String集合来查询
                List<string> superOrganNameList = new List<string>();
                var clist = await _context.CityZone.Where(w => w.BelongedTo == belongedTo).FirstOrDefaultAsync();
                var supervisionDepartmentIdLogin = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//操作人科室

                if (!string.IsNullOrWhiteSpace(supervisionDepartmentIdLogin))
                {
                    //只查看当前登录科室的数据
                    supervisionDepartmentId = supervisionDepartmentIdLogin;
                }
                var proList = new List<ProjectInfoStatisticsNewViewModel>();
                var count = 0;

                //有无单位查询
                if (string.IsNullOrWhiteSpace(entType) || string.IsNullOrWhiteSpace(entName))
                {
                    //没有竣工状态也没有备案时间筛选
                    var proModel = _context.ProjectOverview.Where(p => p.RecordDate != null && p.RecordDate < now).Select(p => new ProjectInfoStatisticsNewViewModel
                    {
                        Id = p.Id,
                        BelongedTo = p.BelongedTo,
                        RecordNumber = p.RecordNumber,
                        RecordDate = p.RecordDate,
                        ProjectName = p.ProjectName,
                        ProjectAddress = p.ProjectAddress,
                        ShiGongXuekeNo = p.ShiGongXuekeNo,
                        SupervisionDepartmentId = p.SupervisionDepartmentId,
                        FactCompletionDate = p.FactCompletionDate,
                        ProjectCategory = p.ProjectCategory,
                        ProBigCategory = p.ProBigCategory,
                        Remark = p.Remark,
                        SuperOrganName = "",
                        SupervisionDepartmentName = "",
                        SGDW = "",
                        JSDW = "",
                        JLDW = "",
                        SGDWEntCode = "",
                        JSDWEntCode = "",
                        JLDWEntCode = "",
                        ProjectPrice = p.ProjectPrice,
                        ProjectAcreage = p.ProjectAcreage,
                        ProjectStartDateTimne = p.ProjectStartDateTimne,
                        ProjectEndDateTimne = p.ProjectEndDateTimne,
                        ProjectTarget = p.ProjectTarget,
                        ProjectState = p.FactCompletionDate == null ? "未竣工" : "已竣工",
                        YiChouChaCount = 0,
                        JiHuaChouChaCount = "0",
                        PersonCount = 0,
                    });
                    #region 竣工状态和时间筛选
                    if (projectState == 1)
                    {
                        //未竣工
                        proModel = proModel.Where(x => x.FactCompletionDate == null);
                        if (!string.IsNullOrWhiteSpace(beginTime))
                        {
                            var beginTimes = Convert.ToDateTime(beginTime);

                            proModel = proModel.Where(x => x.RecordDate >= beginTimes);
                        }
                        if (!string.IsNullOrWhiteSpace(endTime))
                        {
                            var endTimeS = Convert.ToDateTime(endTime).AddDays(1);
                            proModel = proModel.Where(x => x.RecordDate < endTimeS);
                        }
                    }
                    else if (projectState == 2)
                    {
                        //已竣工
                        proModel = proModel.Where(x => x.FactCompletionDate != null);
                        if (!string.IsNullOrWhiteSpace(beginTime))
                        {
                            var beginTimes = Convert.ToDateTime(beginTime);

                            proModel = proModel.Where(x => x.FactCompletionDate >= beginTimes);
                        }
                        if (!string.IsNullOrWhiteSpace(endTime))
                        {
                            var endTimeS = Convert.ToDateTime(endTime).AddDays(1);
                            proModel = proModel.Where(x => x.FactCompletionDate < endTimeS);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(beginTime))
                        {
                            var beginTimes = Convert.ToDateTime(beginTime);

                            proModel = proModel.Where(x => x.RecordDate >= beginTimes);
                        }
                        if (!string.IsNullOrWhiteSpace(endTime))
                        {
                            var endTimeS = Convert.ToDateTime(endTime).AddDays(1);
                            proModel = proModel.Where(x => x.RecordDate < endTimeS);
                        }
                    }
                    #endregion
                    if (clist.ParentCityCode == "320000")
                    {
                        superOrganNameList = await _context.CityZone.Where(w => w.ParentCityCode == clist.CityCode).Select(s => s.BelongedTo).ToListAsync();
                        proModel = proModel.Where(x => superOrganNameList.Contains(x.BelongedTo));
                    }
                    else if (belongedTo != "AJ320000-1")
                    {
                        proModel = proModel.Where(x => x.BelongedTo == belongedTo);
                    }
                    //工程类别
                    if (projectType == "一般房屋建筑工程")
                    {

                        proModel = proModel.Where(x => x.ProBigCategory.Contains("房屋建筑工程"));
                    }
                    else if (projectType == "市政工程")
                    {
                        proModel = proModel.Where(x => x.ProBigCategory.Contains("市政"));
                    }
                    else if (projectType == "轨道交通工程")
                    {
                        proModel = proModel.Where(x => x.ProBigCategory.Contains("轨道"));
                    }
                    else if (!string.IsNullOrEmpty(projectType))
                    {
                        proModel = proModel.Where(x => x.ProBigCategory.Contains(projectType));
                    }

                    if (!string.IsNullOrWhiteSpace(recordNumber))
                    {
                        proModel = proModel.Where(x => x.RecordNumber.Contains(recordNumber));
                    }
                    if (!string.IsNullOrWhiteSpace(projectName))
                    {
                        proModel = proModel.Where(x => x.ProjectName.Contains(projectName));
                    }
                    if (!string.IsNullOrWhiteSpace(projectType))
                    {
                        proModel = proModel.Where(x => x.ProjectCategory.Contains(projectType));
                    }
                    if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                    {
                        proModel = proModel.Where(x => x.SupervisionDepartmentId.Contains(supervisionDepartmentId));
                    }
                    //获取总数后分页
                    count = proModel.Count();
                    proList = await proModel.OrderByDescending(o => o.RecordDate).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
                }
                else
                {
                    var proModel = (from p in _context.ProjectOverview
                                    join proEnt in _context.ProjectEntSnapshot.Where(w => w.MainUnit == "是")
                                    on new { p.BelongedTo, p.RecordNumber }
                                    equals new { proEnt.BelongedTo, proEnt.RecordNumber }
                                    into list
                                    from plist in list.DefaultIfEmpty()
                                    where plist.EnterpriseType == entType && plist.EnterpriseName.Contains(entName)
                                     && p.RecordDate != null && p.RecordDate < now
                                    select new ProjectInfoStatisticsNewViewModel
                                    {
                                        Id = p.Id,
                                        BelongedTo = p.BelongedTo,
                                        RecordNumber = p.RecordNumber,
                                        RecordDate = p.RecordDate,
                                        ProjectName = p.ProjectName,
                                        ProjectAddress = p.ProjectAddress,
                                        ShiGongXuekeNo = p.ShiGongXuekeNo,
                                        SupervisionDepartmentId = p.SupervisionDepartmentId,
                                        FactCompletionDate = p.FactCompletionDate,
                                        ProjectCategory = p.ProjectCategory,
                                        Remark = p.Remark,
                                        SuperOrganName = "",
                                        SupervisionDepartmentName = "",
                                        SGDW = "",
                                        JSDW = "",
                                        JLDW = "",
                                        SGDWEntCode = "",
                                        JSDWEntCode = "",
                                        JLDWEntCode = "",
                                        ProjectPrice = p.ProjectPrice,
                                        ProjectAcreage = p.ProjectAcreage,
                                        ProjectStartDateTimne = p.ProjectStartDateTimne,
                                        ProjectEndDateTimne = p.ProjectEndDateTimne,
                                        ProjectTarget = p.ProjectTarget,
                                        ProjectState = p.FactCompletionDate == null ? "未竣工" : "已竣工",
                                        YiChouChaCount = 0,
                                        JiHuaChouChaCount = "0",
                                        PersonCount = 0
                                    });
                    if (clist.ParentCityCode == "320000")
                    {
                        superOrganNameList = await _context.CityZone.Where(w => w.ParentCityCode == clist.CityCode).Select(s => s.BelongedTo).ToListAsync();
                        proModel = proModel.Where(x => superOrganNameList.Contains(x.BelongedTo));
                    }
                    else if (belongedTo != "AJ320000-1")
                    {
                        proModel = proModel.Where(x => x.BelongedTo == belongedTo);
                    }
                    #region 竣工状态和时间筛选
                    if (projectState == 1)
                    {
                        //未竣工
                        proModel = proModel.Where(x => x.FactCompletionDate == null);
                        if (!string.IsNullOrWhiteSpace(beginTime))
                        {
                            var beginTimes = Convert.ToDateTime(beginTime);

                            proModel = proModel.Where(x => x.RecordDate >= beginTimes);
                        }
                        if (!string.IsNullOrWhiteSpace(endTime))
                        {
                            var endTimeS = Convert.ToDateTime(endTime).AddDays(1);
                            proModel = proModel.Where(x => x.RecordDate < endTimeS);
                        }

                    }
                    else if (projectState == 2)
                    {
                        //已竣工
                        proModel = proModel.Where(x => x.FactCompletionDate != null);
                        if (!string.IsNullOrWhiteSpace(beginTime))
                        {
                            var beginTimes = Convert.ToDateTime(beginTime);

                            proModel = proModel.Where(x => x.FactCompletionDate >= beginTimes);
                        }
                        if (!string.IsNullOrWhiteSpace(endTime))
                        {
                            var endTimeS = Convert.ToDateTime(endTime).AddDays(1);
                            proModel = proModel.Where(x => x.FactCompletionDate < endTimeS);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(beginTime))
                        {
                            var beginTimes = Convert.ToDateTime(beginTime);

                            proModel = proModel.Where(x => x.RecordDate >= beginTimes);
                        }
                        if (!string.IsNullOrWhiteSpace(endTime))
                        {
                            var endTimeS = Convert.ToDateTime(endTime).AddDays(1);
                            proModel = proModel.Where(x => x.RecordDate < endTimeS);
                        }
                    }
                    #endregion
                    //工程类别
                    if (!string.IsNullOrWhiteSpace(projectType))
                    {
                        proModel = proModel.Where(x => x.ProjectCategory.Contains(projectType));
                    }
                    if (!string.IsNullOrWhiteSpace(recordNumber))
                    {
                        proModel = proModel.Where(x => x.RecordNumber.Contains(recordNumber));
                    }
                    if (!string.IsNullOrWhiteSpace(projectName))
                    {
                        proModel = proModel.Where(x => x.ProjectName.Contains(projectName));
                    }
                    if (!string.IsNullOrWhiteSpace(projectType))
                    {
                        proModel = proModel.Where(x => x.ProjectCategory.Contains(projectType));
                    }
                    if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                    {
                        proModel = proModel.Where(x => x.SupervisionDepartmentId.Contains(supervisionDepartmentId));
                    }
                    //获取总数后分页
                    count = proModel.Count();
                    proList = await proModel.OrderByDescending(o => o.RecordDate).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
                }

                var belongedToList = proList.GroupBy(w => w.BelongedTo).Select(s => s.Key).ToList();
                var recordNumberToList = proList.Select(s => s.RecordNumber).ToList();
                var keshiIds = proList.GroupBy(w => w.SupervisionDepartmentId).Select(s => s.Key).ToList();
                //获取计划抽查数量
                var planCheckTimes = await _context.ProSupervisionPlan.Where(w =>
                belongedToList.Contains(w.BelongedTo) && recordNumberToList.Contains(w.RecordNumber)).Select(s => new
                {
                    BelongedTo = s.BelongedTo,
                    RecordNumber = s.RecordNumber,
                    PlanCheckTimes = s.PlanCheckTimes
                }).AsNoTracking().ToListAsync();


                var yiChouCha = await _context.DailyInspect.Where(w =>
                belongedToList.Contains(w.BelongedTo) && recordNumberToList.Contains(w.RecordNumber)
                && w.TreatmentType > 0 && !string.IsNullOrEmpty(w.Tzsno)).Select(s => new
                {
                    BelongedTo = s.BelongedTo,
                    RecordNumber = s.RecordNumber
                }).AsNoTracking().ToListAsync();
                var cityZoneList = await _context.CityZone.Where(w => belongedToList.Contains(w.BelongedTo)).ToListAsync();

                var danweiList = await _context.ProjectEntSnapshot.Where(w => recordNumberToList.Contains(w.RecordNumber)
                        && w.MainUnit == "是").AsNoTracking().ToListAsync();
                var keshiList = await _context.SupervisionDepartment.Where(w => keshiIds.Contains(w.SupervisionDepartmentId)).AsNoTracking().ToListAsync();
                proList.ForEach(x =>
                {
                    if (!string.IsNullOrWhiteSpace(x.SupervisionDepartmentId))
                    {
                        x.SupervisionDepartmentName = keshiList.Where(w => w.SupervisionDepartmentId == x.SupervisionDepartmentId).Select(x => x.Name).FirstOrDefault();
                    }
                    x.BelongsDepartments = x.SupervisionDepartmentName;
                    x.SuperOrganName = cityZoneList.Where(w => w.BelongedTo == x.BelongedTo).OrderByDescending(w => w.Id).Select(w => w.SuperOrganName).FirstOrDefault();

                    x.SGDW = danweiList.Where(w => w.BelongedTo == x.BelongedTo && w.RecordNumber == x.RecordNumber && w.EnterpriseType == "施工单位").OrderByDescending(w => w.Id).Select(w => w.EnterpriseName).FirstOrDefault();
                    //x.JSDW = danweiList.Where(w => w.BelongedTo == x.BelongedTo && w.RecordNumber == x.RecordNumber && w.EnterpriseType == "建设单位").OrderByDescending(w => w.Id).Select(w => w.EnterpriseName).FirstOrDefault();
                    //x.JLDW = danweiList.Where(w => w.BelongedTo == x.BelongedTo && w.RecordNumber == x.RecordNumber && w.EnterpriseType == "监理单位").OrderByDescending(w => w.Id).Select(w => w.EnterpriseName).FirstOrDefault();
                    x.SGDWEntCode = danweiList.Where(w => w.BelongedTo == x.BelongedTo && w.RecordNumber == x.RecordNumber && w.EnterpriseType == "施工单位").OrderByDescending(w => w.Id).Select(w => w.OrganizationCode).FirstOrDefault();
                    //x.JSDWEntCode = danweiList.Where(w => w.BelongedTo == x.BelongedTo && w.RecordNumber == x.RecordNumber && w.EnterpriseType == "建设单位").OrderByDescending(w => w.Id).Select(w => w.OrganizationCode).FirstOrDefault();
                    //x.JLDWEntCode = danweiList.Where(w => w.BelongedTo == x.BelongedTo && w.RecordNumber == x.RecordNumber && w.EnterpriseType == "监理单位").OrderByDescending(w => w.Id).Select(w => w.OrganizationCode).FirstOrDefault();
                    //x.YiChouChaCount = yiChouCha.Where(w => w.BelongedTo == x.BelongedTo && w.RecordNumber == x.RecordNumber).Count();
                    //x.JiHuaChouChaCount = planCheckTimes.Where(w => w.BelongedTo == x.BelongedTo && w.RecordNumber == x.RecordNumber)
                    //.Select(s => s.PlanCheckTimes).FirstOrDefault() == null ? "0" : planCheckTimes.Where(w => w.BelongedTo == x.BelongedTo
                    //   && w.RecordNumber == x.RecordNumber).Select(s => s.PlanCheckTimes).FirstOrDefault();
                });

                return ResponseViewModel<List<ProjectInfoStatisticsNewViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, proList, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("查看历史检查记录接口失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<ProjectInfoStatisticsNewViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


    }
}
