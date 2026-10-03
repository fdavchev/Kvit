try {
  const theme = localStorage.getItem('kvit.theme')
  if (theme === 'light' || theme === 'dark') {
    document.documentElement.dataset.theme = theme
    const color = document.querySelector(
      `meta[name="theme-color"][media="(prefers-color-scheme: ${theme})"]`,
    ).content
    document.querySelectorAll('meta[name="theme-color"]').forEach((meta) => {
      meta.content = color
    })
  }
} catch (error) {
  console.warn('Could not apply the saved "kvit.theme" before the first paint', error)
}
