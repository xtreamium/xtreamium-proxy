import sys
from cx_Freeze import setup, Executable

build_exe_options = {
    "packages": ["os", "src"],
    "include_files": ["src/", ('src/xt-proxy.py', 'xt-proxy')],
    "excludes": ["tkinter"]
}

base = None
if sys.platform == "win32":
    base = "Win32GUI"

setup(
    name="xtreamium-proxy",
    version="0.1",
    description="XTreamium Proxy",
    options={"build_exe": build_exe_options},
    executables=[Executable(script="src/xt-proxy.py", base=base)],
)
