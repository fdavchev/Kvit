import { fireEvent, render, within } from '@testing-library/react'
import { toast, type Action, type ExternalToast } from 'sonner'
import { vi } from 'vitest'
import { showDangerToast, showUndoToast } from '@/shared/toasts/showUndoToast'

export interface ShownToast {
  message: string
  options: ExternalToast
}

export interface ToastStyling {
  className: string | undefined
  classNames: unknown
  unstyled: boolean | undefined
  style: unknown
  actionButtonStyle: unknown
}

export function toastsShownWithOptions(): ShownToast[] {
  return vi.mocked(toast).mock.calls.map(([message, options]) => {
    if (typeof message !== 'string') {
      throw new Error(`Expected the toast message to be a text, got ${JSON.stringify(message)}`)
    }
    return { message, options: options ?? {} }
  })
}

export function shownToastTexts(): unknown[] {
  return [...vi.mocked(toast).mock.calls, ...vi.mocked(toast.success).mock.calls].map(
    ([text]) => text,
  )
}

export function toastShownWithText(message: string): ShownToast {
  const found = toastsShownWithOptions().find((shown) => shown.message === message)
  if (found === undefined) {
    throw new Error(
      `No toast was shown with the text "${message}". Shown: ${JSON.stringify(toastsShownWithOptions().map((shown) => shown.message))}`,
    )
  }
  return found
}

export function actionOf(shown: ShownToast): Action {
  const { action } = shown.options
  if (
    typeof action !== 'object' ||
    action === null ||
    !('label' in action) ||
    !('onClick' in action) ||
    typeof action.onClick !== 'function'
  ) {
    throw new Error(`The toast "${shown.message}" has no action with a label and onClick`)
  }
  return { label: action.label, onClick: action.onClick }
}

export function pressToastAction(shown: ShownToast): void {
  const { onClick } = actionOf(shown)
  const detachedPage = document.createElement('div')
  render(
    <button type="button" data-testid="toast-action" onClick={onClick}>
      toast action
    </button>,
    { container: detachedPage },
  )
  fireEvent.click(within(detachedPage).getByTestId('toast-action'))
}

export function stylingOf(shown: ShownToast): ToastStyling {
  const { className, classNames, unstyled, style, actionButtonStyle } = shown.options
  return { className, classNames, unstyled, style, actionButtonStyle }
}

export function referenceToastStyling(danger: boolean): ToastStyling {
  vi.mocked(toast).mockClear()
  showUndoToast('reference', { danger, undoLabel: 'reference', onUndo: () => {} })
  const [reference] = toastsShownWithOptions()
  vi.mocked(toast).mockClear()
  return stylingOf(reference)
}

export function referenceDangerToastStyling(): ToastStyling {
  vi.mocked(toast).mockClear()
  showDangerToast('reference')
  const [reference] = toastsShownWithOptions()
  vi.mocked(toast).mockClear()
  return stylingOf(reference)
}
