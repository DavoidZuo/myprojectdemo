//κ��Դ 2019.12.2
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Transactions;
using Aspose.Words;
using JSAJ.Core.Common;
using MCUtil.Security;
using JSAJ.Core.Models.LargeMachinery;
using JSAJ.Core.ViewModels.LargeMachinery;
using JSAJ.Core.Common.DataBll;
using JSAJ.Core.Common.QRCodes;
using JSAJ.ViewModels.ViewModels;
using Microsoft.Extensions.Configuration;
using NPOI.OpenXml4Net.OPC;
using NPOI.SS.Formula.Functions;
using System.Security.Cryptography;
using System.Security.Claims;
using JSAJ.ViewModels.ViewModels.manager;
using Newtonsoft.Json;
using JSAJ.Models.Models.Login;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Common;
using ViewModels;
using Aspose.Words.Drawing;

namespace JSAJ.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    //[Authorize]


    public class MachineryController : ControllerBase
    {
        public IConfiguration Configuration { get; }
        private readonly JssanjianmanagerContext _context;
        private readonly ILogger<MachineryController> _logger;
        private readonly string _wordTemplte;
        private readonly string _buildWords;
        private readonly IWebHostEnvironment _environment;
        private OssFileSetting _ossFileSetting;
        private IQRCode _iqrcode;
        private readonly Jsgginterface _jsgginterface;
        public MachineryController(IWebHostEnvironment environment, ILogger<MachineryController> logger, JssanjianmanagerContext context
            , IOptions<OssFileSetting> oss, IQRCode qRCode, IConfiguration configuration, IOptions<Jsgginterface> ntoptionjsgginterface)
        {
            _environment = environment;
            _context = context;
            _logger = logger;
            _buildWords = environment.WebRootPath + Path.DirectorySeparatorChar + "BuildPdf" + Path.DirectorySeparatorChar;
            _wordTemplte = environment.WebRootPath + Path.DirectorySeparatorChar + "doc" + Path.DirectorySeparatorChar;
            _ossFileSetting = oss.Value;
            _iqrcode = qRCode;
            Configuration = configuration;
            _jsgginterface = ntoptionjsgginterface.Value;
        }


        #region ��Ȩ��Ϣ���
        /// <summary>
        /// ��Ȩ�����걨�б�
        /// </summary>
        /// <param name="total">ҳ��</param>
        /// <param name="size">ÿҳ����</param>
        /// <param name="machineryType">��е���</param>
        /// <param name="oem">��������</param>
        /// <param name="leaveTheFactoryNo">�������</param>
        /// <param name="manufacturingLicense">�������֤��</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<MachineryInfoViewModel>>> MachineryList(int total, int size, int machineryType = -1, string oem = "",
            string leaveTheFactoryNo = "", string manufacturingLicense = "")
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id     

                var mlist = await _context.MachineryInfos.Where(w => w.DeleteMark == 0 && w.EntGUID == tokenId && w.CancellationState != CancellationStateEnum.���ͨ��
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
                //��ȡ���ղ�����б�����
                var totalCount = mlist.Count;
                //������Դ��ҳ
                mlist = mlist.OrderByDescending(o => o.CreateDate).Skip((total - 1) * size).Take(size).ToList();
                return ResponseViewModel<List<MachineryInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, mlist, totalCount);




            }
            catch (Exception ex)
            {

                return ResponseViewModel<List<MachineryInfoViewModel>>.Create(Status.ERROR, Message.ERROR);
            }

        }
        /// <summary>
        /// ��ȡ����
        /// </summary>
        /// <param name="total">ҳ��</param>
        /// <param name="size">ÿҳ����</param>
        /// <param name="projectName">��Ŀ����</param>
        /// <param name="machineryType">�豸����</param>
        /// <param name="machineryModel">�豸�ͺ�</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<MachineryListViewModel>>> GetMachineryModel(int total, int size, string? projectName, int? machineryType, string? machineryModel)
        {
            try
            {
                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//�����˿���
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                IQueryable<MachineryInfo> list = null;
                list = _context.MachineryInfos.Where(s => s.CheckState == MachineryState.����ʹ�õǼ�ͨ�� && s.UseReviewBelongedTo == belongedTo && s.DeleteMark == 0);

                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    list = list.Where(s => s.ProjectName.Contains(projectName));
                }
                if (machineryType != null)
                {
                    list = list.Where(s => s.MachineryType == (MachineryTypeEnum)machineryType);
                }
                //�޸�Ϊ�豸��Ϣ��
                if (!string.IsNullOrWhiteSpace(machineryModel))
                {
                    list = list.Where(s => s.PropertyRightsRecordNo == machineryModel);
                }
                if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                {
                    list = from A in list
                           join B in _context.ProjectOverview
                            on new { A.BelongedTo, A.RecordNumber } equals new { B.BelongedTo, B.RecordNumber }
                           where B.SupervisionDepartmentId == supervisionDepartmentId
                           select A;
                }
                if (list.Count() == 0)
                {
                    return ResponseViewModel<List<MachineryListViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<MachineryListViewModel>(), list.Count());
                }
                if (total == 0)
                {
                    total = 1;
                }
                if (size == 0)
                {
                    size = 10;
                }
                var model = list.OrderByDescending(o => o.UpdateDate)
                    .Skip((total - 1) * size)
                    .Take(size)
                    .ToList();
                List<MachineryListViewModel> lists = new List<MachineryListViewModel>();
                var a = 0;
                model.ForEach(x =>
                 {
                     MachineryListViewModel mode = new MachineryListViewModel();
                     mode.RecordNumber = x.RecordNumber;
                     mode.ProjectName = x.ProjectName;
                     mode.MachineryType = x.MachineryType;
                     mode.MachineryTypeName = x.MachineryType.ToString();
                     mode.PropertyRightsRecordNo = x.PropertyRightsRecordNo;
                     mode.UseRecordNo = x.UseRecordNo;
                     mode.EntName = x.EntName;
                     mode.UseSubmitDate = x.UseSubmitDate;
                     mode.MachineryInfoId = x.MachineryInfoId;
                     mode.RegistrationOfUseId = x.RegistrationOfUseId;
                     mode.MaxRatedLiftingCapacity = x.MaxRatedLiftingCapacity;
                     mode.Knm = x.Knm;
                     mode.FreeStandingHeight = x.FreeStandingHeight;
                     mode.AttachedHeight = x.AttachedHeight;
                     mode.MaxRange = x.MaxRange;
                     mode.BuyDate = x.BuyDate;
                     lists.Add(mode);
                 });
                return ResponseViewModel<List<MachineryListViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, lists, lists.Count);

            }
            catch (Exception ex)
            {
                return ResponseViewModel<List<MachineryListViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// ��ȡ���л�е�豸
        /// </summary>
        /// <param name="propertyRightsRecordNo">�豸��Ϣ��</param>
        /// <param name="leaveTheFactoryNo">�������</param>
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
                //list = _context.MachineryInfos.Where(s => s.CancellationState == CancellationStateEnum.���ͨ��||s.CheckState== MachineryState.ʹ�õǼ�ע�����ͨ��);
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
        /// �༭����Դ
        /// </summary>
        /// <param name="machineryInfoId">��еID</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<MachineryInfoViewModel>> EditMachineryList(string machineryInfoId)
        {
            try
            {
                //EntCode   
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id     
                var mlist = await _context.MachineryInfos.Where(w => w.DeleteMark == 0 && w.EntGUID == tokenId
                && w.MachineryInfoId == machineryInfoId)
                                  .Select(s => new MachineryInfoViewModel
                                  {
                                      MachineryInfoId = s.MachineryInfoId,
                                      MachineryType = s.MachineryType,
                                      MachineryName = s.MachineryName,
                                      MachineryModel = s.MachineryModel,
                                      OEM = s.OEM,
                                      ManufacturingLicense = s.ManufacturingLicense,
                                      LeaveTheFactoryDate = s.LeaveTheFactoryDate,
                                      LeaveTheFactoryNo = s.LeaveTheFactoryNo,
                                      State = s.State,
                                      MaxRatedLiftingCapacity = s.MaxRatedLiftingCapacity,
                                      Knm = s.Knm,
                                      MaxInstallHeight = s.MaxInstallHeight,
                                      FreeStandingHeight = s.FreeStandingHeight,
                                      AttachedHeight = s.AttachedHeight,
                                      MaxRange = s.MaxRange,
                                      BuyDate = s.BuyDate,
                                      IsSafetyFile = s.IsSafetyFile,
                                      IsSecondHand = s.IsSecondHand
                                  }).FirstOrDefaultAsync();



                return ResponseViewModel<MachineryInfoViewModel>.Create(Status.SUCCESS, Message.SUCCESS, mlist);
            }
            catch (Exception ex)
            {

                return ResponseViewModel<MachineryInfoViewModel>.Create(Status.ERROR, Message.ERROR);
            }


        }

        /// <summary>
        /// ���/�༭�����Ȩ��Ϣ
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> AddUpMachineryInfo([FromBody] MachineryInfoViewModel viewModel)
        {

            try
            {


                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id                                                          



                var canLease = await _context.EntRegisterInfoMag.Where(w => w.EntRegisterInfoMagId
                 == tokenId).FirstOrDefaultAsync();



                //���o������Ϣ 
                if (string.IsNullOrWhiteSpace(viewModel.MachineryInfoId))
                {
                    var machineryInfos = await _context.MachineryInfos
                .Where(w => w.DeleteMark == 0 && w.ManufacturingLicense == viewModel.ManufacturingLicense
                && w.LeaveTheFactoryNo == viewModel.LeaveTheFactoryNo)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();
                    if (machineryInfos != null && machineryInfos.IsNoAction == 1)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "�û�е���������ѱ��������޷��ڻ�еϵͳ������");
                    }

                    //�Ƿ�֮ǰ���ڹ�
                    if (machineryInfos == null || machineryInfos.CancellationState == CancellationStateEnum.���ͨ��)
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

                        //���ͼƬ
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

                            return ResponseViewModel<string>.Create(Status.FAIL, "�û�е�Ѿ����������������걨��", "�û�е�Ѿ����������������걨��");
                        }
                        else
                        {

                            return ResponseViewModel<string>.Create(Status.FAIL, "�û�е�����걨�У����������걨��", "�û�е�����걨�У����������걨��");
                        }

                    }

                }
                //�༭������Ϣ
                else
                {
                    var machineryInfoEdit = await _context.MachineryInfos.Where(x => x.MachineryInfoId == viewModel.MachineryInfoId
                  ).FirstOrDefaultAsync();
                    if (machineryInfoEdit == null)
                    {
                        return ResponseViewModel<string>.Create(Status.ERROR, "��ǰ��е״̬���ɱ༭");
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
                    machineryInfoEdit.CancellationState = CancellationStateEnum.δ�ύ;
                    machineryInfoEdit.IsSecondHand = viewModel.IsSecondHand;
                    machineryInfoEdit.IsSafetyFile = viewModel.IsSafetyFile;
                    _context.MachineryInfos.Update(machineryInfoEdit);

                    //��ɾ������ͼƬ
                    var delFileList = _context.RecordAttachments
                        .Where(w => w.DeleteMark == 0 && w.AttachmentId == viewModel.MachineryInfoId)
                        .ToList();

                    delFileList.ForEach(g =>
                    {
                        g.DeleteMark = 1;

                        _context.RecordAttachments.UpdateRange(delFileList);

                    });

                    //Ȼ�����
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
        /// ɾ����Ȩ��Ϣ
        /// </summary>
        /// <param name="machineryInfoId">��еID</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> DelMachineryInfo([FromQuery] string machineryInfoId)
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id   

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
        /// �ύ����վ���ڵ�
        /// </summary>
        /// <param name="parentCityCode">����վ��ID</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<CityInfoViewModel>>> CityTree(string parentCityCode, int isShowXieHui)
        {
            try
            {

                if (parentCityCode == "320000")
                {
                    CityInfoViewModel xiehui = new CityInfoViewModel();
                    xiehui.ParentCityCode = "";
                    xiehui.CityShortName = "";
                    xiehui.CityLongName = "";
                    xiehui.CityCode = "AAAAA";
                    xiehui.BelongedTo = "AAAAAA-1";
                    xiehui.SuperOrganName = "����ʡ������ҵЭ��";

                    List<CityInfoViewModel> allList = new List<CityInfoViewModel>();
                    allList.Add(xiehui);
                    var cityList = await _context.CityZone.Where(w => w.ParentCityCode == parentCityCode && w.CountyCompartment != null && w.CityCompartment != null)
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
                                CityLongName = s.Key.CityLongName + "����վ",
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

                    if (parentCityCode == "AAAAA")
                    {
                        CityInfoViewModel xiehui = new CityInfoViewModel();
                        xiehui.ParentCityCode = "AAAAA";
                        xiehui.CityShortName = "";
                        xiehui.CityLongName = "";
                        xiehui.CityCode = "";
                        xiehui.BelongedTo = "AJ320000-1";
                        xiehui.SuperOrganName = "����ʡ������ҵЭ�Ὠ����ȫ�豸����ֻ�";
                        allList.Add(xiehui);
                    }
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
                                        CityLongName = s.Key.CityLongName + "����վ",
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
        /// ���ݰ���վ���ֲ�ѯ
        /// </summary>
        /// <param name="parentCityCode">����վ��ID</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<CityInfoViewModel>>> CitySelect(string SuperOrganName)
        {
            try
            {


                var cityList = await _context.CityZone.Where(w => w.SuperOrganName.Contains(SuperOrganName) ||
                w.BelongedTo.Contains(SuperOrganName)).Select(s => new CityInfoViewModel
                {
                    ParentCityCode = s.ParentCityCode,
                    CityShortName = s.CityShortName,
                    CityLongName = s.CityLongName,
                    BelongedTo = s.BelongedTo,
                    SuperOrganName = s.SuperOrganName,
                    CityCode = s.CityCode,
                }).ToListAsync();


                return ResponseViewModel<List<CityInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, cityList);


            }
            catch (Exception ex)
            {

                return ResponseViewModel<List<CityInfoViewModel>>.Create(Status.ERROR, Message.ERROR);
            }

        }




        /// <summary>
        /// �ļ��ϴ��б�
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
                    return ResponseViewModel<List<MachineryConfigView>>.Create(Status.ERROR, "�뵽��λ��Ϣά���˵��ϴ�Ӫҵִ��!");
                }
                var recordAttments = await _context.RecordAttachments.Where(w => w.DeleteMark == 0
                 && w.AttachmentId == machineryInfoId).ToListAsync();
                var recordNumberConfig = await _context.RecordConfigs.Where(w => w.DeleteMark == 0
                && w.Type == RecordConfigTypeEnum.��Ȩ����).OrderBy(x => x.Sort).ToListAsync();
                List<MachineryConfigView> mConfigList = new List<MachineryConfigView>();
                recordNumberConfig.ForEach(x =>
                {
                    MachineryConfigView item = new MachineryConfigView();
                    item.AttachmentName = x.AttachmentName;
                    item.RecordConfigId = x.RecordConfigId;
                    item.TemplateUrl = x.TemplateUrl;
                    item.Required = x.Required;
                    item.FileCount = recordAttments.Where(w => w.RecordConfigId == x.RecordConfigId).Count();
                    if (item.FileCount > 0 && item.AttachmentName != "��Ȩ��λӪҵִ��")
                    {
                        item.ReaderOnly = false;
                        item.FileUrl = recordAttments.Where(y => y.RecordConfigId == x.RecordConfigId && y.Type == RecordConfigTypeEnum.��Ȩ����)
                                        .Select(y => new FileUploadUrlViem
                                        {
                                            FileType = x.AttachmentName,
                                            RecordConfigId = y.RecordConfigId,
                                            FileName = y.FileName,
                                            FileUrl = y.FileUrl,
                                            Suffix = Path.GetExtension(y.FileName).Replace(".", "")
                                        }).ToList();
                    }
                    else if (item.AttachmentName == "��Ȩ��λӪҵִ��")
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
        /// �����ϴ�ͼƬ�Ĳ���
        /// </summary>
        /// <param name="iform"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseViewModel<List<RecordAttachment>> MachineryUpFile([FromForm] IFormCollection iform)
        {
            try
            {

                List<RecordAttachment> rlist = new List<RecordAttachment>();
                //�õ�����
                var recordConfigId = iform["RecordConfigId"].ToString();
                if (string.IsNullOrWhiteSpace(recordConfigId)
                    )
                {
                    return ResponseViewModel<List<RecordAttachment>>.Create(Status.FAIL, "��������");
                }

                if (iform.Files.Count != 1)
                {
                    return ResponseViewModel<List<RecordAttachment>>.Create(Status.FAIL, "���ϴ�һ���ļ�");
                }

                var file = iform.Files[0];
                var fileName = file.FileName;
                var url = Util.UploadFileToServer(file, _environment, Request, "MachineryImg");

                foreach (var item in iform.Files)
                {
                    RecordAttachment rModel = new RecordAttachment()
                    {
                        FileName = item.FileName,
                        FileUrl = Util.UploadFileToServer(file, _environment, Request, "MachineryImg"),
                        RecordConfigId = recordConfigId,

                    };
                    rlist.Add(rModel);
                }


                return ResponseViewModel<List<RecordAttachment>>.Create(Status.SUCCESS, Message.SUCCESS, rlist);

            }
            catch (Exception ex)
            {

                _logger.LogError("�ϴ���Ȩ����ͼƬ��" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<RecordAttachment>>.Create(Status.ERROR, "�ϴ���Ȩ����ͼƬ" + ex.Message + ex.StackTrace);
            }

        }

        ///// <summary>
        ///// �ϴ���ʷ��¼
        ///// </summary>
        ///// <param name="machineryInfoId"></param>
        ///// <param name="recordConfigId"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet]
        //public async Task<ResponseViewModel<List<MachineryFileHisViewModel>>> MachineryFileHisList(string machineryInfoId, string recordConfigId)
        //{

        //    try
        //    {
        //        if (recordConfigId == "1")
        //        {

        //            var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id    
        //            List<MachineryFileHisViewModel> lists = new List<MachineryFileHisViewModel>();

        //            var malist = await _context.EntRegisterInfoMag.Where(w => w.EntRegisterInfoMagId == tokenId)
        //            .Select(s => new MachineryFileHisViewModel
        //            {

        //                FileUrl = s.EntCodeImg,

        //            }).FirstOrDefaultAsync();
        //            lists.Add(malist);
        //            if (malist.FileUrl == null)
        //            {
        //                return ResponseViewModel<List<MachineryFileHisViewModel>>.Create(Status.SUCCESS, Message.SUCCESS);
        //            }

        //            return ResponseViewModel<List<MachineryFileHisViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, lists);
        //        }
        //        else
        //        {
        //            var malist = await _context.RecordAttachments.Where(w => w.DeleteMark == 0 && w.AttachmentId == machineryInfoId
        //                           && w.RecordConfigId == recordConfigId && w.Type == (RecordConfigTypeEnum)0).Select(s => new MachineryFileHisViewModel
        //                           {
        //                               FileName = s.FileName,
        //                               FileUrl = s.FileUrl,
        //                               RecordAttachmentId = s.RecordAttachmentId,
        //                               CreateDate = s.CreateDate,
        //                               FileType = s.FileUrl.Substring(s.FileUrl.Length - 3, 3)
        //                           }).OrderByDescending(o => o.CreateDate).ToListAsync();
        //            return ResponseViewModel<List<MachineryFileHisViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, malist, malist.Count());

        //        }



        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError("�ϴ���Ȩ����ͼƬ��" + ex.Message + ex.StackTrace, ex);
        //        return ResponseViewModel<List<MachineryFileHisViewModel>>.Create(Status.ERROR, Message.ERROR);
        //    }

        //}


        /// <summary>
        /// �ϴ���ʷ��¼
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
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id    
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

                if (recordConfigId == "1" || imgType.AttachmentName == "��Ȩ��λӪҵִ��")
                {
                    List<MachineryFileHisViewModel> lists = new List<MachineryFileHisViewModel>();
                    MachineryFileHisViewModel malist = new MachineryFileHisViewModel();
                    malist.RecordConfigId = _context.RecordConfigs.Where(x => x.DeleteMark == 0 && x.AttachmentName == "��Ȩ��λӪҵִ��" && x.Type == RecordConfigTypeEnum.��Ȩ����).Select(x => x.RecordConfigId).FirstOrDefault();
                    malist.FileType = "��Ȩ��λӪҵִ��";
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
                                         && a.Type == RecordConfigTypeEnum.��Ȩ����
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
                        if (x.FileType == "��Ȩ��λӪҵִ��")
                        {
                            x.FileType = "��Ȩ��λӪҵִ��";
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

                _logger.LogError("�ϴ���Ȩ����ͼƬ��" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<MachineryFileHisViewModel>>.Create(Status.ERROR, Message.ERROR);
            }

        }

        #endregion

        #region ��Ȩ��Ϣ���

        /// <summary>
        /// ��Ȩ�������
        /// </summary>
        /// <param name="entCode"></param>
        /// <param name="total"></param>
        /// <param name="size"></param>
        /// <param name="machineryName"></param>
        /// <param name="oem"></param>
        /// <param name="leaveTheFactoryNo"></param>
        /// <param name="manufacturingLicense"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<UpMachInfoViewModel>>> UpDateMachineryList(int total, int size, int machinerytype = -1, string machineryName = "",
        string leaveTheFactoryNo = "", string manufacturingLicense = "", string propertyRightsRecordNo = "")
        {
            try
            {

                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id    

                var entInfo = await _context.EntRegisterInfoMag.Where(w => w.EntRegisterInfoMagId == tokenId).FirstOrDefaultAsync();



                var changModel = await _context.ChangeOfTitles.Where(w => w.DeleteMark == 0 && w.AfterEntCode == entInfo.EntCode).ToListAsync();

                var machineryModel = await _context.MachineryInfos.Where(w => w.DeleteMark == 0 && w.CancellationState == (CancellationStateEnum)2).ToListAsync();

                var mlist = (from cm in changModel
                             join mm in machineryModel
                             on cm.MachineryInfoId equals mm.MachineryInfoId
                             where mm.MachineryName.Contains(machineryName) && mm.LeaveTheFactoryNo.Contains(leaveTheFactoryNo)
                       && mm.ManufacturingLicense.Contains(manufacturingLicense)
                             select new UpMachInfoViewModel
                             {
                                 ChangeOfTitleId = cm.ChangeOfTitleId,
                                 MachineryInfoId = cm.MachineryInfoId,
                                 PropertyRightsRecordNo = mm.PropertyRightsRecordNo,
                                 MachineryType = mm.MachineryType,
                                 MachineryName = mm.MachineryName,
                                 MachineryModel = mm.MachineryModel,
                                 ManufacturingLicense = mm.ManufacturingLicense,
                                 LeaveTheFactoryNo = mm.LeaveTheFactoryNo,
                                 BeforeEntName = cm.BeforeEntName,
                                 BeforeEntCode = cm.BeforeEntCode,
                                 AfterEntName = cm.AfterEntName,
                                 AfterEntCode = cm.AfterEntCode,
                                 LeaveTheFactoryDate = mm.LeaveTheFactoryDate,
                                 AggrementUrl = cm.AggrementUrl,
                                 AggrementSuffix = cm.AggrementUrl.Substring(cm.AggrementUrl.Length - 3, 3),
                                 ProofUrl = cm.ProofUrl,
                                 ProofSuffix = cm.ProofUrl.Substring(cm.ProofUrl.Length - 3, 3),
                                 State = cm.State,
                                 ChangeDate = cm.ChangeDate,
                                 Remark = cm.Remark,
                                 Reason = cm.Reason,
                                 MaxRatedLiftingCapacity = mm.MaxRatedLiftingCapacity,
                                 Knm = mm.Knm,
                                 MaxInstallHeight = mm.MaxInstallHeight,
                                 FreeStandingHeight = mm.FreeStandingHeight,
                                 AttachedHeight = mm.AttachedHeight,
                                 MaxRange = mm.MaxRange,
                                 BuyDate = mm.BuyDate,
                                 CreateDate = mm.CreateDate,
                                 ReviewBelongedTo = cm.BelongedTo == null ? "" : _context.CityZone.Where(w => w.BelongedTo == cm.BelongedTo)
                                 .Select(s => s.SuperOrganName).FirstOrDefault()

                             }).ToList();



                if (propertyRightsRecordNo != "")
                {
                    mlist = mlist.Where(w => w.PropertyRightsRecordNo.Contains(propertyRightsRecordNo)).ToList();

                }


                if (machinerytype != -1)
                {
                    mlist = mlist.Where(w => w.MachineryType.GetHashCode() == machinerytype).ToList();

                }
                var totalCount = mlist.Count;
                //������Դ��ҳ
                mlist = mlist.OrderByDescending(o => o.CreateDate).Skip((total - 1) * size).Take(size).ToList();

                return ResponseViewModel<List<UpMachInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, mlist, totalCount);

            }
            catch (Exception ex)
            {

                return ResponseViewModel<List<UpMachInfoViewModel>>.Create(Status.ERROR, Message.ERROR);
            }

        }

        /// <summary>
        /// ��е������
        /// </summary>
        /// <param name="machineryName"></param>
        /// <param name="machineryModel"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<MachineryInfo>>> SelectboxInfo(string manufacturingLicense,
            string leaveTheFactoryNo, string propertyRightsRecordNo)
        {
            try
            {

                var machineryInfo = await _context.MachineryInfos.Where(w => (w.ManufacturingLicense.Contains(manufacturingLicense)
           || w.LeaveTheFactoryNo.Contains(leaveTheFactoryNo) || w.PropertyRightsRecordNo.Contains(propertyRightsRecordNo))
           && w.DeleteMark == 0 && w.CancellationState == (CancellationStateEnum)2).ToListAsync();


                return ResponseViewModel<List<MachineryInfo>>.Create(Status.SUCCESS, Message.SUCCESS, machineryInfo);
            }
            catch (Exception ex)
            {

                return ResponseViewModel<List<MachineryInfo>>.Create(Status.ERROR, Message.ERROR);
            }

        }


        /// <summary>
        /// �����豸���ƻ��߱�ż���ԭ����λ��Ϣ
        /// </summary>
        /// <param name="machineryName"></param>
        /// <param name="machineryModel"></param>
        /// <returns></returns>
        [HttpGet]
        //[Authorize]
        public async Task<ResponseViewModel<UpMachInfoViewModel>> BeforeInfo(string manufacturingLicense,
            string leaveTheFactoryNo, string propertyRightsRecordNo)
        {
            try
            {
                var machineryInfo = await _context.MachineryInfos.Where(w => (w.ManufacturingLicense == manufacturingLicense
                || w.LeaveTheFactoryNo == leaveTheFactoryNo || w.PropertyRightsRecordNo == propertyRightsRecordNo)
                && w.CancellationState == (CancellationStateEnum)2)
                    .FirstOrDefaultAsync();

                if (machineryInfo == null)
                {
                    return ResponseViewModel<UpMachInfoViewModel>.Create(Status.FAIL, "���޸û�е");
                }

                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id    

                var afterEntInfo = await _context.EntRegisterInfoMag.Where(w => w.EntRegisterInfoMagId
                 == tokenId).FirstOrDefaultAsync();

                var beforeEntInfo = await _context.EntRegisterInfoMag.Where(w => w.EntRegisterInfoMagId
                == machineryInfo.EntGUID).FirstOrDefaultAsync();

                UpMachInfoViewModel umModel = new UpMachInfoViewModel()
                {
                    PropertyRightsRecordNo = machineryInfo.PropertyRightsRecordNo,
                    BeforeEntCode = beforeEntInfo.EntCode,
                    BeforeEntName = beforeEntInfo.EntName,
                    AfterEntCode = afterEntInfo.EntCode,
                    AfterEntName = afterEntInfo.EntName,
                    MachineryInfoId = machineryInfo.MachineryInfoId,
                    ManufacturingLicense = machineryInfo.ManufacturingLicense,
                    LeaveTheFactoryNo = machineryInfo.LeaveTheFactoryNo
                };


                return ResponseViewModel<UpMachInfoViewModel>.Create(Status.SUCCESS, Message.SUCCESS, umModel);
            }
            catch (Exception ex)
            {

                return ResponseViewModel<UpMachInfoViewModel>.Create(Status.ERROR, Message.ERROR);
            }

        }

        /// <summary>
        /// �����ϴ�ͼƬ�Ĳ���
        /// </summary>
        /// <param name="iform"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseViewModel<RecordAttachment> UpdateMachineryUpFile([FromForm] IFormCollection iform)
        {
            try
            {


                if (iform.Files.Count != 1)
                {
                    return ResponseViewModel<RecordAttachment>.Create(Status.FAIL, "���ϴ�һ���ļ�");
                }

                var file = iform.Files[0];
                var fileName = file.FileName;
                var url = Util.UploadFileToServer(file, _environment, Request, "MachineryImg");


                RecordAttachment rModel = new RecordAttachment()
                {
                    FileName = fileName,
                    FileUrl = url,
                };

                return ResponseViewModel<RecordAttachment>.Create(Status.SUCCESS, Message.SUCCESS, rModel);

            }
            catch (Exception ex)
            {

                _logger.LogError("�ϴ���Ȩ����ͼƬ��" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<RecordAttachment>.Create(Status.ERROR, "�ϴ���Ȩ����ͼƬ" + ex.Message + ex.StackTrace);
            }

        }



        /// <summary>
        /// �������
        /// </summary>
        /// <param name="iform"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> UpDateAddMachinery(UpMachInfoViewModel viewModel)
        {
            try
            {
                //�жϱ�������ӻ��Ǳ༭

                //���õ�ԭ��Ȩ���ֲ�Ȩһ���ı����¼
                var machineryInfoId = await _context.ChangeOfTitles.Where(w => w.DeleteMark == 0
                && w.BeforeEntName == viewModel.BeforeEntName
                && w.AfterEntName == viewModel.AfterEntName).Select(s => s.MachineryInfoId).ToListAsync();

                var machineryModel = await _context.MachineryInfos.Where(w => w.DeleteMark == 0
                && machineryInfoId.Contains(w.MachineryInfoId) && w.CancellationState == CancellationStateEnum.���ͨ��).ToListAsync();

                var cancellationState = await _context.MachineryInfos.Where(w => w.MachineryInfoId == viewModel.MachineryInfoId)
                    .Select(s => s.CancellationState).FirstOrDefaultAsync();


                if (cancellationState != (CancellationStateEnum)2)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "��Ҫ���豸�Ĳ�Ȩ��λ�Ȱ���ע���Ǽ�", "��Ҫ���豸�Ĳ�Ȩ��λ�Ȱ���ע���Ǽ�");

                }

                //�ж��Ǳ༭�������
                if (string.IsNullOrWhiteSpace(viewModel.ChangeOfTitleId))
                {

                    //var isNull = 0;
                    //�õ�����ԭ��Ȩ���ֲ�Ȩ��ͬ�ı����¼,�Է��������λ�е
                    for (int i = 0; i < machineryModel.Count; i++)
                    {
                        //�жϱ��α���Ļ�е�Ƿ���ڹ�
                        if (machineryModel[i].ManufacturingLicense == viewModel.ManufacturingLicense
                            && machineryModel[i].LeaveTheFactoryNo == viewModel.LeaveTheFactoryNo)
                        {
                            if (machineryModel[i].State == 2)
                            {

                                return ResponseViewModel<string>.Create(Status.FAIL, "�û�е�Ѿ�����ɹ����������±��", "�û�е�Ѿ�����ɹ����������±��");
                            }
                            else if (machineryModel[i].State == 0 || machineryModel[i].State == 1 || machineryModel[i].State == 3)
                            {

                                return ResponseViewModel<string>.Create(Status.FAIL, "�û�е���ڱ���У��������±��", "�û�е���ڱ���У��������±��");
                            }

                        }

                    }

                    //��ӱ����¼
                    ChangeOfTitle cModel = new ChangeOfTitle()
                    {
                        ChangeOfTitleId = SecurityManage.GuidUpper(),
                        MachineryInfoId = viewModel.MachineryInfoId,
                        BeforeEntCode = viewModel.BeforeEntCode,
                        BeforeEntName = viewModel.BeforeEntName,
                        AfterEntCode = viewModel.AfterEntCode,
                        AfterEntName = viewModel.AfterEntName,
                        ChangeDate = viewModel.ChangeDate,
                        ProofUrl = viewModel.ProofUrl,
                        AggrementUrl = viewModel.AggrementUrl,
                        State = 0,
                        DeleteMark = 0,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        Remark = viewModel.Remark,

                    };

                    await _context.ChangeOfTitles.AddAsync(cModel);
                    _context.SaveChanges();
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, cModel.ChangeOfTitleId);
                }
                else
                {



                    //�õ�����ԭ��Ȩ���ֲ�Ȩ��ͬ�ı����¼,�Է��������λ�е
                    for (int i = 0; i < machineryModel.Count; i++)
                    {
                        //�жϱ��α༭�޸ĵĻ�е�Ƿ���ڹ�
                        if (machineryModel[i].ManufacturingLicense == viewModel.ManufacturingLicense
                            && machineryModel[i].LeaveTheFactoryNo == viewModel.LeaveTheFactoryNo)
                        {
                            if (machineryModel[i].State == 2)
                            {

                                return ResponseViewModel<string>.Create(Status.FAIL, "�û�е�Ѿ�����ɹ����������±��", "�û�е�Ѿ�����ɹ����������±��");
                            }
                            else if (machineryModel[i].State == 0 || machineryModel[i].State == 1 || machineryModel[i].State == 3)
                            {

                                return ResponseViewModel<string>.Create(Status.FAIL, "�û�е���ڱ���У��������±��", "�û�е���ڱ���У��������±��");
                            }

                        }

                    }
                    //�޸ı����¼
                    var changeInfo = await _context.ChangeOfTitles.Where(w => w.ChangeOfTitleId == viewModel.ChangeOfTitleId)
                     .OrderByDescending(o => o.CreateDate).FirstOrDefaultAsync();
                    changeInfo.ChangeOfTitleId = viewModel.ChangeOfTitleId;
                    changeInfo.MachineryInfoId = viewModel.MachineryInfoId;
                    changeInfo.BeforeEntCode = viewModel.BeforeEntCode;
                    changeInfo.BeforeEntName = viewModel.BeforeEntName;
                    changeInfo.AfterEntCode = viewModel.AfterEntCode;
                    changeInfo.AfterEntName = viewModel.AfterEntName;
                    changeInfo.ChangeDate = viewModel.ChangeDate;
                    changeInfo.Remark = viewModel.Remark;
                    changeInfo.ProofUrl = viewModel.ProofUrl;
                    changeInfo.AggrementUrl = viewModel.AggrementUrl;
                    changeInfo.UpdateDate = DateTime.Now;
                    _context.ChangeOfTitles.Update(changeInfo);
                    _context.SaveChanges();

                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, viewModel.ChangeOfTitleId);
                }


            }
            catch (Exception ex)
            {

                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }


        }



        /// <summary>
        /// ɾ����Ȩ�����Ϣ
        /// </summary>
        /// <param name="machineryInfoId"></param>
        /// <param name="belongedTo"></param>
        /// <param name="recordNumber"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> DelUpMachineryInfo(string changeOfTitleId)
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id   
                var delModel = await _context.ChangeOfTitles.Where(w => w.DeleteMark == 0
                && w.ChangeOfTitleId == changeOfTitleId).FirstOrDefaultAsync();

                delModel.DeleteMark = 1;

                _context.ChangeOfTitles.Update(delModel);

                _context.SaveChanges();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "ɾ���ɹ�");
            }
            catch (Exception)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }


        }

        #endregion

        #region ��Ȩע����¼
        /// <summary>
        /// ��Ȩע����¼
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<CancelMachinfoViewModel>>> CancelMachineyList(int total, int size, int machinerytype = -1, string machineryName = "",
        string leaveTheFactoryNo = "", string manufacturingLicense = "")
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id   
                var machModel = await _context.MachineryInfos.Where(w => w.DeleteMark == 0
                 && (w.State == 2 && (w.CheckState == (MachineryState)15 || w.CheckState == (MachineryState)0 || w.CheckState == (MachineryState)3))
                 && w.EntGUID == tokenId && w.MachineryName.Contains(machineryName)
                 && w.LeaveTheFactoryNo.Contains(leaveTheFactoryNo)
                 && w.ManufacturingLicense.Contains(manufacturingLicense)
                ).ToListAsync();

                var rModel = await _context.RecordAttachments.Where(w => w.Type == RecordConfigTypeEnum.��Ȩע�� && w.DeleteMark == 0).ToListAsync();

                var canModel = await _context.CancellationOfPropertyRights.Where(w => w.DeleteMark == 0).ToListAsync();

                var maId = machModel.Select(s => s.MachineryInfoId).ToList();


                var fileCount = rModel.Where(w => maId.Contains(w.AttachmentId)).Count();

                var malist = machModel.Select(mm => new CancelMachinfoViewModel
                {
                    CancellationOfPropertyRightsId = mm.CancellationOfPropertyRightsId,
                    MachineryInfoId = mm.MachineryInfoId,
                    PropertyRightsRecordNo = mm.PropertyRightsRecordNo,
                    MachineryType = mm.MachineryType,
                    MachineryName = mm.MachineryName,
                    MachineryModel = mm.MachineryModel,
                    OEM = mm.OEM,
                    LeaveTheFactoryDate = mm.LeaveTheFactoryDate,
                    CancellationState = mm.CancellationState,
                    CancellReason = mm.CancellReason,
                    FileCount = rModel.Where(w => w.AttachmentId == mm.MachineryInfoId).Count(),
                    MaxRatedLiftingCapacity = mm.MaxRatedLiftingCapacity,
                    Knm = mm.Knm,
                    MaxInstallHeight = mm.MaxInstallHeight,
                    FreeStandingHeight = mm.FreeStandingHeight,
                    AttachedHeight = mm.AttachedHeight,
                    MaxRange = mm.MaxRange,
                    BuyDate = mm.BuyDate,
                    LetterOfCommitmentId = mm.LetterOfCommitmentId,
                    Reson = mm.CancellationReason,
                    CreateDate = mm.CreateDate,
                    ReviewBelongedTo = mm.CancellationBelongedTo == null ? "" : _context.CityZone.Where(w => w.BelongedTo == mm.CancellationBelongedTo)
                 .Select(s => s.SuperOrganName)
                 .FirstOrDefault()
                }).ToList();


                if (machinerytype != -1)
                {
                    malist = malist.Where(w => w.MachineryType.GetHashCode() == machinerytype).ToList();
                }

                var totalCount = malist.Count;
                //������Դ��ҳ
                malist = malist.OrderByDescending(o => o.CreateDate).Skip((total - 1) * size).Take(size).ToList();
                return ResponseViewModel<List<CancelMachinfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, malist, totalCount);

            }
            catch (Exception ex)
            {

                return ResponseViewModel<List<CancelMachinfoViewModel>>.Create(Status.ERROR, Message.ERROR);
            }

        }
        /// <summary>
        /// ��е����ע���б�
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<string>> CancelMachineyss([FromBody] ZhuXiao info)
        {
            try
            {
                var query = await _context.MachineryInfos.Where(s => s.MachineryInfoId == info.MachineryInfoId
                  && s.State == 2 && s.CancellationState == 0)
                    .FirstOrDefaultAsync();
                if (query == null)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "���ݴ���");
                }
                query.CheckState = MachineryState.ʹ�õǼ�ע�����ͨ��;
                query.CancellationState = CancellationStateEnum.���ͨ��;
                query.CancellationReason = info.Reason;
                var query1 = await _context.RecordAttachments.Where(s => s.AttachmentId == info.MachineryInfoId && s.DeleteMark == 0).ToListAsync();
                query1.ForEach(x =>
                {
                    x.DeleteMark = 1;
                });
                var guid = SecurityManage.GuidUpper();
                List<LetterOfCommitment> list = new List<LetterOfCommitment>();
                info.Url.ForEach(x =>
                {
                    LetterOfCommitment mode = new LetterOfCommitment();

                    mode.Url = x;
                    mode.Type = "����ע����ŵ��";
                    mode.LetterOfCommitmentId = guid;
                    list.Add(mode);
                });
                if (info.Url.Count > 0)
                {
                    query.LetterOfCommitmentId = guid;
                }

                _context.MachineryInfos.UpdateRange(query);
                _context.RecordAttachments.UpdateRange(query1);
                _context.LetterOfCommitments.AddRange(list);
                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("��е����ע��ʧ�ܣ�" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }
        /// <summary>
        /// ��ȡע����ŵ��
        /// </summary>s
        /// <param name="letterOfCommitmentId"></param>
        /// <returns></returns>
        [HttpGet]

        public async Task<ResponseViewModel<List<string>>> GetUrlList(string letterOfCommitmentId)
        {
            try
            {
                var query = await _context.LetterOfCommitments
                    .Where(s => s.LetterOfCommitmentId == letterOfCommitmentId && s.Type == "����ע����ŵ��")
                    .Select(k => k.Url)
                    .ToListAsync();
                return ResponseViewModel<List<string>>.Create(Status.SUCCESS, Message.SUCCESS, query);
            }
            catch (Exception ex)
            {
                _logger.LogError("��е����ע��ʧ�ܣ�" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<string>>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// ��Ȩע���ϴ�
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<string>> CanceUpFile([FromForm] IFormCollection iform)
        {
            try
            {


                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id    
                var machineryInfoId = iform["MachineryInfoId"].ToString();

                var machModel = await _context.MachineryInfos.Where(w => w.MachineryInfoId == machineryInfoId).FirstOrDefaultAsync();

                if (machModel.CheckState == (MachineryState)2)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "��ǰ��е�Ѿ���װ��֪�޷�ע��", "��ǰ��е�Ѿ���װ��֪�޷�ע��");

                }

                var file = iform.Files[0];
                var fileName = file.FileName;
                var url = Util.UploadFileToServer(file, _environment, Request, "MachineryImg");
                RecordAttachment fileModel = new RecordAttachment()
                {
                    RecordAttachmentId = SecurityManage.GuidUpper(),
                    FileName = fileName,
                    FileUrl = url,
                    RecordConfigId = "",
                    DeleteMark = 0,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    AttachmentId = machineryInfoId,
                    Type = RecordConfigTypeEnum.��Ȩע��,
                };

                _context.RecordAttachments.Add(fileModel);

                await _context.SaveChangesAsync();


                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {

                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }



        }


        /// <summary>
        /// ��Ȩע���ļ��ϴ���¼
        /// </summary>
        /// <param name="machineryInfoId"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<MachineryFileHisViewModel>>> CanceUpFileList(string machineryInfoId)
        {
            try
            {

                var malist = await _context.RecordAttachments.Where(w => w.DeleteMark == 0 && w.AttachmentId == machineryInfoId
                   && w.Type == (RecordConfigTypeEnum)3).Select(s => new MachineryFileHisViewModel
                   {
                       FileName = s.FileName,
                       FileUrl = s.FileUrl,
                       RecordAttachmentId = s.RecordAttachmentId,
                       CreateDate = s.CreateDate,
                       FileType = s.FileUrl.Substring(s.FileUrl.Length - 3, 3)
                   }).ToListAsync();

                return ResponseViewModel<List<MachineryFileHisViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, malist, malist.Count);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<List<MachineryFileHisViewModel>>.Create(Status.ERROR, Message.ERROR);
            }


        }

        /// <summary>
        /// ��Ȩע���ļ�ɾ��
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> DelCanceFile(string recordAttachmentId)
        {
            try
            {
                var delFile = await _context.RecordAttachments.Where(w => w.RecordAttachmentId ==
                   recordAttachmentId && w.Type == (RecordConfigTypeEnum)3).FirstOrDefaultAsync();

                delFile.DeleteMark = 1;

                _context.SaveChanges();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {

                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }

        }
        #endregion

        #region �ύ�����أ�����ͼ
        /// <summary>
        /// �ύ����
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="belongedTo"></param>
        /// <param name="workType">��������</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> SubmitMachineryInfo(string typeId, string belongedTo, string workType)
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id    

                //�޸�ʱ��
                if (workType == "PropertyRecord")
                {
                    //�޸Ļ�е��Ϣ���״̬
                    var machModel = await _context.MachineryInfos.Where(w => w.EntGUID == tokenId
        && w.MachineryInfoId == typeId).FirstOrDefaultAsync();

                    machModel.State = 1;
                    machModel.ReviewBelongedTo = belongedTo;
                    machModel.SubmitDate = DateTime.Now;
                    //�ڻ�е��Ϣ����������һ���ύ��¼
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



                    //�޸���Ϣ����������״̬
                    var changeModel = await _context.ChangeOfTitles.Where(w => w.ChangeOfTitleId == typeId).FirstOrDefaultAsync();

                    changeModel.State = 1;
                    changeModel.BelongedTo = belongedTo;
                    changeModel.SubmitDate = DateTime.Now;
                    changeModel.SubmitUserId = tokenId;
                    _context.ChangeOfTitles.Update(changeModel);

                    //�ڻ�е��Ϣ����������һ���ύ��¼
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
                    //�ж�����ע����¼
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
                        return ResponseViewModel<string>.Create(Status.FAIL, "��ǰ��е�Ѿ���װ��֪�޷�ע��", "��ǰ��е�Ѿ���װ��֪�޷�ע��");

                    }

                    machModel.CancellationOfPropertyRightsId = canModel.CancellationOfPropertyRightsId;
                    machModel.CancellationState = (CancellationStateEnum)1;
                    machModel.CancellationBelongedTo = belongedTo;
                    machModel.CancellationOfPropertySubmitDate = DateTime.Now;
                    _context.MachineryInfos.Update(machModel);

                    _context.CancellationOfPropertyRights.Add(canModel);


                }


                _context.SaveChanges();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "�ύ�ɹ�,��ȴ����");

            }
            catch (Exception ex)
            {

                _logger.LogError("�ύ��Ȩ��������ӿ�ʧ�ܣ�" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }

        }


        /// <summary>
        /// ����
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> RevokeMachineryInfo(string typeId, string workType)
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id   


                if (workType == "PropertyRecord")
                {
                    //�޸Ĳ�Ȩ���״̬,ʱ��
                    var machModel = await _context.MachineryInfos.Where(w => w.EntGUID == tokenId
        && w.MachineryInfoId == typeId).FirstOrDefaultAsync();

                    if (machModel.State == 2)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "�����ͨ���޷�����");
                    }
                    machModel.State = 0;
                    //machModel.ReviewBelongedTo = "";
                    _context.MachineryInfos.Update(machModel);


                }
                else if (workType == "PropertyChange")
                {
                    //�޸Ĳ�Ȩ���״̬,ʱ��
                    var changeModel = await _context.ChangeOfTitles.Where(w => w.ChangeOfTitleId == typeId).FirstOrDefaultAsync();
                    if (changeModel.State == 2)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "�����ͨ���޷�����");
                    }
                    changeModel.State = 0;
                    //changeModel.BelongedTo = "";
                    _context.ChangeOfTitles.Update(changeModel);
                }
                else
                {
                    //�޸Ĳ�Ȩע��״̬,ʱ��
                    var canceModel = await _context.MachineryInfos.Where(w => w.CancellationOfPropertyRightsId
                        == typeId).FirstOrDefaultAsync();
                    if (canceModel.CancellationState == (CancellationStateEnum)2)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "�����ͨ���޷�����");
                    }
                    canceModel.CancellationState = 0;
                    //canceModel.CancellationBelongedTo = "";
                    _context.MachineryInfos.Update(canceModel);

                }

                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "�����ɹ�");
                //    }

            }
            catch (Exception ex)
            {

                _logger.LogError("�ύ��Ȩ��������ӿ�ʧ�ܣ�" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// �鿴����ͼ  --�ݲ�ʹ�� 2019.12.7
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
                    int d_left = left + width + 20; // div left ��ȡ�ı���left+width        
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
                _logger.LogError("�鿴����ͼ�ӿ�ʧ�ܣ�" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<workflowViewImg>.Create(Status.ERROR, Message.ERROR);
            }
        }
        #endregion

        #region ���
        /// <summary>
        /// ��Ȩ��������б�
        /// </summary>
        /// <param name="page">ҳ��</param>
        /// <param name="limit">����</param>
        /// <param name="machineryType">��е����</param>
        /// <param name="machineryName">��е����</param>
        /// <param name="oem">�豸����</param>
        /// <param name="machineryModel">��е�ͺ�</param>
        /// <param name="isReview">�Ƿ���˹���0��δ����1���Ѵ���</param>
        /// <param name="begin">����ʼ����</param>
        /// <param name="end">�����������</param>
        /// <param name="submitUnit">�ύ��λ</param>
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
                var data = from A in _context.MachineryInfos
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

                           };



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
                    // δ����
                    data = data.Where(w => w.State == 1);
                }
                else
                {
                    // �����
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
                _logger.LogError("��ȡ��˵Ļ�е�б�" + ex.Message);
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// ��Ȩ��������б�
        /// </summary>
        /// <param name="page">ҳ��</param>
        /// <param name="limit">����</param>
        /// <param name="machineryType">��е����</param>
        /// <param name="machineryName">��е����</param>
        /// <param name="oem">�豸����</param>
        /// <param name="machineryModel">��е�ͺ�</param>
        /// <param name="isReview">�Ƿ���˹���0��δ����1���Ѵ���</param>
        /// <param name="begin">����ʼ����</param>
        /// <param name="end">�����������</param>
        /// <param name="submitUnit">�ύ��λ</param>
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
                               LeaveTheFactoryDate = A.LeaveTheFactoryDate,//��������                             
                               LeaveTheFactoryNo = A.LeaveTheFactoryNo,//�������
                               ManufacturingLicense = A.ManufacturingLicense,//�������֤���                              
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
                    // δ����
                    data = data.Where(w => w.State == 1);
                }
                else
                {
                    // �����
                    data = data.Where(w => w.State == 2 || w.State == 3);
                    if (begin != null && end != null)
                    {
                        data = data.Where(w => w.ReviewDate >= begin && w.ReviewDate <= end);
                    }
                }

                var count = await data.CountAsync();
                if (count == 0)
                {
                    //������
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
                    "�豸����","�豸�ͺ�","�豸��Ϣ��","��������","�ύ��λ","��������","�������","�������֤���"
                    ,"���������(kg)","�������߶�(m)","����Ÿ߶�(m)","������(m)","���������(KN��m)"
                    ,"����ʱ��","�ύʱ��" ,"���ʱ��"
                };

                listL.Add(biaoti);

                listL.AddRange(list);

                var fileName = "��Ϣ�Ǽ���Ϣ����";
                return DataTableExcel2(listL, fileName);

            }
            catch (Exception ex)
            {
                _logger.LogError("��ȡ��˵Ļ�е�б�" + ex.Message);
                return NoContent();
            }
        }

        public IActionResult DataTableExcel2(List<List<string>> dataTable, string fileName, string sheetName = "name")
        {
            try
            {
                //����EXCEL������
                IWorkbook workBook = new XSSFWorkbook();
                //����sheet�ļ���
                ISheet sheet = workBook.CreateSheet(sheetName);


                #region ���Excel��Ԫ���е�����

                //���������зǱ�ͷ������ݣ����������ݲ����д����������
                for (int i = 0; i < dataTable.Count; i++)
                {
                    IRow row = sheet.CreateRow(i);//��ʾ���������ݱ�ĵڶ��п�ʼ������������ݣ���һ���Ѿ�������
                    for (int j = 0; j < dataTable[i].Count; j++)//����������ÿ����Ԫ��cell��������������ڴ����ĵ�Ԫ���С�
                    {
                        //string fieldName = dataTable[j].ToString();// �ֶ���
                        //ExcelCell columnModel = SetColumnName(i);

                        //�����ݶ���cell��Ԫ����
                        ICell cell = row.CreateCell(j);
                        cell.SetCellValue(dataTable[i][j]);

                        row.Cells.Add(cell);
                    }
                }
                #endregion
                //����������Excel�ļ�
                //������д�룬ͨ�����ķ�ʽ���д��������ļ�
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
        /// ��Ϣ�������
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
                var userName = User.FindFirst(ClaimsIdentity.DefaultNameClaimType)?.Value;//��ǰ������
                var data = await _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId && w.State == 1
                        && w.ReviewBelongedTo == belongedTo
                        && (w.CancellationState == CancellationStateEnum.��˲�ͨ�� || w.CancellationState == CancellationStateEnum.δ�ύ))
                    .OrderByDescending(o => o.SubmitDate)
                    .FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"��е�����ڻ��ѱ���ˣ������ظ�����");
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
                _logger.LogError("��Ϣ�����ˣ�" + ex.Message, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// ��ȡʹ�õǼ���˼�¼-����վ���ʹ�õǼǼ����б�
        /// </summary>
        /// <param name="page">ҳ��</param>
        /// <param name="limit">����</param>
        /// <param name="recordNumber">������ģ����ѯ</param>
        /// <param name="projectName">��Ŀ����</param>
        /// <param name="entName">ʹ�õ�λ����ģ����ѯ</param>
        /// <param name="leaveTheFactoryNo">�������ģ����ѯ</param>
        /// <param name="propertyRightsRecordNo">�豸��Ϣ���</param>
        /// <param name="machineryType">��е����</param>
        /// <param name="machineryName">��е����</param>
        /// <param name="machineryModel">��е�ͺ�</param>
        /// <param name="isReview">�Ƿ���</param>
        /// <param name="begin">��ʼ����</param>
        /// <param name="end">��������</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ReviewUseViewModel>>>
            GetReviewRegisterOfUse(int page, int limit, string recordNumber,
                string projectName, string entName, string leaveTheFactoryNo, string propertyRightsRecordNo
            , MachineryTypeEnum? machineryType, string machineryName, string machineryModel,
                int isReview, DateTime? begin, DateTime? end)
        {
            try
            {
                _context.Database.SetCommandTimeout(300000);
                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//�����˿���
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;

                var installs = await _context.InstallationNotificationRecords
                    .Where(w => w.DeleteMark == 0 && w.Type == 0 && w.State == 2)
                    .Select(k => new { k.InstallationPosition, k.MachineryInfoId, k.Id })
                    .ToListAsync();
                var data = _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0
                    && w.UseAuditStatusJldw == 2 && w.UseReviewBelongedTo == belongedTo);
                if (!string.IsNullOrWhiteSpace(supervisionDepartmentId))
                {
                    data = from A in data
                           join B in _context.ProjectOverview
                           on new { A.BelongedTo, A.RecordNumber } equals new { B.BelongedTo, B.RecordNumber }
                           where B.SupervisionDepartmentId == supervisionDepartmentId
                           select A;
                }
                //������
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    data = data.Where(w => w.RecordNumber.Contains(recordNumber));
                }
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    data = data.Where(w => w.ProjectName.Contains(projectName));
                }
                //ʹ�õ�λ
                if (!string.IsNullOrWhiteSpace(entName))
                {
                    data = data.Where(w => w.EntName.Contains(entName));
                }
                //�������
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    data = data.Where(w => w.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                //�豸��Ϣ��
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
                    // δ����
                    data = data.Where(w => w.CheckState == MachineryState.����ʹ�õǼ������);
                }
                else
                {
                    // �����
                    data = data.Where(w => w.CheckState == MachineryState.����ʹ�õǼ�δͨ��
                        || w.CheckState == MachineryState.����ʹ�õǼ�ͨ��
                        || w.CheckState == MachineryState.��ж��֪�����
                        || w.CheckState == MachineryState.��ж��֪���ͨ��
                        || w.CheckState == MachineryState.��ж��֪��˲�ͨ��
                        || w.CheckState == MachineryState.ʹ�õǼ�ע�������
                        || w.CheckState == MachineryState.ʹ�õǼ�ע�����ͨ��
                        || w.CheckState == MachineryState.ʹ�õǼ�ע����˲�ͨ��);
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

                    //��ѯ��Ŀ�ϴ��Ĵ��µļ�ⱨ����Ƭ
                    var recordConfigImg = await _context.RecordConfigs.Where(x => x.Type == RecordConfigTypeEnum.����ʹ�õǼǵĸ�������
                    && x.DeleteMark == 0 && x.AttachmentName == "��ⱨ���ϴ������£�").OrderBy(x => x.Sort).FirstOrDefaultAsync();

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
                                model.FileName = "��ⱨ��.pdf";
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
                            model.FileName = "��ⱨ��.pdf";
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
                _logger.LogError("��ȡʹ�õǼ���˼�¼��" + ex.Message, ex);
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// ��ȡʹ�õǼ���˼�¼-����վ���ʹ�õǼǼ����б�
        /// </summary>
        /// <param name="page">ҳ��</param>
        /// <param name="limit">����</param>
        /// <param name="recordNumber">������ģ����ѯ</param>
        /// <param name="projectName">��Ŀ����</param>
        /// <param name="entName">ʹ�õ�λ����ģ����ѯ</param>
        /// <param name="leaveTheFactoryNo">�������ģ����ѯ</param>
        /// <param name="propertyRightsRecordNo">�豸��Ϣ���</param>
        /// <param name="machineryType">��е����</param>
        /// <param name="machineryName">��е����</param>
        /// <param name="machineryModel">��е�ͺ�</param>
        /// <param name="isReview">�Ƿ���</param>
        /// <param name="begin">��ʼ����</param>
        /// <param name="end">��������</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ReviewUseViewModel>>>
            GetReviewRegisterOfUseJLDW(int page, int limit, string recordNumber,
                string projectName, string entName, string leaveTheFactoryNo, string propertyRightsRecordNo
            , MachineryTypeEnum? machineryType, string machineryName, string machineryModel,
                int isReview, DateTime? begin, DateTime? end)
        {
            try
            {

                var logintype = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//�����˿���
                var Uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                var entcode = User.FindFirst(nameof(ClaimTypeEnum.EntCode))?.Value;

                if (logintype != "1" || string.IsNullOrEmpty(Uuid) || string.IsNullOrEmpty(entcode))
                {
                    return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.FAIL, Message.FAIL);
                }
                _context.Database.SetCommandTimeout(300000);
                var data = from a in _context.MachineryInfos
                           join b in _context.ProjectEntSnapshot.Where(w => w.MainUnit == "��" && w.EnterpriseType == "����λ" && w.OrganizationCode == entcode)
                           on new { a.BelongedTo, a.RecordNumber } equals new { b.BelongedTo, b.RecordNumber }
                           where a.DeleteMark == 0
                           select a;

                //������
                if (!string.IsNullOrWhiteSpace(recordNumber))
                {
                    data = data.Where(w => w.RecordNumber.Contains(recordNumber));
                }
                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    data = data.Where(w => w.ProjectName.Contains(projectName));
                }
                //ʹ�õ�λ
                if (!string.IsNullOrWhiteSpace(entName))
                {
                    data = data.Where(w => w.EntName.Contains(entName));
                }
                //�������
                if (!string.IsNullOrWhiteSpace(leaveTheFactoryNo))
                {
                    data = data.Where(w => w.LeaveTheFactoryNo.Contains(leaveTheFactoryNo));
                }
                //�豸��Ϣ��
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
                    // δ����
                    data = data.Where(w => w.CheckState == MachineryState.����ʹ�õǼ������ && w.UseAuditStatusJldw == 1);
                }
                else
                {
                    // �����
                    data = data.Where(w =>  w.UseAuditStatusJldw > 1);
                    if (begin != null && end != null)
                    {
                        data = data.Where(w => w.UseReviewDateJldw != null && w.UseReviewDateJldw >= begin && w.UseReviewDateJldw < ((DateTime)end).AddDays(1));
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
                         UseAuditStatusJldw = s.UseAuditStatusJldw,
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

                    //��ѯ��Ŀ�ϴ��Ĵ��µļ�ⱨ����Ƭ
                    var recordConfigImg = await _context.RecordConfigs.Where(x => x.Type == RecordConfigTypeEnum.����ʹ�õǼǵĸ�������
                    && x.DeleteMark == 0 && x.AttachmentName == "��ⱨ���ϴ������£�").OrderBy(x => x.Sort).FirstOrDefaultAsync();

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
                                model.FileName = "��ⱨ��.pdf";
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
                            model.FileName = "��ⱨ��.pdf";
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
                        s.InstallationPosition = _context.InstallationNotificationRecords
                    .Where(w => w.MachineryInfoId == s.MachineryInfoId && w.DeleteMark == 0 && w.Type == 0 && w.State == 2)
                        .OrderByDescending(o => o.Id).Select(k => k.InstallationPosition).FirstOrDefault();
                    });
                }
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("��ȡʹ�õǼ���˼�¼��" + ex.Message, ex);
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }








        /// <summary>
        /// ʹ�õǼ����-�������
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> ReviewRegisterOfUseMachineryJLDW([FromBody] ReviewMachineryViewModel viewModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(viewModel.MachineryInfoId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                if (viewModel.MachineryState != MachineryState.����ʹ�õǼ�δͨ�� && viewModel.MachineryState != MachineryState.����ʹ�õǼ�ͨ��)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                var userName = User.FindFirst(ClaimsIdentity.DefaultNameClaimType)?.Value;//��ǰ������


                var data = await _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId && w.CheckState == MachineryState.����ʹ�õǼ������
                        && w.UseAuditStatusJldw == 1)
                    .OrderByDescending(o => o.UseSubmitDate)
                    .FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"��е�����ڻ��ѱ���ˣ������ظ�����");
                }
                var now = DateTime.Now;

                data.UseReasonJldw = viewModel.Reason;
                data.UseReviewDateJldw = now;
                data.UpdateDate = now;
                if (viewModel.MachineryState == MachineryState.����ʹ�õǼ�ͨ��)
                {
                    data.UseAuditStatusJldw = 2;
                    //// ʹ�õǼ�ͨ������ȡʹ�õǼǺ�
                    //data.UseRecordNo = Util.GetUseRecordNo(_context, data.MachineryType, belongedTo);

                }
                else
                {
                    data.UseAuditStatusJldw = 3;
                    data.CheckState = MachineryState.����ʹ�õǼ�δͨ��;
                }
                _context.MachineryInfos.Update(data);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("ʹ�õǼ���ˣ�" + ex.Message, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, ex.Message + ex.StackTrace);
            }
        }




        /// <summary>
        /// ʹ�õǼ����-����վ
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
                if (viewModel.MachineryState != MachineryState.����ʹ�õǼ�δͨ�� && viewModel.MachineryState != MachineryState.����ʹ�õǼ�ͨ��)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                var userName = User.FindFirst(ClaimsIdentity.DefaultNameClaimType)?.Value;//��ǰ������


                var data = await _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId && w.CheckState == MachineryState.����ʹ�õǼ������
                        && w.UseReviewBelongedTo == belongedTo)
                    .OrderByDescending(o => o.UseSubmitDate)
                    .FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"��е�����ڻ��ѱ���ˣ������ظ�����");
                }

                var record = await _context.RegistrationOfUses
                   .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId && w.RegistrationOfUseId == data.RegistrationOfUseId)
                   .OrderByDescending(o => o.CreateDate)
                   .FirstOrDefaultAsync();

                var now = DateTime.Now;
                data.CheckState = viewModel.MachineryState;
                data.UseReason = viewModel.Reason;
                data.UseReviewDate = now;
                data.UpdateDate = now;
                if (viewModel.MachineryState == MachineryState.����ʹ�õǼ�ͨ��)
                {
                    // ʹ�õǼ�ͨ������ȡʹ�õǼǺ�
                    data.UseRecordNo = Util.GetUseRecordNo(_context, data.MachineryType, belongedTo);

                    var userList = await _context.InstallPeoples.Where(d => d.MachineryInfoId == viewModel.MachineryInfoId && d.Type == 2 && d.InstallationNotificationRecordId == data.RegistrationOfUseId && d.DeleteMark == 0).ToListAsync();
                    userList.ForEach(e =>
                    {
                        DriverChangeRecord mode = new DriverChangeRecord();
                        mode.ConstructionUnit = data.EntName;
                        mode.MachineryPersonId = e.MachineryPersonId;
                        mode.DeleteMark = 0;
                        mode.DriverChangeRecordId = SecurityManage.GuidUpper();
                        mode.CreateDate = DateTime.Now;
                        mode.UpdateDate = DateTime.Now;
                        _context.DriverChangeRecords.Add(mode);
                    });
                }
                _context.MachineryInfos.Update(data);
                if (record != null)
                {
                    record.UpdateDate = now;
                    record.CheckState = viewModel.MachineryState;
                    record.Reason = viewModel.Reason;
                    record.ReviewUserId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                    if (viewModel.MachineryState == MachineryState.����ʹ�õǼ�ͨ��)
                    {
                        // ʹ�õǼ�ͨ������ȡʹ�õǼǺ�
                        record.UseRecordNo = data.UseRecordNo;

                    }
                    _context.RegistrationOfUses.Update(record);
                }

                var xiangmu = await _context.ProjectOverview.Where(w => w.BelongedTo == data.BelongedTo && w.RecordNumber == data.RecordNumber && !string.IsNullOrEmpty(w.ProjectCode)).FirstOrDefaultAsync();
                //var xiangmubind = _context.ProvinceWorkEditBindProject.Where(s => s.BelongedTo == xiangmu.BelongedTo
                //                  && s.RecordNumber == xiangmu.RecordNumber).OrderByDescending(s => s.UpdateDate).FirstOrDefault();

                if (xiangmu != null && !string.IsNullOrEmpty(data.SPSXSLBM))
                {

                    #region ����ʡ���İ����Ϣ

                    GetGongGaiTokenParam model = new GetGongGaiTokenParam();
                    model.appKey = _jsgginterface.appKey;
                    model.appSecret = _jsgginterface.appSecret;

                    //����ʡ���Ļ�ȡtoken
                    string postContent = DataBll.GetGongGaiToken(_jsgginterface.UrlGetToken, "application/x-www-form-urlencoded", model);
                    if (!string.IsNullOrEmpty(postContent))
                    {

                        GongGaiSystemViewModel result0 = JsonConvert.DeserializeObject<GongGaiSystemViewModel>(postContent);
                        if (result0 != null && result0.status.code == 1)
                        {
                            //��ȡ��������
                            CityZone cueecity = _context.CityZone.Where(w => w.BelongedTo == belongedTo).FirstOrDefault();

                            TokenParam headermodel = new TokenParam();
                            headermodel.access_token = result0.custom.access_token;
                            headermodel.time_stamp = result0.custom.timeStamp;
                            headermodel.app_key = _jsgginterface.appKey;


                            ProjectParam projectparam = new ProjectParam();
                            projectparam.ProjectCode = xiangmu.ProjectCode;
                            string paramstr1 = "";
                            //���óɹ����Ȼ�ȡʡ���ĵ���Ŀ��Ϣ
                            //token�ɹ���
                            //��ȡ��Ŀ��Ϣ
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
                                    //�������
                                    banjian.SPSXSLBM = data.SPSXSLBM;
                                    //������=������˵�λ
                                    banjian.BLCS = cueecity.SuperOrganName;
                                    //������
                                    banjian.BLR = userName;
                                    if (viewModel.MachineryState == MachineryState.����ʹ�õǼ�ͨ��)
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
                                    //�����������������Ϣ
                                    string postContentspbj = DataBll.RequestUrl("POST", "application/x-www-form-urlencoded", _jsgginterface.UrlSafe, headermodel, banjian, out paramstr2);
                                    RequestRecordLog rizghi = new RequestRecordLog();
                                    rizghi.RequestURL = _jsgginterface.UrlSafe;
                                    rizghi.ReturnInformation = postContentspbj;
                                    rizghi.RequestParameters = paramstr2;
                                    rizghi.ForeignKey = "RegistrationOfUseId=" + data.RegistrationOfUseId;
                                    rizghi.Model = "ʡ����-�������������Ϣ";
                                    rizghi.CreateDate = DateTime.Now;
                                    _context.RequestRecord.Add(rizghi);
                                    await _context.SaveChangesAsync();
                                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS + "(Ps:" + _jsgginterface.UrlSafe + "���Ͱ�����" + postContentspbj + ")");

                                }
                                else
                                {

                                    RequestRecordLog rizghi = new RequestRecordLog();
                                    rizghi.RequestURL = _jsgginterface.UrlGetxmjbxx;
                                    rizghi.ReturnInformation = postContentxm;
                                    rizghi.RequestParameters = paramstr1;
                                    rizghi.ForeignKey = "ProjectCode=" + xiangmu.ProjectCode;
                                    rizghi.Model = "ʡ����-��ȡ��Ŀ��Ϣ�ӿ�";
                                    rizghi.CreateDate = DateTime.Now;
                                    if (result1 != null && result1.status.code == 1 && string.IsNullOrEmpty(result1.custom.results.projectnum))
                                    {
                                        rizghi.Remark = "û������Ŀ���" + result1.custom.results.projectnum;
                                    }
                                    _context.RequestRecord.Add(rizghi);
                                    await _context.SaveChangesAsync();

                                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS + "(Ps:" + _jsgginterface.UrlGetxmjbxx + "��ȡ��Ŀ��Ϣ�ӿ�" + postContentxm + ")");
                                }
                            }
                            else
                            {
                                RequestRecordLog rizghi = new RequestRecordLog();
                                rizghi.RequestURL = _jsgginterface.UrlGetxmjbxx;
                                rizghi.ReturnInformation = postContentxm;
                                rizghi.RequestParameters = paramstr1;
                                rizghi.ForeignKey = "ProjectCode=" + xiangmu.ProjectCode;
                                rizghi.Model = "ʡ����-��ȡ��Ŀ��Ϣ�ӿ�";
                                rizghi.CreateDate = DateTime.Now;
                                rizghi.Remark = "��ȡ��Ŀ��Ϣ�ӿ��޷�Ӧ";
                                _context.RequestRecord.Add(rizghi);
                                await _context.SaveChangesAsync();
                                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS + "(Ps:" + _jsgginterface.UrlGetxmjbxx + "��ȡ��Ŀ��Ϣ�ӿ��޷�Ӧ)");
                            }
                        }
                        else
                        {
                            await _context.SaveChangesAsync();
                            return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS + "(Ps:" + _jsgginterface.UrlGetToken + "��ȡtoken�ӿڣ�" + postContent + ")");
                        }
                    }
                    else
                    {
                        await _context.SaveChangesAsync();
                        return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS + "(Ps:" + _jsgginterface.UrlGetToken + "��ȡtoken�ӿڽӿ���Ӧ)");
                    }

                    #endregion
                }

                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("ʹ�õǼ���ˣ�" + ex.Message, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, ex.Message + ex.StackTrace);
            }
        }




        /// <summary>
        /// ����ʹ�õǼ���
        /// </summary>
        /// <param name="registrationOfUseId"></param>
        /// <returns></returns>
        [HttpGet]
        //[Authorize]
        public async Task<ResponseViewModel<string>> GetUseRecordCer(string registrationOfUseId)
        {
            try
            {
                var machineryInfo = await _context.MachineryInfos
                    .Where(w => w.RegistrationOfUseId == registrationOfUseId && w.CancellationState != CancellationStateEnum.���ͨ��)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                if (machineryInfo == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "�豸������");
                }
                else if (machineryInfo.UseReviewDate == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "���豸ʹ�õǼ�δ�����ಿ�����ͨ��");
                }
                var jiancesuo = await _context.TestingInstituteInfo
                    .Where(w => w.TestingInstituteInfoId == machineryInfo.TestingInstituteInfoId)
                    .FirstOrDefaultAsync();

                var reg = await _context.RegistrationOfUses
                    .Where(w => w.RegistrationOfUseId == registrationOfUseId && w.DeleteMark == 0)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                //var weizhi = await _context.InstallationNotificationRecords
                //    .Where(s => s.DeleteMark == 0 && s.Type == 0 && s.MachineryInfoId == machineryInfo.MachineryInfoId)
                //    .FirstOrDefaultAsync();
                if (reg == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "ʹ�õǼǼ�¼�����ڻ��ѱ�ɾ�����޷�����ʹ�õǻ���");
                }
                // ��ȡʹ�õǼ���
                // �����龰
                // 1.��ȡ��ǰ��е��ʹ�õǻ���
                // 2.��ȡ��ǰ��е������ʹ�õǼ���
                if (machineryInfo == null)
                {
                    // ����ʹ�õǼǲ飬δ�鵽��е��ʱ��
                    if (reg != null && !string.IsNullOrWhiteSpace(reg.UseRecordCerUrl))
                    {
                        return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, reg.UseRecordCerUrl);
                    }
                    return ResponseViewModel<string>.Create(Status.FAIL, "��е�����ڻ��ѱ�ɾ��");
                }
                Dictionary<string, string> dic = new Dictionary<string, string>();



                dic.Add("UseRecordNo", reg.UseRecordNo);
                dic.Add("MachineryName", machineryInfo.MachineryType.ToString());
                dic.Add("ManufacturingLicense", machineryInfo.ManufacturingLicense);
                dic.Add("MachineryModel", machineryInfo.MachineryModel + $"({machineryInfo.MaxRatedLiftingCapacity})");
                dic.Add("LeaveTheFactoryNo", machineryInfo.LeaveTheFactoryNo);
                dic.Add("MachineryType", machineryInfo.MachineryType.ToString());
                dic.Add("EntName", machineryInfo.EntName);
                dic.Add("ProjectName", machineryInfo.ProjectName);
                dic.Add("MechanismName", jiancesuo.MechanismName);
                //var now = reg.UpdateDate;
                // ��ȡ��Ŀ��������Ա ��Ŀ����
                var person = await _context.ProjectPersonSnapshot
                    .Where(w => w.BelongedTo == machineryInfo.BelongedTo && w.RecordNumber == machineryInfo.RecordNumber && w.PersonType == "��Ŀ����")
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                dic.Add("PersonName", person?.PersonName);
                dic.Add("PersonPhone", person?.PersonPhone);
                var install = await _context.InstallationNotificationRecords
                    .Where(w => w.MachineryInfoId == machineryInfo.MachineryInfoId
                        && w.BelongedTo == machineryInfo.BelongedTo
                        && w.RecordNumber == machineryInfo.RecordNumber
                        && w.State == 2 && w.DeleteMark == 0)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                var installEntName = "";
                var intelligenceLevel = "";
                if (install != null)
                {
                    var ent = await _context.EntRegisterInfoMag.Where(w => w.EntRegisterInfoMagId == install.EntGUID)
                        .OrderByDescending(o => o.Id)
                        .FirstOrDefaultAsync();
                    installEntName = ent?.EntName;
                    intelligenceLevel = ent.IntelligenceLevel.ToString();
                    dic.Add("InstallationPosition", install.InstallationPosition);
                }
                dic.Add("InstallEntName", installEntName);

                dic.Add("IntelligenceLevel", intelligenceLevel);
                if (machineryInfo.IsOldData == 1 && install == null)
                {
                    dic.Add("PlanInstallDate", machineryInfo.InstallationSelfInspectionDate?.ToString("yyyy-MM-dd"));
                }
                else
                {
                    dic.Add("PlanInstallDate", install?.PlanInstallDate?.ToString("yyyy-MM-dd"));
                }

                dic.Add("InstallReviewDate", reg.InstallationAcceptanceDate?.ToString("yyyy-MM-dd"));

                var checkReviceDate = machineryInfo.CheckReviewDate;
                if (machineryInfo.ReCheckReviewDate != null)
                {
                    checkReviceDate = machineryInfo.ReCheckReviewDate;
                }
                var org = await _context.CityZone
                    .Where(w => w.BelongedTo == reg.UseReviewBelongedTo)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                if (org == null)
                {
                    dic.Add("OrgName", "");
                }
                else
                {
                    if (org.SuperOrganName == "���ݾ��ü������������蹤��������ȫ�ලվ")
                    {
                        dic.Add("OrgName", "���ݾ��ü�����������ί��");
                    }
                    else if (org.SuperOrganName == "��ͨ�н��谲ȫ�����ලվ")
                    {
                        dic.Add("OrgName", "��ͨ��ס���ͳ��罨���");
                    }
                    else
                    {
                        dic.Add("OrgName", org.SuperOrganName);
                    }

                }
                dic.Add("PropertyRightsRecordNo", machineryInfo.PropertyRightsRecordNo);

                dic.Add("UseReviewDate", machineryInfo.UseReviewDate?.ToString("yyyy-MM-dd"));
                //dic.Add("InstallationPosition", weizhi == null ? "" : weizhi.InstallationPosition);
                dic.Add("CheckReviewDate", checkReviceDate?.ToString("yyyy-MM-dd"));

                //dic.Add("Year", now.Year.ToString());
                //dic.Add("Month", now.Month.ToString());
                //dic.Add("Day", now.Day.ToString());

                var baseUrl = Util.GetBaseUrl(Request);
                string webRootPath = _wordTemplte + "Machinery/RegistrationOfUseMain.docx";
                //try
                //{
                //    Aspose.Words.Document doc = new Aspose.Words.Document(webRootPath);
                //    DocumentBuilder builder = new DocumentBuilder(doc);

                //    if (doc.Range.Bookmarks["QZImgLS"] != null)
                //    {
                //        builder.MoveToBookmark("QZImgLS"); //���Ƕ�λ����ǩ��ȥ��  

                //        //_wordTemplte + "Machinery/njdengjizhang.png"

                //        builder.InsertImage(_wordTemplte + "Machinery/sydjz.png"
                //                , RelativeHorizontalPosition.Page, 438, RelativeVerticalPosition.Line, 0, 70, 50, WrapType.None);

                //    }
                //    ////����ͼƬ����
                //    //DocumentBuilder builder = new DocumentBuilder(webRootPath);
                //    //builder.endBookmark("picjs");
                //    ////insertimage("·��","ˮƽλ��","��ƫ����","��ֱλ��","�ϱ߾�","ͼƬ��","ͼƬ��","ͼƬ��������");
                //    //builder.insertImage(basePath + "11" + File.separator + "11" + ".png", RelativeHorizontalPosition.MARGIN, 300, RelativeVerticalPosition.MARGIN, 430, 100, 25, WrapType.NONE);
                //    //builder.endBookmark("picsg");
                //    //builder.insertImage(basePath + "11" + File.separator + "11" + ".png", RelativeHorizontalPosition.MARGIN, 300, RelativeVerticalPosition.MARGIN, 460, 80, 15, WrapType.NONE);
                //    //builder.endBookmark("picjl");
                //    //builder.insertImage(basePath + "11" + File.separator + "11" + ".png", RelativeHorizontalPosition.MARGIN, 300, RelativeVerticalPosition.MARGIN, 490, 80, 15, WrapType.NONE);

                //}
                //catch (Exception ex)
                //{

                //    throw;
                //}

                var fileName = registrationOfUseId + "UseRecordCer.pdf";
                var url = Util.BuildWordToServer(_environment, Request, dic, new List<List<string>>(), webRootPath, "registrationOfUse", fileName);
                machineryInfo.UseRecordUrl = url;
                _context.MachineryInfos.Update(machineryInfo);
                _context.SaveChanges();
                //var url = Util.BuildWord(_ossFileSetting, dic, new List<List<string>>(), webRootPath, "UseRecordCer");

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, url);
            }
            catch (Exception ex)
            {
                _logger.LogError("��ȡʹ�õǼ�֤��" + ex.Message + "\r\n" + ex.StackTrace);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// ��ӡ�Ǽ���
        /// </summary>
        /// <param name="registrationOfUseId">�ǼǼ�¼��id</param>
        /// <returns></returns>
        [HttpGet]
        //[Authorize]
        public async Task<ResponseViewModel<List<RegistrationOfUse>>> GetRecordCerList()
        {
            try
            {
                var aa = await _context.RegistrationOfUses.GroupBy(g => g.MachineryInfoId).Select(a => new
                {
                    Id = a.Max(m => m.Id)
                }).ToListAsync();
                var reg = await _context.RegistrationOfUses
                    .Where(w => w.DeleteMark == 0 && w.CheckState == MachineryState.����ʹ�õǼ�ͨ�� && aa.Select(p => p.Id).Contains(w.Id))
                    .OrderByDescending(o => o.Id).ToListAsync();
                if (reg.Count == 0)
                {
                    return ResponseViewModel<List<RegistrationOfUse>>.Create(Status.FAIL, "ʹ�õǼǼ�¼�����ڻ��ѱ�ɾ�����޷�����ʹ�õǻ���");
                }
                return ResponseViewModel<List<RegistrationOfUse>>.Create(Status.SUCCESS, Message.SUCCESS, reg, reg.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError("��ȡʹ�õǼ�֤��" + ex.Message + "\r\n" + ex.StackTrace);
                return ResponseViewModel<List<RegistrationOfUse>>.Create(Status.ERROR, Message.ERROR);
            }

        }

        /// <summary>
        /// ��ȡ��Ȩ�������б�
        /// </summary>
        /// <param name="page">ҳ��</param>
        /// <param name="limit">����</param>
        /// <param name="machineryType">��е����</param>
        /// <param name="machineryName">��е����</param>
        /// <param name="oem">��������</param>
        /// <param name="machineryModel">��е�ͺ�</param>
        /// <param name="isReview">�Ƿ��Ѵ���</param>
        /// <param name="begin">����ʼ</param>
        /// <param name="end">�������ʱ��</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ChangeOfTitleViewModel>>>
            GetReviewChangeOfTitle(int page, int limit, MachineryTypeEnum? machineryType, string machineryName,
            string oem, string machineryModel,
            int isReview, DateTime? begin, DateTime? end, string beforeEntName, string afterEntName, string propertyRightsRecordNo)
        {
            try
            {
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var data = from c in _context.ChangeOfTitles
                           join m in _context.MachineryInfos
                           on c.MachineryInfoId equals m.MachineryInfoId into mData
                           from m in mData.DefaultIfEmpty()
                           where c.DeleteMark == 0 && m.DeleteMark == 0 && c.BelongedTo == belongedTo
                              && m.CancellationState == CancellationStateEnum.���ͨ��
                           select new ChangeOfTitleViewModel
                           {
                               MachineryType = m.MachineryType,
                               AfterEntCode = c.AfterEntCode,
                               AfterEntName = c.AfterEntName,
                               AggrementUrl = c.AggrementUrl,
                               BeforeEntCode = c.BeforeEntCode,
                               BeforeEntName = c.BeforeEntName,
                               BelongedTo = c.BelongedTo,
                               ChangeDate = c.ChangeDate,
                               ChangeOfTitleId = c.ChangeOfTitleId,
                               CreateDate = c.CreateDate,
                               DeleteMark = c.DeleteMark,
                               Id = c.Id,
                               MachineryInfoId = c.MachineryInfoId,
                               MachineryModel = m.MachineryModel,
                               MachineryName = m.MachineryName,
                               ProofUrl = c.ProofUrl,
                               Reason = c.Reason,
                               Remark = c.Remark,
                               UpdateDate = c.UpdateDate,
                               State = c.State,
                               ReviewDate = c.ReviewDate,
                               ReviewUserId = c.ReviewUserId,
                               SubmitDate = c.SubmitDate,
                               SubmitUserId = c.SubmitUserId,
                               // ��е��Ϣ
                               BuyDate = m.BuyDate,
                               Knm = m.Knm,
                               MaxInstallHeight = m.MaxInstallHeight,
                               FreeStandingHeight = m.FreeStandingHeight,
                               AttachedHeight = m.AttachedHeight,
                               MaxRange = m.MaxRange,
                               MaxRatedLiftingCapacity = m.MaxRatedLiftingCapacity,
                               OEM = m.OEM,
                               PropertyRightsRecordNo = m.PropertyRightsRecordNo

                           };
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
                if (!string.IsNullOrWhiteSpace(machineryModel))
                {
                    data = data.Where(w => w.MachineryModel.Contains(machineryModel));
                }
                if (isReview == 0)
                {
                    // δ����
                    data = data.Where(w => w.State == 1);
                }
                else
                {
                    // �����
                    data = data.Where(w => w.State == 2
                        || w.State == 3);
                    if (begin != null && end != null)
                    {
                        data = data.Where(w => w.ReviewDate >= begin && w.ReviewDate <= end);
                    }
                }
                if (!string.IsNullOrWhiteSpace(propertyRightsRecordNo))
                {
                    data = data.Where(w => w.PropertyRightsRecordNo.Contains(propertyRightsRecordNo));
                }
                if (!string.IsNullOrWhiteSpace(beforeEntName))
                {
                    data = data.Where(w => w.BeforeEntName.Contains(beforeEntName));
                }
                if (!string.IsNullOrWhiteSpace(afterEntName))
                {
                    data = data.Where(w => w.AfterEntName.Contains(afterEntName));
                }
                var count = await data.CountAsync();
                if (count == 0)
                {
                    return ResponseViewModel<List<ChangeOfTitleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<ChangeOfTitleViewModel>(), count);
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
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();
                return ResponseViewModel<List<ChangeOfTitleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("��ȡ��Ȩ�������б�" + ex.Message, ex);
                return ResponseViewModel<List<ChangeOfTitleViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// ��Ȩ������
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> ReviewChangeOfTitle([FromBody] ReviewMachineryViewModel viewModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(viewModel.ChangeOfTitleId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                if (viewModel.State != 2 && viewModel.State != 3)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var data = await _context.ChangeOfTitles
                    .Where(w => w.DeleteMark == 0 && w.ChangeOfTitleId == viewModel.ChangeOfTitleId && w.BelongedTo == belongedTo)
                    .OrderByDescending(o => o.CreateDate)
                    .FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "�����¼�����ڻ��ѱ�ɾ��������ʧ��");
                }
                if (data.State != 1)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "����ѱ���ˣ������ظ����");
                }
                var now = DateTime.Now;
                data.Reason = viewModel.Reason;
                data.State = viewModel.State;
                data.ReviewDate = now;
                data.ReviewUserId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                data.UpdateDate = now;

                if (viewModel.State == 2)
                {
                    // ���ͨ��
                    // ��ѯ�������¼�����Ļ�еid����һ��
                    // ������һ������ע��״̬���򷵻��쳣
                    var machinery = await _context.MachineryInfos
                        .Where(w => w.MachineryInfoId == data.MachineryInfoId)
                        .OrderByDescending(o => o.CreateDate)
                        .FirstOrDefaultAsync();

                    if (machinery == null || machinery.CancellationState != CancellationStateEnum.���ͨ��)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "�û�еδע�����ѱ���Ȩ���");
                    }

                    // �Ƿ��������
                    // �µĵ�λ��֤�Ƿ��������
                    var ent = await _context.EntRegisterInfoMag
                        .Where(w => w.EntRegisterInfoMagId == data.SubmitUserId)
                        .OrderByDescending(o => o.Id)
                        .FirstOrDefaultAsync();
                    if (ent == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "�����Ĳ�Ȩ��λ��Ϣ�쳣���޷���ɱ��");
                    }
                    // ����һ�����������ͨ���ļ�¼
                    var newMachinery = new MachineryInfo
                    {
                        MachineryType = machinery.MachineryType,
                        MachineryName = machinery.MachineryName,
                        MachineryModel = machinery.MachineryModel,
                        OEM = machinery.OEM,
                        ManufacturingLicense = machinery.ManufacturingLicense,
                        LeaveTheFactoryNo = machinery.LeaveTheFactoryNo,
                        MaxRatedLiftingCapacity = machinery.MaxRatedLiftingCapacity,
                        LeaveTheFactoryDate = machinery.LeaveTheFactoryDate,
                        Knm = machinery.Knm,
                        MaxInstallHeight = machinery.MaxInstallHeight,
                        FreeStandingHeight = machinery.FreeStandingHeight,
                        AttachedHeight = machinery.AttachedHeight,
                        MaxRange = machinery.MaxRange,
                        BuyDate = machinery.BuyDate,
                        CreateDate = now,
                        DeleteMark = 0,
                        MachineryInfoId = SecurityManage.GuidUpper(),
                        State = 2,
                        EntGUID = ent.EntRegisterInfoMagId,
                        CanLease = ent.CanLease,
                        PropertyRightsRecordNo = Util.GetMachineryRecordNo(_context, machinery.MachineryType, belongedTo),
                        ReviewBelongedTo = data.BelongedTo,
                        ReviewDate = now
                    };
                    await _context.MachineryInfos.AddAsync(newMachinery);
                }

                var changeRecord = await _context.ChangeRecords
                    .Where(w => w.MachineryInfoId == data.MachineryInfoId && w.ReviewBelongedTo == data.BelongedTo)
                    .OrderByDescending(o => o.CreateDate)
                    .FirstOrDefaultAsync();
                if (changeRecord != null)
                {
                    changeRecord.UpdateDate = now;
                    changeRecord.ReviewUserId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                    changeRecord.State = (CancellationStateEnum)viewModel.State;
                    changeRecord.Reason = viewModel.Reason;
                    changeRecord.UpdateDate = now;
                    _context.ChangeRecords.Update(changeRecord);
                }
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
                //return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("��˲�Ȩ�����" + ex.Message, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }
        #endregion

        #region ��������

        /// <summary>
        /// ��ȡ������б�
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
        [Authorize(Roles = "�����,�Ͼ������")]
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
                    // δ����
                    data = data.Where(w => w.MachineryCheckState == MachineryCheckStateEnum.�����
                    || w.MachineryCheckState == MachineryCheckStateEnum.������);
                }
                else
                {

                    // �����
                    data = data.Where(w => w.MachineryCheckState == MachineryCheckStateEnum.��ⲻ�ϸ�
                        || w.MachineryCheckState == MachineryCheckStateEnum.���ϸ�
                        || w.MachineryCheckState == MachineryCheckStateEnum.����ϸ�
                        || w.MachineryCheckState == MachineryCheckStateEnum.���첻�ϸ�
                        || w.MachineryCheckState == MachineryCheckStateEnum.���������);

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
                _logger.LogError("��ȡ��е����¼��" + ex.Message, ex);
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// ��ȡ������б�
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
        [Authorize(Roles = "�����,�Ͼ������,���������")]
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
                           where a.DeleteMark == 0 && (a.MachineryType == MachineryTypeEnum.��ʽ���ػ�
                    || a.MachineryType == MachineryTypeEnum.ʩ��������)
                        && a.TestingInstituteInfoId == testingId
                           select new { a, b.IsRectificationCheckFinish };//IsRectificationCheckFinish=0 ������ 1�������

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
                    // δ����
                    data = data.Where(w => w.a.MachineryCheckState == MachineryCheckStateEnum.�����
                    || w.a.MachineryCheckState == MachineryCheckStateEnum.������);
                    data = data.OrderByDescending(o => o.a.CheckSubmitDate);
                }
                else if (isReview == 2)
                {
                    //������
                    data = data.Where(w => (w.a.MachineryCheckState == MachineryCheckStateEnum.��ⲻ�ϸ�
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.���ϸ�
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.����ϸ�
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.���첻�ϸ�
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.���������
                        ) && w.IsRectificationCheckFinish == 0);

                }
                else if (isReview == 1)
                {

                    // �����
                    data = data.Where(w => (w.a.MachineryCheckState == MachineryCheckStateEnum.��ⲻ�ϸ�
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.���ϸ�
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.����ϸ�
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.���첻�ϸ�
                        || w.a.MachineryCheckState == MachineryCheckStateEnum.���������
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
                    if (m.IsRectificationCheckFinish != 1 && m.CheckState == MachineryState.��װ��֪���ͨ��)
                    {
                        m.IsCanRecall = 1;
                    }

                    //�Ƿ����ü������
                    m.CheckEquipmentConfiguresCount = _context.CheckEquipmentConfigures.Where(w => w.TestingInstituteInfoId == testingId && w.DeleteMark == 0).OrderByDescending(w => w.Id).Count();

                    //�Ƿ񱣴油������
                    var buchongziliao = _context.MachineryInfoSupplementaryInformations.Where(w => w.MachineryInfoId == m.MachineryInfoId && w.CheckRecordId == m.CheckRecordId && w.DeleteMark == 0).OrderByDescending(w => w.Id).FirstOrDefault();

                    // �Ƿ񱣴�������ͽ���
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
                _logger.LogError("��ȡ��е����¼��" + ex.Message, ex);
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        [HttpPost]
        [Authorize(Roles = "�����,�Ͼ������")]
        public async Task<ResponseViewModel<string>> CheckMachinery([FromBody] ReviewMachineryViewModel viewModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(viewModel.MachineryInfoId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                if (viewModel.MachineryCheckState != MachineryCheckStateEnum.��ⲻ�ϸ�
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.���ϸ�
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.���������
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.����ϸ�
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.���첻�ϸ�)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;
                var data = await _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId
                        && (w.MachineryCheckState == MachineryCheckStateEnum.�����
                            || w.MachineryCheckState == MachineryCheckStateEnum.������)
                        && w.TestingInstituteInfoId == testingId)
                    .OrderByDescending(o => o.UseSubmitDate)
                    .FirstOrDefaultAsync();


                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"��е�����ڻ��ѱ���⣬�����ظ�����");
                }

                var now = DateTime.Now;
                //data.CheckState = viewModel.MachineryState;

                data.CheckUrl = viewModel.RecordUrl;
                if (data.MachineryCheckState == MachineryCheckStateEnum.�����)
                {
                    data.CheckReviewDate = viewModel.CheckReviewDate;
                    data.ReCheckReviewDate = viewModel.CheckReviewDate;
                }
                else if (data.MachineryCheckState == MachineryCheckStateEnum.������)
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
                    return ResponseViewModel<string>.Create(Status.FAIL, $"��еί�м���¼�����ڻ��ѱ�ɾ�����޷����м��");
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
                _logger.LogError("�������⣺" + ex.Message, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        [HttpPost]
        [Authorize(Roles = "�����,�Ͼ������")]
        public async Task<ResponseViewModel<string>> CheckMachinery2([FromBody] ReviewMachineryViewModel viewModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(viewModel.MachineryInfoId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                if (viewModel.MachineryCheckState != MachineryCheckStateEnum.��ⲻ�ϸ�
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.���ϸ�
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.���������
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.����ϸ�
                    && viewModel.MachineryCheckState != MachineryCheckStateEnum.���첻�ϸ�)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL);
                }
                var userId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var testingId = User.FindFirst(nameof(ClaimTypeEnum.TestingInstituteInfoId))?.Value;
                var data = await _context.MachineryInfos
                    .Where(w => w.DeleteMark == 0 && w.MachineryInfoId == viewModel.MachineryInfoId
                        && (w.MachineryCheckState == MachineryCheckStateEnum.����� || w.MachineryCheckState == MachineryCheckStateEnum.������)
                        && w.TestingInstituteInfoId == testingId)
                    .OrderByDescending(o => o.UseSubmitDate)
                    .FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"��е�����ڻ��ѱ���⣬�����ظ�����");
                }

                var now = DateTime.Now;
                //data.CheckState = viewModel.MachineryState;
                data.CheckReviewDate = now;
                data.MachineryCheckState = viewModel.MachineryCheckState;
                data.CheckUrl = viewModel.RecordUrl;
                data.UpdateDate = now;
                _context.MachineryInfos.Update(data);
                var checkRecord = await _context.CheckRecords
                    .Where(w => w.CheckRecordId == data.CheckRecordId && w.TestingInstituteInfoId == testingId)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                if (checkRecord == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, $"��еί�м���¼�����ڻ��ѱ�ɾ�����޷����м��");
                }
                checkRecord.ReviewUserId = userId;
                checkRecord.UpdateDate = now;
                checkRecord.CheckState = viewModel.MachineryCheckState;
                checkRecord.RecordUrl = viewModel.RecordUrl;

                // һ����Ŀ ��֤��Ŀ���ϸ���
                //�Ȳ�����ϸ���
                var bad = await _context.CheckDetails
                    .Where(w => w.MachineryInfoId == viewModel.MachineryInfoId && w.CheckRecordId == viewModel.CheckRecordId && (w.CheckResult.Contains("���ϸ�") || w.CheckResult.Contains("������")))
                    .ToListAsync();
                var contentIds = bad.Select(s => s.ContentId).ToList();
                var config = await _context.TestContentItems
                    .Where(w => w.DeleteMark == 0 && contentIds.Contains(w.ContentId))
                    .ToListAsync();
                checkRecord.GeneralItem = config.Where(w => w.IsNecessary == 0).Count();
                checkRecord.GuaranteeItem = config.Count - checkRecord.GeneralItem;
                checkRecord.UpdateDate = now;
                _context.CheckRecords.Update(checkRecord);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("�������⣺" + ex.Message, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }
        /// <summary>
        /// ��ȡ�����
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
                    return ResponseViewModel<CheckRecord>.Create(Status.FAIL, "����������ڻ��ѱ�ɾ��");
                }

                return ResponseViewModel<CheckRecord>.Create(Status.SUCCESS, Message.SUCCESS, data);
            }
            catch (Exception ex)
            {
                _logger.LogError("�鿴��ⱨ�棺" + ex.Message, ex);
                return ResponseViewModel<CheckRecord>.Create(Status.ERROR, Message.ERROR);
            }
        }
        #endregion

        #region �Ѽ���е�б�-����վ�˲鿴

        /// <summary>
        /// ��ȡ������б�
        /// </summary>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <param name="projectName"></param>
        /// <param name="machineryType"></param>
        /// <param name="machineryName"></param>
        /// <param name="machineryModel"></param>
        /// <param name="checkState">���״̬</param>
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

                var supervisionDepartmentId = User.FindFirst(nameof(ClaimTypeEnum.SupervisionDepartmentId))?.Value;//�����˿���
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
                    // δ���
                    data = data.Where(w => w.MachineryCheckState == MachineryCheckStateEnum.�����
                    || w.MachineryCheckState == MachineryCheckStateEnum.������);
                }
                else if (checkState == 2)
                {
                    //���ϸ�
                    data = data.Where(w => w.MachineryCheckState == MachineryCheckStateEnum.���ϸ�
                        || w.MachineryCheckState == MachineryCheckStateEnum.����ϸ�);
                }
                else if (checkState == 3)
                {
                    //��ⲻ�ϸ�
                    data = data.Where(w => w.MachineryCheckState == MachineryCheckStateEnum.��ⲻ�ϸ�
                        || w.MachineryCheckState == MachineryCheckStateEnum.���첻�ϸ�);
                }
                else if (checkState == 4)
                {
                    //������
                    data = from a in data
                           join b in _context.CheckRecords
                           on a.CheckRecordId equals b.CheckRecordId
                           where ((a.MachineryCheckState == MachineryCheckStateEnum.��ⲻ�ϸ�
                        || a.MachineryCheckState == MachineryCheckStateEnum.���ϸ�
                        || a.MachineryCheckState == MachineryCheckStateEnum.����ϸ�
                        || a.MachineryCheckState == MachineryCheckStateEnum.���첻�ϸ�
                        || a.MachineryCheckState == MachineryCheckStateEnum.���������
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
                _logger.LogError("��ȡ��е����¼��" + ex.Message, ex);
                return ResponseViewModel<List<ReviewUseViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        #endregion






        /// <summary>
        /// ά�޸�������Դ
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<MachineryInfoViewModelS>>> MaintenanceFirstList(int total, int size, int machinerytype = -1, string oem = "",
        string leaveTheFactoryNo = "", string manufacturingLicense = "")
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id   
                var machModel = await _context.MachineryInfos.Where(w => w.DeleteMark == 0
                 && w.State == 2 && w.CancellationState != CancellationStateEnum.���ͨ�� && w.EntGUID == tokenId && w.OEM.Contains(oem)
                 && w.LeaveTheFactoryNo.Contains(leaveTheFactoryNo)
                 && w.ManufacturingLicense.Contains(manufacturingLicense)
                ).ToListAsync();

                if (machinerytype != -1)
                {
                    machModel = machModel.Where(w => w.MachineryType.GetHashCode() == machinerytype).ToList();
                }


                var mModel = await _context.Maintenance.Where(w => w.DeleteMark == 0).ToListAsync();

                var malist = machModel.Select(s => new MachineryInfoViewModelS
                {
                    MachineryInfoId = s.MachineryInfoId,
                    PropertyRightsRecordNo = s.PropertyRightsRecordNo,
                    EntGUID = s.EntGUID,
                    MachineryType = ((MachineryTypeEnum)s.MachineryType).ToString(),
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
                    ReviewBelongedTo = s.ReviewBelongedTo == null ? "" : _context.CityZone.Where(w => w.BelongedTo == s.ReviewBelongedTo)
                .Select(s => s.SuperOrganName).FirstOrDefault(),
                    FileCount = mModel.Where(w => w.MachineryInfoId == s.MachineryInfoId).Count(),
                    //QualityNo = s.QualityNo,
                }).OrderByDescending(o => o.CreateDate).ToList();



                var totalCount = malist.Count;
                //������Դ��ҳ
                malist = malist.OrderByDescending(o => o.CreateDate).Skip((total - 1) * size).Take(size).ToList();
                return ResponseViewModel<List<MachineryInfoViewModelS>>.Create(Status.SUCCESS, Message.SUCCESS, malist, totalCount);

            }
            catch (Exception ex)
            {

                return ResponseViewModel<List<MachineryInfoViewModelS>>.Create(Status.ERROR, Message.ERROR);
            }

        }

        /// <summary>
        /// ά�޸����¼���
        /// </summary>
        /// <param name="machineryInfoId">��еID</param>
        /// <returns></returns>
        [HttpPost]
        //[Authorize]
        public async Task<ResponseViewModel<string>> MaintenanceList(MaintenanceViewModel viewModel)
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id   

                Maintenance mModel = new Maintenance();

                mModel.MachineryInfoId = viewModel.MachineryInfoId;
                mModel.MajorRecords = viewModel.MajorRecords;
                mModel.MaintenanceRecordId = SecurityManage.GuidUpper();
                mModel.CreateDate = DateTime.Now;
                mModel.UpdateDate = DateTime.Now;
                mModel.DeleteMark = 0;

                _context.Maintenance.Add(mModel);

                //��ӹ����ͬ
                viewModel.PurchaseContractList.ForEach(g =>
                {
                    RecordAttachment rModel = new RecordAttachment()
                    {
                        FileName = g.FileName,
                        FileUrl = g.FileUrl,
                        RecordConfigId = "58363CD69FC449E3B6D66726AA05C0F5",
                        Type = (RecordConfigTypeEnum)5,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        DeleteMark = 0,
                        RecordAttachmentId = SecurityManage.GuidUpper(),
                        AttachmentId = mModel.MaintenanceRecordId

                    };
                    _context.RecordAttachments.AddRange(rModel);

                });
                //��ӷ�Ʊ����Чƾ֤
                viewModel.InvoiceList.ForEach(g =>
                {
                    RecordAttachment rModel = new RecordAttachment()
                    {
                        FileName = g.FileName,
                        FileUrl = g.FileUrl,
                        RecordConfigId = "5C68552FB15C496DB0091477E2741E7D",
                        Type = (RecordConfigTypeEnum)5,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        DeleteMark = 0,
                        RecordAttachmentId = SecurityManage.GuidUpper(),
                        AttachmentId = mModel.MaintenanceRecordId

                    };
                    _context.RecordAttachments.AddRange(rModel);

                });

                viewModel.QualificationsList.ForEach(g =>
                {
                    RecordAttachment rModel = new RecordAttachment()
                    {
                        FileName = g.FileName,
                        FileUrl = g.FileUrl,
                        RecordConfigId = "F185B58DC4384B97B1EE206A73DDF071",
                        Type = (RecordConfigTypeEnum)5,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        DeleteMark = 0,
                        RecordAttachmentId = SecurityManage.GuidUpper(),
                        AttachmentId = mModel.MaintenanceRecordId

                    };
                    _context.RecordAttachments.AddRange(rModel);

                });





                _context.SaveChanges();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, tokenId);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }


        }


        /// <summary>
        /// ά�޸�����ʷ��¼
        /// </summary>
        /// <param name="machineryInfoId">��еID</param>
        /// <returns></returns>
        [HttpGet]
        //[Authorize]
        public async Task<ResponseViewModel<List<MaintenanceViewModel>>> MaintenanceHistoryList(string machineryInfoId)
        {
            try
            {
                List<MaintenanceViewModel> listMain = new List<MaintenanceViewModel>();
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id   

                var maList = await _context.Maintenance.Where(w => w.MachineryInfoId == machineryInfoId).ToListAsync();

                var idList = maList.Select(s => s.MaintenanceRecordId).ToList();

                var reList = await _context.RecordAttachments.Where(w => w.Type == (RecordConfigTypeEnum)5).ToListAsync();


                maList.ForEach(g =>
                {

                    var listm = maList.Where(w => w.MaintenanceRecordId == g.MaintenanceRecordId).Select(s => new MaintenanceViewModel
                    {
                        MachineryInfoId = s.MachineryInfoId,
                        MajorRecords = s.MajorRecords,
                        CreateDate = s.CreateDate,
                        PurchaseContractList = reList.Where(w => w.AttachmentId == g.MaintenanceRecordId
                         && w.RecordConfigId == "58363CD69FC449E3B6D66726AA05C0F5").Select(s => new MaintenanceImgList
                         {
                             FileName = s.FileName,
                             FileUrl = s.FileUrl,
                             Suffix = Path.GetExtension(s.FileUrl).Replace(".", ""),
                         }).ToList(),
                        InvoiceList = reList.Where(w => w.AttachmentId == g.MaintenanceRecordId
                                   && w.RecordConfigId == "5C68552FB15C496DB0091477E2741E7D").Select(s => new MaintenanceImgList
                                   {
                                       FileName = s.FileName,
                                       FileUrl = s.FileUrl,
                                       Suffix = Path.GetExtension(s.FileUrl).Replace(".", ""),
                                   }).ToList(),
                        QualificationsList = reList.Where(w => w.AttachmentId == g.MaintenanceRecordId
                                 && w.RecordConfigId == "F185B58DC4384B97B1EE206A73DDF071").Select(s => new MaintenanceImgList
                                 {
                                     FileName = s.FileName,
                                     FileUrl = s.FileUrl,
                                     Suffix = Path.GetExtension(s.FileUrl).Replace(".", ""),
                                 }).ToList(),
                    }).FirstOrDefault();

                    listMain.Add(listm);

                });


                _context.SaveChanges();

                return ResponseViewModel<List<MaintenanceViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, listMain);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<List<MaintenanceViewModel>>.Create(Status.ERROR, Message.ERROR);
            }


        }



        /// <summary>
        /// ��е��Ϣ���
        /// </summary>
        /// <param name="total"></param>
        /// <param name="size"></param>
        /// <param name="machinerytype"></param>
        /// <param name="machineryName"></param>
        /// <param name="leaveTheFactoryNo"></param>
        /// <param name="manufacturingLicense"></param>
        /// <returns></returns>
        public async Task<ResponseViewModel<List<MachineryInfoViewModelS>>> RecordMachineryInstall(int page, int size, string belongedTo, string recordNumber)
        {
            try
            {
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//��ҵ��Id  


                var install = await _context.InstallationNotificationRecords.Where(w => w.Type == 0 && w.RecordNumber == recordNumber
                && w.BelongedTo == belongedTo).Select(s => s.MachineryInfoId).ToListAsync();


                var machModel = await _context.MachineryInfos.Where(w => install.Contains(w.MachineryInfoId)
                && ((w.CheckState > (MachineryState)9 && w.CheckState < (MachineryState)12)
                || w.CheckState == (MachineryState)13) && w.DeleteMark == 0).ToListAsync();

                var malist = machModel.Select(s => new MachineryInfoViewModelS
                {
                    MachineryInfoId = s.MachineryInfoId,
                    PropertyRightsRecordNo = s.PropertyRightsRecordNo,
                    EntGUID = s.EntGUID,
                    MachineryType = ((MachineryTypeEnum)s.MachineryType).ToString(),
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
                    ReviewBelongedTo = s.ReviewBelongedTo == null ? "" : _context.CityZone.Where(w => w.BelongedTo == s.ReviewBelongedTo)
                 .Select(s => s.SuperOrganName).FirstOrDefault(),
                }).OrderByDescending(o => o.CreateDate).ToList();


                var count = malist.Count;

                malist = malist.Skip((page - 1 * size)).Take(size).ToList();

                //������Դ��ҳ
                return ResponseViewModel<List<MachineryInfoViewModelS>>.Create(Status.SUCCESS, Message.SUCCESS, malist, count);

            }
            catch (Exception ex)
            {

                return ResponseViewModel<List<MachineryInfoViewModelS>>.Create(Status.ERROR, Message.ERROR);
            }

        }


        /// <summary>
        /// ��е��Ϣ��ʷ��¼
        /// </summary>
        /// <param name="total"></param>
        /// <param name="size"></param>
        /// <param name="machinerytype"></param>
        /// <param name="machineryName"></param>
        /// <param name="leaveTheFactoryNo"></param>
        /// <param name="manufacturingLicense"></param>
        /// <returns></returns>
        public async Task<ResponseViewModel<List<RecordMachineryViewModel>>> RecordMachineryInstallHistory(string machineryInfoId)
        {
            try
            {

                List<RecordMachineryViewModel> rlist = new List<RecordMachineryViewModel>();
                //��Ȩ���� 
                var checkUrl = "";
                var model1 = await _context.MachineryInfos.Where(w => w.MachineryInfoId
                == machineryInfoId && w.State == 2).OrderByDescending(o => o.CreateDate).FirstOrDefaultAsync();

                if (model1 != null)
                {

                    checkUrl = model1.CheckUrl;

                    RecordMachineryViewModel model1s = new RecordMachineryViewModel()
                    {
                        Id = "1",
                        type = "��Ȩ����",
                        CreateDate = model1.CreateDate.ToString(),
                        CheckUrl = model1.CheckUrl
                    };

                    rlist.Add(model1s);
                }
                //��װ��֪
                var model2 = await _context.InstallationNotificationRecords.Where(w => w.MachineryInfoId == machineryInfoId
                && w.State == 2).Select(s => new RecordMachineryViewModel
                {
                    Id = "2",
                    type = "��װ��֪",
                    CreateDate = s.CreateDate.ToString(),
                    CheckUrl = ""
                }).OrderByDescending(o => o.CreateDate).FirstOrDefaultAsync();
                if (model2 != null)
                {
                    rlist.Add(model2);
                }
                //���ϸ�ʱ��
                var model3 = await _context.CheckRecords.Where(w => w.MachineryInfoId == machineryInfoId
                && w.CheckState == (MachineryCheckStateEnum)16)
                 .Select(s => new RecordMachineryViewModel
                 {
                     Id = "3",
                     type = "���ϸ�",
                     CreateDate = s.CreateDate.ToString(),
                     CheckUrl = checkUrl
                 }).OrderByDescending(o => o.CreateDate).FirstOrDefaultAsync();

                if (model3 != null)
                {
                    rlist.Add(model3);
                }
                //����ʹ�õǼ�ʱ��
                var model4 = await _context.RegistrationOfUses.Where(w => w.MachineryInfoId == machineryInfoId
                && w.CheckState == (MachineryState)10).Select(s => new RecordMachineryViewModel
                {
                    Id = "4",
                    type = "����ʹ�õǼ�",
                    CreateDate = s.CreateDate.ToString(),
                    CheckUrl = ""
                }).OrderByDescending(o => o.CreateDate).FirstOrDefaultAsync();

                if (model4 != null)
                {
                    rlist.Add(model4);
                }
                //��ж��֪ʱ��(����ʹ��ע��ʱ��)--��Ϊǰһ��ֻ��ʾʹ���еĻ�е���Բ����ڲ�ж��֪֮��ļ�¼
                var model5 = await _context.InstallationNotificationRecords.Where(w => w.MachineryInfoId == machineryInfoId
               && w.State == 12).Select(s => new RecordMachineryViewModel
               {
                   Id = "5",
                   type = "����ʹ��ע��",
                   CreateDate = "",
                   CheckUrl = ""
               }).OrderByDescending(o => o.CreateDate).FirstOrDefaultAsync();


                if (model5 != null)
                {
                    rlist.Add(model5);
                }

                return ResponseViewModel<List<RecordMachineryViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, rlist, rlist.Count);
            }
            catch (Exception ex)
            {

                return ResponseViewModel<List<RecordMachineryViewModel>>.Create(Status.ERROR, Message.ERROR);
            }

        }
        /// <summary>
        /// �޸ĳ�ʡ״̬
        /// </summary>
        /// <param name="inRecord">��ʡ����</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> UpdateMachineryInfo([FromBody] OutRecordViewModel OutRecord)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(OutRecord.MachineryInfoId))
                {
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
                }

                var list = _context.MachineryInfos
                    .FirstOrDefault(s => s.MachineryInfoId == OutRecord.MachineryInfoId && s.DeleteMark == 0);
                if (list == null)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "û�в�ѯ������");
                }
                list.AccessStatus = OutRecord.AccessStatus;
                _context.MachineryInfos.Update(list);
                InOutRecord inOutRecord = new InOutRecord();
                inOutRecord.InOutRecordId = SecurityManage.GuidUpper();
                inOutRecord.MachineryInfoId = OutRecord.MachineryInfoId;
                inOutRecord.OutProvinceTime = OutRecord.OutProvinceTime;
                inOutRecord.OutProvinceAddress = OutRecord.OutProvinceAddress;
                inOutRecord.CheckState = list.CheckState;
                inOutRecord.Close = 0;
                inOutRecord.DeleteMark = 0;
                inOutRecord.CreateDate = DateTime.Now;
                inOutRecord.UpdateDate = DateTime.Now;
                _context.InOutRecords.Add(inOutRecord);
                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }
        /// <summary>
        /// �޸Ļ�ʡ״̬
        /// </summary>
        /// <param name="OutRecord">��ʡ����</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> UpdateMachineresInfo([FromBody] InRecordViewModel InRecord)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(InRecord.MachineryInfoId))
                {
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
                }
                var list = _context.MachineryInfos
                    .FirstOrDefault(s => s.MachineryInfoId == InRecord.MachineryInfoId && s.DeleteMark == 0);
                if (list == null)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "û�в�ѯ������");
                }
                list.AccessStatus = InRecord.AccessStatus;
                _context.MachineryInfos.Update(list);
                var model = _context.InOutRecords
                    .FirstOrDefault(s => s.MachineryInfoId == InRecord.MachineryInfoId && s.Close == 0);
                if (model == null)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "û�в�ѯ������");
                }
                model.InProvinceTime = InRecord.InProvinceTime;
                model.Close = 1;
                model.UpdateDate = DateTime.Now;
                _context.InOutRecords.Update(model);
                var config = _context.RecordConfigs
                    .FirstOrDefault(s => s.AttachmentName == "ʹ�õǼ�ע����" && s.DeleteMark == 0 && s.Type == RecordConfigTypeEnum.ʡ���豸ע��);
                RecordAttachment recordAttachment = new RecordAttachment();
                recordAttachment.RecordAttachmentId = SecurityManage.GuidUpper();
                recordAttachment.FileName = InRecord.FileName;
                recordAttachment.FileUrl = InRecord.url;
                recordAttachment.RecordConfigId = config.RecordConfigId;
                recordAttachment.DeleteMark = 0;
                recordAttachment.AttachmentId = model.InOutRecordId;
                recordAttachment.CreateDate = DateTime.Now;
                recordAttachment.UpdateDate = DateTime.Now;
                _context.RecordAttachments.Add(recordAttachment);
                await _context.SaveChangesAsync();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }
        /// <summary>
        /// �����豸��Ϣpdf
        /// </summary>
        /// <param name="machineryInfoId">��еid</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> GetDataPrint(string machineryInfoId, string imgurl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(machineryInfoId) || string.IsNullOrWhiteSpace(imgurl))
                {
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
                }
                var conn = Configuration.GetConnectionString("QRcodeurl");
                imgurl = conn + imgurl;
                var list = _context.MachineryInfos
                    .FirstOrDefault(s => s.MachineryInfoId == machineryInfoId && s.DeleteMark == 0);
                if (list == null)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "û�в�ѯ������");
                }

                var EntName = _context.EntRegisterInfoMag
                    .FirstOrDefault(s => s.EntRegisterInfoMagId == list.EntGUID)
                    .EntName;
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("MachineryType", list.MachineryType.ToString());
                dic.Add("PropertyRightsRecordNo", list.PropertyRightsRecordNo);
                dic.Add("MachineryModel", list.MachineryModel);
                dic.Add("LeaveTheFactoryNo", list.LeaveTheFactoryNo);
                dic.Add("EntName", EntName);
                dic.Add("OEM", list.OEM);
                dic.Add("LeaveTheFactoryDate", list.LeaveTheFactoryDate == null ? "" : string.Format("{0:d}", list.LeaveTheFactoryDate));
                dic.Add("DateTime", DateTime.Now.ToString("yyyy-MM-dd"));
                string webRootPath = _wordTemplte + "Machinery/ProvincialData.docx";
                var bitmap = _iqrcode.GetQRCode(imgurl, 2, null);
                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Jpeg);
                var url = Util.BuildWordAndImg(_environment, Request, dic, new List<List<string>>(), webRootPath, "Machinery", ms, machineryInfoId + "ProvincialData.doc");
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, url);
            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }
        /// <summary>
        /// ��ȡurl
        /// <param name="iform"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseViewModel<string> UpImg([FromForm] IFormCollection iform)
        {
            try
            {
                var url = Util.UploadFileToServer(iform.Files[0], _environment, Request, "OutProvinceImg");
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, url);
            }
            catch (Exception ex)
            {
                _logger.LogError("url��ȡʧ�ܣ�" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }
        /// <summary>
        /// ��ȡ��е��ϸ��Ϣ
        /// <param name="machineryInfoid">��еid</param>
        /// <returns></returns>
        [HttpGet]
        public ResponseViewModel<MachineryListViewModel> GetMachinery(string machineryInfoid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(machineryInfoid))
                {
                    return ResponseViewModel<MachineryListViewModel>.Create(Status.SUCCESS, Message.SUCCESS, new MachineryListViewModel());
                }
                var data = from A in _context.MachineryInfos.Where(w => w.DeleteMark == 0)
                           join B in _context.EntRegisterInfoMag on A.EntGUID equals B.EntRegisterInfoMagId
                           select new MachineryListViewModel
                           {
                               MachineryType = A.MachineryType,
                               PropertyRightsRecordNo = A.PropertyRightsRecordNo,
                               MachineryModel = A.MachineryModel,
                               LeaveTheFactoryNo = A.LeaveTheFactoryNo,
                               ManufacturingLicense = A.ManufacturingLicense,
                               EntGUID = A.EntGUID,
                               EntName = B.EntName.ToString(),
                               CheckState = A.CheckState,
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
                               ReviewDate = A.ReviewDate,

                           };
                var model = data.FirstOrDefault(s => s.MachineryInfoId == machineryInfoid);
                return ResponseViewModel<MachineryListViewModel>.Create(Status.SUCCESS, Message.SUCCESS, model);
            }
            catch (Exception ex)
            {
                _logger.LogError("url��ȡʧ�ܣ�" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<MachineryListViewModel>.Create(Status.ERROR, Message.ERROR);
            }
        }
    }

}