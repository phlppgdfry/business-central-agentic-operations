import { propose, seedStock, RequestError, AuditTrail } from './rules.js';
const $ = id => document.getElementById(id);
const esc = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const PRESETS = {
  'Backorder': 'Please order another 40 units of product X for our Antwerp site.',
  'In stock': 'Can we get 25 pcs of Product Y delivered to Antwerp?',
  'Unknown product': 'Please order 10 units of product Z for Antwerp.',
  'Ambiguous number': 'For Antwerp site 2, please order 15 units of PROD-X.',
};
let stock = seedStock(), seq = 1000, n = 0;
const proposals = [];
const trail = new AuditTrail();

function renderStock() {
  $('bc-stock').innerHTML = Object.entries(stock).map(([k, v]) => `<div class="kpi-tile"><div class="v">${v}</div><div class="l">${k} available in Business Central (simulated)</div></div>`).join('');
}
function renderAudit() {
  $('bc-audit').innerHTML = trail.events.slice().reverse().map(e => `<li><div><b>${e.eventType}</b> <span class="dim small mono">${e.correlationId}</span></div><div class="small muted">${esc(e.detail)}</div><div class="mono bc-hash">prev ${e.previousHash.slice(0, 10)}… → <b>${e.hash.slice(0, 10)}…</b></div></li>`).join('');
}
function renderProposals() {
  $('bc-proposals').innerHTML = proposals.slice().reverse().map(p => `<div class="bc-prop">
    <div class="bc-ph"><b>${p.item.description} × ${p.request.quantity}</b><span class="pill ${p.status === 'Approved' ? 'ok' : 'warn'}">${p.status === 'Approved' ? 'Approved' : 'Pending approval'}</span></div>
    <div class="small muted">${esc(p.customer.name)} · ${esc(p.explanation)}</div>
    ${p.order ? `<div class="bc-so">Sales order <b class="mono">${p.order}</b> created by ${esc(p.approvedBy)}${p.backorder ? ` · backorder ${p.backorder}` : ''}</div>`
      : `<div class="bc-row"><input value="manager@contoso.example" aria-label="Approver" data-for="${p.id}"><button type="button" class="btn primary bc-sm" data-approve="${p.id}">Approve</button></div>`}</div>`).join('') || '<p class="small dim">Proposals appear here and wait for a person.</p>';
}
function step(n, title, body, cls = '') { return `<div class="bc-step ${cls}"><div class="bc-n">${n}</div><div><b>${title}</b><div class="small">${body}</div></div></div>`; }

async function create() {
  const text = $('bc-req').value, id = `order-${String(++n).padStart(3, '0')}`;
  try {
    const p = propose(text, stock);
    await trail.append(id, 'RequestExtracted', `Quantity=${p.request.quantity}; customerHint=${p.request.customerHint}; itemHint=${p.request.itemHint}`);
    Object.assign(p, { id });
    proposals.push(p);
    await trail.append(id, 'ProposalCreated', `Proposal=${id}; status=PendingApproval; backorder=${p.backorder}`);
    $('bc-pipe').innerHTML =
      step(1, 'Extract', `<code>${esc(JSON.stringify({ quantity: p.request.quantity, item: p.request.itemHint, site: p.request.requestedSite }))}</code>`) +
      step(2, 'Look up in Business Central', `${esc(p.customer.name)} (${p.customer.number}) · ${esc(p.item.description)} · ${p.available} available`) +
      step(3, 'Apply rules', p.backorder ? `Backorder of <b>${p.backorder}</b> unit(s)` : 'Enough stock, no backorder') +
      step(4, 'Proposal, not an order', 'Status Pending approval. The sales order is only created after a person approves.', 'hl');
    renderProposals(); renderAudit();
  } catch (e) {
    if (!(e instanceof RequestError)) throw e;
    $('bc-pipe').innerHTML = step('!', 'Refused with HTTP 400', esc(e.message) + '<div class="dim" style="margin-top:4px">No guess, no proposal, no ERP call. A person can rephrase or handle it.</div>', 'bad');
  }
}
async function approve(id, who) {
  const p = proposals.find(x => x.id === id);
  if (!p || p.status !== 'PendingApproval' || !who.trim()) return;
  p.backorder = Math.max(0, p.request.quantity - (stock[p.item.number] ?? 0));
  p.order = `SO-${++seq}`; p.status = 'Approved'; p.approvedBy = who;
  stock[p.item.number] = Math.max(0, (stock[p.item.number] ?? 0) - p.request.quantity);
  await trail.append(id, 'SalesOrderCreated', `SalesOrder=${p.order}; approvedBy=${who}`);
  renderStock(); renderProposals(); renderAudit();
}
$('bc-presets').innerHTML = Object.keys(PRESETS).map(k => `<button type="button">${k}</button>`).join('');
$('bc-presets').addEventListener('click', e => {
  if (e.target.tagName !== 'BUTTON') return;
  $('bc-req').value = PRESETS[e.target.textContent];
  document.querySelectorAll('#bc-presets button').forEach(b => b.setAttribute('aria-selected', b === e.target));
});
$('bc-create').addEventListener('click', create);
$('bc-proposals').addEventListener('click', e => { const id = e.target.dataset.approve; if (id) approve(id, document.querySelector(`[data-for="${id}"]`).value); });
$('bc-verify').addEventListener('click', async () => {
  const ok = await AuditTrail.verify(trail.events);
  $('bc-verdict').innerHTML = trail.events.length ? `<div class="pill ${ok ? 'ok' : 'bad'}" style="margin:8px 0">${ok ? `✓ ${trail.events.length} events verified: every hash and link recomputes` : '✗ Chain broken: an event was changed after it was written'}</div>` : '';
});
$('bc-tamper').addEventListener('click', () => {
  const e = trail.events.find(x => x.eventType === 'ProposalCreated');
  if (e) { e.detail = e.detail.replace('backorder=', 'backorder=0; was='); renderAudit(); $('bc-verdict').innerHTML = '<div class="small dim" style="margin:8px 0">One event was edited in place. Now press Verify chain.</div>'; }
});
document.querySelector('#bc-presets button').click();
renderStock(); renderProposals();
await create();
