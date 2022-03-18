using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ViewModels;

namespace JSAJ.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]

    public class MenusController : ControllerBase
    {
        private readonly ILogger<MenusController> _logger;
        private readonly JssanjianmanagerContext _context;
        public MenusController(ILogger<MenusController> logger, JssanjianmanagerContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<ResponseViewModel<List<MenusTreeViewModel>>> GetMenusTree()
        {
            try
            {
                // 获取所有的菜单权限
                var data = await _context.MenuPermissionsV2s
                    .Where(w => w.DeleteMark == 0)
                    .OrderBy(o => o.Sort)
                   .OrderBy(o => o.Sort)
                   .AsNoTracking()
                   .ToListAsync();

                // 获取所有根元素，然后递归
                var roots = data.Where(w => string.IsNullOrWhiteSpace(w.ParentMenuId))
                    .OrderBy(o => o.Sort)
                    .Select(s => new MenusTreeViewModel
                    {
                        Children = new List<MenusTreeViewModel>(),
                        Component = s.Component,
                        Key = s.MenuId,
                        MenuId = s.MenuId,
                        ParentMenuId = s.ParentMenuId,
                        Name = s.Name,
                        Path = s.Path,
                        Redirect = s.Redirect,
                        Sort = s.Sort,
                        Title = s.Title,
                        Hidden = s.Hidden,
                        Meta = new Meta
                        {
                            Title = s.Title,
                            Hidden = s.Hidden,
                            HideHeader = s.HideHeader,
                            Icon = s.Icon,
                            KeepAlive = s.KeepAlive,
                            Permission = new List<string>() { s.PermisionId }
                        },
                        ScopedSlots = new ScopedSlots
                        {
                            Type = s.Icon
                        }
                    }).ToList();


                for (int i = 0; i < roots.Count; i++)
                {
                    RecursiveMenus(data, roots[i]);
                }

                return ResponseViewModel<List<MenusTreeViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, roots);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取树级菜单：" + ex.Message + ex.StackTrace, ex);
                return ResponseViewModel<List<MenusTreeViewModel>>.Create(Status.ERROR, "程序异常:" + ex.Message + ex.StackTrace);
            }
        }


        private void RecursiveMenus(List<MenuPermissionsV2> datas, MenusTreeViewModel parent)
        {
            var child = datas.Where(w => w.ParentMenuId == parent.MenuId)
                .Select(s => new MenusTreeViewModel
                {
                    Children = new List<MenusTreeViewModel>(),
                    Component = s.Component,
                    Key = s.MenuId,
                    MenuId = s.MenuId,
                    ParentMenuId = s.ParentMenuId,
                    Name = s.Name,
                    Path = s.Path,
                    Redirect = s.Redirect,
                    Sort = s.Sort,
                    Title = s.Title,
                    Hidden = s.Hidden,
                    Meta = new Meta
                    {
                        Title = s.Title,
                        Hidden = s.Hidden,
                        HideHeader = s.HideHeader,
                        Icon = s.Icon,
                        KeepAlive = s.KeepAlive,
                        Permission = new List<string>() { s.PermisionId }
                    },
                    ScopedSlots = new ScopedSlots
                    {
                        Type = s.Icon
                    }
                }).ToList();
            if (child.Count > 0)
            {
                parent.Children.AddRange(child);
                for (int i = 0; i < child.Count; i++)
                {
                    RecursiveMenus(datas, child[i]);
                }
            }
        }

        [HttpPost]
        public async Task<ResponseViewModel<string>> AddOrUpdateMenus([FromBody] MenuViewModel model)
        {
            try
            {
                var now = DateTime.Now;
                if (string.IsNullOrWhiteSpace(model.MenuId))
                {
                    // permisiondId不允许重复
                    if (await _context.MenuPermissionsV2s
                        .AnyAsync(a => a.PermisionId == model.Meta.Permission[0] && a.DeleteMark == 0))
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "权限id不允许重复，请更换权限id再进行保存");
                    }
                    // 新增
                    var data = new MenuPermissionsV2
                    {
                        MenuId = SecurityManage.GuidUpper(),
                        Component = model.Component,
                        CreateDate = now,
                        DeleteMark = 0,
                        Hidden = model.Meta.Hidden,
                        HideHeader = model.Meta.HideHeader,
                        Icon = model.Meta.Icon,
                        KeepAlive = model.Meta.KeepAlive,
                        Name = model.Name,
                        ParentMenuId = model.ParentMenuId,
                        Path = model.Path,
                        PermisionId = model.Meta.Permission[0],
                        Redirect = model.Redirect,
                        Sort = model.Sort,
                        Title = model.Meta.Title,
                        UpdateDate = now
                    };
                    await _context.MenuPermissionsV2s.AddAsync(data);
                }
                else
                {
                    // permisiondId不允许重复
                    if (await _context.MenuPermissionsV2s
                        .AnyAsync(a => a.PermisionId == model.Meta.Permission[0] && a.DeleteMark == 0 && a.MenuId != model.MenuId))
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "权限id不允许重复，请更换权限id再进行保存");
                    }
                    // 更新
                    var data = await _context.MenuPermissionsV2s.Where(w => w.MenuId == model.MenuId)
                        .OrderByDescending(o => o.UpdateDate)
                        .FirstOrDefaultAsync();
                    if (data == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "菜单不存在，请稍后重试");
                    }
                    if (data.DeleteMark == 1)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "菜单已被删除，请稍后重试");
                    }

                    data.ParentMenuId = model.ParentMenuId;
                    data.Component = model.Component;

                    data.Hidden = model.Meta.Hidden;
                    data.HideHeader = model.Meta.HideHeader;
                    data.Icon = model.Meta.Icon;
                    data.KeepAlive = model.Meta.KeepAlive;
                    data.Name = model.Name;
                    data.ParentMenuId = model.ParentMenuId;
                    data.Path = model.Path;
                    data.PermisionId = model.Meta.Permission[0];
                    data.Redirect = model.Redirect;
                    data.Sort = model.Sort;
                    data.Title = model.Meta.Title;
                    data.UpdateDate = now;
                    _context.MenuPermissionsV2s.Update(data);
                }
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("添加/更新菜单", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 删除菜单
        /// </summary>
        /// <param name="menuIds"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<string>> RemoveMenus([FromBody] List<string> menuIds)
        {
            try
            {
                // 会删除菜单和子菜单
                var data = await _context.MenuPermissionsV2s
                    .Where(w => w.DeleteMark == 0 && (menuIds.Contains(w.MenuId) || menuIds.Contains(w.ParentMenuId)))
                    .ToListAsync();
                var now = DateTime.Now;
                for (int i = 0; i < data.Count; i++)
                {
                    data[i].DeleteMark = 1;
                    data[i].UpdateDate = now;
                }
                _context.MenuPermissionsV2s.UpdateRange(data);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("删除菜单", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, Message.ERROR);
            }
        }
    }
}