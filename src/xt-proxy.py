import os
from tempfile import gettempdir
import PySimpleGUI as sg
import screeninfo

import pystray
from PIL import Image
from appdirs import user_config_dir
from pystray import MenuItem

from src.args import get_args
from src.proxy import HttpServer

import logging

SETTINGS_PATH = user_config_dir('xtreamium')
settings = sg.UserSettings(
    path=SETTINGS_PATH,
    convert_bools_and_none=True)

log_file = os.path.join(gettempdir(), 'xtreamium-proxy.log')

logging.basicConfig(filename=log_file, encoding='utf-8', level=logging.DEBUG)
logging.debug('XT Proxy Starting')

stderrLogger = logging.StreamHandler()
stderrLogger.setFormatter(logging.Formatter(logging.BASIC_FORMAT))
logging.getLogger().addHandler(stderrLogger)


def show_settings():
    location = (0, 0)
    for m in screeninfo.get_monitors():
        if m.is_primary:
            location = (int(m.width / 2), int(m.height / 2))

    sg.theme("DarkAmber")
    layout = [
        [sg.Text('Listen host.')],
        [sg.InputText(key='host', default_text=settings['host'])],
        [sg.Text('Listen port.')],
        [sg.InputText(key='port', default_text=settings['port'])],

        [sg.Text('MPV Arguments')],
        [sg.Multiline(size=(45, 5), key='mpv-args', default_text=settings['mpv-args'])],
        [sg.Button('Ok'), sg.Button('Cancel')]
    ]

    window = sg.Window(
        'Xtreamium Proxy Settings',
        layout,
        location=location,
        resizable=True,
        finalize=True,
        element_padding=(15, 15),
        scaling=2.5)

    window.set_min_size((400, 400))

    # Event Loop to process "events" and get the "values" of the inputs
    while True:
        event, values = window.read()
        if event == sg.WIN_CLOSED or event == 'Cancel':  # if user closes window or clicks cancel
            break
        if event in 'Ok':
            settings.set('host', values['host'])
            settings.set('port', int(values['port']))
            settings.set('mpv-args', values['mpv-args'])
            settings.save()

    window.close()


def on_quit():
    server.stop()
    icon.visible = False
    icon.stop()


if __name__ == '__main__':

    if not settings.exists():
        settings.set('mpv-args',
                     "--keep-open=yes\n--geometry=1024x768-0-0\n--ontop\n--screen=2\n--ytdl-format=bestvideo[ext=mp4]"
                     "[height<=?720]+bestaudio[ext=m4a]\n--border=no")

        settings.set('host', 'localhost')
        settings.set('port', 9531)
        settings.save()

    server = HttpServer(
        settings.get('host'),
        settings.get('port'),
        settings.get('mpv-args')
    )

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
