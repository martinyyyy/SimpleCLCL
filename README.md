## What is it?
SimpleCLCL is a simple multiclipboard. 
It's written in .NET 4.5 using WPF. This started out as a quick (& maybe dirty) implementation of a multi clipboard but over time has envolved into something quite special. The way I code has completely changed and I even get annoyed in pair programing session if the partner doesn't have some kind of mutli clipboard.
This project was originaly inspired by http://www.nakka.com/soft/clcl/index_eng.html (thus the name SimpleCLCL) and used to be hosted on [Codeplex](https://simpleclcl.codeplex.com/).

## Why did you write this?
Easy, I don't like the style of the original one and it is missing a few things I really wanted. 

## Features
- saves up to 150 of your last clipboard entrys (even between restarts)
- only supports text (no pics, files. richtext gets stripped to *only* text when inserted using SimpleCLCL)
- inserts text only to clipboard
- inserts text to clipboard and sends crtl+v to last app
- search the clipboard history
- tooltip for "too long text" (accessible by mouseover and right-arrow key)
- clear history
- favorites
- rightclick to open a textbox for easier copy of text parts
- Run on startup
- Placeholder: Replace a given Placeholder in your string with the latest Clipboard Entry

## How to use
- ALT+C opens the list of saved clipboard items next to the mouse pointer
- select item by cursor, keyboard (up and down key or Shift)
- insert into clipboard and output to last app with space or return
- only copy to clipboard with crtl+c
- begin typing to open search

## Smartscreen / Stuck at 100% installation
Windows 10 Smartscreen may prevent the program from running because the executable is not signed by a known authority. Click on more info in the dialog and press run anyway to bypass the filter.

Sometimes Smartscreen doesn't even work like intended and doesn't inform the user the program is not known and just crashes. If you see the process "Smartscreen" in your task manager and SimpleCLCL isn't showing up, kill all Smartscreen process and the installation should run to completion. 
