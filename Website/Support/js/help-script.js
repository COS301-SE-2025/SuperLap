// Help Page JavaScript

// Back to Top Button Functionality
window.addEventListener('scroll', function() {
    const backToTop = document.getElementById('backToTop');
    if (window.pageYOffset > 300) {
        backToTop.classList.add('show');
    } else {
        backToTop.classList.remove('show');
    }
});

function scrollToTop() {
    window.scrollTo({
        top: 0,
        behavior: 'smooth'
    });
}

// Smooth scrolling for TOC links
document.addEventListener('DOMContentLoaded', function() {
    const tocLinks = document.querySelectorAll('.toc-item');
    
    tocLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const targetId = this.getAttribute('href');
            const targetElement = document.querySelector(targetId);
            
            if (targetElement) {
                const headerOffset = 100; // Account for sticky navbar
                const elementPosition = targetElement.getBoundingClientRect().top;
                const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

                window.scrollTo({
                    top: offsetPosition,
                    behavior: 'smooth'
                });
            }
        });
    });

    // Highlight active section in navigation
    const sections = document.querySelectorAll('.help-section');
    const tocItems = document.querySelectorAll('.toc-item');
    
    function highlightActiveSection() {
        let currentSection = '';
        
        sections.forEach(section => {
            const sectionTop = section.getBoundingClientRect().top;
            const sectionHeight = section.offsetHeight;
            
            if (sectionTop <= 150 && sectionTop + sectionHeight > 150) {
                currentSection = section.getAttribute('id');
            }
        });
        
        tocItems.forEach(item => {
            item.classList.remove('active');
            if (item.getAttribute('href') === '#' + currentSection) {
                item.classList.add('active');
            }
        });
    }
    
    // Throttled scroll listener for better performance
    let ticking = false;
    
    function onScroll() {
        if (!ticking) {
            requestAnimationFrame(() => {
                highlightActiveSection();
                ticking = false;
            });
            ticking = true;
        }
    }
    
    window.addEventListener('scroll', onScroll);
    
    // Image zoom functionality
    const images = document.querySelectorAll('.image-container img');
    
    images.forEach(img => {
        img.addEventListener('click', function() {
            if (this.classList.contains('zoomed')) {
                this.classList.remove('zoomed');
                this.style.transform = 'scale(1)';
                this.style.cursor = 'zoom-in';
                this.style.zIndex = 'auto';
            } else {
                this.classList.add('zoomed');
                this.style.transform = 'scale(1.5)';
                this.style.cursor = 'zoom-out';
                this.style.zIndex = '1000';
                this.style.position = 'relative';
                this.style.transition = 'transform 0.3s ease';
            }
        });
        
        // Set initial cursor
        img.style.cursor = 'zoom-in';
    });
    
    // Add loading animation for images
    images.forEach(img => {
        img.addEventListener('load', function() {
            this.style.opacity = '1';
            this.style.transform = 'translateY(0)';
        });
        
        // Set initial state
        img.style.opacity = '0';
        img.style.transform = 'translateY(20px)';
        img.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
    });
    
    // Copy to clipboard functionality for links
    const linkElements = document.querySelectorAll('.req-link');
    
    linkElements.forEach(link => {
        link.addEventListener('click', function(e) {
            // Allow normal link behavior, but add a subtle animation
            this.style.transform = 'scale(0.95)';
            setTimeout(() => {
                this.style.transform = 'scale(1)';
            }, 150);
        });
    });
    
    // Add intersection observer for scroll animations
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -100px 0px'
    };
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in');
            }
        });
    }, observerOptions);
    
    // Observe sections and step containers
    const elementsToObserve = document.querySelectorAll('.help-section, .step-container, .subsection');
    elementsToObserve.forEach(element => {
        observer.observe(element);
    });
});

