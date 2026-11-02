# 主選單背景與 Logo

- Logo：`Assets/UI/MenuBackground/OccidentLogo.png`，獨立於背景載入與繪製。
- 新背景接入位置：`Assets/UI/MenuBackground/CustomBackground.png`。
- 背景為可選資源；沒有此檔案時，不繪製自訂背景，保留原版主選單景色及 Occident Logo。
- 提供新圖後，以以上檔名放入專案，重新編譯並重新載入模組即可；不需要修改選單繪圖程式。
- 新背景採等比例填滿並置中裁切，不拉伸；Logo 沿用目前位置與縮放規則。
- 舊 `StaticBackground.png` 已移出開發專案並保存備份。
