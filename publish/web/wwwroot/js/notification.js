// 通知功能
let notificationHub = null;
let unreadCount = 0;

// 初始化SignalR连接
async function initNotificationHub() {
    notificationHub = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .build();

    // 处理接收到的通知
    notificationHub.on("ReceiveNotification", (notification) => {
        addNotification(notification);
        updateUnreadCount(1);
        showNotificationToast(notification);
    });

    // 处理标记已读
    notificationHub.on("MarkAsRead", (notificationId) => {
        markNotificationAsRead(notificationId);
        updateUnreadCount(-1);
    });

    // 处理标记全部已读
    notificationHub.on("MarkAllAsRead", () => {
        markAllNotificationsAsRead();
        resetUnreadCount();
    });

    try {
        await notificationHub.start();
        console.log("已连接到通知中心");
        loadNotifications();
    } catch (err) {
        console.error("连接通知中心失败:", err);
    }
}

// 加载通知列表
async function loadNotifications() {
    try {
        const response = await fetch('/api/notifications');
        const data = await response.json();
        
        if (data.success) {
            renderNotifications(data.notifications);
            updateUnreadCount(data.unreadCount);
        }
    } catch (error) {
        console.error('加载通知失败:', error);
    }
}

// 渲染通知列表
function renderNotifications(notifications) {
    const notificationList = document.querySelector('.notification-list');
    const noNotifications = document.querySelector('.no-notifications');
    
    if (!notifications || notifications.length === 0) {
        notificationList.innerHTML = '';
        noNotifications.classList.remove('d-none');
        return;
    }

    noNotifications.classList.add('d-none');
    notificationList.innerHTML = notifications.map(notification => createNotificationElement(notification)).join('');
}

// 创建通知元素
function createNotificationElement(notification) {
    const timeAgo = getTimeAgo(new Date(notification.createdAt));
    const unreadClass = notification.isRead ? '' : 'unread';
    
    return `
        <div class="notification-item p-3 border-bottom ${unreadClass}" data-id="${notification.id}">
            <div class="d-flex">
                <img src="${notification.senderAvatar || '/images/default-avatar.png'}" 
                     class="rounded-circle me-2" 
                     alt="${notification.senderName}"
                     style="width: 40px; height: 40px;">
                <div class="flex-grow-1">
                    <div class="notification-content">
                        <a href="${notification.link}" class="text-decoration-none">
                            <span class="fw-bold">${notification.senderName}</span>
                            ${notification.content}
                        </a>
                    </div>
                    <small class="text-muted">${timeAgo}</small>
                </div>
                ${!notification.isRead ? `
                    <button class="btn btn-link btn-sm text-decoration-none mark-read" 
                            onclick="markAsRead(${notification.id}, event)">
                        标记已读
                    </button>
                ` : ''}
            </div>
        </div>
    `;
}

// 添加新通知
function addNotification(notification) {
    const notificationList = document.querySelector('.notification-list');
    const noNotifications = document.querySelector('.no-notifications');
    
    noNotifications.classList.add('d-none');
    const notificationElement = createNotificationElement(notification);
    notificationList.insertAdjacentHTML('afterbegin', notificationElement);
}

// 更新未读数量
function updateUnreadCount(delta) {
    unreadCount = Math.max(0, unreadCount + delta);
    const badge = document.querySelector('.notification-badge');
    
    if (unreadCount > 0) {
        badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
        badge.classList.remove('d-none');
    } else {
        badge.classList.add('d-none');
    }
}

// 重置未读数量
function resetUnreadCount() {
    unreadCount = 0;
    const badge = document.querySelector('.notification-badge');
    badge.classList.add('d-none');
}

// 标记通知为已读
async function markAsRead(notificationId, event) {
    event?.preventDefault();
    
    try {
        const response = await fetch(`/api/notifications/${notificationId}/read`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        
        if (response.ok) {
            markNotificationAsRead(notificationId);
            updateUnreadCount(-1);
        }
    } catch (error) {
        console.error('标记通知已读失败:', error);
    }
}

// 标记所有通知为已读
async function markAllAsRead() {
    try {
        const response = await fetch('/api/notifications/read-all', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        
        if (response.ok) {
            markAllNotificationsAsRead();
            resetUnreadCount();
        }
    } catch (error) {
        console.error('标记所有通知已读失败:', error);
    }
}

// 在UI中标记通知为已读
function markNotificationAsRead(notificationId) {
    const notification = document.querySelector(`.notification-item[data-id="${notificationId}"]`);
    if (notification) {
        notification.classList.remove('unread');
        const markReadBtn = notification.querySelector('.mark-read');
        if (markReadBtn) {
            markReadBtn.remove();
        }
    }
}

// 在UI中标记所有通知为已读
function markAllNotificationsAsRead() {
    document.querySelectorAll('.notification-item.unread').forEach(notification => {
        notification.classList.remove('unread');
        const markReadBtn = notification.querySelector('.mark-read');
        if (markReadBtn) {
            markReadBtn.remove();
        }
    });
}

// 显示通知提醒
function showNotificationToast(notification) {
    const toast = document.createElement('div');
    toast.className = 'notification-toast';
    toast.innerHTML = `
        <div class="toast-header">
            <img src="${notification.senderAvatar || '/images/default-avatar.png'}" 
                 class="rounded me-2" 
                 alt="${notification.senderName}"
                 style="width: 20px; height: 20px;">
            <strong class="me-auto">${notification.senderName}</strong>
            <small class="text-muted">刚刚</small>
            <button type="button" class="btn-close" onclick="this.parentElement.parentElement.remove()"></button>
        </div>
        <div class="toast-body">
            ${notification.content}
        </div>
    `;
    
    document.body.appendChild(toast);
    setTimeout(() => {
        toast.classList.add('show');
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 5000);
    }, 100);
}

// 获取相对时间
function getTimeAgo(date) {
    const seconds = Math.floor((new Date() - date) / 1000);
    
    let interval = Math.floor(seconds / 31536000);
    if (interval > 1) return interval + ' 年前';
    
    interval = Math.floor(seconds / 2592000);
    if (interval > 1) return interval + ' 个月前';
    
    interval = Math.floor(seconds / 86400);
    if (interval > 1) return interval + ' 天前';
    
    interval = Math.floor(seconds / 3600);
    if (interval > 1) return interval + ' 小时前';
    
    interval = Math.floor(seconds / 60);
    if (interval > 1) return interval + ' 分钟前';
    
    if(seconds < 10) return '刚刚';
    
    return Math.floor(seconds) + ' 秒前';
}

// 初始化事件监听
document.addEventListener('DOMContentLoaded', () => {
    // 初始化通知中心连接
    initNotificationHub();
    
    // 标记全部已读按钮
    const markAllReadBtn = document.querySelector('.mark-all-read');
    if (markAllReadBtn) {
        markAllReadBtn.addEventListener('click', markAllAsRead);
    }
}); 