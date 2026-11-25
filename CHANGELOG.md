# 《輓歌》Vesper — Beta v0.2.1
**玩家動作、職業介面與新內容 / Player Movement, Class Interface, and New Content**

## 繁體中文

### 玩家動作與介面

- 新增消耗耐力的閃避翻滾。基礎耐力為 50，每次翻滾消耗 12。
- 重做角色建立的起源頁。八個職業分頁顯示，提供職業資訊與起始武器選擇。
- 新增潛能充滿與釋放的顯示、共鳴預覽及飛行量表，並調整翅膀飛行時間的回復方式。
- 更新生命、魔力與耐力顯示，以及物品欄、裝備欄和標誌貼圖。

### 新坐騎與敵人

- 新增 Poka 坐騎與召喚哨，支援蓄力跳躍、緩降與水面浮力，並加入坐騎動畫與叫聲。
- 新增猩紅地形敵人高瞰者。牠會踏出震波、對起跳做出甩頭反擊，並在半血時減傷並呼喚援兵。

### 多人遊玩與設定

- 同步閃避翻滾、潛能、資源條外觀及 Poka 的動作與叫聲。翻滾請求加入重複與過期檢查，避免同一請求重複扣除耐力。
- 可分別調整單人、多人自己的坐騎及附近玩家坐騎的音量。伺服器可調整高瞰者的攻擊數值與反應機率。
- 多人遊玩時，伺服器與所有玩家請使用相同版本。

### 暫時移除與內容調整

- 蘑菇法杖（古蕈柱杖）與小蘑菇（菇寶寶）暫時移除，未來可能重新調整後回歸。相關的瓶中蘑菇也自本版移除。
- 移除目前版本的孢夢森林（孢夢之森）。未來預計以重新設計的環境內容取代，相關貼圖也將重新繪製。
- 本版移除舊版自訂世界建立選項、超大型世界建立功能及耶夢加德預覽。

## English

### Player Movement and Interface

- Added a stamina-based dodge roll. Base stamina is 50, and each roll costs 12.
- Rebuilt the Origin page with eight classes across two pages, class details, and starting weapon choices.
- Added potential charge and release indicators, a resonance preview, and a flight meter. Changed how wing flight time recovers.
- Updated health, mana, and stamina displays, along with inventory, equipment slot, and logo artwork.

### New Mount and Enemy

- Added the Poka mount and its summoning whistle, with a charged jump, slow fall, buoyancy, new animations, and a mount call.
- Added the Highwatcher to the Crimson. It creates a stomp shockwave, can counter a player's jump with a head strike, and reduces incoming damage while calling reinforcements at half health.

### Multiplayer and Settings

- Added multiplayer sync for dodge rolls, potential, resource bar appearance, and Poka's animations and call. Duplicate and expired roll requests are checked to prevent a request from spending stamina twice.
- Added separate mount volume controls for single-player, your own mount in multiplayer, and nearby players' mounts. Servers can tune Highwatcher attacks and reaction chances.
- Use the same version on the server and all clients.

### Temporary Removals and Content Changes

- Temporarily removed the mushroom staff (Ancient Agaric Staff) and Baby Mushroom. They may return after further adjustments. The related Mushroom in a Bottle was also removed from this build.
- Removed the current Spore Dream Forest. A redesigned environment is planned to replace it, and its textures will be redrawn.
- Removed the previous custom world creation options, extra-large world creation, and the Jormungandr preview from this build.

---

# 《輓歌》Vesper — Beta v0.2.0

**大型核心架構重構 / Major Core Architecture Refactoring**

---

## 繁體中文

本次 Beta 更新主要針對模組核心架構進行大規模重構，重新整理既有系統，並為後續職業、戰鬥機制、世界內容與多人遊戲功能的擴充建立基礎。

### 核心系統重構

- 重構職業系統，重新整理職業資料、分類、角色建立與初始裝備管理。
- 建立角色屬性系統的基礎架構，整合屬性資料、數值計算與配點機制。
- 建立職業詞綴系統的基礎架構，整理詞綴生成、抽取權重、取得來源與相關規則。
- 重整玩家狀態管理及資料同步機制。
- 重新組織各系統間的依賴關係，減少後續擴充與維護的複雜度。

### 世界與環境

