#!/bin/bash

# 如果任何命令失敗，腳本將立即退出
set -e

TARGET_DIR="/app/wwwroot/logs/views/"

echo "Sync file start..."

# 確保目標目錄存在
mkdir -p "$TARGET_DIR"

# 強制將來源目錄的所有內容（包含隱藏檔）複製並覆蓋到目標目錄
# -a 參數：遞迴複製並保留檔案屬性
rsync -a "/app/source_wwwroot/logs/views/" "/app/wwwroot/logs/views/"

echo "Sync complete."

# --- 邏輯結束 ---

# 執行傳遞給此腳本的命令（即 Dockerfile 中的 CMD）
# "$@" 會代表 ["dotnet", "XXX.dll"]
echo "Executing command: $@"
exec "$@"