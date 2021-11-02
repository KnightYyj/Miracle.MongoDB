﻿using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Miracle.MongoDB.GridFS
{
    public class UploadGridFSInfo
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// BusinessApp与该值至少要有一个.当该值存在时,优先取此值.
        /// </summary>
        public string App { get; set; }
        /// <summary>
        /// Business类型
        /// </summary>
        [Required]
        public string BusinessType { get; set; }
        /// <summary>
        /// 可用于资源中心,记录所属目录id
        /// </summary>
        public string CategoryId { get; set; }
    }

    public class UploadGridFSMulti : UploadGridFSInfo
    {
        /// <summary>
        /// 资源ID
        /// </summary>
        public List<string> DeleteIds { get; set; } = new();
        /// <summary>
        /// 上传文件(单或多文件)
        /// </summary>
        [Required]
        public IFormFileCollection File { get; set; }
    }

    public class UploadGridFSSingle : UploadGridFSInfo
    {
        /// <summary>
        /// 资源ID
        /// </summary>
        public string DeleteId { get; set; }
        /// <summary>
        /// 上传文件(单或多文件)
        /// </summary>
        [Required]
        public IFormFile File { get; set; }
    }
}
