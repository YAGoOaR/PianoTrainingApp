# Piano Training app for CASIO LK-S250

![Early gameplay](https://github.com/YAGoOaR/Images/blob/main/PianoTrainingApp/gameplay-early.png)

![Early gameplay video](https://github.com/YAGoOaR/Images/blob/main/PianoTrainingApp/demo.gif)

This program is designed for use specifically with CASIO LK-S250 synthesizer. It can read MIDI files and guide the user to correctly play it on a piano. It supports learning with piano key lighting, falling notes on screen and sheet music.
Now the development is in progress and the executable program will be placed in [Releases](https://github.com/YAGoOaR/PianoTrainingApp/releases) when ready.

## Description

The application connects to the synthesizer via the MIDI interface, reads MIDI messages of key state changes, and sends messages bytewise to turn on the note hint light at the right time. The program is able to parse MIDI files, split tracks by notes, and play the music. Falling notes are displayed on the screen to show the user what to press.

## Unique features

- The program lights up piano keys in advance, letting the user be prepared to press a key.
- Key light frequency indicates time to press a key.
- The program uses a workaround to CASIO LK-S250 hardware limitation (piano can show only 4 keys at once) - by fast switching key lights sequentially, it can display any number of notes.

## License 
[![License](http://img.shields.io/:license-mit-blue.svg?style=flat-square)](http://badges.mit-license.org)
</br>- Copyright 2024 © <a href="https://github.com/YAGoOaR" target="_blank">YAGoOaR</a>.
