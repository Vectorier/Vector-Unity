# Runtime Level Editor V1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Edit existing runners in a loaded XML level, playtest without reloading the scene, and save a separate playable XML copy.

**Architecture:** Keep the loaded XML and each runner's source node associated with the runtime graph. A runtime session owns authored values and switches between edit and playtest; Unity Editor menu commands and Scene-view handles are only its first controls. Export clones source XML, applies edited values, and clones shared definitions when one instance changes.

**Tech Stack:** Unity 6000.3.11f1, C#, `System.Xml`, Unity Editor SceneView/Handles, existing `Location`/`Runner` model.

**Spec:** `docs/superpowers/specs/2026-09-30-runtime-level-editor-v1-design.md`

## Global Constraints

- Edit existing runners only; do not create or delete runners.
- Support position edits for existing runners and width/height edits for platform, trapezoid, trigger, and area runners.
- Keep the loaded `Location` and scene through edit/playtest transitions.
- Save a new XML level; never overwrite source level or shared source XML.
- Preserve inactive variants, trigger programs, choices, and untouched XML.
- Add Unity Editor menu controls; do not modify game menu screens or UI prefabs.
- Preserve the user's unrelated working-tree changes; stage and commit only task files.

## Review Focus

- A shared named object appears twice: editing one instance changes only that instance in exported XML (Task 4 verification).
- The current mode filters a choice variant: export keeps the inactive branch intact (Task 4 verification).
- A trigger has nested events and template references: position editing retains its complete program (Task 4 verification).
- A save name contains `..`, a separator, or matches a built-in level: Save As rejects it without changing files (Task 4 verification).
- Edit/playtest repeats after resizing a quad: collision grid contains the quad once at its edited bounds (Tasks 2 and 3 verification).

## File map

- `Assets/Scripts/Nekki/Vector/Core/Location/{Sets,Elements,ObjectRunner,Runner}.cs`: source XML provenance and authored baselines.
- `Assets/Scripts/Nekki/Vector/Core/Location/LevelCreation/BaseObjectRunner.cs`: parent and source-node access for nested objects.
- `Assets/Scripts/Nekki/Vector/Core/Location/{QuadRunner,TrapezoidRunner,Location}.cs`: geometry editing, reset baselines, and reset order.
- `Assets/Scripts/LevelEditor/{RunnerEdit,RuntimeLevelEditorSession,LevelXmlExporter}.cs`: edit adapter, mode state, and lossless Save As.
- `Assets/Scripts/{LevelMainController,LevelSceneController,DebugMenu}.cs`: pause input/simulation and route playtest endings.
- `Assets/Editor/{RuntimeLevelEditorMenu,RuntimeLevelEditorSceneTools,RuntimeLevelEditorSelection,RuntimeLevelEditorVerifier}.cs`: menu, handles, Undo-backed selection proxy, and executable checks. Unity-generated `.meta` files accompany new files.

## Verification command

