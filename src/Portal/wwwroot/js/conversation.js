(function () {
    const conversationId = window.portalConversationId;
    const currentUserId = window.portalCurrentUserId;

    const log = document.getElementById("messageLog");
    const form = document.getElementById("sendForm");
    const input = document.getElementById("messageBody");

    // Built with DOM methods, not innerHTML - senderDisplayName and body are
    // user-supplied text and must never be interpreted as markup.
    function appendMessage(senderDisplayName, body, sentAt, isMine) {
        const li = document.createElement("li");
        li.className = "list-group-item" + (isMine ? " list-group-item-primary" : "");

        const header = document.createElement("div");
        const strong = document.createElement("strong");
        strong.textContent = senderDisplayName;
        const time = document.createElement("span");
        time.className = "text-muted ms-2";
        time.textContent = new Date(sentAt).toLocaleString();
        header.appendChild(strong);
        header.appendChild(time);

        const bodyDiv = document.createElement("div");
        bodyDiv.textContent = body;

        li.appendChild(header);
        li.appendChild(bodyDiv);
        log.appendChild(li);
        log.scrollTop = log.scrollHeight;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/conversations")
        .withAutomaticReconnect()
        .build();

    // Every surface (Show page, popup) renders exclusively from this event, never by
    // also appending locally on submit - the hub broadcasts to the whole group
    // including the sender, so appending on submit as well would double the
    // sender's own message.
    connection.on("ReceiveMessage", (senderDisplayName, senderId, body, sentAt) => {
        appendMessage(senderDisplayName, body, sentAt, senderId === currentUserId);
    });

    let connected = false;
    connection.start()
        .then(() => {
            connected = true;
            return connection.invoke("JoinConversation", conversationId);
        })
        .catch(err => console.error("SignalR connection failed:", err));

    // If the hub connection never came up, let the form submit normally - the plain
    // HTTP SendMessage action still works, just without the live update.
    form.addEventListener("submit", (event) => {
        if (!connected) {
            return;
        }

        event.preventDefault();
        const body = input.value;
        if (!body) {
            return;
        }

        connection.invoke("SendMessage", conversationId, body)
            .then(() => { input.value = ""; })
            .catch(err => console.error("SendMessage failed:", err));
    });
})();
