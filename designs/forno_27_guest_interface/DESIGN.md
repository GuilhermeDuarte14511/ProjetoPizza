---
name: Forno 27 Guest Interface
colors:
  surface: '#f8f9ff'
  surface-dim: '#d7dae2'
  surface-bright: '#f8f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f1f3fc'
  surface-container: '#ebeef6'
  surface-container-high: '#e5e8f0'
  surface-container-highest: '#dfe2eb'
  on-surface: '#181c22'
  on-surface-variant: '#59413a'
  inverse-surface: '#2d3137'
  inverse-on-surface: '#eef1f9'
  outline: '#8d7168'
  outline-variant: '#e1bfb5'
  surface-tint: '#ac3500'
  primary: '#a83300'
  on-primary: '#ffffff'
  primary-container: '#cc4916'
  on-primary-container: '#fffbff'
  inverse-primary: '#ffb59d'
  secondary: '#6646c4'
  on-secondary: '#ffffff'
  secondary-container: '#9c7efe'
  on-secondary-container: '#32008a'
  tertiary: '#006a3b'
  on-tertiary: '#ffffff'
  tertiary-container: '#268451'
  on-tertiary-container: '#f6fff4'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#ffdbd0'
  primary-fixed-dim: '#ffb59d'
  on-primary-fixed: '#390c00'
  on-primary-fixed-variant: '#832600'
  secondary-fixed: '#e8deff'
  secondary-fixed-dim: '#cdbdff'
  on-secondary-fixed: '#20005f'
  on-secondary-fixed-variant: '#4e2aab'
  tertiary-fixed: '#9af6b8'
  tertiary-fixed-dim: '#7ed99e'
  on-tertiary-fixed: '#00210f'
  on-tertiary-fixed-variant: '#00522d'
  background: '#f8f9ff'
  on-background: '#181c22'
  surface-variant: '#dfe2eb'
typography:
  display-lg:
    fontFamily: Inter
    fontSize: 34px
    fontWeight: '600'
    lineHeight: 42px
    letterSpacing: -0.02em
  display-md:
    fontFamily: Inter
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
  section-title:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  product-title:
    fontFamily: Inter
    fontSize: 22px
    fontWeight: '600'
    lineHeight: 28px
  price-lg:
    fontFamily: Inter
    fontSize: 26px
    fontWeight: '700'
    lineHeight: 32px
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 26px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  label-caps:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
    letterSpacing: 0.05em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  touch-target-min: 52px
  grid-margin: 32px
  gutter: 20px
  container-padding: 24px
  stack-gap: 16px
---

## Brand & Style
The design system is centered on an "Artisanal Warmth" narrative, bridging the gap between traditional pizzeria authenticity and modern digital convenience. The goal is to evoke an appetizing, welcoming, and high-end casual dining experience.

The style is **Modern/Corporate** with **Tactile** influences: 
- **Warm Minimalism:** Heavy use of the cream background (`#FFF8F3`) to reduce eye strain and provide a "paper-like" quality compared to sterile whites.
- **High-Quality Imagery:** The UI acts as a frame for high-resolution food photography. Surfaces are designed to disappear behind the vibrant colors of the ingredients.
- **Kiosk-Optimized:** Every element is sized for physical interaction, featuring large touch targets and simplified navigation to ensure accessibility for children and seniors alike.

## Colors
The palette is grounded in earth tones with high-contrast functional accents.
- **Primary (Terracotta):** Used for the main "Add to Cart" actions and critical navigation paths.
- **Secondary (Account):** A sophisticated purple reserved for loyalty and personalized profile interactions.
- **Background & Surfaces:** The Cream base ensures the interface feels warmer and more inviting than a standard tablet app. White is reserved strictly for interactive cards to create a clear "layering" effect.
- **Functional States:** Success (Forest Green), Alert (Amber), and Error (Crimson) follow standard semantic patterns but are slightly desaturated to maintain the artisanal aesthetic.

## Typography
Inter is used for its exceptional legibility on tablet displays. 
- **Hierarchical Contrast:** Use `display-lg` for category headers and welcome screens. 
- **Product Focus:** Product titles use `semibold` weights to stand out against white card backgrounds. 
- **Monetary Values:** Prices are always emphasized with a heavier weight and the Primary Terracotta color to ensure they are easily spotted during the selection process.
- **Readability:** Line heights are generous (min 1.4x) to assist users with visual impairments or those viewing the tablet from a distance.

## Layout & Spacing
The design uses a **Fixed Grid** model optimized for a 1280x800 landscape orientation.
- **Safe Zones:** A 32px outer margin ensures no content is clipped by tablet bezels or accidental palm touches.
- **Rhythm:** A 4px baseline grid is used, with most spacing following 8px increments.
- **Touch Targets:** No interactive element (buttons, toggles, list items) may be shorter than 52px. 
- **Navigation:** A persistent left-hand sidebar or bottom-bar (depending on flow depth) provides quick access to "Categories," "My Order," and "Help."

## Elevation & Depth
This design system utilizes **Tonal Layers** and **Ambient Shadows** to create a physical sense of depth on the flat glass screen.
- **Level 0 (Background):** The Cream surface (`#FFF8F3`), flat.
- **Level 1 (Cards/Items):** White surfaces with a very soft, diffused shadow (15% opacity Primary-Dark tint, 12px blur) to make them appear "lifted" and tappable.
- **Level 2 (Modals/Overlays):** Used for product customization or checkout. These feature a 20% background dimming (scrim) to focus the user’s attention.
- **Outlines:** Subtle borders (`#E8E1DC`) are used on Level 1 elements to maintain definition when overlapping.

## Shapes
The shape language is friendly and modern, utilizing significant rounding to avoid a "clinical" or overly technical feel.
- **Base Components:** 12px (`0.75rem`) corner radius for standard buttons and input fields.
- **Product Cards:** 24px (`1.5rem`) corner radius to create a soft, container-like look for food photography.
- **Selection States:** Use thick 3px inner strokes in Primary Terracotta to indicate active selection.

## Components
- **Buttons:** Primary buttons use a solid Terracotta fill with white text. Secondary buttons use an outline style with 2px weight. All buttons must maintain the 52px minimum height.
- **Product Cards:** Must feature a top-aligned image (aspect ratio 4:3), followed by the Product Title, a short description, and the Price. The entire card is the touch target for opening details.
- **Quantity Selector:** Large "+" and "-" buttons (min 56x56px) flanking a bold numerical value.
- **Chips:** Used for dietary filters (e.g., "Vegan," "Gluten-Free"). These use a soft-rounded pill shape with icons.
- **Lists:** Ingredient lists should have generous vertical padding (16px) and toggle switches for "Remove" actions.
- **Input Fields:** Used for special instructions; must trigger a large, kiosk-optimized numeric or alphabetical keyboard that doesn't obscure the input field.