﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using ImageUploadDemo.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ImageUploadDemo.Controllers
{

    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private ConfigHelper _config;
        private readonly IHubContext<ImageHub> _hubContext;

        public ImageController(ConfigHelper config, IHubContext<ImageHub> hubContext)
        {
            _config = config;
            _hubContext = hubContext;
        }


        [HttpPost]
        public async Task<IActionResult> Post(List<IFormFile> files)
        {
            if (files.Count == 0) return BadRequest("No files in request. Aborted.");

            try
            {
                var result = new List<BlobUploadModel>();

                foreach (var formFile in files)
                {
                    if (formFile.Length > 0)
                    {
                        var fileName = $"{formFile.FileName}___{Guid.NewGuid()}";

                        // Retrieve reference to a blob
                        var blobContainer = await BlobHelper.GetBlobContainer(_config);
                        var blob = blobContainer.GetBlockBlobReference(fileName);

                        // Set the blob content type
                        blob.Properties.ContentType = formFile.ContentType;


                        var filePath = Path.GetTempFileName();
                        {
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await formFile.CopyToAsync(stream);
                            }
                        }

                        var newFilePath = Path.GetTempFileName();
                        try
                        {
                            using (var fs = new FileStream(filePath, FileMode.Open))
                            {
                                var img = new MagickImage(fs);
                                img.AutoOrient();   // Fix orientation
                                img.Strip();        // remove all EXIF information
                                img.Write(newFilePath);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }


                        // Upload file into blob storage, basically copying it from local disk into Azure
                        using (var fs = new FileStream(newFilePath, FileMode.Open))
                        {
                            await blob.UploadFromStreamAsync(fs);
                        }

                        // Delete local file from disk
                        //File.Delete(fileData.LocalFileName);

                        // Create blob upload model with properties from blob info
                        var blobUpload = new BlobUploadModel
                        {
                            FileName = blob.Name,
                            FileUrl = blob.Uri.AbsoluteUri,
                            FileSizeInBytes = blob.Properties.Length
                        };

                        // Add uploaded blob to the list
                        result.Add(blobUpload);
                    }
                }

                await _hubContext.Clients.All.SendAsync("UploadedImage", result.Select(e => e.FileUrl).ToList());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return BadRequest(e.Message);
            }
            
            return Ok(true);
        }


        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var container = await BlobHelper.GetBlobContainer(_config);
            await container.CreateIfNotExistsAsync();

            var list = container.ListBlobsSegmentedAsync(new BlobContinuationToken()).Result.Results;

            return Ok(list.Select(e => e.Uri).ToList());
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string imageUrl)
        {
            try
            {
                var container = await BlobHelper.GetBlobContainer(_config);
                await container.CreateIfNotExistsAsync();

                var list = container.ListBlobsSegmentedAsync(new BlobContinuationToken()).Result.Results;

                var listOfBlobUrls = list.Select(e => e.Uri.AbsoluteUri.ToString()).ToList();

                var htmlImageUrl = imageUrl.Replace(" ", "%20");

                var imageUrlToDelete = listOfBlobUrls.FirstOrDefault(e => e.Equals(htmlImageUrl));

                if (string.IsNullOrEmpty(imageUrlToDelete)) return BadRequest("Image not found");


                imageUrlToDelete = imageUrlToDelete.Replace("%20", " ");
                var blobName = imageUrlToDelete.Split(@"/")[4];
                await BlobHelper.DeleteBlob(blobName, _config);

                var filtered = listOfBlobUrls.Where(e => e != imageUrlToDelete).ToList();

                return Ok(filtered.Select(e => new Uri(e)).ToList());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return BadRequest(e.Message);
            }
        }

    }
}
