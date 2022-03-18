using Common;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
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
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly JssanjianmanagerContext _context;
        private readonly ILogger<NewsController> _logger;
        private readonly string _wordTemplte;
        private readonly string _buildWords;
        private readonly IWebHostEnvironment _environment;
        private OssFileSetting _ossFileSetting;
        public NewsController(IWebHostEnvironment environment, ILogger<NewsController> logger, JssanjianmanagerContext context, IOptions<OssFileSetting> oss)
        {
            _environment = environment;
            _context = context;
            _logger = logger;
            _buildWords = environment.WebRootPath + Path.DirectorySeparatorChar + "BuildPdf" + Path.DirectorySeparatorChar;
            _wordTemplte = environment.WebRootPath + Path.DirectorySeparatorChar + "doc" + Path.DirectorySeparatorChar;
            _ossFileSetting = oss.Value;
        }

        /// <summary>
        /// 资讯显示列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <param name="loginObjectId">发布对象</param>
        /// <param name="newType">0最新资讯 1厅发文件 2主管部门</param>
        /// <param name="belongedTo"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<NewsModel>>> GetRecentNews(int page, int limit, int loginObjectId,
            int newType = 0, string belongedTo = "")
        {
            try
            {
                var type = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//0项目登录 1企业登录
                var tokenId = User.FindFirst(nameof(ClaimTypeEnum.UserId))?.Value;//企业表Id   

                List<string> belongedToList = new List<string>();
                belongedToList.Add("AJ320000-1");
                if (!string.IsNullOrEmpty(belongedTo))
                {
                    belongedToList.Add(belongedTo);
                }

                if (!string.IsNullOrWhiteSpace(tokenId) && type == "1")
                {

                    var organizationCode = await _context.EntRegisterInfoMag
                .Where(w => w.EntRegisterInfoMagId == tokenId).Select(s => s.EntCode)
                .FirstOrDefaultAsync();

                    belongedToList.AddRange(_context.ProjectEntSnapshot
   .Where(w => w.OrganizationCode == organizationCode).GroupBy(g => g.BelongedTo)
   .Select(s => s.Key).ToList());
                }
                var sendingObject = "";
                if (loginObjectId == 0)
                {
                    sendingObject = "项目部";
                }
                else if (loginObjectId == 1)
                {
                    sendingObject = "企业";
                }
                else if (loginObjectId == 2)
                {
                    sendingObject = "安监站";
                }
                else if (loginObjectId == 3)
                {
                    sendingObject = "检测机构";
                }
                //var times = DateTime.Now;

                //var year = times.Year;
                //var month = times.Month;
                //var day = times.Day;

                DateTime time = DateTime.Today.AddMonths(-3);
                //if (month > 3)
                //{
                //    month = month - 3;
                //}
                //else
                //{
                //    month = month + 12 - 3;
                //    year = year - 1;
                //}

                //DateTime time = Convert.ToDateTime(year + "-" + month + "-" + day);



                var data = _context.News
                    .Where(w => w.DeleteMark == 0 && w.IsPublish == 1
                    && w.SendingObject.Contains(sendingObject) && (belongedToList.Contains(w.BelongedTo))
                    && w.PublishDate >= time);

                if (newType == 1)
                {
                    data = _context.News
                       .Where(w => w.DeleteMark == 0 && w.IsPublish == 1
                       && w.SendingObject.Contains(sendingObject)
                        && w.BelongedTo == "AJ320000-1");
                }
                else if (newType == 2)
                {
                    data = _context.News
                       .Where(w => w.DeleteMark == 0 && w.IsPublish == 1
                       && w.SendingObject.Contains(sendingObject)
                       && w.BelongedTo == belongedTo);
                }

                if (loginObjectId == 1 && newType == 0)
                {
                    // belongedToList.Contains(w.BelongedTo)
                    data = _context.News
                        .Where(w => w.DeleteMark == 0 && w.IsPublish == 1
                         && w.SendingObject.Contains(sendingObject)
                         && w.PublishDate >= time);

                }

                var count = await data.CountAsync();
                var result = await data.OrderByDescending(o => o.PublishDate)
                    .Skip((page - 1) * limit).Take(limit)
                    .Select(s => new NewsModel
                    {
                        Author = s.Author,
                        PublishDate = s.PublishDate,
                        Content = s.Content,
                        NewsId = s.NewsId,
                        TitleNews = s.Title,
                        SelectCount = s.SelectCount,
                        Label = s.Label,
                        SubContent = s.SubContent,
                        ContentLable = s.ContentLable,
                        FileUrl = s.ImgUrl,
                        NewsType = s.NewsType,
                        FileName = s.ImgName,
                        FileType = s.ImgUrl.Substring(s.ImgUrl.Length - 3, 3),
                        LabelList = new List<string>(),
                        SendingObject = s.SendingObject,
                        SendingObjectList = new List<string>(),
                        NewsClass = s.NewsClass,
                        Subtitle = s.Subtitle,
                        ContentLength = s.Content.Length > 300 ? 1 : 0

                    }).ToListAsync();

                result.ForEach(g =>
                {
                    if (!string.IsNullOrWhiteSpace(g.Label))
                    {
                        var lablist = g.Label.Split(',').ToList();
                        g.LabelList = lablist;
                    }
                    if (!string.IsNullOrWhiteSpace(g.SendingObject))
                    {
                        var seclist = g.SendingObject.Split(',').ToList();
                        g.SendingObjectList = seclist;
                    }

                });

                return ResponseViewModel<List<NewsModel>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取最新资讯列表：", ex);
                return ResponseViewModel<List<NewsModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }

    }
}
