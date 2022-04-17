import argparse


def get_args():
    parser = argparse.ArgumentParser(description='Xtream Arguments')

    parser.add_argument('--host', type=str, default='localhost', help='Host address to listen on (default localhost)')
    parser.add_argument('--port', type=int, default=9531, help='Port to listen on (default 9531)')

    return parser.parse_args()
