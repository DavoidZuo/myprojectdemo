using Common;
using MCUtil.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViewModels;

namespace JSAJNanJing.Controller
{
    /// <summary>
    /// 版本更新
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SendPhoneMsgController : ControllerBase
    {

        private readonly IWebHostEnvironment _environment;

        private readonly ILogger<SendPhoneMsgController> _logger;
        private string _wordTemplte;
        public SendPhoneMsgController(IWebHostEnvironment environment, ILogger<SendPhoneMsgController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ResponseViewModel<string>> SetPassKey()
        {
            try
            {
                DateTime now = DateTime.Now;
                //把当前时间生成10位数的时间戳
                string shijiancuo = SecurityManage.GetTimestamp10bit(now).ToString();
                //时间戳aes加密
                string miwen = SecurityManage.EncryptString_Aes(shijiancuo, KeyAndIV.KEY, KeyAndIV.IV);
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, miwen);
            }
            catch (Exception ex)
            {
                _logger.LogError("操作成功：", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }

        /// <summary>
        /// 删除消息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ResponseViewModel<string>> SetRedisValue(string key, string code)
        {
            try
            {

                await RedisHelper.SetAsync(key, code, 600);
                return ResponseViewModel<string>.Create(Status.SUCCESS, Message.SUCCESS, "操作成功");
            }
            catch (Exception ex)
            {
                _logger.LogError("操作成功：", ex);
                return ResponseViewModel<string>.Create(Status.ERROR, Message.ERROR);
            }
        }


    }
}
