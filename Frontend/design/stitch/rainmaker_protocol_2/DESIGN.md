---
name: Rainmaker Protocol
colors:
  surface: '#f8f9ff'
  surface-dim: '#cfdbec'
  surface-bright: '#f8f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#eef4ff'
  surface-container: '#e4efff'
  surface-container-high: '#dee9fb'
  surface-container-highest: '#d8e3f5'
  on-surface: '#111c29'
  on-surface-variant: '#434654'
  inverse-surface: '#26313f'
  inverse-on-surface: '#e9f1ff'
  outline: '#737686'
  outline-variant: '#c3c5d7'
  surface-tint: '#1353d8'
  primary: '#003fb1'
  on-primary: '#ffffff'
  primary-container: '#1a56db'
  on-primary-container: '#d4dcff'
  inverse-primary: '#b5c4ff'
  secondary: '#9d4300'
  on-secondary: '#ffffff'
  secondary-container: '#fd761a'
  on-secondary-container: '#5c2400'
  tertiary: '#852b00'
  on-tertiary: '#ffffff'
  tertiary-container: '#ad3b00'
  on-tertiary-container: '#ffd4c5'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#dbe1ff'
  primary-fixed-dim: '#b5c4ff'
  on-primary-fixed: '#00174d'
  on-primary-fixed-variant: '#003dab'
  secondary-fixed: '#ffdbca'
  secondary-fixed-dim: '#ffb690'
  on-secondary-fixed: '#341100'
  on-secondary-fixed-variant: '#783200'
  tertiary-fixed: '#ffdbcf'
  tertiary-fixed-dim: '#ffb59a'
  on-tertiary-fixed: '#380d00'
  on-tertiary-fixed-variant: '#802a00'
  background: '#f8f9ff'
  on-background: '#111c29'
  surface-variant: '#d8e3f5'
typography:
  page-title:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
    letterSpacing: -0.02em
  section-heading:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 24px
    letterSpacing: -0.01em
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-md:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '500'
    lineHeight: 18px
  helper-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  base: 4px
  gap-field: 16px
  gap-card: 24px
  padding-card: 24px
  sidebar-primary-width: 88px
  sidebar-secondary-width: 240px
  topbar-height: 64px
---

## Brand & Style
The design system is engineered for a high-performance enterprise environment where clarity, speed, and precision are paramount. The aesthetic is rooted in **Modern SaaS Minimalism**, drawing inspiration from developer-centric tools that prioritize information density without sacrificing visual elegance.

The UI targets a professional audience that values discipline and reliability. It employs a rigorous systematic approach: heavy use of whitespace to separate complex data modules, a restrained color palette to reduce cognitive load, and a clear hierarchy that guides the user through dense ERP workflows. The atmosphere is calm, trustworthy, and technologically advanced, particularly when punctuated by specific AI-driven accents.

## Colors
The color architecture is built on a foundation of "Rainmaker Blue" to establish institutional trust. 

- **Primary & Action:** #1A56DB is used for primary actions and global navigation. Hover states should shift to #1E429F.
- **AI Intelligence Layer:** #F97316 is reserved strictly for AI-augmented features. It must not be used for standard warnings or actions. This color signals "machine intelligence at work," used for sparkles, AI-suggested fields, and automated insight badges.
- **Surface Strategy:** The UI uses a "layered white" approach. The base page background is #F7F9FC to provide contrast for pure white (#FFFFFF) cards and containers, which house the primary content.
- **Typography & UI:** All text follows a strict grayscale to ensure maximum legibility against the light surfaces.

## Typography
The system uses **Inter** exclusively to leverage its exceptional legibility in data-heavy interfaces. 

- **Vertical Rhythm:** Line heights are strictly optimized for a 4px baseline grid.
- **Hierarchy:** Page titles utilize a slight negative letter-spacing to appear more cohesive at larger scales. Labels and helper text are essential for the ERP context; labels use a Medium weight (500) to distinguish them from editable body text.
- **Data Display:** For tabular data, use `body-md` with tabular lining figures if available to ensure columns of numbers align perfectly.

## Layout & Spacing
The layout follows a **Fixed-Fluid Hybrid** model optimized for 1440px desktop displays.

- **Navigation Architecture:** 
  - A primary **Global Sidebar** (88px) anchored to the left provides high-level module switching. 
  - A **Secondary Navigation** drawer (240px) handles sub-module tree structures and filtering.
- **Grid System:** Content resides in a fluid container with a max-width of 1440px. Internal card layouts utilize a 24px gutter.
- **Spacing Scale:** All dimensions are multiples of 4px. Use 16px for internal element grouping (e.g., label to input) and 24px for component-to-component spacing.

## Elevation & Depth
Depth is communicated through **Subtle Layering** rather than dramatic shadows. 

- **Surface Level 0:** The #F7F9FC page background.
- **Surface Level 1 (Cards):** Pure white #FFFFFF with a 1px solid border in #E5E9F0. Shadows should be nearly imperceptible: `0 1px 3px 0 rgba(0, 0, 0, 0.02), 0 1px 2px -1px rgba(0, 0, 0, 0.03)`.
- **Surface Level 2 (Modals/Popovers):** These use a more pronounced but still soft shadow to indicate temporary overlay status over the primary UI.
- **Interactive States:** Buttons and interactive cards do not lift on hover; instead, they utilize subtle background color shifts or border-color darkening to maintain the "flat" professional aesthetic.

## Shapes
The shape language is disciplined and geometric. 

- **Containers:** Cards and large modules use an **8px (rounded-lg)** radius to provide a modern, approachable feel while maintaining a structural look.
- **Controls:** Functional elements like buttons, input fields, and checkboxes use a tighter **6px** radius. This distinction helps users subconsciously differentiate between "containers of information" and "interactable tools."
- **Icons:** Use a 1.5pt or 2pt stroke weight with slightly rounded caps to match the typography's visual weight.

## Components
- **Buttons:** Primary buttons are #1A56DB with white text, 40px height. Secondary buttons use a white background with a #E5E9F0 border.
- **AI Activity Feeds:** These cards utilize a subtle #FFF3E8 left-border accent and the AI Sparkle icon in #F97316. Text within these feeds may use a slightly more relaxed leading for readability.
- **Data Tables:** Headers use a #F7F9FC background with `label-md` text. Rows are 48px high with 1px #E5E9F0 bottom borders. No vertical borders.
- **Input Fields:** 40px height, #FFFFFF background, #E5E9F0 border. On focus, the border shifts to #1A56DB with a subtle 2px outer glow in #EBF2FF.
- **KPI Cards:** Display a large "Value" (Page Title style) and a "Trend" indicator (Success or Danger colors). These are always housed in the standard 8px radius card.
- **Navigation Items:** Active states in the sidebar use a high-contrast white icon on the blue background; in secondary navigation, they use a subtle blue tint background and #1A56DB text.