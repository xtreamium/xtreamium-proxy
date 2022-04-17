import os

import pystray
from PIL import Image
from pystray import MenuItem

from src.args import get_args
from src.proxy import HttpServer


def show_settings():
    print("Hello Sailor")


def on_quit():
    server.stop()
    icon.visible = False
    icon.stop()


if __name__ == '__main__':
    args = get_args()
    server = HttpServer(args.host, args.port)
    server.start()

    image = Image.open(os.path.join(os.path.dirname(os.path.abspath(__file__)), './res/systray-icon.png'))

    menu = (
        MenuItem('Settings', show_settings),
        MenuItem('Quit', on_quit)
    )

    icon = pystray.Icon("XTreamium Proxy", image, "XTreamium Proxy", menu, HAS_MENU=True)
    icon.run()
