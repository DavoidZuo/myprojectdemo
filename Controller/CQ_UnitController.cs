using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Models;
using JSAJ.Core.Models.LargeMachinery;
using JSAJ.Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViewModels;

namespace JSAJ.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]

    public class CQ_UnitController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MenusController> _logger;
        private readonly JssanjianmanagerContext _context;
        private JwtSettings settings;
        private OssFileSetting _ossFileSetting;
        public CQ_UnitController(IWebHostEnvironment environment, ILogger<MenusController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options, IOptions<OssFileSetting> oss)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            settings = options.Value;
            _ossFileSetting = oss.Value;
        }

        #region 单位信息维护
        //反填，根据id反填
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<object>> GetEnterprise()
        {
            //_context.RecordConfigs
            //EntregisterInfoMagId
            var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
            var data = await _context.EntRegisterInfoMag.Where(a => a.EntRegisterInfoMagId == userId).
                Select(a => new
                {
                    a.EntName, //单位名称
                    a.SocialCreditCode, //统一社会信用代码
                    a.UnitAddress, //单位地址（新加字段）
                    a.JuridicalPerson, // 法定代表人
                    a.JuridicalPersonPhone, // 法定代表人联系电话 
                    a.TechnologyPerson, //技术负责人（新加字段）
                    a.TechnologyPersonphone, //技术负责人联系电话 （新加字段）
                    a.EquipmentPerson, //设备负责人 （新加字段）
                    a.EquipmentPersonphone,//设备负责人联系电话 （新加字段）
                    a.EntCodeImg //营业执照
                }).FirstOrDefaultAsync();
            if (data == null)
            {
                return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL, "找不到此项目！");
            }
            return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, data);
        }

        //上传图片获取url
        [HttpPost]
        public ResponseViewModel<string> UpImg([FromForm]IFormCollection iform)
        {
            try
            {
                var fileImg = iform.Files[0];
                var fileUrl = Util.UploadFileToServer(fileImg, _environment, Request, "CQ_Unit");
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, fileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError("url获取失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        //修改，
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<object>> UptEnterprise([FromBody]EntRegisterInfoMagViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.EntName) || string.IsNullOrWhiteSpace(model.SocialCreditCode) ||
                    string.IsNullOrWhiteSpace(model.UnitAddress) || string.IsNullOrWhiteSpace(model.JuridicalPerson) ||
                    string.IsNullOrWhiteSpace(model.TechnologyPerson) || string.IsNullOrWhiteSpace(model.EquipmentPerson))
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL, "带*为必填项，请检查是否填写完整！");
                }

                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var data = await _context.EntRegisterInfoMag.Where(a => a.EntRegisterInfoMagId == userId).FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL, "修改失败！");
                }

                data.EntName = model.EntName;
                data.SocialCreditCode = model.SocialCreditCode;
                data.UnitAddress = model.UnitAddress;
                data.JuridicalPerson = model.JuridicalPerson;
                data.JuridicalPersonPhone = model.JuridicalPersonPhone;
                data.TechnologyPerson = model.TechnologyPerson;
                data.TechnologyPersonphone = model.TechnologyPersonphone;
                data.EquipmentPerson = model.EquipmentPerson;
                data.EquipmentPersonphone = model.EquipmentPersonphone;
                data.EntCodeImg = model.EntCodeImg;
                _context.EntRegisterInfoMag.Update(data);
                await _context.SaveChangesAsync();
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "修改成功!");
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，修改失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR, "系统异常，修改失败");
            }
        }

        #endregion

        #region 企业基本信息
        /// <summary>
        /// 获取企业信息
        /// 安拆和产权
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<EntRegisterInfoMag>> GetEnterpriseMessage()
        {
            try
            {
                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var data = await _context.EntRegisterInfoMag.Where(a => a.EntRegisterInfoMagId == userId).
                    OrderByDescending(o => o.Id).FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<EntRegisterInfoMag>.Create(Status.FAIL, "基本信息不存在或已被删除");
                }
                return ResponseViewModel<EntRegisterInfoMag>.Create(Status.SUCCESS, Message.SUCCESS, data);
            }
            catch(Exception ex)
            {
                _logger.LogError("获取企业基本信息：" + ex.Message, ex);
                return ResponseViewModel<EntRegisterInfoMag>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 南京市场入库申请
        /// 安拆和产权
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<EntRegisterInfoMag>> GetNanJingEnterprise()
        {
            try
            {
                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var data = await _context.EntRegisterInfoMag.Where(a => a.EntRegisterInfoMagId == userId).
                    OrderByDescending(o => o.Id).FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<EntRegisterInfoMag>.Create(Status.FAIL, "基本信息不存在或已被删除");
                }
                return ResponseViewModel<EntRegisterInfoMag>.Create(Status.SUCCESS, Message.SUCCESS, data);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取企业基本信息：" + ex.Message, ex);
                return ResponseViewModel<EntRegisterInfoMag>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 更新企业信息
        /// 安拆单位和产权单位
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> UptEnterpriseMessage([FromBody]EntRegisterInfoMag model)
        {
            try
            {
                
                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var data = _context.EntRegisterInfoMag
                    .Where(a => a.EntRegisterInfoMagId == userId)
                    .OrderByDescending(o=>o.Id)
                    .FirstOrDefault();
                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "基本信息不存在或已被删除");
                }
                data.UnitAddress = model.UnitAddress;
                data.JuridicalPerson = model.JuridicalPerson;
                data.JuridicalPersonPhone = model.JuridicalPersonPhone;
                data.JuridicalPersonIdCard = model.JuridicalPersonIdCard;
                data.TechnologyPerson = model.TechnologyPerson;
                data.TechnologyPersonphone = model.TechnologyPersonphone;
                data.EquipmentPerson = model.EquipmentPerson;
                data.EquipmentPersonphone = model.EquipmentPersonphone;
                data.BusinessLicenseUrl = model.BusinessLicenseUrl;
                data.EnterpriseQualification = model.EnterpriseQualification;
                data.SafetyProductionNo = model.SafetyProductionNo;
                data.RegisteredCapital = model.RegisteredCapital;
                data.RegisteredDate = model.RegisteredDate;
                data.RegisteredAddress = model.RegisteredAddress;
               // data.IntelligenceLevelUrl = model.IntelligenceLevelUrl;
                data.IntelligenceLevel = model.IntelligenceLevel;
                data.LicenceUrl = model.LicenceUrl;
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "修改成功!");
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，修改失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "系统异常，修改失败");
            }
        }



        /// <summary>
        /// 更新企业信息
        /// 安拆单位和产权单位
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> UptNJEnterpriseMessage([FromBody] EntRegisterInfoMag model)
        {
            try
            {

                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var data = _context.EntRegisterInfoMag
                    .Where(a => a.EntRegisterInfoMagId == userId)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefault();
                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "基本信息不存在或已被删除");
                }
                data.UnitAddress = model.UnitAddress;
                data.JuridicalPerson = model.JuridicalPerson;
                data.JuridicalPersonPhone = model.JuridicalPersonPhone;
                data.JuridicalPersonIdCard = model.JuridicalPersonIdCard;
                data.TechnologyPerson = model.TechnologyPerson;
                data.TechnologyPersonphone = model.TechnologyPersonphone;
                data.EquipmentPerson = model.EquipmentPerson;
                data.EquipmentPersonphone = model.EquipmentPersonphone;
                data.BusinessLicenseUrl = model.BusinessLicenseUrl;
                data.EnterpriseQualification = model.EnterpriseQualification;
                data.SafetyProductionNo = model.SafetyProductionNo;
                data.RegisteredCapital = model.RegisteredCapital;
                data.RegisteredDate = model.RegisteredDate;
                data.RegisteredAddress = model.RegisteredAddress;
                // data.IntelligenceLevelUrl = model.IntelligenceLevelUrl;
                data.IntelligenceLevel = model.IntelligenceLevel;
                data.LicenceUrl = model.LicenceUrl;
                data.EntMainQualification=model.EntMainQualification;
                data.BusinessLicensDate=model.BusinessLicensDate;
                data.AddQualifiCation=model.AddQualifiCation;
                data.EntQualificationDate=model.EntQualificationDate;
                data.NJFilingCertificateDate=model.NJFilingCertificateDate;
                data.NJAddress=model.NJAddress;
                data.EntQualificationCertificateUrl=model.EntQualificationCertificateUrl;
                data.NJFilingCertificateUrl=model.NJFilingCertificateUrl;
                data.CreditManNumberUrl=model.CreditManNumberUrl;
                data.NJName=model.NJName;
                data.NJIdcard=model.NJIdcard;
                data.NJPhone=model.NJPhone;
                data.NJemail=model.NJemail;
                data.NJArtName=model.NJArtName;
                data.NJArtIdcard=model.NJArtIdcard;
                data.NJArtMajor=model.NJArtMajor;
                data.NJArtTitle=model.NJArtTitle;
                data.IsOrNotExamine=model.IsOrNotExamine;
                _context.EntRegisterInfoMag.Update(data);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "修改成功!");
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，修改失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "系统异常，修改失败");
            }
        }
        #endregion


        /// <summary>
        /// 更新企业信息
        /// 安拆单位和产权单位
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<object>> ExamineUnitInfo(int page, int limit,string entName)
        {
            try
            {
                //var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                //var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;
                //var data = from a in _context.MachineryInfos
                //           join b in _context.CheckRecords
                //           on a.CheckRecordId equals b.CheckRecordId
                //           where a.DeleteMark == 0
                //        && a.TestingInstituteInfoId == testingId
                //           select new { a.EntGUID };
               
                //var entGuid = data.Select(s => s.EntGUID).Distinct().ToList();
                var info = _context.EntRegisterInfoMag.Where(w => w.DeleteMark==0 && (w.EntType == "安拆单位" || (w.IntelligenceLevel!=0 && w.IntelligenceLevelUrl!=null && w.IntelligenceLevelUrl!="")));
                if (!string.IsNullOrEmpty(entName))
                {
                    info=info.Where(w => w.EntName.Contains(entName));
                }
                var count=info.Count();
                var result = info.Skip((page - 1) * limit).Take(limit).ToList();
                return ResponseViewModel<object>.Create(Status.SUCCESS,Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR, "系统异常");
            }
        }

        /// <summary>
        /// 删除企业信息
        /// 安拆单位和产权单位
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> DelEnterpriseMessage(List<string> entRegisterInfoMagIds)
        {
            try
            {

                foreach (var item in entRegisterInfoMagIds)
                {
                    var data = _context.EntRegisterInfoMag
                    .Where(a => a.EntRegisterInfoMagId == item)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefault();
                    if (data == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "基本信息不存在或已被删除");
                    }
                    data.IsOrNotExamine = 3;
                    _context.EntRegisterInfoMag.Update(data);
                }

                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "修改成功!");
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，修改失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "系统异常，修改失败");
            }
        }



        /// <summary>
        /// 审核入库企业信息
        /// 安拆单位和产权单位
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> ExamineEntMessage([FromBody] ExamineInfo info)
        {
            try
            {

                    var data = _context.EntRegisterInfoMag
                    .Where(a => a.EntRegisterInfoMagId == info.entRegisterInfoMagId)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefault();
                    if (data == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "基本信息不存在或已被删除");
                    }
                if (info.IsOrNotExamine==2)
                {
                    data.IsOrNotExamine = info.IsOrNotExamine;
                    data.ExamineRemark=null;
                }
                else
                {
                    data.IsOrNotExamine = info.IsOrNotExamine;
                    data.ExamineRemark=info.remark;
                }
                _context.EntRegisterInfoMag.Update(data);
                

                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "审核成功!");
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，审核失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "系统异常，审核失败");
            }
        }
    }
}