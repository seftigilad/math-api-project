/*
 * Math Operations API
 *
 * No description provided (generated by Swagger Codegen https://github.com/swagger-api/swagger-codegen)
 *
 * OpenAPI spec version: 1.0.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using IO.Swagger.Attributes;

using Microsoft.AspNetCore.Authorization;
using IO.Swagger.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace IO.Swagger.Controllers
{ 
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    public class DefaultApiController : ControllerBase
    {

        private readonly IMemoryCache _cache;
        private readonly ILogger<DefaultApiController> _logger;
        private readonly HttpClient _httpClient;

        public DefaultApiController(IMemoryCache cache, ILogger<DefaultApiController> logger, HttpClient httpClient)
        {
            _cache = cache;
            _logger = logger;
            _httpClient = httpClient;
        }
        /// <summary>
        /// Perform a math operation on two numbers
        /// </summary>
        /// <remarks>Accepts two numbers and performs an operation like add, subtract, divide, etc.</remarks>
        /// <param name="body"></param>
        /// <param name="xArithmeticOpID"></param>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        [Authorize]
        [HttpPost]
        [Route("/api/math")]
        [ValidateModelState]
        [SwaggerOperation("ApiMathPost")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse200), description: "Success")]
        public virtual async Task<IActionResult> ApiMathPostAsync([FromBody]ApiMathBody body, [FromHeader(Name = "X-ArithmeticOp-ID")][Required()]string xArithmeticOpID)
        {

            string key = $"{xArithmeticOpID}:{body.X}:{body.Y}";

            if (_cache.TryGetValue(key, out decimal cachedResult))
            {
                _logger.LogInformation("CacheHit");
                return Ok(new { result = cachedResult });
            }
            // Call Mockoon to get operation description
            try
            {
                var response = await _httpClient.GetAsync($"http://localhost:3002/api/meta/{xArithmeticOpID}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var json = JsonDocument.Parse(jsonString);
                    var description = json.RootElement.GetProperty("description").GetString();

                    _logger.LogInformation("Mockoon description: " + description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error calling Mockoon: " + ex.Message);
            }
            // recalculate
            decimal result;
            switch (xArithmeticOpID)
            {
                case "add":
                    result = body.X + body.Y;
                    break;
                case "subtract":
                    result = body.X - body.Y;
                    break;
                case "multiply":
                    result = body.X * body.Y;
                    break;
                case "divide":
                    if (body.Y == 0)
                        return BadRequest("Cannot divide by zero.");
                    result = body.X / body.Y;
                    break;
                default:
                    return BadRequest("Unknown operation");
            }

            // save cache 30 seconds
            _cache.Set(key, result, TimeSpan.FromSeconds(30));

            return Ok(new { result });
        }
    }
}
