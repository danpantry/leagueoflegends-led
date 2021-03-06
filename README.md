# leagueoflegends-led
A modular League of Legends integration for RGB LED devices.

![](repo/video-gif.gif)


## Features
- Health Bar
- Death Lights
- Keeps track of cooldowns and mana
- Custom spellcast animations for several champions
- Compatible with E1.31 devices

![Death light](repo/img-death.png)
![Out of Mana light](repo/img-oom.png)

## Usage
- Setup a LED strip that listens to color data via sACN. If you don't have a LED strip, you will be able to see the simulated LED display in your screen.
- Load into a LoL game (preferably with Vel'Koz)
- When the game loads, open the program
- Enjoy the lights!

## Roadmap / Planned Features
- Animation sets for each champion
- Effects for summoner spells
- Effects for items
- Razer Chroma integration

## Known Issues
- Window focus isn't taken into account
- Game chat isn't taken into account (keypresses are treated as ability casts)

## Contributing
Check out the [contributing guide](CONTRIBUTING.md). Help is greatly appreciated with anything, really.
