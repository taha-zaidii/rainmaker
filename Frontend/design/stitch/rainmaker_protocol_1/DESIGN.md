---
name: Rainmaker Protocol
colors:
  surface: '#f8f9fb'
  surface-dim: '#d9dadc'
  surface-bright: '#f8f9fb'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f3f4f6'
  surface-container: '#edeef0'
  surface-container-high: '#e7e8ea'
  surface-container-highest: '#e1e2e4'
  on-surface: '#191c1e'
  on-surface-variant: '#434654'
  inverse-surface: '#2e3132'
  inverse-on-surface: '#f0f1f3'
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
  tertiary: '#434a57'
  on-tertiary: '#ffffff'
  tertiary-container: '#5b626f'
  on-tertiary-container: '#d7deee'
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
  tertiary-fixed: '#dce2f3'
  tertiary-fixed-dim: '#c0c7d6'
  on-tertiary-fixed: '#151c27'
  on-tertiary-fixed-variant: '#404754'
  background: '#f8f9fb'
  on-background: '#191c1e'
  surface-variant: '#e1e2e4'
typography:
  display-lg:
    fontFamily: Inter
    fontSize: 48px
    fontWeight: '700'
    lineHeight: 56px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '600'
    lineHeight: 40px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  headline-sm:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  body-lg:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
  label-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.05em
  headline-lg-mobile:
    fontFamily: Inter
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 4px
  container-margin: 24px
  gutter: 16px
  stack-xs: 4px
  stack-sm: 8px
  stack-md: 16px
  stack-lg: 24px
  stack-xl: 48px
---

## Brand & Style
The design system is engineered for a high-performance enterprise environment where data density meets algorithmic intelligence. The brand personality is authoritative, precise, and technologically advanced, designed to instill confidence in institutional users managing complex financial or technical protocols.

The visual style follows a **Corporate Modern** aesthetic with **Minimalist** tendencies. It prioritizes clarity and functional efficiency, utilizing significant white space to manage cognitive load without sacrificing the data density required for professional workflows. The primary interface is clean and systematic, while specific AI-driven features are highlighted using a distinct accent logic to signal machine-learning insights.

## Colors
This design system utilizes a strategic split-palette approach to differentiate human-triggered actions from system-generated intelligence.

- **Primary Blue (#1A56DB):** Used for standard interactive elements, navigation, and core branding. It represents stability and the foundational protocol.
- **Accent Orange (#F97316):** Reserved exclusively for AI-related components, automated insights, and predictive data points. This creates a clear mental model: orange equals "Machine Thinking."
- **Neutrals:** A range of cool grays is used for typography and structural borders to maintain a professional, calibrated atmosphere.
- **Backgrounds:** A tiered system of white and off-white (`#F9FAFB`) separates the workspace from global navigation containers.

## Typography
The system relies entirely on **Inter** for its neutral, systematic, and highly legible characteristics, particularly in data-heavy tables. 

- **Weight Strategy:** Use `600` (Semi-Bold) for headlines and `500` for interactive labels to ensure a clear visual hierarchy against dense body text.
- **Readability:** For tabular data and technical values, use a `tabular-nums` OpenType setting to ensure columns of numbers align perfectly.
- **Scale:** The type scale is intentionally tight. Large display sizes are reserved for dashboard overviews, while `body-md` (14px) serves as the primary workhorse for the majority of the application interface to maximize information density.

## Layout & Spacing
The layout employs a **Fluid Grid** system designed for 24/7 monitoring and high-intensity workflows. 

- **Grid Model:** A 12-column grid on desktop with 16px gutters allows for flexible dashboard widgets. 
- **Density:** The system uses a 4px base unit. Component internal padding is kept lean (e.g., 8px vertical padding on list items) to allow more data to be visible above the fold.
- **Breakpoints:** 
  - **Desktop (1280px+):** Full 12-column layout with persistent side navigation.
  - **Tablet (768px - 1279px):** 8-column layout; side navigation collapses to icons.
  - **Mobile (Below 768px):** 4-column fluid layout; margins reduce to 16px.

## Elevation & Depth
Depth is conveyed through **Tonal Layers** and subtle, functional shadows rather than decorative effects.

- **Level 0 (Base):** Background color `#F9FAFB`.
- **Level 1 (Cards/Containers):** Pure white `#FFFFFF` with a 1px border of `#E5E7EB`. Used for primary content areas.
- **Level 2 (Dropdowns/Popovers):** White background with a soft, diffused shadow (`0 4px 6px -1px rgb(0 0 0 / 0.1)`).
- **AI Elevation:** Orange elements do not use extra depth; they rely on color contrast and internal glows (`inner-shadow`) to signify their "active intelligence" status.

## Shapes
The shape language is structured and professional. A consistent **8px (0.5rem)** radius is applied to all cards and primary containers to soften the interface without appearing overly consumer-focused.

- **Small Components:** Buttons and input fields utilize the standard 8px radius.
- **Interactive Indicators:** Small 2px radius "pills" or tags are used within tables for status indicators to maintain high legibility at small scales.
- **Icons:** Use a consistent 2px stroke weight with slightly rounded joins to match the component radius.

## Components
- **Buttons:** 
  - *Standard:* Solid Blue (#1A56DB) for primary actions; Outlined for secondary.
  - *AI-Action:* Solid Orange (#F97316) only when the action triggers an automated process or generative insight.
- **Input Fields:** 1px border (#D1D5DB) that shifts to Primary Blue on focus. Labels sit above the field in `label-sm`.
- **Cards:** White background, 8px radius, 1px border. No shadow by default to maintain a clean, flat aesthetic.
- **Chips/Badges:** 
  - Use subtle background tints (Blue-50, Orange-50) with high-contrast text for status tracking.
- **Data Tables:** High-density rows (40px height). Zebra striping is avoided in favor of subtle hover states and 1px horizontal dividers.
- **AI Insight Panel:** A specialized container with a subtle Orange-50 border and a "sparkle" icon to denote system-generated content.