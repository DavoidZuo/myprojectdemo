using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspose.Words;
using Aspose.Words.Tables;
using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Common.DataBll;
using JSAJ.Core.Models;
using JSAJ.Core.Models.LargeMachinery;
using JSAJ.Core.ViewModels;
using MCUtil.DBS;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using qcloudsms_csharp;
using ViewModels;

namespace JSAJ.Core.Controllers.manage
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PersonalInformationsController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        private readonly ILogger<PersonalInformationsController> _logger;
        private readonly JssanjianmanagerContext _context;
        private JwtSettings settings;
        private OssFileSetting _ossFileSetting;
        private readonly string _wordTemplte;
        private readonly string _buildWords;

        public PersonalInformationsController(IWebHostEnvironment environment, ILogger<PersonalInformationsController> logger, JssanjianmanagerContext context, IOptions<OssFileSetting> oss)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _wordTemplte = environment.WebRootPath + Path.DirectorySeparatorChar + "doc" + Path.DirectorySeparatorChar;
            _buildWords = environment.WebRootPath + Path.DirectorySeparatorChar + "BuildPdf" + Path.DirectorySeparatorChar;
            _ossFileSetting = oss.Value;
        }
        [HttpGet]
        public async Task<ResponseViewModel<string>> UpdatePassword(string password, string newPassword, int? type)
        {
            try
            {
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;

                if (type == 3)//检测所修改
                {
                    var query1 = await _context.TestingInstituteUser.Where(s => s.TestingInstituteUserId == uuid)
                        .FirstOrDefaultAsync();
                    if (query1 == null)
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "数据异常！");
                    }
                    password = SecurityManage.StringToMD5(password);
                    if (password == query1.UserPwd)
                    {
                        query1.UserPwd = SecurityManage.StringToMD5(newPassword);
                    }
                    else
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "密码输入错误");
                    }
                    _context.TestingInstituteUser.UpdateRange(query1);

                }
                else
                {
                    var query = await _context.SysUserManager
                    .Where(s => s.Uuid == uuid)
                    .FirstOrDefaultAsync();
                    if (query == null)
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "数据异常！");
                    }
                    password = SecurityManage.StringToMD5(password);
                    if (password == query.UserPwd)
                    {
                        query.UserPwd = SecurityManage.StringToMD5(newPassword);
                    }
                    else
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "密码输入错误");
                    }
                    _context.SysUserManager.UpdateRange(query);
                }


                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, "修改密码成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("修改密码错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }
        public async Task<ResponseViewModel<PersonalInformationViewModel>> GetInformation()
        {
            try
            {
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                var query = await _context.SysUserManager
                    .Where(s => s.Uuid == uuid)
                    .FirstOrDefaultAsync();
                PersonalInformationViewModel mode = new PersonalInformationViewModel();
                if (query == null)
                {
                    return ResponseViewModel<PersonalInformationViewModel>.Create(Status.SUCCESS, Message.SUCCESS, mode);
                }
                mode.CreatDate = query.CreateDate;
                mode.Department = query.Department;
                mode.Email = query.Email;
                if (query.Photo == null || query.Photo == "")
                {
                    mode.Photo = "http://jss-file-test2019.oss-cn-shanghai.aliyuncs.com/uploadFile/organizationPerson/2020-04-01/AA70835D58AA4C0A9745CD5E5AE483CB.png";
                }
                else
                {
                    mode.Photo = query.Photo;
                }
                mode.UserName = query.UserName;
                mode.UserPhone = query.UserPhone;
                // mode.RoleId = query.RoleId;
                return ResponseViewModel<PersonalInformationViewModel>.Create(Status.SUCCESS, Message.SUCCESS, mode);
            }
            catch (Exception ex)
            {
                _logger.LogError("修改密码错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<PersonalInformationViewModel>.Create(Status.ERROR, Message.ERROR);
            }
        }
        [HttpPost]
        public async Task<ResponseViewModel<string>> UpdatPhotoUrl([FromForm] IFormCollection iform)
        {
            try
            {
                var url = Util.UploadFileToServer(iform.Files[0], _environment, Request, "organizationPerson");
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, url);

            }
            catch (Exception ex)
            {
                _logger.LogError("上传照片错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }

        }
        [HttpGet]
        public async Task<ResponseViewModel<string>> SaveInformation(string photo, string email)
        {
            try
            {
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                var query = await _context.SysUserManager
                    .Where(s => s.Uuid == uuid)
                    .FirstOrDefaultAsync();
                query.Photo = photo;
                query.Email = email;
                _context.SysUserManager.UpdateRange(query);
                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, "保存个人信息成功");

            }
            catch (Exception ex)
            {
                _logger.LogError("上传照片错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        //项目0 企业1
        public async Task<ResponseViewModel<string>> UpdatePhoto(string photo, string type, string phone)
        {
            try
            {
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                if (type == "0")
                {
                    var query = await _context.AppUserInfo
                          .Where(s => s.UserPhoneNum == phone)
                          .FirstOrDefaultAsync();
                    if (query == null)
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "修改头像失败！");
                    }
                    query.HeadImgUrl = photo;
                    _context.AppUserInfo.UpdateRange(query);
                }

                _context.SaveChanges();


                return ResponseViewModel<string>.Create(Status.SUCCESS, "保存头像成功");

            }
            catch (Exception ex)
            {
                _logger.LogError("上传照片错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }
        public async Task<ResponseViewModel<string>> GetPhoto(string phone)
        {
            try
            {
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;

                var query = await _context.AppUserInfo
                      .Where(s => s.UserPhoneNum == phone)
                      .FirstOrDefaultAsync();


                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, query.HeadImgUrl);

            }
            catch (Exception ex)
            {
                _logger.LogError("上传照片错误：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        //[HttpGet]
        //public async Task<ResponseViewModel<string>> GetYzm()
        /// <summary>
        /// 发送手机验证码
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> SendValidCode(string phone, string type)
        {
            try
            {
                if (type == "1")
                {
                    var query = await _context.SysUserManager
                        .Where(s => s.UserPhone == phone && s.DeleteMark == 0)
                        .FirstOrDefaultAsync();
                    if (query == null)
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "该手机号未注册！");
                    }

                }
                if (type == "2")
                {
                    var query = await _context.AppUserInfo
                       .Where(s => s.UserPhoneNum == phone)
                       .FirstOrDefaultAsync();
                    if (query == null)
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "该手机号未注册！");
                    }
                }
                if (type == "3")
                {
                    var query = await _context.EntRegisterInfoMag
                       .Where(s => s.RegisteredMansPhone == phone)
                       .FirstOrDefaultAsync();
                    if (query == null)
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "该手机号未注册！");
                    };

                }
                int appid = 1400082495;
                string appkey = "40aceebea6c48b82ef448315c1228bc2";
                var templateId = 105140;
                SmsSingleSender ssender = new SmsSingleSender(appid, appkey);
                string smsSign = "";
                Random random = new Random();
                var code = random.Next(1000, 9999).ToString();
                var result = ssender.sendWithParam("86", phone,
                    templateId, new[] { code, "10" }, smsSign, "", "");
                await RedisHelper.SetAsync(phone + "codeJSAJ", code, 600);
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "验证码发送成功！请注意查收");
            }
            catch (Exception ex)
            {
                _logger.LogError("发送短信验证码：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "验证码发送失败！请稍后重试");

            }

        }
        /// <summary>
        /// 获取验证码是否正确
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> CheckVerifiCode(string phone, string code, string pwd, string type)
        {
            try
            {
                object codeNew = await RedisHelper.GetAsync(phone + "codeJSAJ");
                if (codeNew != null && code.Equals(codeNew.ToString()))
                {
                    if (type == "1")
                    {
                        var query = await _context.SysUserManager
                            .Where(s => s.UserPhone == phone && s.DeleteMark == 0)
                            .FirstOrDefaultAsync();
                        if (query == null)
                        {
                            return ResponseViewModel<string>.Create(Status.ERROR, "修改密码失败！");
                        }
                        query.UserPwd = TokenValidate.StrConversionMD5(pwd).ToUpper();
                        _context.SysUserManager.UpdateRange(query);

                    }
                    if (type == "2")
                    {
                        var query = await _context.AppUserInfo
                           .Where(s => s.UserPhoneNum == phone)
                           .FirstOrDefaultAsync();
                        if (query == null)
                        {
                            return ResponseViewModel<string>.Create(Status.ERROR, "修改密码失败！");
                        }
                        query.UserPwd = TokenValidate.StrConversionMD5(pwd).ToUpper();
                        _context.AppUserInfo.UpdateRange(query);
                    }
                    if (type == "3")
                    {
                        var query = await _context.EntRegisterInfoMag
                           .Where(s => s.RegisteredMansPhone == phone)
                           .FirstOrDefaultAsync();
                        if (query == null)
                        {
                            return ResponseViewModel<string>.Create(Status.ERROR, "修改密码失败！");
                        }
                        query.EntPassWd = TokenValidate.StrConversionMD5(pwd).ToUpper();
                        _context.EntRegisterInfoMag.UpdateRange(query);

                    }
                    _context.SaveChanges();

                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
                }
                else
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "验证码输入错误！");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("验证码：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }

        }


    }
}
