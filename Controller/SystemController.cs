using Common;
using JSAJ.Core.Common.QRCodes;
using JSAJ.Core.Controllers;
using JSAJ.Core.Models;
using JSAJ.Core.ViewModels;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViewModels;

namespace JSAJNanJing.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly ILogger<MenusController> _logger;
        private readonly JssanjianmanagerContext _context;
        private readonly ForbidChangeRole _changeRole;

        private IQRCode _iqrcode;
        public SystemController(ILogger<MenusController> logger, JssanjianmanagerContext context, IOptions<ForbidChangeRole> options, IQRCode qRCode)
        {
            _logger = logger;
            _context = context;
            _changeRole = options.Value;
            _iqrcode = qRCode;
        }


        [HttpPost]
        public async Task<ResponseViewModel<string>> AddOrUpdateRoles([FromBody] Role role)
        {
            try
            {
                var now = DateTime.Now;
                var userId = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                if (string.IsNullOrWhiteSpace(role.RoleId))
                {
                    // 角色名称不允许重复
                    var data = await _context.Roles
                        .Where(w => w.Deleted == 0 && w.Name == role.Name)
                        .OrderByDescending(o => o.CreateTime)
                        .FirstOrDefaultAsync();
                    if (data != null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "角色名称不允许重复，请修改后重新添加");
                    }
                    // 新增
                    role.RoleId = SecurityManage.GuidUpper();
                    role.Deleted = 0;
                    role.CreatorId = userId;
                    role.CreateTime = now;
                    role.Status = 1;
                    role.IsMainRole = 1;
                    role.BelongedTo = belongedTo;

                    await _context.Roles.AddAsync(role);
                }
                else
                {
                    // 更新
                    var data = await _context.Roles.Where(w => w.RoleId == role.RoleId && w.Deleted == 0)
                        .OrderByDescending(o => o.CreateTime)
                        .FirstOrDefaultAsync();

                    if (data == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "角色不存在或已被删除，无法修改");
                    }

                    if (_changeRole.ForbidChange.Contains(data.Name))
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "固有角色不允许删除和修改");
                    }
                    // 角色名称不允许重复
                    var anotherRole = await _context.Roles
                        .Where(w => w.Deleted == 0 && w.Name == role.Name && w.RoleId != role.RoleId && w.IsMainRole == 1)
                        .OrderByDescending(o => o.CreateTime)
                        .FirstOrDefaultAsync();
                    if (anotherRole != null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "角色名称不允许重复，请修改后重新提交");
                    }
                    data.SupervisionDepartmentId = role.SupervisionDepartmentId;
                    data.Describe = role.Describe;
                    data.Name = role.Name;
                    data.IsOwner = role.IsOwner;
                    data.BelongedTo = belongedTo;
                    _context.Roles.Update(data);
                }

                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("添加或更新角色：", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 配置安监站角色权限,不可配置管理员权限
        /// 2019-11-29
        /// machuanlong
        /// </summary>
        /// <param name="permissions"></param>
        /// <returns></returns>
        [HttpPost]
        //[Authorize]
        public async Task<ResponseViewModel<string>> AddAJPermission([FromBody] List<PermissionOfRoleViewModel> viewModels)
        {
            if (viewModels.Count == 0)
            {
                return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "更新配置数据不可为空");
            }
            try
            {
                var ajRole = await _context.Roles
                    .Where(w => w.Status == 1 && w.Deleted == 0 && w.IsMainRole == 1 && w.Name == "安监站")
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                if (ajRole == null)
                {
                    return ResponseViewModel<string>.Create(Status.WARN, "管理员角色已失效，非法操作！");
                }
                string roleId = viewModels[0].RoleId;
                var permissions = await _context.Permissions
                .Where(w => w.RoleId == roleId && w.DeleteMark == 0)
                .ToListAsync();
                permissions.ForEach(f => f.DeleteMark = 1);

                var acPermissions = await _context.ActionWithPermissions
                    .Where(w => w.RoleId == roleId && w.DeleteMark == 0)
                    .ToListAsync();
                acPermissions.ForEach(f => f.DeleteMark = 1);
                var now = DateTime.Now;
                List<Permission> newPermissions = new List<Permission>();
                var configs = await _context.ActionConfigs.Where(w => w.DeleteMark == 0).ToListAsync();
                List<ActionWithPermission> actionWithPermissions = new List<ActionWithPermission>();
                viewModels.ForEach(f =>
                {
                    if (f.Selected != null && f.Selected.Count > 0)
                    {

                        Permission tempPermission = new Permission()
                        {
                            CreateTime = now,
                            PermissionId = f.PermissionId,
                            DeleteMark = 0,
                            PermissionName = f.Name,
                            RoleId = roleId,
                            //ActionLists = JsonConvert.SerializeObject(f.Selected),

                        };
                        var tempAction = configs
                            .Where(w => f.Selected.Contains(w.Action))
                            .Select(s => new ActionWithPermission
                            {
                                Action = s.Action,
                                DefaultCheck = s.DefaultCheck,
                                Describe = s.Describe,
                                ActionWithPermissionId = SecurityManage.GuidUpper(),
                                CreateDate = now,
                                DeleteMark = 0,
                                PermissionId = f.PermissionId,
                                UpdateDate = now,
                                RoleId = roleId,
                                MenuId = f.MenuId
                            }).ToList();

                        actionWithPermissions.AddRange(tempAction);
                        //tempPermission.Actions = JsonConvert.SerializeObject(tempAction);
                        newPermissions.Add(tempPermission);

                    }
                });

                if (!viewModels.Any(s => s.Selected != null && s.Selected.Count > 0 && s.MenuId == "6E7DDD5BABE445ABB9F76295F02CC279"))
                {
                    Permission tempPermission = new Permission()
                    {
                        CreateTime = now,
                        PermissionId = "supervisionHome",
                        DeleteMark = 0,
                        PermissionName = "首页",
                        RoleId = roleId,
                        //ActionLists = JsonConvert.SerializeObject(f.Selected),

                    };
                    //var tempAction = configs
                    //    .Where(w => f.Selected.Contains(w.Action))
                    //    .Select(s => new ActionWithPermission
                    //    {
                    //        Action = s.Action,
                    //        DefaultCheck = s.DefaultCheck,
                    //        Describe = s.Describe,
                    //        ActionWithPermissionId = SecurityManage.GuidUpper(),
                    //        CreateDate = now,
                    //        DeleteMark = 0,
                    //        PermissionId = "supervisionHome",
                    //        UpdateDate = now,
                    //        RoleId = roleId,
                    //        MenuId = "6E7DDD5BABE445ABB9F76295F02CC279"
                    //    }).ToList();

                    //actionWithPermissions.AddRange(tempAction);
                    //tempPermission.Actions = JsonConvert.SerializeObject(tempAction);
                    newPermissions.Add(tempPermission);
                }
                _context.Permissions.UpdateRange(permissions);
                _context.ActionWithPermissions.UpdateRange(acPermissions);
                await _context.Permissions.AddRangeAsync(newPermissions);
                await _context.ActionWithPermissions.AddRangeAsync(actionWithPermissions);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "权限配置成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("配置角色权限", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "内部错误");
            }
        }

        [HttpPost]
        public async Task<ResponseViewModel<string>> RemoveRoles([FromBody] Role role)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(role.RoleId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "参数不完整，删除失败");
                }

                var data = await _context.Roles.Where(w => w.Deleted == 0 && w.Status == 1 && w.RoleId == role.RoleId)
                    .OrderByDescending(o => o.CreateTime)
                    .FirstOrDefaultAsync();
                if (data == null)
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "角色不存在或已被删除");
                }

                if (_changeRole.ForbidChange.Contains(data.Name))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "固有角色不允许删除和修改");
                }
                data.Deleted = 1;
                _context.Roles.Update(data);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "角色删除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("删除角色：", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 获取角色列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<Role>>> GetRoles()
        {
            try
            {
                var data = await _context.Roles
                    .Where(w => w.Status == 1 && w.Deleted == 0 && w.IsMainRole == 1)
                    .OrderBy(o => o.CreateTime)
                    .ToListAsync();
                return ResponseViewModel<List<Role>>.Create(Status.SUCCESS, Message.SUCCESS, data);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取角色：", ex);
                return ResponseViewModel<List<Role>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 添加或修改安监站角色
        /// </summary>
        ///<returns></returns>
        [HttpPost]
        public async Task<ResponseViewModel<string>> AddOrUpdateAJRoles([FromBody] Role role)
        {
            try
            {
                var now = DateTime.Now;
                var useruuid = User.FindFirst(nameof(ClaimTypeEnum.Uuid))?.Value;
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                if (string.IsNullOrWhiteSpace(role.RoleId))
                {
                    if (_changeRole.ForbidChange.Contains(role.Name))
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, "此角色名称禁止使用，请更换后重新保存", "此角色名称禁止使用，请更换后重新保存");
                    }
                    // 角色名称不允许重复
                    var data = await _context.Roles
                        .Where(w => w.Deleted == 0 && w.Name == role.Name && w.BelongedTo == belongedTo)
                        .OrderByDescending(o => o.CreateTime)
                        .FirstOrDefaultAsync();
                    if (data != null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "角色名称不允许重复，请修改后重新添加");
                    }
                    // 新增
                    role.RoleId = SecurityManage.GuidUpper();
                    role.Deleted = 0;
                    role.CreatorId = useruuid;
                    role.BelongedTo = belongedTo;
                    role.CreateTime = now;
                    role.Status = 1;
                    role.IsMainRole = 0;
                    await _context.Roles.AddAsync(role);
                }
                else
                {
                    // 更新
                    var data = await _context.Roles.Where(w => w.RoleId == role.RoleId && w.Deleted == 0)
                        .OrderByDescending(o => o.CreateTime)
                        .FirstOrDefaultAsync();

                    if (data == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "角色不存在或已被删除，无法修改");
                    }

                    if (_changeRole.ForbidChange.Contains(data.Name))
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "固有角色不允许删除和修改");
                    }
                    // 角色名称不允许重复
                    var anotherRole = await _context.Roles
                        .Where(w => w.Deleted == 0 && w.Name == role.Name && w.RoleId != role.RoleId && w.BelongedTo == belongedTo)
                        .OrderByDescending(o => o.CreateTime)
                        .FirstOrDefaultAsync();
                    if (anotherRole != null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "角色名称不允许重复，请修改后重新提交");
                    }
                    data.SupervisionDepartmentId = role.SupervisionDepartmentId;
                    data.BelongedTo = belongedTo;
                    data.Describe = role.Describe;
                    data.Name = role.Name;
                    data.IsOwner = role.IsOwner;
                    _context.Roles.Update(data);
                }

                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("添加或更新角色：", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 获取安监站角色列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<Role>>> GetAJRoles()
        {
            try
            {
                var belongedTo = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;
                var data = await _context.Roles
                    .Where(w => (w.Status == 1 && w.Deleted == 0 && w.IsMainRole == 0 && w.BelongedTo == belongedTo) || (w.Name == "安监站" && w.Deleted == 0 && w.IsMainRole == 1))
                    .OrderBy(o => o.CreateTime)
                    .ToListAsync();
                return ResponseViewModel<List<Role>>.Create(Status.SUCCESS, Message.SUCCESS, data);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取角色：", ex);
                return ResponseViewModel<List<Role>>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 获取安监站下角色权限
        /// </summary>
        /// <param name="belongedTo"></param>
        /// <param name="offName"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        [HttpGet]
        //[Authorize]
        public async Task<ResponseViewModel<List<PermissionOfRoleViewModel>>> GetPermissionOfAJRole(string roleId)
        {
            try
            {
                var ajRole = await _context.Roles
                    .Where(w => w.Status == 1 && w.Deleted == 0 && w.IsMainRole == 1 && w.Name == "安监站")
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                if (ajRole == null)
                {
                    return ResponseViewModel<List<PermissionOfRoleViewModel>>.Create(Status.WARN, "管理员角色已失效，非法操作！");
                }
                var permissionIds = await _context.Permissions.Where(w => w.RoleId == ajRole.RoleId && w.DeleteMark == 0)
                    .Select(s => s.PermissionId)
                    .ToListAsync();
                // 先查出所有菜单
                var menus = await _context.MenuPermissionsV2s
                    .Where(w => w.DeleteMark == 0
                        && !string.IsNullOrWhiteSpace(w.Path)
                        && string.IsNullOrWhiteSpace(w.Redirect)
                        && permissionIds.Contains(w.PermisionId))
                    .Select(s => new PermissionOfRoleViewModel
                    {
                        Name = s.Title,

                        Deleted = 0,
                        Status = 1,
                        Indeterminate = false,
                        MenuId = s.MenuId,
                        CheckedAll = false,
                        PermissionId = s.PermisionId,
                        PermissionActions = _context.ActionConfigs.Where(w => w.DeleteMark == 0 && w.MenuId == s.MenuId),
                        //BelongedTo = belongedTo,
                        //OffName = offName,
                        //GroupName = groupName
                    }).AsNoTracking().ToListAsync();

                // 查出当前角色所拥有的权限
                var permissions = await _context.Permissions.Where(w => w.DeleteMark == 0 && w.RoleId == roleId)
                    .Select(s => new PermissionViewModel
                    {
                        //ActionLists = _context.ActionWithPermissions
                        //    .Where(w=>w.RoleId==roleId&&w.DeleteMark==0&&w.PermissionId==s.PermissionId).Select(s=>s.Action),
                        RoleId = s.RoleId,
                        DeleteMark = s.DeleteMark,

                        CreateTime = s.CreateTime,
                        Id = s.Id,
                        PermissionId = s.PermissionId,
                        PermissionName = s.PermissionName
                    })
                    .AsNoTracking()
                    .ToListAsync();

                var actionss = await _context.ActionWithPermissions
                  .Where(w => w.RoleId == roleId
                 && permissions.Select(k => k.PermissionId).Contains(w.PermissionId)
                 && w.DeleteMark == 0)
                  .ToListAsync();

                permissions.ForEach(s =>
                {
                    s.Actions = actionss.Where(k => k.PermissionId == s.PermissionId).ToList();

                });

                // 处理数据
                menus.ForEach(f =>
                {
                    f.ActionOptions = f.PermissionActions.Select(s => new ActionOptions
                    {
                        Label = s.Describe,
                        Value = s.Action
                    }).ToList();
                    f.Actions = f.PermissionActions.Select(s => s.Action).ToList();
                    f.RoleId = roleId;
                });

                var menuDic = menus.ToDictionary(d => d.PermissionId);
                permissions.ForEach(f =>
                {
                    if (string.IsNullOrWhiteSpace(f.PermissionId))
                    {

                    }
                    else if (menuDic.ContainsKey(f.PermissionId))
                    {
                        menuDic[f.PermissionId].Selected = f.ActionList.ToList();
                        var temp = menuDic[f.PermissionId].PermissionActions?.Select(s => s.Action).ToList();
                        if (temp != null && menuDic[f.PermissionId].Selected != null)
                        {
                            if (temp.All(menuDic[f.PermissionId].Selected.Contains))
                            {
                                menuDic[f.PermissionId].CheckedAll = true;
                            }
                        }
                    }
                });

                return ResponseViewModel<List<PermissionOfRoleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, menus);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取角色权限", ex);
                return ResponseViewModel<List<PermissionOfRoleViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 根据角色获取安监站人员
        /// </summary>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <param name="belongedTo"></param>
        /// <param name="offName"></param>
        /// <param name="groupName"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<SysUserManager>>> GetAJRoleUser(int page, int limit, string belongedTo
            , string roleId, string query)
        {
            try
            {
                //登录人belongedto
                var belongedto = User.FindFirst(nameof(ClaimTypeEnum.BelongedTo))?.Value;

                if (string.IsNullOrWhiteSpace(belongedTo))
                {
                    return ResponseViewModel<List<SysUserManager>>.Create(Status.FAIL, Message.FAIL);
                }
                // 暂时这么处理，若以后数据变多此处加载过慢，请关联表查询角色
                var roleDic = await (from a in _context.Roles
                                     join b in _context.UserRoles
                                     on a.RoleId equals b.RoleId
                                     where a.BelongedTo == belongedTo && a.Deleted == 0 && b.DeleteMark == 0
                                     select new
                                     {
                                         a.RoleId,
                                         a.Name,
                                         b.SysUserManagerUuid,
                                     }).ToListAsync();


                var data = from a in _context.SysUserManager
                           join b in _context.UserRoles.Where(x => x.DeleteMark == 0)
                           on a.Uuid equals b.SysUserManagerUuid
                            into t1
                           from r in t1.DefaultIfEmpty()
                           where a.DeleteMark == 0
                           select new { a, r };

                if (belongedTo != "AJ320000-1")
                {
                    // 查询所有的
                    data = data.Where(w => w.a.BelongedTo == belongedTo);
                }
                if (!string.IsNullOrWhiteSpace(query))
                {
                    // 查询所有的
                    data = data.Where(w => w.a.UserName.Contains(query));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(roleId))
                    {
                        data = data.Where(w => w.r != null && w.r.RoleId == roleId);
                    }
                }
                var count = await data.CountAsync();
                if (count <= 0)
                {
                    return ResponseViewModel<List<SysUserManager>>.Create(Status.SUCCESS, Message.SUCCESS, new List<SysUserManager>(), count);
                }
                if (page == 0)
                {
                    page = 1;
                }
                if (limit == 0)
                {
                    limit = 10;
                }

                var result = await data.Select(o => o.a).Distinct().OrderByDescending(o => o.Id)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .AsNoTracking()
                    .ToListAsync();


                result.ForEach(f =>
                {
                    var roles = roleDic.Where(w => w.SysUserManagerUuid == f.Uuid)
                    .Select(w => new KeyValueDic { Key = w.RoleId, Value = w.Name }).ToList();
                    var roleNameStr = "";
                    roles.ForEach(k =>
                    {
                        roleNameStr += k.Value + ",";
                    });
                    f.Role = roleNameStr.TrimEnd(',');
                    f.Roles = roles;

                });
                return ResponseViewModel<List<SysUserManager>>.Create(Status.SUCCESS, Message.SUCCESS, result, count);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取角色对应的用户：" + ex.Message, ex);
                return ResponseViewModel<List<SysUserManager>>.Create(Status.ERROR, Message.ERROR);
            }
        }


        [HttpGet]
        //[Authorize]
        public async Task<ResponseViewModel<List<PermissionOfRoleViewModel>>> GetPermissionOfRole(string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return ResponseViewModel<List<PermissionOfRoleViewModel>>.Create(Status.FAIL, Message.FAIL);
            }
            try
            {
                // 先查出所有菜单
                var menus = await _context.MenuPermissionsV2s
                    .Where(w => w.DeleteMark == 0 && !string.IsNullOrWhiteSpace(w.Path) && string.IsNullOrWhiteSpace(w.Redirect))
                    .Select(s => new PermissionOfRoleViewModel
                    {
                        Name = s.Title,

                        Deleted = 0,
                        Status = 1,
                        Indeterminate = false,
                        MenuId = s.MenuId,
                        CheckedAll = false,
                        PermissionId = s.PermisionId,

                        PermissionActions = _context.ActionConfigs.Where(w => w.DeleteMark == 0 && w.MenuId == s.MenuId)
                    }).AsNoTracking().ToListAsync();



                // 查出当前角色所拥有的权限
                var permissions = await _context.Permissions.Where(w => w.DeleteMark == 0 && w.RoleId == roleId)
                    .Select(s => new PermissionViewModel
                    {
                        //ActionLists = _context.ActionWithPermissions
                        //    .Where(w=>w.RoleId==roleId&&w.DeleteMark==0&&w.PermissionId==s.PermissionId).Select(s=>s.Action),
                        RoleId = s.RoleId,
                        DeleteMark = s.DeleteMark,

                        CreateTime = s.CreateTime,
                        Id = s.Id,
                        PermissionId = s.PermissionId,
                        PermissionName = s.PermissionName
                    })
                    .AsNoTracking()
                    .ToListAsync();

                var actionss = await _context.ActionWithPermissions
                      .Where(w => w.RoleId == roleId
                     && permissions.Select(k => k.PermissionId).Contains(w.PermissionId)
                     && w.DeleteMark == 0)
                      .ToListAsync();

                permissions.ForEach(s =>
                {
                    s.Actions = actionss.Where(k => k.PermissionId == s.PermissionId).ToList();

                });
                // 处理数据
                menus.ForEach(f =>
                {
                    f.ActionOptions = f.PermissionActions.Select(s => new ActionOptions
                    {
                        Label = s.Describe,
                        Value = s.Action
                    }).ToList();
                    f.Actions = f.PermissionActions.Select(s => s.Action).ToList();
                    f.RoleId = roleId;
                });

                var menuDic = menus.ToDictionary(d => d.PermissionId);
                permissions.ForEach(f =>
                {
                    if (string.IsNullOrWhiteSpace(f.PermissionId))
                    {

                    }
                    else if (menuDic.ContainsKey(f.PermissionId))
                    {
                        menuDic[f.PermissionId].Selected = f.ActionList.ToList();
                        var temp = menuDic[f.PermissionId].PermissionActions?.Select(s => s.Action).ToList();
                        if (temp != null && menuDic[f.PermissionId].Selected != null)
                        {
                            if (temp.All(menuDic[f.PermissionId].Selected.Contains))
                            {
                                menuDic[f.PermissionId].CheckedAll = true;
                            }
                        }
                    }
                });

                return ResponseViewModel<List<PermissionOfRoleViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, menus);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取角色权限", ex);
                return ResponseViewModel<List<PermissionOfRoleViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }



        [HttpPost]
        public async Task<ResponseViewModel<string>> AddOrUpdateActionConfig([FromBody] ActionConfig config)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(config.MenuId))
                {
                    return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "菜单id不允许空");
                }
                var now = DateTime.Now;
                if (string.IsNullOrWhiteSpace(config.ActionConfigId))
                {
                    var data = await _context.ActionConfigs
                        .Where(w => w.DeleteMark == 0 && w.Action == config.Action && w.MenuId == config.MenuId)
                        .OrderByDescending(o => o.CreateDate)
                        .FirstOrDefaultAsync();
                    if (data != null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "功能权限id不允许重复，请修改后重新提交");
                    }
                    // 新增
                    config.ActionConfigId = SecurityManage.GuidUpper();
                    config.CreateDate = now;
                    config.DefaultCheck = false;
                    config.DeleteMark = 0;
                    config.UpdateDate = now;
                    await _context.ActionConfigs.AddAsync(config);

                }
                else
                {
                    // 更新
                    var data = await _context.ActionConfigs
                        .Where(w => w.MenuId == config.MenuId && w.ActionConfigId == config.ActionConfigId && w.DeleteMark == 0)
                        .OrderByDescending(o => o.UpdateDate)
                        .FirstOrDefaultAsync();
                    if (data == null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "功能权限不存在或已被删除，无法修改");
                    }

                    var anotherData = await _context.ActionConfigs
                        .Where(w => w.DeleteMark == 0 && w.Action == config.Action && w.MenuId != config.MenuId)
                        .OrderByDescending(o => o.CreateDate)
                        .FirstOrDefaultAsync();
                    if (anotherData != null)
                    {
                        return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "功能权限id不允许重复，请修改后重新提交");
                    }

                    data.Action = config.Action;
                    data.UpdateDate = now;
                    data.DefaultCheck = config.DefaultCheck;
                    data.Describe = config.Describe;
                    _context.ActionConfigs.Update(data);
                }
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, Message.SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError("添加或更新操作权限", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, Message.ERROR);
            }
        }


        /// <summary>
        /// 配置角色权限
        /// 2019-04-19
        /// machuanlong
        /// </summary>
        /// <param name="permissions"></param>
        /// <returns></returns>
        [HttpPost]
        //[Authorize]
        public async Task<ResponseViewModel<string>> AddPermission([FromBody] List<PermissionOfRoleViewModel> viewModels)
        {
            if (viewModels.Count == 0)
            {
                return ResponseViewModel<string>.Create(Status.FAIL, Message.FAIL, "更新配置数据不可为空");
            }
            try
            {
                var permissions = await _context.Permissions
                    .Where(w => w.RoleId == viewModels[0].RoleId && w.DeleteMark == 0)
                    .ToListAsync();
                permissions.ForEach(f => f.DeleteMark = 1);

                var acPermissions = await _context.ActionWithPermissions
                    .Where(w => w.RoleId == viewModels[0].RoleId && w.DeleteMark == 0)
                    .ToListAsync();
                acPermissions.ForEach(f => f.DeleteMark = 1);
                var now = DateTime.Now;
                List<Permission> newPermissions = new List<Permission>();
                var configs = await _context.ActionConfigs.Where(w => w.DeleteMark == 0).ToListAsync();
                List<ActionWithPermission> actionWithPermissions = new List<ActionWithPermission>();
                viewModels.ForEach(f =>
                {
                    if (f.Selected != null && f.Selected.Count > 0)
                    {
                        Permission tempPermission = new Permission()
                        {
                            CreateTime = now,
                            PermissionId = f.PermissionId,
                            DeleteMark = 0,
                            PermissionName = f.Name,
                            RoleId = f.RoleId,
                            //ActionLists = JsonConvert.SerializeObject(f.Selected),

                        };
                        var tempAction = configs
                            .Where(w => f.Selected.Contains(w.Action))
                            .Select(s => new ActionWithPermission
                            {
                                Action = s.Action,
                                DefaultCheck = s.DefaultCheck,
                                Describe = s.Describe,
                                ActionWithPermissionId = SecurityManage.GuidUpper(),
                                CreateDate = now,
                                DeleteMark = 0,
                                PermissionId = f.PermissionId,
                                UpdateDate = now,
                                RoleId = f.RoleId,
                                MenuId = f.MenuId
                            }).ToList();

                        actionWithPermissions.AddRange(tempAction);
                        //tempPermission.Actions = JsonConvert.SerializeObject(tempAction);
                        newPermissions.Add(tempPermission);
                    }
                });

                _context.Permissions.UpdateRange(permissions);
                _context.ActionWithPermissions.UpdateRange(acPermissions);
                await _context.Permissions.AddRangeAsync(newPermissions);
                await _context.ActionWithPermissions.AddRangeAsync(actionWithPermissions);
                await _context.SaveChangesAsync();
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "权限配置成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("配置角色权限", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR, "内部错误");
            }
        }



        /// <summary>
        /// 版本更新列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<VersionInfo>>> GetVersionInfo(int pageIndex, int pageSize)
        {
            try
            {

                var type = User.FindFirst(nameof(ClaimTypeEnum.Type))?.Value;//0项目1企业 
                if (type == "0" || type == "1")
                {
                    var data = await _context.VersionInfo
                                       .Where(w => (w.UnitType + "#").Contains("项目部#") && w.DeleteMark == 0).OrderByDescending(w => w.UpdateTime)
                                       .AsNoTracking()
                                       .ToListAsync();
                    data = data.Skip((pageIndex - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                    return ResponseViewModel<List<VersionInfo>>.Create(Status.SUCCESS, Message.SUCCESS, data);
                }
                else
                {
                    var data = await _context.VersionInfo
                                      .Where(w => (w.UnitType + "#").Contains("安监站#") && w.DeleteMark == 0).OrderByDescending(w => w.UpdateTime)
                                      .AsNoTracking()
                                      .ToListAsync();
                    data = data.Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                    return ResponseViewModel<List<VersionInfo>>.Create(Status.SUCCESS, Message.SUCCESS, data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("版本更新列表：", ex);
                return ResponseViewModel<List<VersionInfo>>.Create(Status.ERROR, Message.ERROR);
            }
        }



        [HttpGet]
        public async Task<ResponseViewModel<List<SelectProjectListViewModel>>> GetUserProInfos(string userNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userNumber))
                {
                    return ResponseViewModel<List<SelectProjectListViewModel>>.Create(Status.FAIL, Message.FAIL);
                }
                List<SelectProjectListViewModel> result = new List<SelectProjectListViewModel>();
                var data = await (from w in _context.AppUserProInfo
                                  join b in _context.ProjectOverview
                                  on new { w.BelongedTo, w.RecordNumber } equals new { b.BelongedTo, b.RecordNumber }
                                  where w.UserNumber == userNumber && !string.IsNullOrEmpty(w.RecordNumber) && w.BelongedTo != "AJ320510-1"
                            && w.BelongedTo != "AJ320583-1" && w.BelongedTo != "AJ320501-1"
                            && w.BelongedTo != "AJ320505-1" && w.BelongedTo != "AJ320506-1"
                            && w.BelongedTo != "AJ320507-1" && w.BelongedTo != "AJ320508-1"
                            && w.BelongedTo != "AJ320511-1" && w.BelongedTo != "AJ320581-1"
                            && w.BelongedTo != "AJ320582-1" && w.BelongedTo != "AJ320584-1"
                            && w.BelongedTo != "AJ320585-1" && w.BelongedTo != "AJ320585-2"
                                  select new SelectProjectListViewModel
                                  {
                                      Id = w.Id,
                                      BelongedTo = b.BelongedTo,
                                      RecordNumber = b.RecordNumber,
                                      ConstructionEntCode = w.ConstructionEntCode,
                                      ConstructionUnit = w.ConstructionUnit,
                                      ProjectName = b.ProjectName,
                                      UserNumber = w.UserNumber,
                                      UserType = w.UserType,
                                      Remarks = w.Remarks,
                                      ProjectCode = b.ProjectCode,
                                      GCDM = b.GCDM,
                                  }).AsNoTracking().ToListAsync();

                var data2 = await (from w in _context.AppUserProInfo
                                   join b in _context.ProjectOverview
                                   on new { w.BelongedTo, w.RecordNumber } equals new { b.BelongedTo, b.RecordNumber }
                                   where w.UserNumber == userNumber && !string.IsNullOrEmpty(w.RecordNumber) && (w.BelongedTo == "AJ320510-1"
                             || w.BelongedTo == "AJ320583-1")
                                   select new SelectProjectListViewModel
                                   {
                                       Id = w.Id,
                                       BelongedTo = b.BelongedTo,
                                       RecordNumber = b.RecordNumber,
                                       ConstructionEntCode = w.ConstructionEntCode,
                                       ConstructionUnit = w.ConstructionUnit,
                                       ProjectName = b.ProjectName,
                                       UserNumber = w.UserNumber,
                                       UserType = w.UserType,
                                       Remarks = w.Remarks,
                                       ProjectCode = b.ProjectCode,
                                       GCDM = b.GCDM,
                                   }).AsNoTracking()
                     .ToListAsync();


                var data3 = await (from w in _context.AppUserProInfo
                                   join b in _context.ProjectOverview
                                   on new { w.BelongedTo, w.RecordNumber } equals new { b.BelongedTo, b.RecordNumber }
                                   where w.UserNumber == userNumber && !string.IsNullOrEmpty(w.RecordNumber)
                                   && (w.BelongedTo == "AJ320501-1" ||
                                    w.BelongedTo == "AJ320505-1" ||
                                    w.BelongedTo == "AJ320506-1" ||
                                    w.BelongedTo == "AJ320507-1" ||
                                    w.BelongedTo == "AJ320508-1" ||
                                    w.BelongedTo == "AJ320511-1" ||
                                    w.BelongedTo == "AJ320581-1" ||
                                    w.BelongedTo == "AJ320582-1" ||
                                    w.BelongedTo == "AJ320584-1" ||
                                    w.BelongedTo == "AJ320585-1" ||
                                    w.BelongedTo == "AJ320585-2")
                                   select new SelectProjectListViewModel
                                   {
                                       Id = w.Id,
                                       BelongedTo = b.BelongedTo,
                                       RecordNumber = b.RecordNumber,
                                       ConstructionEntCode = w.ConstructionEntCode,
                                       ConstructionUnit = w.ConstructionUnit,
                                       ProjectName = b.ProjectName,
                                       UserNumber = w.UserNumber,
                                       UserType = w.UserType,
                                       Remarks = w.Remarks,
                                       ProjectCode = b.ProjectCode,
                                       GCDM = b.GCDM,
                                   }).AsNoTracking()
                  .ToListAsync();

                result.AddRange(data2);
                result.AddRange(data3);
                result.AddRange(data);


                return ResponseViewModel<List<SelectProjectListViewModel>>.Create(Status.SUCCESS, Message.SUCCESS, result);
            }
            catch (Exception ex)
            {
                _logger.LogError("获取用户有哪些项目：", ex);
                return ResponseViewModel<List<SelectProjectListViewModel>>.Create(Status.ERROR, Message.ERROR);
            }
        }
    }
}
