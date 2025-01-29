// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Site JavaScript
(function () {
    'use strict';

    // Document ready
    document.addEventListener('DOMContentLoaded', function () {
        console.log('Site initialized');
    });

    // Global functions
    window.app = {
        init: function () {
            this.setupAjaxDefaults();
            this.setupFormValidation();
        },

        setupAjaxDefaults: function () {
            // Setup AJAX defaults if using jQuery
            if (typeof $ !== 'undefined') {
                $.ajaxSetup({
                    headers: {
                        'X-CSRF-TOKEN': document.querySelector('meta[name="csrf-token"]')?.content
                    }
                });
            }
        },

        setupFormValidation: function () {
            // Add form validation if needed
            const forms = document.querySelectorAll('.needs-validation');
            Array.from(forms).forEach(form => {
                form.addEventListener('submit', event => {
                    if (!form.checkValidity()) {
                        event.preventDefault();
                        event.stopPropagation();
                    }
                    form.classList.add('was-validated');
                });
            });
        }
    };

    // Initialize
    window.app.init();
})();

// 全局变量
let isLoading = false;
let currentPage = 1;
let currentTab = 'recommended';
let hasMoreContent = true;

// 无限滚动加载
function initInfiniteScroll() {
    window.addEventListener('scroll', () => {
        if (isLoading || !hasMoreContent) return;

        const scrollHeight = document.documentElement.scrollHeight;
        const scrollTop = window.scrollY || document.documentElement.scrollTop;
        const clientHeight = window.innerHeight || document.documentElement.clientHeight;

        if (scrollTop + clientHeight >= scrollHeight - 100) {
            loadMorePosts();
        }
    });
}

// 加载更多帖子
async function loadMorePosts() {
    try {
        isLoading = true;
        showLoadingSpinner();

        const response = await fetch(`/api/posts?page=${currentPage + 1}&tab=${currentTab}`);
        const data = await response.json();

        if (data.posts.length === 0) {
            hasMoreContent = false;
            showNoMoreContent();
            return;
        }

        appendPosts(data.posts);
        currentPage++;
    } catch (error) {
        console.error('加载更多帖子时出错:', error);
        showLoadError();
    } finally {
        isLoading = false;
        hideLoadingSpinner();
    }
}

// 切换内容标签
function initTabSwitching() {
    document.querySelectorAll('.content-tabs .nav-link').forEach(tab => {
        tab.addEventListener('click', async (e) => {
            e.preventDefault();
            
            if (isLoading) return;

            const newTab = e.target.dataset.tab;
            if (newTab === currentTab) return;

            // 更新UI
            document.querySelector('.content-tabs .nav-link.active').classList.remove('active');
            e.target.classList.add('active');

            // 重置状态
            currentTab = newTab;
            currentPage = 1;
            hasMoreContent = true;
            
            // 加载新内容
            const postsContainer = document.querySelector('.posts-list');
            postsContainer.innerHTML = '';
            await loadMorePosts();
        });
    });
}

// 返回顶部按钮
function initBackToTop() {
    const backToTopBtn = document.createElement('button');
    backToTopBtn.className = 'back-to-top btn btn-primary';
    backToTopBtn.innerHTML = '<i class="bi bi-arrow-up"></i>';
    document.body.appendChild(backToTopBtn);

    window.addEventListener('scroll', () => {
        if (window.scrollY > 300) {
            backToTopBtn.classList.add('show');
        } else {
            backToTopBtn.classList.remove('show');
        }
    });

    backToTopBtn.addEventListener('click', () => {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });
}

// 夜间模式切换
function initDarkMode() {
    const darkModeToggle = document.getElementById('darkModeToggle');
    if (!darkModeToggle) return;

    const isDarkMode = localStorage.getItem('darkMode') === 'true';
    if (isDarkMode) {
        document.body.classList.add('dark-mode');
        darkModeToggle.checked = true;
    }

    darkModeToggle.addEventListener('change', () => {
        document.body.classList.toggle('dark-mode');
        localStorage.setItem('darkMode', darkModeToggle.checked);
    });
}

// 工具函数
function showLoadingSpinner() {
    const spinner = document.createElement('div');
    spinner.className = 'loading-spinner';
    document.querySelector('.posts-list').appendChild(spinner);
}

function hideLoadingSpinner() {
    const spinner = document.querySelector('.loading-spinner');
    if (spinner) spinner.remove();
}

function showNoMoreContent() {
    const message = document.createElement('div');
    message.className = 'text-center text-muted my-4';
    message.textContent = '没有更多内容了';
    document.querySelector('.posts-list').appendChild(message);
}

function showLoadError() {
    const message = document.createElement('div');
    message.className = 'alert alert-danger my-4';
    message.textContent = '加载失败，请稍后重试';
    document.querySelector('.posts-list').appendChild(message);
}

function appendPosts(posts) {
    const postsContainer = document.querySelector('.posts-list');
    posts.forEach(post => {
        const postElement = createPostElement(post);
        postsContainer.appendChild(postElement);
    });
}

