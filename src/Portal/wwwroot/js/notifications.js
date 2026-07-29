(function () {
    const badge = document.getElementById("notificationBadgeCount");
    if (!badge) {
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/notifications")
        .withAutomaticReconnect()
        .build();

    // Message notifications collapse server-side (see NotificationService) - the same
    // notification id can be pushed again with updated text/timestamp while it's
    // still unread. Track ids already counted so a resend for the same id doesn't
    // double-increment the badge; a genuinely new id (a fresh notification, or the
    // next one after the previous was read) always does.
    const countedIds = new Set();

    connection.on("ReceiveNotification", (id) => {
        if (countedIds.has(id)) {
            return;
        }

        countedIds.add(id);
        const current = parseInt(badge.textContent, 10) || 0;
        badge.textContent = current + 1;
        badge.classList.remove("d-none");
    });

    connection.start().catch(err => console.error("Notification hub connection failed:", err));
})();
