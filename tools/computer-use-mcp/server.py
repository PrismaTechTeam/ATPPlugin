"""
Local computer-use MCP server for the ATP project.

Exposes a minimal, auditable set of desktop mouse/keyboard tools (via pyautogui)
so Claude Code can drive the native WinForms apps (ShadowMain / AutoCount) during demos.

SAFETY:
- pyautogui FAILSAFE is ON: slam the mouse into the TOP-LEFT screen corner to abort instantly.
- Every action returns the cursor position so the caller can verify.
- This server has FULL control of the mouse/keyboard while Claude Code is running it.
  It is a local file (no third-party package beyond pyautogui) — review before trusting.

Run (Claude Code launches this for you via .mcp.json):
    uv run --with mcp --with pyautogui --python 3.11 server.py
"""
from mcp.server.fastmcp import FastMCP
import pyautogui
import time

pyautogui.FAILSAFE = True
pyautogui.PAUSE = 0.15  # small settle between actions

mcp = FastMCP("computer-use")


@mcp.tool()
def screen_size() -> dict:
    """Return the primary screen size in pixels."""
    w, h = pyautogui.size()
    return {"width": w, "height": h}


@mcp.tool()
def cursor_position() -> dict:
    """Return the current mouse cursor position."""
    x, y = pyautogui.position()
    return {"x": x, "y": y}


@mcp.tool()
def move_mouse(x: int, y: int, duration: float = 0.3) -> dict:
    """Move the mouse to absolute screen coordinates (x, y)."""
    pyautogui.moveTo(x, y, duration=duration)
    cx, cy = pyautogui.position()
    return {"moved_to": {"x": cx, "y": cy}}


@mcp.tool()
def click(x: int = -1, y: int = -1, button: str = "left", double: bool = False) -> dict:
    """Click at (x, y). If x or y is -1, click at the current cursor position.
    button: 'left' | 'right' | 'middle'. Set double=true for a double-click."""
    if x >= 0 and y >= 0:
        pyautogui.moveTo(x, y, duration=0.3)
    if double:
        pyautogui.doubleClick(button=button)
    else:
        pyautogui.click(button=button)
    cx, cy = pyautogui.position()
    return {"clicked_at": {"x": cx, "y": cy}, "button": button, "double": double}


@mcp.tool()
def type_text(text: str, interval: float = 0.02) -> dict:
    """Type a string at the current focus."""
    pyautogui.typewrite(text, interval=interval)
    return {"typed": text}


@mcp.tool()
def press_key(key: str) -> dict:
    """Press a single key or a hotkey combo joined by '+' (e.g. 'enter', 'tab', 'ctrl+a')."""
    if "+" in key:
        pyautogui.hotkey(*[k.strip() for k in key.split("+")])
    else:
        pyautogui.press(key)
    return {"pressed": key}


@mcp.tool()
def screenshot(path: str, region_x: int = -1, region_y: int = -1, region_w: int = -1, region_h: int = -1) -> dict:
    """Save a screenshot to an absolute file path. Optionally restrict to a region."""
    if region_x >= 0 and region_y >= 0 and region_w > 0 and region_h > 0:
        img = pyautogui.screenshot(region=(region_x, region_y, region_w, region_h))
    else:
        img = pyautogui.screenshot()
    img.save(path)
    return {"saved": path, "size": {"width": img.width, "height": img.height}}


@mcp.tool()
def wait(seconds: float) -> dict:
    """Sleep for the given number of seconds (max 10)."""
    s = max(0.0, min(10.0, seconds))
    time.sleep(s)
    return {"waited": s}


if __name__ == "__main__":
    mcp.run()
