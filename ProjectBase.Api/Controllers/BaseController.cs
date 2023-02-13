using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectBase.Data.Services;

namespace ProjectBase.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("test")]
    public class BaseController : ControllerBase
    {
        //This is the basic controller protected by authorization.
        //This controller uses base service injection which also uses dapper context to connect to database.
        private readonly ILogger<BaseController> _logger;
        private readonly IBaseService _baseService;

        public BaseController(ILogger<BaseController> logger, IBaseService baseService)
        {
            _logger = logger;
            _baseService = baseService;
        }

        [HttpGet("get-all")]
        public async Task<IEnumerable<int>> GetTests()
        {
            var tests = await _baseService.GetNumber();
            return tests;
        }
    }
}