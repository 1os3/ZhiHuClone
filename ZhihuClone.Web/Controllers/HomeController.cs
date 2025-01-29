using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Web.Models;

namespace ZhihuClone.Web.Controllers;

public class HomeController : Controller
{
    private readonly IPostService _postService;
    private readonly ITopicService _topicService;

    public HomeController(IPostService postService, ITopicService topicService)
    {
        _postService = postService;
        _topicService = topicService;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var posts = await _postService.GetPagedAsync(page, pageSize);
        var hotTopics = await _topicService.GetHotTopicsAsync(10);

        var viewModel = new HomeViewModel
        {
            Posts = posts,
            HotTopics = hotTopics
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
