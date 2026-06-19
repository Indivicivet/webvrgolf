@echo off
echo Starting Python web server on http://localhost:8000

start http://localhost:8000

python -m http.server 8000
