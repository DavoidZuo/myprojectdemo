using Common;
using JSAJ.Core.Controllers;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ViewModels;

namespace JSAJNanJing.Controller
{
    [Route("api/[controller]/[action]")]
    [Authorize]
    [ApiController]
    public class SuperScaleController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MenusController> _logger;
        private readonly JssanjianmanagerContext _context;
        private JwtSettings settings;
        private OssFileSetting _ossFileSetting;
        private readonly string templateWords;
        private readonly string _buildWords;

        public SuperScaleController(IWebHostEnvironment environment, ILogger<MenusController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options, IOptions<OssFileSetting> oss)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            settings = options.Value;
            _ossFileSetting = oss.Value;
            templateWords = environment.WebRootPath + Path.DirectorySeparatorChar + "doc" + Path.DirectorySeparatorChar;
            _buildWords = environment.WebRootPath + Path.DirectorySeparatorChar + "BuildPdf" + Path.DirectorySeparatorChar;
        }


        /// <summary>
        /// css-超规模危大工程附件上传
        /// </summary>
        /// <param name="iform">超规模危大工程附件上传</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<SuperScaleDangerTotal>> GetSuperScaleDangerTotal(string belongedTo, string recordNumber)
        {
            try
            {
                SuperScaleDangerTotal model = new SuperScaleDangerTotal();
                var superScaleDanger = await _context.SuperScaleDanger
                     .Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).ToListAsync();

                //把数据拿到内存里处理
                var contentModel = await _context.MajorHazardContent.Where(w => w.Grade == "危险性较大的分部分项工程清单").ToListAsync();
                var mhuModel = await _context.MajorHazardUpdate.Where(w => w.BelongedTo == belongedTo && w.RecordNumber == recordNumber && w.Grade == "危险性较大的分部分项工程清单").ToListAsync();

                var groupList = (from mu in mhuModel
                                 join mc in contentModel
                                 on new { mu.DangerSouPartOneId, mu.DangerSourceContentId } equals new { DangerSouPartOneId = mc.DangerSouPartOneId.ToString(), DangerSourceContentId = mc.DangerSourceContentId.ToString() }
                                 into list
                                 from nlist in list.DefaultIfEmpty()
                                 where mu.BelongedTo == belongedTo && mu.RecordNumber == recordNumber
                                 select mu).ToList();
                model.SuperScaleDangerCount = superScaleDanger.Count;
                model.DangerCount = groupList.Count;
                return ResponseViewModel<SuperScaleDangerTotal>.Create(Status.SUCCESS, Message.SUCCESS, model);
            }
            catch (Exception ex)
            {
                _logger.LogError("超规模危大工程附件上传：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<SuperScaleDangerTotal>.Create(Status.ERROR, Message.ERROR);
            }
        }



        /// <summary>
        /// css-危大工程列表
        /// </summary>
        /// <param name="belongedTo">0省第一级 1第二级  3第三级</param>
        /// <param name="searchType"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<object>> GetSuperScaleDangerList(string belongedTo, string recordNumber)
        {
            try
            {
                List<SuperScaleDangerViewModel> superScaleList = new List<SuperScaleDangerViewModel>();
                //说明企业下有这个项目则可以查看这个项目的危大工程
                var superScaleDangerList = await _context.SuperScaleDanger.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).ToListAsync();
                var superScaleDangerFile = await _context.SuperScaleDangerFile.Where(x => x.BelongedTo == belongedTo && x.RecordNumber == recordNumber).ToListAsync();

                superScaleDangerList.ForEach(x =>
                {
                    List<FileViewModel> files = new List<FileViewModel>();
                    FileViewModel files1 = new FileViewModel();
                    FileViewModel files2 = new FileViewModel();
                    FileViewModel files3 = new FileViewModel();

                    FileViewModel files4 = new FileViewModel();
                    FileViewModel files5 = new FileViewModel();

                    FileViewModel files6 = new FileViewModel();
                    FileViewModel files7 = new FileViewModel();
                    SuperScaleDangerViewModel model = new SuperScaleDangerViewModel();
                    model.BelongedTo = x.BelongedTo;
                    model.RecordNumber = x.RecordNumber;
                    model.MasterClass = x.MasterClass;
                    model.ZiLiaoMingCheng = x.ZiLiaoMingCheng;
                    model.BeginTime = x.BeginTime == null ? "" : Convert.ToDateTime(x.BeginTime).ToString("yyyy-MM-dd");
                    model.Endtime = x.Endtime == null ? "" : Convert.ToDateTime(x.Endtime).ToString("yyyy-MM-dd");
                    model.DateInfo = model.BeginTime + "~" + model.Endtime;
                    model.Remark = x.Remark;
                    model.Status = x.Status;
                    model.InfoMation = x.InfoMation;
                    model.Uuid = x.Uuid;
                    List<SuperScaleDangerFile> file1 = superScaleDangerFile.Where(k => k.FileName == "技术负责人授权委托书" && k.ModuleName == x.MasterClass && k.Uuid == "").ToList();
                    List<SuperScaleDangerFile> file2 = superScaleDangerFile.Where(k => k.FileName == "专家论证会签到表" && k.ModuleName == x.MasterClass && k.Uuid == "").ToList();
                    List<SuperScaleDangerFile> file3 = superScaleDangerFile.Where(k => k.FileName == "专家论证报告" && k.ModuleName == x.MasterClass && k.Uuid == "").ToList();
                    List<SuperScaleDangerFile> file4 = superScaleDangerFile.Where(k => k.FileName == "交底记录表" && k.ModuleName == x.MasterClass && k.Uuid == "").ToList();
                    List<SuperScaleDangerFile> file5 = superScaleDangerFile.Where(k => k.FileName == "技术负责人授权委托书" && k.ModuleName == x.MasterClass && k.Uuid == x.Uuid).ToList();
                    List<SuperScaleDangerFile> file6 = superScaleDangerFile.Where(k => k.FileName == "验收告知单" && k.ModuleName == x.MasterClass && k.Uuid == x.Uuid).ToList();
                    List<SuperScaleDangerFile> file7 = superScaleDangerFile.Where(k => k.FileName == "验收表" && k.ModuleName == x.MasterClass && k.Uuid == x.Uuid).ToList();
                    files1.fileName = "技术负责人授权委托书(附件4-1)";
                    files1.file = file1;
                    files1.fileCount = file1.Count;

                    files2.fileName = "专家论证会签到表";
                    files2.fileCount = file2.Count;
                    files2.file = file2;

                    files3.fileName = "专家论证报告";
                    files3.fileCount = file3.Count;
                    files3.file = file3;

                    files4.fileName = "交底记录表";
                    files4.fileCount = file4.Count;
                    files4.file = file4;

                    files5.fileName = "技术负责人授权委托书(附件-)";
                    files5.fileCount = file5.Count;
                    files5.file = file5;

                    files6.fileName = "验收告知单";
                    files6.fileCount = file6.Count;
                    files6.file = file6;

                    files7.fileName = "验收表";
                    files7.fileCount = file7.Count;
                    files7.file = file7;
                    files.Add(files1);
                    files.Add(files2);
                    files.Add(files3);
                    files.Add(files4);
                    files.Add(files5);
                    files.Add(files6);
                    files.Add(files7);
                    model.Files = files;
                    superScaleList.Add(model);
                });

                return ResponseViewModel<object>.Create(Status.SUCCESS, Message.SUCCESS, superScaleList, superScaleList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError("危大工程清单SuperScaleController/GetSuperScaleDangerList：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<object>.Create(Status.ERROR, Message.ERROR);
            }
        }
    }
}