Use Unity's installed editor at `C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe`. For source, geometry, and XML verifiers, close any other editor instance using this project, then run `& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\camil\OneDrive\Documents\GitHub\Vector-UnityProject' -executeMethod RuntimeLevelEditorVerifier.<Method> -logFile 'C:\Users\camil\OneDrive\Documents\GitHub\Vector-UnityProject\Logs\level-editor-verification.log'`. Exit code 0 plus `<Method> PASS` in the log means pass. Run session and control verifiers from their Editor menu items while a level is running in Play Mode; they log the same PASS marker or throw an assertion.

### Task 1: Retain XML source provenance

**Files:** Modify `Sets.cs`, `Elements.cs`, `ObjectRunner.cs`, `Runner.cs`, `BaseObjectRunner.cs`; create `Assets/Editor/RuntimeLevelEditorVerifier.cs`.

**Interfaces:** Produce `Sets.LevelDocument : XmlDocument`, `Sets.SourceFiles : IReadOnlyDictionary<XmlDocument,string>`, `Sets.RootObjects : IReadOnlyList<BaseObjectRunner>`, `Runner.SourceNode : XmlNode`, `BaseObjectRunner.Parent : BaseObjectRunner`, `BaseObjectRunner.SourceNode : XmlNode`, and `BaseObjectRunner.DefinitionNode : XmlNode`. Add `Sets(XmlDocument document, string sourcePath, string xmlRootPath)`; the existing constructor delegates to it with production paths.

- [ ] **Step 1: Add `RunSourceBindings` verifier.** Load a level containing direct and named-object runners; assert every active runner has its exact source node, its owning document maps to a filename, and an inactive variant still exists in the source document.
- [ ] **Step 2: Run `RunSourceBindings`; expect failure** because provenance properties are absent.
- [ ] **Step 3: Implement the interfaces.** Assign runner source nodes in `Elements.Parse`; retain instance and definition nodes in `ObjectRunner.Parse`; record level and dependency documents in `Sets`. Use the injected XML root for dependency reads so verifiers use temporary fixtures without changing project assets.
- [ ] **Step 4: Run `RunSourceBindings`; expect `RunSourceBindings PASS`.**
- [ ] **Step 5: Commit only Task 1 files** as `feat: retain level XML source bindings`.

### Task 2: Edit runner geometry and establish authored defaults

**Files:** Create `Assets/Scripts/LevelEditor/RunnerEdit.cs`; modify `Runner.cs`, `QuadRunner.cs`, `TrapezoidRunner.cs`, `Location.cs`, and `RuntimeLevelEditorVerifier.cs`.

**Interfaces:** Produce static `RunnerEdit.Read(Runner) : RunnerEditState`, `RunnerEdit.Apply(Runner, RunnerEditState, Location) : void`, and `RunnerEdit.CaptureBaseline(Runner) : void`; `RunnerEditState` stores `X`, `Y`, optional `Width`, and optional `Height` as floats. Reject non-finite positions and nonpositive quad dimensions. For trapezoids, `Height` edits the left height while preserving the right height.

- [ ] **Step 1: Add `RunRunnerEdits` verifier.** Move a spawn, visual, item, platform, trigger, and area; resize the three quads and a trapezoid. Assert runner positions, quad bounds, collider bounds, and grid collection agree after two edit/reset cycles. Assert invalid dimensions leave geometry unchanged.
- [ ] **Step 2: Run `RunRunnerEdits`; expect failure** because `RunnerEdit` is absent.
- [ ] **Step 3: Implement `RunnerEdit` and baseline capture.** Remove a quad from `Location.Grid` before mutation and reinsert afterward; update runner geometry, any existing collider, and reset defaults. Implement trapezoid width/left-height resizing while retaining right height. Ensure `QuadRunner.SetNormals` replaces rather than accumulates normals when geometry is rebuilt.
- [ ] **Step 4: Run `RunRunnerEdits`; expect `RunRunnerEdits PASS`.**
- [ ] **Step 5: Commit only Task 2 files** as `feat: edit runner geometry in place`.

### Task 3: Switch edit and playtest in one loaded level

**Files:** Create `Assets/Scripts/LevelEditor/RuntimeLevelEditorSession.cs`; modify `LevelMainController.cs`, `LevelSceneController.cs`, `DebugMenu.cs`, `Location.cs`, and `RuntimeLevelEditorVerifier.cs`.

**Interfaces:** Produce `RuntimeLevelEditorSession.Active : RuntimeLevelEditorSession`, `Mode : RuntimeLevelEditorMode` (`Editing`/`Playtesting`/`Closed`), static `Enter(Location location, Sets source) : RuntimeLevelEditorSession`, `BeginPlaytest() : void`, `EndPlaytest() : void`, `Close() : void`, and `ApplyEdit(Runner runner, RunnerEditState state) : void`. `ApplyEdit` replaces the selected runner's authored snapshot; gameplay transformations never do. `BeginPlaytest` and `EndPlaytest` use `Location.Reload` and do not call `SceneManager.LoadScene`.

- [ ] **Step 1: Add `RunSessionTransitions` verifier.** Assert edit mode stops simulation and gameplay input, playtest starts at edited spawn and platform bounds, stopping restores the authored values, and repeated transitions do not duplicate grid entries or change scene identity. Verify an unresolved player spawn refuses playtest and win/death exits playtest before campaign stats or rewards are written.
- [ ] **Step 2: Run `RunSessionTransitions`; expect failure** because session API is absent.
- [ ] **Step 3: Implement session transitions and controller guards.** Reset runners before models on playtest reset so spawns use authored positions; route win/death playtest completion to `EndPlaytest`, while manual restart restarts the current playtest.
- [ ] **Step 4: Run `RunSessionTransitions`; expect `RunSessionTransitions PASS`.**
- [ ] **Step 5: Commit only Task 3 files** as `feat: switch level editor and playtest in place`.

### Task 4: Export a separate XML level

**Files:** Create `Assets/Scripts/LevelEditor/LevelXmlExporter.cs`; modify `RuntimeLevelEditorVerifier.cs`.

**Interfaces:** Produce static `LevelXmlExporter.SaveAs(Sets source, IReadOnlyDictionary<Runner,RunnerEditState> edits, string newLevelId, string outputXmlRoot) : string`, returning the saved level path; the menu supplies `VectorPaths.XmlRoot`, tests supply a temporary root. Accept IDs matching `^[A-Za-z0-9_]+$`; reject collisions with any existing level. Stage dependencies in `custom/<id>/`, then promote the new `levels/<id>.xml` last after validation; remove staged/promoted files on failure.

- [ ] **Step 1: Add `RunXmlExport` verifier.** Build a fixture with a direct platform, a shared named object referenced twice, an inactive variant, and a nested trigger program. Edit one shared instance; assert the saved XML reloads with only that instance changed and all other nodes intact. Assert invalid/colliding IDs write nothing.
- [ ] **Step 2: Run `RunXmlExport`; expect failure** because exporter is absent.
- [ ] **Step 3: Implement source-backed export.** Clone the level XML and dependency closure; use runner type dispatch to apply only edited attributes. Convert runtime position to the source node's coordinates by adding the authored position delta to the original `X`/`Y` values. For an edit sourced from a shared definition, clone each shared definition along its instance path under a unique name, redirect that instance's `Name`, and update cloned `Sets` references. Preserve the meaning of every untouched XML node. Validate required root sections, referenced files/objects, and player spawn names before promotion.
- [ ] **Step 4: Run `RunXmlExport`; expect `RunXmlExport PASS`.**
- [ ] **Step 5: Commit only Task 4 files** as `feat: export edited level XML copy`.

### Task 5: Unity Editor menu and Scene-view controls

**Files:** Create `Assets/Editor/RuntimeLevelEditorMenu.cs`, `Assets/Editor/RuntimeLevelEditorSceneTools.cs`, `Assets/Editor/RuntimeLevelEditorSelection.cs`; modify `RuntimeLevelEditorVerifier.cs`.

**Interfaces:** Menu commands `Vector Level Editor/Enter Runtime Editor`, `Playtest`, `Stop Playtest`, `Save As`, and `Leave Runtime Editor`. Scene-view selection binds to `Runner`, position handles call `RuntimeLevelEditorSession.ApplyEdit`, and edge handles change supported dimensions. `RuntimeLevelEditorSelection : ScriptableObject` stores the selected authored state; `Undo.RecordObject` and `Undo.undoRedoPerformed` route Unity Undo/Redo back through `ApplyEdit`. A small Scene-view overlay shows selected type/name/values; it changes no game UI prefab.

- [ ] **Step 1: Add `RunEditorControls` verifier.** In Play Mode with a loaded level, assert command enablement by session mode, `ApplyEdit` changes the selected runner and session snapshot, and Undo/Redo restores both. Confirm Save As uses a unique name and Leave does not alter built-in XML; exercise Scene-view handles manually in Step 4.
- [ ] **Step 2: Run `RunEditorControls`; expect failure** because menu and tools are absent.
- [ ] **Step 3: Implement menu validation and Scene-view picking/handles.** Disable commands when no `LevelMainController.current.Location` exists. Route controls through the session API; display an explicit error for unsupported fields.
- [ ] **Step 4: Run `RunEditorControls`; expect `RunEditorControls PASS`.** Then manually load `CONSTRUCTION_STORY_01`, edit a platform and spawn, playtest without scene reload, stop, Save As, and launch the saved XML through `-level`.
- [ ] **Step 5: Commit only Task 5 files** as `feat: add runtime level editor controls`.

## Final verification

- [ ] Run source, geometry, and XML verifiers in a fresh Unity Editor process; run session and controls verifiers in Play Mode. Inspect logs for compiler errors and assertions.
- [ ] Repeat the manual edit/playtest/export flow on an unmodified level and one containing a shared object and inactive variant.
- [ ] Run `git diff --check` and review the branch diff against the approved spec and unrelated working-tree changes.
