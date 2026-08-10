#!/usr/bin/env node
/**
 * Enforces docs/DESIGN_SYSTEM_TOKENS.md: raw hex colours and arbitrary
 * Tailwind bracket values are only allowed inside core/theme/tokens.css,
 * which is the single source of truth every other file is meant to consume
 * via semantic utilities (bg-primary, text-danger, etc.).
 *
 * Deliberately dependency-free (no stylelint/postcss plugin) — the project's
 * UI dependency set is intentionally minimal (see RAINMAKER_MASTER_CONTEXT.md
 * §4.0: "Do not introduce ... any UI dependency without asking me first"),
 * and a plain Node script over the existing Angular source tree is enough to
 * catch drift without adding a devDependency for it.
 */
const fs = require("fs");
const path = require("path");

const SRC_ROOT = path.join(__dirname, "..", "src", "app");
const EXCLUDED_FILES = new Set([path.join(SRC_ROOT, "core", "theme", "tokens.css")]);
const SCANNED_EXTENSIONS = new Set([".ts", ".html", ".css"]);

// #abc, #abcd, #aabbcc, #aabbccdd — word-boundary guarded so it doesn't
// false-positive on things like a URL fragment or a git SHA in a comment.
const HEX_COLOR = /#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})\b/g;

// Arbitrary-value Tailwind utilities for the properties tokens.css actually
// governs: color, font-size, radius, spacing. `bg-[#fff]`, `text-[13px]`,
// `rounded-[6px]`, `p-[10px]`, etc.
//
// Deliberately NOT flagged: w/h/max-w/max-h/min-w/min-h/top/left/right/bottom.
// Those are one-off page-container and component dimensions (a careers-page
// max-width and a modal's max-width have no reason to change together the
// way two uses of --color-danger do), not a shared design-system scale —
// tokenizing them would be inventing structure that doesn't reflect real
// reuse. `content-[...]` and `grid-cols-[...]` are structural for the same
// reason and were never in scope.
const ARBITRARY_VALUE = /\b(?:bg|text|border|ring|shadow|from|via|to|fill|stroke|p|px|py|pt|pb|pl|pr|m|mx|my|mt|mb|ml|mr|gap|rounded)-\[[^\]]+\]/g;

function walk(dir, files = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      walk(fullPath, files);
    } else if (SCANNED_EXTENSIONS.has(path.extname(entry.name)) && !EXCLUDED_FILES.has(fullPath)) {
      files.push(fullPath);
    }
  }
  return files;
}

function findViolations(filePath) {
  const text = fs.readFileSync(filePath, "utf8");
  const violations = [];
  const lines = text.split("\n");

  lines.forEach((line, index) => {
    for (const match of line.matchAll(HEX_COLOR)) {
      violations.push({ line: index + 1, kind: "raw hex colour", snippet: match[0] });
    }
    for (const match of line.matchAll(ARBITRARY_VALUE)) {
      violations.push({ line: index + 1, kind: "arbitrary bracket value", snippet: match[0] });
    }
  });

  return violations;
}

function main() {
  const files = walk(SRC_ROOT);
  let totalViolations = 0;

  for (const file of files) {
    const violations = findViolations(file);
    if (violations.length === 0) continue;

    totalViolations += violations.length;
    const relativePath = path.relative(process.cwd(), file);
    for (const v of violations) {
      console.error(`${relativePath}:${v.line}  ${v.kind}: ${v.snippet}`);
    }
  }

  if (totalViolations > 0) {
    console.error(
      `\n${totalViolations} design-token violation(s) found. Raw hex colours and arbitrary ` +
        `bracket values belong only in core/theme/tokens.css — use a semantic utility ` +
        `(bg-primary, text-danger, rounded-card, ...) everywhere else.`
    );
    process.exit(1);
  }

  console.log(`Design token check passed — ${files.length} files scanned, 0 violations.`);
}

main();
