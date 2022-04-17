import asyncio
import logging
import threading
import urllib.parse as urlparse
from http.server import SimpleHTTPRequestHandler, HTTPServer, ThreadingHTTPServer
from subprocess import Popen

logger = logging.getLogger(__file__)


def missing_bin(bin):
    print("======================")
    print(
        f"ERROR: {bin.upper()} does not appear to be installed correctly! please ensure you can launch '{bin}' in the terminal.")
    print("======================")


class CORSRequestHandler(SimpleHTTPRequestHandler):

    def end_headers(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', '*')
        self.send_header('Access-Control-Allow-Headers', '*')
        self.send_header('Access-Control-Allow-Private-Network', 'true')
        self.send_header('Cache-Control', 'no-store, no-cache, must-revalidate')
        logger.debug(f'end_headers')
        return super(CORSRequestHandler, self).end_headers()

    def do_OPTIONS(self):
        logger.debug(f'do_OPTIONS')
        self.send_response(200)
        self.end_headers()

    def do_GET(self):
        logger.debug(f'do_GET')

        try:
            url = urlparse.urlparse(self.path)
            query = urlparse.parse_qs(url.query)
        except:
            query = {}

        urls = str(query["play_url"][0])
        if urls.startswith('magnet:') or urls.endswith('.torrent'):
            try:
                pipe = Popen([
                                 'peerflix', '-k', urls, '--', '--force-window'
                             ] + query.get("mpv_args", []))
            except FileNotFoundError as e:
                logger.error(f'peerflix not found')
                missing_bin('peerflix')
        else:
            try:
                pipe = Popen(['mpv', urls, '--force-window'] +
                             query.get("mpv_args", []))
            except FileNotFoundError as e:
                logger.error(f'MPV not found')
                missing_bin('mpv')
                self.send_response(501, "mpv application missing...")
                self.end_headers()
                return
        logger.debug(f'playing')
        self.send_response(200, "playing...")
        self.end_headers()


class HttpServer(threading.Thread):

    def __init__(self, host, port):
        super().__init__()
        self._host = host
        self._port = port
        self._httpd = ThreadingHTTPServer((self._host, self._port), CORSRequestHandler)
        self._banner = "xtreamium-proxy"

    def stop(self):
        self._httpd.shutdown()

    def run(self):
        logger.debug(f'Server starting @ {self._host}:{self._port}')
        self._httpd.serve_forever()
        logger.debug(f'Server started @ {self._host}:{self._port}')

    def get_banner(self):
        return f'Xtreamium Proxy @ http://{self._host}:{self._port}'
