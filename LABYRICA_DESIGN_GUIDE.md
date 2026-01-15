# Labyrica Design Guide

**A comprehensive design system specification for creating software with the Labyrica visual theme.**

This guide provides all design tokens, patterns, and guidelines needed to build applications that match the Labyrica aesthetic. Use this document when generating new software or interfaces to ensure visual consistency.

---

## 1. Core Identity

### Visual Character
- **Tactical Monochrome**: High-contrast black backgrounds with white/light grey text
- **Industrial Precision**: Sharp edges, minimal decoration, data-focused clarity
- **Layered Glass Aesthetic**: Semi-transparent overlays with backdrop blur effects
- **Technical Sophistication**: Clean, modern sans-serif typography with precise spacing

### Brand Pillars
- **Precise**: Everything aligns to a clear grid; no casual "rounded app" feel
- **Calm Under Load**: Dense information is allowed, but never chaotic
- **Technical Yet Human**: System UI typography with just enough breathing room and motion

---

## 2. Design Tokens

### Colors

#### Backgrounds
- **`background`**: `#000000` (Pure black) - Primary app background, full pages, root layouts
- **`surface`**: `#111111` (Slightly lighter black) - Cards, panels, nav bars, modals, foreground surfaces

#### Text Colors
- **`text.primary`**: `#ffffff` (Pure white) - Main body text, primary headings
- **`text.secondary`**: `#999999` (Mid grey) - Descriptions, meta data, helper text
- **`text.accent`**: `#cccccc` (Light grey) - Labels, subtle UI hints, less important metrics

#### Borders
- **`border.primary`**: `#222222` (Dark grey) - Default borders, card outlines, separators
- **`border.secondary`**: `#333333` (Slightly lighter grey) - Inner dividers, table row separators

#### Status Colors
- **`status.success`**: `#40F99B` (Bright green) - Success states, healthy systems, positive KPIs
- **`status.error`**: `#FF3333` (Red) - Destructive actions, error banners, critical alerts
- **`status.warning`**: `#F5A623` (Amber) - Warnings, risk indicators, non-blocking issues

#### Overlays
- **`overlay.modal`**: `rgba(0, 0, 0, 0.85)` - Full-screen modal backdrops
- **`overlay.dropdown`**: `rgba(0, 0, 0, 0.95)` - Dropdowns, command palettes, flyouts

**Critical Rule**: Always use theme tokens, never hardcode hex values like `#000` or `#111` directly in components.

### Typography

#### Font Families
- **Primary UI**: `'Inter', -apple-system, BlinkMacSystemFont, sans-serif`
- **Code/Technical**: `'SF Mono', Menlo, monospace`

#### Font Sizes
- `xs`: `0.75rem` - Tiny labels, helper text
- `sm`: `0.875rem` - Captions, footers
- `base`: `1rem` - Body copy
- `lg`: `1.125rem` - Lead text, key data
- `xl`: `1.25rem` - Subheadings
- `2xl`: `1.5rem` - Section headings
- `3xl`: `1.875rem` - Page titles
- `4xl`: `2.25rem` - Hero subtitles
- `5xl`: `3rem` - Hero titles

#### Line Height & Letter Spacing
- **Line Height**: `tight: 1.2`, `normal: 1.5`, `relaxed: 1.7`
- **Letter Spacing**: `tight: -0.02em`, `normal: 0`, `wide: 0.05em`

**Usage**: Prefer `normal` or `relaxed` line-height for dense content; `tight` for large titles. Use `tight` letter-spacing with large sizes for sharp, industrial headings.

### Spacing

#### Containers
- **Max Width**: `1440px` (centered with `mx-auto`)
- **Padding**: `default: 1.5rem`, `sm: 2rem`, `lg: 3rem`

#### Grid & Gaps
- **Default Gap**: `1.5rem`
- **Column Gap**: `2rem` (for complex layouts)

#### Vertical Rhythm
- **Major Sections**: `4-6rem` vertical padding (`py-16` to `py-24`)
- **Within Sections**: `1-2rem` spacing between elements

### Border Radius

**Minimal radius for industrial look**:
- `sm`: `2px`
- `default`: `3px`
- `lg`: `4px`
- `xl`: `6px`

**Critical**: Use small radii (2-4px) for industrial, non-playful look. Avoid pill buttons or large rounded corners.

### Shadows

**Very subtle shadows** (mainly for layer separation on dark backgrounds):
- `sm`: `0 1px 2px rgba(0, 0, 0, 0.1)`
- `default`: `0 2px 4px rgba(0, 0, 0, 0.1)`
- `lg`: `0 4px 8px rgba(0, 0, 0, 0.1)`

**Avoid**: Large, "floaty card" shadows. Keep shadows minimal and functional.

### Motion & Effects

#### Transitions
- `default`: `all 0.2s ease`
- `fast`: `all 0.1s ease`
- `slow`: `all 0.3s ease`

**Apply to**: opacity, transform, color changes. Avoid over-animating layout.

#### Backdrop Blur
- `default`: `blur(8px)`
- `strong`: `blur(16px)`

