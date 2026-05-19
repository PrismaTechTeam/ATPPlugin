"""
One-file static server for the Stock Report clone.
Usage:  python serve.py        (defaults to port 8000)
        python serve.py 9000   (custom port)
"""
import sys
import http.server
import socketserver
import webbrowser
import os

PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8000
DIR  = os.path.dirname(os.path.abspath(__file__))

os.chdir(DIR)

class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Cache-Control', 'no-store')
        super().end_headers()

class ThreadedServer(socketserver.ThreadingMixIn, http.server.HTTPServer):
    daemon_threads = True
    allow_reuse_address = True

with ThreadedServer(("", PORT), Handler) as httpd:
    url = f"http://localhost:{PORT}/"
    print(f"Stock Report Clone serving at {url}")
    print("Press Ctrl+C to stop.")
    try:
        webbrowser.open(url)
    except Exception:
        pass
    httpd.serve_forever()
