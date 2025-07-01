using IO.Swagger;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace TestMath
{
    public class MathApiTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly HttpClient _client;

        public MathApiTests(WebApplicationFactory<Startup> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Divide_ByZero_ReturnsBadRequest()
        {
            var token = GenerateJwtToken();
            var response = await _client.SendAsync(CreatePostRequest(5, 0, "divide",token));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        }

        [Fact]
        public async Task Cache_StoresAndRetrievesResult()
        {
            var token = GenerateJwtToken();
            // First request = cache miss
            var first = await _client.SendAsync(CreatePostRequest(3, 2, "add", token));
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
            var firstResult = firstJson.RootElement.GetProperty("result").GetDecimal();

            // Second request = should be cache hit
            var second = await _client.SendAsync(CreatePostRequest(3, 2, "add", token));
            var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
            var secondResult = secondJson.RootElement.GetProperty("result").GetDecimal();

            Assert.Equal(firstResult, secondResult);
        }
        [Fact]
        public async Task RequestWithoutToken_ReturnsUnauthorized()
        {
            var request = CreatePostRequest(1, 1, "add");
            // no token
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RequestWithValidToken_ReturnsOk()
        {
            string token = GenerateJwtToken(); // create HS256 token with exp
            var request = CreatePostRequest(1, 2, "add", token);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private StringContent CreateRequestBody(string operation, decimal x, decimal y)
        {
            var body = new
            {
                operation = operation,
                x = x,
                y = y
            };
            return new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        private HttpRequestMessage CreatePostRequest(decimal x, decimal y, string op, string token = null)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/math");
            req.Content = CreateRequestBody(op, x, y);  // Pass operation here
            req.Headers.Add("X-ArithmeticOp-ID", op);

            if (!string.IsNullOrEmpty(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return req;
        }
        private string GenerateJwtToken()
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("P#nM7!sD8uA^2yJkXqL0*Tf9ZvR4%WqE")); // use your real key
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "yourapp",
                audience: "yourapp",
                claims: new[] { new Claim("sub", "test") },
                expires: DateTime.UtcNow.AddMinutes(5000000),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}