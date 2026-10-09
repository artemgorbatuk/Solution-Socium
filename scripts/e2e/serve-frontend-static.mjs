#!/usr/bin/env node
/**
 * Static host for the built Angular app + proxy /api -> WebApi, for E2E tests (no ng serve).
 * The /api prefix is kept, as in proxy.conf.json: WebApi routes are api/[controller].
 *
 * Usage:
 *   node scripts/e2e/serve-frontend-static.mjs --port 5173 --dist path/to/browser --api http://127.0.0.1:5xxx
 */
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';

const args = parseArgs(process.argv.slice(2));
const port = Number(args.port);
const distRoot = path.resolve(args.dist);
const apiTarget = new URL(args.api);

if (!Number.isInteger(port) || port <= 0) {
  fail('--port <number> is required');
}
if (!fs.existsSync(path.join(distRoot, 'index.html'))) {
  fail(`index.html not found in dist: ${distRoot}`);
}

const mime = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.map': 'application/json',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.woff2': 'font/woff2',
  '.txt': 'text/plain; charset=utf-8',
};

const server = http.createServer(async (req, res) => {
  try {
    const url = new URL(req.url ?? '/', `http://127.0.0.1:${port}`);
    if (url.pathname === '/api' || url.pathname.startsWith('/api/')) {
      await proxyApi(req, res, url);
      return;
    }
    serveStatic(res, url.pathname);
  } catch (error) {
    console.error(error);
    if (!res.headersSent) {
      res.writeHead(502, { 'Content-Type': 'text/plain; charset=utf-8' });
    }
    res.end('Bad Gateway');
  }
});

server.listen(port, '127.0.0.1', () => {
  console.log(`e2e-static listening http://127.0.0.1:${port} dist=${distRoot} api=${apiTarget.origin}`);
});

function parseArgs(argv) {
  const out = { port: '', dist: '', api: '' };
  for (let i = 0; i < argv.length; i += 2) {
    const key = argv[i]?.replace(/^--/, '');
    if (key in out) {
      out[key] = argv[i + 1] ?? '';
    }
  }
  return out;
}

function fail(message) {
  console.error(message);
  process.exit(1);
}

function proxyApi(req, res, url) {
  const targetUrl = new URL(url.pathname + url.search, apiTarget);
  const headers = { ...req.headers, host: apiTarget.host };
  delete headers.connection;

  return new Promise((resolve, reject) => {
    const upstream = http.request(targetUrl, { method: req.method, headers }, (upstreamRes) => {
      res.writeHead(upstreamRes.statusCode ?? 502, upstreamRes.headers);
      upstreamRes.pipe(res);
      upstreamRes.on('end', resolve);
      upstreamRes.on('error', reject);
    });
    upstream.on('error', reject);
    req.pipe(upstream);
  });
}

function serveStatic(res, pathname) {
  let filePath = path.normalize(path.join(distRoot, decodeURIComponent(pathname)));
  if (!filePath.startsWith(distRoot)) {
    res.writeHead(403).end('Forbidden');
    return;
  }

  if (!fs.existsSync(filePath) || fs.statSync(filePath).isDirectory()) {
    // SPA fallback: client-side routes are served by index.html.
    filePath = path.join(distRoot, 'index.html');
  }

  res.writeHead(200, { 'Content-Type': mime[path.extname(filePath).toLowerCase()] ?? 'application/octet-stream' });
  fs.createReadStream(filePath).pipe(res);
}
