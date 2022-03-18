using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViewModels;

namespace JSAJ.Core.Controllers.manage
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class AssignRoleController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        private readonly ILogger<MenusController> _logger;
        private readonly JssanjianmanagerContext _context;
        private JwtSettings settings;
        private OssFileSetting _ossFileSetting;
        private readonly string _wordTemplte;
        private readonly string _buildWords;

        public AssignRoleController(IWebHostEnvironment environment, ILogger<MenusController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options, IOptions<OssFileSetting> oss)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            settings = options.Value;
            _ossFileSetting = oss.Value;
            _wordTemplte = environment.WebRootPath + "\\doc\\";
            _buildWords = environment.WebRootPath + "\\BuildPdf\\";
        }

        /// <summary>
        /// css-分配项目列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <param name="projectName"></param>
        /// <param name="recordNumber"></param>
        /// <param name="entName"></param>
        /// <param name="state">1未分配 2已分配</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<AssignRoleProjectViewModel>>> SelProjectRoleList(int page, int limit, string projectName, string recordNumber,
         string entName, int state, string supervisionDepartmentId, string address)
        {
            try
            {
                //登录人belongedto
                var belongedto = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                if (string.IsNullOrWhiteSpace(belongedto))
                {
                    return ResponseViewModel<List<AssignRoleProjectViewModel>>.Create(Status.FAIL, "仅限后台管理人员");
                }

                var data = from p in _context.ProjectOverview
                           join e in _context.ProjectEntSnapshot.Where(w => w.MainUnit == "是" && w.EnterpriseType == "施工单位")
                           on new { p.BelongedTo, p.RecordNumber } equals new { e.BelongedTo, e.RecordNumber }
                           into eData
                           from e in eData.DefaultIfEmpty()
                           join f in _context.SupervisionDepartment.Where(w => w.DeleteMark == 0)
                           on p.SupervisionDepartmentId equals f.SupervisionDepartmentId
                          into fData
                           from f in fData.DefaultIfEmpty()
                           where p.BelongedTo == belongedto && p.RecordDate != null && p.FactCompletionDate == null && !string.IsNullOrEmpty(p.RecordNumber)
                           orderby p.ProjectStartDateTimne descending
                           select new AssignRoleProjectViewModel
                           {
                               SupervisionDepartmentId = p.SupervisionDepartmentId,
                               SupervisionDepartmentName = f.Name,
                               BelongedTo = p.BelongedTo,
                               ProjectAddress = p.ProjectAddress,
                               EntCode = e.OrganizationCode,
                               EntName = e.EnterpriseName,
                               ProjectStartDate = p.ProjectStartDateTimne,
                               ProjectEndDate = p.ProjectEndDateTimne,
                               ProjectName = p.ProjectName,
                               RecordNumber = p.RecordNumber,
                               SuperviseUrl = p.PrintUrl,
                               ProjectStartDateTimne = p.ProjectStartDateTimne,
                               ProjectEndDateTimne = p.ProjectEndDateTimne,
                               RecordDate = p.RecordDate,
                               Key = p.Id
                           };

                if (!string.IsNullOrWhiteSpace(address))
                {
                    data = data.Where(x => x.ProjectAddress.Contains(address));
                }
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    data = data.Where(x => x.RecordNumber.Contains(recordNumber));
                }

                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    data = data.Where(x => x.ProjectName.Contains(projectName));
                }
                if (!string.IsNullOrWhiteSpace(entName))
                {
                    data = data.Where(x => x.EntName.Contains(entName));
                }

                if (!string.IsNullOrWhiteSpace(entName))
                {
                    data = data.Where(x => x.EntName.Contains(entName));
                }
                if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                {
                    data = data.Where(x => x.SupervisionDepartmentId == supervisionDepartmentId);
                }
                if (state == 1)
                {
                    //未处理
                    data = data.Where(x => string.IsNullOrWhiteSpace(x.SupervisionDepartmentId));
                }
                else
                {
                    data = data.Where(x => !string.IsNullOrWhiteSpace(x.SupervisionDepartmentId));
                }
                var result0 = data.Distinct().OrderByDescending(x => x.Key);
                var count = await result0.CountAsync();
                var result = result0.Skip((page - 1) * limit)
                   .Take(limit)
                   .ToList();

                return ResponseViewModel<List<AssignRoleProjectViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("分配项目列表" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<AssignRoleProjectViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// css-本站监督科室列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <param name="projectName"></param>
        /// <param name="recordNumber"></param>
        /// <param name="entName"></param>
        /// <param name="state">1未分配 2已分配</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<object>> SelSupervisionDepartmentList()
        {
            try
            {
                //登录人belongedto
                var belongedto = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                if (string.IsNullOrWhiteSpace(belongedto))
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "仅限后台管理人员");
                }
                var data = await _context.SupervisionDepartment
                    .Where(w => w.DeleteMark == 0 && w.BelongedTo == belongedto).ToListAsync();
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, data, data.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError("本站角色列表" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// 分配管理角色
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<string>> SetSupervisionDepartmentProject(SetRoleToProjectViewModel model)
        {
            try
            {
                if (model.ids == null || model.ids.Count == 0)
                {

                }
                var data = await _context.ProjectOverview.Where(w => model.ids.Contains(w.Id)).ToListAsync();
                data.ForEach(x =>
                {
                    x.SupervisionDepartmentId = model.SupervisionDepartmentId;
                });
                _context.ProjectOverview.UpdateRange(data);
                await _context.SaveChangesAsync();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "分配成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("分配管理角色：", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }




    }
}