function createPostElement(post) {
    const template = document.createElement('template');
    template.innerHTML = `
        <div class="post-card mb-4 p-3 bg-white rounded shadow-sm">
            <div class="post-header d-flex align-items-center mb-3">
                <img src="${post.authorAvatar || '/images/default-avatar.png'}" 
                     class="rounded-circle me-2" 
                     alt="${post.authorName}" 
                     style="width: 40px; height: 40px;" />
                <div>
                    <h6 class="mb-0">${post.authorName}</h6>
                    <small class="text-muted">${new Date(post.createdAt).toLocaleString()}</small>
                </div>
            </div>
            <h5 class="post-title mb-3">
                <a href="/post/${post.id}" class="text-dark text-decoration-none">
                    ${post.title}
                </a>
            </h5>
            <div class="post-content mb-3">
                ${post.content.length > 200 ? post.content.substring(0, 200) + '...' : post.content}
            </div>
            ${createMediaPreview(post.mediaUrls)}
            <div class="post-footer d-flex align-items-center">
                <button class="btn btn-link p-0 me-3 like-btn ${post.isLiked ? 'text-danger' : 'text-dark'}"
                        data-post-id="${post.id}" 
                        data-liked="${post.isLiked}">
                    <i class="bi bi-heart${post.isLiked ? '-fill' : ''}"></i>
                    <span class="like-count">${post.likeCount}</span>
                </button>
                <a href="/post/${post.id}" class="btn btn-link p-0 me-3 text-dark">
                    <i class="bi bi-chat"></i>
                    ${post.commentCount}
                </a>
                <span class="text-muted">
                    <i class="bi bi-eye"></i>
                    ${post.viewCount}
                </span>
            </div>
        </div>
    `;
    return template.content.firstElementChild;
}

function createMediaPreview(mediaUrls) {
    if (!mediaUrls || mediaUrls.length === 0) return '';

    const mediaElements = mediaUrls.slice(0, 3).map(url => {
        if (url.endsWith('.mp4') || url.endsWith('.webm')) {
            return `<video src="${url}" class="img-thumbnail me-2" style="max-width: 200px;" controls></video>`;
        }
        return `<img src="${url}" class="img-thumbnail me-2" style="max-width: 200px;" alt="" />`;
    }).join('');

    const remainingCount = mediaUrls.length > 3 ? `<span class="text-muted">还有 ${mediaUrls.length - 3} 张图片</span>` : '';

    return `
        <div class="post-media mb-3">
            ${mediaElements}
            ${remainingCount}
        </div>
    `;
}

// 初始化
document.addEventListener('DOMContentLoaded', () => {
    initInfiniteScroll();
    initTabSwitching();
    initBackToTop();
    initDarkMode();

    // 初始化所有工具提示
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // 点赞功能
    document.querySelectorAll('.btn-like').forEach(function (button) {
        button.addEventListener('click', async function (e) {
            e.preventDefault();
            const postId = this.dataset.postId;
            try {
                const response = await fetch(`/api/posts/${postId}/like`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });
                if (response.ok) {
                    const data = await response.json();
                    // 更新点赞数
                    this.querySelector('.like-count').textContent = data.likeCount;
                    // 切换点赞状态
                    this.classList.toggle('btn-outline-primary');
                    this.classList.toggle('btn-primary');
                }
            } catch (error) {
                console.error('点赞失败:', error);
            }
        });
    });

    // 收藏功能
    document.querySelectorAll('.btn-collect').forEach(function (button) {
        button.addEventListener('click', async function (e) {
            e.preventDefault();
            const postId = this.dataset.postId;
            try {
                const response = await fetch(`/api/posts/${postId}/collect`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });
                if (response.ok) {
                    const data = await response.json();
                    // 更新收藏数
                    this.querySelector('.collect-count').textContent = data.collectCount;
                    // 切换收藏状态
                    this.classList.toggle('btn-outline-info');
                    this.classList.toggle('btn-info');
                }
            } catch (error) {
                console.error('收藏失败:', error);
            }
        });
    });

    // 关注功能
    document.querySelectorAll('.btn-follow').forEach(function (button) {
        button.addEventListener('click', async function (e) {
            e.preventDefault();
            const userId = this.dataset.userId;
            try {
                const response = await fetch(`/api/users/${userId}/follow`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });
                if (response.ok) {
                    const data = await response.json();
                    // 更新关注状态
                    this.textContent = data.isFollowing ? '取消关注' : '关注';
                    this.classList.toggle('btn-primary');
                    this.classList.toggle('btn-outline-primary');
                }
            } catch (error) {
                console.error('关注失败:', error);
            }
        });
    });

    // 搜索建议
    const searchInput = document.querySelector('.search-input');
    if (searchInput) {
        let timeoutId;
        searchInput.addEventListener('input', function (e) {
            clearTimeout(timeoutId);
            const query = this.value.trim();
            if (query.length < 2) return;

            timeoutId = setTimeout(async function () {
                try {
                    const response = await fetch(`/api/search/suggest?q=${encodeURIComponent(query)}`);
                    if (response.ok) {
                        const suggestions = await response.json();
                        // 更新搜索建议列表
                        updateSearchSuggestions(suggestions);
                    }
                } catch (error) {
                    console.error('获取搜索建议失败:', error);
                }
            }, 300);
        });
    }
});

// 更新搜索建议
function updateSearchSuggestions(suggestions) {
    const suggestionList = document.querySelector('.search-suggestions');
    if (!suggestionList) return;

    suggestionList.innerHTML = '';
    suggestions.forEach(function (suggestion) {
        const item = document.createElement('a');
        item.href = suggestion.url;
        item.className = 'list-group-item list-group-item-action';
        item.innerHTML = `
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="suggestion-title">${suggestion.title}</div>
                    <small class="text-muted">${suggestion.type}</small>
                </div>
                <small class="text-muted">${suggestion.count || ''}</small>
            </div>
        `;
        suggestionList.appendChild(item);
    });
    suggestionList.style.display = 'block';
}

// 关闭搜索建议
document.addEventListener('click', function (e) {
    const suggestionList = document.querySelector('.search-suggestions');
    if (suggestionList && !e.target.closest('.search-container')) {
        suggestionList.style.display = 'none';
    }
});
