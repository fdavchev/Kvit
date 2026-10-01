"""Render the Kvit round 3 mockups and comparison sheets to PNG, and check every colour pair.

Usage: python render.py [--orange #RRGGBB] [--orange-deep #RRGGBB] [--dark-bg #RRGGBB] [--dark-button #RRGGBB]
--orange replaces the bright brand orange (brand-400), --orange-deep the deep one (brand-700),
--dark-bg the dark-mode background (the dark field, raised and border steps are mixed from it),
--dark-button the dark-mode primary button and round initials button (default #ffd9a8).
Exits with code 1 and lists every pair below 4.5:1 (text), 3:1 (large text, controls, field borders,
underlines) or any screen whose content overflows 360x780.
"""

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from string import Template

from playwright.sync_api import Browser, sync_playwright

MOCKUPS = Path(__file__).resolve().parent
SCREENS = MOCKUPS / "screens"
BUILD = MOCKUPS / "build"
OUTPUT = Path(r"C:\Users\Davchev\Projects\Kvit\docs\design\2026-10-01-round-3")
VIEWPORT = {"width": 360, "height": 780}
SHEET_VIEWPORT = {"width": 1800, "height": 900}
HEX_COLOR = re.compile(r"#[0-9a-fA-F]{6}")
PALETTE_ENTRY = re.compile(r"--([\w-]+):\s*(#[0-9a-fA-F]{6});")
COMPUTED_RGB = re.compile(r"rgba?\((\d+), (\d+), (\d+)")
COMPUTED_SRGB = re.compile(r"color\(srgb ([\d.]+) ([\d.]+) ([\d.]+)")
THEME_LIGHT = "orange-200"
THEME_DARK = "dark-bg"
TEXT_MINIMUM = 4.5
LARGE_MINIMUM = 3.0
SCREEN_NAMES = ("welcome", "signup", "login", "home")
SCHEMES = ("light", "dark")

PAGE = Template("""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<meta name="theme-color" media="(prefers-color-scheme: light)" content="$theme_light">
<meta name="theme-color" media="(prefers-color-scheme: dark)" content="$theme_dark">
<title>Kvit</title>
<link rel="stylesheet" href="../palette.css">
$overrides
<link rel="stylesheet" href="../base.css">
<link rel="stylesheet" href="../app.css">
</head>
<body>
$body
</body>
</html>
""")

SHEET = Template("""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Kvit styles</title>
<link rel="stylesheet" href="../palette.css">
<link rel="stylesheet" href="../sheet.css">
</head>
<body>
<div class="sheet">
$shots
</div>
</body>
</html>
""")

AUDIT_SCRIPT = """() => {
  const transparent = 'rgba(0, 0, 0, 0)';
  const backgroundOf = (start) => {
    for (let node = start; node; node = node.parentElement) {
      const color = getComputedStyle(node).backgroundColor;
      if (color !== transparent) return color;
    }
    return getComputedStyle(document.documentElement).backgroundColor;
  };
  const describe = (element) => element.tagName === 'INPUT' ? '#' + element.id : element.textContent.trim().slice(0, 34);
  const isLarge = (style) => {
    const size = parseFloat(style.fontSize);
    return size >= 24 || (size >= 18.66 && parseInt(style.fontWeight, 10) >= 700);
  };
  const textSelector = '.btn, .lang-option, .wordmark, .tagline, .tagline-translation, .pitch, .title, .greeting, .label, .hint, .note, .placeholder, .avatar';
  const textElements = [...document.querySelectorAll(textSelector)];
  const texts = textElements.map((element) => {
    const style = getComputedStyle(element);
    return { kind: 'text', what: describe(element), foreground: style.color, background: backgroundOf(element), large: isLarge(style) };
  });
  const underlines = textElements
    .filter((element) => getComputedStyle(element).textDecorationLine.includes('underline'))
    .map((element) => ({
      kind: 'underline', what: describe(element), foreground: getComputedStyle(element).textDecorationColor, background: backgroundOf(element), large: true,
    }));
  const controls = [...document.querySelectorAll('.btn-primary, .avatar')].map((element) => ({
    kind: 'control', what: describe(element), foreground: getComputedStyle(element).backgroundColor, background: backgroundOf(element.parentElement), large: true,
  }));
  const fields = [...document.querySelectorAll('.input')].map((element) => ({
    kind: 'field', what: describe(element), foreground: getComputedStyle(element).borderTopColor, background: backgroundOf(element.parentElement), large: true,
  }));
  return {
    overflow: document.documentElement.scrollWidth > innerWidth || document.documentElement.scrollHeight > innerHeight,
    pairs: [...texts, ...underlines, ...controls, ...fields],
  };
}"""


@dataclass(frozen=True)
class Page:
    """One built HTML page; it is screenshotted as <name>-light.png and <name>-dark.png."""

    name: str
    body: str


def hex_color(value: str) -> str:
    """Validate a #RRGGBB argument and return it lower-cased."""
    if not HEX_COLOR.fullmatch(value):
        raise argparse.ArgumentTypeError(f"expected a colour like #F97316, got {value!r}")
    return value.lower()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Render the Kvit round 3 mockups to PNG.")
    parser.add_argument("--orange", type=hex_color, help="replaces the bright brand orange")
    parser.add_argument("--orange-deep", type=hex_color, help="replaces the deep brand orange")
    parser.add_argument("--dark-bg", type=hex_color, help="replaces the dark-mode background")
    parser.add_argument("--dark-button", type=hex_color, help="replaces the dark-mode primary button colour")
    return parser.parse_args()


def fragment(name: str, **values: str) -> str:
    return Template((SCREENS / f"{name}.html").read_text(encoding="utf-8")).substitute(values).rstrip("\n")


