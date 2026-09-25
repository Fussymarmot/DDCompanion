// JS interop helpers for the Blazor UI.

window.submitCharacterForm = () => {
    document.getElementById("character-form")?.requestSubmit();
};

window.blazorReady = () => {};
