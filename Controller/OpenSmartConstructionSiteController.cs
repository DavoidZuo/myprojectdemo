using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Common;
using JSAJ.Core.Common;
using JSAJ.Core.Models;
using JSAJ.Core.Models.notice;
using JSAJ.Core.ViewModels;
using JSAJ.ViewModels.ViewModels.OpenSmartSite;
using MCUtil.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ViewModels;

namespace JSAJ.Core.Controllers.SmartSite
{
    /// <summary>
    /// 智慧工地开通
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]

    public class OpenSmartConstructionSiteController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        private readonly ILogger<OpenSmartConstructionSiteController> _logger;
        private readonly JssanjianmanagerContext _context;
        private JwtSettings settings;
        private OssFileSetting _ossFileSetting;
        private string _wordTemplte;
        private readonly string _SmartSiteWebUrl;
        public OpenSmartConstructionSiteController(IWebHostEnvironment environment, IConfiguration configuration, ILogger<OpenSmartConstructionSiteController> logger, JssanjianmanagerContext context, IOptions<JwtSettings> options, IOptions<OssFileSetting> oss)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            settings = options.Value;
            _ossFileSetting = oss.Value;
            _wordTemplte = environment.WebRootPath + Path.DirectorySeparatorChar + "doc" + Path.DirectorySeparatorChar;
            _SmartSiteWebUrl = configuration.GetConnectionString("SmartSiteWebUrl");
        }


        /// <summary>
        /// 根据备案号精确查找或者根据项目名称模糊匹配
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ResponseViewModel<List<ProjectOverview>>> SearchProjectInfo(string word, int type)
        {
            try
            {


                if (type == 0)
                {
                    var obj = await _context.ProjectOverview.Where(w => w.RecordNumber == word)
                   .OrderByDescending(w => w.Id).ToListAsync();
                    if (obj.Count > 0)
                    {
                        return ResponseViewModel<List<ProjectOverview>>.Create(Status.SUCCESS, Message.SUCCESS, obj, 1);
                    }
                }
                else
                {
                    var obj = await _context.ProjectOverview.Where(w => w.ProjectName.Contains(word))
                  .OrderByDescending(w => w.Id).ToListAsync();
                    if (obj.Count > 0)
                    {
                        return ResponseViewModel<List<ProjectOverview>>.Create(Status.SUCCESS, Message.SUCCESS, obj, 1);
                    }
                }
                return ResponseViewModel<List<ProjectOverview>>.Create(Status.SUCCESS, Message.SUCCESS, null, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError("根据备案号精确查找或者根据项目名称模糊匹配：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<List<ProjectOverview>>.Create(Status.ERROR, Message.ERROR);
            }


        }




        /// <summary>
        /// 根据备案号精确查找或者根据项目名称模糊匹配
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<SmartSiteViewAccount>> SearchSmartSiteAccountJssInfo(string recordNumber, string belongedTo)
        {
            try
            {
                var url = _SmartSiteWebUrl + "api/System/SearchSmartSiteAccountJssInfo?recordNumber=" + recordNumber + "&belongedTo=" + belongedTo;
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";
                string result = "";
                try
                {
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            result = reader.ReadToEnd();

                            ReturnResponseViewModel<SmartSiteViewAccount> res = JsonConvert.DeserializeObject<ReturnResponseViewModel<SmartSiteViewAccount>>(result);
                            if (res != null && res.Code == 0 && res.Data != null)
                            {
                                return ResponseViewModel<SmartSiteViewAccount>.Create(Status.SUCCESS, Message.SUCCESS, res.Data, res.Count);
                            }
                            else
                            {
                                return ResponseViewModel<SmartSiteViewAccount>.Create(res.Code, res.Message, res.Data, res.Count);
                            }
                        }
                    }
                }
                catch
                {
                    return ResponseViewModel<SmartSiteViewAccount>.Create(Status.SUCCESS, url + "接口调用异常");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("返回智慧工地账号：" + ex.Message + "\r\n" + ex.StackTrace, ex);
                return ResponseViewModel<SmartSiteViewAccount>.Create(Status.ERROR, Message.ERROR);
            }


        }



        /// <summary>
        /// 从省安管系统开通的智慧工地项目列表
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<List<SmartSiteViewModelJss>>> SearchSmartSiteAccountFromJss(int pageIndex, int pageSize, string word)
        {
            var url = _SmartSiteWebUrl + "api/System/SearchSmartSiteAccountFromJss?pageIndex=" + pageIndex + "&pageSize=" + pageSize + "&word=" + word;
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            string result = "";
            try
            {
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        result = reader.ReadToEnd();

                        ReturnResponseViewModel<List<SmartSiteViewModelJss>> res = JsonConvert.DeserializeObject<ReturnResponseViewModel<List<SmartSiteViewModelJss>>>(result);
                        if (res != null && res.Code == 0 && res.Data != null)
                        {
                            return ResponseViewModel<List<SmartSiteViewModelJss>>.Create(Status.SUCCESS, Message.SUCCESS, res.Data, res.Count);
                        }
                        else
                        {
                            return ResponseViewModel<List<SmartSiteViewModelJss>>.Create(res.Code, res.Message, res.Data, res.Count);
                        }
                    }
                }
            }
            catch
            {
                return ResponseViewModel<List<SmartSiteViewModelJss>>.Create(Status.SUCCESS, url + "接口调用异常");
            }
        }



        /// <summary>
        /// 开通智慧工地账号
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> AddSmartSiteAccount2(OpenSmartSiteViewModelJss account)
        {

            if (account.Id <= 0
                || string.IsNullOrWhiteSpace(account.BelongedTo)
                 || string.IsNullOrWhiteSpace(account.RecordNumber)
                 )
            {
                return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
            }
            try
            {
                string accountTel = "";
                var projectInfo = await _context.ProjectOverview.Where(a => a.BelongedTo == account.BelongedTo
                && a.RecordNumber == account.RecordNumber && a.Id == account.Id).OrderByDescending(a => a.Id).FirstOrDefaultAsync();
                string cityName = await (from a in _context.CityZone
                                         join b in _context.CityZone
                                         on a.ParentCityCode equals b.CityCode
                                         where a.BelongedTo == account.BelongedTo
                                         orderby b.Id descending
                                         select b.CityShortName).FirstOrDefaultAsync();
                if (projectInfo != null)
                {
                    var xiangmujingli = await _context.AppUserProInfo.Where(a => a.BelongedTo == account.BelongedTo
                   && a.RecordNumber == account.RecordNumber).OrderByDescending(a => a.Id).FirstOrDefaultAsync();
                    if (xiangmujingli != null)
                    {
                        var zhanghao = await _context.AppUserInfo.Where(a => a.UserNumber == xiangmujingli.UserNumber).OrderByDescending(a => a.Id).FirstOrDefaultAsync();
                        accountTel = zhanghao.UserPhoneNum;
                    }
                }
                decimal projectArea = 0;
                var url = _SmartSiteWebUrl + "api/System/AddSmartSiteAccountFromJss";
                string strURL = url;
                //创建一个HTTP请求  
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strURL);
                //Post请求方式  
                request.Method = "POST";
                //内容类型
                request.ContentType = "application/json";

                SmartSiteViewModelJssParam param = new SmartSiteViewModelJssParam();
                param.belongedTo = projectInfo.BelongedTo;
                param.recordNumber = projectInfo.RecordNumber;
                param.projectName = projectInfo.ProjectName;
                param.projectAddress = projectInfo.ProjectAddress;
                param.accountTel = accountTel;
                param.accountValidityStartTime = account.AccountValidityStartTime;
                param.accountValidityEndTime = account.AccountValidityEndTime;
                param.recordDate = ((DateTime)projectInfo.RecordDate);
                param.projectStartDateTimne = ((DateTime)projectInfo.ProjectStartDateTimne);
                param.projectEntDateTimne = ((DateTime)projectInfo.ProjectEndDateTimne);
                param.LongitudeCoordinate = projectInfo.LongitudeCoordinate;
                param.LatitudeCoordinate = projectInfo.LatitudeCoordinate;
                param.ProjectCategory = projectInfo.ProjectCategory;

                if (decimal.TryParse(projectInfo.ProjectArea, out projectArea))
                {
                    param.ProjectArea = projectArea;
                }
                param.ProjectCost = projectInfo.ProjectPrice == null ? 0 : (decimal)projectInfo.ProjectPrice;
                param.City = cityName == null ? "" : cityName.Replace("市", "");
                //设置参数，并进行URL编码 
                string paraUrlCoded = JsonConvert.SerializeObject(param);//System.Web.HttpUtility.UrlEncode(jsonParas); 
                byte[] payload;
                //将Json字符串转化为字节  
                payload = System.Text.Encoding.UTF8.GetBytes(paraUrlCoded);
                //设置请求的ContentLength   
                request.ContentLength = payload.Length;
                //发送请求，获得请求流 
                Stream writer;
                try
                {
                    writer = request.GetRequestStream();//获取用于写入请求数据的Stream对象
                }
                catch (Exception)
                {
                    writer = null;
                    Console.Write("连接服务器失败!");
                }
                //将请求参数写入流
                writer.Write(payload, 0, payload.Length);
                writer.Close();//关闭请求流
                               // String strValue = "";//strValue为http响应所返回的字符流
                HttpWebResponse response;
                try
                {
                    //获得响应流
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException ex)
                {
                    response = ex.Response as HttpWebResponse;
                }
                Stream s = response.GetResponseStream();
                //  Stream postData = Request.InputStream;
                StreamReader sRead = new StreamReader(s);
                string postContent = sRead.ReadToEnd();
                sRead.Close();

                ReturnResponseViewModel<string> res = JsonConvert.DeserializeObject<ReturnResponseViewModel<string>>(postContent);
                if (res != null && res.Code == 0)
                {
                    projectInfo.IsSmartSite = 1;
                    _context.ProjectOverview.Update(projectInfo);
                    _context.SaveChanges();
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, res.Data, res.Count);
                }
                else
                {
                    return ResponseViewModel<string>.Create(res.Code, res.Message, res.Data, res.Count);
                }
            }
            catch (Exception ex1)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }


        }

        /// <summary>
        /// 编辑智慧工地有效期
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> EditSmartSiteAccountJss(SmartSiteViewModelJss model)
        {

            if (string.IsNullOrWhiteSpace(model.ProjectInfoId)
                 || model.AccountValidityEndTime == null
                 || model.AccountValidityStartTime == null
                 )
            {
                return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
            }
            try
            {

                var url = _SmartSiteWebUrl + "api/System/EditSmartSiteAccountJss";
                string strURL = url;
                //创建一个HTTP请求  
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strURL);
                //Post请求方式  
                request.Method = "POST";
                //内容类型
                request.ContentType = "application/json";

                SmartSiteViewModelJss param = new SmartSiteViewModelJss();
                param.AccountValidityStartTime = model.AccountValidityStartTime;
                param.AccountValidityEndTime = model.AccountValidityEndTime;
                param.ProjectInfoId = model.ProjectInfoId;
                //设置参数，并进行URL编码 
                string paraUrlCoded = JsonConvert.SerializeObject(param);//System.Web.HttpUtility.UrlEncode(jsonParas); 
                byte[] payload;
                //将Json字符串转化为字节  
                payload = System.Text.Encoding.UTF8.GetBytes(paraUrlCoded);
                //设置请求的ContentLength   
                request.ContentLength = payload.Length;
                //发送请求，获得请求流 
                Stream writer;
                try
                {
                    writer = request.GetRequestStream();//获取用于写入请求数据的Stream对象
                }
                catch (Exception)
                {
                    writer = null;
                    Console.Write("连接服务器失败!");
                }
                //将请求参数写入流
                writer.Write(payload, 0, payload.Length);
                writer.Close();//关闭请求流
                               // String strValue = "";//strValue为http响应所返回的字符流
                HttpWebResponse response;
                try
                {
                    //获得响应流
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException ex)
                {
                    response = ex.Response as HttpWebResponse;
                }
                Stream s = response.GetResponseStream();
                //  Stream postData = Request.InputStream;
                StreamReader sRead = new StreamReader(s);
                string postContent = sRead.ReadToEnd();
                sRead.Close();

                ReturnResponseViewModel<string> res = JsonConvert.DeserializeObject<ReturnResponseViewModel<string>>(postContent);
                if (res != null && res.Code == 0)
                {
                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, res.Data, res.Count);
                }
                else
                {
                    return ResponseViewModel<string>.Create(res.Code, res.Message, res.Data, res.Count);
                }
            }
            catch (Exception ex1)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }


        }


        /// <summary>
        /// 启用禁用智慧工地
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<ResponseViewModel<string>> EditSmartSiteAccountStatusJss(SmartSiteViewModelJss model)
        {

            if (string.IsNullOrWhiteSpace(model.ProjectInfoId)
                 || string.IsNullOrWhiteSpace(model.BelongedTo)
                 || string.IsNullOrWhiteSpace(model.RecordNumber)
                 || model.Status == null
                 )
            {
                return ResponseViewModel<string>.Create(Status.FAIL, "参数错误");
            }
            try
            {

                var url = _SmartSiteWebUrl + "api/System/EditSmartSiteAccountStatusJss";
                string strURL = url;
                //创建一个HTTP请求  
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strURL);
                //Post请求方式  
                request.Method = "POST";
                //内容类型
                request.ContentType = "application/json";

                SmartSiteViewModelJss param = new SmartSiteViewModelJss();
                param.Status = model.Status;
                param.ProjectInfoId = model.ProjectInfoId;
                //设置参数，并进行URL编码 
                string paraUrlCoded = JsonConvert.SerializeObject(param);//System.Web.HttpUtility.UrlEncode(jsonParas); 
                byte[] payload;
                //将Json字符串转化为字节  
                payload = System.Text.Encoding.UTF8.GetBytes(paraUrlCoded);
                //设置请求的ContentLength   
                request.ContentLength = payload.Length;
                //发送请求，获得请求流 
                Stream writer;
                try
                {
                    writer = request.GetRequestStream();//获取用于写入请求数据的Stream对象
                }
                catch (Exception)
                {
                    writer = null;
                    Console.Write("连接服务器失败!");
                }
                //将请求参数写入流
                writer.Write(payload, 0, payload.Length);
                writer.Close();//关闭请求流
                               // String strValue = "";//strValue为http响应所返回的字符流
                HttpWebResponse response;
                try
                {
                    //获得响应流
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException ex)
                {
                    response = ex.Response as HttpWebResponse;
                }
                Stream s = response.GetResponseStream();
                //  Stream postData = Request.InputStream;
                StreamReader sRead = new StreamReader(s);
                string postContent = sRead.ReadToEnd();
                sRead.Close();

                ReturnResponseViewModel<string> res = JsonConvert.DeserializeObject<ReturnResponseViewModel<string>>(postContent);
                if (res != null && res.Code == 0)
                {
                    var projectInfo = await _context.ProjectOverview.Where(w => w.BelongedTo == model.BelongedTo && w.RecordNumber == model.RecordNumber).FirstOrDefaultAsync();
                    projectInfo.IsSmartSite = model.Status;
                    _context.ProjectOverview.Update(projectInfo);
                    _context.SaveChanges();

                    return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, res.Data, res.Count);
                }
                else
                {
                    return ResponseViewModel<string>.Create(res.Code, res.Message, res.Data, res.Count);
                }
            }
            catch (Exception ex1)
            {
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }


        }

    }

}