**Usage**: Combine with dark overlays (`bg-black/40` + `backdrop-blur-[4px]`) for glassy panel effects.

---

## 3. Layout & Structure

### Page Layout

#### Top Navigation
- **Position**: Fixed at top (`fixed top-0 left-0 right-0 z-50`)
- **Background**: Dark (`background` or `surface`), becomes more opaque on scroll (`bg-black/90`)
- **Structure**: Single row with logo left, navigation right (desktop), compact menu icon (mobile)
- **Border**: Subtle bottom border (`border-white/5`)

#### Content Container
- **Max Width**: `1440px` centered (`max-w-[1440px] mx-auto`)
- **Padding**: `px-4` (mobile), `px-6` (tablet), `px-8` (desktop)
- **Mobile**: Full width with padding

#### Vertical Rhythm
- **Major Sections**: `4-6rem` vertical padding (`py-16`, `py-24`)
- **Within Sections**: `1-2rem` spacing (`space-y-*`)

### Information Density

**Safe to show complex data** and multiple stacked sections. Mitigate complexity with:
- Clear section titles/labels in translucent tags
- Horizontal dividers (`border-white/5` or `border.primary`)
- Logical grouping into cards with `surface` background

---

## 4. Components & Patterns

### 4.1 Navigation

#### Desktop Navigation
- **Background**: Dark (`background` or `surface`), subtle bottom border (`border-white/5`)
- **Items**: Medium spacing (`space-x-8`), uppercase or tracking-wide text
- **Active State**: White text (`text-white`) + **thin underline** (`h-[1px] bg-white/80`)
- **Inactive State**: `text-gray-400` → `hover:text-white` transition

#### Mobile Navigation
- **Dropdown**: Full-width with `bg-black/95` overlay
- **Animation**: Smooth translate/opacity transition (`translate-y-0 opacity-100`)
- **Active Item**: Subtle background tint (`bg-white/5`) + small indicator dot

**Key Pattern**: Active state = **line + color** change, never just color.

### 4.2 Hero Sections

#### Pattern Structure
1. **Full viewport height** (`min-h-screen`)
2. **Animated background layer** (optional 3D network visualization)
3. **Dark overlay** for readability: `bg-black/20` (mobile), easing off on larger screens
4. **Text on glassy panels**: `bg-black/40` + `backdrop-blur-[4px]`

#### Content Hierarchy
1. **Small tagline badge**: Uppercase, tracking-wide, translucent background
2. **Secondary heading**: Medium size, light weight
3. **Primary headline**: Large (`text-4xl` to `text-5xl`), max 12-14 characters wide
4. **Body copy**: Short, scannable paragraphs
5. **Primary CTA**: White button with black text

**Use for**: Landing screens, dashboard overview sections, major workflow entry points.

### 4.3 Buttons & CTAs

#### Primary Button
- **Background**: White (`#ffffff`)
- **Text**: Black (`#000000`)
- **Hover**: Slight dim (`hover:bg-white/90`)
- **Typography**: Uppercase, medium weight, tracking-wider
- **Size**: `px-5/6`, `py-2.5/3`
- **Radius**: Small (`borderRadius.default` or `3px`)

#### Secondary/Ghost Button
- **Background**: Transparent or subtle `surface` background
- **Text**: White
- **Border**: Thin border using `border.primary`
- **Hover**: Light background tint (`bg-white/5`)

#### Rules
- **DO**: Use exactly **one primary CTA** per major view
- **DO**: Keep iconography minimal (simple arrow `→` is enough)
- **AVOID**: Multi-colored button sets, pill buttons, saturated hues outside status colors

### 4.4 Cards & Panels

#### Standard Card
- **Background**: `theme.colors.surface` (`#111111`)
- **Border**: `border.primary` (`#222222`), very subtle shadow (`style.shadows.sm`)
- **Radius**: Small (`sm` to `lg`: 2-4px)
- **Padding**: `2em` or `p-8` to `p-12`
- **Content**: `text.primary` for key data, `text.secondary` for labels

#### Section Tags
- **Style**: Small rectangular tags with translucent background
- **Background**: `bg-white/10` or `bg-black/40`
- **Text**: White, small size (`text-sm`), uppercase or tracking-wide
- **Padding**: `px-3 py-1.5` to `px-4 py-2`

**Critical**: Cards use `surface` background (`#111111`), NOT brighter backgrounds like `bg-white/[0.02]`. Keep cards dark and consistent.

### 4.5 Status Indicators

#### Success (`#40F99B`)
- **Use for**: Healthy systems, successful operations, positive KPIs
- **Style**: Small highlights (text, tiny badges, subtle bars), not huge blocks

#### Error (`#FF3333`)
- **Use for**: Critical errors, destructive confirmation buttons, key alerts
- **Style**: Combine with clear messaging, border, or icon

#### Warning (`#F5A623`)
- **Use for**: Risk, degraded performance, soft-blocking issues
- **Style**: Icons + short text, not large warning panels

### 4.6 Modals & Overlays

#### Modal Backdrop
- **Background**: `overlay.modal` (`rgba(0, 0, 0, 0.85)`)
- **Content**: `surface` panel, small radius, 1px border, subtle shadow
- **Close**: Clear "X" or explicit secondary button

