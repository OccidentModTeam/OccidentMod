# 素材分類

以用途分類；移動已使用的素材時，同步更新程式中的資源路徑。圖片與音效內容維持原樣。

| 目錄 | 用途 | 目前接入狀態 |
| --- | --- | --- |
| `UI/MenuBackground/` | 主選單背景、Occident Logo | 已使用 |
| `UI/CharacterCreation/` | 選角與職業建立介面圖示 | ClassInfoToggle 已使用 |
| `UI/WorldCreation/Size/` | 世界尺寸選項圖示 | GiantWorld 已使用 |
| `UI/WorldCreation/Difficulty/Icons/` | 難度選項圖示 | 已使用 |
| `UI/WorldCreation/Difficulty/Previews/` | 難度預覽圖片 | 已使用 |
| `UI/HUD/Stealth/` | 潛行條與潛行計量圖 | 尚未找到程式引用 |
| `UI/HUD/ResourceBars/` | SP、MP、Energy 資源條圖片 | 尚未找到程式引用 |
| `UI/Equipment/` | 裝備介面，例如盾牌欄位 | 尚未找到程式引用 |
| `Sounds/UI/` | 介面提示音 | warning.wav 尚未找到程式引用 |
| `Sprites/Weapon/` | 武器貼圖，依 Magic、Melee、Ranged、Summon 分類 | 古蕈柱杖已接入，其他待接入；桌面來源保留副本 |
| `Sprites/Item/Furniture/` | 家具物品與放置貼圖 | 瓶中蘑菇已接入，32×32 原圖以無間距 2×2 圖塊使用 |
| `Sprites/Summoned Entity/` | 召喚實體貼圖 | baby_mushroom：8 格，每格32×36，僅供召喚物 |
| `Sprites/Town Pets/` | 城鎮寵物貼圖 | city_baby_mushroom：57 格，每格80×100，僅供城鎮菇寶寶；_Head 是第一格裁出的頭像 |
| `Unassigned/` | 尚未確認用途的新素材暫存區 | 目前為空 |

## 本次匯入素材對應

- 第一張 `codex-clipboard-8a257771-01c5-4ebf-8798-6c63615b7263.png` 與 `UI/HUD/Stealth/StealthBar.png` 的 SHA-256 完全相同，合併保留後者。
- 第二張 `codex-clipboard-2eedfb5d-c6d0-4497-8aa4-7e22a2fbf27d.png` 與 `UI/HUD/Stealth/StealthMeter.png` 的 SHA-256 完全相同，合併保留後者。

原始副本已另存於此次整理工作的備份中。用途不明的新素材可先放入 `Unassigned/`，確認後再命名分類，避免根據外形誤判。

## 資源引用規則

- ModContent 資源路徑使用 `/`，並省略副檔名，例如 `OccidentMod/Assets/UI/CharacterCreation/ClassInfoToggle`。
- 區分大小寫，名稱須與實際檔案一致；拼接形成的動態路徑也要一併檢查。
- 模組根目錄的 `icon.png`、`icon_small.png`、`icon_workshop.png` 保留原位置，供模組與工坊圖示使用。
- 新增物品、敵怪等 Content 類別時，須考量 tModLoader 的預設貼圖路徑；若貼圖另放 Assets，需在類別中明確指定 Texture。
