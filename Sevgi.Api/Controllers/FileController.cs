﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sevgi.Data.Services;

namespace Sevgi.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("file")]
    public class FileController : ControllerBase
    {
        //This is the basic controller protected by authorization.
        //This controller uses base service injection which also uses dapper context to connect to database.
        private readonly ILogger<BaseController> _logger;
        private readonly IBaseService _baseService;

        public FileController(ILogger<BaseController> logger, IBaseService baseService)
        {
            _logger = logger;
            _baseService = baseService;
        }
        [AllowAnonymous]
        [HttpPost("upload-image")]
        public async Task<String> GetTests()
        {

            
            String test = "Deneme";
            return test;
        }
    }
}