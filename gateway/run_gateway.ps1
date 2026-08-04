$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$env:PIP_CACHE_DIR = Join-Path $ScriptDir ".pip-cache"
$env:TEMP = Join-Path $ScriptDir ".tmp"
$env:TMP = Join-Path $ScriptDir ".tmp"

New-Item -ItemType Directory -Force -Path $env:PIP_CACHE_DIR, $env:TEMP | Out-Null

& (Join-Path $ScriptDir ".venv\Scripts\python.exe") -m uvicorn --app-dir $ScriptDir npc_agent_gateway:app --host 127.0.0.1 --port 8787 --reload
