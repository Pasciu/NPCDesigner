# NPC Designer for Potion Craft

A medieval character customization workshop for **Potion Craft: Alchemist Simulator**.

NPC Designer lets you design, customize, randomize, and save custom Non-Player Characters (NPCs) directly from the game's main menu, set inside an authentic potion-shop counter scene with live in-game character animations and a native ingredient-style asset browser.

---

## Features

- **Authentic Potion-Shop Stage:**
  - Your character stands right behind the wooden shop counter, surrounded by the shop's warm plank wall, bookshelves, stacked tomes, cupboards, bottles, and wooden chest — framing the lower torso cleanly just like real customers in the shop, without queues, walking, or trading menus.
- **Living In-Game Character Movement & Reactions:**
  - Characters use the game's genuine idle head-tilt and eye movements: they smoothly follow your mouse cursor as you move around the screen and naturally glance around the room when your cursor rests.
  - Cycling **Umore** (*Arrabbiato*, *Contrariato*, *Neutro*, *Felice*, *Entusiasta*) updates facial expressions in real time and plays the game's authentic reaction effects on *Entusiasta* and *Arrabbiato*.
- **Ingredient-Style Visual Asset Panel:**
  - The right side uses Potion Craft's familiar parchment inventory panel with a **4×4 grid of clickable ingredient-style slots**.
  - Every slot displays a live visual preview of the actual clothing, face, eyes, hairstyle, beard, hat, accessory, or color swatch — just click any slot to equip it immediately onto your character.
  - The currently equipped slot is highlighted in warm gold.
- **Quick Group Tabs & Pagination:**
  - Jump directly between **Corpo**, **Stile**, **Colori**, and **Preset** across the top of the panel.
  - Use the `<` / `>` arrows below the tabs to browse subcategories, and `< Prec` / `Succ >` below the grid to flip pages when a category has many options.
- **Gender-Coherent Customization (*Genere*):**
  - Switch between **Maschio**, **Femmina**, and **Tutti** directly from the counter bar below the character.
  - Automatically filters bodies, faces, head shapes, hairstyles, and compatible bust options to match the chosen gender and outfit while keeping head and neck alignment natural.
- **One-Click Randomizer & Local Presets:**
  - Click **Casuale** for instant gender-coherent randomized characters.
  - Click **Salva Preset** to save creations locally and reload them anytime from the **Preset** tab without touching your game saves.

---

## Controls and Usage

- **Opening the Designer:**
  - Click **NPC Designer** on the Title Screen, or press **F6**.
- **Equipping Assets:**
  - Pick a group at the top right (**Corpo**, **Stile**, **Colori**, or **Preset**).
  - Switch subcategory using `<` and `>` (for example *Corpo*, *Viso*, *Occhi*, *Cranio*, *Seno*, *Capelli*, *Barba*, *Cappello*, *Accessori*, or *Colori*).
  - Click any slot in the 4×4 grid to immediately equip that asset or color onto your character.
  - Use `< Prec` and `Succ >` to browse additional pages.
- **Gender, Mood, Randomize & Save:**
  - Click **Genere: ...** on the counter bar to switch between *Maschio*, *Femmina*, and *Tutti*.
  - Click **Umore: ...** on the counter bar to cycle expressions and trigger happy/angry reactions.
  - Click **Casuale** to randomize or **Salva Preset** to save.
- **Returning to Main Menu:**
  - Click **< Torna al Menu**, or press **Escape** (or **F6**).

---

## Installation & Requirements

1. **Prerequisites:**
   - **Potion Craft: Alchemist Simulator** (Steam, GOG, or PC Game Pass).
   - **BepInEx 5.4.x (Mono x64)** installed in the game directory.
2. **Installation:**
   - Copy `NPCDesigner.dll` into `Potion Craft/BepInEx/plugins/NPCDesigner/`.
   - Launch the game.
3. **Preset Storage Location:**
   - Saved presets are stored in `Potion Craft/BepInEx/plugins/NPCDesigner/Presets/`.

---

## Building from Source

Requires the `.NET SDK` and a legitimate local installation of *Potion Craft: Alchemist Simulator*:

```powershell
dotnet build -c Release -p:GameRootPath="C:\Path\To\Potion Craft"
```

Pass `-p:DeployToGame=true` to automatically copy `NPCDesigner.dll` into `BepInEx/plugins/NPCDesigner/`.

Built with **[Candied](https://github.com/Pasciu/Candied)** · Licensed under the **[MIT License](./LICENSE)**.

---

## Technical Details

For developers and modders interested in the architecture, hooks, and technical implementation details, refer to the [Technical Architecture & Hooks Guide](Patches/README.md).
