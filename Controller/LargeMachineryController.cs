using Aspose.Words;
using Aspose.Words.Tables;
using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Common.DataBll;
using JSAJ.Core.Controllers;
using JSAJ.Core.Models;
using JSAJ.Core.Models.LargeMachinery;
using JSAJ.Core.ViewModels;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ViewModels;
using Worker = JSAJ.Core.Common.Worker;

namespace JSAJNanJing.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LargeMachineryController : ControllerBase
    {
        private readonly JssanjianmanagerContext _context;

        private readonly ILogger<RegisterController> _logger;
        private readonly string _wordTemplte;
        private readonly string _buildWords;
        private readonly string _workJson;
        private OssFileSetting _ossFileSetting;

        private readonly IWebHostEnvironment _environment;
        public LargeMachineryController(IWebHostEnvironment environment, ILogger<RegisterController> logger, JssanjianmanagerContext context, IOptions<OssFileSetting> oss)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _wordTemplte = environment.WebRootPath + Path.DirectorySeparatorChar + "doc" + Path.DirectorySeparatorChar;
            _buildWords = environment.WebRootPath + Path.DirectorySeparatorChar + "BuildPdf" + Path.DirectorySeparatorChar;
            _workJson = environment.WebRootPath + Path.DirectorySeparatorChar + "TimeJson" + Path.DirectorySeparatorChar;
            _ossFileSetting = oss.Value;
        }


        /// <summary>
        /// 回显图片
        /// </summary>
        /// <param name="installationNotificationRecordId"></param>
        /// <param name="recordConfigId"></param>
        /// <returns></returns>
        public async Task<ResponseViewModel<List<RecordAttachment>>> GetUrl(string installationNotificationRecordId, string recordConfigId)
        {
            try
            {
                var query = await _context.RecordAttachments
                    .Where(s => s.RecordConfigId == recordConfigId && s.AttachmentId == installationNotificationRecordId && s.DeleteMark == 0)
                    .ToListAsync();
                return ResponseViewModel<List<RecordAttachment>>.Create(Status.SUCCESS, Message.SUCCESS, query);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取配置文件信息：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<RecordAttachment>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 查看申请详细信息
        /// </summary>
        /// <param name="installationNotificationRecordId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<EditInformation>> GetApplyInformation(string installationNotificationRecordId, int type)
        {
            if (string.IsNullOrEmpty(installationNotificationRecordId))
            {
                return ResponseViewModel<EditInformation>.Create(Status.SUCCESS, "缺少参数", null);
            }
            var query = await _context.InstallationNotificationRecords
                .Where(s => s.InstallationNotificationRecordId == installationNotificationRecordId && s.Type == type && s.DeleteMark == 0)
                .FirstOrDefaultAsync();
            if (query==null)
            {
                return ResponseViewModel<EditInformation>.Create(Status.SUCCESS, "参数错误", null);

            }
            var entUrl = await _context.EntRegisterInfoMag
                  .Where(s => s.EntRegisterInfoMagId == query.EntGUID)
                  .FirstOrDefaultAsync();
            Dictionary<int, SpecialWorker> aa = Worker.WorkerDic;
            var data1 = aa.Where(a => a.Value.IsSpecial == true).Select(a => new WorkTypeCodeViewModel { Name = a.Value.Value, key = a.Key });
            var data2 = _context.PersonTypes.ToList();
            var query1 = (from A in _context.InstallPeoples
                          join B in _context.MachineryPeoples on A.MachineryPersonId equals B.MachineryPersonId
                          where A.InstallationNotificationRecordId == installationNotificationRecordId
                          select new PeopleInformation
                          {
                              MachineryPersonId = A.MachineryPersonId,
                              PersonName = B.PersonName,
                              PersonType = _context.PersonTypes.Where(s => s.Code == B.PersonType).Select(k => k.Name).FirstOrDefault(),
                              Sex = B.Sex == 0 ? "男" : "女",
                              IdCard = B.IdCard,
                              CerUrl = B.CerUrl,
                              TypeCode = B.WorkTypeCode,
                              Tel = B.Tel,
                              SpecialWorkerTypeNo = B.SpecialWorkerTypeNo,
                              DeleteMark=B.DeleteMark
                          }).ToList();
            var query3 = await _context.MachineryInfos
                  .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                  .FirstOrDefaultAsync();
            var A1 = await _context.RecordAttachments.Where(s => s.AttachmentId == installationNotificationRecordId).ToListAsync();
            var B1 = await _context.RecordConfigs.Where(s => s.Type == RecordConfigTypeEnum.安装_拆卸告知).ToListAsync();
            if (type == 1)
            {
                B1 = B1.Where(s => s.AttachmentName != "保养记录表" && s.AttachmentName != "设备租赁合同").ToList();
            }
            List<FlieurlList> flieurls = new List<FlieurlList>();
            B1.ForEach(x =>
            {
                FlieurlList list = new FlieurlList();
                list.FileType = x.AttachmentName;
                list.RecordConfigId = x.RecordConfigId;
                list.Required = x.Required;
                list.TemplateUrl = x.TemplateName;
                list.TemplateName = x.TemplateName;
                List<FileList> urlList = new List<FileList>();
                A1.ForEach(f =>
                {
                    if (f.RecordConfigId == x.RecordConfigId)
                    {
                        FileList mode = new FileList();
                        mode.FileName = f.FileName;
                        mode.FileUrl = f.FileUrl;
                        mode.Suffix = Path.GetExtension(f.FileUrl).Replace(".", "");
                        urlList.Add(mode);


                    }
                });
                if (x.AttachmentName == "安装资质证书")
                {
                    FileList mode = new FileList();
                    mode.FileName = "";
                    mode.FileUrl = entUrl.IntelligenceLevelUrl;
                    mode.Suffix = "";
                    if (entUrl.EntQualificationCertificateUrl != null)
                    {
                        mode.Suffix = Path.GetExtension(entUrl.EntQualificationCertificateUrl).Replace(".", "");
                        urlList.Add(mode);
                    }
                }
                if (x.AttachmentName == "安全生产许可证")
                {
                    FileList mode1 = new FileList();
                    mode1.FileName = "";
                    mode1.FileUrl = entUrl.LicenceUrl;
                    mode1.Suffix = "";
                    if (entUrl.LicenceUrl != null)
                    {
                        mode1.Suffix = Path.GetExtension(entUrl.LicenceUrl).Replace(".", "");
                        urlList.Add(mode1);
                    }

                }
                list.FileUrl = urlList;


                flieurls.Add(list);

            });



            var data = await _context.EntRegisterInfoMag
                .Where(s => s.EntRegisterInfoMagId == query3.EntGUID)
                .FirstOrDefaultAsync();
            EditInformation information = new EditInformation();
            information.MachineryInfoId = query.MachineryInfoId;
            information.BelongedTo = query.BelongedTo;
            information.RecordNumber = query.RecordNumber;
            information.ProjectName = query.ProjectName;
            information.ProjectAddress = query.ProjectAddress;
            //
            information.EntCode = query.EntCode;
            information.EntName = query.EntName;
            information.EntGUID = query.EntGUID;
            information.CQEntName = data == null ? "" : data.EntName;
            //安装日期
            information.PlanInstallDate = query.PlanInstallDate;
            information.PlanInstallEndDate = query.PlanInstallEndDate;
            information.PlanUseBeginDate = query.PlanUseBeginDate;
            information.PlanUseEndDate = query.PlanUseEndDate;
            //拟拆卸日期
            information.PlanDisassembleBeginDate = query.PlanDisassembleBeginDate;
            information.PlanDisassembleEndDate = query.PlanDisassembleEndDate;
            //安装基本信息
            //information.MachineryPersonId = query.MachineryPersonId;(暂不需要匹配数据库人员)
            information.InstallationPosition = query.InstallationPosition;
            information.PropertyRightsRecordNo = query3.PropertyRightsRecordNo;
            information.MachineryTypeCode = query3.MachineryType.GetHashCode();
            information.MachineryName = query3.MachineryName;
            information.MachineModel = query3.MachineryModel;
            information.Knm = query3.Knm;
            information.InstallLeader = query.InstallLeader;
            information.InstallLeaderTel = query.InstallLeaderTel;
            information.SafetyPerson = query.SafetyPerson;
            information.SafetyPersonTel = query.SafetyPersonTel;
            information.InstallHeight = query.InstallHeight;
            information.Remark = query.Remark;
            information.Type = query.Type;

            information.Peoples = query1;
            information.FileUrlList = flieurls;


            return ResponseViewModel<EditInformation>.Create(Status.SUCCESS, Message.SUCCESS, information);
        }


        public async Task<ResponseViewModel<List<MachinList>>> GetMachinserList(string belongedTo, string recordNumber)
        {
            try
            {
                var data = await _context.MachineryInfos.Where(w => w.BelongedTo == belongedTo
                && w.RecordNumber == recordNumber && w.DeleteMark == 0)
                    .Select(k => new MachinList
                    {
                        MachineryType = k.MachineryType,
                        MachineryModel = k.MachineryModel,
                        PropertyRightsRecordNo = k.PropertyRightsRecordNo,
                        OEM = k.OEM,
                        ManufacturingLicense = k.ManufacturingLicense,
                        LeaveTheFactoryNo = k.LeaveTheFactoryNo,
                        CheckState = k.CheckState,
                        MachineryInfoId = k.MachineryInfoId
                    })
                    .ToListAsync();
                return ResponseViewModel<List<MachinList>>.Create(Status.SUCCESS, Message.SUCCESS, data, data.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取项目的机械信息：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<MachinList>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 获取安拆申请信息列表
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ApplyInformation>>> GetApplyList(string projectName, string machineryType,
            string propertyRightsRecordNo, string entName, int type, int pageIndex, int pageSize, int state)
        {
            try
            {

                var entGUID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;


                var data = (from A in _context.InstallationNotificationRecords
                            join B in _context.MachineryInfos
                            on A.MachineryInfoId equals B.MachineryInfoId
                            into t1
                            from r in t1.DefaultIfEmpty()
                            join C in _context.EntRegisterInfoMag on r.EntGUID equals C.EntRegisterInfoMagId
                            where A.Type == type && A.DeleteMark == 0 && A.EntGUID == entGUID && r.DeleteMark == 0
                            select new ApplyInformation
                            {
                                InstallationNotificationRecordId = A.InstallationNotificationRecordId,
                                UpdateTime = A.UpdateDate,
                                RecordNumber = A.RecordNumber,
                                BelongedTo = A.BelongedTo,
                                ProjectName = A.ProjectName,
                                ProjectAddres = A.ProjectAddress,
                                MachineryType = r.MachineryType.ToString(),
                                MachineryInfoId = r.MachineryInfoId,
                                MachineryName = r.MachineryName,
                                MachineryModel = r.MachineryModel,
                                PropertyRightsRecordNo = r.PropertyRightsRecordNo,
                                EntName = C.EntName,
                                ApplyRemark = A.Reason,
                                State = A.State,
                                AuditStatus=A.AuditStatus,
                                InstallationPosition = A.InstallationPosition
                            }).Distinct().ToList();
                //data = from A in data
                //       join B in _context.ProjectOverview
                //       on new { A.BelongedTo, A.RecordNumber } equals new { B.BelongedTo, B.RecordNumber }
                //       select new ApplyInformation
                //       {
                //           InstallationNotificationRecordId = A.InstallationNotificationRecordId,
                //           UpdateTime = A.UpdateTime,
                //           RecordNumber = A.RecordNumber,
                //           BelongedTo = A.BelongedTo,
                //           ProjectName = A.ProjectName,
                //           ProjectAddres = A.ProjectAddres,
                //           MachineryType = A.MachineryType,
                //           MachineryInfoId = A.MachineryInfoId,
                //           MachineryName = A.MachineryName,
                //           MachineryModel = A.MachineryModel,
                //           PropertyRightsRecordNo = A.PropertyRightsRecordNo,
                //           EntName = A.EntName,
                //           ApplyRemark = A.ApplyRemark,
                //           State = A.State,
                //           InstallationPosition = A.InstallationPosition,
                //           DepartmentName = B.SupervisionDepartmentId
                //       };
                data.ForEach(f =>
                {
                    f.DepartmentName=_context.ProjectOverview.Where(w => w.BelongedTo==f.BelongedTo && w.RecordNumber==f.RecordNumber).Select(s => s.SupervisionDepartmentId).FirstOrDefault();
                });
                //判断搜索条件
                IEnumerable<ApplyInformation> personIEn = data;
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.ProjectName) && s.ProjectName.Contains(projectName));
                }
                if (!string.IsNullOrWhiteSpace(machineryType))
                {
                    personIEn = personIEn
                    .Where(s => !string.IsNullOrWhiteSpace(s.MachineryType) && s.MachineryType == machineryType);

                }
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.PropertyRightsRecordNo) && s.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }

                if (!string.IsNullOrWhiteSpace(entName))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.EntName) && s.EntName.Contains(entName));
                }
                if (state != -1)
                {
                    personIEn = personIEn
                        .Where(s => s.State == state);
                }

                int queryCount = personIEn.Count();
                var result = personIEn.OrderByDescending(t => t.UpdateTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                result.ForEach(x =>
                {
                    x.DepartmentName = _context.SupervisionDepartment.Where(w => w.SupervisionDepartmentId == x.DepartmentName).Select(k => k.Name).FirstOrDefault();
                });

                return ResponseViewModel<List<ApplyInformation>>.Create(Status.SUCCESS, Message.SUCCESS, result, queryCount);
            }
            catch (Exception ex)
            {

                _logger.LogError("获取告知信息列表失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<ApplyInformation>>.Create(Status.ERROR, Message.ERROR);
            }



        }


        /// <summary>
        /// 获取项目名称
        /// </summary>
        /// <param name="entName"></param>
        /// <param name="projectName"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<object>> GetProjrctList(string projectName)
        {
            try
            {
                var query = from A in _context.ProjectOverview
                            join B in _context.ProjectEntSnapshot
                            on new { A.RecordNumber, A.BelongedTo } equals new { B.RecordNumber, B.BelongedTo }
                            where !A.RecordNumber.StartsWith("T") && B.EnterpriseType == "施工单位"
                            select new
                            {
                                A.ProjectName,
                                B.EnterpriseName,
                                A.RecordNumber
                            };


                var data = await query.Where(s => s.ProjectName.Contains(projectName))
                    .OrderBy(k => k.ProjectName.Length)
                    .Select(p => new { p.ProjectName, p.EnterpriseName, p.RecordNumber })
                    .Take(50)
                    .ToListAsync();
                //
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, data);


            }
            catch (Exception ex)
            {
                _logger.LogError("获取项目名称失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 获取安拆申请的项目名称
        /// </summary>
        /// <param name="entName"></param>
        /// <param name="projectName"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<object>> GetEntProjrctList(string projectName)
        {
            try
            {
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var recordNumber = _context.EquipmentInsDis.Where(s => s.EntRegisterInfoMagId==uuid).Select(s => s.RecordNumber).ToList();

                var query = from A in _context.ProjectOverview
                            join B in _context.ProjectEntSnapshot
                            on new { A.RecordNumber, A.BelongedTo } equals new { B.RecordNumber, B.BelongedTo }
                            where !A.RecordNumber.StartsWith("T") && B.EnterpriseType == "施工单位"
                            select new
                            {
                                A.ProjectName,
                                B.EnterpriseName,
                                A.RecordNumber
                            };


                var data = await query.Where(s => recordNumber.Contains(s.RecordNumber) && s.ProjectName.Contains(projectName))
                    .OrderBy(k => k.ProjectName.Length)
                    .Select(p => new { p.ProjectName, p.EnterpriseName, p.RecordNumber })
                    .Take(50)
                    .ToListAsync();
                //
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, data);


            }
            catch (Exception ex)
            {
                _logger.LogError("获取项目名称失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 获取安监站提供的提示信息
        /// </summary>
        /// <param name="belongedTo"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> GetAJinfomercial(string belongedTo, int type)
        {
            try
            {
                var query = await _context.AJPersonOfUseNotices
                    .Where(s => s.BelongedTo == belongedTo && s.DeleteMark == 0).FirstOrDefaultAsync();
                if (query == null)
                {
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "");
                }

                var data = "提示：" + query.NoticeInstall;
                if (type == 1)
                {
                    data = "提示：" + query.NoticeDisassemble;
                }
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, data);
            }
            catch (Exception ex)
            {

                _logger.LogError("获取提示失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// 获取人员信息
        /// </summary>
        /// <param name="typee"></param>
        /// <param name="workType"></param>
        /// <param name="peopleName"></param>
        /// machineyType=3,4,6 门式桥式架桥机不做审核限制
        ///  塔式起重机 = 0,//t   S  W Q  T
        ///  施工升降机 = 1,//s
        ///  货运施工升降机 = 2,//w
        ///  桥式起重机 = 3,//q
        ///  门式起重机 = 4,
        ///  物料提升机 = 5,
        ///  架桥机 = 6
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<InstallPeoples>>> GetInstallPeoplesQy(string peopleName, int type, string recordNumber, string belongedTo, int machineyType)
        {
            try
            {
                List<MachineryPeople> peoples = new List<MachineryPeople>();
                var entGUID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var now = DateTime.Now.Date;
                var data = _context.MachineryPeoples.Where(w => w.DeleteMark == 0 && w.EntGUID==entGUID && w.UseEndDate.Value.Date.AddDays(1)<=now).ToList();

                //判断门、桥、架 安拆人员不审核
                if (machineyType== 0 || machineyType == 1 || machineyType == 2 || machineyType == 5)
                {
                    data=data.Where(w => w.AuditStatus==2).ToList();
                }
                //施工，物料当天不能在两个项目上 可以在多台机械上
                if (machineyType == 1 || machineyType == 5)
                {
                    var a=data.Where(w => w.RecordNumber!=null && w.RecordNumber!="").ToList();
                    if (a.Count() > 0)
                    {
                       var c=data.Where(w => w.RecordNumber==recordNumber).ToList();
                        peoples.AddRange(c);
                    }
                    var b=data.Where(w => w.RecordNumber==null || w.RecordNumber=="").ToList();
                    if (b.Count() > 0)
                    {
                        peoples.AddRange(b);
                    }

                }
                //塔 门 桥 当天一人不能在多台机械上
                if (machineyType==0 || machineyType ==3 || machineyType == 4)
                {
                    MachineryTypeEnum typeEnum = (MachineryTypeEnum)machineyType;
                    foreach (var item in data)
                    {
                        var infos = _context.InstallPeoples.Where(w => w.DeleteMark==0 && w.Type==type && w.MachineryPersonId==item.MachineryPersonId).ToList();
                        if (infos.Count()>0)
                        {
                            foreach (var item1 in infos)
                            {
                                var info = _context.InstallationNotificationRecords.Where(w => w.DeleteMark ==0 && w.InstallationNotificationRecordId == item1.InstallationNotificationRecordId && w.Type==type).FirstOrDefault();
                                if (info==null)
                                {
                                  
                                        peoples.Add(item);
                                }
                                
                            }
                        }
                        else
                        {
                            peoples.Add(item);
                        }
                    }
                }
                if (machineyType ==2 || machineyType ==6)
                {
                    peoples.AddRange(data);
                }

                if (!string.IsNullOrWhiteSpace(peopleName))
                {
                    data = data.Where(w => w.PersonName.Contains(peopleName)).ToList();
                }

                if (type == 0)
                {
                    data = data.Where(w => w.EntGUID == entGUID).ToList();
                }
                if (type == 1)
                {
                    data = data.Where(w => w.RecordNumber == recordNumber && w.BelongedTo == belongedTo).ToList();
                }
                List<InstallPeoples> list = new List<InstallPeoples>();
                var query = await _context.PersonTypes.ToListAsync();
                peoples.Distinct().ToList().ForEach(x =>
                {
                    Dictionary<int, SpecialWorker> aa = Worker.WorkerDic;
                    var data1 = aa.Where(a => a.Value.IsSpecial == true).Select(a => new WorkTypeCodeViewModel { Name = a.Value.Value, key = a.Key }).ToList();
                    InstallPeoples mode = new InstallPeoples();
                    mode.MachineryPersonId = x.MachineryPersonId;
                    mode.PersonName = x.PersonName;
                    mode.PersonTypeCode = x.PersonType;
                    mode.PersonType = query.Where(s => s.Code == x.PersonType).Select(k => k.Name).FirstOrDefault();
                    mode.Sex = x.Sex == 0 ? "男" : "女";
                    mode.WorkTypeCode = data1.Where(s => s.key == x.WorkTypeCode).Select(k => k.Name).FirstOrDefault();// x.WorkTypeCode;
                    mode.IdCard = x.IdCard;
                    mode.Tel = x.Tel;
                    mode.CerValidEndDate = x.CerValidEndDate;
                    mode.SpecialWorkerTypeNo = x.SpecialWorkerTypeNo;
                    mode.CerUrl = x.CerUrl;
                    mode.EntGUID = entGUID;
                    mode.RecordNumber = x.RecordNumber;
                    mode.BelongedTo = x.BelongedTo;
                    list.Add(mode);
                });
                return ResponseViewModel<List<InstallPeoples>>.Create(Status.SUCCESS, Message.SUCCESS, list);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取项目失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<InstallPeoples>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 获取人员信息
        /// </summary>
        /// <param name="typee"></param>
        /// <param name="workType"></param>
        /// <param name="peopleName"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<InstallPeoples>>> GetInstallPeoples(string peopleName, int type, string recordNumber, string belongedTo)
        {
            try
            {
               
                var entGUID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var now = DateTime.Now.Date;
                var InsInfo = _context.InstallationNotificationRecords.Where(w => w.DeleteMark==0 && w.Type== type && w.EntGUID==entGUID && w.PlanInstallEndDate.Value.Date.AddDays(1)<=now).Select(s => new { s.InstallationNotificationRecordId, s.PlanInstallEndDate }).ToList();
                var query1 = (from A in _context.InstallPeoples
                              join B in _context.MachineryPeoples on A.MachineryPersonId equals B.MachineryPersonId
                              where InsInfo.Select(s=>s.InstallationNotificationRecordId).Contains(A.InstallationNotificationRecordId) && A.DeleteMark==0 && B.DeleteMark==0 && B.AuditStatus==2 && A.Type==type
                              select new PeopleInformation
                              {
                                  MachineryPersonId = A.MachineryPersonId,
                                  PersonName = B.PersonName,
                                  PersonType = _context.PersonTypes.Where(s => s.Code == B.PersonType).Select(k => k.Name).FirstOrDefault(),
                                  Sex = B.Sex == 0 ? "男" : "女",
                                  IdCard = B.IdCard,
                                  CerUrl = B.CerUrl,
                                  TypeCode = B.WorkTypeCode,
                                  Tel = B.Tel,
                                  SpecialWorkerTypeNo = B.SpecialWorkerTypeNo,
                                  DeleteMark=B.DeleteMark
                              }).Distinct().ToList();
                var data = _context.MachineryPeoples.Where(w => w.DeleteMark == 0 && w.AuditStatus==2);

                if (!string.IsNullOrWhiteSpace(peopleName))
                {
                    data = data.Where(w => w.PersonName.Contains(peopleName));
                }

                if (type == 0)
                {
                    data = data.Where(w => w.EntGUID == entGUID);
                }
                if (type == 1)
                {
                    data = data.Where(w => w.RecordNumber == recordNumber && w.BelongedTo == belongedTo);
                }
                List<InstallPeoples> list = new List<InstallPeoples>();
                var query = await _context.PersonTypes.ToListAsync();
                data.ToList().ForEach(x =>
                {
                    Dictionary<int, SpecialWorker> aa = Worker.WorkerDic;
                    var data1 = aa.Where(a => a.Value.IsSpecial == true).Select(a => new WorkTypeCodeViewModel { Name = a.Value.Value, key = a.Key }).ToList();
                    InstallPeoples mode = new InstallPeoples();
                    mode.MachineryPersonId = x.MachineryPersonId;
                    mode.PersonName = x.PersonName;
                    mode.PersonTypeCode = x.PersonType;
                    mode.PersonType = query.Where(s => s.Code == x.PersonType).Select(k => k.Name).FirstOrDefault();
                    mode.Sex = x.Sex == 0 ? "男" : "女";
                    mode.WorkTypeCode = data1.Where(s => s.key == x.WorkTypeCode).Select(k => k.Name).FirstOrDefault();// x.WorkTypeCode;
                    mode.IdCard = x.IdCard;
                    mode.Tel = x.Tel;
                    mode.CerValidEndDate = x.CerValidEndDate;
                    mode.SpecialWorkerTypeNo = x.SpecialWorkerTypeNo;
                    mode.CerUrl = x.CerUrl;
                    mode.EntGUID = entGUID;
                    mode.RecordNumber = x.RecordNumber;
                    mode.BelongedTo = x.BelongedTo;
                    list.Add(mode);
                });
                return ResponseViewModel<List<InstallPeoples>>.Create(Status.SUCCESS, Message.SUCCESS, list);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取项目失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<InstallPeoples>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 获取项目信息
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<LargeMachineryViewModel>> GetProjectInformation(string projectName, string entName)
        {
            try
            {
                var query = await _context.ProjectEntSnapshot
               .Where(s => s.EnterpriseType == "施工单位" && s.MainUnit == "是" && s.ProjectName == projectName
               && s.EnterpriseName == entName && !s.RecordNumber.StartsWith("T"))
               .OrderByDescending(k => k.Id)
               .FirstOrDefaultAsync();

                LargeMachineryViewModel model = new LargeMachineryViewModel();
                if (query == null)
                {
                    return ResponseViewModel<LargeMachineryViewModel>.Create(Status.ERROR, "该项目无主体施工单位，请联系管理员");
                }

                //var query1 = await _context.EntRegisterInfoMag
                //    .Where(s => s.EntCode == query.OrganizationCode)
                //    .FirstOrDefaultAsync();
                //if (query1 == null)
                //{
                //    return ResponseViewModel<LargeMachineryViewModel>.Create(Status.ERROR, "该项目的施工单位未在系统内注册，请联系施工单位在系统登录页面进行企业账号注册");
                //}
                model.BelongedTo = query.BelongedTo;
                model.RecordNumber = query.RecordNumber;
                model.ProjectAddress = query.ProjectAddress;
                model.ProjectName = query.ProjectName;
                model.EntCode = query.OrganizationCode;
                model.EntName = query.EnterpriseName;
                return ResponseViewModel<LargeMachineryViewModel>.Create(Status.SUCCESS, Message.SUCCESS, model);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取项目信息失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<LargeMachineryViewModel>.Create(Status.ERROR, Message.ERROR);
            }

        }


        /// <summary>
        /// 获取安装资质
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseViewModel<string>> GetZiZhi()
        {
            try
            {
                var entCode = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                var query = await _context.EntRegisterInfoMag.Where(s => s.EntRegisterInfoMagId == entCode).Select(k => k.IntelligenceLevelUrl).FirstOrDefaultAsync();
                if (query == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "未上传资质证书！");
                }
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// 获取机械使用说明书
        /// </summary>
        /// <param name="manualName"></param>
        /// <returns></returns>
        public async Task<ResponseViewModel<List<InstructionManualViewModel>>> GetManualList(string manualName)
        {
            try
            {
                var data = await _context.InstructionManuals.Where(w => w.DeleteMark == 0)
                    .Select(k => new InstructionManualViewModel
                    {
                        ManualName = k.ManualName,
                        Url = k.Url
                    }).ToListAsync();
                if (!string.IsNullOrWhiteSpace(manualName))
                {
                    data = data.Where(s => s.ManualName.Contains(manualName)).ToList();
                }
                return ResponseViewModel<List<InstructionManualViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, data);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取机械使用说明书文件信息：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<InstructionManualViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 获取配置文件信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<FileUrl>>> GetEnclosure(int type)
        {
            try
            {
                var encCode = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var entUrl = await _context.EntRegisterInfoMag
                    .Where(s => s.EntRegisterInfoMagId == encCode)
                    .FirstOrDefaultAsync();
                var query = await _context.RecordConfigs
                    .Where(s => s.Type == RecordConfigTypeEnum.安装_拆卸告知)
                    .ToListAsync();
                List<FileUrl> list = new List<FileUrl>();
                //List<FileList> fileLists = new List<FileList>();
                query.ForEach(x =>
                {
                    if (type == 0)
                    {


                        FileUrl mode = new FileUrl();
                        mode.FileType = x.AttachmentName;
                        mode.RecordConfigId = x.RecordConfigId;
                        mode.Required = x.Required;
                        mode.TemplateName = x.TemplateName;
                        mode.TemplateUrl = x.TemplateUrl;
                        if (x.AttachmentName == "安装资质证书")
                        {
                            List<FileList> fileLists = new List<FileList>();
                            FileList list1 = new FileList();
                            mode.RouteUrl = entUrl.EntQualificationCertificateUrl;
                            mode.NoUpload = true;
                            list1.FileName = "";
                            list1.FileUrl = entUrl.EntQualificationCertificateUrl;

                            list1.Suffix = "";
                            if (mode.RouteUrl != null)
                            {
                                list1.Suffix = Path.GetExtension(entUrl.EntQualificationCertificateUrl).Replace(".", "");
                            }

                            fileLists.Add(list1);
                            mode.File = fileLists;
                        }
                        if (x.AttachmentName == "安全生产许可证")
                        {
                            List<FileList> fileLists = new List<FileList>();
                            FileList list1 = new FileList();
                            mode.RouteUrl = entUrl.LicenceUrl;
                            mode.NoUpload = true;
                            list1.FileName = "";
                            list1.FileUrl = entUrl.LicenceUrl;
                            list1.Suffix = "";
                            if (mode.RouteUrl != null)
                            {
                                list1.Suffix = Path.GetExtension(entUrl.LicenceUrl).Replace(".", "");
                            }
                            fileLists.Add(list1);
                            mode.File = fileLists;
                        }
                        list.Add(mode);
                    }
                    else
                    {

                        FileUrl mode = new FileUrl();
                        mode.FileType = x.AttachmentName;
                        mode.RecordConfigId = x.RecordConfigId;
                        mode.Required = x.Required;
                        mode.TemplateName = x.TemplateName;
                        mode.TemplateUrl = x.TemplateUrl;
                        if (x.AttachmentName == "安装资质证书")
                        {
                            FileList list1 = new FileList();
                            List<FileList> fileLists = new List<FileList>();
                            mode.RouteUrl = entUrl.EntQualificationCertificateUrl;
                            mode.NoUpload = true;
                            list1.FileName = "";
                            list1.FileUrl = entUrl.EntQualificationCertificateUrl;
                            if (entUrl.EntQualificationCertificateUrl != null)
                            {
                                list1.Suffix = Path.GetExtension(entUrl.EntQualificationCertificateUrl).Replace(".", "");
                            }

                            fileLists.Add(list1);
                            mode.File = fileLists;
                        }
                        if (x.AttachmentName == "安全生产许可证")
                        {
                            List<FileList> fileLists = new List<FileList>();
                            FileList list1 = new FileList();
                            mode.RouteUrl = entUrl.LicenceUrl;
                            mode.NoUpload = true;
                            list1.FileName = "";
                            list1.FileUrl = entUrl.LicenceUrl;
                            if (entUrl.LicenceUrl != null)
                            {
                                list1.Suffix = Path.GetExtension(entUrl.LicenceUrl).Replace(".", "");
                            }

                            fileLists.Add(list1);
                            mode.File = fileLists;
                        }
                        if (x.AttachmentName != "保养记录表" && x.AttachmentName != "设备租赁合同")
                        {
                            list.Add(mode);
                        }
                        ;

                    }

                });
                return ResponseViewModel<List<FileUrl>>.Create(Status.SUCCESS, Message.SUCCESS, list);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取项目失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<FileUrl>>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// 查询机械信息
        /// </summary>
        /// <param name="propertyRightsRecordNo"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<PropertyInformation>> GetMechanicsInfo(string entCode, string propertyRightsRecordNo, int type, string belongedTo, string recordNumber)
        {
            try
            {
                var info = _context.EquipmentInsDis.Where(w => w.RecordNumber==recordNumber && w.BelongedTo==belongedTo && w.DeleteMark==0).ToList();
                var query = await _context.MachineryInfos
                    .Where(s => s.PropertyRightsRecordNo == propertyRightsRecordNo && s.DeleteMark == 0)
                    .FirstOrDefaultAsync();
             

                if (query == null)
                {
                    return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "机械设备信息号输入错误!");
                }
                var query1 =  _context.InstallationNotificationRecords
               .Where(s => s.MachineryInfoId == query.MachineryInfoId && s.DeleteMark == 0 && s.Type ==0)
               .FirstOrDefault();
                var query11 = _context.InstallationNotificationRecords
                .Where(s => s.MachineryInfoId == query.MachineryInfoId && s.DeleteMark == 0 &&  s.Type ==1)
                .FirstOrDefault();
                if (query11!=null && query1!=null)
                {
                    if (query1.AuditStatus!= 6 || query11.AuditStatus !=6 || query11.JianliStatus !=1)   //query1!=null && query11 !=null 
                    {
                        return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "该机械正在被使用，无法进行使用!");
                    }
                }
             
                int Etype = 0;
                switch (query.MachineryType)
                {
                    case MachineryTypeEnum.塔式起重机:
                        Etype=0;
                        break;
                    case MachineryTypeEnum.施工升降机:
                        Etype=1;
                        break;
                    case MachineryTypeEnum.货运施工升降机:
                        Etype=6;
                        break;
                    case MachineryTypeEnum.桥式起重机:
                        Etype=3;
                        break;
                    case MachineryTypeEnum.门式起重机:
                        Etype=4;
                        break;
                    case MachineryTypeEnum.物料提升机:
                        Etype=2;
                        break;
                    case MachineryTypeEnum.架桥机:
                        Etype=5;
                        break;
                    default:
                        break;
                }
                info=info.Where(w => w.DeviceTypes==Etype).ToList();
                if (info.Count() == 0)
                {
                    return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "项目未添加该作业类型!");

                }

                var attachment = await _context.RecordAttachments
                                  .Where(w => w.AttachmentId == query.MachineryInfoId && w.DeleteMark == 0 && w.Type == RecordConfigTypeEnum.产权备案)
                                  .ToListAsync();
                if (query.IsOldData == 1)
                {

                    var coun1 = attachment.Where(w => w.RecordConfigId == "317F1383F79E4CF7B782810F9A3482E5").Count();
                    if (coun1 < 1)
                    {
                        return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "该机械未上传机械制造许可证！,请先在设备信息登记界面上传!");
                    }
                    var coun2 = attachment.Where(w => w.RecordConfigId == "8F997689665245F4A10E14867050CCDC").Count();
                    if (coun2 < 1)
                    {
                        return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "该机械未上传产品合格证！,请先在设备信息登记界面上传!");
                    }
                    var coun3 = attachment.Where(w => w.RecordConfigId == "654515DBCAB841ABA9367DE52E876142").Count();
                    if (coun3 < 1)
                    {
                        return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "该机械未上传购机合同、发票或有效凭证！,请先在设备信息登记界面上传!");
                    }
                    var coun4 = attachment.Where(w => w.RecordConfigId == "41FAB8E027EC4BE58B0864B779E35EEC").Count();
                    if (coun4 < 1)
                    {
                        return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "该机械未上传承诺书！,请先在设备信息登记界面上传!");
                    }
                }



                //判断机械是否在使用
                if (query.State != 2)
                {
                    return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "该机械正在审核，信息暂未入库!");
                }
                if (query.CancellationState != CancellationStateEnum.未提交 && query.CancellationState != CancellationStateEnum.审核不通过)
                {
                    return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "该机械已备案注销，暂不可使用!");
                }
                if (query.DeleteMark == 1)
                {
                    return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "该机械已删除，暂不可使用!");
                }
                var cnaleas = await _context.EntRegisterInfoMag.Where(w => w.EntRegisterInfoMagId == query.EntGUID)
                    .FirstOrDefaultAsync();
                if (cnaleas == null)
                {
                    return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "机械暂无单位拥有，无法使用");
                }
                var query2 = await _context.MachineryInfos
                                    .Where(s => s.EntGUID == entCode)
                                    .FirstOrDefaultAsync();
                //if (query2 == null && cnaleas.CanLease != 1)
                //{
                //    return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "该设备非项目使用单位自有设备也不是租赁企业设备，请重新选择");
                //}
                var query3 = cnaleas;
                var CQEntName = await _context.EntRegisterInfoMag
                    .Where(s => s.EntRegisterInfoMagId == query.EntGUID)
                    .FirstOrDefaultAsync();
                //判断当前公司是否添加申请过此台机械
                var query4 = await _context.InstallationNotificationRecords
                    .Where(s => s.EntGUID == query3.EntRegisterInfoMagId
                    && s.MachineryInfoId == query.MachineryInfoId
                    && s.State == 0 && s.DeleteMark == 0)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                var query5 = await _context.InstallationNotificationRecords
                   .Where(s => s.EntGUID == query3.EntRegisterInfoMagId
                   && s.MachineryInfoId == query.MachineryInfoId
                   && s.State == 10 && s.DeleteMark == 0)
                   .OrderByDescending(o => o.Id)
                   .FirstOrDefaultAsync();
                if (type == 0 && query4 != null || type == 1 && query5 != null)
                {
                    return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "请勿重复添加此台机械的申请");
                }
                if (query.CheckState != MachineryState.使用登记注销审核通过
                    && query.CheckState != MachineryState.安装告知审核不通过
                    && query.CheckState != MachineryState.未安装告知
                    && type == 0)
                {
                    return ResponseViewModel<PropertyInformation>.Create(Status.FAIL, "此机械正在被使用，无法申请安装告知");
                }
                else if (query.CheckState != MachineryState.办理使用登记通过
                    && query.CheckState != MachineryState.拆卸告知审核不通过
                    && type == 1)
                {
                    return ResponseViewModel<PropertyInformation>.Create(Status.FAIL, "当前状态无法申请拆卸告知");
                }

                if ((type == 0 && query.CheckState == MachineryState.未安装告知
                    || query.CheckState != MachineryState.使用登记注销审核通过
                    ) || (type == 1 && query.CheckState == MachineryState.办理使用登记通过
                    || query.CheckState != MachineryState.拆卸告知审核不通过
                    ))
                {
                    PropertyInformation mode = new PropertyInformation();
                    mode.PropertyRightsRecordNo = query.PropertyRightsRecordNo;
                    mode.MachineryTypeCode = query.MachineryType.GetHashCode();
                    mode.MachineryName = query.MachineryName;
                    mode.MachineryInfoId = query.MachineryInfoId;
                    mode.EntCode = query.EntGUID;
                    mode.CQEntName = CQEntName.EntName;
                    mode.EntName = query3.EntName;
                    mode.Knm = query.Knm;
                    mode.MachineModel = query.MachineryModel;
                    return ResponseViewModel<PropertyInformation>.Create(Status.SUCCESS, Message.SUCCESS, mode);
                }



                return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, "获取机械信息失败");
            }
            catch (Exception ex)
            {
                _logger.LogError("查询机械信息失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<PropertyInformation>.Create(Status.ERROR, Message.ERROR);
            }

        }


        [HttpPost]
        public async Task<ResponseViewModel<string>> GetMchineChengNoshu([FromBody] ChengNoshu query)
        {
            try
            {

                //MachineryChengNoshu
                //var query = await _context.MachineryInfos
                //    .Where(s => s.MachineryInfoId == machineryInfoId)
                //    .FirstOrDefaultAsync();
                //生成检查表路径
                string webRootPath = _wordTemplte + "Machinery/MachineryChengNoshu.docx";
                Aspose.Words.Document doc = new Aspose.Words.Document(webRootPath);

                DocumentBuilder builder = new DocumentBuilder(doc);
                Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                keyValuePairs.Add("MachineType", query.MachineType ?? "");
                keyValuePairs.Add("MachineryModel", query.MachineryModel ?? "");
                keyValuePairs.Add("OEM", query.OEM ?? "");

                keyValuePairs.Add("LeaveTheFactoryNo", query.LeaveTheFactoryNo ?? "");
                keyValuePairs.Add("ManufacturingLicense", query.ManufacturingLicense ?? "");
                keyValuePairs.Add("LeaveTheFactoryDate", query.LeaveTheFactoryDate?.ToString("yyyy-MM-dd"));
                keyValuePairs.Add("BuyDate", query.BuyDate?.ToString("yyyy-MM-dd"));
                var data = query.LeaveTheFactoryNo.Replace("#", "");
                //CanBeDelete存在此文件夹是可以定期删除的
                var path = new DataBll(_environment, _context).BuildPdfToServer(_environment, doc, keyValuePairs, "CanBeDeleted", SecurityManage.GuidUpper() + "MachineryChengNoshu.pdf", Request);
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, path);
            }
            catch (Exception ex)
            {

                _logger.LogError("下载机械入库承诺书：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 保存申请安装告知/拆卸告知
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<ResponseViewModel<string>> SaveNotificationApply([FromBody] SubmitApplication info)
        {
            try
            {

                var entGUID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//企业表Id
                var query = await _context.MachineryInfos
                    .Where(s => s.MachineryInfoId == info.MachineryInfoId && s.DeleteMark == 0)
                    .FirstOrDefaultAsync();
                if (query == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "该机械未备案");
                }
                if (query.CheckState != MachineryState.使用登记注销审核通过
                    && query.CheckState != MachineryState.安装告知审核不通过
                    && query.CheckState != MachineryState.未安装告知
                    && info.Type == 0)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "此机械正在被使用，无法申请安装告知");
                }
                else if (query.CheckState != MachineryState.办理使用登记通过
                    && query.CheckState != MachineryState.拆卸告知审核不通过
                    && info.Type == 1)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "当前状态无法申请拆卸告知");
                }
                if (info.Type == 1)
                {
                    if (query.BelongedTo != info.BelongedTo || query.RecordNumber != info.RecordNumber)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "此机械不在该项目上，无法申请拆卸告知");
                    }
                }
                InstallationNotificationRecord installation = new InstallationNotificationRecord();
                //添加人员信息
                List<InstallPeople> list = new List<InstallPeople>();

                var people = _context.MachineryPeoples.Where(w => info.PeopleLists.Select(s=>s.PMachineryPersonId).Contains(w.MachineryPersonId) && w.DeleteMark == 0 && w.Type == 0).ToList();
                foreach (var item in people)
                {
                    item.UseEndDate=info.PlanInstallEndDate;
                    item.BelongedTo=info.BelongedTo;
                    item.RecordNumber=info.RecordNumber;
                    _context.MachineryPeoples.Update(item);
                }
                var guId = SecurityManage.GuidUpper();
                //编辑更新保存
                if (!string.IsNullOrWhiteSpace(info.EditNumber))
                {
                    var obj = await _context.InstallationNotificationRecords
                        .Where(s => s.InstallationNotificationRecordId == info.EditNumber)
                        .FirstOrDefaultAsync();
                    guId = info.EditNumber;
                    obj.InstallationNotificationRecordId = guId;
                    obj.MachineryInfoId = query.MachineryInfoId;
                    //项目信息
                    obj.BelongedTo = info.BelongedTo;
                    obj.RecordNumber = info.RecordNumber;
                    obj.ProjectName = info.ProjectName;
                    obj.ProjectAddress = info.ProjectAddress;
                    obj.EntCode = info.EntCode;
                    obj.EntName = info.EntName;
                    query.EntCode = info.EntCode;
                    query.EntName = info.EntName;
                    //安装日期
                    obj.PlanInstallDate = info.PlanInstallDate;
                    obj.PlanInstallEndDate = info.PlanInstallEndDate;
                    obj.PlanUseBeginDate = info.PlanUseBeginDate;
                    obj.PlanUseEndDate = info.PlanUseEndDate;
                    //拟拆卸日期
                    obj.PlanDisassembleBeginDate = info.PlanDisassembleBeginDate;
                    obj.PlanDisassembleEndDate = info.PlanDisassembleEndDate;

                    //安装基本信息
                    obj.InstallationPosition = info.InstallationPosition;
                    obj.MachineryPersonId = info.MachineryPersonId;
                    obj.InstallLeader = info.InstallLeader;
                    obj.InstallLeaderTel = info.InstallLeaderTel;
                    obj.SafetyPerson = info.SafetyPerson;
                    obj.SafetyPersonTel = info.SafetyPersonTel;
                    obj.InstallHeight = info.InstallHeight;
                    obj.Remark = info.Remark;
                    obj.Type = info.Type;
                    obj.State = MachineryState.未安装告知.GetHashCode();
                    if (info.Type == 1)
                    {
                        obj.State = MachineryState.办理使用登记通过.GetHashCode();
                    }
                    obj.UpdateDate = DateTime.Now;
                    obj.DeleteMark = 0;
                    obj.AuditStatus=0;
                    query.AuditStatus=0;
                    obj.EntGUID = entGUID;
                    _context.InstallationNotificationRecords.UpdateRange(obj);
                    var peorles = await _context.InstallPeoples
                        .Where(s => s.InstallationNotificationRecordId == info.EditNumber)
                        .ToListAsync();
                    _context.InstallPeoples.RemoveRange(peorles);
                    info.PeopleLists.ForEach(x =>
                    {
                        InstallPeople peoples = new InstallPeople();
                        peoples.InstallationNotificationRecordId = guId;
                        peoples.InstallPeopleId = SecurityManage.GuidUpper();
                        peoples.MachineryPersonId = x.PMachineryPersonId;
                        peoples.Remark = x.PRemark;
                        peoples.Type = x.PType;
                        peoples.CreateDate = DateTime.Now;
                        peoples.MachineryInfoId = info.MachineryInfoId;
                        peoples.IsFree = 1;
                        _context.InstallPeoples.AddAsync(peoples);
                    });
                    var attachments = await _context.RecordAttachments
                        .Where(s => s.AttachmentId == info.EditNumber)
                        .ToListAsync();
                    _context.RecordAttachments.RemoveRange(attachments);
                    info.FileUrls.ForEach(x =>
                    {
                        if (x != null)
                        {
                            RecordAttachment attachment = new RecordAttachment();
                            attachment.RecordAttachmentId = SecurityManage.GuidUpper();
                            attachment.AttachmentId = guId;
                            attachment.Type = RecordConfigTypeEnum.安装_拆卸告知;
                            attachment.FileName = x.FileName;
                            attachment.FileUrl = x.RouteUrl;
                            attachment.RecordConfigId = x.RecordConfigId;
                            attachment.CreateDate = DateTime.Now;
                            _context.RecordAttachments.AddAsync(attachment);
                        }
                    });
                }
                //添加保存
                else
                {
                    installation.InstallationNotificationRecordId = guId;
                    installation.MachineryInfoId = query.MachineryInfoId;
                    //项目信息
                    installation.BelongedTo = info.BelongedTo;
                    installation.RecordNumber = info.RecordNumber;
                    installation.ProjectName = info.ProjectName;
                    installation.ProjectAddress = info.ProjectAddress;
                    installation.EntCode = info.EntCode;
                    installation.EntName = info.EntName;
                    query.EntCode = info.EntCode;
                    query.EntName = info.EntName;
                    //安装日期
                    installation.PlanInstallDate = info.PlanInstallDate;
                    installation.PlanInstallEndDate = info.PlanInstallEndDate;
                    installation.PlanUseBeginDate = info.PlanUseBeginDate;
                    installation.PlanUseEndDate = info.PlanUseEndDate;
                    //拟拆卸日期
                    installation.PlanDisassembleBeginDate = info.PlanDisassembleBeginDate;
                    installation.PlanDisassembleEndDate = info.PlanDisassembleEndDate;

                    //安装基本信息
                    installation.InstallationPosition = info.InstallationPosition;
                    installation.MachineryPersonId = info.MachineryPersonId;
                    installation.InstallLeader = info.InstallLeader;
                    installation.InstallLeaderTel = info.InstallLeaderTel;
                    installation.InstallHeight = info.InstallHeight;
                    installation.Remark = info.Remark;
                    installation.Type = info.Type;
                    installation.SafetyPerson = info.SafetyPerson;
                    installation.SafetyPersonTel = info.SafetyPersonTel;
                    installation.State = MachineryState.未安装告知.GetHashCode();
                    if (info.Type == 1)
                    {
                        installation.State = MachineryState.办理使用登记通过.GetHashCode();
                    }

                    installation.CreateDate = DateTime.Now;
                    installation.UpdateDate = DateTime.Now;
                    installation.DeleteMark = 0;
                    installation.EntGUID = entGUID;
                    await _context.InstallationNotificationRecords.AddAsync(installation);

                    info.PeopleLists.ForEach(x =>
                    {
                        InstallPeople peoples = new InstallPeople();
                        peoples.InstallationNotificationRecordId = guId;
                        peoples.InstallPeopleId = SecurityManage.GuidUpper();
                        peoples.MachineryPersonId = x.PMachineryPersonId;
                        peoples.Remark = x.PRemark;
                        peoples.WorkType = x.PType;
                        peoples.CreateDate = DateTime.Now;
                        peoples.MachineryInfoId = info.MachineryInfoId;
                        peoples.IsFree = 1;
                        _context.InstallPeoples.AddAsync(peoples);
                    });

                    info.FileUrls.ForEach(x =>
                    {
                        if (x != null)
                        {
                            RecordAttachment attachment = new RecordAttachment();
                            attachment.RecordAttachmentId = SecurityManage.GuidUpper();
                            attachment.AttachmentId = guId;
                            attachment.Type = RecordConfigTypeEnum.安装_拆卸告知;
                            attachment.FileName = x.FileName;
                            attachment.FileUrl = x.RouteUrl;
                            attachment.RecordConfigId = x.RecordConfigId;
                            attachment.CreateDate = DateTime.Now;
                            _context.RecordAttachments.AddAsync(attachment);
                        }
                    });
                }
        ; _context.MachineryInfos.UpdateRange(query);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, guId);
            }
            catch (Exception ex)
            {

                _logger.LogError("保存申请失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }

        }

        /// <summary>
        /// 提交申请
        /// </summary>
        /// <param name="installationNotificationRecordId"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> SubmitApplication([FromBody] SubmitApplicationClass info)
        {
            try
            {
                var query1 = await _context.InstallationNotificationRecords
              .Where(s => s.InstallationNotificationRecordId == info.InstallationNotificationRecordId
              && s.Type == info.Type && s.AuditStatus==1)
              .FirstOrDefaultAsync();
                if (true)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "该记录使用单位已经审核，无法撤回！");
                }
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;//后台账号登录   
                var type = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//0项目登录 1企业登录
                if (type != "1")
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                if (info.RouteUrl == null && info.Type == 0)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "未上传安装告知申请表");
                }
                if (info.RouteUrl == null && info.Type == 1)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "未上传拆卸告知申请表");
                }
                var fujian = await _context.RecordAttachments
                    .Where(w => w.RecordConfigId == info.RecordConfigId && w.AttachmentId == info.InstallationNotificationRecordId && w.Type == RecordConfigTypeEnum.安装_拆卸告知)
                    .ToListAsync();
                fujian.ForEach(x =>
                {
                    x.DeleteMark = 1;
                });
                _context.RecordAttachments.UpdateRange(fujian);
                if (info.Type == 0)
                {
                    List<RecordAttachment> list = new List<RecordAttachment>();
                    info.RouteUrl.ForEach(x =>
                    {
                        RecordAttachment attachment = new RecordAttachment();
                        attachment.RecordAttachmentId = SecurityManage.GuidUpper();
                        attachment.AttachmentId = info.InstallationNotificationRecordId;
                        attachment.Type = RecordConfigTypeEnum.安装_拆卸告知;
                        attachment.FileName = "安装告知表";
                        attachment.FileUrl = x;
                        attachment.RecordConfigId = info.RecordConfigId;
                        attachment.CreateDate = DateTime.Now;
                        list.Add(attachment);
                    });

                    await _context.RecordAttachments.AddRangeAsync(list);
                }
                else
                {
                    List<RecordAttachment> list = new List<RecordAttachment>();
                    info.RouteUrl.ForEach(x =>
                    {
                        RecordAttachment attachment = new RecordAttachment();
                        attachment.RecordAttachmentId = SecurityManage.GuidUpper();
                        attachment.AttachmentId = info.InstallationNotificationRecordId;
                        attachment.Type = RecordConfigTypeEnum.安装_拆卸告知;
                        attachment.FileName = "拆卸告知表";
                        attachment.FileUrl = x;
                        attachment.RecordConfigId = info.RecordConfigId;
                        attachment.CreateDate = DateTime.Now;
                        list.Add(attachment);
                    });

                    await _context.RecordAttachments.AddRangeAsync(list);
                }


                var userID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var query = await _context.InstallationNotificationRecords
                .Where(s => s.InstallationNotificationRecordId == info.InstallationNotificationRecordId
                && s.Type == info.Type)
                .FirstOrDefaultAsync();

                var machinery = await _context.MachineryInfos
                    .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                    .FirstOrDefaultAsync();
                if (machinery.CheckState == MachineryState.未安装告知
                    || machinery.CheckState == MachineryState.安装告知审核不通过
                    || machinery.CheckState == MachineryState.使用登记注销审核通过
                    && info.Type == 0)
                {
                    query.State = 1;
                    query.AuditStatus=0;
                    machinery.AuditStatus=0;
                    machinery.BelongedTo = query.BelongedTo;
                    machinery.RecordNumber = query.RecordNumber;
                    machinery.EntCode = query.EntCode;
                    machinery.EntName = query.EntName;
                    machinery.ProjectName = query.ProjectName;
                    machinery.CheckState = MachineryState.安装告知审核中;
                    machinery.InstallSubmitDate = DateTime.Now;
                    machinery.AnChaiEntUuid=uuid;
                    query.SubmitUserId = userID;
                }
                else if (machinery.CheckState == MachineryState.办理使用登记通过
                    || machinery.CheckState == MachineryState.拆卸告知审核不通过
                    && info.Type == 1)
                {
                    query.State = 11;
                    machinery.CheckState = MachineryState.拆卸告知审核中;
                    query.AuditStatus=0;
                    machinery.AuditStatus=0;
                    machinery.UninstallSubmitDate = DateTime.Now;
                    query.SubmitUserId = userID;
                }
                else
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "机械正在使用,无法提交申请");
                }
                query.UpdateDate = DateTime.Now;
                query.SubmitDate = DateTime.Now;
                query.ReviewBelongedTo = query.BelongedTo;
                _context.InstallationNotificationRecords.UpdateRange(query);
                _context.MachineryInfos.UpdateRange(machinery);

                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, "告知申请已提交，请等待受理");
            }
            catch (Exception ex)
            {

                _logger.LogError("提交告知申请失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }



        }



        /// <summary>
        /// 提交申请-监理单位
        /// </summary>
        /// <param name="installationNotificationRecordId"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> SubmitApplicationJL([FromBody] SubmitApplicationClass info)
        {
            try
            {
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;//后台账号登录   
                var type = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//0项目登录 1企业登录
                if (type != "1")
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                if (info.RouteUrl == null && info.Type == 0)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "未上传安装告知申请表");
                }
                if (info.RouteUrl == null && info.Type == 1)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "未上传拆卸告知申请表");
                }
                var fujian = await _context.RecordAttachments
                    .Where(w => w.RecordConfigId == info.RecordConfigId && w.AttachmentId == info.InstallationNotificationRecordId && w.Type == RecordConfigTypeEnum.安装_拆卸告知)
                    .ToListAsync();
                fujian.ForEach(x =>
                {
                    x.DeleteMark = 1;
                });
                _context.RecordAttachments.UpdateRange(fujian);
                if (info.Type == 0)
                {
                    List<RecordAttachment> list = new List<RecordAttachment>();
                    info.RouteUrl.ForEach(x =>
                    {
                        RecordAttachment attachment = new RecordAttachment();
                        attachment.RecordAttachmentId = SecurityManage.GuidUpper();
                        attachment.AttachmentId = info.InstallationNotificationRecordId;
                        attachment.Type = RecordConfigTypeEnum.安装_拆卸告知;
                        attachment.FileName = "安装告知表";
                        attachment.FileUrl = x;
                        attachment.RecordConfigId = info.RecordConfigId;
                        attachment.CreateDate = DateTime.Now;
                        list.Add(attachment);
                    });

                    await _context.RecordAttachments.AddRangeAsync(list);
                }
                else
                {
                    List<RecordAttachment> list = new List<RecordAttachment>();
                    info.RouteUrl.ForEach(x =>
                    {
                        RecordAttachment attachment = new RecordAttachment();
                        attachment.RecordAttachmentId = SecurityManage.GuidUpper();
                        attachment.AttachmentId = info.InstallationNotificationRecordId;
                        attachment.Type = RecordConfigTypeEnum.安装_拆卸告知;
                        attachment.FileName = "拆卸告知表";
                        attachment.FileUrl = x;
                        attachment.RecordConfigId = info.RecordConfigId;
                        attachment.CreateDate = DateTime.Now;
                        list.Add(attachment);
                    });

                    await _context.RecordAttachments.AddRangeAsync(list);
                }


                var userID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var query = await _context.InstallationNotificationRecords
                .Where(s => s.InstallationNotificationRecordId == info.InstallationNotificationRecordId
                && s.Type == info.Type)
                .FirstOrDefaultAsync();

                var machinery = await _context.MachineryInfos
                    .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                    .FirstOrDefaultAsync();
                if ( machinery.CheckState == MachineryState.安装告知审核中
                     && query.AuditStatus==1
                    && info.Type == 0)
                {
                    query.State = 1;
                    query.AuditStatus=1;
                    machinery.AuditStatus=AuditStatusEnum.通过;
                    machinery.BelongedTo = query.BelongedTo;
                    machinery.RecordNumber = query.RecordNumber;
                    machinery.EntCode = query.EntCode;
                    machinery.EntName = query.EntName;
                    machinery.ProjectName = query.ProjectName;
                    machinery.CheckState = MachineryState.安装告知审核中;
                    machinery.InstallSubmitDate = DateTime.Now;
                    machinery.AnChaiEntUuid=uuid;
                    query.SubmitUserId = userID;
                }
                else if (machinery.CheckState == MachineryState.拆卸告知审核中
                        && query.AuditStatus==1
                    && info.Type == 1)
                {
                    query.State = 11;
                    machinery.CheckState = MachineryState.拆卸告知审核中;
                    query.AuditStatus=1;
                    machinery.AuditStatus=AuditStatusEnum.通过;
                    machinery.UninstallSubmitDate = DateTime.Now;
                    query.SubmitUserId = userID;
                }
                else
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "机械正在使用,无法提交申请");
                }
                query.UpdateDate = DateTime.Now;
                query.SubmitDate = DateTime.Now;
                query.ReviewBelongedTo = query.BelongedTo;
                _context.InstallationNotificationRecords.UpdateRange(query);
                _context.MachineryInfos.UpdateRange(machinery);

                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, "告知申请已提交，请等待受理");
            }
            catch (Exception ex)
            {

                _logger.LogError("提交告知申请失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }



        }

        /// <summary>
        /// 获取审核列表
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="machineryType"></param>
        /// <param name="machineryName"></param>
        /// <param name="machineryModel"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <param name="type"></param>
        /// <param name="applyState"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="anzhuangUnit">安装单位搜索条件</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ToExamineInformation>>> GetToExamineInformation(string recordNumber, string leaveTheFactoryNo, string propertyRightsRecordNo, string projectName, string machineryType,
            string machineryName, string machineryModel, DateTime? beginTime, DateTime? endTime, int type, string applyState, int pageIndex, int pageSize, string anzhuangUnit)
        {
            try
            {
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//操作人科室

                var data = from A in _context.InstallationNotificationRecords
                           join B in _context.MachineryInfos
                           on A.MachineryInfoId equals B.MachineryInfoId
                           into t1
                           from r in t1.DefaultIfEmpty()
                           join C in _context.EntRegisterInfoMag on A.EntGUID equals C.EntRegisterInfoMagId
                           where A.Type == type && A.DeleteMark == 0 && A.BelongedTo == belongedTo && r.DeleteMark == 0
                           select new ToExamineInformation
                           {
                               MachineryInfoId = r.MachineryInfoId,
                               InstallationNotificationRecordId = A.InstallationNotificationRecordId,
                               UpdateTime = A.SubmitDate,
                               RecordNumber = A.RecordNumber,
                               BelongedTo = A.BelongedTo,
                               ProjectName = A.ProjectName,
                               MachineryType = r.MachineryType.ToString(),
                               MachineryModel = r.MachineryModel,
                               MachineryName = r.MachineryName,
                               PropertyRightsRecordNo = r.PropertyRightsRecordNo,
                               MaxRatedLiftingCapacity = r.MaxRatedLiftingCapacity,
                               Knm = r.Knm,
                               MaxInstallHeight = r.MaxInstallHeight,
                               MaxRange = r.MaxRange,
                               BuyDate = r.BuyDate,
                               ApplyDate = A.UpdateDate,
                               EntName = C.EntName,
                               State = A.State,
                               SubmitDate = r.InstallSubmitDate,
                               SubmitDate1 = r.UninstallSubmitDate,
                               IsExamineState = "1",
                               Reson = A.Reason,
                               InstallationPosition = A.InstallationPosition,
                               LeaveTheFactoryNo = r.LeaveTheFactoryNo,
                               UpdateDate = A.UpdateDate,
                               ChanQuanEntGUID=r.EntGUID,

                           };
                if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                {
                    data = from A in data
                           join B in _context.ProjectOverview
                           on new { A.BelongedTo, A.RecordNumber } equals new { B.BelongedTo, B.RecordNumber }
                           where B.SupervisionDepartmentId == supervisionDepartmentId
                           select A;
                }


                if (!string.IsNullOrWhiteSpace(anzhuangUnit))
                {
                    data = data.Where(s => s.EntName.Contains(anzhuangUnit));
                }


                if (applyState == "0")
                {
                    data = data.Where(s => s.State == 1 || s.State == 11);
                }
                if (applyState == "1")
                {
                    data = data.Where(s => s.State != 1 && s.State != 11 && s.State != 0 && s.State != 10);
                }

                IEnumerable<ToExamineInformation> personIEn = data;
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.ProjectName) && s.ProjectName.Contains(projectName));
                }
                if (!string.IsNullOrWhiteSpace(machineryType))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryType) && s.MachineryType == machineryType);
                }
                if (!string.IsNullOrWhiteSpace(machineryName))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryName) && s.MachineryName.Contains(machineryName));
                }
                if (!string.IsNullOrWhiteSpace(machineryModel))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryModel) && s.MachineryModel.Contains(machineryModel));
                }
                if (beginTime != null && endTime != null)
                {

                    personIEn = personIEn
                       .Where(s => s.UpdateDate != null
                       && s.UpdateDate > beginTime && s.UpdateDate < ((DateTime)endTime).AddDays(1));
                }
                if (!string.IsNullOrWhiteSpace(belongedTo))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.BelongedTo) && s.BelongedTo.Contains(belongedTo));
                }
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.LeaveTheFactoryNo) && s.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.PropertyRightsRecordNo) && s.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.RecordNumber) && s.RecordNumber.Contains(recordNumber));
                }
                int queryCount = personIEn.Count();
                var result = personIEn.OrderByDescending(t => t.UpdateTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                result.ForEach(x =>
                {
                    if (type == 1)
                    {
                        x.SubmitDate = x.SubmitDate1;
                    }
                    if (x.State == 1 || x.State == 11)
                    {
                        x.IsExamineState = "0";
                    }
                    x.ChanQuanEntName = _context.EntRegisterInfoMag.Where(s => s.EntRegisterInfoMagId == x.ChanQuanEntGUID).Select(s => s.EntName).FirstOrDefault();
                });
                return ResponseViewModel<List<ToExamineInformation>>.Create(Status.SUCCESS, Message.SUCCESS, result, queryCount);

            }
            catch (Exception ex)
            {

                _logger.LogError("获取告知列表失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<ToExamineInformation>>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        ///使用单位获取安装告知列表
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="machineryType"></param>
        /// <param name="machineryName"></param>
        /// <param name="machineryModel"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <param name="type"></param>
        /// <param name="applyState"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="anzhuangUnit">安装单位搜索条件</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ToExamineInformation>>> GetProToExamineInformation(string recordNumber, string leaveTheFactoryNo, string propertyRightsRecordNo, string projectName, string machineryType,
            string machineryName, string machineryModel, DateTime? beginTime, DateTime? endTime, int type, string applyState, int pageIndex, int pageSize, string anzhuangUnit, string belongedTo)
        {
            try
            {
                //var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//操作人科室

                var data = from A in _context.InstallationNotificationRecords
                           join B in _context.MachineryInfos
                           on A.MachineryInfoId equals B.MachineryInfoId
                           into t1
                           from r in t1.DefaultIfEmpty()
                           join C in _context.EntRegisterInfoMag on A.EntGUID equals C.EntRegisterInfoMagId
                           where A.Type == type && A.DeleteMark == 0 && A.BelongedTo == belongedTo && A.RecordNumber==recordNumber && r.DeleteMark == 0 && A.State!=0 && A.State!=10
                           select new ToExamineInformation
                           {
                               MachineryInfoId = r.MachineryInfoId,
                               InstallationNotificationRecordId = A.InstallationNotificationRecordId,
                               UpdateTime = A.SubmitDate,
                               RecordNumber = A.RecordNumber,
                               BelongedTo = A.BelongedTo,
                               ProjectName = A.ProjectName,
                               MachineryType = r.MachineryType.ToString(),
                               MachineryModel = r.MachineryModel,
                               MachineryName = r.MachineryName,
                               PropertyRightsRecordNo = r.PropertyRightsRecordNo,
                               MaxRatedLiftingCapacity = r.MaxRatedLiftingCapacity,
                               Knm = r.Knm,
                               MaxInstallHeight = r.MaxInstallHeight,
                               MaxRange = r.MaxRange,
                               BuyDate = r.BuyDate,
                               ApplyDate = A.UpdateDate,
                               EntName = C.EntName,
                               State = A.State,
                               SubmitDate = r.InstallSubmitDate,
                               SubmitDate1 = r.UninstallSubmitDate,
                               IsExamineState = "0",
                               Reson = A.Reason,
                               InstallationPosition = A.InstallationPosition,
                               LeaveTheFactoryNo = r.LeaveTheFactoryNo,
                               UpdateDate = A.UpdateDate,
                               ChanQuanEntGUID=r.EntGUID,
                               IsAffirm="0",
                               AuditStatus=A.AuditStatus
                           };
                if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                {
                    data = from A in data
                           join B in _context.ProjectOverview
                           on new { A.BelongedTo, A.RecordNumber } equals new { B.BelongedTo, B.RecordNumber }
                           where B.SupervisionDepartmentId == supervisionDepartmentId
                           select A;
                }


                if (!string.IsNullOrWhiteSpace(anzhuangUnit))
                {
                    data = data.Where(s => s.EntName.Contains(anzhuangUnit));
                }


                if (applyState == "0")
                {
                    data = data.Where(s => s.AuditStatus == 0 /*&& (s.State==1 || s.State == 11)*/);
                }
                if (applyState == "1")
                {
                    data = data.Where(s => s.AuditStatus == 1 || (s.AuditStatus == 5 && (s.State == 2 || s.State == 12)) || (s.AuditStatus == 6 && (s.State == 2 || s.State == 12)));
                }
                if (applyState == "2")
                {
                    data = data.Where(s => s.AuditStatus == 2  || (s.AuditStatus == 5 && (s.State == 3 || s.State == 13)) || (s.AuditStatus == 6 && (s.State == 3 || s.State == 13)) || (s.AuditStatus == 7 && (s.State == 3 || s.State == 13)));
                }
                if (applyState == "3")
                {
                    data = data.Where(s => s.AuditStatus == 3 /*&& s.State != 1 && s.State != 11 && s.State != 0 && s.State != 10*/);
                }
                if (applyState == "4")
                {
                    data = data.Where(s => s.AuditStatus == 4 /*&& s.State != 1 && s.State != 11 && s.State != 0 && s.State != 10*/);
                }
                IEnumerable<ToExamineInformation> personIEn = data;
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.ProjectName) && s.ProjectName.Contains(projectName));
                }
                if (!string.IsNullOrWhiteSpace(machineryType))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryType) && s.MachineryType == machineryType);
                }
                if (!string.IsNullOrWhiteSpace(machineryName))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryName) && s.MachineryName.Contains(machineryName));
                }
                if (!string.IsNullOrWhiteSpace(machineryModel))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryModel) && s.MachineryModel.Contains(machineryModel));
                }
                if (beginTime != null && endTime != null)
                {

                    personIEn = personIEn
                       .Where(s => s.UpdateDate != null
                       && s.UpdateDate > beginTime && s.UpdateDate < ((DateTime)endTime).AddDays(1));
                }
                if (!string.IsNullOrWhiteSpace(belongedTo))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.BelongedTo) && s.BelongedTo.Contains(belongedTo));
                }
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.LeaveTheFactoryNo) && s.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.PropertyRightsRecordNo) && s.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.RecordNumber) && s.RecordNumber.Contains(recordNumber));
                }
                int queryCount = personIEn.Count();
                var result = personIEn.OrderByDescending(t => t.UpdateTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                result.ForEach(x =>
                {
                    if (type == 1)
                    {
                        x.SubmitDate = x.SubmitDate1;
                    }
                    if (x.State == 1 || x.State == 11)
                    {
                        x.IsAffirm="1";
                        //x.IsExamineState = "0";
                    }
                    x.ChanQuanEntName = _context.EntRegisterInfoMag.Where(s => s.EntRegisterInfoMagId == x.ChanQuanEntGUID).Select(s => s.EntName).FirstOrDefault();
                });
                return ResponseViewModel<List<ToExamineInformation>>.Create(Status.SUCCESS, Message.SUCCESS, result, queryCount);

            }
            catch (Exception ex)
            {

                _logger.LogError("获取告知列表失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<ToExamineInformation>>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        ///监理单位获取安装拆卸列表
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="machineryType"></param>
        /// <param name="machineryName"></param>
        /// <param name="machineryModel"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <param name="type"></param>
        /// <param name="applyState"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="anzhuangUnit">安装单位搜索条件</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ToExamineInformation>>> GetJianLiToExamineInformation(string recordNumber, string leaveTheFactoryNo, string propertyRightsRecordNo, string projectName, string machineryType,
           string machineryName, string machineryModel, DateTime? beginTime, DateTime? endTime, int type, string applyState, int pageIndex, int pageSize, string anzhuangUnit, string entCode)
        {
            try
            {
                var lgbelongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;//操作人科室

                var danwei = new List<EntInfo>();
                if (entCode!=null)
                {
                    danwei = _context.ProjectEntSnapshot.Where(w => w.EnterpriseType=="监理单位" && w.MainUnit=="是" && w.OrganizationCode==entCode)
                                     .Select(s => new EntInfo { BelongedTo=s.BelongedTo, RecordNumber=s.RecordNumber }).ToList();

                }
                else
                {
                    danwei=_context.ProjectEntSnapshot.Where(w => w.EnterpriseType=="监理单位" && w.MainUnit=="是" && w.BelongedTo== lgbelongedTo)
                                                      .Select(s => new EntInfo { BelongedTo=s.BelongedTo, RecordNumber=s.RecordNumber }).ToList();
                }
                //var danwei = _context.ProjectEntSnapshot.Where(w => w.EnterpriseType=="监理单位" && w.MainUnit=="是" && w.OrganizationCode==entCode).Select(s=>new {s.BelongedTo,s.RecordNumber }).ToList();
                //var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//操作人科室
                var recordnumer = danwei.Select(s => s.RecordNumber).Distinct().ToList();
                var data = from A in _context.InstallationNotificationRecords
                           join B in _context.MachineryInfos
                           on A.MachineryInfoId equals B.MachineryInfoId
                           into t1
                           from r in t1.DefaultIfEmpty()
                           join C in _context.EntRegisterInfoMag on A.EntGUID equals C.EntRegisterInfoMagId
                           where A.Type == type && A.DeleteMark == 0 && recordnumer.Contains(A.RecordNumber) && r.DeleteMark == 0
                           select new ToExamineInformation
                           {
                               MachineryInfoId = r.MachineryInfoId,
                               InstallationNotificationRecordId = A.InstallationNotificationRecordId,
                               UpdateTime = A.SubmitDate,
                               RecordNumber = A.RecordNumber,
                               BelongedTo = A.BelongedTo,
                               ProjectName = A.ProjectName,
                               MachineryType = r.MachineryType.ToString(),
                               MachineryModel = r.MachineryModel,
                               MachineryName = r.MachineryName,
                               PropertyRightsRecordNo = r.PropertyRightsRecordNo,
                               MaxRatedLiftingCapacity = r.MaxRatedLiftingCapacity,
                               Knm = r.Knm,
                               MaxInstallHeight = r.MaxInstallHeight,
                               MaxRange = r.MaxRange,
                               BuyDate = r.BuyDate,
                               ApplyDate = A.UpdateDate,
                               EntName = C.EntName,
                               State = A.State,
                               SubmitDate = r.InstallSubmitDate,
                               SubmitDate1 = r.UninstallSubmitDate,
                               IsExamineState = "1",
                               Reson = A.Reason,
                               InstallationPosition = A.InstallationPosition,
                               LeaveTheFactoryNo = r.LeaveTheFactoryNo,
                               UpdateDate = A.UpdateDate,
                               ChanQuanEntGUID=r.EntGUID,
                               AuditStatus=A.AuditStatus

                           };
                if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                {
                    data = from A in data
                           join B in _context.ProjectOverview
                           on new { A.BelongedTo, A.RecordNumber } equals new { B.BelongedTo, B.RecordNumber }
                           where B.SupervisionDepartmentId == supervisionDepartmentId
                           select A;
                }


                if (!string.IsNullOrWhiteSpace(anzhuangUnit))
                {
                    data = data.Where(s => s.EntName.Contains(anzhuangUnit));
                }


                if (applyState == "0")
                {
                    data = data.Where(s => (s.State == 1 || s.State == 11) && (s.AuditStatus == 1));
                }
                if (applyState == "1")
                {
                    data = data.Where(s => s.State != 1 && s.State != 11 && s.State != 0 && s.State != 10 && (s.AuditStatus==5 || s.AuditStatus==6 || s.AuditStatus==2 || s.AuditStatus==7));
                }

                IEnumerable<ToExamineInformation> personIEn = data;
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.ProjectName) && s.ProjectName.Contains(projectName));
                }
                if (!string.IsNullOrWhiteSpace(machineryType))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryType) && s.MachineryType == machineryType);
                }
                if (!string.IsNullOrWhiteSpace(machineryName))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryName) && s.MachineryName.Contains(machineryName));
                }
                if (!string.IsNullOrWhiteSpace(machineryModel))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryModel) && s.MachineryModel.Contains(machineryModel));
                }
                if (beginTime != null && endTime != null)
                {

                    personIEn = personIEn
                       .Where(s => s.UpdateDate != null
                       && s.UpdateDate > beginTime && s.UpdateDate < ((DateTime)endTime).AddDays(1));
                }
                //if (!string.IsNullOrWhiteSpace(belongedTo))
                //{
                //    personIEn = personIEn
                //        .Where(s => !string.IsNullOrWhiteSpace(s.BelongedTo) && s.BelongedTo.Contains(belongedTo));
                //}
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.LeaveTheFactoryNo) && s.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.PropertyRightsRecordNo) && s.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.RecordNumber) && s.RecordNumber.Contains(recordNumber));
                }
                int queryCount = personIEn.Count();
                var result = personIEn.OrderByDescending(t => t.UpdateTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                result.ForEach(x =>
                {
                    if (type == 1)
                    {
                        x.SubmitDate = x.SubmitDate1;
                    }
                    if (x.State == 1 || x.State == 11)
                    {
                        x.IsExamineState = "0";
                    }
                    x.ChanQuanEntName = _context.EntRegisterInfoMag.Where(s => s.EntRegisterInfoMagId == x.ChanQuanEntGUID).Select(s => s.EntName).FirstOrDefault();
                });
                return ResponseViewModel<List<ToExamineInformation>>.Create(Status.SUCCESS, Message.SUCCESS, result, queryCount);

            }
            catch (Exception ex)
            {

                _logger.LogError("获取告知列表失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<ToExamineInformation>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 主管部门获取安装拆卸列表
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="machineryType"></param>
        /// <param name="machineryName"></param>
        /// <param name="machineryModel"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <param name="type"></param>
        /// <param name="applyState"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="anzhuangUnit">安装单位搜索条件</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ToExamineInformation>>> GetZhuGuanExamineInformation(string recordNumber, string leaveTheFactoryNo, string propertyRightsRecordNo, string projectName, string machineryType,
            string machineryName, string machineryModel, DateTime? beginTime, DateTime? endTime, int type, string applyState, int pageIndex, int pageSize, string anzhuangUnit, string entCode)
        {
            try
            {
                var lgbelongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;//操作人科室

                var danwei = new List<EntInfo>();
                if (entCode!=null)
                {
                    danwei = _context.ProjectEntSnapshot.Where(w => w.EnterpriseType=="监理单位" && w.MainUnit=="是" && w.OrganizationCode==entCode)
                                     .Select(s => new EntInfo { BelongedTo=s.BelongedTo, RecordNumber=s.RecordNumber }).ToList();

                }
                else
                {
                    danwei=_context.ProjectEntSnapshot.Where(w => w.EnterpriseType=="监理单位" && w.MainUnit=="是" && w.BelongedTo== lgbelongedTo)
                                                      .Select(s => new EntInfo { BelongedTo=s.BelongedTo, RecordNumber=s.RecordNumber }).ToList();
                }
                //var danwei = _context.ProjectEntSnapshot.Where(w => w.EnterpriseType=="监理单位" && w.MainUnit=="是" && w.OrganizationCode==entCode).Select(s=>new {s.BelongedTo,s.RecordNumber }).ToList();
                //var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//操作人科室
                var belongedTo = danwei.Select(s => s.BelongedTo).Distinct().ToList();
                var data = from A in _context.InstallationNotificationRecords
                           join B in _context.MachineryInfos
                           on A.MachineryInfoId equals B.MachineryInfoId
                           into t1
                           from r in t1.DefaultIfEmpty()
                           join C in _context.EntRegisterInfoMag on A.EntGUID equals C.EntRegisterInfoMagId
                           where A.Type == type && A.DeleteMark == 0 && belongedTo.Contains(A.BelongedTo) && r.DeleteMark == 0
                           select new ToExamineInformation
                           {
                               MachineryInfoId = r.MachineryInfoId,
                               InstallationNotificationRecordId = A.InstallationNotificationRecordId,
                               UpdateTime = A.SubmitDate,
                               RecordNumber = A.RecordNumber,
                               BelongedTo = A.BelongedTo,
                               ProjectName = A.ProjectName,
                               MachineryType = r.MachineryType.ToString(),
                               MachineryModel = r.MachineryModel,
                               MachineryName = r.MachineryName,
                               PropertyRightsRecordNo = r.PropertyRightsRecordNo,
                               MaxRatedLiftingCapacity = r.MaxRatedLiftingCapacity,
                               Knm = r.Knm,
                               MaxInstallHeight = r.MaxInstallHeight,
                               MaxRange = r.MaxRange,
                               BuyDate = r.BuyDate,
                               ApplyDate = A.UpdateDate,
                               EntName = C.EntName,
                               State = A.State,
                               SubmitDate = r.InstallSubmitDate,
                               SubmitDate1 = r.UninstallSubmitDate,
                               IsExamineState = "1",
                               Reson = A.Reason,
                               InstallationPosition = A.InstallationPosition,
                               LeaveTheFactoryNo = r.LeaveTheFactoryNo,
                               UpdateDate = A.UpdateDate,
                               ChanQuanEntGUID=r.EntGUID,
                               AuditStatus=A.AuditStatus
                           };
                if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                {
                    data = from A in data
                           join B in _context.ProjectOverview
                           on new { A.BelongedTo, A.RecordNumber } equals new { B.BelongedTo, B.RecordNumber }
                           where B.SupervisionDepartmentId == supervisionDepartmentId
                           select A;
                }


                if (!string.IsNullOrWhiteSpace(anzhuangUnit))
                {
                    data = data.Where(s => s.EntName.Contains(anzhuangUnit));
                }


                if (applyState == "0")
                {
                    data = data.Where(s => (s.State == 2 || s.State == 3 || s.State == 12 || s.State == 13) && (s.AuditStatus==5 && s.AuditStatus!=7));
                }
                if (applyState == "1")
                {
                    data = data.Where(s => s.State != 1 && s.State != 11 && s.State != 0 && s.State != 10  && s.AuditStatus!=7 && (s.AuditStatus==6 || s.AuditStatus == 2));
                }

                IEnumerable<ToExamineInformation> personIEn = data;
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.ProjectName) && s.ProjectName.Contains(projectName));
                }
                if (!string.IsNullOrWhiteSpace(machineryType))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryType) && s.MachineryType == machineryType);
                }
                if (!string.IsNullOrWhiteSpace(machineryName))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryName) && s.MachineryName.Contains(machineryName));
                }
                if (!string.IsNullOrWhiteSpace(machineryModel))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.MachineryModel) && s.MachineryModel.Contains(machineryModel));
                }
                if (beginTime != null && endTime != null)
                {

                    personIEn = personIEn
                       .Where(s => s.UpdateDate != null
                       && s.UpdateDate > beginTime && s.UpdateDate < ((DateTime)endTime).AddDays(1));
                }
                //if (!string.IsNullOrWhiteSpace(belongedTo))
                //{
                //    personIEn = personIEn
                //        .Where(s => !string.IsNullOrWhiteSpace(s.BelongedTo) && s.BelongedTo.Contains(belongedTo));
                //}
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.LeaveTheFactoryNo) && s.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.PropertyRightsRecordNo) && s.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    personIEn = personIEn
                        .Where(s => !string.IsNullOrWhiteSpace(s.RecordNumber) && s.RecordNumber.Contains(recordNumber));
                }
                int queryCount = personIEn.Count();
                var result = personIEn.OrderByDescending(t => t.UpdateTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                result.ForEach(x =>
                {
                    if (type == 1)
                    {
                        x.SubmitDate = x.SubmitDate1;
                    }
                    if (x.State == 1 || x.State == 11)
                    {
                        x.IsExamineState = "0";
                    }
                    x.ChanQuanEntName = _context.EntRegisterInfoMag.Where(s => s.EntRegisterInfoMagId == x.ChanQuanEntGUID).Select(s => s.EntName).FirstOrDefault();
                });
                return ResponseViewModel<List<ToExamineInformation>>.Create(Status.SUCCESS, Message.SUCCESS, result, queryCount);

            }
            catch (Exception ex)
            {

                _logger.LogError("获取告知列表失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<ToExamineInformation>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 保存审核情况
        /// </summary>
        /// <param name="state"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<string>> SaveExamine(string installationNotificationRecordId, int type, int state, string remark)
        {
            try
            {
                var userID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var entType = User.FindFirst(nameof(ClaimTypeEnum.EntType))?.Value;

                var query = await _context.InstallationNotificationRecords
                    .Where(s => s.InstallationNotificationRecordId == installationNotificationRecordId)
                    .FirstOrDefaultAsync();
                var query1 = await _context.MachineryInfos
                    .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                    .FirstOrDefaultAsync();
                var query2 = await _context.InstallPeoples
                    .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                    .ToListAsync();

                var query3 = await _context.CheckRecords
                  .Where(s => s.MachineryInfoId == query.MachineryInfoId && s.DeleteMark == 0)
                  .ToListAsync();

                var now = DateTime.Now;
                if (type == 0)
                {

                    if (state == 0)
                    {
                        query.State = 2;
                        query.AuditStatus=5;
                        query.UpdateDate = now;
                        query1.AuditStatus=AuditStatusEnum.监理单位已审核;
                        query1.CheckState = MachineryState.安装告知审核通过;
                    }
                    else
                    {
                        if (entType=="安拆单位")
                        {
                            query.State = 3;
                            query.AuditStatus=2;
                            query.UpdateDate = now;
                            query.Reason = remark;
                            query1.CheckState = MachineryState.安装告知审核不通过;
                            query1.AuditStatus=AuditStatusEnum.已退回;
                        }
                        if (entType=="监理单位")
                        {
                            query.State = 3;
                            query.AuditStatus=7;
                            query.UpdateDate = now;
                            query.Reason = remark;
                            query1.CheckState = MachineryState.安装告知审核不通过;
                            query1.AuditStatus=AuditStatusEnum.监理单位不通过;
                        }
                    }
                    query1.InstallReviewDate = now;
                    query.ReviewUserId = userID;
                }
                if (type == 1)
                {
                    if (state == 0)
                    {
                        query.State = 12;
                        query.UpdateDate = DateTime.Now;
                        query1.UseRecordNo = null;
                        query1.RegistrationOfUseId = null;
                        query1.CheckRecordId = null;
                        query1.TestingInstituteInfoId = null;
                        query1.LetterOfCommitmentId = null;
                        query1.MachineryCheckState = MachineryCheckStateEnum.未检测;
                        query.AuditStatus=5;
                        query1.CheckState = MachineryState.使用登记注销审核通过;
                        query1.AuditStatus = AuditStatusEnum.监理单位已审核;

                        query2.ForEach(x =>
                        {
                            //x.DeleteMark = 1;
                            x.IsFree = 0;
                        });
                        query3.ForEach(x =>
                        {
                            x.DeleteMark = 1;
                        });
                        _context.CheckRecords.UpdateRange(query3);
                    }
                    else
                    {
                        if (entType=="安拆单位")
                        {
                            query.State = 13;
                            query.AuditStatus=2;
                            query.UpdateDate = DateTime.Now;
                            query.Reason = remark;
                            query1.CheckState = MachineryState.拆卸告知审核不通过;
                            query1.AuditStatus=AuditStatusEnum.已退回;
                        }
                        if (entType=="监理单位")
                        {
                            query.State = 13;
                            query.AuditStatus=7;
                            query.UpdateDate = DateTime.Now;
                            query.Reason = remark;
                            query1.CheckState = MachineryState.拆卸告知审核不通过;
                            query1.AuditStatus=AuditStatusEnum.监理单位不通过;
                        }

                    }
                    query1.UninstallReviewDate = DateTime.Now;
                    query.ReviewUserId = userID;
                }
                _context.InstallationNotificationRecords.UpdateRange(query);
                _context.MachineryInfos.UpdateRange(query1);
                _context.InstallPeoples.UpdateRange(query2);

                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "提交审核成功！");
            }
            catch (Exception ex)
            {

                _logger.LogError("保存告知失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }
        /// <summary>
        /// 主管部门保存审核情况
        /// </summary>
        /// <param name="state"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<string>> SaveZGExamine(string installationNotificationRecordId, int type, int state, string remark)
        {
            try
            {
                var userID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var query = await _context.InstallationNotificationRecords
                    .Where(s => s.InstallationNotificationRecordId == installationNotificationRecordId)
                    .FirstOrDefaultAsync();
                var query1 = await _context.MachineryInfos
                    .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                    .FirstOrDefaultAsync();
                var query2 = await _context.InstallPeoples
                    .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                    .ToListAsync();

                var query3 = await _context.CheckRecords
                  .Where(s => s.MachineryInfoId == query.MachineryInfoId && s.DeleteMark == 0)
                  .ToListAsync();

                var now = DateTime.Now;
                if (type == 0)
                {
                    if (state == 0)
                    {
                        query.State = 2;
                        query.AuditStatus=6;
                        query.UpdateDate = now;
                        query1.CheckState = MachineryState.安装告知审核通过;
                        query1.AuditStatus=AuditStatusEnum.主管部门已审核;
                    }
                    else
                    {
                        query.State = 3;
                        query.AuditStatus=2;
                        query.UpdateDate = now;
                        query.Reason = remark;
                        query1.CheckState = MachineryState.安装告知审核不通过;
                        query1.AuditStatus=AuditStatusEnum.已退回;
                    }
                    query1.InstallReviewDate = now;
                    query.ReviewUserId = userID;
                }
                if (type == 1)
                {
                    if (state == 0)
                    {
                        query.State = 12;
                        query.UpdateDate = DateTime.Now;
                        query1.UseRecordNo = null;
                        query1.RegistrationOfUseId = null;
                        query1.CheckRecordId = null;
                        query1.TestingInstituteInfoId = null;
                        query1.LetterOfCommitmentId = null;
                        query1.MachineryCheckState = MachineryCheckStateEnum.未检测;
                        query.AuditStatus=6;
                        query1.CheckState = MachineryState.使用登记注销审核通过;
                        query1.AuditStatus=AuditStatusEnum.主管部门已审核;
                        query2.ForEach(x =>
                        {
                            //x.DeleteMark = 1;
                            x.IsFree = 1;
                        });
                        query3.ForEach(x =>
                        {
                            x.DeleteMark = 1;
                        });
                        _context.CheckRecords.UpdateRange(query3);
                    }
                    else
                    {
                        query.State = 13;
                        query.AuditStatus=2;
                        query.UpdateDate = DateTime.Now;
                        query.Reason = remark;
                        query1.CheckState = MachineryState.拆卸告知审核不通过;
                        query1.AuditStatus=AuditStatusEnum.已退回;
                    }
                    query1.UninstallReviewDate = DateTime.Now;
                    query.ReviewUserId = userID;
                }
                _context.InstallationNotificationRecords.UpdateRange(query);
                _context.MachineryInfos.UpdateRange(query1);
                _context.InstallPeoples.UpdateRange(query2);

                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "提交审核成功！");
            }
            catch (Exception ex)
            {

                _logger.LogError("保存告知失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 使用单位确认安装告知情况
        /// </summary>
        /// <param name="installationNotificationRecordId"></param>
        /// <param name="type">安装 0 拆卸 1</param>
        /// <param name="state">确认 0 退回 1  撤回 2</param>
        /// <param name="remark"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<string>> SaveProExamine(string installationNotificationRecordId, int type, int state, string remark)
        {
            try
            {
                var userID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var query = await _context.InstallationNotificationRecords
                    .Where(s => s.InstallationNotificationRecordId == installationNotificationRecordId)
                    .FirstOrDefaultAsync();
                if (query.State==0)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR,"该申请记录已被撤回，无法确认！");
                }
                var query1 = await _context.MachineryInfos
                    .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                    .FirstOrDefaultAsync();
                //var query2 = await _context.InstallPeoples
                //    .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                //    .ToListAsync();

                var query3 = await _context.CheckRecords
                  .Where(s => s.MachineryInfoId == query.MachineryInfoId && s.DeleteMark == 0)
                  .ToListAsync();

                var now = DateTime.Now;
                if (type == 0)
                {
                    if (state == 0)
                    {
                        query.AuditStatus = 1;
                        //query.State = now;
                        query1.AuditStatus = AuditStatusEnum.通过;
                    }
                    else if (state==2)
                    {
                        query.AuditStatus = 3;
                        //query.UpdateDate = now;
                        query1.AuditStatus = AuditStatusEnum.已撤回;
                    }
                    else
                    {
                        query.AuditStatus = 8;
                        query.State = 3;
                        query1.CheckState = MachineryState.安装告知审核不通过;
                        query1.AuditStatus = AuditStatusEnum.使用单位退回;
                    }
                    //query1.InstallReviewDate = now;
                    //query.ReviewUserId = userID;
                }
                if (type == 1)
                {
                    if (state == 0)
                    {
                        query.AuditStatus = 1;
                        //query.UpdateDate = DateTime.Now;
                        //query1.UseRecordNo = null;
                        //query1.RegistrationOfUseId = null;
                        //query1.CheckRecordId = null;
                        //query1.TestingInstituteInfoId = null;
                        //query1.LetterOfCommitmentId = null;
                        //query1.MachineryCheckState = MachineryCheckStateEnum.未检测;

                        query1.AuditStatus = AuditStatusEnum.通过;
                        //query2.ForEach(x =>
                        //{
                        //    //x.DeleteMark = 1;
                        //    x.IsFree = 1;
                        //});
                        //query3.ForEach(x =>
                        //{
                        //    x.DeleteMark = 1;
                        //});
                        //_context.CheckRecords.UpdateRange(query3);
                    }
                    else
                    {
                        query.AuditStatus = 8;
                        query.State = 13;
                        query1.CheckState = MachineryState.拆卸告知审核不通过;
                        query1.AuditStatus = AuditStatusEnum.使用单位退回;
                    }
                    //query1.UninstallReviewDate = DateTime.Now;
                    //query.ReviewUserId = userID;
                }
                _context.InstallationNotificationRecords.UpdateRange(query);
                _context.MachineryInfos.UpdateRange(query1);
                //_context.InstallPeoples.UpdateRange(query2);

                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "确认成功！");
            }
            catch (Exception ex)
            {

                _logger.LogError("保存告知失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 撤销申请
        /// </summary>
        /// <param name="installationNotificationRecordId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> RevokeApplication(string installationNotificationRecordId, int type)
        {
            try
            {
                var query = await _context.InstallationNotificationRecords
                .Where(s => s.InstallationNotificationRecordId == installationNotificationRecordId
                && s.Type == type)
                .FirstOrDefaultAsync();
                if (query.AuditStatus==1)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR,"项目上已经确认该设备申请，无法撤回申请！");
                }
                var machinery = await _context.MachineryInfos
                    .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                    .FirstOrDefaultAsync();
                if (query.State == 1 && type == 0)
                {
                    query.State = 0;
                    machinery.BelongedTo = "";
                    machinery.RecordNumber = "";
                    machinery.EntCode = "";
                    machinery.EntName = "";
                    machinery.CheckState = MachineryState.未安装告知;
                }
                else if (query.State == 11 && type == 1)
                {
                    query.State = 10;
                    machinery.CheckState = MachineryState.办理使用登记通过;
                }
                else
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "该申请状态不是告知申请中不能撤销！");
                }
                _context.InstallationNotificationRecords.UpdateRange(query);
                _context.MachineryInfos.UpdateRange(machinery);
                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "撤销成功");
            }
            catch (Exception ex)
            {

                _logger.LogError("提交申请失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }

        }
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="iform"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseViewModel<string> UploadFileList([FromForm] IFormCollection iform)
        {
            try
            {

                var url = Util.UploadFileToServer(iform.Files[0], _environment, Request, "RecordAttachment");

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, url);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取配置文件信息：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 下载安装告知申请表
        /// </summary>
        /// <param name="installationNotificationRecordId"></param>
        /// <returns></returns>
        public async Task<ResponseViewModel<string>> GetInstallationApplication(string installationNotificationRecordId, int type)
        {
            try
            {
                var query = await _context.InstallationNotificationRecords
                .Where(s => s.InstallationNotificationRecordId == installationNotificationRecordId && s.DeleteMark == 0)
                .FirstOrDefaultAsync();
                if (query == null)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "参数错误！");
                }
                var query1 = await _context.MachineryInfos
                    .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                    .FirstOrDefaultAsync();
                Dictionary<int, SpecialWorker> aa = Worker.WorkerDic;
                //var data1 = aa.Where(a => a.Value.IsSpecial == true).Select(a => new WorkTypeCodeViewModel { Name = a.Value.Value, key = a.Key }).ToList();
                var cityZone = await _context.CityZone.Where(x => x.BelongedTo == query.BelongedTo).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                var number = await _context.MachineryInfoSupplementaryInformations.Where(w => w.MachineryInfoId==query.MachineryInfoId && w.DeleteMark==0).FirstOrDefaultAsync();
                var list = await (from A in _context.InstallPeoples
                                  join B in _context.MachineryPeoples on A.MachineryPersonId equals B.MachineryPersonId
                                  where A.InstallationNotificationRecordId == installationNotificationRecordId
                                  select new Person
                                  {
                                      Name = B.PersonName,
                                      WorkCode = B.WorkTypeCode, //data1.Where(s => s.key == B.WorkTypeCode).Select(k => k.Name).First(),
                                      SpecialWorkerTypeNo = B.SpecialWorkerTypeNo,
                                  }).ToListAsync();

                var enCode = await _context.EntRegisterInfoMag
                    .Where(s => s.EntRegisterInfoMagId == query.EntCode)
                    .Select(k => k.EntCode).FirstOrDefaultAsync();
                //var enCode = query3.Where(s => s.EntRegisterInfoMagId == query.EntCode).Select(k => k.EntCode).FirstOrDefault();
                var query4 = await _context.ProjectPersonSnapshot
                    .Where(s => s.BelongedTo == query.BelongedTo && s.RecordNumber == query.RecordNumber
                    && s.EntCode == enCode && s.PersonType == "项目经理")
                    .Select(k => k.PersonName).FirstOrDefaultAsync();


                //生成检查表路径
                string webRootPath = _wordTemplte + "Anzhuanggaozhi" + ".doc";
                if (type == 1)
                {
                    webRootPath = _wordTemplte + "Chaixiegaozhi" + ".doc";
                }

                Aspose.Words.Document doc = new Aspose.Words.Document(webRootPath);

                DocumentBuilder builder = new DocumentBuilder(doc);


                Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                //安装告知表
                if (query1 != null)
                {

                    keyValuePairs.Add("MachineryName", query1.MachineryName ?? "");
                    keyValuePairs.Add("MaxInstallHeight", query1.MaxInstallHeight.ToString() ?? "");
                    keyValuePairs.Add("ManufacturingLicense", query1.ManufacturingLicense ?? "");
                    keyValuePairs.Add("OEM", query1.OEM ?? "");
                    keyValuePairs.Add("MachineryModel", query1.MachineryModel ?? "");
                    keyValuePairs.Add("DetectionNumber", number==null ? "" : number.DetectionNumber);
                    keyValuePairs.Add("IsOrNullDetection", number==null ? "" : (number.DetectionNumber=="" || number.DetectionNumber==null || number.DetectionNumber=="无此项") ? "否" : "是");
                    keyValuePairs.Add("QualityNo", query1.LeaveTheFactoryNo ?? "");
                    keyValuePairs.Add("SuperOrganName", cityZone.SuperOrganName ?? "");
                    keyValuePairs.Add("LeaveTheFactoryDate", query1.LeaveTheFactoryDate == null ? "" : query1.LeaveTheFactoryDate?.ToString("yyyy-MM-dd"));
                    keyValuePairs.Add("PropertyRightsRecordNo", query1.PropertyRightsRecordNo ?? "");
                    keyValuePairs.Add("CQEntName", _context.EntRegisterInfoMag.Where(s => s.EntRegisterInfoMagId == query1.EntGUID).Select(k => k.EntName).FirstOrDefault() ?? "");

                }



                if (query != null)
                {
                    keyValuePairs.Add("InstallationPosition", query.InstallationPosition ?? "");//更改为安装位置
                    keyValuePairs.Add("ACEntName", _context.EntRegisterInfoMag.Where(s => s.EntRegisterInfoMagId == query.EntGUID).Select(k => k.EntName).FirstOrDefault() ?? "");
                    keyValuePairs.Add("ACEntName1", _context.EntRegisterInfoMag.Where(s => s.EntRegisterInfoMagId == query.EntGUID).Select(k => k.EntName).FirstOrDefault() ?? "");
                    //keyValuePairs.Add("ProjectPerson", query.InstallLeader ?? "");//安拆现场负责人
                    keyValuePairs.Add("ProjectPerson1", query.SafetyPerson ?? "");//专职安全生产管理人员
                    keyValuePairs.Add("InstallLeader", query.InstallLeader ?? "");
                    keyValuePairs.Add("PlanUseBeginDate", query.PlanInstallDate?.ToString("yyyy-MM-dd"));
                    keyValuePairs.Add("PlanUseEntDate", query.PlanInstallEndDate?.ToString("yyyy-MM-dd"));
                    keyValuePairs.Add("ProjectName", query.ProjectName ?? "");
                    keyValuePairs.Add("ProjectAddress", query.ProjectAddress ?? "");
                    keyValuePairs.Add("SGEntName", query.EntName ?? "");
                    if (query.PlanDisassembleBeginDate != null && query.PlanDisassembleEndDate != null)
                    {
                        keyValuePairs.Add("PlanDisassembleDate", query.PlanDisassembleBeginDate?.ToString("yyyy-MM-dd") + "~" + query.PlanDisassembleEndDate?.ToString("yyyy-MM-dd"));
                    }
                }
                if (query4 != null)
                {
                    keyValuePairs.Add("ProjectManager", query4 ?? "");
                }


                string[] strTitleName = null;
                int[] strTitleWidth = null;
                // 判断word中是否存在当前标签
                if (doc.Range.Bookmarks["PO_ProjectPerson"] != null)
                {
                    builder.MoveToBookmark("PO_ProjectPerson");// 定位到书签去
                    strTitleName = new string[] { "姓名", "工种", "资格证编号", "备注" };
                    strTitleWidth = new int[] { 200, 420, 235, 240 };
                    for (int j = 0; j < strTitleName.Length; j++)
                    {
                        string filedName = strTitleName[j].ToString();
                        builder.InsertCell();// 添加一个单元格
                        builder.CellFormat.Width = strTitleWidth[j];
                        builder.CellFormat.Borders.LineStyle = Aspose.Words.LineStyle.Single;
                        builder.CellFormat.Borders.Color = System.Drawing.Color.Black;
                        builder.CellFormat.VerticalMerge = Aspose.Words.Tables.CellMerge.None;
                        builder.CellFormat.VerticalAlignment = CellVerticalAlignment.Center;//垂直居中对齐
                        builder.ParagraphFormat.Alignment = ParagraphAlignment.Center;//水平居中对齐
                        builder.ParagraphFormat.LeftIndent = 0;//段落缩进
                        builder.Write(filedName);
                    }
                    builder.EndRow();
                    doc.Range.Bookmarks["PO_ProjectPerson"].Text = "";    // 清掉标示
                }
                for (var m = 0; m < list.Count; m++)
                {
                    for (var n = 0; n < strTitleName.Length; n++)
                    {
                        builder.InsertCell();// 添加一个单元格
                        builder.CellFormat.Width = strTitleWidth[n];
                        builder.CellFormat.Borders.LineStyle = Aspose.Words.LineStyle.Single;
                        builder.CellFormat.Borders.Color = System.Drawing.Color.Black;
                        builder.CellFormat.VerticalMerge = Aspose.Words.Tables.CellMerge.None;
                        builder.CellFormat.VerticalAlignment = CellVerticalAlignment.Center;//垂直居中对齐
                        builder.ParagraphFormat.Alignment = ParagraphAlignment.Center;//水平居中对齐
                        builder.ParagraphFormat.LeftIndent = 0;//段落缩进
                        if (strTitleName[n] == "姓名")
                        {
                            var name = list[m].Name ?? "";
                            builder.Write(name);
                        }
                        else if (strTitleName[n] == "工种")
                        {
                            var codeName = list[m].CodeName ?? "";
                            builder.Write(codeName);
                        }
                        else if (strTitleName[n] == "资格证编号")
                        {
                            var specialWorkerTypeNo = list[m].SpecialWorkerTypeNo ?? "";
                            builder.Write(specialWorkerTypeNo);
                        }
                    }
                    builder.EndRow();
                    doc.Range.Bookmarks["PO_ProjectPerson"].Text = "";    // 清掉标示
                }
                //CanBeDeleted存在此文件夹是可以定期删除的
                var path = new DataBll(_environment, _context).BuildPdfToServer(_environment, doc, keyValuePairs, "CanBeDeleted", installationNotificationRecordId + "AnzhuanggaozhiShenQing.pdf", Request);
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, path);

            }
            catch (Exception ex)
            {

                _logger.LogError("下载安装告知申请表：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }


        }


        #region 项目搜索添加设备到安拆单位
        /// <summary>
        /// 添加安拆
        /// </summary>
        /// <param name="type"></param>
        /// <param name="recordnumber"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<object>> AddAnChaiInfo([FromBody] EquipmentInsDisViewModel info)
        {
            try
            {
                if (info.BelongedTo==null || info.RecordNumber==null || info.EntRegisterInfoMagId ==null)
                {
                    return ResponseViewModel<object>.Create(Status.ERROR, "缺少参数");
                }
                var now = DateTime.Now;
                var infos = _context.EquipmentInsDis.Where(w => w.RecordNumber==info.RecordNumber && w.JobType==info.JobType && w.DeviceTypes==info.DeviceTypes && w.EntRegisterInfoMagId==info.EntRegisterInfoMagId && w.DeleteMark==0).FirstOrDefault();
                if (infos!=null)
                {
                    infos.SubmitDate=now;
                    _context.EquipmentInsDis.Update(infos);
                }
                else
                {


                    EquipmentInsDis data = new EquipmentInsDis
                    {
                        AuditStatus=1,
                        BelongedTo=info.BelongedTo,
                        Contacts=info.Contacts,
                        SocialUnicode=info.SocialUnicode,
                        DeviceTypes=info.DeviceTypes,
                        Tel=info.Tel,
                        EntName=info.EntName,
                        EntRegisterInfoMagId=info.EntRegisterInfoMagId,
                        JobType=info.JobType,
                        RecordNumber=info.RecordNumber,
                        SubmitDate=now,
                        DeleteMark=0
                    };
                    _context.EquipmentInsDis.Add(data);
                }
                _context.SaveChanges();
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("添加安拆设备：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 撤回安拆
        /// </summary>
        /// <param name="type"></param>
        /// <param name="recordnumber"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<object>> UptAnChaiInfo([FromBody] EquipmentInsDisViewModel info)
        {
            try
            {
                var data = _context.EquipmentInsDis.Where(w => w.EntRegisterInfoMagId==info.EntRegisterInfoMagId && w.RecordNumber==info.RecordNumber && w.DeleteMark==0 && w.JobType==info.JobType && w.DeviceTypes==info.DeviceTypes).FirstOrDefault();
                if (data==null)
                {
                    return ResponseViewModel<object>.Create(Status.ERROR, "数据不存在或已被撤回");
                }
                data.SubmitDate=DateTime.Now;
                data.AuditStatus=0;
                data.JobType=info.JobType;
                data.DeviceTypes=info.DeviceTypes;
                data.DeleteMark=1;
                _context.EquipmentInsDis.Update(data);
                _context.SaveChanges();
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("添加安拆设备：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 模糊搜索获取入库安拆单位与全部安拆单位
        /// </summary>
        /// <param name="type"></param>
        /// <param name="entName"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<object>> GetEntUnitInfo(int type, string entName)
        {
            try
            {
                var data = new EntRegisterInfoMag();
                if (type==0 || type==1 || type == 2)
                {
                    if (!string.IsNullOrEmpty(entName))
                    {
                        data = _context.EntRegisterInfoMag.Where(w => w.DeleteMark==0 && (w.EntType == "安拆单位" || (w.IntelligenceLevel!=0 && w.IntelligenceLevelUrl!=null &&w.IntelligenceLevelUrl!="")) &&(w.EntName.Contains(entName) ||  w.SocialCreditCode.Contains(entName))).FirstOrDefault();
                        if (data==null)
                        {
                            return ResponseViewModel<object>.Create(Status.ERROR, "没有搜索到该安拆单位，请联系企业去注册!");
                        }
                        else
                        {
                            if (data.IsOrNotExamine==0 || data.IsOrNotExamine==3 || data.IsOrNotExamine==4)
                            {
                                return ResponseViewModel<object>.Create(Status.ERROR, "安拆单位未办理入库申请!");
                            }
                            if (data.IsOrNotExamine==1)
                            {
                                return ResponseViewModel<object>.Create(Status.ERROR, "安拆单位入库审核中!");
                            }

                        }


                    }

                }
                else
                {
                    data = _context.EntRegisterInfoMag.Where(w => w.DeleteMark==0 && (w.EntType == "安拆单位" || (w.IntelligenceLevel!=0 && w.IntelligenceLevelUrl!=null &&w.IntelligenceLevelUrl!="")) && (w.EntName.Contains(entName) || w.SocialCreditCode.Contains(entName))).FirstOrDefault();
                    if (data==null)
                    {
                        return ResponseViewModel<object>.Create(Status.ERROR, "没有搜索到该安拆单位，请重新输入");
                    }
                }

                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, data);
            }
            catch (Exception ex)
            {
                _logger.LogError("模糊搜索安拆单位：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }

        [HttpGet]
        public async Task<ResponseViewModel<object>> GetEquipmentInsSuper(int pageIndex, int pageSize, int? type, string entName, string devstatus, string devtype)
        {
            try
            {
                var entCode = User.FindFirst(nameof(ClaimTypeEnum.EntCode))?.Value;//操作人科室
                var danwei = _context.ProjectEntSnapshot.Where(w => w.EnterpriseType=="监理单位" && w.MainUnit=="是" && w.OrganizationCode==entCode).Select(s => new { s.BelongedTo, s.RecordNumber }).ToList();
                //var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//操作人科室
                var recordNumber = danwei.Select(s => s.RecordNumber).Distinct().ToList();
                var data = from A in _context.InstallationNotificationRecords
                           join B in _context.MachineryInfos
                           on A.MachineryInfoId equals B.MachineryInfoId
                           into t1
                           from r in t1.DefaultIfEmpty()
                           join C in _context.EntRegisterInfoMag on A.EntGUID equals C.EntRegisterInfoMagId
                           where A.Type == type && A.DeleteMark == 0 && recordNumber.Contains(A.RecordNumber) && r.DeleteMark == 0 && A.AuditStatus != 0
                           select new EquipmentInsDisInfo
                           {
                               MachineryInfoId = r.MachineryInfoId,
                               InstallationNotificationRecordId = A.InstallationNotificationRecordId,
                               UpdateTime = A.SubmitDate,
                               RecordNumber = A.RecordNumber,
                               BelongedTo = A.BelongedTo,
                               ProjectName = A.ProjectName,
                               MachineryModel = r.MachineryModel,
                               //MachineryName = r.MachineryName,
                               PropertyRightsRecordNo = r.PropertyRightsRecordNo,
                               EntName = C.EntName,
                               State = A.State,
                               IsExamineState = "1",
                               InstallationPosition = A.InstallationPosition,
                               LeaveTheFactoryNo = r.LeaveTheFactoryNo,
                               UpdateDate = A.UpdateDate,
                               ChanQuanEntGUID=r.EntGUID,
                               TestingInstituteInfoId=r.TestingInstituteInfoId,
                               InstallationUnit=_context.EntRegisterInfoMag.Where(w => w.EntRegisterInfoMagId==A.EntGUID && w.EntType=="安拆单位").Select(s => s.EntName).FirstOrDefault(),
                               DeviceStatus=A.JianliStatus.ToString(),
                               MachineryName =r.MachineryType,
                               //MachineryName=r.MachineryName,
                               EntRegisterInfoMagId=A.EntGUID,
                           };
                if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                {
                    data = from A in data
                           join B in _context.ProjectOverview
                           on new { A.BelongedTo, A.RecordNumber } equals new { B.BelongedTo, B.RecordNumber }
                           where B.SupervisionDepartmentId == supervisionDepartmentId
                           select A;
                }
                if (!string.IsNullOrWhiteSpace(devstatus))
                {
                    data = data.Where(w => w.DeviceStatus.Contains(devstatus));
                }
                if (!string.IsNullOrWhiteSpace(devtype))
                {
                    data = data.Where(w => w.MachineryName==(MachineryTypeEnum)Enum.Parse(typeof(MachineryTypeEnum),devtype));
                }
                if (!string.IsNullOrWhiteSpace(entName))
                {
                    data = data.Where(w => w.EntName.Contains(entName) || w.InstallationUnit.Contains(entName) || w.PropertyRightsRecordNo.Contains(entName));
                }
                //安装告知审核通过
                if (type == 0)
                {
                    data = data.Where(s => s.State == 2);
                }
                //拆卸告知审核通过
                if (type == 1)
                {
                    data = data.Where(s => s.State == 12);
                }
                var result = data.OrderByDescending(t => t.UpdateTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                int queryCount = data.Count();
                result.ForEach(f =>
                    {
                        f.OrganizationName=_context.TestingInstituteInfo.Where(w => w.TestingInstituteInfoId==f.TestingInstituteInfoId).Select(s => s.MechanismName).FirstOrDefault();
                        f.SetupScriptUrl=_context.EquipmentUrl.Where(w => w.SetupScriptUrlID==f.InstallationNotificationRecordId && w.DeleteMark==0).Select(s => s.SetupScriptUrl).ToList();
                    }
                );
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, result, queryCount);

            }
            catch (Exception ex)
            {

                _logger.LogError("获取告知列表失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }

        }


        /// <summary>
        /// 操作设备安装是否完成操作
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<object>> UptEquipmentInfo([FromBody] EquipmentInsInfo info)
        {
            try
            {
                var infos = _context.InstallationNotificationRecords.Where(w => w.InstallationNotificationRecordId==info.InstallationNotificationRecordId).FirstOrDefault();
                //var data = _context.EquipmentInsDis.Where(w => w.EntRegisterInfoMagId==info.EntRegisterInfoMagId).FirstOrDefault();
                var datas = _context.EquipmentUrl.Where(w => w.SetupScriptUrlID==info.InstallationNotificationRecordId && w.DeleteMark==0).ToList();
                //if (info.SetupScriptUrl.Count()==0)
                //{
                //    return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "上传文件不能为空！");
                //}

                if (info.DeviceStatus=="0")
                {
                    infos.JianliStatus=0;
                    _context.InstallationNotificationRecords.Update(infos);
                    foreach (var item in datas)
                    {
                        item.DeleteMark=1;
                        _context.EquipmentUrl.Update(item);
                    }
                }
                else
                {


                    infos.JianliStatus=1;
                    _context.InstallationNotificationRecords.Update(infos);
                    foreach (var item in info.SetupScriptUrl)
                    {
                        EquipmentUrl url = new EquipmentUrl();
                        url.EquimentUrlID=SecurityManage.GuidUpper();
                        url.SetupScriptUrl=item;
                        url.SetupScriptUrlID=info.InstallationNotificationRecordId;
                        url.DeleteMark=0;
                        _context.EquipmentUrl.Add(url);
                    }
                }
                _context.SaveChanges();
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "操作成功！");
            }
            catch (Exception ex)
            {
                _logger.LogError("操作失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }


        ///// <summary>
        ///// 操作设备安装是否完成删除操作
        ///// </summary>
        ///// <param name="info"></param>
        ///// <returns></returns>
        //[HttpPost]
        //[Authorize]
        //public async Task<ResponseViewModel<object>> DelEquipmentInfo([FromBody] EquipmentInsInfo info)
        //{
        //    try
        //    {
        //        var data = _context.EquipmentInsDis.Where(w => w.EntRegisterInfoMagId==info.InstallationNotificationRecordId).FirstOrDefault();
        //        var datas = _context.EquipmentUrl.Where(w => w.SetupScriptUrlID==data.SetupScriptUrlID).ToList();
        //        if (info.SetupScriptUrl.Count()==0)
        //        {
        //            return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "上传文件不能为空！");
        //        }
        //        data.DeviceStatus="0";
        //        _context.EquipmentInsDis.Update(data);
        //        foreach (var item in info.SetupScriptUrl)
        //        {
        //            EquipmentUrl url = new EquipmentUrl();
        //            url.SetupScriptUrl=item;
        //            url.SetupScriptUrlID=data.SetupScriptUrlID;
        //            url.DeleteMark=1;
        //            _context.EquipmentUrl.Update(url);
        //        }
        //        _context.SaveChanges();
        //        return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "操作成功！");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("操作失败：" + ex.Message + ex.StackTrace, ex);
        //        return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
        //    }
        //}

        /// <summary>
        /// 项目获取所发起的安拆列表
        /// </summary>
        /// <param name="type"></param>
        /// <param name="recordnumber"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<object>> GetAnChaiInfo(int pageIndex, int pageSize, int? type, string entName, string recordNumber, string belongedTo)
        {
            try
            {
                //var lgbelongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;//操作人科室 A.Type == s.JobType && && r.MachineryType==s.DeviceTypes
                var data = (from A in _context.InstallationNotificationRecords
                            join B in _context.MachineryInfos
                            on A.MachineryInfoId equals B.MachineryInfoId
                            into t1
                            from r in t1.DefaultIfEmpty()
                            join C in _context.EntRegisterInfoMag on A.EntGUID equals C.EntRegisterInfoMagId
                            where A.DeleteMark == 0 && A.BelongedTo==belongedTo && A.RecordNumber==recordNumber && r.DeleteMark == 0
                            select new MachineryData
                            {
                                RecordNumber=A.RecordNumber,
                                BelongedTo=A.BelongedTo,
                                Type=A.Type,
                                MachineryType=r.MachineryType.ToString()=="塔式起重机" ? 0 : (r.MachineryType.ToString()=="施工升降机" ? 1 : (r.MachineryType.ToString()=="物料提升机" ? 2 : (r.MachineryType.ToString()=="桥式起重机" ? 3 : (r.MachineryType.ToString() =="门式起重机" ? 4 : (r.MachineryType.ToString() =="货运施工升降机" ? 5 : 6)))))
                            }).ToList();

                var info = _context.EquipmentInsDis.Where(w => w.RecordNumber==recordNumber && w.BelongedTo==belongedTo && w.DeleteMark==0).ToList();
                var count = info.Count();
                if (!string.IsNullOrEmpty(entName))
                {
                    info=info.Where(w => w.SocialUnicode==entName || w.EntName.Contains(entName)).ToList();
                    count=info.Count();
                }
                if (type!=null)
                {

                    info=info.Where(w => w.JobType==type).ToList();
                    count=info.Count();
                }
                var result = info.Select(s => new EquipmentViewModel
                {
                    EntName=s.EntName,
                    AuditStatus=s.AuditStatus,
                    Tel=s.Tel,
                    //JobTypeI=s.JobType.Value,
                    SocialUnicode=s.SocialUnicode,
                    EntRegisterInfoMagId=s.EntRegisterInfoMagId,
                    JobType=s.JobType==0 ? "安装" : "拆卸",
                    RecordNumber=s.RecordNumber,
                    Contacts=s.Contacts,
                    DeviceTypes=s.DeviceTypes,
                    SubmitDate=s.SubmitDate.Value.ToString("yyyy-MM-dd"),
                    BelongedTo=s.BelongedTo,
                    ProjStatus=data.Where(w => w.Type==s.JobType && w.BelongedTo == s.BelongedTo && w.RecordNumber == s.RecordNumber && w.MachineryType ==s.DeviceTypes).FirstOrDefault()==null ? "0" : "1"
                    //DeviceTypeName=s.DeviceTypes==0 ?"塔式起重机" :(s.DeviceTypes==1 ? "施工升降机" : (s.DeviceTypes==2 ? "物料提升机" : (s.DeviceTypes==3 ? "桥式起重机" :(s.DeviceTypes == 4 ? "门式起重机" : "架桥机")))) 
                }).Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {

                _logger.LogError("安拆单位列表：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }

        #endregion

        /// <summary>
        /// 删除申请
        /// </summary>
        /// <param name="installationNotificationRecordId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> DeleteApply(string installationNotificationRecordId, int type)
        {
            try
            {
                var query = await _context.InstallationNotificationRecords
                    .Where(s => s.InstallationNotificationRecordId == installationNotificationRecordId
                    && s.Type == type && s.DeleteMark == 0)
                    .FirstOrDefaultAsync();
                if (query == null)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "数据异常，删除失败");
                }
                var query1 = await _context.MachineryInfos
                    .Where(s => s.MachineryInfoId == query.MachineryInfoId)
                    .FirstOrDefaultAsync();

                if (query.State != 0 && query.State != 10)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "该状态无法删除！");
                }
                query.DeleteMark = 1;
                _context.InstallationNotificationRecords.UpdateRange(query);
                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "删除成功");
            }
            catch (Exception ex)
            {

                _logger.LogError("删除申请：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

    }
}
