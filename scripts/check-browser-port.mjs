// Checks the browser port (site/assets/rules.js) against the fixtures the C# tests also use.
// Run: node scripts/check-browser-port.mjs
import { readFileSync } from 'node:fs';
import { propose, RequestError, AuditTrail } from '../site/assets/rules.js';

const { cases } = JSON.parse(readFileSync(new URL('../tests/parity/extraction-cases.json', import.meta.url), 'utf8'));
let failed = 0;
for (const c of cases) {
  let ok;
  try {
    const p = propose(c.request);
    const e = c.expected;
    ok = !e.error && p.item.number === e.item && p.request.quantity === e.quantity && p.available === e.available && p.backorder === e.backorder;
  } catch (err) {
    ok = err instanceof RequestError && !!c.expected.error && err.message.includes(c.expected.error);
  }
  if (!ok) failed++;
  console.log(`${ok ? 'ok  ' : 'FAIL'} ${c.name}`);
}
const trail = new AuditTrail();
await trail.append('c1', 'RequestExtracted', 'Quantity=40');
await trail.append('c1', 'ProposalCreated', 'status=PendingApproval');
const chainOk = await AuditTrail.verify(trail.events);
const tampered = trail.events.map((e, i) => i ? { ...e, detail: 'status=Approved' } : e);
const tamperCaught = !(await AuditTrail.verify(tampered));
console.log(`${chainOk && tamperCaught ? 'ok  ' : 'FAIL'} audit chain verifies and detects an edit`);
if (failed || !chainOk || !tamperCaught) { console.error('browser port differs from the C# behaviour'); process.exit(1); }
console.log(`browser port matches all ${cases.length} shared cases`);
