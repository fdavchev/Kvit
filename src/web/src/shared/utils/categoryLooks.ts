export function categoryNameKey(category: { key: string }): string {
  return `categories.${category.key}`
}

export function categoryColor(category: { color: string }): string {
  return `var(--category-${category.color})`
}
