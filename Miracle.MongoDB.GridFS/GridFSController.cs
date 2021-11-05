﻿using Microsoft.AspNetCore.Mvc;
using Miracle.Common;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Miracle.MongoDB.GridFS
{
    [ApiController]
    [Route("[controller]")]
    public class GridFSController : ControllerBase
    {
        private readonly GridFSBucket bucket;
        private readonly IMongoCollection<GridFSItemInfo> Coll;
        private readonly FilterDefinitionBuilder<GridFSItemInfo> _bf = Builders<GridFSItemInfo>.Filter;
        public GridFSController(GridFSBucket bucket, IMongoCollection<GridFSItemInfo> collection)
        {
            this.bucket = bucket;
            Coll = collection;
        }

        /// <summary>
        /// 获取已上传文件列表
        /// </summary>
        /// <param name="info">关键字支持:文件名,用户名,用户ID,App名称以及业务名称模糊匹配</param>
        /// <returns></returns>
        [HttpPost("Infos")]
        public async Task<object> Infos(KeywordPageInfo info)
        {
            var f = _bf.Empty;
            if (!string.IsNullOrEmpty(info.Keyword)) f &= _bf.Or(_bf.Where(c => c.FileName.Contains(info.Keyword)), _bf.Where(c => c.UserName.Contains(info.Keyword)), _bf.Where(c => c.UserId.Contains(info.Keyword)), _bf.Where(c => c.App.Contains(info.Keyword)), _bf.Where(c => c.BusinessType.Contains(info.Keyword)));
            var total = await Coll.CountDocumentsAsync(f);
            var list = await Coll.FindAsync(f, new()
            {
                Sort = Builders<GridFSItemInfo>.Sort.Descending(c => c.CreatTime),
                Limit = info.Size,
                Skip = (info.Index - 1) * info.Size
            }).Result.ToListAsync();
            return PageResult.Wrap(total, list);
        }

        /// <summary>
        /// 添加一个或多个文件
        /// </summary>
        /// <param name="dto">UploadGridFS</param>
        /// <returns></returns>
        [HttpPost("UploadMulti")]
        public async Task<IEnumerable<GridFSItem>> PostMulti([FromForm] UploadGridFSMulti fs)
        {
            if (string.IsNullOrWhiteSpace(fs.BusinessType)) throw new("BusinessType can not be null");
            if (fs.File is null || fs.File.Count == 0) throw new("no files find");
            if (fs.DeleteIds.Count > 0) foreach (var did in fs.DeleteIds) await bucket.DeleteAsync(ObjectId.Parse(did));
            var rsList = new List<GridFSItem>();
            var infos = new List<GridFSItemInfo>();
            foreach (var item in fs.File)
            {
                if (item.ContentType is null) throw new("ContentType in File is null");
                var bapp = !string.IsNullOrWhiteSpace(fs.App) ? fs.App : GridFSExtensions.BusinessApp;
                if (string.IsNullOrWhiteSpace(bapp)) throw new("BusinessApp can't be null");
                var metadata = new Dictionary<string, object> { { "contentType", item.ContentType }, { "app", bapp }, { "creator", new { fs.UserId, fs.UserName }.ToBsonDocument() } };
                if (!string.IsNullOrWhiteSpace(fs.BusinessType)) metadata.Add("business", fs.BusinessType);
                if (!string.IsNullOrWhiteSpace(fs.CategoryId)) metadata.Add("category", fs.CategoryId);
                var upo = new GridFSUploadOptions
                {
                    BatchSize = fs.File.Count,
                    Metadata = new(metadata)
                };
                var oid = await bucket.UploadFromStreamAsync(item.FileName, item.OpenReadStream(), upo);
                rsList.Add(new()
                {
                    FileId = oid.ToString(),
                    FileName = item.FileName,
                    Length = item.Length,
                    ContentType = item.ContentType
                });
                infos.Add(new()
                {
                    FileId = oid.ToString(),
                    FileName = item.FileName,
                    Length = item.Length,
                    ContentType = item.ContentType,
                    UserId = fs.UserId,
                    UserName = fs.UserName,
                    App = fs.App,
                    BusinessType = fs.BusinessType,
                    CategoryId = fs.CategoryId,
                    CreatTime = DateTime.Now
                });
            }
            await Coll.InsertManyAsync(infos);
            return rsList;
        }

        /// <summary>
        /// 添加一个或多个文件
        /// </summary>
        /// <param name="dto">UploadGridFS</param>
        /// <returns></returns>
        [HttpPost("UploadSingle")]
        public async Task<GridFSItem> PostSingle([FromForm] UploadGridFSSingle fs)
        {
            if (string.IsNullOrWhiteSpace(fs.BusinessType)) throw new("BusinessType can not be null");
            if (fs.File is null) throw new("no files find");
            if (!string.IsNullOrEmpty(fs.DeleteId)) await bucket.DeleteAsync(ObjectId.Parse(fs.DeleteId));
            if (fs.File.ContentType is null) throw new("ContentType in File is null");
            var bapp = !string.IsNullOrWhiteSpace(fs.App) ? fs.App : GridFSExtensions.BusinessApp;
            if (string.IsNullOrWhiteSpace(bapp)) throw new("BusinessApp can't be null");
            var metadata = new Dictionary<string, object> { { "contentType", fs.File.ContentType }, { "app", bapp }, { "creator", new { fs.UserId, fs.UserName }.ToBsonDocument() } };
            if (!string.IsNullOrWhiteSpace(fs.BusinessType)) metadata.Add("business", fs.BusinessType);
            if (!string.IsNullOrWhiteSpace(fs.CategoryId)) metadata.Add("category", fs.CategoryId);
            var upo = new GridFSUploadOptions
            {
                BatchSize = 1,
                Metadata = new(metadata)
            };
            var oid = await bucket.UploadFromStreamAsync(fs.File.FileName, fs.File.OpenReadStream(), upo);
            await Coll.InsertOneAsync(new()
            {
                FileId = oid.ToString(),
                FileName = fs.File.FileName,
                Length = fs.File.Length,
                ContentType = fs.File.ContentType,
                UserId = fs.UserId,
                UserName = fs.UserName,
                App = fs.App,
                BusinessType = fs.BusinessType,
                CategoryId = fs.CategoryId,
                CreatTime = DateTime.Now
            });
            return new()
            {
                FileId = oid.ToString(),
                FileName = fs.File.FileName,
                Length = fs.File.Length,
                ContentType = fs.File.ContentType
            };
        }

        /// <summary>
        /// 下载
        /// </summary>
        /// <param name="id">文件ID</param>
        /// <returns></returns>
        [HttpGet("Download/{id}")]
        public async Task<FileStreamResult> Download(string id)
        {
            var stream = await bucket.OpenDownloadStreamAsync(ObjectId.Parse(id), new() { Seekable = true });
            return File(stream, stream.FileInfo.Metadata["contentType"].AsString, stream.FileInfo.Filename);
        }

        /// <summary>
        /// 打开文件内容
        /// </summary>
        /// <param name="id">文件ID</param>
        /// <returns></returns>
        [HttpGet("FileContent/{id}")]
        public async Task<FileContentResult> FileContent(string id)
        {
            var fi = await bucket.Find("{_id:ObjectId('" + id + "')}").SingleOrDefaultAsync() ?? throw new("no data find");
            var bytes = await bucket.DownloadAsBytesAsync(ObjectId.Parse(id), new GridFSDownloadOptions() { Seekable = true });
            return File(bytes, fi.Metadata["contentType"].AsString, fi.Filename);
        }

        /// <summary>
        /// 重命名文件
        /// </summary>
        /// <param name="id">文件ID</param>
        /// <param name="newname">新名称</param>
        /// <returns></returns>
        [HttpPut("{id}/Rename/{newname}")]
        public async Task Rename(string id, string newname)
        {
            await bucket.RenameAsync(ObjectId.Parse(id), newname);
            _ = await Coll.UpdateOneAsync(c => c.FileId == id, Builders<GridFSItemInfo>.Update.Set(c => c.FileName, newname));
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="id">文件ID</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            await bucket.DeleteAsync(ObjectId.Parse(id));
            _ = await Coll.DeleteOneAsync(c => c.FileId == id);
        }

        /// <summary>
        /// 批量删除文件
        /// </summary>
        /// <param name="ids">文件ID集合</param>
        /// <returns></returns>
        [HttpDelete("DeleteMulti")]
        public async Task DeleteMulti(string[] ids)
        {
            foreach (var id in ids)
            {
                await bucket.DeleteAsync(ObjectId.Parse(id));
            }
            _ = await Coll.DeleteManyAsync(c => ids.Contains(c.FileId));
        }
    }
}