#### Dropdowns
- **Background**: `overlay.dropdown` (`rgba(0, 0, 0, 0.95)`)
- **Animation**: Smooth fade + translate
- **Items**: Hover state with `bg-white/5`

---

## 5. Motion & Interactions

### General Motion Principles
- **Duration**: Short, confident animations (0.1-0.3s)
- **Easing**: `ease` or `ease-out` for entrances
- **Pattern**: Favor **translation + opacity** for entrances

### Interactive Elements

#### Hover States
- **Text Links**: `text-gray-400` → `text-white` transition
- **Buttons**: Slight background dim or color shift
- **Cards**: Subtle background tint (`hover:bg-white/5`)

#### Focus States
- **Critical**: Ensure visible outlines for accessibility
- **Style**: High-contrast focus rings

#### Micro-interactions
- **Arrows**: Tiny translations (`translate-x-1`) to signal direction
- **Icons**: Subtle scale (`scale-105`) on hover

---

## 6. Content & Tone

### Voice
- **Short, tactical phrases**: "Data Driven Solutions", "Old Problems, Innovative Solutions"
- **Emphasize capabilities and outcomes**: "Strategic Digital Tools for enabling innovator's potential"
- **Avoid**: Marketing fluff, excessive adjectives

### Text Length
- **Hero copy**: One or two short lines (max ~40 characters wide)
- **Descriptions**: Keep paragraphs tight and scannable
- **Labels**: Concise, uppercase or tracking-wide when appropriate

---

## 7. Implementation Guidelines

### Use Theme Tokens
- **Always** pull from theme tokens instead of hardcoding colors, font sizes, or radii
- **If you need a new token**: Add it to the theme configuration rather than scattering literals

### Framework Agnostic
- This guide applies to **any framework** (React, Vue, Angular, vanilla JS, etc.)
- Map tokens to your platform's styling system (CSS variables, Tailwind config, styled-components theme, etc.)

### Consistency Checklist
When building a new Labyrica-themed screen, ensure:
1. ✅ Fixed nav at top with dark background
2. ✅ 1440px max width center container
3. ✅ Dark background (`#000000`) with light surfaces (`#111111`)
4. ✅ Same hero, button, and status color patterns
5. ✅ Small border radius (2-4px), not pill-shaped
6. ✅ Short, confident animations (0.1-0.3s)
7. ✅ Glassy overlay effects with backdrop blur
8. ✅ High contrast text (white on black)
9. ✅ Status colors used consistently
10. ✅ No brighter card backgrounds - use `surface` (`#111111`) only

---

## 8. AI Generation Prompt

**Use this prompt when generating Labyrica-themed software:**

```
Create a [type of application] using the Labyrica design system:

COLORS:
- Background: #000000 (pure black)
- Surface/Cards: #111111 (slightly lighter black)
- Text Primary: #ffffff (white)
- Text Secondary: #999999 (mid grey)
- Borders: #222222 (dark grey)
- Success: #40F99B (bright green)
- Error: #FF3333 (red)
- Warning: #F5A623 (amber)

TYPOGRAPHY:
- Font: Inter, system-ui, sans-serif
- Code Font: SF Mono, Menlo, monospace
- Use tight letter-spacing for large headings
- Line-height: 1.5 for body, 1.2 for titles

LAYOUT:
- Max width: 1440px centered
- Padding: 1.5-3rem
- Small border radius: 2-4px (industrial, not rounded)
- Fixed top navigation with dark background

COMPONENTS:
- Cards: #111111 background (NOT brighter), subtle border
- Buttons: White bg, black text for primary
- Glassy overlays: bg-black/40 + backdrop-blur
- Active nav: underline + color change

MOTION:
- Short animations: 0.1-0.3s
- Smooth transitions on hover
- Translation + opacity for entrances

STYLE:
- High contrast, monochrome, tactical
- Sharp edges, minimal decoration
- Dense information allowed but organized
- No pill buttons, no bright card backgrounds
```

---

## 9. Examples

### Example: Hero Section
```html
<div class="min-h-screen bg-black">
  <div class="max-w-[1440px] mx-auto px-6 py-24">
    <div class="backdrop-blur-[4px] bg-black/40 inline-block px-2 py-1">
      <span class="text-sm tracking-wide text-white">SECTION TAG</span>
    </div>
    <h1 class="text-5xl font-light text-white mt-4">MAIN HEADLINE</h1>
    <p class="text-xl text-white/70 mt-6 max-w-[40ch]">Description text here.</p>
    <button class="bg-white text-black px-6 py-3 mt-8 uppercase tracking-wider">
      Primary Action →
    </button>
  </div>
</div>
```

### Example: Card Component
```html
<div class="bg-[#111111] border border-[#222222] rounded-[3px] p-8">
  <div class="text-white text-lg mb-2">Card Title</div>
  <div class="text-[#999999] text-sm">Card description or content.</div>
</div>
```

---

**End of Design Guide**

Use this document as the single source of truth when generating Labyrica-themed software. All design decisions should reference these tokens and patterns.
