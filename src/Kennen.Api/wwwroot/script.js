(function () {
  'use strict';

  const navbar = document.querySelector('.navbar');
  const navToggle = document.querySelector('.nav-toggle');
  const navMenu = document.querySelector('.nav-menu');
  const skipLink = document.querySelector('.skip-link');
  const contactForm = document.querySelector('.contact-form form');

  // Analytics helper. A no-op when GA is unconfigured, still loading, or blocked by the
  // visitor, so tracking can never break the surrounding feature.
  const track = (name, params) => {
    if (typeof window.gtag === 'function') window.gtag('event', name, params || {});
  };

  // Skip-link focus target handling
  if (skipLink) {
    skipLink.addEventListener('click', (e) => {
      const main = document.getElementById('main-content');
      if (main) {
        e.preventDefault();
        main.setAttribute('tabindex', '-1');
        main.focus();
        main.addEventListener('blur', () => main.removeAttribute('tabindex'), { once: true });
      }
    });
  }

  // Mobile menu toggle
  if (navToggle && navMenu) {
    navToggle.addEventListener('click', () => {
      const isOpen = navMenu.classList.toggle('active');
      navToggle.setAttribute('aria-expanded', String(isOpen));
      navToggle.setAttribute('aria-label', isOpen ? 'Close navigation menu' : 'Open navigation menu');
    });

    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && navMenu.classList.contains('active')) {
        navMenu.classList.remove('active');
        navToggle.setAttribute('aria-expanded', 'false');
        navToggle.setAttribute('aria-label', 'Open navigation menu');
        navToggle.focus();
      }
    });
  }

  // Smooth scrolling for navigation and CTA links
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', (e) => {
      const targetId = anchor.getAttribute('href');
      if (targetId === '#') return;
      const target = document.querySelector(targetId);
      if (!target) return;
      e.preventDefault();

      const headerOffset = navbar ? navbar.offsetHeight + 16 : 0;
      const offsetPosition = target.getBoundingClientRect().top + window.pageYOffset - headerOffset;

      if (navMenu && navMenu.classList.contains('active')) {
        navMenu.classList.remove('active');
        navToggle.setAttribute('aria-expanded', 'false');
      }

      window.scrollTo({
        top: offsetPosition,
        behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth'
      });

      target.setAttribute('tabindex', '-1');
      target.focus({ preventScroll: true });
      target.addEventListener('blur', () => target.removeAttribute('tabindex'), { once: true });
    });
  });

  // Navbar scrolled state
  window.addEventListener('scroll', () => {
    if (navbar) navbar.classList.toggle('scrolled', window.scrollY > 50);
  });

  // Form validation with accessible status, submitting to the Kennen API
  if (contactForm) {
    const nameInput = contactForm.querySelector('#name');
    const emailInput = contactForm.querySelector('#email');
    const companyInput = contactForm.querySelector('#company');
    const messageInput = contactForm.querySelector('#message');
    const formStatus = contactForm.querySelector('#form-status');
    const submitButton = contactForm.querySelector('button[type="submit"]');
    const apiBaseUrl = (window.KENNEN_CONFIG && window.KENNEN_CONFIG.apiBaseUrl) || '';

    const setStatus = (text, variant) => {
      if (!formStatus) return;
      formStatus.textContent = text;
      formStatus.className = variant ? `form-status form-status--${variant}` : 'form-status';
    };

    contactForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      setStatus('', null);

      [nameInput, emailInput, messageInput].forEach(input => {
        if (input) input.removeAttribute('aria-invalid');
      });

      const errors = [];
      if (!nameInput || !nameInput.value.trim()) {
        errors.push('Please enter your name.');
        if (nameInput) nameInput.setAttribute('aria-invalid', 'true');
      }

      if (!emailInput || !emailInput.value.trim()) {
        errors.push('Please enter your email.');
        if (emailInput) emailInput.setAttribute('aria-invalid', 'true');
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(emailInput.value.trim())) {
        errors.push('Please enter a valid email address.');
        if (emailInput) emailInput.setAttribute('aria-invalid', 'true');
      }

      // The API requires at least 10 characters, so mirror that here to avoid a round-trip.
      if (!messageInput || !messageInput.value.trim()) {
        errors.push('Please enter your message.');
        if (messageInput) messageInput.setAttribute('aria-invalid', 'true');
      } else if (messageInput.value.trim().length < 10) {
        errors.push('Please provide a little more detail in your message.');
        messageInput.setAttribute('aria-invalid', 'true');
      }

      if (errors.length) {
        setStatus(errors.join(' '), 'error');
        track('form_validation_error', { form_name: 'contact', error_count: errors.length });
        return;
      }

      track('form_submit', { form_name: 'contact' });

      const originalButtonText = submitButton ? submitButton.textContent : '';
      if (submitButton) {
        submitButton.disabled = true;
        submitButton.textContent = 'Sending…';
      }
      setStatus('Sending your message…', null);

      try {
        const response = await fetch(`${apiBaseUrl}/api/contact`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            name: nameInput.value.trim(),
            email: emailInput.value.trim(),
            company: companyInput && companyInput.value.trim() ? companyInput.value.trim() : null,
            message: messageInput.value.trim(),
            source: 'website-contact'
          })
        });

        if (response.ok) {
          setStatus('Thank you for your message. Our team will get back to you shortly.', 'success');
          // GA4 recommended event for a completed enquiry - shows up under Conversions once marked as one.
          track('generate_lead', { form_name: 'contact', source: 'website-contact' });
          contactForm.reset();
          return;
        }

        track('form_error', { form_name: 'contact', status: response.status });

        if (response.status === 429) {
          setStatus('You have sent several messages already. Please wait a few minutes before trying again.', 'error');
          return;
        }

        // ASP.NET Core returns a ProblemDetails / ValidationProblemDetails body.
        const problem = await response.json().catch(() => null);
        const fieldErrors = problem && problem.errors
          ? Object.values(problem.errors).flat()
          : [];

        setStatus(
          fieldErrors.length
            ? fieldErrors.join(' ')
            : 'We could not send your message. Please email contact@kennen-technologies.com instead.',
          'error'
        );
      } catch (error) {
        // Network failure or CORS rejection - never leave the visitor without a route to us.
        setStatus('We could not reach our servers. Please email contact@kennen-technologies.com instead.', 'error');
        track('form_error', { form_name: 'contact', status: 'network' });
      } finally {
        if (submitButton) {
          submitButton.disabled = false;
          submitButton.textContent = originalButtonText;
        }
      }
    });

    [nameInput, emailInput, messageInput].forEach(input => {
      if (input) {
        input.addEventListener('input', () => {
          if (input.hasAttribute('aria-invalid')) {
            input.removeAttribute('aria-invalid');
          }
        });
      }
    });
  }

  // Highlight the section currently in view
  const navLinks = Array.from(document.querySelectorAll('.nav-menu > li > a[href^="#"]'));
  const sectionsById = navLinks
    .map(link => document.querySelector(link.getAttribute('href')))
    .filter(Boolean);

  if (sectionsById.length) {
    const sectionObserver = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (!entry.isIntersecting) return;
        navLinks.forEach(link => {
          link.classList.toggle('is-active', link.getAttribute('href') === '#' + entry.target.id);
        });
      });
    }, { rootMargin: '-45% 0px -50% 0px' });

    sectionsById.forEach(section => sectionObserver.observe(section));
  }

  // Scroll reveal with staggered children and reduced-motion support
  const revealTargets = [
    ...document.querySelectorAll('.section-head'),
    ...document.querySelectorAll('.grid-reveal > *'),
    ...document.querySelectorAll('.about-content, .hero-panel, .ai-framework-intro, .contact-content')
  ];

  if (matchMedia('(prefers-reduced-motion: reduce)').matches) {
    revealTargets.forEach(el => el.classList.add('reveal', 'is-visible'));
  } else {
    revealTargets.forEach(el => {
      el.classList.add('reveal');
      const siblings = el.parentElement ? Array.from(el.parentElement.children) : [];
      const index = siblings.indexOf(el);
      el.style.setProperty('--reveal-delay', Math.min(index, 5) * 70 + 'ms');
    });

    const revealObserver = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-visible');
          revealObserver.unobserve(entry.target);
        }
      });
    }, { threshold: 0.15, rootMargin: '0px 0px -60px 0px' });

    revealTargets.forEach(el => revealObserver.observe(el));
  }

  // CTA and contact-link click tracking. Delegated so it also covers links added later.
  document.addEventListener('click', (e) => {
    const link = e.target.closest('a');
    if (!link) return;

    const href = link.getAttribute('href') || '';
    const label = link.textContent.trim().slice(0, 100);

    if (href.startsWith('mailto:')) {
      track('contact_click', { method: 'email', link_text: label });
    } else if (href.startsWith('tel:')) {
      track('contact_click', { method: 'phone', link_text: label });
    } else if (link.classList.contains('btn')) {
      track('cta_click', {
        link_text: label,
        link_url: href,
        location: link.closest('section, nav, footer')?.className.split(' ')[0] || 'unknown'
      });
    }
  });

  // Counter animation for stats
  const statsSection = document.querySelector('.stats');
  if (statsSection && !matchMedia('(prefers-reduced-motion: reduce)').matches) {
    const statsObserver = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          statsSection.querySelectorAll('.stat-number').forEach(stat => {
            const raw = stat.textContent.trim();
            const target = parseInt(raw, 10);
            if (!isNaN(target)) animateCounter(stat, target, raw.replace(/^[\d.,]+/, ''), 1600);
          });
          statsObserver.unobserve(entry.target);
        }
      });
    }, { threshold: 0.4 });

    statsObserver.observe(statsSection);
  }

  function animateCounter(element, target, suffix, duration) {
    const start = performance.now();
    const step = (now) => {
      const progress = Math.min((now - start) / duration, 1);
      // ease-out cubic
      const eased = 1 - Math.pow(1 - progress, 3);
      element.textContent = Math.round(target * eased) + suffix;
      if (progress < 1) requestAnimationFrame(step);
    };
    element.textContent = '0' + suffix;
    requestAnimationFrame(step);
  }
})();