def build_pages() -> list[Page]:
    signup = fragment(
        "form",
        screen_class="screen-signup",
        title="Create account",
        submit="Create account",
        fields="\n".join([
            fragment("field-name"),
            fragment("field-email"),
            fragment(
                "field-password",
                autocomplete="new-password",
                enterkeyhint="done",
                described_by=' aria-describedby="password-hint"',
                hint=fragment("password-hint"),
            ),
        ]),
        extras=fragment("form-link", href="/login", label="I already have an account"),
    )
    login = fragment(
        "form",
        screen_class="screen-login",
        title="Log in",
        submit="Log in",
        fields="\n".join([
            fragment("field-email"),
            fragment("field-password", autocomplete="current-password", enterkeyhint="go", described_by="", hint=""),
        ]),
        extras="\n".join([fragment("login-note"), fragment("form-link", href="/signup", label="I don’t have an account")]),
    )
    return [
        Page("welcome", fragment("welcome")),
        Page("signup", signup),
        Page("login", login),
        Page("home", fragment("home")),
    ]


def read_palette() -> dict[str, str]:
    palette = dict(PALETTE_ENTRY.findall((MOCKUPS / "palette.css").read_text(encoding="utf-8")))
    missing = [name for name in ("orange", "orange-deep", THEME_LIGHT, THEME_DARK) if name not in palette]
    if missing:
        raise ValueError(f"palette.css must define these as #RRGGBB: {', '.join(missing)}")
    return palette


def override_style(overrides: dict[str, str]) -> str:
    if not overrides:
        return ""
    declarations = " ".join(f"--{name}: {value};" for name, value in overrides.items())
    return f"<style>:root {{ {declarations} }}</style>"


def parse_computed_color(css_color: str) -> tuple[float, float, float]:
    """Return 0-255 channels from a computed rgb()/rgba() or color(srgb ...) value."""
    if match := COMPUTED_RGB.match(css_color):
        return tuple(float(part) for part in match.groups())
    if match := COMPUTED_SRGB.match(css_color):
        return tuple(float(part) * 255 for part in match.groups())
    raise ValueError(f"Unexpected computed colour: {css_color}")


def relative_luminance(css_color: str) -> float:
    def channel(value: float) -> float:
        srgb = value / 255
        return srgb / 12.92 if srgb <= 0.04045 else ((srgb + 0.055) / 1.055) ** 2.4

    red, green, blue = (channel(value) for value in parse_computed_color(css_color))
    return 0.2126 * red + 0.7152 * green + 0.0722 * blue


def contrast_ratio(foreground: str, background: str) -> float:
    lighter, darker = sorted((relative_luminance(foreground), relative_luminance(background)), reverse=True)
    return (lighter + 0.05) / (darker + 0.05)


def audit_page(shot: str, audit: dict) -> list[str]:
    """Print every colour pair of one screenshot and return a description of each failure."""
    failures = [f"{shot}: content overflows 360x780"] if audit["overflow"] else []
    for pair in audit["pairs"]:
        ratio = contrast_ratio(pair["foreground"], pair["background"])
        minimum = LARGE_MINIMUM if pair["large"] else TEXT_MINIMUM
        verdict = "ok" if ratio >= minimum else "FAIL"
        line = f"{shot:14} {pair['kind']:9} {ratio:5.2f} {verdict:4} {pair['what']!r} {pair['foreground']} on {pair['background']}"
        print(line)
        if ratio < minimum:
            failures.append(line)
    return failures


def render_page(browser: Browser, html_path: Path, scheme: str, shot: str) -> list[str]:
    context = browser.new_context(viewport=VIEWPORT, device_scale_factor=2, color_scheme=scheme)
    page = context.new_page()
    page.goto(html_path.as_uri())
    failures = audit_page(shot, page.evaluate(AUDIT_SCRIPT))
    page.screenshot(path=str(OUTPUT / f"{shot}.png"))
    context.close()
    return failures


def render_sheet(browser: Browser, scheme: str) -> None:
    images = [
        f'<img class="sheet-shot" src="{(OUTPUT / f"{name}-{scheme}.png").as_uri()}" alt="{name} {scheme}">'
        for name in SCREEN_NAMES
    ]
    html_path = BUILD / f"compare-{scheme}.html"
    html_path.write_text(SHEET.substitute(shots="\n".join(images)), encoding="utf-8")
    context = browser.new_context(viewport=SHEET_VIEWPORT, device_scale_factor=1, color_scheme=scheme)
    page = context.new_page()
    page.goto(html_path.as_uri(), wait_until="load")
    page.locator("body").screenshot(path=str(OUTPUT / f"compare-{scheme}.png"))
    context.close()


def main() -> None:
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")
    args = parse_args()
    requested = {"orange": args.orange, "orange-deep": args.orange_deep, "dark-bg": args.dark_bg, "dark-button": args.dark_button}
    overrides = {name: value for name, value in requested.items() if value}
    palette = read_palette() | overrides
    BUILD.mkdir(exist_ok=True)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    failures = []
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch()
        for page in build_pages():
            html_path = BUILD / f"{page.name}.html"
            html_path.write_text(
                PAGE.substitute(
                    theme_light=palette[THEME_LIGHT],
                    theme_dark=palette[THEME_DARK],
                    overrides=override_style(overrides),
                    body=page.body,
                ),
                encoding="utf-8",
            )
            for scheme in SCHEMES:
                failures += render_page(browser, html_path, scheme, f"{page.name}-{scheme}")
        for scheme in SCHEMES:
            render_sheet(browser, scheme)
        browser.close()
    if failures:
        raise SystemExit("Contrast or layout failures:\n" + "\n".join(failures))


if __name__ == "__main__":
    main()
