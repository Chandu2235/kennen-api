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
    if (page === 'pricing') loadPricing();
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
          <td>${escape(l.phone) || '—'}</td>
          <td>${escape(l.engagement) || '—'}</td>
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
        <p><strong>Phone:</strong> ${escape(lead.phone) || '—'}</p>
        <p><strong>Engagement:</strong> ${escape(lead.engagement) || '—'}</p>
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
      const [sections, stats, testimonials] = await Promise.all([
        api('/api/admin/content/sections'),
        api('/api/admin/content/stats'),
        api('/api/admin/content/testimonials')
      ]);
      const list = document.getElementById('content-list');
      list.innerHTML = `
        <div class="filters">
          <button id="new-section" class="btn-primary">+ New section</button>
          <button id="new-stat" class="btn-secondary">+ Stat</button>
          <button id="new-testimonial" class="btn-secondary">+ Testimonial</button>
          <button id="refresh-content" class="btn-secondary">Refresh</button>
        </div>

        <h2 class="content-heading">Sections</h2>
        <div id="sections-list">${renderSectionList(sections)}</div>

        <h2 class="content-heading">Stats</h2>
        <table class="data-table" id="stats-table">
          <thead><tr><th>Value</th><th>Label</th><th>Description</th><th>Order</th><th>Status</th><th>Actions</th></tr></thead>
          <tbody>${(stats || []).map(s => `
            <tr>
              <td>${escape(s.value)}</td>
              <td>${escape(s.label)}</td>
              <td>${escape(s.description) || '—'}</td>
              <td>${s.displayOrder}</td>
              <td><span class="badge" style="background:${s.isPublished ? '#22c55e33' : ''};color:${s.isPublished ? '#22c55e' : '#888'}">${s.isPublished ? 'Published' : 'Draft'}</span></td>
              <td>
                <button class="btn-secondary" data-id="${s.id}" data-action="edit-stat">Edit</button>
                <button class="btn-danger" data-id="${s.id}" data-action="delete-stat">Delete</button>
              </td>
            </tr>`).join('')}</tbody>
        </table>

        <h2 class="content-heading">Testimonials</h2>
        <table class="data-table" id="testimonials-table">
          <thead><tr><th>Initials</th><th>Title</th><th>Organisation</th><th>Order</th><th>Status</th><th>Actions</th></tr></thead>
          <tbody>${(testimonials || []).map(t => `
            <tr>
              <td>${escape(t.authorInitials)}</td>
              <td>${escape(t.authorTitle)}</td>
              <td>${escape(t.organisation)}</td>
              <td>${t.displayOrder}</td>
              <td><span class="badge" style="background:${t.isPublished ? '#22c55e33' : ''};color:${t.isPublished ? '#22c55e' : '#888'}">${t.isPublished ? 'Published' : 'Draft'}</span></td>
              <td>
                <button class="btn-secondary" data-id="${t.id}" data-action="edit-testimonial">Edit</button>
                <button class="btn-danger" data-id="${t.id}" data-action="delete-testimonial">Delete</button>
              </td>
            </tr>`).join('')}</tbody>
        </table>`;

      attachContentHandlers(sections);
    } catch (err) { setGlobalError(err.message); }
  }

  function renderSectionList(sections) {
    return (sections || []).map(s => `
      <div class="content-section" data-id="${s.id}">
        <h3>
          <span>${escape(s.eyebrow)}: ${escape(s.heading)} <small>(${s.key})</small></span>
          <span class="badge" style="background:${s.isPublished ? '#22c55e33' : ''};color:${s.isPublished ? '#22c55e' : '#888'}">${s.isPublished ? 'Published' : 'Draft'}</span>
        </h3>
        <p style="color:var(--muted);margin-top:0.25rem">${s.items.length} items · order ${s.displayOrder}</p>
        <ul class="items">${(s.items || []).map(i => `
          <li data-item-id="${i.id}">
            ${escape(i.title)} ${i.isPublished ? '' : '<small>(draft)</small>'}
            <button class="btn-icon" data-action="edit-item" data-item-id="${i.id}" data-section-id="${s.id}" title="Edit">✎</button>
            <button class="btn-icon" data-action="delete-item" data-item-id="${i.id}" title="Delete">×</button>
          </li>`).join('')}</ul>
        <div class="content-actions">
          <button class="btn-secondary" data-action="edit-section" data-id="${s.id}">Edit section</button>
          <button class="btn-secondary" data-action="add-item" data-id="${s.id}">+ Add item</button>
          <button class="btn-danger" data-action="delete-section" data-id="${s.id}">Delete section</button>
        </div>
      </div>`).join('');
  }

  function attachContentHandlers(sections) {
    document.getElementById('refresh-content')?.addEventListener('click', loadContent);
    document.getElementById('new-section')?.addEventListener('click', () => openSectionModal());
    document.getElementById('new-stat')?.addEventListener('click', () => openStatModal());
    document.getElementById('new-testimonial')?.addEventListener('click', () => openTestimonialModal());

    document.getElementById('sections-list')?.querySelectorAll('button[data-action]').forEach(b =>
      b.addEventListener('click', () => {
        const action = b.dataset.action;
        const id = b.dataset.id;
        if (action === 'edit-section') editSection(id, sections);
        if (action === 'delete-section') deleteSection(id);
        if (action === 'add-item') addItem(id);
      }));

    document.getElementById('sections-list')?.querySelectorAll('li button[data-action]').forEach(b =>
      b.addEventListener('click', (e) => {
        e.stopPropagation();
        const action = b.dataset.action;
        const itemId = b.dataset.itemId;
        const sectionId = b.dataset.sectionId;
        if (action === 'edit-item') editItem(itemId, sectionId, sections);
        if (action === 'delete-item') deleteItem(itemId);
      }));

    document.getElementById('stats-table')?.querySelectorAll('button[data-action]').forEach(b =>
      b.addEventListener('click', () => {
        const action = b.dataset.action;
        const id = b.dataset.id;
        if (action === 'edit-stat') editStat(id);
        if (action === 'delete-stat') deleteStat(id);
      }));

    document.getElementById('testimonials-table')?.querySelectorAll('button[data-action]').forEach(b =>
      b.addEventListener('click', () => {
        const action = b.dataset.action;
        const id = b.dataset.id;
        if (action === 'edit-testimonial') editTestimonial(id);
        if (action === 'delete-testimonial') deleteTestimonial(id);
      }));
  }

  async function editSection(id, sections) {
    const s = (sections || []).find(x => x.id === id);
    if (!s) return;
    openModal(`
      <h2>Edit section</h2>
      <label for="c-key">Key</label><input id="c-key" type="text" value="${escape(s.key)}" readonly>
      <label for="c-eyebrow">Eyebrow</label><input id="c-eyebrow" type="text" value="${escape(s.eyebrow)}">
      <label for="c-heading">Heading</label><input id="c-heading" type="text" value="${escape(s.heading)}">
      <label for="c-desc">Description</label><textarea id="c-desc" rows="4">${escape(s.description || '')}</textarea>
      <label for="c-order">Display order</label><input id="c-order" type="number" value="${s.displayOrder}">
      <label for="c-published"><input type="checkbox" id="c-published" ${s.isPublished ? 'checked' : ''}> Published</label>
      <div class="actions">
        <button class="btn-primary" id="save-section">Save</button>
        <button class="btn-secondary close">Cancel</button>
      </div>`, () => {});

    document.getElementById('save-section').addEventListener('click', async () => {
      const payload = {
        key: document.getElementById('c-key').value.trim(),
        eyebrow: document.getElementById('c-eyebrow').value.trim(),
        heading: document.getElementById('c-heading').value.trim(),
        description: document.getElementById('c-desc').value.trim() || null,
        displayOrder: parseInt(document.getElementById('c-order').value || '0', 10),
        isPublished: document.getElementById('c-published').checked
      };
      try { await api('/api/admin/content/sections/' + id, { method: 'PUT', body: payload }); document.getElementById('modal').style.display = 'none'; loadContent(); }
      catch (err) { setGlobalError(err.message); }
    });
  }

  function openSectionModal(existing) {
    const isEdit = !!existing;
    openModal(`
      <h2>${isEdit ? 'Edit section' : 'New section'}</h2>
      <label for="c-key">Key</label><input id="c-key" type="text" value="${isEdit ? escape(existing.key) : ''}" ${isEdit ? 'readonly' : ''}>
      <label for="c-eyebrow">Eyebrow</label><input id="c-eyebrow" type="text" value="${isEdit ? escape(existing.eyebrow) : ''}">
      <label for="c-heading">Heading</label><input id="c-heading" type="text" value="${isEdit ? escape(existing.heading) : ''}">
      <label for="c-desc">Description</label><textarea id="c-desc" rows="4">${isEdit ? escape(existing.description || '') : ''}</textarea>
      <label for="c-order">Display order</label><input id="c-order" type="number" value="${isEdit ? existing.displayOrder : '0'}">
      <label for="c-published"><input type="checkbox" id="c-published" ${!isEdit || existing.isPublished ? 'checked' : ''}> Published</label>
      <div class="actions">
        <button class="btn-primary" id="save-section">Save</button>
        <button class="btn-secondary close">Cancel</button>
      </div>`);
    const id = isEdit ? existing.id : '';
    const method = isEdit ? 'PUT' : 'POST';
    const path = isEdit ? '/api/admin/content/sections/' + id : '/api/admin/content/sections';
    document.getElementById('save-section').addEventListener('click', async () => {
      const payload = {
        key: document.getElementById('c-key').value.trim(),
        eyebrow: document.getElementById('c-eyebrow').value.trim(),
        heading: document.getElementById('c-heading').value.trim(),
        description: document.getElementById('c-desc').value.trim() || null,
        displayOrder: parseInt(document.getElementById('c-order').value || '0', 10),
        isPublished: document.getElementById('c-published').checked
      };
      try { await api(path, { method, body: payload }); document.getElementById('modal').style.display = 'none'; loadContent(); }
      catch (err) { setGlobalError(err.message); }
    });
  }

  async function deleteSection(id) {
    if (!confirm('Delete this section and all its items?')) return;
    try { await api('/api/admin/content/sections/' + id, { method: 'DELETE' }); loadContent(); }
    catch (err) { setGlobalError(err.message); }
  }

  async function addItem(sectionId) {
    openItemModal(sectionId);
  }

  async function editItem(itemId, sectionId, sections) {
    const section = (sections || []).find(s => s.id === sectionId);
    const item = section && (section.items || []).find(i => i.id === itemId);
    if (!item) return;
    openItemModal(sectionId, item);
  }

  function openItemModal(sectionId, item) {
    const isEdit = !!item;
    openModal(`
      <h2>${isEdit ? 'Edit item' : 'Add item'}</h2>
      <label for="i-title">Title</label><input id="i-title" type="text" value="${isEdit ? escape(item.title) : ''}">
      <label for="i-summary">Summary</label><textarea id="i-summary" rows="4">${isEdit ? escape(item.summary || '') : ''}</textarea>
      <label for="i-icon">Icon / number (optional, e.g. 01 or emoji)</label><input id="i-icon" type="text" value="${isEdit ? escape(item.icon || '') : ''}">
      <label for="i-order">Display order</label><input id="i-order" type="number" value="${isEdit ? item.displayOrder : '0'}">
      <label for="i-published"><input type="checkbox" id="i-published" ${!isEdit || item.isPublished ? 'checked' : ''}> Published</label>
      <div class="actions">
        <button class="btn-primary" id="save-item">Save</button>
        <button class="btn-secondary close">Cancel</button>
      </div>`);
    const id = isEdit ? item.id : '';
    const method = isEdit ? 'PUT' : 'POST';
    const path = isEdit ? '/api/admin/content/items/' + id : '/api/admin/content/sections/' + sectionId + '/items';
    document.getElementById('save-item').addEventListener('click', async () => {
      const payload = {
        title: document.getElementById('i-title').value.trim(),
        summary: document.getElementById('i-summary').value.trim() || null,
        icon: document.getElementById('i-icon').value.trim() || null,
        displayOrder: parseInt(document.getElementById('i-order').value || '0', 10),
        isPublished: document.getElementById('i-published').checked
      };
      try { await api(path, { method, body: payload }); document.getElementById('modal').style.display = 'none'; loadContent(); }
      catch (err) { setGlobalError(err.message); }
    });
  }

  async function deleteItem(id) {
    if (!confirm('Delete this item?')) return;
    try { await api('/api/admin/content/items/' + id, { method: 'DELETE' }); loadContent(); }
    catch (err) { setGlobalError(err.message); }
  }

  async function editStat(id) {
    let stat;
    try { stat = await api('/api/admin/content/stats'); stat = (stat || []).find(s => s.id === id); } catch (err) { return setGlobalError(err.message); }
    if (!stat) return;
    openStatModal(stat);
  }

  function openStatModal(stat) {
    const isEdit = !!stat;
    openModal(`
      <h2>${isEdit ? 'Edit stat' : 'New stat'}</h2>
      <label for="s-value">Value (e.g. 40%)</label><input id="s-value" type="text" value="${isEdit ? escape(stat.value) : ''}">
      <label for="s-label">Label</label><input id="s-label" type="text" value="${isEdit ? escape(stat.label) : ''}">
      <label for="s-desc">Description</label><textarea id="s-desc" rows="3">${isEdit ? escape(stat.description || '') : ''}</textarea>
      <label for="s-order">Display order</label><input id="s-order" type="number" value="${isEdit ? stat.displayOrder : '0'}">
      <label for="s-published"><input type="checkbox" id="s-published" ${!isEdit || stat.isPublished ? 'checked' : ''}> Published</label>
      <div class="actions">
        <button class="btn-primary" id="save-stat">Save</button>
        <button class="btn-secondary close">Cancel</button>
      </div>`);
    const id = isEdit ? stat.id : '';
    const method = isEdit ? 'PUT' : 'POST';
    const path = isEdit ? '/api/admin/content/stats/' + id : '/api/admin/content/stats';
    document.getElementById('save-stat').addEventListener('click', async () => {
      const payload = {
        value: document.getElementById('s-value').value.trim(),
        label: document.getElementById('s-label').value.trim(),
        description: document.getElementById('s-desc').value.trim() || null,
        displayOrder: parseInt(document.getElementById('s-order').value || '0', 10),
        isPublished: document.getElementById('s-published').checked
      };
      try { await api(path, { method, body: payload }); document.getElementById('modal').style.display = 'none'; loadContent(); }
      catch (err) { setGlobalError(err.message); }
    });
  }

  async function deleteStat(id) {
    if (!confirm('Delete this stat?')) return;
    try { await api('/api/admin/content/stats/' + id, { method: 'DELETE' }); loadContent(); }
    catch (err) { setGlobalError(err.message); }
  }

  async function editTestimonial(id) {
    let list;
    try { list = await api('/api/admin/content/testimonials'); list = (list || []).find(t => t.id === id); } catch (err) { return setGlobalError(err.message); }
    if (!list) return;
    openTestimonialModal(list);
  }

  function openTestimonialModal(t) {
    const isEdit = !!t;
    openModal(`
      <h2>${isEdit ? 'Edit testimonial' : 'New testimonial'}</h2>
      <label for="t-quote">Quote</label><textarea id="t-quote" rows="5">${isEdit ? escape(t.quote) : ''}</textarea>
      <label for="t-initials">Author initials</label><input id="t-initials" type="text" value="${isEdit ? escape(t.authorInitials) : ''}">
      <label for="t-title">Author title</label><input id="t-title" type="text" value="${isEdit ? escape(t.authorTitle) : ''}">
      <label for="t-org">Organisation</label><input id="t-org" type="text" value="${isEdit ? escape(t.organisation) : ''}">
      <label for="t-order">Display order</label><input id="t-order" type="number" value="${isEdit ? t.displayOrder : '0'}">
      <label for="t-published"><input type="checkbox" id="t-published" ${!isEdit || t.isPublished ? 'checked' : ''}> Published</label>
      <div class="actions">
        <button class="btn-primary" id="save-testimonial">Save</button>
        <button class="btn-secondary close">Cancel</button>
      </div>`);
    const id = isEdit ? t.id : '';
    const method = isEdit ? 'PUT' : 'POST';
    const path = isEdit ? '/api/admin/content/testimonials/' + id : '/api/admin/content/testimonials';
    document.getElementById('save-testimonial').addEventListener('click', async () => {
      const payload = {
        quote: document.getElementById('t-quote').value.trim(),
        authorInitials: document.getElementById('t-initials').value.trim(),
        authorTitle: document.getElementById('t-title').value.trim(),
        organisation: document.getElementById('t-org').value.trim(),
        displayOrder: parseInt(document.getElementById('t-order').value || '0', 10),
        isPublished: document.getElementById('t-published').checked
      };
      try { await api(path, { method, body: payload }); document.getElementById('modal').style.display = 'none'; loadContent(); }
      catch (err) { setGlobalError(err.message); }
    });
  }

  async function deleteTestimonial(id) {
    if (!confirm('Delete this testimonial?')) return;
    try { await api('/api/admin/content/testimonials/' + id, { method: 'DELETE' }); loadContent(); }
    catch (err) { setGlobalError(err.message); }
  }

  // ---------------------------------------------------------------------------
  // Pricing
  // ---------------------------------------------------------------------------
  async function loadPricing() {
    try {
      const plans = await api('/api/admin/pricing/plans');
      const list = document.getElementById('pricing-list');
      list.innerHTML = `
        <div class="filters">
          <button id="new-pricing-plan" class="btn-primary">+ New plan</button>
          <button id="refresh-pricing" class="btn-secondary">Refresh</button>
        </div>
        <div id="pricing-plans-list">${renderPlanList(plans)}</div>`;

      document.getElementById('refresh-pricing')?.addEventListener('click', loadPricing);
      document.getElementById('new-pricing-plan')?.addEventListener('click', () => openPlanModal());
      document.getElementById('pricing-plans-list')?.querySelectorAll('button[data-action]').forEach(b =>
        b.addEventListener('click', () => {
          const action = b.dataset.action;
          const id = b.dataset.id;
          const planId = b.dataset.planId;
          if (action === 'edit-plan') editPlan(id);
          if (action === 'delete-plan') deletePlan(id);
          if (action === 'add-feature') addFeature(planId);
          if (action === 'edit-feature') editFeature(id, planId, plans);
          if (action === 'delete-feature') deleteFeature(id);
        }));
    } catch (err) { setGlobalError(err.message); }
  }

  function renderPlanList(plans) {
    return (plans || []).map(p => `
      <div class="content-section" data-id="${p.id}">
        <h3>
          <span>${escape(p.name)} <small>(${p.slug})</small></span>
          <span>
            <span class="badge" style="background:${p.isPublished ? '#22c55e33' : ''};color:${p.isPublished ? '#22c55e' : '#888'}">${p.isPublished ? 'Published' : 'Draft'}</span>
            ${p.isFeatured ? '<span class="badge" style="background:#eab30833;color:#eab308">Featured</span>' : ''}
          </span>
        </h3>
        <p style="color:var(--muted);margin-top:0.25rem">${p.category} · ${p.price}${p.billingPeriod || ''} · ${p.features.length} features · order ${p.displayOrder}</p>
        <ul class="items">${(p.features || []).map(f => `
          <li data-feature-id="${f.id}">
            ${escape(f.text)} ${f.isPublished ? '' : '<small>(draft)</small>'}
            <button class="btn-icon" data-action="edit-feature" data-id="${f.id}" data-plan-id="${p.id}" title="Edit">✎</button>
            <button class="btn-icon" data-action="delete-feature" data-id="${f.id}" title="Delete">×</button>
          </li>`).join('')}</ul>
        <div class="content-actions">
          <button class="btn-secondary" data-action="edit-plan" data-id="${p.id}">Edit plan</button>
          <button class="btn-secondary" data-action="add-feature" data-plan-id="${p.id}">+ Add feature</button>
          <button class="btn-danger" data-action="delete-plan" data-id="${p.id}">Delete plan</button>
        </div>
      </div>`).join('');
  }

  async function editPlan(id) {
    let plan;
    try { plan = (await api('/api/admin/pricing/plans')).find(p => p.id === id); } catch (err) { return setGlobalError(err.message); }
    if (!plan) return;
    openPlanModal(plan);
  }

  function openPlanModal(plan) {
    const isEdit = !!plan;
    openModal(`
      <h2>${isEdit ? 'Edit plan' : 'New plan'}</h2>
      <label for="p-slug">Slug</label><input id="p-slug" type="text" value="${isEdit ? escape(plan.slug) : ''}" ${isEdit ? 'readonly' : ''}>
      <label for="p-category">Category</label><input id="p-category" type="text" value="${isEdit ? escape(plan.category || 'ai') : 'ai'}" placeholder="ai or qa-testing">
      <label for="p-name">Name</label><input id="p-name" type="text" value="${isEdit ? escape(plan.name) : ''}">
      <label for="p-subtitle">Subtitle</label><input id="p-subtitle" type="text" value="${isEdit ? escape(plan.subtitle || '') : ''}">
      <label for="p-price">Price</label><input id="p-price" type="text" value="${isEdit ? escape(plan.price) : ''}">
      <label for="p-billing">Billing period</label><input id="p-billing" type="text" value="${isEdit ? escape(plan.billingPeriod || '') : ''}">
      <label for="p-desc">Description</label><textarea id="p-desc" rows="3">${isEdit ? escape(plan.description || '') : ''}</textarea>
      <label for="p-order">Display order</label><input id="p-order" type="number" value="${isEdit ? plan.displayOrder : '0'}">
      <label for="p-featured"><input type="checkbox" id="p-featured" ${isEdit && plan.isFeatured ? 'checked' : ''}> Featured</label>
      <label for="p-published"><input type="checkbox" id="p-published" ${!isEdit || plan.isPublished ? 'checked' : ''}> Published</label>
      <div class="actions">
        <button class="btn-primary" id="save-plan">Save</button>
        <button class="btn-secondary close">Cancel</button>
      </div>`);
    const id = isEdit ? plan.id : '';
    const method = isEdit ? 'PUT' : 'POST';
    const path = isEdit ? '/api/admin/pricing/plans/' + id : '/api/admin/pricing/plans';
    document.getElementById('save-plan').addEventListener('click', async () => {
      const payload = {
        slug: document.getElementById('p-slug').value.trim(),
        category: document.getElementById('p-category').value.trim(),
        name: document.getElementById('p-name').value.trim(),
        subtitle: document.getElementById('p-subtitle').value.trim() || null,
        price: document.getElementById('p-price').value.trim(),
        billingPeriod: document.getElementById('p-billing').value.trim() || null,
        description: document.getElementById('p-desc').value.trim() || null,
        displayOrder: parseInt(document.getElementById('p-order').value || '0', 10),
        isFeatured: document.getElementById('p-featured').checked,
        isPublished: document.getElementById('p-published').checked
      };
      try { await api(path, { method, body: payload }); document.getElementById('modal').style.display = 'none'; loadPricing(); }
      catch (err) { setGlobalError(err.message); }
    });
  }

  async function deletePlan(id) {
    if (!confirm('Delete this plan and all its features?')) return;
    try { await api('/api/admin/pricing/plans/' + id, { method: 'DELETE' }); loadPricing(); }
    catch (err) { setGlobalError(err.message); }
  }

  async function addFeature(planId) {
    openFeatureModal(planId);
  }

  async function editFeature(featureId, planId, plans) {
    const plan = (plans || []).find(p => p.id === planId);
    const feature = plan && (plan.features || []).find(f => f.id === featureId);
    if (!feature) return;
    openFeatureModal(planId, feature);
  }

  function openFeatureModal(planId, feature) {
    const isEdit = !!feature;
    openModal(`
      <h2>${isEdit ? 'Edit feature' : 'Add feature'}</h2>
      <label for="f-text">Feature text</label><input id="f-text" type="text" value="${isEdit ? escape(feature.text) : ''}">
      <label for="f-icon">Icon (optional, e.g. ✓)</label><input id="f-icon" type="text" value="${isEdit ? escape(feature.icon || '') : ''}">
      <label for="f-order">Display order</label><input id="f-order" type="number" value="${isEdit ? feature.displayOrder : '0'}">
      <label for="f-published"><input type="checkbox" id="f-published" ${!isEdit || feature.isPublished ? 'checked' : ''}> Published</label>
      <div class="actions">
        <button class="btn-primary" id="save-feature">Save</button>
        <button class="btn-secondary close">Cancel</button>
      </div>`);
    const id = isEdit ? feature.id : '';
    const method = isEdit ? 'PUT' : 'POST';
    const path = isEdit ? '/api/admin/pricing/features/' + id : '/api/admin/pricing/plans/' + planId + '/features';
    document.getElementById('save-feature').addEventListener('click', async () => {
      const payload = {
        text: document.getElementById('f-text').value.trim(),
        icon: document.getElementById('f-icon').value.trim() || null,
        displayOrder: parseInt(document.getElementById('f-order').value || '0', 10),
        isPublished: document.getElementById('f-published').checked
      };
      try { await api(path, { method, body: payload }); document.getElementById('modal').style.display = 'none'; loadPricing(); }
      catch (err) { setGlobalError(err.message); }
    });
  }

  async function deleteFeature(id) {
    if (!confirm('Delete this feature?')) return;
    try { await api('/api/admin/pricing/features/' + id, { method: 'DELETE' }); loadPricing(); }
    catch (err) { setGlobalError(err.message); }
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
