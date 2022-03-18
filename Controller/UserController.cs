using Common;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using JSAJ.Models.Models.Login;
using MCUtil.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ViewModels;

namespace JSAJNanJing.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UserController> _logger;
        private readonly JssanjianmanagerContext _context;
        private readonly Jsgginterface _jsgginterface;
        private JwtSettings settings;
        public UserController(IWebHostEnvironment environment, ILogger<UserController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            settings = options.Value;
        }

        /// <summary>
        /// 操珊珊- 江苏省安全施工管理系统登录(项目,企业,安监站登录)
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<object>> Login([FromBody] LoginViewModel viewModel)
        {

            if (string.IsNullOrWhiteSpace(viewModel.Account)
                || string.IsNullOrWhiteSpace(viewModel.Password)
                  || string.IsNullOrWhiteSpace(viewModel.Type))
            {
                return ResponseViewModel<object>.Create(Status.FAIL, "参数错误", "参数错误");
            }
            try
            {
                DateTime now = DateTime.Now;
                UserInfoViewModel loginInfo = new UserInfoViewModel();
                string tokenResult = "";
                //项目登录
                if (viewModel.Type == "0")
                {
                    // 根据账号，查出用户
                    var userInfoList = await _context.AppUserInfo
                        .Where(w => w.UserPhoneNum == viewModel.Account).ToListAsync();
                    if (userInfoList.Count == 0)
                    {
                        return ResponseViewModel<object>.Create(Status.FAIL, "账号不存在！");
                    }
                    if (userInfoList.Count > 1)
                    {
                        return ResponseViewModel<object>.Create(Status.FAIL, "该手机号存在两个账号,请联系管理员!");
                    }
                    var userInfo = userInfoList[0];
                    if (userInfo.UserPwd != TokenValidate.StrConversionMD5(viewModel.Password).ToUpper())
                    {
                        DateTime time0 = now.AddMinutes(-30);
                        var errorcount = _context.OperationLogs.Where(w => w.CreateDate > time0 && w.LoginUserUuid == userInfo.UserNumber && w.State == "密码错误").Count();
                        if (errorcount >= 4)
                        {
                            OperationLog errorlog = new OperationLog();
                            errorlog.OperationLogId = SecurityManage.GuidUpper();
                            errorlog.platformType = 0;
                            errorlog.LoginUserUuid = userInfo.UserNumber;
                            errorlog.LoginUserName = userInfo.UserName;
                            errorlog.CreateDate = DateTime.Now;
                            errorlog.EventType = "登录";
                            errorlog.State = "密码错误";
                            _context.OperationLogs.Add(errorlog);
                            await _context.SaveChangesAsync();
                            return ResponseViewModel<object>.Create(Status.FAIL, "密码错误，账户已锁定请30分钟之后重试");
                        }
                        else if (errorcount > 0 && errorcount < 4)
                        {
                            OperationLog errorlog = new OperationLog();
                            errorlog.OperationLogId = SecurityManage.GuidUpper();
                            errorlog.platformType = 0;
                            errorlog.LoginUserUuid = userInfo.UserNumber;
                            errorlog.LoginUserName = userInfo.UserName;
                            errorlog.CreateDate = DateTime.Now;
                            errorlog.EventType = "登录";
                            errorlog.State = "密码错误";
                            _context.OperationLogs.Add(errorlog);
                            await _context.SaveChangesAsync();
                            return ResponseViewModel<object>.Create(Status.FAIL, "密码错误，您还有" + (4 - errorcount) + "次重试机会");
                        }
                        else
                        {
                            #region 登录日志
                            OperationLog errorlog = new OperationLog();
                            errorlog.OperationLogId = SecurityManage.GuidUpper();
                            errorlog.platformType = 0;
                            errorlog.LoginUserUuid = userInfo.UserNumber;
                            errorlog.LoginUserName = userInfo.UserName;
                            errorlog.CreateDate = DateTime.Now;
                            errorlog.EventType = "登录";
                            errorlog.State = "密码错误";
                            _context.OperationLogs.Add(errorlog);
                            await _context.SaveChangesAsync();
                            return ResponseViewModel<object>.Create(Status.FAIL, "账号密码错误，请确认后重新输入");
                            #endregion
                        }
                    }

                    var userProInfo = await _context.AppUserProInfo.Where(x => x.UserNumber == userInfo.UserNumber).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                    if (userProInfo == null)
                    {
                        return ResponseViewModel<object>.Create(Status.FAIL, "该账号未绑定任何项目！");
                    }

                    loginInfo.Type = 0;
                    loginInfo.Phone = userInfo.UserPhoneNum;
                    loginInfo.Name = userInfo.UserName;
                    loginInfo.UserId = userInfo.UserNumber;
                    loginInfo.Uuid = loginInfo.UserId;
                    loginInfo.EntType = userInfo.EntType;
                    loginInfo.UserType = userProInfo.UserType;
                    loginInfo.HeadImg = "http://49.4.69.188:7000/xiangmuhead.png";
                    loginInfo.EntName = userProInfo.ConstructionUnit;
                    //loginInfo.ModelProject = new UserBll().GetAppUserInfoLogin(userInfo);
                    loginInfo.RoleName = "项目";
                    if (userProInfo.BelongedTo == "AJ320801-1")
                    {
                        loginInfo.RoleName = "淮安项目";
                    }
                }
                else if (viewModel.Type == "1")
                {
                    //企业登录
                    var userInfo = await _context.EntRegisterInfoMag
                        .Where(w => w.EntCode.Equals(viewModel.Account) && w.EntType == viewModel.EntType).OrderByDescending(x => x.EntCode).FirstOrDefaultAsync();

                    //var userInfoList = await _context.EntRegisterInfoMag
                    //   .Where(w => w.EntCode.Equals(viewModel.Account) && w.EntType == viewModel.EntType).OrderByDescending(x => x.EntCode).ToListAsync();

                    if (userInfo == null)
                    {
                        return ResponseViewModel<object>.Create(Status.FAIL, "账号不存在");
                    }

                    if (userInfo.EntPassWd != TokenValidate.StrConversionMD5(viewModel.Password).ToUpper())
                    {
                        DateTime time0 = now.AddMinutes(-30);
                        var errorcount = _context.OperationLogs.Where(w => w.CreateDate > time0 && w.LoginUserUuid == userInfo.EntCode && w.State == "密码错误").Count();
                        if (errorcount >= 4)
                        {
                            OperationLog errorlog = new OperationLog();
                            errorlog.OperationLogId = SecurityManage.GuidUpper();
                            errorlog.platformType = 1;
                            errorlog.LoginUserUuid = userInfo.EntCode;
                            errorlog.LoginUserName = userInfo.EntName;
                            errorlog.CreateDate = DateTime.Now;
                            errorlog.EventType = "登录";
                            errorlog.State = "密码错误";
                            _context.OperationLogs.Add(errorlog);
                            await _context.SaveChangesAsync();
                            return ResponseViewModel<object>.Create(Status.FAIL, "密码错误，账户已锁定请30分钟之后重试");
                        }
                        else if (errorcount > 0 && errorcount < 4)
                        {
                            OperationLog errorlog = new OperationLog();
                            errorlog.OperationLogId = SecurityManage.GuidUpper();
                            errorlog.platformType = 1;
                            errorlog.LoginUserUuid = userInfo.EntCode;
                            errorlog.LoginUserName = userInfo.EntName;
                            errorlog.CreateDate = DateTime.Now;
                            errorlog.EventType = "登录";
                            errorlog.State = "密码错误";
                            _context.OperationLogs.Add(errorlog);
                            await _context.SaveChangesAsync();
                            return ResponseViewModel<object>.Create(Status.FAIL, "密码错误，您还有" + (4 - errorcount) + "次重试机会");
                        }
                        else
                        {
                            #region 登录日志
                            OperationLog errorlog = new OperationLog();
                            errorlog.OperationLogId = SecurityManage.GuidUpper();
                            errorlog.platformType = 1;
                            errorlog.LoginUserUuid = userInfo.EntCode;
                            errorlog.LoginUserName = userInfo.EntName;
                            errorlog.CreateDate = DateTime.Now;
                            errorlog.EventType = "登录";
                            errorlog.State = "密码错误";
                            _context.OperationLogs.Add(errorlog);
                            await _context.SaveChangesAsync();
                            return ResponseViewModel<object>.Create(Status.FAIL, "账号密码错误，请确认后重新输入");
                            #endregion
                        }

                        //if (userInfoList.Count > 1)
                        //{
                        //    userInfo = userInfoList.Where(x => x.EntPassWd == TokenValidate.StrConversionMD5(viewModel.Password).ToUpper()).FirstOrDefault();
                        //    if (userInfo == null)
                        //    {
                        //        return ResponseViewModel<object>.Create(Status.FAIL, "密码错误");
                        //    }
                        //}
                        //else
                        //{

                        //    return ResponseViewModel<object>.Create(Status.FAIL, "密码错误");
                        //}

                    }
                    //if (userInfo.EntType != "产权单位")
                    //{
                    //    if (userInfo.AccountStatus == "0")
                    //    {
                    //        return ResponseViewModel<object>.Create(Status.FAIL, "请等待审核");
                    //    }
                    //    else if (userInfo.AccountStatus == "2")
                    //    {
                    //        return ResponseViewModel<object>.Create(Status.FAIL, "审核不通过，请重新注册!");
                    //    }

                    //}

                    loginInfo.Type = 1;
                    loginInfo.UserId = userInfo.EntRegisterInfoMagId.ToString();
                    loginInfo.Uuid = loginInfo.UserId;
                    loginInfo.Phone = userInfo.RegisteredMansPhone;
                    loginInfo.Name = userInfo.RegisteredMans;
                    loginInfo.EntCode = userInfo.EntCode;//企业登录才返回的
                    loginInfo.IsOrNotExamine = userInfo.IsOrNotExamine;//企业登录才返回的
                    loginInfo.EntName = userInfo.EntName;
                    loginInfo.EntType = userInfo.EntType;
                    loginInfo.IntelligenceLevelValue = userInfo.IntelligenceLevel.GetHashCode();
                    loginInfo.IntelligenceLevelName = userInfo.IntelligenceLevel.GetHashCode() == 0 ? "" : userInfo.IntelligenceLevel.ToString();
                    loginInfo.HeadImg = "http://49.4.69.188:7000/qiyehead.png";
                    loginInfo.RoleName = loginInfo.EntType;
                }


                #region 获取角色和权限
                var role = await _context.Roles.Where(w => w.Name == loginInfo.RoleName
                && w.Status == 1 && w.Deleted == 0).Select(s => new RolesViewModel
                {
                    Id = s.Id,
                    RoleId = s.RoleId,
                    Deleted = s.Deleted,
                    CreateTime = s.CreateTime,
                    CreatorId = s.CreatorId,
                    Describe = s.Describe,
                    Name = s.Name,
                    Status = s.Status,
                    IsMainRole = s.IsMainRole
                }).FirstOrDefaultAsync();

                if (role == null)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "未配置角色不允许登录");
                }
                var permissions = await _context.Permissions
               .Where(s => s.RoleId == role.RoleId && s.DeleteMark == 0)
               .Select(s => new PermissionViewModel
               {
                   CreateTime = s.CreateTime,
                   DeleteMark = s.DeleteMark,
                   PermissionId = s.PermissionId,
                   Id = s.Id,
                   PermissionName = s.PermissionName,
                   RoleId = s.RoleId
               }).ToListAsync();
                var actionss = await _context.ActionWithPermissions
                         .Where(w => w.RoleId == role.RoleId
                        && permissions.Select(k => k.PermissionId).Contains(w.PermissionId) && w.DeleteMark == 0)
                         .ToListAsync();
                permissions.ForEach(s =>
                {
                    s.Actions = actionss.Where(k => k.PermissionId == s.PermissionId).ToList();

                });
                if (permissions == null || permissions.Count == 0)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "未配置权限不允许登录");
                }
                role.Permissions = permissions;
                loginInfo.Role = role;
                #endregion

                //拿到jwt的key值，进行一次加密
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var claims = new Claim[] {
                        new Claim(nameof(ClaimTypeEnum.UserId),loginInfo.UserId),
                        new Claim(nameof(ClaimTypeEnum.Uuid),loginInfo.Uuid),
                        new Claim(ClaimsIdentity.DefaultNameClaimType,loginInfo.Name??""),
                        new Claim(nameof(ClaimTypeEnum.Phone),loginInfo.Phone??""),
                        new Claim(nameof(ClaimTypeEnum.EntType),loginInfo.EntType??""),
                        new Claim(nameof(ClaimTypeEnum.Type),loginInfo.Type.ToString()),
                        new Claim(nameof(ClaimTypeEnum.EntCode),loginInfo.EntCode??""),
                        new Claim(nameof(ClaimTypeEnum.IsOrNotExamine),loginInfo.IsOrNotExamine.ToString()),
                        new Claim(ClaimsIdentity.DefaultRoleClaimType,loginInfo.RoleName.ToString())
                     };
                var token = new JwtSecurityToken(
                                settings.Issuer,
                                settings.Audience,
                                claims,
                                DateTime.Now,
                                DateTime.Now.AddDays(1),
                                creds);
                tokenResult = new JwtSecurityTokenHandler().WriteToken(token);


                #region 登录日志
                OperationLog newlog = new OperationLog();
                newlog.OperationLogId = SecurityManage.GuidUpper();
                newlog.platformType = loginInfo.Type;
                newlog.LoginUserUuid = loginInfo.Uuid;
                newlog.LoginUserName = loginInfo.Name;
                newlog.CreateDate = DateTime.Now;
                newlog.EventType = "登录";
                newlog.State = "成功";
                _context.OperationLogs.Add(newlog);
                await _context.SaveChangesAsync();
                #endregion

                return ResponseViewModel<object>.Create(0, "登录成功", loginInfo, 0, "bearer " + tokenResult);
            }
            catch (Exception ex)
            {
                _logger.LogError("用户登陆：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        ///  江苏省安全施工管理系统登录安监站登录/检测所账号登录
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<object>> OrgLogin([FromBody] LoginViewModel viewModel)
        {

            if (string.IsNullOrWhiteSpace(viewModel.Account)
                || string.IsNullOrWhiteSpace(viewModel.Password)
                || string.IsNullOrWhiteSpace(viewModel.Type)
                  )
            {
                return ResponseViewModel<object>.Create(Status.FAIL, "参数错误", "参数错误");
            }
            try
            {
                SysUserInfoViewModel loginInfo = new SysUserInfoViewModel();
                DateTime now = DateTime.Now;
                List<RolesViewModel> roles = null;
                //安监站登录或者检测所登录
                if (viewModel.Type == "2")
                {
                    // 根据账号，查出检测所账户用户
                    var jcsUser = await _context.TestingInstituteUser
                        .Where(w => w.Account == viewModel.Account && w.DeleteTag == 0).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                    // 根据账号，查出用户
                    var userInfoList = await _context.SysUserManager
                        .Where(w => w.UserPhone == viewModel.Account && w.DeleteMark == 0 && w.AccountType == 0).ToListAsync();

                    if (jcsUser == null && userInfoList.Count == 0)
                    {
                        return ResponseViewModel<object>.Create(Status.FAIL, "账号不存在");
                    }
                    else if (jcsUser != null)
                    {

                        // 根据账号，查出用户
                        var testingInstituteInfo = await _context.TestingInstituteInfo
                            .Where(w => w.TestingInstituteInfoId == jcsUser.TestingInstituteInfoId).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                        if (testingInstituteInfo == null || testingInstituteInfo.EndDateTime == null
                            || testingInstituteInfo.EndDateTime < DateTime.Today
                            || testingInstituteInfo.BeginDateTime == null
                            || testingInstituteInfo.BeginDateTime > DateTime.Today)
                        {
                            return ResponseViewModel<object>.Create(Status.FAIL, "所属检测单位未设置有效期或不在有效期内！请联系省建筑业协会建筑安全设备分会，025-51865728!");
                        }

                        //if (jcsUser.State == 0)
                        //{
                        //    return ResponseViewModel<object>.Create(Status.FAIL, "账号已失效");
                        //}
                        /*else*/
                        if (jcsUser.UserPwd != TokenValidate.StrConversionMD5(viewModel.Password).ToUpper())
                        {
                            return ResponseViewModel<object>.Create(Status.FAIL, "密码错误");
                        }

                        #region 检测所账号
                        loginInfo.Type = 3;
                        loginInfo.UserSearchLevelType = 100;
                        loginInfo.BelongedTo = testingInstituteInfo.BelongedTo;
                        loginInfo.Phone = jcsUser.Account;
                        loginInfo.Name = jcsUser.TestingInstituteUserName;
                        loginInfo.UserId = jcsUser.TestingInstituteUserId;
                        loginInfo.Uuid = jcsUser.TestingInstituteUserId;
                        loginInfo.TestingInstituteInfoId = jcsUser.TestingInstituteInfoId;
                        loginInfo.HeadImg = "https://timgsa.baidu.com/timg?image&quality=80&size=b9999_10000&sec=1571926953190&di=05c411b549ef672dfdb751648c549a46&imgtype=0&src=http%3A%2F%2Fb-ssl.duitang.com%2Fuploads%2Fitem%2F201706%2F27%2F20170627214911_iTuCQ.jpeg";
                        loginInfo.SuperOrganName = testingInstituteInfo.MechanismName;
                        roles = await _context.Roles.Where(w => w.Status == 1 && w.Deleted == 0 && w.RoleId == jcsUser.RoleId).Select(s => new RolesViewModel
                        {
                            Id = s.Id,
                            RoleId = s.RoleId,
                            Deleted = s.Deleted,
                            CreateTime = s.CreateTime,
                            CreatorId = s.CreatorId,
                            Describe = s.Describe,
                            Name = s.Name,
                            Status = s.Status,
                            IsMainRole = s.IsMainRole,
                            SupervisionDepartmentId = s.SupervisionDepartmentId
                        }).ToListAsync();

                        if (roles.Count == 0)
                        {
                            return ResponseViewModel<object>.Create(Status.FAIL, "暂未分配系统角色,请联系管理员");
                        }
                        var permissionsAll = await _context.Permissions
           .Where(s => s.DeleteMark == 0 && roles.Select(p => p.RoleId).Contains(s.RoleId))
           .Select(s => new PermissionViewModel
           {
               CreateTime = s.CreateTime,
               DeleteMark = s.DeleteMark,
               PermissionId = s.PermissionId,
               Id = s.Id,
               PermissionName = s.PermissionName,
               RoleId = s.RoleId
           }).ToListAsync();
                        if (permissionsAll.Count == 0)
                        {
                            return ResponseViewModel<object>.Create(Status.FAIL, "未配置权限不允许登录,请联系管理员");
                        }
                        var actionss = await _context.ActionWithPermissions
                         .Where(w => roles.Select(p => p.RoleId).Contains(w.RoleId)
                         && w.DeleteMark == 0).ToListAsync();
                        roles.ForEach(k =>
                        {
                            k.Permissions = permissionsAll.Where(y => y.RoleId == k.RoleId).ToList();
                            if (k.Permissions.Count > 0)
                            {
                                k.Permissions.ForEach(s =>
                                {
                                    s.Actions = actionss.Where(k => k.PermissionId == s.PermissionId).ToList();
                                });
                            }

                            //拿到jwt的key值，进行一次加密
                            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
                            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                            var claims = new Claim[] {
                                                        new Claim(nameof(ClaimTypeEnum.UserId),loginInfo.UserId),
                                                        new Claim(nameof(ClaimTypeEnum.Uuid),loginInfo.Uuid),
                                                        new Claim(ClaimsIdentity.DefaultNameClaimType,loginInfo.Name.ToString()),
                                                        new Claim(nameof(ClaimTypeEnum.Phone),loginInfo.Phone??""),
                                                        new Claim(nameof(ClaimTypeEnum.EntType),loginInfo.EntType??""),
                                                        new Claim(nameof(ClaimTypeEnum.Type),loginInfo.Type.ToString()),
                                                        new Claim(nameof(ClaimTypeEnum.UserSearchLevelType),loginInfo.UserSearchLevelType==null?"":loginInfo.UserSearchLevelType.ToString()),
                                                        new Claim(nameof(ClaimTypeEnum.MunicipalJurisdiction ),loginInfo.MunicipalJurisdiction==null?"":loginInfo.MunicipalJurisdiction.ToString()),
                                                        new Claim(nameof(ClaimTypeEnum.BelongedTo),loginInfo.BelongedTo??""),
                                                        new Claim(nameof(ClaimTypeEnum.Department),loginInfo.Department??""),
                                                        new Claim(nameof(ClaimTypeEnum.Station),loginInfo.Station??""),
                                                        new Claim(nameof(ClaimTypeEnum.RoleId),k.RoleId??""),
                                                        new Claim(ClaimsIdentity.DefaultRoleClaimType,k.Name.ToString()),
                                                        new Claim(nameof(ClaimTypeEnum.SupervisionDepartmentId),k.SupervisionDepartmentId??""),
                                                        new Claim(nameof(ClaimTypeEnum.TestingInstituteInfoId),loginInfo.TestingInstituteInfoId??""),
                                                     };
                            var token = new JwtSecurityToken(
                                            settings.Issuer,
                                            settings.Audience,
                                            claims,
                                            DateTime.Now,
                                            DateTime.Now.AddDays(1),
                                            creds);
                            k.Token = "bearer " + new JwtSecurityTokenHandler().WriteToken(token); ;

                        });
                        loginInfo.RoleList = roles;

                        #region 登录日志
                        OperationLog newlog = new OperationLog();
                        newlog.OperationLogId = SecurityManage.GuidUpper();
                        newlog.platformType = loginInfo.Type;
                        newlog.LoginUserUuid = loginInfo.Uuid;
                        newlog.LoginUserName = loginInfo.Name;
                        newlog.BelongedTo = loginInfo.BelongedTo;
                        newlog.ParentBelongedTo = loginInfo.ParentBelongedTo;
                        newlog.CreateDate = now;
                        newlog.EventType = "检测所登录";
                        newlog.State = "成功";
                        _context.OperationLogs.Add(newlog);
                        await _context.SaveChangesAsync();
                        #endregion

                        return ResponseViewModel<object>.Create(Status.SUCCESS, "登录成功", loginInfo);
                        #endregion
                    }
                    else
                    {
                        var userInfo = userInfoList[0];
                        if (userInfo.UserPwd != TokenValidate.StrConversionMD5(viewModel.Password).ToUpper())
                        {
                            return ResponseViewModel<object>.Create(Status.FAIL, "密码错误");
                        }
                        var cityZone = await _context.CityZone.Where(x => x.BelongedTo == userInfo.BelongedTo).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                        #region 安监站账号
                        loginInfo.Type = 2;
                        loginInfo.BelongedTo = userInfo.BelongedTo;
                        loginInfo.Phone = userInfo.UserPhone;
                        loginInfo.Name = userInfo.UserName;
                        loginInfo.SuperOrganName = cityZone.SuperOrganName;
                        loginInfo.Abbreviation = cityZone.Abbreviation;
                        if (userInfo.BelongedTo == "AJ320801-1" && userInfo.GroupName == "分管局长")
                        {
                            loginInfo.SuperOrganName = "淮安市住房和城乡建设局";
                            loginInfo.Abbreviation = "淮安市住房和城乡建设局";
                        }
                        //if (userInfo.BelongedTo == "AJ321011-1")
                        //{
                        //    loginInfo.SuperOrganName = "扬州经济技术开发区安全监督站";
                        //}
                        loginInfo.UserId = userInfo.Id.ToString();
                        loginInfo.Uuid = userInfo.Uuid.ToString();
                        loginInfo.Department = userInfo.Department;
                        loginInfo.Station = userInfo.Station;
                        loginInfo.DustState = userInfo.DustState;
                        if (!string.IsNullOrWhiteSpace(userInfo.Photo))
                        {
                            loginInfo.HeadImg = userInfo.Photo;
                        }
                        else
                        {
                            loginInfo.HeadImg = "https://timgsa.baidu.com/timg?image&quality=80&size=b9999_10000&sec=1571926953190&di=05c411b549ef672dfdb751648c549a46&imgtype=0&src=http%3A%2F%2Fb-ssl.duitang.com%2Fuploads%2Fitem%2F201706%2F27%2F20170627214911_iTuCQ.jpeg";
                        }
                        if (loginInfo.BelongedTo == "AJ320000-1")
                        {
                            loginInfo.UserSearchLevelType = 0;
                        }
                        else
                        {
                            var cityAndParentCityInfo = await (from a in _context.CityZone
                                                               join b in _context.CityZone
                                                               on a.ParentCityCode equals b.CityCode
                                                               where a.BelongedTo == loginInfo.BelongedTo
                                                               select new
                                                               {
                                                                   BelongedTo = a.BelongedTo,
                                                                   CityCode = a.CityCode,
                                                                   MunicipalJurisdiction = a.Flag,
                                                                   LatitudeCoordinate = a.LatitudeCoordinate,
                                                                   LongitudeCoordinate = a.LongitudeCoordinate,
                                                                   SuperOrganName = a.SuperOrganName,
                                                                   ParentLatitudeCoordinate = b.LatitudeCoordinate,
                                                                   ParentLongitudeCoordinate = b.LongitudeCoordinate,
                                                                   ParentSuperOrganName = b.SuperOrganName,
                                                                   ParentBelongedTo = b.BelongedTo,
                                                                   SearchType = b.BelongedTo == "AJ320000-1" ? 1 : 2,
                                                                   ParentCityCode = b.CityCode
                                                               }).FirstOrDefaultAsync();

                            loginInfo.SuperOrganName = cityAndParentCityInfo.SuperOrganName;
                            loginInfo.LatitudeCoordinate = cityAndParentCityInfo.LatitudeCoordinate;
                            loginInfo.ParentBelongedTo = cityAndParentCityInfo.ParentBelongedTo;
                            loginInfo.LongitudeCoordinate = cityAndParentCityInfo.LongitudeCoordinate;
                            loginInfo.ParentSuperOrganName = cityAndParentCityInfo.ParentSuperOrganName;
                            loginInfo.ParentLatitudeCoordinate = cityAndParentCityInfo.ParentLatitudeCoordinate;
                            loginInfo.ParentLongitudeCoordinate = cityAndParentCityInfo.ParentLongitudeCoordinate;
                            if (cityAndParentCityInfo.ParentBelongedTo == "AJ320000-1")
                            {
                                //市辖安监站领导岗角色或者市级领导
                                loginInfo.UserSearchLevelType = 1;
                                loginInfo.MunicipalJurisdiction = cityAndParentCityInfo.MunicipalJurisdiction;

                            }
                            else
                            {
                                loginInfo.UserSearchLevelType = 2;
                                loginInfo.MunicipalJurisdiction = cityAndParentCityInfo.MunicipalJurisdiction;
                            }
                        }

                        roles = await (from a in _context.UserRoles
                                       join b in _context.Roles
                                       on a.RoleId equals b.RoleId
                                       join c in _context.SupervisionDepartment
                                       on b.SupervisionDepartmentId equals c.SupervisionDepartmentId
                                       into t0
                                       from c0 in t0.DefaultIfEmpty()
                                       where a.SysUserManagerUuid == loginInfo.Uuid && b.Status == 1 && b.Deleted == 0
                                       select new RolesViewModel
                                       {
                                           Id = b.Id,
                                           RoleId = b.RoleId,
                                           Deleted = b.Deleted,
                                           CreateTime = b.CreateTime,
                                           CreatorId = b.CreatorId,
                                           Describe = b.Describe,
                                           IsOwner = b.IsOwner,
                                           Name = b.Name,
                                           Status = b.Status,
                                           IsMainRole = b.IsMainRole,
                                           SupervisionDepartmentId = b.SupervisionDepartmentId,
                                           SupervisionDepartmentName = c0.Name
                                       }).OrderByDescending(x => x.IsMainRole).ThenByDescending(x => x.IsOwner).ToListAsync();

                        if (roles.Count == 0)
                        {
                            return ResponseViewModel<object>.Create(Status.FAIL, "暂未分配系统角色,请联系管理员");
                        }
                        var permissionsAll = await _context.Permissions
                                                   .Where(s => s.DeleteMark == 0 && roles.Select(p => p.RoleId).Contains(s.RoleId))
                                                   .Select(s => new PermissionViewModel
                                                   {
                                                       CreateTime = s.CreateTime,
                                                       DeleteMark = s.DeleteMark,
                                                       PermissionId = s.PermissionId,
                                                       Id = s.Id,
                                                       PermissionName = s.PermissionName,
                                                       RoleId = s.RoleId
                                                   }).ToListAsync();
                        if (permissionsAll.Count == 0)
                        {
                            return ResponseViewModel<object>.Create(Status.FAIL, "未配置权限不允许登录,请联系管理员");
                        }
                        var actionss = await _context.ActionWithPermissions
                         .Where(w => roles.Select(p => p.RoleId).Contains(w.RoleId)
                         && w.DeleteMark == 0).ToListAsync();
                        roles.ForEach(k =>
                        {
                            k.Permissions = permissionsAll.Where(y => y.RoleId == k.RoleId).ToList();
                            if (k.Permissions.Count > 0)
                            {
                                k.Permissions.ForEach(s =>
                                {
                                    s.Actions = actionss.Where(k => k.PermissionId == s.PermissionId).ToList();
                                });
                            }

                            //拿到jwt的key值，进行一次加密
                            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
                            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                            var claims = new Claim[] {
                                                        new Claim(nameof(ClaimTypeEnum.UserId),loginInfo.UserId),
                                                        new Claim(nameof(ClaimTypeEnum.Uuid),loginInfo.Uuid),
                                                        new Claim(ClaimsIdentity.DefaultNameClaimType,loginInfo.Name.ToString()),
                                                        new Claim(nameof(ClaimTypeEnum.Phone),loginInfo.Phone??""),
                                                        new Claim(nameof(ClaimTypeEnum.EntType),loginInfo.EntType??""),
                                                        new Claim(nameof(ClaimTypeEnum.Type),loginInfo.Type.ToString()),
                                                        new Claim(nameof(ClaimTypeEnum.UserSearchLevelType),loginInfo.UserSearchLevelType==null?"":loginInfo.UserSearchLevelType.ToString()),
                                                        new Claim(nameof(ClaimTypeEnum.MunicipalJurisdiction ),loginInfo.MunicipalJurisdiction==null?"":loginInfo.MunicipalJurisdiction.ToString()),
                                                        new Claim(nameof(ClaimTypeEnum.BelongedTo),loginInfo.BelongedTo??""),
                                                        new Claim(nameof(ClaimTypeEnum.Department),k.SupervisionDepartmentName??"站领导"),
                                                        new Claim(nameof(ClaimTypeEnum.Station),loginInfo.Station??""),
                                                        new Claim(nameof(ClaimTypeEnum.RoleId),k.RoleId??""),
                                                        new Claim(nameof(ClaimTypeEnum.SuperOrganName ),loginInfo.SuperOrganName??""),
                                                        new Claim(nameof(ClaimTypeEnum.LatitudeCoordinate ),loginInfo.LatitudeCoordinate??""),
                                                        new Claim(nameof(ClaimTypeEnum.LongitudeCoordinate ),loginInfo.LongitudeCoordinate??""),
                                                        new Claim(nameof(ClaimTypeEnum.ParentSuperOrganName ),loginInfo.ParentSuperOrganName??""),
                                                        new Claim(nameof(ClaimTypeEnum.ParentLatitudeCoordinate ),loginInfo.ParentLatitudeCoordinate??""),
                                                        new Claim(nameof(ClaimTypeEnum.ParentLongitudeCoordinate ),loginInfo.ParentLongitudeCoordinate??""),
                                                        new Claim(ClaimsIdentity.DefaultRoleClaimType,k.Name.ToString()),
                                                        new Claim(nameof(ClaimTypeEnum.SupervisionDepartmentId),k.SupervisionDepartmentId??""),
                                                        new Claim(nameof(ClaimTypeEnum.TestingInstituteInfoId),loginInfo.TestingInstituteInfoId??""),
                                                     };
                            var token = new JwtSecurityToken(
                                            settings.Issuer,
                                            settings.Audience,
                                            claims,
                                            DateTime.Now,
                                            DateTime.Now.AddDays(1),
                                            creds);
                            k.Token = "bearer " + new JwtSecurityTokenHandler().WriteToken(token);

                        });
                        loginInfo.RoleList = roles;
                        //loginInfo.DustState=

                        #region 登录日志
                        OperationLog newlog = new OperationLog();
                        newlog.OperationLogId = SecurityManage.GuidUpper();
                        newlog.platformType = loginInfo.Type;
                        newlog.LoginUserUuid = loginInfo.Uuid;
                        newlog.LoginUserName = loginInfo.Name;
                        newlog.BelongedTo = loginInfo.BelongedTo;
                        newlog.ParentBelongedTo = loginInfo.ParentBelongedTo;
                        newlog.CreateDate = now;
                        newlog.EventType = "登录";
                        newlog.State = "成功";
                        _context.OperationLogs.Add(newlog);
                        await _context.SaveChangesAsync();
                        #endregion
                        return ResponseViewModel<object>.Create(Status.SUCCESS, "登录成功", loginInfo);
                        #endregion
                    }
                }
                return ResponseViewModel<object>.Create(Status.FAIL, "登录失败,访问类型错误!");
            }
            catch (Exception ex)
            {
                _logger.LogError("用户登陆：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }
    }
}
