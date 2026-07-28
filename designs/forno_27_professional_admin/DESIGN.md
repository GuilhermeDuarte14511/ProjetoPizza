---
name: Forno 27 Professional Admin
colors:
  surface: '#f8f9ff'
  surface-dim: '#d5dae5'
  surface-bright: '#f8f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#eef4ff'
  surface-container: '#e9eef9'
  surface-container-high: '#e3e8f3'
  surface-container-highest: '#dde3ee'
  on-surface: '#161c24'
  on-surface-variant: '#59413a'
  inverse-surface: '#2b3139'
  inverse-on-surface: '#ebf1fc'
  outline: '#8d7168'
  outline-variant: '#e1bfb5'
  surface-tint: '#ac3500'
  primary: '#a83300'
  on-primary: '#ffffff'
  primary-container: '#cc4916'
  on-primary-container: '#fffbff'
  inverse-primary: '#ffb59d'
  secondary: '#ad3305'
  on-secondary: '#ffffff'
  secondary-container: '#ff6e40'
  on-secondary-container: '#631800'
  tertiary: '#00628b'
  on-tertiary: '#ffffff'
  tertiary-container: '#007cae'
  on-tertiary-container: '#fcfcff'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#ffdbd0'
  primary-fixed-dim: '#ffb59d'
  on-primary-fixed: '#390c00'
  on-primary-fixed-variant: '#832600'
  secondary-fixed: '#ffdbd0'
  secondary-fixed-dim: '#ffb59f'
  on-secondary-fixed: '#3a0a00'
  on-secondary-fixed-variant: '#852400'
  tertiary-fixed: '#c7e7ff'
  tertiary-fixed-dim: '#85cfff'
  on-tertiary-fixed: '#001e2e'
  on-tertiary-fixed-variant: '#004c6c'
  background: '#f8f9ff'
  on-background: '#161c24'
  surface-variant: '#dde3ee'
typography:
  display-financial:
    fontFamily: Inter
    fontSize: 30px
    fontWeight: '700'
    lineHeight: 36px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Inter
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 34px
    letterSpacing: -0.01em
  headline-lg-mobile:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 30px
  headline-md:
    fontFamily: Inter
    fontSize: 22px
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
  label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.02em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  sidebar-width: 260px
  topbar-height: 64px
  gutter: 24px
  card-padding: 20px
  stack-gap: 16px
  section-margin: 32px
---

## Brand & Style

This design system establishes a sophisticated, operationally-focused environment for high-end pizzeria management. It moves away from cliché "pizzeria" tropes (checkered patterns, cartoonish illustrations) in favor of a **Corporate Modern** aesthetic that emphasizes efficiency, precision, and culinary authority.

The system utilizes a high-contrast layout where a dark, structured navigation anchor (the sidebar) meets a crisp, light-gray workspace. This creates a clear mental model: the sidebar is the "control room," and the main stage is the "operational floor." The emotional response is one of reliability and calm control, even during peak kitchen hours. 

Key design principles include:
- **Substantiality:** Elements feel grounded through deliberate use of white space and subtle, realistic shadows.
- **Precision:** Clean lines and a strict grid system reflect the mathematical precision of inventory and financial management.
- **Warmth through Accent:** The terracotta primary color provides a singular, high-energy focal point that nods to the heat of the oven without overwhelming the professional interface.

## Colors

The palette is anchored by a professional **Dark Graphite** sidebar and a neutral **Very Light Gray** workspace. This ensures the **Terracotta** primary color remains an "action" color, used exclusively for primary buttons, active states, and critical paths.

- **Primary & Secondary Orange:** Reserved for high-priority interactions and brand presence.
- **Surface Strategy:** Backgrounds utilize the light gray (#F5F6F8) to reduce glare, while functional cards use pure white (#FFFFFF) to pop against the canvas.
- **Semantic Clarity:** Status colors are saturated and distinct to ensure immediate recognition of order states (e.g., "In Oven" vs. "Delivered").
- **Typography Tiers:** Primary text (#20242A) provides near-black contrast for maximum legibility, while secondary text (#6B7280) is used for metadata and labels.

## Typography

The system uses **Inter** for its systematic, utilitarian clarity. It scales from dense data tables to high-impact financial dashboards.

- **Financial Emphasis:** A specific `display-financial` tier is used for revenue, average ticket, and cost metrics to ensure they are the most prominent elements on the page.
- **Hierarchy:** Headlines use semi-bold weights with slight negative letter-spacing to appear more compact and modern.
- **Utility:** Small labels (12px) should be used for table headers and secondary metadata, often in all-caps or medium weights to maintain structure.

## Layout & Spacing

The layout follows a **Fixed Sidebar + Fluid Content** model. The interface is divided into three distinct zones:

1.  **Sidebar (Left):** 260px width. Houses navigation groups: 'Operação', 'Financeiro', and 'Gestão'.
2.  **Top Bar (Global):** 64px height. Contains breadcrumbs on the left, a centered global search, and operational status indicators (Cashier/Tables) on the right.
3.  **Content Canvas:** Uses a fluid 12-column grid with 24px gutters. 

**Mobile Adaptations:**
- On tablet, the sidebar collapses into an icon-only rail or a hidden drawer.
- On mobile, cards stack vertically, and padding reduces to 16px to maximize horizontal space for data.

## Elevation & Depth

This design system uses **Tonal Layers** combined with **Ambient Shadows** to create a sense of organized stacking.

- **Level 0 (Background):** #F5F6F8. The base canvas.
- **Level 1 (Cards/Surface):** Pure #FFFFFF. These use a subtle 1px border (#E4E7EC) and a soft shadow (0px 4px 6px -1px rgba(0,0,0,0.05)) to appear slightly raised.
- **Level 2 (Popovers/Modals):** These use a more pronounced shadow (0px 10px 15px -3px rgba(0,0,0,0.1)) and a background blur to focus user attention.
- **Sidebar Depth:** The Dark Graphite sidebar is treated as the lowest layer or "foundation," providing a solid visual anchor that doesn't require shadows to separate itself from the light workspace.

## Shapes

The design uses a **Rounded** language (8px / 0.5rem base) to soften the professional aesthetic and make the interface feel more approachable for staff.

- **Base Components:** Buttons, inputs, and small cards use 8px corners.
- **Large Containers:** Main content cards and modals use `rounded-lg` (16px) to create a more distinct containerized feel.
- **Interactive States:** Focus states should mirror the object's roundedness with a 2px offset ring in the Primary color.

## Components

### Buttons
- **Primary:** Terracotta background, white text. 8px border radius.
- **Secondary:** White background, light gray border (#E4E7EC), primary text.
- **Icon Buttons:** Ghost style (no background) for secondary actions; contained for primary actions.

### Navigation (Sidebar)
- **Groups:** 'Operação', 'Financeiro', 'Gestão' as capitalized labels with 12px Medium weight and 40% opacity.
- **Items:** Dark graphite background. Active state uses a vertical terracotta bar on the left edge and a subtle background highlight.

### Cards & Indicators
- **Operational Indicators:** Located in the top bar. Use small dots (Success Green for "Open Cashier") and clear numerical labels for "Occupied Tables."
- **Financial Cards:** Large bold values centered, with a small percentage indicator (Success or Error) for trend analysis.

### Inputs & Fields
- **Linear Style:** 1px border (#E4E7EC) that transitions to Terracotta on focus.
- **Search:** Global search in the top bar should be wide (min 300px) with a subtle "CMD+K" hint.

### Chips/Tags
- **Status Tags:** Lightly tinted backgrounds based on status colors (e.g., Success green at 10% opacity) with high-contrast text of the same hue.