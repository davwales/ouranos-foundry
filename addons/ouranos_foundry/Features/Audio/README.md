# Audio

Sound effects, music and volume for any game. Sound effects play on a pool of reused players with per-cue polyphony limits, music crossfades between tracks and playlists, and each audio bus gets a linear 0..1 volume that the save system can persist.

## Pieces

| Type | Kind | Role |
|------|------|------|
| `AudioManager` | Node, `ISaveable` | The autoload: `Sounds`, `Music`, and `SetVolume`/`GetVolume`/`SetMuted`/`IsMuted` per bus. Uses a child `SoundPool` and `MusicPlayer`, or creates them. |
| `SoundPool` | Node | One-shot sound effects: `Play(cue)`, `Play2D(cue, position)`, `Play3D(cue, position)`, `Stop(cue)`, `StopAll`, `GetPlayingCount(cue)`, `SetRng`. |
| `MusicPlayer` | Node | Music with crossfades: `Play(track, fade)`, `PlayPlaylist(playlist, fade)`, `Stop(fade)`, `CurrentTrack`, `CurrentPlaylist`, `SetRng`. Signal `TrackChanged(track)`. |
| `VolumeSlider` | HSlider | A volume slider for one `Bus`. No code needed. |
| `AudioCue` | Resource | A sound effect: `Streams` to pick from, `VolumeDb`/`VolumeVarianceDb`, `PitchScale`/`PitchVariance`, `Bus`, `MaxPolyphony`. |
| `MusicPlaylist` | Resource | `Tracks`, `Shuffle`, `Loop`. |

## Usage

1. Add `AudioManager.cs` as an autoload (*Project Settings → Globals → Autoload*), named e.g. `Audio`. It creates `SFX` and `Music` buses if your bus layout doesn't have them.
2. Create an `AudioCue` per sound (**New Resource → AudioCue**) and add a few variations of it to `Streams`.
3. Play sounds and music:

```csharp
var audio = GetNode<AudioManager>("/root/Audio");

audio.Sounds.Play(clickCue);                       // UI and other non-positional sounds
audio.Sounds.Play2D(hitCue, enemy.GlobalPosition); // or Play3D in a 3D game
audio.Music.Play(dungeonTheme);                    // crossfades over FadeDuration
audio.Music.PlayPlaylist(overworldPlaylist);
audio.SetVolume("Music", 0.5f);
```

For a settings menu, add a `VolumeSlider` per bus and set its `Bus`.

From GDScript, call the same members and pass every argument, because C# default arguments aren't exposed to it: `Audio.Music.Play(theme, -1)`, where `-1` (`MusicPlayer.DefaultFade`) means "use `FadeDuration`".

### Saving volumes

`AudioManager` is an `ISaveable` that stores every bus's volume and mute under `SaveKey` (`"audio"`). Volumes are usually settings rather than game state, so give them their own slot instead of adding the manager to the `saveable` group, which would put them in every game save:

```csharp
settings.Register(audio);          // a SaveManager, e.g. with SaveDirectory "user://settings"
if (settings.HasSave("settings"))
{
    settings.Load("settings");
}

// After the player changes a volume:
settings.Save("settings");
```

## Semantics

- **Volume** is linear amplitude, from 0 (silent) to 1 (the bus at 0 dB), clamped to that range. It's stored on the `AudioServer`, so the manager, a `VolumeSlider` and your code all see the same value. A missing bus pushes an error and reads as 0. `Load` ignores buses missing from the current layout.
- **Cues:** each play picks one of `Streams` at random, shifts the volume by up to ±`VolumeVarianceDb` and scales the pitch by up to ±`PitchVariance`. A cue with an empty `Bus` plays on the pool's `Bus` (default `SFX`). A player whose bus doesn't exist plays on Master.
- **Determinism:** picks come from a `SeededRng` (fixed seed by default). `SoundPool.SetRng` and `MusicPlayer.SetRng` swap in your own stream, and `AudioCue.Pick(rng)` gives the same pick for the same RNG state.
- **Voices:** the pool creates players as they're needed, up to `MaxVoices` (default 32), and reuses finished ones. When a cue already has `MaxPolyphony` voices playing (0 means no limit), playing it again restarts its oldest voice. When every voice is busy, the oldest voice overall is restarted with the new sound. So the newest sounds always play, and capping spammy cues (footsteps, hits) keeps them from crowding out the rest. `Play*` returns false only for a null or empty cue, or a pool outside the tree.
- **Positional sounds** play at a fixed position, on players that are children of the pool, so they play in the pool's viewport. For a sound that follows a moving node, or one in a `SubViewport`, use your own `AudioStreamPlayer2D`/`3D` there (you can still call `cue.Pick(rng)` for its variations), or put a `SoundPool` in that viewport.
- **Music:** `Play` fades the current track out and the new one in over `FadeDuration` (default 1.5 s), or the duration you pass (0 cuts). Playing the track that's already playing does nothing, so re-entering an area doesn't restart its music, and a track that is fading out fades back in. `Play` doesn't repeat a track: turn on looping in the stream's import settings. `Stop` fades out, and `TrackChanged` reports every change, with null when the music stops.
- **Playlists** crossfade into the next track as the current one ends (or loops back to its start). `Shuffle` plays every track once per pass in random order, never the same track twice in a row. Without `Loop`, the music stops after the last track. Calling `PlayPlaylist` with the playlist that's already playing does nothing. Set `Playlist` and `Autoplay` to start one when the player is ready.
- **Pausing:** `AudioManager` defaults to `ProcessMode.Always`, so music keeps playing and fading in pause menus. Set its `ProcessMode` (or a child's) to `Pausable` to pause it with the game.

## Extending

- **Custom selection** (never repeat the last stream, pick by surface material): subclass `AudioCue` and override `Pick`.
- **Attenuation and panning:** subclass `SoundPool` and override `CreatePlayer(positioning)` to configure the `AudioStreamPlayer2D`/`3D` it creates (`MaxDistance`, `Attenuation`, `UnitSize`, …). Return the same player type.
- **More saved settings:** subclass `AudioManager` and override `Save`/`Load`, calling the base methods.
- **Several pools** (UI, world, voice-over, each with its own voice budget): add more `SoundPool` nodes, e.g. in the scene whose viewport they belong to.
