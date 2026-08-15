---
name: design-check
description: Use when creating or editing any UI component, screen, or style in Rainmaker. Verifies spacing, color, and typography against the design tokens and checks accessibility basics before finalizing.
---

When this skill is active:
1. Read /docs/design/tokens.md before writing any styles.
2. Never use raw hex/px values — resolve to the nearest existing token.
3. Check contrast ratio is 4.5:1+ for text.
4. Confirm interactive elements are keyboard-navigable.
5. If a new pattern doesn't fit existing tokens, flag it — don't silently invent one.