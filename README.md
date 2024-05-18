# Piano Training app

This program is designed for use specifically with CASIO LK-S250 synthesizer. It can read MIDI files and guide the user to correctly play it on a piano. It supports learning with piano key lighting, falling notes on screen and sheet music (upcoming feature).

## Description

The application connects to the synthesizer via the MIDI interface, reads MIDI messages of key state changes, and sends messages bytewise to turn on the note hint light at the right time. The program is able to parse MIDI files, split tracks by notes, and play the music. Falling notes are displayed on the screen to show the user what to press.

## Unique features

- The program lights up piano keys in advance, letting the user be prepared to press a key.
- Key lighting frequency indicates time to press a key.
- The program uses a workaround to CASIO LK-S250 hardware limitation (piano can show only 4 keys at once) - by fast switching key lights sequentially, it can display any number of notes.

This description will be extended soon.
