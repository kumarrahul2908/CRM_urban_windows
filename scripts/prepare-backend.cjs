#!/usr/bin/env node
'use strict';
/**
 * Builds the Node/Fastify backend from kumarrahul2908/urban-dry-clean-crm-node (the single
 * source of truth for all business logic and the REST API this app talks to) and stages a
 * self-contained copy of it - dist/, public/, db/, production-only node_modules, plus a portable
 * Windows Node.js runtime (node.exe only) - so the native app never requires Node.js to be
 * separately installed on the shop PC, and never duplicates backend code in this repo.
 *
 * Output layout: <target>/backend/{node/node.exe, dist/, public/, db/, node_modules/, package.json}
 * BackendProcessManager (src/Udc.Client/BackendProcessManager.cs) expects exactly this layout
 * relative to Udc.App.exe's own directory, so <target> should be the `dotnet publish` output
 * directory (pass it as the first CLI argument) in CI / real builds.
 *
 * Backend source:
 *   - By default, shallow-clones BACKEND_REPO_URL at BACKEND_REF into a temp directory.
 *     urban-dry-clean-crm-node is a PRIVATE repo, so an anonymous clone fails (git tries to
 *     prompt for credentials, which errors out non-interactively in CI) - set BACKEND_REPO_TOKEN
 *     to a token with read access to it (a fine-grained PAT is enough; CI passes this from a
 *     repo secret of the same name - see README.md "Building").
 *   - Set BACKEND_LOCAL_PATH to a local checkout instead (e.g. for side-by-side local dev with
 *     ../urban-dry-clean-crm-node) - skips the clone (and BACKEND_REPO_TOKEN) and builds that
 *     directory in place.
 *
 * Windows-only: relies on `tar` (bsdtar, built into Windows 10/Server 2019+) to extract the
 * Node.js zip, and `git` to clone the backend repo. This script only ever runs on Windows (CI's
 * windows-latest runner, or a Windows dev machine) - same as the app it's staging for.
 */
const { execFileSync } = require('node:child_process');
const https = require('node:https');
const fs = require('node:fs');
const path = require('node:path');
const os = require('node:os');

const NODE_VERSION = process.env.NATIVE_APP_NODE_VERSION || '22.11.0';
const NODE_ZIP_URL = `https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-win-x64.zip`;

const BACKEND_REPO_URL = process.env.BACKEND_REPO_URL || 'https://github.com/kumarrahul2908/urban-dry-clean-crm-node.git';
const BACKEND_REF = process.env.BACKEND_REF || 'main';
const BACKEND_LOCAL_PATH = process.env.BACKEND_LOCAL_PATH || '';
const BACKEND_REPO_TOKEN = process.env.BACKEND_REPO_TOKEN || '';

/** Embeds a token for HTTPS git auth (x-access-token works for both a PAT and a GITHUB_TOKEN). */
function withAuth(url, token) {
  if (!token || !url.startsWith('https://')) return url;
  return url.replace('https://', `https://x-access-token:${token}@`);
}

/** Never let a token reach the console, even though CI also masks registered secret values. */
function redact(text) {
  return BACKEND_REPO_TOKEN ? text.split(BACKEND_REPO_TOKEN).join('***') : text;
}

const repoRoot = path.resolve(__dirname, '..');
const targetArg = process.argv[2];
const targetDir = targetArg ? path.resolve(targetArg) : path.join(repoRoot, 'resources');
const backendOut = path.join(targetDir, 'backend');

function run(cmd, args, cwd) {
  console.log(redact(`[prepare-backend] $ ${cmd} ${args.join(' ')}  (in ${cwd})`));
  execFileSync(cmd, args, { cwd, stdio: 'inherit' });
}

function rmrf(p) {
  fs.rmSync(p, { recursive: true, force: true });
}

function copyDir(src, dest) {
  if (!fs.existsSync(src)) return;
  fs.mkdirSync(path.dirname(dest), { recursive: true });
  fs.cpSync(src, dest, { recursive: true });
}

