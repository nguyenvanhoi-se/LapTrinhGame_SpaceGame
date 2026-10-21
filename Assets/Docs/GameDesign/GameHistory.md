# Game History

MainMenu contains a `GameHistoryUI` panel opened by HISTORY. BACK restores the
existing menu buttons. CLEAR HISTORY opens a blocking confirmation; CANCEL keeps
all records and DELETE clears only history. The scroll list uses the shared
`GameHistoryRow` prefab and existing Audiowide TMP asset / `ButtonTextState` helper.
No additional scene or build-setting entry is required.

## Run lifecycle

`GameManager.SetupLevel` creates a new GUID and clock for each scene instance.
`LevelComplete` and `GameOver` finalize once, before the existing score reset and
scene transition. The record snapshots `GetLevelScore()` separately from optional
`GetTotalScore()`. A completed run cannot subsequently become a Game Over record.
Restart creates another scene instance/run; Resume keeps the same run; leaving
through Pause > MAIN MENU does not save an unfinished run.

Time is monotonic real elapsed time while GameManager is Playing, excluding its
Paused state. Resume resets the clock before gameplay resumes. It includes the
existing level-start banner and does not scale with slow motion. No ship, score,
unlock, damage, spawn or difficulty settings are changed by this feature.

## Persistence

`GameHistoryManager` is one lazily created persistent service. Its
`GameHistoryStore` owns the data; returned records are copies. AddHistory,
GetHistory, ClearHistory, SaveHistory and LoadHistory are available on the manager.

File: `Application.persistentDataPath/game_history.json`.
On this Windows project: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/SpaceGame/game_history.json`.

Schema version 1:

```json
{
  "schemaVersion": 1,
  "records": [
    {
      "id": "<32-character GUID>",
      "level": 2,
      "score": 200,
      "hasTotalScore": true,
      "totalScore": 300,
      "playedSeconds": 288.0,
      "endedAt": "2026-10-21T10:00:00.0000000+07:00",
      "result": "COMPLETED"
    }
  ]
}
```

This example is documentation only. No sample records are installed in player
data. Result is COMPLETED or GAME OVER. `endedAt` retains the machine UTC offset;
the UI displays the local date/time. At most 100 newest records are retained,
ordered by timestamp. IDs are deduplicated on add and load.

Writes flush a unique temporary file in the same directory, then atomically
replace the main file, retaining `.bak`. Failed writes retain pending records in
memory and retry on application pause/quit or SaveHistory. Concurrent external
file changes are detected and preserved. Missing files load empty without writing.
Empty/corrupt files are preserved; a valid backup can be read. Before a subsequent
successful save, unreadable original bytes are copied to a unique
`.unreadable-<timestamp>-<GUID>` archive. Future schema files are never overwritten.
Confirmed clear empties both main and recovery copy, without touching PlayerPrefs.
Unreadable-file archives remain available for manual recovery.

## Verification

Use **Tools > Space Explorer > Verify Game History** in Edit Mode after saving
open scenes and prefabs. The editor harness checks storage errors, deduplication,
retention, real Level1–Level4 completion, PlayerHealth death, pause duration,
restart/abandonment, unlock preservation, UI raycast clicks, confirmation,
scrolling and a second fresh Play Mode session. It uses only isolated JSON under
`Temp/GameHistoryWork`, restores the original progress/preferences and scene setup,
and verifies that the real history file was not modified.

Results: `Temp/GameHistoryWork/results.txt`. Preview images can be generated with
**Tools > Space Explorer > Render Game History Preview**; synthetic preview rows
exist only in temporary preview scenes. A standalone player build and device
touch-input test remain separate manual checks.
