import sys
from cx_Freeze import setup, Executable

options = {
    "build_exe": {
        "includes": ["xt-proxy", "proxy"],
        "path": sys.path + ["src"],
    },
    "bdist_msi": {
        "includes": ["xt-proxy", "proxy"],
        "path": sys.path + ["src"],
        'upgrade_code': '{66620F3A-DC3A-11E2-B341-002219E9B01E}',
        'add_to_path': False,
        'initial_target_dir': '[ProgramFilesFolder]\\XTreamiumProxy\\',
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
            shortcut_name="XTreamium Proxy",
            shortcut_dir="ProgramMenuFolder",
        )
    ],
)
