# Group26_DatabaseConnectivity

## Project Title
**Climate Defense — Database-Connected Tower Defense Prototype**

## Group Number and Members
- **Group 26**
- [Jherwin Reyes] — Game Logic Developer
- [Jon Francis Gellido] — Database Developer
- [Rodge Emmanuel Ramos] — UI & Testing Lead
- [Edz Bedirico] — Documentation Lead

## Description
A 2D tower-defense prototype built in Unity 6 LTS. The player deploys up to 5 unit types across 6 lanes to defend a Climate Bar from incoming enemy waves. Oxygen is the resource, decreasing each turn. The player wins by clearing 3 waves and loses if the Climate Bar reaches 6. On Game Over, the run is saved to a local SQLite database and displayed in an in-game leaderboard.

## Tools and Technologies
- Unity 6 LTS (6000.0.54f1)
- C#
- TextMeshPro
- sqlite-net + native SQLite libraries
- Git / GitHub

## Database or Storage Used
**SQLite** — local file stored at `Application.persistentDataPath/group26_scores.db`.

Table: `ScoreRecord`

| Field | Type | Description |
|---|---|---|
| player_id | INTEGER PRIMARY KEY AUTOINCREMENT | Unique record identifier |
| player_name | TEXT | Name entered by the player at start |
| score | INTEGER | Enemies × 10 + Waves × 100 + Win bonus 500 |
| wave_reached | INTEGER | Wave 1–3 |
| climate_state | INTEGER | Final climate state (0–6) |
| oxygen_remaining | INTEGER | Oxygen left at end |
| result | TEXT | WIN or LOSE |
| created_at | TEXT | Timestamp (yyyy-MM-dd HH:mm:ss) |

## How to Run
1. Clone this repository.
2. Open the project in **Unity 6 LTS (6000.0.54f1)**.
3. Ensure SQLite native libraries are present in `Assets/Plugins/` (see `docs/SQLiteSetup.md` if included).
4. Open `Assets/Scenes/SampleScene.unity`.
5. Press **Play** in the Editor, or build for **Windows** / **Android** via File → Build Profiles.
6. Enter your name on the start screen, play a round, and view the leaderboard.

## What Data Is Saved and Retrieved
- **Saved:** One record per completed playthrough (win or lose) with player name, score, wave reached, final climate state, remaining oxygen, result, and timestamp.
- **Retrieved:** Top 10 records sorted by score descending, shown in the in-game Leaderboard panel.

## Known Limitations
- SQLite is local per device; no cloud sync across devices.
- No account system; player names are free-text.
- Leaderboard is not paginated (limited to top 10).
- Android build tested on one physical device only.
- No achievements or badges beyond the win/lose result.

## References
- sqlite-net: https://github.com/praeclarum/sqlite-net
- SQLite4Unity3d: https://github.com/robertofalconi/SQLite4Unity3d
- Unity 6 Documentation: https://docs.unity3d.com/6000.0/Documentation/Manual/
- TextMeshPro: https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest
