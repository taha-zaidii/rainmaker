# Enterprise Design System & Tokens

## 1. Single Source of Truth
To ensure UI/UX consistency across Rainmaker's modules (Admin, HRMS, Careers, LMS), all styling is anchored to CSS token variables defined in `Frontend/src/app/core/theme/tokens.css`.
- **Theme Variables**: Colors, fonts, shadows, and layout spacing are driven exclusively by these tokens.
- **Centralized Propagation**: A single variable change (e.g., `--color-primary`) immediately propagates to every rendered component across the platform.

## 1a. Enforcement
`npm run lint:tokens` (`Frontend/scripts/check-design-tokens.js`, gated into `npm run build`) fails the build on any raw hex colour or arbitrary-value color/font-size/spacing/radius utility (`bg-[#fff]`, `text-[13px]`, ...) found outside `tokens.css`. Page-container and component one-off dimensions (`max-w-[...]`, `w-[...]`, `h-[...]`) are deliberately not enforced — those don't share a "change together" meaning the way a color or type size does, so tokenizing them would invent structure the design doesn't have.

Typography beyond Tailwind's own `text-xs`–`text-2xl` defaults (which are left alone) is covered by a small literal-pixel scale — `--text-10/11/13/15/17/19/28/38` — named by value rather than role (caption/label/...) since the same pixel value serves different roles on different screens today; inventing a false semantic name would be worse than an honest numeric one.

## 2. Reusable Component Primitives
Feature modules must not redefine base structural elements like cards or tables. All UIs are built using declarative shared primitives found in `shared/components/ui/`:

### `<rm-table>`
A dynamic structural table wrapper that projects user content (`<ng-content>`) inside a standardized border, padding, and background model. Eliminates raw `table` elements carrying redundant classes.

### `<rm-drawer>`
A reactive slide-out panel used heavily in candidate evaluation and AI-assisted workflows. 
- **Configuration**: Exposes Signal inputs for `icon`, `title`, `subtitle`, and `variant`.
- **Variants**: Supports a `'default'` theme for standard HR operations, and an `'ai'` theme (which leverages the reserved AI-orange UI cues) when presenting generative feedback.

## 3. Angular 19 Reactive State
- Components should leverage **Signals** for low-latency, localized DOM updates.
- By decoupling structural primitives from data, we enforce a clean unidirectional data flow that avoids redundant re-renders and bloated DOM trees.
