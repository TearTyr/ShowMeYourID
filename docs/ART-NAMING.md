# Art folders and naming

All art the game loads at run time lives under `Assets/_Project/Resources/Art/`. The game finds a
sprite by its **key**, which is the file name without `.png`. Keys are unique across every folder,
so a file can move between folders without any code change.

## Folders

| Folder | What goes there |
|---|---|
| `Characters/` | Student busts, anomaly busts, the administrator, the guard, hooded figure |
| `Environment/Hallway/` | Hallway backgrounds and doors |
| `Environment/Rooms/` | Classroom and comfort room backgrounds |
| `Environment/Props/` | Doors, the clock, the token (cut to the visible art) |
| `Environment/Inspect/` | `inspect_*` close ups shown beside the text when a clue is inspected |
| `UI/SecurityDesk/` | Everything on the security desk screen |
| `UI/MainMenu/` | Everything on the main menu |

The source files (PSDs, full size exports) stay in the top level `Art/` folder, outside `Assets`,
so Unity does not import them. The `Tools/prep_*.py` scripts cut the source art into the game ready
files and write them to the folders above.

## Naming

Lowercase, `snake_case`, and `<category>_<subject>_<detail>`:

- `char_student_03` and `char_student_03_anomaly`: a student bust and the same person, wrong
- `char_admin`, `char_admin_glitch`: the administrator at the badge and at the window
- `char_guard_idle`, `char_guard_walk_a`, `char_guard_walk_b`: the player
- `bg_hallway_lit`, `bg_hallway_dark`, `bg_classroom_1`, `bg_comfort_room`: backgrounds
- `prop_clock`, `prop_door_classroom_1`: set dressing
- `inspect_noticeboard`, `inspect_clock`: close ups for clue inspection
- `sec_*` for the security desk and `menu_*` for the main menu

Do not use spaces, capitals or words like `final`, `new` or `temp` in a file name. Every `.png` needs its
`.meta` file next to it (run `python Tools/generate_meta.py` after adding files).
