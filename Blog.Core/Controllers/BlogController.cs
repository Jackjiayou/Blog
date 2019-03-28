﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blog.Core.Common;
using Blog.Core.Common.Helper;
using Blog.Core.IServices;
using Blog.Core.Model.Models;
using Blog.Core.SwaggerHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Profiling;
using static Blog.Core.SwaggerHelper.CustomApiVersion;

namespace Blog.Core.Controllers
{
    /// <summary>
    /// Blog控制器所有接口
    /// </summary>
    [Produces("application/json")]
    [Route("api/Blog")]
    public class BlogController : Controller
    {
        readonly IBlogArticleServices _blogArticleServices;
        readonly IRedisCacheManager _redisCacheManager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="blogArticleServices"></param>
        /// <param name="redisCacheManager"></param>
        public BlogController(IBlogArticleServices blogArticleServices, IRedisCacheManager redisCacheManager)
        {
            _blogArticleServices = blogArticleServices;
            _redisCacheManager = redisCacheManager;
        }


        /// <summary>
        /// 获取博客列表
        /// </summary>
        /// <param name="id"></param>
        /// <param name="page"></param>
        /// <param name="bcategory"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<object> Get(int id, int page = 1, string bcategory = "技术博文")
        {
            int intTotalCount = 6;
            int total;
            int totalCount = 1;
            List<BlogArticle> blogArticleList = new List<BlogArticle>();

            using (MiniProfiler.Current.Step("开始加载数据："))
            {
                try
                {
                    if (_redisCacheManager.Get<object>("Redis.Blog") != null)
                    {
                        MiniProfiler.Current.Step("从Redis服务器中加载数据：");
                        blogArticleList = _redisCacheManager.Get<List<BlogArticle>>("Redis.Blog");
                    }
                    else
                    {
                        MiniProfiler.Current.Step("从MSSQL服务器中加载数据：");
                        blogArticleList = await _blogArticleServices.Query(a => a.bcategory == bcategory);
                        _redisCacheManager.Set("Redis.Blog", blogArticleList, TimeSpan.FromHours(2));
                    }

                }
                catch (Exception e)
                {
                    MiniProfiler.Current.CustomTiming("Errors：", e.Message);
                    blogArticleList = await _blogArticleServices.Query(a => a.bcategory == bcategory);
                }
            }

            total = blogArticleList.Count();
            totalCount = blogArticleList.Count() / intTotalCount;

            using (MiniProfiler.Current.Step("获取成功后，开始处理最终数据"))
            {
                blogArticleList = blogArticleList.OrderByDescending(d => d.bID).Skip((page - 1) * intTotalCount).Take(intTotalCount).ToList();

                foreach (var item in blogArticleList)
                {
                    if (!string.IsNullOrEmpty(item.bcontent))
                    {
                        item.bRemark = (HtmlHelper.ReplaceHtmlTag(item.bcontent)).Length >= 200 ? (HtmlHelper.ReplaceHtmlTag(item.bcontent)).Substring(0, 200) : (HtmlHelper.ReplaceHtmlTag(item.bcontent));
                        int totalLength = 500;
                        if (item.bcontent.Length > totalLength)
                        {
                            item.bcontent = item.bcontent.Substring(0, totalLength);
                        }
                    }
                }
            }

            return Ok(new
            {
                success = true,
                page = page,
                total = total,
                pageCount = totalCount,
                data = blogArticleList
            });
        }


        // GET: api/Blog/5
        /// <summary>
        /// 获取详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        //[Authorize("Permission")]
        public async Task<object> Get(int id)
        {
            var model = await _blogArticleServices.GetBlogDetails(id);
            return Ok(new
            {
                success = true,
                data = model
            });
        }



        [HttpGet]
        [Route("DetailNuxtNoPer")]
        public async Task<object> DetailNuxtNoPer(int id)
        {
            var model = await _blogArticleServices.GetBlogDetails(id);
            return Ok(new
            {
                success = true,
                data = model
            });
        }


        /// <summary>
        /// 获取博客测试信息 v2版本
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        ////MVC自带特性 对 api 进行组管理
        //[ApiExplorerSettings(GroupName = "v2")]
        ////路径 如果以 / 开头，表示绝对路径，反之相对 controller 的想u地路径
        //[Route("/api/v2/blog/Blogtest")]

        //和上边的版本控制以及路由地址都是一样的
        [CustomRoute(ApiVersions.V2, "Blogtest")]
        public async Task<object> V2_Blogtest()
        {
            return Ok(new { status = 220, data = "我是第二版的博客信息" });
        }



    }
}
