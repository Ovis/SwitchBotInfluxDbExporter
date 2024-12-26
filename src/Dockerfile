FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

WORKDIR /app

# アプリケーションの公開ファイルをコピー
COPY ./ /app/

# コンテナ起動時に実行されるコマンドを設定
WORKDIR /app/bin
ENTRYPOINT ["dotnet", "/app/bin/SwitchBotInfluxDbExporter.dll"]