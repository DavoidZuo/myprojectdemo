using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Common.DataBll;
using JSAJ.Core.Common.DataUtil;
using JSAJ.Core.Controllers;
using JSAJ.Core.Models;
using JSAJ.Core.Models.LargeMachinery;
using JSAJ.Core.ViewModels;
using JSAJ.Models.Models.Login;
using JSAJ.ViewModels.ViewModels.manager;
using MCUtil.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ViewModels;

namespace JSAJNanJing.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class RegistrationOfUseController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MenusController> _logger;
        private readonly JssanjianmanagerContext _context;
        private OssFileSetting _ossFileSetting;
        private readonly string _wordTemplte;
        private JwtSettings settings;
        private readonly Jsgginterface _jsgginterface;
        public RegistrationOfUseController(IWebHostEnvironment environment, ILogger<MenusController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options, IOptions<OssFileSetting> oss, IOptions<Jsgginterface> ntoptionjsgginterface)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            _wordTemplte = environment.WebRootPath + Path.DirectorySeparatorChar + "doc" + Path.DirectorySeparatorChar;
            settings = options.Value;
            _ossFileSetting = oss.Value;
            _jsgginterface = ntoptionjsgginterface.Value;
        }
        [HttpGet]
        public ResponseViewModel<List<EnumDataViewModel>> GetMachineryTypeList()
        {
            try
            {

                List<EnumDataViewModel> list = EnumHelper.GetMachineryTypeEnum();
                return ResponseViewModel<List<EnumDataViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, list);
            }
            catch (Exception ex)
            {
                _logger.LogError("大型机械列表：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<EnumDataViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// css-待提交检测设备列表-安拆单位提交安拆单位
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="belongedTo"></param>
        /// <param name="recordNumber"></param>
        /// <param name="machineryType"></param>
        /// <param name="machineryName"></param>
        /// <param name="machineryCheckState">99超期未检测</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<RegistrationOfUseListView_Model>>> GetCheckListNew(int pageIndex, int pageSize,
            int machineryType, string machineryName, int machineryCheckState)
        {
            try
            {


                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;//后台账号登录   
                var type = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//0项目登录 1企业登录
                if (type != "1")
                {
                    return ResponseViewModel<List<RegistrationOfUseListView_Model>>.Create(Status.FAIL, Message.FAIL);
                }

                if (pageIndex <= 0 || pageSize <= 0)
                {
                    return ResponseViewModel<List<RegistrationOfUseListView_Model>>.Create(Status.FAIL, "参数错误");
                }
                var data = _context.MachineryInfos.Where(x => x.AnChaiEntUuid == uuid
                && x.DeleteMark == 0 && x.State == 2 && x.AuditStatus == AuditStatusEnum.主管部门已审核
               && x.CheckState == MachineryState.安装告知审核通过
                            ).Select(a =>
                             new RegistrationOfUseListView_Model
                             {
                                 UpdateDate = a.UpdateDate,
                                 RegistrationOfUseId = a.RegistrationOfUseId,
                                 MachineryInfoId = a.MachineryInfoId,
                                 MachineryType = a.MachineryType.GetHashCode(),
                                 MachineryTypeName = ((MachineryTypeEnum)a.MachineryType).ToString(),
                                 MachineryName = a.MachineryName,
                                 MachineryModel = a.MachineryModel,
                                 PropertyRightsRecordNo = a.PropertyRightsRecordNo,//设备信息号
                                 LeaveTheFactoryNo = a.LeaveTheFactoryNo,//出厂编号
                                 ChanQuanEntGUID = a.EntGUID,//所属产权单位id 如果是施工单位进行产权备案那么则是施工单位的id   
                                 CheckStateValue = a.CheckState.GetHashCode(),
                                 CheckState = ((MachineryState)a.CheckState).ToString(),
                                 MachineryCheckStateValue = a.MachineryCheckState.GetHashCode(),
                                 MachineryCheckState = ((MachineryCheckStateEnum)a.MachineryCheckState).ToString(),
                                 InstallReviewDate = a.InstallReviewDate,
                                 //PlanInstallDate = b.PlanInstallDate,//拟安装日期
                                 //PlanUseBeginDate = b.PlanUseBeginDate,//拟使用日期
                                 BuyDate = a.BuyDate,
                                 MaxRatedLiftingCapacity = a.MaxRatedLiftingCapacity,
                                 Knm = a.Knm,
                                 MaxInstallHeight = a.MaxInstallHeight,
                                 MaxRange = a.MaxRange,
                                 UseReason = a.UseReason,
                                 EntCode = a.EntCode,
                                 EntName = a.EntName,
                                 TestingInstituteInfoId = a.TestingInstituteInfoId,
                                 AnChaiEntName = "",
                                 ChanQuanEntName = "",
                                 CheckRecordId = a.CheckRecordId,
                                 RecordUrl = a.CheckUrl,
                             }).ToList();

                if (machineryType >= 0)
                {
                    data = data.Where(x => x.MachineryType == machineryType).ToList();
                }
                if (!string.IsNullOrWhiteSpace(machineryName))
                {
                    data = data.Where(x => x.MachineryName.Contains(machineryName)).ToList();
                }
                if (machineryCheckState >= 0)
                {
                    if (machineryCheckState == 99)
                    {
                        //超期未检测
                        data = data.Where(x => x.MachineryCheckState == MachineryCheckStateEnum.未检测.ToString() && DateTime.Today > ((DateTime)x.PlanInstallDate).AddDays(30)).ToList();
                    }
                    else
                    {
                        data = data.Where(x => x.MachineryCheckState == ((MachineryCheckStateEnum)machineryCheckState).ToString()).ToList();
                    }

                }

                int totalCount = data.Count;
                var dataList = data.OrderByDescending
                    (x => x.UpdateDate).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

                var unitList = await _context.EntRegisterInfoMag
                    .Where(x => dataList.Select(y => y.ChanQuanEntGUID).Contains(x.EntRegisterInfoMagId)
                    || dataList.Select(y => y.AnChaiEntGUID).Contains(x.EntRegisterInfoMagId)
                      || dataList.Select(y => y.EntCode).Contains(x.EntRegisterInfoMagId))
                       .ToListAsync();
                var anzhuanggaozhiList = await _context.InstallationNotificationRecords.Where(x => x.DeleteMark == 0 && x.State == 2 && dataList.Select(y => y.MachineryInfoId).Contains(x.MachineryInfoId))
                    .OrderByDescending(x => x.Id).ToListAsync();
                var jiancedanwei = await _context.TestingInstituteInfo.ToListAsync();
                dataList.ForEach(w =>
                {
                    w.IsRectificationCheckFinishText = "";
                    if (w.CheckState == MachineryState.安装告知审核通过.ToString() && w.InstallReviewDate != null && DateTime.Today >= ((DateTime)w.InstallReviewDate).AddDays(30))
                    {
                        w.CheckState = "超期未检测";
                        w.CheckStateValue = 99;
                    }
                    else if (w.CheckState == MachineryState.安装告知审核通过.ToString())
                    {
                        w.CheckState = "安装告知通过,未检测";
                    }
                    var jiance = _context.CheckRecords.Where(r => r.CheckRecordId == w.CheckRecordId).OrderByDescending(r => r.Id).FirstOrDefault();
                    if (jiance != null)
                    {
                        w.IsRectificationCheckFinish = jiance.IsRectificationCheckFinish;

                        if (jiance.IsRectificationCheckFinish == 0)
                        {
                            w.IsRectificationCheckFinishText = "待整改";

                        }
                        else if (jiance.IsRectificationCheckFinish == 1)
                        {
                            w.IsRectificationCheckFinishText = "整改完毕";
                        }
                    }
                    //var anzhuanggaozhi = _context.InstallationNotificationRecords.Where(x => x.DeleteMark == 0 && x.State == 2 && x.MachineryInfoId == w.MachineryInfoId).OrderByDescending(x => x.Id).FirstOrDefault();
                    var anzhuanggaozhi = anzhuanggaozhiList.Where(x => x.MachineryInfoId == w.MachineryInfoId).OrderByDescending(x => x.Id).FirstOrDefault();
                    if (anzhuanggaozhi != null)
                    {
                        w.PlanInstallDate = anzhuanggaozhi.PlanInstallDate;
                        w.PlanUseBeginDate = anzhuanggaozhi.PlanUseBeginDate;//拟使用日期

                        w.AnChaiEntGUID = anzhuanggaozhi.EntGUID;//安拆单位id   
                    }
                    w.TestingInstituteInfoName = jiancedanwei.Where(x => x.TestingInstituteInfoId == w.TestingInstituteInfoId).Select(x => x.MechanismName).FirstOrDefault();

                    //w.EntCode = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == w.EntCode)
                    //                                   .Select(x => x.EntCode).FirstOrDefault();//使用单位
                    //if (!string.IsNullOrWhiteSpace(w.AnChaiEntGUID))
                    //{
                    //    w.AnChaiEntName = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == w.AnChaiEntGUID)
                    //                                    .Select(x => x.EntName).FirstOrDefault();//安拆单位的名字
                    //}
                    //if (!string.IsNullOrWhiteSpace(w.AnChaiEntGUID))
                    //{
                    //    w.ChanQuanEntName = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == w.ChanQuanEntGUID)
                    //               .Select(x => x.EntName).FirstOrDefault();
                    //}

                    w.EntCode = unitList.Where(x => x.EntRegisterInfoMagId == w.EntCode)
                                                       .Select(x => x.EntCode).FirstOrDefault();//使用单位
                    if (!string.IsNullOrWhiteSpace(w.AnChaiEntGUID))
                    {
                        w.AnChaiEntName = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == w.AnChaiEntGUID)
                                                        .Select(x => x.EntName).FirstOrDefault();//安拆单位的名字
                    }
                    if (!string.IsNullOrWhiteSpace(w.AnChaiEntGUID))
                    {
                        w.ChanQuanEntName = unitList.Where(x => x.EntRegisterInfoMagId == w.ChanQuanEntGUID)
                                   .Select(x => x.EntName).FirstOrDefault();
                    }

                });
                return ResponseViewModel<List<RegistrationOfUseListView_Model>>.Create(Status.SUCCESS, Message.SUCCESS, dataList, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError("检测列表：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<RegistrationOfUseListView_Model>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 机械办理使用登记信息信息
        /// </summary>
        /// <param name="machineryInfoId">机械信息</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<MachineryInfoDjViewModel>> GetMachineryInfos(string machineryInfoId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(machineryInfoId))
                {
                    return ResponseViewModel<MachineryInfoDjViewModel>.Create(Status.FAIL, "参数错误");
                }
                var query = await _context.InstallationNotificationRecords.Where(w => w.MachineryInfoId == machineryInfoId && w.Type == 0 && w.DeleteMark == 0)
                    .OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                var data = await _context.MachineryInfos.Where(x => x.MachineryInfoId == machineryInfoId).FirstOrDefaultAsync();
                MachineryInfoDjViewModel model = new MachineryInfoDjViewModel();
                model.EntCode = data.EntCode;
                model.EntName = data.EntName;//使用单位名称
                if (query != null)
                {
                    model.InstallationPosition = query.InstallationPosition ?? "";
                    model.EntCode = query.EntCode;
                    model.EntName = query.EntName;//使用单位名称
                }

                model.MachineryInfoId = data.MachineryInfoId;

                model.PropertyRightsRecordNo = data.PropertyRightsRecordNo;
                model.MachineryType = data.MachineryType.GetHashCode();
                model.MachineryTypeName = ((MachineryTypeEnum)data.MachineryType).ToString();
                model.MachineryName = data.MachineryName;
                model.EntGUID = data.EntGUID;
                model.EntCodeMaintain = data.EntCodeMaintain;
                model.EntNameMaintain = data.EntNameMaintain;
                model.CheckRecordId = data.CheckRecordId;
                model.CheckDate = data.CheckReviewDate;
                model.RegistrationOfUseId = data.RegistrationOfUseId;
                model.InstallationAcceptanceDate = data.InstallationAcceptanceDate;
                model.InstallationSelfInspectionDate = data.InstallationSelfInspectionDate;
                if (!string.IsNullOrWhiteSpace(model.EntGUID))
                {
                    var chanquanUnit = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == model.EntGUID).OrderByDescending(x => x.Id).FirstOrDefault();
                    if (chanquanUnit != null)
                    {
                        model.ChanQuanUnitCode = chanquanUnit.EntCode;
                        model.ChanQuanUnitName = chanquanUnit.EntName;
                    }

                }


                if (!string.IsNullOrWhiteSpace(data.CheckRecordId))
                {
                    //设备检测唯一编号CheckRecordId
                    var checkRecords = _context.CheckRecords.Where(x => x.CheckRecordId == data.CheckRecordId).FirstOrDefault();
                    if (checkRecords != null)
                    {
                        var testingInstituteInfo = _context.TestingInstituteInfo.Where(x => x.TestingInstituteInfoId == checkRecords.TestingInstituteInfoId).FirstOrDefault();
                        if (testingInstituteInfo != null)
                        {
                            model.MechanismName = testingInstituteInfo.MechanismName;
                            model.TestingInstituteId = testingInstituteInfo.TestingInstituteInfoId;
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(data.RegistrationOfUseId))
                {
                    //使用登记
                    var registrationOfUses = _context.RegistrationOfUses.Where(x => x.RegistrationOfUseId == data.RegistrationOfUseId).FirstOrDefault();
                    model.LeadingCadre = registrationOfUses.LeadingCadre;
                    model.LeadingCadreTel = registrationOfUses.LeadingCadreTel;

                    var peopleType = await _context.PersonTypes.Where(x => x.DeleteMark == 0).ToListAsync();

                    var userList = (from a in _context.InstallPeoples.Where(x => x.InstallationNotificationRecordId == data.RegistrationOfUseId && x.Type == 2 && x.IsFree == 0 && x.DeleteMark == 0)
                                    join b in _context.MachineryPeoples
                                    on a.MachineryPersonId equals b.MachineryPersonId
                                    select new InstallPeopleViewModel
                                    {
                                        MachineryPersonId = a.MachineryPersonId,
                                        SpecialWorkerTypeNo = b.SpecialWorkerTypeNo,
                                        PersonType = b.PersonType.ToString(),
                                        PersonName = b.PersonName,
                                        Sex = b.Sex == 0 ? "男" : "女",
                                        WorkTypeCodeValue = b.WorkTypeCode,
                                        IdCard = b.IdCard,
                                        Tel = b.Tel,
                                        RecordNumber = b.Tel,
                                        BelongedTo = b.RecordNumber,
                                        Remark = a.Remark,
                                        CerUrl = b.CerUrl
                                    }).ToList();

                    userList.ForEach(x =>
                    {
                        if (x.WorkTypeCodeValue != null && Worker.WorkerDic.ContainsKey((int)x.WorkTypeCodeValue))
                        {
                            x.WorkTypeCode = Worker.WorkerDic[(int)x.WorkTypeCodeValue].Value;
                        }
                        else
                        {
                            x.WorkTypeCode = "其他";
                        }
                        x.PersonType = peopleType.Where(s => s.Code == Convert.ToInt32(x.PersonType)).OrderByDescending(k => k.Id).Select(k => k.Name).FirstOrDefault();
                    });
                    model.UserList = userList;
                }

                #region 附件列表
                var fileHeGe = await _context.RecordConfigs.Where(x => x.Type == RecordConfigTypeEnum.产权备案
                    && x.DeleteMark == 0 && (x.AttachmentName == "制造许可证" || x.AttachmentName == "产品合格证" || x.AttachmentName == "监督检验证明"))
                    .OrderBy(x => x.Sort).ToListAsync();

                var FileTypeList = await _context.RecordConfigs.Where(x => x.Type == RecordConfigTypeEnum.办理使用登记的附件配置
                     && x.DeleteMark == 0).OrderBy(x => x.Sort).ToListAsync();
                List<FileUploadUrlViem> fileUrlList = new List<FileUploadUrlViem>();
                List<RecordAttachment> fileUrlAllList = new List<RecordAttachment>();
                fileUrlAllList = _context.RecordAttachments.Where(y => (y.AttachmentId == machineryInfoId || y.AttachmentId == data.RegistrationOfUseId) && y.DeleteMark == 0).OrderByDescending(y => y.CreateDate).ToList();
                List<FileInfoDengJiViewModel> fileList = new List<FileInfoDengJiViewModel>();

                fileHeGe.ForEach(x =>
                {
                    FileInfoDengJiViewModel file = new FileInfoDengJiViewModel();
                    file.ReaderOnly = true;
                    file.AttachmentName = x.AttachmentName;
                    file.Required = x.Required;
                    file.RecordConfigId = x.RecordConfigId;
                    if (fileUrlAllList.Count > 0)
                    {
                        file.FileUrl = fileUrlAllList.Where(y => y.AttachmentId == machineryInfoId && y.RecordConfigId == x.RecordConfigId && y.Type == RecordConfigTypeEnum.产权备案).Select(y => new FileUploadUrlViem { FileType = file.AttachmentName, RecordConfigId = y.RecordConfigId, FileName = y.FileName, FileUrl = y.FileUrl, Suffix = Path.GetExtension(y.FileName).Replace(".", "") }).ToList();
                    }
                    else
                    {
                        file.FileUrl = fileUrlList;
                    }
                    fileList.Add(file);
                });

                FileTypeList.ForEach(x =>
                {
                    FileInfoDengJiViewModel file = new FileInfoDengJiViewModel();
                    file.ReaderOnly = false;
                    file.AttachmentName = x.AttachmentName;
                    file.Required = x.Required;
                    file.RecordConfigId = x.RecordConfigId;
                    if (fileUrlAllList.Count > 0)
                    {
                        file.FileUrl = fileUrlAllList.Where(y => y.AttachmentId == data.RegistrationOfUseId && y.RecordConfigId == x.RecordConfigId && y.Type == RecordConfigTypeEnum.办理使用登记的附件配置).Select(y => new FileUploadUrlViem { FileType = file.AttachmentName, RecordConfigId = y.RecordConfigId, FileName = y.FileName, FileUrl = y.FileUrl, Suffix = Path.GetExtension(y.FileName).Replace(".", "") }).ToList();
                    }
                    else
                    {
                        file.FileUrl = fileUrlList;
                    }
                    fileList.Add(file);
                });
                model.FileInfoList = fileList;
                #endregion
                return ResponseViewModel<MachineryInfoDjViewModel>.Create(Status.SUCCESS, Message.SUCCESS, model);
            }
            catch (Exception ex)
            {
                _logger.LogError("机械办理使用登记信息：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<MachineryInfoDjViewModel>.Create(Status.ERROR, Message.ERROR + ex.Message + ex.StackTrace);
            }
        }


        /// <summary>
        /// 获取使用人员信息
        /// </summary>
        /// <param name="peopleName">人员名字</param>`
        /// <param name="type">1.0：企业人员 2.项目人员</param>
        /// <param name="entCode">企业id</param>
        /// <param name="recordNumber">项目的备案号</param>
        /// <param name="belongedTo">项目所属的机构编号</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<InstallPeopleViewModel>>> GetInstallPeoples(
            string peopleName, string recordNumber, string belongedTo, string machineryInfoId)
        {
            try
            {
                var dataMachineryInfos = await _context.MachineryInfos.Where(x => x.MachineryInfoId == machineryInfoId).FirstOrDefaultAsync();
                if (string.IsNullOrWhiteSpace(dataMachineryInfos.EntCode))
                {
                    return ResponseViewModel<List<InstallPeopleViewModel>>.Create(Status.FAIL, "机械所在施工单位信息有误");
                }
                //机械人员管理
                var data = _context.MachineryPeoples.Where(w => w.BelongedTo == belongedTo && w.RecordNumber == recordNumber && w.DeleteMark == 0);
                if (!string.IsNullOrWhiteSpace(peopleName))
                {
                    data = data.Where(w => w.PersonName.Contains(peopleName));
                }
                var benxiangmuData = data.ToList();
                ////以前的脏数据
                //var zangshuju = await _context.MachineryPeoples.Where(w =>
                // (benxiangmuData.Select(x => x.IdCard.Trim()).Contains(w.IdCard.Trim())
                // || benxiangmuData.Select(x => x.SpecialWorkerTypeNo.Trim()).Contains(w.SpecialWorkerTypeNo.Trim()))
                // && w.BelongedTo != belongedTo && w.RecordNumber != recordNumber && w.DeleteMark == 0).ToListAsync();

                List<InstallPeopleViewModel> list = new List<InstallPeopleViewModel>();
                var installPeoples = await _context.InstallPeoples.Where(x => x.DeleteMark == 0 && x.BelongedTo == belongedTo && x.RecordNumber == recordNumber
                && x.MachineryInfoId != machineryInfoId && x.IsFree == 0).ToListAsync();

                var peopleType = await _context.PersonTypes.Where(x => x.DeleteMark == 0).ToListAsync();
                benxiangmuData.ForEach(x =>
                {
                    //判断此人有没有被其他机械的锁定记录
                    var isExists = installPeoples.Where(y => y.MachineryPersonId == x.MachineryPersonId).ToList();

                    //var otherXiangmuRenYuan = zangshuju.Where(y => y.SpecialWorkerTypeNo.Trim() == x.SpecialWorkerTypeNo.Trim() || y.IdCard.Trim() == x.IdCard.Trim())
                    //.Select(y => y.MachineryPersonId).ToList();

                    if (isExists == null || isExists.Count == 0)
                    {
                        //if (otherXiangmuRenYuan != null || otherXiangmuRenYuan.Count > 0)
                        //{
                        //    //判断这个人有没有被其他项目上使用
                        //    var isExistsOtherProject = _context.InstallPeoples.Where(y => y.DeleteMark == 0 && y.MachineryInfoId != machineryInfoId && y.IsFree == 0 && otherXiangmuRenYuan.Contains(y.MachineryPersonId)).ToList();
                        //    //如果这个人也被其他项目添加了并且这个人在其他项目上没有被使用才可以添加
                        //    if (isExistsOtherProject == null || isExistsOtherProject.Count == 0)
                        //    {
                        //        InstallPeopleViewModel mode = new InstallPeopleViewModel();
                        //        mode.MachineryPersonId = x.MachineryPersonId;
                        //        mode.CerUrl = x.CerUrl;
                        //        mode.SpecialWorkerTypeNo = x.SpecialWorkerTypeNo;
                        //        mode.PersonType = peopleType.Where(s => s.Code == x.PersonType).OrderByDescending(k => k.Id).Select(k => k.Name).FirstOrDefault();
                        //        mode.PersonName = x.PersonName;
                        //        mode.Sex = x.Sex == 0 ? "男" : "女";
                        //        if (x.WorkTypeCode != null && Worker.WorkerDic.ContainsKey((int)x.WorkTypeCode))
                        //        {
                        //            mode.WorkTypeCode = Worker.WorkerDic[(int)x.WorkTypeCode].Value;
                        //        }
                        //        else
                        //        {
                        //            mode.WorkTypeCode = "其他";
                        //        }
                        //        mode.IdCard = x.IdCard;
                        //        mode.Tel = x.Tel;
                        //        mode.CerValidEndDate = x.CerValidEndDate;
                        //        mode.RecordNumber = x.RecordNumber;
                        //        mode.BelongedTo = x.BelongedTo;
                        //        list.Add(mode);
                        //    }
                        //}
                        //else
                        //{
                        InstallPeopleViewModel mode = new InstallPeopleViewModel();
                        mode.MachineryPersonId = x.MachineryPersonId;
                        mode.CerUrl = x.CerUrl;
                        mode.SpecialWorkerTypeNo = x.SpecialWorkerTypeNo;
                        mode.PersonType = peopleType.Where(s => s.Code == x.PersonType).OrderByDescending(k => k.Id).Select(k => k.Name).FirstOrDefault();
                        mode.PersonName = x.PersonName;
                        mode.Sex = x.Sex == 0 ? "男" : "女";
                        if (x.WorkTypeCode != null && Worker.WorkerDic.ContainsKey((int)x.WorkTypeCode))
                        {
                            mode.WorkTypeCode = Worker.WorkerDic[(int)x.WorkTypeCode].Value;
                        }
                        else
                        {
                            mode.WorkTypeCode = "其他";
                        }
                        mode.IdCard = x.IdCard;
                        mode.Tel = x.Tel;
                        mode.CerValidEndDate = x.CerValidEndDate;
                        mode.RecordNumber = x.RecordNumber;
                        mode.BelongedTo = x.BelongedTo;
                        list.Add(mode);
                        //}

                    }

                });
                return ResponseViewModel<List<InstallPeopleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, list);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取项目失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<InstallPeopleViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 办理使用登记列表
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="belongedTo"></param>
        /// <param name="recordNumber"></param>
        /// <param name="machineryType"></param>
        /// <param name="machineryName"></param>
        /// <param name="checkState">99超期未检测</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<RegistrationOfUseListView_Model>>> GetRegistrationOfUseList(int pageIndex, int pageSize, string belongedTo, string recordNumber,
            int machineryType, string machineryName, int useDengJiStateValue)
        {
            try
            {
                if (pageIndex <= 0 || pageSize <= 0 || string.IsNullOrWhiteSpace(belongedTo) || string.IsNullOrWhiteSpace(recordNumber))
                {
                    return ResponseViewModel<List<RegistrationOfUseListView_Model>>.Create(Status.FAIL, "参数错误");
                }

                var installs = await _context.InstallationNotificationRecords
                    .Where(w => w.DeleteMark == 0 && w.Type == 0 && w.State == 2 && w.BelongedTo == belongedTo && w.RecordNumber == recordNumber)
                    .Select(k => new { k.InstallationPosition, k.MachineryInfoId, k.Id })
                    .ToListAsync();

                var data = (from a in _context.MachineryInfos.Where(x => x.BelongedTo == belongedTo
                                        && x.RecordNumber == recordNumber && x.DeleteMark == 0
                                        && x.State == 2)
                            where (a.CheckState == MachineryState.安装告知审核通过
                             || a.CheckState == MachineryState.办理使用登记审核中
                            || a.CheckState == MachineryState.办理使用登记未通过
                            || a.CheckState == MachineryState.办理使用登记通过)
                            && (a.MachineryCheckState == MachineryCheckStateEnum.复检合格
                            || a.MachineryCheckState == MachineryCheckStateEnum.检测合格)
                            select new RegistrationOfUseListView_Model
                            {
                                UpdateDate = a.UpdateDate,
                                RegistrationOfUseId = a.RegistrationOfUseId,
                                MachineryInfoId = a.MachineryInfoId,
                                MachineryType = a.MachineryType.GetHashCode(),
                                MachineryTypeName = ((MachineryTypeEnum)a.MachineryType).ToString(),
                                MachineryName = a.MachineryName,
                                MachineryModel = a.MachineryModel,
                                //AnChaiEntGUID = b.EntGUID,//安拆单位id                             
                                PropertyRightsRecordNo = a.PropertyRightsRecordNo,//产权备案编号
                                ChanQuanEntGUID = a.EntGUID,//所属产权单位id 如果是施工单位进行产权备案那么则是施工单位的id   
                                CheckStateValue = a.CheckState.GetHashCode(),
                                CheckState = ((MachineryState)a.CheckState).ToString(),
                                MachineryCheckStateValue = a.MachineryCheckState.GetHashCode(),
                                MachineryCheckState = ((MachineryCheckStateEnum)a.MachineryCheckState).ToString(),
                                InstallReviewDate = a.InstallReviewDate,
                                UseDengJiStateValue = a.CheckState == MachineryState.安装告知审核通过 ? a.MachineryCheckState.GetHashCode() : a.CheckState.GetHashCode(),
                                UseDengJiStateText = a.CheckState == MachineryState.安装告知审核通过 ? ((MachineryCheckStateEnum)a.MachineryCheckState).ToString() : ((MachineryState)a.CheckState).ToString(),
                                //PlanInstallDate = b.PlanInstallDate,//拟安装日期
                                //PlanUseBeginDate = b.PlanUseBeginDate,//拟使用日期
                                BuyDate = a.BuyDate,
                                MaxRatedLiftingCapacity = a.MaxRatedLiftingCapacity,
                                Knm = a.Knm,
                                MaxInstallHeight = a.MaxInstallHeight,
                                MaxRange = a.MaxRange,
                                UseReason = a.UseReason == null ? a.UseReasonJldw : a.UseReason,
                                EntCode = a.EntCode,
                                EntName = a.EntName,
                                TestingInstituteInfoId = a.TestingInstituteInfoId,
                                AnChaiEntName = "",
                                ChanQuanEntName = "",
                                CheckRecordId = a.CheckRecordId,
                                RecordUrl = a.CheckUrl,
                                UseSubmitDate = a.UseSubmitDate,//使用登记提交日期
                                UseReviewDate = a.UseReviewDate,//使用登记审核日期
                                UseRecordNo = a.UseRecordNo,
                                RegistrationOfUseApplicationUrl = a.RegistrationOfUseApplicationUrl
                            }).Distinct().ToList();

                if (machineryType >= 0)
                {
                    data = data.Where(x => x.MachineryType == machineryType).ToList();
                }
                if (!string.IsNullOrWhiteSpace(machineryName))
                {
                    data = data.Where(x => x.MachineryName.Contains(machineryName)).ToList();
                }
                if (useDengJiStateValue >= 0)
                {
                    data = data.Where(x => x.UseDengJiStateValue == useDengJiStateValue).ToList();

                }
                int totalCount = data.Count;
                //使用登记提交日期排序
                var dataList = data.OrderByDescending(r => r.UseSubmitDate)
                    .ThenByDescending(r => r.UpdateDate).Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize).ToList();

                dataList.ForEach(w =>
                {

                    var anzhuanggaozhi = _context.InstallationNotificationRecords.Where(x => x.DeleteMark == 0 && x.State == 2
                    && x.MachineryInfoId == w.MachineryInfoId).OrderByDescending(x => x.Id).FirstOrDefault();
                    if (anzhuanggaozhi != null)
                    {
                        w.PlanInstallDate = anzhuanggaozhi.PlanInstallDate;
                        w.PlanUseBeginDate = anzhuanggaozhi.PlanUseBeginDate;//拟使用日期
                        w.AnChaiEntGUID = anzhuanggaozhi.EntGUID;//安拆单位id   
                    }

                    w.EntCode = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == w.EntCode)
                                                         .Select(x => x.EntCode).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(w.AnChaiEntGUID))
                    {
                        w.AnChaiEntName = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == w.AnChaiEntGUID)
                                                        .Select(x => x.EntName).FirstOrDefault();//安拆单位的名字
                    }
                    if (!string.IsNullOrWhiteSpace(w.ChanQuanEntGUID))
                    {
                        w.ChanQuanEntName = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == w.ChanQuanEntGUID)
                                   .Select(x => x.EntName).FirstOrDefault();
                    }
                    w.InstallationPosition = installs.Where(k => k.MachineryInfoId == w.MachineryInfoId)
                      .OrderByDescending(o => o.Id).Select(k => k.InstallationPosition).FirstOrDefault();
                });
                return ResponseViewModel<List<RegistrationOfUseListView_Model>>.Create(Status.SUCCESS, Message.SUCCESS, dataList, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError("办理使用登记列表：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<RegistrationOfUseListView_Model>>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// 下载使用登记申请书
        /// </summary>
        /// <param name="machineryInfoId">大型机械id</param>
        /// <param name="registrationOfUseId"> 使用登记记录表id</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> DownloadRegistrationOfUseApplication(string machineryInfoId, string registrationOfUseId)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(machineryInfoId)
                    || string.IsNullOrWhiteSpace(registrationOfUseId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
                }
                string mechanismName = "";

                var machineryInfos = await _context.MachineryInfos.Where(x => x.MachineryInfoId == machineryInfoId && x.RegistrationOfUseId == registrationOfUseId && x.DeleteMark == 0).FirstOrDefaultAsync();
                if (machineryInfos == null)
                    return ResponseViewModel<string>.Create(Status.FAIL, "机械信息不存在");

                var installationNotificationRecords = await _context.InstallationNotificationRecords.Where(x => x.MachineryInfoId == machineryInfos.MachineryInfoId
                && x.State == 2 && x.DeleteMark == 0).OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync();
                if (installationNotificationRecords == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "安装数据没有");
                }
                if (machineryInfos.CheckRecordId == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "未找到检测报告，请重新委托检测！");
                }
                var checkRecords = _context.CheckRecords.Where(x => x.CheckRecordId == machineryInfos.CheckRecordId).FirstOrDefault();
                if (checkRecords != null)
                {
                    var testingInstituteInfo = _context.TestingInstituteInfo.Where(x => x.TestingInstituteInfoId == checkRecords.TestingInstituteInfoId).FirstOrDefault();
                    if (testingInstituteInfo != null)
                    {
                        mechanismName = testingInstituteInfo.MechanismName;
                    }
                }


                var weizhi = await _context.InstallationNotificationRecords
                    .Where(s => s.DeleteMark == 0 && s.Type == 0 && s.MachineryInfoId == machineryInfoId)
                    .FirstOrDefaultAsync();
                string chanQuanEntName = "";
                if (!string.IsNullOrWhiteSpace(machineryInfos.EntGUID))
                {
                    chanQuanEntName = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == machineryInfos.EntGUID)
                               .Select(x => x.EntName).FirstOrDefault();
                }

                var anchaiunit = await _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == installationNotificationRecords.EntGUID).FirstOrDefaultAsync();

                //使用登记记录x
                var dengji = await _context.RegistrationOfUses.Where(x => x.RegistrationOfUseId == registrationOfUseId).FirstOrDefaultAsync();
                if (dengji == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "请保存使用登记信息");
                }

                var project = await _context.ProjectOverview.Where(x => x.BelongedTo == machineryInfos.BelongedTo
                && x.RecordNumber == machineryInfos.RecordNumber).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                if (project == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "项目信息不存在", "项目信息不存在");
                }
                var sgName = await _context.ProjectPersonSnapshot.Where(w => w.BelongedTo == machineryInfos.BelongedTo &&
               w.RecordNumber == machineryInfos.RecordNumber && w.EnterpriseType == "施工单位" && w.PersonType == "项目经理")
                   .AsNoTracking().FirstOrDefaultAsync();

                //生成施工单位的复工申请书
                string webRootPath = _wordTemplte + "RegistrationOfUseApplication.doc";

                Aspose.Words.Document doc = new Aspose.Words.Document(webRootPath);
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("ProjectAddress", project.ProjectAddress);
                dic.Add("ProjectName", project.ProjectName);
                dic.Add("RecordNumber", project.RecordNumber);
                dic.Add("SGEntName", machineryInfos.EntName);
                dic.Add("SGuserName", sgName == null ? "" : sgName.PersonName);//施工单位项目经理
                dic.Add("SGuserPhone", sgName == null ? "" : sgName.PersonPhone);//施工单位项目经理

                dic.Add("MachineryName", machineryInfos.MachineryName);
                dic.Add("MachineryModel", machineryInfos.MachineryModel);
                //dic.Add("PropertyRightsRecordNo", machineryInfos.PropertyRightsRecordNo);
                dic.Add("InstallationPosition", installationNotificationRecords == null ? "" : installationNotificationRecords.InstallationPosition);

                dic.Add("ManufacturingLicense", machineryInfos.ManufacturingLicense);
                dic.Add("LeaveTheFactoryNo", machineryInfos.LeaveTheFactoryNo);
                dic.Add("AnChaiEntName", anchaiunit.EntName);//安装单位
                dic.Add("IntelligenceLevel", anchaiunit.IntelligenceLevel.ToString());//安装单位
                dic.Add("PlanInstallDate", installationNotificationRecords.PlanInstallDate == null ? "" : ((DateTime)installationNotificationRecords.PlanInstallDate).ToString("yyyy-MM-dd"));//拟安装日期
                dic.Add("PlanInstallDateEnd", installationNotificationRecords.PlanInstallEndDate == null ? "" : ((DateTime)installationNotificationRecords.PlanInstallEndDate).ToString("yyyy-MM-dd"));//拟安装日期
                dic.Add("InstallationSelfInspectionDate", machineryInfos.InstallationSelfInspectionDate == null ? "" : ((DateTime)machineryInfos.InstallationSelfInspectionDate).ToString("yyyy-MM-dd"));//安装验收日期
                dic.Add("InstallationAcceptanceDate", machineryInfos.InstallationAcceptanceDate == null ? "" : ((DateTime)machineryInfos.InstallationAcceptanceDate).ToString("yyyy-MM-dd"));//安装验收日期
                dic.Add("ChanQuanEntName", chanQuanEntName);//产权单位名称
                dic.Add("PropertyRightsRecordNo", machineryInfos.PropertyRightsRecordNo);//产权单位名称

                dic.Add("MechanismName", mechanismName);
                dic.Add("CheckDate", ((DateTime)machineryInfos.CheckReviewDate).ToString("yyyy-MM-dd"));//检测日期
                var path = new DataBll(_context).BuildPdfToServer(_environment, doc, dic, "CanBeDeleted", machineryInfos.MachineryInfoId + "_ShenQing.pdf", Request);
                //var path = new DataBll(_context).BuildPdf(_ossFileSetting, doc, dic, "RegistrationOfUse", machineryInfos.MachineryInfoId + "_ShenQing.pdf");
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, path);
            }
            catch (Exception ex)
            {
                _logger.LogError("下载使用登记申请书RegistrationOfUseController/DownloadRegistrationOfUseApplication：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// css-保存使用登记信息
        /// </summary>
        [HttpPost]
        public async Task<ResponseViewModel<string>> UpdateRegistrationOfUse([FromBody] MachineryInfoDjUpdateViewModel entity)
        {

            try
            {
                if (entity == null

                    || string.IsNullOrWhiteSpace(entity.MachineryInfoId)
                    || string.IsNullOrWhiteSpace(entity.LeadingCadre)
                    || string.IsNullOrWhiteSpace(entity.LeadingCadreTel)
                    || string.IsNullOrWhiteSpace(entity.EntCodeMaintain)
                    || string.IsNullOrWhiteSpace(entity.EntNameMaintain)
                    || entity.PeopleLists == null || entity.PeopleLists.Count == 0
                    || entity.FileUrls == null || entity.FileUrls.Count == 0
                    || entity.InstallationSelfInspectionDate == null || entity.InstallationAcceptanceDate == null
                    )
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
                }
                //bool isWeiBaoHeTong = false;//维保合同是否必传

                string registrationOfUseId = "";
                var machineryInfos = await _context.MachineryInfos.Where(x => x.MachineryInfoId == entity.MachineryInfoId && x.DeleteMark == 0).FirstOrDefaultAsync();
                if (machineryInfos == null)
                    return ResponseViewModel<string>.Create(Status.FAIL, "机械信息不存在");
                if (machineryInfos.MachineryCheckState != MachineryCheckStateEnum.复检合格
                    && machineryInfos.MachineryCheckState != MachineryCheckStateEnum.检测合格
                    && machineryInfos.CheckState != MachineryState.办理使用登记未通过)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "当前状态不可办理使用登记");
                }
                DateTime nowTime = DateTime.Now;
                List<InstallPeople> userNewList = new List<InstallPeople>();
                List<RecordAttachment> fileNewList = new List<RecordAttachment>();
                //if (!string.IsNullOrWhiteSpace(machineryInfos.EntCode))
                //{
                //    EntRegisterInfoMag useUnitEnt = _context.EntRegisterInfoMag.Where(h => h.EntRegisterInfoMagId == machineryInfos.EntCode).FirstOrDefault();

                //    if (useUnitEnt.EntCode != entity.EntCodeMaintain)
                //    {
                //        isWeiBaoHeTong = true;//维保合同必传
                //    }

                //}

                //if (isWeiBaoHeTong)
                //{
                //    List<FileInfoDengJiViewModel> fileList = entity.FileUrls.Where(h => h.AttachmentName == "维保合同").ToList();
                //    if (fileList.Count==0 || fileList[0].FileUrl == null || fileList[0].FileUrl.Count == 0)
                //    {
                //        return ResponseViewModel<string>.Create(Status.FAIL, "维保单位非使用单位必须上传维保合同");
                //    }
                //}
                //有使用登记记录id并且状态是检测通过=说明是撤回来的数据
                if (!string.IsNullOrWhiteSpace(entity.RegistrationOfUseId))
                {
                    registrationOfUseId = entity.RegistrationOfUseId;
                    //传了使用登记id 修改
                    var dengji = await _context.RegistrationOfUses.Where(x => x.RegistrationOfUseId == entity.RegistrationOfUseId).FirstOrDefaultAsync();
                    dengji.LeadingCadre = entity.LeadingCadre;
                    dengji.LeadingCadreTel = entity.LeadingCadreTel;
                    dengji.InstallationSelfInspectionDate = entity.InstallationSelfInspectionDate;
                    dengji.InstallationAcceptanceDate = entity.InstallationAcceptanceDate;
                    dengji.CheckState = machineryInfos.CheckState;
                    dengji.UseReviewBelongedTo = machineryInfos.BelongedTo;
                    _context.RegistrationOfUses.Update(dengji);//修改负责人及电话
                }
                else
                {
                    registrationOfUseId = SecurityManage.GuidUpper();
                    RegistrationOfUse dengji = new RegistrationOfUse();
                    dengji.RegistrationOfUseId = registrationOfUseId;
                    dengji.MachineryInfoId = entity.MachineryInfoId;
                    dengji.CheckState = machineryInfos.CheckState;
                    dengji.LeadingCadre = entity.LeadingCadre;
                    dengji.LeadingCadreTel = entity.LeadingCadreTel;
                    dengji.InstallationSelfInspectionDate = entity.InstallationSelfInspectionDate;
                    dengji.InstallationAcceptanceDate = entity.InstallationAcceptanceDate;
                    dengji.UpdateDate = nowTime;
                    dengji.CreateDate = nowTime;
                    dengji.DeleteMark = 0;
                    dengji.UseReviewBelongedTo = machineryInfos.BelongedTo;
                    await _context.RegistrationOfUses.AddAsync(dengji);//修改负责人及电话

                }
                //更新大型机械表最新的使用登记记录id
                machineryInfos.RegistrationOfUseId = registrationOfUseId;
                machineryInfos.EntCodeMaintain = entity.EntCodeMaintain;
                machineryInfos.EntNameMaintain = entity.EntNameMaintain;
                machineryInfos.InstallationSelfInspectionDate = entity.InstallationSelfInspectionDate;
                machineryInfos.InstallationAcceptanceDate = entity.InstallationAcceptanceDate;
                _context.MachineryInfos.Update(machineryInfos);
                //删除旧数据
                var jixie = await _context.MachineryInfos
                    .Where(s => s.MachineryInfoId == entity.MachineryInfoId && s.DeleteMark == 0)
                    .FirstOrDefaultAsync();
                var people = _context.InstallPeoples.Where(s =>
                        s.InstallationNotificationRecordId == jixie.RegistrationOfUseId && s.Type == 2 && s.IsFree == 0)
                    .ToList();
                _context.InstallPeoples.RemoveRange(people);
                //大型机械人员库信息
                var peoples = await _context.MachineryPeoples
                    .Where(j => entity.PeopleLists.Select(u => u.MachineryPersonId).Contains(j.MachineryPersonId)).ToListAsync();
                #region 新增人员或者附件
                //新增
                //新增全部的特种作业人员列表
                foreach (InstallPeopleViewModel item in entity.PeopleLists)
                {

                    string installPeoplenewId = SecurityManage.GuidUpper();
                    InstallPeople isExistsUser = userNewList.Find(x => x.InstallPeopleId == installPeoplenewId);
                    if (isExistsUser == null)
                    {

                        InstallPeople user = new InstallPeople();
                        user.MachineryInfoId = entity.MachineryInfoId;
                        user.IsFree = 0;
                        user.InstallPeopleId = installPeoplenewId;
                        user.MachineryPersonId = item.MachineryPersonId;
                        user.InstallationNotificationRecordId = registrationOfUseId;
                        user.Type = 2;
                        user.WorkType = 0;
                        if (peoples.Count > 0)
                        {
                            var gongzhong = peoples.Where(j => j.MachineryPersonId == item.MachineryPersonId).Select(j => j.WorkTypeCode).FirstOrDefault();
                            if (gongzhong != null)
                            {
                                user.WorkType = (int)gongzhong;
                            }
                        }
                        user.DeleteMark = 0;
                        user.CreateDate = nowTime;
                        user.UpdateDate = nowTime;
                        user.Remark = item.Remark;
                        user.BelongedTo = machineryInfos.BelongedTo;
                        user.RecordNumber = machineryInfos.RecordNumber;
                        userNewList.Add(user);
                    }
                }
                await _context.InstallPeoples.AddRangeAsync(userNewList);
                //删除旧图片
                var file = _context.RecordAttachments
                    .Where(s => s.AttachmentId == jixie.RegistrationOfUseId && s.DeleteMark == 0).ToList();
                _context.RecordAttachments.RemoveRange(file);

                foreach (FileInfoDengJiViewModel files in entity.FileUrls)
                {
                    foreach (FileUploadUrlViem item in files.FileUrl)
                    {
                        string fileItemId = SecurityManage.GuidUpper();
                        RecordAttachment isExistsFile = fileNewList.Find(x => x.RecordAttachmentId == fileItemId);
                        if (isExistsFile == null)
                        {
                            RecordAttachment img = new RecordAttachment();
                            img.RecordAttachmentId = fileItemId;
                            img.AttachmentId = registrationOfUseId;
                            img.FileName = item.FileName;
                            img.FileUrl = item.FileUrl;
                            img.RecordConfigId = files.RecordConfigId;
                            img.Type = RecordConfigTypeEnum.办理使用登记的附件配置;
                            img.DeleteMark = 0;
                            img.CreateDate = nowTime;
                            img.UpdateDate = nowTime;
                            fileNewList.Add(img);
                        }

                    }
                }
                await _context.RecordAttachments.AddRangeAsync(fileNewList);
                #endregion

                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, registrationOfUseId);
            }
            catch (Exception)
            {

                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "保存失败");
            }
        }


        /// <summary>
        /// 提交/撤回使用登记信息
        /// </summary>
        /// <param name="machineryInfoId">大型机械id</param>
        /// <param name="registrationOfUseId"> 使用登记记录表id</param>
        /// <param name="isSubmit">1提交 2撤回</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> SubmitRegistrationOfUse(string machineryInfoId, string registrationOfUseId
            , int isSubmit, string registrationOfUseApplicationUrl = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(machineryInfoId)
                    || string.IsNullOrWhiteSpace(registrationOfUseId)
                    || (isSubmit != 1 && isSubmit != 2))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
                }

                var machineryInfos = await _context.MachineryInfos.Where(x => x.MachineryInfoId == machineryInfoId && x.RegistrationOfUseId == registrationOfUseId && x.DeleteMark == 0).FirstOrDefaultAsync();

                var xiangmubind = _context.ProvinceWorkEditBindProject.Where(s => s.BelongedTo == machineryInfos.BelongedTo
                                   && s.RecordNumber == machineryInfos.RecordNumber).OrderByDescending(s => s.UpdateDate).FirstOrDefault();

                if (machineryInfos == null)
                    return ResponseViewModel<string>.Create(Status.FAIL, "机械信息不存在");
                //使用登记记录
                var dengji = await _context.RegistrationOfUses.Where(x => x.RegistrationOfUseId == registrationOfUseId).FirstOrDefaultAsync();

                if (isSubmit == 1)
                {
                    if (string.IsNullOrWhiteSpace(registrationOfUseApplicationUrl))
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "请上传使用登记备案表");
                    }
                    //提交
                    if (machineryInfos.MachineryCheckState != MachineryCheckStateEnum.检测合格
                        && machineryInfos.MachineryCheckState != MachineryCheckStateEnum.复检合格
                        && machineryInfos.CheckState != MachineryState.办理使用登记未通过)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "当前状态不可办理使用登记");
                    }
                    machineryInfos.CheckState = MachineryState.办理使用登记审核中;
                    machineryInfos.UseSubmitDate = DateTime.Now;
                    machineryInfos.UseAuditStatusJldw = 1;//等待监理单位审核
                    machineryInfos.RegistrationOfUseApplicationUrl = registrationOfUseApplicationUrl;
                    machineryInfos.UseReviewBelongedTo = machineryInfos.BelongedTo;
                    if (xiangmubind != null)
                    {
                        machineryInfos.SPSXSLBM = xiangmubind.SPSXSLBM;
                    }
                    dengji.CheckState = MachineryState.办理使用登记审核中;
                    dengji.RegistrationOfUseApplicationUrl = registrationOfUseApplicationUrl;

                    _context.MachineryInfos.Update(machineryInfos);
                    _context.RegistrationOfUses.Update(dengji);
                }
                else
                {
                    //撤回之后当前状态都是检测通过
                    if (machineryInfos.CheckState != MachineryState.办理使用登记审核中 || machineryInfos.UseAuditStatusJldw != 1)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "当前状态不可撤回");
                    }
                    machineryInfos.UseAuditStatusJldw = 0;
                    //machineryInfos.CheckState = MachineryState.检测合格;
                    machineryInfos.CheckState = MachineryState.安装告知审核通过;
                    machineryInfos.UseReviewBelongedTo = "";
                    dengji.CheckState = MachineryState.安装告知审核通过;
                    machineryInfos.UseSubmitDate = null;//提交日期设为null
                    machineryInfos.IsSendGongGai = 0;
                    if (xiangmubind != null)
                    {
                        machineryInfos.SPSXSLBM = xiangmubind.SPSXSLBM;
                    }
                    _context.MachineryInfos.Update(machineryInfos);
                    _context.RegistrationOfUses.Update(dengji);
                }


                var xiangmu = await _context.ProjectOverview.Where(w => w.BelongedTo == machineryInfos.BelongedTo && w.RecordNumber == machineryInfos.RecordNumber && !string.IsNullOrEmpty(w.ProjectCode)).FirstOrDefaultAsync();


                if (xiangmu != null && xiangmubind != null)
                {
                    var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                    var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                    var userName = User.FindFirst(ClaimsIdentity.DefaultNameClaimType)?.Value;//当前人名字

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
                            CityZone cueecity = _context.CityZone.Where(w => w.BelongedTo == machineryInfos.BelongedTo).FirstOrDefault();
                            ////配置文件地址
                            //string webRootPath = _environment.WebRootPath + Path.DirectorySeparatorChar + "json" + Path.DirectorySeparatorChar + "AppKeyConfig.json";
                            //GetGongGaiAppKeyList json = new GetGongGaiAppKeyList();
                            ////转化成实体对象
                            //json = TaskDataUtil.GetGongGaiAppKeyListJson(webRootPath);
                            ////从配置文件读取当前地区的appkey
                            //CityAppKeyList citykey = json.AppKeyList.Where(k => k.CityCode == cueecity.ParentCityCode).FirstOrDefault();

                            TokenParam headermodel = new TokenParam();
                            headermodel.access_token = result0.custom.access_token;
                            headermodel.time_stamp = result0.custom.timeStamp;
                            headermodel.app_key = _jsgginterface.appKey;
                            ProjectParam projectparam = new ProjectParam();
                            projectparam.ProjectCode = xiangmu.ProjectCode;
                            //调用成功后先获取省工改的项目信息
                            //token成功了
                            //调取项目信息
                            string param3 = "";
                            string postContentxm = DataBll.RequestUrl("POST", "application/x-www-form-urlencoded", _jsgginterface.UrlGetxmjbxx, headermodel, projectparam, out param3);
                            if (!string.IsNullOrEmpty(postContentxm))
                            {
                                GongGaiRoot result1 = JsonConvert.DeserializeObject<GongGaiRoot>(postContentxm);
                                if (result1 != null && result1.status.code == 1 && !string.IsNullOrEmpty(result1.custom.results.projectnum))
                                {

                                    ShenPiBanJianParam banjian = new ShenPiBanJianParam();
                                    banjian.RowGuid = Guid.NewGuid().ToString();
                                    banjian.ParentGuid = machineryInfos.RegistrationOfUseId;
                                    banjian.XZQHDM = result1.custom.results.xzqhdm;
                                    banjian.JSDDXZQH = result1.custom.results.jsddxzqh;
                                    banjian.ProjectName = result1.custom.results.xmmc;
                                    banjian.ProjectCode = result1.custom.results.xmdm;
                                    banjian.ProjectNum = result1.custom.results.projectnum;
                                    banjian.GCDM = xiangmu.GCDM;
                                    //事项编码默认010410000N
                                    banjian.SPSXSLBM = xiangmubind.SPSXSLBM;
                                    //办理处室=安监审核单位
                                    banjian.BLCS = cueecity.SuperOrganName;
                                    //办理人
                                    banjian.BLR = userName;
                                    if (isSubmit == 1)
                                    {
                                        //提交
                                        banjian.BLZT = 8;
                                        banjian.BLYJ = "项目端提交审核";
                                        banjian.Beizu = "项目端提交审核";
                                    }
                                    else
                                    {
                                        //撤回
                                        banjian.BLZT = 2;
                                        banjian.BLYJ = "项目端撤回";
                                        banjian.Beizu = "项目端撤回";
                                    }

                                    banjian.BLSJ = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                    banjian.SJYXBS = 1;
                                    string banjianparamstr = "";
                                    //调用推送审批办件信息
                                    string postContentspbj = DataBll.RequestUrl("POST", "application/x-www-form-urlencoded", _jsgginterface.UrlSafe, headermodel, banjian, out banjianparamstr);
                                    RequestRecordLog rizghi = new RequestRecordLog();
                                    rizghi.RequestURL = _jsgginterface.UrlSafe;
                                    rizghi.ReturnInformation = postContentspbj;
                                    rizghi.RequestParameters = banjianparamstr;
                                    rizghi.ForeignKey = "RegistrationOfUseId=" + machineryInfos.RegistrationOfUseId;
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
                                    rizghi.RequestParameters = param3;
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
                                rizghi.RequestParameters = param3;
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
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "提交成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("提交使用登记信息RegistrationOfUseController/SubmitRegistrationOfUse：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, ex.Message + ex.StackTrace);
            }
        }


        /// <summary>
        /// 安拆单位办理使用登记列表
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="belongedTo"></param>
        /// <param name="recordNumber"></param>
        /// <param name="machineryType"></param>
        /// <param name="machineryName"></param>
        /// <param name="checkState">99超期未检测</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<RegistrationOfUseListView_Model>>> GetEnterpriseRegistrationOfUseList(int pageIndex, int pageSize, string uuid,
            int machineryType, string machineryName, int useDengJiStateValue)
        {
            try
            {
                if (pageIndex <= 0 || pageSize <= 0 || string.IsNullOrWhiteSpace(uuid))
                {
                    return ResponseViewModel<List<RegistrationOfUseListView_Model>>.Create(Status.FAIL, "参数错误");
                }

                var installs = await _context.InstallationNotificationRecords
                    .Where(w => w.DeleteMark == 0 && w.Type == 0 && w.State == 2 && w.EntGUID == uuid)
                    .Select(k => new { k.InstallationPosition, k.MachineryInfoId, k.Id })
                    .ToListAsync();

                var data = (from a in _context.MachineryInfos.Where(x => x.EntGUID == uuid
                                         && x.DeleteMark == 0
                                        && x.State == 2)
                            where (a.CheckState == MachineryState.安装告知审核通过
                             || a.CheckState == MachineryState.办理使用登记审核中
                            || a.CheckState == MachineryState.办理使用登记未通过
                            || a.CheckState == MachineryState.办理使用登记通过)
                            && (a.MachineryCheckState == MachineryCheckStateEnum.复检合格
                            || a.MachineryCheckState == MachineryCheckStateEnum.检测合格)
                            select new RegistrationOfUseListView_Model
                            {
                                UpdateDate = a.UpdateDate,
                                RegistrationOfUseId = a.RegistrationOfUseId,
                                MachineryInfoId = a.MachineryInfoId,
                                MachineryType = a.MachineryType.GetHashCode(),
                                MachineryTypeName = ((MachineryTypeEnum)a.MachineryType).ToString(),
                                MachineryName = a.MachineryName,
                                MachineryModel = a.MachineryModel,
                                //AnChaiEntGUID = b.EntGUID,//安拆单位id                             
                                PropertyRightsRecordNo = a.PropertyRightsRecordNo,//产权备案编号
                                ChanQuanEntGUID = a.EntGUID,//所属产权单位id 如果是施工单位进行产权备案那么则是施工单位的id   
                                CheckStateValue = a.CheckState.GetHashCode(),
                                CheckState = ((MachineryState)a.CheckState).ToString(),
                                MachineryCheckStateValue = a.MachineryCheckState.GetHashCode(),
                                MachineryCheckState = ((MachineryCheckStateEnum)a.MachineryCheckState).ToString(),
                                InstallReviewDate = a.InstallReviewDate,
                                UseDengJiStateValue = a.CheckState == MachineryState.安装告知审核通过 ? a.MachineryCheckState.GetHashCode() : a.CheckState.GetHashCode(),
                                UseDengJiStateText = a.CheckState == MachineryState.安装告知审核通过 ? ((MachineryCheckStateEnum)a.MachineryCheckState).ToString() : ((MachineryState)a.CheckState).ToString(),
                                //PlanInstallDate = b.PlanInstallDate,//拟安装日期
                                //PlanUseBeginDate = b.PlanUseBeginDate,//拟使用日期
                                BuyDate = a.BuyDate,
                                MaxRatedLiftingCapacity = a.MaxRatedLiftingCapacity,
                                Knm = a.Knm,
                                MaxInstallHeight = a.MaxInstallHeight,
                                MaxRange = a.MaxRange,
                                UseReason = a.UseReason,
                                EntCode = a.EntCode,
                                EntName = a.EntName,
                                TestingInstituteInfoId = a.TestingInstituteInfoId,
                                AnChaiEntName = "",
                                ChanQuanEntName = "",
                                CheckRecordId = a.CheckRecordId,
                                RecordUrl = a.CheckUrl,
                                UseSubmitDate = a.UseSubmitDate,//使用登记提交日期
                                UseReviewDate = a.UseReviewDate,//使用登记审核日期
                                UseRecordNo = a.UseRecordNo,
                                RegistrationOfUseApplicationUrl = a.RegistrationOfUseApplicationUrl
                            }).Distinct().ToList();

                if (machineryType >= 0)
                {
                    data = data.Where(x => x.MachineryType == machineryType).ToList();
                }
                if (!string.IsNullOrWhiteSpace(machineryName))
                {
                    data = data.Where(x => x.MachineryName.Contains(machineryName)).ToList();
                }
                if (useDengJiStateValue >= 0)
                {
                    data = data.Where(x => x.UseDengJiStateValue == useDengJiStateValue).ToList();

                }
                int totalCount = data.Count;
                //使用登记提交日期排序
                var dataList = data.OrderByDescending(r => r.UseSubmitDate)
                    .ThenByDescending(r => r.UpdateDate).Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize).ToList();

                dataList.ForEach(w =>
                {

                    var anzhuanggaozhi = _context.InstallationNotificationRecords.Where(x => x.DeleteMark == 0 && x.State == 2
                    && x.MachineryInfoId == w.MachineryInfoId).OrderByDescending(x => x.Id).FirstOrDefault();
                    if (anzhuanggaozhi != null)
                    {
                        w.PlanInstallDate = anzhuanggaozhi.PlanInstallDate;
                        w.PlanUseBeginDate = anzhuanggaozhi.PlanUseBeginDate;//拟使用日期
                        w.AnChaiEntGUID = anzhuanggaozhi.EntGUID;//安拆单位id   
                    }

                    w.EntCode = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == w.EntCode)
                                                         .Select(x => x.EntCode).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(w.AnChaiEntGUID))
                    {
                        w.AnChaiEntName = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == w.AnChaiEntGUID)
                                                        .Select(x => x.EntName).FirstOrDefault();//安拆单位的名字
                    }
                    if (!string.IsNullOrWhiteSpace(w.ChanQuanEntGUID))
                    {
                        w.ChanQuanEntName = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == w.ChanQuanEntGUID)
                                   .Select(x => x.EntName).FirstOrDefault();
                    }
                    w.InstallationPosition = installs.Where(k => k.MachineryInfoId == w.MachineryInfoId)
                      .OrderByDescending(o => o.Id).Select(k => k.InstallationPosition).FirstOrDefault();
                });
                return ResponseViewModel<List<RegistrationOfUseListView_Model>>.Create(Status.SUCCESS, Message.SUCCESS, dataList, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError("办理使用登记列表：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<RegistrationOfUseListView_Model>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// css-检测机构列表
        /// </summary>   
        /// <returns></returns>
        [HttpGet]
        public ResponseViewModel<List<TestingInstituteInfo>> GetTestingInstituteInfoList(string name = null, string belongedTo = null)
        {
            try
            {

                //List<TestingInstituteInfo> list = new List<TestingInstituteInfo>();

                var list = _context.TestingInstituteInfo.Where(x => x.CityCode == "320100" && x.DeleteTag == 0).ToList();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    list = list.Where(x => x.MechanismName.Contains(name)).ToList();
                }
                return ResponseViewModel<List<TestingInstituteInfo>>.Create(Status.SUCCESS, Message.SUCCESS, list, list.Count);

            }
            catch (Exception ex)
            {
                _logger.LogError("检测机构列表：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<TestingInstituteInfo>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 提交检测
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<string>> SubmitCheck(List<CheckRecordAddViewModel> list)
        {
            try
            {
                if (list != null && list.Count > 0)
                {
                    var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//当前登录人userId
                    List<CheckRecord> addList = new List<CheckRecord>();
                    List<CheckRecord> editJianceList = new List<CheckRecord>();
                    List<MachineryInfo> jixieUpdateList = new List<MachineryInfo>();
                    var jixieList = await _context.MachineryInfos.Where(x => list.Select(t => t.MachineryInfoId).Contains(x.MachineryInfoId)).ToListAsync();
                    foreach (CheckRecordAddViewModel viemModel in list)
                    {
                        DateTime nowTime = DateTime.Now;
                        var jixie = jixieList.Where(k => k.MachineryInfoId == viemModel.MachineryInfoId).FirstOrDefault();
                        if (jixie == null)
                        {

                            return ResponseViewModel<string>.Create(Status.FAIL, "机械信息不存在");
                        }

                        if (jixie.MachineryCheckState != MachineryCheckStateEnum.复检中
                            || jixie.MachineryCheckState != MachineryCheckStateEnum.检测中)
                        {
                            MachineryCheckStateEnum stateNext = MachineryCheckStateEnum.检测中;
                            DateTime? checkSubmitDate = null;//提交检测时间
                            DateTime? reCheckSubmitDate = null; //重新检测时间

                            if (jixie.MachineryCheckState == MachineryCheckStateEnum.复检不合格
                                || jixie.MachineryCheckState == MachineryCheckStateEnum.检测不合格
                                || jixie.MachineryCheckState == MachineryCheckStateEnum.检测合格
                                || jixie.MachineryCheckState == MachineryCheckStateEnum.复检合格)
                            {
                                reCheckSubmitDate = nowTime;
                                stateNext = MachineryCheckStateEnum.复检中;
                                if (jixie.CheckSubmitDate == null)
                                {
                                    checkSubmitDate = nowTime;
                                }
                                if (!string.IsNullOrEmpty(jixie.TestingInstituteInfoId) && viemModel.TestingInstituteInfoId != jixie.TestingInstituteInfoId)
                                {

                                    return ResponseViewModel<string>.Create(Status.FAIL, "检测未通过的机械请选择原有的检测所检测");
                                }
                                else if (jixie.MachineryCheckState == MachineryCheckStateEnum.检测合格 && string.IsNullOrEmpty(jixie.TestingInstituteInfoId))
                                {
                                    //说明是关联的机械
                                    stateNext = MachineryCheckStateEnum.检测中;
                                    checkSubmitDate = nowTime;
                                }
                            }
                            else
                            {
                                stateNext = MachineryCheckStateEnum.检测中;
                                checkSubmitDate = nowTime;
                            }
                            bool isAdd = true;
                            if (!string.IsNullOrEmpty(jixie.CheckRecordId))
                            {
                                var jiancejilu = _context.CheckRecords.Where(a => a.MachineryInfoId == viemModel.MachineryInfoId && a.CheckRecordId == jixie.CheckRecordId).FirstOrDefault();
                                if (jiancejilu != null && jiancejilu.IsRectificationCheckFinish == 0)
                                {
                                    jiancejilu.IsRectificationCheckFinish = 1;
                                    jiancejilu.SubmitUserId = userId;//提交id
                                    jiancejilu.MachineryInfoId = viemModel.MachineryInfoId;
                                    jiancejilu.TestingInstituteInfoId = viemModel.TestingInstituteInfoId;
                                    jiancejilu.DeleteMark = 0;
                                    jiancejilu.CheckState = stateNext;
                                    jiancejilu.UpdateDate = nowTime;
                                    editJianceList.Add(jiancejilu);
                                    jixie.TestingInstituteInfoId = jiancejilu.TestingInstituteInfoId;
                                    isAdd = false;
                                }
                            }
                            if (isAdd)
                            {
                                CheckRecord model = new CheckRecord();
                                model.SubmitUserId = userId;//提交id
                                model.CheckRecordId = SecurityManage.GuidUpper();
                                model.MachineryInfoId = viemModel.MachineryInfoId;
                                model.TestingInstituteInfoId = viemModel.TestingInstituteInfoId;
                                model.DeleteMark = 0;
                                model.CheckState = stateNext;
                                model.CreateDate = nowTime;
                                model.UpdateDate = nowTime;
                                addList.Add(model);
                                jixie.CheckRecordId = model.CheckRecordId;
                                jixie.TestingInstituteInfoId = model.TestingInstituteInfoId;
                            }
                            //jixie.CheckState = stateNext;
                            jixie.MachineryCheckState = stateNext;
                            jixie.CheckSubmitDate = checkSubmitDate;//委托检测申请日期
                            jixie.ReCheckSubmitDate = reCheckSubmitDate;//委托重新检测申请日期
                            jixie.CheckReviewDate = null;
                            jixie.ReCheckReviewDate = null;
                            jixieUpdateList.Add(jixie);


                        }
                        else
                        {
                            return ResponseViewModel<string>.Create(Status.FAIL, "当前状态不可提交检测");
                        }


                    }
                    if (addList.Count > 0)
                    {
                        await _context.CheckRecords.AddRangeAsync(addList);
                    }

                    if (editJianceList.Count > 0)
                    {
                        _context.CheckRecords.UpdateRange(editJianceList);
                    }
                    _context.MachineryInfos.UpdateRange(jixieUpdateList);
                    await _context.SaveChangesAsync();
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "提交成功");
                }
                else
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("提交检测RegistrationOfUseController/SubmitCheck：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 撤回已提交的检测
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> RecallCheck(string machineryInfoId)
        {
            try
            {

                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//当前登录人userId
                List<CheckRecord> addList = new List<CheckRecord>();
                List<MachineryInfo> jixieUpdateList = new List<MachineryInfo>();
                var jixie = await _context.MachineryInfos.Where(x => x.MachineryInfoId == machineryInfoId).FirstOrDefaultAsync();

                DateTime nowTime = DateTime.Now;
                if (jixie == null)
                {

                    return ResponseViewModel<string>.Create(Status.FAIL, "机械信息不存在");
                }

                if (jixie.MachineryCheckState == MachineryCheckStateEnum.复检中 || jixie.MachineryCheckState == MachineryCheckStateEnum.检测中)
                {
                    var jiancejilu = await _context.CheckRecords.Where(w => w.CheckRecordId == jixie.CheckRecordId).FirstOrDefaultAsync();
                    if (jiancejilu != null)
                    {
                        //上一次检测记录
                        var shangyicijilu = await _context.CheckRecords.Where(w => w.MachineryInfoId == jixie.MachineryInfoId && w.DeleteMark == 0 && w.CheckRecordId != jiancejilu.CheckRecordId).OrderByDescending(w => w.Id).FirstOrDefaultAsync();
                        var buchongziliao = await _context.MachineryInfoSupplementaryInformations.Where(w => w.CheckRecordId == jixie.CheckRecordId).FirstOrDefaultAsync();
                        if (buchongziliao != null)
                        {
                            return ResponseViewModel<string>.Create(Status.ERROR, "该机械检测单位已在处理中。。。");
                        }
                        else
                        {
                            if (shangyicijilu == null)
                            {
                                //如果检测的记录列表只有一条检测说明是第一次提交
                                jixie.CheckRecordId = null;
                                jixie.MachineryCheckState = MachineryCheckStateEnum.未检测;
                                jixie.TestingInstituteInfoId = null;
                                jixie.CheckSubmitDate = null;//委托检测申请日期
                                jixie.ReCheckSubmitDate = null;//委托重新检测申请日期
                                jiancejilu.DeleteMark = 1;
                                _context.MachineryInfos.Update(jixie);
                                _context.CheckRecords.Update(jiancejilu);

                            }
                            else
                            {
                                //如果检测的记录列表只有一条检测说明是第一次提交
                                jixie.CheckRecordId = shangyicijilu.CheckRecordId;
                                jixie.MachineryCheckState = shangyicijilu.CheckState;
                                jixie.TestingInstituteInfoId = shangyicijilu.TestingInstituteInfoId;
                                jixie.CheckSubmitDate = null;//委托检测申请日期
                                jixie.ReCheckSubmitDate = null;//委托重新检测申请日期
                                jiancejilu.DeleteMark = 1;
                                _context.MachineryInfos.Update(jixie);
                                _context.CheckRecords.Update(jiancejilu);
                            }
                        }
                    }
                    else
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "无提交检测记录不可撤回");
                    }
                }
                else
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "当前状态不可撤回");
                }
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "提交成功");

            }
            catch (Exception ex)
            {
                _logger.LogError("提交检测RegistrationOfUseController/SubmitCheck：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


    }
}
