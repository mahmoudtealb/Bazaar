// ============================================
// Unread Messages Notification System
// ============================================

(function () {
    'use strict';

    // Get current user ID from page (will be set by layout)
    const currentUserId = window.currentUserId || null;
    let signalRConnection = null;
    let pollingInterval = null;

    // ============================================
    // Update Badge Count
    // ============================================
    function updateBadges(count) {
        const badge1 = document.getElementById('msgBadge');
        const badge2 = document.getElementById('adminMsgBadge');

        if (badge1) {
            if (count > 0) {
                badge1.innerText = count > 99 ? '99+' : count;
                badge1.style.display = 'inline-block';
            } else {
                badge1.style.display = 'none';
            }
        }

        if (badge2) {
            if (count > 0) {
                badge2.innerText = count > 99 ? '99+' : count;
                badge2.style.display = 'inline-block';
            } else {
                badge2.style.display = 'none';
            }
        }
    }

    // ============================================
    // Refresh Message Count from Server
    // ============================================
    async function refreshMessageCount() {
        try {
            const response = await fetch('/Notifications/GetUnreadCount', {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                console.warn('Failed to fetch unread count');
                return;
            }

            const data = await response.json();
            updateBadges(data.count || 0);
        } catch (error) {
            console.error('Error refreshing message count:', error);
        }
    }

    // ============================================
    // Mark Conversation as Read
    // ============================================
    async function markConversationAsRead(otherUserId) {
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            const response = await fetch('/Notifications/MarkConversationAsRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token || ''
                },
                credentials: 'same-origin',
                body: JSON.stringify({ otherUserId: otherUserId })
            });

            if (response.ok) {
                const data = await response.json();
                if (data.success) {
                    refreshMessageCount();
                }
            }
        } catch (error) {
            console.error('Error marking conversation as read:', error);
        }
    }

    // ============================================
    // SignalR Connection Setup
    // ============================================
    function setupSignalR() {
        if (typeof signalR === 'undefined') {
            console.warn('SignalR not available, using polling fallback');
            startPolling();
            return;
        }

        try {
            signalRConnection = new signalR.HubConnectionBuilder()
                .withUrl("/chathub")
                .withAutomaticReconnect()
                .build();

            // Listen for new unread messages
            signalRConnection.on("NewUnreadMessage", function (payload) {
                if (payload && payload.toUserId && currentUserId) {
                    const toUserId = parseInt(payload.toUserId);
                    const currentUser = parseInt(currentUserId);
                    if (toUserId === currentUser) {
                        refreshMessageCount();
                    }
                }
            });

            // Listen for regular messages (in case we need to update count)
            signalRConnection.on("ReceiveMessage", function (payload) {
                if (payload && payload.receiverId && currentUserId) {
                    const receiverId = parseInt(payload.receiverId);
                    const currentUser = parseInt(currentUserId);
                    if (receiverId === currentUser) {
                        refreshMessageCount();
                    }
                }
            });

            // Start connection
            signalRConnection.start()
                .then(function () {
                    console.log('SignalR connected for notifications');
                    // Initial refresh
                    refreshMessageCount();
                })
                .catch(function (error) {
                    console.warn('SignalR connection failed, using polling fallback:', error);
                    startPolling();
                });

            // Handle reconnection
            signalRConnection.onreconnecting(function () {
                console.log('SignalR reconnecting...');
            });

            signalRConnection.onreconnected(function () {
                console.log('SignalR reconnected');
                refreshMessageCount();
            });

        } catch (error) {
            console.error('Error setting up SignalR:', error);
            startPolling();
        }
    }

    // ============================================
    // Polling Fallback
    // ============================================
    function startPolling() {
        if (pollingInterval) {
            clearInterval(pollingInterval);
        }

        // Initial refresh
        refreshMessageCount();

        // Poll every 5 seconds
        pollingInterval = setInterval(function () {
            refreshMessageCount();
        }, 5000);
    }

    // ============================================
    // Initialize when DOM is ready
    // ============================================
    function init() {
        if (!currentUserId) {
            // User not authenticated, don't initialize
            return;
        }

        // Try SignalR first, fallback to polling
        setupSignalR();

        // Also refresh when page becomes visible (user switches tabs)
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) {
                refreshMessageCount();
            }
        });
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Export functions for use in other scripts
    window.notificationSystem = {
        refreshMessageCount: refreshMessageCount,
        markConversationAsRead: markConversationAsRead,
        updateBadges: updateBadges
    };

})();

