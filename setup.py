import sys
from cx_Freeze import setup, Executable

options = {
    "build_exe": {
        "includes": ["xt-proxy.py", "proxy"],
        "path": sys.path + ["src"],
    }
}

base = None
if sys.platform == "win32":
    base = "Win32GUI"

setup(
    name="xtreamium-proxy",
    version="0.1",
    description="XTreamium Proxy",
    options=options,
    executables=[
        Executable(
            script="src/xt-proxy.py",
            base=base,
            shortcutName="XTreamium Proxy",
            shortcutDir="ProgramMenuFolder",
        )
    ],
)
