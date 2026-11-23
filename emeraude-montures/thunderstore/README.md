# Emeraude Montures

Tweaks for the mounts of [Mount Up Restored](https://thunderstore.io/c/valheim/p/Meldurson/MountUpRestored/).

## Mounts

Wolves, boars and loxes can be ridden. The base game also allows the asksvin: a saddle can no longer be put on it, and it can no longer be ridden. An existing saddle can still be removed (alternate interact). The refusal message is in French.

## Health

A tamed wolf or boar (not the lox, which keeps its own health) gets the max health of the player riding it, like a second life: a boar no longer dies in a few hits. It never gets less than its own max health, and keeps its health percentage (a boar at full health stays at full health). The value is saved on the creature and follows the rider's food while riding.

## Camera

Mount Up attaches the rider to a bone of the mount's back, which moves with every stride, and the camera followed it. While riding, the camera now follows the mount's body with a smoothed rider position, and stays level.

## Installation

Install it on **every player's game**: the rider's game sets the mount's health and moves the camera. Installing it on the server too is harmless.

## Configuration

`BepInEx/config/emeraude.montures.cfg`

| Setting | Default | Effect |
|---|---|---|
| `Camera.CameraSmoothing` | `1` | While riding, how long the camera takes to follow the rider's bobbing on the mount, in seconds. Higher is steadier. |
