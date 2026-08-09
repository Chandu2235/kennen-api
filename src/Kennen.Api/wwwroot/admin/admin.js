(function () {
  'use strict';

  // The server (or inline config) sets the API base for preview tunnels;
  // default is same-origin relative paths for production deployments.
  const API = window.__KENNEN_API_BASE__ || '';
  let accessToken = localStorage.getItem('kennen_access_token');
  let refreshToken = localStorage.getItem('kennen_refresh_token');
  let tokenExpires = parseInt(localStorage.getItem('kennen_token_expires') || '0', 10);
  let currentUser = JSON.parse(localStorage.getItem('kennen_user') || 'null');

  const state = {
    page: 'dashboard',
    leads: { page: 1, status: '', search: '' },
    applications: { page: 1, search: '' }
  };

  // ---------------------------------------------------------------------------
  // HTTP helpers
  // ---------------------------------------------------------------------------
  async function api(path, options = {}) {
    const headers = { 'Content-Type': 'application/json' };
    if (accessToken) headers.Authorization = 'Bearer ' + accessToken;

    let response = await fetch(API + path, {
      ...options,
      headers: { ...headers, ...options.headers },
      body: options.body && typeof options.body === 'object' && !(options.body instanceof FormData)
        ? JSON.stringify(options.body)
        : options.body
    });

    if (response.status === 401 && refreshToken) {
      const refresh = await fetch(API + '/api/auth/refresh', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken })
      });

      if (!refresh.ok) {
        logout();
        throw new Error('Session expired. Please log in again.');
      }

      const data = await refresh.json();
      setTokens(data);

      return api(path, options);
    }

    if (!response.ok) {
      const problem = await response.json().catch(() => ({}));
      throw new Error(problem.detail || problem.title || 'Request failed');
    }

    const contentType = response.headers.get('Content-Type') || '';
    if (contentType.includes('application/json')) {
      return response.json();
    }
    return response;
  }

  function setTokens(data) {
    accessToken = data.accessToken;
    refreshToken = data.refreshToken;
    currentUser = data.user;
    tokenExpires = new Date(data.accessTokenExpiresAtUtc).getTime();
    localStorage.setItem('kennen_access_token', accessToken);
    localStorage.setItem('kennen_refresh_token', refreshToken);
    localStorage.setItem('kennen_user', JSON.stringify(currentUser));
    localStorage.setItem('kennen_token_expires', tokenExpires.toString());
  }

  function logout() {
    if (refreshToken) {
      fetch(API + '/api/auth/logout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + (accessToken || '') },
        body: JSON.stringify({ refreshToken })
      }).catch(() => {});
    }
    localStorage.removeItem('kennen_access_token');
    localStorage.removeItem('kennen_refresh_token');
    localStorage.removeItem('kennen_user');
    localStorage.removeItem('kennen_token_expires');
    accessToken = null; refreshToken = null; currentUser = null;
    showLogin();
  }

  // ---------------------------------------------------------------------------
  // UI navigation
  // ---------------------------------------------------------------------------
  function showLogin() {
    document.getElementById('login-view').style.display = '';
    document.getElementById('admin-view').style.display = 'none';
  }

  function showAdmin() {
    document.getElementById('login-view').style.display = 'none';
    document.getElementById('admin-view').style.display = '';
    document.getElementById('user-name').textContent = currentUser?.fullName || 'Admin';
    navigate('dashboard');
  }

  function navigate(page) {
    state.page = page;
    document.querySelectorAll('nav a').forEach(a => a.classList.toggle('active', a.dataset.page === page));
    document.querySelectorAll('.page').forEach(p => p.style.display = 'none');
    document.getElementById('page-' + page).style.display = '';
    document.getElementById('page-title').textContent = page.charAt(0).toUpperCase() + page.slice(1);

    if (page === 'dashboard') loadDashboard();
    if (page === 'leads') loadLeads();
    if (page === 'content') loadContent();
    if (page === 'careers') loadJobs();
    if (page === 'applications') loadApplications();
  }

  function setGlobalError(text) {
    document.getElementById('global-error').textContent = text;
    setTimeout(() => { document.getElementById('global-error').textContent = ''; }, 6000);
  }

  function openModal(html, onClose) {
    const modal = document.getElementById('modal');
    const body = document.getElementById('modal-body');
    body.innerHTML = html;
    modal.style.display = '';
    body.querySelector('.close')?.addEventListener('click', () => { modal.style.display = 'none'; if (onClose) onClose(); });
  }

  document.getElementById('modal').addEventListener('click', e => {
    if (e.target.id === 'modal') e.target.style.display = 'none';
  });

  // ---------------------------------------------------------------------------
  // Login
  // ---------------------------------------------------------------------------
  document.getElementById('login-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value;
    document.getElementById('login-error').textContent = '';

    try {
      const data = await api('/api/auth/login', { method: 'POST', body: { email, password } });
      setTokens(data);
      showAdmin();
    } catch (err) {
      document.getElementById('login-error').textContent = err.message;
    }
  });

  document.getElementById('logout').addEventListener('click', logout);

  document.querySelectorAll('nav a').forEach(a => {
    a.addEventListener('click', (e) => { e.preventDefault(); navigate(a.dataset.page); });
  });

  // ---------------------------------------------------------------------------
  // Dashboard
  // ---------------------------------------------------------------------------
  async function loadDashboard() {
    try {
      const [leads, jobs, apps] = await Promise.all([
        api('/api/admin/leads?PageSize=1'),
        api('/api/admin/careers/jobs'),
        api('/api/admin/careers/applications?PageSize=1')
      ]);
      document.getElementById('dashboard-cards').innerHTML = `
        <div class="card"><h3>${leads.totalCount}</h3><p>Leads</p></div>
        <div class="card"><h3>${jobs.length}</h3><p>Job postings</p></div>
        <div class="card"><h3>${apps.totalCount}</h3><p>Applications</p></div>`;
    } catch (err) { setGlobalError(err.message); }
  }

  // ---------------------------------------------------------------------------
  // Leads
  // ---------------------------------------------------------------------------
  async function loadLeads() {
    const qs = new URLSearchParams({ Page: state.leads.page, PageSize: '25' });
    if (state.leads.status) qs.set('Status', state.leads.status);
    if (state.leads.search) qs.set('Search', state.leads.search);

    try {
      const data = await api('/api/admin/leads?' + qs.toString());
      const body = document.getElementById('leads-body');
      body.innerHTML = data.items.map(l => `
        <tr>
          <td>${escape(l.name)}</td>
          <td>${escape(l.email)}</td>
          <td>${escape(l.company) || '—'}</td>
          <td><span class="badge ${l.status}">${l.status}</span></td>
          <td>${new Date(l.createdAtUtc).toLocaleString()}</td>
          <td>
            <button class="btn-secondary" data-id="${l.id}" data-action="view-lead">View</button>
            <button class="btn-secondary" data-id="${l.id}" data-action="edit-lead">Edit</button>
          </td>
        </tr>`).join('');

      body.querySelectorAll('button[data-action="view-lead"]').forEach(b => b.addEventListener('click', () => viewLead(b.dataset.id)));
      body.querySelectorAll('button[data-action="edit-lead"]').forEach(b => b.addEventListener('click', () => editLead(b.dataset.id)));

      renderPagination('leads-paging', data, (p) => { state.leads.page = p; loadLeads(); });
    } catch (err) { setGlobalError(err.message); }
  }

  async function viewLead(id) {
    try {
      const lead = await api('/api/admin/leads/' + id);
      openModal(`
        <h2>Lead from ${escape(lead.name)}</h2>
        <p><strong>Email:</strong> ${escape(lead.email)}</p>
        <p><strong>Company:</strong> ${escape(lead.company) || '—'}</p>
        <p><strong>Source:</strong> ${escape(lead.source)}</p>
        <p><strong>Status:</strong> <span class="badge ${lead.status}">${lead.status}</span></p>
        <p><strong>Received:</strong> ${new Date(lead.createdAtUtc).toLocaleString()}</p>
        <label>Message</label>
        <textarea rows="6" readonly>${escape(lead.message)}</textarea>
        ${lead.internalNotes ? `<label>Internal notes</label><textarea rows="4" readonly>${escape(lead.internalNotes)}</textarea>` : ''}
        <div class="actions"><button class="btn-secondary close">Close</button></div>`);
    } catch (err) { setGlobalError(err.message); }
  }

  async function editLead(id) {
    try {
      const lead = await api('/api/admin/leads/' + id);
      openModal(`
        <h2>Triage lead</h2>
        <p>${escape(lead.name)} &lt;${escape(lead.email)}&gt;</p>
        <label for="lead-status-edit">Status</label>
        <select id="lead-status-edit">${leadStatusOptions(lead.status)}</select>
        <label for="lead-notes-edit">Internal notes</label>
        <textarea id="lead-notes-edit" rows="4">${escape(lead.internalNotes || '')}</textarea>
        <div class="actions">
          <button class="btn-primary" id="save-lead">Save</button>
          <button class="btn-secondary close">Cancel</button>
        </div>`);

      document.getElementById('save-lead').addEventListener('click', async () => {
        const payload = {
          status: document.getElementById('lead-status-edit').value,
          internalNotes: document.getElementById('lead-notes-edit').value
        };
        try {
          await api('/api/admin/leads/' + id, { method: 'PATCH', body: payload });
          document.getElementById('modal').style.display = 'none';
          loadLeads();
        } catch (err) { setGlobalError(err.message); }
      });
    } catch (err) { setGlobalError(err.message); }
  }

  function leadStatusOptions(selected) {
    const statuses = ['New','Contacted','Qualified','Won','Lost','Spam'];
    return statuses.map(s => `<option value="${s}" ${s === selected ? 'selected' : ''}>${s}</option>`).join('');
  }

  document.getElementById('lead-search').addEventListener('input', debounce(() => { state.leads.search = document.getElementById('lead-search').value; state.leads.page = 1; loadLeads(); }, 300));
  document.getElementById('lead-status').addEventListener('change', () => { state.leads.status = document.getElementById('lead-status').value; state.leads.page = 1; loadLeads(); });
  document.getElementById('lead-refresh').addEventListener('click', () => loadLeads());

  // ---------------------------------------------------------------------------
  // Content
  // ---------------------------------------------------------------------------
  async function loadContent() {
    try {
      const sections = await api('/api/admin/content/sections');
      const list = document.getElementById('content-list');
      list.innerHTML = sections.map(s => `
        <div class="content-section">
          <h3>
            <span>${escape(s.eyebrow)}: ${escape(s.heading)} <small>(${s.key})</small></span>
            <span class="badge" style="background:${s.isPublished ? '#22c55e33' : ''};color:${s.isPublished ? '#22c55e' : '#888'}">${s.isPublished ? 'Published' : 'Draft'}</span>
          </h3>
          <p style="color:var(--muted);margin-top:0.25rem">${s.items.length} items</p>
          <ul class="items">${s.items.map(i => `<li>${escape(i.title)} ${i.isPublished ? '' : '<small>(draft)</small>'}</li>`).join('')}</ul>
        </div>`).join('');
    } catch (err) { setGlobalError(err.message); }
  }

  // ---------------------------------------------------------------------------
  // Careers
  // ---------------------------------------------------------------------------
  async function loadJobs() {
    try {
      const jobs = await api('/api/admin/careers/jobs');
      document.getElementById('jobs-body').innerHTML = jobs.map(j => `
        <tr>
          <td>${escape(j.title)}</td>
          <td>${escape(j.department)}</td>
          <td>${escape(j.location)}</td>
          <td>${j.isPublished ? (j.closesAtUtc && new Date(j.closesAtUtc) < new Date() ? 'Closed' : 'Open') : 'Draft'}</td>
          <td>
            <button class="btn-secondary" data-id="${j.id}" data-action="view-job">View</button>
            <button class="btn-danger" data-id="${j.id}" data-action="delete-job">Delete</button>
          </td>
        </tr>`).join('');

      document.getElementById('jobs-body').querySelectorAll('button[data-action="view-job"]').forEach(b =>
        b.addEventListener('click', () => viewJob(b.dataset.id)));
      document.getElementById('jobs-body').querySelectorAll('button[data-action="delete-job"]').forEach(b =>
        b.addEventListener('click', async () => { if (!confirm('Delete this job?')) return; try { await api('/api/admin/careers/jobs/' + b.dataset.id, { method: 'DELETE' }); loadJobs(); } catch(err){ setGlobalError(err.message); } }));
    } catch (err) { setGlobalError(err.message); }
  }

  async function viewJob(id) {
    try {
      const job = await api('/api/admin/careers/jobs/' + id);
      openModal(`
        <h2>${escape(job.title)}</h2>
        <p><strong>Slug:</strong> ${escape(job.slug)}</p>
        <p><strong>Department:</strong> ${escape(job.department)} | <strong>Location:</strong> ${escape(job.location)}</p>
        <p><strong>Type:</strong> ${job.employmentType} (${job.workArrangement})</p>
        <p><strong>Status:</strong> ${job.isPublished ? (job.isOpenForApplications ? 'Open' : 'Closed') : 'Draft'}</p>
        <label>Description</label>
        <textarea rows="6" readonly>${escape(job.description)}</textarea>
        <label>Responsibilities</label>
        <textarea rows="4" readonly>${(job.responsibilities || []).map(x => '• ' + x).join('\n')}</textarea>
        <label>Requirements</label>
        <textarea rows="4" readonly>${(job.requirements || []).map(x => '• ' + x).join('\n')}</textarea>
        <div class="actions"><button class="btn-secondary close">Close</button></div>`);
    } catch (err) { setGlobalError(err.message); }
  }

  document.getElementById('new-job').addEventListener('click', () => {
    openModal(`
      <h2>New job posting</h2>
      <label for="j-slug">Slug</label><input id="j-slug" type="text" value="">
      <label for="j-title">Title</label><input id="j-title" type="text" value="">
      <label for="j-dept">Department</label><input id="j-dept" type="text" value="">
      <label for="j-loc">Location</label><input id="j-loc" type="text" value="">
      <label for="j-desc">Description</label><textarea id="j-desc" rows="4"></textarea>
      <label for="j-reqs">Requirements (one per line)</label><textarea id="j-reqs" rows="4"></textarea>
      <div class="actions">
        <button class="btn-primary" id="save-job">Save draft</button>
        <button class="btn-secondary close">Cancel</button>
      </div>`);

    document.getElementById('save-job').addEventListener('click', async () => {
      const body = {
        slug: document.getElementById('j-slug').value.trim(),
        title: document.getElementById('j-title').value.trim(),
        department: document.getElementById('j-dept').value.trim(),
        location: document.getElementById('j-loc').value.trim(),
        description: document.getElementById('j-desc').value.trim(),
        requirements: (document.getElementById('j-reqs').value || '').split('\n').map(x => x.trim()).filter(Boolean),
        responsibilities: [],
        employmentType: 'FullTime',
        workArrangement: 'Hybrid',
        isPublished: false
      };
      try { await api('/api/admin/careers/jobs', { method: 'POST', body }); document.getElementById('modal').style.display = 'none'; loadJobs(); }
      catch (err) { setGlobalError(err.message); }
    });
  });

  // ---------------------------------------------------------------------------
  // Applications
  // ---------------------------------------------------------------------------
  async function loadApplications() {
    const qs = new URLSearchParams({ Page: state.applications.page, PageSize: '25' });
    if (state.applications.search) qs.set('Search', state.applications.search);

    try {
      const data = await api('/api/admin/careers/applications?' + qs.toString());
      document.getElementById('applications-body').innerHTML = data.items.map(a => `
        <tr>
          <td>${escape(a.fullName)}</td>
          <td>${escape(a.email)}</td>
          <td>${escape(a.jobTitle) || '—'}</td>
          <td><span class="badge ${a.status}">${a.status}</span></td>
          <td>${new Date(a.createdAtUtc).toLocaleString()}</td>
          <td>
            <a class="btn-secondary" href="${API}/api/admin/careers/applications/${a.id}/resume" target="_blank" download>Download CV</a>
          </td>
        </tr>`).join('');
      renderPagination('apps-paging', data, (p) => { state.applications.page = p; loadApplications(); });
    } catch (err) { setGlobalError(err.message); }
  }

  document.getElementById('app-search').addEventListener('input', debounce(() => { state.applications.search = document.getElementById('app-search').value; state.applications.page = 1; loadApplications(); }, 300));
  document.getElementById('app-refresh').addEventListener('click', () => loadApplications());

  // ---------------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------------
  function renderPagination(id, data, onPage) {
    const el = document.getElementById(id);
    let html = '';
    for (let p = 1; p <= data.totalPages; p++) {
      html += `<button ${p === data.page ? 'disabled' : ''}>${p}</button>`;
    }
    el.innerHTML = html;
    el.querySelectorAll('button').forEach((b, idx) => b.addEventListener('click', () => onPage(idx + 1)));
  }

  function debounce(fn, ms) {
    let t;
    return () => { clearTimeout(t); t = setTimeout(fn, ms); };
  }

  function escape(text) {
    const d = document.createElement('div');
    d.textContent = text == null ? '' : String(text);
    return d.innerHTML;
  }

  // ---------------------------------------------------------------------------
  // Startup
  // ---------------------------------------------------------------------------
  if (accessToken && tokenExpires > Date.now() + 60000) {
    showAdmin();
  } else if (refreshToken) {
    api('/api/auth/me').then(showAdmin).catch(showLogin);
  } else {
    showLogin();
  }
})();
