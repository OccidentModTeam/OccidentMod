# 素材備忘錄（Codex 請先閱讀）

Codex：處理本目錄素材前，請先閱讀本文件，再核對目前的實際檔案、程式引用與 tModLoader 資源載入方式。本文件記錄的是整理規則和已知狀態，可能因後續開發而過時；若文件與 repository 現況不一致，請以實際檔案和程式碼為準，並在完成工作時更新本文件。

## 工作原則

- 依素材用途分類；移動已使用的素材時，必須同步更新所有程式資源路徑，包括動態拼接的路徑。
- 不要修改圖片或音效的內容。若任務明確要求轉檔或編修，先確認需求，再保留原始素材。
- 不要根據檔名或外觀自行推測用途。用途不明的新素材先放入 `Unassigned/`，確認後再分類和命名。
- 整理素材時避免刪除重複檔或原始副本；先核對引用及雜湊，並確認有安全保留來源。
- 修改資源路徑後，確認路徑大小寫與實際檔名一致，並執行可用的建置或載入驗證。

## 素材目錄與目前接入狀態

| 目錄 | 用途 | 已知狀態 |
| --- | --- | --- |
| `UI/MenuBackground/` | 主選單背景、Occident Logo | 已使用 |
| `UI/CharacterCreation/` | 角色建立與職業介面圖示 | `ClassInfoToggle` 已使用 |
| `UI/WorldCreation/Size/` | 世界尺寸選項圖示 | `GiantWorld` 已使用 |
| `UI/WorldCreation/Difficulty/Icons/` | 難度選項圖示 | 已使用 |
| `UI/WorldCreation/Difficulty/Previews/` | 難度預覽圖片 | 已使用 |
| `UI/HUD/Stealth/` | 潛行條與潛行計量圖 | 尚未找到程式引用 |
| `UI/HUD/ResourceBars/` | SP、MP、Energy 資源條圖片 | 尚未找到程式引用 |
| `UI/Equipment/` | 裝備介面素材，例如盾牌欄位 | 尚未找到程式引用 |
| `Sounds/UI/` | 介面提示音 | `warning.wav` 尚未找到程式引用 |
| `Backgrounds/SporeDreamForest/` | 孢夢之森地表背景 | 使用 `精灵-0040.png` 的原樣副本；天空保留透明，背景以整數倍率及水平視差繪製 |
| `Sprites/Environment/SporeDreamForest/` | 孢夢之森草皮、巨菇與植被 | 圖集規格見下方「孢夢之森素材規格」 |
| `Sprites/Weapon/` | 武器貼圖，依 Magic、Melee、Ranged、Summon 分類 | 古蕈柱杖已接入；其他待接入。桌面來源保留副本 |
| `Sprites/Item/Furniture/` | 家具物品與放置貼圖 | 瓶中蘑菇已接入；32×32 原圖以無間距 2×2 圖塊使用 |
| `Sprites/Summoned Entity/` | 召喚實體貼圖 | `baby_mushroom`：8 格，每格 32×36，僅供召喚物 |
| `Sprites/Town Pets/` | 城鎮寵物貼圖 | `city_baby_mushroom`：57 格，每格 80×100，僅供城鎮菇寶寶；`_Head` 是第一格裁出的頭像 |
| `Unassigned/` | 尚未確認用途的新素材暫存區 | 目前為空 |
| `Sprites/Prototypes/FungalWyrm/` | 耶夢加得的灰階像素造型預覽 | 10 張透明底 PNG；由 FungalWyrmPreview 明確引用，僅供無傷害分節預覽 |

「已使用」與「尚未找到程式引用」是目前已知狀態，不是永久保證。若要判斷素材是否可移動、移除或接入，請搜尋實際引用並檢查動態路徑。

## 孢夢之森素材規格

以下是已記錄的圖集切割規格。使用或修改圖集前，請再核對實際圖片尺寸與切割程式；不要拉伸素材來湊尺寸。

### 0.1.1 巨菇素材

- 直段：`amanita_crema_tiles_02`，每格 16×20。
- 橫段：`amanita_crema_tiles`，每格 20×16。
- 彎頭：`amanita_crema_tiles_corner`，四款完整彎頭，每格 20×20。
- 封口：`amanita_crema_tiles_top`，每格 16×24。
- 各格間距：2px。
- 菇傘：每款 130×74，取上方 72px；接頭中心 x=66。

### 0.1.2 巨菇圖集

- 直柱：108×86；每格 20×20，步距 22。
- 橫柱：108×88；每格 20×20，步距 22；列起點 y=0、22、46、68。
- 轉角：86×64；四方向、三排紋理；每格 20×20，步距 22。
- 封頂：88×24；每格 16×24，步距 18。
- 菇傘：386×72；三款各取 126×72，步距 130；接頭中心 x=62。
- 新增邊緣需以原像素溢出繪製，不可拉伸；菇傘僅朝上或左右旋轉 90 度。

## 資源路徑規則

- `ModContent` 資源路徑使用 `/`，省略副檔名。例如：`OccidentMod/Assets/UI/CharacterCreation/ClassInfoToggle`。
- 資源路徑區分大小寫，必須與實際檔案名稱一致。
- 若路徑由字串或變數動態拼接，檢查最終組合結果，不要只檢查固定字串。
- 模組根目錄的 `icon.png`、`icon_small.png`、`icon_workshop.png` 保留原位置，供模組與工坊圖示使用。
- 新增物品、敵怪等 `Content` 類別時，先確認 tModLoader 預設貼圖路徑；若貼圖放在 `Assets/`，需在類別中明確指定 `Texture`。

## 已記錄的素材匯入資訊

- 第一張 `codex-clipboard-8a257771-01c5-4ebf-8798-6c63615b7263.png` 與 `UI/HUD/Stealth/StealthBar.png` 的 SHA-256 相同，整理時合併保留後者。
- 第二張 `codex-clipboard-2eedfb5d-c6d0-4497-8aa4-7e22a2fbf27d.png` 與 `UI/HUD/Stealth/StealthMeter.png` 的 SHA-256 相同，整理時合併保留後者。
- 原始副本另存於此次整理工作的備份中。若再次整理或清理素材，請先確認該備份仍存在且內容可用；不要假設備份一直保留在 repository 內。

## 耶夢加得造型預覽（2026-11-03）

- `Sprites/Prototypes/FungalWyrm/` 包含 Head、Forebody、BodyLarge、BodyMedium、BodySmall、TailRoot、TailFin、FinNear、FinFar、BodyVariant。
- 使用 `Content/NPCs/Prototypes/FungalWyrmPreview.cs` 繪製，`/fungalwyrm 14-48` 調整長度，`/fungalwyrm clear` 移除。
- 這是單人無傷害的外觀原型，沒有各體節獨立受擊、肉繭、吞食或戰鬥階段。
- 輸入來源為本次生成的透明圖集；拆件經裁切、最近鄰取樣和灰階整理，並非已完成手工逐像素修訂的正式素材。
- 原始來源與靜態預覽保留於本次 Codex 工作輸出中；尚未搬入桌面 ATLAS，不應描述成已完成創作素材歸檔。
- 靜態排布與幾何檢查不能取代動態目視。尾根與曲線已有修訂，最後版本仍待使用者遊戲驗收。
- 僅保留一份正式 OccidentMod 包供使用者測試；素材與來源備份不能放入遊戲 Mods 目錄。
