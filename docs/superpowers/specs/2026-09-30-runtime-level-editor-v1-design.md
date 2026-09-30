# Runtime Level Editor V1

## Intent and scope

Let a creator load an existing XML level, enter editor mode while that level is running, change existing runners, playtest immediately in the same loaded scene, return to editing, and save a separate XML level. The first entry point is a Unity Editor top-menu command available during Play Mode. It adds no game menu screens; the editor core must remain usable by a later in-game entry point.

V1 edits position for existing runners and width/height for existing platform, trapezoid, trigger, and area runners. It does not create or delete runners, edit trigger programs, or package new art. Saving never overwrites the source level.

## Existing architecture

`Elements.Parse` creates runners from XML. `ObjectRunner` retains nested object order in `LayerOrder`; `BaseSets` flattens active runners into `AllRunners` and type-specific lists. Some runners, especially quads, lack a GameObject during ordinary play, so the editor selects the runner model and draws its own handles. The original XML document remains available alongside the runtime graph. Named objects may pull data from shared XML files, and variant selection removes inactive branches from the runtime graph.

`Location.Reload` resets runner simulation state against defaults recorded on load. Quad geometry and the collision grid are separate from Unity transforms. Editor changes must update the runner's geometry and grid, then establish a new playtest baseline; simply moving a Transform is insufficient.

## Components and data flow

1. **Editor session.** A `RuntimeLevelEditorSession` refers to the current `Location`, loaded XML source, selected runner, and an immutable authored-state snapshot. It exposes `EnterEdit`, `BeginPlaytest`, `EndPlaytest`, and `SaveAs`. It never calls `SceneManager.LoadScene` during the edit/playtest loop.
2. **Entry and controls.** Unity Editor menu commands enter/leave edit mode, start/stop playtest, and Save As. Scene-view picking and handles operate on existing runtime runners. Gameplay ticks and gameplay input are suspended in edit mode. The selected runner's type, name, position, and supported dimensions are inspectable without modifying game UI prefabs.
3. **Edit operations.** A small runner-specific adapter layer reads and writes authored fields. Quad edits remove the old quad from the spatial grid before mutation, rebuild geometry, then add the updated quad. Moving a spawn changes the baseline used when the player is reset. All edits update the authored snapshot and support Unity Undo where the Unity Editor controls are used.
4. **Playtest.** Starting a playtest commits the current authored values as runner reset defaults, resets triggers, collectibles, models, camera, and simulation clocks, then resumes gameplay in the same `Location`. Stopping playtest restores the authored snapshot and editor handles. Win/death in a playtest returns to edit mode rather than campaign progression or Snail Runner's automatic restart.
5. **XML export.** `Elements`-style type dispatch writes supported authored runner fields. Export begins from a clone of the source XML, preserving all unedited nodes, attributes, inactive variants, trigger content, choices, and level-wide metadata. For a runner instantiated from a shared named object, the export creates a level-specific copy of the referenced definition and redirects only the edited instance, preserving other instances. Save As writes a new level XML and any copied dependency XML beside the game's XML resource tree, using a validated unique name. The saved level can be launched by the existing `-level` path.

## Error handling and validation

The menu commands are enabled only when a level is loaded in Play Mode. Editing refuses unsupported runner fields with an explicit message. Save As validates the filename, writes to a temporary file, reparses the output with the existing XML parser, and replaces the destination only after validation succeeds. An export failure leaves the source and any earlier saved copy intact. The editor reports unresolved object references and invalid spawn references before playtest or save.

## Verification

- Load a representative built-in XML level, enter edit mode, move a platform and spawn, resize a platform, and begin playtest without a scene reload. Check visual placement, collision, player spawn, and return to the same edited state.
- Save a new XML level, launch it through `-level`, and compare runner counts, type, hierarchy, and edited values with the authored snapshot.
- Repeat with a level containing a named shared object and inactive choice variant. Confirm the exported copy preserves both and leaves the original files unchanged.
- Verify win, death, restart, and edit/playtest transitions do not write campaign rewards or replace the authored snapshot with transient gameplay positions.

## Deferred

An in-game menu entry point, built-player editing controls, adding/deleting runners, property editing beyond position and the listed dimensions, external asset packaging, and community-level browsing are later increments. The runtime session and exporter interfaces are independent of Unity Editor UI so these additions do not require replacing the core.
