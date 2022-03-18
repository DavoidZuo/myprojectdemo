using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Common.DataBll;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using JSAJ.Models.Models;
using MCUtil.DBS;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using ViewModels;

namespace JSAJ.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class TestingInstituteController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MenusController> _logger;
        private readonly JssanjianmanagerContext _context;
        private JwtSettings settings;
        public TestingInstituteController(IWebHostEnvironment environment, ILogger<MenusController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            settings = options.Value;
        }


        /// <summary>
        /// 新增/修改检测机构
        /// </summary>
        /// <param name="belongedTo">机构编号</param>
        /// <param name="organizationLevel">0-第一级 1第二级 2第三级</param>
        /// <returns></returns>
        //[Authorize(Roles = "超级管理员")]
        [HttpPost]
        public async Task<ResponseViewModel<object>> EditTestingInstitute([FromBody] TestingInstituteInfo viemModel)
        {
            try
            {
                if (viemModel != null
                    && !string.IsNullOrWhiteSpace(viemModel.BelongedTo)
                    && !string.IsNullOrWhiteSpace(viemModel.MechanismName)
                    && !string.IsNullOrWhiteSpace(viemModel.MechanismNumber)
                    && !string.IsNullOrWhiteSpace(viemModel.OrganizationCode)
                    && !string.IsNullOrWhiteSpace(viemModel.CityCode)
                    && !string.IsNullOrWhiteSpace(viemModel.Address))
                {

                    bool isEdit = false;
                    if (!string.IsNullOrWhiteSpace(viemModel.TestingInstituteInfoId))
                    {
                        isEdit = true;
                    }
                    DateTime nowTime = DateTime.Now;
                    if (isEdit)
                    {
                        var data = await _context.TestingInstituteInfo.Where(x => x.TestingInstituteInfoId == viemModel.TestingInstituteInfoId).FirstOrDefaultAsync();
                        data.UpdateDate = nowTime;
                        data.DeleteTag = 0;
                        data.CityCode = viemModel.CityCode;
                        data.MechanismName = viemModel.MechanismName;
                        data.MechanismNumber = viemModel.MechanismNumber;
                        data.BelongedTo = viemModel.BelongedTo;
                        data.MechanismNumber = viemModel.MechanismNumber;
                        data.MechanismName = viemModel.MechanismName;
                        data.Address = viemModel.Address;
                        data.PostalCode = viemModel.PostalCode;
                        data.TechnicalDirector = viemModel.TechnicalDirector;
                        data.TechnicalTitle = viemModel.TechnicalTitle;
                        data.LegalPerson = viemModel.LegalPerson;
                        data.LegalPersonPhone = viemModel.LegalPersonPhone;
                        //data.BusinessLicenseNumber = viemModel.BusinessLicenseNumber;
                        data.OrganizationCode = viemModel.OrganizationCode;
                        //data.TaxRegistrationNumber = viemModel.TaxRegistrationNumber;
                        data.InspectionScope = viemModel.InspectionScope;
                        data.BeginDateTime = viemModel.BeginDateTime;
                        data.EndDateTime = viemModel.EndDateTime;
                        _context.TestingInstituteInfo.Update(data);
                    }

                    else
                    {
                        var data = await _context.TestingInstituteInfo.Where(x => x.OrganizationCode == viemModel.OrganizationCode && x.TestingInstituteInfoId != viemModel.TestingInstituteInfoId).FirstOrDefaultAsync();
                        if (data != null)
                            return ResponseViewModel<object>.Create(Status.FAIL, "信息已重复", "信息已重复");
                        viemModel.TestingInstituteInfoId = SecurityManage.GuidUpper();
                        viemModel.CreateDate = nowTime;
                        viemModel.UpdateDate = nowTime;
                        await _context.TestingInstituteInfo.AddAsync(viemModel);

                    }
                    await _context.SaveChangesAsync();
                    return ResponseViewModel<object>.Create(Status.SUCCESS, "保存成功", "保存成功");
                }
                else
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "参数错误", "参数错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("编辑检测机构信息OrganizationController/EditTestingInstitute：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, "检测机构信息保存失败", "检测机构信息保存失败");
            }
        }

        /// <summary>
        /// 检测机构回显数据
        /// </summary>
        /// <param name="belongedTo">机构编号</param>
        /// <param name="organizationLevel">0-第一级 1第二级 2第三级</param>
        /// <returns></returns>
        //[Authorize(Roles = "超级管理员")]
        [HttpGet]
        public async Task<ResponseViewModel<TestingInstituteInfo>> GetTestingInstituteInfo(string testingInstituteInfoId)
        {
            try
            {

                if (!string.IsNullOrEmpty(testingInstituteInfoId))
                {
                    //查询机构
                    TestingInstituteInfo testingInstituteInfo = await _context.TestingInstituteInfo.Where(x => x.TestingInstituteInfoId == testingInstituteInfoId).FirstOrDefaultAsync();
                    if (testingInstituteInfo == null)
                    { return ResponseViewModel<TestingInstituteInfo>.Create(Status.FAIL, "检测机构不存在"); }

                    return ResponseViewModel<TestingInstituteInfo>.Create(Status.SUCCESS, Message.SUCCESS, testingInstituteInfo);
                }
                else
                {

                    return ResponseViewModel<TestingInstituteInfo>.Create(Status.FAIL, "参数错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("检测机构回显数据TestingInstitute/GetTestingInstituteInfo：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<TestingInstituteInfo>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 删除检测机构
        /// </summary>
        /// <param name="belongedTo">机构编号</param>
        /// <param name="organizationLevel">0-第一级 1第二级 2第三级</param>
        /// <returns></returns>
        //[Authorize(Roles = "超级管理员")]
        [HttpGet]
        public async Task<ResponseViewModel<object>> DelTestingInstitute(string mechanismUuid)
        {
            try
            {

                if (!string.IsNullOrEmpty(mechanismUuid))
                {
                    //查询机构
                    TestingInstituteInfo testingInstituteInfo = await _context.TestingInstituteInfo.Where(x => x.TestingInstituteInfoId == mechanismUuid).FirstOrDefaultAsync();
                    if (testingInstituteInfo != null)
                    {

                        testingInstituteInfo.DeleteTag = 1;
                        _context.TestingInstituteInfo.Update(testingInstituteInfo);
                        await _context.SaveChangesAsync();
                        return ResponseViewModel<object>.Create(Status.SUCCESS, "检测机构删除成功", "检测机构删除成功");
                    }
                    else
                    {
                        return ResponseViewModel<object>.Create(Status.FAIL, "检测机构不存在", "检测机构不存在");
                    }
                }
                else
                {

                    return ResponseViewModel<object>.Create(Status.FAIL, "参数错误", "参数错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("检测机构删除TestingInstitute/DelTestingInstitute：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, "检测机构删除失败", "检测机构删除失败");
            }
        }


        /// <summary>
        /// 添加人员信息/修改人员信息
        /// </summary>
        /// <param name="viewModel">修改的话多传一个人员id</param>
        /// <returns></returns>
        //[Authorize]
        [HttpPost]
        public async Task<ResponseViewModel<object>> UpdateUserInfo([FromBody] TestingInstituteUser viewModel)
        {
            try
            {
                if (viewModel == null
                    || string.IsNullOrWhiteSpace(viewModel.TestingInstituteInfoId)
                    || string.IsNullOrWhiteSpace(viewModel.Sex)
                    || string.IsNullOrWhiteSpace(viewModel.UserPwd)
                    || string.IsNullOrWhiteSpace(viewModel.Account)
                    || (viewModel.State != 0 && viewModel.State != 1))
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "参数错误！");
                }
                DateTime nowTime = DateTime.Now;

                var role = User.FindFirst(nameof(ClaimsIdentity.RoleClaimType))?.Value;//当前人角色
                var makeBelongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;//当前操作人的BelongedTo

                var roleId = await _context.Roles.Where(x => x.Name == "检测所" && x.Deleted == 0)
                   .OrderByDescending(x => x.Id).Select(x => x.RoleId).FirstOrDefaultAsync();//查询检测所的角色id
                if (string.IsNullOrWhiteSpace(viewModel.TestingInstituteUserId))
                {
                    TestingInstituteUser user = await _context.TestingInstituteUser.Where(x => x.Account == viewModel.Account).FirstOrDefaultAsync();
                    if (user != null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "信息已存在！");
                    }

                    //判断手机号是否重复
                    SysUserManager userInfo = await _context.SysUserManager.Where(x => x.UserPhone == viewModel.Account && x.DeleteMark == 0).OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync();
                    if (userInfo != null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "该账号已注册后台账号！", "该账号已注册后台账号！");
                    }

                    viewModel.TestingInstituteUserId = SecurityManage.GuidUpper();
                    viewModel.UserPwd = SecurityManage.StringToMD5(viewModel.UserPwd);
                    viewModel.DeleteTag = 0;
                    viewModel.RoleId = roleId;
                    viewModel.CreateDate = nowTime;
                    viewModel.UpdateDate = nowTime;
                    await _context.TestingInstituteUser.AddAsync(viewModel);
                    await _context.SaveChangesAsync();
                    return ResponseViewModel<object>.Create(Status.SUCCESS, "信息修改成功", "信息修改成功");

                }
                else
                {
                    TestingInstituteUser user = await _context.TestingInstituteUser.Where(x => x.TestingInstituteUserId == viewModel.TestingInstituteUserId).FirstOrDefaultAsync();
                    if (user == null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "信息不存在！");
                    }
                    //判断账号和别人的是否一致
                    TestingInstituteUser isAccountExist = await _context.TestingInstituteUser.Where(x => x.Account == viewModel.Account && x.TestingInstituteUserId != viewModel.TestingInstituteUserId).FirstOrDefaultAsync();
                    if (isAccountExist != null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "手机号已被其他账户占用！");
                    }
                    user.Sex = viewModel.Sex;
                    user.TestingInstituteInfoId = viewModel.TestingInstituteInfoId;
                    user.TestingInstituteUserName = viewModel.TestingInstituteUserName;
                    user.Account = viewModel.Account;
                    user.UpdateDate = DateTime.Now;
                    user.State = viewModel.State;
                    user.DeleteTag = 0;
                    user.RoleId = roleId;
                    user.UserPwd = SecurityManage.StringToMD5(viewModel.UserPwd);
                    user.JobTitle = viewModel.JobTitle;
                    _context.TestingInstituteUser.Update(user);
                    await _context.SaveChangesAsync();
                    return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "信息保存成功");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("检测时账号编辑TestingInstitute/UpdateUserInfo：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, "信息保存失败");
            }
        }

        /// <summary>
        /// 删除检测所账号信息
        /// </summary>
        /// <param name="viewModel">修改的话多传一个人员id</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<object>> DelUserInfo(List<string> testingInstituteUserId)
        {
            try
            {
                if (testingInstituteUserId == null || testingInstituteUserId.Count == 0)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "参数错误！");
                }
                var role = User.FindFirst(nameof(ClaimsIdentity.RoleClaimType))?.Value;//当前人角色
                var makeBelongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;//当前操作人的BelongedTo

                var userList = await _context.TestingInstituteUser.Where(x => testingInstituteUserId.Contains
                (x.TestingInstituteUserId)).ToListAsync();
                if (userList == null || userList.Count == 0)
                {
                    return ResponseViewModel<object>.Create(Status.ERROR, "信息不存在！");
                }
                userList.ForEach(x =>
                {
                    x.DeleteTag = 1;
                });
                _context.TestingInstituteUser.UpdateRange(userList);
                await _context.SaveChangesAsync();
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "删除成功");

            }
            catch (Exception ex)
            {
                _logger.LogError("检测所账号删除TestingInstitute/DelUserInfo：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, "检测所账号删除失败");
            }
        }

        /// <summary>
        /// css-左侧检测机构列表
        /// </summary>
        /// <param name="belongedTo">0省第一级 1第二级  3第三级</param>
        /// <param name="searchType"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<MenuInfoViewModel>>> GetOrganizationList(string cityCode, string belongedTo, int searchType)
        {
            try
            {
                ScopedSlotsViewModel scopedSlots = new ScopedSlotsViewModel();
                scopedSlots.Title = "custom";
                List<MenuInfoViewModel> organizationList = new List<MenuInfoViewModel>();

                var cityList = await _context.CityZone.ToListAsync();
                if (searchType == 0)
                {
                    //查找一级机构信息
                    var data = cityList.Where(x => string.IsNullOrWhiteSpace(x.ParentCityCode)).Select(x => new MenuInfoViewModel
                    {
                        Title = x.CityShortName,
                        Key = x.BelongedTo,
                        IsLeaf = false,
                        BelongedTo = x.BelongedTo,
                        CityCode = x.CityCode,
                        IsTestingInstitute = 0,
                        QueryHierarchy = 1,
                        IsAddTestingInstitute = 0,
                        scopedSlots = scopedSlots
                    }).ToList();
                    return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, data);
                }
                else if (searchType == 1)
                {
                    //查第二级
                    //var shi = cityList.Where(x => x.ParentCityCode == cityCode).Select(x => x.CityCode).ToList();


                    //var testingInstituteInfoList = await _context.TestingInstituteInfo.Where(x => x.DeleteTag == 0 && x.State == 1).ToListAsync();

                    //查找二级机构信息
                    List<MenuInfoViewModel> list = new List<MenuInfoViewModel>();
                    var data1 = cityList.Where(x => x.ParentCityCode == cityCode).Select(x => new MenuInfoViewModel
                    {
                        Title = x.SuperOrganName.Replace("江苏省", ""),
                        Key = x.BelongedTo,
                        IsLeaf = false,
                        BelongedTo = x.BelongedTo,
                        CityCode = x.CityCode,
                        IsTestingInstitute = 0,
                        QueryHierarchy = 2,
                        IsAddTestingInstitute = 1,
                        scopedSlots = scopedSlots
                    }).ToList();
                    list.AddRange(data1);

                    return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, list);
                }
                else if (searchType == 2)
                {

                    var testingInstituteInfoList = await _context.TestingInstituteInfo.Where(x => x.DeleteTag == 0).ToListAsync();

                    //查找第三级机构信息
                    List<MenuInfoViewModel> list = new List<MenuInfoViewModel>();

                    var testingInstituteInfoRange = _context.TestingInstituteInfo.Where(x => x.BelongedTo == belongedTo && x.DeleteTag == 0).Select(x => new MenuInfoViewModel
                    {
                        Title = x.MechanismName,
                        Key = x.TestingInstituteInfoId,
                        TestingInstituteInfoId = x.TestingInstituteInfoId,
                        IsLeaf = true,
                        BelongedTo = x.BelongedTo,
                        QueryHierarchy = -1,
                        IsTestingInstitute = 1,
                        IsAddTestingInstitute = 0,
                        scopedSlots = scopedSlots
                    }).ToList();
                    list.AddRange(testingInstituteInfoRange);

                    return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, list);
                }
                return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.FAIL, "参数错误");
            }
            catch (Exception ex)
            {
                _logger.LogError("组织机构菜单：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<MenuInfoViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// css-右侧检测机构人员列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageIndex">页大小</param>
        /// <param name="word">关键字(人名模糊查询)</param>
        /// <param name="searchType">0全省 1市 2安监站</param>
        /// <param name="belongedTo">机构编号（全省的不需要传值）</param>      
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<TestingInstituteUserViewModel>>> GetTestingInstituteUserList(int pageIndex, int pageSize, int searchType
            , string belongedTo, string testingInstituteInfoName, int state, string testingInstituteInfoId)
        {
            try
            {
                List<TestingInstituteUserViewModel> uselist = null;
                int totalCount = 0;

                if (searchType == 0)
                {
                    uselist = await (from a in _context.TestingInstituteUser.Where(x => x.DeleteTag == 0)
                                     join b in _context.TestingInstituteInfo.Where(y => y.DeleteTag == 0)
                                     on a.TestingInstituteInfoId equals b.TestingInstituteInfoId
                                      into t0
                                     from res in t0.DefaultIfEmpty()
                                     select new TestingInstituteUserViewModel
                                     {
                                         TestingInstituteUserId = a.TestingInstituteUserId,
                                         TestingInstituteInfoId = a.TestingInstituteInfoId,
                                         MechanismName = res.MechanismName,
                                         TestingInstituteUserName = a.TestingInstituteUserName,
                                         Address = res.Address,
                                         BelongedTo = res.BelongedTo,
                                         JobTitle = a.JobTitle,
                                         State = a.State,
                                         Sex = a.Sex,
                                         Account = a.Account
                                     }).ToListAsync();

                }
                else if (searchType == 1 && !string.IsNullOrEmpty(belongedTo))
                {
                    //全市下的检测所账号管理
                    //市
                    var shi = await _context.CityZone.Where(x => x.BelongedTo == belongedTo).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                    var anjianzhan = await _context.CityZone.Where(x => x.ParentCityCode == shi.CityCode).Select(x => x.BelongedTo).ToListAsync();


                    uselist = await (from a in _context.TestingInstituteUser.Where(x => x.DeleteTag == 0)
                                     join b in _context.TestingInstituteInfo.Where(y => y.DeleteTag == 0)
                                     on a.TestingInstituteInfoId equals b.TestingInstituteInfoId
                                      into t0
                                     from res in t0.DefaultIfEmpty()
                                     where (res.BelongedTo == shi.BelongedTo || anjianzhan.Contains(res.BelongedTo))
                                     select new TestingInstituteUserViewModel
                                     {
                                         TestingInstituteUserId = a.TestingInstituteUserId,
                                         TestingInstituteInfoId = a.TestingInstituteInfoId,
                                         MechanismName = res.MechanismName,
                                         TestingInstituteUserName = a.TestingInstituteUserName,
                                         Address = res.Address,
                                         BelongedTo = res.BelongedTo,
                                         JobTitle = a.JobTitle,
                                         State = a.State,
                                         Sex = a.Sex,
                                         Account = a.Account
                                     }).ToListAsync();

                }
                else if (searchType == 2 && !string.IsNullOrEmpty(belongedTo))
                {
                    //安监站级别
                    var shi = await _context.CityZone.Where(x => x.BelongedTo == belongedTo).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                    uselist = await (from a in _context.TestingInstituteUser.Where(x => x.DeleteTag == 0)
                                     join b in _context.TestingInstituteInfo.Where(y => y.DeleteTag == 0)
                                     on a.TestingInstituteInfoId equals b.TestingInstituteInfoId
                                      into t0
                                     from res in t0.DefaultIfEmpty()
                                     where res.BelongedTo == shi.BelongedTo
                                     select new TestingInstituteUserViewModel
                                     {
                                         TestingInstituteUserId = a.TestingInstituteUserId,
                                         TestingInstituteInfoId = a.TestingInstituteInfoId,
                                         MechanismName = res.MechanismName,
                                         TestingInstituteUserName = a.TestingInstituteUserName,
                                         Address = res.Address,
                                         BelongedTo = res.BelongedTo,
                                         JobTitle = a.JobTitle,
                                         State = a.State,
                                         Sex = a.Sex,
                                         Account = a.Account

                                     }).ToListAsync();
                }

                if (!string.IsNullOrWhiteSpace(testingInstituteInfoName))
                {
                    uselist = uselist.Where(x => x.MechanismName.Contains(testingInstituteInfoName)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(testingInstituteInfoId))
                {
                    uselist = uselist.Where(x => x.TestingInstituteInfoId == testingInstituteInfoId).ToList();
                }
                if (state == 0 || state == 1)
                {
                    uselist = uselist.Where(x => x.State == state).ToList();
                }
                totalCount = uselist.Count();
                uselist = uselist.OrderBy(x => x.BelongedTo).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                return ResponseViewModel<List<TestingInstituteUserViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, uselist, totalCount);


            }
            catch (Exception ex)
            {
                _logger.LogError("人员-数据：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<TestingInstituteUserViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        #region 人员库

        /// <summary>
        /// 添加人员库人员信息/修改人员库人员信息
        /// </summary>
        /// <param name="viewModel">修改的话多传一个人员id</param>
        /// <returns></returns>
        //[Authorize]
        [HttpPost]
        public async Task<ResponseViewModel<object>> UpdateTestingInstituteWorker([FromBody] TestingInstituteWorker viewModel)
        {
            try
            {
                if (viewModel == null
                    || string.IsNullOrWhiteSpace(viewModel.WokerName)
                    || string.IsNullOrWhiteSpace(viewModel.WokerPhone)
                    || string.IsNullOrWhiteSpace(viewModel.CertificateUrl))
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "参数错误！");
                }
                DateTime nowTime = DateTime.Now;
                var role = User.FindFirst(nameof(ClaimsIdentity.RoleClaimType))?.Value;//当前人角色
                var makeBelongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;//当前操作人的BelongedTo
                var testingInstituteInfoId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;//当前操作人的BelongedTo

                if (string.IsNullOrWhiteSpace(viewModel.TestingInstituteWorkerId))
                {
                    TestingInstituteWorker user = await _context.TestingInstituteWorker.Where(x => x.WokerPhone == viewModel.WokerPhone && x.DeleteTag == 0).FirstOrDefaultAsync();
                    if (user != null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "信息已存在！");
                    }
                    //判断手机号是否重复
                    TestingInstituteWorker userInfo = await _context.TestingInstituteWorker.Where(x => x.WokerPhone == viewModel.WokerPhone && x.DeleteTag == 0).OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync();
                    if (userInfo != null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "该账号已注册后台账号！", "该账号已注册后台账号！");
                    }

                    viewModel.TestingInstituteWorkerId = SecurityManage.GuidUpper();
                    viewModel.TestingInstituteInfoId = testingInstituteInfoId;
                    //viewModel.WokerName = viewModel.WokerName;
                    //viewModel.WokerIdCard = viewModel.WokerIdCard;
                    //viewModel.CertificateNo = viewModel.CertificateNo;
                    //viewModel.ValidDateBegin = viewModel.ValidDateBegin;
                    //viewModel.ValidDateEnd = viewModel.ValidDateEnd;
                    //viewModel.Position = viewModel.Position;
                    viewModel.DeleteTag = 0;
                    //viewModel.WokerPhone = viewModel.WokerPhone;
                    viewModel.CreateDate = nowTime;
                    viewModel.UpdateDate = nowTime;
                    await _context.TestingInstituteWorker.AddAsync(viewModel);
                    await _context.SaveChangesAsync();
                    return ResponseViewModel<object>.Create(Status.SUCCESS, "信息修改成功", "信息修改成功");

                }
                else
                {
                    TestingInstituteWorker user = await _context.TestingInstituteWorker.Where(x => x.TestingInstituteWorkerId == viewModel.TestingInstituteWorkerId).FirstOrDefaultAsync();
                    if (user == null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "信息不存在！");
                    }
                    //判断账号和别人的是否一致
                    TestingInstituteWorker isAccountExist = await _context.TestingInstituteWorker.Where(x => (x.WokerPhone == viewModel.WokerPhone||x.WokerIdCard == viewModel.WokerIdCard || x.CertificateNo == viewModel.CertificateNo)
                    && x.TestingInstituteWorkerId != viewModel.TestingInstituteWorkerId).FirstOrDefaultAsync();
                    if (isAccountExist != null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "手机号或身份证或证书编号 已和" + isAccountExist.WokerName + "的信息重复！");
                    }
                    user.WokerName = viewModel.WokerName;
                    user.WokerPhone = viewModel.WokerPhone;
                    user.UpdateDate = DateTime.Now;
                    user.CertificateUrl = viewModel.CertificateUrl;
                    user.WokerIdCard = viewModel.WokerIdCard;
                    user.CertificateNo = viewModel.CertificateNo;
                    user.ValidDateBegin = viewModel.ValidDateBegin;
                    user.ValidDateEnd = viewModel.ValidDateEnd;
                    user.Position = viewModel.Position;
                    user.DeleteTag = 0;
                    user.UpdateDate = nowTime;
                    _context.TestingInstituteWorker.Update(user);
                    await _context.SaveChangesAsync();
                    return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "信息保存成功");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("检测所人员库编辑：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, "信息保存失败");
            }
        }

        /// <summary>
        /// 删除检测所人员库人员信息信息
        /// </summary>
        /// <param name="viewModel">修改的话多传一个人员id</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<object>> DelTestingInstituteWorker(List<string> testingInstituteWorkerId)
        {
            try
            {
                if (testingInstituteWorkerId == null || testingInstituteWorkerId.Count == 0)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "参数错误！");
                }
                var role = User.FindFirst(nameof(ClaimsIdentity.RoleClaimType))?.Value;//当前人角色
                var makeBelongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;//当前操作人的BelongedTo

                var userList = await _context.TestingInstituteWorker.Where(x => testingInstituteWorkerId.Contains
                (x.TestingInstituteWorkerId)).ToListAsync();
                if (userList == null || userList.Count == 0)
                {
                    return ResponseViewModel<object>.Create(Status.ERROR, "信息不存在！");
                }
                userList.ForEach(x =>
                {
                    x.DeleteTag = 1;
                });
                _context.TestingInstituteWorker.UpdateRange(userList);
                await _context.SaveChangesAsync();
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "删除成功");

            }
            catch (Exception ex)
            {
                _logger.LogError("检测所人员库删除人员信息：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, "检测所账号删除失败");
            }
        }

        /// <summary>
        /// css-检测所人员库数据查询
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageIndex">页大小</param>
        /// <param name="word">关键字(人名模糊查询)</param>
        /// <param name="searchType">0全省 1市 2安监站</param>
        /// <param name="belongedTo">机构编号（全省的不需要传值）</param>      
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<TestingInstituteWorker>>> GetTestingInstituteWorkerList(int pageIndex, int pageSize, int searchType
            , string belongedTo, string word)
        {
            try
            {
                List<TestingInstituteWorker> uselist = null;
                var testingInstituteInfoId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;//当前操作人的BelongedTo

                int totalCount = 0;

                if (searchType == 0)
                {
                    uselist = await (from a in _context.TestingInstituteWorker.Where(x => x.DeleteTag == 0)
                                     join b in _context.TestingInstituteInfo.Where(y => y.DeleteTag == 0)
                                     on a.TestingInstituteInfoId equals b.TestingInstituteInfoId
                                      into t0
                                     from res in t0.DefaultIfEmpty()
                                     select a).ToListAsync();

                }
                else if (searchType == 1 && !string.IsNullOrEmpty(belongedTo))
                {
                    //全市下的检测所账号管理
                    //市
                    var shi = await _context.CityZone.Where(x => x.BelongedTo == belongedTo).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                    var anjianzhan = await _context.CityZone.Where(x => x.ParentCityCode == shi.CityCode).Select(x => x.BelongedTo).ToListAsync();


                    uselist = await (from a in _context.TestingInstituteWorker.Where(x => x.DeleteTag == 0)
                                     join b in _context.TestingInstituteInfo.Where(y => y.DeleteTag == 0)
                                     on a.TestingInstituteInfoId equals b.TestingInstituteInfoId
                                      into t0
                                     from res in t0.DefaultIfEmpty()
                                     where (res.BelongedTo == shi.BelongedTo || anjianzhan.Contains(res.BelongedTo))
                                     select a).ToListAsync();

                }
                else if (searchType == 2 && !string.IsNullOrEmpty(belongedTo))
                {
                    //安监站级别
                    var shi = await _context.CityZone.Where(x => x.BelongedTo == belongedTo).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                    uselist = await (from a in _context.TestingInstituteWorker.Where(x => x.DeleteTag == 0)
                                     join b in _context.TestingInstituteInfo.Where(y => y.DeleteTag == 0)
                                     on a.TestingInstituteInfoId equals b.TestingInstituteInfoId
                                      into t0
                                     from res in t0.DefaultIfEmpty()
                                     where res.BelongedTo == shi.BelongedTo
                                     select a).ToListAsync();
                }

                if (!string.IsNullOrEmpty(testingInstituteInfoId))
                {
                    uselist = uselist.Where(x => x.TestingInstituteInfoId == testingInstituteInfoId).ToList();
                }

                if (!string.IsNullOrWhiteSpace(word))
                {
                    uselist = uselist.Where(x => x.WokerPhone.Contains(word) || x.WokerName.Contains(word)).ToList();
                }



                totalCount = uselist.Count();
                uselist = uselist.OrderByDescending(x => x.Id).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                return ResponseViewModel<List<TestingInstituteWorker>>.Create(Status.SUCCESS, Message.SUCCESS, uselist, totalCount);


            }
            catch (Exception ex)
            {
                _logger.LogError("检测所人员库数据：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<TestingInstituteWorker>>.Create(Status.ERROR, Message.ERROR);
            }
        }
        #endregion


    }
}