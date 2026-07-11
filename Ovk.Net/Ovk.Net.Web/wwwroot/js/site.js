// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("change", function (event) {
    if (event.target && event.target.id === "avatar_file") {
        event.target.closest("form").submit();
    }
});

document.addEventListener("click", function (event) {
    const row = event.target.closest("[data-href]");
    if (row && !event.target.closest("a")) window.location.assign(row.dataset.href);
});

document.addEventListener("keydown", function (event) {
    const row = event.target.closest("[data-href]");
    if (row && (event.key === "Enter" || event.key === " ")) {
        event.preventDefault();
        window.location.assign(row.dataset.href);
    }
});
