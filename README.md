# HeadlessAllowList

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that adds per-session allowlists to headless servers

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Place [HeadlessAllowList.dll](https://github.com/GrandtheUK/HeadlessAllowList/releases/latest/download/HeadlessAllowList.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create this folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Resonite logs.

## Commands
HeadlessAllowList adds some additional commands to the Headless CLI that can be used as below
```bash
allowlist session [SessionID] add/remove/block/unblock [UserId] # Adds, removes or blocks a userId from the sessionId
allowlist session [SessionID] enable/disable # Enables disables allowlist for a sessionId
allowlist session [SessionID] exclusive enable/disable # Enables exclusive AllowList on a session
allowlist session [SessionID] list # Lists allowed users for a session
allowlist session [SessionID] message set [quoted deny message] # sets the deny message for a given session
allowlist session [SessionID] message remove # removes the deny message for a given session (falls back to global)
allowlist global add/remove/block/unblock [UserId] # Adds, removes or blocks a userId from all hosted sessions
allowlist global message set [quoted deny message] # sets the deny message globally
allowlist global message remove # removes the deny message globally (falls back to hard coded message)
allowlist global list # lists allowed users in global whitelist
```
`SessionID` can be replaced with `@` to target the currently focused world.

## The Allowlist
This mods enables you to either set one global whitelist or add multiple separate whitelists per session, it's your choice. It also gives global and session blocklists as well. 
An important option is the exclusive mode, which only accepts users in the allowlist for the specific session and rejects global allowlist if they aren't on the session allowlist.

The allowlist can be edited in the config file but it can be easier to manage with the commands above.

## Custom Deny Messages

Custom Deny messages can be set per session with a global fallback option with the following command with quotes which will be removed upon adding.

```bash
allowlist session [SessionID] message set "My custom session Deny Message"
allowlist global message set "My custom global Deny Message"
```