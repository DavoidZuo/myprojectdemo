using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViewModels;

namespace JSAJNanJing.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MajorHazardController : ControllerBase
    {
        private readonly JssanjianmanagerContext _context;
        private readonly ILogger<MajorHazardController> _logger;
        public MajorHazardController(ILogger<MajorHazardController> logger, JssanjianmanagerContext context)
        {
            _logger = logger;
            _context = context;
        }


        /// <summary>
        /// 较大危大工程数据源-魏锦源2019.10.31
        /// </summary>
        /// <param name="BelongedTo"></param>
        /// <param name="RecordNumber"></param>
        /// <param name="Grade"></param>
        /// <returns></returns>
        public async Task<ResponseViewModel<List<MajorHazardViewModel>>> GetMajorHazard(string belongedTo, string recordNumber)
        {

            try
            {
                List<MajorHazardViewModel> mhvlist = new List<MajorHazardViewModel>();
                //把数据拿到内存里处理
                var contentModel = await _context.MajorHazardContent.Where(w => w.Grade == "危险性较大的分部分项工程清单" && w.DangerSouPartOneId != null && w.DangerSourceContentId != null).ToListAsync();
                var mhuModel = await _context.MajorHazardUpdate.Where(w => w.BelongedTo == belongedTo && w.RecordNumber == recordNumber && w.Grade == "危险性较大的分部分项工程清单").ToListAsync();

                var groupList = (from mu in mhuModel
                                 join mc in contentModel
                                 on new { mu.DangerSouPartOneId, mu.DangerSourceContentId }
                                 equals new { DangerSouPartOneId = mc.DangerSouPartOneId.ToString(), DangerSourceContentId = mc.DangerSourceContentId.ToString() }
                                 into list
                                 from nlist in list.DefaultIfEmpty()
                                 select new
                                 {
                                     MajorHazardId = mu.Id,
                                     Jtxx = mu.Jtxx,//具体信息
                                     BelongedTo = nlist?.BelongedTo,
                                     RecordNumber = mu.RecordNumber,
                                     Grade = nlist?.Grade,
                                     DangerSouPartOneId = nlist?.DangerSouPartOneId,
                                     DangerSouPartOneName = nlist?.DangerSouPartOneName,
                                     Remark = nlist?.Remark,
                                     DangerSourceUpdateDate = mu.DangerSourceUpdateDate,//包含开始日期和结束日期
                                     DangerSourceUpdateStatus = mu.DangerSourceUpdateStatus,
                                     Id = nlist?.Id,
                                     DangerSourceContentId = nlist?.DangerSourceContentId,
                                     DangerSourceContent = nlist?.DangerSourceContent//内容

                                 }).ToList();


                groupList.ForEach(g =>
                {

                    MajorHazardViewModel model = new MajorHazardViewModel();

                    //获取当前日期与开始日期和结束日期
                    var Time = g.DangerSourceUpdateDate.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                    if (Time.Length == 2)
                    {
                        var nTime = DateTime.Now.ToString("yyyyMMdd");
                        var sTime = Time[0].Replace("-", "");
                        var eTime = Time[1].Replace("-", "");
                        model.Id = (int)g.MajorHazardId;
                        model.Grade = g.Grade;
                        model.Content = g.DangerSourceContent;
                        model.DangerSouName = g.DangerSouPartOneName;
                        //拿三个时间进行判断获得状态
                        model.DangerSourceUpdateStatus = nTime.CompareTo(sTime) == -1 ? "未开工" : nTime.CompareTo(eTime) == 1 ? "已解除" : "施工中";
                        model.beginEndTime = g.DangerSourceUpdateDate.Replace("&", "～");
                        model.InfoMation = g.Jtxx;
                        model.BeginTime = Time[0];
                        model.EndTime = Time[1];
                        model.danNote = g.Remark;
                        model.DangerSouPartOneId = g.DangerSouPartOneId;
                        model.DangerSourceContentId = g.DangerSourceContentId;
                        mhvlist.Add(model);
                    }

                });
                return ResponseViewModel<List<MajorHazardViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, mhvlist, mhvlist.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError("获取较大危大工程数据源：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<MajorHazardViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }
    }
}
