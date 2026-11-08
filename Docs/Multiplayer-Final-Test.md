# Multiplayer final test

Use ArenaGameplay in Unity 6000.6.3f1. Its scene and NetworkPlayer prefab were configured and saved. The older Configure Step A Scene command is for the initial connection-only setup.

1. Build a new Windows copy with ArenaGameplay first in File > Build Profiles > Scene List. Put the output on D: under Builds.
2. Press Play in the Editor and click Host. Launch the Windows build, enter 127.0.0.1 and click Join. Expect a count of 2 and two player rigs. Use LAN Menu to collapse the panel.
3. After the countdown, compare both timers. Move or turn each rig and check remote head and hand tracking. Use an XR headset or the Editor simulator for weapon and locomotion inputs.
4. Land four valid hits on the other player. Compare health, kills and deaths, then check that the target respawns at full health after 3 seconds. Verify that walls stop shots and a dead player cannot fire.
5. Let the match finish and compare the winner message. Stop the client, reconnect, then stop the host and verify the client returns to the menu. On LAN, confirm the sixth connection is rejected.
6. For headset verification, build for Android, use the host's LAN IP instead of loopback, and test both controller rays and the wrist HUD.

The host starts the 300-second match once two players are present. The limit is five players including the host. The scene keeps the original XR rig for the menu and hands tracking to the spawned local player. Respawn and initial spawn use the existing arena spawn validation. The shot validator uses a 0.60 m origin tolerance, 15-degree aim tolerance and 25 damage. Test shot tolerance while players move.

Game Rules review covers PlayerHealth, PlayerScore, MatchManager, ArenaSkyDisplay and ArenaSpawnManager. The combat module's source and public contracts were preserved. The network relay uses the existing shot-result factory and suppresses haptics on remote copies.