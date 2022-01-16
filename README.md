# mario
Automated trading of crypto with Coinbase Pro using C# utilizing technical analysis.
<br/><br/>
The source code can be adapted to work with equities (stocks), forex, commodities, or any markets where price/volume instruments can be gauged.

Warning
=======

THIS IS A PROOF OF CONCEPT.<br/>
THIS IS NOT PRODUCTION QUALITY CODE.<br/>
TAKE THE KEY ASPECTS OF THE LOGIC AND CODE IT THE "RIGHT" WAY (CODING IT IN RUST INSTEAD OF C#).
<br/>
<br/>
TRADING IN CRYPTOCURRENCY INVOLVES A LOT OF RISKS.<br/>
KNOW WHAT YOU'RE DOING OR YOU'RE GOING TO END UP LOSING EVERYTHING.<br/>
I GURANTEE IT.
<br/>
<br/>
I (THE AUTHOR OF THIS WORK) TAKE NO RESPONSBILITY OF WHAT HAPPENS TO YOU OR YOUR TRADING ACCOUNT(S) IF YOU USE ANY ASPECT OF THIS WORK.
<br/>
<br/>
THE SOURCE CODE AVAILABLE IN THIS PROJECT IS ONLY FOR EDUCATIONAL STUDY.
<br/>
<br/>
TRADING PURELY ON TECHNICAL ANALYSIS ALSO CARRY RISKS AS THERE ARE "FALSE SIGNALS" (I.E. BEAR/BULL TRAPS)

Requirements
============

1. Coinbase Pro account with API credentials
2. MongoDB 5.x
3. Microsoft .NET 6.0

Optional
=======================

1. Microsoft Visual Studio
2. MongoDB Compass

Project Dependencies
===================================
1. Coinbase.Pro
2. MongoDB.Driver
3. Newtonsoft.Json
4. Skender.Stock.Indicators
5. Tulip.NETCore
6. WebSocketSharp-netstandard

Setup
===================================
1. Create a MongoDB database and setup the collections from the folder - "MongoDB_Schema".
2. Create a CLI (command line interface) project and add the source code from this project.
3. Add the required project dependencies listed above into your project solution.
4. Program.cs is the entry point.

<hr/>

The project was built using Apple MacOS so there are OS specific calls that you will need to workaround (if you're on a different platform). An example, to trigger the audio sound when buying/selling a coin - the code utilizes the command line "afplay" which is MacOS specific. Windows/Linux systems can substitute with VLCPLayer command interface to mimic the same functionality.
