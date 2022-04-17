import os
from tempfile import gettempdir

import pystray
from PIL import Image
from pystray import MenuItem

from src.args import get_args
from src.proxy import HttpServer

import logging

log_file = os.path.join(gettempdir(), 'xtreamium-proxy.log')

logging.basicConfig(filename=log_file, encoding='utf-8', level=logging.DEBUG)
logging.debug('XT Proxy Starting')

stderrLogger = logging.StreamHandler()
stderrLogger.setFormatter(logging.Formatter(logging.BASIC_FORMAT))
logging.getLogger().addHandler(stderrLogger)


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

    icon = pystray.Icon(
        name="XTreamium Proxy",
        icon=image,
        title=server.get_banner(),
        menu=menu,
        HAS_MENU=True)

    icon.run()
