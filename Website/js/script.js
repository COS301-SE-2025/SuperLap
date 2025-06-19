function download() {
  // Placeholder download action
  alert("Download started!");
  // You can also trigger an actual file download like this:
  // window.location.href = 'path/to/your/file.pdf';
}

// Show/hide the back-to-top button on scroll
window.addEventListener("scroll", () => {
  const button = document.getElementById("backToTop");
  if (window.scrollY > 300) {
    button.classList.add("show");
  } else {
    button.classList.remove("show");
  }
});

// Scroll smoothly to the top
function scrollToTop() {
  window.scrollTo({
    top: 0,
    behavior: "smooth"
  });
}

function updateCountdown() {
  // Set your target date and time here (Year, Month (0-11), Day, Hour, Minute, Second)
  const targetDate = new Date("2025-09-29T00:00:00");
  const now = new Date();
  const diff = targetDate - now;

  if (diff <= 0) {
    // If countdown is over
    document.querySelector('.countdown-timer').innerHTML = "<span>🎉 Project Complete</span>";
    return;
  }

  const days = Math.floor(diff / (1000 * 60 * 60 * 24));
  const hours = Math.floor((diff / (1000 * 60 * 60)) % 24);
  const minutes = Math.floor((diff / (1000 * 60)) % 60);
  const seconds = Math.floor((diff / 1000) % 60);

  document.getElementById("days").textContent = String(days).padStart(2, '0');
  document.getElementById("hours").textContent = String(hours).padStart(2, '0');
  document.getElementById("minutes").textContent = String(minutes).padStart(2, '0');
  document.getElementById("seconds").textContent = String(seconds).padStart(2, '0');
}

// Update every second
setInterval(updateCountdown, 1000);
updateCountdown(); // initial run

