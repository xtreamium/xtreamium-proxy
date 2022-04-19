import sys
from cx_Freeze import setup, Executable

options = {
    "build_exe": {
        "includes": ["xt-proxy", "proxy"],
        "path": sys.path + ["src"],
    },
    "bdist_msi": {
        'upgrade_code': '{886dbe34-e78c-436e-9d75-64126d434ae4}',
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
