# Changelog

## 1.1.0 — 2026-11-23
- The lox can be ridden again (saddle and riding as in the base game). Its health is not changed: only wolves and boars get the rider's max health.
- The asksvin still can't be ridden.

## 1.0.0 — 2026-11-13
- Only wolves and boars can be ridden: a saddle can no longer be put on a lox or an asksvin, and they can no longer be ridden. An existing saddle can still be removed.
- A tamed wolf or boar gets the max health of the player riding it (never less than its own), keeping its health percentage. The value stays on the creature.
- While riding, the camera follows the mount's body, smoothed, instead of the bone the rider is attached to, and no longer tilts.
- Setting `Camera.CameraSmoothing` (1 s by default).