- 大幅重構孢夢之森的世界生成架構。
- 重新整理地形、植被、環境效果及生態域相關系統。
- 調整孢夢之森生成規模與世界尺寸的適配邏輯。
- 整理世界建立介面、自訂難度及世界選項管理。
- 暫時停用尚未完成相容性修復的超大型世界建立功能。

### 遊戲內容

- 整理既有武器、物品、方塊、NPC 與召喚物的程式結構。
- 整合孢夢之森相關內容與新的世界系統。
- 新增三種可重新種植的孢夢矮菇物品。
- 整理耶夢加德相關程式架構。
- 調整部分召喚物與相關增益效果的資料及本地化內容。

### 介面與本地化

- 重構角色建立及職業選擇介面架構。
- 重新整理世界建立與自訂選項介面。
- 整合英文與簡體中文本地化資源。
- 補充部分物品、NPC、生態域及測試工具的翻譯。
- 統一模組內部資源與本地化引用。

### 相容性與開發維護

- 大幅重新組織模組原始碼及資源目錄。
- 統一內部 C# 命名空間。
- 保留原有模組識別及相關相容性資料結構。
- 重整網路封包管理、玩家資料同步與世界選項儲存架構。
- 改善核心系統的模組化程度，為後續內容更新建立基礎。

### 已知問題與限制

- 12000 × 3600 超大型世界建立功能暫時停用。
- 既有超大型世界的載入問題尚未修復。
- 部分新系統仍處於 Beta 開發與調整階段。
- 舊版角色、世界存檔及多人連線功能的完整相容性仍需進一步驗證。

本次更新以核心架構重構為主要目標，並不代表所有規劃中的遊戲內容均已完成。

更多職業、裝備、敵人、首領及遊戲機制將於後續版本逐步擴充。

感謝各位對《輓歌》的支持與耐心！

---

## English

This Beta update focuses on a major refactoring of the mod's core architecture, reorganizing existing systems and establishing a foundation for future expansions to classes, combat mechanics, world content, and multiplayer features.

### Core System Refactoring

- Refactored the class system, including class data, categories, character creation, and starting equipment management.
- Established the foundational architecture for character attributes, stat calculations, and attribute allocation.
- Established the foundations of the class-based prefix system, including generation rules, weighted selection, acquisition sources, and related mechanics.
- Reorganized player state management and data synchronization.
- Restructured dependencies between core systems to simplify future development and maintenance.

### World and Environment

- Significantly refactored the Spore Dream Forest world generation architecture.
- Reorganized terrain, vegetation, environmental effects, and biome-related systems.
- Revised world-size scaling logic for Spore Dream Forest generation.
- Reorganized world creation, custom difficulty, and world option management.
- Temporarily disabled extra-large world creation pending further compatibility fixes.

### Gameplay Content

- Reorganized the implementation of existing weapons, items, tiles, NPCs, and summoned entities.
- Integrated Spore Dream Forest content with the restructured world systems.
- Added three replantable Spore Dream mushroom cap items.
- Reorganized the Jormungandr-related code architecture.
- Updated selected summon-related data, buffs, and localization entries.

### User Interface and Localization

- Refactored the architecture of character creation and class selection interfaces.
- Reorganized world creation and custom option interfaces.
- Consolidated English and Simplified Chinese localization resources.
- Expanded translations for selected items, NPCs, biomes, and testing utilities.
- Standardized internal asset and localization references.

### Compatibility and Maintenance

- Significantly reorganized the mod's source code and asset directories.
- Standardized internal C# namespaces.
- Preserved the original mod identity and related compatibility data structures.
- Reorganized network packet handling, player synchronization, and world option persistence.
- Improved the modular organization of core systems to support future development.

### Known Issues and Limitations

- Extra-large world creation (12000 × 3600) is temporarily disabled.
- Loading issues affecting existing extra-large worlds remain unresolved.
- Some new systems remain under active Beta development and adjustment.
- Full compatibility with existing characters, world saves, and multiplayer functionality requires further verification.

This update primarily focuses on rebuilding the mod's core architecture. It does not mean that all planned gameplay content has been completed.

Additional classes, equipment, enemies, bosses, and gameplay mechanics will be introduced gradually in future updates.

Thank you for your continued support and patience!