function download(url, destPath) {
  return new Promise((resolve, reject) => {
    const file = fs.createWriteStream(destPath);
    const request = (u) => {
      https.get(u, (res) => {
        if (res.statusCode && res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
          request(res.headers.location);
          return;
        }
        if (res.statusCode !== 200) {
          reject(new Error(`GET ${u} -> HTTP ${res.statusCode}`));
          return;
        }
        res.pipe(file);
        file.on('finish', () => file.close(() => resolve()));
      }).on('error', reject);
    };
    request(url);
  });
}

async function stageNodeRuntime() {
  const nodeOutDir = path.join(backendOut, 'node');
  const nodeExeOut = path.join(nodeOutDir, 'node.exe');
  if (fs.existsSync(nodeExeOut)) {
    console.log('[prepare-backend] portable node.exe already staged, skipping download');
    return;
  }

  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'udc-node-'));
  const zipPath = path.join(tmpDir, 'node.zip');
  console.log(`[prepare-backend] downloading ${NODE_ZIP_URL}`);
  await download(NODE_ZIP_URL, zipPath);

  console.log('[prepare-backend] extracting node.exe');
  run('tar', ['-xf', zipPath, '-C', tmpDir], tmpDir);
  const extractedDir = path.join(tmpDir, `node-v${NODE_VERSION}-win-x64`);
  const extractedExe = path.join(extractedDir, 'node.exe');
  if (!fs.existsSync(extractedExe)) {
    throw new Error(`node.exe not found at ${extractedExe} after extracting the Node.js zip`);
  }

  fs.mkdirSync(nodeOutDir, { recursive: true });
  fs.copyFileSync(extractedExe, nodeExeOut);
  rmrf(tmpDir);
}

function getBackendSource() {
  if (BACKEND_LOCAL_PATH) {
    console.log(`[prepare-backend] using local backend checkout at ${BACKEND_LOCAL_PATH}`);
    return { dir: path.resolve(BACKEND_LOCAL_PATH), cleanup: () => {} };
  }
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'udc-backend-'));
  console.log(`[prepare-backend] cloning ${BACKEND_REPO_URL}#${BACKEND_REF}${BACKEND_REPO_TOKEN ? ' (authenticated)' : ''}`);
  const cloneUrl = withAuth(BACKEND_REPO_URL, BACKEND_REPO_TOKEN);
  run('git', ['clone', '--depth', '1', '--branch', BACKEND_REF, cloneUrl, tmpDir], process.cwd());
  return { dir: tmpDir, cleanup: () => rmrf(tmpDir) };
}

async function main() {
  const backend = getBackendSource();
  try {
    // 1. Build the backend fresh, so the packaged app always ships the current code.
    run('npm', ['ci'], backend.dir);
    run('npm', ['run', 'build'], backend.dir);

    // 2. Reset the staging area.
    rmrf(backendOut);
    fs.mkdirSync(backendOut, { recursive: true });

    // 3. Copy the pieces the running server needs (same set as the backend repo's Dockerfile).
    copyDir(path.join(backend.dir, 'dist'), path.join(backendOut, 'dist'));
    copyDir(path.join(backend.dir, 'public'), path.join(backendOut, 'public'));
    copyDir(path.join(backend.dir, 'db'), path.join(backendOut, 'db'));
    fs.copyFileSync(path.join(backend.dir, 'package.json'), path.join(backendOut, 'package.json'));
    fs.copyFileSync(path.join(backend.dir, 'package-lock.json'), path.join(backendOut, 'package-lock.json'));

    // 4. Production-only node_modules for the staged copy (no tsx/typescript/vitest/dev tooling).
    //    Every runtime dependency (mysql2, fastify, bcryptjs, zod, ...) is pure JavaScript, so it
    //    runs unmodified under the bundled portable Node below - no native/ABI rebuild needed.
    run('npm', ['ci', '--omit=dev'], backendOut);
  } finally {
    backend.cleanup();
  }

  // 5. Portable Node.js runtime, so the shop PC never needs Node.js installed separately.
  await stageNodeRuntime();

  console.log('[prepare-backend] done ->', backendOut);
}

main().catch((err) => {
  console.error('[prepare-backend] FAILED -', err.message);
  process.exitCode = 1;
});
