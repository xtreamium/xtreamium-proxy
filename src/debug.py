import threading
import time
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler


class MyServer(threading.Thread):
    def run(self):
        self.server = ThreadingHTTPServer(('localhost', 8000), SimpleHTTPRequestHandler)
        self.server.serve_forever()

    def stop(self):
        self.server.shutdown()


if __name__ == '__main__':
    s = MyServer()
    s.start()
    print('thread alive:', s.is_alive())  # True
    time.sleep(2)
    s.stop()
    print('thread alive:', s.is_alive())  # False
