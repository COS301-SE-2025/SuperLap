// Help Page JavaScript - Enhanced Mobile Version

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

// Mobile Menu Functionality
function toggleMobileMenu() {
    const mobileNav = document.getElementById('mobileNav');
    const body = document.body;
    
    if (mobileNav.classList.contains('active')) {
        mobileNav.classList.remove('active');
        body.style.overflow = '';
    } else {
        mobileNav.classList.add('active');
        body.style.overflow = 'hidden';
    }
}

function closeMobileMenu() {
    const mobileNav = document.getElementById('mobileNav');
    const body = document.body;
    
    mobileNav.classList.remove('active');
    body.style.overflow = '';
}

// Initialize mobile menu if it doesn't exist
function createMobileMenu() {
    const navbar = document.querySelector('.navbar');
    
    // Add mobile navigation if it doesn't exist
    if (!document.querySelector('.mobile-nav')) {
        const navLinks = document.querySelector('.nav-links');
        const downloadBtn = document.querySelector('.download-btn');
        
        const mobileNav = document.createElement('div');
        mobileNav.id = 'mobileNav';
        mobileNav.className = 'mobile-nav';
        
        mobileNav.innerHTML = `
            <button class="close-mobile-menu" onclick="closeMobileMenu()">×</button>
            ${navLinks.outerHTML}
            ${downloadBtn.outerHTML}
        `;
        
        document.body.appendChild(mobileNav);
    }
}

// Smooth scrolling for TOC links
document.addEventListener('DOMContentLoaded', function() {
    // Create mobile menu
    createMobileMenu();
    
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
                
                // Close mobile menu if open
                closeMobileMenu();
            }
        });
    });

    // Mobile navigation links
    document.addEventListener('click', function(e) {
        if (e.target.matches('.mobile-nav .nav-links a')) {
            closeMobileMenu();
        }
    });

    // Close mobile menu when clicking outside
    document.addEventListener('click', function(e) {
        const mobileNav = document.getElementById('mobileNav');
        const mobileMenuBtn = document.querySelector('.mobile-menu-btn');
        
        if (mobileNav && mobileNav.classList.contains('active') && 
            !mobileNav.contains(e.target) && 
            !mobileMenuBtn.contains(e.target)) {
            closeMobileMenu();
        }
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

    // Handle window resize for mobile responsiveness
    let resizeTimeout;
    window.addEventListener('resize', function() {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(function() {
            // Close mobile menu on resize to desktop
            if (window.innerWidth > 768) {
                closeMobileMenu();
            }
            
            // Adjust image zoom on mobile
            if (window.innerWidth <= 480) {
                images.forEach(img => {
                    if (img.classList.contains('zoomed')) {
                        img.style.transform = 'scale(1.1)';
                    }
                });
            }
        }, 250);
    });

    // Touch gesture support for mobile
    let touchStartY = 0;
    let touchEndY = 0;
    
    document.addEventListener('touchstart', function(e) {
        touchStartY = e.changedTouches[0].screenY;
    }, { passive: true });
    
    document.addEventListener('touchend', function(e) {
        touchEndY = e.changedTouches[0].screenY;
        handleSwipe();
    }, { passive: true });
    
    function handleSwipe() {
        const swipeThreshold = 100;
        const diff = touchStartY - touchEndY;
        
        if (Math.abs(diff) > swipeThreshold) {
            if (diff > 0) {
                // Swipe up - hide back to top button temporarily
                const backToTop = document.getElementById('backToTop');
                if (backToTop && backToTop.classList.contains('show')) {
                    backToTop.style.transform = 'translateY(20px)';
                    setTimeout(() => {
                        backToTop.style.transform = '';
                    }, 1000);
                }
            }
        }
    }

    // Prevent zoom on double tap for better UX
    let lastTouchEnd = 0;
    document.addEventListener('touchend', function(event) {
        const now = (new Date()).getTime();
        if (now - lastTouchEnd <= 300) {
            event.preventDefault();
        }
        lastTouchEnd = now;
    }, false);
});

// Download button functionality
function download() {
    // Direct download from the GitHub release link
    window.location.href = "https://github.com/COS301-SE-2025/SuperLap/releases/download/v1.0.0/SuperLap.Installer.exe";
}

// Print functionality
function printHelp() {
    window.print();
}

// Add keyboard navigation
document.addEventListener('keydown', function(e) {
    // Don't trigger shortcuts when mobile menu is open
    if (document.getElementById('mobileNav')?.classList.contains('active')) {
        if (e.key === 'Escape') {
            closeMobileMenu();
        }
        return;
    }
    
    // Press 'H' to go to top
    if (e.key.toLowerCase() === 'h' && !e.ctrlKey && !e.altKey) {
        scrollToTop();
    }
    
    // Press Escape to close search or mobile menu
    if (e.key === 'Escape') {
        closeMobileMenu();
        closeSearch();
    }
    
    // Press numbers 1-4 to navigate to sections
    const sectionMap = {
        '1': '#login-register',
        '2': '#racing-line', 
        '3': '#gallery',
        '4': '#analytics',
        '5': '#game-integration'
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
            const searchBox = document.getElementById('searchBox');
            const searchInput = document.getElementById('searchInput');
            
            if (window.innerWidth <= 768) {
                searchBox.style.left = '10px';
                searchBox.style.right = '10px';
                searchBox.style.width = 'auto';
                searchInput.style.width = '100%';
                searchInput.style.maxWidth = '200px';
            }
            
            searchBox.style.display = 'block';
            searchInput.focus();
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

// Performance optimizations for mobile
function optimizeForMobile() {
    // Reduce animations on slower devices
    const isLowEnd = navigator.hardwareConcurrency <= 2 || 
                     navigator.deviceMemory <= 2 || 
                     /Android.*Chrome\/[.0-9]*\s(Mobile|eliboM)/i.test(navigator.userAgent);
    
    if (isLowEnd) {
        const style = document.createElement('style');
        style.textContent = `
            *, *::before, *::after {
                animation-duration: 0.1s !important;
                animation-delay: 0s !important;
                transition-duration: 0.1s !important;
                transition-delay: 0s !important;
            }
        `;
        document.head.appendChild(style);
    }
    
    // Lazy load images on mobile
    if ('IntersectionObserver' in window && window.innerWidth <= 768) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    if (img.dataset.src) {
                        img.src = img.dataset.src;
                        img.removeAttribute('data-src');
                        observer.unobserve(img);
                    }
                }
            });
        });
        
        document.querySelectorAll('img[data-src]').forEach(img => {
            imageObserver.observe(img);
        });
    }
}

// Initialize mobile optimizations
document.addEventListener('DOMContentLoaded', optimizeForMobile);

// Debug logging
console.log('SuperLap Help Page loaded successfully!');
console.log('Mobile features enabled');
console.log('Keyboard shortcuts:');
console.log('- Press H to scroll to top');
console.log('- Press 1-4 to navigate to sections');
console.log('- Press Ctrl+F to search');
console.log('- Press Escape to close menus');
console.log('- Click images to zoom in/out');
console.log('- Swipe gestures supported on mobile');