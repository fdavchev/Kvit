import { useLayoutEffect, useRef } from 'react'
import { Link, useLocation } from 'react-router'
import { currentNavItemIndex, isCurrentNavPath, type KvitNavItem } from './kvitNavItem'

export interface KvitRailSubItem {
  to: string
  label: string
  emoji: string
}

export interface KvitRailItem extends KvitNavItem {
  subItems?: readonly KvitRailSubItem[]
}

interface KvitRailProps {
  brand: string
  items: readonly KvitRailItem[]
}

const slidingAttribute = 'data-sliding'

export function KvitRail({ brand, items }: KvitRailProps) {
  const { pathname } = useLocation()
  const activeIndex: number = currentNavItemIndex(items, pathname)
  const trackRef = useRef<HTMLDivElement>(null)
  const pillRef = useRef<HTMLSpanElement>(null)
  const linkRefs = useRef<(HTMLAnchorElement | null)[]>([])

  useLayoutEffect(() => {
    const track = trackRef.current
    const pill = pillRef.current
    const activeLink = linkRefs.current[activeIndex]
    if (track === null || pill === null || activeLink === undefined || activeLink === null) {
      return
    }

    const placePill = (): void => {
      pill.style.translate = `0 ${activeLink.offsetTop}px`
      pill.style.height = `${activeLink.offsetHeight}px`
    }

    placePill()
    if (!pill.hasAttribute(slidingAttribute)) {
      pill.getBoundingClientRect()
      pill.setAttribute(slidingAttribute, '')
    }

    const observer = new ResizeObserver(placePill)
    observer.observe(track)
    return () => observer.disconnect()
  }, [activeIndex])

  return (
    <nav className="sticky top-0 flex h-dvh flex-col overflow-y-auto bg-muted px-[clamp(0.6rem,1.2vw,1rem)] py-[clamp(0.9rem,1.6vw,1.4rem)]">
      <p className="px-3 pt-1 pb-5 text-2xl font-black text-wordmark">{brand}</p>
      <div ref={trackRef} className="relative">
        {activeIndex !== -1 && (
          <span
            ref={pillRef}
            aria-hidden="true"
            data-rail-pill
            className="pointer-events-none absolute inset-x-0 top-0 rounded-full bg-tab-active shadow-[0_1px_4px_var(--track-shadow)] motion-safe:data-sliding:transition-[translate,height] motion-safe:data-sliding:duration-200 motion-safe:data-sliding:ease-out"
          />
        )}
        <ul className="flex flex-col gap-1">
          {items.map((item, index) => (
            <li key={item.to} className="flex flex-col">
              <Link
                ref={(link) => {
                  linkRefs.current[index] = link
                }}
                to={item.to}
                aria-current={index === activeIndex ? 'page' : undefined}
                className="pressable relative flex min-h-12 items-center gap-3 rounded-full px-3 font-bold text-muted-foreground hover:text-foreground aria-[current=page]:text-tab-active-foreground"
              >
                {item.icon}
                {item.label}
              </Link>
              {item.subItems !== undefined && item.subItems.length > 0 && (
                <ul className="ms-8 mt-1 mb-2 flex flex-col gap-0.5">
                  {item.subItems.map((subItem) => (
                    <li key={subItem.to}>
                      <Link
                        to={subItem.to}
                        aria-current={isCurrentNavPath(subItem.to, pathname) ? 'true' : undefined}
                        className="pressable flex min-h-10 min-w-0 items-center gap-2 rounded-xl px-2.5 text-[0.9375rem] font-semibold text-foreground hover:bg-track-active aria-[current=true]:bg-track-active aria-[current=true]:font-extrabold"
                      >
                        <span aria-hidden="true">{subItem.emoji}</span>
                        <span className="truncate">{subItem.label}</span>
                      </Link>
                    </li>
                  ))}
                </ul>
              )}
            </li>
          ))}
        </ul>
      </div>
    </nav>
  )
}
