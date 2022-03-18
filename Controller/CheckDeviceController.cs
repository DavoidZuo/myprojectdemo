using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Aspose.Words;
using Aspose.Words.Drawing;
using Aspose.Words.Fields;
using Aspose.Words.Tables;
using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Common.DataBll;
using JSAJ.Core.Models;
using JSAJ.Core.Models.LargeMachinery;
using JSAJ.Core.Models.notice;
using JSAJ.Core.ViewModels;
using JSAJ.Models.Models.LargeMachinery;
using MCUtil.DBS;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViewModels;

namespace JSAJ.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CheckDeviceController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly JssanjianmanagerContext _context;
        private readonly string _wordTemplte;
        //private OssFileSetting _ossFileSetting;
        public CheckDeviceController(JssanjianmanagerContext context, ILogger<CheckDeviceController> logger,
                                     IWebHostEnvironment environment
                                     )
        {
           
            _environment = environment;
            _context = context;
            //_ossFileSetting = oss.Value;
            _wordTemplte = environment.WebRootPath + Path.DirectorySeparatorChar + "doc" + Path.DirectorySeparatorChar + "LargeMachinery" + Path.DirectorySeparatorChar;
        }
        public async Task<ResponseViewModel<List<CheckItemViewModel>>> GetCheckItems(MachineryTypeEnum? itemType, string machineryInfoId, string checkRecordId)
        {
            if (itemType == null)
            {
                return ResponseViewModel<List<CheckItemViewModel>>.Create(Status.FAIL, Message.FAIL);
            }
            try
            {
                var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;
                if (string.IsNullOrEmpty(testingId))
                {
                    return ResponseViewModel<List<CheckItemViewModel>>.Create(Status.FAIL, Message.FAIL);
                }
                var checkConfig = _context.CheckConfigures.Where(a => a.TestingInstituteInfoId == testingId && a.DeleteMark == 0).OrderByDescending(a => a.Id).FirstOrDefault();
                if (checkConfig == null || string.IsNullOrEmpty(checkConfig.InspectionBasis))
                {
                    return ResponseViewModel<List<CheckItemViewModel>>.Create(Status.FAIL, "未配置检验依据标准");
                }
                var item = await _context.TestBigItems
                   .Where(w => w.DeleteMark == 0 && w.ItemType == itemType && w.InspectionBasis == checkConfig.InspectionBasis)
                   .OrderBy(o => o.OrderBy)
                   .AsNoTracking()
                   .ToListAsync();
                var itemUids = item.Select(s => s.Uid).ToList();
                var content = await _context.TestContentItems
                    .Where(w => w.DeleteMark == 0 && w.InspectionBasis == checkConfig.InspectionBasis && itemUids.Contains(w.Uid))
                    .OrderBy(o => o.OrderBy)
                    .AsNoTracking()
                    .ToListAsync();
                var contentIds = content.Select(s => s.ContentId).Distinct().ToList();
                var viewModels = new List<CheckItemViewModel>();
                string checkDetailSql = "select [w].[ID], [w].[CheckConclusion], " +
                    "[w].[CheckNumber], [w].[CheckResult], [w].[ContentID], [w].[CreateDate], " +
                    "[w].[TContent], [w].[CheckRecordId], [w].[MachineryInfoId],[w].[InspectionBasis] from " +
                    "(select CheckDetails.*, row_number() over(partition by ContentID order by CreateDate desc) rn " +
                    "from CheckDetails where CheckRecordId='" + checkRecordId + "' and MachineryInfoId='" + machineryInfoId + "') w " +
                    "where rn = 1";
                if (contentIds.Count > 0)
                {
                    string contentIdStrs = string.Join("', N'", contentIds);
                    checkDetailSql += $" and ContentID IN ('{contentIdStrs}')";
                }

                var checkDetail = await _context.CheckDetails.FromSqlRaw(checkDetailSql)
                    .AsNoTracking()
                    .ToListAsync();
                var checkDetailDic = checkDetail.ToDictionary(d => d.ContentId);

                foreach (var node in item)
                {
                    var viewModel = new CheckItemViewModel();
                    viewModel.Title = node.Category;
                    viewModel.Key = node.Uid;
                    var temp = content.Where(w => w.Uid == node.Uid).ToList();
                    viewModel.TestContentItems = new List<ContentItem>();

                    foreach (var detail in temp)
                    {
                        var contentItem = new ContentItem()
                        {
                            Tcontent = detail.Tcontent,
                            ContentId = detail.ContentId,
                            CheckConclusion = detail.CheckConclusion,
                            CheckResult = detail.CheckResult,
                            IsNecessary = detail.IsNecessary,
                            OrderBy = detail.OrderBy
                        };
                        if (!string.IsNullOrWhiteSpace(detail.ContentId) && checkDetailDic.ContainsKey(detail.ContentId))
                        {
                            contentItem.CheckConclusion = checkDetailDic[detail.ContentId].CheckConclusion;
                            contentItem.CheckResult = checkDetailDic[detail.ContentId].CheckResult;
                        }
                        if (string.IsNullOrWhiteSpace(contentItem.CheckConclusion))
                        {
                            contentItem.CheckConclusion = "合格";
                        }
                        if (string.IsNullOrWhiteSpace(contentItem.CheckResult))
                        {
                            contentItem.CheckResult = "符合";
                        }
                        viewModel.TestContentItems.Add(contentItem);


                    }
                    if (viewModel.TestContentItems.Count > 0)
                    {
                        viewModel.Id = viewModel.TestContentItems[0].OrderBy;
                    }
                    viewModels.Add(viewModel);
                }
                viewModels = viewModels.OrderBy(o => o.Id).ToList();
                int index = (int)viewModels.Select(t => t.Id).Max();
                #region 自定义检查项配置
                int otherIndex = 0;
                var checkContentsItemList = await _context.CheckContentConfigures.Where(a => a.TestingInstituteInfoId == testingId && a.InspectionBasis == checkConfig.InspectionBasis && a.MachineryType == itemType && a.DeleteMark == 0).OrderByDescending(a => a.Id).ToListAsync();
                var zidingyicheckitem = await _context.CheckDetailsCustom.Where(a => a.CheckRecordId == checkRecordId && a.InspectionBasis == checkConfig.InspectionBasis && a.MachineryInfoId == machineryInfoId).OrderBy(a => a.Id).ToListAsync();

                if (checkContentsItemList.Count > 0)
                {
                    CheckItemViewModel item1 = new CheckItemViewModel();
                    item1.Id = index;
                    item1.Title = "其他";
                    item1.Key = Guid.NewGuid().ToString();
                    List<ContentItem> listzidingyi = new List<ContentItem>();
                    checkContentsItemList.ForEach(d =>
                    {
                        otherIndex++;
                        ContentItem zixiang = new ContentItem();
                        zixiang.ContentId = d.CheckContentConfigureId;
                        zixiang.Tcontent = d.TestContent;
                        zixiang.CheckConclusion = "合格";
                        zixiang.CheckResult = "符合";
                        if (zidingyicheckitem.Count > 0)
                        {
                            var findcheckItem = zidingyicheckitem.Where(f => f.CheckContentConfigureId == d.CheckContentConfigureId).FirstOrDefault();
                            if (findcheckItem != null)
                            {
                                zixiang.CheckConclusion = findcheckItem.CheckConclusion;
                                zixiang.CheckResult = findcheckItem.CheckResult;
                            }
                        }
                        zixiang.IsNecessary = 0;
                        zixiang.OrderBy = otherIndex;
                        listzidingyi.Add(zixiang);
                    });

                    item1.TestContentItems = listzidingyi;
                    viewModels.Add(item1);
                }

                #endregion



                return ResponseViewModel<List<CheckItemViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, viewModels);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<List<CheckItemViewModel>>.Create(Status.ERROR, Message.ERROR + ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// 保存检测内容
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<ResponseViewModel<string>> AddCheckDetail([FromBody] ReviewMachineryViewModel viewModel)
        {

            var details = viewModel.Details;
            if (details == null || details.Count == 0 || string.IsNullOrWhiteSpace(viewModel.CheckRecordId))
            {
                return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
            }
            try
            {
                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;

                if (string.IsNullOrEmpty(testingId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                var jianceconfig = _context.CheckConfigures.Where(w => w.TestingInstituteInfoId == testingId && w.DeleteMark == 0).FirstOrDefault();
                viewModel.TestingInstituteInfoId = testingId;
                viewModel.InspectionBasis = jianceconfig.InspectionBasis;
                var data = await _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId
                        && (w.MachineryCheckState == MachineryCheckStateEnum.检测中 || w.MachineryCheckState == MachineryCheckStateEnum.复检中)
                        && w.TestingInstituteInfoId == testingId)
                    .OrderByDescending(o => o.UseSubmitDate)
                    .FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"机械不存在或已被检测，无需重复操作");
                }

                await AddOrUpdateCheckDetails(viewModel);
                await AddOrUpdateCheckDetailsCustom(viewModel);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR + ex.Message + ex.StackTrace);
            }
        }


        /// <summary>
        /// 保存补充资料
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<ResponseViewModel<string>> SaveSupplementaryInformation([FromBody] MachineryInfoSupplementaryInformationViewModel viewModel)
        {
            var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
            var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;
            DateTime now = DateTime.Now;
            string checkRecordId = "";
            if (viewModel == null
                || string.IsNullOrWhiteSpace(viewModel.MachineryInfoId))
            {
                return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
            }
            try
            {
                var jixie = await _context.MachineryInfos.Where(w => w.MachineryInfoId == viewModel.MachineryInfoId).OrderByDescending(w => w.Id).FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(jixie.CheckRecordId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
                }

                var jianceZiLiao = await _context.MachineryInfoSupplementaryInformations.Where(w => w.MachineryInfoId == viewModel.MachineryInfoId
                   && w.CheckRecordId == jixie.CheckRecordId).FirstOrDefaultAsync();


                var bianhaolist = await _context.MachineryInfoSupplementaryInformations.Where(w => w.MachineryInfoId != viewModel.MachineryInfoId
                 && w.TestingInstituteInfoId == jixie.TestingInstituteInfoId && w.CheckNumber != null).Select(w => w.CheckNumber).ToListAsync();
                if (bianhaolist.Contains(viewModel.CheckNumber))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "检测编号已被使用！");
                }

                if (jianceZiLiao != null)
                {

                    jianceZiLiao.MachineryInfoId = viewModel.MachineryInfoId;
                    jianceZiLiao.TestingInstituteInfoId = jixie.TestingInstituteInfoId;
                    jianceZiLiao.CheckRecordId = jixie.CheckRecordId;
                    checkRecordId = jianceZiLiao.CheckRecordId;
                    jianceZiLiao.MachineryType = viewModel.MachineryType;
                    jianceZiLiao.CheckDate = viewModel.CheckDate;
                    jianceZiLiao.CheckNumber = viewModel.CheckNumber;
                    jianceZiLiao.UseYearBegin = viewModel.UseYearBegin;
                    jianceZiLiao.UseYearEnd = viewModel.UseYearEnd;
                    jianceZiLiao.Remark = viewModel.Remark;

                    //jianceZiLiao.IssueDate = viewModel.IssueDate;
                    jianceZiLiao.Weather = viewModel.Weather;
                    jianceZiLiao.Temperature = viewModel.Temperature;
                    jianceZiLiao.Humidity = viewModel.Humidity;
                    jianceZiLiao.WindSpeed = viewModel.WindSpeed;
                    jianceZiLiao.RatedLoadWeight = viewModel.RatedLoadWeight;
                    jianceZiLiao.RatedLiftingCapacity = viewModel.RatedLiftingCapacity;
                    jianceZiLiao.TimeHeight = viewModel.TimeHeight;
                    jianceZiLiao.DetectionNumber = viewModel.DetectionNumber;
                    jianceZiLiao.RatedLiftingTorqu = viewModel.RatedLiftingTorqu;
                    jianceZiLiao.IndependentHeight = viewModel.IndependentHeight;
                    jianceZiLiao.MaxInstallHeight = viewModel.MaxInstallHeight;
                    jianceZiLiao.MaxCrest = viewModel.MaxCrest;
                    jianceZiLiao.InstallCrest = viewModel.InstallCrest;
                    //jianceZiLiao.Avgweight = viewModel.Avgweight;
                    jianceZiLiao.FzNumberA = viewModel.FzNumberA;
                    jianceZiLiao.FzNumberB = viewModel.FzNumberB;
                    jianceZiLiao.FzDateA = viewModel.FzDateA;
                    jianceZiLiao.FzDateB = viewModel.FzDateB;
                    jianceZiLiao.DeleteMark = 0;
                    jianceZiLiao.UpdateDate = now;
                    jianceZiLiao.ValiDate = viewModel.ValiDate;
                    jixie.CheckNumber = viewModel.CheckNumber;
                    jixie.ValiDate = viewModel.ValiDate;

                    var delrenlist = _context.MechanicalTestingPersonnels.Where(s => s.CheckRecordId == jixie.CheckRecordId && s.Type == 0).ToList();
                    _context.MechanicalTestingPersonnels.RemoveRange(delrenlist);

                    if (viewModel.TestingInstituteWorker.Count > 0)
                    {
                        List<MechanicalTestingPersonnel> renlist = new List<MechanicalTestingPersonnel>();
                        viewModel.TestingInstituteWorker.ForEach(w =>
                        {
                            MechanicalTestingPersonnel ren = new MechanicalTestingPersonnel();
                            ren.CheckRecordId = jixie.CheckRecordId;
                            ren.MachineryInfoId = jixie.MachineryInfoId;
                            ren.Type = 0;
                            ren.TestingInstituteWorkerId = w.TestingInstituteWorkerId;
                            ren.WokerName = w.WokerName;
                            renlist.Add(ren);
                        });
                        await _context.MechanicalTestingPersonnels.AddRangeAsync(renlist);
                    }

                    _context.MachineryInfoSupplementaryInformations.Update(jianceZiLiao);
                }
                else
                {
                    MachineryInfoSupplementaryInformation model = new MachineryInfoSupplementaryInformation();
                    model.MachineryInfoId = viewModel.MachineryInfoId;
                    model.TestingInstituteInfoId = jixie.TestingInstituteInfoId;
                    model.CheckNumber = viewModel.CheckNumber;
                    jixie.CheckNumber = viewModel.CheckNumber;
                    model.CheckRecordId = jixie.CheckRecordId;
                    checkRecordId = model.CheckRecordId;
                    model.MachineryType = viewModel.MachineryType;
                    model.CheckDate = viewModel.CheckDate;
                    //model.IssueDate = viewModel.IssueDate;
                    model.Weather = viewModel.Weather;
                    model.Temperature = viewModel.Temperature;
                    model.Humidity = viewModel.Humidity;
                    model.WindSpeed = viewModel.WindSpeed;
                    model.RatedLoadWeight = viewModel.RatedLoadWeight;
                    model.RatedLiftingCapacity = viewModel.RatedLiftingCapacity;
                    model.TimeHeight = viewModel.TimeHeight;
                    model.DetectionNumber = viewModel.DetectionNumber;
                    model.RatedLiftingTorqu = viewModel.RatedLiftingTorqu;
                    model.IndependentHeight = viewModel.IndependentHeight;
                    model.MaxInstallHeight = viewModel.MaxInstallHeight;
                    model.MaxCrest = viewModel.MaxCrest;
                    model.InstallCrest = viewModel.InstallCrest;
                    //model.Avgweight = viewModel.Avgweight;
                    model.FzNumberA = viewModel.FzNumberA;
                    model.FzNumberB = viewModel.FzNumberB;
                    model.FzDateA = viewModel.FzDateA;
                    model.FzDateB = viewModel.FzDateB;
                    model.DeleteMark = 0;
                    model.UpdateDate = now;
                    model.CreateDate = now;
                    model.UseYearBegin = viewModel.UseYearBegin;
                    model.UseYearEnd = viewModel.UseYearEnd;

                    jixie.ValiDate = viewModel.ValiDate;



                    if (viewModel.TestingInstituteWorker.Count > 0)
                    {
                        List<MechanicalTestingPersonnel> renlist = new List<MechanicalTestingPersonnel>();
                        viewModel.TestingInstituteWorker.ForEach(w =>
                        {
                            MechanicalTestingPersonnel ren = new MechanicalTestingPersonnel();
                            ren.CheckRecordId = jixie.CheckRecordId;
                            ren.MachineryInfoId = jixie.MachineryInfoId;
                            ren.Type = 0;
                            ren.TestingInstituteWorkerId = w.TestingInstituteWorkerId;
                            ren.WokerName = w.WokerName;
                            renlist.Add(ren);
                        });
                        await _context.MechanicalTestingPersonnels.AddRangeAsync(renlist);
                    }

                    await _context.MachineryInfoSupplementaryInformations.AddAsync(model);
                }

                _context.MachineryInfos.Update(jixie);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, checkRecordId, 1);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR + ":" + ex.Message + ex.StackTrace);
            }
        }


        /// <summary>
        /// 更新照片审核方式字段
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<ResponseViewModel<string>> SavePhotoAuditMethod([FromBody] UpdateAuditMethodParaViewModel para)
        {
            var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
            var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;
            if (para == null
                || string.IsNullOrWhiteSpace(para.MachineryInfoId)
                || string.IsNullOrWhiteSpace(para.CheckRecordId))
            {
                return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
            }
            try
            {
                var jilu = await _context.CheckRecords.Where(w => w.MachineryInfoId == para.MachineryInfoId
                   && w.CheckRecordId == para.CheckRecordId).FirstOrDefaultAsync();
                if (jilu == null)
                {

                    return ResponseViewModel<string>.Create(Status.ERROR, "检测记录不存在");
                }
                var delrenlist = _context.MechanicalTestingPersonnels.Where(s => s.CheckRecordId == para.CheckRecordId && s.Type == 1).ToList();

                if (delrenlist.Count > 0)
                {
                    _context.MechanicalTestingPersonnels.RemoveRange(delrenlist);

                }
                if (para.TestingInstituteWorker.Count > 0)
                {
                    List<MechanicalTestingPersonnel> renlist = new List<MechanicalTestingPersonnel>();
                    para.TestingInstituteWorker.ForEach(w =>
                    {
                        MechanicalTestingPersonnel ren = new MechanicalTestingPersonnel();
                        ren.CheckRecordId = para.CheckRecordId;
                        ren.MachineryInfoId = para.MachineryInfoId;
                        ren.Type = 1;
                        ren.TestingInstituteWorkerId = w.TestingInstituteWorkerId;
                        ren.WokerName = w.WokerName;
                        renlist.Add(ren);
                    });
                    await _context.MechanicalTestingPersonnels.AddRangeAsync(renlist);
                }
                //jilu.TestingInstituteWorkerId = para.TestingInstituteWorkerId;
                //jilu.WokerName = para.WokerName;
                jilu.PhotoAuditMethod = para.PhotoAuditMethod;
                _context.CheckRecords.Update(jilu);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR+ex.Message+ex.StackTrace);
            }
        }



        /// <summary>
        /// 查看补充资料
        /// </summary>
        /// <param name="machineryInfoId"></param>
        /// <param name="checkRecordId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<ResponseViewModel<SeeMachineryInfoSupplementaryInformationViewModel>> SeeSupplementaryInformation(string machineryInfoId)
        {
            ;
            if (string.IsNullOrWhiteSpace(machineryInfoId))
            {
                return ResponseViewModel<SeeMachineryInfoSupplementaryInformationViewModel>.Create(Status.FAIL, "参数错误");
            }
            try
            {

                var jixie = await _context.MachineryInfos.Where(w => w.MachineryInfoId == machineryInfoId).OrderByDescending(w => w.Id).FirstOrDefaultAsync();

                if (jixie == null)
                {
                    return ResponseViewModel<SeeMachineryInfoSupplementaryInformationViewModel>.Create(Status.FAIL, "参数错误");
                }

                SeeMachineryInfoSupplementaryInformationViewModel renturnModel =
                    await _context.MachineryInfoSupplementaryInformations.Where(w =>
                    w.MachineryInfoId == machineryInfoId && w.CheckRecordId == jixie.CheckRecordId &&
                w.DeleteMark == 0).OrderByDescending(w => w.Id).Select(
                    e => new SeeMachineryInfoSupplementaryInformationViewModel
                    {
                        Id = e.Id,
                        MachineryInfoId = e.MachineryInfoId,
                        TestingInstituteInfoId = e.TestingInstituteInfoId,
                        CheckRecordId = e.CheckRecordId,
                        CheckNumber = e.CheckNumber,
                        MachineryType = e.MachineryType,
                        CheckDate = e.CheckDate,
                        IssueDate = e.IssueDate,
                        Weather = e.Weather,
                        Temperature = e.Temperature,
                        Humidity = e.Humidity,
                        WindSpeed = e.WindSpeed,
                        RatedLoadWeight = e.RatedLoadWeight,
                        RatedLiftingCapacity = e.RatedLiftingCapacity,
                        TimeHeight = e.TimeHeight,
                        DetectionNumber = e.DetectionNumber,
                        RatedLiftingTorqu = e.RatedLiftingTorqu,
                        IndependentHeight = e.IndependentHeight,
                        MaxInstallHeight = e.MaxInstallHeight,
                        MaxCrest = e.MaxCrest,
                        InstallCrest = e.InstallCrest,
                        Avgweight = e.Avgweight,
                        FzNumberA = e.FzNumberA,
                        FzNumberB = e.FzNumberB,
                        FzDateA = e.FzDateA,
                        FzDateB = e.FzDateB,
                        ValiDate = e.ValiDate,
                        Remark = e.Remark,
                        UseYearBegin = e.UseYearBegin,
                        UseYearEnd = e.UseYearEnd,

                    }).FirstOrDefaultAsync();
                if (renturnModel == null)
                {
                    renturnModel = new SeeMachineryInfoSupplementaryInformationViewModel();
                }

                if (renturnModel.UseYearBegin == null || renturnModel.UseYearEnd == null)
                {
                    var anzhuanggaozhi = _context.InstallationNotificationRecords.Where(x => x.DeleteMark == 0 && x.State == 2
   && x.MachineryInfoId == jixie.MachineryInfoId).OrderByDescending(x => x.Id).FirstOrDefault();
                    if (anzhuanggaozhi != null && !string.IsNullOrEmpty(anzhuanggaozhi.EntGUID))
                    {

                        //使用年限
                        if (anzhuanggaozhi.PlanUseBeginDate != null)
                        {
                            renturnModel.UseYearBegin = anzhuanggaozhi.PlanUseBeginDate;
                        }

                        if (anzhuanggaozhi.PlanUseBeginDate != null)
                        {
                            renturnModel.UseYearEnd = anzhuanggaozhi.PlanUseBeginDate;
                        }


                    }
                }



                //塔式起重机最大额度起重量-单位（t）
                renturnModel.RatedLiftingCapacity = jixie.MaxRatedLiftingCapacity == null ? "" : jixie.MaxRatedLiftingCapacity.ToString();
                renturnModel.RatedLiftingTorqu = jixie.Knm == null ? "" : jixie.Knm.ToString();
                renturnModel.IndependentHeight = jixie.FreeStandingHeight == null ? "" : jixie.FreeStandingHeight.ToString();
                renturnModel.MaxInstallHeight = jixie.MaxInstallHeight == null ? "" : jixie.MaxInstallHeight.ToString();
                renturnModel.MaxCrest = jixie.MaxRange == null ? "" : jixie.MaxRange.ToString();


                renturnModel.TestingInstituteWorker = _context.MechanicalTestingPersonnels.Where(s => s.CheckRecordId == jixie.CheckRecordId && s.Type == 0).Select(s => new TestingInstituteWorkerViewModel { TestingInstituteWorkerId = s.TestingInstituteWorkerId, WokerName = s.WokerName }).ToList();


                return ResponseViewModel<SeeMachineryInfoSupplementaryInformationViewModel>.Create(Status.SUCCESS, Message.SUCCESS, renturnModel, 1);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<SeeMachineryInfoSupplementaryInformationViewModel>.Create(Status.ERROR, Message.ERROR + ex.Message + ex.StackTrace);
            }
        }


        /// <summary>
        /// 批量更新或者添加检查项
        /// </summary>
        /// <param name="details"></param>
        /// <returns></returns>
        private async Task AddOrUpdateCheckDetails(ReviewMachineryViewModel viewModel)
        {
            try
            {
                var now = DateTime.Now;
                var details = viewModel.Details;
                var contentIds = details.Select(s => s.ContentId).ToList();
                // 先查出保存的这些是否存在
                var deldata = await _context.CheckDetails
                    .Where(w => w.MachineryInfoId == viewModel.MachineryInfoId && w.CheckRecordId == viewModel.CheckRecordId
                    && w.InspectionBasis != viewModel.InspectionBasis && !contentIds.Contains(w.ContentId))
                    .ToListAsync();
                _context.CheckDetails.RemoveRange(deldata);

                // 先查出保存的这些是否存在
                var data = await _context.CheckDetails
                    .Where(w => w.MachineryInfoId == viewModel.MachineryInfoId && w.CheckRecordId == viewModel.CheckRecordId
                     && contentIds.Contains(w.ContentId)).Distinct()
                    .ToListAsync();
                var dataDic = data.ToDictionary(d => d.ContentId);
                List<string> result = new List<string>();
                foreach (var item in details)
                {
                    if (string.IsNullOrWhiteSpace(item.CheckConclusion))
                    {
                        item.CheckConclusion = "合格";
                    }
                    if (string.IsNullOrWhiteSpace(item.CheckResult))
                    {
                        item.CheckResult = "符合";
                    }
                    if (dataDic.ContainsKey(item.ContentId))
                    {
                        dataDic[item.ContentId].CheckResult = item.CheckResult;
                        dataDic[item.ContentId].CheckConclusion = item.CheckConclusion;
                        dataDic[item.ContentId].InspectionBasis = viewModel.InspectionBasis;

                    }
                    else
                    {
                        item.CreateDate = now;
                        item.MachineryInfoId = viewModel.MachineryInfoId;
                        item.CheckRecordId = viewModel.CheckRecordId;
                        item.InspectionBasis = viewModel.InspectionBasis;
                        await _context.CheckDetails.AddAsync(item);
                    }
                    if (item.CheckResult != null && (item.CheckResult.Contains("不合格") || item.CheckResult.Contains("不符合")))
                    {
                        result.Add(item.ContentId);
                    }
                }
                _context.CheckDetails.UpdateRange(data);
            }
            catch (Exception ex)
            {

                throw;
            }
        }




        /// <summary>
        /// 批量更新或者添加自定义检查项
        /// </summary>
        /// <param name="details"></param>
        /// <returns></returns>
        private async Task AddOrUpdateCheckDetailsCustom(ReviewMachineryViewModel viewModel)
        {
            var now = DateTime.Now;
            var details = viewModel.CustomDetails;
            var contentIds = details.Select(s => s.CheckContentConfigureId).ToList();
            // 先查出保存的这些是否存在
            var deldata = await _context.CheckDetailsCustom
                .Where(w => w.MachineryInfoId == viewModel.MachineryInfoId && w.CheckRecordId == viewModel.CheckRecordId
                && w.InspectionBasis != viewModel.InspectionBasis && !contentIds.Contains(w.CheckContentConfigureId))
                .ToListAsync();
            _context.CheckDetailsCustom.RemoveRange(deldata);

            // 先查出保存的这些是否存在
            var data = await _context.CheckDetailsCustom
                .Where(w => w.MachineryInfoId == viewModel.MachineryInfoId && w.CheckRecordId == viewModel.CheckRecordId
                 && contentIds.Contains(w.CheckContentConfigureId))
                .ToListAsync();

            var dataDic = data.ToDictionary(d => d.CheckContentConfigureId);
            List<string> result = new List<string>();
            foreach (var item in details)
            {
                if (string.IsNullOrWhiteSpace(item.CheckConclusion))
                {
                    item.CheckConclusion = "合格";
                }
                if (string.IsNullOrWhiteSpace(item.CheckResult))
                {
                    item.CheckResult = "符合";
                }
                if (dataDic.ContainsKey(item.CheckContentConfigureId))
                {
                    dataDic[item.CheckContentConfigureId].CheckResult = item.CheckResult;
                    dataDic[item.CheckContentConfigureId].CheckConclusion = item.CheckConclusion;
                    dataDic[item.CheckContentConfigureId].InspectionBasis = viewModel.InspectionBasis;
                }
                else
                {
                    item.CreateDate = now;
                    item.MachineryInfoId = viewModel.MachineryInfoId;
                    item.CheckRecordId = viewModel.CheckRecordId;
                    item.InspectionBasis = viewModel.InspectionBasis;
                    await _context.CheckDetailsCustom.AddAsync(item);
                }
                if (item.CheckResult != null && (item.CheckResult.Contains("不合格") || item.CheckResult.Contains("不符合")))
                {
                    result.Add(item.CheckContentConfigureId);
                }
            }
            _context.CheckDetailsCustom.UpdateRange(data);
        }






        public string GetWordName(MachineryTypeEnum MachineryType, string inspectionBasis)
        {
            string wordTemplteName = "";

            if (inspectionBasis.Contains("DGJ32/J65"))
            {
                if (MachineryType == MachineryTypeEnum.塔式起重机)
                {
                    wordTemplteName = "tsqzj32.doc";
                }
                else if (MachineryType == MachineryTypeEnum.施工升降机)
                {
                    wordTemplteName = "sgsjj32.doc";
                }


            }
            else
            {
                if (MachineryType == MachineryTypeEnum.塔式起重机)
                {
                    wordTemplteName = "tsqzj305.doc";
                }
                else if (MachineryType == MachineryTypeEnum.施工升降机)
                {
                    wordTemplteName = "sgsjj305.doc";
                }
            }

            return wordTemplteName;
        }


        /// <summary>
        /// 查看检测的保证项目不合格数以及检测报告
        /// </summary>
        /// <param name="machineryInfoId"></param>
        /// <param name="checkRecordId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<ResponseViewModel<CheckRecordViewModel>> SeeCheckDetailInfo(string machineryInfoId, string checkRecordId)
        {
            ;
            if (string.IsNullOrWhiteSpace(machineryInfoId))
            {
                return ResponseViewModel<CheckRecordViewModel>.Create(Status.FAIL, "参数错误");
            }
            try
            {
                var jiance = await _context.CheckRecords.Where(w => w.CheckRecordId == checkRecordId).OrderByDescending(w => w.Id).FirstOrDefaultAsync();
                if (jiance != null)
                {
                    var buchongziliao = await _context.MachineryInfoSupplementaryInformations.Where(a => a.MachineryInfoId == machineryInfoId && a.CheckRecordId == checkRecordId && a.DeleteMark == 0).FirstOrDefaultAsync();
                    CheckRecordViewModel result = new CheckRecordViewModel();
                    //查询机械检测明细数据
                    var checkDeatil = await (from a in _context.CheckDetails
                                             join b in _context.TestContentItems on a.ContentId equals b.ContentId
                                             where a.MachineryInfoId == machineryInfoId && a.CheckRecordId == checkRecordId
                                             orderby b.OrderBy ascending
                                             select new
                                             {
                                                 ID = b.Id,
                                                 TContent = a.Tcontent,
                                                 CheckResult = a.CheckResult,
                                                 CheckConclusion = a.CheckConclusion,
                                                 IsNecessary = b.IsNecessary,
                                                 OrderBy = b.OrderBy
                                             }).ToListAsync();


                    result.CheckRecordId = jiance.CheckRecordId;
                    result.MachineryInfoId = jiance.MachineryInfoId;
                    result.CheckState = jiance.CheckState.GetHashCode();
                    result.TestingInstituteInfoId = jiance.TestingInstituteInfoId;
                    result.RecordUrl = jiance.RecordUrl;
                    result.GeneralItem = null;
                    result.GuaranteeItem = null;
                    result.IsCheckFinish = 0;
                    if (checkDeatil.Count > 0)
                    {
                        if (buchongziliao != null)
                        {
                            result.IsCheckFinish = 1;
                        }
                        //保证项目不合格数                  
                        result.GuaranteeItem = checkDeatil.Where(w => w.IsNecessary == 1 && (w.CheckResult.Contains("不合格")
                        || w.CheckResult.Contains("不符合"))).Count();
                        //一般项目不合格数
                        result.GeneralItem = checkDeatil.Where(w => w.IsNecessary == 0 && (w.CheckResult.Contains("不合格")
                        || w.CheckResult.Contains("不符合"))).Count();

                    }
                    return ResponseViewModel<CheckRecordViewModel>.Create(Status.SUCCESS, Message.SUCCESS, result, 1);
                }
                else
                {
                    return ResponseViewModel<CheckRecordViewModel>.Create(Status.SUCCESS, Message.SUCCESS, null, 0);
                }


            }
            catch (Exception ex)
            {
                //_logger.LogError("回显补充资料：" + ex.StackTrace, ex);
                return ResponseViewModel<CheckRecordViewModel>.Create(Status.ERROR, Message.ERROR + ex.Message + ex.StackTrace);
            }
        }



        /// <summary>
        /// 保存填写的检测结果
        /// </summary>
        /// <param name="machineryInfoId"></param>
        /// <param name="checkRecordId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<ResponseViewModel<string>> SaveCheckDetailInfo(CheckRecordViewModel model)
        {
            ;
            if (string.IsNullOrWhiteSpace(model.MachineryInfoId)
                || string.IsNullOrWhiteSpace(model.CheckRecordId)
                || model.GeneralItem == null || model.GuaranteeItem == null)
            {
                return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
            }

            try
            {
                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;
                var data = await _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == model.MachineryInfoId
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
                data.MachineryCheckState = (MachineryCheckStateEnum)model.CheckState;
                data.CheckUrl = model.RecordUrl;
                if (data.MachineryCheckState == MachineryCheckStateEnum.检测中)
                {
                    data.CheckReviewDate = now;
                    data.ReCheckReviewDate = now;
                }
                else if (data.MachineryCheckState == MachineryCheckStateEnum.复检中)
                {
                    data.CheckReviewDate = now;
                    data.ReCheckReviewDate = now;
                    if (model.CheckState == MachineryCheckStateEnum.检测合格.GetHashCode())
                    {
                        data.MachineryCheckState = MachineryCheckStateEnum.复检合格;
                    }
                    else if (model.CheckState == MachineryCheckStateEnum.检测不合格.GetHashCode())
                    {
                        data.MachineryCheckState = MachineryCheckStateEnum.复检不合格;
                    }
                }
                data.UpdateDate = now;
                _context.MachineryInfos.Update(data);
                var jiance = await _context.CheckRecords
                    .Where(w => w.CheckRecordId == data.CheckRecordId && w.TestingInstituteInfoId == testingId && w.CheckRecordId == model.CheckRecordId)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                if (jiance == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"机械委托检测记录不存在或已被删除，无法进行检测");
                }
                if (string.IsNullOrEmpty(jiance.RecordUrl))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"请先生成检测报告！");
                }
                if (jiance != null)
                {
                    jiance.CheckRecordId = model.CheckRecordId;
                    jiance.MachineryInfoId = model.MachineryInfoId;
                    jiance.CheckState = (MachineryCheckStateEnum)model.CheckState;
                    jiance.TestingInstituteInfoId = testingId;
                    jiance.RecordUrl = model.RecordUrl;
                    jiance.ReviewUserId = userId;
                    jiance.GeneralItem = (int)model.GeneralItem;
                    jiance.GuaranteeItem = (int)model.GuaranteeItem;
                    _context.CheckRecords.Update(jiance);
                    await _context.SaveChangesAsync();
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
                }
                else
                {
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, null, 0);
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError("保存填写的检测结果：" + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR + ex.Message + ex.StackTrace);
            }
        }



        /// <summary>
        /// 预览报告
        /// </summary>
        /// <param name="paraModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<LookWordCheckRecordViewModel>> BuildWordsNew(BuildWordCheckRecordViewModel paraModel)
        {
            try
            {
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;

                var userName = User.FindFirst(ClaimsIdentity.DefaultNameClaimType)?.Value;//当前人名字

                if (paraModel == null
                    || string.IsNullOrWhiteSpace(paraModel.MachineryInfoId)
                     || string.IsNullOrWhiteSpace(paraModel.CheckRecordId)
                     || paraModel.IsLook < 0 || paraModel.IsLook > 0
                    || paraModel.MachineryCheckState <= 0)
                {
                    return ResponseViewModel<LookWordCheckRecordViewModel>.Create(Status.FAIL, "参数错误");
                }
                DateTime now = DateTime.Now;
                Random nn = new Random();
                int suijisun = nn.Next(1, 300);
                var jiance = await _context.CheckRecords.Where(s => s.DeleteMark == 0
                    && s.MachineryInfoId == paraModel.MachineryInfoId && s.CheckRecordId == paraModel.CheckRecordId).FirstOrDefaultAsync();
                if (jiance == null)
                {
                    return ResponseViewModel<LookWordCheckRecordViewModel>.Create(Status.FAIL, $"机械委托检测记录不存在或已被删除，无法进行检测");
                }
                if (paraModel.IsLook == 1 && !string.IsNullOrWhiteSpace(jiance.RecordUrl))
                {
                    LookWordCheckRecordViewModel returnmodel0 = new LookWordCheckRecordViewModel();
                    returnmodel0.PreviewReportUrl = jiance.PreviewReportUrl;
                    returnmodel0.MachineryInfoId = jiance.MachineryInfoId;
                    returnmodel0.PreviewReportMachineryCheckState = jiance.PreviewReportMachineryCheckState;
                    returnmodel0.CheckRecordId = jiance.CheckRecordId;


                    //查看之前生成的文档
                    return ResponseViewModel<LookWordCheckRecordViewModel>.Create(Status.SUCCESS, Message.SUCCESS, returnmodel0, 1);
                }

                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;


                if (string.IsNullOrEmpty(testingId) && paraModel.IsLook == 0)
                {
                    return ResponseViewModel<LookWordCheckRecordViewModel>.Create(Status.FAIL, "当前角色不可操作");
                }
                var jiancedanwei = _context.TestingInstituteInfo.Where(a => a.TestingInstituteInfoId == testingId).FirstOrDefault();
                var checkConfig = _context.CheckConfigures.Where(a => a.TestingInstituteInfoId == testingId && a.DeleteMark == 0).OrderByDescending(a => a.Id).FirstOrDefault();

                string docWordFile = "";
                int buhegeCount = 0;//保证项目不合格数
                int buhegeCountYB = 0;//一般项目不合格数  

                var jixieInfo = await _context.MachineryInfos
                          .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == paraModel.MachineryInfoId
                              && (w.MachineryCheckState == MachineryCheckStateEnum.检测中
                                || w.MachineryCheckState == MachineryCheckStateEnum.复检中)
                              && w.TestingInstituteInfoId == testingId)
                          .OrderByDescending(o => o.Id)
                          .FirstOrDefaultAsync();
                if (jixieInfo == null)
                {
                    return ResponseViewModel<LookWordCheckRecordViewModel>.Create(Status.FAIL, $"机械不存在或已被检测，无需重复操作");
                }

                string jiexietype = "";
                int jxtype = 0;
                if (jixieInfo.MachineryType == MachineryTypeEnum.塔式起重机)
                {
                    jiexietype = "T";
                    jxtype = 0;
                }
                else if (jixieInfo.MachineryType == MachineryTypeEnum.施工升降机)
                {
                    jiexietype = "S";
                    jxtype = 1;
                }

                var jiancesuo = await _context.TestingInstituteInfo.Where(s => s.TestingInstituteInfoId == testingId).FirstOrDefaultAsync();
                if (jiancesuo == null || string.IsNullOrEmpty(jiancesuo.AbbreviationCode))
                {
                    return ResponseViewModel<LookWordCheckRecordViewModel>.Create(Status.FAIL, "请先配置检测机构缩写代码");
                }
                var projectInfo = await _context.ProjectOverview.Where(s => s.BelongedTo == jixieInfo.BelongedTo && s.RecordNumber == jixieInfo.RecordNumber).FirstOrDefaultAsync();
                if (projectInfo == null)
                {
                    return ResponseViewModel<LookWordCheckRecordViewModel>.Create(Status.FAIL, "项目【" + jixieInfo.RecordNumber + "】不存在");
                }
                string fileName = "";

                var buchongziliao = await _context.MachineryInfoSupplementaryInformations.Where(s => s.MachineryInfoId == jixieInfo.MachineryInfoId && s.CheckRecordId == paraModel.CheckRecordId).OrderByDescending(s => s.CreateDate).FirstOrDefaultAsync();

                string checkDate = buchongziliao.CheckDate.ToString("yyyy-MM-dd");
                //查询机械检测明细数据
                var checkDeatil = from a in _context.CheckDetails
                                  join b in _context.TestContentItems on a.ContentId equals b.ContentId
                                  where a.MachineryInfoId == jixieInfo.MachineryInfoId && a.CheckRecordId == paraModel.CheckRecordId
                                  orderby b.OrderBy ascending
                                  select new
                                  {
                                      ID = b.Id,
                                      TContent = a.Tcontent,
                                      CheckResult = a.CheckResult,
                                      CheckConclusion = a.CheckConclusion,
                                      IsNecessary = b.IsNecessary,
                                      OrderBy = b.OrderBy
                                  };

                var checkdeatailcustom = await (from a in _context.CheckDetailsCustom.Where(a => a.MachineryInfoId == jixieInfo.MachineryInfoId
                 && a.CheckRecordId == paraModel.CheckRecordId && a.InspectionBasis == checkConfig.InspectionBasis)
                                                join b in _context.CheckContentConfigures
                                                on a.CheckContentConfigureId equals b.CheckContentConfigureId
                                                where b.DeleteMark == 0
                                                select a).ToListAsync();
                DataTable dtCheckDetail = DataBll.ListToDataTable(checkDeatil.ToList());//检测内容转DataTable   
                string wordTemplteName = GetWordName(jixieInfo.MachineryType, checkConfig.InspectionBasis);
                if (wordTemplteName == "")
                    return ResponseViewModel<LookWordCheckRecordViewModel>.Create(Status.ERROR, "缺少该机械模板，请联系管理员！");
                string webRootPath = _wordTemplte + wordTemplteName;
                Aspose.Words.Document doc = new Aspose.Words.Document(webRootPath);
                DocumentBuilder builder = new DocumentBuilder(doc);


                #region 自动生成检测报告编号 目前需求变更 暂时不需要
                ////获得当前最大流水号值
                //var maxInfo = await _context.MachineryCheckSerialNumbers.Where(s => s.AbbreviationCode == jiancesuo.AbbreviationCode
                //   && s.MachinerytypeCode == jiexietype && s.Year == now.Year).FirstOrDefaultAsync();
                //if (maxInfo != null)
                //{
                //    jixieInfo.CheckNumber = jiancesuo.AbbreviationCode + jiexietype + now.ToString("yyyyMMdd") + maxInfo.Number.ToString().PadLeft(5, '0');
                //    maxInfo.Number = maxInfo.Number + 1;
                //    _context.MachineryCheckSerialNumbers.Update(maxInfo);
                //    _context.MachineryInfos.Update(jixieInfo);
                //}
                //else
                //{
                //    MachineryCheckSerialNumber maxinfonew = new MachineryCheckSerialNumber();
                //    maxinfonew.MachinerytypeCode = jiexietype;
                //    maxinfonew.Number = 1;
                //    maxinfonew.AbbreviationCode = jiancesuo.AbbreviationCode;
                //    maxinfonew.Year = now.Year;
                //    await _context.MachineryCheckSerialNumbers.AddAsync(maxinfonew);
                //    jixieInfo.CheckNumber = jiancesuo.AbbreviationCode + jiexietype + now.ToString("yyyyMMdd") + maxinfonew.Number.ToString().PadLeft(5, '0');
                //    _context.MachineryInfos.Update(jixieInfo);
                //} 
                #endregion

                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("JiGouCode", checkConfig.CheckCode);
                for (int i = 0; i < 15; i++)
                {
                    dic.Add("BaoGaoNo" + i, jixieInfo.CheckNumber);

                }
                dic.Add("TimeHeight", buchongziliao.TimeHeight);
                dic.Add("IndependentHeight0", buchongziliao.IndependentHeight);
                dic.Add("SupplementaryRemarks", buchongziliao.Remark);//补充资料填写的备注
                dic.Add("DetectionNumber", buchongziliao.DetectionNumber);

                var jianlidanwei = await _context.ProjectEntSnapshot
                  .Where(s => s.RecordNumber == projectInfo.RecordNumber
                  && s.BelongedTo == projectInfo.BelongedTo
                  && s.EnterpriseType == "监理单位" && s.MainUnit == "是")
                  .FirstOrDefaultAsync();
                if (jianlidanwei != null)
                {
                    dic.Add("SuperUnit", jianlidanwei.EnterpriseName);
                    //监理单位
                    dic.Add("SuperUnit0", jianlidanwei.EnterpriseName);
                }
                //检验人员
                //dic.Add("SignJianYan", userName);
                //检验人员
                //dic.Add("SignJianYan0", userName);
                dic.Add("ProjectName", projectInfo.ProjectName);
                dic.Add("ProjectName0", projectInfo.ProjectName);
                dic.Add("PropertyRightsRecordNo", jixieInfo.PropertyRightsRecordNo == null ? "" : jixieInfo.PropertyRightsRecordNo);
                if (checkConfig.InspectionBasis.Contains("DGJ32/J65"))
                {
                    dic.Add("InspectionBasis", checkConfig.InspectionBasis + "——2015");
                }
                else
                {
                    dic.Add("InspectionBasis", checkConfig.InspectionBasis);
                }

                dic.Add("MachineryType", jixieInfo.MachineryType.ToString());
                dic.Add("SD", buchongziliao.Humidity);
                dic.Add("ValidityDate", buchongziliao.ValiDate == null ? "一年" : ((DateTime)buchongziliao.ValiDate).ToString("yyyy年MM月dd日"));
                dic.Add("Temperature", buchongziliao.Temperature.ToString().Replace("℃", ""));// 温度
                dic.Add("Weather", buchongziliao.Weather);
                dic.Add("WindPower", buchongziliao.WindSpeed.Replace("m/s", "").Replace("M/S", ""));
                dic.Add("PropertyNum", projectInfo.RecordNumber);
                dic.Add("NowHeightSg", buchongziliao.TimeHeight);
                dic.Add("ManufacturingLicense", jixieInfo.ManufacturingLicense);
                dic.Add("CheckYear", Convert.ToDateTime(checkDate).Year.ToString());
                dic.Add("CheckMonth", Convert.ToDateTime(checkDate).Month.ToString());
                dic.Add("CheckDay", Convert.ToDateTime(checkDate).Day.ToString());
                if (jiancedanwei != null)
                {
                    dic.Add("TestingInstituteName", jiancedanwei.MechanismName == null ? "" : jiancedanwei.MechanismName);
                    dic.Add("TestingInstituteAddress", jiancedanwei.Address == null ? "" : jiancedanwei.Address);
                    dic.Add("TestingInstitutePostalCode", jiancedanwei.PostalCode == null ? "" : jiancedanwei.PostalCode);
                }
                #region 检验仪器
                var peizhi = await _context.CheckEquipmentConfigures.Where(a => a.TestingInstituteInfoId == testingId && a.MachineryType == jixieInfo.MachineryType && a.DeleteMark == 0).OrderBy(a => a.Id).ToListAsync();

                for (int i = 0; i < peizhi.Count; i++)
                {
                    dic.Add("CheckEquipmentName" + i, peizhi[i].CheckEquipmentName);
                    dic.Add("CheckEquipmentModel" + i, peizhi[i].CheckEquipmentModel);
                    dic.Add("CheckEquipmentNo" + i, peizhi[i].CheckEquipmentNo);
                    dic.Add("DeviceState" + i, peizhi[i].CheckEquipmentState);
                }
                #endregion

                dic.Add("FzNumberA", buchongziliao.FzNumberA);
                dic.Add("FzNumberB", buchongziliao.FzNumberA);
                dic.Add("FzDateA", buchongziliao.FzDateA);
                dic.Add("FzDateB", buchongziliao.FzDateB);


                //使用年限
                if (buchongziliao.UseYearBegin != null && buchongziliao.UseYearEnd != null)
                {
                    dic.Add("UseOfYear", ((DateTime)buchongziliao.UseYearBegin).ToString("yyyy-MM-dd") + "~" + ((DateTime)buchongziliao.UseYearEnd).ToString("yyyy-MM-dd"));
                }

                dic.Add("DeviceModel", jixieInfo.MachineryModel);
                dic.Add("DeviceModel0", jixieInfo.MachineryModel);
                dic.Add("UseUnit", jixieInfo.EntName);
                dic.Add("UseUnit0", jixieInfo.EntName);
                dic.Add("ShiGongDiDian", projectInfo.ProjectAddress);
                dic.Add("Manufacturer", jixieInfo.OEM);
                dic.Add("Manufacturer0", jixieInfo.OEM);

                var anzhuanggaozhi = _context.InstallationNotificationRecords.Where(x => x.DeleteMark == 0 && x.State == 2
        && x.MachineryInfoId == jixieInfo.MachineryInfoId).OrderByDescending(x => x.Id).FirstOrDefault();
                if (anzhuanggaozhi != null && !string.IsNullOrEmpty(anzhuanggaozhi.EntGUID))
                {

                    #region 安装位置
                    dic.Add("InstallPosition", anzhuanggaozhi.InstallationPosition);
                    dic.Add("InstallReviewDate", jixieInfo.InstallReviewDate == null ? "" : ((DateTime)jixieInfo.InstallReviewDate).ToString("yyyy-MM-dd"));
                    ////使用年限
                    //if (anzhuanggaozhi.PlanUseBeginDate != null && anzhuanggaozhi.PlanUseEndDate != null)
                    //{
                    //    dic.Add("UseOfYear", ((DateTime)anzhuanggaozhi.PlanUseBeginDate).ToString("yyyy-MM-dd") + "~" + ((DateTime)anzhuanggaozhi.PlanUseBeginDate).ToString("yyyy-MM-dd"));
                    //}
                    #endregion

                    var anchaiName = _context.EntRegisterInfoMag.Where(x => x.EntRegisterInfoMagId == anzhuanggaozhi.EntGUID)
                                                    .Select(x => x.EntName).FirstOrDefault();//安拆单位的名字

                    dic.Add("InstallLeader0", anchaiName == null ? "" : anzhuanggaozhi.InstallLeader);
                    //安装单位
                    dic.Add("InstallUnit", anchaiName == null ? "" : anchaiName);

                    dic.Add("InstallUnit0", anchaiName == null ? "" : anchaiName);
                }



                docWordFile = jiance.RecordUrl;
                //获取检测报告日期


                dic.Add("CheckDate", checkDate);
                dic.Add("QianFaDate", now.ToString("yyyy-MM-dd"));
                dic.Add("OutFactoryNum0", jixieInfo.LeaveTheFactoryNo);
                dic.Add("ProdutionDate0", jixieInfo.LeaveTheFactoryDate == null ? "" : ((DateTime)jixieInfo.LeaveTheFactoryDate).ToString("yyyy-MM-dd"));
                dic.Add("RatedLoadWeight0", buchongziliao.RatedLoadWeight);
                dic.Add("TaJiRatedLoadWeight0", buchongziliao.RatedLiftingCapacity);

                if (doc.Range.FormFields["PO_KNM0"] != null)
                {
                    FormField PO_KNM0 = doc.Range.FormFields["PO_KNM0"];
                    PO_KNM0.Result = buchongziliao.RatedLiftingTorqu;
                }
                if (doc.Range.FormFields["PO_MaxCrest0"] != null)
                {
                    FormField PO_MaxCrest0 = doc.Range.FormFields["PO_MaxCrest0"];
                    PO_MaxCrest0.Result = buchongziliao.MaxCrest;
                }
                if (doc.Range.FormFields["PO_installCrest0"] != null)
                {
                    FormField PO_installCrest0 = doc.Range.FormFields["PO_installCrest0"];
                    PO_installCrest0.Result = buchongziliao.InstallCrest;
                }

                if (doc.Range.FormFields["PO_MaxInstallHeight0"] != null)
                {
                    FormField PO_MaxInstallHeight0 = doc.Range.FormFields["PO_MaxInstallHeight0"];
                    PO_MaxInstallHeight0.Result = buchongziliao.MaxInstallHeight;
                }
                if (doc.Range.FormFields["PO_ProjectAddress0"] != null)
                {
                    FormField PO_ProjectAddress0 = doc.Range.FormFields["PO_ProjectAddress0"];
                    PO_ProjectAddress0.Result = projectInfo.ProjectAddress;
                }

                if (doc.Range.FormFields["PO_TimeHeight0"] != null)
                {
                    FormField PO_TimeHeight0 = doc.Range.FormFields["PO_TimeHeight0"];
                    PO_TimeHeight0.Result = buchongziliao.TimeHeight;
                }
                if (doc.Range.FormFields["PO_DetectionNumber0"] != null)
                {
                    FormField PO_DetectionNumber0 = doc.Range.FormFields["PO_DetectionNumber0"];
                    PO_DetectionNumber0.Result = buchongziliao.DetectionNumber;
                }

                MachineryCheckStateEnum baogaojieguo = jixieInfo.MachineryCheckState;

                if (dtCheckDetail != null && dtCheckDetail.Rows.Count > 0)
                {
                    //保证项目不合格数
                    DataRow[] row = dtCheckDetail.Select("IsNecessary=1 and (CheckConclusion='不合格' OR CheckConclusion='不符合')");

                    jiance.GuaranteeItem = row.Count();
                    if (doc.Range.FormFields["PO_BuHeGe"] != null)
                    {
                        FormField markCheckDate = doc.Range.FormFields["PO_BuHeGe"];
                        markCheckDate.Result = row.Count().ToString();
                        buhegeCount = row.Count();
                    }
                    //一般项目不合格数
                    DataRow[] row2 = dtCheckDetail.Select("IsNecessary=0 and (CheckConclusion='不合格' OR CheckConclusion='不符合')");

                    jiance.GeneralItem = row2.Count();
                    if (doc.Range.FormFields["PO_BuHeGe2"] != null)
                    {
                        FormField markCheckDate = doc.Range.FormFields["PO_BuHeGe2"];
                        markCheckDate.Result = row2.Count().ToString();
                        buhegeCountYB = row2.Count();
                    }
                    string ffjiancejieguoResult = "不合格";
                    if (jixieInfo.MachineryCheckState == MachineryCheckStateEnum.检测中)
                    {



                        baogaojieguo = MachineryCheckStateEnum.检测不合格;
                        if (jiance.GuaranteeItem > 0)
                        {

                            ffjiancejieguoResult = "不合格";
                            baogaojieguo = MachineryCheckStateEnum.检测不合格;
                        }
                        else if (jiance.GeneralItem < 5 && jixieInfo.MachineryType == MachineryTypeEnum.塔式起重机)
                        {
                            ffjiancejieguoResult = "合格";
                            baogaojieguo = MachineryCheckStateEnum.检测合格;
                        }
                        else if (jiance.GeneralItem < 4 && jixieInfo.MachineryType == MachineryTypeEnum.施工升降机)
                        {
                            ffjiancejieguoResult = "合格";
                            baogaojieguo = MachineryCheckStateEnum.检测合格;
                        }

                        else if (jiance.GeneralItem < 3 && jixieInfo.MachineryType == MachineryTypeEnum.货运施工升降机)
                        {
                            ffjiancejieguoResult = "合格";
                            baogaojieguo = MachineryCheckStateEnum.检测合格;
                        }
                        else if (jiance.GuaranteeItem == 0 && jiance.GeneralItem == 0)
                        {
                            ffjiancejieguoResult = "合格";
                            baogaojieguo = MachineryCheckStateEnum.检测合格;
                        }


                    }
                    else if (jixieInfo.MachineryCheckState == MachineryCheckStateEnum.复检中)
                    {
                        ffjiancejieguoResult = "不合格";
                        baogaojieguo = MachineryCheckStateEnum.复检不合格;
                        if (jiance.GuaranteeItem > 0)
                        {

                            ffjiancejieguoResult = "不合格";
                            baogaojieguo = MachineryCheckStateEnum.复检不合格;
                        }
                        else if (jiance.GeneralItem < 5 && jixieInfo.MachineryType == MachineryTypeEnum.塔式起重机)
                        {
                            ffjiancejieguoResult = "合格";
                            baogaojieguo = MachineryCheckStateEnum.复检合格;
                        }
                        else if (jiance.GeneralItem < 4 && jixieInfo.MachineryType == MachineryTypeEnum.施工升降机)
                        {
                            ffjiancejieguoResult = "合格";
                            baogaojieguo = MachineryCheckStateEnum.复检合格;
                        }
                        else if (jiance.GeneralItem < 3 && jixieInfo.MachineryType == MachineryTypeEnum.货运施工升降机)
                        {
                            ffjiancejieguoResult = "合格";
                            baogaojieguo = MachineryCheckStateEnum.复检合格;
                        }
                        else if (jiance.GuaranteeItem == 0 && jiance.GeneralItem == 0)
                        {

                            ffjiancejieguoResult = "合格";
                            baogaojieguo = MachineryCheckStateEnum.复检合格;
                        }
                        if (checkConfig.InspectionBasis.Contains("DGJ32/J65"))
                        {
                            if (ffjiancejieguoResult == "合格")
                            {
                                ffjiancejieguoResult = "复检合格";
                            }
                            else if (ffjiancejieguoResult == "不合格")
                            {
                                ffjiancejieguoResult = "复检不合格";
                            }
                        }
                    }
                    dic.Add("QianZhang", ffjiancejieguoResult);
                    foreach (DataRow item in dtCheckDetail.Rows)
                    {
                        if (item["OrderBy"].ToString() == "")
                        {
                            continue;
                        }
                        int orderbyIndex = Convert.ToInt32(item["OrderBy"].ToString()) - 1;

                        dic.Add("CheckResult" + orderbyIndex, item["CheckResult"].ToString());
                        dic.Add("CheckConclusion" + orderbyIndex, item["CheckConclusion"].ToString());

                    }
                }

                if (checkdeatailcustom.Count > 0)
                {
                    for (int i = 0; i < checkdeatailcustom.Count; i++)
                    {
                        dic.Add("CustomTestContent" + (i + 1), checkdeatailcustom[i].TestContent);
                        dic.Add("CustomCheckResult" + (i + 1), checkdeatailcustom[i].CheckResult);
                        dic.Add("CheckCustomConclusion" + (i + 1), checkdeatailcustom[i].CheckConclusion);
                    }
                }


                var newName = jixieInfo.MachineryInfoId + ".pdf";
                fileName = new DataBll(_context).BuildPdfToServer(_environment, doc, dic, "LargeMachinery/JianCeBaoGao", newName, Request, now);

                if (string.IsNullOrEmpty(fileName))
                {
                    return ResponseViewModel<LookWordCheckRecordViewModel>.Create(Status.ERROR, "生成报告错误");
                }

                jiance.TestingInstituteInfoId = testingId;
                jiance.PreviewReportUrl = fileName + "?aa=" + suijisun;
                jiance.PreviewReportMachineryCheckState = baogaojieguo.GetHashCode();
                jiance.ReviewUserId = userId;
                _context.CheckRecords.Update(jiance);

                LookWordCheckRecordViewModel returnmodel = new LookWordCheckRecordViewModel();
                returnmodel.PreviewReportUrl = fileName + "?aa=" + suijisun;
                returnmodel.MachineryInfoId = paraModel.MachineryInfoId;
                returnmodel.PreviewReportMachineryCheckState = baogaojieguo.GetHashCode();
                returnmodel.CheckRecordId = paraModel.CheckRecordId;

                await _context.SaveChangesAsync();
                return ResponseViewModel<LookWordCheckRecordViewModel>.Create(Status.SUCCESS, Message.SUCCESS, returnmodel);

            }
            catch (Exception ex)
            {
                return ResponseViewModel<LookWordCheckRecordViewModel>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 确认报告
        /// </summary>
        /// <param name="paraModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> ConfirmWordsNew(LookWordCheckRecordViewModel paraModel)
        {
            try
            {
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;

                var userName = User.FindFirst(ClaimsIdentity.DefaultNameClaimType)?.Value;//当前人名字
                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;


                if (string.IsNullOrEmpty(testingId) && paraModel.IsLook == 0)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "当前角色不可操作");
                }
                if (paraModel == null
                    || string.IsNullOrWhiteSpace(paraModel.MachineryInfoId)
                     || string.IsNullOrWhiteSpace(paraModel.CheckRecordId)
                     || string.IsNullOrWhiteSpace(paraModel.PreviewReportUrl)
                     || paraModel.IsLook < 0 || paraModel.IsLook > 0
                    || paraModel.PreviewReportMachineryCheckState <= 0)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
                }


                DateTime now = DateTime.Now;
                Random nn = new Random();
                int suijisun = nn.Next(1, 300);
                var jiance = await _context.CheckRecords.Where(s => s.DeleteMark == 0
                    && s.MachineryInfoId == paraModel.MachineryInfoId && s.CheckRecordId == paraModel.CheckRecordId).FirstOrDefaultAsync();
                if (jiance == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"机械委托检测记录不存在或已被删除，无法进行检测");
                }

                var jixieInfo = await _context.MachineryInfos
                            .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == paraModel.MachineryInfoId
                                && (w.MachineryCheckState == MachineryCheckStateEnum.检测中
                                  || w.MachineryCheckState == MachineryCheckStateEnum.复检中))
                            .OrderByDescending(o => o.Id)
                            .FirstOrDefaultAsync();
                if (jixieInfo == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"机械不存在或已被检测，无需重复操作");

                }



                jixieInfo.CheckUrl = paraModel.PreviewReportUrl;
                jixieInfo.MachineryCheckState = (MachineryCheckStateEnum)paraModel.PreviewReportMachineryCheckState;
                jixieInfo.UpdateDate = now;
                jixieInfo.CheckReviewDate = now;
                jixieInfo.ReCheckReviewDate = now;
                jiance.RecordUrl = paraModel.PreviewReportUrl;
                jiance.CheckState = (MachineryCheckStateEnum)paraModel.PreviewReportMachineryCheckState;
                jiance.UpdateDate = now;
                jiance.ReviewUserId = userId;
                _context.MachineryInfos.Update(jixieInfo);
                _context.CheckRecords.Update(jiance);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);

            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// 撤回检测报告
        /// </summary>
        /// <param name="paraModel"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<string>> RecallCheck(string machineryInfoId, string checkRecordId)
        {
            try
            {
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;

                var userName = User.FindFirst(ClaimsIdentity.DefaultNameClaimType)?.Value;//当前人名字
                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;


                if (string.IsNullOrEmpty(testingId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "当前角色不可操作");
                }
                if (string.IsNullOrWhiteSpace(machineryInfoId)
                     || string.IsNullOrWhiteSpace(checkRecordId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
                }


                DateTime now = DateTime.Now;
                Random nn = new Random();
                int suijisun = nn.Next(1, 300);
                var jiance = await _context.CheckRecords.Where(s => s.DeleteMark == 0
                    && s.MachineryInfoId == machineryInfoId && s.CheckRecordId == checkRecordId).FirstOrDefaultAsync();
                if (jiance == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"机械委托检测记录不存在或已被删除，无法撤回");
                }

                var jixieInfo = await _context.MachineryInfos
                            .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == machineryInfoId)
                            .OrderByDescending(o => o.Id)
                            .FirstOrDefaultAsync();
                if (jixieInfo == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"机械不存在，无法撤回");

                }

                if (jixieInfo.MachineryCheckState == MachineryCheckStateEnum.复检合格 || jixieInfo.MachineryCheckState == MachineryCheckStateEnum.复检不合格)
                {
                    jixieInfo.MachineryCheckState = MachineryCheckStateEnum.复检中;
                    jiance.CheckState = MachineryCheckStateEnum.检测中;
                }
                else if (jixieInfo.MachineryCheckState == MachineryCheckStateEnum.检测合格 || jixieInfo.MachineryCheckState == MachineryCheckStateEnum.检测不合格)
                {
                    jixieInfo.MachineryCheckState = MachineryCheckStateEnum.检测中;
                    jiance.CheckState = MachineryCheckStateEnum.检测中;
                }
                jixieInfo.CheckUrl = "";
                jixieInfo.UpdateDate = now;
                jixieInfo.CheckReviewDate = null;
                jixieInfo.ReCheckReviewDate = null;
                jiance.UpdateDate = now;
                jiance.ReviewUserId = null;
                _context.MachineryInfos.Update(jixieInfo);
                _context.CheckRecords.Update(jiance);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);

            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }




        ///// <summary>
        ///// 自有平台上传检测结果接口
        ///// </summary>
        ///// <param name="paraModel"></param>
        ///// <returns></returns>
        //[HttpPost]
        //public async Task<ResponseViewModel<string>> UploadCheckResult(UploadCheckResultViewModel uploadCheckResultViewModel)
        //{
        //    try
        //    {
        //        DateTime now = DateTime.Now;
        //        Random nn = new Random();
        //        int suijisun = nn.Next(1, 300);

        //        if (uploadCheckResultViewModel == null
        //            || (uploadCheckResultViewModel.MachineryCheckState != MachineryCheckStateEnum.检测合格
        //           && uploadCheckResultViewModel.MachineryCheckState != MachineryCheckStateEnum.检测不合格)
        //            || string.IsNullOrEmpty(uploadCheckResultViewModel.CheckUrl)
        //            || uploadCheckResultViewModel.CheckReviewDate == null
        //            )
        //        {
        //            return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
        //        }

        //        var jixieInfo = await _context.MachineryInfos.Where(s => s.PropertyRightsRecordNo == uploadCheckResultViewModel.PropertyRightsRecordNo).FirstOrDefaultAsync();
        //        if (jixieInfo == null)
        //        {
        //            return ResponseViewModel<string>.Create(Status.FAIL, "机械不存在");

        //        }
        //        CheckRecordThirdParty model = new CheckRecordThirdParty();
        //        model.ReviewUserName = uploadCheckResultViewModel.ReviewUserName;//检测人
        //        model.CheckRecordThirdPartyId = SecurityManage.GuidUpper();
        //        model.MachineryInfoId = jixieInfo.MachineryInfoId;
        //        model.TestingInstituteInfoName = uploadCheckResultViewModel.TestingInstituteInfoName;
        //        model.DeleteMark = 0;
        //        model.CheckState = uploadCheckResultViewModel.MachineryCheckState;
        //        model.CreateDate = now;
        //        model.UpdateDate = now;
        //        model.SubmitUserName = uploadCheckResultViewModel.SubmitUserName;
        //        model.CheckRectificationReamrk = uploadCheckResultViewModel.CheckRectificationReamrk;
        //        model.CheckReamrk = uploadCheckResultViewModel.CheckReamrk;
        //        _context.CheckRecordThirdPartys.Add(model);
        //        jixieInfo.IsOwnCheck = 1;
        //        jixieInfo.ChangeRecordId = model.CheckRecordThirdPartyId;
        //        jixieInfo.CheckUrl = uploadCheckResultViewModel.CheckUrl;
        //        jixieInfo.UpdateDate = now;
        //        jixieInfo.CheckReviewDate = uploadCheckResultViewModel.CheckReviewDate;
        //        jixieInfo.ReCheckReviewDate = uploadCheckResultViewModel.CheckReviewDate;
        //        _context.MachineryInfos.Update(jixieInfo);
        //        await _context.SaveChangesAsync();
        //        return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);

        //    }
        //    catch (Exception ex)
        //    {
        //        return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
        //    }
        //}



    }
}