import type { AnchorHTMLAttributes, MouseEvent } from 'react'
import { useLocation } from 'wouter'
import { runViewTransition } from '../../lib/viewTransitions'

type Props = Omit<AnchorHTMLAttributes<HTMLAnchorElement>, 'href'> & { href: string }

export function ViewTransitionLink({ href, onClick, ...props }: Props) {
  const [, navigate] = useLocation()

  function handleClick(event: MouseEvent<HTMLAnchorElement>) {
    onClick?.(event)
    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return
    event.preventDefault()
    runViewTransition(() => navigate(href))
  }

  return <a href={href} onClick={handleClick} {...props} />
}
