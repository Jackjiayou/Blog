﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blog.Core.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImgController : Controller
    {
        // GET: api/Img
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Img/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }

        // GET: api/Download
        [HttpGet]
        [Route("down")]
        public FileStreamResult DownImg()
        {
            var addrUrl = Directory.GetCurrentDirectory() + "\\微信截图_20190304212953.png";
            var stream = System.IO.File.OpenRead(addrUrl);
            string fileExt = ".jpg";  // 这里可以写一个获取文件扩展名的方法，获取扩展名
            //获取文件的ContentType
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            var memi = provider.Mappings[fileExt];
            var fileName = Path.GetFileName(addrUrl);


            return File(stream, memi, fileName);
        }


        [HttpPost]
        [Route("Pic")]
        public async Task<MessageModel<string>> InsertPicture([FromServices]IHostingEnvironment environment)
        {
            var data = new MessageModel<string>();
            string path = string.Empty;
            string foldername = "images";
            var files = Request.Form.Files;
            if (files == null || !files.Any()) { data.msg = "请选择上传的文件。"; return data; }
            //格式限制
            var allowType = new string[] { "image/jpg", "image/png", "image/jpeg" };

            string folderpath = Path.Combine(environment.WebRootPath, foldername);
            if (!System.IO.Directory.Exists(folderpath))
            {
                System.IO.Directory.CreateDirectory(folderpath);
            }

            if (files.Any(c => allowType.Contains(c.ContentType)))
            {
                if (files.Sum(c => c.Length) <= 1024 * 1024 * 4)
                {
                    //foreach (var file in files)
                    var file = files.FirstOrDefault();
                    string strpath = Path.Combine(foldername, DateTime.Now.ToString("MMddHHmmss") + file.FileName);
                    path = Path.Combine(environment.WebRootPath, strpath);

                    using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        await file.CopyToAsync(stream);
                    }

                    data = new MessageModel<string>()
                    {
                        response = strpath,
                        msg = "上传成功",
                        success = true,
                    };
                    return data;
                }
                else
                {
                    data.msg = "图片过大";
                    return data;
                }
            }
            else

            {
                data.msg = "图片格式错误";
                return data;
            }
        }


        // POST: api/Img
        [HttpPost]
        public void Post([FromBody] object formdata)
        {
        }

        // PUT: api/Img/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }

}
