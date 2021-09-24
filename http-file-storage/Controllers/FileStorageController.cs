using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using torrent_client;

namespace file_storage.Controllers
{
    [Route("storage")]
    [ApiController]
    public class FileStorageController : ControllerBase
    {
        private readonly ILogger<FileStorageController> _logger;
        private readonly string _path = @"C:\Storage";

        public FileStorageController(ILogger<FileStorageController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [HttpGet("{*filename}")]
        public ActionResult GetFile(string filename)
        {
            if (filename == null)
            {
                filename = "";
            }
            if (isFile(filename))
            {
                try
                {
                    string path = Path.Combine(_path, filename);
                    FileStream file = new FileStream(path, FileMode.Open);
                    return File(file, "application/unknown", Path.GetFileName(filename));
                }
                catch
                {
                    return StatusCode(500);
                }
            }
            else
            {
                string directoryname = filename;
                try
                {
                    IReadOnlyCollection<string> files = FileSystem.GetFiles(Path.Combine(_path, directoryname));
                    IReadOnlyCollection<string> directories = FileSystem.GetDirectories(Path.Combine(_path, directoryname));
                    List<Element> content = new List<Element>();
                    foreach (var item in directories)
                    {
                        content.Add(new Element(Path.GetFileName(item), "Folder"));
                    }
                    foreach (var item in files)
                    {
                        content.Add(new Element(Path.GetFileName(item), "File"));
                    }
                    return new JsonResult(content, new JsonSerializerOptions { });
                }
                catch
                {
                    return NotFound();
                }

            }
        }

        private bool isFile(string str)
        {
            if (System.IO.File.Exists(Path.Combine(_path, str)))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        [HttpHead("{*filename}")]
        public ActionResult GetFileInfo(string filename)
        {
            try
            {
                string Path = FileSystem.GetFileInfo(System.IO.Path.Combine(_path, filename)).ToString();
                FileInfo fileInfo = new FileInfo(Path);
                if (fileInfo.Exists)
                {
                    Response.Headers.Add("Name", fileInfo.Name);
                    Response.Headers.Add("Path", fileInfo.DirectoryName);
                    Response.Headers.Add("Size", SizeConverter.FormatBytes(fileInfo.Length));
                    Response.Headers.Add("Creation-date", fileInfo.CreationTime.ToString());
                    Response.Headers.Add("Changed-date", fileInfo.LastWriteTime.ToString());
                    return Ok();
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpDelete("{*filename}")]
        public ActionResult DeleteFile(string filename)
        {
            try
            {
                if (isFile(filename))
                {
                    FileSystem.DeleteFile(Path.Combine(_path, filename));
                }
                else
                {
                    FileSystem.DeleteDirectory(Path.Combine(_path, filename), DeleteDirectoryOption.DeleteAllContents);
                }
                return Ok();
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPut("{*path}")]
        public ActionResult Put(string path)
        {
            if (path == null)
            {
                path = "";
            }
            IFormFileCollection formFiles;
            try
            {
                formFiles = Request.Form.Files;
            }
            catch
            {
                formFiles = null;
            }
            bool isCopyFile = Request.Headers.ContainsKey("COPY");
            string pathToFile;
            if (!isCopyFile && formFiles != null)
            {
                return UploadFiles(formFiles, path);
            }
            else if (isCopyFile)
            {
                pathToFile = Request.Headers["COPY"];
                return CopyFile(pathToFile, path);
            }
            else
            {
                return BadRequest();
            }
        }

        private ActionResult UploadFiles(IFormFileCollection Files, string path)
        {
            int count = 0;
            string pathTo = Path.Combine(_path, path);
            CreateDirectory(pathTo);

            foreach (var file in Files)
            {
                try
                {
                    using (var fileStream = new FileStream(Path.Combine(pathTo, file.FileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    count++;
                }
                catch { }
            }
            if (count == 0)
            {
                return StatusCode(500);
            }
            else if (count != Files.Count)
            {
                return Ok();
            }
            else
            {
                return Ok();
            }
        }

        private ActionResult CopyFile(string From, string To)
        {
            string pathFrom = Path.Combine(_path, From);
            string pathTo = Path.Combine(_path, To);
            CreateDirectory(pathTo);
            if (isFile(pathFrom))
            {
                try
                {
                    using (var fileStream = new FileStream(pathTo, FileMode.Create))
                    {
                        using (var fromCopyStream = new FileStream(pathFrom, FileMode.Open))
                        {
                            fromCopyStream.CopyTo(fileStream);
                        }
                    }
                }
                catch
                {
                    return StatusCode(500);
                }
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        private void CreateDirectory(string pathToFile)
        {
            string SavePath = Path.GetDirectoryName(pathToFile);
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }
        }
    }
}
