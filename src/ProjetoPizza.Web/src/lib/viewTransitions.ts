export function runViewTransition(update: () => void) {
  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches
  if (prefersReducedMotion || !document.startViewTransition) {
    update()
    return
  }
  document.startViewTransition(update)
}
