//------------------------------------------------------------------------- Download Button Functionality
function download() {
    // Placeholder download action
    alert("❌ Download unavailable at the Moment. But we're working on it!");
    // You can also trigger an actual file download like this:
    // window.location.href = 'path/to/your/file.pdf';
}

//------------------------------------------------------------------------- Back to Top Button Functionality
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

//------------------------------------------------------------------------- Countdown Timer Functionality
function updateCountdown() {
    // (Year, Month (0-11), Day, Hour, Minute, Second)
    const targetDate = new Date("2025-09-29T00:00:00");
    const now = new Date();
    const diff = targetDate - now;

    if (diff <= 0) { // Need to update this later - but for now this is what it does when it's run out of time
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

//------------------------------------------------------------------------- About button

const container = document.querySelector('.box-container');
const rightButton = document.querySelector('.right-button');
const leftButton = document.querySelector('.left-button');

rightButton.addEventListener('click', () => {
    container.classList.add('right-open');  // Open right panel
});

leftButton.addEventListener('click', () => {
    container.classList.remove('right-open'); // Close right panel
});

//------------------------------------------------------------------------- Send Eamil

const form = document.getElementById("contact_form");
const popup = document.getElementById("popup");

form.addEventListener("submit", function (e) {
    e.preventDefault(); // prevent default submit

    const formData = new FormData(form);

    fetch(form.action, {
        method: "POST",
        body: formData,
        headers: {
            'Accept': 'application/json'
        }
    })
        .then(response => {
            if (response.ok) {
                showPopup("✅ Message sent successfully!");
                form.reset(); // Clear the form
            } else {
                showPopup("❌ Oops! Something went wrong.");
            }
        })
        .catch(error => {
            showPopup("❌ Network error. Try again later.");
        });
});

function showPopup(message) {
    popup.textContent = message;
    popup.style.display = "block";
    setTimeout(() => {
        popup.style.display = "none";
    }, 4000);
}

//------------------------------------------------------------------------- Slider code

// Right button (starts the slider)
document.querySelector('.right-button').addEventListener('click', function () {
    document.querySelector('.box-container').classList.add('middle-1-open');
    document.querySelector('.box-container').classList.remove('middle-2-open', 'middle-3-open', 'middle-4-open', 'right-open');
});

// Slide 1: Back to home (removes all)
document.querySelector('.middle-1-left').addEventListener('click', function () {
    document.querySelector('.box-container').classList.remove('middle-1-open', 'middle-2-open', 'middle-3-open', 'middle-4-open', 'right-open');
});

// Slide 1: Forward to Slide 2
document.querySelector('.middle-1-right').addEventListener('click', function () {
    document.querySelector('.box-container').classList.add('middle-2-open');
    document.querySelector('.box-container').classList.remove('middle-1-open', 'middle-3-open', 'middle-4-open', 'right-open');
});

// Slide 2: Back to Slide 1
document.querySelector('.middle-2-left').addEventListener('click', function () {
    document.querySelector('.box-container').classList.add('middle-1-open');
    document.querySelector('.box-container').classList.remove('middle-2-open', 'middle-3-open', 'middle-4-open', 'right-open');
});

// Slide 2: Forward to Slide 3
document.querySelector('.middle-2-right').addEventListener('click', function () {
    document.querySelector('.box-container').classList.add('middle-3-open');
    document.querySelector('.box-container').classList.remove('middle-1-open', 'middle-2-open', 'middle-4-open', 'right-open');
});

// Slide 3: Back to Slide 2
document.querySelector('.middle-3-left').addEventListener('click', function () {
    document.querySelector('.box-container').classList.add('middle-2-open');
    document.querySelector('.box-container').classList.remove('middle-1-open', 'middle-3-open', 'middle-4-open', 'right-open');
});

// Slide 3: Forward to Slide 4 (FIXED - was going to right-open)
document.querySelector('.middle-3-right').addEventListener('click', function () {
    document.querySelector('.box-container').classList.add('middle-4-open');
    document.querySelector('.box-container').classList.remove('middle-1-open', 'middle-2-open', 'middle-3-open', 'right-open');
});

// Slide 4: Back to Slide 3
document.querySelector('.middle-4-left').addEventListener('click', function () {
    document.querySelector('.box-container').classList.add('middle-3-open');
    document.querySelector('.box-container').classList.remove('middle-1-open', 'middle-2-open', 'middle-4-open', 'right-open');
});

// Slide 4: Forward to right-open (final slide)
document.querySelector('.middle-4-right').addEventListener('click', function () {
    document.querySelector('.box-container').classList.add('right-open');
    document.querySelector('.box-container').classList.remove('middle-1-open', 'middle-2-open', 'middle-3-open', 'middle-4-open');
});

// Left button (from final slide, goes back to Slide 4)
document.querySelector('.left-button').addEventListener('click', function () {
    document.querySelector('.box-container').classList.add('middle-4-open');
    document.querySelector('.box-container').classList.remove('middle-1-open', 'middle-2-open', 'middle-3-open', 'right-open');
});