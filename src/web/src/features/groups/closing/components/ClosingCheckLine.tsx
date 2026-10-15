interface ClosingCheckLineProps {
  mark: string
  text: string
}

export function ClosingCheckLine({ mark, text }: ClosingCheckLineProps) {
  return (
    <li className="flex items-center gap-2 text-[0.9375rem]">
      <span aria-hidden="true" className="w-4 flex-none text-center font-extrabold text-positive">
        {mark}
      </span>
      <span>{text}</span>
    </li>
  )
}
