using Common;
using JSAJ.Core.Common.DataBll;
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
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ViewModels;

namespace JSAJNanJing.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MenusController> _logger;
        private readonly JssanjianmanagerContext _context;
        private JwtSettings settings;
        private OssFileSetting _ossFileSetting;
        public OrganizationController(IWebHostEnvironment environment, ILogger<MenusController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options, IOptions<OssFileSetting> oss)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            settings = options.Value;
            _ossFileSetting = oss.Value;
        }


        /// <summary>
        /// css-左侧组织机构列表
        /// </summary>
        /// <param name="belongedTo">0省第一级 1第二级  3第三级</param>
        /// <param name="searchType"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<MenuInfoViewModel>>> GetOrganizationListFirst()
        {
            try
            {
                //解析Token
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;//后台账号登录   
                var portType = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//后台账号登录   
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var roleId = User.FindFirst(nameof(ClaimTypeEnum.RoleId))?.Value;
                var roleName = User.FindFirst(ClaimsIdentity.DefaultRoleClaimType)?.Value;
                if (portType != "2")
                {
                    return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.FAIL, "非法访问,仅限后台管理人员访问");
                }
                ScopedSlotsViewModel scopedSlots = new ScopedSlotsViewModel();
                scopedSlots.Title = "custom";
                List<MenuInfoViewModel> organizationList = new List<MenuInfoViewModel>();
                if (belongedTo == "AJ320000-1")
                {
                    organizationList = await _context.CityZone.Where(x => string.IsNullOrEmpty(x.ParentCityCode))
                        .Select(x => new MenuInfoViewModel
                        {
                            QueryHierarchy = 1,
                            MunicipalJurisdiction = x.Flag,
                            Key = x.BelongedTo,
                            Title = x.SuperOrganName,
                            LatitudeCoordinate = x.LatitudeCoordinate,
                            LongitudeCoordinate = x.LongitudeCoordinate,
                            Address = x.Address,
                            CityCode = x.CityCode,
                            ParentCityCode = x.ParentCityCode,
                            IsLeaf = false,
                            IsAddItem = 0,
                            CanDelete = 0,
                            UserCount = 0,
                            scopedSlots = scopedSlots
                        }).ToListAsync();
                    return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, organizationList, organizationList.Count);
                }
                else
                {
                    var benrenroles = await _context.Roles.Where(a => a.RoleId == roleId)
                        .OrderBy(a => a.Id).FirstOrDefaultAsync();

                    var cityAndParentCityInfo = await (from a in _context.CityZone
                                                       join b in _context.CityZone
                                                       on a.ParentCityCode equals b.CityCode
                                                       where a.BelongedTo == belongedTo
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
                        //  //如果当前登录人的上一级机构编码是AJ320000-1或者
                        //  //市辖安监站领导岗角色
                        //  //可以看到全市安监站数据

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
                        organizationList = _context.CityZone.Where(t => t.BelongedTo == parentBelongedTo)
                            .Select(t => new MenuInfoViewModel
                            {
                                QueryHierarchy = 2,
                                Key = t.BelongedTo,
                                MunicipalJurisdiction = t.Flag,
                                Title = t.SuperOrganName,
                                LatitudeCoordinate = t.LatitudeCoordinate,
                                LongitudeCoordinate = t.LongitudeCoordinate,
                                Address = t.Address,
                                CityCode = t.CityCode,
                                ParentCityCode = t.ParentCityCode,
                                IsLeaf = false,
                                IsAddItem = roleName == "超级管理员" ? 1 : 0,
                                CanDelete = 0,
                                UserCount = 0,
                                scopedSlots = scopedSlots
                            }).ToList();
                        return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, organizationList, organizationList.Count);
                    }
                    else
                    {
                        organizationList = await _context.CityZone.Where(x => x.BelongedTo == belongedTo)
                                            .Select(x => new MenuInfoViewModel
                                            {
                                                QueryHierarchy = -1,
                                                Key = x.BelongedTo,
                                                Title = x.SuperOrganName,
                                                MunicipalJurisdiction = x.Flag,
                                                CityCode = x.CityCode,
                                                ParentCityCode = x.ParentCityCode,
                                                LatitudeCoordinate = x.LatitudeCoordinate,
                                                LongitudeCoordinate = x.LongitudeCoordinate,
                                                Address = x.Address,
                                                IsLeaf = true,
                                                IsAddItem = 0,
                                                CanAddUser = (belongedTo == x.BelongedTo || roleName == "超级管理员") ? 1 : 0,
                                                CanDelete = roleName == "超级管理员" ? 1 : 0,
                                                UserCount = 0,
                                                scopedSlots = scopedSlots
                                            }).ToListAsync();

                        return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, organizationList, organizationList.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("组织机构菜单：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 添加监督科室/修改监督科室
        /// </summary>
        /// <param name="viewModel">修改的话多传一个人员id</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<ResponseViewModel<object>> UpdateSupervisionDepartment([FromBody] SupervisionDepartment viewModel)
        {
            try
            {

                var role = User.FindFirst(nameof(ClaimsIdentity.RoleClaimType))?.Value;//当前人角色
                var makeBelongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;//当前操作人的BelongedTo
                var makeUuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;//当前操作人的Uuid
                if (string.IsNullOrEmpty(makeBelongedTo))
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL);
                }

                if (viewModel == null || string.IsNullOrEmpty(viewModel.Name))
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "参数错误");
                }

                var now = DateTime.Now;
                string userUuid = null;
                List<UserRole> jueseGuanLianList = new List<UserRole>();
                if (!String.IsNullOrWhiteSpace(viewModel.SupervisionDepartmentId))
                {

                    SupervisionDepartment keshi = await _context.SupervisionDepartment.Where(x => x.SupervisionDepartmentId == viewModel.SupervisionDepartmentId && x.DeleteMark == 0).FirstOrDefaultAsync();

                    //判断手机号是否重复
                    SupervisionDepartment userInfoIsExist = await _context.SupervisionDepartment.Where(x => (x.Name == viewModel.Name
                    && x.SupervisionDepartmentId != viewModel.SupervisionDepartmentId) && x.DeleteMark == 0
                    && x.BelongedTo == makeBelongedTo).OrderByDescending(x => x.UpdateTime).FirstOrDefaultAsync();
                    if (userInfoIsExist != null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "监督科室名称重复！", "监督科室名称重复！");
                    }
                    if (keshi != null)
                    {
                        keshi.Name = viewModel.Name;
                        keshi.Describe = viewModel.Describe;
                        keshi.DeleteMark = 0;
                        keshi.CreatorId = userUuid;
                        keshi.UpdateTime = DateTime.Now;
                        _context.SupervisionDepartment.Update(keshi);
                    }
                }
                else
                {
                    SupervisionDepartment addkeshi = new SupervisionDepartment();
                    addkeshi.BelongedTo = makeBelongedTo;
                    addkeshi.SupervisionDepartmentId = SecurityManage.GuidUpper();
                    addkeshi.Name = viewModel.Name;
                    addkeshi.Describe = viewModel.Describe;
                    addkeshi.CreatorId = userUuid;
                    addkeshi.CreateTime = DateTime.Now;
                    addkeshi.UpdateTime = DateTime.Now;
                    addkeshi.DeleteMark = 0;
                    await _context.SupervisionDepartment.AddAsync(addkeshi);

                }
                await _context.SaveChangesAsync();
                return ResponseViewModel<object>.Create(Status.SUCCESS, "保存成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("添加监督科室/修改监督科室OrganizationController/UpdateSupervisionDepartment：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, "保存失败");
            }
        }


        /// <summary>
        /// 添加人员信息/修改人员信息
        /// </summary>
        /// <param name="viewModel">修改的话多传一个人员id</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<ResponseViewModel<object>> UpdateUserInfo([FromBody] SysUserManager viewModel)
        {
            try
            {

                var role = User.FindFirst(nameof(ClaimsIdentity.RoleClaimType))?.Value;//当前人角色
                var makeBelongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;//当前操作人的BelongedTo
                var makeUuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;//当前操作人的Uuid

                if (makeBelongedTo != viewModel.BelongedTo)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "当前角色不允许操作非所属机构人员信息");
                }
                if (!DataBll.CheckparmAddUserInfo(viewModel))
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "请将人员信息填写完整");
                }

                var now = DateTime.Now;
                string userUuid = null;
                List<UserRole> jueseGuanLianList = new List<UserRole>();
                if (!String.IsNullOrWhiteSpace(viewModel.Uuid))
                {

                    SysUserManager user = await _context.SysUserManager.Where(x => x.Uuid == viewModel.Uuid && x.DeleteMark == 0 && x.AccountType == 0).FirstOrDefaultAsync();
                    #region 新
                    //判断手机号是否重复
                    SysUserManager userInfo = await _context.SysUserManager.Where(x => (x.UserPhone == viewModel.UserPhone && x.Uuid != viewModel.Uuid) && x.DeleteMark == 0).OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync();
                    if (userInfo != null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "该手机号已存在！", "该手机号已存在！");
                    }
                    SysUserManager userInfoidcard = await _context.SysUserManager.Where(x => (x.UserCardId == viewModel.UserCardId && x.Uuid != viewModel.Uuid) && x.DeleteMark == 0).OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync();
                    if (userInfoidcard != null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "该身份证号已存在！", "该身份证号已存在！");
                    }
                    //判断手机号是否重复
                    TestingInstituteUser userInfoJc = await _context.TestingInstituteUser.Where(x => x.Account == viewModel.UserPhone && x.DeleteTag == 0).OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync();
                    if (userInfoJc != null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "该手机号已经注册了检测所账号！", "该手机号已经注册了检测所账号！");
                    }
                    #endregion
                    userUuid = viewModel.Uuid;
                    user.UserName = viewModel.UserName;
                    //user.BelongedTo = viewModel.BelongedTo;
                    user.Brithday = viewModel.Brithday;
                    user.IdType = viewModel.IdType;
                    user.UserCardId = viewModel.UserCardId;
                    user.UserSex = viewModel.UserSex;
                    user.UserPhone = viewModel.UserPhone;
                    user.Profession = viewModel.Profession;
                    user.ControlLevel = viewModel.ControlLevel;
                    user.Technical = viewModel.Technical;
                    user.Technical = viewModel.Technical;
                    user.UserPwd = SecurityManage.StringToMD5(viewModel.UserPwd);
                    user.PersonState = viewModel.PersonState;
                    user.Photo = viewModel.Photo;
                    user.Edulevel = viewModel.Edulevel;
                    user.LawCertificate = viewModel.LawCertificate;
                    user.EnforceLawNumber = viewModel.EnforceLawNumber;
                    //user.RoleId = viewModel.RoleId;
                    //user.SupervisionDepartmentId = viewModel.SupervisionDepartmentId;
                    _context.SysUserManager.Update(user);
                    var jueseListOld = await _context.UserRoles.Where(x => x.SysUserManagerUuid == user.Uuid).ToListAsync();
                    //删除原来的角色关联
                    _context.UserRoles.RemoveRange(jueseListOld);
                }
                else
                {
                    //判断手机号或者身份证在同一个安检机构，同一个岗位是否已经存在
                    SysUserManager user = await _context.SysUserManager.Where(x => (x.UserCardId == viewModel.UserCardId || x.UserPhone == viewModel.UserPhone) && x.DeleteMark == 0 && x.AccountType == 0).OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync();
                    if (user != null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "人员身份证号或手机号已存在！");
                    }
                    viewModel.Uuid = SecurityManage.GuidUpper();
                    userUuid = viewModel.Uuid;
                    viewModel.UserPwd = SecurityManage.StringToMD5(viewModel.UserPwd);
                    viewModel.CreateDate = DateTime.Now;
                    await _context.SysUserManager.AddAsync(viewModel);

                }

                if (viewModel.Roles != null && viewModel.Roles.Count > 0 && !string.IsNullOrWhiteSpace(userUuid))
                {
                    var jueseList = await _context.Roles.Where(x => viewModel.Roles.Select(k => k.Key).Contains(x.RoleId)).ToListAsync();
                    viewModel.Roles.ForEach(y =>
                    {

                        UserRole juese = new UserRole();
                        juese.RoleId = y.Key;
                        juese.SysUserManagerUuid = userUuid;
                        juese.DeleteMark = 0;
                        juese.CreateDate = now;
                        juese.UpdateDate = now;
                        juese.CreatorId = makeUuid;
                        jueseGuanLianList.Add(juese);
                    });
                    await _context.UserRoles.AddRangeAsync(jueseGuanLianList);

                }
                await _context.SaveChangesAsync();
                return ResponseViewModel<object>.Create(Status.SUCCESS, "人员信息保存成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("人员添加OrganizationController/AddUserInfo：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, "人员信息保存失败");
            }
        }


        /// <summary>
        /// css-监督科室列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageIndex">页大小</param>
        /// <param name="word">关键字(人名模糊查询)</param>
        /// <param name="searchType">0全省 1市 2安监站</param>
        /// <param name="belongedTo">机构编号（全省的不需要传值）</param>      
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<ResponseViewModel<object>> GetSupervisionDepartmentList(int pageIndex, int pageSize, string word)
        {
            try
            {
                var makeBelongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;//当前操作人的BelongedTo
                var makeUuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;//当前操作人的Uuid
                if (string.IsNullOrEmpty(makeBelongedTo))
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL);
                }

                if (pageIndex <= 0 || pageSize <= 0)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "参数错误");
                }
                var supervisionDepartmentList = _context.SupervisionDepartment.Where(w => w.DeleteMark == 0 && w.BelongedTo == makeBelongedTo);
                if (!string.IsNullOrEmpty(word))
                {
                    supervisionDepartmentList = supervisionDepartmentList.Where(w => w.Name.Contains(word));
                }
                int totalCount = supervisionDepartmentList.Count();
                var supervisionDepartmentListRes = supervisionDepartmentList.OrderBy(x => x.BelongedTo).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                var keshilist = await _context.SupervisionDepartment.Where(w => w.DeleteMark == 0 && w.BelongedTo == makeBelongedTo).ToListAsync();
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, keshilist, totalCount);

            }
            catch (Exception)
            {

                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// css-右侧机构人员列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageIndex">页大小</param>
        /// <param name="word">关键字(人名模糊查询)</param>
        /// <param name="searchType">0全省 1市 2安监站</param>
        /// <param name="belongedTo">机构编号（全省的不需要传值）</param>      
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<ResponseViewModel<object>> GetAnJianUserList(int pageIndex, int pageSize, int searchType, string belongedTo, string word)
        {
            try
            {
                List<OrganizationUserList> uselist = null;
                int totalCount = 0;
                var roles = await (from a in _context.UserRoles
                                   join b in _context.Roles
                                   on a.RoleId equals b.RoleId
                                   where a.DeleteMark == 0 && b.Deleted == 0
                                   select new { a.SysUserManagerUuid, b.RoleId, b.Name, b.SupervisionDepartmentId }).ToListAsync();
                if (searchType == 0)
                {
                    uselist = await (from a in _context.CityZone
                                     join a1 in _context.CityZone on a.ParentCityCode equals a1.CityCode
                                     join a2 in _context.CityZone on a1.ParentCityCode equals a2.CityCode
                                     join b in _context.SysUserManager
                                     on a.BelongedTo equals b.BelongedTo
                                     where string.IsNullOrWhiteSpace(a2.ParentCityCode) && b.DeleteMark == 0 && b.AccountType == 0
                                     select new OrganizationUserList
                                     {
                                         id = b.Id,
                                         Uuid = b.Uuid,
                                         BelongedTo = b.BelongedTo,
                                         UserName = b.UserName,
                                         UserNumber = b.UserName,
                                         UserPhone = b.UserPhone,
                                         UserCardID = b.UserCardId,
                                         UserSex = b.UserSex,
                                         Department = b.Department,
                                         GroupName = b.GroupName,
                                         SuperOrganName = a.SuperOrganName,
                                         CityCode = a.CityCode,
                                         Station = b.Station,
                                         StationCertificate = b.StationCertificate,
                                         Technical = b.Technical,
                                         LawCertificate = b.LawCertificate,
                                         ControlLevel = b.ControlLevel,
                                         Brithday = b.Brithday,
                                         Offices = b.Offices,
                                         Status = b.Status,
                                         MovePhone = b.MovePhone,
                                         LastUpdateTime = b.LastUpdateTime,
                                         Remark = b.Remark,
                                         AccountNumber = b.AccountNumber,
                                         CreateDate = b.CreateDate,
                                         StopDate = b.StopDate,
                                         Education = b.Education,
                                         Degree = b.Degree,
                                         Email = b.Email,
                                         Isminumber = b.Isminumber,
                                         FirstTimeToWork = b.FirstTimeToWork,
                                         PutinWorkdate = b.PutinWorkdate,
                                         Authority = b.Authority,
                                         HeadImgUrl = b.HeadImgUrl,
                                         WechatMinpOpenId = b.WechatMinpOpenId,
                                         WechatMinpSessionKey = b.WechatMinpSessionKey,
                                         Profession = b.Profession,
                                         Post = b.Post,
                                         PositionalTitle = b.PositionalTitle,
                                         EnforceLawNumber = b.EnforceLawNumber,
                                         IdType = b.IdType,
                                         Edulevel = b.Edulevel,
                                         PersonState = b.PersonState,
                                         //SupervisionDepartmentId = b.SupervisionDepartmentId,
                                         Photo = b.Photo
                                     }).OrderBy(x => x.CityCode).ToListAsync();
                }
                else if (searchType == 1 && !string.IsNullOrEmpty(belongedTo))
                {
                    uselist = await (from a in _context.CityZone
                                     join a1 in _context.CityZone on a.ParentCityCode equals a1.CityCode
                                     join b in _context.SysUserManager
                                     on a.BelongedTo equals b.BelongedTo
                                     where a1.BelongedTo == belongedTo && b.DeleteMark == 0
                                     select new OrganizationUserList
                                     {
                                         id = b.Id,
                                         Uuid = b.Uuid,
                                         BelongedTo = b.BelongedTo,
                                         UserName = b.UserName,
                                         UserNumber = b.UserName,
                                         UserPhone = b.UserPhone,
                                         UserCardID = b.UserCardId,
                                         UserSex = b.UserSex,
                                         Department = b.Department,
                                         GroupName = b.GroupName,
                                         SuperOrganName = a.SuperOrganName,
                                         CityCode = a.CityCode,
                                         Station = b.Station,
                                         StationCertificate = b.StationCertificate,
                                         Technical = b.Technical,
                                         LawCertificate = b.LawCertificate,
                                         ControlLevel = b.ControlLevel,
                                         Brithday = b.Brithday,
                                         Offices = b.Offices,
                                         Status = b.Status,
                                         MovePhone = b.MovePhone,
                                         LastUpdateTime = b.LastUpdateTime,
                                         Remark = b.Remark,
                                         AccountNumber = b.AccountNumber,
                                         CreateDate = b.CreateDate,
                                         StopDate = b.StopDate,
                                         Education = b.Education,
                                         Degree = b.Degree,
                                         Email = b.Email,
                                         Isminumber = b.Isminumber,
                                         FirstTimeToWork = b.FirstTimeToWork,
                                         PutinWorkdate = b.PutinWorkdate,
                                         Authority = b.Authority,
                                         HeadImgUrl = b.HeadImgUrl,
                                         WechatMinpOpenId = b.WechatMinpOpenId,
                                         WechatMinpSessionKey = b.WechatMinpSessionKey,
                                         Profession = b.Profession,
                                         Post = b.Post,
                                         PositionalTitle = b.PositionalTitle,
                                         EnforceLawNumber = b.EnforceLawNumber,
                                         IdType = b.IdType,
                                         Edulevel = b.Edulevel,
                                         PersonState = b.PersonState,
                                         // SupervisionDepartmentId = b.SupervisionDepartmentId,
                                         Photo = b.Photo
                                     }).OrderBy(x => x.CityCode).ToListAsync();
                }
                else if (searchType == 2 && !string.IsNullOrEmpty(belongedTo))
                {
                    uselist = await (from a in _context.CityZone
                                     join b in _context.SysUserManager
                                     on a.BelongedTo equals b.BelongedTo
                                     where a.BelongedTo == belongedTo && b.DeleteMark == 0
                                     select new OrganizationUserList
                                     {
                                         id = b.Id,
                                         Uuid = b.Uuid,
                                         BelongedTo = b.BelongedTo,
                                         UserName = b.UserName,
                                         UserNumber = b.UserName,
                                         UserPhone = b.UserPhone,
                                         UserCardID = b.UserCardId,
                                         UserSex = b.UserSex,
                                         Department = b.Department,
                                         GroupName = b.GroupName,
                                         SuperOrganName = a.SuperOrganName,
                                         CityCode = a.CityCode,
                                         Station = b.Station,
                                         StationCertificate = b.StationCertificate,
                                         Technical = b.Technical,
                                         LawCertificate = b.LawCertificate,
                                         ControlLevel = b.ControlLevel,
                                         Brithday = b.Brithday,
                                         Offices = b.Offices,
                                         Status = b.Status,
                                         MovePhone = b.MovePhone,
                                         LastUpdateTime = b.LastUpdateTime,
                                         Remark = b.Remark,
                                         AccountNumber = b.AccountNumber,
                                         CreateDate = b.CreateDate,
                                         StopDate = b.StopDate,
                                         Education = b.Education,
                                         Degree = b.Degree,
                                         Email = b.Email,
                                         Isminumber = b.Isminumber,
                                         FirstTimeToWork = b.FirstTimeToWork,
                                         PutinWorkdate = b.PutinWorkdate,
                                         Authority = b.Authority,
                                         HeadImgUrl = b.HeadImgUrl,
                                         WechatMinpOpenId = b.WechatMinpOpenId,
                                         WechatMinpSessionKey = b.WechatMinpSessionKey,
                                         Profession = b.Profession,
                                         Post = b.Post,
                                         PositionalTitle = b.PositionalTitle,
                                         EnforceLawNumber = b.EnforceLawNumber,
                                         IdType = b.IdType,
                                         Edulevel = b.Edulevel,
                                         PersonState = b.PersonState,
                                         // SupervisionDepartmentId = b.SupervisionDepartmentId,
                                         Photo = b.Photo
                                     }).OrderBy(x => x.CityCode).ToListAsync();
                }


                if (!string.IsNullOrWhiteSpace(word))
                {
                    uselist = uselist.Where(x => x.UserName.Contains(word)).ToList();
                }

                totalCount = uselist.Count();
                uselist = uselist.OrderBy(x => x.BelongedTo).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                uselist.ForEach(x =>
                {
                    //x.SupervisionDepartmentId = roles.Where(y => y.SysUserManagerUuid == x.Uuid).Select(y => new KeyValueDic { Key = y.RoleId, Value = y.Name }).ToList();
                    x.Roles = roles.Where(y => y.SysUserManagerUuid == x.Uuid).Select(y =>
                     new KeyValueDic { Key = y.RoleId, Value = y.Name, SupervisionDepartmentId = y.SupervisionDepartmentId }
                    ).ToList();
                });
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, uselist, totalCount);


            }
            catch (Exception ex)
            {
                _logger.LogError("人员-数据：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// css-左侧组织机构列表
        /// </summary>
        /// <param name="belongedTo">0省第一级 1第二级  3第三级</param>
        /// <param name="searchType"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<MenuInfoViewModel>>> GetOrganizationList(string cityCode, int searchType)
        {
            try
            {
                var roleName = User.FindFirst(ClaimsIdentity.DefaultRoleClaimType)?.Value;
                ScopedSlotsViewModel scopedSlots = new ScopedSlotsViewModel();
                scopedSlots.Title = "custom";
                List<MenuInfoViewModel> organizationList = new List<MenuInfoViewModel>();
                if (searchType == 0)
                {
                    organizationList = await _context.CityZone.Where(x => string.IsNullOrEmpty(x.ParentCityCode))
                          .Select(x => new MenuInfoViewModel
                          {
                              QueryHierarchy = 1,
                              Key = x.BelongedTo,
                              Title = x.SuperOrganName,
                              MunicipalJurisdiction = x.Flag,
                              LatitudeCoordinate = x.LatitudeCoordinate,
                              LongitudeCoordinate = x.LongitudeCoordinate,
                              Address = x.Address,
                              CityCode = x.CityCode,
                              ParentCityCode = x.ParentCityCode,
                              IsLeaf = false,
                              IsAddItem = 0,
                              CanDelete = 0,
                              UserCount = 0,
                              scopedSlots = scopedSlots
                          }).OrderBy(x => x.CityCode).ToListAsync();


                    //查找一级机构信息
                    var data = (from a in _context.SysUserManager
                                join b in _context.CityZone
                                on a.BelongedTo equals b.BelongedTo
                                into resData1
                                from res1 in resData1.DefaultIfEmpty()
                                join b2 in _context.CityZone.Where(j => organizationList.Select(k => k.CityCode).Contains(j.ParentCityCode))
                                on res1.ParentCityCode equals b2.CityCode
                                into resData2
                                from res2 in resData2.DefaultIfEmpty()
                                group a by new
                                {
                                    AnJianBelongedTo = res1.BelongedTo, //人员所在安监站
                                    AnJianSuperOrganName = res1.SuperOrganName,
                                    AnJianCityCode = res1.CityCode,
                                    ParentBelongedTo = res2.BelongedTo,//人员所在市
                                    ParentSuperOrganName = res2.SuperOrganName,//市名称
                                    ParentCityCode = res2.CityCode,
                                    FirstCityCode = res2.ParentCityCode
                                    //FirstBelongedTo = res3.BelongedTo,
                                    //FirstSuperOrganName = res3.SuperOrganName,
                                    //FirstCityCode = res3.CityCode,
                                } into g
                                select new JiGouCengJiViemModel
                                {
                                    AnJianBelongedTo = g.Key.AnJianBelongedTo,
                                    AnJianSuperOrganName = g.Key.AnJianSuperOrganName,
                                    AnJianCityCode = g.Key.AnJianCityCode,
                                    ParentBelongedTo = g.Key.ParentBelongedTo,//人员所在市
                                    ParentSuperOrganName = g.Key.ParentSuperOrganName,//市名称
                                    ParentCityCode = g.Key.ParentCityCode,
                                    CityCode = g.Key.FirstCityCode,
                                    //SuperOrganName = g.Key.FirstSuperOrganName,
                                    //SuperOrganCode = g.Key.FirstBelongedTo,
                                    //CityCode = g.Key.FirstCityCode,
                                    UserCount = g.Count()
                                }).OrderBy(g => g.AnJianCityCode).ToList();
                    organizationList.ForEach(x =>
                    {
                        x.UserCount = data.Where(k => k.CityCode == x.CityCode).Select(k => k.UserCount).Sum();

                    });
                    return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, organizationList);
                }
                else if (searchType == 1)
                {
                    //第二级 市信息
                    organizationList = await _context.CityZone.Where(x => x.ParentCityCode == cityCode)
                      .Select(x => new MenuInfoViewModel
                      {
                          QueryHierarchy = 2,
                          Key = x.BelongedTo,
                          Title = x.SuperOrganName,
                          LatitudeCoordinate = x.LatitudeCoordinate,
                          LongitudeCoordinate = x.LongitudeCoordinate,
                          Address = x.Address,
                          MunicipalJurisdiction = x.Flag,
                          CityCode = x.CityCode,
                          ParentCityCode = x.ParentCityCode,
                          IsLeaf = false,
                          IsAddItem = roleName == "超级管理员" ? 1 : 0,
                          CanDelete = 0,
                          UserCount = 0,
                          scopedSlots = scopedSlots
                      }).OrderBy(x => x.CityCode).ToListAsync();

                    //查找二级机构信息
                    var data = (from a in _context.SysUserManager
                                join b in _context.CityZone
                                on a.BelongedTo equals b.BelongedTo
                                into resData1
                                from res1 in resData1.DefaultIfEmpty()
                                join b2 in _context.CityZone
                                on res1.ParentCityCode equals b2.CityCode
                                into resData2
                                from res2 in resData2.DefaultIfEmpty()
                                where res2.ParentCityCode == cityCode

                                group a by new
                                {
                                    AnJianBelongedTo = res1.BelongedTo, //人员所在安监站
                                    AnJianSuperOrganName = res1.SuperOrganName,
                                    AnJianCityCode = res1.CityCode,
                                    ParentBelongedTo = res2.BelongedTo,//人员所在市
                                    ParentSuperOrganName = res2.SuperOrganName,//市名称
                                    ParentCityCode = res2.CityCode,//市的cityCode
                                } into g
                                select new
                                {
                                    AnJianBelongedTo = g.Key.AnJianBelongedTo,
                                    AnJianSuperOrganName = g.Key.AnJianSuperOrganName,
                                    AnJianCityCode = g.Key.AnJianCityCode,
                                    ParentBelongedTo = g.Key.ParentBelongedTo,//人员所在市
                                    ParentSuperOrganName = g.Key.ParentSuperOrganName,//市名称
                                    ParentCityCode = g.Key.ParentCityCode,
                                    UserCount = g.Count()
                                }).OrderBy(x => x.AnJianCityCode);

                    organizationList.ForEach(x =>
                    {
                        x.UserCount = data.Where(k => k.ParentCityCode == x.CityCode).Select(k => k.UserCount).Sum();

                    });


                    return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, organizationList);
                }
                else if (searchType == 2)
                {
                    //第三级 安监站信息
                    organizationList = await _context.CityZone.Where(x => x.ParentCityCode == cityCode)
                      .Select(x => new MenuInfoViewModel
                      {
                          QueryHierarchy = -1,
                          Key = x.BelongedTo,
                          Title = x.SuperOrganName,
                          LatitudeCoordinate = x.LatitudeCoordinate,
                          LongitudeCoordinate = x.LongitudeCoordinate,
                          Address = x.Address,
                          MunicipalJurisdiction = x.Flag,
                          CityCode = x.CityCode,
                          ParentCityCode = x.ParentCityCode,
                          IsLeaf = true,
                          IsAddItem = 0,
                          CanDelete = roleName == "超级管理员" ? 1 : 0,
                          UserCount = 0,
                          scopedSlots = scopedSlots
                      }).OrderBy(x => x.CityCode).ToListAsync();

                    //查找三级机构信息
                    var data = (from a in _context.SysUserManager
                                join b in _context.CityZone
                                on a.BelongedTo equals b.BelongedTo
                                into resData1
                                from res1 in resData1.DefaultIfEmpty()
                                where res1.ParentCityCode == cityCode
                                group a by new
                                {
                                    AnJianBelongedTo = res1.BelongedTo, //人员所在安监站
                                    AnJianSuperOrganName = res1.SuperOrganName,
                                    AnJianCityCode = res1.CityCode,
                                } into g
                                select new
                                {
                                    AnJianBelongedTo = g.Key.AnJianBelongedTo,
                                    AnJianSuperOrganName = g.Key.AnJianSuperOrganName,
                                    AnJianCityCode = g.Key.AnJianCityCode,
                                    UserCount = g.Count()
                                }).OrderBy(x => x.AnJianCityCode);

                    organizationList.ForEach(x =>
                    {
                        x.UserCount = data.Where(k => k.AnJianCityCode == x.CityCode).Select(k => k.UserCount).Sum();

                    });
                    return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, organizationList, organizationList.Count);
                }
                return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.FAIL, "参数错误");
            }
            catch (Exception ex)
            {
                _logger.LogError("组织机构菜单：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

    }
}
