# Combat module

This folder owns weapon input, ammunition, shot requests, host-side shot validation,
cover blocking and weapon feedback. It deliberately does not own health, death,
respawning, kills or scores.

## Local setup

1. Add `ShotValidator` to a scene-level object.
2. Create a gun object and add `AmmoController`, `LaserWeapon`, `WeaponInput`,
   `LocalCombatNetworkBridge` and (optionally) `WeaponEffects`.
3. Add a child transform at the barrel tip and assign it as the weapon muzzle.
4. Assign the scene validator to `LocalCombatNetworkBridge`.
5. Assign Input System actions to `WeaponInput`. The fire action should be bound to
   the controller trigger; reload may use a controller button.
6. Put arena cover and player hit colliders on layers included by the validator's
   collision mask.
7. Add `DebugDamageableTarget` to a cube/player dummy for local testing. Replace it
   with the game-rules implementation during integration.

The first non-shooter collider hit by the ray stops the laser. A collider without an
`IDamageable` implementation is therefore treated as cover.

## Networking contract

`LocalCombatNetworkBridge` exists only for independent development. The networking
owner should implement another `CombatNetworkBridge` that:

1. sends `ShotRequest` to the host;
2. rejects duplicate sequence numbers and non-owning clients;
3. validates the request with the host's authoritative muzzle/player state;
4. keeps authoritative ammunition and fire-rate state on the host;
5. applies validated damage through `IDamageable` on the host; and
6. returns/broadcasts the result, then calls `ResolveAuthoritativeShot` for effects.

Never let a client directly choose the victim, damage amount, health, kill count or
score. `ShotValidator` uses its own configured damage and range rather than trusting
those values from the request.

## Game-rules contract

The game-rules owner implements:

- `IDamageable` on player hit targets to receive validated `DamageInfo`;
- `ICombatantState` on the local/network player to expose the player ID and prevent
  firing while dead, respawning, frozen or spectating.

The rules system remains responsible for health, death, respawn, kill attribution
and score updates.

## XR feedback contract

Controller-specific vibration can be connected by implementing `IWeaponHaptics` and
assigning that component to `WeaponEffects`. This keeps the module compilable before
the team selects and installs its XR provider packages.

## Development disclosure

AI assistance was used to design, implement and review this combat foundation. The
team must include that fact, the files affected and the human verification performed
in the final contribution statement, as required by the assignment.
