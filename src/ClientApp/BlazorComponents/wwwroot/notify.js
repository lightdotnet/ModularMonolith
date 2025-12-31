async function playAudio(playerName) {
    var audioPlayer = document.getElementById(playerName);

    if (audioPlayer == null)
        return;

    audioPlayer.play();
}

async function pauseAudio(playerName) {
    var audioPlayer = document.getElementById(playerName);

    if (audioPlayer == null)
        return;

    audioPlayer.pause();
}

async function requestNotificationPermission() {
    await Notification.requestPermission().then(function (result) {
        if (result === 'granted') {
            console.warn('Notification permission granted.');
        } else {
            console.warn('Notification permission denied.');
        }
    });
}

window.notify = async function (title, options) {
    if (!("Notification" in window)) {
        console.warn("This browser does not support notifications.");
        return;
    }

    // Check current permission
    if (Notification.permission === "granted") {
        new Notification(title, options);
    } else if (Notification.permission !== "denied") {
        // Ask for permission
        const result = await Notification.requestPermission();
        if (result === "granted") {
            new Notification(title, options);
        } else {
            console.warn("Notification permission denied or dismissed.");
        }
    } else {
        console.warn("Notifications denied permanently.");
    }
};

window.setAppBadge = async function (value) {
    if ('setAppBadge' in navigator) {
        if (value) {
            await navigator.setAppBadge(value); // number
        } else {
            await navigator.setAppBadge(); // red dot only
        }
        alert("App badge set to " + value);
    } else {
        console.warn("Badging API not supported.");
    }
};

window.clearAppBadge = async function () {
    if ('clearAppBadge' in navigator) {
        await navigator.clearAppBadge();
    }
};