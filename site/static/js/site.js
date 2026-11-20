// Mobile menu.
(function () {
  var toggle = document.querySelector(".menu-toggle");
  var nav = document.getElementById("topnav");
  if (!toggle || !nav) return;
  toggle.addEventListener("click", function () {
    var open = toggle.getAttribute("aria-expanded") === "true";
    toggle.setAttribute("aria-expanded", String(!open));
    nav.classList.toggle("open", !open);
  });
})();
