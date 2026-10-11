# Bug 19: Entity name tooltip is not hoverable and uses manual fixed positioning

## Description

The entity name tooltip is a JS-positioned fixed box that the pointer can never enter and that cannot be dismissed with Escape; positioning is manual instead of using the browser's anchor positioning, and layering uses the maximum int32 z-index.

## Actual

- `.entity-name-tooltip`: `position: fixed`, `z-index: 2147483647`, `pointer-events: none` (`src/ServiceBusViewer.Web/src/index.css`, lines 144-159).
- Shown after a 3000ms hover/focus interest delay and hidden immediately on the anchor's `onMouseLeave` (`src/ServiceBusViewer.Web/src/components/layout/EntityTooltipProvider.tsx`, lines 28 and 94).
- Position computed in `useLayoutEffect` from `getBoundingClientRect` with manual edge clamping/flip (lines 67-87); no Escape handling anywhere in the provider.
- The pointer cannot move onto the tooltip (leaving the anchor hides it; the box has no pointer events), so magnified users cannot read the wrapping tooltip text.

## Steps to Reproduce

1. Open the viewer with a long entity name that is truncated with an ellipsis.
2. Hover the name for 3 seconds; the tooltip appears above it.
3. Move the pointer onto the tooltip. It hides; it cannot be hovered.
4. With the tooltip open, press Escape. Nothing happens.
5. Hover a name in a top row. The tooltip flips below the row (manual flip logic).

Frequency: deterministic.

## Expected

The tooltip stays shown while the pointer (or focus) is on the anchor or on the tooltip itself, is dismissible with Escape, is positioned with CSS anchor positioning (explicit anchor name, `position-try` flip fallback) with the guide's conditional polyfill strategy for unsupported browsers, and does not rely on the maximum int32 z-index. The existing 3s interest delay and focus-triggered behavior are preserved.

## Expected Basis

- Documentation: [ui-behaviors/interest-triggered-tooltips](../../../.agents/skills/modern-web-guidance/guides/ui-behaviors/interest-triggered-tooltips.md) - WCAG 1.4.13 (Dismissible/Hoverable/Persistent); MANDATORY explicit `anchor-name`/`position-anchor`; `anchor()` with `position-try` fallback; conditional popover/anchor-positioning polyfills.
- Documentation: [ui-atoms/position-aware-tooltips](../../../.agents/skills/modern-web-guidance/guides/ui-atoms/position-aware-tooltips.md) - anchored positioning, best practices and fallback strategies.
- Developer confirmation: 2026-10-08 CSS/Tailwind review; developer accepted this finding as a defect and assigned this Bug ID.

## Impact

Magnified/low-vision users and pointer-precision users reading long entity names; WCAG 1.4.13 (Hoverable, Dismissible) violation. Severity: Medium.

## Acceptance Criteria

- [x] A shown tooltip can be hovered: moving the pointer from the anchor onto the tooltip does not dismiss it.
- [x] Escape dismisses a shown tooltip.
- [x] Near the top viewport edge the tooltip appears below the anchor; near side edges it stays within the viewport; wrapping long text stays within the current max-width.
- [x] No `z-index: 2147483647`; the tooltip layers per the pattern (top layer / normal stacking) without hacks.
- [x] In browser tiers without native support, the conditional (feature-detected, bundled) polyfill strategy keeps the tooltip visible and correctly positioned.

## Test Cases

- [x] Hover the anchor for 3s: tooltip appears; move the pointer onto the tooltip: it stays; move the pointer off the tooltip: it hides.
- [x] With the tooltip shown, Escape hides it; keyboard focus on the anchor shows it after the delay and blur hides it.
- [x] Anchors in a top row, near the left edge, and near the right edge: the tooltip flips/clamps correctly and is never clipped or off-screen.
- [x] A long entity name wraps within the max-width and remains hoverable while wrapping.

## Contract Impact

Contract readiness: Ready.

### Readiness Blockers

None.

### Declarations

None.

## Dependencies

- None.

## Notes / Additional Context

- Intake: [defect packet](../../planned/defects/%5BDEFECT%5D%20entity-tooltip-not-hoverable-fixed-positioning.md)

## Root Cause

The entity name tooltip was a manually positioned `position: fixed` div with `pointer-events: none`, `z-index: 2147483647`, and `getBoundingClientRect` positioning math in `EntityTooltipProvider.tsx`. Hiding it on the anchor's `onMouseLeave` made it impossible to hover, and no Escape/light-dismiss handling existed — violating WCAG 1.4.13 (Hoverable, Dismissible).

## Fix

- `src/ServiceBusViewer.Web/src/components/layout/EntityTooltipProvider.tsx`: tooltip is now a native Popover API element (`popover`, `showPopover()`/`hidePopover()`) giving top-layer stacking, Escape light-dismiss, and hoverability. The 3s interest delay and focus/blur/touch triggering are preserved; a 150ms hide delay lets the pointer travel onto the tooltip; a `toggle` listener syncs React state on native dismissal; the active anchor receives an inline `anchor-name` instead of JS coordinate math.
- `src/ServiceBusViewer.Web/src/index.css`: `.entity-name-tooltip` uses explicit CSS anchor positioning (`position-anchor: --entity-tooltip`, `bottom: anchor(top)`, `justify-self: anchor-center`, `position-try: flip-block, flip-inline`); removed `position: fixed`, the int32 z-index, and `pointer-events: none`.
- `src/ServiceBusViewer.Web/src/main.tsx`: conditional bundled polyfills, feature-detected: `@oddbird/popover-polyfill` and `@oddbird/css-anchor-positioning` (new dependencies).

## Validation

`npm run build` and `npm run lint` pass; polyfills bundle as lazy chunks. Live validation against the Aspire emulator stack (Chrome): hover 3s shows the tooltip; moving the pointer onto it keeps it open; moving off hides it; Escape dismisses it; focus shows it after the delay and blur hides it; it renders above the anchor with the `position-try` flip fallback and stays within the viewport near edges; `z-index: auto` (top layer, no hacks). No console errors.

## Outcome

Fixed. The tooltip is hoverable, Escape-dismissible, and positioned with CSS anchor positioning plus conditional polyfills for unsupported browsers, per the Expected Basis guides.
