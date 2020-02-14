// Utility function to escape user input
function escapeUserInput(text) {
    var textarea = document.createElement('textarea');
    textarea.textContent = text;
    return textarea.innerHTML;
}