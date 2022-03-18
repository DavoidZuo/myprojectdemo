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
    public class MapController : ControllerBase
    {
        private readonly JssanjianmanagerContext _context;
        private readonly ILogger<MapController> _logger;
        public MapController(ILogger<MapController> logger, JssanjianmanagerContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        [Authorize]
        /// <summary>
        /// 获取监督机构菜单
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseViewModel<List<nodeManage>>> SearchSuperOrganList()
        {
            try
            {
                //解析Token
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;//后台账号登录   
                var portType = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//后台账号登录   
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//操作人科室
                var benrenRoleId = User.FindFirst(nameof(ClaimTypeEnum.RoleId))?.Value;//操作人角色
                if (portType != "2")
                {
                    return ResponseViewModel<List<nodeManage>>.Create(Status.FAIL, "非法访问,仅限后台管理人员访问");
                }
                if (belongedTo == "AJ320000-1")
                {
                    //省级机构
                    var shengCity = _context.CityZone.Where(x => x.BelongedTo == belongedTo && x.IsHide == 0)
                        .Select(x => new nodeManage
                        {
                            Title = x.SuperOrganName,
                            Key = x.BelongedTo,
                            BelongedTo = x.BelongedTo,
                            RoleId = null,
                            SupervisionDepartmentId = null,
                            CityCode = x.CityCode,
                            Value = x.BelongedTo,
                            SearchType = 0,
                            LatitudeCoordinate = x.LatitudeCoordinate,
                            LongitudeCoordinate = x.LongitudeCoordinate,
                            MunicipalJurisdiction = x.Flag,
                            Children = _context.CityZone.Where(t => t.ParentCityCode == x.CityCode && t.IsHide == 0)
                            .Select(t => new nodeManage
                            {
                                Title = t.SuperOrganName.Replace("江苏省", ""),
                                Key = t.BelongedTo,
                                BelongedTo = t.BelongedTo,
                                MunicipalJurisdiction = t.Flag,
                                LatitudeCoordinate = t.LatitudeCoordinate,
                                LongitudeCoordinate = t.LongitudeCoordinate,
                                Children = _context.CityZone.Where(y => y.ParentCityCode == t.CityCode && y.IsHide == 0)
                               .Select(y => new nodeManage
                               {
                                   Title = y.SuperOrganName,
                                   Key = y.BelongedTo,
                                   BelongedTo = y.BelongedTo,
                                   RoleId = null,
                                   SupervisionDepartmentId = null,
                                   MunicipalJurisdiction = y.Flag,
                                   LatitudeCoordinate = y.LatitudeCoordinate,
                                   LongitudeCoordinate = y.LongitudeCoordinate,
                                   Children = _context.SupervisionDepartment.Where(w => w.BelongedTo == y.BelongedTo
                                   && w.DeleteMark == 0)
                                   .Select(b =>
                                       new nodeManage
                                       {
                                           Title = b.Name,
                                           Key = b.SupervisionDepartmentId,
                                           Value = b.SupervisionDepartmentId,
                                           BelongedTo = y.BelongedTo,
                                           RoleId = b.SupervisionDepartmentId,
                                           SupervisionDepartmentId = b.SupervisionDepartmentId,
                                           LatitudeCoordinate = y.LatitudeCoordinate,
                                           LongitudeCoordinate = y.LongitudeCoordinate,
                                           Children = null,
                                           CityCode = "",
                                           SearchType = 3
                                       }
                                   ),
                                   CityCode = y.CityCode,
                                   Value = y.BelongedTo,
                                   SearchType = 2
                               }).OrderBy(y => y.CityCode),
                                CityCode = t.CityCode,
                                Value = t.BelongedTo,
                                SearchType = 1
                            }).OrderBy(t => t.CityCode)
                        }).ToList();

                    return ResponseViewModel<List<nodeManage>>.Create(Status.SUCCESS, Message.SUCCESS, shengCity, shengCity.Count);
                }
                else
                {

                    List<nodeManage> resList = new List<nodeManage>();

                    //本人角色
                    var benrenroles = await _context.Roles.Where(a => a.RoleId == benrenRoleId)
                        .OrderBy(a => a.Id).FirstOrDefaultAsync();
                    var cityAndParentCityInfo = await (from a in _context.CityZone
                                                       join b in _context.CityZone
                                                       on a.ParentCityCode equals b.CityCode
                                                       where a.BelongedTo == belongedTo && a.IsHide == 0 && b.IsHide == 0
                                                       select new nodeManage
                                                       {
                                                           Title = a.SuperOrganName,
                                                           Key = a.BelongedTo,
                                                           BelongedTo = a.BelongedTo,
                                                           CityCode = a.CityCode,
                                                           Value = a.BelongedTo,
                                                           MunicipalJurisdiction = a.Flag,
                                                           ParentBelongedTo = b.BelongedTo,
                                                           SearchType = b.BelongedTo == "AJ320000-1" ? 1 : 2,
                                                           LatitudeCoordinate = a.LatitudeCoordinate,
                                                           LongitudeCoordinate = a.LongitudeCoordinate,
                                                           ParentCityCode = b.CityCode
                                                       }).FirstOrDefaultAsync();
                    if (cityAndParentCityInfo.ParentBelongedTo == "AJ320000-1"
                        || (cityAndParentCityInfo.MunicipalJurisdiction == 1 && benrenroles.IsOwner >= 1))
                    {
                        string parentBelongedTo = "";
                        string parentCityCode = "";
                        if (cityAndParentCityInfo.ParentBelongedTo == "AJ320000-1")
                        {   //说明是市级账号
                            parentBelongedTo = cityAndParentCityInfo.BelongedTo;
                            parentCityCode = cityAndParentCityInfo.CityCode;

                        }
                        else
                        {
                            //说明是市辖安监站的领导岗账号
                            parentBelongedTo = cityAndParentCityInfo.ParentBelongedTo;
                            parentCityCode = cityAndParentCityInfo.ParentCityCode;
                        }


                        resList = _context.CityZone.Where(t => t.BelongedTo == parentBelongedTo && t.IsHide == 0)
                            .Select(t => new nodeManage
                            {
                                Title = t.SuperOrganName.Replace("江苏省", ""),
                                Key = t.BelongedTo,
                                BelongedTo = t.BelongedTo,
                                MunicipalJurisdiction = t.Flag,
                                LatitudeCoordinate = t.LatitudeCoordinate,
                                LongitudeCoordinate = t.LongitudeCoordinate,
                                CityCode = t.CityCode,
                                Value = t.BelongedTo,
                                SearchType = 1,
                                Children = _context.CityZone.Where(y => y.ParentCityCode == t.CityCode && t.IsHide == 0)
                               .Select(y => new nodeManage
                               {
                                   Title = y.SuperOrganName,
                                   Key = y.BelongedTo,
                                   BelongedTo = y.BelongedTo,
                                   RoleId = null,
                                   SupervisionDepartmentId = null,
                                   MunicipalJurisdiction = y.Flag,
                                   LatitudeCoordinate = y.LatitudeCoordinate,
                                   LongitudeCoordinate = y.LongitudeCoordinate,
                                   CityCode = y.CityCode,
                                   Value = y.BelongedTo,
                                   SearchType = 2,
                                   Children = _context.SupervisionDepartment.Where(w => w.BelongedTo == y.BelongedTo
                                   && w.DeleteMark == 0)
                                   .Select(b =>
                                       new nodeManage
                                       {
                                           Title = b.Name,
                                           Key = b.SupervisionDepartmentId,
                                           Value = b.SupervisionDepartmentId,
                                           BelongedTo = y.BelongedTo,
                                           RoleId = b.SupervisionDepartmentId,
                                           SupervisionDepartmentId = b.SupervisionDepartmentId,
                                           LatitudeCoordinate = y.LatitudeCoordinate,
                                           LongitudeCoordinate = y.LongitudeCoordinate,
                                           Children = null,
                                           CityCode = "",
                                           SearchType = 3
                                       }
                                   )
                               }).OrderBy(y => y.CityCode)
                            }).ToList();
                        return ResponseViewModel<List<nodeManage>>.Create(Status.SUCCESS, Message.SUCCESS, resList, resList.Count);
                    }
                    else
                    {
                        var keshiList = await _context.SupervisionDepartment.Where(w => w.BelongedTo == belongedTo
                          && w.DeleteMark == 0).Select(b =>
                                         new nodeManage
                                         {
                                             Title = b.Name,
                                             Key = b.SupervisionDepartmentId,
                                             Value = b.SupervisionDepartmentId,
                                             BelongedTo = b.BelongedTo,
                                             RoleId = b.SupervisionDepartmentId,
                                             SupervisionDepartmentId = b.SupervisionDepartmentId,
                                             LatitudeCoordinate = cityAndParentCityInfo.LatitudeCoordinate,
                                             LongitudeCoordinate = cityAndParentCityInfo.LongitudeCoordinate,
                                             Children = null,
                                             CityCode = "",
                                             SearchType = 3
                                         }
                                   ).ToListAsync();
                        var keshi = keshiList.Where(x => x.SupervisionDepartmentId == supervisionDepartmentId).ToList();
                        if (keshi != null && keshi.Count > 0)
                        {
                            //说明是监督科室的角色之一
                            return ResponseViewModel<List<nodeManage>>.Create(Status.SUCCESS, Message.SUCCESS, keshi, keshi.Count);
                        }
                        else
                        {
                            cityAndParentCityInfo.Children = keshiList;
                            resList.Add(cityAndParentCityInfo);
                            return ResponseViewModel<List<nodeManage>>.Create(Status.SUCCESS, Message.SUCCESS, resList, resList.Count);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("获取监督机构列表：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<List<nodeManage>>.Create(Status.ERROR, Message.ERROR);
            }


        }
    }
}
