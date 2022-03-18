using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Models;
using JSAJ.Core.Models.notice;
using JSAJ.Core.ViewModels;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViewModels;

namespace JSAJ.Core.Controllers.notice
{
    /// <summary>
    /// 通知
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class NoticeController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        private readonly ILogger<NoticeController> _logger;
        private readonly JssanjianmanagerContext _context;
        private JwtSettings settings;
        private OssFileSetting _ossFileSetting;
        private string _wordTemplte;
        public NoticeController(IWebHostEnvironment environment, ILogger<NoticeController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options, IOptions<OssFileSetting> oss)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            settings = options.Value;
            _ossFileSetting = oss.Value;
            _wordTemplte = environment.WebRootPath + Path.DirectorySeparatorChar + "doc" + Path.DirectorySeparatorChar;
        }


        /// <summary>
        /// 待办通知,消息通知列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<NoticeViewModel>>> GetNoticeList(int pageIndex, int pageSize, int noticeType = -1,
            int isHandle = -1, int isRead = -1, string searchTypeWord = null)
        {
            try
            {
                var type = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//0项目登录 1企业登录 2安监站登录
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;//当前人uuid  
                var roid = User.FindFirst(nameof(ClaimTypeEnum.RoleId))?.Value;//当前人uuid  //当前人登录的角色id
                var data = from a in _context.ToDoWorkReminder
                           join b in _context.NoticeUser
                           on a.ToDoWorkReminderId equals b.ToDoWorkReminderId
                           where b.NoticeUserId == uuid && a.DeleteMark == 0 && b.DeleteMark == 0
                           select new NoticeViewModel
                           {
                               Id = b.Id,
                               NoticeUserId = b.NoticeUserId,
                               RoleId = b.RoleId,
                               Content = a.Content,
                               ToDoWorkReminderId = a.ToDoWorkReminderId,
                               JumpUrl = a.JumpUrl == "" ? "" : a.JumpUrl,
                               Parameter = a.Parameter,
                               Port = a.Port,
                               NoticeType = a.NoticeType.ToString(),
                               NoticeTypeValue = a.NoticeType,
                               NoticeModel = a.NoticeModel,
                               IsHandle = a.IsHandle,
                               IsRead = b.IsRead,
                               CreateDate = b.CreateDate,
                               UpdateDate = b.UpdateDate
                           };
                if (type == "2")
                {
                    //安监站的通知需要筛选角色
                    data = data.Where(x => x.RoleId == roid);
                }
                if (noticeType != -1)
                {
                    data = data.Where(x => x.NoticeTypeValue == (EnumNoticeType)noticeType);
                }
                if (isHandle != -1)
                {
                    data = data.Where(x => x.IsHandle == isHandle);
                }
                if (isRead != -1)
                {
                    data = data.Where(x => x.IsRead == isRead);
                }
                if (!string.IsNullOrWhiteSpace(searchTypeWord))
                {
                    data = data.Where(x => x.Content.Contains(searchTypeWord));

                }
                if (type == "0")
                {
                    //项目端
                    data = data.Where(x => x.Port == EnumPort.项目端);
                }
                else if (type == "1") //企业端口
                {
                    data = data.Where(x => x.Port == EnumPort.企业端);
                }
                else if (type == "2") //安监站
                {
                    data = data.Where(x => x.Port == EnumPort.安监部门);
                }
                else if (type == "3") //检测单位
                {
                    data = data.Where(x => x.Port == EnumPort.检测所);
                }
                var resultData = await data.ToListAsync();
                var count = resultData.Count();
                var resultList = resultData.OrderByDescending(w => w.CreateDate).Skip((pageIndex - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                return ResponseViewModel<List<NoticeViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, resultList, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("消息列表：", ex);
                return ResponseViewModel<List<NoticeViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 删除消息
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<string>> DelNotice(List<int> ids)
        {
            try
            {
                var noticeUsers = await _context.NoticeUser.Where(w => ids.Contains(w.Id) && w.DeleteMark == 0).ToListAsync();
                noticeUsers.ForEach(x =>
                {
                    x.DeleteMark = 1;
                });
                _context.NoticeUser.UpdateRange(noticeUsers);
                await _context.SaveChangesAsync();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "删除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("删除消息：", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 修改消息为已读标记
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<string>> ReadNotice(List<int> ids)
        {
            try
            {

                var noticeUsers = await _context.NoticeUser.Where(w => ids.Contains(w.Id) && w.DeleteMark == 0).ToListAsync();
                noticeUsers.ForEach(x =>
                {
                    x.IsRead = 1;
                });
                _context.NoticeUser.UpdateRange(noticeUsers);
                await _context.SaveChangesAsync();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "标记已读成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("标记已读成功：", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 全部已读
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> ReadNoticeAll(int noticeType = -1)
        {
            try
            {
                var type = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//0项目登录 1企业登录
                var uuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;//当前人uuid  
                EnumPort port = EnumPort.项目端;
                if (type == "1") //企业端口
                {
                    port = EnumPort.企业端;
                }
                else if (type == "2") //安监站
                {
                    port = EnumPort.安监部门;
                }
                else if (type == "3") //检测单位
                {
                    port = EnumPort.检测所;
                }

                var data = from a in _context.ToDoWorkReminder
                           join b in _context.NoticeUser
                           on a.ToDoWorkReminderId equals b.ToDoWorkReminderId
                           where b.NoticeUserId == uuid && a.DeleteMark == 0 && b.DeleteMark == 0 && b.IsRead == 0
                           select new NoticeViewModel
                           {
                               Id = b.Id,
                               Content = a.Content,
                               ToDoWorkReminderId = a.ToDoWorkReminderId,
                               JumpUrl = a.JumpUrl == "" ? "" : a.JumpUrl,
                               Parameter = a.Parameter,
                               Port = a.Port,
                               NoticeType = a.NoticeType.ToString(),
                               NoticeTypeValue = a.NoticeType,
                               NoticeModel = a.NoticeModel,
                               IsHandle = a.IsHandle,
                               IsRead = b.IsRead,
                               CreateDate = b.CreateDate,
                               UpdateDate = b.UpdateDate
                           };
                if (noticeType != -1)
                {
                    data = data.Where(x => x.NoticeTypeValue == (EnumNoticeType)noticeType);
                }

                var result = data.ToList();
                var noticeUsers = await _context.NoticeUser.Where(w =>
                result.Select(y => y.Id).Contains(w.Id)).ToListAsync();
                noticeUsers.ForEach(x =>
                {
                    x.IsRead = 1;
                });
                _context.NoticeUser.UpdateRange(noticeUsers);
                await _context.SaveChangesAsync();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "全部已读标记成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("标记已读成功：", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 添加通知
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<string>> AddNotice(AddNoticeViewModel model)
        {
            try
            {

                EnumNoticeModel noticeModel;
                EnumPort port;
                DateTime now = DateTime.Now;
                if (model == null || string.IsNullOrWhiteSpace(model.NoticeConfigId)
                    || EnumNoticeModel.TryParse(model.NoticeModel.ToString(), out noticeModel)
                     || EnumPort.TryParse(model.Port.ToString(), out port)
                     || string.IsNullOrWhiteSpace(model.Content)
                     || model.NoticeUserIds == null || model.NoticeUserIds.Count > 0)

                {
                    return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
                }

                var jueseList = await _context.UserRoles.Where(x => model.NoticeUserIds.Contains(x.SysUserManagerUuid) && x.DeleteMark == 0).ToListAsync();
                ToDoWorkReminder xiaoxi = new ToDoWorkReminder();
                xiaoxi.NoticeConfigId = model.NoticeConfigId;
                xiaoxi.ToDoWorkReminderId = SecurityManage.GuidUpper();
                xiaoxi.NoticeType = model.NoticeType;
                xiaoxi.Port = port;
                xiaoxi.NoticeModel = noticeModel;
                xiaoxi.Content = model.Content;
                xiaoxi.IsHandle = 0;
                xiaoxi.CreateDate = now;
                xiaoxi.UpdateDate = now;
                xiaoxi.DeleteMark = 0;
                _context.ToDoWorkReminder.Add(xiaoxi);

                List<NoticeUser> userList = new List<NoticeUser>();
                foreach (string item in model.NoticeUserIds)
                {
                    var itemRoids = jueseList.Where(x => x.SysUserManagerUuid == item).ToList();
                    itemRoids.ForEach(x =>
                    {
                        NoticeUser user = new NoticeUser();
                        user.NoticeUserId = x.SysUserManagerUuid;
                        user.RoleId = x.RoleId;
                        user.ToDoWorkReminderId = xiaoxi.ToDoWorkReminderId;
                        user.IsRead = 0;
                        user.CreateDate = now;
                        user.UpdateDate = now;
                        user.DeleteMark = 0;
                        userList.Add(user);
                    }
                    );

                }
                _context.NoticeUser.AddRange(userList);

                await _context.SaveChangesAsync();

                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "操作成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("添加通知：", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


    }
}