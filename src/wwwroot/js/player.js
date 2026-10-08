(() => {
    let playerIntervalId;
    let timeout = 5000;
    let playerReference;
    let volumeSlider;
    let volumeChangeHandler;

    window.markAppReady = () => {
        document.documentElement.classList.add("app-ready");
    };

    function setCurrentTrackName(value) {
        const div = document.getElementById("NowPlaying");
        if (div && div.innerHTML !== value) {
            div.innerHTML = value;
        }
    }

    function getCurrentTrackName() {
        const player = document.getElementById("audio");
        if (player && player.src && playerReference) {
            playerReference.invokeMethodAsync("GetCurrentTrackNameAsync")
                .then(result => setCurrentTrackName(result || ""))
                .catch(error => {
                    console.error("Error invoking GetCurrentTrackNameAsync:", error);
                    setCurrentTrackName("");
                });
        } else {
            setCurrentTrackName("");
        }
    }

    window.setPageRef = dotNetRef => {
        playerReference = dotNetRef;
    };

    window.initComponent = titleDelay => {
        timeout = titleDelay || timeout;
        if (playerIntervalId) {
            clearInterval(playerIntervalId);
        }
        playerIntervalId = setInterval(getCurrentTrackName, timeout);
    };

    window.cleanupPlayer = () => {
        if (playerIntervalId) {
            clearInterval(playerIntervalId);
            playerIntervalId = null;
        }
        playerReference = null;
        if (volumeSlider && volumeChangeHandler) {
            volumeSlider.removeEventListener("input", volumeChangeHandler);
        }
        volumeSlider = null;
        volumeChangeHandler = null;
    };

    window.initVolumeControl = () => {
        volumeSlider = document.getElementById("volumeSlider");
        if (!volumeSlider) {
            return;
        }

        volumeChangeHandler = () => {
            window.setVolume(volumeSlider.value);
        };
        volumeSlider.addEventListener("input", volumeChangeHandler);
    };

    window.playAudio = isPlaying => {
        const player = document.getElementById("audio");
        if (!player) {
            return;
        }

        if (isPlaying && player.paused) {
            player.play().catch(error => console.error("Error playing audio:", error));
        } else if (!isPlaying && !player.paused) {
            player.pause();
        }
    };

    window.setVolume = volume => {
        const player = document.getElementById("audio");
        if (player) {
            player.volume = Math.max(0, Math.min(1, Number(volume)));
        }
    };
})();
