# BOMBE README

| 學號 | 姓名 | 系統用戶名 |
| :-- | :-- | :-- |
| 61247080S | 張皓棠 | 61247080S |
| 61247069S | 李永豐 | Tim |
| 41047054S | 陳柏瑜 | AA |

此文件會針對編譯 BOMBE 專案方式進行說明

# 編譯

> 如果想要同時編譯 malware 與 EDR，請再命令提示字元 (cmd) 輸入以下指令
```bash
cd /PATH/TO/BOMBE/PROJECT/FOLDER
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

> 如果想要僅編譯 malware 或 EDR 其中一項，請再命令提示字元 (cmd) 輸入以下指令
```bash
cd /PATH/TO/BOMBE/PROJECT/MALWARE/OR/EDR/FOLDER
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

執行指令後，在 `./EDR/bin` 下會產生一個 `/Release` 的目錄，接著在 `./EDR/bin/Release/net6.0/win-x64/publish` 路徑下會出現 `edr.exe` 以及 `edr.pdb` 檔案，產生的 `edr.exe` 即為 EDR 執行檔。
同理，執行指令後，在 `./Malware/bin` 下會產生一個 `/Release` 的目錄，接著在 `./Malware/bin/Release/net6.0/win-x64/publish` 路徑下會出現 `mal.exe` 以及 `mal.pdb` 檔案，產生的 `mal.exe` 即為 MAL 執行檔。

## 指令說明

- `-r win-x64`：指定 x64 為執行系統
- `--self-contained`：打包執行所需要的 `.dll` 檔及相關檔案，生成包含執行時的自包含應用程式，使檔案在不論是否有裝 .Net 的環境中都能正常地執行
- `-p:publishSingleFile=true`：將所需檔案統一打包建置成一個檔案

# Contact

若有任何問題，請隨時透過以下電子郵件聯繫我們
- 61247080s@gapps.ntnu.edu.tw
- 61247069s@gapps.ntnu.edu.tw
- 41047054s@gapps.ntnu.edu.tw