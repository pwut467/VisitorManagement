(function () {
  function setRevealed(root, revealed) {
    var text = root.querySelector(".national-id-text");
    var icon = root.querySelector(".national-id-toggle i");
    var button = root.querySelector(".national-id-toggle");
    if (!text || !icon || !button) return;

    if (revealed) {
      text.textContent = root.getAttribute("data-full") || "";
      icon.classList.remove("bi-eye");
      icon.classList.add("bi-eye-slash");
      button.title = "ซ่อนเลขบัตรประชาชน";
      button.setAttribute("aria-label", "ซ่อนเลขบัตรประชาชน");
      root.dataset.revealed = "1";
    } else {
      text.textContent = root.getAttribute("data-masked") || "";
      icon.classList.remove("bi-eye-slash");
      icon.classList.add("bi-eye");
      button.title = "แสดงเลขบัตรประชาชน";
      button.setAttribute("aria-label", "แสดงเลขบัตรประชาชน");
      root.dataset.revealed = "0";
    }
  }

  document.addEventListener("click", function (event) {
    var button = event.target.closest(".national-id-toggle");
    if (!button) return;
    var root = button.closest(".national-id-reveal");
    if (!root) return;
    event.preventDefault();
    setRevealed(root, root.dataset.revealed !== "1");
  });
})();