// Add CSS for fade-in animation
const style = document.createElement('style');
style.textContent = `
    .fade-in {
        animation: fadeInUp 0.6s ease forwards;
    }
    
    @keyframes fadeInUp {
        from {
            opacity: 0;
            transform: translateY(30px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    
    .help-section,
    .step-container,
    .subsection {
        opacity: 0;
        transform: translateY(30px);
        transition: opacity 0.6s ease, transform 0.6s ease;
    }
    
    .toc-item.active {
        background: #db1f1f !important;
        transform: translateY(-5px);
        border-color: #f27a69;
    }
    
    .image-container img {
        transition: transform 0.3s ease, z-index 0.3s ease;
    }
    
    .req-link {
        transition: transform 0.15s ease, color 0.3s ease;
    }
`;
document.head.appendChild(style);

// Download button functionality
function download() {
    // Direct download from the GitHub release link
    window.location.href = "https://github.com/COS301-SE-2025/SuperLap/releases/download/v1.0.0.alpha/SuperLap.Installer.exe";
}
// Print functionality
function printHelp() {
    window.print();
}

// Add keyboard navigation
document.addEventListener('keydown', function(e) {
    // Press 'H' to go to top
    if (e.key.toLowerCase() === 'h' && !e.ctrlKey && !e.altKey) {
        scrollToTop();
    }
    
    // Press numbers 1-4 to navigate to sections
    const sectionMap = {
        '1': '#login-register',
        '2': '#racing-line', 
        '3': '#gallery-analytics',
        '4': '#game-integration'
    };
    
    if (sectionMap[e.key] && !e.ctrlKey && !e.altKey) {
        const targetElement = document.querySelector(sectionMap[e.key]);
        if (targetElement) {
            const headerOffset = 100;
            const elementPosition = targetElement.getBoundingClientRect().top;
            const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

            window.scrollTo({
                top: offsetPosition,
                behavior: 'smooth'
            });
        }
    }
});

// Add search functionality (basic)
function addSearchFunctionality() {
    const searchContainer = document.createElement('div');
    searchContainer.innerHTML = `
        <div style="position: fixed; top: 100px; right: 20px; z-index: 1001; background: #25292C; padding: 10px; border-radius: 10px; box-shadow: 0 4px 20px rgba(0,0,0,0.5); display: none;" id="searchBox">
            <input type="text" id="searchInput" placeholder="Search help content..." style="padding: 8px; border: none; border-radius: 5px; background: #161a1d; color: white; width: 250px;">
            <button onclick="closeSearch()" style="background: #db1f1f; border: none; color: white; padding: 8px 12px; border-radius: 5px; margin-left: 5px; cursor: pointer;">×</button>
        </div>
    `;
    document.body.appendChild(searchContainer);
    
    // Add search trigger (Ctrl+F)
    document.addEventListener('keydown', function(e) {
        if (e.ctrlKey && e.key === 'f') {
            e.preventDefault();
            document.getElementById('searchBox').style.display = 'block';
            document.getElementById('searchInput').focus();
        }
        
        if (e.key === 'Escape') {
            document.getElementById('searchBox').style.display = 'none';
        }
    });
    
    // Search functionality
    document.getElementById('searchInput').addEventListener('input', function(e) {
        const searchTerm = e.target.value.toLowerCase();
        const sections = document.querySelectorAll('.help-section');
        
        sections.forEach(section => {
            const content = section.textContent.toLowerCase();
            if (searchTerm && content.includes(searchTerm)) {
                section.style.outline = '2px solid #db1f1f';
            } else {
                section.style.outline = 'none';
            }
        });
    });
}

function closeSearch() {
    document.getElementById('searchBox').style.display = 'none';
    document.querySelectorAll('.help-section').forEach(section => {
        section.style.outline = 'none';
    });
}

// Initialize search functionality
addSearchFunctionality();

console.log('SuperLap Help Page loaded successfully!');
console.log('Keyboard shortcuts:');
console.log('- Press H to scroll to top');
console.log('- Press 1-4 to navigate to sections');
console.log('- Press Ctrl+F to search');
console.log('- Click images to zoom in/out');