using Microsoft.AspNetCore.Mvc;

// ReSharper disable All

namespace API.API_Clean_Architecture.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase {
	private IConfiguration _Configuration { get; set; }

	public TestController(IConfiguration configuration) {
		_Configuration = configuration;
	}

	[HttpGet]
	public List<string> GetNames() {
		return ["a", "ab", "abc", "abcd",];
	}
}