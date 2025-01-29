using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ZhihuClone.Core.Interfaces;
using ZhihuClone.Core.Models;
using ZhihuClone.Web.Models.User;
using ZhihuClone.Web.Models.Post;
using System.Linq;
using System.Collections.Generic;
using ZhihuClone.Core.Models.Content;
using ZhihuClone.Core.Models.Search;

namespace ZhihuClone.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IPostService _postService;
        private readonly IAnswerService _answerService;
        private readonly IFollowService _followService;
        private readonly ISearchHistoryService _searchHistoryService;
        private readonly ICollectionService _collectionService;

        public UserController(
            IUserService userService,
            IPostService postService,
            IAnswerService answerService,
            IFollowService followService,
            ISearchHistoryService searchHistoryService,
            ICollectionService collectionService)
        {
            _userService = userService;
            _postService = postService;
            _answerService = answerService;
            _followService = followService;
            _searchHistoryService = searchHistoryService;
            _collectionService = collectionService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var viewModel = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Nickname = user.Nickname,
                Avatar = user.Avatar,
                Bio = user.Bio,
                Location = user.Location,
                Company = user.Company,
                Title = user.Title,
                Website = user.Website,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            ViewData["Title"] = $"{user.UserName}的主页";

            return Ok(viewModel);
        }

        [HttpGet("{id}/posts")]
        public async Task<IActionResult> GetPosts(int id, int page = 1, int pageSize = 10)
        {
            var posts = await _postService.GetUserPostsAsync(id, page, pageSize);
            return Ok(posts);
        }

        [HttpGet("{id}/collections")]
        public async Task<IActionResult> GetCollections(int id, int page = 1, int pageSize = 10)
        {
            var posts = await _postService.GetUserCollectionsAsync(id, page, pageSize);
            return Ok(posts);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpdateViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            if (user.Id != int.Parse(User.Identity?.Name ?? "0"))
                return Forbid();

            user.Nickname = model.Nickname;
            user.Bio = model.Bio;
            user.Location = model.Location;
            user.Company = model.Company;
            user.Title = model.Title;
            user.Website = model.Website;
            user.UpdatedAt = DateTime.UtcNow;

            await _userService.UpdateAsync(user);
            return Ok(user);
        }

        [Authorize]
        [HttpPost("{id}/follow")]
        public async Task<IActionResult> Follow(int id)
        {
            var userId = int.Parse(User.Identity?.Name ?? "0");
            if (userId == 0)
                return Unauthorized();

            var success = await _userService.FollowUserAsync(userId, id);
            return success ? Ok() : BadRequest();
        }

        [Authorize]
        [HttpPost("{id}/unfollow")]
        public async Task<IActionResult> Unfollow(int id)
        {
            var userId = int.Parse(User.Identity?.Name ?? "0");
            if (userId == 0)
                return Unauthorized();

            var success = await _userService.UnfollowUserAsync(userId, id);
            return success ? Ok() : BadRequest();
        }

        [HttpGet("profile/{userId}")]
        public async Task<IActionResult> Profile(int userId)
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            var currentUserId = User.Identity?.IsAuthenticated == true ? 
                int.Parse(User.FindFirst("nameid")?.Value ?? "0") : 0;
            var isFollowing = currentUserId > 0 && await _followService.IsFollowingAsync(currentUserId, userId);
            var followingCount = await _followService.GetFollowingCountAsync(userId);
            var followersCount = await _followService.GetFollowersCountAsync(userId);
            var posts = await _postService.GetUserPostsAsync(userId, 1, 10);
            var postCount = await _postService.GetUserPostCountAsync(userId);
            var answers = await _answerService.GetAnswersByUserIdAsync(userId, 1, 10);
            var collections = await _collectionService.GetUserCollectionsAsync(userId, 1, 10);
            var searchHistories = currentUserId == userId 
                ? await _searchHistoryService.GetUserSearchHistoryAsync(userId, 1, 10)
                : new List<SearchHistory>();

            var viewModel = new UserProfileViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Nickname = user.Nickname,
                Avatar = user.Avatar,
                Bio = user.Bio,
                Location = user.Location,
                Company = user.Company,
                Title = user.Title,
                Website = user.Website,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Occupation = user.Title,
                JoinDate = user.CreatedAt,
                IsCurrentUser = currentUserId == userId,
                IsFollowing = isFollowing,
                FollowingCount = followingCount,
                FollowersCount = followersCount,
                PostCount = postCount,
                Posts = posts,
                Answers = answers,
                Collections = collections,
                SearchHistories = searchHistories
            };

            ViewData["Title"] = $"{user.UserName}的主页";
            return View(viewModel);
        }
    }

    public class UpdateProfileRequest
    {
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? Occupation { get; set; }
        public string Avatar { get; set; } = null!;
    }
} 