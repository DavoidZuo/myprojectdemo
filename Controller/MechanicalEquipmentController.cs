using JSAJ.Core.Models;
using JSAJ.ViewModels.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViewModels;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Common;
using JSAJ.Core.Models.LargeMachinery;
using Microsoft.EntityFrameworkCore;
using System.IO;
using JSAJ.Core.ViewModels.LargeMachinery;
using JSAJ.Core.ViewModels;
using MCUtil.Security;
using JSAJ.Core.Common;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Security.Claims;
using JSAJ.ViewModels.ViewModels.manager;
using Newtonsoft.Json;
using JSAJ.Core.Common.DataBll;
using JSAJ.Models.Models.Login;
using Microsoft.Extensions.Options;

namespace JSAJNanJing.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MechanicalEquipmentController : ControllerBase
    {
        private readonly JssanjianmanagerContext _context;
        private readonly ILogger<MechanicalEquipmentController> _logger;
        private readonly Jsgginterface _jsgginterface;

        public MechanicalEquipmentController(ILogger<MechanicalEquipmentController> logger, JssanjianmanagerContext context, IOptions<Jsgginterface> ntoptionjsgginterface)
        {
            _context = context;
            _logger = logger;
            _jsgginterface = ntoptionjsgginterface.Value;
        }


        [HttpPost]
        [Authorize(Roles = "检测所,南京检测所")]
        public async Task<ResponseViewModel<string>> CheckMachinery([FromBody] ReviewMachineryViewModel viewModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(viewModel.MachineryInfoId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                if (viewModel.MachineryCheckState != MachineryCheckStateEnum.检测不合格
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.检测合格
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.非我所检测
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.复检合格
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.复检不合格)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;
                var data = await _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId
                        && (w.MachineryCheckState == MachineryCheckStateEnum.检测中
                            || w.MachineryCheckState == MachineryCheckStateEnum.复检中)
                        && w.TestingInstituteInfoId == testingId)
                    .OrderByDescending(o => o.UseSubmitDate)
                    .FirstOrDefaultAsync();


                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"机械不存在或已被检测，无需重复操作");
                }

                var now = DateTime.Now;
                //data.CheckState = viewModel.MachineryState;

                data.CheckUrl = viewModel.RecordUrl;
                if (data.MachineryCheckState == MachineryCheckStateEnum.检测中)
                {
                    data.CheckReviewDate = viewModel.CheckReviewDate;
                    data.ReCheckReviewDate = viewModel.CheckReviewDate;
                }
                else if (data.MachineryCheckState == MachineryCheckStateEnum.复检中)
                {
                    data.CheckReviewDate = viewModel.CheckReviewDate;
                    data.ReCheckReviewDate = viewModel.CheckReviewDate;
                }
                data.MachineryCheckState = viewModel.MachineryCheckState;
                data.UpdateDate = now;
                _context.MachineryInfos.Update(data);
                var checkRecord = await _context.CheckRecords
                    .Where(w => w.CheckRecordId == data.CheckRecordId && w.TestingInstituteInfoId == testingId)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                if (checkRecord == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"机械委托检测记录不存在或已被删除，无法进行检测");
                }
                checkRecord.ReviewUserId = userId;
                checkRecord.UpdateDate = now;
                checkRecord.CheckState = viewModel.MachineryCheckState;
                checkRecord.RecordUrl = viewModel.RecordUrl;
                checkRecord.GeneralItem = viewModel.GeneralItem;
                checkRecord.GuaranteeItem = viewModel.GuaranteeItem;
                checkRecord.UpdateDate = now;
                _context.CheckRecords.Update(checkRecord);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("检测所检测：" + ex.Message, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 获取使用登记审核记录
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="limit">条数</param>
        /// <param name="recordNumber">备案号模糊查询</param>
        /// <param name="projectName">项目名称</param>
        /// <param name="entName">使用单位名称模糊查询</param>
        /// <param name="leaveTheFactoryNo">出厂编号模糊查询</param>
        /// <param name="propertyRightsRecordNo">设备信息编号</param>
        /// <param name="machineryType">机械类型</param>
        /// <param name="machineryName">机械名称</param>
        /// <param name="machineryModel">机械型号</param>
        /// <param name="isReview">是否处理</param>
        /// <param name="begin">开始日期</param>
        /// <param name="end">结束日期</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ReviewUseViewModel>>>
            GetReviewRegisterOfUse(int page, int limit, string recordNumber,
                string projectName, string entName, string leaveTheFactoryNo, string propertyRightsRecordNo
            , MachineryTypeEnum? machineryType, string machineryName, string machineryModel,
                int isReview, DateTime? begin, DateTime? end,string entCode)
        {
            try
            {
                var danwei = _context.ProjectEntSnapshot.Where(w => w.EnterpriseType=="监理单位" && w.MainUnit=="是" && w.OrganizationCode==entCode).Select(s => new { s.BelongedTo, s.RecordNumber }).ToList();
                var belongedTo = danwei.Select(s => s.BelongedTo).Distinct().ToList();

                _context.Database.SetCommandTimeout(300000);
                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//操作人科室
                //var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;

                var installs = await _context.InstallationNotificationRecords
                    .Where(w => w.DeleteMark == 0 && w.Type == 0 && w.State == 2)
                    .Select(k => new { k.InstallationPosition, k.MachineryInfoId, k.Id })
                    .ToListAsync();
                var data = _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0
                        && belongedTo.Contains(w.BelongedTo));
                if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                {
                    data = from A in data
                           join B in _context.ProjectOverview
                           on new { A.BelongedTo, A.RecordNumber } equals new { B.BelongedTo, B.RecordNumber }
                           where B.SupervisionDepartmentId == supervisionDepartmentId
                           select A;
                }
                //备案号
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    data = data.Where(w => w.RecordNumber.Contains(recordNumber));
                }
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    data = data.Where(w => w.ProjectName.Contains(projectName));
                }
                //使用单位
                if (!string.IsNullOrWhiteSpace(entName))
                {
                    data = data.Where(w => w.EntName.Contains(entName));
                }
                //出厂编号
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    data = data.Where(w => w.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                //设备信息号
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    data = data.Where(w => w.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }

                if (machineryType != null)
                {
                    data = data.Where(w => w.MachineryType == machineryType);
                }
                if (!string.IsNullOrWhiteSpace(machineryName))
                {
                    data = data.Where(w => w.MachineryName.Contains(machineryName));
                }
                if (!string.IsNullOrWhiteSpace(machineryModel))
                {
                    data = data.Where(w => w.MachineryModel.Contains(machineryModel));
                }

                if (isReview == 0)
                {
                    // 未处理
                    data = data.Where(w => w.CheckState == MachineryState.办理使用登记审核中);
                }
                else
                {
                    // 处理过
                    data = data.Where(w => w.CheckState == MachineryState.办理使用登记未通过
                        || w.CheckState == MachineryState.办理使用登记通过
                        || w.CheckState == MachineryState.拆卸告知审核中
                        || w.CheckState == MachineryState.拆卸告知审核通过
                        || w.CheckState == MachineryState.拆卸告知审核不通过
                        || w.CheckState == MachineryState.使用登记注销审核中
                        || w.CheckState == MachineryState.使用登记注销审核通过
                        || w.CheckState == MachineryState.使用登记注销审核不通过);
                    if (begin != null && end != null)
                    {
                        data = data.Where(w => w.UseReviewDate != null && w.UseReviewDate >= begin && w.UseReviewDate < ((DateTime)end).AddDays(1));
                    }
                }

                var count = await data.CountAsync();
                if (count == 0)
                {
                    return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<ReviewUseViewModel>(), count);
                }
                if (page == 0)
                {
                    page = 1;
                }
                if (limit == 0)
                {
                    limit = 10;
                }
                var result = await data.OrderByDescending(o => o.UpdateDate)
                     .Skip((page - 1) * limit).Take(limit)
                     .Select(s => new ReviewUseViewModel
                     {
                         MachineryInfoId = s.MachineryInfoId,
                         EntGUID = s.EntGUID,
                         MachineryType = s.MachineryType,
                         MachineryName = s.MachineryName,
                         MachineryModel = s.MachineryModel,
                         OEM = s.OEM,
                         CheckState = s.CheckState,
                         ManufacturingLicense = s.ManufacturingLicense,
                         LeaveTheFactoryNo = s.LeaveTheFactoryNo,
                         MaxRatedLiftingCapacity = s.MaxRatedLiftingCapacity,
                         LeaveTheFactoryDate = s.LeaveTheFactoryDate,
                         Knm = s.Knm,
                         MaxInstallHeight = s.MaxInstallHeight,
                         FreeStandingHeight = s.FreeStandingHeight,
                         AttachedHeight = s.AttachedHeight,
                         MaxRange = s.MaxRange,
                         BuyDate = s.BuyDate,
                         TestingInstituteInfoId = s.TestingInstituteInfoId,
                         CheckReviewDate = s.CheckReviewDate,
                         UseSubmitDate = s.UseSubmitDate,
                         IsReview = isReview,
                         RecordNumber = s.RecordNumber,
                         ProjectName = s.ProjectName,
                         EntName = s.EntName,
                         BelongedTo = s.BelongedTo,
                         RegistrationOfUseApplicationUrl = s.RegistrationOfUseApplicationUrl,
                         PropertyRightsRecordNo = s.PropertyRightsRecordNo,
                         UseRecordNo = s.UseRecordNo,
                         CheckUrl = s.CheckUrl,
                         RegistrationOfUseId = s.RegistrationOfUseId,
                         RegistrationOfUseState = s.CheckState,
                         UseReviewDate = s.UseReviewDate,
                         UseReason = s.UseReason,
                     })
                     .AsNoTracking()
                     .ToListAsync();
                if (result.Count > 0)
                {
                    var qiye = await _context.EntRegisterInfoMag
                                               .Where(w => data.Select(q => q.EntCode).Contains(w.EntRegisterInfoMagId))
                                               .OrderByDescending(o => o.Id)
                                               .Select(s => new { s.Id, s.EntCode, s.EntRegisterInfoMagId })
                                               .ToListAsync();

                    var jianceDanwei = await _context.TestingInstituteUser.Where(w => data.Select(q => q.TestingInstituteInfoId).Contains(w.TestingInstituteInfoId))
                                  .Select(s => new { s.Id, s.TestingInstituteInfoId, s.TestingInstituteUserName }).ToListAsync();

                    //查询项目上传的带章的检测报告照片
                    var recordConfigImg = await _context.RecordConfigs.Where(x => x.Type == RecordConfigTypeEnum.办理使用登记的附件配置
                    && x.DeleteMark == 0 && x.AttachmentName == "检测报告上传（带章）").OrderBy(x => x.Sort).FirstOrDefaultAsync();

                    result.ForEach(s =>
                    {
                        if (recordConfigImg != null)
                        {
                            s.CheckUrlList = _context.RecordAttachments.Where(y => y.RecordConfigId == recordConfigImg.RecordConfigId && (
                                                    y.AttachmentId == s.MachineryInfoId
                                                    || y.AttachmentId == s.RegistrationOfUseId) && y.DeleteMark == 0).Select(y => new FileUploadUrlViem { FileType = recordConfigImg.AttachmentName, RecordConfigId = y.RecordConfigId, FileName = y.FileName, FileUrl = y.FileUrl, Suffix = Path.GetExtension(y.FileName).Replace(".", "") }).ToList();

                            if (s.CheckUrlList.Count == 0)
                            {
                                List<FileUploadUrlViem> file = new List<FileUploadUrlViem>();
                                FileUploadUrlViem model = new FileUploadUrlViem();
                                model.FileName = "检测报告.pdf";
                                model.FileUrl = s.CheckUrl;
                                model.Suffix = Path.GetExtension(s.CheckUrl).Replace(".", "");
                                file.Add(model);
                                s.CheckUrlList = file;
                            }
                        }
                        else
                        {
                            List<FileUploadUrlViem> file = new List<FileUploadUrlViem>();
                            FileUploadUrlViem model = new FileUploadUrlViem();
                            model.FileName = "检测报告.pdf";
                            model.FileUrl = s.CheckUrl;
                            model.Suffix = Path.GetExtension(s.CheckUrl).Replace(".", "");
                            file.Add(model);
                            s.CheckUrlList = file;
                        }

                        s.EntCode = qiye.Where(w => w.EntRegisterInfoMagId == s.EntCode)
                            .OrderByDescending(o => o.Id)
                            .Select(s => s.EntCode)
                            .FirstOrDefault();
                        s.TestingName = jianceDanwei.Where(w => w.TestingInstituteInfoId == s.TestingInstituteInfoId).OrderByDescending(o => o.Id)
                         .Select(s => s.TestingInstituteUserName).FirstOrDefault();
                        s.InstallationPosition = installs.Where(w => w.MachineryInfoId == s.MachineryInfoId)
                        .OrderByDescending(o => o.Id).Select(k => k.InstallationPosition).FirstOrDefault();
                    });
                }
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取使用登记审核记录：" + ex.Message, ex);
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 使用登记审核
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> ReviewRegisterOfUseMachinery([FromBody] ReviewMachineryViewModel viewModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(viewModel.MachineryInfoId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                if (viewModel.MachineryState != MachineryState.办理使用登记未通过 && viewModel.MachineryState != MachineryState.办理使用登记通过)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                var userName = User.FindFirst(ClaimsIdentity.DefaultNameClaimType)?.Value;//当前人名字


                var data = await _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId && w.CheckState == MachineryState.办理使用登记审核中
                        && w.UseReviewBelongedTo == belongedTo)
                    .OrderByDescending(o => o.UseSubmitDate)
                    .FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"机械不存在或已被审核，无需重复操作");
                }
                var now = DateTime.Now;
                data.CheckState = viewModel.MachineryState;
                data.UseReason = viewModel.Reason;
                data.UseReviewDate = now;
                data.UpdateDate = now;
                if (viewModel.MachineryState == MachineryState.办理使用登记通过)
                {
                    // 使用登记通过，获取使用登记号
                    data.UseRecordNo = Util.GetUseRecordNo(_context, data.MachineryType, belongedTo);

                }
                _context.MachineryInfos.Update(data);

                var record = await _context.RegistrationOfUses
                    .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId && w.RegistrationOfUseId == data.RegistrationOfUseId)
                    .OrderByDescending(o => o.CreateDate)
                    .FirstOrDefaultAsync();


                if (record != null)
                {
                    record.UpdateDate = now;
                    record.CheckState = viewModel.MachineryState;
                    record.Reason = viewModel.Reason;
                    record.ReviewUserId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                    if (viewModel.MachineryState == MachineryState.办理使用登记通过)
                    {
                        // 使用登记通过，获取使用登记号
                        record.UseRecordNo = data.UseRecordNo;

                    }
                    _context.RegistrationOfUses.Update(record);
                }

                var xiangmu = await _context.ProjectOverview.Where(w => w.BelongedTo == data.BelongedTo && w.RecordNumber == data.RecordNumber && !string.IsNullOrEmpty(w.ProjectCode)).FirstOrDefaultAsync();
                //var xiangmubind = _context.ProvinceWorkEditBindProject.Where(s => s.BelongedTo == xiangmu.BelongedTo
                //                  && s.RecordNumber == xiangmu.RecordNumber).OrderByDescending(s => s.UpdateDate).FirstOrDefault();

                if (xiangmu != null && !string.IsNullOrEmpty(data.SPSXSLBM))
                {

                    #region 推送省工改办件信息

                    GetGongGaiTokenParam model = new GetGongGaiTokenParam();
                    model.appKey = _jsgginterface.appKey;
                    model.appSecret = _jsgginterface.appSecret;

                    //调用省工改获取token
                    string postContent = DataBll.GetGongGaiToken(_jsgginterface.UrlGetToken, "application/x-www-form-urlencoded", model);
                    if (!string.IsNullOrEmpty(postContent))
                    {

                        GongGaiSystemViewModel result0 = JsonConvert.DeserializeObject<GongGaiSystemViewModel>(postContent);
                        if (result0 != null && result0.status.code == 1)
                        {
                            //获取地区代码
                            CityZone cueecity = _context.CityZone.Where(w => w.BelongedTo == belongedTo).FirstOrDefault();

                            TokenParam headermodel = new TokenParam();
                            headermodel.access_token = result0.custom.access_token;
                            headermodel.time_stamp = result0.custom.timeStamp;
                            headermodel.app_key = _jsgginterface.appKey;


                            ProjectParam projectparam = new ProjectParam();
                            projectparam.ProjectCode = xiangmu.ProjectCode;
                            string paramstr1 = "";
                            //调用成功后先获取省工改的项目信息
                            //token成功了
                            //调取项目信息
                            string postContentxm = DataBll.RequestUrl("POST", "application/x-www-form-urlencoded", _jsgginterface.UrlGetxmjbxx, headermodel, projectparam, out paramstr1);
                            if (!string.IsNullOrEmpty(postContentxm))
                            {
                                GongGaiRoot result1 = JsonConvert.DeserializeObject<GongGaiRoot>(postContentxm);
                                if (result1 != null && result1.status.code == 1 && !string.IsNullOrEmpty(result1.custom.results.projectnum))
                                {

                                    ShenPiBanJianParam banjian = new ShenPiBanJianParam();
                                    banjian.RowGuid = Guid.NewGuid().ToString();
                                    banjian.ParentGuid = data.RegistrationOfUseId;
                                    banjian.XZQHDM = result1.custom.results.xzqhdm;
                                    banjian.JSDDXZQH = result1.custom.results.jsddxzqh;
                                    banjian.ProjectName = result1.custom.results.xmmc;
                                    banjian.ProjectCode = result1.custom.results.xmdm;
                                    banjian.ProjectNum = result1.custom.results.projectnum;
                                    banjian.GCDM = xiangmu.GCDM;
                                    //事项编码
                                    banjian.SPSXSLBM = data.SPSXSLBM;
                                    //办理处室=安监审核单位
                                    banjian.BLCS = cueecity.SuperOrganName;
                                    //办理人
                                    banjian.BLR = userName;
                                    if (viewModel.MachineryState == MachineryState.办理使用登记通过)
                                    {
                                        banjian.BLZT = 11;
                                    }
                                    else
                                    {
                                        banjian.BLZT = 13;
                                    }
                                    banjian.BLYJ = viewModel.Reason;
                                    banjian.BLSJ = now.ToString("yyyy-MM-dd HH:mm:ss");
                                    banjian.Beizu = viewModel.Reason;
                                    banjian.SJYXBS = 1;
                                    string paramstr2 = "";
                                    //调用推送审批办件信息
                                    string postContentspbj = DataBll.RequestUrl("POST", "application/x-www-form-urlencoded", _jsgginterface.UrlSafe, headermodel, banjian, out paramstr2);
                                    RequestRecordLog rizghi = new RequestRecordLog();
                                    rizghi.RequestURL = _jsgginterface.UrlSafe;
                                    rizghi.ReturnInformation = postContentspbj;
                                    rizghi.RequestParameters = paramstr2;
                                    rizghi.ForeignKey = "RegistrationOfUseId=" + data.RegistrationOfUseId;
                                    rizghi.Model = "省工改-推送审批办件信息";
                                    rizghi.CreateDate = DateTime.Now;
                                    _context.RequestRecord.Add(rizghi);
                                    await _context.SaveChangesAsync();
                                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS + "(Ps:" + _jsgginterface.UrlSafe + "推送办件结果" + postContentspbj + ")");

                                }
                                else
                                {

                                    RequestRecordLog rizghi = new RequestRecordLog();
                                    rizghi.RequestURL = _jsgginterface.UrlGetxmjbxx;
                                    rizghi.ReturnInformation = postContentxm;
                                    rizghi.RequestParameters = paramstr1;
                                    rizghi.ForeignKey = "ProjectCode=" + xiangmu.ProjectCode;
                                    rizghi.Model = "省工改-获取项目信息接口";
                                    rizghi.CreateDate = DateTime.Now;
                                    if (result1 != null && result1.status.code == 1 && string.IsNullOrEmpty(result1.custom.results.projectnum))
                                    {
                                        rizghi.Remark = "没返回项目编号" + result1.custom.results.projectnum;
                                    }
                                    _context.RequestRecord.Add(rizghi);
                                    await _context.SaveChangesAsync();

                                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS + "(Ps:" + _jsgginterface.UrlGetxmjbxx + "获取项目信息接口" + postContentxm + ")");
                                }
                            }
                            else
                            {
                                RequestRecordLog rizghi = new RequestRecordLog();
                                rizghi.RequestURL = _jsgginterface.UrlGetxmjbxx;
                                rizghi.ReturnInformation = postContentxm;
                                rizghi.RequestParameters = paramstr1;
                                rizghi.ForeignKey = "ProjectCode=" + xiangmu.ProjectCode;
                                rizghi.Model = "省工改-获取项目信息接口";
                                rizghi.CreateDate = DateTime.Now;
                                rizghi.Remark = "获取项目信息接口无反应";
                                _context.RequestRecord.Add(rizghi);
                                await _context.SaveChangesAsync();
                                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS + "(Ps:" + _jsgginterface.UrlGetxmjbxx + "获取项目信息接口无反应)");
                            }
                        }
                        else
                        {
                            await _context.SaveChangesAsync();
                            return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS + "(Ps:" + _jsgginterface.UrlGetToken + "获取token接口：" + postContent + ")");
                        }
                    }
                    else
                    {
                        await _context.SaveChangesAsync();
                        return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS + "(Ps:" + _jsgginterface.UrlGetToken + "获取token接口接口响应)");
                    }

                    #endregion
                }

                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("使用登记审核：" + ex.Message, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, ex.Message + ex.StackTrace);
            }
        }

        [HttpGet]
        /// <summary>
        /// 获取空闲机械设备
        /// </summary>
        /// <param name="propertyRightsRecordNo">设备信息号</param>
        /// <param name="leaveTheFactoryNo">出厂编号</param>
        /// <returns></returns>
        [HttpGet]
        //[Authorize]
        public async Task<ResponseViewModel<List<MachineryListViewModel>>> GetMachineryList(string propertyRightsRecordNo, string leaveTheFactoryNo, string manufacturingLicense)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(propertyRightsRecordNo) && string.IsNullOrWhiteSpace(leaveTheFactoryNo) && string.IsNullOrWhiteSpace(manufacturingLicense))
                {
                    return ResponseViewModel<List<MachineryListViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<MachineryListViewModel>(), 0);
                }
                //IQueryable<MachineryInfo> list = null;
                //list = _context.MachineryInfos.Where(s => s.CancellationState == CancellationStateEnum.审核通过||s.CheckState== MachineryState.使用登记注销审核通过);
                var data = from A in _context.MachineryInfos
                           join B in _context.EntRegisterInfoMag on A.EntGUID equals B.EntRegisterInfoMagId
                           where A.DeleteMark == 0
                           select new MachineryListViewModel
                           {
                               MachineryType = A.MachineryType,
                               PropertyRightsRecordNo = A.PropertyRightsRecordNo,
                               MachineryModel = A.MachineryModel,
                               LeaveTheFactoryNo = A.LeaveTheFactoryNo,
                               ManufacturingLicense = A.ManufacturingLicense,
                               EntGUID = A.EntGUID,
                               EntName = B.EntName,
                               CheckState = A.CheckState,
                               CancellationState = A.CancellationState,
                               MaxRatedLiftingCapacity = A.MaxRatedLiftingCapacity,
                               Knm = A.Knm,
                               MaxInstallHeight = A.MaxInstallHeight,
                               MaxRange = A.MaxRange,
                               BuyDate = A.BuyDate,
                               MachineryInfoId = A.MachineryInfoId,
                               LeaveTheFactoryDate = A.LeaveTheFactoryDate,
                               FreeStandingHeight = A.FreeStandingHeight,
                               AttachedHeight = A.AttachedHeight,
                               OEM = A.OEM,
                               AccessStatus = A.AccessStatus,
                           };
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    data = data.Where(s => s.PropertyRightsRecordNo == propertyRightsRecordNo);
                }
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    data = data.Where(s => s.LeaveTheFactoryNo == leaveTheFactoryNo);
                }
                if (!string.IsNullOrWhiteSpace(manufacturingLicense))
                {
                    data = data.Where(s => s.ManufacturingLicense == manufacturingLicense);
                }
                return ResponseViewModel<List<MachineryListViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, data.ToList(), data.ToList().Count);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<List<MachineryListViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 产权备案申报列表
        /// </summary>
        /// <param name="total">页码</param>
        /// <param name="size">每页数量</param>
        /// <param name="machineryType">机械类别</param>
        /// <param name="oem">生产厂家</param>
        /// <param name="leaveTheFactoryNo">出厂编号</param>
        /// <param name="manufacturingLicense">制造许可证号</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<MachineryInfoViewModel>>> MachineryList(int total, int size, int machineryType = -1, string oem = "",
            string leaveTheFactoryNo = "", string manufacturingLicense = "")
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//企业表Id     

                var mlist = await _context.MachineryInfos.Where(w => w.DeleteMark == 0 && w.EntGUID == tokenId && w.CancellationState != CancellationStateEnum.审核通过
        && w.OEM.Contains(oem) && w.LeaveTheFactoryNo.Contains(leaveTheFactoryNo)
        && w.ManufacturingLicense.Contains(manufacturingLicense))
             .Select(s => new MachineryInfoViewModel
             {
                 MachineryInfoId = s.MachineryInfoId,
                 PropertyRightsRecordNo = s.PropertyRightsRecordNo,
                 EntGUID = s.EntGUID,
                 MachineryType = s.MachineryType,
                 MachineryName = s.MachineryName,
                 MachineryModel = s.MachineryModel,
                 OEM = s.OEM,
                 LeaveTheFactoryDate = s.LeaveTheFactoryDate,
                 LeaveTheFactoryNo = s.LeaveTheFactoryNo,
                 State = s.State,
                 MaxRatedLiftingCapacity = s.MaxRatedLiftingCapacity,
                 ManufacturingLicense = s.ManufacturingLicense,
                 Knm = s.Knm,
                 MaxInstallHeight = s.MaxInstallHeight,
                 FreeStandingHeight = s.FreeStandingHeight,
                 AttachedHeight = s.AttachedHeight,
                 MaxRange = s.MaxRange,
                 BuyDate = s.BuyDate,
                 Reason = s.Reason,
                 CreateDate = s.CreateDate,
                 IsOldData = s.IsOldData,
                 SubminDate = s.SubmitDate,
                 IsSecondHand = s.IsSecondHand,
                 IsSafetyFile = s.IsSafetyFile,
                 ReviewBelongedTo = s.ReviewBelongedTo == null ? "" : _context.CityZone.Where(w => w.BelongedTo == s.ReviewBelongedTo)
                 .Select(s => s.SuperOrganName).FirstOrDefault(),
                 //QualityNo = s.QualityNo,

             }).ToListAsync();
                if (machineryType != -1)
                {
                    mlist = mlist.Where(w => w.MachineryType == (MachineryTypeEnum)machineryType).ToList();

                }
                //获取最终查出的列表数量
                var totalCount = mlist.Count;
                //对数据源分页
                mlist = mlist.OrderByDescending(o => o.CreateDate).Skip((total - 1) * size).Take(size).ToList();
                return ResponseViewModel<List<MachineryInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, mlist, totalCount);




            }
            catch (Exception ex)
            {

                return ResponseViewModel<List<MachineryInfoViewModel>>.Create(Status.ERROR, Message.ERROR);
            }

        }

        /// <summary>
        /// 添加/编辑保存产权信息
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> AddUpMachineryInfo([FromBody] MachineryInfoViewModel viewModel)
        {

            try
            {


                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//企业表Id                                                          



                var canLease = await _context.EntRegisterInfoMag.Where(w => w.EntRegisterInfoMagId
                 == tokenId).FirstOrDefaultAsync();



                //添加o基本信息 
                if (string.IsNullOrWhiteSpace(viewModel.MachineryInfoId))
                {
                    var machineryInfos = await _context.MachineryInfos
                .Where(w => w.DeleteMark == 0 && w.ManufacturingLicense == viewModel.ManufacturingLicense
                && w.LeaveTheFactoryNo == viewModel.LeaveTheFactoryNo)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();
                    if (machineryInfos != null && machineryInfos.IsNoAction == 1)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "该机械存在问题已被锁定，无法在机械系统操作！");
                    }

                    //是否之前存在过
                    if (machineryInfos == null || machineryInfos.CancellationState == CancellationStateEnum.审核通过)
                    {
                        MachineryInfo mmodel = new MachineryInfo()
                        {
                            MachineryType = (MachineryTypeEnum)viewModel.MachineryType,
                            MachineryName = viewModel.MachineryName,
                            MachineryModel = viewModel.MachineryModel,
                            OEM = viewModel.OEM,
                            ManufacturingLicense = viewModel.ManufacturingLicense,
                            LeaveTheFactoryNo = viewModel.LeaveTheFactoryNo,
                            MaxRatedLiftingCapacity = viewModel.MaxRatedLiftingCapacity,
                            LeaveTheFactoryDate = viewModel.LeaveTheFactoryDate,
                            Knm = viewModel.Knm,
                            MaxInstallHeight = viewModel.MaxInstallHeight,
                            FreeStandingHeight = viewModel.FreeStandingHeight,
                            AttachedHeight = viewModel.AttachedHeight,
                            MaxRange = viewModel.MaxRange,
                            BuyDate = viewModel.BuyDate,
                            QualityNo = "",
                            PropertyRightsRecordNo = "",
                            CreateDate = DateTime.Now,
                            DeleteMark = 0,
                            MachineryInfoId = SecurityManage.GuidUpper(),
                            State = 0,
                            EntGUID = tokenId,
                            CanLease = canLease == null ? 1 : canLease.CanLease,
                            IsSecondHand = viewModel.IsSecondHand,
                            IsSafetyFile = viewModel.IsSafetyFile

                        };

                        //添加图片
                        viewModel.FileInfoList.ForEach(g =>
                        {

                            RecordAttachment rModel = new RecordAttachment()
                            {
                                FileName = g.FileName,
                                FileUrl = g.FileUrl,
                                RecordConfigId = g.RecordConfigId,
                                Type = 0,
                                CreateDate = DateTime.Now,
                                UpdateDate = DateTime.Now,
                                DeleteMark = 0,
                                RecordAttachmentId = SecurityManage.GuidUpper(),
                                AttachmentId = mmodel.MachineryInfoId

                            };
                            _context.RecordAttachments.AddRange(rModel);
                        });

                        await _context.MachineryInfos.AddAsync(mmodel);

                        _context.SaveChanges();

                        return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, mmodel.MachineryInfoId);

                    }
                    else
                    {

                        if (machineryInfos.State == 2)
                        {

                            return ResponseViewModel<string>.Create(Status.FAIL, "该机械已经备案，请勿重新申报！", "该机械已经备案，请勿重新申报！");
                        }
                        else
                        {

                            return ResponseViewModel<string>.Create(Status.FAIL, "该机械正在申报中，请勿重新申报！", "该机械正在申报中，请勿重新申报！");
                        }

                    }

                }
                //编辑基本信息
                else
                {
                    var machineryInfoEdit = await _context.MachineryInfos.Where(x => x.MachineryInfoId == viewModel.MachineryInfoId
                  ).FirstOrDefaultAsync();
                    if (machineryInfoEdit == null)
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "当前机械状态不可编辑");
                    }
                    machineryInfoEdit.MachineryType = (MachineryTypeEnum)viewModel.MachineryType;
                    machineryInfoEdit.MachineryName = viewModel.MachineryName;
                    machineryInfoEdit.MachineryModel = viewModel.MachineryModel;
                    machineryInfoEdit.OEM = viewModel.OEM;
                    machineryInfoEdit.ManufacturingLicense = viewModel.ManufacturingLicense;
                    machineryInfoEdit.LeaveTheFactoryNo = viewModel.LeaveTheFactoryNo;
                    machineryInfoEdit.MaxRatedLiftingCapacity = viewModel.MaxRatedLiftingCapacity;
                    machineryInfoEdit.LeaveTheFactoryDate = viewModel.LeaveTheFactoryDate;
                    machineryInfoEdit.Knm = viewModel.Knm;
                    machineryInfoEdit.MaxInstallHeight = viewModel.MaxInstallHeight;
                    machineryInfoEdit.FreeStandingHeight = viewModel.FreeStandingHeight;
                    machineryInfoEdit.AttachedHeight = viewModel.AttachedHeight;
                    machineryInfoEdit.MaxRange = viewModel.MaxRange;
                    machineryInfoEdit.BuyDate = viewModel.BuyDate;
                    machineryInfoEdit.UpdateDate = DateTime.Now;
                    //machineryOnly.QualityNo = viewModel.QualityNo;
                    //machineryInfos.State = viewModel.State;
                    machineryInfoEdit.CanLease = canLease.CanLease;
                    machineryInfoEdit.CancellationState = CancellationStateEnum.未提交;
                    machineryInfoEdit.IsSecondHand = viewModel.IsSecondHand;
                    machineryInfoEdit.IsSafetyFile = viewModel.IsSafetyFile;
                    _context.MachineryInfos.Update(machineryInfoEdit);

                    //先删除所有图片
                    var delFileList = _context.RecordAttachments
                        .Where(w => w.DeleteMark == 0 && w.AttachmentId == viewModel.MachineryInfoId)
                        .ToList();

                    delFileList.ForEach(g =>
                    {
                        g.DeleteMark = 1;

                        _context.RecordAttachments.UpdateRange(delFileList);

                    });

                    //然后添加
                    viewModel.FileInfoList.ForEach(g =>
                    {

                        RecordAttachment rModel = new RecordAttachment()
                        {
                            FileName = g.FileName,
                            FileUrl = g.FileUrl,
                            RecordConfigId = g.RecordConfigId,
                            Type = 0,
                            CreateDate = DateTime.Now,
                            UpdateDate = DateTime.Now,
                            DeleteMark = 0,
                            RecordAttachmentId = SecurityManage.GuidUpper(),
                            AttachmentId = viewModel.MachineryInfoId

                        };
                        _context.RecordAttachments.AddRange(rModel);
                    });
                    _context.SaveChanges();
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, viewModel.MachineryInfoId);

                }


            }
            catch (Exception ex)
            {

                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }

        }


          /// <summary>
        /// 提交安监站树节点
        /// </summary>
        /// <param name="parentCityCode">安监站父ID</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<CityInfoViewModel>>> CityTree(string parentCityCode, int isShowXieHui)
        {
            try
            {

                if (parentCityCode == "320000")
                {
                    //CityInfoViewModel xiehui = new CityInfoViewModel();
                    //xiehui.ParentCityCode = "";
                    //xiehui.CityShortName = "";
                    //xiehui.CityLongName = "";
                    //xiehui.CityCode = "AAAAA";
                    //xiehui.BelongedTo = "AAAAAA-1";
                    //xiehui.SuperOrganName = "江苏省建筑行业协会";

                    List<CityInfoViewModel> allList = new List<CityInfoViewModel>();
                    //allList.Add(xiehui);
                    var cityList = await _context.CityZone.Where(w => w.ParentCityCode == parentCityCode && w.BelongedTo=="AJ320100-1" && w.CountyCompartment != null && w.CityCompartment != null)
                            .GroupBy(g => new
                            {
                                g.ParentCityCode,
                                g.CityShortName,
                                g.CityLongName,
                                g.CityCode,
                                g.BelongedTo,
                                g.SuperOrganName,
                            }).
                            Select(s => new
                            CityInfoViewModel
                            {
                                ParentCityCode = s.Key.ParentCityCode,
                                CityShortName = s.Key.CityShortName,
                                CityLongName = s.Key.CityLongName + "安监站",
                                CityCode = s.Key.CityCode,
                                BelongedTo = s.Key.BelongedTo,
                                SuperOrganName = s.Key.SuperOrganName
                            }).OrderBy(o => o.CityCode).ToListAsync();
                    allList.AddRange(cityList);
                    allList = allList.OrderBy(o => o.CityCode).ToList();
                    return ResponseViewModel<List<CityInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, allList, allList.Count);

                }
                else
                {
                    List<CityInfoViewModel> allList = new List<CityInfoViewModel>();

                    //if (parentCityCode == "AAAAA")
                    //{
                    //    CityInfoViewModel xiehui = new CityInfoViewModel();
                    //    xiehui.ParentCityCode = "AAAAA";
                    //    xiehui.CityShortName = "";
                    //    xiehui.CityLongName = "";
                    //    xiehui.CityCode = "";
                    //    xiehui.BelongedTo = "AJ320000-1";
                    //    xiehui.SuperOrganName = "江苏省建筑行业协会建筑安全设备管理分会";
                    //    allList.Add(xiehui);
                    //}
                    var cityList = await _context.CityZone.Where(w => w.ParentCityCode == parentCityCode && w.CountyCompartment != null && w.CityCompartment != null && w.BelongedTo != "AJ320601-1")
                                    .GroupBy(g => new
                                    {
                                        g.ParentCityCode,
                                        g.CityShortName,
                                        g.CityLongName,
                                        g.BelongedTo,
                                        g.SuperOrganName,
                                        g.CityCode,
                                    }).
                                    Select(s => new
                                    CityInfoViewModel
                                    {
                                        ParentCityCode = s.Key.ParentCityCode,
                                        CityShortName = s.Key.CityShortName,
                                        CityLongName = s.Key.CityLongName + "安监站",
                                        BelongedTo = s.Key.BelongedTo,
                                        SuperOrganName = s.Key.SuperOrganName,
                                        CityCode = s.Key.CityCode
                                    }).OrderBy(o => o.CityCode).ToListAsync();

                    allList.AddRange(cityList);
                    allList = allList.OrderBy(o => o.CityCode).ToList();
                    return ResponseViewModel<List<CityInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, allList, allList.Count);


                }
            }
            catch (Exception ex)
            {

                return ResponseViewModel<List<CityInfoViewModel>>.Create(Status.ERROR, Message.ERROR);
            }

        }


        /// <summary>
        /// 产权备案审核列表
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="limit">条数</param>
        /// <param name="machineryType">机械类型</param>
        /// <param name="machineryName">机械名称</param>
        /// <param name="oem">设备厂家</param>
        /// <param name="machineryModel">机械型号</param>
        /// <param name="isReview">是否审核过，0：未处理，1：已处理</param>
        /// <param name="begin">处理开始日期</param>
        /// <param name="end">处理结束日期</param>
        /// <param name="submitUnit">提交单位</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ReviewUseViewModel>>>
            GetReviewMachinery(int page, int limit, MachineryTypeEnum? machineryType,
                string machineryName, string oem, string machineryModel, int isReview, DateTime? begin, DateTime? end, string leaveTheFactoryNo
            , string propertyRightsRecordNo, string submitUnit)
        {
            try
            {
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var data = (from A in _context.MachineryInfos
                           join B in _context.EntRegisterInfoMag
                           on A.EntGUID equals B.EntRegisterInfoMagId
                           where A.DeleteMark == 0 && A.ReviewBelongedTo == belongedTo
                           select new ReviewUseViewModel
                           {
                               MachineryInfoId = A.MachineryInfoId,
                               EntGUID = A.EntGUID,
                               MachineryType = A.MachineryType,
                               MachineryName = A.MachineryName,
                               MachineryModel = A.MachineryModel,
                               OEM = A.OEM,
                               ManufacturingLicense = A.ManufacturingLicense,
                               LeaveTheFactoryNo = A.LeaveTheFactoryNo,
                               MaxRatedLiftingCapacity = A.MaxRatedLiftingCapacity,
                               LeaveTheFactoryDate = A.LeaveTheFactoryDate,
                               Knm = A.Knm,
                               MaxInstallHeight = A.MaxInstallHeight,
                               FreeStandingHeight = A.FreeStandingHeight,
                               AttachedHeight = A.AttachedHeight,
                               MaxRange = A.MaxRange,
                               BuyDate = A.BuyDate,
                               TestingInstituteInfoId = A.TestingInstituteInfoId,
                               CheckReviewDate = A.CheckReviewDate,
                               SubmitDate = A.SubmitDate,
                               IsReview = isReview,
                               RecordNumber = A.RecordNumber,
                               ProjectName = A.ProjectName,
                               EntName = A.EntName,
                               EntCode = A.EntCode,
                               BelongedTo = A.BelongedTo,
                               PropertyRightsRecordNo = A.PropertyRightsRecordNo,
                               CQName = B.EntName,
                               State = A.State,
                               ReviewDate = A.ReviewDate,
                               IsSecondHand = A.IsSecondHand,
                               IsSafetyFile = A.IsSafetyFile,
                               UseReason = A.Reason,

                           }).Distinct();



                if (machineryType != null)
                {
                    data = data.Where(w => w.MachineryType == machineryType);
                }
                if (!string.IsNullOrWhiteSpace(machineryName))
                {
                    data = data.Where(w => w.MachineryName.Contains(machineryName));
                }
                if (!string.IsNullOrWhiteSpace(oem))
                {
                    data = data.Where(w => w.OEM.Contains(oem));
                }
                if (!string.IsNullOrWhiteSpace(submitUnit))
                {
                    data = data.Where(w => w.CQName.Contains(submitUnit));
                }
                if (!string.IsNullOrWhiteSpace(machineryModel))
                {
                    data = data.Where(w => w.MachineryModel.Contains(machineryModel));
                }
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    data = data.Where(w => w.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    data = data.Where(w => w.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }
                if (isReview == 0)
                {
                    // 未处理
                    data = data.Where(w => w.State == 1);
                }
                else
                {
                    // 处理过
                    data = data.Where(w => w.State == 2 || w.State == 3);
                    if (begin != null && end != null)
                    {
                        data = data.Where(w => w.ReviewDate >= begin && w.ReviewDate <= end);
                    }
                }

                var count = await data.CountAsync();
                if (count == 0)
                {
                    return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<ReviewUseViewModel>(), count);
                }
                if (page == 0)
                {
                    page = 1;
                }
                if (limit == 0)
                {
                    limit = 10;
                }
                var result = await data.OrderByDescending(o => o.SubmitDate)
                    .Skip((page - 1) * limit).Take(limit)
                    .AsNoTracking()
                    .ToListAsync();
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取审核的机械列表：" + ex.Message);
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 产权备案审核列表
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="limit">条数</param>
        /// <param name="machineryType">机械类型</param>
        /// <param name="machineryName">机械名称</param>
        /// <param name="oem">设备厂家</param>
        /// <param name="machineryModel">机械型号</param>
        /// <param name="isReview">是否审核过，0：未处理，1：已处理</param>
        /// <param name="begin">处理开始日期</param>
        /// <param name="end">处理结束日期</param>
        /// <param name="submitUnit">提交单位</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult>
            GetReviewMachineryExcel(MachineryTypeEnum? machineryType,
                string oem, string machineryModel, int isReview, DateTime? begin, DateTime? end, string leaveTheFactoryNo
            , string propertyRightsRecordNo, string submitUnit)
        {
            try
            {
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var data = from A in _context.MachineryInfos
                           join B in _context.EntRegisterInfoMag
                           on A.EntGUID equals B.EntRegisterInfoMagId
                           where A.DeleteMark == 0 && A.ReviewBelongedTo == belongedTo
                           select new ReviewUseViewModelExcel
                           {

                               MachineryType = A.MachineryType,
                               MachineryModel = A.MachineryModel,
                               PropertyRightsRecordNo = A.PropertyRightsRecordNo,
                               OEM = A.OEM,
                               CQName = B.EntName,
                               LeaveTheFactoryDate = A.LeaveTheFactoryDate,//出厂日期                             
                               LeaveTheFactoryNo = A.LeaveTheFactoryNo,//出厂编号
                               ManufacturingLicense = A.ManufacturingLicense,//制造许可证编号                              
                               MaxRatedLiftingCapacity = A.MaxRatedLiftingCapacity,
                               MaxInstallHeight = A.MaxInstallHeight,
                               AttachedHeight = A.AttachedHeight,
                               MaxRange = A.MaxRange,
                               Knm = A.Knm,
                               FreeStandingHeight = A.FreeStandingHeight,
                               BuyDate = A.BuyDate,
                               SubmitDate = A.SubmitDate,
                               ReviewDate = A.ReviewDate,
                               State = A.State

                           };
                if (machineryType != null)
                {
                    data = data.Where(w => w.MachineryType == machineryType);
                }
                if (!string.IsNullOrWhiteSpace(oem))
                {
                    data = data.Where(w => w.OEM.Contains(oem));
                }
                if (!string.IsNullOrWhiteSpace(submitUnit))
                {
                    data = data.Where(w => w.CQName.Contains(submitUnit));
                }
                if (!string.IsNullOrWhiteSpace(machineryModel))
                {
                    data = data.Where(w => w.MachineryModel.Contains(machineryModel));
                }
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    data = data.Where(w => w.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    data = data.Where(w => w.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }
                if (isReview == 0)
                {
                    // 未处理
                    data = data.Where(w => w.State == 1);
                }
                else
                {
                    // 处理过
                    data = data.Where(w => w.State == 2 || w.State == 3);
                    if (begin != null && end != null)
                    {
                        data = data.Where(w => w.ReviewDate >= begin && w.ReviewDate <= end);
                    }
                }

                var count = await data.CountAsync();
                if (count == 0)
                {
                    //无数据
                    return NoContent();
                }

                var result = await data.OrderByDescending(o => o.SubmitDate)
                    .AsNoTracking()
                    .ToListAsync();

                var list = result.Select(s => new List<string>
                {
                    s.MachineryType.ToString(),
                     s.MachineryModel,
                     s.PropertyRightsRecordNo,
                     s.OEM,
                     s.CQName,
                     s.LeaveTheFactoryDate == null ? "" : ((DateTime)s.LeaveTheFactoryDate).ToString("yyyy-MM-dd"),
                     s.LeaveTheFactoryNo,
                     s.ManufacturingLicense,
                   s.MaxRatedLiftingCapacity == null ? "" : s.MaxRatedLiftingCapacity.ToString(),
                    s.FreeStandingHeight == null ? "" : s.FreeStandingHeight.ToString(),
                   s.AttachedHeight== null ? "" : s.AttachedHeight.ToString(),
                   s.MaxRange == null ? "" : s.MaxRange.ToString(),
                   s.Knm== null ? "" : s.Knm.ToString(),
                   s.BuyDate == null ? "" : ((DateTime)s.BuyDate).ToString("yyyy-MM-dd") ,
                   s.SubmitDate == null ? "" : ((DateTime)s.SubmitDate).ToString("yyyy-MM-dd") ,
                   s.ReviewDate== null ? "" : ((DateTime)s.ReviewDate).ToString("yyyy-MM-dd")
                });

                List<List<string>> listL = new List<List<string>>();
                var biaoti = new List<string>()
                {
                    "设备类型","设备型号","设备信息号","生产厂家","提交单位","出厂日期","出厂编号","制造许可证编号"
                    ,"最大起重量(kg)","最大独立高度(m)","最大附着高度(m)","最大幅度(m)","额定起重力矩(KN·m)"
                    ,"购买时间","提交时间" ,"审核时间"
                };

                listL.Add(biaoti);

                listL.AddRange(list);

                var fileName = "信息登记信息导出";
                return DataTableExcel2(listL, fileName);

            }
            catch (Exception ex)
            {
                _logger.LogError("获取审核的机械列表：" + ex.Message);
                return NoContent();
            }
        }

        public IActionResult DataTableExcel2(List<List<string>> dataTable, string fileName, string sheetName = "name")
        {
            try
            {
                //创建EXCEL工作薄
                IWorkbook workBook = new XSSFWorkbook();
                //创建sheet文件表
                ISheet sheet = workBook.CreateSheet(sheetName);


                #region 填充Excel单元格中的数据

                //给工作薄中非表头填充数据，遍历行数据并进行创建和填充表格
                for (int i = 0; i < dataTable.Count; i++)
                {
                    IRow row = sheet.CreateRow(i);//表示从整张数据表的第二行开始创建并填充数据，第一行已经创建。
                    for (int j = 0; j < dataTable[i].Count; j++)//遍历并创建每个单元格cell，将行数据填充在创建的单元格中。
                    {
                        //string fieldName = dataTable[j].ToString();// 字段名
                        //ExcelCell columnModel = SetColumnName(i);

                        //将数据读到cell单元格中
                        ICell cell = row.CreateCell(j);
                        cell.SetCellValue(dataTable[i][j]);

                        row.Cells.Add(cell);
                    }
                }
                #endregion
                //工作流创建Excel文件
                //工作流写入，通过流的方式进行创建生成文件
                MemoryStream stream = new MemoryStream();
                workBook.Write(stream);

                byte[] buffer = stream.ToArray();

                return File(buffer, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName + ".xlsx");
            }
            catch (Exception ex)
            {
                return NoContent();
            }

        }


        /// <summary>
        /// 信息备案审核
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> ReviewMachinery([FromBody] ReviewMachineryViewModel viewModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(viewModel.MachineryInfoId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var userName = User.FindFirst(ClaimsIdentity.DefaultNameClaimType)?.Value;//当前人名字
                var data = await _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId && w.State == 1
                        && w.ReviewBelongedTo == belongedTo
                        && (w.CancellationState == CancellationStateEnum.审核不通过 || w.CancellationState == CancellationStateEnum.未提交))
                    .OrderByDescending(o => o.SubmitDate)
                    .FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"机械不存在或已被审核，无需重复操作");
                }
                var now = DateTime.Now;
                data.State = viewModel.State;
                data.Reason = viewModel.Reason;
                data.ReviewDate = now;
                data.ReviewUserId = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                data.ReviewUserName = userName;
                if (viewModel.State == 2)
                {

                    data.PropertyRightsRecordNo = Util.GetMachineryRecordNo(_context, data.MachineryType, belongedTo);
                }
                _context.MachineryInfos.Update(data);
                if (!string.IsNullOrEmpty(data.MachineryRecordId))
                {
                    var record = await _context.MachineryRecords
                   .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId && w.MachineryRecordId == data.MachineryRecordId)
                   .OrderByDescending(o => o.CreateDate)
                   .FirstOrDefaultAsync();
                    if (record != null)
                    {
                        record.UpdateDate = now;
                        record.State = (CancellationStateEnum)viewModel.State;
                        record.Reason = viewModel.Reason;
                        record.ReviewUserId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                        _context.MachineryRecords.Update(record);
                    }
                }
                else
                {
                    var record = await _context.MachineryRecords
                  .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId)
                  .OrderByDescending(o => o.CreateDate)
                  .FirstOrDefaultAsync();
                    if (record != null)
                    {
                        record.UpdateDate = now;
                        record.State = (CancellationStateEnum)viewModel.State;
                        record.Reason = viewModel.Reason;
                        record.ReviewUserId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                        _context.MachineryRecords.Update(record);
                    }
                }
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("信息入库审核：" + ex.Message, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        #region 提交，撤回，流程图
        /// <summary>
        /// 提交申请
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="belongedTo"></param>
        /// <param name="workType">申请类型</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> SubmitMachineryInfo(string typeId, string belongedTo, string workType)
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//企业表Id    

                //修改时间
                if (workType == "PropertyRecord")
                {
                    //修改机械信息表的状态
                    var machModel = await _context.MachineryInfos.Where(w => w.EntGUID == tokenId
        && w.MachineryInfoId == typeId).FirstOrDefaultAsync();

                    machModel.State = 1;
                    machModel.ReviewBelongedTo = belongedTo;
                    machModel.SubmitDate = DateTime.Now;
                    //在机械信息表里面增加一条提交记录
                    MachineryRecord mrModel = new MachineryRecord()
                    {
                        MachineryRecordId = SecurityManage.GuidUpper(),
                        MachineryInfoId = typeId,
                        State = (CancellationStateEnum)1,
                        ReviewBelongedTo = belongedTo,
                        DeleteMark = 0,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        SubmitUserId = tokenId,
                    };
                    machModel.MachineryRecordId = mrModel.MachineryRecordId;

                    _context.MachineryInfos.Update(machModel);
                    _context.MachineryRecords.Add(mrModel);

                }
                else if (workType == "PropertyChange")
                {



                    //修改信息变更表里面的状态
                    var changeModel = await _context.ChangeOfTitles.Where(w => w.ChangeOfTitleId == typeId).FirstOrDefaultAsync();

                    changeModel.State = 1;
                    changeModel.BelongedTo = belongedTo;
                    changeModel.SubmitDate = DateTime.Now;
                    changeModel.SubmitUserId = tokenId;
                    _context.ChangeOfTitles.Update(changeModel);

                    //在机械信息表里面增加一条提交记录
                    ChangeRecord crModel = new ChangeRecord()
                    {
                        ChangeRecordId = SecurityManage.GuidUpper(),
                        MachineryInfoId = typeId,
                        State = (CancellationStateEnum)1,
                        ReviewBelongedTo = belongedTo,
                        DeleteMark = 0,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        SubmitUserId = tokenId,
                    };

                    _context.ChangeRecords.Add(crModel);

                }
                else
                {

                    var canceModel = await _context.CancellationOfPropertyRights.Where(w => w.CancellationOfPropertyRightsId
                    == typeId && w.State != (CancellationStateEnum)3).FirstOrDefaultAsync();
                    //判断有无注销记录
                    //if (canceModel == null)
                    //{
                    CancellationOfPropertyRights canModel = new CancellationOfPropertyRights()
                    {
                        CancellationOfPropertyRightsId = SecurityManage.GuidUpper(),
                        MachineryInfoId = typeId,
                        State = (CancellationStateEnum)1,
                        ReviewBelongedTo = belongedTo,
                        DeleteMark = 0,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        SubmitUserId = tokenId,
                    };

                    var machModel = await _context.MachineryInfos.Where(w => w.EntGUID == tokenId
        && w.MachineryInfoId == typeId).FirstOrDefaultAsync();


                    if (machModel.CheckState == (MachineryState)2)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "当前机械已经安装告知无法注销", "当前机械已经安装告知无法注销");

                    }

                    machModel.CancellationOfPropertyRightsId = canModel.CancellationOfPropertyRightsId;
                    machModel.CancellationState = (CancellationStateEnum)1;
                    machModel.CancellationBelongedTo = belongedTo;
                    machModel.CancellationOfPropertySubmitDate = DateTime.Now;
                    _context.MachineryInfos.Update(machModel);

                    _context.CancellationOfPropertyRights.Add(canModel);


                }


                _context.SaveChanges();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "提交成功,请等待审核");

            }
            catch (Exception ex)
            {

                _logger.LogError("提交产权备案申请接口失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }

        }



        #region 已检测机械列表-安监站端查看

        /// <summary>
        /// 获取检测所列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <param name="projectName"></param>
        /// <param name="machineryType"></param>
        /// <param name="machineryName"></param>
        /// <param name="machineryModel"></param>
        /// <param name="checkState">检测状态</param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<ReviewUseViewModel>>>
            GetCheckMachineryManage(int page, int limit,
                string projectName, string recordNumber, string leaveTheFactoryNo, string propertyRightsRecordNo,
                int checkState, DateTime? begin, DateTime? end)
        {
            try
            {

                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//操作人科室
                var loginBelongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;

                var data = _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0
                        && w.BelongedTo == loginBelongedTo);





                if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                {
                    data = from A in data
                           join B in _context.ProjectOverview
                           on new { A.BelongedTo, A.RecordNumber } equals new { B.BelongedTo, B.RecordNumber }
                           where B.SupervisionDepartmentId == supervisionDepartmentId
                           select A;
                }

                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    data = data.Where(w => w.ProjectName.Contains(projectName));
                }
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    data = data.Where(w => w.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    data = data.Where(w => w.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    data = data.Where(w => w.RecordNumber.Contains(recordNumber));
                }

                if (checkState == 1)
                {
                    // 未检测
                    data = data.Where(w => w.MachineryCheckState == MachineryCheckStateEnum.检测中
                    || w.MachineryCheckState == MachineryCheckStateEnum.复检中);
                }
                else if (checkState == 2)
                {
                    //检测合格
                    data = data.Where(w => w.MachineryCheckState == MachineryCheckStateEnum.检测合格
                        || w.MachineryCheckState == MachineryCheckStateEnum.复检合格);
                }
                else if (checkState == 3)
                {
                    //检测不合格
                    data = data.Where(w => w.MachineryCheckState == MachineryCheckStateEnum.检测不合格
                        || w.MachineryCheckState == MachineryCheckStateEnum.复检不合格);
                }
                else if (checkState == 4)
                {
                    //整改中
                    data = from a in data
                           join b in _context.CheckRecords
                           on a.CheckRecordId equals b.CheckRecordId
                           where ((a.MachineryCheckState == MachineryCheckStateEnum.检测不合格
                        || a.MachineryCheckState == MachineryCheckStateEnum.检测合格
                        || a.MachineryCheckState == MachineryCheckStateEnum.复检合格
                        || a.MachineryCheckState == MachineryCheckStateEnum.复检不合格
                        || a.MachineryCheckState == MachineryCheckStateEnum.非我所检测
                        ) && b.IsRectificationCheckFinish == 0)
                           select a;
                }


                if (begin != null && end != null)
                {
                    DateTime endTime = ((DateTime)end).AddDays(1);
                    data = data.Where(w => (w.CheckReviewDate >= begin && w.CheckReviewDate < end)
                     || (w.ReCheckReviewDate >= begin && w.ReCheckReviewDate < end));
                }
                var count = await data.CountAsync();
                if (count == 0)
                {
                    return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<ReviewUseViewModel>(), count);
                }
                if (page == 0)
                {
                    page = 1;
                }
                if (limit == 0)
                {
                    limit = 10;
                }


                var installs = await _context.InstallationNotificationRecords
                    .Where(w => w.DeleteMark == 0 && w.Type == 0 && w.State == 2)
                    .Select(k => new { k.InstallationPosition, k.MachineryInfoId, k.Id })
                    .ToListAsync();

                var result = await data.OrderByDescending(o => o.ReviewDate)
                    .Skip((page - 1) * limit).Take(limit)
                    .Select(s => new ReviewUseViewModel
                    {
                        MachineryCheckState = s.MachineryCheckState,
                        MachineryInfoId = s.MachineryInfoId,
                        EntGUID = s.EntGUID,
                        MachineryType = s.MachineryType,
                        MachineryName = s.MachineryName,
                        MachineryModel = s.MachineryModel,
                        OEM = s.OEM,
                        ManufacturingLicense = s.ManufacturingLicense,
                        LeaveTheFactoryNo = s.LeaveTheFactoryNo,
                        MaxRatedLiftingCapacity = s.MaxRatedLiftingCapacity,
                        LeaveTheFactoryDate = s.LeaveTheFactoryDate,
                        Knm = s.Knm,
                        MaxInstallHeight = s.MaxInstallHeight,
                        FreeStandingHeight = s.FreeStandingHeight,
                        AttachedHeight = s.AttachedHeight,
                        MaxRange = s.MaxRange,
                        BuyDate = s.BuyDate,
                        TestingInstituteInfoId = s.TestingInstituteInfoId,
                        CheckReviewDate = s.CheckReviewDate,
                        CheckSubmitDate = s.CheckSubmitDate,
                        BelongedTo = s.BelongedTo,
                        CheckRecordId = s.CheckRecordId,
                        RecordNumber = s.RecordNumber,
                        ProjectName = s.ProjectName,
                        EntName = s.EntName,
                        EntCode = s.EntCode,
                        PropertyRightsRecordNo = s.PropertyRightsRecordNo,
                        ReCheckReviewDate = s.ReCheckReviewDate,
                        ReCheckSubmitDate = s.ReCheckSubmitDate,
                        HistoricalRectification = _context.RectificationCheckDetails.Where(a => a.CheckRecordId == s.CheckRecordId && a.MachineryInfoId == s.MachineryInfoId).Select(w => w.CheckRecordId).FirstOrDefault(),

                        IsRectificationCheckFinish = _context.CheckRecords.Where(w => w.CheckRecordId == s.CheckRecordId).OrderByDescending(o => o.CreateDate)
                             .Select(s => s.IsRectificationCheckFinish).FirstOrDefault(),
                        TestingName = _context.TestingInstituteUser.Where(w => w.TestingInstituteInfoId == s.TestingInstituteInfoId).OrderByDescending(o => o.CreateDate)
                            .Select(s => s.TestingInstituteUserName).FirstOrDefault(),
                        InstallationPosition = _context.InstallationNotificationRecords.Where(w => w.MachineryInfoId == s.MachineryInfoId && w.DeleteMark == 0 && w.Type == 0 && w.State == 2)
                        .OrderByDescending(o => o.Id).Select(k => k.InstallationPosition).FirstOrDefault()
                    })

                    .AsNoTracking()
                    .ToListAsync();
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取机械检测记录：" + ex.Message, ex);
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        #endregion

        /// <summary>
        /// 删除产权信息
        /// </summary>
        /// <param name="machineryInfoId">机械ID</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> DelMachineryInfo([FromQuery] string machineryInfoId)
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//企业表Id   

                var delModel = await _context.MachineryInfos.Where(w => w.DeleteMark == 0 && w.MachineryInfoId == machineryInfoId
                 && w.EntGUID == tokenId).FirstOrDefaultAsync();

                delModel.DeleteMark = 1;

                _context.MachineryInfos.Update(delModel);

                _context.SaveChanges();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, tokenId);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }


        }

        /// <summary>
        /// 撤回
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> RevokeMachineryInfo(string typeId, string workType)
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//企业表Id   


                if (workType == "PropertyRecord")
                {
                    //修改产权入库状态,时间
                    var machModel = await _context.MachineryInfos.Where(w => w.EntGUID == tokenId
        && w.MachineryInfoId == typeId).FirstOrDefaultAsync();

                    if (machModel.State == 2)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "审核已通过无法撤回");
                    }
                    machModel.State = 0;
                    //machModel.ReviewBelongedTo = "";
                    _context.MachineryInfos.Update(machModel);


                }
                else if (workType == "PropertyChange")
                {
                    //修改产权变更状态,时间
                    var changeModel = await _context.ChangeOfTitles.Where(w => w.ChangeOfTitleId == typeId).FirstOrDefaultAsync();
                    if (changeModel.State == 2)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "审核已通过无法撤回");
                    }
                    changeModel.State = 0;
                    //changeModel.BelongedTo = "";
                    _context.ChangeOfTitles.Update(changeModel);
                }
                else
                {
                    //修改产权注销状态,时间
                    var canceModel = await _context.MachineryInfos.Where(w => w.CancellationOfPropertyRightsId
                        == typeId).FirstOrDefaultAsync();
                    if (canceModel.CancellationState == (CancellationStateEnum)2)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "审核已通过无法撤回");
                    }
                    canceModel.CancellationState = 0;
                    //canceModel.CancellationBelongedTo = "";
                    _context.MachineryInfos.Update(canceModel);

                }

                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "撤销成功");
                //    }

            }
            catch (Exception ex)
            {

                _logger.LogError("提交产权备案申请接口失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// 查看流程图  --暂不使用 2019.12.7
        /// </summary>
        /// <param name="BelongedTo"></param>
        /// <param name="RecordNumber"></param>
        /// <param name="WorkflowType"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<workflowViewImg>> SelectTaskImg(string entCode, string machineryInfoId, string workflowType)
        {
            try
            {
                var orderId = machineryInfoId;
                var te = await _context.WorkflowTemplate.Where(w => w.WorkflowType == workflowType).ToListAsync();
                var ie = await _context.WorkflowInstance.Where(w => w.OrderId == orderId && w.Status != "DELETE").ToListAsync();
                var tk = await _context.WorkflowTask.Where(w => w.Flag == "open").ToListAsync();
                var ul = await _context.WorkflowUrl.ToListAsync();
                var dataSet = (from k in tk
                               join i in ie
                               on k.ProcessInstanceId equals i.ProcessInstanceId
                               join t in te
                               on i.ProcessDefinitionId equals t.ProcessDefinitionId
                               join u in ul
                               on k.FormResourceName equals u.UrlType
                               select new workflowViewModel
                               {
                                   WorkflowName = t.WorkflowName,
                                   WorkflowType = t.WorkflowType,
                                   ProcessDefinitionId = t.ProcessDefinitionId,
                                   Img_path = t.ImgPath,
                                   FormResourceName = k.FormResourceName,
                                   Coordinate = u.Coordinate,
                                   Contact = u.Contact

                               }).ToList();

                if (dataSet.Count > 0)
                {

                    var Coordinate = dataSet[0].Coordinate;
                    var Contact = dataSet[0].Contact;
                    int left = 0;
                    int top = 0;
                    int width = 0;
                    int height = 0;
                    if (Coordinate != "")
                    {
                        left = Int32.Parse(Coordinate.Split(',')[0]);
                        top = Int32.Parse(Coordinate.Split(',')[1]);
                        width = Int32.Parse(Coordinate.Split(',')[2]);
                        height = Int32.Parse(Coordinate.Split(',')[3]);
                    }
                    int d_left = left + width + 20; // div left 获取文本框left+width        
                    var ImageUrl = Util.GetBaseUrl(Request) + "xml/" + dataSet[0].Img_path;


                    workflowViewImg viewModel = new workflowViewImg()
                    {
                        //literal1 = literal1,
                        //literal2 = literal2,
                        Contact = Contact,
                        Left = left,
                        Top = top,
                        Width = width,
                        Height = height,
                        D_Left = d_left,
                        ImgUrl = ImageUrl,
                    };

                    return ResponseViewModel<workflowViewImg>.Create(Status.SUCCESS, Message.SUCCESS, viewModel);
                    //InitImage(dataSet[0], str, dataSet[0].Img_path);
                }





                return ResponseViewModel<workflowViewImg>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("查看流程图接口失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<workflowViewImg>.Create(Status.ERROR, Message.ERROR);
            }
        }
        #endregion



        /// <summary>
        /// 文件上传列表
        /// </summary>
        /// <param name="machineryInfoId"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<MachineryConfigView>>> MachineryFileList(string machineryInfoId)
        {
            try
            {

                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;

                MachineryInfo jixie = null;
                EntRegisterInfoMag Unitdata = null;
                if (!string.IsNullOrWhiteSpace(machineryInfoId))
                {
                    jixie = await _context.MachineryInfos.Where(a => a.MachineryInfoId == machineryInfoId).
               OrderByDescending(o => o.Id).FirstOrDefaultAsync();
                }
                if (jixie != null)
                {
                    Unitdata = await _context.EntRegisterInfoMag.Where(a => a.EntRegisterInfoMagId == jixie.EntGUID).
                   OrderByDescending(o => o.Id).FirstOrDefaultAsync();
                }
                else
                {
                    Unitdata = await _context.EntRegisterInfoMag.Where(a => a.EntRegisterInfoMagId == uuid).
                 OrderByDescending(o => o.Id).FirstOrDefaultAsync();
                }
                if (Unitdata == null || string.IsNullOrWhiteSpace(Unitdata.BusinessLicenseUrl))
                {
                    return ResponseViewModel<List<MachineryConfigView>>.Create(Status.ERROR, "请到单位信息维护菜单上传营业执照!");
                }
                var recordAttments = await _context.RecordAttachments.Where(w => w.DeleteMark == 0
                 && w.AttachmentId == machineryInfoId).ToListAsync();
                var recordNumberConfig = await _context.RecordConfigs.Where(w => w.DeleteMark == 0
                && w.Type == RecordConfigTypeEnum.产权备案).OrderBy(x => x.Sort).ToListAsync();
                List<MachineryConfigView> mConfigList = new List<MachineryConfigView>();
                recordNumberConfig.ForEach(x =>
                {
                    MachineryConfigView item = new MachineryConfigView();
                    item.AttachmentName = x.AttachmentName;
                    item.RecordConfigId = x.RecordConfigId;
                    item.TemplateUrl = x.TemplateUrl;
                    item.Required = x.Required;
                    item.FileCount = recordAttments.Where(w => w.RecordConfigId == x.RecordConfigId).Count();
                    if (item.FileCount > 0 && item.AttachmentName != "产权单位营业执照")
                    {
                        item.ReaderOnly = false;
                        item.FileUrl = recordAttments.Where(y => y.RecordConfigId == x.RecordConfigId && y.Type == RecordConfigTypeEnum.产权备案)
                                        .Select(y => new FileUploadUrlViem
                                        {
                                            FileType = x.AttachmentName,
                                            RecordConfigId = y.RecordConfigId,
                                            FileName = y.FileName,
                                            FileUrl = y.FileUrl,
                                            Suffix = Path.GetExtension(y.FileName).Replace(".", "")
                                        }).ToList();
                    }
                    else if (item.AttachmentName == "产权单位营业执照")
                    {
                        if (Unitdata != null)
                        {
                            List<FileUploadUrlViem> zhizhaoList = new List<FileUploadUrlViem>();

                            FileUploadUrlViem zhizhao = new FileUploadUrlViem();
                            zhizhao.FileType = x.AttachmentName;
                            zhizhao.RecordConfigId = x.RecordConfigId;
                            zhizhao.FileName = Unitdata.BusinessLicenseUrl.Substring(Unitdata.BusinessLicenseUrl.LastIndexOf("/") + 1);
                            zhizhao.FileUrl = Unitdata.BusinessLicenseUrl;
                            zhizhao.Suffix = Path.GetExtension(Unitdata.BusinessLicenseUrl).Replace(".", "");
                            zhizhaoList.Add(zhizhao);
                            item.FileUrl = zhizhaoList;
                            item.FileCount = 1;
                            item.ReaderOnly = true;
                            item.Required = x.Required;
                        }
                    }
                    mConfigList.Add(item);
                });
                return ResponseViewModel<List<MachineryConfigView>>.Create(Status.SUCCESS, Message.SUCCESS, mConfigList);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<List<MachineryConfigView>>.Create(Status.ERROR, Message.ERROR);
            }


        }

        /// <summary>
        /// 获取检测所列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <param name="projectName"></param>
        /// <param name="machineryType"></param>
        /// <param name="machineryName"></param>
        /// <param name="machineryModel"></param>
        /// <param name="isReview"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "检测所,南京检测所,无锡检测所")]
        public async Task<ResponseViewModel<List<ReviewUseViewModel>>>
            GetCheckMachineryWrite(int page, int limit,
                string projectName, string recordNumber, string leaveTheFactoryNo, int machineryType, string propertyRightsRecordNo,
                string machineryModel, int isReview, string checkNumber, DateTime? begin, DateTime? end)
        {
            try
            {

                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;
                if (string.IsNullOrEmpty(testingId))
                {
                    return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.FAIL, Message.FAIL);
                }
                var data = from a in _context.MachineryInfos
                           join b in _context.CheckRecords
                           on a.CheckRecordId equals b.CheckRecordId
                           where a.DeleteMark == 0 && (a.MachineryType == MachineryTypeEnum.塔式起重机
                    || a.MachineryType == MachineryTypeEnum.施工升降机)
                        && a.TestingInstituteInfoId == testingId
                           select new { a, b.IsRectificationCheckFinish };//IsRectificationCheckFinish=0 待整改 1整改完成

                if (machineryType == 0 || machineryType == 1)
                {
                    data = data.Where(w => w.a.MachineryType == (MachineryTypeEnum)machineryType);
                }
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    data = data.Where(w => w.a.ProjectName.Contains(projectName));
                }
                if (!string.IsNullOrWhiteSpace(checkNumber))
                {
                    data = data.Where(w => w.a.CheckNumber.Contains(checkNumber));
                }
                if (!string.IsNullOrWhiteSpace(machineryModel))
                {
                    data = data.Where(w => w.a.MachineryModel.Contains(machineryModel));
                }
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    data = data.Where(w => w.a.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    data = data.Where(w => w.a.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    data = data.Where(w => w.a.RecordNumber.Contains(recordNumber));
                }

                if (isReview == 0)
                {
                    // 未处理
                    data = data.Where(w => w.a.MachineryCheckState == MachineryCheckStateEnum.检测中
                    || w.a.MachineryCheckState == MachineryCheckStateEnum.复检中);
                    data = data.OrderByDescending(o => o.a.CheckSubmitDate);
                }
                else if (isReview == 2)
                {
                    //整改中
                    data = data.Where(w => (w.a.MachineryCheckState == MachineryCheckStateEnum.检测不合格
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.检测合格
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.复检合格
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.复检不合格
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.非我所检测
                        ) && w.IsRectificationCheckFinish == 0);

                }
                else if (isReview == 1)
                {

                    // 处理过
                    data = data.Where(w => (w.a.MachineryCheckState == MachineryCheckStateEnum.检测不合格
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.检测合格
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.复检合格
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.复检不合格
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.非我所检测
                        ) && (w.IsRectificationCheckFinish == 1 || w.IsRectificationCheckFinish == null));

                    if (begin != null && end != null)
                    {
                        DateTime endTime = ((DateTime)end).AddDays(1);
                        data = data.Where(w => (w.a.CheckReviewDate >= begin && w.a.CheckReviewDate < end)
                         || (w.a.ReCheckReviewDate >= begin && w.a.ReCheckReviewDate < end));
                    }

                    data = data.OrderByDescending(o => o.a.CheckReviewDate);
                }

                var count = await data.CountAsync();
                if (count == 0)
                {
                    return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<ReviewUseViewModel>(), count);
                }
                if (page == 0)
                {
                    page = 1;
                }
                if (limit == 0)
                {
                    limit = 10;
                }
                var result = await data
                    .Skip((page - 1) * limit).Take(limit)
                    .Select(s => new ReviewUseViewModel
                    {
                        MachineryInfoId = s.a.MachineryInfoId,
                        CheckNumber = s.a.CheckNumber,
                        EntGUID = s.a.EntGUID,
                        MachineryType = s.a.MachineryType,
                        MachineryName = s.a.MachineryName,
                        MachineryModel = s.a.MachineryModel,
                        OEM = s.a.OEM,
                        ManufacturingLicense = s.a.ManufacturingLicense,
                        LeaveTheFactoryNo = s.a.LeaveTheFactoryNo,
                        MaxRatedLiftingCapacity = s.a.MaxRatedLiftingCapacity,
                        LeaveTheFactoryDate = s.a.LeaveTheFactoryDate,
                        Knm = s.a.Knm,
                        CheckState = s.a.CheckState,
                        MaxInstallHeight = s.a.MaxInstallHeight,
                        FreeStandingHeight = s.a.FreeStandingHeight,
                        AttachedHeight = s.a.AttachedHeight,
                        MaxRange = s.a.MaxRange,
                        BuyDate = s.a.BuyDate,
                        TestingInstituteInfoId = s.a.TestingInstituteInfoId,
                        CheckReviewDate = s.a.CheckReviewDate,
                        CheckSubmitDate = s.a.CheckSubmitDate,
                        BelongedTo = s.a.BelongedTo,
                        IsReview = isReview,
                        CheckRecordId = s.a.CheckRecordId,
                        RecordNumber = s.a.RecordNumber,
                        ProjectName = s.a.ProjectName,
                        EntName = s.a.EntName,
                        EntCode = s.a.EntCode,
                        PropertyRightsRecordNo = s.a.PropertyRightsRecordNo,
                        ReCheckReviewDate = s.a.ReCheckReviewDate,
                        ReCheckSubmitDate = s.a.ReCheckSubmitDate,
                        IsRectificationCheckFinish = s.IsRectificationCheckFinish,

                        //IsRectificationCheckFinish = _context.CheckRecords.Where(w => w.CheckRecordId == s.CheckRecordId).OrderByDescending(o => o.CreateDate)
                        //    .Select(s => s.IsRectificationCheckFinish).FirstOrDefault(),
                        TestingName = _context.TestingInstituteUser.Where(w => w.TestingInstituteInfoId == s.a.TestingInstituteInfoId).OrderByDescending(o => o.CreateDate)
                            .Select(s => s.TestingInstituteUserName).FirstOrDefault()

                    })
                    .AsNoTracking()
                    .ToListAsync();
                result.ForEach(m =>
                {
                    m.IsCanRecall = 0;
                    if (m.IsRectificationCheckFinish != 1 && m.CheckState == MachineryState.安装告知审核通过)
                    {
                        m.IsCanRecall = 1;
                    }

                    //是否配置检测仪器
                    m.CheckEquipmentConfiguresCount = _context.CheckEquipmentConfigures.Where(w => w.TestingInstituteInfoId == testingId && w.DeleteMark == 0).OrderByDescending(w => w.Id).Count();

                    //是否保存补充资料
                    var buchongziliao = _context.MachineryInfoSupplementaryInformations.Where(w => w.MachineryInfoId == m.MachineryInfoId && w.CheckRecordId == m.CheckRecordId && w.DeleteMark == 0).OrderByDescending(w => w.Id).FirstOrDefault();

                    // 是否保存检验结果和结论
                    var jyjg = _context.CheckDetails
                        .Where(w => w.MachineryInfoId == m.MachineryInfoId && w.CheckRecordId == m.CheckRecordId)
                        .Count();

                    m.IsBuildReport = 0;
                    if (buchongziliao != null)
                    {
                        m.ValiDate = buchongziliao.ValiDate;
                    }

                    if (jyjg > 0 && buchongziliao != null)
                    {
                        m.IsBuildReport = 1;

                    }

                });
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取机械检测记录：" + ex.Message, ex);
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 获取检测所列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <param name="projectName"></param>
        /// <param name="machineryType"></param>
        /// <param name="machineryName"></param>
        /// <param name="machineryModel"></param>
        /// <param name="isReview"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "检测所,南京检测所")]
        public async Task<ResponseViewModel<List<ReviewUseViewModel>>>
            GetCheckMachinery(int page, int limit,
                string projectName, string recordNumber, string leaveTheFactoryNo, string propertyRightsRecordNo,
                int isReview, DateTime? begin, DateTime? end)
        {
            try
            {

                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;
                var data = _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0
                        && w.TestingInstituteInfoId == testingId);
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    data = data.Where(w => w.ProjectName.Contains(projectName));
                }
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    data = data.Where(w => w.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    data = data.Where(w => w.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    data = data.Where(w => w.RecordNumber.Contains(recordNumber));
                }

                if (isReview == 0)
                {
                    // 未处理
                    data = data.Where(w => w.MachineryCheckState == MachineryCheckStateEnum.检测中
                    || w.MachineryCheckState == MachineryCheckStateEnum.复检中);
                }
                else
                {

                    // 处理过
                    data = data.Where(w => w.MachineryCheckState == MachineryCheckStateEnum.检测不合格
                        || w.MachineryCheckState == MachineryCheckStateEnum.检测合格
                        || w.MachineryCheckState == MachineryCheckStateEnum.复检合格
                        || w.MachineryCheckState == MachineryCheckStateEnum.复检不合格
                        || w.MachineryCheckState == MachineryCheckStateEnum.非我所检测);

                    if (begin != null && end != null)
                    {
                        DateTime endTime = ((DateTime)end).AddDays(1);
                        data = data.Where(w => (w.CheckReviewDate >= begin && w.CheckReviewDate < end)
                         || (w.ReCheckReviewDate >= begin && w.ReCheckReviewDate < end));
                    }
                }

                var count = await data.CountAsync();
                if (count == 0)
                {
                    return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<ReviewUseViewModel>(), count);
                }
                if (page == 0)
                {
                    page = 1;
                }
                if (limit == 0)
                {
                    limit = 10;
                }
                var result = await data.OrderByDescending(o => o.ReviewDate)
                    .Skip((page - 1) * limit).Take(limit)
                    .Select(s => new ReviewUseViewModel
                    {
                        MachineryInfoId = s.MachineryInfoId,
                        EntGUID = s.EntGUID,
                        MachineryType = s.MachineryType,
                        MachineryName = s.MachineryName,
                        MachineryModel = s.MachineryModel,
                        OEM = s.OEM,
                        ManufacturingLicense = s.ManufacturingLicense,
                        LeaveTheFactoryNo = s.LeaveTheFactoryNo,
                        MaxRatedLiftingCapacity = s.MaxRatedLiftingCapacity,
                        LeaveTheFactoryDate = s.LeaveTheFactoryDate,
                        Knm = s.Knm,
                        MaxInstallHeight = s.MaxInstallHeight,
                        FreeStandingHeight = s.FreeStandingHeight,
                        AttachedHeight = s.AttachedHeight,
                        MaxRange = s.MaxRange,
                        BuyDate = s.BuyDate,
                        TestingInstituteInfoId = s.TestingInstituteInfoId,
                        CheckReviewDate = s.CheckReviewDate,
                        CheckSubmitDate = s.CheckSubmitDate,
                        BelongedTo = s.BelongedTo,
                        IsReview = isReview,
                        CheckRecordId = s.CheckRecordId,
                        RecordNumber = s.RecordNumber,
                        ProjectName = s.ProjectName,
                        EntName = s.EntName,
                        EntCode = s.EntCode,
                        PropertyRightsRecordNo = s.PropertyRightsRecordNo,
                        ReCheckReviewDate = s.ReCheckReviewDate,
                        ReCheckSubmitDate = s.ReCheckSubmitDate,
                        IsRectificationCheckFinish = _context.CheckRecords.Where(w => w.CheckRecordId == s.CheckRecordId).OrderByDescending(o => o.CreateDate)
                            .Select(s => s.IsRectificationCheckFinish).FirstOrDefault(),
                        TestingName = _context.TestingInstituteUser.Where(w => w.TestingInstituteInfoId == s.TestingInstituteInfoId).OrderByDescending(o => o.CreateDate)
                            .Select(s => s.TestingInstituteUserName).FirstOrDefault()
                    })
                    .AsNoTracking()
                    .ToListAsync();
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取机械检测记录：" + ex.Message, ex);
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 上传历史记录
        /// </summary>
        /// <param name="machineryInfoId"></param>
        /// <param name="recordConfigId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<ResponseViewModel<List<MachineryFileHisViewModel>>> MachineryFileHisList(string machineryInfoId, string recordConfigId)
        {

            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//企业表Id    
                MachineryInfo jixie = null;
                EntRegisterInfoMag Unitdata = null;
                if (!string.IsNullOrWhiteSpace(machineryInfoId))
                {
                    jixie = await _context.MachineryInfos.Where(a => a.MachineryInfoId == machineryInfoId).
               OrderByDescending(o => o.Id).FirstOrDefaultAsync();
                }
                if (jixie != null)
                {
                    Unitdata = await _context.EntRegisterInfoMag.Where(a => a.EntRegisterInfoMagId == jixie.EntGUID).
                   OrderByDescending(o => o.Id).FirstOrDefaultAsync();
                }
                else
                {
                    Unitdata = await _context.EntRegisterInfoMag.Where(a => a.EntRegisterInfoMagId == tokenId).
                 OrderByDescending(o => o.Id).FirstOrDefaultAsync();
                }
                var imgType = await _context.RecordConfigs.Where(x => x.RecordConfigId == recordConfigId).FirstOrDefaultAsync();

                if (recordConfigId == "1" || imgType.AttachmentName == "产权单位营业执照")
                {
                    List<MachineryFileHisViewModel> lists = new List<MachineryFileHisViewModel>();
                    MachineryFileHisViewModel malist = new MachineryFileHisViewModel();
                    malist.RecordConfigId = _context.RecordConfigs.Where(x => x.DeleteMark == 0 && x.AttachmentName == "产权单位营业执照" && x.Type == RecordConfigTypeEnum.产权备案).Select(x => x.RecordConfigId).FirstOrDefault();
                    malist.FileType = "产权单位营业执照";
                    malist.Required = true;
                    malist.FileName = Unitdata.BusinessLicenseUrl.Substring(Unitdata.BusinessLicenseUrl.LastIndexOf("/") + 1);
                    malist.FileUrl = Unitdata.BusinessLicenseUrl;
                    malist.Suffix = Path.GetExtension(Unitdata.BusinessLicenseUrl).Replace(".", "");
                    lists.Add(malist);
                    if (malist.FileUrl == null)
                    {
                        return ResponseViewModel<List<MachineryFileHisViewModel>>.Create(Status.SUCCESS, Message.SUCCESS);
                    }

                    return ResponseViewModel<List<MachineryFileHisViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, lists);
                }
                else
                {
                    var malist = await (from a in _context.RecordAttachments
                                        join b in _context.RecordConfigs
                                        on a.RecordConfigId equals b.RecordConfigId
                                        where a.AttachmentId == machineryInfoId
                                            && a.DeleteMark == 0
                                         && a.RecordConfigId == recordConfigId
                                         && a.Type == RecordConfigTypeEnum.产权备案
                                        select new MachineryFileHisViewModel
                                        {
                                            Required = b.Required,
                                            RecordConfigId = b.RecordConfigId,
                                            FileName = a.FileName,
                                            FileUrl = a.FileUrl,
                                            RecordAttachmentId = a.RecordAttachmentId,
                                            CreateDate = a.CreateDate,
                                            Suffix = Path.GetExtension(a.FileUrl).Replace(".", ""),
                                            FileType = b.AttachmentName,
                                        }).OrderByDescending(o => o.CreateDate).ToListAsync();

                    malist.ForEach(x =>
                    {
                        if (x.FileType == "产权单位营业执照")
                        {
                            x.FileType = "产权单位营业执照";
                            x.FileName = Unitdata.BusinessLicenseUrl.Substring(Unitdata.BusinessLicenseUrl.LastIndexOf("/") + 1);
                            x.FileUrl = Unitdata.BusinessLicenseUrl;
                            x.Suffix = Path.GetExtension(Unitdata.BusinessLicenseUrl).Replace(".", "");
                        }

                    });

                    return ResponseViewModel<List<MachineryFileHisViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, malist, malist.Count());

                }



            }
            catch (Exception ex)
            {

                _logger.LogError("上传产权备案图片：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<MachineryFileHisViewModel>>.Create(Status.ERROR, Message.ERROR);
            }

        }




        /// <summary>
        /// 获取检测结果
        /// </summary>
        /// <param name="checkRecordId"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<CheckRecord>> GetMachineryCheckRecord(string checkRecordId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(checkRecordId))
                {
                    return ResponseViewModel<CheckRecord>.Create(Status.FAIL, Message.FAIL);
                }
                var data = await _context.CheckRecords
                    .Where(w => w.CheckRecordId == checkRecordId)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<CheckRecord>.Create(Status.FAIL, "检测结果不存在或已被删除");
                }

                return ResponseViewModel<CheckRecord>.Create(Status.SUCCESS, Message.SUCCESS, data);
            }
            catch (Exception ex)
            {
                _logger.LogError("查看检测报告：" + ex.Message, ex);
                return ResponseViewModel<CheckRecord>.Create(Status.ERROR, Message.ERROR);
            }
        }
    }
}
