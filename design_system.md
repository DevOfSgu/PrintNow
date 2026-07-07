---
name: PrintNow
colors:
  surface: '#f9f9f9'
  surface-dim: '#dadada'
  surface-bright: '#f9f9f9'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f3f3f3'
  surface-container: '#eeeeee'
  surface-container-high: '#e8e8e8'
  surface-container-highest: '#e2e2e2'
  on-surface: '#1a1c1c'
  on-surface-variant: '#5b403f'
  inverse-surface: '#2f3131'
  inverse-on-surface: '#f1f1f1'
  outline: '#906f6e'
  outline-variant: '#e4bdbc'
  surface-tint: '#bc0f2a'
  primary: '#b20024'
  on-primary: '#ffffff'
  primary-container: '#d62839'
  on-primary-container: '#fff2f1'
  inverse-primary: '#ffb3b1'
  secondary: '#675b5e'
  on-secondary: '#ffffff'
  secondary-container: '#efdee1'
  on-secondary-container: '#6d6164'
  tertiary: '#585757'
  on-tertiary: '#ffffff'
  tertiary-container: '#706f6f'
  on-tertiary-container: '#f7f4f3'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#ffdad8'
  primary-fixed-dim: '#ffb3b1'
  on-primary-fixed: '#410007'
  on-primary-fixed-variant: '#92001c'
  secondary-fixed: '#efdee1'
  secondary-fixed-dim: '#d2c3c6'
  on-secondary-fixed: '#22191c'
  on-secondary-fixed-variant: '#4f4447'
  tertiary-fixed: '#e5e2e1'
  tertiary-fixed-dim: '#c8c6c5'
  on-tertiary-fixed: '#1c1b1b'
  on-tertiary-fixed-variant: '#474746'
  background: '#f9f9f9'
  on-background: '#1a1c1c'
  surface-variant: '#e2e2e2'
typography:
  headline-xl:
    fontFamily: Inter
    fontSize: 48px
    fontWeight: '700'
    lineHeight: 56px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
    letterSpacing: -0.01em
  headline-lg-mobile:
    fontFamily: Inter
    fontSize: 28px
    fontWeight: '700'
    lineHeight: 36px
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  label-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
    letterSpacing: 0.01em
  label-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 8px
  xs: 4px
  sm: 12px
  md: 24px
  lg: 40px
  xl: 64px
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 48px
---

## Brand & Style

The brand identity is built on reliability, speed, and precision. It serves as a professional bridge between creative users and industrial print shops. The visual style follows a **Modern / Corporate** aesthetic with a focus on high-utility flat design. 

The personality is direct and efficient. We avoid unnecessary ornamentation or skeuomorphism, favoring clear information hierarchy and purposeful whitespace. The UI should feel like a high-end productivity tool—dependable enough for a business owner, yet approachable enough for a student.

## Colors

The palette is anchored by a high-energy Primary Dark Red, used strategically for actions and brand moments. The Secondary Light Pink serves as a soft structural alternative to harsh grays, providing warmth to the background layers without sacrificing professionalism.

- **Primary (#D62839):** Used for primary buttons, active states, and critical brand accents.
- **Secondary (#FDECEF):** Used for large background areas, section containers, and subtle hover states.
- **Surface / Neutral:** Deep charcoal (#1A1A1A) for maximum text legibility and soft grays for borders.

## Typography

This design system utilizes **Inter** exclusively to maintain a systematic, utilitarian feel. The hierarchy relies on substantial weight contrasts between headlines and body text. 

- **Headlines:** Set with tight letter-spacing and bold weights to command attention.
- **Body:** Optimized for readability with generous line-heights.
- **Labels:** Used for UI metadata, navigation items, and button text, often utilizing medium or semibold weights for clarity at smaller sizes.

## Layout & Spacing

The layout follows a strict **Fluid Grid** logic based on an 8px base unit. 

- **Desktop:** 12-column grid with 24px gutters. Maximum content width is 1280px.
- **Tablet:** 8-column grid with 20px gutters.
- **Mobile:** 4-column grid with 16px gutters and 16px side margins.

Horizontal spacing should be used to group related print services, while vertical spacing (using the `lg` and `xl` tokens) should clearly separate different stages of the ordering flow.

## Elevation & Depth

To maintain a "Modern Flat" aesthetic, this design system avoids heavy drop shadows. Depth is communicated through **Tonal Layers** and **Low-Contrast Outlines**.

- **Level 0 (Background):** Primary background uses the Secondary Light Pink (#FDECEF).
- **Level 1 (Cards/Containers):** Pure white surfaces with a 1px solid border in a subtle neutral-200.
- **Level 2 (Interaction):** On hover, cards do not lift; instead, they receive a slightly thicker 2px border in the Primary Red or a very soft 4px ambient blur with 5% opacity to indicate focus.

## Shapes

The design system utilizes a **Rounded** shape language to soften the industrial nature of printing services.

- **Standard (8px):** Applied to buttons, input fields, and small cards.
- **Large (16px):** Reserved for main content containers and featured shop modules.
- **Pill:** Only used for status indicators (e.g., "In Progress", "Ready for Pickup").

## Components

### Buttons
- **Primary:** Background #D62839, Text #FFFFFF. 8px border-radius. No shadow.
- **Secondary:** Background transparent, Border 1px #D62839, Text #D62839.
- **Padding:** 12px vertical, 24px horizontal for standard buttons.

### Input Fields
- White background with a 1px #E5E5E5 border. 
- On focus: Border changes to #D62839 with a 2px offset ring.
- Labels are always positioned above the input in `label-md` semibold.

### Cards
- White background, 8px border-radius, 1px light gray stroke.
- Internal padding should follow the `md` (24px) spacing token.

### Chips & Status
- Small, 12px font size, pill-shaped.
- Use low-saturation backgrounds with high-saturation text for readability (e.g., soft green background with dark green text for "Completed").

### Shop Lists
- Use a clean list format with 16px spacing between items. 
- Thumbnails for shop logos should have a 4px radius to distinguish them from larger UI cards.
