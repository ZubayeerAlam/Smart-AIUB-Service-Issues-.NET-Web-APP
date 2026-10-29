// Ask before submitting any form that has a data-confirm attribute
// (used by the Activate/Deactivate buttons). The text is read from the
// attribute, never built into JavaScript, so names with quotes are safe.
document.addEventListener("submit", function (event) {
    const message = event.target.dataset ? event.target.dataset.confirm : null;
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});
