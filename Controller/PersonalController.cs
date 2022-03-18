using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Aspose.Words.Lists;
using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Common.DataBll;
using JSAJ.Core.Controllers;
using JSAJ.Core.Models;
using JSAJ.Core.Models.LargeMachinery;
using JSAJ.Core.ViewModels;
using JSAJ.ViewModels.ViewModels;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPOI.SS.Formula.Functions;
using ViewModels;

namespace JSAJNanJing.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]

    public class PersonalController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MenusController> _logger;
        private readonly JssanjianmanagerContext _context;
        private JwtSettings settings;
        private OssFileSetting _ossFileSetting;
        public PersonalController(IWebHostEnvironment environment, ILogger<MenusController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options, IOptions<OssFileSetting> oss)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            settings = options.Value;
            _ossFileSetting = oss.Value;
        }
        /// <summary>
        /// 获取人员类型
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<PersonType>>> GetPeopleType()
        {
            try
            {
                var data = await _context.PersonTypes
                    .Where(w => w.DeleteMark == 0)
                    .AsNoTracking()
                    .ToListAsync();
                return ResponseViewModel<List<PersonType>>.Create(Status.SUCCESS, Message.SUCCESS, data);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取人员类型：" + ex.Message, ex);
                return ResponseViewModel<List<PersonType>>.Create(Status.ERROR, Message.ERROR);
            }

        }
        //获取工种
        [HttpGet]
        public ResponseViewModel<List<WorkTypeCodeViewModel>> GetWorkTypeCode()
        {
            Dictionary<int, SpecialWorker> aa = Worker.WorkerDic;
            var data = aa.Where(a => a.Value.IsSpecial == true).Select(a => new WorkTypeCodeViewModel { Name = a.Value.Value, key = a.Key }).ToList();
            return ResponseViewModel<List<WorkTypeCodeViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, data);
        }


        //查询
        [HttpGet]
        public async Task<ResponseViewModel<List<MachineryPeopleViewModel>>> SearchMachineryPeople(int pageIndex, int pageSize, int type, int? userType, string userName)
        {
            try
            {
                var entGuid = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                var data = _context.MachineryPeoples
                    .Where(w => w.DeleteMark == 0 && w.Type == type && w.EntGUID == entGuid);
                if (userType != null)
                {
                    data = data.Where(w => w.PersonType == userType);
                }
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    data = data.Where(w => w.PersonName.Contains(userName));
                }
                var count = await data.CountAsync();
                if (count <= 0)
                {
                    return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<MachineryPeopleViewModel>(), count);
                }
                if (pageIndex == 0)
                {
                    pageIndex = 1;
                }
                if (pageSize == 0)
                {
                    pageSize = 10;
                }
                var result = await data.OrderByDescending(o => o.CreateDate)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new MachineryPeopleViewModel
                    {
                        PersonTypeLabel = _context.PersonTypes
                            .Where(w => w.Code == s.PersonType)
                            .Select(s2 => s2.Name)
                            .FirstOrDefault(),
                        BelongedTo = s.BelongedTo,
                        CerAgency = s.CerAgency,
                        CerName = s.CerName,
                        CerUrl = s.CerUrl,
                        CerValidBeginDate = s.CerValidBeginDate,
                        CerValidEndDate = s.CerValidEndDate,
                        CreateDate = s.CreateDate,
                        DeleteMark = s.DeleteMark,
                        EntGUID = s.EntGUID,
                        Id = s.Id,
                        IdCard = s.IdCard,
                        IdCardUrl = s.IdCardUrl,
                        MachineryPersonId = s.MachineryPersonId,
                        PersonName = s.PersonName,
                        PersonType = s.PersonType,
                        RecordNumber = s.RecordNumber,
                        Sex = s.Sex,
                        Tel = s.Tel,
                        Type = s.Type,
                        UpdateDate = s.UpdateDate,
                        WorkTypeCode = s.WorkTypeCode,
                        SpecialWorkerTypeNo = s.SpecialWorkerTypeNo
                    })
                    .AsNoTracking()
                    .ToListAsync();
                return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取机械人员：" + ex.Message, ex);
                return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }
        [HttpGet]
        public async Task<ResponseViewModel<List<MachineryPeopleViewModel>>> SearchMachineryPeoplePro(int pageIndex, int pageSize, int type, int? userType, string userName, string recordNumber, string belongedTo)
        {
            try
            {
                //   var entGuid = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value; 
                var data = _context.MachineryPeoples
                    .Where(w => w.DeleteMark == 0 && w.Type == type && w.RecordNumber == recordNumber && w.BelongedTo == belongedTo);
                if (userType != null)
                {
                    data = data.Where(w => w.PersonType == userType);
                }
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    data = data.Where(w => w.PersonName.Contains(userName));
                }
                var count = await data.CountAsync();
                if (count <= 0)
                {
                    return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<MachineryPeopleViewModel>(), count);
                }
                if (pageIndex == 0)
                {
                    pageIndex = 1;
                }
                if (pageSize == 0)
                {
                    pageSize = 10;
                }
                var result = await data.OrderByDescending(o => o.CreateDate)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new MachineryPeopleViewModel
                    {
                        PersonTypeLabel = _context.PersonTypes
                            .Where(w => w.Code == s.PersonType)
                            .Select(s2 => s2.Name)
                            .FirstOrDefault(),
                        BelongedTo = s.BelongedTo,
                        CerAgency = s.CerAgency,
                        CerName = s.CerName,
                        CerUrl = s.CerUrl,
                        CerValidBeginDate = s.CerValidBeginDate,
                        CerValidEndDate = s.CerValidEndDate,
                        CreateDate = s.CreateDate,
                        DeleteMark = s.DeleteMark,
                        EntGUID = s.EntGUID,
                        Id = s.Id,
                        IdCard = s.IdCard,
                        IdCardUrl = s.IdCardUrl,
                        MachineryPersonId = s.MachineryPersonId,
                        PersonName = s.PersonName,
                        PersonType = s.PersonType,
                        RecordNumber = s.RecordNumber,
                        Sex = s.Sex,
                        Tel = s.Tel,
                        Type = s.Type,
                        UpdateDate = s.UpdateDate,
                        WorkTypeCode = s.WorkTypeCode,
                        SpecialWorkerTypeNo = s.SpecialWorkerTypeNo
                    })
                    .AsNoTracking()
                    .ToListAsync();
                return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取机械人员：" + ex.Message, ex);
                return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }
        //获取url
        [HttpPost]
        public ResponseViewModel<string> UpImg([FromForm] IFormCollection iform)
        {
            try
            {
                var url = Util.UploadFileToServer(iform.Files[0], _environment, Request, "MachineryPeopleImg");

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, url);
                //var fileImg = iform.Files[0];
                //var fileUrl = Util.UploadAnyFile(fileImg, _environment, Request, "uploadFile");
                //return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, fileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError("url获取失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


        //根据MachineryPersonId 删除,修改字段逻辑删除
        [HttpPost]
        public async Task<ResponseViewModel<object>> DeleteMachineryPeople([FromBody] List<string> listId)
        {
            try
            {
                if (listId.Count > 0)
                {
                    var peopple = _context.MachineryPeoples.Where(s => listId.Contains(s.MachineryPersonId));
                    //查询被锁人员
                    var data = await _context.InstallPeoples.Where(w => listId.Contains(w.MachineryPersonId) && w.DeleteMark == 0 && w.IsFree == 0).Select(k => k.MachineryPersonId).ToListAsync();
                    var resuft = await peopple.Where(w => !data.Contains(w.MachineryPersonId)).ToListAsync();

                    //var peopple = from A in _context.MachineryPeoples
                    //              join B in _context.InstallPeoples
                    //              on A.MachineryPersonId equals B.MachineryPersonId
                    //              into T1
                    //              from B1 in T1.DefaultIfEmpty()
                    //              where A.DeleteMark == 0 && B1.IsFree != 0 && B1.DeleteMark == 0 && listId.Contains(A.MachineryPersonId)
                    //              select A;


                    foreach (var item in resuft)
                    {
                        item.DeleteMark = 1;
                    }
                    _context.MachineryPeoples.UpdateRange(resuft);
                    await _context.SaveChangesAsync();
                    return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, "删除成功");
                }

                else
                {
                    return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL, "参数错误");
                }

            }
            catch (Exception)
            {
                return ResponseViewModel<object>.Create(Status.FAIL, Message.FAIL, "删除失败");
            }
        }

        ////查看
        //[HttpGet]
        //public async Task<ResponseViewModel<MachineryPeopleViewModel>> GetMachineryPeople(string personId)
        //{
        //    var data = await _context.MachineryPeoples.Where(a => a.MachineryPersonId == personId)
        //        .Select(a => new MachineryPeopleViewModel
        //        {
        //            PersonType = a.PersonType,  //人员类型
        //            PersonName = a.PersonName,   //姓名
        //            Sex = a.Sex == 0 ? "男" : "女", //性别
        //            IdCard = a.IdCard,  //身份证
        //            CerName = a.CerName,//证书名称
        //            CerAgency = a.CerAgency,//发证机构
        //            Tel = a.Tel, //电话
        //            WorkTypeCode = a.WorkTypeCode,   //工种
        //            beginTime = ((DateTime)a.CerValidBeginDate).ToString("yyyy-MM-dd"),
        //            endTime = ((DateTime)a.CerValidEndDate).ToString("yyyy-MM-dd"),
        //            MachineryPersonId = a.MachineryPersonId,
        //            IdCardUrl = a.IdCardUrl,
        //            CerUrl = a.CerUrl
        //        }).FirstOrDefaultAsync();

        //    return ResponseViewModel<MachineryPeopleViewModel>.Create(Status.SUCCESS, Message.SUCCESS, data);
        //}


        /// <summary>
        /// 修改人员信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> UptMachineryPeople([FromBody] MachineryPeople model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.PersonName) ||
                    model.Sex == null || string.IsNullOrWhiteSpace(model.IdCard) ||
                     string.IsNullOrWhiteSpace(model.IdCard) ||
                    string.IsNullOrWhiteSpace(model.Tel) || model.WorkTypeCode == 0)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "带*为必填项，请检查是否填写完整！");
                }

                //判断证书号是否在其他项目上添加过

                var datazs = await _context.MachineryPeoples
                  .Where(a => (a.SpecialWorkerTypeNo.Trim() == model.SpecialWorkerTypeNo.Trim())
                  && a.MachineryPersonId != model.MachineryPersonId
                  && a.DeleteMark == 0)
                  .OrderByDescending(o => o.CreateDate)
                  .FirstOrDefaultAsync();
                if (datazs != null)
                {
                    var aa = "";
                    string request = "";
                    if (datazs.Type == 0)
                    {
                        aa = _context.EntRegisterInfoMag
                            .Where(w => w.EntRegisterInfoMagId == datazs.EntGUID && w.DeleteMark == 0)
                            .Select(k => k.EntName)
                            .FirstOrDefault();
                        request = datazs.SpecialWorkerTypeNo + "该证书已存在" + aa + "企业";
                        return ResponseViewModel<string>.Create(Status.ERROR, request);
                    }
                    else
                    {
                        aa = _context.ProjectOverview.Where(w => w.RecordNumber == datazs.RecordNumber && w.BelongedTo == datazs.BelongedTo)
                            .Select(k => k.ProjectName)
                            .FirstOrDefault();
                        request = datazs.SpecialWorkerTypeNo + "该证书已存在" + aa + "项目";
                        return ResponseViewModel<string>.Create(Status.ERROR, request);
                    }
                }
                var data = await _context.MachineryPeoples
                    .Where(a => a.MachineryPersonId == model.MachineryPersonId)
                    .FirstOrDefaultAsync();



                data.PersonType = model.PersonType;
                data.PersonName = model.PersonName;
                data.Sex = model.Sex;
                data.Tel = model.Tel;
                data.IdCard = model.IdCard;
                data.CerName = model.CerName;
                data.CerAgency = model.CerAgency;
                data.CerValidBeginDate = model.CerValidBeginDate;
                data.CerValidEndDate = model.CerValidEndDate;

                data.WorkTypeCode = model.WorkTypeCode;
                data.IdCardUrl = model.IdCardUrl;
                data.CerUrl = model.CerUrl;
                data.UpdateDate = DateTime.Now;
                data.SpecialWorkerTypeNo = model.SpecialWorkerTypeNo;

                _context.MachineryPeoples.Update(data);
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
        /// 新增人员
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> AddPeoples([FromBody] MachineryPeople model)
        {
            try
            {
                var portType = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//后台账号登录
                string uuid = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;

                //根证书证号判断当前人员是否在其他单位存在
                var type = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;
                var data = await _context.MachineryPeoples
                    .Where(a => (a.SpecialWorkerTypeNo.Trim() == model.SpecialWorkerTypeNo.Trim()) && a.DeleteMark == 0)
                    .OrderByDescending(o => o.CreateDate)
                    .FirstOrDefaultAsync();
                if (data != null)
                {
                    var aa = "";
                    string request = "";
                    if (data.Type == 0)
                    {
                        aa = _context.EntRegisterInfoMag
                            .Where(w => w.EntRegisterInfoMagId == data.EntGUID && w.DeleteMark == 0)
                            .Select(k => k.EntName)
                            .FirstOrDefault();
                        aa = aa == null ? "其他" : aa;
                        request = data.SpecialWorkerTypeNo + "该证书已存在" + aa + "企业";
                        return ResponseViewModel<string>.Create(Status.ERROR, request);
                    }
                    else
                    {
                        aa = _context.ProjectOverview.Where(w => w.RecordNumber == data.RecordNumber && w.BelongedTo == data.BelongedTo)
                            .Select(k => k.ProjectName)
                            .FirstOrDefault();
                        aa = aa == null ? "其他" : aa;
                        request = data.SpecialWorkerTypeNo + "该证书已存在" + aa + "项目";
                        return ResponseViewModel<string>.Create(Status.ERROR, request);
                    }


                }

                model.MachineryPersonId = SecurityManage.GuidUpper();

                model.CreateDate = DateTime.Now;
                model.EntGUID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                model.Type = 0;
                if (type == "0")
                {
                    model.Type = 1;
                }
                model.UpdateDate = DateTime.Now;

                string msg = "";
                //根身份证号判断当前人员是否在其他单位存在
                var sfzdata = await (from a in _context.MachineryPeoples.Where(a => a.IdCard.Trim() == model.IdCard && a.DeleteMark == 0)
                                     join c in _context.ProjectOverview
                                     on new { a.BelongedTo, a.RecordNumber } equals new { c.BelongedTo, c.RecordNumber }
                                     select new { a.PersonName, a.SpecialWorkerTypeNo, c.ProjectName, c.RecordNumber, c.BelongedTo }).Distinct().ToListAsync();
                if (sfzdata.Count > 0)
                {

                    string xiangmu = "";
                    sfzdata.ForEach(s =>
                    {
                        xiangmu += s.ProjectName + "|";

                    });
                    xiangmu = xiangmu.TrimEnd('|');
                    msg = "添加成功！但是该特种作业人员已在[" + xiangmu + "]项目登记工作，请核对该作业人员是否为本人，如非本人请在系统中删除。（本信息将同时推送该特种作业人员登记的项目及安全监管部门，对于重复登记的特种作业人员，系统将推送安全监管部门核查）";

                    var beianhao = sfzdata.Select(s => s.RecordNumber).ToList();
                    var benxiangmu = _context.ProjectOverview.Where(r => r.RecordNumber == model.RecordNumber).FirstOrDefault();

                    Dictionary<string, string> dic = new Dictionary<string, string>();
                    dic.Add("@姓名@", model.PersonName);
                    dic.Add("@身份证号@", model.IdCard);
                    dic.Add("@项目名称@", benxiangmu.ProjectName);
                    dic.Add("@后录入项目@", benxiangmu.ProjectName);
                    dic.Add("@先录入项目@", xiangmu);
                    //提醒先输入方
                    new DataBll(_context).AddNoticeList("A0046", dic, "", null, 0, beianhao);

                    //提醒主管部门
                    new DataBll(_context).AddNotice("A0047", dic, "", null, 0, benxiangmu.BelongedTo, benxiangmu.RecordNumber);
                    sfzdata.ForEach(e =>
                    {
                        //先输入方的主管部门
                        new DataBll(_context).AddNotice("A0047", dic, "", null, 0, e.BelongedTo, e.RecordNumber);
                    });
                }


                await _context.MachineryPeoples.AddAsync(model);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, msg);

            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "系统异常，添加失败" + ex.Message + ex.StackTrace);
            }

        }

        //是否存在 之后的确定  新增
        //[HttpPost]
        //[Authorize]
        // public async Task<ResponseViewModel<string>> AddPeoplesTrue([FromBody] MachineryPeopleViewModel model)
        // {
        //     try
        //     {
        //         MachineryPeople mp = new MachineryPeople();
        //         mp.MachineryPersonId = SecurityManage.GuidUpper();
        //         mp.PersonType = model.PersonType;
        //         mp.PersonName = model.PersonName;
        //         mp.Sex = model.Sex == "男" ? 0 : 1;
        //         mp.CerAgency = model.CerAgency;
        //         mp.IdCard = model.IdCard;
        //         mp.Tel = model.Tel;
        //         mp.WorkTypeCode = model.WorkTypeCode;
        //         mp.CerValidBeginDate = Convert.ToDateTime(model.CerValidBeginDate);
        //         mp.CerValidEndDate = Convert.ToDateTime(model.CerValidEndDate);
        //         mp.PersonType = model.PersonType;
        //         mp.IdCardUrl = model.IdCardUrl;
        //         mp.CerUrl = model.CerUrl;
        //         mp.CerName = model.CerName;
        //         mp.CreateDate = DateTime.Now;
        //         mp.UpdateDate = DateTime.Now;

        //         mp.Type = 0;
        //         mp.EntGUID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
        //         await _context.MachineryPeoples.AddAsync(mp);
        //         await _context.SaveChangesAsync();
        //         return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "添加成功!");

        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError("系统异常，添加失败：" + ex.Message + ex.StackTrace, ex);
        //         return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "系统异常，添加失败");
        //     }
        // }
        [HttpGet]
        public async Task<ResponseViewModel<List<DriversMachinery>>> GetDriversMachineryList(string recordNumber, string belongedTo, int peopleType, int pageIndex, int pageSize, string name, string macheName)
        {
            try
            {

                var data = from A in _context.InstallPeoples
                           join B in _context.MachineryInfos
                           on A.MachineryInfoId equals B.MachineryInfoId
                           into t1
                           from r in t1.DefaultIfEmpty()
                           join C in _context.MachineryPeoples on A.MachineryPersonId equals C.MachineryPersonId
                           where A.RecordNumber == recordNumber && A.BelongedTo == belongedTo && A.DeleteMark == 0
                           && A.IsFree == 0 && A.Type == 2
                           select new DriversMachinery
                           {
                               Sex = C.Sex == 0 ? "男" : C.Sex == 1 ? "女" : "未知",
                               EffectiveTime = C.CerValidEndDate,
                               IdCard = C.IdCard,
                               PersonPhone = C.Tel,
                               MachineryName = r.MachineryName,
                               MachineryInfoId = r.MachineryInfoId,
                               PropertyRightsRecordNo = r.PropertyRightsRecordNo,
                               Name = C.PersonName,
                               UpdateTime = A.UpdateDate,
                               MachineryType = r.MachineryType.GetHashCode(),
                               PersonType = C.WorkTypeCode,
                               MachineryPersonId = C.MachineryPersonId,


                           };
                if (peopleType != 0)
                {
                    data = data.Where(s => s.PersonType == peopleType);
                }
                if (!string.IsNullOrWhiteSpace(name))
                {
                    data = data.Where(w => w.Name.Contains(name));
                }
                if (!string.IsNullOrWhiteSpace(macheName))
                {
                    data = data.Where(w => w.MachineryName.Contains(macheName));
                }

                var count = data.Count();
                var result = data.OrderByDescending(o => o.UpdateTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                result.ForEach(x =>
                {
                    if (x.PersonType != null && Worker.WorkerDic.ContainsKey((int)x.PersonType))
                    {
                        x.PersonTypeName = Worker.WorkerDic[(int)x.PersonType].Value;
                    }
                    else
                    {
                        x.PersonTypeName = "其他";
                    }
                });
                return ResponseViewModel<List<DriversMachinery>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);

            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，添加失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<DriversMachinery>>.Create(Status.ERROR, "系统异常");
            }
        }


        [HttpGet]
        public async Task<ResponseViewModel<List<PersonInfoViewModel>>> GetPersons(int personType, string recordNumber, string belongedTo)
        {
            try
            {

                var machery = _context.MachineryPeoples
                    .Where(s => s.BelongedTo == belongedTo && s.RecordNumber == recordNumber && s.DeleteMark == 0 && s.WorkTypeCode == personType);

                var query = await _context.InstallPeoples
                    .Where(s => s.BelongedTo == belongedTo && s.RecordNumber == recordNumber && s.IsFree == 0 && s.DeleteMark == 0)
                    .Select(k => k.MachineryPersonId)
                    .ToListAsync();

                var data = machery.Where(s => !query.Contains(s.MachineryPersonId))
                    .Select(k => new PersonInfoViewModel
                    {
                        PersonName = k.PersonName,
                        MachineryPersonId = k.MachineryPersonId
                    }).ToList();
                return ResponseViewModel<List<PersonInfoViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, data);
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，添加失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<PersonInfoViewModel>>.Create(Status.ERROR, "系统异常");
            }
        }


        [HttpGet]
        public async Task<ResponseViewModel<object>> PersonInfos(string machineryPersonId)
        {
            try
            {
                var query = await _context.MachineryPeoples.Where(s => s.MachineryPersonId == machineryPersonId && s.DeleteMark == 0)
                    .FirstOrDefaultAsync();
                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, query);
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，添加失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, "系统异常");
            }
        }
        [HttpPost]
        public async Task<ResponseViewModel<string>> SavePersonChange(PersonInfoChangeViewModel viewmodel)
        {
            try
            {
                if (string.IsNullOrEmpty(viewmodel.MachineryInfoId)
                    || string.IsNullOrEmpty(viewmodel.NewMachineryPersonId)
                    || string.IsNullOrEmpty(viewmodel.BelongedTo)
                    || string.IsNullOrEmpty(viewmodel.RecordNumber)
                    || string.IsNullOrEmpty(viewmodel.MachineryPersonId))

                {
                    return ResponseViewModel<string>.Create(Status.ERROR, Message.FAIL);
                }
                var query = await _context.InstallPeoples.Where(s => s.BelongedTo == viewmodel.BelongedTo && s.RecordNumber == viewmodel.RecordNumber
                && s.MachineryPersonId == viewmodel.MachineryPersonId && s.DeleteMark == 0 && s.IsFree == 0)
                    .FirstOrDefaultAsync();
                query.IsFree = 1;//解绑人员
                var query1 = await _context.MachineryPeoples.Where(s => s.MachineryPersonId == viewmodel.NewMachineryPersonId && s.DeleteMark == 0)
                    .FirstOrDefaultAsync();
                InstallPeople peoples = new InstallPeople();
                peoples.InstallPeopleId = SecurityManage.GuidUpper();
                peoples.MachineryPersonId = viewmodel.NewMachineryPersonId;
                peoples.BelongedTo = viewmodel.BelongedTo;
                peoples.RecordNumber = viewmodel.RecordNumber;
                peoples.IsFree = 0;
                peoples.MachineryInfoId = viewmodel.MachineryInfoId;
                peoples.DeleteMark = 0;
                //peoples.Type = query.Type;
                peoples.Type = 2;
                peoples.CreateDate = DateTime.Now;
                peoples.UpdateDate = DateTime.Now;
                peoples.InstallationNotificationRecordId = query.InstallationNotificationRecordId;
                _context.InstallPeoples.Update(query);
                _context.InstallPeoples.Add(peoples);

                var query3 = await _context.ProjectEntSnapshot
                   .Where(s => s.RecordNumber == viewmodel.RecordNumber
                   && s.BelongedTo == viewmodel.BelongedTo
                   && s.EnterpriseType == "施工单位" && s.MainUnit == "是")
                   .FirstOrDefaultAsync();
                DriverChangeRecord mode = new DriverChangeRecord();
                mode.ConstructionUnit = query3.EnterpriseName;
                mode.MachineryPersonId = viewmodel.NewMachineryPersonId;
                mode.MachineryInfoId = viewmodel.MachineryInfoId;
                mode.DeleteMark = 0;
                mode.DriverChangeRecordId = SecurityManage.GuidUpper();
                mode.CreateDate = DateTime.Now;
                mode.UpdateDate = DateTime.Now;
                _context.DriverChangeRecords.Add(mode);
                _context.SaveChanges();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，添加失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, "系统异常");
            }
        }
        [HttpGet]
        public async Task<ResponseViewModel<List<ChangeRecords>>> PersonChangeRecord(string machineryInfoId)
        {
            try
            {
                var query = await (from a in _context.DriverChangeRecords.Where(s => s.MachineryInfoId == machineryInfoId && s.DeleteMark == 0)
                                   join b in _context.MachineryPeoples
                                   on a.MachineryPersonId equals b.MachineryPersonId
                                   orderby a.CreateDate descending
                                   select new ChangeRecords
                                   {
                                       ChangeTime = a.CreateDate,
                                       MachineryPersonId = a.MachineryPersonId,
                                       PersonName = b.PersonName,
                                       EntName = a.ConstructionUnit

                                   }).ToListAsync();
                return ResponseViewModel<List<ChangeRecords>>.Create(Status.SUCCESS, Message.SUCCESS, query);
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，添加失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<ChangeRecords>>.Create(Status.ERROR, "系统异常");
            }
        }

        /// <summary>
        /// 安拆单位添加安拆人员信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> AddAnChaPeoples([FromBody] MachineryPeople model)
        {
            try
            {
                var portType = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//后台账号登录
                string uuid = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;

                //根证书证号判断当前人员是否在其他单位存在
                var type = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;
                var data = await _context.MachineryPeoples
                    .Where(a => (a.SpecialWorkerTypeNo.Trim() == model.SpecialWorkerTypeNo.Trim()) && a.DeleteMark == 0)
                    .OrderByDescending(o => o.CreateDate)
                    .FirstOrDefaultAsync();
                var data1 = await _context.MachineryPeoples
                                .Where(a => a.IdCard==model.IdCard && a.SpecialWorkerTypeNo==model.SpecialWorkerTypeNo && a.DeleteMark == 0)
                                .OrderByDescending(o => o.CreateDate)
                                .FirstOrDefaultAsync();
                if (data1!=null)
                {
                    return ResponseViewModel<string>.Create(Status.ERROR, "该人员已存在！");

                }
                if (data != null)
                {
                    var aa = "";
                    string request = "";
                    if (data.Type == 0)
                    {
                        aa = _context.EntRegisterInfoMag
                            .Where(w => w.EntRegisterInfoMagId == data.EntGUID && w.DeleteMark == 0)
                            .Select(k => k.EntName)
                            .FirstOrDefault();
                        aa = aa == null ? "其他" : aa;
                        request = data.SpecialWorkerTypeNo + "该证书已存在" + aa + "企业";
                        return ResponseViewModel<string>.Create(Status.ERROR, request);
                    }
                    else
                    {
                        aa = _context.ProjectOverview.Where(w => w.RecordNumber == data.RecordNumber && w.BelongedTo == data.BelongedTo)
                            .Select(k => k.ProjectName)
                            .FirstOrDefault();
                        aa = aa == null ? "其他" : aa;
                        request = data.SpecialWorkerTypeNo + "该证书已存在" + aa + "项目";
                        return ResponseViewModel<string>.Create(Status.ERROR, request);
                    }


                }

                model.MachineryPersonId = SecurityManage.GuidUpper();
                model.UseEndDate=DateTime.Now.Date.AddDays(-3);
                model.CreateDate = DateTime.Now;
                model.EntGUID = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;
                model.Type = 0;
                if (type == "0")
                {
                    model.Type = 1;
                }
                model.UpdateDate = DateTime.Now;

                string msg = "";
                //根身份证号判断当前人员是否在其他单位存在
                var sfzdata = await (from a in _context.MachineryPeoples.Where(a => a.IdCard.Trim() == model.IdCard && a.DeleteMark == 0)
                                     join c in _context.ProjectOverview
                                     on new { a.BelongedTo, a.RecordNumber } equals new { c.BelongedTo, c.RecordNumber }
                                     select new { a.PersonName, a.SpecialWorkerTypeNo, c.ProjectName, c.RecordNumber, c.BelongedTo }).Distinct().ToListAsync();
                if (sfzdata.Count > 0)
                {

                    string xiangmu = "";
                    sfzdata.ForEach(s =>
                    {
                        xiangmu += s.ProjectName + "|";

                    });
                    xiangmu = xiangmu.TrimEnd('|');
                    msg = "添加成功！但是该特种作业人员已在[" + xiangmu + "]项目登记工作，请核对该作业人员是否为本人，如非本人请在系统中删除。（本信息将同时推送该特种作业人员登记的项目及安全监管部门，对于重复登记的特种作业人员，系统将推送安全监管部门核查）";

                    var beianhao = sfzdata.Select(s => s.RecordNumber).ToList();
                    var benxiangmu = _context.ProjectOverview.Where(r => r.RecordNumber == model.RecordNumber).FirstOrDefault();

                    Dictionary<string, string> dic = new Dictionary<string, string>();
                    dic.Add("@姓名@", model.PersonName);
                    dic.Add("@身份证号@", model.IdCard);
                    dic.Add("@项目名称@", benxiangmu.ProjectName);
                    dic.Add("@后录入项目@", benxiangmu.ProjectName);
                    dic.Add("@先录入项目@", xiangmu);
                    //提醒先输入方
                    new DataBll(_context).AddNoticeList("A0046", dic, "", null, 0, beianhao);

                    //提醒主管部门
                    new DataBll(_context).AddNotice("A0047", dic, "", null, 0, benxiangmu.BelongedTo, benxiangmu.RecordNumber);
                    sfzdata.ForEach(e =>
                    {
                        //先输入方的主管部门
                        new DataBll(_context).AddNotice("A0047", dic, "", null, 0, e.BelongedTo, e.RecordNumber);
                    });
                }


                await _context.MachineryPeoples.AddAsync(model);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, msg);

            }
            catch (Exception ex)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "系统异常，添加失败" + ex.Message + ex.StackTrace);
            }

        }


        /// <summary>
        /// 检测所--查询安拆人员审核
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="type"></param>
        /// <param name="userType"></param>
        /// <param name="userName"></param>
        /// <param name="AuditStatus"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<MachineryPeopleViewModel>>> SearchSHPeoplePro(int pageIndex, int pageSize, int type, int? userType, string userName,int? AuditStatus)
        {
            try
            {
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                //var cityCode = _context.CityZone.Where(w => w.BelongedTo==belongedTo).Select(s => s.CityCode).FirstOrDefault();
                //var belongedTos = _context.CityZone.Where(w => w.ParentCityCode==cityCode).Select(s => s.BelongedTo).Distinct().ToList();
                //var Entcode = _context.ProjectEntSnapshot.Where(w => belongedTos.Contains(w.BelongedTo)).Select(s => s.OrganizationCode).Distinct().ToList();
                var data = _context.MachineryPeoples
                    .Where(w => w.DeleteMark == 0 && w.Type == type /*&& Entcode.Contains(w.EntGUID)*/ && w.AuditStatus!=0);
                if (userType != null)
                {
                    data = data.Where(w => w.PersonType == userType);
                }
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    data = data.Where(w => w.PersonName.Contains(userName));
                }
                if (AuditStatus != null)
                {
                    data = data.Where(w => w.AuditStatus == AuditStatus && w.AuditStatus!=0);
                }
                var count = await data.CountAsync();
                if (count <= 0)
                {
                    return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<MachineryPeopleViewModel>(), count);
                }
                if (pageIndex == 0)
                {
                    pageIndex = 1;
                }
                if (pageSize == 0)
                {
                    pageSize = 10;
                }
                var result = await data.OrderByDescending(o => o.CreateDate)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new MachineryPeopleViewModel
                    {
                        PersonTypeLabel = _context.PersonTypes
                            .Where(w => w.Code == s.PersonType)
                            .Select(s2 => s2.Name)
                            .FirstOrDefault(),
                        BelongedTo = s.BelongedTo,
                        CerAgency = s.CerAgency,
                        CerName = s.CerName,
                        CerUrl = s.CerUrl,
                        CerValidBeginDate = s.CerValidBeginDate,
                        CerValidEndDate = s.CerValidEndDate,
                        CreateDate = s.CreateDate,
                        DeleteMark = s.DeleteMark,
                        EntGUID = s.EntGUID,
                        Id = s.Id,
                        IdCard = s.IdCard,
                        IdCardUrl = s.IdCardUrl,
                        MachineryPersonId = s.MachineryPersonId,
                        PersonName = s.PersonName,
                        PersonType = s.PersonType,
                        RecordNumber = s.RecordNumber,
                        Sex = s.Sex,
                        Tel = s.Tel,
                        Type = s.Type,
                        UpdateDate = s.UpdateDate,
                        WorkTypeCode = s.WorkTypeCode,
                        SpecialWorkerTypeNo = s.SpecialWorkerTypeNo,
                        AuditStatus=s.AuditStatus,
                        Major=s.Major,
                        PeopleTitle=s.PeopleTitle,
                        SocialSecCercate=s.SocialSecCercate,
                        Birthday=s.Birthday,
                        ProfessionalYears=s.ProfessionalYears
                    })
                    .AsNoTracking()
                    .ToListAsync();
                result.ForEach(f=>
                {
                    f.UnitName=_context.EntRegisterInfoMag.Where(w => w.EntRegisterInfoMagId==f.EntGUID).Select(s => s.EntName).FirstOrDefault();

                });
                return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取机械人员：" + ex.Message, ex);
                return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 安拆单位--提交安拆人员信息进行审核
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="type"></param>
        /// <param name="userType"></param>
        /// <param name="userName"></param>
        /// <param name="AuditStatus"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<MachineryPeopleViewModel>>> SearchAnChaPeoplePro(int pageIndex, int pageSize, int type, int? userType, string userName, int? AuditStatus)
        {
            try
            {
                var entGuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;

                var data = _context.MachineryPeoples
                    .Where(w => w.DeleteMark == 0 && w.Type == type && w.EntGUID==entGuid);
                if (userType != null)
                {
                    data = data.Where(w => w.PersonType == userType);
                }
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    data = data.Where(w => w.PersonName.Contains(userName));
                }
                if (AuditStatus != null)
                {
                    data = data.Where(w => w.AuditStatus == AuditStatus);
                }
                var count = await data.CountAsync();
                if (count <= 0)
                {
                    return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, new List<MachineryPeopleViewModel>(), count);
                }
                if (pageIndex == 0)
                {
                    pageIndex = 1;
                }
                if (pageSize == 0)
                {
                    pageSize = 10;
                }
                var result = await data.OrderByDescending(o => o.UpdateDate)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new MachineryPeopleViewModel
                    {
                        PersonTypeLabel = _context.PersonTypes
                            .Where(w => w.Code == s.PersonType)
                            .Select(s2 => s2.Name)
                            .FirstOrDefault(),
                        BelongedTo = s.BelongedTo,
                        CerAgency = s.CerAgency,
                        CerName = s.CerName,
                        CerUrl = s.CerUrl,
                        CerValidBeginDate = s.CerValidBeginDate,
                        CerValidEndDate = s.CerValidEndDate,
                        CreateDate = s.CreateDate,
                        DeleteMark = s.DeleteMark,
                        EntGUID = s.EntGUID,
                        Id = s.Id,
                        IdCard = s.IdCard,
                        IdCardUrl = s.IdCardUrl,
                        MachineryPersonId = s.MachineryPersonId,
                        PersonName = s.PersonName,
                        PersonType = s.PersonType,
                        RecordNumber = s.RecordNumber,
                        Sex = s.Sex,
                        Tel = s.Tel,
                        Type = s.Type,
                        UpdateDate = s.UpdateDate,
                        WorkTypeCode = s.WorkTypeCode,
                        SpecialWorkerTypeNo = s.SpecialWorkerTypeNo,
                        AuditStatus=s.AuditStatus,
                        Major=s.Major,
                        PeopleTitle=s.PeopleTitle,
                        SocialSecCercate=s.SocialSecCercate,
                        Birthday=s.Birthday,
                        ProfessionalYears=s.ProfessionalYears
                    })
                    .AsNoTracking()
                    .ToListAsync();
                result.ForEach(f =>
                {
                    f.UnitName=_context.EntRegisterInfoMag.Where(w => w.EntRegisterInfoMagId==f.EntGUID).Select(s => s.EntName).FirstOrDefault();

                });
                return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取机械人员：" + ex.Message, ex);
                return ResponseViewModel<List<MachineryPeopleViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 安拆人员-提交时编辑
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<object>> UpdateAnChaPeoples([FromBody]MachineryPeople model)
        {
            try
            {
                var info = _context.MachineryPeoples.Where(w => w.MachineryPersonId==model.MachineryPersonId && w.DeleteMark==0).FirstOrDefault();
                info.PersonName=model.PersonName;
                info.PersonType=model.PersonType;
                info.Major=model.Major;
                info.AuditStatus=model.AuditStatus;
                info.Birthday=model.Birthday;
                info.CerAgency=model.CerAgency;
                info.PeopleTitle=model.PeopleTitle;
                info.WorkTypeCode=model.WorkTypeCode;
                info.SpecialWorkerTypeNo=model.SpecialWorkerTypeNo;
                info.SocialSecCercate=model.SocialSecCercate;
                info.Sex=model.Sex;
                info.CerName=model.CerName;
                info.CerUrl=model.CerUrl;
                info.CerValidBeginDate=model.CerValidBeginDate;
                info.CerValidEndDate=model.CerValidEndDate;
                info.IdCard=model.IdCard;
                info.IdCardUrl=model.IdCardUrl;
                info.ProfessionalYears=model.ProfessionalYears;
                info.Tel=model.Tel;
                _context.MachineryPeoples.Update(info);
                _context.SaveChanges();
                return ResponseViewModel<object>.Create(Status.SUCCESS,Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("修改信息：" + ex.Message, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 安拆人员-删除
        /// </summary>
        /// <param name="MachineryPersonIds"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> DelAnChaPeoples([FromBody] List<string> MachineryPersonIds)
        {
            try
            {
                foreach (var item in MachineryPersonIds)
                {
                    var info = _context.MachineryPeoples.Where(w => w.MachineryPersonId==item && w.DeleteMark==0).FirstOrDefault();
                    if (info == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "基本信息不存在或已被删除");
                    }
                    info.DeleteMark=1;
                    _context.MachineryPeoples.Update(info);
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
        /// 安拆人员提交---至待审核
        /// </summary>
        /// <param name="MachineryPersonIds"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> UptAnChaPeoples([FromBody] List<string> MachineryPersonIds)
        {
            try
            {
                foreach (var item in MachineryPersonIds)
                {
                    var info = _context.MachineryPeoples.Where(w => w.MachineryPersonId==item && w.DeleteMark==0).FirstOrDefault();
                    if (info == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "基本信息不存在或已被删除");
                    }
                    info.AuditStatus=1;
                    _context.MachineryPeoples.Update(info);
                }
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "提交成功!");
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，提交成功：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "系统异常，提交成功");
            }
        }

        /// <summary>
        /// 安拆人员撤回---至待提交
        /// </summary>
        /// <param name="MachineryPersonIds"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> WdwAnChaPeoples([FromBody] MachineryPeople AnChaPeoples)
        {
            try
            {
               
                    var info = _context.MachineryPeoples.Where(w => w.MachineryPersonId==AnChaPeoples.MachineryPersonId && w.DeleteMark==0).FirstOrDefault();
                    if (info == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "基本信息不存在或已被删除");
                    }
                    if (info.AuditStatus == 2)
                    {
                    return ResponseViewModel<string>.Create(Status.FAIL, "该人员已经审核，无法撤回，请刷新页面查看状态！");
                    }
                info.AuditStatus=0;
                    _context.MachineryPeoples.Update(info);
                
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "提交成功!");
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，提交成功：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "系统异常，提交成功");
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="pageIndex"></param>
        ///// <param name="pageSize"></param>
        ///// <param name="userName">姓名</param>
        ///// <param name="SpecialWorkerTypeNo">证书编号</param>
        ///// <param name="idCard">身份证</param>
        ///// <returns></returns>

        //[HttpGet]
        //public async Task<ResponseViewModel<dynamic>> SearchMachineryPeoplePro(int pageIndex, int pageSize
        //    , string userName, string SpecialWorkerTypeNo, string idCard)
        //{
        //    try
        //    {
        //        var data = (from a in _context.MachineryPeoples.WhereIf(!string.IsNullOrEmpty(userName), w => w.PersonName.Contains(userName))
        //                   .WhereIf(!string.IsNullOrEmpty(SpecialWorkerTypeNo), w => w.PersonName.Contains(SpecialWorkerTypeNo))
        //                    .WhereIf(!string.IsNullOrEmpty(idCard), w => w.IdCard.Contains(SpecialWorkerTypeNo))
        //                    join b in _context.InstallPeoples
        //                    on a.MachineryPersonId equals b.MachineryPersonId
        //                    where b.IsFree == 0 && b.DeleteMark == 0 && b.RecordNumber != null
        //                    select new
        //                    {
        //                        a.Id,
        //                        a.MachineryPersonId,
        //                        a.CerName,
        //                        a.CerValidBeginDate,
        //                        a.CerValidEndDate,
        //                        a.CreateDate
        //                    ,
        //                        a.DeleteMark,
        //                        a.CerUrl,
        //                        a.CerAgency,
        //                        a.BelongedTo,
        //                        a.RecordNumber,
        //                        a.Sex,
        //                        a.Tel,
        //                        a.Type,
        //                        a.WorkTypeCode,
        //                        a.SpecialWorkerTypeNo,
        //                        a.EntGUID,
        //                        a.IdCard,
        //                        a.IdCardUrl,
        //                        a.PersonName,
        //                        a.PersonType,
        //                        b.MachineryInfoId,
        //                        b.UpdateDate,
        //                        b.IsFree
        //                    });

        //        var count = await data.CountAsync();
        //        if (count == 0)
        //        {
        //            return ResponseViewModel<dynamic>.Create(Status.SUCCESS, Message.SUCCESS, new List<object>(), count);
        //        }
        //        if (pageIndex == 0)
        //        {
        //            pageIndex = 1;
        //        }
        //        if (pageSize == 0)
        //        {
        //            pageSize = 10;
        //        }
        //        var queryData = await data.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        //        var result = queryData.Select(s => new SearchMachineryPeopleViewModel
        //        {
        //            MachinePersonId = s.MachineryPersonId,
        //            BelongedTo = s.BelongedTo,
        //            CerAgency = s.CerAgency,
        //            CerName = s.CerName,
        //            CerUrl = s.CerUrl,
        //            CerValidBeginDate = s.CerValidBeginDate,
        //            CerValidEndDate = s.CerValidEndDate,
        //            CreateDate = s.CreateDate,
        //            DeleteMark = s.DeleteMark,
        //            EntGUID = s.EntGUID,
        //            Id = s.Id,
        //            IdCard = s.IdCard,
        //            IdCardUrl = s.IdCardUrl,
        //            MachineryPersonId = s.MachineryPersonId,
        //            PersonName = s.PersonName,
        //            PersonType = s.PersonType,
        //            RecordNumber = s.RecordNumber,
        //            Sex = s.Sex,
        //            Tel = s.Tel,
        //            Type = s.Type,
        //            UpdateDate = s.UpdateDate,
        //            WorkTypeCode = s.WorkTypeCode,
        //            WorkTypeName = WorkerDic[(int)s.WorkTypeCode].Value,
        //            SpecialWorkerTypeNo = s.SpecialWorkerTypeNo,
        //            ProjectName =_context.ProjectOverview.Where(p => p.BelongedTo==s.BelongedTo&&p.RecordNumber==s.RecordNumber).OrderByDescending(r => r.Id).Select(q => q.ProjectName).FirstOrDefault(),
        //            MachineName = _context.MachineryInfos.Where(q => q.MachineryInfoId == s.MachineryInfoId).Select(q => q.PropertyRightsRecordNo).FirstOrDefault()

        //        }).ToList();
        //        return ResponseViewModel<dynamic>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("特种作业人员：" + ex.Message, ex);
        //        return ResponseViewModel<dynamic>.Create(Status.ERROR, Message.ERROR);
        //    }
        //}

        #region zidian

        /// 大型机械专用工种
        /// </summary>
        public static Dictionary<int, SpecialWorker> WorkerDic = new Dictionary<int, SpecialWorker>()
        {
            { 1,new SpecialWorker{ IsSpecial=true,Value="建筑电工" } },
            { 2,new SpecialWorker{IsSpecial=true,Value="建筑架子工" } },
            { 3,new SpecialWorker{IsSpecial=true,Value= "建筑起重信号司索工" } },
            { 4, new SpecialWorker{IsSpecial=true,Value="建筑起重机械司机（塔式起重机）" } },
            { 5,new SpecialWorker{IsSpecial=true,Value="建筑起重机械司机（施工升降机）" } },
            { 6,new SpecialWorker{IsSpecial=true,Value="建筑起重机械司机（物料提升机）" } },
            { 7,new SpecialWorker{IsSpecial=true,Value="建筑起重机械安装拆卸工（塔式起重机）" } },
            { 8,new SpecialWorker{IsSpecial=true,Value="建筑起重机械安装拆卸工（施工升降机物料提升机）" } },
            { 9,new SpecialWorker{IsSpecial=true,Value ="高处作业吊篮安装拆卸工" } },
            { 10,new SpecialWorker{IsSpecial=true,Value="建筑焊工" } },
            { 11,new SpecialWorker{IsSpecial=true,Value="建筑起重机械安装质量检验工（塔式起重机）" } },
            {12,new SpecialWorker{IsSpecial=true,Value="建筑起重机械安装质量检验工（施工升降机）"} },
            { 13,new SpecialWorker{IsSpecial=true,Value="桩机操作工" } },
            { 14, new SpecialWorker{IsSpecial=true,Value="建筑混凝土泵操作工" } },
            { 15, new SpecialWorker{IsSpecial=true,Value="建筑施工现场场内叉车司机" } },
            { 16,new SpecialWorker{IsSpecial=true,Value="建筑施工现场场内装载车司机" } },
            { 17,new SpecialWorker{IsSpecial=true,Value= "建筑施工现场场内翻斗车司机" } },
            { 18,new SpecialWorker{IsSpecial=true,Value="建筑施工现场场内推土机司机" } },
            { 19,new SpecialWorker{IsSpecial=true,Value="建筑施工现场场内挖掘机司机" } },
            { 20,new SpecialWorker{IsSpecial=true,Value="建筑施工现场场内压路机司机" } },
            { 21,new SpecialWorker{IsSpecial=true,Value= "建筑施工现场场内平地机司机" } },
            { 22,new SpecialWorker{IsSpecial=true,Value= "建筑施工现场场内沥青混凝土摊铺机司机" } },
            { 23,new SpecialWorker{IsSpecial=true,Value= "附着式升降脚手架子工" } },
            { 24,new SpecialWorker{IsSpecial=true,Value= "桥（门）式起重机司机" } },
            { 25,new SpecialWorker{IsSpecial=true,Value= "架桥机司机" } },
            { 26,new SpecialWorker{IsSpecial=true,Value= "隧道运土轨道电瓶车司机" } },
            { 27,new SpecialWorker{IsSpecial=true,Value= "盾构机司机" } }
        };
        #endregion


        /// <summary>
        /// 检测所--安拆人员信息入库--审核
        /// </summary>
        /// <param name="MachineryPersonIds"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> SubAnChaPeoples([FromBody] List<string> MachineryPersonIds)
        {
            try
            {
                foreach (var item in MachineryPersonIds)
                {
                    var info = _context.MachineryPeoples.Where(w => w.MachineryPersonId==item && w.DeleteMark==0).FirstOrDefault();
                    if (info == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "基本信息不存在或已被删除");
                    }
                    var info1 = _context.MachineryPeoples.Where(w => w.MachineryPersonId==item && w.DeleteMark==0 && w.AuditStatus== 0).FirstOrDefault();
                    if (true)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "该条提交记录已被撤回!");
                    }
                    info.AuditStatus=2;
                    _context.MachineryPeoples.Update(info);
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
        /// 检测所--安拆人员信息入库--审核
        /// </summary>
        /// <param name="MachineryPersonIds"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<string>> SubAnChaPeople(string MachineryPersonId,int type)
        {
            try
            {
                
                    var info = _context.MachineryPeoples.Where(w => w.MachineryPersonId==MachineryPersonId && w.DeleteMark==0).FirstOrDefault();
                    if (info == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "基本信息不存在或已被删除");
                    }
                var info1 = _context.MachineryPeoples.Where(w => w.MachineryPersonId==MachineryPersonId && w.DeleteMark==0 && w.AuditStatus== 0).FirstOrDefault();
                if (info1!=null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "该条提交记录已被撤回!");
                }
                if (type==0)
                {
                    info.AuditStatus=2;

                }
                else
                {
                    info.AuditStatus=3;
                }
                _context.MachineryPeoples.Update(info);
                
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "修改成功!");
            }
            catch (Exception ex)
            {
                _logger.LogError("系统异常，修改失败：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "系统异常，修改失败");
            }
        }
    }
}






