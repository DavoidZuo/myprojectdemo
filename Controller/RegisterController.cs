using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using Microsoft.Extensions.Logging;
using JSAJ.Core.Common;
using JSAJ.Core.Common.DataBll;
using Microsoft.AspNetCore.Hosting;
using MCUtil.Security;
using JSAJ.Core.Common.DataUtil;
using Microsoft.Extensions.Options;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Common;
using ViewModels;
using qcloudsms_csharp;
using MCUtil.DBS;

namespace JSAJ.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    //[Authorize]

    public class RegisterController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly JssanjianmanagerContext _context;
        private readonly ILogger<RegisterController> _logger;
        private OssFileSetting _ossFileSetting;
        private readonly string dapperConn;
        public RegisterController(ILogger<RegisterController> logger, JssanjianmanagerContext context, IWebHostEnvironment environment, IOptions<OssFileSetting> oss, IConfiguration configuration)
        {
            dapperConn = configuration.GetConnectionString("JssAnjianmanager");
            _environment = environment;
            _ossFileSetting = oss.Value;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 查询企业资质等级下拉框数据源
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ResponseViewModel<List<EnumDataViewModel>> GetIntelligenceLevel()
        {
            try
            {

                List<EnumDataViewModel> list = EnumHelper.GetIntelligenceLevel();
                return ResponseViewModel<List<EnumDataViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, list);
            }
            catch (Exception ex)
            {
                _logger.LogError("大型机械列表：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<EnumDataViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// 根据备案号查询项目
        /// </summary>
        /// <param name="recordNumber"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<RegisterViewModel>>> SearchProject(string recordNumber)
        {
            //定义返回对象
            List<RegisterViewModel> numbers = new List<RegisterViewModel>();
            try
            {
                var query = await _context.ProjectOverview.Where
                (s => s.RecordNumber == recordNumber).ToListAsync();
                if (query == null)
                {
                    return ResponseViewModel<List<RegisterViewModel>>.Create(Status.ERROR, Message.ERROR);
                }
                query.ForEach(x =>
                {
                    RegisterViewModel model = new RegisterViewModel();
                    model.BelongedTo = x.BelongedTo;
                    model.ProjectName = x.ProjectName;
                    model.RecordNumber = x.RecordNumber;
                    numbers.Add(model);
                });
            }
            catch (Exception ex)
            {

                _logger.LogError("查询备案号错误：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<List<RegisterViewModel>>.Create(Status.ERROR, Message.ERROR);
            }


            return ResponseViewModel<List<RegisterViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, numbers);
        }
        /// <summary>
        /// 根据项目信息查询施工单位
        /// </summary>
        /// <param name="belongedTo"></param>
        /// <param name="recordNumber"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<RegisterViewModel>>> SearchEnt(string belongedTo, string recordNumber)
        {
            //定义返回对象
            List<RegisterViewModel> numbers = new List<RegisterViewModel>();
            try
            {
                var query = await _context.ProjectEntSnapshot
               .Where(s => s.BelongedTo.Equals(belongedTo)
               && s.RecordNumber.Equals(recordNumber)
               && s.EnterpriseType == "施工单位"
               && s.MainUnit == "是")
               .AsNoTracking()
               .ToListAsync();


                query.ForEach(x =>
                {
                    RegisterViewModel model = new RegisterViewModel();
                    model.EntCode = x.OrganizationCode;
                    model.EnterpriseName = x.EnterpriseName;
                    numbers.Add(model);
                });
                return ResponseViewModel<List<RegisterViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, numbers);
            }
            catch (Exception ex)
            {
                _logger.LogError("查询施工单位错误：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<List<RegisterViewModel>>.Create(Status.ERROR, Message.ERROR);
            }

        }
        /// <summary>
        /// 发送手机验证码
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> SendValidCode(string phone)
        {
            try
            {
                int appid = 1400082495;
                string appkey = "40aceebea6c48b82ef448315c1228bc2";
                var templateId = 105140;
                SmsSingleSender ssender = new SmsSingleSender(appid, appkey);
                string smsSign = "";
                Random random = new Random();
                var code = random.Next(1000, 9999).ToString();
                var result = ssender.sendWithParam("86", phone,
                    templateId, new[] { code, "10" }, smsSign, "", "");
                try
                {
                    await RedisHelper.SetAsync(phone + "codeJSAJ", code, 600);
                }
                catch (Exception)
                {

                    return ResponseViewModel<string>.Create(Status.ERROR, "验证码发送成功！写入redis缓存失败");
                }
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "验证码发送成功！请注意查收");
            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, "验证码发送失败！请稍后重试" + ex.Message + "\r\n" + ex.StackTrace);

            }

        }
        /// <summary>
        /// 获取验证码是否正确
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> CheckVerifiCode(string phone, string code)
        {
            try
            {
                object codeNew = await RedisHelper.GetAsync(phone + "codeJSAJ");
                if (codeNew != null && code.Equals(codeNew.ToString()))
                {
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "验证成功");
                }
                else
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "验证失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("验证码：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "验证失败，请稍后再试");
            }

        }
        /// <summary>
        /// 根据项目名称或企业名称查询备案号
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<RegisterViewModel>>> SearchRecordNumber(int pageIndex, int pageSize, string projectName
            , string recordNumber, string enterpriseName)
        {
            if (string.IsNullOrWhiteSpace(projectName) && string.IsNullOrWhiteSpace(recordNumber) && string.IsNullOrWhiteSpace(enterpriseName))
            {
                return ResponseViewModel<List<RegisterViewModel>>.Create(Status.FAIL, "请输入搜索条件");
            }

            string cmdText = @"select BelongedTo,RecordNumber,EnterpriseName,ProjectAddress,ProjectName
from ProjectEntSnapshot where EnterpriseType = '施工单位' and MainUnit = '是'";

            if (!string.IsNullOrWhiteSpace(projectName))
            {
                cmdText += " and ProjectName like '%" + projectName + "%'";
            }
            if (!string.IsNullOrWhiteSpace(recordNumber))
            {
                cmdText += " and RecordNumber = '" + recordNumber + "'";
            }

            if (!string.IsNullOrWhiteSpace(enterpriseName))
            {
                cmdText += " and EnterpriseName like '%" + enterpriseName + "%'";
            }

            DapperHelper<RegisterViewModel> dapperProject = new DapperHelper<RegisterViewModel>(dapperConn);
            var numbers0 = await dapperProject.QueryAsync(cmdText);



            //定义返回对象
            List<RegisterViewModel> numbers = new List<RegisterViewModel>();
            //var numbers0 = _context.ProjectEntSnapshot
            //     .Where(s => s.EnterpriseType == "施工单位"
            //     && s.MainUnit == "是").Select(x => new RegisterViewModel()
            //     {
            //         BelongedTo = x.BelongedTo,
            //         RecordNumber = x.RecordNumber,
            //         ProjectName = x.ProjectName,
            //         ProjectAddress = x.ProjectAddress,
            //         EnterpriseName = x.EnterpriseName,
            //     });
            //if (!string.IsNullOrWhiteSpace(projectName))
            //{
            //    numbers0 = numbers0.Where(d => d.ProjectName.Contains(projectName));
            //}
            //if (!string.IsNullOrWhiteSpace(recordNumber))
            //{
            //    numbers0 = numbers0.Where(d => d.RecordNumber == recordNumber);
            //}

            //if (!string.IsNullOrWhiteSpace(enterpriseName))
            //{
            //    numbers0 = numbers0.Where(d => d.EnterpriseName.Contains(enterpriseName));
            //}

            int queryCount = numbers0.Count();
            numbers = numbers0.OrderByDescending(t => t.RecordNumber)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize).ToList();

            return ResponseViewModel<List<RegisterViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, numbers, queryCount);
        }
        /// <summary>
        /// 项目注册
        /// </summary>
        /// <param name="register"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<object>> RegisterProject([FromBody] RegisterViewModel register)
        {
            try
            {
                if (register.RecordNumber.StartsWith("T"))
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL, "该项目未备案通过,禁止注册！");
                }
                //验证验证码是否正确
                var code = RedisHelper.Get(register.UserPhoneNumber + "codeJSAJ");
                if (code != register.Code)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL, "验证码错误");
                }
                //判断人员是否存在
                var user = await _context.AppUserInfo
                    .Where(s => s.UserPhoneNum == register.UserPhoneNumber)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();


                if (user != null)
                {
                    //判断该人员是否已有账号
                    var query = await _context.AppUserProInfo.Where(s => s.UserNumber == user.UserNumber).FirstOrDefaultAsync();
                    if (user.UserCardId != register.UserCardID)
                    {
                        return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL, "注册失败!该手机号已被使用");
                    }
                    if (query != null && query.ConstructionEntCode != register.EntCode)
                    {
                        return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL, "注册失败!该人员已在其他企业注册");
                    }
                    if (query != null && query.BelongedTo == register.BelongedTo && query.RecordNumber == register.RecordNumber)
                    {
                        return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL, "注册失败!该账号重复注册");
                    }


                    //插入注册信息
                    AppUserEntInfo mode = new AppUserEntInfo();
                    mode.UserNumber = user.UserNumber;
                    mode.UserName = register.UserName;
                    mode.UserCardId = register.UserCardID;
                    mode.UserPhoneNum = register.UserPhoneNumber;
                    mode.EntCode = register.EntCode;
                    mode.EntName = register.EnterpriseName;
                    mode.EntType = "施工单位";
                    await _context.AppUserEntInfo.AddAsync(mode);

                    AppUserProInfo mode2 = new AppUserProInfo();
                    mode2.BelongedTo = register.BelongedTo;
                    mode2.RecordNumber = register.RecordNumber;
                    mode2.ProjectName = register.ProjectName;
                    mode2.ConstructionEntCode = register.EntCode;
                    mode2.ConstructionUnit = register.EnterpriseName;
                    mode2.UserNumber = user.UserNumber;
                    mode2.UserType = register.UserType;
                    await _context.AppUserProInfo.AddAsync(mode2);
                    await _context.SaveChangesAsync();
                    return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "注册成功!");
                }
                else
                {
                    //插入注册信息
                    string userNumber = "X" + DateTime.Now.ToString("yyyyMMddHHmmssffffff");
                    AppUserEntInfo mode = new AppUserEntInfo();
                    mode.UserNumber = userNumber;
                    mode.UserName = register.UserName;
                    mode.UserCardId = register.UserCardID;
                    mode.UserPhoneNum = register.UserPhoneNumber;
                    mode.EntCode = register.EntCode;
                    mode.EntName = register.EnterpriseName;
                    mode.EntType = "施工单位";
                    await _context.AppUserEntInfo.AddAsync(mode);

                    AppUserInfo mode1 = new AppUserInfo();
                    mode1.UserNumber = userNumber;
                    mode1.UserName = register.UserName;
                    mode1.PositionName = register.UserType;
                    mode1.UserCardId = register.UserCardID;
                    mode1.UserPhoneNum = register.UserPhoneNumber;
                    mode1.UserPwd = TokenValidate.StrConversionMD5(register.UserPWD).ToUpper();
                    mode1.EntType = "施工单位";
                    await _context.AppUserInfo.AddAsync(mode1);

                    AppUserProInfo mode2 = new AppUserProInfo();
                    mode2.BelongedTo = register.BelongedTo;
                    mode2.RecordNumber = register.RecordNumber;
                    mode2.ProjectName = register.ProjectName;
                    mode2.ConstructionEntCode = register.EntCode;
                    mode2.ConstructionUnit = register.EnterpriseName;
                    mode2.UserNumber = userNumber;
                    mode2.UserType = register.UserType;
                    await _context.AppUserProInfo.AddAsync(mode2);
                    await _context.SaveChangesAsync();
                    return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "注册成功!");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("项目注册：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR, "注册失败!，请检查填写信息");
            }

        }


        /// <summary>
        /// 上传社会统一信用代码证扫面件
        /// machuanlong
        /// 2019-04-22
        /// </summary>
        /// <param name="iform">扫描件</param>
        /// <returns></returns>
        [HttpPost]
        public ResponseViewModel<string> UploadScanningCopy(IFormCollection iform)
        {
            try
            {
                var file = iform.Files[0];
                var url = Util.UploadFileToServer(file, _environment, Request, "EntCode");
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, url);
            }
            catch (Exception ex)
            {
                _logger.LogError("上传社会统一信用代码证：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "扫描件上传失败");
            }
        }


        /// <summary>
        /// 企业注册
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<object>> RegisterEnt([FromBody] RegisterEntViewModel viewModel)
        {
            try
            {
                if (!DataBll.CheckparmRegist(viewModel))
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, "请将注册信息填写完整", "请将注册信息填写完整");
                }

                //判断企业是否已经注册
                var hasEnt = _context.EntRegisterInfoMag
                    .Where(w => w.EntCode == viewModel.EntCode && w.EntType == viewModel.EnterpriseType && w.DeleteMark == 0)
                    .AsNoTracking()
                    .FirstOrDefault();
                //// 手机号不允许重复
                //var hasMobile = _context.EntRegisterInfoMag
                //    .Where(w => w.RegisteredMansPhone == viewModel.RegisteredMansPhone)
                //    .FirstOrDefault();
                //hasMobile.AccountStatus == "1"
                if (hasEnt != null)
                {
                    return ResponseViewModel<object>.Create(Status.SUCCESS, "该企业已注册" + viewModel.EnterpriseType + "请勿重复注册");
                }
                //if (hasEnt != null && hasEnt.EntType == "施工单位" && viewModel.EnterpriseType == "安拆单位" && hasEnt.AccountStatus != "-1")
                //{
                //    return ResponseViewModel<object>.Create(Status.WARN, "请登录施工企业账号进行安拆资质申请", "请登录施工企业账号进行安拆资质申请");
                //}
                //if (hasEnt != null && hasEnt.EntType == "产权单位" && viewModel.EnterpriseType == "安拆单位" && hasEnt.AccountStatus != "-1")
                //{
                //    return ResponseViewModel<object>.Create(Status.WARN, "该企业已有产权单位账号，如需进行安拆告知操作，请登陆产权单位账号进行安装资质申请。", "该企业已有产权单位账号，如需进行安拆告知操作，请登陆产权单位账号进行安装资质申请。");
                //}
                //else if (hasEnt != null && hasEnt.AccountStatus == "2")
                //{

                //    //获取
                //    var data = await _context.EntRegisterInfoMag.Where(e => e.EntCode == viewModel.EntCode && e.EntType == viewModel.EnterpriseType && e.DeleteMark == 0).FirstOrDefaultAsync();

                //    //删除
                //    if (data != null)
                //    {
                //        _context.EntRegisterInfoMag.RemoveRange(data);
                //    }

                //    _context.SaveChanges();
                //    RegisterEntViewModels mode = new RegisterEntViewModels()
                //    {
                //        Reson = _context.EntRegisteredAudit.Where(e => e.EntCode == data.EntCode).OrderByDescending(o => o.Id).Select(k => k.AuditOpinion).FirstOrDefault(),
                //        EntCode = data.EntCode,
                //        RegisteredMans = data.RegisteredMans,
                //        RegisteredMansPhone = data.RegisteredMansPhone,
                //        EntName = data.EntName,
                //        IdCard = data.RegisteredMansCardId,

                //        Url = data.EntCodeImg,
                //        EnterpriseType = data.EntType,
                //        IntelligenceLevel = data.IntelligenceLevel.GetHashCode(),
                //        IntelligenceLevelUrl = data.IntelligenceLevelUrl,
                //        LicenceUrl = data.LicenceUrl,
                //        IsOrNotExamine=0
                //    };
                //    return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, mode);


                //}
                //else if (hasEnt != null && hasEnt.AccountStatus == "-1")
                //{
                //    //企业信息之前被其他账号注册但是已经被驳回的时候可以注册
                //    //审核不通过
                //    hasMobile.RegisteredDate = DateTime.Now;
                //    hasMobile.EntCode = viewModel.EntCode;
                //    hasMobile.EntName = viewModel.EntName;
                //    hasMobile.EntPassWd = TokenValidate.StrConversionMD5(viewModel.PassWord).ToUpper();
                //    hasMobile.EntType = viewModel.EnterpriseType;
                //    hasMobile.RegisteredMans = viewModel.RegisteredMans;
                //    hasMobile.RegisteredMansPhone = viewModel.RegisteredMansPhone;
                //    hasMobile.RegisteredMansCardId = viewModel.IdCard;
                //    hasMobile.EntCodeImg = viewModel.Url;
                //    hasMobile.BusinessLicenseUrl = viewModel.Url;
                //    hasMobile.AccountStatus = "0";
                //    if (viewModel.EnterpriseType == "建设单位")
                //    {
                //        hasMobile.Competence = "BuildUnit";
                //    }
                //    else if (viewModel.EnterpriseType == "监理单位")
                //    {
                //        hasMobile.Competence = "SupervisionUnit";
                //    }
                //    else if (viewModel.EnterpriseType == "安拆单位")
                //    {
                //        hasMobile.IntelligenceLevel = (IntelligenceLevel)viewModel.IntelligenceLevel;
                //        hasMobile.IntelligenceLevelUrl = viewModel.IntelligenceLevelUrl;
                //        hasMobile.LicenceUrl = viewModel.LicenceUrl;
                //        hasMobile.Competence = "InstallDisUnit";
                //    }
                //    else if (viewModel.EnterpriseType == "施工单位")
                //    {
                //        hasMobile.Competence = "ConstructionUnit";
                //    }
                //    else if (viewModel.EnterpriseType == "产权单位")
                //    {
                //        hasMobile.Competence = "PropertyRightUnit";
                //    }
                //    _context.EntRegisterInfoMag.Update(hasMobile);
                //}
                //else if (hasMobile != null && hasEnt != null)
                //{
                //    if (hasMobile.AccountStatus == "0")
                //    {
                //        return ResponseViewModel<object>.Create(Status.WARN, "该手机号已被注册，正在审核中，请耐心等待！", "该手机号已被注册，正在审核中，请耐心等待！");
                //    }
                //    else if (hasMobile.AccountStatus == "1")
                //    {
                //        return ResponseViewModel<object>.Create(Status.WARN, "该手机号已被注册，不可重复注册", "该手机号已被注册，不可重复注册");
                //    }
                //    else if (hasMobile.AccountStatus == "-1")
                //    {
                //        //审核不通过
                //        hasMobile.RegisteredDate = DateTime.Now;
                //        hasMobile.EntCode = viewModel.EntCode;
                //        hasMobile.EntName = viewModel.EntName;
                //        hasMobile.EntPassWd = TokenValidate.StrConversionMD5(viewModel.PassWord).ToUpper();
                //        hasMobile.EntType = viewModel.EnterpriseType;
                //        hasMobile.RegisteredMans = viewModel.RegisteredMans;
                //        hasMobile.RegisteredMansPhone = viewModel.RegisteredMansPhone;
                //        hasMobile.RegisteredMansCardId = viewModel.IdCard;
                //        hasMobile.EntCodeImg = viewModel.Url;
                //        hasMobile.AccountStatus = "0";
                //        if (viewModel.EnterpriseType == "轨道交通单位")
                //        {
                //            hasMobile.Competence = "RailTransitUnit";
                //        }
                //        if (viewModel.EnterpriseType == "建设单位")
                //        {
                //            hasMobile.Competence = "BuildUnit";
                //        }
                //        else if (viewModel.EnterpriseType == "监理单位")
                //        {
                //            hasMobile.Competence = "SupervisionUnit";
                //        }
                //        else if (viewModel.EnterpriseType == "安拆单位")
                //        {
                //            hasMobile.IntelligenceLevel = (IntelligenceLevel)viewModel.IntelligenceLevel;
                //            hasMobile.IntelligenceLevelUrl = viewModel.IntelligenceLevelUrl;
                //            hasMobile.LicenceUrl = viewModel.LicenceUrl;
                //            hasMobile.Competence = "InstallDisUnit";
                //        }
                //        else if (viewModel.EnterpriseType == "施工单位")
                //        {
                //            hasMobile.Competence = "ConstructionUnit";
                //        }
                //        else if (viewModel.EnterpriseType == "产权单位")
                //        {
                //            hasMobile.Competence = "PropertyRightUnit";
                //        }
                //        _context.EntRegisterInfoMag.Update(hasMobile);
                //    }

                //}
                //else
                //{
                EntRegisterInfoMag model = new EntRegisterInfoMag();
                model.EntRegisterInfoMagId = SecurityManage.GuidUpper();
                model.RegisteredDate = DateTime.Now;
                model.EntCode = viewModel.EntCode;
                model.EntName = viewModel.EntName;
                model.EntPassWd = TokenValidate.StrConversionMD5(viewModel.PassWord).ToUpper();
                model.EntType = viewModel.EnterpriseType;
                model.RegisteredMans = viewModel.RegisteredMans;
                model.RegisteredMansPhone = viewModel.RegisteredMansPhone;
                model.RegisteredMansCardId = viewModel.IdCard;
                model.EntCodeImg = viewModel.Url;
                model.Remarks = "";
                //model.AccountStatus = "1";
                if (viewModel.EnterpriseType == "建设单位")
                {
                    model.Competence = "BuildUnit";
                }
                else if (viewModel.EnterpriseType == "监理单位")
                {
                    model.Competence = "SupervisionUnit";
                }
                else if (viewModel.EnterpriseType == "安拆单位")
                {
                    model.IntelligenceLevel = (IntelligenceLevel)viewModel.IntelligenceLevel;
                    model.IntelligenceLevelUrl = viewModel.IntelligenceLevelUrl;
                    model.LicenceUrl = viewModel.LicenceUrl;
                    model.Competence = "InstallDisUnit";
                }
                else if (viewModel.EnterpriseType == "施工单位")
                {
                    model.Competence = "ConstructionUnit";
                }
                else if (viewModel.EnterpriseType == "产权单位")
                {
                    model.Competence = "PropertyRightUnit";
                }

                await _context.EntRegisterInfoMag.AddAsync(model);
                //}
                await _context.SaveChangesAsync();
                return ResponseViewModel<object>.Create(Status.SUCCESS, "注册成功,请等待管理员审核", "注册成功,请等待管理员审核");
            }
            catch (Exception ex)
            {
                _logger.LogError("注册错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, "该企业注册失败" + ex.Message + ex.StackTrace, "该企业注册失败");
            }
        }


        /// <summary>
        /// 获取企业注册审核列表
        /// </summary>
        /// <param name = "entType" ></ param >
        /// < param name="entName"></param>
        /// <param name = "pageIndex" ></ param >
        /// < param name="pageSize"></param>
        /// <param name = "state" ></ param >
        /// < returns ></ returns >
        //[HttpGet]
        //public async Task<ResponseViewModel<List<AuditList>>> GetEnterpriseAuditList(string entType, string entName, int pageIndex, int pageSize, string state, string entCode)
        //{
        //    try
        //    {
        //        var query = _context.EntRegisterInfoMag
        //            .Where(s => s.EntType == "建设单位" || s.EntType == "施工单位" ||
        //            s.EntType == "监理单位" || s.EntType == "安拆单位" || s.EntType == "轨道交通单位");

        //        if (!string.IsNullOrWhiteSpace(entType))
        //        {
        //            query = query.Where(s => s.EntType == entType);
        //        }
        //        if (!string.IsNullOrWhiteSpace(entName))
        //        {
        //            query = query.Where(s => !string.IsNullOrEmpty(s.EntName)
        //            && s.EntName.Contains(entName));
        //        }

        //        if (!string.IsNullOrWhiteSpace(entCode))
        //        {
        //            query = query.Where(s => !string.IsNullOrEmpty(s.EntCode)
        //            && s.EntCode.Contains(entCode));
        //        }
        //        if (!string.IsNullOrWhiteSpace(state))
        //        {
        //            if (state == "0")
        //            {
        //                query = query.Where(s => !string.IsNullOrEmpty(s.AccountStatus)
        //            && s.AccountStatus == "0")
        //            ;
        //            }
        //            else
        //            {
        //                query = query.Where(s => !string.IsNullOrEmpty(s.AccountStatus)
        //            && s.AccountStatus != "0");
        //            }


        //        }
        //        int count = query.Count();
        //        query = query.OrderByDescending(t => t.Id).Skip((pageIndex - 1) * pageSize).Take(pageSize);


        //        List<AuditList> list = new List<AuditList>();
        //        query.ToList().ForEach(x =>
        //        {
        //            AuditList mode = new AuditList();
        //            mode.AccountStatus = x.AccountStatus;
        //            mode.EntCode = x.EntCode;
        //            mode.EntName = x.EntName;
        //            mode.EntType = x.EntType;
        //            mode.RegisteredDate = x.RegisteredDate;
        //            mode.RegisteredMans = x.RegisteredMans;
        //            mode.RegisteredMansPhone = x.RegisteredMansPhone;
        //            mode.AccountStatus = x.AccountStatus;
        //            list.Add(mode);
        //        });
        //        return ResponseViewModel<List<AuditList>>.Create(Status.SUCCESS, Message.SUCCESS, list, count);

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("获取企业注册审核列表错误：" + ex.Message + ex.StackTrace, ex);
        //        return ResponseViewModel<List<AuditList>>.Create(Status.ERROR, Message.ERROR);
        //    }
        //}

        [HttpGet]
        public async Task<ResponseViewModel<List<UrlList>>> GetUrlList(string entCode, string entType)
        {

            try
            {
                var query = await _context.EntRegisterInfoMag
                    .Where(s => s.EntCode == entCode && s.EntType == entType)
                    .FirstOrDefaultAsync();
                List<UrlList> lists = new List<UrlList>();

                UrlList mode = new UrlList();
                mode.FileName = "营业执照";
                mode.FileUrl = query.EntCodeImg;
                if (query.EntCodeImg != null)
                {
                    mode.Suffix = Path.GetExtension(query.EntCodeImg).Replace(".", "");
                }

                lists.Add(mode);
                if (entType == "安拆单位")
                {
                    UrlList mode1 = new UrlList();
                    mode1.FileName = "资质证书"; //IntelligenceLevelUrl//安全生产许可证LicenceUrl
                    mode1.FileUrl = query.IntelligenceLevelUrl;
                    if (query.IntelligenceLevelUrl != null)
                    {
                        mode1.Suffix = Path.GetExtension(query.IntelligenceLevelUrl).Replace(".", "");
                    }

                    lists.Add(mode1);
                    UrlList mode2 = new UrlList();
                    mode2.FileName = "安全生产许可证";
                    mode2.FileUrl = query.LicenceUrl;
                    if (query.LicenceUrl != null)
                    {
                        mode2.Suffix = Path.GetExtension(query.LicenceUrl).Replace(".", "");
                    }
                    lists.Add(mode2);
                }
                return ResponseViewModel<List<UrlList>>.Create(Status.SUCCESS, Message.SUCCESS, lists);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取企业注册审核配置文件错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<UrlList>>.Create(Status.ERROR, Message.ERROR);
            }
        }
        ///// <summary>
        ///// 企业注册审核
        ///// </summary>
        ///// <param name="state"></param>
        ///// <param name="remark"></param>
        ///// <param name="entCode"></param>
        ///// <returns></returns>
        //[HttpGet]
        //public async Task<ResponseViewModel<string>> ToExamineEnterprise(string state, string remark, string entCode, string entType)
        //{
        //    try
        //    {
        //        var user = User.FindFirst(ClaimsIdentity.DefaultNameClaimType)?.Value;
        //        var query = await _context.EntRegisterInfoMag
        //            .Where(s => s.EntCode == entCode && s.EntType == entType)
        //            .FirstOrDefaultAsync();
        //        query.AccountStatus = state;
        //        EntRegisteredAudit audit = new EntRegisteredAudit();
        //        audit.EntCode = query.EntCode;
        //        audit.EntType = query.EntType;
        //        audit.EntName = query.EntName;
        //        audit.AuditMans = user;
        //        audit.AuditDate = DateTime.Now;
        //        audit.AuditResults = state;
        //        audit.AuditOpinion = remark;

        //        _context.EntRegisterInfoMag.UpdateRange(query);
        //        _context.EntRegisteredAudit.AddRange(audit);
        //        _context.SaveChanges();

        //        //给注册人发送短信


        //        return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("企业注册审核错误：" + ex.Message + ex.StackTrace, ex);
        //        return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
        //    }
        //}

        [HttpGet]
        public async Task<ResponseViewModel<List<ApprovalHistoryList>>> ApprovalHistory(string entCode, string entType)
        {
            try
            {
                var query = await _context.EntRegisteredAudit
                    .Where(s => s.EntCode == entCode && s.EntType == entType)
                    .ToListAsync();
                List<ApprovalHistoryList> lists = new List<ApprovalHistoryList>();
                if (query.Count == 0)
                {
                    return ResponseViewModel<List<ApprovalHistoryList>>.Create(Status.SUCCESS, Message.SUCCESS, lists);
                }
                query.ForEach(x =>
                {
                    ApprovalHistoryList mode = new ApprovalHistoryList();
                    mode.AuditDate = x.AuditDate;
                    mode.AuditMans = x.AuditMans;
                    mode.AuditState = x.AuditResults;
                    lists.Add(mode);

                });
                return ResponseViewModel<List<ApprovalHistoryList>>.Create(Status.SUCCESS, Message.SUCCESS, lists);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取企业注册审核历史错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<ApprovalHistoryList>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        public async Task<ResponseViewModel<List<PropertyUnit>>> GetPropertyUnitInfos(int pageIndex, int pageSize, string entName)
        {
            try
            {
                var query = _context.EntRegisterInfoMag.Where(s => s.EntType == "产权单位" || s.EntType == "施工单位" || s.EntType == "安拆单位");
                if (!string.IsNullOrWhiteSpace(entName))
                {
                    query = query.Where(s => s.EntName.Contains(entName));
                }
                int count = query.Count();
                var list = await query.OrderByDescending(t => t.Id)
                     .Skip((pageIndex - 1) * pageSize).Take(pageSize)
                     .Select(k => new PropertyUnit
                     {
                         //AccountStatus = k.AccountStatus,
                         Canlease = k.CanLease,
                         EntCode = k.EntCode,
                         EntName = k.EntName,
                         EntType = k.EntType,
                         TechnologyPhone = k.TechnologyPersonphone,
                         EntRegisterInfoMagId = k.EntRegisterInfoMagId,
                         RegisteredMans = k.RegisteredMans,
                         ValidityBeginTime = k.ValidityBeginTime,
                         ValidityEndTime = k.ValidityEndTime
                     }).ToListAsync();
                return ResponseViewModel<List<PropertyUnit>>.Create(Status.SUCCESS, Message.SUCCESS, list, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取产权企业错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<PropertyUnit>>.Create(Status.ERROR, Message.ERROR);
            }
        }
        [HttpPost]
        public async Task<ResponseViewModel<string>> SavePropertyUnitInfo(PropertyUnit info)
        {
            try
            {
                if (info == null
                    || string.IsNullOrWhiteSpace(info.EntCode)
                    || string.IsNullOrWhiteSpace(info.EntName)
                    || string.IsNullOrWhiteSpace(info.Password)
                    || string.IsNullOrWhiteSpace(info.RegisteredMans)
                    || info.ValidityBeginTime == null || info.ValidityEndTime == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
                }
                var isExists = await _context.EntRegisterInfoMag.Where(x => x.EntType == "产权单位" && x.EntCode == info.EntCode).ToListAsync();
                if (isExists.Count > 0)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "社会统一信用社代码证号已存在！");
                }
                EntRegisterInfoMag mode = new EntRegisterInfoMag();
                mode.EntCode = info.EntCode;
                mode.EntName = info.EntName;
                mode.EntType = "产权单位";
                mode.EntPassWd = TokenValidate.StrConversionMD5(info.Password).ToUpper();
                mode.EntRegisterInfoMagId = SecurityManage.GuidUpper();
                mode.TechnologyPersonphone = info.TechnologyPhone;
                mode.CanLease = 0;
                //mode.AccountStatus = "0";
                mode.RegisteredMans = info.RegisteredMans;
                mode.ValidityBeginTime = info.ValidityBeginTime;
                mode.ValidityEndTime = info.ValidityEndTime;
                _context.EntRegisterInfoMag.AddRange(mode);
                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("保存产权企业错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }
        public async Task<ResponseViewModel<string>> SaveAuthorization(string canLease, string accountStatus, string entRegisterInfoMagId)
        {
            try
            {
                var mode = await _context.EntRegisterInfoMag.Where(s => s.EntRegisterInfoMagId == entRegisterInfoMagId).FirstOrDefaultAsync();
                if (mode == null)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "查询账号错误，请联系管理员！");
                }
                //EntRegisterInfoMag mode = new EntRegisterInfoMag();
                if (canLease == "1")
                {
                    mode.CanLease = 1;
                }
                if (canLease == "0")
                {
                    mode.CanLease = 0;
                }
                //if (!string.IsNullOrWhiteSpace(accountStatus))
                //{
                //    mode.AccountStatus = accountStatus;
                //}

                _context.EntRegisterInfoMag.UpdateRange(mode);
                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("保存授权信息错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        public async Task<ResponseViewModel<string>> UpdatePropertyUnitInfo(PropertyUnit info)
        {
            try
            {
                var mode = await _context.EntRegisterInfoMag.Where(s => s.EntRegisterInfoMagId == info.EntRegisterInfoMagId).FirstOrDefaultAsync();
                if (mode == null)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "查询账号错误，请联系管理员！");
                }
                var isExists = await _context.EntRegisterInfoMag.Where(x => x.EntType == "产权单位"
                && x.EntCode == info.EntCode && x.EntRegisterInfoMagId != info.EntRegisterInfoMagId).ToListAsync();
                if (isExists.Count > 0)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "社会统一信用社代码证号和其他产权单位重复！");
                }
                mode.EntCode = info.EntCode;
                mode.EntName = info.EntName;
                mode.EntType = "产权单位";
                if (!string.IsNullOrWhiteSpace(info.Password))
                {
                    mode.EntPassWd = TokenValidate.StrConversionMD5(info.Password).ToUpper();
                }
                mode.TechnologyPersonphone = info.TechnologyPhone;
                mode.EntRegisterInfoMagId = info.EntRegisterInfoMagId;
                mode.ValidityBeginTime = info.ValidityBeginTime;
                mode.ValidityEndTime = info.ValidityEndTime;
                mode.RegisteredMans = info.RegisteredMans;
                _context.EntRegisterInfoMag.Update(mode);
                _context.SaveChanges();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("修改产权企业错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        public async Task<ResponseViewModel<string>> DeletePropertyUnitInfo(string entRegisterInfoMagId)
        {
            try
            {
                var mode = await _context.EntRegisterInfoMag.Where(s => s.EntRegisterInfoMagId == entRegisterInfoMagId).FirstOrDefaultAsync();
                if (mode == null)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "查询账号错误，请联系管理员！");
                }
                _context.EntRegisterInfoMag.RemoveRange(mode);
                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("删除产权企业错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

    }
}