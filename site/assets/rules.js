// Browser port of OrderRequestExtractor, DemoBusinessCentralClient, OrderProposalService and
// HashChainedAuditTrail (src/). tests/parity/extraction-cases.json is checked against both the C#
// code (tests/AgenticOperations.Tests) and this file (scripts/check-browser-port.mjs) in CI.
const KNOWN_ITEMS = [['product x', 'PROD-X'], ['prod-x', 'PROD-X'], ['product y', 'PROD-Y'], ['prod-y', 'PROD-Y']];
export const CUSTOMERS = [{ number: 'ANTWERP', name: 'Contoso Antwerp Site', site: 'Antwerp' }];
export const ITEMS = [{ number: 'PROD-X', description: 'Product X' }, { number: 'PROD-Y', description: 'Product Y' }];
export const seedStock = () => ({ 'PROD-X': 32, 'PROD-Y': 120 });

export class RequestError extends Error {}

export function extract(text) {
  const lower = text.toLowerCase();
  let m = /\b(\d+)\s*(?:units?|pcs|pieces|stuks?)\b/i.exec(text) || /\b(\d+)\b/.exec(text);
  const quantity = m ? Number(m[1]) : NaN;
  if (!m || !Number.isSafeInteger(quantity) || quantity > 2147483647) throw new RequestError('No quantity could be extracted.');
  if (quantity < 1) throw new RequestError('The quantity must be at least 1.');
  const items = [...new Set(KNOWN_ITEMS.filter(([p]) => lower.includes(p)).map(([, i]) => i))];
  if (!items.length) throw new RequestError('No known item was named. The demo catalogue has Product X and Product Y.');
  if (items.length > 1) throw new RequestError('More than one item was named; create one proposal per item.');
  if (!lower.includes('antwerp')) throw new RequestError('No known customer site was named. The demo customer is the Antwerp site.');
  return { customerHint: 'ANTWERP', itemHint: items[0], quantity, requestedSite: 'Antwerp', originalText: text };
}

export function propose(text, stock = seedStock()) {
  const request = extract(text);
  const customer = CUSTOMERS.find(c => c.number.includes(request.customerHint) || c.site.toLowerCase().includes(request.requestedSite.toLowerCase()));
  const item = ITEMS.find(i => i.number === request.itemHint);
  const available = stock[item.number] ?? 0;
  const backorder = Math.max(0, request.quantity - available);
  const explanation = backorder === 0
    ? `${request.quantity} unit(s) are available. Ready for approval.`
    : `Only ${available} unit(s) are available. Approval will create an order for ${request.quantity}, including a ${backorder}-unit backorder.`;
  return { request, customer, item, available, backorder, explanation, status: 'PendingApproval' };
}

async function sha256Hex(s) {
  const buf = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(s));
  return [...new Uint8Array(buf)].map(b => b.toString(16).padStart(2, '0')).join('').toUpperCase();
}
export class AuditTrail {
  events = [];
  async append(correlationId, eventType, detail) {
    const previousHash = this.events.at(-1)?.hash ?? 'GENESIS';
    const occurredAtUtc = new Date().toISOString();
    const hash = await sha256Hex(`${previousHash}|${occurredAtUtc}|${correlationId}|${eventType}|${detail}`);
    this.events.push({ occurredAtUtc, correlationId, eventType, detail, previousHash, hash });
  }
  static async verify(chain) {
    let previous = 'GENESIS';
    for (const e of chain) {
      if (e.previousHash !== previous) return false;
      if (e.hash !== await sha256Hex(`${e.previousHash}|${e.occurredAtUtc}|${e.correlationId}|${e.eventType}|${e.detail}`)) return false;
      previous = e.hash;
    }
    return true;
  }
}
