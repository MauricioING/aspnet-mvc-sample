// Utility function to escape user input
function escapeUserInput(text) {
    var textarea = document.createElement('textarea');
    textarea.textContent = text;
    return textarea.innerHTML;
}

// Initialize Bootstrap custom file input
$(document).ready(function () {
    bsCustomFileInput.init();
});
